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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SoundConfig));
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
            resources.ApplyResources(this.Cancel, "Cancel");
            this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Cancel.Name = "Cancel";
            this.Cancel.UseVisualStyleBackColor = true;
            this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
            // 
            // OK
            // 
            resources.ApplyResources(this.OK, "OK");
            this.OK.Name = "OK";
            this.OK.UseVisualStyleBackColor = true;
            this.OK.Click += new System.EventHandler(this.Ok_Click);
            // 
            // cbEnableNormal
            // 
            resources.ApplyResources(this.cbEnableNormal, "cbEnableNormal");
            this.cbEnableNormal.Name = "cbEnableNormal";
            this.cbEnableNormal.UseVisualStyleBackColor = true;
            this.cbEnableNormal.CheckedChanged += new System.EventHandler(this.UpdateSoundDialog);
            // 
            // grpSoundVol
            // 
            resources.ApplyResources(this.grpSoundVol, "grpSoundVol");
            this.grpSoundVol.Controls.Add(this.nudRWFF);
            this.grpSoundVol.Controls.Add(this.cbEnableRWFF);
            this.grpSoundVol.Controls.Add(this.tbRWFF);
            this.grpSoundVol.Controls.Add(this.label2);
            this.grpSoundVol.Controls.Add(this.label1);
            this.grpSoundVol.Controls.Add(this.tbNormal);
            this.grpSoundVol.Controls.Add(this.nudNormal);
            this.grpSoundVol.Controls.Add(this.cbEnableNormal);
            this.grpSoundVol.Name = "grpSoundVol";
            this.grpSoundVol.TabStop = false;
            // 
            // nudRWFF
            // 
            resources.ApplyResources(this.nudRWFF, "nudRWFF");
            this.nudRWFF.Name = "nudRWFF";
            this.nudRWFF.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.nudRWFF.ValueChanged += new System.EventHandler(this.nudRWFF_ValueChanged);
            // 
            // cbEnableRWFF
            // 
            resources.ApplyResources(this.cbEnableRWFF, "cbEnableRWFF");
            this.cbEnableRWFF.Name = "cbEnableRWFF";
            this.cbEnableRWFF.UseVisualStyleBackColor = true;
            // 
            // tbRWFF
            // 
            resources.ApplyResources(this.tbRWFF, "tbRWFF");
            this.tbRWFF.LargeChange = 10;
            this.tbRWFF.Maximum = 100;
            this.tbRWFF.Name = "tbRWFF";
            this.tbRWFF.TickFrequency = 10;
            this.tbRWFF.Scroll += new System.EventHandler(this.TbRwff_Scroll);
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // tbNormal
            // 
            resources.ApplyResources(this.tbNormal, "tbNormal");
            this.tbNormal.LargeChange = 10;
            this.tbNormal.Maximum = 100;
            this.tbNormal.Name = "tbNormal";
            this.tbNormal.TickFrequency = 10;
            this.tbNormal.Scroll += new System.EventHandler(this.TrackBar1_Scroll);
            // 
            // nudNormal
            // 
            resources.ApplyResources(this.nudNormal, "nudNormal");
            this.nudNormal.Name = "nudNormal";
            this.nudNormal.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.nudNormal.ValueChanged += new System.EventHandler(this.SoundVolNumeric_ValueChanged);
            // 
            // listBoxSoundDevices
            // 
            resources.ApplyResources(this.listBoxSoundDevices, "listBoxSoundDevices");
            this.listBoxSoundDevices.FormattingEnabled = true;
            this.listBoxSoundDevices.Name = "listBoxSoundDevices";
            // 
            // SoundDeviceLabel
            // 
            resources.ApplyResources(this.SoundDeviceLabel, "SoundDeviceLabel");
            this.SoundDeviceLabel.Name = "SoundDeviceLabel";
            // 
            // BufferSizeLabel
            // 
            resources.ApplyResources(this.BufferSizeLabel, "BufferSizeLabel");
            this.BufferSizeLabel.Name = "BufferSizeLabel";
            // 
            // BufferSizeNumeric
            // 
            resources.ApplyResources(this.BufferSizeNumeric, "BufferSizeNumeric");
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
            this.BufferSizeNumeric.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            // 
            // BufferSizeUnitsLabel
            // 
            resources.ApplyResources(this.BufferSizeUnitsLabel, "BufferSizeUnitsLabel");
            this.BufferSizeUnitsLabel.Name = "BufferSizeUnitsLabel";
            // 
            // grpOutputMethod
            // 
            resources.ApplyResources(this.grpOutputMethod, "grpOutputMethod");
            this.grpOutputMethod.Controls.Add(this.rbOutputMethodOpenAL);
            this.grpOutputMethod.Controls.Add(this.rbOutputMethodXAudio2);
            this.grpOutputMethod.Name = "grpOutputMethod";
            this.grpOutputMethod.TabStop = false;
            // 
            // rbOutputMethodOpenAL
            // 
            resources.ApplyResources(this.rbOutputMethodOpenAL, "rbOutputMethodOpenAL");
            this.rbOutputMethodOpenAL.Name = "rbOutputMethodOpenAL";
            this.rbOutputMethodOpenAL.TabStop = true;
            this.rbOutputMethodOpenAL.UseVisualStyleBackColor = true;
            this.rbOutputMethodOpenAL.CheckedChanged += new System.EventHandler(this.OutputMethodRadioButtons_CheckedChanged);
            // 
            // rbOutputMethodXAudio2
            // 
            resources.ApplyResources(this.rbOutputMethodXAudio2, "rbOutputMethodXAudio2");
            this.rbOutputMethodXAudio2.Name = "rbOutputMethodXAudio2";
            this.rbOutputMethodXAudio2.TabStop = true;
            this.rbOutputMethodXAudio2.UseVisualStyleBackColor = true;
            this.rbOutputMethodXAudio2.CheckedChanged += new System.EventHandler(this.OutputMethodRadioButtons_CheckedChanged);
            // 
            // cbMuteFrameAdvance
            // 
            resources.ApplyResources(this.cbMuteFrameAdvance, "cbMuteFrameAdvance");
            this.cbMuteFrameAdvance.Name = "cbMuteFrameAdvance";
            this.cbMuteFrameAdvance.UseVisualStyleBackColor = true;
            // 
            // cbEnableMaster
            // 
            resources.ApplyResources(this.cbEnableMaster, "cbEnableMaster");
            this.cbEnableMaster.Name = "cbEnableMaster";
            this.cbEnableMaster.UseVisualStyleBackColor = true;
            this.cbEnableMaster.CheckedChanged += new System.EventHandler(this.UpdateSoundDialog);
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // SoundConfig
            // 
            this.AcceptButton = this.OK;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.Cancel;
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
            this.Name = "SoundConfig";
            this.ShowIcon = false;
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