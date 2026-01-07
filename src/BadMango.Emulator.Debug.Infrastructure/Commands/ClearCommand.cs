// <copyright file="ClearCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

/// <summary>
/// Clears the console screen.
/// </summary>
/// <remarks>
/// Clears all text from the console and moves the cursor to the top-left corner.
/// </remarks>
public sealed class ClearCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClearCommand"/> class.
    /// </summary>
    public ClearCommand()
        : base("clear", "Clear the console screen")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["cls"];

    /// <inheritdoc/>
    public string Synopsis => "clear";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Clears all text from the console and moves the cursor to the top-left corner. " +
        "If output is redirected (e.g., to a file), this command has no effect.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "clear                   Clear the console screen",
        "cls                     Alias for clear",
    ];

    /// <inheritdoc/>
    public string? SideEffects => null;

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["help", "exit"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            Console.Clear();
        }
        catch (IOException)
        {
            // Console.Clear() can throw if output is redirected
            // In that case, we just do nothing
        }

        return CommandResult.Ok();
    }
}