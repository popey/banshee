# Validation — Banshee 2.6.2-snap4

Tested on 2026-09-09, amd64, Ubuntu 24.04 host, GNOME/X11-compatible display,
PipeWire with its PulseAudio server. Built on Ubuntu 22.04 in Snapcraft's LXD
container, exclusively using `snapcraft --use-lxd`. Installed with
`snap install --dangerous ./banshee_2.6.2-snap4_amd64.snap`.

## Passed

- Source and snap compilation, YAML parsing, launcher shell syntax.
- Bundled Mono runtime starts Banshee; final installed snap launches normally
  under strict confinement, without test environment overrides.
- Toolbar, sidebar, and search icons render correctly. See
  [installed application screenshot](docs/banshee-snap4.png).
- Import through the GUI folder chooser, using a generated 30-second MP3 tone
  with artist, album, and title tags. No native crash after the URI-list fix.
- Track decoding and seeking; playback position advances.
- Final installed build opens an active, unmuted audio stream on the host:
  `pactl list sink-inputs` reports Banshee, the test title, `Corked: no`,
  `Mute: no`, 44.1 kHz mono PCM, `pipewire.snap.id = banshee`, and
  `pipewire.snap.audio.playback = true`. Physical speaker output was not
  independently listened to by the agent.
- Created and named a playlist, dragged the track into it, and verified its
  database entry. Track and playlist survive quitting, snap update, and
  normal relaunch.

## Fixes included

- Ubuntu's GStreamer 1.0, Mono profile, SQLite, and native-library patches.
- Modern Mono's CollectionExtensions naming collision.
- Explicit Mono build environment, assembly lookup, and GTK# native-glue layout.
- Respect the existing `--disable-dbus` option before attempting a connection.
- Correct GSList/string ownership for GTK folder and file import URI lists.
- Runtime GdkPixbuf loader cache and build-time shared MIME database generation.
- Host PulseAudio socket selection and private audio/BLAS/LAPACK library paths.
- Revision-specific GStreamer cache and an explicit desktop icon path.

## Remaining limitations

This is a development preservation build, not a fully modernized application.

- Historical snap4 limitation: D-Bus was disabled. See the desktop integration
  results below for its restoration in snap9 and later.
- Device syncing and optical-disc integration are disabled/unavailable.
- Old online metadata services can fail with obsolete API responses or TLS
  errors. Certificate trust is now packaged; see the podcast validation below.
  Last.fm authentication/scrobbling, Archive integration, and radio remain
  unvalidated or broken. Podcast HTTPS streaming is validated below.
- GTK# accessibility initialization warnings remain; screen-reader support is
  not validated. The optional canberra GTK module also produces a warning.
- TagLib# initially estimates this VBR MP3 as 17 seconds, while GStreamer
  correctly plays it as 30 seconds. Broader format testing remains necessary.
- Snapcraft still reports optional GPU-library/content-interface advisories
  and unused-library notices (including libraries dynamically loaded by Mono).
  No missing-library lint errors remain. Hardware-accelerated video is untested.
- Removable-media access is declared but optional; connecting and testing it
  is left to users who need it. Only amd64 has been built.
- Store publication and a 2.9.1 comparison are deferred.

## Real library import test

On 2026-09-09, imported `~/Music` through the GUI folder chooser in the
installed `2.6.2-snap4` snap. All 449 audio files (343 MP3, 106 FLAC) were
represented by matching source URIs in the database; zero missing files,
zero duplicate URIs, and no empty track titles. The resulting library has
450 tracks including the earlier generated test tone. Source file inventory,
sizes and modification timestamps were unchanged. Album/artist browsing and
some album artwork populated successfully; this test does not establish
complete artwork retrieval or playback of every imported track.


## HTTPS and podcast validation — 2026-09-09/10

Built and installed `2.6.2-snap6` with `snapcraft --use-lxd`, strict confinement.
The certificate-only snap5 passed installed-runtime HTTPS checks: Linux Matters
feed and Archive search returned HTTP 200; Last.fm returned HTTP 400 for the
intentionally unauthenticated/missing-key request, demonstrating TLS success
without claiming API authentication. Expired, self-signed, and wrong-host
badssl endpoints all failed certificate validation. Snap6 retains that store.

