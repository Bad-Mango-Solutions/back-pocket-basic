# Architecture Overview

Comprehensive overview of the BackPocketBASIC project structure and architecture.

## Table of Contents

- [Project Structure](#project-structure)
- [Module Overview](#module-overview)
- [Core Components](#core-components)
- [Emulator Framework](#emulator-framework)
- [Execution Pipeline](#execution-pipeline)
- [Technologies Used](#technologies-used)
- [Design Patterns](#design-patterns)
- [Dependencies](#dependencies)

## Project Structure

```
back-pocket-basic/
├── src/
│   ├── BadMango.Basic/                # Core BASIC interpreter library
│   │   ├── AST/                       # Abstract Syntax Tree nodes
│   │   ├── Emulation/                 # Legacy 6502 and Apple II emulation
│   │   ├── Execution/                 # Interpreter implementation
│   │   ├── IO/                        # I/O abstraction layer
│   │   ├── Lexer/                     # Tokenization (source → tokens)
│   │   ├── Parser/                    # Parsing (tokens → AST)
│   │   ├── Runtime/                   # Runtime environment & state
│   │   └── Tokens/                    # Token type definitions
│   ├── BadMango.Basic.Console/        # Console application (bpbasic)
│   │
│   ├── BadMango.Emulator.Core/        # Core emulator abstractions
│   │   ├── Cpu/                       # CPU state, registers, opcode handling
│   │   ├── Interfaces/                # IMemory, ICpu, IDebugger interfaces
│   │   ├── Configuration/             # CPU capabilities and modes
│   │   └── Signaling/                 # Interrupt and event signaling
│   ├── BadMango.Emulator.Emulation/   # CPU implementations
│   │   ├── Cpu/                       # 65C02, 65816, 65832 implementations
│   │   └── Debugging/                 # Debug support for CPUs
│   ├── BadMango.Emulator.Bus/         # System bus and memory management
│   │   ├── MainBus                    # Primary system bus
│   │   ├── PhysicalMemory             # RAM/ROM memory
│   │   ├── DeviceRegistry             # Device management
│   │   └── Scheduler                  # Event scheduling
│   ├── BadMango.Emulator.Devices/     # Peripheral device implementations
│   │   ├── KeyboardController         # Keyboard input
│   │   ├── SpeakerController          # Audio output
│   │   ├── VideoModeController        # Display modes
│   │   └── DiskII*                    # Disk II controller stubs
│   ├── BadMango.Emulator.Systems/     # Complete system configurations
│   ├── BadMango.Emulator.Debug/       # Debugging infrastructure
│   ├── BadMango.Emulator.Debug.Infrastructure/ # Debug service support
│   ├── BadMango.Emulator.Infrastructure/ # Event and registration utilities
│   ├── BadMango.Emulator.Interop/     # Native interoperability
│   ├── BadMango.Emulator.Configuration/ # Configuration management
│   ├── BadMango.Emulator.UI/          # Avalonia-based GUI
│   └── BadMango.Emulator.UI.Abstractions/ # UI abstraction interfaces
│
├── tests/
│   ├── BadMango.Basic.Tests/          # BASIC interpreter tests
│   ├── BadMango.Emulator.Tests/       # Emulator core tests
│   ├── BadMango.Emulator.Bus.Tests/   # Bus and memory tests
│   ├── BadMango.Emulator.Devices.Tests/ # Device tests
│   ├── BadMango.Emulator.Configuration.Tests/
│   ├── BadMango.Emulator.Debug.Tests/
│   ├── BadMango.Emulator.Debug.Infrastructure.Tests/
│   ├── BadMango.Emulator.Infrastructure.Tests/
│   └── BadMango.Emulator.UI.Tests/    # UI ViewModel tests
│
├── samples/                           # Sample BASIC programs
├── wiki/                              # GitHub Wiki documentation
├── schemas/                           # JSON schemas
├── specs/                             # Specifications
├── reference/                         # Reference materials
├── profiles/                          # Machine profiles
├── BackPocketBasic.slnx               # Solution file
├── README.md                          # Project overview
├── CONTRIBUTING.md                    # Contribution guidelines
└── SETUP_GUIDE.md                     # Setup instructions
```

## Module Overview

| Module | Purpose |
|--------|---------|
| **BadMango.Basic** | Applesoft BASIC interpreter with integrated 6502 emulation |
| **BadMango.Basic.Console** | Command-line interface for running BASIC programs |
| **BadMango.Emulator.Core** | CPU abstractions, interfaces, and common types |
| **BadMango.Emulator.Emulation** | 65C02/65816/65832 CPU implementations |
| **BadMango.Emulator.Bus** | System bus, memory mapping, device routing |
| **BadMango.Emulator.Devices** | Peripheral hardware emulation |
| **BadMango.Emulator.Systems** | Complete system configurations (Apple II, IIgs) |
| **BadMango.Emulator.Debug** | Debugging and tracing infrastructure |
| **BadMango.Emulator.UI** | Cross-platform Avalonia GUI |

## Core Components

### 1. Lexer (`BasicLexer`)

**Purpose**: Converts BASIC source code into tokens.

**Location**: `src/BadMango.Basic/Lexer/`

**Responsibilities**:
- Read source code character by character
- Identify keywords, operators, literals, identifiers
- Generate token stream
- Handle line numbers
- Report lexical errors

**Example**:
```
Input:  "10 PRINT "HELLO""
Output: [LineNumber(10), Keyword(PRINT), String("HELLO")]
```

**Key Classes**:
- `BasicLexer` - Main lexer implementation
- Token types defined in `Tokens/TokenType.cs`

---

### 2. Parser (`BasicParser`)

**Purpose**: Converts token stream into Abstract Syntax Tree (AST).

**Location**: `src/BadMango.Basic/Parser/`

**Responsibilities**:
- Parse tokens according to grammar rules
- Build AST representing program structure
- Validate syntax
- Report parse errors

**Example**:
```
Input:  [LineNumber(10), Keyword(PRINT), String("HELLO")]
Output: PrintStatement(StringLiteral("HELLO"))
```

**Key Classes**:
- `BasicParser` - Main parser implementation
- Grammar rules for each statement type

---

### 3. Abstract Syntax Tree (AST)

**Purpose**: Represents the program structure as a tree of nodes.

**Location**: `src/BadMango.Basic/AST/`

**Node Types**:

**Statements**:
- `PrintStatement` - PRINT command
- `LetStatement` - Variable assignment
- `ForStatement` - FOR...NEXT loops
- `IfStatement` - IF...THEN conditionals
- `GotoStatement` - GOTO jumps
- `GosubStatement` - GOSUB subroutine calls
- Many more...

**Expressions**:
- `BinaryExpression` - Operations (a + b, a * b)
- `UnaryExpression` - Unary operations (-x, NOT x)
- `NumberLiteral` - Numeric constants
- `StringLiteral` - String constants
- `VariableReference` - Variable access
- `FunctionCall` - Function invocations

**Base Interfaces**:
- `IStatement` - All statements implement this
- `IExpression` - All expressions implement this
- Visitor pattern for traversal

---

### 4. Interpreter (`BasicInterpreter`)

**Purpose**: Executes the AST using the visitor pattern.

**Location**: `src/BadMango.Basic/Execution/`

**Responsibilities**:
- Traverse AST nodes
- Execute statements
- Evaluate expressions
- Manage program flow
- Handle errors

**Execution Model**:
- Visitor pattern for node traversal
- State management via `RuntimeContext`
- Line-by-line execution
- Stack for GOSUB/RETURN

**Key Classes**:
- `BasicInterpreter` - Main interpreter
- `RuntimeContext` - Execution state

---

### 5. Runtime Environment

**Purpose**: Manages program state during execution.

**Location**: `src/BadMango.Basic/Runtime/`

**Components**:

**`RuntimeContext`**:
- Variable storage
- Array management
- FOR loop stack
- GOSUB call stack
- DATA pointer

**`BasicValue`**:
- Represents values (numbers, strings, integers)
- Type conversions (including MBF, BasicInteger, BasicString)
- Operator implementations
- `AsMbf()`, `AsBasicInteger()`, `AsBasicString()` for authentic Apple II representations

**`DataManager`**:
- Manages DATA statements
- Tracks READ pointer
- Handles RESTORE

**`ForLoopState`**:
- FOR loop metadata
- Counter variables
- Loop limits and steps

**`VariableTable`**:
- Variable storage
- Scope management
- Type checking

**Apple II Storage Types** (in `Emulation` namespace):
- `MBF` - Microsoft Binary Format (5-byte floating-point)
- `BasicInteger` - 16-bit signed integer (2-byte, little-endian)
- `BasicString` - 7-bit ASCII string (max 255 chars)

---

### 6. Legacy 6502 Emulation (BadMango.Basic)

**Purpose**: Provides basic 6502 CPU and Apple II emulation for the BASIC interpreter.

**Location**: `src/BadMango.Basic/Emulation/`

**Components**:

**`Cpu6502`**:
- Basic 6502 instruction set
- Registers (A, X, Y, SP, PC, Status)
- Opcode execution

**`AppleMemory`**:
- 64KB memory space
- Memory-mapped I/O
- Apple II memory map
- Bounds checking

**`AppleSystem`**:
- Coordinates CPU and memory
- System initialization
- Hardware emulation

**`AppleSpeaker`**:
- Speaker emulation
- Sound generation

See [6502 Emulation](6502-Emulation) for details.

---

### 7. Advanced Emulator Framework

**Purpose**: Provides a comprehensive, modular emulator framework supporting multiple CPU variants and system configurations.

**Location**: `src/BadMango.Emulator.*`

#### BadMango.Emulator.Core

**CPU Abstractions**:
- `Registers` - Universal register structure supporting 8/16/32-bit modes
- `ProcessorStatusFlags` - P register flag management
- `CpuCapabilities` - Feature detection for CPU variants
- `OpcodeTable` / `OpcodeHandler` - Instruction dispatch system

**Key Interfaces**:
- `IMemory` - Memory access abstraction
- `ICpu` - CPU lifecycle and execution

#### BadMango.Emulator.Emulation

**CPU Implementations**:
- `CpuBase` - Common CPU infrastructure
- `Cpu65C02` - Full WDC 65C02 with all addressing modes
- `Cpu65816` - Apple IIgs 65816 (planned)
- `Cpu65832` - Hypothetical 32-bit variant (planned)

**Instruction Organization**:
```
Instructions.cs          # Core instruction infrastructure
Instructions.65C02.cs    # 65C02-specific instructions
Instructions.Arithmetic.cs
Instructions.Branch.cs
Instructions.Compare.cs
Instructions.Flags.cs
Instructions.Jump.cs
Instructions.Logical.cs
Instructions.Shift.cs
Instructions.Stack.cs
Instructions.Transfer.cs
```

**Addressing Modes** (`AddressingModes.cs`):
- Shared implementations for all addressing modes
- Compositional pattern: instructions accept addressing mode delegates
- Eliminates code duplication across CPU variants

#### BadMango.Emulator.Bus

**System Bus**:
- `MainBus` - Primary system bus with layered memory mapping
- `PhysicalMemory` - RAM and ROM backing stores
- `MappingStack` / `MappingLayer` - Flexible memory mapping
- `DeviceRegistry` - Peripheral registration and routing

**Memory Management**:
- `LanguageCardController` - Bank switching
- `AuxiliaryMemoryController` - Extended memory
- `RegionManager` - Memory region definitions

**Scheduling**:
- `Scheduler` - Event-based timing
- `ScheduledEvent` - Timed callbacks

#### BadMango.Emulator.Devices

**Peripheral Implementations**:
- `KeyboardController` - Keyboard input handling
- `SpeakerController` - Audio toggle emulation
- `VideoModeController` - Display mode switching
- `GameIOController` - Paddle/button input
- `DiskIIControllerStub` - Placeholder for disk emulation
- `ThunderclockCard` - Real-time clock
- `PocketWatchCard` - Alternative clock card

#### BadMango.Emulator.UI

**Avalonia-based GUI** (cross-platform):
- `MainWindow` - Application shell
- `MachineManagerViewModel` - Machine lifecycle management
- `ThemeService` - Dark/light theme support
- `NavigationService` - View navigation

---

### 8. I/O Abstraction

**Purpose**: Abstracts input/output for testability.

**Location**: `src/BadMango.Basic/IO/`

**Interfaces**:

**`IInputOutput`**:
- `WriteLine(string)` - Output text
- `Write(string)` - Output without newline
- `ReadLine()` - Read line input
- `ReadKey()` - Read single key

**Implementations**:
- `ConsoleIO` - Standard console I/O
- `MockIO` - Testing implementation

**Benefits**:
- Testable without actual console
- Can redirect I/O for embedding
- Platform abstraction

---

### 9. Console Application

**Purpose**: Command-line interface for running BASIC programs.

**Location**: `src/BadMango.Basic.Console/`

**Features**:
- File loading
- Argument parsing
- Logging setup
- Host configuration

**Usage**:
```bash
bpbasic <basic-file>
```

---

## Execution Pipeline

### Step-by-Step Flow

```
Source Code
    ↓
[Lexer]
    ↓
Token Stream
    ↓
[Parser]
    ↓
Abstract Syntax Tree
    ↓
[Interpreter]
    ↓
Execution (with Runtime Context)
    ↓
Output
```

### Detailed Example

**Source Code**:
```basic
10 X = 5
20 PRINT X * 2
```

**1. Lexer Output**:
```
Line 10: [LineNumber(10), Identifier(X), Equals, Number(5)]
Line 20: [LineNumber(20), Keyword(PRINT), Identifier(X), Multiply, Number(2)]
```

**2. Parser Output** (AST):
```
Program
├── Line 10: LetStatement
│   ├── Variable: X
│   └── Value: NumberLiteral(5)
└── Line 20: PrintStatement
    └── Expression: BinaryExpression
        ├── Left: VariableReference(X)
        ├── Operator: Multiply
        └── Right: NumberLiteral(2)
```

**3. Interpreter Execution**:
```
1. Execute Line 10:
   - Evaluate: NumberLiteral(5) → 5
   - Store: X = 5

2. Execute Line 20:
   - Evaluate: VariableReference(X) → 5
   - Evaluate: NumberLiteral(2) → 2
   - Evaluate: BinaryExpression(5 * 2) → 10
   - Print: 10
```

**4. Output**:
```
10
```

---

## Technologies Used

### Core Technologies

| Technology | Version | Purpose |
|------------|---------|---------|
| **.NET** | 10.0 | Runtime and framework |
| **C#** | 13.0 | Implementation language |

### Libraries

| Library | Purpose |
|---------|---------|
| **Microsoft.Extensions.Hosting** | Application hosting model |
| **Serilog** | Structured logging |
| **Autofac** | Dependency injection container |
| **Avalonia UI** | Cross-platform GUI framework (11.x) |
| **CommunityToolkit.Mvvm** | MVVM implementation for UI |
| **NUnit** | Unit testing framework |
| **Moq** | Mocking framework for tests |

### Development Tools

- **Visual Studio / VS Code / Rider** - IDEs
- **Git** - Version control
- **GitHub Actions** - CI/CD
- **dotnet CLI** - Build and test automation

---

## Design Patterns

### 1. Visitor Pattern

**Used In**: AST traversal, interpreter execution

**Why**: Separates algorithms from object structure

**Example**:
```csharp
interface IStatement
{
    void Accept(IStatementVisitor visitor);
}

class PrintStatement : IStatement
{
    public void Accept(IStatementVisitor visitor)
    {
        visitor.Visit(this);
    }
}
```

---

### 2. Interpreter Pattern

**Used In**: Command execution

**Why**: Represents grammar and evaluates sentences

**Example**: Each AST node knows how to evaluate itself via visitor.

---

### 3. Strategy Pattern

**Used In**: I/O abstraction

**Why**: Makes I/O strategy interchangeable

**Example**:
```csharp
interface IInputOutput
{
    void WriteLine(string text);
}

class ConsoleIO : IInputOutput { ... }
class MockIO : IInputOutput { ... }
```

---

### 4. Factory Pattern

**Used In**: AST node creation

**Why**: Centralizes object creation

**Example**: Parser creates appropriate AST nodes based on token type.

---

### 5. Singleton Pattern

**Used In**: System components (CPU, Memory)

**Why**: Single instance of emulated hardware

**Example**: `AppleSystem` coordinates single CPU and memory instance.

---

### 6. Dependency Injection

**Used In**: Throughout application

**Why**: Loose coupling, testability

**Example**:
```csharp
public class BasicInterpreter
{
    public BasicInterpreter(
        IInputOutput io,
        AppleSystem system,
        ILogger logger)
    {
        // Dependencies injected
    }
}
```

---

## Dependencies

### Dependency Graph

```
BadMango.Basic.Console
    └─> BadMango.Basic
            ├─> Microsoft.Extensions.Hosting
            ├─> Serilog
            └─> Autofac

BadMango.Emulator.UI
    ├─> BadMango.Emulator.Core
    ├─> BadMango.Emulator.Bus
    ├─> Avalonia
    ├─> CommunityToolkit.Mvvm
    ├─> Autofac
    └─> Serilog

BadMango.Emulator.Emulation
    └─> BadMango.Emulator.Core

BadMango.Emulator.Devices
    ├─> BadMango.Emulator.Core
    └─> BadMango.Emulator.Bus

BadMango.Basic.Tests
    ├─> BadMango.Basic
    ├─> NUnit
    └─> Moq

BadMango.Emulator.*.Tests
    ├─> Respective Emulator Projects
    ├─> NUnit
    └─> Moq
```

### Dependency Injection Container

**Registration** (`InterpreterModule.cs`):
```csharp
builder.RegisterType<BasicLexer>()
    .AsImplementedInterfaces()
    .InstancePerLifetimeScope();

builder.RegisterType<BasicParser>()
    .AsImplementedInterfaces()
    .InstancePerLifetimeScope();

builder.RegisterType<BasicInterpreter>()
    .AsImplementedInterfaces()
    .InstancePerLifetimeScope();
```

---

## Key Design Decisions

### Why AST Instead of Direct Interpretation?

**Advantages**:
- Cleaner separation of concerns
- Easier to implement optimizations
- Better error reporting
- Extensible design

### Why Visitor Pattern?

**Advantages**:
- Add new operations without modifying AST nodes
- Type-safe traversal
- Clear separation between structure and algorithms

### Why Emulated 6502?

**Purpose**:
- Authentic PEEK/POKE/CALL behavior
- Matches Apple II memory map
- Educational value
- Historical accuracy

### Why Dependency Injection?

**Benefits**:
- Testability (mock dependencies)
- Loose coupling
- Configuration flexibility
- Clear dependencies

---

## Performance Considerations

### Optimization Strategies

1. **Token Caching**: Lexer caches tokens for reuse
2. **Variable Lookup**: Hash-based variable tables
3. **Loop Optimization**: Pre-computed loop bounds
4. **String Pooling**: Reuse common strings

### Performance Characteristics

- **Lexing**: O(n) where n = source length
- **Parsing**: O(n) where n = token count
- **Variable Access**: O(1) average case
- **Array Access**: O(1) for bounds-checked access

---

## Testing Strategy

### Unit Tests

- **Component Level**: Test each class independently
- **Mocking**: Use Moq for dependencies
- **Coverage**: High coverage on core components

### Integration Tests

- **End-to-End**: Test complete programs
- **Sample Programs**: Use samples as tests
- **Regression**: Prevent breaking changes

### Test Structure

```csharp
[Test]
public void Print_OutputsToConsole()
{
    // Arrange
    var mockIO = new MockIO();
    var interpreter = new BasicInterpreter(mockIO, ...);
    
    // Act
    interpreter.Execute("10 PRINT \"HELLO\"");
    
    // Assert
    Assert.That(mockIO.Output, Contains.Substring("HELLO"));
}
```

---

## Extensibility Points

### Adding New Commands

1. Define token type
2. Create AST node
3. Update lexer
4. Update parser
5. Implement visitor method
6. Add tests

### Adding New Functions

1. Define token type
2. Update lexer for function name
3. Update expression parser
4. Implement evaluation logic
5. Add tests

### Adding New I/O Targets

1. Implement `IInputOutput`
2. Register in DI container
3. Configure in application startup

---

## Related Topics

- **[6502 Emulation](6502-Emulation)** - CPU and memory details
- **[Memory Map](Memory-Map)** - Apple II memory layout
- **[API Reference](API-Reference)** - Library integration
- **[Development Setup](Development-Setup)** - Contributing
- **[Testing Guide](Testing-Guide)** - Writing tests

## Further Reading

- [Compiler Design Theory](https://en.wikipedia.org/wiki/Compiler)
- [Visitor Pattern](https://refactoring.guru/design-patterns/visitor)
- [6502 Architecture](http://www.6502.org/)
- [Apple II Reference Manual](https://www.apple2.org/)
