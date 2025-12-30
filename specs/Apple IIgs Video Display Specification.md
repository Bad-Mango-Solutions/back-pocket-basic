# Apple IIgs Video Display Specification

## Document Information

| Field        | Value                                         |
|--------------|-----------------------------------------------|
| Version      | 1.0                                           |
| Date         | 2025-12-28                                    |
| Status       | Initial Draft                                 |
| Applies To   | PocketGS (Apple IIgs)                         |

---

## 1. Overview

The Apple IIgs features a significantly enhanced video system compared to earlier Apple II
models. It includes all classic Apple II display modes for backward compatibility, plus new
Super Hi-Res graphics modes capable of displaying 320×200 or 640×200 pixels with up to 256
colors from a palette of 4,096.

### 1.1 Video Architecture

The IIgs video system consists of several key components:

1. **Mega II**: Custom chip that provides Apple IIe compatibility, including classic video
   modes and memory shadowing.

2. **Video Graphics Controller (VGC)**: Generates the Super Hi-Res display and handles
   border colors.

3. **Color Look-Up Tables (CLUTs)**: 16 palettes of 16 colors each, selectable per scanline.

4. **RAM-based graphics**: All video data is stored in main RAM (Bank $E1), not a separate
   frame buffer.

### 1.2 Display Timing

| Parameter           | Value              |
|---------------------|--------------------|
| Horizontal scan     | 15.7 kHz           |
| Vertical refresh    | 60 Hz (59.94)      |
| Active lines        | 200 (SHR) / 192 (classic) |
| Border lines        | Top 8, Bottom 8    |
| Horizontal pixels   | 320 or 640 (SHR)   |
| Border width        | Variable           |

---

## 2. Classic Apple II Modes (Mega II)

The IIgs maintains full backward compatibility with Apple IIe display modes through the
Mega II chip. These modes operate identically to the IIe, with memory shadowing to banks
$E0-$E1.

### 2.1 Memory Shadowing

The Mega II shadows specific memory regions to bank $E0-$E1:

| Classic Address | Shadowed To      | Purpose               |
|-----------------|------------------|-----------------------|
| $0400-$07FF     | $E0/0400-$E0/07FF | Text/Lo-res Page 1   |
| $0800-$0BFF     | $E0/0800-$E0/0BFF | Text/Lo-res Page 2   |
| $2000-$3FFF     | $E0/2000-$E0/3FFF | Hi-res Page 1        |
| $4000-$5FFF     | $E0/4000-$E0/5FFF | Hi-res Page 2        |

The SHADOW register ($C035) controls which regions are shadowed:

| Bit | When Set (1)                           |
|-----|----------------------------------------|
| 0   | Inhibit text page 1 shadowing         |
| 1   | Inhibit text page 2 shadowing         |
| 2   | Inhibit hi-res page 1 shadowing       |
| 3   | Inhibit hi-res page 2 shadowing       |
| 4   | Inhibit super hi-res shadowing        |
| 5   | Inhibit auxiliary hi-res shadowing    |
| 6   | Inhibit text aux shadowing            |
| 7   | Inhibit I/O and LC shadowing          |

### 2.2 Mode Selection

Classic modes are selected via the same soft switches as the IIe:

- $C050/$C051: Graphics/Text
- $C052/$C053: Full-screen/Mixed
- $C054/$C055: Page 1/Page 2
- $C056/$C057: Lo-res/Hi-res
- $C00C/$C00D: 40/80 column
- $C05E/$C05F: Double hi-res

---

## 3. Super Hi-Res Graphics Mode

Super Hi-Res (SHR) is the IIgs's native graphics mode, providing vastly improved color and
resolution compared to classic modes.

### 3.1 Memory Layout

Super Hi-Res uses bank $E1 memory:

| Address Range     | Size   | Purpose                           |
|-------------------|--------|-----------------------------------|
| $E1/2000-$E1/9CFF | 31.5KB | Pixel data (200 lines × 160 bytes)|
| $E1/9D00-$E1/9DFF | 256B   | Scan-line Control Bytes (SCBs)    |
| $E1/9E00-$E1/9FFF | 512B   | Color palettes (16 × 32 bytes)    |

