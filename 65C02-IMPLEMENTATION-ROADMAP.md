# 65C02 Implementation Roadmap

This document lists all remaining instructions that need to be implemented for complete 65C02 CPU emulation.

## Implementation Status Summary

**Implemented: 13 instructions**
- LDA, LDX, LDY, STA (load/store)
- BRK, NOP (control)
- CLC, SEC, CLI, SEI, CLD, SED, CLV (flags)

**Remaining: ~43 instructions** (grouped by category below)

---

## Load/Store Operations

### ✅ Implemented
- **LDA** - Load Accumulator (8 addressing modes)
- **LDX** - Load X Register (1 addressing mode - needs more)
- **LDY** - Load Y Register (1 addressing mode - needs more)
- **STA** - Store Accumulator (7 addressing modes)

### ⏳ Not Yet Implemented

#### LDX - Load X Register (Additional Modes)
| Opcode | Addressing Mode | Cycles | Status |
|--------|----------------|--------|--------|
| 0xA6 | Zero Page | 3 | ⏳ |
| 0xB6 | Zero Page,Y | 4 | ⏳ |
| 0xAE | Absolute | 4 | ⏳ |
| 0xBE | Absolute,Y | 4+ | ⏳ |

#### LDY - Load Y Register (Additional Modes)
| Opcode | Addressing Mode | Cycles | Status |
|--------|----------------|--------|--------|
| 0xA4 | Zero Page | 3 | ⏳ |
| 0xB4 | Zero Page,X | 4 | ⏳ |
| 0xAC | Absolute | 4 | ⏳ |
| 0xBC | Absolute,X | 4+ | ⏳ |

#### STX - Store X Register
| Opcode | Addressing Mode | Cycles | Status |
|--------|----------------|--------|--------|
| 0x86 | Zero Page | 3 | ⏳ |
| 0x96 | Zero Page,Y | 4 | ⏳ |
| 0x8E | Absolute | 4 | ⏳ |

#### STY - Store Y Register
| Opcode | Addressing Mode | Cycles | Status |
|--------|----------------|--------|--------|
| 0x84 | Zero Page | 3 | ⏳ |
| 0x94 | Zero Page,X | 4 | ⏳ |
| 0x8C | Absolute | 4 | ⏳ |

**Implementation Notes:**
- Use compositional pattern: `Instructions.LDX(AddressingModes.ZeroPage)`
- Reuse existing addressing mode implementations
- Update Z and N flags appropriately

---

## Register Transfer Operations

### ⏳ Not Yet Implemented

#### Transfer Between Registers
| Opcode | Instruction | Description | Cycles | Flags | Status |
|--------|-------------|-------------|--------|-------|--------|
| 0xAA | TAX | Transfer A to X | 2 | N, Z | ⏳ |
| 0xA8 | TAY | Transfer A to Y | 2 | N, Z | ⏳ |
| 0x8A | TXA | Transfer X to A | 2 | N, Z | ⏳ |
| 0x98 | TYA | Transfer Y to A | 2 | N, Z | ⏳ |
| 0x9A | TXS | Transfer X to SP | 2 | - | ⏳ |
| 0xBA | TSX | Transfer SP to X | 2 | N, Z | ⏳ |

**Implementation Notes:**
- All use Implied addressing mode
- Most set N and Z flags (except TXS)
- Simple register-to-register copies
- Example: `Instructions.TAX(AddressingModes.Implied)`

---

## Arithmetic Operations

### ⏳ Not Yet Implemented

#### ADC - Add with Carry
| Opcode | Addressing Mode | Cycles | Status |
|--------|----------------|--------|--------|
| 0x69 | Immediate | 2 | ⏳ |
| 0x65 | Zero Page | 3 | ⏳ |
| 0x75 | Zero Page,X | 4 | ⏳ |
| 0x6D | Absolute | 4 | ⏳ |
| 0x7D | Absolute,X | 4+ | ⏳ |
| 0x79 | Absolute,Y | 4+ | ⏳ |
| 0x61 | Indirect,X | 6 | ⏳ |
| 0x71 | Indirect,Y | 5+ | ⏳ |

**Flags Affected:** N, V, Z, C

