# Keyboard Mapping Specification

**Document Purpose:** Defines how PC keyboard input maps to Apple II-family and native machine keyboards.  
**Version:** 1.0  
**Date:** 2025-01-13

---

## Overview

This document specifies keyboard mapping for the three machine personalities:

| Machine | Keyboard Model | Primary Era | Special Requirements |
|---------|---------------|-------------|---------------------|
| **Pocket2e** | Apple IIe | 1983 | Open/Closed Apple keys, RESET |
| **PocketGS** | Apple IIgs | 1986 | ADB-style with keypad, Option/Command |
| **PocketME** | Modern PC | Native | Standard 104-key layout |

The **PocketME** uses standard PC keyboard semantics natively. This spec focuses primarily on the **Pocket2e** and **PocketGS** mappings.

---

## Pocket2e Keyboard (Apple IIe)

### Physical Layout Reference

The Apple IIe keyboard is a 63-key ASCII keyboard with:
- Main QWERTY section
- No numeric keypad
- Two "Apple" keys (Open-Apple ? and Closed-Apple ?)
- RESET key (directly triggers CPU reset)
- CTRL and SHIFT modifiers
- No separate function keys (F1-F12)

### Key Generation Model

The Apple IIe keyboard generates 7-bit ASCII codes with the high bit (bit 7) used as a "strobe" indicator:

```
$C000 (KBD)    - Read keyboard data (bit 7 = key available, bits 0-6 = ASCII)
$C010 (KBDSTR) - Clear keyboard strobe (any access clears bit 7 of $C000)
```

### PC to Apple IIe Key Mapping

#### Alphanumeric Keys (Direct Mapping)

| PC Key | Apple IIe Code | Notes |
|--------|---------------|-------|
| A-Z | $C1-$DA | Uppercase by default |
| a-z | $E1-$FA | With SHIFT pressed, generates uppercase |
| 0-9 | $B0-$B9 | Top row numbers |
| Space | $A0 | |
| Return/Enter | $8D | Carriage return |
| Backspace | $88 | Left arrow / delete |
| Tab | $89 | (IIe enhanced only) |
| Escape | $9B | |

#### Shifted Symbol Keys

