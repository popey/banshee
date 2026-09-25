/***************************************************************************
 *  HttpFileDownloadTask.cs
 *
 *  Copyright (C) 2007 Michael C. Urbanski
 *  Written by Mike Urbanski <michael.c.urbanski@gmail.com>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW:
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),
 *  to deal in the Software without restriction, including without limitation
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,
 *  and/or sell copies of the Software, and to permit persons to whom the
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 *  DEALINGS IN THE SOFTWARE.
 */

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.ComponentModel;

using Migo.Net;
using MN = Migo.Net;
using Migo.TaskCore;

namespace Migo.DownloadCore
{
	public class HttpFileDownloadTask : Task, IDisposable
	{
	    // Roll these into a enum
        private bool canceled;
        private bool completed;
        private bool executing;
        private bool paused;
        private bool stopped;

	    private bool disposed;

        private bool preexistingFile;

        private int modified;
        private int rangeError;

        private string mimeType;
	    private Uri remoteUri;
	    private string localPath;
	
        private HttpStatusCode httpStatus;
        private HttpFileDownloadErrors error;

	    private AsyncWebClient wc;	
	    private FileStream localStream;	
	
        private ManualResetEvent mre;
	
	    public virtual long BytesReceived {
	        get {
	            long ret = 0;
	
	            lock (SyncRoot) {
	                if (wc != null) {
                        ret =  wc.Status.BytesReceived;
	                }
	            }
	
	            return ret;
	        }
	    }

        public string ErrorMessage { get; private set; }

        private void SetErrorMessage (Exception exception)
        {
            var web = exception as WebException;
            if (web != null) {
                var response = web.Response as HttpWebResponse;
                if (response != null) {
                    ErrorMessage = "The server returned HTTP " + (int) response.StatusCode + ".";
                } else if (web.Status == WebExceptionStatus.TrustFailure || web.Status == WebExceptionStatus.SecureChannelFailure) {
                    ErrorMessage = "The server's TLS certificate could not be verified.";
                } else if (web.Status == WebExceptionStatus.Timeout) {
                    ErrorMessage = "The download timed out.";
                } else if (web.Status == WebExceptionStatus.NameResolutionFailure) {
                    ErrorMessage = "The server name could not be resolved.";
                } else {
                    ErrorMessage = "The network connection failed or was interrupted.";
                }
            } else if (exception is UnauthorizedAccessException) {
                ErrorMessage = "Permission denied when writing the download folder.";
            } else if (exception is IOException) {
                ErrorMessage = "The download could not be read or written completely. Check disk space and retry.";
            } else {
                ErrorMessage = "The download could not be completed.";
            }
        }

        public HttpFileDownloadErrors Error
        {
            get { return error; }
        }

        public HttpStatusCode HttpStatusCode
        {
            get { return httpStatus; }
        }

        public string LocalPath
        {
            get { return localPath; }
        }

        public string MimeType
        {
            get { return mimeType; }
        }

        public Uri RemoteUri
        {
            get { return remoteUri; }
        }

	    public override WaitHandle WaitHandle
	    {
	        get {
	            lock (SyncRoot) {
	                if (mre == null) {
                        mre = new ManualResetEvent (false);
	                }
	
	                return mre;
	            }
	        }
	    }
	
	    public HttpFileDownloadTask (string remoteUri, string localPath, object userState)
            : base (String.Empty, userState)
	    {
	        this.remoteUri = new Uri (remoteUri);
	        this.localPath = localPath;	
	    }
	
	    public HttpFileDownloadTask (string remoteUri, string localPath)
            : this (remoteUri, localPath, null) {}

        private bool SetStopped ()
        {
            bool ret = false;

            lock (SyncRoot) {
                if (!canceled && !stopped) {
                    ret = stopped = true;
                }
            }

            return ret;
        }

        private bool SetCanceled ()
        {
            bool ret = false;

            lock (SyncRoot) {
                if (!canceled && !stopped) {
                    ret = canceled = true;
                }
            }

            return ret;
        }

        private bool SetCompleted ()
        {
            bool ret = false;

            lock (SyncRoot) {
                if (!completed) {
                    ret = completed = true;
                }
            }

            return ret;
        }

        protected bool SetExecuting ()
        {
            bool ret = false;

            lock (SyncRoot) {
                // SRSLY?  WTF!  Use an enum!
                if (!(canceled || executing || completed || paused || stopped)) {
                    ret = executing = true;

                    if (mre != null) {
                        mre.Reset ();
                    }
                }
            }

            return ret;
        }

        public override void CancelAsync ()
	    {
            //Console.WriteLine ("CancelAsync ():  {0} - {1}", remoteUri.ToString (), Status);

            bool cancelLive = false;

            lock (SyncRoot) {
                if (executing) {
                    cancelLive = true;
                }

                if (SetCanceled ()) {
                    if (cancelLive) {
                        wc.CancelAsync ();
                    } else {
                        if (SetCompleted ()) {
                            CloseLocalStream (true);
                            SetStatus (TaskStatus.Cancelled);
                            OnTaskCompleted (null, true);
                        }
                    }
                }
            }
	    }

