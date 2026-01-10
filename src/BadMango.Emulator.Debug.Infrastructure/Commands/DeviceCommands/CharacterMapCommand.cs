// <copyright file="CharacterMapCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands.DeviceCommands;

using Devices;
using Devices.Interfaces;

/// <summary>
/// Manages character ROM data for the video device.
/// </summary>
/// <remarks>
/// <para>
/// Provides commands to load character ROM data from files and preview
/// the active character set. The character ROM defines the bitmap glyphs
/// used for text rendering in the Apple II video modes.
/// </para>
/// <para>
/// Subcommands:
/// </para>
/// <list type="bullet">
/// <item><description><c>load</c> - Load character data from a binary file</description></item>
/// <item><description><c>preview</c> - Display a preview of the character set</description></item>
/// <item><description><c>default</c> - Load the built-in default character ROM</description></item>
/// <item><description><c>status</c> - Show current character ROM status</description></item>
/// </list>
/// </remarks>
[DeviceDebugCommand]
public sealed class CharacterMapCommand : CommandHandlerBase, ICommandHelp
{
    private readonly IDebugWindowManager? windowManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="CharacterMapCommand"/> class.
    /// </summary>
    /// <param name="windowManager">
    /// Optional debug window manager for character preview window.
    /// If null, preview command displays character map in the console.
    /// </param>
    public CharacterMapCommand(IDebugWindowManager? windowManager = null)
        : base("charactermap", "Manage character ROM data")
    {
        this.windowManager = windowManager;
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["charmap", "cmap"];

    /// <inheritdoc/>
    public override string Usage => "charactermap <subcommand> [args]";

    /// <inheritdoc/>
    public string Synopsis => "charactermap <load|preview|default|status> [args]";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Manages character ROM data for the video device. The character ROM contains " +
        "bitmap font data used for text mode rendering. Supports loading custom character " +
        "sets from 4KB binary files and previewing the active character map.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "charactermap status          Show character ROM status",
        "charactermap default         Load the built-in default character ROM",
        "charactermap load font.rom   Load character data from font.rom",
        "charactermap preview         Preview the character set in console",
        "charmap default              Short alias for loading default ROM",
    ];

    /// <inheritdoc/>
    public string? SideEffects =>
        "The 'load' and 'default' subcommands modify the video device's character ROM. " +
        "The 'preview' subcommand may open a window if Avalonia UI is available.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["load", "switches", "about"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context is not IDebugContext debugContext)
        {
            return CommandResult.Error("Debug context required for this command.");
        }

        if (args.Length == 0)
        {
            return ExecuteStatus(debugContext);
        }

        string subcommand = args[0].ToLowerInvariant();
        string[] subArgs = args.Length > 1 ? args[1..] : [];

