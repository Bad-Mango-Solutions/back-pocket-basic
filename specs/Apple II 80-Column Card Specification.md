# Apple II 80-Column Card Specification

## Document Information

| Field        | Value                                              |
|--------------|----------------------------------------------------|
| Version      | 1.0                                                |
| Date         | 2025-12-28                                         |
| Status       | Initial Draft                                      |
| Applies To   | Pocket2e (Apple IIe)                               |

---

## 1. Overview

The 80-column card (also known as the Extended 80-Column Card or Apple IIe Memory Expansion
Card) is a key component that provides:

1. **80-column text display**: Doubles the standard 40-column display
2. **Auxiliary memory**: 64KB additional RAM for double hi-res graphics and applications
3. **Slot 3 emulation**: Appears as a slot 3 device for compatibility

### 1.1 Card Variants

| Card                    | Memory | Double Hi-Res | Notes                    |
|-------------------------|--------|---------------|--------------------------|
| Apple 80-Column Text    | 1KB    | No            | Text only                |
| Apple Extended 80-Col   | 64KB   | Yes           | Standard IIe Enhanced    |
| Applied Engineering     | 256KB+ | Yes           | Expanded versions        |

### 1.2 Apple IIc Built-In

The Apple IIc has the 80-column/auxiliary memory functionality built into the motherboard,
with the same software interface as the IIe card.

---

## 2. Memory Architecture

### 2.1 Main vs. Auxiliary RAM

The Apple IIe with 80-column card has two 64KB banks:

```
???????????????????????????????????????????????????????????????
?                    Main RAM (64KB)                           ?
???????????????????????????????????????????????????????????????
? $0000-$01FF ? Zero Page & Stack                              ?
? $0200-$03FF ? System variables                               ?
? $0400-$07FF ? Text Page 1 (40-col, odd columns for 80-col)   ?
? $0800-$0BFF ? Text Page 2                                    ?
? $0C00-$1FFF ? Free RAM                                       ?
? $2000-$3FFF ? Hi-Res Page 1 (odd bytes for double hi-res)    ?
? $4000-$5FFF ? Hi-Res Page 2                                  ?
? $6000-$BFFF ? Free RAM                                       ?
? $C000-$CFFF ? I/O and slot ROM                               ?
? $D000-$FFFF ? ROM / Language Card RAM                        ?
???????????????????????????????????????????????????????????????

???????????????????????????????????????????????????????????????
?                  Auxiliary RAM (64KB)                        ?
???????????????????????????????????????????????????????????????
? $0000-$01FF ? Alternate Zero Page & Stack                    ?
? $0200-$03FF ? Alternate system area                          ?
? $0400-$07FF ? 80-col Text Page 1 (even columns)              ?
? $0800-$0BFF ? 80-col Text Page 2 (even columns)              ?
? $0C00-$1FFF ? Free RAM (ProDOS uses for system)              ?
? $2000-$3FFF ? Double Hi-Res Page 1 (even bytes)              ?
? $4000-$5FFF ? Double Hi-Res Page 2 (even bytes)              ?
? $6000-$BFFF ? Free RAM (used by AppleWorks, etc.)            ?
? $C000-$CFFF ? Mirrors main I/O (no aux ROM)                  ?
? $D000-$FFFF ? Auxiliary bank RAM                             ?
???????????????????????????????????????????????????????????????
```

### 2.2 Bank Switching Soft Switches

| Address | Name      | Write | Effect                              |
|---------|-----------|-------|-------------------------------------|
| $C000   | 80STOREOFF| W     | Disable 80STORE mode                |
| $C001   | 80STOREON | W     | Enable 80STORE mode                 |
| $C002   | RDMAINRAM | W     | Read from main RAM ($0200-$BFFF)    |
| $C003   | RDCARDRAM | W     | Read from auxiliary RAM             |
| $C004   | WRMAINRAM | W     | Write to main RAM ($0200-$BFFF)     |
| $C005   | WRCARDRAM | W     | Write to auxiliary RAM              |
| $C006   | SETSLOTCX | W     | Peripheral ROM in $C100-$CFFF       |
| $C007   | SETINTCX  | W     | Internal ROM in $C100-$CFFF         |
| $C008   | SETSTDZP  | W     | Main zero page and stack            |
| $C009   | SETALTZP  | W     | Alternate zero page and stack       |
| $C00A   | SETINTC3  | W     | Internal ROM at $C300               |
| $C00B   | SETSLOTC3 | W     | Slot 3 ROM at $C300                 |
| $C00C   | 80COLOFF  | W     | 40-column display                   |
| $C00D   | 80COLON   | W     | 80-column display                   |
| $C00E   | ALTCHAROFF| W     | Primary character set               |
| $C00F   | ALTCHARON | W     | Alternate character set (MouseText) |

