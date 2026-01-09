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

1. **Device-owned character memory**: `VideoDevice` manages its own 4KB ROM bank
   for character data using the `PhysicalMemory` type.

2. **Dual character set support**: The ROM holds two complete 2KB character sets
   to support the ALTCHAR switch.

3. **Configuration-driven ROM loading**: Character ROM files are referenced via
   the `rom-images` section in machine profiles.

4. **RAM overlay capability**: Advanced feature to layer system RAM over character
   ROM regions for custom character sets.

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
| $60-$7F     | Flashing space-? | MouseText glyphs    |

---

## 3. VideoDevice Character ROM Architecture

### 3.1 Device-Owned Memory

The `VideoDevice` owns a 4KB `PhysicalMemory` instance for character data:

```csharp
/// <summary>
/// Video device with integrated character ROM support.
/// </summary>
public sealed partial class VideoDevice : IVideoDevice, ISoftSwitchProvider
{
    /// <summary>
    /// Character ROM size: 4KB for two complete character sets.
    /// </summary>
    public const int CharacterRomSize = 4096;

    /// <summary>
    /// Size of each character set within the ROM.
    /// </summary>
    public const int CharacterSetSize = 2048;

    private PhysicalMemory? characterRom;

    /// <summary>
    /// Gets the character ROM memory, if loaded.
    /// </summary>
    public IPhysicalMemory? CharacterRom => characterRom;

    /// <summary>
    /// Initializes the character ROM with data from a ROM image.
    /// </summary>
    /// <param name="romData">The 4KB character ROM data.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when romData is not exactly 4096 bytes.
    /// </exception>
    public void LoadCharacterRom(ReadOnlySpan<byte> romData)
    {
        if (romData.Length != CharacterRomSize)
        {
            throw new ArgumentException(
                $"Character ROM must be exactly {CharacterRomSize} bytes, " +
                $"but got {romData.Length} bytes.",
                nameof(romData));
        }

        characterRom = new PhysicalMemory(romData, "CharacterROM");
    }
}
```

### 3.2 Character Data Access Interface

The video device exposes character bitmap data through a dedicated interface:

```csharp
/// <summary>
/// Provides access to character bitmap data for rendering.
/// </summary>
public interface ICharacterRomProvider
{
    /// <summary>
    /// Gets whether character ROM is loaded and available.
    /// </summary>
    bool IsCharacterRomLoaded { get; }

    /// <summary>
    /// Gets one scanline (row) of pixels for a character.
    /// </summary>
    /// <param name="charCode">The 8-bit character code (0-255).</param>
    /// <param name="scanline">The scanline within the character (0-7).</param>
    /// <param name="useAltCharSet">True to use alternate character set.</param>
    /// <returns>
    /// 7 bits representing pixels. Bit 6 is the leftmost pixel,
    /// bit 0 is the rightmost pixel.
    /// </returns>
    byte GetCharacterScanline(byte charCode, int scanline, bool useAltCharSet);

    /// <summary>
    /// Gets all 8 scanlines for a character as a span.
    /// </summary>
    /// <param name="charCode">The 8-bit character code (0-255).</param>
    /// <param name="useAltCharSet">True to use alternate character set.</param>
    /// <returns>8 bytes representing the character bitmap.</returns>
    ReadOnlySpan<byte> GetCharacterBitmap(byte charCode, bool useAltCharSet);
}
```

Implementation in `VideoDevice`:

```csharp
public sealed partial class VideoDevice : ICharacterRomProvider
{
    /// <inheritdoc />
    public bool IsCharacterRomLoaded => characterRom != null;

    /// <inheritdoc />
    public byte GetCharacterScanline(byte charCode, int scanline, bool useAltCharSet)
    {
        if (characterRom == null)
        {
            return 0x00; // No ROM loaded - return blank
        }

        ArgumentOutOfRangeException.ThrowIfGreaterThan(scanline, 7);

        int offset = GetCharacterRomOffset(charCode, useAltCharSet) + scanline;
        return characterRom.AsReadOnlySpan()[offset];
    }

    /// <inheritdoc />
    public ReadOnlySpan<byte> GetCharacterBitmap(byte charCode, bool useAltCharSet)
    {
        if (characterRom == null)
        {
            return ReadOnlySpan<byte>.Empty;
        }

        int offset = GetCharacterRomOffset(charCode, useAltCharSet);
        return characterRom.AsReadOnlySpan().Slice(offset, 8);
    }

    private static int GetCharacterRomOffset(byte charCode, bool useAltCharSet)
    {
        int baseOffset = useAltCharSet ? CharacterSetSize : 0;
        return baseOffset + (charCode * 8);
    }
}
```

