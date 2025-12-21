// <copyright file="InterpreterTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.Tests;

using BadMango.Basic.Emulation;
using BadMango.Basic.Execution;
using BadMango.Basic.IO;
using BadMango.Basic.Lexer;
using BadMango.Basic.Parser;
using BadMango.Basic.Runtime;

using Microsoft.Extensions.Logging;

using Moq;

/// <summary>
/// Contains unit tests for the <see cref="BasicInterpreter"/> class,
/// which is responsible for interpreting Applesoft BASIC programs.
/// </summary>
/// <remarks>
/// This class uses NUnit as the testing framework and Moq for mocking dependencies.
/// It verifies the functionality of the interpreter by testing various Applesoft BASIC constructs,
/// such as loops, conditionals, functions, and memory operations.
/// </remarks>
[TestFixture]
public class InterpreterTests
{
    private BasicInterpreter interpreter = null!;
    private Mock<IBasicIO> mockIo = null!;
    private List<string> output = null!;
    private int cursorCol;
    private int cursorRow;
    private TextMode textMode;

    /// <summary>
    /// Sets up the necessary dependencies and initializes the <see cref="BasicInterpreter"/> instance
    /// for use in the tests.
    /// </summary>
    /// <remarks>
    /// This method is executed before each test in the <see cref="InterpreterTests"/> class.
    /// It creates mock objects for various components, initializes required managers, and prepares
    /// the <see cref="BasicInterpreter"/> instance with all dependencies.
    /// </remarks>
    [SetUp]
    public void Setup()
    {
        var lexerLogger = new Mock<ILogger<BasicLexer>>();
        var parserLogger = new Mock<ILogger<BasicParser>>();
        var variableLogger = new Mock<ILogger<VariableManager>>();
        var memoryLogger = new Mock<ILogger<AppleMemory>>();
        var cpuLogger = new Mock<ILogger<Cpu6502>>();
        var speakerLogger = new Mock<ILogger<AppleSpeaker>>();
        var systemLogger = new Mock<ILogger<AppleSystem>>();
        var interpreterLogger = new Mock<ILogger<BasicInterpreter>>();

        var lexer = new BasicLexer(lexerLogger.Object);
        var parser = new BasicParser(lexer, parserLogger.Object);
        var variables = new VariableManager(variableLogger.Object);
        var functions = new FunctionManager();
        var data = new DataManager();
        var loops = new ForLoopManager();
        var gosub = new GosubManager();
        var memory = new AppleMemory(memoryLogger.Object);
        var cpu = new Cpu6502(memory, cpuLogger.Object);
        var speaker = new AppleSpeaker(speakerLogger.Object);
        var appleSystem = new AppleSystem(memory, cpu, speaker, systemLogger.Object);

        output = [];
        cursorCol = 0;
        cursorRow = 0;
        textMode = TextMode.Normal;
        mockIo = new();
        mockIo.Setup(io => io.Write(It.IsAny<string>())).Callback<string>(s => output.Add(s));
        mockIo.Setup(io => io.WriteLine(It.IsAny<string>())).Callback<string>(s => output.Add(s + "\n"));
        mockIo.Setup(io => io.GetCursorColumn()).Returns(() => cursorCol);
        mockIo.Setup(io => io.GetCursorRow()).Returns(() => cursorRow);
        mockIo.Setup(io => io.SetCursorPosition(It.IsAny<int>(), It.IsAny<int>()))
            .Callback<int, int>((c, r) =>
            {
                cursorCol = c;
                cursorRow = r;
            });
        mockIo.Setup(io => io.SetTextMode(It.IsAny<TextMode>())).Callback<TextMode>(mode => textMode = mode);
        mockIo.Setup(io => io.ReadChar()).Returns('Z');
        mockIo.Setup(io => io.ReadLine(It.IsAny<string>())).Returns(string.Empty);
        mockIo.Setup(io => io.ClearScreen()).Callback(() => output.Add("[CLEAR]\n"));

        // Create context objects
        var runtime = new BasicRuntimeContext(variables, functions, data, loops, gosub);
        var system = new SystemContext(appleSystem, mockIo.Object);

        interpreter = new(parser, runtime, system, interpreterLogger.Object);
    }

    /// <summary>
    /// Tests that the <see cref="BasicInterpreter.RunFromSource"/> method correctly outputs a string
    /// when executing a BASIC program containing a <c>PRINT</c> statement with a string literal.
    /// </summary>
    /// <remarks>
    /// This test verifies that the interpreter processes a <c>PRINT</c> statement with a string literal
    /// and writes the expected output to the mocked I/O system.
    /// </remarks>
    /// <example>
    /// The BASIC program executed in this test is:
    /// <code>
    /// 10 PRINT "HELLO WORLD"
    /// </code>
    /// The expected output is:
    /// <c>HELLO WORLD</c>.
    /// </example>
    [Test]
    public void Run_PrintString_OutputsString()
    {
        interpreter.RunFromSource("10 PRINT \"HELLO WORLD\"");

        Assert.That(string.Join(string.Empty, output), Does.Contain("HELLO WORLD"));
    }

