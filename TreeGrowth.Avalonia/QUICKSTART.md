# Quick Start Guide

## Running the Application

### On Windows
```powershell
cd E:\Code\_Test\TreeGrowth2\TreeGrowth.Avalonia
dotnet run
```

### On macOS
```bash
cd /path/to/TreeGrowth2/TreeGrowth.Avalonia
dotnet run
```

### On Linux
```bash
cd /path/to/TreeGrowth2/TreeGrowth.Avalonia
dotnet run
```

## Creating a Standalone App

### For macOS (Apple Silicon M1/M2/M3)
```bash
dotnet publish -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true
```

The app will be in: `bin/Release/net9.0/osx-arm64/publish/`

### For macOS (Intel)
```bash
dotnet publish -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true
```

### For Windows
```powershell
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

### For Linux
```bash
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true
```

## Publishing Notes

The `--self-contained` flag includes the .NET runtime, so users don't need .NET installed.
The `-p:PublishSingleFile=true` flag creates a single executable file.

## Running the Published App

### macOS
```bash
cd bin/Release/net9.0/osx-arm64/publish/
./TreeGrowth.Avalonia
```

On first run on macOS, you may need to:
1. Right-click the app
2. Select "Open"
3. Click "Open" in the security dialog

Or from terminal:
```bash
xattr -cr ./TreeGrowth.Avalonia
chmod +x ./TreeGrowth.Avalonia
./TreeGrowth.Avalonia
```

### Windows
Double-click `TreeGrowth.Avalonia.exe` in the publish folder.

### Linux
```bash
chmod +x ./TreeGrowth.Avalonia
./TreeGrowth.Avalonia
```

## Troubleshooting

### "No compatible framework found" error
Make sure you're using the correct runtime identifier for your system:
- macOS M1/M2/M3: `osx-arm64`
- macOS Intel: `osx-x64`
- Windows: `win-x64`
- Linux: `linux-x64`

### App won't start on macOS
Run: `xattr -cr ./TreeGrowth.Avalonia` to remove quarantine attributes.

### Missing libraries on Linux
Install: `sudo apt-get install libfontconfig1 libice6 libsm6`

## Performance Tips

- Start with default parameters (p=0.01, f=0.000001)
- Increase Steps/Frame for faster simulation
- The simulation auto-scales speed based on grid size
- Press Start/Stop to toggle simulation
- Press Reset to reinitialize
