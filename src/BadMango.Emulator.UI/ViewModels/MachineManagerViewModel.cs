// <copyright file="MachineManagerViewModel.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.ViewModels;

using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

/// <summary>
/// ViewModel for the Machine Manager view.
/// Manages machine profiles and active instances.
/// </summary>
public partial class MachineManagerViewModel : ViewModelBase
{
    /// <summary>
    /// Gets or sets the view title displayed in the header.
    /// </summary>
    [ObservableProperty]
    private string viewTitle = "Machine Manager";

    /// <summary>
    /// Gets or sets the currently selected machine profile.
    /// </summary>
    [ObservableProperty]
    private MachineProfileViewModel? selectedProfile;

    /// <summary>
    /// Gets or sets the currently selected machine instance.
    /// </summary>
    [ObservableProperty]
    private MachineInstanceViewModel? selectedInstance;

    /// <summary>
    /// Initializes a new instance of the <see cref="MachineManagerViewModel"/> class.
    /// </summary>
    public MachineManagerViewModel()
    {
        // Initialize with sample profiles for Phase 1 stub
        Profiles = new ObservableCollection<MachineProfileViewModel>
        {
            new()
            {
                Id = "default-iie",
                Name = "Apple IIe Enhanced",
                Personality = "Pocket2e",
                CpuType = "65C02 @ 1.023 MHz",
                RamSize = "128 KB",
            },
            new()
            {
                Id = "dev-machine",
                Name = "Development Machine",
                Personality = "Pocket2e",
                CpuType = "65C02 @ 1.023 MHz",
                RamSize = "128 KB",
            },
        };

        Instances = new ObservableCollection<MachineInstanceViewModel>();

        if (Profiles.Count > 0)
        {
            SelectedProfile = Profiles[0];
        }
    }

    /// <summary>
    /// Gets the collection of saved machine profiles.
    /// </summary>
    public ObservableCollection<MachineProfileViewModel> Profiles { get; }

    /// <summary>
    /// Gets the collection of active machine instances.
    /// </summary>
    public ObservableCollection<MachineInstanceViewModel> Instances { get; }

    /// <summary>
    /// Creates a new machine profile.
    /// </summary>
    [RelayCommand]
    private void CreateProfile()
    {
        var newProfile = new MachineProfileViewModel
        {
            Id = Guid.NewGuid().ToString(),
            Name = "New Profile",
            Personality = "Pocket2e",
            CpuType = "65C02 @ 1.023 MHz",
            RamSize = "128 KB",
        };
        Profiles.Add(newProfile);
        SelectedProfile = newProfile;
    }

    /// <summary>
    /// Deletes the selected machine profile.
    /// </summary>
    [RelayCommand]
    private void DeleteProfile()
    {
        if (SelectedProfile is not null)
        {
            Profiles.Remove(SelectedProfile);
            SelectedProfile = Profiles.Count > 0 ? Profiles[0] : null;
        }
    }

    /// <summary>
    /// Starts a new instance from the selected profile.
    /// </summary>
    [RelayCommand]
    private void StartInstance()
    {
        if (SelectedProfile is null)
        {
            return;
        }

        var instance = new MachineInstanceViewModel
        {
            ProfileName = SelectedProfile.Name,
            Status = "Running",
            CpuInfo = SelectedProfile.CpuType,
            RamInfo = SelectedProfile.RamSize,
        };
        Instances.Add(instance);
        SelectedInstance = instance;
    }

    /// <summary>
    /// Stops the selected machine instance.
    /// </summary>
    [RelayCommand]
    private void StopInstance()
    {
        if (SelectedInstance is not null)
        {
            Instances.Remove(SelectedInstance);
            SelectedInstance = Instances.Count > 0 ? Instances[0] : null;
        }
    }

    /// <summary>
    /// Pauses the selected machine instance.
    /// </summary>
    [RelayCommand]
    private void PauseInstance()
    {
        if (SelectedInstance is not null)
        {
            SelectedInstance.Status = SelectedInstance.Status == "Running" ? "Paused" : "Running";
        }
    }

    /// <summary>
    /// Resets the selected machine instance.
    /// </summary>
    [RelayCommand]
    private void ResetInstance()
    {
        if (SelectedInstance is not null)
        {
            SelectedInstance.Status = "Running";
        }
    }
}