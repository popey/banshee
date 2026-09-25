# First-run music selection

The snap offers an “Add your music” dialog once for an empty music library.
It suggests the real desktop's XDG music directory when configured, including
localized names and custom absolute locations. It does not scan on launch.

- **Add music** remembers the selected library directory and imports its files
  in place. Copying and automatic renaming are disabled for this initial import.
- **Choose another folder…** opens a local folder chooser. Cancelling the chooser
  returns to the welcome dialog without importing.
- **Not now**, or closing the welcome dialog, dismisses setup persistently.
  The existing Media → Import Media action remains available later.
- Existing populated libraries skip setup automatically. Refreshes retain the
  saved decision and library location.

The helper is snap-only and reads `SNAP_REAL_HOME/.config/user-dirs.dirs` as
configuration data. It never executes that file. `$HOME` in its Music setting
means the real home, not the snap's private data directory. There is no guessed
English `Music` fallback, and a Music setting equal to the home directory is
considered disabled. Without a usable suggestion, users choose a folder.

Confinement still applies to the chosen folder. External storage needs a mounted
drive, suitable filesystem permissions and the removable-media connection.
Unreadable selections show guidance rather than starting an import. Arbitrary
mount locations and desktop virtual network URLs are not promised; the chooser
selects local filesystem paths, including accessible mounted network storage.

Implementation: `patches/snap-music-setup.patch`, applied after the existing
patch series. This adds a small Nereid helper; it does not change global HOME,
move media, or change the user's desktop folder configuration.

## Validation — 2026-09-12

Built with `snapcraft --use-lxd` and installed as local revision x3, version
2.6.2. Artifact: `banshee_2.6.2_amd64.snap`, SHA256
`aeb0139aa621fd61de3dd206214cb8d1746581120ec18c36980a5e0e242daf09`.

- All 14 parser cases passed against the compiled Nereid assembly in LXD.
- Fresh confined profiles correctly suggested `Musique # collection` from a
  synthetic desktop XDG setting, with zero tracks imported before confirmation.
- Cancelling the folder chooser returned to setup. Not now saved dismissal;
  relaunch showed no welcome dialog and still contained zero tracks.
- Confirming the suggestion imported one fixture track from its original path,
  correctly escaping spaces and `#` in its URI.
- Choosing a different folder did not import until Add music was pressed.
  A controlled filesystem permission denial showed guidance. Restoring access,
  closing the error and retrying imported successfully. A GTK error-dialog
  destruction issue found during testing was fixed and retested.
- The selected path and imported track persisted across relaunch, with copying
  and renaming disabled and no repeat welcome dialog.
- The real profile retained 3,256 music tracks, six podcast subscriptions and
  `/home/minnie/Music`; setup was skipped for its populated library.

[Welcome dialog screenshot](banshee-music-setup.png) now shows the actual
clean-install suggestion. USB hardware and arbitrary NAS mount configurations
were not exercised by this change.

## Full purge and reinstall verification

At the user's request, removed Banshee with `snap remove --purge banshee` and
confirmed its entire user-data directory disappeared while the original Music
directory remained. Reinstalled the same verified artifact as revision x1.

The real first-run dialog suggested `/home/minnie/Music`, with zero tracks and
zero podcast subscriptions before confirmation. Clicking Add music imported
3,256 tracks, all referencing existing original files, with no duplicate URIs.
Copying and renaming remained disabled. After quitting and relaunching, all
3,256 tracks persisted and the setup dialog did not reappear.

The purge cleared previous podcast subscriptions, downloads and Last.fm settings.
They were not restored as part of this first-run test. The earlier Store
screenshots remain available as captured artifacts.