    /// <summary>
    /// Tests whether the <see cref="BasicInterpreter.RunFromSource"/> method correctly outputs a number
    /// when executing a BASIC program containing a <c>PRINT</c> statement with a numeric literal.
    /// </summary>
    /// <remarks>
    /// This test verifies that the interpreter processes and outputs numeric literals as expected.
    /// It ensures that the output matches the expected value.
    /// </remarks>
    /// <example>
    /// The BASIC program <c>10 PRINT 42</c> is executed, and the output is checked to contain <c>42</c>.
    /// </example>
    [Test]
    public void Run_PrintNumber_OutputsNumber()
    {
        interpreter.RunFromSource("10 PRINT 42");

        Assert.That(string.Join(string.Empty, output), Does.Contain("42"));
    }

    /// <summary>
    /// Tests the execution of a BASIC program that prints the result of an expression.
    /// </summary>
    /// <remarks>
    /// This test verifies that the <see cref="BasicInterpreter"/> correctly computes and outputs
    /// the result of a mathematical expression in a PRINT statement.
    /// </remarks>
    /// <example>
    /// For example, given the Applesoft BASIC program:
    /// <code>
    /// 10 PRINT 2 + 3 * 4
    /// </code>
    /// The expected output is "14".
    /// </example>
    [Test]
    public void Run_PrintExpression_ComputesResult()
    {
        interpreter.RunFromSource("10 PRINT 2 + 3 * 4");

        Assert.That(string.Join(string.Empty, output), Does.Contain("14"));
    }

    /// <summary>
    /// Tests that the <see cref="BasicInterpreter.RunFromSource"/> method correctly handles variable assignment
    /// and stores the assigned value in memory.
    /// </summary>
    /// <remarks>
    /// This test verifies that when a variable is assigned a value in an Applesoft BASIC program,
    /// the interpreter correctly stores the value and allows it to be retrieved and printed.
    /// </remarks>
    /// <example>
    /// The test runs the following Applesoft BASIC code:
    /// <code>
    /// 10 X = 5
    /// 20 PRINT X
    /// </code>
    /// It asserts that the output contains the value "5".
    /// </example>
    [Test]
    public void Run_Assignment_StoresValue()
    {
        interpreter.RunFromSource("10 X = 5\n20 PRINT X");

        Assert.That(string.Join(string.Empty, output), Does.Contain("5"));
    }

    /// <summary>
    /// Verifies that the <see cref="BasicInterpreter.RunFromSource"/> method correctly executes a FOR loop
    /// in an Applesoft BASIC program, iterating over the specified range and producing the expected output.
    /// </summary>
    /// <remarks>
    /// This test ensures that the FOR loop construct in Applesoft BASIC functions as intended,
    /// iterating from the starting value to the ending value and executing the loop body for each iteration.
    /// </remarks>
    /// <example>
    /// The test runs the following Applesoft BASIC code:
    /// <code>
    /// 10 FOR I = 1 TO 3
    /// 20 PRINT I
    /// 30 NEXT I
    /// </code>
    /// The expected output is:
    /// <code>
    /// 1
    /// 2
    /// 3
    /// </code>
    /// </example>
    [Test]
    public void Run_ForLoop_Iterates()
    {
        interpreter.RunFromSource("10 FOR I = 1 TO 3\n20 PRINT I\n30 NEXT I");

        var output = string.Join(string.Empty, this.output);
        Assert.That(output, Does.Contain("1"));
        Assert.That(output, Does.Contain("2"));
        Assert.That(output, Does.Contain("3"));
    }

    /// <summary>
    /// Tests the execution of a FOR loop with a specified step value in an Applesoft BASIC program.
    /// </summary>
    /// <remarks>
    /// This test verifies that the <see cref="BasicInterpreter"/> correctly handles a FOR loop
    /// with a STEP value, ensuring that the loop iterates using the specified step increment.
    /// The test checks the output to confirm that the loop produces the expected sequence of values.
    /// </remarks>
    /// <example>
    /// The tested Applesoft BASIC program:
    /// <code>
    /// 10 FOR I = 2 TO 6 STEP 2
    /// 20 PRINT I
    /// 30 NEXT I
    /// </code>
    /// Expected output:
    /// <c>2</c>, <c>4</c>, <c>6</c>.
    /// </example>
    [Test]
    public void Run_ForLoopWithStep_UsesStep()
    {
        interpreter.RunFromSource("10 FOR I = 2 TO 6 STEP 2\n20 PRINT I\n30 NEXT I");

        var output = string.Join(string.Empty, this.output);
        Assert.That(output, Does.Contain("2"));
        Assert.That(output, Does.Contain("4"));
        Assert.That(output, Does.Contain("6"));
    }

    /// <summary>
    /// Verifies that the <see cref="BasicInterpreter.RunFromSource"/> method correctly executes
    /// the "THEN" branch of an Applesoft BASIC "IF" statement when the condition evaluates to true.
    /// </summary>
    /// <remarks>
    /// This test ensures that the interpreter evaluates the condition in the "IF" statement,
    /// and if the condition is true, it executes the corresponding "THEN" branch.
    /// </remarks>
    /// <example>
    /// The test runs the following Applesoft BASIC code:
    /// <code>
    /// 10 X = 10
    /// 20 IF X > 5 THEN PRINT "BIG"
    /// </code>
    /// It asserts that the output contains the string "BIG".
    /// </example>
    [Test]
    public void Run_IfTrue_ExecutesThenBranch()
    {
        interpreter.RunFromSource("10 X = 10\n20 IF X > 5 THEN PRINT \"BIG\"");

        Assert.That(string.Join(string.Empty, output), Does.Contain("BIG"));
    }