### 2.3 Status Read Switches

| Address | Name      | Read | Bit 7 Meaning                       |
|---------|-----------|------|-------------------------------------|
| $C013   | RDRAMRD   | R    | 1 = Reading auxiliary RAM           |
| $C014   | RDRAMWRT  | R    | 1 = Writing auxiliary RAM           |
| $C015   | RDCXROM   | R    | 1 = Internal ROM selected           |
| $C016   | RDALTZP   | R    | 1 = Alternate zero page active      |
| $C017   | RDC3ROM   | R    | 1 = Slot 3 ROM active               |
| $C018   | RD80STORE | R    | 1 = 80STORE mode active             |
| $C01C   | RDPAGE2   | R    | 1 = Page 2 selected (for 80STORE)   |
| $C01F   | RD80COL   | R    | 1 = 80-column mode active           |

---

## 3. 80STORE Mode

The 80STORE soft switch enables automatic bank switching for display memory:

### 3.1 How 80STORE Works

When 80STORE is enabled ($C001):
- PAGE2 ($C054/$C055) selects main/aux for display memory access
- Text page ($0400-$07FF) switches between banks
- Hi-res page ($2000-$3FFF) switches between banks (if HIRES is on)

```
80STORE on, PAGE1 ($C054): Access main RAM for display
80STORE on, PAGE2 ($C055): Access auxiliary RAM for display
```

### 3.2 80STORE Truth Table

| 80STORE | PAGE2 | HIRES | $0400-$07FF | $2000-$3FFF |
|---------|-------|-------|-------------|-------------|
| Off     | -     | -     | Per RAMRD   | Per RAMRD   |
| On      | Off   | Off   | Main        | Per RAMRD   |
| On      | On    | Off   | Auxiliary   | Per RAMRD   |
| On      | Off   | On    | Main        | Main        |
| On      | On    | On    | Auxiliary   | Auxiliary   |

### 3.3 Code Example: Writing to Both Banks

```assembly
; Write character to 80-column display
; A = character, X = column (0-79), Y = row

Write80Col:
    PHA                 ; Save character
    JSR GetBaseAddr     ; Get base address for row in Y
    TXA
    LSR A               ; Divide column by 2
    TAY                 ; Y = offset within row
    PLA                 ; Restore character
    
    TXA
    AND #$01            ; Check if odd or even column
    BNE WriteOdd
    
WriteEven:
    STA $C055           ; Select PAGE2 (auxiliary) via 80STORE
    STA (Base),Y        ; Write to auxiliary RAM
    STA $C054           ; Back to PAGE1 (main)
    RTS
    
WriteOdd:
    STA $C054           ; Select PAGE1 (main)
    STA (Base),Y        ; Write to main RAM
    RTS
```

---

## 4. 80-Column Text Display

### 4.1 Character Interleaving

In 80-column mode, characters alternate between auxiliary (even) and main (odd) memory:

```
Screen column:  0   1   2   3   4   5   6   7   ...
Memory bank:    AUX MN  AUX MN  AUX MN  AUX MN  ...
Byte offset:    0   0   1   1   2   2   3   3   ...
```

### 4.2 Memory Layout

Same as 40-column text, but with interleaved banks:

| Row | Aux Address | Main Address | Base |
|-----|-------------|--------------|------|
| 0   | $0400       | $0400        | $400 |
| 1   | $0480       | $0480        | $480 |
| 2   | $0500       | $0500        | $500 |
| ... | ...         | ...          | ...  |

### 4.3 Display Refresh

The video hardware reads from both banks simultaneously:

1. Aux byte ? Even pixel columns (0, 2, 4, ...)
2. Main byte ? Odd pixel columns (1, 3, 5, ...)
3. Character ROM ? 7×8 pixel patterns
4. Combined output ? 560 pixels per line (14 per character pair)

---

## 5. Double Hi-Res Graphics

### 5.1 Memory Organization

Double hi-res uses both banks of hi-res memory:

```
Byte position:  0    1    2    3    4    5    6    ...
Memory bank:    AUX  MAIN AUX  MAIN AUX  MAIN AUX  ...
Address:        $2000 $2000 $2001 $2001 $2002 $2002 ...
```

### 5.2 Enabling Double Hi-Res

```assembly
    STA $C00D       ; 80COLON - Required for double hi-res
    STA $C05E       ; DHIRESON (AN3 off) - Enable double hi-res
    STA $C050       ; Graphics mode
    STA $C057       ; Hi-res mode
```

### 5.3 Pixel Format

