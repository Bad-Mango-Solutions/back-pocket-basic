// <copyright file="SpeakerController.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

using Interfaces;

/// <summary>
/// Speaker controller handling $C030 toggle.
/// </summary>
/// <remarks>
/// <para>
/// The Apple II speaker is a simple 1-bit output toggled by accessing $C030.
/// Each access (read or write) toggles the speaker between high and low states.
/// Sound is produced by toggling the speaker at audio frequencies.
/// </para>
/// <para>
/// This controller records toggle events with cycle timestamps, allowing
/// audio synthesis systems to generate accurate waveforms from the toggle history.
/// </para>
/// </remarks>
public sealed class SpeakerController : ISpeakerDevice
{
    private const byte SpeakerToggleOffset = 0x30;

    private readonly List<(ulong Cycle, bool State)> pendingToggles = [];
    private bool state;
    private IScheduler? scheduler;

    /// <inheritdoc />
    public event Action<ulong, bool>? Toggled;

    /// <inheritdoc />
    public string Name => "Speaker Controller";

    /// <inheritdoc />
    public string DeviceType => "Speaker";

    /// <inheritdoc />
    public PeripheralKind Kind => PeripheralKind.Motherboard;

    /// <inheritdoc />
    public bool State => state;

    /// <inheritdoc />
    public IReadOnlyList<(ulong Cycle, bool State)> PendingToggles => pendingToggles;

    /// <inheritdoc />
    public void Initialize(IEventContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        scheduler = context.Scheduler;
    }

    /// <inheritdoc />
    public void RegisterHandlers(IOPageDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        // $C030: Speaker toggle (read and write)
        dispatcher.Register(SpeakerToggleOffset, ToggleSpeakerRead, ToggleSpeakerWrite);
    }

    /// <inheritdoc />
    public void Reset()
    {
        state = false;
        pendingToggles.Clear();
    }

    /// <inheritdoc />
    public IList<(ulong Cycle, bool State)> DrainToggles()
    {
        var result = new List<(ulong, bool)>(pendingToggles);
        pendingToggles.Clear();
        return result;
    }

    private byte ToggleSpeakerRead(byte offset, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            Toggle(context.Cycle);
        }

        return 0xFF; // Floating bus
    }

    private void ToggleSpeakerWrite(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            Toggle(context.Cycle);
        }
    }

    private void Toggle(ulong cycle)
    {
        state = !state;
        pendingToggles.Add((cycle, state));
        Toggled?.Invoke(cycle, state);
    }
}