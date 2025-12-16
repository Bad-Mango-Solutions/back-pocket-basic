// <copyright file="AppleSystem.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Emulation;

using Microsoft.Extensions.Logging;

/// <summary>
/// Apple II system emulation layer.
/// </summary>
public class AppleSystem : IAppleSystem
{
    private readonly ILogger<AppleSystem> _logger;
    private char? _keyboardBuffer;

    public ICpu Cpu { get; }

    public IMemory Memory { get; }

    public IAppleSpeaker Speaker { get; }

    // Important Apple II memory locations
    public static class MemoryLocations
    {
        // Zero page locations
        public const int WNDLFT = 0x20;   // Left edge of scroll window
        public const int WNDWDTH = 0x21;  // Width of scroll window
        public const int WNDTOP = 0x22;   // Top of scroll window
        public const int WNDBTM = 0x23;   // Bottom of scroll window
        public const int CH = 0x24;       // Cursor horizontal position
        public const int CV = 0x25;       // Cursor vertical position
        public const int GPTS = 0x2C;     // Graphics point coordinates
        public const int PROMPT = 0x33;   // Input prompt character

        // BASIC variables
        public const int TXTTAB = 0x67;   // Start of BASIC program
        public const int VARTAB = 0x69;   // Start of simple variables
        public const int ARYTAB = 0x6B;   // Start of array variables
        public const int STREND = 0x6D;   // End of arrays / start of free space
        public const int FRETOP = 0x6F;   // Top of string storage
        public const int FRESPC = 0x71;   // Temp pointer for strings
        public const int MEMSIZ = 0x73;   // Top of memory
        public const int CURLIN = 0x75;   // Current line number
        public const int OLDLIN = 0x77;   // Previous line number (for CONT)
        public const int OLDTXT = 0x79;   // Pointer to statement to execute
        public const int DATLIN = 0x7B;   // Line number of current DATA
        public const int DATPTR = 0x7D;   // Pointer to next DATA item
        public const int VARNAM = 0x82;   // Current variable name
        public const int VARPNT = 0x83;   // Pointer to current variable

        // ROM entry points
        public const int OUTCH = 0xFDED;  // Character output routine
        public const int RDKEY = 0xFD0C;  // Read keyboard
        public const int GETLN = 0xFD6A;  // Get line of input
        public const int CROUT = 0xFD8E;  // Output carriage return
        public const int BELL = 0xFBE4;   // Ring bell
        public const int HOME = 0xFC58;   // Clear screen and home cursor
        public const int CLREOL = 0xFC9C; // Clear to end of line
        public const int COUT = 0xFDF0;   // Character output (uses OUTCH)

        // Keyboard
        public const int KBD = 0xC000;    // Keyboard data
        public const int KBDSTRB = 0xC010;// Keyboard strobe

        // Soft switches
        public const int SPKR = 0xC030;   // Speaker toggle
        public const int TXTCLR = 0xC050; // Graphics mode
        public const int TXTSET = 0xC051; // Text mode
        public const int MIXCLR = 0xC052; // Full screen
        public const int MIXSET = 0xC053; // Mixed mode
        public const int PAGE1 = 0xC054;  // Display page 1
        public const int PAGE2 = 0xC055;  // Display page 2
        public const int LORES = 0xC056;  // Lo-res mode
        public const int HIRES = 0xC057;  // Hi-res mode
    }

    public AppleSystem(IMemory memory, ICpu cpu, IAppleSpeaker speaker, ILogger<AppleSystem> logger)
    {
        Memory = memory;
        Cpu = cpu;
        Speaker = speaker;
        _logger = logger;

        // Wire up the speaker to memory for $C030 access
        Memory.SetSpeaker(speaker);

        InitializeSystem();
    }

    private void InitializeSystem()
    {
        // Set up text window defaults (40x24)
        Memory.Write(MemoryLocations.WNDLFT, 0);
        Memory.Write(MemoryLocations.WNDWDTH, 40);
        Memory.Write(MemoryLocations.WNDTOP, 0);
        Memory.Write(MemoryLocations.WNDBTM, 24);
        Memory.Write(MemoryLocations.CH, 0);
        Memory.Write(MemoryLocations.CV, 0);
        Memory.Write(MemoryLocations.PROMPT, (byte)']'); // Applesoft prompt

        _logger.LogDebug("Apple II system initialized");
    }

    public byte Peek(int address)
    {
        ValidateAddress(address);
        return Memory.Read(address);
    }

    public void Poke(int address, byte value)
    {
        ValidateAddress(address);
        Memory.Write(address, value);
        _logger.LogTrace("POKE {Address}, {Value}", address, value);
    }

    public void Call(int address)
    {
        ValidateAddress(address);
        _logger.LogDebug("CALL {Address}", address);

        // Handle common ROM calls specially
        if (HandleRomCall(address))
        {
            return;
        }

        // Execute actual machine code
        try
        {
            // Set up a simple return by writing RTS at the return address
            // This allows the 6502 to execute and return properly
            Cpu.Execute(address);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing CALL at ${Address:X4}", address);
            throw;
        }
    }

    private bool HandleRomCall(int address)
    {
        // Handle common ROM calls that we can emulate
        switch (address)
        {
            case MemoryLocations.HOME:
                // Clear screen - handled by interpreter
                return true;

            case MemoryLocations.BELL:
                // Bell - use the Apple II speaker emulation
                Speaker.Beep();
                return true;

            case -868: // $FC5C - Clear screen
            case 64092: // Same address as unsigned
                return true;

            case -936: // $FC58 - HOME
            case 64344:
                return true;

            default:
                // For addresses in ROM range, log warning
                if (address >= 0xD000)
                {
                    _logger.LogWarning("CALL to ROM address ${Address:X4} - may not work correctly", address);
                }
                return false;
        }
    }

    public void SetKeyboardInput(char key)
    {
        _keyboardBuffer = key;
        // Set keyboard data with high bit set (key pressed)
        Memory.Write(MemoryLocations.KBD, (byte)(key | 0x80));
    }

    public char? GetKeyboardInput()
    {
        var key = _keyboardBuffer;
        _keyboardBuffer = null;
        // Clear the strobe
        Memory.Write(MemoryLocations.KBD, (byte)(Memory.Read(MemoryLocations.KBD) & 0x7F));
        return key;
    }

    public void Reset()
    {
        Cpu.Reset();
        InitializeSystem();
        _keyboardBuffer = null;
    }

    private void ValidateAddress(int address)
    {
        if (address < 0 || address >= Memory.Size)
        {
            throw new MemoryAccessException(
                $"Address {address} (${address:X4}) out of bounds. Valid range: 0-{Memory.Size - 1} ($0000-${Memory.Size - 1:X4})");
        }
    }
}