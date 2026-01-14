# Machine Profile Configuration Guide

## Document Information

| Field        | Value                                              |
|--------------|----------------------------------------------------|
| Version      | 1.0                                                |
| Date         | 2026-01-14                                         |
| Status       | Initial Draft                                      |
| Applies To   | Pocket2e (Apple IIe)                               |

---

## 1. Overview

This document explains how to create a fully functional machine profile that configures all required components for an Apple IIe-compatible emulator, including video, character generation, Language Card, and the Extended 80-Column Card.

Machine profiles are JSON files that define the complete configuration of an emulated system, including:

- **CPU type and clock speed**
- **ROM images to load**
- **Physical memory allocations**
- **Memory region mappings**
- **Motherboard devices**
- **Slot cards**
- **Boot configuration**

---

## 2. Profile Structure

A machine profile is organized into the following major sections:

```json
{
    "$schema": "...",
    "name": "...",
    "displayName": "...",
    "description": "...",
    "cpu": { ... },
    "addressSpace": 16,
    "memory": {
        "rom-images": [ ... ],
        "physical": [ ... ],
        "regions": [ ... ]
    },
    "devices": {
        "motherboard": [ ... ],
        "slots": { ... }
    },
    "boot": { ... }
}
```

---

## 3. Section Reference

### 3.1 CPU Configuration

The `cpu` section defines the processor type and clock speed.

```json
"cpu": {
    "type": "65C02",
    "clockSpeed": 1020484
}
```

| Field | Description |
|-------|-------------|
| `type` | CPU type: `"6502"`, `"65C02"`, or `"65816"` |
| `clockSpeed` | Clock frequency in Hz (Apple IIe standard: 1,020,484 Hz ≈ 1.02 MHz) |

---

### 3.2 Memory: ROM Images

The `memory.rom-images` section declares ROM files that will be loaded and made available for use by physical memory regions and devices.

```json
"rom-images": [
    {
        "name": "boot-rom",
        "source": "embedded://BadMango.Emulator.Devices/BadMango.Emulator.Devices.Resources.boot.rom",
        "size": "0x0800",
        "required": true,
        "on_verification_fail": "stop"
    },
    {
        "name": "basic",
        "source": "library://roms/basic.rom",
        "size": "0x2800",
        "required": true,
        "on_verification_fail": "fallback"
    },
    {
        "name": "character-rom",
        "source": "embedded://BadMango.Emulator.Devices/BadMango.Emulator.Devices.Resources.pocket2-charset.rom",
        "size": "0x1000",
        "required": true,
        "on_verification_fail": "stop"
    }
]
```

| Field | Description |
|-------|-------------|
| `name` | Unique identifier for referencing this ROM elsewhere in the profile |
| `source` | ROM file location (see Source Types below) |
| `size` | Size of the ROM in hexadecimal bytes |
| `required` | Whether the emulator should fail if this ROM cannot be loaded |
| `on_verification_fail` | Action on checksum failure: `"stop"` or `"fallback"` |

#### Source Types

| Prefix | Description | Example |
|--------|-------------|---------|
| `embedded://` | Embedded resource in assembly | `embedded://BadMango.Emulator.Devices/...Resources.boot.rom` |
| `library://` | ROM library path | `library://roms/basic.rom` |
| `file://` | Local filesystem path | `file:///path/to/rom.bin` |

---

### 3.3 Memory: Physical Allocations

The `memory.physical` section defines physical memory blocks that will be mapped into the address space.

```json
"physical": [
    {
        "name": "main-ram-48k",
        "size": "0xC000",
        "fill": "0x00"
    },
    {
        "name": "system-rom-12k",
        "size": "0x3000",
        "sources": [
            {
                "type": "rom-image",
                "name": "basic",
                "rom-image": "basic",
                "offset": "0x0000"
            },
            {
                "type": "rom-image",
                "name": "boot-rom",
                "rom-image": "boot-rom",
                "offset": "0x2800"
            }
        ]
    }
]
```

