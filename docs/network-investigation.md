# Network investigation — 2026-09-09

Installed baseline: Banshee `2.6.2-snap4`, strictly confined.
This is diagnosis and prioritization; the installed snap does not yet contain
a trust-store fix or the Archive request changes.

## Shared certificate failure — confirmed

The staged `/etc/mono/certstore` is empty, and no populated machine Mono
trust store is bundled at `/usr/share/.mono`. Ubuntu normally fills these
stores via certificate synchronization hooks, which do not run when
Snapcraft extracts stage packages.

Compiled `tools/network-probe.cs` using Mono in the existing LXD builder and
ran it with the installed snap's Mono and native libraries under
`snap run --shell banshee`. This uses HttpWebRequest like the integrations,
with normal TLS certificate validation and no authentication credentials.

| Request | Installed runtime | Isolated populated user trust store |
| --- | --- | --- |
| Linux Matters RSS | CERTIFICATE_VERIFY_FAILED | HTTP 200, application/xml |
| Archive advanced search, current query | CERTIFICATE_VERIFY_FAILED | HTTP 200, application/json, search results |
| Last.fm API 2.0, request without API key | CERTIFICATE_VERIFY_FAILED | HTTP 400, application error rather than TLS failure |

For the isolated test, copied Ubuntu's populated Mono certificate store from
the LXD builder to an ignored workspace directory and selected it through a
separate XDG_CONFIG_HOME (`.mono/new-certs/Trust`). The application user's
trust store/settings were not modified. Setting only MONO_CFG_DIR to a copy
of `/etc/mono` did not resolve trust: Mono's X509 stores use application-data
paths. A production fix must populate **and expose the effective store**.

Mono documents `cert-sync` for importing system CA roots:
https://www.mono-project.com/docs/about-mono/releases/4.8.0/
Store path implementation:
https://raw.githubusercontent.com/mono/mono/main/mcs/class/Mono.Security/Mono.Security.X509/X509StoreManager.cs

## Linux Matters

https://linuxmatters.sh/episode/index.xml fetches successfully outside the
snap and parses as RSS with channel title `Linux Matters` and 90 items at
measurement time. It also fetches inside snap confinement using the isolated
populated trust store. This establishes the TLS failure, not complete
compatibility with Banshee's RSS parser, enclosure downloads, or playback.
Those are the next GUI acceptance tests.

## Internet Archive

Banshee's `InternetArchive/Search.cs` uses HTTP and appends `fmt=json` and
`xmlsearch=Search`. An HTTPS request with the legacy parameters returns
HTTP 200 but this JSON error:

```
[UNSUPPORTED_VALUE] A requested parameter has an inappropriate value Search for request parameter xmlsearch
```

Using `https://archive.org/advancedsearch.php?q=linux&output=json&rows=1`
returns a normal responseHeader/response object and search results. Therefore
there are at least two issues: TLS trust and outdated query parameters.
The details implementation also uses an old `details/{id}&output=json`
endpoint and must be validated separately.

## Last.fm

The code already uses API 2.0 for `auth.getToken`, `auth.getSession`,
`track.updateNowPlaying`, and `track.scrobble`. Core request and authorization
URLs are HTTP. Older auxiliary data code also references API 1.0.

The unauthenticated HTTPS diagnostic returns Last.fm error 6 (missing required
parameter) after TLS is fixed. This establishes reachability only. Historical
Banshee API credentials, user authorization, scrobble acceptance, and radio
have not been validated. The project needs an API-credential ownership plan
and an authorized account test; no account writes were performed here.

Current public documentation still includes authentication and Scrobbling 2.0:
https://www.last.fm/api/authspec
https://www.last.fm/api/scrobbling

## Reproducing the diagnostic

Compile `tools/network-probe.cs` with `mcs` in LXD. The resulting executable
is a temporary diagnostic, not part of the snap. Run using the installed
snap's `usr/bin/mono`, `MONO_CFG_DIR=$SNAP/etc`, and its `usr/lib` and
multiarch native-library paths. Pass public URLs as arguments. A missing-key
Last.fm request intentionally yields a nonzero result even when TLS works.


## Packaged fix and runtime follow-up (2026-09-10)

Superseding the initial isolated-store experiment: snap5 packages an Ubuntu
CA bundle and cert-sync-generated legacy/new Mono stores, exposed at
`/usr/share/.mono` using a layout. Valid HTTPS now works inside the installed
snap, and expired/self-signed/wrong-host certificates remain rejected.
Snap6 additionally fixes revision-specific library folders. Linux Matters
subscribe/refresh, one episode download, playback/seek, and restart persistence
passed. See VALIDATION.md for exact evidence and outstanding failed-download
retry/direct-streaming limitations. Archive API repair and Last.fm account
integration remain outstanding; TLS success alone does not restore those apps.


## Podcast recovery and native streaming (snap7)

The remaining streaming failure was GIO proxy-portal rejection, not the
Mono certificate store. Selecting the direct resolver with bundled TLS module
and CA paths restored HTTPS streaming and seeking; native invalid-certificate
checks still fail correctly. Failed downloads were silently excluded by
FeedEnclosure.AsyncDownload's status guard. Fixed that guard and download
response integrity/restart handling. Real GUI recovery, a controlled HTTP 404
retry, and refresh persistence passed; see the snap7 section of VALIDATION.md.