| PC Key | Unshifted | Shifted |
|--------|-----------|---------|
| 1 | $B1 (1) | $A1 (!) |
| 2 | $B2 (2) | $C0 (@) |
| 3 | $B3 (3) | $A3 (#) |
| 4 | $B4 (4) | $A4 ($) |
| 5 | $B5 (5) | $A5 (%) |
| 6 | $B6 (6) | $DE (^) |
| 7 | $B7 (7) | $A6 (&) |
| 8 | $B8 (8) | $AA (*) |
| 9 | $B9 (9) | $A8 (() |
| 0 | $B0 (0) | $A9 ()) |
| - | $AD (-) | $DF (_) |
| = | $BD (=) | $AB (+) |
| [ | $DB ([) | $FB ({) |
| ] | $DD (]) | $FD (}) |
| \ | $DC (\) | $FC (\|) |
| ; | $BB (;) | $BA (:) |
| ' | $A7 (') | $A2 (") |
| , | $AC (,) | $BC (<) |
| . | $AE (.) | $BE (>) |
| / | $AF (/) | $BF (?) |
| ` | $E0 (\`) | $FE (~) |

#### Control Key Combinations

When CTRL is held, alphabetic keys generate control codes:

| PC Keys | Apple IIe Code | Common Name |
|---------|---------------|-------------|
| CTRL+A | $81 | SOH |
| CTRL+B | $82 | STX |
| CTRL+C | $83 | ETX (Break) |
| CTRL+D | $84 | EOT |
| CTRL+G | $87 | BEL (Bell) |
| CTRL+H | $88 | BS (Backspace) |
| CTRL+I | $89 | HT (Tab) |
| CTRL+J | $8A | LF (Line Feed) |
| CTRL+K | $8B | VT |
| CTRL+L | $8C | FF (Clear Screen) |
| CTRL+M | $8D | CR (Return) |
| CTRL+N | $8E | SO |
| CTRL+P | $90 | DLE |
| CTRL+Q | $91 | DC1 (XON) |
| CTRL+R | $92 | DC2 |
| CTRL+S | $93 | DC3 (XOFF) |
| CTRL+X | $98 | CAN |
| CTRL+[ | $9B | ESC |

#### Apple Key Mappings

The Open-Apple and Closed-Apple keys don't generate keyboard codes—they're read via soft switches:

| PC Key | Apple IIe Function | Soft Switch |
|--------|-------------------|-------------|
| Left Alt | Open-Apple (?) | $C061 bit 7 |
| Right Alt | Closed-Apple (?) | $C062 bit 7 |
| **Alternative:** | | |
| Left Windows | Open-Apple (?) | $C061 bit 7 |
| Right Windows | Closed-Apple (?) | $C062 bit 7 |

#### Arrow Keys

| PC Key | Apple IIe Code | Notes |
|--------|---------------|-------|
| Left Arrow | $88 | Same as Backspace |
| Right Arrow | $95 | CTRL+U |
| Up Arrow | $8B | CTRL+K |
| Down Arrow | $8A | CTRL+J (Line Feed) |

#### Special Key Mappings

| PC Key | Apple IIe Function | Implementation |
|--------|-------------------|----------------|
| F12 | RESET | Triggers CPU reset (like CTRL+RESET) |
| CTRL+F12 | Cold RESET | Full system reset |
| Pause/Break | CTRL+C | Generates $83 (Break) |
| Insert | N/A | Ignored or user-configurable |
| Delete | $FF or N/A | IIe enhanced: forward delete |
| Home | CTRL+Q | Or user-configurable |
| End | CTRL+E | Or user-configurable |
| Page Up | CTRL+U | Or user-configurable |
| Page Down | CTRL+D | Or user-configurable |

---

## PocketGS Keyboard (Apple IIgs)

### Physical Layout Reference

The Apple IIgs keyboard (ADB) is a 105-key keyboard with:
- Full QWERTY layout
- Numeric keypad
- Function keys F1-F15
- Command (?) and Option keys
- Control key
- Caps Lock with LED
- Separate cursor keys (inverted-T)
- Clear key on numeric keypad

### Key Generation Model

The IIgs uses ADB (Apple Desktop Bus) with a more sophisticated model:

```
$C000 (KBD)    - Read keyboard data (same as IIe for compatibility)
$C010 (KBDSTR) - Clear keyboard strobe
$C025 (KEYMOD) - Modifier key status register
```

**Modifier Status Register ($C025):**
```
Bit 7: Command (?) key
Bit 6: Option key
Bit 5: Reserved
Bit 4: Reserved
Bit 3: Caps Lock
Bit 2: Reserved
Bit 1: Control key
Bit 0: Shift key
```

### PC to Apple IIgs Key Mapping

#### Basic Keys

Same as Pocket2e for alphanumeric and punctuation keys, with these additions:

#### Function Keys

| PC Key | IIgs Scancode | Common Use |
|--------|--------------|------------|
| F1 | $7A | Help |
| F2 | $78 | Edit |
| F3 | $63 | Cut |
| F4 | $76 | Copy |
| F5 | $60 | Paste |
| F6 | $61 | Clear |
| F7 | $62 | (Application-defined) |
| F8 | $64 | (Application-defined) |
| F9 | $65 | (Application-defined) |
| F10 | $6D | (Application-defined) |
| F11 | $67 | (Application-defined) |
| F12 | $6F | (Application-defined) |

#### Numeric Keypad

| PC Numpad Key | IIgs Code | Notes |
|---------------|-----------|-------|
| Num Lock | Toggle | Controls numpad behavior |
| / | $2F | Keypad divide |
| * | $2A | Keypad multiply |
| - | $2D | Keypad minus |
| + | $2B | Keypad plus |
| Enter | $8D | Keypad enter (same as Return) |
| . | $2E | Keypad decimal |
| 0-9 | $30-$39 | Keypad numbers (with Num Lock) |
| Clear | $1B | Keypad clear (ESC equivalent) |

#### Modifier Key Mappings

| PC Key | IIgs Function | $C025 Bit |
|--------|--------------|-----------|
| Left Shift | Shift | Bit 0 |
| Right Shift | Shift | Bit 0 |
| Left Ctrl | Control | Bit 1 |
| Right Ctrl | Control | Bit 1 |
| Caps Lock | Caps Lock | Bit 3 |
| Left Alt | Option | Bit 6 |
| Right Alt | Option | Bit 6 |
| Left Windows | Command (?) | Bit 7 |
| Right Windows | Command (?) | Bit 7 |

#### Arrow Keys (Separate from IIe)

| PC Key | IIgs Scancode | ASCII (if generated) |
|--------|--------------|---------------------|
| Up | $3E | $0B (VT) |
| Down | $3D | $0A (LF) |
| Left | $3B | $08 (BS) |
| Right | $3C | $15 (NAK) |

#### Special Key Mappings

| PC Key | IIgs Function | Notes |
|--------|--------------|-------|
| F12 | Reset | Same as Pocket2e |
| CTRL+F12 | Cold Reset | Same as Pocket2e |
| Scroll Lock | Control Panel | Opens IIgs control panel |
| Print Screen | Screen capture | If implemented |

---

## PocketME Keyboard (Native)

The **PocketME** runs as a modern 65832-based system and uses standard PC keyboard semantics:

- Standard USB HID key codes
- No translation layer
- OS-level keyboard handling
- Full modifier support (Ctrl, Alt, Shift, Win/Super)
- Function keys F1-F24
- Multimedia keys (if supported by host)

### Reserved Key Combinations

Even in native mode, some keys are reserved for emulator control:

| Key Combination | Function |
|-----------------|----------|
| CTRL+ALT+F12 | Open emulator menu |
| CTRL+ALT+R | Reset machine |
| CTRL+ALT+Q | Quit emulator |
| CTRL+ALT+P | Pause/resume |
| CTRL+ALT+S | Save state |
| CTRL+ALT+L | Load state |

---

## Implementation Architecture

### IKeyboardController Interface

```csharp
/// <summary>
/// Abstracts keyboard input handling for different machine personalities.
/// </summary>
public interface IKeyboardController
{
    /// <summary>
    /// Gets whether a key is currently available in the buffer.
    /// </summary>
    bool KeyAvailable { get; }
    
    /// <summary>
    /// Reads the current key code (with strobe bit for Apple II modes).
    /// </summary>
    byte ReadKey();
    
    /// <summary>
    /// Clears the keyboard strobe.
    /// </summary>
    void ClearStrobe();
    
    /// <summary>
    /// Gets the current modifier key state.
    /// </summary>
    KeyModifiers GetModifiers();
    
    /// <summary>
    /// Injects a key press (for paste operations, etc.).
    /// </summary>
    void InjectKey(byte keyCode);
    
    /// <summary>
    /// Resets the keyboard controller state.
    /// </summary>
    void Reset();
}
```

### AppleIIeKeyboardController

```csharp
/// <summary>
/// Keyboard controller for Apple IIe compatibility mode.
/// </summary>
public sealed class AppleIIeKeyboardController : IKeyboardController, IBusTarget
{
    private byte _keyBuffer;
    private bool _strobeSet;
    private bool _openAppleDown;
    private bool _closedAppleDown;
    
    // Soft switch reads for Apple keys
    // $C061 - Open Apple (button 0)
    // $C062 - Closed Apple (button 1)
    // $C063 - Button 2 (optional)
    
    public byte Read(BusAccess access)
    {
        return access.Address switch
        {
            0xC000 => (byte)(_keyBuffer | (_strobeSet ? 0x80 : 0x00)),
            0xC010 => ClearStrobeAndReturn(),
            0xC061 => (byte)(_openAppleDown ? 0x80 : 0x00),
            0xC062 => (byte)(_closedAppleDown ? 0x80 : 0x00),
            _ => 0x00
        };
    }
}
```

### Keyboard Event Flow

```
???????????????????     ????????????????????     ???????????????????
?   Host OS       ???????  Key Translator  ???????  Keyboard       ?
?   (PC Keyboard) ?     ?  (Personality)   ?     ?  Controller     ?
???????????????????     ????????????????????     ???????????????????
                                                         ?
                                                         ?
                                                 ???????????????????
                                                 ?  $C000 Buffer   ?
                                                 ?  + Strobe       ?
                                                 ???????????????????
```

---

## Configuration Options

### User-Configurable Mappings

The emulator should support user-customizable key mappings via configuration:

```json
{
  "keyboard": {
    "personality": "Pocket2e",
    "mappings": {
      "openApple": "LeftAlt",
      "closedApple": "RightAlt",
      "reset": "F12",
      "coldReset": "Ctrl+F12",
      "paste": "Ctrl+V"
    },
    "options": {
      "repeatRate": 15,
      "repeatDelay": 500,
      "capsLockBehavior": "Toggle"
    }
  }
}
```

### Paste Support

For pasting text from the host clipboard:

1. Convert clipboard text to Apple II character codes
2. Queue characters in keyboard buffer
3. Respect keyboard timing (simulated repeat rate)
4. Handle line endings (convert CRLF ? CR)

---

## International Keyboard Considerations

### Character Set Limitations

The Apple II uses 7-bit ASCII with MouseText extensions. Characters outside this range should be:
- Transliterated where possible (é ? e)
- Ignored if no mapping exists
- Logged as warnings in debug mode

### Future Localization

Support for non-US keyboard layouts may include:
- AZERTY (French)
- QWERTZ (German)
- UK layout (minor differences)

This would require additional translation tables per locale.

---

## Testing Requirements

### Unit Tests

1. **Key code generation** - Verify correct codes for all key combinations
2. **Modifier handling** - Test Shift, Ctrl, Apple key behavior
3. **Strobe behavior** - Verify $C000/$C010 semantics
4. **Buffer overflow** - Ensure proper handling of rapid key input

### Integration Tests

1. **GET statement** - BASIC `GET A$` receives correct characters
2. **INPUT statement** - Line editing with backspace works
3. **Control characters** - CTRL+C breaks properly
4. **Apple keys** - Detected correctly via $C061/$C062

---

## References

- Apple IIe Technical Reference Manual - Chapter 7: Keyboard
- Apple IIgs Hardware Reference - Chapter 6: ADB and Keyboard
- Inside the Apple IIe - Jim Sather
- Understanding the Apple IIe - Jim Sather

---

*Document last updated: 2025-01-13*
