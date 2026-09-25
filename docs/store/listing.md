# Banshee Store listing

## Title

Banshee

## Summary

Your music collection, podcasts and a little nostalgia

## Description

Banshee is back. Enjoy the classic Linux music player, revived for today's desktops.

Browse your music collection by artist and album, build playlists and listen to your favourite podcasts, all in a familiar GTK interface with album artwork and desktop playback controls.

- Play and organise your local music collection.
- Subscribe to podcasts, download episodes and listen offline.
- Scrobble your listening to Last.fm and discover related artists, albums and tracks.
- Search the Internet Archive and stream audio, including LibriVox audiobooks.
- Control playback through your desktop's media controls and MPRIS-compatible tools.

On first launch, Banshee suggests your desktop's music folder. You can choose another folder or skip setup until later. Music is imported only when you confirm, and your files stay where they are by default.

This is an unofficial community preservation build of Banshee 2.6.2, maintained by popey. It includes compatibility fixes and restores selected online services. It is not an official release from the original Banshee developers, and not every historical feature or service is supported.

Last.fm features require an internet connection; scrobbling also requires a Last.fm account. Podcast and Internet Archive browsing and downloads require an internet connection.

For music on USB or other mounted drives, enable removable-media access:

    sudo snap connect banshee:removable-media

The drive must be mounted somewhere the snap can access, with suitable file permissions.

## Suggested screenshot order

1. `01-library.png` — Browse your music collection.
2. `03-podcasts.png` — Follow your favourite podcasts.
3. `05-lastfm.png` — Discover related music with Last.fm.
4. `06-internet-archive.png` — Explore audiobooks and audio from the Internet Archive.

`04-linux-matters.png` is an alternative podcast shot; `02-playback.png` shows
Now Playing with album artwork. All six are available in the gallery.

## Assets and provenance

- `icon.png`: transparent 512×512 PNG rendered from the upstream high-resolution
  SVG, retaining the original Banshee artwork.
- `icon.svg`: unchanged upstream vector source from Banshee 2.6.2,
  `data/icon-theme-hicolor/src/music-player-banshee-hires.svg`.
- Screenshots: native, unedited captures of this snap running on Linux with Yaru;
  1600×900 PNGs, each below 2 MB. See `README.md` for artwork curation notes.
- Icon sizing reference: https://snapcraft.io/about/listing
- Metadata limits reference: https://snapcraft.io/docs/snapcraft-yaml-schema/

This file is prepared copy for the Store dashboard. It has not been applied to
the live listing or the description in `snap/snapcraft.yaml`.
