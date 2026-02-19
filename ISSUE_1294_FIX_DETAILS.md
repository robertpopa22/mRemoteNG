RDP session on wrong monitor's taskbar fix

- Added OnEnterFullScreenMode handler to RdpProtocol
- Apply WS_EX_APPWINDOW style to RDP fullscreen window to force taskbar item on the correct monitor
- Added necessary P/Invoke definitions to NativeMethods

Fixes #1294