# Apple II Character Generation Specification

## Document Information

| Field        | Value                                              |
|--------------|----------------------------------------------------|
| Version      | 1.0                                                |
| Date         | 2026-01-09                                         |
| Status       | Initial Draft                                      |
| Applies To   | Pocket2e (Apple IIe), Pocket2c (Apple IIc)         |
| Related Docs | Apple II Video Display Specification               |
|              | Apple II 80-Column Card Specification              |
|              | Display Rendering Integration Specification        |
|              | Architecture Spec v1.0                             |

---

## 1. Overview

This specification describes how the Apple II generates text characters on screen,
focusing on the character generator ROM architecture and its emulation. Understanding
the character generation system is essential for accurate text mode rendering and
for supporting advanced features like the ALTCHAR switch and custom character sets.

### 1.1 Historical Context

The original Apple II series stored character bitmaps in a dedicated ROM chip that
was **not directly accessible to the CPU**. The video hardware read character codes
from text page memory ($0400-$07FF or $0800-$0BFF) and used those codes to index
into the character ROM, which provided 8-byte bitmap patterns for each character.

This design has several implications for emulation:

1. **Separate memory space**: The character ROM exists outside the CPU's address
   space, similar to how a modern GPU has its own memory.

2. **Device-owned ROM**: The video device owns and manages its character data,
   not the system bus or CPU.

3. **Multiple character sets**: The Apple IIe introduced the ALTCHAR switch to
   select between primary and alternate character sets (including MouseText).

4. **Potential for customization**: While the original hardware used fixed ROM,
   emulation can support custom character sets and RAM overlays.

### 1.2 Design Goals

This specification defines:

1. **Device-owned character memory**: `CharacterDevice` manages its own 4KB ROM bank
   for character data using the `PhysicalMemory` type.

2. **Dual character set support**: The ROM holds two complete 2KB character sets
   to support the ALTCHAR switch.

3. **Dual glyph RAM banks**: Two 4KB glyph RAM banks enable custom character overlays
   controlled by soft switches (ALTGLYPH1, ALTGLYPH2).

4. **Per-bank flash control**: The NOFLASH1 and NOFLASH2 soft switches allow
   disabling flashing behavior on a per-bank basis. Bank 2 defaults to NOFLASH.

5. **Glyph RAM read/write control**: RDGLYPH and WRTGLYPH soft switches control
   access to glyph RAM, enabling programs to load custom character sets.

6. **Configuration-driven ROM loading**: Character ROM files are referenced via
   the `rom-images` section in machine profiles.

---

## 2. Character Bitmap Format

### 2.1 Bitmap Structure

Each character occupies 8 bytes in the character ROM, representing 8 scanlines of
the character from top to bottom. Each byte contains 7 pixels (bits 0-6) with bit 7
unused in the standard format.

```
Character 'A' (0x41) in typical Apple II font:

Byte 0 (top):     0 0 0 1 1 0 0 0   = $18
Byte 1:           0 0 1 0 0 1 0 0   = $24
Byte 2:           0 1 0 0 0 0 1 0   = $42
Byte 3:           0 1 0 0 0 0 1 0   = $42
Byte 4:           0 1 1 1 1 1 1 0   = $7E
Byte 5:           0 1 0 0 0 0 1 0   = $42
Byte 6:           0 1 0 0 0 0 1 0   = $42
Byte 7 (bottom):  0 0 0 0 0 0 0 0   = $00

Visual representation (bit 6 = leftmost pixel):
  ██
 █  █
█    █
█    █
██████
█    █
█    █
```

### 2.2 ROM Organization

The character ROM is organized as follows:

| Offset      | Size  | Content                              |
|-------------|-------|--------------------------------------|
| $0000-$07FF | 2KB   | Primary character set (256 chars)    |
| $0800-$0FFF | 2KB   | Alternate character set (256 chars)  |

Total size: **4KB (4096 bytes)**

