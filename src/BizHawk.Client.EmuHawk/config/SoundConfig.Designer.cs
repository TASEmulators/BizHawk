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
			this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.tbNormal = new System.Windows.Forms.TrackBar();
			this.nudNormal = new System.Windows.Forms.NumericUpDown();
			this.listBoxSoundDevices = new System.Windows.Forms.ListBox();
			this.SoundDeviceLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.BufferSizeLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.BufferSizeNumeric = new System.Windows.Forms.NumericUpDown();
			this.BufferSizeUnitsLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.grpOutputMethod = new System.Windows.Forms.GroupBox();
			this.rbOutputMethodOpenAL = new System.Windows.Forms.RadioButton();
			this.rbOutputMethodXAudio2 = new System.Windows.Forms.RadioButton();
			this.cbMuteFrameAdvance = new System.Windows.Forms.CheckBox();
			this.cbEnableMaster = new System.Windows.Forms.CheckBox();
			this.label3 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.grpSoundVol.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.nudRWFF)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.tbRWFF)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.tbNormal)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.nudNormal)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.BufferSizeNumeric)).BeginInit();
			this.grpOutputMethod.SuspendLayout();
			this.SuspendLayout();
			// 
			// Cancel
			// 
			this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.Cancel.Location = new System.Drawing.Point(317, 244);
			this.Cancel.Name = "Cancel";
			this.Cancel.Size = new System.Drawing.Size(75, 23);
			this.Cancel.TabIndex = 1;
			this.Cancel.Text = "&Cancel";
			this.Cancel.UseVisualStyleBackColor = true;
			this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
			// 
			// OK
			// 
			this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OK.Location = new System.Drawing.Point(236, 244);
			this.OK.Name = "OK";
			this.OK.Size = new System.Drawing.Size(75, 23);
			this.OK.TabIndex = 0;
			this.OK.Text = "&OK";
			this.OK.UseVisualStyleBackColor = true;
			this.OK.Click += new System.EventHandler(this.Ok_Click);
			// 
			// cbEnableNormal
			// 
			this.cbEnableNormal.AutoSize = true;
			this.cbEnableNormal.Location = new System.Drawing.Point(6, 20);
			this.cbEnableNormal.Name = "cbEnableNormal";
			this.cbEnableNormal.Size = new System.Drawing.Size(48, 17);
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
			this.grpSoundVol.Location = new System.Drawing.Point(12, 12);
			this.grpSoundVol.Name = "grpSoundVol";
			this.grpSoundVol.Size = new System.Drawing.Size(117, 255);
			this.grpSoundVol.TabIndex = 2;
			this.grpSoundVol.TabStop = false;
			this.grpSoundVol.Text = "Volume";
			// 
			// nudRWFF
			// 
			this.nudRWFF.Location = new System.Drawing.Point(58, 223);
			this.nudRWFF.Name = "nudRWFF";
			this.nudRWFF.Size = new System.Drawing.Size(45, 20);
			this.nudRWFF.TabIndex = 7;
			this.nudRWFF.Value = new decimal(new int[] { 100, 0, 0, 0 });
			this.nudRWFF.ValueChanged += new System.EventHandler(this.nudRWFF_ValueChanged);
			// 
			// cbEnableRWFF
			// 
			this.cbEnableRWFF.AutoSize = true;
			this.cbEnableRWFF.Location = new System.Drawing.Point(58, 20);
			this.cbEnableRWFF.Name = "cbEnableRWFF";
			this.cbEnableRWFF.Size = new System.Drawing.Size(48, 17);
			this.cbEnableRWFF.TabIndex = 4;
			this.cbEnableRWFF.Text = "Ena.";
			this.cbEnableRWFF.UseVisualStyleBackColor = true;
			// 
			// tbRWFF
			// 
			this.tbRWFF.LargeChange = 10;
			this.tbRWFF.Location = new System.Drawing.Point(64, 53);
			this.tbRWFF.Maximum = 100;
			this.tbRWFF.Name = "tbRWFF";
			this.tbRWFF.Orientation = System.Windows.Forms.Orientation.Vertical;
			this.tbRWFF.Size = new System.Drawing.Size(45, 164);
			this.tbRWFF.TabIndex = 6;
			this.tbRWFF.TickFrequency = 10;
			this.tbRWFF.Scroll += new System.EventHandler(this.TbRwff_Scroll);
			// 
			// label2
			// 
			this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label2.Location = new System.Drawing.Point(56, 42);
			this.label2.Name = "label2";
			this.label2.Text = "RW && FF";
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label1.Location = new System.Drawing.Point(6, 42);
			this.label1.Name = "label1";
			this.label1.Text = "Normal";
			// 
			// tbNormal
			// 
			this.tbNormal.LargeChange = 10;
			this.tbNormal.Location = new System.Drawing.Point(8, 53);
			this.tbNormal.Maximum = 100;
			this.tbNormal.Name = "tbNormal";
			this.tbNormal.Orientation = System.Windows.Forms.Orientation.Vertical;
			this.tbNormal.Size = new System.Drawing.Size(45, 164);
			this.tbNormal.TabIndex = 2;
			this.tbNormal.TickFrequency = 10;
			this.tbNormal.Scroll += new System.EventHandler(this.TrackBar1_Scroll);
			// 
			// nudNormal
			// 
			this.nudNormal.Location = new System.Drawing.Point(5, 223);
			this.nudNormal.Name = "nudNormal";
			this.nudNormal.Size = new System.Drawing.Size(45, 20);
			this.nudNormal.TabIndex = 3;
			this.nudNormal.Value = new decimal(new int[] { 100, 0, 0, 0 });
			this.nudNormal.ValueChanged += new System.EventHandler(this.SoundVolNumeric_ValueChanged);
			// 
			// listBoxSoundDevices
			// 
			this.listBoxSoundDevices.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
			this.listBoxSoundDevices.FormattingEnabled = true;
			this.listBoxSoundDevices.Location = new System.Drawing.Point(138, 110);
			this.listBoxSoundDevices.Name = "listBoxSoundDevices";
			this.listBoxSoundDevices.Size = new System.Drawing.Size(254, 95);
			this.listBoxSoundDevices.TabIndex = 8;
			// 
			// SoundDeviceLabel
			// 
			this.SoundDeviceLabel.Location = new System.Drawing.Point(135, 89);
			this.SoundDeviceLabel.Name = "SoundDeviceLabel";
			this.SoundDeviceLabel.Text = "Sound Device:";
			// 
			// BufferSizeLabel
			// 
			this.BufferSizeLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.BufferSizeLabel.Location = new System.Drawing.Point(135, 210);
			this.BufferSizeLabel.Name = "BufferSizeLabel";
			this.BufferSizeLabel.Text = "Buffer Size:";
			// 
			// BufferSizeNumeric
			// 
			this.BufferSizeNumeric.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.BufferSizeNumeric.Location = new System.Drawing.Point(202, 208);
			this.BufferSizeNumeric.Maximum = new decimal(new int[] { 250, 0, 0, 0 });
			this.BufferSizeNumeric.Minimum = new decimal(new int[] { 30, 0, 0, 0 });
			this.BufferSizeNumeric.Name = "BufferSizeNumeric";
			this.BufferSizeNumeric.Size = new System.Drawing.Size(59, 20);
			this.BufferSizeNumeric.TabIndex = 10;
			this.BufferSizeNumeric.Value = new decimal(new int[] { 100, 0, 0, 0 });
			// 
			// BufferSizeUnitsLabel
			// 
			this.BufferSizeUnitsLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.BufferSizeUnitsLabel.Location = new System.Drawing.Point(267, 210);
			this.BufferSizeUnitsLabel.Name = "BufferSizeUnitsLabel";
			this.BufferSizeUnitsLabel.Text = "milliseconds";
			// 
			// grpOutputMethod
			// 
			this.grpOutputMethod.Controls.Add(this.rbOutputMethodOpenAL);
			this.grpOutputMethod.Controls.Add(this.rbOutputMethodXAudio2);
			this.grpOutputMethod.Location = new System.Drawing.Point(292, 12);
			this.grpOutputMethod.Name = "grpOutputMethod";
			this.grpOutputMethod.Size = new System.Drawing.Size(100, 73);
			this.grpOutputMethod.TabIndex = 12;
			this.grpOutputMethod.TabStop = false;
			this.grpOutputMethod.Text = "Output Method";
			// 
			// rbOutputMethodOpenAL
			// 
			this.rbOutputMethodOpenAL.AutoSize = true;
			this.rbOutputMethodOpenAL.Location = new System.Drawing.Point(6, 43);
			this.rbOutputMethodOpenAL.Name = "rbOutputMethodOpenAL";
			this.rbOutputMethodOpenAL.Size = new System.Drawing.Size(64, 17);
			this.rbOutputMethodOpenAL.TabIndex = 2;
			this.rbOutputMethodOpenAL.TabStop = true;
			this.rbOutputMethodOpenAL.Text = "OpenAL";
			this.rbOutputMethodOpenAL.UseVisualStyleBackColor = true;
			this.rbOutputMethodOpenAL.CheckedChanged += new System.EventHandler(this.OutputMethodRadioButtons_CheckedChanged);
			// 
			// rbOutputMethodXAudio2
			// 
			this.rbOutputMethodXAudio2.AutoSize = true;
			this.rbOutputMethodXAudio2.Location = new System.Drawing.Point(6, 20);
			this.rbOutputMethodXAudio2.Name = "rbOutputMethodXAudio2";
			this.rbOutputMethodXAudio2.Size = new System.Drawing.Size(65, 17);
			this.rbOutputMethodXAudio2.TabIndex = 1;
			this.rbOutputMethodXAudio2.TabStop = true;
			this.rbOutputMethodXAudio2.Text = "XAudio2";
			this.rbOutputMethodXAudio2.UseVisualStyleBackColor = true;
			this.rbOutputMethodXAudio2.CheckedChanged += new System.EventHandler(this.OutputMethodRadioButtons_CheckedChanged);
			// 
			// cbMuteFrameAdvance
			// 
			this.cbMuteFrameAdvance.AutoSize = true;
			this.cbMuteFrameAdvance.Location = new System.Drawing.Point(139, 68);
			this.cbMuteFrameAdvance.Name = "cbMuteFrameAdvance";
			this.cbMuteFrameAdvance.Size = new System.Drawing.Size(128, 17);
			this.cbMuteFrameAdvance.TabIndex = 6;
			this.cbMuteFrameAdvance.Text = "Mute Frame Advance";
			this.cbMuteFrameAdvance.UseVisualStyleBackColor = true;
			// 
			// cbEnableMaster
			// 
			this.cbEnableMaster.AutoSize = true;
			this.cbEnableMaster.Location = new System.Drawing.Point(139, 16);
			this.cbEnableMaster.Name = "cbEnableMaster";
			this.cbEnableMaster.Size = new System.Drawing.Size(128, 17);
			this.cbEnableMaster.TabIndex = 4;
			this.cbEnableMaster.Text = "Sound Master Enable";
			this.cbEnableMaster.UseVisualStyleBackColor = true;
			this.cbEnableMaster.CheckedChanged += new System.EventHandler(this.UpdateSoundDialog);
			// 
			// label3
			// 
			this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label3.Location = new System.Drawing.Point(161, 35);
			this.label3.Name = "label3";
			this.label3.Text = "Controls whether cores\neven generate audio.";
			// 
			// SoundConfig
			// 
			this.AcceptButton = this.OK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.Cancel;
			this.ClientSize = new System.Drawing.Size(404, 279);
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
			this.MinimumSize = new System.Drawing.Size(279, 259);
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
		private System.Windows.Forms.RadioButton rbOutputMethodOpenAL;
		private System.Windows.Forms.NumericUpDown nudRWFF;
		private System.Windows.Forms.CheckBox cbEnableRWFF;
		private System.Windows.Forms.TrackBar tbRWFF;
		private BizHawk.WinForms.Controls.LocLabelEx label2;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private System.Windows.Forms.CheckBox cbMuteFrameAdvance;
		private System.Windows.Forms.CheckBox cbEnableMaster;
		private BizHawk.WinForms.Controls.LocLabelEx label3;
	}
}