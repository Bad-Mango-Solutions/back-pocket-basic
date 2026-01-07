# Apple II Video Display Specification

## Document Information

| Field        | Value                                         |
|--------------|-----------------------------------------------|
| Version      | 1.0                                           |
| Date         | 2025-12-28                                    |
| Status       | Initial Draft                                 |
| Applies To   | Pocket2e (Apple IIe), Pocket2c (Apple IIc)    |

---

## 1. Overview

This specification describes the video display systems of the Apple II, Apple IIe, and Apple
IIc computers, as well as the 80-column text card. Understanding these systems is essential
for accurate emulation of the visual output that users expect.

### 1.1 Why Video Emulation Matters

The Apple II's video system is intimately tied to its memory architecture. Unlike modern
systems where video memory is separate, the Apple II uses a **memory-mapped display** where
the CPU directly reads and writes the same memory that the video hardware scans. This design
has several implications:

1. **No frame buffer abstraction**: Changes to display memory are visible immediately (within
   the current scanline's timing constraints).

2. **Interleaved memory layout**: For historical hardware reasons, consecutive screen rows
   are not stored at consecutive memory addresses.

3. **Soft switches control modes**: The display mode (text, lo-res, hi-res) is selected by
   accessing memory-mapped soft switches, not by writing to a "mode register."

4. **Auxiliary memory for 80-column/double-res**: The Apple IIe's extended modes use both
   main and auxiliary memory banks, interleaving columns from each.

### 1.2 Display Timing

The Apple II generates a standard NTSC (or PAL for European models) composite video signal:

| Parameter           | NTSC Value        | PAL Value         |
|---------------------|-------------------|-------------------|
| Horizontal scan     | 15.734 kHz        | 15.625 kHz        |
| Vertical refresh    | 60 Hz             | 50 Hz             |
| Active lines        | 192               | 192               |
| Total lines/frame   | 262               | 312               |
| Horizontal pixels   | 280 (560 DHR)     | 280 (560 DHR)     |
| CPU cycles/line     | 65                | 65                |
| CPU cycles/frame    | 17,030            | 20,280            |

**Emulation note**: For most software, accurate frame timing (1/60 or 1/50 second) is
sufficient. Cycle-exact scanline timing is only needed for software that uses "racing the
beam" techniques for visual effects or copy protection.

---

## 2. Text Display Modes

### 2.1 40-Column Text Mode

The standard Apple II text mode displays 40 columns × 24 rows of characters.

#### 2.1.1 Memory Layout

Text page 1 occupies $0400-$07FF (1KB). The memory is organized into 8 groups of 3 rows
each, with each group separated by 128 bytes:

| Screen Row | Memory Address Range | Base Address |
|------------|----------------------|--------------|
| 0          | $0400-$0427          | $0400        |
| 1          | $0480-$04A7          | $0480        |
| 2          | $0500-$0527          | $0500        |
| 3          | $0580-$05A7          | $0580        |
| 4          | $0600-$0627          | $0600        |
| 5          | $0680-$06A7          | $0680        |
| 6          | $0700-$0727          | $0700        |
| 7          | $0780-$07A7          | $0780        |
| 8          | $0428-$044F          | $0428        |
| 9          | $04A8-$04CF          | $04A8        |
| 10         | $0528-$054F          | $0528        |
| 11         | $05A8-$05CF          | $05A8        |
| 12         | $0628-$064F          | $0628        |
| 13         | $06A8-$06CF          | $06A8        |
| 14         | $0728-$074F          | $0728        |
| 15         | $07A8-$07CF          | $07A8        |
| 16         | $0450-$0477          | $0450        |
| 17         | $04D0-$04F7          | $04D0        |
| 18         | $0550-$0577          | $0550        |
| 19         | $05D0-$05F7          | $05D0        |
| 20         | $0650-$0677          | $0650        |
| 21         | $06D0-$06F7          | $06D0        |
| 22         | $0750-$0777          | $0750        |
| 23         | $07D0-$07F7          | $07D0        |

Text page 2 occupies $0800-$0BFF with the same layout offset by $0400.

#### 2.1.2 Base Address Calculation

```csharp
/// <summary>
/// Calculates the base address for a text screen row.
/// </summary>
/// <param name="row">Row number (0-23).</param>
/// <param name="page">Page number (1 or 2).</param>
/// <returns>Base address for the row.</returns>
public static ushort TextBaseAddress(int row, int page = 1)
{
    int pageBase = page == 1 ? 0x0400 : 0x0800;
    int group = row / 8;       // 0, 1, or 2
    int offset = row % 8;      // 0-7
    return (ushort)(pageBase + (offset * 128) + (group * 40));
}
```

#### 2.1.3 Character Encoding

Each byte in text memory represents one character. The encoding differs slightly between
Apple II models:

| Bit 7 | Bits 6-0  | Apple II    | Apple IIe (Normal) | Apple IIe (Alternate) |
|-------|-----------|-------------|--------------------|-----------------------|
| 0     | $00-$1F   | Inverse @-_ | Inverse @-_        | Inverse @-_           |
| 0     | $20-$3F   | Inverse sp-?| Inverse sp-?       | Inverse sp-?          |
| 0     | $40-$5F   | Flash @-_   | Flash @-_          | MouseText             |
| 0     | $60-$7F   | Flash sp-?  | Flash sp-?         | MouseText             |
| 1     | $00-$1F   | Normal @-_  | Normal @-_         | Normal @-_            |
| 1     | $20-$3F   | Normal sp-? | Normal sp-?        | Normal sp-?           |
| 1     | $40-$5F   | Normal @-_  | Normal @-_         | Normal @-_            |
| 1     | $60-$7F   | Normal @-_  | Lowercase a-z      | Lowercase a-z         |

**Inverse**: White text on black background (colors reversed)
**Flash**: Alternates between normal and inverse at ~1.9 Hz
**MouseText**: Special graphical characters for GUI elements (IIe/IIc only)

#### 2.1.4 Character Generator ROM

The character shapes are defined by an 8×8 pixel matrix stored in the character generator
ROM. Each character occupies 8 bytes (one per scanline), with each bit representing one
pixel (1 = lit, 0 = dark).

```csharp
/// <summary>
/// Represents the character generator ROM.
/// </summary>
public interface ICharacterGenerator
{
    /// <summary>
    /// Gets the pixel row for a character.
    /// </summary>
    /// <param name="charCode">Character code (0-255).</param>
    /// <param name="scanline">Scanline within character (0-7).</param>
    /// <param name="flash">Current flash state (true = inverted).</param>
    /// <param name="altCharSet">Use alternate character set (MouseText).</param>
    /// <returns>8 pixels as bits, MSB = leftmost pixel.</returns>
    byte GetCharacterRow(byte charCode, int scanline, bool flash, bool altCharSet);
}
```

### 2.2 80-Column Text Mode (Apple IIe/IIc)

The 80-column mode displays 80 columns × 24 rows by interleaving characters from main and
auxiliary memory.

#### 2.2.1 Memory Organization

- **Odd columns (1, 3, 5, ...)**: Read from main memory ($0400-$07FF)
- **Even columns (0, 2, 4, ...)**: Read from auxiliary memory ($0400-$07FF)

The interleaving happens at the character level:

```
Screen column:  0   1   2   3   4   5  ...  78  79
Memory source:  AUX MN  AUX MN  AUX MN  ... AUX MN
Memory offset:  0   0   1   1   2   2  ...  39  39
```

#### 2.2.2 Enabling 80-Column Mode

80-column mode requires several soft switches:

```csharp
// Enable 80-column mode
bus.Read(0xC00D);   // 80COLON - Enable 80-column display
bus.Read(0xC00F);   // ALTCHARON - Enable alternate character set (optional)
bus.Read(0xC001);   // 80STOREON - PAGE2 selects aux memory for writes

// Switch back to 40-column
bus.Read(0xC00C);   // 80COLOFF - Disable 80-column display
```

#### 2.2.3 The 80-Column Card (Slot 3)

On the original Apple II and II+, 80-column capability required an expansion card in slot 3.
The Apple IIe Enhanced and IIc have this functionality built-in, but the addressing remains
compatible with the slot 3 card:

- **$C300-$C3FF**: 80-column firmware ROM
- **$C800-$CFFF**: Extended firmware (when slot 3 is selected)

The internal ROM soft switch ($C00A/$C00B for slot 3) controls whether the built-in firmware
or an external card's ROM is visible.

---

## 3. Graphics Modes

### 3.1 Lo-Res Graphics (40×48)

Lo-res mode provides a 40×48 grid of colored blocks, with each block being 7×4 pixels.

#### 3.1.1 Memory Sharing with Text

Lo-res graphics shares memory with text mode ($0400-$07FF for page 1, $0800-$0BFF for page 2).
Each byte represents two vertically stacked blocks:

- **Low nibble (bits 0-3)**: Top block color
- **High nibble (bits 4-7)**: Bottom block color

#### 3.1.2 Color Palette

| Value | Color         | NTSC Approximation |
|-------|---------------|--------------------|
| 0     | Black         | #000000            |
| 1     | Magenta       | #DD0033            |
| 2     | Dark Blue     | #000099            |
| 3     | Purple        | #DD22DD            |
| 4     | Dark Green    | #007722            |
| 5     | Gray 1        | #555555            |
| 6     | Medium Blue   | #2222FF            |
| 7     | Light Blue    | #66AAFF            |
| 8     | Brown         | #885500            |
| 9     | Orange        | #FF6600            |
| 10    | Gray 2        | #AAAAAA            |
| 11    | Pink          | #FF9988            |
| 12    | Light Green   | #11DD00            |
| 13    | Yellow        | #FFFF00            |
| 14    | Aqua          | #44FF99            |
| 15    | White         | #FFFFFF            |

**Note**: Actual colors vary significantly depending on the monitor and NTSC artifact
handling. The values above are approximate.

### 3.2 Hi-Res Graphics (280×192)

Hi-res mode provides 280 horizontal pixels × 192 vertical lines, with limited color
capability due to NTSC artifact coloring.

#### 3.2.1 Memory Layout

Hi-res page 1 occupies $2000-$3FFF (8KB), page 2 is at $4000-$5FFF. The memory layout is
similar to text mode but with more bytes per row:

```csharp
/// <summary>
/// Calculates the base address for a hi-res screen row.
/// </summary>
/// <param name="row">Row number (0-191).</param>
/// <param name="page">Page number (1 or 2).</param>
/// <returns>Base address for the row.</returns>
public static ushort HiResBaseAddress(int row, int page = 1)
{
    int pageBase = page == 1 ? 0x2000 : 0x4000;
    int group = row / 64;          // 0, 1, or 2
    int subRow = (row % 64) / 8;   // 0-7
    int scanLine = row % 8;        // 0-7
    return (ushort)(pageBase + (scanLine * 1024) + (subRow * 128) + (group * 40));
}
```

#### 3.2.2 Pixel Encoding

Each byte represents 7 pixels (bits 0-6) plus a palette selector (bit 7):

```
Bit 7: Palette select (0 = violet/green, 1 = blue/orange)
Bits 0-6: 7 pixels, LSB is leftmost
```

#### 3.2.3 NTSC Artifact Coloring

The Apple II's famous color graphics are actually an artifact of how the NTSC signal is
generated. Individual pixels alternate between "on" and "off" states at the NTSC color
burst frequency, creating the perception of color:

| Bit Pattern | Even Column | Odd Column  | Palette 0    | Palette 1    |
|-------------|-------------|-------------|--------------|--------------|
| 0           | Off         | Off         | Black        | Black        |
| 1           | On          | Off         | Violet       | Blue         |
| 1           | Off         | On          | Green        | Orange       |
| 1           | On          | On          | White        | White        |

**Emulation approaches**:
1. **Monochrome**: Treat each bit as a white/black pixel
2. **Color (simple)**: Apply color based on column parity and palette bit
3. **Color (accurate)**: Simulate NTSC encoding/decoding for authentic artifact colors

### 3.3 Double Lo-Res (80×48) - Apple IIe/IIc

Double lo-res mode provides 80×48 colored blocks by interleaving main and auxiliary memory.

#### 3.3.1 Enabling Double Lo-Res

```csharp
bus.Read(0xC00D);   // 80COLON
bus.Read(0xC05E);   // DHIRESON (also AN3 off)
bus.Read(0xC050);   // Graphics mode
bus.Read(0xC056);   // Lo-res mode
```

#### 3.3.2 Memory Layout

Similar to 80-column text, odd columns come from main memory and even columns from auxiliary:

- **Even columns**: Auxiliary $0400-$07FF
- **Odd columns**: Main $0400-$07FF

### 3.4 Double Hi-Res (560×192) - Apple IIe/IIc

Double hi-res provides 560 horizontal pixels (or 140 colors) using both memory banks.

#### 3.4.1 Enabling Double Hi-Res

```csharp
bus.Read(0xC00D);   // 80COLON
bus.Read(0xC05E);   // DHIRESON
bus.Read(0xC050);   // Graphics mode
bus.Read(0xC057);   // Hi-res mode
```

#### 3.4.2 Memory Layout

- **Odd byte groups**: Main $2000-$3FFF
- **Even byte groups**: Auxiliary $2000-$3FFF

Each pair of bytes (one from aux, one from main) provides 14 pixels:

```
AUX byte bits 0-6 | MAIN byte bits 0-6
= 7 pixels        | 7 pixels
= 14 pixels total
```

#### 3.4.3 Double Hi-Res Color

In color mode, groups of 4 bits define one of 16 colors:

```
Pixel position:    0   1   2   3   4   5   6   7   8   9  10  11  12  13
Aux byte bits:     0   1   2   3   4   5   6   -   -   -   -   -   -   -
Main byte bits:    -   -   -   -   -   -   -   0   1   2   3   4   5   6

Color pixel:       [  0  ] [  1  ] [  2  ] [  3  ] [  4  ] [  5  ] [  6  ]
                   aux     aux     aux     aux     main    main    main
                   0-3     1-4     2-5     3-6     0-3     1-4     2-5
```

This sliding window approach creates 140 color pixels (560 monochrome) per line.

---

## 4. Mixed Mode Display

### 4.1 Text Window in Graphics Mode

The Apple II can display a 4-line text window at the bottom of the screen while showing
graphics in the upper portion:

```csharp
bus.Read(0xC053);   // MIXSET - Enable mixed mode (4 lines text at bottom)
bus.Read(0xC052);   // MIXCLR - Full screen graphics
```

In mixed mode:
- **Rows 0-19 (160 lines)**: Graphics mode
- **Rows 20-23 (32 lines)**: Text mode

---

## 5. Soft Switch Summary

### 5.1 Display Mode Switches

| Address | Name      | Write | Effect                              |
|---------|-----------|-------|-------------------------------------|
| $C050   | TXTCLR    | R/W   | Graphics mode                       |
| $C051   | TXTSET    | R/W   | Text mode                           |
| $C052   | MIXCLR    | R/W   | Full screen                         |
| $C053   | MIXSET    | R/W   | Mixed mode (4-line text window)     |
| $C054   | PAGE1     | R/W   | Display page 1                      |
| $C055   | PAGE2     | R/W   | Display page 2                      |
| $C056   | LORES     | R/W   | Lo-res graphics                     |
| $C057   | HIRES     | R/W   | Hi-res graphics                     |

### 5.2 80-Column/Double Hi-Res Switches (IIe/IIc)

| Address | Name       | Write | Effect                              |
|---------|------------|-------|-------------------------------------|
| $C00C   | 80COLOFF   | W     | 40-column display                   |
| $C00D   | 80COLON    | W     | 80-column display                   |
| $C00E   | ALTCHAROFF | W     | Primary character set               |
| $C00F   | ALTCHARON  | W     | Alternate character set (MouseText) |
| $C05E   | DHIRESOFF  | R/W   | Single hi-res (AN3 off)             |
| $C05F   | DHIRESON   | R/W   | Double hi-res (AN3 on)              |

### 5.3 Status Reads (IIe/IIc)

| Address | Name      | Read  | Bit 7 Meaning                       |
|---------|-----------|-------|-------------------------------------|
| $C019   | RDVBLBAR  | R     | 0 = Vertical blanking               |
| $C01A   | RDTEXT    | R     | 1 = Text mode                       |
| $C01B   | RDMIXED   | R     | 1 = Mixed mode                      |
| $C01C   | RDPAGE2   | R     | 1 = Page 2 displayed                |
| $C01D   | RDHIRES   | R     | 1 = Hi-res mode                     |
| $C01E   | RDALTCHAR | R     | 1 = Alternate character set         |
| $C01F   | RD80COL   | R     | 1 = 80-column mode                  |

---

## 6. Video Controller Interface

```csharp
/// <summary>
/// Interface for the Apple II video display controller.
/// </summary>
public interface IVideoController : IScheduledDevice
{
    // ??? Mode State ?????????????????????????????????????????????????????
    
    /// <summary>Gets whether text mode is active (vs. graphics).</summary>
    bool IsTextMode { get; }
    
    /// <summary>Gets whether mixed mode is active (4-line text window).</summary>
    bool IsMixedMode { get; }
    
    /// <summary>Gets whether hi-res mode is active (vs. lo-res).</summary>
    bool IsHiResMode { get; }
    
    /// <summary>Gets whether page 2 is displayed.</summary>
    bool IsPage2 { get; }
    
    /// <summary>Gets whether 80-column mode is active.</summary>
    bool Is80ColumnMode { get; }
    
    /// <summary>Gets whether double hi-res mode is active.</summary>
    bool IsDoubleHiResMode { get; }
    
    /// <summary>Gets whether alternate character set (MouseText) is active.</summary>
    bool IsAltCharSet { get; }
    
    // ??? Mode Switching ?????????????????????????????????????????????????
    
    /// <summary>Sets text mode (returns floating bus value).</summary>
    byte SetText();
    
    /// <summary>Sets graphics mode (returns floating bus value).</summary>
    byte SetGraphics();
    
    /// <summary>Sets full-screen mode (returns floating bus value).</summary>
    byte SetFullScreen();
    
    /// <summary>Sets mixed mode (returns floating bus value).</summary>
    byte SetMixed();
    
    /// <summary>Sets page 1 display (returns floating bus value).</summary>
    byte SetPage1();
    
    /// <summary>Sets page 2 display (returns floating bus value).</summary>
    byte SetPage2();
    
    /// <summary>Sets lo-res graphics (returns floating bus value).</summary>
    byte SetLoRes();
    
    /// <summary>Sets hi-res graphics (returns floating bus value).</summary>
    byte SetHiRes();
    
    // ??? 80-Column Support (IIe/IIc) ????????????????????????????????????
    
    /// <summary>Enables 80-column mode.</summary>
    void Enable80Column();
    
    /// <summary>Disables 80-column mode.</summary>
    void Disable80Column();
    
    /// <summary>Enables alternate character set (MouseText).</summary>
    void EnableAltCharSet();
    
    /// <summary>Disables alternate character set.</summary>
    void DisableAltCharSet();
    
    /// <summary>Enables double hi-res mode.</summary>
    void EnableDoubleHiRes();
    
    /// <summary>Disables double hi-res mode.</summary>
    void DisableDoubleHiRes();
    
    // ??? Rendering ??????????????????????????????????????????????????????
    
    /// <summary>Gets whether the display is currently in vertical blanking.</summary>
    bool IsVerticalBlanking { get; }
    
    /// <summary>Gets the current scanline number (0-261 NTSC, 0-311 PAL).</summary>
    int CurrentScanline { get; }
    
    /// <summary>
    /// Renders a frame to the provided pixel buffer.
    /// </summary>
    /// <param name="buffer">Pixel buffer (280×192 or 560×192 ARGB).</param>
    /// <param name="width">Buffer width in pixels.</param>
    /// <param name="height">Buffer height in pixels.</param>
    void RenderFrame(Span<uint> buffer, int width, int height);
    
    // ??? Events ?????????????????????????????????????????????????????????
    
    /// <summary>Raised at the start of vertical blanking.</summary>
    event Action? VBlankStart;
    
    /// <summary>Raised at the end of vertical blanking.</summary>
    event Action? VBlankEnd;
}
```

---

## 7. Implementation Notes

### 7.1 Rendering Strategy

For most applications, rendering once per frame (at VBlank) is sufficient:

```csharp
public void RenderFrame(Span<uint> buffer, int width, int height)
{
    if (IsTextMode)
        RenderText(buffer, width, height);
    else if (IsHiResMode)
        RenderHiRes(buffer, width, height);
    else
        RenderLoRes(buffer, width, height);
    
    if (IsMixedMode && !IsTextMode)
        RenderTextWindow(buffer, width, height);
}
```

### 7.2 Flash Timing

The flash rate for inverse/flash characters is approximately 1.9 Hz (every 16 frames at
60 Hz). Implement this with a frame counter:

```csharp
private int _flashCounter;
private bool _flashState;

public void OnVBlank()
{
    _flashCounter++;
    if (_flashCounter >= 16)
    {
        _flashCounter = 0;
        _flashState = !_flashState;
    }
}
```

### 7.3 NTSC Color Simulation

For accurate NTSC artifact color simulation, consider using a shader or lookup table that
accounts for the phase relationship between adjacent pixels.

---

## Document History

| Version | Date       | Changes                            |
|---------|------------|------------------------------------|
| 1.0     | 2025-12-28 | Initial specification              |

---

## Appendix A: Bus Architecture Integration

This appendix provides implementation guidance for integrating the video display system
with the emulator's bus architecture as defined in the Architecture Specification.

### A.1 IBusTarget Implementation

The video controller should implement `IBusTarget` for soft switch access:

```csharp
/// <summary>
/// Video controller soft switch handler implementing IBusTarget.
/// </summary>
public sealed class VideoSoftSwitches : IBusTarget
{
    private readonly IVideoController _video;
    
    /// <inheritdoc/>
    public TargetCaps Capabilities => TargetCaps.SideEffects;
    
    /// <inheritdoc/>
    public byte Read8(Addr physicalAddress, in BusAccess access)
    {
        // Skip side effects for debug/peek access
        if (access.IsSideEffectFree)
            return ReadStatusOnly(physicalAddress);
        
        ushort offset = (ushort)(physicalAddress & 0x00FF);
        return offset switch
        {
            0x50 => _video.SetGraphics(),
            0x51 => _video.SetText(),
            0x52 => _video.SetFullScreen(),
            0x53 => _video.SetMixed(),
            0x54 => _video.SetPage1(),
            0x55 => _video.SetPage2(),
            0x56 => _video.SetLoRes(),
            0x57 => _video.SetHiRes(),
            // Status reads (IIe)
            0x19 => (byte)(_video.IsVerticalBlanking ? 0x00 : 0x80),
            0x1A => (byte)(_video.IsTextMode ? 0x80 : 0x00),
            0x1B => (byte)(_video.IsMixedMode ? 0x80 : 0x00),
            0x1C => (byte)(_video.IsPage2 ? 0x80 : 0x00),
            0x1D => (byte)(_video.IsHiResMode ? 0x80 : 0x00),
            0x1F => (byte)(_video.Is80ColumnMode ? 0x80 : 0x00),
            _ => FloatingBus()
        };
    }
    
    /// <inheritdoc/>
    public void Write8(Addr physicalAddress, byte value, in BusAccess access)
    {
        // Soft switches respond to writes same as reads
        Read8(physicalAddress, access);
    }
    
    private byte FloatingBus() => 0xFF;
}
```

### A.2 Composite Page Integration

The video soft switches are part of the Apple II I/O page composite target. Register them
with the `AppleIIIOPage` composite:

```csharp
public sealed class AppleIIIOPage : ICompositeTarget
{
    private readonly VideoSoftSwitches _videoSwitches;
    // ...other handlers...
    
    /// <inheritdoc/>
    public IBusTarget? ResolveTarget(Addr offset, AccessIntent intent)
    {
        return offset switch
        {
            // Video mode switches ($C050-$C057)
            >= 0x50 and <= 0x57 => _videoSwitches,
            // Video status reads ($C019-$C01F)
            >= 0x19 and <= 0x1F => _videoSwitches,
            // 80-column switches ($C00C-$C00F) - IIe only
            >= 0x0C and <= 0x0F => _videoSwitches,
            // Double hi-res ($C05E-$C05F)
            >= 0x5E and <= 0x5F => _videoSwitches,
            // ...other handlers...
            _ => null
        };
    }
    
    /// <inheritdoc/>
    public RegionTag GetSubRegionTag(Addr offset)
    {
        return offset switch
        {
            >= 0x50 and <= 0x5F => RegionTag.VideoMode,
            >= 0x19 and <= 0x1F => RegionTag.VideoStatus,
            >= 0x0C and <= 0x0F => RegionTag.Video80Col,
            _ => RegionTag.Unknown
        };
    }
}
```

### A.3 Scheduler Integration for VBlank

The video controller participates in the scheduler for scanline timing:

```csharp
public sealed class VideoController : IScheduledDevice, ISchedulable
{
    private readonly IScheduler _scheduler;
    private const ulong CyclesPerScanline = 65;
    private const int ScanlinesPerFrame = 262;  // NTSC
    
    /// <inheritdoc/>
    public void Initialize(IEventContext context)
    {
        _scheduler = context.Scheduler;
        _scheduler.ScheduleAfter(this, CyclesPerScanline);
    }
    
    /// <inheritdoc/>
    public ulong Execute(ulong currentCycle)
    {
        _currentScanline++;
        
        if (_currentScanline >= ScanlinesPerFrame)
        {
            _currentScanline = 0;
            _flashCounter++;
            if (_flashCounter >= 16)
            {
                _flashCounter = 0;
                _flashState = !_flashState;
            }
        }
        
        // Check for VBlank boundaries
        if (_currentScanline == 192)
            VBlankStart?.Invoke();
        else if (_currentScanline == 0)
            VBlankEnd?.Invoke();
        
        // Schedule next scanline
        _scheduler.ScheduleAfter(this, CyclesPerScanline);
        return CyclesPerScanline;
    }
}
```

### A.4 Device Registry

Register the video controller with the device registry for observability:

```csharp
public void RegisterVideoDevice(IDeviceRegistry registry)
{
    int deviceId = registry.GenerateId();
    registry.Register(
        deviceId,
        DevicePageId.CreateCompatIO(guestId: 0, page: 0x05),  // Video page
        kind: "VideoController",
        name: "Apple IIe Video",
        wiringPath: "main/video");
}
```

### A.5 Memory Region Configuration

Configure the video memory regions in the region manager:

```csharp
public void ConfigureVideoMemory(IRegionManager regions, RamTarget mainRam)
{
    // Text page 1 ($0400-$07FF)
    regions.Map(new MemoryRegion(
        baseAddress: 0x0400,
        size: 0x0400,
        tag: RegionTag.TextPage1,
        target: mainRam));
    
    // Text page 2 ($0800-$0BFF)
    regions.Map(new MemoryRegion(
        baseAddress: 0x0800,
        size: 0x0400,
        tag: RegionTag.TextPage2,
        target: mainRam));
    
    // Hi-res page 1 ($2000-$3FFF)
    regions.Map(new MemoryRegion(
        baseAddress: 0x2000,
        size: 0x2000,
        tag: RegionTag.HiResPage1,
        target: mainRam));
    
    // Hi-res page 2 ($4000-$5FFF)
    regions.Map(new MemoryRegion(
        baseAddress: 0x4000,
        size: 0x2000,
        tag: RegionTag.HiResPage2,
        target: mainRam));
}
```

### A.6 BusAccess Usage in Video

When the video controller needs to read display memory for rendering, use proper
`BusAccess` semantics:

```csharp
public void RenderFrame(IMemoryBus bus, Span<uint> pixels)
{
    var access = new BusAccess(
        Address: 0,
        Value: 0,
        WidthBits: 8,
        Mode: CpuMode.Compat,
        EmulationFlag: true,
        Intent: AccessIntent.DmaRead,  // Video is like DMA
        SourceId: _deviceId,
        Cycle: _scheduler.CurrentCycle,
        Flags: AccessFlags.NoSideEffects);  // Reading for display only
    
    if (IsTextMode)
        RenderTextMode(bus, pixels, access);
    else if (IsHiResMode)
        RenderHiResMode(bus, pixels, access);
    else
        RenderLoResMode(bus, pixels, access);
}

private void RenderTextMode(IMemoryBus bus, Span<uint> pixels, BusAccess access)
{
    for (int row = 0; row < 24; row++)
    {
        ushort baseAddr = TextBaseAddress(row, IsPage2 ? 2 : 1);
        
        for (int col = 0; col < 40; col++)
        {
            var charAccess = access with { Address = (Addr)(baseAddr + col) };
            byte charCode = bus.Read8(charAccess).Value;
            RenderCharacter(charCode, row, col, pixels);
        }
    }
}
```

### A.7 Trap Handler for Video ROM Routines

Video-related ROM routines can be trapped for performance:

```csharp
public static class VideoTraps
{
    /// <summary>
    /// Trap handler for HOME ($FC58) - clear screen and home cursor.
    /// </summary>
    public static TrapResult HomeHandler(ICpu cpu, IMemoryBus bus, IEventContext context)
    {
        var access = new BusAccess(
            Address: 0x0400,
            Value: 0xA0,  // Space character
            WidthBits: 8,
            Mode: CpuMode.Compat,
            EmulationFlag: true,
            Intent: AccessIntent.DataWrite,
            SourceId: cpu.DeviceId,
            Cycle: context.Scheduler.CurrentCycle,
            Flags: AccessFlags.None);
        
        // Clear text page
        for (ushort addr = 0x0400; addr < 0x0800; addr++)
        {
            bus.Write8(access with { Address = addr });
        }
        
        // Home cursor (set CV=0, CH=0)
        bus.Write8(access with { Address = 0x25, Value = 0 });  // CV
        bus.Write8(access with { Address = 0x24, Value = 0 });  // CH
        
        return new TrapResult(
            Handled: true,
            CyclesConsumed: new Cycle(2000),  // Approximate
            ReturnAddress: null);
    }
}
```

## Appendix B: Frame Buffer Construction for Host UI

This appendix provides implementation guidance for rendering Apple II video output to
a host display canvas, specifically targeting the Avalonia-based emulator UI.

### B.1 Frame Buffer Overview

The Apple II video controller operates on guest memory, reading character codes, pixel data,
and soft switch states to determine what to display. The host UI needs a pixel buffer that
can be rendered efficiently to screen. This appendix describes the process of converting
guest video memory into a host-compatible frame buffer.

#### B.1.1 Frame Buffer Dimensions

| Mode            | Native Resolution | Recommended Buffer | Aspect Ratio |
|-----------------|-------------------|-------------------|--------------|
| Text 40         | 280×192           | 280×192 or 560×384 | 4:3          |
| Text 80         | 560×192           | 560×192 or 560×384 | 4:3          |
| Lo-Res          | 40×48             | 280×192           | 4:3          |
| Hi-Res          | 280×192           | 280×192 or 560×384 | 4:3          |
| Double Hi-Res   | 560×192           | 560×192 or 560×384 | 4:3          |
| Mixed Mode      | varies            | 280×192 or 560×192 | 4:3          |

**Recommendation**: Use a 560×384 buffer for all modes. This allows:
- Integer 2× scaling in both dimensions for 280×192 modes
- Native resolution for 560×192 modes with 2× vertical scaling
- Consistent canvas size regardless of mode switching

#### B.1.2 Pixel Format

For Avalonia/SkiaSharp integration, use 32-bit ARGB (or BGRA on some platforms):

```csharp
/// <summary>
/// Converts an Apple II color index to 32-bit ARGB.
/// </summary>
/// <param name="colorIndex">Apple II color (0-15).</param>
/// <returns>32-bit ARGB pixel value.</returns>
public static uint AppleIIColorToArgb(int colorIndex)
{
    // NTSC artifact color approximations
    ReadOnlySpan<uint> palette =
    [
        0xFF000000,  // 0: Black
        0xFFDD0033,  // 1: Magenta
        0xFF000099,  // 2: Dark Blue
        0xFFDD22DD,  // 3: Purple (Violet)
        0xFF007722,  // 4: Dark Green
        0xFF555555,  // 5: Gray 1
        0xFF2222FF,  // 6: Medium Blue
        0xFF66AAFF,  // 7: Light Blue
        0xFF885500,  // 8: Brown
        0xFFFF6600,  // 9: Orange
        0xFFAAAAAA,  // 10: Gray 2
        0xFFFF9988,  // 11: Pink
        0xFF11DD00,  // 12: Light Green
        0xFFFFFF00,  // 13: Yellow
        0xFF44FF99,  // 14: Aqua
        0xFFFFFFFF,  // 15: White
    ];
    
    return palette[colorIndex & 0x0F];
}
```

### B.2 Rendering Pipeline Architecture

The rendering pipeline connects the video controller to the host display surface:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        Guest System (Emulator Core)                      │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────────┐ │
│  │ Main RAM    │  │ Aux RAM     │  │ Char Gen    │  │ Soft Switches   │ │
│  │ $0400-$07FF │  │ $0400-$07FF │  │ ROM         │  │ Text/GR/HGR/etc │ │
│  │ $2000-$3FFF │  │ $2000-$3FFF │  │             │  │                 │ │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘  └────────┬────────┘ │
│         │                │                │                   │          │
│         └────────────────┴────────────────┴───────────────────┘          │
│                                    │                                     │
│                           ┌────────▼────────┐                            │
│                           │ IVideoController │                           │
│                           │                  │                           │
│                           │ • Mode state     │                           │
│                           │ • Flash state    │                           │
│                           │ • RenderFrame()  │                           │
│                           └────────┬─────────┘                           │
└────────────────────────────────────┼─────────────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                           IDisplayService                               │
│  ┌─────────────────────────────────────────────────────────────────────┐ │
│  │                      Frame Buffer (uint[])                          │ │
│  │                                                                     │ │
│  │    560×384 pixels @ 32-bit ARGB = 860,160 bytes (~840 KB)          │ │
│  │                                                                     │ │
│  └─────────────────────────────────────────────────────────────────────┘ │
│                                    │                                     │
│                                    ▼                                     │
│  ┌─────────────────────────────────────────────────────────────────────┐ │
│  │                      Post-Processing                                │ │
│  │  • Scanline effects       • Color correction                        │ │
│  │  • CRT curvature         • Phosphor simulation                      │ │
│  └─────────────────────────────────────────────────────────────────────┘ │
└────────────────────────────────────┼─────────────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                        Host UI (Avalonia/SkiaSharp)                      │
│  ┌─────────────────────────────────────────────────────────────────────┐ │
│  │                      WriteableBitmap                                │ │
│  │                                                                     │ │
│  │    Scaled to window size with chosen scaling mode                   │ │
│  │                                                                     │ │
│  └─────────────────────────────────────────────────────────────────────┘ │
│                                    │                                     │
│                                    ▼                                     │
│  ┌─────────────────────────────────────────────────────────────────────┐ │
│  │                      VideoDisplay Control                           │ │
│  │                      (Avalonia UserControl)                         │ │
│  └─────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘
```

### B.3 Mode-Specific Rendering

#### B.3.1 Text Mode Rendering (40/80 Column)

Text mode renders characters from text memory using the character generator ROM:

```csharp
/// <summary>
/// Renders 40-column text mode to the frame buffer.
/// </summary>
/// <param name="buffer">Target pixel buffer (560×384 recommended).</param>
/// <param name="width">Buffer width in pixels.</param>
/// <param name="height">Buffer height in pixels.</param>
/// <param name="mainMemory">Main RAM ($0400-$07FF region).</param>
/// <param name="charGen">Character generator ROM.</param>
/// <param name="page2">True to use text page 2.</param>
/// <param name="flashState">Current flash state (toggles ~1.9 Hz).</param>
/// <param name="altCharSet">True for alternate character set (MouseText).</param>
public void RenderText40(
    Span<uint> buffer,
    int width,
    int height,
    ReadOnlySpan<byte> mainMemory,
    ICharacterGenerator charGen,
    bool page2,
    bool flashState,
    bool altCharSet)
{
    int pageBase = page2 ? 0x0800 : 0x0400;
    
    // Scale factors for 560×384 target
    int scaleX = width / 280;   // 2 for 560 width
    int scaleY = height / 192;  // 2 for 384 height
    int charWidth = 7 * scaleX;
    int charHeight = 8 * scaleY;
    
    for (int row = 0; row < 24; row++)
    {
        int rowBase = TextBaseAddress(row, page2 ? 2 : 1);
        int pixelY = row * charHeight;
        
        for (int col = 0; col < 40; col++)
        {
            byte charCode = mainMemory[rowBase - pageBase + col];
            int pixelX = col * charWidth;
            
            // Render character (8 scanlines)
            for (int scanline = 0; scanline < 8; scanline++)
            {
                byte pixels = charGen.GetCharacterRow(
                    charCode, scanline, flashState, altCharSet);
                
                // Render 7 pixels per scanline
                for (int bit = 0; bit < 7; bit++)
                {
                    bool lit = (pixels & (0x40 >> bit)) != 0;
                    uint color = lit ? 0xFFFFFFFF : 0xFF000000;
                    
                    // Scale up to target resolution
                    for (int sy = 0; sy < scaleY; sy++)
                    {
                        for (int sx = 0; sx < scaleX; sx++)
                        {
                            int bufferX = pixelX + (bit * scaleX) + sx;
                            int bufferY = pixelY + (scanline * scaleY) + sy;
                            buffer[(bufferY * width) + bufferX] = color;
                        }
                    }
                }
            }
        }
    }
}

/// <summary>
/// Renders 80-column text mode to the frame buffer.
/// </summary>
/// <param name="buffer">Target pixel buffer.</param>
/// <param name="width">Buffer width in pixels (560 recommended).</param>
/// <param name="height">Buffer height in pixels.</param>
/// <param name="mainMemory">Main RAM.</param>
/// <param name="auxMemory">Auxiliary RAM.</param>
/// <param name="charGen">Character generator ROM.</param>
/// <param name="page2">True to use text page 2.</param>
/// <param name="flashState">Current flash state.</param>
/// <param name="altCharSet">True for alternate character set.</param>
public void RenderText80(
    Span<uint> buffer,
    int width,
    int height,
    ReadOnlySpan<byte> mainMemory,
    ReadOnlySpan<byte> auxMemory,
    ICharacterGenerator charGen,
    bool page2,
    bool flashState,
    bool altCharSet)
{
    int pageBase = page2 ? 0x0800 : 0x0400;
    int scaleY = height / 192;
    int charHeight = 8 * scaleY;
    
    for (int row = 0; row < 24; row++)
    {
        int rowBase = TextBaseAddress(row, page2 ? 2 : 1);
        int pixelY = row * charHeight;
        
        for (int col = 0; col < 80; col++)
        {
            // Odd columns from main, even columns from aux
            int memCol = col / 2;
            ReadOnlySpan<byte> source = (col & 1) == 0 ? auxMemory : mainMemory;
            byte charCode = source[rowBase - pageBase + memCol];
            
            int pixelX = col * 7;  // 7 pixels per character at native res
            
            for (int scanline = 0; scanline < 8; scanline++)
            {
                byte pixels = charGen.GetCharacterRow(
                    charCode, scanline, flashState, altCharSet);
                
                for (int bit = 0; bit < 7; bit++)
                {
                    bool lit = (pixels & (0x40 >> bit)) != 0;
                    uint color = lit ? 0xFFFFFFFF : 0xFF000000;
                    
                    for (int sy = 0; sy < scaleY; sy++)
                    {
                        int bufferX = pixelX + bit;
                        int bufferY = pixelY + (scanline * scaleY) + sy;
                        buffer[(bufferY * width) + bufferX] = color;
                    }
                }
            }
        }
    }
}
```

#### B.3.2 Lo-Res Graphics Rendering

Lo-res mode displays 40×48 colored blocks:

```csharp
/// <summary>
/// Renders lo-res graphics mode to the frame buffer.
/// </summary>
/// <param name="buffer">Target pixel buffer.</param>
/// <param name="width">Buffer width in pixels.</param>
/// <param name="height">Buffer height in pixels.</param>
/// <param name="memory">Main RAM ($0400-$07FF region).</param>
/// <param name="page2">True to use page 2.</param>
/// <param name="mixedMode">True to render only top 40 rows (for mixed mode).</param>
public void RenderLoRes(
    Span<uint> buffer,
    int width,
    int height,
    ReadOnlySpan<byte> memory,
    bool page2,
    bool mixedMode)
{
    int pageBase = page2 ? 0x0800 : 0x0400;
    int maxGraphicsRows = mixedMode ? 40 : 48;  // Mixed mode: only top 40 rows
    
    // Block dimensions scaled to target
    int blockWidth = width / 40;    // 14 for 560 width
    int blockHeight = height / 48;  // 8 for 384 height
    
    for (int loRow = 0; loRow < maxGraphicsRows; loRow++)
    {
        // Each text row = 2 lo-res rows
        int textRow = loRow / 2;
        bool topBlock = (loRow & 1) == 0;
        
        int rowBase = TextBaseAddress(textRow, page2 ? 2 : 1);
        int pixelY = loRow * blockHeight;
        
        for (int col = 0; col < 40; col++)
        {
            byte data = memory[rowBase - pageBase + col];
            int colorIndex = topBlock ? (data & 0x0F) : ((data >> 4) & 0x0F);
            uint color = AppleIIColorToArgb(colorIndex);
            
            int pixelX = col * blockWidth;
            
            // Fill block
            for (int y = 0; y < blockHeight; y++)
            {
                for (int x = 0; x < blockWidth; x++)
                {
                    buffer[((pixelY + y) * width) + pixelX + x] = color;
                }
            }
        }
    }
}
```

#### B.3.3 Hi-Res Graphics Rendering

Hi-res mode requires handling NTSC artifact coloring:

```csharp
/// <summary>
/// Renders hi-res graphics with NTSC artifact coloring.
/// </summary>
/// <param name="buffer">Target pixel buffer.</param>
/// <param name="width">Buffer width in pixels.</param>
/// <param name="height">Buffer height in pixels.</param>
/// <param name="memory">Main RAM ($2000-$3FFF or $4000-$5FFF).</param>
/// <param name="page2">True to use page 2.</param>
/// <param name="mixedMode">True to render only top 160 lines.</param>
/// <param name="colorMode">True for color, false for monochrome.</param>
public void RenderHiRes(
    Span<uint> buffer,
    int width,
    int height,
    ReadOnlySpan<byte> memory,
    bool page2,
    bool mixedMode,
    bool colorMode)
{
    int pageBase = page2 ? 0x4000 : 0x2000;
    int maxLines = mixedMode ? 160 : 192;
    int scaleX = width / 280;
    int scaleY = height / 192;
    
    for (int line = 0; line < maxLines; line++)
    {
        int lineBase = HiResBaseAddress(line, page2 ? 2 : 1) - pageBase;
        int pixelY = line * scaleY;
        
        for (int byteCol = 0; byteCol < 40; byteCol++)
        {
            byte data = memory[lineBase + byteCol];
            bool palette = (data & 0x80) != 0;  // Bit 7 selects color group
            
            int pixelX = byteCol * 7 * scaleX;
            
            if (colorMode)
            {
                RenderHiResColorByte(buffer, width, pixelX, pixelY, 
                                     scaleX, scaleY, data, palette, byteCol);
            }
            else
            {
                RenderHiResMonoByte(buffer, width, pixelX, pixelY,
                                    scaleX, scaleY, data);
            }
        }
    }
}

private void RenderHiResMonoByte(
    Span<uint> buffer, int width,
    int pixelX, int pixelY,
    int scaleX, int scaleY,
    byte data)
{
    // Monochrome: each bit = one pixel
    for (int bit = 0; bit < 7; bit++)
    {
        bool lit = (data & (1 << bit)) != 0;
        uint color = lit ? 0xFFFFFFFF : 0xFF000000;
        
        for (int sy = 0; sy < scaleY; sy++)
        {
            for (int sx = 0; sx < scaleX; sx++)
            {
                int x = pixelX + (bit * scaleX) + sx;
                int y = pixelY + sy;
                buffer[(y * width) + x] = color;
            }
        }
    }
}

private void RenderHiResColorByte(
    Span<uint> buffer, int width,
    int pixelX, int pixelY,
    int scaleX, int scaleY,
    byte data, bool palette, int byteCol)
{
    // NTSC artifact coloring (simplified)
    // Adjacent ON bits create white
    // Single ON bit on even column: Violet (palette 0) or Blue (palette 1)
    // Single ON bit on odd column: Green (palette 0) or Orange (palette 1)
    
    uint violet = 0xFFDD22DD;
    uint green = 0xFF11DD00;
    uint blue = 0xFF2222FF;
    uint orange = 0xFFFF6600;
    uint white = 0xFFFFFFFF;
    uint black = 0xFF000000;
    
    uint color0 = palette ? blue : violet;
    uint color1 = palette ? orange : green;
    
    for (int bit = 0; bit < 7; bit++)
    {
        bool current = (data & (1 << bit)) != 0;
        bool prev = bit > 0 && (data & (1 << (bit - 1))) != 0;
        bool next = bit < 6 && (data & (1 << (bit + 1))) != 0;
        
        uint color;
        if (!current)
        {
            color = black;
        }
        else if (prev || next)
        {
            color = white;  // Adjacent bits = white
        }
        else
        {
            // Absolute screen column determines color
            int screenCol = (byteCol * 7) + bit;
            color = (screenCol & 1) == 0 ? color0 : color1;
        }
        
        for (int sy = 0; sy < scaleY; sy++)
        {
            for (int sx = 0; sx < scaleX; sx++)
            {
                int x = pixelX + (bit * scaleX) + sx;
                int y = pixelY + sy;
                buffer[(y * width) + x] = color;
            }
        }
    }
}
```

#### B.3.4 Double Hi-Res Rendering

Double hi-res interleaves main and auxiliary memory:

```csharp
/// <summary>
/// Renders double hi-res graphics mode.
/// </summary>
/// <param name="buffer">Target pixel buffer (560 width native).</param>
/// <param name="width">Buffer width in pixels.</param>
/// <param name="height">Buffer height in pixels.</param>
/// <param name="mainMemory">Main RAM hi-res page.</param>
/// <param name="auxMemory">Auxiliary RAM hi-res page.</param>
/// <param name="page2">True to use page 2.</param>
/// <param name="mixedMode">True to render only top 160 lines.</param>
/// <param name="colorMode">True for 140-column color, false for 560-column mono.</param>
public void RenderDoubleHiRes(
    Span<uint> buffer,
    int width,
    int height,
    ReadOnlySpan<byte> mainMemory,
    ReadOnlySpan<byte> auxMemory,
    bool page2,
    bool mixedMode,
    bool colorMode)
{
    int pageBase = page2 ? 0x4000 : 0x2000;
    int maxLines = mixedMode ? 160 : 192;
    int scaleY = height / 192;
    
    for (int line = 0; line < maxLines; line++)
    {
        int lineBase = HiResBaseAddress(line, page2 ? 2 : 1) - pageBase;
        int pixelY = line * scaleY;
        
        if (colorMode)
        {
            RenderDHRColorLine(buffer, width, pixelY, scaleY, lineBase,
                              mainMemory, auxMemory);
        }
        else
        {
            RenderDHRMonoLine(buffer, width, pixelY, scaleY, lineBase,
                             mainMemory, auxMemory);
        }
    }
}

private void RenderDHRMonoLine(
    Span<uint> buffer, int width,
    int pixelY, int scaleY,
    int lineBase,
    ReadOnlySpan<byte> mainMemory,
    ReadOnlySpan<byte> auxMemory)
{
    // 560 pixels per line: aux[0] bits 0-6, main[0] bits 0-6, aux[1] bits 0-6, ...
    int pixelX = 0;
    
    for (int byteCol = 0; byteCol < 40; byteCol++)
    {
        byte auxData = auxMemory[lineBase + byteCol];
        byte mainData = mainMemory[lineBase + byteCol];
        
        // Render 7 pixels from aux byte
        for (int bit = 0; bit < 7; bit++)
        {
            bool lit = (auxData & (1 << bit)) != 0;
            uint color = lit ? 0xFFFFFFFF : 0xFF000000;
            
            for (int sy = 0; sy < scaleY; sy++)
            {
                buffer[((pixelY + sy) * width) + pixelX] = color;
            }
            pixelX++;
        }
        
        // Render 7 pixels from main byte
        for (int bit = 0; bit < 7; bit++)
        {
            bool lit = (mainData & (1 << bit)) != 0;
            uint color = lit ? 0xFFFFFFFF : 0xFF000000;
            
            for (int sy = 0; sy < scaleY; sy++)
            {
                buffer[((pixelY + sy) * width) + pixelX] = color;
            }
            pixelX++;
        }
    }
}

private void RenderDHRColorLine(
    Span<uint> buffer, int width,
    int pixelY, int scaleY,
    int lineBase,
    ReadOnlySpan<byte> mainMemory,
    ReadOnlySpan<byte> auxMemory)
{
    // Double hi-res color: groups of 4 bits = 1 color pixel
    // 140 color pixels per line (560 / 4)
    // Each color pixel = 4 screen pixels wide
    
    int pixelX = 0;
    
    for (int byteCol = 0; byteCol < 40; byteCol++)
    {
        byte auxData = auxMemory[lineBase + byteCol];
        byte mainData = mainMemory[lineBase + byteCol];
        
        // 14 bits total: aux bits 0-6, main bits 0-6
        // Creates 3.5 color pixels (we process in pairs)
        ushort combined = (ushort)(auxData | (mainData << 7));
        
        // Process as 4-bit nibbles for color
        for (int nibble = 0; nibble < 3; nibble++)
        {
            int colorIndex = (combined >> (nibble * 4)) & 0x0F;
            uint color = AppleIIColorToArgb(colorIndex);
            
            // Each color pixel = 4 screen pixels
            for (int px = 0; px < 4; px++)
            {
                for (int sy = 0; sy < scaleY; sy++)
                {
                    if (pixelX < width)
                    {
                        buffer[((pixelY + sy) * width) + pixelX] = color;
                    }
                }
                pixelX++;
            }
        }
        
        // Handle the remaining 2 bits + next byte's first 2 bits
        // (This is simplified; real DHR color is more complex)
    }
}
```

### B.4 Mixed Mode Handling

Mixed mode displays graphics in the top 160 lines and text in the bottom 32 lines:

```csharp
/// <summary>
/// Renders mixed mode display (graphics + 4 lines of text).
/// </summary>
/// <param name="buffer">Target pixel buffer.</param>
/// <param name="width">Buffer width in pixels.</param>
/// <param name="height">Buffer height in pixels.</param>
/// <param name="videoController">Video controller for mode state.</param>
/// <param name="mainMemory">Main RAM.</param>
/// <param name="auxMemory">Auxiliary RAM (for 80-col/DHR).</param>
/// <param name="charGen">Character generator ROM.</param>
public void RenderMixedMode(
    Span<uint> buffer,
    int width,
    int height,
    IVideoController videoController,
    ReadOnlySpan<byte> mainMemory,
    ReadOnlySpan<byte> auxMemory,
    ICharacterGenerator charGen)
{
    // Render graphics portion (top 160 lines / 20 text rows)
    if (videoController.IsHiResMode)
    {
        if (videoController.IsDoubleHiResMode)
        {
            RenderDoubleHiRes(buffer, width, height, mainMemory, auxMemory,
                             videoController.IsPage2, mixedMode: true, colorMode: true);
        }
        else
        {
            RenderHiRes(buffer, width, height, mainMemory,
                       videoController.IsPage2, mixedMode: true, colorMode: true);
        }
    }
    else
    {
        // Lo-res graphics
        RenderLoRes(buffer, width, height, mainMemory,
                   videoController.IsPage2, mixedMode: true);
    }
    
    // Render text portion (bottom 4 rows = lines 160-191)
    RenderTextWindow(buffer, width, height, mainMemory, auxMemory, charGen,
                    videoController.IsPage2, videoController.Is80ColumnMode,
                    flashState: videoController.FlashState,
                    altCharSet: videoController.IsAltCharSet);
}

private void RenderTextWindow(
    Span<uint> buffer,
    int width,
    int height,
    ReadOnlySpan<byte> mainMemory,
    ReadOnlySpan<byte> auxMemory,
    ICharacterGenerator charGen,
    bool page2,
    bool is80Column,
    bool flashState,
    bool altCharSet)
{
    int scaleY = height / 192;
    int charHeight = 8 * scaleY;
    int pageBase = page2 ? 0x0800 : 0x0400;
    
    // Render text rows 20-23 (screen lines 160-191)
    for (int textRow = 20; textRow < 24; textRow++)
    {
        int rowBase = TextBaseAddress(textRow, page2 ? 2 : 1);
        int pixelY = textRow * charHeight;
        
        int cols = is80Column ? 80 : 40;
        int charWidth = width / cols;
        
        for (int col = 0; col < cols; col++)
        {
            byte charCode;
            if (is80Column)
            {
                int memCol = col / 2;
                charCode = (col & 1) == 0 
                    ? auxMemory[rowBase - pageBase + memCol]
                    : mainMemory[rowBase - pageBase + memCol];
            }
            else
            {
                charCode = mainMemory[rowBase - pageBase + col];
            }
            
            int pixelX = col * charWidth;
            
            for (int scanline = 0; scanline < 8; scanline++)
            {
                byte pixels = charGen.GetCharacterRow(
                    charCode, scanline, flashState, altCharSet);
                
                int pixelWidth = charWidth / 7;
                for (int bit = 0; bit < 7; bit++)
                {
                    bool lit = (pixels & (0x40 >> bit)) != 0;
                    uint color = lit ? 0xFFFFFFFF : 0xFF000000;
                    
                    for (int sy = 0; sy < scaleY; sy++)
                    {
                        for (int px = 0; px < pixelWidth; px++)
                        {
                            int x = pixelX + (bit * pixelWidth) + px;
                            int y = pixelY + (scanline * scaleY) + sy;
                            if (x < width && y < height)
                            {
                                buffer[(y * width) + x] = color;
                            }
                        }
                    }
                }
            }
        }
    }
}
```

### B.5 Frame Buffer Management

#### B.5.1 Double Buffering

To avoid tearing, use double buffering:

```csharp
/// <summary>
/// Manages double-buffered frame rendering.
/// </summary>
public sealed class FrameBufferManager : IDisposable
{
    private readonly uint[] _frontBuffer;
    private readonly uint[] _backBuffer;
    private readonly int _width;
    private readonly int _height;
    private readonly object _swapLock = new();
    private bool _backBufferDirty;
    
    /// <summary>
    /// Initializes a new frame buffer manager.
    /// </summary>
    /// <param name="width">Buffer width in pixels.</param>
    /// <param name="height">Buffer height in pixels.</param>
    public FrameBufferManager(int width, int height)
    {
        _width = width;
        _height = height;
        _frontBuffer = new uint[width * height];
        _backBuffer = new uint[width * height];
    }
    
    /// <summary>
    /// Gets the back buffer for rendering.
    /// </summary>
    public Span<uint> BackBuffer => _backBuffer;
    
    /// <summary>
    /// Marks the back buffer as ready for display.
    /// </summary>
    public void Present()
    {
        lock (_swapLock)
        {
            _backBufferDirty = true;
        }
    }
    
    /// <summary>
    /// Copies the latest frame to the target bitmap.
    /// </summary>
    /// <param name="target">WriteableBitmap to copy to.</param>
    /// <returns>True if a new frame was copied.</returns>
    public bool CopyToTarget(WriteableBitmap target)
    {
        lock (_swapLock)
        {
            if (!_backBufferDirty)
                return false;
            
            // Swap buffers
            Array.Copy(_backBuffer, _frontBuffer, _frontBuffer.Length);
            _backBufferDirty = false;
        }
        
        // Copy to bitmap outside lock
        using var frameBuffer = target.Lock();
        unsafe
        {
            fixed (uint* src = _frontBuffer)
            {
                Buffer.MemoryCopy(src, (void*)frameBuffer.Address,
                    _frontBuffer.Length * sizeof(uint),
                    _frontBuffer.Length * sizeof(uint));
            }
        }
        
        return true;
    }
    
    public void Dispose()
    {
        // No unmanaged resources
    }
}
```

#### B.5.2 VBlank Synchronization

Render at VBlank for smooth display:

```csharp
/// <summary>
/// Coordinates frame rendering with video controller VBlank.
/// </summary>
public sealed class VBlankSynchronizer
{
    private readonly IVideoController _video;
    private readonly FrameBufferManager _bufferManager;
    private readonly IDisplayRenderer _renderer;
    private readonly ManualResetEventSlim _frameReady = new(false);
    
    public VBlankSynchronizer(
        IVideoController video,
        FrameBufferManager bufferManager,
        IDisplayRenderer renderer)
    {
        _video = video;
        _bufferManager = bufferManager;
        _renderer = renderer;
        
        _video.VBlankStart += OnVBlank;
    }
    
    private void OnVBlank()
    {
        // Render the frame at VBlank start
        _renderer.RenderFrame(_bufferManager.BackBuffer, _video);
        _bufferManager.Present();
        _frameReady.Set();
    }
    
    /// <summary>
    /// Waits for the next frame to be ready.
    /// </summary>
    /// <param name="timeout">Maximum wait time.</param>
    /// <returns>True if a frame became ready.</returns>
    public bool WaitForFrame(TimeSpan timeout)
    {
        bool result = _frameReady.Wait(timeout);
        _frameReady.Reset();
        return result;
    }
}
```

### B.6 Avalonia Integration

#### B.6.1 VideoDisplay Control

```csharp
/// <summary>
/// Avalonia control for rendering Apple II video output.
/// </summary>
public partial class VideoDisplay : UserControl
{
    public static readonly StyledProperty<IMachine?> MachineProperty =
        AvaloniaProperty.Register<VideoDisplay, IMachine?>(nameof(Machine));
    
    public static readonly StyledProperty<double> ScaleProperty =
        AvaloniaProperty.Register<VideoDisplay, double>(nameof(Scale), 2.0);
    
    public static readonly StyledProperty<ColorPalette> PaletteProperty =
        AvaloniaProperty.Register<VideoDisplay, ColorPalette>(
            nameof(Palette), ColorPalette.NTSC);
    
    public static readonly StyledProperty<bool> ScanlineEffectProperty =
        AvaloniaProperty.Register<VideoDisplay, bool>(nameof(ScanlineEffect), false);
    
    private WriteableBitmap? _frameBitmap;
    private FrameBufferManager? _bufferManager;
    private IDisplayRenderer? _renderer;
    private DispatcherTimer? _refreshTimer;
    
    public IMachine? Machine
    {
        get => GetValue(MachineProperty);
        set => SetValue(MachineProperty, value);
    }
    
    public double Scale
    {
        get => GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }
    
    public ColorPalette Palette
    {
        get => GetValue(PaletteProperty);
        set => SetValue(PaletteProperty, value);
    }
    
    public bool ScanlineEffect
    {
        get => GetValue(ScanlineEffectProperty);
        set => SetValue(ScanlineEffectProperty, value);
    }
    
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        
        if (change.Property == MachineProperty)
        {
            OnMachineChanged(change.GetOldValue<IMachine?>(), 
                           change.GetNewValue<IMachine?>());
        }
    }
    
    private void OnMachineChanged(IMachine? oldMachine, IMachine? newMachine)
    {
        if (oldMachine != null)
        {
            _refreshTimer?.Stop();
        }
        
        if (newMachine != null)
        {
            InitializeForMachine(newMachine);
        }
    }
    
    private void InitializeForMachine(IMachine machine)
    {
        // Create frame buffer (560×384 for all modes)
        _bufferManager = new FrameBufferManager(560, 384);
        _frameBitmap = new WriteableBitmap(
            new PixelSize(560, 384),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Opaque);
        
        // Create renderer
        _renderer = new AppleIIDisplayRenderer(Palette);
        
        // Subscribe to VBlank for frame rendering
        if (machine.Video is IVideoController video)
        {
            video.VBlankStart += OnVBlank;
        }
        
        // Start refresh timer for UI updates (separate from emulation)
        _refreshTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(16.67), // 60 Hz
            DispatcherPriority.Render,
            RefreshDisplay);
        _refreshTimer.Start();
    }
    
    private void OnVBlank()
    {
        // Called on emulator thread at VBlank
        if (_bufferManager != null && _renderer != null && Machine?.Video != null)
        {
            _renderer.RenderFrame(_bufferManager.BackBuffer, Machine.Video, Machine.Bus);
            _bufferManager.Present();
        }
    }
    
    private void RefreshDisplay(object? sender, EventArgs e)
    {
        // Called on UI thread at 60 Hz
        if (_bufferManager?.CopyToTarget(_frameBitmap!) == true)
        {
            InvalidateVisual();
        }
    }
    
    public override void Render(DrawingContext context)
    {
        if (_frameBitmap == null)
        {
            base.Render(context);
            return;
        }
        
        // Calculate destination rect with scaling
        var scale = Scale;
        var destSize = new Size(_frameBitmap.Size.Width * scale, 
                               _frameBitmap.Size.Height * scale);
        
        // Center in control
        var x = (Bounds.Width - destSize.Width) / 2;
        var y = (Bounds.Height - destSize.Height) / 2;
        var destRect = new Rect(x, y, destSize.Width, destSize.Height);
        
        // Draw with nearest-neighbor for crisp pixels
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);
        context.DrawImage(_frameBitmap, destRect);
        
        // Apply scanline effect if enabled
        if (ScanlineEffect)
        {
            RenderScanlines(context, destRect);
        }
    }
    
    private void RenderScanlines(DrawingContext context, Rect destRect)
    {
        // Draw semi-transparent horizontal lines for scanline effect
        using var pen = new Pen(new SolidColorBrush(Color.FromArgb(64, 0, 0, 0)), 1);
        
        double lineSpacing = destRect.Height / 192;  // One line per original scanline
        for (int i = 0; i < 192; i++)
        {
            double y = destRect.Y + (i * lineSpacing) + (lineSpacing / 2);
            context.DrawLine(pen, 
                new Point(destRect.X, y), 
                new Point(destRect.Right, y));
        }
    }
}
```

#### B.6.2 IDisplayRenderer Interface

```csharp
/// <summary>
/// Renders Apple II video output to a frame buffer.
/// </summary>
public interface IDisplayRenderer
{
    /// <summary>
    /// Renders the current frame to the buffer.
    /// </summary>
    /// <param name="buffer">Target pixel buffer.</param>
    /// <param name="video">Video controller for mode state.</param>
    /// <param name="bus">Memory bus for reading video memory.</param>
    void RenderFrame(Span<uint> buffer, IVideoController video, IMemoryBus bus);
    
