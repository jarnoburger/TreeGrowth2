using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace TreeGrowth.Avalonia.Core
{
    /// <summary>
    /// Forest Fire Simulation Engine (Drossel-Schwabl cellular automaton model)
    /// Cross-platform compatible - no UI dependencies
    /// </summary>
    public class ForestFireSimulation
    {
        // Cell states
        public const int VACANT = 0;
        public const int TREE = 1;
        public const int BURNING = 2;
        public const int RECENTLY_BURNED_STATE = -1;

        private const int REFERENCE_GRID_SIZE = 512 * 512;

        private int _outputWidth;
        private int _outputHeight;
        private int _cellSize;

        public int LogicalWidth => _outputWidth / _cellSize;
        public int LogicalHeight => _outputHeight / _cellSize;
        public int TotalLogicalCells => LogicalWidth * LogicalHeight;

        private int[] _grid;
        private bool _isFireActive;
        private long _Ns = 0;
        private int _treeCount = 0;
        private long _totalFires = 0;
        private int _currentFireSize = 0;

        private List<(int x, int y)> _fireList = new();
        private List<(int x, int y)> _nextFireList = new();
        private readonly HashSet<(int x, int y)> _burningCells = new();
        private readonly Dictionary<int, int> _fireStats = new();

        public int[] Grid => _grid;
        public bool IsFireActive => _isFireActive;
        public long Timesteps => _Ns;
        public int TreeCount => _treeCount;
        public long TotalFires => _totalFires;
        public IReadOnlyDictionary<int, int> FireStatistics => _fireStats;

        // Parameters
        public double P { get; set; } = 0.01;
        public double F { get; set; } = 1e-5;
        public bool UseMooreNeighborhood { get; set; } = true;
        public int BurnDecayFrames { get; set; } = 15;
        public int FireAnimationSpeed { get; set; } = 1;
        public bool AnimateFires { get; set; } = true;

        // Perlin Noise Distribution
        private bool _usePerlinDistribution = false;
        private PerlinNoise? _perlinNoise;
        private double[,]? _densityMap;
        private double _noiseScale = 50.0;
        private int _noiseOctaves = 4;
        private double _noiseThreshold = 0.3;
        private double _noiseStrength = 1.0;

        public bool UsePerlinDistribution
        {
            get => _usePerlinDistribution;
            set
            {
                if (_usePerlinDistribution != value)
                {
                    _usePerlinDistribution = value;
                    if (value)
                        RegenerateNoiseMap();
                }
            }
        }

        public double NoiseScale
        {
            get => _noiseScale;
            set
            {
                _noiseScale = Math.Max(1.0, value);
                if (_usePerlinDistribution)
                    RegenerateNoiseMap();
            }
        }

        public int NoiseOctaves
        {
            get => _noiseOctaves;
            set
            {
                _noiseOctaves = Math.Clamp(value, 1, 8);
                if (_usePerlinDistribution)
                    RegenerateNoiseMap();
            }
        }

        public double NoiseThreshold
        {
            get => _noiseThreshold;
            set => _noiseThreshold = Math.Clamp(value, 0.0, 1.0);
        }

        public double NoiseStrength
        {
            get => _noiseStrength;
            set => _noiseStrength = Math.Clamp(value, 0.0, 1.0);
        }

        private int _baseStepsPerFrame = 1000;
        private int _stepsPerFrame = 1000;
        public int BaseStepsPerFrame
        {
            get => _baseStepsPerFrame;
            set
            {
                _baseStepsPerFrame = Math.Max(1, value);
                UpdateStepsPerFrame();
            }
        }
        public int StepsPerFrame => _stepsPerFrame;

        private XorShift128Plus _rng;
        private readonly ParallelOptions _parallelOptions;

        public ForestFireSimulation(int outputWidth, int outputHeight, int cellSize, int maxDegreeOfParallelism)
        {
            _outputWidth = outputWidth;
            _outputHeight = outputHeight;
            _cellSize = cellSize;
            _grid = new int[LogicalHeight * LogicalWidth];

            _parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism
            };

            UpdateStepsPerFrame();
        }

        public void SetGridSize(int outputWidth, int outputHeight, int cellSize)
        {
            _outputWidth = outputWidth;
            _outputHeight = outputHeight;
            _cellSize = cellSize;
            _grid = new int[LogicalHeight * LogicalWidth];
            UpdateStepsPerFrame();
        }

        public void Initialize(string seed)
        {
            _rng = XorShift128Plus.FromString(seed);
            Array.Clear(_grid, 0, _grid.Length);
            _Ns = 0;
            _treeCount = 0;
            _totalFires = 0;
            _isFireActive = false;
            _fireList.Clear();
            _nextFireList.Clear();
            _burningCells.Clear();
            _currentFireSize = 0;
            _fireStats.Clear();

            // Regenerate noise map if using Perlin distribution
            if (_usePerlinDistribution)
                RegenerateNoiseMap();
        }

        private void RegenerateNoiseMap()
        {
            var seedValue = (int)(_rng.NextU64() & 0x7FFFFFFF);
            _perlinNoise = new PerlinNoise(seedValue, _noiseScale, _noiseOctaves);
            _densityMap = _perlinNoise.GenerateNoiseMap(LogicalWidth, LogicalHeight);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double GetDensityMultiplier(int x, int y)
        {
            if (!_usePerlinDistribution || _densityMap == null)
                return 1.0;

            double noiseValue = _densityMap[y, x];

            // Apply threshold: values below threshold have zero probability
            if (noiseValue < _noiseThreshold)
                return 0.0;

            // Normalize above threshold: map [threshold, 1] to [0, 1]
            double normalized = (noiseValue - _noiseThreshold) / (1.0 - _noiseThreshold);

            // Blend between uniform (1.0) and noise-based distribution
            return 1.0 - _noiseStrength + _noiseStrength * normalized;
        }

        private void UpdateStepsPerFrame()
        {
            double scaleFactor = (double)TotalLogicalCells / REFERENCE_GRID_SIZE;
            _stepsPerFrame = (int)Math.Round(_baseStepsPerFrame * scaleFactor);
            _stepsPerFrame = Math.Max(100, _stepsPerFrame);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetCell(int x, int y)
        {
            if ((uint)x >= LogicalWidth || (uint)y >= LogicalHeight)
                return VACANT;
            return _grid[y * LogicalWidth + x];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetCell(int x, int y, int value)
        {
            if ((uint)x < LogicalWidth && (uint)y < LogicalHeight)
                _grid[y * LogicalWidth + x] = value;
        }

        public bool TryStartFireAt(int x, int y)
        {
            if ((uint)x >= LogicalWidth || (uint)y >= LogicalHeight)
                return false;

            if (GetCell(x, y) != TREE)
                return false;

            if (_isFireActive)
            {
                SetCell(x, y, BURNING);
                _treeCount--;
                _currentFireSize++;
                _fireList.Add((x, y));
            }
            else
            {
                StartFire(x, y);
            }

            return true;
        }

        public void Step()
        {
            DecayRecentlyBurned();

            if (_isFireActive)
            {
                RunTreeGrowth();
                if (AnimateFires)
                    RunFireStepAnimated();
                else
                    RunFireInstantly();
            }
            else
            {
                RunSimulationSteps();
            }
        }

        private void RunSimulationSteps()
        {
            for (int i = 0; i < _stepsPerFrame; i++)
            {
                int xg = _rng.NextInt(LogicalWidth);
                int yg = _rng.NextInt(LogicalHeight);

                if (GetCell(xg, yg) == VACANT)
                {
                    // Apply spatial density multiplier from Perlin noise
                    double densityMultiplier = GetDensityMultiplier(xg, yg);
                    double adjustedP = P * densityMultiplier;

                    if (_rng.NextDouble() < adjustedP)
                    {
                        SetCell(xg, yg, TREE);
                        _treeCount++;
                    }
                }

                int xf = _rng.NextInt(LogicalWidth);
                int yf = _rng.NextInt(LogicalHeight);

                if (GetCell(xf, yf) == TREE && _rng.NextDouble() < F)
                {
                    StartFire(xf, yf);
                }

                _Ns++;
            }
        }

        private void RunTreeGrowth()
        {
            for (int i = 0; i < _stepsPerFrame; i++)
            {
                int xg = _rng.NextInt(LogicalWidth);
                int yg = _rng.NextInt(LogicalHeight);

                if (GetCell(xg, yg) == VACANT)
                {
                    // Apply spatial density multiplier from Perlin noise
                    double densityMultiplier = GetDensityMultiplier(xg, yg);
                    double adjustedP = P * densityMultiplier;

                    if (_rng.NextDouble() < adjustedP)
                    {
                        SetCell(xg, yg, TREE);
                        _treeCount++;
                    }
                }

                int xf = _rng.NextInt(LogicalWidth);
                int yf = _rng.NextInt(LogicalHeight);

                if (GetCell(xf, yf) == TREE && _rng.NextDouble() < F)
                {
                    SetCell(xf, yf, BURNING);
                    _treeCount--;
                    _currentFireSize++;
                    _fireList.Add((xf, yf));
                }

                _Ns++;
            }
        }

        private void StartFire(int x, int y)
        {
            _isFireActive = true;
            SetCell(x, y, BURNING);
            _treeCount--;
            _currentFireSize = 1;
            _fireList.Add((x, y));
        }

        private void RunFireStepAnimated()
        {
            for (int step = 0; step < FireAnimationSpeed && _isFireActive; step++)
            {
                _nextFireList.Clear();

                for (int i = 0; i < _fireList.Count; i++)
                {
                    var cell = _fireList[i];
                    SpreadFireFrom(cell);

                    int burnValue = RECENTLY_BURNED_STATE - BurnDecayFrames + 1;
                    SetCell(cell.x, cell.y, burnValue);
                    _burningCells.Add((cell.x, cell.y));
                }

                (_fireList, _nextFireList) = (_nextFireList, _fireList);

                if (_fireList.Count == 0)
                    EndFire();
            }
        }

        private void RunFireInstantly()
        {
            while (_isFireActive)
            {
                _nextFireList.Clear();

                for (int i = 0; i < _fireList.Count; i++)
                {
                    var cell = _fireList[i];
                    SpreadFireFrom(cell);

                    int burnValue = RECENTLY_BURNED_STATE - BurnDecayFrames + 1;
                    SetCell(cell.x, cell.y, burnValue);
                    _burningCells.Add((cell.x, cell.y));
                }

                (_fireList, _nextFireList) = (_nextFireList, _fireList);

                if (_fireList.Count == 0)
                    EndFire();
            }
        }

        private void SpreadFireFrom((int x, int y) cell)
        {
            if (UseMooreNeighborhood)
            {
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        TryIgnite(cell.x + dx, cell.y + dy);
                    }
            }
            else
            {
                TryIgnite(cell.x - 1, cell.y);
                TryIgnite(cell.x + 1, cell.y);
                TryIgnite(cell.x, cell.y - 1);
                TryIgnite(cell.x, cell.y + 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TryIgnite(int nx, int ny)
        {
            if ((uint)nx < LogicalWidth && (uint)ny < LogicalHeight && GetCell(nx, ny) == TREE)
            {
                SetCell(nx, ny, BURNING);
                _treeCount--;
                _currentFireSize++;
                _nextFireList.Add((nx, ny));
            }
        }

        private void EndFire()
        {
            _isFireActive = false;
            RecordFireStatistics();
            _currentFireSize = 0;
        }

        private void RecordFireStatistics()
        {
            if (_currentFireSize <= 0) return;
            _fireStats.TryGetValue(_currentFireSize, out int c);
            _fireStats[_currentFireSize] = c + 1;
            _totalFires++;
        }

        private void DecayRecentlyBurned()
        {
            if (_burningCells.Count == 0) return;

            var cellsArray = new (int x, int y)[_burningCells.Count];
            _burningCells.CopyTo(cellsArray);

            var removeList = new System.Collections.Concurrent.ConcurrentBag<(int x, int y)>();

            Parallel.ForEach(cellsArray, _parallelOptions, cell =>
            {
                int idx = cell.y * LogicalWidth + cell.x;
                int v = _grid[idx];

                if (v <= RECENTLY_BURNED_STATE)
                {
                    v++;
                    if (v > RECENTLY_BURNED_STATE)
                    {
                        v = VACANT;
                        removeList.Add((cell.x, cell.y));
                    }
                    _grid[idx] = v;
                }
                else
                {
                    removeList.Add((cell.x, cell.y));
                }
            });

            foreach (var cell in removeList)
            {
                _burningCells.Remove(cell);
            }
        }

        internal struct XorShift128Plus
        {
            private ulong _s0, _s1;

            public static XorShift128Plus FromString(string seed)
            {
                if (string.IsNullOrWhiteSpace(seed)) seed = "default";
                ulong h1 = 1469598103934665603UL;
                ulong h2 = 1099511628211UL;

                foreach (char c in seed)
                {
                    h1 ^= c;
                    h1 *= 1099511628211UL;
                    h2 += (ulong)c * 0x9E3779B97F4A7C15UL;
                    h2 ^= (h2 >> 27);
                }

                if (h1 == 0 && h2 == 0) h1 = 1;

                return new XorShift128Plus { _s0 = h1, _s1 = h2 };
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ulong NextU64()
            {
                ulong x = _s0;
                ulong y = _s1;
                _s0 = y;
                x ^= x << 23;
                x ^= x >> 17;
                x ^= y ^ (y >> 26);
                _s1 = x;
                return _s0 + _s1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public double NextDouble()
            {
                return (NextU64() >> 11) * (1.0 / (1UL << 53));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int NextInt(int maxExclusive)
            {
                if (maxExclusive <= 1) return 0;
                return (int)(NextU64() % (uint)maxExclusive);
            }
        }
    }
}
