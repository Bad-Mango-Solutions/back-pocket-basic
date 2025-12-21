// <copyright file="BasicLexer.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.Lexer;

using Microsoft.Extensions.Logging;

using Tokens;

/// <summary>
/// Tokenizer for Applesoft BASIC source code.
/// </summary>
public class BasicLexer : ILexer
{
    /// <summary>
    /// A dictionary containing the mapping of Applesoft BASIC keywords to their corresponding
    /// <see cref="TokenType"/> values. This dictionary is case-insensitive and is used by the lexer
    /// to identify and tokenize keywords in Applesoft BASIC source code.
    /// </summary>
    private static readonly Dictionary<string, TokenType> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Program Control
        { "REM", TokenType.REM },
        { "LET", TokenType.LET },
        { "DIM", TokenType.DIM },
        { "DEF", TokenType.DEF },
        { "FN", TokenType.FN },
        { "END", TokenType.END },
        { "STOP", TokenType.STOP },

        // Flow Control
        { "GOTO", TokenType.GOTO },
        { "GO TO", TokenType.GOTO },
        { "GOSUB", TokenType.GOSUB },
        { "RETURN", TokenType.RETURN },
        { "ON", TokenType.ON },
        { "IF", TokenType.IF },
        { "THEN", TokenType.THEN },
        { "ELSE", TokenType.ELSE },
        { "FOR", TokenType.FOR },
        { "TO", TokenType.TO },
        { "STEP", TokenType.STEP },
        { "NEXT", TokenType.NEXT },

        // I/O
        { "PRINT", TokenType.PRINT },
        { "INPUT", TokenType.INPUT },
        { "GET", TokenType.GET },
        { "DATA", TokenType.DATA },
        { "READ", TokenType.READ },
        { "RESTORE", TokenType.RESTORE },

        // Graphics (stubbed)
        { "GR", TokenType.GR },
        { "HGR", TokenType.HGR },
        { "HGR2", TokenType.HGR2 },
        { "TEXT", TokenType.TEXT },
        { "COLOR", TokenType.COLOR },
        { "HCOLOR", TokenType.HCOLOR },
        { "PLOT", TokenType.PLOT },
        { "HPLOT", TokenType.HPLOT },
        { "DRAW", TokenType.DRAW },
        { "XDRAW", TokenType.XDRAW },
        { "HTAB", TokenType.HTAB },
        { "VTAB", TokenType.VTAB },
        { "HOME", TokenType.HOME },
        { "INVERSE", TokenType.INVERSE },
        { "FLASH", TokenType.FLASH },
        { "NORMAL", TokenType.NORMAL },

        // Memory/System
        { "PEEK", TokenType.PEEK },
        { "POKE", TokenType.POKE },
        { "CALL", TokenType.CALL },
        { "HIMEM", TokenType.HIMEM },
        { "LOMEM", TokenType.LOMEM },
        { "CLEAR", TokenType.CLEAR },
        { "NEW", TokenType.NEW },
        { "RUN", TokenType.RUN },
        { "LIST", TokenType.LIST },
        { "CONT", TokenType.CONT },

        // String Functions
        { "MID$", TokenType.MID_S },
        { "LEFT$", TokenType.LEFT_S },
        { "RIGHT$", TokenType.RIGHT_S },
        { "LEN", TokenType.LEN },
        { "VAL", TokenType.VAL },
        { "STR$", TokenType.STR_S },
        { "CHR$", TokenType.CHR_S },
        { "ASC", TokenType.ASC },

        // Math Functions
        { "ABS", TokenType.ABS },
        { "ATN", TokenType.ATN },
        { "COS", TokenType.COS },
        { "EXP", TokenType.EXP },
        { "INT", TokenType.INT },
        { "LOG", TokenType.LOG },
        { "RND", TokenType.RND },
        { "SGN", TokenType.SGN },
        { "SIN", TokenType.SIN },
        { "SQR", TokenType.SQR },
        { "TAN", TokenType.TAN },

