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
        private readonly int _cellSizeShift; // For fast division via bit shift
        private readonly ParallelOptions _parallelOptions;
        private const int PARALLEL_BATCH_SIZE = 32; // Process 32 rows per task

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
        private readonly byte[] _bloomTempBuffer; // Reusable temp buffer for bloom
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
            
            // Calculate bit shift for fast division (only works for power of 2)
            _cellSizeShift = cellSize switch
            {
                1 => 0,
                2 => 1,
                4 => 2,
                8 => 3,
                16 => 4,
                _ => -1 // Use normal division
            };

            int bufferSize = outputHeight * outputWidth * 4;
            _pixelBuffer = new byte[bufferSize];
            _blurBuffer = new byte[bufferSize];
            _bloomTempBuffer = new byte[bufferSize]; // Preallocate temp buffer
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
        // =============================================================

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
        // === MAIN RENDER METHOD (OPTIMIZED) ===
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
            int cellSizeShift = _cellSizeShift;
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

            // === RENDER CELLS WITH SCALING (OPTIMIZED WITH BATCHING) ===
            int numBatches = (outputH + PARALLEL_BATCH_SIZE - 1) / PARALLEL_BATCH_SIZE;
            
            Parallel.For(0, numBatches, _parallelOptions, batchIdx =>
            {
                var localRng = _threadRng.Value;
                int startY = batchIdx * PARALLEL_BATCH_SIZE;
                int endY = Math.Min(startY + PARALLEL_BATCH_SIZE, outputH);

                unsafe
                {
                    fixed (byte* bufferPtr = buffer)
                    fixed (int* gridPtr = grid)
                    {
                        for (int outputY = startY; outputY < endY; outputY++)
                        {
                            // Map output Y to logical Y (optimized)
                            int logicalY = cellSizeShift >= 0 ? (outputY >> cellSizeShift) : (outputY / cellSize);
                            if (logicalY >= logicalH) logicalY = logicalH - 1;

                            int rowStartBuffer = outputY * outputW * 4;
                            byte* rowPtr = bufferPtr + rowStartBuffer;

                            for (int outputX = 0; outputX < outputW; outputX++)
                            {
                                // Map output X to logical X (optimized)
                                int logicalX = cellSizeShift >= 0 ? (outputX >> cellSizeShift) : (outputX / cellSize);
                                if (logicalX >= logicalW) logicalX = logicalW - 1;

                                int state = gridPtr[logicalY * logicalW + logicalX];
                                int pixelOffset = outputX * 4;

                                if (state == ForestFireSimulation.BURNING)
                                {
                                    byte g = (byte)Math.Min(255, fireG + (flickerRange > 0 ? localRng.NextInt(flickerRange) : 0));
                                    rowPtr[pixelOffset] = fireB;
                                    rowPtr[pixelOffset + 1] = g;
                                    rowPtr[pixelOffset + 2] = fireR;
                                    rowPtr[pixelOffset + 3] = 255;
                                }
                                else if (state <= ForestFireSimulation.RECENTLY_BURNED_STATE)
                                {
                                    double decayProgress = (state - ForestFireSimulation.RECENTLY_BURNED_STATE + 1) / (double)burnDecay;
                                    double intensity = 1.0 - decayProgress;
                                    rowPtr[pixelOffset] = (byte)(burnB * intensity);
                                    rowPtr[pixelOffset + 1] = (byte)(burnG * intensity);
                                    rowPtr[pixelOffset + 2] = (byte)(burnR * intensity);
                                    rowPtr[pixelOffset + 3] = 255;
                                }
                                else if (state == ForestFireSimulation.TREE)
                                {
                                    *(uint*)(rowPtr + pixelOffset) = colorTreeBgra;
                                }
                                else // VACANT
                                {
                                    *(uint*)(rowPtr + pixelOffset) = colorVacantBgra;
                                }
                            }
                        }
                    }
                }
            });

            LastDrawMs = sw.ElapsedMilliseconds;

            // === APPLY BLOOM IF ENABLED ===
            byte[] finalBuffer = buffer;
            if (EnableBloom && BloomRadius > 0)
            {
                var bloomSw = System.Diagnostics.Stopwatch.StartNew();
                ApplyBloomOptimized(buffer, _blurBuffer, _bloomTempBuffer, outputW, outputH);
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

        /// <summary>
        /// Gets the internal bitmap reference for comparison purposes
        /// </summary>
        internal Bitmap GetInternalBitmap() => _bitmap;

        // ============================================================
        // === BLOOM/BLUR EFFECT (OPTIMIZED) ===
        // ============================================================

        private void ApplyBloomOptimized(byte[] src, byte[] dst, byte[] temp, int width, int height)
        {
            float intensity = BloomIntensity;
            int radius = BloomRadius;
            float[] kernel = _bloomKernel;
            int kernelSize = kernel.Length;
            
            // Calculate number of batches for horizontal pass
            int numBatches = (height + PARALLEL_BATCH_SIZE - 1) / PARALLEL_BATCH_SIZE;

            // Horizontal pass (batched)
            Parallel.For(0, numBatches, _parallelOptions, batchIdx =>
            {
                int startY = batchIdx * PARALLEL_BATCH_SIZE;
                int endY = Math.Min(startY + PARALLEL_BATCH_SIZE, height);

                unsafe
                {
                    fixed (byte* srcPtr = src)
                    fixed (byte* tempPtr = temp)
                    fixed (float* kernelPtr = kernel)
                    {
                        for (int y = startY; y < endY; y++)
                        {
                            int rowStart = y * width * 4;

                            for (int x = 0; x < width; x++)
                            {
                                int idx = rowStart + x * 4;

                                // Check if this pixel should be bloomed
                                if (BloomFireOnly)
                                {
                                    byte srcR = srcPtr[idx + 2];
                                    byte srcG = srcPtr[idx + 1];
                                    byte srcB = srcPtr[idx + 0];

                                    // Fast color distance check
                                    int treeDistSq = (srcR - ColorTree.R) * (srcR - ColorTree.R) +
                                                     (srcG - ColorTree.G) * (srcG - ColorTree.G) +
                                                     (srcB - ColorTree.B) * (srcB - ColorTree.B);
                                    int vacantDistSq = (srcR - ColorVacant.R) * (srcR - ColorVacant.R) +
                                                       (srcG - ColorVacant.G) * (srcG - ColorVacant.G) +
                                                       (srcB - ColorVacant.B) * (srcB - ColorVacant.B);

                                    if (treeDistSq < 1200 || vacantDistSq < 1200) // ~20² * 3
                                    {
                                        // Copy without blur
                                        *(uint*)(tempPtr + idx) = *(uint*)(srcPtr + idx);
                                        continue;
                                    }
                                }

                                float sumB = 0, sumG = 0, sumR = 0;

                                for (int k = 0; k < kernelSize; k++)
                                {
                                    int sx = x + k - radius;
                                    sx = Math.Clamp(sx, 0, width - 1);

                                    int sIdx = rowStart + sx * 4;
                                    float w = kernelPtr[k];
                                    sumB += srcPtr[sIdx + 0] * w;
                                    sumG += srcPtr[sIdx + 1] * w;
                                    sumR += srcPtr[sIdx + 2] * w;
                                }

                                tempPtr[idx + 0] = (byte)Math.Min(255, sumB);
                                tempPtr[idx + 1] = (byte)Math.Min(255, sumG);
                                tempPtr[idx + 2] = (byte)Math.Min(255, sumR);
                                tempPtr[idx + 3] = 255;
                            }
                        }
                    }
                }
            });

            // Vertical pass + blend with original (batched)
            Parallel.For(0, numBatches, _parallelOptions, batchIdx =>
            {
                int startY = batchIdx * PARALLEL_BATCH_SIZE;
                int endY = Math.Min(startY + PARALLEL_BATCH_SIZE, height);

                unsafe
                {
                    fixed (byte* srcPtr = src)
                    fixed (byte* tempPtr = temp)
                    fixed (byte* dstPtr = dst)
                    fixed (float* kernelPtr = kernel)
                    {
                        for (int y = startY; y < endY; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                int idx = (y * width + x) * 4;

                                float sumB = 0, sumG = 0, sumR = 0;

                                for (int k = 0; k < kernelSize; k++)
                                {
                                    int sy = y + k - radius;
                                    sy = Math.Clamp(sy, 0, height - 1);

                                    int sIdx = (sy * width + x) * 4;
                                    float w = kernelPtr[k];
                                    sumB += tempPtr[sIdx + 0] * w;
                                    sumG += tempPtr[sIdx + 1] * w;
                                    sumR += tempPtr[sIdx + 2] * w;
                                }

                                // Blend blurred with original (additive bloom)
                                byte origB = srcPtr[idx + 0];
                                byte origG = srcPtr[idx + 1];
                                byte origR = srcPtr[idx + 2];

                                dstPtr[idx + 0] = (byte)Math.Min(255, origB + sumB * intensity);
                                dstPtr[idx + 1] = (byte)Math.Min(255, origG + sumG * intensity);
                                dstPtr[idx + 2] = (byte)Math.Min(255, origR + sumR * intensity);
                                dstPtr[idx + 3] = 255;
                            }
                        }
                    }
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