### 3.2 Display Modes

The SHR mode supports two primary display configurations:

| Mode   | Resolution | Colors per line | Pixel width |
|--------|------------|-----------------|-------------|
| 320    | 320×200    | 16 (from 256)   | 2 dots      |
| 640    | 640×200    | 4 (dithered 16) | 1 dot       |

### 3.3 Scan-Line Control Bytes (SCBs)

Each of the 200 scanlines has an associated SCB at $E1/9D00 + line:

```
Bit 7: Reserved (must be 0)
Bit 6: Fill mode (1 = fill from left edge)
Bit 5: Interrupt enable (1 = generate interrupt on this line)
Bit 4: Reserved (must be 0)
Bits 3-0: Palette number (0-15)
```

Additionally, the SCB controls the display mode for the line:

| Bit 7 | Bit 6 | Mode                              |
|-------|-------|-----------------------------------|
| 0     | 0     | 320 mode, normal                  |
| 0     | 1     | 320 mode, fill mode               |
| 1     | 0     | 640 mode, normal                  |
| 1     | 1     | 640 mode, fill mode               |

Wait—the correct encoding per the IIgs Hardware Reference:

```
Bit 7: Mode (0 = 320 mode, 1 = 640 mode)
Bit 6: Fill mode
Bit 5: Interrupt on this line
Bit 4: Color table enable (320 mode only)
Bits 3-0: Palette number
```

### 3.4 320 Mode Pixel Format

In 320 mode, each byte contains two 4-bit pixels:

```
Bits 7-4: Right pixel (even column)
Bits 3-0: Left pixel (odd column)
```

Each 4-bit value is an index into the currently selected palette (0-15).

### 3.5 640 Mode Pixel Format

In 640 mode, each byte contains four 2-bit pixels:

```
Bits 7-6: Pixel 0 (leftmost)
Bits 5-4: Pixel 1
Bits 3-2: Pixel 2
Bits 1-0: Pixel 3 (rightmost)
```

Each 2-bit value selects colors 0-3 from the palette. The dithering pattern creates the
appearance of additional colors:

| 2-bit Value | Color Index   | Typical Usage         |
|-------------|---------------|-----------------------|
| 0           | Palette[0]    | Background            |
| 1           | Palette[4]    | Dither color 1        |
| 2           | Palette[8]    | Dither color 2        |
| 3           | Palette[12]   | Foreground            |

### 3.6 Color Palettes

The IIgs has 16 palettes, each containing 16 colors. Each color is a 12-bit RGB value:

```
Color entry format (2 bytes, little-endian):
Byte 0: Bits 3-0 = Blue (0-15)
        Bits 7-4 = Green (0-15)
Byte 1: Bits 3-0 = Red (0-15)
        Bits 7-4 = Reserved (0)
```

Palettes are stored at $E1/9E00:

```
Palette 0:  $E1/9E00-$E1/9E1F (16 colors × 2 bytes = 32 bytes)
Palette 1:  $E1/9E20-$E1/9E3F
...
Palette 15: $E1/9FE0-$E1/9FFF
```

### 3.7 Fill Mode

When fill mode is enabled for a scanline, the leftmost pixel of each byte "fills" to the
left edge of the screen. This is useful for creating solid backgrounds efficiently:

```
Normal:    [P0][P1][P2][P3]...
Fill mode: [P0][P0][P0][P0][P0][P0]...[P0][P1][P2][P3]...
```

---

## 4. Border Color

The IIgs displays a configurable border around the active display area.

### 4.1 Border Color Register

The border color is set via the BORDER register at $C034:

```
Bits 3-0: Border color (palette 0, color index 0-15)
Bits 7-4: Reserved
```

### 4.2 Border Dimensions

The border extends:
- **Top**: 8 scanlines (lines 0-7)
- **Bottom**: 8 scanlines (lines 208-215)
- **Left/Right**: Variable depending on display mode and overscan settings

---