#### SBC - Subtract with Carry
| Opcode | Addressing Mode | Cycles | Status |
|--------|----------------|--------|--------|
| 0xE9 | Immediate | 2 | ⏳ |
| 0xE5 | Zero Page | 3 | ⏳ |
| 0xF5 | Zero Page,X | 4 | ⏳ |
| 0xED | Absolute | 4 | ⏳ |
| 0xFD | Absolute,X | 4+ | ⏳ |
| 0xF9 | Absolute,Y | 4+ | ⏳ |
| 0xE1 | Indirect,X | 6 | ⏳ |
| 0xF1 | Indirect,Y | 5+ | ⏳ |

**Flags Affected:** N, V, Z, C

#### INC - Increment Memory
| Opcode | Addressing Mode | Cycles | Status |
|--------|----------------|--------|--------|
| 0xE6 | Zero Page | 5 | ⏳ |
| 0xF6 | Zero Page,X | 6 | ⏳ |
| 0xEE | Absolute | 6 | ⏳ |
| 0xFE | Absolute,X | 7 | ⏳ |

**Flags Affected:** N, Z

#### DEC - Decrement Memory
| Opcode | Addressing Mode | Cycles | Status |
|--------|----------------|--------|--------|
| 0xC6 | Zero Page | 5 | ⏳ |
| 0xD6 | Zero Page,X | 6 | ⏳ |
| 0xCE | Absolute | 6 | ⏳ |
| 0xDE | Absolute,X | 7 | ⏳ |

**Flags Affected:** N, Z

#### Register Increment/Decrement
| Opcode | Instruction | Description | Cycles | Flags | Status |
|--------|-------------|-------------|--------|-------|--------|
| 0xE8 | INX | Increment X | 2 | N, Z | ⏳ |
| 0xC8 | INY | Increment Y | 2 | N, Z | ⏳ |
| 0xCA | DEX | Decrement X | 2 | N, Z | ⏳ |
| 0x88 | DEY | Decrement Y | 2 | N, Z | ⏳ |

**Implementation Notes:**
- ADC and SBC require overflow detection and decimal mode support
- INC/DEC operate on memory, need read-modify-write pattern
- INX/INY/DEX/DEY use Implied addressing, simple register operations
- All update N and Z flags appropriately

---

## Logical Operations

### ⏳ Not Yet Implemented

#### AND - Logical AND
| Opcode | Addressing Mode | Cycles | Status |
|--------|----------------|--------|--------|
| 0x29 | Immediate | 2 | ⏳ |
| 0x25 | Zero Page | 3 | ⏳ |
| 0x35 | Zero Page,X | 4 | ⏳ |
| 0x2D | Absolute | 4 | ⏳ |
| 0x3D | Absolute,X | 4+ | ⏳ |
| 0x39 | Absolute,Y | 4+ | ⏳ |
| 0x21 | Indirect,X | 6 | ⏳ |
| 0x31 | Indirect,Y | 5+ | ⏳ |

**Flags Affected:** N, Z

#### ORA - Logical OR
| Opcode | Addressing Mode | Cycles | Status |
|--------|----------------|--------|--------|
| 0x09 | Immediate | 2 | ⏳ |
| 0x05 | Zero Page | 3 | ⏳ |
| 0x15 | Zero Page,X | 4 | ⏳ |
| 0x0D | Absolute | 4 | ⏳ |
| 0x1D | Absolute,X | 4+ | ⏳ |
| 0x19 | Absolute,Y | 4+ | ⏳ |
| 0x01 | Indirect,X | 6 | ⏳ |
| 0x11 | Indirect,Y | 5+ | ⏳ |

**Flags Affected:** N, Z

#### EOR - Exclusive OR
| Opcode | Addressing Mode | Cycles | Status |
|--------|----------------|--------|--------|
| 0x49 | Immediate | 2 | ⏳ |
| 0x45 | Zero Page | 3 | ⏳ |
| 0x55 | Zero Page,X | 4 | ⏳ |
| 0x4D | Absolute | 4 | ⏳ |
| 0x5D | Absolute,X | 4+ | ⏳ |
| 0x59 | Absolute,Y | 4+ | ⏳ |
| 0x41 | Indirect,X | 6 | ⏳ |
| 0x51 | Indirect,Y | 5+ | ⏳ |

**Flags Affected:** N, Z

