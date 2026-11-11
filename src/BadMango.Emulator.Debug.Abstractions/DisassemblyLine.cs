// <copyright file="DisassemblyLine.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Abstractions;

/// <summary>
/// A single disassembly line produced by the debugger.
/// </summary>
/// <param name="Address">The instruction address.</param>
/// <param name="Mnemonic">The mnemonic name.</param>
/// <param name="Operand">The operand text.</param>
/// <param name="Size">The instruction size in bytes.</param>
/// <param name="Bytes">The raw instruction bytes.</param>
/// <param name="Comment">An optional comment.</param>
public sealed record DisassemblyLine(uint Address, string Mnemonic, string Operand, int Size, string Bytes, string? Comment = null);