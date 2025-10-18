# Connection Frame Color - Visual Examples

## Before and After

### Without Frame Color (Default)
```
┌──────────────────────────────────────────────┐
│  Connection Tab                              │
├──────────────────────────────────────────────┤
│                                              │
│                                              │
│   [Normal connection content area]          │
│                                              │
│                                              │
│                                              │
└──────────────────────────────────────────────┘
```

### With Red Frame Color (Production)
```
┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃  Connection Tab (Production)                ┃
┣━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┫
┃  ╔════════════════════════════════════════╗ ┃
┃  ║                                        ║ ┃
┃  ║  [Connection content area]             ║ ┃
┃  ║   With 4-pixel RED border all around   ║ ┃
┃  ║                                        ║ ┃
┃  ╚════════════════════════════════════════╝ ┃
┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛
```

### With Green Frame Color (Test)
```
┌──────────────────────────────────────────────┐
│  Connection Tab (Test)                       │
├──────────────────────────────────────────────┤
│  ╔════════════════════════════════════════╗ │
│  ║                                        ║ │
│  ║  [Connection content area]             ║ │
│  ║   With 4-pixel GREEN border all around ║ │
│  ║                                        ║ │
│  ╚════════════════════════════════════════╝ │
└──────────────────────────────────────────────┘
```

## Side-by-Side Comparison

```
  Test Connection              Production Connection
┌─────────────────────┐       ┏━━━━━━━━━━━━━━━━━━━━━┓
│ [test server]       │       ┃ [production server] ┃
│                     │       ┃  ╔═══════════════╗  ┃
│  ┌───────────────┐  │       ┃  ║               ║  ┃
│  │               │  │       ┃  ║  !WARNING!    ║  ┃
│  │  SSH Session  │  │       ┃  ║  Production   ║  ┃
│  │  (Green)      │  │       ┃  ║  Environment  ║  ┃
│  └───────────────┘  │       ┃  ║   (Red)       ║  ┃
│                     │       ┃  ╚═══════════════╝  ┃
└─────────────────────┘       ┗━━━━━━━━━━━━━━━━━━━━━┛
  Safe to experiment          Requires extra caution!
```

## Property Grid Display

When you select a connection in mRemoteNG, the Config panel will show:

```
┌─ Display ──────────────────────────────────┐
│                                            │
│  Name:                   My Server         │
│  Description:            Production DB     │
│  Icon:                   SSH               │
│  Panel:                  General           │
│  Color:                  [empty]           │
│  Tab Color:              [empty]           │
│  Connection Frame Color: ▼ Red (Production)│
│                           ├─ None          │
│                           ├─ Red (Prod...)│
│                           ├─ Yellow (St...)│
│                           ├─ Green (Test) │
│                           ├─ Blue (Dev)   │
│                           └─ Purple (C...)│
└────────────────────────────────────────────┘
```

## Folder Inheritance Example

```
📁 Production Servers (ConnectionFrameColor: Red)
   ├─ 📄 Web Server 1 (inherits Red)
   ├─ 📄 Web Server 2 (inherits Red)
   └─ 📄 Database Server (inherits Red)

📁 Development Servers (ConnectionFrameColor: Blue)
   ├─ 📄 Dev Server 1 (inherits Blue)
   └─ 📄 Dev Server 2 (inherits Blue)

📁 Test Servers (ConnectionFrameColor: Green)
   ├─ 📄 Test Server 1 (inherits Green)
   └─ 📄 QA Server (inherits Green)
```

All connections in the "Production Servers" folder will automatically 
get a red border when you connect to them (assuming inheritance is enabled).

## Color Palette

The implementation uses these specific colors:

- **Red (Production)**:    RGB(220, 53, 69)   - High visibility warning color
- **Yellow (Staging/UAT)**: RGB(255, 193, 7)   - Caution/warning color
- **Green (Test)**:         RGB(40, 167, 69)   - Safe/go color
- **Blue (Development)**:   RGB(0, 123, 255)   - Calm, neutral color
- **Purple (Custom)**:      RGB(111, 66, 193)  - Distinct custom color
- **None (Default)**:       Transparent        - No border

These colors are chosen for:
1. High contrast and visibility
2. Universal recognition (red = danger, green = safe)
3. Accessibility considerations
4. Consistency with modern UI design patterns
