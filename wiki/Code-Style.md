# Code Style

Coding standards and style guidelines for the BackPocketBASIC project.

## Overview

This project follows Microsoft's C# coding conventions with some additional project-specific guidelines. Most style rules are enforced through `.editorconfig` and `StyleCop.json`.

## Core Principles

1. **Readability First** - Code is read more often than written
2. **Consistency** - Follow existing patterns
3. **Simplicity** - Keep it simple and straightforward
4. **Maintainability** - Write code that's easy to maintain

## General C# Style

### Naming Conventions

#### Classes and Interfaces

```csharp
// Classes: PascalCase
public class BasicInterpreter { }
public class TokenType { }

// Interfaces: PascalCase with 'I' prefix
public interface IInputOutput { }
public interface ILexer { }
```

#### Methods and Properties

```csharp
// Methods: PascalCase
public void Execute(string source) { }
public BasicValue Evaluate(Expression expr) { }

// Properties: PascalCase
public string Name { get; set; }
public int LineNumber { get; }
```

#### Variables and Parameters

```csharp
// Local variables: camelCase
var lineNumber = 10;
var tokenList = new List<Token>();

// Parameters: camelCase
public void Process(string source, int lineNumber) { }

// Private fields: _camelCase (underscore prefix)
private readonly IInputOutput _io;
private int _currentLine;
```

#### Constants

```csharp
// Constants: PascalCase
public const int MaxLineNumber = 63999;
public const string DefaultPrompt = ">";

// Or UPPER_CASE for truly constant values
private const double EPSILON = 1e-10;
```

### Indentation and Spacing

#### Indentation

- **Use 4 spaces** (not tabs)
- Configured in `.editorconfig`

```csharp
public void Method()
{
    if (condition)
    {
        DoSomething();
    }
}
```

#### Braces

```csharp
// Opening brace on new line (Allman style)
public void Method()
{
    // Method body
}

// Even for single-line if statements
if (condition)
{
    DoSomething();
}
```

#### Spacing

```csharp
// Space after keywords
if (condition)
for (int i = 0; i < 10; i++)
while (running)

// Space around operators
int sum = a + b;
bool result = x > 5 && y < 10;

// No space before/after parentheses in method calls
Method();
DoSomething(param1, param2);

// Space after commas
Method(arg1, arg2, arg3);
```

### Line Length

- **Prefer lines under 120 characters**
- Break long lines for readability
- Break before operators on continuation lines

```csharp
// Good
var result = CalculateSomethingComplicated(
    firstParameter,
    secondParameter,
    thirdParameter);

// Good
if (conditionOne
    && conditionTwo
    && conditionThree)
{
    // Code
}
```

## Project-Specific Guidelines

### Use Ternary for Simple Conditionals

**From repository memories:**

```csharp
// Preferred for simple conditional assignments
dataPointer = (position < 0 || position >= dataValues.Count) ? 0 : position;

// Preferred for simple conditional returns
return num >= 0 ? (int)Math.Floor(num) : (int)Math.Ceiling(num);
```

### Floating-Point Comparisons

**From repository memories:**

```csharp
// Use epsilon-based comparisons
private const double EPSILON = 1e-10;

public bool Equals(double a, double b)
{
    return Math.Abs(a - b) < EPSILON;
}
```

### XML Documentation

Add XML comments for public APIs:

```csharp
/// <summary>
/// Executes a BASIC program from source code.
/// </summary>
/// <param name="source">The BASIC source code to execute.</param>
/// <exception cref="SyntaxErrorException">Thrown when source contains syntax errors.</exception>
public void Execute(string source)
{
    // Implementation
}
```

### Null Handling

```csharp
// Use null-conditional operator
var length = text?.Length ?? 0;

// Use null-coalescing operator
var result = value ?? defaultValue;

// Use pattern matching
if (obj is SpecificType specific)
{
    specific.DoSomething();
}
```

### String Handling

```csharp
// Use string interpolation
var message = $"Error on line {lineNumber}: {errorMessage}";

// Not
var message = "Error on line " + lineNumber + ": " + errorMessage;

// Use verbatim strings for multi-line or paths
var sql = @"
    SELECT * 
    FROM table 
    WHERE condition";

var path = @"C:\Path\To\File.txt";
```

### Collection Initialization

```csharp
// Collection initializers
var numbers = new List<int> { 1, 2, 3, 4, 5 };

var dictionary = new Dictionary<string, int>
{
    ["one"] = 1,
    ["two"] = 2,
    ["three"] = 3
};

// Use LINQ for transformations
var squares = numbers.Select(n => n * n).ToList();
var filtered = numbers.Where(n => n > 5).ToList();
```

### Dependency Injection

```csharp
// Constructor injection (preferred)
public class BasicInterpreter
{
    private readonly IInputOutput _io;
    private readonly AppleSystem _system;
    private readonly ILogger _logger;
    
    public BasicInterpreter(
        IInputOutput io,
        AppleSystem system,
        ILogger<BasicInterpreter> logger)
    {
        _io = io ?? throw new ArgumentNullException(nameof(io));
        _system = system ?? throw new ArgumentNullException(nameof(system));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}
```

## File Organization

### File Structure