Each byte provides 7 pixels (bit 7 is different for aux vs main):

**Auxiliary bytes** (bit 7 ignored, always 0):
```
Bits 0-6: 7 pixels (0 or 1)
Bit 7: Must be 0
```

**Main bytes** (bit 7 is palette bit in some modes):
```
Bits 0-6: 7 pixels (0 or 1)  
Bit 7: Palette select (for color mode)
```

### 5.4 Resolution

- **Monochrome**: 560 × 192 pixels (7 pixels per byte × 80 bytes × 192 lines)
- **Color**: 140 × 192 with 16 colors (4 bits per pixel, sliding window)

---

## 6. Alternate Zero Page and Stack

### 6.1 Purpose

The alternate zero page/stack allows:
- Separate contexts for interrupt handlers
- Bank-switching without corrupting zero page
- Additional workspace for programs

### 6.2 Enabling Alternate ZP

```assembly
    STA $C009       ; SETALTZP - Switch to alternate zero page/stack
    ; Zero page is now at aux $0000-$00FF
    ; Stack is now at aux $0100-$01FF
    
    STA $C008       ; SETSTDZP - Switch back to main zero page/stack
```

### 6.3 Language Card Bank Selection

When ALTZP is enabled, language card bank selection also uses auxiliary RAM:

| ALTZP | $D000-$DFFF source   | $E000-$FFFF source |
|-------|----------------------|--------------------|
| Off   | Main LC RAM or ROM   | Main LC RAM or ROM |
| On    | Aux LC RAM or ROM    | Aux LC RAM or ROM  |

---

## 7. Slot 3 Firmware

### 7.1 80-Column Card ROM

The 80-column card's firmware appears at:
- **$C300-$C3FF**: Slot 3 ROM (256 bytes)
- **$C800-$CFFF**: Expansion ROM (2KB when selected)

### 7.2 Firmware Entry Points

| Address | Function                                    |
|---------|---------------------------------------------|
| $C300   | Initialization / Pascal protocol            |
| $C305   | Output character                            |
| $C307   | Input status (not used by 80-col)           |
| $C309   | Input character (not used by 80-col)        |

### 7.3 Internal vs. Slot ROM

The IIe can switch between internal and slot 3 ROM:

```assembly
    STA $C00A       ; SETINTC3 - Use internal 80-col firmware
    STA $C00B       ; SETSLOTC3 - Use slot 3 card ROM (if present)
```

---

## 8. Memory Map Implementation

```csharp
/// <summary>
/// Manages auxiliary memory bank switching for the Apple IIe.
/// </summary>
public sealed class AuxiliaryMemoryController : IBusTarget
{
    private readonly byte[] _mainRam;
    private readonly byte[] _auxRam;
    private bool _ramRead;     // Read from auxiliary
    private bool _ramWrite;    // Write to auxiliary
    private bool _80Store;     // 80STORE mode
    private bool _page2;       // PAGE2 selection
    private bool _hires;       // HIRES mode
    private bool _altZp;       // Alternate zero page
    
    public byte Read8(Addr address, in BusAccess access)
    {
        var bank = SelectReadBank(address);
        return bank[(int)address];
    }
    
    public void Write8(Addr address, byte value, in BusAccess access)
    {
        var bank = SelectWriteBank(address);
        bank[(int)address] = value;
    }
    
    private byte[] SelectReadBank(Addr address)
    {
        // Zero page / stack
        if (address < 0x0200)
            return _altZp ? _auxRam : _mainRam;
        
        // 80STORE display regions
        if (_80Store)
        {
            // Text page
            if (address >= 0x0400 && address < 0x0800)
                return _page2 ? _auxRam : _mainRam;
            
            // Hi-res page (only if HIRES is on)
            if (_hires && address >= 0x2000 && address < 0x4000)
                return _page2 ? _auxRam : _mainRam;
        }
        
        // General RAM ($0200-$BFFF)
        if (address < 0xC000)
            return _ramRead ? _auxRam : _mainRam;
        
        return _mainRam;
    }
    
    private byte[] SelectWriteBank(Addr address)
    {
        // Zero page / stack
        if (address < 0x0200)
            return _altZp ? _auxRam : _mainRam;
        
        // 80STORE display regions
        if (_80Store)
        {
            // Text page
            if (address >= 0x0400 && address < 0x0800)
                return _page2 ? _auxRam : _mainRam;
            
            // Hi-res page (only if HIRES is on)
            if (_hires && address >= 0x2000 && address < 0x4000)
                return _page2 ? _auxRam : _mainRam;
        }
        
        // General RAM ($0200-$BFFF)
        if (address < 0xC000)
            return _ramWrite ? _auxRam : _mainRam;
        
        return _mainRam;
    }
    
    // Soft switch handlers
    public void Handle80StoreOff() => _80Store = false;
    public void Handle80StoreOn() => _80Store = true;
    public void HandleRdMainRam() => _ramRead = false;
    public void HandleRdCardRam() => _ramRead = true;
    public void HandleWrMainRam() => _ramWrite = false;
    public void HandleWrCardRam() => _ramWrite = true;
    public void HandleSetStdZp() => _altZp = false;
    public void HandleSetAltZp() => _altZp = true;
    public void HandlePage1() => _page2 = false;
    public void HandlePage2() => _page2 = true;
    public void HandleLoRes() => _hires = false;
    public void HandleHiRes() => _hires = true;
}
```

