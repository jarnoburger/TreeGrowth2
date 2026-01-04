# TreeGrowth.Avalonia - Cross-Platform Forest Fire Simulation

A high-performance forest fire simulation using the Drossel-Schwabl cellular automaton model, built with **Avalonia UI** and **SkiaSharp** for true cross-platform compatibility.

## ?? Features

- **Cross-Platform**: Runs on Windows, macOS (Intel & Apple Silicon), and Linux
- **High Performance**: Optimized rendering with SkiaSharp and unsafe code
- **Real-Time Simulation**: Interactive forest fire propagation with configurable parameters
- **Modern UI**: Built with Avalonia MVVM architecture
- **No Windows Dependencies**: Uses SkiaSharp instead of System.Drawing
- **Settings Persistence**: Save/load configurations with keyboard shortcuts
- **Overlay Support**: Load PNG/JPG images with alpha blending
- **Perlin Noise Distribution**: Natural-looking tree density patterns
- **Color Presets**: Pre-defined color schemes
- **Bloom Effects**: Optional glow effects for fires

## ?? Quick Start

### Prerequisites

- .NET 9.0 SDK or later
- For macOS: No additional dependencies needed!
- For Linux: `libSkiaSharp` (usually auto-installed)

### Building

```bash
cd TreeGrowth.Avalonia
dotnet restore
dotnet build
```

### Running

```bash
dotnet run
```

## ?? Publishing for Different Platforms

### Windows (x64)
```bash
dotnet publish -c Release -r win-x64 --self-contained
```

### macOS (Apple Silicon)
```bash
dotnet publish -c Release -r osx-arm64 --self-contained
```

### macOS (Intel)
```bash
dotnet publish -c Release -r osx-x64 --self-contained
```

### Linux (x64)
```bash
dotnet publish -c Release -r linux-x64 --self-contained
```

## ?? Controls

### Keyboard Shortcuts
- **Space** - Start/Stop simulation
- **Ctrl+S** - Save settings
- **Ctrl+Shift+S** - Save settings as...
- **Ctrl+O** - Load settings
- **Alt+F4** - Exit (with auto-save)

### Mouse
- Click and drag sliders to adjust parameters
- Use menu bar for file operations

## ?? Features Documentation

### ??? Overlay Images
Load transparent PNG images to overlay on the simulation. Perfect for logos, watermarks, or visual effects.

**See:** [OVERLAY_IMPLEMENTATION.md](OVERLAY_IMPLEMENTATION.md)

### ?? Settings Persistence
Save and load your favorite configurations. Settings auto-save on exit and auto-load on startup.

**See:** [SETTINGS_PERSISTENCE.md](SETTINGS_PERSISTENCE.md)

## ?? Simulation Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| Tree Growth (p) | 0.01 | Probability a vacant cell becomes a tree |
| Lightning (f) | 0.000001 | Probability a tree is struck by lightning |
| Steps/Frame | 1000 | Simulation timesteps per render frame |
| Cell Size | 1 | Logical cell size in pixels |

## ?? Perlin Noise Distribution

Create natural-looking forest patterns with Perlin noise:

- **Scale**: Size of noise features (larger = bigger patches)
- **Octaves**: Detail levels (more = finer detail)
- **Threshold**: Minimum density for tree growth
- **Strength**: How much noise affects distribution

## ?? Color Presets

Choose from built-in color schemes:
- Classic (default brown/orange)
- Forest (green/red)
- Winter (white/blue)
- Volcanic (black/red)
- Ocean (blue/cyan)

## ??? Architecture

### Core Components

- **`ForestFireSimulation.cs`**: Platform-independent simulation engine
- **`ForestFireRenderer.cs`**: SkiaSharp-based high-performance renderer
- **`SimulationSettings.cs`**: Settings persistence and serialization
- **`PerlinNoise.cs`**: 2D noise generator for spatial patterns
- **`ColorPresetManager.cs`**: Pre-defined color schemes
- **`MainWindowViewModel.cs`**: MVVM ViewModel with CommunityToolkit.Mvvm
- **`MainWindow.axaml`**: Avalonia XAML UI definition

### Key Technologies

- **Avalonia UI 11.3**: Cross-platform XAML framework
- **SkiaSharp 2.88**: High-performance 2D graphics
- **CommunityToolkit.Mvvm 8.2**: Modern MVVM helpers
- **.NET 9.0**: Latest .NET runtime

## ?? Performance Optimizations

1. **Batched Parallel Rendering**: Processes rows in batches to reduce overhead
2. **Unsafe Pointer Operations**: Direct memory access for pixel manipulation
3. **Bit-Shift Division**: Fast coordinate mapping for power-of-2 cell sizes
4. **Thread-Local RNG**: Lock-free random number generation
5. **Pre-allocated Buffers**: Zero-allocation rendering loop
6. **Scaled Overlay Caching**: Avoids repeated image resizing

## ?? About the Model

This implements the **Drossel-Schwabl forest fire model**, a cellular automaton that exhibits self-organized criticality. The model demonstrates:

- Power-law distribution of fire sizes
- Emergent complex behavior from simple rules
- Self-organization to a critical state
- Scale-invariant dynamics

## ?? Contributing

This is a demonstration project showing how to migrate Windows Forms applications to cross-platform Avalonia. Feel free to:

- Add new features (additional effects, analytics, etc.)
- Improve performance
- Enhance the UI
- Add more simulation modes

## ?? License

This project demonstrates cross-platform .NET development patterns.

## ?? Acknowledgments

- Based on the Drossel-Schwabl forest fire model
- Original Windows Forms implementation in TreeFlip2 project
- Built with Avalonia UI framework
- Rendering powered by SkiaSharp

## ?? Related Projects

- **TreeFlip2**: Original Windows-only version with Windows Forms
- **TreeGrowth**: Legacy implementation
- **TreeFlip**: Alternative implementation

---

**Ready for macOS!** ?? Just run `dotnet publish -r osx-arm64` and deploy!

**Ready for Linux!** ?? Just run `dotnet publish -r linux-x64` and deploy!

**Ready for Windows!** ?? Just run `dotnet publish -r win-x64` and deploy!
