# Idea adoption plan

Written 2026-10-02. These are judged ideas from `ideas-db/`. Each one is reimplemented in this tree. The original patch is not applied and is not copied.

One commit per bullet. A bullet that names several commits is one commit and carries every source link. The commit body records the source URL. A closing word is not placed next to an issue number. `#149` and `#177` stay as they are.

## Confirmed bugs

- [x] `7746827c2b` — a command-line value is split on `:`, so a Windows path disappears. A token that is not itself a switch stays whole. Switch forms (`/cons:C:\...`, `/name=value`, a bare flag) stay as they are.
  Source: https://github.com/mRemoteNG/mRemoteNG/commit/7746827c2b4af03f16d59fe482cb6f050bef70c9
- [x] `f634039a17` — file transfer is offered only for SSH1 and SSH2. OpenSSH is included in the same offer.
  Source: https://github.com/mRemoteNG/mRemoteNG/commit/f634039a177f9204454e5fd8680b3cce3ee50863
- [x] `b7c487412f`, `a320091188` — clearing the connection-tree filter does not restore expansion. Same family as `#149`. Already in this tree: `70d545ed9` restores the snapshot, `a93729afa` batches the clear. The expansion tests pass.
  Sources: https://github.com/mRemoteNG/mRemoteNG/commit/b7c487412fc8ee89d9001dcbfad754484a42b31b
  https://github.com/mRemoteNG/mRemoteNG/commit/a32009118810e2237c029756814292f30a01343c
- [x] `85056294af` — unsaved connection edits can be lost on shutdown. The failure-path save already exists. The gap is flushing the deferred save.
  Source: https://github.com/mRemoteNG/mRemoteNG/commit/85056294af97fa3ad7316a36097e0d38f67a9500
- [x] `c4d0596f24` — a tab stays behind when its close is cancelled.
  Source: https://github.com/mRemoteNG/mRemoteNG/commit/c4d0596f2485d6493ea30bd27219f40c2a068759
- [x] `0121f0be4c` — a port scan that cannot start names the reason. The Scan button stays on Scan until that check accepts the scan.
  Source: https://github.com/mRemoteNG/mRemoteNG/commit/0121f0be4cea9a485f91f41d2bae56ea2b288f65
- [x] `232fbf32ff` — Enter on a multi-selection opens each selected connection.
  Source: https://github.com/mRemoteNG/mRemoteNG/commit/232fbf32ffc324b12ceb8b8e251e4637742a8073
- [x] `dd54616a2e` — a failed legacy decrypt returns no tree. It does not throw, so the loader does not open the file dialog again.
  Source: https://github.com/mRemoteNG/mRemoteNG/commit/dd54616a2e47bdb94e18b2fbafbd2a30764a3728
- [x] `a677fae337` — closing a connection tab already swallows `ObjectDisposedException`. The window calls that decision, and the test calls it too.
  Source: https://github.com/mRemoteNG/mRemoteNG/commit/a677fae337a8c49890c6a0e2d87b9739d708d25d

## Small, and worth taking

- [ ] `53451f91f5` — the first main window opens at 90% of the screen.
  Source: https://github.com/mRemoteNG/mRemoteNG/commit/53451f91f5195273903d0ac51e92ec1ee45e13ab
- [ ] `0045263765` — the detected PuTTY path is shown on the Advanced page.
  Source: https://github.com/mRemoteNG/mRemoteNG/commit/004526376515164a858c98a9a1c782d04a28c33c
- [ ] `c4837b6551`, `739847ef5a`, `2a90030329` — task-dialog text is measured the way it is drawn, and the command button draws its focus ring.
  Sources: https://github.com/mRemoteNG/mRemoteNG/commit/c4837b6551b9d8fe8ffc9980159905d0442824f3
  https://github.com/mRemoteNG/mRemoteNG/commit/739847ef5a331f124a67e09d20f59c478f54adf0
  https://github.com/mRemoteNG/mRemoteNG/commit/2a900303298b1f4583b2c74ad65373d7c4ab140d
- [ ] `4d62f0a6e5` — the English automatic-reconnect label says what the control does. One string is adapted. The resource file is not imported wholesale.
  Source: https://github.com/mRemoteNG/mRemoteNG/commit/4d62f0a6e5ccee65625a2ca728414f7a8ff6927e
- [ ] `d6f4872b8b` — Close is on the panel-tab menu.
  Source: https://github.com/mRemoteNG/mRemoteNG/commit/d6f4872b8bd1a73e4f293a78243ed424b7347e3e
- [ ] `d500a8e9dd` — the window title shows the full path when the connections file path is relative.
  Source: https://github.com/mRemoteNG/mRemoteNG/commit/d500a8e9dda08af453e3e69f3d891e2be4145686
- [ ] `6c1dbeaf82` — the sole-candidate path test does not depend on the machine disk.
  Source: https://github.com/mRemoteNG/mRemoteNG/commit/6c1dbeaf8209f23145c456805bb5cd72d03cff32
- [ ] `6eabb55063` — the dead space in the port-scan window is removed.
  Source: https://github.com/mRemoteNG/mRemoteNG/commit/6eabb55063017b51f2a2a0097f9714508ae98b19

## Larger, on our design

- [ ] `eb03e059b2` — the user can choose the interface font.
  Source: https://github.com/mRemoteNG/mRemoteNG/commit/eb03e059b2ecc1a1b00dc70056b70cdb348a2195
- [ ] `3f94a2c239`, `1329450e78` — follow the system theme, including dark title bars. Our theme already applies live, so the two-restart path is not copied.
  Sources: https://github.com/mRemoteNG/mRemoteNG/commit/3f94a2c23980a384cbf15386ae7ffc506a92e6e5
  https://github.com/mRemoteNG/mRemoteNG/commit/1329450e782556cc4fdc33bd916aedabe9434064
- [ ] `4edeaba5c1`, `08b056f698`, `9216eeec3f` — an RDP resize that moves again while it is applied, and an RDP 8 session that reconnects only to change size. Trace first. This touches `#177`. It is not treated as a general reconnect defect.
  Sources: https://github.com/mRemoteNG/mRemoteNG/commit/4edeaba5c11fe819d1cf1fd37584e6b4fa79c325
  https://github.com/mRemoteNG/mRemoteNG/commit/08b056f698587be31a2582070549e24dc7147fcb
  https://github.com/mRemoteNG/mRemoteNG/commit/9216eeec3fc53ba08f2ea15804a58983916a8f6e
- [ ] `c535880a14` — one address field on the port scan. The CIDR parser already exists.
  Source: https://github.com/mRemoteNG/mRemoteNG/commit/c535880a14c0395a18dfe24a3c22d4c1853dfe2a
- [ ] `42f6eaaf62` — notification-panel messages keep one order across threads.
  Source: https://github.com/mRemoteNG/mRemoteNG/commit/42f6eaaf62bcc6cd83538304a1c0b8184eb50f6c
- [ ] `b1e1dcfe5d` — handing a console to another process does not leave an empty window.
  Source: https://github.com/mRemoteNG/mRemoteNG/commit/b1e1dcfe5db54b1acd6d191c57dd3980a38ab3cf
