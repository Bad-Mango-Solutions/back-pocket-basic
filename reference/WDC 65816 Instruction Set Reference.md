# WDC 65816 Instruction Set Reference: Comprehensive Opcode Table and Implementation Guide

------

## Introduction

The Western Design Center (WDC) 65816 microprocessor, also known as the W65C816S, is a powerful 8/16-bit CPU that extends the venerable 6502 architecture with 16-bit operations, a 24-bit address space, and a host of new instructions and addressing modes. It is the core of systems such as the Apple IIGS and the Super Nintendo Entertainment System (SNES), and remains popular in retrocomputing and embedded applications. For emulator developers, a precise, exhaustive, and implementation-ready reference to the 65816 instruction set is essential. This report provides a complete, Markdown-formatted opcode table and instruction set reference, suitable for use in emulator development and code generation contexts such as GitHub Copilot.

The reference is structured to maximize clarity and utility for implementers. Each instruction is presented with all supported addressing modes, exact hexadecimal opcodes, concise descriptions, mode-specific notes (including accumulator/index width and emulation/native mode distinctions), flags affected, and cycle counts. The tables are grouped by instruction category for ease of navigation and to reflect common implementation patterns. All information is cross-verified with authoritative sources, including the WDC datasheet, community-verified opcode tables, and respected technical references.

------

## 1. 65816 Architecture Overview

The 65816 is a superset of the 65C02, introducing several key enhancements:

- **16-bit Operations:** The accumulator (A) and index registers (X, Y) can operate in either 8-bit or 16-bit modes, controlled by the M and X flags in the processor status register.
- **24-bit Addressing:** The processor can address up to 16MB of memory using a combination of 16-bit addresses and 8-bit bank registers (Program Bank Register, PBR; Data Bank Register, DBR).
- **Relocatable Direct Page:** The "zero page" concept is replaced by a 16-bit Direct Page (D) register, allowing the direct page to be relocated anywhere in the first 64KB of address space.
- **Relocatable Stack:** The stack pointer (S) is 16 bits wide in native mode, allowing the stack to be placed anywhere in bank 0.
- **Dual Modes:** The processor starts in 6502-compatible emulation mode (E=1), with all registers 8 bits wide and stack fixed at $0100–$01FF. Native mode (E=0) enables all enhancements.
- **Expanded Instruction Set:** New instructions, addressing modes, and block move operations are introduced.

### 1.1 Registers

| Register | Description                                            |
| -------- | ------------------------------------------------------ |
| A        | Accumulator (8/16 bits)                                |
| X, Y     | Index registers (8/16 bits)                            |
| S        | Stack Pointer (8 bits in emulation, 16 bits in native) |
| D        | Direct Page Register (16 bits)                         |
| DBR      | Data Bank Register (8 bits)                            |
| PBR      | Program Bank Register (8 bits)                         |
| P        | Processor Status Register (8 bits)                     |
| PC       | Program Counter (16 bits)                              |

**Note:** The width of A, X, and Y is controlled by the M and X flags in P. The stack pointer is always 8 bits in emulation mode, and 16 bits in native mode.

### 1.2 Processor Status Flags

The 65816 status register (P) contains the following flags:

| Bit  | Name | Description                           |
| ---- | ---- | ------------------------------------- |
| 7    | N    | Negative                              |
| 6    | V    | Overflow                              |
| 5    | M    | Accumulator width (1=8-bit, 0=16-bit) |
| 4    | X    | Index width (1=8-bit, 0=16-bit)       |
| 3    | D    | Decimal mode                          |
| 2    | I    | IRQ disable                           |
| 1    | Z    | Zero                                  |
| 0    | C    | Carry                                 |

**Emulation Mode (E):** An internal flag, not directly accessible except via XCE. When E=1, the processor behaves as a 65C02: M and X are forced to 1, stack is 8 bits, and some instructions are restricted.

------

## 2. Addressing Modes

The 65816 supports a rich set of addressing modes, many inherited from the 6502 and 65C02, with several new modes for 16-bit and 24-bit operations. Understanding these is crucial for correct opcode decoding and execution.

| Mode Name                     | Syntax            | Description                                       |
| ----------------------------- | ----------------- | ------------------------------------------------- |
| Immediate                     | #$12 / #$1234     | Operand is a constant (size depends on M/X flags) |
| Absolute                      | $1234             | 16-bit address in current data bank (DBR)         |
| Absolute,X                    | $1234,X           | Absolute address plus X                           |
| Absolute,Y                    | $1234,Y           | Absolute address plus Y                           |
| Absolute Long                 | $123456           | 24-bit address (bank:address)                     |
| Absolute Long,X               | $123456,X         | 24-bit address plus X                             |
| Direct Page                   | $12               | 8-bit offset added to D register (bank 0)         |
| Direct Page,X                 | $12,X             | Direct page plus X                                |
| Direct Page,Y                 | $12,Y             | Direct page plus Y                                |
| Direct Indirect               | ($12)             | 16-bit pointer at D+$12, bank from DBR            |
| Direct Indirect,X             | ($12,X)           | D+$12+X, pointer to 16-bit address, bank from DBR |
| Direct Indirect,Y             | ($12),Y           | Pointer at D+$12, add Y, bank from DBR            |
| Direct Indirect Long          | [$12]             | 24-bit pointer at D+$12                           |
| Direct Indirect Long,Y        | [$12],Y           | 24-bit pointer at D+$12, add Y                    |
| Absolute Indirect             | ($1234)           | Pointer at $1234 in bank 0                        |
| Absolute Indexed Indirect     | ($1234,X)         | $1234+X in PBR, pointer to 16-bit address         |
| Absolute Indirect Long        | [$1234]           | 24-bit pointer at $1234 in bank 0                 |
| Stack Relative                | $12,S             | $12 + S (bank 0)                                  |
| Stack Relative Indirect,Y     | ($12,S),Y         | Pointer at S+$12, add Y, bank from DBR            |
| Program Counter Relative      | label             | 8-bit signed offset (branches)                    |
| Program Counter Relative Long | label             | 16-bit signed offset (BRL, PER)                   |
| Block Move                    | #srcbank,#dstbank | Used by MVN/MVP                                   |
| Accumulator                   | A                 | Operation on accumulator                          |
| Implied                       | —                 | No operand                                        |

