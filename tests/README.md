# Revival regression runner

Inside the LXD builder, run:

```sh
sh /path/to/tests/run-built-regressions.sh /root/parts/banshee/build
```

This compiles the existing probes and runs 24 service checks, nine podcast
transfer cases and 14 XDG folder cases against freshly built assemblies.
It uses local fixtures, not a real Last.fm account or the music library.
Results of the Git-source migration are in
[the migration validation](../docs/revival-validation.md).

# Podcast recovery checks

These tests exercise the actual compiled `Migo.dll` against a local HTTP
fixture. They do not use public services or the user's music database.
Run inside the core22 LXD builder after building Banshee:

```sh
export MONO_CFG_DIR=/etc
export MONO_PATH=/root/parts/banshee/build/bin
mcs -r:/root/parts/banshee/build/bin/Migo.dll \
    -out:/root/podcast-download-probe.exe /root/podcast-download-probe.cs
python3 /root/podcast-downloads.py /root/podcast-download-probe.exe
```

Copy the two test files into `/root` with `lxc --project snapcraft file push`
first. The paths above describe the existing local Snapcraft builder; adjust
its name/paths for another builder. Build the application with
`snapcraft --use-lxd`, never destructive mode.

Cases: fresh download, partial resume, ignored Range, wrong Content-Range,
complete partial file (HTTP 416 recovery), unknown response length, truncated
response, HTTP 404, and filesystem failure. Successful files must match the
fixture byte-for-byte. Failed transfers must complete without being imported.
The GUI test additionally checks retrying an existing DownloadFailed episode,
HTTPS stream/play/seek, and persistence of downloads after a snap refresh.

`tools/gstreamer-network-probe.c` exercises the native GStreamer HTTPS source.
Compile it in LXD with `gcc` and `pkg-config --cflags --libs gstreamer-1.0`, then
run it inside the installed snap with the launcher's native library, plugin,
GIO and certificate environment. It must receive data from a valid HTTPS URL
and reject expired, self-signed, and wrong-host certificates.

## Desktop controls

With the snap running, select a normal seekable library track, then run
`/usr/bin/python3 tests/mpris-controls.py` in the host desktop session.
Requires `python3-dbus` and `python3-gi`. It tests play/pause, metadata,
volume, shuffle, repeat, absolute/relative seeking, and PropertiesChanged.
It changes playback position, restores the three preferences, and leaves
playback paused. Do not use the legacy OpenUri radio path for this test.
Tray actions, media keys, GNOME artwork and notifications require additional
real desktop checks described in VALIDATION.md.


# Internet Archive and Last.fm

Copy `internet-services-probe.cs` into `/root` in the LXD builder, then:

```sh
export MONO_CFG_DIR=/etc
export MONO_PATH=/root/parts/banshee/build/bin
mcs -r:$MONO_PATH/Lastfm.dll -r:$MONO_PATH/Banshee.InternetArchive.dll \
    -r:$MONO_PATH/Hyena.dll -out:/root/internet-services-probe.exe \
    /root/internet-services-probe.cs
mono /root/internet-services-probe.exe
mono /root/internet-services-probe.exe live
```

Default mode runs local fixtures, including a loopback HTTP server on port
18963. It checks Archive metadata, 64-bit JSON integers, escaped URLs, API
errors, Last.fm request/error handling, and retention of unacknowledged queued
tracks. Queue tests use an in-memory fake queue and never submit scrobbles.

`live` checks Archive search/details, Last.fm artist and public user data,
and token/signature handling. It creates an unauthorised token but does not
open a browser, persist credentials, or submit listening history. The token
is never printed. Live checks depend on external service availability.
Use a consenting account separately for actual authorization/scrobbling.

To test the installed strict snap, copy the compiled executable beside its
source and run from a snap shell (replace the checkout path as appropriate):

```sh
snap run --shell banshee -c '/home/minnie/Projects/banshee/tests/run-internet-services.sh'
snap run --shell banshee -c '/home/minnie/Projects/banshee/tests/run-internet-services.sh live'
```

The script uses the installed assemblies and trust store. It does not launch
Banshee or change the user's account configuration.

## Snap first-run music selection

`music-folders-probe.cs` loads the built Nereid assembly and tests the actual XDG
parser with localized names, spaces, quoting, external paths, disabled folders
and shell-substitution rejection. Compile with `mcs` in the LXD build container,
then run `mono music-folders-probe.exe /path/to/bin/Nereid.exe`.

`run-music-setup.sh` runs the installed snap with an isolated profile under
`SNAP_USER_COMMON/music-setup-test`. Invoke it through `snap run --shell banshee`
with case `skip`, `choose`, `suggest` or `error`. It supplies a synthetic desktop setting
for `Musique # collection`; it does not change the real desktop or library.
Check no import before confirmation, chooser cancellation, Not now persistence,
explicit import from both the suggestion and another folder, and relaunch.
Reusing each case preserves its profile for persistence tests.
