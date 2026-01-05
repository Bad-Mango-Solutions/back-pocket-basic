// <copyright file="MachineBuilderTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

using BadMango.Emulator.Core.Cpu;
using BadMango.Emulator.Core.Interfaces.Cpu;
using BadMango.Emulator.Core.Interfaces.Signaling;

using Interfaces;

using Moq;

/// <summary>
/// Unit tests for <see cref="MachineBuilder"/>.
/// </summary>
[TestFixture]
public class MachineBuilderTests
{
    /// <summary>
    /// Verifies that a basic machine can be built with default configuration.
    /// </summary>
    [Test]
    public void Build_WithDefaultConfiguration_CreatesMachine()
    {
        var mockCpu = CreateMockCpu();
        var builder = new MachineBuilder()
            .WithCpuFactory(_ => mockCpu.Object);

        var machine = builder.Build();

        Assert.Multiple(() =>
        {
            Assert.That(machine, Is.Not.Null);
            Assert.That(machine.Cpu, Is.SameAs(mockCpu.Object));
            Assert.That(machine.State, Is.EqualTo(MachineState.Stopped));
        });
    }

    /// <summary>
    /// Verifies that address space can be configured.
    /// </summary>
    [Test]
    public void WithAddressSpace_SetsAddressSpaceBits()
    {
        var mockCpu = CreateMockCpu();
        var builder = new MachineBuilder()
            .WithAddressSpace(24)
            .WithCpuFactory(_ => mockCpu.Object);

        var machine = builder.Build();

        // 24 bits = 16MB = 4096 pages of 4KB each
        Assert.That(machine.Bus.PageCount, Is.EqualTo(4096));
    }