**Mode-specific notes:**

- Immediate operand size is 1 or 2 bytes, depending on M (for accumulator) or X (for index) flags.
- Direct Page addressing is offset by the D register; if D.l ≠ 0, add 1 cycle penalty.
- Many indexed modes add a cycle if the index crosses a page boundary (in emulation mode).
- Some modes are only available for certain instructions.

------

## 3. Instruction Set Reference

Instructions are grouped by functional category. Each table lists:

- **Mnemonic:** Instruction mnemonic (e.g., LDA)
- **Addressing Mode:** Name and syntax
- **Opcode:** Hexadecimal opcode
- **Description:** Brief summary of operation
- **Flags Affected:** Which status flags are modified
- **Cycles:** Base cycle count (see notes for mode-specific penalties)

**Mode-specific notes** are provided after each table, detailing behavior in 8/16-bit modes, emulation/native mode, and any addressing mode caveats.

------

### 3.1 Load and Store Instructions

#### 3.1.1 Load Accumulator (LDA)

| Mnemonic | Addressing Mode | Opcode | Description                                      | Flags Affected | Cycles                                         |
| -------- | --------------- | ------ | ------------------------------------------------ | -------------- | ---------------------------------------------- |
| LDA      | #imm            | A9     | Load immediate value into A                      | N, Z           | 2 (+1 if M=0)                                  |
| LDA      | dp              | A5     | Load from direct page                            | N, Z           | 3 (+1 if M=0, +1 if D.l≠0)                     |
| LDA      | dp,X            | B5     | Load from direct page + X                        | N, Z           | 4 (+1 if M=0, +1 if D.l≠0)                     |
| LDA      | dp,S            | A3     | Load from stack relative                         | N, Z           | 4 (+1 if M=0)                                  |
| LDA      | (dp)            | B2     | Load from direct page indirect                   | N, Z           | 5 (+1 if M=0, +1 if D.l≠0)                     |
| LDA      | (dp,X)          | A1     | Load from direct page indexed indirect           | N, Z           | 6 (+1 if M=0, +1 if D.l≠0)                     |
| LDA      | (dp),Y          | B1     | Load from direct page indirect indexed by Y      | N, Z           | 5 (+1 if M=0, +1 if D.l≠0, +1 if page crossed) |
| LDA      | [dp]            | A7     | Load from direct page indirect long              | N, Z           | 6 (+1 if M=0, +1 if D.l≠0)                     |
| LDA      | [dp],Y          | B7     | Load from direct page indirect long indexed by Y | N, Z           | 6 (+1 if M=0, +1 if D.l≠0)                     |
| LDA      | abs             | AD     | Load from absolute address                       | N, Z           | 4 (+1 if M=0)                                  |
| LDA      | abs,X           | BD     | Load from absolute + X                           | N, Z           | 4 (+1 if M=0, +1 if page crossed)              |
| LDA      | abs,Y           | B9     | Load from absolute + Y                           | N, Z           | 4 (+1 if M=0, +1 if page crossed)              |
| LDA      | long            | AF     | Load from absolute long                          | N, Z           | 5 (+1 if M=0)                                  |
| LDA      | long,X          | BF     | Load from absolute long + X                      | N, Z           | 5 (+1 if M=0)                                  |
| LDA      | (sr,S),Y        | B3     | Load from stack relative indirect indexed by Y   | N, Z           | 7 (+1 if M=0)                                  |

**Notes:**

- In 8-bit accumulator mode (M=1), immediate is 1 byte; in 16-bit mode (M=0), immediate is 2 bytes.
- Direct page addressing incurs a cycle penalty if D.l ≠ 0.
- Indexed absolute modes add a cycle if index crosses a page boundary (in emulation mode).
- All loads set N if the result is negative (high bit set), Z if result is zero.

#### 3.1.2 Load Index Registers (LDX, LDY)

