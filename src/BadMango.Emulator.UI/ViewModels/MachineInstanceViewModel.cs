// <copyright file="MachineInstanceViewModel.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// ViewModel representing a running machine instance.
/// </summary>
public partial class MachineInstanceViewModel : ViewModelBase
{
    /// <summary>
    /// Gets or sets the name of the profile this instance was created from.
    /// </summary>
    [ObservableProperty]
    private string profileName = string.Empty;

    /// <summary>
    /// Gets or sets the current status of the instance (Running, Paused, etc.).
    /// </summary>
    [ObservableProperty]
    private string status = string.Empty;

    /// <summary>
    /// Gets or sets the CPU information for this instance.
    /// </summary>
    [ObservableProperty]
    private string cpuInfo = string.Empty;

    /// <summary>
    /// Gets or sets the RAM information for this instance.
    /// </summary>
    [ObservableProperty]
    private string ramInfo = string.Empty;
}