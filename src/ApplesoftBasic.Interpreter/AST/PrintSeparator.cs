// <copyright file="PrintSeparator.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// Represents the types of separators used in PRINT statements in Applesoft BASIC.
/// </summary>
/// <summary>
/// No separator is used between expressions.
/// </summary>
/// <summary>
/// A comma separator, which aligns output to the next 16-column zone.
/// </summary>
/// <summary>
/// A semicolon separator, which outputs expressions without additional spacing.
/// </summary>
public enum PrintSeparator
{
    /// <summary>
    /// Represents the absence of a separator between items in a PRINT statement.
    /// </summary>
    None,

    /// <summary>
    /// Represents a comma (,) separator in a PRINT statement.
    /// </summary>
    /// <remarks>
    /// A comma separator aligns the output to the next 16-column zone
    /// when used in a PRINT statement.
    /// </remarks>
    Comma, // Tab to next column

    /// <summary>
    /// Represents a semicolon (;) used as a separator in a PRINT statement.
    /// </summary>
    /// <remarks>
    /// When a semicolon is used as a separator, no additional space is added between the printed items.
    /// </remarks>
    Semicolon, // No space
}