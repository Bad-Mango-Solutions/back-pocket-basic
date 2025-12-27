// <copyright file="MainWindow.axaml.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Views;

using System;
using System.Globalization;

using Avalonia.Controls;
using Avalonia.Data.Converters;

/// <summary>
/// Main application window containing navigation and content area.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Converter for theme toggle button text.
    /// </summary>
    public static readonly IValueConverter ThemeTextConverter = new ThemeTextValueConverter();

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
    }

    private sealed class ThemeTextValueConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isDark)
            {
                return isDark ? "Switch to Light" : "Switch to Dark";
            }

            return "Toggle Theme";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}