## 5. Text Modes

### 5.1 40/80 Column Text

The IIgs supports the same 40-column and 80-column text modes as the Apple IIe. These are
generated by the Mega II chip using shadowed memory.

### 5.2 Super Hi-Res Text

There is no dedicated "Super Hi-Res text mode." Text in SHR applications is drawn as
graphics, typically using font data stored in RAM. The Toolbox provides text-drawing
routines that render to the SHR graphics buffer.

---

## 6. Video Control Registers

### 6.1 New Video Register ($C029)

| Bit | Name    | Function                                    |
|-----|---------|---------------------------------------------|
| 7   | SUPER   | 1 = Super Hi-Res mode active                |
| 6   | LINEAR  | 1 = Linearize SHR bank (bank $E1 only)      |
| 5   | DHIRES  | 1 = Disable classic double-hi-res artifacts |
| 4-1 | Reserved| Must be 0                                   |
| 0   | A2BANK  | Classic video bank: 0 = bank 0, 1 = bank 1  |

### 6.2 Shadow Register ($C035)

Controls memory shadowing for video regions (see section 2.1).

### 6.3 Speed Register ($C036)

| Bit | Name     | Function                                   |
|-----|----------|--------------------------------------------|
| 7   | ALLFAST  | 1 = Fast mode for all memory               |
| 6   | SLOTFAST | 1 = Fast mode for slot ROM access          |
| 5-1 | Reserved | Must be 0                                  |
| 0   | SLOW     | 1 = Slow (1 MHz) mode                      |

### 6.4 State Register ($C068)

| Bit | Name    | Function                                    |
|-----|---------|---------------------------------------------|
| 7   | RDLCBNK | Language card bank 2 selected               |
| 6   | RDLCRAM | Reading language card RAM                   |
| 5   | INTCXROM| Internal slot ROM enabled                   |
| 4   | ALTZP   | Alternate zero page enabled                 |
| 3   | 80STORE | 80STORE mode enabled                        |
| 2   | RAMRD   | Reading auxiliary RAM                       |
| 1   | RAMWRT  | Writing auxiliary RAM                       |
| 0   | PAGE2   | Page 2 selected                             |

---

## 7. Interrupt Support

### 7.1 Scanline Interrupts

The IIgs can generate an interrupt on any scanline by setting bit 5 of its SCB:

```csharp
// Set scanline 100 to generate an interrupt
memory.Write(0xE19D64, memory.Read(0xE19D64) | 0x20);  // SCB for line 100

// Enable scanline interrupts in VGC
memory.Write(0xC041, 0x08);  // Enable VGC interrupt
```

### 7.2 Vertical Blank Interrupt

The VGC generates a VBL interrupt at the start of vertical blanking:

```csharp
// Enable VBL interrupt
byte vgcInt = memory.Read(0xC041);
memory.Write(0xC041, vgcInt | 0x08);
```

---

## 8. Video Controller Interface (IIgs)

```csharp
/// <summary>
/// Interface for the Apple IIgs video display controller.
/// Extends the base IVideoController with IIgs-specific features.
/// </summary>
public interface IIgsVideoController : IVideoController
{
    // ??? Super Hi-Res State ?????????????????????????????????????????????
    
    /// <summary>Gets whether Super Hi-Res mode is active.</summary>
    bool IsSuperHiResMode { get; }
    
    /// <summary>Gets or sets the border color (0-15).</summary>
    int BorderColor { get; set; }
    
    // ??? Memory Access ??????????????????????????????????????????????????
    
    /// <summary>Gets the SCB for a scanline (0-199).</summary>
    byte GetScanlineControlByte(int line);
    
    /// <summary>Sets the SCB for a scanline.</summary>
    void SetScanlineControlByte(int line, byte value);
    
    /// <summary>Gets a color from a palette.</summary>
    /// <param name="palette">Palette number (0-15).</param>
    /// <param name="color">Color index (0-15).</param>
    /// <returns>12-bit RGB color (0x0RGB).</returns>
    ushort GetPaletteColor(int palette, int color);
    
    /// <summary>Sets a color in a palette.</summary>
    void SetPaletteColor(int palette, int color, ushort rgb);
    
    // ??? Interrupts ?????????????????????????????????????????????????????
    
    /// <summary>Gets whether scanline interrupts are enabled.</summary>
    bool ScanlineInterruptsEnabled { get; set; }
    
    /// <summary>Raised when a scanline interrupt occurs.</summary>
    event Action<int>? ScanlineInterrupt;
    
    // ??? Shadowing ??????????????????????????????????????????????????????
    
    /// <summary>Gets or sets the shadow register value.</summary>
    byte ShadowRegister { get; set; }
    
    /// <summary>Gets whether text page shadowing is enabled.</summary>
    bool IsTextShadowingEnabled { get; }
    
    /// <summary>Gets whether hi-res page shadowing is enabled.</summary>
    bool IsHiResShadowingEnabled { get; }
    
    /// <summary>Gets whether super hi-res shadowing is enabled.</summary>
    bool IsSuperHiResShadowingEnabled { get; }
}
```

