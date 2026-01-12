// <copyright file="TextEditorWindow.axaml.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.TextEditor;

using System.ComponentModel;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

using AvaloniaEdit;
using AvaloniaEdit.TextMate;

using TextMateSharp.Grammars;

/// <summary>
/// A text editor window with syntax highlighting support.
/// </summary>
/// <remarks>
/// <para>
/// This window provides a full-featured text editor with support for
/// opening, editing, and saving text files with syntax highlighting.
/// </para>
/// <para>
/// Supported file types include plain text, Markdown, JSON, and assembly
/// language source files.
/// </para>
/// </remarks>
public partial class TextEditorWindow : Window
{
    private readonly RegistryOptions registryOptions;
    private TextMate.Installation? textMateInstallation;
    private AvaloniaEdit.Search.SearchPanel? searchPanel;
    private string? currentFilePath;
    private SyntaxLanguage currentLanguage = SyntaxLanguage.PlainText;
    private bool isModified;
    private string savedContent = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextEditorWindow"/> class.
    /// </summary>
    public TextEditorWindow()
    {
        this.InitializeComponent();

        // Initialize TextMate with a dark theme
        this.registryOptions = new RegistryOptions(ThemeName.DarkPlus);

        this.SetupEditor();
        this.SetupSyntaxMenu();
        this.SetupKeyBindings();

        this.Closing += this.OnWindowClosing;
    }

    /// <summary>
    /// Opens a file in the editor.
    /// </summary>
    /// <param name="filePath">The file path to open. Supports library:// scheme.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task OpenFileAsync(string filePath)
    {
        string resolvedPath = FilePathResolver.ResolvePath(filePath);

        if (!File.Exists(resolvedPath))
        {
            await this.ShowErrorDialogAsync($"File not found: {filePath}");
            return;
        }

        try
        {
            string content = await File.ReadAllTextAsync(resolvedPath);
            this.Editor.Text = content;
            this.savedContent = content;
            this.currentFilePath = filePath;
            this.isModified = false;
            this.UpdateTitle();
            this.UpdateStatusBar();

            // Auto-detect syntax highlighting
            var language = SyntaxHighlightingManager.GetLanguageForFile(resolvedPath);
            this.SetSyntaxLanguage(language);
        }
        catch (Exception ex)
        {
            await this.ShowErrorDialogAsync($"Failed to open file: {ex.Message}");
        }
    }

    private void SetupEditor()
    {
        // Install TextMate syntax highlighting
        this.textMateInstallation = this.Editor.InstallTextMate(this.registryOptions);

        // Track text changes for modified indicator
        this.Editor.TextChanged += this.OnEditorTextChanged;

        // Track cursor position
        this.Editor.TextArea.Caret.PositionChanged += this.OnCaretPositionChanged;

        // Set initial syntax
        this.SetSyntaxLanguage(SyntaxLanguage.PlainText);
    }

    private void SetupSyntaxMenu()
    {
        var menuItems = SyntaxHighlightingManager.GetSupportedLanguages()
            .Select(language =>
            {
                var menuItem = new MenuItem
                {
                    Header = SyntaxHighlightingManager.GetDisplayName(language),
                    Tag = language,
                };
                menuItem.Click += this.OnSyntaxMenuItemClick;
                return menuItem;
            });

        foreach (var menuItem in menuItems)
        {
            this.SyntaxMenuItem.Items.Add(menuItem);
        }
    }

    private void SetupKeyBindings()
    {
        this.KeyDown += this.OnKeyDown;
    }

    private void SetSyntaxLanguage(SyntaxLanguage language)
    {
        this.currentLanguage = language;

        var scope = SyntaxHighlightingManager.GetTextMateScope(language);
        if (scope != null && this.textMateInstallation != null)
        {
            try
            {
                this.textMateInstallation.SetGrammar(scope);
            }
            catch
            {
                // Fall back to no highlighting if grammar isn't available
                this.textMateInstallation.SetGrammar(null);
            }
        }
        else
        {
            this.textMateInstallation?.SetGrammar(null);
        }

        this.SyntaxText.Text = SyntaxHighlightingManager.GetDisplayName(language);
        this.UpdateSyntaxMenuChecks();
    }

    private void UpdateSyntaxMenuChecks()
    {
        foreach (var item in this.SyntaxMenuItem.Items.OfType<MenuItem>())
        {
            if (item.Tag is SyntaxLanguage language)
            {
                item.Icon = language == this.currentLanguage
                    ? new TextBlock { Text = "✓" }
                    : null;
            }
        }
    }

