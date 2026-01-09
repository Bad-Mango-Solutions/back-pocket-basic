// <copyright file="CommandContext.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

/// <summary>
/// Default implementation of <see cref="ICommandContext"/>.
/// </summary>
/// <remarks>
/// Provides command handlers with access to the dispatcher and console I/O.
/// </remarks>
public sealed class CommandContext : ICommandContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandContext"/> class.
    /// </summary>
    /// <param name="dispatcher">The command dispatcher.</param>
    /// <param name="output">The output writer.</param>
    /// <param name="error">The error writer.</param>
    /// <param name="input">The input reader for interactive commands.</param>
    public CommandContext(ICommandDispatcher dispatcher, TextWriter output, TextWriter error, TextReader? input = null)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        this.Dispatcher = dispatcher;
        this.Output = output;
        this.Error = error;
        this.Input = input;
    }

    /// <inheritdoc/>
    public ICommandDispatcher Dispatcher { get; }

    /// <inheritdoc/>
    public TextWriter Output { get; }

    /// <inheritdoc/>
    public TextWriter Error { get; }

    /// <inheritdoc/>
    public TextReader? Input { get; }

    /// <summary>
    /// Creates a command context using the standard console streams.
    /// </summary>
    /// <param name="dispatcher">The command dispatcher.</param>
    /// <returns>A new <see cref="CommandContext"/> using console streams.</returns>
    public static CommandContext CreateConsoleContext(ICommandDispatcher dispatcher)
    {
        return new(dispatcher, Console.Out, Console.Error, Console.In);
    }
}