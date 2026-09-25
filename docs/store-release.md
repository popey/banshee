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
