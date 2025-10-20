****************
AnyDesk Protocol
****************

AnyDesk is a remote desktop application that provides a fast and secure connection to remote computers. mRemoteNG supports connecting to AnyDesk through its command-line interface.

Prerequisites
=============

Before using the AnyDesk protocol in mRemoteNG, ensure that:

1. AnyDesk is installed on your local machine
2. The AnyDesk executable is located in one of the default installation paths:
   
   - ``C:\Program Files (x86)\AnyDesk\AnyDesk.exe``
   - ``C:\Program Files\AnyDesk\AnyDesk.exe``
   - Or in your system's PATH environment variable

Configuration
=============

To configure an AnyDesk connection in mRemoteNG:

Connection Properties
---------------------

.. list-table::
   :widths: 30 70
   :header-rows: 1

   * - Property
     - Description
   * - Protocol
     - Select ``AnyDesk`` from the protocol dropdown
   * - Hostname
     - The AnyDesk ID or alias of the remote computer (e.g., ``123456789`` or ``mycomputer@ad``)
   * - Username
     - Reserved for future use (currently not used by AnyDesk CLI)
   * - Password
     - The password for unattended access (optional). If provided, it will be automatically passed to AnyDesk using the ``--with-password`` flag

Usage Examples
==============

Basic Connection
----------------

For a simple connection without password:

- **Protocol**: AnyDesk
- **Hostname**: 123456789

This will launch AnyDesk and connect to the specified ID. You will be prompted to enter the password manually if required.

Unattended Access
-----------------

For automatic connection with password:

- **Protocol**: AnyDesk
- **Hostname**: 123456789
- **Password**: your_anydesk_password

This will automatically pipe the password to AnyDesk for unattended access.

Using Alias
-----------

If you have configured an alias in AnyDesk:

- **Protocol**: AnyDesk
- **Hostname**: mycomputer@ad
- **Password**: your_anydesk_password

Features
========

- **Automatic Password Authentication**: When a password is provided, mRemoteNG uses PowerShell to pipe the password to AnyDesk as recommended by AnyDesk's CLI documentation
- **Window Integration**: The AnyDesk window is embedded within mRemoteNG's interface for a seamless experience
- **Plain Mode**: AnyDesk is launched with the ``--plain`` flag to minimize the AnyDesk UI and provide a cleaner connection experience

Troubleshooting
===============

AnyDesk Not Found
-----------------

If you receive an error that AnyDesk is not installed:

1. Verify that AnyDesk is installed on your machine
2. Check that the executable exists in one of the default paths
3. Alternatively, add the AnyDesk installation directory to your system's PATH environment variable

Connection Issues
-----------------

If the connection fails or the window doesn't appear:

1. Verify that the AnyDesk ID is correct
2. Check that the remote computer has AnyDesk running
3. Ensure the password is correct (for unattended access)
4. Check the message log in mRemoteNG for specific error messages

Window Not Embedding
--------------------

If the AnyDesk window doesn't appear embedded in mRemoteNG:

1. AnyDesk may take a few seconds to launch - wait up to 10 seconds
2. Some AnyDesk versions may not support window embedding
3. Check the Windows Task Manager to verify that AnyDesk.exe is running

Notes
=====

- The AnyDesk protocol works with the free version of AnyDesk
- For security reasons, passwords are never stored in plain text - they are encrypted by mRemoteNG
- The Username field is reserved for future use and is currently not utilized by the AnyDesk CLI
- AnyDesk connections are integrated into mRemoteNG's tabbed interface for easy management

References
==========

- `AnyDesk Command Line Interface Documentation <https://support.anydesk.com/Command_Line_Interface>`_
- `AnyDesk Official Website <https://anydesk.com/>`_
