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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RewindConfig));
            this.OK = new System.Windows.Forms.Button();
            this.Cancel = new System.Windows.Forms.Button();
            this.RewindEnabledBox = new System.Windows.Forms.CheckBox();
            this.UseCompression = new System.Windows.Forms.CheckBox();
            this.label4 = new BizHawk.WinForms.Controls.LabelEx();
            this.BufferSizeUpDown = new System.Windows.Forms.NumericUpDown();
            this.label3 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.StateSizeLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label6 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.FullnessLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.TargetRewindIntervalRadioButton = new System.Windows.Forms.RadioButton();
            this.TargetFrameLengthRadioButton = new System.Windows.Forms.RadioButton();
            this.locSingleRowFLP1 = new BizHawk.WinForms.Controls.LocSingleRowFLP();
            this.labelEx3 = new BizHawk.WinForms.Controls.LabelEx();
            this.labelEx2 = new BizHawk.WinForms.Controls.LabelEx();
            this.labelEx1 = new BizHawk.WinForms.Controls.LabelEx();
            this.cbDeltaCompression = new System.Windows.Forms.CheckBox();
            this.TargetFrameLengthNumeric = new System.Windows.Forms.NumericUpDown();
            this.TargetRewindIntervalNumeric = new System.Windows.Forms.NumericUpDown();
            this.EstTimeLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label11 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.ApproxFramesLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label8 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.RewindFramesUsedLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label7 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.groupBox6 = new System.Windows.Forms.GroupBox();
            this.rbStatesText = new System.Windows.Forms.RadioButton();
            this.rbStatesBinary = new System.Windows.Forms.RadioButton();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.btnResetCompression = new System.Windows.Forms.Button();
            this.trackBarCompression = new System.Windows.Forms.TrackBar();
            this.nudCompression = new System.Windows.Forms.NumericUpDown();
            this.groupBox7 = new System.Windows.Forms.GroupBox();
            this.label20 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.KbLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.BigScreenshotNumeric = new System.Windows.Forms.NumericUpDown();
            this.LowResLargeScreenshotsCheckbox = new System.Windows.Forms.CheckBox();
            this.label13 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label14 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.ScreenshotInStatesCheckbox = new System.Windows.Forms.CheckBox();
            this.label15 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label16 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.BackupSavestatesCheckbox = new System.Windows.Forms.CheckBox();
            this.label12 = new BizHawk.WinForms.Controls.LocLabelEx();
            ((System.ComponentModel.ISupportInitialize)(this.BufferSizeUpDown)).BeginInit();
            this.groupBox4.SuspendLayout();
            this.locSingleRowFLP1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TargetFrameLengthNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TargetRewindIntervalNumeric)).BeginInit();
            this.groupBox6.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarCompression)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudCompression)).BeginInit();
            this.groupBox7.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.BigScreenshotNumeric)).BeginInit();
            this.SuspendLayout();
            // 
            // OK
            // 
            resources.ApplyResources(this.OK, "OK");
            this.OK.Name = "OK";
            this.toolTip1.SetToolTip(this.OK, resources.GetString("OK.ToolTip"));
            this.OK.UseVisualStyleBackColor = true;
            this.OK.Click += new System.EventHandler(this.Ok_Click);
            // 
            // Cancel
            // 
            resources.ApplyResources(this.Cancel, "Cancel");
            this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Cancel.Name = "Cancel";
            this.toolTip1.SetToolTip(this.Cancel, resources.GetString("Cancel.ToolTip"));
            this.Cancel.UseVisualStyleBackColor = true;
            this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
            // 
            // RewindEnabledBox
            // 
            resources.ApplyResources(this.RewindEnabledBox, "RewindEnabledBox");
            this.RewindEnabledBox.Name = "RewindEnabledBox";
            this.toolTip1.SetToolTip(this.RewindEnabledBox, resources.GetString("RewindEnabledBox.ToolTip"));
            this.RewindEnabledBox.UseVisualStyleBackColor = true;
            // 
            // UseCompression
            // 
            resources.ApplyResources(this.UseCompression, "UseCompression");
            this.UseCompression.Name = "UseCompression";
            this.toolTip1.SetToolTip(this.UseCompression, resources.GetString("UseCompression.ToolTip"));
            this.UseCompression.UseVisualStyleBackColor = true;
            this.UseCompression.CheckedChanged += new System.EventHandler(this.UseCompression_CheckedChanged);
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            this.toolTip1.SetToolTip(this.label4, resources.GetString("label4.ToolTip"));
            // 
            // BufferSizeUpDown
            // 
            resources.ApplyResources(this.BufferSizeUpDown, "BufferSizeUpDown");
            this.BufferSizeUpDown.Maximum = new decimal(new int[] {
            15,
            0,
            0,
            0});
            this.BufferSizeUpDown.Minimum = new decimal(new int[] {
            6,
            0,
            0,
            0});
            this.BufferSizeUpDown.Name = "BufferSizeUpDown";
            this.toolTip1.SetToolTip(this.BufferSizeUpDown, resources.GetString("BufferSizeUpDown.ToolTip"));
            this.BufferSizeUpDown.Value = new decimal(new int[] {
            9,
            0,
            0,
            0});
            this.BufferSizeUpDown.ValueChanged += new System.EventHandler(this.BufferSizeUpDown_ValueChanged);
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            this.toolTip1.SetToolTip(this.label3, resources.GetString("label3.ToolTip"));
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            this.toolTip1.SetToolTip(this.label1, resources.GetString("label1.ToolTip"));
            // 
            // StateSizeLabel
            // 
            resources.ApplyResources(this.StateSizeLabel, "StateSizeLabel");
            this.StateSizeLabel.Name = "StateSizeLabel";
            this.toolTip1.SetToolTip(this.StateSizeLabel, resources.GetString("StateSizeLabel.ToolTip"));
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            this.toolTip1.SetToolTip(this.label6, resources.GetString("label6.ToolTip"));
            // 
            // FullnessLabel
            // 
            resources.ApplyResources(this.FullnessLabel, "FullnessLabel");
            this.FullnessLabel.Name = "FullnessLabel";
            this.toolTip1.SetToolTip(this.FullnessLabel, resources.GetString("FullnessLabel.ToolTip"));
            // 
            // groupBox4
            // 
            resources.ApplyResources(this.groupBox4, "groupBox4");
            this.groupBox4.Controls.Add(this.TargetRewindIntervalRadioButton);
            this.groupBox4.Controls.Add(this.TargetFrameLengthRadioButton);
            this.groupBox4.Controls.Add(this.locSingleRowFLP1);
            this.groupBox4.Controls.Add(this.cbDeltaCompression);
            this.groupBox4.Controls.Add(this.TargetFrameLengthNumeric);
            this.groupBox4.Controls.Add(this.TargetRewindIntervalNumeric);
            this.groupBox4.Controls.Add(this.UseCompression);
            this.groupBox4.Controls.Add(this.RewindEnabledBox);
            this.groupBox4.Controls.Add(this.label3);
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
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.TabStop = false;
            this.toolTip1.SetToolTip(this.groupBox4, resources.GetString("groupBox4.ToolTip"));
            // 
            // TargetRewindIntervalRadioButton
            // 
            resources.ApplyResources(this.TargetRewindIntervalRadioButton, "TargetRewindIntervalRadioButton");
            this.TargetRewindIntervalRadioButton.Name = "TargetRewindIntervalRadioButton";
            this.TargetRewindIntervalRadioButton.TabStop = true;
            this.toolTip1.SetToolTip(this.TargetRewindIntervalRadioButton, resources.GetString("TargetRewindIntervalRadioButton.ToolTip"));
            this.TargetRewindIntervalRadioButton.UseVisualStyleBackColor = true;
            this.TargetRewindIntervalRadioButton.CheckedChanged += new System.EventHandler(this.RewindInterval_CheckedChanged);
            // 
            // TargetFrameLengthRadioButton
            // 
            resources.ApplyResources(this.TargetFrameLengthRadioButton, "TargetFrameLengthRadioButton");
            this.TargetFrameLengthRadioButton.Name = "TargetFrameLengthRadioButton";
            this.TargetFrameLengthRadioButton.TabStop = true;
            this.toolTip1.SetToolTip(this.TargetFrameLengthRadioButton, resources.GetString("TargetFrameLengthRadioButton.ToolTip"));
            this.TargetFrameLengthRadioButton.UseVisualStyleBackColor = true;
            // 
            // locSingleRowFLP1
            // 
            resources.ApplyResources(this.locSingleRowFLP1, "locSingleRowFLP1");
            this.locSingleRowFLP1.Controls.Add(this.labelEx3);
            this.locSingleRowFLP1.Controls.Add(this.BufferSizeUpDown);
            this.locSingleRowFLP1.Controls.Add(this.labelEx2);
            this.locSingleRowFLP1.Controls.Add(this.labelEx1);
            this.locSingleRowFLP1.Controls.Add(this.label4);
            this.locSingleRowFLP1.Name = "locSingleRowFLP1";
            this.toolTip1.SetToolTip(this.locSingleRowFLP1, resources.GetString("locSingleRowFLP1.ToolTip"));
            // 
            // labelEx3
            // 
            resources.ApplyResources(this.labelEx3, "labelEx3");
            this.labelEx3.Name = "labelEx3";
            this.toolTip1.SetToolTip(this.labelEx3, resources.GetString("labelEx3.ToolTip"));
            // 
            // labelEx2
            // 
            resources.ApplyResources(this.labelEx2, "labelEx2");
            this.labelEx2.Name = "labelEx2";
            this.toolTip1.SetToolTip(this.labelEx2, resources.GetString("labelEx2.ToolTip"));
            // 
            // labelEx1
            // 
            resources.ApplyResources(this.labelEx1, "labelEx1");
            this.labelEx1.Name = "labelEx1";
            this.toolTip1.SetToolTip(this.labelEx1, resources.GetString("labelEx1.ToolTip"));
            // 
            // cbDeltaCompression
            // 
            resources.ApplyResources(this.cbDeltaCompression, "cbDeltaCompression");
            this.cbDeltaCompression.Name = "cbDeltaCompression";
            this.toolTip1.SetToolTip(this.cbDeltaCompression, resources.GetString("cbDeltaCompression.ToolTip"));
            this.cbDeltaCompression.UseVisualStyleBackColor = true;
            // 
            // TargetFrameLengthNumeric
            // 
            resources.ApplyResources(this.TargetFrameLengthNumeric, "TargetFrameLengthNumeric");
            this.TargetFrameLengthNumeric.Maximum = new decimal(new int[] {
            500000,
            0,
            0,
            0});
            this.TargetFrameLengthNumeric.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.TargetFrameLengthNumeric.Name = "TargetFrameLengthNumeric";
            this.toolTip1.SetToolTip(this.TargetFrameLengthNumeric, resources.GetString("TargetFrameLengthNumeric.ToolTip"));
            this.TargetFrameLengthNumeric.Value = new decimal(new int[] {
            600,
            0,
            0,
            0});
            this.TargetFrameLengthNumeric.ValueChanged += new System.EventHandler(this.FrameLength_ValueChanged);
            // 
            // TargetRewindIntervalNumeric
            // 
            resources.ApplyResources(this.TargetRewindIntervalNumeric, "TargetRewindIntervalNumeric");
            this.TargetRewindIntervalNumeric.Maximum = new decimal(new int[] {
            500000,
            0,
            0,
            0});
            this.TargetRewindIntervalNumeric.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.TargetRewindIntervalNumeric.Name = "TargetRewindIntervalNumeric";
            this.toolTip1.SetToolTip(this.TargetRewindIntervalNumeric, resources.GetString("TargetRewindIntervalNumeric.ToolTip"));
            this.TargetRewindIntervalNumeric.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.TargetRewindIntervalNumeric.ValueChanged += new System.EventHandler(this.RewindInterval_ValueChanged);
            // 
            // EstTimeLabel
            // 
            resources.ApplyResources(this.EstTimeLabel, "EstTimeLabel");
            this.EstTimeLabel.Name = "EstTimeLabel";
            this.toolTip1.SetToolTip(this.EstTimeLabel, resources.GetString("EstTimeLabel.ToolTip"));
            // 
            // label11
            // 
            resources.ApplyResources(this.label11, "label11");
            this.label11.Name = "label11";
            this.toolTip1.SetToolTip(this.label11, resources.GetString("label11.ToolTip"));
            // 
            // ApproxFramesLabel
            // 
            resources.ApplyResources(this.ApproxFramesLabel, "ApproxFramesLabel");
            this.ApproxFramesLabel.Name = "ApproxFramesLabel";
            this.toolTip1.SetToolTip(this.ApproxFramesLabel, resources.GetString("ApproxFramesLabel.ToolTip"));
            // 
            // label8
            // 
            resources.ApplyResources(this.label8, "label8");
            this.label8.Name = "label8";
            this.toolTip1.SetToolTip(this.label8, resources.GetString("label8.ToolTip"));
            // 
            // RewindFramesUsedLabel
            // 
            resources.ApplyResources(this.RewindFramesUsedLabel, "RewindFramesUsedLabel");
            this.RewindFramesUsedLabel.Name = "RewindFramesUsedLabel";
            this.toolTip1.SetToolTip(this.RewindFramesUsedLabel, resources.GetString("RewindFramesUsedLabel.ToolTip"));
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            this.toolTip1.SetToolTip(this.label7, resources.GetString("label7.ToolTip"));
            // 
            // groupBox6
            // 
            resources.ApplyResources(this.groupBox6, "groupBox6");
            this.groupBox6.Controls.Add(this.rbStatesText);
            this.groupBox6.Controls.Add(this.rbStatesBinary);
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.TabStop = false;
            this.toolTip1.SetToolTip(this.groupBox6, resources.GetString("groupBox6.ToolTip"));
            // 
            // rbStatesText
            // 
            resources.ApplyResources(this.rbStatesText, "rbStatesText");
            this.rbStatesText.Name = "rbStatesText";
            this.rbStatesText.TabStop = true;
            this.toolTip1.SetToolTip(this.rbStatesText, resources.GetString("rbStatesText.ToolTip"));
            this.rbStatesText.UseVisualStyleBackColor = true;
            // 
            // rbStatesBinary
            // 
            resources.ApplyResources(this.rbStatesBinary, "rbStatesBinary");
            this.rbStatesBinary.Name = "rbStatesBinary";
            this.rbStatesBinary.TabStop = true;
            this.toolTip1.SetToolTip(this.rbStatesBinary, resources.GetString("rbStatesBinary.ToolTip"));
            this.rbStatesBinary.UseVisualStyleBackColor = true;
            // 
            // btnResetCompression
            // 
            resources.ApplyResources(this.btnResetCompression, "btnResetCompression");
            this.btnResetCompression.Name = "btnResetCompression";
            this.toolTip1.SetToolTip(this.btnResetCompression, resources.GetString("btnResetCompression.ToolTip"));
            this.btnResetCompression.UseVisualStyleBackColor = true;
            this.btnResetCompression.Click += new System.EventHandler(this.BtnResetCompression_Click);
            // 
            // trackBarCompression
            // 
            resources.ApplyResources(this.trackBarCompression, "trackBarCompression");
            this.trackBarCompression.LargeChange = 1;
            this.trackBarCompression.Maximum = 9;
            this.trackBarCompression.Name = "trackBarCompression";
            this.toolTip1.SetToolTip(this.trackBarCompression, resources.GetString("trackBarCompression.ToolTip"));
            this.trackBarCompression.Value = 1;
            this.trackBarCompression.ValueChanged += new System.EventHandler(this.TrackBarCompression_ValueChanged);
            // 
            // nudCompression
            // 
            resources.ApplyResources(this.nudCompression, "nudCompression");
            this.nudCompression.Maximum = new decimal(new int[] {
            9,
            0,
            0,
            0});
            this.nudCompression.Name = "nudCompression";
            this.toolTip1.SetToolTip(this.nudCompression, resources.GetString("nudCompression.ToolTip"));
            this.nudCompression.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudCompression.ValueChanged += new System.EventHandler(this.NudCompression_ValueChanged);
            // 
            // groupBox7
            // 
            resources.ApplyResources(this.groupBox7, "groupBox7");
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
            this.groupBox7.Name = "groupBox7";
            this.groupBox7.TabStop = false;
            this.toolTip1.SetToolTip(this.groupBox7, resources.GetString("groupBox7.ToolTip"));
            // 
            // label20
            // 
            resources.ApplyResources(this.label20, "label20");
            this.label20.Name = "label20";
            this.toolTip1.SetToolTip(this.label20, resources.GetString("label20.ToolTip"));
            // 
            // KbLabel
            // 
            resources.ApplyResources(this.KbLabel, "KbLabel");
            this.KbLabel.Name = "KbLabel";
            this.toolTip1.SetToolTip(this.KbLabel, resources.GetString("KbLabel.ToolTip"));
            // 
            // BigScreenshotNumeric
            // 
            resources.ApplyResources(this.BigScreenshotNumeric, "BigScreenshotNumeric");
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
            this.toolTip1.SetToolTip(this.BigScreenshotNumeric, resources.GetString("BigScreenshotNumeric.ToolTip"));
            this.BigScreenshotNumeric.Value = new decimal(new int[] {
            128,
            0,
            0,
            0});
            // 
            // LowResLargeScreenshotsCheckbox
            // 
            resources.ApplyResources(this.LowResLargeScreenshotsCheckbox, "LowResLargeScreenshotsCheckbox");
            this.LowResLargeScreenshotsCheckbox.Name = "LowResLargeScreenshotsCheckbox";
            this.toolTip1.SetToolTip(this.LowResLargeScreenshotsCheckbox, resources.GetString("LowResLargeScreenshotsCheckbox.ToolTip"));
            this.LowResLargeScreenshotsCheckbox.UseVisualStyleBackColor = true;
            // 
            // label13
            // 
            resources.ApplyResources(this.label13, "label13");
            this.label13.Name = "label13";
            this.toolTip1.SetToolTip(this.label13, resources.GetString("label13.ToolTip"));
            // 
            // label14
            // 
            resources.ApplyResources(this.label14, "label14");
            this.label14.Name = "label14";
            this.toolTip1.SetToolTip(this.label14, resources.GetString("label14.ToolTip"));
            // 
            // ScreenshotInStatesCheckbox
            // 
            resources.ApplyResources(this.ScreenshotInStatesCheckbox, "ScreenshotInStatesCheckbox");
            this.ScreenshotInStatesCheckbox.Name = "ScreenshotInStatesCheckbox";
            this.toolTip1.SetToolTip(this.ScreenshotInStatesCheckbox, resources.GetString("ScreenshotInStatesCheckbox.ToolTip"));
            this.ScreenshotInStatesCheckbox.UseVisualStyleBackColor = true;
            this.ScreenshotInStatesCheckbox.CheckedChanged += new System.EventHandler(this.ScreenshotInStatesCheckbox_CheckedChanged);
            // 
            // label15
            // 
            resources.ApplyResources(this.label15, "label15");
            this.label15.Name = "label15";
            this.toolTip1.SetToolTip(this.label15, resources.GetString("label15.ToolTip"));
            // 
            // label16
            // 
            resources.ApplyResources(this.label16, "label16");
            this.label16.Name = "label16";
            this.toolTip1.SetToolTip(this.label16, resources.GetString("label16.ToolTip"));
            // 
            // BackupSavestatesCheckbox
            // 
            resources.ApplyResources(this.BackupSavestatesCheckbox, "BackupSavestatesCheckbox");
            this.BackupSavestatesCheckbox.Name = "BackupSavestatesCheckbox";
            this.toolTip1.SetToolTip(this.BackupSavestatesCheckbox, resources.GetString("BackupSavestatesCheckbox.ToolTip"));
            this.BackupSavestatesCheckbox.UseVisualStyleBackColor = true;
            // 
            // label12
            // 
            resources.ApplyResources(this.label12, "label12");
            this.label12.Name = "label12";
            this.toolTip1.SetToolTip(this.label12, resources.GetString("label12.ToolTip"));
            // 
            // RewindConfig
            // 
            this.AcceptButton = this.OK;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.Cancel;
            this.Controls.Add(this.groupBox7);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.Cancel);
            this.Controls.Add(this.OK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "RewindConfig";
            this.toolTip1.SetToolTip(this, resources.GetString("$this.ToolTip"));
            this.Load += new System.EventHandler(this.RewindConfig_Load);
            ((System.ComponentModel.ISupportInitialize)(this.BufferSizeUpDown)).EndInit();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.locSingleRowFLP1.ResumeLayout(false);
            this.locSingleRowFLP1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TargetFrameLengthNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TargetRewindIntervalNumeric)).EndInit();
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
		private System.Windows.Forms.CheckBox RewindEnabledBox;
		private System.Windows.Forms.CheckBox UseCompression;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private BizHawk.WinForms.Controls.LocLabelEx StateSizeLabel;
		private BizHawk.WinForms.Controls.LabelEx label4;
		private System.Windows.Forms.NumericUpDown BufferSizeUpDown;
		private BizHawk.WinForms.Controls.LocLabelEx label3;
		private BizHawk.WinForms.Controls.LocLabelEx label6;
		private BizHawk.WinForms.Controls.LocLabelEx FullnessLabel;
		private System.Windows.Forms.GroupBox groupBox4;
		private BizHawk.WinForms.Controls.LocLabelEx RewindFramesUsedLabel;
		private BizHawk.WinForms.Controls.LocLabelEx label7;
		private BizHawk.WinForms.Controls.LocLabelEx ApproxFramesLabel;
		private BizHawk.WinForms.Controls.LocLabelEx label8;
		private BizHawk.WinForms.Controls.LocLabelEx EstTimeLabel;
		private BizHawk.WinForms.Controls.LocLabelEx label11;
		private System.Windows.Forms.GroupBox groupBox6;
		private System.Windows.Forms.RadioButton rbStatesText;
		private System.Windows.Forms.RadioButton rbStatesBinary;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.TrackBar trackBarCompression;
		private System.Windows.Forms.NumericUpDown nudCompression;
		private System.Windows.Forms.Button btnResetCompression;
		private System.Windows.Forms.GroupBox groupBox7;
		private BizHawk.WinForms.Controls.LocLabelEx label12;
		private BizHawk.WinForms.Controls.LocLabelEx KbLabel;
		private System.Windows.Forms.NumericUpDown BigScreenshotNumeric;
		private System.Windows.Forms.CheckBox LowResLargeScreenshotsCheckbox;
		private BizHawk.WinForms.Controls.LocLabelEx label13;
		private BizHawk.WinForms.Controls.LocLabelEx label14;
		private System.Windows.Forms.CheckBox ScreenshotInStatesCheckbox;
		private BizHawk.WinForms.Controls.LocLabelEx label15;
		private BizHawk.WinForms.Controls.LocLabelEx label16;
		private System.Windows.Forms.CheckBox BackupSavestatesCheckbox;
		private BizHawk.WinForms.Controls.LocLabelEx label20;
		private System.Windows.Forms.NumericUpDown TargetFrameLengthNumeric;
		private System.Windows.Forms.NumericUpDown TargetRewindIntervalNumeric;
		private System.Windows.Forms.CheckBox cbDeltaCompression;
		private WinForms.Controls.LocSingleRowFLP locSingleRowFLP1;
		private WinForms.Controls.LabelEx labelEx3;
		private WinForms.Controls.LabelEx labelEx2;
		private WinForms.Controls.LabelEx labelEx1;
		private System.Windows.Forms.RadioButton TargetFrameLengthRadioButton;
		private System.Windows.Forms.RadioButton TargetRewindIntervalRadioButton;
	}
}
