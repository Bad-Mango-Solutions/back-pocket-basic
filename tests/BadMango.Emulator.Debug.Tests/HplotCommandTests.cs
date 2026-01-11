// <copyright file="HplotCommandTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Tests;

/// <summary>
/// Unit tests for hi-res graphics address calculation.
/// </summary>
[TestFixture]
public class HplotCommandTests
{
    private const ushort HiResPage1Base = 0x2000;
    private const int HiResWidth = 280;
    private const int HiResHeight = 192;

    /// <summary>
    /// Verifies hi-res row 0 base address.
    /// </summary>
    [Test]
    public void HiResRowAddress_Row0_IsCorrect()
    {
        ushort address = ComputeHiResRowAddress(0);
        Assert.That(address, Is.EqualTo(0x2000));
    }

    /// <summary>
    /// Verifies hi-res row addresses follow interleaved pattern.
    /// </summary>
    [Test]
    public void HiResRowAddress_Row1_IsCorrect()
    {
        ushort address = ComputeHiResRowAddress(1);

        // Row 1 should be at $2400 (scanline 1 of first group)
        Assert.That(address, Is.EqualTo(0x2400));
    }

    /// <summary>
    /// Verifies hi-res row 8 address.
    /// </summary>
    [Test]
    public void HiResRowAddress_Row8_IsCorrect()
    {
        ushort address = ComputeHiResRowAddress(8);

        // Row 8 should be at $2080 (first row of second subgroup)
        Assert.That(address, Is.EqualTo(0x2080));
    }

    /// <summary>
    /// Verifies hi-res row 64 address.
    /// </summary>
    [Test]
    public void HiResRowAddress_Row64_IsCorrect()
    {
        ushort address = ComputeHiResRowAddress(64);

        // Row 64 should be at $2028 (first row of second group)
        Assert.That(address, Is.EqualTo(0x2028));
    }

    /// <summary>
    /// Verifies all hi-res row addresses are within page 1 bounds.
    /// </summary>
    [Test]
    public void HiResRowAddresses_AllWithinPage1Bounds()
    {
        for (int row = 0; row < HiResHeight; row++)
        {
            ushort address = ComputeHiResRowAddress(row);
            Assert.That(address, Is.GreaterThanOrEqualTo(0x2000), $"Row {row} address too low");
            Assert.That(address, Is.LessThan(0x4000), $"Row {row} address too high");
        }
    }

    /// <summary>
    /// Verifies hi-res row addresses don't overlap.
    /// </summary>
    [Test]
    public void HiResRowAddresses_DoNotOverlap()
    {
        var usedAddresses = new HashSet<ushort>();

        for (int row = 0; row < HiResHeight; row++)
        {
            ushort baseAddr = ComputeHiResRowAddress(row);
            for (int byteCol = 0; byteCol < 40; byteCol++)
            {
                ushort addr = (ushort)(baseAddr + byteCol);
                Assert.That(usedAddresses.Contains(addr), Is.False, $"Address {addr:X4} at row {row} is duplicated");
                usedAddresses.Add(addr);
            }
        }

        // Should have exactly 192 * 40 = 7680 unique addresses
        Assert.That(usedAddresses.Count, Is.EqualTo(7680));
    }

    /// <summary>
    /// Verifies X coordinate to byte/bit conversion.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="expectedByte">The expected byte offset.</param>
    /// <param name="expectedBit">The expected bit position.</param>
    [TestCase(0, 0, 0)]
    [TestCase(6, 0, 6)]
    [TestCase(7, 1, 0)]
    [TestCase(13, 1, 6)]
    [TestCase(279, 39, 6)]
    public void HiResPixelPosition_ConvertsCorrectly(int x, int expectedByte, int expectedBit)
    {
        var (byteOffset, bitPosition) = HiResPixelToByteAndBit(x);
        Assert.That(byteOffset, Is.EqualTo(expectedByte));
        Assert.That(bitPosition, Is.EqualTo(expectedBit));
    }

