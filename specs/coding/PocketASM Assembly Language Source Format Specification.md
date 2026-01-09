# PocketASM Assembly Language Source Format Specification

------

## Introduction

The PocketASM assembly language is designed as a modern, extensible, and robust source format for 65C02 assembly programming, with a strong foundation in the Apple IIe-series assembly style. This specification aims to provide a definitive guide for assembler and build system implementers, ensuring clarity, consistency, and future extensibility. PocketASM balances the familiarity of classic 6502/65C02 assembly conventions with modern conveniences such as advanced macro systems, conditional assembly, symbol visibility controls, and compatibility with contemporary debugging and linking workflows. The format is intended to facilitate both direct binary output for emulation or ROM embedding and modular development for larger projects.

This document details the syntax, semantics, and operational rules of PocketASM source files, covering all aspects from basic instruction formatting to advanced features like symbol file emission, linking models, and extensibility mechanisms. Where appropriate, examples are provided to illustrate usage, and compatibility with established tools and debugging formats is considered throughout.

------

## 1. Source File Syntax and Structure

### 1.1. Line Structure and Fields

PocketASM source files are organized as a sequence of lines, each optionally containing up to four fields:

- **Label Field**: Optional. Begins at the first non-whitespace character of the line. Used to define labels or macro names.
- **Operator Field**: Specifies the instruction mnemonic, assembler directive, or macro invocation.
- **Argument Field**: Contains operands, arguments, or parameters for the operator.
- **Comment Field**: Begins with a semicolon (`;`) and extends to the end of the line. Comments are ignored by the assembler.

**Example:**

```assembly
LOOP    LDA #$10     ; Load immediate value 0x10 into accumulator
        STA $400     ; Store accumulator at screen memory
        INX          ; Increment X register
        BNE LOOP     ; Branch if not zero
```

This structure is consistent with both classic Apple IIe assembly and modern cross-assembler conventions.

#### 1.1.1. Whitespace and Formatting

- Fields are separated by one or more spaces or tabs.
- Blank lines are permitted and ignored.
- Indentation is optional but recommended for readability.

#### 1.1.2. Comments

- Any text following a semicolon (`;`) is treated as a comment.
- Comments can appear on their own line or after code.

**Example:**

```assembly
; This is a comment line
LDA #$41    ; Load ASCII 'A'
```

------

### 1.2. Labels and Symbols

#### 1.2.1. Global Labels

- Defined by placing a symbol at the start of a line, optionally followed by a colon (`:`).
- Must begin with a letter or underscore, followed by alphanumeric characters or underscores.
- Case sensitivity is assembler-dependent; PocketASM recommends case-insensitive labels for compatibility.

**Example:**

```assembly
START:  LDX #0
```

#### 1.2.2. Local Labels

- Local labels are scoped to the nearest preceding global label or explicitly delimited by a `.LOCAL` directive.
- Syntax: one to three decimal digits (1–255) immediately followed by a dollar sign (`$`), e.g., `10$`.
- Local labels can be reused in different scopes.

**Example:**

```assembly
LOOP    LDA #0
10$     INX
        BNE 10$
```

#### 1.2.3. Symbol Assignment

- Symbols can be assigned values using the `=` or `.EQU` directive.
- Symbols represent constants, addresses, or expressions.

**Example:**

```assembly
BUFFER = $2000
MAXLEN .EQU 128
```

#### 1.2.4. Special Symbols

- The asterisk (`*`) denotes the current program counter.

------

### 1.3. Instruction Formatting and Addressing Modes

PocketASM supports all 65C02 instruction mnemonics and addressing modes, with syntax closely following established conventions.

#### 1.3.1. Mnemonics

- Mnemonics are three-letter codes (e.g., `LDA`, `STA`, `JSR`).
- Case-insensitive.

#### 1.3.2. Addressing Modes

