// <copyright file="ParserTests.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Tests;

using Interpreter.AST;
using Interpreter.Lexer;
using Interpreter.Parser;
using Microsoft.Extensions.Logging;
using Moq;

[TestFixture]
public class ParserTests
{
    private BasicParser _parser = null!;

    [SetUp]
    public void Setup()
    {
        var lexerLogger = new Mock<ILogger<BasicLexer>>();
        var parserLogger = new Mock<ILogger<BasicParser>>();
        var lexer = new BasicLexer(lexerLogger.Object);
        _parser = new BasicParser(lexer, parserLogger.Object);
    }

    [Test]
    public void Parse_PrintStatement_CreatesPrintNode()
    {
        var program = _parser.Parse("10 PRINT \"HELLO\"");

        Assert.That(program.Lines, Has.Count.EqualTo(1));
        Assert.That(program.Lines[0].LineNumber, Is.EqualTo(10));
        Assert.That(program.Lines[0].Statements[0], Is.InstanceOf<PrintStatement>());
    }

    [Test]
    public void Parse_LetStatement_CreatesLetNode()
    {
        var program = _parser.Parse("10 LET X = 5");

        var stmt = program.Lines[0].Statements[0] as LetStatement;
        Assert.That(stmt, Is.Not.Null);
        Assert.That(stmt!.Variable.Name, Is.EqualTo("X"));
    }

    [Test]
    public void Parse_ImplicitLet_CreatesLetNode()
    {
        var program = _parser.Parse("10 X = 5");

        Assert.That(program.Lines[0].Statements[0], Is.InstanceOf<LetStatement>());
    }

    [Test]
    public void Parse_ForLoop_CreatesForNode()
    {
        var program = _parser.Parse("10 FOR I = 1 TO 10");

        var stmt = program.Lines[0].Statements[0] as ForStatement;
        Assert.That(stmt, Is.Not.Null);
        Assert.That(stmt!.Variable, Is.EqualTo("I"));
    }

    [Test]
    public void Parse_ForLoopWithStep_IncludesStep()
    {
        var program = _parser.Parse("10 FOR I = 1 TO 10 STEP 2");

        var stmt = program.Lines[0].Statements[0] as ForStatement;
        Assert.That(stmt!.Step, Is.Not.Null);
    }

    [Test]
    public void Parse_IfThen_CreatesIfNode()
    {
        var program = _parser.Parse("10 IF X > 5 THEN PRINT \"BIG\"");

        Assert.That(program.Lines[0].Statements[0], Is.InstanceOf<IfStatement>());
    }

    [Test]
    public void Parse_IfThenGoto_CreatesIfWithLineNumber()
    {
        var program = _parser.Parse("10 IF X > 5 THEN 100");

        var stmt = program.Lines[0].Statements[0] as IfStatement;
        Assert.That(stmt!.GotoLineNumber, Is.EqualTo(100));
    }

    [Test]
    public void Parse_Goto_CreatesGotoNode()
    {
        var program = _parser.Parse("10 GOTO 100");

        var stmt = program.Lines[0].Statements[0] as GotoStatement;
        Assert.That(stmt, Is.Not.Null);
        Assert.That(stmt!.LineNumber, Is.EqualTo(100));
    }

    [Test]
    public void Parse_Gosub_CreatesGosubNode()
    {
        var program = _parser.Parse("10 GOSUB 100");

        var stmt = program.Lines[0].Statements[0] as GosubStatement;
        Assert.That(stmt, Is.Not.Null);
        Assert.That(stmt!.LineNumber, Is.EqualTo(100));
    }

    [Test]
    public void Parse_Dim_CreatesDimNode()
    {
        var program = _parser.Parse("10 DIM A(10)");

        var stmt = program.Lines[0].Statements[0] as DimStatement;
        Assert.That(stmt, Is.Not.Null);
        Assert.That(stmt!.Arrays, Has.Count.EqualTo(1));
    }

