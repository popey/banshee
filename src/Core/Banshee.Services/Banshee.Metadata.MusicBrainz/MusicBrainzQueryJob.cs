//
// MusicBrainzQueryJob.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Aurélien Mino <aurelien.mino@gmail.com>
//
// Copyright (C) 2006-2008 Novell, Inc.
// Copyright (C) 2010 Aurélien Mino
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
using System.Collections;
using System.IO;
using System.Net;
using System.Xml;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Web;
using System.Text.RegularExpressions;

using MusicBrainz;

using Hyena;
using Banshee.Base;
using Banshee.Metadata;
using Banshee.Kernel;
using Banshee.Collection;
using Banshee.Streaming;
using Banshee.Networking;
using Banshee.Collection.Database;

namespace Banshee.Metadata.MusicBrainz
{
    // The old MusicBrainz v1/ASIN lookup is retired. Tagged release IDs can
    // address Cover Art Archive directly; untagged albums fall back to Last.fm.
    public class MusicBrainzQueryJob : MetadataServiceJob
    {
        public MusicBrainzQueryJob (IBasicTrackInfo track) { Track = track; }
        public override void Run () { Lookup (); }
        public bool Lookup ()
        {
            var track = Track as TrackInfo;
            Guid mbid;
            if (track == null || (track.MediaAttributes & TrackMediaAttributes.Podcast) != 0 ||
                track.ArtworkId == null || CoverArtSpec.CoverExists (track.ArtworkId) ||
                !Guid.TryParse (track.AlbumMusicBrainzId, out mbid) || !InternetConnected) {
                return false;
            }
            try {
                if (SaveHttpStreamCover (new Uri ("https://coverartarchive.org/release/" +
                    mbid.ToString () + "/front-500"), track.ArtworkId, null)) {
                    AddTag (new StreamTag { Name = CommonTags.AlbumCoverId, Value = track.ArtworkId });
                    return true;
                }
            } catch (WebException e) {
                // Missing art, rate limiting and network failures must permit the fallback.
                if (e.Response != null) e.Response.Close ();
                Log.Debug ("Cover Art Archive lookup unavailable", e.Status.ToString ());
            }
            return false;
        }
    }
}
