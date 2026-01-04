# ?? Color Customization Guide

## ? **Implementation Complete!**

Color presets and custom color picker functionality have been successfully added to TreeGrowth.Avalonia.

---

## ?? Features Added

### 1. **Color Presets** ??
8 pre-configured color schemes for instant visual variety:

| Preset | Description | Style |
|--------|-------------|-------|
| **Warm** | Original warm beige tones | Default, natural |
| **Atmosphere** | Sky blue background with forest green trees | Aerial view |
| **Forest** | Natural forest green and earth brown | Realistic forest |
| **Night** | Dark mode with bright fire | Low light |
| **ANWB** | ANWB brand colors (yellow/blue) | Branded |
| **Infrared** | Heat map style visualization | Scientific |
| **Ocean** | Blue-green aquatic palette | Underwater theme |
| **Monochrome** | Black and white with orange fire | High contrast |

### 2. **Custom Color Pickers** ???
Individual color pickers for each simulation element:
- **Tree Color** - Color of living trees
- **Vacant Color** - Background/empty space color
- **Fire Color** - Active burning fire color
- **Burnout Color** - Recently burned areas color

---

## ?? How to Use

### Apply a Color Preset

1. **Find the "?? Colors" section** in the control panel
2. **Select a preset** from the dropdown menu
3. **Click "Apply Preset"** button
4. Colors update instantly in the simulation

### Customize Individual Colors

1. **Click on a color picker** (below each color preview)
2. **Choose your color** from the picker
3. **Click "Apply Colors"** to update the simulation
4. Changes apply in real-time

### Workflow Tips

? **Start with a preset** to get a cohesive color scheme  
? **Fine-tune individual colors** to match your preference  
? **Preview colors** in the color bars before applying  
? **Experiment freely** - changes are reversible  

---

## ?? Color Preset Details

### ?? **Warm** (Default)
```
Tree:    #c6a491 (Warm beige)
Vacant:  #A98268 (Darker beige)
Fire:    #FFC800 (Orange-yellow)
Burnout: #FFBF00 (Amber glow)
```
**Use Case:** General purpose, natural appearance

### ?? **Atmosphere**
```
Tree:    #2D5016 (Forest green)
Vacant:  #87CEEB (Sky blue)
Fire:    #FF6400 (Deep orange)
Burnout: #FF3200 (Red glow)
```
**Use Case:** Aerial/satellite view simulation

### ?? **Forest**
```
Tree:    #228B22 (Forest green)
Vacant:  #8B4513 (Saddle brown)
Fire:    #FF8C00 (Dark orange)
Burnout: #FF4500 (Red-orange)
```
**Use Case:** Realistic forest fire visualization

### ?? **Night**
```
Tree:    #1a472a (Dark green)
Vacant:  #0d1117 (Near black)
Fire:    #FFC832 (Bright yellow)
Burnout: #FF6400 (Orange)
```
**Use Case:** Low-light/nighttime simulation

### ?? **ANWB**
```
Tree:    #FFD100 (ANWB yellow)
Vacant:  #003082 (ANWB blue)
Fire:    #FFFFFF (White)
Burnout: #FFC800 (Yellow glow)
```
**Use Case:** Branded/corporate presentations

### ??? **Infrared**
```
Tree:    #00FF00 (Bright green - cold)
Vacant:  #000080 (Navy - coldest)
Fire:    #FF0000 (Red - hot)
Burnout: #FFFF00 (Yellow - warm)
```
**Use Case:** Scientific heat visualization

### ?? **Ocean**
```
Tree:    #20B2AA (Light sea green)
Vacant:  #191970 (Midnight blue)
Fire:    #FF7F50 (Coral)
Burnout: #FF6347 (Tomato)
```
**Use Case:** Aquatic/alternative theme

### ? **Monochrome**
```
Tree:    #FFFFFF (White)
Vacant:  #1a1a1a (Near black)
Fire:    #FF8C00 (Orange)
Burnout: #C86400 (Dark orange)
```
**Use Case:** High contrast, accessibility

---

## ?? Technical Implementation

### Files Added
1. `Core/ColorPresetManager.cs` - Manages 8 color presets

### Files Modified
1. `ViewModels/MainWindowViewModel.cs` - Added color properties and commands
2. `Views/MainWindow.axaml` - Added color UI section

### Key Components

#### ColorPresetManager
```csharp
public static class ColorPresetManager
{
    public enum Preset { Warm, Atmosphere, Forest, Night, ANWB, Infrared, Ocean, Monochrome }
    public record ColorScheme(SKColor Tree, SKColor Vacant, SKColor Fire, SKColor Burnout);
    public static ColorScheme GetPreset(Preset preset);
}
```

#### ViewModel Properties
```csharp
[ObservableProperty] private Color _treeColor;
[ObservableProperty] private Color _vacantColor;
[ObservableProperty] private Color _fireColor;
[ObservableProperty] private Color _burnoutColor;
[ObservableProperty] private int _selectedPresetIndex;
public List<string> ColorPresetNames { get; }
```

#### Commands
- `ApplyColorPresetCommand` - Applies selected preset
- `UpdateColorsCommand` - Applies custom colors

---

## ?? Quick Actions

### Reset to Default Colors
1. Select **"Warm"** preset
2. Click **"Apply Preset"**

### Create High Contrast View
1. Select **"Monochrome"** preset
2. Click **"Apply Preset"**

### Experiment with Custom Colors
1. Start with any preset
2. Adjust individual colors using pickers
3. Click **"Apply Colors"**
4. If unsure, reapply original preset

---

## ?? Integration with Other Features

### Works With:
? **Perlin Noise** - Colors adapt to organic patterns  
? **Bloom Effects** - Fire colors glow realistically  
? **All Simulation Parameters** - Colors are independent  
? **Real-time Updates** - Change colors while running  

### Color Tips:
- ?? **Bright fire colors** work best with bloom enabled
- ?? **Contrasting tree/vacant colors** improve visibility
- ?? **Saturated colors** look better at lower densities
- ?? **Dark vacant colors** emphasize tree patterns

---

## ?? Color Psychology

| Color Scheme | Emotional Impact | Best For |
|--------------|------------------|----------|
| Warm | Comfortable, natural | General demonstrations |
| Atmosphere | Open, expansive | Presentations |
| Forest | Realistic, grounded | Educational content |
| Night | Dramatic, intense | Videos/screenshots |
| ANWB | Professional, branded | Corporate use |
| Infrared | Scientific, analytical | Research |
| Ocean | Calm, alternative | Creative projects |
| Monochrome | Clear, accessible | Print/accessibility |

---

## ?? Troubleshooting

**Colors don't update after clicking "Apply Colors"?**
- Make sure you clicked the button
- Try reselecting the preset

**Color picker not showing?**
- Ensure you're using Avalonia 11.0+
- Check if `ColorPicker` control is available

**Preset looks different than expected?**
- Check your monitor color calibration
- Bloom effects can alter perceived colors

---

## ? Future Enhancements (Optional)

Potential additions:
- ?? Save custom color schemes
- ?? Export/import color palettes
- ?? More presets (Sunset, Desert, Arctic, etc.)
- ?? Random color generator
- ?? Gradient fire effects

---

## ?? Success!

You now have **full color customization** with:
- ? 8 beautiful presets
- ? Individual color pickers
- ? Real-time updates
- ? Easy preset switching
- ? Cross-platform compatibility

**Enjoy creating beautiful forest fire visualizations!** ??????
