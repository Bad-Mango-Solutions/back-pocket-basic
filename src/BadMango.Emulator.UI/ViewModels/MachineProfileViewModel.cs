// <copyright file="MachineProfileViewModel.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// ViewModel representing a machine profile configuration.
/// </summary>
public partial class MachineProfileViewModel : ViewModelBase
{
    /// <summary>
    /// Gets or sets the unique identifier of the profile.
    /// </summary>
    [ObservableProperty]
    private string id = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the profile.
    /// </summary>
    [ObservableProperty]
    private string name = string.Empty;

    /// <summary>
    /// Gets or sets the machine personality type.
    /// </summary>
    [ObservableProperty]
    private string personality = string.Empty;

    /// <summary>
    /// Gets or sets the CPU type and speed.
    /// </summary>
    [ObservableProperty]
    private string cpuType = string.Empty;

    /// <summary>
    /// Gets or sets the RAM size configuration.
    /// </summary>
    [ObservableProperty]
    private string ramSize = string.Empty;
}