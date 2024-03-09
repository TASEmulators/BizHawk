namespace BizHawk.Client.EmuHawk
{
	partial class SoundConfig
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
            this.Cancel = new System.Windows.Forms.Button();
            this.OK = new System.Windows.Forms.Button();
            this.cbEnableNormal = new System.Windows.Forms.CheckBox();
            this.grpSoundVol = new System.Windows.Forms.GroupBox();
            this.nudRWFF = new System.Windows.Forms.NumericUpDown();
            this.cbEnableRWFF = new System.Windows.Forms.CheckBox();
            this.tbRWFF = new System.Windows.Forms.TrackBar();
            this.tbNormal = new System.Windows.Forms.TrackBar();
            this.nudNormal = new System.Windows.Forms.NumericUpDown();
            this.listBoxSoundDevices = new System.Windows.Forms.ListBox();
            this.BufferSizeNumeric = new System.Windows.Forms.NumericUpDown();
            this.grpOutputMethod = new System.Windows.Forms.GroupBox();
            this.rbOutputMethodOpenAL = new System.Windows.Forms.RadioButton();
            this.rbOutputMethodXAudio2 = new System.Windows.Forms.RadioButton();
            this.rbOutputMethodDirectSound = new System.Windows.Forms.RadioButton();
            this.cbMuteFrameAdvance = new System.Windows.Forms.CheckBox();
            this.cbEnableMaster = new System.Windows.Forms.CheckBox();
            this.cbMuteInBG = new System.Windows.Forms.CheckBox();
            this.cbMuteOnLag = new System.Windows.Forms.CheckBox();
            this.FpsThresholdNumeric = new System.Windows.Forms.NumericUpDown();
            this.fpsThresholdLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label3 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.BufferSizeUnitsLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.BufferSizeLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.SoundDeviceLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.grpSoundVol.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudRWFF)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbRWFF)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbNormal)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudNormal)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.BufferSizeNumeric)).BeginInit();
            this.grpOutputMethod.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.FpsThresholdNumeric)).BeginInit();
            this.SuspendLayout();
            // 
            // Cancel
            // 
            this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Cancel.Location = new System.Drawing.Point(1009, 893);
            this.Cancel.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.Cancel.Name = "Cancel";
            this.Cancel.Size = new System.Drawing.Size(200, 55);
            this.Cancel.TabIndex = 1;
            this.Cancel.Text = "&Cancel";
            this.Cancel.UseVisualStyleBackColor = true;
            this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
            // 
            // OK
            // 
            this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OK.Location = new System.Drawing.Point(793, 893);
            this.OK.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.OK.Name = "OK";
            this.OK.Size = new System.Drawing.Size(200, 55);
            this.OK.TabIndex = 0;
            this.OK.Text = "&OK";
            this.OK.UseVisualStyleBackColor = true;
            this.OK.Click += new System.EventHandler(this.Ok_Click);
            // 
            // cbEnableNormal
            // 
            this.cbEnableNormal.AutoSize = true;
            this.cbEnableNormal.Location = new System.Drawing.Point(16, 48);
            this.cbEnableNormal.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.cbEnableNormal.Name = "cbEnableNormal";
            this.cbEnableNormal.Size = new System.Drawing.Size(111, 36);
            this.cbEnableNormal.TabIndex = 0;
            this.cbEnableNormal.Text = "Ena.";
            this.cbEnableNormal.UseVisualStyleBackColor = true;
            this.cbEnableNormal.CheckedChanged += new System.EventHandler(this.UpdateSoundDialog);
            // 
            // grpSoundVol
            // 
            this.grpSoundVol.Controls.Add(this.nudRWFF);
            this.grpSoundVol.Controls.Add(this.cbEnableRWFF);
            this.grpSoundVol.Controls.Add(this.tbRWFF);
            this.grpSoundVol.Controls.Add(this.label2);
            this.grpSoundVol.Controls.Add(this.label1);
            this.grpSoundVol.Controls.Add(this.tbNormal);
            this.grpSoundVol.Controls.Add(this.nudNormal);
            this.grpSoundVol.Controls.Add(this.cbEnableNormal);
            this.grpSoundVol.Location = new System.Drawing.Point(32, 29);
            this.grpSoundVol.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.grpSoundVol.Name = "grpSoundVol";
            this.grpSoundVol.Padding = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.grpSoundVol.Size = new System.Drawing.Size(312, 608);
            this.grpSoundVol.TabIndex = 2;
            this.grpSoundVol.TabStop = false;
            this.grpSoundVol.Text = "Volume";
            // 
            // nudRWFF
            // 
            this.nudRWFF.Location = new System.Drawing.Point(155, 532);
            this.nudRWFF.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.nudRWFF.Name = "nudRWFF";
            this.nudRWFF.Size = new System.Drawing.Size(120, 38);
            this.nudRWFF.TabIndex = 7;
            this.nudRWFF.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.nudRWFF.ValueChanged += new System.EventHandler(this.nudRWFF_ValueChanged);
            // 
            // cbEnableRWFF
            // 
            this.cbEnableRWFF.AutoSize = true;
            this.cbEnableRWFF.Location = new System.Drawing.Point(155, 48);
            this.cbEnableRWFF.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.cbEnableRWFF.Name = "cbEnableRWFF";
            this.cbEnableRWFF.Size = new System.Drawing.Size(111, 36);
            this.cbEnableRWFF.TabIndex = 4;
            this.cbEnableRWFF.Text = "Ena.";
            this.cbEnableRWFF.UseVisualStyleBackColor = true;
            // 
            // tbRWFF
            // 
            this.tbRWFF.LargeChange = 10;
            this.tbRWFF.Location = new System.Drawing.Point(171, 126);
            this.tbRWFF.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.tbRWFF.Maximum = 100;
            this.tbRWFF.Name = "tbRWFF";
            this.tbRWFF.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.tbRWFF.Size = new System.Drawing.Size(114, 391);
            this.tbRWFF.TabIndex = 6;
            this.tbRWFF.TickFrequency = 10;
            this.tbRWFF.Scroll += new System.EventHandler(this.TbRwff_Scroll);
            // 
            // tbNormal
            // 
            this.tbNormal.LargeChange = 10;
            this.tbNormal.Location = new System.Drawing.Point(21, 126);
            this.tbNormal.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.tbNormal.Maximum = 100;
            this.tbNormal.Name = "tbNormal";
            this.tbNormal.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.tbNormal.Size = new System.Drawing.Size(114, 391);
            this.tbNormal.TabIndex = 2;
            this.tbNormal.TickFrequency = 10;
            this.tbNormal.Scroll += new System.EventHandler(this.TrackBar1_Scroll);
            // 
            // nudNormal
            // 
            this.nudNormal.Location = new System.Drawing.Point(13, 532);
            this.nudNormal.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.nudNormal.Name = "nudNormal";
            this.nudNormal.Size = new System.Drawing.Size(120, 38);
            this.nudNormal.TabIndex = 3;
            this.nudNormal.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.nudNormal.ValueChanged += new System.EventHandler(this.SoundVolNumeric_ValueChanged);
            // 
            // listBoxSoundDevices
            // 
            this.listBoxSoundDevices.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listBoxSoundDevices.FormattingEnabled = true;
            this.listBoxSoundDevices.ItemHeight = 31;
            this.listBoxSoundDevices.Location = new System.Drawing.Point(371, 354);
            this.listBoxSoundDevices.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.listBoxSoundDevices.Name = "listBoxSoundDevices";
            this.listBoxSoundDevices.Size = new System.Drawing.Size(820, 438);
            this.listBoxSoundDevices.TabIndex = 8;
            // 
            // BufferSizeNumeric
            // 
            this.BufferSizeNumeric.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.BufferSizeNumeric.Location = new System.Drawing.Point(535, 806);
            this.BufferSizeNumeric.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.BufferSizeNumeric.Maximum = new decimal(new int[] {
            250,
            0,
            0,
            0});
            this.BufferSizeNumeric.Minimum = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.BufferSizeNumeric.Name = "BufferSizeNumeric";
            this.BufferSizeNumeric.Size = new System.Drawing.Size(157, 38);
            this.BufferSizeNumeric.TabIndex = 10;
            this.BufferSizeNumeric.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            // 
            // grpOutputMethod
            // 
            this.grpOutputMethod.Controls.Add(this.rbOutputMethodOpenAL);
            this.grpOutputMethod.Controls.Add(this.rbOutputMethodXAudio2);
            this.grpOutputMethod.Controls.Add(this.rbOutputMethodDirectSound);
            this.grpOutputMethod.Location = new System.Drawing.Point(779, 29);
            this.grpOutputMethod.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.grpOutputMethod.Name = "grpOutputMethod";
            this.grpOutputMethod.Padding = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.grpOutputMethod.Size = new System.Drawing.Size(267, 215);
            this.grpOutputMethod.TabIndex = 12;
            this.grpOutputMethod.TabStop = false;
            this.grpOutputMethod.Text = "Output Method";
            // 
            // rbOutputMethodOpenAL
            // 
            this.rbOutputMethodOpenAL.AutoSize = true;
            this.rbOutputMethodOpenAL.Location = new System.Drawing.Point(16, 155);
            this.rbOutputMethodOpenAL.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.rbOutputMethodOpenAL.Name = "rbOutputMethodOpenAL";
            this.rbOutputMethodOpenAL.Size = new System.Drawing.Size(156, 36);
            this.rbOutputMethodOpenAL.TabIndex = 2;
            this.rbOutputMethodOpenAL.TabStop = true;
            this.rbOutputMethodOpenAL.Text = "OpenAL";
            this.rbOutputMethodOpenAL.UseVisualStyleBackColor = true;
            this.rbOutputMethodOpenAL.CheckedChanged += new System.EventHandler(this.OutputMethodRadioButtons_CheckedChanged);
            // 
            // rbOutputMethodXAudio2
            // 
            this.rbOutputMethodXAudio2.AutoSize = true;
            this.rbOutputMethodXAudio2.Location = new System.Drawing.Point(16, 100);
            this.rbOutputMethodXAudio2.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.rbOutputMethodXAudio2.Name = "rbOutputMethodXAudio2";
            this.rbOutputMethodXAudio2.Size = new System.Drawing.Size(160, 36);
            this.rbOutputMethodXAudio2.TabIndex = 1;
            this.rbOutputMethodXAudio2.TabStop = true;
            this.rbOutputMethodXAudio2.Text = "XAudio2";
            this.rbOutputMethodXAudio2.UseVisualStyleBackColor = true;
            this.rbOutputMethodXAudio2.CheckedChanged += new System.EventHandler(this.OutputMethodRadioButtons_CheckedChanged);
            // 
            // rbOutputMethodDirectSound
            // 
            this.rbOutputMethodDirectSound.AutoSize = true;
            this.rbOutputMethodDirectSound.Location = new System.Drawing.Point(16, 45);
            this.rbOutputMethodDirectSound.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.rbOutputMethodDirectSound.Name = "rbOutputMethodDirectSound";
            this.rbOutputMethodDirectSound.Size = new System.Drawing.Size(208, 36);
            this.rbOutputMethodDirectSound.TabIndex = 0;
            this.rbOutputMethodDirectSound.TabStop = true;
            this.rbOutputMethodDirectSound.Text = "DirectSound";
            this.rbOutputMethodDirectSound.UseVisualStyleBackColor = true;
            this.rbOutputMethodDirectSound.CheckedChanged += new System.EventHandler(this.OutputMethodRadioButtons_CheckedChanged);
            // 
            // cbMuteFrameAdvance
            // 
            this.cbMuteFrameAdvance.AutoSize = true;
            this.cbMuteFrameAdvance.Location = new System.Drawing.Point(371, 155);
            this.cbMuteFrameAdvance.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.cbMuteFrameAdvance.Name = "cbMuteFrameAdvance";
            this.cbMuteFrameAdvance.Size = new System.Drawing.Size(321, 36);
            this.cbMuteFrameAdvance.TabIndex = 6;
            this.cbMuteFrameAdvance.Text = "Mute Frame Advance";
            this.cbMuteFrameAdvance.UseVisualStyleBackColor = true;
            // 
            // cbEnableMaster
            // 
            this.cbEnableMaster.AutoSize = true;
            this.cbEnableMaster.Location = new System.Drawing.Point(371, 38);
            this.cbEnableMaster.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.cbEnableMaster.Name = "cbEnableMaster";
            this.cbEnableMaster.Size = new System.Drawing.Size(325, 36);
            this.cbEnableMaster.TabIndex = 4;
            this.cbEnableMaster.Text = "Sound Master Enable";
            this.cbEnableMaster.UseVisualStyleBackColor = true;
            this.cbEnableMaster.CheckedChanged += new System.EventHandler(this.UpdateSoundDialog);
            // 
            // cbMuteInBG
            // 
            this.cbMuteInBG.AutoSize = true;
            this.cbMuteInBG.Location = new System.Drawing.Point(371, 208);
            this.cbMuteInBG.Name = "cbMuteInBG";
            this.cbMuteInBG.Size = new System.Drawing.Size(301, 36);
            this.cbMuteInBG.TabIndex = 16;
            this.cbMuteInBG.Text = "Mute in background";
            this.cbMuteInBG.UseVisualStyleBackColor = true;
            // 
            // cbMuteOnLag
            // 
            this.cbMuteOnLag.AutoSize = true;
            this.cbMuteOnLag.Location = new System.Drawing.Point(371, 258);
            this.cbMuteOnLag.Name = "cbMuteOnLag";
            this.cbMuteOnLag.Size = new System.Drawing.Size(200, 36);
            this.cbMuteOnLag.TabIndex = 21;
            this.cbMuteOnLag.Text = "Mute on lag";
            this.cbMuteOnLag.UseVisualStyleBackColor = true;
            this.cbMuteOnLag.CheckedChanged += new System.EventHandler(this.muteOnLag_CheckedChanged);
            // 
            // FpsThresholdNumeric
            // 
            this.FpsThresholdNumeric.Location = new System.Drawing.Point(1071, 260);
            this.FpsThresholdNumeric.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.FpsThresholdNumeric.Name = "FpsThresholdNumeric";
            this.FpsThresholdNumeric.Size = new System.Drawing.Size(120, 38);
            this.FpsThresholdNumeric.TabIndex = 10;
            this.FpsThresholdNumeric.Value = new decimal(new int[] {
            56,
            0,
            0,
            0});
            // 
            // fpsThresholdLabel
            // 
            this.fpsThresholdLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.fpsThresholdLabel.Location = new System.Drawing.Point(648, 262);
            this.fpsThresholdLabel.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.fpsThresholdLabel.Name = "fpsThresholdLabel";
            this.fpsThresholdLabel.Text = "FPS Threshold for Mute on Lag\r\n";
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label3.Location = new System.Drawing.Point(408, 77);
            this.label3.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label3.Name = "label3";
            this.label3.Text = "Controls whether cores\neven generate audio.";
            // 
            // BufferSizeUnitsLabel
            // 
            this.BufferSizeUnitsLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.BufferSizeUnitsLabel.Location = new System.Drawing.Point(708, 808);
            this.BufferSizeUnitsLabel.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.BufferSizeUnitsLabel.Name = "BufferSizeUnitsLabel";
            this.BufferSizeUnitsLabel.Text = "milliseconds";
            // 
            // BufferSizeLabel
            // 
            this.BufferSizeLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.BufferSizeLabel.Location = new System.Drawing.Point(365, 808);
            this.BufferSizeLabel.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.BufferSizeLabel.Name = "BufferSizeLabel";
            this.BufferSizeLabel.Text = "Buffer Size:";
            // 
            // SoundDeviceLabel
            // 
            this.SoundDeviceLabel.Location = new System.Drawing.Point(365, 315);
            this.SoundDeviceLabel.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.SoundDeviceLabel.Name = "SoundDeviceLabel";
            this.SoundDeviceLabel.Text = "Sound Device:";
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label2.Location = new System.Drawing.Point(149, 100);
            this.label2.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label2.Name = "label2";
            this.label2.Text = "RW && FF";
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.Location = new System.Drawing.Point(16, 100);
            this.label1.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label1.Name = "label1";
            this.label1.Text = "Normal";
            // 
            // SoundConfig
            // 
            this.AcceptButton = this.OK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(16F, 31F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.Cancel;
            this.ClientSize = new System.Drawing.Size(1226, 964);
            this.Controls.Add(this.fpsThresholdLabel);
            this.Controls.Add(this.FpsThresholdNumeric);
            this.Controls.Add(this.cbMuteOnLag);
            this.Controls.Add(this.cbMuteInBG);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.cbEnableMaster);
            this.Controls.Add(this.cbMuteFrameAdvance);
            this.Controls.Add(this.grpOutputMethod);
            this.Controls.Add(this.BufferSizeUnitsLabel);
            this.Controls.Add(this.BufferSizeNumeric);
            this.Controls.Add(this.BufferSizeLabel);
            this.Controls.Add(this.SoundDeviceLabel);
            this.Controls.Add(this.listBoxSoundDevices);
            this.Controls.Add(this.grpSoundVol);
            this.Controls.Add(this.OK);
            this.Controls.Add(this.Cancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.MinimumSize = new System.Drawing.Size(691, 496);
            this.Name = "SoundConfig";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Sound Configuration";
            this.Load += new System.EventHandler(this.SoundConfig_Load);
            this.grpSoundVol.ResumeLayout(false);
            this.grpSoundVol.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudRWFF)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbRWFF)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbNormal)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudNormal)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.BufferSizeNumeric)).EndInit();
            this.grpOutputMethod.ResumeLayout(false);
            this.grpOutputMethod.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.FpsThresholdNumeric)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button Cancel;
		private System.Windows.Forms.Button OK;
		private System.Windows.Forms.CheckBox cbEnableNormal;
		private System.Windows.Forms.GroupBox grpSoundVol;
		private System.Windows.Forms.NumericUpDown nudNormal;
		private System.Windows.Forms.TrackBar tbNormal;
		private System.Windows.Forms.ListBox listBoxSoundDevices;
		private BizHawk.WinForms.Controls.LocLabelEx SoundDeviceLabel;
		private BizHawk.WinForms.Controls.LocLabelEx BufferSizeLabel;
		private System.Windows.Forms.NumericUpDown BufferSizeNumeric;
		private BizHawk.WinForms.Controls.LocLabelEx BufferSizeUnitsLabel;
		private System.Windows.Forms.GroupBox grpOutputMethod;
		private System.Windows.Forms.RadioButton rbOutputMethodXAudio2;
		private System.Windows.Forms.RadioButton rbOutputMethodDirectSound;
		private System.Windows.Forms.RadioButton rbOutputMethodOpenAL;
		private System.Windows.Forms.NumericUpDown nudRWFF;
		private System.Windows.Forms.CheckBox cbEnableRWFF;
		private System.Windows.Forms.TrackBar tbRWFF;
		private BizHawk.WinForms.Controls.LocLabelEx label2;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private System.Windows.Forms.CheckBox cbMuteFrameAdvance;
		private System.Windows.Forms.CheckBox cbEnableMaster;
		private BizHawk.WinForms.Controls.LocLabelEx label3;
		private System.Windows.Forms.CheckBox cbMuteInBG;
		private System.Windows.Forms.CheckBox cbMuteOnLag;
		private System.Windows.Forms.NumericUpDown FpsThresholdNumeric;
		private WinForms.Controls.LocLabelEx fpsThresholdLabel;
	}
}