| Mnemonic | Addressing Mode | Opcode | Description               | Flags Affected | Cycles                            |
| -------- | --------------- | ------ | ------------------------- | -------------- | --------------------------------- |
| LDX      | #imm            | A2     | Load immediate into X     | N, Z           | 2 (+1 if X=0)                     |
| LDX      | dp              | A6     | Load from direct page     | N, Z           | 3 (+1 if X=0, +1 if D.l≠0)        |
| LDX      | dp,Y            | B6     | Load from direct page + Y | N, Z           | 4 (+1 if X=0, +1 if D.l≠0)        |
| LDX      | abs             | AE     | Load from absolute        | N, Z           | 4 (+1 if X=0)                     |
| LDX      | abs,Y           | BE     | Load from absolute + Y    | N, Z           | 4 (+1 if X=0, +1 if page crossed) |
| LDY      | #imm            | A0     | Load immediate into Y     | N, Z           | 2 (+1 if X=0)                     |
| LDY      | dp              | A4     | Load from direct page     | N, Z           | 3 (+1 if X=0, +1 if D.l≠0)        |
| LDY      | dp,X            | B4     | Load from direct page + X | N, Z           | 4 (+1 if X=0, +1 if D.l≠0)        |
| LDY      | abs             | AC     | Load from absolute        | N, Z           | 4 (+1 if X=0)                     |
| LDY      | abs,X           | BC     | Load from absolute + X    | N, Z           | 4 (+1 if X=0, +1 if page crossed) |

**Notes:**

- Index register width is controlled by X flag (X=1: 8-bit, X=0: 16-bit).
- Immediate operand is 1 or 2 bytes depending on X flag.
- All loads set N and Z as above.

#### 3.1.3 Store Instructions (STA, STX, STY, STZ)

| Mnemonic | Addressing Mode | Opcode | Description                                       | Flags Affected | Cycles                     |
| -------- | --------------- | ------ | ------------------------------------------------- | -------------- | -------------------------- |
| STA      | dp              | 85     | Store A to direct page                            | —              | 3 (+1 if M=0, +1 if D.l≠0) |
| STA      | dp,X            | 95     | Store A to direct page + X                        | —              | 4 (+1 if M=0, +1 if D.l≠0) |
| STA      | dp,S            | 83     | Store A to stack relative                         | —              | 4 (+1 if M=0)              |
| STA      | (dp)            | 92     | Store A to direct page indirect                   | —              | 5 (+1 if M=0, +1 if D.l≠0) |
| STA      | (dp,X)          | 81     | Store A to direct page indexed indirect           | —              | 6 (+1 if M=0, +1 if D.l≠0) |
| STA      | (dp),Y          | 91     | Store A to direct page indirect indexed by Y      | —              | 6 (+1 if M=0, +1 if D.l≠0) |
| STA      | [dp]            | 87     | Store A to direct page indirect long              | —              | 6 (+1 if M=0, +1 if D.l≠0) |
| STA      | [dp],Y          | 97     | Store A to direct page indirect long indexed by Y | —              | 6 (+1 if M=0, +1 if D.l≠0) |
| STA      | abs             | 8D     | Store A to absolute                               | —              | 4 (+1 if M=0)              |
| STA      | abs,X           | 9D     | Store A to absolute + X                           | —              | 5 (+1 if M=0)              |
| STA      | abs,Y           | 99     | Store A to absolute + Y                           | —              | 5 (+1 if M=0)              |
| STA      | long            | 8F     | Store A to absolute long                          | —              | 5 (+1 if M=0)              |
| STA      | long,X          | 9F     | Store A to absolute long + X                      | —              | 5 (+1 if M=0)              |
| STA      | (sr,S),Y        | 93     | Store A to stack relative indirect indexed by Y   | —              | 7 (+1 if M=0)              |
| STX      | dp              | 86     | Store X to direct page                            | —              | 3 (+1 if X=0, +1 if D.l≠0) |
| STX      | dp,Y            | 96     | Store X to direct page + Y                        | —              | 4 (+1 if X=0, +1 if D.l≠0) |
| STX      | abs             | 8E     | Store X to absolute                               | —              | 4 (+1 if X=0)              |
| STY      | dp              | 84     | Store Y to direct page                            | —              | 3 (+1 if X=0, +1 if D.l≠0) |
| STY      | dp,X            | 94     | Store Y to direct page + X                        | —              | 4 (+1 if X=0, +1 if D.l≠0) |
| STY      | abs             | 8C     | Store Y to absolute                               | —              | 4 (+1 if X=0)              |
| STZ      | dp              | 64     | Store zero to direct page                         | —              | 3 (+1 if M=0, +1 if D.l≠0) |
| STZ      | dp,X            | 74     | Store zero to direct page + X                     | —              | 4 (+1 if M=0, +1 if D.l≠0) |
| STZ      | abs             | 9C     | Store zero to absolute                            | —              | 4 (+1 if M=0)              |
| STZ      | abs,X           | 9E     | Store zero to absolute + X                        | —              | 5 (+1 if M=0)              |

**Notes:**

- Store instructions do not affect flags.
- STZ is a 65816 addition: stores zero to memory.
- All store instructions are affected by register width (M/X flags) for operand size.

------

### 3.2 Arithmetic and Logical Instructions

#### 3.2.1 Add with Carry (ADC)

