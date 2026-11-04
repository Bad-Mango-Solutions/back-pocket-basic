// <copyright file="Generator.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Unit.Components;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

using Moq;

using Serilog;

/// <summary>Defines a random value generator that handles text, numbers, and other data.</summary>
[ExcludeFromCodeCoverage] // Utility service with edge case handling.
public class Generator
{
    /// <summary>Uppercase characters A through Z.</summary>
    public const string Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    /// <summary>Lowercase characters a through z.</summary>
    public const string Lower = "abcdefghijklmnopqrstuvwxyz";

    /// <summary>Digits 0 through 9.</summary>
    public const string Numbers = "0123456789";

    /// <summary>Hexadecimal digits 0 through f.</summary>
    public const string Hexadecimal = "0123456789abcdef";

    /// <summary>Defines alphanumeric characters. A-Z, a-z, and 0-9.</summary>
    public const string Alphanumeric = Upper + Lower + Numbers;

    /// <summary>Defines universally file-safe characters. 0-9 and A-Z.</summary>
    public const string FileSafe = Upper + Numbers;

    /// <summary>Defines URL-safe characters. Alphanumeric plus _ and -.</summary>
    public const string UrlSafe = Upper + Lower + Numbers + "_-";

    /// <summary>Defines safe characters for identifier names. 0-9, a-z, -, _, ., and space.</summary>
    public const string NameSafe = Lower + Numbers + "_-. ";

    private static readonly Lazy<string> LazyWestern = new(() => CharacterRange(end: 0x007E));

    private static readonly Lazy<string> LazyUnicode = new(() => CharacterRange());

    /// <summary>Lazily constructs a singleton generator.</summary>
    private static readonly Lazy<Generator> Singleton = new(() => new Generator());

    /// <summary>Lazily constructs a list of valid distinct cultures.</summary>
    private static readonly Lazy<List<CultureInfo>> CultureData = new(
        () => CultureInfo.GetCultures(CultureTypes.SpecificCultures).Where(c => c.LCID != 0x1000).ToList());

    /// <summary>The seed for the random value generator.</summary>
    private readonly Random random;

    /// <summary>Initializes a new instance of the <see cref="Generator"/> class.</summary>
    public Generator()
    {
        var bytes = RandomNumberGenerator.GetBytes(4);
        random = new Random(BitConverter.ToInt32(bytes));
    }

    /// <summary>Initializes a new instance of the <see cref="Generator"/> class.</summary>
    /// <param name="seed">The seed for the random value generator.</param>
    public Generator(int seed) => random = new Random(seed);

    /// <summary>Defines the letters, digits, and punctuation marks available through most western keyboards.</summary>
    /// <remarks>In UTF-8 notation, the range of 0x0021 (!) through 0x007E (~).</remarks>
    public static string Western { get; } = LazyWestern.Value;

    /// <summary>Defines all UTF-8 characters which are letters, digits, or punctuation marks.</summary>
    /// <remarks>In UTF-8 notation, the range of 0x0000 through 0xFFFF.</remarks>
    public static string Unicode { get; } = LazyUnicode.Value;

    /// <summary>A singleton generator for global usage.</summary>
    public static Generator Global => Singleton.Value;

    /// <summary>Generates a new mock logger, preconfigured to return itself during chain calls such as ForContext.</summary>
    /// <returns>A logging mock.</returns>
    public static Mock<ILogger> Log() => new() { DefaultValueProvider = new SelfProvider() };

    /// <summary>Generates a random string of a given length.</summary>
    /// <param name="length">The length of the string. If -1, will be a random number between 5 and 25.</param>
    /// <param name="subset">The character subset to use when generating text.</param>
    /// <returns>A random string.</returns>
    public string Text(int length, string subset = Alphanumeric)
    {
        var set = subset.ToCharArray();
        var buffer = new StringBuilder(length);
        for (var i = 0; i < length; i++) { buffer.Append(set[random.Next(set.Length)]); }

        return buffer.ToString();
    }

    /// <summary>Generates a random string of a given length.</summary>
    /// <param name="subset">The character subset to use when generating text.</param>
    /// <returns>A random string.</returns>
    public string Text(string subset = Alphanumeric) => Text(Integer(5, 26), subset);

    /// <summary>Gets a random byte value.</summary>
    /// <returns>A random byte value.</returns>
    public byte Byte()
    {
        var result = new byte[1];
        random.NextBytes(result);
        return result[0];
    }

    /// <summary>Gets a random boolean value.</summary>
    /// <param name="odds">The odds that the result will be <see langword="true"/>.</param>
    /// <returns>A random boolean value.</returns>
    public bool Boolean(double odds = 0.5d) => random.NextDouble() < odds;

    /// <summary>Gets a random integer value.</summary>
    /// <returns>A random integer value.</returns>
    public int Integer() => random.Next();

    /// <summary>Gets a random integer value.</summary>
    /// <param name="maxValue">The maximum value of the integer.</param>
    /// <returns>A random integer value.</returns>
    public int Integer(int maxValue) => random.Next(maxValue);

    /// <summary>Gets a random integer value.</summary>
    /// <param name="minValue">The inclusive minimum bound of the result.</param>
    /// <param name="maxValue">The exclusive maximum bound of the result.</param>
    /// <returns>A random integer value.</returns>
    public int Integer(int minValue, int maxValue) => random.Next(minValue, maxValue);

    /// <summary>Gets a random long value.</summary>
    /// <returns>A random long value.</returns>
    public long Long() => Long(0L, long.MaxValue);