| Mode         | Syntax    | Example       | Description                               |
| ------------ | --------- | ------------- | ----------------------------------------- |
| Immediate    | `#value`  | `LDA #$10`    | Operand is a constant                     |
| Zero Page    | `$addr`   | `LDA $00`     | 8-bit address in $0000–$00FF              |
| Zero Page,X  | `$addr,X` | `LDA $10,X`   | Zero page address plus X                  |
| Zero Page,Y  | `$addr,Y` | `LDX $20,Y`   | Zero page address plus Y                  |
| Absolute     | `$addr`   | `STA $2000`   | 16-bit address                            |
| Absolute,X   | `$addr,X` | `LDA $4000,X` | Absolute address plus X                   |
| Absolute,Y   | `$addr,Y` | `LDA $4000,Y` | Absolute address plus Y                   |
| Indirect     | `($addr)` | `JMP ($FFFC)` | Address stored at $addr                   |
| (Indirect,X) | `($zp,X)` | `LDA ($20,X)` | Zero page pointer plus X, then indirect   |
| (Indirect),Y | `($zp),Y` | `LDA ($20),Y` | Zero page pointer, then add Y             |
| Accumulator  | `A`       | `LSR A`       | Operate on accumulator                    |
| Implied      | (none)    | `CLC`         | No operand                                |
| Relative     | `label`   | `BNE LOOP`    | Branch to label within -126 to +129 bytes |

**Example:**

```assembly
LDA #$FF         ; Immediate
STA $400         ; Absolute
LDA $FB,X        ; Zero Page,X
JMP ($FFFC)      ; Indirect
```

#### 1.3.3. Numeric Literals

- Hexadecimal: `$1A7F`
- Decimal: `12345`
- Octal: `@17777`
- Binary: `%01010100111`
- Character: `'A'` (ASCII code)

------

### 1.4. Expressions and Operators

PocketASM supports expressions in operands and directives, evaluated left to right with defined precedence.

#### 1.4.1. Operators

| Operator | Description    | Example     |
| -------- | -------------- | ----------- |
| `+`      | Addition       | `LABEL+1`   |
| `-`      | Subtraction    | `LABEL-1`   |
| `*`      | Multiplication | `2*LABEL`   |
| `/`      | Division       | `LABEL/2`   |
| `!`      | Bitwise XOR    | `LABEL!$FF` |
| `.`      | Bitwise OR     | `LABEL.$0F` |
| `&`      | Bitwise AND    | `LABEL&$7F` |
| `<`      | Low byte       | `<LABEL`    |
| `>`      | High byte      | `>LABEL`    |

**Example:**

```assembly
LDA #<BUFFER     ; Low byte of BUFFER
LDA #>BUFFER     ; High byte of BUFFER
```

------

## 2. Directives and Pseudo-Ops

PocketASM provides a comprehensive set of directives for code organization, data definition, memory allocation, and assembler control.

### 2.1. Origin and Section Control

- `.ORG <address>`: Set the program counter to `<address>`.
- `.CODE`, `.DATA`, `.BSS`, `.PAGE0`: Switch between code, initialized data, uninitialized data, and zero page sections.

**Example:**

```assembly
.ORG $8000
.CODE
```

### 2.2. Data Definition

- `.BYTE` or `.DB`: Define bytes.
- `.WORD` or `.DW`: Define 16-bit words (little-endian).
- `.DBYTE`: Define 16-bit words (big-endian).
- `.LONG`: Define 32-bit values.
- `.SPACE` or `.DS`: Reserve bytes (uninitialized).
- `.RMB <n>`: Reserve n bytes (like `*=*+n`).

**Example:**

```assembly
.BYTE $01, $02, $03
.WORD $1234, $5678
.SPACE 16
```

### 2.3. Constants and Equates

- `.EQU` or `=`: Define a constant symbol.
- `.SET`: Define or redefine a symbol.

**Example:**

```assembly
MAXLEN .EQU 128
COUNT  .SET 0
```

### 2.4. File Inclusion

- `.INCLUDE "filename"`: Include another source file.
- `.APPEND "filename"`: Continue assembly from another file.
- `.INSERT "filename"`: Insert binary data from a file.

### 2.5. Conditional Assembly

