namespace TreeGrowth
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        // --- UI Controls ---
        private System.Windows.Forms.Timer _timer;
        private System.Windows.Forms.PictureBox _picture;
        private System.Windows.Forms.TrackBar _pBar;
        private System.Windows.Forms.TrackBar _fBar;
        private System.Windows.Forms.TrackBar _speedBar;
        private System.Windows.Forms.TextBox _seedBox;
        private System.Windows.Forms.CheckBox _animateCheck;
        private System.Windows.Forms.Button _startBtn;
        private System.Windows.Forms.Button _resetBtn;
        private System.Windows.Forms.Label _pLabel;
        private System.Windows.Forms.Label _fLabel;
        private System.Windows.Forms.Label _speedLabel;
        private System.Windows.Forms.Label _nsLabel;
        private System.Windows.Forms.Label _treesLabel;
        private System.Windows.Forms.Label _densityLabel;
        private System.Windows.Forms.Label _firesLabel;
        private System.Windows.Forms.Label _parametersLabel;
        private System.Windows.Forms.Label _seedLabel;
        private System.Windows.Forms.Label _statsLabel;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            if (disposing)
            {
                _bmp?.Dispose();
                _timer?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            _timer = new System.Windows.Forms.Timer(components);
            _picture = new PictureBox();
            _pBar = new TrackBar();
            _fBar = new TrackBar();
            _speedBar = new TrackBar();
            _seedBox = new TextBox();
            _animateCheck = new CheckBox();
            _startBtn = new Button();
            _resetBtn = new Button();
            _pLabel = new Label();
            _fLabel = new Label();
            _speedLabel = new Label();
            _nsLabel = new Label();
            _treesLabel = new Label();
            _densityLabel = new Label();
            _firesLabel = new Label();
            _parametersLabel = new Label();
            _seedLabel = new Label();
            _statsLabel = new Label();
            ((System.ComponentModel.ISupportInitialize)_picture).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_pBar).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_fBar).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_speedBar).BeginInit();
            SuspendLayout();
            // 
            // _picture
            // 
            _picture.BorderStyle = BorderStyle.FixedSingle;
            _picture.Location = new Point(13, 14);
            _picture.Margin = new Padding(4, 5, 4, 5);
            _picture.Name = "_picture";
            _picture.Size = new Size(1920, 1080);
            _picture.SizeMode = PictureBoxSizeMode.Zoom;
            _picture.TabIndex = 0;
            _picture.TabStop = false;
            // 
            // _pBar
            // 
            _pBar.Location = new Point(1375, 1234);
            _pBar.Margin = new Padding(4, 5, 4, 5);
            _pBar.Maximum = 2000;
            _pBar.Name = "_pBar";
            _pBar.Size = new Size(543, 69);
            _pBar.TabIndex = 2;
            _pBar.TickFrequency = 50;
            _pBar.Value = 100;
            _pBar.Scroll += OnPBarScroll;
            // 
            // _fBar
            // 
            _fBar.Location = new Point(1375, 1334);
            _fBar.Margin = new Padding(4, 5, 4, 5);
            _fBar.Maximum = 100000;
            _fBar.Name = "_fBar";
            _fBar.Size = new Size(543, 69);
            _fBar.TabIndex = 4;
            _fBar.TickFrequency = 10000;
            _fBar.Value = 20;
            _fBar.Scroll += OnFBarScroll;
            // 
            // _speedBar
            // 
            _speedBar.Location = new Point(1375, 1434);
            _speedBar.Margin = new Padding(4, 5, 4, 5);
            _speedBar.Maximum = 10000;
            _speedBar.Minimum = 1;
            _speedBar.Name = "_speedBar";
            _speedBar.Size = new Size(543, 69);
            _speedBar.TabIndex = 6;
            _speedBar.TickFrequency = 500;
            _speedBar.Value = 1000;
            _speedBar.Scroll += OnSpeedBarScroll;
            // 
            // _seedBox
            // 
            _seedBox.Location = new Point(50, 1317);
            _seedBox.Margin = new Padding(4, 5, 4, 5);
            _seedBox.Name = "_seedBox";
            _seedBox.Size = new Size(284, 31);
            _seedBox.TabIndex = 9;
            _seedBox.Text = "default";
            // 
            // _animateCheck
            // 
            _animateCheck.AutoSize = true;
            _animateCheck.Checked = true;
            _animateCheck.CheckState = CheckState.Checked;
            _animateCheck.Location = new Point(365, 1320);
            _animateCheck.Margin = new Padding(4, 5, 4, 5);
            _animateCheck.Name = "_animateCheck";
            _animateCheck.Size = new Size(145, 29);
            _animateCheck.TabIndex = 10;
            _animateCheck.Text = "Animate Fires";
            _animateCheck.UseVisualStyleBackColor = true;
            _animateCheck.CheckedChanged += OnAnimateCheckChanged;
            // 
            // _startBtn
            // 
            _startBtn.ForeColor = Color.Black;
            _startBtn.Location = new Point(50, 1384);
            _startBtn.Margin = new Padding(4, 5, 4, 5);
            _startBtn.Name = "_startBtn";
            _startBtn.Size = new Size(171, 38);
            _startBtn.TabIndex = 11;
            _startBtn.Text = "Start";
            _startBtn.UseVisualStyleBackColor = true;
            _startBtn.Click += OnStartBtnClick;
            // 
            // _resetBtn
            // 
            _resetBtn.ForeColor = Color.Black;
            _resetBtn.Location = new Point(250, 1384);
            _resetBtn.Margin = new Padding(4, 5, 4, 5);
            _resetBtn.Name = "_resetBtn";
            _resetBtn.Size = new Size(171, 38);
            _resetBtn.TabIndex = 12;
            _resetBtn.Text = "Reset";
            _resetBtn.UseVisualStyleBackColor = true;
            _resetBtn.Click += OnResetBtnClick;
            // 
            // _pLabel
            // 
            _pLabel.AutoSize = true;
            _pLabel.Location = new Point(1375, 1278);
            _pLabel.Margin = new Padding(4, 0, 4, 0);
            _pLabel.Name = "_pLabel";
            _pLabel.Size = new Size(71, 25);
            _pLabel.TabIndex = 3;
            _pLabel.Text = "_pLabel";
            // 
            // _fLabel
            // 
            _fLabel.AutoSize = true;
            _fLabel.Location = new Point(1375, 1378);
            _fLabel.Margin = new Padding(4, 0, 4, 0);
            _fLabel.Name = "_fLabel";
            _fLabel.Size = new Size(62, 25);
            _fLabel.TabIndex = 5;
            _fLabel.Text = "_flabel";
            // 
            // _speedLabel
            // 
            _speedLabel.AutoSize = true;
            _speedLabel.Location = new Point(1375, 1478);
            _speedLabel.Margin = new Padding(4, 0, 4, 0);
            _speedLabel.Name = "_speedLabel";
            _speedLabel.Size = new Size(108, 25);
            _speedLabel.TabIndex = 7;
            _speedLabel.Text = "_speedLabel";
            // 
            // _nsLabel
            // 
            _nsLabel.AutoSize = true;
            _nsLabel.Location = new Point(783, 1283);
            _nsLabel.Margin = new Padding(4, 0, 4, 0);
            _nsLabel.Name = "_nsLabel";
            _nsLabel.Size = new Size(78, 25);
            _nsLabel.TabIndex = 14;
            _nsLabel.Text = "_nsLabel";
            // 
            // _treesLabel
            // 
            _treesLabel.AutoSize = true;
            _treesLabel.Location = new Point(783, 1320);
            _treesLabel.Margin = new Padding(4, 0, 4, 0);
            _treesLabel.Name = "_treesLabel";
            _treesLabel.Size = new Size(98, 25);
            _treesLabel.TabIndex = 15;
            _treesLabel.Text = "_treesLabel";
            // 
            // _densityLabel
            // 
            _densityLabel.AutoSize = true;
            _densityLabel.Location = new Point(783, 1357);
            _densityLabel.Margin = new Padding(4, 0, 4, 0);
            _densityLabel.Name = "_densityLabel";
            _densityLabel.Size = new Size(117, 25);
            _densityLabel.TabIndex = 16;
            _densityLabel.Text = "_densityLabel";
            // 
            // _firesLabel
            // 
            _firesLabel.AutoSize = true;
            _firesLabel.Location = new Point(783, 1393);
            _firesLabel.Margin = new Padding(4, 0, 4, 0);
            _firesLabel.Name = "_firesLabel";
            _firesLabel.Size = new Size(93, 25);
            _firesLabel.TabIndex = 17;
            _firesLabel.Text = "_firesLabel";
            // 
            // _parametersLabel
            // 
            _parametersLabel.AutoSize = true;
            _parametersLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _parametersLabel.ForeColor = Color.White;
            _parametersLabel.Location = new Point(1375, 1191);
            _parametersLabel.Margin = new Padding(4, 0, 4, 0);
            _parametersLabel.Name = "_parametersLabel";
            _parametersLabel.Size = new Size(119, 28);
            _parametersLabel.TabIndex = 1;
            _parametersLabel.Text = "Parameters";
            // 
            // _seedLabel
            // 
            _seedLabel.AutoSize = true;
            _seedLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _seedLabel.ForeColor = Color.White;
            _seedLabel.Location = new Point(50, 1280);
            _seedLabel.Margin = new Padding(4, 0, 4, 0);
            _seedLabel.Name = "_seedLabel";
            _seedLabel.Size = new Size(57, 28);
            _seedLabel.TabIndex = 8;
            _seedLabel.Text = "Seed";
            // 
            // _statsLabel
            // 
            _statsLabel.AutoSize = true;
            _statsLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _statsLabel.ForeColor = Color.White;
            _statsLabel.Location = new Point(50, 1467);
            _statsLabel.Margin = new Padding(4, 0, 4, 0);
            _statsLabel.Name = "_statsLabel";
            _statsLabel.Size = new Size(142, 28);
            _statsLabel.TabIndex = 13;
            _statsLabel.Text = "Live Statistics";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(22, 22, 22);
            ClientSize = new Size(1954, 1579);
            Controls.Add(_firesLabel);
            Controls.Add(_densityLabel);
            Controls.Add(_treesLabel);
            Controls.Add(_nsLabel);
            Controls.Add(_statsLabel);
            Controls.Add(_resetBtn);
            Controls.Add(_startBtn);
            Controls.Add(_animateCheck);
            Controls.Add(_seedBox);
            Controls.Add(_seedLabel);
            Controls.Add(_speedLabel);
            Controls.Add(_speedBar);
            Controls.Add(_fLabel);
            Controls.Add(_fBar);
            Controls.Add(_pLabel);
            Controls.Add(_pBar);
            Controls.Add(_parametersLabel);
            Controls.Add(_picture);
            ForeColor = Color.White;
            Margin = new Padding(4, 5, 4, 5);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Forest Fire Model (Drossel–Schwabl) — WinForms";
            ((System.ComponentModel.ISupportInitialize)_picture).EndInit();
            ((System.ComponentModel.ISupportInitialize)_pBar).EndInit();
            ((System.ComponentModel.ISupportInitialize)_fBar).EndInit();
            ((System.ComponentModel.ISupportInitialize)_speedBar).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}