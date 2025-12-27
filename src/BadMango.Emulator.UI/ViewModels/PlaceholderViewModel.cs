// <copyright file="PlaceholderViewModel.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// ViewModel for placeholder views that are not yet implemented.
/// </summary>
public partial class PlaceholderViewModel : ViewModelBase
{
    [ObservableProperty]
    private string title;

    [ObservableProperty]
    private string message;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaceholderViewModel"/> class.
    /// </summary>
    /// <param name="title">The title to display.</param>
    /// <param name="message">The placeholder message to display.</param>
    public PlaceholderViewModel(string title, string message)
    {
        this.title = title;
        this.message = message;
    }
}