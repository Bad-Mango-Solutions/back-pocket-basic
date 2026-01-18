// <copyright file="ScheduleMonitorWindow.axaml.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Views;

using System.Collections.ObjectModel;

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Schedule monitor window displaying scheduler events in real-time.
/// </summary>
/// <remarks>
/// <para>
/// This window provides real-time monitoring of the scheduler's event queue,
/// showing pending events and logging when events are scheduled and consumed.
/// </para>
/// <para>
/// The window uses a <see cref="DispatcherTimer"/> to poll the scheduler state
/// at approximately 60Hz for smooth UI updates.
/// </para>
/// </remarks>
public partial class ScheduleMonitorWindow : Window
{
    /// <summary>
    /// Maximum number of log entries to keep in the event log.
    /// </summary>
    private const int MaxLogEntries = 500;

    /// <summary>
    /// Refresh interval in milliseconds, targeting approximately 60Hz refresh rate.
    /// </summary>
    private const double RefreshIntervalMs = 16.67;

    private readonly DispatcherTimer updateTimer;
    private readonly ObservableCollection<PendingEventInfo> pendingEvents = [];
    private readonly ObservableCollection<string> eventLog = [];

    // UI Control references
    private readonly ComboBox deviceFilter;
    private readonly ComboBox kindFilter;
    private readonly CheckBox pauseUpdatesCheckbox;
    private readonly TextBlock pendingEventsHeader;
    private readonly DataGrid pendingEventsGrid;
    private readonly ItemsControl eventLogList;
    private readonly TextBlock currentCycleText;
    private readonly TextBlock pendingCountText;
    private readonly TextBlock totalScheduledText;
    private readonly TextBlock consumedCountText;

    private IMachine? machine;
    private ulong totalScheduled;
    private ulong totalConsumed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduleMonitorWindow"/> class.
    /// </summary>
    public ScheduleMonitorWindow()
    {
        InitializeComponent();

        // Get references to UI controls
        deviceFilter = this.FindControl<ComboBox>("DeviceFilter")!;
        kindFilter = this.FindControl<ComboBox>("KindFilter")!;
        pauseUpdatesCheckbox = this.FindControl<CheckBox>("PauseUpdatesCheckbox")!;
        pendingEventsHeader = this.FindControl<TextBlock>("PendingEventsHeader")!;
        pendingEventsGrid = this.FindControl<DataGrid>("PendingEventsGrid")!;
        eventLogList = this.FindControl<ItemsControl>("EventLogList")!;
        currentCycleText = this.FindControl<TextBlock>("CurrentCycleText")!;
        pendingCountText = this.FindControl<TextBlock>("PendingCountText")!;
        totalScheduledText = this.FindControl<TextBlock>("TotalScheduledText")!;
        consumedCountText = this.FindControl<TextBlock>("ConsumedCountText")!;

        // Bind collections to UI
        pendingEventsGrid.ItemsSource = pendingEvents;
        eventLogList.ItemsSource = eventLog;

        // Populate the Kind filter with ScheduledEventKind values
        PopulateKindFilter();

        // Set up update timer at ~60Hz
        updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(RefreshIntervalMs),
        };
        updateTimer.Tick += OnTimerTick;

        // Allow closing with Escape key
        KeyDown += OnKeyDown;
        Opened += OnWindowOpened;
        Closed += OnWindowClosed;
    }

    /// <summary>
    /// Sets the machine to monitor.
    /// </summary>
    /// <param name="machineToMonitor">The machine containing the scheduler to monitor.</param>
    public void SetMachine(IMachine machineToMonitor)
    {
        ArgumentNullException.ThrowIfNull(machineToMonitor);
        machine = machineToMonitor;

        // Populate device filter with devices from the machine
        PopulateDeviceFilter();
    }

    private static string FormatNumber(ulong value)
    {
        if (value >= 1_000_000_000)
        {
            return $"{value / 1_000_000_000.0:F2}G";
        }

        if (value >= 1_000_000)
        {
            return $"{value / 1_000_000.0:F2}M";
        }

        if (value >= 1_000)
        {
            return $"{value / 1_000.0:F1}K";
        }

        return value.ToString();
    }

    private void PopulateKindFilter()
    {
        // Add all ScheduledEventKind enum values
        foreach (var kind in Enum.GetValues<ScheduledEventKind>())
        {
            if (kind != ScheduledEventKind.None)
            {
                kindFilter.Items.Add(new ComboBoxItem { Content = kind.ToString() });
            }
        }
    }

    private void PopulateDeviceFilter()
    {
        // Clear existing items except "All"
        while (deviceFilter.Items.Count > 1)
        {
            deviceFilter.Items.RemoveAt(1);
        }

        if (machine is null)
        {
            return;
        }

        // Get devices from machine (if available through a component interface)
        var scheduledDevices = machine.GetComponents<IScheduledDevice>();
        foreach (var device in scheduledDevices)
        {
            deviceFilter.Items.Add(new ComboBoxItem { Content = device.Name });
        }
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        updateTimer.Start();
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        updateTimer.Stop();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (pauseUpdatesCheckbox.IsChecked == true)
        {
            return;
        }

        UpdateSchedulerState();
    }

    private void UpdateSchedulerState()
    {
        if (machine is null)
        {
            return;
        }

        var scheduler = machine.Scheduler;
        if (scheduler is null)
        {
            return;
        }

        // Update current cycle
        currentCycleText.Text = FormatNumber(scheduler.Now);

        // Update pending count
        var pendingCount = scheduler.PendingEventCount;
        pendingCountText.Text = pendingCount.ToString();
        pendingEventsHeader.Text = $"Pending Events ({pendingCount})";

        // Update statistics
        totalScheduledText.Text = FormatNumber(totalScheduled);
        consumedCountText.Text = FormatNumber(totalConsumed);

        // Note: The current IScheduler interface doesn't expose pending events details
        // for iteration. This would require an ISchedulerObserver interface to be added
        // to the Scheduler class to notify when events are scheduled and consumed.
        // For now, we display the pending count from the scheduler.
    }

    private void AddLogEntry(string entry)
    {
        eventLog.Add(entry);

        // Trim log if it exceeds maximum
        while (eventLog.Count > MaxLogEntries)
        {
            eventLog.RemoveAt(0);
        }
    }

    private void OnClearLogClick(object? sender, RoutedEventArgs e)
    {
        eventLog.Clear();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        Close();
        e.Handled = true;
    }

    /// <summary>
    /// Represents information about a pending scheduled event for display.
    /// </summary>
    /// <param name="Handle">The event handle as a hex string.</param>
    /// <param name="DeviceName">The name of the device that scheduled the event.</param>
    /// <param name="Kind">The kind of scheduled event.</param>
    /// <param name="DueCycle">The cycle at which the event is due.</param>
    /// <param name="Priority">The event priority.</param>
    public record PendingEventInfo(
        string Handle,
        string DeviceName,
        string Kind,
        string DueCycle,
        int Priority);
}