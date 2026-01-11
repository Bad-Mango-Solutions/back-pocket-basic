// <copyright file="VideoCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using BadMango.Emulator.Devices;

/// <summary>
/// Command to manage the video display window.
/// </summary>
/// <remarks>
/// <para>
/// This command provides subcommands to open, close, and configure the video
/// display window. The video window shows the emulated display output and
/// accepts keyboard input.
/// </para>
/// <para>
/// Subcommands:
/// </para>
/// <list type="bullet">
/// <item><description>open - Opens the video display window</description></item>
/// <item><description>close - Closes the video display window</description></item>
/// <item><description>scale &lt;n&gt; - Sets the display scale (1-4)</description></item>
/// <item><description>fps [on|off] - Toggles FPS display overlay</description></item>
/// <item><description>refresh - Forces a display refresh</description></item>
/// </list>
/// </remarks>
[DeviceDebugCommand]
public sealed class VideoCommand : CommandHandlerBase, ICommandHelp
{
    private readonly IDebugWindowManager? windowManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoCommand"/> class.
    /// </summary>
    /// <param name="windowManager">
    /// Optional debug window manager for managing the video window.
    /// </param>
    public VideoCommand(IDebugWindowManager? windowManager = null)
        : base("video", "Manage the video display window")
    {
        this.windowManager = windowManager;
    }

    /// <inheritdoc/>
    public string Synopsis => "video [open|close|scale <n>|fps [on|off]|refresh]";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Manages the video display window for viewing emulated graphics output. " +
        "The window shows the current display mode (text, lo-res, or hi-res) " +
        "and accepts keyboard input that is forwarded to the emulated system.\n\n" +
        "Subcommands:\n" +
        "  open    - Open the video display window\n" +
        "  close   - Close the video display window\n" +
        "  scale n - Set display scale (1=native, 2=2×, 3=3×, 4=4×)\n" +
        "  fps     - Toggle FPS display, or 'fps on'/'fps off'\n" +
        "  refresh - Force an immediate display refresh";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } =
    [
        new("open", null, "subcommand", "Open the video display window", null),
        new("close", null, "subcommand", "Close the video display window", null),
        new("scale <n>", null, "subcommand", "Set display scale factor (1-4)", null),
        new("fps [on|off]", null, "subcommand", "Toggle or set FPS display overlay", null),
        new("refresh", null, "subcommand", "Force an immediate display refresh", null),
    ];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "video open              Open the video display window",
        "video close             Close the video display window",
        "video scale 2           Set display scale to 2× (default)",
        "video scale 1           Set display to native resolution",
        "video fps               Toggle FPS display",
        "video fps on            Enable FPS display",
        "video refresh           Force display refresh",
    ];

    /// <inheritdoc/>
    public string? SideEffects => "Opens, closes, or modifies the video display window.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["print", "plot", "hplot", "gr", "hgr", "text"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (args.Length == 0)
        {
            return ShowStatus(context);
        }

        string subcommand = args[0].ToLowerInvariant();
        string[] subArgs = args.Length > 1 ? args[1..] : [];

        return subcommand switch
        {
            "open" => OpenWindow(context),
            "close" => CloseWindow(context),
            "scale" => SetScale(context, subArgs),
            "fps" => ToggleFps(context, subArgs),
            "refresh" => RefreshDisplay(context),
            _ => CommandResult.Error($"Unknown subcommand: {subcommand}. Use 'help video' for usage."),
        };
    }

    private CommandResult ShowStatus(ICommandContext context)
    {
        if (windowManager is null)
        {
            context.Output.WriteLine("Video window manager not available (headless mode).");
            return CommandResult.Ok();
        }

        bool isOpen = windowManager.IsWindowOpen("Video");
        context.Output.WriteLine($"Video window: {(isOpen ? "open" : "closed")}");
        return CommandResult.Ok();
    }

    private CommandResult OpenWindow(ICommandContext context)
    {
        if (windowManager is null)
        {
            return CommandResult.Error("Video window not available in headless mode.");
        }

        if (windowManager.IsWindowOpen("Video"))
        {
            context.Output.WriteLine("Video window is already open.");
            return CommandResult.Ok();
        }

        // Get machine from debug context to pass to video window
        object? machineContext = null;
        if (context is IDebugContext debugContext)
        {
            machineContext = debugContext.Machine;
        }

        // Fire and forget - don't block the REPL
        _ = windowManager.ShowWindowAsync("Video", machineContext);
        return CommandResult.Ok("Opening video window...");
    }

    private CommandResult CloseWindow(ICommandContext context)
    {
        if (windowManager is null)
        {
            return CommandResult.Error("Video window not available in headless mode.");
        }

        if (!windowManager.IsWindowOpen("Video"))
        {
            context.Output.WriteLine("Video window is not open.");
            return CommandResult.Ok();
        }

        _ = windowManager.CloseWindowAsync("Video");
        return CommandResult.Ok("Closing video window...");
    }

    private CommandResult SetScale(ICommandContext context, string[] args)
    {
        if (args.Length == 0)
        {
            return CommandResult.Error("Usage: video scale <1-4>");
        }

        if (!int.TryParse(args[0], out int scale) || scale < 1 || scale > 4)
        {
            return CommandResult.Error("Scale must be between 1 and 4.");
        }

        if (windowManager is null)
        {
            return CommandResult.Error("Video window not available in headless mode.");
        }

        // Pass scale as context to the window
        _ = windowManager.ShowWindowAsync("Video", new VideoWindowContext { Scale = scale });
        return CommandResult.Ok($"Setting video scale to {scale}×...");
    }

    private CommandResult ToggleFps(ICommandContext context, string[] args)
    {
        if (windowManager is null)
        {
            return CommandResult.Error("Video window not available in headless mode.");
        }

        bool? showFps = null;
        if (args.Length > 0)
        {
            showFps = args[0].ToLowerInvariant() switch
            {
                "on" or "true" or "1" => true,
                "off" or "false" or "0" => false,
                _ => null,
            };

            if (showFps is null)
            {
                return CommandResult.Error("Usage: video fps [on|off]");
            }
        }

        _ = windowManager.ShowWindowAsync("Video", new VideoWindowContext { ToggleFps = true, ShowFps = showFps });
        return CommandResult.Ok(showFps.HasValue
            ? $"FPS display {(showFps.Value ? "enabled" : "disabled")}."
            : "FPS display toggled.");
    }

    private CommandResult RefreshDisplay(ICommandContext context)
    {
        if (windowManager is null)
        {
            return CommandResult.Error("Video window not available in headless mode.");
        }

        if (!windowManager.IsWindowOpen("Video"))
        {
            return CommandResult.Error("Video window is not open. Use 'video open' first.");
        }

        _ = windowManager.ShowWindowAsync("Video", new VideoWindowContext { ForceRefresh = true });
        return CommandResult.Ok("Display refreshed.");
    }
}