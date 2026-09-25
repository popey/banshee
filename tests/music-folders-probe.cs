// Run against the built Nereid.exe; no GTK display or user profile required.
using System;
using System.Reflection;
class MusicFoldersProbe
{
    static int Main (string[] args)
    {
        var assembly = Assembly.LoadFrom (args[0]);
        var parse = assembly.GetType ("Nereid.SnapMusicFolders", true).GetMethod (
            "Parse", BindingFlags.Static | BindingFlags.NonPublic);
        string[,] cases = {
            { "XDG_MUSIC_DIR=\"$HOME/Musique\"", "/home/test/Musique" },
            { "# comment\nXDG_MUSIC_DIR=\"$HOME/音楽 # albums\"\n", "/home/test/音楽 # albums" },
            { "XDG_MUSIC_DIR=\"/media/music/My collection\" # USB", "/media/music/My collection" },
            { "XDG_MUSIC_DIR=\"$HOME\"", null },
            { "XDG_MUSIC_DIR=\"/home/test/\"", null },
            { "XDG_MUSIC_DIR=\"/\"", null },
            { "XDG_MUSIC_DIR=\"relative\"", null },
            { "XDG_MUSIC_DIR=\"$OTHER/music\"", null },
            { "XDG_MUSIC_DIR=\"$(touch /tmp/should-not-exist)\"", null },
            { "XDG_MUSIC_DIR=\"/tmp/`command`\"", null },
            { "XDG_MUSIC_DIR=\"$HOME/Music\"; command", null },
            { "XDG_MUSIC_DIR=\"$HOME/Cash \\$ and \\\"quotes\\\"\"", "/home/test/Cash $ and \"quotes\"" },
            { "XDG_DOWNLOAD_DIR=\"$HOME/Downloads\"", null }
        };
        for (int i = 0; i < cases.GetLength (0); i++) {
            string actual = (string)parse.Invoke (null, new object[] { cases[i,0], "/home/test" });
            if (actual != cases[i,1]) throw new Exception ("Case " + i + ": " + actual);
        }
        if (parse.Invoke (null, new object[] { cases[0,0], null }) != null) throw new Exception ("Missing home");
        Console.WriteLine ("PASS: 14 XDG music-folder cases");
        return 0;
    }
}
