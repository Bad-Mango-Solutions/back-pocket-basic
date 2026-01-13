// <copyright file="BoolToConnectionStatusConverter.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Editor.ViewModels;

using System.Globalization;

using Avalonia.Data.Converters;

/// <summary>
/// Converts a boolean to a connection status string.
/// </summary>
public sealed class BoolToConnectionStatusConverter : IValueConverter
{
    /// <summary>
    /// Gets the singleton instance of the converter.
    /// </summary>
    public static BoolToConnectionStatusConverter Instance { get; } = new();

    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isConnected && isConnected)
        {
            return "Connected";
        }

        return "Disconnected";
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}