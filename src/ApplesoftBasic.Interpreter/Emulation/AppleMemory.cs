// <copyright file="AppleMemory.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Emulation;

using Microsoft.Extensions.Logging;

/// <summary>
/// Emulated 64KB memory space for Apple II.
/// </summary>
public class AppleMemory : IMemory
{
    private readonly byte[] _memory;
    private readonly ILogger<AppleMemory> _logger;
    private IAppleSpeaker? _speaker;

    // Standard Apple II memory size (64KB)
    public const int StandardMemorySize = 65536;

    // Apple II memory map constants
    public const int ZeroPage = 0x0000;
    public const int Stack = 0x0100;
    public const int TextPage1 = 0x0400;
    public const int TextPage2 = 0x0800;
    public const int LoResPage1 = 0x0400;
    public const int LoResPage2 = 0x0800;
    public const int HiResPage1 = 0x2000;
    public const int HiResPage2 = 0x4000;
    public const int BasicProgram = 0x0800;
    public const int BasicVariables = 0x9600;
    public const int DOS = 0x9D00;
    public const int IOSpace = 0xC000;
    public const int RomStart = 0xD000;

    // Speaker soft switch address
    public const int SpeakerToggle = 0xC030;

    public int Size => _memory.Length;

    public AppleMemory(ILogger<AppleMemory> logger, int size = StandardMemorySize)
    {
        _logger = logger;
        _memory = new byte[size];
        InitializeMemory();
    }

    public void SetSpeaker(IAppleSpeaker? speaker)
    {
        _speaker = speaker;
    }

    private void InitializeMemory()
    {
        // Initialize with typical Apple II boot state
        Clear();

        // Set up some standard memory locations
        WriteWord(0x03F0, 0xC600); // Reset vector points to boot ROM
        WriteWord(0x03F2, 0xFA62); // Applesoft cold start
        WriteWord(0x03F4, 0xFA62); // Applesoft warm start

        // BASIC pointers
        WriteWord(0x67, BasicProgram); // TXTTAB - Start of BASIC program
        WriteWord(0x69, BasicProgram); // VARTAB - Start of variables
        WriteWord(0x6B, BasicProgram); // ARYTAB - Start of arrays
        WriteWord(0x6D, BasicProgram); // STREND - End of arrays
        WriteWord(0x6F, BasicVariables); // FRETOP - Top of string space
        WriteWord(0x73, 0x9600); // MEMSIZ - Top of memory
        WriteWord(0x4C, 0x0801); // CURLIN - Current line number storage

        // Keyboard/input locations
        _memory[0xC000] = 0x00; // Keyboard data
        _memory[0xC010] = 0x00; // Keyboard strobe

        _logger.LogDebug("Memory initialized with {Size} bytes", _memory.Length);
    }

    public byte Read(int address)
    {
        ValidateAddress(address);

        // Handle soft switches and I/O
        if (address >= IOSpace && address < RomStart)
        {
            return HandleIORead(address);
        }

        return _memory[address];
    }

    public void Write(int address, byte value)
    {
        ValidateAddress(address);

        // Handle soft switches and I/O
        if (address >= IOSpace && address < RomStart)
        {
            HandleIOWrite(address, value);
            return;
        }

        // Prevent writing to ROM area
        if (address >= RomStart)
        {
            _logger.LogWarning("Attempted write to ROM address ${Address:X4}", address);
            return;
        }

        _memory[address] = value;
    }

    public ushort ReadWord(int address)
    {
        byte low = Read(address);
        byte high = Read(address + 1);
        return (ushort)(low | (high << 8));
    }

    public void WriteWord(int address, ushort value)
    {
        Write(address, (byte)(value & 0xFF));
        Write(address + 1, (byte)(value >> 8));
    }

    public void Clear()
    {
        Array.Clear(_memory, 0, _memory.Length);
    }

    private void ValidateAddress(int address)
    {
        if (address < 0 || address >= _memory.Length)
        {
            throw new MemoryAccessException($"Memory address ${address:X4} out of bounds (0-${_memory.Length - 1:X4})");
        }
    }

    private byte HandleIORead(int address)
    {
        // Apple II soft switch handling
        return address switch
        {
            0xC000 => _memory[0xC000],  // KBD - Keyboard data
            0xC010 => ClearKeyboardStrobe(),
            SpeakerToggle => ToggleSpeaker(),
            0xC050 => SetGraphicsMode(),
            0xC051 => SetTextMode(),
            0xC052 => SetFullScreen(),
            0xC053 => SetMixedMode(),
            0xC054 => SetPage1(),
            0xC055 => SetPage2(),
            0xC056 => SetLoRes(),
            0xC057 => SetHiRes(),
            0xC061 => ReadPushButton0(),
            0xC062 => ReadPushButton1(),
            0xC064 => ReadPaddle0(),
            0xC065 => ReadPaddle1(),
            _ => _memory[address]
        };
    }

    private void HandleIOWrite(int address, byte value)
    {
        // Most soft switches are read-activated, but some accept writes
        switch (address)
        {
            case 0xC010:
                ClearKeyboardStrobe();
                break;
            case SpeakerToggle:
                ToggleSpeaker();
                break;
            default:
                _memory[address] = value;
                break;
        }
    }

    private byte ClearKeyboardStrobe()
    {
        _memory[0xC000] &= 0x7F; // Clear high bit
        return _memory[0xC010];
    }

    private byte ToggleSpeaker()
    {
        // Trigger speaker click for authentic Apple II sound
        _speaker?.Click();
        return 0;
    }

    private byte SetGraphicsMode() => 0;

    private byte SetTextMode() => 0;

    private byte SetFullScreen() => 0;

    private byte SetMixedMode() => 0;

    private byte SetPage1() => 0;

    private byte SetPage2() => 0;

    private byte SetLoRes() => 0;

    private byte SetHiRes() => 0;

    private byte ReadPushButton0() => 0;

    private byte ReadPushButton1() => 0;

    private byte ReadPaddle0() => 128; // Center position

    private byte ReadPaddle1() => 128; // Center position

    /// <summary>
    /// Loads data into memory at the specified address.
    /// </summary>
    public void LoadData(int startAddress, byte[] data)
    {
        if (startAddress + data.Length > _memory.Length)
        {
            throw new MemoryAccessException($"Data too large to fit at address ${startAddress:X4}");
        }

        Array.Copy(data, 0, _memory, startAddress, data.Length);
        _logger.LogDebug("Loaded {Length} bytes at ${Address:X4}", data.Length, startAddress);
    }

    /// <summary>
    /// Gets a copy of a memory region.
    /// </summary>
    /// <returns></returns>
    public byte[] GetRegion(int startAddress, int length)
    {
        ValidateAddress(startAddress);
        ValidateAddress(startAddress + length - 1);

        var region = new byte[length];
        Array.Copy(_memory, startAddress, region, 0, length);
        return region;
    }
}