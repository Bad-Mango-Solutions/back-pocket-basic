// <copyright file="ISpeakerDevice.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Interfaces;

using Bus.Interfaces;

/// <summary>
/// Speaker device interface for audio output.
/// </summary>
/// <remarks>
/// <para>
/// This interface defines the host-facing API for the speaker device,
/// allowing the emulator frontend to synthesize audio from speaker toggles.
/// </para>
/// <para>
/// The Apple II speaker is a simple 1-bit output toggled by accessing $C030.
/// Each access toggles the speaker between high and low states. Sound is
/// produced by toggling the speaker at audio frequencies.
/// </para>
/// </remarks>
public interface ISpeakerDevice : IMotherboardDevice
{
    /// <summary>
    /// Event raised when the speaker is toggled.
    /// </summary>
    /// <remarks>
    /// The event parameters are the cycle at which the toggle occurred and the
    /// resulting speaker state (high or low).
    /// </remarks>
    event Action<ulong, bool>? Toggled;

    /// <summary>
    /// Gets a value indicating whether the speaker is currently toggled high.
    /// </summary>
    /// <value><see langword="true"/> if the speaker is high; otherwise, <see langword="false"/>.</value>
    bool State { get; }

    /// <summary>
    /// Gets the toggle events since last drain (cycle, state pairs).
    /// </summary>
    /// <value>A read-only list of pending toggle events, each containing the cycle and resulting state.</value>
    IReadOnlyList<(ulong Cycle, bool State)> PendingToggles { get; }

    /// <summary>
    /// Drains pending toggle events for audio synthesis.
    /// </summary>
    /// <returns>The toggle events that were pending.</returns>
    /// <remarks>
    /// <para>
    /// The audio synthesis system should call this periodically (e.g., every audio buffer)
    /// to retrieve the toggle events that occurred since the last call. The returned
    /// events are removed from the pending queue.
    /// </para>
    /// <para>
    /// Each event contains the cycle at which the toggle occurred and the resulting
    /// speaker state (high or low). These can be used to generate a square wave
    /// for audio output.
    /// </para>
    /// </remarks>
    IList<(ulong Cycle, bool State)> DrainToggles();
}