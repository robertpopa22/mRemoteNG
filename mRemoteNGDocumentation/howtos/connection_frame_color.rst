##############################
Connection Frame Color
##############################

********
Overview
********

The Connection Frame Color feature allows you to visually distinguish between different connection environments (e.g., production, staging, test, development) by adding a colored border around the connection panel. This helps prevent accidental actions on critical systems by providing a clear visual indicator.

***************
How to Use
***************

Setting the Frame Color
========================

1. In the Connections panel, select the connection or folder you want to configure
2. In the Config panel, find the **Display** section
3. Locate the **Connection Frame Color** dropdown
4. Select one of the following options:

   - **None** - No colored border (default)
   - **Red (Production)** - For production environments
   - **Yellow (Staging/UAT)** - For staging or UAT environments  
   - **Green (Test)** - For test environments
   - **Blue (Development)** - For development environments
   - **Purple (Custom)** - For other custom environments

5. The border will appear immediately when you connect to the session

Visual Examples
===============

When a connection has a frame color set:

- A 4-pixel wide colored border appears around the entire connection panel
- The border is always visible, making it impossible to miss
- Different colors help you quickly identify the environment type

Inheritance
===========

Like other connection properties, the Connection Frame Color can be inherited from parent folders:

1. Set the Connection Frame Color on a folder
2. Enable inheritance for child connections (check "Inherit Connection Frame Color")
3. All connections in that folder will automatically use the same frame color

This is particularly useful for organizing connections by environment in folder structures like:

::

    Production/
    ├── Server1 (inherits Red)
    ├── Server2 (inherits Red)
    └── Database (inherits Red)
    
    Development/
    ├── DevServer1 (inherits Blue)
    └── DevServer2 (inherits Blue)

***************
Best Practices
***************

Environment Organization
========================

Consider using this convention:

- **Red** for production systems (critical, requires extra caution)
- **Yellow** for staging/UAT systems (pre-production testing)
- **Green** for test systems (safe for experimentation)
- **Blue** for development systems (individual developer environments)
- **Purple** for special cases (maintenance, temporary, etc.)

Folder Structure
================

Organize your connections by environment to take advantage of inheritance:

1. Create top-level folders for each environment
2. Set the appropriate Connection Frame Color on each folder
3. Enable inheritance for all child connections
4. New connections added to each folder will automatically get the correct frame color

***************
Troubleshooting
***************

Border Not Visible
==================

If the colored border is not showing:

1. Verify the Connection Frame Color is set to something other than "None"
2. Check if inheritance is disabled - set the color directly on the connection
3. Ensure you're viewing an active connection (the border only appears on connected sessions)

Border Too Subtle
=================

The border is designed to be 4 pixels wide for clear visibility. If you find it difficult to see:

- Check your display settings and color calibration
- Consider using a different color that contrasts better with your theme
- The Red color is specifically chosen to be highly visible for production warnings