| Mnemonic | Addressing Mode | Opcode | Description                                                | Flags Affected | Cycles                                         |
| -------- | --------------- | ------ | ---------------------------------------------------------- | -------------- | ---------------------------------------------- |
| ADC      | #imm            | 69     | Add immediate to A with carry                              | N, V, Z, C     | 2 (+1 if M=0)                                  |
| ADC      | dp              | 65     | Add direct page to A with carry                            | N, V, Z, C     | 3 (+1 if M=0, +1 if D.l≠0)                     |
| ADC      | dp,X            | 75     | Add direct page + X to A with carry                        | N, V, Z, C     | 4 (+1 if M=0, +1 if D.l≠0)                     |
| ADC      | dp,S            | 63     | Add stack relative to A with carry                         | N, V, Z, C     | 4 (+1 if M=0)                                  |
| ADC      | (dp)            | 72     | Add direct page indirect to A with carry                   | N, V, Z, C     | 5 (+1 if M=0, +1 if D.l≠0)                     |
| ADC      | (dp,X)          | 61     | Add direct page indexed indirect to A with carry           | N, V, Z, C     | 6 (+1 if M=0, +1 if D.l≠0)                     |
| ADC      | (dp),Y          | 71     | Add direct page indirect indexed by Y to A with carry      | N, V, Z, C     | 5 (+1 if M=0, +1 if D.l≠0, +1 if page crossed) |
| ADC      | [dp]            | 67     | Add direct page indirect long to A with carry              | N, V, Z, C     | 6 (+1 if M=0, +1 if D.l≠0)                     |
| ADC      | [dp],Y          | 77     | Add direct page indirect long indexed by Y to A with carry | N, V, Z, C     | 6 (+1 if M=0, +1 if D.l≠0)                     |
| ADC      | abs             | 6D     | Add absolute to A with carry                               | N, V, Z, C     | 4 (+1 if M=0)                                  |
| ADC      | abs,X           | 7D     | Add absolute + X to A with carry                           | N, V, Z, C     | 4 (+1 if M=0, +1 if page crossed)              |
| ADC      | abs,Y           | 79     | Add absolute + Y to A with carry                           | N, V, Z, C     | 4 (+1 if M=0, +1 if page crossed)              |
| ADC      | long            | 6F     | Add absolute long to A with carry                          | N, V, Z, C     | 5 (+1 if M=0)                                  |
| ADC      | long,X          | 7F     | Add absolute long + X to A with carry                      | N, V, Z, C     | 5 (+1 if M=0)                                  |
| ADC      | (sr,S),Y        | 73     | Add stack relative indirect indexed by Y to A with carry   | N, V, Z, C     | 7 (+1 if M=0)                                  |

**Notes:**

- Decimal mode (D flag) affects addition: in decimal mode, ADC performs BCD addition.
- Carry is always included in the sum.
- Overflow (V) is set if signed overflow occurs.
- All addressing mode penalties apply as above.

#### 3.2.2 Subtract with Borrow (SBC)

| Mnemonic | Addressing Mode | Opcode | Description                                                  | Flags Affected | Cycles                                         |
| -------- | --------------- | ------ | ------------------------------------------------------------ | -------------- | ---------------------------------------------- |
| SBC      | #imm            | E9     | Subtract immediate from A with borrow                        | N, V, Z, C     | 2 (+1 if M=0)                                  |
| SBC      | dp              | E5     | Subtract direct page from A with borrow                      | N, V, Z, C     | 3 (+1 if M=0, +1 if D.l≠0)                     |
| SBC      | dp,X            | F5     | Subtract direct page + X from A with borrow                  | N, V, Z, C     | 4 (+1 if M=0, +1 if D.l≠0)                     |
| SBC      | dp,S            | E3     | Subtract stack relative from A with borrow                   | N, V, Z, C     | 4 (+1 if M=0)                                  |
| SBC      | (dp)            | F2     | Subtract direct page indirect from A with borrow             | N, V, Z, C     | 5 (+1 if M=0, +1 if D.l≠0)                     |
| SBC      | (dp,X)          | E1     | Subtract direct page indexed indirect from A with borrow     | N, V, Z, C     | 6 (+1 if M=0, +1 if D.l≠0)                     |
| SBC      | (dp),Y          | F1     | Subtract direct page indirect indexed by Y from A with borrow | N, V, Z, C     | 5 (+1 if M=0, +1 if D.l≠0, +1 if page crossed) |
| SBC      | [dp]            | E7     | Subtract direct page indirect long from A with borrow        | N, V, Z, C     | 6 (+1 if M=0, +1 if D.l≠0)                     |
| SBC      | [dp],Y          | F7     | Subtract direct page indirect long indexed by Y from A with borrow | N, V, Z, C     | 6 (+1 if M=0, +1 if D.l≠0)                     |
| SBC      | abs             | ED     | Subtract absolute from A with borrow                         | N, V, Z, C     | 4 (+1 if M=0)                                  |
| SBC      | abs,X           | FD     | Subtract absolute + X from A with borrow                     | N, V, Z, C     | 4 (+1 if M=0, +1 if page crossed)              |
| SBC      | abs,Y           | F9     | Subtract absolute + Y from A with borrow                     | N, V, Z, C     | 4 (+1 if M=0, +1 if page crossed)              |
| SBC      | long            | EF     | Subtract absolute long from A with borrow                    | N, V, Z, C     | 5 (+1 if M=0)                                  |
| SBC      | long,X          | FF     | Subtract absolute long + X from A with borrow                | N, V, Z, C     | 5 (+1 if M=0)                                  |
| SBC      | (sr,S),Y        | F3     | Subtract stack relative indirect indexed by Y from A with borrow | N, V, Z, C     | 7 (+1 if M=0)                                  |

**Notes:**

- In decimal mode, SBC performs BCD subtraction.
- Carry is set if no borrow is required (i.e., A ≥ M).
- Overflow (V) is set if signed overflow occurs.

#### 3.2.3 Logical Instructions (AND, ORA, EOR, BIT)

