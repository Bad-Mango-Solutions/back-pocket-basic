// <copyright file="BasicValue.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Runtime;

/// <summary>
/// Represents a BASIC value (number or string).
/// </summary>
public readonly struct BasicValue
{
    private readonly double _numericValue;
    private readonly string? _stringValue;
    private readonly bool _isString;

    public bool IsString => _isString;

    public bool IsNumeric => !_isString;

    private BasicValue(double numericValue)
    {
        _numericValue = numericValue;
        _stringValue = null;
        _isString = false;
    }

    private BasicValue(string stringValue)
    {
        _numericValue = 0;
        _stringValue = stringValue;
        _isString = true;
    }

    public static BasicValue FromNumber(double value) => new(value);

    public static BasicValue FromString(string value) => new(value);

    public static BasicValue Zero => new(0);

    public static BasicValue Empty => new(string.Empty);

    public double AsNumber()
    {
        if (_isString)
        {
            // Try to parse string as number (Applesoft behavior)
            return double.TryParse(_stringValue, out double result) ? result : 0;
        }
        return _numericValue;
    }

    public string AsString()
    {
        if (_isString)
        {
            return _stringValue ?? string.Empty;
        }

        // Format number like Applesoft
        if (_numericValue == 0) return "0";
        if (_numericValue == Math.Floor(_numericValue) && Math.Abs(_numericValue) < 1e10)
        {
            return ((long)_numericValue).ToString();
        }

        // Use E notation for very large/small numbers
        if (Math.Abs(_numericValue) >= 1e10 || (Math.Abs(_numericValue) < 0.01 && _numericValue != 0))
        {
            return _numericValue.ToString("E8").TrimEnd('0').TrimEnd('.');
        }

        return _numericValue.ToString("G9");
    }

    public int AsInteger()
    {
        double num = AsNumber();
        // Applesoft truncates toward zero
        return num >= 0 ? (int)Math.Floor(num) : (int)Math.Ceiling(num);
    }

    public bool IsTrue()
    {
        if (_isString)
        {
            return !string.IsNullOrEmpty(_stringValue);
        }
        return _numericValue != 0;
    }

    public override string ToString() => AsString();

    public static implicit operator BasicValue(double value) => FromNumber(value);

    public static implicit operator BasicValue(int value) => FromNumber(value);

    public static implicit operator BasicValue(string value) => FromString(value);

    public static BasicValue operator +(BasicValue a, BasicValue b)
    {
        if (a.IsString || b.IsString)
        {
            return FromString(a.AsString() + b.AsString());
        }
        return FromNumber(a.AsNumber() + b.AsNumber());
    }

    public static BasicValue operator -(BasicValue a, BasicValue b)
        => FromNumber(a.AsNumber() - b.AsNumber());

    public static BasicValue operator *(BasicValue a, BasicValue b)
        => FromNumber(a.AsNumber() * b.AsNumber());

    public static BasicValue operator /(BasicValue a, BasicValue b)
    {
        double divisor = b.AsNumber();
        if (divisor == 0)
        {
            throw new BasicRuntimeException("?DIVISION BY ZERO ERROR");
        }
        return FromNumber(a.AsNumber() / divisor);
    }

    public static BasicValue operator ^(BasicValue a, BasicValue b)
        => FromNumber(Math.Pow(a.AsNumber(), b.AsNumber()));

    public static BasicValue operator -(BasicValue a)
        => FromNumber(-a.AsNumber());

    public static bool operator ==(BasicValue a, BasicValue b)
    {
        if (a.IsString && b.IsString)
        {
            return a.AsString() == b.AsString();
        }
        return a.AsNumber() == b.AsNumber();
    }

    public static bool operator !=(BasicValue a, BasicValue b) => !(a == b);

    public static bool operator <(BasicValue a, BasicValue b)
    {
        if (a.IsString && b.IsString)
        {
            return string.Compare(a.AsString(), b.AsString(), StringComparison.Ordinal) < 0;
        }
        return a.AsNumber() < b.AsNumber();
    }

    public static bool operator >(BasicValue a, BasicValue b)
    {
        if (a.IsString && b.IsString)
        {
            return string.Compare(a.AsString(), b.AsString(), StringComparison.Ordinal) > 0;
        }
        return a.AsNumber() > b.AsNumber();
    }

    public static bool operator <=(BasicValue a, BasicValue b) => !(a > b);

    public static bool operator >=(BasicValue a, BasicValue b) => !(a < b);

    public override bool Equals(object? obj) => obj is BasicValue other && this == other;

    public override int GetHashCode() => _isString ? _stringValue?.GetHashCode() ?? 0 : _numericValue.GetHashCode();
}
