namespace TreeGrowth
{
    public partial class Form2 : Form
    {
        // ============================================================
        // === CONFIGURATION ===
        // ============================================================

        private int _outputWidth = 1920;
        private int _outputHeight = 1080;
        private int _cellSize = 1;

        // ============================================================
        // === CORE COMPONENTS ===
        // ============================================================

        private ForestFireSimulation _simulation = null!;
        private ForestFireRenderer _renderer = null!;

        // ============================================================
        // === UI STATE ===
        // ============================================================

        private bool _isSimulating;
        private bool _showStats = true;
        private bool _isFullscreen = false;
        private FormBorderStyle _previousBorderStyle;
        private FormWindowState _previousWindowState;

        // ============================================================
        // === PARAMETERS ===
        // ============================================================

        private double _p = 0.01;
        private double _f = 1e-5;
        private double _pfRatio = 10.0;

        // ============================================================
        // === PERFORMANCE MONITORING ===
        // ============================================================

        private int _targetFps = 60;
        private DateTime _lastFrameTime = DateTime.Now;
        private double _actualFps = 0;
        private int _frameCounter = 0;
        private const int UI_UPDATE_INTERVAL = 10;

        // Guard flag to prevent event handlers during initialization
        private bool _isInitializing = true;

        // ============================================================
        // === NDI STREAMING ===
        // ============================================================

        private NdiSender? _ndiSender;
        private bool _ndiEnabled = false;
        private readonly string _ndiSourceName = "Forest Fire Simulation";

        // ============================================================
        // === CONSTRUCTOR ===
        // ============================================================

        public Form2()
        {
            InitializeComponent();

            // Load saved settings or use defaults
            var settings = SimulationSettings.LoadDefaults();
            ApplySettings(settings);

            Text = "Forest Fire Model — OPTIMIZED (Cell Scaling + Bloom)";
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;
            KeyDown += OnKeyDown;

            _timer.Tick += (_, __) => StepFrame();

            // Add mouse click handler for starting fires
            _picture.MouseDown += OnPictureMouseDown;

            InitializeSimulation();
            UpdateTimerInterval();

            _isInitializing = false;
        }

        // ============================================================
        // === SETTINGS MANAGEMENT ===
        // ============================================================

        /// <summary>
        /// Captures current application state into a settings object
        /// </summary>
        private SimulationSettings CaptureSettings()
        {
            return new SimulationSettings
            {
                // Grid Configuration
                OutputWidth = _outputWidth,
                OutputHeight = _outputHeight,
                CellSize = _cellSize,

                // Simulation Parameters
                P = _p,
                F = _f,
                PFRatio = _pfRatio,
                BaseStepsPerFrame = _simulation.BaseStepsPerFrame,
                AnimateFires = _simulation.AnimateFires,
                UseMooreNeighborhood = _simulation.UseMooreNeighborhood,

                // Perlin Noise Distribution
                UsePerlinDistribution = _simulation.UsePerlinDistribution,
                NoiseScale = _simulation.NoiseScale,
                NoiseOctaves = _simulation.NoiseOctaves,
                NoiseThreshold = _simulation.NoiseThreshold,
                NoiseStrength = _simulation.NoiseStrength,

                // Visual Parameters
                BurnDecayFrames = _simulation.BurnDecayFrames,
                FireAnimationSpeed = _simulation.FireAnimationSpeed,
                FireFlickerRange = _renderer.FireFlickerRange,
                TargetFps = _targetFps,

                // Colors
                ColorTree = _renderer.ColorTree,
                ColorVacant = _renderer.ColorVacant,
                ColorFireBase = _renderer.ColorFireBase,
                ColorBurnout = _renderer.ColorBurnout,

                // Bloom Settings
                EnableBloom = _renderer.EnableBloom,
                BloomRadius = _renderer.BloomRadius,
                BloomIntensity = _renderer.BloomIntensity,
                BloomFireOnly = _renderer.BloomFireOnly,

                // UI State
                Seed = _seedBox.Text,
                NeighborhoodIndex = _neighborhoodCombo.SelectedIndex,
                GridSizeIndex = _gridSizeCombo.SelectedIndex,
                CellSizeIndex = _cellSizeCombo.SelectedIndex,
                PresetIndex = _presetCombo.SelectedIndex
            };
        }

