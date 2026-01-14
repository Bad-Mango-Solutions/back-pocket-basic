// <copyright file="LoadCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using System.Globalization;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Loads binary data from a file into memory.
/// </summary>
/// <remarks>
/// <para>
/// Reads a binary file and writes its contents to memory starting at
/// the specified address. The file must exist and be accessible.
/// </para>
/// <para>
/// By default, loads to address $0000. Use the address argument to
/// specify a different starting location.
/// </para>
/// </remarks>
public sealed class LoadCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoadCommand"/> class.
    /// </summary>
    public LoadCommand()
        : base("load", "Load binary file into memory")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["l"];

    /// <inheritdoc/>
    public override string Usage => "load <filename> [address]";

    /// <inheritdoc/>
    public string Synopsis => "load <filename> [address]";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Loads a binary file into memory starting at the specified address. " +
        "The file is read as raw bytes and written using debug write (no I/O side " +
        "effects). By default loads to $0000. Reports the number of bytes loaded " +
        "and the address range written.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "load program.bin $800            Load file at $0800",
        "load rom.bin $F000               Load ROM image at $F000",
        "load data.bin                    Load file at $0000 (default)",
        "load library://roms/test.bin $800  Load from library directory",
    ];

    /// <inheritdoc/>
    public string? SideEffects =>
        "Writes file contents to memory. Uses debug write which bypasses ROM " +
        "protection and does not trigger I/O side effects.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["save", "poke", "mem"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context is not IDebugContext debugContext)
        {
            return CommandResult.Error("Debug context required for this command.");
        }

        if (debugContext.Bus is null)
        {
            return CommandResult.Error("No memory bus attached to debug context.");
        }

        if (args.Length == 0)
        {
            return CommandResult.Error("Filename required. Usage: load <filename> [address]");
        }

        string filename = args[0];

        uint startAddress = 0;
        if (args.Length > 1 && !TryParseAddress(args[1], out startAddress))
        {
            return CommandResult.Error($"Invalid address: '{args[1]}'. Use hex format ($1234 or 0x1234) or decimal.");
        }

        // Resolve the path using the path resolver if available
        string resolvedPath = filename;
        if (debugContext.PathResolver is not null)
        {
            if (!debugContext.PathResolver.TryResolve(filename, out string? resolved))
            {
                return CommandResult.Error($"Cannot resolve path: '{filename}'. Library root may not be configured.");
            }

            resolvedPath = resolved!;
        }

        // Check if file exists
        if (!File.Exists(resolvedPath))
        {
            string errorMessage = resolvedPath != filename
                ? $"File not found: '{filename}' (resolved to '{resolvedPath}')"
                : $"File not found: '{filename}'";
            return CommandResult.Error(errorMessage);
        }

        try
        {
            byte[] data = File.ReadAllBytes(resolvedPath);

            if (data.Length == 0)
            {
                string errorMessage = resolvedPath != filename
                    ? $"File is empty: '{filename}' (resolved to '{resolvedPath}')"
                    : $"File is empty: '{filename}'";
                return CommandResult.Error(errorMessage);
            }

            // Calculate memory size from bus page count
            uint memorySize = (uint)debugContext.Bus.PageCount << debugContext.Bus.PageShift;

            // Validate address range
            if (startAddress >= memorySize)
            {
                return CommandResult.Error($"Address ${startAddress:X4} is out of range (memory size: ${memorySize:X4}).");
            }

            if (startAddress + (uint)data.Length > memorySize)
            {
                return CommandResult.Error($"File would exceed memory bounds. Start: ${startAddress:X4}, Size: {data.Length}, Memory size: ${memorySize:X4}");
            }

            // Write data to memory
            for (int i = 0; i < data.Length; i++)
            {
                WriteByte(debugContext.Bus, startAddress + (uint)i, data[i]);
            }

            debugContext.Output.WriteLine($"Loaded {data.Length} bytes from '{filename}' to ${startAddress:X4}-${startAddress + (uint)data.Length - 1:X4}");

            return CommandResult.Ok();
        }
        catch (IOException ex)
        {
            return CommandResult.Error($"Error reading file: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return CommandResult.Error($"Access denied: {ex.Message}");
        }
    }

    private static void WriteByte(IMemoryBus bus, uint address, byte value)
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

        bus.TryWrite8(access, value);
    }

    private static bool TryParseAddress(string value, out uint result)
    {
        result = 0;

        if (value.StartsWith("$", StringComparison.Ordinal))
        {
            return uint.TryParse(value[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
        }

        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return uint.TryParse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
        }

        return uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }
}