// Snap first-run integration. Copyright 2026 Banshee revival contributors.
// Licensed under the MIT license, as the surrounding Nereid source.
using System;
using System.IO;
using System.Text.RegularExpressions;
using Gtk;
using Mono.Unix;
using Banshee.Configuration;
using Banshee.Library;
using Banshee.ServiceStack;

namespace Nereid
{
    internal static class SnapMusicFolders
    {
        // Read data, never source user-dirs.dirs as shell code. XDG values are
        // absolute paths or $HOME-relative paths, independent of UI language.
        internal static string Parse (string text, string home)
        {
            if (String.IsNullOrEmpty (home) || !Path.IsPathRooted (home)) return null;
            foreach (string line in text.Split ('\n')) {
                var match = Regex.Match (line, @"^\s*XDG_MUSIC_DIR\s*=\s*""((?:\\.|[^""\\])*)""\s*(?:#.*)?$");
                if (!match.Success) continue;
                string value = match.Groups[1].Value;
                if (value.StartsWith ("$HOME/")) value = home.TrimEnd ('/') + value.Substring (5);
                else if (value == "$HOME") return null; // XDG marks this folder disabled.
                // Only decode shell quoting escapes; reject other substitutions.
                if (Regex.IsMatch (value, @"(?<!\\)[$`]") || Regex.IsMatch (value, @"\\[^\\""$`]")) return null;
                value = Regex.Replace (value, @"\\([\\""$`])", "$1");
                if (!Path.IsPathRooted (value)) return null;
                try {
                    value = Path.GetFullPath (value);
                    return value.TrimEnd ('/') == home.TrimEnd ('/') || value == "/" ? null : value;
                } catch (ArgumentException) { return null; }
            }
            return null;
        }

        internal static string Suggest ()
        {
            string home = Environment.GetEnvironmentVariable ("SNAP_REAL_HOME");
            if (String.IsNullOrEmpty (home)) return null;
            try {
                string file = Path.Combine (home, ".config/user-dirs.dirs");
                // No English-name fallback when the desktop has no Music setting.
                return File.Exists (file) ? Parse (File.ReadAllText (file), home) : null;
            } catch (IOException) { return null; }
              catch (UnauthorizedAccessException) { return null; }
        }
    }

    internal static class SnapMusicSetup
    {
        private static readonly SchemaEntry<bool> finished = new SchemaEntry<bool> (
            "snap", "music-setup-complete", false, null, null);

        internal static void Show (Gtk.Window parent)
        {
            if (String.IsNullOrEmpty (Environment.GetEnvironmentVariable ("SNAP")) || finished.Get ()) return;
            var library = ServiceManager.SourceManager.MusicLibrary;
            if (library == null) return;
            // Never interrupt an existing populated library on a snap refresh.
            if (library.Count > 0) { finished.Set (true); return; }

            string folder = SnapMusicFolders.Suggest ();
            var dialog = new Dialog (Catalog.GetString ("Add your music"), parent, DialogFlags.Modal);
            dialog.SetDefaultSize (560, -1);
            dialog.BorderWidth = 12;
            dialog.VBox.Spacing = 12;
            var explanation = new Label (Catalog.GetString (
                "Choose a folder to add to your music library.\nYour music files will stay where they are."));
            explanation.Xalign = 0;
            dialog.VBox.PackStart (explanation, false, false, 0);
            var location = new Label ();
            location.Xalign = 0;
            location.Selectable = true;
            location.LineWrap = true;
            dialog.VBox.PackStart (location, false, false, 0);
            dialog.AddButton (Catalog.GetString ("Not now"), ResponseType.Cancel);
            dialog.AddButton (Catalog.GetString ("Choose another folder…"), ResponseType.Apply);
            var import = dialog.AddButton (Catalog.GetString ("Add music"), ResponseType.Ok);
            dialog.DefaultResponse = ResponseType.Cancel;
            try {
                while (true) {
                    location.Text = folder ?? Catalog.GetString ("No music folder is configured. Choose a folder to get started.");
                    import.Sensitive = !String.IsNullOrEmpty (folder) && Directory.Exists (folder);
                    dialog.ShowAll ();
                    var response = (ResponseType)dialog.Run ();
                    if (response == ResponseType.Apply) {
                        var chooser = new Gtk.FileChooserDialog (Catalog.GetString ("Choose your music folder"),
                            dialog, FileChooserAction.SelectFolder, Stock.Cancel, ResponseType.Cancel,
                            Stock.Open, ResponseType.Ok);
                        chooser.LocalOnly = true;
                        string start = folder ?? Environment.GetEnvironmentVariable ("SNAP_REAL_HOME");
                        if (!String.IsNullOrEmpty (start) && Directory.Exists (start)) chooser.SetCurrentFolder (start);
                        try {
                            if ((ResponseType)chooser.Run () == ResponseType.Ok) folder = chooser.Filename;
                        } finally { chooser.Destroy (); }
                        continue;
                    }
                    if (response != ResponseType.Ok) { finished.Set (true); break; }
                    try {
                        // Check access without enumerating the collection or importing yet.
                        using (var entries = Directory.EnumerateFileSystemEntries (folder).GetEnumerator ()) entries.MoveNext ();
                    } catch (Exception e) {
                        if (!(e is IOException) && !(e is UnauthorizedAccessException)) throw;
                        using (var error = new MessageDialog (dialog, DialogFlags.Modal, MessageType.Error,
                            ButtonsType.Close, Catalog.GetString ("Banshee cannot read that folder."))) {
                            error.SecondaryText = Catalog.GetString (
                                "Check that the drive is mounted and you have permission to read it.\nFor an external drive, enable removable-media access for Banshee in your software settings, or run:\nsnap connect banshee:removable-media");
                            error.Run ();
                            error.Destroy ();
                        }
                        continue;
                    }
                    library.CreateSchema<string> ("library-location").Set (folder);
                    library.CreateSchema<bool> ("copy-on-import").Set (false);
                    library.CreateSchema<bool> ("move-on-info-save").Set (false);
                    ServiceManager.Get<LibraryImportManager> ().Enqueue (new string[] { new Uri (folder).AbsoluteUri });
                    finished.Set (true);
                    ServiceManager.SourceManager.SetActiveSource (library);
                    break;
                }
            } finally { dialog.Destroy (); }
        }
    }
}
