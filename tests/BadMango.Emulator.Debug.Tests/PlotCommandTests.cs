// <copyright file="PlotCommandTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Tests;

/// <summary>
/// Unit tests for lo-res graphics address calculation and color handling.
/// </summary>
[TestFixture]
public class PlotCommandTests
{
    private const ushort TextPage1Base = 0x0400;
    private const int LoResWidth = 40;
    private const int LoResHeight = 48;

    /// <summary>
    /// Verifies lo-res coordinate to address conversion for (0,0).
    /// </summary>
    [Test]
    public void LoResAddress_Origin_IsCorrect()
    {
        var (address, isTop) = LoResCoordToAddress(0, 0);
        Assert.That(address, Is.EqualTo(0x0400));
        Assert.That(isTop, Is.True);
    }

    /// <summary>
    /// Verifies lo-res coordinate distinguishes top and bottom blocks.
    /// </summary>
    [Test]
    public void LoResAddress_TopAndBottom_AreDistinct()
    {
        var (addr0, isTop0) = LoResCoordToAddress(0, 0); // Top block
        var (addr1, isTop1) = LoResCoordToAddress(0, 1); // Bottom block

        Assert.That(addr0, Is.EqualTo(addr1), "Same byte for top and bottom");
        Assert.That(isTop0, Is.True);
        Assert.That(isTop1, Is.False);
    }

    /// <summary>
    /// Verifies all lo-res coordinates map to valid addresses.
    /// </summary>
    [Test]
    public void LoResAddresses_AllWithinValidRange()
    {
        for (int y = 0; y < LoResHeight; y++)
        {
            for (int x = 0; x < LoResWidth; x++)
            {
                var (address, _) = LoResCoordToAddress(x, y);
                Assert.That(address, Is.GreaterThanOrEqualTo(0x0400));
                Assert.That(address, Is.LessThan(0x0800));
            }
        }
    }

