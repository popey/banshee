// Explicit desktop smoke test: opens a harmless page, never an auth token.
using System;
class InstalledBrowserProbe
{
    static int Main ()
    {
        bool opened = Banshee.Web.Browser.Open ("https://www.last.fm/api", false);
        Console.WriteLine (opened ? "PASS installed snap browser handoff" : "FAIL installed snap browser handoff");
        return opened ? 0 : 1;
    }
}
