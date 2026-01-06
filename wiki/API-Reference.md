# API Reference

Documentation for developers integrating the BackPocketBASIC library into their applications.

## Overview

The `BadMango.Basic` library can be embedded in .NET applications to provide Applesoft BASIC scripting capabilities.

## Installation

### Add NuGet Package Reference

**Option 1:** Add via .csproj:
```xml
<ItemGroup>
  <ProjectReference Include="path/to/BadMango.Basic/BadMango.Basic.csproj" />
</ItemGroup>
```

**Option 2:** Add via dotnet CLI (when published to NuGet):
```bash
dotnet add package BadMango.Basic
```

## Quick Start

### Basic Usage

```csharp
using BadMango.Basic;
using BadMango.Basic.IO;

// Create I/O handler
var io = new ConsoleIO();

// Create interpreter
var interpreter = new BasicInterpreter(io);

// Run BASIC code
string program = @"
10 PRINT ""HELLO FROM BASIC""
20 X = 42
30 PRINT X * 2
";

interpreter.Execute(program);
```

### With Dependency Injection

```csharp
using Autofac;
using BadMango.Basic;

// Register services
var builder = new ContainerBuilder();
builder.RegisterModule<InterpreterModule>();

var container = builder.Build();

// Resolve interpreter
using var scope = container.BeginLifetimeScope();
var interpreter = scope.Resolve<IBasicInterpreter>();

// Execute code
interpreter.Execute("10 PRINT \"HELLO\"");
```

## Core Interfaces

### IBasicInterpreter

Main interface for executing BASIC programs.

```csharp
public interface IBasicInterpreter
{
    /// <summary>
    /// Executes BASIC source code.
    /// </summary>
    void Execute(string source);
    
    /// <summary>
    /// Executes a parsed program.
    /// </summary>
    void ExecuteProgram(Program program);
    
    /// <summary>
    /// Resets the interpreter state.
    /// </summary>
    void Reset();
}
```

**Methods:**

#### Execute(string source)

Executes BASIC source code.

**Parameters:**
- `source` - BASIC program text

**Example:**
```csharp
interpreter.Execute("10 PRINT \"HELLO\"\n20 END");
```

#### ExecuteProgram(Program program)

Executes a pre-parsed program.

**Parameters:**
- `program` - Parsed AST

**Example:**
```csharp
var lexer = new BasicLexer();
var parser = new BasicParser();
var tokens = lexer.Tokenize(source);
var program = parser.Parse(tokens);
interpreter.ExecuteProgram(program);
```

#### Reset()

Resets interpreter state (clears variables, stack, etc.).

```csharp
interpreter.Reset();
```

---

### IInputOutput

Interface for custom I/O implementations.

```csharp
public interface IInputOutput
{
    /// <summary>
    /// Writes a line of text followed by newline.
    /// </summary>
    void WriteLine(string text);
    
    /// <summary>
    /// Writes text without newline.
    /// </summary>
    void Write(string text);
    
    /// <summary>
    /// Reads a line of input from the user.
    /// </summary>
    string ReadLine();
    
    /// <summary>
    /// Reads a single character without echo.
    /// </summary>
    char ReadKey();
}
```

**Implementations:**

#### ConsoleIO

Standard console I/O:
```csharp
var io = new ConsoleIO();
```

#### Custom Implementation

Create custom I/O handler:
```csharp
public class CustomIO : IInputOutput
{
    private readonly TextWriter _output;
    private readonly TextReader _input;
    
    public CustomIO(TextWriter output, TextReader input)
    {
        _output = output;
        _input = input;
    }
    
    public void WriteLine(string text) => _output.WriteLine(text);
    public void Write(string text) => _output.Write(text);
    public string ReadLine() => _input.ReadLine() ?? "";
    public char ReadKey()
    {
        int ch = _input.Read();
        return ch >= 0 ? (char)ch : '\0';
    }
}
```

---

### ILexer

Tokenizes BASIC source code.

```csharp
public interface ILexer
{
    /// <summary>
    /// Tokenizes BASIC source code.
    /// </summary>
    IEnumerable<Token> Tokenize(string source);
}
```

**Usage:**
```csharp
var lexer = new BasicLexer();
var tokens = lexer.Tokenize("10 PRINT \"HELLO\"");

foreach (var token in tokens)
{
    Console.WriteLine($"{token.Type}: {token.Value}");
}
```

---

### IParser

Parses tokens into AST.

```csharp
public interface IParser
{
    /// <summary>
    /// Parses tokens into a program AST.
    /// </summary>
    Program Parse(IEnumerable<Token> tokens);
}
```

**Usage:**
```csharp
var parser = new BasicParser();
var program = parser.Parse(tokens);
```

---

## Core Classes

### BasicInterpreter

Main interpreter implementation.

