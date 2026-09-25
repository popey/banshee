# Internet service restoration

The 2.6.2 preservation build carries four additional patches, applied to the
unchanged vendored tarball. This work does not change the upstream version,
confinement or base. Builds use `snapcraft --use-lxd`; artifacts and logs retain
their fixed filenames.

`internet-archive-api.patch` moves Archive requests to HTTPS, removes the
rejected `xmlsearch` parameter, and requests JSON explicitly. Item details use
`/metadata/{identifier}`. The reader accepts both current scalar/list metadata
and the old cached JSON layout, handles current file arrays and reviews,
excludes private files, and escapes each download path component. API error
objects are errors even when the server returns HTTP 200. Missing download
statistics are omitted from the view.

`lastfm-api.patch` moves API and authorization requests to HTTPS. Write requests
send their signed parameters in the form body. Diagnostic request formatting
redacts session keys and authorization tokens. Error handling accepts JSON
whitespace and extracts XML error codes correctly. Scrobble batches are limited
to 50 entries. Failed or unrecognized responses retain the queue and wait before
retrying. A failed now-playing update releases its state for the next track.
An authorized, enabled scrobbler starts its saved queue during application
initialization; signed-out accounts do not start scrobble or now-playing
requests.

The recommendation and user-data readers now use API 2 XML responses, retaining
the existing typed UI models. Supported paths include similar artists, top
albums/tracks, recent/loved tracks and user charts. Unsupported legacy methods
fail explicitly instead of calling API 1.0. Historical Last.fm radio remains a
separate, unvalidated feature.

`snap-browser-launch.patch` routes links through snapd’s existing desktop
launcher D-Bus API, so browser authorization works from strict confinement.
Browser launch errors omit URL query strings, which may contain tokens.

`hyena-json-numbers.patch` preserves 64-bit JSON integers, fixing negative file
sizes caused by Int32 overflow while keeping small integers boxed as Int32.

The historical Banshee application key was accepted by public artist/user
lookups and the token/signature handshake.
The account test below also confirms browser authorization and real scrobbling.
Establish who maintains the application registration before publishing; working
historical credentials do not settle that maintenance question.

## Validation

`tests/internet-services-probe.cs` exercises the compiled assemblies. Its default
mode uses local fixtures and a loopback HTTP listener; it does not read the user's
library or submit scrobbles. The `live` mode checks Archive search/details,
Last.fm public artist/user data, and token/signature handling without authorizing
an account or submitting listening history. Account authorization, persistence, now-playing, real scrobbling, offline
replay and sign-out were subsequently tested with the consenting account
provided by the user; see the account validation below.

The initial 22 fixture checks and six live API checks passed against the
installed strict snap, local revision x13, on 2026-09-11. The account follow-up
build x15 passes 24 fixture checks, including signed-out submission guards. The app displayed the public-domain
LibriVox item **Alice Dugdale**, with ten MP3 chapters, and streamed chapter one.
An empty search displayed “No matches.” A real timeout displayed “Try Again”
and recovered on retry. The opened item survived the snap refresh.

The direct chapter download delivered 15,307,325 bytes and parsed as a complete
1,098.94-second MP3. This was a URL/host download check: the upstream Archive
view offers playback but does not implement an in-app file download action.
Item artwork also remains a follow-up: `ImageUrl` is repaired but the upstream
view does not consume it, and playback can still advertise a missing local
artwork file through the existing MPRIS behavior. Search subscriptions were not
tested. Do not advertise these as restored features.

The final GUI also displayed Erasure recommendations, top albums and top tracks
in View → Context Pane; `docs/banshee.png` captures it. Local playback passed.

Last.fm public artist/user data and token/signature requests passed from the
installed snap. Those standalone probes do not authorize accounts or submit scrobbles.
The historical radio extension remains outside this validation.

See `VALIDATION.md` for build/runtime evidence.

## Consenting-account validation

Using the credentials supplied by the user, Firefox login and Banshee browser
authorization succeeded. The session persisted across restart. Last.fm reported
real now-playing and accepted three Erasure tracks, each once: “Lay All Your
Love On Me”, “S O S”, and “Take A Chance On Me”.

The third track finished with only Banshee’s network interface disconnected.
The pending scrobble survived failed requests and application restart. Testing
exposed a missing startup call; the final build starts an authorized, enabled
saved queue during initialization. It uploaded that preserved play while the
player was stopped, without another song being played. The queue became empty.

Logout cleared the stored session and username. An eight-second signed-out
playback sample produced neither a now-playing update nor another scrobble.
The account was then reauthorized, network access restored, and playback left
paused. `removable-media` is already configured; its optional manual connection
command is in the README.

The build remains version `2.6.2`, installed as local revision x15. Credentials
were entered from `.env` through stdin, not command-line values, and were not
included in recorded test output. `.env` is ignored by git. See `VALIDATION.md`
for the fixed artifact checksum and test details.

## API references

- [Archive metadata read API](https://archive.org/developers/md-read.html)
- [Last.fm desktop authentication](https://www.last.fm/api/authspec)
- [Last.fm scrobbling parameters and POST format](https://www.last.fm/api/show/track.scrobble)
- [Last.fm recent tracks](https://www.last.fm/api/show/user.getRecentTracks)

Keep these compatibility changes separate when evaluating 2.9.1. Its later
release date does not establish that the service URLs and response formats are
compatible with today's APIs; test the same fixtures against that branch.

Artwork follow-up: `artwork-reliability.patch` adds Last.fm album API 2 lookups
and Internet Archive item-image caching. See `artwork.md` for provider order,
MPRIS behavior and validation.
