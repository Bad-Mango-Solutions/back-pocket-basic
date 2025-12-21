# Testing Guide

Comprehensive guide for running and writing tests for BackPocketBASIC.

## Overview

The project uses NUnit for unit testing and Moq for mocking. All tests are in the `tests/BadMango.Basic.Tests` project.

## Running Tests

### Command Line

```bash
# Run all tests
dotnet test BackPocketBasic.slnx

# Run with verbose output
dotnet test BackPocketBasic.slnx --verbosity detailed

# Run specific test class
dotnet test --filter FullyQualifiedName~BasicInterpreterTests

# Run specific test method
dotnet test --filter Name=Print_OutputsCorrectText

# Run tests matching pattern
dotnet test --filter Name~Print

# Run tests in category
dotnet test --filter Category=Parser
```

### Visual Studio

1. **Test Explorer**: `Test → Windows → Test Explorer`
2. **Run All**: Click "Run All" button
3. **Run Specific**: Right-click test → Run
4. **Debug**: Right-click test → Debug

### Visual Studio Code

With .NET Core Test Explorer extension:
1. Open Test Explorer panel
2. Click run/debug icons next to tests

### JetBrains Rider

1. **Unit Tests**: `View → Tool Windows → Unit Tests`
2. **Run All**: Click "Run All" icon
3. **Run Specific**: Right-click test → Run
4. **Debug**: Right-click test → Debug

## Test Organization

### Directory Structure

```
tests/BadMango.Basic.Tests/
├── InterpreterTests.cs      # High-level interpreter tests
├── LexerTests.cs            # Tokenization tests
├── ParserTests.cs           # Parsing tests
├── EmulationTests.cs        # 6502 CPU/memory tests
├── RuntimeTests.cs          # Runtime environment tests
├── BuiltInFunctionTests.cs  # Function tests
├── CommandTests.cs          # Individual command tests
└── IntegrationTests.cs      # End-to-end tests
```

### Test Categories

Tests are organized by categories:

- **Lexer** - Tokenization tests
- **Parser** - Parsing tests
- **Interpreter** - Execution tests
- **Emulation** - CPU/memory tests
- **Functions** - Built-in function tests
- **Integration** - End-to-end tests

## Writing Tests

### Test Structure

Use the **Arrange-Act-Assert** pattern:

```csharp
[Test]
public void Print_Statement_OutputsText()
{
    // Arrange - Set up test data and dependencies
    var mockIO = new Mock<IInputOutput>();
    var interpreter = CreateInterpreter(mockIO.Object);
    var program = "10 PRINT \"HELLO\"";
    
    // Act - Perform the operation
    interpreter.Execute(program);
    
    // Assert - Verify the results
    mockIO.Verify(io => io.WriteLine("HELLO"), Times.Once);
}
```

### Test Naming Convention

Use descriptive names that explain what is being tested:

```
MethodName_Scenario_ExpectedBehavior
```

**Examples:**
```csharp
[Test]
public void Print_WithString_OutputsToConsole() { }

[Test]
public void ForLoop_WithStep_CountsCorrectly() { }

[Test]
public void Peek_ValidAddress_ReturnsMemoryValue() { }

[Test]
public void Parse_InvalidSyntax_ThrowsException() { }
```

### Basic Test Template

```csharp
using NUnit.Framework;
using Moq;
using BadMango.Basic;

namespace BadMango.Basic.Tests
{
    [TestFixture]
    public class YourComponentTests
    {
        private Mock<IInputOutput> _mockIO;
        private BasicInterpreter _interpreter;
        
        [SetUp]
        public void SetUp()
        {
            // Runs before each test
            _mockIO = new Mock<IInputOutput>();
            _interpreter = CreateInterpreter(_mockIO.Object);
        }
        
        [TearDown]
        public void TearDown()
        {
            // Runs after each test (if needed)
        }
        
        [Test]
        public void YourTest_Scenario_ExpectedBehavior()
        {
            // Arrange
            var input = "test data";
            
            // Act
            var result = _interpreter.Execute(input);
            
            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }
        
        private BasicInterpreter CreateInterpreter(IInputOutput io)
        {
            var system = new AppleSystem();
            var logger = NullLogger<BasicInterpreter>.Instance;
            return new BasicInterpreter(io, system, logger);
        }
    }
}
```

## Common Test Patterns

### Testing Output

```csharp
[Test]
public void Print_OutputsToIO()
{
    // Arrange
    var mockIO = new Mock<IInputOutput>();
    var interpreter = CreateInterpreter(mockIO.Object);
    
    // Act
    interpreter.Execute("10 PRINT \"HELLO\"");
    
    // Assert
    mockIO.Verify(io => io.WriteLine("HELLO"), Times.Once);
}
```

