// <copyright file="GlyphEditorViewModel.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Editor.ViewModels;

using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Devices;

using Models;

using Services;

/// <summary>
/// Main view model for the glyph editor window.
/// </summary>
public sealed partial class GlyphEditorViewModel : ViewModelBase
{
    private readonly IGlyphFileService fileService;
    private readonly IEmulatorConnection emulatorConnection;
    private readonly Dictionary<(byte CharCode, bool IsAlt), GlyphEditHistory> historyMap = [];
    private readonly HashSet<byte> selectedCharCodes = [];
    private readonly DispatcherTimer flashTimer;

    private List<CharacterGlyph>? clipboard;

    [ObservableProperty]
    private GlyphFile? currentFile;

    [ObservableProperty]
    private bool useAlternateSet;

    [ObservableProperty]
    private byte selectedCharCode;

    [ObservableProperty]
    private CharacterGlyph? selectedGlyph;

    [ObservableProperty]
    private bool showGrid = true;

    [ObservableProperty]
    private bool flashPreviewEnabled;

    [ObservableProperty]
    private bool isFlashOn;

    [ObservableProperty]
    private int gridZoomLevel = 2;

    [ObservableProperty]
    private string windowTitle = "Character Glyph Editor";

    [ObservableProperty]
    private string statusText = "Ready";

    [ObservableProperty]
    private bool isEmulatorConnected;

