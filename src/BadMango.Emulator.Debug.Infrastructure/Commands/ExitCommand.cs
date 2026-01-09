// <copyright file="ExitCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

/// <summary>
/// Exits the debug console.
/// </summary>
/// <remarks>
/// Signals the REPL to terminate gracefully.
/// </remarks>
public sealed class ExitCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExitCommand"/> class.
    /// </summary>
    public ExitCommand()
        : base("exit", "Exit the debug console")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["quit", "q"];

    /// <inheritdoc/>
    public string Synopsis => "exit";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Exits the debug console and returns control to the operating system. " +
        "Any unsaved state or changes will be lost. Use 'save' to save memory " +
        "contents to a file before exiting if needed.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "exit                    Exit the debug console",
        "quit                    Alias for exit",
        "q                       Short alias for exit",
    ];

    /// <inheritdoc/>
    public string? SideEffects =>
        "Terminates the debug session. Unsaved state will be lost.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["save", "help"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);

        return CommandResult.Exit("Goodbye!");
    }
}