| Field | Description |
|-------|-------------|
| `name` | Unique identifier for this physical memory block |
| `size` | Size of the allocation in hexadecimal bytes |
| `fill` | Initial fill value for RAM (optional, default `0x00`) |
| `sources` | Array of ROM images to load into this block (for ROM regions) |

#### Source Entry Fields

| Field | Description |
|-------|-------------|
| `type` | Always `"rom-image"` |
| `name` | Descriptive name |
| `rom-image` | Name of the ROM image from `rom-images` section |
| `offset` | Byte offset within the physical block to load the ROM |

---

### 3.4 Memory: Regions

The `memory.regions` section maps physical memory to the CPU's address space.

```json
"regions": [
    {
        "name": "main-ram",
        "type": "ram",
        "start": "0x0000",
        "size": "0xC000",
        "permissions": "rwx",
        "source": "main-ram-48k",
        "source-offset": "0x0000"
    },
    {
        "name": "io-region",
        "type": "composite",
        "start": "0xC000",
        "size": "0x1000",
        "handler": "composite-io"
    },
    {
        "name": "system-rom",
        "type": "rom",
        "start": "0xD000",
        "size": "0x3000",
        "permissions": "rx",
        "source": "system-rom-12k",
        "source-offset": "0x0000"
    }
]
```

| Field | Description |
|-------|-------------|
| `name` | Unique identifier for this region |
| `type` | Region type: `"ram"`, `"rom"`, or `"composite"` |
| `start` | Starting address in CPU address space |
| `size` | Size of the region |
| `permissions` | Access permissions: `r` (read), `w` (write), `x` (execute) |
| `source` | Physical memory block name to map |
| `source-offset` | Offset within the physical block |
| `handler` | For composite regions, the I/O handler name |

#### Region Types

| Type | Description |
|------|-------------|
| `ram` | Read/write RAM region |
| `rom` | Read-only ROM region |
| `composite` | Special I/O region with multiple handlers (soft switches, slot I/O) |

---

### 3.5 Devices: Motherboard

The `devices.motherboard` section configures built-in devices that are always present on the motherboard.

```json
"motherboard": [
    { "type": "keyboard", "name": "Keyboard", "enabled": true },
    {
        "type": "video",
        "name": "Video Device",
        "enabled": true
    },
    {
        "type": "character",
        "name": "Character Generator",
        "enabled": true,
        "config": {
            "character-rom": "character-rom"
        }
    },
    {
        "type": "speaker",
        "name": "Speaker",
        "enabled": true
    },
    {
        "type": "languagecard",
        "name": "Language Card",
        "enabled": true
    },
    {
        "type": "extended80column",
        "name": "Extended 80-Column Card",
        "enabled": true,
        "config": {
            "expansion-rom": "80col-expansion-rom"
        }
    }
]
```

| Field | Description |
|-------|-------------|
| `type` | Device type identifier (see Device Types below) |
| `name` | Display name for the device |
| `enabled` | Whether the device is active |
| `config` | Device-specific configuration options |

#### Device Types

| Type | Description |
|------|-------------|
| `keyboard` | Keyboard controller ($C000, $C010) |
| `video` | Video display controller |
| `character` | Character generator (ROM/RAM and ALTCHAR switch) |
| `speaker` | Speaker toggle ($C030) |
| `languagecard` | Language Card 16KB RAM ($C080-$C08F) |
| `extended80column` | Extended 80-Column Card with 64KB auxiliary RAM |

#### Device Configuration: Character Generator

The `character` device requires a ROM image reference:

```json
{
    "type": "character",
    "name": "Character Generator",
    "enabled": true,
    "config": {
        "character-rom": "character-rom"
    }
}
```

| Config Field | Description |
|--------------|-------------|
| `character-rom` | Name of the ROM image containing character bitmaps (4KB) |

#### Device Configuration: Extended 80-Column Card

The `extended80column` device provides 64KB auxiliary RAM and 80-column text support:

```json
{
    "type": "extended80column",
    "name": "Extended 80-Column Card",
    "enabled": true,
    "config": {
        "expansion-rom": "80col-expansion-rom"
    }
}
```

