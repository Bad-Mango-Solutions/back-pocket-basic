// <copyright file="GlyphEditorWindow.axaml.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Editor.Views;

using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

using Devices;

using Rendering;

using Services;

using ViewModels;

/// <summary>
/// Code-behind for the Character Glyph Editor window.
/// </summary>
public partial class GlyphEditorWindow : Window
{
    private const int GridSize = 16;
    private const int BitmapPixelSize = 35;
    private const int BitmapGridWidth = 7;
    private const int BitmapGridHeight = 8;
    private const int PreviewScale = 8;
    private const int Preview80Scale = 4; // Half-width for 80-column

    private GlyphEditorViewModel? viewModel;
    private bool isDrawing;
    private bool drawValue;
    private bool isDraggingSelection;
    private byte dragStartCharCode;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlyphEditorWindow"/> class.
    /// </summary>
    public GlyphEditorWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GlyphEditorWindow"/> class
    /// with the specified view model.
    /// </summary>
    /// <param name="viewModel">The view model for this window.</param>
    public GlyphEditorWindow(GlyphEditorViewModel viewModel)
        : this()
    {
        this.viewModel = viewModel;
        DataContext = viewModel;

        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        Opened += OnWindowOpened;
        Closing += OnWindowClosing;
    }

    /// <summary>
    /// Creates a new glyph editor window with default services.
    /// </summary>
    /// <returns>A new <see cref="GlyphEditorWindow"/>.</returns>
    public static GlyphEditorWindow Create()
    {
        var window = new GlyphEditorWindow();
        var fileService = new GlyphFileService(window);
        var emulatorConnection = new EmulatorConnectionService();
        var vm = new GlyphEditorViewModel(fileService, emulatorConnection);

        window.viewModel = vm;
        window.DataContext = vm;
        vm.PropertyChanged += window.OnViewModelPropertyChanged;
        window.Opened += window.OnWindowOpened;
        window.Closing += window.OnWindowClosing;

        return window;
    }

    /// <summary>
    /// Creates a new glyph editor window connected to a character device.
    /// </summary>
    /// <param name="characterDevice">The character device to connect to.</param>
    /// <returns>A new <see cref="GlyphEditorWindow"/>.</returns>
    public static GlyphEditorWindow Create(Devices.Interfaces.ICharacterDevice characterDevice)
    {
        // Create the window with a connected emulator connection
        var window = new GlyphEditorWindow();
        var emulatorConnection = new EmulatorConnectionService();
        var fileService = new GlyphFileService(window);
        var vm = new GlyphEditorViewModel(fileService, emulatorConnection);

        window.viewModel = vm;
        window.DataContext = vm;
        vm.PropertyChanged += window.OnViewModelPropertyChanged;
        window.Opened += window.OnWindowOpened;
        window.Closing += window.OnWindowClosing;

        if (characterDevice is Devices.Interfaces.IGlyphHotLoader)
        {
            emulatorConnection.Connect(characterDevice);

            // Load existing ROM data if available
            var romData = characterDevice.GetCharacterRomData();
            if (!romData.IsEmpty)
            {
                var file = Models.GlyphFile.LoadFromBytes(romData.ToArray());
                vm.CurrentFile = file;
            }
        }

        return window;
    }

