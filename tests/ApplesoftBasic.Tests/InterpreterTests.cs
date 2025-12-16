// <copyright file="InterpreterTests.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Tests;

using Interpreter.Emulation;
using Interpreter.Execution;
using Interpreter.IO;
using Interpreter.Lexer;
using Interpreter.Parser;
using Interpreter.Runtime;
using Microsoft.Extensions.Logging;
using Moq;

[TestFixture]
public class InterpreterTests
{
    private BasicInterpreter _interpreter = null!;
    private Mock<IBasicIO> _mockIO = null!;
    private List<string> _output = null!;

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
        var loops = new LoopManager();
        var gosub = new GosubManager();
        var memory = new AppleMemory(memoryLogger.Object);
        var cpu = new Cpu6502(memory, cpuLogger.Object);
        var speaker = new AppleSpeaker(speakerLogger.Object);
        var appleSystem = new AppleSystem(memory, cpu, speaker, systemLogger.Object);

        _output = new List<string>();
        _mockIO = new Mock<IBasicIO>();
        _mockIO.Setup(io => io.Write(It.IsAny<string>())).Callback<string>(s => _output.Add(s));
        _mockIO.Setup(io => io.WriteLine(It.IsAny<string>())).Callback<string>(s => _output.Add(s + "\n"));
        _mockIO.Setup(io => io.GetCursorColumn()).Returns(0);

