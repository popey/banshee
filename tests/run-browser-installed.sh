#!/bin/sh
# Desktop smoke test: opens Last.fm API documentation in the real browser.
set -eu
: "${SNAP:?Run this inside the installed snap shell}"
export MONO_CFG_DIR="$SNAP/etc" MONO_GAC_PREFIX="$SNAP/usr"
export MONO_PATH="$SNAP/usr/lib/banshee:$SNAP/usr/lib/banshee/Extensions"
for assembly_dir in "$SNAP"/usr/lib/cli/*; do
    [ ! -d "$assembly_dir" ] || MONO_PATH="$MONO_PATH:$assembly_dir"
done
export LD_LIBRARY_PATH="$SNAP/usr/lib/x86_64-linux-gnu:$SNAP/usr/lib"
exec "$SNAP/usr/bin/mono" "$(dirname "$0")/browser-installed-probe.exe"