```csharp
// 1. Using statements (sorted)
using System;
using System.Collections.Generic;
using System.Linq;
using BadMango.Basic.AST;
using Microsoft.Extensions.Logging;

// 2. Namespace
namespace BadMango.Basic.Execution
{
    // 3. Class/Interface
    public class BasicInterpreter : IBasicInterpreter
    {
        // 4. Constants
        private const double EPSILON = 1e-10;
        
        // 5. Fields
        private readonly IInputOutput _io;
        private int _currentLine;
        
        // 6. Constructors
        public BasicInterpreter(IInputOutput io)
        {
            _io = io;
        }
        
        // 7. Properties
        public bool IsRunning { get; private set; }
        
        // 8. Public methods
        public void Execute(string source)
        {
            // Implementation
        }
        
        // 9. Private methods
        private void ProcessLine(int lineNumber)
        {
            // Implementation
        }
    }
}
```

### One Class Per File

- Each class in its own file
- File name matches class name
- Exception: Small related helper classes

```
BasicInterpreter.cs           → class BasicInterpreter
BasicValue.cs                 → class BasicValue
IInputOutput.cs               → interface IInputOutput
```

## Error Handling

### Exceptions

```csharp
// Throw specific exceptions
throw new SyntaxErrorException($"Invalid syntax on line {lineNumber}");
throw new ArgumentNullException(nameof(parameter));
throw new InvalidOperationException("Cannot execute without program");

// Catch specific exceptions
try
{
    Execute(source);
}
catch (SyntaxErrorException ex)
{
    _logger.LogError(ex, "Syntax error in source");
    throw;
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error during execution");
    throw;
}
```

### Validation

```csharp
// Validate parameters
public void SetValue(int index, BasicValue value)
{
    if (index < 0)
        throw new ArgumentOutOfRangeException(nameof(index));
        
    if (value == null)
        throw new ArgumentNullException(nameof(value));
        
    _values[index] = value;
}
```

## LINQ and Functional Style

### When to Use LINQ

```csharp
// Good use cases
var activeUsers = users.Where(u => u.IsActive).ToList();
var userNames = users.Select(u => u.Name).ToList();
var hasErrors = items.Any(i => i.HasError);

// Avoid for simple operations
// Instead of:
var first = items.Where(i => i.Id == targetId).FirstOrDefault();
// Use:
var first = items.FirstOrDefault(i => i.Id == targetId);
```

### Method Chaining

```csharp
// Readable chaining
var result = collection
    .Where(item => item.IsValid)
    .Select(item => item.Transform())
    .OrderBy(item => item.Score)
    .ToList();
```

## Comments

### When to Comment

```csharp
// Good: Explain WHY, not WHAT
// Calculate using Fast Fourier Transform for better performance
var result = FFT(data);

// Good: Explain complex algorithms
// Use trial division up to sqrt(n) to test primality
for (int d = 2; d <= Math.Sqrt(n); d++)
{
    if (n % d == 0)
        return false;
}

// Bad: Obvious comments
// Increment i
i++;

// Set name to "John"
name = "John";
```

### TODO Comments

```csharp
// TODO: Implement graphics rendering
// TODO: Add support for DOS 3.3 disk format
// FIXME: Handle edge case when array is empty
// HACK: Temporary workaround until library is updated
```

## Testing Code Style

### Test Naming

```csharp
[Test]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange
    // Act
    // Assert
}
```

### Test Organization

```csharp
[TestFixture]
public class BasicInterpreterTests
{
    private Mock<IInputOutput> _mockIO;
    private BasicInterpreter _interpreter;
    
    [SetUp]
    public void SetUp()
    {
        _mockIO = new Mock<IInputOutput>();
        _interpreter = CreateInterpreter(_mockIO.Object);
    }
    
    [Test]
    public void Execute_SimpleProgram_RunsSuccessfully()
    {
        // Arrange
        var program = "10 PRINT \"HELLO\"";
        
        // Act
        _interpreter.Execute(program);
        
        // Assert
        _mockIO.Verify(io => io.WriteLine("HELLO"), Times.Once);
    }
}
```

## Common Patterns

### Visitor Pattern

```csharp
public interface IStatementVisitor
{
    void Visit(PrintStatement statement);
    void Visit(LetStatement statement);
    // Other visit methods
}

public class PrintStatement : IStatement
{
    public void Accept(IStatementVisitor visitor)
    {
        visitor.Visit(this);
    }
}
```

### Factory Pattern

```csharp
public class TokenFactory
{
    public static Token CreateKeyword(string text)
    {
        return new Token(TokenType.Keyword, text);
    }
    
    public static Token CreateNumber(double value)
    {
        return new Token(TokenType.Number, value.ToString());
    }
}
```

## Tools and Enforcement

### EditorConfig

Project includes `.editorconfig`:
- Automatically enforced by most IDEs
- Defines indentation, line endings, etc.

### StyleCop

Rules defined in `StyleCop.json`:
- Documentation requirements
- Naming conventions
- Code organization

### IDE Support

**Visual Studio:**
- Built-in code style enforcement
- Format document: Ctrl+K, Ctrl+D

**VS Code:**
- Install C# extension
- Format document: Shift+Alt+F

**Rider:**
- Built-in code analysis
- Reformat code: Ctrl+Alt+L

## Code Review Checklist

Before submitting PR, verify:

- [ ] Follows naming conventions
- [ ] XML docs for public APIs
- [ ] Tests added/updated
- [ ] No unnecessary comments
- [ ] Consistent with existing code
- [ ] No warnings in build
- [ ] Code formatted properly

## References

- **[Microsoft C# Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)**
- **[.NET Framework Design Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/)**

## Related Topics

- **[Development Setup](Development-Setup)** - Environment configuration
- **[Testing Guide](Testing-Guide)** - Writing tests
- **[Contributing](https://github.com/Bad-Mango-Solutions/back-pocket-basic/blob/main/CONTRIBUTING.md)** - Contribution process