    /// <summary>
    /// Verifies color nibble placement for top block.
    /// </summary>
    /// <param name="current">The current byte value.</param>
    /// <param name="color">The color to set.</param>
    /// <param name="expected">The expected result.</param>
    [TestCase(0x00, 5, 0x05)]
    [TestCase(0xF0, 5, 0xF5)]
    [TestCase(0x0F, 5, 0x05)]
    public void SetTopColor_SetsLowNibble(byte current, int color, byte expected)
    {
        byte result = SetTopColor(current, color);
        Assert.That(result, Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies color nibble placement for bottom block.
    /// </summary>
    /// <param name="current">The current byte value.</param>
    /// <param name="color">The color to set.</param>
    /// <param name="expected">The expected result.</param>
    [TestCase(0x00, 5, 0x50)]
    [TestCase(0x0F, 5, 0x5F)]
    [TestCase(0xF0, 5, 0x50)]
    public void SetBottomColor_SetsHighNibble(byte current, int color, byte expected)
    {
        byte result = SetBottomColor(current, color);
        Assert.That(result, Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies all 16 colors can be set.
    /// </summary>
    [Test]
    public void LoResColors_AllSixteenColorsWork()
    {
        for (int color = 0; color < 16; color++)
        {
            byte topResult = SetTopColor(0x00, color);
            byte bottomResult = SetBottomColor(0x00, color);

            Assert.That(topResult & 0x0F, Is.EqualTo(color));
            Assert.That((bottomResult >> 4) & 0x0F, Is.EqualTo(color));
        }
    }

    /// <summary>
    /// Verifies horizontal line produces correct points.
    /// </summary>
    [Test]
    public void DrawLine_Horizontal_ProducesCorrectPoints()
    {
        var points = DrawLine(0, 0, 5, 0);

        Assert.That(points.Count, Is.EqualTo(6)); // 0 to 5 inclusive
        Assert.That(points, Does.Contain(new Point(0, 0)));
        Assert.That(points, Does.Contain(new Point(5, 0)));
        Assert.That(points.All(p => p.Y == 0), Is.True);
    }

    /// <summary>
    /// Verifies vertical line produces correct points.
    /// </summary>
    [Test]
    public void DrawLine_Vertical_ProducesCorrectPoints()
    {
        var points = DrawLine(0, 0, 0, 5);

        Assert.That(points.Count, Is.EqualTo(6));
        Assert.That(points, Does.Contain(new Point(0, 0)));
        Assert.That(points, Does.Contain(new Point(0, 5)));
        Assert.That(points.All(p => p.X == 0), Is.True);
    }

    /// <summary>
    /// Verifies diagonal line produces correct points.
    /// </summary>
    [Test]
    public void DrawLine_Diagonal_ProducesCorrectPoints()
    {
        var points = DrawLine(0, 0, 5, 5);

        Assert.That(points.Count, Is.EqualTo(6));
        Assert.That(points, Does.Contain(new Point(0, 0)));
        Assert.That(points, Does.Contain(new Point(5, 5)));
    }

    /// <summary>
    /// Verifies single point line.
    /// </summary>
    [Test]
    public void DrawLine_SinglePoint_ProducesOnePoint()
    {
        var points = DrawLine(5, 5, 5, 5);

        Assert.That(points.Count, Is.EqualTo(1));
        Assert.That(points[0], Is.EqualTo(new Point(5, 5)));
    }

    /// <summary>
    /// Verifies reverse direction line covers same start and end points.
    /// </summary>
    [Test]
    public void DrawLine_ReverseDirection_CoversSameEndpoints()
    {
        var forward = DrawLine(0, 0, 10, 5);
        var reverse = DrawLine(10, 5, 0, 0);

        // Both should include the start and end points
        Assert.That(forward, Does.Contain(new Point(0, 0)));
        Assert.That(forward, Does.Contain(new Point(10, 5)));
        Assert.That(reverse, Does.Contain(new Point(0, 0)));
        Assert.That(reverse, Does.Contain(new Point(10, 5)));

        // Should have similar count (Bresenham may vary slightly)
        Assert.That(Math.Abs(forward.Count - reverse.Count), Is.LessThanOrEqualTo(1));
    }

    /// <summary>
    /// Verifies steep line (dy > dx) works correctly.
    /// </summary>
    [Test]
    public void DrawLine_Steep_ProducesCorrectPoints()
    {
        var points = DrawLine(0, 0, 2, 10);

        Assert.That(points.Count, Is.EqualTo(11)); // More Y steps than X
        Assert.That(points, Does.Contain(new Point(0, 0)));
        Assert.That(points, Does.Contain(new Point(2, 10)));
    }

    /// <summary>
    /// Verifies negative direction lines work.
    /// </summary>
    [Test]
    public void DrawLine_NegativeDirection_ProducesCorrectPoints()
    {
        var points = DrawLine(5, 5, 0, 0);

        Assert.That(points.Count, Is.EqualTo(6));
        Assert.That(points, Does.Contain(new Point(5, 5)));
        Assert.That(points, Does.Contain(new Point(0, 0)));
    }

    private static (ushort Address, bool IsTopBlock) LoResCoordToAddress(int x, int y)
    {
        int textRow = y / 2;
        bool isTop = (y % 2) == 0;

        int group = textRow / 8;
        int offset = textRow % 8;
        ushort address = (ushort)(TextPage1Base + (offset * 128) + (group * 40) + x);

        return (address, isTop);
    }

    private static byte SetTopColor(byte current, int color)
    {
        return (byte)((current & 0xF0) | (color & 0x0F));
    }

    private static byte SetBottomColor(byte current, int color)
    {
        return (byte)((current & 0x0F) | ((color & 0x0F) << 4));
    }

    private static List<Point> DrawLine(int x0, int y0, int x1, int y1)
    {
        var points = new List<Point>();

        int dx = Math.Abs(x1 - x0);
        int sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1 - y0);
        int sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;

        while (true)
        {
            points.Add(new Point(x0, y0));

            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            int e2 = 2 * err;
            if (e2 >= dy)
            {
                err += dy;
                x0 += sx;
            }

            if (e2 <= dx)
            {
                err += dx;
                y0 += sy;
            }
        }

        return points;
    }

    private record struct Point(int X, int Y);
}