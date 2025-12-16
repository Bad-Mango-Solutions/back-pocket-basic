// <copyright file="ProDosEmulator.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Emulation;

using Microsoft.Extensions.Logging;

/// <summary>
/// Provides an emulation layer for ProDOS, the operating system used on Apple II computers.
/// </summary>
/// <remarks>
/// The <see cref="ProDosEmulator"/> class simulates the behavior of ProDOS, including its
/// Machine Language Interface (MLI) calls, system locations, and device management.
/// It integrates with the <see cref="IMemory"/> interface to manage memory operations
/// and uses logging to provide debug information about the emulation process.
/// </remarks>
public class ProDosEmulator
{
    // ProDOS system locations
    // ReSharper disable InconsistentNaming
#pragma warning disable SA1600
#pragma warning disable SA1310
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public const int MLI = 0xBF00;        // Machine Language Interface entry
    public const int DEVADR = 0xBF10;     // Device driver addresses
    public const int DEVNUM = 0xBF30;     // Device number
    public const int DATETIME = 0xBF90;   // Date/time storage
    public const int MACHID = 0xBF98;     // Machine identification
    public const int PREFIX = 0xBF00;     // Current prefix

    // ProDOS MLI calls
    public const byte CREATE = 0xC0;
    public const byte DESTROY = 0xC1;
    public const byte RENAME = 0xC2;
    public const byte SET_FILE_INFO = 0xC3;
    public const byte GET_FILE_INFO = 0xC4;
    public const byte ONLINE = 0xC5;
    public const byte SET_PREFIX = 0xC6;
    public const byte GET_PREFIX = 0xC7;
    public const byte OPEN = 0xC8;
    public const byte NEWLINE = 0xC9;
    public const byte READ = 0xCA;
    public const byte WRITE = 0xCB;
    public const byte CLOSE = 0xCC;
    public const byte FLUSH = 0xCD;
    public const byte SET_MARK = 0xCE;
    public const byte GET_MARK = 0xCF;
    public const byte SET_EOF = 0xD0;
    public const byte GET_EOF = 0xD1;
    public const byte SET_BUF = 0xD2;
    public const byte GET_BUF = 0xD3;

    // ReSharper restore InconsistentNaming
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore SA1310
#pragma warning restore SA1600

    private readonly IMemory memory;
    private readonly ILogger<ProDosEmulator> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProDosEmulator"/> class.
    /// </summary>
    /// <param name="memory">
    /// An implementation of the <see cref="IMemory"/> interface, used to manage
    /// and interact with the emulated memory space of the Apple II system.
    /// </param>
    /// <param name="logger">
    /// An instance of <see cref="ILogger{ProDosEmulator}"/> used for logging
    /// debug information and tracking the emulation process.
    /// </param>
    /// <remarks>
    /// This constructor sets up the ProDOS emulation environment by initializing
    /// system locations, setting the machine ID, and configuring the date and time.
    /// </remarks>
    public ProDosEmulator(IMemory memory, ILogger<ProDosEmulator> logger)
    {
        this.memory = memory;
        this.logger = logger;

        InitializeProDos();
    }

    /// <summary>
    /// Handles a ProDOS Machine Language Interface (MLI) call by executing the specified command
    /// with the provided parameter list.
    /// </summary>
    /// <param name="command">
    /// The MLI command to execute. This is a byte value representing the specific ProDOS operation.
    /// </param>
    /// <param name="parameterList">
    /// The memory address of the parameter list for the MLI call. This list contains the arguments
    /// required for the specified command.
    /// </param>
    /// <returns>
    /// A byte value indicating the result of the MLI call. Typically, a value of <c>0x00</c> indicates
    /// success, while other values represent error codes.
    /// </returns>
    /// <remarks>
    /// This method logs the details of the MLI call for debugging purposes and delegates the execution
    /// of specific commands to corresponding private handler methods.
    /// </remarks>
    public byte HandleMliCall(byte command, int parameterList)
    {
        logger.LogDebug("ProDOS MLI call: ${Command:X2} params at ${Params:X4}", command, parameterList);

        return command switch
        {
            GET_FILE_INFO => HandleGetFileInfo(parameterList),
            ONLINE => HandleOnline(parameterList),
            GET_PREFIX => HandleGetPrefix(parameterList),
            _ => 0x01, // Bad MLI call number
        };
    }

    private void InitializeProDos()
    {
        // Set machine ID (Apple IIe)
        memory.Write(MACHID, 0xB3); // Apple IIe, 128K, 80-col

        // Set date/time to current
        var now = DateTime.Now;
        int dosDate = ((now.Year - 1900) << 9) | (now.Month << 5) | now.Day;
        int dosTime = (now.Hour << 8) | now.Minute;

        memory.WriteWord(DATETIME, (ushort)dosDate);
        memory.WriteWord(DATETIME + 2, (ushort)dosTime);

        logger.LogDebug("ProDOS emulation initialized");
    }

    private byte HandleGetFileInfo(int parameterList)
    {
        // Return "file not found" for simplicity
        return 0x46;
    }

    private byte HandleOnline(int parameterList)
    {
        // Return volume name
        int bufferAddr = memory.ReadWord(parameterList + 1);

        // Write a simple volume name
        byte[] volumeName = { 0x06, (byte)'V', (byte)'O', (byte)'L', (byte)'U', (byte)'M', (byte)'E' };
        for (int i = 0; i < volumeName.Length; i++)
        {
            memory.Write(bufferAddr + i, volumeName[i]);
        }

        return 0; // Success
    }

    private byte HandleGetPrefix(int parameterList)
    {
        int bufferAddr = memory.ReadWord(parameterList + 1);

        // Write a default prefix
        byte[] prefix = { 0x07, (byte)'/', (byte)'V', (byte)'O', (byte)'L', (byte)'U', (byte)'M', (byte)'E' };
        for (int i = 0; i < prefix.Length; i++)
        {
            memory.Write(bufferAddr + i, prefix[i]);
        }

        return 0; // Success
    }
}