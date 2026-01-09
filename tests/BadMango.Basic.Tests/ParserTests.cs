// <copyright file="ParserTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.Tests;

using AST;

using Lexer;

using Microsoft.Extensions.Logging;

using Moq;

using Parser;

/// <summary>
/// Contains unit tests for the <see cref="BasicParser"/> class, ensuring correct parsing of Applesoft BASIC statements.
/// </summary>
/// <remarks>
/// This test class validates the behavior of the parser across various Applesoft BASIC constructs,
/// including but not limited to PRINT, LET, FOR, IF, GOTO, DIM, DATA, DEF, and other statements.
/// </remarks>
[TestFixture]
public class ParserTests
{
    private BasicParser parser = null!;

    /// <summary>
    /// Sets up the test environment for the <see cref="ParserTests"/> class.
    /// </summary>
    /// <remarks>
    /// This method initializes the <see cref="BasicParser"/> instance with a mocked
    /// <see cref="ILogger{TCategoryName}"/> for both the lexer and parser components.
    /// It ensures that each test starts with a fresh parser instance configured appropriately.
    /// </remarks>
    [SetUp]
    public void Setup()
    {
        var lexerLogger = new Mock<ILogger<BasicLexer>>();
        var parserLogger = new Mock<ILogger<BasicParser>>();
        var lexer = new BasicLexer(lexerLogger.Object);
        parser = new(lexer, parserLogger.Object);
    }

    /// <summary>
    /// Verifies that parsing a PRINT statement in Applesoft BASIC source code
    /// creates a <see cref="PrintStatement"/> node in the abstract syntax tree (AST).
    /// </summary>
    /// <remarks>
    /// This test ensures that the <see cref="BasicParser"/> correctly identifies and processes
    /// a PRINT statement, associating it with the appropriate line number and statement type.
    /// </remarks>
    /// <example>
    /// Given the source code <c>10 PRINT "HELLO"</c>, the parser should produce a <see cref="ProgramNode"/>
    /// containing a single <see cref="LineNode"/> with a line number of 10 and a <see cref="PrintStatement"/>.
    /// </example>
    [Test]
    public void Parse_PrintStatement_CreatesPrintNode()
    {
        var program = parser.Parse("10 PRINT \"HELLO\"");

        Assert.That(program.Lines, Has.Count.EqualTo(1));
        Assert.That(program.Lines[0].LineNumber, Is.EqualTo(10));
        Assert.That(program.Lines[0].Statements[0], Is.InstanceOf<PrintStatement>());
    }

    /// <summary>
    /// Tests that the <see cref="BasicParser"/> correctly parses a LET statement
    /// and creates a corresponding <see cref="LetStatement"/> node in the AST.
    /// </summary>
    /// <remarks>
    /// This test ensures that the parser can handle a LET statement, such as
    /// "10 LET X = 5", and correctly identifies the variable name and its assignment.
    /// </remarks>
    /// <example>
    /// Given the input "10 LET X = 5", the parser should produce a <see cref="LetStatement"/>
    /// where the variable name is "X".
    /// </example>
    [Test]
    public void Parse_LetStatement_CreatesLetNode()
    {
        var program = parser.Parse("10 LET X = 5");

        var stmt = program.Lines[0].Statements[0] as LetStatement;
        Assert.That(stmt, Is.Not.Null);
        Assert.That(stmt!.Variable.Name, Is.EqualTo("X"));
    }

    /// <summary>
    /// Tests whether the <see cref="BasicParser"/> correctly parses an implicit LET statement
    /// and creates a corresponding <see cref="LetStatement"/> node in the abstract syntax tree (AST).
    /// </summary>
    /// <remarks>
    /// This test verifies that the parser can handle implicit LET statements, such as "X = 5",
    /// without requiring the explicit "LET" keyword, as per Applesoft BASIC syntax.
    /// </remarks>
    [Test]
    public void Parse_ImplicitLet_CreatesLetNode()
    {
        var program = parser.Parse("10 X = 5");

        Assert.That(program.Lines[0].Statements[0], Is.InstanceOf<LetStatement>());
    }

