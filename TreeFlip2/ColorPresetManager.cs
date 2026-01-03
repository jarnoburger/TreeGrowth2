using System;
using System.Collections.Generic;
using System.Drawing;

namespace TreeGrowth
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
        public record ColorScheme(Color Tree, Color Vacant, Color Fire, Color Burnout);

        /// <summary>
        /// Dictionary of all available color presets
        /// </summary>
        private static readonly Dictionary<Preset, ColorScheme> _presets = new()
        {
            { Preset.Warm, new ColorScheme(
                ColorTranslator.FromHtml("#c6a491"),  // tree: warm beige
                ColorTranslator.FromHtml("#A98268"),  // vacant: darker beige
                Color.FromArgb(255, 200, 0),          // fire: orange-yellow
                Color.FromArgb(255, 191, 0)           // burnout: amber glow
            )},
            { Preset.Atmosphere, new ColorScheme(
                ColorTranslator.FromHtml("#2D5016"),  // tree: forest green
                ColorTranslator.FromHtml("#87CEEB"),  // vacant: sky blue
                Color.FromArgb(255, 100, 0),          // fire: deep orange
                Color.FromArgb(255, 50, 0)            // burnout: red glow
            )},
            { Preset.Forest, new ColorScheme(
                ColorTranslator.FromHtml("#228B22"),  // tree: forest green
                ColorTranslator.FromHtml("#8B4513"),  // vacant: saddle brown (earth)
                Color.FromArgb(255, 140, 0),          // fire: dark orange
                Color.FromArgb(255, 69, 0)            // burnout: red-orange
            )},
            { Preset.Night, new ColorScheme(
                ColorTranslator.FromHtml("#1a472a"),  // tree: dark green
                ColorTranslator.FromHtml("#0d1117"),  // vacant: near black
                Color.FromArgb(255, 200, 50),         // fire: bright yellow
                Color.FromArgb(255, 100, 0)           // burnout: orange
            )},
            { Preset.ANWB, new ColorScheme(
                ColorTranslator.FromHtml("#FFD100"),  // tree: ANWB yellow
                ColorTranslator.FromHtml("#003082"),  // vacant: ANWB blue
                Color.FromArgb(255, 255, 255),        // fire: white
                Color.FromArgb(255, 200, 0)           // burnout: yellow glow
            )},
            { Preset.Infrared, new ColorScheme(
                ColorTranslator.FromHtml("#00FF00"),  // tree: bright green (cold)
                ColorTranslator.FromHtml("#000080"),  // vacant: navy (coldest)
                Color.FromArgb(255, 0, 0),            // fire: red (hot)
                Color.FromArgb(255, 255, 0)           // burnout: yellow (warm)
            )},
            { Preset.Ocean, new ColorScheme(
                ColorTranslator.FromHtml("#20B2AA"),  // tree: light sea green
                ColorTranslator.FromHtml("#191970"),  // vacant: midnight blue
                Color.FromArgb(255, 127, 80),         // fire: coral
                Color.FromArgb(255, 99, 71)           // burnout: tomato
            )},
            { Preset.Monochrome, new ColorScheme(
                ColorTranslator.FromHtml("#FFFFFF"),  // tree: white
                ColorTranslator.FromHtml("#1a1a1a"),  // vacant: near black
                Color.FromArgb(255, 140, 0),          // fire: orange
                Color.FromArgb(200, 100, 0)           // burnout: dark orange
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
            return _presets.TryGetValue(preset, out colors);
        }

        /// <summary>
        /// Gets all available preset names
        /// </summary>
        /// <returns>Array of preset names</returns>
        public static string[] GetPresetNames()
        {
            return Enum.GetNames(typeof(Preset));
        }

        /// <summary>
        /// Gets the total number of available presets
        /// </summary>
        public static int PresetCount => _presets.Count;
    }
}