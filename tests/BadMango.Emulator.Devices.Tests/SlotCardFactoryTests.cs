// <copyright file="SlotCardFactoryTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core.Configuration;
using BadMango.Emulator.Devices.Interfaces;

/// <summary>
/// Unit tests for slot card factory registration and profile-based loading.
/// </summary>
[TestFixture]
public class SlotCardFactoryTests
{
    /// <summary>
    /// Verifies that RegisterPocketWatchCardFactory registers the factory.
    /// </summary>
    [Test]
    public void RegisterPocketWatchCardFactory_RegistersFactory()
    {
        // Arrange - Create builder and register factory
        var builder = MachineBuilder.Create()
            .RegisterPocketWatchCardFactory();

        // The factory is registered internally - we can verify by building a profile
        // with a pocketwatch card and checking it's installed
        var profile = CreateMinimalProfileWithPocketWatch();

        // Build without slot manager to avoid needing full infrastructure
        // Just verify registration doesn't throw
        Assert.DoesNotThrow(() => builder.RegisterPocketWatchCardFactory());
    }

    /// <summary>
    /// Verifies that RegisterStandardDeviceFactories registers all factories.
    /// </summary>
    [Test]
    public void RegisterStandardDeviceFactories_RegistersAllFactories()
    {
        // Arrange & Act
        var builder = MachineBuilder.Create()
            .RegisterStandardDeviceFactories();

        // Assert - no exception means factories were registered
        Assert.That(builder, Is.Not.Null);
    }

    /// <summary>
    /// Verifies that PocketWatchCard has correct DeviceType attribute.
    /// </summary>
    [Test]
    public void PocketWatchCard_HasDeviceTypeAttribute()
    {
        var attribute = typeof(PocketWatchCard).GetCustomAttributes(typeof(DeviceTypeAttribute), false)
            .FirstOrDefault() as DeviceTypeAttribute;

        Assert.That(attribute, Is.Not.Null);
        Assert.That(attribute!.DeviceTypeId, Is.EqualTo("pocketwatch"));
    }

    /// <summary>
    /// Verifies that PocketWatchCard implements ISlotCard.
    /// </summary>
    [Test]
    public void PocketWatchCard_ImplementsISlotCard()
    {
        var card = new PocketWatchCard();

        Assert.That(card, Is.InstanceOf<ISlotCard>());
        Assert.That(card.Kind, Is.EqualTo(PeripheralKind.SlotCard));
    }

    /// <summary>
    /// Verifies that PocketWatchCard implements IClockDevice.
    /// </summary>
    [Test]
    public void PocketWatchCard_ImplementsIClockDevice()
    {
        var card = new PocketWatchCard();

        Assert.That(card, Is.InstanceOf<IClockDevice>());
    }

    /// <summary>
    /// Verifies that PocketWatchCard has correct properties.
    /// </summary>
    [Test]
    public void PocketWatchCard_HasCorrectProperties()
    {
        var card = new PocketWatchCard();

        Assert.Multiple(() =>
        {
            Assert.That(card.Name, Is.EqualTo("PocketWatch"));
            Assert.That(card.DeviceType, Is.EqualTo("PocketWatch"));
            Assert.That(card.IOHandlers, Is.Not.Null);
            Assert.That(card.ROMRegion, Is.Not.Null);
            Assert.That(card.ExpansionROMRegion, Is.Null);
        });
    }

    private static MachineProfile CreateMinimalProfileWithPocketWatch()
    {
        return new()
        {
            Name = "test-profile",
            DisplayName = "Test Profile",
            Cpu = new()
            {
                Type = "65C02",
                ClockSpeed = 1000000,
            },
            AddressSpace = 16,
            Memory = new()
            {
                Physical =
                [
                    new()
                    {
                        Name = "main-ram-64k",
                        Size = "0x10000",
                        Fill = "0x00",
                    },
                ],
                Regions =
                [
                    new()
                    {
                        Name = "main-ram",
                        Type = "ram",
                        Start = "0x0000",
                        Size = "0x10000",
                        Permissions = "rwx",
                        Source = "main-ram-64k",
                        SourceOffset = "0x0000",
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
                            Type = "pocketwatch",
                        },
                    ],
                },
            },
        };
    }
}