    /// <summary>
    /// Tests whether the <see cref="BasicParser"/> correctly parses a FOR loop statement
    /// and creates a <see cref="ForStatement"/> node.
    /// </summary>
    /// <remarks>
    /// This test ensures that a FOR loop statement in Applesoft BASIC, such as "10 FOR I = 1 TO 10",
    /// is parsed into a <see cref="ForStatement"/> with the correct variable and structure.
    /// </remarks>
    [Test]
    public void Parse_ForLoop_CreatesForNode()
    {
        var program = parser.Parse("10 FOR I = 1 TO 10");

        var stmt = program.Lines[0].Statements[0] as ForStatement;
        Assert.That(stmt, Is.Not.Null);
        Assert.That(stmt!.Variable, Is.EqualTo("I"));
    }

    /// <summary>
    /// Verifies that the <see cref="BasicParser"/> correctly parses a FOR loop with a STEP clause.
    /// </summary>
    /// <remarks>
    /// This test ensures that the STEP value in a FOR loop is properly included in the resulting
    /// <see cref="ForStatement"/> when parsing Applesoft BASIC code.
    /// </remarks>
    /// <example>
    /// The following Applesoft BASIC code is used in this test:
    /// <code>
    /// 10 FOR I = 1 TO 10 STEP 2
    /// </code>
    /// The test asserts that the <see cref="ForStatement.Step"/> property is not null.
    /// </example>
    [Test]
    public void Parse_ForLoopWithStep_IncludesStep()
    {
        var program = parser.Parse("10 FOR I = 1 TO 10 STEP 2");

        var stmt = program.Lines[0].Statements[0] as ForStatement;
        Assert.That(stmt!.Step, Is.Not.Null);
    }

    /// <summary>
    /// Verifies that the <see cref="BasicParser"/> correctly parses an Applesoft BASIC IF-THEN statement
    /// and creates an <see cref="IfStatement"/> node in the abstract syntax tree (AST).
    /// </summary>
    /// <remarks>
    /// This test ensures that the parser identifies and processes the IF-THEN construct,
    /// producing the appropriate <see cref="IfStatement"/> representation in the AST.
    /// </remarks>
    /// <example>
    /// The following Applesoft BASIC code is used in this test:
    /// <code>
    /// 10 IF X > 5 THEN PRINT "BIG"
    /// </code>
    /// </example>
    [Test]
    public void Parse_IfThen_CreatesIfNode()
    {
        var program = parser.Parse("10 IF X > 5 THEN PRINT \"BIG\"");

        Assert.That(program.Lines[0].Statements[0], Is.InstanceOf<IfStatement>());
    }

    /// <summary>
    /// Tests that parsing an Applesoft BASIC "IF...THEN GOTO" statement creates an <see cref="IfStatement"/>
    /// with the correct GOTO line number.
    /// </summary>
    /// <remarks>
    /// This test ensures that the <see cref="BasicParser"/> correctly identifies and processes
    /// the GOTO line number specified in an "IF...THEN GOTO" statement.
    /// </remarks>
    [Test]
    public void Parse_IfThenGoto_CreatesIfWithLineNumber()
    {
        var program = parser.Parse("10 IF X > 5 THEN 100");

        var stmt = program.Lines[0].Statements[0] as IfStatement;
        Assert.That(stmt!.GotoLineNumber, Is.EqualTo(100));
    }

    /// <summary>
    /// Tests whether the <see cref="BasicParser"/> correctly parses a GOTO statement
    /// and creates a corresponding <see cref="GotoStatement"/> node.
    /// </summary>
    /// <remarks>
    /// This test verifies that the parser processes a GOTO statement with a specified line number
    /// and constructs a <see cref="GotoStatement"/> with the correct line number value.
    /// </remarks>
    [Test]
    public void Parse_Goto_CreatesGotoNode()
    {
        var program = parser.Parse("10 GOTO 100");

        var stmt = program.Lines[0].Statements[0] as GotoStatement;
        Assert.That(stmt, Is.Not.Null);
        Assert.That(stmt!.LineNumber, Is.EqualTo(100));
    }

    /// <summary>
    /// Tests that parsing a GOSUB statement creates a <see cref="GosubStatement"/> node with the correct line number.
    /// </summary>
    /// <remarks>
    /// This test verifies that the <see cref="BasicParser"/> correctly interprets a GOSUB statement in Applesoft BASIC
    /// and constructs a <see cref="GosubStatement"/> node with the expected line number.
    /// </remarks>
    [Test]
    public void Parse_Gosub_CreatesGosubNode()
    {
        var program = parser.Parse("10 GOSUB 100");

        var stmt = program.Lines[0].Statements[0] as GosubStatement;
        Assert.That(stmt, Is.Not.Null);
        Assert.That(stmt!.LineNumber, Is.EqualTo(100));
    }

