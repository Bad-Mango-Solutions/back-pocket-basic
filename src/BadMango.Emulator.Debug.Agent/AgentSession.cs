// <copyright file="AgentSession.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Agent;

using BadMango.Emulator.Debug.Infrastructure.Commands;

/// <summary>
/// Represents a single agent session with a live emulator instance.
/// </summary>
public sealed class AgentSession : IDisposable
{
    private readonly ICommandDispatcher dispatcher;
    private readonly IDebugContext debugContext;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentSession"/> class.
    /// </summary>
    /// <param name="dispatcher">The command dispatcher used to execute commands in this session.</param>
    /// <param name="debugContext">The debug context that provides execution state and output.</param>
    public AgentSession(ICommandDispatcher dispatcher, IDebugContext debugContext)
    {
        this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        this.debugContext = debugContext ?? throw new ArgumentNullException(nameof(debugContext));
    }

    /// <summary>
    /// Executes a command line and returns the command output.
    /// </summary>
    /// <param name="commandLine">The command line to execute.</param>
    /// <param name="cancellationToken">A cancellation token that can cancel the operation.</param>
    /// <returns>The captured command output, or an empty string when the command line is blank.</returns>
    public async Task<string> ExecuteAsync(string commandLine, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
        {
            return string.Empty;
        }

        var result = dispatcher.Dispatch(debugContext, commandLine);

        if (!string.IsNullOrEmpty(result.Message))
        {
            if (result.Success)
            {
                debugContext.Output.WriteLine(result.Message);
            }
            else
            {
                debugContext.Error.WriteLine("Error: " + result.Message);
            }
        }

        await Task.CompletedTask;
        return result.Message ?? (result.Success ? "ok" : "error");
    }

    /// <summary>
    /// Executes a structured tool call and returns its result.
    /// </summary>
    /// <param name="toolName">The name of the tool to invoke.</param>
    /// <param name="arguments">The arguments for the tool call.</param>
    /// <param name="ct">A cancellation token that can cancel the operation.</param>
    /// <returns>The tool result, or <see langword="null"/> when no structured result is available.</returns>
    public Task<object?> ExecuteStructuredAsync(string toolName, Dictionary<string, object?> arguments, CancellationToken ct = default)
    {
        _ = toolName;
        _ = arguments;
        _ = ct;
        return Task.FromResult<object?>(null);
    }

    /// <summary>
    /// Releases resources used by this session.
    /// </summary>
    public void Dispose()
    {
        if (!disposed)
        {
            (debugContext as IDisposable)?.Dispose();
            disposed = true;
        }
    }
}