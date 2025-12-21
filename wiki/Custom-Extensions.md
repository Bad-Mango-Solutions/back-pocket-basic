# Custom Extensions

Modern extensions to Applesoft BASIC provided by this interpreter.

## Overview

While maintaining compatibility with classic Applesoft BASIC, this interpreter includes several modern extensions that enhance functionality without breaking existing programs.

## SLEEP Command

The `SLEEP` command pauses program execution for a specified duration.

### Syntax

```basic
SLEEP milliseconds
```

### Parameters

- **milliseconds**: Number of milliseconds to pause (integer, 0 or greater)

### Examples

#### Basic Usage

```basic
10 PRINT "STARTING..."
20 SLEEP 1000
30 PRINT "ONE SECOND LATER"
```

#### Countdown Timer

```basic
10 HOME
20 FOR I = 10 TO 1 STEP -1
30 PRINT I
40 SLEEP 1000
50 NEXT I
60 PRINT "BLAST OFF!"
```

#### Animation Effect

```basic
10 HOME
20 FOR I = 1 TO 20
30 HTAB I: PRINT "*"
40 SLEEP 100
50 HOME
60 NEXT I
```

#### Progress Indicator

```basic
10 PRINT "PROCESSING";
20 FOR I = 1 TO 10
30 PRINT ".";
40 SLEEP 500
50 NEXT I
60 PRINT " DONE!"
```

### Use Cases

1. **Pacing Output**: Slow down program output for readability
2. **Animation**: Create time-based animations
3. **User Experience**: Add deliberate pauses for effect
4. **Simulations**: Model time-based processes
5. **Debugging**: Add pauses to observe program state

### Notes

- Duration is specified in milliseconds (1000 ms = 1 second)
- Execution blocks for the specified duration
- Does not consume significant CPU during sleep
- Minimum practical sleep is ~15-20ms on most systems

### Complete Example

See `samples/sleep.bas` for a full demonstration:

```bash
dotnet run --project src/BadMango.Basic.Console/BadMango.Basic.Console.csproj -- samples/sleep.bas
```

**samples/sleep.bas:**
```basic
10 REM SLEEP COMMAND DEMONSTRATION
20 HOME
30 PRINT "COUNTDOWN FROM 5..."
40 PRINT
50 FOR I = 5 TO 1 STEP -1
60 PRINT I; "...";
70 SLEEP 1000
80 NEXT I
90 PRINT
100 PRINT
110 PRINT "BLAST OFF!"
```

## Why Extensions?

### Maintaining Compatibility

The interpreter maintains full backward compatibility with classic Applesoft BASIC. All extensions:

- Use new command names that don't conflict with original syntax
- Are optional - classic programs work without modification
- Preserve original behavior of all standard commands

### Modern Development

Modern developers expect certain capabilities:

1. **Time Control**: SLEEP provides essential timing capabilities
2. **Debugging**: Helps with testing and development
3. **User Experience**: Enables better program pacing

### Future Extensions

Potential future extensions under consideration:

#### File I/O

```basic
OPEN "filename", mode
CLOSE file_number
PRINT# file_number, data
INPUT# file_number, variable
```

#### Enhanced String Functions

```basic
UCASE$(string)    : REM Convert to uppercase
LCASE$(string)    : REM Convert to lowercase
TRIM$(string)     : REM Remove leading/trailing spaces
INSTR(str$, sub$) : REM Find substring position
```

#### Date/Time Functions

```basic
DATE$             : REM Current date
TIME$             : REM Current time
TIMER             : REM Milliseconds since midnight
```

#### Extended Math

```basic
MIN(a, b)         : REM Minimum of two values
MAX(a, b)         : REM Maximum of two values
ROUND(x, digits)  : REM Round to decimal places
```

#### System Information

```basic
SYSTEM$           : REM Operating system name
VERSION$          : REM Interpreter version
```

## Extension Guidelines

If you're considering contributing an extension:

