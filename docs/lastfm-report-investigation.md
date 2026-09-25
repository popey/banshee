# Last.fm login report investigation

Investigated 2026-09-25 following [Joey Sneddon's article](https://www.omgubuntu.co.uk/2026/09/banshee-music-player-snap-ubuntu).
It reports inability to log in, an Internet Archive error and unsuccessful
video playback, without exact errors or reproduction steps. This is not a
confirmed scrobbling-only or historical-radio failure.

## Evidence

- Six live API checks passed against the installed Git build.
- All six also passed with Mono paths pointing to retained Store revision 1
  inside the current snap shell: Archive search/metadata, Last.fm artist/user
  data, token acquisition and signed session requests. The deliberately
  unauthorized token was rejected as expected. This tests Store assemblies,
  not a clean installation on Joey's machine.
- Reproduced a defect against Store revision 1: supplied a
  `Lastfm.Browser.Open` delegate returning false, then called
  `Account.RequestAuthorization`. The failing callback was invoked, yet the
  method returned `StationError.None` (success). No browser opened or session
  was saved. Diagnostic source: `/tmp/banshee-auth-failure-probe.cs`.
- `LastfmPreferences.OnSignInClicked` enters the authorization-pending state
  before requesting a token and ignores the returned error. Token/network
  failures can leave misleading instructions too.
- After `FetchSessionKey` succeeds, `OnFinishSignInClicked` overwrites the
  API-returned username with the pre-login entry. A blank, mistyped or different
  name can disagree with the authenticated session. This was found by source
  inspection, not reproduced through the GUI in this pass.

Files: `src/Libraries/Lastfm/Lastfm/Account.cs`,
`src/Extensions/Banshee.Lastfm/Banshee.Lastfm/LastfmPreferences.cs`, and
`src/Core/Banshee.Services/Banshee.Web/Browser.cs`.

These findings do not establish which failure Joey encountered. Successful
API probes do not prove the browser handoff works on another desktop.

## Next checks

Obtain Ubuntu version, snap revision, exact login error, whether the browser
opened, whether authorization succeeded there, and what happened after Finish
Logging In. Obtain the Archive query/item and video codec separately.
Only relevant redacted log excerpts are needed; do not share config.xml,
authorization URLs, session keys or tokens.

The confirmed login defects have now been fixed and validated in a local build;
see [the fix and validation record](lastfm-login-fix.md). Store stable remains
revision 1. These fixes do not establish the cause of Joey's reported failures.
