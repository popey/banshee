# Local revival branch validation

Validated 2026-09-25. Local repository: `revival/` within the preservation
workspace; branch `revival/2.6.2`, based on upstream tag `2.6.2`.
No remote push, default-branch change, installation or Store release performed.

## Commits and source

- Hyena vendored from the exact release-pinned commit, with provenance and
  original licenses. No Git submodules or obsolete submodule fetch remain.
- All 24 source patches imported separately. Original authors/dates retained
  for the ten distribution patches; local changes use an explicit Codex
  author identity. Each commit records the originating patch and SHA256.
- Application source matches the independently patched audit checkout.
- Packaging builds `source: .`, with no tarball extraction or patch replay.
- A fresh local clone contains Hyena and the snap launcher. The upstream
  `bin/` ignore rule needed an explicit launcher exception.
- Tracked-file audit found no `.env`, snap artifacts, compiled executables or
  runtime logs. A scan for common GitHub tokens/private keys found no matches.
- Whitespace checks report inherited Hyena, distribution-patch and copied
  forum-template whitespace; those originals were preserved rather than
  mixed with an unrelated formatting change.

## LXD build

`snapcraft --use-lxd` completed successfully in a new LXD instance:
`snapcraft-banshee-on-amd64-for-amd64-1860594854`.
The Git checkout regenerated configure/build files and compiled successfully.
All 1,933 committed application/build/recipe input files checked against the
builder's pulled source matched exactly.

Artifact: `banshee_2.6.2_amd64.snap` (approximately 125 MiB), strict core22 amd64.
SHA256: `3a71448a5897bbcd2d1bf311631f9c52f1967578d041e94be0c60ce48703e506`.
Build log: `/tmp/banshee-build.log`.

Snapcraft reported library-linter warnings and absent contact/issues/source-code/
website metadata. These remain follow-up packaging items; the build succeeded.
This artifact has not been installed or GUI-tested. The installed stable
revision 1 was not replaced. No binary reproducibility claim is made.

## Regression checks

Restarted the build container after Snapcraft stopped it, then ran
`tests/run-built-regressions.sh` against `/root/parts/banshee/build`:

- 24 service/API fixture checks passed.
- Nine podcast-download/recovery/error cases passed.
- 14 XDG music-folder parsing cases passed.

The fixture warnings deliberately exercise invalid sessions, malformed
responses and download failures. These checks submit no real scrobbles.
Regression log: `/tmp/banshee-regression.log`.

## Next steps

Review the local history and attribution, then publish the revival branch when
authorized. Before replacing the stable release, perform an installed desktop
smoke test of this Git-built artifact. Set the default branch and connect any
Store build automation only as subsequent explicit publication steps.

## Installed desktop smoke test

Completed later on 2026-09-25 on Ubuntu GNOME/X11, using the artifact and
SHA256 above. Installed over Store revision 1 as local revision x1, retaining
the existing profile. The Git-built snap is now the installed version.

- Started successfully with Yaru styling, readable fonts and toolbar icons.
- Retained 3,256 music tracks, six feeds, 3,415 podcast entries and both
  completed podcast downloads. SQLite integrity check returned `ok`.
- `tests/mpris-controls.py` passed play/pause/toggle, absolute and relative
  seek, volume, shuffle, repeat and PropertiesChanged signals.
- Local music playback worked; Last.fm's API confirmed now-playing reporting
  using the retained login. A full new scrobble was not tested in this pass.
- Linux Matters and Waveform downloads played to 9.056 and 8.676 seconds,
  respectively, with existing local artwork URLs. Download files remain in
  SNAP_USER_COMMON/Podcasts across the revision change.
- Internet Archive's Alice Dugdale chapter 1 streamed to 7.478 seconds.
  Its cover appeared in GNOME's media panel. The notification-area menu rendered.
- Six live Archive/Last.fm API checks passed using the installed assemblies.
- Quit and relaunched successfully; the library/subscriptions/downloads
  persisted and playback was left stopped.

No blocker was found in those flows. Optical-disc hardware services remain
unavailable. GTK/Hyena accessibility initialization emitted warnings when
opening podcasts; ordinary controls worked, but screen-reader operation was
not validated. This is recorded in the roadmap for investigation.

This pass did not repeat a purge/first-run test, a new podcast download, every
media format or external-drive testing. No Store release or default-branch
change was made. The issue-tracking investigation was added to TODO.md; GitHub
Issues itself was not enabled by this task.
