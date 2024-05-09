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
			this.PatternGroup.Controls.Add(this.Table1PaletteLabel);
			this.PatternGroup.Controls.Add(this.Table0PaletteLabel);
			this.PatternGroup.Controls.Add(this.PatternView);
			this.PatternGroup.Location = new System.Drawing.Point(293, 37);
			this.PatternGroup.Name = "PatternGroup";
			this.PatternGroup.Size = new System.Drawing.Size(272, 169);
			this.PatternGroup.TabIndex = 0;
			this.PatternGroup.TabStop = false;
			this.PatternGroup.Text = "Pattern Tables";
			// 
			// Table1PaletteLabel
			// 
			this.Table1PaletteLabel.Location = new System.Drawing.Point(129, 150);
			this.Table1PaletteLabel.Name = "Table1PaletteLabel";
			this.Table1PaletteLabel.Text = "Palette: 0";
			// 
			// Table0PaletteLabel
			// 
			this.Table0PaletteLabel.Location = new System.Drawing.Point(6, 150);
			this.Table0PaletteLabel.Name = "Table0PaletteLabel";
			this.Table0PaletteLabel.Text = "Palette: 0";
			// 
			// PatternView
			// 
			this.PatternView.BackColor = System.Drawing.Color.Transparent;
			this.PatternView.ContextMenuStrip = this.PatternContext;
			this.PatternView.Location = new System.Drawing.Point(7, 20);
			this.PatternView.Name = "PatternView";
			this.PatternView.Pal0 = 0;
			this.PatternView.Pal1 = 0;
			this.PatternView.Pattern = ((System.Drawing.Bitmap)(resources.GetObject("PatternView.Pattern")));
			this.PatternView.Size = new System.Drawing.Size(256, 128);
			this.PatternView.TabIndex = 0;
			this.PatternView.Text = "Pattern Tables";
			this.PatternView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.PatternView_Click);
			this.PatternView.MouseEnter += new System.EventHandler(this.PatternView_MouseEnter);
			this.PatternView.MouseLeave += new System.EventHandler(this.PatternView_MouseLeave);
			this.PatternView.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PatternView_MouseMove);
			// 
			// PatternContext
			// 
			this.PatternContext.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.PatternContext.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.PatternSaveImageMenuItem,
            this.PatternImageToClipboardMenuItem,
            this.PatternRefreshMenuItem});
			this.PatternContext.Name = "PatternContext";
			this.PatternContext.Size = new System.Drawing.Size(166, 70);
			// 
			// PatternSaveImageMenuItem
			// 
			this.PatternSaveImageMenuItem.Text = "&Save Image...";
			this.PatternSaveImageMenuItem.Click += new System.EventHandler(this.SavePatternScreenshotMenuItem_Click);
			// 
			// PatternImageToClipboardMenuItem
			// 
			this.PatternImageToClipboardMenuItem.Text = "Image to &Clipboard";
			this.PatternImageToClipboardMenuItem.Click += new System.EventHandler(this.CopyPatternToClipboardMenuItem_Click);
			// 
			// PatternRefreshMenuItem
			// 
			this.PatternRefreshMenuItem.Text = "&Refresh";
			this.PatternRefreshMenuItem.Click += new System.EventHandler(this.PatternRefreshMenuItem_Click);
			// 
			// PalettesGroup
			// 
			this.PalettesGroup.Controls.Add(this.PaletteView);
			this.PalettesGroup.Location = new System.Drawing.Point(12, 271);
			this.PalettesGroup.Name = "PalettesGroup";
			this.PalettesGroup.Size = new System.Drawing.Size(272, 65);
			this.PalettesGroup.TabIndex = 1;
			this.PalettesGroup.TabStop = false;
			this.PalettesGroup.Text = "Palettes";
			// 
			// PaletteView
			// 
			this.PaletteView.BackColor = System.Drawing.Color.Transparent;
			this.PaletteView.ContextMenuStrip = this.PaletteContext;
			this.PaletteView.Location = new System.Drawing.Point(6, 19);
			this.PaletteView.Name = "PaletteView";
			this.PaletteView.Size = new System.Drawing.Size(256, 32);
			this.PaletteView.TabIndex = 0;
			this.PaletteView.Text = "Palettes";
			this.PaletteView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.PaletteView_MouseClick);
			this.PaletteView.MouseEnter += new System.EventHandler(this.PaletteView_MouseEnter);
			this.PaletteView.MouseLeave += new System.EventHandler(this.PaletteView_MouseLeave);
			this.PaletteView.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PaletteView_MouseMove);
			// 
			// PaletteContext
			// 
			this.PaletteContext.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.PaletteContext.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.PaletteSaveImageMenuItem,
            this.PaletteImageToClipboardMenuItem,
            this.PaletteRefreshMenuItem});
			this.PaletteContext.Name = "PaletteContext";
			this.PaletteContext.Size = new System.Drawing.Size(166, 70);
			// 
			// PaletteSaveImageMenuItem
			// 
			this.PaletteSaveImageMenuItem.Text = "&Save Image...";
			this.PaletteSaveImageMenuItem.Click += new System.EventHandler(this.SavePaletteScreenshotMenuItem_Click);
			// 
			// PaletteImageToClipboardMenuItem
			// 
			this.PaletteImageToClipboardMenuItem.Text = "Image to &Clipboard";
			this.PaletteImageToClipboardMenuItem.Click += new System.EventHandler(this.CopyPaletteToClipboardMenuItem_Click);
			// 
			// PaletteRefreshMenuItem
			// 
			this.PaletteRefreshMenuItem.Text = "&Refresh";
			this.PaletteRefreshMenuItem.Click += new System.EventHandler(this.PaletteRefreshMenuItem_Click);
			// 
			// DetailsBox
			// 
			this.DetailsBox.Controls.Add(this.label2);
			this.DetailsBox.Controls.Add(this.Value5Label);
			this.DetailsBox.Controls.Add(this.Value4Label);
			this.DetailsBox.Controls.Add(this.label1);
			this.DetailsBox.Controls.Add(this.ZoomBox);
			this.DetailsBox.Controls.Add(this.Value3Label);
			this.DetailsBox.Controls.Add(this.Value2Label);
			this.DetailsBox.Controls.Add(this.ValueLabel);
			this.DetailsBox.Controls.Add(this.AddressLabel);
			this.DetailsBox.Location = new System.Drawing.Point(12, 95);
			this.DetailsBox.Name = "DetailsBox";
			this.DetailsBox.Size = new System.Drawing.Size(272, 170);
			this.DetailsBox.TabIndex = 2;
			this.DetailsBox.TabStop = false;
			this.DetailsBox.Text = "Details";
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(6, 31);
			this.label2.Name = "label2";
			this.label2.Text = "Shift-click to remember selection";
			// 
			// Value5Label
			// 
			this.Value5Label.Location = new System.Drawing.Point(144, 148);
			this.Value5Label.Name = "Value5Label";
			this.Value5Label.Text = "Value 5";
			// 
			// Value4Label
			// 
			this.Value4Label.Location = new System.Drawing.Point(144, 120);
			this.Value4Label.Name = "Value4Label";
			this.Value4Label.Text = "Value 4";
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(6, 16);
			this.label1.Name = "label1";
			this.label1.Text = "Hover over item to view details";
			// 
			// ZoomBox
			// 
			this.ZoomBox.Location = new System.Drawing.Point(196, 16);
			this.ZoomBox.Name = "ZoomBox";
			this.ZoomBox.Size = new System.Drawing.Size(64, 64);
			this.ZoomBox.TabIndex = 6;
			this.ZoomBox.TabStop = false;
			this.ZoomBox.Text = "Details";
			// 
			// Value3Label
			// 
			this.Value3Label.Location = new System.Drawing.Point(144, 92);
			this.Value3Label.Name = "Value3Label";
			this.Value3Label.Text = "Value 3";
			// 
			// Value2Label
			// 
			this.Value2Label.Location = new System.Drawing.Point(10, 148);
			this.Value2Label.Name = "Value2Label";
			this.Value2Label.Text = "Value 2";
			// 
			// ValueLabel
			// 
			this.ValueLabel.Location = new System.Drawing.Point(10, 120);
			this.ValueLabel.Name = "ValueLabel";
			this.ValueLabel.Text = "Value 1";
			// 
			// AddressLabel
			// 
			this.AddressLabel.Location = new System.Drawing.Point(10, 92);
			this.AddressLabel.Name = "AddressLabel";
			this.AddressLabel.Text = "Address";
			// 
			// SpriteViewerBox
			// 
			this.SpriteViewerBox.Controls.Add(this.SpriteView);
			this.SpriteViewerBox.Location = new System.Drawing.Point(293, 212);
			this.SpriteViewerBox.Name = "SpriteViewerBox";
			this.SpriteViewerBox.Size = new System.Drawing.Size(272, 124);
			this.SpriteViewerBox.TabIndex = 5;
			this.SpriteViewerBox.TabStop = false;
			this.SpriteViewerBox.Text = "Sprites";
			// 
			// SpriteView
			// 
			this.SpriteView.BackColor = System.Drawing.Color.Transparent;
			this.SpriteView.ContextMenuStrip = this.SpriteContext;
			this.SpriteView.Location = new System.Drawing.Point(6, 18);
			this.SpriteView.Name = "SpriteView";
			this.SpriteView.Size = new System.Drawing.Size(257, 100);
			this.SpriteView.Sprites = ((System.Drawing.Bitmap)(resources.GetObject("SpriteView.Sprites")));
			this.SpriteView.TabIndex = 0;
			this.SpriteView.Text = "Sprites";
			this.SpriteView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.SpriteView_MouseClick);
			this.SpriteView.MouseEnter += new System.EventHandler(this.SpriteView_MouseEnter);
			this.SpriteView.MouseLeave += new System.EventHandler(this.SpriteView_MouseLeave);
			this.SpriteView.MouseMove += new System.Windows.Forms.MouseEventHandler(this.SpriteView_MouseMove);
			// 
			// SpriteContext
			// 
			this.SpriteContext.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.SpriteContext.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SpriteSaveImageMenuItem,
            this.SpriteImageToClipboardMenuItem,
            this.SpriteRefreshMenuItem});
			this.SpriteContext.Name = "SpriteContext";
			this.SpriteContext.Size = new System.Drawing.Size(166, 70);
			// 
			// SpriteSaveImageMenuItem
			// 
			this.SpriteSaveImageMenuItem.Text = "&Save Image...";
			this.SpriteSaveImageMenuItem.Click += new System.EventHandler(this.SaveSpriteScreenshotMenuItem_Click);
			// 
			// SpriteImageToClipboardMenuItem
			// 
			this.SpriteImageToClipboardMenuItem.Text = "Image to &Clipboard";
			this.SpriteImageToClipboardMenuItem.Click += new System.EventHandler(this.CopySpriteToClipboardMenuItem_Click);
			// 
			// SpriteRefreshMenuItem
			// 
			this.SpriteRefreshMenuItem.Text = "&Refresh";
			this.SpriteRefreshMenuItem.Click += new System.EventHandler(this.SpriteRefreshMenuItem_Click);
			// 
			// txtScanline
			// 
			this.txtScanline.Location = new System.Drawing.Point(7, 16);
			this.txtScanline.Name = "txtScanline";
			this.txtScanline.Size = new System.Drawing.Size(60, 20);
			this.txtScanline.TabIndex = 6;
			this.txtScanline.Text = "0";
			this.txtScanline.TextChanged += new System.EventHandler(this.ScanlineTextBox_TextChanged);
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.txtScanline);
			this.groupBox1.Location = new System.Drawing.Point(12, 37);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(75, 52);
			this.groupBox1.TabIndex = 8;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Scanline";
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.label4);
			this.groupBox2.Controls.Add(this.label3);
			this.groupBox2.Controls.Add(this.RefreshRate);
			this.groupBox2.Location = new System.Drawing.Point(93, 37);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(191, 52);
			this.groupBox2.TabIndex = 9;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Refresh";
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(140, 20);
			this.label4.Name = "label4";
			this.label4.Text = "Less";
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(6, 19);
			this.label3.Name = "label3";
			this.label3.Text = "More";
			// 
			// RefreshRate
			// 
			this.RefreshRate.AutoSize = false;
			this.RefreshRate.LargeChange = 2;
			this.RefreshRate.Location = new System.Drawing.Point(39, 15);
			this.RefreshRate.Maximum = 8;
			this.RefreshRate.Minimum = 1;
			this.RefreshRate.Name = "RefreshRate";
			this.RefreshRate.Size = new System.Drawing.Size(104, 31);
			this.RefreshRate.TabIndex = 0;
			this.RefreshRate.TickFrequency = 8;
			this.RefreshRate.Value = 1;
			// 
			// NesPPUMenu
			// 
			this.NesPPUMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.NesPPUMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu,
            this.PatternSubMenu,
            this.SettingsSubMenu});
			this.NesPPUMenu.TabIndex = 10;
			// 
			// FileSubMenu
			// 
			this.FileSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SavePaletteScreenshotMenuItem,
            this.SavePatternScreenshotMenuItem,
            this.SaveSpriteScreenshotMenuItem,
            this.toolStripSeparator1,
            this.CopyPaletteToClipboardMenuItem,
            this.CopyPatternToClipboardMenuItem,
            this.CopySpriteToClipboardMenuItem});
			this.FileSubMenu.Text = "&File";
			// 
			// SavePaletteScreenshotMenuItem
			// 
			this.SavePaletteScreenshotMenuItem.Text = "Save Palette Screenshot...";
			this.SavePaletteScreenshotMenuItem.Click += new System.EventHandler(this.SavePaletteScreenshotMenuItem_Click);
			// 
			// SavePatternScreenshotMenuItem
			// 
			this.SavePatternScreenshotMenuItem.Text = "Save Pattern Screenshot...";
			this.SavePatternScreenshotMenuItem.Click += new System.EventHandler(this.SavePatternScreenshotMenuItem_Click);
			// 
			// SaveSpriteScreenshotMenuItem
			// 
			this.SaveSpriteScreenshotMenuItem.Text = "Save Sprite Screenshot...";
			this.SaveSpriteScreenshotMenuItem.Click += new System.EventHandler(this.SaveSpriteScreenshotMenuItem_Click);
			// 
			// CopyPaletteToClipboardMenuItem
			// 
			this.CopyPaletteToClipboardMenuItem.Text = "Copy Palette to Clipboard";
			this.CopyPaletteToClipboardMenuItem.Click += new System.EventHandler(this.CopyPaletteToClipboardMenuItem_Click);
			// 
			// CopyPatternToClipboardMenuItem
			// 
			this.CopyPatternToClipboardMenuItem.Text = "Copy Pattern to Clipboard";
			this.CopyPatternToClipboardMenuItem.Click += new System.EventHandler(this.CopyPatternToClipboardMenuItem_Click);
			// 
			// CopySpriteToClipboardMenuItem
			// 
			this.CopySpriteToClipboardMenuItem.Text = "Copy Sprite to Clipboard";
			this.CopySpriteToClipboardMenuItem.Click += new System.EventHandler(this.CopySpriteToClipboardMenuItem_Click);
			// 
			// PatternSubMenu
			// 
			this.PatternSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Table0PaletteSubMenu,
            this.Table1PaletteSubMenu});
			this.PatternSubMenu.Text = "&Pattern";
			// 
			// Table0PaletteSubMenu
			// 
			this.Table0PaletteSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Table0P0MenuItem,
            this.Table0P1MenuItem,
            this.Table0P2MenuItem,
            this.Table0P3MenuItem,
            this.Table0P4MenuItem,
            this.Table0P5MenuItem,
            this.Table0P6MenuItem,
            this.Table0P7MenuItem});
			this.Table0PaletteSubMenu.Text = "Table 0 Palette";
			this.Table0PaletteSubMenu.DropDownOpened += new System.EventHandler(this.Table0PaletteSubMenu_DropDownOpened);
			// 
			// Table0P0MenuItem
			// 
			this.Table0P0MenuItem.Text = "0";
			this.Table0P0MenuItem.Click += new System.EventHandler(this.Palette_Click);
			// 
			// Table0P1MenuItem
			// 
			this.Table0P1MenuItem.Text = "1";
			this.Table0P1MenuItem.Click += new System.EventHandler(this.Palette_Click);
			// 
			// Table0P2MenuItem
			// 
			this.Table0P2MenuItem.Text = "2";
			this.Table0P2MenuItem.Click += new System.EventHandler(this.Palette_Click);
			// 
			// Table0P3MenuItem
			// 
			this.Table0P3MenuItem.Text = "3";
			this.Table0P3MenuItem.Click += new System.EventHandler(this.Palette_Click);
			// 
			// Table0P4MenuItem
			// 
			this.Table0P4MenuItem.Text = "4";
			this.Table0P4MenuItem.Click += new System.EventHandler(this.Palette_Click);
			// 
			// Table0P5MenuItem
			// 
			this.Table0P5MenuItem.Text = "5";
			this.Table0P5MenuItem.Click += new System.EventHandler(this.Palette_Click);
			// 
			// Table0P6MenuItem
			// 
			this.Table0P6MenuItem.Text = "6";
			this.Table0P6MenuItem.Click += new System.EventHandler(this.Palette_Click);
			// 
			// Table0P7MenuItem
			// 
			this.Table0P7MenuItem.Text = "7";
			this.Table0P7MenuItem.Click += new System.EventHandler(this.Palette_Click);
			// 
			// Table1PaletteSubMenu
			// 
			this.Table1PaletteSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Table1P0MenuItem,
            this.Table1P1MenuItem,
            this.Table1P2MenuItem,
            this.Table1P3MenuItem,
            this.Table1P4MenuItem,
            this.Table1P5MenuItem,
            this.Table1P6MenuItem,
            this.Table1P7MenuItem});
			this.Table1PaletteSubMenu.Text = "Table 1 Palette";
			this.Table1PaletteSubMenu.DropDownOpened += new System.EventHandler(this.Table1PaletteSubMenu_DropDownOpened);
			// 
			// Table1P0MenuItem
			// 
			this.Table1P0MenuItem.Text = "0";
			this.Table1P0MenuItem.Click += new System.EventHandler(this.Palette_Click);
			// 
			// Table1P1MenuItem
			// 
			this.Table1P1MenuItem.Text = "1";
			this.Table1P1MenuItem.Click += new System.EventHandler(this.Palette_Click);
			// 
			// Table1P2MenuItem
			// 
			this.Table1P2MenuItem.Text = "2";
			this.Table1P2MenuItem.Click += new System.EventHandler(this.Palette_Click);
			// 
			// Table1P3MenuItem
			// 
			this.Table1P3MenuItem.Text = "3";
			this.Table1P3MenuItem.Click += new System.EventHandler(this.Palette_Click);
			// 
			// Table1P4MenuItem
			// 
			this.Table1P4MenuItem.Text = "4";
			this.Table1P4MenuItem.Click += new System.EventHandler(this.Palette_Click);
			// 
			// Table1P5MenuItem
			// 
			this.Table1P5MenuItem.Text = "5";
			this.Table1P5MenuItem.Click += new System.EventHandler(this.Palette_Click);
			// 
			// Table1P6MenuItem
			// 
			this.Table1P6MenuItem.Text = "6";
			this.Table1P6MenuItem.Click += new System.EventHandler(this.Palette_Click);
			// 
			// Table1P7MenuItem
			// 
			this.Table1P7MenuItem.Text = "7";
			this.Table1P7MenuItem.Click += new System.EventHandler(this.Palette_Click);
			// 
			// SettingsSubMenu
			// 
			this.SettingsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cHRROMTileViewerToolStripMenuItem});
			this.SettingsSubMenu.Text = "&Settings";
			this.SettingsSubMenu.DropDownOpened += new System.EventHandler(this.SettingsSubMenu_DropDownOpened);
			// 
			// cHRROMTileViewerToolStripMenuItem
			// 
			this.cHRROMTileViewerToolStripMenuItem.Text = "CHR ROM Tile Viewer";
			this.cHRROMTileViewerToolStripMenuItem.Click += new System.EventHandler(this.ChrROMTileViewerToolStripMenuItem_Click);
			// 
			// NesPPUStatusBar
			// 
			this.NesPPUStatusBar.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.NesPPUStatusBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
			this.NesPPUStatusBar.Location = new System.Drawing.Point(0, 349);
			this.NesPPUStatusBar.Name = "NesPPUStatusBar";
			this.NesPPUStatusBar.SizingGrip = false;
			this.NesPPUStatusBar.TabIndex = 11;
			// 
			// toolStripStatusLabel1
			// 
			this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
			this.toolStripStatusLabel1.Size = new System.Drawing.Size(319, 17);
			this.toolStripStatusLabel1.Text = "Use CTRL+C to copy the pane under the mouse to the clipboard.";
			// 
			// Messagetimer
			// 
			this.Messagetimer.Interval = 5000;
			this.Messagetimer.Tick += new System.EventHandler(this.MessageTimer_Tick);
			// 
			// CHRROMGroup
			// 
			this.CHRROMGroup.Controls.Add(this.label5);
			this.CHRROMGroup.Controls.Add(this.numericUpDownCHRROMBank);
			this.CHRROMGroup.Controls.Add(this.CHRROMView);
			this.CHRROMGroup.Location = new System.Drawing.Point(574, 37);
			this.CHRROMGroup.Name = "CHRROMGroup";
			this.CHRROMGroup.Size = new System.Drawing.Size(272, 299);
			this.CHRROMGroup.TabIndex = 12;
			this.CHRROMGroup.TabStop = false;
			this.CHRROMGroup.Text = "CHR ROM Tiles";
			// 
			// label5
			// 
			this.label5.Location = new System.Drawing.Point(6, 156);
			this.label5.Name = "label5";
			this.label5.Text = "Bank:";
			// 
			// numericUpDownCHRROMBank
			// 
			this.numericUpDownCHRROMBank.Location = new System.Drawing.Point(47, 154);
			this.numericUpDownCHRROMBank.Name = "numericUpDownCHRROMBank";
			this.numericUpDownCHRROMBank.Size = new System.Drawing.Size(121, 20);
			this.numericUpDownCHRROMBank.TabIndex = 1;
			this.numericUpDownCHRROMBank.ValueChanged += new System.EventHandler(this.NumericUpDownChrRomBank_ValueChanged);
			// 
			// CHRROMView
			// 
			this.CHRROMView.BackColor = System.Drawing.Color.Transparent;
			this.CHRROMView.Location = new System.Drawing.Point(7, 20);
			this.CHRROMView.Name = "CHRROMView";
			this.CHRROMView.Pal0 = 0;
			this.CHRROMView.Pal1 = 0;
			this.CHRROMView.Pattern = ((System.Drawing.Bitmap)(resources.GetObject("CHRROMView.Pattern")));
			this.CHRROMView.Size = new System.Drawing.Size(256, 128);
			this.CHRROMView.TabIndex = 0;
			this.CHRROMView.Text = "patternViewer1";
			// 
			// NesPPU
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(854, 371);
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
			this.MinimumSize = new System.Drawing.Size(577, 366);
			this.Name = "NesPPU";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
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