    /// <summary>
    /// Verifies that invalid address space throws.
    /// </summary>
    [Test]
    public void WithAddressSpace_InvalidBits_ThrowsArgumentOutOfRangeException()
    {
        var builder = new MachineBuilder();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.WithAddressSpace(11));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.WithAddressSpace(33));
        });
    }

    /// <summary>
    /// Verifies that CPU family can be configured.
    /// </summary>
    [Test]
    public void WithCpu_SetsCpuFamily()
    {
        // This test verifies the configuration works, actual CPU creation
        // is tested via WithCpuFactory
        var mockCpu = CreateMockCpu();
        var builder = new MachineBuilder()
            .WithCpu(CpuFamily.Cpu65C02)
            .WithCpuFactory(_ => mockCpu.Object);

        var machine = builder.Build();

        Assert.That(machine.Cpu, Is.Not.Null);
    }

    /// <summary>
    /// Verifies that components can be added and retrieved.
    /// </summary>
    [Test]
    public void AddComponent_CanRetrieveComponent()
    {
        var mockCpu = CreateMockCpu();
        var testComponent = new TestComponent { Name = "Test" };

        var machine = new MachineBuilder()
            .WithCpuFactory(_ => mockCpu.Object)
            .AddComponent(testComponent)
            .Build();

        var retrieved = machine.GetComponent<TestComponent>();

        Assert.Multiple(() =>
        {
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved!.Name, Is.EqualTo("Test"));
        });
    }

    /// <summary>
    /// Verifies that multiple components can be retrieved.
    /// </summary>
    [Test]
    public void AddComponent_MultipleComponents_CanRetrieveAll()
    {
        var mockCpu = CreateMockCpu();
        var component1 = new TestComponent { Name = "First" };
        var component2 = new TestComponent { Name = "Second" };

        var machine = new MachineBuilder()
            .WithCpuFactory(_ => mockCpu.Object)
            .AddComponent(component1)
            .AddComponent(component2)
            .Build();

        var all = machine.GetComponents<TestComponent>().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(all, Has.Count.EqualTo(2));
            Assert.That(all[0].Name, Is.EqualTo("First"));
            Assert.That(all[1].Name, Is.EqualTo("Second"));
        });
    }

    /// <summary>
    /// Verifies HasComponent returns true when component exists.
    /// </summary>
    [Test]
    public void HasComponent_WhenExists_ReturnsTrue()
    {
        var mockCpu = CreateMockCpu();

        var machine = new MachineBuilder()
            .WithCpuFactory(_ => mockCpu.Object)
            .AddComponent(new TestComponent())
            .Build();

        Assert.That(machine.HasComponent<TestComponent>(), Is.True);
    }

    /// <summary>
    /// Verifies HasComponent returns false when component doesn't exist.
    /// </summary>
    [Test]
    public void HasComponent_WhenNotExists_ReturnsFalse()
    {
        var mockCpu = CreateMockCpu();

        var machine = new MachineBuilder()
            .WithCpuFactory(_ => mockCpu.Object)
            .Build();

        Assert.That(machine.HasComponent<TestComponent>(), Is.False);
    }

    /// <summary>
    /// Verifies that ROM can be added to the machine.
    /// </summary>
    [Test]
    public void WithRom_MapsRomToAddress()
    {
        var mockCpu = CreateMockCpu();
        var romData = new byte[4096];
        romData[0] = 0xEA; // NOP

        var machine = new MachineBuilder()
            .WithCpuFactory(_ => mockCpu.Object)
            .WithRom(romData, 0xF000, "Test ROM")
            .Build();

        // Verify ROM is mapped - read should succeed
        var access = new BusAccess(
            Address: 0xF000u,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: AccessIntent.DataRead,
            SourceId: 0,
            Cycle: 0,
            Flags: AccessFlags.None);
        var result = machine.Bus.TryRead8(access);

        Assert.Multiple(() =>
        {
            Assert.That(result.Ok, Is.True);
            Assert.That(result.Value, Is.EqualTo(0xEA));
        });
    }

    /// <summary>
    /// Verifies that multiple ROMs can be added.
    /// </summary>
    [Test]
    public void WithRoms_MapsMultipleRoms()
    {
        var mockCpu = CreateMockCpu();
        var rom1 = new byte[4096];
        rom1[0] = 0x01;
        var rom2 = new byte[4096];
        rom2[0] = 0x02;

        var machine = new MachineBuilder()
            .WithCpuFactory(_ => mockCpu.Object)
            .WithRoms(
                RomDescriptor.Custom(rom1, 0xE000, "ROM1"),
                RomDescriptor.Custom(rom2, 0xF000, "ROM2"))
            .Build();

        var access1 = new BusAccess(
            Address: 0xE000u,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: AccessIntent.DataRead,
            SourceId: 0,
            Cycle: 0,
            Flags: AccessFlags.None);
        var access2 = new BusAccess(
            Address: 0xF000u,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: AccessIntent.DataRead,
            SourceId: 0,
            Cycle: 0,
            Flags: AccessFlags.None);

        Assert.Multiple(() =>
        {
            Assert.That(machine.Bus.TryRead8(access1).Value, Is.EqualTo(0x01));
            Assert.That(machine.Bus.TryRead8(access2).Value, Is.EqualTo(0x02));
        });
    }

    /// <summary>
    /// Verifies that memory can be configured via callback.
    /// </summary>
    [Test]
    public void ConfigureMemory_CallbackIsInvoked()
    {
        var mockCpu = CreateMockCpu();
        var callbackInvoked = false;

        _ = new MachineBuilder()
            .WithCpuFactory(_ => mockCpu.Object)
            .ConfigureMemory((bus, devices) =>
            {
                callbackInvoked = true;
            })
            .Build();

        Assert.That(callbackInvoked, Is.True);
    }

    /// <summary>
    /// Verifies that layers can be created.
    /// </summary>
    /// <remarks>
    /// Layers are created but NOT auto-activated during build. This allows
    /// controlling devices (like LanguageCardController) to manage their
    /// layer's active state based on soft switch configuration.
    /// </remarks>
    [Test]
    public void CreateLayer_CreatesLayerWithPriority()
    {
        var mockCpu = CreateMockCpu();

        var machine = new MachineBuilder()
            .WithCpuFactory(_ => mockCpu.Object)
            .CreateLayer("TestLayer", 100)
            .Build();

        var layer = machine.Bus.GetLayer("TestLayer");

        Assert.Multiple(() =>
        {
            Assert.That(layer, Is.Not.Null);
            Assert.That(layer!.Value.Name, Is.EqualTo("TestLayer"));
            Assert.That(layer.Value.Priority, Is.EqualTo(100));
            Assert.That(layer.Value.IsActive, Is.False, "Layers are not auto-activated");
        });
    }

    /// <summary>
    /// Verifies that duplicate layer names throw.
    /// </summary>
    [Test]
    public void CreateLayer_DuplicateName_ThrowsArgumentException()
    {
        var builder = new MachineBuilder()
            .CreateLayer("Test", 1);

        Assert.Throws<ArgumentException>(() => builder.CreateLayer("Test", 2));
    }

    /// <summary>
    /// Verifies that scheduled devices are initialized.
    /// </summary>
    [Test]
    public void AddDevice_InitializesDeviceWithContext()
    {
        var mockCpu = CreateMockCpu();
        var mockDevice = new Mock<IScheduledDevice>();
        mockDevice.Setup(d => d.Name).Returns("TestDevice");

        _ = new MachineBuilder()
            .WithCpuFactory(_ => mockCpu.Object)
            .AddDevice(mockDevice.Object)
            .Build();

        mockDevice.Verify(d => d.Initialize(It.IsAny<IEventContext>()), Times.Once);
    }

    /// <summary>
    /// Verifies that null component throws.
    /// </summary>
    [Test]
    public void AddComponent_NullComponent_ThrowsArgumentNullException()
    {
        var builder = new MachineBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.AddComponent<object>(null!));
    }

    /// <summary>
    /// Verifies that null device throws.
    /// </summary>
    [Test]
    public void AddDevice_NullDevice_ThrowsArgumentNullException()
    {
        var builder = new MachineBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.AddDevice(null!));
    }

    /// <summary>
    /// Verifies that null CPU factory throws.
    /// </summary>
    [Test]
    public void WithCpuFactory_NullFactory_ThrowsArgumentNullException()
    {
        var builder = new MachineBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.WithCpuFactory(null!));
    }

    /// <summary>
    /// Verifies builder returns same instance for chaining.
    /// </summary>
    [Test]
    public void FluentMethods_ReturnSameBuilder()
    {
        var builder = new MachineBuilder();
        var mockCpu = CreateMockCpu();

        var result1 = builder.WithAddressSpace(16);
        var result2 = builder.WithCpu(CpuFamily.Cpu65C02);
        var result3 = builder.WithCpuFactory(_ => mockCpu.Object);
        var result4 = builder.AddComponent(new TestComponent());
        var result5 = builder.ConfigureMemory((_, _) => { });
        var result6 = builder.CreateLayer("Test", 1);

        Assert.Multiple(() =>
        {
            Assert.That(result1, Is.SameAs(builder));
            Assert.That(result2, Is.SameAs(builder));
            Assert.That(result3, Is.SameAs(builder));
            Assert.That(result4, Is.SameAs(builder));
            Assert.That(result5, Is.SameAs(builder));
            Assert.That(result6, Is.SameAs(builder));
        });
    }

    /// <summary>
    /// Verifies that AfterBuild callback is invoked after machine is built.
    /// </summary>
    [Test]
    public void AfterBuild_CallbackIsInvokedAfterBuild()
    {
        var mockCpu = CreateMockCpu();
        IMachine? receivedMachine = null;
        var callbackInvoked = false;

        var machine = new MachineBuilder()
            .WithCpuFactory(_ => mockCpu.Object)
            .AfterBuild(m =>
            {
                callbackInvoked = true;
                receivedMachine = m;
            })
            .Build();

        Assert.Multiple(() =>
        {
            Assert.That(callbackInvoked, Is.True);
            Assert.That(receivedMachine, Is.SameAs(machine));
        });
    }

    /// <summary>
    /// Verifies that multiple AfterBuild callbacks are invoked in order.
    /// </summary>
    [Test]
    public void AfterBuild_MultipleCallbacks_InvokedInOrder()
    {
        var mockCpu = CreateMockCpu();
        var invocationOrder = new List<int>();

        _ = new MachineBuilder()
            .WithCpuFactory(_ => mockCpu.Object)
            .AfterBuild(_ => invocationOrder.Add(1))
            .AfterBuild(_ => invocationOrder.Add(2))
            .AfterBuild(_ => invocationOrder.Add(3))
            .Build();

        Assert.That(invocationOrder, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    /// <summary>
    /// Verifies that AfterBuild null callback throws.
    /// </summary>
    [Test]
    public void AfterBuild_NullCallback_ThrowsArgumentNullException()
    {
        var builder = new MachineBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.AfterBuild(null!));
    }

    /// <summary>
    /// Verifies that AfterBuild callback can access components.
    /// </summary>
    [Test]
    public void AfterBuild_CallbackCanAccessComponents()
    {
        var mockCpu = CreateMockCpu();
        var testComponent = new TestComponent { Name = "TestValue" };
        TestComponent? retrievedComponent = null;

        _ = new MachineBuilder()
            .WithCpuFactory(_ => mockCpu.Object)
            .AddComponent(testComponent)
            .AfterBuild(m => retrievedComponent = m.GetComponent<TestComponent>())
            .Build();

        Assert.Multiple(() =>
        {
            Assert.That(retrievedComponent, Is.Not.Null);
            Assert.That(retrievedComponent!.Name, Is.EqualTo("TestValue"));
        });
    }

    /// <summary>
    /// Verifies that AfterDeviceInit callback is invoked after device initialization.
    /// </summary>
    [Test]
    public void AfterDeviceInit_CallbackIsInvokedAfterDeviceInit()
    {
        var mockCpu = CreateMockCpu();
        var callbackInvoked = false;

        _ = new MachineBuilder()
            .WithCpuFactory(_ => mockCpu.Object)
            .AfterDeviceInit(m =>
            {
                callbackInvoked = true;
            })
            .Build();

        Assert.That(callbackInvoked, Is.True);
    }

    /// <summary>
    /// Verifies that null AfterDeviceInit callback throws.
    /// </summary>
    [Test]
    public void AfterDeviceInit_NullCallback_ThrowsArgumentNullException()
    {
        var builder = new MachineBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.AfterDeviceInit(null!));
    }

    /// <summary>
    /// Verifies that BeforeSlotCardInstall callback is invoked before slot card installation.
    /// </summary>
    [Test]
    public void BeforeSlotCardInstall_CallbackIsInvokedBeforeSlotCardInstall()
    {
        var mockCpu = CreateMockCpu();
        var callbackInvoked = false;

        _ = new MachineBuilder()
            .WithCpuFactory(_ => mockCpu.Object)
            .BeforeSlotCardInstall(m =>
            {
                callbackInvoked = true;
            })
            .Build();

        Assert.That(callbackInvoked, Is.True);
    }

    /// <summary>
    /// Verifies that null BeforeSlotCardInstall callback throws.
    /// </summary>
    [Test]
    public void BeforeSlotCardInstall_NullCallback_ThrowsArgumentNullException()
    {
        var builder = new MachineBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.BeforeSlotCardInstall(null!));
    }

    /// <summary>
    /// Verifies that AfterSlotCardInstall callback is invoked after slot card installation.
    /// </summary>
    [Test]
    public void AfterSlotCardInstall_CallbackIsInvokedAfterSlotCardInstall()
    {
        var mockCpu = CreateMockCpu();
        var callbackInvoked = false;

        _ = new MachineBuilder()
            .WithCpuFactory(_ => mockCpu.Object)
            .AfterSlotCardInstall(m =>
            {
                callbackInvoked = true;
            })
            .Build();

        Assert.That(callbackInvoked, Is.True);
    }

    /// <summary>
    /// Verifies that null AfterSlotCardInstall callback throws.
    /// </summary>
    [Test]
    public void AfterSlotCardInstall_NullCallback_ThrowsArgumentNullException()
    {
        var builder = new MachineBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.AfterSlotCardInstall(null!));
    }

    /// <summary>
    /// Verifies that build callbacks are invoked in the correct order.
    /// </summary>
    [Test]
    public void Build_CallbacksInvokedInCorrectOrder()
    {
        var mockCpu = CreateMockCpu();
        var callOrder = new List<string>();

        _ = new MachineBuilder()
            .WithCpuFactory(_ => mockCpu.Object)
            .BeforeDeviceInit(m => callOrder.Add("BeforeDeviceInit"))
            .AfterDeviceInit(m => callOrder.Add("AfterDeviceInit"))
            .BeforeSlotCardInstall(m => callOrder.Add("BeforeSlotCardInstall"))
            .AfterSlotCardInstall(m => callOrder.Add("AfterSlotCardInstall"))
            .AfterBuild(m => callOrder.Add("AfterBuild"))
            .Build();

        Assert.That(callOrder, Is.EqualTo(new[]
        {
            "BeforeDeviceInit",
            "AfterDeviceInit",
            "BeforeSlotCardInstall",
            "AfterSlotCardInstall",
            "AfterBuild",
        }));
    }

    /// <summary>
    /// Verifies that composite layers can be added to the builder.
    /// </summary>
    [Test]
    public void AddCompositeLayer_AddsLayerAndComponent()
    {
        var mockCpu = CreateMockCpu();
        var mockLayer = new Mock<ICompositeLayer>();
        mockLayer.Setup(l => l.Name).Returns("TestLayer");
        mockLayer.Setup(l => l.Priority).Returns(100);
        mockLayer.Setup(l => l.AddressRange).Returns((0xD000u, 0x3000u));

        var machine = new MachineBuilder()
            .WithCpuFactory(_ => mockCpu.Object)
            .AddCompositeLayer(mockLayer.Object)
            .Build();

        var retrieved = machine.GetComponent<ICompositeLayer>();

        Assert.Multiple(() =>
        {
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved!.Name, Is.EqualTo("TestLayer"));
        });
    }

    /// <summary>
    /// Verifies that null composite layer throws.
    /// </summary>
    [Test]
    public void AddCompositeLayer_NullLayer_ThrowsArgumentNullException()
    {
        var builder = new MachineBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.AddCompositeLayer(null!));
    }

    private static Mock<ICpu> CreateMockCpu()
    {
        var mockCpu = new Mock<ICpu>();
        mockCpu.Setup(c => c.Halted).Returns(false);
        mockCpu.Setup(c => c.IsStopRequested).Returns(false);
        mockCpu.Setup(c => c.Step()).Returns(new CpuStepResult(CpuRunState.Running, 1));
        return mockCpu;
    }

    private sealed class TestComponent
    {
        public string Name { get; set; } = string.Empty;
    }
}