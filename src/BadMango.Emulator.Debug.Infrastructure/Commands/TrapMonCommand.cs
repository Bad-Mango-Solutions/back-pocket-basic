// <copyright file="TrapMonCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

/// <summary>
/// Opens a trap monitor window for monitoring trap registration and invocation.
/// </summary>
/// <remarks>
/// <para>
/// This command opens an Avalonia-based trap monitor window that displays
/// all registered traps, their enabled/disabled state, and logs trap invocations
/// in real-time for diagnostic purposes.
/// </para>
/// <para>
/// If no window manager is registered (e.g., running in headless mode),
/// the command displays an error message.
/// </para>
/// </remarks>
public sealed class TrapMonCommand : CommandHandlerBase, ICommandHelp
{
    private readonly IDebugWindowManager? windowManager;
    private readonly IDebugContext? debugContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrapMonCommand"/> class.
    /// </summary>
    /// <param name="windowManager">
    /// Optional debug window manager for opening popup windows.
    /// If null, the command will fail with an error.
    /// </param>
    /// <param name="debugContext">
    /// Optional debug context providing access to the machine.
    /// If null, a machine-less window will be opened.
    /// </param>
    public TrapMonCommand(IDebugWindowManager? windowManager = null, IDebugContext? debugContext = null)
        : base("trapmon", "Open the trap monitor window")
    {
        this.windowManager = windowManager;
        this.debugContext = debugContext;
    }

    /// <inheritdoc/>
    public string Synopsis => "trapmon";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Opens a trap monitor window that displays all registered traps and their invocations " +
        "in real-time. The window shows trap metadata (address, name, category, operation, " +
        "memory context, slot, enabled state), filtering options for category, operation, " +
        "context, and enabled state, plus a searchable invocation log. Useful for diagnosing " +
        "ROM interception behavior and trap-related issues.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "trapmon                   Open the trap monitor window",
    ];

    /// <inheritdoc/>
    public string? SideEffects => "Opens a popup window when Avalonia UI is available.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["statmon", "schedmon"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);

        // If a window manager is available, try to show the popup window
        if (windowManager is null)
        {
            return CommandResult.Error(
                "Trap monitor requires a graphical UI environment. " +
                "No window manager is available.");
        }

        // Get the machine from the debug context to pass to the window
        object? machineContext = null;
        if (debugContext is { Machine: not null })
        {
            machineContext = debugContext.Machine;
        }

        // Fire and forget the async operation - we don't want to block the REPL
        _ = windowManager.ShowWindowAsync("TrapMonitor", machineContext);
        return CommandResult.Ok("Opening Trap Monitor window...");
    }
}