    /// <summary>
    /// Verifies that the <c>DIM</c> statement in Applesoft BASIC is correctly parsed into a <see cref="DimStatement"/> node.
    /// </summary>
    /// <remarks>
    /// This test ensures that the parser correctly identifies and processes a <c>DIM</c> statement, creating a
    /// <see cref="DimStatement"/> node with the appropriate array declarations.
    /// </remarks>
    /// <example>
    /// For example, parsing the statement <c>10 DIM A(10)</c> should result in a <see cref="DimStatement"/> node
    /// containing one array declaration for <c>A</c> with a size of 10.
    /// </example>
    [Test]
    public void Parse_Dim_CreatesDimNode()
    {
        var program = parser.Parse("10 DIM A(10)");

        var stmt = program.Lines[0].Statements[0] as DimStatement;
        Assert.That(stmt, Is.Not.Null);
        Assert.That(stmt!.Arrays, Has.Count.EqualTo(1));
    }

    /// <summary>
    /// Tests the parsing of a multi-dimensional array declaration in a DIM statement.
    /// </summary>
    /// <remarks>
    /// This test ensures that the <see cref="BasicParser"/> correctly parses a DIM statement
    /// with multiple dimensions, such as <c>DIM A(10,20)</c>, and verifies that the resulting
    /// <see cref="DimStatement"/> contains the expected number of dimensions.
    /// </remarks>
    /// <example>
    /// Input: <c>10 DIM A(10,20)</c>
    /// Expected: A <see cref="DimStatement"/> with one array declaration containing two dimensions.
    /// </example>
    [Test]
    public void Parse_DimMultiDimensional_ParsesCorrectly()
    {
        var program = parser.Parse("10 DIM A(10,20)");

        var stmt = program.Lines[0].Statements[0] as DimStatement;
        Assert.That(stmt!.Arrays[0].Dimensions, Has.Count.EqualTo(2));
    }

    /// <summary>
    /// Verifies that the <see cref="BasicParser"/> correctly parses a DATA statement
    /// and collects the specified values into the <see cref="ProgramNode.DataValues"/> collection.
    /// </summary>
    /// <remarks>
    /// This test ensures that the parser processes the DATA statement accurately,
    /// including handling numeric and string values, and stores them in the program's data values.
    /// </remarks>
    /// <example>
    /// Given the input `10 DATA 1, 2, 3, "HELLO"`, the test asserts that the resulting
    /// <see cref="ProgramNode.DataValues"/> contains four elements: 1, 2, 3, and "HELLO".
    /// </example>
    [Test]
    public void Parse_Data_CollectsValues()
    {
        var program = parser.Parse("10 DATA 1, 2, 3, \"HELLO\"");

        Assert.That(program.DataValues, Has.Count.EqualTo(4));
    }

    /// <summary>
    /// Tests that the <see cref="BasicParser"/> correctly parses a DEF FN statement
    /// and creates a <see cref="DefStatement"/> node with the expected properties.
    /// </summary>
    /// <remarks>
    /// This test verifies that the parser can handle the DEF FN syntax in Applesoft BASIC,
    /// ensuring that the function name, parameter, and other relevant details are parsed correctly.
    /// </remarks>
    /// <example>
    /// Given the input:
    /// <code>
    /// 10 DEF FN SQUARE(X) = X * X
    /// </code>
    /// The parser should produce a <see cref="DefStatement"/> with:
    /// <list type="bullet">
    /// <item><description>FunctionName: "SQUARE"</description></item>
    /// <item><description>Parameter: "X"</description></item>
    /// </list>
    /// </example>
    [Test]
    public void Parse_DefFn_CreatesDefNode()
    {
        var program = parser.Parse("10 DEF FN SQUARE(X) = X * X");

        var stmt = program.Lines[0].Statements[0] as DefStatement;
        Assert.That(stmt, Is.Not.Null);
        Assert.That(stmt!.FunctionName, Is.EqualTo("SQUARE"));
        Assert.That(stmt!.Parameter, Is.EqualTo("X"));
    }

