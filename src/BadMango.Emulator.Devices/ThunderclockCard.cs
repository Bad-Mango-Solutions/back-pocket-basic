// <copyright file="ThunderclockCard.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

using Interfaces;

/// <summary>
/// Thunderclock Plus slot card - provides real-time clock to Apple II.
/// </summary>
/// <remarks>
/// <para>
/// The Thunderclock Plus is a real-time clock card that provides ProDOS-compatible
/// time services. It uses a simple protocol through slot I/O space.
/// </para>
/// <para>
/// This stub implementation surfaces DateTime.Now (or a fixed time) to the
/// emulated system, providing basic clock functionality without full
/// Thunderclock hardware emulation.
/// </para>
/// </remarks>
public sealed class ThunderclockCard : IClockDevice
{
    private readonly SlotIOHandlers handlers = new();
    private readonly IBusTarget romRegion;
    private readonly byte[] timeData = new byte[8];
    private DateTime fixedTime;
    private bool useHostTime = true;

    // Thunderclock state
    private int readIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThunderclockCard"/> class.
    /// </summary>
    public ThunderclockCard()
    {
        // Set up I/O handlers
        // Thunderclock uses a few specific offsets for clock access
        handlers.Set(0x00, ReadClockData, WriteClockControl);
        handlers.Set(0x01, ReadClockStatus, null);

        // Create ROM with identification bytes
        romRegion = new ThunderclockRom();
    }

    // ─── IPeripheral ────────────────────────────────────────────────────

    /// <inheritdoc />
    public string Name => "Thunderclock Plus";

    /// <inheritdoc />
    public string DeviceType => "Thunderclock";

    /// <inheritdoc />
    public PeripheralKind Kind => PeripheralKind.SlotCard;

    // ─── ISlotCard ──────────────────────────────────────────────────────

    /// <inheritdoc />
    public int SlotNumber { get; set; }

    /// <inheritdoc />
    public SlotIOHandlers? IOHandlers => handlers;

    /// <inheritdoc />
    public IBusTarget? ROMRegion => romRegion;

    /// <inheritdoc />
    public IBusTarget? ExpansionROMRegion => null;

    // ─── IClockDevice ───────────────────────────────────────────────────

    /// <inheritdoc />
    public DateTime CurrentTime => useHostTime ? DateTime.Now : fixedTime;

    /// <inheritdoc />
    public bool UseHostTime
    {
        get => useHostTime;
        set => useHostTime = value;
    }

    /// <inheritdoc />
    public void SetFixedTime(DateTime time)
    {
        fixedTime = time;
        useHostTime = false;
    }

    // ─── IScheduledDevice ───────────────────────────────────────────────

    /// <inheritdoc />
    public void Initialize(IEventContext context)
    {
        // Thunderclock doesn't need scheduler access
    }

    /// <inheritdoc />
    public void Reset()
    {
        readIndex = 0;
    }

    /// <inheritdoc />
    public void OnExpansionROMSelected()
    {
        // No expansion ROM
    }

    /// <inheritdoc />
    public void OnExpansionROMDeselected()
    {
        // No expansion ROM
    }

    private byte ReadClockData(byte offset, in BusAccess context)
    {
        // Latch current time on first read
        if (readIndex == 0)
        {
            LatchTime();
        }

        byte value = timeData[readIndex];
        readIndex = (readIndex + 1) % timeData.Length;
        return value;
    }

    private byte ReadClockStatus(byte offset, in BusAccess context)
    {
        // Status: bit 7 = data ready
        return 0x80;
    }

    private void WriteClockControl(byte offset, byte value, in BusAccess context)
    {
        // Reset read index on any write
        readIndex = 0;
    }

    private void LatchTime()
    {
        var time = CurrentTime;

        // Thunderclock format (ProDOS compatible):
        // Byte 0: Month (1-12)
        // Byte 1: Day of week (0=Sunday)
        // Byte 2: Day of month (1-31)
        // Byte 3: Hour (0-23)
        // Byte 4: Minute (0-59)
        // Byte 5: Second (0-59)
        // Byte 6: Year low byte (since 1900)
        // Byte 7: Year high byte
        int year = time.Year - 1900;
        timeData[0] = (byte)time.Month;
        timeData[1] = (byte)time.DayOfWeek;
        timeData[2] = (byte)time.Day;
        timeData[3] = (byte)time.Hour;
        timeData[4] = (byte)time.Minute;
        timeData[5] = (byte)time.Second;
        timeData[6] = (byte)(year & 0xFF);
        timeData[7] = (byte)((year >> 8) & 0xFF);
    }
}