    /// <summary>
    /// Gets or sets the color palette.
    /// </summary>
    ColorPalette Palette { get; set; }
}

/// <summary>
/// Renders Apple II (IIe/IIc) video modes.
/// </summary>
public sealed class AppleIIDisplayRenderer : IDisplayRenderer
{
    private const int BufferWidth = 560;
    private const int BufferHeight = 384;
    
    private readonly ICharacterGenerator _charGen;
    private readonly byte[] _mainMemoryCache = new byte[0x6000];  // $0000-$5FFF
    private readonly byte[] _auxMemoryCache = new byte[0x6000];
    
    public ColorPalette Palette { get; set; }
    
    public AppleIIDisplayRenderer(ColorPalette palette)
    {
        Palette = palette;
        _charGen = new AppleIIeCharacterGenerator();
    }
    
    public void RenderFrame(Span<uint> buffer, IVideoController video, IMemoryBus bus)
    {
        // Read video memory into cache (DMA-style, no side effects)
        CacheVideoMemory(bus, video);
        
        // Clear buffer
        buffer.Fill(0xFF000000);
        
        // Render based on current mode
        if (video.IsTextMode)
        {
            if (video.Is80ColumnMode)
            {
                RenderText80(buffer, BufferWidth, BufferHeight,
                            _mainMemoryCache.AsSpan(), _auxMemoryCache.AsSpan(),
                            _charGen, video.IsPage2, video.FlashState, video.IsAltCharSet);
            }
            else
            {
                RenderText40(buffer, BufferWidth, BufferHeight,
                            _mainMemoryCache.AsSpan(), _charGen,
                            video.IsPage2, video.FlashState, video.IsAltCharSet);
            }
        }
        else if (video.IsMixedMode)
        {
            RenderMixedMode(buffer, BufferWidth, BufferHeight, video,
                           _mainMemoryCache.AsSpan(), _auxMemoryCache.AsSpan(), _charGen);
        }
        else if (video.IsHiResMode)
        {
            if (video.IsDoubleHiResMode)
            {
                RenderDoubleHiRes(buffer, BufferWidth, BufferHeight,
                                 _mainMemoryCache.AsSpan(), _auxMemoryCache.AsSpan(),
                                 video.IsPage2, mixedMode: false, colorMode: true);
            }
            else
            {
                RenderHiRes(buffer, BufferWidth, BufferHeight,
                           _mainMemoryCache.AsSpan(), video.IsPage2,
                           mixedMode: false, colorMode: Palette != ColorPalette.Monochrome);
            }
        }
        else
        {
            // Lo-res
            RenderLoRes(buffer, BufferWidth, BufferHeight,
                       _mainMemoryCache.AsSpan(), video.IsPage2, mixedMode: false);
        }
    }
    