### Testing Input

```csharp
[Test]
public void Input_ReadsFromIO()
{
    // Arrange
    var mockIO = new Mock<IInputOutput>();
    mockIO.Setup(io => io.ReadLine()).Returns("42");
    var interpreter = CreateInterpreter(mockIO.Object);
    
    // Act
    interpreter.Execute("10 INPUT X\n20 PRINT X");
    
    // Assert
    mockIO.Verify(io => io.WriteLine("42"), Times.Once);
}
```

### Testing Variables

```csharp
[Test]
public void Let_AssignsVariable()
{
    // Arrange
    var interpreter = CreateInterpreter();
    
    // Act
    interpreter.Execute("10 X = 42\n20 PRINT X");
    
    // Assert
    // Verify output shows 42
    mockIO.Verify(io => io.WriteLine("42"), Times.Once);
}
```

### Testing Exceptions

```csharp
[Test]
public void Parse_InvalidSyntax_ThrowsException()
{
    // Arrange
    var parser = new BasicParser();
    var invalidTokens = GetInvalidTokens();
    
    // Act & Assert
    Assert.Throws<SyntaxErrorException>(() => parser.Parse(invalidTokens));
}
```

### Testing Floating-Point Values

```csharp
[Test]
public void Calculate_ReturnsApproximateValue()
{
    // Arrange
    const double expected = 3.14159;
    const double epsilon = 1e-5;
    
    // Act
    var result = Calculate();
    
    // Assert
    Assert.That(result, Is.EqualTo(expected).Within(epsilon));
}
```

## Mocking with Moq

### Setting Up Mocks

```csharp
// Create mock
var mockIO = new Mock<IInputOutput>();

// Setup method to return value
mockIO.Setup(io => io.ReadLine()).Returns("test");

// Setup method to return different values on successive calls
mockIO.SetupSequence(io => io.ReadLine())
    .Returns("first")
    .Returns("second")
    .Returns("third");

// Use mock
var interpreter = CreateInterpreter(mockIO.Object);
```

### Verifying Calls

```csharp
// Verify method was called once
mockIO.Verify(io => io.WriteLine("HELLO"), Times.Once);

// Verify method was never called
mockIO.Verify(io => io.WriteLine("GOODBYE"), Times.Never);

// Verify method was called at least once
mockIO.Verify(io => io.Write(It.IsAny<string>()), Times.AtLeastOnce);

// Verify with argument matcher
mockIO.Verify(io => io.WriteLine(It.Is<string>(s => s.Contains("ERROR"))));
```

### Argument Matchers

```csharp
// Any value of type
It.IsAny<string>()
It.IsAny<int>()

// Matching condition
It.Is<string>(s => s.Length > 5)
It.Is<int>(n => n > 0)

// Regex match
It.IsRegex("[0-9]+")
```

## NUnit Assertions

### Common Assertions

```csharp
// Equality
Assert.That(actual, Is.EqualTo(expected));
Assert.That(actual, Is.Not.EqualTo(unexpected));

// Null checks
Assert.That(value, Is.Null);
Assert.That(value, Is.Not.Null);

// Boolean
Assert.That(condition, Is.True);
Assert.That(condition, Is.False);

// Numeric comparisons
Assert.That(value, Is.GreaterThan(5));
Assert.That(value, Is.LessThan(10));
Assert.That(value, Is.InRange(5, 10));

// String assertions
Assert.That(text, Is.Empty);
Assert.That(text, Does.Contain("substring"));
Assert.That(text, Does.StartWith("prefix"));
Assert.That(text, Does.EndWith("suffix"));
Assert.That(text, Is.EqualTo("expected").IgnoreCase);

// Collection assertions
Assert.That(collection, Is.Empty);
Assert.That(collection, Has.Count.EqualTo(5));
Assert.That(collection, Does.Contain(item));
Assert.That(collection, Has.Member(item));

// Type checks
Assert.That(obj, Is.InstanceOf<ExpectedType>());
Assert.That(obj, Is.AssignableTo<BaseType>());

// Exception assertions
Assert.Throws<ExceptionType>(() => MethodThatThrows());
Assert.DoesNotThrow(() => SafeMethod());
```

## Test Fixtures

### Multiple Tests with Same Setup

```csharp
[TestFixture]
public class CalculatorTests
{
    private Calculator _calculator;
    
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Runs once before all tests in fixture
    }
    
    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        // Runs once after all tests in fixture
    }
    
    [SetUp]
    public void SetUp()
    {
        // Runs before each test
        _calculator = new Calculator();
    }
    
    [TearDown]
    public void TearDown()
    {
        // Runs after each test
        _calculator = null;
    }
    
    [Test]
    public void Add_TwoNumbers_ReturnsSum()
    {
        Assert.That(_calculator.Add(2, 3), Is.EqualTo(5));
    }
}
```

