//
// MetadataServiceJob.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006-2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Net;
using System.Collections.Generic;

using Hyena;

using Banshee.Base;
using Banshee.Kernel;
using Banshee.Collection;
using Banshee.Streaming;
using Banshee.Networking;
using Banshee.ServiceStack;

namespace Banshee.Metadata
{
    public class MetadataServiceJob : IMetadataLookupJob
    {
        private MetadataService service;
        private bool cancelled;
        private IBasicTrackInfo track;
        private List<StreamTag> tags = new List<StreamTag>();
        private IMetadataLookupJob current_job;

        protected bool InternetConnected {
            get { return ServiceManager.Get<Network> ().Connected; }
        }

        protected MetadataServiceJob()
        {
        }

        public MetadataServiceJob(MetadataService service, IBasicTrackInfo track)
        {
            this.service = service;
            this.track = track;
        }

        public virtual void Cancel ()
        {
            cancelled = true;
            lock (this) {
                if (current_job != null) {
                    current_job.Cancel ();
                }
            }
        }

        public virtual void Run()
        {
            foreach(IMetadataProvider provider in service.Providers) {
                if (cancelled)
                    break;;

                try {
                    lock (this) {
                        current_job = provider.CreateJob(track);
                    }

                    current_job.Run();

                    if (cancelled)
                        break;;

                    foreach(StreamTag tag in current_job.ResultTags) {
                        AddTag(tag);
                    }

                    lock (this) {
                        current_job = null;
                    }
                } catch (System.Threading.ThreadAbortException) {
                    throw;
                } catch(Exception e) {
                   Hyena.Log.Exception (e);
                }
            }

            service.OnHaveResult (this);
        }

        public virtual IBasicTrackInfo Track {
            get { return track; }
            protected set { track = value; }
        }

        public virtual IList<StreamTag> ResultTags {
            get { return tags; }
        }

        protected void AddTag(StreamTag tag)
        {
            tags.Add(tag);
        }

        protected HttpWebResponse GetHttpStream(Uri uri)
        {
            return GetHttpStream(uri, null);
        }

        protected HttpWebResponse GetHttpStream(Uri uri, string [] ignoreMimeTypes)
        {
            if(!InternetConnected) {
                throw new NetworkUnavailableException();
            }

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri.AbsoluteUri);
            request.UserAgent = Banshee.Web.Browser.UserAgent;
            request.Timeout = 10 * 1000;
            request.ReadWriteTimeout = 10 * 1000;
            request.KeepAlive = false;
            request.AllowAutoRedirect = true;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if(ignoreMimeTypes != null) {
                string [] content_types = response.Headers.GetValues("Content-Type");
                if(content_types != null) {
                    foreach(string content_type in content_types) {
                        for(int i = 0; i < ignoreMimeTypes.Length; i++) {
                            if(content_type == ignoreMimeTypes[i]) {
                                response.Close ();
                                return null;
                            }
                        }
                    }
                }
            }

            return response;
        }

        protected bool SaveHttpStream(Uri uri, string path)
        {
            return SaveHttpStream(uri, path, null);
        }

        protected bool SaveHttpStream(Uri uri, string path, string [] ignoreMimeTypes)
        {
            HttpWebResponse response = GetHttpStream(uri, ignoreMimeTypes);
            Stream from_stream = response == null ? null : response.GetResponseStream ();
            if(from_stream == null) {
                if (response != null) {
                    response.Close ();
                }
                return false;
            }

            SaveAtomically (path, from_stream);

            from_stream.Close ();

            return true;
        }

        protected bool SaveHttpStreamCover (Uri uri, string albumArtistId, string [] ignoreMimeTypes)
        {
            if (String.IsNullOrEmpty (albumArtistId)) return false;
            string path = CoverArtSpec.GetPath (albumArtistId);
            if (File.Exists (path)) return true;
            if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) return false;
            using (var response = GetHttpStream (uri, ignoreMimeTypes)) {
                if (response == null) return false;
                using (var stream = response.GetResponseStream ()) {
                    return SaveCoverResponse (path, stream, response.ContentLength, response.ContentType);
                }
            }
        }

        // Shared by the album, podcast and Archive providers. Never cache an HTML
        // error as a cover, or let a truncated/oversized download hide future retries.
        internal bool SaveCoverResponse (string path, Stream stream, long length, string contentType)
        {
            const int max = 8 * 1024 * 1024;
            if (stream == null || length > max ||
                (contentType != null && (contentType.StartsWith ("text/", StringComparison.OrdinalIgnoreCase) ||
                    contentType.IndexOf ("json", StringComparison.OrdinalIgnoreCase) >= 0))) return false;
            using (var bytes = new MemoryStream ()) {
                byte[] buffer = new byte[8192];
                int count;
                while ((count = stream.Read (buffer, 0, buffer.Length)) > 0) {
                    if (bytes.Length + count > max) return false;
                    bytes.Write (buffer, 0, count);
                }
                if (length >= 0 && bytes.Length != length) return false;
                byte[] data = bytes.ToArray ();
                bool jpeg = data.Length >= 4 && data[0] == 0xff && data[1] == 0xd8 &&
                    data[data.Length - 2] == 0xff && data[data.Length - 1] == 0xd9;
                bool png = data.Length >= 20 && data[0] == 137 && data[1] == 80 && data[2] == 78 &&
                    data[3] == 71 && data[4] == 13 && data[5] == 10 && data[6] == 26 && data[7] == 10 &&
                    data[data.Length - 8] == 73 && data[data.Length - 7] == 69 &&
                    data[data.Length - 6] == 78 && data[data.Length - 5] == 68;
                bool gif = data.Length >= 14 && data[0] == 71 && data[1] == 73 && data[2] == 70 &&
                    data[3] == 56 && (data[4] == 55 || data[4] == 57) && data[5] == 97 &&
                    data[data.Length - 1] == 59;
                if (!jpeg && !png && !gif) return false;
                Directory.CreateDirectory (Path.GetDirectoryName (path));
                bytes.Position = 0;
                SaveAtomically (path, bytes);
                return File.Exists (path);
            }
        }

        protected void SaveAtomically (string path, Stream from_stream)
        {
            if (String.IsNullOrEmpty (path) || from_stream == null || !from_stream.CanRead) {
                return;
            }

            SafeUri path_uri = new SafeUri (path);
            if (Banshee.IO.File.Exists (path_uri)) {
                return;
            }

            // Save the file to a temporary path while downloading/copying,
            // so that nobody sees it and thinks it's ready for use before it is
            SafeUri tmp_uri = new SafeUri (String.Format ("{0}.part", path));
            try {
                Banshee.IO.StreamAssist.Save (from_stream, Banshee.IO.File.OpenWrite (tmp_uri, true));
                Banshee.IO.File.Move (tmp_uri, path_uri);
            } catch (Exception e) {
                Hyena.Log.Exception (e);
            }
        }
    }
}
