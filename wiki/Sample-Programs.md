# Sample Programs

Guided walkthroughs of the sample programs included in the `samples/` directory.

## Overview

The repository includes several sample programs that demonstrate various features of Applesoft BASIC. Each program is documented here with explanations of key concepts.

## Running Samples

To run any sample program:

```bash
dotnet run --project src/BadMango.Basic.Console/BadMango.Basic.Console.csproj -- samples/<filename>.bas
```

Or with the built executable:

```bash
./src/BadMango.Basic.Console/bin/Debug/net10.0/bpbasic samples/<filename>.bas
```

## Available Samples

| File | Description | Key Features |
|------|-------------|--------------|
| `demo.bas` | Comprehensive feature demo | Variables, loops, functions, arrays |
| `primes.bas` | Prime number finder | Algorithms, nested loops |
| `fibonacci.bas` | Fibonacci sequence | Recursion concepts, loops |
| `memory.bas` | PEEK/POKE demonstration | Memory operations, emulation |
| `sleep.bas` | SLEEP command demo | Custom extensions, timing |
| `speaker.bas` | Speaker/sound demo | Hardware emulation, timing |

---

## demo.bas - Feature Demonstration

A comprehensive demonstration of Applesoft BASIC features.

### What It Demonstrates

- Variable types (numeric, string, integer)
- Arithmetic operations
- String operations
- Arrays (single and multi-dimensional)
- Loops (FOR...NEXT)
- Conditional statements (IF...THEN)
- User-defined functions (DEF FN)
- Subroutines (GOSUB/RETURN)
- Text display commands

### Key Code Sections

#### Variable Types

```basic
10 REM NUMERIC VARIABLES
20 X = 42
30 Y = 3.14159

40 REM STRING VARIABLES
50 NAME$ = "APPLESOFT"

60 REM INTEGER VARIABLES
70 COUNT% = 100
```

#### User-Defined Functions

```basic
100 DEF FN SQUARE(N) = N * N
110 DEF FN CUBE(N) = N * N * N
120 PRINT FN SQUARE(5), FN CUBE(3)
```

#### Arrays

```basic
200 DIM NUMBERS(10)
210 FOR I = 0 TO 10
220 NUMBERS(I) = I * I
230 NEXT I
```

### Learning Objectives

- Understand variable naming conventions
- See how different data types work
- Learn array declaration and access
- Practice with loops and conditionals

---

## primes.bas - Prime Number Finder

Finds and displays prime numbers within a given range.

### Algorithm Overview

The program uses trial division to test primality:

1. For each number n in range
2. Test if n is divisible by any number from 2 to √n
3. If no divisors found, n is prime

### Key Code

```basic
10 REM PRIME NUMBER FINDER
20 INPUT "FIND PRIMES UP TO: "; LIMIT
30 PRINT "2";
40 FOR N = 3 TO LIMIT STEP 2
50   ISPRIME = 1
60   FOR D = 3 TO INT(SQR(N)) STEP 2
70     IF N / D = INT(N / D) THEN ISPRIME = 0: GOTO 100
80   NEXT D
100  IF ISPRIME THEN PRINT N;
110 NEXT N
```

### Key Concepts

**Trial Division:**
- Only test odd numbers (after 2)
- Only test up to square root of n
- Skip even divisors

**Performance Optimization:**
- `STEP 2` to skip even numbers
- Early exit when divisor found
- Testing only to √n reduces checks

**Mathematical Insight:**
If n is not prime, it has at least one divisor ≤ √n.

### Learning Objectives

- Understand algorithm optimization
- Learn nested loop patterns
- Practice with mathematical operations
- See efficiency techniques in BASIC

### Sample Output

```
FIND PRIMES UP TO: 50
2 3 5 7 11 13 17 19 23 29 31 37 41 43 47
```

---

## fibonacci.bas - Fibonacci Sequence

Generates the Fibonacci sequence where each number is the sum of the two preceding ones.

### Mathematical Background

The Fibonacci sequence: 0, 1, 1, 2, 3, 5, 8, 13, 21, 34, ...

- F(0) = 0
- F(1) = 1
- F(n) = F(n-1) + F(n-2)

### Key Code

```basic
10 REM FIBONACCI SEQUENCE
20 INPUT "HOW MANY TERMS: "; N
30 A = 0: B = 1
40 PRINT A; B;
50 FOR I = 3 TO N
60   C = A + B
70   PRINT C;
80   A = B
90   B = C
100 NEXT I
```

### Algorithm Explanation

**Iterative Approach:**

1. Start with F(0)=0, F(1)=1
2. For each new term:
   - Add previous two terms
   - Shift values: previous becomes current, current becomes previous
3. Repeat for desired count

**Why Iterative vs Recursive:**

This implementation uses iteration rather than recursion because:
- More efficient (O(n) instead of O(2^n))
- Avoids stack overflow for large n
- Clearer in BASIC

### Learning Objectives

- Understand the Fibonacci sequence
- Learn the "sliding window" pattern
- See efficient sequence generation
- Practice variable swapping

### Sample Output

```
HOW MANY TERMS: 10
0 1 1 2 3 5 8 13 21 34
```

### Interesting Facts

- Appears frequently in nature (flower petals, spiral shells)
- Ratio of consecutive terms approaches golden ratio (φ ≈ 1.618)
- Used in computer science for algorithm analysis

---

## memory.bas - PEEK/POKE Demonstration

Demonstrates direct memory access using PEEK and POKE commands.

### What It Demonstrates

- Writing to memory with POKE
- Reading from memory with PEEK
- Memory address ranges
- Emulated memory space

### Key Code

