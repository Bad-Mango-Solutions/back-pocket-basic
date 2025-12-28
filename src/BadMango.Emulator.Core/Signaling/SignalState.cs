// <copyright file="SignalState.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Represents the state of a signal line.
/// </summary>
public enum SignalState : byte
{
    /// <summary>
    /// Signal is not asserted (inactive/high).
    /// </summary>
    Clear,

    /// <summary>
    /// Signal is asserted (active/low).
    /// </summary>
    Asserted,
}