    [Test]
    public void Parse_DimMultiDimensional_ParsesCorrectly()
    {
        var program = _parser.Parse("10 DIM A(10,20)");

        var stmt = program.Lines[0].Statements[0] as DimStatement;
        Assert.That(stmt!.Arrays[0].Dimensions, Has.Count.EqualTo(2));
    }

    [Test]
    public void Parse_Data_CollectsValues()
    {
        var program = _parser.Parse("10 DATA 1, 2, 3, \"HELLO\"");

        Assert.That(program.DataValues, Has.Count.EqualTo(4));
    }

    [Test]
    public void Parse_DefFn_CreatesDefNode()
    {
        var program = _parser.Parse("10 DEF FN SQUARE(X) = X * X");

        var stmt = program.Lines[0].Statements[0] as DefStatement;
        Assert.That(stmt, Is.Not.Null);
        Assert.That(stmt!.FunctionName, Is.EqualTo("SQUARE"));
        Assert.That(stmt!.Parameter, Is.EqualTo("X"));
    }

    [Test]
    public void Parse_OnGoto_CreatesOnGotoNode()
    {
        var program = _parser.Parse("10 ON X GOTO 100, 200, 300");

        var stmt = program.Lines[0].Statements[0] as OnGotoStatement;
        Assert.That(stmt, Is.Not.Null);
        Assert.That(stmt!.LineNumbers, Has.Count.EqualTo(3));
    }

    [Test]
    public void Parse_Poke_CreatesPokeNode()
    {
        var program = _parser.Parse("10 POKE 49152, 255");

        Assert.That(program.Lines[0].Statements[0], Is.InstanceOf<PokeStatement>());
    }

    [Test]
    public void Parse_Call_CreatesCallNode()
    {
        var program = _parser.Parse("10 CALL -936");

        Assert.That(program.Lines[0].Statements[0], Is.InstanceOf<CallStatement>());
    }

    [Test]
    public void Parse_Sleep_CreatesSleepNode()
    {
        var program = _parser.Parse("10 SLEEP 1000");

        Assert.That(program.Lines[0].Statements[0], Is.InstanceOf<SleepStatement>());
    }

    [Test]
    public void Parse_MultipleStatementsOnLine_ParsesAll()
    {
        var program = _parser.Parse("10 X = 1 : Y = 2 : PRINT X + Y");

        Assert.That(program.Lines[0].Statements, Has.Count.EqualTo(3));
    }

    [Test]
    public void Parse_BinaryExpression_ParsesCorrectly()
    {
        var program = _parser.Parse("10 X = 1 + 2 * 3");

        var stmt = program.Lines[0].Statements[0] as LetStatement;
        Assert.That(stmt!.Value, Is.InstanceOf<BinaryExpression>());
    }

    [Test]
    public void Parse_FunctionCall_ParsesCorrectly()
    {
        var program = _parser.Parse("10 X = SIN(3.14)");

        var stmt = program.Lines[0].Statements[0] as LetStatement;
        Assert.That(stmt!.Value, Is.InstanceOf<FunctionCallExpression>());
    }

    [Test]
    public void Parse_StringFunction_ParsesCorrectly()
    {
        var program = _parser.Parse("10 A$ = MID$(B$, 1, 5)");

        var stmt = program.Lines[0].Statements[0] as LetStatement;
        Assert.That(stmt!.Value, Is.InstanceOf<FunctionCallExpression>());
    }

    [Test]
    public void Parse_ArrayAccess_ParsesCorrectly()
    {
        var program = _parser.Parse("10 X = A(5)");

        var stmt = program.Lines[0].Statements[0] as LetStatement;
        Assert.That(stmt!.Value, Is.InstanceOf<ArrayAccessExpression>());
    }

    [Test]
    public void Parse_LinesSortedByNumber()
    {
        var program = _parser.Parse("30 PRINT \"C\"\n10 PRINT \"A\"\n20 PRINT \"B\"");

        Assert.That(program.Lines[0].LineNumber, Is.EqualTo(10));
        Assert.That(program.Lines[1].LineNumber, Is.EqualTo(20));
        Assert.That(program.Lines[2].LineNumber, Is.EqualTo(30));
    }
}
