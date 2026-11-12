// <copyright file="ICommandContext.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

/// <summary>
/// Provides context for command execution including access to emulator components.
/// </summary>
/// <remarks>
/// The command context serves as the bridge between command handlers and the
/// emulator subsystems (CPU, bus, memory, etc.). It provides a consistent
/// interface for commands to interact with the emulator state.
/// </remarks>
public interface ICommandContext
{
    /// <summary>
    /// Gets the command dispatcher for accessing other commands.
    /// </summary>
    ICommandDispatcher Dispatcher { get; }

    /// <summary>
    /// Gets or sets the console output writer. Can be replaced for capture in agent mode.
    /// </summary>
    TextWriter Output { get; set; }

    /// <summary>
    /// Gets or sets the console error writer. Can be replaced for capture in agent mode.
    /// </summary>
    TextWriter Error { get; set; }

    /// <summary>
    /// Gets the console input reader for interactive commands.
    /// </summary>
    /// <remarks>
    /// This property may be null if the context does not support interactive input.
    /// Commands should check for null before attempting to read input.
    /// </remarks>
    TextReader? Input { get; }

    /// <summary>
    /// Gets a value indicating whether output should be in JSON format (structured)
    /// rather than human-readable text.
    /// </summary>
    bool JsonOutput { get; }
}