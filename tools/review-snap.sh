#!/bin/bash
# Full official review, using the same native packing tools as the LXD build.
set -euo pipefail
repo=$(cd -- "$(dirname -- "$0")/.." && pwd)
source "$repo/tools/lxd-builder.sh"
artifact=$(realpath "${1:-$repo/banshee_2.6.2_amd64.snap}")
builder=$(banshee_builder)
banshee_start_builder "$builder"
sha256sum "$artifact" > /tmp/banshee-snap.sha256
snap known --remote snap-declaration series=16 \
    snap-id=pSJ9a0lVlORrbzCe5OTnjntmDPkcwGkb > /tmp/banshee-store-declaration.assert
python3 - <<'PY'
import json
from pathlib import Path
import yaml
assertion = yaml.safe_load(Path('/tmp/banshee-store-declaration.assert').read_text().split('\n\n', 1)[0])
if assertion.get('snap-name') != 'banshee' or assertion.get('snap-id') != 'pSJ9a0lVlORrbzCe5OTnjntmDPkcwGkb':
    raise SystemExit('Unexpected Store declaration')
Path('/tmp/banshee-review-slots.json').write_text(json.dumps(assertion['slots'], indent=2) + '\n')
PY
lxc --project snapcraft file push "$artifact" "$builder/root/banshee-review.snap"
lxc --project snapcraft file push /tmp/banshee-review-slots.json "$builder/root/"
lxc --project snapcraft file push "$repo/tools/review-in-builder.sh" "$builder/root/"
# Do not pipe this through tee: propagate review failure to callers/CI.
status=0
lxc --project snapcraft exec "$builder" -- bash /root/review-in-builder.sh \
    > /tmp/banshee-snap-review.log 2>&1 || status=$?
cat /tmp/banshee-snap-review.log
# Detect an accidental replacement of the host artifact during review.
sha256sum --check /tmp/banshee-snap.sha256
exit "$status"
