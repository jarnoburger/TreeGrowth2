using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Media;

namespace TreeGrowth.Avalonia.Core
{
    /// <summary>
    /// Holds all simulation and rendering settings for save/load functionality
    /// Cross-platform compatible using Avalonia Color types
    /// </summary>
    public class SimulationSettings
    {
        // Grid Configuration
        public int OutputWidth { get; set; } = 1920;
        public int OutputHeight { get; set; } = 1080;
        public int CellSize { get; set; } = 1;

        // Simulation Parameters
        public double P { get; set; } = 0.01;
        public double F { get; set; } = 1e-5;
        public int BaseStepsPerFrame { get; set; } = 1000;
        public bool AnimateFires { get; set; } = true;
        public bool UseMooreNeighborhood { get; set; } = true;

        // Perlin Noise Distribution
        public bool UsePerlinDistribution { get; set; } = false;
        public double NoiseScale { get; set; } = 50.0;
        public int NoiseOctaves { get; set; } = 4;
        public double NoiseThreshold { get; set; } = 0.3;
        public double NoiseStrength { get; set; } = 1.0;

        // Visual Parameters
        public int BurnDecayFrames { get; set; } = 15;
        public int FireAnimationSpeed { get; set; } = 1;
        public int FireFlickerRange { get; set; } = 105;
        public int TargetFps { get; set; } = 60;

        // Colors (stored as ARGB uint32 for JSON serialization)
        public uint ColorTreeArgb { get; set; } = 0xFFC6A491;  // #C6A491
        public uint ColorVacantArgb { get; set; } = 0xFFA98268; // #A98268
        public uint ColorFireBaseArgb { get; set; } = 0xFFFFC800; // #FFC800
        public uint ColorBurnoutArgb { get; set; } = 0xFFFFBF00; // #FFBF00

        // Bloom Settings
        public bool EnableBloom { get; set; } = false;
        public int BloomRadius { get; set; } = 2;
        public float BloomIntensity { get; set; } = 0.5f;
        public bool BloomFireOnly { get; set; } = true;

        // Overlay Settings
        public bool ShowOverlay { get; set; } = false;
        public string OverlayFilePath { get; set; } = string.Empty;

        // UI State
        public string Seed { get; set; } = "default";
        public int SelectedPresetIndex { get; set; } = 0;

        /// <summary>
        /// Color conversion helpers (JsonIgnore to avoid serialization)
        /// </summary>
        [JsonIgnore]
        public Color ColorTree
        {
            get => ColorFromArgb(ColorTreeArgb);
            set => ColorTreeArgb = ColorToArgb(value);
        }

        [JsonIgnore]
        public Color ColorVacant
        {
            get => ColorFromArgb(ColorVacantArgb);
            set => ColorVacantArgb = ColorToArgb(value);
        }

        [JsonIgnore]
        public Color ColorFireBase
        {
            get => ColorFromArgb(ColorFireBaseArgb);
            set => ColorFireBaseArgb = ColorToArgb(value);
        }

        [JsonIgnore]
        public Color ColorBurnout
        {
            get => ColorFromArgb(ColorBurnoutArgb);
            set => ColorBurnoutArgb = ColorToArgb(value);
        }

        /// <summary>
        /// Gets the default settings file path in the user's app data directory (cross-platform)
        /// </summary>
        public static string DefaultSettingsPath
        {
            get
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string appFolder = Path.Combine(appDataPath, "TreeGrowth.Avalonia");
                Directory.CreateDirectory(appFolder); // Ensure directory exists
                return Path.Combine(appFolder, "settings.json");
            }
        }

        /// <summary>
        /// Saves settings to a JSON file
        /// </summary>
        public void SaveToFile(string filePath)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            string json = JsonSerializer.Serialize(this, options);
            
            // Ensure directory exists
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Loads settings from a JSON file
        /// </summary>
        public static SimulationSettings LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                return new SimulationSettings();

            string json = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Deserialize<SimulationSettings>(json, options) 
                   ?? new SimulationSettings();
        }

        /// <summary>
        /// Loads settings from the default location, or returns defaults if not found
        /// </summary>
        public static SimulationSettings LoadDefaults()
        {
            try
            {
                return LoadFromFile(DefaultSettingsPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
                return new SimulationSettings();
            }
        }

        /// <summary>
        /// Saves settings to the default location
        /// </summary>
        public void SaveDefaults()
        {
            try
            {
                SaveToFile(DefaultSettingsPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Converts Avalonia Color to ARGB uint32
        /// </summary>
        private static uint ColorToArgb(Color color)
        {
            return ((uint)color.A << 24) | ((uint)color.R << 16) | ((uint)color.G << 8) | color.B;
        }

        /// <summary>
        /// Converts ARGB uint32 to Avalonia Color
        /// </summary>
        private static Color ColorFromArgb(uint argb)
        {
            byte a = (byte)((argb >> 24) & 0xFF);
            byte r = (byte)((argb >> 16) & 0xFF);
            byte g = (byte)((argb >> 8) & 0xFF);
            byte b = (byte)(argb & 0xFF);
            return Color.FromArgb(a, r, g, b);
        }
    }
}