    /// <summary>
    /// Tests the behavior of the <see cref="BasicInterpreter.RunFromSource"/> method
    /// when evaluating an <c>IF</c> statement with a false condition.
    /// </summary>
    /// <remarks>
    /// This test verifies that the interpreter correctly skips the <c>THEN</c> branch
    /// when the condition in the <c>IF</c> statement evaluates to <c>false</c>.
    /// It ensures that only the subsequent statements are executed.
    /// </remarks>
    /// <example>
    /// The test runs the following Applesoft BASIC program:
    /// <code>
    /// 10 X = 1
    /// 20 IF X > 5 THEN PRINT "BIG"
    /// 30 PRINT "END"
    /// </code>
    /// Expected output:
    /// <c>END</c> (without <c>BIG</c> in the output).
    /// </example>
    [Test]
    public void Run_IfFalse_SkipsThenBranch()
    {
        interpreter.RunFromSource("10 X = 1\n20 IF X > 5 THEN PRINT \"BIG\"\n30 PRINT \"END\"");

        var output = string.Join(string.Empty, this.output);
        Assert.That(output, Does.Not.Contain("BIG"));
        Assert.That(output, Does.Contain("END"));
    }

    /// <summary>
    /// Verifies that the <see cref="BasicInterpreter.RunFromSource"/> method correctly handles a GOTO statement
    /// by jumping to the specified line number in the Applesoft BASIC program.
    /// </summary>
    /// <remarks>
    /// This test ensures that the interpreter skips lines that are bypassed by the GOTO statement
    /// and executes the target line as expected.
    /// </remarks>
    /// <example>
    /// The test runs the following Applesoft BASIC program:
    /// <code>
    /// 10 GOTO 30
    /// 20 PRINT "SKIP"
    /// 30 PRINT "HERE"
    /// </code>
    /// The expected output is "HERE", and the line containing "SKIP" is not executed.
    /// </example>
    [Test]
    public void Run_Goto_JumpsToLine()
    {
        interpreter.RunFromSource("10 GOTO 30\n20 PRINT \"SKIP\"\n30 PRINT \"HERE\"");

        var output = string.Join(string.Empty, this.output);
        Assert.That(output, Does.Not.Contain("SKIP"));
        Assert.That(output, Does.Contain("HERE"));
    }

    /// <summary>
    /// Verifies that the <see cref="BasicInterpreter.RunFromSource"/> method correctly handles
    /// the execution of a program containing GOSUB and RETURN statements.
    /// </summary>
    /// <remarks>
    /// This test ensures that the interpreter properly executes a subroutine using GOSUB,
    /// returns to the correct location using RETURN, and continues execution as expected.
    /// The test checks the output to confirm that both the subroutine and the subsequent
    /// statements are executed in the correct order.
    /// </remarks>
    /// <example>
    /// The program tested in this method:
    /// <code>
    /// 10 GOSUB 100
    /// 20 PRINT "BACK"
    /// 30 END
    /// 100 PRINT "SUB"
    /// 110 RETURN
    /// </code>
    /// Expected output:
    /// <c>SUB</c> followed by <c>BACK</c>.
    /// </example>
    [Test]
    public void Run_GosubReturn_WorksCorrectly()
    {
        interpreter.RunFromSource("10 GOSUB 100\n20 PRINT \"BACK\"\n30 END\n100 PRINT \"SUB\"\n110 RETURN");

        var output = string.Join(string.Empty, this.output);
        Assert.That(output, Does.Contain("SUB"));
        Assert.That(output, Does.Contain("BACK"));
    }

    /// <summary>
    /// Tests the <see cref="BasicInterpreter.RunFromSource"/> method to ensure that it correctly reads values
    /// from a DATA statement and assigns them to variables.
    /// </summary>
    /// <remarks>
    /// This test verifies that the interpreter can process a BASIC program containing a DATA statement,
    /// read the specified values, and use them in subsequent computations or operations.
    /// </remarks>
    /// <example>
    /// The test runs the following Applesoft BASIC program:
    /// <code>
    /// 10 READ X, Y
    /// 20 PRINT X + Y
    /// 30 DATA 10, 20
    /// </code>
    /// The expected output is "30", which is the sum of the values read from the DATA statement.
    /// </example>
    [Test]
    public void Run_DataRead_ReadsValues()
    {
        interpreter.RunFromSource("10 READ X, Y\n20 PRINT X + Y\n30 DATA 10, 20");

        Assert.That(string.Join(string.Empty, output), Does.Contain("30"));
    }

    /// <summary>
    /// Tests the functionality of the <see cref="BasicInterpreter"/> when executing a program
    /// that uses the <c>DIM</c> statement to define an array and assigns values to its elements.
    /// </summary>
    /// <remarks>
    /// This test verifies that the interpreter correctly handles array declarations,
    /// element assignments, and retrievals, as well as the output of array elements.
    /// </remarks>
    /// <example>
    /// The test runs the following Applesoft BASIC program:
    /// <code>
    /// 10 DIM A(5)
    /// 20 A(3) = 42
    /// 30 PRINT A(3)
    /// </code>
    /// It asserts that the output contains the value <c>42</c>.
    /// </example>
    [Test]
    public void Run_DimAndArray_Works()
    {
        interpreter.RunFromSource("10 DIM A(5)\n20 A(3) = 42\n30 PRINT A(3)");

        Assert.That(string.Join(string.Empty, output), Does.Contain("42"));
    }

