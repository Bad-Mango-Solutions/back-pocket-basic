// <copyright file="RestoreStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// RESTORE statement.
/// </summary>
public class RestoreStatement : IStatement
{
    public int? LineNumber { get; set; }

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitRestoreStatement(this);
}