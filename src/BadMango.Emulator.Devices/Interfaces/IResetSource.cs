// <copyright file="IResetSource.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Interfaces;

/// <summary>
/// Source capable of driving the machine RESET line.
/// </summary>
public interface IResetSource
{
    /// <summary>
    /// Gets or sets a value indicating whether Control must be held to assert RESET.
    /// </summary>
    bool RequireControlForReset { get; set; }

    /// <summary>
    /// Presses the reset key/source.
    /// </summary>
    void PressResetKey();

    /// <summary>
    /// Releases the reset key/source.
    /// </summary>
    void ReleaseResetKey();
}