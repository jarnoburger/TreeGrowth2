using System;
using System.Collections.Generic;
using SkiaSharp;

namespace TreeGrowth.Avalonia.Core
{
    /// <summary>
    /// Manages color presets for the forest fire simulation
    /// </summary>
    public static class ColorPresetManager
    {
        /// <summary>
        /// Available color presets for the simulation
        /// </summary>
        public enum Preset
        {
            Warm,           // Original warm tones
            Atmosphere,     // Sky blue background
            Forest,         // Natural forest green
            Night,          // Dark mode with bright fire
            ANWB,           // ANWB brand colors
            Infrared,       // Heat map style
            Ocean,          // Blue-green palette
            Monochrome      // Black and white with orange fire
        }

        /// <summary>
        /// Color scheme containing all four simulation colors
        /// </summary>
        public record ColorScheme(SKColor Tree, SKColor Vacant, SKColor Fire, SKColor Burnout);

        /// <summary>
        /// Dictionary of all available color presets
        /// </summary>
        private static readonly Dictionary<Preset, ColorScheme> _presets = new()
        {
            { Preset.Warm, new ColorScheme(
                SKColor.Parse("#c6a491"),  // tree: warm beige
                SKColor.Parse("#A98268"),  // vacant: darker beige
                new SKColor(255, 200, 0),  // fire: orange-yellow
                new SKColor(255, 191, 0)   // burnout: amber glow
            )},
            { Preset.Atmosphere, new ColorScheme(
                SKColor.Parse("#2D5016"),  // tree: forest green
                SKColor.Parse("#87CEEB"),  // vacant: sky blue
                new SKColor(255, 100, 0),  // fire: deep orange
                new SKColor(255, 50, 0)    // burnout: red glow
            )},
            { Preset.Forest, new ColorScheme(
                SKColor.Parse("#228B22"),  // tree: forest green
                SKColor.Parse("#8B4513"),  // vacant: saddle brown (earth)
                new SKColor(255, 140, 0),  // fire: dark orange
                new SKColor(255, 69, 0)    // burnout: red-orange
            )},
            { Preset.Night, new ColorScheme(
                SKColor.Parse("#1a472a"),  // tree: dark green
                SKColor.Parse("#0d1117"),  // vacant: near black
                new SKColor(255, 200, 50), // fire: bright yellow
                new SKColor(255, 100, 0)   // burnout: orange
            )},
            { Preset.ANWB, new ColorScheme(
                SKColor.Parse("#FFD100"),  // tree: ANWB yellow
                SKColor.Parse("#003082"),  // vacant: ANWB blue
                new SKColor(255, 255, 255),// fire: white
                new SKColor(255, 200, 0)   // burnout: yellow glow
            )},
            { Preset.Infrared, new ColorScheme(
                SKColor.Parse("#00FF00"),  // tree: bright green (cold)
                SKColor.Parse("#000080"),  // vacant: navy (coldest)
                new SKColor(255, 0, 0),    // fire: red (hot)
                new SKColor(255, 255, 0)   // burnout: yellow (warm)
            )},
            { Preset.Ocean, new ColorScheme(
                SKColor.Parse("#20B2AA"),  // tree: light sea green
                SKColor.Parse("#191970"),  // vacant: midnight blue
                new SKColor(255, 127, 80), // fire: coral
                new SKColor(255, 99, 71)   // burnout: tomato
            )},
            { Preset.Monochrome, new ColorScheme(
                SKColor.Parse("#FFFFFF"),  // tree: white
                SKColor.Parse("#1a1a1a"),  // vacant: near black
                new SKColor(255, 140, 0),  // fire: orange
                new SKColor(200, 100, 0)   // burnout: dark orange
            )}
        };

        /// <summary>
        /// Gets the color scheme for a specific preset
        /// </summary>
        /// <param name="preset">The preset to retrieve</param>
        /// <returns>ColorScheme with all four colors</returns>
        public static ColorScheme GetPreset(Preset preset)
        {
            return _presets.TryGetValue(preset, out var colors) 
                ? colors 
                : _presets[Preset.Warm]; // Default to Warm if not found
        }

        /// <summary>
        /// Tries to get a color scheme for a specific preset
        /// </summary>
        /// <param name="preset">The preset to retrieve</param>
        /// <param name="colors">Output parameter with the color scheme</param>
        /// <returns>True if preset exists, false otherwise</returns>
        public static bool TryGetPreset(Preset preset, out ColorScheme colors)
        {
            return _presets.TryGetValue(preset, out colors!);
        }

        /// <summary>
        /// Gets all available preset names
        /// </summary>
        /// <returns>Array of preset names</returns>
        public static string[] GetPresetNames()
        {
            return Enum.GetNames<Preset>();
        }

        /// <summary>
        /// Gets the total number of available presets
        /// </summary>
        public static int PresetCount => _presets.Count;
    }
}
