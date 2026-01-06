# BackPocketBASIC

A fully-featured Applesoft BASIC interpreter written in .NET, complete with 6502 CPU emulation and Apple II memory space emulation.

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![License](https://img.shields.io/badge/license-MIT-green)

## Features

### Complete Applesoft BASIC Implementation

- **Program Control**: `REM`, `LET`, `DIM`, `DEF FN`, `END`, `STOP`, `CLEAR`
- **Flow Control**: `GOTO`, `GOSUB`/`RETURN`, `ON...GOTO`, `ON...GOSUB`, `IF...THEN`, `FOR...TO...STEP...NEXT`
- **I/O**: `PRINT`, `INPUT`, `GET`, `DATA`, `READ`, `RESTORE`
- **Memory/System**: `PEEK`, `POKE`, `CALL`, `HIMEM:`, `LOMEM:`, `&`, `USR`
- **Text Display**: `HOME`, `HTAB`, `VTAB`, `INVERSE`, `FLASH`, `NORMAL`
- **Graphics** (stubbed for future UI): `GR`, `HGR`, `HGR2`, `TEXT`, `COLOR=`, `HCOLOR=`, `PLOT`, `HPLOT`, `DRAW`, `XDRAW`

### Built-in Functions

| Category | Functions |
|----------|-----------|
| **Math** | `ABS`, `ATN`, `COS`, `EXP`, `INT`, `LOG`, `RND`, `SGN`, `SIN`, `SQR`, `TAN` |
| **String** | `LEN`, `VAL`, `ASC`, `MID$`, `LEFT$`, `RIGHT$`, `STR$`, `CHR$` |
| **Utility** | `PEEK`, `FRE`, `POS`, `SCRN`, `PDL`, `TAB`, `SPC`, `USR` |

### Custom Extensions

- **`SLEEP`** - Pauses execution for a specified number of milliseconds
  ```basic
  10 SLEEP 1000
  REM Pauses for 1 second
  ```

### Machine Language Integration

- **`&` (Ampersand)** - Calls a machine language routine at address $03F5 (1013)
  ```basic
  10 REM SET UP AN RTS AT $03F5
  20 POKE 1013, 96
  30 &
  40 PRINT "RETURNED FROM ML"
  ```

- **`USR`** - Calls a user machine language routine with a floating-point parameter
  ```basic
  10 REM SET UP AN RTS AT $000A
  20 POKE 10, 96
  30 X = USR(3.14159)
  40 PRINT "RESULT: "; X
  ```
  The parameter is evaluated and stored in FAC1 at $009D before the routine is called.
  The routine should return its result in FAC1.

### Multi-CPU Emulation (65C02/65816/65832)

- **Unified CPU Architecture**: Single codebase supports multiple CPU variants through runtime mode detection
- **65C02 Support**: Full WDC 65C02 instruction set (70 instructions, 15 addressing modes)
- **65816-Ready**: Architecture prepared for Apple IIgs 65816 emulation
- **65832-Ready**: Foundation for future 32-bit 65832 support
- **Extension Methods**: Size-aware register access (byte/word/dword views) adapts to CPU mode
- **Runtime Mode Detection**: Instructions adapt behavior based on CPU flags (E, M, X)
- **64KB Memory**: Emulated memory space matching Apple II memory map
- **Memory Operations**: `PEEK`, `POKE`, `CALL`, `&`, and `USR` operate with bounds checking
- **ProDOS Emulation**: System call emulation layer

## Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) or later

### Building

```bash
# Clone the repository
git clone https://github.com/Bad-Mango-Solutions/back-pocket-basic.git
cd back-pocket-basic

# Build the solution
dotnet build BackPocketBasic.slnx

# Run tests
dotnet test BackPocketBasic.slnx

# Run tests with statement coverage
dotnet test BackPocketBasic.slnx --collect "XPlat Code Coverage"
```

### Running a BASIC Program

```bash
dotnet run --project src/BadMango.Basic.Console/BadMango.Basic.Console.csproj -- <path-to-basic-file>
```

Or after building:

```bash
./src/BadMango.Basic.Console/bin/Debug/net10.0/bpbasic samples/demo.bas
```

## Sample Programs

Several sample programs are included in the `samples/` directory:

| File | Description |
|------|-------------|
| `demo.bas` | Comprehensive demo of language features |
| `primes.bas` | Prime number finder |
| `fibonacci.bas` | Fibonacci sequence generator |
| `memory.bas` | PEEK/POKE memory demonstration |
| `sleep.bas` | SLEEP command countdown demo |

### Example: Hello World

```basic
10 REM HELLO WORLD PROGRAM
20 HOME
30 PRINT "HELLO, WORLD!"
40 END
```

### Example: Using Variables and Loops

```basic
10 REM CALCULATE FACTORIAL
20 INPUT "ENTER A NUMBER: "; N
30 F = 1
40 FOR I = 1 TO N
50 F = F * I
60 NEXT I
70 PRINT N; "! = "; F
80 END
```

### Example: User-Defined Functions

```basic
10 DEF FN SQUARE(X) = X * X
20 DEF FN CUBE(X) = X * X * X
30 FOR I = 1 TO 5
40 PRINT I; " SQUARED = "; FN SQUARE(I); ", CUBED = "; FN CUBE(I)
50 NEXT I
60 END
```

### Example: Using PEEK and POKE

```basic
10 REM STORE AND RETRIEVE VALUES FROM MEMORY
20 POKE 768, 42
30 PRINT "VALUE AT 768: "; PEEK(768)
40 END
```

## Architecture

### Project Structure

```
back-pocket-basic/
├── src/
│   ├── BadMango.Basic/                # Core BASIC interpreter library
│   │   ├── AST/                       # Abstract Syntax Tree nodes
│   │   ├── Emulation/                 # Legacy 6502 and Apple II emulation
│   │   ├── Execution/                 # Interpreter implementation
│   │   ├── IO/                        # I/O abstraction
│   │   ├── Lexer/                     # Tokenizer
│   │   ├── Parser/                    # Parser
│   │   ├── Runtime/                   # Runtime environment
│   │   └── Tokens/                    # Token definitions
│   ├── BadMango.Basic.Console/        # Console application (assembly: bpbasic)
│   │
│   ├── BadMango.Emulator.Core/        # Core emulator abstractions
│   │   ├── Cpu/                       # CPU state, registers, instructions
│   │   ├── Interfaces/                # Core interfaces (IMemory, ICpu)
│   │   └── Signaling/                 # Interrupt and signal handling
│   ├── BadMango.Emulator.Emulation/   # CPU implementations
│   │   └── Cpu/                       # 65C02, 65816, 65832 implementations
│   ├── BadMango.Emulator.Bus/         # System bus and memory management
│   ├── BadMango.Emulator.Devices/     # Peripheral device implementations
│   ├── BadMango.Emulator.Systems/     # Complete system configurations
│   ├── BadMango.Emulator.Debug/       # Debugging infrastructure
│   ├── BadMango.Emulator.Infrastructure/ # Event and registration utilities
│   ├── BadMango.Emulator.Interop/     # Interoperability layer
│   ├── BadMango.Emulator.Configuration/ # Configuration management
│   ├── BadMango.Emulator.UI/          # Avalonia-based GUI (optional)
│   └── BadMango.Emulator.UI.Abstractions/ # UI abstraction interfaces
│
├── tests/
│   ├── BadMango.Basic.Tests/          # BASIC interpreter unit tests
│   ├── BadMango.Emulator.Tests/       # Emulator core tests
│   ├── BadMango.Emulator.Bus.Tests/   # Bus and memory tests
│   ├── BadMango.Emulator.Devices.Tests/ # Device tests
│   └── ... (additional test projects)
│
├── samples/                           # Sample BASIC programs
├── wiki/                              # GitHub Wiki documentation
└── BackPocketBasic.slnx               # Solution file
```

### Technologies Used

- **.NET 10.0** - Runtime and SDK
- **Avalonia UI 11.2+** - Cross-platform GUI framework (Emulator UI)
- **Microsoft.Extensions.Hosting** - Application hosting model
- **Serilog** - Structured logging
- **Autofac** - Dependency injection
- **CommunityToolkit.Mvvm** - MVVM implementation (Emulator UI)
- **NUnit** - Unit testing framework
- **Moq** - Mocking framework

### Key Components

#### BASIC Interpreter

1. **Lexer** (`BasicLexer`) - Tokenizes BASIC source code into tokens
2. **Parser** (`BasicParser`) - Parses tokens into an Abstract Syntax Tree
3. **Interpreter** (`BasicInterpreter`) - Executes the AST using the visitor pattern
4. **Memory** (`AppleMemory`) - 64KB emulated memory space for `PEEK`/`POKE`
5. **Apple System** (`AppleSystem`) - Coordinates CPU and memory emulation

#### Advanced Emulator Framework

1. **CPU Core** (`BadMango.Emulator.Core`) - Abstract CPU definitions and interfaces
   - `Registers` - Universal register set supporting 8/16/32-bit modes
   - `ProcessorStatusFlags` - Status flag management
   - `OpcodeTable` - Instruction dispatch infrastructure
2. **CPU Implementations** (`BadMango.Emulator.Emulation`)
   - `Cpu65C02` - Full WDC 65C02 implementation
   - `Cpu65816` - Apple IIgs 65816 support (planned)
   - `Cpu65832` - Hypothetical 32-bit 65832 support (planned)
   - `AddressingModes` - Shared addressing mode implementations
   - `Instructions` - Organized by category (Arithmetic, Branch, Compare, etc.)
3. **System Bus** (`BadMango.Emulator.Bus`) - Memory management and device routing
   - `MainBus` - System bus with layered memory mapping
   - `PhysicalMemory` - RAM/ROM management
   - `DeviceRegistry` - Peripheral device registration
4. **Devices** (`BadMango.Emulator.Devices`) - Peripheral implementations
   - Keyboard, Speaker, Video Mode controllers
   - Disk II controller stubs
   - Thunderclock and PocketWatch cards
5. **UI** (`BadMango.Emulator.UI`) - Avalonia-based graphical interface

## Language Reference

### Variables

- **Numeric variables**: `A`, `X1`, `COUNT` (only first 2 characters are significant)
- **String variables**: `A$`, `NAME$` (end with `$`)
- **Integer variables**: `N%`, `I%` (end with `%`)

### Arrays

Arrays are 0-based but `DIM` specifies the maximum index:

```basic
10 DIM A(10)        : REM Creates array with indices 0-10 (11 elements)
20 DIM B(5,5)       : REM 2-dimensional array
30 A(5) = 100
40 B(2,3) = 50
```

### Operators

| Operator | Description |
|----------|-------------|
| `+` | Addition / String concatenation |
| `-` | Subtraction |
| `*` | Multiplication |
| `/` | Division |
| `^` | Exponentiation |
| `=` | Equal to |
| `<>` or `><` | Not equal to |
| `<` | Less than |
| `>` | Greater than |
| `<=` | Less than or equal |
| `>=` | Greater than or equal |
| `AND` | Logical AND |
| `OR` | Logical OR |
| `NOT` | Logical NOT |

### Memory Map

The emulated Apple II memory map:

| Address Range | Description |
|---------------|-------------|
| `$0000-$00FF` | Zero Page |
| `$0100-$01FF` | Stack |
| `$0400-$07FF` | Text Page 1 / Lo-Res Page 1 |
| `$0800-$0BFF` | Text Page 2 / Lo-Res Page 2 |
| `$0800-$95FF` | BASIC Program Area |
| `$2000-$3FFF` | Hi-Res Page 1 |
| `$4000-$5FFF` | Hi-Res Page 2 |
| `$C000-$CFFF` | I/O Space (Soft Switches) |
| `$D000-$FFFF` | ROM Area |

## Logging

Logs are written to:
- **Console**: Warnings and errors only
- **File**: `logs/backpocket-<date>.log` with full debug information

## Future Enhancements

- [ ] Graphics UI for `GR`, `HGR`, `PLOT`, `HPLOT` commands
- [ ] Interactive REPL mode
- [ ] File I/O commands (`OPEN`, `CLOSE`, `PRINT#`, `INPUT#`)
- [ ] 65816 extended processor mode
- [ ] Disk image support

## Contributing

Contributions are welcome! Please see our [Contributing Guide](CONTRIBUTING.md) for details on:

- How to set up your development environment
- Coding standards and guidelines
- How to submit pull requests
- Reporting bugs and requesting features

### Quick Links

- **[Contributing Guide](CONTRIBUTING.md)** - Comprehensive contribution guidelines
- **[Setup Guide](SETUP_GUIDE.md)** - Repository configuration and branch protection setup
- **[Security Policy](.github/SECURITY.md)** - How to report security vulnerabilities

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Apple II and Applesoft BASIC are trademarks of Apple Inc.
- This is an educational project and is not affiliated with Apple Inc.
- Thanks to the retro computing community for preserving Apple II documentation
