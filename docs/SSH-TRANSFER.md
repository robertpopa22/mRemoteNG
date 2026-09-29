# SSH file transfer

SFTP is the default transfer option. It passes paths through the file-transfer protocol without
running them through a remote shell and is the appropriate choice for Windows or an unknown shell.

The SCP option requires a POSIX-compatible shell. Remote paths are explicitly shell-quoted using
SSH.NET's `ShellQuote` transformation; spaces, quotes and command substitutions in a path are
literal filename characters. No fallback to the older double-quote transformation is used.
This changes the supported SCP shell contract; servers requiring a different command interpreter
must use SFTP. Neither protocol setting changes the application's SSH authentication settings.

SSH.NET 2026.0.0 is used with its required BouncyCastle dependency. Merely upgrading SSH.NET would
retain the unsafe legacy path default. See the
[publisher's advisory](https://github.com/sshnet/SSH.NET/security/advisories/GHSA-mggc-4xg6-vcxf).

SFTP closes its local input stream when the transfer is disposed and checks the asynchronous
result before reporting completion. A server-side failure must not be presented as a completed
transfer, and the controls become available again after an error.
