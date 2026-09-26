# Browser launcher activation — 2026-09-26

Joey reported Ubuntu 26.04 LTS with Firefox installed as a snap. Clicking the
Last.fm authorization button displayed “Could not launch URL …
NullReferenceException” followed by the old Preferred Applications advice.

The snap-specific browser path used dbus-sharp's `GetObject` followed by
`launcher.OpenURL`. In this library GetObject returns null when the service
name is not currently owned; it does not activate the service. Consequently,
a desktop where `io.snapcraft.Launcher` had not yet started hit a null reference
before attempting to launch Firefox. Revision 2 fixed handling of this failure
in the login dialog but did not fix service activation.

Browser.Open now calls StartServiceByName before obtaining the proxy. This
starts the D-Bus-activatable snap user service, or succeeds if it is already
running. A missing proxy is also handled explicitly rather than dereferenced.
The existing desktop interface allows service activation; no new plugs or
confinement changes are required.

## Evidence

An isolated private-session-bus fixture in the core22 LXD builder reproduced
failure with the previous Browser.cs. With the patched file, the same fixture
passed all three checks: initially stopped service, already running service,
and complete dummy authorization URL delivered exactly once per invocation.
The test compiles real Browser.cs with the build's runtime dependencies.

This reproduces a concrete cause matching Joey's exception. Confirmation on
his desktop remains outstanding. His Archive and video reports are separate.
No Last.fm credentials or real authorization tokens are used by this fixture.

The misleading generic Preferred Applications wording is inherited from the
old browser error dialog; users do not need to find a Banshee browser setting.

## Completed validation

- `snapcraft --use-lxd` completed successfully.
- All 66 regression checks passed against the completed build.
- Installed locally as revision x3.
- The installed Banshee.Services assembly opened the public Last.fm API page
  through the real desktop launcher under snap confinement; Firefox's window
  title confirmed `API Docs | Last.fm`.
- Artifact: `banshee_2.6.2_amd64.snap`.
- SHA-256: `d56461467c674729aa29e213e7f67edd7c9733c6e6e6aad697b58950a04a7c64`.
- Logs: `/tmp/banshee-build.log`, `/tmp/banshee-regression.log`.

The private bus tests cover a stopped launcher without stopping the user's
real desktop service. The desktop check confirms the installed implementation
and confinement permissions; it does not repeat account consent or scrobbling.
Store stable remains revision 2 without this activation fix. Publication and
confirmation on Joey's machine are outstanding.
