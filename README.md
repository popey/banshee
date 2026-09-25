# Banshee revival: 2.6.2

An unofficial preservation of Banshee 2.6.2 for the Snap Store. This branch
contains the actual patched source, snap packaging and regression checks.
The original upstream README remains in `README`.

## Build

```sh
git clone --branch revival/2.6.2 https://github.com/popey/banshee.git
cd banshee
snapcraft --use-lxd > /tmp/banshee-build.log 2>&1
```

Use LXD, never destructive mode. The artifact is
`banshee_2.6.2_amd64.snap`. Hyena is vendored at the upstream release's pinned
revision; no submodule fetch is needed. Snapcraft builds the checked-out source
and does not unpack a release tarball or reapply a patch series.

The strict amd64/core22 snap bundles Mono, GTK# 2 and GStreamer. It restores
podcast downloads and streaming, Last.fm scrobbling, Internet Archive search
and playback, artwork, MPRIS and tray integration. First-run music selection
imports in place after confirmation. Artwork and podcast downloads use
SNAP_USER_COMMON; settings and the library database use SNAP_USER_DATA.

Optional external-drive access: `snap connect banshee:removable-media`.
Optical-disc/device-sync support and retired services are not promised.

## Source and attribution

Based on upstream tag `2.6.2`, commit
`4721061828e7d0de89c649fabfaae4135bb49e50`. Upstream master and tags are preserved.
See [Hyena provenance](src/Hyena/REVIVAL-PROVENANCE.md),
[patch-to-commit mapping](docs/provenance/patch-commits.json), and
[Ubuntu copyright manifest](docs/provenance/ubuntu-copyright).
The first ten compatibility patches came from Ubuntu's
`banshee_2.9.0+really2.6.2-7ubuntu3.debian.tar.xz`; original authors and dates
are retained in Git. Local fixes and their provenance are separate commits.
Original license notices remain alongside the source.

## Validation and roadmap

See [tests](tests/README.md), [roadmap](TODO.md), and
[publication plan](docs/source-publication.md). Historical validation documents
describe the earlier tarball-based builds; they are not evidence that a new
Git-based build has passed. Git migration results are recorded separately.
A later `revival/2.9.1` branch will start from its own upstream release tag.
