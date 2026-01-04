using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;

namespace TreeGrowth.Avalonia.Core
{
    /// <summary>
    /// Cross-platform renderer using SkiaSharp
    /// Renders forest fire simulation to SKBitmap for use with Avalonia
    /// </summary>
    public class ForestFireRenderer : IDisposable
    {
        private readonly int _outputWidth;
        private readonly int _outputHeight;
        private readonly int _cellSize;
        private readonly int _cellSizeShift;
        private readonly ParallelOptions _parallelOptions;
        private const int PARALLEL_BATCH_SIZE = 32;

        // Colors
        public SKColor ColorTree { get; set; } = new SKColor(198, 164, 145);
        public SKColor ColorVacant { get; set; } = new SKColor(169, 130, 104);
        public SKColor ColorFireBase { get; set; } = new SKColor(255, 200, 0);
        public SKColor ColorBurnout { get; set; } = new SKColor(255, 191, 0);
        public int FireFlickerRange { get; set; } = 105;

        // Bloom
        public bool EnableBloom { get; set; } = false;
        public int BloomRadius { get; set; } = 2;
        public float BloomIntensity { get; set; } = 0.5f;
        public bool BloomFireOnly { get; set; } = true;

        // Overlay
        private SKBitmap? _overlayImage;
        private SKBitmap? _scaledOverlayCache;
        public bool ShowOverlay { get; set; } = false;

        private readonly byte[] _pixelBuffer;
        private readonly byte[] _blurBuffer;
        private readonly byte[] _bloomTempBuffer;
        private float[] _bloomKernel = Array.Empty<float>();
        private readonly SKBitmap _bitmap;

        private readonly ThreadLocal<ForestFireSimulation.XorShift128Plus> _threadRng;

        public long LastDrawMs { get; private set; }
        public long LastBloomMs { get; private set; }

        public ForestFireRenderer(int outputWidth, int outputHeight, int cellSize, int maxDegreeOfParallelism)
        {
            _outputWidth = outputWidth;
            _outputHeight = outputHeight;
            _cellSize = cellSize;

            _cellSizeShift = cellSize switch
            {
                1 => 0,
                2 => 1,
                4 => 2,
                8 => 3,
                16 => 4,
                _ => -1
            };

            int bufferSize = outputHeight * outputWidth * 4;
            _pixelBuffer = new byte[bufferSize];
            _blurBuffer = new byte[bufferSize];
            _bloomTempBuffer = new byte[bufferSize];
            
            // Create SkiaSharp bitmap
            _bitmap = new SKBitmap(outputWidth, outputHeight, SKColorType.Bgra8888, SKAlphaType.Premul);

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

            for (int i = 0; i < size; i++)
                _bloomKernel[i] /= sum;
        }

        public void UpdateBloomKernel()
        {
            InitializeBloomKernel();
        }

        /// <summary>
        /// Loads an overlay image from a file path
        /// </summary>
        public bool LoadOverlayImage(string filePath)
        {
            try
            {
                // Dispose old overlay
                _overlayImage?.Dispose();
                _overlayImage = null;
                _scaledOverlayCache?.Dispose();
                _scaledOverlayCache = null;

                // Load new overlay
                _overlayImage = SKBitmap.Decode(filePath);
                return _overlayImage != null;
            }
            catch
            {
                _overlayImage?.Dispose();
                _overlayImage = null;
                _scaledOverlayCache?.Dispose();
                _scaledOverlayCache = null;
                return false;
            }
        }

        /// <summary>
        /// Loads an overlay image from a byte array
        /// </summary>
        public bool LoadOverlayImage(byte[] imageData)
        {
            try
            {
                // Dispose old overlay
                _overlayImage?.Dispose();
                _overlayImage = null;
                _scaledOverlayCache?.Dispose();
                _scaledOverlayCache = null;

                // Load new overlay
                _overlayImage = SKBitmap.Decode(imageData);
                return _overlayImage != null;
            }
            catch
            {
                _overlayImage?.Dispose();
                _overlayImage = null;
                _scaledOverlayCache?.Dispose();
                _scaledOverlayCache = null;
                return false;
            }
        }

        /// <summary>
        /// Clears the overlay image
        /// </summary>
        public void ClearOverlayImage()
        {
            _overlayImage?.Dispose();
            _overlayImage = null;
            _scaledOverlayCache?.Dispose();
            _scaledOverlayCache = null;
        }

        public SKBitmap Render(ForestFireSimulation simulation)
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

            uint colorTreeBgra = ColorToBgra(ColorTree);
            uint colorVacantBgra = ColorToBgra(ColorVacant);
            byte fireR = ColorFireBase.Red;
            byte fireG = ColorFireBase.Green;
            byte fireB = ColorFireBase.Blue;
            byte burnR = ColorBurnout.Red;
            byte burnG = ColorBurnout.Green;
            byte burnB = ColorBurnout.Blue;
            int flickerRange = FireFlickerRange;
            int burnDecay = simulation.BurnDecayFrames;

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
                            int logicalY = cellSizeShift >= 0 ? (outputY >> cellSizeShift) : (outputY / cellSize);
                            if (logicalY >= logicalH) logicalY = logicalH - 1;

                            int rowStartBuffer = outputY * outputW * 4;
                            byte* rowPtr = bufferPtr + rowStartBuffer;

