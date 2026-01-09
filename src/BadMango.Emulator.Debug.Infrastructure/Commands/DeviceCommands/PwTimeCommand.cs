// <copyright file="PwTimeCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands.DeviceCommands;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Devices;
using BadMango.Emulator.Devices.Interfaces;

/// <summary>
/// Displays the current time from a PocketWatch card or interprets/copies timestamp data.
/// </summary>
/// <remarks>
/// <para>
/// Provides three modes of operation:
/// </para>
/// <list type="bullet">
/// <item><description><c>read</c> - Interpret 8 bytes from memory as a Thunderclock timestamp</description></item>
/// <item><description><c>slot</c> - Read time directly from a PocketWatch card in the specified slot</description></item>
/// <item><description><c>copy</c> - Read time from a slot and write the 8-byte timestamp to memory</description></item>
/// </list>
/// <para>
/// The Thunderclock timestamp format is:
/// </para>
/// <list type="bullet">
/// <item><description>Byte 0: Month (1-12)</description></item>
/// <item><description>Byte 1: Day of week (0=Sunday)</description></item>
/// <item><description>Byte 2: Day of month (1-31)</description></item>
/// <item><description>Byte 3: Hour (0-23)</description></item>
/// <item><description>Byte 4: Minute (0-59)</description></item>
/// <item><description>Byte 5: Second (0-59)</description></item>
/// <item><description>Byte 6: Year low byte (since 1900)</description></item>
/// <item><description>Byte 7: Year high byte</description></item>
/// </list>
/// </remarks>
[DeviceDebugCommand]
public sealed class PwTimeCommand : CommandHandlerBase, ICommandHelp
{
    private static readonly string[] DayNames = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

    /// <summary>
    /// Initializes a new instance of the <see cref="PwTimeCommand"/> class.
    /// </summary>
    public PwTimeCommand()
        : base("pwtime", "Display or copy time from PocketWatch card")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["pocketwatch-time", "thundertime"];

    /// <inheritdoc/>
    public override string Usage => "pwtime <read|slot|copy> <args>";

