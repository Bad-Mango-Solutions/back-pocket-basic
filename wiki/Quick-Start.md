# Quick Start

Get started with BackPocketBASIC in just a few minutes!

## Prerequisites

Make sure you've completed the [Installation](Installation) steps first.

## Running Your First Program

### Hello World

Create a new file called `hello.bas`:

```basic
10 REM HELLO WORLD PROGRAM
20 HOME
30 PRINT "HELLO, WORLD!"
40 END
```

Run it:

```bash
dotnet run --project src/BadMango.Basic.Console/BadMango.Basic.Console.csproj -- hello.bas
```

**Output:**
```
HELLO, WORLD!
```

### Using the Console Application

The general syntax is:

```bash
dotnet run --project src/BadMango.Basic.Console/BadMango.Basic.Console.csproj -- <basic-file>
```

Or after building, use the executable directly:

```bash
./src/BadMango.Basic.Console/bin/Debug/net10.0/bpbasic <basic-file>
```

For the release build:

```bash
./src/BadMango.Basic.Console/bin/Release/net10.0/bpbasic <basic-file>
```

## Try the Sample Programs

The `samples/` directory includes several example programs:

### Demo Program

A comprehensive demonstration of language features:

```bash
dotnet run --project src/BadMango.Basic.Console/BadMango.Basic.Console.csproj -- samples/demo.bas
```

### Prime Number Finder

```bash
dotnet run --project src/BadMango.Basic.Console/BadMango.Basic.Console.csproj -- samples/primes.bas
```

### Fibonacci Sequence

```bash
dotnet run --project src/BadMango.Basic.Console/BadMango.Basic.Console.csproj -- samples/fibonacci.bas
```

### Memory Operations (PEEK/POKE)

```bash
dotnet run --project src/BadMango.Basic.Console/BadMango.Basic.Console.csproj -- samples/memory.bas
```

### SLEEP Command Demo

```bash
dotnet run --project src/BadMango.Basic.Console/BadMango.Basic.Console.csproj -- samples/sleep.bas
```

## Basic BASIC Concepts

### Line Numbers

Every BASIC statement starts with a line number:

```basic
10 PRINT "LINE 10"
20 PRINT "LINE 20"
30 PRINT "LINE 30"
```

Lines are executed in numerical order (not the order you type them).

### Variables

Three types of variables:

```basic
10 REM NUMERIC VARIABLE
20 N = 42

30 REM STRING VARIABLE
40 S$ = "HELLO"

50 REM INTEGER VARIABLE
60 I% = 100
```

### Simple Input and Output

```basic
10 REM GET INPUT
20 INPUT "WHAT IS YOUR NAME"; N$
30 PRINT "HELLO, "; N$

40 REM SIMPLE MATH
50 INPUT "ENTER A NUMBER"; X
60 PRINT "DOUBLE IS"; X * 2
```

### Loops

```basic
10 REM COUNT TO 10
20 FOR I = 1 TO 10
30 PRINT I
40 NEXT I
```

## Common BASIC Programs

### Simple Calculator

Create `calculator.bas`:

```basic
10 REM SIMPLE CALCULATOR
20 INPUT "ENTER FIRST NUMBER: "; A
30 INPUT "ENTER SECOND NUMBER: "; B
40 PRINT "SUM:"; A + B
50 PRINT "DIFFERENCE:"; A - B
60 PRINT "PRODUCT:"; A * B
70 PRINT "QUOTIENT:"; A / B
80 END
```

### Factorial Calculator

Create `factorial.bas`:

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

### Number Guessing Game

Create `guess.bas`:

```basic
10 REM NUMBER GUESSING GAME
20 SECRET = INT(RND(1) * 100) + 1
30 TRIES = 0
40 PRINT "I'M THINKING OF A NUMBER 1-100"
50 INPUT "YOUR GUESS: "; G
60 TRIES = TRIES + 1
70 IF G = SECRET THEN GOTO 120
80 IF G < SECRET THEN PRINT "TOO LOW!"
90 IF G > SECRET THEN PRINT "TOO HIGH!"
100 GOTO 50
120 PRINT "CORRECT! YOU GOT IT IN"; TRIES; "TRIES!"
130 END
```

