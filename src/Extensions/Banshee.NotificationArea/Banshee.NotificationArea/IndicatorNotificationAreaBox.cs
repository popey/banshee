// Banshee preservation packaging: adapt the existing GTK menu to Ayatana.
// SPDX-License-Identifier: MIT
using System;
using System.Runtime.InteropServices;
using Gtk;
using Banshee.MediaEngine;

namespace Banshee.NotificationArea
{
    public sealed class IndicatorNotificationAreaBox : INotificationAreaBox
    {
        private IntPtr indicator;
        // The native indicator owns its exported menu. Keep the managed wrapper
        // alive as well; the service retains the action handlers.
        private Menu menu;

        [DllImport ("libayatana-appindicator.so.1")]
        private static extern IntPtr app_indicator_new (string id, string icon, int category);
        [DllImport ("libayatana-appindicator.so.1")]
        private static extern void app_indicator_set_menu (IntPtr indicator, IntPtr menu);
        [DllImport ("libayatana-appindicator.so.1")]
        private static extern void app_indicator_set_status (IntPtr indicator, int status);
        [DllImport ("libayatana-appindicator.so.1")]
        private static extern void app_indicator_set_title (IntPtr indicator, string title);
        [DllImport ("libgobject-2.0.so.0")]
        private static extern void g_object_unref (IntPtr obj);

        public IndicatorNotificationAreaBox (Menu menu)
        {
            this.menu = menu;
            string snap = Environment.GetEnvironmentVariable ("SNAP");
            string icon = String.IsNullOrEmpty (snap) ? "media-player-banshee"
                : System.IO.Path.Combine (snap, "usr/share/icons/hicolor/48x48/apps/media-player-banshee.png");
            indicator = app_indicator_new ("banshee", icon, 0);
            if (indicator == IntPtr.Zero) throw new InvalidOperationException ("Could not create indicator");
            app_indicator_set_title (indicator, "Banshee");
            app_indicator_set_menu (indicator, menu.Handle);
        }

        // Ayatana handles activation and popup positioning through the menu.
        public event EventHandler Disconnected { add {} remove {} }
        public event EventHandler Activated { add {} remove {} }
        public event PopupMenuHandler PopupMenuEvent { add {} remove {} }
        public Widget Widget { get { return null; } }
        public void PositionMenu (Menu menu, out int x, out int y, out bool push_in)
        { x = y = 0; push_in = true; }
        public void OnPlayerEvent (PlayerEventArgs args) {}
        public void Show () { if (indicator != IntPtr.Zero) app_indicator_set_status (indicator, 1); }
        public void Hide () { if (indicator != IntPtr.Zero) app_indicator_set_status (indicator, 0); }
        public void Dispose ()
        {
            if (indicator == IntPtr.Zero) return;
            Hide ();
            g_object_unref (indicator);
            indicator = IntPtr.Zero;
            GC.KeepAlive (menu);
            menu = null;
        }
    }
}
