// <copyright file="VideoCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using System.Linq;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Devices;
using BadMango.Emulator.Devices.Interfaces;
using BadMango.Emulator.Rendering;

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
/// <item><description>color &lt;mode&gt; - Sets the color mode (green, amber, white, color)</description></item>
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
    public string Synopsis => "video [open|close|scale <n>|color <mode>|fps [on|off]|refresh|state|screen|capture|memory]";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Manages the video display window for viewing emulated graphics output. " +
        "The window shows the current display mode (text, lo-res, or hi-res) " +
        "and accepts keyboard input that is forwarded to the emulated system.\n\n" +
        "Subcommands:\n" +
        "  open     - Open the video display window\n" +
        "  close    - Close the video display window\n" +
        "  scale n  - Set display scale (1=native, 2=2×, 3=3×, 4=4×)\n" +
        "  color m  - Set color mode: green, amber, white, or color\n" +
        "  fps      - Toggle FPS display, or 'fps on'/'fps off'\n" +
        "  refresh  - Force an immediate display refresh\n" +
        "  state    - Show current video mode and flags (JSON friendly)\n" +
        "  screen   - Dump logical screen content (text grid or graphics data)\n" +
        "  capture  - Render current frame buffer (for AI diagnostics)\n" +
        "  memory   - Show video memory pages in use\n" +
        "  audio    - Show basic audio/speaker state (headless friendly)";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } =
    [
        new("open", null, "subcommand", "Open the video display window", null),
        new("close", null, "subcommand", "Close the video display window", null),
        new("scale <n>", null, "subcommand", "Set display scale factor (1-4)", null),
        new("color <mode>", null, "subcommand", "Set color mode: green, amber, white, or color", null),
        new("fps [on|off]", null, "subcommand", "Toggle or set FPS display overlay", null),
        new("refresh", null, "subcommand", "Force an immediate display refresh", null),
        new("state", null, "subcommand", "Show current video mode/flags (good for --json)", null),
        new("screen", null, "subcommand", "Dump decoded screen content for current mode", null),
        new("capture", null, "subcommand", "Capture current frame buffer (pixels for AI)", null),
        new("memory", null, "subcommand", "Show active video memory pages", null),
        new("audio", null, "subcommand", "Report speaker/audio state for headless diagnostics", null),
    ];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "video open              Open the video display window",
        "video close             Close the video display window",
        "video scale 2           Set display scale to 2× (default)",
        "video scale 1           Set display to native resolution",
        "video color green       Set classic green phosphor display",
        "video color amber       Set amber phosphor display",
        "video color white       Set white phosphor display",
        "video color color       Set full color mode for graphics",
        "video fps               Toggle FPS display",
        "video fps on            Enable FPS display",
        "video refresh           Force display refresh",
        "video state             Current mode (use with --json)",
        "video screen            Decoded screen content",
        "video capture           Frame buffer capture (headless friendly)",
        "video memory            Video RAM pages in use",
    ];

    /// <inheritdoc/>
    public string? SideEffects => "Opens, closes, or modifies the video display window.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["print", "plot", "hplot", "gr", "hgr", "text", "mem", "read", "peek", "switches"];

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
            "color" => SetColorMode(context, subArgs),
            "fps" => ToggleFps(context, subArgs),
            "refresh" => RefreshDisplay(context),
            "state" => ShowVideoState(context),
            "screen" => DumpScreen(context, subArgs),
            "capture" => CaptureFrame(context),
            "memory" => DumpVideoMemory(context, subArgs),
            "audio" => ShowAudioState(context),
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

        // Fire and forget - don't block the REPL
        _ = windowManager.ShowWindowAsync("Video", CreateWindowContext(context));
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

        _ = windowManager.ShowWindowAsync("Video", CreateWindowContext(context, videoContext => videoContext.Scale = scale));
        return CommandResult.Ok($"Setting video scale to {scale}×...");
    }

    private CommandResult SetColorMode(ICommandContext context, string[] args)
    {
        if (args.Length == 0)
        {
            return CommandResult.Error("Usage: video color <green|amber|white|color>");
        }

        DisplayColorMode? mode = args[0].ToLowerInvariant() switch
        {
            "green" => DisplayColorMode.Green,
            "amber" => DisplayColorMode.Amber,
            "white" => DisplayColorMode.White,
            "color" => DisplayColorMode.Color,
            _ => null,
        };

        if (mode is null)
        {
            return CommandResult.Error("Color mode must be: green, amber, white, or color.");
        }

        if (windowManager is null)
        {
            return CommandResult.Error("Video window not available in headless mode.");
        }

        _ = windowManager.ShowWindowAsync("Video", CreateWindowContext(context, videoContext => videoContext.ColorMode = mode));
        return CommandResult.Ok($"Setting color mode to {args[0]}...");
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

        _ = windowManager.ShowWindowAsync("Video", CreateWindowContext(context, videoContext =>
        {
            videoContext.ToggleFps = true;
            videoContext.ShowFps = showFps;
        }));
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

        _ = windowManager.ShowWindowAsync("Video", CreateWindowContext(context, videoContext => videoContext.ForceRefresh = true));
        return CommandResult.Ok("Display refreshed.");
    }

    private CommandResult ShowVideoState(ICommandContext context)
    {
        if (context is not IDebugContext dc || dc.Machine is null)
        {
            return CommandResult.Error("No machine attached.");
        }

        var video = dc.Machine.GetComponent<IVideoDevice>();
        if (video == null)
        {
            return CommandResult.Error("No video device.");
        }

        bool useJson = context.JsonOutput;

        if (useJson)
        {
            var state = new
            {
                mode = video.CurrentMode.ToString(),
                isText = video.IsTextMode,
                isMixed = video.IsMixedMode,
                isPage2 = video.IsPage2,
                isHiRes = video.IsHiRes,
                is80Col = video.Is80Column,
                isDoubleHiRes = video.IsDoubleHiRes,
                isVbl = video.IsVerticalBlanking,
            };
            string json = System.Text.Json.JsonSerializer.Serialize(state, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            context.Output.WriteLine(json);
            return CommandResult.Ok();
        }

        context.Output.WriteLine("Video State:");
        context.Output.WriteLine($"  Mode: {video.CurrentMode}");
        context.Output.WriteLine($"  Text:{video.IsTextMode} Mixed:{video.IsMixedMode} Page2:{video.IsPage2}");
        context.Output.WriteLine($"  HiRes:{video.IsHiRes} 80Col:{video.Is80Column} DoubleHiRes:{video.IsDoubleHiRes}");
        context.Output.WriteLine($"  VBL: {video.IsVerticalBlanking}");
        return CommandResult.Ok();
    }

    private CommandResult DumpScreen(ICommandContext context, string[] args)
    {
        if (context is not IDebugContext dc || dc.Machine is null)
        {
            return CommandResult.Error("No machine.");
        }

        var video = dc.Machine.GetComponent<IVideoDevice>();
        if (video == null)
        {
            return CommandResult.Error("No video device.");
        }

        bool useJson = context.JsonOutput;
        VideoMode mode = video.CurrentMode;

        // Get physical providers for correct main/aux (bypass banking for display view)
        var mainProv = dc.Machine.GetComponent<IMainMemoryProvider>();
        var auxDev = dc.Machine.GetComponent<IExtended80ColumnDevice>();

        if (mode == VideoMode.Text40 || mode == VideoMode.Text80 ||
            mode == VideoMode.LoRes || mode == VideoMode.LoResMixed ||
            mode == VideoMode.DoubleLoRes || mode == VideoMode.DoubleLoResMixed)
        {
            // Text / Lores use text page memory
            bool is80 = mode == VideoMode.Text80 || mode == VideoMode.DoubleLoRes || mode == VideoMode.DoubleLoResMixed;
            bool isPage2 = video.IsPage2 && !is80; // In 80-col, page2 is aux at page1 addr

            int cols = is80 ? 80 : 40;
            int rows = 24;
            var grid = new List<string>(rows);

            for (int row = 0; row < rows; row++)
            {
                int group = row / 8;
                int offset = row % 8;
                ushort rowBase = (ushort)(0x0400 + (offset * 128) + (group * 40));
                if (isPage2)
                {
                    rowBase += 0x0400; // page 2 for 40-col
                }

                var rowChars = new char[cols];
                for (int c = 0; c < cols; c++)
                {
                    ushort addr = (ushort)(rowBase + (c / (is80 ? 2 : 1)));
                    byte code;
                    if (is80)
                    {
                        bool even = (c % 2) == 0;
                        code = even && auxDev != null ? auxDev.ReadAuxRam(addr) : (mainProv?.ReadMainRam(addr) ?? 0);
                    }
                    else
                    {
                        code = mainProv?.ReadMainRam(addr) ?? 0;
                    }

                    // Improved decode for agent validation (better preserves letters/numbers vs spaces; full inverse/flash in real renderer)
                    byte c7 = (byte)(code & 0x7F);
                    char ch;
                    if (c7 >= 0x20 && c7 <= 0x7E)
                    {
                        ch = (char)c7;
                    }
                    else if (c7 < 0x20)
                    {
                        ch = (char)(c7 + 0x40); // map controls/inverse to visible
                    }
                    else
                    {
                        ch = '.';
                    }

                    rowChars[c] = ch;
                }

                grid.Add(new string(rowChars));
            }

            if (useJson)
            {
                var data = new { mode = mode.ToString(), rows = grid.Count, cols, grid, };
                context.Output.WriteLine(System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                return CommandResult.Ok();
            }
            else
            {
                context.Output.WriteLine($"Screen ({mode}, {cols}x{rows}):");
                foreach (var line in grid)
                {
                    context.Output.WriteLine("  " + line);
                }

                return CommandResult.Ok();
            }
        }

        // Graphics modes (LoRes/HiRes/Double variants handled above for lores; here for HiRes etc.)
        // Provide structured info + memory page using physical providers where possible.
        ushort pageBase = (mode == VideoMode.HiRes || mode == VideoMode.HiResMixed || mode == VideoMode.DoubleHiRes)
            ? (ushort)(video.IsPage2 ? 0x4000 : 0x2000)
            : (ushort)(video.IsPage2 ? 0x0800 : 0x0400);

        // Sample a few bytes from the primary video page using physical read (bypass banking)
        var sample = new List<string>();
        for (int i = 0; i < 8; i++)
        {
            ushort a = (ushort)(pageBase + i);
            byte b = mainProv?.ReadMainRam(a) ?? 0;
            sample.Add($"0x{b:X2}");
        }

        if (useJson)
        {
            var info = new
            {
                mode = mode.ToString(),
                isMixed = video.IsMixedMode,
                isPage2 = video.IsPage2,
                isHiRes = video.IsHiRes,
                isDoubleHiRes = video.IsDoubleHiRes,
                primaryVideoPage = $"0x{pageBase:X4}",
                sampleBytes = sample,
                note = "Use 'video capture' for full rendered pixels (headless OK) or 'video memory'/'mem' for raw."
            };
            context.Output.WriteLine(System.Text.Json.JsonSerializer.Serialize(info, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return CommandResult.Ok();
        }

        context.Output.WriteLine($"Current video mode: {mode} (graphics, page ~0x{pageBase:X4})");
        context.Output.WriteLine("  Use 'video capture' for frame buffer or 'video memory' + mem for raw data.");
        return CommandResult.Ok();
    }

    private CommandResult CaptureFrame(ICommandContext context)
    {
        if (context is not IDebugContext dc || dc.Machine is null || dc.Bus is null)
        {
            return CommandResult.Error("Machine + bus required for frame capture.");
        }

        var video = dc.Machine.GetComponent<IVideoDevice>();
        if (video == null)
        {
            return CommandResult.Error("No video device.");
        }

        VideoMode mode = video.CurrentMode;

        try
        {
            var renderer = new Pocket2VideoRenderer();
            int w = renderer.CanonicalWidth;
            int h = renderer.CanonicalHeight;
            uint[] pixels = new uint[w * h];

            // Use physical main and aux RAM for accurate video fetch (bypasses MMU soft switches like 80STORE/PAGE2/RAMRD).
            // This matches real Apple IIe hardware behavior and the snapshot technique in VideoWindow.
            // Critical for 80-column (even cols from aux, odd from main at PAGE1 addresses) and double modes.
            var mainProvider = dc.Machine.GetComponent<IMainMemoryProvider>();
            var auxDevice = dc.Machine.GetComponent<IExtended80ColumnDevice>();

            Func<ushort, byte> readMain = addr =>
            {
                if (mainProvider != null)
                {
                    return mainProvider.ReadMainRam(addr);
                }

                // Fallback to bus (may be affected by current banking)
                var access = new BusAccess(
                    Address: addr,
                    Value: 0,
                    WidthBits: 8,
                    Mode: BusAccessMode.Decomposed,
                    EmulationFlag: true,
                    Intent: AccessIntent.DebugRead,
                    SourceId: 0,
                    Cycle: 0,
                    Flags: AccessFlags.NoSideEffects);
                var res = dc.Bus.TryRead8(access);
                return res.Fault.IsFault ? (byte)0 : res.Value;
            };

            Func<ushort, byte>? readAux = auxDevice != null
                ? (Func<ushort, byte>)(addr => auxDevice.ReadAuxRam(addr))
                : null;

            var charProvider = dc.Machine.GetComponent<ICharacterRomProvider>();
            ReadOnlySpan<byte> charRom = charProvider?.IsCharacterRomLoaded == true
                ? charProvider.GetCharacterRomData().Span
                : default;

            renderer.RenderFrame(
                pixels.AsSpan(),
                mode,
                readMain,
                charRom,
                useAltCharSet: false,
                isPage2: video.IsPage2,
                flashState: false,
                noFlash1Enabled: false,
                noFlash2Enabled: false,
                colorMode: DisplayColorMode.Green,
                readAuxMemory: readAux);

            bool useJson = context.JsonOutput;

            if (useJson)
            {
                // Compact: dimensions + sample of pixels (BGRA uints). Full frame available via renderer in host.
                var sample = pixels.Take(256).Select(p => $"0x{p:X8}").ToArray();
                var cap = new
                {
                    mode = mode.ToString(),
                    width = w,
                    height = h,
                    samplePixels = sample,
                    sampleCount = sample.Length,
                    note = "Full 560x384 BGRA frame rendered headlessly via renderer (use capture for AI video diagnostics).",
                };
                string json = System.Text.Json.JsonSerializer.Serialize(cap, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                context.Output.WriteLine(json);
            }
            else
            {
                context.Output.WriteLine($"Captured {w}x{h} frame for mode {mode} (headless render OK).");
            }

            return CommandResult.Ok();
        }
        catch (Exception ex)
        {
            return CommandResult.Error($"Capture error: {ex.Message}");
        }
    }

    private CommandResult DumpVideoMemory(ICommandContext context, string[] args)
    {
        if (context is not IDebugContext dc || dc.Bus is null)
        {
            return CommandResult.Error("No bus attached.");
        }

        bool useJson = context.JsonOutput;

        var pages = new[]
        {
            new { name = "Text/LoRes P1", start = "0x0400", end = "0x07FF", note = "40/80 col text or lores" },
            new { name = "Text/LoRes P2", start = "0x0800", end = "0x0BFF", note = "page 2" },
            new { name = "HiRes P1", start = "0x2000", end = "0x3FFF", note = "280x192 or double" },
            new { name = "HiRes P2", start = "0x4000", end = "0x5FFF", note = "page 2" },
        };

        if (useJson)
        {
            context.Output.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { videoMemory = pages }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return CommandResult.Ok();
        }

        context.Output.WriteLine("Video memory pages (use 'mem $addr $len' or 'read' on these for raw exposure):");
        foreach (var p in pages)
        {
            context.Output.WriteLine($"  {p.name}: {p.start}-{p.end} ({p.note})");
        }

        return CommandResult.Ok();
    }

    private CommandResult ShowAudioState(ICommandContext context)
    {
        if (context is not IDebugContext dc || dc.Machine is null)
        {
            return CommandResult.Error("Machine required.");
        }

        bool useJson = context.JsonOutput;

        var speaker = dc.Machine.GetComponent<ISpeakerDevice>();
        var access = new BusAccess(
            Address: 0xC030,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: AccessIntent.DebugRead,
            SourceId: 0,
            Cycle: 0,
            Flags: AccessFlags.NoSideEffects);
        var res = dc.Bus?.TryRead8(access) ?? default;
        byte speakerPeek = res.Fault.IsFault ? (byte)0xFF : res.Value;

        int toggleCount = speaker?.PendingToggles.Count ?? 0;
        bool state = speaker?.State ?? false;
        var lastToggle = toggleCount > 0 ? speaker!.PendingToggles[^1] : ((ulong)0, false);

        if (useJson)
        {
            var info = new
            {
                speakerAddr = "$C030",
                lastPeek = $"0x{speakerPeek:X2}",
                deviceState = state,
                toggleCount,
                lastToggleCycle = lastToggle.Item1,
                lastToggleState = lastToggle.Item2,
                note = "Speaker toggles on access to $C030. PendingToggles holds history for waveform synthesis. Headless profiles may stub audio output."
            };
            context.Output.WriteLine(System.Text.Json.JsonSerializer.Serialize(info, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return CommandResult.Ok();
        }

        context.Output.WriteLine("Audio/Speaker State (headless):");
        context.Output.WriteLine("  Speaker toggle address: $C030");
        context.Output.WriteLine($"  Peek (debug, no click): 0x{speakerPeek:X2}");
        context.Output.WriteLine($"  Device state: {state}");
        context.Output.WriteLine($"  Toggles recorded: {toggleCount}");
        if (toggleCount > 0)
            context.Output.WriteLine($"  Last toggle: cycle {lastToggle.Item1}, state {lastToggle.Item2}");
        context.Output.WriteLine("  Note: use read/peek SPEAKER or the device for interaction; PendingToggles for synthesis.");
        return CommandResult.Ok();
    }

    private VideoWindowContext CreateWindowContext(ICommandContext context, Action<VideoWindowContext>? configure = null)
    {
        var videoContext = new VideoWindowContext
        {
            Machine = (context as IDebugContext)?.Machine,
        };

        configure?.Invoke(videoContext);
        return videoContext;
    }
}