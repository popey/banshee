#!/bin/bash
# Run deterministic regressions against the completed LXD build.
set -euo pipefail
repo=$(cd -- "$(dirname -- "$0")/.." && pwd)
source "$repo/tools/lxd-builder.sh"
builder=$(banshee_builder)
banshee_start_builder "$builder"
lxc --project snapcraft file push --recursive "$repo/tests" "$builder/root/"
lxc --project snapcraft exec "$builder" -- sh /root/tests/run-built-regressions.sh \
    /root/parts/banshee/build > /tmp/banshee-regression.log 2>&1
cat /tmp/banshee-regression.log
