namespace BizHawk.Client.EmuHawk
{
	partial class RewindConfig
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

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
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.OK = new System.Windows.Forms.Button();
			this.Cancel = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.RewindEnabledBox = new System.Windows.Forms.CheckBox();
			this.UseCompression = new System.Windows.Forms.CheckBox();
			this.label1 = new System.Windows.Forms.Label();
			this.StateSizeLabel = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.BufferSizeUpDown = new System.Windows.Forms.NumericUpDown();
			this.label3 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.FullnessLabel = new System.Windows.Forms.Label();
			this.groupBox4 = new System.Windows.Forms.GroupBox();
			this.EstTimeLabel = new System.Windows.Forms.Label();
			this.label11 = new System.Windows.Forms.Label();
			this.ApproxFramesLabel = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.RewindFramesUsedLabel = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.groupBox6 = new System.Windows.Forms.GroupBox();
			this.rbStatesText = new System.Windows.Forms.RadioButton();
			this.rbStatesBinary = new System.Windows.Forms.RadioButton();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.btnResetCompression = new System.Windows.Forms.Button();
			this.trackBarCompression = new System.Windows.Forms.TrackBar();
			this.nudCompression = new System.Windows.Forms.NumericUpDown();
			this.groupBox7 = new System.Windows.Forms.GroupBox();
			this.label20 = new System.Windows.Forms.Label();
			this.KbLabel = new System.Windows.Forms.Label();
			this.BigScreenshotNumeric = new System.Windows.Forms.NumericUpDown();
			this.LowResLargeScreenshotsCheckbox = new System.Windows.Forms.CheckBox();
			this.label13 = new System.Windows.Forms.Label();
			this.label14 = new System.Windows.Forms.Label();
			this.ScreenshotInStatesCheckbox = new System.Windows.Forms.CheckBox();
			this.label15 = new System.Windows.Forms.Label();
			this.label16 = new System.Windows.Forms.Label();
			this.BackupSavestatesCheckbox = new System.Windows.Forms.CheckBox();
			this.label12 = new System.Windows.Forms.Label();
			this.groupBox1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.BufferSizeUpDown)).BeginInit();
			this.groupBox4.SuspendLayout();
			this.groupBox6.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBarCompression)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.nudCompression)).BeginInit();
			this.groupBox7.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.BigScreenshotNumeric)).BeginInit();
			this.SuspendLayout();
			// 
			// OK
			// 
			this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OK.Location = new System.Drawing.Point(575, 470);
			this.OK.Name = "OK";
			this.OK.Size = new System.Drawing.Size(75, 23);
			this.OK.TabIndex = 0;
			this.OK.Text = "&OK";
			this.OK.UseVisualStyleBackColor = true;
			this.OK.Click += new System.EventHandler(this.Ok_Click);
			// 
			// Cancel
			// 
			this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.Cancel.Location = new System.Drawing.Point(656, 470);
			this.Cancel.Name = "Cancel";
			this.Cancel.Size = new System.Drawing.Size(75, 23);
			this.Cancel.TabIndex = 1;
			this.Cancel.Text = "&Cancel";
			this.Cancel.UseVisualStyleBackColor = true;
			this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.RewindEnabledBox);
			this.groupBox1.Controls.Add(this.UseCompression);
			this.groupBox1.Controls.Add(this.label4);
			this.groupBox1.Controls.Add(this.BufferSizeUpDown);
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Location = new System.Drawing.Point(12, 90);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(371, 118);
			this.groupBox1.TabIndex = 3;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Rewind Settings";
			// 
			// SmallStateEnabledBox
			// 
			this.RewindEnabledBox.AutoSize = true;
			this.RewindEnabledBox.Location = new System.Drawing.Point(16, 19);
			this.RewindEnabledBox.Name = "RewindEnabledBox";
			this.RewindEnabledBox.Size = new System.Drawing.Size(65, 17);
			this.RewindEnabledBox.TabIndex = 1;
			this.RewindEnabledBox.Text = "Enabled";
			this.RewindEnabledBox.UseVisualStyleBackColor = true;
			// 
			// UseCompression
			// 
			this.UseCompression.AutoSize = true;
			this.UseCompression.Location = new System.Drawing.Point(16, 39);
			this.UseCompression.Name = "UseCompression";
			this.UseCompression.Size = new System.Drawing.Size(306, 17);
			this.UseCompression.TabIndex = 5;
			this.UseCompression.Text = "Use compression (economizes buffer usage at cost of CPU)";
			this.UseCompression.UseVisualStyleBackColor = true;
			this.UseCompression.CheckedChanged += new System.EventHandler(this.UseCompression_CheckedChanged);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 17);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(81, 13);
			this.label1.TabIndex = 5;
			this.label1.Text = "Avg. State Size:";
			// 
			// StateSizeLabel
			// 
			this.StateSizeLabel.AutoSize = true;
			this.StateSizeLabel.Location = new System.Drawing.Point(92, 17);
			this.StateSizeLabel.Name = "StateSizeLabel";
			this.StateSizeLabel.Size = new System.Drawing.Size(30, 13);
			this.StateSizeLabel.TabIndex = 6;
			this.StateSizeLabel.Text = "0 KB";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(149, 69);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(23, 13);
			this.label4.TabIndex = 9;
			this.label4.Text = "MB";
			// 
			// BufferSizeUpDown
			// 
			this.BufferSizeUpDown.Location = new System.Drawing.Point(93, 67);
			this.BufferSizeUpDown.Maximum = new decimal(new int[] {
            2097512,
            0,
            0,
            0});
			this.BufferSizeUpDown.Minimum = new decimal(new int[] {
            64,
            0,
            0,
            0});
			this.BufferSizeUpDown.Name = "BufferSizeUpDown";
			this.BufferSizeUpDown.Size = new System.Drawing.Size(52, 20);
			this.BufferSizeUpDown.TabIndex = 8;
			this.BufferSizeUpDown.Value = new decimal(new int[] {
            512,
            0,
            0,
            0});
			this.BufferSizeUpDown.ValueChanged += new System.EventHandler(this.BufferSizeUpDown_ValueChanged);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(6, 69);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(81, 13);
			this.label3.TabIndex = 7;
			this.label3.Text = "Max buffer size:";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(67, 48);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(26, 13);
			this.label6.TabIndex = 10;
			this.label6.Text = "Full:";
			// 
			// FullnessLabel
			// 
			this.FullnessLabel.AutoSize = true;
			this.FullnessLabel.Location = new System.Drawing.Point(94, 48);
			this.FullnessLabel.Name = "FullnessLabel";
			this.FullnessLabel.Size = new System.Drawing.Size(21, 13);
			this.FullnessLabel.TabIndex = 11;
			this.FullnessLabel.Text = "0%";
			// 
			// groupBox4
			// 
			this.groupBox4.Controls.Add(this.EstTimeLabel);
			this.groupBox4.Controls.Add(this.label11);
			this.groupBox4.Controls.Add(this.ApproxFramesLabel);
			this.groupBox4.Controls.Add(this.label8);
			this.groupBox4.Controls.Add(this.RewindFramesUsedLabel);
			this.groupBox4.Controls.Add(this.label7);
			this.groupBox4.Controls.Add(this.label1);
			this.groupBox4.Controls.Add(this.FullnessLabel);
			this.groupBox4.Controls.Add(this.label6);
			this.groupBox4.Controls.Add(this.StateSizeLabel);
			this.groupBox4.Location = new System.Drawing.Point(12, 12);
			this.groupBox4.Name = "groupBox4";
			this.groupBox4.Size = new System.Drawing.Size(371, 72);
			this.groupBox4.TabIndex = 2;
			this.groupBox4.TabStop = false;
			this.groupBox4.Text = "Current Statistics";
			// 
			// EstTimeLabel
			// 
			this.EstTimeLabel.AutoSize = true;
			this.EstTimeLabel.Location = new System.Drawing.Point(273, 32);
			this.EstTimeLabel.Name = "EstTimeLabel";
			this.EstTimeLabel.Size = new System.Drawing.Size(32, 13);
			this.EstTimeLabel.TabIndex = 19;
			this.EstTimeLabel.Text = "0 min";
			// 
			// label11
			// 
			this.label11.AutoSize = true;
			this.label11.Location = new System.Drawing.Point(221, 32);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(54, 13);
			this.label11.TabIndex = 18;
			this.label11.Text = "Est. Time:";
			// 
			// ApproxFramesLabel
			// 
			this.ApproxFramesLabel.AutoSize = true;
			this.ApproxFramesLabel.Location = new System.Drawing.Point(273, 17);
			this.ApproxFramesLabel.Name = "ApproxFramesLabel";
			this.ApproxFramesLabel.Size = new System.Drawing.Size(47, 13);
			this.ApproxFramesLabel.TabIndex = 15;
			this.ApproxFramesLabel.Text = "0 frames";
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(209, 17);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(66, 13);
			this.label8.TabIndex = 14;
			this.label8.Text = "Est. storage:";
			// 
			// RewindFramesUsedLabel
			// 
			this.RewindFramesUsedLabel.AutoSize = true;
			this.RewindFramesUsedLabel.Location = new System.Drawing.Point(94, 32);
			this.RewindFramesUsedLabel.Name = "RewindFramesUsedLabel";
			this.RewindFramesUsedLabel.Size = new System.Drawing.Size(13, 13);
			this.RewindFramesUsedLabel.TabIndex = 13;
			this.RewindFramesUsedLabel.Text = "0";
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(15, 32);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(78, 13);
			this.label7.TabIndex = 12;
			this.label7.Text = "Frames Stored:";
			// 
			// groupBox6
			// 
			this.groupBox6.Controls.Add(this.rbStatesText);
			this.groupBox6.Controls.Add(this.rbStatesBinary);
			this.groupBox6.Location = new System.Drawing.Point(22, 78);
			this.groupBox6.Name = "groupBox6";
			this.groupBox6.Size = new System.Drawing.Size(215, 48);
			this.groupBox6.TabIndex = 4;
			this.groupBox6.TabStop = false;
			this.groupBox6.Text = "Type";
			// 
			// rbStatesText
			// 
			this.rbStatesText.AutoSize = true;
			this.rbStatesText.Location = new System.Drawing.Point(88, 18);
			this.rbStatesText.Name = "rbStatesText";
			this.rbStatesText.Size = new System.Drawing.Size(46, 17);
			this.rbStatesText.TabIndex = 1;
			this.rbStatesText.TabStop = true;
			this.rbStatesText.Text = "Text";
			this.rbStatesText.UseVisualStyleBackColor = true;
			// 
			// rbStatesBinary
			// 
			this.rbStatesBinary.AutoSize = true;
			this.rbStatesBinary.Location = new System.Drawing.Point(6, 18);
			this.rbStatesBinary.Name = "rbStatesBinary";
			this.rbStatesBinary.Size = new System.Drawing.Size(54, 17);
			this.rbStatesBinary.TabIndex = 1;
			this.rbStatesBinary.TabStop = true;
			this.rbStatesBinary.Text = "Binary";
			this.rbStatesBinary.UseVisualStyleBackColor = true;
			// 
			// btnResetCompression
			// 
			this.btnResetCompression.AutoSize = true;
			this.btnResetCompression.Location = new System.Drawing.Point(243, 34);
			this.btnResetCompression.Name = "btnResetCompression";
			this.btnResetCompression.Size = new System.Drawing.Size(27, 27);
			this.btnResetCompression.TabIndex = 3;
			this.toolTip1.SetToolTip(this.btnResetCompression, "Reset to default");
			this.btnResetCompression.UseVisualStyleBackColor = true;
			this.btnResetCompression.Click += new System.EventHandler(this.BtnResetCompression_Click);
			// 
			// trackBarCompression
			// 
			this.trackBarCompression.LargeChange = 1;
			this.trackBarCompression.Location = new System.Drawing.Point(22, 37);
			this.trackBarCompression.Maximum = 9;
			this.trackBarCompression.Name = "trackBarCompression";
			this.trackBarCompression.Size = new System.Drawing.Size(157, 45);
			this.trackBarCompression.TabIndex = 1;
			this.toolTip1.SetToolTip(this.trackBarCompression, "0 = None; 9 = Maximum");
			this.trackBarCompression.Value = 1;
			this.trackBarCompression.ValueChanged += new System.EventHandler(this.TrackBarCompression_ValueChanged);
			// 
			// nudCompression
			// 
			this.nudCompression.Location = new System.Drawing.Point(185, 37);
			this.nudCompression.Maximum = new decimal(new int[] {
            9,
            0,
            0,
            0});
			this.nudCompression.Name = "nudCompression";
			this.nudCompression.Size = new System.Drawing.Size(52, 20);
			this.nudCompression.TabIndex = 2;
			this.nudCompression.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.nudCompression.ValueChanged += new System.EventHandler(this.NudCompression_ValueChanged);
			// 
			// groupBox7
			// 
			this.groupBox7.Controls.Add(this.label20);
			this.groupBox7.Controls.Add(this.KbLabel);
			this.groupBox7.Controls.Add(this.BigScreenshotNumeric);
			this.groupBox7.Controls.Add(this.LowResLargeScreenshotsCheckbox);
			this.groupBox7.Controls.Add(this.label13);
			this.groupBox7.Controls.Add(this.label14);
			this.groupBox7.Controls.Add(this.ScreenshotInStatesCheckbox);
			this.groupBox7.Controls.Add(this.label15);
			this.groupBox7.Controls.Add(this.label16);
			this.groupBox7.Controls.Add(this.BackupSavestatesCheckbox);
			this.groupBox7.Controls.Add(this.label12);
			this.groupBox7.Controls.Add(this.groupBox6);
			this.groupBox7.Controls.Add(this.btnResetCompression);
			this.groupBox7.Controls.Add(this.nudCompression);
			this.groupBox7.Controls.Add(this.trackBarCompression);
			this.groupBox7.Location = new System.Drawing.Point(389, 12);
			this.groupBox7.Name = "groupBox7";
			this.groupBox7.Size = new System.Drawing.Size(342, 408);
			this.groupBox7.TabIndex = 6;
			this.groupBox7.TabStop = false;
			this.groupBox7.Text = "Savestate Options";
			// 
			// label20
			// 
			this.label20.AutoSize = true;
			this.label20.Location = new System.Drawing.Point(21, 291);
			this.label20.Name = "label20";
			this.label20.Size = new System.Drawing.Size(315, 13);
			this.label20.TabIndex = 34;
			this.label20.Text = "Use a low resolution screenshot for better save/load performance";
			// 
			// KbLabel
			// 
			this.KbLabel.AutoSize = true;
			this.KbLabel.Location = new System.Drawing.Point(276, 271);
			this.KbLabel.Name = "KbLabel";
			this.KbLabel.Size = new System.Drawing.Size(21, 13);
			this.KbLabel.TabIndex = 33;
			this.KbLabel.Text = "KB";
			// 
			// BigScreenshotNumeric
			// 
			this.BigScreenshotNumeric.Location = new System.Drawing.Point(212, 267);
			this.BigScreenshotNumeric.Maximum = new decimal(new int[] {
            8192,
            0,
            0,
            0});
			this.BigScreenshotNumeric.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.BigScreenshotNumeric.Name = "BigScreenshotNumeric";
			this.BigScreenshotNumeric.Size = new System.Drawing.Size(58, 20);
			this.BigScreenshotNumeric.TabIndex = 32;
			this.BigScreenshotNumeric.Value = new decimal(new int[] {
            128,
            0,
            0,
            0});
			// 
			// LowResLargeScreenshotsCheckbox
			// 
			this.LowResLargeScreenshotsCheckbox.AutoSize = true;
			this.LowResLargeScreenshotsCheckbox.Location = new System.Drawing.Point(21, 269);
			this.LowResLargeScreenshotsCheckbox.Name = "LowResLargeScreenshotsCheckbox";
			this.LowResLargeScreenshotsCheckbox.Size = new System.Drawing.Size(195, 17);
			this.LowResLargeScreenshotsCheckbox.TabIndex = 31;
			this.LowResLargeScreenshotsCheckbox.Text = "Low Res Screenshots on buffers >=";
			this.LowResLargeScreenshotsCheckbox.UseVisualStyleBackColor = true;
			// 
			// label13
			// 
			this.label13.AutoSize = true;
			this.label13.Location = new System.Drawing.Point(21, 235);
			this.label13.Name = "label13";
			this.label13.Size = new System.Drawing.Size(186, 13);
			this.label13.TabIndex = 30;
			this.label13.Text = "black screen on the frame it is loaded.";
			// 
			// label14
			// 
			this.label14.AutoSize = true;
			this.label14.Location = new System.Drawing.Point(19, 221);
			this.label14.Name = "label14";
			this.label14.Size = new System.Drawing.Size(290, 13);
			this.label14.TabIndex = 29;
			this.label14.Text = "Saves a screenshot and loads it on loadstate so there isn\'t a";
			// 
			// ScreenshotInStatesCheckbox
			// 
			this.ScreenshotInStatesCheckbox.AutoSize = true;
			this.ScreenshotInStatesCheckbox.Location = new System.Drawing.Point(22, 201);
			this.ScreenshotInStatesCheckbox.Name = "ScreenshotInStatesCheckbox";
			this.ScreenshotInStatesCheckbox.Size = new System.Drawing.Size(180, 17);
			this.ScreenshotInStatesCheckbox.TabIndex = 28;
			this.ScreenshotInStatesCheckbox.Text = "Save a screenshot in savestates";
			this.ScreenshotInStatesCheckbox.UseVisualStyleBackColor = true;
			this.ScreenshotInStatesCheckbox.CheckedChanged += new System.EventHandler(this.ScreenshotInStatesCheckbox_CheckedChanged);
			// 
			// label15
			// 
			this.label15.AutoSize = true;
			this.label15.Location = new System.Drawing.Point(39, 171);
			this.label15.Name = "label15";
			this.label15.Size = new System.Drawing.Size(102, 13);
			this.label15.TabIndex = 27;
			this.label15.Text = "before overwriting it.";
			// 
			// label16
			// 
			this.label16.AutoSize = true;
			this.label16.Location = new System.Drawing.Point(39, 158);
			this.label16.Name = "label16";
			this.label16.Size = new System.Drawing.Size(292, 13);
			this.label16.TabIndex = 26;
			this.label16.Text = "When set, the client will make a backup copy of a savestate";
			// 
			// BackupSavestatesCheckbox
			// 
			this.BackupSavestatesCheckbox.AutoSize = true;
			this.BackupSavestatesCheckbox.Location = new System.Drawing.Point(21, 138);
			this.BackupSavestatesCheckbox.Name = "BackupSavestatesCheckbox";
			this.BackupSavestatesCheckbox.Size = new System.Drawing.Size(119, 17);
			this.BackupSavestatesCheckbox.TabIndex = 25;
			this.BackupSavestatesCheckbox.Text = "Backup Savestates";
			this.BackupSavestatesCheckbox.UseVisualStyleBackColor = true;
			// 
			// label12
			// 
			this.label12.AutoSize = true;
			this.label12.Location = new System.Drawing.Point(19, 21);
			this.label12.Name = "label12";
			this.label12.Size = new System.Drawing.Size(96, 13);
			this.label12.TabIndex = 0;
			this.label12.Text = "Compression Level";
			// 
			// RewindConfig
			// 
			this.AcceptButton = this.OK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.Cancel;
			this.ClientSize = new System.Drawing.Size(741, 505);
			this.Controls.Add(this.groupBox7);
			this.Controls.Add(this.groupBox4);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.Cancel);
			this.Controls.Add(this.OK);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "RewindConfig";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Rewind & Savestate Cofiguration";
			this.Load += new System.EventHandler(this.RewindConfig_Load);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.BufferSizeUpDown)).EndInit();
			this.groupBox4.ResumeLayout(false);
			this.groupBox4.PerformLayout();
			this.groupBox6.ResumeLayout(false);
			this.groupBox6.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBarCompression)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.nudCompression)).EndInit();
			this.groupBox7.ResumeLayout(false);
			this.groupBox7.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.BigScreenshotNumeric)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button OK;
		private System.Windows.Forms.Button Cancel;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.CheckBox RewindEnabledBox;
		private System.Windows.Forms.CheckBox UseCompression;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label StateSizeLabel;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.NumericUpDown BufferSizeUpDown;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label FullnessLabel;
		private System.Windows.Forms.GroupBox groupBox4;
		private System.Windows.Forms.Label RewindFramesUsedLabel;
		private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label ApproxFramesLabel;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label EstTimeLabel;
				private System.Windows.Forms.Label label11;
				private System.Windows.Forms.GroupBox groupBox6;
				private System.Windows.Forms.RadioButton rbStatesText;
				private System.Windows.Forms.RadioButton rbStatesBinary;
				private System.Windows.Forms.ToolTip toolTip1;
				private System.Windows.Forms.TrackBar trackBarCompression;
				private System.Windows.Forms.NumericUpDown nudCompression;
				private System.Windows.Forms.Button btnResetCompression;
				private System.Windows.Forms.GroupBox groupBox7;
				private System.Windows.Forms.Label label12;
				private System.Windows.Forms.Label KbLabel;
				private System.Windows.Forms.NumericUpDown BigScreenshotNumeric;
				private System.Windows.Forms.CheckBox LowResLargeScreenshotsCheckbox;
				private System.Windows.Forms.Label label13;
				private System.Windows.Forms.Label label14;
				private System.Windows.Forms.CheckBox ScreenshotInStatesCheckbox;
				private System.Windows.Forms.Label label15;
				private System.Windows.Forms.Label label16;
				private System.Windows.Forms.CheckBox BackupSavestatesCheckbox;
				private System.Windows.Forms.Label label20;
	}
}