// <copyright file="VideoModeTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

/// <summary>
/// Unit tests for the <see cref="VideoMode"/> enumeration.
/// </summary>
[TestFixture]
public class VideoModeTests
{
    /// <summary>
    /// Verifies that VideoMode has expected value count.
    /// </summary>
    [Test]
    public void VideoMode_HasExpectedValueCount()
    {
        var values = Enum.GetValues<VideoMode>();
        Assert.That(values, Has.Length.EqualTo(10));
    }

    /// <summary>
    /// Verifies that Text40 value is defined.
    /// </summary>
    [Test]
    public void VideoMode_HasText40Value()
    {
        Assert.That(Enum.IsDefined(typeof(VideoMode), VideoMode.Text40), Is.True);
    }

    /// <summary>
    /// Verifies that Text80 value is defined.
    /// </summary>
    [Test]
    public void VideoMode_HasText80Value()
    {
        Assert.That(Enum.IsDefined(typeof(VideoMode), VideoMode.Text80), Is.True);
    }

    /// <summary>
    /// Verifies that LoRes value is defined.
    /// </summary>
    [Test]
    public void VideoMode_HasLoResValue()
    {
        Assert.That(Enum.IsDefined(typeof(VideoMode), VideoMode.LoRes), Is.True);
    }

    /// <summary>
    /// Verifies that DoubleLoRes value is defined.
    /// </summary>
    [Test]
    public void VideoMode_HasDoubleLoResValue()
    {
        Assert.That(Enum.IsDefined(typeof(VideoMode), VideoMode.DoubleLoRes), Is.True);
    }

    /// <summary>
    /// Verifies that HiRes value is defined.
    /// </summary>
    [Test]
    public void VideoMode_HasHiResValue()
    {
        Assert.That(Enum.IsDefined(typeof(VideoMode), VideoMode.HiRes), Is.True);
    }

    /// <summary>
    /// Verifies that DoubleHiRes value is defined.
    /// </summary>
    [Test]
    public void VideoMode_HasDoubleHiResValue()
    {
        Assert.That(Enum.IsDefined(typeof(VideoMode), VideoMode.DoubleHiRes), Is.True);
    }

    /// <summary>
    /// Verifies that LoResMixed value is defined.
    /// </summary>
    [Test]
    public void VideoMode_HasLoResMixedValue()
    {
        Assert.That(Enum.IsDefined(typeof(VideoMode), VideoMode.LoResMixed), Is.True);
    }

    /// <summary>
    /// Verifies that DoubleLoResMixed value is defined.
    /// </summary>
    [Test]
    public void VideoMode_HasDoubleLoResMixedValue()
    {
        Assert.That(Enum.IsDefined(typeof(VideoMode), VideoMode.DoubleLoResMixed), Is.True);
    }

    /// <summary>
    /// Verifies that HiResMixed value is defined.
    /// </summary>
    [Test]
    public void VideoMode_HasHiResMixedValue()
    {
        Assert.That(Enum.IsDefined(typeof(VideoMode), VideoMode.HiResMixed), Is.True);
    }

    /// <summary>
    /// Verifies that DoubleHiResMixed value is defined.
    /// </summary>
    [Test]
    public void VideoMode_HasDoubleHiResMixedValue()
    {
        Assert.That(Enum.IsDefined(typeof(VideoMode), VideoMode.DoubleHiResMixed), Is.True);
    }
}