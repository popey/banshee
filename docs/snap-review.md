# Reproducible local review

## Diagnosis — 27 September 2026

Stable revision 3's SHA-256 is
`d56461467c674729aa29e213e7f67edd7c9733c6e6e6aad697b58950a04a7c64`.
The core24 review-tools snap's default run reported a SquashFS repack checksum
mismatch, plus three declaration findings for already-approved interfaces.

The default reviewer packs with SquashFS 4.6.1. The core22 builder's native
mksquashfs is 4.5. Although Snapcraft also bundles a 4.6.1 binary, its packing
code invokes `snap pack`, and its PATH includes native `/usr/bin` rather than
its bundled `usr/bin`. Comparing bundled version labels alone was misleading.

A controlled unpack/repack of the exact published artifact in LXD used native
4.5, xz, the original filesystem timestamp, and the reviewer's standard options
(`-noappend -all-root -no-xattrs -no-fragments`). It produced **exactly the
original SHA-256**, proving byte-for-byte reconstruction. The 4.6.1 diagnostic
repack differed in six directory-size entries in its listing, with equal
archive sizes. The discrepancy is archive serialization, not a Banshee source
or runtime change.

We then ran the **full unmodified official reviewer**, version
`0.48+20260921-1827UTC`, using the builder's native Python and tools. We supplied
the `slots` section of Banshee's current signed Store declaration (revision 1,
timestamp `2026-09-25T06:22:47.988854Z`). Result: **pass, exit code 0**.
`SNAP_FAKEROOT_RESQUASHFS=1` remained enabled and the resquash check was not
disabled. This supersedes the unresolved status in the historical release log.
No replacement artifact or Store upload was necessary.

## Required pre-upload command

After `snapcraft --use-lxd`:

```sh
tools/review-snap.sh banshee_2.6.2_amd64.snap
```

The script selects the sole Banshee container in the `snapcraft` LXD project.
If more than one exists, explicitly set `BANSHEE_BUILDER` to the builder for
this artifact. It starts a stopped builder, copies the exact artifact, fetches
the signed Store declaration with `snap known --remote`, and installs the
official review-tools snap in the builder if needed. It invokes that snap's
unchanged Python reviewer with native tools, rather than entering its core24
runtime. Python compatibility or missing dependencies fail the check.

The host needs Python 3/PyYAML, snapd and LXD access. Logs and evidence use
fixed paths under `/tmp`: `banshee-snap-review.log`, `banshee-snap.sha256`,
`banshee-store-declaration.assert`, and `banshee-review-slots.json`.
The wrapper propagates every review failure and verifies the artifact did not
change during review. The declaration allows only the slots approved for this
snap; there is no blanket interface allow-list or checksum waiver.

Always rerun after rebuilding or replacing the artifact. A build, runtime test,
previous artifact's review, or Store acceptance does not replace this check.