---

## 9. Auxiliary Memory Interface

```csharp
/// <summary>
/// Interface for Apple IIe auxiliary memory management.
/// </summary>
public interface IAuxiliaryMemory
{
    // ??? Bank State ?????????????????????????????????????????????????????
    
    /// <summary>Gets whether reads come from auxiliary RAM.</summary>
    bool ReadingAuxiliary { get; }
    
    /// <summary>Gets whether writes go to auxiliary RAM.</summary>
    bool WritingAuxiliary { get; }
    
    /// <summary>Gets whether 80STORE mode is enabled.</summary>
    bool Is80StoreEnabled { get; }
    
    /// <summary>Gets whether alternate zero page is active.</summary>
    bool IsAltZpEnabled { get; }
    
    /// <summary>Gets whether PAGE2 is selected.</summary>
    bool IsPage2Selected { get; }
    
    // ??? Direct Access ??????????????????????????????????????????????????
    
    /// <summary>Gets the main RAM array.</summary>
    Span<byte> MainRam { get; }
    
    /// <summary>Gets the auxiliary RAM array.</summary>
    Span<byte> AuxiliaryRam { get; }
    
    // ??? Soft Switch Handlers ???????????????????????????????????????????
    
    /// <summary>Handles a soft switch access.</summary>
    /// <param name="address">Soft switch address ($C000-$C0FF).</param>
    /// <param name="isWrite">True if write access, false if read.</param>
    /// <returns>Value for read accesses, 0 for writes.</returns>
    byte HandleSoftSwitch(ushort address, bool isWrite);
}
```

---

## 10. MouseText Characters

When the alternate character set is enabled ($C00F), codes $40-$5F display MouseText:

| Code  | Character | Description                  |
|-------|-----------|------------------------------|
| $40   | ?         | Left half block              |
| $41   | ?         | Upper left, lower right      |
| $42   | ?         | Lower left block             |
| $43   | ?         | Horizontal line              |
| $44   | ?         | Upper left block             |
| $45   | ?         | Right half block             |
| $46   | ?         | Diagonal (forward slash)     |
| $47   | ?         | Diagonal (backslash)         |
| $48   | ?         | Upper right block            |
| $49   | ?         | Upper half block             |
| $4A   | ?         | Lower half block             |
| $4B   | ?         | Lower right block            |
| $4C   | ?         | Upper left + upper right + lower left |
| $4D   | ?         | Upper left + upper right + lower right |
| $4E   | ?         | Upper left + lower left + lower right |
| $4F   | ?         | Upper right + lower left + lower right |
| $50   | ?         | Filled apple (open apple key)|
| $51   | ?         | Open apple (closed apple key)|
| $52   | ?         | Pointer right                |
| $53   | ?         | Down arrow                   |
| $54   | ?         | Pointer left                 |
| $55   | ?         | Up arrow                     |
| $56   | ?         | Checkmark                    |
| $57   | ?         | Left arrow                   |
| $58   | ?         | Left/right arrow             |
| $59   | ?         | Up/down arrow                |
| $5A   | ??         | Mouse cursor                 |
| $5B   | ?         | Control key symbol           |
| $5C   | ?         | Return arrow                 |
| $5D   | ?         | Delete key                   |
| $5E   | ?         | Folder                       |
| $5F   | ?         | Command key (option)         |

---

## 11. Implementation Notes

### 11.1 Video RAM Coordination

When rendering 80-column text or double hi-res, coordinate access to both banks:

```csharp
public void Render80ColumnLine(int row, Span<uint> pixels)
{
    int baseAddr = TextBaseAddress(row);
    
    for (int col = 0; col < 40; col++)
    {
        byte auxChar = _auxRam[baseAddr + col];
        byte mainChar = _mainRam[baseAddr + col];
        
        RenderCharacter(auxChar, col * 2, pixels);      // Even column
        RenderCharacter(mainChar, col * 2 + 1, pixels); // Odd column
    }
}
```

