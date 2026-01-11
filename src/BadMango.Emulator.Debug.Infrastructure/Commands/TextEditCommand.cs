// <copyright file="TextEditCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using BadMango.Emulator.Devices;

/// <summary>
/// Opens a text editor window for editing source code and other text files.
/// </summary>
/// <remarks>
/// <para>
/// This command opens an Avalonia-based text editor window with syntax highlighting
/// support for various file types including plain text, Markdown, JSON, and assembly
/// source code.
/// </para>
/// <para>
/// The editor supports the library:// scheme for opening files in the user's
/// profile directory (~/.backpocket).
/// </para>
/// </remarks>
[DeviceDebugCommand]
public sealed class TextEditCommand : CommandHandlerBase, ICommandHelp
{
    private readonly IDebugWindowManager? windowManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextEditCommand"/> class.
    /// </summary>
    /// <param name="windowManager">
    /// Optional debug window manager for opening popup windows.
    /// If null, an error message is displayed.
    /// </param>
    public TextEditCommand(IDebugWindowManager? windowManager = null)
        : base("textedit", "Open a text editor window")
    {
        this.windowManager = windowManager;
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["edit", "te"];

    /// <inheritdoc/>
    public override string Usage => "textedit [filepath]";

    /// <inheritdoc/>
    public string Synopsis => "textedit [filepath]";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Opens an Avalonia-based text editor window with syntax highlighting support. " +
        "If a file path is provided, the file is opened for editing. " +
        "The editor supports the library:// scheme for opening files in ~/.backpocket. " +
        "Supported file types include plain text (.txt), Markdown (.md), JSON (.json), " +
        "and assembly source files (.s, .asm, .h). " +
        "Multiple editor windows can be opened simultaneously.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } =
    [
        new CommandOption("filepath", null, "string", "Optional path to a file to open. Supports library:// scheme.", null),
    ];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "textedit                      Open a new empty editor window",
        "textedit myfile.s             Open myfile.s for editing",
        "textedit library://roms/test.s   Open a file from the library directory",
    ];

    /// <inheritdoc/>
    public string? SideEffects => "Opens a popup window when Avalonia UI is available.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["about", "help"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);

        // If a window manager is available, try to show the text editor window
        if (this.windowManager is not null)
        {
            // Pass file path as context if provided
            string? filePath = args.Length > 0 ? args[0] : null;

            // Fire and forget the async operation - we don't want to block the REPL
            _ = this.windowManager.ShowWindowAsync("TextEditor", filePath);
            return CommandResult.Ok("Opening text editor window...");
        }

        // No UI available
        context.Output.WriteLine("Text editor requires Avalonia UI infrastructure.");
        context.Output.WriteLine("Run the debugger in UI mode to use this feature.");
        return CommandResult.Error("No UI available");
    }
}