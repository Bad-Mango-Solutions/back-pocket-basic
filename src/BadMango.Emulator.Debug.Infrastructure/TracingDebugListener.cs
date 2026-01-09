// <copyright file="TracingDebugListener.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure;

using System.Text;

using Core.Cpu;
using Core.Debugger;
using Core.Interfaces.Cpu;
using Core.Interfaces.Debugging;

/// <summary>
/// A debug step listener that traces instruction execution to output streams.
/// </summary>
/// <remarks>
/// <para>
/// This listener captures instruction execution details and formats them for
/// display in the debug console or logging to a file. It can be attached to
/// the CPU via <see cref="ICpu.AttachDebugger"/> to receive step notifications.
/// </para>
/// <para>
/// The trace output includes the PC, opcode bytes, instruction mnemonic, operands,
/// and register state after execution. Example output:
/// <code>
/// $1000: A9 01    LDA #$01       A=01 X=00 Y=00 SP=FF P=34 [..I.....] Cycles=2
/// $1002: 85 10    STA $10        A=01 X=00 Y=00 SP=FF P=34 [..I.....] Cycles=5
/// </code>
/// </para>
/// </remarks>
public sealed class TracingDebugListener : IDebugStepListener
{
    private readonly Lock syncLock = new();
    private readonly List<TraceRecord> recordBuffer = [];
    private TextWriter? consoleOutput;
    private StreamWriter? fileOutput;
    private bool isEnabled;
    private int maxBufferedRecords = 10000;

    /// <summary>
    /// Gets or sets a value indicating whether tracing is enabled.
    /// </summary>
    /// <remarks>
    /// When disabled, the listener ignores all step events. This allows the listener
    /// to remain attached to the CPU while selectively enabling/disabling tracing.
    /// </remarks>
    public bool IsEnabled
    {
        get => isEnabled;
        set => isEnabled = value;
    }