```basic
10 REM MEMORY DEMONSTRATION
20 HOME
30 PRINT "POKE/PEEK DEMONSTRATION"
40 PRINT

50 REM WRITE VALUES TO MEMORY
60 FOR I = 0 TO 9
70   POKE 768 + I, I * 10
80 NEXT I

90 REM READ VALUES BACK
100 PRINT "MEMORY AT 768-777:"
110 FOR I = 0 TO 9
120   V = PEEK(768 + I)
130   PRINT "ADDRESS "; 768 + I; ": "; V
140 NEXT I
```

### Memory Concepts

**Page 3 ($300-$3FF):**
- Addresses 768-1023
- Traditionally used for machine code routines
- Safe area for user programs

**Memory Safety:**
- This interpreter uses emulated memory
- Won't affect your actual computer's memory
- Bounds checking prevents invalid access

### Learning Objectives

- Understand memory addressing
- Learn PEEK/POKE syntax
- See hexadecimal addressing
- Practice with memory operations

### Sample Output

```
POKE/PEEK DEMONSTRATION

MEMORY AT 768-777:
ADDRESS 768: 0
ADDRESS 769: 10
ADDRESS 770: 20
ADDRESS 771: 30
ADDRESS 772: 40
ADDRESS 773: 50
ADDRESS 774: 60
ADDRESS 775: 70
ADDRESS 776: 80
ADDRESS 777: 90
```

### Advanced Usage

**Storing Strings in Memory:**
```basic
10 S$ = "HELLO"
20 FOR I = 1 TO LEN(S$)
30   POKE 768 + I, ASC(MID$(S$, I, 1))
40 NEXT I
```

**Reading Back:**
```basic
50 FOR I = 1 TO 5
60   C = PEEK(768 + I)
70   PRINT CHR$(C);
80 NEXT I
```

---

## sleep.bas - SLEEP Command Demo

Demonstrates the SLEEP extension command for timing control.

### What It Demonstrates

- SLEEP command syntax
- Countdown timer implementation
- Pacing program output
- Time-based animation

### Key Code

```basic
10 REM SLEEP COMMAND DEMONSTRATION
20 HOME
30 PRINT "COUNTDOWN FROM 5..."
40 PRINT
50 FOR I = 5 TO 1 STEP -1
60   PRINT I; "...";
70   SLEEP 1000
80 NEXT I
90 PRINT
100 PRINT
110 PRINT "BLAST OFF!"
```

### Timing Concepts

**Milliseconds:**
- 1000 ms = 1 second
- SLEEP 500 = half second
- SLEEP 100 = one tenth second

**Use Cases:**
- Countdown timers
- Animation frames
- Pacing output for readability
- Simulating time-based processes

### Learning Objectives

- Use SLEEP for timing
- Create countdown effects
- Control program pacing
- Enhance user experience

### Sample Output

```
COUNTDOWN FROM 5...

5 ... 4 ... 3 ... 2 ... 1 ...

BLAST OFF!
```

(Each number appears with a 1-second delay)

---

## speaker.bas - Speaker Demonstration

Demonstrates the Apple II speaker emulation with timed beeps.

### What It Demonstrates

- PEEK from speaker location ($C030)
- Hardware emulation
- Timing with SLEEP
- Sound generation principles

### Key Concepts

**Apple II Speaker:**
- Located at memory address $C030 (49200 decimal)
- Any read or write toggles speaker
- Rapid toggling creates tones
- Timing determines pitch

**Emulation:**
- This interpreter emulates the speaker
- Uses system beep on supported platforms
- Demonstrates hardware memory mapping

### Learning Objectives

- Understand memory-mapped I/O
- See hardware emulation in action
- Learn about Apple II architecture
- Practice combining PEEK and SLEEP

---

## Creating Your Own Programs

### Tips for Writing Sample Programs

1. **Start Simple**: Begin with basic features
2. **Add Comments**: Use REM to explain your code
3. **Test Incrementally**: Add features gradually
4. **Use Meaningful Names**: Even with 2-character limit, choose well
5. **Include Output**: Make results visible

### Program Structure Template

```basic
10 REM PROGRAM NAME AND PURPOSE
20 REM AUTHOR: YOUR NAME
30 REM DATE: MM/DD/YYYY
40 REM
50 REM INITIALIZE
60 HOME
70 GOSUB 1000: REM SETUP
80 REM
90 REM MAIN PROGRAM
100 GOSUB 2000: REM MAIN LOGIC
110 END
120 REM
1000 REM SUBROUTINE: SETUP
1010 REM ... initialization code ...
1090 RETURN
2000 REM SUBROUTINE: MAIN LOGIC
2010 REM ... main code ...
2090 RETURN
```

### Sample Ideas

- **Math Programs**: Calculators, converters, equation solvers
- **Games**: Number guessing, text adventures, puzzles
- **Utilities**: Text formatters, data processors
- **Simulations**: Physics, biology, economics
- **Art**: ASCII art generators, pattern makers
- **Education**: Quizzes, tutorials, demonstrations

---

## Next Steps

- **[Language Reference](Language-Reference)** - Learn all commands
- **[Built-in Functions](Built-in-Functions)** - Explore functions
- **[Custom Extensions](Custom-Extensions)** - Modern additions
- **[Contributing](https://github.com/jpactor/applesoft-basic/blob/main/CONTRIBUTING.md)** - Share your programs

## External Resources

- [Vintage BASIC Games](http://www.atariarchives.org/basicgames/) - Classic game programs
- [Apple II Documentation](https://www.apple2.org/) - Historical programs
- [Creative Computing](https://www.atariarchives.org/) - Program archives