| Config Field | Description |
|--------------|-------------|
| `expansion-rom` | (Optional) Name of the ROM image for expansion ROM at $C100-$CFFF |

The Extended 80-Column Card controls these soft switches:

| Address | Name | Function |
|---------|------|----------|
| $C000/$C001 | 80STORE | PAGE2 controls display memory bank selection |
| $C002/$C003 | RAMRD | Read from auxiliary RAM ($0200-$BFFF) |
| $C004/$C005 | RAMWRT | Write to auxiliary RAM ($0200-$BFFF) |
| $C006/$C007 | INTCXROM | Internal ROM at $C100-$CFFF |
| $C008/$C009 | ALTZP | Alternate zero page and stack |
| $C00A/$C00B | SLOTC3ROM | Slot 3 ROM control |
| $C00C/$C00D | 80COL | 80-column display mode |

---

### 3.6 Devices: Slots

The `devices.slots` section configures the peripheral slot system.

```json
"slots": {
    "io-region": "io-region",
    "enabled": true,
    "internalC3Rom": false,
    "internalCxRom": false,
    "cards": [
        {
            "slot": 4,
            "type": "pocketwatch"
        }
    ]
}
```

| Field | Description |
|-------|-------------|
| `io-region` | Name of the composite I/O region from `memory.regions` |
| `enabled` | Enable/disable slot system |
| `internalC3Rom` | Use internal ROM at $C300 instead of slot 3 card |
| `internalCxRom` | Use internal ROM at $C100-$CFFF instead of peripheral ROMs |
| `cards` | Array of slot card configurations |

#### Slot Card Configuration

```json
{
    "slot": 4,
    "type": "pocketwatch",
    "config": { ... }
}
```

| Field | Description |
|-------|-------------|
| `slot` | Slot number (1-7) |
| `type` | Card type identifier |
| `config` | Card-specific configuration |

---

### 3.7 Boot Configuration

The `boot` section controls startup behavior.

```json
"boot": {
    "autoStart": false,
    "autoVideoWindowOpen": true
}
```

| Field | Description |
|-------|-------------|
| `autoStart` | Start emulation automatically when profile loads |
| `autoVideoWindowOpen` | Open the video display window automatically |
| `startupSlot` | (Optional) Slot number to boot from (e.g., 6 for disk) |

---

## 4. Complete Example: Pocket2e with Extended 80-Column Card

This example shows a complete profile based on `pocket2e-lite.json` with the Extended 80-Column Card configured for 80-column text and 128KB total RAM.

