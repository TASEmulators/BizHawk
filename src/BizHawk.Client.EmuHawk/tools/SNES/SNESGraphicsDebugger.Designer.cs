using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	partial class SNESGraphicsDebugger
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SNESGraphicsDebugger));
            this.menuStrip1 = new BizHawk.WinForms.Controls.MenuStripEx();
            this.fileToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.saveScreenshotAsToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.saveScreenshotToClipboardToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.groupFreeze = new System.Windows.Forms.GroupBox();
            this.pnGroupFreeze = new System.Windows.Forms.Panel();
            this.labelMemory = new BizHawk.WinForms.Controls.LocLabelEx();
            this.check2x = new System.Windows.Forms.CheckBox();
            this.comboDisplayType = new System.Windows.Forms.ComboBox();
            this.label47 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.pnBackdropColor = new System.Windows.Forms.Panel();
            this.comboPalette = new System.Windows.Forms.ComboBox();
            this.checkBackdropColor = new System.Windows.Forms.CheckBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.checkScanlineControl = new System.Windows.Forms.CheckBox();
            this.label19 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.nudScanline = new System.Windows.Forms.NumericUpDown();
            this.sliderScanline = new System.Windows.Forms.TrackBar();
            this.label24 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.groupBox6 = new System.Windows.Forms.GroupBox();
            this.labelClipboard = new BizHawk.WinForms.Controls.LocLabelEx();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label26 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.txtOBSELT1OfsBits = new System.Windows.Forms.TextBox();
            this.groupBox8 = new System.Windows.Forms.GroupBox();
            this.radioButton6 = new System.Windows.Forms.RadioButton();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.radioButton10 = new System.Windows.Forms.RadioButton();
            this.radioButton15 = new System.Windows.Forms.RadioButton();
            this.radioButton5 = new System.Windows.Forms.RadioButton();
            this.radioButton14 = new System.Windows.Forms.RadioButton();
            this.radioButton4 = new System.Windows.Forms.RadioButton();
            this.radioButton3 = new System.Windows.Forms.RadioButton();
            this.radioButton13 = new System.Windows.Forms.RadioButton();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.lblEnPrio3 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.txtOBSELT1OfsDescr = new System.Windows.Forms.TextBox();
            this.checkEN1_OBJ = new System.Windows.Forms.CheckBox();
            this.label30 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.checkEN1_BG4 = new System.Windows.Forms.CheckBox();
            this.txtOBSELBaseBits = new System.Windows.Forms.TextBox();
            this.checkEN1_BG3 = new System.Windows.Forms.CheckBox();
            this.txtOBSELBaseDescr = new System.Windows.Forms.TextBox();
            this.checkEN1_BG2 = new System.Windows.Forms.CheckBox();
            this.label29 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.checkEN1_BG1 = new System.Windows.Forms.CheckBox();
            this.txtOBSELSizeBits = new System.Windows.Forms.TextBox();
            this.checkEN3_OBJ = new System.Windows.Forms.CheckBox();
            this.checkEN2_OBJ = new System.Windows.Forms.CheckBox();
            this.txtOBSELSizeDescr = new System.Windows.Forms.TextBox();
            this.lblEnPrio2 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label28 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.lblEnPrio1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label20 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.checkScreenExtbg = new System.Windows.Forms.CheckBox();
            this.label38 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label21 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.txtScreenCGADSUB_Half = new System.Windows.Forms.CheckBox();
            this.checkScreenHires = new System.Windows.Forms.CheckBox();
            this.lblEnPrio0 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label18391 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.checkEN0_OBJ = new System.Windows.Forms.CheckBox();
            this.checkScreenOverscan = new System.Windows.Forms.CheckBox();
            this.checkEN0_BG4 = new System.Windows.Forms.CheckBox();
            this.label198129381279841 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.checkEN0_BG3 = new System.Windows.Forms.CheckBox();
            this.checkScreenObjInterlace = new System.Windows.Forms.CheckBox();
            this.checkEN0_BG2 = new System.Windows.Forms.CheckBox();
            this.label123812831 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.checkEN0_BG1 = new System.Windows.Forms.CheckBox();
            this.checkScreenInterlace = new System.Windows.Forms.CheckBox();
            this.txtScreenCGADSUB_AddSub_Descr = new System.Windows.Forms.TextBox();
            this.label2193813 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.txtScreenCGADSUB_AddSub = new System.Windows.Forms.TextBox();
            this.label36 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.checkTMOBJ = new BizHawk.Client.EmuHawk.CustomCheckBox();
            this.checkTSOBJ = new BizHawk.Client.EmuHawk.CustomCheckBox();
            this.checkMathBK = new BizHawk.Client.EmuHawk.CustomCheckBox();
            this.label35 = new BizHawk.Client.EmuHawk.HorizontalLine();
            this.checkMathBG4 = new BizHawk.Client.EmuHawk.CustomCheckBox();
            this.checkMathBG3 = new BizHawk.Client.EmuHawk.CustomCheckBox();
            this.checkMathBG2 = new BizHawk.Client.EmuHawk.CustomCheckBox();
            this.checkMathBG1 = new BizHawk.Client.EmuHawk.CustomCheckBox();
            this.label33 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.checkMathOBJ = new BizHawk.Client.EmuHawk.CustomCheckBox();
            this.lblTS = new BizHawk.WinForms.Controls.LocLabelEx();
            this.checkTSBG4 = new BizHawk.Client.EmuHawk.CustomCheckBox();
            this.checkTSBG3 = new BizHawk.Client.EmuHawk.CustomCheckBox();
            this.checkTSBG2 = new BizHawk.Client.EmuHawk.CustomCheckBox();
            this.checkTSBG1 = new BizHawk.Client.EmuHawk.CustomCheckBox();
            this.lblTM = new BizHawk.WinForms.Controls.LocLabelEx();
            this.checkTMBG4 = new BizHawk.Client.EmuHawk.CustomCheckBox();
            this.checkTMBG3 = new BizHawk.Client.EmuHawk.CustomCheckBox();
            this.checkTMBG2 = new BizHawk.Client.EmuHawk.CustomCheckBox();
            this.checkTMBG1 = new BizHawk.Client.EmuHawk.CustomCheckBox();
            this.label32 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label31 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label25 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.txtScreenCGWSEL_MathFixed = new System.Windows.Forms.TextBox();
            this.label2893719831 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.txtScreenCGWSEL_ColorSubMask = new System.Windows.Forms.TextBox();
            this.label23 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.txtScreenCGWSEL_ColorMask = new System.Windows.Forms.TextBox();
            this.label22 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label27 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.checkScreenCGWSEL_DirectColor = new System.Windows.Forms.CheckBox();
            this.label16 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.txtScreenBG4TSize = new System.Windows.Forms.TextBox();
            this.txtScreenBG3TSize = new System.Windows.Forms.TextBox();
            this.txtScreenBG2TSize = new System.Windows.Forms.TextBox();
            this.txtScreenBG1TSize = new System.Windows.Forms.TextBox();
            this.txtScreenBG4Bpp = new System.Windows.Forms.TextBox();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.txtScreenBG3Bpp = new System.Windows.Forms.TextBox();
            this.txtModeBits = new System.Windows.Forms.TextBox();
            this.txtScreenBG2Bpp = new System.Windows.Forms.TextBox();
            this.label8 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label7 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.txtScreenBG1Bpp = new System.Windows.Forms.TextBox();
            this.lblBG3 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label4 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label5 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.txtBG1Scroll = new System.Windows.Forms.TextBox();
            this.label37 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.txtBG1MapSizeBytes = new System.Windows.Forms.TextBox();
            this.txtBGPaletteInfo = new System.Windows.Forms.TextBox();
            this.rbBG4 = new System.Windows.Forms.RadioButton();
            this.rbBG3 = new System.Windows.Forms.RadioButton();
            this.rbBG2 = new System.Windows.Forms.RadioButton();
            this.rbBG1 = new System.Windows.Forms.RadioButton();
            this.txtBG1TSizeDescr = new System.Windows.Forms.TextBox();
            this.comboBGProps = new System.Windows.Forms.ComboBox();
            this.label15 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.txtBG1TSizeBits = new System.Windows.Forms.TextBox();
            this.label13 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.txtBG1Colors = new System.Windows.Forms.TextBox();
            this.txtBG1Bpp = new System.Windows.Forms.TextBox();
            this.label12 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.txtBG1TDAddrDescr = new System.Windows.Forms.TextBox();
            this.label11 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.txtBG1SCAddrDescr = new System.Windows.Forms.TextBox();
            this.label9 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.txtBG1TDAddrBits = new System.Windows.Forms.TextBox();
            this.label10 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.txtBG1SizeInPixels = new System.Windows.Forms.TextBox();
            this.label3 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.txtBG1SCAddrBits = new System.Windows.Forms.TextBox();
            this.txtBG1SizeInTiles = new System.Windows.Forms.TextBox();
            this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.txtBG1SizeBits = new System.Windows.Forms.TextBox();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.paletteViewer = new BizHawk.Client.EmuHawk.SNESGraphicsViewer();
            this.tabctrlDetails = new System.Windows.Forms.TabControl();
            this.tpPalette = new System.Windows.Forms.TabPage();
            this.label53 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label52 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label51 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.txtPaletteDetailsAddress = new System.Windows.Forms.TextBox();
            this.txtPaletteDetailsIndex = new System.Windows.Forms.TextBox();
            this.txtPaletteDetailsIndexHex = new System.Windows.Forms.TextBox();
            this.txtDetailsPaletteColorRGB = new System.Windows.Forms.TextBox();
            this.txtDetailsPaletteColorHex = new System.Windows.Forms.TextBox();
            this.txtDetailsPaletteColor = new System.Windows.Forms.TextBox();
            this.pnDetailsPaletteColor = new System.Windows.Forms.Panel();
            this.tpTile = new System.Windows.Forms.TabPage();
            this.label45 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.txtTilePalette = new System.Windows.Forms.TextBox();
            this.txtTileNumber = new System.Windows.Forms.TextBox();
            this.txtTileMode = new System.Windows.Forms.TextBox();
            this.label18 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.txtTileColors = new System.Windows.Forms.TextBox();
            this.txtTileBpp = new System.Windows.Forms.TextBox();
            this.label42 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.txtTileAddress = new System.Windows.Forms.TextBox();
            this.viewerTile = new BizHawk.Client.EmuHawk.SNESGraphicsViewer();
            this.tpMapEntry = new System.Windows.Forms.TabPage();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.checkMapEntryVFlip = new System.Windows.Forms.CheckBox();
            this.label34 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.checkMapEntryHFlip = new System.Windows.Forms.CheckBox();
            this.label17 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.lblMapEntryHFlip = new BizHawk.WinForms.Controls.LocLabelEx();
            this.txtMapEntryPalette = new System.Windows.Forms.TextBox();
            this.label14 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label6 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.txtMapEntryTileAddr = new System.Windows.Forms.TextBox();
            this.txtMapEntryPrio = new System.Windows.Forms.TextBox();
            this.txtMapEntryLocation = new System.Windows.Forms.TextBox();
            this.txtMapEntryTileNum = new System.Windows.Forms.TextBox();
            this.viewerMapEntryTile = new BizHawk.Client.EmuHawk.SNESGraphicsViewer();
            this.tpOBJ = new System.Windows.Forms.TabPage();
            this.txtObjPriority = new System.Windows.Forms.TextBox();
            this.label50 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.txtObjPaletteMemo = new System.Windows.Forms.TextBox();
            this.label49 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.txtObjPalette = new System.Windows.Forms.TextBox();
            this.label48 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.txtObjNameAddr = new System.Windows.Forms.TextBox();
            this.txtObjName = new System.Windows.Forms.TextBox();
            this.txtObjSize = new System.Windows.Forms.TextBox();
            this.label46 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.cbObjLarge = new System.Windows.Forms.CheckBox();
            this.txtObjNumber = new System.Windows.Forms.TextBox();
            this.txtObjCoord = new System.Windows.Forms.TextBox();
            this.cbObjVFlip = new System.Windows.Forms.CheckBox();
            this.label43 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.cbObjHFlip = new System.Windows.Forms.CheckBox();
            this.label44 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.viewerObj = new BizHawk.Client.EmuHawk.SNESGraphicsViewer();
            this.viewerPanel = new System.Windows.Forms.Panel();
            this.viewer = new BizHawk.Client.EmuHawk.SNESGraphicsViewer();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.messagetimer = new System.Windows.Forms.Timer(this.components);
            this.menuStrip1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.groupFreeze.SuspendLayout();
            this.pnGroupFreeze.SuspendLayout();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudScanline)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderScanline)).BeginInit();
            this.groupBox6.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox8.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.tabctrlDetails.SuspendLayout();
            this.tpPalette.SuspendLayout();
            this.tpTile.SuspendLayout();
            this.tpMapEntry.SuspendLayout();
            this.tpOBJ.SuspendLayout();
            this.viewerPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            resources.ApplyResources(this.menuStrip1, "menuStrip1");
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.toolTip1.SetToolTip(this.menuStrip1, resources.GetString("menuStrip1.ToolTip"));
            // 
            // fileToolStripMenuItem
            // 
            resources.ApplyResources(this.fileToolStripMenuItem, "fileToolStripMenuItem");
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveScreenshotAsToolStripMenuItem,
            this.saveScreenshotToClipboardToolStripMenuItem});
            // 
            // saveScreenshotAsToolStripMenuItem
            // 
            resources.ApplyResources(this.saveScreenshotAsToolStripMenuItem, "saveScreenshotAsToolStripMenuItem");
            // 
            // saveScreenshotToClipboardToolStripMenuItem
            // 
            resources.ApplyResources(this.saveScreenshotToClipboardToolStripMenuItem, "saveScreenshotToClipboardToolStripMenuItem");
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.viewerPanel, 1, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.toolTip1.SetToolTip(this.tableLayoutPanel1, resources.GetString("tableLayoutPanel1.ToolTip"));
            // 
            // panel1
            // 
            resources.ApplyResources(this.panel1, "panel1");
            this.panel1.Controls.Add(this.groupFreeze);
            this.panel1.Controls.Add(this.check2x);
            this.panel1.Controls.Add(this.comboDisplayType);
            this.panel1.Controls.Add(this.label47);
            this.panel1.Controls.Add(this.pnBackdropColor);
            this.panel1.Controls.Add(this.comboPalette);
            this.panel1.Controls.Add(this.checkBackdropColor);
            this.panel1.Controls.Add(this.groupBox3);
            this.panel1.Controls.Add(this.label24);
            this.panel1.Controls.Add(this.groupBox6);
            this.panel1.Controls.Add(this.groupBox2);
            this.panel1.Controls.Add(this.groupBox1);
            this.panel1.Controls.Add(this.groupBox5);
            this.panel1.Controls.Add(this.tabctrlDetails);
            this.panel1.Name = "panel1";
            this.toolTip1.SetToolTip(this.panel1, resources.GetString("panel1.ToolTip"));
            // 
            // groupFreeze
            // 
            resources.ApplyResources(this.groupFreeze, "groupFreeze");
            this.groupFreeze.Controls.Add(this.pnGroupFreeze);
            this.groupFreeze.Name = "groupFreeze";
            this.groupFreeze.TabStop = false;
            this.toolTip1.SetToolTip(this.groupFreeze, resources.GetString("groupFreeze.ToolTip"));
            // 
            // pnGroupFreeze
            // 
            resources.ApplyResources(this.pnGroupFreeze, "pnGroupFreeze");
            this.pnGroupFreeze.Controls.Add(this.labelMemory);
            this.pnGroupFreeze.Name = "pnGroupFreeze";
            this.toolTip1.SetToolTip(this.pnGroupFreeze, resources.GetString("pnGroupFreeze.ToolTip"));
            // 
            // labelMemory
            // 
            resources.ApplyResources(this.labelMemory, "labelMemory");
            this.labelMemory.Name = "labelMemory";
            this.toolTip1.SetToolTip(this.labelMemory, resources.GetString("labelMemory.ToolTip"));
            // 
            // check2x
            // 
            resources.ApplyResources(this.check2x, "check2x");
            this.check2x.Name = "check2x";
            this.toolTip1.SetToolTip(this.check2x, resources.GetString("check2x.ToolTip"));
            this.check2x.UseVisualStyleBackColor = true;
            this.check2x.CheckedChanged += new System.EventHandler(this.check2x_CheckedChanged);
            // 
            // comboDisplayType
            // 
            resources.ApplyResources(this.comboDisplayType, "comboDisplayType");
            this.comboDisplayType.DisplayMember = "descr";
            this.comboDisplayType.DropDownHeight = 200;
            this.comboDisplayType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboDisplayType.FormattingEnabled = true;
            this.comboDisplayType.Name = "comboDisplayType";
            this.toolTip1.SetToolTip(this.comboDisplayType, resources.GetString("comboDisplayType.ToolTip"));
            this.comboDisplayType.ValueMember = "type";
            this.comboDisplayType.SelectedIndexChanged += new System.EventHandler(this.comboDisplayType_SelectedIndexChanged);
            // 
            // label47
            // 
            resources.ApplyResources(this.label47, "label47");
            this.label47.Name = "label47";
            this.toolTip1.SetToolTip(this.label47, resources.GetString("label47.ToolTip"));
            // 
            // pnBackdropColor
            // 
            resources.ApplyResources(this.pnBackdropColor, "pnBackdropColor");
            this.pnBackdropColor.BackColor = System.Drawing.Color.Red;
            this.pnBackdropColor.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pnBackdropColor.Name = "pnBackdropColor";
            this.toolTip1.SetToolTip(this.pnBackdropColor, resources.GetString("pnBackdropColor.ToolTip"));
            this.pnBackdropColor.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.pnBackdropColor_MouseDoubleClick);
            // 
            // comboPalette
            // 
            resources.ApplyResources(this.comboPalette, "comboPalette");
            this.comboPalette.DisplayMember = "descr";
            this.comboPalette.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboPalette.FormattingEnabled = true;
            this.comboPalette.Name = "comboPalette";
            this.toolTip1.SetToolTip(this.comboPalette, resources.GetString("comboPalette.ToolTip"));
            this.comboPalette.ValueMember = "type";
            this.comboPalette.SelectedIndexChanged += new System.EventHandler(this.comboPalette_SelectedIndexChanged);
            // 
            // checkBackdropColor
            // 
            resources.ApplyResources(this.checkBackdropColor, "checkBackdropColor");
            this.checkBackdropColor.Name = "checkBackdropColor";
            this.toolTip1.SetToolTip(this.checkBackdropColor, resources.GetString("checkBackdropColor.ToolTip"));
            this.checkBackdropColor.UseVisualStyleBackColor = true;
            this.checkBackdropColor.CheckedChanged += new System.EventHandler(this.checkBackdropColor_CheckedChanged);
            // 
            // groupBox3
            // 
            resources.ApplyResources(this.groupBox3, "groupBox3");
            this.groupBox3.Controls.Add(this.checkScanlineControl);
            this.groupBox3.Controls.Add(this.label19);
            this.groupBox3.Controls.Add(this.nudScanline);
            this.groupBox3.Controls.Add(this.sliderScanline);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.TabStop = false;
            this.toolTip1.SetToolTip(this.groupBox3, resources.GetString("groupBox3.ToolTip"));
            // 
            // checkScanlineControl
            // 
            resources.ApplyResources(this.checkScanlineControl, "checkScanlineControl");
            this.checkScanlineControl.Name = "checkScanlineControl";
            this.toolTip1.SetToolTip(this.checkScanlineControl, resources.GetString("checkScanlineControl.ToolTip"));
            this.checkScanlineControl.UseVisualStyleBackColor = true;
            this.checkScanlineControl.CheckedChanged += new System.EventHandler(this.checkScanlineControl_CheckedChanged);
            // 
            // label19
            // 
            resources.ApplyResources(this.label19, "label19");
            this.label19.Name = "label19";
            this.toolTip1.SetToolTip(this.label19, resources.GetString("label19.ToolTip"));
            // 
            // nudScanline
            // 
            resources.ApplyResources(this.nudScanline, "nudScanline");
            this.nudScanline.Maximum = new decimal(new int[] {
            224,
            0,
            0,
            0});
            this.nudScanline.Name = "nudScanline";
            this.toolTip1.SetToolTip(this.nudScanline, resources.GetString("nudScanline.ToolTip"));
            this.nudScanline.ValueChanged += new System.EventHandler(this.nudScanline_ValueChanged);
            // 
            // sliderScanline
            // 
            resources.ApplyResources(this.sliderScanline, "sliderScanline");
            this.sliderScanline.Maximum = 224;
            this.sliderScanline.Name = "sliderScanline";
            this.sliderScanline.TickFrequency = 16;
            this.toolTip1.SetToolTip(this.sliderScanline, resources.GetString("sliderScanline.ToolTip"));
            this.sliderScanline.Value = 224;
            this.sliderScanline.ValueChanged += new System.EventHandler(this.sliderScanline_ValueChanged);
            // 
            // label24
            // 
            resources.ApplyResources(this.label24, "label24");
            this.label24.Name = "label24";
            this.toolTip1.SetToolTip(this.label24, resources.GetString("label24.ToolTip"));
            // 
            // groupBox6
            // 
            resources.ApplyResources(this.groupBox6, "groupBox6");
            this.groupBox6.Controls.Add(this.labelClipboard);
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.TabStop = false;
            this.toolTip1.SetToolTip(this.groupBox6, resources.GetString("groupBox6.ToolTip"));
            // 
            // labelClipboard
            // 
            resources.ApplyResources(this.labelClipboard, "labelClipboard");
            this.labelClipboard.Name = "labelClipboard";
            this.toolTip1.SetToolTip(this.labelClipboard, resources.GetString("labelClipboard.ToolTip"));
            // 
            // groupBox2
            // 
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Controls.Add(this.label26);
            this.groupBox2.Controls.Add(this.txtOBSELT1OfsBits);
            this.groupBox2.Controls.Add(this.groupBox8);
            this.groupBox2.Controls.Add(this.lblEnPrio3);
            this.groupBox2.Controls.Add(this.txtOBSELT1OfsDescr);
            this.groupBox2.Controls.Add(this.checkEN1_OBJ);
            this.groupBox2.Controls.Add(this.label30);
            this.groupBox2.Controls.Add(this.checkEN1_BG4);
            this.groupBox2.Controls.Add(this.txtOBSELBaseBits);
            this.groupBox2.Controls.Add(this.checkEN1_BG3);
            this.groupBox2.Controls.Add(this.txtOBSELBaseDescr);
            this.groupBox2.Controls.Add(this.checkEN1_BG2);
            this.groupBox2.Controls.Add(this.label29);
            this.groupBox2.Controls.Add(this.checkEN1_BG1);
            this.groupBox2.Controls.Add(this.txtOBSELSizeBits);
            this.groupBox2.Controls.Add(this.checkEN3_OBJ);
            this.groupBox2.Controls.Add(this.checkEN2_OBJ);
            this.groupBox2.Controls.Add(this.txtOBSELSizeDescr);
            this.groupBox2.Controls.Add(this.lblEnPrio2);
            this.groupBox2.Controls.Add(this.label28);
            this.groupBox2.Controls.Add(this.lblEnPrio1);
            this.groupBox2.Controls.Add(this.label20);
            this.groupBox2.Controls.Add(this.checkScreenExtbg);
            this.groupBox2.Controls.Add(this.label38);
            this.groupBox2.Controls.Add(this.label21);
            this.groupBox2.Controls.Add(this.txtScreenCGADSUB_Half);
            this.groupBox2.Controls.Add(this.checkScreenHires);
            this.groupBox2.Controls.Add(this.lblEnPrio0);
            this.groupBox2.Controls.Add(this.label18391);
            this.groupBox2.Controls.Add(this.checkEN0_OBJ);
            this.groupBox2.Controls.Add(this.checkScreenOverscan);
            this.groupBox2.Controls.Add(this.checkEN0_BG4);
            this.groupBox2.Controls.Add(this.label198129381279841);
            this.groupBox2.Controls.Add(this.checkEN0_BG3);
            this.groupBox2.Controls.Add(this.checkScreenObjInterlace);
            this.groupBox2.Controls.Add(this.checkEN0_BG2);
            this.groupBox2.Controls.Add(this.label123812831);
            this.groupBox2.Controls.Add(this.checkEN0_BG1);
            this.groupBox2.Controls.Add(this.checkScreenInterlace);
            this.groupBox2.Controls.Add(this.txtScreenCGADSUB_AddSub_Descr);
            this.groupBox2.Controls.Add(this.label2193813);
            this.groupBox2.Controls.Add(this.txtScreenCGADSUB_AddSub);
            this.groupBox2.Controls.Add(this.label36);
            this.groupBox2.Controls.Add(this.checkTMOBJ);
            this.groupBox2.Controls.Add(this.checkTSOBJ);
            this.groupBox2.Controls.Add(this.checkMathBK);
            this.groupBox2.Controls.Add(this.label35);
            this.groupBox2.Controls.Add(this.checkMathBG4);
            this.groupBox2.Controls.Add(this.checkMathBG3);
            this.groupBox2.Controls.Add(this.checkMathBG2);
            this.groupBox2.Controls.Add(this.checkMathBG1);
            this.groupBox2.Controls.Add(this.label33);
            this.groupBox2.Controls.Add(this.checkMathOBJ);
            this.groupBox2.Controls.Add(this.lblTS);
            this.groupBox2.Controls.Add(this.checkTSBG4);
            this.groupBox2.Controls.Add(this.checkTSBG3);
            this.groupBox2.Controls.Add(this.checkTSBG2);
            this.groupBox2.Controls.Add(this.checkTSBG1);
            this.groupBox2.Controls.Add(this.lblTM);
            this.groupBox2.Controls.Add(this.checkTMBG4);
            this.groupBox2.Controls.Add(this.checkTMBG3);
            this.groupBox2.Controls.Add(this.checkTMBG2);
            this.groupBox2.Controls.Add(this.checkTMBG1);
            this.groupBox2.Controls.Add(this.label32);
            this.groupBox2.Controls.Add(this.label31);
            this.groupBox2.Controls.Add(this.label25);
            this.groupBox2.Controls.Add(this.txtScreenCGWSEL_MathFixed);
            this.groupBox2.Controls.Add(this.label2893719831);
            this.groupBox2.Controls.Add(this.txtScreenCGWSEL_ColorSubMask);
            this.groupBox2.Controls.Add(this.label23);
            this.groupBox2.Controls.Add(this.txtScreenCGWSEL_ColorMask);
            this.groupBox2.Controls.Add(this.label22);
            this.groupBox2.Controls.Add(this.label27);
            this.groupBox2.Controls.Add(this.checkScreenCGWSEL_DirectColor);
            this.groupBox2.Controls.Add(this.label16);
            this.groupBox2.Controls.Add(this.txtScreenBG4TSize);
            this.groupBox2.Controls.Add(this.txtScreenBG3TSize);
            this.groupBox2.Controls.Add(this.txtScreenBG2TSize);
            this.groupBox2.Controls.Add(this.txtScreenBG1TSize);
            this.groupBox2.Controls.Add(this.txtScreenBG4Bpp);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.txtScreenBG3Bpp);
            this.groupBox2.Controls.Add(this.txtModeBits);
            this.groupBox2.Controls.Add(this.txtScreenBG2Bpp);
            this.groupBox2.Controls.Add(this.label8);
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Controls.Add(this.txtScreenBG1Bpp);
            this.groupBox2.Controls.Add(this.lblBG3);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            this.toolTip1.SetToolTip(this.groupBox2, resources.GetString("groupBox2.ToolTip"));
            // 
            // label26
            // 
            resources.ApplyResources(this.label26, "label26");
            this.label26.Name = "label26";
            this.toolTip1.SetToolTip(this.label26, resources.GetString("label26.ToolTip"));
            // 
            // txtOBSELT1OfsBits
            // 
            resources.ApplyResources(this.txtOBSELT1OfsBits, "txtOBSELT1OfsBits");
            this.txtOBSELT1OfsBits.BackColor = System.Drawing.Color.LightGreen;
            this.txtOBSELT1OfsBits.Name = "txtOBSELT1OfsBits";
            this.txtOBSELT1OfsBits.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtOBSELT1OfsBits, resources.GetString("txtOBSELT1OfsBits.ToolTip"));
            // 
            // groupBox8
            // 
            resources.ApplyResources(this.groupBox8, "groupBox8");
            this.groupBox8.Controls.Add(this.radioButton6);
            this.groupBox8.Controls.Add(this.radioButton1);
            this.groupBox8.Controls.Add(this.radioButton10);
            this.groupBox8.Controls.Add(this.radioButton15);
            this.groupBox8.Controls.Add(this.radioButton5);
            this.groupBox8.Controls.Add(this.radioButton14);
            this.groupBox8.Controls.Add(this.radioButton4);
            this.groupBox8.Controls.Add(this.radioButton3);
            this.groupBox8.Controls.Add(this.radioButton13);
            this.groupBox8.Controls.Add(this.radioButton2);
            this.groupBox8.Name = "groupBox8";
            this.groupBox8.TabStop = false;
            this.toolTip1.SetToolTip(this.groupBox8, resources.GetString("groupBox8.ToolTip"));
            // 
            // radioButton6
            // 
            resources.ApplyResources(this.radioButton6, "radioButton6");
            this.radioButton6.Name = "radioButton6";
            this.radioButton6.TabStop = true;
            this.toolTip1.SetToolTip(this.radioButton6, resources.GetString("radioButton6.ToolTip"));
            this.radioButton6.UseVisualStyleBackColor = true;
            // 
            // radioButton1
            // 
            resources.ApplyResources(this.radioButton1, "radioButton1");
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.TabStop = true;
            this.toolTip1.SetToolTip(this.radioButton1, resources.GetString("radioButton1.ToolTip"));
            this.radioButton1.UseVisualStyleBackColor = true;
            // 
            // radioButton10
            // 
            resources.ApplyResources(this.radioButton10, "radioButton10");
            this.radioButton10.Name = "radioButton10";
            this.radioButton10.TabStop = true;
            this.toolTip1.SetToolTip(this.radioButton10, resources.GetString("radioButton10.ToolTip"));
            this.radioButton10.UseVisualStyleBackColor = true;
            // 
            // radioButton15
            // 
            resources.ApplyResources(this.radioButton15, "radioButton15");
            this.radioButton15.Name = "radioButton15";
            this.radioButton15.TabStop = true;
            this.toolTip1.SetToolTip(this.radioButton15, resources.GetString("radioButton15.ToolTip"));
            this.radioButton15.UseVisualStyleBackColor = true;
            // 
            // radioButton5
            // 
            resources.ApplyResources(this.radioButton5, "radioButton5");
            this.radioButton5.Name = "radioButton5";
            this.radioButton5.TabStop = true;
            this.toolTip1.SetToolTip(this.radioButton5, resources.GetString("radioButton5.ToolTip"));
            this.radioButton5.UseVisualStyleBackColor = true;
            // 
            // radioButton14
            // 
            resources.ApplyResources(this.radioButton14, "radioButton14");
            this.radioButton14.Name = "radioButton14";
            this.radioButton14.TabStop = true;
            this.toolTip1.SetToolTip(this.radioButton14, resources.GetString("radioButton14.ToolTip"));
            this.radioButton14.UseVisualStyleBackColor = true;
            // 
            // radioButton4
            // 
            resources.ApplyResources(this.radioButton4, "radioButton4");
            this.radioButton4.Name = "radioButton4";
            this.radioButton4.TabStop = true;
            this.toolTip1.SetToolTip(this.radioButton4, resources.GetString("radioButton4.ToolTip"));
            this.radioButton4.UseVisualStyleBackColor = true;
            // 
            // radioButton3
            // 
            resources.ApplyResources(this.radioButton3, "radioButton3");
            this.radioButton3.Name = "radioButton3";
            this.radioButton3.TabStop = true;
            this.toolTip1.SetToolTip(this.radioButton3, resources.GetString("radioButton3.ToolTip"));
            this.radioButton3.UseVisualStyleBackColor = true;
            // 
            // radioButton13
            // 
            resources.ApplyResources(this.radioButton13, "radioButton13");
            this.radioButton13.Name = "radioButton13";
            this.radioButton13.TabStop = true;
            this.toolTip1.SetToolTip(this.radioButton13, resources.GetString("radioButton13.ToolTip"));
            this.radioButton13.UseVisualStyleBackColor = true;
            // 
            // radioButton2
            // 
            resources.ApplyResources(this.radioButton2, "radioButton2");
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.TabStop = true;
            this.toolTip1.SetToolTip(this.radioButton2, resources.GetString("radioButton2.ToolTip"));
            this.radioButton2.UseVisualStyleBackColor = true;
            // 
            // lblEnPrio3
            // 
            resources.ApplyResources(this.lblEnPrio3, "lblEnPrio3");
            this.lblEnPrio3.ForeColor = System.Drawing.Color.Blue;
            this.lblEnPrio3.Name = "lblEnPrio3";
            this.toolTip1.SetToolTip(this.lblEnPrio3, resources.GetString("lblEnPrio3.ToolTip"));
            this.lblEnPrio3.Click += new System.EventHandler(this.lblEnPrio3_Click);
            this.lblEnPrio3.DoubleClick += new System.EventHandler(this.lblEnPrio3_Click);
            // 
            // txtOBSELT1OfsDescr
            // 
            resources.ApplyResources(this.txtOBSELT1OfsDescr, "txtOBSELT1OfsDescr");
            this.txtOBSELT1OfsDescr.Name = "txtOBSELT1OfsDescr";
            this.txtOBSELT1OfsDescr.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtOBSELT1OfsDescr, resources.GetString("txtOBSELT1OfsDescr.ToolTip"));
            // 
            // checkEN1_OBJ
            // 
            resources.ApplyResources(this.checkEN1_OBJ, "checkEN1_OBJ");
            this.checkEN1_OBJ.Name = "checkEN1_OBJ";
            this.toolTip1.SetToolTip(this.checkEN1_OBJ, resources.GetString("checkEN1_OBJ.ToolTip"));
            this.checkEN1_OBJ.UseVisualStyleBackColor = true;
            this.checkEN1_OBJ.CheckedChanged += new System.EventHandler(this.checkEN_CheckedChanged);
            // 
            // label30
            // 
            resources.ApplyResources(this.label30, "label30");
            this.label30.Name = "label30";
            this.toolTip1.SetToolTip(this.label30, resources.GetString("label30.ToolTip"));
            // 
            // checkEN1_BG4
            // 
            resources.ApplyResources(this.checkEN1_BG4, "checkEN1_BG4");
            this.checkEN1_BG4.Name = "checkEN1_BG4";
            this.toolTip1.SetToolTip(this.checkEN1_BG4, resources.GetString("checkEN1_BG4.ToolTip"));
            this.checkEN1_BG4.UseVisualStyleBackColor = true;
            this.checkEN1_BG4.CheckedChanged += new System.EventHandler(this.checkEN_CheckedChanged);
            // 
            // txtOBSELBaseBits
            // 
            resources.ApplyResources(this.txtOBSELBaseBits, "txtOBSELBaseBits");
            this.txtOBSELBaseBits.BackColor = System.Drawing.Color.LightGreen;
            this.txtOBSELBaseBits.Name = "txtOBSELBaseBits";
            this.txtOBSELBaseBits.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtOBSELBaseBits, resources.GetString("txtOBSELBaseBits.ToolTip"));
            // 
            // checkEN1_BG3
            // 
            resources.ApplyResources(this.checkEN1_BG3, "checkEN1_BG3");
            this.checkEN1_BG3.Name = "checkEN1_BG3";
            this.toolTip1.SetToolTip(this.checkEN1_BG3, resources.GetString("checkEN1_BG3.ToolTip"));
            this.checkEN1_BG3.UseVisualStyleBackColor = true;
            this.checkEN1_BG3.CheckedChanged += new System.EventHandler(this.checkEN_CheckedChanged);
            // 
            // txtOBSELBaseDescr
            // 
            resources.ApplyResources(this.txtOBSELBaseDescr, "txtOBSELBaseDescr");
            this.txtOBSELBaseDescr.Name = "txtOBSELBaseDescr";
            this.txtOBSELBaseDescr.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtOBSELBaseDescr, resources.GetString("txtOBSELBaseDescr.ToolTip"));
            // 
            // checkEN1_BG2
            // 
            resources.ApplyResources(this.checkEN1_BG2, "checkEN1_BG2");
            this.checkEN1_BG2.Name = "checkEN1_BG2";
            this.toolTip1.SetToolTip(this.checkEN1_BG2, resources.GetString("checkEN1_BG2.ToolTip"));
            this.checkEN1_BG2.UseVisualStyleBackColor = true;
            this.checkEN1_BG2.CheckedChanged += new System.EventHandler(this.checkEN_CheckedChanged);
            // 
            // label29
            // 
            resources.ApplyResources(this.label29, "label29");
            this.label29.Name = "label29";
            this.toolTip1.SetToolTip(this.label29, resources.GetString("label29.ToolTip"));
            // 
            // checkEN1_BG1
            // 
            resources.ApplyResources(this.checkEN1_BG1, "checkEN1_BG1");
            this.checkEN1_BG1.Name = "checkEN1_BG1";
            this.toolTip1.SetToolTip(this.checkEN1_BG1, resources.GetString("checkEN1_BG1.ToolTip"));
            this.checkEN1_BG1.UseVisualStyleBackColor = true;
            this.checkEN1_BG1.CheckedChanged += new System.EventHandler(this.checkEN_CheckedChanged);
            // 
            // txtOBSELSizeBits
            // 
            resources.ApplyResources(this.txtOBSELSizeBits, "txtOBSELSizeBits");
            this.txtOBSELSizeBits.BackColor = System.Drawing.Color.LightGreen;
            this.txtOBSELSizeBits.Name = "txtOBSELSizeBits";
            this.txtOBSELSizeBits.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtOBSELSizeBits, resources.GetString("txtOBSELSizeBits.ToolTip"));
            // 
            // checkEN3_OBJ
            // 
            resources.ApplyResources(this.checkEN3_OBJ, "checkEN3_OBJ");
            this.checkEN3_OBJ.Name = "checkEN3_OBJ";
            this.toolTip1.SetToolTip(this.checkEN3_OBJ, resources.GetString("checkEN3_OBJ.ToolTip"));
            this.checkEN3_OBJ.UseVisualStyleBackColor = true;
            this.checkEN3_OBJ.CheckedChanged += new System.EventHandler(this.checkEN_CheckedChanged);
            // 
            // checkEN2_OBJ
            // 
            resources.ApplyResources(this.checkEN2_OBJ, "checkEN2_OBJ");
            this.checkEN2_OBJ.Name = "checkEN2_OBJ";
            this.toolTip1.SetToolTip(this.checkEN2_OBJ, resources.GetString("checkEN2_OBJ.ToolTip"));
            this.checkEN2_OBJ.UseVisualStyleBackColor = true;
            this.checkEN2_OBJ.CheckedChanged += new System.EventHandler(this.checkEN_CheckedChanged);
            // 
            // txtOBSELSizeDescr
            // 
            resources.ApplyResources(this.txtOBSELSizeDescr, "txtOBSELSizeDescr");
            this.txtOBSELSizeDescr.Name = "txtOBSELSizeDescr";
            this.txtOBSELSizeDescr.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtOBSELSizeDescr, resources.GetString("txtOBSELSizeDescr.ToolTip"));
            // 
            // lblEnPrio2
            // 
            resources.ApplyResources(this.lblEnPrio2, "lblEnPrio2");
            this.lblEnPrio2.ForeColor = System.Drawing.Color.Blue;
            this.lblEnPrio2.Name = "lblEnPrio2";
            this.toolTip1.SetToolTip(this.lblEnPrio2, resources.GetString("lblEnPrio2.ToolTip"));
            this.lblEnPrio2.Click += new System.EventHandler(this.lblEnPrio2_Click);
            this.lblEnPrio2.DoubleClick += new System.EventHandler(this.lblEnPrio2_Click);
            // 
            // label28
            // 
            resources.ApplyResources(this.label28, "label28");
            this.label28.Name = "label28";
            this.toolTip1.SetToolTip(this.label28, resources.GetString("label28.ToolTip"));
            // 
            // lblEnPrio1
            // 
            resources.ApplyResources(this.lblEnPrio1, "lblEnPrio1");
            this.lblEnPrio1.ForeColor = System.Drawing.Color.Blue;
            this.lblEnPrio1.Name = "lblEnPrio1";
            this.toolTip1.SetToolTip(this.lblEnPrio1, resources.GetString("lblEnPrio1.ToolTip"));
            this.lblEnPrio1.Click += new System.EventHandler(this.lblEnPrio1_Click);
            this.lblEnPrio1.DoubleClick += new System.EventHandler(this.lblEnPrio1_Click);
            // 
            // label20
            // 
            resources.ApplyResources(this.label20, "label20");
            this.label20.Name = "label20";
            this.toolTip1.SetToolTip(this.label20, resources.GetString("label20.ToolTip"));
            // 
            // checkScreenExtbg
            // 
            resources.ApplyResources(this.checkScreenExtbg, "checkScreenExtbg");
            this.checkScreenExtbg.Name = "checkScreenExtbg";
            this.toolTip1.SetToolTip(this.checkScreenExtbg, resources.GetString("checkScreenExtbg.ToolTip"));
            this.checkScreenExtbg.UseVisualStyleBackColor = true;
            // 
            // label38
            // 
            resources.ApplyResources(this.label38, "label38");
            this.label38.Name = "label38";
            this.toolTip1.SetToolTip(this.label38, resources.GetString("label38.ToolTip"));
            // 
            // label21
            // 
            resources.ApplyResources(this.label21, "label21");
            this.label21.Name = "label21";
            this.toolTip1.SetToolTip(this.label21, resources.GetString("label21.ToolTip"));
            // 
            // txtScreenCGADSUB_Half
            // 
            resources.ApplyResources(this.txtScreenCGADSUB_Half, "txtScreenCGADSUB_Half");
            this.txtScreenCGADSUB_Half.Name = "txtScreenCGADSUB_Half";
            this.toolTip1.SetToolTip(this.txtScreenCGADSUB_Half, resources.GetString("txtScreenCGADSUB_Half.ToolTip"));
            this.txtScreenCGADSUB_Half.UseVisualStyleBackColor = true;
            // 
            // checkScreenHires
            // 
            resources.ApplyResources(this.checkScreenHires, "checkScreenHires");
            this.checkScreenHires.Name = "checkScreenHires";
            this.toolTip1.SetToolTip(this.checkScreenHires, resources.GetString("checkScreenHires.ToolTip"));
            this.checkScreenHires.UseVisualStyleBackColor = true;
            // 
            // lblEnPrio0
            // 
            resources.ApplyResources(this.lblEnPrio0, "lblEnPrio0");
            this.lblEnPrio0.ForeColor = System.Drawing.Color.Blue;
            this.lblEnPrio0.Name = "lblEnPrio0";
            this.toolTip1.SetToolTip(this.lblEnPrio0, resources.GetString("lblEnPrio0.ToolTip"));
            this.lblEnPrio0.Click += new System.EventHandler(this.lblEnPrio0_Click);
            this.lblEnPrio0.DoubleClick += new System.EventHandler(this.lblEnPrio0_Click);
            // 
            // label18391
            // 
            resources.ApplyResources(this.label18391, "label18391");
            this.label18391.Name = "label18391";
            this.toolTip1.SetToolTip(this.label18391, resources.GetString("label18391.ToolTip"));
            // 
            // checkEN0_OBJ
            // 
            resources.ApplyResources(this.checkEN0_OBJ, "checkEN0_OBJ");
            this.checkEN0_OBJ.Name = "checkEN0_OBJ";
            this.toolTip1.SetToolTip(this.checkEN0_OBJ, resources.GetString("checkEN0_OBJ.ToolTip"));
            this.checkEN0_OBJ.UseVisualStyleBackColor = true;
            this.checkEN0_OBJ.CheckedChanged += new System.EventHandler(this.checkEN_CheckedChanged);
            // 
            // checkScreenOverscan
            // 
            resources.ApplyResources(this.checkScreenOverscan, "checkScreenOverscan");
            this.checkScreenOverscan.Name = "checkScreenOverscan";
            this.toolTip1.SetToolTip(this.checkScreenOverscan, resources.GetString("checkScreenOverscan.ToolTip"));
            this.checkScreenOverscan.UseVisualStyleBackColor = true;
            // 
            // checkEN0_BG4
            // 
            resources.ApplyResources(this.checkEN0_BG4, "checkEN0_BG4");
            this.checkEN0_BG4.Name = "checkEN0_BG4";
            this.toolTip1.SetToolTip(this.checkEN0_BG4, resources.GetString("checkEN0_BG4.ToolTip"));
            this.checkEN0_BG4.UseVisualStyleBackColor = true;
            this.checkEN0_BG4.CheckedChanged += new System.EventHandler(this.checkEN_CheckedChanged);
            // 
            // label198129381279841
            // 
            resources.ApplyResources(this.label198129381279841, "label198129381279841");
            this.label198129381279841.Name = "label198129381279841";
            this.toolTip1.SetToolTip(this.label198129381279841, resources.GetString("label198129381279841.ToolTip"));
            // 
            // checkEN0_BG3
            // 
            resources.ApplyResources(this.checkEN0_BG3, "checkEN0_BG3");
            this.checkEN0_BG3.Name = "checkEN0_BG3";
            this.toolTip1.SetToolTip(this.checkEN0_BG3, resources.GetString("checkEN0_BG3.ToolTip"));
            this.checkEN0_BG3.UseVisualStyleBackColor = true;
            this.checkEN0_BG3.CheckedChanged += new System.EventHandler(this.checkEN_CheckedChanged);
            // 
            // checkScreenObjInterlace
            // 
            resources.ApplyResources(this.checkScreenObjInterlace, "checkScreenObjInterlace");
            this.checkScreenObjInterlace.Name = "checkScreenObjInterlace";
            this.toolTip1.SetToolTip(this.checkScreenObjInterlace, resources.GetString("checkScreenObjInterlace.ToolTip"));
            this.checkScreenObjInterlace.UseVisualStyleBackColor = true;
            // 
            // checkEN0_BG2
            // 
            resources.ApplyResources(this.checkEN0_BG2, "checkEN0_BG2");
            this.checkEN0_BG2.Name = "checkEN0_BG2";
            this.toolTip1.SetToolTip(this.checkEN0_BG2, resources.GetString("checkEN0_BG2.ToolTip"));
            this.checkEN0_BG2.UseVisualStyleBackColor = true;
            this.checkEN0_BG2.CheckedChanged += new System.EventHandler(this.checkEN_CheckedChanged);
            // 
            // label123812831
            // 
            resources.ApplyResources(this.label123812831, "label123812831");
            this.label123812831.Name = "label123812831";
            this.toolTip1.SetToolTip(this.label123812831, resources.GetString("label123812831.ToolTip"));
            // 
            // checkEN0_BG1
            // 
            resources.ApplyResources(this.checkEN0_BG1, "checkEN0_BG1");
            this.checkEN0_BG1.Name = "checkEN0_BG1";
            this.toolTip1.SetToolTip(this.checkEN0_BG1, resources.GetString("checkEN0_BG1.ToolTip"));
            this.checkEN0_BG1.UseVisualStyleBackColor = true;
            this.checkEN0_BG1.CheckedChanged += new System.EventHandler(this.checkEN_CheckedChanged);
            // 
            // checkScreenInterlace
            // 
            resources.ApplyResources(this.checkScreenInterlace, "checkScreenInterlace");
            this.checkScreenInterlace.Name = "checkScreenInterlace";
            this.toolTip1.SetToolTip(this.checkScreenInterlace, resources.GetString("checkScreenInterlace.ToolTip"));
            this.checkScreenInterlace.UseVisualStyleBackColor = true;
            // 
            // txtScreenCGADSUB_AddSub_Descr
            // 
            resources.ApplyResources(this.txtScreenCGADSUB_AddSub_Descr, "txtScreenCGADSUB_AddSub_Descr");
            this.txtScreenCGADSUB_AddSub_Descr.Name = "txtScreenCGADSUB_AddSub_Descr";
            this.txtScreenCGADSUB_AddSub_Descr.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtScreenCGADSUB_AddSub_Descr, resources.GetString("txtScreenCGADSUB_AddSub_Descr.ToolTip"));
            // 
            // label2193813
            // 
            resources.ApplyResources(this.label2193813, "label2193813");
            this.label2193813.Name = "label2193813";
            this.toolTip1.SetToolTip(this.label2193813, resources.GetString("label2193813.ToolTip"));
            // 
            // txtScreenCGADSUB_AddSub
            // 
            resources.ApplyResources(this.txtScreenCGADSUB_AddSub, "txtScreenCGADSUB_AddSub");
            this.txtScreenCGADSUB_AddSub.BackColor = System.Drawing.Color.LightGreen;
            this.txtScreenCGADSUB_AddSub.Name = "txtScreenCGADSUB_AddSub";
            this.txtScreenCGADSUB_AddSub.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtScreenCGADSUB_AddSub, resources.GetString("txtScreenCGADSUB_AddSub.ToolTip"));
            // 
            // label36
            // 
            resources.ApplyResources(this.label36, "label36");
            this.label36.Name = "label36";
            this.toolTip1.SetToolTip(this.label36, resources.GetString("label36.ToolTip"));
            // 
            // checkTMOBJ
            // 
            resources.ApplyResources(this.checkTMOBJ, "checkTMOBJ");
            this.checkTMOBJ.CheckBackColor = System.Drawing.SystemColors.Control;
            this.checkTMOBJ.ForceChecked = null;
            this.checkTMOBJ.Name = "checkTMOBJ";
            this.toolTip1.SetToolTip(this.checkTMOBJ, resources.GetString("checkTMOBJ.ToolTip"));
            this.checkTMOBJ.UseVisualStyleBackColor = true;
            // 
            // checkTSOBJ
            // 
            resources.ApplyResources(this.checkTSOBJ, "checkTSOBJ");
            this.checkTSOBJ.CheckBackColor = System.Drawing.SystemColors.Control;
            this.checkTSOBJ.ForceChecked = null;
            this.checkTSOBJ.Name = "checkTSOBJ";
            this.toolTip1.SetToolTip(this.checkTSOBJ, resources.GetString("checkTSOBJ.ToolTip"));
            this.checkTSOBJ.UseVisualStyleBackColor = true;
            // 
            // checkMathBK
            // 
            resources.ApplyResources(this.checkMathBK, "checkMathBK");
            this.checkMathBK.CheckBackColor = System.Drawing.SystemColors.Control;
            this.checkMathBK.ForceChecked = null;
            this.checkMathBK.Name = "checkMathBK";
            this.toolTip1.SetToolTip(this.checkMathBK, resources.GetString("checkMathBK.ToolTip"));
            this.checkMathBK.UseVisualStyleBackColor = true;
            // 
            // label35
            // 
            resources.ApplyResources(this.label35, "label35");
            this.label35.Name = "label35";
            this.toolTip1.SetToolTip(this.label35, resources.GetString("label35.ToolTip"));
            // 
            // checkMathBG4
            // 
            resources.ApplyResources(this.checkMathBG4, "checkMathBG4");
            this.checkMathBG4.CheckBackColor = System.Drawing.SystemColors.Control;
            this.checkMathBG4.ForceChecked = null;
            this.checkMathBG4.Name = "checkMathBG4";
            this.toolTip1.SetToolTip(this.checkMathBG4, resources.GetString("checkMathBG4.ToolTip"));
            this.checkMathBG4.UseVisualStyleBackColor = true;
            // 
            // checkMathBG3
            // 
            resources.ApplyResources(this.checkMathBG3, "checkMathBG3");
            this.checkMathBG3.CheckBackColor = System.Drawing.SystemColors.Control;
            this.checkMathBG3.ForceChecked = null;
            this.checkMathBG3.Name = "checkMathBG3";
            this.toolTip1.SetToolTip(this.checkMathBG3, resources.GetString("checkMathBG3.ToolTip"));
            this.checkMathBG3.UseVisualStyleBackColor = true;
            // 
            // checkMathBG2
            // 
            resources.ApplyResources(this.checkMathBG2, "checkMathBG2");
            this.checkMathBG2.CheckBackColor = System.Drawing.SystemColors.Control;
            this.checkMathBG2.ForceChecked = null;
            this.checkMathBG2.Name = "checkMathBG2";
            this.toolTip1.SetToolTip(this.checkMathBG2, resources.GetString("checkMathBG2.ToolTip"));
            this.checkMathBG2.UseVisualStyleBackColor = true;
            // 
            // checkMathBG1
            // 
            resources.ApplyResources(this.checkMathBG1, "checkMathBG1");
            this.checkMathBG1.CheckBackColor = System.Drawing.SystemColors.Control;
            this.checkMathBG1.ForceChecked = null;
            this.checkMathBG1.Name = "checkMathBG1";
            this.toolTip1.SetToolTip(this.checkMathBG1, resources.GetString("checkMathBG1.ToolTip"));
            this.checkMathBG1.UseVisualStyleBackColor = true;
            // 
            // label33
            // 
            resources.ApplyResources(this.label33, "label33");
            this.label33.Name = "label33";
            this.toolTip1.SetToolTip(this.label33, resources.GetString("label33.ToolTip"));
            // 
            // checkMathOBJ
            // 
            resources.ApplyResources(this.checkMathOBJ, "checkMathOBJ");
            this.checkMathOBJ.CheckBackColor = System.Drawing.SystemColors.Control;
            this.checkMathOBJ.ForceChecked = null;
            this.checkMathOBJ.Name = "checkMathOBJ";
            this.toolTip1.SetToolTip(this.checkMathOBJ, resources.GetString("checkMathOBJ.ToolTip"));
            this.checkMathOBJ.UseVisualStyleBackColor = true;
            // 
            // lblTS
            // 
            resources.ApplyResources(this.lblTS, "lblTS");
            this.lblTS.Name = "lblTS";
            this.toolTip1.SetToolTip(this.lblTS, resources.GetString("lblTS.ToolTip"));
            // 
            // checkTSBG4
            // 
            resources.ApplyResources(this.checkTSBG4, "checkTSBG4");
            this.checkTSBG4.CheckBackColor = System.Drawing.SystemColors.Control;
            this.checkTSBG4.ForceChecked = null;
            this.checkTSBG4.Name = "checkTSBG4";
            this.toolTip1.SetToolTip(this.checkTSBG4, resources.GetString("checkTSBG4.ToolTip"));
            this.checkTSBG4.UseVisualStyleBackColor = true;
            // 
            // checkTSBG3
            // 
            resources.ApplyResources(this.checkTSBG3, "checkTSBG3");
            this.checkTSBG3.CheckBackColor = System.Drawing.SystemColors.Control;
            this.checkTSBG3.ForceChecked = null;
            this.checkTSBG3.Name = "checkTSBG3";
            this.toolTip1.SetToolTip(this.checkTSBG3, resources.GetString("checkTSBG3.ToolTip"));
            this.checkTSBG3.UseVisualStyleBackColor = true;
            // 
            // checkTSBG2
            // 
            resources.ApplyResources(this.checkTSBG2, "checkTSBG2");
            this.checkTSBG2.CheckBackColor = System.Drawing.SystemColors.Control;
            this.checkTSBG2.ForceChecked = null;
            this.checkTSBG2.Name = "checkTSBG2";
            this.toolTip1.SetToolTip(this.checkTSBG2, resources.GetString("checkTSBG2.ToolTip"));
            this.checkTSBG2.UseVisualStyleBackColor = true;
            // 
            // checkTSBG1
            // 
            resources.ApplyResources(this.checkTSBG1, "checkTSBG1");
            this.checkTSBG1.CheckBackColor = System.Drawing.SystemColors.Control;
            this.checkTSBG1.ForceChecked = null;
            this.checkTSBG1.Name = "checkTSBG1";
            this.toolTip1.SetToolTip(this.checkTSBG1, resources.GetString("checkTSBG1.ToolTip"));
            this.checkTSBG1.UseVisualStyleBackColor = true;
            // 
            // lblTM
            // 
            resources.ApplyResources(this.lblTM, "lblTM");
            this.lblTM.Name = "lblTM";
            this.toolTip1.SetToolTip(this.lblTM, resources.GetString("lblTM.ToolTip"));
            // 
            // checkTMBG4
            // 
            resources.ApplyResources(this.checkTMBG4, "checkTMBG4");
            this.checkTMBG4.CheckBackColor = System.Drawing.SystemColors.Control;
            this.checkTMBG4.ForceChecked = null;
            this.checkTMBG4.Name = "checkTMBG4";
            this.toolTip1.SetToolTip(this.checkTMBG4, resources.GetString("checkTMBG4.ToolTip"));
            this.checkTMBG4.UseVisualStyleBackColor = true;
            // 
            // checkTMBG3
            // 
            resources.ApplyResources(this.checkTMBG3, "checkTMBG3");
            this.checkTMBG3.CheckBackColor = System.Drawing.SystemColors.Control;
            this.checkTMBG3.ForceChecked = null;
            this.checkTMBG3.Name = "checkTMBG3";
            this.toolTip1.SetToolTip(this.checkTMBG3, resources.GetString("checkTMBG3.ToolTip"));
            this.checkTMBG3.UseVisualStyleBackColor = true;
            // 
            // checkTMBG2
            // 
            resources.ApplyResources(this.checkTMBG2, "checkTMBG2");
            this.checkTMBG2.CheckBackColor = System.Drawing.SystemColors.Control;
            this.checkTMBG2.ForceChecked = null;
            this.checkTMBG2.Name = "checkTMBG2";
            this.toolTip1.SetToolTip(this.checkTMBG2, resources.GetString("checkTMBG2.ToolTip"));
            this.checkTMBG2.UseVisualStyleBackColor = true;
            // 
            // checkTMBG1
            // 
            resources.ApplyResources(this.checkTMBG1, "checkTMBG1");
            this.checkTMBG1.CheckBackColor = System.Drawing.SystemColors.Control;
            this.checkTMBG1.ForceChecked = null;
            this.checkTMBG1.Name = "checkTMBG1";
            this.toolTip1.SetToolTip(this.checkTMBG1, resources.GetString("checkTMBG1.ToolTip"));
            this.checkTMBG1.UseVisualStyleBackColor = true;
            // 
            // label32
            // 
            resources.ApplyResources(this.label32, "label32");
            this.label32.Name = "label32";
            this.toolTip1.SetToolTip(this.label32, resources.GetString("label32.ToolTip"));
            // 
            // label31
            // 
            resources.ApplyResources(this.label31, "label31");
            this.label31.Name = "label31";
            this.toolTip1.SetToolTip(this.label31, resources.GetString("label31.ToolTip"));
            // 
            // label25
            // 
            resources.ApplyResources(this.label25, "label25");
            this.label25.Name = "label25";
            this.toolTip1.SetToolTip(this.label25, resources.GetString("label25.ToolTip"));
            // 
            // txtScreenCGWSEL_MathFixed
            // 
            resources.ApplyResources(this.txtScreenCGWSEL_MathFixed, "txtScreenCGWSEL_MathFixed");
            this.txtScreenCGWSEL_MathFixed.BackColor = System.Drawing.Color.LightGreen;
            this.txtScreenCGWSEL_MathFixed.Name = "txtScreenCGWSEL_MathFixed";
            this.txtScreenCGWSEL_MathFixed.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtScreenCGWSEL_MathFixed, resources.GetString("txtScreenCGWSEL_MathFixed.ToolTip"));
            // 
            // label2893719831
            // 
            resources.ApplyResources(this.label2893719831, "label2893719831");
            this.label2893719831.Name = "label2893719831";
            this.toolTip1.SetToolTip(this.label2893719831, resources.GetString("label2893719831.ToolTip"));
            // 
            // txtScreenCGWSEL_ColorSubMask
            // 
            resources.ApplyResources(this.txtScreenCGWSEL_ColorSubMask, "txtScreenCGWSEL_ColorSubMask");
            this.txtScreenCGWSEL_ColorSubMask.BackColor = System.Drawing.Color.LightGreen;
            this.txtScreenCGWSEL_ColorSubMask.Name = "txtScreenCGWSEL_ColorSubMask";
            this.txtScreenCGWSEL_ColorSubMask.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtScreenCGWSEL_ColorSubMask, resources.GetString("txtScreenCGWSEL_ColorSubMask.ToolTip"));
            // 
            // label23
            // 
            resources.ApplyResources(this.label23, "label23");
            this.label23.Name = "label23";
            this.toolTip1.SetToolTip(this.label23, resources.GetString("label23.ToolTip"));
            // 
            // txtScreenCGWSEL_ColorMask
            // 
            resources.ApplyResources(this.txtScreenCGWSEL_ColorMask, "txtScreenCGWSEL_ColorMask");
            this.txtScreenCGWSEL_ColorMask.BackColor = System.Drawing.Color.LightGreen;
            this.txtScreenCGWSEL_ColorMask.Name = "txtScreenCGWSEL_ColorMask";
            this.txtScreenCGWSEL_ColorMask.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtScreenCGWSEL_ColorMask, resources.GetString("txtScreenCGWSEL_ColorMask.ToolTip"));
            // 
            // label22
            // 
            resources.ApplyResources(this.label22, "label22");
            this.label22.Name = "label22";
            this.toolTip1.SetToolTip(this.label22, resources.GetString("label22.ToolTip"));
            // 
            // label27
            // 
            resources.ApplyResources(this.label27, "label27");
            this.label27.Name = "label27";
            this.toolTip1.SetToolTip(this.label27, resources.GetString("label27.ToolTip"));
            // 
            // checkScreenCGWSEL_DirectColor
            // 
            resources.ApplyResources(this.checkScreenCGWSEL_DirectColor, "checkScreenCGWSEL_DirectColor");
            this.checkScreenCGWSEL_DirectColor.Name = "checkScreenCGWSEL_DirectColor";
            this.toolTip1.SetToolTip(this.checkScreenCGWSEL_DirectColor, resources.GetString("checkScreenCGWSEL_DirectColor.ToolTip"));
            this.checkScreenCGWSEL_DirectColor.UseVisualStyleBackColor = true;
            // 
            // label16
            // 
            resources.ApplyResources(this.label16, "label16");
            this.label16.Name = "label16";
            this.toolTip1.SetToolTip(this.label16, resources.GetString("label16.ToolTip"));
            // 
            // txtScreenBG4TSize
            // 
            resources.ApplyResources(this.txtScreenBG4TSize, "txtScreenBG4TSize");
            this.txtScreenBG4TSize.Name = "txtScreenBG4TSize";
            this.txtScreenBG4TSize.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtScreenBG4TSize, resources.GetString("txtScreenBG4TSize.ToolTip"));
            // 
            // txtScreenBG3TSize
            // 
            resources.ApplyResources(this.txtScreenBG3TSize, "txtScreenBG3TSize");
            this.txtScreenBG3TSize.Name = "txtScreenBG3TSize";
            this.txtScreenBG3TSize.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtScreenBG3TSize, resources.GetString("txtScreenBG3TSize.ToolTip"));
            // 
            // txtScreenBG2TSize
            // 
            resources.ApplyResources(this.txtScreenBG2TSize, "txtScreenBG2TSize");
            this.txtScreenBG2TSize.Name = "txtScreenBG2TSize";
            this.txtScreenBG2TSize.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtScreenBG2TSize, resources.GetString("txtScreenBG2TSize.ToolTip"));
            // 
            // txtScreenBG1TSize
            // 
            resources.ApplyResources(this.txtScreenBG1TSize, "txtScreenBG1TSize");
            this.txtScreenBG1TSize.Name = "txtScreenBG1TSize";
            this.txtScreenBG1TSize.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtScreenBG1TSize, resources.GetString("txtScreenBG1TSize.ToolTip"));
            // 
            // txtScreenBG4Bpp
            // 
            resources.ApplyResources(this.txtScreenBG4Bpp, "txtScreenBG4Bpp");
            this.txtScreenBG4Bpp.Name = "txtScreenBG4Bpp";
            this.txtScreenBG4Bpp.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtScreenBG4Bpp, resources.GetString("txtScreenBG4Bpp.ToolTip"));
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            this.toolTip1.SetToolTip(this.label1, resources.GetString("label1.ToolTip"));
            // 
            // txtScreenBG3Bpp
            // 
            resources.ApplyResources(this.txtScreenBG3Bpp, "txtScreenBG3Bpp");
            this.txtScreenBG3Bpp.Name = "txtScreenBG3Bpp";
            this.txtScreenBG3Bpp.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtScreenBG3Bpp, resources.GetString("txtScreenBG3Bpp.ToolTip"));
            // 
            // txtModeBits
            // 
            resources.ApplyResources(this.txtModeBits, "txtModeBits");
            this.txtModeBits.BackColor = System.Drawing.Color.LightGreen;
            this.txtModeBits.Name = "txtModeBits";
            this.txtModeBits.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtModeBits, resources.GetString("txtModeBits.ToolTip"));
            // 
            // txtScreenBG2Bpp
            // 
            resources.ApplyResources(this.txtScreenBG2Bpp, "txtScreenBG2Bpp");
            this.txtScreenBG2Bpp.Name = "txtScreenBG2Bpp";
            this.txtScreenBG2Bpp.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtScreenBG2Bpp, resources.GetString("txtScreenBG2Bpp.ToolTip"));
            // 
            // label8
            // 
            resources.ApplyResources(this.label8, "label8");
            this.label8.Name = "label8";
            this.toolTip1.SetToolTip(this.label8, resources.GetString("label8.ToolTip"));
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            this.toolTip1.SetToolTip(this.label7, resources.GetString("label7.ToolTip"));
            // 
            // txtScreenBG1Bpp
            // 
            resources.ApplyResources(this.txtScreenBG1Bpp, "txtScreenBG1Bpp");
            this.txtScreenBG1Bpp.Name = "txtScreenBG1Bpp";
            this.txtScreenBG1Bpp.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtScreenBG1Bpp, resources.GetString("txtScreenBG1Bpp.ToolTip"));
            // 
            // lblBG3
            // 
            resources.ApplyResources(this.lblBG3, "lblBG3");
            this.lblBG3.Name = "lblBG3";
            this.toolTip1.SetToolTip(this.lblBG3, resources.GetString("lblBG3.ToolTip"));
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            this.toolTip1.SetToolTip(this.label4, resources.GetString("label4.ToolTip"));
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            this.toolTip1.SetToolTip(this.label5, resources.GetString("label5.ToolTip"));
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.txtBG1Scroll);
            this.groupBox1.Controls.Add(this.label37);
            this.groupBox1.Controls.Add(this.txtBG1MapSizeBytes);
            this.groupBox1.Controls.Add(this.txtBGPaletteInfo);
            this.groupBox1.Controls.Add(this.rbBG4);
            this.groupBox1.Controls.Add(this.rbBG3);
            this.groupBox1.Controls.Add(this.rbBG2);
            this.groupBox1.Controls.Add(this.rbBG1);
            this.groupBox1.Controls.Add(this.txtBG1TSizeDescr);
            this.groupBox1.Controls.Add(this.comboBGProps);
            this.groupBox1.Controls.Add(this.label15);
            this.groupBox1.Controls.Add(this.txtBG1TSizeBits);
            this.groupBox1.Controls.Add(this.label13);
            this.groupBox1.Controls.Add(this.txtBG1Colors);
            this.groupBox1.Controls.Add(this.txtBG1Bpp);
            this.groupBox1.Controls.Add(this.label12);
            this.groupBox1.Controls.Add(this.txtBG1TDAddrDescr);
            this.groupBox1.Controls.Add(this.label11);
            this.groupBox1.Controls.Add(this.txtBG1SCAddrDescr);
            this.groupBox1.Controls.Add(this.label9);
            this.groupBox1.Controls.Add(this.txtBG1TDAddrBits);
            this.groupBox1.Controls.Add(this.label10);
            this.groupBox1.Controls.Add(this.txtBG1SizeInPixels);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.txtBG1SCAddrBits);
            this.groupBox1.Controls.Add(this.txtBG1SizeInTiles);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.txtBG1SizeBits);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            this.toolTip1.SetToolTip(this.groupBox1, resources.GetString("groupBox1.ToolTip"));
            // 
            // txtBG1Scroll
            // 
            resources.ApplyResources(this.txtBG1Scroll, "txtBG1Scroll");
            this.txtBG1Scroll.Name = "txtBG1Scroll";
            this.txtBG1Scroll.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtBG1Scroll, resources.GetString("txtBG1Scroll.ToolTip"));
            // 
            // label37
            // 
            resources.ApplyResources(this.label37, "label37");
            this.label37.Name = "label37";
            this.toolTip1.SetToolTip(this.label37, resources.GetString("label37.ToolTip"));
            // 
            // txtBG1MapSizeBytes
            // 
            resources.ApplyResources(this.txtBG1MapSizeBytes, "txtBG1MapSizeBytes");
            this.txtBG1MapSizeBytes.Name = "txtBG1MapSizeBytes";
            this.txtBG1MapSizeBytes.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtBG1MapSizeBytes, resources.GetString("txtBG1MapSizeBytes.ToolTip"));
            // 
            // txtBGPaletteInfo
            // 
            resources.ApplyResources(this.txtBGPaletteInfo, "txtBGPaletteInfo");
            this.txtBGPaletteInfo.Name = "txtBGPaletteInfo";
            this.txtBGPaletteInfo.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtBGPaletteInfo, resources.GetString("txtBGPaletteInfo.ToolTip"));
            // 
            // rbBG4
            // 
            resources.ApplyResources(this.rbBG4, "rbBG4");
            this.rbBG4.Name = "rbBG4";
            this.rbBG4.TabStop = true;
            this.toolTip1.SetToolTip(this.rbBG4, resources.GetString("rbBG4.ToolTip"));
            this.rbBG4.UseVisualStyleBackColor = true;
            this.rbBG4.CheckedChanged += new System.EventHandler(this.rbBGX_CheckedChanged);
            // 
            // rbBG3
            // 
            resources.ApplyResources(this.rbBG3, "rbBG3");
            this.rbBG3.Name = "rbBG3";
            this.rbBG3.TabStop = true;
            this.toolTip1.SetToolTip(this.rbBG3, resources.GetString("rbBG3.ToolTip"));
            this.rbBG3.UseVisualStyleBackColor = true;
            this.rbBG3.CheckedChanged += new System.EventHandler(this.rbBGX_CheckedChanged);
            // 
            // rbBG2
            // 
            resources.ApplyResources(this.rbBG2, "rbBG2");
            this.rbBG2.Name = "rbBG2";
            this.rbBG2.TabStop = true;
            this.toolTip1.SetToolTip(this.rbBG2, resources.GetString("rbBG2.ToolTip"));
            this.rbBG2.UseVisualStyleBackColor = true;
            this.rbBG2.CheckedChanged += new System.EventHandler(this.rbBGX_CheckedChanged);
            // 
            // rbBG1
            // 
            resources.ApplyResources(this.rbBG1, "rbBG1");
            this.rbBG1.Name = "rbBG1";
            this.rbBG1.TabStop = true;
            this.toolTip1.SetToolTip(this.rbBG1, resources.GetString("rbBG1.ToolTip"));
            this.rbBG1.UseVisualStyleBackColor = true;
            this.rbBG1.CheckedChanged += new System.EventHandler(this.rbBGX_CheckedChanged);
            // 
            // txtBG1TSizeDescr
            // 
            resources.ApplyResources(this.txtBG1TSizeDescr, "txtBG1TSizeDescr");
            this.txtBG1TSizeDescr.Name = "txtBG1TSizeDescr";
            this.txtBG1TSizeDescr.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtBG1TSizeDescr, resources.GetString("txtBG1TSizeDescr.ToolTip"));
            // 
            // comboBGProps
            // 
            resources.ApplyResources(this.comboBGProps, "comboBGProps");
            this.comboBGProps.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBGProps.FormattingEnabled = true;
            this.comboBGProps.Items.AddRange(new object[] {
            resources.GetString("comboBGProps.Items"),
            resources.GetString("comboBGProps.Items1"),
            resources.GetString("comboBGProps.Items2"),
            resources.GetString("comboBGProps.Items3")});
            this.comboBGProps.Name = "comboBGProps";
            this.toolTip1.SetToolTip(this.comboBGProps, resources.GetString("comboBGProps.ToolTip"));
            this.comboBGProps.SelectedIndexChanged += new System.EventHandler(this.comboBGProps_SelectedIndexChanged);
            // 
            // label15
            // 
            resources.ApplyResources(this.label15, "label15");
            this.label15.Name = "label15";
            this.toolTip1.SetToolTip(this.label15, resources.GetString("label15.ToolTip"));
            // 
            // txtBG1TSizeBits
            // 
            resources.ApplyResources(this.txtBG1TSizeBits, "txtBG1TSizeBits");
            this.txtBG1TSizeBits.BackColor = System.Drawing.Color.LightGreen;
            this.txtBG1TSizeBits.Name = "txtBG1TSizeBits";
            this.txtBG1TSizeBits.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtBG1TSizeBits, resources.GetString("txtBG1TSizeBits.ToolTip"));
            // 
            // label13
            // 
            resources.ApplyResources(this.label13, "label13");
            this.label13.Name = "label13";
            this.toolTip1.SetToolTip(this.label13, resources.GetString("label13.ToolTip"));
            // 
            // txtBG1Colors
            // 
            resources.ApplyResources(this.txtBG1Colors, "txtBG1Colors");
            this.txtBG1Colors.Name = "txtBG1Colors";
            this.txtBG1Colors.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtBG1Colors, resources.GetString("txtBG1Colors.ToolTip"));
            // 
            // txtBG1Bpp
            // 
            resources.ApplyResources(this.txtBG1Bpp, "txtBG1Bpp");
            this.txtBG1Bpp.Name = "txtBG1Bpp";
            this.txtBG1Bpp.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtBG1Bpp, resources.GetString("txtBG1Bpp.ToolTip"));
            // 
            // label12
            // 
            resources.ApplyResources(this.label12, "label12");
            this.label12.Name = "label12";
            this.toolTip1.SetToolTip(this.label12, resources.GetString("label12.ToolTip"));
            // 
            // txtBG1TDAddrDescr
            // 
            resources.ApplyResources(this.txtBG1TDAddrDescr, "txtBG1TDAddrDescr");
            this.txtBG1TDAddrDescr.Name = "txtBG1TDAddrDescr";
            this.txtBG1TDAddrDescr.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtBG1TDAddrDescr, resources.GetString("txtBG1TDAddrDescr.ToolTip"));
            // 
            // label11
            // 
            resources.ApplyResources(this.label11, "label11");
            this.label11.Name = "label11";
            this.toolTip1.SetToolTip(this.label11, resources.GetString("label11.ToolTip"));
            // 
            // txtBG1SCAddrDescr
            // 
            resources.ApplyResources(this.txtBG1SCAddrDescr, "txtBG1SCAddrDescr");
            this.txtBG1SCAddrDescr.Name = "txtBG1SCAddrDescr";
            this.txtBG1SCAddrDescr.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtBG1SCAddrDescr, resources.GetString("txtBG1SCAddrDescr.ToolTip"));
            // 
            // label9
            // 
            resources.ApplyResources(this.label9, "label9");
            this.label9.Name = "label9";
            this.toolTip1.SetToolTip(this.label9, resources.GetString("label9.ToolTip"));
            // 
            // txtBG1TDAddrBits
            // 
            resources.ApplyResources(this.txtBG1TDAddrBits, "txtBG1TDAddrBits");
            this.txtBG1TDAddrBits.BackColor = System.Drawing.Color.LightGreen;
            this.txtBG1TDAddrBits.Name = "txtBG1TDAddrBits";
            this.txtBG1TDAddrBits.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtBG1TDAddrBits, resources.GetString("txtBG1TDAddrBits.ToolTip"));
            // 
            // label10
            // 
            resources.ApplyResources(this.label10, "label10");
            this.label10.Name = "label10";
            this.toolTip1.SetToolTip(this.label10, resources.GetString("label10.ToolTip"));
            // 
            // txtBG1SizeInPixels
            // 
            resources.ApplyResources(this.txtBG1SizeInPixels, "txtBG1SizeInPixels");
            this.txtBG1SizeInPixels.Name = "txtBG1SizeInPixels";
            this.txtBG1SizeInPixels.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtBG1SizeInPixels, resources.GetString("txtBG1SizeInPixels.ToolTip"));
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            this.toolTip1.SetToolTip(this.label3, resources.GetString("label3.ToolTip"));
            // 
            // txtBG1SCAddrBits
            // 
            resources.ApplyResources(this.txtBG1SCAddrBits, "txtBG1SCAddrBits");
            this.txtBG1SCAddrBits.BackColor = System.Drawing.Color.LightGreen;
            this.txtBG1SCAddrBits.Name = "txtBG1SCAddrBits";
            this.txtBG1SCAddrBits.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtBG1SCAddrBits, resources.GetString("txtBG1SCAddrBits.ToolTip"));
            // 
            // txtBG1SizeInTiles
            // 
            resources.ApplyResources(this.txtBG1SizeInTiles, "txtBG1SizeInTiles");
            this.txtBG1SizeInTiles.Name = "txtBG1SizeInTiles";
            this.txtBG1SizeInTiles.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtBG1SizeInTiles, resources.GetString("txtBG1SizeInTiles.ToolTip"));
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            this.toolTip1.SetToolTip(this.label2, resources.GetString("label2.ToolTip"));
            // 
            // txtBG1SizeBits
            // 
            resources.ApplyResources(this.txtBG1SizeBits, "txtBG1SizeBits");
            this.txtBG1SizeBits.BackColor = System.Drawing.Color.LightGreen;
            this.txtBG1SizeBits.Name = "txtBG1SizeBits";
            this.txtBG1SizeBits.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtBG1SizeBits, resources.GetString("txtBG1SizeBits.ToolTip"));
            // 
            // groupBox5
            // 
            resources.ApplyResources(this.groupBox5, "groupBox5");
            this.groupBox5.Controls.Add(this.paletteViewer);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.TabStop = false;
            this.toolTip1.SetToolTip(this.groupBox5, resources.GetString("groupBox5.ToolTip"));
            // 
            // paletteViewer
            // 
            resources.ApplyResources(this.paletteViewer, "paletteViewer");
            this.paletteViewer.BackColor = System.Drawing.Color.Transparent;
            this.paletteViewer.Name = "paletteViewer";
            this.paletteViewer.TabStop = false;
            this.toolTip1.SetToolTip(this.paletteViewer, resources.GetString("paletteViewer.ToolTip"));
            this.paletteViewer.MouseClick += new System.Windows.Forms.MouseEventHandler(this.paletteViewer_MouseClick);
            this.paletteViewer.MouseDown += new System.Windows.Forms.MouseEventHandler(this.paletteViewer_MouseDown);
            this.paletteViewer.MouseMove += new System.Windows.Forms.MouseEventHandler(this.paletteViewer_MouseMove);
            // 
            // tabctrlDetails
            // 
            resources.ApplyResources(this.tabctrlDetails, "tabctrlDetails");
            this.tabctrlDetails.Controls.Add(this.tpPalette);
            this.tabctrlDetails.Controls.Add(this.tpTile);
            this.tabctrlDetails.Controls.Add(this.tpMapEntry);
            this.tabctrlDetails.Controls.Add(this.tpOBJ);
            this.tabctrlDetails.Name = "tabctrlDetails";
            this.tabctrlDetails.SelectedIndex = 0;
            this.toolTip1.SetToolTip(this.tabctrlDetails, resources.GetString("tabctrlDetails.ToolTip"));
            // 
            // tpPalette
            // 
            resources.ApplyResources(this.tpPalette, "tpPalette");
            this.tpPalette.Controls.Add(this.label53);
            this.tpPalette.Controls.Add(this.label52);
            this.tpPalette.Controls.Add(this.label51);
            this.tpPalette.Controls.Add(this.txtPaletteDetailsAddress);
            this.tpPalette.Controls.Add(this.txtPaletteDetailsIndex);
            this.tpPalette.Controls.Add(this.txtPaletteDetailsIndexHex);
            this.tpPalette.Controls.Add(this.txtDetailsPaletteColorRGB);
            this.tpPalette.Controls.Add(this.txtDetailsPaletteColorHex);
            this.tpPalette.Controls.Add(this.txtDetailsPaletteColor);
            this.tpPalette.Controls.Add(this.pnDetailsPaletteColor);
            this.tpPalette.Name = "tpPalette";
            this.toolTip1.SetToolTip(this.tpPalette, resources.GetString("tpPalette.ToolTip"));
            this.tpPalette.UseVisualStyleBackColor = true;
            // 
            // label53
            // 
            resources.ApplyResources(this.label53, "label53");
            this.label53.Name = "label53";
            this.toolTip1.SetToolTip(this.label53, resources.GetString("label53.ToolTip"));
            // 
            // label52
            // 
            resources.ApplyResources(this.label52, "label52");
            this.label52.Name = "label52";
            this.toolTip1.SetToolTip(this.label52, resources.GetString("label52.ToolTip"));
            // 
            // label51
            // 
            resources.ApplyResources(this.label51, "label51");
            this.label51.Name = "label51";
            this.toolTip1.SetToolTip(this.label51, resources.GetString("label51.ToolTip"));
            // 
            // txtPaletteDetailsAddress
            // 
            resources.ApplyResources(this.txtPaletteDetailsAddress, "txtPaletteDetailsAddress");
            this.txtPaletteDetailsAddress.Name = "txtPaletteDetailsAddress";
            this.txtPaletteDetailsAddress.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtPaletteDetailsAddress, resources.GetString("txtPaletteDetailsAddress.ToolTip"));
            // 
            // txtPaletteDetailsIndex
            // 
            resources.ApplyResources(this.txtPaletteDetailsIndex, "txtPaletteDetailsIndex");
            this.txtPaletteDetailsIndex.Name = "txtPaletteDetailsIndex";
            this.txtPaletteDetailsIndex.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtPaletteDetailsIndex, resources.GetString("txtPaletteDetailsIndex.ToolTip"));
            // 
            // txtPaletteDetailsIndexHex
            // 
            resources.ApplyResources(this.txtPaletteDetailsIndexHex, "txtPaletteDetailsIndexHex");
            this.txtPaletteDetailsIndexHex.Name = "txtPaletteDetailsIndexHex";
            this.txtPaletteDetailsIndexHex.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtPaletteDetailsIndexHex, resources.GetString("txtPaletteDetailsIndexHex.ToolTip"));
            // 
            // txtDetailsPaletteColorRGB
            // 
            resources.ApplyResources(this.txtDetailsPaletteColorRGB, "txtDetailsPaletteColorRGB");
            this.txtDetailsPaletteColorRGB.Name = "txtDetailsPaletteColorRGB";
            this.txtDetailsPaletteColorRGB.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtDetailsPaletteColorRGB, resources.GetString("txtDetailsPaletteColorRGB.ToolTip"));
            // 
            // txtDetailsPaletteColorHex
            // 
            resources.ApplyResources(this.txtDetailsPaletteColorHex, "txtDetailsPaletteColorHex");
            this.txtDetailsPaletteColorHex.Name = "txtDetailsPaletteColorHex";
            this.txtDetailsPaletteColorHex.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtDetailsPaletteColorHex, resources.GetString("txtDetailsPaletteColorHex.ToolTip"));
            // 
            // txtDetailsPaletteColor
            // 
            resources.ApplyResources(this.txtDetailsPaletteColor, "txtDetailsPaletteColor");
            this.txtDetailsPaletteColor.Name = "txtDetailsPaletteColor";
            this.txtDetailsPaletteColor.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtDetailsPaletteColor, resources.GetString("txtDetailsPaletteColor.ToolTip"));
            // 
            // pnDetailsPaletteColor
            // 
            resources.ApplyResources(this.pnDetailsPaletteColor, "pnDetailsPaletteColor");
            this.pnDetailsPaletteColor.BackColor = System.Drawing.Color.Red;
            this.pnDetailsPaletteColor.Name = "pnDetailsPaletteColor";
            this.toolTip1.SetToolTip(this.pnDetailsPaletteColor, resources.GetString("pnDetailsPaletteColor.ToolTip"));
            this.pnDetailsPaletteColor.DoubleClick += new System.EventHandler(this.pnDetailsPaletteColor_DoubleClick);
            // 
            // tpTile
            // 
            resources.ApplyResources(this.tpTile, "tpTile");
            this.tpTile.Controls.Add(this.label45);
            this.tpTile.Controls.Add(this.txtTilePalette);
            this.tpTile.Controls.Add(this.txtTileNumber);
            this.tpTile.Controls.Add(this.txtTileMode);
            this.tpTile.Controls.Add(this.label18);
            this.tpTile.Controls.Add(this.txtTileColors);
            this.tpTile.Controls.Add(this.txtTileBpp);
            this.tpTile.Controls.Add(this.label42);
            this.tpTile.Controls.Add(this.txtTileAddress);
            this.tpTile.Controls.Add(this.viewerTile);
            this.tpTile.Name = "tpTile";
            this.toolTip1.SetToolTip(this.tpTile, resources.GetString("tpTile.ToolTip"));
            this.tpTile.UseVisualStyleBackColor = true;
            // 
            // label45
            // 
            resources.ApplyResources(this.label45, "label45");
            this.label45.Name = "label45";
            this.toolTip1.SetToolTip(this.label45, resources.GetString("label45.ToolTip"));
            // 
            // txtTilePalette
            // 
            resources.ApplyResources(this.txtTilePalette, "txtTilePalette");
            this.txtTilePalette.Name = "txtTilePalette";
            this.txtTilePalette.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtTilePalette, resources.GetString("txtTilePalette.ToolTip"));
            // 
            // txtTileNumber
            // 
            resources.ApplyResources(this.txtTileNumber, "txtTileNumber");
            this.txtTileNumber.Name = "txtTileNumber";
            this.txtTileNumber.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtTileNumber, resources.GetString("txtTileNumber.ToolTip"));
            // 
            // txtTileMode
            // 
            resources.ApplyResources(this.txtTileMode, "txtTileMode");
            this.txtTileMode.Name = "txtTileMode";
            this.txtTileMode.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtTileMode, resources.GetString("txtTileMode.ToolTip"));
            // 
            // label18
            // 
            resources.ApplyResources(this.label18, "label18");
            this.label18.Name = "label18";
            this.toolTip1.SetToolTip(this.label18, resources.GetString("label18.ToolTip"));
            // 
            // txtTileColors
            // 
            resources.ApplyResources(this.txtTileColors, "txtTileColors");
            this.txtTileColors.Name = "txtTileColors";
            this.txtTileColors.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtTileColors, resources.GetString("txtTileColors.ToolTip"));
            // 
            // txtTileBpp
            // 
            resources.ApplyResources(this.txtTileBpp, "txtTileBpp");
            this.txtTileBpp.Name = "txtTileBpp";
            this.txtTileBpp.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtTileBpp, resources.GetString("txtTileBpp.ToolTip"));
            // 
            // label42
            // 
            resources.ApplyResources(this.label42, "label42");
            this.label42.Name = "label42";
            this.toolTip1.SetToolTip(this.label42, resources.GetString("label42.ToolTip"));
            // 
            // txtTileAddress
            // 
            resources.ApplyResources(this.txtTileAddress, "txtTileAddress");
            this.txtTileAddress.Name = "txtTileAddress";
            this.txtTileAddress.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtTileAddress, resources.GetString("txtTileAddress.ToolTip"));
            // 
            // viewerTile
            // 
            resources.ApplyResources(this.viewerTile, "viewerTile");
            this.viewerTile.BackColor = System.Drawing.Color.Transparent;
            this.viewerTile.Name = "viewerTile";
            this.viewerTile.TabStop = false;
            this.toolTip1.SetToolTip(this.viewerTile, resources.GetString("viewerTile.ToolTip"));
            // 
            // tpMapEntry
            // 
            resources.ApplyResources(this.tpMapEntry, "tpMapEntry");
            this.tpMapEntry.Controls.Add(this.textBox2);
            this.tpMapEntry.Controls.Add(this.checkMapEntryVFlip);
            this.tpMapEntry.Controls.Add(this.label34);
            this.tpMapEntry.Controls.Add(this.checkMapEntryHFlip);
            this.tpMapEntry.Controls.Add(this.label17);
            this.tpMapEntry.Controls.Add(this.lblMapEntryHFlip);
            this.tpMapEntry.Controls.Add(this.txtMapEntryPalette);
            this.tpMapEntry.Controls.Add(this.label14);
            this.tpMapEntry.Controls.Add(this.label6);
            this.tpMapEntry.Controls.Add(this.txtMapEntryTileAddr);
            this.tpMapEntry.Controls.Add(this.txtMapEntryPrio);
            this.tpMapEntry.Controls.Add(this.txtMapEntryLocation);
            this.tpMapEntry.Controls.Add(this.txtMapEntryTileNum);
            this.tpMapEntry.Controls.Add(this.viewerMapEntryTile);
            this.tpMapEntry.Name = "tpMapEntry";
            this.toolTip1.SetToolTip(this.tpMapEntry, resources.GetString("tpMapEntry.ToolTip"));
            this.tpMapEntry.UseVisualStyleBackColor = true;
            // 
            // textBox2
            // 
            resources.ApplyResources(this.textBox2, "textBox2");
            this.textBox2.Name = "textBox2";
            this.textBox2.ReadOnly = true;
            this.toolTip1.SetToolTip(this.textBox2, resources.GetString("textBox2.ToolTip"));
            // 
            // checkMapEntryVFlip
            // 
            resources.ApplyResources(this.checkMapEntryVFlip, "checkMapEntryVFlip");
            this.checkMapEntryVFlip.Name = "checkMapEntryVFlip";
            this.toolTip1.SetToolTip(this.checkMapEntryVFlip, resources.GetString("checkMapEntryVFlip.ToolTip"));
            this.checkMapEntryVFlip.UseVisualStyleBackColor = true;
            // 
            // label34
            // 
            resources.ApplyResources(this.label34, "label34");
            this.label34.Name = "label34";
            this.toolTip1.SetToolTip(this.label34, resources.GetString("label34.ToolTip"));
            // 
            // checkMapEntryHFlip
            // 
            resources.ApplyResources(this.checkMapEntryHFlip, "checkMapEntryHFlip");
            this.checkMapEntryHFlip.Name = "checkMapEntryHFlip";
            this.toolTip1.SetToolTip(this.checkMapEntryHFlip, resources.GetString("checkMapEntryHFlip.ToolTip"));
            this.checkMapEntryHFlip.UseVisualStyleBackColor = true;
            // 
            // label17
            // 
            resources.ApplyResources(this.label17, "label17");
            this.label17.Name = "label17";
            this.toolTip1.SetToolTip(this.label17, resources.GetString("label17.ToolTip"));
            // 
            // lblMapEntryHFlip
            // 
            resources.ApplyResources(this.lblMapEntryHFlip, "lblMapEntryHFlip");
            this.lblMapEntryHFlip.Name = "lblMapEntryHFlip";
            this.toolTip1.SetToolTip(this.lblMapEntryHFlip, resources.GetString("lblMapEntryHFlip.ToolTip"));
            // 
            // txtMapEntryPalette
            // 
            resources.ApplyResources(this.txtMapEntryPalette, "txtMapEntryPalette");
            this.txtMapEntryPalette.BackColor = System.Drawing.Color.LightGreen;
            this.txtMapEntryPalette.Name = "txtMapEntryPalette";
            this.txtMapEntryPalette.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtMapEntryPalette, resources.GetString("txtMapEntryPalette.ToolTip"));
            // 
            // label14
            // 
            resources.ApplyResources(this.label14, "label14");
            this.label14.Name = "label14";
            this.toolTip1.SetToolTip(this.label14, resources.GetString("label14.ToolTip"));
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            this.toolTip1.SetToolTip(this.label6, resources.GetString("label6.ToolTip"));
            // 
            // txtMapEntryTileAddr
            // 
            resources.ApplyResources(this.txtMapEntryTileAddr, "txtMapEntryTileAddr");
            this.txtMapEntryTileAddr.Name = "txtMapEntryTileAddr";
            this.txtMapEntryTileAddr.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtMapEntryTileAddr, resources.GetString("txtMapEntryTileAddr.ToolTip"));
            // 
            // txtMapEntryPrio
            // 
            resources.ApplyResources(this.txtMapEntryPrio, "txtMapEntryPrio");
            this.txtMapEntryPrio.BackColor = System.Drawing.Color.LightGreen;
            this.txtMapEntryPrio.Name = "txtMapEntryPrio";
            this.txtMapEntryPrio.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtMapEntryPrio, resources.GetString("txtMapEntryPrio.ToolTip"));
            // 
            // txtMapEntryLocation
            // 
            resources.ApplyResources(this.txtMapEntryLocation, "txtMapEntryLocation");
            this.txtMapEntryLocation.Name = "txtMapEntryLocation";
            this.txtMapEntryLocation.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtMapEntryLocation, resources.GetString("txtMapEntryLocation.ToolTip"));
            // 
            // txtMapEntryTileNum
            // 
            resources.ApplyResources(this.txtMapEntryTileNum, "txtMapEntryTileNum");
            this.txtMapEntryTileNum.BackColor = System.Drawing.Color.LightGreen;
            this.txtMapEntryTileNum.Name = "txtMapEntryTileNum";
            this.txtMapEntryTileNum.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtMapEntryTileNum, resources.GetString("txtMapEntryTileNum.ToolTip"));
            // 
            // viewerMapEntryTile
            // 
            resources.ApplyResources(this.viewerMapEntryTile, "viewerMapEntryTile");
            this.viewerMapEntryTile.BackColor = System.Drawing.Color.Transparent;
            this.viewerMapEntryTile.Name = "viewerMapEntryTile";
            this.viewerMapEntryTile.TabStop = false;
            this.toolTip1.SetToolTip(this.viewerMapEntryTile, resources.GetString("viewerMapEntryTile.ToolTip"));
            // 
            // tpOBJ
            // 
            resources.ApplyResources(this.tpOBJ, "tpOBJ");
            this.tpOBJ.Controls.Add(this.txtObjPriority);
            this.tpOBJ.Controls.Add(this.label50);
            this.tpOBJ.Controls.Add(this.txtObjPaletteMemo);
            this.tpOBJ.Controls.Add(this.label49);
            this.tpOBJ.Controls.Add(this.txtObjPalette);
            this.tpOBJ.Controls.Add(this.label48);
            this.tpOBJ.Controls.Add(this.txtObjNameAddr);
            this.tpOBJ.Controls.Add(this.txtObjName);
            this.tpOBJ.Controls.Add(this.txtObjSize);
            this.tpOBJ.Controls.Add(this.label46);
            this.tpOBJ.Controls.Add(this.cbObjLarge);
            this.tpOBJ.Controls.Add(this.txtObjNumber);
            this.tpOBJ.Controls.Add(this.txtObjCoord);
            this.tpOBJ.Controls.Add(this.cbObjVFlip);
            this.tpOBJ.Controls.Add(this.label43);
            this.tpOBJ.Controls.Add(this.cbObjHFlip);
            this.tpOBJ.Controls.Add(this.label44);
            this.tpOBJ.Controls.Add(this.viewerObj);
            this.tpOBJ.Name = "tpOBJ";
            this.toolTip1.SetToolTip(this.tpOBJ, resources.GetString("tpOBJ.ToolTip"));
            this.tpOBJ.UseVisualStyleBackColor = true;
            // 
            // txtObjPriority
            // 
            resources.ApplyResources(this.txtObjPriority, "txtObjPriority");
            this.txtObjPriority.BackColor = System.Drawing.Color.LightGreen;
            this.txtObjPriority.Name = "txtObjPriority";
            this.txtObjPriority.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtObjPriority, resources.GetString("txtObjPriority.ToolTip"));
            // 
            // label50
            // 
            resources.ApplyResources(this.label50, "label50");
            this.label50.Name = "label50";
            this.toolTip1.SetToolTip(this.label50, resources.GetString("label50.ToolTip"));
            // 
            // txtObjPaletteMemo
            // 
            resources.ApplyResources(this.txtObjPaletteMemo, "txtObjPaletteMemo");
            this.txtObjPaletteMemo.Name = "txtObjPaletteMemo";
            this.txtObjPaletteMemo.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtObjPaletteMemo, resources.GetString("txtObjPaletteMemo.ToolTip"));
            // 
            // label49
            // 
            resources.ApplyResources(this.label49, "label49");
            this.label49.Name = "label49";
            this.toolTip1.SetToolTip(this.label49, resources.GetString("label49.ToolTip"));
            // 
            // txtObjPalette
            // 
            resources.ApplyResources(this.txtObjPalette, "txtObjPalette");
            this.txtObjPalette.BackColor = System.Drawing.Color.LightGreen;
            this.txtObjPalette.Name = "txtObjPalette";
            this.txtObjPalette.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtObjPalette, resources.GetString("txtObjPalette.ToolTip"));
            // 
            // label48
            // 
            resources.ApplyResources(this.label48, "label48");
            this.label48.Name = "label48";
            this.toolTip1.SetToolTip(this.label48, resources.GetString("label48.ToolTip"));
            // 
            // txtObjNameAddr
            // 
            resources.ApplyResources(this.txtObjNameAddr, "txtObjNameAddr");
            this.txtObjNameAddr.Name = "txtObjNameAddr";
            this.txtObjNameAddr.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtObjNameAddr, resources.GetString("txtObjNameAddr.ToolTip"));
            // 
            // txtObjName
            // 
            resources.ApplyResources(this.txtObjName, "txtObjName");
            this.txtObjName.BackColor = System.Drawing.Color.LightGreen;
            this.txtObjName.Name = "txtObjName";
            this.txtObjName.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtObjName, resources.GetString("txtObjName.ToolTip"));
            // 
            // txtObjSize
            // 
            resources.ApplyResources(this.txtObjSize, "txtObjSize");
            this.txtObjSize.Name = "txtObjSize";
            this.txtObjSize.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtObjSize, resources.GetString("txtObjSize.ToolTip"));
            // 
            // label46
            // 
            resources.ApplyResources(this.label46, "label46");
            this.label46.Name = "label46";
            this.toolTip1.SetToolTip(this.label46, resources.GetString("label46.ToolTip"));
            // 
            // cbObjLarge
            // 
            resources.ApplyResources(this.cbObjLarge, "cbObjLarge");
            this.cbObjLarge.Name = "cbObjLarge";
            this.toolTip1.SetToolTip(this.cbObjLarge, resources.GetString("cbObjLarge.ToolTip"));
            this.cbObjLarge.UseVisualStyleBackColor = true;
            // 
            // txtObjNumber
            // 
            resources.ApplyResources(this.txtObjNumber, "txtObjNumber");
            this.txtObjNumber.Name = "txtObjNumber";
            this.txtObjNumber.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtObjNumber, resources.GetString("txtObjNumber.ToolTip"));
            // 
            // txtObjCoord
            // 
            resources.ApplyResources(this.txtObjCoord, "txtObjCoord");
            this.txtObjCoord.Name = "txtObjCoord";
            this.txtObjCoord.ReadOnly = true;
            this.toolTip1.SetToolTip(this.txtObjCoord, resources.GetString("txtObjCoord.ToolTip"));
            // 
            // cbObjVFlip
            // 
            resources.ApplyResources(this.cbObjVFlip, "cbObjVFlip");
            this.cbObjVFlip.Name = "cbObjVFlip";
            this.toolTip1.SetToolTip(this.cbObjVFlip, resources.GetString("cbObjVFlip.ToolTip"));
            this.cbObjVFlip.UseVisualStyleBackColor = true;
            // 
            // label43
            // 
            resources.ApplyResources(this.label43, "label43");
            this.label43.Name = "label43";
            this.toolTip1.SetToolTip(this.label43, resources.GetString("label43.ToolTip"));
            // 
            // cbObjHFlip
            // 
            resources.ApplyResources(this.cbObjHFlip, "cbObjHFlip");
            this.cbObjHFlip.Name = "cbObjHFlip";
            this.toolTip1.SetToolTip(this.cbObjHFlip, resources.GetString("cbObjHFlip.ToolTip"));
            this.cbObjHFlip.UseVisualStyleBackColor = true;
            // 
            // label44
            // 
            resources.ApplyResources(this.label44, "label44");
            this.label44.Name = "label44";
            this.toolTip1.SetToolTip(this.label44, resources.GetString("label44.ToolTip"));
            // 
            // viewerObj
            // 
            resources.ApplyResources(this.viewerObj, "viewerObj");
            this.viewerObj.BackColor = System.Drawing.Color.Transparent;
            this.viewerObj.Name = "viewerObj";
            this.viewerObj.TabStop = false;
            this.toolTip1.SetToolTip(this.viewerObj, resources.GetString("viewerObj.ToolTip"));
            // 
            // viewerPanel
            // 
            resources.ApplyResources(this.viewerPanel, "viewerPanel");
            this.viewerPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.viewerPanel.Controls.Add(this.viewer);
            this.viewerPanel.Name = "viewerPanel";
            this.tableLayoutPanel1.SetRowSpan(this.viewerPanel, 2);
            this.toolTip1.SetToolTip(this.viewerPanel, resources.GetString("viewerPanel.ToolTip"));
            // 
            // viewer
            // 
            resources.ApplyResources(this.viewer, "viewer");
            this.viewer.BackColor = System.Drawing.Color.Transparent;
            this.viewer.Name = "viewer";
            this.viewer.TabStop = false;
            this.toolTip1.SetToolTip(this.viewer, resources.GetString("viewer.ToolTip"));
            this.viewer.MouseDown += new System.Windows.Forms.MouseEventHandler(this.viewer_MouseDown);
            this.viewer.MouseMove += new System.Windows.Forms.MouseEventHandler(this.viewer_MouseMove);
            this.viewer.MouseUp += new System.Windows.Forms.MouseEventHandler(this.viewer_MouseUp);
            // 
            // toolTip1
            // 
            this.toolTip1.AutoPopDelay = 5000;
            this.toolTip1.InitialDelay = 250;
            this.toolTip1.ReshowDelay = 100;
            // 
            // messagetimer
            // 
            this.messagetimer.Interval = 5000;
            this.messagetimer.Tick += new System.EventHandler(this.MessageTimer_Tick);
            // 
            // SNESGraphicsDebugger
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.menuStrip1);
            this.KeyPreview = true;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "SNESGraphicsDebugger";
            this.ShowIcon = false;
            this.toolTip1.SetToolTip(this, resources.GetString("$this.ToolTip"));
            this.Load += new System.EventHandler(this.SNESGraphicsDebugger_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.groupFreeze.ResumeLayout(false);
            this.pnGroupFreeze.ResumeLayout(false);
            this.pnGroupFreeze.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudScanline)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderScanline)).EndInit();
            this.groupBox6.ResumeLayout(false);
            this.groupBox6.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox8.ResumeLayout(false);
            this.groupBox8.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox5.ResumeLayout(false);
            this.tabctrlDetails.ResumeLayout(false);
            this.tpPalette.ResumeLayout(false);
            this.tpPalette.PerformLayout();
            this.tpTile.ResumeLayout(false);
            this.tpTile.PerformLayout();
            this.tpMapEntry.ResumeLayout(false);
            this.tpMapEntry.PerformLayout();
            this.tpOBJ.ResumeLayout(false);
            this.tpOBJ.PerformLayout();
            this.viewerPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private MenuStripEx menuStrip1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx fileToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx saveScreenshotAsToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx saveScreenshotToClipboardToolStripMenuItem;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.GroupBox groupBox2;
		private BizHawk.WinForms.Controls.LocLabelEx label16;
		private System.Windows.Forms.TextBox txtScreenBG4TSize;
		private System.Windows.Forms.TextBox txtScreenBG3TSize;
		private System.Windows.Forms.TextBox txtScreenBG2TSize;
		private System.Windows.Forms.TextBox txtScreenBG1TSize;
		private System.Windows.Forms.TextBox txtScreenBG4Bpp;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private System.Windows.Forms.TextBox txtScreenBG3Bpp;
		private System.Windows.Forms.TextBox txtModeBits;
		private System.Windows.Forms.TextBox txtScreenBG2Bpp;
		private BizHawk.WinForms.Controls.LocLabelEx label8;
		private BizHawk.WinForms.Controls.LocLabelEx label7;
		private System.Windows.Forms.TextBox txtScreenBG1Bpp;
		private BizHawk.WinForms.Controls.LocLabelEx lblBG3;
		private BizHawk.WinForms.Controls.LocLabelEx label4;
		private BizHawk.WinForms.Controls.LocLabelEx label5;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.RadioButton rbBG4;
		private System.Windows.Forms.RadioButton rbBG3;
		private System.Windows.Forms.RadioButton rbBG2;
		private System.Windows.Forms.RadioButton rbBG1;
		private System.Windows.Forms.TextBox txtBG1TSizeDescr;
		private System.Windows.Forms.ComboBox comboBGProps;
		private BizHawk.WinForms.Controls.LocLabelEx label15;
		private System.Windows.Forms.TextBox txtBG1TSizeBits;
		private BizHawk.WinForms.Controls.LocLabelEx label13;
		private System.Windows.Forms.TextBox txtBG1Colors;
		private System.Windows.Forms.TextBox txtBG1Bpp;
		private BizHawk.WinForms.Controls.LocLabelEx label12;
		private System.Windows.Forms.TextBox txtBG1TDAddrDescr;
		private BizHawk.WinForms.Controls.LocLabelEx label11;
		private System.Windows.Forms.TextBox txtBG1SCAddrDescr;
		private BizHawk.WinForms.Controls.LocLabelEx label9;
		private System.Windows.Forms.TextBox txtBG1TDAddrBits;
		private BizHawk.WinForms.Controls.LocLabelEx label10;
		private System.Windows.Forms.TextBox txtBG1SizeInPixels;
		private BizHawk.WinForms.Controls.LocLabelEx label3;
		private System.Windows.Forms.TextBox txtBG1SCAddrBits;
		private BizHawk.WinForms.Controls.LocLabelEx label2;
		private System.Windows.Forms.TextBox txtBG1SizeBits;
		private BizHawk.WinForms.Controls.LocLabelEx label19;
		private System.Windows.Forms.ComboBox comboDisplayType;
		private System.Windows.Forms.TrackBar sliderScanline;
		private System.Windows.Forms.GroupBox groupBox5;
		private SNESGraphicsViewer paletteViewer;
		private System.Windows.Forms.NumericUpDown nudScanline;
		private System.Windows.Forms.TabControl tabctrlDetails;
		private System.Windows.Forms.TabPage tpPalette;
		private System.Windows.Forms.TextBox txtPaletteDetailsAddress;
		private System.Windows.Forms.TextBox txtPaletteDetailsIndex;
		private System.Windows.Forms.TextBox txtPaletteDetailsIndexHex;
		private System.Windows.Forms.TextBox txtDetailsPaletteColorRGB;
		private System.Windows.Forms.TextBox txtDetailsPaletteColorHex;
		private System.Windows.Forms.TextBox txtDetailsPaletteColor;
		private System.Windows.Forms.Panel pnDetailsPaletteColor;
		private System.Windows.Forms.TabPage tpTile;
		private System.Windows.Forms.Panel viewerPanel;
		private SNESGraphicsViewer viewer;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.CheckBox checkScanlineControl;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.CheckBox check2x;
		private BizHawk.WinForms.Controls.LocLabelEx label21;
		private System.Windows.Forms.CheckBox checkScreenExtbg;
		private BizHawk.WinForms.Controls.LocLabelEx label20;
		private BizHawk.WinForms.Controls.LocLabelEx label18391;
		private System.Windows.Forms.CheckBox checkScreenHires;
		private BizHawk.WinForms.Controls.LocLabelEx label198129381279841;
		private System.Windows.Forms.CheckBox checkScreenOverscan;
		private BizHawk.WinForms.Controls.LocLabelEx label123812831;
		private System.Windows.Forms.CheckBox checkScreenObjInterlace;
		private BizHawk.WinForms.Controls.LocLabelEx label2193813;
		private System.Windows.Forms.CheckBox checkScreenInterlace;
		private System.Windows.Forms.TextBox txtScreenCGWSEL_ColorMask;
		private BizHawk.WinForms.Controls.LocLabelEx label22;
		private BizHawk.WinForms.Controls.LocLabelEx label2893719831;
		private System.Windows.Forms.TextBox txtScreenCGWSEL_ColorSubMask;
		private BizHawk.WinForms.Controls.LocLabelEx label25;
		private System.Windows.Forms.TextBox txtScreenCGWSEL_MathFixed;
		private BizHawk.WinForms.Controls.LocLabelEx label23;
		private BizHawk.WinForms.Controls.LocLabelEx label27;
		private System.Windows.Forms.CheckBox checkScreenCGWSEL_DirectColor;
		private SNESGraphicsViewer viewerTile;
		private System.Windows.Forms.Panel pnBackdropColor;
		private System.Windows.Forms.CheckBox checkBackdropColor;
		private BizHawk.WinForms.Controls.LocLabelEx label24;
		private System.Windows.Forms.Timer messagetimer;
		private System.Windows.Forms.TextBox txtOBSELT1OfsBits;
		private System.Windows.Forms.TextBox txtOBSELT1OfsDescr;
		private BizHawk.WinForms.Controls.LocLabelEx label30;
		private System.Windows.Forms.TextBox txtOBSELBaseBits;
		private System.Windows.Forms.TextBox txtOBSELBaseDescr;
		private BizHawk.WinForms.Controls.LocLabelEx label29;
		private System.Windows.Forms.TextBox txtOBSELSizeBits;
		private BizHawk.WinForms.Controls.LocLabelEx label26;
		private System.Windows.Forms.TextBox txtOBSELSizeDescr;
		private BizHawk.WinForms.Controls.LocLabelEx label28;
		private System.Windows.Forms.ComboBox comboPalette;
		private BizHawk.WinForms.Controls.LocLabelEx lblTS;
		private BizHawk.WinForms.Controls.LocLabelEx lblTM;
		private BizHawk.WinForms.Controls.LocLabelEx label32;
		private BizHawk.WinForms.Controls.LocLabelEx label31;
		private CustomCheckBox checkTSBG4;
		private CustomCheckBox checkTSBG3;
		private CustomCheckBox checkTSBG2;
		private CustomCheckBox checkTSBG1;
		private CustomCheckBox checkTMBG4;
		private CustomCheckBox checkTMBG3;
		private CustomCheckBox checkTMBG2;
		private CustomCheckBox checkTMBG1;
		private System.Windows.Forms.TabPage tpMapEntry;
		private System.Windows.Forms.TextBox txtMapEntryTileNum;
		private SNESGraphicsViewer viewerMapEntryTile;
		private System.Windows.Forms.CheckBox checkMapEntryVFlip;
		private BizHawk.WinForms.Controls.LocLabelEx label34;
		private System.Windows.Forms.CheckBox checkMapEntryHFlip;
		private BizHawk.WinForms.Controls.LocLabelEx label17;
		private BizHawk.WinForms.Controls.LocLabelEx lblMapEntryHFlip;
		private System.Windows.Forms.TextBox txtMapEntryPalette;
		private BizHawk.WinForms.Controls.LocLabelEx label14;
		private BizHawk.WinForms.Controls.LocLabelEx label6;
		private System.Windows.Forms.TextBox txtMapEntryTileAddr;
		private System.Windows.Forms.TextBox txtMapEntryPrio;
		private System.Windows.Forms.TextBox txtMapEntryLocation;
		private HorizontalLine label35;
		private CustomCheckBox checkMathBG4;
		private CustomCheckBox checkMathBG3;
		private CustomCheckBox checkMathBG2;
		private CustomCheckBox checkMathBG1;
		private BizHawk.WinForms.Controls.LocLabelEx label33;
		private CustomCheckBox checkMathOBJ;
		private CustomCheckBox checkTMOBJ;
		private CustomCheckBox checkTSOBJ;
		private CustomCheckBox checkMathBK;
		private System.Windows.Forms.TextBox txtScreenCGADSUB_AddSub;
		private BizHawk.WinForms.Controls.LocLabelEx label36;
		private System.Windows.Forms.TextBox txtScreenCGADSUB_AddSub_Descr;
		private BizHawk.WinForms.Controls.LocLabelEx label38;
		private System.Windows.Forms.CheckBox txtScreenCGADSUB_Half;
		private BizHawk.WinForms.Controls.LocLabelEx lblEnPrio0;
		private System.Windows.Forms.CheckBox checkEN0_OBJ;
		private System.Windows.Forms.CheckBox checkEN0_BG4;
		private System.Windows.Forms.CheckBox checkEN0_BG3;
		private System.Windows.Forms.CheckBox checkEN0_BG2;
		private System.Windows.Forms.CheckBox checkEN0_BG1;
		private BizHawk.WinForms.Controls.LocLabelEx lblEnPrio3;
		private System.Windows.Forms.CheckBox checkEN1_OBJ;
		private System.Windows.Forms.CheckBox checkEN1_BG4;
		private System.Windows.Forms.CheckBox checkEN1_BG3;
		private System.Windows.Forms.CheckBox checkEN1_BG2;
		private System.Windows.Forms.CheckBox checkEN1_BG1;
		private System.Windows.Forms.CheckBox checkEN3_OBJ;
		private System.Windows.Forms.CheckBox checkEN2_OBJ;
		private BizHawk.WinForms.Controls.LocLabelEx lblEnPrio2;
		private BizHawk.WinForms.Controls.LocLabelEx lblEnPrio1;
		private System.Windows.Forms.TextBox txtBGPaletteInfo;
		private System.Windows.Forms.GroupBox groupBox6;
		private BizHawk.WinForms.Controls.LocLabelEx labelClipboard;
		private BizHawk.WinForms.Controls.LocLabelEx label47;
		private System.Windows.Forms.TabPage tpOBJ;
		private System.Windows.Forms.GroupBox groupFreeze;
		private System.Windows.Forms.GroupBox groupBox8;
		private System.Windows.Forms.RadioButton radioButton6;
		private System.Windows.Forms.RadioButton radioButton1;
		private System.Windows.Forms.RadioButton radioButton10;
		private System.Windows.Forms.RadioButton radioButton15;
		private System.Windows.Forms.RadioButton radioButton5;
		private System.Windows.Forms.RadioButton radioButton14;
		private System.Windows.Forms.RadioButton radioButton4;
		private System.Windows.Forms.RadioButton radioButton3;
		private System.Windows.Forms.RadioButton radioButton13;
		private System.Windows.Forms.RadioButton radioButton2;
		private System.Windows.Forms.Panel pnGroupFreeze;
		private BizHawk.WinForms.Controls.LocLabelEx labelMemory;
		private System.Windows.Forms.TextBox txtBG1MapSizeBytes;
		private System.Windows.Forms.TextBox txtBG1SizeInTiles;
		private System.Windows.Forms.TextBox txtTileMode;
		private BizHawk.WinForms.Controls.LocLabelEx label18;
		private System.Windows.Forms.TextBox txtTileColors;
		private System.Windows.Forms.TextBox txtTileBpp;
		private BizHawk.WinForms.Controls.LocLabelEx label42;
		private System.Windows.Forms.TextBox txtTileAddress;
		private SNESGraphicsViewer viewerObj;
		private System.Windows.Forms.TextBox txtObjNumber;
		private System.Windows.Forms.TextBox txtObjCoord;
		private System.Windows.Forms.CheckBox cbObjVFlip;
		private BizHawk.WinForms.Controls.LocLabelEx label43;
		private System.Windows.Forms.CheckBox cbObjHFlip;
		private BizHawk.WinForms.Controls.LocLabelEx label44;
		private System.Windows.Forms.TextBox txtTileNumber;
		private BizHawk.WinForms.Controls.LocLabelEx label45;
		private System.Windows.Forms.TextBox txtTilePalette;
		private BizHawk.WinForms.Controls.LocLabelEx label48;
		private System.Windows.Forms.TextBox txtObjNameAddr;
		private System.Windows.Forms.TextBox txtObjName;
		private System.Windows.Forms.TextBox txtObjSize;
		private BizHawk.WinForms.Controls.LocLabelEx label46;
		private System.Windows.Forms.CheckBox cbObjLarge;
		private BizHawk.WinForms.Controls.LocLabelEx label53;
		private BizHawk.WinForms.Controls.LocLabelEx label52;
		private BizHawk.WinForms.Controls.LocLabelEx label51;
		private System.Windows.Forms.TextBox textBox2;
		private System.Windows.Forms.TextBox txtObjPriority;
		private BizHawk.WinForms.Controls.LocLabelEx label50;
		private System.Windows.Forms.TextBox txtObjPaletteMemo;
		private BizHawk.WinForms.Controls.LocLabelEx label49;
		private System.Windows.Forms.TextBox txtObjPalette;
		private System.Windows.Forms.TextBox txtBG1Scroll;
		private BizHawk.WinForms.Controls.LocLabelEx label37;
	}
}