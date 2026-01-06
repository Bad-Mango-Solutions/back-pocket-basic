// <copyright file="MachineFactoryTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

using BadMango.Emulator.Core.Configuration;

using Core.Cpu;

/// <summary>
/// Unit tests for the <see cref="MachineFactory"/> class.
/// </summary>
[TestFixture]
public class MachineFactoryTests
{
    /// <summary>
    /// Creates a standard test profile using the new regions-based schema.
    /// </summary>
    /// <returns>A machine profile for testing.</returns>
    private static MachineProfile CreateTestProfile()
    {
        return new MachineProfile
        {
            Name = "test-65c02",
            DisplayName = "Test 65C02",
            Cpu = new CpuProfileSection { Type = "65C02" },
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

    /// <summary>
    /// Verifies that the CPU is properly reset after creation, setting E and CP flags appropriately
    /// for 65C02 emulation mode.
    /// </summary>
    /// <remarks>
    /// This test validates the fix for the issue where the debug console's CPU started in a bad state
    /// because Reset() was not called after CPU creation. Without Reset(), the E (emulation mode)
    /// and CP (compatibility mode) flags would not be set, causing the CPU to behave as a 32-bit system.
    /// </remarks>
    [Test]
    public void CreateSystem_65C02_CpuIsProperlyReset()
    {
        var profile = CreateTestProfile();

        var (cpu, _, _, _) = MachineFactory.CreateSystem(profile);

        var registers = cpu.GetRegisters();

        // Verify emulation mode flags are set (the key fix)
        Assert.Multiple(() =>
        {
            Assert.That(registers.E, Is.True, "E (emulation mode) flag should be set for 65C02");
            Assert.That(registers.CP, Is.True, "CP (compatibility mode) flag should be set for 65C02");
        });
    }

    /// <summary>
    /// Verifies that the CPU status register is properly initialized after reset.
    /// </summary>
    [Test]
    public void CreateSystem_65C02_StatusFlagsProperlyInitialized()
    {
        var profile = CreateTestProfile();

        var (cpu, _, _, _) = MachineFactory.CreateSystem(profile);

        var registers = cpu.GetRegisters();

        // The I (interrupt disable) flag should be set after reset
        Assert.That(registers.P.IsInterruptDisabled(), Is.True, "I flag should be set after reset");
    }

    /// <summary>
    /// Verifies that CreateSystem throws ArgumentNullException when profile is null.
    /// </summary>
    [Test]
    public void CreateSystem_NullProfile_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => MachineFactory.CreateSystem(null!));
    }

    /// <summary>
    /// Verifies that CreateSystem creates all expected components.
    /// </summary>
    [Test]
    public void CreateSystem_65C02_ReturnsAllComponents()
    {
        var profile = CreateTestProfile();

        var (cpu, memory, disassembler, info) = MachineFactory.CreateSystem(profile);

        Assert.Multiple(() =>
        {
            Assert.That(cpu, Is.Not.Null);
            Assert.That(memory, Is.Not.Null);
            Assert.That(disassembler, Is.Not.Null);
            Assert.That(info, Is.Not.Null);
        });
    }
}