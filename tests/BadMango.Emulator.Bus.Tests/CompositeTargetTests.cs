// <copyright file="CompositeTargetTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

using Interfaces;

/// <summary>
/// Unit tests for <see cref="ICompositeTarget"/> and <see cref="MainBus"/> composite target dispatch.
/// </summary>
[TestFixture]
public class CompositeTargetTests
{
    private const int PageSize = 4096;

    /// <summary>
    /// Verifies that MainBus dispatches reads to sub-targets via ICompositeTarget.
    /// </summary>
    [Test]
    public void Read8_CompositeTarget_DispatchesToSubTarget()
    {
        var bus = new MainBus();

        // Create sub-targets using RAM with distinct values
        var memory1 = new PhysicalMemory(PageSize, "SubTarget1");
        memory1.Fill(0xAA);
        var subTarget1 = new RamTarget(memory1.Slice(0, PageSize));

        var memory2 = new PhysicalMemory(PageSize, "SubTarget2");
        memory2.Fill(0xBB);
        var subTarget2 = new RamTarget(memory2.Slice(0, PageSize));

        var composite = new TestCompositeTarget(subTarget1, subTarget2);

        bus.MapPage(0, new(1, RegionTag.Io, PagePerms.ReadWrite, TargetCaps.SupportsPeek, composite, 0));

        // Read from first sub-region (offset < 0x100 returns subTarget1)
        var access1 = CreateTestAccess(0x0050, AccessIntent.DataRead);
        byte value1 = bus.Read8(access1);
        Assert.That(value1, Is.EqualTo(0xAA), "Should read from subTarget1");

        // Read from second sub-region (offset >= 0x100 returns subTarget2)
        var access2 = CreateTestAccess(0x0200, AccessIntent.DataRead);
        byte value2 = bus.Read8(access2);
        Assert.That(value2, Is.EqualTo(0xBB), "Should read from subTarget2");
    }

    /// <summary>
    /// Verifies that MainBus dispatches writes to sub-targets via ICompositeTarget.
    /// </summary>
    [Test]
    public void Write8_CompositeTarget_DispatchesToSubTarget()
    {
        var bus = new MainBus();

        var memory = new PhysicalMemory(PageSize, "SubTarget");
        var subTarget = new RamTarget(memory.Slice(0, PageSize));
        var composite = new TestCompositeTarget(subTarget, subTarget);

        bus.MapPage(0, new(1, RegionTag.Io, PagePerms.ReadWrite, TargetCaps.SupportsPoke, composite, 0));

        var access = CreateTestAccess(0x0100, AccessIntent.DataWrite);
        bus.Write8(access, 0x42);

        // Verify the value was written to the sub-target's memory
        Assert.That(memory.AsSpan()[0x100], Is.EqualTo(0x42));
    }

    /// <summary>
    /// Verifies that Read8 returns floating bus value when composite target returns null sub-target.
    /// </summary>
    [Test]
    public void Read8_CompositeTarget_NullSubTarget_ReturnsFloatingBus()
    {
        var bus = new MainBus();

        var composite = new TestCompositeTarget(null, null);

        bus.MapPage(0, new(1, RegionTag.Io, PagePerms.ReadWrite, TargetCaps.SupportsPeek, composite, 0));

        var access = CreateTestAccess(0x0100, AccessIntent.DataRead);
        byte value = bus.Read8(access);

        Assert.That(value, Is.EqualTo(0xFF), "Should return floating bus value (0xFF)");
    }

    /// <summary>
    /// Verifies that TryRead8 returns correct sub-region tag from composite target.
    /// </summary>
    [Test]
    public void TryRead8_CompositeTarget_ReturnsSubRegionTag()
    {
        var bus = new MainBus();

        var memory = new PhysicalMemory(PageSize, "SubTarget");
        memory.AsSpan()[0x100] = 0x42;
        var subTarget = new RamTarget(memory.Slice(0, PageSize));
        var composite = new TestCompositeTarget(subTarget, subTarget);

        bus.MapPage(0, new(1, RegionTag.Io, PagePerms.ReadWrite, TargetCaps.SupportsPeek, composite, 0));

        var access = CreateTestAccess(0x0100, AccessIntent.DataRead);
        var result = bus.TryRead8(access);

        Assert.Multiple(() =>
        {
            Assert.That(result.Ok, Is.True);
            Assert.That(result.Value, Is.EqualTo(0x42));
            Assert.That(result.Fault.RegionTag, Is.EqualTo(RegionTag.Slot), "Should use sub-region tag");
        });
    }

    /// <summary>
    /// Verifies that composite targets are passed the correct offset.
    /// </summary>
    [Test]
    public void Read8_CompositeTarget_PassesCorrectOffset()
    {
        var bus = new MainBus();
        Addr capturedOffset = 0;

        var memory = new PhysicalMemory(PageSize, "SubTarget");
        memory.Fill(0x42);
        var subTarget = new RamTarget(memory.Slice(0, PageSize));
        var composite = new CapturingCompositeTarget(subTarget, offset => capturedOffset = offset);

        bus.MapPage(0, new(1, RegionTag.Io, PagePerms.ReadWrite, TargetCaps.SupportsPeek, composite, 0));

        // Access address 0x0ABC - offset within page should be 0xABC
        var access = CreateTestAccess(0x0ABC, AccessIntent.DataRead);
        bus.Read8(access);

        Assert.That(capturedOffset, Is.EqualTo(0xABCu), "Composite target should receive page offset");
    }

