using System.Drawing;
using System.Windows.Forms;

namespace TreeGrowth
{
    partial class Form2
    {
        private System.ComponentModel.IContainer components = null;

        // --- Timer ---
        private System.Windows.Forms.Timer _timer;

        // --- Menu Strip ---
        private System.Windows.Forms.MenuStrip _menuStrip;
        private System.Windows.Forms.ToolStripMenuItem _fileMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _saveSettingsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _saveSettingsAsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _loadSettingsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _revertToDefaultsMenuItem;
        private System.Windows.Forms.ToolStripSeparator _menuSeparator;
        private System.Windows.Forms.ToolStripMenuItem _exitMenuItem;

        // --- Main Display ---
        private System.Windows.Forms.PictureBox _picture;
        private System.Windows.Forms.Panel _parametersPanel;
        private System.Windows.Forms.Panel _statsPanel;
        private System.Windows.Forms.Panel _visualPanel;

        // --- Simulation Parameters ---
        private System.Windows.Forms.TrackBar _pBar;
        private System.Windows.Forms.TrackBar _fBar;
        private System.Windows.Forms.TrackBar _speedBar;
        private System.Windows.Forms.TrackBar _ratioBar;
        private System.Windows.Forms.Label _pLabel;
        private System.Windows.Forms.Label _fLabel;
        private System.Windows.Forms.Label _speedLabel;
        private System.Windows.Forms.Label _ratioLabel;

        // --- Visual Parameters ---
        private System.Windows.Forms.TrackBar _burnDecayBar;
        private System.Windows.Forms.TrackBar _fireSpeedBar;
        private System.Windows.Forms.TrackBar _fpsBar;
        private System.Windows.Forms.TrackBar _flickerBar;
        private System.Windows.Forms.Label _burnDecayLabel;
        private System.Windows.Forms.Label _fireSpeedLabel;
        private System.Windows.Forms.Label _fpsLabel;
        private System.Windows.Forms.Label _flickerLabel;

        // --- Color Buttons ---
        private System.Windows.Forms.Button _treeColorBtn;
        private System.Windows.Forms.Button _vacantColorBtn;
        private System.Windows.Forms.Button _fireColorBtn;
        private System.Windows.Forms.Button _burnoutColorBtn;
        private System.Windows.Forms.Label _colorsLabel;

        // --- Dropdowns ---
        private System.Windows.Forms.ComboBox _gridSizeCombo;
        private System.Windows.Forms.ComboBox _neighborhoodCombo;
        private System.Windows.Forms.Label _gridSizeLabel;
        private System.Windows.Forms.Label _neighborhoodLabel;

        // === NEW: Cell Size Controls ===
        private System.Windows.Forms.ComboBox _cellSizeCombo;
        private System.Windows.Forms.Label _cellSizeLabel;

        // === NEW: Preset Controls ===
        private System.Windows.Forms.ComboBox _presetCombo;
        private System.Windows.Forms.Label _presetLabel;

        // === NEW: Bloom Controls ===
        private System.Windows.Forms.CheckBox _bloomCheck;
        private System.Windows.Forms.TrackBar _bloomRadiusBar;
        private System.Windows.Forms.Label _bloomRadiusLabel;
        private System.Windows.Forms.TrackBar _bloomIntensityBar;
        private System.Windows.Forms.Label _bloomIntensityLabel;
        private System.Windows.Forms.CheckBox _bloomFireOnlyCheck;
        private System.Windows.Forms.Panel _bloomPanel;
        private System.Windows.Forms.Label _bloomHeader;

        // === NEW: NDI Control ===
        private System.Windows.Forms.CheckBox _ndiCheckBox;

        // --- Stats Labels ---
        private System.Windows.Forms.Label _nsLabel;
        private System.Windows.Forms.Label _treesLabel;
        private System.Windows.Forms.Label _densityLabel;
        private System.Windows.Forms.Label _firesLabel;
        private System.Windows.Forms.Label _fpsActualLabel;
        private System.Windows.Forms.Label _gridInfoLabel;

        // --- Section Headers ---
        private System.Windows.Forms.Label _parametersHeader;
        private System.Windows.Forms.Label _visualHeader;
        private System.Windows.Forms.Label _statsHeader;