    private void CacheVideoMemory(IMemoryBus bus, IVideoController video)
    {
        // Create a side-effect-free bus access for DMA reads
        var access = new BusAccess(
            Address: 0,
            Value: 0,
            WidthBits: 8,
            Mode: CpuMode.Compat,
            EmulationE: true,
            Intent: AccessIntent.DmaRead,
            SourceId: 0,  // Video device ID
            Cycle: 0,
            Flags: AccessFlags.NoSideEffects);
        
        // Cache text pages
        for (int i = 0; i < 0x0800; i++)
        {
            _mainMemoryCache[0x0400 + i] = bus.Read8(access with { Address = (Addr)(0x0400 + i) }).Value;
        }
        
        // Cache hi-res pages
        for (int i = 0; i < 0x4000; i++)
        {
            _mainMemoryCache[0x2000 + i] = bus.Read8(access with { Address = (Addr)(0x2000 + i) }).Value;
        }
        
        // Cache auxiliary memory if needed
        if (video.Is80ColumnMode || video.IsDoubleHiResMode)
        {
            // Read from auxiliary bank
            // (Implementation depends on how aux memory is mapped)
        }
    }
    
    // ... mode-specific rendering methods as shown earlier ...
}
```

### B.7 Performance Considerations

#### B.7.1 Memory Access Optimization

Minimize bus reads during rendering by caching video memory:

```csharp
/// <summary>
/// Optimized video memory cache that tracks dirty regions.
/// </summary>
public sealed class VideoMemoryCache
{
    private readonly byte[] _textPage1 = new byte[0x0400];
    private readonly byte[] _textPage2 = new byte[0x0400];
    private readonly byte[] _hiResPage1 = new byte[0x2000];
    private readonly byte[] _hiResPage2 = new byte[0x2000];
    private readonly byte[] _auxTextPage1 = new byte[0x0400];
    private readonly byte[] _auxTextPage2 = new byte[0x0400];
    private readonly byte[] _auxHiResPage1 = new byte[0x2000];
    private readonly byte[] _auxHiResPage2 = new byte[0x2000];
    
