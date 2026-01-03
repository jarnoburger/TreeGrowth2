using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TreeGrowth
{
    public partial class Form1 : Form
    {
        // --- Constants mirroring the Veritasium sim ---
        private const int GRID_SIZE = 512;
        private const int TOTAL_SITES = GRID_SIZE * GRID_SIZE;

        private const int VACANT = 0;
        private const int TREE = 1;
        private const int BURNING = 2;

        private const int RECENTLY_BURNED_STATE = -1; // sentinel "just burned"
        private const int BURN_DECAY_FRAMES = 15;
        private const int FIRE_ANIMATION_SPEED = 1;  // cells processed per frame step (like their JS loop count)

        // --- Color constants ---
        // UI Colors
        private static readonly Color COLOR_FORM_BACKGROUND = Color.FromArgb(22, 22, 22);
        private static readonly Color COLOR_FORM_FOREGROUND = Color.White;
        private static readonly Color COLOR_GRID_BACKGROUND = ColorTranslator.FromHtml("#A98268");
        private static readonly Color COLOR_BUTTON_FOREGROUND = Color.Black;
        private static readonly Color COLOR_LABEL_TITLE = Color.White;

        // Simulation cell colors
        private static readonly Color COLOR_TREE = ColorTranslator.FromHtml("#c6a491");
        private static readonly Color COLOR_VACANT = ColorTranslator.FromHtml("#A98268");
        
        // Fire colors (as bytes for performance in tight render loop)
        private const byte COLOR_FIRE_RED = 255;
        private const byte COLOR_FIRE_GREEN_MIN = 150;
        private const byte COLOR_FIRE_GREEN_RANGE = 105; // results in 150-254
        private const byte COLOR_FIRE_BLUE = 0;
        
        // Burnout fade colors
        private const byte COLOR_BURNOUT_RED = 255;
        private const byte COLOR_BURNOUT_GREEN = 191;
        private const byte COLOR_BURNOUT_BLUE = 0;

        // --- Simulation state ---
        private int[,] _grid = new int[GRID_SIZE, GRID_SIZE];

        private bool _isSimulating;
        private bool _isFireActive;

        private double _p = 0.01;
        private double _f = 1e-5;
        private int _stepsPerFrame = 1000;
        private bool _animateFires = true;

        private long _Ns = 0;
        private int _treeCount = 0;
        private long _totalFires = 0;

        // Use List instead of Queue to avoid boxing and improve performance
        private readonly List<(int x, int y)> _fireList = new();
        private readonly List<(int x, int y)> _nextFireList = new();
        private int _currentFireSize = 0;

        // Track burning cells for efficient decay processing
        private readonly HashSet<(int x, int y)> _burningCells = new();

        // Optional: keep stats (matches their Map fireStats)
        private readonly Dictionary<int, int> _fireStats = new();

        // --- Rendering ---
        private Bitmap _bmp = new(GRID_SIZE, GRID_SIZE, PixelFormat.Format32bppArgb);
        
        // Pre-allocated buffer to avoid allocation every frame
        private byte[] _pixelBuffer;
        
        // Frame counter for less frequent UI updates
        private int _frameCounter = 0;
        private const int UI_UPDATE_INTERVAL = 10; // Update UI every 10 frames

        // Seedable RNG
        private XorShift128Plus _rng;

        public Form1()
        {
            InitializeComponent();

            Text = "Forest Fire Model (Drossel–Schwabl) — WinForms";
            StartPosition = FormStartPosition.CenterScreen;

            // ~60 FPS-ish
            _timer.Interval = 16;
            _timer.Tick += (_, __) => StepFrame();

            // Pre-allocate pixel buffer
            _pixelBuffer = new byte[GRID_SIZE * GRID_SIZE * 4];

            InitializeSimulation();
        }

        private void OnPBarScroll(object sender, EventArgs e)
        {
            _p = _pBar.Value / 10000.0;
            UpdateMaxFAndLabels();
        }

        private void OnFBarScroll(object sender, EventArgs e)
        {
            _f = TrackBarToF(_fBar.Value, CurrentFMax());
            UpdateMaxFAndLabels();
        }

        private void OnSpeedBarScroll(object sender, EventArgs e)
        {
            _stepsPerFrame = _speedBar.Value;
            UpdateMaxFAndLabels();
        }

        private void OnAnimateCheckChanged(object sender, EventArgs e)
        {
            _animateFires = _animateCheck.Checked;
        }

        private void OnStartBtnClick(object sender, EventArgs e)
        {
            _isSimulating = !_isSimulating;
            _startBtn.Text = _isSimulating ? "Stop" : "Start";
            if (_isSimulating) _timer.Start();
            else _timer.Stop();
            UpdateFormTitle();
        }

        private void OnResetBtnClick(object sender, EventArgs e)
        {
            _isSimulating = false;
            _timer.Stop();
            _startBtn.Text = "Start";
            InitializeSimulation();
        }

        private void InitializeSimulation()
        {
            // params from UI
            _p = _pBar.Value / 10000.0;  // 0..0.05
            _stepsPerFrame = _speedBar.Value;
            _animateFires = _animateCheck.Checked;

            UpdateMaxFAndLabels();

            // seedable RNG
            _rng = XorShift128Plus.FromString(_seedBox.Text.Trim());

            // clear grid
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
            _frameCounter = 0;

            DrawGrid();
            UpdateStatsLabels();
        }

        private void StepFrame()
        {
            DecayRecentlyBurned();

            if (_isFireActive)
            {
                // Run tree growth even during fire phase
                RunTreeGrowth();
                
                if (_animateFires) RunFireStepAnimated();
                else RunFireInstantly();
            }
            else
            {
                RunSimulationSteps();
            }

            DrawGrid();
            
            // Update UI less frequently to reduce overhead
            _frameCounter++;
            if (_frameCounter >= UI_UPDATE_INTERVAL)
            {
                UpdateStatsLabels();
                _frameCounter = 0;
            }
        }

        // --- Core simulation logic (mirrors their JS) ---
        private void RunSimulationSteps()
        {
            for (int i = 0; i < _stepsPerFrame; i++)
            {
                // Tree growth attempt
                int xg = _rng.NextInt(GRID_SIZE);
                int yg = _rng.NextInt(GRID_SIZE);

                if (_grid[yg, xg] == VACANT && _rng.NextDouble() < _p)
                {
                    _grid[yg, xg] = TREE;
                    _treeCount++;
                }

                // Lightning strike attempt
                int xf = _rng.NextInt(GRID_SIZE);
                int yf = _rng.NextInt(GRID_SIZE);

                if (_grid[yf, xf] == TREE && _rng.NextDouble() < _f)
                {
                    StartFire(xf, yf);
                }

                _Ns++;
            }
        }

        // Extract tree growth logic to reuse during fire phase
        private void RunTreeGrowth()
        {
            for (int i = 0; i < _stepsPerFrame; i++)
            {
                // Tree growth attempt
                int xg = _rng.NextInt(GRID_SIZE);
                int yg = _rng.NextInt(GRID_SIZE);

                if (_grid[yg, xg] == VACANT && _rng.NextDouble() < _p)
                {
                    _grid[yg, xg] = TREE;
                    _treeCount++;
                }

                // Lightning strike attempt (can start additional fires)
                int xf = _rng.NextInt(GRID_SIZE);
                int yf = _rng.NextInt(GRID_SIZE);

                if (_grid[yf, xf] == TREE && _rng.NextDouble() < _f)
                {
                    // Add to existing fire
                    _grid[yf, xf] = BURNING;
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
            _grid[y, x] = BURNING;
            _treeCount--;
            _currentFireSize = 1;
            _fireList.Add((x, y));
        }

        private void RunFireStepAnimated()
        {
            for (int step = 0; step < FIRE_ANIMATION_SPEED && _isFireActive; step++)
            {
                _nextFireList.Clear();

                for (int i = 0; i < _fireList.Count; i++)
                {
                    var cell = _fireList[i];

                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;

                            int nx = cell.x + dx;
                            int ny = cell.y + dy;

                            if ((uint)nx < GRID_SIZE && (uint)ny < GRID_SIZE && _grid[ny, nx] == TREE)
                            {
                                _grid[ny, nx] = BURNING;
                                _treeCount--;
                                _currentFireSize++;
                                _nextFireList.Add((nx, ny));
                            }
                        }
                    }

                    // mark as "recently burned" starting at -BURN_DECAY_FRAMES
                    int burnValue = RECENTLY_BURNED_STATE - BURN_DECAY_FRAMES + 1;
                    _grid[cell.y, cell.x] = burnValue;
                    _burningCells.Add((cell.x, cell.y));
                }

                // swap lists
                var temp = _fireList;
_fireList.Clear();
_fireList.AddRange(_nextFireList);
_nextFireList.Clear();
_nextFireList.AddRange(temp);

                if (_fireList.Count == 0) EndFire();
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

                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;

                            int nx = cell.x + dx;
                            int ny = cell.y + dy;

                            if ((uint)nx < GRID_SIZE && (uint)ny < GRID_SIZE && _grid[ny, nx] == TREE)
                            {
                                _grid[ny, nx] = BURNING;
                                _treeCount--;
                                _currentFireSize++;
                                _nextFireList.Add((nx, ny));
                            }
                        }
                    }

                    int burnValue = RECENTLY_BURNED_STATE - BURN_DECAY_FRAMES + 1;
                    _grid[cell.y, cell.x] = burnValue;
                    _burningCells.Add((cell.x, cell.y));
                }

                // swap lists
                var temp = _fireList;
_fireList.Clear();
_fireList.AddRange(_nextFireList);
_nextFireList.Clear();
_nextFireList.AddRange(temp);

                if (_fireList.Count == 0) EndFire();
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
            // Only process cells we know are burning - MASSIVE optimization
            if (_burningCells.Count == 0) return;

            var toRemove = new List<(int x, int y)>(_burningCells.Count);

            foreach (var (x, y) in _burningCells)
            {
                int v = _grid[y, x];
                if (v <= RECENTLY_BURNED_STATE)
                {
                    v++;
                    if (v > RECENTLY_BURNED_STATE)
                    {
                        v = VACANT;
                        toRemove.Add((x, y));
                    }
                    _grid[y, x] = v;
                }
                else
                {
                    toRemove.Add((x, y));
                }
            }

            // Remove cells that are done burning
            foreach (var cell in toRemove)
            {
                _burningCells.Remove(cell);
            }
        }

        // --- Rendering (fast-ish bitmap write) ---
        private void DrawGrid()
        {
            if (_bmp.Width != GRID_SIZE || _bmp.Height != GRID_SIZE)
            {
                _bmp.Dispose();
                _bmp = new Bitmap(GRID_SIZE, GRID_SIZE, PixelFormat.Format32bppArgb);
            }

            var rect = new Rectangle(0, 0, GRID_SIZE, GRID_SIZE);
            BitmapData bd = _bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            int stride = bd.Stride;

            // Pre-calculate decay intensity lookup table
            Span<byte> decayR = stackalloc byte[BURN_DECAY_FRAMES];
            Span<byte> decayG = stackalloc byte[BURN_DECAY_FRAMES];
            Span<byte> decayB = stackalloc byte[BURN_DECAY_FRAMES];
            
            for (int i = 0; i < BURN_DECAY_FRAMES; i++)
            {
                double intensity = 1.0 - (i / (double)BURN_DECAY_FRAMES);
                decayR[i] = (byte)(COLOR_BURNOUT_RED * intensity);
                decayG[i] = (byte)(COLOR_BURNOUT_GREEN * intensity);
                decayB[i] = (byte)(COLOR_BURNOUT_BLUE * intensity);
            }

            // Optimized rendering loop
            for (int y = 0; y < GRID_SIZE; y++)
            {
                int rowStart = y * stride;
                for (int x = 0; x < GRID_SIZE; x++)
                {
                    int state = _grid[y, x];
                    byte r, g, b;

                    if (state == BURNING)
                    {
                        // flicker like their JS (g in 150..254)
                        r = COLOR_FIRE_RED;
                        g = (byte)(COLOR_FIRE_GREEN_MIN + _rng.NextInt(COLOR_FIRE_GREEN_RANGE));
                        b = COLOR_FIRE_BLUE;
                    }
                    else if (state <= RECENTLY_BURNED_STATE)
                    {
                        // Use pre-calculated decay values
                        int decayIndex = state - RECENTLY_BURNED_STATE + 1;
                        if (decayIndex >= 0 && decayIndex < BURN_DECAY_FRAMES)
                        {
                            r = decayR[decayIndex];
                            g = decayG[decayIndex];
                            b = decayB[decayIndex];
                        }
                        else
                        {
                            r = COLOR_VACANT.R;
                            g = COLOR_VACANT.G;
                            b = COLOR_VACANT.B;
                        }
                    }
                    else if (state == TREE)
                    {
                        r = COLOR_TREE.R;
                        g = COLOR_TREE.G;
                        b = COLOR_TREE.B;
                    }
                    else // VACANT
                    {
                        r = COLOR_VACANT.R;
                        g = COLOR_VACANT.G;
                        b = COLOR_VACANT.B;
                    }

                    int idx = rowStart + x * 4;
                    _pixelBuffer[idx + 0] = b;
                    _pixelBuffer[idx + 1] = g;
                    _pixelBuffer[idx + 2] = r;
                    _pixelBuffer[idx + 3] = 255;
                }
            }

            Marshal.Copy(_pixelBuffer, 0, bd.Scan0, _pixelBuffer.Length);
            _bmp.UnlockBits(bd);

            _picture.Image = _bmp;
        }

        private void UpdateStatsLabels()
        {
            _nsLabel.Text = $"Timesteps (Ns): {_Ns:n0}";
            _treesLabel.Text = $"Tree Count: {_treeCount:n0}";
            double density = (double)_treeCount / TOTAL_SITES * 100.0;
            _densityLabel.Text = $"Tree Density (ρ): {density:0.00}%";
            _firesLabel.Text = $"Total Fires: {_totalFires:n0}";
            
            UpdateFormTitle();
        }

        private void UpdateMaxFAndLabels()
        {
            // Their UI caps f at p/10
            double fMax = CurrentFMax();
            _f = TrackBarToF(_fBar.Value, fMax);

            _pLabel.Text = $"Tree Growth Probability (p): {_p:0.0000}";
            _fLabel.Text = $"Lightning Strike Probability (f): {_f:0.00e+0}   (max {fMax:0.00e+0})";
            _speedLabel.Text = $"Steps per Frame (Growth): {_stepsPerFrame:n0}";
        }

        private void UpdateFormTitle()
        {
            string status = _isSimulating ? "Running" : "Paused";
            double density = (double)_treeCount / TOTAL_SITES * 100.0;
            Text = $"Forest Fire Model (Drossel–Schwabl) — {status} | Trees: {_treeCount:n0} ({density:0.00}%) | Fires: {_totalFires:n0}";
        }

        private double CurrentFMax() => Math.Max(_p / 10.0, 1e-12);

        // Map TrackBar 0..100000 -> 0..fMax with more resolution near 0 (log-ish feel)
        private static double TrackBarToF(int v, double fMax)
        {
            double t = v / 100000.0;          // 0..1
            double curved = t * t * t;        // bias toward small values
            return curved * fMax;
        }
    }

    /// <summary>
    /// Simple fast seedable RNG (xorshift128+).
    /// Not cryptographic; perfect for sims.
    /// </summary>
    internal struct XorShift128Plus
    {
        private ulong _s0, _s1;

        public static XorShift128Plus FromString(string seed)
        {
            if (string.IsNullOrWhiteSpace(seed)) seed = "default";
            // FNV-1a 64-bit-ish hashing into two states
            ulong h1 = 1469598103934665603UL;
            ulong h2 = 1099511628211UL;

            foreach (char c in seed)
            {
                h1 ^= c;
                h1 *= 1099511628211UL;
                h2 += (ulong)c * 0x9E3779B97F4A7C15UL;
                h2 ^= (h2 >> 27);
            }

            // avoid all-zero state
            if (h1 == 0 && h2 == 0) h1 = 1;

            return new XorShift128Plus { _s0 = h1, _s1 = h2 };
        }

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

        public double NextDouble()
        {
            // 53 bits -> [0,1)
            return (NextU64() >> 11) * (1.0 / (1UL << 53));
        }

        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 1) return 0;
            // fast modulo; fine for UI sims
            return (int)(NextU64() % (uint)maxExclusive);
        }
    }
}
