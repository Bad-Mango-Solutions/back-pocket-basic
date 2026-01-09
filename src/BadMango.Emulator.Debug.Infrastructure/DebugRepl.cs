// <copyright file="DebugRepl.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure;

using BadMango.Emulator.Debug.Infrastructure.Commands;

/// <summary>
/// Read-Eval-Print Loop (REPL) for the debug console.
/// </summary>
/// <remarks>
/// Provides an interactive command-line interface for debugging
/// the emulator. The REPL reads commands from the user, dispatches
/// them to registered handlers, and displays the results.
/// </remarks>
public sealed class DebugRepl
{
    private readonly ICommandDispatcher dispatcher;
    private readonly ICommandContext context;
    private readonly TextReader input;
    private readonly string prompt;
    private readonly IDebugContext debugContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="DebugRepl"/> class.
    /// </summary>
    /// <param name="dispatcher">The command dispatcher.</param>
    /// <param name="context">The command context.</param>
    /// <param name="input">The input reader.</param>
    /// <param name="prompt">The prompt string to display.</param>
    public DebugRepl(ICommandDispatcher dispatcher, ICommandContext context, TextReader input, string prompt = "> ")
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrEmpty(prompt);

        this.dispatcher = dispatcher;
        this.context = context;
        this.input = input;
        this.prompt = prompt;
        this.debugContext = context as IDebugContext ?? throw new ArgumentException("Context must implement IDebugContext.", nameof(context));
    }

    /// <summary>
    /// Gets or sets a value indicating whether the REPL should show the banner on start.
    /// </summary>
    public bool ShowBanner { get; set; } = true;

    /// <summary>
    /// Creates a REPL with standard console I/O and default built-in commands.
    /// </summary>
    /// <returns>A new <see cref="DebugRepl"/> configured for console use.</returns>
    public static DebugRepl CreateConsoleRepl()
    {
        var dispatcher = new CommandDispatcher();

        // Register built-in commands
        dispatcher.Register(new HelpCommand());
        dispatcher.Register(new ExitCommand());
        dispatcher.Register(new VersionCommand());
        dispatcher.Register(new ClearCommand());

        var context = DebugContext.CreateConsoleContext(dispatcher);

        return new(dispatcher, context, Console.In);
    }

    /// <summary>
    /// Runs the REPL until the user exits or input is exhausted.
    /// </summary>
    public void Run()
    {
        if (this.ShowBanner)
        {
            this.DisplayBanner();
        }

        while (true)
        {
            this.context.Output.Write(this.prompt);

            var line = this.input.ReadLine();
            if (line is null)
            {
                // End of input
                break;
            }

            var result = this.ProcessLine(line);
            if (result.ShouldExit)
            {
                if (!string.IsNullOrEmpty(result.Message))
                {
                    this.context.Output.WriteLine(result.Message);
                }

                break;
            }
        }
    }

    /// <summary>
    /// Processes a single command line.
    /// </summary>
    /// <param name="line">The command line to process.</param>
    /// <returns>The result of processing the command.</returns>
    public CommandResult ProcessLine(string line)
    {
        var trimmed = line.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return CommandResult.Ok();
        }

        var result = this.dispatcher.Dispatch(this.context, trimmed);

        if (!string.IsNullOrEmpty(result.Message) && !result.ShouldExit)
        {
            if (result.Success)
            {
                this.context.Output.WriteLine(result.Message);
            }
            else
            {
                this.context.Error.WriteLine($"Error: {result.Message}");
            }
        }

        return result;
    }

    private void DisplayBanner()
    {
        var machineDescription = this.debugContext.MachineInfo?.Summary ?? "No machine attached";

        this.context.Output.WriteLine("═══════════════════════════════════════════════════════════════════════");
        this.context.Output.WriteLine("  Emulator Debug Console");
        this.context.Output.WriteLine($"  Machine:  {machineDescription}");
        this.context.Output.WriteLine("  Type 'help' for a list of commands, 'exit' to quit");
        this.context.Output.WriteLine("═══════════════════════════════════════════════════════════════════════");
        this.context.Output.WriteLine();
    }
}