### 11.2 Soft Switch Timing

Soft switch accesses have immediate effect:

```csharp
public byte HandleSoftSwitch(ushort address, bool isWrite)
{
    switch (address & 0xFF)
    {
        case 0x00: _80Store = false; break;
        case 0x01: _80Store = true; break;
        case 0x02: _ramRead = false; break;
        case 0x03: _ramRead = true; break;
        case 0x04: _ramWrite = false; break;
        case 0x05: _ramWrite = true; break;
        // ... etc
    }
    
    return FloatingBus();
}
```

### 11.3 ProDOS and AppleWorks

ProDOS and AppleWorks make heavy use of auxiliary memory:

- ProDOS system globals in aux $0800-$0BFF
- AppleWorks desktop in aux $2000-$BFFF
- File buffers in aux $C000-$FFFF (language card area)

---

## Document History

| Version | Date       | Changes                            |
|---------|------------|------------------------------------|
| 1.0     | 2025-12-28 | Initial specification              |

---

## Appendix A: Bus Architecture Integration

This appendix provides implementation guidance for integrating the 80-column card
with the emulator's bus architecture.

### A.1 Auxiliary Memory Controller

The auxiliary memory controller manages bank switching and implements `IBusTarget`:

```csharp
/// <summary>
/// Auxiliary memory controller for Apple IIe 80-column card.
/// </summary>
public sealed class AuxiliaryMemoryController : IBusTarget, IScheduledDevice
{
    private readonly byte[] _mainRam;
    private readonly byte[] _auxRam;
    
    // Bank switching state
    private bool _ramRead;      // RAMRD: Read from auxiliary
    private bool _ramWrite;     // RAMWRT: Write to auxiliary
    private bool _80Store;      // 80STORE mode
    private bool _page2;        // PAGE2 selection
    private bool _hires;        // HIRES mode
    private bool _altZp;        // Alternate zero page
    private bool _80Column;     // 80-column display mode
    private bool _altCharSet;   // Alternate character set
    
    /// <inheritdoc/>
    public TargetCaps Capabilities => TargetCaps.None;
    
    public AuxiliaryMemoryController(int mainSize = 65536, int auxSize = 65536)
    {
        _mainRam = new byte[mainSize];
        _auxRam = new byte[auxSize];
    }
    
    /// <inheritdoc/>
    public byte Read8(Addr physicalAddress, in BusAccess access)
    {
        var bank = SelectReadBank(physicalAddress);
        return bank[(int)(physicalAddress & 0xFFFF)];
    }
    
    /// <inheritdoc/>
    public void Write8(Addr physicalAddress, byte value, in BusAccess access)
    {
        var bank = SelectWriteBank(physicalAddress);
        bank[(int)(physicalAddress & 0xFFFF)] = value;
    }
    
    private byte[] SelectReadBank(Addr address)
    {
        ushort addr = (ushort)(address & 0xFFFF);
        
        // Zero page / stack
        if (addr < 0x0200)
            return _altZp ? _auxRam : _mainRam;
        
        // 80STORE display regions
        if (_80Store)
        {
            // Text page 1
            if (addr >= 0x0400 && addr < 0x0800)
                return _page2 ? _auxRam : _mainRam;
            
            // Hi-res page 1 (only if HIRES is on)
            if (_hires && addr >= 0x2000 && addr < 0x4000)
                return _page2 ? _auxRam : _mainRam;
        }
        
        // General RAM ($0200-$BFFF)
        if (addr < 0xC000)
            return _ramRead ? _auxRam : _mainRam;
        
        return _mainRam;
    }
    
    private byte[] SelectWriteBank(Addr address)
    {
        ushort addr = (ushort)(address & 0xFFFF);
        
        // Zero page / stack
        if (addr < 0x0200)
            return _altZp ? _auxRam : _mainRam;
        
        // 80STORE display regions
        if (_80Store)
        {
            // Text page 1
            if (addr >= 0x0400 && addr < 0x0800)
                return _page2 ? _auxRam : _mainRam;
            
            // Hi-res page 1 (only if HIRES is on)
            if (_hires && addr >= 0x2000 && addr < 0x4000)
                return _page2 ? _auxRam : _mainRam;
        }
        
        // General RAM ($0200-$BFFF)
        if (addr < 0xC000)
            return _ramWrite ? _auxRam : _mainRam;
        
        return _mainRam;
    }
    
    /// <inheritdoc/>
    public void Clear()
    {
        Array.Clear(_mainRam);
        Array.Clear(_auxRam);
    }
}
```

### A.2 Soft Switch Handler

The 80-column soft switches implement `IBusTarget`:

