// <copyright file="EnumToBoolConverter.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Editor.ViewModels;

using System.Globalization;

using Avalonia.Data.Converters;

using Devices;

/// <summary>
/// Converts an enum value to a boolean for radio button binding.
/// </summary>
public sealed class EnumToBoolConverter : IValueConverter
{
    /// <summary>
    /// Gets the singleton instance of the converter.
    /// </summary>
    public static EnumToBoolConverter Instance { get; } = new();

    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
        {
            return false;
        }

        string parameterString = parameter.ToString() ?? string.Empty;
        return value.ToString() == parameterString;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true && parameter != null)
        {
            string parameterString = parameter.ToString() ?? string.Empty;
            return Enum.Parse(targetType, parameterString);
        }

        return GlyphLoadTarget.GlyphRom;
    }
}