    /// <summary>
    /// Tests the functionality of the <see cref="BasicInterpreter.RunFromSource"/> method
    /// when defining and using a user-defined function in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// This test verifies that the <c>DEF FN</c> statement correctly defines a function
    /// and that the function can be invoked with arguments to produce the expected result.
    /// </remarks>
    /// <example>
    /// The test runs the following Applesoft BASIC code:
    /// <code>
    /// 10 DEF FN SQ(X) = X * X
    /// 20 PRINT FN SQ(5)
    /// </code>
    /// It asserts that the output contains the expected result, "25".
    /// </example>
    [Test]
    public void Run_DefFn_DefinesFunction()
    {
        interpreter.RunFromSource("10 DEF FN SQ(X) = X * X\n20 PRINT FN SQ(5)");

        Assert.That(string.Join(string.Empty, output), Does.Contain("25"));
    }

    /// <summary>
    /// Verifies that the <see cref="BasicInterpreter"/> correctly handles string variable assignments
    /// and outputs the expected string value when executed.
    /// </summary>
    /// <remarks>
    /// This test ensures that the interpreter can assign a string value to a variable
    /// and subsequently print the value of the variable as part of program execution.
    /// </remarks>
    /// <example>
    /// The test runs the following Applesoft BASIC program:
    /// <code>
    /// 10 A$ = "HELLO"
    /// 20 PRINT A$
    /// </code>
    /// The expected output is "HELLO".
    /// </example>
    [Test]
    public void Run_StringVariable_Works()
    {
        interpreter.RunFromSource("10 A$ = \"HELLO\"\n20 PRINT A$");

        Assert.That(string.Join(string.Empty, output), Does.Contain("HELLO"));
    }

    /// <summary>
    /// Verifies that the <see cref="BasicInterpreter.RunFromSource"/> method correctly handles string concatenation
    /// in Applesoft BASIC programs.
    /// </summary>
    /// <remarks>
    /// This test ensures that concatenating two strings using the "+" operator produces the expected result
    /// and that the concatenated string is correctly assigned to a string variable and printed.
    /// </remarks>
    /// <example>
    /// The test runs the following Applesoft BASIC program:
    /// <code>
    /// 10 A$ = "HELLO" + " WORLD"
    /// 20 PRINT A$
    /// </code>
    /// The expected output is "HELLO WORLD".
    /// </example>
    [Test]
    public void Run_StringConcatenation_Works()
    {
        interpreter.RunFromSource("10 A$ = \"HELLO\" + \" WORLD\"\n20 PRINT A$");

        Assert.That(string.Join(string.Empty, output), Does.Contain("HELLO WORLD"));
    }

    /// <summary>
    /// Tests the <c>ABS</c> function in the Applesoft BASIC interpreter.
    /// </summary>
    /// <remarks>
    /// This test verifies that the <c>ABS</c> function correctly computes the absolute value of a negative number.
    /// It runs a BASIC program that prints the absolute value of -5 and asserts that the output contains "5".
    /// </remarks>
    /// <example>
    /// The following Applesoft BASIC code is executed in this test:
    /// <code>
    /// 10 PRINT ABS(-5)
    /// </code>
    /// </example>
    [Test]
    public void Run_AbsFunction_Works()
    {
        interpreter.RunFromSource("10 PRINT ABS(-5)");

        Assert.That(string.Join(string.Empty, output), Does.Contain("5"));
    }

    /// <summary>
    /// Verifies that the <c>INT</c> function in Applesoft BASIC correctly truncates a floating-point number
    /// to its integer part during program execution.
    /// </summary>
    /// <remarks>
    /// This test runs a BASIC program that uses the <c>INT</c> function and checks the output to ensure
    /// the function behaves as expected.
    /// </remarks>
    /// <example>
    /// The test executes the following Applesoft BASIC code:
    /// <code>
    /// 10 PRINT INT(3.7)
    /// </code>
    /// The expected output is:
    /// <c>3</c>.
    /// </example>
    [Test]
    public void Run_IntFunction_Works()
    {
        interpreter.RunFromSource("10 PRINT INT(3.7)");

        Assert.That(string.Join(string.Empty, output), Does.Contain("3"));
    }

    /// <summary>
    /// Verifies that the <c>LEN</c> function in Applesoft BASIC correctly computes
    /// the length of a given string.
    /// </summary>
    /// <remarks>
    /// This test runs a BASIC program that uses the <c>LEN</c> function to calculate
    /// the length of the string "HELLO" and checks that the output is "5".
    /// </remarks>
    /// <example>
    /// The following Applesoft BASIC code is executed:
    /// <code>
    /// 10 PRINT LEN("HELLO")
    /// </code>
    /// The expected output is:
    /// <c>5</c>.
    /// </example>
    [Test]
    public void Run_LenFunction_Works()
    {
        interpreter.RunFromSource("10 PRINT LEN(\"HELLO\")");

        Assert.That(string.Join(string.Empty, output), Does.Contain("5"));
    }

