using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkiaSharp;
using TreeGrowth.Avalonia.Core;

namespace TreeGrowth.Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private ForestFireSimulation _simulation;
    private ForestFireRenderer _renderer;
    private readonly DispatcherTimer _timer;
    private bool _isSimulating;
    private DateTime _lastFrameTime = DateTime.Now;
    private WriteableBitmap? _writeableBitmap;

    // Grid configuration (needed for settings)
    private int _outputWidth = 1920;
    private int _outputHeight = 1080;
    private int _cellSize = 1;
    private int _targetFps = 60;

    // ============================================================
    // === OBSERVABLE PROPERTIES ===
    // ============================================================

    [ObservableProperty]
    private WriteableBitmap? _simulationImage;

    [ObservableProperty]
    private string _statusText = "Paused";

    [ObservableProperty]
    private string _fpsText = "FPS: 0";

    [ObservableProperty]
    private string _statsText = "Trees: 0 | Fires: 0";

    [ObservableProperty]
    private double _actualFps;

    [ObservableProperty]
    private int _treeCount;

    [ObservableProperty]
    private long _totalFires;

    [ObservableProperty]
    private long _timesteps;

    [ObservableProperty]
    private double _density;

    [ObservableProperty]
    private bool _isRunning;

    // Button text that changes based on state
    public string ToggleButtonText => IsRunning ? "⏸ Stop" : "▶ Start";

    // Parameters
    [ObservableProperty]
    private double _treeGrowth = 0.01;

    [ObservableProperty]
    private double _lightning = 0.000001;

    [ObservableProperty]
    private int _stepsPerFrame = 1000;

    [ObservableProperty]
    private string _seed = "default";

    // Perlin Noise Parameters
    [ObservableProperty]
    private bool _usePerlinNoise = false;

    [ObservableProperty]
    private double _noiseScale = 50.0;

    [ObservableProperty]
    private int _noiseOctaves = 4;

    [ObservableProperty]
    private double _noiseThreshold = 0.3;

    [ObservableProperty]
    private double _noiseStrength = 1.0;

    // Overlay Properties
    [ObservableProperty]
    private bool _showOverlay = false;

    [ObservableProperty]
    private string _overlayFilePath = string.Empty;

    [ObservableProperty]
    private bool _hasOverlayImage = false;

    // Color Properties
    [ObservableProperty]
    private Color _treeColor = Color.FromRgb(198, 164, 145);

    [ObservableProperty]
    private Color _vacantColor = Color.FromRgb(169, 130, 104);

    [ObservableProperty]
    private Color _fireColor = Color.FromRgb(255, 200, 0);

    [ObservableProperty]
    private Color _burnoutColor = Color.FromRgb(255, 191, 0);

    [ObservableProperty]
    private int _selectedPresetIndex = 0;

    // Color Preset Names for UI binding
    public List<string> ColorPresetNames { get; }

    // Storage provider for file dialogs
    public IStorageProvider? StorageProvider { get; set; }

    public MainWindowViewModel()
    {
        // Initialize color preset names
        ColorPresetNames = ColorPresetManager.GetPresetNames().ToList();

        // Load saved settings or use defaults
        var settings = SimulationSettings.LoadDefaults();
        ApplySettings(settings);

        // Create timer for animation
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1000.0 / _targetFps)
        };
        _timer.Tick += OnTimerTick;

        // Initial render
        UpdateSimulationImage();
    }

    /// <summary>
    /// Applies settings to the application state
    /// </summary>
    private void ApplySettings(SimulationSettings settings)
    {
        // Apply grid configuration
        _outputWidth = settings.OutputWidth;
        _outputHeight = settings.OutputHeight;
        _cellSize = settings.CellSize;
        _targetFps = settings.TargetFps;

        // Initialize simulation and renderer
        _simulation = new ForestFireSimulation(_outputWidth, _outputHeight, _cellSize, Environment.ProcessorCount);
        _renderer = new ForestFireRenderer(_outputWidth, _outputHeight, _cellSize, Environment.ProcessorCount);

        // Apply simulation parameters
        _simulation.P = settings.P;
        _simulation.F = settings.F;
        _simulation.BaseStepsPerFrame = settings.BaseStepsPerFrame;
        _simulation.AnimateFires = settings.AnimateFires;
        _simulation.UseMooreNeighborhood = settings.UseMooreNeighborhood;
        _simulation.BurnDecayFrames = settings.BurnDecayFrames;
        _simulation.FireAnimationSpeed = settings.FireAnimationSpeed;

        // Apply Perlin noise settings
        _simulation.UsePerlinDistribution = settings.UsePerlinDistribution;
        _simulation.NoiseScale = settings.NoiseScale;
        _simulation.NoiseOctaves = settings.NoiseOctaves;
        _simulation.NoiseThreshold = settings.NoiseThreshold;
        _simulation.NoiseStrength = settings.NoiseStrength;

        // Apply renderer settings
        _renderer.FireFlickerRange = settings.FireFlickerRange;
        _renderer.EnableBloom = settings.EnableBloom;
        _renderer.BloomRadius = settings.BloomRadius;
        _renderer.BloomIntensity = settings.BloomIntensity;
        _renderer.BloomFireOnly = settings.BloomFireOnly;

        // Apply colors
        TreeColor = settings.ColorTree;
        VacantColor = settings.ColorVacant;
        FireColor = settings.ColorFireBase;
        BurnoutColor = settings.ColorBurnout;
        UpdateRendererColors();

        // Apply UI parameters
        TreeGrowth = settings.P;
        Lightning = settings.F;
        StepsPerFrame = settings.BaseStepsPerFrame;
        Seed = settings.Seed;
        UsePerlinNoise = settings.UsePerlinDistribution;
        NoiseScale = settings.NoiseScale;
        NoiseOctaves = settings.NoiseOctaves;
        NoiseThreshold = settings.NoiseThreshold;
        NoiseStrength = settings.NoiseStrength;
        SelectedPresetIndex = settings.SelectedPresetIndex;

        // Initialize simulation
        _simulation.Initialize(settings.Seed);

        // Create WriteableBitmap for Avalonia
        _writeableBitmap = new WriteableBitmap(
            new global::Avalonia.PixelSize(_outputWidth, _outputHeight),
            new global::Avalonia.Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);
        
        SimulationImage = _writeableBitmap;
    }

    /// <summary>
    /// Captures current application state into a settings object
    /// </summary>
    private SimulationSettings CaptureSettings()
    {
        return new SimulationSettings
        {
            // Grid Configuration
            OutputWidth = _outputWidth,
            OutputHeight = _outputHeight,
            CellSize = _cellSize,
            TargetFps = _targetFps,

            // Simulation Parameters
            P = TreeGrowth,
            F = Lightning,
            BaseStepsPerFrame = StepsPerFrame,
            AnimateFires = _simulation.AnimateFires,
            UseMooreNeighborhood = _simulation.UseMooreNeighborhood,
            BurnDecayFrames = _simulation.BurnDecayFrames,
            FireAnimationSpeed = _simulation.FireAnimationSpeed,

            // Perlin Noise Distribution
            UsePerlinDistribution = UsePerlinNoise,
            NoiseScale = NoiseScale,
            NoiseOctaves = NoiseOctaves,
            NoiseThreshold = NoiseThreshold,
            NoiseStrength = NoiseStrength,

            // Visual Parameters
            FireFlickerRange = _renderer.FireFlickerRange,
            EnableBloom = _renderer.EnableBloom,
            BloomRadius = _renderer.BloomRadius,
            BloomIntensity = _renderer.BloomIntensity,
            BloomFireOnly = _renderer.BloomFireOnly,

            // Colors
            ColorTree = TreeColor,
            ColorVacant = VacantColor,
            ColorFireBase = FireColor,
            ColorBurnout = BurnoutColor,

            // Overlay Settings
            ShowOverlay = ShowOverlay,
            OverlayFilePath = OverlayFilePath,

            // UI State
            Seed = Seed,
            SelectedPresetIndex = SelectedPresetIndex
        };
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        _simulation.Step();
        UpdateSimulationImage();
        UpdateStats();

        var elapsed = (DateTime.Now - _lastFrameTime).TotalSeconds;
        if (elapsed > 0)
        {
            ActualFps = 1.0 / elapsed;
            FpsText = $"FPS: {ActualFps:0.0}";
        }
        _lastFrameTime = DateTime.Now;
    }

    private void UpdateSimulationImage()
    {
        // Render to SKBitmap
        var skBitmap = _renderer.Render(_simulation);

        // Create a new WriteableBitmap for this frame
        var newBitmap = new WriteableBitmap(
            new global::Avalonia.PixelSize(skBitmap.Width, skBitmap.Height),
            new global::Avalonia.Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        // Copy pixels to WriteableBitmap
        using (var lockedBitmap = newBitmap.Lock())
        {
            unsafe
            {
                // Get source pixels from SKBitmap
                IntPtr srcPtr = skBitmap.GetPixels();
                // Get destination pixels from WriteableBitmap
                IntPtr dstPtr = lockedBitmap.Address;
                
                // Copy pixel data
                int byteCount = skBitmap.Width * skBitmap.Height * 4;
                Buffer.MemoryCopy((void*)srcPtr, (void*)dstPtr, byteCount, byteCount);
            }
        }

        // Dispose old bitmap and update property
        var oldBitmap = _writeableBitmap;
        _writeableBitmap = newBitmap;
        SimulationImage = newBitmap;
        oldBitmap?.Dispose();
    }

    private void UpdateStats()
    {
        TreeCount = _simulation.TreeCount;
        TotalFires = _simulation.TotalFires;
        Timesteps = _simulation.Timesteps;
        Density = (double)_simulation.TreeCount / _simulation.TotalLogicalCells * 100.0;
        
        string overlayStatus = ShowOverlay && HasOverlayImage ? " | 🖼 Overlay" : "";
        StatsText = $"Trees: {TreeCount:n0} ({Density:0.0}%) | Fires: {TotalFires:n0} | Steps: {Timesteps:n0}{overlayStatus}";
        StatusText = _isSimulating ? "▶ Running" : "⏸ Paused";
    }

    [RelayCommand]
    private void ToggleSimulation()
    {
        _isSimulating = !_isSimulating;
        IsRunning = _isSimulating;

        if (_isSimulating)
            _timer.Start();
        else
            _timer.Stop();

        UpdateStats();
        OnPropertyChanged(nameof(ToggleButtonText));
    }

    [RelayCommand]
    private void ResetSimulation()
    {
        _isSimulating = false;
        _timer.Stop();
        IsRunning = false;

        _simulation.P = TreeGrowth;
        _simulation.F = Lightning;
        _simulation.BaseStepsPerFrame = StepsPerFrame;
        _simulation.Initialize(Seed);

        UpdateSimulationImage();
        UpdateStats();
        OnPropertyChanged(nameof(ToggleButtonText));
    }

    // Auto-update parameters when sliders change
    partial void OnTreeGrowthChanged(double value)
    {
        if (_simulation != null)
            _simulation.P = value;
    }

    partial void OnLightningChanged(double value)
    {
        if (_simulation != null)
            _simulation.F = value;
    }

    partial void OnStepsPerFrameChanged(int value)
    {
        if (_simulation != null)
            _simulation.BaseStepsPerFrame = value;
    }

    partial void OnUsePerlinNoiseChanged(bool value)
    {
        if (_simulation != null)
            _simulation.UsePerlinDistribution = value;
    }

    partial void OnNoiseScaleChanged(double value)
    {
        if (_simulation != null)
            _simulation.NoiseScale = value;
    }

    partial void OnNoiseOctavesChanged(int value)
    {
        if (_simulation != null)
            _simulation.NoiseOctaves = value;
    }

    partial void OnNoiseThresholdChanged(double value)
    {
        if (_simulation != null)
            _simulation.NoiseThreshold = value;
    }

    partial void OnNoiseStrengthChanged(double value)
    {
        if (_simulation != null)
            _simulation.NoiseStrength = value;
    }

    [RelayCommand]
    private async Task LoadOverlayImageAsync()
    {
        if (StorageProvider == null) return;

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Load Overlay Image",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Image Files")
                {
                    Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp" }
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = new[] { "*" }
                }
            }
        });

        if (files.Count > 0)
        {
            try
            {
                var file = files[0];
                var filePath = file.Path.LocalPath;

                if (_renderer.LoadOverlayImage(filePath))
                {
                    OverlayFilePath = Path.GetFileName(filePath);
                    HasOverlayImage = true;
                    ShowOverlay = true;
                    _renderer.ShowOverlay = true;
                    
                    UpdateSimulationImage();
                    UpdateStats();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load overlay: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private void ClearOverlayImage()
    {
        _renderer.ClearOverlayImage();
        HasOverlayImage = false;
        ShowOverlay = false;
        _renderer.ShowOverlay = false;
        OverlayFilePath = string.Empty;
        
        UpdateSimulationImage();
        UpdateStats();
    }

    partial void OnShowOverlayChanged(bool value)
    {
        _renderer.ShowOverlay = value;
        UpdateSimulationImage();
        UpdateStats();
    }

    [RelayCommand]
    private void ApplyColorPreset()
    {
        if (SelectedPresetIndex < 0 || SelectedPresetIndex >= ColorPresetNames.Count)
            return;

        var presetName = ColorPresetNames[SelectedPresetIndex];
        if (Enum.TryParse<ColorPresetManager.Preset>(presetName, out var preset))
        {
            var scheme = ColorPresetManager.GetPreset(preset);
            
            // Convert SKColor to Avalonia Color
            TreeColor = Color.FromRgb(scheme.Tree.Red, scheme.Tree.Green, scheme.Tree.Blue);
            VacantColor = Color.FromRgb(scheme.Vacant.Red, scheme.Vacant.Green, scheme.Vacant.Blue);
            FireColor = Color.FromRgb(scheme.Fire.Red, scheme.Fire.Green, scheme.Fire.Blue);
            BurnoutColor = Color.FromRgb(scheme.Burnout.Red, scheme.Burnout.Green, scheme.Burnout.Blue);

            // Apply colors to renderer
            UpdateRendererColors();
        }
    }

    [RelayCommand]
    private void UpdateColors()
    {
        UpdateRendererColors();
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        try
        {
            var settings = CaptureSettings();
            settings.SaveDefaults();
            
            Debug.WriteLine($"✓ Settings saved to: {SimulationSettings.DefaultSettingsPath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SaveSettingsAsAsync()
    {
        if (StorageProvider == null) return;

        try
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Settings As",
                SuggestedFileName = "forest_fire_settings.json",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("JSON Settings")
                    {
                        Patterns = new[] { "*.json" }
                    },
                    new FilePickerFileType("All Files")
                    {
                        Patterns = new[] { "*" }
                    }
                }
            });

            if (file != null)
            {
                var settings = CaptureSettings();
                settings.SaveToFile(file.Path.LocalPath);
                
                Debug.WriteLine($"✓ Settings saved to: {file.Path.LocalPath}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task LoadSettingsAsync()
    {
        if (StorageProvider == null) return;

        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Load Settings",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("JSON Settings")
                    {
                        Patterns = new[] { "*.json" }
                    },
                    new FilePickerFileType("All Files")
                    {
                        Patterns = new[] { "*" }
                    }
                }
            });

            if (files.Count > 0)
            {
                bool wasRunning = _isSimulating;
                if (wasRunning)
                {
                    _isSimulating = false;
                    _timer.Stop();
                    IsRunning = false;
                }

                var settings = SimulationSettings.LoadFromFile(files[0].Path.LocalPath);
                ApplySettings(settings);
                
                UpdateSimulationImage();
                UpdateStats();
                OnPropertyChanged(nameof(ToggleButtonText));

                Debug.WriteLine($"✓ Settings loaded from: {files[0].Path.LocalPath}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load settings: {ex.Message}");
        }
    }

    [RelayCommand]
    private void RevertToDefaults()
    {
        try
        {
            bool wasRunning = _isSimulating;
            if (wasRunning)
            {
                _isSimulating = false;
                _timer.Stop();
                IsRunning = false;
            }

            // Create new default settings
            var defaults = new SimulationSettings();
            ApplySettings(defaults);
            
            UpdateSimulationImage();
            UpdateStats();
            OnPropertyChanged(nameof(ToggleButtonText));

            Debug.WriteLine("✓ Settings reset to factory defaults");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to revert to defaults: {ex.Message}");
        }
    }

    private void UpdateRendererColors()
    {
        // Convert Avalonia Color to SKColor
        _renderer.ColorTree = new SKColor(TreeColor.R, TreeColor.G, TreeColor.B);
        _renderer.ColorVacant = new SKColor(VacantColor.R, VacantColor.G, VacantColor.B);
        _renderer.ColorFireBase = new SKColor(FireColor.R, FireColor.G, FireColor.B);
        _renderer.ColorBurnout = new SKColor(BurnoutColor.R, BurnoutColor.G, BurnoutColor.B);
    }

    public void Dispose()
    {
        // Auto-save settings on exit
        try
        {
            var settings = CaptureSettings();
            settings.SaveDefaults();
            Debug.WriteLine("✓ Settings auto-saved on exit");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to auto-save settings: {ex.Message}");
        }

        _timer?.Stop();
        _renderer?.Dispose();
        _writeableBitmap?.Dispose();
    }
}