#### BIT - Bit Test
| Opcode | Addressing Mode | Cycles | Status |
|--------|----------------|--------|--------|
| 0x24 | Zero Page | 3 | ⏳ |
| 0x2C | Absolute | 4 | ⏳ |

**Flags Affected:** N (bit 7 of memory), V (bit 6 of memory), Z (result of AND)

**Implementation Notes:**
- AND, ORA, EOR follow same pattern as LDA
- BIT is special: sets N/V from memory bits, Z from A AND memory
- All use existing addressing modes

---

## Shift and Rotate Operations

### ⏳ Not Yet Implemented

#### ASL - Arithmetic Shift Left
| Opcode | Addressing Mode | Cycles | Status |
|--------|----------------|--------|--------|
| 0x0A | Accumulator | 2 | ⏳ |
| 0x06 | Zero Page | 5 | ⏳ |
| 0x16 | Zero Page,X | 6 | ⏳ |
| 0x0E | Absolute | 6 | ⏳ |
| 0x1E | Absolute,X | 7 | ⏳ |

**Flags Affected:** N, Z, C

#### LSR - Logical Shift Right
| Opcode | Addressing Mode | Cycles | Status |
|--------|----------------|--------|--------|
| 0x4A | Accumulator | 2 | ⏳ |
| 0x46 | Zero Page | 5 | ⏳ |
| 0x56 | Zero Page,X | 6 | ⏳ |
| 0x4E | Absolute | 6 | ⏳ |
| 0x5E | Absolute,X | 7 | ⏳ |

**Flags Affected:** N, Z, C

#### ROL - Rotate Left
| Opcode | Addressing Mode | Cycles | Status |
|--------|----------------|--------|--------|
| 0x2A | Accumulator | 2 | ⏳ |
| 0x26 | Zero Page | 5 | ⏳ |
| 0x36 | Zero Page,X | 6 | ⏳ |
| 0x2E | Absolute | 6 | ⏳ |
| 0x3E | Absolute,X | 7 | ⏳ |

**Flags Affected:** N, Z, C

#### ROR - Rotate Right
| Opcode | Addressing Mode | Cycles | Status |
|--------|----------------|--------|--------|
| 0x6A | Accumulator | 2 | ⏳ |
| 0x66 | Zero Page | 5 | ⏳ |
| 0x76 | Zero Page,X | 6 | ⏳ |
| 0x6E | Absolute | 6 | ⏳ |
| 0x7E | Absolute,X | 7 | ⏳ |

**Flags Affected:** N, Z, C

**Implementation Notes:**
- Need new Accumulator addressing mode (returns address 0, operates on A register directly)
- Memory operations use read-modify-write pattern
- ASL/LSR: shift bits, bit 7/0 goes to carry
- ROL/ROR: rotate through carry flag
- All update N, Z, C flags

---

## Comparison Operations

### ⏳ Not Yet Implemented

#### CMP - Compare Accumulator
| Opcode | Addressing Mode | Cycles | Status |
|--------|----------------|--------|--------|
| 0xC9 | Immediate | 2 | ⏳ |
| 0xC5 | Zero Page | 3 | ⏳ |
| 0xD5 | Zero Page,X | 4 | ⏳ |
| 0xCD | Absolute | 4 | ⏳ |
| 0xDD | Absolute,X | 4+ | ⏳ |
| 0xD9 | Absolute,Y | 4+ | ⏳ |
| 0xC1 | Indirect,X | 6 | ⏳ |
| 0xD1 | Indirect,Y | 5+ | ⏳ |

**Flags Affected:** N, Z, C

#### CPX - Compare X Register
| Opcode | Addressing Mode | Cycles | Status |
|--------|----------------|--------|--------|
| 0xE0 | Immediate | 2 | ⏳ |
| 0xE4 | Zero Page | 3 | ⏳ |
| 0xEC | Absolute | 4 | ⏳ |

**Flags Affected:** N, Z, C

#### CPY - Compare Y Register
| Opcode | Addressing Mode | Cycles | Status |
|--------|----------------|--------|--------|
| 0xC0 | Immediate | 2 | ⏳ |
| 0xC4 | Zero Page | 3 | ⏳ |
| 0xCC | Absolute | 4 | ⏳ |

