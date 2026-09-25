// Run against the built/installed assemblies with an isolated XDG cache.
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using Banshee.Base;
using Banshee.Collection;
using Banshee.Metadata;
using Banshee.ServiceStack;
using Banshee.Networking;
using Banshee.InternetArchive;
using Lastfm.Data;
class ArtworkProbe {
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static int passed;
    static string root;
    static readonly byte[] png=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jL1kAAAAASUVORK5CYII=");
    static void Check(bool ok,string name) { if(!ok) throw new Exception(name); Console.WriteLine("PASS "+name);passed++; }
    class Job : MetadataServiceJob {
        public bool Download(string url,string id) { return SaveHttpStreamCover(new Uri(url),id,null); }
    }
    static bool Save(byte[] data,long length,string mime,string path) {
        return (bool)typeof(MetadataServiceJob).GetMethod("SaveCoverResponse",Private).Invoke(new Job(),new object[]{path,new MemoryStream(data),length,mime});
    }
    static TrackInfo Track(string artist,string album) { return new TrackInfo {ArtistName=artist,AlbumTitle=album,TrackTitle="Artwork validation",Uri=new Hyena.SafeUri("file:///tmp/artwork-validation.mp3")}; }
    static void Fixtures() {
        var method=typeof(LastfmData<AlbumData>).GetMethod("BuildDataUrl",BindingFlags.NonPublic|BindingFlags.Static);
        var url=(string)method.Invoke(null,new object[]{"album/"+Uri.EscapeDataString("Björk / A+B")+"/"+Uri.EscapeDataString("Debut & live?")+"/info.xml"});
        Check(url.Contains("method=album.getInfo") && url.Contains("artist=Bj%C3%B6rk%20%2F%20A%2BB") && url.Contains("album=Debut%20%26%20live%3F") && !url.Contains("%25"),"Last.fm album names are encoded once, including Unicode and slashes");
        var xml=new XmlDocument();xml.LoadXml("<album><image size='mega'>https://example.org/large.png</image><image size='small'>https://example.org/small.jpg</image></album>");
        var images=new AlbumCoverUrls {Root=xml.DocumentElement};
        Check(images.Small.EndsWith("small.jpg") && images.AllUrls().Last().EndsWith("large.png"),"Last.fm API 2 images ordered by size, not response order");
        DataCore.CachePath=Path.Combine(root,"fixture-lastfm");DataCore.UserAgent="Banshee artwork fixture";
        typeof(DataCore).GetMethod("Initialize",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        var albumUrl=(string)method.Invoke(null,new object[]{"album/Fixture/Album/info.xml"});
        var cached=(string)typeof(DataCore).GetMethod("GetCachedPathFromUrl",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).Invoke(null,new object[]{albumUrl});
        File.WriteAllText(cached,"<lfm status='ok'><album><name>Album</name><artist>Fixture</artist><image size='large'>https://example.org/cover.jpg</image></album></lfm>");
        var album=new LastfmAlbumData("Fixture","Album");
        Check(album.AlbumData!=null && album.AlbumCoverUrls.Large.EndsWith("cover.jpg"),"Album metadata and image objects have separate typed cache entries");
        string path=Path.Combine(root,"response.jpg");File.Delete(path);
        Check(!Save(Encoding.UTF8.GetBytes("<html>bad gateway</html>"),-1,"text/html",path) && !File.Exists(path),"HTML errors leave no artwork cache entry");
        Check(!Save(new byte[0],0,"image/jpeg",path) && !File.Exists(path),"Empty images rejected");
        Check(!Save(png,png.Length+1,"image/png",path) && !File.Exists(path),"Truncated HTTP responses rejected");
        Check(!Save(png,9*1024*1024,"image/png",path) && !File.Exists(path),"Oversized declared responses rejected");
        Check(!Save(new byte[8*1024*1024+1],-1,"image/png",path) && !File.Exists(path),"Oversized chunked responses rejected");
        Check(!Save(png.Take(png.Length-12).ToArray(),-1,"image/png",path) && !File.Exists(path),"Incomplete PNG rejected");
        Check(Save(png,png.Length,"image/png",path) && File.ReadAllBytes(path).SequenceEqual(png),"Valid image saved after failures");
        string gifPath=Path.Combine(root,"gif.jpg");File.Delete(gifPath);
        byte[] gif=Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///ywAAAAAAQABAAACAUwAOw==");
        Check(Save(gif,gif.Length,"image/gif",gifPath),"GIF covers remain supported for existing podcast feeds");
        Check(Save(new byte[]{255,216,255,217},4,"image/jpeg",path) && File.ReadAllBytes(path).SequenceEqual(png),"Existing cover preserved");
        string blocker=Path.Combine(root,"blocker");File.WriteAllText(blocker,"not a directory");
        bool rejected=false;try {Save(png,png.Length,"image/png",Path.Combine(blocker,"bad.jpg"));} catch(TargetInvocationException) {rejected=true;}
        Check(rejected,"Filesystem failure cannot report a successful save");
        var track=Track("Artwork probe","Missing # album");File.Delete(CoverArtSpec.GetPath(track.ArtworkId));
        Check(!new Banshee.Mpris.Metadata(track).DataStore.ContainsKey("mpris:artUrl"),"MPRIS omits missing artwork");
        File.WriteAllBytes(CoverArtSpec.GetPath(track.ArtworkId),png);
        var art=(string)new Banshee.Mpris.Metadata(track).DataStore["mpris:artUrl"];
        Check(new Uri(art).IsFile && File.Exists(new Uri(art).LocalPath) && art.Contains("%23"),"MPRIS emits escaped local URL to an existing image");
        Check(!new Banshee.Mpris.Metadata(Track(null,null)).DataStore.ContainsKey("mpris:artUrl"),"MPRIS omits artwork for unknown albums");
        var archive=new DetailsSource.ArchiveTrackInfo {ItemId="item/with ? punctuation",ArtistName="Artist",AlbumTitle="Album",Uri=new Hyena.SafeUri("https://archive.org/download/item/track.mp3")};
        Check(archive.ArtworkId.StartsWith("internet-archive-") && !archive.ArtworkId.Contains("/") && archive.ArtworkId!=Track("Artist","Album").ArtworkId,"Archive artwork has safe item-specific cache identity");
        var providers=MetadataService.Instance.Providers.Select(p=>p.GetType().Name).ToArray();
        Check(providers.SequenceEqual(new[]{"EmbeddedMetadataProvider","FileSystemMetadataProvider","MusicBrainzMetadataProvider","LastFMMetadataProvider"}),"Local artwork precedes current remote providers; Rhapsody retired");
    }
    static void Live() {
        // Network with no manager represents upstream's connected fallback; avoid the user's DB/settings.
        var network=(Network)FormatterServices.GetUninitializedObject(typeof(Network));
        ServiceManager.RegisterService(network);
        DataCore.CachePath=Path.Combine(root,"lastfm");DataCore.UserAgent="Banshee/2.6.2 artwork validation";
        var track=Track("Massive Attack","Heligoland");
        string path=CoverArtSpec.GetPath(track.ArtworkId);File.Delete(path);
        var job=new Banshee.Metadata.LastFM.LastFMQueryJob(track);job.Run();
        Check(File.Exists(path) && new FileInfo(path).Length>1024 && job.ResultTags.Count==1,"Live automatic Last.fm album cover downloaded and announced");
        track=Track("Big Daddy Kane","Collabos And Rarities (1998-2005)");track.AlbumMusicBrainzId="32234198-4da7-47a1-add3-3bc51ec88dd8";
        File.Delete(CoverArtSpec.GetPath(track.ArtworkId));
        var mb=new Banshee.Metadata.MusicBrainz.MusicBrainzQueryJob(track);
        Check(mb.Lookup() && File.Exists(CoverArtSpec.GetPath(track.ArtworkId)),"Live tagged MusicBrainz release uses Cover Art Archive");
        const string id="alice_dugdale_2006_librivox";
        path=CoverArtSpec.GetPath(DetailsSource.ArtworkIdForItem(id));File.Delete(path);
        new DetailsSource.ArtworkJob(id,"https://archive.org/services/img/"+id).Run();
        var ia=new DetailsSource.ArchiveTrackInfo {ItemId=id,ArtistName="Anthony Trollope",AlbumTitle="Alice Dugdale",Uri=new Hyena.SafeUri("https://archive.org/download/"+id+"/test.mp3")};
        Check(File.Exists(path) && new FileInfo(path).Length>1024 && new Banshee.Mpris.Metadata(ia).DataStore.ContainsKey("mpris:artUrl"),"Live Archive item cover cached and exposed locally by MPRIS");
        var missing=Track("Banshee nonexistent artwork fixture 682917","No such album 682917");
        new Banshee.Metadata.LastFM.LastFMQueryJob(missing).Run();
        Check(!CoverArtSpec.CoverExists(missing.ArtworkId),"Live absent album does not cache a placeholder or error");
    }
    static int Main(string[] args) {
        try {
            root=Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
            if(String.IsNullOrEmpty(root) || !root.Contains("artwork-probe")) throw new Exception("Set an isolated artwork-probe cache");
            Directory.CreateDirectory(root);Directory.CreateDirectory(CoverArtSpec.RootPath);
            Hyena.Paths.ApplicationName="artwork-probe";
            Mono.Addins.AddinManager.Initialize(Path.Combine(root,"addins"));
            if(args.Length>0 && args[0]=="live") Live();else Fixtures();
            Console.WriteLine(passed+" artwork checks passed");return 0;
        } catch(Exception e) { Console.Error.WriteLine(e.ToString());return 1; }
    }
}
