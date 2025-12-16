// <copyright file="MemoryAccessException.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Emulation;

/// <summary>
/// Exception thrown for invalid memory access.
/// </summary>
public class MemoryAccessException : Exception
{
    public MemoryAccessException(string message) : base(message) { }
}