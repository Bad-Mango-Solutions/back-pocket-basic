// <copyright file="BasicValue.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Runtime;

/// <summary>
/// Represents a BASIC value (number or string).
/// </summary>
public readonly struct BasicValue
{
    /// <summary>
    /// Epsilon value for approximate floating-point comparisons in BASIC operations.
    /// This tolerance (1e-10) is appropriate for the precision of Applesoft BASIC.
    /// </summary>
    private const double Epsilon = 1e-10;

    private readonly double numericValue;
    private readonly string? stringValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="BasicValue"/> struct with a numeric value.
    /// </summary>
    /// <param name="numericValue">The numeric value to initialize the instance with.</param>
    private BasicValue(double numericValue)
    {
        this.numericValue = numericValue;
        stringValue = null;
        IsString = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BasicValue"/> struct with a string value.
    /// </summary>
    /// <param name="stringValue">The string value to initialize the instance with.</param>
    private BasicValue(string stringValue)
    {
        numericValue = 0;
        this.stringValue = stringValue;
        IsString = true;
    }

    /// <summary>
    /// Gets a <see cref="BasicValue"/> representing the numeric value zero.
    /// </summary>
    /// <remarks>
    /// This property is used as the default numeric value in various contexts,
    /// such as initializing numeric arrays or representing a default numeric state.
    /// </remarks>
    public static BasicValue Zero => new(0);

    /// <summary>
    /// Gets a <see cref="BasicValue"/> instance representing an empty string.
    /// </summary>
    /// <remarks>
    /// This property is used as the default value for string-based variables or arrays
    /// in the Applesoft BASIC interpreter.
    /// </remarks>
    public static BasicValue Empty => new(string.Empty);

    /// <summary>
    /// Gets a value indicating whether this <see cref="BasicValue"/> represents a string.
    /// </summary>
    /// <value>
    /// <c>true</c> if this instance represents a string; otherwise, <c>false</c>.
    /// </value>
    public bool IsString { get; }

    /// <summary>
    /// Gets a value indicating whether this <see cref="BasicValue"/> represents a numeric value.
    /// </summary>
    /// <remarks>
    /// A <see cref="BasicValue"/> is considered numeric if it is not a string.
    /// </remarks>
    public bool IsNumeric => !IsString;

    /// <summary>
    /// Implicitly converts a <see cref="double"/> value to a <see cref="BasicValue"/> instance.
    /// </summary>
    /// <param name="value">The numeric value to convert.</param>
    /// <returns>A <see cref="BasicValue"/> instance representing the specified numeric value.</returns>
    public static implicit operator BasicValue(double value) => FromNumber(value);

    /// <summary>
    /// Implicitly converts an <see cref="int"/> value to a <see cref="BasicValue"/> instance.
    /// </summary>
    /// <param name="value">The integer value to convert.</param>
    /// <returns>A <see cref="BasicValue"/> instance representing the specified integer value.</returns>
    public static implicit operator BasicValue(int value) => FromNumber(value);

    /// <summary>
    /// Implicitly converts a <see cref="string"/> value to a <see cref="BasicValue"/> instance.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    /// <returns>A <see cref="BasicValue"/> instance representing the specified string value.</returns>
    public static implicit operator BasicValue(string value) => FromString(value);

    /// <summary>
    /// Adds two <see cref="BasicValue"/> instances.
    /// </summary>
    /// <param name="a">The first <see cref="BasicValue"/> operand.</param>
    /// <param name="b">The second <see cref="BasicValue"/> operand.</param>
    /// <returns>
    /// A new <see cref="BasicValue"/> representing the result of the addition.
    /// If either operand is a string, the result is the concatenation of their string representations.
    /// Otherwise, the result is the sum of their numeric values.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the operation cannot be performed due to invalid operand types.
    /// </exception>
    public static BasicValue operator +(BasicValue a, BasicValue b)
    {
        if (a.IsString || b.IsString)
        {
            return FromString(a.AsString() + b.AsString());
        }

        return FromNumber(a.AsNumber() + b.AsNumber());
    }

    /// <summary>
    /// Subtracts one <see cref="BasicValue"/> from another.
    /// </summary>
    /// <param name="a">The minuend, represented as a <see cref="BasicValue"/>.</param>
    /// <param name="b">The subtrahend, represented as a <see cref="BasicValue"/>.</param>
    /// <returns>A new <see cref="BasicValue"/> representing the result of the subtraction.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if either <paramref name="a"/> or <paramref name="b"/> is not numeric.
    /// </exception>
    public static BasicValue operator -(BasicValue a, BasicValue b)
        => FromNumber(a.AsNumber() - b.AsNumber());

    /// <summary>
    /// Multiplies two <see cref="BasicValue"/> instances.
    /// </summary>
    /// <param name="a">The first <see cref="BasicValue"/> operand.</param>
    /// <param name="b">The second <see cref="BasicValue"/> operand.</param>
    /// <returns>
    /// A new <see cref="BasicValue"/> instance representing the product of the numeric values of
    /// <paramref name="a"/> and <paramref name="b"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if either <paramref name="a"/> or <paramref name="b"/> is not numeric.
    /// </exception>
    public static BasicValue operator *(BasicValue a, BasicValue b)
        => FromNumber(a.AsNumber() * b.AsNumber());

    /// <summary>
    /// Divides one <see cref="BasicValue"/> by another.
    /// </summary>
    /// <param name="a">The dividend, represented as a <see cref="BasicValue"/>.</param>
    /// <param name="b">The divisor, represented as a <see cref="BasicValue"/>.</param>
    /// <returns>A new <see cref="BasicValue"/> representing the result of the division.</returns>
    /// <exception cref="BasicRuntimeException">
    /// Thrown when the divisor is zero, as division by zero is not allowed.
    /// </exception>
    public static BasicValue operator /(BasicValue a, BasicValue b)
    {
        double divisor = b.AsNumber();
        if (divisor == 0.0)
        {
            throw new BasicRuntimeException("?DIVISION BY ZERO ERROR");
        }

        return FromNumber(a.AsNumber() / divisor);
    }

    /// <summary>
    /// Computes the result of raising one <see cref="BasicValue"/> to the power of another.
    /// </summary>
    /// <param name="a">The base value, represented as a <see cref="BasicValue"/>.</param>
    /// <param name="b">The exponent value, represented as a <see cref="BasicValue"/>.</param>
    /// <returns>A <see cref="BasicValue"/> representing the result of raising <paramref name="a"/> to the power of <paramref name="b"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if either <paramref name="a"/> or <paramref name="b"/> is not numeric.
    /// </exception>
    public static BasicValue operator ^(BasicValue a, BasicValue b)
        => FromNumber(Math.Pow(a.AsNumber(), b.AsNumber()));

    /// <summary>
    /// Negates the numeric value of the specified <see cref="BasicValue"/> instance.
    /// </summary>
    /// <param name="a">The <see cref="BasicValue"/> instance to negate.</param>
    /// <returns>
    /// A new <see cref="BasicValue"/> instance representing the negated numeric value of the input.
    /// If the input is not numeric, the behavior depends on the implementation of <see cref="BasicValue.AsNumber"/>.
    /// </returns>
    public static BasicValue operator -(BasicValue a)
        => FromNumber(-a.AsNumber());

    /// <summary>
    /// Determines whether two <see cref="BasicValue"/> instances are equal.
    /// </summary>
    /// <param name="a">The first <see cref="BasicValue"/> to compare.</param>
    /// <param name="b">The second <see cref="BasicValue"/> to compare.</param>
    /// <returns>
    /// <c>true</c> if both <see cref="BasicValue"/> instances are equal; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// If both instances represent strings, they are compared using string equality.
    /// If both instances represent numbers, they are compared numerically using exact equality
    /// to maintain the Equals/GetHashCode contract required for use in hash-based collections.
    /// </remarks>
    public static bool operator ==(BasicValue a, BasicValue b)
    {
        if (a.IsString && b.IsString)
        {
            return a.AsString() == b.AsString();
        }

        return a.AsNumber() == b.AsNumber();
    }

    /// <summary>
    /// Determines whether two <see cref="BasicValue"/> instances are not equal.
    /// </summary>
    /// <param name="a">The first <see cref="BasicValue"/> to compare.</param>
    /// <param name="b">The second <see cref="BasicValue"/> to compare.</param>
    /// <returns>
    /// <c>true</c> if the two <see cref="BasicValue"/> instances are not equal; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// If both instances represent strings, they are compared using string inequality.
    /// If both instances represent numbers, they are compared numerically.
    /// </remarks>
    public static bool operator !=(BasicValue a, BasicValue b) => !(a == b);

    /// <summary>
    /// Determines whether one <see cref="BasicValue"/> is less than another.
    /// </summary>
    /// <param name="a">The first <see cref="BasicValue"/> to compare.</param>
    /// <param name="b">The second <see cref="BasicValue"/> to compare.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="a"/> is less than <paramref name="b"/>; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// If both values are strings, the comparison is performed using ordinal string comparison.
    /// If both values are numeric, the comparison is performed numerically.
    /// </remarks>
    public static bool operator <(BasicValue a, BasicValue b)
    {
        if (a.IsString && b.IsString)
        {
            return string.Compare(a.AsString(), b.AsString(), StringComparison.Ordinal) < 0;
        }

        return a.AsNumber() < b.AsNumber();
    }

    /// <summary>
    /// Determines whether one <see cref="BasicValue"/> is greater than another.
    /// </summary>
    /// <param name="a">The first <see cref="BasicValue"/> to compare.</param>
    /// <param name="b">The second <see cref="BasicValue"/> to compare.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="a"/> is greater than <paramref name="b"/>; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// If both <paramref name="a"/> and <paramref name="b"/> are strings, the comparison is performed
    /// using <see cref="string.Compare(string, string, StringComparison)"/> with <see cref="StringComparison.Ordinal"/>.
    /// If either value is numeric, the comparison is performed numerically.
    /// </remarks>
    public static bool operator >(BasicValue a, BasicValue b)
    {
        if (a.IsString && b.IsString)
        {
            return string.Compare(a.AsString(), b.AsString(), StringComparison.Ordinal) > 0;
        }

        return a.AsNumber() > b.AsNumber();
    }

    /// <summary>
    /// Determines whether one <see cref="BasicValue"/> is less than or equal to another.
    /// </summary>
    /// <param name="a">The first <see cref="BasicValue"/> to compare.</param>
    /// <param name="b">The second <see cref="BasicValue"/> to compare.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="a"/> is less than or equal to <paramref name="b"/>; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// If both <paramref name="a"/> and <paramref name="b"/> are strings, the comparison is performed
    /// using <see cref="string.Compare(string, string, StringComparison)"/> with <see cref="StringComparison.Ordinal"/>.
    /// If either value is numeric, the comparison is performed numerically.
    /// </remarks>
    public static bool operator <=(BasicValue a, BasicValue b) => !(a > b);

    /// <summary>
    /// Determines whether one <see cref="BasicValue"/> is greater than or equal to another.
    /// </summary>
    /// <param name="a">The first <see cref="BasicValue"/> to compare.</param>
    /// <param name="b">The second <see cref="BasicValue"/> to compare.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="a"/> is greater than or equal to <paramref name="b"/>; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// If both values are strings, the comparison is performed using ordinal string comparison.
    /// If both values are numeric, the comparison is performed numerically.
    /// </remarks>
    public static bool operator >=(BasicValue a, BasicValue b) => !(a < b);

    /// <summary>
    /// Creates a new instance of the <see cref="BasicValue"/> struct from a numeric value.
    /// </summary>
    /// <param name="value">The numeric value to initialize the <see cref="BasicValue"/> instance with.</param>
    /// <returns>A <see cref="BasicValue"/> instance representing the specified numeric value.</returns>
    public static BasicValue FromNumber(double value) => new(value);

    /// <summary>
    /// Creates a new instance of the <see cref="BasicValue"/> struct from a string value.
    /// </summary>
    /// <param name="value">The string value to initialize the <see cref="BasicValue"/> instance with.</param>
    /// <returns>A <see cref="BasicValue"/> instance representing the specified string value.</returns>
    public static BasicValue FromString(string value) => new(value);

    /// <summary>
    /// Converts the current <see cref="BasicValue"/> instance to a numeric value.
    /// </summary>
    /// <returns>
    /// The numeric value represented by the current <see cref="BasicValue"/> instance.
    /// If the instance represents a string, it attempts to parse the string as a number.
    /// If parsing fails, it returns 0.
    /// </returns>
    public double AsNumber()
    {
        if (IsString)
        {
            // Try to parse string as number (Applesoft behavior)
            return double.TryParse(stringValue, out double result) ? result : 0;
        }

        return numericValue;
    }

    /// <summary>
    /// Converts the current <see cref="BasicValue"/> instance to its string representation.
    /// </summary>
    /// <returns>
    /// If the current instance represents a string, returns the string value.
    /// If the current instance represents a numeric value:
    /// - Returns "0" if the value is zero.
    /// - Returns the integer representation if the value is a whole number and within a reasonable range.
    /// - Returns the scientific notation if the value is very large, very small, or not a whole number.
    /// </returns>
    public string AsString()
    {
        if (IsString)
        {
            return stringValue ?? string.Empty;
        }

        // Format number like Applesoft
        if (numericValue == 0)
        {
            return "0";
        }

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (numericValue == Math.Floor(numericValue) && Math.Abs(numericValue) < 1e10)
        {
            return ((long)numericValue).ToString();
        }

        // Use E notation for very large/small numbers
        if (Math.Abs(numericValue) >= 1e10 || (Math.Abs(numericValue) < 0.01 && numericValue != 0.0))
        {
            return numericValue.ToString("E8").TrimEnd('0').TrimEnd('.');
        }

        return numericValue.ToString("G9");
    }

    /// <summary>
    /// Converts the current <see cref="BasicValue"/> to an integer representation.
    /// </summary>
    /// <remarks>
    /// If the value is numeric, it is truncated toward zero to produce an integer.
    /// If the value is not numeric, an exception may be thrown.
    /// </remarks>
    /// <returns>
    /// An integer representation of the current <see cref="BasicValue"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the current <see cref="BasicValue"/> cannot be converted to a numeric value.
    /// </exception>
    public int AsInteger()
    {
        double num = AsNumber();

        // Applesoft truncates toward zero
        return num >= 0 ? (int)Math.Floor(num) : (int)Math.Ceiling(num);
    }

    /// <summary>
    /// Determines whether the current <see cref="BasicValue"/> instance represents a "true" value.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the instance is a non-empty string or a non-zero numeric value; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// A <see cref="BasicValue"/> is considered "true" if it is either:
    /// <list type="bullet">
    /// <item>A string that is not null or empty.</item>
    /// <item>A numeric value that is not equal to zero.</item>
    /// </list>
    /// </remarks>
    public bool IsTrue()
    {
        if (IsString)
        {
            return !string.IsNullOrEmpty(stringValue);
        }

        return numericValue != 0.0;
    }

    /// <summary>
    /// Returns a string representation of the current <see cref="BasicValue"/> instance.
    /// </summary>
    /// <returns>
    /// If the current instance represents a string, returns the string value.
    /// If the current instance represents a numeric value:
    /// - Returns "0" if the value is zero.
    /// - Returns the integer representation if the value is a whole number and within a reasonable range.
    /// - Returns the scientific notation if the value is very large, very small, or not a whole number.
    /// </returns>
    public override string ToString() => AsString();

    /// <summary>
    /// Determines whether the specified object is equal to the current <see cref="BasicValue"/> instance.
    /// </summary>
    /// <param name="obj">The object to compare with the current <see cref="BasicValue"/> instance.</param>
    /// <returns>
    /// <c>true</c> if the specified object is a <see cref="BasicValue"/> and is equal to the current instance;
    /// otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object? obj) => obj is BasicValue other && this == other;

    /// <summary>
    /// Returns a hash code for the current <see cref="BasicValue"/> instance.
    /// </summary>
    /// <returns>
    /// A hash code that represents the current <see cref="BasicValue"/> instance.
    /// If the instance represents a string, the hash code of the string is returned.
    /// If the instance represents a numeric value, the hash code of the numeric value is returned.
    /// </returns>
    public override int GetHashCode() => IsString ? stringValue?.GetHashCode() ?? 0 : numericValue.GetHashCode();

    /// <summary>
    /// Determines whether this <see cref="BasicValue"/> is approximately equal to another using epsilon-based comparison.
    /// </summary>
    /// <param name="other">The <see cref="BasicValue"/> to compare with.</param>
    /// <returns>
    /// <c>true</c> if both values are approximately equal within epsilon tolerance; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method is intended for BASIC interpreter operations where floating-point equality should use
    /// tolerance-based comparison. For exact equality (e.g., for use in hash-based collections), use the
    /// == operator or Equals method instead.
    /// String values are compared exactly, not approximately.
    /// </remarks>
    public bool ApproximatelyEquals(BasicValue other)
    {
        if (IsString && other.IsString)
        {
            return AsString() == other.AsString();
        }

        if (IsString || other.IsString)
        {
            return false;
        }

        return AreDoublesApproximatelyEqual(AsNumber(), other.AsNumber());
    }

    /// <summary>
    /// Determines whether two double values are approximately equal within epsilon tolerance.
    /// </summary>
    /// <param name="a">The first double value to compare.</param>
    /// <param name="b">The second double value to compare.</param>
    /// <returns>
    /// <c>true</c> if the absolute difference between the two values is less than or equal to epsilon
    /// or the relative difference is within the tolerance; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Uses a hybrid approach: absolute epsilon for small numbers, relative epsilon for large numbers.
    /// This ensures accurate comparisons across different magnitude ranges.
    /// </remarks>
    private static bool AreDoublesApproximatelyEqual(double a, double b)
    {
        double diff = Math.Abs(a - b);

        // For numbers close to zero, use absolute epsilon
        if (diff <= Epsilon)
        {
            return true;
        }

        // For larger numbers, use relative epsilon
        double maxAbs = Math.Max(Math.Abs(a), Math.Abs(b));
        return diff <= Epsilon * maxAbs;
    }
}