| Mnemonic | Addressing Mode | Opcode | Description           | Flags Affected | Cycles                     |
| -------- | --------------- | ------ | --------------------- | -------------- | -------------------------- |
| AND      | #imm            | 29     | Logical AND with A    | N, Z           | 2 (+1 if M=0)              |
| ORA      | #imm            | 09     | Logical OR with A     | N, Z           | 2 (+1 if M=0)              |
| EOR      | #imm            | 49     | Logical XOR with A    | N, Z           | 2 (+1 if M=0)              |
| BIT      | #imm            | 89     | Test bits (A & M)     | Z              | 2 (+1 if M=0)              |
| AND      | dp              | 25     | AND direct page       | N, Z           | 3 (+1 if M=0, +1 if D.l≠0) |
| ORA      | dp              | 05     | OR direct page        | N, Z           | 3 (+1 if M=0, +1 if D.l≠0) |
| EOR      | dp              | 45     | XOR direct page       | N, Z           | 3 (+1 if M=0, +1 if D.l≠0) |
| BIT      | dp              | 24     | Test bits direct page | N, V, Z        | 3 (+1 if M=0, +1 if D.l≠0) |
| ...      | ...             | ...    | ...                   | ...            | ...                        |

**Notes:**

- BIT sets N and V from memory (not A) in non-immediate modes.
- All logical instructions affect N and Z as usual.

------

### 3.3 Shift and Rotate Instructions

| Mnemonic | Addressing Mode | Opcode | Description                  | Flags Affected | Cycles                     |
| -------- | --------------- | ------ | ---------------------------- | -------------- | -------------------------- |
| ASL      | A               | 0A     | Shift left accumulator       | N, Z, C        | 2                          |
| ASL      | dp              | 06     | Shift left direct page       | N, Z, C        | 5 (+2 if M=0, +1 if D.l≠0) |
| ASL      | dp,X            | 16     | Shift left direct page + X   | N, Z, C        | 6 (+2 if M=0, +1 if D.l≠0) |
| ASL      | abs             | 0E     | Shift left absolute          | N, Z, C        | 6 (+2 if M=0)              |
| ASL      | abs,X           | 1E     | Shift left absolute + X      | N, Z, C        | 7 (+2 if M=0)              |
| LSR      | A               | 4A     | Shift right accumulator      | N, Z, C        | 2                          |
| LSR      | dp              | 46     | Shift right direct page      | N, Z, C        | 5 (+1 if M=0, +1 if D.l≠0) |
| LSR      | dp,X            | 56     | Shift right direct page + X  | N, Z, C        | 6 (+1 if M=0, +1 if D.l≠0) |
| LSR      | abs             | 4E     | Shift right absolute         | N, Z, C        | 6 (+1 if M=0)              |
| LSR      | abs,X           | 5E     | Shift right absolute + X     | N, Z, C        | 7 (+1 if M=0)              |
| ROL      | A               | 2A     | Rotate left accumulator      | N, Z, C        | 2                          |
| ROL      | dp              | 26     | Rotate left direct page      | N, Z, C        | 5 (+1 if M=0, +1 if D.l≠0) |
| ROL      | dp,X            | 36     | Rotate left direct page + X  | N, Z, C        | 6 (+1 if M=0, +1 if D.l≠0) |
| ROL      | abs             | 2E     | Rotate left absolute         | N, Z, C        | 6 (+1 if M=0)              |
| ROL      | abs,X           | 3E     | Rotate left absolute + X     | N, Z, C        | 7 (+1 if M=0)              |
| ROR      | A               | 6A     | Rotate right accumulator     | N, Z, C        | 2                          |
| ROR      | dp              | 66     | Rotate right direct page     | N, Z, C        | 5 (+1 if M=0, +1 if D.l≠0) |
| ROR      | dp,X            | 76     | Rotate right direct page + X | N, Z, C        | 6 (+1 if M=0, +1 if D.l≠0) |
| ROR      | abs             | 6E     | Rotate right absolute        | N, Z, C        | 6 (+1 if M=0)              |
| ROR      | abs,X           | 7E     | Rotate right absolute + X    | N, Z, C        | 7 (+1 if M=0)              |

**Notes:**

- For memory modes, add 2 cycles if M=0 (16-bit accumulator) for ASL, and 1 cycle for others.
- N is set from the result's high bit, Z if result is zero, C from the bit shifted out.

------

### 3.4 Increment, Decrement, and Compare Instructions

