// <copyright file="KeyboardMachineTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using System.Text;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core.Configuration;
using BadMango.Emulator.Debug.Infrastructure;
using BadMango.Emulator.Devices;
using BadMango.Emulator.Devices.Interfaces;
using BadMango.Emulator.Systems;

/// <summary>
/// End-to-end keyboard tests on a real Apple IIe Enhanced machine (<c>pocket2e-a2-enh</c> profile).
/// </summary>
/// <remarks>
/// Input tests run a 6502 polling loop in RAM that waits for <c>$C000</c> bit 7, clears the strobe
/// through <c>$C010</c>, and stores each character to a RAM buffer, the way firmware reads the
/// keyboard. The tests drive input only through the keyboard device and its translator; they never
/// read or poke <c>$C000</c> between steps.
/// </remarks>
[TestFixture]
public sealed class KeyboardMachineTests
{
    private const string ProfileName = "pocket2e-a2-enh";
    private const ushort KeyboardDataAddress = 0xC000;
    private const ushort KeyboardStrobeAddress = 0xC010;
    private const ushort PollLoopAddress = 0x6000;
    private const ushort IdleLoopAddress = 0x6080;
    private const ushort BufferCountAddress = 0x60FF;
    private const ushort BufferAddress = 0x6100;

    /// <summary>
    /// Emulated cycles per millisecond at about 1.02 MHz.
    /// </summary>
    private const ulong CyclesPerMillisecond = 1020;

    /// <summary>
    /// How long a host key is held in the host-key tests (20 ms).
    /// </summary>
    private const ulong HoldCycles = 20 * CyclesPerMillisecond;

    /// <summary>
    /// The gap after releasing a host key in the host-key tests (10 ms).
    /// </summary>
    private const ulong GapCycles = 10 * CyclesPerMillisecond;

    private static string RepositoryRoot { get; } = LocateRepositoryRoot();

    /// <summary>
    /// Verifies keyboard data/strobe behavior through real soft-switch reads and writes.
    /// </summary>
    [Test]
    public void KeyboardSoftSwitches_KeyDownAndStrobeClear_BehaveOnMachineBus()
    {
        var machine = CreateMachine();
        var keyboard = machine.GetComponent<AppleIIeKeyboard>()!;

        keyboard.KeyDown((byte)'A');
        byte keyData = machine.Cpu.Read8(KeyboardDataAddress);
        byte keyDownBeforeRelease = machine.Cpu.Read8(KeyboardStrobeAddress);
        byte keyDataAfterReadClear = machine.Cpu.Read8(KeyboardDataAddress);

        keyboard.KeyUp();
        byte keyDownAfterRelease = machine.Cpu.Read8(KeyboardStrobeAddress);

        keyboard.KeyDown((byte)'B');
        machine.Cpu.Write8(KeyboardStrobeAddress, 0x00);
        byte keyDataAfterWriteClear = machine.Cpu.Read8(KeyboardDataAddress);

        Assert.Multiple(() =>
        {
            Assert.That(keyData, Is.EqualTo((byte)('A' | 0x80)));
            Assert.That(keyDownBeforeRelease, Is.EqualTo(0x80));
            Assert.That(keyDataAfterReadClear, Is.EqualTo((byte)'A'));
            Assert.That(keyDownAfterRelease, Is.EqualTo(0x00));
            Assert.That(keyDataAfterWriteClear, Is.EqualTo((byte)'B'));
        });
    }

    /// <summary>
    /// Verifies that debugger reads of <c>$C010</c> do not clear the keyboard strobe.
    /// </summary>
    [Test]
    public void KeyboardSoftSwitches_DebugReadC010_DoesNotClearStrobe()
    {
        var machine = CreateMachine();
        var keyboard = machine.GetComponent<AppleIIeKeyboard>()!;

        keyboard.KeyDown((byte)'A');
        byte keyDataBeforeDebugRead = machine.Cpu.Read8(KeyboardDataAddress);
        byte keyDownDebugRead = DebugPeek(machine.Bus, KeyboardStrobeAddress);
        byte keyDataAfterDebugRead = machine.Cpu.Read8(KeyboardDataAddress);
        byte keyDownNormalRead = machine.Cpu.Read8(KeyboardStrobeAddress);
        byte keyDataAfterNormalRead = machine.Cpu.Read8(KeyboardDataAddress);

        Assert.Multiple(() =>
        {
            Assert.That(keyDataBeforeDebugRead, Is.EqualTo((byte)('A' | 0x80)));
            Assert.That(keyDownDebugRead, Is.EqualTo(0x80));
            Assert.That(keyDataAfterDebugRead, Is.EqualTo((byte)('A' | 0x80)));
            Assert.That(keyDownNormalRead, Is.EqualTo(0x80));
            Assert.That(keyDataAfterNormalRead, Is.EqualTo((byte)'A'));
        });
    }

