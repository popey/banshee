using System;
using DBus;
class DBusProbe {
    static int Main() {
        try { Console.WriteLine("Session bus connected: " + Bus.Session.NameHasOwner("org.freedesktop.DBus")); return 0; }
        catch(Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