### 3.3 Update to IVideoDevice Interface

Add character ROM provider capability to the video device interface:

```csharp
/// <summary>
/// Video mode controller interface for display rendering.
/// </summary>
public interface IVideoDevice : IMotherboardDevice, ICharacterRomProvider
{
    // ... existing members ...

    /// <summary>
    /// Loads character ROM data into the video device.
    /// </summary>
    /// <param name="romData">The 4KB character ROM data.</param>
    void LoadCharacterRom(ReadOnlySpan<byte> romData);
}
```

---

## 4. Profile Configuration

### 4.1 ROM Image Definition

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

### 4.2 Device ROM Loading

Unlike system ROM which maps to the CPU address space, character ROM is loaded
directly into the video device. This requires a new configuration section:

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

The `character-rom` property references the ROM image by name from `rom-images`.

### 4.3 MachineBuilder Integration

The builder loads device-specific ROMs during device initialization:

```csharp
/// <summary>
/// Configures a motherboard device with its ROM resources.
/// </summary>
private void ConfigureMotherboardDeviceRoms(
    IMotherboardDevice device,
    MotherboardDeviceEntry deviceEntry,
    Dictionary<string, (string ResolvedPath, uint Size)> romImages)
{
    // Check for character-rom configuration
    if (device is IVideoDevice videoDevice &&
        deviceEntry.Config?.TryGetProperty("character-rom", out var charRomProp) == true)
    {
        string romName = charRomProp.GetString() ?? string.Empty;
        if (romImages.TryGetValue(romName, out var romInfo))
        {
            byte[] romData = File.ReadAllBytes(romInfo.ResolvedPath);
            videoDevice.LoadCharacterRom(romData);
        }
    }
}
```

---

## 5. ALTCHAR Switch Support

### 5.1 Soft Switch Behavior

The ALTCHAR soft switch at $C00E/$C00F selects the active character set:

| Address | Name       | Action                              |
|---------|------------|-------------------------------------|
| $C00E   | ALTCHAROFF | Write: Select primary character set |
| $C00F   | ALTCHARON  | Write: Select alternate character set |
| $C01E   | RDALTCHAR  | Read: Bit 7 = 1 if alternate active |

### 5.2 VideoDevice Implementation

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

### 5.3 Rendering Integration

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

## 6. Custom Character RAM Feature (Advanced)

### 6.1 Concept

The `VideoDevice` owns both character ROM (loaded from profile) and character RAM
(allocated internally). Custom character support is achieved through a two-phase
process using soft-switch controlled memory windows:

1. **Data Transfer Phase**: Copy character data between system RAM and the video
   device's internal character RAM using memory-mapped windows.

2. **Rendering Phase**: Select whether character rendering uses the internal ROM
   or internal RAM.

This follows the same self-contained pattern as `LanguageCardDevice`, where the
device manages its own memory and uses soft switches to control access modes.

### 6.2 Design Goals

| Goal                    | Description                                           |
|-------------------------|-------------------------------------------------------|
| **Self-contained**      | VideoDevice owns its 4KB RAM internally               |
| **No profile changes**  | Works like Language Card - just enable the device     |
| **Flexible transfer**   | Copy data in either direction (ROM→RAM, RAM→video)    |
| **Partial updates**     | Programs can modify only specific characters          |
| **Split routing**       | Separate read and write targets during transfer       |