    /// <summary>Gets a random long value.</summary>
    /// <param name="maxValue">The exclusive maximum bound of the result.</param>
    /// <returns>A random long value.</returns>
    public long Long(long maxValue) => Long(0L, maxValue);

    /// <summary>Gets a random long value.</summary>
    /// <param name="minValue">The inclusive minimum bound of the result.</param>
    /// <param name="maxValue">The exclusive maximum bound of the result.</param>
    /// <returns>A random long value.</returns>
    public long Long(long minValue, long maxValue) =>
        (long)(random.NextDouble() * (maxValue - minValue)) + minValue;

    /// <summary>Gets a random decimal value.</summary>
    /// <returns>A random decimal value.</returns>
    public decimal Decimal() => new(random.Next(), random.Next(), random.Next(), false, 0);

    /// <summary>Gets a random decimal value.</summary>
    /// <param name="maxValue">The maximum value of the decimal.</param>
    /// <returns>A random decimal value.</returns>
    public decimal Decimal(decimal maxValue) => Decimal() % maxValue;

    /// <summary>Gets a random decimal value.</summary>
    /// <param name="minValue">The minimum value of the decimal.</param>
    /// <param name="maxValue">The maximum value of the decimal.</param>
    /// <returns>A random decimal value.</returns>
    public decimal Decimal(decimal minValue, decimal maxValue)
    {
        var range = maxValue - minValue;
        var peak = decimal.MaxValue - (decimal.MaxValue % range);
        decimal baseValue;

        do { baseValue = Decimal(); } while (baseValue > peak);

        return (baseValue % range) + minValue;
    }

    /// <summary>Generates a random valid e-mail address.</summary>
    /// <returns>A random mail address.</returns>
    public MailAddress Email() =>
        new($"{ValidSegment()}@{ValidSegment()}.{Mini()}", Text());

    /// <summary>Generates a random URL.</summary>
    /// <param name="maxSegments">The maximum number of route segments to generate.</param>
    /// <returns>A random URI.</returns>
    public Uri Url(int maxSegments = 1)
    {
        var segmentCount = random.Next(0, maxSegments);
        var segments = Enumerable.Range(0, segmentCount).Select(s => ValidSegment());

        var endingPart = segmentCount > 0 ? "/" : string.Empty;
        var useTls = Boolean() ? "s" : string.Empty;
        var url = $"http{useTls}://{ValidSegment()}.{ValidSegment()}.{Mini()}/{string.Join("/", segments)}{endingPart}";

        return new Uri(url);
    }

    /// <summary>Generates a random hash.</summary>
    /// <returns>A random hash.</returns>
    public byte[] Hash()
    {
        var bytes = Encoding.UTF8.GetBytes(Text());
        using var sha = SHA256.Create();
        return sha.ComputeHash(bytes);
    }

    /// <summary>Generates a sequence of random values.</summary>
    /// <typeparam name="T">The type of values being generated.</typeparam>
    /// <param name="minSize">The inclusive minimum size of the sequence.</param>
    /// <param name="maxSize">The exclusive maximum size of the sequence.</param>
    /// <param name="selection">A generator for each item in the sequence.</param>
    /// <returns>A random set of values.</returns>
    public List<T> Sequence<T>(int minSize, int maxSize, Func<int, T> selection) =>
        Enumerable.Range(0, Integer(minSize, maxSize)).Select(selection).ToList();

    /// <summary>Generates one entry of a given collection.</summary>
    /// <typeparam name="T">The type of collection to pull from.</typeparam>
    /// <param name="set">A collection of possible objects.</param>
    /// <returns>A single result.</returns>
    public T OneOf<T>(IEnumerable<T> set)
    {
        var localSet = set.ToArray();
        return localSet[Integer(0, localSet.Length)];
    }

    /// <summary>Generates a random enum of a given type.</summary>
    /// <typeparam name="TEnum">The type of enum to generate.</typeparam>
    /// <returns>A randomly selected enum.</returns>
    public TEnum Enum<TEnum>()
    {
        var values = System.Enum.GetValues(typeof(TEnum));
        var index = Integer(0, values.Length);
        return (TEnum)values.GetValue(index)!;
    }

    /// <summary>Generates a random date and time.</summary>
    /// <param name="increment">The amount of time to offset the date from now.</param>
    /// <param name="multiplier">An optional multiplier to apply to the increment.</param>
    /// <param name="start">The optional date to start calculating from.</param>
    /// <returns>A random date based around the start date, or <see cref="DateTimeOffset.UtcNow"/> if null.</returns>
    public DateTimeOffset Date(long increment, int multiplier = -1, DateTimeOffset? start = null) => (start ?? DateTimeOffset.UtcNow).AddTicks(Long(increment) * multiplier);

    /// <summary>Fetches a random culture.</summary>
    /// <returns>A random culture.</returns>
    public CultureInfo Culture() => OneOf(CultureData.Value);

    private static string CharacterRange(int start = 0x0021, int end = 0xFFFF)
    {
        var range = Enumerable.Range(start, end)
            .Select(i => (char)i)
            .Where(c => char.IsLetterOrDigit(c) || char.IsPunctuation(c))
            .ToArray();

        return new string(range);
    }

    private string ValidSegment() => Regex.Replace(Text().Trim('.'), "[\\.]+", ".");

    private string Mini(int length = 3) => Text(length, Lower);

    private class SelfProvider : LookupOrFallbackDefaultValueProvider
    {
        public SelfProvider() => Register(typeof(ILogger), (type, mock) => mock.Object);
    }
}