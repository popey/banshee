#!/bin/bash
# Called by review-snap.sh inside the Snapcraft LXD builder.
set -euo pipefail
if ! snap list review-tools >/dev/null 2>&1; then
    snap install review-tools
fi
review_root=/snap/review-tools/current
review_sites=("$review_root"/lib/python*/site-packages)
if [[ ${#review_sites[@]} != 1 || ! -d "${review_sites[0]}/reviewtools" ]]; then
    echo 'Cannot locate the official review-tools Python package' >&2
    exit 1
fi
# Use the official, unmodified reviewer with native Python and SquashFS.
# Running through the snap launcher would substitute its core24 SquashFS 4.6.1
# for the core22 builder's 4.5, producing a different archive serialization.
export PATH=/usr/sbin:/usr/bin:/sbin:/bin
export PYTHONPATH="${review_sites[0]}:$review_root/usr/lib/python3/dist-packages"
export SNAP_FAKEROOT_RESQUASHFS=1
# Never inherit a setting that disables the checksum or fragment checks.
unset SNAP_ENFORCE_RESQUASHFS
snap list review-tools
mksquashfs -version
sha256sum /root/banshee-review.snap
exec /usr/bin/python3 "$review_root/bin/snap-review" \
    --slots /root/banshee-review-slots.json /root/banshee-review.snap
