// <copyright file="DeviceFactoryRegistryTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Devices.Interfaces;
using BadMango.Unit.Components;

/// <summary>
/// Unit tests for the <see cref="DeviceFactoryRegistry"/> class.
/// </summary>
[TestFixture]
public class DeviceFactoryRegistryTests
{
    /// <summary>
    /// Verifies that EnsureInitialized discovers motherboard device factories.
    /// </summary>
    [Test]
    public void EnsureInitialized_DiscoversMotherboardDeviceFactories()
    {
        DeviceFactoryRegistry.EnsureInitialized();

        Assert.That(DeviceFactoryRegistry.MotherboardDeviceFactories, Does.ContainKey("speaker"));
    }

    /// <summary>
    /// Verifies that EnsureInitialized discovers keyboard device factory.
    /// </summary>
    [Test]
    public void EnsureInitialized_DiscoversKeyboardDeviceFactory()
    {
        DeviceFactoryRegistry.EnsureInitialized();

        Assert.That(DeviceFactoryRegistry.MotherboardDeviceFactories, Does.ContainKey("keyboard"));
    }

    /// <summary>
    /// Verifies that EnsureInitialized discovers video device factory.
    /// </summary>
    [Test]
    public void EnsureInitialized_DiscoversVideoDeviceFactory()
    {
        DeviceFactoryRegistry.EnsureInitialized();

        Assert.That(DeviceFactoryRegistry.MotherboardDeviceFactories, Does.ContainKey("video"));
    }

    /// <summary>
    /// Verifies that EnsureInitialized discovers slot card factories.
    /// </summary>
    [Test]
    public void EnsureInitialized_DiscoversSlotCardFactories()
    {
        DeviceFactoryRegistry.EnsureInitialized();

        Assert.That(DeviceFactoryRegistry.SlotCardFactories, Does.ContainKey("pocketwatch"));
    }

    /// <summary>
    /// Verifies that speaker factory creates a valid SpeakerController.
    /// </summary>
    [Test]
    public void SpeakerFactory_CreatesSpeakerController()
    {
        DeviceFactoryRegistry.EnsureInitialized();

        var factory = DeviceFactoryRegistry.MotherboardDeviceFactories["speaker"];
        using var device = factory(null!) as IDisposable;

        Assert.That(device, Is.Not.Null);
        Assert.That(device, Is.InstanceOf<IMotherboardDevice>());
    }

    /// <summary>
    /// Verifies that keyboard factory creates a valid KeyboardDevice.
    /// </summary>
    [Test]
    public void KeyboardFactory_CreatesKeyboardDevice()
    {
        DeviceFactoryRegistry.EnsureInitialized();

        var factory = DeviceFactoryRegistry.MotherboardDeviceFactories["keyboard"];
        var device = factory(null!);

        Assert.That(device, Is.Not.Null);
        Assert.That(device, Is.InstanceOf<IKeyboardDevice>());
        Assert.That(device.Name, Is.EqualTo("Keyboard"));
    }

    /// <summary>
    /// Verifies that video factory creates a valid VideoDevice.
    /// </summary>
    [Test]
    public void VideoFactory_CreatesVideoDevice()
    {
        DeviceFactoryRegistry.EnsureInitialized();

        var factory = DeviceFactoryRegistry.MotherboardDeviceFactories["video"];
        var device = factory(null!);

        Assert.That(device, Is.Not.Null);
        Assert.That(device, Is.InstanceOf<IVideoDevice>());
        Assert.That(device.Name, Is.EqualTo("Video Device"));
    }

    /// <summary>
    /// Verifies that pocketwatch factory creates a valid PocketWatchCard.
    /// </summary>
    [Test]
    public void PocketWatchFactory_CreatesPocketWatchCard()
    {
        DeviceFactoryRegistry.EnsureInitialized();

        var factory = DeviceFactoryRegistry.SlotCardFactories["pocketwatch"];
        var card = factory(null!, null);

        Assert.That(card, Is.Not.Null);
        Assert.That(card, Is.InstanceOf<ISlotCard>());
        Assert.That(card.Name, Is.EqualTo("PocketWatch"));
    }