Each 2KB segment contains 256 characters × 8 bytes = 2048 bytes.

### 2.3 Character Code to ROM Address

To find the bitmap for a character:

```csharp
/// <summary>
/// Calculates the ROM offset for a character's bitmap.
/// </summary>
/// <param name="charCode">The 8-bit character code.</param>
/// <param name="useAltCharSet">True to use alternate character set.</param>
/// <returns>Offset into the 4KB character ROM.</returns>
public static int GetCharacterRomOffset(byte charCode, bool useAltCharSet)
{
    int baseOffset = useAltCharSet ? 0x0800 : 0x0000;
    return baseOffset + (charCode * 8);
}
```

### 2.4 Character Set Contents

#### Primary Character Set ($0000-$07FF)

| Code Range  | Characters                              | Display Mode    |
|-------------|-----------------------------------------|-----------------|
| $00-$1F     | @ A B C ... through inverse symbols     | Inverse         |
| $20-$3F     | Space ! " # ... through ?               | Inverse         |
| $40-$5F     | @ A B C ... through _ (same as $00-$1F) | Flashing        |
| $60-$7F     | Space ! " # ... (same as $20-$3F)       | Flashing        |
| $80-$9F     | @ A B C ... through _                   | Normal          |
| $A0-$BF     | Space ! " # ... through ?               | Normal          |
| $C0-$DF     | @ A B C ... through _                   | Normal          |
| $E0-$FF     | Lowercase a b c ... (IIe) or uppercase  | Normal          |

#### Alternate Character Set ($0800-$0FFF)

The alternate character set is identical to the primary set except:

| Code Range  | Primary Set      | Alternate Set (IIe) |
|-------------|------------------|---------------------|
| $40-$5F     | Flashing @-_     | MouseText glyphs    |
| $60-$7F     | Flashing space-? | Inverse lowercase   |
| $C0-$DF     | @-_              | MouseText glyphs    |

---

## 3. CharacterDevice Architecture

### 3.1 Device Overview

The `CharacterDevice` is a dedicated motherboard device that manages all character
generation functionality. This separates character glyph management from video mode
control, following the separation of concerns principle.

Key features:
- 4KB character ROM for primary and alternate character sets
- Two 4KB glyph RAM banks for custom character overlays
- Soft switch-controlled glyph bank selection
- Per-bank flash control
- Glyph RAM read/write access control

### 3.2 Device-Owned Memory

The `CharacterDevice` owns multiple `PhysicalMemory` instances:

```csharp
/// <summary>
/// Character generator device managing character glyph ROM and RAM.
/// </summary>
[DeviceType("character")]
public sealed class CharacterDevice : ICharacterDevice, ISoftSwitchProvider
{
    /// <summary>
    /// Character ROM size: 4KB for two complete character sets.
    /// </summary>
    public const int CharacterRomSize = 4096;

    /// <summary>
    /// Size of each character set within the ROM.
    /// </summary>
    public const int CharacterSetSize = 2048;

    /// <summary>
    /// Size of each glyph RAM bank: 4KB.
    /// </summary>
    public const int GlyphBankSize = 4096;

    private readonly PhysicalMemory glyphBank1;
    private readonly PhysicalMemory glyphBank2;
    private PhysicalMemory? characterRom;

    // Soft switch state
    private bool altGlyph1Enabled;
    private bool altGlyph2Enabled;
    private bool noFlash1Enabled;
    private bool noFlash2Enabled = true; // Bank 2 defaults to NOFLASH
    private bool glyphReadEnabled;
    private bool glyphWriteEnabled;
}
```

### 3.3 Character Data Access Interface

The character device implements `ICharacterDevice` which extends `ICharacterRomProvider`:

```csharp
/// <summary>
/// Character generator device interface for text rendering.
/// </summary>
public interface ICharacterDevice : IMotherboardDevice, ICharacterRomProvider
{
    /// <summary>
    /// Gets a value indicating whether glyph bank 1 overlay is enabled.
    /// </summary>
    bool IsAltGlyph1Enabled { get; }

    /// <summary>
    /// Gets a value indicating whether glyph bank 2 overlay is enabled.
    /// </summary>
    bool IsAltGlyph2Enabled { get; }

    /// <summary>
    /// Gets a value indicating whether flashing is disabled for glyph bank 1.
    /// </summary>
    bool IsNoFlash1Enabled { get; }

    /// <summary>
    /// Gets a value indicating whether flashing is disabled for glyph bank 2.
    /// </summary>
    bool IsNoFlash2Enabled { get; }

    /// <summary>
    /// Gets a value indicating whether glyph RAM reading is enabled.
    /// </summary>
    bool IsGlyphReadEnabled { get; }

    /// <summary>
    /// Gets a value indicating whether glyph RAM writing is enabled.
    /// </summary>
    bool IsGlyphWriteEnabled { get; }

    /// <summary>
    /// Loads character ROM data into the character device.
    /// </summary>
    void LoadCharacterRom(byte[] romData);

    /// <summary>
    /// Gets a full scanline row of pixels for an entire text row.
    /// </summary>
    void GetScanlineRow(
        ReadOnlySpan<byte> characterCodes,
        int scanline,
        bool useAltCharSet,
        bool flashState,
        Span<byte> outputBuffer);

    /// <summary>
    /// Gets a single character's scanline with proper overlay and flash handling.
    /// </summary>
    byte GetCharacterScanlineWithEffects(
        byte charCode,
        int scanline,
        bool useAltCharSet,
        bool flashState);
}
```

### 3.4 Glyph Bank Overlay Behavior

When `ALTGLYPH1` or `ALTGLYPH2` is enabled, character reads are redirected to the
corresponding glyph RAM bank instead of the character ROM:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    CharacterDevice Internal Memory                       │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│   ┌─────────────────────┐     ┌─────────────────────┐                  │
│   │  Character ROM      │     │  Glyph Bank 1       │                  │
│   │  (4KB, from profile)│     │  (4KB, internal)    │                  │
│   │                     │     │                     │                  │
│   │  $0000-$07FF: Pri   │     │  Overlays primary   │                  │
│   │  $0800-$0FFF: Alt   │     │  when ALTGLYPH1 on  │                  │
│   └─────────────────────┘     └─────────────────────┘                  │
│                                                                         │
│   ┌─────────────────────┐                                              │
│   │  Glyph Bank 2       │                                              │
│   │  (4KB, internal)    │                                              │
│   │                     │                                              │
│   │  Overlays alternate │                                              │
│   │  when ALTGLYPH2 on  │                                              │
│   └─────────────────────┘                                              │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 3.5 Soft Switch Definitions

The character device uses soft switches in the $C060-$C06B and $C024-$C029 ranges:

| Address | Name           | Function                                       |
|---------|----------------|------------------------------------------------|
| $C060   | RDGLYPHOFF     | Disable glyph RAM reading                      |
| $C061   | RDGLYPHON      | Enable glyph RAM reading                       |
| $C062   | WRTGLYPHOFF    | Disable glyph RAM writing                      |
| $C063   | WRTGLYPHON     | Enable glyph RAM writing                       |
| $C064   | NOFLASH1OFF    | Enable flashing for bank 1                     |
| $C065   | NOFLASH1ON     | Disable flashing for bank 1                    |
| $C066   | NOFLASH2OFF    | Enable flashing for bank 2                     |
| $C067   | NOFLASH2ON     | Disable flashing for bank 2                    |
| $C068   | ALTGLYPH1OFF   | Disable glyph bank 1 overlay                   |
| $C069   | ALTGLYPH1ON    | Enable glyph bank 1 overlay                    |
| $C06A   | ALTGLYPH2OFF   | Disable glyph bank 2 overlay                   |
| $C06B   | ALTGLYPH2ON    | Enable glyph bank 2 overlay                    |
| $C024   | RDALTGLYPH1    | Read glyph bank 1 status (bit 7)               |
| $C025   | RDALTGLYPH2    | Read glyph bank 2 status (bit 7)               |
| $C026   | RDNOFLASH1     | Read no-flash bank 1 status (bit 7)            |
| $C027   | RDNOFLASH2     | Read no-flash bank 2 status (bit 7)            |
| $C028   | RDGLYPHRD      | Read glyph read status (bit 7)                 |
| $C029   | RDGLYPHWR      | Read glyph write status (bit 7)                |

