# Last.fm login failure handling — 2026-09-25

The login dialog previously displayed “Finish Logging In” even when acquiring
an authorization token or opening the browser failed. Account.RequestAuthorization
also reported success when the browser callback returned false.

The patched flow checks both operations, displays an appropriate browser,
network or invalid-response error, and only offers Finish Logging In after a
successful browser launch. Retrying discards stale tokens. Session responses
are validated and the username returned by Last.fm is retained instead of
being overwritten by the name typed before authorization. The optional
streaming extension no longer has to exist to finish signing in.

Transport failures without an HTTP response now propagate to the caller;
HTTP error responses remain available for API error parsing. Authorization
exception logs contain only error types/status, not tokens or response data.

## Validation

- Built successfully with `snapcraft --use-lxd`.
- Installed locally as revision x2, version 2.6.2.
- 63 regression checks passed: 24 service, nine podcast transfer, 14 XDG
  folder and 16 authorization checks.
- Three installed-snap GTK preferences checks passed: browser failure,
  network failure and successful retry.
- Six live API checks passed: Archive search/metadata, Last.fm recommendations,
  public user data, token acquisition and signed unauthorized-session rejection.
- The complete regression suite passed on rerun after packaging finished;
  an earlier concurrent run timed out on the podcast HTTP-404 fixture.

Artifact: `banshee_2.6.2_amd64.snap`.
SHA-256: `41549280fb91a2c2c2daeb55070fda7eac391357c91ac008bb841f5118a345a7`.
Local logs: `/tmp/banshee-build.log`, `/tmp/banshee-regression.log`.

The browser-error paths were tested with injected callbacks; the canonical
username was tested with a fixture session. This pass did not repeat real
browser consent or a full scrobble. Earlier successful scrobbling does not
establish why authorization failed on another desktop.

Published to Store stable as revision 2 on 2026-09-25 using the tested artifact
above. The upload completed successfully and reported the revision released.
Joey's exact login, Archive and video failures remain open pending reproduction
details. See [the investigation](lastfm-report-investigation.md).
