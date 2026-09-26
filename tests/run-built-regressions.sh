#!/bin/sh
# Run inside the LXD builder against a freshly compiled Banshee tree.
set -eu
build_dir="${1:?Usage: run-built-regressions.sh /path/to/banshee/build}"
test_dir="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
probe_dir=/tmp/banshee-regression-probes
mkdir -p "$probe_dir"
export MONO_CFG_DIR=/etc
export MONO_PATH="$build_dir/bin"
mcs -r:"$MONO_PATH/Lastfm.dll" -r:"$MONO_PATH/Banshee.InternetArchive.dll" \
    -r:"$MONO_PATH/Hyena.dll" -out:"$probe_dir/internet-services.exe" \
    "$test_dir/internet-services-probe.cs"
mono "$probe_dir/internet-services.exe"
mcs -r:"$MONO_PATH/Migo.dll" -out:"$probe_dir/podcast-download.exe" \
    "$test_dir/podcast-download-probe.cs"
python3 "$test_dir/podcast-downloads.py" "$probe_dir/podcast-download.exe"
mcs -out:"$probe_dir/music-folders.exe" "$test_dir/music-folders-probe.cs"
mono "$probe_dir/music-folders.exe" "$MONO_PATH/Nereid.exe"
mcs -r:"$MONO_PATH/Lastfm.dll" -r:"$MONO_PATH/Hyena.dll" \
    -out:"$probe_dir/lastfm-auth.exe" "$test_dir/lastfm-auth-probe.cs"
mono "$probe_dir/lastfm-auth.exe"
sh "$test_dir/run-browser-launch.sh" "$build_dir"
