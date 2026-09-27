# Banshee revival working rules

- Build snaps with `snapcraft --use-lxd`; never use destructive mode.
- Reuse the versioned artifact name `banshee_2.6.2_amd64.snap` and fixed log
  filenames; do not add build-counter suffixes.
- Before **every** Store upload, run `review-tools.snap-review` (also exposed as
  `snap-review`) on the exact snap artifact to be uploaded. Save output to
  `/tmp/banshee-snap-review.log`, inspect its exit status and findings, and
  record the artifact SHA-256 and review result. Rerun if the artifact changes.
- Resolve unexpected review findings before uploading. Document any findings
  covered by existing Store interface approvals; never silently ignore them.
- Build/runtime tests and successful Store processing do not replace this
  pre-upload local review.