### 6.3 Memory Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      VideoDevice Internal Memory                        │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│   ┌─────────────────────┐     ┌─────────────────────┐                  │
│   │  Character ROM      │     │  Character RAM      │                  │
│   │  (4KB, from profile)│     │  (4KB, internal)    │                  │
│   │                     │     │                     │                  │
│   │  $0000-$07FF: Pri   │     │  $0000-$07FF: Pri   │                  │
│   │  $0800-$0FFF: Alt   │     │  $0800-$0FFF: Alt   │                  │
│   └─────────────────────┘     └─────────────────────┘                  │
│            │                           │                               │
│            └───────────┬───────────────┘                               │
│                        ▼                                               │
│   ┌─────────────────────────────────────────────────────────────┐      │
│   │              Character Source Selection                      │      │
│   │         (CHARROM/CHARRAM soft switch controls)              │      │
│   └─────────────────────────────────────────────────────────────┘      │
│                        │                                               │
│                        ▼                                               │
│              Character Rendering Output                                │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 6.4 Memory Window Architecture

The video device provides a 4KB memory window at a configurable overlay address
(default: $2000-$2FFF) with split read/write routing controlled by soft switches:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    Memory Window Routing Modes                          │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Mode 0: WINDOW OFF (Default)                                          │
│  ┌─────────────┐                                                       │
│  │ System RAM  │◄──── Reads and Writes                                 │
│  │ ($2000)     │                                                       │
│  └─────────────┘                                                       │
│                                                                         │
│  Mode 1: WRITE TO VIDEO RAM                                            │
│  ┌─────────────┐      ┌─────────────┐                                  │
│  │ System RAM  │◄──── Reads         │ Video RAM  │◄──── Writes         │
│  │ ($2000)     │                    │ (internal) │                     │
│  └─────────────┘      └─────────────┘                                  │
│                                                                         │
│  Mode 2: READ FROM VIDEO ROM                                           │
│  ┌─────────────┐      ┌─────────────┐                                  │
│  │ Video ROM   │◄──── Reads         │ System RAM │◄──── Writes         │
│  │ (internal)  │                    │ ($2000)    │                     │
│  └─────────────┘      └─────────────┘                                  │
│                                                                         │
│  Mode 3: READ FROM VIDEO RAM                                           │
│  ┌─────────────┐      ┌─────────────┐                                  │
│  │ Video RAM   │◄──── Reads         │ System RAM │◄──── Writes         │
│  │ (internal)  │                    │ ($2000)    │                     │
│  └─────────────┘      └─────────────┘                                  │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 6.5 Soft Switch Definitions

The video device uses four soft switches in an unused I/O range. These follow the
Apple II convention where any access (read or write) triggers the switch:

| Address | Name           | Function                                       |
|---------|----------------|------------------------------------------------|
| $C06C   | VCHARWINOFF    | Disable character memory window (Mode 0)       |
| $C06D   | VCHARWINSYSRD  | Enable window: reads=system, writes=video RAM  |
| $C06E   | VCHARWINROMRD  | Enable window: reads=video ROM, writes=system  |
| $C06F   | VCHARWINRAMRD  | Enable window: reads=video RAM, writes=system  |
| $C02C   | RDVCHARWIN     | Read window mode status (bits 0-1)             |
| $C02D   | VCHARROMSEL    | Select character ROM for rendering             |
| $C02E   | VCHARRAMSEL    | Select character RAM for rendering             |
| $C02F   | RDVCHARSRC     | Read character source status (bit 7)           |

**Status Register Encoding ($C02C):**
- Bits 0-1: Window mode (0=off, 1=write-to-vram, 2=read-vrom, 3=read-vram)
- Bits 2-7: Reserved (return 0)

**Character Source Status ($C02F):**
- Bit 7: 1 = Character RAM selected, 0 = Character ROM selected

### 6.6 VideoDevice Implementation

