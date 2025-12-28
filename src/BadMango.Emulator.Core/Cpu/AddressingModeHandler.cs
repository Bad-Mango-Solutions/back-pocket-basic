// <copyright file="AddressingModeHandler.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Cpu;

using Interfaces;

/// <summary>
/// Delegate representing an addressing mode that computes an effective address.
/// </summary>
/// <typeparam name="TState">The CPU state type.</typeparam>
/// <param name="memory">The memory interface.</param>
/// <param name="state">Reference to the CPU state.</param>
/// <returns>The effective address computed by the addressing mode.</returns>
public delegate Addr AddressingModeHandler<TState>(IMemory memory, ref TState state)
    where TState : struct;