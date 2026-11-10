// <copyright file="ResetSignalMachineTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using System.Text;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core;
using BadMango.Emulator.Core.Configuration;
using BadMango.Emulator.Core.Cpu;
using BadMango.Emulator.Core.Interfaces.Signaling;
using BadMango.Emulator.Core.Signaling;
using BadMango.Emulator.Debug.Infrastructure;
using BadMango.Emulator.Debug.Infrastructure.Commands;
using BadMango.Emulator.Devices;
using BadMango.Emulator.Devices.Interfaces;
using BadMango.Emulator.Storage.Formats;
using BadMango.Emulator.Storage.Media;
using BadMango.Emulator.Systems;

/// <summary>
/// End-to-end tests of the RESET signal on a real Apple IIe Enhanced machine
/// (<c>pocket2e-a2-enh</c> profile with the Apple ROMs), covering power-on, soft and
/// hard reset, device responses, the keyboard Reset key and the REPL <c>reset</c> command.
/// </summary>
[TestFixture]
public sealed class ResetSignalMachineTests
{
    private const string ProfileName = "pocket2e-a2-enh";
    private const ushort LoopAddress = 0x6000;
    private const ushort LcBank1ReadWrite = 0xC08B;

    private static string RepositoryRoot { get; } = LocateRepositoryRoot();

    /// <summary>
    /// Verifies that a freshly built machine has been power-on reset: PC is the ROM reset
    /// vector and the CPU registers are in their power-on state.
    /// </summary>
    [Test]
    public void PowerOn_BuiltMachine_IsAtResetVectorWithPowerOnRegisters()
    {
        var machine = CreateMachine();

        Assert.Multiple(() =>
        {
            Assert.That(machine.Cpu.GetPC(), Is.EqualTo(ReadResetVector(machine)));
            Assert.That(machine.Cpu.Registers.E, Is.True, "E");
            Assert.That(machine.Cpu.Registers.CP, Is.True, "CP");
            Assert.That(machine.Cpu.Registers.P & ProcessorStatusFlags.M, Is.EqualTo(ProcessorStatusFlags.M), "M");
            Assert.That(machine.Cpu.Registers.P & ProcessorStatusFlags.X, Is.EqualTo(ProcessorStatusFlags.X), "X");
            Assert.That(machine.Cpu.Registers.P.IsInterruptDisabled(), Is.True, "I");
            Assert.That(machine.Cpu.Registers.P.IsDecimalModeEnabled(), Is.False, "D");
            Assert.That(machine.Cpu.Registers.SP.GetByte(), Is.EqualTo(new Registers(true, 0).SP.GetByte()), "S must be the power-on value, not decremented");
            Assert.That(machine.Signals.IsAsserted(SignalLine.Reset), Is.False);
        });
    }

    /// <summary>
    /// Verifies that the ROM boots to the Apple //e banner after power-on.
    /// </summary>
    [Test]
    public void PowerOn_RomBootsToAppleIIeBanner()
    {
        var machine = CreateMachine();

        StepFor(machine, 3_000_000);

        Assert.That(ReadTextLine(machine, 0), Does.Contain("Apple //e"));
    }

    /// <summary>
    /// Verifies that a soft reset runs the warm reset sequence (S decremented by three, no
    /// stack writes, I set, D clear) and continues at the ROM reset vector, whose handler then
    /// runs (cold-starting, because the power-up byte is invalid).
    /// </summary>
    [Test]
    public void SoftReset_RunsWarmResetSequenceAndRebootsRom()
    {
        var machine = CreateMachine();
        StepFor(machine, 3_000_000);
        Poke(machine, 0x0100, new byte[256]);
        WriteLoop(machine);
        machine.Cpu.Registers.SP.SetByte(0xA0);
        machine.Cpu.Registers.P.SetDecimalMode(true);
        machine.Cpu.Registers.P.SetInterruptDisable(false);
        ClearTextLine(machine, 0);

        // Invalidate the power-up byte ($03F4 must equal $03F3 EOR $A5) so the ROM reset
        // handler takes its cold-start path and redraws the banner.
        machine.Cpu.Poke8(0x03F4, (byte)(machine.Cpu.Peek8(0x03F3) ^ 0xA5 ^ 0xFF));

        machine.Reset();

        Assert.Multiple(() =>
        {
            Assert.That(machine.Cpu.GetPC(), Is.EqualTo(ReadResetVector(machine)));
            Assert.That(machine.Cpu.Registers.SP.GetByte(), Is.EqualTo(0x9D));
            Assert.That(machine.Cpu.Registers.P.IsInterruptDisabled(), Is.True);
            Assert.That(machine.Cpu.Registers.P.IsDecimalModeEnabled(), Is.False);
            Assert.That(machine.Cpu.Peek8(0x019E) | machine.Cpu.Peek8(0x019F) | machine.Cpu.Peek8(0x01A0), Is.Zero, "RESET must not write the stack");
        });

        StepFor(machine, 3_000_000);
        Assert.That(ReadTextLine(machine, 0), Does.Contain("Apple //e"), "The ROM reset handler must run after a soft reset");
    }