---

## 9. Implementation Notes

### 9.1 Memory Bank Organization

The IIgs video system reads from specific memory banks:

- **Classic modes**: Bank $E0 (via Mega II shadowing)
- **Super Hi-Res**: Bank $E1 ($2000-$9FFF)

### 9.2 Rendering Pipeline

```csharp
public void RenderFrame(Span<uint> buffer, int width, int height)
{
    if (IsSuperHiResMode)
    {
        RenderSuperHiRes(buffer, width, height);
    }
    else
    {
        // Classic mode - delegate to Mega II emulation
        RenderClassicMode(buffer, width, height);
    }
    
    // Render border
    RenderBorder(buffer, width, height, BorderColor);
}

private void RenderSuperHiRes(Span<uint> buffer, int width, int height)
{
    for (int line = 0; line < 200; line++)
    {
        byte scb = GetScanlineControlByte(line);
        int palette = scb & 0x0F;
        bool is640Mode = (scb & 0x80) != 0;
        bool fillMode = (scb & 0x40) != 0;
        
        if (is640Mode)
            RenderSHRLine640(buffer, line, palette, fillMode);
        else
            RenderSHRLine320(buffer, line, palette, fillMode);
        
        // Check for scanline interrupt
        if ((scb & 0x20) != 0 && ScanlineInterruptsEnabled)
            ScanlineInterrupt?.Invoke(line);
    }
}
```

### 9.3 Color Conversion

Convert 12-bit IIgs colors to 32-bit ARGB:

```csharp
public static uint IIgsColorToArgb(ushort color)
{
    int r = (color >> 8) & 0x0F;
    int g = (color >> 4) & 0x0F;
    int b = color & 0x0F;
    
    // Expand 4-bit to 8-bit (multiply by 17)
    r = r * 17;
    g = g * 17;
    b = b * 17;
    
    return 0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | (uint)b;
}
```

---

## Document History

| Version | Date       | Changes                            |
|---------|------------|------------------------------------|
| 1.0     | 2025-12-28 | Initial specification              |

---

## Appendix A: Bus Architecture Integration

This appendix provides implementation guidance for integrating the Apple IIgs video
system with the emulator's bus architecture.

### A.1 Super Hi-Res Memory Target

The SHR memory region ($E1/2000-$E1/9FFF) implements `IBusTarget`:

```csharp
/// <summary>
/// Super Hi-Res video memory target in bank $E1.
/// </summary>
public sealed class SuperHiResMemory : IBusTarget
{
    private readonly byte[] _pixelData;     // 31.5KB: $2000-$9CFF
    private readonly byte[] _scbs;          // 256 bytes: $9D00-$9DFF
    private readonly ushort[] _palettes;    // 256 entries: $9E00-$9FFF
    
    /// <inheritdoc/>
    public TargetCaps Capabilities => TargetCaps.WideAtomic | TargetCaps.SideEffects;
    
    /// <inheritdoc/>
    public byte Read8(Addr physicalAddress, in BusAccess access)
    {
        uint offset = physicalAddress & 0x7FFF;  // Mask to $2000-$9FFF range
        
        return offset switch
        {
            < 0x7D00 => _pixelData[offset],                    // Pixel data
            < 0x7E00 => _scbs[offset - 0x7D00],               // SCBs
            _ => ReadPaletteByte(offset - 0x7E00)              // Palettes
        };
    }
    
    /// <inheritdoc/>
    public void Write8(Addr physicalAddress, byte value, in BusAccess access)
    {
        if (access.IsSideEffectFree)
            return;  // Debug writes don't modify SHR memory
        
        uint offset = physicalAddress & 0x7FFF;
        
        switch (offset)
        {
            case < 0x7D00:
                _pixelData[offset] = value;
                break;
            case < 0x7E00:
                _scbs[offset - 0x7D00] = value;
                OnScbChanged((int)(offset - 0x7D00), value);
                break;
            default:
                WritePaletteByte(offset - 0x7E00, value);
                break;
        }
    }
    
    private void OnScbChanged(int line, byte value)
    {
        // Check for scanline interrupt enable (bit 5)
        if ((value & 0x20) != 0 && ScanlineInterruptsEnabled)
            _pendingInterruptLines.Add(line);
    }
}
```

### A.2 VGC Register Target

The Video Graphics Controller registers implement `IBusTarget`:

```csharp
/// <summary>
/// VGC control registers as IBusTarget.
/// </summary>
public sealed class VgcRegisters : IBusTarget
{
    private readonly IIgsVideoController _video;
    private readonly ISignalBus _signals;
    private readonly int _deviceId;
    
    /// <inheritdoc/>
    public TargetCaps Capabilities => TargetCaps.SideEffects;
    
    /// <inheritdoc/>
    public byte Read8(Addr physicalAddress, in BusAccess access)
    {
        byte offset = (byte)(physicalAddress & 0xFF);
        
        return offset switch
        {
            0x29 => GetNewVideoRegister(),   // NEWVIDEO
            0x34 => (byte)_video.BorderColor, // BORDERCOLOR
            0x35 => _video.ShadowRegister,   // SHADOW
            _ => 0x00
        };
    }
    
    /// <inheritdoc/>
    public void Write8(Addr physicalAddress, byte value, in BusAccess access)
    {
        if (access.IsSideEffectFree)
            return;
        
        byte offset = (byte)(physicalAddress & 0xFF);
        
        switch (offset)
        {
            case 0x29:  // NEWVIDEO
                SetNewVideoRegister(value);
                break;
            case 0x34:  // BORDERCOLOR
                _video.BorderColor = value & 0x0F;
                break;
            case 0x35:  // SHADOW
                _video.ShadowRegister = value;
                break;
        }
    }
    
    private byte GetNewVideoRegister()
    {
        byte result = 0;
        if (_video.IsSuperHiResMode) result |= 0x80;
        if (_video.IsLinearized) result |= 0x40;
        return result;
    }
    
    private void SetNewVideoRegister(byte value)
    {
        bool wasShr = _video.IsSuperHiResMode;
        _video.IsSuperHiResMode = (value & 0x80) != 0;
        _video.IsLinearized = (value & 0x40) != 0;
        
        if (wasShr != _video.IsSuperHiResMode)
            VideoModeChanged?.Invoke();
    }
}
```

### A.3 Memory Shadowing Implementation

The Mega II shadowing is handled by the memory bus through mapping stacks:

```csharp
/// <summary>
/// Configures Mega II memory shadowing for IIgs.
/// </summary>
public sealed class Mega2Shadowing
{
    private readonly IMappingStack _mappingStack;
    private readonly RamTarget _mainRam;     // Bank $00-$01
    private readonly RamTarget _e0Ram;       // Bank $E0
    private readonly RamTarget _e1Ram;       // Bank $E1
    
    public void ConfigureShadowing(byte shadowRegister)
    {
        // Text page 1 shadowing
        if ((shadowRegister & 0x01) == 0)
        {
            _mappingStack.Push(new MappingEntry(
                baseAddress: 0x000400,
                size: 0x0400,
                target: _e0Ram,
                shadowSource: _mainRam,
                tag: RegionTag.TextPage1Shadow));
        }
        
        // Hi-res page 1 shadowing
        if ((shadowRegister & 0x04) == 0)
        {
            _mappingStack.Push(new MappingEntry(
                baseAddress: 0x002000,
                size: 0x2000,
                target: _e0Ram,
                shadowSource: _mainRam,
                tag: RegionTag.HiResPage1Shadow));
        }
        
        // Super hi-res shadowing (bank $E1)
        if ((shadowRegister & 0x10) == 0)
        {
            _mappingStack.Push(new MappingEntry(
                baseAddress: 0xE12000,
                size: 0x8000,
                target: _e1Ram,
                tag: RegionTag.ShrShadow));
        }
    }
}
```

### A.4 Scanline Interrupt Scheduling

The VGC uses the scheduler for scanline interrupts:

```csharp
public sealed class VgcController : IScheduledDevice, ISchedulable
{
    private const ulong CyclesPerScanline = 65;
    private const int ActiveLines = 200;
    private const int TotalLines = 262;
    
    /// <inheritdoc/>
    public void Initialize(IEventContext context)
    {
        _scheduler = context.Scheduler;
        _signals = context.Signals;
        _scheduler.ScheduleAfter(this, CyclesPerScanline);
    }
    
    /// <inheritdoc/>
    public ulong Execute(ulong currentCycle)
    {
        _currentLine++;
        
        if (_currentLine >= TotalLines)
        {
            _currentLine = 0;
            // VBL interrupt at start of vertical blank
            if (_vblInterruptEnabled)
                _signals.Assert(SignalLine.IRQ, _deviceId);
        }
        
        // Check for scanline interrupt
        if (_currentLine < ActiveLines && ScanlineInterruptsEnabled)
        {
            byte scb = _shrMemory.GetScb(_currentLine);
            if ((scb & 0x20) != 0)
            {
                _signals.Assert(SignalLine.IRQ, _deviceId);
                ScanlineInterrupt?.Invoke(_currentLine);
            }
        }
        
        _scheduler.ScheduleAfter(this, CyclesPerScanline);
        return CyclesPerScanline;
    }
}
```

### A.5 Composite Page for IIgs I/O

The IIgs extends the Apple II I/O composite page:

```csharp
public sealed class IIgsIOPage : ICompositeTarget
{
    private readonly AppleIIIOPage _baseIoPage;  // Inherited IIe behavior
    private readonly VgcRegisters _vgcRegisters;
    private readonly SoundRegisters _docRegisters;
    
    /// <inheritdoc/>
    public IBusTarget? ResolveTarget(Addr offset, AccessIntent intent)
    {
        // IIgs-specific registers
        return offset switch
        {
            0x29 => _vgcRegisters,  // NEWVIDEO
            0x34 => _vgcRegisters,  // BORDERCOLOR
            0x35 => _vgcRegisters,  // SHADOW
            0x36 => _speedRegisters, // SPEED
            0x3C or 0x3D => _docRegisters,  // DOC sound
            // Fall through to base IIe behavior
            _ => _baseIoPage.ResolveTarget(offset, intent)
        };
    }
    
    /// <inheritdoc/>
    public RegionTag GetSubRegionTag(Addr offset)
    {
        return offset switch
        {
            0x29 or 0x34 or 0x35 => RegionTag.VgcControl,
            0x36 => RegionTag.SpeedControl,
            0x3C or 0x3D => RegionTag.DocSound,
            _ => _baseIoPage.GetSubRegionTag(offset)
        };
    }
}
```

### A.6 Device Registry for IIgs Video