```csharp
public sealed partial class VideoDevice
{
    /// <summary>
    /// Size of character memory (ROM and RAM), 4KB each.
    /// </summary>
    public const uint CharacterMemorySize = 0x1000;

    /// <summary>
    /// Default overlay address for character memory window.
    /// </summary>
    public const Addr DefaultCharWindowBase = 0x2000;

    /// <summary>
    /// Layer name for the character memory window.
    /// </summary>
    public const string CharWindowLayerName = "VIDEO_CHARWIN";

    /// <summary>
    /// Layer priority for the character memory window.
    /// </summary>
    public const int CharWindowLayerPriority = 25;

    private readonly PhysicalMemory characterRam;
    private readonly RamTarget characterRamTarget;

    private PhysicalMemory? characterRom;
    private RomTarget? characterRomTarget;

    private IMemoryBus? bus;
    private Addr charWindowBase = DefaultCharWindowBase;

    private CharWindowMode windowMode = CharWindowMode.Off;
    private bool useCharacterRam = false;

    /// <summary>
    /// Character memory window routing modes.
    /// </summary>
    public enum CharWindowMode : byte
    {
        /// <summary>Window disabled, normal system RAM access.</summary>
        Off = 0,

        /// <summary>Reads from system RAM, writes to video character RAM.</summary>
        WriteToVideoRam = 1,

        /// <summary>Reads from video character ROM, writes to system RAM.</summary>
        ReadVideoRom = 2,

        /// <summary>Reads from video character RAM, writes to system RAM.</summary>
        ReadVideoRam = 3,
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoDevice"/> class.
    /// </summary>
    public VideoDevice()
    {
        // ... existing initialization ...

        // Create internal character RAM (4KB)
        characterRam = new PhysicalMemory(CharacterMemorySize, "VideoCharRAM");
        characterRamTarget = new RamTarget(
            characterRam.Slice(0, CharacterMemorySize),
            "VideoCharRAM");
    }

    /// <summary>
    /// Gets the current character memory window mode.
    /// </summary>
    public CharWindowMode WindowMode => windowMode;

    /// <summary>
    /// Gets a value indicating whether character RAM is selected for rendering.
    /// </summary>
    public bool IsCharacterRamSelected => useCharacterRam;

    /// <summary>
    /// Configures the character memory window on the bus.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method sets up the layered mapping for the character memory window.
    /// The window overlays a 4KB region of system memory (default $2000-$2FFF)
    /// and provides split read/write routing controlled by soft switches.
    /// </para>
    /// <para>
    /// Called automatically during device initialization if character ROM is loaded.
    /// </para>
    /// </remarks>
    public void ConfigureCharacterWindow(IMemoryBus bus, IDeviceRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(registry);

        this.bus = bus;

        // Save base mapping for the window region so we can restore it
        if (bus is MainBus mainBus)
        {
            int startPage = (int)(charWindowBase >> 12);
            mainBus.SaveBaseMappingRange(startPage, 1); // Save 1 page (4KB)
        }

        // Create the character window layer
        var layer = bus.CreateLayer(CharWindowLayerName, CharWindowLayerPriority);

        // Layer starts inactive (window off)
        // When activated, the target and permissions change based on windowMode
    }

    /// <summary>
    /// Sets the character memory window mode.
    /// </summary>
    private void SetWindowMode(CharWindowMode mode)
    {
        if (windowMode == mode || bus == null)
        {
            return;
        }

        windowMode = mode;
        ApplyWindowState();
    }

    /// <summary>
    /// Applies the current window state to the bus layers.
    /// </summary>
    private void ApplyWindowState()
    {
        if (bus == null)
        {
            return;
        }

        if (windowMode == CharWindowMode.Off)
        {
            // Deactivate window layer - normal system RAM access
            if (bus.IsLayerActive(CharWindowLayerName))
            {
                bus.DeactivateLayer(CharWindowLayerName);
            }
            return;
        }

        // Activate layer with appropriate target and permissions
        IBusTarget target = windowMode switch
        {
            CharWindowMode.WriteToVideoRam => characterRamTarget,
            CharWindowMode.ReadVideoRom => characterRomTarget 
                ?? throw new InvalidOperationException("Character ROM not loaded"),
            CharWindowMode.ReadVideoRam => characterRamTarget,
            _ => throw new InvalidOperationException($"Unknown window mode: {windowMode}")
        };

        PagePerms perms = windowMode switch
        {
            // Write to video RAM: reads pass through to system, writes go to video
            CharWindowMode.WriteToVideoRam => PagePerms.Write,
            // Read from video: reads from video, writes pass through to system
            CharWindowMode.ReadVideoRom or CharWindowMode.ReadVideoRam => PagePerms.ReadExecute,
            _ => PagePerms.None
        };

        // Update the layer mapping
        if (!bus.IsLayerActive(CharWindowLayerName))
        {
            bus.ActivateLayer(CharWindowLayerName);
        }

        bus.UpdateLayerMapping(CharWindowLayerName, charWindowBase, target, perms);
    }

    /// <summary>
    /// Sets the character rendering source.
    /// </summary>
    private void SetCharacterSource(bool useRam)
    {
        if (useCharacterRam != useRam)
        {
            useCharacterRam = useRam;
            OnModeChanged();
        }
    }
}
```