                            for (int outputX = 0; outputX < outputW; outputX++)
                            {
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
                                else
                                {
                                    *(uint*)(rowPtr + pixelOffset) = colorVacantBgra;
                                }
                            }
                        }
                    }
                }
            });

            LastDrawMs = sw.ElapsedMilliseconds;

            byte[] finalBuffer = buffer;
            if (EnableBloom && BloomRadius > 0)
            {
                var bloomSw = System.Diagnostics.Stopwatch.StartNew();
                ApplyBloomOptimized(buffer, _blurBuffer, _bloomTempBuffer, outputW, outputH);
                LastBloomMs = bloomSw.ElapsedMilliseconds;
                finalBuffer = _blurBuffer;
            }

            CopyBufferToBitmap(finalBuffer);

            // Apply overlay if enabled
            if (ShowOverlay && _overlayImage != null)
            {
                ApplyOverlay();
            }

            return _bitmap;
        }

        /// <summary>
        /// Applies the overlay image on top of the rendered bitmap
        /// </summary>
        private unsafe void ApplyOverlay()
        {
            if (_overlayImage == null) return;

            // Create or update scaled overlay cache
            if (_scaledOverlayCache == null ||
                _scaledOverlayCache.Width != _outputWidth ||
                _scaledOverlayCache.Height != _outputHeight)
            {
                _scaledOverlayCache?.Dispose();
                _scaledOverlayCache = _overlayImage.Resize(
                    new SKImageInfo(_outputWidth, _outputHeight, SKColorType.Bgra8888, SKAlphaType.Premul),
                    SKFilterQuality.High);
            }

            if (_scaledOverlayCache == null) return;

            // Get pixel pointers
            IntPtr basePtr = _bitmap.GetPixels();
            IntPtr overlayPtr = _scaledOverlayCache.GetPixels();

            int totalPixels = _outputWidth * _outputHeight;

            byte* basePixels = (byte*)basePtr;
            byte* overlayPixels = (byte*)overlayPtr;

            // Parallel alpha blending
            Parallel.For(0, totalPixels, _parallelOptions, i =>
            {
                int offset = i * 4;
                byte overlayA = overlayPixels[offset + 3];

                if (overlayA == 255)
                {
                    // Fully opaque - direct copy
                    basePixels[offset] = overlayPixels[offset];
                    basePixels[offset + 1] = overlayPixels[offset + 1];
                    basePixels[offset + 2] = overlayPixels[offset + 2];
                    basePixels[offset + 3] = 255;
                }
                else if (overlayA > 0)
                {
                    // Partial transparency - alpha blending
                    byte baseB = basePixels[offset];
                    byte baseG = basePixels[offset + 1];
                    byte baseR = basePixels[offset + 2];

                    byte overlayB = overlayPixels[offset];
                    byte overlayG = overlayPixels[offset + 1];
                    byte overlayR = overlayPixels[offset + 2];

                    // Integer-based alpha blending
                    int invAlpha = 255 - overlayA;
                    basePixels[offset] = (byte)((overlayB * overlayA + baseB * invAlpha) / 255);
                    basePixels[offset + 1] = (byte)((overlayG * overlayA + baseG * invAlpha) / 255);
                    basePixels[offset + 2] = (byte)((overlayR * overlayA + baseR * invAlpha) / 255);
                    basePixels[offset + 3] = 255;
                }
            });
        }

        public byte[] GetPixelBuffer()
        {
            return EnableBloom ? _blurBuffer : _pixelBuffer;
        }

        internal SKBitmap GetInternalBitmap() => _bitmap;

        private void ApplyBloomOptimized(byte[] src, byte[] dst, byte[] temp, int width, int height)
        {
            float intensity = BloomIntensity;
            int radius = BloomRadius;
            float[] kernel = _bloomKernel;
            int kernelSize = kernel.Length;

            int numBatches = (height + PARALLEL_BATCH_SIZE - 1) / PARALLEL_BATCH_SIZE;

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

                                if (BloomFireOnly)
                                {
                                    byte srcR = srcPtr[idx + 2];
                                    byte srcG = srcPtr[idx + 1];
                                    byte srcB = srcPtr[idx + 0];

                                    int treeDistSq = (srcR - ColorTree.Red) * (srcR - ColorTree.Red) +
                                                     (srcG - ColorTree.Green) * (srcG - ColorTree.Green) +
                                                     (srcB - ColorTree.Blue) * (srcB - ColorTree.Blue);
                                    int vacantDistSq = (srcR - ColorVacant.Red) * (srcR - ColorVacant.Red) +
                                                       (srcG - ColorVacant.Green) * (srcG - ColorVacant.Green) +
                                                       (srcB - ColorVacant.Blue) * (srcB - ColorVacant.Blue);

                                    if (treeDistSq < 1200 || vacantDistSq < 1200)
                                    {
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ColorToBgra(SKColor c) => (uint)(c.Blue | (c.Green << 8) | (c.Red << 16) | (255 << 24));

        private unsafe void CopyBufferToBitmap(byte[] buffer)
        {
            IntPtr pixelsPtr = _bitmap.GetPixels();
            fixed (byte* bufferPtr = buffer)
            {
                Buffer.MemoryCopy(bufferPtr, (void*)pixelsPtr, buffer.Length, buffer.Length);
            }
        }

        public void Dispose()
        {
            _threadRng?.Dispose();
            _bitmap?.Dispose();
            _overlayImage?.Dispose();
            _scaledOverlayCache?.Dispose();
        }
    }
}
