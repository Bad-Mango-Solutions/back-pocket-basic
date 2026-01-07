// <copyright file="ICommandHelp.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

/// <summary>
/// Provides enhanced help documentation for a command.
/// </summary>
/// <remarks>
/// Commands implementing this interface provide detailed help documentation
/// including synopsis, description, options, examples, side effects, and
/// related commands.
/// </remarks>
public interface ICommandHelp
{
    /// <summary>
    /// Gets the primary name of the command.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a one-line usage pattern for the command.
    /// </summary>
    /// <example>"read &lt;address&gt; [count]".</example>
    string Synopsis { get; }

    /// <summary>
    /// Gets a detailed explanation of the command (2-5 sentences).
    /// </summary>
    string DetailedDescription { get; }

    /// <summary>
    /// Gets the list of command options with their descriptions.
    /// </summary>
    IReadOnlyList<CommandOption> Options { get; }

    /// <summary>
    /// Gets practical usage examples for the command.
    /// </summary>
    IReadOnlyList<string> Examples { get; }

    /// <summary>
    /// Gets a description of what state the command modifies, or null if no side effects.
    /// </summary>
    string? SideEffects { get; }

    /// <summary>
    /// Gets a list of related command names.
    /// </summary>
    IReadOnlyList<string> SeeAlso { get; }
}