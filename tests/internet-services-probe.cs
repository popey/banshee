// Compile against the built Lastfm and InternetArchive assemblies; no user profile required.
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Xml;
using System.Collections.Generic;
using InternetArchive;
using Lastfm;
using Lastfm.Data;
class ServicesProbe {
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static int passed;
    static void Check(bool value, string name) { if (!value) throw new Exception(name); Console.WriteLine("PASS " + name); passed++; }
    static void Set(object obj,string field,object value) { obj.GetType().GetField(field,Hidden).SetValue(obj,value); }
    static object Call(object obj,string name,params object[] args) { return obj.GetType().GetMethod(name,Hidden).Invoke(obj,args); }
    static LastfmRequest Response(string text) {
        var req=new LastfmRequest("test");
        Set(req,"response_stream",new MemoryStream(Encoding.UTF8.GetBytes(text)));
        return req;
    }
    class Queue : IQueue {
        public event EventHandler TrackAdded;
        public int Count { get; private set; }
        public Queue() { Count=3; }
        public void Save() {} public void Load() {}
        public List<IQueuedTrack> GetTracks() { return new List<IQueuedTrack>(); }
        public void Add(object t,DateTime d) {} public void RemoveInvalidTracks() { throw new Exception("Unexpected queue deletion"); }
        public void RemoveRange(int first,int count) { Count-=count; }
    }
    static void QueueCase(string json,bool remove,string label) {
        var queue=new Queue();
        var connection=Activator.CreateInstance(typeof(AudioscrobblerConnection),Hidden,null,new object[]{queue},null);
        var request=Response(json);
        SendRequestHandler handler=delegate {};
        Set(request,"send_handler",handler);
        Set(connection,"current_scrobble_request",request);
        var result=handler.BeginInvoke(null,1);
        Call(connection,"OnScrobbleResponse",result);
        Check(queue.Count==(remove?2:3),label);
    }
    static void Fixtures() {
        var numbers=(Hyena.Json.JsonArray)new Hyena.Json.Deserializer("[-9223372036854775808,9223372036854775807,1,1.5e2]").Deserialize();
        Check((long)numbers[0]==Int64.MinValue && (long)numbers[1]==Int64.MaxValue && numbers[2] is int && (double)numbers[3]==150,"JSON Int64 bounds, Int32 compatibility and exponent");
        var details=new Details("fixture",@"{""metadata"":{""creator"":""Artist"",""description"":[""one"",""two""]},""files"":[{""name"":""dir/song + #?.mp3"",""format"":""VBR MP3"",""size"":2147483648,""length"":""3:45.5"",""track"":""2/12""},{""name"":""secret.mp3"",""private"":true}],""reviews"":[{""stars"":""4""},{""stars"":2}]}");
        Check(details.Creator=="Artist" && details.Description=="one"+Environment.NewLine+"two","Archive scalar and list metadata");
        var file=details.Files.Single();
        Console.WriteLine("Archive file: size={0} track={1} seconds={2}",file.Size,file.Track,file.Length.TotalSeconds);
        Check(file.Size==2147483648L && file.Track==2 && file.Length.TotalSeconds==225.5,"Archive numeric metadata and fractional duration");
        Check(file.Location=="https://archive.org/download/fixture/dir/song%20%2B%20%23%3F.mp3","Archive escaped filenames and private-file exclusion");
        Check(details.NumReviews==2 && details.AvgRating==3 && !details.HasDownloadStats,"Archive modern reviews and absent statistics");
        details=new Details("legacy",@"{""metadata"":{""creator"":[""Artist""]},""files"":{""/song.mp3"":{""size"":""123"",""length"":""120.25""}},""reviews"":{""reviews"":[]}}");
        Check(details.Files.Single().Length.TotalSeconds==120.25 && details.Creator=="Artist","Archive legacy cached metadata");
        foreach(var bad in new[]{"[]","{}","{\"error\":\"missing\"}"}) {
            bool rejected=false; try { new Details("bad",bad); } catch(InvalidDataException) { rejected=true; }
            Check(rejected,"Archive rejects invalid item "+bad);
        }
        bool error=false; try { new SearchResults("{\"error\":\"unsupported\"}"); } catch(InvalidDataException) { error=true; }
        Check(error,"Archive search error is not empty success");
        Check(new SearchResults("{\"response\":{\"numFound\":0,\"start\":0,\"docs\":[]}}").TotalResults==0,"Archive empty search succeeds");
        Check((int)Response("{\"error\" : 9, \"message\":\"Invalid session\"}").GetError()==9,"Last.fm JSON error whitespace");
        var xml=Response("<lfm status=\"failed\"><error code=\"9\">Invalid</error></lfm>"); xml.ResponseFormat=ResponseFormat.Raw;
        Check((int)xml.GetError()==9,"Last.fm XML error code");
        var secret=new LastfmRequest("test");secret.AddParameter("sk","TEST-SESSION");secret.AddParameter("token","TEST-TOKEN");
        Check(!secret.ToString().Contains("TEST-"),"Last.fm diagnostic credential redaction");
        var doc=new XmlDocument();doc.LoadXml("<lfm status='ok'><lovedtracks><track><name>Song</name><artist><name>Artist</name><url>https://example.com</url></artist></track></lovedtracks></lfm>");
        var entries=new DataEntryCollection<RecentTrack>(doc);
        Check(entries.Count==1 && entries[0].Artist=="Artist","Last.fm API 2 nested XML entries");
        QueueCase("{\"error\":9,\"message\":\"Invalid session\"}",false,"Last.fm retains queue on invalid session");
        QueueCase("{\"unexpected\":true}",false,"Last.fm retains queue on malformed success");
        QueueCase("{\"scrobbles\":{\"@attr\":{\"accepted\":\"1\",\"ignored\":\"0\"},\"scrobble\":{\"ignoredMessage\":{\"code\":\"0\"}}}}",true,"Last.fm removes acknowledged scrobble only");
        QueueCase("{\"scrobbles\":{\"@attr\":{\"accepted\":\"0\",\"ignored\":\"0\"},\"scrobble\":{\"ignoredMessage\":{\"code\":\"0\"}}}}",false,"Last.fm retains queue on wrong acknowledgement count");
        var np=Activator.CreateInstance(typeof(AudioscrobblerConnection),Hidden,null,new object[]{new Queue()},null);
        var npRequest=Response("{\"error\":9}"); SendRequestHandler npHandler=delegate {};
        Set(npRequest,"send_handler",npHandler);Set(np,"current_now_playing_request",npRequest);Set(np,"now_playing_started",true);
        Call(np,"OnNowPlayingResponse",npHandler.BeginInvoke(null,null));
        Check(!(bool)np.GetType().GetField("now_playing_started",Hidden).GetValue(np),"Last.fm failed now-playing releases next track");
        var lifecycle=(AudioscrobblerConnection)Activator.CreateInstance(typeof(AudioscrobblerConnection),Hidden,null,new object[]{new Queue()},null);
        lifecycle.UpdateNetworkState(false);
        LastfmCore.Account.SessionKey=null;
        lifecycle.Start();
        lifecycle.NowPlaying("Test artist","Test track","Test album",120,1);
        Check(!lifecycle.Started && !(bool)lifecycle.GetType().GetField("now_playing_started",Hidden).GetValue(lifecycle),"Last.fm signed-out account starts no submissions");
        LastfmCore.Account.SessionKey="TEST-SESSION";
        lifecycle.Start();
        Check(lifecycle.Started,"Last.fm authenticated queue can start while offline");
        lifecycle.Stop();LastfmCore.Account.SessionKey=null;
        using(var listener=new HttpListener()) {
            listener.Prefixes.Add("http://127.0.0.1:18963/");listener.Start();
            string path=null,body=null,method=null;
            var thread=new Thread(delegate() { var ctx=listener.GetContext();path=ctx.Request.RawUrl;method=ctx.Request.HttpMethod;using(var reader=new StreamReader(ctx.Request.InputStream)) body=reader.ReadToEnd();ctx.Response.StatusCode=400;var bytes=Encoding.UTF8.GetBytes("{\"error\":9}");ctx.Response.OutputStream.Write(bytes,0,bytes.Length);ctx.Response.Close(); });thread.Start();
            using(var response=(Stream)Call(secret,"Post","http://127.0.0.1:18963/","method=test&sk=TEST-SESSION&track=Bj%C3%B6rk")) { Check(response!=null,"Last.fm preserves HTTP error response body"); }
            thread.Join();
            Check(path=="/" && method=="POST" && body.Contains("sk=TEST-SESSION") && body.Contains("Bj%C3%B6rk"),"Last.fm POST parameters stay in form body");
        }
    }
    static void Live() {
        var search=new Search { Query="identifier:alice_dugdale_2006_librivox",NumResults=3 };
        var results=search.GetResults();Check(results.TotalResults>0,"Live Archive search");
        var details=new Details("alice_dugdale_2006_librivox");Check(details.Files.Any(f=>f.Format!=null && f.Format.Contains("MP3")),"Live Archive metadata and audio files");
        DataCore.CachePath=Path.Combine(Path.GetTempPath(),"banshee-lastfm-probe-cache");
        DataCore.UserAgent="Banshee/2.6.2 service-validation";
        var artist=new LastfmArtistData("Erasure");Check(artist.SimilarArtists.Count>0 && artist.TopAlbums.Count>0 && artist.TopTracks.Count>0,"Live Last.fm recommendations");
        var user=new LastfmUserData("rj");
        Check(user.RecentTracks.Count>0 && user.RecentLovedTracks.Count>0 && user.GetTopArtists(TopType.Overall).Count>0,"Live Last.fm public user panels");
        var request=new LastfmRequest("auth.getToken");request.Send();var token=request.GetResponseObject();Check(token!=null && token.ContainsKey("token"),"Live Last.fm authorization token");
        var session=new LastfmRequest("auth.getSession",RequestType.SessionRequest,ResponseFormat.Json);session.AddParameter("token",(string)token["token"]);session.Send();
        Check((int)session.GetError()==14,"Live Last.fm signature accepted, browser authorization pending");
    }
    static int Main(string[] args) {
        try { if(args.Length>0 && args[0]=="live") Live();else Fixtures();Console.WriteLine(passed+" checks passed");return 0; }
        catch(Exception e) { Console.Error.WriteLine(e.GetType().Name+": "+e.Message); return 1; }
    }
}