```json
{
    "$schema": "https://bad-mango-solutions.github.io/schemas/machine-profile.schema.json",
    "name": "pocket2e-80col",
    "displayName": "Pocket2e with 80-Column Card",
    "description": "Pocket2e system with Extended 80-Column Card providing 64KB auxiliary RAM, 80-column text display, double hi-res graphics, and full peripheral emulation.",

    "cpu": {
        "type": "65C02",
        "clockSpeed": 1020484
    },

    "addressSpace": 16,

    "memory": {
        "rom-images": [
            {
                "name": "boot-rom",
                "source": "embedded://BadMango.Emulator.Devices/BadMango.Emulator.Devices.Resources.boot.rom",
                "size": "0x0800",
                "required": true,
                "on_verification_fail": "stop"
            },
            {
                "name": "basic",
                "source": "library://roms/basic.rom",
                "size": "0x2800",
                "required": true,
                "on_verification_fail": "fallback"
            },
            {
                "name": "character-rom",
                "source": "embedded://BadMango.Emulator.Devices/BadMango.Emulator.Devices.Resources.pocket2-charset.rom",
                "size": "0x1000",
                "required": true,
                "on_verification_fail": "stop"
            },
            {
                "name": "80col-expansion-rom",
                "source": "library://roms/80col-expansion.rom",
                "size": "0x0F00",
                "required": false,
                "on_verification_fail": "fallback"
            }
        ],
        "physical": [
            {
                "name": "main-ram-48k",
                "size": "0xC000",
                "fill": "0x00"
            },
            {
                "name": "system-rom-12k",
                "size": "0x3000",
                "sources": [
                    {
                        "type": "rom-image",
                        "name": "basic",
                        "rom-image": "basic",
                        "offset": "0x0000"
                    },
                    {
                        "type": "rom-image",
                        "name": "boot-rom",
                        "rom-image": "boot-rom",
                        "offset": "0x2800"
                    }
                ]
            }
        ],
        "regions": [
            {
                "name": "main-ram",
                "type": "ram",
                "start": "0x0000",
                "size": "0xC000",
                "permissions": "rwx",
                "source": "main-ram-48k",
                "source-offset": "0x0000"
            },
            {
                "name": "io-region",
                "type": "composite",
                "start": "0xC000",
                "size": "0x1000",
                "handler": "composite-io"
            },
            {
                "name": "system-rom",
                "type": "rom",
                "start": "0xD000",
                "size": "0x3000",
                "permissions": "rx",
                "source": "system-rom-12k",
                "source-offset": "0x0000"
            }
        ]
    },

    "devices": {
        "motherboard": [
            {
                "type": "keyboard",
                "name": "Keyboard",
                "enabled": true
            },
            {
                "type": "video",
                "name": "Video Device",
                "enabled": true
            },
            {
                "type": "character",
                "name": "Character Generator",
                "enabled": true,
                "config": {
                    "character-rom": "character-rom"
                }
            },
            {
                "type": "speaker",
                "name": "Speaker",
                "enabled": true
            },
            {
                "type": "languagecard",
                "name": "Language Card",
                "enabled": true
            },
            {
                "type": "extended80column",
                "name": "Extended 80-Column Card",
                "enabled": true,
                "config": {
                    "expansion-rom": "80col-expansion-rom"
                }
            }
        ],
        "slots": {
            "io-region": "io-region",
            "enabled": true,
            "internalC3Rom": false,
            "internalCxRom": false,
            "cards": [
                {
                    "slot": 4,
                    "type": "pocketwatch"
                }
            ]
        }
    },

    "boot": {
        "autoStart": false,
        "autoVideoWindowOpen": true
    }
}
```

---

## 5. Extended 80-Column Card Memory Model

When the Extended 80-Column Card is enabled, the system has access to 128KB of RAM:

### 5.1 Memory Banks

| Bank | Size | Address Range | Purpose |
|------|------|---------------|---------|
| Main | 64KB | $0000-$FFFF | Primary RAM, ROM, I/O |
| Auxiliary | 64KB | $0000-$BFFF | Secondary RAM for 80-col, double hi-res |

### 5.2 Bank Switching

The Extended 80-Column Card creates memory layers that overlay main memory:

| Layer | Address | Soft Switch | Purpose |
|-------|---------|-------------|---------|
| AUX_ZP | $0000-$01FF | ALTZP | Alternate zero page and stack |
| AUX_RAM | $0200-$BFFF | RAMRD/RAMWRT | General auxiliary RAM |
| AUX_TEXT | $0400-$07FF | 80STORE+PAGE2 | 80-column text page |
| AUX_HIRES1 | $2000-$3FFF | 80STORE+PAGE2+HIRES | Double hi-res page 1 |
| INT_CXROM | $C100-$CFFF | INTCXROM | Internal expansion ROM |

### 5.3 80-Column Text Memory Interleaving

In 80-column mode, text characters are interleaved between main and auxiliary memory:

| Column Position | Memory Bank | Address Range |
|-----------------|-------------|---------------|
| Even (0, 2, 4...) | Auxiliary | $0400-$07FF |
| Odd (1, 3, 5...) | Main | $0400-$07FF |

This allows 80 characters per line using the standard 40-column memory layout in each bank.

---

## 6. References

- [Apple II 80-Column Card Specification](../video/Apple%20II%2080-Column%20Card%20Specification.md)
- [Apple II Soft Switches Reference](Apple%20II%20Soft%20Switches%20Reference.md)
- [Apple II Language Card Technical Reference](Apple%20II%20Language%20Card%20Technical%20Reference%20and%20Memory%20Mapping%20Analysis.md)
