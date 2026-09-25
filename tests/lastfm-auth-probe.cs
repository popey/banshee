// Exercise actual Account transitions with deterministic transport/browser failures.
using System;
using System.Net;
using Lastfm;
using Hyena.Json;
class AuthProbe {
    class FixtureAccount : Account {
        public string Response = "{\"token\":\"fixture-token\"}";
        public Exception Failure;
        public int Calls;
        protected override JsonObject SendAuthorizationRequest(LastfmRequest request) {
            Calls++;
            if (Failure != null) throw Failure;
            return (JsonObject)new Deserializer(Response).Deserialize();
        }
    }
    static int passed;
    static void Check(bool ok,string name) { if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);passed++; }
    static int Main() {
        var original=Browser.Open;
        try {
            var a=new FixtureAccount();int browsers=0;
            Browser.Open=delegate(string url){browsers++;return false;};
            Check(a.RequestAuthorization()==StationError.BrowserLaunchFailed && !a.HasPendingAuthorization,"browser false is failure, not pending");
            int calls=a.Calls;
            Check(a.FetchSessionKey()==StationError.TokenNotAuthorized && a.Calls==calls,"failed launch cannot finish or send a stale token");
            Browser.Open=delegate(string url){throw new Exception("fixture-secret-url");};
            Check(a.RequestAuthorization()==StationError.BrowserLaunchFailed && !a.HasPendingAuthorization,"throwing browser is a safe launch failure");
            Browser.Open=delegate(string url){browsers++;return true;};
            Check(a.RequestAuthorization()==StationError.None && a.HasPendingAuthorization,"retry can open browser and enter pending state");
            a.Failure=new WebException("fixture-secret-url",WebExceptionStatus.Timeout);
            Check(a.RequestAuthorization()==StationError.NetworkError && !a.HasPendingAuthorization,"network failure clears previous pending token");
            a.Failure=null;a.Response="{\"error\":11,\"message\":\"offline\"}";int before=browsers;
            Check(a.RequestAuthorization()==StationError.ServiceOffline && browsers==before && !a.HasPendingAuthorization,"API error neither opens browser nor leaves pending state");
            foreach(var response in new[]{"{}","{\"token\":\"\"}","{\"token\":42}","null"}) {
                a.Response=response;
                Check(a.RequestAuthorization()==StationError.InvalidResponse && !a.HasPendingAuthorization,"invalid token response: "+response);
            }
            a.Response="{\"token\":\"fixture-token\"}";a.RequestAuthorization();
            a.Response="{\"error\":14,\"message\":\"not authorized\"}";
            Check(a.FetchSessionKey()==StationError.TokenNotAuthorized && a.HasPendingAuthorization,"pending browser approval can be checked again");
            a.Response="{\"session\":{\"name\":\"CanonicalUser\",\"key\":\"fixture-session\",\"subscriber\":0}}";
            Check(a.FetchSessionKey()==StationError.None && a.UserName=="CanonicalUser" && a.SessionKey=="fixture-session" && !a.HasPendingAuthorization,"session returns canonical identity and consumes token");
            a=new FixtureAccount();a.RequestAuthorization();a.Response="{\"session\":{\"name\":\"CanonicalUser\"}}";
            Check(a.FetchSessionKey()==StationError.InvalidResponse && a.SessionKey==null && a.UserName==null,"incomplete session never partially signs in");
            foreach(var error in new[]{StationError.BrowserLaunchFailed,StationError.NetworkError,StationError.InvalidResponse})
                Check(!String.IsNullOrEmpty(RadioConnection.ErrorMessageFor(error)),"actionable message for "+error);
            Console.WriteLine(passed+" authentication checks passed");return 0;
        } finally {Browser.Open=original;}
    }
}