    /// <summary>
    /// Verifies that the VBlank cadence survives a soft reset: the scheduler flush must not
    /// lose the next VBlank edge, and its phase is preserved.
    /// </summary>
    [Test]
    public void SoftReset_KeepsVBlankCadenceAndPhase()
    {
        var machine = CreateMachine();
        var video = machine.GetComponent<VideoDevice>()!;
        var vblankStarts = new List<ulong>();
        video.VBlankOccurred += () => vblankStarts.Add(machine.Now.Value);
        WriteLoop(machine);

        StepFor(machine, 200_000);
        Assert.That(vblankStarts, Is.Not.Empty);
        ulong lastBeforeReset = vblankStarts[^1];

        // Reset mid-frame, well away from a VBlank edge.
        StepUntil(machine, () => machine.Now.Value >= lastBeforeReset + 8_000);
        machine.Reset();
        WriteLoop(machine);
        vblankStarts.Clear();

        StepFor(machine, 500_000);

        Assert.That(vblankStarts, Has.Count.InRange(28, 30), "VBlank must keep firing every frame after a soft reset");
        ulong offset = (vblankStarts[0] - lastBeforeReset) % VideoDevice.CyclesPerFrame;
        ulong drift = Math.Min(offset, VideoDevice.CyclesPerFrame - offset);
        Assert.That(drift, Is.LessThan(64UL), "The first VBlank after reset must stay on the pre-reset frame phase");
    }

    /// <summary>
    /// Verifies that a soft reset that lands inside vertical blanking does not leave
    /// <c>RDVBL</c> stuck: blanking still ends and starts again.
    /// </summary>
    [Test]
    public void SoftReset_DuringVerticalBlanking_DoesNotLeaveBlankingStuck()
    {
        var machine = CreateMachine();
        var video = machine.GetComponent<VideoDevice>()!;
        WriteLoop(machine);
        StepUntil(machine, () => video.IsVerticalBlanking);

        machine.Reset();
        WriteLoop(machine);

        bool sawEnd = StepUntil(machine, () => !video.IsVerticalBlanking, maxCycles: VideoDevice.VBlankDurationCycles + 100);
        bool sawNextStart = StepUntil(machine, () => video.IsVerticalBlanking, maxCycles: VideoDevice.CyclesPerFrame + 100);

        Assert.Multiple(() =>
        {
            Assert.That(sawEnd, Is.True, "Vertical blanking must end after the reset");
            Assert.That(sawNextStart, Is.True, "The next vertical blanking must start after the reset");
        });
    }

