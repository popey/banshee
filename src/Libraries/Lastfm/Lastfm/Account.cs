//
// Account.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
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
using System.Collections;
using System.Net;
using System.Text;

using Hyena;

namespace Lastfm
{
    public class Account
    {
        public event EventHandler Updated;

        // Only used during the authentication process
        private string authentication_token;

        public bool HasPendingAuthorization {
            get { return !String.IsNullOrEmpty (authentication_token); }
        }

        private string username;
        public string UserName {
            get { return username; }
            set { username = value; }
        }

        private string session_key;
        public string SessionKey {
            get { return session_key; }
            set { session_key = value; }
        }

        private bool subscriber;
        public bool Subscriber {
            get { return subscriber; }
            set { subscriber = value; }
        }

        private string scrobble_url;
        public string ScrobbleUrl {
            get { return scrobble_url; }
            set { scrobble_url = value; }
        }

        public string SignUpUrl {
            get { return "https://www.last.fm/join"; }
        }

        public void SignUp ()
        {
            Browser.Open (SignUpUrl);
        }

        public void VisitUserProfile (string username)
        {
            Browser.Open (String.Format ("https://www.last.fm/user/{0}", Uri.EscapeDataString (username)));
        }

        public string HomePageUrl {
            get { return "https://www.last.fm/"; }
        }

        public void VisitHomePage ()
        {
            Browser.Open (HomePageUrl);
        }

        public virtual void Save ()
        {
            OnUpdated ();
        }

        // Isolate transport so authentication transitions can be checked without
        // live credentials or browser authorization.
        protected virtual Hyena.Json.JsonObject SendAuthorizationRequest (LastfmRequest request)
        {
            request.Send ();
            return request.GetResponseObject ();
        }

        public StationError RequestAuthorization ()
        {
            // A retry must never reuse an earlier browser authorization token.
            authentication_token = null;
            try {
                LastfmRequest get_token = new LastfmRequest ("auth.getToken", RequestType.Read, ResponseFormat.Json);
                var response = SendAuthorizationRequest (get_token);
                object error_code;
                if (response.TryGetValue ("error", out error_code)) {
                    Log.WarningFormat ("Lastfm error {0} : {1}", (int)error_code, (string)response["message"]);
                    return (StationError) Convert.ToInt32 (error_code);
                }

                var token = response.ContainsKey ("token") ? response["token"] as string : null;
                if (String.IsNullOrEmpty (token)) {
                    return StationError.InvalidResponse;
                }

                // Browser handlers may either return false or throw. Neither is success.
                try {
                    if (!Browser.Open (String.Format ("https://www.last.fm/api/auth?api_key={0}&token={1}",
                        LastfmCore.ApiKey, Uri.EscapeDataString (token)))) {
                        return StationError.BrowserLaunchFailed;
                    }
                } catch (Exception e) {
                    Log.WarningFormat ("Last.fm browser launch failed ({0})", e.GetType ().Name);
                    return StationError.BrowserLaunchFailed;
                }
                authentication_token = token;
                return StationError.None;
            } catch (WebException e) {
                Log.WarningFormat ("Last.fm authorization request failed ({0})", e.Status);
                return StationError.NetworkError;
            } catch (Exception e) {
                // Exception messages can contain authorization URLs or response data.
                Log.WarningFormat ("Last.fm authorization response failed ({0})", e.GetType ().Name);
                return StationError.InvalidResponse;
            }
        }

        public StationError FetchSessionKey ()
        {
            if (authentication_token == null) {
                return StationError.TokenNotAuthorized;
            }

            try {
                LastfmRequest get_session = new LastfmRequest ("auth.getSession", RequestType.SessionRequest, ResponseFormat.Json);
                get_session.AddParameter ("token", authentication_token);
                var response = SendAuthorizationRequest (get_session);
                object error_code;
                if (response.TryGetValue ("error", out error_code)) {
                    Log.WarningFormat ("Lastfm error {0} : {1}", (int)error_code, (string)response["message"]);
                    return (StationError) Convert.ToInt32 (error_code);
                }

                var session = (Hyena.Json.JsonObject)response["session"];
                var name = session.ContainsKey ("name") ? session["name"] as string : null;
                var key = session.ContainsKey ("key") ? session["key"] as string : null;
                if (String.IsNullOrEmpty (name) || String.IsNullOrEmpty (key)) {
                    return StationError.InvalidResponse;
                }
                UserName = name;
                SessionKey = key;
                Subscriber = session.ContainsKey ("subscriber") && session["subscriber"].ToString ().Equals ("1");

                // The authentication token is only valid once, and for a limited time
                authentication_token = null;

                return StationError.None;
            } catch (WebException e) {
                Log.WarningFormat ("Last.fm session request failed ({0})", e.Status);
                return StationError.NetworkError;
            } catch (Exception e) {
                Log.WarningFormat ("Last.fm session response failed ({0})", e.GetType ().Name);
                return StationError.InvalidResponse;
            }
        }

        protected void OnUpdated ()
        {
            EventHandler handler = Updated;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
    }
}

