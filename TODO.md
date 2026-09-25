# Banshee preservation roadmap

Target: a trustworthy Banshee 2.6.2 preservation snap, first available for
opt-in testing on the Snap Store, then suitable for a Linux Matters demo.
Builds must use LXD, never destructive mode. Use upstream versions without
local build-counter suffixes and reuse fixed artifact/log filenames (see README).
The intended release path is 2.6.2 in stable after validation, with 2.9.1
potentially evaluated in candidate or beta. Version 2.6.2 is published in stable;
the Git-source migration is local and awaiting validation/publication.

## Current baseline

- [ ] Harden Last.fm login: handle token/browser-launch failures and preserve
  the API-returned username. Investigate Joey's login/Archive/video reports
  with exact reproduction details. [Investigation](docs/lastfm-report-investigation.md).

- [ ] Investigate enabling GitHub Issues on `popey/banshee`: check fork
  settings, decide labels and bug-report templates, and link the tracker
  from the README and Snap Store metadata once enabled.
- [ ] Investigate GTK/Hyena accessibility initialization warnings (`model_changed`,
  AtkSelection/AtkTable) observed when opening podcasts in the Git-built snap;
  compare with the Store build and test screen-reader behaviour.

- [x] Pin and vendor the unchanged upstream 2.6.2 source archive.
- [x] Build a strict amd64/core22 snap with its Mono and GTK# 2 runtime.
- [x] Fix modern compilation, GTK import crash, icons, and audio setup.
- [x] Import all 449 MP3/FLAC files in the real test library without missing
  files or duplicates; verify playlist persistence and an audio stream.
- [x] Diagnose shared HTTPS trust failure and Archive's search parameter error.
- [x] Restore D-Bus single-instance activation, MPRIS/media keys, native
  Ayatana tray actions, and track notifications; validated on Ubuntu GNOME.

The branch incorporates **24 upstream source patches**: ten carried from Ubuntu,
and fourteen local patches (modern Mono/shell test, D-Bus disable option,
GTK file-chooser URI ownership, stable snap library folders, podcast download reliability, newer D-Bus bindings, Ayatana integration,
the snap MPRIS desktop-entry identity, Internet Archive API compatibility,
Last.fm API compatibility/recovery, JSON integer handling, the snap browser launcher, automatic artwork recovery, and first-run music selection). See
`docs/provenance/patch-commits.json`. This is a patched
source build, not merely a wrapper around an untouched executable.
The revival branch contains the patched source directly. Snapcraft configuration
and the launcher supply the confinement, bundled runtime, paths, and caches.

## P0 — next work: HTTPS and podcasts

1. [x] Populate Mono's trust stores in the snap build using Ubuntu's CA bundle
   and `cert-sync`; expose them at the paths Mono actually reads. Include the
   appropriate CA packages and a refresh mechanism tied to snap updates.
   Merely setting `MONO_CFG_DIR` or extracting `ca-certificates-mono` is not
   enough: package post-install hooks do not run during staging.
   **Done when:** HTTPS succeeds inside the installed snap for Linux Matters,
   Archive, and Last.fm; expired, self-signed, and wrong-host certificates
   remain rejected. Do not bypass certificate or hostname validation.
2. [x] Restore the Linux Matters subscription using
   `https://linuxmatters.sh/episode/index.xml`.
   **Done when:** subscribe/refresh lists episodes, download one chosen episode,
   play and seek it, restart, and verify subscription/download persistence.
   Avoid automatically downloading the entire feed during testing.
3. [x] Fix failed-download retry/recovery (including complete partial files),
   validate resumed/truncated responses, and restore direct HTTPS streaming.
   Episode download errors distinguish HTTP status, TLS, DNS, timeout and
   filesystem failures without exposing request URLs or credentials.
   Nine downloader regression cases and GUI HTTP-404 recovery passed in snap7.
   Existing podcast download survived a snap refresh and replayed after restart.

Follow-up polish:
- [ ] Extend detailed errors to feed subscription/refresh and invalid-feed
  parsing (episode download errors are now improved).
- [ ] Automatically dismiss a previous episode error after successful retry;
  currently the closeable message records the earlier failure.
- [ ] Test authenticated feeds, proxy-only networks, very large episodes,
  interruption/resume across shutdown, and more podcast providers.

## P1 — service restoration, in this order

