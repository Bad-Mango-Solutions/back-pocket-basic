// <copyright file="PrintStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// PRINT statement.
/// </summary>
public class PrintStatement : IStatement
{
    public List<IExpression> Expressions { get; } = new();

    public List<PrintSeparator> Separators { get; } = new();

    public bool EndsWithSeparator { get; set; }

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitPrintStatement(this);
}