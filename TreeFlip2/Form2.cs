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
        // ============================================================
        // === GRID CONFIGURATION ===
        // ============================================================
        
        // Output resolution (what gets displayed)
        private int _outputWidth = 1920;
        private int _outputHeight = 1080;
        
        // === NEW: Cell Size Multiplier for scaling ===
        // Cell size determines how many output pixels represent one logical cell
        // cellSize=1: 1920x1080 logical cells (fine detail)
        // cellSize=4: 480x270 logical cells (chunky, organic look)
        // cellSize=8: 240x135 logical cells (very chunky, great for LED walls)
        private int _cellSize = 1;
        
        // Logical grid dimensions (computed from output / cellSize)
        private int _logicalWidth => _outputWidth / _cellSize;
        private int _logicalHeight => _outputHeight / _cellSize;
        private int TotalLogicalCells => _logicalWidth * _logicalHeight;
        
        // Compatibility properties for existing code
        private int _gridWidth => _outputWidth;
        private int _gridHeight => _outputHeight;
        private int TotalSites => TotalLogicalCells;

        // --- Reference grid for speed scaling ---
        private const int REFERENCE_GRID_SIZE = 512 * 512; // 262,144 cells
        private int _baseStepsPerFrame = 1000;
        private int _stepsPerFrame = 1000;

        // Cell states
        private const int VACANT = 0;
        private const int TREE = 1;
        private const int BURNING = 2;
        private const int RECENTLY_BURNED_STATE = -1;

        // --- Visual Parameters ---
        private int _burnDecayFrames = 15;
        private int _fireAnimationSpeed = 1;
        private bool _useMooreNeighborhood = true;

        // ============================================================
        // === COLOR CONFIGURATION ===
        // ============================================================
        
        private Color _colorTree = ColorTranslator.FromHtml("#c6a491");
        private Color _colorVacant = ColorTranslator.FromHtml("#A98268");
        private Color _colorFireBase = Color.FromArgb(255, 200, 0);
        private Color _colorBurnout = Color.FromArgb(255, 191, 0);
        private int _fireFlickerRange = 105;

        // === NEW: Color Presets ===
        public enum ColorPreset
        {
            Warm,           // Original warm tones
            Atmosphere,     // Sky blue background
            Forest,         // Natural forest green
            Night,          // Dark mode with bright fire
            ANWB,           // ANWB brand colors
            Infrared,       // Heat map style
            Ocean,          // Blue-green palette
            Monochrome      // Black and white with orange fire
        }

        private static readonly Dictionary<ColorPreset, (Color tree, Color vacant, Color fire, Color burnout)> _presets = new()
        {
            { ColorPreset.Warm, (
                ColorTranslator.FromHtml("#c6a491"),  // tree: warm beige
                ColorTranslator.FromHtml("#A98268"),  // vacant: darker beige
                Color.FromArgb(255, 200, 0),          // fire: orange-yellow
                Color.FromArgb(255, 191, 0)           // burnout: amber glow
            )},
            { ColorPreset.Atmosphere, (
                ColorTranslator.FromHtml("#2D5016"),  // tree: forest green
                ColorTranslator.FromHtml("#87CEEB"),  // vacant: sky blue
                Color.FromArgb(255, 100, 0),          // fire: deep orange
                Color.FromArgb(255, 50, 0)            // burnout: red glow
            )},
            { ColorPreset.Forest, (
                ColorTranslator.FromHtml("#228B22"),  // tree: forest green
                ColorTranslator.FromHtml("#8B4513"),  // vacant: saddle brown (earth)
                Color.FromArgb(255, 140, 0),          // fire: dark orange
                Color.FromArgb(255, 69, 0)            // burnout: red-orange
            )},
            { ColorPreset.Night, (
                ColorTranslator.FromHtml("#1a472a"),  // tree: dark green
                ColorTranslator.FromHtml("#0d1117"),  // vacant: near black
                Color.FromArgb(255, 200, 50),         // fire: bright yellow
                Color.FromArgb(255, 100, 0)           // burnout: orange
            )},
            { ColorPreset.ANWB, (
                ColorTranslator.FromHtml("#FFD100"),  // tree: ANWB yellow
                ColorTranslator.FromHtml("#003082"),  // vacant: ANWB blue
                Color.FromArgb(255, 255, 255),        // fire: white
                Color.FromArgb(255, 200, 0)           // burnout: yellow glow
            )},
            { ColorPreset.Infrared, (
                ColorTranslator.FromHtml("#00FF00"),  // tree: bright green (cold)
                ColorTranslator.FromHtml("#000080"),  // vacant: navy (coldest)
                Color.FromArgb(255, 0, 0),            // fire: red (hot)
                Color.FromArgb(255, 255, 0)           // burnout: yellow (warm)
            )},
            { ColorPreset.Ocean, (
                ColorTranslator.FromHtml("#20B2AA"),  // tree: light sea green
                ColorTranslator.FromHtml("#191970"),  // vacant: midnight blue
                Color.FromArgb(255, 127, 80),         // fire: coral
                Color.FromArgb(255, 99, 71)           // burnout: tomato
            )},
            { ColorPreset.Monochrome, (
                ColorTranslator.FromHtml("#FFFFFF"),  // tree: white
                ColorTranslator.FromHtml("#1a1a1a"),  // vacant: near black
                Color.FromArgb(255, 140, 0),          // fire: orange
                Color.FromArgb(200, 100, 0)           // burnout: dark orange
            )}
        };

        // ============================================================
        // === BLUR/BLOOM EFFECT ===
        // ============================================================
        
        private bool _enableBloom = false;
        private int _bloomRadius = 2;        // Blur kernel radius (1-5)
        private float _bloomIntensity = 0.5f; // How much bloom to blend (0-1)
        private bool _bloomFireOnly = true;   // Only bloom fire/glow, not trees
        
        // Pre-allocated blur buffers
        private byte[] _blurBuffer = null!;
        private float[] _bloomKernel = null!;

        // ============================================================
        // === GRID DATA ===
        // ============================================================
        
        private int[] _grid = null!;
        private byte[] _pixelBuffer = null!;
        private ThreadLocal<XorShift128Plus> _threadRng = null!;

        private bool _isSimulating;
        private bool _isFireActive;

        private double _p = 0.01;
        private double _f = 1e-5;
        private double _pfRatio = 10.0;
        private bool _animateFires = true;

        private long _Ns = 0;
        private int _treeCount = 0;
        private long _totalFires = 0;
        private long _totalTreeAge = 0; // For compatibility with existing code

        private List<(int x, int y)> _fireList = new();
        private List<(int x, int y)> _nextFireList = new();
        private int _currentFireSize = 0;

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
        private long _lastBloomMs = 0;

        private int _frameCounter = 0;
        private const int UI_UPDATE_INTERVAL = 10;

        // Guard flag to prevent event handlers during initialization
        private bool _isInitializing = true;

        private readonly ParallelOptions _parallelOptions;
        private XorShift128Plus _rng;

        // === NDI Streaming ===
        private NdiSender? _ndiSender;
        private bool _ndiEnabled = false;
        private readonly string _ndiSourceName = "Forest Fire Simulation";

        public Form2()
        {
            _parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            InitializeComponent();

            // === Initialize ComboBoxes with default values ===    
            // Note: _isInitializing = true prevents event handlers from triggering during this
            _neighborhoodCombo.SelectedIndex = 0;  // 8 Neighbors
            _gridSizeCombo.SelectedIndex = 0;      // 1920×1080 HD
            _cellSizeCombo.SelectedIndex = 0;      // 1×1 Fine
            _presetCombo.SelectedIndex = 0;        // Warm

            Text = "Forest Fire Model — OPTIMIZED (Cell Scaling + Bloom)";
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;
            KeyDown += OnKeyDown;

            _timer.Tick += (_, __) => StepFrame();

            InitializeGrid();
            InitializeBloomKernel();
            InitializeSimulation();
            UpdateTimerInterval();
            
            _isInitializing = false;  // Now event handlers can work normally
        }

        // ============================================================
        // === GRID INITIALIZATION ===
        // ============================================================

        private void InitializeGrid()
        {
            // Logical grid (simulation runs on this)
            _grid = new int[_logicalHeight * _logicalWidth];
            
            // Pixel buffer for output resolution
            _pixelBuffer = new byte[_outputHeight * _outputWidth * 4];
            
            // Blur buffer (same size as pixel buffer)
            _blurBuffer = new byte[_outputHeight * _outputWidth * 4];
            
            // Dispose old ThreadLocal to prevent memory leak
            _threadRng?.Dispose();
            _threadRng = new ThreadLocal<XorShift128Plus>(
                () => XorShift128Plus.FromString($"thread_{Environment.CurrentManagedThreadId}_{DateTime.Now.Ticks}"),
                trackAllValues: false
            );

            _bmp?.Dispose();
            _bmp = new Bitmap(_outputWidth, _outputHeight, PixelFormat.Format32bppArgb);

            UpdateStepsPerFrame();
        }

        private void InitializeBloomKernel()
        {
            // Generate Gaussian kernel for bloom
            int size = _bloomRadius * 2 + 1;
            _bloomKernel = new float[size];
            float sigma = Math.Max(1.0f, _bloomRadius / 2.0f); // Minimum sigma of 1.0 to avoid too narrow kernel
            float sum = 0;
            
            for (int i = 0; i < size; i++)
            {
                int x = i - _bloomRadius;
                _bloomKernel[i] = (float)Math.Exp(-(x * x) / (2 * sigma * sigma));
                sum += _bloomKernel[i];
            }
            
            // Normalize
            for (int i = 0; i < size; i++)
                _bloomKernel[i] /= sum;
        }

        private void UpdateStepsPerFrame()
        {
            // Scale based on LOGICAL grid size (not output resolution)
            double scaleFactor = (double)TotalLogicalCells / REFERENCE_GRID_SIZE;
            _stepsPerFrame = (int)Math.Round(_baseStepsPerFrame * scaleFactor);
            _stepsPerFrame = Math.Max(100, _stepsPerFrame);
        }

        // ============================================================
        // === GRID ACCESS (Logical coordinates) ===
        // ============================================================

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GridIndex(int x, int y) => y * _logicalWidth + x;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetCell(int x, int y) => _grid[y * _logicalWidth + x];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetCell(int x, int y, int value) => _grid[y * _logicalWidth + x] = value;

        // ============================================================
        // === HELPER METHODS ===
        // ============================================================

        private double CurrentFMax() => Math.Max(_p / 10.0, 1e-12);

        private static double TrackBarToF(int v, double fMax)
        {
            double t = v / 100000.0;
            double curved = t * t * t;
            return curved * fMax;
        }

        // ============================================================
        // === EVENT HANDLERS ===
        // ============================================================

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
            else if (e.KeyCode == Keys.N && e.Control)
            {
                // Ctrl+N: Toggle NDI
                ToggleNDI();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.S && !_seedBox.Focused && !e.Control)
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
            else if (e.KeyCode == Keys.B)
            {
                _enableBloom = !_enableBloom;
                _bloomCheck.Checked = _enableBloom;
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
                _bloomPanel.Visible = false;  // NEW: Hide bloom panel in fullscreen
            }
            else
            {
                FormBorderStyle = _previousBorderStyle;
                WindowState = _previousWindowState;

                _statsPanel.Visible = _showStats;
                _parametersPanel.Visible = _showStats;
                _visualPanel.Visible = _showStats;
                _controlPanel.Visible = true;
                _bloomPanel.Visible = _showStats;  // NEW: Restore bloom panel visibility
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

        private void OnRatioBarScroll(object sender, EventArgs e)
        {
            _pfRatio = _ratioBar.Value / 10.0;
            _f = _p / _pfRatio;
            UpdateMaxFAndLabels();
        }

        private void OnSpeedBarScroll(object sender, EventArgs e)
        {
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
            _burnDecayLabel.Text = $"Burn Decay: {_burnDecayFrames}";
        }

        private void OnFireSpeedScroll(object sender, EventArgs e)
        {
            _fireAnimationSpeed = _fireSpeedBar.Value;
            _fireSpeedLabel.Text = $"Fire Speed: {_fireAnimationSpeed}";
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
            _flickerLabel.Text = $"Flicker: {_fireFlickerRange}";
        }

        private void OnNeighborhoodChanged(object sender, EventArgs e)
        {
            _useMooreNeighborhood = _neighborhoodCombo.SelectedIndex == 0;
        }

        // === NEW: Cell Size Handler ===
        private void OnCellSizeChanged(object sender, EventArgs e)
        {
            if (_isInitializing) return;  // Skip during initialization
            
            bool wasRunning = _isSimulating;
            if (wasRunning)
            {
                _isSimulating = false;
                _timer.Stop();
            }

            _cellSize = _cellSizeCombo.SelectedIndex switch
            {
                0 => 1,   // 1x1 (Fine)
                1 => 2,   // 2x2
                2 => 4,   // 4x4
                3 => 8,   // 8x8 (Chunky)
                4 => 16,  // 16x16 (Very Chunky)
                _ => 1
            };

            InitializeGrid();
            InitializeSimulation();
            _startBtn.Text = "Start (Space)";
        }

        private void OnGridSizeChanged(object sender, EventArgs e)
        {
            if (_isInitializing) return;  // Skip during initialization
            
            bool wasRunning = _isSimulating;
            if (wasRunning)
            {
                _isSimulating = false;
                _timer.Stop();
            }

            switch (_gridSizeCombo.SelectedIndex)
            {
                case 0: _outputWidth = 1920; _outputHeight = 1080; break;
                case 1: _outputWidth = 1024; _outputHeight = 1024; break;
                case 2: _outputWidth = 2560; _outputHeight = 1440; break;
                case 3: _outputWidth = 3840; _outputHeight = 2160; break;
                case 4: _outputWidth = 512; _outputHeight = 512; break;
            }

            InitializeGrid();
            InitializeSimulation();
            _startBtn.Text = "Start (Space)";
        }

        // === Color Handlers ===
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

        // === NEW: Preset Handler ===
        private void OnPresetChanged(object sender, EventArgs e)
        {
            if (_presetCombo.SelectedIndex < 0) return;
            
            var preset = (ColorPreset)_presetCombo.SelectedIndex;
            if (_presets.TryGetValue(preset, out var colors))
            {
                _colorTree = colors.tree;
                _colorVacant = colors.vacant;
                _colorFireBase = colors.fire;
                _colorBurnout = colors.burnout;
                
                // Update UI buttons
                _treeColorBtn.BackColor = _colorTree;
                _vacantColorBtn.BackColor = _colorVacant;
                _fireColorBtn.BackColor = _colorFireBase;
                _burnoutColorBtn.BackColor = _colorBurnout;
                _picture.BackColor = _colorVacant;
            }
        }

        // === NEW: Bloom Handlers ===
        private void OnBloomCheckChanged(object sender, EventArgs e)
        {
            _enableBloom = _bloomCheck.Checked;
        }

        private void OnBloomRadiusScroll(object sender, EventArgs e)
        {
            _bloomRadius = _bloomRadiusBar.Value;
            _bloomRadiusLabel.Text = $"Bloom Radius: {_bloomRadius}";
            InitializeBloomKernel();
        }

        private void OnBloomIntensityScroll(object sender, EventArgs e)
        {
            _bloomIntensity = _bloomIntensityBar.Value / 100f;
            _bloomIntensityLabel.Text = $"Bloom Intensity: {_bloomIntensity:P0}";
        }

        private void OnBloomFireOnlyChanged(object sender, EventArgs e)
        {
            _bloomFireOnly = _bloomFireOnlyCheck.Checked;
        }

        // ============================================================
        // === SIMULATION ===
        // ============================================================

        private void InitializeSimulation()
        {
            _p = _pBar.Value / 10000.0;
            _baseStepsPerFrame = _speedBar.Value;
            UpdateStepsPerFrame();
            _animateFires = _animateCheck.Checked;

            UpdateMaxFAndLabels();

            _rng = XorShift128Plus.FromString(_seedBox.Text.Trim());

            Array.Clear(_grid, 0, _grid.Length);

            _Ns = 0;
            _treeCount = 0;
            _totalFires = 0;
            _totalTreeAge = 0;
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
                RunTreeGrowth();
                if (_animateFires) RunFireStepAnimated();
                else RunFireInstantly();
            }
            else
            {
                RunSimulationSteps();
            }

            DrawGridOptimized();
            
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
                int xg = _rng.NextInt(_logicalWidth);
                int yg = _rng.NextInt(_logicalHeight);

                if (GetCell(xg, yg) == VACANT && _rng.NextDouble() < _p)
                {
                    SetCell(xg, yg, TREE);
                    _treeCount++;
                }

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
                int xg = _rng.NextInt(_logicalWidth);
                int yg = _rng.NextInt(_logicalHeight);

                if (GetCell(xg, yg) == VACANT && _rng.NextDouble() < _p)
                {
                    SetCell(xg, yg, TREE);
                    _treeCount++;
                }

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
            if ((uint)nx < _logicalWidth && (uint)ny < _logicalHeight && GetCell(nx, ny) == TREE)
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
        // === PARALLEL DECAY ===
        // ============================================================

        private void DecayRecentlyBurnedParallel()
        {
            if (_burningCells.Count == 0) return;

            var sw = System.Diagnostics.Stopwatch.StartNew();

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

            _lastDecayMs = sw.ElapsedMilliseconds;
        }

        // ============================================================
        // === OPTIMIZED RENDERING WITH CELL SCALING ===
        // ============================================================

        private unsafe void DrawGridOptimized()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            if (_bmp.Width != _outputWidth || _bmp.Height != _outputHeight)
            {
                _bmp?.Dispose();
                _bmp = new Bitmap(_outputWidth, _outputHeight, PixelFormat.Format32bppArgb);
                _pixelBuffer = new byte[_outputWidth * _outputHeight * 4];
                _blurBuffer = new byte[_outputWidth * _outputHeight * 4];
            }

            int outputW = _outputWidth;
            int outputH = _outputHeight;
            int logicalW = _logicalWidth;
            int logicalH = _logicalHeight;
            int cellSize = _cellSize;
            int[] grid = _grid;
            byte[] buffer = _pixelBuffer;

            // Pre-compute colors
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

            // === RENDERING WITH CELL SIZE SCALING ===
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

            _lastDrawMs = sw.ElapsedMilliseconds;

            // === APPLY BLOOM IF ENABLED ===
            if (_enableBloom && _bloomRadius > 0)
            {
                var bloomSw = System.Diagnostics.Stopwatch.StartNew();
                ApplyBloom(buffer, _blurBuffer, outputW, outputH);
                _lastBloomMs = bloomSw.ElapsedMilliseconds;
            }

            // === Send NDI frame if enabled ===
            if (_ndiEnabled && _ndiSender != null)
            {
                try
                {
                    _ndiSender.SendFrame(_enableBloom ? _blurBuffer : buffer, outputW, outputH);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"NDI send error: {ex.Message}");
                }
            }

            // Copy buffer to bitmap
            var rect = new Rectangle(0, 0, outputW, outputH);
            BitmapData bd = _bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(_enableBloom ? _blurBuffer : buffer, 0, bd.Scan0, buffer.Length);
            _bmp.UnlockBits(bd);

            _picture.Image = _bmp;
        }

        // ============================================================
        // === BLOOM/BLUR EFFECT ===
        // ============================================================

        private void ApplyBloom(byte[] src, byte[] dst, int width, int height)
        {
            // Two-pass separable Gaussian blur for performance
            // First pass: horizontal blur into temp buffer
            // Second pass: vertical blur into dst buffer
            
            byte[] temp = new byte[src.Length];
            float intensity = _bloomIntensity;
            int radius = _bloomRadius;
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
                    if (_bloomFireOnly)
                    {
                        // Only bloom non-tree, non-vacant pixels (fire/glow)
                        byte srcR = src[idx + 2];
                        byte srcG = src[idx + 1];
                        byte srcB = src[idx + 0];
                        
                        // Simple check: is this close to tree or vacant color?
                        bool isTree = Math.Abs(srcR - _colorTree.R) < 20 && 
                                     Math.Abs(srcG - _colorTree.G) < 20 && 
                                     Math.Abs(srcB - _colorTree.B) < 20;
                        bool isVacant = Math.Abs(srcR - _colorVacant.R) < 20 && 
                                       Math.Abs(srcG - _colorVacant.G) < 20 && 
                                       Math.Abs(srcB - _colorVacant.B) < 20;
                        
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
                    
                    // Blend blurred with original
                    byte origB = src[idx + 0];
                    byte origG = src[idx + 1];
                    byte origR = src[idx + 2];
                    
                    // Additive bloom: add blur on top of original (clamped)
                    dst[idx + 0] = (byte)Math.Min(255, origB + sumB * intensity);
                    dst[idx + 1] = (byte)Math.Min(255, origG + sumG * intensity);
                    dst[idx + 2] = (byte)Math.Min(255, origR + sumR * intensity);
                    dst[idx + 3] = 255;
                }
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ColorToBgra(Color c) => (uint)(c.B | (c.G << 8) | (c.R << 16) | (255 << 24));

        // ============================================================
        // === UI UPDATES ===
        // ============================================================

        private void UpdateStatsLabels()
        {
            _nsLabel.Text = $"Timesteps: {_Ns:n0}";
            _treesLabel.Text = $"Trees: {_treeCount:n0}";
            double density = (double)_treeCount / TotalLogicalCells * 100.0;
            _densityLabel.Text = $"Density: {density:0.00}%";
            _firesLabel.Text = $"Fires: {_totalFires:n0}";
            
            string bloomInfo = _enableBloom ? $" Bloom:{_lastBloomMs}ms" : "";
            _fpsActualLabel.Text = $"FPS: {_actualFps:0.0} (Draw:{_lastDrawMs}ms{bloomInfo})";
            _gridInfoLabel.Text = $"Logical: {_logicalWidth}×{_logicalHeight} | Cell: {_cellSize}px";

            UpdateFormTitle();
        }

        private void UpdateMaxFAndLabels()
        {
            double fMax = CurrentFMax();
            
            _fBar.Value = Math.Min(_fBar.Maximum, Math.Max(0, (int)(Math.Pow(_f / fMax, 1.0 / 3.0) * 100000)));

            _pLabel.Text = $"Tree Growth (p): {_p:0.0000}";
            _fLabel.Text = $"Lightning (f): {_f:0.00e+0}";
            _ratioLabel.Text = $"Ratio (p/f): {_pfRatio:0.0}";
            
            double scaleFactor = (double)TotalLogicalCells / REFERENCE_GRID_SIZE;
            _speedLabel.Text = $"Steps/Frame: {_baseStepsPerFrame:n0} (×{scaleFactor:0.0} = {_stepsPerFrame:n0})";
        }

        private void UpdateFormTitle()
        {
            string status = _isSimulating ? "▶ Running" : "⏸ Paused";
            double density = (double)_treeCount / TotalLogicalCells * 100.0;
            string ndiStatus = _ndiEnabled ? " | 📡 NDI" : "";
            Text = $"Forest Fire OPTIMIZED — {status} | {_outputWidth}×{_outputHeight} | Trees: {_treeCount:n0} ({density:0.0}%) | FPS: {_actualFps:0}{ndiStatus}";
        }

        // === NDI Methods ===

        private void InitializeNDI()
        {
            try
            {
                _ndiSender?.Dispose();
                _ndiSender = new NdiSender(_ndiSourceName, _outputWidth, _outputHeight);
                _ndiEnabled = true;
                
                if (_ndiCheckBox != null)
                {
                    _ndiCheckBox.Checked = true;
                }
                
                UpdateFormTitle();
                System.Diagnostics.Debug.WriteLine($"✓ NDI initialized: {_ndiSourceName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "NDI Initialization Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                _ndiEnabled = false;
                
                if (_ndiCheckBox != null)
                {
                    _ndiCheckBox.Checked = false;
                }
            }
        }

        private void ShutdownNDI()
        {
            _ndiSender?.Dispose();
            _ndiSender = null;
            _ndiEnabled = false;
            
            if (_ndiCheckBox != null)
            {
                _ndiCheckBox.Checked = false;
            }
            
            UpdateFormTitle();
            System.Diagnostics.Debug.WriteLine("✓ NDI shut down");
        }

        private void ToggleNDI()
        {
            if (_ndiEnabled)
                ShutdownNDI();
            else
                InitializeNDI();
        }

        private void OnNdiCheckChanged(object? sender, EventArgs e)
        {
            if (_ndiCheckBox.Checked && !_ndiEnabled)
            {
                InitializeNDI();
            }
            else if (!_ndiCheckBox.Checked && _ndiEnabled)
            {
                ShutdownNDI();
            }
        }

        // Update or add Dispose method:
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _bmp?.Dispose();
                _timer?.Dispose();
                _ndiSender?.Dispose();  // Clean up NDI
                _threadRng?.Dispose();
            }
            base.Dispose(disposing);
        }

        // === XorShift128Plus RNG ===
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