        // --- Control Panel ---
        private System.Windows.Forms.Button _fullscreenBtn;
        private System.Windows.Forms.Button _resetBtn;
        private System.Windows.Forms.Button _startBtn;
        private System.Windows.Forms.CheckBox _animateCheck;
        private System.Windows.Forms.TextBox _seedBox;
        private System.Windows.Forms.Label _seedLabel;
        private System.Windows.Forms.Panel _controlPanel;

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            _timer = new System.Windows.Forms.Timer(components);
            _menuStrip = new MenuStrip();
            _fileMenuItem = new ToolStripMenuItem();
            _saveSettingsMenuItem = new ToolStripMenuItem();
            _saveSettingsAsMenuItem = new ToolStripMenuItem();
            _loadSettingsMenuItem = new ToolStripMenuItem();
            _revertToDefaultsMenuItem = new ToolStripMenuItem();
            _menuSeparator = new ToolStripSeparator();
            _exitMenuItem = new ToolStripMenuItem();
            _picture = new PictureBox();
            _parametersPanel = new Panel();
            _parametersHeader = new Label();
            _pBar = new TrackBar();
            _pLabel = new Label();
            _fBar = new TrackBar();
            _fLabel = new Label();
            _ratioBar = new TrackBar();
            _ratioLabel = new Label();
            _speedBar = new TrackBar();
            _speedLabel = new Label();
            _statsPanel = new Panel();
            _statsHeader = new Label();
            _nsLabel = new Label();
            _treesLabel = new Label();
            _densityLabel = new Label();
            _firesLabel = new Label();
            _fpsActualLabel = new Label();
            _gridInfoLabel = new Label();
            _neighborhoodLabel = new Label();
            _neighborhoodCombo = new ComboBox();
            _visualPanel = new Panel();
            _visualHeader = new Label();
            _burnDecayBar = new TrackBar();
            _burnDecayLabel = new Label();
            _fireSpeedBar = new TrackBar();
            _fireSpeedLabel = new Label();
            _fpsBar = new TrackBar();
            _fpsLabel = new Label();
            _flickerBar = new TrackBar();
            _flickerLabel = new Label();
            _colorsLabel = new Label();
            _treeColorBtn = new Button();
            _vacantColorBtn = new Button();
            _fireColorBtn = new Button();
            _burnoutColorBtn = new Button();
            _presetLabel = new Label();
            _presetCombo = new ComboBox();
            _gridSizeLabel = new Label();
            _gridSizeCombo = new ComboBox();
            _cellSizeLabel = new Label();
            _cellSizeCombo = new ComboBox();
            _bloomPanel = new Panel();
            _bloomHeader = new Label();
            _bloomCheck = new CheckBox();
            _bloomRadiusBar = new TrackBar();
            _bloomRadiusLabel = new Label();
            _bloomIntensityBar = new TrackBar();
            _bloomIntensityLabel = new Label();
            _bloomFireOnlyCheck = new CheckBox();
            _fullscreenBtn = new Button();
            _resetBtn = new Button();
            _startBtn = new Button();
            _animateCheck = new CheckBox();
            _seedBox = new TextBox();
            _seedLabel = new Label();
            _controlPanel = new Panel();
            _ndiCheckBox = new CheckBox();
            _menuStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_picture).BeginInit();
            _parametersPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_pBar).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_fBar).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_ratioBar).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_speedBar).BeginInit();
            _statsPanel.SuspendLayout();
            _visualPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_burnDecayBar).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_fireSpeedBar).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_fpsBar).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_flickerBar).BeginInit();
            _bloomPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_bloomRadiusBar).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_bloomIntensityBar).BeginInit();
            _controlPanel.SuspendLayout();
            SuspendLayout();
            // 
            // _menuStrip
            // 
            _menuStrip.BackColor = Color.FromArgb(45, 45, 45);
            _menuStrip.ForeColor = Color.White;
            _menuStrip.ImageScalingSize = new Size(24, 24);
            _menuStrip.Items.AddRange(new ToolStripItem[] { _fileMenuItem });
            _menuStrip.Location = new Point(0, 0);
            _menuStrip.Name = "_menuStrip";
            _menuStrip.Padding = new Padding(8, 3, 0, 3);
            _menuStrip.Size = new Size(1920, 35);
            _menuStrip.TabIndex = 0;
            _menuStrip.Text = "menuStrip";
            // 
            // _fileMenuItem
            // 
            _fileMenuItem.DropDownItems.AddRange(new ToolStripItem[] { _saveSettingsMenuItem, _saveSettingsAsMenuItem, _loadSettingsMenuItem, _revertToDefaultsMenuItem, _menuSeparator, _exitMenuItem });
            _fileMenuItem.ForeColor = Color.White;
            _fileMenuItem.Name = "_fileMenuItem";
            _fileMenuItem.Size = new Size(54, 29);
            _fileMenuItem.Text = "&File";
            // 
            // _saveSettingsMenuItem
            // 
            _saveSettingsMenuItem.Name = "_saveSettingsMenuItem";
            _saveSettingsMenuItem.ShortcutKeys = Keys.Control | Keys.S;
            _saveSettingsMenuItem.Size = new Size(366, 34);
            _saveSettingsMenuItem.Text = "&Save Settings";
            _saveSettingsMenuItem.Click += OnSaveSettingsMenuClick;
            // 
            // _saveSettingsAsMenuItem
            // 
            _saveSettingsAsMenuItem.Name = "_saveSettingsAsMenuItem";
            _saveSettingsAsMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.S;
            _saveSettingsAsMenuItem.Size = new Size(366, 34);
            _saveSettingsAsMenuItem.Text = "Save Settings &As...";
            _saveSettingsAsMenuItem.Click += OnSaveSettingsAsMenuClick;
            // 
            // _loadSettingsMenuItem
            // 
            _loadSettingsMenuItem.Name = "_loadSettingsMenuItem";
            _loadSettingsMenuItem.ShortcutKeys = Keys.Control | Keys.O;
            _loadSettingsMenuItem.Size = new Size(366, 34);
            _loadSettingsMenuItem.Text = "&Load Settings...";
            _loadSettingsMenuItem.Click += OnLoadSettingsMenuClick;
            // 
            // _revertToDefaultsMenuItem
            // 
            _revertToDefaultsMenuItem.Name = "_revertToDefaultsMenuItem";
            _revertToDefaultsMenuItem.Size = new Size(366, 34);
            _revertToDefaultsMenuItem.Text = "&Revert to Defaults";
            _revertToDefaultsMenuItem.Click += OnRevertToDefaultsMenuClick;
            // 
            // _menuSeparator
            // 
            _menuSeparator.Name = "_menuSeparator";
            _menuSeparator.Size = new Size(363, 6);
            // 
            // _exitMenuItem
            // 
            _exitMenuItem.Name = "_exitMenuItem";
            _exitMenuItem.ShortcutKeys = Keys.Alt | Keys.F4;
            _exitMenuItem.Size = new Size(366, 34);
            _exitMenuItem.Text = "E&xit";
            _exitMenuItem.Click += OnExitMenuClick;
            // 
            // _picture
            // 
            _picture.BackColor = Color.FromArgb(169, 130, 104);
            _picture.Location = new Point(0, 35);
            _picture.Name = "_picture";
            _picture.Size = new Size(1920, 1045);
            _picture.SizeMode = PictureBoxSizeMode.Zoom;
            _picture.TabIndex = 1;
            _picture.TabStop = false;
            // 
            // _parametersPanel
            // 
            _parametersPanel.BackColor = Color.FromArgb(35, 35, 35);
            _parametersPanel.Controls.Add(_parametersHeader);
            _parametersPanel.Controls.Add(_pBar);
            _parametersPanel.Controls.Add(_pLabel);
            _parametersPanel.Controls.Add(_fBar);
            _parametersPanel.Controls.Add(_fLabel);
            _parametersPanel.Controls.Add(_ratioBar);
            _parametersPanel.Controls.Add(_ratioLabel);
            _parametersPanel.Controls.Add(_speedBar);
            _parametersPanel.Controls.Add(_speedLabel);
            _parametersPanel.Location = new Point(484, 1115);
            _parametersPanel.Name = "_parametersPanel";
            _parametersPanel.Padding = new Padding(15);
            _parametersPanel.Size = new Size(507, 213);
            _parametersPanel.TabIndex = 3;
            // 
            // _parametersHeader
            // 
            _parametersHeader.AutoSize = true;
            _parametersHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _parametersHeader.ForeColor = Color.FromArgb(255, 140, 0);
            _parametersHeader.Location = new Point(15, 5);
            _parametersHeader.Name = "_parametersHeader";
            _parametersHeader.Size = new Size(247, 25);
            _parametersHeader.TabIndex = 0;
            _parametersHeader.Text = "SIMULATION PARAMETERS";
            // 
            // _pBar
            // 
            _pBar.BackColor = Color.FromArgb(35, 35, 35);
            _pBar.Location = new Point(17, 58);
            _pBar.Maximum = 1000;
            _pBar.Name = "_pBar";
            _pBar.Size = new Size(150, 69);
            _pBar.TabIndex = 1;
            _pBar.TickFrequency = 50;
            _pBar.Value = 100;
            _pBar.Scroll += OnPBarScroll;
            // 
            // _pLabel
            // 
            _pLabel.AutoSize = true;
            _pLabel.Font = new Font("Segoe UI", 8F);
            _pLabel.ForeColor = Color.White;
            _pLabel.Location = new Point(14, 38);
            _pLabel.Name = "_pLabel";
            _pLabel.Size = new Size(173, 21);
            _pLabel.TabIndex = 2;
            _pLabel.Text = "Tree Growth (p): 0.0100";
            // 
            // _fBar
            // 
            _fBar.BackColor = Color.FromArgb(35, 35, 35);
            _fBar.Location = new Point(182, 58);
            _fBar.Maximum = 100000;
            _fBar.Name = "_fBar";
            _fBar.Size = new Size(150, 69);
            _fBar.TabIndex = 3;
            _fBar.TickFrequency = 10000;
            _fBar.Value = 20;
            _fBar.Scroll += OnFBarScroll;
            // 
            // _fLabel
            // 
            _fLabel.AutoSize = true;
            _fLabel.Font = new Font("Segoe UI", 8F);
            _fLabel.ForeColor = Color.White;
            _fLabel.Location = new Point(179, 38);
            _fLabel.Name = "_fLabel";
            _fLabel.Size = new Size(155, 21);
            _fLabel.TabIndex = 4;
            _fLabel.Text = "Lightning (f): 1.00e-5";
            // 
            // _ratioBar
            // 
            _ratioBar.BackColor = Color.FromArgb(35, 35, 35);
            _ratioBar.Location = new Point(333, 62);
            _ratioBar.Maximum = 100;
            _ratioBar.Minimum = 1;
            _ratioBar.Name = "_ratioBar";
            _ratioBar.Size = new Size(140, 69);
            _ratioBar.TabIndex = 5;
            _ratioBar.TickFrequency = 10;
            _ratioBar.Value = 10;
            _ratioBar.Scroll += OnRatioBarScroll;
            // 
            // _ratioLabel
            // 
            _ratioLabel.AutoSize = true;
            _ratioLabel.Font = new Font("Segoe UI", 8F);
            _ratioLabel.ForeColor = Color.White;
            _ratioLabel.Location = new Point(344, 38);
            _ratioLabel.Name = "_ratioLabel";
            _ratioLabel.Size = new Size(117, 21);
            _ratioLabel.TabIndex = 6;
            _ratioLabel.Text = "Ratio (p/f): 10.0";
            // 
            // _speedBar
            // 
            _speedBar.BackColor = Color.FromArgb(35, 35, 35);
            _speedBar.Location = new Point(18, 150);
            _speedBar.Maximum = 80000;
            _speedBar.Minimum = 1;
            _speedBar.Name = "_speedBar";
            _speedBar.Size = new Size(220, 69);
            _speedBar.TabIndex = 7;
            _speedBar.TickFrequency = 500;
            _speedBar.Value = 1000;
            _speedBar.Scroll += OnSpeedBarScroll;
            // 
            // _speedLabel
            // 
            _speedLabel.AutoSize = true;
            _speedLabel.Font = new Font("Segoe UI", 8F);
            _speedLabel.ForeColor = Color.White;
            _speedLabel.Location = new Point(18, 129);
            _speedLabel.Name = "_speedLabel";
            _speedLabel.Size = new Size(143, 21);
            _speedLabel.TabIndex = 8;
            _speedLabel.Text = "Steps/Frame: 1,000";
            // 
            // _statsPanel
            // 
            _statsPanel.BackColor = Color.FromArgb(35, 35, 35);
            _statsPanel.Controls.Add(_statsHeader);
            _statsPanel.Controls.Add(_nsLabel);
            _statsPanel.Controls.Add(_treesLabel);
            _statsPanel.Controls.Add(_densityLabel);
            _statsPanel.Controls.Add(_firesLabel);
            _statsPanel.Controls.Add(_fpsActualLabel);
            _statsPanel.Controls.Add(_gridInfoLabel);
            _statsPanel.Controls.Add(_neighborhoodLabel);
            _statsPanel.Controls.Add(_neighborhoodCombo);
            _statsPanel.Location = new Point(1650, 1115);
            _statsPanel.Name = "_statsPanel";
            _statsPanel.Padding = new Padding(15);
            _statsPanel.Size = new Size(270, 213);
            _statsPanel.TabIndex = 5;
            // 
            // _statsHeader
            // 
            _statsHeader.AutoSize = true;
            _statsHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _statsHeader.ForeColor = Color.FromArgb(255, 140, 0);
            _statsHeader.Location = new Point(15, 5);
            _statsHeader.Name = "_statsHeader";
            _statsHeader.Size = new Size(151, 25);
            _statsHeader.TabIndex = 0;
            _statsHeader.Text = "LIVE STATISTICS";
            // 
            // _nsLabel
            // 
            _nsLabel.AutoSize = true;
            _nsLabel.Font = new Font("Segoe UI", 9F);
            _nsLabel.ForeColor = Color.White;
            _nsLabel.Location = new Point(15, 30);
            _nsLabel.Name = "_nsLabel";
            _nsLabel.Size = new Size(147, 25);
            _nsLabel.TabIndex = 1;
            _nsLabel.Text = "Timesteps (Ns): 0";
            // 
            // _treesLabel
            // 
            _treesLabel.AutoSize = true;
            _treesLabel.Font = new Font("Segoe UI", 9F);
            _treesLabel.ForeColor = Color.White;
            _treesLabel.Location = new Point(15, 50);
            _treesLabel.Name = "_treesLabel";
            _treesLabel.Size = new Size(115, 25);
            _treesLabel.TabIndex = 2;
            _treesLabel.Text = "Tree Count: 0";
            // 
            // _densityLabel
            // 
            _densityLabel.AutoSize = true;
            _densityLabel.Font = new Font("Segoe UI", 9F);
            _densityLabel.ForeColor = Color.White;
            _densityLabel.Location = new Point(15, 70);
            _densityLabel.Name = "_densityLabel";
            _densityLabel.Size = new Size(155, 25);
            _densityLabel.TabIndex = 3;
            _densityLabel.Text = "Density (ρ): 0.00%";
            // 
            // _firesLabel
            // 
            _firesLabel.AutoSize = true;
            _firesLabel.Font = new Font("Segoe UI", 9F);
            _firesLabel.ForeColor = Color.White;
            _firesLabel.Location = new Point(140, 30);
            _firesLabel.Name = "_firesLabel";
            _firesLabel.Size = new Size(109, 25);
            _firesLabel.TabIndex = 4;
            _firesLabel.Text = "Total Fires: 0";
            // 
            // _fpsActualLabel
            // 
            _fpsActualLabel.AutoSize = true;
            _fpsActualLabel.Font = new Font("Segoe UI", 9F);
            _fpsActualLabel.ForeColor = Color.LimeGreen;
            _fpsActualLabel.Location = new Point(140, 50);
            _fpsActualLabel.Name = "_fpsActualLabel";
            _fpsActualLabel.Size = new Size(74, 25);
            _fpsActualLabel.TabIndex = 5;
            _fpsActualLabel.Text = "FPS: 0.0";
            // 
            // _gridInfoLabel
            // 
            _gridInfoLabel.AutoSize = true;
            _gridInfoLabel.Font = new Font("Segoe UI", 9F);
            _gridInfoLabel.ForeColor = Color.FromArgb(180, 180, 180);
            _gridInfoLabel.Location = new Point(15, 95);
            _gridInfoLabel.Name = "_gridInfoLabel";
            _gridInfoLabel.Size = new Size(146, 25);
            _gridInfoLabel.TabIndex = 6;
            _gridInfoLabel.Text = "Grid: 1920×1080";
            // 
            // _neighborhoodLabel
            // 
            _neighborhoodLabel.AutoSize = true;
            _neighborhoodLabel.Font = new Font("Segoe UI", 8F);
            _neighborhoodLabel.ForeColor = Color.FromArgb(180, 180, 180);
            _neighborhoodLabel.Location = new Point(15, 143);
            _neighborhoodLabel.Name = "_neighborhoodLabel";
            _neighborhoodLabel.Size = new Size(62, 21);
            _neighborhoodLabel.TabIndex = 7;
            _neighborhoodLabel.Text = "Spread:";
            // 
            // _neighborhoodCombo
            // 
            _neighborhoodCombo.BackColor = Color.FromArgb(50, 50, 50);
            _neighborhoodCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _neighborhoodCombo.ForeColor = Color.White;
            _neighborhoodCombo.Items.AddRange(new object[] { "8 Neighbors", "4 Neighbors" });
            _neighborhoodCombo.Location = new Point(94, 138);
            _neighborhoodCombo.Name = "_neighborhoodCombo";
            _neighborhoodCombo.Size = new Size(120, 33);
            _neighborhoodCombo.TabIndex = 8;
            _neighborhoodCombo.SelectedIndexChanged += OnNeighborhoodChanged;
            // 
            // _visualPanel
            // 
            _visualPanel.BackColor = Color.FromArgb(35, 35, 35);
            _visualPanel.Controls.Add(_visualHeader);
            _visualPanel.Controls.Add(_burnDecayBar);
            _visualPanel.Controls.Add(_burnDecayLabel);
            _visualPanel.Controls.Add(_fireSpeedBar);
            _visualPanel.Controls.Add(_fireSpeedLabel);
            _visualPanel.Controls.Add(_fpsBar);
            _visualPanel.Controls.Add(_fpsLabel);
            _visualPanel.Controls.Add(_flickerBar);
            _visualPanel.Controls.Add(_flickerLabel);
            _visualPanel.Controls.Add(_colorsLabel);
            _visualPanel.Controls.Add(_treeColorBtn);
            _visualPanel.Controls.Add(_vacantColorBtn);
            _visualPanel.Controls.Add(_fireColorBtn);
            _visualPanel.Controls.Add(_burnoutColorBtn);
            _visualPanel.Controls.Add(_presetLabel);
            _visualPanel.Controls.Add(_presetCombo);
            _visualPanel.Controls.Add(_gridSizeLabel);
            _visualPanel.Controls.Add(_gridSizeCombo);
            _visualPanel.Controls.Add(_cellSizeLabel);
            _visualPanel.Controls.Add(_cellSizeCombo);
            _visualPanel.Location = new Point(997, 1115);
            _visualPanel.Name = "_visualPanel";
            _visualPanel.Padding = new Padding(15);
            _visualPanel.Size = new Size(653, 213);
            _visualPanel.TabIndex = 4;
            // 
            // _visualHeader
            // 
            _visualHeader.AutoSize = true;
            _visualHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _visualHeader.ForeColor = Color.FromArgb(255, 140, 0);
            _visualHeader.Location = new Point(15, 5);
            _visualHeader.Name = "_visualHeader";
            _visualHeader.Size = new Size(198, 25);
            _visualHeader.TabIndex = 0;
            _visualHeader.Text = "VISUAL PARAMETERS";
            // 
            // _burnDecayBar
            // 
            _burnDecayBar.BackColor = Color.FromArgb(35, 35, 35);
            _burnDecayBar.Location = new Point(11, 33);
            _burnDecayBar.Maximum = 60;
            _burnDecayBar.Minimum = 1;
            _burnDecayBar.Name = "_burnDecayBar";
            _burnDecayBar.Size = new Size(120, 69);
            _burnDecayBar.TabIndex = 1;
            _burnDecayBar.TickFrequency = 10;
            _burnDecayBar.Value = 15;
            _burnDecayBar.Scroll += OnBurnDecayScroll;
            // 
            // _burnDecayLabel
            // 
            _burnDecayLabel.AutoSize = true;
            _burnDecayLabel.Font = new Font("Segoe UI", 8F);
            _burnDecayLabel.ForeColor = Color.White;
            _burnDecayLabel.Location = new Point(17, 98);
            _burnDecayLabel.Name = "_burnDecayLabel";
            _burnDecayLabel.Size = new Size(114, 21);
            _burnDecayLabel.TabIndex = 2;
            _burnDecayLabel.Text = "Burn Decay: 15";
            // 
            // _fireSpeedBar
            // 
            _fireSpeedBar.BackColor = Color.FromArgb(35, 35, 35);
            _fireSpeedBar.Location = new Point(143, 33);
            _fireSpeedBar.Maximum = 20;
            _fireSpeedBar.Minimum = 1;
            _fireSpeedBar.Name = "_fireSpeedBar";
            _fireSpeedBar.Size = new Size(120, 69);
            _fireSpeedBar.TabIndex = 3;
            _fireSpeedBar.TickFrequency = 5;
            _fireSpeedBar.Value = 1;
            _fireSpeedBar.Scroll += OnFireSpeedScroll;
            // 
            // _fireSpeedLabel
            // 
            _fireSpeedLabel.AutoSize = true;
            _fireSpeedLabel.Font = new Font("Segoe UI", 8F);
            _fireSpeedLabel.ForeColor = Color.White;
            _fireSpeedLabel.Location = new Point(147, 98);
            _fireSpeedLabel.Name = "_fireSpeedLabel";
            _fireSpeedLabel.Size = new Size(99, 21);
            _fireSpeedLabel.TabIndex = 4;
            _fireSpeedLabel.Text = "Fire Speed: 1";
            // 
            // _fpsBar
            // 
            _fpsBar.BackColor = Color.FromArgb(35, 35, 35);
            _fpsBar.Location = new Point(277, 33);
            _fpsBar.Maximum = 120;
            _fpsBar.Minimum = 10;
            _fpsBar.Name = "_fpsBar";
            _fpsBar.Size = new Size(120, 69);
            _fpsBar.TabIndex = 5;
            _fpsBar.TickFrequency = 20;
            _fpsBar.Value = 60;
            _fpsBar.Scroll += OnFpsScroll;
            // 
            // _fpsLabel
            // 
            _fpsLabel.AutoSize = true;
            _fpsLabel.Font = new Font("Segoe UI", 8F);
            _fpsLabel.ForeColor = Color.White;
            _fpsLabel.Location = new Point(277, 98);
            _fpsLabel.Name = "_fpsLabel";
            _fpsLabel.Size = new Size(107, 21);
            _fpsLabel.TabIndex = 6;
            _fpsLabel.Text = "Target FPS: 60";
            // 
            // _flickerBar
            // 
            _flickerBar.BackColor = Color.FromArgb(35, 35, 35);
            _flickerBar.Location = new Point(407, 33);
            _flickerBar.Maximum = 200;
            _flickerBar.Name = "_flickerBar";
            _flickerBar.Size = new Size(120, 69);
            _flickerBar.TabIndex = 7;
            _flickerBar.TickFrequency = 50;
            _flickerBar.Value = 105;
            _flickerBar.Scroll += OnFlickerScroll;
            // 
            // _flickerLabel
            // 
            _flickerLabel.AutoSize = true;
            _flickerLabel.Font = new Font("Segoe UI", 8F);
            _flickerLabel.ForeColor = Color.White;
            _flickerLabel.Location = new Point(407, 106);
            _flickerLabel.Name = "_flickerLabel";
            _flickerLabel.Size = new Size(89, 21);
            _flickerLabel.TabIndex = 8;
            _flickerLabel.Text = "Flicker: 105";
            // 
            // _colorsLabel
            // 
            _colorsLabel.AutoSize = true;
            _colorsLabel.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            _colorsLabel.ForeColor = Color.FromArgb(180, 180, 180);
            _colorsLabel.Location = new Point(17, 123);
            _colorsLabel.Name = "_colorsLabel";
            _colorsLabel.Size = new Size(75, 21);
            _colorsLabel.TabIndex = 9;
            _colorsLabel.Text = "COLORS:";
            // 
            // _treeColorBtn
            // 
            _treeColorBtn.BackColor = Color.FromArgb(198, 164, 145);
            _treeColorBtn.FlatStyle = FlatStyle.Flat;
            _treeColorBtn.ForeColor = Color.Black;
            _treeColorBtn.Location = new Point(14, 150);
            _treeColorBtn.Name = "_treeColorBtn";
            _treeColorBtn.Size = new Size(70, 51);
            _treeColorBtn.TabIndex = 10;
            _treeColorBtn.Text = "Tree";
            _treeColorBtn.UseVisualStyleBackColor = false;
            _treeColorBtn.Click += OnTreeColorClick;
            // 
            // _vacantColorBtn
            // 
            _vacantColorBtn.BackColor = Color.FromArgb(169, 130, 104);
            _vacantColorBtn.FlatStyle = FlatStyle.Flat;
            _vacantColorBtn.ForeColor = Color.Black;
            _vacantColorBtn.Location = new Point(90, 150);
            _vacantColorBtn.Name = "_vacantColorBtn";
            _vacantColorBtn.Size = new Size(70, 51);
            _vacantColorBtn.TabIndex = 11;
            _vacantColorBtn.Text = "Empty";
            _vacantColorBtn.UseVisualStyleBackColor = false;
            _vacantColorBtn.Click += OnVacantColorClick;
            // 
            // _fireColorBtn
            // 
            _fireColorBtn.BackColor = Color.FromArgb(255, 200, 0);
            _fireColorBtn.FlatStyle = FlatStyle.Flat;
            _fireColorBtn.ForeColor = Color.Black;
            _fireColorBtn.Location = new Point(165, 150);
            _fireColorBtn.Name = "_fireColorBtn";
            _fireColorBtn.Size = new Size(70, 51);
            _fireColorBtn.TabIndex = 12;
            _fireColorBtn.Text = "Fire";
            _fireColorBtn.UseVisualStyleBackColor = false;
            _fireColorBtn.Click += OnFireColorClick;
            // 
            // _burnoutColorBtn
            // 
            _burnoutColorBtn.BackColor = Color.FromArgb(255, 191, 0);
            _burnoutColorBtn.FlatStyle = FlatStyle.Flat;
            _burnoutColorBtn.ForeColor = Color.Black;
            _burnoutColorBtn.Location = new Point(240, 150);
            _burnoutColorBtn.Name = "_burnoutColorBtn";
            _burnoutColorBtn.Size = new Size(70, 51);
            _burnoutColorBtn.TabIndex = 13;
            _burnoutColorBtn.Text = "Glow";
            _burnoutColorBtn.UseVisualStyleBackColor = false;
            _burnoutColorBtn.Click += OnBurnoutColorClick;
            // 
            // _presetLabel
            // 
            _presetLabel.AutoSize = true;
            _presetLabel.Font = new Font("Segoe UI", 8F);
            _presetLabel.ForeColor = Color.White;
            _presetLabel.Location = new Point(316, 144);
            _presetLabel.Name = "_presetLabel";
            _presetLabel.Size = new Size(56, 21);
            _presetLabel.TabIndex = 14;
            _presetLabel.Text = "Preset:";
            // 
            // _presetCombo
            // 
            _presetCombo.BackColor = Color.FromArgb(50, 50, 50);
            _presetCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _presetCombo.ForeColor = Color.White;
            _presetCombo.Items.AddRange(new object[] { "Warm", "Atmosphere", "Forest", "Night", "ANWB", "Infrared", "Ocean", "Monochrome" });
            _presetCombo.Location = new Point(316, 168);
            _presetCombo.Name = "_presetCombo";
            _presetCombo.Size = new Size(100, 33);
            _presetCombo.TabIndex = 15;
            _presetCombo.SelectedIndexChanged += OnPresetChanged;
            // 
            // _gridSizeLabel
            // 
            _gridSizeLabel.AutoSize = true;
            _gridSizeLabel.Font = new Font("Segoe UI", 8F);
            _gridSizeLabel.ForeColor = Color.White;
            _gridSizeLabel.Location = new Point(426, 144);
            _gridSizeLabel.Name = "_gridSizeLabel";
            _gridSizeLabel.Size = new Size(43, 21);
            _gridSizeLabel.TabIndex = 16;
            _gridSizeLabel.Text = "Grid:";
            // 
            // _gridSizeCombo
            // 
            _gridSizeCombo.BackColor = Color.FromArgb(50, 50, 50);
            _gridSizeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _gridSizeCombo.ForeColor = Color.White;
            _gridSizeCombo.Items.AddRange(new object[] { "1920×1080 HD", "1024×1024", "2560×1440 QHD", "3840×2160 4K", "512×512 Fast" });
            _gridSizeCombo.Location = new Point(426, 168);
            _gridSizeCombo.Name = "_gridSizeCombo";
            _gridSizeCombo.Size = new Size(110, 33);
            _gridSizeCombo.TabIndex = 17;
            _gridSizeCombo.SelectedIndexChanged += OnGridSizeChanged;
            // 
            // _cellSizeLabel
            // 
            _cellSizeLabel.AutoSize = true;
            _cellSizeLabel.Font = new Font("Segoe UI", 8F);
            _cellSizeLabel.ForeColor = Color.White;
            _cellSizeLabel.Location = new Point(544, 144);
            _cellSizeLabel.Name = "_cellSizeLabel";
            _cellSizeLabel.Size = new Size(71, 21);
            _cellSizeLabel.TabIndex = 18;
            _cellSizeLabel.Text = "Cell Size:";
            // 
            // _cellSizeCombo
            // 
            _cellSizeCombo.BackColor = Color.FromArgb(50, 50, 50);
            _cellSizeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _cellSizeCombo.ForeColor = Color.White;
            _cellSizeCombo.Items.AddRange(new object[] { "1×1 Fine", "2×2", "4×4", "8×8 Chunky", "16×16 LED" });
            _cellSizeCombo.Location = new Point(544, 168);
            _cellSizeCombo.Name = "_cellSizeCombo";
            _cellSizeCombo.Size = new Size(105, 33);
            _cellSizeCombo.TabIndex = 19;
            _cellSizeCombo.SelectedIndexChanged += OnCellSizeChanged;
            // 
            // _bloomPanel
            // 
            _bloomPanel.BackColor = Color.FromArgb(35, 35, 35);
            _bloomPanel.Controls.Add(_bloomHeader);
            _bloomPanel.Controls.Add(_bloomCheck);
            _bloomPanel.Controls.Add(_bloomRadiusBar);
            _bloomPanel.Controls.Add(_bloomRadiusLabel);
            _bloomPanel.Controls.Add(_bloomIntensityBar);
            _bloomPanel.Controls.Add(_bloomIntensityLabel);
            _bloomPanel.Controls.Add(_bloomFireOnlyCheck);
            _bloomPanel.Location = new Point(0, 1328);
            _bloomPanel.Name = "_bloomPanel";
            _bloomPanel.Padding = new Padding(15);
            _bloomPanel.Size = new Size(991, 120);
            _bloomPanel.TabIndex = 6;
            // 
            // _bloomHeader
            // 
            _bloomHeader.AutoSize = true;
            _bloomHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _bloomHeader.ForeColor = Color.FromArgb(255, 100, 150);
            _bloomHeader.Location = new Point(15, 5);
            _bloomHeader.Name = "_bloomHeader";
            _bloomHeader.Size = new Size(143, 25);
            _bloomHeader.TabIndex = 0;
            _bloomHeader.Text = "BLOOM EFFECT";
            // 
            // _bloomCheck
            // 
            _bloomCheck.AutoSize = true;
            _bloomCheck.ForeColor = Color.White;
            _bloomCheck.Location = new Point(15, 35);
            _bloomCheck.Name = "_bloomCheck";
            _bloomCheck.Size = new Size(172, 29);
            _bloomCheck.TabIndex = 1;
            _bloomCheck.Text = "Enable Bloom (B)";
            _bloomCheck.CheckedChanged += OnBloomCheckChanged;
            // 
            // _bloomRadiusBar
            // 
            _bloomRadiusBar.BackColor = Color.FromArgb(35, 35, 35);
            _bloomRadiusBar.Location = new Point(193, 50);
            _bloomRadiusBar.Maximum = 8;
            _bloomRadiusBar.Minimum = 1;
            _bloomRadiusBar.Name = "_bloomRadiusBar";
            _bloomRadiusBar.Size = new Size(100, 69);
            _bloomRadiusBar.TabIndex = 2;
            _bloomRadiusBar.Value = 2;
            _bloomRadiusBar.Scroll += OnBloomRadiusScroll;
            // 
            // _bloomRadiusLabel
            // 
            _bloomRadiusLabel.AutoSize = true;
            _bloomRadiusLabel.Font = new Font("Segoe UI", 8F);
            _bloomRadiusLabel.ForeColor = Color.White;
            _bloomRadiusLabel.Location = new Point(193, 30);
            _bloomRadiusLabel.Name = "_bloomRadiusLabel";
            _bloomRadiusLabel.Size = new Size(122, 21);
            _bloomRadiusLabel.TabIndex = 3;
            _bloomRadiusLabel.Text = "Bloom Radius: 2";
            // 
            // _bloomIntensityBar
            // 
            _bloomIntensityBar.BackColor = Color.FromArgb(35, 35, 35);
            _bloomIntensityBar.Location = new Point(313, 50);
            _bloomIntensityBar.Maximum = 100;
            _bloomIntensityBar.Minimum = 10;
            _bloomIntensityBar.Name = "_bloomIntensityBar";
            _bloomIntensityBar.Size = new Size(100, 69);
            _bloomIntensityBar.TabIndex = 4;
            _bloomIntensityBar.TickFrequency = 20;
            _bloomIntensityBar.Value = 50;
            _bloomIntensityBar.Scroll += OnBloomIntensityScroll;
            // 
            // _bloomIntensityLabel
            // 
            _bloomIntensityLabel.AutoSize = true;
            _bloomIntensityLabel.Font = new Font("Segoe UI", 8F);
            _bloomIntensityLabel.ForeColor = Color.White;
            _bloomIntensityLabel.Location = new Point(313, 30);
            _bloomIntensityLabel.Name = "_bloomIntensityLabel";
            _bloomIntensityLabel.Size = new Size(156, 21);
            _bloomIntensityLabel.TabIndex = 5;
            _bloomIntensityLabel.Text = "Bloom Intensity: 50%";
            // 
            // _bloomFireOnlyCheck
            // 
            _bloomFireOnlyCheck.AutoSize = true;
            _bloomFireOnlyCheck.Checked = true;
            _bloomFireOnlyCheck.CheckState = CheckState.Checked;
            _bloomFireOnlyCheck.ForeColor = Color.FromArgb(180, 180, 180);
            _bloomFireOnlyCheck.Location = new Point(802, 50);
            _bloomFireOnlyCheck.Name = "_bloomFireOnlyCheck";
            _bloomFireOnlyCheck.Size = new Size(155, 29);
            _bloomFireOnlyCheck.TabIndex = 6;
            _bloomFireOnlyCheck.Text = "Fire/Glow Only";
            _bloomFireOnlyCheck.CheckedChanged += OnBloomFireOnlyChanged;
            // 
            // _fullscreenBtn
            // 
            _fullscreenBtn.BackColor = Color.FromArgb(60, 60, 120);
            _fullscreenBtn.FlatStyle = FlatStyle.Flat;
            _fullscreenBtn.ForeColor = Color.White;
            _fullscreenBtn.Location = new Point(278, 138);
            _fullscreenBtn.Name = "_fullscreenBtn";
            _fullscreenBtn.Size = new Size(140, 35);
            _fullscreenBtn.TabIndex = 5;
            _fullscreenBtn.Text = "Fullscreen (F11)";
            _fullscreenBtn.UseVisualStyleBackColor = false;
            _fullscreenBtn.Click += OnFullscreenBtnClick;
            // 
            // _resetBtn
            // 
            _resetBtn.BackColor = Color.FromArgb(80, 80, 80);
            _resetBtn.FlatStyle = FlatStyle.Flat;
            _resetBtn.ForeColor = Color.White;
            _resetBtn.Location = new Point(168, 138);
            _resetBtn.Name = "_resetBtn";
            _resetBtn.Size = new Size(100, 35);
            _resetBtn.TabIndex = 4;
            _resetBtn.Text = "Reset";
            _resetBtn.UseVisualStyleBackColor = false;
            _resetBtn.Click += OnResetBtnClick;
            // 
            // _startBtn
            // 
            _startBtn.BackColor = Color.FromArgb(0, 120, 0);
            _startBtn.FlatStyle = FlatStyle.Flat;
            _startBtn.ForeColor = Color.White;
            _startBtn.Location = new Point(38, 138);
            _startBtn.Name = "_startBtn";
            _startBtn.Size = new Size(120, 35);
            _startBtn.TabIndex = 3;
            _startBtn.Text = "Start (Space)";
            _startBtn.UseVisualStyleBackColor = false;
            _startBtn.Click += OnStartBtnClick;
            // 
            // _animateCheck
            // 
            _animateCheck.AutoSize = true;
            _animateCheck.Checked = true;
            _animateCheck.CheckState = CheckState.Checked;
            _animateCheck.ForeColor = Color.White;
            _animateCheck.Location = new Point(180, 47);
            _animateCheck.Name = "_animateCheck";
            _animateCheck.Size = new Size(145, 29);
            _animateCheck.TabIndex = 2;
            _animateCheck.Text = "Animate Fires";
            _animateCheck.CheckedChanged += OnAnimateCheckChanged;
            // 
            // _seedBox
            // 
            _seedBox.BackColor = Color.FromArgb(50, 50, 50);
            _seedBox.BorderStyle = BorderStyle.FixedSingle;
            _seedBox.ForeColor = Color.White;
            _seedBox.Location = new Point(15, 45);
            _seedBox.Name = "_seedBox";
            _seedBox.Size = new Size(150, 31);
            _seedBox.TabIndex = 1;
            _seedBox.Text = "default";
            // 
            // _seedLabel
            // 
            _seedLabel.AutoSize = true;
            _seedLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _seedLabel.ForeColor = Color.FromArgb(180, 180, 180);
            _seedLabel.Location = new Point(15, 15);
            _seedLabel.Name = "_seedLabel";
            _seedLabel.Size = new Size(55, 25);
            _seedLabel.TabIndex = 0;
            _seedLabel.Text = "SEED";
            // 
            // _controlPanel
            // 
            _controlPanel.BackColor = Color.FromArgb(35, 35, 35);
            _controlPanel.Controls.Add(_seedLabel);
            _controlPanel.Controls.Add(_seedBox);
            _controlPanel.Controls.Add(_animateCheck);
            _controlPanel.Controls.Add(_startBtn);
            _controlPanel.Controls.Add(_resetBtn);
            _controlPanel.Controls.Add(_fullscreenBtn);
            _controlPanel.Controls.Add(_ndiCheckBox);
            _controlPanel.Location = new Point(0, 1115);
            _controlPanel.Name = "_controlPanel";
            _controlPanel.Padding = new Padding(15);
            _controlPanel.Size = new Size(492, 213);
            _controlPanel.TabIndex = 2;
            // 
            // _ndiCheckBox
            // 
            _ndiCheckBox.AutoSize = true;
            _ndiCheckBox.ForeColor = Color.White;
            _ndiCheckBox.Location = new Point(180, 80);
            _ndiCheckBox.Name = "_ndiCheckBox";
            _ndiCheckBox.Size = new Size(226, 29);
            _ndiCheckBox.TabIndex = 8;
            _ndiCheckBox.Text = "📡 NDI Stream (Ctrl+N)";
            _ndiCheckBox.CheckedChanged += OnNdiCheckChanged;
            // 
            // Form2
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(22, 22, 22);
            ClientSize = new Size(1920, 1448);
            Controls.Add(_menuStrip);
            Controls.Add(_picture);
            Controls.Add(_controlPanel);
            Controls.Add(_parametersPanel);
            Controls.Add(_visualPanel);
            Controls.Add(_statsPanel);
            Controls.Add(_bloomPanel);
            ForeColor = Color.White;
            MainMenuStrip = _menuStrip;
            Name = "Form2";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Forest Fire Model — LED Wall Presentation Mode";
            Load += Form2_Load;
            _menuStrip.ResumeLayout(false);
            _menuStrip.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_picture).EndInit();
            _parametersPanel.ResumeLayout(false);
            _parametersPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_pBar).EndInit();
            ((System.ComponentModel.ISupportInitialize)_fBar).EndInit();
            ((System.ComponentModel.ISupportInitialize)_ratioBar).EndInit();
            ((System.ComponentModel.ISupportInitialize)_speedBar).EndInit();
            _statsPanel.ResumeLayout(false);
            _statsPanel.PerformLayout();
            _visualPanel.ResumeLayout(false);
            _visualPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_burnDecayBar).EndInit();
            ((System.ComponentModel.ISupportInitialize)_fireSpeedBar).EndInit();
            ((System.ComponentModel.ISupportInitialize)_fpsBar).EndInit();
            ((System.ComponentModel.ISupportInitialize)_flickerBar).EndInit();
            _bloomPanel.ResumeLayout(false);
            _bloomPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_bloomRadiusBar).EndInit();
            ((System.ComponentModel.ISupportInitialize)_bloomIntensityBar).EndInit();
            _controlPanel.ResumeLayout(false);
            _controlPanel.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