    private static (int X, int Y) GetPixelCoordinates(Avalonia.Point position)
    {
        int x = (int)(position.X / BitmapPixelSize);
        int y = (int)(position.Y / BitmapPixelSize);
        return (x, y);
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        RenderCharacterGrid();
        RenderBitmapEditor();
        RenderPreview();
        RenderPreview80();
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        // TODO: Check for unsaved changes and prompt
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(GlyphEditorViewModel.CurrentFile):
            case nameof(GlyphEditorViewModel.UseAlternateSet):
            case nameof(GlyphEditorViewModel.ShowGrid):
                RenderCharacterGrid();
                RenderBitmapEditor();
                RenderPreview();
                RenderPreview80();
                break;

            case nameof(GlyphEditorViewModel.SelectedCharCode):
            case nameof(GlyphEditorViewModel.SelectedGlyph):
                RenderCharacterGrid();
                RenderBitmapEditor();
                RenderPreview();
                RenderPreview80();
                break;

            case nameof(GlyphEditorViewModel.IsFlashOn):
                RenderPreview();
                RenderPreview80();
                break;
        }
    }

    private void RenderCharacterGrid()
    {
        CharacterCanvas.Children.Clear();

        if (viewModel?.CurrentFile == null)
        {
            return;
        }

        // Fixed zoom level 3x, with pixel scale of 4 (3+1)
        int pixelScale = viewModel.GridZoomLevel + 1;

        // Cell dimensions: 7 pixels wide × 8 pixels tall, plus 2px padding
        int cellWidth = (CharacterRenderer.CharacterWidth * pixelScale) + 2;
        int cellHeight = (CharacterRenderer.CharacterHeight * pixelScale) + 2;
        int totalWidth = GridSize * cellWidth;
        int totalHeight = GridSize * cellHeight;

        CharacterCanvas.Width = totalWidth;
        CharacterCanvas.Height = totalHeight;

        var glyphData = viewModel.CurrentFile.ToByteArray();
        int romOffset = viewModel.UseAlternateSet ? Models.GlyphFile.CharacterSetSize : 0;

        for (int charIndex = 0; charIndex < 256; charIndex++)
        {
            int row = charIndex / GridSize;
            int col = charIndex % GridSize;
            int x = col * cellWidth;
            int y = row * cellHeight;

            // Determine cell background based on selection state
            IBrush bgColor;
            if (charIndex == viewModel.SelectedCharCode)
            {
                bgColor = new SolidColorBrush(Color.FromRgb(0, 80, 120)); // Primary selection
            }
            else if (viewModel.IsCharacterSelected((byte)charIndex))
            {
                bgColor = new SolidColorBrush(Color.FromRgb(0, 60, 90)); // Multi-selection
            }
            else
            {
                bgColor = new SolidColorBrush(Color.FromRgb(40, 40, 40));
            }

            var cellRect = new Rectangle
            {
                Width = cellWidth - 1,
                Height = cellHeight - 1,
                Fill = bgColor,
            };
            Canvas.SetLeft(cellRect, x);
            Canvas.SetTop(cellRect, y);
            CharacterCanvas.Children.Add(cellRect);

            // Render character pixels (no grid lines between individual pixels)
            for (int scanline = 0; scanline < 8; scanline++)
            {
                int dataOffset = romOffset + (charIndex * 8) + scanline;
                if (dataOffset >= glyphData.Length)
                {
                    continue;
                }

                byte scanlineData = glyphData[dataOffset];

                for (int pixel = 0; pixel < 7; pixel++)
                {
                    bool isSet = (scanlineData & (1 << (6 - pixel))) != 0;
                    if (isSet)
                    {
                        var pixelRect = new Rectangle
                        {
                            Width = pixelScale,
                            Height = pixelScale,
                            Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0)),
                        };
                        Canvas.SetLeft(pixelRect, x + (pixel * (double)pixelScale) + 1.0);
                        Canvas.SetTop(pixelRect, y + (scanline * (double)pixelScale) + 1.0);
                        CharacterCanvas.Children.Add(pixelRect);
                    }
                }
            }

            // Click handler
            var hitRect = new Rectangle
            {
                Width = cellWidth,
                Height = cellHeight,
                Fill = Brushes.Transparent,
                Tag = charIndex,
            };
            hitRect.PointerPressed += OnCharacterCellClicked;
            Canvas.SetLeft(hitRect, x);
            Canvas.SetTop(hitRect, y);
            CharacterCanvas.Children.Add(hitRect);
        }

        // Draw grid lines between character cells (not between pixels)
        if (viewModel.ShowGrid)
        {
            for (int i = 0; i <= GridSize; i++)
            {
                var hLine = new Line
                {
                    StartPoint = new Avalonia.Point(0, i * cellHeight),
                    EndPoint = new Avalonia.Point(totalWidth, i * cellHeight),
                    Stroke = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                    StrokeThickness = 1,
                };
                CharacterCanvas.Children.Add(hLine);

                var vLine = new Line
                {
                    StartPoint = new Avalonia.Point(i * cellWidth, 0),
                    EndPoint = new Avalonia.Point(i * cellWidth, totalHeight),
                    Stroke = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                    StrokeThickness = 1,
                };
                CharacterCanvas.Children.Add(vLine);
            }
        }
    }

    private void OnCharacterCellClicked(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Rectangle rect && rect.Tag is int charIndex && viewModel != null)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                // Shift+click: range selection from current to clicked
                viewModel.ExtendSelectionTo((byte)charIndex);
            }
            else if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                // Ctrl+click: toggle selection
                viewModel.ToggleSelection((byte)charIndex);
            }
            else
            {
                // Plain click: start potential drag selection
                isDraggingSelection = true;
                dragStartCharCode = (byte)charIndex;
                viewModel.SelectCharacter((byte)charIndex);
            }

            RenderCharacterGrid();
            e.Handled = true;
        }
    }

    private void OnCharacterCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!isDraggingSelection || viewModel?.CurrentFile == null)
        {
            return;
        }

        var point = e.GetCurrentPoint(CharacterCanvas);
        var charCode = GetCharacterCodeFromPosition(point.Position);
        if (charCode.HasValue && charCode.Value != dragStartCharCode)
        {
            // Clear and select range
            viewModel.SelectCharacter(dragStartCharCode);
            viewModel.ExtendSelectionTo(charCode.Value);
            RenderCharacterGrid();
        }
    }

    private void OnCharacterCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        isDraggingSelection = false;
    }

    private byte? GetCharacterCodeFromPosition(Avalonia.Point position)
    {
        if (viewModel == null)
        {
            return null;
        }

        int pixelScale = viewModel.GridZoomLevel + 1;
        int cellWidth = (CharacterRenderer.CharacterWidth * pixelScale) + 2;
        int cellHeight = (CharacterRenderer.CharacterHeight * pixelScale) + 2;

        int col = (int)(position.X / cellWidth);
        int row = (int)(position.Y / cellHeight);

        if (col < 0 || col >= GridSize || row < 0 || row >= GridSize)
        {
            return null;
        }

        return (byte)((row * GridSize) + col);
    }

    private void RenderBitmapEditor()
    {
        BitmapEditorCanvas.Children.Clear();

        if (viewModel?.SelectedGlyph == null)
        {
            return;
        }

        var glyph = viewModel.SelectedGlyph;

        // Background
        var bgRect = new Rectangle
        {
            Width = BitmapGridWidth * BitmapPixelSize,
            Height = BitmapGridHeight * BitmapPixelSize,
            Fill = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
        };
        Canvas.SetLeft(bgRect, 0);
        Canvas.SetTop(bgRect, 0);
        BitmapEditorCanvas.Children.Add(bgRect);

        // Draw pixels
        for (int y = 0; y < BitmapGridHeight; y++)
        {
            for (int x = 0; x < BitmapGridWidth; x++)
            {
                bool isSet = glyph[x, y];
                var color = isSet
                    ? new SolidColorBrush(Color.FromRgb(0, 255, 0))
                    : new SolidColorBrush(Color.FromRgb(50, 50, 50));

                var pixelRect = new Rectangle
                {
                    Width = BitmapPixelSize - 2,
                    Height = BitmapPixelSize - 2,
                    Fill = color,
                    RadiusX = 2,
                    RadiusY = 2,
                };
                Canvas.SetLeft(pixelRect, (x * (double)BitmapPixelSize) + 1.0);
                Canvas.SetTop(pixelRect, (y * (double)BitmapPixelSize) + 1.0);
                BitmapEditorCanvas.Children.Add(pixelRect);
            }
        }

        // Grid lines
        for (int i = 0; i <= BitmapGridWidth; i++)
        {
            var vLine = new Line
            {
                StartPoint = new Avalonia.Point(i * BitmapPixelSize, 0),
                EndPoint = new Avalonia.Point(i * BitmapPixelSize, BitmapGridHeight * BitmapPixelSize),
                Stroke = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                StrokeThickness = 1,
            };
            BitmapEditorCanvas.Children.Add(vLine);
        }

        for (int i = 0; i <= BitmapGridHeight; i++)
        {
            var hLine = new Line
            {
                StartPoint = new Avalonia.Point(0, i * BitmapPixelSize),
                EndPoint = new Avalonia.Point(BitmapGridWidth * BitmapPixelSize, i * BitmapPixelSize),
                Stroke = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                StrokeThickness = 1,
            };
            BitmapEditorCanvas.Children.Add(hLine);
        }
    }

    private void RenderPreview()
    {
        PreviewCanvas.Children.Clear();

        if (viewModel?.SelectedGlyph == null)
        {
            return;
        }

        var glyph = viewModel.SelectedGlyph;
        bool invert = viewModel.FlashPreviewEnabled && viewModel.IsFlashOn &&
                      viewModel.SelectedCharCode is >= 0x40 and < 0x80 &&
                      !viewModel.UseAlternateSet;

        // Background
        var bgRect = new Rectangle
        {
            Width = BitmapGridWidth * PreviewScale,
            Height = BitmapGridHeight * PreviewScale,
            Fill = Brushes.Black,
        };
        PreviewCanvas.Children.Add(bgRect);

        // Draw pixels
        for (int y = 0; y < BitmapGridHeight; y++)
        {
            for (int x = 0; x < BitmapGridWidth; x++)
            {
                bool isSet = glyph[x, y];
                if (invert)
                {
                    isSet = !isSet;
                }

                if (isSet)
                {
                    var pixelRect = new Rectangle
                    {
                        Width = PreviewScale,
                        Height = PreviewScale,
                        Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0)),
                    };
                    Canvas.SetLeft(pixelRect, (double)x * PreviewScale);
                    Canvas.SetTop(pixelRect, (double)y * PreviewScale);
                    PreviewCanvas.Children.Add(pixelRect);
                }
            }
        }
    }

    private void RenderPreview80()
    {
        Preview80Canvas.Children.Clear();

        if (viewModel?.SelectedGlyph == null)
        {
            return;
        }

        var glyph = viewModel.SelectedGlyph;
        bool invert = viewModel.FlashPreviewEnabled && viewModel.IsFlashOn &&
                      viewModel.SelectedCharCode is >= 0x40 and < 0x80 &&
                      !viewModel.UseAlternateSet;

        // Background - half width for 80-column mode
        var bgRect = new Rectangle
        {
            Width = BitmapGridWidth * Preview80Scale,
            Height = BitmapGridHeight * PreviewScale,
            Fill = Brushes.Black,
        };
        Preview80Canvas.Children.Add(bgRect);

        // Draw pixels - half width (4px wide, 8px tall per pixel)
        for (int y = 0; y < BitmapGridHeight; y++)
        {
            for (int x = 0; x < BitmapGridWidth; x++)
            {
                bool isSet = glyph[x, y];
                if (invert)
                {
                    isSet = !isSet;
                }

                if (isSet)
                {
                    var pixelRect = new Rectangle
                    {
                        Width = Preview80Scale,
                        Height = PreviewScale,
                        Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0)),
                    };
                    Canvas.SetLeft(pixelRect, (double)x * Preview80Scale);
                    Canvas.SetTop(pixelRect, (double)y * PreviewScale);
                    Preview80Canvas.Children.Add(pixelRect);
                }
            }
        }
    }

    private void OnBitmapEditorPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (viewModel?.SelectedGlyph == null)
        {
            return;
        }

        var point = e.GetCurrentPoint(BitmapEditorCanvas);
        var (x, y) = GetPixelCoordinates(point.Position);

        if (x >= 0 && x < BitmapGridWidth && y >= 0 && y < BitmapGridHeight)
        {
            isDrawing = true;
            drawValue = !viewModel.SelectedGlyph[x, y];
            viewModel.TogglePixel(x, y);
            RenderBitmapEditor();
            RenderPreview();
            RenderPreview80();
            e.Handled = true;
        }
    }

    private void OnBitmapEditorPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!isDrawing || viewModel?.SelectedGlyph == null)
        {
            return;
        }

        var point = e.GetCurrentPoint(BitmapEditorCanvas);
        var (x, y) = GetPixelCoordinates(point.Position);

        if (x >= 0 && x < BitmapGridWidth && y >= 0 && y < BitmapGridHeight)
        {
            if (viewModel.SelectedGlyph[x, y] != drawValue)
            {
                viewModel.SetPixel(x, y, drawValue);
                RenderBitmapEditor();
                RenderPreview();
                RenderPreview80();
            }
        }
    }

    private void OnBitmapEditorPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        isDrawing = false;
    }

    private void OnExitClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnAboutClick(object? sender, RoutedEventArgs e)
    {
        // Show about dialog
    }
}