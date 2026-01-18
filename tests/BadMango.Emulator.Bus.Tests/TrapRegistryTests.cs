// <copyright file="TrapRegistryTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core;
using BadMango.Emulator.Core.Interfaces.Cpu;

using Moq;

/// <summary>
/// Unit tests for the <see cref="TrapRegistry"/> class.
/// </summary>
[TestFixture]
public class TrapRegistryTests
{
    private const int PageSize = 4096;

    private TrapRegistry registry = null!;
    private Mock<ICpu> mockCpu = null!;
    private Mock<IMemoryBus> mockBus = null!;
    private Mock<IEventContext> mockContext = null!;

    /// <summary>
    /// Sets up test fixtures before each test.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        registry = new();
        mockCpu = new();
        mockBus = new();
        mockContext = new();
    }

    // ─── Constructor Tests ──────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the default constructor creates an empty registry.
    /// </summary>
    [Test]
    public void Constructor_Default_CreatesEmptyRegistry()
    {
        var testRegistry = new TrapRegistry();
        Assert.That(testRegistry.Count, Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies that the constructor with slot manager accepts the parameter.
    /// </summary>
    [Test]
    public void Constructor_WithSlotManager_Succeeds()
    {
        var mockSlotManager = new Mock<ISlotManager>();
        var testRegistry = new TrapRegistry(mockSlotManager.Object);
        Assert.That(testRegistry.Count, Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies that the constructor with slot manager and language card accepts parameters.
    /// </summary>
    [Test]
    public void Constructor_WithSlotManagerAndLanguageCard_Succeeds()
    {
        var mockSlotManager = new Mock<ISlotManager>();
        var mockLanguageCard = new Mock<ILanguageCardState>();
        var testRegistry = new TrapRegistry(mockSlotManager.Object, mockLanguageCard.Object);
        Assert.That(testRegistry.Count, Is.EqualTo(0));
    }

    // ─── Registration Tests ─────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that Register adds a trap and increments count.
    /// </summary>
    [Test]
    public void Register_ValidTrap_IncrementsCount()
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;

        registry.Register(0xFC58, "HOME", TrapCategory.MonitorRom, handler);

        Assert.That(registry.Count, Is.EqualTo(1));
    }

    /// <summary>
    /// Verifies that Register allows multiple traps at different addresses.
    /// </summary>
    [Test]
    public void Register_MultipleAddresses_AllAdded()
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;

        registry.Register(0xFC58, "HOME", TrapCategory.MonitorRom, handler);
        registry.Register(0xFDED, "COUT", TrapCategory.MonitorRom, handler);
        registry.Register(0xFD0C, "RDKEY", TrapCategory.MonitorRom, handler);

        Assert.That(registry.Count, Is.EqualTo(3));
    }

    /// <summary>
    /// Verifies that Register throws when trap already exists at address.
    /// </summary>
    [Test]
    public void Register_DuplicateAddress_ThrowsInvalidOperationException()
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;

        registry.Register(0xFC58, "HOME", TrapCategory.MonitorRom, handler);

        Assert.Throws<InvalidOperationException>(() =>
            registry.Register(0xFC58, "HOME2", TrapCategory.MonitorRom, handler));
    }

    /// <summary>
    /// Verifies that Register allows different operations at same address.
    /// </summary>
    [Test]
    public void Register_DifferentOperationsAtSameAddress_AllAdded()
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;

        registry.Register(0xC050, TrapOperation.Read, "READ_SWITCH", TrapCategory.OnboardDevice, handler);
        registry.Register(0xC050, TrapOperation.Write, "WRITE_SWITCH", TrapCategory.OnboardDevice, handler);

        Assert.That(registry.Count, Is.EqualTo(2));
    }

    /// <summary>
    /// Verifies that Register throws when name is null.
    /// </summary>
    [Test]
    public void Register_NullName_ThrowsArgumentNullException()
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;

        Assert.Throws<ArgumentNullException>(() =>
            registry.Register(0xFC58, null!, TrapCategory.MonitorRom, handler));
    }

    /// <summary>
    /// Verifies that Register throws when handler is null.
    /// </summary>
    [Test]
    public void Register_NullHandler_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            registry.Register(0xFC58, "HOME", TrapCategory.MonitorRom, null!));
    }

    // ─── RegisterSlotDependent Tests ────────────────────────────────────────────

    /// <summary>
    /// Verifies that RegisterSlotDependent adds a trap with slot info.
    /// </summary>
    [Test]
    public void RegisterSlotDependent_ValidTrap_IncrementsCount()
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;

        registry.RegisterSlotDependent(0xC600, 6, "DISK_BOOT", TrapCategory.SlotFirmware, handler);

        Assert.That(registry.Count, Is.EqualTo(1));
    }

    /// <summary>
    /// Verifies that RegisterSlotDependent marks traps in expansion ROM space correctly.
    /// </summary>
    [Test]
    public void RegisterSlotDependent_ExpansionRomAddress_SetsRequiresExpansionRom()
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;

        registry.RegisterSlotDependent(0xC803, 6, "DISK_ENTRY", TrapCategory.SlotFirmware, handler);

        var info = registry.GetTrapInfo(0xC803);
        Assert.That(info, Is.Not.Null);
        Assert.That(info!.Value.SlotNumber, Is.EqualTo(6));
    }

    /// <summary>
    /// Verifies that RegisterSlotDependent throws for invalid slot numbers.
    /// </summary>
    /// <param name="slot">The invalid slot number.</param>
    [TestCase(0)]
    [TestCase(8)]
    [TestCase(-1)]
    public void RegisterSlotDependent_InvalidSlot_ThrowsArgumentOutOfRangeException(int slot)
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            registry.RegisterSlotDependent(0xC600, slot, "TEST", TrapCategory.SlotFirmware, handler));
    }

    /// <summary>
    /// Verifies that RegisterSlotDependent works for all valid slots.
    /// </summary>
    /// <param name="slot">The valid slot number.</param>
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    public void RegisterSlotDependent_ValidSlot_Succeeds(int slot)
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;

        Assert.DoesNotThrow(() =>
            registry.RegisterSlotDependent(0xC000 + (uint)(slot << 8), slot, "TEST", TrapCategory.SlotFirmware, handler));
    }

    // ─── Unregister Tests ───────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that Unregister removes an existing trap.
    /// </summary>
    [Test]
    public void Unregister_ExistingTrap_ReturnsTrue()
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;
        registry.Register(0xFC58, "HOME", TrapCategory.MonitorRom, handler);

        bool result = registry.Unregister(0xFC58);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    /// Verifies that Unregister returns false for non-existent trap.
    /// </summary>
    [Test]
    public void Unregister_NonExistentTrap_ReturnsFalse()
    {
        bool result = registry.Unregister(0xFC58);

        Assert.That(result, Is.False);
    }

    /// <summary>
    /// Verifies that Unregister with operation only removes specific operation.
    /// </summary>
    [Test]
    public void Unregister_SpecificOperation_OnlyRemovesThatOperation()
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;
        registry.Register(0xC050, TrapOperation.Read, "READ_SWITCH", TrapCategory.OnboardDevice, handler);
        registry.Register(0xC050, TrapOperation.Write, "WRITE_SWITCH", TrapCategory.OnboardDevice, handler);

        registry.Unregister(0xC050, TrapOperation.Read);

        Assert.Multiple(() =>
        {
            Assert.That(registry.Count, Is.EqualTo(1));
            Assert.That(registry.HasTrap(0xC050, TrapOperation.Read), Is.False);
            Assert.That(registry.HasTrap(0xC050, TrapOperation.Write), Is.True);
        });
    }

    // ─── UnregisterSlotTraps Tests ──────────────────────────────────────────────

    /// <summary>
    /// Verifies that UnregisterSlotTraps removes all traps for a slot.
    /// </summary>
    [Test]
    public void UnregisterSlotTraps_RemovesAllTrapsForSlot()
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;

        registry.RegisterSlotDependent(0xC600, 6, "DISK_BOOT", TrapCategory.SlotFirmware, handler);
        registry.RegisterSlotDependent(0xC803, 6, "DISK_ENTRY", TrapCategory.SlotFirmware, handler);
        registry.RegisterSlotDependent(0xC500, 5, "OTHER_CARD", TrapCategory.SlotFirmware, handler);

        registry.UnregisterSlotTraps(6);

        Assert.Multiple(() =>
        {
            Assert.That(registry.Count, Is.EqualTo(1));
            Assert.That(registry.HasTrap(0xC600), Is.False);
            Assert.That(registry.HasTrap(0xC803), Is.False);
            Assert.That(registry.HasTrap(0xC500), Is.True);
        });
    }

    /// <summary>
    /// Verifies that UnregisterSlotTraps throws for invalid slot.
    /// </summary>
    [Test]
    public void UnregisterSlotTraps_InvalidSlot_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => registry.UnregisterSlotTraps(0));
    }

    // ─── HasTrap Tests ──────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that HasTrap returns true for registered traps.
    /// </summary>
    [Test]
    public void HasTrap_ExistingTrap_ReturnsTrue()
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;
        registry.Register(0xFC58, "HOME", TrapCategory.MonitorRom, handler);

        Assert.That(registry.HasTrap(0xFC58), Is.True);
    }

    /// <summary>
    /// Verifies that HasTrap returns false for non-existent traps.
    /// </summary>
    [Test]
    public void HasTrap_NonExistentTrap_ReturnsFalse()
    {
        Assert.That(registry.HasTrap(0xFC58), Is.False);
    }

    /// <summary>
    /// Verifies that HasTrap with operation checks specific operation.
    /// </summary>
    [Test]
    public void HasTrap_SpecificOperation_ChecksThatOperation()
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;
        registry.Register(0xC050, TrapOperation.Read, "READ_SWITCH", TrapCategory.OnboardDevice, handler);

        Assert.Multiple(() =>
        {
            Assert.That(registry.HasTrap(0xC050, TrapOperation.Read), Is.True);
            Assert.That(registry.HasTrap(0xC050, TrapOperation.Write), Is.False);
            Assert.That(registry.HasTrap(0xC050, TrapOperation.Call), Is.False);
        });
    }

    // ─── GetTrapInfo Tests ──────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that GetTrapInfo returns correct info for existing trap.
    /// </summary>
    [Test]
    public void GetTrapInfo_ExistingTrap_ReturnsCorrectInfo()
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;
        registry.Register(0xFC58, "HOME", TrapCategory.MonitorRom, handler, "Clear screen");

        var info = registry.GetTrapInfo(0xFC58);

        Assert.That(info, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(info!.Value.Address, Is.EqualTo((Addr)0xFC58));
            Assert.That(info.Value.Name, Is.EqualTo("HOME"));
            Assert.That(info.Value.Category, Is.EqualTo(TrapCategory.MonitorRom));
            Assert.That(info.Value.Operation, Is.EqualTo(TrapOperation.Call));
            Assert.That(info.Value.Description, Is.EqualTo("Clear screen"));
            Assert.That(info.Value.IsEnabled, Is.True);
            Assert.That(info.Value.SlotNumber, Is.Null);
        });
    }

    /// <summary>
    /// Verifies that GetTrapInfo returns null for non-existent trap.
    /// </summary>
    [Test]
    public void GetTrapInfo_NonExistentTrap_ReturnsNull()
    {
        var info = registry.GetTrapInfo(0xFC58);
        Assert.That(info, Is.Null);
    }

    // ─── SetEnabled Tests ───────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that SetEnabled disables an existing trap.
    /// </summary>
    [Test]
    public void SetEnabled_DisableTrap_ReturnsTrue()
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;
        registry.Register(0xFC58, "HOME", TrapCategory.MonitorRom, handler);

        bool result = registry.SetEnabled(0xFC58, false);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(registry.GetTrapInfo(0xFC58)?.IsEnabled, Is.False);
        });
    }

    /// <summary>
    /// Verifies that SetEnabled returns false for non-existent trap.
    /// </summary>
    [Test]
    public void SetEnabled_NonExistentTrap_ReturnsFalse()
    {
        bool result = registry.SetEnabled(0xFC58, false);
        Assert.That(result, Is.False);
    }

    // ─── SetCategoryEnabled Tests ───────────────────────────────────────────────

    /// <summary>
    /// Verifies that SetCategoryEnabled disables all traps in category.
    /// </summary>
    [Test]
    public void SetCategoryEnabled_DisableCategory_ReturnsCount()
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;
        registry.Register(0xFC58, "HOME", TrapCategory.MonitorRom, handler);
        registry.Register(0xFDED, "COUT", TrapCategory.MonitorRom, handler);
        registry.Register(0xD000, "BASIC", TrapCategory.BasicInterpreter, handler);

        int count = registry.SetCategoryEnabled(TrapCategory.MonitorRom, false);

        Assert.That(count, Is.EqualTo(2));
    }

    /// <summary>
    /// Verifies that disabling a category affects trap execution.
    /// </summary>
    [Test]
    public void SetCategoryEnabled_DisabledCategory_TrapNotExecuted()
    {
        bool handlerCalled = false;
        TrapHandler handler = (cpu, bus, ctx) =>
        {
            handlerCalled = true;
            return TrapResult.Success(new(10));
        };

        registry.Register(0xFC58, "HOME", TrapCategory.MonitorRom, handler);
        registry.SetCategoryEnabled(TrapCategory.MonitorRom, false);

        var result = registry.TryExecute(0xFC58, mockCpu.Object, mockBus.Object, mockContext.Object);

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.False);
            Assert.That(handlerCalled, Is.False);
        });
    }

    // ─── TryExecute Tests ───────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that TryExecute returns NotHandled when no trap exists.
    /// </summary>
    [Test]
    public void TryExecute_NoTrap_ReturnsNotHandled()
    {
        var result = registry.TryExecute(0xFC58, mockCpu.Object, mockBus.Object, mockContext.Object);
        Assert.That(result.Handled, Is.False);
    }

    /// <summary>
    /// Verifies that TryExecute invokes handler for existing enabled trap.
    /// </summary>
    [Test]
    public void TryExecute_EnabledTrap_InvokesHandler()
    {
        bool handlerCalled = false;
        TrapHandler handler = (cpu, bus, ctx) =>
        {
            handlerCalled = true;
            return TrapResult.Success(new(10));
        };

        registry.Register(0xFC58, "HOME", TrapCategory.MonitorRom, handler);

        var result = registry.TryExecute(0xFC58, mockCpu.Object, mockBus.Object, mockContext.Object);

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.True);
            Assert.That(handlerCalled, Is.True);
            Assert.That(result.CyclesConsumed.Value, Is.EqualTo(10UL));
        });
    }

    /// <summary>
    /// Verifies that TryExecute does not invoke handler for disabled trap.
    /// </summary>
    [Test]
    public void TryExecute_DisabledTrap_ReturnsNotHandled()
    {
        bool handlerCalled = false;
        TrapHandler handler = (cpu, bus, ctx) =>
        {
            handlerCalled = true;
            return TrapResult.Success(new(10));
        };

        registry.Register(0xFC58, "HOME", TrapCategory.MonitorRom, handler);
        registry.SetEnabled(0xFC58, false);

        var result = registry.TryExecute(0xFC58, mockCpu.Object, mockBus.Object, mockContext.Object);

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.False);
            Assert.That(handlerCalled, Is.False);
        });
    }

    /// <summary>
    /// Verifies that TryExecute passes correct parameters to handler.
    /// </summary>
    [Test]
    public void TryExecute_PassesCorrectParametersToHandler()
    {
        ICpu? capturedCpu = null;
        IMemoryBus? capturedBus = null;
        IEventContext? capturedContext = null;

        TrapHandler handler = (cpu, bus, ctx) =>
        {
            capturedCpu = cpu;
            capturedBus = bus;
            capturedContext = ctx;
            return TrapResult.Success(new(10));
        };

        registry.Register(0xFC58, "HOME", TrapCategory.MonitorRom, handler);
        registry.TryExecute(0xFC58, mockCpu.Object, mockBus.Object, mockContext.Object);

        Assert.Multiple(() =>
        {
            Assert.That(capturedCpu, Is.SameAs(mockCpu.Object));
            Assert.That(capturedBus, Is.SameAs(mockBus.Object));
            Assert.That(capturedContext, Is.SameAs(mockContext.Object));
        });
    }

    // ─── Context-Aware Execution Tests ──────────────────────────────────────────

    /// <summary>
    /// Verifies that slot-dependent trap fires when slot has card installed.
    /// </summary>
    [Test]
    public void TryExecute_SlotDependentTrap_SlotHasCard_ExecutesTrap()
    {
        var mockSlotManager = new Mock<ISlotManager>();
        var mockCard = new Mock<ISlotCard>();
        mockSlotManager.Setup(m => m.GetCard(6)).Returns(mockCard.Object);

        var contextRegistry = new TrapRegistry(mockSlotManager.Object);
        bool handlerCalled = false;
        TrapHandler handler = (cpu, bus, ctx) =>
        {
            handlerCalled = true;
            return TrapResult.Success(new(10));
        };

        contextRegistry.RegisterSlotDependent(0xC600, 6, "DISK_BOOT", TrapCategory.SlotFirmware, handler);

        var result = contextRegistry.TryExecute(0xC600, mockCpu.Object, mockBus.Object, mockContext.Object);

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.True);
            Assert.That(handlerCalled, Is.True);
        });
    }

    /// <summary>
    /// Verifies that slot-dependent trap does not fire when slot has no card.
    /// </summary>
    [Test]
    public void TryExecute_SlotDependentTrap_SlotEmpty_DoesNotExecute()
    {
        var mockSlotManager = new Mock<ISlotManager>();
        mockSlotManager.Setup(m => m.GetCard(6)).Returns((ISlotCard?)null);

        var contextRegistry = new TrapRegistry(mockSlotManager.Object);
        bool handlerCalled = false;
        TrapHandler handler = (cpu, bus, ctx) =>
        {
            handlerCalled = true;
            return TrapResult.Success(new(10));
        };

        contextRegistry.RegisterSlotDependent(0xC600, 6, "DISK_BOOT", TrapCategory.SlotFirmware, handler);

        var result = contextRegistry.TryExecute(0xC600, mockCpu.Object, mockBus.Object, mockContext.Object);

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.False);
            Assert.That(handlerCalled, Is.False);
        });
    }

    /// <summary>
    /// Verifies that expansion ROM trap fires when correct slot is active.
    /// </summary>
    [Test]
    public void TryExecute_ExpansionRomTrap_CorrectSlotActive_ExecutesTrap()
    {
        var mockSlotManager = new Mock<ISlotManager>();
        var mockCard = new Mock<ISlotCard>();
        mockSlotManager.Setup(m => m.GetCard(6)).Returns(mockCard.Object);
        mockSlotManager.Setup(m => m.ActiveExpansionSlot).Returns(6);

        var contextRegistry = new TrapRegistry(mockSlotManager.Object);
        bool handlerCalled = false;
        TrapHandler handler = (cpu, bus, ctx) =>
        {
            handlerCalled = true;
            return TrapResult.Success(new(10));
        };

        contextRegistry.RegisterSlotDependent(0xC803, 6, "DISK_ENTRY", TrapCategory.SlotFirmware, handler);

        var result = contextRegistry.TryExecute(0xC803, mockCpu.Object, mockBus.Object, mockContext.Object);

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.True);
            Assert.That(handlerCalled, Is.True);
        });
    }

    /// <summary>
    /// Verifies that expansion ROM trap does not fire when different slot is active.
    /// </summary>
    [Test]
    public void TryExecute_ExpansionRomTrap_DifferentSlotActive_DoesNotExecute()
    {
        var mockSlotManager = new Mock<ISlotManager>();
        var mockCard = new Mock<ISlotCard>();
        mockSlotManager.Setup(m => m.GetCard(6)).Returns(mockCard.Object);
        mockSlotManager.Setup(m => m.ActiveExpansionSlot).Returns(5); // Different slot

        var contextRegistry = new TrapRegistry(mockSlotManager.Object);
        bool handlerCalled = false;
        TrapHandler handler = (cpu, bus, ctx) =>
        {
            handlerCalled = true;
            return TrapResult.Success(new(10));
        };

        contextRegistry.RegisterSlotDependent(0xC803, 6, "DISK_ENTRY", TrapCategory.SlotFirmware, handler);

        var result = contextRegistry.TryExecute(0xC803, mockCpu.Object, mockBus.Object, mockContext.Object);

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.False);
            Assert.That(handlerCalled, Is.False);
        });
    }

    /// <summary>
    /// Verifies that slot-dependent trap without slot manager does not fire.
    /// </summary>
    [Test]
    public void TryExecute_SlotDependentTrap_NoSlotManager_DoesNotExecute()
    {
        bool handlerCalled = false;
        TrapHandler handler = (cpu, bus, ctx) =>
        {
            handlerCalled = true;
            return TrapResult.Success(new(10));
        };

        registry.RegisterSlotDependent(0xC600, 6, "DISK_BOOT", TrapCategory.SlotFirmware, handler);

        var result = registry.TryExecute(0xC600, mockCpu.Object, mockBus.Object, mockContext.Object);

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.False);
            Assert.That(handlerCalled, Is.False);
        });
    }

    /// <summary>
    /// Verifies that ROM trap fires when Language Card RAM is disabled.
    /// </summary>
    [Test]
    public void TryExecute_RomTrap_LanguageCardRamDisabled_ExecutesTrap()
    {
        var mockSlotManager = new Mock<ISlotManager>();
        var (mockLanguageCard, setRamEnabled) = CreateMockLanguageCard();

        // By default, RAM read is disabled (ROM is visible)
        Assert.That(mockLanguageCard.Object.IsRamReadEnabled, Is.False, "Precondition: RAM should be disabled");

        var contextRegistry = new TrapRegistry(mockSlotManager.Object, mockLanguageCard.Object);
        bool handlerCalled = false;
        TrapHandler handler = (cpu, bus, ctx) =>
        {
            handlerCalled = true;
            return TrapResult.Success(new(10));
        };

        contextRegistry.Register(0xD000, "APPLESOFT", TrapCategory.BasicInterpreter, handler);

        var result = contextRegistry.TryExecute(0xD000, mockCpu.Object, mockBus.Object, mockContext.Object);

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.True, "Trap should execute when ROM is visible");
            Assert.That(handlerCalled, Is.True);
        });
    }

    /// <summary>
    /// Verifies that ROM trap does not fire when Language Card RAM is enabled.
    /// </summary>
    [Test]
    public void TryExecute_RomTrap_LanguageCardRamEnabled_DoesNotExecute()
    {
        var mockSlotManager = new Mock<ISlotManager>();
        var (mockLanguageCard, setRamEnabled) = CreateMockLanguageCard();

        // Enable RAM read
        setRamEnabled(true);
        Assert.That(mockLanguageCard.Object.IsRamReadEnabled, Is.True, "Precondition: RAM should be enabled");

        var contextRegistry = new TrapRegistry(mockSlotManager.Object, mockLanguageCard.Object);
        bool handlerCalled = false;
        TrapHandler handler = (cpu, bus, ctx) =>
        {
            handlerCalled = true;
            return TrapResult.Success(new(10));
        };

        contextRegistry.Register(0xD000, "APPLESOFT", TrapCategory.BasicInterpreter, handler);

        var result = contextRegistry.TryExecute(0xD000, mockCpu.Object, mockBus.Object, mockContext.Object);

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.False, "Trap should not execute when RAM is visible");
            Assert.That(handlerCalled, Is.False);
        });
    }

    /// <summary>
    /// Verifies that traps below $D000 are not affected by Language Card state.
    /// </summary>
    [Test]
    public void TryExecute_TrapBelowD000_LanguageCardRamEnabled_StillExecutes()
    {
        var mockSlotManager = new Mock<ISlotManager>();
        var (mockLanguageCard, setRamEnabled) = CreateMockLanguageCard();

        // Enable RAM read
        setRamEnabled(true);

        var contextRegistry = new TrapRegistry(mockSlotManager.Object, mockLanguageCard.Object);
        bool handlerCalled = false;
        TrapHandler handler = (cpu, bus, ctx) =>
        {
            handlerCalled = true;
            return TrapResult.Success(new(10));
        };

        // Trap at $CFFF is below $D000 and should not be affected by LC state
        contextRegistry.Register(0xCFFF, "CFFF_RELEASE", TrapCategory.MonitorRom, handler);

        var result = contextRegistry.TryExecute(0xCFFF, mockCpu.Object, mockBus.Object, mockContext.Object);

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.True);
            Assert.That(handlerCalled, Is.True);
        });
    }

    // ─── Clear Tests ────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that Clear removes all traps.
    /// </summary>
    [Test]
    public void Clear_RemovesAllTraps()
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;
        registry.Register(0xFC58, "HOME", TrapCategory.MonitorRom, handler);
        registry.Register(0xFDED, "COUT", TrapCategory.MonitorRom, handler);

        registry.Clear();

        Assert.That(registry.Count, Is.EqualTo(0));
    }

    // ─── GetRegisteredAddresses Tests ───────────────────────────────────────────

    /// <summary>
    /// Verifies that GetRegisteredAddresses returns all unique addresses.
    /// </summary>
    [Test]
    public void GetRegisteredAddresses_ReturnsAllUniqueAddresses()
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;
        registry.Register(0xFC58, "HOME", TrapCategory.MonitorRom, handler);
        registry.Register(0xFDED, "COUT", TrapCategory.MonitorRom, handler);
        registry.Register(0xC050, TrapOperation.Read, "READ_SWITCH", TrapCategory.OnboardDevice, handler);
        registry.Register(0xC050, TrapOperation.Write, "WRITE_SWITCH", TrapCategory.OnboardDevice, handler);

        var addresses = registry.GetRegisteredAddresses().ToList();

        Assert.That(addresses, Has.Count.EqualTo(3));
        Assert.That(addresses, Does.Contain((Addr)0xFC58));
        Assert.That(addresses, Does.Contain((Addr)0xFDED));
        Assert.That(addresses, Does.Contain((Addr)0xC050));
    }

    // ─── GetAllTraps Tests ──────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that GetAllTraps returns all registered trap info.
    /// </summary>
    [Test]
    public void GetAllTraps_ReturnsAllTrapInfo()
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;
        registry.Register(0xFC58, "HOME", TrapCategory.MonitorRom, handler);
        registry.Register(0xFDED, "COUT", TrapCategory.MonitorRom, handler);

        var traps = registry.GetAllTraps().ToList();

        Assert.That(traps, Has.Count.EqualTo(2));
    }

    // ─── Language Card RAM Trap Tests ───────────────────────────────────────────

    /// <summary>
    /// Verifies that RegisterLanguageCardRam adds a trap targeting LC RAM.
    /// </summary>
    [Test]
    public void RegisterLanguageCardRam_ValidTrap_IncrementsCount()
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;

        registry.RegisterLanguageCardRam(0xD000, "LC_RAM_ENTRY", TrapCategory.BasicInterpreter, handler);

        Assert.That(registry.Count, Is.EqualTo(1));
    }

    /// <summary>
    /// Verifies that separate traps can be registered for ROM and LC RAM at the same address.
    /// </summary>
    [Test]
    public void RegisterLanguageCardRam_SameAddressAsRom_AllowsBothTraps()
    {
        TrapHandler romHandler = (cpu, bus, ctx) => TrapResult.Success(new(10));
        TrapHandler lcHandler = (cpu, bus, ctx) => TrapResult.Success(new(20));

        registry.Register(0xD000, "ROM_ENTRY", TrapCategory.BasicInterpreter, romHandler);
        registry.RegisterLanguageCardRam(0xD000, "LC_RAM_ENTRY", TrapCategory.BasicInterpreter, lcHandler);

        Assert.That(registry.Count, Is.EqualTo(2));
    }

    /// <summary>
    /// Verifies that LC RAM trap fires when Language Card RAM is enabled.
    /// </summary>
    [Test]
    public void TryExecute_LcRamTrap_LanguageCardRamEnabled_ExecutesTrap()
    {
        var mockSlotManager = new Mock<ISlotManager>();
        var (mockLanguageCard, setRamEnabled) = CreateMockLanguageCard();

        // Enable RAM read
        setRamEnabled(true);
        Assert.That(mockLanguageCard.Object.IsRamReadEnabled, Is.True, "Precondition: RAM should be enabled");

        var contextRegistry = new TrapRegistry(mockSlotManager.Object, mockLanguageCard.Object);
        bool handlerCalled = false;
        TrapHandler handler = (cpu, bus, ctx) =>
        {
            handlerCalled = true;
            return TrapResult.Success(new(10));
        };

        contextRegistry.RegisterLanguageCardRam(0xD000, "LC_RAM_ENTRY", TrapCategory.BasicInterpreter, handler);

        var result = contextRegistry.TryExecute(0xD000, mockCpu.Object, mockBus.Object, mockContext.Object);

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.True, "LC RAM trap should execute when LC RAM is active");
            Assert.That(handlerCalled, Is.True);
        });
    }

    /// <summary>
    /// Verifies that LC RAM trap does not fire when Language Card RAM is disabled.
    /// </summary>
    [Test]
    public void TryExecute_LcRamTrap_LanguageCardRamDisabled_DoesNotExecute()
    {
        var mockSlotManager = new Mock<ISlotManager>();
        var (mockLanguageCard, setRamEnabled) = CreateMockLanguageCard();

        // By default, RAM read is disabled (ROM is visible)
        Assert.That(mockLanguageCard.Object.IsRamReadEnabled, Is.False, "Precondition: RAM should be disabled");

        var contextRegistry = new TrapRegistry(mockSlotManager.Object, mockLanguageCard.Object);
        bool handlerCalled = false;
        TrapHandler handler = (cpu, bus, ctx) =>
        {
            handlerCalled = true;
            return TrapResult.Success(new(10));
        };

        contextRegistry.RegisterLanguageCardRam(0xD000, "LC_RAM_ENTRY", TrapCategory.BasicInterpreter, handler);

        var result = contextRegistry.TryExecute(0xD000, mockCpu.Object, mockBus.Object, mockContext.Object);

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.False, "LC RAM trap should not execute when ROM is visible");
            Assert.That(handlerCalled, Is.False);
        });
    }

    /// <summary>
    /// Verifies that the correct trap fires based on Language Card state when both ROM and LC RAM traps exist.
    /// </summary>
    [Test]
    public void TryExecute_BothRomAndLcRamTraps_SelectsCorrectTrap()
    {
        var mockSlotManager = new Mock<ISlotManager>();
        var (mockLanguageCard, setRamEnabled) = CreateMockLanguageCard();

        var contextRegistry = new TrapRegistry(mockSlotManager.Object, mockLanguageCard.Object);

        bool romHandlerCalled = false;
        bool lcHandlerCalled = false;

        TrapHandler romHandler = (cpu, bus, ctx) =>
        {
            romHandlerCalled = true;
            return TrapResult.Success(new(10));
        };

        TrapHandler lcHandler = (cpu, bus, ctx) =>
        {
            lcHandlerCalled = true;
            return TrapResult.Success(new(20));
        };

        contextRegistry.Register(0xD000, "ROM_ENTRY", TrapCategory.BasicInterpreter, romHandler);
        contextRegistry.RegisterLanguageCardRam(0xD000, "LC_RAM_ENTRY", TrapCategory.BasicInterpreter, lcHandler);

        // With ROM visible (LC RAM disabled), ROM trap should fire
        var result1 = contextRegistry.TryExecute(0xD000, mockCpu.Object, mockBus.Object, mockContext.Object);
        Assert.Multiple(() =>
        {
            Assert.That(result1.Handled, Is.True, "ROM trap should execute when ROM is visible");
            Assert.That(romHandlerCalled, Is.True, "ROM handler should be called");
            Assert.That(lcHandlerCalled, Is.False, "LC handler should not be called");
        });

        // Reset flags
        romHandlerCalled = false;
        lcHandlerCalled = false;

        // Enable LC RAM
        setRamEnabled(true);
        Assert.That(mockLanguageCard.Object.IsRamReadEnabled, Is.True);

        // Now LC RAM trap should fire
        var result2 = contextRegistry.TryExecute(0xD000, mockCpu.Object, mockBus.Object, mockContext.Object);
        Assert.Multiple(() =>
        {
            Assert.That(result2.Handled, Is.True, "LC RAM trap should execute when LC RAM is active");
            Assert.That(lcHandlerCalled, Is.True, "LC handler should be called");
            Assert.That(romHandlerCalled, Is.False, "ROM handler should not be called");
        });
    }

    /// <summary>
    /// Verifies that UnregisterLanguageCardRam removes the LC RAM trap.
    /// </summary>
    [Test]
    public void UnregisterLanguageCardRam_RemovesLcRamTrap()
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;
        registry.RegisterLanguageCardRam(0xD000, "LC_RAM_ENTRY", TrapCategory.BasicInterpreter, handler);

        bool result = registry.UnregisterLanguageCardRam(0xD000);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    /// Verifies that UnregisterLanguageCardRam only removes LC RAM trap, not ROM trap.
    /// </summary>
    [Test]
    public void UnregisterLanguageCardRam_OnlyRemovesLcRamTrap()
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;
        registry.Register(0xD000, "ROM_ENTRY", TrapCategory.BasicInterpreter, handler);
        registry.RegisterLanguageCardRam(0xD000, "LC_RAM_ENTRY", TrapCategory.BasicInterpreter, handler);

        registry.UnregisterLanguageCardRam(0xD000);

        Assert.Multiple(() =>
        {
            Assert.That(registry.Count, Is.EqualTo(1));
            Assert.That(registry.HasTrap(0xD000), Is.True, "ROM trap should still exist");
        });
    }

    /// <summary>
    /// Verifies that HasTrap returns true when either ROM or LC RAM trap exists.
    /// </summary>
    [Test]
    public void HasTrap_ReturnsTrueForEitherRomOrLcRamTrap()
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;

        // Only LC RAM trap
        registry.RegisterLanguageCardRam(0xD000, "LC_RAM_ENTRY", TrapCategory.BasicInterpreter, handler);
        Assert.That(registry.HasTrap(0xD000), Is.True, "Should return true for LC RAM trap");

        // Add ROM trap too
        registry.Register(0xD000, "ROM_ENTRY", TrapCategory.BasicInterpreter, handler);
        Assert.That(registry.HasTrap(0xD000), Is.True, "Should return true when both traps exist");

        // Remove ROM trap
        registry.Unregister(0xD000);
        Assert.That(registry.HasTrap(0xD000), Is.True, "Should still return true for LC RAM trap");

        // Remove LC RAM trap
        registry.UnregisterLanguageCardRam(0xD000);
        Assert.That(registry.HasTrap(0xD000), Is.False, "Should return false when no traps exist");
    }

    /// <summary>
    /// Verifies that RegisterLanguageCardRam throws when duplicate LC RAM trap exists.
    /// </summary>
    [Test]
    public void RegisterLanguageCardRam_DuplicateLcRamTrap_ThrowsInvalidOperationException()
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;
        registry.RegisterLanguageCardRam(0xD000, "LC_RAM_ENTRY", TrapCategory.BasicInterpreter, handler);

        Assert.Throws<InvalidOperationException>(() =>
            registry.RegisterLanguageCardRam(0xD000, "LC_RAM_ENTRY2", TrapCategory.BasicInterpreter, handler));
    }

    /// <summary>
    /// Verifies that TrapInfo includes TargetsLcRam flag.
    /// </summary>
    [Test]
    public void GetTrapInfo_LcRamTrap_ReturnsCorrectInfo()
    {
        var mockSlotManager = new Mock<ISlotManager>();
        var (mockLanguageCard, setRamEnabled) = CreateMockLanguageCard();

        // Enable LC RAM to make LC RAM trap visible
        setRamEnabled(true);

        var contextRegistry = new TrapRegistry(mockSlotManager.Object, mockLanguageCard.Object);
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;
        contextRegistry.RegisterLanguageCardRam(0xD000, "LC_RAM_ENTRY", TrapCategory.BasicInterpreter, handler, "Test LC RAM trap");

        var allTraps = contextRegistry.GetAllTraps().ToList();
        var lcRamTrap = allTraps.First();

        Assert.Multiple(() =>
        {
            Assert.That(lcRamTrap.Address, Is.EqualTo((Addr)0xD000));
            Assert.That(lcRamTrap.Name, Is.EqualTo("LC_RAM_ENTRY"));
            Assert.That(lcRamTrap.TargetsLcRam, Is.True);
        });
    }

    /// <summary>
    /// Verifies that GetTrapInfo with MemoryContext parameter returns correct trap.
    /// </summary>
    [Test]
    public void GetTrapInfo_WithMemoryContext_ReturnsCorrectTrap()
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;
        registry.Register(0xD000, "ROM_ENTRY", TrapCategory.BasicInterpreter, handler);
        registry.RegisterLanguageCardRam(0xD000, "LC_RAM_ENTRY", TrapCategory.BasicInterpreter, handler);

        var romInfo = registry.GetTrapInfo(0xD000, TrapOperation.Call, MemoryContexts.Rom);
        var lcInfo = registry.GetTrapInfo(0xD000, TrapOperation.Call, MemoryContexts.LanguageCardRam);

        Assert.Multiple(() =>
        {
            Assert.That(romInfo, Is.Not.Null);
            Assert.That(romInfo!.Value.Name, Is.EqualTo("ROM_ENTRY"));
            Assert.That(romInfo.Value.TargetsLcRam, Is.False);

            Assert.That(lcInfo, Is.Not.Null);
            Assert.That(lcInfo!.Value.Name, Is.EqualTo("LC_RAM_ENTRY"));
            Assert.That(lcInfo.Value.TargetsLcRam, Is.True);
        });
    }

    /// <summary>
    /// Verifies that GetTrapsAtAddress returns all traps at the address.
    /// </summary>
    [Test]
    public void GetTrapsAtAddress_ReturnsBothRomAndLcRamTraps()
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;
        registry.Register(0xD000, "ROM_ENTRY", TrapCategory.BasicInterpreter, handler);
        registry.RegisterLanguageCardRam(0xD000, "LC_RAM_ENTRY", TrapCategory.BasicInterpreter, handler);

        var traps = registry.GetTrapsAtAddress(0xD000).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(traps, Has.Count.EqualTo(2));
            Assert.That(traps.Any(t => t.Name == "ROM_ENTRY" && !t.TargetsLcRam), Is.True);
            Assert.That(traps.Any(t => t.Name == "LC_RAM_ENTRY" && t.TargetsLcRam), Is.True);
        });
    }

    /// <summary>
    /// Verifies that SetEnabled with MemoryContext enables/disables correct trap.
    /// </summary>
    [Test]
    public void SetEnabled_WithMemoryContext_EnablesDisablesCorrectTrap()
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;
        registry.Register(0xD000, "ROM_ENTRY", TrapCategory.BasicInterpreter, handler);
        registry.RegisterLanguageCardRam(0xD000, "LC_RAM_ENTRY", TrapCategory.BasicInterpreter, handler);

        // Disable LC RAM trap
        registry.SetEnabled(0xD000, TrapOperation.Call, enabled: false, MemoryContexts.LanguageCardRam);

        var romInfo = registry.GetTrapInfo(0xD000, TrapOperation.Call, MemoryContexts.Rom);
        var lcInfo = registry.GetTrapInfo(0xD000, TrapOperation.Call, MemoryContexts.LanguageCardRam);

        Assert.Multiple(() =>
        {
            Assert.That(romInfo!.Value.IsEnabled, Is.True, "ROM trap should still be enabled");
            Assert.That(lcInfo!.Value.IsEnabled, Is.False, "LC RAM trap should be disabled");
        });

        // Re-enable LC RAM trap
        registry.SetEnabled(0xD000, TrapOperation.Call, enabled: true, MemoryContexts.LanguageCardRam);
        lcInfo = registry.GetTrapInfo(0xD000, TrapOperation.Call, MemoryContexts.LanguageCardRam);
        Assert.That(lcInfo!.Value.IsEnabled, Is.True, "LC RAM trap should be re-enabled");
    }

    /// <summary>
    /// Verifies that Clear also clears disabled categories.
    /// </summary>
    [Test]
    public void Clear_AlsoClearsDisabledCategories()
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.Success(new(10));
        registry.Register(0xD000, "TEST", TrapCategory.BasicInterpreter, handler);

        // Disable the category
        registry.SetCategoryEnabled(TrapCategory.BasicInterpreter, false);

        // Clear all
        registry.Clear();

        // Register a new trap in the same category
        registry.Register(0xD000, "TEST2", TrapCategory.BasicInterpreter, handler);

        // Execute should work because the category should be re-enabled after clear
        var result = registry.TryExecute(0xD000, mockCpu.Object, mockBus.Object, mockContext.Object);
        Assert.That(result.Handled, Is.True, "Category should not be disabled after Clear()");
    }

    /// <summary>
    /// Verifies that RegisterWithContext registers trap with custom memory context.
    /// </summary>
    [Test]
    public void RegisterWithContext_CustomContext_RegistersTrap()
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.Success(new(10));
        var customContext = MemoryContexts.Custom("PRODOS_RAM");

        registry.RegisterWithContext(0xD000, customContext, "PRODOS_ENTRY", TrapCategory.OperatingSystem, handler);

        Assert.That(registry.Count, Is.EqualTo(1));

        var trapInfo = registry.GetTrapInfo(0xD000, TrapOperation.Call, customContext);
        Assert.That(trapInfo, Is.Not.Null);
        Assert.That(trapInfo!.Value.Name, Is.EqualTo("PRODOS_ENTRY"));
        Assert.That(trapInfo.Value.MemoryContext, Is.EqualTo(customContext));
    }

    /// <summary>
    /// Verifies that GetRegisteredContexts returns all unique contexts.
    /// </summary>
    [Test]
    public void GetRegisteredContexts_ReturnsAllUniqueContexts()
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;
        var customContext = MemoryContexts.Custom("CUSTOM");

        registry.Register(0xFC58, "HOME", TrapCategory.MonitorRom, handler);
        registry.RegisterLanguageCardRam(0xD000, "LC_ENTRY", TrapCategory.BasicInterpreter, handler);
        registry.RegisterWithContext(0xE000, customContext, "CUSTOM_ENTRY", TrapCategory.UserDefined, handler);

        var contexts = registry.GetRegisteredContexts().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(contexts, Has.Count.EqualTo(3));
            Assert.That(contexts, Does.Contain(MemoryContexts.Rom));
            Assert.That(contexts, Does.Contain(MemoryContexts.LanguageCardRam));
            Assert.That(contexts, Does.Contain(customContext));
        });
    }

    /// <summary>
    /// Verifies that UnregisterContextTraps removes all traps in a context.
    /// </summary>
    [Test]
    public void UnregisterContextTraps_RemovesAllTrapsInContext()
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;
        var customContext = MemoryContexts.Custom("CUSTOM");

        registry.RegisterWithContext(0xD000, customContext, "ENTRY1", TrapCategory.UserDefined, handler);
        registry.RegisterWithContext(0xE000, customContext, "ENTRY2", TrapCategory.UserDefined, handler);
        registry.Register(0xFC58, "HOME", TrapCategory.MonitorRom, handler);

        Assert.That(registry.Count, Is.EqualTo(3));

        registry.UnregisterContextTraps(customContext);

        Assert.Multiple(() =>
        {
            Assert.That(registry.Count, Is.EqualTo(1));
            Assert.That(registry.HasTrap(0xFC58), Is.True, "ROM trap should still exist");
            Assert.That(registry.HasTrap(0xD000), Is.False, "Custom context trap should be removed");
            Assert.That(registry.HasTrap(0xE000), Is.False, "Custom context trap should be removed");
        });
    }

    /// <summary>
    /// Verifies that multiple traps can coexist at same address with different contexts.
    /// </summary>
    [Test]
    public void Register_MultipleContextsSameAddress_AllCoexist()
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;

        registry.Register(0xD000, "ROM", TrapCategory.BasicInterpreter, handler);
        registry.RegisterLanguageCardRam(0xD000, "LC_RAM", TrapCategory.BasicInterpreter, handler);
        registry.RegisterWithContext(0xD000, MemoryContexts.AuxiliaryRam, "AUX_RAM", TrapCategory.BasicInterpreter, handler);

        Assert.That(registry.Count, Is.EqualTo(3));

        var traps = registry.GetTrapsAtAddress(0xD000).ToList();
        Assert.That(traps, Has.Count.EqualTo(3));
    }

    /// <summary>
    /// Verifies that TrapRegistered event is raised when a trap is registered.
    /// </summary>
    [Test]
    public void TrapRegistered_WhenRegisteringTrap_RaisesEvent()
    {
        var localRegistry = new TrapRegistry();
        TrapInfo? raisedInfo = null;
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;

        localRegistry.TrapRegistered += info => raisedInfo = info;

        localRegistry.Register(0xFC58, "HOME", TrapCategory.MonitorRom, handler);

        Assert.That(raisedInfo, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(raisedInfo!.Value.Address, Is.EqualTo((Addr)0xFC58));
            Assert.That(raisedInfo!.Value.Name, Is.EqualTo("HOME"));
            Assert.That(raisedInfo!.Value.Category, Is.EqualTo(TrapCategory.MonitorRom));
        });
    }

    /// <summary>
    /// Verifies that TrapUnregistered event is raised when a trap is unregistered.
    /// </summary>
    [Test]
    public void TrapUnregistered_WhenUnregisteringTrap_RaisesEvent()
    {
        var localRegistry = new TrapRegistry();
        Addr? raisedAddress = null;
        TrapOperation? raisedOperation = null;
        MemoryContext? raisedContext = null;
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;

        localRegistry.TrapUnregistered += (addr, op, ctx) =>
        {
            raisedAddress = addr;
            raisedOperation = op;
            raisedContext = ctx;
        };

        localRegistry.Register(0xFDED, "COUT", TrapCategory.MonitorRom, handler);
        localRegistry.Unregister(0xFDED);

        Assert.Multiple(() =>
        {
            Assert.That(raisedAddress, Is.EqualTo((Addr)0xFDED));
            Assert.That(raisedOperation, Is.EqualTo(TrapOperation.Call));
            Assert.That(raisedContext, Is.EqualTo(MemoryContexts.Rom));
        });
    }

    /// <summary>
    /// Verifies that TrapEnabledChanged event is raised when a trap's enabled state changes.
    /// </summary>
    [Test]
    public void TrapEnabledChanged_WhenSettingEnabled_RaisesEvent()
    {
        var localRegistry = new TrapRegistry();
        bool? newEnabledState = null;
        var eventRaised = false;
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;

        localRegistry.TrapEnabledChanged += (addr, op, ctx, enabled) =>
        {
            newEnabledState = enabled;
            eventRaised = true;
        };

        localRegistry.Register(0xFC58, "HOME", TrapCategory.MonitorRom, handler);
        localRegistry.SetEnabled(0xFC58, false);

        Assert.Multiple(() =>
        {
            Assert.That(eventRaised, Is.True);
            Assert.That(newEnabledState, Is.False);
        });
    }

    /// <summary>
    /// Verifies that TrapEnabledChanged event is not raised when setting the same enabled state.
    /// </summary>
    [Test]
    public void TrapEnabledChanged_WhenSettingSameState_DoesNotRaiseEvent()
    {
        var localRegistry = new TrapRegistry();
        var eventCount = 0;
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;

        localRegistry.TrapEnabledChanged += (_, _, _, _) => eventCount++;

        localRegistry.Register(0xFC58, "HOME", TrapCategory.MonitorRom, handler);

        // Initially enabled, setting to enabled again should not raise
        localRegistry.SetEnabled(0xFC58, true);

        Assert.That(eventCount, Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies that TrapInvoked event is raised when a trap is executed.
    /// </summary>
    [Test]
    public void TrapInvoked_WhenExecutingTrap_RaisesEvent()
    {
        // Arrange - Create scheduler and context mocks for TryExecute
        var mockScheduler = new Mock<IScheduler>();
        mockScheduler.Setup(s => s.Now).Returns(new Cycle(12345));
        mockContext.Setup(c => c.Scheduler).Returns(mockScheduler.Object);

        var localRegistry = new TrapRegistry();
        TrapInfo? raisedInfo = null;
        TrapResult? raisedResult = null;
        Cycle? raisedCycle = null;
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.Success(new Cycle(10));

        localRegistry.TrapInvoked += (info, result, cycle) =>
        {
            raisedInfo = info;
            raisedResult = result;
            raisedCycle = cycle;
        };

        localRegistry.Register(0xFC58, "HOME", TrapCategory.MonitorRom, handler);

        // Act
        localRegistry.TryExecute(0xFC58, mockCpu.Object, mockBus.Object, mockContext.Object);

        // Assert
        Assert.That(raisedInfo, Is.Not.Null);
        Assert.That(raisedResult, Is.Not.Null);
        Assert.That(raisedCycle, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(raisedInfo!.Value.Address, Is.EqualTo((Addr)0xFC58));
            Assert.That(raisedInfo!.Value.Name, Is.EqualTo("HOME"));
            Assert.That(raisedResult!.Value.Handled, Is.True);
            Assert.That(raisedResult!.Value.CyclesConsumed.Value, Is.EqualTo(10ul));
            Assert.That(raisedCycle!.Value.Value, Is.EqualTo(12345ul));
        });
    }

    // ─── Helper Methods ─────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a mock Language Card state for testing trap context-awareness.
    /// </summary>
    /// <returns>A tuple containing the mock Language Card state and a function to enable/disable RAM.</returns>
    private static (Mock<ILanguageCardState> MockLanguageCard, Action<bool> SetRamEnabled) CreateMockLanguageCard()
    {
        var mockLanguageCard = new Mock<ILanguageCardState>();
        bool ramEnabled = false;

        mockLanguageCard.Setup(lc => lc.IsRamReadEnabled).Returns(() => ramEnabled);
        mockLanguageCard.Setup(lc => lc.IsRamWriteEnabled).Returns(() => ramEnabled);
        mockLanguageCard.Setup(lc => lc.SelectedBank).Returns(1);

        void SetRamEnabled(bool enabled)
        {
            ramEnabled = enabled;
        }

        return (mockLanguageCard, SetRamEnabled);
    }

    /// <summary>
    /// Creates a mock event context with the specified bus.
    /// </summary>
    /// <param name="bus">The memory bus to configure in the mock context.</param>
    /// <returns>A mock <see cref="IEventContext"/> with the specified bus.</returns>
    private static IEventContext CreateMockEventContext(IMemoryBus bus)
    {
        var mockContext = new Mock<IEventContext>();
        mockContext.Setup(c => c.Bus).Returns(bus);
        return mockContext.Object;
    }

    /// <summary>
    /// Helper method to create test bus access structures.
    /// </summary>
    /// <param name="address">The memory address for the access.</param>
    /// <param name="intent">The access intent (read or write).</param>
    /// <param name="flags">Optional access flags.</param>
    /// <returns>A <see cref="BusAccess"/> configured with the specified parameters.</returns>
    private static BusAccess CreateTestAccess(
        Addr address,
        AccessIntent intent,
        AccessFlags flags = AccessFlags.None)
    {
        return new(
            Address: address,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: intent,
            SourceId: 0,
            Cycle: 0,
            Flags: flags);
    }
}