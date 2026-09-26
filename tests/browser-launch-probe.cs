// Run on a private session bus with a D-Bus-activatable fake snap launcher.
using System;
using System.IO;
using DBus;
using Banshee.Web;

public class FakeLauncher : ISnapUrlLauncher
{
    public void OpenURL (string url)
    {
        File.AppendAllText (Environment.GetEnvironmentVariable ("BANSHEE_BROWSER_RECORD"), url + "\n");
    }
}

class BrowserLaunchProbe
{
    static int Main (string[] args)
    {
        var bus = Bus.Session;
        if (args.Length > 0 && args[0] == "serve") {
            bus.Register (new ObjectPath ("/io/snapcraft/Launcher"), new FakeLauncher ());
            bus.RequestName ("io.snapcraft.Launcher");
            while (true) bus.Iterate ();
        }
        if (bus.NameHasOwner ("io.snapcraft.Launcher"))
            throw new Exception ("Fixture launcher must initially be stopped");
        string url = "https://www.last.fm/api/auth?api_key=fixture&token=fixture";
        for (int attempt = 0; attempt < 2; attempt++) {
            if (!Browser.Open (url, false))
                throw new Exception ("Browser launch failed: " + (attempt == 0 ? "cold" : "warm"));
            Console.WriteLine ("PASS browser launch: " + (attempt == 0 ? "cold activation" : "already running"));
        }
        var lines = File.ReadAllLines (Environment.GetEnvironmentVariable ("BANSHEE_BROWSER_RECORD"));
        if (lines.Length != 2 || lines[0] != url || lines[1] != url)
            throw new Exception ("Launcher must receive each complete URL exactly once");
        Console.WriteLine ("PASS complete authorization URL delivered exactly once per click");
        return 0;
    }
}
