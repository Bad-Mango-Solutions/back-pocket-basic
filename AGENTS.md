# AGENTS.md

Guidance for AI coding agents, LLM tools, and automated assistants working in this repository.

This file focuses on practical workflows, especially **interacting with `emudbg`** (the emulator debug console / REPL). See also `.github/copilot-instructions.md` for broader coding standards, testing requirements, logging rules (Serilog), StyleCop, dependency injection, etc.

## Project Context

- **Solution**: `BackPocketBasic.slnx` (modern .NET solution file). All projects target **net10.0**.
- **Primary Debug Tool**: `emudbg` — an interactive REPL for inspecting and controlling a live emulated machine. It is the main lever for debugging the emulator, devices, storage, video, BASIC runtime, etc.
- The long-term vision (documented in `PRD-emudbg-agent.md` and `emudbg-agent-plan.md`) is to make `emudbg` excellent for **both humans and AI agents**.

### Current Scope (stabilization phase)

**This project is currently a 65C02 Apple IIe emulator.** The immediate goal is a working, cycle-accurate Enhanced IIe — not a general multi-CPU platform yet.

| In scope now | Not in scope yet (future plans) |
|--------------|----------------------------------|
| **65C02** in Apple IIe **emulation mode** (always 8-bit A/X/Y) | 65816 / 65832 native mode, IIgs, etc. |
| Profile **`pocket2e-a2-enh`** (128 KB, Extended 80-Column, Language Card, Disk II) | Broad non–Apple IIe machine profiles |
| ProDOS / DOS 3.3 boot, Applesoft BASIC, slot devices, storage | Speculative 65832 OS / privilege model |
| `emudbg` agent workflows for boot validation | Full MCP production host (in progress) |

**Implications for agents and fixes:**

- Treat **`Cpu65C02`** as the production CPU: `E=1`, `CP=1`, 8-bit index/accumulator — never 16-bit index mode on Apple IIe.
- Prefer **Apple IIe hardware fidelity** over forward-looking 65816 abstractions when they conflict.
- Functional bar for stabilization: **ProDOS 2.0.3 → BASIC.SYSTEM → `]` prompt**, plus `PRINT` / `CATALOG` smoke tests on a copied disk image (never boot the golden master writeable).
- The codebase contains 65816/65832 scaffolding (register widths, opcode tables, etc.) for **future** expansion; do not expand that surface unless the user asks.

- **Core (long-term)**: Cycle-accurate 65C02 emulator for Apple II, plus a full Applesoft BASIC implementation; broader CPU support is planned after IIe stabilization.

## Building and Running

```powershell
# From repo root
dotnet build BackPocketBasic.slnx
# or just the debug host
dotnet build src/BadMango.Emulator.Debug/BadMango.Emulator.Debug.csproj
```

The output executable name is `emudbg`:

- Direct: `src\BadMango.Emulator.Debug\bin\Debug\net10.0\emudbg.exe`
- Dev-friendly: `dotnet run --project src/BadMango.Emulator.Debug`

**New CLI options (added for humans + agents):**

```
emudbg --help
emudbg -p pocket2e-lite
emudbg --profile simple-65c02 --exec "regs;step 5;regs;exit" --no-banner
emudbg -e "boot\nregs\ndasm" --no-banner
emudbg --file my-commands.emudbg
```

- `-p, --profile <name>` — override the machine profile at startup.
- `-e, --exec <cmds>` — run semicolon- or newline-separated commands then exit (non-interactive).
- `-f, --file <path>` — execute commands from a text file.
- `--no-banner` — suppress banner and prompts (ideal for agents/scripts).
- `--headless` — skip GUI entirely (no Avalonia, for headless/CI/agent hosts).
- `-h, --help` — usage.

On launch (when no CLI commands are given), `emudbg` creates a machine using the default (or --profile) profile and enters the REPL.

## Interacting with the emudbg REPL (Critical for Agents)

### Recommended Interaction Pattern (Piping / Batch Commands)

The REPL is line-oriented: it prints a banner + prompt, reads lines from stdin, dispatches via `CommandDispatcher`, and writes results to `Output` / `Error`.

**Preferred for agents:** Use the new CLI flags (`--profile`, `--exec`, `--file`, `--no-banner`).

**Fallback (still works):** send a batch of commands via stdin and capture everything:

**PowerShell (this environment):**
```powershell
$cmds = @(
    "profile list"
    "boot"
    "regs"
    "step 20"
    "regs"
    "dasm"
    "mem $300 64"
    "exit"
) -join "`n"