        public void Dispose ()
        {
            lock (SyncRoot) {
                if (!disposed) {
                    if (mre != null) {
                        mre.Close ();
                        mre = null;
                    }

                    disposed = true;
                }
            }
        }

	    public override void ExecuteAsync ()
	    {
            if (SetExecuting ()) {                            	
                lock (SyncRoot) {
                    SetStatus (TaskStatus.Running);
                    ExecuteImpl ();
                }
            }
	    }
	
        private void ExecuteImpl ()
        {
            Exception err = null;
            error = HttpFileDownloadErrors.None;
            ErrorMessage = null;
            bool fileOpenError = false;

            try {
                OpenLocalStream ();
            } catch (UnauthorizedAccessException e) {
                err = e;
                error = HttpFileDownloadErrors.UnauthorizedFileAccess;
            } catch (IOException e) {
                err = e;
                error = HttpFileDownloadErrors.SharingViolation; // Probably
            } catch (Exception e) {
                err = e;
                error = HttpFileDownloadErrors.Unknown;
            }

            if (error != HttpFileDownloadErrors.None) {
                fileOpenError = true;   	
            }

            if (err == null) {
                try {
                    InitWebClient ();
                    wc.DownloadFileAsync (remoteUri, localStream);
                } catch (Exception e) {
                    err = e;
                }
            }

            if (err != null) {
                SetErrorMessage (err);
                executing = false;
                SetCompleted ();

                if (!fileOpenError) {
                    CloseLocalStream (true);
                }

                DestroyWebClient ();

                SetStatus (TaskStatus.Failed);
                OnTaskCompleted (err, false);
                if (mre != null) mre.Set ();
            }
        }
	
	    public override void Pause ()
	    {
            lock (SyncRoot) {	
                //Console.WriteLine ("Pause ():  {0} - {1}", remoteUri.ToString (), Status);
                if (!IsCompleted && !paused) {
                    paused = true;

                    if (Status == TaskStatus.Running) {
                        wc.CancelAsync ();
                    } else {
                        SetStatus (TaskStatus.Paused);
                    }
                }
            }
	    }
	
	    public override void Resume ()
	    {
            lock (SyncRoot) {
                //Console.WriteLine ("Resume ():  {0} - {1}", remoteUri.ToString (), Status);
                if (Status == TaskStatus.Paused) {
                    paused = false;
                    SetStatus (TaskStatus.Ready);
                }
            }
	    }	
	
        public override void Stop ()
        {
            //Console.WriteLine ("Stop ():  {0} - {1}", remoteUri.ToString (), Status);

            bool stopLive = false;
            TaskStatus oldStatus = TaskStatus.Ready;

            lock (SyncRoot) {
                if (executing) {
                    stopLive = true;
                } else {
                    oldStatus = Status;
                    SetStatus (TaskStatus.Stopped, false);
                }

                if (SetStopped ()) {
                    if (stopLive) {
                        wc.CancelAsync ();
                    } else {
                        if (SetCompleted ()) {
                            CloseLocalStream (false);
                            OnStatusChanged (oldStatus, TaskStatus.Stopped);
                            OnTaskCompleted (null, false);
                        }
                    }
                }
            }
        }

	    private void InitWebClient ()
	    {
            wc = new AsyncWebClient ();

            if (localStream.Length > 0) {
                wc.Range = Convert.ToInt32 (localStream.Length);
                //Console.WriteLine ("Adding Range:  {0}", wc.Range);
            }

	        wc.Timeout = (5 * 60 * 1000);
	        wc.DownloadFileCompleted += OnDownloadFileCompletedHandler;
	        wc.DownloadProgressChanged += OnDownloadProgressChangedHandler;
	        wc.ResponseReceived += OnResponseReceivedHandler;	
	    }
	
	    private void DestroyWebClient ()
	    {
            if (wc != null) {
                wc.DownloadFileCompleted -= OnDownloadFileCompletedHandler;
                wc.DownloadProgressChanged -= OnDownloadProgressChangedHandler;
                wc.ResponseReceived -= OnResponseReceivedHandler;
                wc = null;        	
            }            	
	    }

        private void OpenLocalStream ()
        {
            if (File.Exists (localPath)) {
                localStream = File.Open (
                    localPath, FileMode.Open,
                    FileAccess.Write, FileShare.None
                );

                localStream.Seek (0, SeekOrigin.End);
                preexistingFile = true;
            } else {
                preexistingFile = false;

                if (!Directory.Exists (Path.GetDirectoryName (localPath))) {
                    Directory.CreateDirectory (Path.GetDirectoryName (localPath));
                }

                localStream = File.Open (
                    localPath, FileMode.OpenOrCreate,
                    FileAccess.Write, FileShare.None
                );
            }
        }

