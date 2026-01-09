// <copyright file="CpuStepResult.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Cpu;

/// <summary>
/// Represents the result of a single CPU step, including its state and the number of cycles consumed.
/// </summary>
/// <param name="State">The state of the CPU after the step.</param>
/// <param name="CyclesConsumed">The number of cycles consumed during the step.</param>
public readonly record struct CpuStepResult(
    CpuRunState State,
    Cycle CyclesConsumed);