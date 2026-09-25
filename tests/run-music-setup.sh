#!/bin/sh
# Run with: snap run --shell banshee -c '/absolute/path/tests/run-music-setup.sh skip'
# Each case keeps its own profile in common; repeat a case to test persistence.
set -eu
: "${SNAP:?Run inside the installed snap shell}"
case "${1:-}" in skip|choose|suggest|error) setup_case=$1 ;; *) exit 2 ;; esac
setup_root="$SNAP_USER_COMMON/music-setup-test"
export SNAP_USER_COMMON="$setup_root/$setup_case/common"
export SNAP_USER_DATA="$setup_root/$setup_case/data"
export SNAP_REAL_HOME="$setup_root/desktop-home"
mkdir -p "$SNAP_REAL_HOME/.config" "$SNAP_REAL_HOME/Musique # collection"
printf 'XDG_MUSIC_DIR="$HOME/Musique # collection"\n' > "$SNAP_REAL_HOME/.config/user-dirs.dirs"
cp "$(dirname "$0")/../test-runtime/music-setup-media/tone.wav" "$SNAP_REAL_HOME/Musique # collection/tone.wav"
exec "$SNAP/bin/banshee-launch" --disable-dbus