Through the normal GUI, subscribed to the correct Linux Matters feed, loaded
90 episodes with artwork, refreshed, and kept automatic downloads disabled.
Removed the empty failed subscription for the incorrect `/episodes/` URL.
Downloaded “Talking to my Computer” (LMP88.mp3, 34,406,894 bytes), played it,
and sought to 18:51 of 40:35. PipeWire reported the named podcast stream active
and unmuted, 44.1 kHz mono PCM; this is output verification, not a listening test.
After a clean quit/restart, refreshed the feed and replayed the same saved file.
Database retained the subscription, 90 episodes, successful download status 3,
and all 449 user music tracks. Left playback paused.

The test exposed revision-specific library folders saved by early builds.
`snap-library-paths.patch` normalizes snap revision folders to their stable
`current` alias; new downloads use that path. It does not rewrite arbitrary
existing track URIs or move external music files. A subsequent snap refresh
with an existing podcast download still needs explicit regression testing.

Remaining findings: the first episode's failed save left a complete partial
file and subsequent retries did not recover. Preserved that file in
`/tmp/banshee-snap5-failed-save-LMP89.mp3`; testing a fresh episode succeeded.
The cause of retry failure needs investigation. Direct remote playback produced
GStreamer resource-read errors; downloaded playback succeeded. Debug-mode
startup after snap5 refresh failed while rebuilding Mono.Addins descriptions;
normal startup and subsequent restarts worked. These are tracked in TODO.md.


## Podcast reliability — 2.6.2-snap7 (2026-09-10)

Built and installed with `snapcraft --use-lxd`; strict confinement retained.
`podcast-download-reliability.patch` fixes the guard that silently excluded
DownloadFailed episodes from manual retry, prevents corrupted appends when a
server ignores/misreports Range, resets restart state, handles unknown response
lengths, and rejects truncated known-length responses. Download failures now
show safe error categories and retry instructions. No certificate checks were
removed. The launcher selects the bundled GIO module and CA paths and defaults
to the direct (`dummy`) proxy resolver: the previous desktop proxy portal
returned NotAllowed inside the snap. Automatic proxy discovery needs separate
validation; an explicitly supplied resolver is preserved.

Validation:

- Nine local HTTP/filesystem regression cases passed against the compiled
  Migo library: fresh, resumed, ignored Range, wrong Content-Range, HTTP 416
  from a complete partial, unknown response length, truncated response, HTTP
  404, and filesystem failure. Successful payloads matched byte-for-byte.
  See `tests/README.md` for reproducible commands.
- Restored the original complete partial LMP89.mp3, then chose Download for
  the existing failed “Peering into the Tube” entry. It recovered automatically
  to DownloadStatus 3; the 31,831,167-byte saved file's SHA-256 matched the
  preserved original. No manual database editing or resetting of status.
- Streamed undownloaded “Herding online exams” directly over HTTPS and sought
  to 13:42 of 29:56. PipeWire showed the named stream active/unmuted, mono
  44.1 kHz PCM. Database LocalPath stayed null and DownloadStatus stayed 0.
  This verifies audio output, not an independent listening test.
- Native GStreamer HTTPS checks rejected expired, self-signed and wrong-host
  certificates after the resolver fix.
- The snap6-to-snap7 refresh preserved all 449 user music URIs, subscription
  settings, and the exact SHA-256 of the existing LMP88.mp3 download. After
  another restart, both real downloads and all 90 Linux Matters episodes
  persisted; LMP88 replayed through an active audio stream.
- A local GUI fixture first returned HTTP 404. The app displayed its status
  and retry instructions. After switching the fixture to success, retrying
  in the same session downloaded the exact expected payload. Removed the
  generated test subscription/file and stopped the fixture server afterwards.

The earlier retry/direct-streaming failures above are superseded by these
results. Broad feed-subscription error reporting, proxy-only networks,
authenticated feeds, and shutdown/resume scenarios remain follow-up work.
GTK accessibility and occasional GStreamer query warnings still occur; no
resource-read errors occurred during the successful streaming test. Banshee
is left open with playback paused.

