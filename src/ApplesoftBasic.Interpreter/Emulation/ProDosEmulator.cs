// <copyright file="ProDosEmulator.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Emulation;

using Microsoft.Extensions.Logging;

/// <summary>
/// ProDOS emulation layer.
/// </summary>
public class ProDosEmulator
{
    private readonly IMemory _memory;
    private readonly ILogger<ProDosEmulator> _logger;

    // ProDOS system locations
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

    public ProDosEmulator(IMemory memory, ILogger<ProDosEmulator> logger)
    {
        _memory = memory;
        _logger = logger;

        InitializeProDos();
    }

    private void InitializeProDos()
    {
        // Set machine ID (Apple IIe)
        _memory.Write(MACHID, 0xB3); // Apple IIe, 128K, 80-col

        // Set date/time to current
        var now = DateTime.Now;
        int dosDate = ((now.Year - 1900) << 9) | (now.Month << 5) | now.Day;
        int dosTime = (now.Hour << 8) | now.Minute;

        _memory.WriteWord(DATETIME, (ushort)dosDate);
        _memory.WriteWord(DATETIME + 2, (ushort)dosTime);

        _logger.LogDebug("ProDOS emulation initialized");
    }

    /// <summary>
    /// Handles a ProDOS MLI call.
    /// </summary>
    /// <returns></returns>
    public byte HandleMliCall(byte command, int parameterList)
    {
        _logger.LogDebug("ProDOS MLI call: ${Command:X2} params at ${Params:X4}", command, parameterList);

        return command switch
        {
            GET_FILE_INFO => HandleGetFileInfo(parameterList),
            ONLINE => HandleOnline(parameterList),
            GET_PREFIX => HandleGetPrefix(parameterList),
            _ => 0x01 // Bad MLI call number
        };
    }

    private byte HandleGetFileInfo(int parameterList)
    {
        // Return "file not found" for simplicity
        return 0x46;
    }

    private byte HandleOnline(int parameterList)
    {
        // Return volume name
        int bufferAddr = _memory.ReadWord(parameterList + 1);

        // Write a simple volume name
        byte[] volumeName = { 0x06, (byte)'V', (byte)'O', (byte)'L', (byte)'U', (byte)'M', (byte)'E' };
        for (int i = 0; i < volumeName.Length; i++)
        {
            _memory.Write(bufferAddr + i, volumeName[i]);
        }

        return 0; // Success
    }

    private byte HandleGetPrefix(int parameterList)
    {
        int bufferAddr = _memory.ReadWord(parameterList + 1);

        // Write a default prefix
        byte[] prefix = { 0x07, (byte)'/', (byte)'V', (byte)'O', (byte)'L', (byte)'U', (byte)'M', (byte)'E' };
        for (int i = 0; i < prefix.Length; i++)
        {
            _memory.Write(bufferAddr + i, prefix[i]);
        }

        return 0; // Success
    }
}