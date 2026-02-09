// <copyright file="MachineBuilderIntegrationTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core.Cpu;
using BadMango.Emulator.Emulation.Cpu;

/// <summary>
/// Integration tests for the machine builder that exercise the full machine lifecycle
/// with real CPU and memory implementations.
/// </summary>
[TestFixture]
public class MachineBuilderIntegrationTests
{
    /// <summary>
    /// Verifies that a minimal machine can be built with a RAM and ROM configuration.
    /// </summary>
    [Test]
    public void Build_WithMinimalConfig_CreatesFunctionalMachine()
    {
        // Arrange
        var (ram, ramTarget) = Create64KRam();
        var rom = CreateStubRom();

        // Act
        var machine = CreateMinimalMachineBuilder()
            .MapRegion(0x0000, 64 * 1024, ramTarget, RegionTag.Ram, PagePerms.All)
            .WithRom(rom, 0xC000, "ROM")
            .Build();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(machine, Is.Not.Null, "Machine should be created");
            Assert.That(machine.Cpu, Is.Not.Null, "CPU should be created");
            Assert.That(machine.Bus, Is.Not.Null, "Bus should be created");
            Assert.That(machine.State, Is.EqualTo(MachineState.Stopped), "Initial state should be Stopped");
        });
    }

    /// <summary>
    /// Verifies that machine reset sets PC from the reset vector.
    /// </summary>
    [Test]
    public void Reset_LoadsPCFromResetVector()
    {
        // Arrange
        var (ram, ramTarget) = Create64KRam();
        var rom = CreateStubRom();

        var machine = CreateMinimalMachineBuilder()
            .MapRegion(0x0000, 64 * 1024, ramTarget, RegionTag.Ram, PagePerms.All)
            .WithRom(rom, 0xC000, "ROM")
            .Build();

        // Act
        machine.Reset();

        // Assert - PC should be loaded from reset vector ($FFFC-$FFFD)
        // The stub ROM sets reset vector to $FF00
        Assert.That(
            machine.Cpu.GetPC(),
            Is.EqualTo(0xFF00),
            "PC should be loaded from reset vector");
    }

    /// <summary>
    /// Verifies that Step executes a single instruction and advances the PC.
    /// </summary>
    [Test]
    public void Step_ExecutesSingleInstruction()
    {
        // Arrange
        var (ram, ramTarget) = Create64KRam();
        var rom = CreateStubRom();

        var machine = CreateMinimalMachineBuilder()
            .MapRegion(0x0000, 64 * 1024, ramTarget, RegionTag.Ram, PagePerms.All)
            .WithRom(rom, 0xC000, "ROM")
            .Build();

        machine.Reset();

        // Act - Step should execute the JMP $FF00 at $FF00
        var result = machine.Step();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(CpuRunState.Running), "CPU should still be running");

            // JMP is 3 bytes but jumps back to same address, so PC is still $FF00
            Assert.That(
                machine.Cpu.GetPC(),
                Is.EqualTo(0xFF00),
                "PC should be at $FF00 after JMP $FF00");
        });
    }

    /// <summary>
    /// Verifies that StateChanged event is raised when the machine state changes.
    /// </summary>
    [Test]
    public void Run_RaisesStateChangedEvent()
    {
        // Arrange - Create a ROM that immediately halts
        var rom = new byte[16384];
        Array.Fill(rom, (byte)0xEA); // NOP fill
        rom[0x3F00] = 0xDB; // STP - halt CPU

        // Set vectors to $FF00
        rom[0x3FFA] = 0x00;
        rom[0x3FFB] = 0xFF;
        rom[0x3FFC] = 0x00;
        rom[0x3FFD] = 0xFF;
        rom[0x3FFE] = 0x00;
        rom[0x3FFF] = 0xFF;

        var (ram, ramTarget) = Create64KRam();

        var machine = CreateMinimalMachineBuilder()
            .MapRegion(0x0000, 64 * 1024, ramTarget, RegionTag.Ram, PagePerms.All)
            .WithRom(rom, 0xC000, "ROM")
            .Build();

        machine.Reset();

        var stateChanges = new List<MachineState>();
        machine.StateChanged += state => stateChanges.Add(state);

        // Act - Run should halt immediately on STP
        machine.Run();

        // Assert - Should have transitioned to Running then to Stopped
        Assert.That(
            stateChanges,
            Contains.Item(MachineState.Running),
            "Should have transitioned to Running state");
    }

    /// <summary>
    /// Verifies that components can be added and retrieved from the machine.
    /// </summary>
    [Test]
    public void GetComponent_ReturnsRegisteredComponents()
    {
        // Arrange
        var testComponent = new TestComponent("Test");
        var (ram, ramTarget) = Create64KRam();

        // Act
        var machine = CreateMinimalMachineBuilder()
            .MapRegion(0x0000, 64 * 1024, ramTarget, RegionTag.Ram, PagePerms.All)
            .AddComponent(testComponent)
            .Build();

        // Assert
        var retrieved = machine.GetComponent<TestComponent>();
        Assert.That(retrieved, Is.SameAs(testComponent), "Should retrieve added component");
    }

    /// <summary>
    /// Verifies that factorial calculation works with the full machine.
    /// This is a comprehensive integration test using the factorial.bin sample.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The factorial.bin program calculates 5! = 120 (0x78).
    /// The program is designed to run at $1000 and stores the result at $0010.
    /// </para>
    /// </remarks>
    [Test]
    public void FactorialProgram_ComputesCorrectResult()
    {
        // Arrange - Create ROM with reset vector pointing to $1000
        var rom = new byte[16384];
        Array.Fill(rom, (byte)0xEA);

        // Set reset vector to $1000 (where factorial.bin is designed to run)
        rom[0x3FFA] = 0x00;
        rom[0x3FFB] = 0xFF;
        rom[0x3FFC] = 0x00; // RESET low byte -> $1000
        rom[0x3FFD] = 0x10; // RESET high byte
        rom[0x3FFE] = 0x00;
        rom[0x3FFF] = 0xFF;

        var (ram, ramTarget) = Create64KRam();

        var machine = CreateMinimalMachineBuilder()
            .MapRegion(0x0000, 64 * 1024, ramTarget, RegionTag.Ram, PagePerms.All)
            .WithRom(rom, 0xC000, "ROM")
            .Build();

        // Factorial code designed to run at $1000
        // Computes 5! = 120 (0x78), stores result at $0010, then halts with STP
        byte[] factorialBin =
        [
            0xA9, 0x01,       // $1000: LDA #$01      ; result = 1
            0x85, 0x10,       // $1002: STA $10       ; store result at $10
            0xA9, 0x05,       // $1004: LDA #$05      ; counter = 5
            0x85, 0x12,       // $1006: STA $12       ; store counter at $12

            // Loop start at $1008
            0xA5, 0x12,       // $1008: LDA $12       ; load counter
            0xF0, 0x12,       // $100A: BEQ $101E     ; if zero, done (jump +18)
            0x85, 0x11,       // $100C: STA $11       ; multiplier = counter
            0xA9, 0x00,       // $100E: LDA #$00      ; product = 0

            // Multiply loop at $1010
            0x18,             // $1010: CLC
            0x65, 0x10,       // $1011: ADC $10       ; product += result
            0xC6, 0x11,       // $1013: DEC $11       ; multiplier--
            0xD0, 0xF9,       // $1015: BNE $1010     ; loop (-7)
            0x85, 0x10,       // $1017: STA $10       ; store new result
            0xC6, 0x12,       // $1019: DEC $12       ; counter--
            0x4C, 0x08, 0x10, // $101B: JMP $1008     ; back to loop start

            // Done at $101E
            0xA5, 0x10,       // $101E: LDA $10       ; load result
            0xDB,             // $1020: STP           ; halt CPU
        ];

        // Load factorial code into RAM at $1000
        for (int i = 0; i < factorialBin.Length; i++)
        {
            machine.Cpu.Write8((ushort)(0x1000 + i), factorialBin[i]);
        }

        // Reset to load PC from reset vector ($1000)
        machine.Reset();

        // Verify PC is at $1000
        Assert.That(
            machine.Cpu.GetPC(),
            Is.EqualTo(0x1000),
            "PC should be at factorial code start ($1000)");

        // Act - Run until halted (STP instruction) or max cycles
        int maxInstructions = 10000;
        int instructionCount = 0;
        CpuStepResult result;

        do
        {
            result = machine.Step();
            instructionCount++;
        }
        while (result.State == CpuRunState.Running && instructionCount < maxInstructions);

        // Assert
        Assert.Multiple(() =>
        {
            // STP instruction may return Halted or Stopped depending on implementation
            Assert.That(
                result.State == CpuRunState.Halted || result.State == CpuRunState.Stopped,
                Is.True,
                $"CPU should have halted. State was {result.State}, executed {instructionCount} instructions.");

            // Read result from $0010
            var factorialResult = machine.Cpu.Read8(0x0010);
            Assert.That(
                factorialResult,
                Is.EqualTo(0x78),
                "Factorial result should be 120 (0x78)");
        });
    }

    /// <summary>
    /// Verifies that the machine can be stopped during execution.
    /// </summary>
    [Test]
    public void Stop_HaltsExecution()
    {
        // Arrange
        var (ram, ramTarget) = Create64KRam();
        var rom = CreateStubRom();

        var machine = CreateMinimalMachineBuilder()
            .MapRegion(0x0000, 64 * 1024, ramTarget, RegionTag.Ram, PagePerms.All)
            .WithRom(rom, 0xC000, "ROM")
            .Build();

        machine.Reset();

        // Act
        machine.Stop();

        // Assert
        Assert.That(
            machine.State,
            Is.EqualTo(MachineState.Stopped),
            "Machine should be in Stopped state");
    }

    /// <summary>
    /// Verifies that memory read/write works through the CPU.
    /// </summary>
    [Test]
    public void MemoryReadWrite_WorksCorrectly()
    {
        // Arrange
        var (ram, ramTarget) = Create64KRam();
        var rom = CreateStubRom();

        var machine = CreateMinimalMachineBuilder()
            .MapRegion(0x0000, 64 * 1024, ramTarget, RegionTag.Ram, PagePerms.All)
            .WithRom(rom, 0xC000, "ROM")
            .Build();

        // Act - Write to zero page
        machine.Cpu.Write8(0x0050, 0x42);

        // Assert - Read back
        var result = machine.Cpu.Read8(0x0050);
        Assert.That(result, Is.EqualTo(0x42), "Should read back written value");
    }

    /// <summary>
    /// Verifies that ROM is read-only.
    /// </summary>
    [Test]
    public void Rom_IsReadOnly()
    {
        // Arrange
        var (ram, ramTarget) = Create64KRam();
        var rom = CreateStubRom();

        var machine = CreateMinimalMachineBuilder()
            .MapRegion(0x0000, 64 * 1024, ramTarget, RegionTag.Ram, PagePerms.All)
            .WithRom(rom, 0xC000, "ROM")
            .Build();

        // Read original value from ROM
        var originalValue = machine.Cpu.Read8(0xC000);

        // Act - Attempt to write to ROM
        machine.Cpu.Write8(0xC000, 0xFF);

        // Assert - ROM should be unchanged
        var afterWrite = machine.Cpu.Read8(0xC000);
        Assert.That(
            afterWrite,
            Is.EqualTo(originalValue),
            "ROM should be read-only");
    }

    /// <summary>
    /// Verifies that the scheduler integrates with the machine correctly.
    /// </summary>
    [Test]
    public void Scheduler_AdvancesCycles()
    {
        // Arrange
        var (ram, ramTarget) = Create64KRam();
        var rom = CreateStubRom();

        var machine = CreateMinimalMachineBuilder()
            .MapRegion(0x0000, 64 * 1024, ramTarget, RegionTag.Ram, PagePerms.All)
            .WithRom(rom, 0xC000, "ROM")
            .Build();

        machine.Reset();
        var initialCycle = machine.Now;

        // Act - Execute some instructions
        for (int i = 0; i < 10; i++)
        {
            machine.Step();
        }

        // Assert - Cycles should have advanced
        Assert.That(
            machine.Now,
            Is.GreaterThan(initialCycle),
            "Cycle counter should advance as instructions execute");
    }

    /// <summary>
    /// Verifies that HasComponent correctly reports component presence.
    /// </summary>
    [Test]
    public void HasComponent_ReportsCorrectly()
    {
        // Arrange
        var (ram, ramTarget) = Create64KRam();
        var testComponent = new TestComponent("Test");

        // Act
        var machine = CreateMinimalMachineBuilder()
            .MapRegion(0x0000, 64 * 1024, ramTarget, RegionTag.Ram, PagePerms.All)
            .AddComponent(testComponent)
            .Build();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(
                machine.HasComponent<TestComponent>(),
                Is.True,
                "Should have TestComponent");
            Assert.That(
                machine.HasComponent<string>(),
                Is.False,
                "Should not have string component");
        });
    }

    /// <summary>
    /// Verifies that GetComponents returns all matching components.
    /// </summary>
    [Test]
    public void GetComponents_ReturnsAllMatching()
    {
        // Arrange
        var (ram, ramTarget) = Create64KRam();

        // Act
        var machine = CreateMinimalMachineBuilder()
            .MapRegion(0x0000, 64 * 1024, ramTarget, RegionTag.Ram, PagePerms.All)
            .AddComponent("Component1")
            .AddComponent("Component2")
            .Build();

        // Assert
        var strings = machine.GetComponents<string>().ToList();
        Assert.That(strings, Has.Count.EqualTo(2), "Should have two string components");
        Assert.That(strings, Contains.Item("Component1"));
        Assert.That(strings, Contains.Item("Component2"));
    }

    /// <summary>
    /// Verifies that devices registry contains registered devices.
    /// </summary>
    [Test]
    public void DeviceRegistry_ContainsDevices()
    {
        // Arrange
        var (ram, ramTarget) = Create64KRam();

        // Act
        var machine = CreateMinimalMachineBuilder()
            .MapRegion(0x0000, 64 * 1024, ramTarget, RegionTag.Ram, PagePerms.All)
            .Build();

        // Assert - Device registry should exist
        Assert.That(machine.Devices, Is.Not.Null, "Device registry should exist");
    }

    /// <summary>
    /// Verifies that building without a CPU factory throws an appropriate error.
    /// </summary>
    [Test]
    public void Build_WithoutCpuFactory_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new MachineBuilder()
            .WithAddressSpace(16)
            .WithCpu(CpuFamily.Cpu65C02);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.That(
            ex!.Message,
            Does.Contain("CPU factory"),
            "Error should mention CPU factory requirement");
    }

    /// <summary>
    /// Verifies that AfterBuild callbacks are invoked.
    /// </summary>
    [Test]
    public void AfterBuild_CallbacksAreInvoked()
    {
        // Arrange
        var (ram, ramTarget) = Create64KRam();
        var callbackInvoked = false;
        IMachine? machineFromCallback = null;

        // Act
        var machine = CreateMinimalMachineBuilder()
            .MapRegion(0x0000, 64 * 1024, ramTarget, RegionTag.Ram, PagePerms.All)
            .AfterBuild(m =>
            {
                callbackInvoked = true;
                machineFromCallback = m;
            })
            .Build();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(callbackInvoked, Is.True, "AfterBuild callback should be invoked");
            Assert.That(
                machineFromCallback,
                Is.SameAs(machine),
                "Callback should receive the built machine");
        });
    }

    /// <summary>
    /// Verifies that multiple AfterBuild callbacks are invoked in order.
    /// </summary>
    [Test]
    public void AfterBuild_MultipleCallbacks_InvokedInOrder()
    {
        // Arrange
        var (ram, ramTarget) = Create64KRam();
        var order = new List<int>();

        // Act
        var machine = CreateMinimalMachineBuilder()
            .MapRegion(0x0000, 64 * 1024, ramTarget, RegionTag.Ram, PagePerms.All)
            .AfterBuild(_ => order.Add(1))
            .AfterBuild(_ => order.Add(2))
            .AfterBuild(_ => order.Add(3))
            .Build();

        // Assert
        Assert.That(order, Is.EqualTo(new[] { 1, 2, 3 }), "Callbacks should be invoked in order");
    }

    /// <summary>
    /// Creates a minimal 64KB RAM machine with CPU factory.
    /// </summary>
    private static MachineBuilder CreateMinimalMachineBuilder()
    {
        return new MachineBuilder()
            .WithAddressSpace(16)
            .WithCpu(CpuFamily.Cpu65C02)
            .WithCpuFactory(ctx => new Cpu65C02(ctx));
    }

    /// <summary>
    /// Creates a 64KB RAM region.
    /// </summary>
    private static (PhysicalMemory Memory, RamTarget Target) Create64KRam()
    {
        var memory = new PhysicalMemory(64 * 1024, "MainRAM");
        var target = new RamTarget(memory.Slice(0, 64 * 1024));
        return (memory, target);
    }

    /// <summary>
    /// Creates a stub ROM with vectors at $FFFA-$FFFF pointing to $FF00,
    /// and a NOP sled at $FF00 that jumps to itself.
    /// </summary>
    private static byte[] CreateStubRom()
    {
        // Create 16KB ROM starting at $C000
        var rom = new byte[16384];
        Array.Fill(rom, (byte)0xEA); // NOP fill

        // Put JMP $FF00 at $FF00 (offset $3F00 in the ROM)
        rom[0x3F00] = 0x4C; // JMP
        rom[0x3F01] = 0x00; // low byte
        rom[0x3F02] = 0xFF; // high byte

        // Set up vectors at the end of ROM ($FFFA-$FFFF = offsets $3FFA-$3FFF)
        // NMI, RESET, IRQ -> $FF00
        rom[0x3FFA] = 0x00;
        rom[0x3FFB] = 0xFF;
        rom[0x3FFC] = 0x00;
        rom[0x3FFD] = 0xFF;
        rom[0x3FFE] = 0x00;
        rom[0x3FFF] = 0xFF;

        return rom;
    }

    /// <summary>
    /// Test component for component bucket testing.
    /// </summary>
    /// <param name="Name">The component name.</param>
    private sealed record TestComponent(string Name);

    // ─── TrapRegistry Build Order Tests ─────────────────────────────────────────

    /// <summary>
    /// Verifies that the <see cref="MachineBuilder"/> automatically creates and registers
    /// a <see cref="ITrapRegistry"/> component during <see cref="MachineBuilder.Build"/>.
    /// </summary>
    [Test]
    public void Build_CreatesAndRegistersTrapRegistryComponent()
    {
        var (_, ramTarget) = Create64KRam();
        var rom = CreateStubRom();

        var machine = CreateMinimalMachineBuilder()
            .MapRegion(0x0000, 64 * 1024, ramTarget, RegionTag.Ram, PagePerms.All)
            .WithRom(rom, 0xC000, "ROM")
            .Build();

        var trapRegistry = machine.GetComponent<ITrapRegistry>();

        Assert.Multiple(() =>
        {
            Assert.That(trapRegistry, Is.Not.Null,
                "MachineBuilder.Build() should create a TrapRegistry component");
            Assert.That(trapRegistry, Is.InstanceOf<TrapRegistry>(),
                "Component should be a real TrapRegistry instance");
        });
    }

    /// <summary>
    /// Verifies that the <see cref="ITrapRegistry"/> is available as a component
    /// before devices are initialized, so peripherals can register traps during
    /// their initialization phase.
    /// </summary>
    [Test]
    public void Build_TrapRegistryAvailableBeforeDeviceInit()
    {
        var (_, ramTarget) = Create64KRam();
        var rom = CreateStubRom();
        ITrapRegistry? registryDuringDeviceInit = null;

        var machine = CreateMinimalMachineBuilder()
            .MapRegion(0x0000, 64 * 1024, ramTarget, RegionTag.Ram, PagePerms.All)
            .WithRom(rom, 0xC000, "ROM")
            .BeforeDeviceInit(m =>
            {
                // This callback runs just before devices are initialized.
                // The TrapRegistry should already be available.
                registryDuringDeviceInit = m.GetComponent<ITrapRegistry>();
            })
            .Build();

        Assert.That(registryDuringDeviceInit, Is.Not.Null,
            "TrapRegistry should be available before device initialization");
        Assert.That(registryDuringDeviceInit, Is.SameAs(machine.GetComponent<ITrapRegistry>()),
            "Registry during device init should be the same instance as final component");
    }
}