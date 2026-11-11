// <copyright file="DebugJsonContext.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Abstractions;

using System.Text.Json.Serialization;

/// <summary>
/// The source-generated JSON context for debug session contracts.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UseStringEnumConverter = false)]
[JsonSerializable(typeof(RegisterSnapshot))]
[JsonSerializable(typeof(MemoryBlock))]
[JsonSerializable(typeof(DisassemblyLine))]
[JsonSerializable(typeof(Breakpoint))]
[JsonSerializable(typeof(Watchpoint))]
[JsonSerializable(typeof(RunCondition))]
[JsonSerializable(typeof(RunBudget))]
[JsonSerializable(typeof(RunResult))]
[JsonSerializable(typeof(MachineInfo))]
[JsonSerializable(typeof(DebugEnvelope<RegisterSnapshot>))]
[JsonSerializable(typeof(DebugEnvelope<MemoryBlock>))]
[JsonSerializable(typeof(DebugEnvelope<DisassemblyLine>))]
[JsonSerializable(typeof(DebugEnvelope<Breakpoint>))]
[JsonSerializable(typeof(DebugEnvelope<Watchpoint>))]
[JsonSerializable(typeof(DebugEnvelope<RunCondition>))]
[JsonSerializable(typeof(DebugEnvelope<RunBudget>))]
[JsonSerializable(typeof(DebugEnvelope<RunResult>))]
[JsonSerializable(typeof(DebugEnvelope<MachineInfo>))]
public partial class DebugSessionJsonContext : JsonSerializerContext
{
}