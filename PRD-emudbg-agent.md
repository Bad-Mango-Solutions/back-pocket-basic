# Product Requirements Document: `emudbg` Agent Mode

## Overview

Create an LLM-operable agent mode for `emudbg` so a model-aware client can drive the live emulator as a diagnostic tool. The agent must be able to boot a profile, inspect memory/registers, set breakpoints and watchpoints, control tracing, step execution, and interact with disk and runtime state without relying on a human at the console.

This mode should reuse the existing debug infrastructure and command surface wherever possible, while exposing a machine-friendly transport suitable for local tooling integrations such as MCP clients.

## Problem Statement

The current `emudbg` application is an interactive REPL intended for human operators. That works for manual debugging, but it is inefficient for an LLM agent that needs structured results, repeatable control, and the ability to coordinate long-running diagnostics in real time against a live machine.

## Goals

- Allow an LLM agent to control `emudbg` as a local tool.
- Support real-time diagnostics against a live emulator session.
- Expose structured actions for common debugging workflows.
- Preserve access to the existing REPL command surface.
- Keep the agent host compatible with non-Windows environments used by tooling.
- Reuse the existing debug console infrastructure and runtime state.

## Non-Goals

- Do not redesign the emulator or debugger architecture from scratch.
- Do not expose the agent over a network in v1.
- Do not replace the existing human REPL.
- Do not require the LLM to parse only human-formatted console output.

## User Stories

1. As an LLM agent, I can start a debug session and boot a profile.
2. As an LLM agent, I can inspect CPU registers, memory, and disassembly in structured form.
3. As an LLM agent, I can add/remove/list breakpoints and watchpoints.
4. As an LLM agent, I can configure tracing for a narrow PC range and dump captured records.
5. As an LLM agent, I can run the machine, poll progress, and stop execution when needed.
6. As an LLM agent, I can fall back to a generic command execution path for any existing console command.
7. As a user on Linux/Ubuntu, I can run the agent without Windows-only video or audio dependencies.

## Functional Requirements

### Session Control
- The agent must initialize a single debug session backed by the existing emulator runtime.
- The agent must support boot, reset, run, stop, step, and status operations.
- Long-running execution must be controllable through start/poll/stop semantics.

### Structured Diagnostics
- The agent must expose registers, memory reads, memory writes, and disassembly as structured outputs.
- Breakpoints, watchpoints, and trace settings must be queryable and editable.
- Disk operations and mount state must remain accessible.

### Generic Command Access
- The agent must provide a raw command execution tool that dispatches existing `emudbg` commands.
- The agent must capture stdout and stderr separately from protocol output.

### Transport
- The agent should expose tools through a local machine-readable protocol suitable for agent clients.
- Tool output must be structured JSON rather than human-only text wherever practical.
- The transport must support cancellation, bounded output, and polling for long-running actions.

## Platform Requirements

### Headless Video
- The agent must run with headless Avalonia video support when a display is not available.
- The runtime must not require a Windows desktop session for diagnostics.

### Audio
- The agent must support a fake or stubbed speaker controller on platforms where the Windows-only speaker implementation is unavailable.
- Audio output is not a primary diagnostic requirement and may be no-op in agent mode.

### Host Compatibility
- The agent must run on Linux/Ubuntu hosts used for automated and local tool execution.
- The design should avoid direct dependencies on OS-specific UI or audio services unless a fallback is available.

## Proposed Tool Surface

### Session / Execution
- `boot`
- `reset`
- `step`
- `run_start`
- `run_status`
- `run_stop`
- `exec` for arbitrary command dispatch

### CPU / Memory
- `regs`
- `mem_read`
- `mem_write`
- `disasm`

### Debugging
- `break_add`
- `break_remove`
- `break_list`
- `break_clear`
- `watch_add`
- `watch_remove`
- `watch_list`
- `watch_clear`
- `trace_configure`
- `trace_dump`

### Disk / Mounting
- `disk_insert`
- `disk_eject`
- `disk_read_sector`
- `mount_list`

## Success Criteria

- An LLM agent can complete a boot-and-diagnose workflow without human intervention.
- Common debug operations return structured responses that are easy for tools to consume.
- The agent can run on Linux/Ubuntu without Windows-only video or speaker dependencies.
- Existing REPL behavior continues to work for human users.
- The implementation is covered by unit and integration tests.

## Risks

- Concurrency between a live run loop and diagnostic queries may need careful serialization.
- Output capture must not interfere with protocol framing.
- Some existing components may write directly to the console and require redirection in agent mode.
- Headless rendering and speaker stubbing may require additional host-specific wiring.

## Deliverables

- A new agent host executable for `emudbg`.
- A structured tool layer for common diagnostics.
- A generic command execution escape hatch.
- Documentation for client integration and usage.
- Tests covering the agent runtime and tool behaviors.