    [ObservableProperty]
    private GlyphLoadTarget hotLoadTarget = GlyphLoadTarget.Rom;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlyphEditorViewModel"/> class.
    /// </summary>
    /// <param name="fileService">The file service for dialogs and file operations.</param>
    /// <param name="emulatorConnection">The emulator connection service.</param>
    public GlyphEditorViewModel(
        IGlyphFileService fileService,
        IEmulatorConnection emulatorConnection)
    {
        this.fileService = fileService;
        this.emulatorConnection = emulatorConnection;

        this.emulatorConnection.ConnectionStateChanged += (_, connected) =>
        {
            IsEmulatorConnected = connected;
        };

        // Set up flash timer (~3.75 Hz, Apple II flash rate)
        flashTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(267),
        };
        flashTimer.Tick += (_, _) =>
        {
            if (FlashPreviewEnabled)
            {
                IsFlashOn = !IsFlashOn;
            }
        };
    }

    /// <summary>
    /// Gets the display mode for the selected character code.
    /// </summary>
    public string DisplayMode => SelectedCharCode switch
    {
        < 0x40 => "Inverse",
        < 0x80 => UseAlternateSet ? "MouseText" : "Flashing",
        _ => "Normal",
    };

    /// <summary>
    /// Gets the printable character representation.
    /// </summary>
    public string CharacterDisplay
    {
        get
        {
            char c = (char)(SelectedCharCode & 0x7F);
            if (c < 0x20 || c == 0x7F)
            {
                return $"${SelectedCharCode:X2}";
            }

            return $"${SelectedCharCode:X2} '{c}'";
        }
    }

    /// <summary>
    /// Gets a value indicating whether there are items in the clipboard.
    /// </summary>
    public bool HasClipboard => clipboard != null && clipboard.Count > 0;

    /// <summary>
    /// Gets the number of selected characters.
    /// </summary>
    public int SelectionCount => selectedCharCodes.Count > 0 ? selectedCharCodes.Count : 1;

    /// <summary>
    /// Toggles a pixel at the specified coordinates.
    /// </summary>
    /// <param name="x">The X coordinate (0-6).</param>
    /// <param name="y">The Y coordinate (0-7).</param>
    public void TogglePixel(int x, int y)
    {
        if (SelectedGlyph == null || CurrentFile == null)
        {
            return;
        }

        if (x < 0 || x > 6 || y < 0 || y > 7)
        {
            return;
        }

        RecordCurrentState();
        SelectedGlyph[x, y] = !SelectedGlyph[x, y];
        CurrentFile.MarkModified();
        OnPropertyChanged(nameof(SelectedGlyph));
        UpdateWindowTitle();
    }

    /// <summary>
    /// Sets a pixel at the specified coordinates.
    /// </summary>
    /// <param name="x">The X coordinate (0-6).</param>
    /// <param name="y">The Y coordinate (0-7).</param>
    /// <param name="value">True to set the pixel, false to clear it.</param>
    public void SetPixel(int x, int y, bool value)
    {
        if (SelectedGlyph == null || CurrentFile == null)
        {
            return;
        }

        if (x < 0 || x > 6 || y < 0 || y > 7)
        {
            return;
        }

        if (SelectedGlyph[x, y] != value)
        {
            RecordCurrentState();
            SelectedGlyph[x, y] = value;
            CurrentFile.MarkModified();
            OnPropertyChanged(nameof(SelectedGlyph));
            UpdateWindowTitle();
        }
    }

    /// <summary>
    /// Selects a character by its code.
    /// </summary>
    /// <param name="charCode">The character code to select.</param>
    public void SelectCharacter(byte charCode)
    {
        SelectedCharCode = charCode;
        selectedCharCodes.Clear();
        selectedCharCodes.Add(charCode);
        OnPropertyChanged(nameof(SelectionCount));
    }

    /// <summary>
    /// Adds a character to the current selection.
    /// </summary>
    /// <param name="charCode">The character code to add.</param>
    public void AddToSelection(byte charCode)
    {
        selectedCharCodes.Add(charCode);
        OnPropertyChanged(nameof(SelectionCount));
    }

    /// <summary>
    /// Toggles a character's selection state.
    /// </summary>
    /// <param name="charCode">The character code to toggle.</param>
    public void ToggleSelection(byte charCode)
    {
        if (selectedCharCodes.Contains(charCode))
        {
            selectedCharCodes.Remove(charCode);
        }
        else
        {
            selectedCharCodes.Add(charCode);
        }

        OnPropertyChanged(nameof(SelectionCount));
    }

    /// <summary>
    /// Clears the character selection.
    /// </summary>
    public void ClearSelection()
    {
        selectedCharCodes.Clear();
        selectedCharCodes.Add(SelectedCharCode);
        OnPropertyChanged(nameof(SelectionCount));
    }

    /// <summary>
    /// Creates a new empty glyph file.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [RelayCommand]
    private async Task NewFileAsync()
    {
        if (!await ConfirmDiscardChangesAsync())
        {
            return;
        }

        CurrentFile = GlyphFile.CreateNew();
        historyMap.Clear();
        SelectedCharCode = 0;
        UpdateSelectedGlyph();
        UpdateWindowTitle();
        StatusText = "New file created";
    }

    /// <summary>
    /// Opens an existing glyph file.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [RelayCommand]
    private async Task OpenFileAsync()
    {
        if (!await ConfirmDiscardChangesAsync())
        {
            return;
        }

        var path = await fileService.ShowOpenDialogAsync();
        if (path == null)
        {
            return;
        }

        try
        {
            CurrentFile = GlyphFile.Load(path);
            historyMap.Clear();
            SelectedCharCode = 0;
            UpdateSelectedGlyph();
            UpdateWindowTitle();
            StatusText = $"Opened: {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Failed to open file: {ex.Message}");
        }
    }

    /// <summary>
    /// Saves the current glyph file.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [RelayCommand]
    private async Task SaveFileAsync()
    {
        if (CurrentFile == null)
        {
            return;
        }

        if (CurrentFile.FilePath == null)
        {
            await SaveFileAsAsync();
            return;
        }

        try
        {
            CurrentFile.Save();
            UpdateWindowTitle();
            StatusText = $"Saved: {Path.GetFileName(CurrentFile.FilePath)}";
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Failed to save file: {ex.Message}");
        }
    }

    /// <summary>
    /// Saves the current glyph file with a new name.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [RelayCommand]
    private async Task SaveFileAsAsync()
    {
        if (CurrentFile == null)
        {
            return;
        }

        var path = await fileService.ShowSaveDialogAsync();
        if (path == null)
        {
            return;
        }

        try
        {
            CurrentFile.Save(path);
            UpdateWindowTitle();
            StatusText = $"Saved: {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Failed to save file: {ex.Message}");
        }
    }

    /// <summary>
    /// Imports character data from a ROM file.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [RelayCommand]
    private async Task ImportFromRomAsync()
    {
        var path = await fileService.ShowOpenRomDialogAsync();
        if (path == null)
        {
            return;
        }

        try
        {
            var romData = await File.ReadAllBytesAsync(path);

            // Apple II character ROMs are typically 2KB or 4KB
            if (romData.Length == 2048)
            {
                // Single character set - load into primary
                CurrentFile ??= GlyphFile.CreateNew();

                for (int i = 0; i < 256; i++)
                {
                    CurrentFile.GetGlyph((byte)i, false).CopyFrom(romData.AsSpan(i * 8, 8));
                }

                CurrentFile.MarkModified();
            }
            else if (romData.Length == 4096)
            {
                // Full character ROM - load both sets
                CurrentFile = GlyphFile.LoadFromBytes(romData);
                CurrentFile.MarkModified();
            }
            else
            {
                await ShowErrorAsync($"Unexpected ROM size: {romData.Length} bytes. Expected 2048 or 4096.");
                return;
            }

            historyMap.Clear();
            UpdateSelectedGlyph();
            UpdateWindowTitle();
            StatusText = $"Imported from: {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Failed to import ROM: {ex.Message}");
        }
    }

    /// <summary>
    /// Exports a preview image of the character set.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [RelayCommand]
    private async Task ExportPreviewImageAsync()
    {
        if (CurrentFile == null)
        {
            return;
        }

        var path = await fileService.ShowSaveImageDialogAsync();
        if (path == null)
        {
            return;
        }

        try
        {
            await fileService.ExportCharacterSetPreview(CurrentFile, UseAlternateSet, path);
            StatusText = $"Exported preview: {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Failed to export preview: {ex.Message}");
        }
    }

    /// <summary>
    /// Undoes the last modification to the selected glyph.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        if (SelectedGlyph == null || CurrentFile == null)
        {
            return;
        }

        var history = GetCurrentHistory();
        if (history.Undo(SelectedGlyph))
        {
            CurrentFile.MarkModified();
            OnPropertyChanged(nameof(SelectedGlyph));
            UpdateWindowTitle();
        }
    }

    private bool CanUndo() => GetCurrentHistory().CanUndo;

    /// <summary>
    /// Redoes the last undone modification to the selected glyph.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        if (SelectedGlyph == null || CurrentFile == null)
        {
            return;
        }

        var history = GetCurrentHistory();
        if (history.Redo(SelectedGlyph))
        {
            CurrentFile.MarkModified();
            OnPropertyChanged(nameof(SelectedGlyph));
            UpdateWindowTitle();
        }
    }

    private bool CanRedo() => GetCurrentHistory().CanRedo;

    /// <summary>
    /// Copies the selected glyph(s) to the clipboard.
    /// </summary>
    [RelayCommand]
    private void CopySelectedGlyphs()
    {
        if (CurrentFile == null)
        {
            return;
        }

        var codesToCopy = selectedCharCodes.Count > 0
            ? selectedCharCodes.OrderBy(c => c).ToList()
            : [SelectedCharCode];

        clipboard = codesToCopy
            .Select(c => CurrentFile.GetGlyph(c, UseAlternateSet).Clone())
            .ToList();

        OnPropertyChanged(nameof(HasClipboard));
        StatusText = $"Copied {clipboard.Count} character(s)";
    }

    /// <summary>
    /// Pastes glyphs from the clipboard.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanPaste))]
    private void PasteGlyphs()
    {
        if (CurrentFile == null || clipboard == null || clipboard.Count == 0)
        {
            return;
        }

        int targetCode = SelectedCharCode;
        int pasteCount = 0;

        foreach (var glyph in clipboard)
        {
            if (targetCode > 255)
            {
                break;
            }

            var targetGlyph = CurrentFile.GetGlyph((byte)targetCode, UseAlternateSet);
            var history = GetHistoryFor((byte)targetCode, UseAlternateSet);
            history.RecordState(targetGlyph);

            Array.Copy(glyph.Scanlines, targetGlyph.Scanlines, 8);
            targetCode++;
            pasteCount++;
        }

        CurrentFile.MarkModified();
        UpdateSelectedGlyph();
        UpdateWindowTitle();
        StatusText = $"Pasted {pasteCount} character(s)";
    }

    private bool CanPaste() => clipboard != null && clipboard.Count > 0;

    /// <summary>
    /// Inverts all pixels in the selected glyph.
    /// </summary>
    [RelayCommand]
    private void InvertGlyph()
    {
        if (SelectedGlyph == null || CurrentFile == null)
        {
            return;
        }

        RecordCurrentState();
        SelectedGlyph.Invert();
        CurrentFile.MarkModified();
        OnPropertyChanged(nameof(SelectedGlyph));
        UpdateWindowTitle();
    }

    /// <summary>
    /// Clears all pixels in the selected glyph.
    /// </summary>
    [RelayCommand]
    private void ClearGlyph()
    {
        if (SelectedGlyph == null || CurrentFile == null)
        {
            return;
        }

        RecordCurrentState();
        SelectedGlyph.Clear();
        CurrentFile.MarkModified();
        OnPropertyChanged(nameof(SelectedGlyph));
        UpdateWindowTitle();
    }

    /// <summary>
    /// Fills all pixels in the selected glyph.
    /// </summary>
    [RelayCommand]
    private void FillGlyph()
    {
        if (SelectedGlyph == null || CurrentFile == null)
        {
            return;
        }

        RecordCurrentState();
        SelectedGlyph.Fill();
        CurrentFile.MarkModified();
        OnPropertyChanged(nameof(SelectedGlyph));
        UpdateWindowTitle();
    }

    /// <summary>
    /// Flips the selected glyph horizontally.
    /// </summary>
    [RelayCommand]
    private void FlipHorizontal()
    {
        if (SelectedGlyph == null || CurrentFile == null)
        {
            return;
        }

        RecordCurrentState();

        for (int y = 0; y < 8; y++)
        {
            byte original = SelectedGlyph.Scanlines[y];
            byte flipped = 0;

            for (int x = 0; x < 7; x++)
            {
                if ((original & (1 << x)) != 0)
                {
                    flipped |= (byte)(1 << (6 - x));
                }
            }

            SelectedGlyph.Scanlines[y] = flipped;
        }

        CurrentFile.MarkModified();
        OnPropertyChanged(nameof(SelectedGlyph));
        UpdateWindowTitle();
    }

    /// <summary>
    /// Flips the selected glyph vertically.
    /// </summary>
    [RelayCommand]
    private void FlipVertical()
    {
        if (SelectedGlyph == null || CurrentFile == null)
        {
            return;
        }

        RecordCurrentState();
        Array.Reverse(SelectedGlyph.Scanlines);
        CurrentFile.MarkModified();
        OnPropertyChanged(nameof(SelectedGlyph));
        UpdateWindowTitle();
    }

    /// <summary>
    /// Shifts the glyph up with wraparound.
    /// </summary>
    [RelayCommand]
    private void ShiftUp()
    {
        if (SelectedGlyph == null || CurrentFile == null)
        {
            return;
        }

        RecordCurrentState();

        byte top = SelectedGlyph.Scanlines[0];
        for (int y = 0; y < 7; y++)
        {
            SelectedGlyph.Scanlines[y] = SelectedGlyph.Scanlines[y + 1];
        }

        SelectedGlyph.Scanlines[7] = top;

        CurrentFile.MarkModified();
        OnPropertyChanged(nameof(SelectedGlyph));
        UpdateWindowTitle();
    }

    /// <summary>
    /// Shifts the glyph down with wraparound.
    /// </summary>
    [RelayCommand]
    private void ShiftDown()
    {
        if (SelectedGlyph == null || CurrentFile == null)
        {
            return;
        }

        RecordCurrentState();

        byte bottom = SelectedGlyph.Scanlines[7];
        for (int y = 7; y > 0; y--)
        {
            SelectedGlyph.Scanlines[y] = SelectedGlyph.Scanlines[y - 1];
        }

        SelectedGlyph.Scanlines[0] = bottom;

        CurrentFile.MarkModified();
        OnPropertyChanged(nameof(SelectedGlyph));
        UpdateWindowTitle();
    }

    /// <summary>
    /// Shifts the glyph left with wraparound.
    /// </summary>
    [RelayCommand]
    private void ShiftLeft()
    {
        if (SelectedGlyph == null || CurrentFile == null)
        {
            return;
        }

        RecordCurrentState();

        for (int y = 0; y < 8; y++)
        {
            byte row = SelectedGlyph.Scanlines[y];
            bool leftBit = (row & 0x40) != 0;
            row = (byte)((row << 1) & 0x7F);
            if (leftBit)
            {
                row |= 0x01;
            }

            SelectedGlyph.Scanlines[y] = row;
        }

        CurrentFile.MarkModified();
        OnPropertyChanged(nameof(SelectedGlyph));
        UpdateWindowTitle();
    }

    /// <summary>
    /// Shifts the glyph right with wraparound.
    /// </summary>
    [RelayCommand]
    private void ShiftRight()
    {
        if (SelectedGlyph == null || CurrentFile == null)
        {
            return;
        }

        RecordCurrentState();

        for (int y = 0; y < 8; y++)
        {
            byte row = SelectedGlyph.Scanlines[y];
            bool rightBit = (row & 0x01) != 0;
            row = (byte)(row >> 1);
            if (rightBit)
            {
                row |= 0x40;
            }

            SelectedGlyph.Scanlines[y] = row;
        }

        CurrentFile.MarkModified();
        OnPropertyChanged(nameof(SelectedGlyph));
        UpdateWindowTitle();
    }

    /// <summary>
    /// Rotates the glyph clockwise.
    /// </summary>
    [RelayCommand]
    private void RotateClockwise()
    {
        if (SelectedGlyph == null || CurrentFile == null)
        {
            return;
        }

        RecordCurrentState();

        // For a 7×8 glyph, rotation is approximate
        var original = new byte[8];
        Array.Copy(SelectedGlyph.Scanlines, original, 8);
        Array.Clear(SelectedGlyph.Scanlines);

        for (int y = 0; y < 7; y++)
        {
            for (int x = 0; x < 7; x++)
            {
                bool pixel = (original[y] & (1 << (6 - x))) != 0;
                if (pixel)
                {
                    int newX = 6 - y;
                    int newY = x;
                    if (newY < 8)
                    {
                        SelectedGlyph.Scanlines[newY] |= (byte)(1 << (6 - newX));
                    }
                }
            }
        }

        CurrentFile.MarkModified();
        OnPropertyChanged(nameof(SelectedGlyph));
        UpdateWindowTitle();
    }

    /// <summary>
    /// Rotates the glyph counter-clockwise.
    /// </summary>
    [RelayCommand]
    private void RotateCounterClockwise()
    {
        if (SelectedGlyph == null || CurrentFile == null)
        {
            return;
        }

        RecordCurrentState();

        var original = new byte[8];
        Array.Copy(SelectedGlyph.Scanlines, original, 8);
        Array.Clear(SelectedGlyph.Scanlines);

        for (int y = 0; y < 7; y++)
        {
            for (int x = 0; x < 7; x++)
            {
                bool pixel = (original[y] & (1 << (6 - x))) != 0;
                if (pixel)
                {
                    int newX = y;
                    int newY = 6 - x;
                    if (newY < 8 && newX < 7)
                    {
                        SelectedGlyph.Scanlines[newY] |= (byte)(1 << (6 - newX));
                    }
                }
            }
        }

        CurrentFile.MarkModified();
        OnPropertyChanged(nameof(SelectedGlyph));
        UpdateWindowTitle();
    }

    /// <summary>
    /// Hot-loads the current glyph data to the emulator.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanHotLoad))]
    private void HotLoadToEmulator()
    {
        if (CurrentFile == null || !IsEmulatorConnected)
        {
            return;
        }

        var data = CurrentFile.ToByteArray();
        if (emulatorConnection.HotLoad(data, HotLoadTarget))
        {
            StatusText = $"Hot-loaded to emulator ({HotLoadTarget})";
        }
        else
        {
            StatusText = "Failed to hot-load to emulator";
        }
    }

    private bool CanHotLoad() => CurrentFile != null && IsEmulatorConnected;

    /// <summary>
    /// Increases the grid zoom level.
    /// </summary>
    [RelayCommand]
    private void ZoomIn()
    {
        if (GridZoomLevel < 4)
        {
            GridZoomLevel++;
        }
    }

    /// <summary>
    /// Decreases the grid zoom level.
    /// </summary>
    [RelayCommand]
    private void ZoomOut()
    {
        if (GridZoomLevel > 1)
        {
            GridZoomLevel--;
        }
    }

    partial void OnSelectedCharCodeChanged(byte value)
    {
        UpdateSelectedGlyph();
    }

    partial void OnUseAlternateSetChanged(bool value)
    {
        UpdateSelectedGlyph();
    }

    partial void OnFlashPreviewEnabledChanged(bool value)
    {
        if (value)
        {
            flashTimer.Start();
        }
        else
        {
            flashTimer.Stop();
            IsFlashOn = false;
        }
    }

    private GlyphEditHistory GetCurrentHistory()
    {
        return GetHistoryFor(SelectedCharCode, UseAlternateSet);
    }

    private GlyphEditHistory GetHistoryFor(byte charCode, bool isAlt)
    {
        var key = (charCode, isAlt);
        if (!historyMap.TryGetValue(key, out var history))
        {
            history = new GlyphEditHistory();
            historyMap[key] = history;
        }

        return history;
    }

    private void RecordCurrentState()
    {
        if (SelectedGlyph != null)
        {
            GetCurrentHistory().RecordState(SelectedGlyph);
        }
    }

    private void UpdateSelectedGlyph()
    {
        if (CurrentFile != null)
        {
            SelectedGlyph = CurrentFile.GetGlyph(SelectedCharCode, UseAlternateSet);
        }
        else
        {
            SelectedGlyph = null;
        }

        OnPropertyChanged(nameof(DisplayMode));
        OnPropertyChanged(nameof(CharacterDisplay));
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    private void UpdateWindowTitle()
    {
        var fileName = CurrentFile?.FilePath != null
            ? Path.GetFileName(CurrentFile.FilePath)
            : "Untitled";
        var modified = CurrentFile?.IsModified == true ? " *" : string.Empty;
        WindowTitle = $"{fileName}{modified} - Character Glyph Editor";
    }

    private async Task<bool> ConfirmDiscardChangesAsync()
    {
        if (CurrentFile?.IsModified != true)
        {
            return true;
        }

        return await fileService.ShowConfirmDiscardDialogAsync();
    }

    private async Task ShowErrorAsync(string message)
    {
        await fileService.ShowErrorDialogAsync(message);
    }
}