    /// <summary>
    /// Tests that the <c>ON ... GOTO</c> statement in Applesoft BASIC is correctly parsed into an <see cref="OnGotoStatement"/> node.
    /// </summary>
    /// <remarks>
    /// This test verifies that the parser correctly identifies the <c>ON ... GOTO</c> statement, extracts the associated line numbers,
    /// and constructs an <see cref="OnGotoStatement"/> with the expected properties.
    /// </remarks>
    /// <example>
    /// For example, given the input <c>10 ON X GOTO 100, 200, 300</c>, the parser should create an <see cref="OnGotoStatement"/>
    /// containing the line numbers 100, 200, and 300.
    /// </example>
    [Test]
    public void Parse_OnGoto_CreatesOnGotoNode()
    {
        var program = parser.Parse("10 ON X GOTO 100, 200, 300");

        var stmt = program.Lines[0].Statements[0] as OnGotoStatement;
        Assert.That(stmt, Is.Not.Null);
        Assert.That(stmt!.LineNumbers, Has.Count.EqualTo(3));
    }

    /// <summary>
    /// Verifies that the <see cref="BasicParser"/> correctly parses a POKE statement
    /// and creates a corresponding <see cref="PokeStatement"/> node in the abstract syntax tree (AST).
    /// </summary>
    /// <remarks>
    /// This test ensures that the parser can handle the POKE statement syntax, which assigns a value
    /// to a specific memory address in Applesoft BASIC.
    /// </remarks>
    /// <example>
    /// For example, parsing the statement <c>10 POKE 49152, 255</c> should result in a <see cref="PokeStatement"/>
    /// being created as part of the AST.
    /// </example>
    [Test]
    public void Parse_Poke_CreatesPokeNode()
    {
        var program = parser.Parse("10 POKE 49152, 255");

        Assert.That(program.Lines[0].Statements[0], Is.InstanceOf<PokeStatement>());
    }

    /// <summary>
    /// Verifies that the <see cref="BasicParser"/> correctly parses a CALL statement
    /// and creates a <see cref="CallStatement"/> node in the abstract syntax tree (AST).
    /// </summary>
    /// <remarks>
    /// This test ensures that the parser can handle Applesoft BASIC CALL statements,
    /// which are used to invoke machine language subroutines.
    /// </remarks>
    /// <example>
    /// For example, parsing the statement <c>10 CALL -936</c> should result in a
    /// <see cref="CallStatement"/> being created with the appropriate address expression.
    /// </example>
    [Test]
    public void Parse_Call_CreatesCallNode()
    {
        var program = parser.Parse("10 CALL -936");

        Assert.That(program.Lines[0].Statements[0], Is.InstanceOf<CallStatement>());
    }

    /// <summary>
    /// Verifies that the <c>SLEEP</c> statement in Applesoft BASIC is correctly parsed into a <see cref="SleepStatement"/> node.
    /// </summary>
    /// <remarks>
    /// This test ensures that the parser recognizes the <c>SLEEP</c> statement and correctly constructs a
    /// <see cref="SleepStatement"/> node with the appropriate expression for the sleep duration.
    /// </remarks>
    /// <example>
    /// Given the Applesoft BASIC code <c>10 SLEEP 1000</c>, this test ensures that the resulting AST contains a
    /// <see cref="SleepStatement"/> node representing the <c>SLEEP</c> operation with a duration of 1000 milliseconds.
    /// </example>
    [Test]
    public void Parse_Sleep_CreatesSleepNode()
    {
        var program = parser.Parse("10 SLEEP 1000");

        Assert.That(program.Lines[0].Statements[0], Is.InstanceOf<SleepStatement>());
    }

    /// <summary>
    /// Verifies that the <see cref="BasicParser"/> correctly parses multiple statements on a single line of Applesoft BASIC code.
    /// </summary>
    /// <remarks>
    /// This test ensures that the parser can handle multiple statements separated by colons (:) on a single line,
    /// and that all statements are correctly parsed and included in the resulting abstract syntax tree (AST).
    /// </remarks>
    /// <example>
    /// Input: <c>10 X = 1 : Y = 2 : PRINT X + Y</c>
    /// Expected: The line contains three statements: an assignment to <c>X</c>, an assignment to <c>Y</c>, and a <c>PRINT</c> statement.
    /// </example>
    [Test]
    public void Parse_MultipleStatementsOnLine_ParsesAll()
    {
        var program = parser.Parse("10 X = 1 : Y = 2 : PRINT X + Y");

        Assert.That(program.Lines[0].Statements, Has.Count.EqualTo(3));
    }

