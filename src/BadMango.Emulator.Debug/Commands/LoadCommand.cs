// <copyright file="LoadCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Commands;

using System.Globalization;

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
public sealed class LoadCommand : CommandHandlerBase
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
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context is not IDebugContext debugContext)
        {
            return CommandResult.Error("Debug context required for this command.");
        }

        if (debugContext.Memory is null)
        {
            return CommandResult.Error("No memory attached to debug context.");
        }

        if (args.Length == 0)
        {
            return CommandResult.Error("Filename required. Usage: load <filename> [address]");
        }

        string filename = args[0];

        uint startAddress = 0;
        if (args.Length > 1)
        {
            if (!TryParseAddress(args[1], out startAddress))
            {
                return CommandResult.Error($"Invalid address: '{args[1]}'. Use hex format ($1234 or 0x1234) or decimal.");
            }
        }

        // Check if file exists
        if (!File.Exists(filename))
        {
            return CommandResult.Error($"File not found: '{filename}'");
        }

        try
        {
            byte[] data = File.ReadAllBytes(filename);

            if (data.Length == 0)
            {
                return CommandResult.Error($"File is empty: '{filename}'");
            }

            // Validate address range
            if (startAddress >= debugContext.Memory.Size)
            {
                return CommandResult.Error($"Address ${startAddress:X4} is out of range (memory size: ${debugContext.Memory.Size:X4}).");
            }

            if (startAddress + (uint)data.Length > debugContext.Memory.Size)
            {
                return CommandResult.Error($"File would exceed memory bounds. Start: ${startAddress:X4}, Size: {data.Length}, Memory size: ${debugContext.Memory.Size:X4}");
            }

            // Write data to memory
            for (int i = 0; i < data.Length; i++)
            {
                debugContext.Memory.Write(startAddress + (uint)i, data[i]);
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