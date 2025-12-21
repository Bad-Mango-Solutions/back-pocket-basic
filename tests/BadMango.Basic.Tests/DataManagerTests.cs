// <copyright file="DataManagerTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.Tests;

using BadMango.Basic.Runtime;

/// <summary>
/// Tests for <see cref="DataManager"/>.
/// </summary>
[TestFixture]
public class DataManagerTests
{
    /// <summary>
    /// Read returns sequential values.
    /// </summary>
    [Test]
    public void Read_ReturnsValuesInOrder()
    {
        var manager = new DataManager();
        manager.Initialize([1.5, "ABC"]);

        Assert.That(manager.Read().AsNumber(), Is.EqualTo(1.5));
        Assert.That(manager.Read().AsString(), Is.EqualTo("ABC"));
    }

    /// <summary>
    /// Restore resets pointer and RestoreToPosition sets position.
    /// </summary>
    [Test]
    public void RestoreAndRestoreToPosition_ResetPointer()
    {
        var manager = new DataManager();
        manager.Initialize([10, 20, 30]);

        Assert.That(manager.Read().AsNumber(), Is.EqualTo(10));
        manager.RestoreToPosition(1);
        Assert.That(manager.Read().AsNumber(), Is.EqualTo(20));

        manager.Restore();
        Assert.That(manager.Read().AsNumber(), Is.EqualTo(10));
    }

    /// <summary>
    /// Reading past end throws out-of-data.
    /// </summary>
    [Test]
    public void Read_PastEnd_ThrowsOutOfData()
    {
        var manager = new DataManager();
        manager.Initialize([42]);
        manager.Read();

        Assert.That(() => manager.Read(), Throws.TypeOf<BasicRuntimeException>().With.Message.Contains("OUT OF DATA"));
    }
}