## Desktop integration — snap9 baseline (2026-09-11)

- Strict snap starts with D-Bus enabled using Ubuntu's dbus-sharp 0.8.1
  (assembly ABI 2.0). Primary, CollectionIndexer and MPRIS bus names have
  explicit snap slots. MPRIS addin defaults to enabled.
- Automated controls on a real Depeche Mode library track passed play,
  pause, play/pause, volume, shuffle, repeat, absolute and relative seeking,
  and PropertiesChanged signals. See `tests/mpris-controls.py`.
- Synthesized XF86AudioPlay and XF86AudioNext keys reached Banshee through
  the desktop. GNOME's media-panel pause button paused a streaming podcast.
- Linux Matters episode 87 streamed from HTTPS and advertised a local
  `file:///home/minnie/snap/banshee/common/.cache/media-art/podcast-….jpg`.
  The file exists and GNOME visibly displayed its cover. The library track
  likewise advertised a local cached album image.
- All track types use the common MPRIS metadata builder, which emits local
  file artwork URLs. A non-null artwork ID does not guarantee its cache file
  exists: missing/failed artwork downloads may still yield a fallback icon.
- Native Ayatana indicator registered with GNOME's StatusNotifierWatcher;
  tray Play and Quit actions worked. Initial tests found widgetless
  notifications and focus-dependent Show/Hide needed further fixes.
- MPRIS OpenUri uses upstream's radio path even for file URLs; a test file
  played but Pause stopped it. Normal library playback has working pause
  and seeking. Do not claim every MPRIS OpenUri use behaves like an import.

The 2.9.1 source comparison is recorded in
[desktop migration notes](docs/2.9.1-desktop-comparison.md). Its MPRIS C# files
are identical; the new D-Bus and snap desktop-entry patches apply to it.
No 2.9.1 build/runtime validation is claimed. Its GTK3 tray requires a port.

## Final desktop build — 2.6.2-snap10 (2026-09-11)

Built exclusively with `snapcraft --use-lxd`, installed without sudo, and
launched normally under strict confinement on the same Ubuntu GNOME host.

- Repeated the automated MPRIS controls successfully against the final build.
- Second `snap run banshee --show` exited successfully while the primary
  MPRIS owner and Unix PID remained unchanged. No second library writer.
- Verified MPRIS DesktopEntry is `banshee_banshee`, matching the installed
  snap desktop file; GNOME now shows Banshee's application icon.
- Synthesized media play/pause keys changed the reported playback state;
  Next changed the track ID.
- Captured Banshee's real `org.freedesktop.Notifications.Notify` call with
  title, artist/album and a local cached JPEG. GNOME's notification panel
  visibly displayed both the notification and its cover image. Notifications
  retain upstream low urgency; a popup banner is not guaranteed by GNOME.
- Ayatana Show/Hide toggled the actual X window between IsViewable and
  IsUnMapped. MPRIS Raise restored it; Alt+F4 hid it without terminating
  Banshee. Tray Play started playback. Tray Quit terminated the process and
  released the MPRIS bus name.
- Removed the custom star-rating widget from the exported indicator menu;
  the GTK drawing widget otherwise appeared as a blank rating row. Ratings
  remain available inside Banshee.
- No D-Bus startup or notification exceptions in the final runtime log.
  Existing accessibility and disabled optical-hardware warnings remain.
- Library still lists 450 tracks (449 user files plus the test tone), the
  preservation playlist, and the 90-episode Linux Matters subscription.

Scope: Ubuntu GNOME with ubuntu-appindicators enabled. Other desktops,
multiple competing players, and an environment without a status-notifier
host remain untested. Explicit --disable-dbus bypasses D-Bus single-instance
activation. Store interface review and publication remain future work.

## Shared GTK2 themes — 2.6.2 (2026-09-11)

- Built with `snapcraft --use-lxd` to `banshee_2.6.2_amd64.snap`; reused
  `/tmp/banshee-build.log` and `/tmp/banshee-runtime.log`.
