using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TreeGrowth
{
    /// <summary>
    /// Holds all simulation and rendering settings for save/load functionality
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
        public double PFRatio { get; set; } = 10.0;
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

        // Colors (stored as ARGB integers for JSON serialization)
        public int ColorTreeArgb { get; set; } = Color.FromArgb(198, 164, 145).ToArgb();
        public int ColorVacantArgb { get; set; } = Color.FromArgb(169, 130, 104).ToArgb();
        public int ColorFireBaseArgb { get; set; } = Color.FromArgb(255, 200, 0).ToArgb();
        public int ColorBurnoutArgb { get; set; } = Color.FromArgb(255, 191, 0).ToArgb();

        // Bloom Settings
        public bool EnableBloom { get; set; } = false;
        public int BloomRadius { get; set; } = 2;
        public float BloomIntensity { get; set; } = 0.5f;
        public bool BloomFireOnly { get; set; } = true;

        // UI State
        public string Seed { get; set; } = string.Empty;
        public int NeighborhoodIndex { get; set; } = 0;
        public int GridSizeIndex { get; set; } = 0;
        public int CellSizeIndex { get; set; } = 0;
        public int PresetIndex { get; set; } = 0;

        [JsonIgnore]
        public Color ColorTree
        {
            get => Color.FromArgb(ColorTreeArgb);
            set => ColorTreeArgb = value.ToArgb();
        }

        [JsonIgnore]
        public Color ColorVacant
        {
            get => Color.FromArgb(ColorVacantArgb);
            set => ColorVacantArgb = value.ToArgb();
        }

        [JsonIgnore]
        public Color ColorFireBase
        {
            get => Color.FromArgb(ColorFireBaseArgb);
            set => ColorFireBaseArgb = value.ToArgb();
        }

        [JsonIgnore]
        public Color ColorBurnout
        {
            get => Color.FromArgb(ColorBurnoutArgb);
            set => ColorBurnoutArgb = value.ToArgb();
        }

        /// <summary>
        /// Gets the default settings file path in the application directory
        /// </summary>
        public static string DefaultSettingsPath => 
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "forest_fire_settings.json");

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
    }
}