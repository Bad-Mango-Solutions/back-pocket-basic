# 6502 Emulation

Details about the CPU emulation architecture and Apple II hardware emulation.

## Overview

BackPocketBASIC includes two levels of CPU emulation:

1. **Legacy Emulation** (`BadMango.Basic.Emulation`) - A focused 6502 implementation integrated with the BASIC interpreter for `PEEK`, `POKE`, and `CALL` operations.

2. **Advanced Emulator Framework** (`BadMango.Emulator.*`) - A comprehensive, modular emulator supporting multiple CPU variants (65C02, 65816, 65832) with accurate peripheral emulation.

## Table of Contents

- [Why Emulate the 6502?](#why-emulate-the-6502)
- [CPU Architecture](#cpu-architecture)
- [Multi-CPU Framework](#multi-cpu-framework)
- [Instruction Set](#instruction-set)
- [Memory System](#memory-system)
- [Hardware Emulation](#hardware-emulation)
- [Usage in BASIC](#usage-in-basic)

## Why Emulate the 6502?

### Historical Accuracy

The Apple II used the MOS 6502 CPU. To faithfully reproduce Applesoft BASIC behavior, we need to emulate:

- Memory-mapped I/O
- PEEK and POKE operations
- Machine code execution via CALL
- Memory layout and addressing

### Educational Value

Understanding the 6502 provides insights into:

- Early computer architecture
- Assembly language programming
- How BASIC interacted with hardware
- Computing history

### Functional Requirements

Applesoft BASIC programs use low-level operations:

- `PEEK(address)` - Read memory byte
- `POKE address, value` - Write memory byte
- `CALL address` - Execute machine code

These require an emulated CPU and memory space.

## CPU Architecture

### MOS 6502 Overview

**Specifications:**
- 8-bit processor
- 16-bit address bus (64KB address space)
- 1-3 MHz clock speed (Apple II: 1.023 MHz)
- Little-endian byte order
- No multiply or divide instructions

### Registers

The 6502 has minimal registers:

| Register | Size | Purpose |
|----------|------|---------|
| **A** | 8-bit | Accumulator (primary data register) |
| **X** | 8-bit | Index register X |
| **Y** | 8-bit | Index register Y |
| **SP** | 8-bit | Stack pointer ($0100-$01FF) |
| **PC** | 16-bit | Program counter |
| **P** | 8-bit | Processor status flags |

**Note**: The emulator uses `SP` (Stack Pointer) consistently throughout the codebase for accuracy. While the 6502 documentation sometimes abbreviates this as `S`, using `SP` is more precise and matches conventions from other processor architectures, improving code clarity and maintainability.

### Processor Status Flags (P Register)

```
  7  6  5  4  3  2  1  0
  N  V  -  B  D  I  Z  C

N = Negative flag
V = Overflow flag
- = Unused (always 1)
B = Break flag
D = Decimal mode
I = Interrupt disable
Z = Zero flag
C = Carry flag
```

**Flag Meanings:**

- **N (Negative)**: Set if result is negative (bit 7 = 1)
- **V (Overflow)**: Set on signed arithmetic overflow
- **B (Break)**: Set when BRK instruction executed
- **D (Decimal)**: Enables BCD arithmetic mode
- **I (Interrupt)**: Disables interrupts when set
- **Z (Zero)**: Set if result is zero
- **C (Carry)**: Set on carry/borrow

---

## Instruction Set

### Instruction Categories

#### Load/Store

| Opcode | Instruction | Description |
|--------|-------------|-------------|
| LDA | Load Accumulator | A = memory |
| LDX | Load X Register | X = memory |
| LDY | Load Y Register | Y = memory |
| STA | Store Accumulator | memory = A |
| STX | Store X Register | memory = X |
| STY | Store Y Register | memory = Y |

#### Transfer

| Opcode | Instruction | Description |
|--------|-------------|-------------|
| TAX | Transfer A to X | X = A |
| TAY | Transfer A to Y | Y = A |
| TXA | Transfer X to A | A = X |
| TYA | Transfer Y to A | A = Y |
| TSX | Transfer SP to X | X = SP |
| TXS | Transfer X to SP | SP = X |

#### Arithmetic

| Opcode | Instruction | Description |
|--------|-------------|-------------|
| ADC | Add with Carry | A = A + memory + C |
| SBC | Subtract with Carry | A = A - memory - (1-C) |
| INC | Increment Memory | memory = memory + 1 |
| INX | Increment X | X = X + 1 |
| INY | Increment Y | Y = Y + 1 |
| DEC | Decrement Memory | memory = memory - 1 |
| DEX | Decrement X | X = X - 1 |
| DEY | Decrement Y | Y = Y - 1 |

#### Logic

| Opcode | Instruction | Description |
|--------|-------------|-------------|
| AND | Logical AND | A = A & memory |
| ORA | Logical OR | A = A \| memory |
| EOR | Exclusive OR | A = A ^ memory |
| BIT | Bit Test | Test bits in memory |

#### Shift/Rotate

| Opcode | Instruction | Description |
|--------|-------------|-------------|
| ASL | Arithmetic Shift Left | Shift left one bit |
| LSR | Logical Shift Right | Shift right one bit |
| ROL | Rotate Left | Rotate left through carry |
| ROR | Rotate Right | Rotate right through carry |

#### Branch

| Opcode | Instruction | Description |
|--------|-------------|-------------|
| BCC | Branch if Carry Clear | If C = 0 |
| BCS | Branch if Carry Set | If C = 1 |
| BEQ | Branch if Equal | If Z = 1 |
| BNE | Branch if Not Equal | If Z = 0 |
| BMI | Branch if Minus | If N = 1 |
| BPL | Branch if Plus | If N = 0 |
| BVC | Branch if Overflow Clear | If V = 0 |
| BVS | Branch if Overflow Set | If V = 1 |

#### Jump/Call

| Opcode | Instruction | Description |
|--------|-------------|-------------|
| JMP | Jump | PC = address |
| JSR | Jump to Subroutine | Push PC, jump |
| RTS | Return from Subroutine | Pop PC |
| RTI | Return from Interrupt | Pop P and PC |

#### Stack

| Opcode | Instruction | Description |
|--------|-------------|-------------|
| PHA | Push Accumulator | Push A to stack |
| PHP | Push Processor Status | Push P to stack |
| PLA | Pull Accumulator | Pop A from stack |
| PLP | Pull Processor Status | Pop P from stack |

#### System

| Opcode | Instruction | Description | Addressing | Implemented |
|--------|-------------|-------------|------------|-------------|
| **BRK** | **Break** | **Trigger software interrupt** | **Implied** | **✅** |
| **NOP** | **No Operation** | **Do nothing** | **Implied** | **✅** |
| **CLC** | **Clear Carry** | **C = 0** | **Implied** | **✅** |
| **SEC** | **Set Carry** | **C = 1** | **Implied** | **✅** |
| **CLI** | **Clear Interrupt** | **I = 0** | **Implied** | **✅** |
| **SEI** | **Set Interrupt** | **I = 1** | **Implied** | **✅** |
| **CLV** | **Clear Overflow** | **V = 0** | **Implied** | **✅** |
| **CLD** | **Clear Decimal** | **D = 0** | **Implied** | **✅** |
| **SED** | **Set Decimal** | **D = 1** | **Implied** | **✅** |
| CMP | Compare Accumulator | Compare A with memory | Various | ⏳ |
| CPX | Compare X Register | Compare X with memory | Various | ⏳ |
| CPY | Compare Y Register | Compare Y with memory | Various | ⏳ |

**Note**: All flag manipulation instructions (**CLC, SEC, CLI, SEI, CLV, CLD, SED**) use the **Implied** addressing mode and are implemented as compositional higher-order functions in `Instructions.cs`.

### Addressing Modes

The 6502 supports multiple addressing modes:

| Mode | Syntax | Example | Description |
|------|--------|---------|-------------|
| **Implied** | - | `CLC`, `NOP` | No operand needed |
| Accumulator | A | `ASL A` | Operate on A |
| **Immediate** | #nn | `LDA #$42` | Use literal value |
| **Zero Page** | nn | `LDA $20` | Address $00-$FF |
| **Zero Page,X** | nn,X | `LDA $20,X` | ZP + X |
| **Zero Page,Y** | nn,Y | `LDX $20,Y` | ZP + Y |
| **Absolute** | nnnn | `LDA $1234` | Full 16-bit address |
| **Absolute,X** | nnnn,X | `LDA $1234,X` | Address + X |
| **Absolute,Y** | nnnn,Y | `LDA $1234,Y` | Address + Y |
| Indirect | (nnnn) | `JMP ($1234)` | Address at address |
| **Indirect,X** | (nn,X) | `LDA ($20,X)` | Indexed indirect |
| **Indirect,Y** | (nn),Y | `LDA ($20),Y` | Indirect indexed |
| Relative | offset | `BNE $10` | For branches |

**Bold** indicates addressing modes with shared, reusable implementations in `AddressingModes.cs`.

### Compositional Architecture

The emulator uses a **compositional pattern** where:
- **Addressing modes** are delegates that compute and return an address
- **Instructions** are higher-order functions that accept an addressing mode delegate
- This eliminates code duplication - one instruction implementation works with all addressing modes

**Example:**
```csharp
// One LDA implementation works with all addressing modes
handlers[0xA9] = Instructions.LDA(AddressingModes.Immediate);
handlers[0xA5] = Instructions.LDA(AddressingModes.ZeroPage);
handlers[0xB5] = Instructions.LDA(AddressingModes.ZeroPageX);
handlers[0xBD] = Instructions.LDA(AddressingModes.AbsoluteX);
```

**Shared Addressing Modes (12 total):**
- Implied, Immediate
- ZeroPage, ZeroPageX, ZeroPageY
- Absolute, AbsoluteX, AbsoluteY
- IndirectX, IndirectY
- AbsoluteXWrite, AbsoluteYWrite, IndirectYWrite (for store instructions)

---

## Multi-CPU Framework

The advanced emulator framework (`BadMango.Emulator.*`) provides a unified architecture supporting multiple CPU variants.

### Supported CPU Variants

| CPU | Status | Description |
|-----|--------|-------------|
| **65C02** | ✅ Implemented | WDC 65C02 with all addressing modes |
| **65816** | 🔨 Stub | Apple IIgs 16-bit CPU (placeholder) |
| **65832** | 🔨 Stub | Hypothetical 32-bit extension (placeholder) |

### Architecture

The multi-CPU framework uses several design patterns:

**Compositional Instructions:**
```csharp
// Instructions are higher-order functions that accept addressing mode delegates
public static OpcodeHandler LDA(AddressingModeHandler addressingMode)
{
    return (ref Registers regs, IMemory memory) =>
    {
        ushort address = addressingMode(ref regs, memory);
        regs.A = memory.Read(address);
        UpdateNZ(ref regs, regs.A);
    };
}
```

**Unified Register Set:**
```csharp
public struct Registers
{
    // 8-bit registers (all modes)
    public byte A { get; set; }   // Accumulator
    public byte X { get; set; }   // X Index
    public byte Y { get; set; }   // Y Index
    public byte SP { get; set; }  // Stack Pointer (low byte)

    // 16-bit registers (65816/65832)
    public ushort C { get; set; } // Full accumulator
    public ushort PC { get; set; } // Program Counter

    // Status flags
    public ProcessorStatusFlags P { get; set; }
}
```

**Opcode Table:**
```csharp
// Each CPU variant builds its opcode table at initialization
var builder = new Cpu65C02OpcodeTableBuilder();
OpcodeTable table = builder.Build();
```

### Project Organization

```
BadMango.Emulator.Core/
├── Cpu/
│   ├── Registers.cs              # Universal register structure
│   ├── ProcessorStatusFlags.cs   # Status flag enum and helpers
│   ├── OpcodeTable.cs            # Opcode dispatch table
│   └── OpcodeHandler.cs          # Handler delegate type

BadMango.Emulator.Emulation/
├── Cpu/
│   ├── CpuBase.cs                # Common CPU infrastructure
│   ├── Cpu65C02.cs               # 65C02 implementation
│   ├── Cpu65816.cs               # 65816 implementation (planned)
│   ├── Cpu65832.cs               # 65832 implementation (planned)
│   ├── AddressingModes.cs        # Shared addressing modes
│   └── Instructions.*.cs         # Instructions by category
```

### Instruction Categories

Instructions are organized into files by category:

| File | Instructions |
|------|--------------|
| `Instructions.Arithmetic.cs` | ADC, SBC, INC, DEC, INX, INY, DEX, DEY |
| `Instructions.Branch.cs` | BCC, BCS, BEQ, BNE, BMI, BPL, BVC, BVS |
| `Instructions.Compare.cs` | CMP, CPX, CPY, BIT |
| `Instructions.Flags.cs` | CLC, SEC, CLI, SEI, CLV, CLD, SED |
| `Instructions.Jump.cs` | JMP, JSR, RTS, RTI, BRK |
| `Instructions.Logical.cs` | AND, ORA, EOR |
| `Instructions.Shift.cs` | ASL, LSR, ROL, ROR |
| `Instructions.Stack.cs` | PHA, PLA, PHP, PLP |
| `Instructions.Transfer.cs` | TAX, TAY, TXA, TYA, TSX, TXS |

---

## Memory System

### Apple II Memory Map

See [Memory Map](Memory-Map) for detailed layout.

**Key Regions:**

| Address Range | Size | Purpose |
|---------------|------|---------|
| $0000-$00FF | 256 bytes | Zero Page (fast access) |
| $0100-$01FF | 256 bytes | Stack |
| $0200-$03FF | 512 bytes | Input buffer, system data |
| $0400-$07FF | 1KB | Text Page 1 / Lo-Res Page 1 |
| $0800-$0BFF | 1KB | Text Page 2 / Lo-Res Page 2 |
| $0800-$95FF | ~37KB | BASIC Program & Variables |
| $2000-$3FFF | 8KB | Hi-Res Page 1 |
| $4000-$5FFF | 8KB | Hi-Res Page 2 |
| $C000-$CFFF | 4KB | I/O Space (Soft Switches) |
| $D000-$FFFF | 12KB | ROM (Applesoft, Monitor) |

### Memory-Mapped I/O

The Apple II used memory-mapped I/O. Reading or writing certain addresses controlled hardware:

**Examples:**

| Address | Name | Function |
|---------|------|----------|
| $C000 | KBD | Keyboard data (read) |
| $C010 | KBDSTRB | Clear keyboard strobe |
| $C030 | SPKR | Toggle speaker |
| $C050 | TXTCLR | Set graphics mode |
| $C051 | TXTSET | Set text mode |
| $C052 | MIXCLR | Full screen mode |
| $C053 | MIXSET | Mixed text/graphics |
| $C054 | LOWSCR | Page 1 display |
| $C055 | HISCR | Page 2 display |
| $C056 | LORES | Lo-res graphics |
| $C057 | HIRES | Hi-res graphics |

---

## Hardware Emulation

### AppleSystem Class

**Location**: `src/BadMango.Basic/Emulation/AppleSystem.cs`

**Purpose**: Coordinates CPU and memory emulation.

**Components**:
- `Cpu6502` - Processor emulation
- `AppleMemory` - Memory space
- `AppleSpeaker` - Speaker emulation

**Initialization**:
```csharp
public AppleSystem()
{
    Memory = new AppleMemory();
    Speaker = new AppleSpeaker(Memory);
    Cpu = new Cpu6502(Memory);
}
```

### Cpu6502 Class

**Location**: `src/BadMango.Basic/Emulation/Cpu6502.cs`

**Key Methods**:
- `Reset()` - Initialize CPU state
- `Execute(ushort address)` - Run code at address
- `Step()` - Execute one instruction
- `SetMemory(ushort address, byte value)` - Write to memory
- `GetMemory(ushort address)` - Read from memory

### AppleMemory Class

**Location**: `src/BadMango.Basic/Emulation/AppleMemory.cs`

**Features**:
- 64KB byte array
- Bounds checking
- Memory-mapped I/O hooks
- Read/write tracking

**Key Methods**:
```csharp
public byte Read(ushort address)
public void Write(ushort address, byte value)
public void WriteRange(ushort address, byte[] data)
```

### AppleSpeaker Class

**Location**: `src/BadMango.Basic/Emulation/AppleSpeaker.cs`

**Purpose**: Emulates the Apple II speaker.

**Behavior**:
- Monitors address $C030
- Toggles on read/write
- Generates system beep (platform-dependent)

### MBF Struct (Microsoft Binary Format)

**Location**: `src/BadMango.Basic/Emulation/MBF.cs`

**Purpose**: Represents the 5-byte floating-point format used by Applesoft BASIC.

**Format** (5 bytes total):
- **Byte 0**: Exponent (biased by 128)
- **Bytes 1-4**: Mantissa (normalized with implicit leading 1)
- **Sign**: Stored in MSB of byte 1

**Key Features**:
- Implicit conversion to/from `double` and `float`
- Explicit methods: `FromDouble()`, `ToDouble()`, `FromBytes()`, `ToBytes()`
- Proper handling of special values (overflow, underflow, zero)
- Infinity and NaN throw `OverflowException` (not supported by MBF)

**Example Usage**:
```csharp
// Create MBF from double (implicit conversion)
MBF value = 3.14159;

// Convert back to double (implicit conversion)
double result = value;

// Explicit methods
MBF pi = MBF.FromDouble(Math.PI);
byte[] bytes = pi.ToBytes();
```

### FacConverter Class

**Location**: `src/BadMango.Basic/Emulation/FacConverter.cs`

**Purpose**: Provides conversion between .NET floating-point types and FAC (Floating-point ACcumulator) memory format.

**Methods**:
- **Legacy (IEEE 754)**: `DoubleToFacBytes()`, `FacBytesToDouble()`, `WriteToMemory()`, `ReadFromMemory()`
- **MBF (Authentic)**: `DoubleToMbf()`, `MbfToDouble()`, `WriteMbfToMemory()`, `ReadMbfFromMemory()`

**Example Usage**:
```csharp
// Using MBF methods for authentic Apple II format
MBF pi = FacConverter.DoubleToMbf(3.14159);
FacConverter.WriteMbfToMemory(memory, FAC1, pi);

// Read back
MBF result = FacConverter.ReadMbfFromMemory(memory, FAC1);
double value = FacConverter.MbfToDouble(result);
```

### BasicInteger Struct

**Location**: `src/BadMango.Basic/Emulation/BasicInteger.cs`

**Purpose**: Represents a 16-bit signed integer as stored in Applesoft BASIC (variables with % suffix).

**Format** (2 bytes):
- 16-bit signed two's complement
- Little-endian byte order in memory
- Range: -32768 to 32767

**Key Features**:
- Implicit conversion to/from `int` and `short`
- Explicit methods: `FromInt()`, `FromDouble()`, `FromBytes()`, `ToBytes()`
- `ToMbf()` for conversion to floating-point format
- Overflow detection with `OverflowException`

**Example Usage**:
```csharp
// Create BasicInteger
BasicInteger value = 42;

// Get raw bytes (little-endian)
byte[] bytes = value.ToBytes();

// Create from bytes
BasicInteger restored = BasicInteger.FromBytes(bytes);

// Convert to MBF for FAC operations
MBF mbf = value.ToMbf();
```

### BasicString Struct

**Location**: `src/BadMango.Basic/Emulation/BasicString.cs`

**Purpose**: Represents a string value as stored on the Apple II using 7-bit ASCII.

**Format**:
- 7-bit ASCII encoding (characters 0-127)
- Maximum length: 255 characters
- Characters outside 7-bit range are masked

**Key Features**:
- Implicit conversion to/from .NET `string`
- Explicit methods: `FromString()`, `FromBytes()`, `FromRawBytes()`, `ToBytes()`
- String operations: `Substring()`, `Concat()`
- Helper methods: `CharToAppleAscii()`, `AppleAsciiToChar()`
- **Span/Memory support**: `AsSpan()`, `AsMemory()`, `FromSpan()`, `FromRawSpan()`
- **Range indexer**: `str[0..5]`, `str[^5..]`
- Copy operations: `CopyTo()`, `TryCopyTo()`

**Example Usage**:
```csharp
// Create BasicString
BasicString value = "HELLO WORLD";

// Get raw 7-bit ASCII bytes
byte[] bytes = value.ToBytes();

// Create from bytes (from emulated memory)
BasicString restored = BasicString.FromBytes(bytes);

// String operations
BasicString sub = value.Substring(0, 5); // "HELLO"
BasicString concat = sub.Concat(" APPLE II");

// Range-based access (C# 8+)
BasicString world = value[6..];     // "WORLD"
BasicString hello = value[0..5];    // "HELLO"
BasicString last5 = value[^5..];    // "WORLD"

// Span-based access (allocation-free)
ReadOnlySpan<byte> span = value.AsSpan();
ReadOnlySpan<byte> slice = value.AsSpan(6..);
ReadOnlyMemory<byte> memory = value.AsMemory();

// Create from spans
byte[] data = new byte[] { 0x48, 0x45, 0x4C, 0x4C, 0x4F };
BasicString fromSpan = BasicString.FromSpan(data.AsSpan());

// Copy to destination
byte[] dest = new byte[20];
value.CopyTo(dest.AsSpan());
// or safely:
if (value.TryCopyTo(dest.AsSpan()))
{
    // copy succeeded
}
```

---

## Usage in BASIC

### PEEK - Read Memory

```basic
10 REM READ A BYTE
20 X = PEEK(768)
30 PRINT X
```

**Implementation**:
```csharp
public int Peek(int address)
{
    return _appleSystem.Memory.Read((ushort)address);
}
```

### POKE - Write Memory

```basic
10 REM WRITE A BYTE
20 POKE 768, 42
```

**Implementation**:
```csharp
public void Poke(int address, int value)
{
    _appleSystem.Memory.Write((ushort)address, (byte)value);
}
```

### CALL - Execute Machine Code

```basic
10 REM LOAD AND CALL MACHINE CODE
20 FOR I = 0 TO 5
30 READ B
40 POKE 768 + I, B
50 NEXT I
60 CALL 768
100 DATA 169, 0, 141, 48, 192, 96
```

**Machine Code** (disassembly):
```
$0300: A9 00     LDA #$00      ; Load 0 into A
$0302: 8D 30 C0  STA $C030     ; Toggle speaker
$0305: 60        RTS           ; Return
```

**Implementation**:
```csharp
public void Call(int address)
{
    _appleSystem.Cpu.Execute((ushort)address);
}
```

---

## Emulation Accuracy

### What's Emulated

✅ **Fully Emulated:**
- All 6502 instructions
- Processor flags
- Memory addressing modes
- Stack operations
- 64KB memory space
- Memory-mapped I/O addresses

✅ **Partially Emulated:**
- Speaker (uses system beep)
- Soft switches (tracked but no visual output)

❌ **Not Emulated:**
- Graphics display (stubbed)
- Disk I/O
- Joystick/paddle hardware
- Serial ports
- Timing (runs at maximum speed)

### Differences from Real Hardware

1. **Speed**: No cycle-accurate timing
2. **Graphics**: No visual output for graphics commands
3. **Sound**: Simple beep instead of actual waveform
4. **Peripherals**: Most I/O devices not emulated

### Use Cases

**Good For:**
- Running BASIC programs
- Learning about the 6502
- PEEK/POKE experiments
- Simple machine code
- Educational purposes

**Not Suitable For:**
- Cycle-accurate emulation
- Graphics-heavy programs
- Disk-based programs
- Programs requiring precise timing

---

## Development

### Testing the Emulator

**Unit Tests**: `tests/BadMango.Basic.Tests/EmulationTests.cs`

**Example Test**:
```csharp
[Test]
public void Cpu_ExecutesLDA_LoadsAccumulator()
{
    var cpu = new Cpu6502(memory);
    memory.Write(0x0300, 0xA9);  // LDA #$42
    memory.Write(0x0301, 0x42);
    
    cpu.Execute(0x0300);
    
    Assert.That(cpu.A, Is.EqualTo(0x42));
}
```

### Extending the Emulator

To add new features:

1. Update `Cpu6502` for new instructions
2. Update `AppleMemory` for new memory regions
3. Add hardware device classes (like `AppleSpeaker`)
4. Register in `AppleSystem`
5. Add tests

---

## Related Topics

- **[Memory Map](Memory-Map)** - Detailed memory layout
- **[Architecture Overview](Architecture-Overview)** - System design
- **[Language Reference](Language-Reference)** - PEEK/POKE/CALL commands

## External Resources

- [6502.org](http://www.6502.org/) - 6502 reference
- [6502 Instruction Reference](http://www.6502.org/tutorials/6502opcodes.html)
- [Apple II Documentation](https://www.apple2.org/)
- [Programming the 65816](http://www.6502.org/tutorials/65c816opcodes.html)
