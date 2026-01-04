using System;
using System.Runtime.CompilerServices;

namespace TreeGrowth.Avalonia.Core
{
    /// <summary>
    /// 2D Perlin Noise generator for spatial distribution patterns
    /// Creates smooth, organic-looking variations in tree density
    /// </summary>
    public class PerlinNoise
    {
        private readonly int[] _permutation;
        private readonly double _scale;
        private readonly int _octaves;
        private readonly double _persistence;
        private readonly double _lacunarity;

        /// <summary>
        /// Creates a new Perlin noise generator
        /// </summary>
        /// <param name="seed">Seed for reproducible patterns</param>
        /// <param name="scale">Scale of noise features (higher = larger patches)</param>
        /// <param name="octaves">Number of noise layers (more = more detail)</param>
        /// <param name="persistence">Amplitude decrease per octave (0.5 = half)</param>
        /// <param name="lacunarity">Frequency increase per octave (2.0 = double)</param>
        public PerlinNoise(int seed = 0, double scale = 50.0, int octaves = 4, double persistence = 0.5, double lacunarity = 2.0)
        {
            _scale = Math.Max(0.001, scale);
            _octaves = Math.Max(1, octaves);
            _persistence = persistence;
            _lacunarity = lacunarity;

            // Create permutation table based on seed
            _permutation = new int[512];
            var random = new Random(seed);
            
            int[] p = new int[256];
            for (int i = 0; i < 256; i++)
                p[i] = i;

            // Shuffle using Fisher-Yates
            for (int i = 255; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (p[i], p[j]) = (p[j], p[i]);
            }

            // Duplicate permutation table
            for (int i = 0; i < 512; i++)
                _permutation[i] = p[i & 255];
        }

        /// <summary>
        /// Gets noise value at given coordinates (returns value in range [0, 1])
        /// </summary>
        public double GetValue(double x, double y)
        {
            double total = 0;
            double frequency = 1.0;
            double amplitude = 1.0;
            double maxValue = 0;

            for (int i = 0; i < _octaves; i++)
            {
                double sampleX = x / _scale * frequency;
                double sampleY = y / _scale * frequency;

                double perlinValue = PerlinRaw(sampleX, sampleY);
                total += perlinValue * amplitude;

                maxValue += amplitude;
                amplitude *= _persistence;
                frequency *= _lacunarity;
            }

            // Normalize to [0, 1]
            return (total / maxValue + 1.0) * 0.5;
        }

        /// <summary>
        /// Raw Perlin noise function (returns value in range [-1, 1])
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double PerlinRaw(double x, double y)
        {
            // Find unit grid cell containing point
            int xi = (int)Math.Floor(x) & 255;
            int yi = (int)Math.Floor(y) & 255;

            // Get relative xy coordinates within cell
            double xf = x - Math.Floor(x);
            double yf = y - Math.Floor(y);

            // Compute fade curves
            double u = Fade(xf);
            double v = Fade(yf);

            // Hash coordinates of the 4 cube corners
            int aa = _permutation[_permutation[xi] + yi];
            int ab = _permutation[_permutation[xi] + yi + 1];
            int ba = _permutation[_permutation[xi + 1] + yi];
            int bb = _permutation[_permutation[xi + 1] + yi + 1];

            // Blend results from the 4 corners
            double x1 = Lerp(Grad(aa, xf, yf), Grad(ba, xf - 1, yf), u);
            double x2 = Lerp(Grad(ab, xf, yf - 1), Grad(bb, xf - 1, yf - 1), u);

            return Lerp(x1, x2, v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double Fade(double t)
        {
            // 6t^5 - 15t^4 + 10t^3 (smoothstep function)
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double Lerp(double a, double b, double t)
        {
            return a + t * (b - a);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double Grad(int hash, double x, double y)
        {
            // Convert hash to gradient vector
            int h = hash & 7;
            double u = h < 4 ? x : y;
            double v = h < 4 ? y : x;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }

        /// <summary>
        /// Pre-generates a noise map for efficient repeated sampling
        /// </summary>
        public double[,] GenerateNoiseMap(int width, int height)
        {
            var map = new double[height, width];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    map[y, x] = GetValue(x, y);
                }
            }

            return map;
        }
    }
}