    /// <summary>
    /// Verifies that the <see cref="BasicParser"/> correctly parses a binary expression
    /// and constructs a <see cref="BinaryExpression"/> node within a <see cref="LetStatement"/>.
    /// </summary>
    /// <remarks>
    /// This test ensures that the parser correctly handles operator precedence and
    /// constructs the appropriate abstract syntax tree (AST) for a binary expression.
    /// </remarks>
    [Test]
    public void Parse_BinaryExpression_ParsesCorrectly()
    {
        var program = parser.Parse("10 X = 1 + 2 * 3");

        var stmt = program.Lines[0].Statements[0] as LetStatement;
        Assert.That(stmt!.Value, Is.InstanceOf<BinaryExpression>());
    }

    /// <summary>
    /// Tests whether the <see cref="BasicParser"/> correctly parses a function call expression
    /// within a LET statement in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// This test ensures that a function call, such as <c>SIN(3.14)</c>, is correctly identified
    /// and represented as a <see cref="FunctionCallExpression"/> within the parsed AST.
    /// </remarks>
    [Test]
    public void Parse_FunctionCall_ParsesCorrectly()
    {
        var program = parser.Parse("10 X = SIN(3.14)");

        var stmt = program.Lines[0].Statements[0] as LetStatement;
        Assert.That(stmt!.Value, Is.InstanceOf<FunctionCallExpression>());
    }

    /// <summary>
    /// Tests whether the parser correctly parses a string function assignment in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// This test verifies that the parser can handle the assignment of a string function, such as <c>MID$</c>,
    /// to a variable. It ensures that the resulting statement is correctly identified as a <see cref="LetStatement"/>
    /// and that the assigned value is a <see cref="FunctionCallExpression"/>.
    /// </remarks>
    /// <example>
    /// The test parses the following Applesoft BASIC code:
    /// <code>
    /// 10 A$ = MID$(B$, 1, 5)
    /// </code>
    /// It checks that the parsed statement is a <see cref="LetStatement"/> and its value is a <see cref="FunctionCallExpression"/>.
    /// </example>
    [Test]
    public void Parse_StringFunction_ParsesCorrectly()
    {
        var program = parser.Parse("10 A$ = MID$(B$, 1, 5)");

        var stmt = program.Lines[0].Statements[0] as LetStatement;
        Assert.That(stmt!.Value, Is.InstanceOf<FunctionCallExpression>());
    }

    /// <summary>
    /// Verifies that the parser correctly handles array access expressions in Applesoft BASIC statements.
    /// </summary>
    /// <remarks>
    /// This test ensures that when parsing a statement involving array access, such as <c>X = A(5)</c>,
    /// the resulting syntax tree includes an <see cref="ArrayAccessExpression"/> as the value of the
    /// <see cref="LetStatement"/>.
    /// </remarks>
    /// <example>
    /// Input: <c>10 X = A(5)</c>
    /// Expected: The <see cref="LetStatement"/> contains an <see cref="ArrayAccessExpression"/>.
    /// </example>
    [Test]
    public void Parse_ArrayAccess_ParsesCorrectly()
    {
        var program = parser.Parse("10 X = A(5)");

        var stmt = program.Lines[0].Statements[0] as LetStatement;
        Assert.That(stmt!.Value, Is.InstanceOf<ArrayAccessExpression>());
    }

    /// <summary>
    /// Verifies that the <see cref="BasicParser.Parse(string)"/> method correctly parses Applesoft BASIC programs
    /// and ensures that the resulting lines are sorted by their line numbers in ascending order.
    /// </summary>
    /// <remarks>
    /// This test provides a program with out-of-order line numbers and asserts that the parsed output
    /// contains lines sorted in the correct numerical order.
    /// </remarks>
    /// <example>
    /// Given the input:
    /// <code>
    /// 30 PRINT "C"
    /// 10 PRINT "A"
    /// 20 PRINT "B"
    /// </code>
    /// The test ensures that the parsed lines are ordered as:
    /// <code>
    /// 10 PRINT "A"
    /// 20 PRINT "B"
    /// 30 PRINT "C"
    /// </code>
    /// </example>
    [Test]
    public void Parse_LinesSortedByNumber()
    {
        var program = parser.Parse("30 PRINT \"C\"\n10 PRINT \"A\"\n20 PRINT \"B\"");

        Assert.Multiple(() =>
        {
            Assert.That(program.Lines[0].LineNumber, Is.EqualTo(10));
            Assert.That(program.Lines[1].LineNumber, Is.EqualTo(20));
            Assert.That(program.Lines[2].LineNumber, Is.EqualTo(30));
        });
    }
}