    /// <summary>
    /// Verifies the display switches after a soft reset against Sather, <i>Understanding the
    /// Apple IIe</i>, Table 7.1 (IOU) and Table 5.4 (MMU): 80STORE, 80COL, ALTCHRSET, PAGE2,
    /// HIRES and AN0–AN3 are reset; TEXT and MIXED keep their values.
    /// </summary>
    [Test]
    public void SoftReset_ResetsDisplaySwitchesPerHardwareReference()
    {
        var machine = CreateMachine();
        var video = machine.GetComponent<VideoDevice>()!;
        var character = machine.GetComponent<CharacterDevice>()!;
        var ext80 = machine.GetComponent<Extended80ColumnDevice>()!;

        machine.Cpu.Write8(0xC001, 0); // 80STOREON
        machine.Cpu.Write8(0xC00D, 0); // 80COLON
        machine.Cpu.Write8(0xC00F, 0); // ALTCHARSETON
        machine.Cpu.Read8(0xC050);     // TEXT off (graphics)
        machine.Cpu.Read8(0xC053);     // MIXED on
        machine.Cpu.Read8(0xC055);     // PAGE2 on
        machine.Cpu.Read8(0xC057);     // HIRES on
        machine.Cpu.Read8(0xC059);     // AN0 on
        machine.Cpu.Read8(0xC05F);     // AN3 on

        Assume.That(video.IsPage2 && video.IsHiRes && video.Is80Column && !video.IsTextMode && video.IsMixedMode, Is.True);
        Assume.That(ext80.Is80StoreEnabled && character.IsAltCharSet, Is.True);

        machine.Reset();

        Assert.Multiple(() =>
        {
            Assert.That(ext80.Is80StoreEnabled, Is.False, "80STORE");
            Assert.That(ext80.Is80ColumnEnabled, Is.False, "80COL (80-column device)");
            Assert.That(video.Is80Column, Is.False, "80COL (video)");
            Assert.That(character.IsAltCharSet, Is.False, "ALTCHRSET");
            Assert.That(video.IsPage2, Is.False, "PAGE2 (video)");
            Assert.That(ext80.IsPage2Selected, Is.False, "PAGE2 (MMU)");
            Assert.That(video.IsHiRes, Is.False, "HIRES (video)");
            Assert.That(ext80.IsHiResEnabled, Is.False, "HIRES (MMU)");
            Assert.That(video.Annunciators, Has.All.False, "AN0-AN3");
            Assert.That(video.IsTextMode, Is.False, "TEXT is not affected by RESET");
            Assert.That(video.IsMixedMode, Is.True, "MIXED is not affected by RESET");
        });
    }

    /// <summary>
    /// Verifies that the RESET vector is fetched through the post-reset memory banking: with the
    /// Language Card mapped for RAM reads and a different vector in Language Card RAM, the CPU
    /// must still start at the ROM vector.
    /// </summary>
    [Test]
    public void SoftReset_FetchesVectorThroughPostResetBanking()
    {
        var machine = CreateMachine();
        ushort romVector = ReadResetVector(machine);

        machine.Cpu.Read8(LcBank1ReadWrite);
        machine.Cpu.Read8(LcBank1ReadWrite);
        machine.Cpu.Write8(0xFFFC, 0x34);
        machine.Cpu.Write8(0xFFFD, 0x12);
        Assume.That(ReadResetVector(machine), Is.EqualTo(0x1234), "Language Card RAM should be visible at $FFFC");

        machine.Reset();

        Assert.Multiple(() =>
        {
            Assert.That(machine.Cpu.GetPC(), Is.EqualTo(romVector), "The vector must come from ROM after the MMU resets");
            Assert.That(machine.GetComponent<LanguageCardDevice>()!.IsRamReadEnabled, Is.False);
        });
    }

    /// <summary>
    /// Verifies that RESET is a wired-OR line: while the keyboard's Reset key holds it low, a
    /// machine reset pulse ends but the CPU stays held until the key is released too.
    /// </summary>
    [Test]
    public void WiredOr_KeyboardHoldsLineAfterMachinePulse()
    {
        var machine = CreateMachine();
        var keyboard = machine.GetComponent<KeyboardDevice>()!;
        WriteLoop(machine);

        keyboard.SetModifiers(KeyboardModifiers.Control);
        keyboard.PressResetKey();
        Assume.That(machine.Signals.IsAsserted(SignalLine.Reset), Is.True);

        machine.Reset();
        uint heldPc = machine.Cpu.GetPC();
        for (int i = 0; i < 10; i++)
        {
            machine.Cpu.Step();
        }

        bool stillAsserted = machine.Signals.IsAsserted(SignalLine.Reset);
        uint pcWhileHeld = machine.Cpu.GetPC();

        keyboard.ReleaseResetKey();
        keyboard.SetModifiers(KeyboardModifiers.None);
        bool assertedAfterRelease = machine.Signals.IsAsserted(SignalLine.Reset);
        machine.Cpu.Step();

        Assert.Multiple(() =>
        {
            Assert.That(stillAsserted, Is.True, "The keyboard still holds RESET after the machine's pulse");
            Assert.That(pcWhileHeld, Is.EqualTo(heldPc), "The CPU must not execute while RESET is held");
            Assert.That(assertedAfterRelease, Is.False);
            Assert.That(machine.Cpu.GetPC(), Is.EqualTo(ReadResetVector(machine)), "Releasing the last source runs the reset sequence");
        });
    }

