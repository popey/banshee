#!/bin/sh
# Compile actual Browser.cs against built dependencies; no host desktop/account.
set -eu
build_dir="${1:?Usage: run-browser-launch.sh /path/to/build [Browser.cs]}"
test_dir="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
browser_source="${2:-$build_dir/src/Core/Banshee.Services/Banshee.Web/Browser.cs}"
probe_dir=/tmp/banshee-browser-probe
mkdir -p "$probe_dir/data/dbus-1/services"
export MONO_CFG_DIR=/etc MONO_PATH="$build_dir/bin"
mcs -r:Mono.Posix -r:/usr/lib/cli/dbus-sharp-2.0/dbus-sharp.dll \
    -r:"$MONO_PATH/Hyena.dll" -r:"$MONO_PATH/Banshee.Core.dll" \
    -r:"$MONO_PATH/Banshee.Services.dll" \
    -out:"$probe_dir/browser-launch.exe" "$browser_source" "$test_dir/browser-launch-probe.cs"
cat > "$probe_dir/data/dbus-1/services/io.snapcraft.Launcher.service" <<SERVICE
[D-BUS Service]
Name=io.snapcraft.Launcher
Exec=/usr/bin/mono $probe_dir/browser-launch.exe serve
SERVICE
export BANSHEE_BROWSER_RECORD="$probe_dir/urls"
: > "$BANSHEE_BROWSER_RECORD"
export XDG_DATA_DIRS="$probe_dir/data" SNAP=/fixture/banshee
exec dbus-run-session -- mono "$probe_dir/browser-launch.exe"
