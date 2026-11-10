// <copyright file="McpServer.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Agent;

using System.Text.Json;

using BadMango.Emulator.Core.Configuration;
using BadMango.Emulator.Core.Interfaces;
using BadMango.Emulator.Core.Interfaces.Cpu;
using BadMango.Emulator.Debug.Infrastructure;
using BadMango.Emulator.Debug.Infrastructure.Commands;
using BadMango.Emulator.Storage.Formats;

using Bus.Interfaces;

/// <summary>
/// Hosts the stdio-based MCP protocol layer for the emudbg agent.
/// </summary>
public sealed class McpServer
{
    private readonly ICommandDispatcher dispatcher;
    private readonly IDebugContext templateContext;
    private bool initialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="McpServer"/> class.
    /// </summary>
    /// <param name="dispatcher">The command dispatcher used to execute emudbg commands.</param>
    /// <param name="templateContext">The base debug context used to create per-call execution contexts.</param>
    public McpServer(ICommandDispatcher dispatcher, IDebugContext templateContext)
    {
        this.dispatcher = dispatcher;
        this.templateContext = templateContext;
    }

    /// <summary>
    /// Runs the MCP stdio server loop and handles incoming JSON-RPC requests.
    /// </summary>
    /// <returns>A task that completes when the input stream closes.</returns>
    public async Task RunAsync()
    {
        using var reader = new StreamReader(Console.OpenStandardInput());
        using var writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var request = JsonSerializer.Deserialize<JsonRpcRequest>(line);
                if (request == null)
                {
                    continue;
                }

                var response = await HandleRequestAsync(request);
                if (response != null)
                {
                    var json = JsonSerializer.Serialize(response);
                    await writer.WriteLineAsync(json);
                }
            }
            catch (Exception ex)
            {
                var errorResp = new { jsonrpc = "2.0", id = (object?)null, error = new { code = -32603, message = ex.Message } };
                await writer.WriteLineAsync(JsonSerializer.Serialize(errorResp));
            }
        }
    }

    private async Task<object?> HandleRequestAsync(JsonRpcRequest req)
    {
        var method = req.Method ?? string.Empty;
        switch (method.ToLowerInvariant())
        {
            case "initialize":
                initialized = true;
                return new
                {
                    jsonrpc = "2.0",
                    id = req.Id,
                    result = new
                    {
                        protocolVersion = "2024-11-05",
                        capabilities = new { tools = new object() },
                        serverInfo = new { name = "emudbg-agent", version = "0.1.0" },
                    },
                };

            case "tools/list":
                return new
                {
                    jsonrpc = "2.0",
                    id = req.Id,
                    result = new
                    {
                        tools = new object[]
                        {
                            new { name = "emudbg_exec", description = "Execute any emudbg command line (supports ; separated). Returns captured output.", inputSchema = new { type = "object", properties = new { command = new { type = "string" } } } },
                            new { name = "emudbg_regs", description = "Get CPU registers (structured).", inputSchema = new { type = "object", properties = new { } } },
                            new { name = "emudbg_disk_insert", description = "Insert/mount a disk image on slot:drive (e.g. 6:1).", inputSchema = new { type = "object", properties = new { slot_drive = new { type = "string" }, path = new { type = "string" } } } },
                            new { name = "emudbg_boot", description = "Boot or reset the machine (immediate, no modifier delay).", inputSchema = new { type = "object", properties = new { } } },
                            new { name = "emudbg_keyboard_type", description = "Type a string into the keyboard (use \\r for return).", inputSchema = new { type = "object", properties = new { text = new { type = "string" } } } },
                            new { name = "emudbg_get_screen", description = "Get current video screen content as text grid (for validation).", inputSchema = new { type = "object", properties = new { } } },
                            new { name = "emudbg_get_video_state", description = "Get video mode and flags.", inputSchema = new { type = "object", properties = new { } } },
                            new { name = "emudbg_get_text_page", description = "Detailed text page ($0400+) content + 80col/aux state. Helps diagnose blank/no-text screens (main vs routed).", inputSchema = new { type = "object", properties = new { } } },
                            new { name = "emudbg_get_clock_state", description = "Inspect Thunderclock/PocketWatch: $0200 ASCII buffer, $BF90/$BF03 time, after ProDOS clock calls.", inputSchema = new { type = "object", properties = new { } } },
                            new { name = "emudbg_check_for_prompt", description = "Scan current screen/video for BASIC prompt (']' or ProDOS banner text). Returns whether prompt visible.", inputSchema = new { type = "object", properties = new { } } },
                            new { name = "emudbg_get_switches", description = "Detailed soft switches (INTCXROM, 80STORE, PAGE2, RAMRD/WRT, ALTZP etc.) + text routing for layering diagnosis.", inputSchema = new { type = "object", properties = new { } } },
                            new { name = "emudbg_trace_on", description = "Enable tracing.", inputSchema = new { type = "object", properties = new { } } },
                            new { name = "emudbg_trace_dump", description = "Dump recent trace records (structured). Use filter on $C600-$C6FF then $0801 for Apple II DOS 3.3 boot stages (C600 loader → JMP 0801 after load to 0800).", inputSchema = new { type = "object", properties = new { count = new { type = "integer" } } } },
                            new { name = "emudbg_pause", description = "Pause machine execution.", inputSchema = new { type = "object", properties = new { } } },
                            new { name = "emudbg_resume", description = "Resume machine execution.", inputSchema = new { type = "object", properties = new { } } },
                            new { name = "emudbg_run_start", description = "Start background run (supports until, --until-cycles etc). Non-blocking. For Apple II DOS: run-until $0801 after C600 activity (loader JMPs to 0801).", inputSchema = new { type = "object", properties = new { command = new { type = "string" } } } },
                            new { name = "emudbg_run_status", description = "Poll status of active or last run (structured).", inputSchema = new { type = "object", properties = new { } } },
                            new { name = "emudbg_run_stop", description = "Request stop for active run.", inputSchema = new { type = "object", properties = new { } } },
                            new { name = "emudbg_trace_status", description = "Get structured trace status (buffer, count, filter).", inputSchema = new { type = "object", properties = new { } } },
                            new { name = "emudbg_buslog", description = "Show/clear bus fault log (structured with --json).", inputSchema = new { type = "object", properties = new { command = new { type = "string" } } } },
                            new { name = "emudbg_fault", description = "Show last bus fault (structured JSON).", inputSchema = new { type = "object", properties = new { } } },
                            new { name = "emudbg_disk_ls", description = "List files on mounted disk volume (e.g. slot:drive). Structured JSON friendly.", inputSchema = new { type = "object", properties = new { slot_drive = new { type = "string" } } } },
                            new { name = "emudbg_disk_readfile", description = "Read a file from the Apple II file system on the disk image.", inputSchema = new { type = "object", properties = new { slot_drive = new { type = "string" }, filename = new { type = "string" } } } },
                            new { name = "emudbg_disk_getfile", description = "Extract file from Apple II disk to host path. Args: slot_drive, filename, hostpath", inputSchema = new { type = "object", properties = new { slot_drive = new { type = "string" }, filename = new { type = "string" }, hostpath = new { type = "string" } } } },
                            new { name = "emudbg_disk_cat", description = "Print text contents of file from Apple II disk (forces text output).", inputSchema = new { type = "object", properties = new { slot_drive = new { type = "string" }, filename = new { type = "string" } } } },
                            new { name = "emudbg_disk_fsinfo", description = "Report detected FS (DOS/ProDOS/Pascal) and volume info for slot:drive.", inputSchema = new { type = "object", properties = new { slot_drive = new { type = "string" } } } },
                            new { name = "emudbg_perf", description = "Emulator performance/introspection stats (instructions, PC, faults).", inputSchema = new { type = "object", properties = new { } } },
                        },
                    },
                };

            case "tools/call":
                if (req.Params is JsonElement paramsEl && paramsEl.TryGetProperty("name", out var nameEl))
                {
                    var toolName = nameEl.GetString() ?? string.Empty;
                    var arguments = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    if (paramsEl.TryGetProperty("arguments", out var argsEl) && argsEl.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in argsEl.EnumerateObject())
                        {
                            object? val = prop.Value.ValueKind switch
                            {
                                JsonValueKind.String => prop.Value.GetString(),
                                JsonValueKind.Number => prop.Value.TryGetInt64(out var l) ? l : (object?)prop.Value.GetDouble(),
                                JsonValueKind.True => true,
                                JsonValueKind.False => false,
                                JsonValueKind.Null => null,
                                _ => prop.Value.GetRawText(),
                            };
                            arguments[prop.Name] = val;
                        }
                    }

                    var result = await CallToolAsync(toolName, arguments);
                    return new { jsonrpc = "2.0", id = req.Id, result };
                }

                return new { jsonrpc = "2.0", id = req.Id, error = new { code = -32602, message = "Invalid params" } };

            default:
                return new { jsonrpc = "2.0", id = req.Id, error = new { code = -32601, message = "Method not found" } };
        }
    }

    private async Task<object> CallToolAsync(string toolName, Dictionary<string, object?> args)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var captureContext = new CaptureContext(templateContext, output, error);

        if (toolName == "emudbg_exec")
        {
            var cmd = args.TryGetValue("command", out var c) ? c?.ToString() ?? string.Empty : string.Empty;
            var result = dispatcher.Dispatch(captureContext, cmd);
            var captured = output.ToString() + error.ToString();
            if (!string.IsNullOrEmpty(result.Message))
            {
                captured += (result.Success ? string.Empty : "Error: ") + result.Message;
            }

            return new { content = new[] { new { type = "text", text = captured.Trim() } } };
        }
        else if (toolName == "emudbg_regs")
        {
            var regsContext = new CaptureContext(templateContext, output, error) { ForceJson = true };
            var result = dispatcher.Dispatch(regsContext, "regs --json");
            var text = output.ToString();
            return new { content = new[] { new { type = "text", text = text.Trim() } } };
        }
        else if (toolName == "emudbg_disk_insert")
        {
            var slotDrive = args.TryGetValue("slot_drive", out var sd) ? sd?.ToString() ?? "6:1" : "6:1";
            var path = args.TryGetValue("path", out var p) ? p?.ToString() ?? "library://disks/dos33-master.dsk" : "library://disks/dos33-master.dsk";
            var cmd = $"disk insert {slotDrive} {path}";
            var result = dispatcher.Dispatch(captureContext, cmd);
            var captured = output.ToString() + error.ToString();
            if (!string.IsNullOrEmpty(result.Message))
            {
                captured += (result.Success ? string.Empty : "Error: ") + result.Message;
            }

            return new { content = new[] { new { type = "text", text = captured.Trim() } } };
        }
        else if (toolName == "emudbg_boot")
        {
            var result = dispatcher.Dispatch(captureContext, "boot --immediate");
            var captured = output.ToString() + error.ToString();
            if (!string.IsNullOrEmpty(result.Message))
            {
                captured += (result.Success ? string.Empty : "Error: ") + result.Message;
            }

            return new { content = new[] { new { type = "text", text = captured.Trim() } } };
        }
        else if (toolName == "emudbg_keyboard_type")
        {
            var text = args.TryGetValue("text", out var t) ? t?.ToString() ?? string.Empty : string.Empty;
            var escaped = text.Replace("\\", "\\\\").Replace("\"", "\\\"");
            var cmd = $"keyboard type \"{escaped}\"";
            var result = dispatcher.Dispatch(captureContext, cmd);
            var captured = output.ToString() + error.ToString();
            if (!string.IsNullOrEmpty(result.Message))
            {
                captured += (result.Success ? string.Empty : "Error: ") + result.Message;
            }

            return new { content = new[] { new { type = "text", text = captured.Trim() } } };
        }
        else if (toolName == "emudbg_get_screen")
        {
            var screenContext = new CaptureContext(templateContext, output, error) { ForceJson = true };
            var result = dispatcher.Dispatch(screenContext, "video screen --json");
            var text = output.ToString();
            return new { content = new[] { new { type = "text", text = text.Trim() } } };
        }
        else if (toolName == "emudbg_get_video_state")
        {
            var stateContext = new CaptureContext(templateContext, output, error) { ForceJson = true };
            var result = dispatcher.Dispatch(stateContext, "video state --json");
            var text = output.ToString();
            return new { content = new[] { new { type = "text", text = text.Trim() } } };
        }
        else if (toolName == "emudbg_trace_on")
        {
            var result = dispatcher.Dispatch(captureContext, "trace on");
            var captured = output.ToString() + error.ToString();
            if (!string.IsNullOrEmpty(result.Message))
            {
                captured += (result.Success ? string.Empty : "Error: ") + result.Message;
            }

            return new { content = new[] { new { type = "text", text = captured.Trim() } } };
        }
        else if (toolName == "emudbg_trace_dump")
        {
            var count = args.TryGetValue("count", out var c) ? c?.ToString() ?? "20" : "20";
            var traceContext = new CaptureContext(templateContext, output, error) { ForceJson = true };
            var result = dispatcher.Dispatch(traceContext, $"trace dump {count} --json");
            var text = output.ToString();
            return new { content = new[] { new { type = "text", text = text.Trim() } } };
        }
        else if (toolName == "emudbg_pause")
        {
            var result = dispatcher.Dispatch(captureContext, "pause");
            var captured = output.ToString() + error.ToString();
            if (!string.IsNullOrEmpty(result.Message))
            {
                captured += (result.Success ? string.Empty : "Error: ") + result.Message;
            }

            return new { content = new[] { new { type = "text", text = captured.Trim() } } };
        }
        else if (toolName == "emudbg_resume")
        {
            var result = dispatcher.Dispatch(captureContext, "resume");
            var captured = output.ToString() + error.ToString();
            if (!string.IsNullOrEmpty(result.Message))
            {
                captured += (result.Success ? string.Empty : "Error: ") + result.Message;
            }

            return new { content = new[] { new { type = "text", text = captured.Trim() } } };
        }
        else if (toolName == "emudbg_run_start")
        {
            var subcmd = args.TryGetValue("command", out var c) ? c?.ToString() ?? string.Empty : string.Empty;
            var cmd = string.IsNullOrWhiteSpace(subcmd) ? "run start" : $"run start {subcmd}";
            var result = dispatcher.Dispatch(captureContext, cmd);
            var captured = output.ToString() + error.ToString();
            if (!string.IsNullOrEmpty(result.Message))
            {
                captured += (result.Success ? string.Empty : "Error: ") + result.Message;
            }

            return new { content = new[] { new { type = "text", text = captured.Trim() } } };
        }
        else if (toolName == "emudbg_run_status")
        {
            var statusContext = new CaptureContext(templateContext, output, error) { ForceJson = true };
            var result = dispatcher.Dispatch(statusContext, "run status --json");
            var text = output.ToString();
            return new { content = new[] { new { type = "text", text = text.Trim() } } };
        }
        else if (toolName == "emudbg_run_stop")
        {
            var result = dispatcher.Dispatch(captureContext, "run stop");
            var captured = output.ToString() + error.ToString();
            if (!string.IsNullOrEmpty(result.Message))
            {
                captured += (result.Success ? string.Empty : "Error: ") + result.Message;
            }

            return new { content = new[] { new { type = "text", text = captured.Trim() } } };
        }
        else if (toolName == "emudbg_trace_status")
        {
            var statusContext = new CaptureContext(templateContext, output, error) { ForceJson = true };
            var result = dispatcher.Dispatch(statusContext, "trace status --json");
            var text = output.ToString();
            return new { content = new[] { new { type = "text", text = text.Trim() } } };
        }
        else if (toolName == "emudbg_buslog")
        {
            var sub = args.TryGetValue("command", out var c) ? c?.ToString() ?? "show" : "show";
            var cmd = $"buslog {sub} --json";
            var blContext = new CaptureContext(templateContext, output, error) { ForceJson = true };
            var result = dispatcher.Dispatch(blContext, cmd);
            var text = output.ToString();
            return new { content = new[] { new { type = "text", text = text.Trim() } } };
        }
        else if (toolName == "emudbg_fault")
        {
            var fContext = new CaptureContext(templateContext, output, error) { ForceJson = true };
            var result = dispatcher.Dispatch(fContext, "fault --json");
            var text = output.ToString();
            return new { content = new[] { new { type = "text", text = text.Trim() } } };
        }
        else if (toolName == "emudbg_disk_ls")
        {
            var sd = args.TryGetValue("slot_drive", out var s) ? s?.ToString() ?? "6:1" : "6:1";
            var cmd = $"disk-ls {sd} --json";
            var lsContext = new CaptureContext(templateContext, output, error) { ForceJson = true };
            var result = dispatcher.Dispatch(lsContext, cmd);
            var text = output.ToString();
            return new { content = new[] { new { type = "text", text = text.Trim() } } };
        }
        else if (toolName == "emudbg_disk_readfile")
        {
            var sd = args.TryGetValue("slot_drive", out var s) ? s?.ToString() ?? "6:1" : "6:1";
            var fn = args.TryGetValue("filename", out var f) ? f?.ToString() ?? string.Empty : string.Empty;
            var cmd = $"disk-readfile {sd} \"{fn}\" --json";
            var rfContext = new CaptureContext(templateContext, output, error) { ForceJson = true };
            var result = dispatcher.Dispatch(rfContext, cmd);
            var text = output.ToString();
            return new { content = new[] { new { type = "text", text = text.Trim() } } };
        }
        else if (toolName == "emudbg_disk_getfile")
        {
            var sd = args.TryGetValue("slot_drive", out var s) ? s?.ToString() ?? "6:1" : "6:1";
            var fn = args.TryGetValue("filename", out var f) ? f?.ToString() ?? string.Empty : string.Empty;
            var hp = args.TryGetValue("hostpath", out var h) ? h?.ToString() ?? "out.bin" : "out.bin";
            var cmd = $"disk-readfile {sd} \"{fn}\" --to {hp}";
            var gfContext = new CaptureContext(templateContext, output, error);
            var result = dispatcher.Dispatch(gfContext, cmd);
            var text = output.ToString();
            return new { content = new[] { new { type = "text", text = text.Trim() } } };
        }
        else if (toolName == "emudbg_disk_cat")
        {
            var sd = args.TryGetValue("slot_drive", out var s) ? s?.ToString() ?? "6:1" : "6:1";
            var fn = args.TryGetValue("filename", out var f) ? f?.ToString() ?? string.Empty : string.Empty;
            var cmd = $"disk-readfile {sd} \"{fn}\"";
            var catContext = new CaptureContext(templateContext, output, error);
            var result = dispatcher.Dispatch(catContext, cmd);
            var text = output.ToString();
            return new { content = new[] { new { type = "text", text = text.Trim() } } };
        }
        else if (toolName == "emudbg_disk_fsinfo")
        {
            var sd = args.TryGetValue("slot_drive", out var s) ? s?.ToString() ?? "6:1" : "6:1";
            var cmd = $"disk-fsinfo {sd} --json";
            var fiContext = new CaptureContext(templateContext, output, error) { ForceJson = true };
            var result = dispatcher.Dispatch(fiContext, cmd);
            var text = output.ToString();
            return new { content = new[] { new { type = "text", text = text.Trim() } } };
        }
        else if (toolName == "emudbg_perf")
        {
            var pContext = new CaptureContext(templateContext, output, error) { ForceJson = true };
            var result = dispatcher.Dispatch(pContext, "perf --json");
            var text = output.ToString();
            return new { content = new[] { new { type = "text", text = text.Trim() } } };
        }
        else if (toolName == "emudbg_get_text_page")
        {
            var mainCtx = new CaptureContext(templateContext, output, error) { ForceJson = true };
            dispatcher.Dispatch(mainCtx, "peek --json $0400 128");
            output.WriteLine("\n--- Video State ---");
            var vsCtx = new CaptureContext(templateContext, output, error) { ForceJson = true };
            dispatcher.Dispatch(vsCtx, "video state --json");
            var captured = output.ToString() + error.ToString();
            return new { content = new[] { new { type = "text", text = captured.Trim() } } };
        }
        else if (toolName == "emudbg_get_clock_state")
        {
            var clkCtx = new CaptureContext(templateContext, output, error) { ForceJson = true };
            dispatcher.Dispatch(clkCtx, "peek --json $0200 16");
            output.WriteLine("\n--- ProDOS Global Time ---");
            var bfCtx = new CaptureContext(templateContext, output, error) { ForceJson = true };
            dispatcher.Dispatch(bfCtx, "peek --json $BF90 4; peek --json $BF03");
            var captured = output.ToString() + error.ToString();
            return new { content = new[] { new { type = "text", text = captured.Trim() } } };
        }
        else if (toolName == "emudbg_check_for_prompt")
        {
            var screenCtx = new CaptureContext(templateContext, output, error) { ForceJson = true };
            dispatcher.Dispatch(screenCtx, "video screen --json");
            var text = output.ToString() + error.ToString();
            var hasPrompt = text.Contains("]") || text.Contains("BASIC") || text.Contains("PRODOS") || text.Contains("Apple");
            var peekCtx = new CaptureContext(templateContext, output, error) { ForceJson = true };
            dispatcher.Dispatch(peekCtx, "peek --json $0400 40");
            var peekText = output.ToString();
            var promptDetected = hasPrompt || !peekText.Contains("\"00\"") || peekText.Contains("DD");
            return new { content = new[] { new { type = "text", text = (promptDetected ? "PROMPT_DETECTED\n" : "NO_PROMPT\n") + text.Trim() } } };
        }
        else if (toolName == "emudbg_get_switches")
        {
            var swCtx = new CaptureContext(templateContext, output, error) { ForceJson = true };
            dispatcher.Dispatch(swCtx, "switches --json");
            output.WriteLine("\n--- Video/80col state ---");
            var vCtx = new CaptureContext(templateContext, output, error) { ForceJson = true };
            dispatcher.Dispatch(vCtx, "video state --json");
            var captured = output.ToString() + error.ToString();
            return new { content = new[] { new { type = "text", text = captured.Trim() } } };
        }

        return new { content = new[] { new { type = "text", text = $"Unknown tool {toolName}" } } };
    }

    private sealed record JsonRpcRequest(
        [property: System.Text.Json.Serialization.JsonPropertyName("id")] object? Id,
        [property: System.Text.Json.Serialization.JsonPropertyName("method")] string? Method,
        [property: System.Text.Json.Serialization.JsonPropertyName("params")] object? Params
    );
}