    private bool _textPage1Dirty = true;
    private bool _hiResPage1Dirty = true;
    // ... more dirty flags ...
    
    /// <summary>
    /// Marks a memory region as dirty (called when CPU writes to video memory).
    /// </summary>
    public void MarkDirty(Addr address)
    {
        if (address >= 0x0400 && address < 0x0800)
            _textPage1Dirty = true;
        else if (address >= 0x2000 && address < 0x4000)
            _hiResPage1Dirty = true;
        // ... etc.
    }
    
    /// <summary>
    /// Refreshes dirty regions from the bus.
    /// </summary>
    public void Refresh(IMemoryBus bus, in BusAccess templateAccess)
    {
        if (_textPage1Dirty)
        {
            for (int i = 0; i < 0x0400; i++)
            {
                _textPage1[i] = bus.Read8(templateAccess with 
                    { Address = (Addr)(0x0400 + i) }).Value;
            }
            _textPage1Dirty = false;
        }
        // ... refresh other dirty regions ...
    }
}
```

#### B.7.2 Lookup Tables

Pre-compute common values:

```csharp
/// <summary>
/// Pre-computed lookup tables for video rendering.
/// </summary>
public static class VideoLookupTables
{
    /// <summary>Text row base addresses.</summary>
    public static readonly ushort[] TextRowBase = new ushort[24];
    
