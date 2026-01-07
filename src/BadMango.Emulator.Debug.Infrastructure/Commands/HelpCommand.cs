// <copyright file="HelpCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

/// <summary>
/// Displays help information for available commands.
/// </summary>
/// <remarks>
/// When invoked without arguments, lists all available commands.
/// When invoked with a command name, shows detailed help for that command.
/// </remarks>
public sealed class HelpCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HelpCommand"/> class.
    /// </summary>
    public HelpCommand()
        : base("help", "Display help information for available commands")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["?", "h"];

    /// <inheritdoc/>
    public override string Usage => "help [command]";

    /// <inheritdoc/>
    public string Synopsis => "help [command]";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "When invoked without arguments, lists all available commands with brief descriptions. " +
        "When invoked with a command name, shows detailed help for that command including " +
        "synopsis, description, options, examples, side effects, and related commands.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "help                    List all available commands",
        "help run                Show detailed help for the 'run' command",
        "help call               Show detailed help for the 'call' command",
    ];

    /// <inheritdoc/>
    public string? SideEffects => null;

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["version"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (args.Length > 0)
        {
            return this.ShowCommandHelp(context, args[0]);
        }

        return this.ShowAllCommands(context);
    }

    private CommandResult ShowAllCommands(ICommandContext context)
    {
        context.Output.WriteLine("Available commands:");
        context.Output.WriteLine();

        var commands = context.Dispatcher.Commands.OrderBy(c => c.Name);
        var maxNameLength = commands.Max(c => c.Name.Length);

        foreach (var command in commands)
        {
            var padding = new string(' ', maxNameLength - command.Name.Length + 2);
            context.Output.WriteLine($"  {command.Name}{padding}{command.Description}");
        }

        context.Output.WriteLine();
        context.Output.WriteLine("Type 'help <command>' for more information on a specific command.");

        return CommandResult.Ok();
    }

    private CommandResult ShowCommandHelp(ICommandContext context, string commandName)
    {
        if (!context.Dispatcher.TryGetHandler(commandName, out var handler) || handler is null)
        {
            return CommandResult.Error($"Unknown command: '{commandName}'");
        }

        // Check if the handler provides enhanced help
        if (handler is ICommandHelp helpProvider)
        {
            ShowEnhancedHelp(context, handler, helpProvider);
        }
        else
        {
            ShowBasicHelp(context, handler);
        }

        return CommandResult.Ok();
    }

    private static void ShowBasicHelp(ICommandContext context, ICommandHandler handler)
    {
        context.Output.WriteLine($"Command: {handler.Name}");
        context.Output.WriteLine($"Description: {handler.Description}");
        context.Output.WriteLine($"Usage: {handler.Usage}");

        if (handler.Aliases.Count > 0)
        {
            context.Output.WriteLine($"Aliases: {string.Join(", ", handler.Aliases)}");
        }
    }

    private static void ShowEnhancedHelp(ICommandContext context, ICommandHandler handler, ICommandHelp helpProvider)
    {
        // Header
        context.Output.WriteLine($"{handler.Name} - {handler.Description}");
        context.Output.WriteLine();

        // Synopsis
        context.Output.WriteLine("SYNOPSIS");
        context.Output.WriteLine($"    {helpProvider.Synopsis}");
        context.Output.WriteLine();

        // Description
        context.Output.WriteLine("DESCRIPTION");
        context.Output.WriteLine($"    {helpProvider.DetailedDescription}");
        context.Output.WriteLine();

        // Aliases
        if (handler.Aliases.Count > 0)
        {
            context.Output.WriteLine("ALIASES");
            context.Output.WriteLine($"    {string.Join(", ", handler.Aliases)}");
            context.Output.WriteLine();
        }

        // Options
        if (helpProvider.Options.Count > 0)
        {
            context.Output.WriteLine("OPTIONS");
            context.Output.WriteLine($"    {"Option",-20} {"Type",-10} {"Default",-12} Description");
            context.Output.WriteLine($"    {new string('─', 70)}");

            foreach (var option in helpProvider.Options)
            {
                string optionName = option.ShortName is not null
                    ? $"{option.Name}, {option.ShortName}"
                    : option.Name;
                string defaultValue = option.DefaultValue ?? "required";

                context.Output.WriteLine($"    {optionName,-20} {option.Type,-10} {defaultValue,-12} {option.Description}");
            }

            context.Output.WriteLine();
        }

        // Examples
        if (helpProvider.Examples.Count > 0)
        {
            context.Output.WriteLine("EXAMPLES");
            foreach (var example in helpProvider.Examples)
            {
                context.Output.WriteLine($"    {example}");
            }

            context.Output.WriteLine();
        }

        // Side effects
        if (!string.IsNullOrEmpty(helpProvider.SideEffects))
        {
            context.Output.WriteLine("SIDE EFFECTS");
            context.Output.WriteLine($"    {helpProvider.SideEffects}");
            context.Output.WriteLine();
        }
        else
        {
            context.Output.WriteLine("SIDE EFFECTS");
            context.Output.WriteLine("    None - this command does not modify emulation state.");
            context.Output.WriteLine();
        }

        // See also
        if (helpProvider.SeeAlso.Count > 0)
        {
            context.Output.WriteLine("SEE ALSO");
            context.Output.WriteLine($"    {string.Join(", ", helpProvider.SeeAlso)}");
        }
    }
}