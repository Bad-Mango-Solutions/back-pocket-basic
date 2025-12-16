// <copyright file="Token.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Tokens;

/// <summary>
/// Represents a single token from the BASIC source code.
/// </summary>
public class Token
{
    public TokenType Type { get; }

    public string Lexeme { get; }

    public object? Value { get; }

    public int Line { get; }

    public int Column { get; }

    public Token(TokenType type, string lexeme, object? value, int line, int column)
    {
        Type = type;
        Lexeme = lexeme;
        Value = value;
        Line = line;
        Column = column;
    }

    public override string ToString()
    {
        return Value != null
            ? $"[{Type}] '{Lexeme}' = {Value} @ {Line}:{Column}"
            : $"[{Type}] '{Lexeme}' @ {Line}:{Column}";
    }
}
