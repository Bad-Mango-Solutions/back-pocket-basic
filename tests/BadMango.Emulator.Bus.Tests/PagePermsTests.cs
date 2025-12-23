// <copyright file="PagePermsTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for the <see cref="PagePerms"/> flags enum.
/// </summary>
[TestFixture]
public class PagePermsTests
{
    /// <summary>
    /// Verifies that None has value 0.
    /// </summary>
    [Test]
    public void PagePerms_None_HasValueZero()
    {
        Assert.That((byte)PagePerms.None, Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies that permission flags are distinct powers of 2.
    /// </summary>
    [Test]
    public void PagePerms_FlagsArePowersOfTwo()
    {
        Assert.Multiple(() =>
        {
            Assert.That((byte)PagePerms.Read, Is.EqualTo(1));
            Assert.That((byte)PagePerms.Write, Is.EqualTo(2));
            Assert.That((byte)PagePerms.Execute, Is.EqualTo(4));
        });
    }

    /// <summary>
    /// Verifies ReadWrite combines Read and Write.
    /// </summary>
    [Test]
    public void PagePerms_ReadWrite_CombinesReadAndWrite()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PagePerms.ReadWrite.HasFlag(PagePerms.Read), Is.True);
            Assert.That(PagePerms.ReadWrite.HasFlag(PagePerms.Write), Is.True);
            Assert.That(PagePerms.ReadWrite.HasFlag(PagePerms.Execute), Is.False);
        });
    }

    /// <summary>
    /// Verifies ReadExecute combines Read and Execute.
    /// </summary>
    [Test]
    public void PagePerms_ReadExecute_CombinesReadAndExecute()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PagePerms.ReadExecute.HasFlag(PagePerms.Read), Is.True);
            Assert.That(PagePerms.ReadExecute.HasFlag(PagePerms.Execute), Is.True);
            Assert.That(PagePerms.ReadExecute.HasFlag(PagePerms.Write), Is.False);
        });
    }

    /// <summary>
    /// Verifies All combines all permissions.
    /// </summary>
    [Test]
    public void PagePerms_All_CombinesAllPerms()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PagePerms.All.HasFlag(PagePerms.Read), Is.True);
            Assert.That(PagePerms.All.HasFlag(PagePerms.Write), Is.True);
            Assert.That(PagePerms.All.HasFlag(PagePerms.Execute), Is.True);
        });
    }

    /// <summary>
    /// Verifies permissions can be combined with bitwise OR.
    /// </summary>
    [Test]
    public void PagePerms_CanBeCombined()
    {
        var combined = PagePerms.Read | PagePerms.Execute;

        Assert.Multiple(() =>
        {
            Assert.That(combined.HasFlag(PagePerms.Read), Is.True);
            Assert.That(combined.HasFlag(PagePerms.Execute), Is.True);
            Assert.That(combined.HasFlag(PagePerms.Write), Is.False);
        });
    }
}