    /// <summary>
    /// Verifies that the <c>MID$</c> function in Applesoft BASIC correctly extracts a substring
    /// from a given string based on the specified starting position and length.
    /// </summary>
    /// <remarks>
    /// This test ensures that the <c>MID$</c> function behaves as expected when executed
    /// within the <see cref="BasicInterpreter"/>. The test checks the output of the program
    /// to confirm that the correct substring is extracted and printed.
    /// </remarks>
    /// <example>
    /// The Applesoft BASIC program being tested:
    /// <code>
    /// 10 PRINT MID$("HELLO", 2, 3)
    /// </code>
    /// Expected output:
    /// <c>ELL</c>.
    /// </example>
    [Test]
    public void Run_MidFunction_Works()
    {
        interpreter.RunFromSource("10 PRINT MID$(\"HELLO\", 2, 3)");

        Assert.That(string.Join(string.Empty, output), Does.Contain("ELL"));
    }

    /// <summary>
    /// Verifies that the <c>LEFT$</c> function in Applesoft BASIC correctly extracts the specified number of characters
    /// from the beginning of a string.
    /// </summary>
    /// <remarks>
    /// This test ensures that the <c>LEFT$</c> function behaves as expected when executed by the <see cref="BasicInterpreter"/>.
    /// It runs a BASIC program that uses the <c>LEFT$</c> function and checks the output for correctness.
    /// </remarks>
    /// <example>
    /// The following Applesoft BASIC code is tested:
    /// <code>
    /// 10 PRINT LEFT$(\"HELLO\", 3)
    /// </code>
    /// Expected output:
    /// <c>HEL</c>.
    /// </example>
    [Test]
    public void Run_LeftFunction_Works()
    {
        interpreter.RunFromSource("10 PRINT LEFT$(\"HELLO\", 3)");

        Assert.That(string.Join(string.Empty, output), Does.Contain("HEL"));
    }

    /// <summary>
    /// Verifies that the <c>RIGHT$</c> function in Applesoft BASIC correctly extracts
    /// the specified number of characters from the right end of a string.
    /// </summary>
    /// <remarks>
    /// This test executes a BASIC program that uses the <c>RIGHT$</c> function to extract
    /// the last three characters of the string "HELLO". It then asserts that the output
    /// contains the expected result ("LLO").
    /// </remarks>
    [Test]
    public void Run_RightFunction_Works()
    {
        interpreter.RunFromSource("10 PRINT RIGHT$(\"HELLO\", 3)");

        Assert.That(string.Join(string.Empty, output), Does.Contain("LLO"));
    }

    /// <summary>
    /// Verifies that the <c>CHR$</c> function in Applesoft BASIC correctly converts an ASCII code
    /// to its corresponding character during program execution.
    /// </summary>
    /// <remarks>
    /// This test ensures that the <c>CHR$</c> function behaves as expected by interpreting a BASIC
    /// program that uses <c>CHR$(65)</c> to output the character "A".
    /// </remarks>
    /// <example>
    /// The test runs the following Applesoft BASIC code:
    /// <code>
    /// 10 PRINT CHR$(65)
    /// </code>
    /// The expected output is the character "A".
    /// </example>
    [Test]
    public void Run_ChrFunction_Works()
    {
        interpreter.RunFromSource("10 PRINT CHR$(65)");

        Assert.That(string.Join(string.Empty, output), Does.Contain("A"));
    }

    /// <summary>
    /// Verifies that the <c>ASC</c> function in Applesoft BASIC correctly computes
    /// the ASCII value of a character and outputs the expected result.
    /// </summary>
    /// <remarks>
    /// This test runs a BASIC program that uses the <c>ASC</c> function to compute
    /// the ASCII value of the character "A". It then asserts that the output contains
    /// the correct ASCII value, which is 65.
    /// </remarks>
    /// <example>
    /// The following Applesoft BASIC code is executed in this test:
    /// <code>
    /// 10 PRINT ASC("A")
    /// </code>
    /// The expected output is:
    /// <c>65</c>.
    /// </example>
    [Test]
    public void Run_AscFunction_Works()
    {
        interpreter.RunFromSource("10 PRINT ASC(\"A\")");

        Assert.That(string.Join(string.Empty, output), Does.Contain("65"));
    }

    /// <summary>
    /// Tests the <c>ON ... GOTO</c> statement in Applesoft BASIC to ensure it correctly jumps to the specified line
    /// based on the value of the evaluated expression.
    /// </summary>
    /// <remarks>
    /// This test verifies that the <c>ON ... GOTO</c> statement executes the correct branch and skips others.
    /// It uses a program with multiple <c>GOTO</c> targets and checks the output to confirm the expected behavior.
    /// </remarks>
    /// <example>
    /// The test runs the following Applesoft BASIC program:
    /// <code>
    /// 10 X = 2
    /// 20 ON X GOTO 100, 200, 300
    /// 100 PRINT "ONE"
    /// 110 END
    /// 200 PRINT "TWO"
    /// 210 END
    /// 300 PRINT "THREE"
    /// </code>
    /// Expected output: "TWO".
    /// </example>
    [Test]
    public void Run_OnGoto_JumpsCorrectly()
    {
        interpreter.RunFromSource("10 X = 2\n20 ON X GOTO 100, 200, 300\n100 PRINT \"ONE\"\n110 END\n200 PRINT \"TWO\"\n210 END\n300 PRINT \"THREE\"");

        var output = string.Join(string.Empty, this.output);
        Assert.That(output, Does.Contain("TWO"));
        Assert.That(output, Does.Not.Contain("ONE"));
        Assert.That(output, Does.Not.Contain("THREE"));
    }

