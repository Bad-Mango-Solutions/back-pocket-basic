// <copyright file="CharacterMapCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands.DeviceCommands;

using Devices;
using Devices.Interfaces;

/// <summary>
/// Manages character ROM data for the character device.
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
    public string Synopsis => "charactermap <load|preview|edit|default|status> [args]";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Manages character ROM data for the character device. The character ROM contains " +
        "bitmap font data used for text mode rendering. Supports loading custom character " +
        "sets from 4KB binary files, previewing the active character map, and editing glyphs.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "charactermap status                   Show character ROM status",
        "charactermap default                  Load the built-in default character ROM",
        "charactermap load font.rom            Load character data from font.rom",
        "charmap load library://roms/font.rom  Load from library directory",
        "charactermap preview                  Preview the character set in console",
        "charactermap edit                     Open the glyph editor window",
        "charmap default                       Short alias for loading default ROM",
    ];

    /// <inheritdoc/>
    public string? SideEffects =>
        "The 'load' and 'default' subcommands modify the character device's character ROM. " +
        "The 'preview' and 'edit' subcommands may open a window if Avalonia UI is available.";

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
            "edit" => ExecuteEdit(debugContext),
            _ => CommandResult.Error(
                $"Unknown subcommand: '{subcommand}'. " +
                "Use 'status', 'default', 'load', 'preview', or 'edit'."),
        };
    }

    private static CommandResult ExecuteStatus(IDebugContext context)
    {
        var characterDevice = context.Machine?.GetComponent<ICharacterDevice>();

        context.Output.WriteLine();
        context.Output.WriteLine("Character ROM Status");
        context.Output.WriteLine("════════════════════");

        if (characterDevice == null)
        {
            context.Output.WriteLine("  Character device: Not found");
            context.Output.WriteLine();
            return CommandResult.Ok();
        }

        context.Output.WriteLine($"  Character device: {characterDevice.Name}");
        context.Output.WriteLine($"  Character ROM loaded: {(characterDevice.IsCharacterRomLoaded ? "Yes" : "No")}");
        context.Output.WriteLine($"  Alternate charset active: {(characterDevice.IsAltCharSet ? "Yes" : "No")}");
        context.Output.WriteLine($"  Glyph bank 1 overlay: {(characterDevice.IsAltGlyph1Enabled ? "Enabled" : "Disabled")}");
        context.Output.WriteLine($"  Glyph bank 2 overlay: {(characterDevice.IsAltGlyph2Enabled ? "Enabled" : "Disabled")}");
        context.Output.WriteLine($"  No-flash bank 1: {(characterDevice.IsNoFlash1Enabled ? "Enabled" : "Disabled")}");
        context.Output.WriteLine($"  No-flash bank 2: {(characterDevice.IsNoFlash2Enabled ? "Enabled" : "Disabled")}");
        context.Output.WriteLine();

        return CommandResult.Ok();
    }

    private static CommandResult ExecuteLoadDefault(IDebugContext context)
    {
        var characterDevice = context.Machine?.GetComponent<ICharacterDevice>();

        if (characterDevice == null)
        {
            return CommandResult.Error("Character device not found in the machine.");
        }

        try
        {
            DefaultCharacterRom.LoadIntoCharacterDevice(characterDevice);
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

        var characterDevice = context.Machine?.GetComponent<ICharacterDevice>();

        if (characterDevice == null)
        {
            return CommandResult.Error("Character device not found in the machine.");
        }

        // Resolve the path using the path resolver if available
        string resolvedPath = filename;
        if (context.PathResolver is not null)
        {
            if (!context.PathResolver.TryResolve(filename, out string? resolved))
            {
                return CommandResult.Error($"Cannot resolve path: '{filename}'. Library root may not be configured.");
            }

            resolvedPath = resolved!;
        }

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

            if (data.Length != CharacterDevice.CharacterRomSize)
            {
                return CommandResult.Error(
                    $"Invalid file size: {data.Length} bytes. " +
                    $"Character ROM must be exactly {CharacterDevice.CharacterRomSize} bytes (4KB).");
            }

            characterDevice.LoadCharacterRom(data);
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

    private static CommandResult DisplayConsolePreview(IDebugContext context, ICharacterDevice characterDevice)
    {
        context.Output.WriteLine();
        context.Output.WriteLine("Character Map Preview (hex codes shown)");
        context.Output.WriteLine("═══════════════════════════════════════");
        context.Output.WriteLine();

        bool useAlt = characterDevice.IsAltCharSet;
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
                byte scanline = characterDevice.GetCharacterScanline(charCode, 3, useAlt);

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
        var characterDevice = context.Machine?.GetComponent<ICharacterDevice>();

        if (characterDevice == null)
        {
            return CommandResult.Error("Character device not found in the machine.");
        }

        if (!characterDevice.IsCharacterRomLoaded)
        {
            context.Output.WriteLine("No character ROM loaded. Use 'charactermap default' to load the default ROM.");
            return CommandResult.Ok();
        }

        // If window manager is available, try to open preview window
        // Pass the character device directly so the window can get its character ROM
        if (windowManager != null)
        {
            _ = windowManager.ShowWindowAsync("CharacterPreview", characterDevice);
            return CommandResult.Ok("Opening character preview window...");
        }

        // Fallback: display ASCII preview in console
        return DisplayConsolePreview(context, characterDevice);
    }

    private CommandResult ExecuteEdit(IDebugContext context)
    {
        var characterDevice = context.Machine?.GetComponent<ICharacterDevice>();

        // If window manager is available, try to open glyph editor window
        if (windowManager != null)
        {
            _ = windowManager.ShowWindowAsync("GlyphEditor", characterDevice);
            return CommandResult.Ok("Opening glyph editor window...");
        }

        return CommandResult.Error(
            "Glyph editor requires Avalonia UI. " +
            "The glyph editor window is not available in console-only mode.");
    }
}