        _interpreter = new BasicInterpreter(
            parser, _mockIO.Object, variables, functions, data, loops, gosub,
            appleSystem, interpreterLogger.Object);
    }

    [Test]
    public void Run_PrintString_OutputsString()
    {
        _interpreter.Run("10 PRINT \"HELLO WORLD\"");

        Assert.That(string.Join("", _output), Does.Contain("HELLO WORLD"));
    }

    [Test]
    public void Run_PrintNumber_OutputsNumber()
    {
        _interpreter.Run("10 PRINT 42");

        Assert.That(string.Join("", _output), Does.Contain("42"));
    }

    [Test]
    public void Run_PrintExpression_ComputesResult()
    {
        _interpreter.Run("10 PRINT 2 + 3 * 4");

        Assert.That(string.Join("", _output), Does.Contain("14"));
    }

    [Test]
    public void Run_Assignment_StoresValue()
    {
        _interpreter.Run("10 X = 5\n20 PRINT X");

        Assert.That(string.Join("", _output), Does.Contain("5"));
    }

    [Test]
    public void Run_ForLoop_Iterates()
    {
        _interpreter.Run("10 FOR I = 1 TO 3\n20 PRINT I\n30 NEXT I");

        var output = string.Join("", _output);
        Assert.That(output, Does.Contain("1"));
        Assert.That(output, Does.Contain("2"));
        Assert.That(output, Does.Contain("3"));
    }

    [Test]
    public void Run_ForLoopWithStep_UsesStep()
    {
        _interpreter.Run("10 FOR I = 2 TO 6 STEP 2\n20 PRINT I\n30 NEXT I");

        var output = string.Join("", _output);
        Assert.That(output, Does.Contain("2"));
        Assert.That(output, Does.Contain("4"));
        Assert.That(output, Does.Contain("6"));
    }

    [Test]
    public void Run_IfTrue_ExecutesThenBranch()
    {
        _interpreter.Run("10 X = 10\n20 IF X > 5 THEN PRINT \"BIG\"");

        Assert.That(string.Join("", _output), Does.Contain("BIG"));
    }

    [Test]
    public void Run_IfFalse_SkipsThenBranch()
    {
        _interpreter.Run("10 X = 1\n20 IF X > 5 THEN PRINT \"BIG\"\n30 PRINT \"END\"");

        var output = string.Join("", _output);
        Assert.That(output, Does.Not.Contain("BIG"));
        Assert.That(output, Does.Contain("END"));
    }

    [Test]
    public void Run_Goto_JumpsToLine()
    {
        _interpreter.Run("10 GOTO 30\n20 PRINT \"SKIP\"\n30 PRINT \"HERE\"");

        var output = string.Join("", _output);
        Assert.That(output, Does.Not.Contain("SKIP"));
        Assert.That(output, Does.Contain("HERE"));
    }

    [Test]
    public void Run_GosubReturn_WorksCorrectly()
    {
        _interpreter.Run("10 GOSUB 100\n20 PRINT \"BACK\"\n30 END\n100 PRINT \"SUB\"\n110 RETURN");

        var output = string.Join("", _output);
        Assert.That(output, Does.Contain("SUB"));
        Assert.That(output, Does.Contain("BACK"));
    }

    [Test]
    public void Run_DataRead_ReadsValues()
    {
        _interpreter.Run("10 READ X, Y\n20 PRINT X + Y\n30 DATA 10, 20");

        Assert.That(string.Join("", _output), Does.Contain("30"));
    }

    [Test]
    public void Run_DimAndArray_Works()
    {
        _interpreter.Run("10 DIM A(5)\n20 A(3) = 42\n30 PRINT A(3)");

        Assert.That(string.Join("", _output), Does.Contain("42"));
    }

    [Test]
    public void Run_DefFn_DefinesFunction()
    {
        _interpreter.Run("10 DEF FN SQ(X) = X * X\n20 PRINT FN SQ(5)");

        Assert.That(string.Join("", _output), Does.Contain("25"));
    }

    [Test]
    public void Run_StringVariable_Works()
    {
        _interpreter.Run("10 A$ = \"HELLO\"\n20 PRINT A$");

        Assert.That(string.Join("", _output), Does.Contain("HELLO"));
    }

    [Test]
    public void Run_StringConcatenation_Works()
    {
        _interpreter.Run("10 A$ = \"HELLO\" + \" WORLD\"\n20 PRINT A$");

        Assert.That(string.Join("", _output), Does.Contain("HELLO WORLD"));
    }

    [Test]
    public void Run_AbsFunction_Works()
    {
        _interpreter.Run("10 PRINT ABS(-5)");

        Assert.That(string.Join("", _output), Does.Contain("5"));
    }

    [Test]
    public void Run_IntFunction_Works()
    {
        _interpreter.Run("10 PRINT INT(3.7)");

        Assert.That(string.Join("", _output), Does.Contain("3"));
    }

    [Test]
    public void Run_LenFunction_Works()
    {
        _interpreter.Run("10 PRINT LEN(\"HELLO\")");

        Assert.That(string.Join("", _output), Does.Contain("5"));
    }

    [Test]
    public void Run_MidFunction_Works()
    {
        _interpreter.Run("10 PRINT MID$(\"HELLO\", 2, 3)");

        Assert.That(string.Join("", _output), Does.Contain("ELL"));
    }

    [Test]
    public void Run_LeftFunction_Works()
    {
        _interpreter.Run("10 PRINT LEFT$(\"HELLO\", 3)");

        Assert.That(string.Join("", _output), Does.Contain("HEL"));
    }

    [Test]
    public void Run_RightFunction_Works()
    {
        _interpreter.Run("10 PRINT RIGHT$(\"HELLO\", 3)");

        Assert.That(string.Join("", _output), Does.Contain("LLO"));
    }

    [Test]
    public void Run_ChrFunction_Works()
    {
        _interpreter.Run("10 PRINT CHR$(65)");

        Assert.That(string.Join("", _output), Does.Contain("A"));
    }

    [Test]
    public void Run_AscFunction_Works()
    {
        _interpreter.Run("10 PRINT ASC(\"A\")");

        Assert.That(string.Join("", _output), Does.Contain("65"));
    }

    [Test]
    public void Run_OnGoto_JumpsCorrectly()
    {
        _interpreter.Run("10 X = 2\n20 ON X GOTO 100, 200, 300\n100 PRINT \"ONE\"\n110 END\n200 PRINT \"TWO\"\n210 END\n300 PRINT \"THREE\"");

        var output = string.Join("", _output);
        Assert.That(output, Does.Contain("TWO"));
        Assert.That(output, Does.Not.Contain("ONE"));
        Assert.That(output, Does.Not.Contain("THREE"));
    }

    [Test]
    public void Run_PeekPoke_WorksWithMemory()
    {
        _interpreter.Run("10 POKE 768, 42\n20 PRINT PEEK(768)");

        Assert.That(string.Join("", _output), Does.Contain("42"));
    }

    [Test]
    public void Run_MultipleStatementsOnLine_ExecutesAll()
    {
        _interpreter.Run("10 X = 1 : Y = 2 : PRINT X + Y");

        Assert.That(string.Join("", _output), Does.Contain("3"));
    }

    [Test]
    public void Run_NestedForLoops_WorkCorrectly()
    {
        _interpreter.Run("10 FOR I = 1 TO 2\n20 FOR J = 1 TO 2\n30 PRINT I * 10 + J\n40 NEXT J\n50 NEXT I");

        var output = string.Join("", _output);
        Assert.That(output, Does.Contain("11"));
        Assert.That(output, Does.Contain("12"));
        Assert.That(output, Does.Contain("21"));
        Assert.That(output, Does.Contain("22"));
    }

    [Test]
    public void Run_LogicalOperators_WorkCorrectly()
    {
        _interpreter.Run("10 IF 1 AND 1 THEN PRINT \"AND\"");
        _interpreter.Run("10 IF 1 OR 0 THEN PRINT \"OR\"");
        _interpreter.Run("10 IF NOT 0 THEN PRINT \"NOT\"");

        var output = string.Join("", _output);
        Assert.That(output, Does.Contain("AND"));
        Assert.That(output, Does.Contain("OR"));
        Assert.That(output, Does.Contain("NOT"));
    }
}