    /// <summary>Hi-res row base addresses.</summary>
    public static readonly ushort[] HiResRowBase = new ushort[192];
    
    /// <summary>Color palette (ARGB).</summary>
    public static readonly uint[] NtscPalette = new uint[16];
    
    /// <summary>Character bitmap cache.</summary>
    public static readonly byte[,] CharacterBitmaps = new byte[256, 8];
    
    static VideoLookupTables()
    {
        // Pre-compute text row addresses
        for (int row = 0; row < 24; row++)
        {
            int group = row / 8;
            int offset = row % 8;
            TextRowBase[row] = (ushort)(0x0400 + (offset * 128) + (group * 40));
        }
        
        // Pre-compute hi-res row addresses
        for (int row = 0; row < 192; row++)
        {
            int group = row / 64;
            int subRow = (row % 64) / 8;
            int scanLine = row % 8;
            HiResRowBase[row] = (ushort)(0x2000 + (scanLine * 1024) + 
                                         (subRow * 128) + (group * 40));
        }
        
        // Initialize color palette
        NtscPalette[0] = 0xFF000000;   // Black
        NtscPalette[1] = 0xFFDD0033;   // Magenta
        // ... etc.
    }
}
```

### B.8 Thread Safety

The rendering pipeline spans multiple threads:

```
┌─────────────────────────┐     ┌─────────────────────────┐
│    Emulator Thread      │     │      UI Thread          │
│                         │     │                         │
│  CPU execution          │     │  DispatcherTimer        │
│         │               │     │         │               │
│         ▼               │     │         ▼               │
│  VBlank callback        │     │  RefreshDisplay()       │
│         │               │     │         │               │
│         ▼               │     │         ▼               │
│  RenderFrame() ─────────┼─────┼─→ CopyToTarget()       │
│         │               │     │         │               │
│         ▼               │     │         ▼               │
│  Present() (back buf)   │     │  InvalidateVisual()    │
│                         │     │         │               │
│                         │     │         ▼               │
│                         │     │  Render()              │
└─────────────────────────┘     └─────────────────────────┘
                    │                       │
                    ▼                       ▼
            ┌──────────────────────────────────────┐
            │         FrameBufferManager           │
            │                                      │
            │  Back Buffer ←─── Render writes      │
            │       │                              │
            │       ▼ (atomic swap on Present)     │
            │  Front Buffer ───→ UI reads          │
            └──────────────────────────────────────┘
