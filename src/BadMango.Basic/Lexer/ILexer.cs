// <copyright file="ILexer.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.Lexer;

using Tokens;

/// <summary>
/// Interface for the BASIC lexer/tokenizer.
/// </summary>
public interface ILexer
{
    /// <summary>
    /// Tokenizes the source code into a list of tokens.
    /// </summary>
    /// <param name="source">The BASIC source code.</param>
    /// <returns>List of tokens.</returns>
    List<Token> Tokenize(string source);
}