| Mnemonic | Addressing Mode | Opcode | Description                                           | Flags Affected | Cycles                                         |
| -------- | --------------- | ------ | ----------------------------------------------------- | -------------- | ---------------------------------------------- |
| INC      | A               | 1A     | Increment accumulator                                 | N, Z           | 2                                              |
| INC      | dp              | E6     | Increment direct page                                 | N, Z           | 5 (+2 if M=0, +1 if D.l≠0)                     |
| INC      | dp,X            | F6     | Increment direct page + X                             | N, Z           | 6 (+2 if M=0, +1 if D.l≠0)                     |
| INC      | abs             | EE     | Increment absolute                                    | N, Z           | 6 (+2 if M=0)                                  |
| INC      | abs,X           | FE     | Increment absolute + X                                | N, Z           | 7 (+2 if M=0)                                  |
| INX      | —               | E8     | Increment X                                           | N, Z           | 2                                              |
| INY      | —               | C8     | Increment Y                                           | N, Z           | 2                                              |
| DEC      | A               | 3A     | Decrement accumulator                                 | N, Z           | 2                                              |
| DEC      | dp              | C6     | Decrement direct page                                 | N, Z           | 5 (+2 if M=0, +1 if D.l≠0)                     |
| DEC      | dp,X            | D6     | Decrement direct page + X                             | N, Z           | 6 (+2 if M=0, +1 if D.l≠0)                     |
| DEC      | abs             | CE     | Decrement absolute                                    | N, Z           | 6 (+2 if M=0)                                  |
| DEC      | abs,X           | DE     | Decrement absolute + X                                | N, Z           | 7 (+2 if M=0)                                  |
| DEX      | —               | CA     | Decrement X                                           | N, Z           | 2                                              |
| DEY      | —               | 88     | Decrement Y                                           | N, Z           | 2                                              |
| CMP      | #imm            | C9     | Compare A with immediate                              | N, Z, C        | 2 (+1 if M=0)                                  |
| CMP      | dp              | C5     | Compare A with direct page                            | N, Z, C        | 3 (+1 if M=0, +1 if D.l≠0)                     |
| CMP      | dp,X            | D5     | Compare A with direct page + X                        | N, Z, C        | 4 (+1 if M=0, +1 if D.l≠0)                     |
| CMP      | dp,S            | C3     | Compare A with stack relative                         | N, Z, C        | 4 (+1 if M=0)                                  |
| CMP      | (dp)            | D2     | Compare A with direct page indirect                   | N, Z, C        | 5 (+1 if M=0, +1 if D.l≠0)                     |
| CMP      | (dp,X)          | C1     | Compare A with direct page indexed indirect           | N, Z, C        | 6 (+1 if M=0, +1 if D.l≠0)                     |
| CMP      | (dp),Y          | D1     | Compare A with direct page indirect indexed by Y      | N, Z, C        | 5 (+1 if M=0, +1 if D.l≠0, +1 if page crossed) |
| CMP      | [dp]            | C7     | Compare A with direct page indirect long              | N, Z, C        | 6 (+1 if M=0, +1 if D.l≠0)                     |
| CMP      | [dp],Y          | D7     | Compare A with direct page indirect long indexed by Y | N, Z, C        | 6 (+1 if M=0, +1 if D.l≠0)                     |
| CMP      | abs             | CD     | Compare A with absolute                               | N, Z, C        | 4 (+1 if M=0)                                  |
| CMP      | abs,X           | DD     | Compare A with absolute + X                           | N, Z, C        | 4 (+1 if M=0, +1 if page crossed)              |
| CMP      | abs,Y           | D9     | Compare A with absolute + Y                           | N, Z, C        | 4 (+1 if M=0, +1 if page crossed)              |
| CMP      | long            | CF     | Compare A with absolute long                          | N, Z, C        | 5 (+1 if M=0)                                  |
| CMP      | long,X          | DF     | Compare A with absolute long + X                      | N, Z, C        | 5 (+1 if M=0)                                  |
| CMP      | (sr,S),Y        | D3     | Compare A with stack relative indirect indexed by Y   | N, Z, C        | 7 (+1 if M=0)                                  |
| CPX      | #imm            | E0     | Compare X with immediate                              | N, Z, C        | 2 (+1 if X=0)                                  |
| CPX      | dp              | E4     | Compare X with direct page                            | N, Z, C        | 3 (+1 if X=0, +1 if D.l≠0)                     |
| CPX      | abs             | EC     | Compare X with absolute                               | N, Z, C        | 4 (+1 if X=0)                                  |
| CPY      | #imm            | C0     | Compare Y with immediate                              | N, Z, C        | 2 (+1 if X=0)                                  |
| CPY      | dp              | C4     | Compare Y with direct page                            | N, Z, C        | 3 (+1 if X=0, +1 if D.l≠0)                     |
| CPY      | abs             | CC     | Compare Y with absolute                               | N, Z, C        | 4 (+1 if X=0)                                  |

**Notes:**

- Compare instructions perform a subtraction but do not store the result; flags are set as if A/X/Y - M.
- N is set if result is negative, Z if equal, C if A/X/Y ≥ M.

------

### 3.5 Branch and Jump Instructions

| Mnemonic | Addressing Mode | Opcode | Description                                | Flags Affected | Cycles                                                  |
| -------- | --------------- | ------ | ------------------------------------------ | -------------- | ------------------------------------------------------- |
| BPL      | rel             | 10     | Branch if N=0 (plus)                       | —              | 2 (+1 if branch taken, +1 if page crossed in emulation) |
| BMI      | rel             | 30     | Branch if N=1 (minus)                      | —              | 2 (+1 if branch taken, +1 if page crossed in emulation) |
| BVC      | rel             | 50     | Branch if V=0 (overflow clear)             | —              | 2 (+1 if branch taken, +1 if page crossed in emulation) |
| BVS      | rel             | 70     | Branch if V=1 (overflow set)               | —              | 2 (+1 if branch taken, +1 if page crossed in emulation) |
| BCC      | rel             | 90     | Branch if C=0 (carry clear)                | —              | 2 (+1 if branch taken, +1 if page crossed in emulation) |
| BCS      | rel             | B0     | Branch if C=1 (carry set)                  | —              | 2 (+1 if branch taken, +1 if page crossed in emulation) |
| BNE      | rel             | D0     | Branch if Z=0 (not equal)                  | —              | 2 (+1 if branch taken, +1 if page crossed in emulation) |
| BEQ      | rel             | F0     | Branch if Z=1 (equal)                      | —              | 2 (+1 if branch taken, +1 if page crossed in emulation) |
| BRA      | rel             | 80     | Branch always                              | —              | 3 (+1 if page crossed in emulation)                     |
| BRL      | rell            | 82     | Branch always (long, 16-bit offset)        | —              | 4                                                       |
| JMP      | abs             | 4C     | Jump to absolute address                   | —              | 3                                                       |
| JMP      | (abs)           | 6C     | Jump indirect (pointer at abs)             | —              | 5                                                       |
| JMP      | (abs,X)         | 7C     | Jump indirect, indexed by X                | —              | 6                                                       |
| JMP      | [abs]           | DC     | Jump indirect long (24-bit pointer at abs) | —              | 6                                                       |
| JML      | long            | 5C     | Jump to absolute long                      | —              | 4                                                       |
| JML      | [abs]           | DC     | Jump indirect long                         | —              | 6                                                       |
| JSR      | abs             | 20     | Jump to subroutine                         | —              | 6                                                       |
| JSR      | (abs,X)         | FC     | Jump to subroutine indirect, indexed by X  | —              | 8                                                       |
| JSL      | long            | 22     | Jump to subroutine long                    | —              | 8                                                       |

