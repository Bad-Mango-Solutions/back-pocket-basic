// <copyright file="ProfileCommandIntegrationTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core.Configuration;
using BadMango.Emulator.Debug.Infrastructure.Commands;
using BadMango.Emulator.Emulation.Cpu;
using BadMango.Emulator.Emulation.Debugging;
using BadMango.Emulator.Systems;

/// <summary>
/// End-to-end integration tests for debug commands with profile-based machines.
/// </summary>
/// <remarks>
/// <para>
/// These tests validate that commands like devicemap, pages, and regions work correctly
/// when using actual machine profiles, particularly the pocket2e profiles which use
/// composite handlers and have complex memory configurations.
/// </para>
/// </remarks>
[TestFixture]
public class ProfileCommandIntegrationTests
{
    private CommandDispatcher dispatcher = null!;
    private StringWriter outputWriter = null!;
    private StringWriter errorWriter = null!;

    /// <summary>
    /// Sets up test fixtures before each test.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        dispatcher = new CommandDispatcher();
        outputWriter = new StringWriter();
        errorWriter = new StringWriter();
    }

    /// <summary>
    /// Cleans up after each test.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        outputWriter.Dispose();
        errorWriter.Dispose();
    }

    /// <summary>
    /// Verifies that CreateDebugSystem returns a complete machine with all components.
    /// </summary>
    [Test]
    public void CreateDebugSystem_Simple65C02_ReturnsCompleteMachine()
    {
        var profile = CreateSimple65C02Profile();

        var (machine, disassembler, info) = MachineFactory.CreateDebugSystem(profile);

        Assert.Multiple(() =>
        {
            Assert.That(machine, Is.Not.Null, "Machine should be created");
            Assert.That(machine.Cpu, Is.Not.Null, "CPU should be available");
            Assert.That(machine.Bus, Is.Not.Null, "Bus should be available");
            Assert.That(machine.Devices, Is.Not.Null, "Device registry should be available");
            Assert.That(disassembler, Is.Not.Null, "Disassembler should be created");
            Assert.That(info, Is.Not.Null, "Machine info should be created");
        });
    }

    /// <summary>
    /// Verifies that devicemap command shows devices when machine is properly attached.
    /// </summary>
    [Test]
    public void DeviceMapCommand_WithMachine_ShowsDevices()
    {
        var profile = CreateSimple65C02Profile();
        var (machine, disassembler, info) = MachineFactory.CreateDebugSystem(profile);

        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        var tracingListener = new TracingDebugListener();
        machine.Cpu.AttachDebugger(tracingListener);
        context.AttachMachine(machine, disassembler, info, tracingListener);

        var command = new DeviceMapCommand();
        var result = command.Execute(context, []);

        var output = outputWriter.ToString();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, $"Command should succeed. Error: {result.Message}");
            Assert.That(output, Does.Contain("Device Registry"), "Output should contain Device Registry header");
            Assert.That(output, Does.Contain("RAM"), "Output should show RAM device");
            Assert.That(context.Machine, Is.Not.Null, "Machine should be attached to context");
            Assert.That(context.Machine?.Devices.Count, Is.GreaterThan(0), "Device registry should have devices");
        });
    }

    /// <summary>
    /// Verifies that regions command shows memory regions when bus is attached.
    /// </summary>
    [Test]
    public void RegionsCommand_WithMachine_ShowsRegions()
    {
        var profile = CreateSimple65C02Profile();
        var (machine, disassembler, info) = MachineFactory.CreateDebugSystem(profile);

        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        var tracingListener = new TracingDebugListener();
        machine.Cpu.AttachDebugger(tracingListener);
        context.AttachMachine(machine, disassembler, info, tracingListener);

        var command = new RegionsCommand();
        var result = command.Execute(context, []);

        var output = outputWriter.ToString();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, $"Command should succeed. Error: {result.Message}");
            Assert.That(output, Does.Contain("Memory Regions"), "Output should contain Memory Regions header");
            Assert.That(output, Does.Contain("Ram"), "Output should show RAM region");
            Assert.That(output, Does.Contain("$0000"), "Output should show starting address");
        });
    }

    /// <summary>
    /// Verifies that pages command shows page table when bus is attached.
    /// </summary>
    [Test]
    public void PagesCommand_WithMachine_ShowsPageTable()
    {
        var profile = CreateSimple65C02Profile();
        var (machine, disassembler, info) = MachineFactory.CreateDebugSystem(profile);

        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        var tracingListener = new TracingDebugListener();
        machine.Cpu.AttachDebugger(tracingListener);
        context.AttachMachine(machine, disassembler, info, tracingListener);

        var command = new PagesCommand();
        var result = command.Execute(context, []);

        var output = outputWriter.ToString();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, $"Command should succeed. Error: {result.Message}");
            Assert.That(output, Does.Contain("Page Table"), "Output should contain Page Table header");
            Assert.That(output, Does.Contain("VirtAddr"), "Output should show virtual address column");
        });
    }

    /// <summary>
    /// Verifies that Pocket2e-style machine with composite handlers creates correct device registry.
    /// </summary>
    [Test]
    public void AsPocket2e_WithStubRom_DeviceRegistryHasExpectedDevices()
    {
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        var devices = machine.Devices.GetAll().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(devices.Count, Is.GreaterThan(0), "Should have devices registered");
            Assert.That(devices.Any(d => d.Kind == "RAM"), Is.True, "Should have RAM device");
        });
    }

    /// <summary>
    /// Verifies that Pocket2e machine reports correct memory regions at power-on.
    /// </summary>
    [Test]
    public void AsPocket2e_PowerOn_RegionsShowCorrectTypes()
    {
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        var opcodeTable = Cpu65C02OpcodeTableBuilder.Build();
        var disassembler = new Disassembler(opcodeTable, machine.Bus);
        var info = new MachineInfo("pocket2e", "Pocket2e", "65C02", 128 * 1024);

        // Note: TracingListener is optional for these tests since we're not tracing execution
        context.AttachMachine(machine, disassembler, info);

        var command = new RegionsCommand();
        var result = command.Execute(context, []);

        var output = outputWriter.ToString();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, $"Command should succeed. Error: {result.Message}");
            Assert.That(output, Does.Contain("Memory Regions"), "Output should contain Memory Regions header");
            Assert.That(output, Does.Contain("Ram"), "Output should show RAM region");
            Assert.That(output, Does.Contain("$0000"), "Output should show main RAM starting address");
        });
    }

    /// <summary>
    /// Verifies that Pocket2e pages command shows I/O page with expected capabilities.
    /// </summary>
    [Test]
    public void AsPocket2e_PagesCommand_ShowsIOPageWithCapabilities()
    {
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        var opcodeTable = Cpu65C02OpcodeTableBuilder.Build();
        var disassembler = new Disassembler(opcodeTable, machine.Bus);
        var info = new MachineInfo("pocket2e", "Pocket2e", "65C02", 128 * 1024);

        // Note: TracingListener is optional for these tests since we're not tracing execution
        context.AttachMachine(machine, disassembler, info);

        // Get pages at $C0xx (page 12 decimal = 0x0C)
        var command = new PagesCommand();
        var result = command.Execute(context, ["$0C", "4"]);

        var output = outputWriter.ToString();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, $"Command should succeed. Error: {result.Message}");
            Assert.That(output, Does.Contain("Page Table"), "Output should contain Page Table header");
            Assert.That(output, Does.Contain("$C000"), "Output should show I/O page address");
            Assert.That(output, Does.Contain("Io"), "Output should show I/O region type");
        });
    }

    /// <summary>
    /// Verifies that devicemap shows devices after attaching machine.
    /// </summary>
    [Test]
    public void DeviceMapCommand_AsPocket2e_ShowsAllDevices()
    {
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        var opcodeTable = Cpu65C02OpcodeTableBuilder.Build();
        var disassembler = new Disassembler(opcodeTable, machine.Bus);
        var info = new MachineInfo("pocket2e", "Pocket2e", "65C02", 128 * 1024);

        // Note: TracingListener is optional for these tests since we're not tracing execution
        context.AttachMachine(machine, disassembler, info);

        var command = new DeviceMapCommand();
        var result = command.Execute(context, []);

        var output = outputWriter.ToString();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, $"Command should succeed. Error: {result.Message}");
            Assert.That(output, Does.Contain("Device Registry"), "Output should contain Device Registry header");
            Assert.That(output, Does.Contain("Total devices:"), "Output should show total device count");

            // Verify devices exist
            Assert.That(context.Machine, Is.Not.Null, "Machine should be attached");
            Assert.That(context.Machine!.Devices.Count, Is.GreaterThan(0), "Should have registered devices");
        });
    }

    /// <summary>
    /// Verifies that RAM pages support Peek and Poke capabilities.
    /// </summary>
    [Test]
    public void PagesCommand_RamPages_ShowPeekPokeCapabilities()
    {
        var profile = CreateSimple65C02Profile();
        var (machine, disassembler, info) = MachineFactory.CreateDebugSystem(profile);

        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        var tracingListener = new TracingDebugListener();
        machine.Cpu.AttachDebugger(tracingListener);
        context.AttachMachine(machine, disassembler, info, tracingListener);

        var command = new PagesCommand();
        var result = command.Execute(context, []);

        var output = outputWriter.ToString();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, $"Command should succeed. Error: {result.Message}");
            Assert.That(output, Does.Contain("Pk"), "RAM pages should support Peek");
            Assert.That(output, Does.Contain("Po"), "RAM pages should support Poke");
        });
    }

    /// <summary>
    /// Verifies that CreateDebugSystem properly resets the machine.
    /// </summary>
    [Test]
    public void CreateDebugSystem_MachineIsReset()
    {
        var profile = CreateSimple65C02Profile();

        var (machine, _, _) = MachineFactory.CreateDebugSystem(profile);

        // CPU should be at reset vector location
        Assert.That(machine.State, Is.EqualTo(MachineState.Stopped), "Machine should be in stopped state");
    }

    /// <summary>
    /// Creates a simple 65C02 profile for testing.
    /// </summary>
    /// <returns>A simple 65C02 machine profile.</returns>
    private static MachineProfile CreateSimple65C02Profile()
    {
        return new MachineProfile
        {
            Name = "test-65c02",
            DisplayName = "Test 65C02 System",
            Cpu = new CpuProfileSection
            {
                Type = "65C02",
                ClockSpeed = 1000000,
            },
            AddressSpace = 16,
            Memory = new MemoryProfileSection
            {
                Regions =
                [
                    new MemoryRegionProfile
                    {
                        Name = "main-ram",
                        Type = "ram",
                        Start = "0x0000",
                        Size = "0x10000",
                        Permissions = "rwx",
                    },
                ],
            },
        };
    }
}