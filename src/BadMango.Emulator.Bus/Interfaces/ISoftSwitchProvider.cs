// <copyright file="ISoftSwitchProvider.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Interface for devices that expose soft switch state for debugging.
/// </summary>
/// <remarks>
/// <para>
/// Motherboard devices and peripherals that manage soft switches can implement
/// this interface to expose their current state to the debugger's <c>switches</c>
/// command.
/// </para>
/// <para>
/// The soft switch state is read-only and intended for display purposes only.
/// To change soft switch state, use the appropriate memory write operations.
/// </para>
/// </remarks>
public interface ISoftSwitchProvider
{
    /// <summary>
    /// Gets the name of this soft switch provider (e.g., "Language Card", "Auxiliary Memory").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Gets the current state of all soft switches managed by this provider.
    /// </summary>
    /// <returns>A collection of soft switch states.</returns>
    IReadOnlyList<SoftSwitchState> GetSoftSwitchStates();
}
