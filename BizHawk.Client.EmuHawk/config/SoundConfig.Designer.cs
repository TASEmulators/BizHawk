namespace BizHawk.Client.EmuHawk
{
	partial class SoundConfig
	{
		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.btnDialogCancel = new BizHawk.WinForms.Controls.SzButtonEx();
			this.btnDialogOK = new BizHawk.WinForms.Controls.SzButtonEx();
			this.cbFullSpeedEnable = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.grpVolume = new BizHawk.WinForms.Controls.LocSzGroupBoxEx();
			this.flpGrpVolume = new BizHawk.WinForms.Controls.LocSingleRowFLP();
			this.flpFullSpeed = new BizHawk.WinForms.Controls.SingleColumnFLP();
			this.lblFullSpeedVolume = new BizHawk.WinForms.Controls.LabelEx();
			this.tbFullSpeedVolume = new System.Windows.Forms.TrackBar();
			this.nudFullSpeedVolume = new BizHawk.WinForms.Controls.SzNUDEx();
			this.flpRWFF = new BizHawk.WinForms.Controls.SingleColumnFLP();
			this.cbRewindFFWEnable = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.lblRewindFFWVolume = new BizHawk.WinForms.Controls.LabelEx();
			this.tbRewindFFWVolume = new System.Windows.Forms.TrackBar();
			this.nudRewindFFWVolume = new BizHawk.WinForms.Controls.SzNUDEx();
			this.listDevices = new System.Windows.Forms.ListBox();
			this.lblDevices = new BizHawk.WinForms.Controls.LocLabelEx();
			this.lblBufferSizeDesc = new BizHawk.WinForms.Controls.LabelEx();
			this.nudBufferSize = new BizHawk.WinForms.Controls.SzNUDEx();
			this.lblBufferSizeUnits = new BizHawk.WinForms.Controls.LabelEx();
			this.flpDialogButtons = new BizHawk.WinForms.Controls.LocSzSingleRowFLP();
			this.flpBufferSize = new BizHawk.WinForms.Controls.SingleRowFLP();
			this.flpFlowRHS = new BizHawk.WinForms.Controls.LocSingleColumnFLP();
			this.flpFlowRHSTop = new BizHawk.WinForms.Controls.SzColumnsToRightFLP();
			this.cbMasterEnable = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.lblMasterEnable = new BizHawk.WinForms.Controls.LocSzLabelEx();
			this.cbMuteFrameAdvance = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.grpSoundMethod = new BizHawk.WinForms.Controls.SzGroupBoxEx();
			this.flpGrpSoundMethod = new BizHawk.WinForms.Controls.LocSingleColumnFLP();
			this.rbSoundMethodDirectSound = new BizHawk.WinForms.Controls.RadioButtonEx();
			this.rbSoundMethodXAudio2 = new BizHawk.WinForms.Controls.RadioButtonEx();
			this.rbSoundMethodOpenAL = new BizHawk.WinForms.Controls.RadioButtonEx();
			this.grpVolume.SuspendLayout();
			this.flpGrpVolume.SuspendLayout();
			this.flpFullSpeed.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.tbFullSpeedVolume)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.nudFullSpeedVolume)).BeginInit();
			this.flpRWFF.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.tbRewindFFWVolume)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.nudRewindFFWVolume)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.nudBufferSize)).BeginInit();
			this.flpDialogButtons.SuspendLayout();
			this.flpBufferSize.SuspendLayout();
			this.flpFlowRHS.SuspendLayout();
			this.flpFlowRHSTop.SuspendLayout();
			this.grpSoundMethod.SuspendLayout();
			this.flpGrpSoundMethod.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnDialogCancel
			// 
			this.btnDialogCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnDialogCancel.Name = "btnDialogCancel";
			this.btnDialogCancel.Size = new System.Drawing.Size(75, 23);
			this.btnDialogCancel.Text = "&Cancel";
			this.btnDialogCancel.Click += new System.EventHandler(this.btnDialogCancel_Click);
			// 
			// btnDialogOK
			// 
			this.btnDialogOK.Name = "btnDialogOK";
			this.btnDialogOK.Size = new System.Drawing.Size(75, 23);
			this.btnDialogOK.Text = "&OK";
			this.btnDialogOK.Click += new System.EventHandler(this.btnDialogOK_Click);
			// 
			// cbFullSpeedEnable
			// 
			this.cbFullSpeedEnable.Name = "cbFullSpeedEnable";
			this.cbFullSpeedEnable.Text = "Ena.";
			this.cbFullSpeedEnable.CheckedChanged += new System.EventHandler(this.cbMasterOrFullSpeed_CheckedChanged);
			// 
			// grpVolume
			// 
			this.grpVolume.Controls.Add(this.flpGrpVolume);
			this.grpVolume.Location = new System.Drawing.Point(4, 4);
			this.grpVolume.Name = "grpVolume";
			this.grpVolume.Size = new System.Drawing.Size(111, 246);
			this.grpVolume.Text = "Volume";
			// 
			// flpGrpVolume
			// 
			this.flpGrpVolume.Controls.Add(this.flpFullSpeed);
			this.flpGrpVolume.Controls.Add(this.flpRWFF);
			this.flpGrpVolume.Location = new System.Drawing.Point(0, 12);
			this.flpGrpVolume.Name = "flpGrpVolume";
			// 
			// flpFullSpeed
			// 
			this.flpFullSpeed.Controls.Add(this.cbFullSpeedEnable);
			this.flpFullSpeed.Controls.Add(this.lblFullSpeedVolume);
			this.flpFullSpeed.Controls.Add(this.tbFullSpeedVolume);
			this.flpFullSpeed.Controls.Add(this.nudFullSpeedVolume);
			this.flpFullSpeed.Name = "flpFullSpeed";
			// 
			// lblFullSpeedVolume
			// 
			this.lblFullSpeedVolume.Name = "lblFullSpeedVolume";
			this.lblFullSpeedVolume.Text = "Normal";
			// 
			// tbFullSpeedVolume
			// 
			this.tbFullSpeedVolume.LargeChange = 10;
			this.tbFullSpeedVolume.Location = new System.Drawing.Point(3, 39);
			this.tbFullSpeedVolume.Maximum = 100;
			this.tbFullSpeedVolume.Name = "tbFullSpeedVolume";
			this.tbFullSpeedVolume.Orientation = System.Windows.Forms.Orientation.Vertical;
			this.tbFullSpeedVolume.Size = new System.Drawing.Size(45, 164);
			this.tbFullSpeedVolume.TabIndex = 2;
			this.tbFullSpeedVolume.TickFrequency = 10;
			this.tbFullSpeedVolume.Scroll += new System.EventHandler(this.tbFullSpeedVolume_Scroll);
			// 
			// nudFullSpeedVolume
			// 
			this.nudFullSpeedVolume.Name = "nudFullSpeedVolume";
			this.nudFullSpeedVolume.Size = new System.Drawing.Size(45, 20);
			this.nudFullSpeedVolume.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
			this.nudFullSpeedVolume.ValueChanged += new System.EventHandler(this.nudFullSpeedVolume_ValueChanged);
			// 
			// flpRWFF
			// 
			this.flpRWFF.Controls.Add(this.cbRewindFFWEnable);
			this.flpRWFF.Controls.Add(this.lblRewindFFWVolume);
			this.flpRWFF.Controls.Add(this.tbRewindFFWVolume);
			this.flpRWFF.Controls.Add(this.nudRewindFFWVolume);
			this.flpRWFF.Name = "flpRWFF";
			// 
			// cbRewindFFWEnable
			// 
			this.cbRewindFFWEnable.Name = "cbRewindFFWEnable";
			this.cbRewindFFWEnable.Text = "Ena.";
			// 
			// lblRewindFFWVolume
			// 
			this.lblRewindFFWVolume.Name = "lblRewindFFWVolume";
			this.lblRewindFFWVolume.Text = "RW && FF";
			// 
			// tbRewindFFWVolume
			// 
			this.tbRewindFFWVolume.LargeChange = 10;
			this.tbRewindFFWVolume.Location = new System.Drawing.Point(3, 39);
			this.tbRewindFFWVolume.Maximum = 100;
			this.tbRewindFFWVolume.Name = "tbRewindFFWVolume";
			this.tbRewindFFWVolume.Orientation = System.Windows.Forms.Orientation.Vertical;
			this.tbRewindFFWVolume.Size = new System.Drawing.Size(45, 164);
			this.tbRewindFFWVolume.TabIndex = 6;
			this.tbRewindFFWVolume.TickFrequency = 10;
			this.tbRewindFFWVolume.Scroll += new System.EventHandler(this.tbRewindFFWVolume_Scroll);
			// 
			// nudRewindFFWVolume
			// 
			this.nudRewindFFWVolume.Name = "nudRewindFFWVolume";
			this.nudRewindFFWVolume.Size = new System.Drawing.Size(45, 20);
			this.nudRewindFFWVolume.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
			// 
			// listDevices
			// 
			this.listDevices.FormattingEnabled = true;
			this.listDevices.Location = new System.Drawing.Point(3, 110);
			this.listDevices.Name = "listDevices";
			this.listDevices.Size = new System.Drawing.Size(254, 95);
			this.listDevices.TabIndex = 8;
			// 
			// lblDevices
			// 
			this.lblDevices.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.lblDevices.Location = new System.Drawing.Point(3, 94);
			this.lblDevices.Name = "lblDevices";
			this.lblDevices.Text = "Sound Device:";
			// 
			// lblBufferSizeDesc
			// 
			this.lblBufferSizeDesc.Name = "lblBufferSizeDesc";
			this.lblBufferSizeDesc.Text = "Buffer Size:";
			// 
			// nudBufferSize
			// 
			this.nudBufferSize.Maximum = new decimal(new int[] {
            250,
            0,
            0,
            0});
			this.nudBufferSize.Minimum = new decimal(new int[] {
            30,
            0,
            0,
            0});
			this.nudBufferSize.Name = "nudBufferSize";
			this.nudBufferSize.Size = new System.Drawing.Size(59, 20);
			this.nudBufferSize.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
			// 
			// lblBufferSizeUnits
			// 
			this.lblBufferSizeUnits.Name = "lblBufferSizeUnits";
			this.lblBufferSizeUnits.Text = "milliseconds";
			// 
			// flpDialogButtons
			// 
			this.flpDialogButtons.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.flpDialogButtons.Controls.Add(this.btnDialogOK);
			this.flpDialogButtons.Controls.Add(this.btnDialogCancel);
			this.flpDialogButtons.Location = new System.Drawing.Point(216, 241);
			this.flpDialogButtons.MinimumSize = new System.Drawing.Size(24, 24);
			this.flpDialogButtons.Name = "flpDialogButtons";
			this.flpDialogButtons.Size = new System.Drawing.Size(162, 29);
			// 
			// flpBufferSize
			// 
			this.flpBufferSize.Controls.Add(this.lblBufferSizeDesc);
			this.flpBufferSize.Controls.Add(this.nudBufferSize);
			this.flpBufferSize.Controls.Add(this.lblBufferSizeUnits);
			this.flpBufferSize.Name = "flpBufferSize";
			// 
			// flpFlowRHS
			// 
			this.flpFlowRHS.Controls.Add(this.flpFlowRHSTop);
			this.flpFlowRHS.Controls.Add(this.lblDevices);
			this.flpFlowRHS.Controls.Add(this.listDevices);
			this.flpFlowRHS.Controls.Add(this.flpBufferSize);
			this.flpFlowRHS.Location = new System.Drawing.Point(118, 4);
			this.flpFlowRHS.Name = "flpFlowRHS";
			// 
			// flpFlowRHSTop
			// 
			this.flpFlowRHSTop.Controls.Add(this.cbMasterEnable);
			this.flpFlowRHSTop.Controls.Add(this.lblMasterEnable);
			this.flpFlowRHSTop.Controls.Add(this.cbMuteFrameAdvance);
			this.flpFlowRHSTop.Controls.Add(this.grpSoundMethod);
			this.flpFlowRHSTop.MinimumSize = new System.Drawing.Size(24, 24);
			this.flpFlowRHSTop.Name = "flpFlowRHSTop";
			this.flpFlowRHSTop.Size = new System.Drawing.Size(245, 94);
			// 
			// cbMasterEnable
			// 
			this.cbMasterEnable.Name = "cbMasterEnable";
			this.cbMasterEnable.Text = "Sound Master Enable";
			this.cbMasterEnable.CheckedChanged += new System.EventHandler(this.cbMasterOrFullSpeed_CheckedChanged);
			// 
			// lblMasterEnable
			// 
			this.lblMasterEnable.Location = new System.Drawing.Point(3, 23);
			this.lblMasterEnable.Name = "lblMasterEnable";
			this.lblMasterEnable.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
			this.lblMasterEnable.Size = new System.Drawing.Size(131, 26);
			this.lblMasterEnable.Text = "Controls whether cores even generate audio.";
			// 
			// cbMuteFrameAdvance
			// 
			this.cbMuteFrameAdvance.Name = "cbMuteFrameAdvance";
			this.cbMuteFrameAdvance.Text = "Mute Frame Advance";
			// 
			// grpSoundMethod
			// 
			this.grpSoundMethod.Controls.Add(this.flpGrpSoundMethod);
			this.grpSoundMethod.Name = "grpSoundMethod";
			this.grpSoundMethod.Size = new System.Drawing.Size(100, 90);
			this.grpSoundMethod.Text = "Output Method";
			// 
			// flpGrpSoundMethod
			// 
			this.flpGrpSoundMethod.Controls.Add(this.rbSoundMethodDirectSound);
			this.flpGrpSoundMethod.Controls.Add(this.rbSoundMethodXAudio2);
			this.flpGrpSoundMethod.Controls.Add(this.rbSoundMethodOpenAL);
			this.flpGrpSoundMethod.Location = new System.Drawing.Point(6, 13);
			this.flpGrpSoundMethod.Name = "flpGrpSoundMethod";
			// 
			// rbSoundMethodDirectSound
			// 
			this.rbSoundMethodDirectSound.Name = "rbSoundMethodDirectSound";
			this.rbSoundMethodDirectSound.Text = "DirectSound";
			this.rbSoundMethodDirectSound.CheckedChanged += new System.EventHandler(this.rbSoundMethodAllRadios_CheckedChanged);
			// 
			// rbSoundMethodXAudio2
			// 
			this.rbSoundMethodXAudio2.Name = "rbSoundMethodXAudio2";
			this.rbSoundMethodXAudio2.Text = "XAudio2";
			this.rbSoundMethodXAudio2.CheckedChanged += new System.EventHandler(this.rbSoundMethodAllRadios_CheckedChanged);
			// 
			// rbSoundMethodOpenAL
			// 
			this.rbSoundMethodOpenAL.Name = "rbSoundMethodOpenAL";
			this.rbSoundMethodOpenAL.Text = "OpenAL";
			this.rbSoundMethodOpenAL.CheckedChanged += new System.EventHandler(this.rbSoundMethodAllRadios_CheckedChanged);
			// 
			// SoundConfig
			// 
			this.AcceptButton = this.btnDialogOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnDialogCancel;
			this.ClientSize = new System.Drawing.Size(382, 274);
			this.Controls.Add(this.grpVolume);
			this.Controls.Add(this.flpDialogButtons);
			this.Controls.Add(this.flpFlowRHS);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MinimumSize = new System.Drawing.Size(398, 313);
			this.Name = "SoundConfig";
			this.ShowIcon = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Sound Configuration";
			this.Load += new System.EventHandler(this.SoundConfig_Load);
			this.grpVolume.ResumeLayout(false);
			this.grpVolume.PerformLayout();
			this.flpGrpVolume.ResumeLayout(false);
			this.flpGrpVolume.PerformLayout();
			this.flpFullSpeed.ResumeLayout(false);
			this.flpFullSpeed.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.tbFullSpeedVolume)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.nudFullSpeedVolume)).EndInit();
			this.flpRWFF.ResumeLayout(false);
			this.flpRWFF.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.tbRewindFFWVolume)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.nudRewindFFWVolume)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.nudBufferSize)).EndInit();
			this.flpDialogButtons.ResumeLayout(false);
			this.flpBufferSize.ResumeLayout(false);
			this.flpBufferSize.PerformLayout();
			this.flpFlowRHS.ResumeLayout(false);
			this.flpFlowRHS.PerformLayout();
			this.flpFlowRHSTop.ResumeLayout(false);
			this.flpFlowRHSTop.PerformLayout();
			this.grpSoundMethod.ResumeLayout(false);
			this.grpSoundMethod.PerformLayout();
			this.flpGrpSoundMethod.ResumeLayout(false);
			this.flpGrpSoundMethod.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private BizHawk.WinForms.Controls.SingleColumnFLP flpFullSpeed;
		private BizHawk.WinForms.Controls.SingleRowFLP flpBufferSize;
		private BizHawk.WinForms.Controls.LocSingleColumnFLP flpFlowRHS;
		private BizHawk.WinForms.Controls.LocSzSingleRowFLP flpDialogButtons;
		private BizHawk.WinForms.Controls.SingleColumnFLP flpRWFF;
		private BizHawk.WinForms.Controls.SzButtonEx btnDialogCancel;
		private BizHawk.WinForms.Controls.SzButtonEx btnDialogOK;
		private BizHawk.WinForms.Controls.CheckBoxEx cbFullSpeedEnable;
		private BizHawk.WinForms.Controls.LocSzGroupBoxEx grpVolume;
		private BizHawk.WinForms.Controls.SzNUDEx nudFullSpeedVolume;
		private System.Windows.Forms.TrackBar tbFullSpeedVolume;
		private System.Windows.Forms.ListBox listDevices;
		private BizHawk.WinForms.Controls.LocLabelEx lblDevices;
		private BizHawk.WinForms.Controls.LabelEx lblBufferSizeDesc;
		private BizHawk.WinForms.Controls.SzNUDEx nudBufferSize;
		private BizHawk.WinForms.Controls.LabelEx lblBufferSizeUnits;
		private BizHawk.WinForms.Controls.SzNUDEx nudRewindFFWVolume;
		private BizHawk.WinForms.Controls.CheckBoxEx cbRewindFFWEnable;
		private System.Windows.Forms.TrackBar tbRewindFFWVolume;
		private BizHawk.WinForms.Controls.LabelEx lblRewindFFWVolume;
		private BizHawk.WinForms.Controls.LabelEx lblFullSpeedVolume;
		private BizHawk.WinForms.Controls.LocSingleRowFLP flpGrpVolume;
		private BizHawk.WinForms.Controls.SzColumnsToRightFLP flpFlowRHSTop;
		private BizHawk.WinForms.Controls.CheckBoxEx cbMasterEnable;
		private BizHawk.WinForms.Controls.LocSzLabelEx lblMasterEnable;
		private BizHawk.WinForms.Controls.CheckBoxEx cbMuteFrameAdvance;
		private BizHawk.WinForms.Controls.SzGroupBoxEx grpSoundMethod;
		private BizHawk.WinForms.Controls.LocSingleColumnFLP flpGrpSoundMethod;
		private BizHawk.WinForms.Controls.RadioButtonEx rbSoundMethodDirectSound;
		private BizHawk.WinForms.Controls.RadioButtonEx rbSoundMethodXAudio2;
		private BizHawk.WinForms.Controls.RadioButtonEx rbSoundMethodOpenAL;
	}
}
