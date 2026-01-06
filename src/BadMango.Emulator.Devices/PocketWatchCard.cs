// <copyright file="PocketWatchCard.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core.Configuration;

using Interfaces;

/// <summary>
/// PocketWatch slot card - a Thunderclock-compatible real-time clock.
/// </summary>
/// <remarks>
/// <para>
/// The PocketWatch card provides ProDOS-compatible time services to the emulated system.
/// It is compatible with the Thunderclock Plus protocol, allowing existing software
/// to access the current date and time.
/// </para>
/// <para>
/// The card supports multiple time sources:
/// <list type="bullet">
/// <item><description>"host" - Uses the host system's clock with timezone offset</description></item>
/// <item><description>"frozen" - Uses a static datetime from configuration</description></item>
/// <item><description>"ntp" - Uses NTP server polling (not yet implemented)</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class PocketWatchCard : IClockDevice
{
    private readonly SlotIOHandlers handlers = new();
    private readonly IBusTarget romRegion;
    private readonly PocketWatchConfig config;
    private DateTime fixedTime;
    private bool useHostTime = true;
    private int timezoneOffsetMinutes;

    // Thunderclock state
    private int readIndex;
    private readonly byte[] timeData = new byte[8];

    /// <summary>
    /// Initializes a new instance of the <see cref="PocketWatchCard"/> class with default configuration.
    /// </summary>
    public PocketWatchCard()
        : this(new PocketWatchConfig())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PocketWatchCard"/> class with specified configuration.
    /// </summary>
    /// <param name="config">The PocketWatch configuration.</param>
    public PocketWatchCard(PocketWatchConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        this.config = config;

        // Configure time source
        ConfigureTimeSource();

        // Set up I/O handlers (Thunderclock protocol)
        handlers.Set(0x00, ReadClockData, WriteClockControl);
        handlers.Set(0x01, ReadClockStatus, null);

        // Create ROM with identification bytes
        romRegion = new PocketWatchRom();
    }

    // ─── IPeripheral ────────────────────────────────────────────────────

    /// <inheritdoc />
    public string Name => "PocketWatch";

    /// <inheritdoc />
    public string DeviceType => "PocketWatch";

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
    public DateTime CurrentTime
    {
        get
        {
            if (useHostTime)
            {
                return DateTimeOffset.UtcNow.AddMinutes(timezoneOffsetMinutes).DateTime;
            }

            return fixedTime;
        }
    }

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
        // PocketWatch doesn't need scheduler access
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

    private void ConfigureTimeSource()
    {
        timezoneOffsetMinutes = config.TimezoneOffset;

        switch (config.TimeSource.ToLowerInvariant())
        {
            case "host":
                useHostTime = true;
                break;

            case "frozen":
                useHostTime = false;
                if (!string.IsNullOrEmpty(config.FrozenTime) &&
                    DateTime.TryParse(config.FrozenTime, out var parsedTime))
                {
                    fixedTime = parsedTime;
                }
                else
                {
                    // Default frozen time if parsing fails
                    fixedTime = new DateTime(1986, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                }

                break;

            case "ntp":
                // NTP support is a future enhancement
                // For now, fall back to host time
                useHostTime = true;
                break;

            default:
                useHostTime = true;
                break;
        }
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