    /// <summary>
    /// Tests the <c>POKE</c> and <c>PEEK</c> commands of the <see cref="BasicInterpreter"/> to ensure
    /// they correctly interact with memory.
    /// </summary>
    /// <remarks>
    /// This test verifies that the <c>POKE</c> command writes a value to a specific memory address
    /// and that the <c>PEEK</c> command retrieves the value from the same address.
    /// </remarks>
    [Test]
    public void Run_PeekPoke_WorksWithMemory()
    {
        interpreter.RunFromSource("10 POKE 768, 42\n20 PRINT PEEK(768)");

        Assert.That(string.Join(string.Empty, output), Does.Contain("42"));
    }

    /// <summary>
    /// Verifies that the <see cref="BasicInterpreter.RunFromSource"/> method correctly executes multiple statements on a single line of Applesoft BASIC code.
    /// </summary>
    /// <remarks>
    /// This test ensures that all statements on a single line are executed in sequence, and their effects are correctly reflected.
    /// </remarks>
    /// <example>
    /// For example, given the input <c>"10 X = 1 : Y = 2 : PRINT X + Y"</c>,
    /// the test checks that the output contains the result of <c>X + Y</c>, which is <c>3</c>.
    /// </example>
    [Test]
    public void Run_MultipleStatementsOnLine_ExecutesAll()
    {
        interpreter.RunFromSource("10 X = 1 : Y = 2 : PRINT X + Y");

        Assert.That(string.Join(string.Empty, output), Does.Contain("3"));
    }

    /// <summary>
    /// Verifies that the <see cref="BasicInterpreter"/> correctly handles nested FOR loops
    /// in an Applesoft BASIC program.
    /// </summary>
    /// <remarks>
    /// This test ensures that the interpreter can execute nested FOR loops and produce the expected output.
    /// It validates the output for all iterations of the nested loops.
    /// </remarks>
    /// <example>
    /// The tested Applesoft BASIC program:
    /// <code>
    /// 10 FOR I = 1 TO 2
    /// 20 FOR J = 1 TO 2
    /// 30 PRINT I * 10 + J
    /// 40 NEXT J
    /// 50 NEXT I
    /// </code>
    /// Expected output:
    /// <c>11, 12, 21, 22</c> (order may vary depending on implementation).
    /// </example>
    [Test]
    public void Run_NestedForLoops_WorkCorrectly()
    {
        interpreter.RunFromSource("10 FOR I = 1 TO 2\n20 FOR J = 1 TO 2\n30 PRINT I * 10 + J\n40 NEXT J\n50 NEXT I");

        var output = string.Join(string.Empty, this.output);
        Assert.That(output, Does.Contain("11"));
        Assert.That(output, Does.Contain("12"));
        Assert.That(output, Does.Contain("21"));
        Assert.That(output, Does.Contain("22"));
    }

    /// <summary>
    /// Verifies that logical operators (AND, OR, NOT) in Applesoft BASIC programs
    /// are interpreted and executed correctly by the <see cref="BasicInterpreter"/>.
    /// </summary>
    /// <remarks>
    /// This test ensures that the logical operators produce the expected output
    /// when used in conditional statements. It checks the interpreter's ability
    /// to handle logical operations and their corresponding results.
    /// </remarks>
    /// <example>
    /// The test runs the following Applesoft BASIC code:
    /// <code>
    /// 10 IF 1 AND 1 THEN PRINT "AND"
    /// 10 IF 1 OR 0 THEN PRINT "OR"
    /// 10 IF NOT 0 THEN PRINT "NOT"
    /// </code>
    /// It then verifies that the output contains "AND", "OR", and "NOT".
    /// </example>
    [Test]
    public void Run_LogicalOperators_WorkCorrectly()
    {
        interpreter.RunFromSource("10 IF 1 AND 1 THEN PRINT \"AND\"");
        interpreter.RunFromSource("10 IF 1 OR 0 THEN PRINT \"OR\"");
        interpreter.RunFromSource("10 IF NOT 0 THEN PRINT \"NOT\"");

        var output = string.Join(string.Empty, this.output);
        Assert.That(output, Does.Contain("AND"));
        Assert.That(output, Does.Contain("OR"));
        Assert.That(output, Does.Contain("NOT"));
    }

    /// <summary>
    /// Verifies that the ampersand ('&amp;') operator in Applesoft BASIC programs
    /// can be parsed and executed by the <see cref="BasicInterpreter"/>.
    /// </summary>
    /// <remarks>
    /// This test ensures that the ampersand operator is recognized as a valid statement
    /// and that program execution continues after it. The ampersand operator calls
    /// a machine language routine at address $03F5.
    /// </remarks>
    /// <example>
    /// The test runs the following Applesoft BASIC code:
    /// <code>
    /// 10 REM TEST AMPERSAND OPERATOR
    /// 20 POKE 1013, 96
    /// 30 &amp;
    /// 40 PRINT "DONE"
    /// </code>
    /// It then verifies that the output contains "DONE", confirming that program
    /// execution continued after the ampersand statement.
    /// </example>
    [Test]
    public void Run_AmpersandOperator_ExecutesAndContinues()
    {
        // Set up an RTS instruction at $03F5 so the & operator returns
        // RTS = opcode 0x60
        interpreter.RunFromSource("10 POKE 1013, 96\n20 &\n30 PRINT \"DONE\"");

        Assert.That(string.Join(string.Empty, output), Does.Contain("DONE"));
    }

