namespace BizHawk.Client.EmuHawk
{
	partial class EmuHawkOptions
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EmuHawkOptions));
            this.OkBtn = new System.Windows.Forms.Button();
            this.CancelBtn = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.cbEnableGCAdapterSupport = new System.Windows.Forms.CheckBox();
            this.cbMergeLAndRModifierKeys = new System.Windows.Forms.CheckBox();
            this.HandleAlternateKeyboardLayoutsCheckBox = new System.Windows.Forms.CheckBox();
            this.NeverAskSaveCheckbox = new System.Windows.Forms.CheckBox();
            this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.AcceptBackgroundInputCheckbox = new System.Windows.Forms.CheckBox();
            this.AcceptBackgroundInputControllerOnlyCheckBox = new System.Windows.Forms.CheckBox();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.RunInBackgroundCheckbox = new System.Windows.Forms.CheckBox();
            this.EnableContextMenuCheckbox = new System.Windows.Forms.CheckBox();
            this.PauseWhenMenuActivatedCheckbox = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.locLabelEx1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.StartPausedCheckbox = new System.Windows.Forms.CheckBox();
            this.label14 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.StartFullScreenCheckbox = new System.Windows.Forms.CheckBox();
            this.label3 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.SingleInstanceModeCheckbox = new System.Windows.Forms.CheckBox();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.NoMixedKeyPriorityCheckBox = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label10 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label9 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.AutosaveSRAMtextBox = new System.Windows.Forms.NumericUpDown();
            this.AutosaveSRAMradioButton1 = new System.Windows.Forms.RadioButton();
            this.label8 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.AutosaveSRAMradioButton2 = new System.Windows.Forms.RadioButton();
            this.AutosaveSRAMradioButton3 = new System.Windows.Forms.RadioButton();
            this.AutosaveSRAMCheckbox = new System.Windows.Forms.CheckBox();
            this.label6 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.cbSkipWaterboxIntegrityChecks = new System.Windows.Forms.CheckBox();
            this.label5 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.cbMoviesOnDisk = new System.Windows.Forms.CheckBox();
            this.LuaDuringTurboCheckbox = new System.Windows.Forms.CheckBox();
            this.label12 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label13 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.FrameAdvSkipLagCheckbox = new System.Windows.Forms.CheckBox();
            this.BackupSRamCheckbox = new System.Windows.Forms.CheckBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.AutosaveSRAMtextBox)).BeginInit();
            this.SuspendLayout();
            // 
            // OkBtn
            // 
            resources.ApplyResources(this.OkBtn, "OkBtn");
            this.OkBtn.Name = "OkBtn";
            this.OkBtn.UseVisualStyleBackColor = true;
            this.OkBtn.Click += new System.EventHandler(this.OkBtn_Click);
            // 
            // CancelBtn
            // 
            resources.ApplyResources(this.CancelBtn, "CancelBtn");
            this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.UseVisualStyleBackColor = true;
            this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
            // 
            // tabControl1
            // 
            resources.ApplyResources(this.tabControl1, "tabControl1");
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            // 
            // tabPage1
            // 
            resources.ApplyResources(this.tabPage1, "tabPage1");
            this.tabPage1.Controls.Add(this.cbEnableGCAdapterSupport);
            this.tabPage1.Controls.Add(this.cbMergeLAndRModifierKeys);
            this.tabPage1.Controls.Add(this.HandleAlternateKeyboardLayoutsCheckBox);
            this.tabPage1.Controls.Add(this.NeverAskSaveCheckbox);
            this.tabPage1.Controls.Add(this.label2);
            this.tabPage1.Controls.Add(this.AcceptBackgroundInputCheckbox);
            this.tabPage1.Controls.Add(this.AcceptBackgroundInputControllerOnlyCheckBox);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Controls.Add(this.RunInBackgroundCheckbox);
            this.tabPage1.Controls.Add(this.EnableContextMenuCheckbox);
            this.tabPage1.Controls.Add(this.PauseWhenMenuActivatedCheckbox);
            this.tabPage1.Controls.Add(this.groupBox1);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // cbEnableGCAdapterSupport
            // 
            resources.ApplyResources(this.cbEnableGCAdapterSupport, "cbEnableGCAdapterSupport");
            this.cbEnableGCAdapterSupport.Name = "cbEnableGCAdapterSupport";
            this.cbEnableGCAdapterSupport.UseVisualStyleBackColor = true;
            // 
            // cbMergeLAndRModifierKeys
            // 
            resources.ApplyResources(this.cbMergeLAndRModifierKeys, "cbMergeLAndRModifierKeys");
            this.cbMergeLAndRModifierKeys.Name = "cbMergeLAndRModifierKeys";
            this.cbMergeLAndRModifierKeys.UseVisualStyleBackColor = true;
            // 
            // HandleAlternateKeyboardLayoutsCheckBox
            // 
            resources.ApplyResources(this.HandleAlternateKeyboardLayoutsCheckBox, "HandleAlternateKeyboardLayoutsCheckBox");
            this.HandleAlternateKeyboardLayoutsCheckBox.Name = "HandleAlternateKeyboardLayoutsCheckBox";
            this.HandleAlternateKeyboardLayoutsCheckBox.UseVisualStyleBackColor = true;
            // 
            // NeverAskSaveCheckbox
            // 
            resources.ApplyResources(this.NeverAskSaveCheckbox, "NeverAskSaveCheckbox");
            this.NeverAskSaveCheckbox.Name = "NeverAskSaveCheckbox";
            this.NeverAskSaveCheckbox.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // AcceptBackgroundInputCheckbox
            // 
            resources.ApplyResources(this.AcceptBackgroundInputCheckbox, "AcceptBackgroundInputCheckbox");
            this.AcceptBackgroundInputCheckbox.Name = "AcceptBackgroundInputCheckbox";
            this.AcceptBackgroundInputCheckbox.UseVisualStyleBackColor = true;
            this.AcceptBackgroundInputCheckbox.CheckedChanged += new System.EventHandler(this.AcceptBackgroundInputCheckbox_CheckedChanged);
            // 
            // AcceptBackgroundInputControllerOnlyCheckBox
            // 
            resources.ApplyResources(this.AcceptBackgroundInputControllerOnlyCheckBox, "AcceptBackgroundInputControllerOnlyCheckBox");
            this.AcceptBackgroundInputControllerOnlyCheckBox.Name = "AcceptBackgroundInputControllerOnlyCheckBox";
            this.AcceptBackgroundInputControllerOnlyCheckBox.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // RunInBackgroundCheckbox
            // 
            resources.ApplyResources(this.RunInBackgroundCheckbox, "RunInBackgroundCheckbox");
            this.RunInBackgroundCheckbox.Name = "RunInBackgroundCheckbox";
            this.RunInBackgroundCheckbox.UseVisualStyleBackColor = true;
            // 
            // EnableContextMenuCheckbox
            // 
            resources.ApplyResources(this.EnableContextMenuCheckbox, "EnableContextMenuCheckbox");
            this.EnableContextMenuCheckbox.Name = "EnableContextMenuCheckbox";
            this.EnableContextMenuCheckbox.UseVisualStyleBackColor = true;
            // 
            // PauseWhenMenuActivatedCheckbox
            // 
            resources.ApplyResources(this.PauseWhenMenuActivatedCheckbox, "PauseWhenMenuActivatedCheckbox");
            this.PauseWhenMenuActivatedCheckbox.Name = "PauseWhenMenuActivatedCheckbox";
            this.PauseWhenMenuActivatedCheckbox.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.locLabelEx1);
            this.groupBox1.Controls.Add(this.StartPausedCheckbox);
            this.groupBox1.Controls.Add(this.label14);
            this.groupBox1.Controls.Add(this.StartFullScreenCheckbox);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.SingleInstanceModeCheckbox);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // locLabelEx1
            // 
            resources.ApplyResources(this.locLabelEx1, "locLabelEx1");
            this.locLabelEx1.Name = "locLabelEx1";
            // 
            // StartPausedCheckbox
            // 
            resources.ApplyResources(this.StartPausedCheckbox, "StartPausedCheckbox");
            this.StartPausedCheckbox.Name = "StartPausedCheckbox";
            this.StartPausedCheckbox.UseVisualStyleBackColor = true;
            // 
            // label14
            // 
            resources.ApplyResources(this.label14, "label14");
            this.label14.Name = "label14";
            // 
            // StartFullScreenCheckbox
            // 
            resources.ApplyResources(this.StartFullScreenCheckbox, "StartFullScreenCheckbox");
            this.StartFullScreenCheckbox.Name = "StartFullScreenCheckbox";
            this.StartFullScreenCheckbox.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // SingleInstanceModeCheckbox
            // 
            resources.ApplyResources(this.SingleInstanceModeCheckbox, "SingleInstanceModeCheckbox");
            this.SingleInstanceModeCheckbox.Name = "SingleInstanceModeCheckbox";
            this.SingleInstanceModeCheckbox.UseVisualStyleBackColor = true;
            // 
            // tabPage3
            // 
            resources.ApplyResources(this.tabPage3, "tabPage3");
            this.tabPage3.Controls.Add(this.NoMixedKeyPriorityCheckBox);
            this.tabPage3.Controls.Add(this.groupBox2);
            this.tabPage3.Controls.Add(this.AutosaveSRAMCheckbox);
            this.tabPage3.Controls.Add(this.label6);
            this.tabPage3.Controls.Add(this.cbSkipWaterboxIntegrityChecks);
            this.tabPage3.Controls.Add(this.label5);
            this.tabPage3.Controls.Add(this.cbMoviesOnDisk);
            this.tabPage3.Controls.Add(this.LuaDuringTurboCheckbox);
            this.tabPage3.Controls.Add(this.label12);
            this.tabPage3.Controls.Add(this.label13);
            this.tabPage3.Controls.Add(this.FrameAdvSkipLagCheckbox);
            this.tabPage3.Controls.Add(this.BackupSRamCheckbox);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // NoMixedKeyPriorityCheckBox
            // 
            resources.ApplyResources(this.NoMixedKeyPriorityCheckBox, "NoMixedKeyPriorityCheckBox");
            this.NoMixedKeyPriorityCheckBox.Name = "NoMixedKeyPriorityCheckBox";
            this.NoMixedKeyPriorityCheckBox.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Controls.Add(this.label10);
            this.groupBox2.Controls.Add(this.label9);
            this.groupBox2.Controls.Add(this.AutosaveSRAMtextBox);
            this.groupBox2.Controls.Add(this.AutosaveSRAMradioButton1);
            this.groupBox2.Controls.Add(this.label8);
            this.groupBox2.Controls.Add(this.AutosaveSRAMradioButton2);
            this.groupBox2.Controls.Add(this.AutosaveSRAMradioButton3);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // label10
            // 
            resources.ApplyResources(this.label10, "label10");
            this.label10.Name = "label10";
            // 
            // label9
            // 
            resources.ApplyResources(this.label9, "label9");
            this.label9.Name = "label9";
            // 
            // AutosaveSRAMtextBox
            // 
            resources.ApplyResources(this.AutosaveSRAMtextBox, "AutosaveSRAMtextBox");
            this.AutosaveSRAMtextBox.Maximum = new decimal(new int[] {
            86400,
            0,
            0,
            0});
            this.AutosaveSRAMtextBox.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.AutosaveSRAMtextBox.Name = "AutosaveSRAMtextBox";
            this.AutosaveSRAMtextBox.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // AutosaveSRAMradioButton1
            // 
            resources.ApplyResources(this.AutosaveSRAMradioButton1, "AutosaveSRAMradioButton1");
            this.AutosaveSRAMradioButton1.Name = "AutosaveSRAMradioButton1";
            this.AutosaveSRAMradioButton1.TabStop = true;
            this.AutosaveSRAMradioButton1.UseVisualStyleBackColor = true;
            // 
            // label8
            // 
            resources.ApplyResources(this.label8, "label8");
            this.label8.Name = "label8";
            // 
            // AutosaveSRAMradioButton2
            // 
            resources.ApplyResources(this.AutosaveSRAMradioButton2, "AutosaveSRAMradioButton2");
            this.AutosaveSRAMradioButton2.Name = "AutosaveSRAMradioButton2";
            this.AutosaveSRAMradioButton2.TabStop = true;
            this.AutosaveSRAMradioButton2.UseVisualStyleBackColor = true;
            // 
            // AutosaveSRAMradioButton3
            // 
            resources.ApplyResources(this.AutosaveSRAMradioButton3, "AutosaveSRAMradioButton3");
            this.AutosaveSRAMradioButton3.Name = "AutosaveSRAMradioButton3";
            this.AutosaveSRAMradioButton3.TabStop = true;
            this.AutosaveSRAMradioButton3.UseVisualStyleBackColor = true;
            this.AutosaveSRAMradioButton3.CheckedChanged += new System.EventHandler(this.AutosaveSRAMRadioButton3_CheckedChanged);
            // 
            // AutosaveSRAMCheckbox
            // 
            resources.ApplyResources(this.AutosaveSRAMCheckbox, "AutosaveSRAMCheckbox");
            this.AutosaveSRAMCheckbox.Name = "AutosaveSRAMCheckbox";
            this.AutosaveSRAMCheckbox.UseVisualStyleBackColor = true;
            this.AutosaveSRAMCheckbox.CheckedChanged += new System.EventHandler(this.AutosaveSRAMCheckbox_CheckedChanged);
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // cbSkipWaterboxIntegrityChecks
            // 
            resources.ApplyResources(this.cbSkipWaterboxIntegrityChecks, "cbSkipWaterboxIntegrityChecks");
            this.cbSkipWaterboxIntegrityChecks.Name = "cbSkipWaterboxIntegrityChecks";
            this.cbSkipWaterboxIntegrityChecks.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // cbMoviesOnDisk
            // 
            resources.ApplyResources(this.cbMoviesOnDisk, "cbMoviesOnDisk");
            this.cbMoviesOnDisk.Name = "cbMoviesOnDisk";
            this.cbMoviesOnDisk.UseVisualStyleBackColor = true;
            // 
            // LuaDuringTurboCheckbox
            // 
            resources.ApplyResources(this.LuaDuringTurboCheckbox, "LuaDuringTurboCheckbox");
            this.LuaDuringTurboCheckbox.Name = "LuaDuringTurboCheckbox";
            this.LuaDuringTurboCheckbox.UseVisualStyleBackColor = true;
            // 
            // label12
            // 
            resources.ApplyResources(this.label12, "label12");
            this.label12.Name = "label12";
            // 
            // label13
            // 
            resources.ApplyResources(this.label13, "label13");
            this.label13.Name = "label13";
            // 
            // FrameAdvSkipLagCheckbox
            // 
            resources.ApplyResources(this.FrameAdvSkipLagCheckbox, "FrameAdvSkipLagCheckbox");
            this.FrameAdvSkipLagCheckbox.Name = "FrameAdvSkipLagCheckbox";
            this.FrameAdvSkipLagCheckbox.UseVisualStyleBackColor = true;
            // 
            // BackupSRamCheckbox
            // 
            resources.ApplyResources(this.BackupSRamCheckbox, "BackupSRamCheckbox");
            this.BackupSRamCheckbox.Name = "BackupSRamCheckbox";
            this.BackupSRamCheckbox.UseVisualStyleBackColor = true;
            // 
            // EmuHawkOptions
            // 
            this.AcceptButton = this.OkBtn;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelBtn;
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.CancelBtn);
            this.Controls.Add(this.OkBtn);
            this.Name = "EmuHawkOptions";
            this.ShowIcon = false;
            this.Load += new System.EventHandler(this.GuiOptions_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.AutosaveSRAMtextBox)).EndInit();
            this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button OkBtn;
		private System.Windows.Forms.Button CancelBtn;
		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.TabPage tabPage3;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.CheckBox BackupSRamCheckbox;
		private System.Windows.Forms.CheckBox FrameAdvSkipLagCheckbox;
		private BizHawk.WinForms.Controls.LocLabelEx label12;
		private BizHawk.WinForms.Controls.LocLabelEx label13;
		private System.Windows.Forms.CheckBox LuaDuringTurboCheckbox;
		private BizHawk.WinForms.Controls.LocLabelEx label5;
		private System.Windows.Forms.CheckBox cbMoviesOnDisk;
		private BizHawk.WinForms.Controls.LocLabelEx label6;
		private System.Windows.Forms.CheckBox cbSkipWaterboxIntegrityChecks;
		private System.Windows.Forms.CheckBox AutosaveSRAMCheckbox;
		private BizHawk.WinForms.Controls.LocLabelEx label8;
		private System.Windows.Forms.RadioButton AutosaveSRAMradioButton3;
		private System.Windows.Forms.RadioButton AutosaveSRAMradioButton2;
		private System.Windows.Forms.RadioButton AutosaveSRAMradioButton1;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.NumericUpDown AutosaveSRAMtextBox;
		private BizHawk.WinForms.Controls.LocLabelEx label10;
		private BizHawk.WinForms.Controls.LocLabelEx label9;
		private System.Windows.Forms.CheckBox HandleAlternateKeyboardLayoutsCheckBox;
		private System.Windows.Forms.CheckBox NeverAskSaveCheckbox;
		private BizHawk.WinForms.Controls.LocLabelEx label2;
		private System.Windows.Forms.CheckBox AcceptBackgroundInputCheckbox;
		private System.Windows.Forms.CheckBox AcceptBackgroundInputControllerOnlyCheckBox;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private System.Windows.Forms.CheckBox RunInBackgroundCheckbox;
		private System.Windows.Forms.CheckBox EnableContextMenuCheckbox;
		private System.Windows.Forms.CheckBox PauseWhenMenuActivatedCheckbox;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.CheckBox StartPausedCheckbox;
		private BizHawk.WinForms.Controls.LocLabelEx label14;
		private System.Windows.Forms.CheckBox StartFullScreenCheckbox;
		private BizHawk.WinForms.Controls.LocLabelEx label3;
		private System.Windows.Forms.CheckBox SingleInstanceModeCheckbox;
		private System.Windows.Forms.CheckBox NoMixedKeyPriorityCheckBox;
		private WinForms.Controls.LocLabelEx locLabelEx1;
		private System.Windows.Forms.CheckBox cbMergeLAndRModifierKeys;
		private System.Windows.Forms.CheckBox cbEnableGCAdapterSupport;
	}
}
