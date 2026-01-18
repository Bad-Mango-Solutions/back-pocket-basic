// <copyright file="TrapMonitorWindow.axaml.cs" company="Bad Mango Solutions">
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
/// Trap monitor window displaying registered traps and invocations in real-time.
/// </summary>
/// <remarks>
/// <para>
/// This window provides real-time monitoring of the trap registry,
/// showing all registered traps and logging when traps are invoked.
/// </para>
/// <para>
/// The window uses a <see cref="DispatcherTimer"/> to poll the trap registry state
/// at approximately 60Hz for smooth UI updates.
/// </para>
/// </remarks>
public partial class TrapMonitorWindow : Window
{
    /// <summary>
    /// Maximum number of log entries to keep in the invocation log.
    /// </summary>
    private const int MaxLogEntries = 500;

    /// <summary>
    /// Refresh interval in milliseconds, targeting approximately 60Hz refresh rate.
    /// </summary>
    private const double RefreshIntervalMs = 16.67;

    private readonly DispatcherTimer updateTimer;
    private readonly ObservableCollection<TrapDisplayInfo> traps = [];
    private readonly ObservableCollection<string> invocationLog = [];

    // UI Control references
    private readonly ComboBox categoryFilter;
    private readonly ComboBox operationFilter;
    private readonly ComboBox contextFilter;
    private readonly CheckBox showEnabledCheckbox;
    private readonly CheckBox showDisabledCheckbox;
    private readonly CheckBox pauseUpdatesCheckbox;
    private readonly TextBox searchTextBox;
    private readonly TextBlock registeredTrapsHeader;
    private readonly DataGrid trapsGrid;
    private readonly ItemsControl invocationLogList;
    private readonly TextBlock totalTrapsText;
    private readonly TextBlock enabledCountText;
    private readonly TextBlock disabledCountText;
    private readonly TextBlock invocationCountText;

    private IMachine? machine;
    private ITrapRegistry? trapRegistry;
    private ulong invocationCount;
    private string lastSearchText = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrapMonitorWindow"/> class.
    /// </summary>
    public TrapMonitorWindow()
    {
        InitializeComponent();

        // Get references to UI controls
        categoryFilter = this.FindControl<ComboBox>("CategoryFilter")!;
        operationFilter = this.FindControl<ComboBox>("OperationFilter")!;
        contextFilter = this.FindControl<ComboBox>("ContextFilter")!;
        showEnabledCheckbox = this.FindControl<CheckBox>("ShowEnabledCheckbox")!;
        showDisabledCheckbox = this.FindControl<CheckBox>("ShowDisabledCheckbox")!;
        pauseUpdatesCheckbox = this.FindControl<CheckBox>("PauseUpdatesCheckbox")!;
        searchTextBox = this.FindControl<TextBox>("SearchTextBox")!;
        registeredTrapsHeader = this.FindControl<TextBlock>("RegisteredTrapsHeader")!;
        trapsGrid = this.FindControl<DataGrid>("TrapsGrid")!;
        invocationLogList = this.FindControl<ItemsControl>("InvocationLogList")!;
        totalTrapsText = this.FindControl<TextBlock>("TotalTrapsText")!;
        enabledCountText = this.FindControl<TextBlock>("EnabledCountText")!;
        disabledCountText = this.FindControl<TextBlock>("DisabledCountText")!;
        invocationCountText = this.FindControl<TextBlock>("InvocationCountText")!;

        // Bind collections to UI
        trapsGrid.ItemsSource = traps;
        invocationLogList.ItemsSource = invocationLog;

        // Populate filters
        PopulateCategoryFilter();
        PopulateOperationFilter();
        PopulateContextFilter();

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

        // Filter change handlers
        categoryFilter.SelectionChanged += OnFilterChanged;
        operationFilter.SelectionChanged += OnFilterChanged;
        contextFilter.SelectionChanged += OnFilterChanged;
        showEnabledCheckbox.IsCheckedChanged += OnFilterChanged;
        showDisabledCheckbox.IsCheckedChanged += OnFilterChanged;
        searchTextBox.TextChanged += OnSearchTextChanged;
    }

