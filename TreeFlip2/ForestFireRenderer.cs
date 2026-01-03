using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace TreeGrowth
{
    /// <summary>
    /// Handles rendering of the forest fire simulation to bitmaps
    /// Supports cell scaling, bloom effects, and various color schemes
    /// </summary>
    public class ForestFireRenderer : IDisposable
    {
        // ============================================================
        // === CONFIGURATION ===
        // ============================================================

        private readonly int _outputWidth;
        private readonly int _outputHeight;
        private readonly int _cellSize;
        private readonly ParallelOptions _parallelOptions;

        // ============================================================
        // === COLOR CONFIGURATION ===
        // ============================================================

        public Color ColorTree { get; set; } = ColorTranslator.FromHtml("#c6a491");
        public Color ColorVacant { get; set; } = ColorTranslator.FromHtml("#A98268");
        public Color ColorFireBase { get; set; } = Color.FromArgb(255, 200, 0);
        public Color ColorBurnout { get; set; } = Color.FromArgb(255, 191, 0);
        public int FireFlickerRange { get; set; } = 105;

        // ============================================================
        // === BLOOM/BLUR EFFECT ===
        // ============================================================

        public bool EnableBloom { get; set; } = false;
        public int BloomRadius { get; set; } = 2;
        public float BloomIntensity { get; set; } = 0.5f;
        public bool BloomFireOnly { get; set; } = true;

        // ============================================================
        // === BUFFERS ===
        // ============================================================

        private readonly byte[] _pixelBuffer;
        private readonly byte[] _blurBuffer;
        private float[] _bloomKernel;
        private readonly Bitmap _bitmap;

        // Thread-local RNG for flicker effects
        private readonly ThreadLocal<ForestFireSimulation.XorShift128Plus> _threadRng;

        // Performance tracking
        public long LastDrawMs { get; private set; }
        public long LastBloomMs { get; private set; }

        // ============================================================
        // === CONSTRUCTOR ===
        // ============================================================

        /// <summary>
        /// Creates a new forest fire renderer
        /// </summary>
        /// <param name="outputWidth">Output width in pixels</param>
        /// <param name="outputHeight">Output height in pixels</param>
        /// <param name="cellSize">Logical cell size in pixels</param>
        /// <param name="maxDegreeOfParallelism">Max parallel threads</param>
        public ForestFireRenderer(int outputWidth, int outputHeight, int cellSize, int maxDegreeOfParallelism)
        {
            _outputWidth = outputWidth;
            _outputHeight = outputHeight;
            _cellSize = cellSize;

            _pixelBuffer = new byte[outputHeight * outputWidth * 4];
            _blurBuffer = new byte[outputHeight * outputWidth * 4];
            _bitmap = new Bitmap(outputWidth, outputHeight, PixelFormat.Format32bppArgb);

            _parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism
            };

            _threadRng = new ThreadLocal<ForestFireSimulation.XorShift128Plus>(
                () => ForestFireSimulation.XorShift128Plus.FromString($"thread_{Environment.CurrentManagedThreadId}_{DateTime.Now.Ticks}"),
                trackAllValues: false
            );

            InitializeBloomKernel();
        }

        // ============================================================
        // === BLOOM KERNEL ===
        // ============================================================

        private void InitializeBloomKernel()
        {
            int size = BloomRadius * 2 + 1;
            _bloomKernel = new float[size];
            float sigma = Math.Max(1.0f, BloomRadius / 2.0f);
            float sum = 0;

            for (int i = 0; i < size; i++)
            {
                int x = i - BloomRadius;
                _bloomKernel[i] = (float)Math.Exp(-(x * x) / (2 * sigma * sigma));
                sum += _bloomKernel[i];
            }

            // Normalize
            for (int i = 0; i < size; i++)
                _bloomKernel[i] /= sum;
        }

        /// <summary>
        /// Updates the bloom kernel when radius changes
        /// </summary>
        public void UpdateBloomKernel()
        {
            InitializeBloomKernel();
        }

        // ============================================================
        // === MAIN RENDER METHOD ===
        // ============================================================

        /// <summary>
        /// Renders the simulation grid to a bitmap
        /// </summary>
        /// <param name="simulation">The simulation to render</param>
        /// <returns>The rendered bitmap</returns>
        public Bitmap Render(ForestFireSimulation simulation)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            int outputW = _outputWidth;
            int outputH = _outputHeight;
            int logicalW = simulation.LogicalWidth;
            int logicalH = simulation.LogicalHeight;
            int cellSize = _cellSize;
            int[] grid = simulation.Grid;
            byte[] buffer = _pixelBuffer;

            // Pre-compute colors
            uint colorTreeBgra = ColorToBgra(ColorTree);
            uint colorVacantBgra = ColorToBgra(ColorVacant);
            byte fireR = ColorFireBase.R;
            byte fireG = ColorFireBase.G;
            byte fireB = ColorFireBase.B;
            byte burnR = ColorBurnout.R;
            byte burnG = ColorBurnout.G;
            byte burnB = ColorBurnout.B;
            int flickerRange = FireFlickerRange;
            int burnDecay = simulation.BurnDecayFrames;

            // === RENDER CELLS WITH SCALING ===
            Parallel.For(0, outputH, _parallelOptions, outputY =>
            {
                var localRng = _threadRng.Value;

                // Map output Y to logical Y
                int logicalY = outputY / cellSize;
                if (logicalY >= logicalH) logicalY = logicalH - 1;

                int rowStartBuffer = outputY * outputW * 4;

                for (int outputX = 0; outputX < outputW; outputX++)
                {
                    // Map output X to logical X
                    int logicalX = outputX / cellSize;
                    if (logicalX >= logicalW) logicalX = logicalW - 1;

                    int state = grid[logicalY * logicalW + logicalX];
                    int bufferIdx = rowStartBuffer + outputX * 4;
                    byte r, g, b;

                    if (state == ForestFireSimulation.BURNING)
                    {
                        r = fireR;
                        g = (byte)Math.Min(255, fireG + (flickerRange > 0 ? localRng.NextInt(flickerRange) : 0));
                        b = fireB;
                    }
                    else if (state <= ForestFireSimulation.RECENTLY_BURNED_STATE)
                    {
                        double decayProgress = (state - ForestFireSimulation.RECENTLY_BURNED_STATE + 1) / (double)burnDecay;
                        double intensity = 1.0 - decayProgress;
                        r = (byte)(burnR * intensity);
                        g = (byte)(burnG * intensity);
                        b = (byte)(burnB * intensity);
                    }
                    else if (state == ForestFireSimulation.TREE)
                    {
                        buffer[bufferIdx + 0] = (byte)(colorTreeBgra);
                        buffer[bufferIdx + 1] = (byte)(colorTreeBgra >> 8);
                        buffer[bufferIdx + 2] = (byte)(colorTreeBgra >> 16);
                        buffer[bufferIdx + 3] = 255;
                        continue;
                    }
                    else // VACANT
                    {
                        buffer[bufferIdx + 0] = (byte)(colorVacantBgra);
                        buffer[bufferIdx + 1] = (byte)(colorVacantBgra >> 8);
                        buffer[bufferIdx + 2] = (byte)(colorVacantBgra >> 16);
                        buffer[bufferIdx + 3] = 255;
                        continue;
                    }

                    buffer[bufferIdx + 0] = b;
                    buffer[bufferIdx + 1] = g;
                    buffer[bufferIdx + 2] = r;
                    buffer[bufferIdx + 3] = 255;
                }
            });

            LastDrawMs = sw.ElapsedMilliseconds;

            // === APPLY BLOOM IF ENABLED ===
            byte[] finalBuffer = buffer;
            if (EnableBloom && BloomRadius > 0)
            {
                var bloomSw = System.Diagnostics.Stopwatch.StartNew();
                ApplyBloom(buffer, _blurBuffer, outputW, outputH);
                LastBloomMs = bloomSw.ElapsedMilliseconds;
                finalBuffer = _blurBuffer;
            }

            // Copy buffer to bitmap
            CopyBufferToBitmap(finalBuffer);

            return _bitmap;
        }

        /// <summary>
        /// Gets the pixel buffer for NDI streaming (after bloom if enabled)
        /// </summary>
        public byte[] GetPixelBuffer()
        {
            return EnableBloom ? _blurBuffer : _pixelBuffer;
        }

        // ============================================================
        // === BLOOM/BLUR EFFECT ===
        // ============================================================

        private void ApplyBloom(byte[] src, byte[] dst, int width, int height)
        {
            // Two-pass separable Gaussian blur for performance
            byte[] temp = new byte[src.Length];
            float intensity = BloomIntensity;
            int radius = BloomRadius;
            float[] kernel = _bloomKernel;
            int kernelSize = kernel.Length;

            // Horizontal pass
            Parallel.For(0, height, _parallelOptions, y =>
            {
                int rowStart = y * width * 4;

                for (int x = 0; x < width; x++)
                {
                    int idx = rowStart + x * 4;

                    // Check if this pixel should be bloomed
                    if (BloomFireOnly)
                    {
                        byte srcR = src[idx + 2];
                        byte srcG = src[idx + 1];
                        byte srcB = src[idx + 0];

                        // Simple check: is this close to tree or vacant color?
                        bool isTree = Math.Abs(srcR - ColorTree.R) < 20 &&
                                     Math.Abs(srcG - ColorTree.G) < 20 &&
                                     Math.Abs(srcB - ColorTree.B) < 20;
                        bool isVacant = Math.Abs(srcR - ColorVacant.R) < 20 &&
                                       Math.Abs(srcG - ColorVacant.G) < 20 &&
                                       Math.Abs(srcB - ColorVacant.B) < 20;

                        if (isTree || isVacant)
                        {
                            // Copy without blur
                            temp[idx + 0] = src[idx + 0];
                            temp[idx + 1] = src[idx + 1];
                            temp[idx + 2] = src[idx + 2];
                            temp[idx + 3] = 255;
                            continue;
                        }
                    }

                    float sumB = 0, sumG = 0, sumR = 0;

                    for (int k = 0; k < kernelSize; k++)
                    {
                        int sx = x + k - radius;
                        if (sx < 0) sx = 0;
                        if (sx >= width) sx = width - 1;

                        int sIdx = rowStart + sx * 4;
                        float w = kernel[k];
                        sumB += src[sIdx + 0] * w;
                        sumG += src[sIdx + 1] * w;
                        sumR += src[sIdx + 2] * w;
                    }

                    temp[idx + 0] = (byte)Math.Min(255, sumB);
                    temp[idx + 1] = (byte)Math.Min(255, sumG);
                    temp[idx + 2] = (byte)Math.Min(255, sumR);
                    temp[idx + 3] = 255;
                }
            });

            // Vertical pass + blend with original
            Parallel.For(0, height, _parallelOptions, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = (y * width + x) * 4;

                    float sumB = 0, sumG = 0, sumR = 0;

                    for (int k = 0; k < kernelSize; k++)
                    {
                        int sy = y + k - radius;
                        if (sy < 0) sy = 0;
                        if (sy >= height) sy = height - 1;

                        int sIdx = (sy * width + x) * 4;
                        float w = kernel[k];
                        sumB += temp[sIdx + 0] * w;
                        sumG += temp[sIdx + 1] * w;
                        sumR += temp[sIdx + 2] * w;
                    }

                    // Blend blurred with original (additive bloom)
                    byte origB = src[idx + 0];
                    byte origG = src[idx + 1];
                    byte origR = src[idx + 2];

                    dst[idx + 0] = (byte)Math.Min(255, origB + sumB * intensity);
                    dst[idx + 1] = (byte)Math.Min(255, origG + sumG * intensity);
                    dst[idx + 2] = (byte)Math.Min(255, origR + sumR * intensity);
                    dst[idx + 3] = 255;
                }
            });
        }

        // ============================================================
        // === HELPER METHODS ===
        // ============================================================

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ColorToBgra(Color c) => (uint)(c.B | (c.G << 8) | (c.R << 16) | (255 << 24));

        private void CopyBufferToBitmap(byte[] buffer)
        {
            var rect = new Rectangle(0, 0, _outputWidth, _outputHeight);
            BitmapData bd = _bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(buffer, 0, bd.Scan0, buffer.Length);
            _bitmap.UnlockBits(bd);
        }

        // ============================================================
        // === DISPOSE ===
        // ============================================================

        public void Dispose()
        {
            _threadRng?.Dispose();
            _bitmap?.Dispose();
        }
    }
}