        /// <summary>
        /// Applies settings to the application state
        /// </summary>
        private void ApplySettings(SimulationSettings settings)
        {
            _isInitializing = true;

            // Apply grid configuration
            _outputWidth = settings.OutputWidth;
            _outputHeight = settings.OutputHeight;
            _cellSize = settings.CellSize;

            // Initialize simulation
            _simulation = new ForestFireSimulation(_outputWidth, _outputHeight, _cellSize, Environment.ProcessorCount);
            _simulation.P = settings.P;
            _simulation.F = settings.F;
            _simulation.UseMooreNeighborhood = settings.UseMooreNeighborhood;
            _simulation.BurnDecayFrames = settings.BurnDecayFrames;
            _simulation.FireAnimationSpeed = settings.FireAnimationSpeed;
            _simulation.AnimateFires = settings.AnimateFires;
            _simulation.BaseStepsPerFrame = settings.BaseStepsPerFrame;

            // Apply Perlin noise settings
            _simulation.UsePerlinDistribution = settings.UsePerlinDistribution;
            _simulation.NoiseScale = settings.NoiseScale;
            _simulation.NoiseOctaves = settings.NoiseOctaves;
            _simulation.NoiseThreshold = settings.NoiseThreshold;
            _simulation.NoiseStrength = settings.NoiseStrength;

            // Initialize renderer
            _renderer = new ForestFireRenderer(_outputWidth, _outputHeight, _cellSize, Environment.ProcessorCount);
            _renderer.ColorTree = settings.ColorTree;
            _renderer.ColorVacant = settings.ColorVacant;
            _renderer.ColorFireBase = settings.ColorFireBase;
            _renderer.ColorBurnout = settings.ColorBurnout;
            _renderer.FireFlickerRange = settings.FireFlickerRange;
            _renderer.EnableBloom = settings.EnableBloom;
            _renderer.BloomRadius = settings.BloomRadius;
            _renderer.BloomIntensity = settings.BloomIntensity;
            _renderer.BloomFireOnly = settings.BloomFireOnly;

            // Apply parameters
            _p = settings.P;
            _f = settings.F;
            _pfRatio = settings.PFRatio;
            _targetFps = settings.TargetFps;

            // Update UI controls
            _seedBox.Text = settings.Seed;
            _neighborhoodCombo.SelectedIndex = settings.NeighborhoodIndex;
            _gridSizeCombo.SelectedIndex = settings.GridSizeIndex;
            _cellSizeCombo.SelectedIndex = settings.CellSizeIndex;
            _presetCombo.SelectedIndex = settings.PresetIndex;

            // Update trackbars (will trigger label updates)
            _pBar.Value = (int)(settings.P * 10000);
            _speedBar.Value = settings.BaseStepsPerFrame;
            _animateCheck.Checked = settings.AnimateFires;
            _burnDecayBar.Value = settings.BurnDecayFrames;
            _fireSpeedBar.Value = settings.FireAnimationSpeed;
            _fpsBar.Value = settings.TargetFps;
            _flickerBar.Value = settings.FireFlickerRange;

            // Update bloom controls
            _bloomCheck.Checked = settings.EnableBloom;
            _bloomRadiusBar.Value = settings.BloomRadius;
            _bloomIntensityBar.Value = (int)(settings.BloomIntensity * 100);
            _bloomFireOnlyCheck.Checked = settings.BloomFireOnly;

            // Update Perlin noise controls
            _perlinCheck.Checked = settings.UsePerlinDistribution;
            _noiseScaleBar.Value = (int)settings.NoiseScale;
            _noiseOctavesBar.Value = settings.NoiseOctaves;
            _noiseThresholdBar.Value = (int)(settings.NoiseThreshold * 100);
            _noiseStrengthBar.Value = (int)(settings.NoiseStrength * 100);

            // Update color buttons
            _treeColorBtn.BackColor = settings.ColorTree;
            _vacantColorBtn.BackColor = settings.ColorVacant;
            _fireColorBtn.BackColor = settings.ColorFireBase;
            _burnoutColorBtn.BackColor = settings.ColorBurnout;
            _picture.BackColor = settings.ColorVacant;

            _isInitializing = false;
        }

