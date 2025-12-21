// <copyright file="PrintStatement.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// Represents a PRINT statement in an Applesoft BASIC program.
/// </summary>
/// <remarks>
/// The <see cref="PrintStatement"/> class is part of the Abstract Syntax Tree (AST) and is used to model
/// the PRINT statement in Applesoft BASIC. It contains the expressions to be printed, separators between them,
/// and a flag indicating whether the statement ends with a separator.
/// </remarks>
public class PrintStatement : IStatement
{
    /// <summary>
    /// Gets the list of expressions to be evaluated and printed by the PRINT statement.
    /// </summary>
    /// <remarks>
    /// Each expression in this list represents a value or function call to be processed
    /// and output during the execution of the PRINT statement. This may include numeric
    /// values, strings, or function calls like TAB and SPC.
    /// </remarks>
    public List<IExpression> Expressions { get; } = [];

    /// <summary>
    /// Gets the list of separators used between expressions in the PRINT statement.
    /// </summary>
    /// <remarks>
    /// Each separator corresponds to the spacing or alignment behavior between expressions.
    /// Possible values include:
    /// <see cref="PrintSeparator.None"/> for no separator,
    /// <see cref="PrintSeparator.Comma"/> for alignment to the next 16-column zone,
    /// and <see cref="PrintSeparator.Semicolon"/> for no additional spacing.
    /// </remarks>
    public List<PrintSeparator> Separators { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the PRINT statement ends with a separator.
    /// </summary>
    /// <remarks>
    /// If <c>true</c>, the PRINT statement ends with a separator, such as a comma or semicolon.
    /// If <c>false</c>, the PRINT statement does not end with a separator, and a newline is printed.
    /// </remarks>
    public bool EndsWithSeparator { get; set; }

    /// <summary>
    /// Accepts a visitor that implements the <see cref="IAstVisitor{T}"/> interface.
    /// </summary>
    /// <typeparam name="T">The type of the result returned by the visitor.</typeparam>
    /// <param name="visitor">The visitor instance that processes this <see cref="PrintStatement"/> node.</param>
    /// <returns>The result of the visitor's <see cref="IAstVisitor{T}.VisitPrintStatement"/> method.</returns>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitPrintStatement(this);
}