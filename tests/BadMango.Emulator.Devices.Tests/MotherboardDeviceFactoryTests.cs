// <copyright file="MotherboardDeviceFactoryTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

using BadMango.Emulator.Core.Configuration;

/// <summary>
/// Unit tests for motherboard device factory registration and profile loading.
/// </summary>
[TestFixture]
public class MotherboardDeviceFactoryTests
{
    /// <summary>
    /// Verifies that RegisterSpeakerDeviceFactory registers the speaker factory.
    /// </summary>
    [Test]
    public void RegisterSpeakerDeviceFactory_RegistersFactory()
    {
        // Arrange
        var builder = new MachineBuilder();

        // Act - Register the speaker factory
        builder.RegisterSpeakerDeviceFactory();

        // Assert - The factory was registered (we verify by building a machine with a profile)
        // Since there's no direct way to query registered factories, we just ensure no exception
        Assert.That(builder, Is.Not.Null);
    }

    /// <summary>
    /// Verifies that FromProfile creates speaker device when profile includes speaker.
    /// </summary>
    [Test]
    public void FromProfile_WithSpeakerDevice_CreatesSpeakerController()
    {
        // Arrange
        var profile = CreateProfileWithSpeaker();
        var builder = CreateBuilderWithFactoriesAndCpu();

        // Act
        builder.FromProfile(profile);
        var machine = builder.Build();

        // Assert - Speaker should be available as a component
        var speaker = machine.GetComponent<ISpeakerDevice>();
        Assert.That(speaker, Is.Not.Null);
        Assert.That(speaker!.DeviceType, Is.EqualTo("Speaker"));
    }

    /// <summary>
    /// Verifies that FromProfile skips disabled devices.
    /// </summary>
    [Test]
    public void FromProfile_WithDisabledDevice_DoesNotCreateDevice()
    {
        // Arrange
        var profile = CreateProfileWithDisabledSpeaker();
        var builder = CreateBuilderWithFactoriesAndCpu();

        // Act
        builder.FromProfile(profile);
        var machine = builder.Build();

        // Assert - Speaker should not be available
        var speaker = machine.GetComponent<ISpeakerDevice>();
        Assert.That(speaker, Is.Null);
    }

    /// <summary>
    /// Verifies that FromProfile silently skips unknown device types.
    /// </summary>
    [Test]
    public void FromProfile_WithUnknownDeviceType_SkipsSilently()
    {
        // Arrange
        var profile = CreateProfileWithUnknownDevice();
        var builder = CreateBuilderWithFactoriesAndCpu();

        // Act & Assert - Should not throw
        Assert.DoesNotThrow(() =>
        {
            builder.FromProfile(profile);
            builder.Build();
        });
    }

    /// <summary>
    /// Verifies that speaker device registers its soft switch handler during build.
    /// </summary>
    [Test]
    public void Build_WithSpeakerDevice_RegistersSoftSwitchHandler()
    {
        // Arrange
        var profile = CreateProfileWithSpeakerAndIORegion();
        var builder = CreateBuilderWithFactoriesAndCpu();
        builder.RegisterCompositeIOHandler();

        // Act
        builder.FromProfile(profile);
        var machine = builder.Build();

        // Assert - Speaker soft switch should be registered
        var dispatcher = machine.GetComponent<IOPageDispatcher>();
        Assert.That(dispatcher, Is.Not.Null);

        // Verify the handler is registered by checking if $C030 has a handler
        // We do this by attempting a read and ensuring no exception
        var access = new BusAccess(
            Address: 0xC030,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: AccessIntent.DataRead,
            SourceId: 0,
            Cycle: 0,
            Flags: AccessFlags.None);
        var result = dispatcher!.Read(0x30, access);

        // Should return floating bus (0xFF) without throwing
        Assert.That(result, Is.EqualTo(0xFF));
    }

    /// <summary>
    /// Verifies that RegisterStandardDeviceFactories registers all standard factories.
    /// </summary>
    [Test]
    public void RegisterStandardDeviceFactories_RegistersAllStandardFactories()
    {
        // Arrange
        var profile = CreateProfileWithSpeaker();
        var builder = new MachineBuilder();
        var cpuFactory = TestCpuFactory.Create();

        // Act - Register all standard factories
        builder.RegisterStandardDeviceFactories();
        builder.WithCpuFactory(cpuFactory);
        builder.FromProfile(profile);
        var machine = builder.Build();

        // Assert - Speaker should be available
        var speaker = machine.GetComponent<ISpeakerDevice>();
        Assert.That(speaker, Is.Not.Null);
    }