```

Ensure thread safety with proper synchronization:

```csharp
/// <summary>
/// Thread-safe frame buffer with atomic swap.
/// </summary>
public sealed class ThreadSafeFrameBuffer
{
    private uint[] _writeBuffer;
    private uint[] _readBuffer;
    private readonly int _width;
    private readonly int _height;
    private readonly ReaderWriterLockSlim _lock = new();
    
    public ThreadSafeFrameBuffer(int width, int height)
    {
        _width = width;
        _height = height;
        _writeBuffer = new uint[width * height];
        _readBuffer = new uint[width * height];
    }
    
    /// <summary>
    /// Gets the write buffer for the emulator thread.
    /// </summary>
    public Span<uint> GetWriteBuffer()
    {
        _lock.EnterReadLock();
        try
        {
            return _writeBuffer;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
    
    /// <summary>
    /// Atomically swaps the buffers.
    /// </summary>
    public void Swap()
    {
        _lock.EnterWriteLock();
        try
        {
            (_writeBuffer, _readBuffer) = (_readBuffer, _writeBuffer);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
    
    /// <summary>
    /// Copies the read buffer to a target bitmap.
    /// </summary>
    public void CopyTo(WriteableBitmap target)
    {
        _lock.EnterReadLock();
        try
        {
            using var frame = target.Lock();
            unsafe
            {
                fixed (uint* src = _readBuffer)
                {
                    Buffer.MemoryCopy(src, (void*)frame.Address,
                        _readBuffer.Length * sizeof(uint),
                        _readBuffer.Length * sizeof(uint));
                }
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
}
```