        /// <summary>
        /// Saves current settings to default location
        /// </summary>
        private void SaveSettings()
        {
            try
            {
                var settings = CaptureSettings();
                settings.SaveDefaults();
                MessageBox.Show(
                    $"Settings saved successfully to:\n{SimulationSettings.DefaultSettingsPath}",
                    "Settings Saved",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to save settings:\n{ex.Message}",
                    "Save Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        /// <summary>
        /// Saves current settings to a user-selected file
        /// </summary>
        private void SaveSettingsAs()
        {
            using var dlg = new SaveFileDialog
            {
                Filter = "JSON Settings|*.json|All Files|*.*",
                DefaultExt = "json",
                FileName = "forest_fire_settings.json",
                Title = "Save Settings As"
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var settings = CaptureSettings();
                    settings.SaveToFile(dlg.FileName);
                    MessageBox.Show(
                        $"Settings saved successfully to:\n{dlg.FileName}",
                        "Settings Saved",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Failed to save settings:\n{ex.Message}",
                        "Save Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
        }

        /// <summary>
        /// Loads settings from a user-selected file
        /// </summary>
        private void LoadSettings()
        {
            using var dlg = new OpenFileDialog
            {
                Filter = "JSON Settings|*.json|All Files|*.*",
                DefaultExt = "json",
                Title = "Load Settings"
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    bool wasRunning = _isSimulating;
                    if (wasRunning)
                    {
                        _isSimulating = false;
                        _timer.Stop();
                    }

                    var settings = SimulationSettings.LoadFromFile(dlg.FileName);
                    ApplySettings(settings);
                    InitializeSimulation();

                    MessageBox.Show(
                        $"Settings loaded successfully from:\n{dlg.FileName}",
                        "Settings Loaded",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );

                    _startBtn.Text = "Start (Space)";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Failed to load settings:\n{ex.Message}",
                        "Load Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
        }

        /// <summary>
        /// Reverts all settings to factory defaults
        /// </summary>
        private void RevertToDefaults()
        {
            var result = MessageBox.Show(
                "This will reset all settings to factory defaults.\n\nAre you sure?",
                "Revert to Defaults",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                try
                {
                    bool wasRunning = _isSimulating;
                    if (wasRunning)
                    {
                        _isSimulating = false;
                        _timer.Stop();
                    }

                    // Create new default settings
                    var defaults = new SimulationSettings();
                    ApplySettings(defaults);
                    InitializeSimulation();

                    MessageBox.Show(
                        "Settings have been reset to factory defaults.",
                        "Defaults Restored",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );

                    _startBtn.Text = "Start (Space)";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Failed to revert to defaults:\n{ex.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
        }

        // ============================================================
        // === MENU EVENT HANDLERS ===
        // ============================================================

        private void OnSaveSettingsMenuClick(object? sender, EventArgs e)
        {
            SaveSettings();
        }

        private void OnSaveSettingsAsMenuClick(object? sender, EventArgs e)
        {
            SaveSettingsAs();
        }

        private void OnLoadSettingsMenuClick(object? sender, EventArgs e)
        {
            LoadSettings();
        }

        private void OnRevertToDefaultsMenuClick(object? sender, EventArgs e)
        {
            RevertToDefaults();
        }

        private void OnExitMenuClick(object? sender, EventArgs e)
        {
            Close();
        }

        // ============================================================
        // === INITIALIZATION ===
        // ============================================================

        private void InitializeSimulation()
        {
            _p = _pBar.Value / 10000.0;
            _simulation.BaseStepsPerFrame = _speedBar.Value;
            _simulation.AnimateFires = _animateCheck.Checked;
            _simulation.P = _p;
            _simulation.F = _f;

            UpdateMaxFAndLabels();

            _simulation.Initialize(_seedBox.Text.Trim());
            _frameCounter = 0;

            DrawFrame();
            UpdateStatsLabels();
        }

        private void RecreateComponents()
        {
            // Recreate renderer with new dimensions
            _renderer?.Dispose();
            _renderer = new ForestFireRenderer(_outputWidth, _outputHeight, _cellSize, Environment.ProcessorCount);

            // Restore renderer settings
            _renderer.ColorTree = _treeColorBtn.BackColor;
            _renderer.ColorVacant = _vacantColorBtn.BackColor;
            _renderer.ColorFireBase = _fireColorBtn.BackColor;
            _renderer.ColorBurnout = _burnoutColorBtn.BackColor;
            _renderer.FireFlickerRange = _flickerBar.Value;
            _renderer.EnableBloom = _bloomCheck.Checked;
            _renderer.BloomRadius = _bloomRadiusBar.Value;
            _renderer.BloomIntensity = _bloomIntensityBar.Value / 100f;
            _renderer.BloomFireOnly = _bloomFireOnlyCheck.Checked;

            // Recreate simulation
            _simulation.SetGridSize(_outputWidth, _outputHeight, _cellSize);
            InitializeSimulation();
        }

        private void UpdateTimerInterval()
        {
            _timer.Interval = Math.Max(1, 1000 / _targetFps);
        }

        // ============================================================
        // === SIMULATION & RENDERING ===
        // ============================================================

        private void StepFrame()
        {
            _simulation.Step();
            DrawFrame();

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

        private void DrawFrame()
        {
            var bitmap = _renderer.Render(_simulation);
            _picture.Image = bitmap;

            // Send NDI frame if enabled
            if (_ndiEnabled && _ndiSender != null)
            {
                try
                {
                    byte[] buffer = _renderer.GetPixelBuffer();
                    _ndiSender.SendFrame(buffer, _outputWidth, _outputHeight);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"NDI send error: {ex.Message}");
                }
            }
        }

        // ============================================================
        // === UI UPDATES ===
        // ============================================================

        private void UpdateStatsLabels()
        {
            _nsLabel.Text = $"Timesteps: {_simulation.Timesteps:n0}";
            _treesLabel.Text = $"Trees: {_simulation.TreeCount:n0}";
            double density = (double)_simulation.TreeCount / _simulation.TotalLogicalCells * 100.0;
            _densityLabel.Text = $"Density: {density:0.00}%";
            _firesLabel.Text = $"Fires: {_simulation.TotalFires:n0}";

            string bloomInfo = _renderer.EnableBloom ? $" Bloom:{_renderer.LastBloomMs}ms" : "";
            _fpsActualLabel.Text = $"FPS: {_actualFps:0.0} (Draw:{_renderer.LastDrawMs}ms{bloomInfo})";
            _gridInfoLabel.Text = $"Logical: {_simulation.LogicalWidth}×{_simulation.LogicalHeight} | Cell: {_cellSize}px";

            UpdateFormTitle();
        }

        private void UpdateMaxFAndLabels()
        {
            double fMax = CurrentFMax();
            _fBar.Value = Math.Min(_fBar.Maximum, Math.Max(0, (int)(Math.Pow(_f / fMax, 1.0 / 3.0) * 100000)));

            _pLabel.Text = $"Tree Growth (p): {_p:0.0000}";
            _fLabel.Text = $"Lightning (f): {_f:0.00e+0}";
            _ratioLabel.Text = $"Ratio (p/f): {_pfRatio:0.0}";

            double scaleFactor = (double)_simulation.TotalLogicalCells / (512 * 512);
            _speedLabel.Text = $"Steps/Frame: {_simulation.BaseStepsPerFrame:n0} (×{scaleFactor:0.0} = {_simulation.StepsPerFrame:n0})";
        }

        private void UpdateFormTitle()
        {
            string status = _isSimulating ? "▶ Running" : "⏸ Paused";
            double density = (double)_simulation.TreeCount / _simulation.TotalLogicalCells * 100.0;
            string ndiStatus = _ndiEnabled ? " | 📡 NDI" : "";
            Text = $"Forest Fire OPTIMIZED — {status} | {_outputWidth}×{_outputHeight} | Trees: {_simulation.TreeCount:n0} ({density:0.0}%) | FPS: {_actualFps:0}{ndiStatus}";
        }

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
        // === MOUSE INTERACTION ===
        // ============================================================

        private void OnPictureMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            // Convert screen coordinates to logical grid coordinates
            int logicalX = (e.X * _outputWidth / _picture.ClientSize.Width) / _cellSize;
            int logicalY = (e.Y * _outputHeight / _picture.ClientSize.Height) / _cellSize;

            // Try to start a fire at the clicked location
            if (_simulation.TryStartFireAt(logicalX, logicalY))
            {
                // Fire started successfully, redraw immediately
                DrawFrame();
                UpdateStatsLabels();
            }
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
                ToggleNDI();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.S && e.Control && e.Shift)
            {
                // Ctrl+Shift+S: Save settings as (check this first!)
                SaveSettingsAs();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.S && e.Control)
            {
                // Ctrl+S: Save settings
                SaveSettings();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.O && e.Control)
            {
                // Ctrl+O: Load settings
                LoadSettings();
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
                _renderer.EnableBloom = !_renderer.EnableBloom;
                _bloomCheck.Checked = _renderer.EnableBloom;
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
                _bloomPanel.Visible = false;
                _perlinPanel.Visible = false;
            }
            else
            {
                FormBorderStyle = _previousBorderStyle;
                WindowState = _previousWindowState;

                _statsPanel.Visible = _showStats;
                _parametersPanel.Visible = _showStats;
                _visualPanel.Visible = _showStats;
                _controlPanel.Visible = true;
                _bloomPanel.Visible = _showStats;
                _perlinPanel.Visible = _showStats;
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
            _simulation.P = _p;
            UpdateMaxFAndLabels();
        }

        private void OnFBarScroll(object sender, EventArgs e)
        {
            _f = TrackBarToF(_fBar.Value, CurrentFMax());
            _simulation.F = _f;
            UpdateMaxFAndLabels();
        }

        private void OnRatioBarScroll(object sender, EventArgs e)
        {
            _pfRatio = _ratioBar.Value / 10.0;
            _f = _p / _pfRatio;
            _simulation.F = _f;
            UpdateMaxFAndLabels();
        }

        private void OnSpeedBarScroll(object sender, EventArgs e)
        {
            _simulation.BaseStepsPerFrame = _speedBar.Value;
            UpdateMaxFAndLabels();
        }

        private void OnAnimateCheckChanged(object sender, EventArgs e)
        {
            _simulation.AnimateFires = _animateCheck.Checked;
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
            _simulation.BurnDecayFrames = _burnDecayBar.Value;
            _burnDecayLabel.Text = $"Burn Decay: {_burnDecayBar.Value}";
        }

        private void OnFireSpeedScroll(object sender, EventArgs e)
        {
            _simulation.FireAnimationSpeed = _fireSpeedBar.Value;
            _fireSpeedLabel.Text = $"Fire Speed: {_fireSpeedBar.Value}";
        }

        private void OnFpsScroll(object sender, EventArgs e)
        {
            _targetFps = _fpsBar.Value;
            _fpsLabel.Text = $"Target FPS: {_targetFps}";
            UpdateTimerInterval();
        }

        private void OnFlickerScroll(object sender, EventArgs e)
        {
            _renderer.FireFlickerRange = _flickerBar.Value;
            _flickerLabel.Text = $"Flicker: {_flickerBar.Value}";
        }

        private void OnNeighborhoodChanged(object sender, EventArgs e)
        {
            _simulation.UseMooreNeighborhood = _neighborhoodCombo.SelectedIndex == 0;
        }

        private void OnCellSizeChanged(object sender, EventArgs e)
        {
            if (_isInitializing) return;

            bool wasRunning = _isSimulating;
            if (wasRunning)
            {
                _isSimulating = false;
                _timer.Stop();
            }

            _cellSize = _cellSizeCombo.SelectedIndex switch
            {
                0 => 1,
                1 => 2,
                2 => 4,
                3 => 8,
                4 => 16,
                _ => 1
            };

            RecreateComponents();
            _startBtn.Text = "Start (Space)";
        }

        private void OnGridSizeChanged(object sender, EventArgs e)
        {
            if (_isInitializing) return;

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

            RecreateComponents();
            _startBtn.Text = "Start (Space)";
        }

        private void OnTreeColorClick(object sender, EventArgs e)
        {
            using var dlg = new ColorDialog { Color = _renderer.ColorTree };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _renderer.ColorTree = dlg.Color;
                _treeColorBtn.BackColor = dlg.Color;
            }
        }

        private void OnVacantColorClick(object sender, EventArgs e)
        {
            using var dlg = new ColorDialog { Color = _renderer.ColorVacant };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _renderer.ColorVacant = dlg.Color;
                _vacantColorBtn.BackColor = dlg.Color;
                _picture.BackColor = dlg.Color;
            }
        }

        private void OnFireColorClick(object sender, EventArgs e)
        {
            using var dlg = new ColorDialog { Color = _renderer.ColorFireBase };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _renderer.ColorFireBase = dlg.Color;
                _fireColorBtn.BackColor = dlg.Color;
            }
        }

        private void OnBurnoutColorClick(object sender, EventArgs e)
        {
            using var dlg = new ColorDialog { Color = _renderer.ColorBurnout };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _renderer.ColorBurnout = dlg.Color;
                _burnoutColorBtn.BackColor = dlg.Color;
            }
        }

        private void OnPresetChanged(object sender, EventArgs e)
        {
            if (_presetCombo.SelectedIndex < 0) return;

            var preset = (ColorPresetManager.Preset)_presetCombo.SelectedIndex;
            var colors = ColorPresetManager.GetPreset(preset);

            _renderer.ColorTree = colors.Tree;
            _renderer.ColorVacant = colors.Vacant;
            _renderer.ColorFireBase = colors.Fire;
            _renderer.ColorBurnout = colors.Burnout;

            _treeColorBtn.BackColor = colors.Tree;
            _vacantColorBtn.BackColor = colors.Vacant;
            _fireColorBtn.BackColor = colors.Fire;
            _burnoutColorBtn.BackColor = colors.Burnout;
            _picture.BackColor = colors.Vacant;
        }

        private void OnBloomCheckChanged(object sender, EventArgs e)
        {
            _renderer.EnableBloom = _bloomCheck.Checked;
        }

        private void OnBloomRadiusScroll(object sender, EventArgs e)
        {
            _renderer.BloomRadius = _bloomRadiusBar.Value;
            _renderer.UpdateBloomKernel();
            _bloomRadiusLabel.Text = $"Bloom Radius: {_bloomRadiusBar.Value}";
        }

        private void OnBloomIntensityScroll(object sender, EventArgs e)
        {
            _renderer.BloomIntensity = _bloomIntensityBar.Value / 100f;
            _bloomIntensityLabel.Text = $"Bloom Intensity: {_renderer.BloomIntensity:P0}";
        }

        private void OnBloomFireOnlyChanged(object sender, EventArgs e)
        {
            _renderer.BloomFireOnly = _bloomFireOnlyCheck.Checked;
        }

        // ============================================================
        // === PERLIN NOISE EVENT HANDLERS ===
        // ============================================================

        private void OnPerlinCheckChanged(object sender, EventArgs e)
        {
            if (_isInitializing) return;
            
            _simulation.UsePerlinDistribution = _perlinCheck.Checked;
            
            // Enable/disable noise parameter controls
            _noiseScaleBar.Enabled = _perlinCheck.Checked;
            _noiseOctavesBar.Enabled = _perlinCheck.Checked;
            _noiseThresholdBar.Enabled = _perlinCheck.Checked;
            _noiseStrengthBar.Enabled = _perlinCheck.Checked;
            
            // Update label colors
            var color = _perlinCheck.Checked ? Color.White : Color.FromArgb(180, 180, 180);
            _noiseScaleLabel.ForeColor = color;
            _noiseOctavesLabel.ForeColor = color;
            _noiseThresholdLabel.ForeColor = color;
            _noiseStrengthLabel.ForeColor = color;
        }

        private void OnNoiseScaleScroll(object sender, EventArgs e)
        {
            _simulation.NoiseScale = _noiseScaleBar.Value;
            double multiplier = _simulation.NoiseScale / 50.0;
            _noiseScaleLabel.Text = $"Scale: {_noiseScaleBar.Value} ({multiplier:0.0}×)";
        }

        private void OnNoiseOctavesScroll(object sender, EventArgs e)
        {
            _simulation.NoiseOctaves = _noiseOctavesBar.Value;
            _noiseOctavesLabel.Text = $"Octaves: {_noiseOctavesBar.Value}";
        }

        private void OnNoiseThresholdScroll(object sender, EventArgs e)
        {
            _simulation.NoiseThreshold = _noiseThresholdBar.Value / 100.0;
            _noiseThresholdLabel.Text = $"Threshold: {_simulation.NoiseThreshold:0.00}";
        }

        private void OnNoiseStrengthScroll(object sender, EventArgs e)
        {
            _simulation.NoiseStrength = _noiseStrengthBar.Value / 100.0;
            _noiseStrengthLabel.Text = $"Strength: {_simulation.NoiseStrength:P0}";
        }

        // ============================================================
        // === NDI METHODS ===
        // ============================================================

        private void InitializeNDI()
        {
            try
            {
                _ndiSender?.Dispose();
                _ndiSender = new NdiSender(_ndiSourceName, _outputWidth, _outputHeight);
                _ndiEnabled = true;

                if (_ndiCheckBox != null)
                    _ndiCheckBox.Checked = true;

                UpdateFormTitle();
                System.Diagnostics.Debug.WriteLine($"✓ NDI initialized: {_ndiSourceName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "NDI Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _ndiEnabled = false;

                if (_ndiCheckBox != null)
                    _ndiCheckBox.Checked = false;
            }
        }

        private void ShutdownNDI()
        {
            _ndiSender?.Dispose();
            _ndiSender = null;
            _ndiEnabled = false;

            if (_ndiCheckBox != null)
                _ndiCheckBox.Checked = false;

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
                InitializeNDI();
            else if (!_ndiCheckBox.Checked && _ndiEnabled)
                ShutdownNDI();
        }

        // ============================================================
        // === DISPOSE ===
        // ============================================================

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Auto-save settings on exit
                try
                {
                    var settings = CaptureSettings();
                    settings.SaveDefaults();
                }
                catch
                {
                    // Silently fail on save during disposal
                }

                _timer?.Dispose();
                _renderer?.Dispose();
                _ndiSender?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void Form2_Load(object sender, EventArgs e)
        {

        }
    }
}