- `.IF <expr>`, `.ELSE`, `.ENDIF`: Conditional code inclusion.
- `.IFE`, `.IFN`, `.IFGE`, `.IFGT`, `.IFLT`, `.IFLE`: Numeric conditionals.
- `.IFDEF`, `.IFNDEF`: Symbol defined or not.

**Example:**

```assembly
.IF DEBUG
    JSR DebugRoutine
.ENDIF
```

### 2.6. Macro System

- `.MACRO` ... `.ENDM`: Define a macro.
- `.REPT <n>` ... `.ENDR`: Repeat a block n times.
- `.IRP <param>,args` ... `.ENDR`: Iterate over arguments.
- `.IRPC <param>,string` ... `.ENDR`: Iterate over characters.

**Example:**

```assembly
CLEAR .MACRO
    LDA #0
    STA $2000
.ENDM

CLEAR
```

### 2.7. Export and Visibility

- `.GLOBAL <symbol>`: Make symbol visible to linker.
- `.EXTERN <symbol>`: Declare external symbol.
- `.EXPORT <symbol>`: Export symbol for linking.
- `.IMPORT <symbol>`: Import symbol from another module.

------

## 3. Macro System

### 3.1. Macro Definition and Invocation

Macros enable reusable code templates with optional parameters.

**Definition:**

```assembly
SWAP .MACRO src, dst
    LDA src
    TAX
    LDA dst
    STA src
    TXA
    STA dst
.ENDM
```

**Invocation:**

```assembly
SWAP $10, $20
```

### 3.2. Parameters and Expansion

- Macros support positional and named parameters.
- Arguments can be expressions, symbols, or literals.
- Angle brackets `< >` allow passing arguments with spaces or commas.

**Example:**

```assembly
LDI .MACRO arg
    LDA #>arg
    LDX #<arg
.ENDM

LDI $1234
```

### 3.3. Repetition and Iteration

- `.REPT n` ... `.ENDR`: Repeat block n times.
- `.IRP param, list` ... `.ENDR`: Iterate over list.
- `.IRPC param, string` ... `.ENDR`: Iterate over string characters.

**Example:**

```assembly
.REPT 4
    NOP
.ENDR
```

### 3.4. Local Symbols in Macros

- Use unique local labels within macros to avoid naming conflicts.
- Some assemblers provide automatic label generation or `\?` for unique labels.

------

## 4. Constants, Expressions, and Evaluation Rules

### 4.1. Numeric Literals

- Hexadecimal: `$1A7F`
- Decimal: `12345`
- Octal: `@17777`
- Binary: `%01010100111`
- Character: `'A'`, `'AB'` (first character in low byte)

### 4.2. Expression Evaluation

- Expressions are evaluated left to right, with operator precedence.
- Supported operators: `+`, `-`, `*`, `/`, `%`, `&`, `|`, `^`, `~`, `<`, `>`, `==`, `!=`, `&&`, `||`.

**Example:**

```assembly
LDA #MAXLEN-1
```

------

## 5. Conditional Assembly and Build-Time Conditionals

Conditional assembly enables code inclusion/exclusion based on symbols or expressions.

**Example:**

```assembly
.IFDEF FEATURE_X
    JSR FeatureXInit
.ELSE
    JSR DefaultInit
.ENDIF
```

- `.IF`, `.ELSE`, `.ENDIF` for general conditions.
- `.IFDEF`, `.IFNDEF` for symbol presence.
- `.IFE`, `.IFN`, `.IFGE`, etc., for numeric comparisons.
- Nesting of conditionals is supported.

------

## 6. Public-Facing Routine Entry Points and Export Mechanism

### 6.1. Exporting Symbols

- Use `.EXPORT <symbol>` or `.GLOBAL <symbol>` to mark routines or data as public.
- Exported symbols are included in the symbol table for linking and debugging.

**Example:**

```assembly
.EXPORT InitScreen
InitScreen:
    LDA #$00
    STA $400
    RTS
```

### 6.2. Importing Symbols

- Use `.IMPORT <symbol>` or `.EXTERN <symbol>` to declare symbols defined in other modules.

------

## 7. Linking Model and Symbol Resolution

### 7.1. Symbol Visibility

