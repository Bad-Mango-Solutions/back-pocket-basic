// <copyright file="MachineFactoryTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core.Configuration;
using BadMango.Emulator.Emulation.Cpu;

using Core.Cpu;

/// <summary>
/// Unit tests for the <see cref="MachineFactory"/> class.
/// </summary>
[TestFixture]
public class MachineFactoryTests
{
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

    /// <summary>
    /// Creates a standard test profile using the new regions-based schema.
    /// </summary>
    /// <returns>A machine profile for testing.</returns>
    private static MachineProfile CreateTestProfile()
    {
        return new()
        {
            Name = "test-65c02",
            DisplayName = "Test 65C02",
            Cpu = new() { Type = "65C02" },
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
        };
    }

    // ─── TrapRegistry Integration Tests ─────────────────────────────────────────

    /// <summary>
    /// Verifies that the CPU receives a functional <see cref="ITrapRegistry"/>
    /// (not <see cref="NullTrapRegistry"/>) when a machine is built from a JSON profile.
    /// </summary>
    [Test]
    public void CreateDebugSystem_65C02_CpuHasFunctionalTrapRegistry()
    {
        var profile = CreateTestProfile();

        var (machine, _, _) = MachineFactory.CreateDebugSystem(profile);

        var cpu = (Cpu65C02)machine.Cpu;

        Assert.Multiple(() =>
        {
            Assert.That(cpu.TrapRegistry, Is.Not.Null, "CPU should have a trap registry");
            Assert.That(cpu.TrapRegistry, Is.Not.SameAs(NullTrapRegistry.Instance),
                "CPU should have a functional trap registry, not the null singleton");
            Assert.That(cpu.TrapRegistry, Is.InstanceOf<TrapRegistry>(),
                "CPU should have a real TrapRegistry instance");
        });
    }

    /// <summary>
    /// Verifies that the <see cref="ITrapRegistry"/> is accessible as a machine component
    /// via <see cref="IEventContext.GetComponent{T}"/>.
    /// </summary>
    [Test]
    public void CreateDebugSystem_65C02_TrapRegistryIsAccessibleAsComponent()
    {
        var profile = CreateTestProfile();

        var (machine, _, _) = MachineFactory.CreateDebugSystem(profile);

        var trapRegistry = machine.GetComponent<ITrapRegistry>();

        Assert.That(trapRegistry, Is.Not.Null, "TrapRegistry should be retrievable as a machine component");
        Assert.That(trapRegistry, Is.InstanceOf<TrapRegistry>(),
            "Component should be a real TrapRegistry instance");
    }

    /// <summary>
    /// Verifies that the trap registry on the CPU is the same instance as the
    /// one stored as a machine component.
    /// </summary>
    [Test]
    public void CreateDebugSystem_65C02_CpuAndComponentShareSameTrapRegistry()
    {
        var profile = CreateTestProfile();

        var (machine, _, _) = MachineFactory.CreateDebugSystem(profile);

        var cpu = (Cpu65C02)machine.Cpu;
        var componentRegistry = machine.GetComponent<ITrapRegistry>();

        Assert.That(cpu.TrapRegistry, Is.SameAs(componentRegistry),
            "CPU and machine component should reference the same TrapRegistry instance");
    }

    /// <summary>
    /// Verifies that traps can be registered and fire during CPU execution
    /// when the machine is built from a profile.
    /// </summary>
    [Test]
    public void CreateDebugSystem_65C02_RegisteredTrapFiresDuringExecution()
    {
        var profile = CreateTestProfile();
        var trapInvoked = false;

        var (machine, _, _) = MachineFactory.CreateDebugSystem(profile);

        var cpu = (Cpu65C02)machine.Cpu;

        // Register a trap at $0400 (our subroutine target)
        cpu.TrapRegistry.Register(
            0x0400,
            "TEST_TRAP",
            TrapCategory.UserDefined,
            (c, bus, ctx) =>
            {
                trapInvoked = true;
                return TrapResult.Success(new Core.Cycle(6));
            });

        // Write test program: JSR $0400; STP
        machine.Cpu.Write8(0x0300, 0x20); // JSR
        machine.Cpu.Write8(0x0301, 0x00); // low byte $0400
        machine.Cpu.Write8(0x0302, 0x04); // high byte $0400
        machine.Cpu.Write8(0x0303, 0xDB); // STP

        // Write RTS stub at $0400 in case trap doesn't fire
        machine.Cpu.Write8(0x0400, 0x60); // RTS

        // Execute
        machine.Cpu.SetPC(0x0300);
        machine.Step(); // JSR $0400
        machine.Step(); // Trap fires at $0400, auto-RTS

        Assert.Multiple(() =>
        {
            Assert.That(trapInvoked, Is.True, "Trap should fire during CPU execution");
            Assert.That(machine.Cpu.GetPC(), Is.EqualTo(0x0303u),
                "PC should return to instruction after JSR");
        });
    }

    /// <summary>
    /// Verifies that <see cref="MachineFactory.CreateMachine"/> also wires the trap registry.
    /// </summary>
    [Test]
    public void CreateMachine_65C02_CpuHasFunctionalTrapRegistry()
    {
        var profile = CreateTestProfile();

        var machine = MachineFactory.CreateMachine(profile);

        var cpu = (Cpu65C02)machine.Cpu;

        Assert.Multiple(() =>
        {
            Assert.That(cpu.TrapRegistry, Is.Not.SameAs(NullTrapRegistry.Instance),
                "CPU should have a functional trap registry from CreateMachine");
            Assert.That(machine.GetComponent<ITrapRegistry>(), Is.Not.Null,
                "TrapRegistry should be a machine component");
            Assert.That(cpu.TrapRegistry, Is.SameAs(machine.GetComponent<ITrapRegistry>()),
                "CPU and component should share the same instance");
        });
    }
}