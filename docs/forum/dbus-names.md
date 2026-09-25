# Request to approve Banshee's session D-Bus names

Hi folks,

Could I request a snap declaration allowing the `banshee` snap to own these two session-bus names, please?

- `org.bansheeproject.Banshee`
- `org.bansheeproject.CollectionIndexer`

I'm publishing the snap as `popey`. This is an unofficial community revival of Banshee 2.6.2, the Mono-based music player. I now own the previously reserved `banshee` snap name, and have uploaded revision 1 for amd64 with strict confinement.

These are Banshee's existing upstream names. The first is used for its application services and communication with the running player. The second is its collection-indexer service, which exposes information about Banshee's media library to clients.

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
  banshee-dbus:
    interface: dbus
    attributes:
      bus: session
      name: org.bansheeproject.Banshee
    request-type: manual-connection
    reasoning: >-
      This is Banshee's existing upstream session-bus name for application
      services and communication with the running player. The snap needs
      approval to own this name and provide these services under confinement.
  banshee-indexer:
    interface: dbus
    attributes:
      bus: session
      name: org.bansheeproject.CollectionIndexer
    request-type: manual-connection
    reasoning: >-
      This is Banshee's existing upstream collection-indexer service. It
      exposes information about Banshee's media library to clients. The snap
      needs approval to own this name and provide the service on the session
      bus.
```

Both slots are listed under the `banshee` app. The app also has an MPRIS slot, for which I'm making a separate approval/auto-connection request.

I uploaded with `--release=stable`, but the Store stopped processing with `will need manual review` and three occurrences of:

> human review required due to 'deny-connection' constraint (interface attributes)

The CLI output didn't identify the individual interfaces. There are no released revisions yet.

The [D-Bus interface documentation](https://snapcraft.io/docs/dbus-interface) describes requesting a declaration to claim a well-known name, and this looks like the same sort of request as the [earlier Diplo thread](https://forum.snapcraft.io/t/dbus-name-com-docsion-diplo-deny-connection-constraint-fails-in-automated-review/21944).

Could you review revision 1 and grant use of these two names for `banshee`, please? The request here is to provide Banshee's own services on the session bus.

Thanks!