```csharp
/// <summary>
/// 80-column card soft switch handler.
/// </summary>
public sealed class AuxSoftSwitches : IBusTarget
{
    private readonly AuxiliaryMemoryController _auxMemory;
    
    /// <inheritdoc/>
    public TargetCaps Capabilities => TargetCaps.SideEffects;
    
    /// <inheritdoc/>
    public byte Read8(Addr physicalAddress, in BusAccess access)
    {
        byte offset = (byte)(physicalAddress & 0xFF);
        
        // Status reads return bit 7
        if (offset >= 0x13 && offset <= 0x1F)
            return ReadStatus(offset);
        
        // Switch accesses (read or write triggers the switch)
        if (!access.IsSideEffectFree)
            HandleSwitch(offset);
        
        return FloatingBus();
    }
    
    /// <inheritdoc/>
    public void Write8(Addr physicalAddress, byte value, in BusAccess access)
    {
        if (access.IsSideEffectFree)
            return;
        
        byte offset = (byte)(physicalAddress & 0xFF);
        HandleSwitch(offset);
    }
    
    private void HandleSwitch(byte offset)
    {
        switch (offset)
        {
            case 0x00: _auxMemory.Set80Store(false); break;
            case 0x01: _auxMemory.Set80Store(true); break;
            case 0x02: _auxMemory.SetRamRead(false); break;
            case 0x03: _auxMemory.SetRamRead(true); break;
            case 0x04: _auxMemory.SetRamWrite(false); break;
            case 0x05: _auxMemory.SetRamWrite(true); break;
            case 0x08: _auxMemory.SetAltZp(false); break;
            case 0x09: _auxMemory.SetAltZp(true); break;
            case 0x0C: _auxMemory.Set80Column(false); break;
            case 0x0D: _auxMemory.Set80Column(true); break;
            case 0x0E: _auxMemory.SetAltCharSet(false); break;
            case 0x0F: _auxMemory.SetAltCharSet(true); break;
            // Video mode switches affect 80STORE behavior
            case 0x54: _auxMemory.SetPage2(false); break;
            case 0x55: _auxMemory.SetPage2(true); break;
            case 0x56: _auxMemory.SetHires(false); break;
            case 0x57: _auxMemory.SetHires(true); break;
        }
    }
    
    private byte ReadStatus(byte offset)
    {
        bool state = offset switch
        {
            0x13 => _auxMemory.IsRamRead,
            0x14 => _auxMemory.IsRamWrite,
            0x16 => _auxMemory.IsAltZp,
            0x18 => _auxMemory.Is80Store,
            0x1C => _auxMemory.IsPage2,
            0x1F => _auxMemory.Is80Column,
            _ => false
        };
        
        return state ? (byte)0x80 : (byte)0x00;
    }
    
    private byte FloatingBus() => 0xFF;
}
```

### A.3 Composite Page Integration

The 80-column switches are part of the Apple II I/O page:

```csharp
public sealed class AppleIIIOPage : ICompositeTarget
{
    private readonly AuxSoftSwitches _auxSwitches;
    private readonly VideoSoftSwitches _videoSwitches;
    
    /// <inheritdoc/>
    public IBusTarget? ResolveTarget(Addr offset, AccessIntent intent)
    {
        return offset switch
        {
            // Bank switching ($C000-$C00F)
            >= 0x00 and <= 0x0F => _auxSwitches,
            
            // Status reads ($C013-$C01F)
            >= 0x13 and <= 0x1F => _auxSwitches,
            
            // Video mode switches ($C050-$C057) - also affect 80STORE
            >= 0x50 and <= 0x57 => _videoSwitches,
            
            // ...other handlers...
            _ => null
        };
    }
    
    /// <inheritdoc/>
    public RegionTag GetSubRegionTag(Addr offset)
    {
        return offset switch
        {
            >= 0x00 and <= 0x0F => RegionTag.BankSwitch,
            >= 0x13 and <= 0x1F => RegionTag.BankStatus,
            >= 0x50 and <= 0x57 => RegionTag.VideoMode,
            _ => RegionTag.Unknown
        };
    }
}
```

### A.4 Memory Mapping with Region Manager

Configure the auxiliary memory regions:

```csharp
public void ConfigureAuxiliaryMemory(
    IRegionManager regions,
    AuxiliaryMemoryController auxMemory)
{
    // Main RAM ($0000-$BFFF)
    regions.Map(new MemoryRegion(
        baseAddress: 0x0000,
        size: 0xC000,
        tag: RegionTag.MainRam,
        target: auxMemory));  // Controller handles bank selection
    
    // Note: $C000-$CFFF is I/O page (handled by composite)
    // Note: $D000-$FFFF is ROM/Language Card (handled separately)
}
```

