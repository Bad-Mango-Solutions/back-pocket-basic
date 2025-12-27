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
    [ObservableProperty]
    private string profileName = string.Empty;

    [ObservableProperty]
    private string status = string.Empty;

    [ObservableProperty]
    private string cpuInfo = string.Empty;

    [ObservableProperty]
    private string ramInfo = string.Empty;
}