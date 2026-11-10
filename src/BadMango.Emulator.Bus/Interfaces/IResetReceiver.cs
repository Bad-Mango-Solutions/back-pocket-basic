// <copyright file="IResetReceiver.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Interfaces;

using BadMango.Emulator.Core;

/// <summary>
/// Receives RESET signal lifecycle notifications.
/// </summary>
public interface IResetReceiver
{
    /// <summary>
    /// Handles the RESET line being asserted (driven low).
    /// </summary>
    /// <param name="cycle">The cycle at which the line transitioned.</param>
    void OnResetAsserted(Cycle cycle);

    /// <summary>
    /// Handles the RESET line being released (rising edge).
    /// </summary>
    /// <param name="cycle">The cycle at which the line transitioned.</param>
    void OnResetReleased(Cycle cycle);
}