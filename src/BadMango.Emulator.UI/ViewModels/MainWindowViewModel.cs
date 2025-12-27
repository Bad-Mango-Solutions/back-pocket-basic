// <copyright file="MainWindowViewModel.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.ViewModels;

using System.Collections.ObjectModel;

using BadMango.Emulator.UI.Services;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

/// <summary>
/// ViewModel for the main application window.
/// Manages navigation, theme switching, and the overall application state.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IThemeService themeService;
    private readonly INavigationService navigationService;

    [ObservableProperty]
    private string title = "BackPocket Emulator";

    [ObservableProperty]
    private ViewModelBase? currentView;

    [ObservableProperty]
    private bool isDarkTheme = true;

    [ObservableProperty]
    private string selectedNavigationItem = "Machine Manager";

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="themeService">The theme service for managing application themes.</param>
    /// <param name="navigationService">The navigation service for view navigation.</param>
    public MainWindowViewModel(IThemeService themeService, INavigationService navigationService)
    {
        this.themeService = themeService;
        this.navigationService = navigationService;

        // Initialize navigation items
        NavigationItems = new ObservableCollection<NavigationItemViewModel>
        {
            new("Machine Manager", "M324 -200v-80h200v-240H324v-80h280v400H324Zm-84 80q-83 0-141.5-58.5T40-320v-320q0-83 58.5-141.5T240-840h480q83 0 141.5 58.5T920-640v320q0 83-58.5 141.5T720-120H240Zm0-80h480q50 0 85-35t35-85v-320q0-50-35-85t-85-35H240q-50 0-85 35t-35 85v320q0 50 35 85t85 35Zm-40-120v-80h200v-240H200v-80h280v400H200Z", true),
            new("Storage", "M200-120q-33 0-56.5-23.5T120-200v-560q0-33 23.5-56.5T200-840h560q33 0 56.5 23.5T840-760v560q0 33-23.5 56.5T760-120H200Zm0-80h560v-560H200v560Zm280-80q83 0 141.5-58.5T680-480q0-83-58.5-141.5T480-680q-83 0-141.5 58.5T280-480q0 83 58.5 141.5T480-280Zm0-160q-25 0-42.5 17.5T420-380q0 25 17.5 42.5T480-320q25 0 42.5-17.5T540-380q0-25-17.5-42.5T480-440Z"),
            new("Display", "M480-280q17 0 28.5-11.5T520-320v-160q0-17-11.5-28.5T480-520H320q-17 0-28.5 11.5T280-480v160q0 17 11.5 28.5T320-280h160Zm-120-80v-80h80v80h-80Zm-160 0v-80h80v80h-80Zm480 0v-80h80v80h-80ZM200-120q-33 0-56.5-23.5T120-200v-560q0-33 23.5-56.5T200-840h560q33 0 56.5 23.5T840-760v560q0 33-23.5 56.5T760-120H200Zm0-80h560v-560H200v560Z"),
            new("Debug", "M480-120q-83 0-156-31.5T197-249q-54-66-85.5-152T80-580h80q0 66 22 127t62 111l58-57q-38-45-60-101t-22-116v-28l162-162 57 57-134 133q0 60 20.5 113t57.5 93l137-137-57-57 56-56 170 170q12 12 12 28.5T708-380q-66 66-152 103t-176 37ZM376-576l-57-57q45-38 101-60t116-22h28l-80 80v-28q0-17 3.5-33.5T497-728l-64 64-57-57 56-56-170-170q-12-12-28.5-12T205-947q66 66 103 152t37 176h-28q60 0 113 20.5t93 57.5l-147 147Z"),
            new("Editor", "M200-200h57l391-391-57-57-391 391v57Zm-40 80q-17 0-28.5-11.5T120-160v-97q0-16 6-30.5t17-25.5l505-504q12-11 26.5-17t30.5-6q16 0 31 6t26 18l55 56q12 11 17.5 26t5.5 30q0 16-5.5 30.5T817-647L313-143q-11 11-25.5 17t-30.5 6h-97Zm600-584-56-56 56 56Zm-141 85-28-29 57 57-29-28Z"),
        };

        // Set initial view
        CurrentView = new MachineManagerViewModel();
    }

    /// <summary>
    /// Gets the collection of navigation items displayed in the navigation panel.
    /// </summary>
    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; }

    /// <summary>
    /// Toggles between dark and light themes.
    /// </summary>
    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        themeService.SetTheme(IsDarkTheme);
    }

    /// <summary>
    /// Navigates to the specified view.
    /// </summary>
    /// <param name="viewName">The name of the view to navigate to.</param>
    [RelayCommand]
    private void Navigate(string viewName)
    {
        SelectedNavigationItem = viewName;
        CurrentView = viewName switch
        {
            "Machine Manager" => new MachineManagerViewModel(),
            "Storage" => new PlaceholderViewModel("Storage Manager", "Disk and ROM image management coming soon."),
            "Display" => new PlaceholderViewModel("Display", "Video display emulation coming soon."),
            "Debug" => new PlaceholderViewModel("Debug Console", "Debug console integration coming soon."),
            "Editor" => new PlaceholderViewModel("Assembly Editor", "Assembly language editor coming soon."),
            _ => CurrentView,
        };

        // Update selected state
        foreach (var item in NavigationItems)
        {
            item.IsSelected = item.Name == viewName;
        }
    }
}