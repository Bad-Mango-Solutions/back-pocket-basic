// <copyright file="Token.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.Tokens;

/// <summary>
/// Represents a single token extracted from Applesoft BASIC source code.
/// </summary>
/// <remarks>
/// A token is a fundamental unit of the source code, such as a keyword, identifier, operator, or literal.
/// This class encapsulates details about the token, including its type, textual representation, value,
/// and its position in the source code (line and column).
/// </remarks>
public class Token
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Token"/> class.
    /// </summary>
    /// <param name="type">The type of the token, representing its category (e.g., keyword, identifier, operator).</param>
    /// <param name="lexeme">The textual representation of the token as it appears in the source code.</param>
    /// <param name="value">The value associated with the token, if applicable (e.g., a number or string literal).</param>
    /// <param name="line">The line number in the source code where the token is located.</param>
    /// <param name="column">The column number in the source code where the token starts.</param>
    public Token(TokenType type, string lexeme, object? value, int line, int column)
    {
        Type = type;
        Lexeme = lexeme;
        Value = value;
        Line = line;
        Column = column;
    }

    /// <summary>
    /// Gets the type of the token.
    /// </summary>
    /// <value>
    /// One of the <see cref="TokenType"/> enumeration values, representing the kind of token,
    /// such as a keyword, operator, literal, or identifier.
    /// </value>
    /// <remarks>
    /// The token type is used to determine how the token should be interpreted and processed
    /// during parsing or execution of Applesoft BASIC source code.
    /// </remarks>
    public TokenType Type { get; }

    /// <summary>
    /// Gets the textual representation of the token as it appears in the source code.
    /// </summary>
    /// <remarks>
    /// The <see cref="Lexeme"/> property contains the exact sequence of characters
    /// that make up this token in the Applesoft BASIC source code. It is useful for
    /// reconstructing the original source or for error reporting.
    /// </remarks>
    public string Lexeme { get; }

    /// <summary>
    /// Gets the value associated with the token.
    /// </summary>
    /// <remarks>
    /// The value represents the interpreted or literal value of the token,
    /// which could be a number, string, or other data type depending on the token type.
    /// This property may be <c>null</c> if the token does not have an associated value.
    /// </remarks>
    public object? Value { get; }

    /// <summary>
    /// Gets the line number in the source code where this token is located.
    /// </summary>
    /// <remarks>
    /// This property indicates the line number in the Applesoft BASIC source code
    /// where the token was extracted. It is useful for error reporting and debugging
    /// to pinpoint the exact location of the token.
    /// </remarks>
    public int Line { get; }

    /// <summary>
    /// Gets the column number in the source code where this token is located.
    /// </summary>
    /// <remarks>
    /// The column number is 1-based and indicates the horizontal position of the token
    /// within its line in the source code.
    /// </remarks>
    public int Column { get; }

    /// <summary>
    /// Returns a string representation of the current <see cref="Token"/> instance.
    /// </summary>
    /// <returns>
    /// A string that includes the token's type, textual representation, value (if applicable),
    /// and its position in the source code (line and column).
    /// </returns>
    /// <remarks>
    /// The returned string is formatted as:
    /// <c>[Type] 'Lexeme' = Value @ Line:Column</c> if the token has a value,
    /// or <c>[Type] 'Lexeme' @ Line:Column</c> if it does not.
    /// </remarks>
    public override string ToString()
    {
        return Value != null
            ? $"[{Type}] '{Lexeme}' = {Value} @ {Line}:{Column}"
            : $"[{Type}] '{Lexeme}' @ {Line}:{Column}";
    }
}