### 1. Don't Break Compatibility

- Never modify existing command behavior
- Use new command names
- Test with classic programs

### 2. Use Appropriate Names

- Choose names that wouldn't exist in classic BASIC
- Avoid single-letter commands
- Use descriptive names (e.g., `SLEEP` not `S`)

### 3. Match Applesoft Style

- Follow Applesoft syntax conventions
- Use similar parameter styles
- Maintain consistent error handling

### 4. Document Thoroughly

- Explain purpose and use cases
- Provide multiple examples
- Note any platform-specific behavior

### 5. Add Tests

- Unit tests for the extension
- Integration tests with sample programs
- Test edge cases and error conditions

## Implementation Details

### SLEEP Implementation

The SLEEP command is implemented in the interpreter at:
- Command parsing: `src/BadMango.Basic/Parser/BasicParser.cs`
- Execution: `src/BadMango.Basic/Execution/BasicInterpreter.cs`

The implementation uses `Thread.Sleep()` for cross-platform compatibility.

### Adding New Extensions

To add a new extension:

1. **Add Token** in `src/BadMango.Basic/Tokens/TokenType.cs`
2. **Add AST Node** in `src/BadMango.Basic/AST/`
3. **Update Lexer** in `src/BadMango.Basic/Lexer/BasicLexer.cs`
4. **Update Parser** in `src/BadMango.Basic/Parser/BasicParser.cs`
5. **Implement Visitor** in `src/BadMango.Basic/Execution/BasicInterpreter.cs`
6. **Add Tests** in `tests/BadMango.Basic.Tests/`
7. **Update Documentation** in `README.md` and wiki

See [Contributing Guide](https://github.com/Bad-Mango-Solutions/back-pocket-basic/blob/main/CONTRIBUTING.md) for detailed steps.

## Requesting Extensions

Have an idea for an extension?

1. **Check Compatibility**: Ensure it doesn't conflict with Applesoft BASIC
2. **Open an Issue**: Use the [Feature Request template](https://github.com/Bad-Mango-Solutions/back-pocket-basic/issues/new?template=feature_request.md)
3. **Explain Use Case**: Describe why the extension would be valuable
4. **Provide Examples**: Show how it would be used

## Extension Philosophy

### Core Principles

1. **Preserve History**: Original Applesoft BASIC behavior is sacred
2. **Add Value**: Extensions should solve real problems
3. **Stay Minimal**: Don't bloat the language unnecessarily
4. **Be Practical**: Focus on useful, implementable features

### What Makes a Good Extension?

✅ **Good Extensions:**
- Solve common problems (like SLEEP for timing)
- Are easy to understand and use
- Have clear, unambiguous syntax
- Work across platforms
- Don't duplicate existing functionality

❌ **Avoid:**
- Features that change existing behavior
- Platform-specific commands (unless clearly marked)
- Overly complex syntax
- Features that can be easily implemented in BASIC itself

## Compatibility Mode

### Future Feature

A potential future feature is a strict compatibility mode:

```basic
10 COMPAT "APPLESOFT"   : REM DISABLE EXTENSIONS
20 SLEEP 1000           : REM WOULD CAUSE ERROR
```

This would allow testing programs for strict Applesoft BASIC compatibility.

## Related Topics

- **[Language Reference](Language-Reference)** - Standard Applesoft BASIC commands
- **[Architecture Overview](Architecture-Overview)** - How extensions are implemented
- **[Development Setup](Development-Setup)** - Contributing extensions
- **[Sample Programs](Sample-Programs)** - See extensions in use

## Summary

The interpreter includes carefully chosen extensions that enhance functionality while preserving the classic Applesoft BASIC experience. The SLEEP command demonstrates how modern capabilities can be added without compromising compatibility.

For more information on contributing extensions, see the [Contributing Guide](https://github.com/Bad-Mango-Solutions/back-pocket-basic/blob/main/CONTRIBUTING.md).
