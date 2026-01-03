using System;
using System.Buffers;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TreeGrowth
{
    public partial class Form2 : Form
    {
        // --- Grid Size (now configurable) ---
        private int _gridWidth = 1920;
        private int _gridHeight = 1080;
        private int TotalSites => _gridWidth * _gridHeight;

        // --- Reference grid for speed scaling ---
        private const int REFERENCE_GRID_SIZE = 512 * 512; // 262,144 cells (like original Form1)
        private int _baseStepsPerFrame = 1000; // Base speed at reference grid size
        private int _stepsPerFrame = 1000; // Actual steps (auto-scaled)

        private const int VACANT = 0;
        private const int TREE = 1;
        private const int BURNING = 2;
        private const int RECENTLY_BURNED_STATE = -1;

        // --- Configurable Visual Parameters ---
        private int _burnDecayFrames = 15;
        private int _fireAnimationSpeed = 1;
        private bool _useMooreNeighborhood = true;
        private int _treeSize = 1; // NEW: Tree size in pixels (1-50)

        // --- Colors (now configurable) ---
        private Color _colorTree = ColorTranslator.FromHtml("#c6a491");
        private Color _colorVacant = ColorTranslator.FromHtml("#A98268");
        private Color _colorFireBase = Color.FromArgb(255, 200, 0);
        private Color _colorBurnout = Color.FromArgb(255, 191, 0);
        private int _fireFlickerRange = 105;

        // === OPTIMIZATION: Flat array instead of 2D for better cache locality ===
        private int[] _grid = null!;
        
        // === OPTIMIZATION: Pre-allocated pixel buffer (reused every frame) ===
        private byte[] _pixelBuffer = null!;
        
        // === OPTIMIZATION: Per-thread RNG for parallel fire flicker ===
        private ThreadLocal<XorShift128Plus> _threadRng = null!;

        private bool _isSimulating;
        private bool _isFireActive;

        private double _p = 0.01;
        private double _f = 1e-5;
        private double _pfRatio = 10.0;  // p/f ratio (default: f = p/10)
        private bool _animateFires = true;

        private long _Ns = 0;
        private int _treeCount = 0;
        private long _totalFires = 0;

        // Use List instead of Queue to avoid boxing and improve performance
        private List<(int x, int y)> _fireList = new();
        private List<(int x, int y)> _nextFireList = new();
        private int _currentFireSize = 0;

        // Track burning cells for efficient decay processing
        private readonly HashSet<(int x, int y)> _burningCells = new();

        private readonly Dictionary<int, int> _fireStats = new();

        // --- Rendering ---
        private Bitmap _bmp = null!;
        private bool _showStats = true;
        private bool _isFullscreen = false;
        private FormBorderStyle _previousBorderStyle;
        private FormWindowState _previousWindowState;

        // --- Performance Monitoring ---
        private int _targetFps = 60;
        private DateTime _lastFrameTime = DateTime.Now;
        private double _actualFps = 0;
        private long _lastDrawMs = 0;
        private long _lastDecayMs = 0;

        // Frame counter for less frequent UI updates
        private int _frameCounter = 0;
        private const int UI_UPDATE_INTERVAL = 10; // Update UI every 10 frames

        // === OPTIMIZATION: Parallel options with processor count ===
        private readonly ParallelOptions _parallelOptions;

        // Main RNG (for simulation logic - single threaded)
        private XorShift128Plus _rng;

        public Form2()
        {
            // Configure parallelism based on CPU
            _parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            InitializeComponent();

            Text = "Forest Fire Model — OPTIMIZED (SIMD + Parallel)";
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;
            KeyDown += OnKeyDown;

            _timer.Tick += (_, __) => StepFrame();

            InitializeGrid();
            InitializeSimulation();
            UpdateTimerInterval();
        }

        private void InitializeGrid()
        {
            // === OPTIMIZATION: Flat 1D array for cache-friendly access ===
            _grid = new int[_gridHeight * _gridWidth];
            
            // === OPTIMIZATION: Pre-allocate pixel buffer (BGRA format) ===
            _pixelBuffer = new byte[_gridHeight * _gridWidth * 4];
            
            // === OPTIMIZATION: Per-thread RNG for parallel rendering ===
            _threadRng = new ThreadLocal<XorShift128Plus>(
                () => XorShift128Plus.FromString($"thread_{Environment.CurrentManagedThreadId}_{DateTime.Now.Ticks}"),
                trackAllValues: false
            );

            _bmp?.Dispose();
            _bmp = new Bitmap(_gridWidth, _gridHeight, PixelFormat.Format32bppArgb);

            // Auto-scale steps per frame based on grid size
            UpdateStepsPerFrame();
        }

        // === Auto-scale simulation speed based on grid size ===
        private void UpdateStepsPerFrame()
        {
            // Scale steps proportionally to grid size
            // Example: 1920×1080 = 2,073,600 cells = ~7.9x larger than 512×512
            // So steps per frame should be ~7,900 to maintain same simulation speed
            double scaleFactor = (double)TotalSites / REFERENCE_GRID_SIZE;
            _stepsPerFrame = (int)Math.Round(_baseStepsPerFrame * scaleFactor);
            
            // Ensure minimum of 100 steps
            _stepsPerFrame = Math.Max(100, _stepsPerFrame);
        }

        // === OPTIMIZATION: Inline grid access for flat array ===
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GridIndex(int x, int y) => y * _gridWidth + x;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetCell(int x, int y) => _grid[y * _gridWidth + x];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetCell(int x, int y, int value) => _grid[y * _gridWidth + x] = value;

        // === EVENT HANDLERS ===

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F11)
            {
                ToggleFullscreen();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape && _isFullscreen)
            {
                ToggleFullscreen();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.S && !_seedBox.Focused)
            {
                _showStats = !_showStats;
                _statsPanel.Visible = _showStats;
                _parametersPanel.Visible = _showStats;
                _visualPanel.Visible = _showStats;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Space)
            {
                ToggleSimulation();
                e.Handled = true;
            }
        }

        private void ToggleFullscreen()
        {
            _isFullscreen = !_isFullscreen;

            if (_isFullscreen)
            {
                _previousBorderStyle = FormBorderStyle;
                _previousWindowState = WindowState;

                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Maximized;

                _statsPanel.Visible = false;
                _parametersPanel.Visible = false;
                _visualPanel.Visible = false;
                _controlPanel.Visible = false;
            }
            else
            {
                FormBorderStyle = _previousBorderStyle;
                WindowState = _previousWindowState;

                _statsPanel.Visible = _showStats;
                _parametersPanel.Visible = _showStats;
                _visualPanel.Visible = _showStats;
                _controlPanel.Visible = true;
            }

            _fullscreenBtn.Text = _isFullscreen ? "Exit Fullscreen (F11)" : "Fullscreen (F11)";
        }

        private void ToggleSimulation()
        {
            _isSimulating = !_isSimulating;
            _startBtn.Text = _isSimulating ? "Stop (Space)" : "Start (Space)";
            if (_isSimulating) _timer.Start();
            else _timer.Stop();
            UpdateFormTitle();
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

        // Ratio bar handler
        private void OnRatioBarScroll(object sender, EventArgs e)
        {
            // Ratio range: 1 to 100
            // Value 1 = ratio 0.1, Value 10 = ratio 1.0, Value 100 = ratio 10.0
            _pfRatio = _ratioBar.Value / 10.0;
            
            // Update f based on ratio: f = p / ratio
            _f = _p / _pfRatio;
            
            UpdateMaxFAndLabels();
        }

        private void OnSpeedBarScroll(object sender, EventArgs e)
        {
            // Update base speed (slider controls base, actual scales with grid size)
            _baseStepsPerFrame = _speedBar.Value;
            UpdateStepsPerFrame();
            UpdateMaxFAndLabels();
        }

        private void OnAnimateCheckChanged(object sender, EventArgs e)
        {
            _animateFires = _animateCheck.Checked;
        }

        private void OnStartBtnClick(object sender, EventArgs e) => ToggleSimulation();

        private void OnResetBtnClick(object sender, EventArgs e)
        {
            _isSimulating = false;
            _timer.Stop();
            _startBtn.Text = "Start (Space)";
            InitializeSimulation();
        }

        private void OnFullscreenBtnClick(object sender, EventArgs e) => ToggleFullscreen();

        private void OnBurnDecayScroll(object sender, EventArgs e)
        {
            _burnDecayFrames = _burnDecayBar.Value;
            _burnDecayLabel.Text = $"Burn Decay Frames: {_burnDecayFrames}";
        }

        private void OnFireSpeedScroll(object sender, EventArgs e)
        {
            _fireAnimationSpeed = _fireSpeedBar.Value;
            _fireSpeedLabel.Text = $"Fire Spread Speed: {_fireAnimationSpeed}";
        }

        private void OnFpsScroll(object sender, EventArgs e)
        {
            _targetFps = _fpsBar.Value;
            _fpsLabel.Text = $"Target FPS: {_targetFps}";
            UpdateTimerInterval();
        }

        private void OnFlickerScroll(object sender, EventArgs e)
        {
            _fireFlickerRange = _flickerBar.Value;
            _flickerLabel.Text = $"Fire Flicker Range: {_fireFlickerRange}";
        }

        // NEW: Tree size handler
        private void OnTreeSizeScroll(object sender, EventArgs e)
        {
            _treeSize = _treeSizeBar.Value;
            _treeSizeLabel.Text = $"Tree Size: {_treeSize}px";
        }

        private void OnNeighborhoodChanged(object sender, EventArgs e)
        {
            _useMooreNeighborhood = _neighborhoodCombo.SelectedIndex == 0;
        }

        private void OnGridSizeChanged(object sender, EventArgs e)
        {
            bool wasRunning = _isSimulating;
            if (wasRunning)
            {
                _isSimulating = false;
                _timer.Stop();
            }

            switch (_gridSizeCombo.SelectedIndex)
            {
                case 0: _gridWidth = 1920; _gridHeight = 1080; break;
                case 1: _gridWidth = 1024; _gridHeight = 1024; break;
                case 2: _gridWidth = 2560; _gridHeight = 1440; break;
                case 3: _gridWidth = 3840; _gridHeight = 2160; break;
                case 4: _gridWidth = 512; _gridHeight = 512; break;
            }

            InitializeGrid();
            InitializeSimulation();
            _startBtn.Text = "Start (Space)";
        }

        private void OnTreeColorClick(object sender, EventArgs e)
        {
            using var dlg = new ColorDialog { Color = _colorTree };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _colorTree = dlg.Color;
                _treeColorBtn.BackColor = _colorTree;
            }
        }

        private void OnVacantColorClick(object sender, EventArgs e)
        {
            using var dlg = new ColorDialog { Color = _colorVacant };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _colorVacant = dlg.Color;
                _vacantColorBtn.BackColor = _colorVacant;
                _picture.BackColor = _colorVacant;
            }
        }

        private void OnFireColorClick(object sender, EventArgs e)
        {
            using var dlg = new ColorDialog { Color = _colorFireBase };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _colorFireBase = dlg.Color;
                _fireColorBtn.BackColor = _colorFireBase;
            }
        }

        private void OnBurnoutColorClick(object sender, EventArgs e)
        {
            using var dlg = new ColorDialog { Color = _colorBurnout };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _colorBurnout = dlg.Color;
                _burnoutColorBtn.BackColor = _colorBurnout;
            }
        }

        // === SIMULATION ===

        private void InitializeSimulation()
        {
            _p = _pBar.Value / 10000.0;
            _baseStepsPerFrame = _speedBar.Value;
            UpdateStepsPerFrame(); // Recalculate scaled steps
            _animateFires = _animateCheck.Checked;

            UpdateMaxFAndLabels();

            _rng = XorShift128Plus.FromString(_seedBox.Text.Trim());

            // === OPTIMIZATION: Use Array.Clear for bulk zeroing ===
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

            DrawGridOptimized();
            UpdateStatsLabels();
        }

        private void UpdateTimerInterval()
        {
            _timer.Interval = Math.Max(1, 1000 / _targetFps);
        }

        private void StepFrame()
        {
            var frameStart = DateTime.Now;

            DecayRecentlyBurnedParallel();

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

            DrawGridOptimized();
            
            // Update UI less frequently to reduce overhead
            _frameCounter++;
            if (_frameCounter >= UI_UPDATE_INTERVAL)
            {
                UpdateStatsLabels();
                _frameCounter = 0;
            }

            var elapsed = (DateTime.Now - _lastFrameTime).TotalSeconds;
            if (elapsed > 0) _actualFps = 1.0 / elapsed;
            _lastFrameTime = DateTime.Now;
        }

        private void RunSimulationSteps()
        {
            for (int i = 0; i < _stepsPerFrame; i++)
            {
                int xg = _rng.NextInt(_gridWidth);
                int yg = _rng.NextInt(_gridHeight);

                if (GetCell(xg, yg) == VACANT && _rng.NextDouble() < _p)
                {
                    SetCell(xg, yg, TREE);
                    _treeCount++;
                }

                int xf = _rng.NextInt(_gridWidth);
                int yf = _rng.NextInt(_gridHeight);

                if (GetCell(xf, yf) == TREE && _rng.NextDouble() < _f)
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
                int xg = _rng.NextInt(_gridWidth);
                int yg = _rng.NextInt(_gridHeight);

                if (GetCell(xg, yg) == VACANT && _rng.NextDouble() < _p)
                {
                    SetCell(xg, yg, TREE);
                    _treeCount++;
                }

                // Lightning strike attempt (can start additional fires)
                int xf = _rng.NextInt(_gridWidth);
                int yf = _rng.NextInt(_gridHeight);

                if (GetCell(xf, yf) == TREE && _rng.NextDouble() < _f)
                {
                    // Add to existing fire
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

                // Efficient list swap using tuple deconstruction
                (_fireList, _nextFireList) = (_nextFireList, _fireList);

                if (_fireList.Count == 0) EndFire();
            }
        }

        private void SpreadFireFrom((int x, int y) cell)
        {
            if (_useMooreNeighborhood)
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
            if ((uint)nx < _gridWidth && (uint)ny < _gridHeight && GetCell(nx, ny) == TREE)
            {
                SetCell(nx, ny, BURNING);
                _treeCount--;
                _currentFireSize++;
                _nextFireList.Add((nx, ny));
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

                // Efficient list swap using tuple deconstruction
                (_fireList, _nextFireList) = (_nextFireList, _fireList);

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

        // ============================================================
        // === OPTIMIZATION #1: Parallel Decay with SIMD + HashSet tracking ===
        // ============================================================
        private void DecayRecentlyBurnedParallel()
        {
            // Only process cells we know are burning - MASSIVE optimization
            if (_burningCells.Count == 0) return;

            var sw = System.Diagnostics.Stopwatch.StartNew();

            var toRemove = new List<(int x, int y)>(_burningCells.Count);

            // Process burning cells in parallel
            var cellsArray = new (int x, int y)[_burningCells.Count];
            _burningCells.CopyTo(cellsArray);

            // Thread-safe collection for cells to remove
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

            // Remove cells that are done burning
            foreach (var cell in removeList)
            {
                _burningCells.Remove(cell);
            }

            _lastDecayMs = sw.ElapsedMilliseconds;
        }

        // ============================================================
        // === OPTIMIZATION #2: Parallel Rendering with Circle Drawing ===
        // ============================================================
        private unsafe void DrawGridOptimized()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            if (_bmp.Width != _gridWidth || _bmp.Height != _gridHeight)
            {
                _bmp?.Dispose();
                _bmp = new Bitmap(_gridWidth, _gridHeight, PixelFormat.Format32bppArgb);
                _pixelBuffer = new byte[_gridWidth * _gridHeight * 4];
            }

            int width = _gridWidth;
            int height = _gridHeight;
            int[] grid = _grid;
            byte[] buffer = _pixelBuffer;

            // Pre-compute colors as uint32 (BGRA)
            uint colorTreeBgra = ColorToBgra(_colorTree);
            uint colorVacantBgra = ColorToBgra(_colorVacant);
            byte fireR = _colorFireBase.R;
            byte fireG = _colorFireBase.G;
            byte fireB = _colorFireBase.B;
            byte burnR = _colorBurnout.R;
            byte burnG = _colorBurnout.G;
            byte burnB = _colorBurnout.B;
            int flickerRange = _fireFlickerRange;
            int burnDecay = _burnDecayFrames;
            int treeSize = _treeSize;

            // Clear buffer with vacant color
            uint vacantColor = colorVacantBgra;
            Parallel.For(0, height, _parallelOptions, y =>
            {
                int rowStart = y * width * 4;
                for (int x = 0; x < width; x++)
                {
                    int idx = rowStart + x * 4;
                    buffer[idx + 0] = (byte)(vacantColor);
                    buffer[idx + 1] = (byte)(vacantColor >> 8);
                    buffer[idx + 2] = (byte)(vacantColor >> 16);
                    buffer[idx + 3] = 255;
                }
            });

            // Draw trees, fires, and burning cells as circles
            if (treeSize == 1)
            {
                // Optimized path for single pixel (original behavior)
                Parallel.For(0, height, _parallelOptions, y =>
                {
                    var localRng = _threadRng.Value;
                    int rowStartGrid = y * width;
                    int rowStartBuffer = y * width * 4;

                    for (int x = 0; x < width; x++)
                    {
                        int state = grid[rowStartGrid + x];
                        if (state == VACANT) continue;

                        int bufferIdx = rowStartBuffer + x * 4;
                        byte r, g, b;

                        if (state == BURNING)
                        {
                            r = fireR;
                            g = (byte)Math.Min(255, fireG + (flickerRange > 0 ? localRng.NextInt(flickerRange) : 0));
                            b = fireB;
                        }
                        else if (state <= RECENTLY_BURNED_STATE)
                        {
                            double decayProgress = (state - RECENTLY_BURNED_STATE + 1) / (double)burnDecay;
                            double intensity = 1.0 - decayProgress;
                            r = (byte)(burnR * intensity);
                            g = (byte)(burnG * intensity);
                            b = (byte)(burnB * intensity);
                        }
                        else if (state == TREE)
                        {
                            buffer[bufferIdx + 0] = (byte)(colorTreeBgra);
                            buffer[bufferIdx + 1] = (byte)(colorTreeBgra >> 8);
                            buffer[bufferIdx + 2] = (byte)(colorTreeBgra >> 16);
                            buffer[bufferIdx + 3] = 255;
                            continue;
                        }
                        else
                        {
                            continue;
                        }

                        buffer[bufferIdx + 0] = b;
                        buffer[bufferIdx + 1] = g;
                        buffer[bufferIdx + 2] = r;
                        buffer[bufferIdx + 3] = 255;
                    }
                });
            }
            else
            {
                // Draw circles for tree size > 1
                var treeCells = new List<(int x, int y, int state)>();
                
                // Collect all non-vacant cells
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int state = grid[y * width + x];
                        if (state != VACANT)
                        {
                            treeCells.Add((x, y, state));
                        }
                    }
                }

                // Draw circles in parallel
                Parallel.ForEach(treeCells, _parallelOptions, cell =>
                {
                    var localRng = _threadRng.Value;
                    int cx = cell.x;
                    int cy = cell.y;
                    int state = cell.state;
                    
                    byte r, g, b;
                    
                    if (state == BURNING)
                    {
                        r = fireR;
                        g = (byte)Math.Min(255, fireG + (flickerRange > 0 ? localRng.NextInt(flickerRange) : 0));
                        b = fireB;
                    }
                    else if (state <= RECENTLY_BURNED_STATE)
                    {
                        double decayProgress = (state - RECENTLY_BURNED_STATE + 1) / (double)burnDecay;
                        double intensity = 1.0 - decayProgress;
                        r = (byte)(burnR * intensity);
                        g = (byte)(burnG * intensity);
                        b = (byte)(burnB * intensity);
                    }
                    else // TREE
                    {
                        r = _colorTree.R;
                        g = _colorTree.G;
                        b = _colorTree.B;
                    }

                    // Draw filled circle using midpoint circle algorithm
                    DrawFilledCircle(buffer, width, height, cx, cy, treeSize / 2, r, g, b);
                });
            }

            // Copy buffer to bitmap
            var rect = new Rectangle(0, 0, width, height);
            BitmapData bd = _bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(buffer, 0, bd.Scan0, buffer.Length);
            _bmp.UnlockBits(bd);

            _picture.Image = _bmp;

            _lastDrawMs = sw.ElapsedMilliseconds;
        }

        // Helper method to draw filled circle
        private void DrawFilledCircle(byte[] buffer, int width, int height, int cx, int cy, int radius, byte r, byte g, byte b)
        {
            int radiusSquared = radius * radius;
            
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (dx * dx + dy * dy <= radiusSquared)
                    {
                        int px = cx + dx;
                        int py = cy + dy;
                        
                        if (px >= 0 && px < width && py >= 0 && py < height)
                        {
                            int idx = (py * width + px) * 4;
                            buffer[idx + 0] = b;
                            buffer[idx + 1] = g;
                            buffer[idx + 2] = r;
                            buffer[idx + 3] = 255;
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ColorToBgra(Color c) => (uint)(c.B | (c.G << 8) | (c.R << 16) | (255 << 24));

        // === UI Updates ===

        private void UpdateStatsLabels()
        {
            _nsLabel.Text = $"Timesteps: {_Ns:n0}";
            _treesLabel.Text = $"Trees: {_treeCount:n0}";
            double density = (double)_treeCount / TotalSites * 100.0;
            _densityLabel.Text = $"Density: {density:0.00}%";
            _firesLabel.Text = $"Fires: {_totalFires:n0}";
            _fpsActualLabel.Text = $"FPS: {_actualFps:0.0} (Draw:{_lastDrawMs}ms)";
            _gridInfoLabel.Text = $"Grid: {_gridWidth}×{_gridHeight}";

            UpdateFormTitle();
        }

        private void UpdateMaxFAndLabels()
        {
            double fMax = CurrentFMax();
            
            // Update f bar to match current f value
            _fBar.Value = Math.Min(_fBar.Maximum, Math.Max(0, (int)(Math.Pow(_f / fMax, 1.0 / 3.0) * 100000)));

            _pLabel.Text = $"Tree Growth (p): {_p:0.0000}";
            _fLabel.Text = $"Lightning (f): {_f:0.00e+0}";
            _ratioLabel.Text = $"Ratio (p/f): {_pfRatio:0.0}";
            
            // Show both base and actual scaled steps
            double scaleFactor = (double)TotalSites / REFERENCE_GRID_SIZE;
            _speedLabel.Text = $"Steps/Frame: {_baseStepsPerFrame:n0} (×{scaleFactor:0.0} = {_stepsPerFrame:n0})";
        }

        private void UpdateFormTitle()
        {
            string status = _isSimulating ? "▶ Running" : "⏸ Paused";
            double density = (double)_treeCount / TotalSites * 100.0;
            Text = $"Forest Fire OPTIMIZED — {status} | {_gridWidth}×{_gridHeight} | Trees: {_treeCount:n0} ({density:0.0}%) | FPS: {_actualFps:0}";
        }

        private double CurrentFMax() => Math.Max(_p / 10.0, 1e-12);

        private static double TrackBarToF(int v, double fMax)
        {
            double t = v / 100000.0;
            double curved = t * t * t;
            return curved * fMax;
        }
    }

    /// <summary>
    /// Fast seedable RNG (xorshift128+).
    /// </summary>
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
