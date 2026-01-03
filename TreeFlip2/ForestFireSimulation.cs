using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace TreeGrowth
{
    /// <summary>
    /// Forest Fire Simulation Engine (Drossel-Schwabl cellular automaton model)
    /// Handles all simulation logic: tree growth, fire propagation, and burn decay
    /// </summary>
    public class ForestFireSimulation
    {
        // ============================================================
        // === CELL STATES ===
        // ============================================================
        
        public const int VACANT = 0;
        public const int TREE = 1;
        public const int BURNING = 2;
        public const int RECENTLY_BURNED_STATE = -1;

        // ============================================================
        // === GRID CONFIGURATION ===
        // ============================================================
        
        private const int REFERENCE_GRID_SIZE = 512 * 512; // For speed scaling

        private int _outputWidth;
        private int _outputHeight;
        private int _cellSize;

        private int _logicalWidth => _outputWidth / _cellSize;
        private int _logicalHeight => _outputHeight / _cellSize;

        /// <summary>Gets the logical grid width</summary>
        public int LogicalWidth => _logicalWidth;

        /// <summary>Gets the logical grid height</summary>
        public int LogicalHeight => _logicalHeight;

        /// <summary>Gets the total number of logical cells</summary>
        public int TotalLogicalCells => _logicalWidth * _logicalHeight;

        // ============================================================
        // === SIMULATION STATE ===
        // ============================================================
        
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

        /// <summary>Gets the simulation grid (direct access for rendering)</summary>
        public int[] Grid => _grid;

        /// <summary>Gets whether a fire is currently active</summary>
        public bool IsFireActive => _isFireActive;

        /// <summary>Gets the total number of simulation timesteps</summary>
        public long Timesteps => _Ns;

        /// <summary>Gets the current tree count</summary>
        public int TreeCount => _treeCount;

        /// <summary>Gets the total number of fires that have occurred</summary>
        public long TotalFires => _totalFires;

        /// <summary>Gets read-only fire statistics (fire size -> occurrence count)</summary>
        public IReadOnlyDictionary<int, int> FireStatistics => _fireStats;

        // ============================================================
        // === SIMULATION PARAMETERS ===
        // ============================================================
        
        private double _p = 0.01;           // Tree growth probability
        private double _f = 1e-5;           // Lightning strike probability
        private int _baseStepsPerFrame = 1000;
        private int _stepsPerFrame = 1000;
        private bool _useMooreNeighborhood = true;
        private int _burnDecayFrames = 15;
        private int _fireAnimationSpeed = 1;
        private bool _animateFires = true;

        // === PERLIN NOISE DISTRIBUTION ===
        private bool _usePerlinDistribution = false;
        private PerlinNoise? _perlinNoise;
        private double[,]? _densityMap;
        private double _noiseScale = 50.0;
        private int _noiseOctaves = 4;
        private double _noiseThreshold = 0.3;  // Minimum noise value for tree growth (0-1)
        private double _noiseStrength = 1.0;   // How much noise affects growth (0=none, 1=full)

        /// <summary>Gets or sets tree growth probability (p)</summary>
        public double P
        {
            get => _p;
            set => _p = Math.Clamp(value, 0.0, 1.0);
        }

        /// <summary>Gets or sets lightning strike probability (f)</summary>
        public double F
        {
            get => _f;
            set => _f = Math.Clamp(value, 0.0, 1.0);
        }

        /// <summary>Gets or sets whether to use Moore (8-neighbor) vs Von Neumann (4-neighbor) neighborhood</summary>
        public bool UseMooreNeighborhood
        {
            get => _useMooreNeighborhood;
            set => _useMooreNeighborhood = value;
        }

        /// <summary>Gets or sets the number of frames for burn decay animation</summary>
        public int BurnDecayFrames
        {
            get => _burnDecayFrames;
            set => _burnDecayFrames = Math.Max(1, value);
        }

        /// <summary>Gets or sets fire animation speed (cells per frame)</summary>
        public int FireAnimationSpeed
        {
            get => _fireAnimationSpeed;
            set => _fireAnimationSpeed = Math.Max(1, value);
        }

        /// <summary>Gets or sets whether to animate fires (vs instant propagation)</summary>
        public bool AnimateFires
        {
            get => _animateFires;
            set => _animateFires = value;
        }

        /// <summary>Gets or sets base steps per frame (auto-scales with grid size)</summary>
        public int BaseStepsPerFrame
        {
            get => _baseStepsPerFrame;
            set
            {
                _baseStepsPerFrame = Math.Max(1, value);
                UpdateStepsPerFrame();
            }
        }

        /// <summary>Gets the actual steps per frame (scaled)</summary>
        public int StepsPerFrame => _stepsPerFrame;

        /// <summary>Gets or sets whether to use Perlin noise for spatial tree distribution</summary>
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

        /// <summary>Gets or sets the scale of Perlin noise features (higher = larger patches)</summary>
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

        /// <summary>Gets or sets the number of octaves in Perlin noise (more = more detail)</summary>
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

        /// <summary>Gets or sets the minimum noise threshold for tree growth (0-1)</summary>
        public double NoiseThreshold
        {
            get => _noiseThreshold;
            set => _noiseThreshold = Math.Clamp(value, 0.0, 1.0);
        }

        /// <summary>Gets or sets how much noise affects growth probability (0=none, 1=full)</summary>
        public double NoiseStrength
        {
            get => _noiseStrength;
            set => _noiseStrength = Math.Clamp(value, 0.0, 1.0);
        }

        // ============================================================
        // === RNG & PARALLEL ===
        // ============================================================
        
        private XorShift128Plus _rng;
        private readonly ParallelOptions _parallelOptions;

        // ============================================================
        // === CONSTRUCTOR ===
        // ============================================================

        /// <summary>
        /// Creates a new forest fire simulation
        /// </summary>
        /// <param name="outputWidth">Display width in pixels</param>
        /// <param name="outputHeight">Display height in pixels</param>
        /// <param name="cellSize">Size of each logical cell in pixels (1=fine, 8=chunky)</param>
        /// <param name="maxDegreeOfParallelism">Max parallel threads (typically CPU core count)</param>
        public ForestFireSimulation(int outputWidth, int outputHeight, int cellSize, int maxDegreeOfParallelism)
        {
            _outputWidth = outputWidth;
            _outputHeight = outputHeight;
            _cellSize = cellSize;

            _grid = new int[_logicalHeight * _logicalWidth];

            _parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism
            };

            UpdateStepsPerFrame();
        }

        // ============================================================
        // === GRID MANAGEMENT ===
        // ============================================================

        /// <summary>
        /// Resizes the simulation grid (clears all state)
        /// </summary>
        public void SetGridSize(int outputWidth, int outputHeight, int cellSize)
        {
            _outputWidth = outputWidth;
            _outputHeight = outputHeight;
            _cellSize = cellSize;
            _grid = new int[_logicalHeight * _logicalWidth];
            UpdateStepsPerFrame();
        }

        /// <summary>
        /// Initializes/resets the simulation with a new seed
        /// </summary>
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

        /// <summary>
        /// Regenerates the Perlin noise density map
        /// </summary>
        private void RegenerateNoiseMap()
        {
            var seed = (int)(_rng.NextU64() & 0x7FFFFFFF);
            _perlinNoise = new PerlinNoise(seed, _noiseScale, _noiseOctaves);
            _densityMap = _perlinNoise.GenerateNoiseMap(_logicalWidth, _logicalHeight);
        }

        /// <summary>
        /// Gets the density multiplier at a given location (based on Perlin noise)
        /// </summary>
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

        // ============================================================
        // === GRID ACCESS ===
        // ============================================================

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GridIndex(int x, int y) => y * _logicalWidth + x;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetCell(int x, int y)
        {
            if ((uint)x >= _logicalWidth || (uint)y >= _logicalHeight)
                return VACANT;
            return _grid[y * _logicalWidth + x];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetCell(int x, int y, int value)
        {
            if ((uint)x < _logicalWidth && (uint)y < _logicalHeight)
                _grid[y * _logicalWidth + x] = value;
        }

        /// <summary>
        /// Manually starts a fire at the specified location (if a tree exists)
        /// </summary>
        /// <param name="x">Logical X coordinate</param>
        /// <param name="y">Logical Y coordinate</param>
        /// <returns>True if fire was started, false if no tree at location</returns>
        public bool TryStartFireAt(int x, int y)
        {
            if ((uint)x >= _logicalWidth || (uint)y >= _logicalHeight)
                return false;

            if (GetCell(x, y) != TREE)
                return false;

            if (_isFireActive)
            {
                // Fire already active, just add this cell to the active fire
                SetCell(x, y, BURNING);
                _treeCount--;
                _currentFireSize++;
                _fireList.Add((x, y));
            }
            else
            {
                // Start a new fire
                StartFire(x, y);
            }

            return true;
        }

        // ============================================================
        // === MAIN SIMULATION STEP ===
        // ============================================================

        /// <summary>
        /// Advances the simulation by one frame
        /// </summary>
        public void Step()
        {
            DecayRecentlyBurned();

            if (_isFireActive)
            {
                RunTreeGrowth();
                if (_animateFires)
                    RunFireStepAnimated();
                else
                    RunFireInstantly();
            }
            else
            {
                RunSimulationSteps();
            }
        }

        // ============================================================
        // === SIMULATION LOGIC ===
        // ============================================================

        private void RunSimulationSteps()
        {
            for (int i = 0; i < _stepsPerFrame; i++)
            {
                // Tree growth attempt
                int xg = _rng.NextInt(_logicalWidth);
                int yg = _rng.NextInt(_logicalHeight);

                if (GetCell(xg, yg) == VACANT)
                {
                    // Apply spatial density multiplier from Perlin noise
                    double densityMultiplier = GetDensityMultiplier(xg, yg);
                    double adjustedP = _p * densityMultiplier;

                    if (_rng.NextDouble() < adjustedP)
                    {
                        SetCell(xg, yg, TREE);
                        _treeCount++;
                    }
                }

                // Lightning strike attempt
                int xf = _rng.NextInt(_logicalWidth);
                int yf = _rng.NextInt(_logicalHeight);

                if (GetCell(xf, yf) == TREE && _rng.NextDouble() < _f)
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
                // Tree growth
                int xg = _rng.NextInt(_logicalWidth);
                int yg = _rng.NextInt(_logicalHeight);

                if (GetCell(xg, yg) == VACANT)
                {
                    // Apply spatial density multiplier from Perlin noise
                    double densityMultiplier = GetDensityMultiplier(xg, yg);
                    double adjustedP = _p * densityMultiplier;

                    if (_rng.NextDouble() < adjustedP)
                    {
                        SetCell(xg, yg, TREE);
                        _treeCount++;
                    }
                }

                // Lightning strike (can add to existing fire)
                int xf = _rng.NextInt(_logicalWidth);
                int yf = _rng.NextInt(_logicalHeight);

                if (GetCell(xf, yf) == TREE && _rng.NextDouble() < _f)
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
            for (int step = 0; step < _fireAnimationSpeed && _isFireActive; step++)
            {
                _nextFireList.Clear();

                for (int i = 0; i < _fireList.Count; i++)
                {
                    var cell = _fireList[i];
                    SpreadFireFrom(cell);

                    int burnValue = RECENTLY_BURNED_STATE - _burnDecayFrames + 1;
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

                    int burnValue = RECENTLY_BURNED_STATE - _burnDecayFrames + 1;
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
            if (_useMooreNeighborhood)
            {
                // 8-neighbor (Moore)
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        TryIgnite(cell.x + dx, cell.y + dy);
                    }
            }
            else
            {
                // 4-neighbor (Von Neumann)
                TryIgnite(cell.x - 1, cell.y);
                TryIgnite(cell.x + 1, cell.y);
                TryIgnite(cell.x, cell.y - 1);
                TryIgnite(cell.x, cell.y + 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TryIgnite(int nx, int ny)
        {
            if ((uint)nx < _logicalWidth && (uint)ny < _logicalHeight && GetCell(nx, ny) == TREE)
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

        // ============================================================
        // === BURN DECAY (PARALLEL) ===
        // ============================================================

        private void DecayRecentlyBurned()
        {
            if (_burningCells.Count == 0) return;

            var cellsArray = new (int x, int y)[_burningCells.Count];
            _burningCells.CopyTo(cellsArray);

            var removeList = new System.Collections.Concurrent.ConcurrentBag<(int x, int y)>();

            Parallel.ForEach(cellsArray, _parallelOptions, cell =>
            {
                int idx = GridIndex(cell.x, cell.y);
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

        // ============================================================
        // === XORSHIFT128+ RNG (EMBEDDED) ===
        // ============================================================

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