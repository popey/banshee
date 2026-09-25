// Runs the real preferences handler/widgets with fake transport and in-memory settings.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Banshee.Configuration;
using Banshee.Preferences;
using Lastfm;
class LoginUiProbe {
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    class FixtureAccount : Account {
        public bool Fail;
        protected override Hyena.Json.JsonObject SendAuthorizationRequest(LastfmRequest request) {
            if(Fail)throw new System.Net.WebException("fixture",System.Net.WebExceptionStatus.Timeout);
            return (Hyena.Json.JsonObject)new Hyena.Json.Deserializer("{\"token\":\"fixture\"}").Deserialize();
        }
    }
    static void Set(object o,string name,object value){o.GetType().GetField(name,Hidden).SetValue(o,value);}
    static IEnumerable<string> Labels(Gtk.Widget w){
        var b=w as Gtk.Button;if(b!=null)yield return b.Label;
        var l=w as Gtk.Label;if(l!=null)yield return l.Text;
        if(w.GetType().FullName=="Hyena.Widgets.WrapLabel")
            yield return (string)w.GetType().GetProperty("Text").GetValue(w,null);
        var c=w as Gtk.Container;if(c!=null)foreach(var child in c.Children)foreach(var text in Labels(child))yield return text;
    }
    static void Main(string[] args){
        Gtk.Application.Init();
        typeof(ConfigurationClient).GetField("client",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,new MemoryConfigurationClient());
        var assembly=Assembly.LoadFrom(args[0]);
        var source=FormatterServices.GetUninitializedObject(assembly.GetType("Banshee.Lastfm.LastfmSource"));
        var account=new FixtureAccount();Set(source,"account",account);
        var prefs=FormatterServices.GetUninitializedObject(assembly.GetType("Banshee.Lastfm.LastfmPreferences"));
        Set(prefs,"source",source);
        Set(prefs,"username_preference",new SchemaPreference<string>(new SchemaEntry<string>("test","username","typed-name","",""),"Username"));
        Set(prefs,"signup_button",new Gtk.LinkButton("https://example.org"));
        Set(prefs,"profile_page_button",new Gtk.LinkButton("https://example.org"));
        var box=new Gtk.Table(0,0,false);Set(prefs,"sign_in_box",box);
        var handler=prefs.GetType().GetMethod("OnSignInClicked",Hidden);
        foreach(var mode in new[]{"browser","network","success"}){
            account.Fail=mode=="network";
            Lastfm.Browser.Open=delegate(string url){return mode!="browser";};
            handler.Invoke(prefs,new object[]{null,EventArgs.Empty});
            var texts=new List<string>(Labels(box));
            bool finish=texts.Contains("Finish Logging In");
            if(finish!=(mode=="success"))throw new Exception("Wrong pending controls: "+mode);
            string all=String.Join(" ",texts.ToArray());
            if(mode=="browser"&&!all.Contains("default browser"))throw new Exception("Missing browser error");
            if(mode=="network"&&!all.Contains("network connection"))throw new Exception("Missing network error");
            Console.WriteLine("PASS real login widgets: "+mode);
        }
        box.Destroy();
    }
}
