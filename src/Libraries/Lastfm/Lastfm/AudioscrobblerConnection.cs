//
// AudioscrobblerConnection.cs
//
// Author:
//   Chris Toshok <toshok@ximian.com>
//   Alexander Hixon <hixon.alexander@mediati.org>
//   Phil Trimble <philtrimble@gmail.com>
//
// Copyright (C) 2005-2008 Novell, Inc.
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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Timers;
using System.Security.Cryptography;
using System.Web;

using Hyena;
using Hyena.Json;
using Mono.Unix;

namespace Lastfm
{
    public class AudioscrobblerConnection
    {
        private enum State {
            Idle,
            NeedTransmit,
            Transmitting,
            WaitingForResponse
        };

        private const int TICK_INTERVAL = 2000; /* 2 seconds */
        private const int RETRY_SECONDS = 60; /* 60 second delay for transmission retries */
        private const int TIME_OUT = 10000; /* 10 seconds timeout for webrequests */

        private bool connected = false; /* if we're connected to network or not */
        public bool Connected {
            get { return connected; }
        }

        private bool started = false; /* engine has started and was/is connected to AS */
        public bool Started {
            get { return started; }
        }

        private System.Timers.Timer timer;
        private DateTime next_interval;

        private IQueue queue;

        private int hard_failures = 0;

        private bool now_playing_started;
        private LastfmRequest current_now_playing_request;
        private LastfmRequest current_scrobble_request;
        private IAsyncResult current_async_result;
        private State state;

        internal AudioscrobblerConnection (IQueue queue)
        {
            LastfmCore.Account.Updated += AccountUpdated;

            state = State.Idle;
            this.queue = queue;
            queue.TrackAdded += delegate(object o, EventArgs args) {
                StartTransitionHandler ();
            };

        }

        private void AccountUpdated (object o, EventArgs args)
        {
            Stop ();
            Start ();
        }

        public void UpdateNetworkState (bool connected)
        {
            Log.DebugFormat ("Audioscrobbler state: {0}", connected ? "connected" : "disconnected");
            this.connected = connected;
        }

        public void Start ()
        {
            if (started || String.IsNullOrEmpty (LastfmCore.Account.SessionKey)) {
                return;
            }

            started = true;
            hard_failures = 0;

            queue.Load ();
            StartTransitionHandler ();
        }

        private void StartTransitionHandler ()
        {
            if (!started) {
                // Don't run if we're not actually started.
                return;
            }

            if (timer == null) {
                timer = new System.Timers.Timer ();
                timer.Interval = TICK_INTERVAL;
                timer.AutoReset = true;
                timer.Elapsed += new ElapsedEventHandler (StateTransitionHandler);

                timer.Start ();
            } else if (!timer.Enabled) {
                timer.Start ();
            }
        }

        public void Stop ()
        {
            StopTransitionHandler ();

            queue.Save ();

            started = false;
        }

        private void StopTransitionHandler ()
        {
            if (timer != null) {
                timer.Stop ();
            }
        }

        private void StateTransitionHandler (object o, ElapsedEventArgs e)
        {
            // if we're not connected, don't bother doing anything involving the network.
            if (!connected) {
                return;
            }

            if ((state == State.Idle || state == State.NeedTransmit) && hard_failures > 2) {
                hard_failures = 0;
            }

            // and address changes in our engine state
            switch (state) {
            case State.Idle:
                if (queue.Count > 0) {
                    state = State.NeedTransmit;
                } else {
                    StopTransitionHandler ();
                }

                break;
            case State.NeedTransmit:
                if (DateTime.Now > next_interval) {
                    TransmitQueue ();
                }
                break;
            case State.Transmitting:
            case State.WaitingForResponse:
                // nothing here
                break;
            }
        }

        private void TransmitQueue ()
        {
            // save here in case we're interrupted before we complete
            // the request.  we save it again when we get an OK back
            // from the server
            queue.Save ();

            next_interval = DateTime.MinValue;

            if (!connected) {
                return;
            }

            current_scrobble_request = new LastfmRequest ("track.scrobble", RequestType.Write, ResponseFormat.Json);
            IList<IQueuedTrack> tracks = queue.GetTracks ();

            int batch_count = Math.Min (50, tracks.Count);
            for (int i = 0; i < batch_count; i++) {
                IQueuedTrack track = tracks[i];

                string str_track_number = String.Empty;
                if (track.TrackNumber != 0) {
                    str_track_number = track.TrackNumber.ToString();
                }

                bool chosen_by_user = (track.TrackAuth.Length == 0);

                current_scrobble_request.AddParameter (String.Format ("timestamp[{0}]", i), track.StartTime.ToString ());
                current_scrobble_request.AddParameter (String.Format ("track[{0}]", i), track.Title);
                current_scrobble_request.AddParameter (String.Format ("artist[{0}]", i), track.Artist);
                current_scrobble_request.AddParameter (String.Format ("album[{0}]", i), track.Album);
                current_scrobble_request.AddParameter (String.Format ("trackNumber[{0}]", i), str_track_number);
                current_scrobble_request.AddParameter (String.Format ("duration[{0}]", i), track.Duration.ToString ());
                current_scrobble_request.AddParameter (String.Format ("mbid[{0}]", i), track.MusicBrainzId);
                current_scrobble_request.AddParameter (String.Format ("chosenByUser[{0}]", i), chosen_by_user ? "1" : "0");
            }

            Log.DebugFormat ("Last.fm scrobbler sending '{0}'", current_scrobble_request.ToString ());

            // The HTTP request owns its timeout. Do not start another upload while
            // its callback can still remove tracks from this queue.
            state = State.WaitingForResponse;
            current_async_result = current_scrobble_request.BeginSend (OnScrobbleResponse, batch_count);
        }

