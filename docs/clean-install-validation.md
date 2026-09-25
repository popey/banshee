# Clean-install curation and service validation

Validated 2026-09-12 against local revision x1, version 2.6.2, following the
authorized purge and reinstall. No source or packaging changes were required
for this curation pass.

## Music and artwork

- Configured the library through the fresh profile to use `/home/minnie/Music`;
  copying and renaming remain disabled.
- Rescan imported 3,256 original music files. Read-only database audit found
  no missing imported paths and no duplicate paths.
- 226 album groups: 208 automatically covered; 17 exceptional Erasure box-set
  and concert groups received manually matched cache artwork. One untitled
  Erasure group remains uncovered. These curation helpers are not shipped.
- Audio files and tags were not modified by cover curation.

### Artist capitalization follow-up

At the user's request, corrected `KhruangBin` to `Khruangbin` in the artist
and album-artist tags of three FLAC files from เครื่องบิน. FLAC audio MD5
signatures remained unchanged. Ran Tools → Rescan Music Library, then verified
all 24 tracks and all five populated albums use `Khruangbin`. The artist browser
now shows one entry and 19 artists overall. Banshee retained an unused old album
row with zero tracks; it does not appear as a duplicate in the library.
All six screenshot files were recaptured with their existing filenames.

## Podcasts

Subscribed through Banshee's normal UI, with automatic episode downloading
disabled. All six feed covers downloaded automatically and decoded successfully.
The library contains 3,404 episode entries, not 3,404 downloaded audio files.

| Feed | Episodes | Downloaded playback sample |
| --- | ---: | --- |
| Linux Matters | 90 | Peering into the Tube |
| Waveform: The MKBHD Podcast | 374 | Did Apple Actually Duo It? |
| Nature Podcast | 925 | Briefing Chat: The Bunsen burner myth that turns out to be just hot air |
| BBC Inside Science | 669 | What AI agents talk about behind your back |
| The Infinite Monkey Cage | 239 | The North Pole Unwrapped - Russell Kane, Felicity Aston and Lloyd Peck |
| In Our Time | 1,107 | In Our Time Returns for a New Series |

All six sample downloads reached completed status and exist under
`/home/minnie/snap/banshee/common/Podcasts/<feed>/`. Total payload: 264,976,851
bytes. Each sample was opened through the UI, advanced approximately eight
seconds and was paused through MPRIS. This verifies decoding and playback
progress, not listening to every full episode.

## Last.fm

The fresh profile completed browser authorization through Banshee's normal
Preferences flow. Song reporting from Banshee is enabled. Credentials were
entered with the authorized helper without printing them.

Played Erasure's “S O S” from Abba-esque to completion. The Last.fm API first
reported it as now playing, then as a completed scrobble with timestamp
1789239938. Account total increased from 12,517 to 12,518. The following track
was automatically paused. Recommended artists, top albums and top tracks also
loaded in Banshee's context pane.

## Internet Archive

Searched in Banshee for `identifier:alice_dugdale_2006_librivox`. One matching
result opened successfully with description, cover artwork and ten MP3
chapters. Chapter 1 streamed and progressed to 7.049 seconds before pausing;
reported duration was 1,098.970 seconds. Earlier transient service failures did
not recur during this check.

## Screenshots and follow-up

[Screenshot gallery](store/index.html) and [captions](store/README.md) contain
six native, unedited 1600×900 captures. Playback was left paused after curation.

The upstream fork / stock master / revival branch publication approach is
recorded in [TODO.md](../TODO.md). No fork or Store publication was performed.
The fresh-install default music-folder issue remains a separate open task:
this profile was explicitly pointed at the real Music directory.
