namespace BizHawk.Client.EmuHawk
{
	partial class Atari2600Debugger
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Atari2600Debugger));
			this.DebuggerMenu = new System.Windows.Forms.MenuStrip();
			this.FileSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.ExitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.OptionsSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.AutoloadMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SaveWindowPositionMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.TopmostMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FloatingWindowMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.RestoreDefaultsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.StepBtn = new System.Windows.Forms.Button();
			this.ScanlineAdvanceBtn = new System.Windows.Forms.Button();
			this.FrameAdvButton = new System.Windows.Forms.Button();
			this.RegistersBox = new System.Windows.Forms.GroupBox();
			this.label6 = new System.Windows.Forms.Label();
			this.YRegisterHexBox = new System.Windows.Forms.TextBox();
			this.XRegisterHexBox = new System.Windows.Forms.TextBox();
			this.ARegisterHexBox = new System.Windows.Forms.TextBox();
			this.SPRegisterHexBox = new System.Windows.Forms.TextBox();
			this.YRegisterBinaryBox = new System.Windows.Forms.TextBox();
			this.XRegisterBinaryBox = new System.Windows.Forms.TextBox();
			this.ARegisterBinaryBox = new System.Windows.Forms.TextBox();
			this.SPRegisterBinaryBox = new System.Windows.Forms.TextBox();
			this.PCRegisterBox = new System.Windows.Forms.TextBox();
			this.label5 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.CoreInfoBox = new System.Windows.Forms.GroupBox();
			this.LastAddressLabel = new System.Windows.Forms.Label();
			this.label9 = new System.Windows.Forms.Label();
			this.DistinctAccesLabel = new System.Windows.Forms.Label();
			this.label11 = new System.Windows.Forms.Label();
			this.TotalCyclesLabel = new System.Windows.Forms.Label();
			this.label10 = new System.Windows.Forms.Label();
			this.ScanlineLabel = new System.Windows.Forms.Label();
			this.FrameLabel = new System.Windows.Forms.Label();
			this.VBlankCheckbox = new System.Windows.Forms.CheckBox();
			this.VSyncChexkbox = new System.Windows.Forms.CheckBox();
			this.label8 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.TracerBox = new System.Windows.Forms.GroupBox();
			this.StepOverButton = new System.Windows.Forms.Button();
			this.StepOutButton = new System.Windows.Forms.Button();
			this.BreakpointGroupBox = new System.Windows.Forms.GroupBox();
			this.RemoveBreakpointButton = new System.Windows.Forms.Button();
			this.AddBreakpointButton = new System.Windows.Forms.Button();
			this.SPRegisterBox = new System.Windows.Forms.NumericUpDown();
			this.ARegisterBox = new System.Windows.Forms.NumericUpDown();
			this.XRegisterBox = new System.Windows.Forms.NumericUpDown();
			this.YRegisterBox = new System.Windows.Forms.NumericUpDown();
			this.BreakpointView = new BizHawk.Client.EmuHawk.VirtualListView();
			this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.TraceView = new BizHawk.Client.EmuHawk.VirtualListView();
			this.Script = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.CFlagCheckbox = new BizHawk.Client.EmuHawk.ReadonlyCheckBox();
			this.ZFlagCheckbox = new BizHawk.Client.EmuHawk.ReadonlyCheckBox();
			this.IFlagCheckbox = new BizHawk.Client.EmuHawk.ReadonlyCheckBox();
			this.DFlagCheckbox = new BizHawk.Client.EmuHawk.ReadonlyCheckBox();
			this.BFlagCheckbox = new BizHawk.Client.EmuHawk.ReadonlyCheckBox();
			this.TFlagCheckbox = new BizHawk.Client.EmuHawk.ReadonlyCheckBox();
			this.VFlagCheckbox = new BizHawk.Client.EmuHawk.ReadonlyCheckBox();
			this.NFlagCheckbox = new BizHawk.Client.EmuHawk.ReadonlyCheckBox();
			this.DebuggerMenu.SuspendLayout();
			this.RegistersBox.SuspendLayout();
			this.CoreInfoBox.SuspendLayout();
			this.TracerBox.SuspendLayout();
			this.BreakpointGroupBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.SPRegisterBox)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.ARegisterBox)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.XRegisterBox)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.YRegisterBox)).BeginInit();
			this.SuspendLayout();
			// 
			// DebuggerMenu
			// 
			this.DebuggerMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu,
            this.OptionsSubMenu});
			this.DebuggerMenu.Location = new System.Drawing.Point(0, 0);
			this.DebuggerMenu.Name = "DebuggerMenu";
			this.DebuggerMenu.Size = new System.Drawing.Size(653, 24);
			this.DebuggerMenu.TabIndex = 0;
			this.DebuggerMenu.Text = "menuStrip1";
			// 
			// FileSubMenu
			// 
			this.FileSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ExitMenuItem});
			this.FileSubMenu.Name = "FileSubMenu";
			this.FileSubMenu.Size = new System.Drawing.Size(37, 20);
			this.FileSubMenu.Text = "&File";
			// 
			// ExitMenuItem
			// 
			this.ExitMenuItem.Name = "ExitMenuItem";
			this.ExitMenuItem.ShortcutKeyDisplayString = "Alt+F4";
			this.ExitMenuItem.Size = new System.Drawing.Size(145, 22);
			this.ExitMenuItem.Text = "&Close";
			this.ExitMenuItem.Click += new System.EventHandler(this.ExitMenuItem_Click);
			// 
			// OptionsSubMenu
			// 
			this.OptionsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.AutoloadMenuItem,
            this.SaveWindowPositionMenuItem,
            this.TopmostMenuItem,
            this.FloatingWindowMenuItem,
            this.toolStripSeparator1,
            this.RestoreDefaultsMenuItem});
			this.OptionsSubMenu.Name = "OptionsSubMenu";
			this.OptionsSubMenu.Size = new System.Drawing.Size(61, 20);
			this.OptionsSubMenu.Text = "&Options";
			this.OptionsSubMenu.DropDownOpened += new System.EventHandler(this.OptionsSubMenu_DropDownOpened);
			// 
			// AutoloadMenuItem
			// 
			this.AutoloadMenuItem.Name = "AutoloadMenuItem";
			this.AutoloadMenuItem.Size = new System.Drawing.Size(191, 22);
			this.AutoloadMenuItem.Text = "&Autoload";
			this.AutoloadMenuItem.Click += new System.EventHandler(this.AutoloadMenuItem_Click);
			// 
			// SaveWindowPositionMenuItem
			// 
			this.SaveWindowPositionMenuItem.Name = "SaveWindowPositionMenuItem";
			this.SaveWindowPositionMenuItem.Size = new System.Drawing.Size(191, 22);
			this.SaveWindowPositionMenuItem.Text = "Save Window Position";
			this.SaveWindowPositionMenuItem.Click += new System.EventHandler(this.SaveWindowPositionMenuItem_Click);
			// 
			// TopmostMenuItem
			// 
			this.TopmostMenuItem.Name = "TopmostMenuItem";
			this.TopmostMenuItem.Size = new System.Drawing.Size(191, 22);
			this.TopmostMenuItem.Text = "Always on Top";
			this.TopmostMenuItem.Click += new System.EventHandler(this.TopmostMenuItem_Click);
			// 
			// FloatingWindowMenuItem
			// 
			this.FloatingWindowMenuItem.Name = "FloatingWindowMenuItem";
			this.FloatingWindowMenuItem.Size = new System.Drawing.Size(191, 22);
			this.FloatingWindowMenuItem.Text = "Floating Window";
			this.FloatingWindowMenuItem.Click += new System.EventHandler(this.FloatingWindowMenuItem_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(188, 6);
			// 
			// RestoreDefaultsMenuItem
			// 
			this.RestoreDefaultsMenuItem.Name = "RestoreDefaultsMenuItem";
			this.RestoreDefaultsMenuItem.Size = new System.Drawing.Size(191, 22);
			this.RestoreDefaultsMenuItem.Text = "Restore Defaults";
			this.RestoreDefaultsMenuItem.Click += new System.EventHandler(this.RestoreDefaultsMenuItem_Click);
			// 
			// StepBtn
			// 
			this.StepBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.StepBtn.Location = new System.Drawing.Point(566, 27);
			this.StepBtn.Name = "StepBtn";
			this.StepBtn.Size = new System.Drawing.Size(75, 23);
			this.StepBtn.TabIndex = 1;
			this.StepBtn.Text = "Step &Into";
			this.StepBtn.UseVisualStyleBackColor = true;
			this.StepBtn.Click += new System.EventHandler(this.StepBtn_Click);
			// 
			// ScanlineAdvanceBtn
			// 
			this.ScanlineAdvanceBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.ScanlineAdvanceBtn.Location = new System.Drawing.Point(566, 117);
			this.ScanlineAdvanceBtn.Name = "ScanlineAdvanceBtn";
			this.ScanlineAdvanceBtn.Size = new System.Drawing.Size(75, 23);
			this.ScanlineAdvanceBtn.TabIndex = 2;
			this.ScanlineAdvanceBtn.Text = "&Scan +1";
			this.ScanlineAdvanceBtn.UseVisualStyleBackColor = true;
			this.ScanlineAdvanceBtn.Click += new System.EventHandler(this.ScanlineAdvanceBtn_Click);
			// 
			// FrameAdvButton
			// 
			this.FrameAdvButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.FrameAdvButton.Location = new System.Drawing.Point(566, 144);
			this.FrameAdvButton.Name = "FrameAdvButton";
			this.FrameAdvButton.Size = new System.Drawing.Size(75, 23);
			this.FrameAdvButton.TabIndex = 3;
			this.FrameAdvButton.Text = "&Frame";
			this.FrameAdvButton.UseVisualStyleBackColor = true;
			this.FrameAdvButton.Click += new System.EventHandler(this.FrameAdvButton_Click);
			// 
			// RegistersBox
			// 
			this.RegistersBox.Controls.Add(this.YRegisterBox);
			this.RegistersBox.Controls.Add(this.XRegisterBox);
			this.RegistersBox.Controls.Add(this.ARegisterBox);
			this.RegistersBox.Controls.Add(this.SPRegisterBox);
			this.RegistersBox.Controls.Add(this.CFlagCheckbox);
			this.RegistersBox.Controls.Add(this.ZFlagCheckbox);
			this.RegistersBox.Controls.Add(this.IFlagCheckbox);
			this.RegistersBox.Controls.Add(this.DFlagCheckbox);
			this.RegistersBox.Controls.Add(this.BFlagCheckbox);
			this.RegistersBox.Controls.Add(this.TFlagCheckbox);
			this.RegistersBox.Controls.Add(this.VFlagCheckbox);
			this.RegistersBox.Controls.Add(this.label6);
			this.RegistersBox.Controls.Add(this.NFlagCheckbox);
			this.RegistersBox.Controls.Add(this.YRegisterHexBox);
			this.RegistersBox.Controls.Add(this.XRegisterHexBox);
			this.RegistersBox.Controls.Add(this.ARegisterHexBox);
			this.RegistersBox.Controls.Add(this.SPRegisterHexBox);
			this.RegistersBox.Controls.Add(this.YRegisterBinaryBox);
			this.RegistersBox.Controls.Add(this.XRegisterBinaryBox);
			this.RegistersBox.Controls.Add(this.ARegisterBinaryBox);
			this.RegistersBox.Controls.Add(this.SPRegisterBinaryBox);
			this.RegistersBox.Controls.Add(this.PCRegisterBox);
			this.RegistersBox.Controls.Add(this.label5);
			this.RegistersBox.Controls.Add(this.label4);
			this.RegistersBox.Controls.Add(this.label3);
			this.RegistersBox.Controls.Add(this.label2);
			this.RegistersBox.Controls.Add(this.label1);
			this.RegistersBox.Location = new System.Drawing.Point(12, 27);
			this.RegistersBox.Name = "RegistersBox";
			this.RegistersBox.Size = new System.Drawing.Size(242, 155);
			this.RegistersBox.TabIndex = 4;
			this.RegistersBox.TabStop = false;
			this.RegistersBox.Text = "Registers";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(7, 126);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(35, 13);
			this.label6.TabIndex = 19;
			this.label6.Text = "Flags:";
			// 
			// YRegisterHexBox
			// 
			this.YRegisterHexBox.Location = new System.Drawing.Point(86, 97);
			this.YRegisterHexBox.Name = "YRegisterHexBox";
			this.YRegisterHexBox.ReadOnly = true;
			this.YRegisterHexBox.Size = new System.Drawing.Size(32, 20);
			this.YRegisterHexBox.TabIndex = 17;
			// 
			// XRegisterHexBox
			// 
			this.XRegisterHexBox.Location = new System.Drawing.Point(86, 77);
			this.XRegisterHexBox.Name = "XRegisterHexBox";
			this.XRegisterHexBox.ReadOnly = true;
			this.XRegisterHexBox.Size = new System.Drawing.Size(32, 20);
			this.XRegisterHexBox.TabIndex = 16;
			// 
			// ARegisterHexBox
			// 
			this.ARegisterHexBox.Location = new System.Drawing.Point(86, 57);
			this.ARegisterHexBox.Name = "ARegisterHexBox";
			this.ARegisterHexBox.ReadOnly = true;
			this.ARegisterHexBox.Size = new System.Drawing.Size(32, 20);
			this.ARegisterHexBox.TabIndex = 15;
			// 
			// SPRegisterHexBox
			// 
			this.SPRegisterHexBox.Location = new System.Drawing.Point(86, 37);
			this.SPRegisterHexBox.Name = "SPRegisterHexBox";
			this.SPRegisterHexBox.ReadOnly = true;
			this.SPRegisterHexBox.Size = new System.Drawing.Size(32, 20);
			this.SPRegisterHexBox.TabIndex = 14;
			// 
			// YRegisterBinaryBox
			// 
			this.YRegisterBinaryBox.Location = new System.Drawing.Point(121, 97);
			this.YRegisterBinaryBox.Name = "YRegisterBinaryBox";
			this.YRegisterBinaryBox.ReadOnly = true;
			this.YRegisterBinaryBox.Size = new System.Drawing.Size(62, 20);
			this.YRegisterBinaryBox.TabIndex = 13;
			this.YRegisterBinaryBox.Text = "0000 0000";
			// 
			// XRegisterBinaryBox
			// 
			this.XRegisterBinaryBox.Location = new System.Drawing.Point(121, 77);
			this.XRegisterBinaryBox.Name = "XRegisterBinaryBox";
			this.XRegisterBinaryBox.ReadOnly = true;
			this.XRegisterBinaryBox.Size = new System.Drawing.Size(62, 20);
			this.XRegisterBinaryBox.TabIndex = 12;
			this.XRegisterBinaryBox.Text = "0000 0000";
			// 
			// ARegisterBinaryBox
			// 
			this.ARegisterBinaryBox.Location = new System.Drawing.Point(121, 57);
			this.ARegisterBinaryBox.Name = "ARegisterBinaryBox";
			this.ARegisterBinaryBox.ReadOnly = true;
			this.ARegisterBinaryBox.Size = new System.Drawing.Size(62, 20);
			this.ARegisterBinaryBox.TabIndex = 11;
			this.ARegisterBinaryBox.Text = "0000 0000";
			// 
			// SPRegisterBinaryBox
			// 
			this.SPRegisterBinaryBox.Location = new System.Drawing.Point(121, 37);
			this.SPRegisterBinaryBox.Name = "SPRegisterBinaryBox";
			this.SPRegisterBinaryBox.ReadOnly = true;
			this.SPRegisterBinaryBox.Size = new System.Drawing.Size(62, 20);
			this.SPRegisterBinaryBox.TabIndex = 10;
			this.SPRegisterBinaryBox.Text = "0000 0000";
			// 
			// PCRegisterBox
			// 
			this.PCRegisterBox.Location = new System.Drawing.Point(36, 16);
			this.PCRegisterBox.Name = "PCRegisterBox";
			this.PCRegisterBox.ReadOnly = true;
			this.PCRegisterBox.Size = new System.Drawing.Size(72, 20);
			this.PCRegisterBox.TabIndex = 5;
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(6, 100);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(17, 13);
			this.label5.TabIndex = 4;
			this.label5.Text = "Y:";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(6, 80);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(17, 13);
			this.label4.TabIndex = 3;
			this.label4.Text = "X:";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(6, 60);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(17, 13);
			this.label3.TabIndex = 2;
			this.label3.Text = "A:";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(6, 40);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(24, 13);
			this.label2.TabIndex = 1;
			this.label2.Text = "SP:";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(6, 19);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(24, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "PC:";
			// 
			// CoreInfoBox
			// 
			this.CoreInfoBox.Controls.Add(this.LastAddressLabel);
			this.CoreInfoBox.Controls.Add(this.label9);
			this.CoreInfoBox.Controls.Add(this.DistinctAccesLabel);
			this.CoreInfoBox.Controls.Add(this.label11);
			this.CoreInfoBox.Controls.Add(this.TotalCyclesLabel);
			this.CoreInfoBox.Controls.Add(this.label10);
			this.CoreInfoBox.Controls.Add(this.ScanlineLabel);
			this.CoreInfoBox.Controls.Add(this.FrameLabel);
			this.CoreInfoBox.Controls.Add(this.VBlankCheckbox);
			this.CoreInfoBox.Controls.Add(this.VSyncChexkbox);
			this.CoreInfoBox.Controls.Add(this.label8);
			this.CoreInfoBox.Controls.Add(this.label7);
			this.CoreInfoBox.Location = new System.Drawing.Point(260, 27);
			this.CoreInfoBox.Name = "CoreInfoBox";
			this.CoreInfoBox.Size = new System.Drawing.Size(265, 155);
			this.CoreInfoBox.TabIndex = 5;
			this.CoreInfoBox.TabStop = false;
			// 
			// LastAddressLabel
			// 
			this.LastAddressLabel.AutoSize = true;
			this.LastAddressLabel.Location = new System.Drawing.Point(191, 15);
			this.LastAddressLabel.Name = "LastAddressLabel";
			this.LastAddressLabel.Size = new System.Drawing.Size(13, 13);
			this.LastAddressLabel.TabIndex = 13;
			this.LastAddressLabel.Text = "0";
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point(96, 15);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(71, 13);
			this.label9.TabIndex = 12;
			this.label9.Text = "Last Address:";
			// 
			// DistinctAccesLabel
			// 
			this.DistinctAccesLabel.AutoSize = true;
			this.DistinctAccesLabel.Location = new System.Drawing.Point(191, 38);
			this.DistinctAccesLabel.Name = "DistinctAccesLabel";
			this.DistinctAccesLabel.Size = new System.Drawing.Size(13, 13);
			this.DistinctAccesLabel.TabIndex = 11;
			this.DistinctAccesLabel.Text = "0";
			// 
			// label11
			// 
			this.label11.AutoSize = true;
			this.label11.Location = new System.Drawing.Point(96, 38);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(94, 13);
			this.label11.TabIndex = 10;
			this.label11.Text = "Distinct Accesses:";
			// 
			// TotalCyclesLabel
			// 
			this.TotalCyclesLabel.AutoSize = true;
			this.TotalCyclesLabel.Location = new System.Drawing.Point(72, 61);
			this.TotalCyclesLabel.Name = "TotalCyclesLabel";
			this.TotalCyclesLabel.Size = new System.Drawing.Size(13, 13);
			this.TotalCyclesLabel.TabIndex = 9;
			this.TotalCyclesLabel.Text = "0";
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Location = new System.Drawing.Point(6, 61);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(68, 13);
			this.label10.TabIndex = 8;
			this.label10.Text = "Total Cycles:";
			// 
			// ScanlineLabel
			// 
			this.ScanlineLabel.AutoSize = true;
			this.ScanlineLabel.Location = new System.Drawing.Point(72, 38);
			this.ScanlineLabel.Name = "ScanlineLabel";
			this.ScanlineLabel.Size = new System.Drawing.Size(13, 13);
			this.ScanlineLabel.TabIndex = 7;
			this.ScanlineLabel.Text = "0";
			// 
			// FrameLabel
			// 
			this.FrameLabel.AutoSize = true;
			this.FrameLabel.Location = new System.Drawing.Point(72, 15);
			this.FrameLabel.Name = "FrameLabel";
			this.FrameLabel.Size = new System.Drawing.Size(13, 13);
			this.FrameLabel.TabIndex = 6;
			this.FrameLabel.Text = "0";
			// 
			// VBlankCheckbox
			// 
			this.VBlankCheckbox.AutoSize = true;
			this.VBlankCheckbox.Location = new System.Drawing.Point(9, 107);
			this.VBlankCheckbox.Name = "VBlankCheckbox";
			this.VBlankCheckbox.Size = new System.Drawing.Size(60, 17);
			this.VBlankCheckbox.TabIndex = 5;
			this.VBlankCheckbox.Text = "VBlank";
			this.VBlankCheckbox.UseVisualStyleBackColor = true;
			// 
			// VSyncChexkbox
			// 
			this.VSyncChexkbox.AutoSize = true;
			this.VSyncChexkbox.Location = new System.Drawing.Point(9, 90);
			this.VSyncChexkbox.Name = "VSyncChexkbox";
			this.VSyncChexkbox.Size = new System.Drawing.Size(57, 17);
			this.VSyncChexkbox.TabIndex = 4;
			this.VSyncChexkbox.Text = "VSync";
			this.VSyncChexkbox.UseVisualStyleBackColor = true;
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(6, 38);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(51, 13);
			this.label8.TabIndex = 2;
			this.label8.Text = "Scanline:";
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(6, 15);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(39, 13);
			this.label7.TabIndex = 0;
			this.label7.Text = "Frame:";
			// 
			// TracerBox
			// 
			this.TracerBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.TracerBox.Controls.Add(this.TraceView);
			this.TracerBox.Location = new System.Drawing.Point(12, 188);
			this.TracerBox.Name = "TracerBox";
			this.TracerBox.Size = new System.Drawing.Size(407, 444);
			this.TracerBox.TabIndex = 6;
			this.TracerBox.TabStop = false;
			this.TracerBox.Text = "Trace log";
			// 
			// StepOverButton
			// 
			this.StepOverButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.StepOverButton.Location = new System.Drawing.Point(566, 56);
			this.StepOverButton.Name = "StepOverButton";
			this.StepOverButton.Size = new System.Drawing.Size(75, 23);
			this.StepOverButton.TabIndex = 7;
			this.StepOverButton.Text = "Step &Over";
			this.StepOverButton.UseVisualStyleBackColor = true;
			// 
			// StepOutButton
			// 
			this.StepOutButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.StepOutButton.Location = new System.Drawing.Point(566, 82);
			this.StepOutButton.Name = "StepOutButton";
			this.StepOutButton.Size = new System.Drawing.Size(75, 23);
			this.StepOutButton.TabIndex = 8;
			this.StepOutButton.Text = "Step O&ut";
			this.StepOutButton.UseVisualStyleBackColor = true;
			// 
			// BreakpointGroupBox
			// 
			this.BreakpointGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.BreakpointGroupBox.Controls.Add(this.RemoveBreakpointButton);
			this.BreakpointGroupBox.Controls.Add(this.AddBreakpointButton);
			this.BreakpointGroupBox.Controls.Add(this.BreakpointView);
			this.BreakpointGroupBox.Location = new System.Drawing.Point(435, 188);
			this.BreakpointGroupBox.Name = "BreakpointGroupBox";
			this.BreakpointGroupBox.Size = new System.Drawing.Size(206, 444);
			this.BreakpointGroupBox.TabIndex = 7;
			this.BreakpointGroupBox.TabStop = false;
			this.BreakpointGroupBox.Text = "Breakpoints";
			// 
			// RemoveBreakpointButton
			// 
			this.RemoveBreakpointButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.RemoveBreakpointButton.Location = new System.Drawing.Point(125, 409);
			this.RemoveBreakpointButton.Name = "RemoveBreakpointButton";
			this.RemoveBreakpointButton.Size = new System.Drawing.Size(75, 23);
			this.RemoveBreakpointButton.TabIndex = 6;
			this.RemoveBreakpointButton.Text = "&Remove";
			this.RemoveBreakpointButton.UseVisualStyleBackColor = true;
			this.RemoveBreakpointButton.Click += new System.EventHandler(this.RemoveBreakpointButton_Click);
			// 
			// AddBreakpointButton
			// 
			this.AddBreakpointButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.AddBreakpointButton.Location = new System.Drawing.Point(8, 409);
			this.AddBreakpointButton.Name = "AddBreakpointButton";
			this.AddBreakpointButton.Size = new System.Drawing.Size(75, 23);
			this.AddBreakpointButton.TabIndex = 5;
			this.AddBreakpointButton.Text = "&Add";
			this.AddBreakpointButton.UseVisualStyleBackColor = true;
			this.AddBreakpointButton.Click += new System.EventHandler(this.AddBreakpointButton_Click);
			// 
			// SPRegisterBox
			// 
			this.SPRegisterBox.Location = new System.Drawing.Point(36, 37);
			this.SPRegisterBox.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
			this.SPRegisterBox.Name = "SPRegisterBox";
			this.SPRegisterBox.Size = new System.Drawing.Size(43, 20);
			this.SPRegisterBox.TabIndex = 27;
			this.SPRegisterBox.Value = new decimal(new int[] {
            255,
            0,
            0,
            0});
			this.SPRegisterBox.ValueChanged += new System.EventHandler(this.SPRegisterBox_ValueChanged);
			// 
			// ARegisterBox
			// 
			this.ARegisterBox.Location = new System.Drawing.Point(36, 56);
			this.ARegisterBox.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
			this.ARegisterBox.Name = "ARegisterBox";
			this.ARegisterBox.Size = new System.Drawing.Size(43, 20);
			this.ARegisterBox.TabIndex = 28;
			this.ARegisterBox.Value = new decimal(new int[] {
            255,
            0,
            0,
            0});
			this.ARegisterBox.ValueChanged += new System.EventHandler(this.ARegisterBox_ValueChanged);
			// 
			// XRegisterBox
			// 
			this.XRegisterBox.Location = new System.Drawing.Point(36, 76);
			this.XRegisterBox.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
			this.XRegisterBox.Name = "XRegisterBox";
			this.XRegisterBox.Size = new System.Drawing.Size(43, 20);
			this.XRegisterBox.TabIndex = 29;
			this.XRegisterBox.Value = new decimal(new int[] {
            255,
            0,
            0,
            0});
			this.XRegisterBox.ValueChanged += new System.EventHandler(this.XRegisterBox_ValueChanged);
			// 
			// YRegisterBox
			// 
			this.YRegisterBox.Location = new System.Drawing.Point(36, 96);
			this.YRegisterBox.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
			this.YRegisterBox.Name = "YRegisterBox";
			this.YRegisterBox.Size = new System.Drawing.Size(43, 20);
			this.YRegisterBox.TabIndex = 30;
			this.YRegisterBox.Value = new decimal(new int[] {
            255,
            0,
            0,
            0});
			this.YRegisterBox.ValueChanged += new System.EventHandler(this.YRegisterBox_ValueChanged);
			// 
			// BreakpointView
			// 
			this.BreakpointView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.BreakpointView.BlazingFast = false;
			this.BreakpointView.CheckBoxes = true;
			this.BreakpointView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
			this.BreakpointView.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.BreakpointView.FullRowSelect = true;
			this.BreakpointView.GridLines = true;
			this.BreakpointView.HideSelection = false;
			this.BreakpointView.ItemCount = 0;
			this.BreakpointView.Location = new System.Drawing.Point(8, 18);
			this.BreakpointView.Name = "BreakpointView";
			this.BreakpointView.SelectAllInProgress = false;
			this.BreakpointView.selectedItem = -1;
			this.BreakpointView.Size = new System.Drawing.Size(192, 384);
			this.BreakpointView.TabIndex = 4;
			this.BreakpointView.TabStop = false;
			this.BreakpointView.UseCompatibleStateImageBehavior = false;
			this.BreakpointView.View = System.Windows.Forms.View.Details;
			this.BreakpointView.SelectedIndexChanged += new System.EventHandler(this.BreakpointView_SelectedIndexChanged);
			this.BreakpointView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.BreakpointView_KeyDown);
			// 
			// columnHeader1
			// 
			this.columnHeader1.Text = "Address";
			this.columnHeader1.Width = 85;
			// 
			// columnHeader2
			// 
			this.columnHeader2.Text = "Type";
			this.columnHeader2.Width = 103;
			// 
			// TraceView
			// 
			this.TraceView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.TraceView.BlazingFast = false;
			this.TraceView.CheckBoxes = true;
			this.TraceView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Script});
			this.TraceView.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.TraceView.FullRowSelect = true;
			this.TraceView.GridLines = true;
			this.TraceView.HideSelection = false;
			this.TraceView.ItemCount = 0;
			this.TraceView.Location = new System.Drawing.Point(8, 18);
			this.TraceView.Name = "TraceView";
			this.TraceView.SelectAllInProgress = false;
			this.TraceView.selectedItem = -1;
			this.TraceView.Size = new System.Drawing.Size(393, 414);
			this.TraceView.TabIndex = 4;
			this.TraceView.TabStop = false;
			this.TraceView.UseCompatibleStateImageBehavior = false;
			this.TraceView.View = System.Windows.Forms.View.Details;
			// 
			// Script
			// 
			this.Script.Text = "Instructions";
			this.Script.Width = 599;
			// 
			// CFlagCheckbox
			// 
			this.CFlagCheckbox.Appearance = System.Windows.Forms.Appearance.Button;
			this.CFlagCheckbox.AutoSize = true;
			this.CFlagCheckbox.Location = new System.Drawing.Point(209, 121);
			this.CFlagCheckbox.Name = "CFlagCheckbox";
			this.CFlagCheckbox.Size = new System.Drawing.Size(24, 23);
			this.CFlagCheckbox.TabIndex = 26;
			this.CFlagCheckbox.Text = "C";
			this.CFlagCheckbox.UseVisualStyleBackColor = true;
			// 
			// ZFlagCheckbox
			// 
			this.ZFlagCheckbox.Appearance = System.Windows.Forms.Appearance.Button;
			this.ZFlagCheckbox.AutoSize = true;
			this.ZFlagCheckbox.Location = new System.Drawing.Point(186, 121);
			this.ZFlagCheckbox.Name = "ZFlagCheckbox";
			this.ZFlagCheckbox.Size = new System.Drawing.Size(24, 23);
			this.ZFlagCheckbox.TabIndex = 25;
			this.ZFlagCheckbox.Text = "Z";
			this.ZFlagCheckbox.UseVisualStyleBackColor = true;
			// 
			// IFlagCheckbox
			// 
			this.IFlagCheckbox.Appearance = System.Windows.Forms.Appearance.Button;
			this.IFlagCheckbox.AutoSize = true;
			this.IFlagCheckbox.Location = new System.Drawing.Point(167, 121);
			this.IFlagCheckbox.Name = "IFlagCheckbox";
			this.IFlagCheckbox.Size = new System.Drawing.Size(20, 23);
			this.IFlagCheckbox.TabIndex = 24;
			this.IFlagCheckbox.Text = "I";
			this.IFlagCheckbox.UseVisualStyleBackColor = true;
			// 
			// DFlagCheckbox
			// 
			this.DFlagCheckbox.Appearance = System.Windows.Forms.Appearance.Button;
			this.DFlagCheckbox.AutoSize = true;
			this.DFlagCheckbox.Location = new System.Drawing.Point(143, 121);
			this.DFlagCheckbox.Name = "DFlagCheckbox";
			this.DFlagCheckbox.Size = new System.Drawing.Size(25, 23);
			this.DFlagCheckbox.TabIndex = 23;
			this.DFlagCheckbox.Text = "D";
			this.DFlagCheckbox.UseVisualStyleBackColor = true;
			// 
			// BFlagCheckbox
			// 
			this.BFlagCheckbox.Appearance = System.Windows.Forms.Appearance.Button;
			this.BFlagCheckbox.AutoSize = true;
			this.BFlagCheckbox.Location = new System.Drawing.Point(118, 121);
			this.BFlagCheckbox.Name = "BFlagCheckbox";
			this.BFlagCheckbox.Size = new System.Drawing.Size(24, 23);
			this.BFlagCheckbox.TabIndex = 22;
			this.BFlagCheckbox.Text = "B";
			this.BFlagCheckbox.UseVisualStyleBackColor = true;
			// 
			// TFlagCheckbox
			// 
			this.TFlagCheckbox.Appearance = System.Windows.Forms.Appearance.Button;
			this.TFlagCheckbox.AutoSize = true;
			this.TFlagCheckbox.Location = new System.Drawing.Point(95, 121);
			this.TFlagCheckbox.Name = "TFlagCheckbox";
			this.TFlagCheckbox.Size = new System.Drawing.Size(24, 23);
			this.TFlagCheckbox.TabIndex = 21;
			this.TFlagCheckbox.Text = "T";
			this.TFlagCheckbox.UseVisualStyleBackColor = true;
			// 
			// VFlagCheckbox
			// 
			this.VFlagCheckbox.Appearance = System.Windows.Forms.Appearance.Button;
			this.VFlagCheckbox.AutoSize = true;
			this.VFlagCheckbox.Location = new System.Drawing.Point(72, 121);
			this.VFlagCheckbox.Name = "VFlagCheckbox";
			this.VFlagCheckbox.Size = new System.Drawing.Size(24, 23);
			this.VFlagCheckbox.TabIndex = 20;
			this.VFlagCheckbox.Text = "V";
			this.VFlagCheckbox.UseVisualStyleBackColor = true;
			// 
			// NFlagCheckbox
			// 
			this.NFlagCheckbox.Appearance = System.Windows.Forms.Appearance.Button;
			this.NFlagCheckbox.AutoSize = true;
			this.NFlagCheckbox.Location = new System.Drawing.Point(48, 121);
			this.NFlagCheckbox.Name = "NFlagCheckbox";
			this.NFlagCheckbox.Size = new System.Drawing.Size(25, 23);
			this.NFlagCheckbox.TabIndex = 18;
			this.NFlagCheckbox.Text = "N";
			this.NFlagCheckbox.UseVisualStyleBackColor = true;
			// 
			// Atari2600Debugger
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(653, 702);
			this.Controls.Add(this.BreakpointGroupBox);
			this.Controls.Add(this.StepOutButton);
			this.Controls.Add(this.StepOverButton);
			this.Controls.Add(this.TracerBox);
			this.Controls.Add(this.CoreInfoBox);
			this.Controls.Add(this.RegistersBox);
			this.Controls.Add(this.FrameAdvButton);
			this.Controls.Add(this.ScanlineAdvanceBtn);
			this.Controls.Add(this.StepBtn);
			this.Controls.Add(this.DebuggerMenu);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.DebuggerMenu;
			this.Name = "Atari2600Debugger";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = " Debugger";
			this.Load += new System.EventHandler(this.Atari2600Debugger_Load);
			this.DebuggerMenu.ResumeLayout(false);
			this.DebuggerMenu.PerformLayout();
			this.RegistersBox.ResumeLayout(false);
			this.RegistersBox.PerformLayout();
			this.CoreInfoBox.ResumeLayout(false);
			this.CoreInfoBox.PerformLayout();
			this.TracerBox.ResumeLayout(false);
			this.BreakpointGroupBox.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.SPRegisterBox)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.ARegisterBox)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.XRegisterBox)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.YRegisterBox)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip DebuggerMenu;
		private System.Windows.Forms.ToolStripMenuItem FileSubMenu;
		private System.Windows.Forms.ToolStripMenuItem ExitMenuItem;
		private System.Windows.Forms.Button StepBtn;
		private System.Windows.Forms.Button ScanlineAdvanceBtn;
		private System.Windows.Forms.Button FrameAdvButton;
		private System.Windows.Forms.GroupBox RegistersBox;
		private System.Windows.Forms.TextBox PCRegisterBox;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox SPRegisterBinaryBox;
		private System.Windows.Forms.TextBox YRegisterBinaryBox;
		private System.Windows.Forms.TextBox XRegisterBinaryBox;
		private System.Windows.Forms.TextBox ARegisterBinaryBox;
		private System.Windows.Forms.TextBox YRegisterHexBox;
		private System.Windows.Forms.TextBox XRegisterHexBox;
		private System.Windows.Forms.TextBox ARegisterHexBox;
		private System.Windows.Forms.TextBox SPRegisterHexBox;
		private ReadonlyCheckBox CFlagCheckbox;
		private ReadonlyCheckBox ZFlagCheckbox;
		private ReadonlyCheckBox IFlagCheckbox;
		private ReadonlyCheckBox DFlagCheckbox;
		private ReadonlyCheckBox BFlagCheckbox;
		private ReadonlyCheckBox TFlagCheckbox;
		private ReadonlyCheckBox VFlagCheckbox;
		private System.Windows.Forms.Label label6;
		private ReadonlyCheckBox NFlagCheckbox;
		private System.Windows.Forms.GroupBox CoreInfoBox;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.GroupBox TracerBox;
		private VirtualListView TraceView;
		public System.Windows.Forms.ColumnHeader Script;
		private System.Windows.Forms.CheckBox VBlankCheckbox;
		private System.Windows.Forms.CheckBox VSyncChexkbox;
		private System.Windows.Forms.ToolStripMenuItem OptionsSubMenu;
		private System.Windows.Forms.ToolStripMenuItem AutoloadMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SaveWindowPositionMenuItem;
		private System.Windows.Forms.ToolStripMenuItem TopmostMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FloatingWindowMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem RestoreDefaultsMenuItem;
		private System.Windows.Forms.Button StepOverButton;
		private System.Windows.Forms.Button StepOutButton;
		private System.Windows.Forms.GroupBox BreakpointGroupBox;
		private VirtualListView BreakpointView;
		public System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.Button AddBreakpointButton;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.Label FrameLabel;
		private System.Windows.Forms.Label ScanlineLabel;
		private System.Windows.Forms.Label TotalCyclesLabel;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.Label DistinctAccesLabel;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.Label LastAddressLabel;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Button RemoveBreakpointButton;
		private System.Windows.Forms.NumericUpDown SPRegisterBox;
		private System.Windows.Forms.NumericUpDown ARegisterBox;
		private System.Windows.Forms.NumericUpDown XRegisterBox;
		private System.Windows.Forms.NumericUpDown YRegisterBox;
	}
}