### Parameterized Tests

```csharp
[Test]
[TestCase(0, 0, 0)]
[TestCase(1, 2, 3)]
[TestCase(-1, 1, 0)]
[TestCase(100, 200, 300)]
public void Add_VariousInputs_ReturnsCorrectSum(int a, int b, int expected)
{
    var result = _calculator.Add(a, b);
    Assert.That(result, Is.EqualTo(expected));
}
```

### Test Categories

```csharp
[Test]
[Category("Parser")]
public void Parser_Test()
{
    // Parser test
}

[Test]
[Category("Integration")]
public void IntegrationTest()
{
    // Integration test
}

// Run only Parser tests:
// dotnet test --filter Category=Parser
```

## Integration Tests

Test complete BASIC programs:

```csharp
[Test]
public void ExecuteProgram_Fibonacci_ProducesCorrectSequence()
{
    // Arrange
    var mockIO = new Mock<IInputOutput>();
    mockIO.Setup(io => io.ReadLine()).Returns("10");
    
    var output = new List<string>();
    mockIO.Setup(io => io.Write(It.IsAny<string>()))
        .Callback<string>(s => output.Add(s));
    
    var interpreter = CreateInterpreter(mockIO.Object);
    
    var program = @"
10 INPUT N
20 A = 0: B = 1
30 FOR I = 1 TO N
40 PRINT A;
50 C = A + B
60 A = B: B = C
70 NEXT I
";
    
    // Act
    interpreter.Execute(program);
    
    // Assert
    var sequence = string.Join("", output);
    Assert.That(sequence, Does.Contain("0"));
    Assert.That(sequence, Does.Contain("1"));
    Assert.That(sequence, Does.Contain("1"));
    Assert.That(sequence, Does.Contain("2"));
}
```

## Code Coverage

### Generating Coverage Reports

```bash
# Install coverage tool
dotnet tool install -g dotnet-coverage

# Run tests with coverage
dotnet test BackPocketBasic.slnx --collect:"XPlat Code Coverage"

# Coverage report will be in TestResults folder
```

### Viewing Coverage

**Visual Studio:**
- **Test → Analyze Code Coverage → All Tests**

**VS Code:**
- Use Coverage Gutters extension

**Rider:**
- Built-in coverage analysis

## Best Practices

### Do's ✅

- **Write tests first** (TDD when possible)
- **One assertion per test** (logical unit)
- **Use descriptive names**
- **Keep tests independent**
- **Test edge cases**
- **Mock external dependencies**
- **Use SetUp/TearDown for common code**
- **Test both success and failure paths**

### Don'ts ❌

- **Don't test implementation details**
- **Don't share state between tests**
- **Don't use Thread.Sleep** (makes tests slow/flaky)
- **Don't test private methods directly**
- **Don't ignore failing tests**
- **Don't write tests without assertions**

## Test Examples

See these test files for examples:

- `InterpreterTests.cs` - High-level interpreter tests
- `LexerTests.cs` - Tokenization examples
- `ParserTests.cs` - Parsing examples
- `EmulationTests.cs` - Hardware emulation tests

## Continuous Integration

Tests run automatically on:
- Every push to a branch
- Every pull request
- See `.github/workflows/ci.yml`

All tests must pass before merging.

## Troubleshooting

### Tests Pass Locally but Fail in CI

**Possible causes:**
- Line ending differences (CRLF vs LF)
- Platform-specific behavior
- Timezone differences
- File path differences

**Solutions:**
- Use `.gitattributes` for line endings
- Write platform-agnostic tests
- Mock time-dependent code

### Flaky Tests

Tests that sometimes pass, sometimes fail:

**Common causes:**
- Race conditions
- Timing dependencies
- Shared state
- External dependencies

**Solutions:**
- Remove `Thread.Sleep`
- Use mocks for external services
- Ensure test independence

## Related Topics

- **[Development Setup](Development-Setup)** - Environment setup
- **[Code Style](Code-Style)** - Coding standards
- **[Architecture Overview](Architecture-Overview)** - System design
- **[Contributing](https://github.com/jpactor/back-pocket-basic/blob/main/CONTRIBUTING.md)** - Contribution guidelines

## External Resources

- [NUnit Documentation](https://docs.nunit.org/)
- [Moq Quickstart](https://github.com/moq/moq4/wiki/Quickstart)
- [Testing Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
