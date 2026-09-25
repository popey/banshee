# Stable Store clean-install validation

Validated on Ubuntu GNOME/X11 on 2026-09-25. Removed the local installation
with `snap remove --purge banshee`, then installed using
`snap install banshee --channel=stable`. No backup or profile migration was
used. Original music files were retained.

Installed result: Banshee 2.6.2, signed Store revision **1**, tracking
**latest/stable**, publisher **popey**. No manual D-Bus/MPRIS connections,
rebuild, or source changes were needed.

## Results

- Fresh music setup suggested `/home/minnie/Music`; no import occurred until
  confirmation. Imported 3,256 tracks. Read-only audit found 3,256 distinct
  original file paths, no missing files, and one Khruangbin artist (24 tracks).
- Album artwork downloaded automatically; Yaru styling and toolbar icons
  displayed correctly. This pass did not restore manually curated covers.
- All three session-bus names were owned: `org.bansheeproject.Banshee`,
  `org.bansheeproject.CollectionIndexer`, and `org.mpris.MediaPlayer2.banshee`.
- `tests/mpris-controls.py` passed playback, pause, toggle, volume, shuffle,
  repeat, absolute/relative seeking and change signals. GNOME's media panel
  visibly displayed the music cover. The notification-area indicator
  registered and displayed its action menu.
- Six podcast subscriptions loaded successfully: Linux Matters, Waveform,
  Nature Podcast, BBC Inside Science, The Infinite Monkey Cage and In Our
  Time. Total: 3,415 episode entries. All six cached feed covers decoded.
  Automatic episode downloads were left disabled.
- Downloaded Linux Matters “Ooh, you are nøughty” (31,905,407 bytes) and
  Waveform “The Worst Idea YouTube's Ever Had...” (168,637,994 bytes).
  Both completed under `~/snap/banshee/common/Podcasts`, then advanced about
  eight seconds during playback. Both exposed existing local artwork files
  through MPRIS.
- Last.fm browser authorization succeeded. Enabled song reporting and played
  Erasure's “S O S” from Abba-esque to completion. The public API reported
  now-playing and then a completed scrobble (timestamp 1790327997); the total
  increased from 12,518 to 12,519. The next song was paused automatically.
- Internet Archive search for `identifier:alice_dugdale_2006_librivox`
  returned Alice Dugdale. Details, cover and ten chapters loaded. Chapter 1
  streamed to 7.915 seconds before pausing, with existing local MPRIS artwork.
- The installed Store assemblies passed 24 service regression checks and
  six live service checks using `tests/run-internet-services.sh`.
- Quit and relaunched through the snap launcher. The music library, six
  subscriptions, two completed downloads and Last.fm credentials persisted.
  Last.fm's user panel loaded again. SQLite integrity check returned `ok`.
  No first-run prompt reappeared. Playback was left stopped.

## Scope and remaining limitations

This is a smoke test of the main flows, not every legacy Banshee feature or
media format. Optical-disc services still report that no HardwareManager is
available. Physical CD/DVD and device-sync functionality were not validated.
`removable-media` remains disconnected by default; external-drive playback
was not tested. No snap-refresh transition was performed during this pass.

The application database/settings remain in revisioned `SNAP_USER_DATA`;
artwork and downloaded podcasts use `SNAP_USER_COMMON`. Restart persistence
does not imply that every metadata file is stored in common.

Runtime log uses the fixed path `/tmp/banshee-runtime.log` (the final launch
replaced the earlier launch log). Existing curated Store screenshots were
left unchanged.
