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
    [ObservableProperty]
    private string id = string.Empty;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string personality = string.Empty;

    [ObservableProperty]
    private string cpuType = string.Empty;

    [ObservableProperty]
    private string ramSize = string.Empty;
}