```csharp
public void RegisterIIgsVideoDevices(IDeviceRegistry registry)
{
    // VGC controller
    registry.Register(
        registry.GenerateId(),
        DevicePageId.Create(DevicePageClass.Framebuffer, instance: 0, page: 0),
        kind: "VGC",
        name: "Video Graphics Controller",
        wiringPath: "main/video/vgc");
    
    // Mega II (classic video)
    registry.Register(
        registry.GenerateId(),
        DevicePageId.Create(DevicePageClass.CompatIO, instance: 0, page: 1),
        kind: "Mega2Video",
        name: "Mega II Video Subsystem",
        wiringPath: "main/video/mega2");
    
    // SHR memory
    registry.Register(
        registry.GenerateId(),
        DevicePageId.Create(DevicePageClass.Framebuffer, instance: 0, page: 1),
        kind: "ShrMemory",
        name: "Super Hi-Res Memory",
        wiringPath: "main/video/shr");
}
```

### A.7 Rendering with Bus Access

```csharp
public void RenderSuperHiRes(IMemoryBus bus, Span<uint> pixels)
{
    var access = new BusAccess(
        Address: 0xE12000,
        Value: 0,
        WidthBits: 8,
        Mode: CpuMode.Native,
        EmulationFlag: false,
        Intent: AccessIntent.DmaRead,
        SourceId: _deviceId,
        Cycle: _scheduler.CurrentCycle,
        Flags: AccessFlags.NoSideEffects);
    
    for (int line = 0; line < 200; line++)
    {
        // Read SCB for this line
        var scbAccess = access with { Address = (Addr)(0xE19D00 + line) };
        byte scb = bus.Read8(scbAccess).Value;
        
        int palette = scb & 0x0F;
        bool is640Mode = (scb & 0x80) != 0;
        bool fillMode = (scb & 0x40) != 0;
        
        // Read palette colors
        var colors = ReadPaletteColors(bus, access, palette);
        
        // Read and render pixel data
        uint lineBase = (uint)(0xE12000 + (line * 160));
        RenderShrLine(bus, access, lineBase, line, colors, is640Mode, fillMode, pixels);
    }
}
```

## Appendix B: Frame Buffer Construction for Host UI

This appendix provides implementation guidance for rendering Apple IIgs video output to
a host display canvas, specifically targeting the Avalonia-based emulator UI.

### B.1 Frame Buffer Overview

The Apple IIgs video system is significantly more complex than the Apple IIe due to:

1. **Super Hi-Res mode** with per-scanline palette selection
2. **16 palettes** of 16 colors each (4,096 possible colors)
3. **Two display modes** (320 and 640) selectable per-scanline
4. **Border color** surrounding the active display area
5. **Scanline Control Bytes (SCBs)** controlling each line's rendering

#### B.1.1 Frame Buffer Dimensions

| Mode            | Native Resolution | Recommended Buffer | Aspect Ratio |
|-----------------|-------------------|-------------------|--------------|
| Classic IIe     | 280×192 / 560×192 | 640×400           | 4:3          |
| Super Hi-Res 320| 320×200           | 640×400           | 4:3          |
| Super Hi-Res 640| 640×200           | 640×400           | 4:3          |
| With Border     | 640×216           | 640×432           | 4:3          |

**Recommendation**: Use a 640×400 or 640×432 buffer (with border) for all IIgs modes.
This provides:
- 2× horizontal scaling for 320 mode
- Native horizontal for 640 mode
- 2× vertical scaling for all modes
- 4:3 aspect ratio for accurate display

#### B.1.2 Pixel Format and Color Depth

IIgs uses 12-bit RGB colors. Convert to 32-bit ARGB for modern displays:

