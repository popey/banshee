# Build automation

`.github/workflows/snap.yml` builds `revival/2.6.2` pushes, pull requests and
manual dispatches on GitHub's Ubuntu 24.04 runner. Documentation-only pushes
are skipped. It uses the official
Canonical build action pinned to a commit and explicitly passes `--use-lxd`.
The application still builds in its core22 LXD environment.

After building, the workflow restarts the builder and runs the 66 deterministic
regression checks with `tools/validate-lxd.sh`. The reviewer then checks the
exact artifact with `tools/review-snap.sh`, using the current Store declaration
and matching native packing tools. A failed check fails the workflow.

The `banshee-2.6.2-amd64` workflow artifact contains the snap, regression/review
logs, SHA-256 and Store declaration evidence. Snapcraft output is in the build
step's job log. The workflow requests read-only repository permission and has
no Store credentials or publishing step. Public service checks, browser consent,
real audio playback and scrobbling still require separate desktop validation.

The Snapcraft website's GitHub connection is a separate integration. Its exact
failure has been requested; Actions does not require that connection. Actions is enabled and the first complete run succeeded.

Run the same wrappers locally after an LXD build. If multiple Banshee builder
containers exist, set `BANSHEE_BUILDER` to the one belonging to that build.


## First complete validation — 27 September 2026

[Run 36344675673](https://github.com/popey/banshee/actions/runs/36344675673)
passed on commit `472034a12e228a87fc629ef99538f02cf92ed32b`: clean LXD build,
all 66 regression checks, full snap review and artifact upload.

[Download snap and evidence](https://github.com/popey/banshee/actions/runs/36344675673/artifacts/10940064136).
The snap's SHA-256 is
`e130907ab47155c5bf403b2dd0f649fccee096270979a368b7a1a17e53247641`.
This CI artifact has not been released to the Store.

The initial clean build caught an omitted Ubuntu patch input:
`src/Backends/Banshee.NowPlaying.X11/Banshee.NowPlaying.X11.dll.config`.
Upstream's `*.config` ignore rule excluded it from the earlier source import,
although it existed locally and was included in published builds. It is now
tracked unchanged from Chow Loong Jin's Ubuntu patch, with a narrow ignore
exception and a provenance note. No other ignored source/build/data files
were found in that audit.

Local wrapper validation also passed: 66 regressions and full review of the
unchanged stable revision 3 artifact, with its SHA-256 verified after review.
