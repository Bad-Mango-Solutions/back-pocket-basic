// <copyright file="MachineBuilderTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

using BadMango.Emulator.Core.Cpu;
using BadMango.Emulator.Core.Interfaces.Cpu;

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

    /// <summary>
    /// Verifies that composite regions with no handler use the default open-bus target.
    /// </summary>
    [Test]
    public void FromProfile_CompositeRegionWithNoHandler_UsesDefaultTarget()
    {
        var mockCpu = CreateMockCpu();

        // Create a profile with a composite region that has no handler
        var profile = new Core.Configuration.MachineProfile
        {
            Name = "test-profile",
            DisplayName = "Test Profile",
            Description = "Test",
            AddressSpace = 16,
            Cpu = new() { Type = "65C02", ClockSpeed = 1000000 },
            Memory = new()
            {
                Regions =
                [
                    new()
                    {
                        Name = "test-composite",
                        Type = "composite",
                        Start = "0xC000",
                        Size = "0x1000",
                        Permissions = "rw",
                        Handler = null, // No handler specified
                    },
                ],
            },
        };

        var machine = new MachineBuilder()
            .WithCpuFactory(_ => mockCpu.Object)
            .FromProfile(profile)
            .Build();

        // Read from the composite region should return $FF (open bus)
        var access = new BusAccess(
            Address: 0xC000u,
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
            Assert.That(result.Value, Is.EqualTo(0xFF), "Default composite should return open-bus value");
        });
    }

    /// <summary>
    /// Verifies that composite regions with "default" handler use the default open-bus target.
    /// </summary>
    [Test]
    public void FromProfile_CompositeRegionWithDefaultHandler_UsesDefaultTarget()
    {
        var mockCpu = CreateMockCpu();

        // Create a profile with a composite region that explicitly specifies "default" handler
        var profile = new Core.Configuration.MachineProfile
        {
            Name = "test-profile",
            DisplayName = "Test Profile",
            Description = "Test",
            AddressSpace = 16,
            Cpu = new() { Type = "65C02", ClockSpeed = 1000000 },
            Memory = new()
            {
                Regions =
                [
                    new()
                    {
                        Name = "test-composite",
                        Type = "composite",
                        Start = "0xC000",
                        Size = "0x1000",
                        Permissions = "rw",
                        Handler = "default", // Explicit default handler
                    },
                ],
            },
        };

        var machine = new MachineBuilder()
            .WithCpuFactory(_ => mockCpu.Object)
            .FromProfile(profile)
            .Build();

        // Read from the composite region should return $FF (open bus)
        var access = new BusAccess(
            Address: 0xC000u,
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
            Assert.That(result.Value, Is.EqualTo(0xFF), "Default composite should return open-bus value");
        });
    }

    /// <summary>
    /// Verifies that composite regions with unknown handler throw an error.
    /// </summary>
    [Test]
    public void FromProfile_CompositeRegionWithUnknownHandler_ThrowsInvalidOperationException()
    {
        var mockCpu = CreateMockCpu();

        // Create a profile with a composite region that has an unknown handler
        var profile = new Core.Configuration.MachineProfile
        {
            Name = "test-profile",
            DisplayName = "Test Profile",
            Description = "Test",
            AddressSpace = 16,
            Cpu = new() { Type = "65C02", ClockSpeed = 1000000 },
            Memory = new()
            {
                Regions =
                [
                    new()
                    {
                        Name = "test-composite",
                        Type = "composite",
                        Start = "0xC000",
                        Size = "0x1000",
                        Permissions = "rw",
                        Handler = "unknown-handler", // Unknown handler
                    },
                ],
            },
        };

        var builder = new MachineBuilder()
            .WithCpuFactory(_ => mockCpu.Object);

        var ex = Assert.Throws<InvalidOperationException>(() => builder.FromProfile(profile));

        Assert.That(ex!.Message, Does.Contain("unknown-handler"));
    }

    /// <summary>
    /// Verifies that <see cref="MachineBuilder.FromProfile"/> passes the optional per-card
    /// <see cref="System.Text.Json.JsonElement"/> configuration blob through to the registered
    /// slot-card factory. This is the plumbing required by config-driven cards such as the
    /// Disk II controller and SmartPort.
    /// </summary>
    [Test]
    public void FromProfile_SlotCardFactory_ReceivesConfigBlob()
    {
        var mockCpu = CreateMockCpu();

        // Parse a config blob that the fake "diskii"-style factory will require.
        using var configDoc = System.Text.Json.JsonDocument.Parse(
            "{\"drive1\":\"foo.dsk\",\"drive2\":\"bar.dsk\"}");
        var configElement = configDoc.RootElement.Clone();

        var profile = new Core.Configuration.MachineProfile
        {
            Name = "test-profile",
            DisplayName = "Test Profile",
            Description = "Test",
            AddressSpace = 16,
            Cpu = new() { Type = "65C02", ClockSpeed = 1000000 },
            Memory = new()
            {
                Regions =
                [
                    new()
                    {
                        Name = "test-composite",
                        Type = "composite",
                        Start = "0xC000",
                        Size = "0x1000",
                        Permissions = "rw",
                    },
                ],
            },
            Devices = new()
            {
                Slots = new()
                {
                    Enabled = true,
                    Cards =
                    [
                        new()
                        {
                            Slot = 6,
                            Type = "fakediskii",
                            Config = configElement,
                        },
                    ],
                },
            },
        };

        System.Text.Json.JsonElement? capturedConfig = null;
        int callCount = 0;
        var fakeCard = new FakeSlotCard();

        var builder = new MachineBuilder()
            .WithCpuFactory(_ => mockCpu.Object);

        builder.RegisterSlotCardFactory("fakediskii", (_, cfg) =>
        {
            callCount++;
            capturedConfig = cfg;
            return fakeCard;
        });

        builder.FromProfile(profile);

        Assert.Multiple(() =>
        {
            Assert.That(callCount, Is.EqualTo(1), "Slot-card factory should be invoked exactly once.");
            Assert.That(capturedConfig.HasValue, Is.True, "Factory should receive the config blob from SlotCardProfile.Config.");
            Assert.That(capturedConfig!.Value.GetProperty("drive1").GetString(), Is.EqualTo("foo.dsk"));
            Assert.That(capturedConfig!.Value.GetProperty("drive2").GetString(), Is.EqualTo("bar.dsk"));
        });
    }

    /// <summary>
    /// Verifies that slot-card factories registered without per-card configuration still
    /// receive a <see langword="null"/> <see cref="System.Text.Json.JsonElement"/> when the
    /// profile entry omits the <c>config</c> property. This guards the "ignore the config
    /// argument" adaptation of legacy parameterless registrations.
    /// </summary>
    [Test]
    public void FromProfile_SlotCardFactory_ReceivesNullConfig_WhenProfileOmitsConfig()
    {
        var mockCpu = CreateMockCpu();

        var profile = new Core.Configuration.MachineProfile
        {
            Name = "test-profile",
            DisplayName = "Test Profile",
            Description = "Test",
            AddressSpace = 16,
            Cpu = new() { Type = "65C02", ClockSpeed = 1000000 },
            Memory = new()
            {
                Regions =
                [
                    new()
                    {
                        Name = "test-composite",
                        Type = "composite",
                        Start = "0xC000",
                        Size = "0x1000",
                        Permissions = "rw",
                    },
                ],
            },
            Devices = new()
            {
                Slots = new()
                {
                    Enabled = true,
                    Cards =
                    [
                        new()
                        {
                            Slot = 4,
                            Type = "fakecard",

                            // No Config specified - factory should receive null.
                        },
                    ],
                },
            },
        };

        System.Text.Json.JsonElement? capturedConfig = default;
        bool factoryCalled = false;
        var fakeCard = new FakeSlotCard();

        var builder = new MachineBuilder()
            .WithCpuFactory(_ => mockCpu.Object);

        builder.RegisterSlotCardFactory("fakecard", (_, cfg) =>
        {
            factoryCalled = true;
            capturedConfig = cfg;
            return fakeCard;
        });

        builder.FromProfile(profile);

        Assert.Multiple(() =>
        {
            Assert.That(factoryCalled, Is.True);
            Assert.That(capturedConfig.HasValue, Is.False, "Factory should receive null when profile omits config.");
        });
    }

    /// <summary>
    /// Verifies that <see cref="MachineBuilder.FromProfile"/> consumes a per-card
    /// <c>config.rom</c> string by looking up the named entry in <c>memory.rom-images</c>,
    /// loading the bytes, and pushing them into the freshly-built slot card via a
    /// public <c>LoadBootRom(byte[])</c> method. This is the wiring required for the
    /// Disk II controller (and any other card with a slot ROM) to honour the
    /// user-supplied $C600 P5A image declared in the profile.
    /// </summary>
    [Test]
    public void FromProfile_SlotCard_ConfigRom_LoadsBootRomBytesIntoCard()
    {
        var mockCpu = CreateMockCpu();

        // Lay down a sentinel ROM file on disk.
        string romPath = Path.Combine(Path.GetTempPath(), $"bm-config-rom-{Guid.NewGuid():N}.rom");
        var romBytes = new byte[256];
        romBytes[0] = 0xA9; // LDA #imm
        romBytes[1] = 0xC6;
        romBytes[2] = 0x60; // RTS
        File.WriteAllBytes(romPath, romBytes);

        try
        {
            using var configDoc = System.Text.Json.JsonDocument.Parse(
                "{\"rom\":\"slot6-boot-rom\"}");
            var configElement = configDoc.RootElement.Clone();

            var profile = new Core.Configuration.MachineProfile
            {
                Name = "test-profile",
                DisplayName = "Test Profile",
                Description = "Test",
                AddressSpace = 16,
                Cpu = new() { Type = "65C02", ClockSpeed = 1000000 },
                Memory = new()
                {
                    RomImages =
                    [
                        new()
                        {
                            Name = "slot6-boot-rom",
                            Source = romPath,
                            Size = "0x100",
                            Required = true,
                        },
                    ],
                    Regions =
                    [
                        new()
                        {
                            Name = "test-composite",
                            Type = "composite",
                            Start = "0xC000",
                            Size = "0x1000",
                            Permissions = "rw",
                        },
                    ],
                },
                Devices = new()
                {
                    Slots = new()
                    {
                        Enabled = true,
                        Cards =
                        [
                            new()
                            {
                                Slot = 6,
                                Type = "fakebootcard",
                                Config = configElement,
                            },
                        ],
                    },
                },
            };

            var fakeCard = new FakeBootRomSlotCard();

            var builder = new MachineBuilder()
                .WithCpuFactory(_ => mockCpu.Object);

            builder.RegisterSlotCardFactory("fakebootcard", (_, _) => fakeCard);

            builder.FromProfile(profile);

            Assert.Multiple(() =>
            {
                Assert.That(fakeCard.LoadedRom, Is.Not.Null, "LoadBootRom should have been invoked.");
                Assert.That(fakeCard.LoadedRom!.Length, Is.EqualTo(256));
                Assert.That(fakeCard.LoadedRom[0], Is.EqualTo((byte)0xA9));
                Assert.That(fakeCard.LoadedRom[1], Is.EqualTo((byte)0xC6));
                Assert.That(fakeCard.LoadedRom[2], Is.EqualTo((byte)0x60));
            });
        }
        finally
        {
            if (File.Exists(romPath))
            {
                File.Delete(romPath);
            }
        }
    }

    /// <summary>
    /// Verifies that <see cref="MachineBuilder.FromProfile"/> raises a clear diagnostic
    /// when <c>config.rom</c> names a ROM image that is not declared in
    /// <c>memory.rom-images</c>, instead of silently dropping the configuration.
    /// </summary>
    [Test]
    public void FromProfile_SlotCard_ConfigRom_UnknownRomImage_Throws()
    {
        var mockCpu = CreateMockCpu();

        using var configDoc = System.Text.Json.JsonDocument.Parse(
            "{\"rom\":\"missing-rom\"}");
        var configElement = configDoc.RootElement.Clone();

        var profile = new Core.Configuration.MachineProfile
        {
            Name = "test-profile",
            DisplayName = "Test Profile",
            Description = "Test",
            AddressSpace = 16,
            Cpu = new() { Type = "65C02", ClockSpeed = 1000000 },
            Memory = new()
            {
                Regions =
                [
                    new()
                    {
                        Name = "test-composite",
                        Type = "composite",
                        Start = "0xC000",
                        Size = "0x1000",
                        Permissions = "rw",
                    },
                ],
            },
            Devices = new()
            {
                Slots = new()
                {
                    Enabled = true,
                    Cards =
                    [
                        new()
                        {
                            Slot = 6,
                            Type = "fakebootcard-missing",
                            Config = configElement,
                        },
                    ],
                },
            },
        };

        var builder = new MachineBuilder()
            .WithCpuFactory(_ => mockCpu.Object);
        builder.RegisterSlotCardFactory("fakebootcard-missing", (_, _) => new FakeBootRomSlotCard());

        var ex = Assert.Throws<InvalidOperationException>(() => builder.FromProfile(profile));
        Assert.That(ex!.Message, Does.Contain("missing-rom"));
    }

    /// <summary>
    /// Verifies that <see cref="MachineBuilder.FromProfile"/> raises a clear diagnostic
    /// when <c>config.rom</c> targets a slot card whose type does not implement the
    /// reflected <c>LoadBootRom(byte[])</c> contract — preventing silent loss of the
    /// requested ROM image.
    /// </summary>
    [Test]
    public void FromProfile_SlotCard_ConfigRom_CardWithoutLoadBootRom_Throws()
    {
        var mockCpu = CreateMockCpu();

        string romPath = Path.Combine(Path.GetTempPath(), $"bm-config-rom-{Guid.NewGuid():N}.rom");
        File.WriteAllBytes(romPath, new byte[256]);

        try
        {
            using var configDoc = System.Text.Json.JsonDocument.Parse(
                "{\"rom\":\"slot6-boot-rom\"}");
            var configElement = configDoc.RootElement.Clone();

            var profile = new Core.Configuration.MachineProfile
            {
                Name = "test-profile",
                DisplayName = "Test Profile",
                Description = "Test",
                AddressSpace = 16,
                Cpu = new() { Type = "65C02", ClockSpeed = 1000000 },
                Memory = new()
                {
                    RomImages =
                    [
                        new()
                        {
                            Name = "slot6-boot-rom",
                            Source = romPath,
                            Size = "0x100",
                            Required = true,
                        },
                    ],
                    Regions =
                    [
                        new()
                        {
                            Name = "test-composite",
                            Type = "composite",
                            Start = "0xC000",
                            Size = "0x1000",
                            Permissions = "rw",
                        },
                    ],
                },
                Devices = new()
                {
                    Slots = new()
                    {
                        Enabled = true,
                        Cards =
                        [
                            new()
                            {
                                Slot = 6,
                                Type = "fakecard-noloader",
                                Config = configElement,
                            },
                        ],
                    },
                },
            };

            var builder = new MachineBuilder()
                .WithCpuFactory(_ => mockCpu.Object);
            builder.RegisterSlotCardFactory("fakecard-noloader", (_, _) => new FakeSlotCard());

            var ex = Assert.Throws<InvalidOperationException>(() => builder.FromProfile(profile));
            Assert.That(ex!.Message, Does.Contain("LoadBootRom"));
        }
        finally
        {
            if (File.Exists(romPath))
            {
                File.Delete(romPath);
            }
        }
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

    /// <summary>
    /// Minimal <see cref="ISlotCard"/> implementation used by slot-card factory tests
    /// to verify that <see cref="MachineBuilder.RegisterSlotCardFactory"/> and
    /// <see cref="MachineBuilder.FromProfile"/> plumb the per-card configuration blob
    /// through to the factory.
    /// </summary>
    private sealed class FakeSlotCard : ISlotCard
    {
        private readonly string deviceType;

        public FakeSlotCard()
            : this(nameof(FakeSlotCard).ToLowerInvariant())
        {
        }

        public FakeSlotCard(string deviceType)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(deviceType);
            this.deviceType = deviceType;
        }

        public string Name => "FakeSlotCard";

        public string DeviceType => this.deviceType;

        public PeripheralKind Kind => PeripheralKind.SlotCard;

        public int SlotNumber { get; set; }

        public SlotIOHandlers? IOHandlers => null;

        public IBusTarget? ROMRegion => null;

        public IBusTarget? ExpansionROMRegion => null;

        public void Initialize(IEventContext context)
        {
        }

        public void Reset()
        {
        }

        public void OnExpansionROMSelected()
        {
        }

        public void OnExpansionROMDeselected()
        {
        }
    }

    /// <summary>
    /// Slot-card test double exposing a public <c>LoadBootRom(byte[])</c> method that
    /// captures the bytes pushed in by <see cref="MachineBuilder.FromProfile"/> when a
    /// per-card <c>config.rom</c> entry is resolved against <c>memory.rom-images</c>.
    /// </summary>
    private sealed class FakeBootRomSlotCard : ISlotCard
    {
        public string Name => "FakeBootRomSlotCard";

        public string DeviceType => "fakebootcard";

        public PeripheralKind Kind => PeripheralKind.SlotCard;

        public int SlotNumber { get; set; }

        public SlotIOHandlers? IOHandlers => null;

        public IBusTarget? ROMRegion => null;

        public IBusTarget? ExpansionROMRegion => null;

        public byte[]? LoadedRom { get; private set; }

        public void LoadBootRom(byte[] romData)
        {
            ArgumentNullException.ThrowIfNull(romData);
            LoadedRom = (byte[])romData.Clone();
        }

        public void Initialize(IEventContext context)
        {
        }

        public void Reset()
        {
        }

        public void OnExpansionROMSelected()
        {
        }

        public void OnExpansionROMDeselected()
        {
        }
    }
}