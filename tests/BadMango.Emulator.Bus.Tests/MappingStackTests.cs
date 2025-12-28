// <copyright file="MappingStackTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

using Interfaces;

/// <summary>
/// Unit tests for the <see cref="MappingStack"/> class.
/// </summary>
[TestFixture]
public class MappingStackTests
{
    /// <summary>
    /// Verifies that a mapping stack can be created.
    /// </summary>
    [Test]
    public void Constructor_CreatesValidStack()
    {
        var stack = new MappingStack(0x1000, 4096);

        Assert.Multiple(() =>
        {
            Assert.That(stack.BaseAddress, Is.EqualTo(0x1000U));
            Assert.That(stack.Size, Is.EqualTo(4096U));
            Assert.That(stack.Count, Is.EqualTo(0));
            Assert.That(stack.ActiveEntry, Is.Null);
        });
    }

    /// <summary>
    /// Verifies that constructor throws for zero size.
    /// </summary>
    [Test]
    public void Constructor_ThrowsForZeroSize()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MappingStack(0, 0));
    }

    /// <summary>
    /// Verifies that Push adds entries to the stack.
    /// </summary>
    [Test]
    public void Push_AddsEntryToStack()
    {
        var stack = new MappingStack(0x1000, 4096);
        var region = CreateTestRegion("test1");

        var entry = new MappingEntry(region, IsActive: true);
        stack.Push(entry);

        Assert.Multiple(() =>
        {
            Assert.That(stack.Count, Is.EqualTo(1));
            Assert.That(stack.ActiveEntry, Is.Not.Null);
            Assert.That(stack.ActiveEntry!.Value.Region.Id, Is.EqualTo("test1"));
        });
    }

    /// <summary>
    /// Verifies that Pop removes and returns the topmost entry.
    /// </summary>
    [Test]
    public void Pop_RemovesAndReturnsTopEntry()
    {
        var stack = new MappingStack(0x1000, 4096);
        var region1 = CreateTestRegion("test1");
        var region2 = CreateTestRegion("test2");

        stack.Push(new MappingEntry(region1, IsActive: true));
        stack.Push(new MappingEntry(region2, IsActive: true));

        var popped = stack.Pop();

        Assert.Multiple(() =>
        {
            Assert.That(popped, Is.Not.Null);
            Assert.That(popped!.Value.Region.Id, Is.EqualTo("test2"));
            Assert.That(stack.Count, Is.EqualTo(1));
            Assert.That(stack.ActiveEntry!.Value.Region.Id, Is.EqualTo("test1"));
        });
    }

    /// <summary>
    /// Verifies that Pop returns null for empty stack.
    /// </summary>
    [Test]
    public void Pop_ReturnsNullForEmptyStack()
    {
        var stack = new MappingStack(0x1000, 4096);

        var popped = stack.Pop();

        Assert.That(popped, Is.Null);
    }

    /// <summary>
    /// Verifies that ActiveEntry returns the topmost active entry.
    /// </summary>
    [Test]
    public void ActiveEntry_ReturnsTopmostActiveEntry()
    {
        var stack = new MappingStack(0x1000, 4096);
        var region1 = CreateTestRegion("test1");
        var region2 = CreateTestRegion("test2");
        var region3 = CreateTestRegion("test3");

        stack.Push(new MappingEntry(region1, IsActive: true));
        stack.Push(new MappingEntry(region2, IsActive: false));
        stack.Push(new MappingEntry(region3, IsActive: true));

        Assert.That(stack.ActiveEntry!.Value.Region.Id, Is.EqualTo("test3"));
    }

    /// <summary>
    /// Verifies that ActiveEntry skips inactive entries.
    /// </summary>
    [Test]
    public void ActiveEntry_SkipsInactiveTopEntry()
    {
        var stack = new MappingStack(0x1000, 4096);
        var region1 = CreateTestRegion("test1");
        var region2 = CreateTestRegion("test2");

        stack.Push(new MappingEntry(region1, IsActive: true));
        stack.Push(new MappingEntry(region2, IsActive: false));

        Assert.That(stack.ActiveEntry!.Value.Region.Id, Is.EqualTo("test1"));
    }

    /// <summary>
    /// Verifies that SetActive changes entry state.
    /// </summary>
    [Test]
    public void SetActive_ChangesEntryState()
    {
        var stack = new MappingStack(0x1000, 4096);
        var region = CreateTestRegion("test1");

        stack.Push(new MappingEntry(region, IsActive: false));

        var result = stack.SetActive("test1", true);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(stack.ActiveEntry, Is.Not.Null);
            Assert.That(stack.Entries[0].IsActive, Is.True);
        });
    }

    /// <summary>
    /// Verifies that SetActive returns false for non-existent region.
    /// </summary>
    [Test]
    public void SetActive_ReturnsFalseForNonExistentRegion()
    {
        var stack = new MappingStack(0x1000, 4096);

        var result = stack.SetActive("nonexistent", true);

        Assert.That(result, Is.False);
    }

    /// <summary>
    /// Verifies that Replace clears stack and adds single entry.
    /// </summary>
    [Test]
    public void Replace_ClearsAndAddsEntry()
    {
        var stack = new MappingStack(0x1000, 4096);
        var region1 = CreateTestRegion("test1");
        var region2 = CreateTestRegion("test2");

        stack.Push(new MappingEntry(region1, IsActive: true));
        stack.Replace(new MappingEntry(region2, IsActive: true));

        Assert.Multiple(() =>
        {
            Assert.That(stack.Count, Is.EqualTo(1));
            Assert.That(stack.ActiveEntry!.Value.Region.Id, Is.EqualTo("test2"));
        });
    }

    /// <summary>
    /// Verifies that Clear removes all entries.
    /// </summary>
    [Test]
    public void Clear_RemovesAllEntries()
    {
        var stack = new MappingStack(0x1000, 4096);
        var region = CreateTestRegion("test1");

        stack.Push(new MappingEntry(region, IsActive: true));
        stack.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(stack.Count, Is.EqualTo(0));
            Assert.That(stack.ActiveEntry, Is.Null);
        });
    }

    /// <summary>
    /// Verifies that ToPageEntry creates valid page entry.
    /// </summary>
    [Test]
    public void ToPageEntry_CreatesValidEntry()
    {
        var stack = new MappingStack(0x1000, 4096);
        var region = CreateTestRegion("test1");

        stack.Push(new MappingEntry(region, IsActive: true));

        var pageEntry = stack.ToPageEntry(deviceId: 1, pageOffset: 0);

        Assert.Multiple(() =>
        {
            Assert.That(pageEntry, Is.Not.Null);
            Assert.That(pageEntry!.Value.DeviceId, Is.EqualTo(1));
            Assert.That(pageEntry.Value.Target, Is.SameAs(region.Target));
            Assert.That(pageEntry.Value.Perms, Is.EqualTo(region.DefaultPermissions));
        });
    }

    /// <summary>
    /// Verifies that ToPageEntry returns null for inactive stack.
    /// </summary>
    [Test]
    public void ToPageEntry_ReturnsNullForInactiveStack()
    {
        var stack = new MappingStack(0x1000, 4096);
        var region = CreateTestRegion("test1");

        stack.Push(new MappingEntry(region, IsActive: false));

        var pageEntry = stack.ToPageEntry(deviceId: 1, pageOffset: 0);

        Assert.That(pageEntry, Is.Null);
    }

    private static IMemoryRegion CreateTestRegion(string id)
    {
        var memory = new PhysicalMemory(4096, $"Memory for {id}");
        return MemoryRegion.CreateRam(id, $"Region {id}", 0x1000, memory);
    }
}