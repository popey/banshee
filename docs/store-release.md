# Store upload — 24 September 2026

Requested release: `latest/stable`, Banshee 2.6.2, amd64.

Changed only `grade: devel` to `grade: stable` in the packaging configuration
and rebuilt using `snapcraft --use-lxd`. The Nereid, Services, Podcasting,
Last.fm and Internet Archive assemblies and launcher matched the previously
validated build by SHA256.

Artifact: `banshee_2.6.2_amd64.snap`

SHA256: `9afbeab697cd95f3effce7f74296267335c765074e44ef8104ab2c9cc0cc679d`

Uploaded using `snapcraft upload banshee_2.6.2_amd64.snap --release=stable`.
The Store accepted the upload as revision **1**, then stopped release processing
with **will need manual review**. It reported three instances of:

> human review required due to 'deny-connection' constraint (interface attributes)

The CLI did not identify individual interfaces in these messages. Do not treat
this as a successful stable release: `snapcraft status banshee` confirmed there
were no released revisions after the upload. No interfaces were removed to
bypass review. Once review is approved, check channel status and release revision
1 to stable if the original release request has not been applied.

## Review requests

Posted by popey:

- [MPRIS auto-connection request](https://forum.snapcraft.io/t/auto-connect-request-for-banshee-mpris/53374/1)
- [Session D-Bus names approval request](https://forum.snapcraft.io/t/request-to-approve-banshees-session-d-bus-names/53375/1)

Posting the requests does not establish approval. Check the reviewers' responses
and Store revision/channel status before announcing availability on stable.

## Stable release confirmed — 25 September 2026

`snapcraft status banshee` now reports version 2.6.2, amd64, revision 1 on
`latest/stable`. `snapcraft revisions banshee` also confirms `latest/stable*`
for revision 1. The original release request took effect after review; no
re-upload or additional release command was needed.

## Browser activation release — 26 September 2026

Uploaded the tested browser activation fix and verified `latest/stable` maps
to revision **3**, version 2.6.2, amd64. Source fix: `228380665`.
Artifact SHA-256:
`d56461467c674729aa29e213e7f67edd7c9733c6e6e6aad697b58950a04a7c64`.
Runtime and regression evidence: [browser activation](browser-launch-activation.md).

### Mandatory local review and retrospective result

The request to always run local snap review arrived after this upload had
completed. `AGENTS.md` and README now require it before every subsequent upload
of the exact artifact; successful Store processing is not a substitute.

Retrospective `review-tools.snap-review` exited **2 (FAIL)**. A diagnostic rerun
with `SNAP_DEBUG_RESQUASHFS=1` also exited 2. Findings:

- `banshee-dbus`, `banshee-indexer`, and `mpris` deny-connection constraints:
  these interfaces have existing Store approvals (forum requests linked above);
  the Store accepted and released revision 3.
- `squashfs_repack_checksum`: unexpected and **unresolved**. Diagnostic archive
  listings differ only in six directory-size fields (12 removed/added lines).
  Listed file metadata is unchanged and both archives are 130932736 bytes.
  This does not prove file-content equality or identify the packing cause.
  The builder's system mksquashfs reports 4.5; the reviewer bundles 4.6.1.
  A tool-version effect is a hypothesis, not a confirmed explanation.

Full diagnostic output: `/tmp/banshee-snap-review.log`.
Extracted diff: `/tmp/banshee-snap-review-diff.log`.
Investigate and resolve the unexpected repack result before the next upload;
do not disable the check to obtain a passing result.

### Review failure resolved — 27 September 2026

The full official review passed (exit 0) using the core22 builder's native
SquashFS tools and the current signed Store declaration. A separate controlled
repack matched revision 3 byte-for-byte. No checks were disabled and the
artifact was unchanged. [Diagnosis and repeatable review](snap-review.md).
