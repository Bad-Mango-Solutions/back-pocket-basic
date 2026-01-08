// <copyright file="DeviceFactoryRegistryTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

using BadMango.Emulator.Bus.Interfaces;

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
    /// Verifies that pocketwatch factory creates a valid PocketWatchCard.
    /// </summary>
    [Test]
    public void PocketWatchFactory_CreatesPocketWatchCard()
    {
        DeviceFactoryRegistry.EnsureInitialized();

        var factory = DeviceFactoryRegistry.SlotCardFactories["pocketwatch"];
        var card = factory(null!);

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
            Assert.That(DeviceFactoryRegistry.SlotCardFactories, Does.ContainKey("POCKETWATCH"));
            Assert.That(DeviceFactoryRegistry.SlotCardFactories, Does.ContainKey("PocketWatch"));
        });
    }
}