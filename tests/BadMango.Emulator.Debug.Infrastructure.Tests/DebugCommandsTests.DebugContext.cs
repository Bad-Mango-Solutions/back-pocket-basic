// <copyright file="DebugCommandsTests.DebugContext.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core.Configuration;

using Moq;

/// <summary>
/// Unit tests for <see cref="DebugContext"/>.
/// </summary>
public partial class DebugCommandsTests
{
    /// <summary>
    /// Verifies that DebugContext reports system attached correctly.
    /// </summary>
    [Test]
    public void DebugContext_ReportsSystemAttached_WhenAllComponentsPresent()
    {
        Assert.That(debugContext.IsSystemAttached, Is.True);
    }

    /// <summary>
    /// Verifies that DebugContext reports system not attached when CPU missing.
    /// </summary>
    [Test]
    public void DebugContext_ReportsNotAttached_WhenCpuMissing()
    {
        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        Assert.That(context.IsSystemAttached, Is.False);
    }

    /// <summary>
    /// Verifies that DebugContext can attach components dynamically.
    /// </summary>
    [Test]
    public void DebugContext_CanAttachComponentsDynamically()
    {
        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        Assert.That(context.IsSystemAttached, Is.False);

        context.AttachSystem(cpu, bus, disassembler);
        Assert.That(context.IsSystemAttached, Is.True);
    }

    /// <summary>
    /// Verifies that DebugContext can detach components.
    /// </summary>
    [Test]
    public void DebugContext_CanDetachComponents()
    {
        debugContext.DetachSystem();
        Assert.That(debugContext.IsSystemAttached, Is.False);
    }

    /// <summary>
    /// Verifies that IsBusAttached is false when no bus is attached.
    /// </summary>
    [Test]
    public void DebugContext_IsBusAttached_IsFalse_WhenNoBusAttached()
    {
        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        Assert.That(context.IsBusAttached, Is.False);
    }

    /// <summary>
    /// Verifies that IsBusAttached is true when a bus is attached.
    /// </summary>
    [Test]
    public void DebugContext_IsBusAttached_IsTrue_WhenBusAttached()
    {
        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        var mockBus = new Mock<IMemoryBus>();
        context.AttachBus(mockBus.Object);
        Assert.That(context.IsBusAttached, Is.True);
    }

    /// <summary>
    /// Verifies that Bus property is null when no bus is attached.
    /// </summary>
    [Test]
    public void DebugContext_Bus_IsNull_WhenNoBusAttached()
    {
        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        Assert.That(context.Bus, Is.Null);
    }

    /// <summary>
    /// Verifies that AttachBus correctly sets the bus property.
    /// </summary>
    [Test]
    public void DebugContext_AttachBus_SetsBusProperty()
    {
        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        var mockBus = new Mock<IMemoryBus>();
        context.AttachBus(mockBus.Object);
        Assert.That(context.Bus, Is.SameAs(mockBus.Object));
    }

    /// <summary>
    /// Verifies that AttachBus throws ArgumentNullException when bus is null.
    /// </summary>
    [Test]
    public void DebugContext_AttachBus_ThrowsArgumentNullException_WhenBusIsNull()
    {
        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        Assert.Throws<ArgumentNullException>(() => context.AttachBus(null!));
    }

    /// <summary>
    /// Verifies that Machine property is null when no machine is attached.
    /// </summary>
    [Test]
    public void DebugContext_Machine_IsNull_WhenNoMachineAttached()
    {
        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        Assert.That(context.Machine, Is.Null);
    }

    /// <summary>
    /// Verifies that AttachMachine correctly sets the machine property.
    /// </summary>
    [Test]
    public void DebugContext_AttachMachine_SetsMachineProperty()
    {
        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        var mockBus = new Mock<IMemoryBus>();
        var mockMachine = new Mock<IMachine>();
        mockMachine.Setup(m => m.Cpu).Returns(cpu);
        mockMachine.Setup(m => m.Bus).Returns(mockBus.Object);
        context.AttachMachine(mockMachine.Object);
        Assert.That(context.Machine, Is.SameAs(mockMachine.Object));
    }