**Constructor:**
```csharp
public BasicInterpreter(
    IInputOutput io,
    AppleSystem appleSystem,
    ILogger<BasicInterpreter> logger)
```

**Example:**
```csharp
var io = new ConsoleIO();
var system = new AppleSystem();
var logger = NullLogger<BasicInterpreter>.Instance;

var interpreter = new BasicInterpreter(io, system, logger);
```

---

### AppleSystem

Coordinates 6502 CPU and memory emulation.

**Properties:**
```csharp
public AppleMemory Memory { get; }
public Cpu6502 Cpu { get; }
public AppleSpeaker Speaker { get; }
```

**Usage:**
```csharp
var system = new AppleSystem();

// Access memory
byte value = system.Memory.Read(0x300);
system.Memory.Write(0x300, 0x42);

// Execute machine code
system.Cpu.Execute(0x300);
```

---

### BasicValue

Represents runtime values (numbers, strings, integers).

**Creation:**
```csharp
var number = BasicValue.FromNumber(42.5);
var str = BasicValue.FromString("HELLO");

// From Apple II storage types
var fromMbf = BasicValue.FromMbf(mbfValue);
var fromInt = BasicValue.FromBasicInteger(intValue);
var fromStr = BasicValue.FromBasicString(strValue);
```

**Type Checking:**
```csharp
if (value.IsNumeric)
{
    double num = value.AsNumber();
}
```

**Apple II Storage Conversions:**
```csharp
// Get authentic Apple II representations
MBF mbf = value.AsMbf();              // 5-byte floating-point
BasicInteger bi = value.AsBasicInteger(); // 16-bit integer
BasicString bs = value.AsBasicString();   // 7-bit ASCII string
```

**Operations:**
```csharp
var sum = a + b;
var product = a * b;
```

---

### Apple II Storage Types

#### MBF (Microsoft Binary Format)

5-byte floating-point format used by Applesoft BASIC.

```csharp
MBF value = 3.14159;          // Implicit conversion
double d = value;             // Implicit conversion back
byte[] bytes = value.ToBytes(); // Raw bytes
MBF restored = MBF.FromBytes(bytes);
```

#### BasicInteger

16-bit signed integer for integer variables (% suffix).

```csharp
BasicInteger value = 42;
byte[] bytes = value.ToBytes();  // Little-endian
BasicInteger restored = BasicInteger.FromBytes(bytes);
```

#### BasicString

7-bit ASCII string (max 255 characters) with Span/Memory support.

```csharp
BasicString value = "HELLO WORLD";
byte[] bytes = value.ToBytes();  // 7-bit ASCII
BasicString restored = BasicString.FromBytes(bytes);

// Range-based slicing
BasicString sub = value[0..5];   // "HELLO"
BasicString end = value[^5..];   // "WORLD"

// Span-based access (allocation-free)
ReadOnlySpan<byte> span = value.AsSpan();
ReadOnlySpan<byte> slice = value.AsSpan(6..);
ReadOnlyMemory<byte> memory = value.AsMemory();

// Create from spans
BasicString fromSpan = BasicString.FromSpan(data.AsSpan());

// Copy operations
value.CopyTo(destination);
bool success = value.TryCopyTo(destination);
```

---

## Usage Scenarios

### Scenario 1: Simple Script Execution

```csharp
using BadMango.Basic;
using BadMango.Basic.IO;

public class SimpleExample
{
    public void Run()
    {
        var io = new ConsoleIO();
        var interpreter = new BasicInterpreter(io);
        
        string script = @"
10 PRINT ""CALCULATING...""
20 SUM = 0
30 FOR I = 1 TO 100
40 SUM = SUM + I
50 NEXT I
60 PRINT ""SUM = ""; SUM
";
        
        interpreter.Execute(script);
    }
}
```

### Scenario 2: Custom I/O Redirection

```csharp
public class CustomIOExample
{
    public string ExecuteWithCapture(string program)
    {
        var output = new StringWriter();
        var input = new StringReader("");
        var io = new CustomIO(output, input);
        
        var interpreter = new BasicInterpreter(io);
        interpreter.Execute(program);
        
        return output.ToString();
    }
}

// Usage
var example = new CustomIOExample();
string result = example.ExecuteWithCapture("10 PRINT \"HELLO\"");
// result = "HELLO\n"
```

### Scenario 3: Interactive REPL

```csharp
public class ReplExample
{
    public void RunRepl()
    {
        var io = new ConsoleIO();
        var interpreter = new BasicInterpreter(io);
        
        while (true)
        {
            Console.Write("> ");
            string line = Console.ReadLine();
            
            if (line == "EXIT") break;
            
            try
            {
                interpreter.Execute(line);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
```

### Scenario 4: File Execution

```csharp
public class FileExample
{
    public void ExecuteFile(string path)
    {
        var io = new ConsoleIO();
        var interpreter = new BasicInterpreter(io);
        
        string program = File.ReadAllText(path);
        interpreter.Execute(program);
    }
}
```

