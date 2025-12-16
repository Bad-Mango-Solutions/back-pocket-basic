// <copyright file="ProgramEndException.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Runtime;

/// <summary>
/// Exception thrown to signal program termination.
/// </summary>
public class ProgramEndException : Exception
{
    public ProgramEndException() : base("Program ended") { }
}