```csharp
/// <summary>
/// Converts a 12-bit IIgs RGB color to 32-bit ARGB.
/// </summary>
/// <param name="iigsColor">16-bit value with RGB in bits 0-11.</param>
/// <returns>32-bit ARGB pixel value.</returns>
public static uint IIgsColorToArgb(ushort iigsColor)
{
    // IIgs format: ----RRRRGGGGBBBB (12-bit, little-endian stored)
    int r = (iigsColor >> 8) & 0x0F;
    int g = (iigsColor >> 4) & 0x0F;
    int b = iigsColor & 0x0F;
    
    // Expand 4-bit to 8-bit (multiply by 17 = 0x11)
    r *= 17;
    g *= 17;
    b *= 17;
    
    return 0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | (uint)b;
}

/// <summary>
/// Pre-computed color table for fast rendering.
/// </summary>
public sealed class IIgsColorTable
{
    private readonly uint[] _argbColors = new uint[4096];
    
    public IIgsColorTable()
    {
        // Pre-compute all 4096 possible colors
        for (int i = 0; i < 4096; i++)
        {
            _argbColors[i] = IIgsColorToArgb((ushort)i);
        }
    }
    
    /// <summary>
    /// Gets the ARGB color for a 12-bit IIgs color.
    /// </summary>
    public uint GetArgb(ushort iigsColor) => _argbColors[iigsColor & 0x0FFF];
}
```

### B.2 Rendering Pipeline Architecture

The IIgs rendering pipeline handles both classic IIe modes (via Mega II) and native
Super Hi-Res modes (via VGC):

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       Guest System (Apple IIgs Emulator)                    │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                         Mega II (IIe Compatibility)                  │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                   │   │
│  │  │ Bank $E0    │  │ Bank $E1    │  │ Char Gen    │                   │   │
│  │  │ Text/Lo-Res │  │ (Shadowed)  │  │ ROM         │                   │   │
│  │  │ Hi-Res      │  │             │  │             │                   │   │
│  │  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘                   │   │
│  │         │                │                │                          │   │
│  │         └────────────────┴────────────────┘                          │   │
│  │                          │                                           │   │
│  │                 ┌────────▼────────┐                                  │   │
│  │                 │ Classic Video   │                                  │   │
│  │                 │ Renderer        │                                  │   │
│  │                 └────────┬────────┘                                  │   │
│  └──────────────────────────┼───────────────────────────────────────────┘   │
│                             │                                               │
│  ┌──────────────────────────┼───────────────────────────────────────────┐   │
│  │                    VGC (Video Graphics Controller)                   │   │
│  │                          │                                           │   │
│  │  ┌───────────────┐  ┌────▼────────┐  ┌───────────────┐              │   │
│  │  │ SHR Memory    │  │ Mode Select │  │ Border Color  │              │   │
│  │  │ $E1/2000-9FFF │  │ ($C029)     │  │ ($C034)       │              │   │
│  │  │               │  └─────────────┘  └───────────────┘              │   │
│  │  │ • Pixel Data  │                                                   │   │
│  │  │ • SCBs        │                                                   │   │
│  │  │ • Palettes    │                                                   │   │
│  │  └───────┬───────┘                                                   │   │
│  │          │                                                           │   │
│  │   ┌──────▼──────────────────────────────────────────────────┐       │   │
│  │   │              Super Hi-Res Renderer                       │       │   │
│  │   │                                                          │       │   │
│  │   │  For each scanline (0-199):                             │       │   │
│  │   │    1. Read SCB → mode, palette, fill                     │       │   │
│  │   │    2. Read 160 bytes of pixel data                       │       │   │
│  │   │    3. Lookup colors from selected palette                │       │   │
│  │   │    4. Render to frame buffer                             │       │   │
│  │   │                                                          │       │   │
│  │   └──────────────────────────┬───────────────────────────────┘       │   │
│  └──────────────────────────────┼───────────────────────────────────────┘   │
│                                 │                                           │
│                        ┌────────▼────────┐                                  │
│                        │ IIgsVideoController                               │
│                        │ • Mode state    │                                  │
│                        │ • RenderFrame() │                                  │
│                        └────────┬────────┘                                  │
└─────────────────────────────────┼───────────────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                            IDisplayService                                  │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │                     Frame Buffer (uint[])                               ││
│  │                                                                         ││
│  │     Scaled to window size with chosen interpolation                     ││
│  │                                                                         ││
│  └─────────────────────────────────────────────────────────────────────────┘│
│                                  │                                          │
│                                  ▼                                          │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │                       Post-Processing                                   ││
│  │  • CRT simulation    • Dithering artifacts    • Color correction        ││
│  └─────────────────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────────────────┘
```