- Three content plugs connected automatically on local install: gtk-2-themes
  and icon-themes from gtk-common-themes, gtk-2-engines from gtk2-common-themes.
- Launcher sets GTK_DATA_PREFIX, GTK_PATH and the shared XDG data path;
  no upstream source patches or desktop preference changes were added.
- Read the live XSettings selection: Net/ThemeName and Net/IconThemeName both
  Yaru. A preliminary gsettings query returned Adwaita defaults and did not
  reflect the active XSettings used by GTK2.
- Normal launch visibly renders Yaru controls, orange selections and icons.
  Library playback started and MPRIS reported Playing. Library, playlist and
  podcast counts remain present. Screenshot: `docs/banshee-theme.png`.
- Quit, disconnected all three theme plugs, and relaunched: basic GTK2 UI
  remained readable with bundled icons, and MPRIS responded. Restored all
  three connections after testing.
- Provider revision 1535 advertises missing/invalid Materia-compact and
  Yaru-MATE GTK2 paths. snap-update-ns reports warnings for those unrelated
  variants; Yaru mounts and works. Do not claim all provider themes validated.
- Dark variants, high contrast, and GTK3/2.9.1 are not covered by this test.

## Internet Archive and Last.fm — 2026-09-11

Built with `snapcraft --use-lxd` and installed without sudo as version `2.6.2`,
local revision **x13**. The fixed artifact is `banshee_2.6.2_amd64.snap`, SHA256
`f23c75488cc3982097f10703e489ade30e9fb0d3af6f11d215eea26cb4784ccb`.
Build/runtime logs remain `/tmp/banshee-build.log` and
`/tmp/banshee-runtime.log`. The unchanged upstream tarball retains SHA256
`f77c089b05e3dc956236d13ff02945fe560f56e402df57621a9196de39ba60f8`.

Three local patches bring the series to 21: Archive API compatibility,
Last.fm API/transport/recovery, and Hyena 64-bit JSON numbers. All apply cleanly.
The LXD build completed with existing library/GPU lint warnings. Known GTK
accessibility and theme-provider mount warnings remain.

- **22 fixture checks passed** against the final LXD assemblies and again
  against the installed strict snap using `tests/run-internet-services.sh`.
  Coverage includes current/legacy Archive metadata, private files and escaped
  URLs, fractional durations, Int64 bounds and small-integer compatibility,
  empty/error responses, Last.fm JSON/XML errors, credential redaction, API 2
  XML models, scrobble retention on failed/malformed/mismatched acknowledgments,
  successful acknowledgment, failed now-playing recovery and POST form bodies.
- **Six live API checks passed** inside the installed snap: Archive search and
  item metadata; Last.fm similar artists/top albums/top tracks; public recent,
  loved and top-artist lists; authorization token; valid signed session request
  returning the expected error 14 before browser authorization. Tokens were
  not printed or persisted. No scrobbles were submitted.
- Archive GUI search for `identifier:alice_dugdale_2006_librivox` returned
  **Alice Dugdale** by Anthony Trollope. Details displayed the description,
  public-domain license metadata, ten MP3 chapters, lengths and sizes.
  Chapter one streamed from HTTPS; MPRIS reported Playing, the correct URL,
  and an advancing position. Playback was paused after 28 seconds. The item
  survived refresh to x13 and remained in the sidebar.
- Empty-query-result test initially encountered a real timeout. The UI showed
  “Timed out searching the Internet Archive” with “Try Again”; retry recovered
  and displayed “No matches.” A malformed `title:(` query displayed an error
  rather than a misleading empty success. These GUI error tests ran on x13.
- Direct chapter URL download (host curl, separate from the app) returned
  **15,307,325 bytes**, parsed by Mutagen as a **1,098.94-second MP3**. The file
  is `/tmp/banshee-archive-chapter.mp3`. Banshee's upstream Archive view has no
  in-app download action; do not count this as in-app download/import support.
