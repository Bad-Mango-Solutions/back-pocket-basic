// <copyright file="PhysicalMemoryTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for the <see cref="PhysicalMemory"/> class.
/// </summary>
[TestFixture]
public class PhysicalMemoryTests
{
    /// <summary>
    /// Verifies that PhysicalMemory can be created with size and name.
    /// </summary>
    [Test]
    public void PhysicalMemory_CanBeCreatedWithSizeAndName()
    {
        var memory = new PhysicalMemory(4096, "Test RAM");

        Assert.Multiple(() =>
        {
            Assert.That(memory.Size, Is.EqualTo(4096));
            Assert.That(memory.Name, Is.EqualTo("Test RAM"));
        });
    }

    /// <summary>
    /// Verifies that PhysicalMemory is zero-initialized when created with size.
    /// </summary>
    [Test]
    public void PhysicalMemory_WithSize_IsZeroInitialized()
    {
        var memory = new PhysicalMemory(64, "Test");

        var span = memory.AsReadOnlySpan();
        for (int i = 0; i < span.Length; i++)
        {
            Assert.That(span[i], Is.EqualTo(0), $"Byte at index {i} should be zero");
        }
    }

    /// <summary>
    /// Verifies that constructor throws for zero size.
    /// </summary>
    [Test]
    public void PhysicalMemory_Constructor_ThrowsForZeroSize()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PhysicalMemory(0U, "Test"));
    }

    /// <summary>
    /// Verifies that constructor throws for null name.
    /// </summary>
    [Test]
    public void PhysicalMemory_Constructor_ThrowsForNullName()
    {
        Assert.Throws<ArgumentNullException>(() => new PhysicalMemory(64, null!));
    }

    /// <summary>
    /// Verifies that constructor throws for empty name.
    /// </summary>
    [Test]
    public void PhysicalMemory_Constructor_ThrowsForEmptyName()
    {
        Assert.Throws<ArgumentException>(() => new PhysicalMemory(64, string.Empty));
    }

    /// <summary>
    /// Verifies that constructor throws for whitespace name.
    /// </summary>
    [Test]
    public void PhysicalMemory_Constructor_ThrowsForWhitespaceName()
    {
        Assert.Throws<ArgumentException>(() => new PhysicalMemory(64, "   "));
    }

    /// <summary>
    /// Verifies that PhysicalMemory can be created with initial data.
    /// </summary>
    [Test]
    public void PhysicalMemory_CanBeCreatedWithInitialData()
    {
        var data = new byte[] { 0x11, 0x22, 0x33, 0x44 };
        var memory = new PhysicalMemory(data, "Test ROM");

        Assert.Multiple(() =>
        {
            Assert.That(memory.Size, Is.EqualTo(4));
            Assert.That(memory.Name, Is.EqualTo("Test ROM"));
        });
    }

    /// <summary>
    /// Verifies that initial data is copied.
    /// </summary>
    [Test]
    public void PhysicalMemory_InitialData_IsCopied()
    {
        var data = new byte[] { 0x11, 0x22, 0x33, 0x44 };
        var memory = new PhysicalMemory(data, "Test");

        // Modify original data
        data[0] = 0xFF;

        Assert.That(memory.AsReadOnlySpan()[0], Is.EqualTo(0x11), "Memory should not be affected by changes to original array");
    }

    /// <summary>
    /// Verifies that constructor with data throws for zero length.
    /// </summary>
    [Test]
    public void PhysicalMemory_ConstructorWithData_ThrowsForZeroLength()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PhysicalMemory(ReadOnlySpan<byte>.Empty, "Test"));
    }

    /// <summary>
    /// Verifies that Slice returns a writable memory segment.
    /// </summary>
    [Test]
    public void PhysicalMemory_Slice_ReturnsWritableMemory()
    {
        var memory = new PhysicalMemory(64, "Test");

        var slice = memory.Slice(10, 4);
        slice.Span[0] = 0xAA;
        slice.Span[3] = 0xBB;

        Assert.Multiple(() =>
        {
            Assert.That(memory.AsReadOnlySpan()[10], Is.EqualTo(0xAA));
            Assert.That(memory.AsReadOnlySpan()[13], Is.EqualTo(0xBB));
        });
    }

    /// <summary>
    /// Verifies that Slice with offset 0 and full size returns entire memory.
    /// </summary>
    [Test]
    public void PhysicalMemory_Slice_FullSizeReturnsEntireMemory()
    {
        var memory = new PhysicalMemory(64, "Test");

        var slice = memory.Slice(0, 64);

        Assert.That(slice.Length, Is.EqualTo(64));
    }

    /// <summary>
    /// Verifies that Slice throws when exceeding bounds.
    /// </summary>
    [Test]
    public void PhysicalMemory_Slice_ThrowsWhenExceedingBounds()
    {
        var memory = new PhysicalMemory(64, "Test");

        Assert.Throws<ArgumentOutOfRangeException>(() => memory.Slice(60, 10));
    }

    /// <summary>
    /// Verifies that SlicePage returns a full page.
    /// </summary>
    [Test]
    public void PhysicalMemory_SlicePage_ReturnsFullPage()
    {
        var memory = new PhysicalMemory(8192, "Test"); // 2 pages

        var page = memory.SlicePage(1);

        Assert.That(page.Length, Is.EqualTo(4096));
    }

    /// <summary>
    /// Verifies that SlicePage with custom page size works.
    /// </summary>
    [Test]
    public void PhysicalMemory_SlicePage_WithCustomPageSize()
    {
        var memory = new PhysicalMemory(1024, "Test");

        var page = memory.SlicePage(1, 256);

        Assert.Multiple(() =>
        {
            Assert.That(page.Length, Is.EqualTo(256));
        });

        // Verify the slice is at the correct offset
        page.Span[0] = 0xAA;
        Assert.That(memory.AsReadOnlySpan()[256], Is.EqualTo(0xAA));
    }

    /// <summary>
    /// Verifies that SlicePage throws for zero page size.
    /// </summary>
    [Test]
    public void PhysicalMemory_SlicePage_ThrowsForZeroPageSize()
    {
        var memory = new PhysicalMemory(4096, "Test");

        Assert.Throws<ArgumentOutOfRangeException>(() => memory.SlicePage(0, 0));
    }

    /// <summary>
    /// Verifies that SlicePage throws when page exceeds bounds.
    /// </summary>
    [Test]
    public void PhysicalMemory_SlicePage_ThrowsWhenExceedingBounds()
    {
        var memory = new PhysicalMemory(4096, "Test");

        Assert.Throws<ArgumentOutOfRangeException>(() => memory.SlicePage(1)); // Page 1 of 1-page memory
    }

    /// <summary>
    /// Verifies that Fill sets all bytes to specified value.
    /// </summary>
    [Test]
    public void PhysicalMemory_Fill_SetsAllBytesToValue()
    {
        var memory = new PhysicalMemory(16, "Test");

        memory.Fill(0xCC);

        var span = memory.AsReadOnlySpan();
        for (int i = 0; i < span.Length; i++)
        {
            Assert.That(span[i], Is.EqualTo(0xCC), $"Byte at index {i} should be 0xCC");
        }
    }

    /// <summary>
    /// Verifies that Clear sets all bytes to zero.
    /// </summary>
    [Test]
    public void PhysicalMemory_Clear_SetsAllBytesToZero()
    {
        var data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
        var memory = new PhysicalMemory(data, "Test");

        memory.Clear();

        var span = memory.AsReadOnlySpan();
        for (int i = 0; i < span.Length; i++)
        {
            Assert.That(span[i], Is.EqualTo(0), $"Byte at index {i} should be 0");
        }
    }

    /// <summary>
    /// Verifies that AsSpan returns writable span.
    /// </summary>
    [Test]
    public void PhysicalMemory_AsSpan_ReturnsWritableSpan()
    {
        var memory = new PhysicalMemory(8, "Test");

        var span = memory.AsSpan();
        span[0] = 0xAA;
        span[7] = 0xBB;

        Assert.Multiple(() =>
        {
            Assert.That(memory.AsReadOnlySpan()[0], Is.EqualTo(0xAA));
            Assert.That(memory.AsReadOnlySpan()[7], Is.EqualTo(0xBB));
        });
    }

    /// <summary>
    /// Verifies that AsReadOnlySpan returns readable span.
    /// </summary>
    [Test]
    public void PhysicalMemory_AsReadOnlySpan_ReturnsReadableSpan()
    {
        var data = new byte[] { 0x11, 0x22, 0x33 };
        var memory = new PhysicalMemory(data, "Test");

        var span = memory.AsReadOnlySpan();

        Assert.That(span.Length, Is.EqualTo(3));
        Assert.That(span[0], Is.EqualTo(0x11));
        Assert.That(span[1], Is.EqualTo(0x22));
        Assert.That(span[2], Is.EqualTo(0x33));
    }

    /// <summary>
    /// Verifies that PageCount returns correct count for default page size.
    /// </summary>
    [Test]
    public void PhysicalMemory_PageCount_ReturnsCorrectCountForDefaultPageSize()
    {
        var memory = new PhysicalMemory(16384, "Test"); // 4 pages at 4KB each

        Assert.That(memory.PageCount(), Is.EqualTo(4));
    }

    /// <summary>
    /// Verifies that PageCount returns correct count for custom page size.
    /// </summary>
    [Test]
    public void PhysicalMemory_PageCount_ReturnsCorrectCountForCustomPageSize()
    {
        var memory = new PhysicalMemory(1024, "Test");

        Assert.That(memory.PageCount(256), Is.EqualTo(4));
    }

    /// <summary>
    /// Verifies that PageCount returns zero for memory smaller than page size.
    /// </summary>
    [Test]
    public void PhysicalMemory_PageCount_ReturnsZeroForSmallMemory()
    {
        var memory = new PhysicalMemory(1024, "Test");

        Assert.That(memory.PageCount(4096), Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies that PageCount throws for zero page size.
    /// </summary>
    [Test]
    public void PhysicalMemory_PageCount_ThrowsForZeroPageSize()
    {
        var memory = new PhysicalMemory(64, "Test");

        Assert.Throws<ArgumentOutOfRangeException>(() => memory.PageCount(0));
    }

    /// <summary>
    /// Verifies that multiple slices share the same underlying storage.
    /// </summary>
    [Test]
    public void PhysicalMemory_MultipleSlices_ShareStorage()
    {
        var memory = new PhysicalMemory(64, "Test");

        var slice1 = memory.Slice(0, 32);
        var slice2 = memory.Slice(16, 32);

        // Write via slice1
        slice1.Span[20] = 0xAA;

        // Read via slice2 (offset 20 in slice1 = offset 4 in slice2)
        Assert.That(slice2.Span[4], Is.EqualTo(0xAA), "Slices should share storage");

        // Also verify via main memory
        Assert.That(memory.AsReadOnlySpan()[20], Is.EqualTo(0xAA), "Main memory should reflect changes");
    }

    /// <summary>
    /// Verifies that a slice can be used with RamTarget.
    /// </summary>
    [Test]
    public void PhysicalMemory_Slice_CanBeUsedWithRamTarget()
    {
        var memory = new PhysicalMemory(4096, "Test RAM");
        var slice = memory.Slice(0, 4096);
        var ram = new RamTarget(slice);
        var access = CreateDefaultAccess();

        ram.Write8(10, 0xAB, in access);
        byte value = ram.Read8(10, in access);

        Assert.That(value, Is.EqualTo(0xAB));
    }

    /// <summary>
    /// Verifies that a slice can be used with RomTarget for read-only access.
    /// </summary>
    [Test]
    public void PhysicalMemory_Slice_CanBeUsedWithRomTarget()
    {
        var data = new byte[] { 0x11, 0x22, 0x33, 0x44 };
        var memory = new PhysicalMemory(data, "Test ROM");
        var slice = memory.Slice(0, 4);
        var rom = new RomTarget(slice);
        var access = CreateDefaultAccess();

        byte value = rom.Read8(1, in access);

        Assert.That(value, Is.EqualTo(0x22));
    }

    /// <summary>
    /// Verifies that Memory property returns the entire memory as ReadOnlyMemory.
    /// </summary>
    [Test]
    public void PhysicalMemory_Memory_ReturnsEntireMemory()
    {
        var data = new byte[] { 0x11, 0x22, 0x33, 0x44 };
        var memory = new PhysicalMemory(data, "Test");

        ReadOnlyMemory<byte> readOnlyMem = memory.Memory;

        Assert.That(readOnlyMem.Length, Is.EqualTo(4));
        Assert.That(readOnlyMem.Span[0], Is.EqualTo(0x11));
        Assert.That(readOnlyMem.Span[1], Is.EqualTo(0x22));
        Assert.That(readOnlyMem.Span[2], Is.EqualTo(0x33));
        Assert.That(readOnlyMem.Span[3], Is.EqualTo(0x44));
    }

    /// <summary>
    /// Verifies that WriteBytePhysical writes a single byte at the specified address.
    /// </summary>
    [Test]
    public void PhysicalMemory_WriteBytePhysical_WritesSingleByte()
    {
        var memory = new PhysicalMemory(16, "Test");
        var debugPrivilege = new DebugPrivilege();

        memory.WriteBytePhysical(debugPrivilege, 5, 0xAA);

        Assert.That(memory.AsReadOnlySpan()[5], Is.EqualTo(0xAA));
    }

    /// <summary>
    /// Verifies that WriteBytePhysical throws when address exceeds bounds.
    /// </summary>
    [Test]
    public void PhysicalMemory_WriteBytePhysical_ThrowsWhenExceedingBounds()
    {
        var memory = new PhysicalMemory(16, "Test");
        var debugPrivilege = new DebugPrivilege();

        Assert.Throws<ArgumentOutOfRangeException>(() => memory.WriteBytePhysical(debugPrivilege, 16, 0xAA));
    }

    /// <summary>
    /// Verifies that WritePhysical writes multiple bytes at the specified address.
    /// </summary>
    [Test]
    public void PhysicalMemory_WritePhysical_WritesMultipleBytes()
    {
        var memory = new PhysicalMemory(16, "Test");
        var debugPrivilege = new DebugPrivilege();
        var dataToWrite = new byte[] { 0x11, 0x22, 0x33 };

        memory.WritePhysical(debugPrivilege, 4, dataToWrite);

        Assert.That(memory.AsReadOnlySpan()[4], Is.EqualTo(0x11));
        Assert.That(memory.AsReadOnlySpan()[5], Is.EqualTo(0x22));
        Assert.That(memory.AsReadOnlySpan()[6], Is.EqualTo(0x33));
    }

    /// <summary>
    /// Verifies that WritePhysical throws when write exceeds bounds.
    /// </summary>
    [Test]
    public void PhysicalMemory_WritePhysical_ThrowsWhenExceedingBounds()
    {
        var memory = new PhysicalMemory(16, "Test");
        var debugPrivilege = new DebugPrivilege();
        var dataToWrite = new byte[] { 0x11, 0x22, 0x33 };

        Assert.Throws<ArgumentOutOfRangeException>(() => memory.WritePhysical(debugPrivilege, 14, dataToWrite));
    }

    /// <summary>
    /// Verifies that WritePhysical with empty data succeeds.
    /// </summary>
    [Test]
    public void PhysicalMemory_WritePhysical_EmptyDataSucceeds()
    {
        var memory = new PhysicalMemory(16, "Test");
        var debugPrivilege = new DebugPrivilege();

        Assert.DoesNotThrow(() => memory.WritePhysical(debugPrivilege, 0, ReadOnlySpan<byte>.Empty));
    }

    /// <summary>
    /// Creates a default BusAccess for testing.
    /// </summary>
    /// <returns>A default BusAccess instance.</returns>
    private static BusAccess CreateDefaultAccess() => new(
        Address: 0,
        Value: 0,
        WidthBits: 8,
        Mode: BusAccessMode.Decomposed,
        EmulationFlag: true,
        Intent: AccessIntent.DataRead,
        SourceId: 0,
        Cycle: 0,
        Flags: AccessFlags.None,
        PrivilegeLevel: PrivilegeLevel.Ring0);
}