**Flags Affected:** N, Z, C

**Implementation Notes:**
- Compare instructions perform subtraction but don't store result
- Set flags based on comparison (C set if register >= memory)
- Follow compositional pattern
- CMP has full addressing mode support like LDA

---

## Branch Operations

### ⏳ Not Yet Implemented

All branch instructions use **Relative addressing mode** (needs to be implemented).

| Opcode | Instruction | Condition | Description | Cycles | Status |
|--------|-------------|-----------|-------------|--------|--------|
| 0x90 | BCC | C = 0 | Branch if Carry Clear | 2+ | ⏳ |
| 0xB0 | BCS | C = 1 | Branch if Carry Set | 2+ | ⏳ |
| 0xF0 | BEQ | Z = 1 | Branch if Equal (Zero) | 2+ | ⏳ |
| 0xD0 | BNE | Z = 0 | Branch if Not Equal | 2+ | ⏳ |
| 0x30 | BMI | N = 1 | Branch if Minus | 2+ | ⏳ |
| 0x10 | BPL | N = 0 | Branch if Plus | 2+ | ⏳ |
| 0x50 | BVC | V = 0 | Branch if Overflow Clear | 2+ | ⏳ |
| 0x70 | BVS | V = 1 | Branch if Overflow Set | 2+ | ⏳ |

**Cycles:** 2 if no branch, 3 if branch taken (same page), 4 if branch to different page

**Implementation Notes:**
- Need new **Relative** addressing mode for signed byte offset
- Offset is signed (-128 to +127)
- Add 1 cycle if branch taken, 2 if page boundary crossed
- Pattern: `Instructions.BCC(AddressingModes.Relative)`

---

## Jump and Subroutine Operations

### ⏳ Not Yet Implemented

#### JMP - Jump
| Opcode | Addressing Mode | Cycles | Status |
|--------|----------------|--------|--------|
| 0x4C | Absolute | 3 | ⏳ |
| 0x6C | Indirect | 5 | ⏳ |

**Implementation Notes:**
- Need **Indirect** addressing mode (reads 16-bit address from memory)
- 6502 bug: if low byte of indirect address is 0xFF, wraps within page
- Simply sets PC to target address

#### JSR - Jump to Subroutine
| Opcode | Addressing Mode | Cycles | Status |
|--------|----------------|--------|--------|
| 0x20 | Absolute | 6 | ⏳ |

**Implementation Notes:**
- Push return address (PC - 1) to stack (high byte, then low byte)
- Set PC to target address
- Stack pointer decrements by 2

#### RTS - Return from Subroutine
| Opcode | Addressing Mode | Cycles | Status |
|--------|----------------|--------|--------|
| 0x60 | Implied | 6 | ⏳ |

**Implementation Notes:**
- Pull return address from stack (low byte, then high byte)
- Increment pulled address and set PC
- Stack pointer increments by 2

#### RTI - Return from Interrupt
| Opcode | Addressing Mode | Cycles | Status |
|--------|----------------|--------|--------|
| 0x40 | Implied | 6 | ⏳ |

**Implementation Notes:**
- Pull P from stack
- Pull PC from stack (low byte, then high byte)
- Stack pointer increments by 3
- Restores processor state after interrupt

---

## Stack Operations

### ⏳ Not Yet Implemented

| Opcode | Instruction | Description | Cycles | Status |
|--------|-------------|-------------|--------|--------|
| 0x48 | PHA | Push Accumulator | 3 | ⏳ |
| 0x08 | PHP | Push Processor Status | 3 | ⏳ |
| 0x68 | PLA | Pull Accumulator | 4 | ⏳ |
| 0x28 | PLP | Pull Processor Status | 4 | ⏳ |

**Implementation Notes:**
- All use Implied addressing
- Stack lives at $0100-$01FF (page 1)
- SP points to next free location
- Push: write to $0100 + SP, then decrement SP
- Pull: increment SP, then read from $0100 + SP
- PLA sets N and Z flags
- PHP sets bit 4 (B flag) when pushing

---

## Implementation Priority