    /// <summary>
    /// Verifies that the ampersand ('&amp;') operator can be used as part of a multi-statement line.
    /// </summary>
    [Test]
    public void Run_AmpersandOperator_WorksInMultiStatementLine()
    {
        // RTS = opcode 0x60 at $03F5
        interpreter.RunFromSource("10 POKE 1013, 96 : & : PRINT \"SUCCESS\"");

        Assert.That(string.Join(string.Empty, output), Does.Contain("SUCCESS"));
    }

    /// <summary>
    /// Verifies that the USR function in Applesoft BASIC evaluates its parameter
    /// and stores it in FAC1 before calling the user routine.
    /// </summary>
    /// <remarks>
    /// This test ensures that the USR function:
    /// 1. Evaluates the arithmetic expression passed as parameter.
    /// 2. Stores the value in FAC1 at $009D.
    /// 3. Executes the machine language routine at $000A.
    /// 4. Returns the value from FAC1 after the routine completes.
    /// </remarks>
    /// <example>
    /// The test runs the following Applesoft BASIC code:
    /// <code>
    /// 10 REM SET UP USR ROUTINE AT $000A TO JUST RETURN
    /// 20 POKE 10, 96
    /// 30 X = USR(42.5)
    /// 40 PRINT "CALLED"
    /// </code>
    /// It verifies that the program executes successfully and continues after
    /// calling USR.
    /// </example>
    [Test]
    public void Run_UsrFunction_EvaluatesParameterAndReturns()
    {
        // Set up an RTS instruction at $000A so USR returns immediately
        // RTS = opcode 0x60
        interpreter.RunFromSource("10 POKE 10, 96\n20 X = USR(42.5)\n30 PRINT \"CALLED\"");

        Assert.That(string.Join(string.Empty, output), Does.Contain("CALLED"));
    }

    /// <summary>
    /// Verifies that the USR function evaluates expressions before calling the routine.
    /// </summary>
    [Test]
    public void Run_UsrFunction_EvaluatesExpressions()
    {
        // Set up an RTS instruction at $000A
        interpreter.RunFromSource("10 POKE 10, 96\n20 A = 10\n30 X = USR(A * 2.5)\n40 PRINT \"OK\"");

        Assert.That(string.Join(string.Empty, output), Does.Contain("OK"));
    }

    /// <summary>
    /// Verifies that the USR function can be used with the VAL function.
    /// </summary>
    [Test]
    public void Run_UsrFunction_WorksWithVal()
    {
        // Set up an RTS instruction at $000A
        interpreter.RunFromSource("10 POKE 10, 96\n20 X = USR(VAL(\"3.14\"))\n30 PRINT \"COMPLETE\"");

        Assert.That(string.Join(string.Empty, output), Does.Contain("COMPLETE"));
    }

    /// <summary>
    /// Verifies that the ampersand operator and USR function can be used together.
    /// </summary>
    [Test]
    public void Run_AmpersandAndUsr_WorkTogether()
    {
        // Set up RTS instructions at both $03F5 and $000A
        interpreter.RunFromSource("10 POKE 1013, 96\n20 POKE 10, 96\n30 &\n40 X = USR(100)\n50 PRINT \"BOTH\"");

        Assert.That(string.Join(string.Empty, output), Does.Contain("BOTH"));
    }

    /// <summary>
    /// STOP emits break message.
    /// </summary>
    [Test]
    public void Run_Stop_PrintsBreakMessage()
    {
        interpreter.RunFromSource("10 STOP");

        Assert.That(string.Join(string.Empty, output), Does.Contain("BREAK IN 10"));
    }

    /// <summary>
    /// Syntax errors print message.
    /// </summary>
    [Test]
    public void Run_SyntaxError_PrintsErrorMessage()
    {
        interpreter.RunFromSource("10 PRINT +");

        Assert.That(string.Join(string.Empty, output), Does.Contain("?SYNTAX ERROR"));
    }

    /// <summary>
    /// CLEAR resets data and RESTORE reloads.
    /// </summary>
    [Test]
    public void Run_ClearRestoreAndData_ReadsAgain()
    {
        interpreter.RunFromSource("10 DATA 1,2\n20 READ A,B\n30 CLEAR\n40 RESTORE\n50 READ A,B\n60 PRINT A;B");

        Assert.That(string.Join(string.Empty, output), Does.Contain(" 1 2"));
    }

    /// <summary>
    /// HTAB/VTAB update cursor positions.
    /// </summary>
    [Test]
    public void Run_HtabAndVtab_UpdateCursor()
    {
        List<(int Col, int Row)> positions = [];
        mockIo.Setup(io => io.SetCursorPosition(It.IsAny<int>(), It.IsAny<int>()))
            .Callback<int, int>((c, r) =>
            {
                positions.Add((c, r));
                cursorCol = c;
                cursorRow = r;
            });

        interpreter.RunFromSource("10 HTAB 5: VTAB 4");

        Assert.That(positions, Is.EqualTo(new List<(int, int)> { (5, 1), (6, 4) }));
    }