### User-Defined Functions

Create `functions.bas`:

```basic
10 REM USER-DEFINED FUNCTIONS
20 DEF FN SQUARE(X) = X * X
30 DEF FN CUBE(X) = X * X * X
40 DEF FN AREA(R) = 3.14159 * R * R
50 FOR I = 1 TO 5
60 PRINT I; "SQUARED ="; FN SQUARE(I)
70 NEXT I
80 PRINT "AREA OF CIRCLE R=5:"; FN AREA(5)
90 END
```

## Understanding Output

The interpreter produces output in several ways:

### Standard Output

BASIC `PRINT` statements appear on standard output:

```basic
10 PRINT "THIS APPEARS ON STDOUT"
```

### Error Messages

Errors appear on standard error:

```
ERROR: SYNTAX ERROR IN LINE 10
ERROR: UNDEFINED VARIABLE 'X' IN LINE 20
```

### Logs

Detailed logs are written to `logs/applesoft-<date>.log`:
- Debug information
- Execution traces
- Performance data

Console shows only warnings and errors by default.

## Tips for Getting Started

### 1. Use Line Numbers

Always start lines with numbers. Common practice is to increment by 10:

```basic
10 REM FIRST LINE
20 REM SECOND LINE
30 REM THIRD LINE
```

This allows you to insert lines later (e.g., line 15, 25).

### 2. Use REM for Comments

```basic
10 REM THIS IS A COMMENT
20 REM COMMENTS EXPLAIN YOUR CODE
```

### 3. Clear the Screen

Use `HOME` to clear the screen:

```basic
10 HOME
20 PRINT "SCREEN IS NOW CLEAR"
```

### 4. End Programs with END

```basic
90 END
```

This explicitly marks the end of your program.

### 5. Test Incrementally

Start small and test often:

```basic
10 PRINT "TESTING"
20 END
```

Then add more features gradually.

## Common Mistakes

### Missing Line Numbers

❌ Wrong:
```basic
PRINT "HELLO"
```

✅ Correct:
```basic
10 PRINT "HELLO"
```

### Wrong Variable Type

❌ Wrong:
```basic
10 N$ = 42          : REM STRING VAR WITH NUMBER
20 X = "HELLO"      : REM NUMBER VAR WITH STRING
```

✅ Correct:
```basic
10 N$ = "HELLO"     : REM STRING VAR WITH STRING
20 X = 42           : REM NUMBER VAR WITH NUMBER
```

### Forgetting TO in FOR Loops

❌ Wrong:
```basic
10 FOR I = 1 10
```

✅ Correct:
```basic
10 FOR I = 1 TO 10
```

### Missing NEXT

❌ Wrong:
```basic
10 FOR I = 1 TO 10
20 PRINT I
30 END
```

✅ Correct:
```basic
10 FOR I = 1 TO 10
20 PRINT I
30 NEXT I
40 END
```

## Next Steps

Now that you know the basics:

1. **[Language Reference](Language-Reference)** - Learn all available commands
2. **[Built-in Functions](Built-in-Functions)** - Explore functions like SIN, COS, LEN
3. **[Sample Programs](Sample-Programs)** - Study more complex examples
4. **[Custom Extensions](Custom-Extensions)** - Learn about SLEEP and other modern additions

## Getting Help

- **[Language Reference](Language-Reference)** - Complete command documentation
- **[GitHub Issues](https://github.com/Bad-Mango-Solutions/back-pocket-basic/issues)** - Report bugs or ask questions
- **[Contributing Guide](https://github.com/Bad-Mango-Solutions/back-pocket-basic/blob/main/CONTRIBUTING.md)** - Learn how to contribute

## External Resources

- [Applesoft BASIC Quick Reference](http://www.landsnail.com/a2ref.htm)
- [Apple II Documentation Project](https://www.apple2.org/)
- [Vintage BASIC Games](http://www.atariarchives.org/basicgames/)