### 6.7 Soft Switch Handlers

```csharp
public sealed partial class VideoDevice
{
    /// <inheritdoc />
    public void RegisterHandlers(IOPageDispatcher dispatcher)
    {
        // ... existing handler registrations ...

        // Character window control switches ($C06C-$C06F)
        dispatcher.Register(0x6C, HandleCharWinOff, HandleCharWinOff);
        dispatcher.Register(0x6D, HandleCharWinSysRd, HandleCharWinSysRd);
        dispatcher.Register(0x6E, HandleCharWinRomRd, HandleCharWinRomRd);
        dispatcher.Register(0x6F, HandleCharWinRamRd, HandleCharWinRamRd);

        // Character status reads ($C02C-$C02F)
        dispatcher.RegisterRead(0x2C, ReadCharWinStatus);
        dispatcher.Register(0x2D, HandleCharRomSel, HandleCharRomSel);
        dispatcher.Register(0x2E, HandleCharRamSel, HandleCharRamSel);
        dispatcher.RegisterRead(0x2F, ReadCharSrcStatus);
    }

    private byte HandleCharWinOff(byte offset, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree) SetWindowMode(CharWindowMode.Off);
        return 0xFF;
    }

    private void HandleCharWinOff(byte offset, byte value, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree) SetWindowMode(CharWindowMode.Off);
    }

    private byte HandleCharWinSysRd(byte offset, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree) SetWindowMode(CharWindowMode.WriteToVideoRam);
        return 0xFF;
    }

    private void HandleCharWinSysRd(byte offset, byte value, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree) SetWindowMode(CharWindowMode.WriteToVideoRam);
    }

    private byte HandleCharWinRomRd(byte offset, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree) SetWindowMode(CharWindowMode.ReadVideoRom);
        return 0xFF;
    }

    private void HandleCharWinRomRd(byte offset, byte value, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree) SetWindowMode(CharWindowMode.ReadVideoRom);
    }

    private byte HandleCharWinRamRd(byte offset, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree) SetWindowMode(CharWindowMode.ReadVideoRam);
        return 0xFF;
    }

    private void HandleCharWinRamRd(byte offset, byte value, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree) SetWindowMode(CharWindowMode.ReadVideoRam);
    }

    private byte HandleCharRomSel(byte offset, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree) SetCharacterSource(useRam: false);
        return 0xFF;
    }

    private void HandleCharRomSel(byte offset, byte value, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree) SetCharacterSource(useRam: false);
    }

    private byte HandleCharRamSel(byte offset, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree) SetCharacterSource(useRam: true);
        return 0xFF;
    }

    private void HandleCharRamSel(byte offset, byte value, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree) SetCharacterSource(useRam: true);
    }

    private byte ReadCharWinStatus(byte offset, in BusAccess ctx)
    {
        return (byte)windowMode;
    }

    private byte ReadCharSrcStatus(byte offset, in BusAccess ctx)
    {
        return useCharacterRam ? (byte)0x80 : (byte)0x00;
    }
}
```

### 6.8 Updated Character Data Access

