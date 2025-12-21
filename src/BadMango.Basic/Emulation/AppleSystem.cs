// <copyright file="AppleSystem.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.Emulation;

using Microsoft.Extensions.Logging;

/// <summary>
/// Apple II system emulation layer.
/// </summary>
public class AppleSystem : IAppleSystem
{
    private readonly ILogger<AppleSystem> logger;
    private char? keyboardBuffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppleSystem"/> class.
    /// </summary>
    /// <param name="memory">The emulated memory space to be used by the system.</param>
    /// <param name="cpu">The CPU emulator to be used by the system.</param>
    /// <param name="speaker">The Apple II speaker emulator to be used by the system.</param>
    /// <param name="logger">The logger instance for logging system events and diagnostics.</param>
    /// <remarks>
    /// This constructor sets up the core components of the Apple II emulation system,
    /// including memory, CPU, and speaker. It also wires up the speaker to the memory
    /// for handling specific memory access and initializes the system state.
    /// </remarks>
    public AppleSystem(IMemory memory, ICpu cpu, IAppleSpeaker speaker, ILogger<AppleSystem> logger)
    {
        Memory = memory;
        Cpu = cpu;
        Speaker = speaker;
        this.logger = logger;

        // Wire up the speaker to memory for $C030 access
        Memory.SetSpeaker(speaker);

        InitializeSystem();
    }

    /// <summary>
    /// Gets the CPU emulator instance associated with the Apple II system.
    /// </summary>
    /// <remarks>
    /// This property provides access to the CPU emulation layer, allowing
    /// execution of machine code, stepping through instructions, and managing
    /// CPU state such as registers and memory interactions.
    /// </remarks>
    public ICpu Cpu { get; }

    /// <summary>
    /// Gets the emulated memory space of the Apple II system.
    /// </summary>
    /// <remarks>
    /// The <see cref="IMemory"/> interface provides methods to read, write, and manage the memory space.
    /// This property is essential for interacting with the memory of the emulated Apple II system,
    /// including setting up system defaults and wiring components like the speaker.
    /// </remarks>
    public IMemory Memory { get; }

    /// <summary>
    /// Gets the Apple II speaker emulation instance.
    /// </summary>
    /// <remarks>
    /// The speaker is used to emulate sound generation in the Apple II system.
    /// It is wired to the memory for handling specific memory access operations
    /// (e.g., $C030) and supports operations such as <see cref="IAppleSpeaker.Beep"/>
    /// and <see cref="IAppleSpeaker.Click"/>.
    /// </remarks>
    public IAppleSpeaker Speaker { get; }

    /// <summary>
    /// Reads a byte of data from the specified memory address.
    /// </summary>
    /// <param name="address">The memory address to read from. Must be a valid address within the emulated memory space.</param>
    /// <returns>The byte of data stored at the specified memory address.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the specified <paramref name="address"/> is outside the valid range of memory addresses.
    /// </exception>
    public byte Peek(int address)
    {
        ValidateAddress(address);
        return Memory.Read(address);
    }

    /// <summary>
    /// Writes a byte value to the specified memory address in the Apple II system.
    /// </summary>
    /// <param name="address">The memory address where the value will be written. Must be a valid address within the memory range.</param>
    /// <param name="value">The byte value to write to the specified memory address.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the specified address is invalid.</exception>
    /// <remarks>
    /// This method emulates the POKE operation in the Apple II system, allowing direct manipulation of memory.
    /// </remarks>
    public void Poke(int address, byte value)
    {
        ValidateAddress(address);
        Memory.Write(address, value);
        logger.LogTrace("POKE {Address}, {Value}", address, value);
    }