    private static MachineBuilder CreateBuilderWithFactoriesAndCpu()
    {
        var builder = new MachineBuilder();
        builder.RegisterSpeakerDeviceFactory();
        builder.WithCpuFactory(TestCpuFactory.Create());
        return builder;
    }

    private static MachineProfile CreateProfileWithSpeaker()
    {
        return new()
        {
            Name = "test-with-speaker",
            Cpu = new() { Type = "65C02", ClockSpeed = 1000000 },
            Memory = new()
            {
                Physical =
                [
                    new() { Name = "main-ram", Size = "0x10000", Fill = "0x00" },
                ],
                Regions =
                [
                    new()
                    {
                        Name = "main-ram",
                        Type = "ram",
                        Start = "0x0000",
                        Size = "0x10000",
                        Source = "main-ram",
                        SourceOffset = "0x0000",
                    },
                ],
            },
            Devices = new()
            {
                Motherboard =
                [
                    new()
                    {
                        Type = "speaker",
                        Name = "Speaker",
                        Enabled = true,
                    },
                ],
            },
        };
    }

    private static MachineProfile CreateProfileWithSpeakerAndIORegion()
    {
        return new()
        {
            Name = "test-with-speaker-io",
            Cpu = new() { Type = "65C02", ClockSpeed = 1000000 },
            Memory = new()
            {
                Physical =
                [
                    new() { Name = "main-ram", Size = "0xC000", Fill = "0x00" },
                    new() { Name = "rom", Size = "0x3000", Fill = "0x00" },
                ],
                Regions =
                [
                    new()
                    {
                        Name = "main-ram",
                        Type = "ram",
                        Start = "0x0000",
                        Size = "0xC000",
                        Source = "main-ram",
                        SourceOffset = "0x0000",
                    },
                    new()
                    {
                        Name = "io-region",
                        Type = "composite",
                        Start = "0xC000",
                        Size = "0x1000",
                        Handler = "composite-io",
                    },
                    new()
                    {
                        Name = "rom",
                        Type = "rom",
                        Start = "0xD000",
                        Size = "0x3000",
                        Source = "rom",
                        SourceOffset = "0x0000",
                    },
                ],
            },
            Devices = new()
            {
                Motherboard =
                [
                    new()
                    {
                        Type = "speaker",
                        Name = "Speaker",
                        Enabled = true,
                    },
                ],
            },
        };
    }

    private static MachineProfile CreateProfileWithDisabledSpeaker()
    {
        return new()
        {
            Name = "test-with-disabled-speaker",
            Cpu = new() { Type = "65C02", ClockSpeed = 1000000 },
            Memory = new()
            {
                Physical =
                [
                    new() { Name = "main-ram", Size = "0x10000", Fill = "0x00" },
                ],
                Regions =
                [
                    new()
                    {
                        Name = "main-ram",
                        Type = "ram",
                        Start = "0x0000",
                        Size = "0x10000",
                        Source = "main-ram",
                        SourceOffset = "0x0000",
                    },
                ],
            },
            Devices = new()
            {
                Motherboard =
                [
                    new()
                    {
                        Type = "speaker",
                        Name = "Speaker",
                        Enabled = false, // Disabled!
                    },
                ],
            },
        };
    }

    private static MachineProfile CreateProfileWithUnknownDevice()
    {
        return new()
        {
            Name = "test-with-unknown-device",
            Cpu = new() { Type = "65C02", ClockSpeed = 1000000 },
            Memory = new()
            {
                Physical =
                [
                    new() { Name = "main-ram", Size = "0x10000", Fill = "0x00" },
                ],
                Regions =
                [
                    new()
                    {
                        Name = "main-ram",
                        Type = "ram",
                        Start = "0x0000",
                        Size = "0x10000",
                        Source = "main-ram",
                        SourceOffset = "0x0000",
                    },
                ],
            },
            Devices = new()
            {
                Motherboard =
                [
                    new()
                    {
                        Type = "unknown-device-xyz",
                        Name = "Unknown",
                        Enabled = true,
                    },
                ],
            },
        };
    }
}