- Final-build local Erasure playback succeeded. Enabling View → Context Pane
  displayed live Last.fm recommended artists (including Pet Shop Boys/Yazoo),
  top albums and top tracks. `docs/banshee.png` records this working UI.
  Playback was left paused and the existing volume of zero was preserved.

Remaining: consenting-account browser authorization, session persistence,
now-playing/scrobbling against the real account, offline replay and sign-out.
The historical application key works for these public/handshake probes;
project ownership of API credentials still needs a release decision. Last.fm
radio is not validated. Archive download/import UI, item artwork caching and
search subscriptions remain follow-ups. Automatic MusicBrainz artwork still
uses obsolete services and can log XML errors; that is separate roadmap work.
See `docs/internet-services.md` for implementation and API references.

## Last.fm consenting-account test and recovery fixes — 2026-09-11

The user supplied `.env` credentials and authorized login and full scrobbling
validation. Credentials were read locally and typed via subprocess stdin, not
expanded into command arguments. Passwords/session tokens were not printed.
Browser inspection excluded authorization URL tokens and account-name fields.
`.env` was added to `.gitignore`; the credential file is absent from the snap.

The browser test found missing host URL launching. `snap-browser-launch.patch`
uses `io.snapcraft.Launcher.OpenURL` through the already connected desktop
interface. The browser callback's error text omits URL query strings. No new
interface or external browser dependency was added. The patch series now has
**22 source patches** (10 Ubuntu, 12 local).

Account tests:

- Firefox login succeeded, the Last.fm consent page identified **Banshee Media
  Player**, and Banshee finished authorization with result `None` (success).
- Enabled song reporting from Banshee only. Restarted the app; saved session
  and username remained, and the real API showed Erasure as now playing.
- Two completed online tracks were accepted: “Lay All Your Love On Me” and
  “S O S”. Playback continued during the interactive test; both are actual
  library tracks, not synthetic submissions.
- Disconnected **only** `banshee:network` and finished “Take A Chance On Me”.
  An automatic watcher paused the following track within one second.
  The third scrobble remained on disk through failed requests and retries
  approximately 62 seconds apart; Last.fm still showed only the first two.
- Restarted offline, then reconnected the network. The queue persisted, but
  no upload occurred: upstream starts its connection only after another
  completed track or account update. This failed gate prompted the startup
  fix in `lastfm-api.patch`. Authorized, enabled startup now starts the queue;
  signed-out accounts cannot start scrobble/now-playing submissions.
- Installed the final build with the original queued play preserved. Startup
  uploaded it at **18:33:25 UTC**, `1 accepted, 0 ignored`, while MPRIS reported
  **Stopped**, with no active track. The local queue became empty. Last.fm's
  history contained each of the three titles exactly once, with the original
  play-start timestamps; total count increased by exactly three.
- Logged out through Banshee's preferences. Both stored username and session
  became empty. Played an eight-second A$AP Rocky sample while signed out,
  then paused; the remote history count stayed unchanged and the recent-tracks
  API reported no now-playing item. Queue remained empty.
- Reauthorized Banshee successfully at **18:37:38 UTC**. Left the account signed
  in, song reporting enabled, and playback paused. Network connectivity is
  restored. `removable-media` remains an available, optional disconnected plug;
  README already documents `snap connect banshee:removable-media`.

Final build: `snapcraft --use-lxd`, installed without sudo, version **2.6.2**,
local revision **x15**. Fixed artifact `banshee_2.6.2_amd64.snap`, SHA256
`ce24f0bdea740895253bb91d99fe8705bc488d7c5042033f8c98e1955302417d`.
The final **24 fixture checks passed in LXD and against the installed strict
snap**. Existing lint/theme/accessibility warnings remain. Last.fm radio and
public-release ownership of API credentials remain outside this validation.
Build/runtime logs remain `/tmp/banshee-build.log` and `/tmp/banshee-runtime.log`.


## Automatic artwork restoration — 2026-09-11/12