        // Utility Functions
        { "FRE", TokenType.FRE },
        { "POS", TokenType.POS },
        { "SCRN", TokenType.SCRN },
        { "PDL", TokenType.PDL },
        { "USR", TokenType.USR },

        // Other
        { "TAB", TokenType.TAB },
        { "SPC", TokenType.SPC },
        { "NOT", TokenType.NOT },
        { "AND", TokenType.AND },
        { "OR", TokenType.OR },

        // File I/O
        { "OPEN", TokenType.OPEN },
        { "CLOSE", TokenType.CLOSE },
        { "ONERR", TokenType.ONERR },
        { "RESUME", TokenType.RESUME },

        // Custom extension
        { "SLEEP", TokenType.SLEEP },
    };

    private readonly ILogger<BasicLexer> logger;
    private readonly List<Token> tokens = [];
    private string source = string.Empty;
    private int start;
    private int current;
    private int line;
    private int column;
    private int startColumn;

    /// <summary>
    /// Initializes a new instance of the <see cref="BasicLexer"/> class.
    /// </summary>
    /// <param name="logger">
    /// The logger instance used for logging diagnostic and runtime information.
    /// </param>
    public BasicLexer(ILogger<BasicLexer> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// Tokenizes the provided Applesoft BASIC source code into a list of tokens.
    /// </summary>
    /// <param name="source">The source code to tokenize.</param>
    /// <returns>A list of <see cref="Token"/> objects representing the tokens in the source code.</returns>
    /// <remarks>
    /// This method processes the provided source code, breaking it down into individual tokens
    /// that can be used for further parsing or interpretation. It ensures that an EOF token
    /// is always added at the end of the token list.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="source"/> is <c>null</c>.</exception>
    /// <example>
    /// Example usage:
    /// <code>
    /// var lexer = new BasicLexer();
    /// var tokens = lexer.Tokenize("10 PRINT \"HELLO\"");
    /// </code>
    /// </example>
    public List<Token> Tokenize(string source)
    {
        this.source = source;
        tokens.Clear();
        start = 0;
        current = 0;
        line = 1;
        column = 1;

        logger.LogDebug("Starting tokenization of {Length} characters", source.Length);

        while (!IsAtEnd())
        {
            start = current;
            startColumn = column;
            ScanToken();
        }

        tokens.Add(new(TokenType.EOF, string.Empty, null, line, column));

        logger.LogDebug("Tokenization complete. Generated {Count} tokens", tokens.Count);

        return tokens;
    }

    private static bool IsDigit(char c) => c >= '0' && c <= '9';

    private static bool IsAlpha(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');

    private static bool IsAlphaNumeric(char c) => IsAlpha(c) || IsDigit(c);

    private void ScanToken()
    {
        char c = Advance();

        switch (c)
        {
            case '(': AddToken(TokenType.LeftParen); break;
            case ')': AddToken(TokenType.RightParen); break;
            case ',': AddToken(TokenType.Comma); break;
            case ';': AddToken(TokenType.Semicolon); break;
            case ':': AddToken(TokenType.Colon); break;
            case '+': AddToken(TokenType.Plus); break;
            case '-': AddToken(TokenType.Minus); break;
            case '*': AddToken(TokenType.Multiply); break;
            case '/': AddToken(TokenType.Divide); break;
            case '^': AddToken(TokenType.Power); break;
            case '#': AddToken(TokenType.Hash); break;
            case '@': AddToken(TokenType.At); break;
            case '?': AddToken(TokenType.PRINT); break; // ? is shorthand for PRINT
            case '&': AddToken(TokenType.AMPERSAND); break;

            case '=': AddToken(TokenType.Equal); break;

            case '<':
                if (Match('='))
                {
                    AddToken(TokenType.LessOrEqual);
                }
                else if (Match('>'))
                {
                    AddToken(TokenType.NotEqual);
                }
                else
                {
                    AddToken(TokenType.LessThan);
                }

                break;

            case '>':
                if (Match('='))
                {
                    AddToken(TokenType.GreaterOrEqual);
                }
                else if (Match('<'))
                {
                    AddToken(TokenType.NotEqual);
                }
                else
                {
                    AddToken(TokenType.GreaterThan);
                }

                break;

            case '"': ScanString(); break;

            case ' ':
            case '\t':
            case '\r':
                // Ignore whitespace
                break;

            case '\n':
                AddToken(TokenType.Newline);
                line++;
                column = 1;
                break;

            default:
                if (IsDigit(c) || (c == '.' && IsDigit(Peek())))
                {
                    ScanNumber();
                }
                else if (IsAlpha(c))
                {
                    ScanIdentifierOrKeyword();
                }
                else
                {
                    logger.LogWarning("Unexpected character '{Char}' at line {Line}, column {Column}", c, line, startColumn);
                    AddToken(TokenType.Unknown);
                }

                break;
        }
    }

    private void ScanString()
    {
        while (Peek() != '"' && !IsAtEnd() && Peek() != '\n')
        {
            Advance();
        }

        if (IsAtEnd() || Peek() == '\n')
        {
            logger.LogWarning("Unterminated string at line {Line}", line);
            AddToken(TokenType.String, source.Substring(start + 1, current - start - 1));
            return;
        }

        // Consume the closing "
        Advance();

        // Extract the string value (without quotes)
        string value = source.Substring(start + 1, current - start - 2);
        AddToken(TokenType.String, value);
    }

    private void ScanNumber()
    {
        // Handle numbers starting with decimal point
        bool hasDecimal = source[start] == '.';

        while (IsDigit(Peek()))
        {
            Advance();
        }

        // Look for decimal part
        if (!hasDecimal && Peek() == '.' && IsDigit(PeekNext()))
        {
            Advance(); // consume the '.'
            while (IsDigit(Peek()))
            {
                Advance();
            }
        }

        // Look for exponent
        if (Peek() == 'E' || Peek() == 'e')
        {
            Advance();
            if (Peek() == '+' || Peek() == '-')
            {
                Advance();
            }

            while (IsDigit(Peek()))
            {
                Advance();
            }
        }

        string text = source.Substring(start, current - start);
        if (double.TryParse(text, out double value))
        {
            AddToken(TokenType.Number, value);
        }
        else
        {
            logger.LogError("Failed to parse number: {Text}", text);
            AddToken(TokenType.Unknown);
        }
    }

    private void ScanIdentifierOrKeyword()
    {
        // Applesoft BASIC identifiers can contain letters and digits
        // String variables end with $, integer variables end with %
        while (IsAlphaNumeric(Peek()))
        {
            Advance();
        }

        // Check for $ or % suffix (string or integer variable)
        if (Peek() == '$' || Peek() == '%')
        {
            Advance();
        }

        string text = source.Substring(start, current - start);

        // Check if it's a keyword (but not if it ends with $ or %)
        if (!text.EndsWith('$') && !text.EndsWith('%') && Keywords.TryGetValue(text, out TokenType type))
        {
            AddToken(type);
        }

        // Special handling for string function keywords that end with $
        else if (text.EndsWith('$'))
        {
            string keywordCandidate = text;
            if (Keywords.TryGetValue(keywordCandidate, out TokenType funcType))
            {
                AddToken(funcType);
            }
            else
            {
                // It's a string variable
                AddToken(TokenType.Identifier, text);
            }
        }
        else
        {
            AddToken(TokenType.Identifier, text);
        }
    }

    private bool IsAtEnd() => current >= source.Length;

    private char Advance()
    {
        column++;
        return source[current++];
    }

    private char Peek() => IsAtEnd() ? '\0' : source[current];

    private char PeekNext() => current + 1 >= source.Length ? '\0' : source[current + 1];

    private bool Match(char expected)
    {
        if (IsAtEnd())
        {
            return false;
        }

        if (source[current] != expected)
        {
            return false;
        }

        current++;
        column++;
        return true;
    }

    private void AddToken(TokenType type, object? value = null)
    {
        string text = source.Substring(start, current - start);
        tokens.Add(new(type, text, value, line, startColumn));
    }
}