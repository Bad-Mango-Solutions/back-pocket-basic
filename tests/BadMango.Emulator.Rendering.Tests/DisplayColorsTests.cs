// <copyright file="DisplayColorsTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Rendering.Tests;

/// <summary>
/// Unit tests for the <see cref="DisplayColors"/> class.
/// Verifies color constants and the GetLoResColor method.
/// </summary>
[TestFixture]
public class DisplayColorsTests
{
    /// <summary>
    /// Verifies that GreenPhosphor has correct value.
    /// </summary>
    [Test]
    public void GreenPhosphor_HasCorrectValue()
    {
        Assert.That(DisplayColors.GreenPhosphor, Is.EqualTo(0xFF33FF33));
    }

    /// <summary>
    /// Verifies that AmberPhosphor has correct value.
    /// </summary>
    [Test]
    public void AmberPhosphor_HasCorrectValue()
    {
        Assert.That(DisplayColors.AmberPhosphor, Is.EqualTo(0xFF00BFFF));
    }

    /// <summary>
    /// Verifies that WhitePhosphor has correct value.
    /// </summary>
    [Test]
    public void WhitePhosphor_HasCorrectValue()
    {
        Assert.That(DisplayColors.WhitePhosphor, Is.EqualTo(0xFFFFFFFF));
    }

    /// <summary>
    /// Verifies that Black has correct value with full alpha.
    /// </summary>
    [Test]
    public void Black_HasFullAlpha()
    {
        Assert.That(DisplayColors.Black, Is.EqualTo(0xFF000000));
        Assert.That(DisplayColors.Black & 0xFF000000, Is.EqualTo(0xFF000000), "Alpha should be fully opaque");
    }

    /// <summary>
    /// Verifies that DarkGray has correct value.
    /// </summary>
    [Test]
    public void DarkGray_HasCorrectValue()
    {
        Assert.That(DisplayColors.DarkGray, Is.EqualTo(0xFF1A1A1A));
    }

    /// <summary>
    /// Verifies that CellBackground has correct value.
    /// </summary>
    [Test]
    public void CellBackground_HasCorrectValue()
    {
        Assert.That(DisplayColors.CellBackground, Is.EqualTo(0xFF222222));
    }

    /// <summary>
    /// Verifies that GetLoResColor returns correct color for index 0 (Black).
    /// </summary>
    [Test]
    public void GetLoResColor_Index0_ReturnsBlack()
    {
        Assert.That(DisplayColors.GetLoResColor(0), Is.EqualTo(DisplayColors.LoResBlack));
    }

    /// <summary>
    /// Verifies that GetLoResColor returns correct color for index 15 (White).
    /// </summary>
    [Test]
    public void GetLoResColor_Index15_ReturnsWhite()
    {
        Assert.That(DisplayColors.GetLoResColor(15), Is.EqualTo(DisplayColors.LoResWhite));
    }

    /// <summary>
    /// Verifies that GetLoResColor masks to lower 4 bits.
    /// </summary>
    [Test]
    public void GetLoResColor_IndexOver15_MasksTo4Bits()
    {
        Assert.That(DisplayColors.GetLoResColor(16), Is.EqualTo(DisplayColors.LoResBlack));
        Assert.That(DisplayColors.GetLoResColor(17), Is.EqualTo(DisplayColors.LoResMagenta));
        Assert.That(DisplayColors.GetLoResColor(255), Is.EqualTo(DisplayColors.LoResWhite));
    }

    /// <summary>
    /// Verifies that GetLoResColor returns all 16 colors correctly.
    /// </summary>
    [Test]
    public void GetLoResColor_AllIndices_ReturnCorrectColors()
    {
        var expectedColors = new uint[]
        {
            DisplayColors.LoResBlack,
            DisplayColors.LoResMagenta,
            DisplayColors.LoResDarkBlue,
            DisplayColors.LoResPurple,
            DisplayColors.LoResDarkGreen,
            DisplayColors.LoResGray1,
            DisplayColors.LoResMediumBlue,
            DisplayColors.LoResLightBlue,
            DisplayColors.LoResBrown,
            DisplayColors.LoResOrange,
            DisplayColors.LoResGray2,
            DisplayColors.LoResPink,
            DisplayColors.LoResLightGreen,
            DisplayColors.LoResYellow,
            DisplayColors.LoResAqua,
            DisplayColors.LoResWhite,
        };

        for (int i = 0; i < 16; i++)
        {
            Assert.That(
                DisplayColors.GetLoResColor(i),
                Is.EqualTo(expectedColors[i]),
                $"Color index {i} should return correct color");
        }
    }

    /// <summary>
    /// Verifies that all Lo-Res colors have full alpha.
    /// </summary>
    [Test]
    public void LoResColors_AllHaveFullAlpha()
    {
        for (int i = 0; i < 16; i++)
        {
            uint color = DisplayColors.GetLoResColor(i);
            Assert.That(
                color & 0xFF000000,
                Is.EqualTo(0xFF000000),
                $"Lo-Res color {i} should have full alpha");
        }
    }

    /// <summary>
    /// Verifies that Gray1 and Gray2 have the same value.
    /// </summary>
    [Test]
    public void Gray1AndGray2_AreSameValue()
    {
        Assert.That(DisplayColors.LoResGray1, Is.EqualTo(DisplayColors.LoResGray2));
    }

    /// <summary>
    /// Verifies that phosphor colors are distinct from each other.
    /// </summary>
    [Test]
    public void PhosphorColors_AreDistinct()
    {
        Assert.That(DisplayColors.GreenPhosphor, Is.Not.EqualTo(DisplayColors.AmberPhosphor));
        Assert.That(DisplayColors.GreenPhosphor, Is.Not.EqualTo(DisplayColors.WhitePhosphor));
        Assert.That(DisplayColors.AmberPhosphor, Is.Not.EqualTo(DisplayColors.WhitePhosphor));
    }

    /// <summary>
    /// Verifies that GetLoResColor handles negative values correctly.
    /// </summary>
    [Test]
    public void GetLoResColor_NegativeIndex_MasksCorrectly()
    {
        Assert.That(DisplayColors.GetLoResColor(-1), Is.EqualTo(DisplayColors.LoResWhite));
    }
}