    /// <summary>
    /// Verifies that intent is passed to composite target's ResolveTarget.
    /// </summary>
    [Test]
    public void Read8_CompositeTarget_PassesIntent()
    {
        var bus = new MainBus();
        AccessIntent capturedIntent = AccessIntent.DataWrite;

        var memory = new PhysicalMemory(PageSize, "SubTarget");
        memory.Fill(0x42);
        var subTarget = new RamTarget(memory.Slice(0, PageSize));
        var composite = new CapturingCompositeTarget(subTarget, intent: intent => capturedIntent = intent);

        bus.MapPage(0, new(1, RegionTag.Io, PagePerms.ReadExecute, TargetCaps.SupportsPeek, composite, 0));

        var access = CreateTestAccess(0x0100, AccessIntent.InstructionFetch, BusAccessMode.Decomposed);
        bus.Read8(access);

        Assert.That(capturedIntent, Is.EqualTo(AccessIntent.InstructionFetch), "Composite target should receive access intent");
    }

    /// <summary>
    /// Helper method to create test bus access structures.
    /// </summary>
    private static BusAccess CreateTestAccess(
        Addr address,
        AccessIntent intent,
        BusAccessMode mode = BusAccessMode.Decomposed,
        byte widthBits = 8,
        AccessFlags flags = AccessFlags.None)
    {
        return new(
            Address: address,
            Value: 0,
            WidthBits: widthBits,
            Mode: mode,
            EmulationFlag: mode == BusAccessMode.Decomposed,
            Intent: intent,
            SourceId: 0,
            Cycle: 0,
            Flags: flags);
    }

    /// <summary>
    /// Test composite target implementation for testing.
    /// </summary>
    private sealed class TestCompositeTarget : ICompositeTarget
    {
        private readonly IBusTarget? subTarget1;
        private readonly IBusTarget? subTarget2;

        public TestCompositeTarget(IBusTarget? subTarget1, IBusTarget? subTarget2)
        {
            this.subTarget1 = subTarget1;
            this.subTarget2 = subTarget2;
        }

        public string Name => "TestComposite";

        public TargetCaps Capabilities => TargetCaps.SupportsPeek | TargetCaps.SupportsPoke;

        public byte Read8(Addr physicalAddress, in BusAccess access)
        {
            var subTarget = ResolveTarget(access.Address & 0xFFF, access.Intent);
            return subTarget?.Read8(physicalAddress, access) ?? 0xFF;
        }

        public void Write8(Addr physicalAddress, byte value, in BusAccess access)
        {
            var subTarget = ResolveTarget(access.Address & 0xFFF, access.Intent);
            subTarget?.Write8(physicalAddress, value, access);
        }

        public IBusTarget? ResolveTarget(Addr offset, AccessIntent intent)
        {
            return offset < 0x100 ? subTarget1 : subTarget2;
        }

        public RegionTag GetSubRegionTag(Addr offset)
        {
            return offset < 0x100 ? RegionTag.Io : RegionTag.Slot;
        }

        public IEnumerable<(Addr StartOffset, Addr Size, RegionTag Tag, string TargetName)> EnumerateSubRegions()
        {
            if (subTarget1 is not null)
            {
                yield return (0x000, 0x100, RegionTag.Io, subTarget1.Name);
            }

            if (subTarget2 is not null)
            {
                yield return (0x100, 0xF00, RegionTag.Slot, subTarget2.Name);
            }
        }
    }

    /// <summary>
    /// Composite target that captures parameters for verification.
    /// </summary>
    private sealed class CapturingCompositeTarget : ICompositeTarget
    {
        private readonly IBusTarget subTarget;
        private readonly Action<Addr>? offsetCapture;
        private readonly Action<AccessIntent>? intentCapture;

        public CapturingCompositeTarget(IBusTarget subTarget, Action<Addr>? offset = null, Action<AccessIntent>? intent = null)
        {
            this.subTarget = subTarget;
            this.offsetCapture = offset;
            this.intentCapture = intent;
        }

        public string Name => "CapturingComposite";

        public TargetCaps Capabilities => TargetCaps.SupportsPeek | TargetCaps.SupportsPoke;

        public byte Read8(Addr physicalAddress, in BusAccess access)
        {
            return subTarget.Read8(physicalAddress, access);
        }

        public void Write8(Addr physicalAddress, byte value, in BusAccess access)
        {
            subTarget.Write8(physicalAddress, value, access);
        }

        public IBusTarget? ResolveTarget(Addr offset, AccessIntent intent)
        {
            offsetCapture?.Invoke(offset);
            intentCapture?.Invoke(intent);
            return subTarget;
        }

        public RegionTag GetSubRegionTag(Addr offset)
        {
            return RegionTag.Unknown;
        }

        public IEnumerable<(Addr StartOffset, Addr Size, RegionTag Tag, string TargetName)> EnumerateSubRegions()
        {
            yield return (0x000, 0x1000, RegionTag.Unknown, subTarget.Name);
        }
    }
}