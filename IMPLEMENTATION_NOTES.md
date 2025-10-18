# Connection Frame Color Feature - Implementation Summary

## Overview
This implementation adds a "Connection Frame Color" feature to mRemoteNG that allows users to visually distinguish between different connection environments (e.g., production, test, development) by displaying a colored border around connection panels.

## Feature Request Context
The feature was requested in issue to prevent accidental operations on production systems. The user cited DBeaver database management tool as an example, which uses a red frame to indicate production database connections.

## Implementation Details

### 1. Data Model (Connection/ConnectionFrameColor.cs)
Created an enum with the following values:
- **None** (default): No colored border
- **Red**: Intended for production environments
- **Yellow**: Intended for staging/UAT environments  
- **Green**: Intended for test environments
- **Blue**: Intended for development environments
- **Purple**: Intended for custom/other environments

### 2. Property Addition (Connection/AbstractConnectionRecord.cs)
- Added `ConnectionFrameColor` property to the base connection record class
- Property is categorized under "Display" section in PropertyGrid
- Uses EnumTypeConverter for proper display in PropertyGrid
- Includes localized descriptions

### 3. Inheritance Support (Connection/ConnectionInfoInheritance.cs)
- Added `ConnectionFrameColor` inheritance property
- Allows folders to set a frame color that child connections can inherit
- Follows the same pattern as other inheritable properties

### 4. Serialization

#### XML Serialization (Config/Serializers/ConnectionSerializers/Xml/)
- **XmlConnectionNodeSerializer28.cs**: Serializes ConnectionFrameColor as an XML attribute
- **XmlConnectionsDeserializer.cs**: Deserializes ConnectionFrameColor from XML
- Includes inheritance attribute handling (InheritConnectionFrameColor)
- Backward compatible: old files without this attribute will default to None

#### CSV Serialization (Config/Serializers/ConnectionSerializers/Csv/)
- **CsvConnectionsSerializerMremotengFormat.cs**: Added ConnectionFrameColor to CSV export
- Includes both value and inheritance columns
- Maintains CSV column order consistency

### 5. Visual Rendering (Connection/InterfaceControl.cs)
- Added custom Paint event handler to InterfaceControl
- Draws a 4-pixel colored border around the connection panel when ConnectionFrameColor is set
- Uses specific colors:
  - Red: RGB(220, 53, 69) - Bootstrap danger red
  - Yellow: RGB(255, 193, 7) - Warning yellow
  - Green: RGB(40, 167, 69) - Success green
  - Blue: RGB(0, 123, 255) - Primary blue
  - Purple: RGB(111, 66, 193) - Purple
- Border is drawn inside the control bounds to avoid clipping

### 6. Localization (Language/Language.resx)
Added language resources for:
- ConnectionFrameColor: "Connection Frame Color"
- PropertyDescriptionConnectionFrameColor: Description shown in PropertyGrid
- FrameColorNone: "None"
- FrameColorRed: "Red (Production)"
- FrameColorYellow: "Yellow (Staging/UAT)"
- FrameColorGreen: "Green (Test)"
- FrameColorBlue: "Blue (Development)"
- FrameColorPurple: "Purple (Custom)"

### 7. Documentation (mRemoteNGDocumentation/howtos/connection_frame_color.rst)
Created comprehensive documentation including:
- Overview and purpose
- Step-by-step usage instructions
- Visual examples
- Inheritance explanation
- Best practices for environment organization
- Troubleshooting guide

## Technical Design Decisions

### Why 4-pixel border?
- Wide enough to be immediately noticeable
- Not so wide as to obscure content
- Consistent with modern UI design patterns

### Why these specific colors?
- Colors chosen based on common conventions:
  - Red = danger/production (universal warning color)
  - Yellow = caution/staging (standard warning color)
  - Green = safe/test (universal "go" color)
  - Blue = development (calm, neutral)
  - Purple = custom (distinct but not alarming)
- Colors use accessible, high-contrast RGB values

### Why enum instead of custom color picker?
- Simpler UI (dropdown vs color picker)
- Ensures consistency across team/organization
- Prevents confusion from too many color choices
- Follows principle of "convention over configuration"
- Can be extended in future if needed

### Why inherit from Panel?
- InterfaceControl is already a Panel (see InterfaceControl.Designer.cs)
- Panel has built-in Paint event support
- No need for additional controls or complexity

## Backward Compatibility
- Old connection files (without ConnectionFrameColor attribute) automatically default to None
- No migration needed
- Feature is completely opt-in
- Does not affect existing functionality

## Testing Recommendations

When testing this feature, verify:

1. **Property Display**: ConnectionFrameColor appears in PropertyGrid under Display section
2. **Enum Values**: All color options appear in dropdown
3. **Visual Rendering**: Border appears when color is selected and connection is active
4. **Inheritance**: Setting on folder and enabling inheritance on child works correctly
5. **Serialization**: 
   - Save connection with frame color set
   - Close and reopen file
   - Verify color is preserved
6. **CSV Export**: ConnectionFrameColor appears in exported CSV
7. **Backward Compatibility**: Open old connection files without errors

## Future Enhancements (Out of Scope)

Potential future improvements:
- Custom color picker support
- Border width customization
- Border style options (solid, dashed, etc.)
- Tab header color indicator in addition to panel border
- Global warning when connecting to production (confirmation dialog)
- Audit logging for production connections

## Files Modified

1. mRemoteNG/Connection/ConnectionFrameColor.cs (NEW)
2. mRemoteNG/Connection/AbstractConnectionRecord.cs
3. mRemoteNG/Connection/ConnectionInfo.cs
4. mRemoteNG/Connection/ConnectionInfoInheritance.cs
5. mRemoteNG/Connection/InterfaceControl.cs
6. mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlConnectionNodeSerializer28.cs
7. mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlConnectionsDeserializer.cs
8. mRemoteNG/Config/Serializers/ConnectionSerializers/Csv/CsvConnectionsSerializerMremotengFormat.cs
9. mRemoteNG/Language/Language.resx
10. mRemoteNGDocumentation/howtos/connection_frame_color.rst (NEW)

## Code Review Checklist

- [x] Property follows existing naming conventions
- [x] Enum values are localized
- [x] Inheritance support implemented
- [x] XML serialization/deserialization working
- [x] CSV serialization updated
- [x] Visual rendering implemented
- [x] Documentation created
- [x] Backward compatibility maintained
- [x] No breaking changes
- [x] Code follows existing patterns in codebase