    /// <summary>
    /// Verifies that AttachMachine also sets CPU and Bus properties from the machine.
    /// </summary>
    [Test]
    public void DebugContext_AttachMachine_SetsCpuAndBusFromMachine()
    {
        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        var mockBus = new Mock<IMemoryBus>();
        var mockMachine = new Mock<IMachine>();
        mockMachine.Setup(m => m.Cpu).Returns(cpu);
        mockMachine.Setup(m => m.Bus).Returns(mockBus.Object);
        context.AttachMachine(mockMachine.Object);

        Assert.Multiple(() =>
        {
            Assert.That(context.Cpu, Is.SameAs(cpu));
            Assert.That(context.Bus, Is.SameAs(mockBus.Object));
            Assert.That(context.IsBusAttached, Is.True);
        });
    }

    /// <summary>
    /// Verifies that AttachMachine throws ArgumentNullException when machine is null.
    /// </summary>
    [Test]
    public void DebugContext_AttachMachine_ThrowsArgumentNullException_WhenMachineIsNull()
    {
        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        Assert.Throws<ArgumentNullException>(() => context.AttachMachine(null!));
    }

    /// <summary>
    /// Verifies that DetachSystem clears bus and machine properties.
    /// </summary>
    [Test]
    public void DebugContext_DetachSystem_ClearsBusAndMachine()
    {
        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        var mockBus = new Mock<IMemoryBus>();
        var mockMachine = new Mock<IMachine>();
        mockMachine.Setup(m => m.Cpu).Returns(cpu);
        mockMachine.Setup(m => m.Bus).Returns(mockBus.Object);
        context.AttachMachine(mockMachine.Object);

        context.DetachSystem();

        Assert.Multiple(() =>
        {
            Assert.That(context.Bus, Is.Null);
            Assert.That(context.Machine, Is.Null);
            Assert.That(context.IsBusAttached, Is.False);
        });
    }

    /// <summary>
    /// Verifies that AttachBus creates MemoryBusAdapter as Memory property for backward compatibility.
    /// </summary>
    [Test]
    public void DebugContext_AttachBus_SetsUpBusProperty()
    {
        var testBus = CreateBusWithRam();
        debugContext.AttachBus(testBus);

        Assert.Multiple(() =>
        {
            Assert.That(debugContext.Bus, Is.Not.Null);
            Assert.That(debugContext.Bus, Is.SameAs(testBus));
            Assert.That(debugContext.IsBusAttached, Is.True);
        });
    }

    /// <summary>
    /// Verifies that DebugContext can be created with full constructor.
    /// </summary>
    [Test]
    public void DebugContext_FullConstructor_SetsAllProperties()
    {
        var testBus = CreateBusWithRam();
        var context = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, testBus, disassembler);