### A.5 80-Column Display Rendering

The video controller reads from both banks for 80-column mode:

```csharp
public void Render80ColumnText(
    IMemoryBus bus,
    Span<uint> pixels,
    in BusAccess baseAccess)
{
    // Force reads to go to specific banks
    var mainAccess = baseAccess with { Flags = AccessFlags.NoSideEffects };
    
    for (int row = 0; row < 24; row++)
    {
        ushort baseAddr = TextBaseAddress(row);
        
        for (int col = 0; col < 40; col++)
        {
            // Even columns from auxiliary RAM
            byte auxChar = ReadAuxMemory(baseAddr + col);
            RenderCharacter(auxChar, col * 2, row, pixels);
            
            // Odd columns from main RAM  
            byte mainChar = ReadMainMemory(baseAddr + col);
            RenderCharacter(mainChar, col * 2 + 1, row, pixels);
        }
    }
}

private byte ReadAuxMemory(ushort address)
{
    // Direct access to auxiliary bank
    return _auxMemoryController.AuxRam[address];
}

private byte ReadMainMemory(ushort address)
{
    // Direct access to main bank
    return _auxMemoryController.MainRam[address];
}
```

### A.6 Double Hi-Res Rendering

```csharp
public void RenderDoubleHiRes(
    IMemoryBus bus,
    Span<uint> pixels,
    in BusAccess baseAccess)
{
    for (int row = 0; row < 192; row++)
    {
        ushort baseAddr = HiResBaseAddress(row);
        
        for (int byteCol = 0; byteCol < 40; byteCol++)
        {
            // Even bytes from auxiliary RAM
            byte auxByte = ReadAuxMemory((ushort)(baseAddr + byteCol + 0x2000));
            
            // Odd bytes from main RAM
            byte mainByte = ReadMainMemory((ushort)(baseAddr + byteCol + 0x2000));
            
            // Combine into 14 pixels
            RenderDoubleHiResBytes(auxByte, mainByte, byteCol, row, pixels);
        }
    }
}

private void RenderDoubleHiResBytes(
    byte auxByte, 
    byte mainByte, 
    int byteCol, 
    int row, 
    Span<uint> pixels)
{
    int pixelBase = (row * 560) + (byteCol * 14);
    
    // Aux byte: bits 0-6 = pixels 0-6
    for (int bit = 0; bit < 7; bit++)
    {
        bool on = (auxByte & (1 << bit)) != 0;
        pixels[pixelBase + bit] = on ? _foregroundColor : _backgroundColor;
    }
    
    // Main byte: bits 0-6 = pixels 7-13
    for (int bit = 0; bit < 7; bit++)
    {
        bool on = (mainByte & (1 << bit)) != 0;
        pixels[pixelBase + 7 + bit] = on ? _foregroundColor : _backgroundColor;
    }
}
```

### A.7 Device Registry

```csharp
public void RegisterAuxMemoryDevices(IDeviceRegistry registry)
{
    // Auxiliary memory controller
    registry.Register(
        registry.GenerateId(),
        DevicePageId.Create(DevicePageClass.CompatIO, instance: 0, page: 0),
        kind: "AuxMemory",
        name: "Apple IIe Auxiliary Memory",
        wiringPath: "main/auxmemory");
    
    // 80-column firmware (slot 3)
    registry.Register(
        registry.GenerateId(),
        DevicePageId.Create(DevicePageClass.SlotROM, instance: 3, page: 0),
        kind: "80ColFirmware",
        name: "80-Column Card Firmware",
        wiringPath: "main/slots/3/firmware");
}
```

### A.8 Slot 3 Firmware as IPeripheral

The 80-column card's slot 3 identity:

```csharp
/// <summary>
/// 80-column card slot 3 firmware interface.
/// </summary>
public sealed class EightyColumnCard : IPeripheral
{
    private readonly byte[] _slotRom;
    private readonly byte[] _expansionRom;
    private readonly AuxiliaryMemoryController _auxMemory;
    
    /// <inheritdoc/>
    public string Name => "Extended 80-Column Card";
    
    /// <inheritdoc/>
    public string DeviceType => "80Column";
    
    /// <inheritdoc/>
    public int SlotNumber { get; set; } = 3;
    
    /// <inheritdoc/>
    public IBusTarget? MMIORegion => null;  // Uses soft switches, not MMIO
    
    /// <inheritdoc/>
    public IBusTarget? ROMRegion => new RomTarget(_slotRom);
    
    /// <inheritdoc/>
    public IBusTarget? ExpansionROMRegion => new RomTarget(_expansionRom);
    
    /// <inheritdoc/>
    public void OnExpansionROMSelected()
    {
        // 80-column firmware selected
    }
    
    /// <inheritdoc/>
    public void OnExpansionROMDeselected()
    {
        // 80-column firmware deselected
    }
    
    /// <inheritdoc/>
    public void Reset()
    {
        _auxMemory.Reset();
    }
}
```