- Built exclusively with `snapcraft --use-lxd`; installed without sudo.
- Version 2.6.2, local revision x16; artifact `banshee_2.6.2_amd64.snap`.
- SHA-256: `730649ea2dcb4c25305a2bb921f2f6d7ec867b2720ddfcee43219be5f375c09f`.
- Added `artwork-reliability.patch`, bringing the series to 23 patches
  (10 Ubuntu, 13 local). The vendored source archive is unchanged.
- 18 isolated artwork checks pass in LXD and the installed strict snap:
  encoding, typed Last.fm album caches, image-size selection, invalid/oversized/
  truncated responses, GIF compatibility, existing covers, filesystem errors,
  MPRIS missing/unknown/local artwork and Archive item identities/provider order.
- Existing 24 Internet Archive/Last.fm service checks pass inside the snap.
- Four live artwork jobs passed in LXD. The installed snap downloaded Last.fm
  and tagged Cover Art Archive images successfully; the later Archive thumbnail
  check received HTTP 502, reproduced with host curl. Another real item's image
  also returned 502. Search still works; cached item details remain usable.
- Real UI recovery: backed up and removed only Heligoland's cover, selected an
  album track in the library, and observed automatic regeneration, visible
  artwork in album browser/player header and an existing local MPRIS art URL.
  No music tags were changed; the backup is `/tmp/banshee-artwork-cover-backup.jpg`.
- Library audit: all 3,256 music files still imported, no duplicates, only the
  untitled Erasure group lacks artwork. `docs/banshee-artwork.png` records the UI.
- Snap refresh emitted gtk-common-themes mount warnings for unrelated Materia/
  Yaru-MATE variants; normal Banshee startup and the tested Yaru UI still work.

See `docs/artwork.md` for implementation scope and the Archive availability
limitation. Existing artwork retry scheduling and edition matching remain open.


## Shared podcast downloads — 2026-09-12

Configured `apps.banshee.environment.XDG_PODCASTS_DIR` as
`$SNAP_USER_COMMON/Podcasts` in snapcraft.yaml. Banshee already honours this
variable. No source patch or migration helper was added; the initially written
migration helper/tests were removed because this is the sole development install.

For this installation only, moved the two existing episodes (66,238,061 bytes)
to `common/Podcasts`, verifying SHA-256 and sizes against all retained copies
before deleting identical duplicates. Replaced revision-local Podcast directories
with compatibility symlinks and updated the active podcast library preference.
Existing database URIs remain valid. The config backup is
`/tmp/banshee-podcast-config-backup.xml`.

Built with `snapcraft --use-lxd`, installed without sudo as x17/version 2.6.2.
Artifact SHA-256: `14ee6ea8e1def043343fd0ffa24d881328262298c0636c62681416a3602dcce2`.
The actual x16 → x17 refresh preserved the symlink, without copying episode
payloads. Verified the environment setting and both old/new paths inside strict
confinement; both database episode URIs resolve to shared files. Normal Banshee
startup completed. Total development-install data fell from about 171 MiB to
107 MiB. Library/configuration databases remain revision-specific for now.


## Purge/reinstall first-run check — 2026-09-12

At the user's request, closed Banshee and ran `snap remove --purge banshee`,
without creating a backup. Confirmed `~/snap/banshee` was gone and `~/Music`
remained. Reinstalled the same local 2.6.2 artifact (SHA-256
`14ee6ea8e1def043343fd0ffa24d881328262298c0636c62681416a3602dcce2`).
The new install is local revision x1 with a fresh user profile.

- Normal startup succeeded; the library contains zero tracks. No import wizard
  appeared. No music was imported or Last.fm account reauthorized during this check.
- Copy-on-import and move/rename-on-save default to false.
- The podcast library defaults directly to `~/snap/banshee/common/Podcasts`.
- The music library still defaults to `~/snap/banshee/current/Music`; recorded
  a follow-up to use the user's real XDG music directory.
- Theme content, home, network, audio and desktop interfaces auto-connected.
  Optional removable-media remains disconnected, as expected.
- Fresh startup logs contain an InternetRadio legacy XSPF migration exception
  in addition to the known accessibility/optical-disc warnings; the UI opens.
- Saved the untouched app window to `docs/banshee-first-run.png` and left the
  empty first-run library open for the user.