```csharp
public sealed partial class VideoDevice : ICharacterRomProvider
{
    /// <inheritdoc />
    public byte GetCharacterScanline(byte charCode, int scanline, bool useAltCharSet)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(scanline, 7);

        int offset = GetCharacterRomOffset(charCode, useAltCharSet) + scanline;

        // Select source based on soft switch state
        if (useCharacterRam)
        {
            return characterRam.AsReadOnlySpan()[offset];
        }

        if (characterRom == null)
        {
            return 0x00; // No ROM loaded - return blank
        }

        return characterRom.AsReadOnlySpan()[offset];
    }

    /// <inheritdoc />
    public ReadOnlySpan<byte> GetCharacterBitmap(byte charCode, bool useAltCharSet)
    {
        int offset = GetCharacterRomOffset(charCode, useAltCharSet);

        if (useCharacterRam)
        {
            return characterRam.AsReadOnlySpan().Slice(offset, 8);
        }

        if (characterRom == null)
        {
            return ReadOnlySpan<byte>.Empty;
        }

        return characterRom.AsReadOnlySpan().Slice(offset, 8);
    }
}
```

### 6.9 Usage Example: Loading Custom Characters from System RAM

```assembly
;-----------------------------------------------------------------------
; LOADCHAR - Load custom character bitmap into video device
;
; This example loads a custom font from $4000 into the video device's
; internal character RAM, then activates it for rendering.
;-----------------------------------------------------------------------

VCHARWINOFF  = $C06C    ; Disable character window
VCHARWINSYS  = $C06D    ; Window: reads=system, writes=video RAM
VCHARWINROM  = $C06E    ; Window: reads=video ROM, writes=system
VCHARWINRAM  = $C06F    ; Window: reads=video RAM, writes=system
VCHARROMSEL  = $C02D    ; Select character ROM for rendering
VCHARRAMSEL  = $C02E    ; Select character RAM for rendering

WINDOW_ADDR  = $2000    ; Character window overlay address
CUSTOM_FONT  = $4000    ; Source address for custom font data

LOADCHAR:
        ; Step 1: Copy custom font from $4000 to window address $2000
        ; (This is just moving data within system RAM)
        LDX #$00
.COPY1: LDA CUSTOM_FONT,X
        STA WINDOW_ADDR,X
        LDA CUSTOM_FONT+$100,X
        STA WINDOW_ADDR+$100,X
        ; ... repeat for all 16 pages (4KB) ...
        INX
        BNE .COPY1

        ; Step 2: Enable window mode - reads from system, writes to video RAM
        STA VCHARWINSYS

        ; Step 3: Copy data through the window into video RAM
        ; LDA reads from system RAM at $2000, STA writes to video RAM
        LDX #$00
.COPY2: LDA WINDOW_ADDR,X
        STA WINDOW_ADDR,X       ; Same address, but write goes to video RAM!
        LDA WINDOW_ADDR+$100,X
        STA WINDOW_ADDR+$100,X
        ; ... repeat for all 16 pages (4KB) ...
        INX
        BNE .COPY2

        ; Step 4: Disable window mode
        STA VCHARWINOFF

        ; Step 5: Switch rendering to use character RAM
        STA VCHARRAMSEL

        ; Done! Characters now render from the custom font.
        RTS
```

---

## 7. Fallback Character Set

### 7.1 Built-in Default

When no character ROM is loaded, the video device can use a built-in fallback:

