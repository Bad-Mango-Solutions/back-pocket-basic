// <copyright file="LogCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

/// <summary>
/// Opens the warning, error, and fatal log window.
/// </summary>
/// <remarks>
/// <para>
/// This command opens an Avalonia-based log window that displays warning, error,
/// and fatal events without cluttering REPL command output.
/// </para>
/// <para>
/// If no window manager is registered (for example in headless mode),
/// the command reports an error.
/// </para>
/// </remarks>
public sealed class LogCommand : CommandHandlerBase, ICommandHelp
{
    private readonly IDebugWindowManager? windowManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogCommand"/> class.
    /// </summary>
    /// <param name="windowManager">Optional debug window manager for opening popup windows.</param>
    public LogCommand(IDebugWindowManager? windowManager = null)
        : base("log", "Open the warning/error/fatal log window")
    {
        this.windowManager = windowManager;
    }

    /// <inheritdoc/>
    public string Synopsis => "log [window]";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Opens the log window for warning, error, and fatal diagnostics. " +
        "Use this to inspect important runtime issues without mixing them into REPL output.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } =
    [
        new("window", null, "subcommand", "Open or focus the log window", null),
    ];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "log                       Open the log window",
        "log window                Open or focus the log window",
    ];

    /// <inheritdoc/>
    public string? SideEffects => "Opens a popup window when Avalonia UI is available.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["statmon", "schedmon", "trapmon"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (this.windowManager is null)
        {
            return CommandResult.Error(
                "Log window requires a graphical UI environment. " +
                "No window manager is available.");
        }

        if (args.Length > 1 || (args.Length > 0 && !args[0].Equals("window", StringComparison.OrdinalIgnoreCase)))
        {
            return CommandResult.Error("Usage: log [window]");
        }

        _ = this.windowManager.ShowWindowAsync("Log");
        return CommandResult.Ok("Opening log window...");
    }
}