    /// <summary>
    /// Verifies that the Reset key alone does not reset the machine when Control is required.
    /// </summary>
    [Test]
    public void WiredOr_ResetKeyWithoutControl_DoesNotAssertTheLine()
    {
        var machine = CreateMachine();
        var keyboard = machine.GetComponent<KeyboardDevice>()!;
        WriteLoop(machine);

        keyboard.PressResetKey();
        bool asserted = machine.Signals.IsAsserted(SignalLine.Reset);
        StepFor(machine, 100);
        keyboard.ReleaseResetKey();

        Assert.Multiple(() =>
        {
            Assert.That(asserted, Is.False);
            Assert.That(machine.Cpu.GetPC(), Is.InRange((uint)LoopAddress, LoopAddress + 2u), "The CPU keeps running the program");
        });
    }

    /// <summary>
    /// Verifies that a soft reset in the middle of <see cref="IKeyboardDevice.TypeString"/>
    /// releases the key being typed (its scheduled release was flushed), drops the
    /// type-ahead, and leaves the keyboard able to type again.
    /// </summary>
    [Test]
    public void SoftReset_DuringTypeString_ReleasesTypedKeyAndKeepsKeyboardUsable()
    {
        var machine = CreateMachine();
        var keyboard = machine.GetComponent<KeyboardDevice>()!;
        WriteLoop(machine);

        keyboard.TypeString("AB", delayMs: 50);
        StepFor(machine, 1_000);
        Assume.That(keyboard.HasKeyDown, Is.True);

        machine.Reset();
        WriteLoop(machine);
        bool keyDownAfterReset = keyboard.HasKeyDown;
        byte dataAfterReset = keyboard.KeyData;

        StepFor(machine, 200_000);
        byte dataAfterTypeAhead = keyboard.KeyData;

        keyboard.TypeString("C", delayMs: 5);
        StepFor(machine, 1_000);
        byte dataAfterNewTyping = keyboard.KeyData;
        StepFor(machine, 20_000);

        Assert.Multiple(() =>
        {
            Assert.That(keyDownAfterReset, Is.False, "The typed key must not stay down after RESET");
            Assert.That(dataAfterReset & 0x80, Is.Zero, "RESET clears the keyboard strobe");
            Assert.That(dataAfterTypeAhead & 0x80, Is.Zero, "RESET discards the type-ahead");
            Assert.That(dataAfterNewTyping, Is.EqualTo((byte)('C' | 0x80)), "The keyboard types again after RESET");
            Assert.That(keyboard.HasKeyDown, Is.False, "The new key is released on schedule");
        });
    }

    /// <summary>
    /// Verifies that a disk inserted while the drive motor runs (deferred to the next scheduler
    /// turn) is not lost when a RESET flushes the scheduler before the insert is applied.
    /// </summary>
    [Test]
    public void SoftReset_DoesNotDropDeferredDiskInsert()
    {
        var machine = CreateMachine();
        var controller = (DiskIIController)machine.GetComponent<ISlotManager>()!.GetCard(6)!;
        WriteLoop(machine);
        machine.Cpu.Read8(0xC0E9); // Slot 6 motor on
        Assume.That(controller.IsMotorOn, Is.True);

        using var image = new DiskImageFactory().Open(Path.Join(RepositoryRoot, "disks", "dos33-master.dsk"), forceReadOnly: true);
        var media = image switch
        {
            Image525AndBlockResult both => both.TrackMedia,
            Image525Result trackOnly => trackOnly.Media,
            _ => null,
        };
        Assume.That(media, Is.Not.Null);

        controller.Mount(0, media!);
        Assume.That(controller.GetDriveSnapshot(0).HasMedia, Is.False, "The insert should be deferred while the motor runs");

        machine.Reset();
        StepFor(machine, 100);

        Assert.Multiple(() =>
        {
            Assert.That(controller.GetDriveSnapshot(0).HasMedia, Is.True, "The deferred insert must survive the RESET flush");
            Assert.That(controller.IsMotorOn, Is.False, "RESET turns the drive off (Sather Table 9.1)");
        });
    }