    /// <summary>
    /// Verifies that <see cref="IAppleIIeKeyboard.TypeString"/> delivers <c>Hello, World!</c> to a
    /// polling loop in order, with nothing dropped or doubled, and that every key is released
    /// (<see cref="IAppleIIeKeyboard.HasKeyDown"/> observably false) before the next one goes down.
    /// </summary>
    [Test]
    public void TypeString_HelloWorld_PollingLoopReceivesEveryCharacterWithKeyUpBetween()
    {
        const string Text = "Hello, World!";
        var machine = CreateMachine();
        var keyboard = machine.GetComponent<AppleIIeKeyboard>()!;
        InstallPollingLoop(machine);

        keyboard.TypeString(Text);

        int keyPresses = 0;
        int releasesSeenBetweenPresses = 0;
        bool wasDown = false;
        ulong end = machine.Now.Value + 4_000_000;
        while ((BufferCount(machine) < Text.Length || keyboard.HasKeyDown) && machine.Now.Value < end)
        {
            machine.Cpu.Step();
            bool isDown = keyboard.HasKeyDown;
            if (isDown && !wasDown)
            {
                keyPresses++;
            }
            else if (!isDown && wasDown)
            {
                releasesSeenBetweenPresses++;
            }

            wasDown = isDown;
        }

        StepFor(machine, 100_000);

        Assert.Multiple(() =>
        {
            Assert.That(ReadBuffer(machine), Is.EqualTo(Text));
            Assert.That(keyPresses, Is.EqualTo(Text.Length), "Each character must be its own key press; HasKeyDown must go false between characters");
            Assert.That(releasesSeenBetweenPresses, Is.EqualTo(Text.Length), "Each typed key must be released");
            Assert.That(keyboard.HasKeyDown, Is.False, "No key should remain down after typing");
            Assert.That(keyboard.KeyData & 0x80, Is.Zero, "No pending strobe should remain after typing");
        });
    }

    /// <summary>
    /// Verifies that typing waits for software to consume a key instead of overwriting it, and that
    /// the queued keys arrive once a program starts reading the keyboard.
    /// </summary>
    [Test]
    public void TypeString_ProgramNotReadingYet_KeepsTypeAheadUntilItReads()
    {
        var machine = CreateMachine();
        var keyboard = machine.GetComponent<AppleIIeKeyboard>()!;
        InstallPollingLoop(machine);
        InstallIdleLoop(machine);

        keyboard.TypeString("AB", delayMs: 2);
        StepFor(machine, 100_000);
        byte latchedWhileIdle = keyboard.KeyData;

        machine.Cpu.SetPC(PollLoopAddress);
        bool received = StepUntil(machine, () => BufferCount(machine) == 2, maxCycles: 200_000);

        Assert.Multiple(() =>
        {
            Assert.That(latchedWhileIdle, Is.EqualTo((byte)('A' | 0x80)), "The first key stays latched until software reads it");
            Assert.That(received, Is.True, "The second key must arrive after the program reads the first");
            Assert.That(ReadBuffer(machine), Is.EqualTo("AB"));
        });
    }

    /// <summary>
    /// Verifies the host-key path: Shift+H, I, Shift+1 through the keyboard's translator give
    /// <c>Hi!</c> in the polling loop's buffer.
    /// </summary>
    [Test]
    public void KeyboardKeys_ShiftH_I_Shift1_TypesHiBang()
    {
        var machine = CreateMachine();
        var keyboard = machine.GetComponent<AppleIIeKeyboard>()!;
        InstallPollingLoop(machine);

        PressKeyboardKey(machine, keyboard, KeyboardKey.H, KeyboardModifiers.Shift);
        PressKeyboardKey(machine, keyboard, KeyboardKey.I, KeyboardModifiers.None);
        PressKeyboardKey(machine, keyboard, KeyboardKey.D1, KeyboardModifiers.Shift);

        Assert.That(ReadBuffer(machine), Is.EqualTo("Hi!"));
    }