4. [x] Internet Archive: use HTTPS directly, remove rejected
   `xmlsearch=Search`, and request `output=json`. Handle JSON error responses
   even when HTTP is 200. Audit item details, artwork, and download URLs
   separately; the old details API cannot be assumed compatible.
   **Done when:** search returns results; opening a public audio item displays
   files; one file streams/downloads; empty and failed searches are readable.
   Validated with Alice Dugdale: ten chapters, HTTPS playback, empty search
   and timeout/retry recovery. Direct MP3 download also passed; the upstream
   Archive view has no in-app download action. See `docs/internet-services.md`.
5. [x] Last.fm: restore HTTPS API/auth, API 2 recommendation/user data,
   browser authorization through the snap desktop launcher, and scrobbling.
   **Validated:** consenting-account login, persisted session after restart,
   live now-playing, three real tracks accepted exactly once, offline queue
   retention/retry, automatic replay after restart, and logout. Signed-out
   playback submitted nothing; the account was reauthorized after testing.
   Keep credentials and session tokens out of diagnostic logs.
   Historical Last.fm radio remains a separate, unvalidated feature.
   See `docs/internet-services.md` and `VALIDATION.md`.
- [ ] Before publishing, establish ownership/maintenance of Last.fm application
  credentials. The historical Banshee registration works for the consenting
  account test; this is not a maintenance arrangement for a Store release.
- [ ] Archive follow-up: add an in-app download/import action and validate search
  subscription. Item artwork is now wired into the cache/UI/MPRIS. Recheck live
  thumbnails: downloads passed in LXD on September 11, but multiple real item
  image endpoints returned HTTP 502 on September 12. See `docs/artwork.md`.
6. [x] Restore automatic album artwork: embedded/folder pictures first, Cover Art
   Archive for tagged release IDs, then Last.fm album API 2. Retire Rhapsody and
   the old MusicBrainz v1/ASIN lookup. Validate recovery in the installed snap.
   Heligoland regenerated automatically in the normal playback UI; MPRIS points
   to its existing local image. 18 artwork fixtures and 24 service regressions
   passed inside the snap. All four live artwork checks passed in LXD; the later
   installed-snap check hit the Archive thumbnail failure described above.
   See `docs/artwork.md` and `VALIDATION.md`.
- [ ] Artwork/metadata follow-up: identify the untitled Erasure remixes before
  choosing cover art; improve edition/bootleg matching and review the background
  scanner's retry scheduling. There are 3,256 user music files and one remaining
  album group without art. Existing user covers and audio tags are preserved.

## P1 — before a public Store edge release

- [x] Offer helpful first-run music selection: suggest the desktop's XDG music
  folder, including localized/custom paths, with Choose another folder and
  Not now. Import only on confirmation, in place; remember the choice and skip
  existing libraries. [Implementation and validation](docs/first-run-music.md).

7. [ ] Share the revival through an upstream fork under `popey` (Martin's
   proposed approach), then switch the Store build to that public source:
   - Identify and verify the canonical upstream repository and the 2.6.2 tag;
     retain upstream history and a stock, unmodified `master` branch.
   - Create `revival/2.6.2` from upstream tag `2.6.2`. Carry
     the Ubuntu compatibility patches with provenance, local fixes, Snapcraft
     packaging, tests and documentation as reviewable commits.
   - Exclude credentials, local profiles/music, downloaded media, generated
     binaries, snaps and diagnostic logs. Preserve licenses and patch authorship.
   - Document how the revival baseline relates to stock master and the later
     2.9.1 experiment; avoid implying that current master is the 2.6.2 release.
   - Make `revival/2.6.2` the default branch once it is ready; build the Store snap
     from that branch with traceable commits/tags and LXD CI validation.
   - Later create `revival/2.9.1` from tag `2.9.1`, porting only applicable
     fixes. Target beta first, then candidate; retain 2.6.2 for stable.
   - Repository choice, verified refs and handoff: [source publication plan](docs/source-publication.md).
   - This is the publication plan only. Fork creation, branch publication,
     default-branch changes and Store publishing have not been performed.
8. [ ] Check/reserve an appropriate available Store name and publisher identity.
   Prepare an accurate unofficial-preservation description, icon, screenshots,
   contact/issue tracker, source link, and supported-feature list.
   - Six populated-app screenshot candidates are available in
     [the gallery](docs/store/index.html), with [captions](docs/store/README.md).
     [Clean-install validation](docs/clean-install-validation.md) records music
     import, six podcast downloads/playback tests, Last.fm scrobbling and
     Internet Archive streaming. Final listing selection remains open.
9. [ ] Review bundled license notices and redistributed dependencies; keep
   source and build instructions accessible.
