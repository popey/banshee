#!/usr/bin/python3
"""Exercise the running snap on a selected, seekable library track.
Changes playback position; restores volume/repeat/shuffle and leaves paused.
Requires host python3-dbus and python3-gi, and the desktop session bus.
"""
import dbus, time
from dbus.mainloop.glib import DBusGMainLoop
from gi.repository import GLib
DBusGMainLoop(set_as_default=True)
bus=dbus.SessionBus(); obj=bus.get_object('org.mpris.MediaPlayer2.banshee','/org/mpris/MediaPlayer2')
p=dbus.Interface(obj,'org.mpris.MediaPlayer2.Player'); props=dbus.Interface(obj,'org.freedesktop.DBus.Properties')
iface='org.mpris.MediaPlayer2.Player'; events=[]
bus.add_signal_receiver(lambda *a:events.append(a),signal_name='PropertiesChanged',dbus_interface='org.freedesktop.DBus.Properties',bus_name='org.mpris.MediaPlayer2.banshee')
def wait():
    end=time.monotonic()+1
    while time.monotonic()<end:
        while GLib.MainContext.default().pending(): GLib.MainContext.default().iteration(False)
        time.sleep(.02)
def get(k): return props.Get(iface,k)
def check(k,v):
    wait(); actual=get(k); assert actual==v,(k,actual,v); print('PASS',k,actual)
original={k:get(k) for k in ['Volume','Shuffle','LoopStatus']}
try:
    p.Play(); check('PlaybackStatus','Playing')
    print('metadata',get('Metadata'))
    p.Pause(); check('PlaybackStatus','Paused')
    p.Play(); check('PlaybackStatus','Playing')
    p.PlayPause(); check('PlaybackStatus','Paused')
    for k,v in [('Volume',dbus.Double(.2)),('Shuffle',dbus.Boolean(True)),('LoopStatus','Track')]: props.Set(iface,k,v); check(k,v)
    p.SetPosition(get('Metadata')['mpris:trackid'],dbus.Int64(5000000)); wait(); assert abs(get('Position')-5000000)<500000; print('PASS SetPosition',get('Position'))
    p.Seek(dbus.Int64(2000000)); wait(); assert abs(get('Position')-7000000)<500000; print('PASS Seek',get('Position'))
    assert events; print('PASS PropertiesChanged',len(events))
finally:
    p.Pause()
    for k,v in original.items(): props.Set(iface,k,v)
