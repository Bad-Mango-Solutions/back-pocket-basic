// <copyright file="OpcodeHandler.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Cpu;

/// <summary>
/// Delegate for opcode handlers that receive machine state via state structure and memory.
/// </summary>
/// <param name="memory">The memory interface.</param>
/// <param name="state">Reference to the CPU state structure.</param>
public delegate void OpcodeHandler(IMemory memory, ref CpuState state);