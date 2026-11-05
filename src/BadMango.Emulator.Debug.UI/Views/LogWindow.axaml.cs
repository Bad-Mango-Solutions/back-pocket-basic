// <copyright file="LogWindow.axaml.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Views;

using System.Collections.ObjectModel;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

using BadMango.Emulator.Debug.Infrastructure;

/// <summary>
/// Displays warning, error, and fatal log messages in a dedicated window.
/// </summary>
public partial class LogWindow : Window
{
    private const int MaxEntries = 1000;

    private readonly IDebugLogEventStream? logEventStream;
    private readonly DispatcherTimer drainTimer;
    private readonly ObservableCollection<string> entries = [];
    private readonly ItemsControl logEntriesList;
    private readonly ScrollViewer logScrollViewer;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogWindow"/> class.
    /// </summary>
    public LogWindow()
        : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LogWindow"/> class.
    /// </summary>
    /// <param name="logEventStream">The stream used to receive log entries.</param>
    public LogWindow(IDebugLogEventStream? logEventStream)
    {
        this.logEventStream = logEventStream;

        this.InitializeComponent();

        this.logEntriesList = this.FindControl<ItemsControl>("LogEntriesList")!;
        this.logScrollViewer = this.FindControl<ScrollViewer>("LogScrollViewer")!;

        this.logEntriesList.ItemsSource = this.entries;

        this.drainTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100),
        };

        this.drainTimer.Tick += this.OnDrainTimerTick;
        this.Opened += this.OnOpened;
        this.Closed += this.OnClosed;
        this.KeyDown += this.OnKeyDown;
    }

    private static string FormatEntry(DebugLogEntry entry)
    {
        return $"[{entry.Timestamp:HH:mm:ss.fff} {entry.Level}] {entry.Message}";
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        this.drainTimer.Start();
        this.DrainLogEntries();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        this.drainTimer.Stop();
    }

    private void OnDrainTimerTick(object? sender, EventArgs e)
    {
        this.DrainLogEntries();
    }

    private void DrainLogEntries()
    {
        if (this.logEventStream is null)
        {
            return;
        }

        var addedAny = false;
        while (this.logEventStream.TryDequeue(out var entry))
        {
            if (entry is null)
            {
                continue;
            }

            this.entries.Add(FormatEntry(entry));
            addedAny = true;
        }

        if (!addedAny)
        {
            return;
        }

        while (this.entries.Count > MaxEntries)
        {
            this.entries.RemoveAt(0);
        }

        this.logScrollViewer.ScrollToEnd();
    }

    private void OnClearClick(object? sender, RoutedEventArgs e)
    {
        this.entries.Clear();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        this.Close();
        e.Handled = true;
    }
}