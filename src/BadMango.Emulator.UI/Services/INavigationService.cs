// <copyright file="INavigationService.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Services;

using BadMango.Emulator.UI.ViewModels;

/// <summary>
/// Service interface for managing navigation between views.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Event raised when the current view changes.
    /// </summary>
    event EventHandler<ViewModelBase?>? CurrentViewChanged;

    /// <summary>
    /// Gets the currently displayed view.
    /// </summary>
    ViewModelBase? CurrentView { get; }

    /// <summary>
    /// Navigates to the specified view.
    /// </summary>
    /// <typeparam name="TViewModel">The type of view model to navigate to.</typeparam>
    void NavigateTo<TViewModel>()
        where TViewModel : ViewModelBase;

    /// <summary>
    /// Navigates to a view with the specified view model instance.
    /// </summary>
    /// <param name="viewModel">The view model instance to display.</param>
    void NavigateTo(ViewModelBase viewModel);
}