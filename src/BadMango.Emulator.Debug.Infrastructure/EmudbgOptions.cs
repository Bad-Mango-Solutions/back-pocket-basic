// <copyright file="EmudbgOptions.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure;

/// <summary>
/// Parsed startup options for emudbg, typically derived from command-line arguments.
/// </summary>
/// <param name="Profile">Optional profile name to load at startup (overrides .default-profile).</param>
/// <param name="ExecCommands">Optional commands to execute (semicolon or newline separated). Implies non-interactive mode.</param>
/// <param name="ScriptFile">Optional path to a file containing commands to execute (one per line).</param>
/// <param name="NoBanner">When true, suppress the startup banner (useful for scripting/agents).</param>
/// <param name="ShowHelp">When true, display usage information and exit.</param>
/// <param name="JsonOutput">When true, enable structured JSON output where supported.</param>
/// <param name="Headless">When true, initialize Avalonia in headless mode for diagnostics without a display.</param>
/// <param name="AgentMode">When true, implies headless + json + non-interactive agent-friendly behavior.</param>
public sealed record EmudbgOptions(
    string? Profile = null,
    string? ExecCommands = null,
    string? ScriptFile = null,
    bool NoBanner = false,
    bool ShowHelp = false,
    bool JsonOutput = false,
    bool Headless = false,
    bool AgentMode = false)
{
    /// <summary>
    /// Parses command-line arguments into <see cref="EmudbgOptions"/>.
    /// Supports --profile/-p, --exec/-e, --file/-f, --no-banner, --help/-h.
    /// Also supports --key=value forms.
    /// </summary>
    /// <param name="args">The command-line arguments passed to the application.</param>
    /// <returns>A populated <see cref="EmudbgOptions"/> instance.</returns>
    public static EmudbgOptions Parse(string[] args)
    {
        string? profile = null;
        string? exec = null;
        string? file = null;
        bool noBanner = false;
        bool showHelp = false;
        bool jsonOutput = false;
        bool headless = false;
        bool agentMode = false;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            // --key=value support
            if (arg.StartsWith("--profile="))
            {
                profile = arg.Split('=', 2)[1];
                continue;
            }

            if (arg.StartsWith("-p="))
            {
                profile = arg.Split('=', 2)[1];
                continue;
            }

            if (arg.StartsWith("--exec="))
            {
                exec = arg.Split('=', 2)[1];
                continue;
            }

            if (arg.StartsWith("-e="))
            {
                exec = arg.Split('=', 2)[1];
                continue;
            }

            if (arg.StartsWith("--file="))
            {
                file = arg.Split('=', 2)[1];
                continue;
            }

            if (arg.StartsWith("-f="))
            {
                file = arg.Split('=', 2)[1];
                continue;
            }

            if (arg.StartsWith("--headless"))
            {
                headless = true;
                continue;
            }

            if (arg.StartsWith("--agent"))
            {
                agentMode = true;
                headless = true;
                jsonOutput = true;
                continue;
            }

            switch (arg)
            {
                case "--profile":
                case "-p":
                    if (i + 1 < args.Length)
                    {
                        profile = args[++i];
                    }

                    break;

                case "--exec":
                case "-e":
                    if (i + 1 < args.Length)
                    {
                        exec = args[++i];
                    }

                    break;

                case "--file":
                case "-f":
                    if (i + 1 < args.Length)
                    {
                        file = args[++i];
                    }

                    break;

                case "--no-banner":
                    noBanner = true;
                    break;

                case "--json":
                case "-j":
                    jsonOutput = true;
                    break;

                case "--headless":
                    headless = true;
                    break;

                case "--agent":
                case "--agent-mode":
                    agentMode = true;
                    headless = true;
                    jsonOutput = true;
                    break;

                case "--help":
                case "-h":
                case "/?":
                case "-?":
                    showHelp = true;
                    break;

                default:
                    // Ignore unknown for forward compatibility (e.g. future flags or passed to host)
                    break;
            }
        }

        return new EmudbgOptions(profile, exec, file, noBanner, showHelp, jsonOutput, headless, agentMode);
    }

    /// <summary>
    /// Creates an appropriate <see cref="TextReader"/> based on the options
    /// (exec commands, script file, or default to console).
    /// </summary>
    /// <returns>A reader for command input.</returns>
    public TextReader CreateInputReader()
    {
        if (!string.IsNullOrWhiteSpace(ExecCommands))
        {
            string commands = ExecCommands
                .Replace("\\n", "\n", StringComparison.Ordinal)
                .Replace(';', '\n');

            if (!commands.Contains("exit", StringComparison.OrdinalIgnoreCase))
            {
                commands += "\nexit";
            }

            return new StringReader(commands);
        }

        if (!string.IsNullOrWhiteSpace(ScriptFile))
        {
            // In production the caller ensures the file exists; return reader for valid cases.
            if (File.Exists(ScriptFile))
            {
                return new StreamReader(ScriptFile);
            }
        }

        return Console.In;
    }
}