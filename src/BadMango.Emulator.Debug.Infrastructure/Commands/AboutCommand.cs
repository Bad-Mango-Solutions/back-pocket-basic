// <copyright file="AboutCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using System.Reflection;

using BadMango.Emulator.Devices;

/// <summary>
/// Opens an About window displaying version and copyright information.
/// </summary>
/// <remarks>
/// <para>
/// This command demonstrates the debug window popup infrastructure by opening
/// an Avalonia-based About window from the console REPL.
/// </para>
/// <para>
/// If no window manager is registered (e.g., running in headless mode),
/// the command displays version information directly in the console.
/// </para>
/// </remarks>
[DeviceDebugCommand]
public sealed class AboutCommand : CommandHandlerBase, ICommandHelp
{
    private readonly IDebugWindowManager? windowManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="AboutCommand"/> class.
    /// </summary>
    /// <param name="windowManager">
    /// Optional debug window manager for opening popup windows.
    /// If null, version information is displayed in the console.
    /// </param>
    public AboutCommand(IDebugWindowManager? windowManager = null)
        : base("about", "Display information about the emulator")
    {
        this.windowManager = windowManager;
    }

    /// <inheritdoc/>
    public string Synopsis => "about";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Opens an About window displaying version, copyright, and project information. " +
        "If the Avalonia UI infrastructure is available, a popup window is shown. " +
        "Otherwise, version information is displayed directly in the console.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "about                   Open the About window or display version info",
    ];

    /// <inheritdoc/>
    public string? SideEffects => "Opens a popup window when Avalonia UI is available.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["version", "help"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);

        // If a window manager is available, try to show the popup window
        // ShowWindowAsync will initialize Avalonia if needed
        if (this.windowManager is not null)
        {
            // Fire and forget the async operation - we don't want to block the REPL
            _ = this.windowManager.ShowWindowAsync("About");
            return CommandResult.Ok("Opening About window...");
        }

        // Fallback: display version information in the console (no UI available)
        return this.DisplayConsoleVersion(context);
    }

    private CommandResult DisplayConsoleVersion(ICommandContext context)
    {
        var assembly = typeof(AboutCommand).Assembly;
        var version = assembly.GetName().Version?.ToString() ?? "Unknown";
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? version;

        context.Output.WriteLine();
        context.Output.WriteLine("-----------------------------------------------------------------------");
        context.Output.WriteLine("  BackPocket BASIC - Emulator Debug Console");
        context.Output.WriteLine($"  Version: {informationalVersion}");
        context.Output.WriteLine();
        context.Output.WriteLine("  A fully-featured Applesoft BASIC interpreter with");
        context.Output.WriteLine("  6502/65C02/65816 CPU emulation and Apple II memory space.");
        context.Output.WriteLine();
        context.Output.WriteLine("  Copyright (c) Bad Mango Solutions. All rights reserved.");
        context.Output.WriteLine("  Licensed under the MIT License.");
        context.Output.WriteLine();
        context.Output.WriteLine("  https://github.com/Bad-Mango-Solutions/back-pocket-basic");
        context.Output.WriteLine("-----------------------------------------------------------------------");
        context.Output.WriteLine();

        return CommandResult.Ok();
    }
}