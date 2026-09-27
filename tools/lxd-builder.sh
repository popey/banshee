#!/bin/bash
# Shared functions; source this from the CI/review scripts.
set -euo pipefail
banshee_builder() {
    if [[ -n "${BANSHEE_BUILDER:-}" ]]; then
        printf '%s\n' "$BANSHEE_BUILDER"
    else
        lxc --project snapcraft list --format json | python3 -c '
import json,sys
names=[x["name"] for x in json.load(sys.stdin) if x["name"].startswith("snapcraft-banshee-")]
if len(names)!=1:
    sys.exit("Set BANSHEE_BUILDER: expected exactly one Banshee build container, found %d" % len(names))
print(names[0])'
    fi
}
banshee_start_builder() {
    local state
    state=$(lxc --project snapcraft list "$1" --format csv -c s)
    case "$state" in
        RUNNING) ;;
        STOPPED) lxc --project snapcraft start "$1" ;;
        *) echo "Unexpected builder state: $state" >&2; return 1 ;;
    esac
}