10. [ ] Run Snap Store review tools and resolve actionable findings. Explain
    optional GPU-library warnings and Mono's dynamically loaded libraries.
    Audit interfaces, including the GTK# layout and optional removable media.
- [x] Store podcast episode downloads in `SNAP_USER_COMMON/Podcasts` through
  the YAML environment setting. The sole development installation was adjusted
  once; no migration helper is shipped.
- [ ] Decide whether to share the library database/configuration across revisions,
  accounting for rollback and 2.9.1 schema compatibility.
11. [ ] Add CI/LXD build and smoke-test coverage: clean build, library import,
    playback, playlist persistence, HTTPS, and feed parsing. Test a fresh user
    profile and an upgrade with an existing library. Preserve user databases.
12. [ ] Define an ongoing rebuild/update cadence for the base, Mono, codecs,
    CA certificates, and other bundled libraries. Record core22 migration
    constraints and evaluate a supported newer base separately.
13. [ ] Publish an opt-in **edge** revision once the above gates pass, with
    explicit limitations. Publication is a future action, not done by this
    planning task. Use beta/candidate for broader validation before stable.

Service restorations need not all block an explicitly local-music-only edge
release, but every advertised feature must work or be clearly marked unavailable.
Podcasts are a priority for the intended Linux Matters demonstration.

## P2 — polish, wider testing, and the podcast demo

- [ ] Figure out dark mode for Banshee 2.6.2: test Yaru-dark, theme selection,
  and readability of custom widgets, dialogs and album/track views.

- [x] Integrate shared GTK2 theme data/icons from gtk-common-themes and
  engines from gtk2-common-themes. Yaru renders through live desktop XSettings;
  playback and fallback startup with all three plugs disconnected passed.
  No upstream patch needed. GTK3/2.9.1 theme testing remains separate.

14. [ ] Investigate debug-mode Mono.Addins cache rebuild failure on refresh.
    Normal startup works. Audit migration of any previously stored absolute
    revision-specific media URIs; library folders now use the stable current alias.
    Desktop D-Bus, single-instance, MPRIS/media keys, tray and notifications
    are now validated in snap10 on Ubuntu GNOME. Test other desktops and
    competing players; explicit --disable-dbus still bypasses single-instance activation.
15. [ ] Investigate GTK# accessibility warnings and keyboard/screen-reader use.
    Test Wayland/XWayland, another desktop, removable media, Unicode paths,
    large libraries, and additional formats. Investigate VBR duration estimates.
16. [ ] Reduce startup warnings from unavailable hardware/optical-disc services
    and retire unsupported integrations in the UI.
17. [ ] Prepare a short Linux Matters demo: install from Store, import local
    MP3/FLAC, show working icons/album browsing, play/seek, persist a playlist,
    subscribe to Linux Matters, and demonstrate Last.fm if validated.
    Explain the preservation snapshot, small compatibility patch series,
    confinement, supported features, and maintenance commitments honestly.
18. [ ] Capture screenshots and a concise release post with install commands,
    limitations, source/issue links, and testing instructions. Promote to
    stable only after independent user testing and regression fixes.

## Later

- [ ] Compare 2.9.1 in a separate build once 2.6.2 is a dependable baseline.
- [ ] Evaluate arm64, portable devices, CD ripping, and video independently.

## Evidence and references

See `docs/network-investigation.md` for the 2026-09-09 diagnosis and
`VALIDATION.md` for existing runtime tests.

- Mono trust-store synchronization: https://www.mono-project.com/docs/about-mono/releases/4.8.0/
- Last.fm API: https://www.last.fm/api
- Last.fm authentication: https://www.last.fm/api/authspec
- Last.fm scrobbling: https://www.last.fm/api/scrobbling
- Archive advanced search: https://archive.org/advancedsearch.php
- Banshee 2.9.1 release notes: https://bansheemediaplayer.github.io/download/archives/2.9.1/

Desktop release follow-up:
- [ ] Check Snap Store review requirements for the Banshee/CollectionIndexer
  D-Bus slots and MPRIS slot before uploading.
- [ ] Test plain GNOME without an AppIndicator extension, KDE and Wayland.
  Ubuntu GNOME with its AppIndicator extension is the tested tray environment.
- [ ] Revisit MPRIS OpenUri's legacy radio semantics for local files.
- [x] MPRIS advertises an escaped local artwork URI only when the cache file
  exists. Missing and unknown artwork is omitted; regression cases passed.
