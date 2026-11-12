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
    /// Gets or sets a value indicating whether the REPL should display the input prompt
    /// before reading each command. Set to false for non-interactive / scripted usage.
    /// </summary>
    public bool ShowPrompt { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to print the exit message (e.g. "Goodbye!")
    /// when the REPL exits. Defaults to true; automatically set to false for non-interactive
    /// input to keep captured output clean.
    /// </summary>
    public bool PrintExitMessage { get; set; } = true;

    /// <summary>
    /// Gets or sets whether this REPL is interactive. If null (default), auto-detected
    /// based on whether the input reader is the real console input and not redirected.
    /// Used to control banner/prompt/exit message for non-interactive friendliness.
    /// </summary>
    public bool? IsInteractive { get; set; }

    /// <summary>
    /// Creates a REPL with standard console I/O and default built-in commands.
    /// </summary>
    /// <param name="windowManager">
    /// Optional debug window manager for opening popup windows from commands.
    /// If null, commands that support popups will fall back to console output.
    /// </param>
    /// <returns>A new <see cref="DebugRepl"/> configured for console use.</returns>
    public static DebugRepl CreateConsoleRepl(IDebugWindowManager? windowManager = null)
    {
        var dispatcher = new CommandDispatcher();

        // Register built-in commands
        dispatcher.Register(new HelpCommand());
        dispatcher.Register(new ExitCommand());
        dispatcher.Register(new VersionCommand());
        dispatcher.Register(new ClearCommand());
        dispatcher.Register(new AboutCommand(windowManager));

        var context = DebugContext.CreateConsoleContext(dispatcher);

        return new(dispatcher, context, Console.In);
    }

    /// <summary>
    /// Runs the REPL until the user exits or input is exhausted.
    /// </summary>
    public void Run()
    {
        bool isInteractiveInput = this.IsInteractive ?? (this.input == Console.In && !Console.IsInputRedirected);
        bool effectiveShowBanner = this.ShowBanner && isInteractiveInput;
        bool effectiveShowPrompt = this.ShowPrompt && isInteractiveInput;
        bool effectivePrintExit = this.PrintExitMessage && isInteractiveInput;

        if (effectiveShowBanner)
        {
            this.DisplayBanner();
        }

        while (true)
        {
            if (effectiveShowPrompt)
            {
                this.context.Output.Write(this.prompt);
            }

            var line = this.input.ReadLine();
            if (line is null)
            {
                // End of input
                break;
            }

            var result = this.ProcessLine(line);
            if (result.ShouldExit)
            {
                if (!string.IsNullOrEmpty(result.Message) && effectivePrintExit)
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

    /// <summary>
    /// Lightweight execution path for running a sequence of commands and exiting.
    /// Bypasses the interactive REPL loop (prompts, banner handling in Run, etc.).
    /// Ideal for --exec, --file, scripting, and AI agent use. Command outputs
    /// are written to the context's Output/Error as usual. Stops on first
    /// command that signals exit.
    /// </summary>
    /// <param name="commands">The command lines to execute in order.</param>
    public void ExecuteBatch(IEnumerable<string> commands)
    {
        foreach (var raw in commands)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var result = ProcessLine(raw);
            if (result.ShouldExit)
            {
                break;
            }
        }
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