        private void OnScrobbleResponse (IAsyncResult ar)
        {
            int nb_tracks_scrobbled = 0;
            try {
                current_scrobble_request.EndSend (ar);
                nb_tracks_scrobbled = (int)ar.AsyncState;

            } catch (Exception e) {
                Log.Exception ("Failed to complete the scrobble request", e);
                next_interval = DateTime.Now.AddSeconds (RETRY_SECONDS);
                state = State.Idle;
                return;
            }

            JsonObject response = null;
            try {
                response = current_scrobble_request.GetResponseObject ();
                if (response == null) throw new InvalidOperationException ("No Last.fm response received");
            } catch (Exception e) {
                Log.Exception ("Failed to process the scrobble response", e);
                next_interval = DateTime.Now.AddSeconds (RETRY_SECONDS);
                state = State.Idle;
                return;
            }

            var error = current_scrobble_request.GetError ();
            if (error != StationError.None) {
                Log.WarningFormat ("Last.fm scrobble failed ({0}); keeping queued tracks for retry", error);
                next_interval = DateTime.Now.AddSeconds (RETRY_SECONDS);
                hard_failures++;
                state = State.Idle;
            } else {
                try {
                    var scrobbles = (JsonObject)response["scrobbles"];
                    var scrobbles_attr = (JsonObject)scrobbles["@attr"];
                    if (Convert.ToInt32 (scrobbles_attr["accepted"]) + Convert.ToInt32 (scrobbles_attr["ignored"]) != nb_tracks_scrobbled) {
                        throw new InvalidOperationException ("Last.fm acknowledged a different number of tracks");
                    }
                    Log.InformationFormat ("Audioscrobbler upload succeeded: {0} accepted, {1} ignored",
                                           scrobbles_attr["accepted"], scrobbles_attr["ignored"]);

                    if (nb_tracks_scrobbled > 1) {
                        var scrobble_array = (JsonArray)scrobbles["scrobble"];
                        foreach (JsonObject scrobbled_track in scrobble_array) {
                            LogIfIgnored (scrobbled_track);
                        }
                    } else {
                        var scrobbled_track = (JsonObject)scrobbles["scrobble"];
                        LogIfIgnored (scrobbled_track);
                    }
                } catch (Exception) {
                    Log.Warning ("Unrecognized Last.fm scrobble response; keeping queued tracks for retry");
                    next_interval = DateTime.Now.AddSeconds (RETRY_SECONDS);
                    state = State.Idle;
                    return;
                }

                hard_failures = 0;

                // we succeeded, pop the elements off our queue
                queue.RemoveRange (0, nb_tracks_scrobbled);
                queue.Save ();

                state = State.Idle;
            }
        }

        private void LogIfIgnored (JsonObject scrobbled_track)
        {
            var ignoredMessage = (JsonObject)scrobbled_track["ignoredMessage"];

            if (Convert.ToInt32 (ignoredMessage["code"]) == 0) {
                return;
            }

            var track = (JsonObject)scrobbled_track["track"];
            var artist = (JsonObject)scrobbled_track["artist"];
            var album = (JsonObject)scrobbled_track["album"];

            Log.InformationFormat ("Track {0} - {1} (on {2}) ignored by Last.fm, reason: {3}",
                                   artist["#text"], track["#text"], album["#text"],
                                   ignoredMessage["#text"]);
        }

        public void NowPlaying (string artist, string title, string album, double duration,
                                int tracknum)
        {
            NowPlaying (artist, title, album, duration, tracknum, "");
        }

        public void NowPlaying (string artist, string title, string album, double duration,
                                int tracknum, string mbrainzid)
        {
            if (String.IsNullOrEmpty (artist) || String.IsNullOrEmpty (title) || !connected ||
                String.IsNullOrEmpty (LastfmCore.Account.SessionKey)) {
                return;
            }

            // FIXME: need a lock for this flag
            if (now_playing_started) {
                return;
            }

            now_playing_started = true;

            string str_track_number = String.Empty;
            if (tracknum != 0) {
                str_track_number = tracknum.ToString();
            }

            LastfmRequest request = new LastfmRequest ("track.updateNowPlaying", RequestType.Write, ResponseFormat.Json);
            request.AddParameter ("track", title);
            request.AddParameter ("artist", artist);
            request.AddParameter ("album", album);
            request.AddParameter ("trackNumber", str_track_number);
            request.AddParameter ("duration", Math.Floor (duration).ToString ());
            request.AddParameter ("mbid", mbrainzid);
            current_now_playing_request = request;

            NowPlaying (current_now_playing_request);
        }

        private void NowPlaying (LastfmRequest request)
        {
            try {
                request.BeginSend (OnNowPlayingResponse);
            }
            catch (Exception e) {
                Log.Warning ("Audioscrobbler NowPlaying failed",
                    String.Format("Failed to post NowPlaying: {0}", e.GetType ().Name), false);
                current_now_playing_request = null;
                now_playing_started = false;
            }

        }

        private void OnNowPlayingResponse (IAsyncResult ar)
        {
            try {
                current_now_playing_request.EndSend (ar);
                var error = current_now_playing_request.GetError ();
                if (error == StationError.None) {
                    Log.Debug ("Submitted NowPlaying track to Audioscrobbler");
                } else {
                    Log.WarningFormat ("Audioscrobbler NowPlaying failed: {0}", error);
                }
            } catch (Exception e) {
                Log.WarningFormat ("Failed to process the NowPlaying response: {0}", e.GetType ().Name);
            } finally {
                // A failed update must not block updates for subsequent tracks.
                current_now_playing_request = null;
                now_playing_started = false;
            }
        }
    }
}
