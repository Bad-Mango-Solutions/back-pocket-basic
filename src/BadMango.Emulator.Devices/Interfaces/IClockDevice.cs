// <copyright file="IClockDevice.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Interfaces;

using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Clock device interface for host time access.
/// </summary>
/// <remarks>
/// <para>
/// This interface is implemented by slot cards like the Thunderclock Plus that
/// surface the host's date/time to the emulated system.
/// </para>
/// <para>
/// Clock cards allow emulated software (especially ProDOS) to maintain accurate
/// timestamps for file operations. The interface supports both real-time mode
/// (using host system time) and fixed-time mode (for reproducible testing).
/// </para>
/// </remarks>
public interface IClockDevice : ISlotCard
{
    /// <summary>
    /// Gets the current time as seen by the emulated system.
    /// </summary>
    /// <value>
    /// The current <see cref="DateTime"/>, either from the host system or
    /// the fixed time if <see cref="UseHostTime"/> is <see langword="false"/>.
    /// </value>
    DateTime CurrentTime { get; }

    /// <summary>
    /// Gets or sets a value indicating whether to use host time or a fixed time.
    /// </summary>
    /// <value>
    /// <see langword="true"/> to use the host system's current time;
    /// <see langword="false"/> to use a fixed time set via <see cref="SetFixedTime"/>.
    /// </value>
    bool UseHostTime { get; set; }

    /// <summary>
    /// Sets a fixed time (when UseHostTime is false).
    /// </summary>
    /// <param name="time">The fixed time to return from <see cref="CurrentTime"/>.</param>
    /// <remarks>
    /// Setting a fixed time automatically sets <see cref="UseHostTime"/> to <see langword="false"/>.
    /// This is useful for reproducible testing scenarios.
    /// </remarks>
    void SetFixedTime(DateTime time);
}