    /// <summary>
    /// Verifies setting individual bits in hi-res byte.
    /// </summary>
    /// <param name="current">The current byte value.</param>
    /// <param name="bit">The bit position to set.</param>
    /// <param name="set">Whether to set or clear the bit.</param>
    /// <param name="expected">The expected result byte.</param>
    [TestCase(0x00, 0, true, 0x01)]
    [TestCase(0x00, 6, true, 0x40)]
    [TestCase(0xFF, 0, false, 0xFE)]
    [TestCase(0xFF, 6, false, 0xBF)]
    public void SetHiResBit_WorksCorrectly(byte current, int bit, bool set, byte expected)
    {
        byte result = SetHiResBit(current, bit, set);
        Assert.That(result, Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies all 7 pixels per byte can be set.
    /// </summary>
    [Test]
    public void HiResBits_AllSevenBitsWork()
    {
        byte allSet = 0x00;

        for (int bit = 0; bit < 7; bit++)
        {
            allSet = SetHiResBit(allSet, bit, true);
        }

        // Should have bits 0-6 set (0x7F), bit 7 is color bit
        Assert.That(allSet, Is.EqualTo(0x7F));
    }

    /// <summary>
    /// Verifies clearing all pixels results in 0x00.
    /// </summary>
    [Test]
    public void HiResBits_ClearAllResults_InZero()
    {
        byte allClear = 0x7F;

        for (int bit = 0; bit < 7; bit++)
        {
            allClear = SetHiResBit(allClear, bit, false);
        }

        Assert.That(allClear, Is.EqualTo(0x00));
    }

    /// <summary>
    /// Verifies TO keyword detection.
    /// </summary>
    [Test]
    public void FindToKeyword_Present_ReturnsIndex()
    {
        string[] args = ["100", "100", "TO", "200", "200"];
        int index = Array.FindIndex(args, a => a.Equals("TO", StringComparison.OrdinalIgnoreCase));
        Assert.That(index, Is.EqualTo(2));
    }

    /// <summary>
    /// Verifies TO keyword is case-insensitive.
    /// </summary>
    /// <param name="toKeyword">The TO keyword variant to test.</param>
    [TestCase("TO")]
    [TestCase("to")]
    [TestCase("To")]
    [TestCase("tO")]
    public void FindToKeyword_CaseInsensitive_Works(string toKeyword)
    {
        string[] args = ["100", "100", toKeyword, "200", "200"];
        int index = Array.FindIndex(args, a => a.Equals("TO", StringComparison.OrdinalIgnoreCase));
        Assert.That(index, Is.EqualTo(2));
    }

    /// <summary>
    /// Verifies TO keyword missing returns -1.
    /// </summary>
    [Test]
    public void FindToKeyword_Missing_ReturnsNegative()
    {
        string[] args = ["100", "100", "1"];
        int index = Array.FindIndex(args, a => a.Equals("TO", StringComparison.OrdinalIgnoreCase));
        Assert.That(index, Is.EqualTo(-1));
    }

    /// <summary>
    /// Verifies point mode argument count.
    /// </summary>
    [Test]
    public void HplotArgs_PointMode_HasTwoOrThreeArgs()
    {
        string[] pointOnly = ["100", "100"];
        string[] pointWithColor = ["100", "100", "0"];

        Assert.That(pointOnly.Length, Is.EqualTo(2));
        Assert.That(pointWithColor.Length, Is.EqualTo(3));
    }

    /// <summary>
    /// Verifies line mode argument count.
    /// </summary>
    [Test]
    public void HplotArgs_LineMode_HasFiveOrSixArgs()
    {
        string[] lineOnly = ["100", "100", "TO", "200", "200"];
        string[] lineWithColor = ["100", "100", "TO", "200", "200", "0"];

        Assert.That(lineOnly.Length, Is.EqualTo(5));
        Assert.That(lineWithColor.Length, Is.EqualTo(6));
    }

    /// <summary>
    /// Verifies horizontal line covers all expected X coordinates.
    /// </summary>
    [Test]
    public void HiResLine_Horizontal_CoversAllXCoordinates()
    {
        var points = DrawLine(0, 100, 279, 100);

        // Should have 280 points
        Assert.That(points.Count, Is.EqualTo(280));

        // All Y coordinates should be 100
        Assert.That(points.All(p => p.Y == 100), Is.True);

        // All X coordinates from 0 to 279 should be present
        var xCoords = points.Select(p => p.X).OrderBy(x => x).ToList();
        Assert.That(xCoords.First(), Is.EqualTo(0));
        Assert.That(xCoords.Last(), Is.EqualTo(279));
    }

    /// <summary>
    /// Verifies vertical line covers all expected Y coordinates.
    /// </summary>
    [Test]
    public void HiResLine_Vertical_CoversAllYCoordinates()
    {
        var points = DrawLine(140, 0, 140, 191);

        // Should have 192 points
        Assert.That(points.Count, Is.EqualTo(192));

        // All X coordinates should be 140
        Assert.That(points.All(p => p.X == 140), Is.True);

        // All Y coordinates from 0 to 191 should be present
        var yCoords = points.Select(p => p.Y).OrderBy(y => y).ToList();
        Assert.That(yCoords.First(), Is.EqualTo(0));
        Assert.That(yCoords.Last(), Is.EqualTo(191));
    }

    /// <summary>
    /// Verifies diagonal line across entire screen.
    /// </summary>
    [Test]
    public void HiResLine_DiagonalFullScreen_WorksCorrectly()
    {
        var points = DrawLine(0, 0, 279, 191);

        // Should include start and end points
        Assert.That(points, Does.Contain(new Point(0, 0)));
        Assert.That(points, Does.Contain(new Point(279, 191)));

        // All points should be within bounds
        Assert.That(points.All(p => p.X >= 0 && p.X < 280), Is.True);
        Assert.That(points.All(p => p.Y >= 0 && p.Y < 192), Is.True);
    }

    private static ushort ComputeHiResRowAddress(int row)
    {
        int group = row / 64;
        int subRow = (row % 64) / 8;
        int scanLine = row % 8;
        return (ushort)(HiResPage1Base + (scanLine * 1024) + (subRow * 128) + (group * 40));
    }

    private static (int ByteOffset, int BitPosition) HiResPixelToByteAndBit(int x)
    {
        int byteOffset = x / 7;
        int bitPosition = x % 7;
        return (byteOffset, bitPosition);
    }

    private static byte SetHiResBit(byte current, int bit, bool set)
    {
        if (set)
        {
            return (byte)(current | (1 << bit));
        }
        else
        {
            return (byte)(current & ~(1 << bit));
        }
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