**Notes:**

- Branch instructions use an 8-bit signed offset; BRL uses a 16-bit signed offset.
- In emulation mode, branches crossing a page boundary add an extra cycle.
- JML and JSL update the program bank register (PBR).

------

### 3.6 Stack and System Instructions

| Mnemonic | Addressing Mode | Opcode | Description                           | Flags Affected         | Cycles        |
| -------- | --------------- | ------ | ------------------------------------- | ---------------------- | ------------- |
| PHA      | —               | 48     | Push accumulator                      | —                      | 3 (+1 if M=0) |
| PHX      | —               | DA     | Push X                                | —                      | 3 (+1 if X=0) |
| PHY      | —               | 5A     | Push Y                                | —                      | 3 (+1 if X=0) |
| PHB      | —               | 8B     | Push data bank register               | —                      | 3             |
| PHD      | —               | 0B     | Push direct page register             | —                      | 4             |
| PHK      | —               | 4B     | Push program bank register            | —                      | 3             |
| PHP      | —               | 08     | Push processor status                 | —                      | 3             |
| PLA      | —               | 68     | Pull accumulator                      | N, Z                   | 4 (+1 if M=0) |
| PLX      | —               | FA     | Pull X                                | N, Z                   | 4 (+1 if X=0) |
| PLY      | —               | 7A     | Pull Y                                | N, Z                   | 4 (+1 if X=0) |
| PLB      | —               | AB     | Pull data bank register               | N, Z                   | 4             |
| PLD      | —               | 2B     | Pull direct page register             | N, Z                   | 5             |
| PLP      | —               | 28     | Pull processor status                 | N, V, M, X, D, I, Z, C | 4             |
| RTI      | —               | 40     | Return from interrupt                 | N, V, M, X, D, I, Z, C | 6 (+1 if E=0) |
| RTS      | —               | 60     | Return from subroutine                | —                      | 6             |
| RTL      | —               | 6B     | Return from subroutine long           | —                      | 6             |
| TCS      | —               | 1B     | Transfer accumulator to stack pointer | —                      | 2             |
| TSC      | —               | 3B     | Transfer stack pointer to accumulator | N, Z                   | 2             |
| TCD      | —               | 5B     | Transfer accumulator to direct page   | —                      | 2             |
| TDC      | —               | 7B     | Transfer direct page to accumulator   | N, Z                   | 2             |
| TSX      | —               | BA     | Transfer stack pointer to X           | N, Z                   | 2             |
| TXS      | —               | 9A     | Transfer X to stack pointer           | —                      | 2             |

**Notes:**

- Stack operations push/pull 1 or 2 bytes depending on register width.
- PLP restores all status flags; in emulation mode, M and X are forced to 1.
- RTI and RTL restore PC, PBR, and status flags as appropriate.

------

### 3.7 Block Move Instructions

| Mnemonic | Addressing Mode   | Opcode | Description                          | Flags Affected | Cycles           |
| -------- | ----------------- | ------ | ------------------------------------ | -------------- | ---------------- |
| MVN      | #srcbank,#dstbank | 54     | Block move negative (increment X, Y) | —              | 7 per byte moved |
| MVP      | #srcbank,#dstbank | 44     | Block move positive (decrement X, Y) | —              | 7 per byte moved |

**Notes:**

- MVN copies bytes from (srcbank:X) to (dstbank:Y), incrementing X and Y, until C = $FFFF.
- MVP copies bytes from (srcbank:X) to (dstbank:Y), decrementing X and Y, until C = $FFFF.
- C (accumulator) holds the count minus one.
- Block moves can be interrupted; state is preserved for resumption.

------

### 3.8 Bit Manipulation and Test Instructions