    /// <summary>
    /// Sets the machine to monitor.
    /// </summary>
    /// <param name="machineToMonitor">The machine containing the trap registry to monitor.</param>
    public void SetMachine(IMachine machineToMonitor)
    {
        ArgumentNullException.ThrowIfNull(machineToMonitor);
        machine = machineToMonitor;

        // Try to get the trap registry from the machine components
        trapRegistry = machine.GetComponent<ITrapRegistry>();

        // Force an initial update of the traps list
        UpdateTrapsList();
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

    private void PopulateCategoryFilter()
    {
        // Add all TrapCategory enum values
        foreach (var category in Enum.GetValues<TrapCategory>())
        {
            categoryFilter.Items.Add(new ComboBoxItem { Content = category.ToString() });
        }
    }

    private void PopulateOperationFilter()
    {
        // Add all TrapOperation enum values
        foreach (var operation in Enum.GetValues<TrapOperation>())
        {
            operationFilter.Items.Add(new ComboBoxItem { Content = operation.ToString() });
        }
    }

    private void PopulateContextFilter()
    {
        // Add common memory contexts
        contextFilter.Items.Add(new ComboBoxItem { Content = MemoryContexts.Rom.Id });
        contextFilter.Items.Add(new ComboBoxItem { Content = MemoryContexts.LanguageCardRam.Id });
        contextFilter.Items.Add(new ComboBoxItem { Content = MemoryContexts.MainRam.Id });
        contextFilter.Items.Add(new ComboBoxItem { Content = MemoryContexts.AuxiliaryRam.Id });
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        updateTimer.Start();
        UpdateTrapsList();
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

        UpdateStatistics();
    }

    private void OnFilterChanged(object? sender, EventArgs e)
    {
        UpdateTrapsList();
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        var searchText = searchTextBox.Text ?? string.Empty;
        if (searchText != lastSearchText)
        {
            lastSearchText = searchText;
            UpdateTrapsList();
        }
    }

    private void UpdateTrapsList()
    {
        if (trapRegistry is null)
        {
            return;
        }

        var allTraps = trapRegistry.GetAllTraps().ToList();

        // Apply filters
        var filteredTraps = allTraps.AsEnumerable();

        // Category filter
        if (categoryFilter.SelectedIndex > 0 && categoryFilter.SelectedItem is ComboBoxItem categoryItem)
        {
            var categoryText = categoryItem.Content?.ToString();
            if (Enum.TryParse<TrapCategory>(categoryText, out var category))
            {
                filteredTraps = filteredTraps.Where(t => t.Category == category);
            }
        }

        // Operation filter
        if (operationFilter.SelectedIndex > 0 && operationFilter.SelectedItem is ComboBoxItem operationItem)
        {
            var operationText = operationItem.Content?.ToString();
            if (Enum.TryParse<TrapOperation>(operationText, out var operation))
            {
                filteredTraps = filteredTraps.Where(t => t.Operation == operation);
            }
        }

        // Context filter
        if (contextFilter.SelectedIndex > 0 && contextFilter.SelectedItem is ComboBoxItem contextItem)
        {
            var contextText = contextItem.Content?.ToString();
            if (!string.IsNullOrEmpty(contextText))
            {
                filteredTraps = filteredTraps.Where(t => t.MemoryContext.Id == contextText);
            }
        }

        // Enabled/Disabled filter
        var showEnabled = showEnabledCheckbox.IsChecked == true;
        var showDisabled = showDisabledCheckbox.IsChecked == true;
        if (!showEnabled || !showDisabled)
        {
            filteredTraps = filteredTraps.Where(t =>
                (showEnabled && t.IsEnabled) || (showDisabled && !t.IsEnabled));
        }

        // Search filter
        var searchText = searchTextBox.Text?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(searchText))
        {
            filteredTraps = filteredTraps.Where(t =>
                t.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                $"${t.Address:X4}".Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        // Update the collection
        traps.Clear();
        foreach (var trap in filteredTraps)
        {
            traps.Add(new TrapDisplayInfo(
                $"${trap.Address:X4}",
                trap.Name,
                trap.Category.ToString(),
                trap.Operation.ToString(),
                trap.MemoryContext.Id,
                trap.SlotNumber?.ToString() ?? "-",
                trap.IsEnabled));
        }

        // Update header
        registeredTrapsHeader.Text = $"Registered Traps ({traps.Count})";
    }

    private void UpdateStatistics()
    {
        if (trapRegistry is null)
        {
            return;
        }

        var allTraps = trapRegistry.GetAllTraps().ToList();
        var totalTraps = allTraps.Count;
        var enabledTraps = allTraps.Count(t => t.IsEnabled);
        var disabledTraps = totalTraps - enabledTraps;

        totalTrapsText.Text = totalTraps.ToString();
        enabledCountText.Text = enabledTraps.ToString();
        disabledCountText.Text = disabledTraps.ToString();
        invocationCountText.Text = FormatNumber(invocationCount);
    }

    private void OnClearLogClick(object? sender, RoutedEventArgs e)
    {
        invocationLog.Clear();
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
    /// Represents information about a registered trap for display.
    /// </summary>
    /// <param name="Address">The trap address as a hex string.</param>
    /// <param name="Name">The trap name.</param>
    /// <param name="Category">The trap category.</param>
    /// <param name="Operation">The trap operation type.</param>
    /// <param name="Context">The memory context.</param>
    /// <param name="Slot">The slot number or "-" if not slot-dependent.</param>
    /// <param name="IsEnabled">Whether the trap is enabled.</param>
    public record TrapDisplayInfo(
        string Address,
        string Name,
        string Category,
        string Operation,
        string Context,
        string Slot,
        bool IsEnabled);
}