- **Public (Global) Symbols**: Exported for use by other modules.
- **Private (Local) Symbols**: Not exported; internal to the module.

### 7.2. Symbol Resolution

- The assembler builds a symbol table during assembly.
- The linker resolves external references, applying relocations as needed.
- Forward references and multiple definitions are handled according to assembler/linker rules.

### 7.3. Relocation and Fixups

- Relocation entries are generated for addresses or symbols not resolved at assembly time.
- The relocation table includes offset, symbol, type, and addend.
- Section-specific relocation tables are supported (e.g., `.rela.text`, `.rela.data`).

------

## 8. Symbol File Emission and Debug Symbol Formats

### 8.1. Symbol Files

- Symbol files map source identifiers to binary addresses and are essential for debugging.
- PocketASM should emit symbol files compatible with modern debugging formats (e.g., PDB, DWARF).
- Symbol files may include:
  - Function and variable names
  - Addresses
  - Data types (where applicable)
  - Source file and line number mappings

### 8.2. Public and Private Symbols

- **Full Symbol Files**: Contain both public and private symbols, including local variables and type information.
- **Stripped Symbol Files**: Contain only public symbols (function entry points, exported variables).

**Best Practice:** Emit both full and stripped symbol files to support debugging and secure distribution.

------

## 9. Output Format Specification

### 9.1. Flat Binary Output

- The default output is a flat binary (byte array), suitable for direct loading into emulators or ROMs.
- The binary is constructed by concatenating code and data sections as specified by `.ORG` and section directives.

### 9.2. Metadata and Headers

- Optional metadata (e.g., entry point, version, platform) may be included in a header or as a separate file.
- For advanced use, support for object file formats (e.g., o65, ELF) is recommended for modular builds and linking.

### 9.3. Section Layout

- Code, data, and uninitialized sections are placed according to their origin and section directives.
- Alignment and padding are handled as specified by `.ALIGN` or similar directives.

------

## 10. Symbol Visibility and Stripping

- **Public Symbols**: Exported for linking and debugging.
- **Private Symbols**: Internal to the module; can be stripped from symbol files for release builds.
- **Stripped Symbol Files**: Contain only public symbols, reducing information leakage and file size.

**Tools:** Provide options to generate full or stripped symbol files, following best practices from modern toolchains.

------

## 11. Extensibility for Future CPU Targets

### 11.1. CPU Selection

- The assembler supports multiple CPU targets via a directive or command-line switch (e.g., `/C1` for 65C02).
- Future CPUs can be added by extending the instruction set and addressing mode tables.

**Example:**

```assembly
.CPU "65C02"
```

### 11.2. Platform-Specific Features

- Platform-specific pragmas or directives enable features unique to a target system (e.g., Apple IIe screen memory).
- Use `.PRAGMA` or similar mechanisms for platform extensions.

**Example:**

```assembly
.PRAGMA APPLE2E_SCREEN
```

------

## 12. Platform-Specific Features and Pragmas (Apple IIe)

- Memory-mapped I/O (e.g., screen at `$400`)
- ROM routines (e.g., `JSR $FDED` for character output)
- HIMEM and memory reservation conventions
- Support for Apple IIe-specific hardware and soft switches

------

## 13. Assembler Error Reporting and Diagnostics

- Errors are reported with codes and messages, both in the console and in listing files.
- Error types include syntax errors, undefined symbols, multiply-defined symbols, invalid opcodes, and more.
- Warnings and informational messages are provided for best practices and potential issues.

**Example Error Codes:**

- `E`: Expression error
- `U`: Undefined symbol
- `M`: Multiply-defined symbol
- `S`: Syntax error

**Best Practice:** Provide clear, actionable diagnostics and support for suppressing or promoting warnings as needed.

------

## 14. Build System Integration and Toolchain Workflow

- PocketASM is designed for integration with modern build systems (e.g., Make, CMake).
- Command-line options allow specification of source files, output files, CPU targets, macro depth, and more.
- Support for dependency generation, incremental builds, and automated testing is recommended.

**Example Command:**

```shell
pocketasm -c 65c02 -o program.bin program.asm
```

------

## 15. Testing, Validation, and Emulation Compatibility

