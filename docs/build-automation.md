# Build automation

`.github/workflows/snap.yml` builds `revival/2.6.2` pushes, pull requests and
manual dispatches on GitHub's Ubuntu 24.04 runner. It uses the official
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
failure has been requested; Actions does not require that connection. Our token
can read workflows and write the source branch, but querying repository Actions
policy settings returned HTTP 403. If Actions is disabled for the fork, the
repository owner must enable it in the GitHub Actions UI.

Run the same wrappers locally after an LXD build. If multiple Banshee builder
containers exist, set `BANSHEE_BUILDER` to the one belonging to that build.