    /// <summary>
    /// Gets or sets the maximum number of records to buffer when not outputting immediately.
    /// </summary>
    /// <remarks>
    /// When <see cref="BufferOutput"/> is true, records are stored in memory up to this limit.
    /// Older records are discarded when the limit is reached.
    /// </remarks>
    public int MaxBufferedRecords
    {
        get => maxBufferedRecords;
        set => maxBufferedRecords = Math.Max(100, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to buffer output instead of writing immediately.
    /// </summary>
    /// <remarks>
    /// When true, trace records are buffered in memory and can be retrieved via <see cref="GetBufferedRecords"/>.
    /// When false, records are written immediately to the configured outputs.
    /// Buffering is useful for post-mortem analysis when the output volume would be too high for real-time display.
    /// </remarks>
    public bool BufferOutput { get; set; }

    /// <summary>
    /// Gets the number of instructions traced since tracing was enabled.
    /// </summary>
    public long InstructionCount { get; private set; }

    /// <summary>
    /// Formats a trace record as a single line of text.
    /// </summary>
    /// <param name="record">The trace record to format.</param>
    /// <returns>A formatted string representing the trace record.</returns>
    public static string FormatTraceRecord(TraceRecord record)
    {
        var sb = new StringBuilder(128);

        // Address
        sb.Append($"${record.PC:X4}: ");

        // Opcode and operand bytes (up to 6 bytes for future 65816/65832 support, padded)
        sb.Append($"{record.Opcode:X2}");
        for (int i = 0; i < record.OperandSize; i++)
        {
            sb.Append($" {record.Operands[i]:X2}");
        }

        // Pad to fixed width for alignment (max 6 bytes = 17 chars + 2 padding = 19 total)
        // Format: "XX XX XX XX XX XX  " (6 bytes * 3 chars each - 1 for no trailing space + 2 padding)
        int bytesWidth = 2 + (record.OperandSize * 3); // "XX" + " XX" per operand
        sb.Append(new string(' ', Math.Max(0, 19 - bytesWidth)));

        // Instruction mnemonic
        sb.Append(record.Instruction.ToString().PadRight(4));

        // Operand formatting
        string operand = FormatOperand(record);
        sb.Append(operand.PadRight(12));

        // Register state (cast P to byte for hex formatting) - formatted as assembly comment
        sb.Append($"; A={record.A:X2} X={record.X:X2} Y={record.Y:X2} SP={record.SP:X2} P={(byte)record.P:X2}");

        // Flags as characters
        sb.Append($" [{FormatFlags(record.P)}]");

        // Cycle count
        sb.Append($" Cyc={record.Cycles}");

        // Halt state if applicable
        if (record.Halted)
        {
            sb.Append($" HALT:{record.HaltReason}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Configures the listener to output to the console.
    /// </summary>
    /// <param name="output">The text writer for console output.</param>
    public void SetConsoleOutput(TextWriter? output)
    {
        lock (syncLock)
        {
            consoleOutput = output;
        }
    }

    /// <summary>
    /// Configures the listener to output to a file.
    /// </summary>
    /// <param name="filePath">The path to the trace file, or null to disable file output.</param>
    /// <exception cref="IOException">Thrown when the file cannot be created or opened.</exception>
    public void SetFileOutput(string? filePath)
    {
        lock (syncLock)
        {
            fileOutput?.Dispose();
            fileOutput = null;

            if (!string.IsNullOrEmpty(filePath))
            {
                fileOutput = new(filePath, append: false, Encoding.UTF8)
                {
                    AutoFlush = true,
                };
            }
        }
    }

    /// <summary>
    /// Clears all buffered trace records.
    /// </summary>
    public void ClearBuffer()
    {
        lock (syncLock)
        {
            recordBuffer.Clear();
        }
    }

    /// <summary>
    /// Resets the instruction count to zero.
    /// </summary>
    public void ResetInstructionCount()
    {
        InstructionCount = 0;
    }

    /// <summary>
    /// Gets a copy of the buffered trace records.
    /// </summary>
    /// <returns>A list of trace records captured since the last clear.</returns>
    public IReadOnlyList<TraceRecord> GetBufferedRecords()
    {
        lock (syncLock)
        {
            return [.. recordBuffer];
        }
    }

    /// <summary>
    /// Gets the most recent trace records (up to the specified count).
    /// </summary>
    /// <param name="count">The maximum number of records to retrieve.</param>
    /// <returns>The most recent trace records.</returns>
    public IReadOnlyList<TraceRecord> GetRecentRecords(int count)
    {
        lock (syncLock)
        {
            if (recordBuffer.Count <= count)
            {
                return [.. recordBuffer];
            }

            return recordBuffer.Skip(recordBuffer.Count - count).ToList();
        }
    }

    /// <summary>
    /// Flushes any buffered output to the file.
    /// </summary>
    public void Flush()
    {
        lock (syncLock)
        {
            fileOutput?.Flush();
        }
    }

    /// <summary>
    /// Closes the file output stream.
    /// </summary>
    public void CloseFileOutput()
    {
        lock (syncLock)
        {
            fileOutput?.Dispose();
            fileOutput = null;
        }
    }

    /// <inheritdoc/>
    public void OnBeforeStep(in DebugStepEventArgs eventData)
    {
        // We capture state in OnAfterStep for complete information
    }

    /// <inheritdoc/>
    public void OnAfterStep(in DebugStepEventArgs eventData)
    {
        if (!isEnabled)
        {
            return;
        }

        InstructionCount++;

        var record = new TraceRecord
        {
            PC = eventData.PC,
            Opcode = eventData.Opcode,
            Instruction = eventData.Instruction,
            AddressingMode = eventData.AddressingMode,
            Operands = eventData.Operands,
            OperandSize = eventData.OperandSize,
            EffectiveAddress = eventData.EffectiveAddress,
            A = eventData.Registers.A.GetByte(),
            X = eventData.Registers.X.GetByte(),
            Y = eventData.Registers.Y.GetByte(),
            SP = eventData.Registers.SP.GetByte(),
            P = eventData.Registers.P,
            Cycles = eventData.Cycles,
            InstructionCycles = eventData.InstructionCycles,
            Halted = eventData.Halted,
            HaltReason = eventData.HaltReason,
        };

        lock (syncLock)
        {
            if (BufferOutput)
            {
                // Remove the oldest records if we're at capacity
                while (recordBuffer.Count >= maxBufferedRecords)
                {
                    recordBuffer.RemoveAt(0);
                }

                recordBuffer.Add(record);
            }
            else
            {
                string formattedLine = FormatTraceRecord(record);
                consoleOutput?.WriteLine(formattedLine);
                fileOutput?.WriteLine(formattedLine);
            }
        }
    }

    /// <summary>
    /// Formats the processor status flags as a character string.
    /// </summary>
    /// <param name="p">The processor status flags.</param>
    /// <returns>A string like "NV..DIZC" with set flags shown and clear flags as dots.</returns>
    private static string FormatFlags(ProcessorStatusFlags p)
    {
        return string.Create(8, p, (chars, flags) =>
        {
            chars[0] = (flags & ProcessorStatusFlags.N) != 0 ? 'N' : '.';
            chars[1] = (flags & ProcessorStatusFlags.V) != 0 ? 'V' : '.';
            chars[2] = '.'; // Reserved/M flag
            chars[3] = '.'; // B/X flag
            chars[4] = (flags & ProcessorStatusFlags.D) != 0 ? 'D' : '.';
            chars[5] = (flags & ProcessorStatusFlags.I) != 0 ? 'I' : '.';
            chars[6] = (flags & ProcessorStatusFlags.Z) != 0 ? 'Z' : '.';
            chars[7] = (flags & ProcessorStatusFlags.C) != 0 ? 'C' : '.';
        });
    }

    /// <summary>
    /// Formats the operand based on the addressing mode.
    /// </summary>
    /// <param name="record">The trace record.</param>
    /// <returns>A formatted operand string.</returns>
    private static string FormatOperand(TraceRecord record)
    {
        ushort GetOperandValue()
        {
            return record.OperandSize switch
            {
                0 => 0,
                1 => record.Operands[0],
                _ => (ushort)(record.Operands[0] | (record.Operands[1] << 8)),
            };
        }

        return record.AddressingMode switch
        {
            CpuAddressingModes.Implied => string.Empty,
            CpuAddressingModes.Accumulator => "A",
            CpuAddressingModes.Immediate => $"#${GetOperandValue():X2}",
            CpuAddressingModes.ZeroPage => $"${GetOperandValue():X2}",
            CpuAddressingModes.ZeroPageX => $"${GetOperandValue():X2},X",
            CpuAddressingModes.ZeroPageY => $"${GetOperandValue():X2},Y",
            CpuAddressingModes.Absolute => $"${GetOperandValue():X4}",
            CpuAddressingModes.AbsoluteX => $"${GetOperandValue():X4},X",
            CpuAddressingModes.AbsoluteY => $"${GetOperandValue():X4},Y",
            CpuAddressingModes.Indirect => $"(${GetOperandValue():X4})",
            CpuAddressingModes.IndirectX => $"(${GetOperandValue():X2},X)",
            CpuAddressingModes.IndirectY => $"(${GetOperandValue():X2}),Y",
            CpuAddressingModes.Relative => FormatRelativeBranch(record),
            _ => string.Empty,
        };
    }

    /// <summary>
    /// Formats a relative branch target address.
    /// </summary>
    /// <param name="record">The trace record.</param>
    /// <returns>The branch target address.</returns>
    private static string FormatRelativeBranch(TraceRecord record)
    {
        if (record.OperandSize == 0)
        {
            return "$????";
        }

        var offset = (sbyte)record.Operands[0];
        var targetAddress = (uint)(record.PC + 2 + offset);
        return $"${targetAddress:X4}";
    }
}