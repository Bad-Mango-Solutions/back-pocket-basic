// <copyright file="NavigationService.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Services;

using BadMango.Emulator.UI.ViewModels;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for managing navigation between views.
/// </summary>
public class NavigationService : INavigationService
{
    private readonly IServiceProvider? serviceProvider;
    private readonly ILogger<NavigationService>? logger;
    private ViewModelBase? currentView;

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationService"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving view models.</param>
    /// <param name="logger">Optional logger for navigation operations.</param>
    public NavigationService(IServiceProvider? serviceProvider = null, ILogger<NavigationService>? logger = null)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    /// <inheritdoc />
    public event EventHandler<ViewModelBase?>? CurrentViewChanged;

    /// <inheritdoc />
    public ViewModelBase? CurrentView => currentView;

    /// <inheritdoc />
    public void NavigateTo<TViewModel>()
        where TViewModel : ViewModelBase
    {
        if (serviceProvider is null)
        {
            logger?.LogWarning("Cannot navigate to {ViewModelType}: Service provider is null", typeof(TViewModel).Name);
            return;
        }

        var viewModel = serviceProvider.GetRequiredService<TViewModel>();
        NavigateTo(viewModel);
    }

    /// <inheritdoc />
    public void NavigateTo(ViewModelBase viewModel)
    {
        currentView = viewModel;
        logger?.LogInformation("Navigated to {ViewModelType}", viewModel.GetType().Name);
        CurrentViewChanged?.Invoke(this, currentView);
    }
}