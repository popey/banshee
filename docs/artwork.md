# Automatic artwork restoration

The 2.6.2 artwork pipeline now keeps embedded pictures and album-folder images
first. Tagged MusicBrainz release IDs address Cover Art Archive's HTTPS
`/release/{mbid}/front-500` endpoint directly. Other missing album covers fall
back to Last.fm `album.getInfo`, with artist/album parameters encoded once and
API 2 image sizes parsed explicitly. The Rhapsody provider and MusicBrainz v1
ASIN/link scraping are no longer used by the automatic pipeline.

The background album scanner carries the album's MusicBrainz release ID into
its lookup job. Existing cached artwork is preserved; audio files and their
tags are not modified. Last.fm album metadata is public API data and does not
require the listener to sign in. Application-key maintenance remains a Store
release task.

Internet Archive item details fetch the item thumbnail on their worker thread,
into a separate `internet-archive-<digest>.jpg` cache entry. That entry is shared
by the item view and its playback tracks. A failed cover request does not stop
item details or playback. Reopening the item retries missing artwork.

MPRIS includes `mpris:artUrl` only when the cache file exists, using a properly
escaped local file URI. This applies to all tracks that use Banshee's MPRIS
metadata builder, including podcasts and Archive playback. Missing covers are
omitted, so desktop clients can use their fallback icon.

Remote cover downloads have request/read timeouts and an 8 MiB limit. Empty,
HTML/JSON, truncated and unsupported image responses are rejected before the
cache entry is committed. The byte checks are a lightweight format check, not
a complete image decoder. Failed writes must not report a successful download.

This does not invent covers for unknown albums or guarantee a match for every
edition/bootleg. The previous one-off recovery is recorded separately in
`library-refresh.md`; it does not prove the automatic providers work.

## Validation

`tests/artwork-probe.cs` exercises the built assemblies with an isolated cache,
including escaped names, API 2 image sizes, malformed/oversized downloads,
cache preservation, local MPRIS URIs, provider order and Archive identities.
Its `live` mode calls the actual artwork jobs for Last.fm, a tagged Cover Art
Archive release, and an Internet Archive item. Run the compiled probe inside
the installed snap with `tests/run-artwork.sh`, adding `live` for network tests.

## API references

- [Last.fm album.getInfo](https://www.last.fm/api/show/album.getInfo)
- [Cover Art Archive API](https://musicbrainz.org/doc/Cover_Art_Archive/API)

## Results — September 11–12, 2026

The final LXD build installed as local revision x16, version 2.6.2. All 18
artwork fixtures and the existing 24 service regressions pass inside the strict
snap. All four live artwork tests passed in LXD on September 11. On September
12, the installed snap again downloaded Last.fm and Cover Art Archive covers,
but `archive.org/services/img/alice_dugdale_2006_librivox` returned HTTP 502.
A host curl request independently reproduced that error, and the item's metadata
endpoint returned `{}`. A second real audiobook's thumbnail also returned 502;
Archive search still returned these items. This is recorded as an external
service/item availability limitation, not a successful installed-snap Archive
image test. No certificate checks were bypassed.

In the normal Banshee library UI, a backed-up Heligoland cache entry was removed
and Banshee automatically regenerated its 300×300 cover (170,641 bytes) when
opening another album track for playback. The album browser and player header
show the cover; MPRIS reports an existing local file URI. The downloaded Last.fm
PNG and Cover Art Archive JPEG were independently decoded successfully. The
user's library still has 3,256 music files, zero missing imports/duplicates and
one untitled Erasure album group without artwork. Screenshot: `banshee-artwork.png`.

The background scanner's existing failed-album retry scheduling remains a
follow-up; this change restores provider lookup on import/playback and carries
release IDs into scan jobs. Edition/bootleg identification remains separate.
