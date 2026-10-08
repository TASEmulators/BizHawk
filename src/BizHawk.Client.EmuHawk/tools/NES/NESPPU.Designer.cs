using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	partial class NesPPU
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NesPPU));
            this.PatternGroup = new System.Windows.Forms.GroupBox();
            this.Table1PaletteLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.Table0PaletteLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.PatternView = new BizHawk.Client.EmuHawk.PatternViewer();
            this.PatternContext = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.PatternSaveImageMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.PatternImageToClipboardMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.PatternRefreshMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.PalettesGroup = new System.Windows.Forms.GroupBox();
            this.PaletteView = new BizHawk.Client.EmuHawk.PaletteViewer();
            this.PaletteContext = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.PaletteSaveImageMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.PaletteImageToClipboardMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.PaletteRefreshMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.DetailsBox = new System.Windows.Forms.GroupBox();
            this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.Value5Label = new BizHawk.WinForms.Controls.LocLabelEx();
            this.Value4Label = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.ZoomBox = new System.Windows.Forms.PictureBox();
            this.Value3Label = new BizHawk.WinForms.Controls.LocLabelEx();
            this.Value2Label = new BizHawk.WinForms.Controls.LocLabelEx();
            this.ValueLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.AddressLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.SpriteViewerBox = new System.Windows.Forms.GroupBox();
            this.SpriteView = new BizHawk.Client.EmuHawk.SpriteViewer();
            this.SpriteContext = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.SpriteSaveImageMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SpriteImageToClipboardMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SpriteRefreshMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.txtScanline = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label4 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label3 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.RefreshRate = new System.Windows.Forms.TrackBar();
            this.NesPPUMenu = new BizHawk.WinForms.Controls.MenuStripEx();
            this.FileSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SavePaletteScreenshotMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SavePatternScreenshotMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SaveSpriteScreenshotMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator1 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.CopyPaletteToClipboardMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.CopyPatternToClipboardMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.CopySpriteToClipboardMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.PatternSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.Table0PaletteSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.Table0P0MenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.Table0P1MenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.Table0P2MenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.Table0P3MenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.Table0P4MenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.Table0P5MenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.Table0P6MenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.Table0P7MenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.Table1PaletteSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.Table1P0MenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.Table1P1MenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.Table1P2MenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.Table1P3MenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.Table1P4MenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.Table1P5MenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.Table1P6MenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.Table1P7MenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SettingsSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.cHRROMTileViewerToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.NesPPUStatusBar = new BizHawk.WinForms.Controls.StatusStripEx();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.Messagetimer = new System.Windows.Forms.Timer(this.components);
            this.CHRROMGroup = new System.Windows.Forms.GroupBox();
            this.label5 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.numericUpDownCHRROMBank = new System.Windows.Forms.NumericUpDown();
            this.CHRROMView = new BizHawk.Client.EmuHawk.PatternViewer();
            this.PatternGroup.SuspendLayout();
            this.PatternContext.SuspendLayout();
            this.PalettesGroup.SuspendLayout();
            this.PaletteContext.SuspendLayout();
            this.DetailsBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ZoomBox)).BeginInit();
            this.SpriteViewerBox.SuspendLayout();
            this.SpriteContext.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.RefreshRate)).BeginInit();
            this.NesPPUMenu.SuspendLayout();
            this.NesPPUStatusBar.SuspendLayout();
            this.CHRROMGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownCHRROMBank)).BeginInit();
            this.SuspendLayout();
            // 
            // PatternGroup
            // 
            resources.ApplyResources(this.PatternGroup, "PatternGroup");
            this.PatternGroup.Controls.Add(this.Table1PaletteLabel);
            this.PatternGroup.Controls.Add(this.Table0PaletteLabel);
            this.PatternGroup.Controls.Add(this.PatternView);
            this.PatternGroup.Name = "PatternGroup";
            this.PatternGroup.TabStop = false;
            // 
            // Table1PaletteLabel
            // 
            resources.ApplyResources(this.Table1PaletteLabel, "Table1PaletteLabel");
            this.Table1PaletteLabel.Name = "Table1PaletteLabel";
            // 
            // Table0PaletteLabel
            // 
            resources.ApplyResources(this.Table0PaletteLabel, "Table0PaletteLabel");
            this.Table0PaletteLabel.Name = "Table0PaletteLabel";
            // 
            // PatternView
            // 
            resources.ApplyResources(this.PatternView, "PatternView");
            this.PatternView.BackColor = System.Drawing.Color.Transparent;
            this.PatternView.ContextMenuStrip = this.PatternContext;
            this.PatternView.Name = "PatternView";
            this.PatternView.Pal0 = 0;
            this.PatternView.Pal1 = 0;
            this.PatternView.Pattern = ((System.Drawing.Bitmap)(resources.GetObject("PatternView.Pattern")));
            this.PatternView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.PatternView_Click);
            this.PatternView.MouseEnter += new System.EventHandler(this.PatternView_MouseEnter);
            this.PatternView.MouseLeave += new System.EventHandler(this.PatternView_MouseLeave);
            this.PatternView.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PatternView_MouseMove);
            // 
            // PatternContext
            // 
            resources.ApplyResources(this.PatternContext, "PatternContext");
            this.PatternContext.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.PatternContext.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.PatternSaveImageMenuItem,
            this.PatternImageToClipboardMenuItem,
            this.PatternRefreshMenuItem});
            this.PatternContext.Name = "PatternContext";
            // 
            // PatternSaveImageMenuItem
            // 
            resources.ApplyResources(this.PatternSaveImageMenuItem, "PatternSaveImageMenuItem");
            this.PatternSaveImageMenuItem.Click += new System.EventHandler(this.SavePatternScreenshotMenuItem_Click);
            // 
            // PatternImageToClipboardMenuItem
            // 
            resources.ApplyResources(this.PatternImageToClipboardMenuItem, "PatternImageToClipboardMenuItem");
            this.PatternImageToClipboardMenuItem.Click += new System.EventHandler(this.CopyPatternToClipboardMenuItem_Click);
            // 
            // PatternRefreshMenuItem
            // 
            resources.ApplyResources(this.PatternRefreshMenuItem, "PatternRefreshMenuItem");
            this.PatternRefreshMenuItem.Click += new System.EventHandler(this.PatternRefreshMenuItem_Click);
            // 
            // PalettesGroup
            // 
            resources.ApplyResources(this.PalettesGroup, "PalettesGroup");
            this.PalettesGroup.Controls.Add(this.PaletteView);
            this.PalettesGroup.Name = "PalettesGroup";
            this.PalettesGroup.TabStop = false;
            // 
            // PaletteView
            // 
            resources.ApplyResources(this.PaletteView, "PaletteView");
            this.PaletteView.BackColor = System.Drawing.Color.Transparent;
            this.PaletteView.ContextMenuStrip = this.PaletteContext;
            this.PaletteView.Name = "PaletteView";
            this.PaletteView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.PaletteView_MouseClick);
            this.PaletteView.MouseEnter += new System.EventHandler(this.PaletteView_MouseEnter);
            this.PaletteView.MouseLeave += new System.EventHandler(this.PaletteView_MouseLeave);
            this.PaletteView.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PaletteView_MouseMove);
            // 
            // PaletteContext
            // 
            resources.ApplyResources(this.PaletteContext, "PaletteContext");
            this.PaletteContext.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.PaletteContext.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.PaletteSaveImageMenuItem,
            this.PaletteImageToClipboardMenuItem,
            this.PaletteRefreshMenuItem});
            this.PaletteContext.Name = "PaletteContext";
            // 
            // PaletteSaveImageMenuItem
            // 
            resources.ApplyResources(this.PaletteSaveImageMenuItem, "PaletteSaveImageMenuItem");
            this.PaletteSaveImageMenuItem.Click += new System.EventHandler(this.SavePaletteScreenshotMenuItem_Click);
            // 
            // PaletteImageToClipboardMenuItem
            // 
            resources.ApplyResources(this.PaletteImageToClipboardMenuItem, "PaletteImageToClipboardMenuItem");
            this.PaletteImageToClipboardMenuItem.Click += new System.EventHandler(this.CopyPaletteToClipboardMenuItem_Click);
            // 
            // PaletteRefreshMenuItem
            // 
            resources.ApplyResources(this.PaletteRefreshMenuItem, "PaletteRefreshMenuItem");
            this.PaletteRefreshMenuItem.Click += new System.EventHandler(this.PaletteRefreshMenuItem_Click);
            // 
            // DetailsBox
            // 
            resources.ApplyResources(this.DetailsBox, "DetailsBox");
            this.DetailsBox.Controls.Add(this.label2);
            this.DetailsBox.Controls.Add(this.Value5Label);
            this.DetailsBox.Controls.Add(this.Value4Label);
            this.DetailsBox.Controls.Add(this.label1);
            this.DetailsBox.Controls.Add(this.ZoomBox);
            this.DetailsBox.Controls.Add(this.Value3Label);
            this.DetailsBox.Controls.Add(this.Value2Label);
            this.DetailsBox.Controls.Add(this.ValueLabel);
            this.DetailsBox.Controls.Add(this.AddressLabel);
            this.DetailsBox.Name = "DetailsBox";
            this.DetailsBox.TabStop = false;
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // Value5Label
            // 
            resources.ApplyResources(this.Value5Label, "Value5Label");
            this.Value5Label.Name = "Value5Label";
            // 
            // Value4Label
            // 
            resources.ApplyResources(this.Value4Label, "Value4Label");
            this.Value4Label.Name = "Value4Label";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // ZoomBox
            // 
            resources.ApplyResources(this.ZoomBox, "ZoomBox");
            this.ZoomBox.Name = "ZoomBox";
            this.ZoomBox.TabStop = false;
            // 
            // Value3Label
            // 
            resources.ApplyResources(this.Value3Label, "Value3Label");
            this.Value3Label.Name = "Value3Label";
            // 
            // Value2Label
            // 
            resources.ApplyResources(this.Value2Label, "Value2Label");
            this.Value2Label.Name = "Value2Label";
            // 
            // ValueLabel
            // 
            resources.ApplyResources(this.ValueLabel, "ValueLabel");
            this.ValueLabel.Name = "ValueLabel";
            // 
            // AddressLabel
            // 
            resources.ApplyResources(this.AddressLabel, "AddressLabel");
            this.AddressLabel.Name = "AddressLabel";
            // 
            // SpriteViewerBox
            // 
            resources.ApplyResources(this.SpriteViewerBox, "SpriteViewerBox");
            this.SpriteViewerBox.Controls.Add(this.SpriteView);
            this.SpriteViewerBox.Name = "SpriteViewerBox";
            this.SpriteViewerBox.TabStop = false;
            // 
            // SpriteView
            // 
            resources.ApplyResources(this.SpriteView, "SpriteView");
            this.SpriteView.BackColor = System.Drawing.Color.Transparent;
            this.SpriteView.ContextMenuStrip = this.SpriteContext;
            this.SpriteView.Name = "SpriteView";
            this.SpriteView.Sprites = ((System.Drawing.Bitmap)(resources.GetObject("SpriteView.Sprites")));
            this.SpriteView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.SpriteView_MouseClick);
            this.SpriteView.MouseEnter += new System.EventHandler(this.SpriteView_MouseEnter);
            this.SpriteView.MouseLeave += new System.EventHandler(this.SpriteView_MouseLeave);
            this.SpriteView.MouseMove += new System.Windows.Forms.MouseEventHandler(this.SpriteView_MouseMove);
            // 
            // SpriteContext
            // 
            resources.ApplyResources(this.SpriteContext, "SpriteContext");
            this.SpriteContext.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.SpriteContext.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SpriteSaveImageMenuItem,
            this.SpriteImageToClipboardMenuItem,
            this.SpriteRefreshMenuItem});
            this.SpriteContext.Name = "SpriteContext";
            // 
            // SpriteSaveImageMenuItem
            // 
            resources.ApplyResources(this.SpriteSaveImageMenuItem, "SpriteSaveImageMenuItem");
            this.SpriteSaveImageMenuItem.Click += new System.EventHandler(this.SaveSpriteScreenshotMenuItem_Click);
            // 
            // SpriteImageToClipboardMenuItem
            // 
            resources.ApplyResources(this.SpriteImageToClipboardMenuItem, "SpriteImageToClipboardMenuItem");
            this.SpriteImageToClipboardMenuItem.Click += new System.EventHandler(this.CopySpriteToClipboardMenuItem_Click);
            // 
            // SpriteRefreshMenuItem
            // 
            resources.ApplyResources(this.SpriteRefreshMenuItem, "SpriteRefreshMenuItem");
            this.SpriteRefreshMenuItem.Click += new System.EventHandler(this.SpriteRefreshMenuItem_Click);
            // 
            // txtScanline
            // 
            resources.ApplyResources(this.txtScanline, "txtScanline");
            this.txtScanline.Name = "txtScanline";
            this.txtScanline.TextChanged += new System.EventHandler(this.ScanlineTextBox_TextChanged);
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.txtScanline);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // groupBox2
            // 
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.RefreshRate);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // RefreshRate
            // 
            resources.ApplyResources(this.RefreshRate, "RefreshRate");
            this.RefreshRate.LargeChange = 2;
            this.RefreshRate.Maximum = 8;
            this.RefreshRate.Minimum = 1;
            this.RefreshRate.Name = "RefreshRate";
            this.RefreshRate.TickFrequency = 8;
            this.RefreshRate.Value = 1;
            // 
            // NesPPUMenu
            // 
            resources.ApplyResources(this.NesPPUMenu, "NesPPUMenu");
            this.NesPPUMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.NesPPUMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu,
            this.PatternSubMenu,
            this.SettingsSubMenu});
            // 
            // FileSubMenu
            // 
            resources.ApplyResources(this.FileSubMenu, "FileSubMenu");
            this.FileSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SavePaletteScreenshotMenuItem,
            this.SavePatternScreenshotMenuItem,
            this.SaveSpriteScreenshotMenuItem,
            this.toolStripSeparator1,
            this.CopyPaletteToClipboardMenuItem,
            this.CopyPatternToClipboardMenuItem,
            this.CopySpriteToClipboardMenuItem});
            // 
            // SavePaletteScreenshotMenuItem
            // 
            resources.ApplyResources(this.SavePaletteScreenshotMenuItem, "SavePaletteScreenshotMenuItem");
            this.SavePaletteScreenshotMenuItem.Click += new System.EventHandler(this.SavePaletteScreenshotMenuItem_Click);
            // 
            // SavePatternScreenshotMenuItem
            // 
            resources.ApplyResources(this.SavePatternScreenshotMenuItem, "SavePatternScreenshotMenuItem");
            this.SavePatternScreenshotMenuItem.Click += new System.EventHandler(this.SavePatternScreenshotMenuItem_Click);
            // 
            // SaveSpriteScreenshotMenuItem
            // 
            resources.ApplyResources(this.SaveSpriteScreenshotMenuItem, "SaveSpriteScreenshotMenuItem");
            this.SaveSpriteScreenshotMenuItem.Click += new System.EventHandler(this.SaveSpriteScreenshotMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            resources.ApplyResources(this.toolStripSeparator1, "toolStripSeparator1");
            // 
            // CopyPaletteToClipboardMenuItem
            // 
            resources.ApplyResources(this.CopyPaletteToClipboardMenuItem, "CopyPaletteToClipboardMenuItem");
            this.CopyPaletteToClipboardMenuItem.Click += new System.EventHandler(this.CopyPaletteToClipboardMenuItem_Click);
            // 
            // CopyPatternToClipboardMenuItem
            // 
            resources.ApplyResources(this.CopyPatternToClipboardMenuItem, "CopyPatternToClipboardMenuItem");
            this.CopyPatternToClipboardMenuItem.Click += new System.EventHandler(this.CopyPatternToClipboardMenuItem_Click);
            // 
            // CopySpriteToClipboardMenuItem
            // 
            resources.ApplyResources(this.CopySpriteToClipboardMenuItem, "CopySpriteToClipboardMenuItem");
            this.CopySpriteToClipboardMenuItem.Click += new System.EventHandler(this.CopySpriteToClipboardMenuItem_Click);
            // 
            // PatternSubMenu
            // 
            resources.ApplyResources(this.PatternSubMenu, "PatternSubMenu");
            this.PatternSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Table0PaletteSubMenu,
            this.Table1PaletteSubMenu});
            // 
            // Table0PaletteSubMenu
            // 
            resources.ApplyResources(this.Table0PaletteSubMenu, "Table0PaletteSubMenu");
            this.Table0PaletteSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Table0P0MenuItem,
            this.Table0P1MenuItem,
            this.Table0P2MenuItem,
            this.Table0P3MenuItem,
            this.Table0P4MenuItem,
            this.Table0P5MenuItem,
            this.Table0P6MenuItem,
            this.Table0P7MenuItem});
            this.Table0PaletteSubMenu.DropDownOpened += new System.EventHandler(this.Table0PaletteSubMenu_DropDownOpened);
            // 
            // Table0P0MenuItem
            // 
            resources.ApplyResources(this.Table0P0MenuItem, "Table0P0MenuItem");
            this.Table0P0MenuItem.Click += new System.EventHandler(this.Palette_Click);
            // 
            // Table0P1MenuItem
            // 
            resources.ApplyResources(this.Table0P1MenuItem, "Table0P1MenuItem");
            this.Table0P1MenuItem.Click += new System.EventHandler(this.Palette_Click);
            // 
            // Table0P2MenuItem
            // 
            resources.ApplyResources(this.Table0P2MenuItem, "Table0P2MenuItem");
            this.Table0P2MenuItem.Click += new System.EventHandler(this.Palette_Click);
            // 
            // Table0P3MenuItem
            // 
            resources.ApplyResources(this.Table0P3MenuItem, "Table0P3MenuItem");
            this.Table0P3MenuItem.Click += new System.EventHandler(this.Palette_Click);
            // 
            // Table0P4MenuItem
            // 
            resources.ApplyResources(this.Table0P4MenuItem, "Table0P4MenuItem");
            this.Table0P4MenuItem.Click += new System.EventHandler(this.Palette_Click);
            // 
            // Table0P5MenuItem
            // 
            resources.ApplyResources(this.Table0P5MenuItem, "Table0P5MenuItem");
            this.Table0P5MenuItem.Click += new System.EventHandler(this.Palette_Click);
            // 
            // Table0P6MenuItem
            // 
            resources.ApplyResources(this.Table0P6MenuItem, "Table0P6MenuItem");
            this.Table0P6MenuItem.Click += new System.EventHandler(this.Palette_Click);
            // 
            // Table0P7MenuItem
            // 
            resources.ApplyResources(this.Table0P7MenuItem, "Table0P7MenuItem");
            this.Table0P7MenuItem.Click += new System.EventHandler(this.Palette_Click);
            // 
            // Table1PaletteSubMenu
            // 
            resources.ApplyResources(this.Table1PaletteSubMenu, "Table1PaletteSubMenu");
            this.Table1PaletteSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Table1P0MenuItem,
            this.Table1P1MenuItem,
            this.Table1P2MenuItem,
            this.Table1P3MenuItem,
            this.Table1P4MenuItem,
            this.Table1P5MenuItem,
            this.Table1P6MenuItem,
            this.Table1P7MenuItem});
            this.Table1PaletteSubMenu.DropDownOpened += new System.EventHandler(this.Table1PaletteSubMenu_DropDownOpened);
            // 
            // Table1P0MenuItem
            // 
            resources.ApplyResources(this.Table1P0MenuItem, "Table1P0MenuItem");
            this.Table1P0MenuItem.Click += new System.EventHandler(this.Palette_Click);
            // 
            // Table1P1MenuItem
            // 
            resources.ApplyResources(this.Table1P1MenuItem, "Table1P1MenuItem");
            this.Table1P1MenuItem.Click += new System.EventHandler(this.Palette_Click);
            // 
            // Table1P2MenuItem
            // 
            resources.ApplyResources(this.Table1P2MenuItem, "Table1P2MenuItem");
            this.Table1P2MenuItem.Click += new System.EventHandler(this.Palette_Click);
            // 
            // Table1P3MenuItem
            // 
            resources.ApplyResources(this.Table1P3MenuItem, "Table1P3MenuItem");
            this.Table1P3MenuItem.Click += new System.EventHandler(this.Palette_Click);
            // 
            // Table1P4MenuItem
            // 
            resources.ApplyResources(this.Table1P4MenuItem, "Table1P4MenuItem");
            this.Table1P4MenuItem.Click += new System.EventHandler(this.Palette_Click);
            // 
            // Table1P5MenuItem
            // 
            resources.ApplyResources(this.Table1P5MenuItem, "Table1P5MenuItem");
            this.Table1P5MenuItem.Click += new System.EventHandler(this.Palette_Click);
            // 
            // Table1P6MenuItem
            // 
            resources.ApplyResources(this.Table1P6MenuItem, "Table1P6MenuItem");
            this.Table1P6MenuItem.Click += new System.EventHandler(this.Palette_Click);
            // 
            // Table1P7MenuItem
            // 
            resources.ApplyResources(this.Table1P7MenuItem, "Table1P7MenuItem");
            this.Table1P7MenuItem.Click += new System.EventHandler(this.Palette_Click);
            // 
            // SettingsSubMenu
            // 
            resources.ApplyResources(this.SettingsSubMenu, "SettingsSubMenu");
            this.SettingsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cHRROMTileViewerToolStripMenuItem});
            this.SettingsSubMenu.DropDownOpened += new System.EventHandler(this.SettingsSubMenu_DropDownOpened);
            // 
            // cHRROMTileViewerToolStripMenuItem
            // 
            resources.ApplyResources(this.cHRROMTileViewerToolStripMenuItem, "cHRROMTileViewerToolStripMenuItem");
            this.cHRROMTileViewerToolStripMenuItem.Click += new System.EventHandler(this.ChrROMTileViewerToolStripMenuItem_Click);
            // 
            // NesPPUStatusBar
            // 
            resources.ApplyResources(this.NesPPUStatusBar, "NesPPUStatusBar");
            this.NesPPUStatusBar.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.NesPPUStatusBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.NesPPUStatusBar.Name = "NesPPUStatusBar";
            this.NesPPUStatusBar.SizingGrip = false;
            // 
            // toolStripStatusLabel1
            // 
            resources.ApplyResources(this.toolStripStatusLabel1, "toolStripStatusLabel1");
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            // 
            // Messagetimer
            // 
            this.Messagetimer.Interval = 5000;
            this.Messagetimer.Tick += new System.EventHandler(this.MessageTimer_Tick);
            // 
            // CHRROMGroup
            // 
            resources.ApplyResources(this.CHRROMGroup, "CHRROMGroup");
            this.CHRROMGroup.Controls.Add(this.label5);
            this.CHRROMGroup.Controls.Add(this.numericUpDownCHRROMBank);
            this.CHRROMGroup.Controls.Add(this.CHRROMView);
            this.CHRROMGroup.Name = "CHRROMGroup";
            this.CHRROMGroup.TabStop = false;
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // numericUpDownCHRROMBank
            // 
            resources.ApplyResources(this.numericUpDownCHRROMBank, "numericUpDownCHRROMBank");
            this.numericUpDownCHRROMBank.Name = "numericUpDownCHRROMBank";
            this.numericUpDownCHRROMBank.ValueChanged += new System.EventHandler(this.NumericUpDownChrRomBank_ValueChanged);
            // 
            // CHRROMView
            // 
            resources.ApplyResources(this.CHRROMView, "CHRROMView");
            this.CHRROMView.BackColor = System.Drawing.Color.Transparent;
            this.CHRROMView.Name = "CHRROMView";
            this.CHRROMView.Pal0 = 0;
            this.CHRROMView.Pal1 = 0;
            this.CHRROMView.Pattern = ((System.Drawing.Bitmap)(resources.GetObject("CHRROMView.Pattern")));
            // 
            // NesPPU
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.CHRROMGroup);
            this.Controls.Add(this.NesPPUStatusBar);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.SpriteViewerBox);
            this.Controls.Add(this.NesPPUMenu);
            this.Controls.Add(this.DetailsBox);
            this.Controls.Add(this.PalettesGroup);
            this.Controls.Add(this.PatternGroup);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.KeyPreview = true;
            this.MainMenuStrip = this.NesPPUMenu;
            this.Name = "NesPPU";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.NesPPU_FormClosed);
            this.Load += new System.EventHandler(this.NesPPU_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.NesPPU_KeyDown);
            this.MouseClick += new System.Windows.Forms.MouseEventHandler(this.NesPPU_MouseClick);
            this.PatternGroup.ResumeLayout(false);
            this.PatternGroup.PerformLayout();
            this.PatternContext.ResumeLayout(false);
            this.PalettesGroup.ResumeLayout(false);
            this.PaletteContext.ResumeLayout(false);
            this.DetailsBox.ResumeLayout(false);
            this.DetailsBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ZoomBox)).EndInit();
            this.SpriteViewerBox.ResumeLayout(false);
            this.SpriteContext.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.RefreshRate)).EndInit();
            this.NesPPUMenu.ResumeLayout(false);
            this.NesPPUMenu.PerformLayout();
            this.NesPPUStatusBar.ResumeLayout(false);
            this.NesPPUStatusBar.PerformLayout();
            this.CHRROMGroup.ResumeLayout(false);
            this.CHRROMGroup.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownCHRROMBank)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.GroupBox PatternGroup;
		private System.Windows.Forms.GroupBox PalettesGroup;
		private PaletteViewer PaletteView;
		private System.Windows.Forms.GroupBox DetailsBox;
		private BizHawk.WinForms.Controls.LocLabelEx ValueLabel;
		private BizHawk.WinForms.Controls.LocLabelEx AddressLabel;
		private PatternViewer PatternView;
		private BizHawk.WinForms.Controls.LocLabelEx Table1PaletteLabel;
		private BizHawk.WinForms.Controls.LocLabelEx Table0PaletteLabel;
		private BizHawk.WinForms.Controls.LocLabelEx Value2Label;
		private System.Windows.Forms.GroupBox SpriteViewerBox;
		private SpriteViewer SpriteView;
		private System.Windows.Forms.TextBox txtScanline;
		private System.Windows.Forms.GroupBox groupBox1;
		private BizHawk.WinForms.Controls.LocLabelEx Value3Label;
		private System.Windows.Forms.PictureBox ZoomBox;
		private BizHawk.WinForms.Controls.LocLabelEx Value5Label;
		private BizHawk.WinForms.Controls.LocLabelEx Value4Label;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private BizHawk.WinForms.Controls.LocLabelEx label2;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.TrackBar RefreshRate;
		private BizHawk.WinForms.Controls.LocLabelEx label4;
		private BizHawk.WinForms.Controls.LocLabelEx label3;
		private MenuStripEx NesPPUMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SettingsSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx PatternSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx Table0PaletteSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx Table0P0MenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx Table0P1MenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx Table0P2MenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx Table0P3MenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx Table0P4MenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx Table0P5MenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx Table0P6MenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx Table0P7MenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx Table1PaletteSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx Table1P0MenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx Table1P1MenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx Table1P2MenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx Table1P3MenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx Table1P4MenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx Table1P5MenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx Table1P6MenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx Table1P7MenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FileSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SavePaletteScreenshotMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SavePatternScreenshotMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SaveSpriteScreenshotMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator1;
		private System.Windows.Forms.ContextMenuStrip PaletteContext;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx PaletteSaveImageMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx PaletteRefreshMenuItem;
		private System.Windows.Forms.ContextMenuStrip PatternContext;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx PatternSaveImageMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx PatternRefreshMenuItem;
		private System.Windows.Forms.ContextMenuStrip SpriteContext;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SpriteSaveImageMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SpriteRefreshMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SpriteImageToClipboardMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx PatternImageToClipboardMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx PaletteImageToClipboardMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CopyPaletteToClipboardMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CopyPatternToClipboardMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CopySpriteToClipboardMenuItem;
		private StatusStripEx NesPPUStatusBar;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
		private System.Windows.Forms.Timer Messagetimer;
		private System.Windows.Forms.GroupBox CHRROMGroup;
		private BizHawk.WinForms.Controls.LocLabelEx label5;
		private System.Windows.Forms.NumericUpDown numericUpDownCHRROMBank;
		private PatternViewer CHRROMView;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx cHRROMTileViewerToolStripMenuItem;
	}
}