    /// <summary>
    /// Verifies Caps Lock through the host-key path: letters are uppercase, Shift with Caps Lock
    /// gives lowercase (current PC-style rule), and digits ignore Caps Lock.
    /// </summary>
    [Test]
    public void KeyboardKeys_CapsLock_UppercasesLettersOnly()
    {
        var machine = CreateMachine();
        var keyboard = machine.GetComponent<AppleIIeKeyboard>()!;
        InstallPollingLoop(machine);

        PressKeyboardKey(machine, keyboard, KeyboardKey.H, KeyboardModifiers.CapsLock);
        PressKeyboardKey(machine, keyboard, KeyboardKey.I, KeyboardModifiers.CapsLock);
        PressKeyboardKey(machine, keyboard, KeyboardKey.D1, KeyboardModifiers.CapsLock);
        PressKeyboardKey(machine, keyboard, KeyboardKey.D1, KeyboardModifiers.CapsLock | KeyboardModifiers.Shift);
        PressKeyboardKey(machine, keyboard, KeyboardKey.X, KeyboardModifiers.CapsLock | KeyboardModifiers.Shift);

        Assert.That(ReadBuffer(machine), Is.EqualTo("HI1!x"));
    }

    /// <summary>
    /// Verifies control codes through the host-key path: Ctrl+G, Ctrl+Shift+G, Ctrl+@ and Ctrl+[.
    /// </summary>
    [Test]
    public void KeyboardKeys_Control_DeliversControlCodes()
    {
        var machine = CreateMachine();
        var keyboard = machine.GetComponent<AppleIIeKeyboard>()!;
        InstallPollingLoop(machine);

        PressKeyboardKey(machine, keyboard, KeyboardKey.G, KeyboardModifiers.Control);
        PressKeyboardKey(machine, keyboard, KeyboardKey.G, KeyboardModifiers.Control | KeyboardModifiers.Shift);
        PressKeyboardKey(machine, keyboard, KeyboardKey.D2, KeyboardModifiers.Control | KeyboardModifiers.Shift);
        PressKeyboardKey(machine, keyboard, KeyboardKey.OpenBracket, KeyboardModifiers.Control);

        Assert.That(ReadBufferBytes(machine), Is.EqualTo(new byte[] { 0x07, 0x07, 0x00, 0x1B }));
    }

    /// <summary>
    /// Verifies that holding Open Apple and Closed Apple does not change the codes the polling loop
    /// receives, while both still show up on the pushbutton soft switches.
    /// </summary>
    [Test]
    public void KeyboardKeys_AppleKeysHeld_DoNotChangeCodes()
    {
        var machine = CreateMachine();
        var keyboard = machine.GetComponent<AppleIIeKeyboard>()!;
        InstallPollingLoop(machine);
        var apples = KeyboardModifiers.OpenApple | KeyboardModifiers.ClosedApple;

        PressKeyboardKey(machine, keyboard, KeyboardKey.A, KeyboardModifiers.OpenApple);
        PressKeyboardKey(machine, keyboard, KeyboardKey.A, KeyboardModifiers.ClosedApple | KeyboardModifiers.Shift);
        PressKeyboardKey(machine, keyboard, KeyboardKey.D1, apples | KeyboardModifiers.Shift);
        keyboard.SetModifiers(apples);
        byte pb0 = machine.Cpu.Read8(0xC061);
        byte pb1 = machine.Cpu.Read8(0xC062);

        Assert.Multiple(() =>
        {
            Assert.That(ReadBuffer(machine), Is.EqualTo("aA!"));
            Assert.That(pb0, Is.EqualTo(0x80), "Open Apple is visible at $C061");
            Assert.That(pb1, Is.EqualTo(0x80), "Closed Apple is visible at $C062");
        });
    }

    private static void PressKeyboardKey(IMachine machine, IAppleIIeKeyboard keyboard, KeyboardKey key, KeyboardModifiers modifiers)
    {
        byte? code = keyboard.Translator.Translate(key, modifiers);
        Assert.That(code, Is.Not.Null, $"{key} with {modifiers} must produce a code");

        int countBefore = BufferCount(machine);
        keyboard.SetModifiers(modifiers);
        keyboard.KeyDown(code!.Value);
        StepFor(machine, HoldCycles);
        keyboard.KeyUp();
        keyboard.SetModifiers(KeyboardModifiers.None);
        StepFor(machine, GapCycles);

        Assert.That(BufferCount(machine), Is.EqualTo(countBefore + 1), $"The polling loop must receive exactly one character for {key}");
    }

