// <copyright file="PocketWatchStatusExtension.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.StatusMonitor;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

using BadMango.Emulator.Devices.Interfaces;

/// <summary>
/// Status window extension for the PocketWatch clock device.
/// </summary>
/// <remarks>
/// <para>
/// This extension displays the current system time as reported by the
/// PocketWatch clock device. It serves as a proof of concept for the
/// status window extension system.
/// </para>
/// </remarks>
public sealed class PocketWatchStatusExtension : IStatusWindowExtension
{
    private readonly IClockDevice clockDevice;
    private TextBlock? timeDisplay;

    /// <summary>
    /// Initializes a new instance of the <see cref="PocketWatchStatusExtension"/> class.
    /// </summary>
    /// <param name="clockDevice">The clock device to monitor.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="clockDevice"/> is null.</exception>
    public PocketWatchStatusExtension(IClockDevice clockDevice)
    {
        ArgumentNullException.ThrowIfNull(clockDevice);
        this.clockDevice = clockDevice;
    }

    /// <inheritdoc/>
    public string Name => "PocketWatch";

    /// <inheritdoc/>
    public int Order => 100;

    /// <inheritdoc/>
    public Control CreateControl()
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(0, 5, 0, 0),
        };

        var header = new TextBlock
        {
            Text = "🕐 PocketWatch",
            FontSize = 12,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.CornflowerBlue,
            Margin = new Thickness(0, 0, 0, 5),
        };
        panel.Children.Add(header);

        var timeRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
        };

        var timeLabel = new TextBlock
        {
            Text = "System Time:",
            FontSize = 11,
            Foreground = Brushes.Gray,
            VerticalAlignment = VerticalAlignment.Center,
        };
        timeRow.Children.Add(timeLabel);

        timeDisplay = new TextBlock
        {
            Text = "00:00:00",
            FontSize = 11,
            FontFamily = new FontFamily("Cascadia Code, Consolas, Courier New, monospace"),
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
        };
        timeRow.Children.Add(timeDisplay);

        panel.Children.Add(timeRow);

        // Show time source
        var sourceRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
        };

        var sourceLabel = new TextBlock
        {
            Text = "Source:",
            FontSize = 10,
            Foreground = Brushes.Gray,
            VerticalAlignment = VerticalAlignment.Center,
        };
        sourceRow.Children.Add(sourceLabel);

        var sourceValue = new TextBlock
        {
            Text = clockDevice.UseHostTime ? "Host" : "Fixed",
            FontSize = 10,
            Foreground = Brushes.DarkGray,
            VerticalAlignment = VerticalAlignment.Center,
        };
        sourceRow.Children.Add(sourceValue);

        panel.Children.Add(sourceRow);

        return panel;
    }

    /// <inheritdoc/>
    public void UpdateDisplay()
    {
        if (timeDisplay == null)
        {
            return;
        }

        var currentTime = clockDevice.CurrentTime;
        timeDisplay.Text = currentTime.ToString("yyyy-MM-dd HH:mm:ss");
    }
}