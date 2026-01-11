// <copyright file="StatMonCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

/// <summary>
/// Opens a status monitor window displaying active machine metrics in real-time.
/// </summary>
/// <remarks>
/// <para>
/// This command opens an Avalonia-based status monitor window that displays
/// CPU registers, machine state, performance metrics, and annunciator visualization
/// with live updates.
/// </para>
/// <para>
/// If no window manager is registered (e.g., running in headless mode),
/// the command displays an error message.
/// </para>
/// </remarks>
public sealed class StatMonCommand : CommandHandlerBase, ICommandHelp
{
    private readonly IDebugWindowManager? windowManager;
    private readonly IDebugContext? debugContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatMonCommand"/> class.
    /// </summary>
    /// <param name="windowManager">
    /// Optional debug window manager for opening popup windows.
    /// If null, the command will fail with an error.
    /// </param>
    /// <param name="debugContext">
    /// Optional debug context providing access to the machine.
    /// If null, a machine-less window will be opened.
    /// </param>
    public StatMonCommand(IDebugWindowManager? windowManager = null, IDebugContext? debugContext = null)
        : base("statmon", "Open the status monitor window")
    {
        this.windowManager = windowManager;
        this.debugContext = debugContext;
    }

    /// <inheritdoc/>
    public string Synopsis => "statmon";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Opens a status monitor window that displays active machine metrics in real-time. " +
        "The window shows CPU registers (A, X, Y, P, SP, PC), machine state " +
        "(Running/Paused/Stopped/Halted/WAI), performance metrics (IPS, cycles, MHz), " +
        "annunciator visualization with scrolling history and fade effects, " +
        "and device extension panels (e.g., PocketWatch time display).";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "statmon                   Open the status monitor window",
    ];

    /// <inheritdoc/>
    public string? SideEffects => "Opens a popup window when Avalonia UI is available.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["regs", "about", "switches"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);

        // If a window manager is available, try to show the popup window
        if (windowManager is null)
        {
            return CommandResult.Error(
                "Status monitor requires a graphical UI environment. " +
                "No window manager is available.");
        }

        // Get the machine from the debug context to pass to the window
        object? machineContext = null;
        if (debugContext is { Machine: not null })
        {
            machineContext = debugContext.Machine;
        }

        // Fire and forget the async operation - we don't want to block the REPL
        _ = windowManager.ShowWindowAsync("StatusMonitor", machineContext);
        return CommandResult.Ok("Opening Status Monitor window...");
    }
}