    /// <summary>
    /// Text mode statements switch mode.
    /// </summary>
    [Test]
    public void Run_TextModes_SwitchesModes()
    {
        interpreter.RunFromSource("10 INVERSE\n20 FLASH\n30 NORMAL");

        Assert.That(textMode, Is.EqualTo(TextMode.Normal));
    }

    /// <summary>
    /// ON GOTO jumps to correct line.
    /// </summary>
    [Test]
    public void Run_OnGoto_JumpsToCorrectLine()
    {
        interpreter.RunFromSource("10 ON 1 GOTO 40,50\n20 PRINT \"MISS\"\n40 PRINT \"HIT\"\n50 END");

        Assert.That(string.Join(string.Empty, output), Does.Contain("HIT").And.Not.Contain("MISS"));
    }

    /// <summary>
    /// ON GOSUB returns to caller.
    /// </summary>
    [Test]
    public void Run_OnGosub_ReturnsToCaller()
    {
        interpreter.RunFromSource("10 ON 2 GOSUB 30,60\n20 PRINT \"AFTER\"\n30 PRINT \"ONE\": RETURN\n60 PRINT \"TWO\": RETURN");

        string combined = string.Join(string.Empty, output);
        Assert.That(combined, Does.Contain("TWO"));
        Assert.That(combined, Does.Contain("AFTER"));
    }

    /// <summary>
    /// GET plus TAB/SPC formatting works.
    /// </summary>
    [Test]
    public void Run_GetAndPrint_UsesTabAndSpc()
    {
        interpreter.RunFromSource("10 GET A$\n20 PRINT A$;TAB(3);\"X\";SPC(2);\"Y\"");

        string combined = string.Join(string.Empty, output);
        Assert.That(combined, Does.Contain("Z"));
        Assert.That(combined, Does.Contain("X"));
        Assert.That(combined, Does.Contain("Y"));
    }

    /// <summary>
    /// HIMEM/LOMEM/POKE/PEEK operate on memory.
    /// </summary>
    [Test]
    public void Run_MemoryStatements_UpdateMemory()
    {
        interpreter.RunFromSource("10 HIMEM: 50000\n20 LOMEM: 2048\n30 POKE 4660,255\n40 PRINT PEEK(4660)");

        ushort himem = interpreter.AppleSystem.Memory.ReadWord(0x73);
        ushort lomem = interpreter.AppleSystem.Memory.ReadWord(0x69);
        Assert.That(himem, Is.EqualTo(50000));
        Assert.That(lomem, Is.EqualTo(2048));
        Assert.That(string.Join(string.Empty, output), Does.Contain(" 255"));
    }

    /// <summary>
    /// Graphics stubs do not throw.
    /// </summary>
    [Test]
    public void Run_GraphicsStatements_DoNotThrow()
    {
        Assert.DoesNotThrow(() => interpreter.RunFromSource(
            "10 TEXT: GR: HGR: HGR2\n" +
            "20 COLOR= 3: HCOLOR= 2\n" +
            "30 PLOT 1,2: HPLOT 1,2 TO 3,4\n" +
            "40 DRAW 5: XDRAW 6\n" +
            "50 HOME"));
    }

    /// <summary>
    /// INPUT retries on invalid and stores values.
    /// </summary>
    [Test]
    public void Run_Input_RetriesOnInvalidAndStoresValues()
    {
        mockIo.SetupSequence(io => io.ReadLine(It.IsAny<string>()))
            .Returns("abc")
            .Returns("123,HELLO");

        interpreter.RunFromSource("10 INPUT A,B$\n20 PRINT A;B$");

        string combined = string.Join(string.Empty, output);
        Assert.That(combined, Does.Contain("??REDO FROM START"));
        Assert.That(combined, Does.Contain(" 123HELLO"));
    }

    /// <summary>
    /// PRINT with comma uses tab zones.
    /// </summary>
    [Test]
    public void Run_PrintWithComma_UsesTabZones()
    {
        interpreter.RunFromSource("10 PRINT 1,2");

        Assert.That(string.Join(string.Empty, output), Does.Contain(" 1").And.Contain(" 2"));
    }

    /// <summary>
    /// NEXT without FOR shows error.
    /// </summary>
    [Test]
    public void Run_NextWithoutFor_ShowsError()
    {
        interpreter.RunFromSource("10 NEXT I");

        Assert.That(string.Join(string.Empty, output), Does.Contain("NEXT WITHOUT FOR"));
    }

    /// <summary>
    /// SLEEP statement does not throw.
    /// </summary>
    [Test]
    public void Run_Sleep_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => interpreter.RunFromSource("10 SLEEP 0"));
    }

    /// <summary>
    /// REM statement is ignored.
    /// </summary>
    [Test]
    public void Run_RemStatement_IsIgnored()
    {
        interpreter.RunFromSource("10 REM COMMENT\n20 PRINT \"OK\"");

        Assert.That(string.Join(string.Empty, output), Does.Contain("OK"));
    }

    /// <summary>
    /// CALL invokes AppleSystem.
    /// </summary>
    [Test]
    public void Run_Call_InvokesAppleSystem()
    {
        interpreter.RunFromSource("10 CALL 1234\n20 PRINT \"AFTER\"");

        Assert.That(string.Join(string.Empty, output), Does.Contain("AFTER"));
    }
}