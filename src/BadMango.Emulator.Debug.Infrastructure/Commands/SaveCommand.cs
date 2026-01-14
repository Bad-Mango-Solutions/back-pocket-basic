// <copyright file="SaveCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using System.Globalization;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Saves memory contents to a binary file.
/// </summary>
/// <remarks>
/// <para>
/// Reads memory from the specified address range and writes it to a file.
/// Both the start address and length must be specified.
/// </para>
/// <para>
/// If the file already exists, it will be overwritten unless the --no-overwrite
/// flag is specified.
/// </para>
/// </remarks>
public sealed class SaveCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SaveCommand"/> class.
    /// </summary>
    public SaveCommand()
        : base("save", "Save memory contents to binary file")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = [];

    /// <inheritdoc/>
    public override string Usage => "save <filename> <address> <length> [--no-overwrite]";

    /// <inheritdoc/>
    public string Synopsis => "save <filename> <address> <length> [--no-overwrite]";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Saves memory contents to a binary file. Reads memory from the specified " +
        "address for the specified length and writes raw bytes to the file. Uses " +
        "debug read (no side effects). Overwrites existing files unless --no-overwrite " +
        "is specified.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } =
    [
        new("--no-overwrite", "-n", "flag", "Fail if file already exists", "off"),
    ];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "save rom.bin $F000 $1000               Save 4KB from $F000 to rom.bin",
        "save program.bin $800 $100             Save 256 bytes from $0800",
        "save data.bin $300 $20 -n              Save with overwrite protection",
        "save library://dumps/mem.bin $0 $100   Save to library directory",
    ];

    /// <inheritdoc/>
    public string? SideEffects =>
        "Creates or overwrites the specified file on disk.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["load", "mem", "peek"];

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

        if (args.Length < 3)
        {
            return CommandResult.Error("Filename, address, and length required. Usage: save <filename> <address> <length>");
        }

        string filename = args[0];

        if (!TryParseAddress(args[1], out uint startAddress))
        {
            return CommandResult.Error($"Invalid address: '{args[1]}'. Use hex format ($1234 or 0x1234) or decimal.");
        }

        if (!TryParseLength(args[2], out int length) || length < 1)
        {
            return CommandResult.Error($"Invalid length: '{args[2]}'. Expected a positive integer.");
        }

        bool noOverwrite = args.Any(arg =>
            arg.Equals("--no-overwrite", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("-n", StringComparison.OrdinalIgnoreCase));

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

        // Calculate memory size from bus page count
        uint memorySize = (uint)debugContext.Bus.PageCount << debugContext.Bus.PageShift;

        // Validate address range
        if (startAddress >= memorySize)
        {
            return CommandResult.Error($"Address ${startAddress:X4} is out of range (memory size: ${memorySize:X4}).");
        }

        if (startAddress + (uint)length > memorySize)
        {
            length = (int)(memorySize - startAddress);
            debugContext.Output.WriteLine($"Warning: Length adjusted to {length} bytes to stay within memory bounds.");
        }

        // Check if file exists
        if (File.Exists(resolvedPath) && noOverwrite)
        {
            string errorMessage = resolvedPath != filename
                ? $"File already exists: '{filename}' (resolved to '{resolvedPath}'). Remove --no-overwrite to overwrite."
                : $"File already exists: '{filename}'. Remove --no-overwrite to overwrite.";
            return CommandResult.Error(errorMessage);
        }

        try
        {
            // Ensure the directory exists for library:// paths
            string? directory = Path.GetDirectoryName(resolvedPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Read data from memory
            byte[] data = new byte[length];
            for (int i = 0; i < length; i++)
            {
                data[i] = ReadByte(debugContext.Bus, startAddress + (uint)i);
            }

            // Write to file
            File.WriteAllBytes(resolvedPath, data);

            debugContext.Output.WriteLine($"Saved {length} bytes from ${startAddress:X4}-${startAddress + (uint)length - 1:X4} to '{filename}'");

            return CommandResult.Ok();
        }
        catch (IOException ex)
        {
            return CommandResult.Error($"Error writing file: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return CommandResult.Error($"Access denied: {ex.Message}");
        }
    }

    private static byte ReadByte(IMemoryBus bus, uint address)
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

        var result = bus.TryRead8(access);
        return result.Ok ? result.Value : (byte)0xFF;
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

    private static bool TryParseLength(string value, out int result)
    {
        result = 0;

        if (value.StartsWith("$", StringComparison.Ordinal))
        {
            return int.TryParse(value[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
        }

        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return int.TryParse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
        }

        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }
}