/// <summary>
/// Wraps an existing debug context and redirects output and error streams for captured tool execution.
/// </summary>
internal sealed class CaptureContext : ICommandContext, IDebugContext
{
    private readonly IDebugContext inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureContext"/> class.
    /// </summary>
    /// <param name="inner">The underlying debug context to delegate to.</param>
    /// <param name="output">The writer used for normal tool output.</param>
    /// <param name="error">The writer used for diagnostic or error output.</param>
    public CaptureContext(IDebugContext inner, TextWriter output, TextWriter error)
    {
        this.inner = inner;
        Output = output;
        Error = error;
        Input = inner.Input;
    }

    public bool ForceJson { get; set; }

    /// <inheritdoc/>
    public ICommandDispatcher Dispatcher => inner.Dispatcher;

    /// <inheritdoc/>
    public TextWriter Output { get; set; }

    /// <inheritdoc/>
    public TextReader? Input { get; set; }

    /// <inheritdoc/>
    public TextWriter Error { get; set; }

    /// <inheritdoc/>
    public bool JsonOutput => ForceJson || inner.JsonOutput;

    /// <inheritdoc/>
    public ICpu? Cpu => inner.Cpu;

    /// <inheritdoc/>
    public IMemoryBus? Bus => inner.Bus;

    /// <inheritdoc/>
    public IDisassembler? Disassembler => inner.Disassembler;