    /// <summary>
    /// Verifies the REPL <c>reset</c> command end to end on a stopped machine: it pulses RESET
    /// through the keyboard's Reset key, restores the user's modifiers, resets devices and stops
    /// at the reset vector.
    /// </summary>
    [Test]
    public void ResetCommand_WhenStopped_PulsesKeyboardResetAndStopsAtVector()
    {
        var machine = CreateMachine();
        var keyboard = machine.GetComponent<KeyboardDevice>()!;
        var video = machine.GetComponent<VideoDevice>()!;
        var probe = new ResetProbe(machine.Signals);
        WriteLoop(machine);
        machine.Cpu.Read8(0xC055); // PAGE2 on
        keyboard.SetModifiers(KeyboardModifiers.Shift);
        var (context, output) = CreateDebugContext(machine);

        var result = new ResetCommand().Execute(context, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(probe.Asserted, Is.EqualTo(1));
            Assert.That(probe.Released, Is.EqualTo(1));
            Assert.That(machine.Signals.IsAsserted(SignalLine.Reset), Is.False);
            Assert.That(keyboard.Modifiers, Is.EqualTo(KeyboardModifiers.Shift), "The command must restore the modifiers");
            Assert.That(video.IsPage2, Is.False);
            Assert.That(machine.Cpu.GetPC(), Is.EqualTo(ReadResetVector(machine)));
            Assert.That(machine.State, Is.EqualTo(MachineState.Stopped));
            Assert.That(output.ToString(), Does.Contain("Soft reset completed."));
        });
    }

    /// <summary>
    /// Verifies the REPL <c>reset</c> command while the machine runs: the reset is applied by
    /// the run loop (never on the REPL thread), the machine keeps running, and the ROM reset
    /// handler runs.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ResetCommand_WhileRunning_ResetsOnTheRunLoopAndKeepsRunning()
    {
        var machine = CreateMachine();
        var probe = new ResetProbe(machine.Signals);
        var (context, _) = CreateDebugContext(machine);

        var runTask = machine.RunAsync();
        try
        {
            Assert.That(SpinWait.SpinUntil(() => machine.State == MachineState.Running, 5000), Is.True);

            for (int i = 1; i <= 10; i++)
            {
                var result = new ResetCommand().Execute(context, []);
                Assert.That(result.Success, Is.True, result.Message);
                int expected = i;
                Assert.That(SpinWait.SpinUntil(() => probe.Released >= expected, 5000), Is.True, $"Reset {i} was not applied");
                Assert.That(machine.State, Is.EqualTo(MachineState.Running));
            }

            var step = new StepCommand().Execute(context, []);
            Assert.That(step.Success, Is.False, "Stepping from the REPL must be refused while the machine runs");
        }
        finally
        {
            machine.Stop();
            await runTask.WaitAsync(TimeSpan.FromSeconds(10));
        }

        Assert.Multiple(() =>
        {
            Assert.That(probe.Asserted, Is.EqualTo(10));
            Assert.That(probe.CallsOnTestThread, Is.Zero, "RESET must be applied on the run loop, not the REPL thread");
        });
    }

    /// <summary>
    /// Verifies that a hard reset power-on resets cards installed with
    /// <see cref="Pocket2eMachineBuilderExtensions.WithCard"/> exactly once and delivers the
    /// RESET signal to receiver cards through the slot manager.
    /// </summary>
    [Test]
    public void HardReset_BuilderCards_AreResetExactlyOnce()
    {
        var receiverCard = new ReceiverCountingCard();
        var plainCard = new CountingCard();
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .WithCard(4, receiverCard)
            .WithCard(5, plainCard)
            .Build();
        machine.Reset();
        receiverCard.Clear();
        plainCard.Clear();

        machine.Reset();
        var afterSoft = (receiverCard.Snapshot(), plainCard.Snapshot());

        receiverCard.Clear();
        plainCard.Clear();
        machine.Reset(hardReset: true);

        Assert.Multiple(() =>
        {
            Assert.That(afterSoft.Item1, Is.EqualTo((Resets: 0, Asserted: 1, Released: 1)), "Soft reset: receiver card");
            Assert.That(afterSoft.Item2, Is.EqualTo((Resets: 1, Asserted: 0, Released: 0)), "Soft reset: plain card");
            Assert.That(receiverCard.Snapshot(), Is.EqualTo((Resets: 1, Asserted: 1, Released: 1)), "Hard reset: receiver card");
            Assert.That(plainCard.Snapshot(), Is.EqualTo((Resets: 1, Asserted: 0, Released: 0)), "Hard reset: plain card");
        });
    }

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

    private static (DebugContext Context, StringWriter Output) CreateDebugContext(IMachine machine)
    {
        var output = new StringWriter();
        var context = new DebugContext(new CommandDispatcher(), output, new StringWriter());
        context.AttachCpu(machine.Cpu);
        context.AttachBus(machine.Bus);
        context.AttachMachine(machine);
        return (context, output);
    }

    private static ushort ReadResetVector(IMachine machine)
    {
        return (ushort)(machine.Cpu.Peek8(0xFFFC) | (machine.Cpu.Peek8(0xFFFD) << 8));
    }

    private static void WriteLoop(IMachine machine)
    {
        // JMP $6000
        Poke(machine, LoopAddress, [0x4C, LoopAddress & 0xFF, LoopAddress >> 8]);
        machine.Cpu.SetPC(LoopAddress);
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

    private static bool StepUntil(IMachine machine, Func<bool> condition, ulong maxCycles = 2_000_000)
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

    private static string ReadTextLine(IMachine machine, int row)
    {
        var text = new StringBuilder();
        for (int i = 0; i < 40; i++)
        {
            byte c = (byte)(machine.Cpu.Peek8((uint)(0x400 + (row * 0x80) + i)) & 0x7F);
            text.Append(c < 0x20 ? (char)(c + 0x40) : (char)c);
        }

        return text.ToString();
    }

    private static void ClearTextLine(IMachine machine, int row)
    {
        for (int i = 0; i < 40; i++)
        {
            machine.Cpu.Poke8((uint)(0x400 + (row * 0x80) + i), 0xA0);
        }
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

    /// <summary>
    /// Records RESET line edges and the threads they are raised on.
    /// </summary>
    private sealed class ResetProbe
    {
        private readonly int testThread = Environment.CurrentManagedThreadId;
        private int asserted;
        private int released;
        private int callsOnTestThread;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResetProbe"/> class.
        /// </summary>
        /// <param name="signals">The signal bus to watch.</param>
        public ResetProbe(ISignalBus signals)
        {
            signals.SignalChanged += (line, nowAsserted, _, _) =>
            {
                if (line != SignalLine.Reset)
                {
                    return;
                }

                Interlocked.Increment(ref nowAsserted ? ref asserted : ref released);
                if (Environment.CurrentManagedThreadId == testThread)
                {
                    Interlocked.Increment(ref callsOnTestThread);
                }
            };
        }

        /// <summary>Gets the number of assert notifications.</summary>
        public int Asserted => Volatile.Read(ref asserted);

        /// <summary>Gets the number of release notifications.</summary>
        public int Released => Volatile.Read(ref released);

        /// <summary>Gets the number of notifications made on the thread that created the probe.</summary>
        public int CallsOnTestThread => Volatile.Read(ref callsOnTestThread);
    }

    /// <summary>
    /// A slot card that counts power-on resets.
    /// </summary>
    private class CountingCard : ISlotCard
    {
        /// <inheritdoc />
        public string Name => "Counting card";

        /// <inheritdoc />
        public string DeviceType => "CountingCard";

        /// <inheritdoc />
        public PeripheralKind Kind => PeripheralKind.SlotCard;

        /// <inheritdoc />
        public int SlotNumber { get; set; }

        /// <inheritdoc />
        public SlotIOHandlers? IOHandlers => null;

        /// <inheritdoc />
        public IBusTarget? ROMRegion => null;

        /// <inheritdoc />
        public IBusTarget? ExpansionROMRegion => null;

        /// <summary>Gets or sets the number of power-on resets.</summary>
        protected int Resets { get; set; }

        /// <summary>Gets or sets the number of assert notifications.</summary>
        protected int Asserted { get; set; }

        /// <summary>Gets or sets the number of release notifications.</summary>
        protected int Released { get; set; }

        /// <summary>
        /// Gets the counters.
        /// </summary>
        /// <returns>The power-on reset, assert and release counts.</returns>
        public (int Resets, int Asserted, int Released) Snapshot() => (Resets, Asserted, Released);

        /// <summary>
        /// Clears the counters.
        /// </summary>
        public void Clear()
        {
            Resets = 0;
            Asserted = 0;
            Released = 0;
        }

        /// <inheritdoc />
        public void Initialize(IEventContext context)
        {
        }

        /// <inheritdoc />
        public void Reset()
        {
            Resets++;
        }

        /// <inheritdoc />
        public void OnExpansionROMSelected()
        {
        }

        /// <inheritdoc />
        public void OnExpansionROMDeselected()
        {
        }
    }

    /// <summary>
    /// A slot card that also responds to the RESET signal and counts its notifications.
    /// </summary>
    private sealed class ReceiverCountingCard : CountingCard, IResetReceiver
    {
        /// <inheritdoc />
        public void OnResetAsserted(Cycle cycle)
        {
            Asserted++;
        }

        /// <inheritdoc />
        public void OnResetReleased(Cycle cycle)
        {
            Released++;
        }
    }
}