// <copyright file="NavigationItemViewModel.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// ViewModel representing a navigation item in the sidebar.
/// </summary>
public partial class NavigationItemViewModel : ViewModelBase
{
    [ObservableProperty]
    private string name;

    [ObservableProperty]
    private string iconPath;

    [ObservableProperty]
    private bool isSelected;

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationItemViewModel"/> class.
    /// </summary>
    /// <param name="name">The display name of the navigation item.</param>
    /// <param name="iconPath">The SVG path data for the icon.</param>
    /// <param name="isSelected">Whether this item is initially selected.</param>
    public NavigationItemViewModel(string name, string iconPath, bool isSelected = false)
    {
        this.name = name;
        this.iconPath = iconPath;
        this.isSelected = isSelected;
    }
}