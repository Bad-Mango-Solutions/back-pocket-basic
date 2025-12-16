// <copyright file="LexerTests.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Tests;

using Interpreter.Lexer;
using Interpreter.Tokens;
using Microsoft.Extensions.Logging;
using Moq;

[TestFixture]
public class LexerTests
{
    private BasicLexer _lexer = null!;

    [SetUp]
    public void Setup()
    {
        var mockLogger = new Mock<ILogger<BasicLexer>>();
        _lexer = new BasicLexer(mockLogger.Object);
    }

    [Test]
    public void Tokenize_EmptySource_ReturnsOnlyEOF()
    {
        var tokens = _lexer.Tokenize("");

        Assert.That(tokens, Has.Count.EqualTo(1));
        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.EOF));
    }

    [Test]
    public void Tokenize_LineNumber_ReturnsNumber()
    {
        var tokens = _lexer.Tokenize("10");

        Assert.That(tokens, Has.Count.EqualTo(2));
        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Number));
        Assert.That(tokens[0].Value, Is.EqualTo(10.0));
    }

    [Test]
    public void Tokenize_PrintStatement_ReturnsCorrectTokens()
    {
        var tokens = _lexer.Tokenize("10 PRINT \"HELLO\"");

        Assert.That(tokens, Has.Count.GreaterThan(3));
        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Number));
        Assert.That(tokens[1].Type, Is.EqualTo(TokenType.PRINT));
        Assert.That(tokens[2].Type, Is.EqualTo(TokenType.String));
        Assert.That(tokens[2].Value, Is.EqualTo("HELLO"));
    }

    [Test]
    public void Tokenize_QuestionMark_RecognizedAsPrint()
    {
        var tokens = _lexer.Tokenize("10 ? \"TEST\"");

        Assert.That(tokens[1].Type, Is.EqualTo(TokenType.PRINT));
    }

    [Test]
    public void Tokenize_MathOperators_RecognizesAll()
    {
        var tokens = _lexer.Tokenize("1 + 2 - 3 * 4 / 5 ^ 6");

        Assert.That(tokens.Any(t => t.Type == TokenType.Plus));
        Assert.That(tokens.Any(t => t.Type == TokenType.Minus));
        Assert.That(tokens.Any(t => t.Type == TokenType.Multiply));
        Assert.That(tokens.Any(t => t.Type == TokenType.Divide));
        Assert.That(tokens.Any(t => t.Type == TokenType.Power));
    }

    [Test]
    public void Tokenize_ComparisonOperators_RecognizesAll()
    {
        var tokens = _lexer.Tokenize("A = B < C > D <= E >= F <> G");

        Assert.That(tokens.Any(t => t.Type == TokenType.Equal));
        Assert.That(tokens.Any(t => t.Type == TokenType.LessThan));
        Assert.That(tokens.Any(t => t.Type == TokenType.GreaterThan));
        Assert.That(tokens.Any(t => t.Type == TokenType.LessOrEqual));
        Assert.That(tokens.Any(t => t.Type == TokenType.GreaterOrEqual));
        Assert.That(tokens.Any(t => t.Type == TokenType.NotEqual));
    }

    [Test]
    public void Tokenize_Keywords_RecognizesAll()
    {
        var source = "PRINT INPUT LET DIM FOR TO STEP NEXT IF THEN GOTO GOSUB RETURN END";
        var tokens = _lexer.Tokenize(source);

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

    [Test]
    public void Tokenize_StringVariable_IncludesDollarSign()
    {
        var tokens = _lexer.Tokenize("A$");

        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Identifier));
        Assert.That(tokens[0].Lexeme, Is.EqualTo("A$"));
    }

    [Test]
    public void Tokenize_IntegerVariable_IncludesPercent()
    {
        var tokens = _lexer.Tokenize("N%");

        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Identifier));
        Assert.That(tokens[0].Lexeme, Is.EqualTo("N%"));
    }

    [Test]
    public void Tokenize_StringFunctions_Recognized()
    {
        var tokens = _lexer.Tokenize("MID$ LEFT$ RIGHT$ CHR$ STR$");

        Assert.That(tokens.Any(t => t.Type == TokenType.MID_S));
        Assert.That(tokens.Any(t => t.Type == TokenType.LEFT_S));
        Assert.That(tokens.Any(t => t.Type == TokenType.RIGHT_S));
        Assert.That(tokens.Any(t => t.Type == TokenType.CHR_S));
        Assert.That(tokens.Any(t => t.Type == TokenType.STR_S));
    }

    [Test]
    public void Tokenize_MathFunctions_Recognized()
    {
        var tokens = _lexer.Tokenize("ABS SIN COS TAN ATN LOG EXP SQR INT RND SGN");

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

    [Test]
    public void Tokenize_FloatingPoint_ParsedCorrectly()
    {
        var tokens = _lexer.Tokenize("3.14159");

        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Number));
        Assert.That(tokens[0].Value, Is.EqualTo(3.14159).Within(0.00001));
    }

    [Test]
    public void Tokenize_ScientificNotation_ParsedCorrectly()
    {
        var tokens = _lexer.Tokenize("1.5E10");

        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Number));
        Assert.That(tokens[0].Value, Is.EqualTo(1.5e10));
    }

    [Test]
    public void Tokenize_CustomSleep_Recognized()
    {
        var tokens = _lexer.Tokenize("SLEEP");

        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.SLEEP));
    }
}