$cmds | & "src\BadMango.Emulator.Debug\bin\Debug\net10.0\emudbg.exe" 2>&1
```

**Unix-style shells:**
```bash
printf "boot\nregs\nstep 5\nregs\nexit\n" | dotnet run --project src/BadMango.Emulator.Debug -- 2>&1
```

**Rules:**
- Always terminate the command list with `exit`.
- Capture both stdout and stderr.
- Send related commands together (mutate → immediately inspect).
- Keep individual sequences focused (one logical debugging task per invocation).

### Core Commands for Debugging the Emulator

Use `help` and `help <command>` liberally inside sequences — they are self-documenting via `ICommandHelp`.

**Essential control & inspection:**
- `boot` (or alias `startup`) — Reset and start the machine (background execution begins). Supports holding modifier keys briefly.
- `reset`, `pause`, `resume`, `halt`, `stop`
- `step [count]` (alias `s`) — Single-step or step N instructions. Preferred for controlled progress.
- `run [limit]` or `run-until <cond>` (dedicated: `run-until $addr`, `run-until bp`, `run-until watch`, `run-until mem $addr $val`, `run-until --until-cycles=N`) — Advanced execution with structured JSON: stopReason, counts, until*/hit flags, finalRegisters snapshot, traceRecords (when buffered). Use limits for safety in agents. Much more efficient than repeated step + check.
- `regs` (aliases: `r`, `registers`) — Full CPU register dump (PC, SP, A/X/Y, flags, E/M/X, etc.).
- `pc [value]` — Get or set program counter.
- `mem <addr> [length]` — Classic hex dump.
- `dasm [addr] [count]` (or `--instructions=N`, `--range`) — Disassemble from PC or address.
- `peek`, `poke`, `read`, `write` — Lower-level access with/without side effects.

**Debugging features:**
- `bp ...` (breakpoints) — add/list/remove/clear
- `watch ...` (watchpoints)
- `trace on|off|configure|dump|...` — Instruction tracing (very useful for agents)
- `fault`, `buslog`

**Machine / profile:**
- `profile` — Show current
- `profile list`
- `profile load <name>` — Switch and rebuild machine (e.g. `pocket2e-lite`, `pocket2e`, `simple-65c02`)
- `profile default [name]`

Profiles live in `profiles/*.json`. The default is resolved in `DebugConsoleModule` (respects `.default-profile` file if present).

**Disk & storage (important for real Apple II software):**
- `disk list`, `disk info`, `disk create`, `disk insert <slot:drive> <path>`, `disk eject`, etc.

**Video / GUI:**
- `video open|close|scale N|color ...|fps|refresh`
- Various `*mon` commands open debug windows (`statmon`, `schedmon`, `trapmon`).

**Notes on GUI for agents:**
- Commands are accepted and the `IDebugWindowManager` (Avalonia) is optional.
- In headless or tool-driven sessions, windows often do not render visibly even if the command says "opened".
- Use `video` subcommands for configuration; fall back to `print`, `plot`, `hplot`, memory inspection, and `dasm` for visibility.

### Example Productive Agent Sequence

```powershell
$seq = "profile list`nboot`nregs`nstep 100`nregs`ndasm $fa00 32`ntrace dump`nexit"
$seq | & ...emudbg.exe...
```

Follow every significant state change or run with `regs`, `dasm`, or targeted `mem`.

### Current Limitations (Be Aware)

- All output is human-formatted text (boxes, tables, disassembly listings). Agents must parse it.
- No built-in JSON output mode yet (future enhancement opportunity).
- Long-running execution (`boot` / `run`) happens on the machine thread; the REPL stays responsive for other commands in the human design.
- The current host always starts the REPL (`DebugRepl.Run()`). There is no native "run a script and exit" or "execute one command" CLI mode today.
- Some output may go directly to console streams (Serilog is at Warning+ by default for console).

See the agent planning documents for the intended future direction (structured tools + generic `exec` + MCP stdio host).

## When Enhancing emudbg

### Key Code Locations
- `src/BadMango.Emulator.Debug/Program.cs` — Host setup (Autofac + `DebugConsoleModule` + `DebugUiModule`), starts `DebugRepl`.
- `src/BadMango.Emulator.Debug.Infrastructure/DebugRepl.cs` — REPL loop, `ProcessLine`, banner.
- `src/BadMango.Emulator.Debug.Infrastructure/Commands/CommandDispatcher.cs` — Registration and dispatch.
- `src/BadMango.Emulator.Debug.Infrastructure/Commands/` — Individual handlers (most implement `ICommandHelp`).
- `DebugContext.cs`, `MachineFactory.cs`, `DebugConsoleModule.cs` — Machine creation and wiring.
- UI windows and monitors live under `BadMango.Emulator.Debug.UI`.

### Improvement Ideas (Prioritize Making It Agent-Friendly)
- ~~Add CLI arguments (e.g. `--profile`, `--exec "cmd1;cmd2"`, `--script file.emudbg`, `--json`).~~ (CLI + --json completed)
- ~~Support a non-interactive / batch mode that doesn't require the full REPL loop.~~ (Lightweight ExecuteBatch / direct dispatch path for --exec/--file; auto-suppression of prompts/banner/"Goodbye!" for clean capture)
- ~~Optional structured output (JSON) for key commands (`regs --json`, `dasm --json`).~~ (Now available for switches, regions, pages, mem/read/peek/write/poke, disk*, devicemap, trace, profile, fault, etc. Use --json or per-command.)
- Better output capture abstraction (currently uses `ICommandContext.Output`).
- Separate lightweight agent host project (MCP stdio) — implementation started in BadMango.Emulator.Debug.Agent (stdio JSON-RPC server with generic `emudbg_exec` and structured examples; reuses dispatcher/context; settable writers for output capture).

### Functional Goal Workflow (MCP Agent Example)
The target: MCP agent boots Enhanced IIe (`pocket2e-a2-enh`) + DOS 3.3 on 6:1 (`library://disks/dos33-master.dsk`), confirms stages via trace (slot ROM boot loader @C600 → loads sector to 0800 and JMPs 0801 for boot sector loader → OS load), types/runs/validates a BASIC from `samples/` (e.g. `fibonacci.bas`) via keyboard + `video screen --json`.

Example MCP sequence (via `emudbg_exec` or dedicated tools):
1. `emudbg_disk_insert` {slot_drive:"6:1", path:"library://disks/dos33-master.dsk"}
2. `emudbg_disk_fsinfo` {slot_drive:"6:1"}
3. `emudbg_disk_ls` {slot_drive:"6:1"}   (or `emudbg_disk_cat` / `emudbg_disk_getfile` for file contents)
4. `emudbg_trace_on`
5. `emudbg_boot` (uses --immediate internally)
6. (client waits ~2-8s for bg run / scheduler to advance boot)
7. `emudbg_pause`
8. `emudbg_trace_dump` {count:20}  (C6xx for slot ROM loader at C600; 0801 for handoff after C600 code loads the sector to 0800 and executes at 0801)
9. `emudbg_get_screen` or `emudbg_get_video_state` (force --json)
10. `emudbg_keyboard_type` {text: full bas with \r between lines + "RUN\r"}
11. (client waits)
12. `emudbg_pause`; `emudbg_get_screen`  (validate grid contains program output / "DONE!")

For ProDOS verification (new functional requirement): use `emudbg_disk_fsinfo` + `emudbg_disk_ls` on prodos203-master.dsk, boot, run with trace/perf, confirm no crash and kernel loaded.

Use `emudbg_exec "disk insert 6:1 ...; boot --immediate; ...; video screen --json"` for complex batches.
Capture via the context writers ensures clean JSON/text for agent parsing.
- ~~Headless-friendly defaults and fake audio/video providers.~~ (Added `--headless` flag that skips DebugUiModule / Avalonia entirely; auto non-int detection already helps)
- Full structured JSON for trace / bp / watch (6.1 done — `trace status/dump`, `bp list`, `watch list` now produce clean objects/arrays).
- `run until $addr` (and `--until=$addr`) with structured result including `untilTarget` + `untilHit` (6.2 done).
- `perf` for introspection (6.9), `fsinfo`/`ls`/`cat` for disk files (6.8), structured JSON on monitors (6.5), enhanced audio state (6.7), full MCP tools (6.10).
- Cross-cutting: tests added for new commands, StyleCop clean (0 warnings on main pkgs), docs updated.

When you change commands, **always verify** by running representative piped sequences (boot + inspect + step + trace + exit) and confirm no regressions in output.

### General Rules
- Follow all rules in `.github/copilot-instructions.md` (especially Serilog injection, XML docs, no suppressed warnings, tests required).
- New commands should implement `ICommandHelp` for discoverability (`help <cmd>`).
- Prefer extending the existing dispatcher and context rather than bypassing them.
- Update this `AGENTS.md` and the planning docs when behavior or recommended workflows change.

## Related Documentation

- `emudbg-agent-plan.md` — Technical approach for exposing emudbg to LLM agents.
- `PRD-emudbg-agent.md` — Requirements for agent mode.
- `specs/video/Pocket2e Debug Video Window (Avalonia) — Specification.md`
- `profiles/` + `schemas/machine-profile.schema.json`
- Root `README.md`, `SETUP_GUIDE.md`, wiki pages.

## Humans vs. Agents

- **Humans**: Just run `emudbg` in a terminal. Type commands interactively. Use the video window for visual debugging.
- **Agents**: Use the batch/piping pattern above. Focus on reproducible, inspect-after-mutate sequences. Help improve the REPL so the same commands become even more powerful for tooling.

Start by exploring with `help` and small sequences. The existing command surface is rich — leverage it.

Welcome! Let's make `emudbg` the best possible debugger for this emulator, usable by both people and AI.
