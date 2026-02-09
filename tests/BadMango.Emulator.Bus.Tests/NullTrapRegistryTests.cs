// <copyright file="NullTrapRegistryTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core.Interfaces.Cpu;

using Moq;

/// <summary>
/// Unit tests for the <see cref="NullTrapRegistry"/> class.
/// </summary>
[TestFixture]
public class NullTrapRegistryTests
{
    /// <summary>
    /// Verifies that the singleton instance is not null.
    /// </summary>
    [Test]
    public void Instance_IsNotNull()
    {
        Assert.That(NullTrapRegistry.Instance, Is.Not.Null);
    }

    /// <summary>
    /// Verifies that Count always returns zero.
    /// </summary>
    [Test]
    public void Count_AlwaysReturnsZero()
    {
        Assert.That(NullTrapRegistry.Instance.Count, Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies that ContainsAddress always returns false.
    /// </summary>
    [Test]
    public void ContainsAddress_AlwaysReturnsFalse()
    {
        Assert.That(NullTrapRegistry.Instance.ContainsAddress(0xFC58), Is.False);
    }

    /// <summary>
    /// Verifies that HasTrap with address always returns false.
    /// </summary>
    [Test]
    public void HasTrap_Address_AlwaysReturnsFalse()
    {
        Assert.That(NullTrapRegistry.Instance.HasTrap(0xFC58), Is.False);
    }

    /// <summary>
    /// Verifies that HasTrap with operation always returns false.
    /// </summary>
    [Test]
    public void HasTrap_WithOperation_AlwaysReturnsFalse()
    {
        Assert.That(NullTrapRegistry.Instance.HasTrap(0xFC58, TrapOperation.Call), Is.False);
    }

    /// <summary>
    /// Verifies that TryExecute always returns NotHandled.
    /// </summary>
    [Test]
    public void TryExecute_AlwaysReturnsNotHandled()
    {
        var mockCpu = new Mock<ICpu>();
        var mockBus = new Mock<IMemoryBus>();
        var mockContext = new Mock<IEventContext>();

        var result = NullTrapRegistry.Instance.TryExecute(0xFC58, mockCpu.Object, mockBus.Object, mockContext.Object);

        Assert.That(result.Handled, Is.False);
    }

    /// <summary>
    /// Verifies that TryExecute with operation always returns NotHandled.
    /// </summary>
    [Test]
    public void TryExecute_WithOperation_AlwaysReturnsNotHandled()
    {
        var mockCpu = new Mock<ICpu>();
        var mockBus = new Mock<IMemoryBus>();
        var mockContext = new Mock<IEventContext>();

        var result = NullTrapRegistry.Instance.TryExecute(0xFC58, TrapOperation.Call, mockCpu.Object, mockBus.Object, mockContext.Object);

        Assert.That(result.Handled, Is.False);
    }

    /// <summary>
    /// Verifies that Register throws NotSupportedException.
    /// </summary>
    [Test]
    public void Register_ThrowsNotSupportedException()
    {
        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;

        Assert.Throws<NotSupportedException>(() =>
            NullTrapRegistry.Instance.Register(0xFC58, "HOME", TrapCategory.MonitorRom, handler));
    }

    /// <summary>
    /// Verifies that GetAllTraps returns empty.
    /// </summary>
    [Test]
    public void GetAllTraps_ReturnsEmpty()
    {
        Assert.That(NullTrapRegistry.Instance.GetAllTraps(), Is.Empty);
    }

    /// <summary>
    /// Verifies that GetRegisteredAddresses returns empty.
    /// </summary>
    [Test]
    public void GetRegisteredAddresses_ReturnsEmpty()
    {
        Assert.That(NullTrapRegistry.Instance.GetRegisteredAddresses(), Is.Empty);
    }

    /// <summary>
    /// Verifies that GetTrapInfo returns null.
    /// </summary>
    [Test]
    public void GetTrapInfo_ReturnsNull()
    {
        Assert.That(NullTrapRegistry.Instance.GetTrapInfo(0xFC58), Is.Null);
    }

    /// <summary>
    /// Verifies that Unregister returns false.
    /// </summary>
    [Test]
    public void Unregister_ReturnsFalse()
    {
        Assert.That(NullTrapRegistry.Instance.Unregister(0xFC58), Is.False);
    }

    /// <summary>
    /// Verifies that SetEnabled returns false.
    /// </summary>
    [Test]
    public void SetEnabled_ReturnsFalse()
    {
        Assert.That(NullTrapRegistry.Instance.SetEnabled(0xFC58, true), Is.False);
    }

    /// <summary>
    /// Verifies that SetCategoryEnabled returns zero.
    /// </summary>
    [Test]
    public void SetCategoryEnabled_ReturnsZero()
    {
        Assert.That(NullTrapRegistry.Instance.SetCategoryEnabled(TrapCategory.MonitorRom, true), Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies that Clear does not throw.
    /// </summary>
    [Test]
    public void Clear_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => NullTrapRegistry.Instance.Clear());
    }
}