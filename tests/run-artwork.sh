#!/bin/sh
# Invoke inside `snap run --shell banshee`, after compiling the adjacent probe.
set -eu
: "${SNAP:?Run this probe inside the installed Banshee snap shell}"
export MONO_CFG_DIR="$SNAP/etc"
export MONO_GAC_PREFIX="$SNAP/usr"
export MONO_PATH="$SNAP/usr/lib/banshee:$SNAP/usr/lib/banshee/Extensions"
for assembly_dir in "$SNAP"/usr/lib/cli/*; do
    [ ! -d "$assembly_dir" ] || MONO_PATH="$MONO_PATH:$assembly_dir"
done
export LD_LIBRARY_PATH="$SNAP/usr/lib/x86_64-linux-gnu:$SNAP/usr/lib"
export XDG_CACHE_HOME="$(dirname "$0")/../test-runtime/artwork-probe # cache"
export XDG_CONFIG_HOME="$(dirname "$0")/../test-runtime/artwork-probe-config"
exec "$SNAP/usr/bin/mono" "$(dirname "$0")/artwork-probe.exe" "$@"
