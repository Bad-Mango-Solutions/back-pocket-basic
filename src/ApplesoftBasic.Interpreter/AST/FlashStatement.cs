// <copyright file="FlashStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// FLASH statement - sets flashing text mode.
/// </summary>
public class FlashStatement : IStatement
{
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitFlashStatement(this);
}