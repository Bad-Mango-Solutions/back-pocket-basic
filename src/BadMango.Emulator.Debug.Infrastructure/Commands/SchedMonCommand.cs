// <copyright file="SchedMonCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

/// <summary>
/// Opens a schedule monitor window for monitoring scheduler events in real-time.
/// </summary>
/// <remarks>
/// <para>
/// This command opens an Avalonia-based schedule monitor window that displays
/// pending scheduler events, event history, and allows filtering by device and
/// event kind for diagnostic purposes.
/// </para>
/// <para>
/// If no window manager is registered (e.g., running in headless mode),
/// the command displays an error message.
/// </para>
/// </remarks>
public sealed class SchedMonCommand : CommandHandlerBase, ICommandHelp
{
    private readonly IDebugWindowManager? windowManager;
    private readonly IDebugContext? debugContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchedMonCommand"/> class.
    /// </summary>
    /// <param name="windowManager">
    /// Optional debug window manager for opening popup windows.
    /// If null, the command will fail with an error.
    /// </param>
    /// <param name="debugContext">
    /// Optional debug context providing access to the machine.
    /// If null, a machine-less window will be opened.
    /// </param>
    public SchedMonCommand(IDebugWindowManager? windowManager = null, IDebugContext? debugContext = null)
        : base("schedmon", "Open the schedule monitor window")
    {
        this.windowManager = windowManager;
        this.debugContext = debugContext;
    }

    /// <inheritdoc/>
    public string Synopsis => "schedmon";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Opens a schedule monitor window that displays scheduler events in real-time. " +
        "The window shows pending events (handle, device, kind, due cycle, priority), " +
        "event history with scheduling and consumption timestamps, and filtering options " +
        "for device and event kind. Useful for diagnosing timing issues and understanding " +
        "device scheduling behavior.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "schedmon                  Open the schedule monitor window",
    ];

    /// <inheritdoc/>
    public string? SideEffects => "Opens a popup window when Avalonia UI is available.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["statmon", "trapmon"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);

        // If a window manager is available, try to show the popup window
        if (windowManager is null)
        {
            return CommandResult.Error(
                "Schedule monitor requires a graphical UI environment. " +
                "No window manager is available.");
        }

        // Get the machine from the debug context to pass to the window
        object? machineContext = null;
        if (debugContext is { Machine: not null })
        {
            machineContext = debugContext.Machine;
        }

        // Fire and forget the async operation - we don't want to block the REPL
        _ = windowManager.ShowWindowAsync("ScheduleMonitor", machineContext);
        return CommandResult.Ok("Opening Schedule Monitor window...");
    }
}