        return subcommand switch
        {
            "status" => ExecuteStatus(debugContext),
            "default" => ExecuteLoadDefault(debugContext),
            "load" => ExecuteLoad(debugContext, subArgs),
            "preview" => ExecutePreview(debugContext),
            _ => CommandResult.Error(
                $"Unknown subcommand: '{subcommand}'. " +
                "Use 'status', 'default', 'load', or 'preview'."),
        };
    }

    private static CommandResult ExecuteStatus(IDebugContext context)
    {
        var videoDevice = context.Machine?.GetComponent<IVideoDevice>();

        context.Output.WriteLine();
        context.Output.WriteLine("Character ROM Status");
        context.Output.WriteLine("════════════════════");

        if (videoDevice == null)
        {
            context.Output.WriteLine("  Video device: Not found");
            context.Output.WriteLine();
            return CommandResult.Ok();
        }

        context.Output.WriteLine($"  Video device: {videoDevice.Name}");
        context.Output.WriteLine($"  Character ROM loaded: {(videoDevice.IsCharacterRomLoaded ? "Yes" : "No")}");
        context.Output.WriteLine($"  Alternate charset active: {(videoDevice.IsAltCharSet ? "Yes" : "No")}");
        context.Output.WriteLine();

        return CommandResult.Ok();
    }

    private static CommandResult ExecuteLoadDefault(IDebugContext context)
    {
        var videoDevice = context.Machine?.GetComponent<IVideoDevice>();

        if (videoDevice == null)
        {
            return CommandResult.Error("Video device not found in the machine.");
        }

        try
        {
            DefaultCharacterRom.LoadIntoVideoDevice(videoDevice);
            context.Output.WriteLine("Loaded default character ROM (4096 bytes).");
            return CommandResult.Ok();
        }
        catch (InvalidOperationException ex)
        {
            return CommandResult.Error($"Failed to load default character ROM: {ex.Message}");
        }
    }

    private static CommandResult ExecuteLoad(IDebugContext context, string[] args)
    {
        if (args.Length == 0)
        {
            return CommandResult.Error("Filename required. Usage: charactermap load <filename>");
        }

        string filename = args[0];

        var videoDevice = context.Machine?.GetComponent<IVideoDevice>();

        if (videoDevice == null)
        {
            return CommandResult.Error("Video device not found in the machine.");
        }

        if (!File.Exists(filename))
        {
            return CommandResult.Error($"File not found: '{filename}'");
        }

        try
        {
            byte[] data = File.ReadAllBytes(filename);

            if (data.Length != VideoDevice.CharacterRomSize)
            {
                return CommandResult.Error(
                    $"Invalid file size: {data.Length} bytes. " +
                    $"Character ROM must be exactly {VideoDevice.CharacterRomSize} bytes (4KB).");
            }

            videoDevice.LoadCharacterRom(data);
            context.Output.WriteLine($"Loaded character ROM from '{filename}' ({data.Length} bytes).");
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
        catch (ArgumentException ex)
        {
            return CommandResult.Error($"Invalid character ROM data: {ex.Message}");
        }
    }

    private static CommandResult DisplayConsolePreview(IDebugContext context, IVideoDevice videoDevice)
    {
        context.Output.WriteLine();
        context.Output.WriteLine("Character Map Preview (hex codes shown)");
        context.Output.WriteLine("═══════════════════════════════════════");
        context.Output.WriteLine();

        bool useAlt = videoDevice.IsAltCharSet;
        context.Output.WriteLine($"Character set: {(useAlt ? "Alternate" : "Primary")}");
        context.Output.WriteLine();

        // Display character codes in a grid (16 columns)
        context.Output.WriteLine("     0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F");
        context.Output.WriteLine("    ═══════════════════════════════════════════════");

        for (int row = 0; row < 16; row++)
        {
            context.Output.Write($" {row:X}  ");

            for (int col = 0; col < 16; col++)
            {
                byte charCode = (byte)((row << 4) | col);

                // Get first scanline to check if character has content
                byte scanline = videoDevice.GetCharacterScanline(charCode, 3, useAlt);

                // Display a simple indicator of whether the character has content
                char displayChar = scanline != 0 ? '█' : '·';
                context.Output.Write($"{displayChar}  ");
            }

            context.Output.WriteLine();
        }

        context.Output.WriteLine();
        context.Output.WriteLine("█ = Character has pixel data, · = Empty character");
        context.Output.WriteLine();

        return CommandResult.Ok();
    }

    private CommandResult ExecutePreview(IDebugContext context)
    {
        var videoDevice = context.Machine?.GetComponent<IVideoDevice>();

        if (videoDevice == null)
        {
            return CommandResult.Error("Video device not found in the machine.");
        }

        if (!videoDevice.IsCharacterRomLoaded)
        {
            context.Output.WriteLine("No character ROM loaded. Use 'charactermap default' to load the default ROM.");
            return CommandResult.Ok();
        }

        // If window manager is available, try to open preview window
        // Pass the video device directly so the window can get its character ROM
        if (windowManager != null)
        {
            _ = windowManager.ShowWindowAsync("CharacterPreview", videoDevice);
            return CommandResult.Ok("Opening character preview window...");
        }

        // Fallback: display ASCII preview in console
        return DisplayConsolePreview(context, videoDevice);
    }
}