**Default State:**
- All glyph overlays disabled
- NoFlash1 disabled (flashing enabled)
- NoFlash2 enabled (flashing disabled by default for bank 2)
- Glyph read/write disabled

---

## 4. Legacy VideoDevice Character ROM Support

### 4.1 Backward Compatibility

The `VideoDevice` retains character ROM functionality for backward compatibility.
Systems can use either:

1. **CharacterDevice** (recommended) - Full support for glyph RAM overlays, per-bank
   flash control, and efficient scanline-based rendering.

2. **VideoDevice** - Simple character ROM support without advanced features. Useful
   for minimal configurations or backward compatibility.

### 4.2 VideoDevice Character ROM API

The VideoDevice implements `ICharacterRomProvider` for basic character data access:

```csharp
public interface ICharacterRomProvider
{
    bool IsCharacterRomLoaded { get; }
    byte GetCharacterScanline(byte charCode, int scanline, bool useAltCharSet);
    Memory<byte> GetCharacterBitmap(byte charCode, bool useAltCharSet);
    Memory<byte> GetCharacterRomData();
}
```

---

## 5. Profile Configuration

### 5.1 ROM Image Definition

Character ROM is defined in the machine profile's `rom-images` section:

```json
{
  "memory": {
    "rom-images": [
      {
        "name": "character-rom",
        "source": "library://roms/apple2e/character.rom",
        "size": "0x1000",
        "required": true,
        "on_verification_fail": "fallback"
      }
    ]
  }
}
```

### 5.2 Device Configuration

For systems using CharacterDevice (recommended):

```json
{
  "devices": {
    "motherboard": [
      {
        "type": "character",
        "name": "character-generator",
        "enabled": true,
        "config": {
          "character-rom": "character-rom"
        }
      },
      {
        "type": "video",
        "name": "video-controller",
        "enabled": true
      }
    ]
  }
}
```

For systems using legacy VideoDevice character ROM:

```json
{
  "devices": {
    "motherboard": [
      {
        "type": "video",
        "name": "video-controller",
        "enabled": true,
        "config": {
          "character-rom": "character-rom"
        }
      }
    ]
  }
}
```

---

## 6. ALTCHAR Switch Support

### 6.1 Soft Switch Behavior

The ALTCHAR soft switch at $C00E/$C00F selects the active character set:

| Address | Name       | Action                              |
|---------|------------|-------------------------------------|
| $C00E   | ALTCHAROFF | Write: Select primary character set |
| $C00F   | ALTCHARON  | Write: Select alternate character set |
| $C01E   | RDALTCHAR  | Read: Bit 7 = 1 if alternate active |

### 6.2 VideoDevice Implementation

```csharp
public sealed partial class VideoDevice
{
    private bool altCharSet;

    /// <inheritdoc />
    public bool IsAltCharSet => altCharSet;

    /// <summary>
    /// Sets the alternate character set state.
    /// </summary>
    /// <param name="enabled">Whether alternate character set is enabled.</param>
    public void SetAltCharSet(bool enabled)
    {
        if (altCharSet != enabled)
        {
            altCharSet = enabled;
            OnModeChanged();
        }
    }
}
```

### 6.3 Rendering Integration

The display renderer queries the ALTCHAR state when rendering text:

```csharp
/// <summary>
/// Renders a character with proper inverse/flash/normal handling.
/// </summary>
private void RenderCharacter(
    ICharacterRomProvider charRom,
    Span<uint> buffer,
    int bufferWidth,
    int pixelX,
    int pixelY,
    byte charCode,
    bool flashState,
    bool useAltCharSet)
{
    // Determine display mode from character code
    bool isInverse = charCode < 0x40;
    bool isFlashing = charCode >= 0x40 && charCode < 0x80;

    // For flashing characters in alt mode, use MouseText instead
    bool actualAltChar = useAltCharSet && isFlashing;

    // Apply flash effect (invert if flashing and flash state is on)
    bool invert = isInverse || (isFlashing && !actualAltChar && flashState);

    // Get character bitmap
    for (int scanline = 0; scanline < 8; scanline++)
    {
        byte pixels = charRom.GetCharacterScanline(charCode, scanline, actualAltChar);

        if (invert)
        {
            pixels = (byte)(~pixels & 0x7F); // Invert only the 7 pixel bits
        }

        // Render 7 pixels
        for (int bit = 0; bit < 7; bit++)
        {
            bool lit = (pixels & (0x40 >> bit)) != 0;
            uint color = lit ? _foregroundColor : _backgroundColor;

            int x = pixelX + bit;
            int y = pixelY + scanline;
            buffer[(y * bufferWidth) + x] = color;
        }
    }
}
```

---

## 7. Custom Character Sets with CharacterDevice

### 7.1 Concept

The `CharacterDevice` provides custom character support through glyph RAM banks:

1. **Glyph Bank 1**: 4KB RAM that can overlay the primary character set when
   ALTGLYPH1 is enabled.

2. **Glyph Bank 2**: 4KB RAM that can overlay the alternate character set when
   ALTGLYPH2 is enabled.

Programs can write custom character data to glyph RAM and enable the overlay
to display custom fonts.

### 7.2 Loading Custom Characters

```assembly
;-----------------------------------------------------------------------
; LOADCHAR - Load custom character bitmap into CharacterDevice glyph RAM
;
; Programs write directly to glyph RAM using WRTGLYPH switch, then
; enable the glyph bank overlay to use custom characters.
;-----------------------------------------------------------------------

WRTGLYPHON   = $C063    ; Enable glyph RAM writing
WRTGLYPHOFF  = $C062    ; Disable glyph RAM writing
ALTGLYPH1ON  = $C069    ; Enable glyph bank 1 overlay
ALTGLYPH1OFF = $C068    ; Disable glyph bank 1 overlay

CUSTOM_FONT  = $4000    ; Source address for custom font data

LOADCHAR:
        ; Step 1: Enable glyph RAM writing
        STA WRTGLYPHON

        ; Step 2: Copy custom font to glyph RAM
        ; (Implementation depends on memory mapping)
        ; ...

        ; Step 3: Disable glyph RAM writing
        STA WRTGLYPHOFF

        ; Step 4: Enable glyph bank 1 overlay for rendering
        STA ALTGLYPH1ON

        ; Done! Characters now render from glyph bank 1.
        RTS
```

### 7.3 Flash Control

Each glyph bank has independent flash control:

- **Bank 1**: Flashing enabled by default (characters $40-$7F flash)
- **Bank 2**: Flashing disabled by default (NOFLASH2 enabled at reset)

```assembly
; Disable flashing for bank 1
NOFLASH1ON   = $C065
        STA NOFLASH1ON  ; Characters $40-$7F no longer flash

; Enable flashing for bank 2
NOFLASH2OFF  = $C066
        STA NOFLASH2OFF ; Characters $40-$7F now flash in alt mode
```

---

## 8. Revision History

| Version | Date       | Author       | Description                                    |
|---------|------------|--------------|------------------------------------------------|
| 1.0     | 2026-01-09 | Initial      | Initial draft with VideoDevice character ROM   |
| 1.1     | 2026-01-13 | CharacterDevice | Added CharacterDevice with glyph RAM banks, per-bank flash control, and enhanced soft switches |
