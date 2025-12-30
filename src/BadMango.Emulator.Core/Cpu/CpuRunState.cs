// <copyright file="CpuRunState.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Cpu;

/// <summary>Represents the various states of the CPU during its execution lifecycle.</summary>
public enum CpuRunState
{
    /// <summary>Represents the default or uninitialized state of the CPU.</summary>
    None,

    /// <summary>Represents the running state of the CPU.</summary>
    Running,

    /// <summary>Represents the state of the CPU waiting for an interrupt.</summary>
    WaitingForInterrupt,

    /// <summary>Represents the stopped state of the CPU.</summary>
    Stopped,

    /// <summary>Represents the halted state of the CPU.</summary>
    Halted,
}