// <copyright file="AppleMemory.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

// Move all public members before private members to comply with SA1202

// ReSharper disable UnusedMember.Global
namespace BadMango.Basic.Emulation;

using Microsoft.Extensions.Logging;

/// <summary>
/// Emulated 64KB memory space for Apple II.
/// </summary>
public class AppleMemory : IMemory
{
    /// <summary>Standard Apple II memory size (64KB).</summary>
    public const int StandardMemorySize = 65536;

    // Apple II memory map constants
#pragma warning disable SA1600
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
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
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore SA1600

    // Private fields
    private readonly byte[] memory;
    private readonly ILogger<AppleMemory> logger;
    private IAppleSpeaker? speaker;

    /// <summary>Initializes a new instance of the <see cref="AppleMemory"/> class.</summary>
    /// <param name="logger">The logger to use for memory operations.</param>
    /// <param name="size">The size of the memory in bytes. Defaults to 64KB.</param>
    public AppleMemory(ILogger<AppleMemory> logger, int size = StandardMemorySize)
    {
        this.logger = logger;
        memory = new byte[size];
        InitializeMemory();
    }

    /// <summary>Gets the size of the memory in bytes.</summary>
    public int Size => memory.Length;

    /// <summary>Sets the speaker for the emulated Apple II memory.</summary>
    /// <param name="speaker">An implementation of <see cref="IAppleSpeaker"/> representing the speaker to be used for sound emulation.Pass <c>null</c> to disable the speaker.
    /// </param>
    public void SetSpeaker(IAppleSpeaker? speaker)
    {
        this.speaker = speaker;
    }

    /// <summary>Reads a byte from the specified memory address.</summary>
    /// <param name="address">The memory address to read from. Must be a valid address within the emulated memory space.</param>
    /// <returns>The byte value stored at the specified memory address.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the specified address is outside the valid memory range.</exception>
    public byte Read(int address)
    {
        ValidateAddress(address);

        // Handle soft switches and I/O
        if (address >= IOSpace && address < RomStart)
        {
            return HandleIORead(address);
        }

        return memory[address];
    }

    /// <summary>Writes a byte value to the specified memory address in the emulated memory space.</summary>
    /// <param name="address">The memory address where the value will be written. Must be within the valid memory range.</param>
    /// <param name="value">The byte value to write to the specified memory address.</param>
    /// <remarks>
    /// - Writing to addresses in the I/O space will trigger specific I/O handling logic.
    /// - Writing to addresses in the ROM area is not allowed and will be logged as a warning.
    /// - An exception of type <see cref="MemoryAccessException"/> will be thrown if the address is out of bounds.
    /// </remarks>
    /// <exception cref="MemoryAccessException">Thrown when the specified address is outside the valid memory range.</exception>
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
            logger.LogWarning("Attempted write to ROM address ${Address:X4}", address);
            return;
        }

        memory[address] = value;
    }

    /// <summary>Reads a 16-bit word (2 bytes) from the specified memory address.</summary>
    /// <param name="address">
    /// The starting address in memory from which to read the word. The word is read as two consecutive bytes:
    /// the low byte from the specified address and the high byte from the next address.
    /// </param>
    /// <returns>
    /// A 16-bit unsigned integer representing the word read from memory. The low byte is read from the specified
    /// address, and the high byte is read from the subsequent address.
    /// </returns>
    /// <remarks>
    /// This method combines two consecutive bytes from memory into a single 16-bit word, with the low byte
    /// occupying the least significant 8 bits and the high byte occupying the most significant 8 bits.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the specified address or the subsequent address is outside the valid memory range.</exception>
    public ushort ReadWord(int address)
    {
        byte low = Read(address);
        byte high = Read(address + 1);
        return (ushort)(low | (high << 8));
    }

    /// <summary>Writes a 16-bit word to the specified memory address in the emulated memory space.</summary>
    /// <param name="address">
    /// The starting address in memory where the word will be written.
    /// The lower byte of the word is written to this address, and the higher byte is written to the next address.
    /// </param>
    /// <param name="value">
    /// The 16-bit word to write to memory. The lower byte is extracted and written first, followed by the higher byte.
    /// </param>
    /// <remarks>
    /// This method assumes that the memory space is contiguous and that the address is valid within the bounds of the memory.
    /// </remarks>
    public void WriteWord(int address, ushort value)
    {
        Write(address, (byte)(value & 0xFF));
        Write(address + 1, (byte)(value >> 8));
    }

    /// <summary>Clears the entire emulated memory by setting all bytes to zero.</summary>
    /// <remarks>
    /// This method resets the memory to its initial state, effectively erasing all data.
    /// It is commonly used during initialization or to reset the memory for a new operation.
    /// </remarks>
    public void Clear()
    {
        Array.Clear(memory, 0, memory.Length);
    }

    /// <summary>Loads data into memory at the specified address.</summary>
    /// <param name="startAddress">The starting address in memory where the data will be loaded.</param>
    /// <param name="data">The byte array containing the data to load into memory.</param>
    public void LoadData(int startAddress, byte[] data)
    {
        if (startAddress + data.Length > memory.Length)
        {
            throw new MemoryAccessException($"Data too large to fit at address ${startAddress:X4}");
        }

        Array.Copy(data, 0, memory, startAddress, data.Length);
        logger.LogDebug("Loaded {Length} bytes at ${Address:X4}", data.Length, startAddress);
    }

    /// <summary>Gets a copy of a memory region.</summary>
    /// <param name="startAddress">The starting address of the memory region.</param>
    /// <param name="length">The length of the memory region to copy.</param>
    /// <returns>A byte array containing a copy of the specified memory region.</returns>
    public byte[] GetRegion(int startAddress, int length)
    {
        ValidateAddress(startAddress);
        ValidateAddress(startAddress + length - 1);

        var region = new byte[length];
        Array.Copy(memory, startAddress, region, 0, length);
        return region;
    }

    // Private methods
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
        memory[0xC000] = 0x00; // Keyboard data
        memory[0xC010] = 0x00; // Keyboard strobe

        logger.LogDebug("Memory initialized with {Size} bytes", memory.Length);
    }

    private void ValidateAddress(int address)
    {
        if (address < 0 || address >= memory.Length)
        {
            throw new MemoryAccessException($"Memory address ${address:X4} out of bounds (0-${memory.Length - 1:X4})");
        }
    }

    private byte HandleIORead(int address)
    {
        // Apple II soft switch handling
        return address switch
        {
            0xC000 => memory[0xC000],  // KBD - Keyboard data
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
            _ => memory[address],
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
                memory[address] = value;
                break;
        }
    }

    private byte ClearKeyboardStrobe()
    {
        memory[0xC000] &= 0x7F; // Clear high bit
        return memory[0xC010];
    }

    private byte ToggleSpeaker()
    {
        // Trigger speaker click for authentic Apple II sound
        speaker?.Click();
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
}