    /// <inheritdoc/>
    public MachineInfo? MachineInfo => inner.MachineInfo;

    /// <inheritdoc/>
    public TracingDebugListener? TracingListener => inner.TracingListener;

    /// <inheritdoc/>
    public BreakpointManager Breakpoints => inner.Breakpoints;

    /// <inheritdoc/>
    public WatchpointManager Watchpoints => inner.Watchpoints;

    /// <inheritdoc/>
    public bool IsSystemAttached => inner.IsSystemAttached;

    /// <inheritdoc/>
    public IMachine? Machine => inner.Machine;

    /// <inheritdoc/>
    public bool IsBusAttached => inner.IsBusAttached;

    /// <inheritdoc/>
    public IDebugPathResolver? PathResolver => inner.PathResolver;

    /// <inheritdoc/>
    public DiskImageFactory? DiskImageFactory => inner.DiskImageFactory;

    /// <inheritdoc/>
    public MountedDiskRegistry MountedDisks => inner.MountedDisks;

    /// <inheritdoc/>
    public CompositeDebugStepListener? StepListener => inner.StepListener;

    /// <inheritdoc/>
    public bool IsRunActive => inner.IsRunActive;

    /// <inheritdoc/>
    public string? ActiveRunDescription => inner.ActiveRunDescription;

    /// <inheritdoc/>
    public BadMango.Emulator.Debug.Infrastructure.Commands.ExecutionCommandBase.ExecutionResult? LastRunResult => inner.LastRunResult;

    /// <inheritdoc/>
    public void RequestRunStop() => inner.RequestRunStop();
}