        Assert.Multiple(() =>
        {
            Assert.That(context.Cpu, Is.SameAs(cpu));
            Assert.That(context.Bus, Is.SameAs(testBus));
            Assert.That(context.Disassembler, Is.SameAs(disassembler));
            Assert.That(context.IsSystemAttached, Is.True);
            Assert.That(context.IsBusAttached, Is.True);
        });
    }

    /// <summary>
    /// Verifies that IsBusAttached returns true when bus attached via AttachSystem.
    /// </summary>
    [Test]
    public void DebugContext_IsBusAttached_True_WhenAttachedViaAttachSystem()
    {
        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        var testBus = CreateBusWithRam();
        context.AttachSystem(cpu, testBus, disassembler);

        Assert.Multiple(() =>
        {
            Assert.That(context.IsBusAttached, Is.True);
            Assert.That(context.Bus, Is.SameAs(testBus));
        });
    }

    /// <summary>
    /// Verifies that AttachMachine with full signature attaches all debug components.
    /// </summary>
    [Test]
    public void DebugContext_AttachMachine_WithFullSignature_SetsAllComponents()
    {
        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        var testBus = CreateBusWithRam();
        var mockMachine = new Mock<IMachine>();
        mockMachine.Setup(m => m.Cpu).Returns(cpu);
        mockMachine.Setup(m => m.Bus).Returns(testBus);

        var machineInfo = new MachineInfo("Test", "Test Machine", "65C02", 64 * 1024);

        context.AttachMachine(mockMachine.Object, disassembler, machineInfo);

        Assert.Multiple(() =>
        {
            Assert.That(context.Machine, Is.SameAs(mockMachine.Object));
            Assert.That(context.Cpu, Is.SameAs(cpu));
            Assert.That(context.Bus, Is.SameAs(testBus));
            Assert.That(context.Disassembler, Is.SameAs(disassembler));
            Assert.That(context.MachineInfo, Is.SameAs(machineInfo));
            Assert.That(context.IsSystemAttached, Is.True);
            Assert.That(context.IsBusAttached, Is.True);
        });
    }

    /// <summary>
    /// Verifies that AttachSystem with bus, machine info, and tracing listener works correctly.
    /// </summary>
    [Test]
    public void DebugContext_AttachSystemWithBusAndTracingListener_WorksCorrectly()
    {
        var testBus = CreateBusWithRam();
        var machineInfo = new MachineInfo("TestMachine", "Test Machine", "65C02", 65536);
        var tracingListener = new TracingDebugListener();
        var context = new DebugContext(dispatcher, outputWriter, errorWriter);

        context.AttachSystem(cpu, testBus, disassembler, machineInfo, tracingListener);

        Assert.Multiple(() =>
        {
            Assert.That(context.Cpu, Is.SameAs(cpu));
            Assert.That(context.Bus, Is.SameAs(testBus));
            Assert.That(context.Disassembler, Is.SameAs(disassembler));
            Assert.That(context.MachineInfo, Is.SameAs(machineInfo));
            Assert.That(context.TracingListener, Is.SameAs(tracingListener));
            Assert.That(context.IsSystemAttached, Is.True);
            Assert.That(context.IsBusAttached, Is.True);
        });
    }

    /// <summary>
    /// Verifies that bus-based memory access patterns work.
    /// </summary>
    [Test]
    public void DebugContext_WithBus_DirectBusAccessWorks()
    {
        // Write to physical memory
        var testBus = CreateBusWithRam(out var physicalMemory);
        physicalMemory.AsSpan()[0x100] = 0x42;

        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        context.AttachSystem(cpu, testBus, disassembler);

        // Read through the Bus interface
        byte value = ReadByte(context.Bus!, 0x100);
        Assert.That(value, Is.EqualTo(0x42));

        // Write through the Bus interface
        WriteByte(context.Bus!, 0x200, 0xAB);
        Assert.That(physicalMemory.AsSpan()[0x200], Is.EqualTo(0xAB));
    }

    /// <summary>
    /// Verifies that MemCommand works with bus-based system.
    /// </summary>
    [Test]
    public void MemCommand_WorksWithBusBasedSystem()
    {
        var testBus = CreateBusWithRam(out var physicalMemory);
        physicalMemory.AsSpan()[0x200] = 0x41; // 'A'
        physicalMemory.AsSpan()[0x201] = 0x42; // 'B'
        physicalMemory.AsSpan()[0x202] = 0x43; // 'C'

        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        context.AttachSystem(cpu, testBus, disassembler);

        var command = new MemCommand();
        var result = command.Execute(context, ["$0200", "16"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("41"));
            Assert.That(outputWriter.ToString(), Does.Contain("42"));
            Assert.That(outputWriter.ToString(), Does.Contain("43"));
            Assert.That(outputWriter.ToString(), Does.Contain("ABC")); // ASCII
        });
    }

    /// <summary>
    /// Verifies that PokeCommand works with bus-based system.
    /// </summary>
    [Test]
    public void PokeCommand_WorksWithBusBasedSystem()
    {
        var testBus = CreateBusWithRam(out var physicalMemory);

        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        context.AttachSystem(cpu, testBus, disassembler);

        var command = new PokeCommand();
        var result = command.Execute(context, ["$0300", "$AB", "$CD", "$EF"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(physicalMemory.AsSpan()[0x300], Is.EqualTo(0xAB));
            Assert.That(physicalMemory.AsSpan()[0x301], Is.EqualTo(0xCD));
            Assert.That(physicalMemory.AsSpan()[0x302], Is.EqualTo(0xEF));
        });
    }
}