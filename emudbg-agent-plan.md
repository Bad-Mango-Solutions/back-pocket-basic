# Expose `emudbg` as an LLM-Operable Tool

## Understanding
The goal is to let an LLM agent drive the existing `emudbg` debug console as a local tool against a live emulator session. The agent should be able to boot, inspect state, set breakpoints/watchpoints, trace execution, and stop or step the machine in real time.

## Assumptions
- `emudbg` currently runs as a human REPL on top of `CommandDispatcher`, `IDebugContext`, and registered `ICommandHandler` instances.
- The existing debug context is sufficient for non-interactive use, with only a new transport layer needed.
- MCP over stdio JSON-RPC is the preferred first transport for local agent clients.
- Long-running operations such as `run` need start/poll/stop semantics.
- Structured tool responses are preferable to parsing human-formatted console output.
- The agent host will reuse the existing debug infrastructure instead of duplicating it.
- The agent must also work in environments without Windows-only video or audio, so headless Avalonia and a fake speaker controller are required for host compatibility.

## Approach
Build a thin agent host that reuses the existing debug infrastructure and exposes it through a machine-readable protocol. The host will wrap `IDebugContext` and `ICommandDispatcher`, capture command output per invocation, and provide a session layer that can serialize access to the emulator while a live run is active.

The tool layer should expose structured operations for the most common diagnostics, including registers, memory, disassembly, run control, breakpoints, watchpoints, tracing, and disk operations. A generic `exec` tool should remain available so the agent can invoke any existing `emudbg` command without waiting for a dedicated wrapper. The transport layer should support a local stdio-based protocol such as MCP so clients can issue tool calls and receive JSON responses.

## Key Files
- `src/BadMango.Emulator.Debug/Program.cs` - current host entrypoint and hosting pattern.
- `src/BadMango.Emulator.Debug.Infrastructure/DebugConsoleModule.cs` - DI registrations reused by the agent host.
- `src/BadMango.Emulator.Debug.Infrastructure/DebugRepl.cs` - current REPL flow that the agent will replace.
- `src/BadMango.Emulator.Debug.Infrastructure/Commands/CommandDispatcher.cs` - command dispatch path used by the generic `exec` tool.
- `src/BadMango.Emulator.Debug.Infrastructure/Commands/DebugContext.cs` - runtime state and emulator handles.
- `src/BadMango.Emulator.Debug.Infrastructure/Commands/ICommandHelp.cs` - source of command descriptions and examples.
- `src/BadMango.Emulator.Debug.Infrastructure/TracingDebugListener.cs` - trace control and buffered trace output.
- `src/BadMango.Emulator.Debug.Infrastructure/BreakpointManager.cs` - breakpoint behavior.
- `src/BadMango.Emulator.Debug.Infrastructure/WatchpointManager.cs` - watchpoint behavior.

## Risks & Open Questions
- Concurrency while the emulator is running may require a single-flight command lock.
- Some output may still be written directly to console streams and must be captured or redirected.
- The exact transport shape may evolve, but MCP is the intended first target.
- Headless video and fake audio must be available on Linux/Ubuntu hosts.
- Tool schemas for structured operations must be stable enough for agent integration.

## Steps
1. Confirm `DebugContext.Output` and `Error` can be swapped per call, or add a scoped writer mechanism.
2. Create a new `BadMango.Emulator.Debug.Agent` project targeting net10.0.
3. Add an `AgentSession` for captured output and serialized command execution.
4. Define a reusable tool contract and registry.
5. Implement a generic `exec` tool for raw command dispatch.
6. Implement structured tools for boot, reset, registers, memory, disassembly, and execution control.
7. Implement structured breakpoint, watchpoint, and trace tools.
8. Implement disk and mount tools.
9. Build the MCP stdio server transport.
10. Add the agent entrypoint and wire the host.
11. Add unit and integration tests.
12. Document agent mode usage and client integration.
13. Validate the build and test suite.
14. Use the new agent tooling to diagnose the live boot path.