### Phase 1: Core Operations (High Priority)
1. **Register Transfers** (TAX, TAY, TXA, TYA, TXS, TSX) - Simple, needed for many programs
2. **Stack Operations** (PHA, PHP, PLA, PLP) - Required for subroutines
3. **Jump/Subroutine** (JMP, JSR, RTS, RTI) - Essential for control flow
4. **Comparison** (CMP, CPX, CPY) - Needed for conditional logic
5. **Branch** (BCC, BCS, BEQ, BNE, BMI, BPL, BVC, BVS) - Needed with comparisons

### Phase 2: Arithmetic (Medium Priority)
1. **Increment/Decrement Registers** (INX, INY, DEX, DEY) - Common in loops
2. **ADC/SBC** - Required for arithmetic operations
3. **INC/DEC** - Memory increment/decrement

### Phase 3: Data Manipulation (Medium Priority)
1. **Logical Operations** (AND, ORA, EOR, BIT) - Bit manipulation
2. **Shift/Rotate** (ASL, LSR, ROL, ROR) - Bit operations
3. **Additional LDX/LDY/STX/STY modes** - Complete load/store

---

## New Addressing Modes Needed

### ⏳ To Implement

1. **Accumulator** - For shift/rotate operations on A register
   - Returns dummy address, operates on accumulator directly
   - Used by: ASL, LSR, ROL, ROR

2. **Relative** - For branch instructions
   - Reads signed byte offset
   - Adds to PC (with page boundary detection)
   - Used by: All branch instructions (BCC, BCS, BEQ, etc.)

3. **Indirect** - For JMP indirect
   - Reads 16-bit address from memory location
   - Note: 65C02 fixed the page wrap bug from the original 6502
   - Used by: JMP (0x6C)

---

## Testing Strategy

For each instruction:

1. **Unit Tests** in `InstructionsTests.cs`
   - Test flag updates (N, Z, C, V as appropriate)
   - Test boundary conditions (zero, negative, overflow)
   - Test with different addressing modes

2. **Integration Tests** in `Cpu65C02Tests.cs`
   - Verify opcodes execute correctly through opcode table
   - Test instruction sequences (e.g., JSR/RTS pairs)
   - Verify cycle counts

3. **Real-World Programs**
   - Test with actual 6502 assembly code
   - Verify compatibility with known programs
   - Test edge cases and corner cases

---

## Benefits of Current Architecture

The compositional architecture we've built provides:

✅ **No Code Duplication** - One instruction implementation works with all addressing modes
✅ **Easy to Add Instructions** - Just implement the instruction logic once
✅ **Easy to Add Addressing Modes** - Immediately available to all compatible instructions
✅ **Type Safe** - Compile-time checking via generics and delegates
✅ **Testable** - Instructions and addressing modes can be tested independently
✅ **Maintainable** - Changes to instruction semantics in one place
✅ **Extensible** - Ready for 65816 and 65832 variants

**Example of how easy it is to add a new instruction:**

```csharp
// In Instructions.cs - implement once
public static OpcodeHandler<Cpu65C02, Cpu65C02State> AND(AddressingMode<Cpu65C02State> mode)
{
    return (cpu, memory, ref state) =>
    {
        Addr address = mode(memory, ref state);
        byte value = memory.Read(address);
        state.A &= value;
        SetZN(value, ref state.P);
    };
}

// In Cpu65C02OpcodeTableBuilder.cs - register all modes
handlers[0x29] = Instructions.AND(AddressingModes.Immediate);
handlers[0x25] = Instructions.AND(AddressingModes.ZeroPage);
handlers[0x35] = Instructions.AND(AddressingModes.ZeroPageX);
handlers[0x2D] = Instructions.AND(AddressingModes.Absolute);
handlers[0x3D] = Instructions.AND(AddressingModes.AbsoluteX);
handlers[0x39] = Instructions.AND(AddressingModes.AbsoluteY);
handlers[0x21] = Instructions.AND(AddressingModes.IndirectX);
handlers[0x31] = Instructions.AND(AddressingModes.IndirectY);
```

That's it! One instruction implementation, eight addressing modes, no duplication.

---

## Next Steps

1. Review this roadmap with the team
2. Prioritize phases based on project needs
3. Implement Phase 1 instructions first (core operations)
4. Add comprehensive tests for each instruction
5. Update documentation as instructions are completed
6. Verify with real 6502 programs

This clean architecture makes completing the 65C02 instruction set straightforward and maintainable!