        private void CloseLocalStream (bool removeFile)
        {
            try {
                if (localStream != null) {
                    localStream.Close ();
                    localStream = null;
                }
            } catch {}

            if (removeFile) {
                RemoveFile ();
            }
        }

        private void RemoveFile ()
        {
            if (File.Exists (localPath)) {
                try {
                    preexistingFile = false;
                    File.Delete (localPath);
                    Directory.Delete (Path.GetDirectoryName (localPath));
                } catch {}
            }
        }

	    private void OnDownloadFileCompletedHandler (object sender,
	                                                 AsyncCompletedEventArgs e)
	    {
            bool retry = false;

            lock (SyncRoot) {
                executing = false;
                TaskStatus newStatus = Status;               	
	
                try {
                    if (e.Error != null) {
                        SetErrorMessage (e.Error);
                        // Keep signed URLs and credentials out of logs.
                        Hyena.Log.Warning ("Podcast download failed", ErrorMessage);
                        WebException we = e.Error as WebException;

                        if (we != null) {
                            if(we.Status == WebExceptionStatus.ProtocolError) {
                                HttpWebResponse resp = we.Response as HttpWebResponse;

                                if (resp != null) {
                                    httpStatus = resp.StatusCode;

                                    // This is going to get triggered if the file on disk is complete.
                                    // Maybe request range-1 and see if a content length of 0 is returned.
                                    if (resp.StatusCode ==
                                        HttpStatusCode.RequestedRangeNotSatisfiable) {

                                        if (rangeError++ == 0) {
                                            retry = true;
                                        }
                                    }
                                }
                            }
                        }

                        if (!retry) {
                            error = HttpFileDownloadErrors.HttpError;
                            newStatus = TaskStatus.Failed;
                        }
                    } else if (modified > 0) {
                        if (modified == 1) {
                            retry = true;
                        } else {
                            newStatus = TaskStatus.Failed;
                        }
                    } else if (canceled) {
                        newStatus = TaskStatus.Cancelled;
                    } else if (paused) {
                        newStatus = TaskStatus.Paused;
                    } else if (stopped) {
                        newStatus = TaskStatus.Stopped;
                    } else {
                        newStatus = TaskStatus.Succeeded;
                    }
                } catch/* (Exception ex)*/ {
                    //Console.WriteLine ("Error__________________{0}", ex.Message);
                } finally {
                    if (retry) {
                        CloseLocalStream (true);
                        DestroyWebClient ();
                        modified = 0;
                        executing = true;
                        ExecuteImpl ();
                    } else if (SetCompleted ()) {
                        switch (newStatus) {
                        case TaskStatus.Cancelled: goto case TaskStatus.Failed;
                        case TaskStatus.Failed:
                            CloseLocalStream (true);
                            break;
                        case TaskStatus.Paused:
                            completed = false;
                            goto case TaskStatus.Succeeded;
                        case TaskStatus.Stopped: goto case TaskStatus.Succeeded;
                        case TaskStatus.Succeeded:
                            CloseLocalStream (false);
                            break;
                        default:  goto case TaskStatus.Succeeded;
                        }

                        DestroyWebClient ();

                        SetStatus (newStatus);
                        OnTaskCompleted (e.Error, canceled);

                        if (mre != null) {
                            mre.Set ();
                        }
                    }
                }
	        }
	    }

	    private void OnDownloadProgressChangedHandler (object sender,
	                                                   MN.DownloadProgressChangedEventArgs e)
	    {
            lock (SyncRoot) {
                if (e.ProgressPercentage != 0) {
                    SetProgress (e.ProgressPercentage);
                }
	        }
	    }	
	
        private void OnResponseReceivedHandler (object sender, EventArgs e)
        {
            lock (SyncRoot) {
                if (wc != null && wc.ResponseHeaders != null) {
                    mimeType = wc.ResponseHeaders.Get ("Content-Type");

                    httpStatus = wc.Response.StatusCode;

                    // A server may ignore Range and return the entire file.
                    // Discard the partial file and retry once, never append it.
                    string content_range = wc.ResponseHeaders.Get ("Content-Range");
                    bool invalid_range = preexistingFile && localStream.Length > 0 &&
                        (httpStatus != HttpStatusCode.PartialContent || content_range == null ||
                         !content_range.StartsWith ("bytes " + localStream.Length + "-", StringComparison.Ordinal));
                    if (invalid_range || (preexistingFile && wc.ResponseHeaders.Get ("Last-Modified") != null &&
                        wc.Response.LastModified.ToUniversalTime () >
                        File.GetLastWriteTimeUtc (localPath))) {
                        ++modified;

                        wc.CancelAsync ();
                    }
                }
            }
        }
	}
}
