This category is for handling requests for privileged interfaces, formerly known as *auto-connections*. The process is documented in [Process for aliases, auto-connections and tracks](https://forum.snapcraft.io/t/process-for-aliases-auto-connections-and-tracks/455).

To make the review of your request easier, please use the following template to provide all the required details and also include any other information that may be relevant.

---

```
name: name of the snap
description: description of the snap
snapcraft: link to snapcraft.yaml if publicly available
upstream: link to the upstream repository if open-source or ‘PRIVATE’ otherwise
upstream-relation: relation of the snap publisher with the upstream
plugs:
  <interface-name>:
    interface: [optional] one of https://snapcraft.io/docs/reference/interfaces/
    attributes: [optional] interface attributes if any
    request-type: installation | manual-connection | auto-connection
    reasoning: why is this interface needed
  ...
slots:
  <interface-name>:
    interface: [optional] one of https://snapcraft.io/docs/reference/interfaces/
    attributes: [optional] interface attributes if any
    request-type: installation | manual-connection | auto-connection
    reasoning: why is this interface needed
  ...
```

where:

*  **\<interface-name>**: the name of the interface as defined in the *snapcraft.yaml* . 
* **interface:**  the interface being requested. Only required if different from interface-name
* **attributes**: the interface attributes. Only required if attribute are defined in the *snapcraft.yaml.* 
* **request-type**: one of *installation* (ie. permission to upload the snap to the store using this interface), *connection* (ie. permission to allow the snap interface to be connected) or *auto-connection* (ie. permission for the snap interface to be automatically connected when the snap is installed).

---

### Example request:

```
name: foo-snap
description: audio player and recorder
snapcraft: https://github.com/jslarraz/foo/snapcraft.yaml
upstream: https://github.com/jslarraz/foo
upstream-relation: maintainer
plugs:
  audio-record:
    request-type: auto-connection
    reasoning: this is needed to record the microphone, which is one of the core
      functionalities of the snap.
  dot-config-foo:
    interface: personal-files
    attributes:
      write: $HOME/.config/foo
    request-type: auto-connection
    reasoning: the application needs to access its configuration files 
  removable-media
    request-type: manual-connection
    reasoning: media files are frequently stored in external disk. 
slots:
  com-jslarraz-foo:
    interface: dbus
    attributes: 
      bus: session
      name: com.jslarraz.foo
    request-type: manual-connection
    reasoning: Notte is a Flutter/GTK3 desktop application that requires 
      a D-Bus session bus slot (com.notte.notte) for proper desktop integration. 
      The GTK toolkit registers on the session bus for window management, 
      single-instance enforcement, and accessibility services. The bus name 
      matches the application ID. Without this slot, the app fails AppArmor 
      policy checks on launch.
```
