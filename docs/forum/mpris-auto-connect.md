# Auto-connect request for banshee:mpris

Hi folks,

I'd like to request approval and auto-connection for the `mpris` slot in the `banshee` snap.

I've been reviving Banshee, the old Mono-based music player, as a snap. A little nostalgia, but also a useful music player :)

```yaml
name: banshee
description: >-
  An unofficial community revival of Banshee 2.6.2, the Mono-based music
  player, with local music playback, podcasts, Last.fm and Internet Archive
  support.
snapcraft: Not publicly available yet; public source and packaging repository planned.
upstream: https://gitlab.gnome.org/Archive/banshee
upstream-relation: >-
  Independent snap maintainer and publisher (popey), maintaining a community
  preservation build with compatibility fixes. Not representing the original
  upstream project.
slots:
  mpris:
    attributes:
      name: banshee
    request-type: auto-connection
    reasoning: >-
      Banshee provides org.mpris.MediaPlayer2.banshee on the session bus.
      This exposes playback controls and track metadata to desktop media
      controls and clients such as playerctl. I'd like this standard music
      player integration to work out of the box when people install the snap.
```

The uploaded build is version `2.6.2`, revision `1`, for amd64 with strict confinement.

The Banshee app uses this slot to provide `org.mpris.MediaPlayer2.banshee` on the session bus. It exposes playback controls and track metadata so desktop media controls and tools such as `playerctl` can interact with the player. We've tested the MPRIS controls locally, including providing local artwork URLs for desktop integration.

I'd like that integration to work out of the box when people install the snap. It seems a straightforward fit for a music player, along the same lines as the [recent Sidra request](https://forum.snapcraft.io/t/auto-connect-request-for-sidra-mpris/51333).

I now own the previously reserved `banshee` snap name. Revision 1 has been uploaded with a request to release to stable, but is awaiting manual review for interface connection rules. There are no published revisions yet. I'm also making a separate request for Banshee's two application-specific D-Bus names.

Could the reviewers approve the MPRIS slot and auto-connection, please?

Thanks!
