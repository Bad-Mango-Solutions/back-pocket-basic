// <copyright file="BoolToColorConverter.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Editor.ViewModels;

using System.Globalization;

using Avalonia.Data.Converters;
using Avalonia.Media;

/// <summary>
/// Converts a boolean to a connection status color.
/// </summary>
public sealed class BoolToColorConverter : IValueConverter
{
    /// <summary>
    /// Gets the singleton instance of the converter.
    /// </summary>
    public static BoolToColorConverter Instance { get; } = new();

    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isConnected && isConnected)
        {
            return new SolidColorBrush(Color.FromRgb(0, 200, 0));
        }

        return new SolidColorBrush(Color.FromRgb(100, 100, 100));
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}