// <copyright file="BasicLexer.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Lexer;

using Tokens;
using Microsoft.Extensions.Logging;

/// <summary>
/// Tokenizer for Applesoft BASIC source code.
/// </summary>
public class BasicLexer : ILexer
{
    private readonly ILogger<BasicLexer> _logger;
    private string _source = string.Empty;
    private readonly List<Token> _tokens = new();
    private int _start;
    private int _current;
    private int _line;
    private int _column;
    private int _startColumn;

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
        { "SLEEP", TokenType.SLEEP }
    };

    public BasicLexer(ILogger<BasicLexer> logger)
    {
        _logger = logger;
    }

    public List<Token> Tokenize(string source)
    {
        _source = source;
        _tokens.Clear();
        _start = 0;
        _current = 0;
        _line = 1;
        _column = 1;

        _logger.LogDebug("Starting tokenization of {Length} characters", source.Length);

        while (!IsAtEnd())
        {
            _start = _current;
            _startColumn = _column;
            ScanToken();
        }

        _tokens.Add(new Token(TokenType.EOF, "", null, _line, _column));

        _logger.LogDebug("Tokenization complete. Generated {Count} tokens", _tokens.Count);

        return _tokens;
    }

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

            case '=': AddToken(TokenType.Equal); break;

            case '<':
                if (Match('='))
                    AddToken(TokenType.LessOrEqual);
                else if (Match('>'))
                    AddToken(TokenType.NotEqual);
                else
                    AddToken(TokenType.LessThan);
                break;

            case '>':
                if (Match('='))
                    AddToken(TokenType.GreaterOrEqual);
                else if (Match('<'))
                    AddToken(TokenType.NotEqual);
                else
                    AddToken(TokenType.GreaterThan);
                break;

            case '"': ScanString(); break;

            case ' ':
            case '\t':
            case '\r':
                // Ignore whitespace
                break;

            case '\n':
                AddToken(TokenType.Newline);
                _line++;
                _column = 1;
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
                    _logger.LogWarning("Unexpected character '{Char}' at line {Line}, column {Column}", c, _line, _startColumn);
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
            _logger.LogWarning("Unterminated string at line {Line}", _line);
            AddToken(TokenType.String, _source.Substring(_start + 1, _current - _start - 1));
            return;
        }

        // Consume the closing "
        Advance();

        // Extract the string value (without quotes)
        string value = _source.Substring(_start + 1, _current - _start - 2);
        AddToken(TokenType.String, value);
    }

    private void ScanNumber()
    {
        // Handle numbers starting with decimal point
        bool hasDecimal = _source[_start] == '.';

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

        string text = _source.Substring(_start, _current - _start);
        if (double.TryParse(text, out double value))
        {
            AddToken(TokenType.Number, value);
        }
        else
        {
            _logger.LogError("Failed to parse number: {Text}", text);
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

        string text = _source.Substring(_start, _current - _start);

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

    private bool IsAtEnd() => _current >= _source.Length;

    private char Advance()
    {
        _column++;
        return _source[_current++];
    }

    private char Peek() => IsAtEnd() ? '\0' : _source[_current];

    private char PeekNext() => _current + 1 >= _source.Length ? '\0' : _source[_current + 1];

    private bool Match(char expected)
    {
        if (IsAtEnd()) return false;
        if (_source[_current] != expected) return false;

        _current++;
        _column++;
        return true;
    }

    private void AddToken(TokenType type, object? value = null)
    {
        string text = _source.Substring(_start, _current - _start);
        _tokens.Add(new Token(type, text, value, _line, _startColumn));
    }

    private static bool IsDigit(char c) => c >= '0' && c <= '9';

    private static bool IsAlpha(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');

    private static bool IsAlphaNumeric(char c) => IsAlpha(c) || IsDigit(c);
}
