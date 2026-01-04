using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
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

    public MainWindowViewModel()
    {
        // Initialize color preset names
        ColorPresetNames = ColorPresetManager.GetPresetNames().ToList();

        // Initialize simulation and renderer
        int width = 1920;
        int height = 1080;
        int cellSize = 1;

        _simulation = new ForestFireSimulation(width, height, cellSize, Environment.ProcessorCount);
        _renderer = new ForestFireRenderer(width, height, cellSize, Environment.ProcessorCount);

        _simulation.P = _treeGrowth;
        _simulation.F = _lightning;
        _simulation.BaseStepsPerFrame = _stepsPerFrame;
        _simulation.Initialize(_seed);

        // Create WriteableBitmap for Avalonia
        _writeableBitmap = new WriteableBitmap(
            new global::Avalonia.PixelSize(width, height),
            new global::Avalonia.Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);
        
        SimulationImage = _writeableBitmap;

        // Create timer for animation
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
        };
        _timer.Tick += OnTimerTick;

        // Initial render
        UpdateSimulationImage();
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
        
        StatsText = $"Trees: {TreeCount:n0} ({Density:0.0}%) | Fires: {TotalFires:n0} | Steps: {Timesteps:n0}";
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
    }

    [RelayCommand]
    private void UpdateParameters()
    {
        _simulation.P = TreeGrowth;
        _simulation.F = Lightning;
        _simulation.BaseStepsPerFrame = StepsPerFrame;
        
        // Update Perlin Noise settings
        _simulation.UsePerlinDistribution = UsePerlinNoise;
        _simulation.NoiseScale = NoiseScale;
        _simulation.NoiseOctaves = NoiseOctaves;
        _simulation.NoiseThreshold = NoiseThreshold;
        _simulation.NoiseStrength = NoiseStrength;
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
        _timer?.Stop();
        _renderer?.Dispose();
        _writeableBitmap?.Dispose();
    }
}
