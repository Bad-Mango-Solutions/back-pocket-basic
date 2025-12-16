// <copyright file="LexerTests.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Tests;

using Interpreter.Lexer;
using Interpreter.Tokens;

using Microsoft.Extensions.Logging;

using Moq;

/// <summary>
/// Contains unit tests for the <see cref="BasicLexer"/> class, ensuring its functionality for tokenizing Applesoft BASIC source code.
/// </summary>
/// <remarks>
/// This test class verifies the correct behavior of the lexer by testing various scenarios, including handling of keywords, operators,
/// literals, and special cases in Applesoft BASIC syntax.
/// </remarks>
[TestFixture]
public class LexerTests
{
    private BasicLexer lexer = null!;

    /// <summary>
    /// Sets up the test environment for the <see cref="LexerTests"/> class.
    /// </summary>
    /// <remarks>
    /// This method is executed before each test in the <see cref="LexerTests"/> class.
    /// It initializes the <see cref="BasicLexer"/> instance with a mocked logger to ensure
    /// a consistent and isolated testing environment.
    /// </remarks>
    [SetUp]
    public void Setup()
    {
        var mockLogger = new Mock<ILogger<BasicLexer>>();
        lexer = new(mockLogger.Object);
    }

    /// <summary>
    /// Tests that the <see cref="BasicLexer.Tokenize(string)"/> method correctly handles an empty source string
    /// by returning a single token of type <see cref="TokenType.EOF"/>.
    /// </summary>
    /// <remarks>
    /// This test ensures that the lexer can handle the edge case of an empty input without errors
    /// and produces the expected end-of-file token.
    /// </remarks>
    [Test]
    public void Tokenize_EmptySource_ReturnsOnlyEOF()
    {
        var tokens = lexer.Tokenize(string.Empty);

        Assert.That(tokens, Has.Count.EqualTo(1));
        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.EOF));
    }

    /// <summary>
    /// Tests whether the <see cref="BasicLexer.Tokenize(string)"/> method correctly tokenizes a line number in Applesoft BASIC source code.
    /// </summary>
    /// <remarks>
    /// This test ensures that a single line number, such as "10", is correctly identified as a <see cref="TokenType.Number"/>
    /// and that its value is accurately parsed.
    /// </remarks>
    /// <example>
    /// Input: "10"
    /// Expected Tokens:
    /// 1. A token of type <see cref="TokenType.Number"/> with a value of 10.0.
    /// 2. A token of type <see cref="TokenType.EOF"/>.
    /// </example>
    [Test]
    public void Tokenize_LineNumber_ReturnsNumber()
    {
        var tokens = lexer.Tokenize("10");

        Assert.That(tokens, Has.Count.EqualTo(2));
        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Number));
        Assert.That(tokens[0].Value, Is.EqualTo(10.0));
    }

    /// <summary>
    /// Tests the <see cref="BasicLexer.Tokenize"/> method to ensure that it correctly tokenizes
    /// a PRINT statement containing a line number, a PRINT keyword, and a string literal.
    /// </summary>
    /// <remarks>
    /// This test verifies that the lexer produces the expected tokens for the input "10 PRINT \"HELLO\"".
    /// It checks that:
    /// <list type="bullet">
    /// <item>The first token is a number representing the line number.</item>
    /// <item>The second token is the PRINT keyword.</item>
    /// <item>The third token is a string literal with the value "HELLO".</item>
    /// </list>
    /// </remarks>
    [Test]
    public void Tokenize_PrintStatement_ReturnsCorrectTokens()
    {
        var tokens = lexer.Tokenize("10 PRINT \"HELLO\"");

        Assert.That(tokens, Has.Count.GreaterThan(3));
        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Number));
        Assert.That(tokens[1].Type, Is.EqualTo(TokenType.PRINT));
        Assert.That(tokens[2].Type, Is.EqualTo(TokenType.String));
        Assert.That(tokens[2].Value, Is.EqualTo("HELLO"));
    }

    /// <summary>
    /// Tests whether the <c>?</c> symbol in Applesoft BASIC source code is correctly recognized as the <see cref="TokenType.PRINT"/> token.
    /// </summary>
    /// <remarks>
    /// This test ensures that the shorthand <c>?</c> for the <c>PRINT</c> statement is properly tokenized by the <see cref="BasicLexer"/>.
    /// </remarks>
    [Test]
    public void Tokenize_QuestionMark_RecognizedAsPrint()
    {
        var tokens = lexer.Tokenize("10 ? \"TEST\"");

        Assert.That(tokens[1].Type, Is.EqualTo(TokenType.PRINT));
    }

    /// <summary>
    /// Tests whether the <see cref="BasicLexer.Tokenize(string)"/> method correctly recognizes
    /// all mathematical operators (+, -, *, /, ^) in the source code.
    /// </summary>
    /// <remarks>
    /// This test ensures that tokens for addition, subtraction, multiplication, division,
    /// and exponentiation are properly identified and included in the tokenized output.
    /// </remarks>
    [Test]
    public void Tokenize_MathOperators_RecognizesAll()
    {
        var tokens = lexer.Tokenize("1 + 2 - 3 * 4 / 5 ^ 6");

        Assert.That(tokens.Any(t => t.Type == TokenType.Plus));
        Assert.That(tokens.Any(t => t.Type == TokenType.Minus));
        Assert.That(tokens.Any(t => t.Type == TokenType.Multiply));
        Assert.That(tokens.Any(t => t.Type == TokenType.Divide));
        Assert.That(tokens.Any(t => t.Type == TokenType.Power));
    }

    /// <summary>
    /// Tests whether the <see cref="BasicLexer"/> correctly recognizes all comparison operators
    /// (e.g., '=', '&lt;', '&gt;', '&lt;=', '&gt;=', '&lt;&gt;') in a given source string.
    /// </summary>
    /// <remarks>
    /// This test verifies that the lexer produces tokens with the appropriate <see cref="TokenType"/>
    /// for each comparison operator present in the input string.
    /// </remarks>
    /// <example>
    /// Input: <c>"A = B &lt; C > DP &lt;= E >= F &lt;&gt; G"</c>
    /// Expected Tokens: <see cref="TokenType.Equal"/>, <see cref="TokenType.LessThan"/>,
    /// <see cref="TokenType.GreaterThan"/>, <see cref="TokenType.LessOrEqual"/>,
    /// <see cref="TokenType.GreaterOrEqual"/>, <see cref="TokenType.NotEqual"/>.
    /// </example>
    [Test]
    public void Tokenize_ComparisonOperators_RecognizesAll()
    {
        var tokens = lexer.Tokenize("A = B < C > DP <= E >= F <> G");

        Assert.That(tokens.Any(t => t.Type == TokenType.Equal));
        Assert.That(tokens.Any(t => t.Type == TokenType.LessThan));
        Assert.That(tokens.Any(t => t.Type == TokenType.GreaterThan));
        Assert.That(tokens.Any(t => t.Type == TokenType.LessOrEqual));
        Assert.That(tokens.Any(t => t.Type == TokenType.GreaterOrEqual));
        Assert.That(tokens.Any(t => t.Type == TokenType.NotEqual));
    }

    /// <summary>
    /// Tests whether the <see cref="BasicLexer"/> correctly recognizes all Applesoft BASIC keywords
    /// and tokenizes them into their respective <see cref="TokenType"/> values.
    /// </summary>
    /// <remarks>
    /// This test ensures that the lexer identifies keywords such as PRINT, INPUT, LET, DIM, FOR, TO,
    /// STEP, NEXT, IF, THEN, GOTO, GOSUB, RETURN, and END, and assigns the appropriate token types.
    /// </remarks>
    /// <seealso cref="BasicLexer.Tokenize(string)"/>
    [Test]
    public void Tokenize_Keywords_RecognizesAll()
    {
        var source = "PRINT INPUT LET DIM FOR TO STEP NEXT IF THEN GOTO GOSUB RETURN END";
        var tokens = lexer.Tokenize(source);

        Assert.That(tokens.Any(t => t.Type == TokenType.PRINT));
        Assert.That(tokens.Any(t => t.Type == TokenType.INPUT));
        Assert.That(tokens.Any(t => t.Type == TokenType.LET));
        Assert.That(tokens.Any(t => t.Type == TokenType.DIM));
        Assert.That(tokens.Any(t => t.Type == TokenType.FOR));
        Assert.That(tokens.Any(t => t.Type == TokenType.TO));
        Assert.That(tokens.Any(t => t.Type == TokenType.STEP));
        Assert.That(tokens.Any(t => t.Type == TokenType.NEXT));
        Assert.That(tokens.Any(t => t.Type == TokenType.IF));
        Assert.That(tokens.Any(t => t.Type == TokenType.THEN));
        Assert.That(tokens.Any(t => t.Type == TokenType.GOTO));
        Assert.That(tokens.Any(t => t.Type == TokenType.GOSUB));
        Assert.That(tokens.Any(t => t.Type == TokenType.RETURN));
        Assert.That(tokens.Any(t => t.Type == TokenType.END));
    }

    /// <summary>
    /// Tests that the <see cref="BasicLexer.Tokenize(string)"/> method correctly tokenizes a string variable
    /// in Applesoft BASIC, ensuring that the dollar sign ('$') is included as part of the identifier.
    /// </summary>
    /// <remarks>
    /// This test verifies that the lexer recognizes string variables, which are denoted by a trailing dollar sign ('$'),
    /// and correctly assigns them the <see cref="TokenType.Identifier"/> type.
    /// </remarks>
    /// <example>
    /// For example, given the input <c>"A$"</c>, the lexer should produce a token with:
    /// <list type="bullet">
    /// <item><description>Type: <see cref="TokenType.Identifier"/></description></item>
    /// <item><description>Lexeme: <c>"A$"</c></description></item>
    /// </list>
    /// </example>
    [Test]
    public void Tokenize_StringVariable_IncludesDollarSign()
    {
        var tokens = lexer.Tokenize("A$");

        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Identifier));
        Assert.That(tokens[0].Lexeme, Is.EqualTo("A$"));
    }

    /// <summary>
    /// Verifies that the <see cref="BasicLexer"/> correctly tokenizes an integer variable
    /// by including the percent sign (%) as part of the identifier.
    /// </summary>
    /// <remarks>
    /// This test ensures that integer variables in Applesoft BASIC, which are denoted
    /// by a trailing percent sign (%), are properly recognized and tokenized as identifiers.
    /// </remarks>
    [Test]
    public void Tokenize_IntegerVariable_IncludesPercent()
    {
        var tokens = lexer.Tokenize("N%");

        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Identifier));
        Assert.That(tokens[0].Lexeme, Is.EqualTo("N%"));
    }

    /// <summary>
    /// Tests whether the <see cref="BasicLexer.Tokenize(string)"/> method correctly recognizes
    /// string functions in the Applesoft BASIC language.
    /// </summary>
    /// <remarks>
    /// This test verifies that the following string functions are tokenized correctly:
    /// <list type="bullet">
    /// <item><description><c>MID$</c></description></item>
    /// <item><description><c>LEFT$</c></description></item>
    /// <item><description><c>RIGHT$</c></description></item>
    /// <item><description><c>CHR$</c></description></item>
    /// <item><description><c>STR$</c></description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="TokenType.MID_S"/>
    /// <seealso cref="TokenType.LEFT_S"/>
    /// <seealso cref="TokenType.RIGHT_S"/>
    /// <seealso cref="TokenType.CHR_S"/>
    /// <seealso cref="TokenType.STR_S"/>
    [Test]
    public void Tokenize_StringFunctions_Recognized()
    {
        var tokens = lexer.Tokenize("MID$ LEFT$ RIGHT$ CHR$ STR$");

        Assert.That(tokens.Any(t => t.Type == TokenType.MID_S));
        Assert.That(tokens.Any(t => t.Type == TokenType.LEFT_S));
        Assert.That(tokens.Any(t => t.Type == TokenType.RIGHT_S));
        Assert.That(tokens.Any(t => t.Type == TokenType.CHR_S));
        Assert.That(tokens.Any(t => t.Type == TokenType.STR_S));
    }

    /// <summary>
    /// Tests whether the lexer correctly recognizes and tokenizes mathematical functions.
    /// </summary>
    /// <remarks>
    /// This test verifies that the lexer identifies tokens corresponding to the following mathematical functions:
    /// <list type="bullet">
    /// <item><description>ABS</description></item>
    /// <item><description>SIN</description></item>
    /// <item><description>COS</description></item>
    /// <item><description>TAN</description></item>
    /// <item><description>ATN</description></item>
    /// <item><description>LOG</description></item>
    /// <item><description>EXP</description></item>
    /// <item><description>SQR</description></item>
    /// <item><description>INT</description></item>
    /// <item><description>RND</description></item>
    /// <item><description>SGN</description></item>
    /// </list>
    /// </remarks>
    [Test]
    public void Tokenize_MathFunctions_Recognized()
    {
        var tokens = lexer.Tokenize("ABS SIN COS TAN ATN LOG EXP SQR INT RND SGN");

        Assert.That(tokens.Any(t => t.Type == TokenType.ABS));
        Assert.That(tokens.Any(t => t.Type == TokenType.SIN));
        Assert.That(tokens.Any(t => t.Type == TokenType.COS));
        Assert.That(tokens.Any(t => t.Type == TokenType.TAN));
        Assert.That(tokens.Any(t => t.Type == TokenType.ATN));
        Assert.That(tokens.Any(t => t.Type == TokenType.LOG));
        Assert.That(tokens.Any(t => t.Type == TokenType.EXP));
        Assert.That(tokens.Any(t => t.Type == TokenType.SQR));
        Assert.That(tokens.Any(t => t.Type == TokenType.INT));
        Assert.That(tokens.Any(t => t.Type == TokenType.RND));
        Assert.That(tokens.Any(t => t.Type == TokenType.SGN));
    }

    /// <summary>
    /// Tests whether the <see cref="BasicLexer.Tokenize(string)"/> method correctly parses floating-point numbers.
    /// </summary>
    /// <remarks>
    /// This test ensures that floating-point numbers, such as "3.14159", are tokenized as numbers with the correct value
    /// and precision within the expected tolerance.
    /// </remarks>
    /// <example>
    /// For example, the input "3.14159" should produce a token of type <see cref="TokenType.Number"/> with a value of 3.14159.
    /// </example>
    [Test]
    public void Tokenize_FloatingPoint_ParsedCorrectly()
    {
        var tokens = lexer.Tokenize("3.14159");

        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Number));
        Assert.That(tokens[0].Value, Is.EqualTo(3.14159).Within(0.00001));
    }

    /// <summary>
    /// Tests the ability of the <see cref="BasicLexer"/> to correctly parse scientific notation numbers.
    /// </summary>
    /// <remarks>
    /// This test ensures that numbers written in scientific notation, such as "1.5E10", are tokenized
    /// correctly as a <see cref="TokenType.Number"/> with the appropriate value.
    /// </remarks>
    /// <example>
    /// For example, the input "1.5E10" should produce a token with:
    /// <list type="bullet">
    /// <item><description>Type: <see cref="TokenType.Number"/></description></item>
    /// <item><description>Value: 1.5e10</description></item>
    /// </list>
    /// </example>
    [Test]
    public void Tokenize_ScientificNotation_ParsedCorrectly()
    {
        var tokens = lexer.Tokenize("1.5E10");

        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Number));
        Assert.That(tokens[0].Value, Is.EqualTo(1.5e10));
    }

    /// <summary>
    /// Verifies that the <c>SLEEP</c> keyword is correctly tokenized by the <see cref="BasicLexer"/>.
    /// </summary>
    /// <remarks>
    /// This test ensures that the lexer recognizes the <c>SLEEP</c> keyword and assigns it the appropriate
    /// <see cref="TokenType.SLEEP"/> token type.
    /// </remarks>
    /// <example>
    /// Given the source code containing the keyword <c>SLEEP</c>, the lexer should produce a token
    /// with <see cref="Token.Type"/> set to <see cref="TokenType.SLEEP"/>.
    /// </example>
    [Test]
    public void Tokenize_CustomSleep_Recognized()
    {
        var tokens = lexer.Tokenize("SLEEP");

        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.SLEEP));
    }
}