    /// <inheritdoc/>
    public string Synopsis => "pwtime <read|slot|copy> <args>";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Displays or copies time from a PocketWatch RTC card.\n\n" +
        "Subcommands:\n" +
        "  read <address>        - Interpret 8 bytes at address as Thunderclock timestamp\n" +
        "  slot <slot>           - Read time directly from PocketWatch card in slot (1-7)\n" +
        "  copy <slot> <address> - Read time from slot and write 8 bytes to memory address\n\n" +
        "The Thunderclock timestamp format is:\n" +
        "  Byte 0: Month (1-12)\n" +
        "  Byte 1: Day of week (0=Sunday, 6=Saturday)\n" +
        "  Byte 2: Day of month (1-31)\n" +
        "  Byte 3: Hour (0-23)\n" +
        "  Byte 4: Minute (0-59)\n" +
        "  Byte 5: Second (0-59)\n" +
        "  Byte 6-7: Year (16-bit, offset from 1900)";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "pwtime read $0300         Interpret timestamp from $0300-$0307",
        "pwtime slot 4             Read time from PocketWatch card in slot 4",
        "pwtime copy 4 $0300       Read time from slot 4 and write to $0300-$0307",
    ];

    /// <inheritdoc/>
    public string? SideEffects => "The 'copy' subcommand writes 8 bytes to the specified memory address.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["mem", "peek", "poke", "devicemap"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context is not IDebugContext debugContext)
        {
            return CommandResult.Error("Debug context required for this command.");
        }

        if (!debugContext.IsBusAttached || debugContext.Bus is null)
        {
            return CommandResult.Error("No bus attached. This command requires a bus-based system.");
        }

        if (args.Length < 1)
        {
            return CommandResult.Error(
                "Usage: pwtime <read|slot|copy> <args>\n" +
                "  read <address>        - Interpret timestamp from memory\n" +
                "  slot <slot>           - Read time from PocketWatch card\n" +
                "  copy <slot> <address> - Copy time from card to memory");
        }

        string subcommand = args[0].ToLowerInvariant();

        return subcommand switch
        {
            "read" => ExecuteRead(debugContext, args),
            "slot" => ExecuteSlot(debugContext, args),
            "copy" => ExecuteCopy(debugContext, args),
            _ => CommandResult.Error($"Unknown subcommand: '{args[0]}'. Expected 'read', 'slot', or 'copy'."),
        };
    }

    private static void DisplayRawTimestamp(
        IDebugContext debugContext,
        int month,
        int dayOfWeek,
        int day,
        int hour,
        int minute,
        int second,
        int fullYear,
        int year)
    {
        debugContext.Output.WriteLine($"  Month:       {month} (expected 1-12)");
        debugContext.Output.WriteLine($"  Day of Week: {dayOfWeek} (expected 0-6)");
        debugContext.Output.WriteLine($"  Day:         {day} (expected 1-31)");
        debugContext.Output.WriteLine($"  Hour:        {hour} (expected 0-23)");
        debugContext.Output.WriteLine($"  Minute:      {minute} (expected 0-59)");
        debugContext.Output.WriteLine($"  Second:      {second} (expected 0-59)");
        debugContext.Output.WriteLine($"  Year:        {fullYear} (1900 + {year})");
    }

    private static BusResult<byte> ReadByte(IMemoryBus bus, uint address)
    {
        var access = new BusAccess(
            Address: address,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: AccessIntent.DebugRead,
            SourceId: 0,
            Cycle: 0,
            Flags: AccessFlags.NoSideEffects);

        return bus.TryRead8(access);
    }

    private static BusResult WriteByte(IMemoryBus bus, uint address, byte value)
    {
        var access = new BusAccess(
            Address: address,
            Value: value,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: AccessIntent.DebugWrite,
            SourceId: 0,
            Cycle: 0,
            Flags: AccessFlags.None);

        return bus.TryWrite8(access, value);
    }

    private static bool TryParseAddress(string input, out uint address)
    {
        address = 0;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        // Handle hex prefix ($, 0x, or just hex digits)
        if (input.StartsWith('$'))
        {
            return uint.TryParse(input[1..], System.Globalization.NumberStyles.HexNumber, null, out address);
        }

        if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return uint.TryParse(input[2..], System.Globalization.NumberStyles.HexNumber, null, out address);
        }

        // Try decimal
        return uint.TryParse(input, out address);
    }

    private CommandResult ExecuteRead(IDebugContext debugContext, string[] args)
    {
        if (args.Length < 2)
        {
            return CommandResult.Error("Usage: pwtime read <address>");
        }

        if (!TryParseAddress(args[1], out uint address))
        {
            return CommandResult.Error($"Invalid address: '{args[1]}'");
        }

        return ReadFromMemory(debugContext, address);
    }

    private CommandResult ExecuteSlot(IDebugContext debugContext, string[] args)
    {
        if (args.Length < 2)
        {
            return CommandResult.Error("Usage: pwtime slot <slot-number>");
        }

        if (!int.TryParse(args[1], out int slot) || slot < 1 || slot > 7)
        {
            return CommandResult.Error($"Invalid slot number: '{args[1]}'. Expected 1-7.");
        }

        return ReadFromPocketWatchCard(debugContext, slot);
    }

    private CommandResult ExecuteCopy(IDebugContext debugContext, string[] args)
    {
        if (args.Length < 3)
        {
            return CommandResult.Error("Usage: pwtime copy <slot-number> <address>");
        }

        if (!int.TryParse(args[1], out int slot) || slot < 1 || slot > 7)
        {
            return CommandResult.Error($"Invalid slot number: '{args[1]}'. Expected 1-7.");
        }

        if (!TryParseAddress(args[2], out uint address))
        {
            return CommandResult.Error($"Invalid address: '{args[2]}'");
        }

        return CopyFromSlotToMemory(debugContext, slot, address);
    }

    private CommandResult ReadFromPocketWatchCard(IDebugContext debugContext, int slot)
    {
        var machine = debugContext.Machine;
        if (machine is null)
        {
            return CommandResult.Error("No machine attached.");
        }

        var slotManager = machine.GetComponent<ISlotManager>();
        if (slotManager is null)
        {
            return CommandResult.Error("No slot manager available.");
        }

        var card = slotManager.GetCard(slot);
        if (card is null)
        {
            return CommandResult.Error($"No card installed in slot {slot}.");
        }

        if (card is not IClockDevice clockDevice)
        {
            return CommandResult.Error($"Card in slot {slot} is not a clock device (found: {card.DeviceType}).");
        }

        // Read the time directly from the device
        var time = clockDevice.CurrentTime;

        // Format and display
        debugContext.Output.WriteLine($"PocketWatch Time (Slot {slot}):");
        debugContext.Output.WriteLine();
        DisplayTime(debugContext, time);

        return CommandResult.Ok();
    }

    private CommandResult CopyFromSlotToMemory(IDebugContext debugContext, int slot, uint address)
    {
        var machine = debugContext.Machine;
        if (machine is null)
        {
            return CommandResult.Error("No machine attached.");
        }

        var slotManager = machine.GetComponent<ISlotManager>();
        if (slotManager is null)
        {
            return CommandResult.Error("No slot manager available.");
        }

        var card = slotManager.GetCard(slot);
        if (card is null)
        {
            return CommandResult.Error($"No card installed in slot {slot}.");
        }

        if (card is not IClockDevice clockDevice)
        {
            return CommandResult.Error($"Card in slot {slot} is not a clock device (found: {card.DeviceType}).");
        }

        // Read the time directly from the device
        var time = clockDevice.CurrentTime;

        // Convert to Thunderclock format
        int year = time.Year - 1900;
        byte[] timeData =
        [
            (byte)time.Month,
            (byte)time.DayOfWeek,
            (byte)time.Day,
            (byte)time.Hour,
            (byte)time.Minute,
            (byte)time.Second,
            (byte)(year & 0xFF),
            (byte)((year >> 8) & 0xFF),
        ];

        // Write to memory
        bool hadFault = false;
        for (int i = 0; i < 8; i++)
        {
            var result = WriteByte(debugContext.Bus!, address + (uint)i, timeData[i]);
            if (result.Fault.IsFault)
            {
                hadFault = true;
            }
        }

        if (hadFault)
        {
            debugContext.Output.WriteLine($"Warning: Some bytes could not be written to ${address:X4}-${address + 7:X4}.");
            debugContext.Output.WriteLine();
        }

        // Display results
        debugContext.Output.WriteLine($"Copied PocketWatch time from slot {slot} to ${address:X4}:");
        debugContext.Output.WriteLine();
        debugContext.Output.WriteLine($"  Raw bytes: {string.Join(" ", timeData.Select(b => $"${b:X2}"))}");
        debugContext.Output.WriteLine();
        DisplayTime(debugContext, time);

        return CommandResult.Ok();
    }

    private CommandResult ReadFromMemory(IDebugContext debugContext, uint address)
    {
        // Read 8 bytes for the timestamp
        var data = new byte[8];
        bool hadFault = false;

        for (int i = 0; i < 8; i++)
        {
            var result = ReadByte(debugContext.Bus!, address + (uint)i);
            if (result.Fault.IsFault)
            {
                hadFault = true;
                data[i] = 0;
            }
            else
            {
                data[i] = result.Value;
            }
        }

        if (hadFault)
        {
            debugContext.Output.WriteLine($"Warning: Some bytes at ${address:X4}-${address + 7:X4} could not be read.");
            debugContext.Output.WriteLine();
        }

        // Parse the timestamp
        int month = data[0];
        int dayOfWeek = data[1];
        int day = data[2];
        int hour = data[3];
        int minute = data[4];
        int second = data[5];
        int year = data[6] | (data[7] << 8);
        int fullYear = year + 1900;

        // Validate ranges
        bool isValid = month >= 1 && month <= 12 &&
                       dayOfWeek >= 0 && dayOfWeek <= 6 &&
                       day >= 1 && day <= 31 &&
                       hour >= 0 && hour <= 23 &&
                       minute >= 0 && minute <= 59 &&
                       second >= 0 && second <= 59;

        // Display results
        debugContext.Output.WriteLine($"Thunderclock Timestamp at ${address:X4}:");
        debugContext.Output.WriteLine();
        debugContext.Output.WriteLine($"  Raw bytes: {string.Join(" ", data.Select(b => $"${b:X2}"))}");
        debugContext.Output.WriteLine();

        if (isValid)
        {
            try
            {
                var dateTime = new DateTime(fullYear, month, day, hour, minute, second);
                DisplayTime(debugContext, dateTime);
            }
            catch (ArgumentOutOfRangeException)
            {
                debugContext.Output.WriteLine("  (Cannot convert to DateTime - values out of range)");
                DisplayRawTimestamp(debugContext, month, dayOfWeek, day, hour, minute, second, fullYear, year);
            }
        }
        else
        {
            debugContext.Output.WriteLine("  WARNING: Invalid timestamp values detected!");
            debugContext.Output.WriteLine();
            DisplayRawTimestamp(debugContext, month, dayOfWeek, day, hour, minute, second, fullYear, year);
        }

        return CommandResult.Ok();
    }

    private void DisplayTime(IDebugContext debugContext, DateTime time)
    {
        string dayName = DayNames[(int)time.DayOfWeek];
        debugContext.Output.WriteLine($"  Date: {dayName}, {time:yyyy-MM-dd}");
        debugContext.Output.WriteLine($"  Time: {time:HH:mm:ss}");
        debugContext.Output.WriteLine();
        debugContext.Output.WriteLine($"  ISO 8601: {time:O}");
    }
}