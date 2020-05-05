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
            this.OkBtn = new System.Windows.Forms.Button();
            this.CancelBtn = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label10 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.AutosaveSRAMtextBox = new System.Windows.Forms.NumericUpDown();
            this.AutosaveSRAMradioButton1 = new System.Windows.Forms.RadioButton();
            this.label8 = new System.Windows.Forms.Label();
            this.AutosaveSRAMradioButton2 = new System.Windows.Forms.RadioButton();
            this.AutosaveSRAMradioButton3 = new System.Windows.Forms.RadioButton();
            this.AutosaveSRAMCheckbox = new System.Windows.Forms.CheckBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label7 = new System.Windows.Forms.Label();
            this.LuaInterfaceRadio = new System.Windows.Forms.RadioButton();
            this.NLuaRadio = new System.Windows.Forms.RadioButton();
            this.label6 = new System.Windows.Forms.Label();
            this.cbSkipWaterboxIntegrityChecks = new System.Windows.Forms.CheckBox();
            this.label5 = new System.Windows.Forms.Label();
            this.cbMoviesOnDisk = new System.Windows.Forms.CheckBox();
            this.LuaDuringTurboCheckbox = new System.Windows.Forms.CheckBox();
            this.label12 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.FrameAdvSkipLagCheckbox = new System.Windows.Forms.CheckBox();
            this.BackupSRamCheckbox = new System.Windows.Forms.CheckBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.rbInputMethodOpenTK = new System.Windows.Forms.RadioButton();
            this.rbInputMethodDirectInput = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.StartPausedCheckbox = new System.Windows.Forms.CheckBox();
            this.label14 = new System.Windows.Forms.Label();
            this.StartFullScreenCheckbox = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.SingleInstanceModeCheckbox = new System.Windows.Forms.CheckBox();
            this.HandleAlternateKeyboardLayoutsCheckBox = new System.Windows.Forms.CheckBox();
            this.NeverAskSaveCheckbox = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.AcceptBackgroundInputCheckbox = new System.Windows.Forms.CheckBox();
            this.AcceptBackgroundInputControllerOnlyCheckBox = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.RunInBackgroundCheckbox = new System.Windows.Forms.CheckBox();
            this.SaveWindowPositionCheckbox = new System.Windows.Forms.CheckBox();
            this.EnableContextMenuCheckbox = new System.Windows.Forms.CheckBox();
            this.PauseWhenMenuActivatedCheckbox = new System.Windows.Forms.CheckBox();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.AutosaveSRAMtextBox)).BeginInit();
            this.panel1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // OkBtn
            // 
            this.OkBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OkBtn.Location = new System.Drawing.Point(280, 398);
            this.OkBtn.Name = "OkBtn";
            this.OkBtn.Size = new System.Drawing.Size(60, 23);
            this.OkBtn.TabIndex = 0;
            this.OkBtn.Text = "&OK";
            this.OkBtn.UseVisualStyleBackColor = true;
            this.OkBtn.Click += new System.EventHandler(this.OkBtn_Click);
            // 
            // CancelBtn
            // 
            this.CancelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBtn.Location = new System.Drawing.Point(346, 398);
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.Size = new System.Drawing.Size(60, 23);
            this.CancelBtn.TabIndex = 1;
            this.CancelBtn.Text = "&Cancel";
            this.CancelBtn.UseVisualStyleBackColor = true;
            this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Location = new System.Drawing.Point(12, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(394, 375);
            this.tabControl1.TabIndex = 2;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.HandleAlternateKeyboardLayoutsCheckBox);
            this.tabPage1.Controls.Add(this.NeverAskSaveCheckbox);
            this.tabPage1.Controls.Add(this.label2);
            this.tabPage1.Controls.Add(this.AcceptBackgroundInputCheckbox);
            this.tabPage1.Controls.Add(this.AcceptBackgroundInputControllerOnlyCheckBox);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Controls.Add(this.RunInBackgroundCheckbox);
            this.tabPage1.Controls.Add(this.SaveWindowPositionCheckbox);
            this.tabPage1.Controls.Add(this.EnableContextMenuCheckbox);
            this.tabPage1.Controls.Add(this.PauseWhenMenuActivatedCheckbox);
            this.tabPage1.Controls.Add(this.groupBox3);
            this.tabPage1.Controls.Add(this.groupBox1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(386, 349);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "General";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.groupBox2);
            this.tabPage3.Controls.Add(this.AutosaveSRAMCheckbox);
            this.tabPage3.Controls.Add(this.panel1);
            this.tabPage3.Controls.Add(this.label6);
            this.tabPage3.Controls.Add(this.cbSkipWaterboxIntegrityChecks);
            this.tabPage3.Controls.Add(this.label5);
            this.tabPage3.Controls.Add(this.cbMoviesOnDisk);
            this.tabPage3.Controls.Add(this.LuaDuringTurboCheckbox);
            this.tabPage3.Controls.Add(this.label12);
            this.tabPage3.Controls.Add(this.label13);
            this.tabPage3.Controls.Add(this.FrameAdvSkipLagCheckbox);
            this.tabPage3.Controls.Add(this.BackupSRamCheckbox);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(386, 349);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Advanced";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label10);
            this.groupBox2.Controls.Add(this.label9);
            this.groupBox2.Controls.Add(this.AutosaveSRAMtextBox);
            this.groupBox2.Controls.Add(this.AutosaveSRAMradioButton1);
            this.groupBox2.Controls.Add(this.label8);
            this.groupBox2.Controls.Add(this.AutosaveSRAMradioButton2);
            this.groupBox2.Controls.Add(this.AutosaveSRAMradioButton3);
            this.groupBox2.Location = new System.Drawing.Point(27, 32);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(0);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(265, 60);
            this.groupBox2.TabIndex = 5;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "AutoSaveRAM";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(9, 34);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(33, 13);
            this.label10.TabIndex = 1;
            this.label10.Text = "every";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(6, 16);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(225, 13);
            this.label9.TabIndex = 0;
            this.label9.Text = "Save SaveRAM to .AutoSaveRAM.SaveRAM";
            // 
            // AutosaveSRAMtextBox
            // 
            this.AutosaveSRAMtextBox.Location = new System.Drawing.Point(151, 33);
            this.AutosaveSRAMtextBox.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.AutosaveSRAMtextBox.Name = "AutosaveSRAMtextBox";
            this.AutosaveSRAMtextBox.Size = new System.Drawing.Size(50, 20);
            this.AutosaveSRAMtextBox.TabIndex = 5;
            // 
            // AutosaveSRAMradioButton1
            // 
            this.AutosaveSRAMradioButton1.AutoSize = true;
            this.AutosaveSRAMradioButton1.Location = new System.Drawing.Point(48, 33);
            this.AutosaveSRAMradioButton1.Name = "AutosaveSRAMradioButton1";
            this.AutosaveSRAMradioButton1.Size = new System.Drawing.Size(36, 17);
            this.AutosaveSRAMradioButton1.TabIndex = 2;
            this.AutosaveSRAMradioButton1.TabStop = true;
            this.AutosaveSRAMradioButton1.Text = "5s";
            this.AutosaveSRAMradioButton1.UseVisualStyleBackColor = true;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(202, 35);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(12, 13);
            this.label8.TabIndex = 6;
            this.label8.Text = "s";
            // 
            // AutosaveSRAMradioButton2
            // 
            this.AutosaveSRAMradioButton2.AutoSize = true;
            this.AutosaveSRAMradioButton2.Location = new System.Drawing.Point(90, 34);
            this.AutosaveSRAMradioButton2.Name = "AutosaveSRAMradioButton2";
            this.AutosaveSRAMradioButton2.Size = new System.Drawing.Size(39, 17);
            this.AutosaveSRAMradioButton2.TabIndex = 3;
            this.AutosaveSRAMradioButton2.TabStop = true;
            this.AutosaveSRAMradioButton2.Text = "5m";
            this.AutosaveSRAMradioButton2.UseVisualStyleBackColor = true;
            // 
            // AutosaveSRAMradioButton3
            // 
            this.AutosaveSRAMradioButton3.AutoSize = true;
            this.AutosaveSRAMradioButton3.Location = new System.Drawing.Point(131, 35);
            this.AutosaveSRAMradioButton3.Name = "AutosaveSRAMradioButton3";
            this.AutosaveSRAMradioButton3.Size = new System.Drawing.Size(14, 13);
            this.AutosaveSRAMradioButton3.TabIndex = 4;
            this.AutosaveSRAMradioButton3.TabStop = true;
            this.AutosaveSRAMradioButton3.UseVisualStyleBackColor = true;
            this.AutosaveSRAMradioButton3.CheckedChanged += new System.EventHandler(this.AutosaveSRAMRadioButton3_CheckedChanged);
            // 
            // AutosaveSRAMCheckbox
            // 
            this.AutosaveSRAMCheckbox.AutoSize = true;
            this.AutosaveSRAMCheckbox.Location = new System.Drawing.Point(6, 35);
            this.AutosaveSRAMCheckbox.Name = "AutosaveSRAMCheckbox";
            this.AutosaveSRAMCheckbox.Size = new System.Drawing.Size(15, 14);
            this.AutosaveSRAMCheckbox.TabIndex = 4;
            this.AutosaveSRAMCheckbox.UseVisualStyleBackColor = true;
            this.AutosaveSRAMCheckbox.CheckedChanged += new System.EventHandler(this.AutosaveSRAMCheckbox_CheckedChanged);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label7);
            this.panel1.Controls.Add(this.LuaInterfaceRadio);
            this.panel1.Controls.Add(this.NLuaRadio);
            this.panel1.Location = new System.Drawing.Point(6, 285);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(377, 61);
            this.panel1.TabIndex = 20;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(3, 1);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(50, 13);
            this.label7.TabIndex = 0;
            this.label7.Text = "Lua Core";
            // 
            // LuaInterfaceRadio
            // 
            this.LuaInterfaceRadio.AutoSize = true;
            this.LuaInterfaceRadio.Location = new System.Drawing.Point(4, 36);
            this.LuaInterfaceRadio.Name = "LuaInterfaceRadio";
            this.LuaInterfaceRadio.Size = new System.Drawing.Size(338, 17);
            this.LuaInterfaceRadio.TabIndex = 2;
            this.LuaInterfaceRadio.TabStop = true;
            this.LuaInterfaceRadio.Text = "Lua+LuaInterface - Faster but memory leaks,  use at your own risk!";
            this.LuaInterfaceRadio.UseVisualStyleBackColor = true;
            // 
            // NLuaRadio
            // 
            this.NLuaRadio.AutoSize = true;
            this.NLuaRadio.Location = new System.Drawing.Point(4, 17);
            this.NLuaRadio.Name = "NLuaRadio";
            this.NLuaRadio.Size = new System.Drawing.Size(96, 17);
            this.NLuaRadio.TabIndex = 1;
            this.NLuaRadio.TabStop = true;
            this.NLuaRadio.Text = "NLua+KopiLua";
            this.NLuaRadio.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(27, 243);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(276, 39);
            this.label6.TabIndex = 19;
            this.label6.Text = "Skips some integrity check exceptions in waterbox cores.\r\nUseful for romhackers. " +
    "Reboot core after changing.\r\nENABLING THIS VOIDS YOUR WARRANTY\r\n";
            // 
            // cbSkipWaterboxIntegrityChecks
            // 
            this.cbSkipWaterboxIntegrityChecks.AutoSize = true;
            this.cbSkipWaterboxIntegrityChecks.Location = new System.Drawing.Point(6, 223);
            this.cbSkipWaterboxIntegrityChecks.Name = "cbSkipWaterboxIntegrityChecks";
            this.cbSkipWaterboxIntegrityChecks.Size = new System.Drawing.Size(170, 17);
            this.cbSkipWaterboxIntegrityChecks.TabIndex = 18;
            this.cbSkipWaterboxIntegrityChecks.Text = "Skip waterbox integrity checks";
            this.cbSkipWaterboxIntegrityChecks.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(27, 194);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(299, 26);
            this.label5.TabIndex = 17;
            this.label5.Text = "Will prevent many Out Of Memory crashes during long movies.\r\nYou must restart the" +
    " program after changing this.";
            // 
            // cbMoviesOnDisk
            // 
            this.cbMoviesOnDisk.AutoSize = true;
            this.cbMoviesOnDisk.Location = new System.Drawing.Point(6, 174);
            this.cbMoviesOnDisk.Name = "cbMoviesOnDisk";
            this.cbMoviesOnDisk.Size = new System.Drawing.Size(259, 17);
            this.cbMoviesOnDisk.TabIndex = 16;
            this.cbMoviesOnDisk.Text = "Store movie working data on disk instead of RAM";
            this.cbMoviesOnDisk.UseVisualStyleBackColor = true;
            // 
            // LuaDuringTurboCheckbox
            // 
            this.LuaDuringTurboCheckbox.AutoSize = true;
            this.LuaDuringTurboCheckbox.Location = new System.Drawing.Point(6, 151);
            this.LuaDuringTurboCheckbox.Name = "LuaDuringTurboCheckbox";
            this.LuaDuringTurboCheckbox.Size = new System.Drawing.Size(166, 17);
            this.LuaDuringTurboCheckbox.TabIndex = 15;
            this.LuaDuringTurboCheckbox.Text = "Run lua scripts when turboing";
            this.LuaDuringTurboCheckbox.UseVisualStyleBackColor = true;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(27, 135);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(231, 13);
            this.label12.TabIndex = 14;
            this.label12.Text = "frames in which no input was polled (lag frames)";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(27, 122);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(268, 13);
            this.label13.TabIndex = 13;
            this.label13.Text = "When enabled, the frame advance button will skip over";
            // 
            // FrameAdvSkipLagCheckbox
            // 
            this.FrameAdvSkipLagCheckbox.AutoSize = true;
            this.FrameAdvSkipLagCheckbox.Location = new System.Drawing.Point(6, 102);
            this.FrameAdvSkipLagCheckbox.Name = "FrameAdvSkipLagCheckbox";
            this.FrameAdvSkipLagCheckbox.Size = new System.Drawing.Size(241, 17);
            this.FrameAdvSkipLagCheckbox.TabIndex = 12;
            this.FrameAdvSkipLagCheckbox.Text = "Frame advance button skips non-input frames";
            this.FrameAdvSkipLagCheckbox.UseVisualStyleBackColor = true;
            // 
            // BackupSRamCheckbox
            // 
            this.BackupSRamCheckbox.AutoSize = true;
            this.BackupSRamCheckbox.Location = new System.Drawing.Point(6, 12);
            this.BackupSRamCheckbox.Name = "BackupSRamCheckbox";
            this.BackupSRamCheckbox.Size = new System.Drawing.Size(203, 17);
            this.BackupSRamCheckbox.TabIndex = 3;
            this.BackupSRamCheckbox.Text = "Backup SaveRAM to .SaveRAM.bak";
            this.BackupSRamCheckbox.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.rbInputMethodOpenTK);
            this.groupBox3.Controls.Add(this.rbInputMethodDirectInput);
            this.groupBox3.Location = new System.Drawing.Point(6, 151);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(369, 48);
            this.groupBox3.TabIndex = 16;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Input Method (requires restart)";
            // 
            // rbInputMethodOpenTK
            // 
            this.rbInputMethodOpenTK.AutoSize = true;
            this.rbInputMethodOpenTK.Location = new System.Drawing.Point(136, 19);
            this.rbInputMethodOpenTK.Name = "rbInputMethodOpenTK";
            this.rbInputMethodOpenTK.Size = new System.Drawing.Size(65, 17);
            this.rbInputMethodOpenTK.TabIndex = 1;
            this.rbInputMethodOpenTK.TabStop = true;
            this.rbInputMethodOpenTK.Text = "OpenTK";
            this.rbInputMethodOpenTK.UseVisualStyleBackColor = true;
            // 
            // rbInputMethodDirectInput
            // 
            this.rbInputMethodDirectInput.AutoSize = true;
            this.rbInputMethodDirectInput.Location = new System.Drawing.Point(7, 20);
            this.rbInputMethodDirectInput.Name = "rbInputMethodDirectInput";
            this.rbInputMethodDirectInput.Size = new System.Drawing.Size(114, 17);
            this.rbInputMethodDirectInput.TabIndex = 0;
            this.rbInputMethodDirectInput.TabStop = true;
            this.rbInputMethodDirectInput.Text = "DirectInput+XInput";
            this.rbInputMethodDirectInput.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.StartPausedCheckbox);
            this.groupBox1.Controls.Add(this.label14);
            this.groupBox1.Controls.Add(this.StartFullScreenCheckbox);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.SingleInstanceModeCheckbox);
            this.groupBox1.Location = new System.Drawing.Point(6, 205);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(369, 121);
            this.groupBox1.TabIndex = 15;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Startup Options";
            // 
            // StartPausedCheckbox
            // 
            this.StartPausedCheckbox.AutoSize = true;
            this.StartPausedCheckbox.Location = new System.Drawing.Point(6, 19);
            this.StartPausedCheckbox.Name = "StartPausedCheckbox";
            this.StartPausedCheckbox.Size = new System.Drawing.Size(86, 17);
            this.StartPausedCheckbox.TabIndex = 2;
            this.StartPausedCheckbox.Text = "Start paused";
            this.StartPausedCheckbox.UseVisualStyleBackColor = true;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(26, 99);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(306, 13);
            this.label14.TabIndex = 12;
            this.label14.Text = "Note: Requires closing and reopening EmuHawk to take effect.";
            // 
            // StartFullScreenCheckbox
            // 
            this.StartFullScreenCheckbox.AutoSize = true;
            this.StartFullScreenCheckbox.Location = new System.Drawing.Point(6, 42);
            this.StartFullScreenCheckbox.Name = "StartFullScreenCheckbox";
            this.StartFullScreenCheckbox.Size = new System.Drawing.Size(110, 17);
            this.StartFullScreenCheckbox.TabIndex = 3;
            this.StartFullScreenCheckbox.Text = "Start in Fullscreen";
            this.StartFullScreenCheckbox.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(26, 85);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(275, 13);
            this.label3.TabIndex = 11;
            this.label3.Text = "Enable to force only one instance of EmuHawk at a time.";
            // 
            // SingleInstanceModeCheckbox
            // 
            this.SingleInstanceModeCheckbox.AutoSize = true;
            this.SingleInstanceModeCheckbox.Location = new System.Drawing.Point(6, 65);
            this.SingleInstanceModeCheckbox.Name = "SingleInstanceModeCheckbox";
            this.SingleInstanceModeCheckbox.Size = new System.Drawing.Size(127, 17);
            this.SingleInstanceModeCheckbox.TabIndex = 10;
            this.SingleInstanceModeCheckbox.Text = "Single instance mode";
            this.SingleInstanceModeCheckbox.UseVisualStyleBackColor = true;
            // 
            // HandleAlternateKeyboardLayoutsCheckBox
            // 
            this.HandleAlternateKeyboardLayoutsCheckBox.AutoSize = true;
            this.HandleAlternateKeyboardLayoutsCheckBox.Location = new System.Drawing.Point(7, 128);
            this.HandleAlternateKeyboardLayoutsCheckBox.Name = "HandleAlternateKeyboardLayoutsCheckBox";
            this.HandleAlternateKeyboardLayoutsCheckBox.Size = new System.Drawing.Size(320, 17);
            this.HandleAlternateKeyboardLayoutsCheckBox.TabIndex = 26;
            this.HandleAlternateKeyboardLayoutsCheckBox.Text = "Handle alternate keyboard layouts (e.g. Dvorak) [experimental]";
            this.HandleAlternateKeyboardLayoutsCheckBox.UseVisualStyleBackColor = true;
            // 
            // NeverAskSaveCheckbox
            // 
            this.NeverAskSaveCheckbox.AutoSize = true;
            this.NeverAskSaveCheckbox.Location = new System.Drawing.Point(6, 29);
            this.NeverAskSaveCheckbox.Name = "NeverAskSaveCheckbox";
            this.NeverAskSaveCheckbox.Size = new System.Drawing.Size(184, 17);
            this.NeverAskSaveCheckbox.TabIndex = 20;
            this.NeverAskSaveCheckbox.Text = "Never be asked to save changes";
            this.NeverAskSaveCheckbox.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(26, 112);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(349, 13);
            this.label2.TabIndex = 25;
            this.label2.Text = "When this is set, the client will receive user input even when focus is lost";
            // 
            // AcceptBackgroundInputCheckbox
            // 
            this.AcceptBackgroundInputCheckbox.AutoSize = true;
            this.AcceptBackgroundInputCheckbox.Location = new System.Drawing.Point(6, 92);
            this.AcceptBackgroundInputCheckbox.Name = "AcceptBackgroundInputCheckbox";
            this.AcceptBackgroundInputCheckbox.Size = new System.Drawing.Size(146, 17);
            this.AcceptBackgroundInputCheckbox.TabIndex = 23;
            this.AcceptBackgroundInputCheckbox.Text = "Accept background input";
            this.AcceptBackgroundInputCheckbox.UseVisualStyleBackColor = true;
            // 
            // AcceptBackgroundInputControllerOnlyCheckBox
            // 
            this.AcceptBackgroundInputControllerOnlyCheckBox.AutoSize = true;
            this.AcceptBackgroundInputControllerOnlyCheckBox.Enabled = false;
            this.AcceptBackgroundInputControllerOnlyCheckBox.Location = new System.Drawing.Point(156, 92);
            this.AcceptBackgroundInputControllerOnlyCheckBox.Name = "AcceptBackgroundInputControllerOnlyCheckBox";
            this.AcceptBackgroundInputControllerOnlyCheckBox.Size = new System.Drawing.Size(117, 17);
            this.AcceptBackgroundInputControllerOnlyCheckBox.TabIndex = 24;
            this.AcceptBackgroundInputControllerOnlyCheckBox.Text = "From controller only";
            this.AcceptBackgroundInputControllerOnlyCheckBox.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(26, 72);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(315, 13);
            this.label1.TabIndex = 22;
            this.label1.Text = "When this is set, the client will continue to run when it loses focus";
            // 
            // RunInBackgroundCheckbox
            // 
            this.RunInBackgroundCheckbox.AutoSize = true;
            this.RunInBackgroundCheckbox.Location = new System.Drawing.Point(6, 52);
            this.RunInBackgroundCheckbox.Name = "RunInBackgroundCheckbox";
            this.RunInBackgroundCheckbox.Size = new System.Drawing.Size(117, 17);
            this.RunInBackgroundCheckbox.TabIndex = 21;
            this.RunInBackgroundCheckbox.Text = "Run in background";
            this.RunInBackgroundCheckbox.UseVisualStyleBackColor = true;
            // 
            // SaveWindowPositionCheckbox
            // 
            this.SaveWindowPositionCheckbox.AutoSize = true;
            this.SaveWindowPositionCheckbox.Location = new System.Drawing.Point(196, 29);
            this.SaveWindowPositionCheckbox.Name = "SaveWindowPositionCheckbox";
            this.SaveWindowPositionCheckbox.Size = new System.Drawing.Size(133, 17);
            this.SaveWindowPositionCheckbox.TabIndex = 19;
            this.SaveWindowPositionCheckbox.Text = "Save Window Position";
            this.SaveWindowPositionCheckbox.UseVisualStyleBackColor = true;
            // 
            // EnableContextMenuCheckbox
            // 
            this.EnableContextMenuCheckbox.AutoSize = true;
            this.EnableContextMenuCheckbox.Location = new System.Drawing.Point(196, 6);
            this.EnableContextMenuCheckbox.Name = "EnableContextMenuCheckbox";
            this.EnableContextMenuCheckbox.Size = new System.Drawing.Size(128, 17);
            this.EnableContextMenuCheckbox.TabIndex = 18;
            this.EnableContextMenuCheckbox.Text = "Enable Context Menu";
            this.EnableContextMenuCheckbox.UseVisualStyleBackColor = true;
            // 
            // PauseWhenMenuActivatedCheckbox
            // 
            this.PauseWhenMenuActivatedCheckbox.AutoSize = true;
            this.PauseWhenMenuActivatedCheckbox.Location = new System.Drawing.Point(6, 6);
            this.PauseWhenMenuActivatedCheckbox.Name = "PauseWhenMenuActivatedCheckbox";
            this.PauseWhenMenuActivatedCheckbox.Size = new System.Drawing.Size(161, 17);
            this.PauseWhenMenuActivatedCheckbox.TabIndex = 17;
            this.PauseWhenMenuActivatedCheckbox.Text = "Pause when menu activated";
            this.PauseWhenMenuActivatedCheckbox.UseVisualStyleBackColor = true;
            // 
            // EmuHawkOptions
            // 
            this.AcceptButton = this.OkBtn;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelBtn;
            this.ClientSize = new System.Drawing.Size(418, 428);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.CancelBtn);
            this.Controls.Add(this.OkBtn);
            this.Name = "EmuHawkOptions";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Customization Options";
            this.Load += new System.EventHandler(this.GuiOptions_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.AutosaveSRAMtextBox)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
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
		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.Label label13;
		private System.Windows.Forms.CheckBox LuaDuringTurboCheckbox;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.CheckBox cbMoviesOnDisk;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.CheckBox cbSkipWaterboxIntegrityChecks;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.RadioButton LuaInterfaceRadio;
		private System.Windows.Forms.RadioButton NLuaRadio;
		private System.Windows.Forms.CheckBox AutosaveSRAMCheckbox;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.RadioButton AutosaveSRAMradioButton3;
		private System.Windows.Forms.RadioButton AutosaveSRAMradioButton2;
		private System.Windows.Forms.RadioButton AutosaveSRAMradioButton1;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.NumericUpDown AutosaveSRAMtextBox;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.CheckBox HandleAlternateKeyboardLayoutsCheckBox;
		private System.Windows.Forms.CheckBox NeverAskSaveCheckbox;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.CheckBox AcceptBackgroundInputCheckbox;
		private System.Windows.Forms.CheckBox AcceptBackgroundInputControllerOnlyCheckBox;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.CheckBox RunInBackgroundCheckbox;
		private System.Windows.Forms.CheckBox SaveWindowPositionCheckbox;
		private System.Windows.Forms.CheckBox EnableContextMenuCheckbox;
		private System.Windows.Forms.CheckBox PauseWhenMenuActivatedCheckbox;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.RadioButton rbInputMethodOpenTK;
		private System.Windows.Forms.RadioButton rbInputMethodDirectInput;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.CheckBox StartPausedCheckbox;
		private System.Windows.Forms.Label label14;
		private System.Windows.Forms.CheckBox StartFullScreenCheckbox;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.CheckBox SingleInstanceModeCheckbox;
	}
}