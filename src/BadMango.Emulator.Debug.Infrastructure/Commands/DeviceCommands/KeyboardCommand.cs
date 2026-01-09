// <copyright file="KeyboardCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands.DeviceCommands;

using System.Globalization;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Devices;
using BadMango.Emulator.Devices.Interfaces;

/// <summary>
/// Debug command for keyboard device interaction.
/// </summary>
/// <remarks>
/// <para>
/// Provides three subcommands for interacting with the keyboard device:
/// </para>
/// <list type="bullet">
/// <item><description><c>press</c> - Send a single key press (ASCII code)</description></item>
/// <item><description><c>type</c> - Queue a string of characters to type</description></item>
/// <item><description><c>read</c> - Read current keyboard state, optionally store at RAM address</description></item>
/// </list>
/// <para>
/// This command is useful for testing keyboard handling in emulated software
/// and for automating input sequences during debugging sessions.
/// </para>
/// </remarks>
[DeviceDebugCommand]
public sealed class KeyboardCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyboardCommand"/> class.
    /// </summary>
    public KeyboardCommand()
        : base("keyboard", "Keyboard device controls")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["kbd"];

    /// <inheritdoc/>
    public override string Usage => "keyboard <press|type|read> <args>";

    /// <inheritdoc/>
    public string Synopsis => "keyboard <press|type|read> <args>";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Interacts with the keyboard device for testing and debugging.\n\n" +
        "Subcommands:\n" +
        "  press <ascii>       - Send a single key press (hex $XX or decimal)\n" +
        "  type <string>       - Queue a string of characters to type\n" +
        "  read [address]      - Read keyboard buffer, optionally store at RAM address";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "keyboard press $8D       Send Return (carriage return)",
        "keyboard press 65        Send 'A' (decimal ASCII)",
        "keyboard type HELLO      Queue HELLO to be typed",
        "keyboard type \"10 PRINT CHR$(7)\\r\"  Queue with escaped return",
        "keyboard read            Display current key register state",
        "keyboard read $0300      Read keystroke buffer into $0300",
    ];

    /// <inheritdoc/>
    public string? SideEffects =>
        "The 'press' and 'type' subcommands inject input into the keyboard device.";

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

        var machine = debugContext.Machine;
        if (machine is null)
        {
            return CommandResult.Error("No machine attached.");
        }

        var keyboard = machine.GetComponent<IKeyboardDevice>();
        if (keyboard is null)
        {
            return CommandResult.Error("No keyboard device found in this machine.");
        }

        if (args.Length == 0)
        {
            return CommandResult.Error(
                "Usage: keyboard <press|type|read> <args>\n" +
                "  press <ascii>   - Send single key\n" +
                "  type <string>   - Queue string to type\n" +
                "  read [address]  - Read keyboard state");
        }

        return args[0].ToLowerInvariant() switch
        {
            "press" => ExecutePress(keyboard, debugContext, args),
            "type" => ExecuteType(keyboard, debugContext, args),
            "read" => ExecuteRead(keyboard, debugContext, args),
            _ => CommandResult.Error($"Unknown subcommand: '{args[0]}'"),
        };
    }

    private static CommandResult ExecutePress(IKeyboardDevice keyboard, IDebugContext ctx, string[] args)
    {
        if (args.Length < 2)
        {
            return CommandResult.Error("Usage: keyboard press <ascii>");
        }

        if (!TryParseAscii(args[1], out byte ascii))
        {
            return CommandResult.Error($"Invalid ASCII value: '{args[1]}'");
        }

        keyboard.KeyDown(ascii);
        keyboard.KeyUp();

        char displayChar = ascii >= 0x20 && ascii < 0x7F ? (char)ascii : '.';
        ctx.Output.WriteLine($"Pressed key: ${ascii:X2} ('{displayChar}')");
        return CommandResult.Ok();
    }

    private static CommandResult ExecuteType(IKeyboardDevice keyboard, IDebugContext ctx, string[] args)
    {
        if (args.Length < 2)
        {
            return CommandResult.Error("Usage: keyboard type <string>");
        }

        // Join remaining args and process escape sequences
        string text = string.Join(" ", args.Skip(1));
        text = ProcessEscapes(text);

        keyboard.TypeString(text);
        ctx.Output.WriteLine($"Queued {text.Length} character(s) to type.");
        return CommandResult.Ok();
    }

    private static CommandResult ExecuteRead(IKeyboardDevice keyboard, IDebugContext ctx, string[] args)
    {
        byte keyData = keyboard.KeyData;
        bool strobeSet = (keyData & 0x80) != 0;
        byte asciiValue = (byte)(keyData & 0x7F);

        ctx.Output.WriteLine("Keyboard State:");
        ctx.Output.WriteLine($"  Key Data: ${keyData:X2}");
        ctx.Output.WriteLine($"  Strobe:   {(strobeSet ? "SET" : "clear")}");
        ctx.Output.WriteLine($"  ASCII:    ${asciiValue:X2} ('{(asciiValue >= 0x20 && asciiValue < 0x7F ? (char)asciiValue : '.')}')");
        ctx.Output.WriteLine($"  Key Down: {keyboard.HasKeyDown}");

        // Optional: write to address if provided
        if (args.Length >= 2 && TryParseAddress(args[1], out uint address) && ctx.Bus is not null)
        {
            // Write key data to specified address
            var access = CreateDebugAccess(address, keyData);
            ctx.Bus.TryWrite8(access, keyData);
            ctx.Output.WriteLine($"  Written to: ${address:X4}");
        }

        return CommandResult.Ok();
    }

    private static bool TryParseAscii(string s, out byte value)
    {
        value = 0;
        if (string.IsNullOrEmpty(s))
        {
            return false;
        }

        // Hex with $ prefix
        if (s.StartsWith('$'))
        {
            return byte.TryParse(s[1..], NumberStyles.HexNumber, null, out value);
        }

        // Hex with 0x prefix
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return byte.TryParse(s[2..], NumberStyles.HexNumber, null, out value);
        }

        // Single character literal
        if (s.Length == 1)
        {
            value = (byte)s[0];
            return true;
        }

        // Decimal
        return byte.TryParse(s, out value);
    }

    private static bool TryParseAddress(string s, out uint address)
    {
        address = 0;

        if (string.IsNullOrWhiteSpace(s))
        {
            return false;
        }

        // Handle hex prefix ($, 0x, or just hex digits)
        if (s.StartsWith('$'))
        {
            return uint.TryParse(s[1..], NumberStyles.HexNumber, null, out address);
        }

        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return uint.TryParse(s[2..], NumberStyles.HexNumber, null, out address);
        }

        // Try decimal
        return uint.TryParse(s, out address);
    }

    private static BusAccess CreateDebugAccess(uint address, byte value)
    {
        return new BusAccess(
            Address: address,
            Value: value,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: AccessIntent.DebugWrite,
            SourceId: 0,
            Cycle: 0,
            Flags: AccessFlags.None);
    }

    private static string ProcessEscapes(string s)
    {
        return s
            .Replace("\\r", "\r")
            .Replace("\\n", "\n")
            .Replace("\\t", "\t")
            .Replace("\\e", "\x1B");
    }
}