### Scenario 5: With Logging

```csharp
using Microsoft.Extensions.Logging;
using Serilog;

public class LoggingExample
{
    public void RunWithLogging()
    {
        // Setup Serilog
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("basic.log")
            .CreateLogger();
        
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSerilog();
        });
        
        var logger = loggerFactory.CreateLogger<BasicInterpreter>();
        var io = new ConsoleIO();
        var system = new AppleSystem();
        
        var interpreter = new BasicInterpreter(io, system, logger);
        interpreter.Execute("10 PRINT \"HELLO\"");
        
        Log.CloseAndFlush();
    }
}
```

---

## Error Handling

### Exception Types

**InterpreterException:**
Base exception for interpreter errors.

**SyntaxErrorException:**
Thrown for syntax errors.

**RuntimeErrorException:**
Thrown for runtime errors.

### Handling Errors

```csharp
try
{
    interpreter.Execute(program);
}
catch (SyntaxErrorException ex)
{
    Console.WriteLine($"Syntax Error: {ex.Message}");
    Console.WriteLine($"Line: {ex.LineNumber}");
}
catch (RuntimeErrorException ex)
{
    Console.WriteLine($"Runtime Error: {ex.Message}");
}
catch (InterpreterException ex)
{
    Console.WriteLine($"Interpreter Error: {ex.Message}");
}
```

---

## Advanced Topics

### Custom Memory Initialization

```csharp
var system = new AppleSystem();

// Pre-load data into memory
byte[] machineCode = { 0xA9, 0x00, 0x60 }; // LDA #$00; RTS
system.Memory.WriteRange(0x300, machineCode);

var interpreter = new BasicInterpreter(io, system, logger);
interpreter.Execute("10 CALL 768");
```

### Program Analysis

```csharp
var lexer = new BasicLexer();
var parser = new BasicParser();

var tokens = lexer.Tokenize(source);
var program = parser.Parse(tokens);

// Analyze AST
foreach (var line in program.Lines)
{
    Console.WriteLine($"Line {line.LineNumber}:");
    foreach (var statement in line.Statements)
    {
        Console.WriteLine($"  {statement.GetType().Name}");
    }
}
```

---

## Performance Considerations

### Best Practices

1. **Reuse Interpreter**: Create once, execute multiple times
2. **Pre-parse**: Parse once, execute multiple times
3. **Limit Output**: Excessive printing can slow execution
4. **Use Integer Variables**: `I%` faster than `I`

### Benchmarking

```csharp
using System.Diagnostics;

var sw = Stopwatch.StartNew();
interpreter.Execute(program);
sw.Stop();

Console.WriteLine($"Execution time: {sw.ElapsedMilliseconds}ms");
```

---

## Dependencies

The interpreter requires:

- **Microsoft.Extensions.Hosting** (≥8.0.0)
- **Serilog** (≥3.1.0)
- **Autofac** (≥8.0.0)

The emulator UI additionally requires:
- **Avalonia** (≥11.0.0)
- **CommunityToolkit.Mvvm** (≥8.0.0)

---

## Emulator Framework API

For advanced emulator integration, the `BadMango.Emulator.*` namespace provides additional APIs:

### CPU Access

```csharp
using BadMango.Emulator.Emulation.Cpu;
using BadMango.Emulator.Core.Cpu;

// Create a 65C02 CPU
var cpu = new Cpu65C02();

// Access registers
cpu.Registers.A = 0x42;
cpu.Registers.X = 0x10;
cpu.Registers.PC = 0x0300;

// Execute a single instruction
cpu.Step(memory);
```

### Memory Bus

```csharp
using BadMango.Emulator.Bus;

// Create a machine
var builder = new MachineBuilder();
var machine = builder.Build();

// Access the bus
var bus = machine.Bus;
byte value = bus.Read(0x0300);
bus.Write(0x0300, 0x42);
```

### Device Integration

```csharp
using BadMango.Emulator.Devices;

// Access keyboard
var keyboard = machine.GetComponent<KeyboardController>();
keyboard.SetKeyDown(0xC1); // 'A' key

// Access speaker
var speaker = machine.GetComponent<SpeakerController>();
speaker.Toggle();
```

---

## Related Topics

- **[Architecture Overview](Architecture-Overview)** - Internal design
- **[Development Setup](Development-Setup)** - Contributing
- **[Language Reference](Language-Reference)** - BASIC commands
- **[6502 Emulation](6502-Emulation)** - CPU and multi-CPU framework

## Support

For issues or questions:
- [GitHub Issues](https://github.com/Bad-Mango-Solutions/back-pocket-basic/issues)
- [Contributing Guide](https://github.com/Bad-Mango-Solutions/back-pocket-basic/blob/main/CONTRIBUTING.md)