    private static void InstallPollingLoop(IMachine machine)
    {
        byte lo = PollLoopAddress & 0xFF;
        byte hi = PollLoopAddress >> 8;
        Poke(
            machine,
            PollLoopAddress,
            [
                0xA2, 0x00,                                  // LDX #$00
                0xAD, 0x00, 0xC0,                            // poll: LDA $C000
                0x10, 0xFB,                                  //       BPL poll
                0x8D, 0x10, 0xC0,                            //       STA $C010   ; clear strobe
                0x29, 0x7F,                                  //       AND #$7F
                0x9D, BufferAddress & 0xFF, BufferAddress >> 8, //    STA buffer,X
                0xE8,                                        //       INX
                0x8E, BufferCountAddress & 0xFF, BufferCountAddress >> 8, // STX count
                0x4C, (byte)(lo + 2), hi,                    //       JMP poll
            ]);
        machine.Cpu.Poke8(BufferCountAddress, 0);
        machine.Cpu.SetPC(PollLoopAddress);
    }

    private static void InstallIdleLoop(IMachine machine)
    {
        // JMP IdleLoopAddress: a program that never reads the keyboard.
        Poke(machine, IdleLoopAddress, [0x4C, IdleLoopAddress & 0xFF, IdleLoopAddress >> 8]);
        machine.Cpu.SetPC(IdleLoopAddress);
    }

    private static int BufferCount(IMachine machine) => machine.Cpu.Peek8(BufferCountAddress);

    private static byte[] ReadBufferBytes(IMachine machine)
    {
        int count = BufferCount(machine);
        var bytes = new byte[count];
        for (int i = 0; i < count; i++)
        {
            bytes[i] = machine.Cpu.Peek8((uint)(BufferAddress + i));
        }

        return bytes;
    }

    private static string ReadBuffer(IMachine machine) => Encoding.ASCII.GetString(ReadBufferBytes(machine));

    private static IMachine CreateMachine()
    {
        string profilePath = Path.Join(RepositoryRoot, "profiles", ProfileName + ".json");
        var loader = new MachineProfileLoader(Path.Join(RepositoryRoot, "profiles"));
        var profile = loader.LoadProfileFromFile(profilePath);
        var resolver = new ProfilePathResolver(libraryRoot: RepositoryRoot, profileFilePath: profilePath);
        var machine = MachineFactory.CreateMachine(profile, resolver);

        // Power on: the first reset after construction is the power-on reset.
        machine.Reset();
        return machine;
    }

    private static void Poke(IMachine machine, int address, byte[] bytes)
    {
        for (int i = 0; i < bytes.Length; i++)
        {
            machine.Cpu.Poke8((uint)(address + i), bytes[i]);
        }
    }

    private static void StepFor(IMachine machine, ulong cycles)
    {
        ulong end = machine.Now.Value + cycles;
        while (machine.Now.Value < end)
        {
            machine.Cpu.Step();
        }
    }

    private static bool StepUntil(IMachine machine, Func<bool> condition, ulong maxCycles)
    {
        ulong end = machine.Now.Value + maxCycles;
        while (!condition())
        {
            if (machine.Now.Value >= end)
            {
                return false;
            }

            machine.Cpu.Step();
        }

        return true;
    }

    private static byte DebugPeek(IMemoryBus bus, ushort address)
    {
        var access = new BusAccess(
            Address: address,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: AccessIntent.DebugRead,
            SourceId: 0,
            Cycle: 0,
            Flags: AccessFlags.NoSideEffects);

        var result = bus.TryRead8(access);
        return result.Ok ? result.Value : (byte)0xFF;
    }

    private static string LocateRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (Directory.Exists(Path.Join(dir.FullName, "profiles")) &&
                Directory.Exists(Path.Join(dir.FullName, "roms")) &&
                Directory.Exists(Path.Join(dir.FullName, "disks")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException($"Could not locate the repository root from {AppContext.BaseDirectory}.");
    }
}