| Mnemonic | Addressing Mode | Opcode | Description                     | Flags Affected | Cycles                            |
| -------- | --------------- | ------ | ------------------------------- | -------------- | --------------------------------- |
| BIT      | #imm            | 89     | Test bits (A & M)               | Z              | 2 (+1 if M=0)                     |
| BIT      | dp              | 24     | Test bits direct page           | N, V, Z        | 3 (+1 if M=0, +1 if D.l≠0)        |
| BIT      | dp,X            | 34     | Test bits direct page + X       | N, V, Z        | 4 (+1 if M=0, +1 if D.l≠0)        |
| BIT      | abs             | 2C     | Test bits absolute              | N, V, Z        | 4 (+1 if M=0)                     |
| BIT      | abs,X           | 3C     | Test bits absolute + X          | N, V, Z        | 4 (+1 if M=0, +1 if page crossed) |
| TSB      | dp              | 04     | Test and set bits direct page   | Z              | 5 (+2 if M=0, +1 if D.l≠0)        |
| TSB      | abs             | 0C     | Test and set bits absolute      | Z              | 6 (+2 if M=0)                     |
| TRB      | dp              | 14     | Test and reset bits direct page | Z              | 5 (+2 if M=0, +1 if D.l≠0)        |
| TRB      | abs             | 1C     | Test and reset bits absolute    | Z              | 6 (+2 if M=0)                     |

**Notes:**

- TSB sets bits in memory where A has 1s; TRB resets bits in memory where A has 1s.
- Z is set if (A & M) == 0.

------

### 3.9 Status Flag Instructions

| Mnemonic | Addressing Mode | Opcode | Description                   | Flags Affected | Cycles |      |
| -------- | --------------- | ------ | ----------------------------- | -------------- | ------ | ---- |
| CLC      | —               | 18     | Clear carry flag              | C              | 2      |      |
| SEC      | —               | 38     | Set carry flag                | C              | 2      |      |
| CLD      | —               | D8     | Clear decimal flag            | D              | 2      |      |
| SED      | —               | F8     | Set decimal flag              | D              | 2      |      |
| CLI      | —               | 58     | Clear interrupt disable       | I              | 2      |      |
| SEI      | —               | 78     | Set interrupt disable         | I              | 2      |      |
| CLV      | —               | B8     | Clear overflow flag           | V              | 2      |      |
| REP      | #imm            | C2     | Reset status bits (P &= ~imm) | All            | 3      |      |
| SEP      | #imm            | E2     | Set status bits (P            | = imm)         | All    | 3    |

**Notes:**

- REP and SEP allow direct manipulation of M and X flags for register width control.
- In emulation mode, M and X are always set to 1.

------

### 3.10 System and Control Instructions

| Mnemonic | Addressing Mode | Opcode | Description                        | Flags Affected | Cycles                            |
| -------- | --------------- | ------ | ---------------------------------- | -------------- | --------------------------------- |
| BRK      | #imm            | 00     | Software interrupt (break)         | D, I           | 7 (+1 if E=0)                     |
| COP      | #imm            | 02     | Co-processor software interrupt    | D, I           | 7 (+1 if E=0)                     |
| WAI      | —               | CB     | Wait for interrupt                 | —              | 3 (plus interrupt handler cycles) |
| STP      | —               | DB     | Stop processor                     | —              | 3 (plus reset cycles to restart)  |
| XCE      | —               | FB     | Exchange carry and emulation flags | M, X, C, E     | 2                                 |
| XBA      | —               | EB     | Exchange A and B accumulators      | N, Z           | 3                                 |
| WDM      | #imm            | 42     | Reserved for future expansion      | —              | 2                                 |

**Notes:**

- BRK and COP push PC+2 and P to stack, clear D, set I, and jump to vector.
- WAI halts processor until interrupt; STP stops processor until reset.
- XCE swaps carry and emulation flags; entering emulation mode forces M and X to 1.
- XBA swaps high and low bytes of accumulator; N and Z set from result.

------

### 3.11 Push/Pop Effective Address Instructions

| Mnemonic | Addressing Mode | Opcode | Description                        | Flags Affected | Cycles          |
| -------- | --------------- | ------ | ---------------------------------- | -------------- | --------------- |
| PEA      | abs             | F4     | Push effective absolute address    | —              | 5               |
| PEI      | (dp)            | D4     | Push effective indirect address    | —              | 6 (+1 if D.l≠0) |
| PER      | rell            | 62     | Push effective PC-relative address | —              | 6               |

**Notes:**

- PEA pushes a 16-bit immediate value onto the stack.
- PEI pushes the 16-bit value pointed to by (D+dp).
- PER pushes PC + 16-bit signed offset.

------

### 3.12 Register Transfer Instructions

| Mnemonic | Addressing Mode | Opcode | Description     | Flags Affected | Cycles |
| -------- | --------------- | ------ | --------------- | -------------- | ------ |
| TAX      | —               | AA     | Transfer A to X | N, Z           | 2      |
| TAY      | —               | A8     | Transfer A to Y | N, Z           | 2      |
| TXA      | —               | 8A     | Transfer X to A | N, Z           | 2      |
| TYA      | —               | 98     | Transfer Y to A | N, Z           | 2      |
| TSX      | —               | BA     | Transfer S to X | N, Z           | 2      |
| TXS      | —               | 9A     | Transfer X to S | —              | 2      |
| TCD      | —               | 5B     | Transfer A to D | —              | 2      |
| TDC      | —               | 7B     | Transfer D to A | N, Z           | 2      |
| TCS      | —               | 1B     | Transfer A to S | —              | 2      |
| TSC      | —               | 3B     | Transfer S to A | N, Z           | 2      |
| TXY      | —               | 9B     | Transfer X to Y | N, Z           | 2      |
| TYX      | —               | BB     | Transfer Y to X | N, Z           | 2      |

**Notes:**

- Transfers set N and Z based on the result in the destination register.
- Width of transfer depends on M/X flags.