```csharp
public sealed partial class VideoDevice
{
    private static readonly Lazy<byte[]> FallbackCharacterRom = new(CreateFallbackRom);

    /// <summary>
    /// Gets character data with fallback to built-in set.
    /// </summary>
    public byte GetCharacterScanlineWithFallback(
        byte charCode,
        int scanline,
        bool useAltCharSet)
    {
        if (characterRom != null)
        {
            return GetCharacterScanline(charCode, scanline, useAltCharSet);
        }

        // Use fallback ROM
        int offset = GetCharacterRomOffset(charCode, useAltCharSet) + scanline;
        return FallbackCharacterRom.Value[offset];
    }

    private static byte[] CreateFallbackRom()
    {
        var rom = new byte[CharacterRomSize];

        // Generate basic ASCII characters programmatically
        // This provides readable text even without a ROM file
        GenerateBasicAsciiFont(rom.AsSpan(0, CharacterSetSize));

        // Copy to alternate set (without MouseText for simplicity)
        rom.AsSpan(0, CharacterSetSize).CopyTo(rom.AsSpan(CharacterSetSize));

        return rom;
    }

    private static void GenerateBasicAsciiFont(Span<byte> target)
    {
        // Generate simple 6x7 font within 8x8 cell
        // This is a minimal implementation for fallback purposes

        // Space (0x20, 0xA0)
        // Letters A-Z (0x41-0x5A, 0xC1-0xDA)
        // Digits 0-9 (0x30-0x39, 0xB0-0xB9)
        // etc.

        // Example: Generate 'A' at code 0xC1 (normal 'A')
        int offset = 0xC1 * 8;
        target[offset + 0] = 0b00011000; // $18
        target[offset + 1] = 0b00100100; // $24
        target[offset + 2] = 0b01000010; // $42
        target[offset + 3] = 0b01000010; // $42
        target[offset + 4] = 0b01111110; // $7E
        target[offset + 5] = 0b01000010; // $42
        target[offset + 6] = 0b01000010; // $42
        target[offset + 7] = 0b00000000; // $00

        // ... generate remaining characters ...
    }
}
```

---

## 8. MouseText Characters

### 8.1 MouseText Glyph Table

When ALTCHAR is enabled, character codes $40-$7F display MouseText instead of
flashing characters:

| Code | Glyph | Description                    |
|------|-------|--------------------------------|
| $40  | ◀     | Left arrow (closed)            |
| $41  | ◆     | Diamond                        |
| $42  | ▼     | Down arrow (closed)            |
| $43  | ─     | Horizontal line                |
| $44  | ◀     | Left arrow (open)              |
| $45  | ▶     | Right arrow (closed)           |
| $46  | ╱     | Diagonal forward slash         |
| $47  | ╲     | Diagonal backslash             |
| $48  | ▲     | Up arrow (closed)              |
| $49  | ▲     | Up arrow (open)                |
| $4A  | ▼     | Down arrow (open)              |
| $4B  | ▶     | Right arrow (open)             |
| $4C  | ┌     | Top-left corner                |
| $4D  | ┐     | Top-right corner               |
| $4E  | └     | Bottom-left corner             |
| $4F  | ┘     | Bottom-right corner            |
| $50  | ●     | Closed apple (solid)           |
| $51  | ○     | Open apple (outline)           |
| $52  | →     | Pointer right                  |
| $53  | ↓     | Down arrow                     |
| $54  | ←     | Pointer left                   |
| $55  | ↑     | Up arrow                       |
| $56  | ✓     | Checkmark                      |
| $57  | ◁     | Left arrow                     |
| $58  | ↔     | Left-right arrow               |
| $59  | ↕     | Up-down arrow                  |
| $5A  | ⌘     | Mouse cursor                   |
| $5B  | ⌃     | Control key symbol             |
| $5C  | ↵     | Return arrow                   |
| $5D  | ⌫     | Delete symbol                  |
| $5E  | 📁    | Folder                         |
| $5F  | ⌥     | Option/Command key             |
| $60-$7F | (same pattern continues)        |

### 8.2 MouseText in Character ROM

The alternate character set ROM ($0800-$0FFF) contains MouseText glyphs at the
appropriate offsets:

```csharp
/// <summary>
/// Checks if a character code maps to MouseText in alternate mode.
/// </summary>
public static bool IsMouseTextCode(byte charCode)
{
    return charCode >= 0x40 && charCode < 0x80;
}

/// <summary>
/// Gets the MouseText glyph description for a character code.
/// </summary>
public static string GetMouseTextDescription(byte charCode)
{
    return (charCode & 0x3F) switch
    {
        0x00 => "Left arrow (closed)",
        0x01 => "Diamond",
        0x02 => "Down arrow (closed)",
        // ... etc
        _ => "Unknown"
    };
}
```

---

## 9. Implementation Checklist

### 9.1 VideoDevice Updates

- [ ] Add `PhysicalMemory? characterRom` field
- [ ] Implement `ICharacterRomProvider` interface
- [ ] Add `LoadCharacterRom(ReadOnlySpan<byte>)` method
- [ ] Update `IVideoDevice` interface with character ROM support
- [ ] Add fallback character generation