    /// <summary>
    /// Verifies that device type lookups are case-insensitive.
    /// </summary>
    [Test]
    public void Factories_AreCaseInsensitive()
    {
        DeviceFactoryRegistry.EnsureInitialized();

        Assert.Multiple(() =>
        {
            Assert.That(DeviceFactoryRegistry.MotherboardDeviceFactories, Does.ContainKey("SPEAKER"));
            Assert.That(DeviceFactoryRegistry.MotherboardDeviceFactories, Does.ContainKey("Speaker"));
            Assert.That(DeviceFactoryRegistry.MotherboardDeviceFactories, Does.ContainKey("KEYBOARD"));
            Assert.That(DeviceFactoryRegistry.MotherboardDeviceFactories, Does.ContainKey("Keyboard"));
            Assert.That(DeviceFactoryRegistry.MotherboardDeviceFactories, Does.ContainKey("VIDEO"));
            Assert.That(DeviceFactoryRegistry.MotherboardDeviceFactories, Does.ContainKey("Video"));
            Assert.That(DeviceFactoryRegistry.SlotCardFactories, Does.ContainKey("POCKETWATCH"));
            Assert.That(DeviceFactoryRegistry.SlotCardFactories, Does.ContainKey("PocketWatch"));
            Assert.That(DeviceFactoryRegistry.SlotCardFactories, Does.ContainKey("DISK-II-COMPATIBLE"));
            Assert.That(DeviceFactoryRegistry.SlotCardFactories, Does.ContainKey("Disk-II-Compatible"));
        });
    }

    /// <summary>
    /// Verifies that EnsureInitialized discovers the Disk II compatible slot card factory.
    /// The full <see cref="DiskIIController"/> is the registered implementation; the
    /// registry resolves its <see cref="Serilog.ILogger"/> dependency at invocation time.
    /// </summary>
    [Test]
    public void EnsureInitialized_DiscoversDiskIIFactory()
    {
        DeviceFactoryRegistry.EnsureInitialized();

        Assert.That(DeviceFactoryRegistry.SlotCardFactories, Does.ContainKey("disk-ii-compatible"));
    }

    /// <summary>
    /// Verifies that the Disk II factory produces the full <see cref="DiskIIController"/>
    /// (not the stub), demonstrating that the registry can satisfy constructor
    /// dependencies (here, an injected <see cref="Serilog.ILogger"/>).
    /// </summary>
    [Test]
    public void DiskIIFactory_CreatesFullDiskIIController()
    {
        DeviceFactoryRegistry.EnsureInitialized();

        var factory = DeviceFactoryRegistry.SlotCardFactories["disk-ii-compatible"];
        var card = factory(null!, null);

        Assert.That(card, Is.Not.Null);
        Assert.That(card, Is.InstanceOf<DiskIIController>());
        Assert.That(card.DeviceType, Is.EqualTo("DiskII"));
    }

    /// <summary>
    /// Verifies that the configurable <see cref="DeviceFactoryRegistry.LoggerFactory"/>
    /// is consulted when the registry constructs a device that takes an injected logger.
    /// </summary>
    [Test]
    public void DiskIIFactory_UsesConfiguredLoggerFactory()
    {
        DeviceFactoryRegistry.EnsureInitialized();
        var loggerMock = Generator.Log();
        var requestedTypes = new List<Type>();
        var previous = DeviceFactoryRegistry.LoggerFactory;
        DeviceFactoryRegistry.LoggerFactory = t =>
        {
            requestedTypes.Add(t);
            return loggerMock.Object;
        };
        try
        {
            var factory = DeviceFactoryRegistry.SlotCardFactories["disk-ii-compatible"];
            var card = factory(null!, null);

            Assert.That(card, Is.InstanceOf<DiskIIController>());
            Assert.That(requestedTypes, Does.Contain(typeof(DiskIIController)));
        }
        finally
        {
            DeviceFactoryRegistry.LoggerFactory = previous;
        }
    }
}