    /// <summary>
    /// Executes a machine code subroutine at the specified memory address.
    /// </summary>
    /// <param name="address">
    /// The memory address where the machine code subroutine begins.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the specified <paramref name="address"/> is invalid.
    /// </exception>
    /// <exception cref="Exception">
    /// Thrown if an error occurs during the execution of the machine code.
    /// </exception>
    /// <remarks>
    /// This method validates the address, handles common ROM calls, and executes
    /// the machine code subroutine. If the subroutine is not a ROM call, the CPU
    /// is instructed to execute the code at the specified address.
    /// </remarks>
    public void Call(int address)
    {
        ValidateAddress(address);
        logger.LogDebug("CALL {Address}", address);

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
            logger.LogError(ex, "Error executing CALL at ${Address:X4}", address);
            throw;
        }
    }

    /// <summary>
    /// Sets the keyboard input for the Apple II system emulation.
    /// </summary>
    /// <param name="key">
    /// The character to be set as the keyboard input. The high bit of the character
    /// is automatically set to indicate that a key is pressed.
    /// </param>
    /// <remarks>
    /// This method updates the internal keyboard buffer and writes the key value
    /// to the memory location associated with the keyboard input.
    /// </remarks>
    public void SetKeyboardInput(char key)
    {
        keyboardBuffer = key;

        // Set keyboard data with high bit set (key pressed)
        Memory.Write(MemoryLocations.KBD, (byte)(key | 0x80));
    }

    /// <summary>
    /// Retrieves the current character from the keyboard buffer, if available.
    /// </summary>
    /// <returns>
    /// The character from the keyboard buffer, or <c>null</c> if the buffer is empty.
    /// </returns>
    /// <remarks>
    /// After retrieving the character, the keyboard buffer is cleared, and the strobe
    /// bit in the memory location <see cref="MemoryLocations.KBD"/> is reset.
    /// </remarks>
    public char? GetKeyboardInput()
    {
        var key = keyboardBuffer;
        keyboardBuffer = null;

        // Clear the strobe
        Memory.Write(MemoryLocations.KBD, (byte)(Memory.Read(MemoryLocations.KBD) & 0x7F));
        return key;
    }

    /// <summary>
    /// Resets the Apple II system to its initial state.
    /// </summary>
    /// <remarks>
    /// This method performs the following actions:
    /// <list type="bullet">
    /// <item><description>Resets the CPU to its initial state.</description></item>
    /// <item><description>Initializes the system components.</description></item>
    /// <item><description>Clears the keyboard input buffer.</description></item>
    /// </list>
    /// </remarks>
    public void Reset()
    {
        Cpu.Reset();
        InitializeSystem();
        keyboardBuffer = null;
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

        logger.LogDebug("Apple II system initialized");
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
                    logger.LogWarning("CALL to ROM address ${Address:X4} - may not work correctly", address);
                }

                return false;
        }
    }

    private void ValidateAddress(int address)
    {
        if (address < 0 || address >= Memory.Size)
        {
            throw new MemoryAccessException(
                $"Address {address} (${address:X4}) out of bounds. Valid range: 0-{Memory.Size - 1} ($0000-${Memory.Size - 1:X4})");
        }
    }

    //// Important Apple II memory locations

    /// <summary>
    /// Contains constants representing important memory locations in the Apple II system.
    /// </summary>
    /// <remarks>
    /// These memory locations include zero-page variables, BASIC variable pointers, ROM entry points,
    /// keyboard-related addresses, and soft switches. They are used to emulate the behavior of the Apple II system.
    /// </remarks>
    public static class MemoryLocations
    {
#pragma warning disable SA1600
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        // Zero page locations
        // ReSharper disable InconsistentNaming
        public const int WNDLFT = 0x20;    // Left edge of scroll window
        public const int WNDWDTH = 0x21;   // Width of scroll window
        public const int WNDTOP = 0x22;    // Top of scroll window
        public const int WNDBTM = 0x23;    // Bottom of scroll window
        public const int CH = 0x24;        // Cursor horizontal position
        public const int CV = 0x25;        // Cursor vertical position
        public const int GPTS = 0x2C;      // Graphics point coordinates
        public const int PROMPT = 0x33;    // Input prompt character

        // USR function vector (JMP instruction at $000A)
        public const int USRADR = 0x000A;  // USR function jump address (3 bytes: JMP + address)

        // Floating-point accumulator (FAC) locations
        public const int FAC1 = 0x009D;    // FAC1 - Primary floating-point accumulator (5 bytes)
        public const int FAC1SIGN = 0x00A2; // FAC1 sign byte
        public const int FAC2 = 0x00A5;    // FAC2 - Secondary floating-point accumulator (5 bytes)

        // Ampersand vector
        public const int AMPERV = 0x03F5;  // Ampersand (&) vector address (JSR target)

        // BASIC variables
        public const int TXTTAB = 0x67;    // Start of BASIC program
        public const int VARTAB = 0x69;    // Start of simple variables
        public const int ARYTAB = 0x6B;    // Start of array variables
        public const int STREND = 0x6D;    // End of arrays / start of free space
        public const int FRETOP = 0x6F;    // Top of string storage
        public const int FRESPC = 0x71;    // Temp pointer for strings
        public const int MEMSIZ = 0x73;    // Top of memory
        public const int CURLIN = 0x75;    // Current line number
        public const int OLDLIN = 0x77;    // Previous line number (for CONT)
        public const int OLDTXT = 0x79;    // Pointer to statement to execute
        public const int DATLIN = 0x7B;    // Line number of current DATA
        public const int DATPTR = 0x7D;    // Pointer to next DATA item
        public const int VARNAM = 0x82;    // Current variable name
        public const int VARPNT = 0x83;    // Pointer to current variable

        // ROM entry points
        public const int OUTCH = 0xFDED;   // Character output routine
        public const int RDKEY = 0xFD0C;   // Read keyboard
        public const int GETLN = 0xFD6A;   // Get line of input
        public const int CROUT = 0xFD8E;   // Output carriage return
        public const int BELL = 0xFBE4;    // Ring bell
        public const int HOME = 0xFC58;    // Clear screen and home cursor
        public const int CLREOL = 0xFC9C;  // Clear to end of line
        public const int COUT = 0xFDF0;    // Character output (uses OUTCH)

        // Keyboard
        public const int KBD = 0xC000;     // Keyboard data
        public const int KBDSTRB = 0xC010; // Keyboard strobe

        // Soft switches
        public const int SPKR = 0xC030;    // Speaker toggle
        public const int TXTCLR = 0xC050;  // Graphics mode
        public const int TXTSET = 0xC051;  // Text mode
        public const int MIXCLR = 0xC052;  // Full screen
        public const int MIXSET = 0xC053;  // Mixed mode
        public const int PAGE1 = 0xC054;   // Display page 1
        public const int PAGE2 = 0xC055;   // Display page 2
        public const int LORES = 0xC056;   // Lo-res mode
        public const int HIRES = 0xC057;   // Hi-res mode

        // ReSharper restore InconsistentNaming
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore SA1600
    }
}