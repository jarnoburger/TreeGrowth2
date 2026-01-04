# TreeGrowth.Avalonia - Cross-Platform Forest Fire Simulation

A high-performance forest fire simulation using the Drossel-Schwabl cellular automaton model, built with **Avalonia UI** and **SkiaSharp** for true cross-platform compatibility.

## ? Features

- **Cross-Platform**: Runs on Windows, macOS (Intel & Apple Silicon), and Linux
- **High Performance**: Optimized rendering with SkiaSharp and unsafe code
- **Real-Time Simulation**: Interactive forest fire propagation with configurable parameters
- **Modern UI**: Built with Avalonia MVVM architecture
- **No Windows Dependencies**: Uses SkiaSharp instead of System.Drawing

## ?? Getting Started

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

- **Start/Stop Button**: Toggle simulation
- **Reset Button**: Reinitialize with current parameters
- **Tree Growth (p)**: Probability of tree growth per timestep
- **Lightning (f)**: Probability of lightning strike per timestep
- **Steps/Frame**: Simulation speed (auto-scaled by grid size)
- **Seed**: Random seed for reproducible simulations

## ??? Architecture

### Core Components

- **`ForestFireSimulation.cs`**: Platform-independent simulation engine
- **`ForestFireRenderer.cs`**: SkiaSharp-based high-performance renderer
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

## ?? Simulation Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| Tree Growth (p) | 0.01 | Probability a vacant cell becomes a tree |
| Lightning (f) | 0.000001 | Probability a tree is struck by lightning |
| Steps/Frame | 1000 | Simulation timesteps per render frame |
| Cell Size | 1 | Logical cell size in pixels |

## ?? About the Model

This implements the **Drossel-Schwabl forest fire model**, a cellular automaton that exhibits self-organized criticality. The model demonstrates:

- Power-law distribution of fire sizes
- Emergent complex behavior from simple rules
- Self-organization to a critical state
- Scale-invariant dynamics

## ?? Contributing

This is a demonstration project showing how to migrate Windows Forms applications to cross-platform Avalonia. Feel free to:

- Add new features (bloom effects, overlays, etc.)
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