- Output binaries are compatible with emulators and hardware.
- Provide test suites and reference implementations to validate assembler correctness.
- Support for automated regression testing and emulator integration is encouraged.

------

## 16. Examples and Reference Implementations

### 16.1. Simple Program

```assembly
.ORG $8000
START:  LDA #$41        ; Load ASCII 'A'
        JSR $FDED       ; Output character
        RTS
```

### 16.2. Macro Example

```assembly
CLEAR .MACRO addr
    LDA #0
    STA addr
.ENDM

CLEAR $2000
```

### 16.3. Conditional Assembly

```assembly
.IFDEF DEBUG
    JSR DebugRoutine
.ENDIF
```

### 16.4. Exported Routine

```assembly
.EXPORT PrintChar
PrintChar:
    LDA #$41
    JSR $FDED
    RTS
```

------

## 17. License, Distribution, and Documentation Conventions

- PocketASM source code and documentation should be distributed under a permissive open-source license (e.g., MIT, BSD, Apache 2.0) to maximize adoption and contribution.
- Always include a `LICENSE` file and clear documentation of licensing terms.
- Documentation should be comprehensive, up-to-date, and include usage examples, error codes, and platform notes.

------

## 18. Extensibility and Future Directions

- The specification is designed for extensibility to new CPUs, platforms, and debugging formats.
- Modular architecture allows for the addition of new directives, macro features, and output formats.
- Community contributions and open-source development are encouraged.

------

## 19. Summary Table: PocketASM Core Directives

| Directive                  | Purpose                             | Example                   |
| -------------------------- | ----------------------------------- | ------------------------- |
| `.ORG`                     | Set program counter                 | `.ORG $8000`              |
| `.BYTE` / `.DB`            | Define bytes                        | `.BYTE $01, $02, $03`     |
| `.WORD` / `.DW`            | Define 16-bit words (little-endian) | `.WORD $1234, $5678`      |
| `.DBYTE`                   | Define 16-bit words (big-endian)    | `.DBYTE $1234`            |
| `.LONG`                    | Define 32-bit values                | `.LONG $12345678`         |
| `.SPACE` / `.DS`           | Reserve bytes                       | `.SPACE 16`               |
| `.EQU` / `=`               | Define constant symbol              | `MAXLEN .EQU 128`         |
| `.SET`                     | Redefine symbol                     | `COUNT .SET 0`            |
| `.INCLUDE`                 | Include source file                 | `.INCLUDE "macros.inc"`   |
| `.MACRO` / `.ENDM`         | Define macro                        | See macro example above   |
| `.REPT` / `.ENDR`          | Repeat block                        | `.REPT 4 ... .ENDR`       |
| `.IF` / `.ELSE` / `.ENDIF` | Conditional assembly                | See conditional example   |
| `.EXPORT`                  | Export symbol                       | `.EXPORT PrintChar`       |
| `.IMPORT`                  | Import symbol                       | `.IMPORT ExternalRoutine` |
| `.GLOBAL`                  | Make symbol global                  | `.GLOBAL Main`            |
| `.EXTERN`                  | Declare external symbol             | `.EXTERN DataBuffer`      |

------

## 20. Reference Implementations and Further Reading

- **Open-Source Assemblers**: NASM, YASM, ca65, VASM, x65, WLA-DX, and others provide valuable reference architectures and feature sets.
- **Apple IIe Assembly Language**: Classic texts and manuals provide foundational knowledge and practical examples.
- **Modern Debugging and Symbol Formats**: Microsoft PDB, DWARF, and ELF documentation for symbol file integration.

------

## Conclusion

PocketASM is a modern, extensible assembly language source format rooted in the Apple IIe tradition but designed for contemporary development workflows. Its specification provides a comprehensive, modular, and future-proof foundation for assembler and build system implementers, supporting advanced macro processing, conditional assembly, robust symbol management, and compatibility with modern debugging and linking standards. By adhering to these conventions and best practices, developers can create maintainable, portable, and high-performance 65C02 (and future CPU) assembly projects suitable for both retrocomputing and modern embedded applications.