### 9.2 Profile Configuration

- [ ] Add character-rom to device config schema
- [ ] Update MachineBuilder to load device ROMs
- [ ] Add sample profile with character ROM reference

### 9.3 Display Renderer Updates

- [ ] Update text rendering to use `ICharacterRomProvider`
- [ ] Implement proper inverse/flash/normal handling
- [ ] Add MouseText support for ALTCHAR mode

### 9.4 Testing

- [ ] Unit tests for character ROM loading
- [ ] Unit tests for character bitmap retrieval
- [ ] Unit tests for ALTCHAR switching
- [ ] Visual regression tests for text rendering
- [ ] Integration tests with full machine profile

---

## 10. Unit Test Examples

```csharp
/// <summary>
/// Unit tests for VideoDevice character ROM functionality.
/// </summary>
[TestFixture]
public class VideoDeviceCharacterRomTests
{
    [Test]
    public void LoadCharacterRom_ValidData_SetsRomLoaded()
    {
        var device = new VideoDevice();
        var romData = new byte[VideoDevice.CharacterRomSize];

        device.LoadCharacterRom(romData);

        Assert.That(device.IsCharacterRomLoaded, Is.True);
    }

    [Test]
    public void LoadCharacterRom_InvalidSize_ThrowsArgumentException()
    {
        var device = new VideoDevice();
        var romData = new byte[1024]; // Wrong size

        Assert.Throws<ArgumentException>(() => device.LoadCharacterRom(romData));
    }

    [Test]
    public void GetCharacterScanline_PrimarySet_ReturnsCorrectData()
    {
        var device = new VideoDevice();
        var romData = new byte[VideoDevice.CharacterRomSize];

        // Set up test pattern at character 'A' (0xC1), scanline 0
        int offset = 0xC1 * 8;
        romData[offset] = 0x18; // Expected pattern

        device.LoadCharacterRom(romData);

        byte result = device.GetCharacterScanline(0xC1, 0, useAltCharSet: false);

        Assert.That(result, Is.EqualTo(0x18));
    }

    [Test]
    public void GetCharacterScanline_AlternateSet_UsesSecondHalf()
    {
        var device = new VideoDevice();
        var romData = new byte[VideoDevice.CharacterRomSize];

        // Set different patterns in primary and alternate sets
        int primaryOffset = 0x40 * 8;
        int altOffset = VideoDevice.CharacterSetSize + (0x40 * 8);
        romData[primaryOffset] = 0xAA;
        romData[altOffset] = 0x55;

        device.LoadCharacterRom(romData);

        byte primary = device.GetCharacterScanline(0x40, 0, useAltCharSet: false);
        byte alternate = device.GetCharacterScanline(0x40, 0, useAltCharSet: true);

        Assert.Multiple(() =>
        {
            Assert.That(primary, Is.EqualTo(0xAA));
            Assert.That(alternate, Is.EqualTo(0x55));
        });
    }

    [Test]
    public void GetCharacterBitmap_ReturnsAll8Scanlines()
    {
        var device = new VideoDevice();
        var romData = new byte[VideoDevice.CharacterRomSize];

        // Fill character 0x00 with sequential values
        for (int i = 0; i < 8; i++)
        {
            romData[i] = (byte)i;
        }

        device.LoadCharacterRom(romData);

        var bitmap = device.GetCharacterBitmap(0x00, useAltCharSet: false);

        Assert.Multiple(() =>
        {
            Assert.That(bitmap.Length, Is.EqualTo(8));
            Assert.That(bitmap[0], Is.EqualTo(0));
            Assert.That(bitmap[7], Is.EqualTo(7));
        });
    }

    [Test]
    public void GetCharacterScanline_NoRomLoaded_ReturnsZero()
    {
        var device = new VideoDevice();

        byte result = device.GetCharacterScanline(0xC1, 0, useAltCharSet: false);

        Assert.That(result, Is.EqualTo(0x00));
    }
}
```

---

## Document History

| Version | Date       | Changes                            |
|---------|------------|------------------------------------|
| 1.0     | 2026-01-09 | Initial specification              |