    private void UpdateTitle()
    {
        string fileName = string.IsNullOrEmpty(this.currentFilePath)
            ? "Untitled"
            : Path.GetFileName(FilePathResolver.IsLibraryPath(this.currentFilePath)
                ? FilePathResolver.ResolvePath(this.currentFilePath)
                : this.currentFilePath);

        this.Title = this.isModified ? $"*{fileName} - Text Editor" : $"{fileName} - Text Editor";
    }

    private void UpdateStatusBar()
    {
        this.FilePathText.Text = string.IsNullOrEmpty(this.currentFilePath)
            ? "Untitled"
            : this.currentFilePath;

        this.ModifiedIndicator.Text = this.isModified ? "Modified" : string.Empty;
    }

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        bool wasModified = this.isModified;
        this.isModified = this.Editor.Text != this.savedContent;

        if (wasModified != this.isModified)
        {
            this.UpdateTitle();
            this.UpdateStatusBar();
        }
    }

    private void OnCaretPositionChanged(object? sender, EventArgs e)
    {
        var caret = this.Editor.TextArea.Caret;
        this.CursorPositionText.Text = $"Ln {caret.Line}, Col {caret.Column}";
    }

    private void OnSyntaxMenuItemClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag is SyntaxLanguage language)
        {
            this.SetSyntaxLanguage(language);
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers == KeyModifiers.Control)
        {
            switch (e.Key)
            {
                case Key.N:
                    this.OnNewClick(sender, e);
                    e.Handled = true;
                    break;
                case Key.O:
                    _ = this.OpenFileDialogAsync();
                    e.Handled = true;
                    break;
                case Key.S when e.KeyModifiers.HasFlag(KeyModifiers.Shift):
                    _ = this.SaveAsAsync();
                    e.Handled = true;
                    break;
                case Key.S:
                    _ = this.SaveAsync();
                    e.Handled = true;
                    break;
                case Key.W:
                    this.Close();
                    e.Handled = true;
                    break;
            }
        }
    }

    private void OnNewClick(object? sender, RoutedEventArgs e)
    {
        // Open a new editor window
        var newWindow = new TextEditorWindow();
        newWindow.Show();
    }

    private async void OnOpenClick(object? sender, RoutedEventArgs e)
    {
        await this.OpenFileDialogAsync();
    }

    private async Task OpenFileDialogAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is not { } storage)
        {
            return;
        }

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open File",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("All Supported Files")
                {
                    Patterns = ["*.txt", "*.md", "*.json", "*.yaml", "*.yml", "*.s", "*.asm", "*.h"],
                },
                new FilePickerFileType("Text Files") { Patterns = ["*.txt"] },
                new FilePickerFileType("Markdown Files") { Patterns = ["*.md"] },
                new FilePickerFileType("JSON Files") { Patterns = ["*.json"] },
                new FilePickerFileType("YAML Files") { Patterns = ["*.yaml", "*.yml"] },
                new FilePickerFileType("Assembly Files") { Patterns = ["*.s", "*.asm", "*.h"] },
                new FilePickerFileType("All Files") { Patterns = ["*"] },
            ],
        });

        if (files.Count > 0)
        {
            var file = files[0];
            var path = file.TryGetLocalPath();
            if (path != null)
            {
                await this.OpenFileAsync(path);
            }
        }
    }

    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        await this.SaveAsync();
    }

    private async Task<bool> SaveAsync()
    {
        if (string.IsNullOrEmpty(this.currentFilePath))
        {
            return await this.SaveAsAsync();
        }

        return await this.SaveToFileAsync(this.currentFilePath);
    }

    private async void OnSaveAsClick(object? sender, RoutedEventArgs e)
    {
        await this.SaveAsAsync();
    }

    private async Task<bool> SaveAsAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is not { } storage)
        {
            return false;
        }

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save File As",
            DefaultExtension = "txt",
            FileTypeChoices =
            [
                new FilePickerFileType("Text Files") { Patterns = ["*.txt"] },
                new FilePickerFileType("Markdown Files") { Patterns = ["*.md"] },
                new FilePickerFileType("JSON Files") { Patterns = ["*.json"] },
                new FilePickerFileType("YAML Files") { Patterns = ["*.yaml", "*.yml"] },
                new FilePickerFileType("Assembly Files") { Patterns = ["*.s", "*.asm", "*.h"] },
                new FilePickerFileType("All Files") { Patterns = ["*"] },
            ],
        });

        if (file != null)
        {
            var path = file.TryGetLocalPath();
            if (path != null)
            {
                return await this.SaveToFileAsync(path);
            }
        }

        return false;
    }

    private async Task<bool> SaveToFileAsync(string filePath)
    {
        try
        {
            string resolvedPath = FilePathResolver.ResolvePath(filePath);

            // Ensure directory exists
            string? directory = Path.GetDirectoryName(resolvedPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(resolvedPath, this.Editor.Text);

            this.currentFilePath = filePath;
            this.savedContent = this.Editor.Text;
            this.isModified = false;
            this.UpdateTitle();
            this.UpdateStatusBar();

            // Auto-detect syntax highlighting for new file extension
            var language = SyntaxHighlightingManager.GetLanguageForFile(resolvedPath);
            this.SetSyntaxLanguage(language);

            return true;
        }
        catch (Exception ex)
        {
            await this.ShowErrorDialogAsync($"Failed to save file: {ex.Message}");
            return false;
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void OnUndoClick(object? sender, RoutedEventArgs e)
    {
        this.Editor.Undo();
    }

    private void OnRedoClick(object? sender, RoutedEventArgs e)
    {
        this.Editor.Redo();
    }

    private void OnCutClick(object? sender, RoutedEventArgs e)
    {
        this.Editor.Cut();
    }

    private void OnCopyClick(object? sender, RoutedEventArgs e)
    {
        this.Editor.Copy();
    }

    private void OnPasteClick(object? sender, RoutedEventArgs e)
    {
        this.Editor.Paste();
    }

    private void OnSelectAllClick(object? sender, RoutedEventArgs e)
    {
        this.Editor.SelectAll();
    }

    private void OnFindReplaceClick(object? sender, RoutedEventArgs e)
    {
        // Install search panel once and reuse it
        this.searchPanel ??= AvaloniaEdit.Search.SearchPanel.Install(this.Editor);
        this.searchPanel.Open();
    }

    private async void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (!this.isModified)
        {
            return;
        }

        e.Cancel = true;

        var result = await this.ShowUnsavedChangesDialogAsync();

        switch (result)
        {
            case UnsavedChangesResult.Save:
                if (await this.SaveAsync())
                {
                    this.isModified = false;
                    this.Close();
                }

                break;
            case UnsavedChangesResult.Discard:
                this.isModified = false;
                this.Close();
                break;
            case UnsavedChangesResult.Cancel:
                // Stay open
                break;
        }
    }

    private async Task<UnsavedChangesResult> ShowUnsavedChangesDialogAsync()
    {
        var dialog = new Window
        {
            Title = "Unsaved Changes",
            Width = 400,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
        };

        var result = UnsavedChangesResult.Cancel;

        var panel = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 15,
        };

        panel.Children.Add(new TextBlock
        {
            Text = "Do you want to save changes before closing?",
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        });

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Spacing = 10,
        };

        var saveButton = new Button { Content = "Save" };
        saveButton.Click += (_, _) =>
        {
            result = UnsavedChangesResult.Save;
            dialog.Close();
        };

        var discardButton = new Button { Content = "Discard" };
        discardButton.Click += (_, _) =>
        {
            result = UnsavedChangesResult.Discard;
            dialog.Close();
        };

        var cancelButton = new Button { Content = "Cancel" };
        cancelButton.Click += (_, _) =>
        {
            result = UnsavedChangesResult.Cancel;
            dialog.Close();
        };

        buttonPanel.Children.Add(saveButton);
        buttonPanel.Children.Add(discardButton);
        buttonPanel.Children.Add(cancelButton);

        panel.Children.Add(buttonPanel);
        dialog.Content = panel;

        await dialog.ShowDialog(this);
        return result;
    }

    private async Task ShowErrorDialogAsync(string message)
    {
        var dialog = new Window
        {
            Title = "Error",
            Width = 400,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
        };

        var panel = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 15,
        };

        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        });

        var okButton = new Button
        {
            Content = "OK",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
        };
        okButton.Click += (_, _) => dialog.Close();

        panel.Children.Add(okButton);
        dialog.Content = panel;

        await dialog.ShowDialog(this);
    }
}