### A.9 Trap Handler for 80-Column Firmware

```csharp
/// <summary>
/// Trap handlers for 80-column firmware routines.
/// </summary>
public sealed class EightyColumnTraps
{
    private readonly AuxiliaryMemoryController _auxMemory;
    private readonly ISlotManager _slots;
    
    /// <summary>
    /// Trap for 80-column initialization ($C300).
    /// </summary>
    public TrapResult InitHandler(ICpu cpu, IMemoryBus bus, IEventContext context)
    {
        // Verify internal 80-col ROM is active or slot 3 has card
        if (!CanHandle80Column())
            return new TrapResult(Handled: false, default, null);
        
        // Select expansion ROM
        _slots.SelectExpansionSlot(3);
        
        // Enable 80-column mode
        _auxMemory.Set80Column(true);
        _auxMemory.Set80Store(true);
        
        // Initialize display
        ClearScreen(bus, context);
        
        return new TrapResult(
            Handled: true,
            CyclesConsumed: new Cycle(500),
            ReturnAddress: null);
    }
    
    /// <summary>
    /// Trap for 80-column character output ($C305).
    /// </summary>
    public TrapResult OutputHandler(ICpu cpu, IMemoryBus bus, IEventContext context)
    {
        if (!CanHandle80Column())
            return new TrapResult(Handled: false, default, null);
        
        byte ch = cpu.A;
        
        // Get current cursor position
        int cv = bus.Peek8(0x25);  // Cursor vertical
        int ch80 = bus.Peek8(0x57B);  // 80-col cursor horizontal
        
        // Handle control characters
        if (ch < 0x20)
        {
            HandleControlCharacter(ch, bus);
        }
        else
        {
            // Write character at cursor position
            WriteCharacter80Col(ch, ch80, cv, bus, context);
            
            // Advance cursor
            ch80++;
            if (ch80 >= 80)
            {
                ch80 = 0;
                cv++;
                if (cv >= 24)
                {
                    ScrollScreen(bus, context);
                    cv = 23;
                }
            }
            
            bus.Poke8(0x57B, (byte)ch80);
            bus.Poke8(0x25, (byte)cv);
        }
        
        return new TrapResult(
            Handled: true,
            CyclesConsumed: new Cycle(50),
            ReturnAddress: null);
    }
    
    private void WriteCharacter80Col(
        byte ch, 
        int col, 
        int row,
        IMemoryBus bus,
        IEventContext context)
    {
        ushort baseAddr = TextBaseAddress(row);
        int offset = col / 2;
        
        // Select bank based on column parity
        if ((col & 1) == 0)
        {
            // Even column - auxiliary memory
            _auxMemory.AuxRam[baseAddr + offset] = ch;
        }
        else
        {
            // Odd column - main memory
            _auxMemory.MainRam[baseAddr + offset] = ch;
        }
    }
}
```

### A.10 MouseText Character Set

```csharp
/// <summary>
/// MouseText character generator.
/// </summary>
public sealed class MouseTextGenerator
{
    private readonly byte[,] _mouseTextPatterns = new byte[32, 8];
    
    public MouseTextGenerator()
    {
        InitializePatterns();
    }
    
    private void InitializePatterns()
    {
        // MouseText characters $40-$5F when alt char set enabled
        // Each character is 8 rows of 7 pixels (stored as 8 bits, bit 7 unused)
        
        // $40: Left half block (?)
        _mouseTextPatterns[0x00, 0] = 0b01111000;
        _mouseTextPatterns[0x00, 1] = 0b01111000;
        // ... etc
        
        // $50: Closed Apple (?)
        _mouseTextPatterns[0x10, 0] = 0b00011100;
        _mouseTextPatterns[0x10, 1] = 0b00111110;
        // ... etc
        
        // $51: Open Apple (?)
        _mouseTextPatterns[0x11, 0] = 0b00011100;
        _mouseTextPatterns[0x11, 1] = 0b00100010;
        // ... etc
    }
    
    public ReadOnlySpan<byte> GetCharacterPattern(byte charCode)
    {
        if (charCode < 0x40 || charCode > 0x5F)
            return ReadOnlySpan<byte>.Empty;
        
        int index = charCode - 0x40;
        var pattern = new byte[8];
        for (int i = 0; i < 8; i++)
            pattern[i] = _mouseTextPatterns[index, i];
        
        return pattern;
    }
}
