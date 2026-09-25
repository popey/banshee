#!/bin/sh
# Run through snap run --shell banshee on a desktop, after compiling the probe.
set -eu
: "${SNAP:?Run this inside the installed snap shell}"
export MONO_CFG_DIR="$SNAP/etc"
export MONO_GAC_PREFIX="$SNAP/usr"
export MONO_PATH="$SNAP/usr/lib/banshee:$SNAP/usr/lib/banshee/Extensions"
for assembly_dir in "$SNAP"/usr/lib/cli/*; do
    [ ! -d "$assembly_dir" ] || MONO_PATH="$MONO_PATH:$assembly_dir"
done
export LD_LIBRARY_PATH="$SNAP/usr/lib/x86_64-linux-gnu:$SNAP/usr/lib"
export LANG=C.UTF-8 LC_ALL=C.UTF-8
exec "$SNAP/usr/bin/mono" "$(dirname "$0")/lastfm-login-ui-probe.exe" "$SNAP/usr/lib/banshee/Extensions/Banshee.Lastfm.dll"
