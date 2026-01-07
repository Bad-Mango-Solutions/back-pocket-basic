// <copyright file="MachineProfileSerializerTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using System.Text.Json;

using BadMango.Emulator.Core.Configuration;

/// <summary>
/// Unit tests for <see cref="MachineProfileSerializer"/> and machine profile schema features.
/// </summary>
[TestFixture]
public class MachineProfileSerializerTests
{
    private MachineProfileSerializer serializer = null!;

    /// <summary>
    /// Sets up the test environment.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        serializer = new MachineProfileSerializer();
    }

    /// <summary>
    /// Tests serialization of a minimal profile.
    /// </summary>
    [Test]
    public void Serialize_MinimalProfile_ReturnsValidJson()
    {
        // Arrange
        var profile = CreateMinimalProfile();

        // Act
        var json = serializer.Serialize(profile);

        // Assert
        Assert.That(json, Is.Not.Null.And.Not.Empty);
        Assert.That(json, Does.Contain("\"name\""));
        Assert.That(json, Does.Contain("\"cpu\""));
        Assert.That(json, Does.Contain("\"memory\""));
    }

    /// <summary>
    /// Tests deserialization of a minimal profile.
    /// </summary>
    [Test]
    public void Deserialize_MinimalProfile_ReturnsProfile()
    {
        // Arrange
        var original = CreateMinimalProfile();
        var json = serializer.Serialize(original);

        // Act
        var deserialized = serializer.Deserialize(json);

        // Assert
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized.Name, Is.EqualTo(original.Name));
        Assert.That(deserialized.Cpu.Type, Is.EqualTo(original.Cpu.Type));
    }

    /// <summary>
    /// Tests round-trip serialization preserves data.
    /// </summary>
    [Test]
    public void RoundTrip_MinimalProfile_PreservesData()
    {
        // Arrange
        var original = CreateMinimalProfile();

        // Act
        var json1 = serializer.Serialize(original);
        var deserialized = serializer.Deserialize(json1);
        var json2 = serializer.Serialize(deserialized);

        // Assert
        Assert.That(json1, Is.EqualTo(json2));
    }

    /// <summary>
    /// Tests validation of round-trip.
    /// </summary>
    [Test]
    public void ValidateRoundTrip_MinimalProfile_ReturnsTrue()
    {
        // Arrange
        var profile = CreateMinimalProfile();

        // Act
        var result = serializer.ValidateRoundTrip(profile);

        // Assert
        Assert.That(result, Is.True);
    }

    /// <summary>
    /// Tests cloning a profile creates a deep copy.
    /// </summary>
    [Test]
    public void Clone_Profile_CreatesSeparateInstance()
    {
        // Arrange
        var original = CreateMinimalProfile();

        // Act
        var cloned = serializer.Clone(original);

        // Assert
        Assert.That(cloned, Is.Not.SameAs(original));
        Assert.That(cloned.Name, Is.EqualTo(original.Name));
    }

    /// <summary>
    /// Tests serialization of swap groups.
    /// </summary>
    [Test]
    public void Serialize_ProfileWithSwapGroups_IncludesSwapGroups()
    {
        // Arrange
        var profile = CreateProfileWithSwapGroups();

        // Act
        var json = serializer.Serialize(profile);

        // Assert
        Assert.That(json, Does.Contain("\"swapGroups\""));
        Assert.That(json, Does.Contain("\"language-card-d000\""));
        Assert.That(json, Does.Contain("\"variants\""));
    }

    /// <summary>
    /// Tests round-trip of swap groups.
    /// </summary>
    [Test]
    public void RoundTrip_ProfileWithSwapGroups_PreservesSwapGroups()
    {
        // Arrange
        var original = CreateProfileWithSwapGroups();

        // Act
        var json = serializer.Serialize(original);
        var deserialized = serializer.Deserialize(json);

        // Assert
        Assert.That(deserialized.Memory.SwapGroups, Is.Not.Null);
        Assert.That(deserialized.Memory.SwapGroups, Has.Count.EqualTo(1));

        var swapGroup = deserialized.Memory.SwapGroups![0];
        Assert.That(swapGroup.Name, Is.EqualTo("language-card-d000"));
        Assert.That(swapGroup.Controller, Is.EqualTo("language-card-controller"));
        Assert.That(swapGroup.VirtualBase, Is.EqualTo("0xD000"));
        Assert.That(swapGroup.Variants, Has.Count.EqualTo(2));
    }

    /// <summary>
    /// Tests serialization of memory controllers.
    /// </summary>
    [Test]
    public void Serialize_ProfileWithControllers_IncludesControllers()
    {
        // Arrange
        var profile = CreateProfileWithControllers();

        // Act
        var json = serializer.Serialize(profile);

        // Assert
        Assert.That(json, Does.Contain("\"controllers\""));
        Assert.That(json, Does.Contain("\"aux-memory\""));
        Assert.That(json, Does.Contain("\"pocket2e-aux-controller\""));
    }

    /// <summary>
    /// Tests round-trip of memory controllers.
    /// </summary>
    [Test]
    public void RoundTrip_ProfileWithControllers_PreservesControllers()
    {
        // Arrange
        var original = CreateProfileWithControllers();

        // Act
        var json = serializer.Serialize(original);
        var deserialized = serializer.Deserialize(json);

        // Assert
        Assert.That(deserialized.Memory.Controllers, Is.Not.Null);
        Assert.That(deserialized.Memory.Controllers, Has.Count.EqualTo(1));

        var controller = deserialized.Memory.Controllers![0];
        Assert.That(controller.Name, Is.EqualTo("aux-memory"));
        Assert.That(controller.Type, Is.EqualTo("pocket2e-aux-controller"));
        Assert.That(controller.Size, Is.EqualTo("0x10000"));
    }

    /// <summary>
    /// Tests serialization of slot system configuration.
    /// </summary>
    [Test]
    public void Serialize_ProfileWithSlots_IncludesSlotsSection()
    {
        // Arrange
        var profile = CreateProfileWithSlots();

        // Act
        var json = serializer.Serialize(profile);

        // Assert
        Assert.That(json, Does.Contain("\"slots\""));
        Assert.That(json, Does.Contain("\"cards\""));
        Assert.That(json, Does.Contain("\"pocketwatch\""));
    }

    /// <summary>
    /// Tests round-trip of slot system configuration.
    /// </summary>
    [Test]
    public void RoundTrip_ProfileWithSlots_PreservesSlotConfiguration()
    {
        // Arrange
        var original = CreateProfileWithSlots();

        // Act
        var json = serializer.Serialize(original);
        var deserialized = serializer.Deserialize(json);

        // Assert
        Assert.That(deserialized.Slots, Is.Not.Null);
        Assert.That(deserialized.Slots!.Enabled, Is.True);
        Assert.That(deserialized.Slots.InternalC3Rom, Is.True);
        Assert.That(deserialized.Slots.Cards, Is.Not.Null);
        Assert.That(deserialized.Slots.Cards, Has.Count.EqualTo(1));

        var card = deserialized.Slots.Cards![0];
        Assert.That(card.Slot, Is.EqualTo(4));
        Assert.That(card.Type, Is.EqualTo("pocketwatch"));
    }

    /// <summary>
    /// Tests serialization of ROM image definitions.
    /// </summary>
    [Test]
    public void Serialize_ProfileWithRomImages_IncludesRomImagesSection()
    {
        // Arrange
        var profile = CreateProfileWithRomImages();

        // Act
        var json = serializer.Serialize(profile);

        // Assert
        Assert.That(json, Does.Contain("\"rom-images\""));
        Assert.That(json, Does.Contain("\"system-rom\""));
        Assert.That(json, Does.Contain("\"on_verification_fail\""));
    }

    /// <summary>
    /// Tests round-trip of ROM image definitions with hash.
    /// </summary>
    [Test]
    public void RoundTrip_ProfileWithRomImages_PreservesRomImageConfiguration()
    {
        // Arrange
        var original = CreateProfileWithRomImages();

        // Act
        var json = serializer.Serialize(original);
        var deserialized = serializer.Deserialize(json);

        // Assert
        Assert.That(deserialized.Memory.RomImages, Is.Not.Null);
        Assert.That(deserialized.Memory.RomImages, Has.Count.EqualTo(1));

        var rom = deserialized.Memory.RomImages![0];
        Assert.That(rom.Name, Is.EqualTo("system-rom"));
        Assert.That(rom.Source, Is.EqualTo("library://roms/test.rom"));
        Assert.That(rom.Size, Is.EqualTo("0x4000"));
        Assert.That(rom.Required, Is.True);
        Assert.That(rom.OnVerificationFail, Is.EqualTo("fallback"));
        Assert.That(rom.Hash, Is.Not.Null);
        Assert.That(rom.Hash!.Sha256, Is.EqualTo("test-hash"));
    }

    /// <summary>
    /// Tests serialization of device configuration.
    /// </summary>
    [Test]
    public void Serialize_ProfileWithDevices_IncludesDevicesSection()
    {
        // Arrange
        var profile = CreateProfileWithDevices();

        // Act
        var json = serializer.Serialize(profile);

        // Assert
        Assert.That(json, Does.Contain("\"devices\""));
        Assert.That(json, Does.Contain("\"keyboard\""));
        Assert.That(json, Does.Contain("\"speaker\""));
    }

    /// <summary>
    /// Tests round-trip of device configuration.
    /// </summary>
    [Test]
    public void RoundTrip_ProfileWithDevices_PreservesDeviceConfiguration()
    {
        // Arrange
        var original = CreateProfileWithDevices();

        // Act
        var json = serializer.Serialize(original);
        var deserialized = serializer.Deserialize(json);

        // Assert
        Assert.That(deserialized.Devices, Is.Not.Null);
        Assert.That(deserialized.Devices!.Keyboard, Is.Not.Null);
        Assert.That(deserialized.Devices.Keyboard!.Preset, Is.EqualTo("enhanced"));
        Assert.That(deserialized.Devices.Keyboard.AutoRepeat, Is.True);
        Assert.That(deserialized.Devices.Speaker, Is.Not.Null);
        Assert.That(deserialized.Devices.Speaker!.Enabled, Is.True);
        Assert.That(deserialized.Devices.Speaker.SampleRate, Is.EqualTo(48000));
    }

    /// <summary>
    /// Tests serialization of boot configuration.
    /// </summary>
    [Test]
    public void Serialize_ProfileWithBoot_IncludesBootSection()
    {
        // Arrange
        var profile = CreateProfileWithBoot();

        // Act
        var json = serializer.Serialize(profile);

        // Assert
        Assert.That(json, Does.Contain("\"boot\""));
        Assert.That(json, Does.Contain("\"autoStart\""));
        Assert.That(json, Does.Contain("\"startupSlot\""));
    }

    /// <summary>
    /// Tests round-trip of boot configuration.
    /// </summary>
    [Test]
    public void RoundTrip_ProfileWithBoot_PreservesBootConfiguration()
    {
        // Arrange
        var original = CreateProfileWithBoot();

        // Act
        var json = serializer.Serialize(original);
        var deserialized = serializer.Deserialize(json);

        // Assert
        Assert.That(deserialized.Boot, Is.Not.Null);
        Assert.That(deserialized.Boot!.AutoStart, Is.True);
        Assert.That(deserialized.Boot.StartupSlot, Is.EqualTo(6));
    }

    /// <summary>
    /// Tests serialization of composite region.
    /// </summary>
    [Test]
    public void Serialize_ProfileWithCompositeRegion_IncludesHandler()
    {
        // Arrange
        var profile = CreateProfileWithCompositeRegion();

        // Act
        var json = serializer.Serialize(profile);

        // Assert
        Assert.That(json, Does.Contain("\"composite\""));
        Assert.That(json, Does.Contain("\"handler\""));
        Assert.That(json, Does.Contain("\"pocket2e-io\""));
    }

    /// <summary>
    /// Tests round-trip of composite region.
    /// </summary>
    [Test]
    public void RoundTrip_ProfileWithCompositeRegion_PreservesHandler()
    {
        // Arrange
        var original = CreateProfileWithCompositeRegion();

        // Act
        var json = serializer.Serialize(original);
        var deserialized = serializer.Deserialize(json);

        // Assert
        Assert.That(deserialized.Memory.Regions, Has.Count.EqualTo(2));
        var ioRegion = deserialized.Memory.Regions![1];
        Assert.That(ioRegion.Type, Is.EqualTo("composite"));
        Assert.That(ioRegion.Handler, Is.EqualTo("pocket2e-io"));
    }

    /// <summary>
    /// Tests serialization preserves schema reference.
    /// </summary>
    [Test]
    public void Serialize_ProfileWithSchema_PreservesSchemaReference()
    {
        // Arrange
        var profile = CreateMinimalProfile();
        profile.Schema = "../schemas/machine-profile.schema.json";

        // Act
        var json = serializer.Serialize(profile);
        var deserialized = serializer.Deserialize(json);

        // Assert
        Assert.That(deserialized.Schema, Is.EqualTo("../schemas/machine-profile.schema.json"));
    }

    /// <summary>
    /// Tests round-trip of full Pocket2e-style profile.
    /// </summary>
    [Test]
    public void RoundTrip_FullPocket2eProfile_PreservesAllData()
    {
        // Arrange
        var original = CreateFullPocket2eProfile();

        // Act
        var result = serializer.ValidateRoundTrip(original);

        // Assert
        Assert.That(result, Is.True);
    }

    /// <summary>
    /// Tests deserialization of null input throws.
    /// </summary>
    [Test]
    public void Deserialize_NullInput_ThrowsArgumentNullException()
    {
        Assert.That(
            () => serializer.Deserialize(null!),
            Throws.ArgumentNullException);
    }

    /// <summary>
    /// Tests serialization of null input throws.
    /// </summary>
    [Test]
    public void Serialize_NullInput_ThrowsArgumentNullException()
    {
        Assert.That(
            () => serializer.Serialize(null!),
            Throws.ArgumentNullException);
    }

    /// <summary>
    /// Tests deserialization of invalid JSON throws.
    /// </summary>
    [Test]
    public void Deserialize_InvalidJson_ThrowsJsonException()
    {
        Assert.That(
            () => serializer.Deserialize("{ invalid json }"),
            Throws.TypeOf<JsonException>());
    }

    /// <summary>
    /// Tests serialization of physical memory configuration.
    /// </summary>
    [Test]
    public void Serialize_ProfileWithPhysicalMemory_IncludesPhysicalSection()
    {
        // Arrange
        var profile = CreateProfileWithPhysicalMemory();

        // Act
        var json = serializer.Serialize(profile);

        // Assert
        Assert.That(json, Does.Contain("\"physical\""));
        Assert.That(json, Does.Contain("\"main-ram-64k\""));
        Assert.That(json, Does.Contain("\"fill\""));
    }

    /// <summary>
    /// Tests round-trip of physical memory configuration.
    /// </summary>
    [Test]
    public void RoundTrip_ProfileWithPhysicalMemory_PreservesPhysicalConfiguration()
    {
        // Arrange
        var original = CreateProfileWithPhysicalMemory();

        // Act
        var json = serializer.Serialize(original);
        var deserialized = serializer.Deserialize(json);

        // Assert
        Assert.That(deserialized.Memory.Physical, Is.Not.Null);
        Assert.That(deserialized.Memory.Physical, Has.Count.EqualTo(1));

        var physical = deserialized.Memory.Physical![0];
        Assert.That(physical.Name, Is.EqualTo("main-ram-64k"));
        Assert.That(physical.Size, Is.EqualTo("0x10000"));
        Assert.That(physical.Fill, Is.EqualTo("0x00"));
    }

    private static MachineProfile CreateMinimalProfile()
    {
        return new MachineProfile
        {
            Name = "test-profile",
            DisplayName = "Test Profile",
            Cpu = new CpuProfileSection
            {
                Type = "65C02",
                ClockSpeed = 1000000,
            },
            AddressSpace = 16,
            Memory = new MemoryProfileSection
            {
                Physical =
                [
                    new PhysicalMemoryProfile
                    {
                        Name = "main-ram-64k",
                        Size = "0x10000",
                        Fill = "0x00",
                    },
                ],
                Regions =
                [
                    new MemoryRegionProfile
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
        };
    }

    private static MachineProfile CreateProfileWithSwapGroups()
    {
        var profile = CreateMinimalProfile();
        profile.Memory.SwapGroups =
        [
            new SwapGroupProfile
            {
                Name = "language-card-d000",
                Controller = "language-card-controller",
                VirtualBase = "0xD000",
                Size = "0x1000",
                Comment = "Bank switching test",
                Variants =
                [
                    new SwapVariantProfile
                    {
                        Name = "rom",
                        Type = "rom",
                        Permissions = "rx",
                    },
                    new SwapVariantProfile
                    {
                        Name = "ram",
                        Type = "ram",
                        Permissions = "rwx",
                    },
                ],
                DefaultVariant = "rom",
            },
        ];
        return profile;
    }

    private static MachineProfile CreateProfileWithControllers()
    {
        var profile = CreateMinimalProfile();
        profile.Memory.Controllers =
        [
            new MemoryControllerProfile
            {
                Name = "aux-memory",
                Type = "pocket2e-aux-controller",
                Size = "0x10000",
                Comment = "64KB auxiliary RAM",
            },
        ];
        return profile;
    }

    private static MachineProfile CreateProfileWithSlots()
    {
        var profile = CreateMinimalProfile();
        profile.Slots = new SlotSystemProfile
        {
            Enabled = true,
            InternalC3Rom = true,
            InternalCxRom = false,
            Cards =
            [
                new SlotCardProfile
                {
                    Slot = 4,
                    Type = "pocketwatch",
                    Config = JsonDocument.Parse("{\"timeSource\":\"host\"}").RootElement,
                },
            ],
        };
        return profile;
    }

    private static MachineProfile CreateProfileWithRomImages()
    {
        var profile = CreateMinimalProfile();
        profile.Memory.RomImages =
        [
            new RomImageProfile
            {
                Name = "system-rom",
                Source = "library://roms/test.rom",
                Size = "0x4000",
                Required = true,
                OnVerificationFail = "fallback",
                Hash = new RomHashProfile
                {
                    Sha256 = "test-hash",
                },
            },
        ];
        return profile;
    }

    private static MachineProfile CreateProfileWithDevices()
    {
        var profile = CreateMinimalProfile();
        profile.Devices = new DevicesProfile
        {
            Keyboard = new KeyboardDeviceProfile
            {
                Preset = "enhanced",
                AutoRepeat = true,
            },
            Speaker = new SpeakerDeviceProfile
            {
                Enabled = true,
                SampleRate = 48000,
            },
            Video = new VideoDeviceProfile
            {
                Preset = "enhanced",
                ColorMode = "ntsc",
            },
            GameIO = new GameIODeviceProfile
            {
                Enabled = true,
                JoystickDeadzone = 0.1,
            },
        };
        return profile;
    }

    private static MachineProfile CreateProfileWithBoot()
    {
        var profile = CreateMinimalProfile();
        profile.Boot = new BootProfile
        {
            AutoStart = true,
            StartupSlot = 6,
        };
        return profile;
    }

    private static MachineProfile CreateProfileWithCompositeRegion()
    {
        var profile = CreateMinimalProfile();
        profile.Memory.Regions!.Add(new MemoryRegionProfile
        {
            Name = "io-page",
            Type = "composite",
            Start = "0xC000",
            Size = "0x1000",
            Handler = "pocket2e-io",
        });
        return profile;
    }

    private static MachineProfile CreateProfileWithPhysicalMemory()
    {
        return new MachineProfile
        {
            Name = "test-profile",
            DisplayName = "Test Profile",
            Cpu = new CpuProfileSection
            {
                Type = "65C02",
                ClockSpeed = 1000000,
            },
            AddressSpace = 16,
            Memory = new MemoryProfileSection
            {
                Physical =
                [
                    new PhysicalMemoryProfile
                    {
                        Name = "main-ram-64k",
                        Size = "0x10000",
                        Fill = "0x00",
                    },
                ],
                Regions =
                [
                    new MemoryRegionProfile
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
        };
    }

    private static MachineProfile CreateFullPocket2eProfile()
    {
        return new MachineProfile
        {
            Schema = "../schemas/machine-profile.schema.json",
            Name = "pocket2e",
            DisplayName = "Pocket2e",
            Description = "Test Pocket2e configuration",
            Cpu = new CpuProfileSection
            {
                Type = "65C02",
                ClockSpeed = 1020484,
            },
            AddressSpace = 16,
            Memory = new MemoryProfileSection
            {
                RomImages =
                [
                    new RomImageProfile
                    {
                        Name = "system-rom",
                        Source = "library://roms/pocket2e-system.rom",
                        Size = "0x4000",
                        Required = true,
                        OnVerificationFail = "fallback",
                    },
                ],
                Physical =
                [
                    new PhysicalMemoryProfile
                    {
                        Name = "main-ram-48k",
                        Size = "0xC000",
                        Fill = "0x00",
                    },
                    new PhysicalMemoryProfile
                    {
                        Name = "system-rom-16k",
                        Size = "0x4000",
                        Sources =
                        [
                            new PhysicalMemorySourceProfile
                            {
                                Type = "rom-image",
                                Name = "system-rom",
                                RomImage = "system-rom",
                                Offset = "0x0000",
                            },
                        ],
                    },
                    new PhysicalMemoryProfile
                    {
                        Name = "language-card-d000-bank1",
                        Size = "0x1000",
                        Fill = "0x00",
                    },
                ],
                Regions =
                [
                    new MemoryRegionProfile
                    {
                        Name = "main-ram",
                        Type = "ram",
                        Start = "0x0000",
                        Size = "0xC000",
                        Permissions = "rwx",
                        Source = "main-ram-48k",
                        SourceOffset = "0x0000",
                    },
                    new MemoryRegionProfile
                    {
                        Name = "io-page",
                        Type = "composite",
                        Start = "0xC000",
                        Size = "0x1000",
                        Handler = "pocket2e-io",
                    },
                ],
                SwapGroups =
                [
                    new SwapGroupProfile
                    {
                        Name = "language-card-d000",
                        Controller = "language-card-controller",
                        VirtualBase = "0xD000",
                        Size = "0x1000",
                        Variants =
                        [
                            new SwapVariantProfile
                            {
                                Name = "rom",
                                Type = "rom",
                                Source = "system-rom-16k",
                                SourceOffset = "0x1000",
                                Permissions = "rx",
                            },
                            new SwapVariantProfile
                            {
                                Name = "bank1",
                                Type = "ram",
                                Source = "language-card-d000-bank1",
                                SourceOffset = "0x0000",
                                Permissions = "rwx",
                            },
                        ],
                        DefaultVariant = "rom",
                    },
                ],
                Controllers =
                [
                    new MemoryControllerProfile
                    {
                        Name = "aux-memory",
                        Type = "pocket2e-aux-controller",
                        Size = "0x10000",
                    },
                ],
            },
            Slots = new SlotSystemProfile
            {
                IoRegion = "io-page",
                Enabled = true,
                InternalC3Rom = true,
                InternalCxRom = false,
                Cards =
                [
                    new SlotCardProfile
                    {
                        Slot = 4,
                        Type = "pocketwatch",
                    },
                ],
            },
            Devices = new DevicesProfile
            {
                Keyboard = new KeyboardDeviceProfile
                {
                    Preset = "enhanced",
                    AutoRepeat = true,
                },
                Speaker = new SpeakerDeviceProfile
                {
                    Enabled = true,
                    SampleRate = 48000,
                },
            },
            Boot = new BootProfile
            {
                AutoStart = true,
                StartupSlot = 6,
            },
        };
    }
}