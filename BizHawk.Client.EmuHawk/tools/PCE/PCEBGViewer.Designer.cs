namespace BizHawk.Client.EmuHawk
{
	partial class PceBgViewer
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PceBgViewer));
			this.PceBgViewerMenu = new MenuStripEx();
			this.ViewerSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.VDC1MenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.VDC2MenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.ExitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.canvas = new BizHawk.Client.EmuHawk.PCEBGCanvas();
			this.groupBox5 = new System.Windows.Forms.GroupBox();
			this.label7 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.RefreshRate = new System.Windows.Forms.TrackBar();
			this.groupBox4 = new System.Windows.Forms.GroupBox();
			this.PaletteLabel = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.PPUAddressLabel = new System.Windows.Forms.Label();
			this.XYLabel = new System.Windows.Forms.Label();
			this.TileIDLabel = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.PceBgViewerMenu.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.groupBox5.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.RefreshRate)).BeginInit();
			this.groupBox4.SuspendLayout();
			this.SuspendLayout();
			// 
			// PceBgViewerMenu
			// 
			this.PceBgViewerMenu.ClickThrough = true;
			this.PceBgViewerMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ViewerSubMenu});
			this.PceBgViewerMenu.Location = new System.Drawing.Point(0, 0);
			this.PceBgViewerMenu.Name = "PceBgViewerMenu";
			this.PceBgViewerMenu.Size = new System.Drawing.Size(676, 24);
			this.PceBgViewerMenu.TabIndex = 2;
			this.PceBgViewerMenu.Text = "menuStrip1";
			// 
			// ViewerSubMenu
			// 
			this.ViewerSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.VDC1MenuItem,
            this.VDC2MenuItem,
            this.toolStripSeparator1,
            this.ExitMenuItem});
			this.ViewerSubMenu.Name = "ViewerSubMenu";
			this.ViewerSubMenu.Size = new System.Drawing.Size(51, 20);
			this.ViewerSubMenu.Text = "&Viewer";
			this.ViewerSubMenu.DropDownOpened += new System.EventHandler(this.FileSubMenu_DropDownOpened);
			// 
			// VDC1MenuItem
			// 
			this.VDC1MenuItem.Name = "VDC1MenuItem";
			this.VDC1MenuItem.Size = new System.Drawing.Size(152, 22);
			this.VDC1MenuItem.Text = "VDC&1";
			this.VDC1MenuItem.Click += new System.EventHandler(this.VDC1MenuItem_Click);
			// 
			// VDC2MenuItem
			// 
			this.VDC2MenuItem.Name = "VDC2MenuItem";
			this.VDC2MenuItem.Size = new System.Drawing.Size(152, 22);
			this.VDC2MenuItem.Text = "VCD&2";
			this.VDC2MenuItem.Click += new System.EventHandler(this.VDC2MenuItem_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(149, 6);
			// 
			// ExitMenuItem
			// 
			this.ExitMenuItem.Name = "ExitMenuItem";
			this.ExitMenuItem.ShortcutKeyDisplayString = "Alt+F4";
			this.ExitMenuItem.Size = new System.Drawing.Size(152, 22);
			this.ExitMenuItem.Text = "E&xit";
			this.ExitMenuItem.Click += new System.EventHandler(this.ExitMenuItem_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.canvas);
			this.groupBox1.Location = new System.Drawing.Point(12, 27);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(529, 536);
			this.groupBox1.TabIndex = 3;
			this.groupBox1.TabStop = false;
			// 
			// canvas
			// 
			this.canvas.BackColor = System.Drawing.SystemColors.ControlLightLight;
			this.canvas.Location = new System.Drawing.Point(8, 15);
			this.canvas.Name = "canvas";
			this.canvas.Size = new System.Drawing.Size(512, 513);
			this.canvas.TabIndex = 0;
			this.canvas.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Canvas_MouseMove);
			// 
			// groupBox5
			// 
			this.groupBox5.Controls.Add(this.label7);
			this.groupBox5.Controls.Add(this.label6);
			this.groupBox5.Controls.Add(this.RefreshRate);
			this.groupBox5.Location = new System.Drawing.Point(554, 122);
			this.groupBox5.Name = "groupBox5";
			this.groupBox5.Size = new System.Drawing.Size(108, 236);
			this.groupBox5.TabIndex = 15;
			this.groupBox5.TabStop = false;
			this.groupBox5.Text = "Refresh";
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(7, 186);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(31, 13);
			this.label7.TabIndex = 2;
			this.label7.Text = "More";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(7, 32);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(29, 13);
			this.label6.TabIndex = 1;
			this.label6.Text = "Less";
			// 
			// RefreshRate
			// 
			this.RefreshRate.LargeChange = 2;
			this.RefreshRate.Location = new System.Drawing.Point(9, 47);
			this.RefreshRate.Maximum = 16;
			this.RefreshRate.Minimum = 1;
			this.RefreshRate.Name = "RefreshRate";
			this.RefreshRate.Orientation = System.Windows.Forms.Orientation.Vertical;
			this.RefreshRate.Size = new System.Drawing.Size(42, 136);
			this.RefreshRate.TabIndex = 0;
			this.RefreshRate.TickFrequency = 4;
			this.RefreshRate.Value = 16;
			// 
			// groupBox4
			// 
			this.groupBox4.Controls.Add(this.PaletteLabel);
			this.groupBox4.Controls.Add(this.label5);
			this.groupBox4.Controls.Add(this.PPUAddressLabel);
			this.groupBox4.Controls.Add(this.XYLabel);
			this.groupBox4.Controls.Add(this.TileIDLabel);
			this.groupBox4.Controls.Add(this.label2);
			this.groupBox4.Controls.Add(this.label1);
			this.groupBox4.Location = new System.Drawing.Point(554, 28);
			this.groupBox4.Name = "groupBox4";
			this.groupBox4.Size = new System.Drawing.Size(108, 87);
			this.groupBox4.TabIndex = 16;
			this.groupBox4.TabStop = false;
			this.groupBox4.Text = "Properties";
			// 
			// PaletteLabel
			// 
			this.PaletteLabel.AutoSize = true;
			this.PaletteLabel.Location = new System.Drawing.Point(64, 60);
			this.PaletteLabel.Name = "PaletteLabel";
			this.PaletteLabel.Size = new System.Drawing.Size(22, 13);
			this.PaletteLabel.TabIndex = 9;
			this.PaletteLabel.Text = "     ";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(6, 60);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(43, 13);
			this.label5.TabIndex = 8;
			this.label5.Text = "Palette:";
			// 
			// PPUAddressLabel
			// 
			this.PPUAddressLabel.AutoSize = true;
			this.PPUAddressLabel.Location = new System.Drawing.Point(64, 60);
			this.PPUAddressLabel.Name = "PPUAddressLabel";
			this.PPUAddressLabel.Size = new System.Drawing.Size(22, 13);
			this.PPUAddressLabel.TabIndex = 5;
			this.PPUAddressLabel.Text = "     ";
			// 
			// XYLabel
			// 
			this.XYLabel.AutoSize = true;
			this.XYLabel.Location = new System.Drawing.Point(64, 43);
			this.XYLabel.Name = "XYLabel";
			this.XYLabel.Size = new System.Drawing.Size(22, 13);
			this.XYLabel.TabIndex = 4;
			this.XYLabel.Text = "     ";
			// 
			// TileIDLabel
			// 
			this.TileIDLabel.AutoSize = true;
			this.TileIDLabel.Location = new System.Drawing.Point(64, 26);
			this.TileIDLabel.Name = "TileIDLabel";
			this.TileIDLabel.Size = new System.Drawing.Size(22, 13);
			this.TileIDLabel.TabIndex = 3;
			this.TileIDLabel.Text = "     ";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(6, 43);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(35, 13);
			this.label2.TabIndex = 1;
			this.label2.Text = "X / Y:";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(6, 26);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(41, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Tile ID:";
			// 
			// PceBgViewer
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(676, 575);
			this.Controls.Add(this.groupBox4);
			this.Controls.Add(this.groupBox5);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.PceBgViewerMenu);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.PceBgViewerMenu;
			this.Name = "PceBgViewer";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Background Viewer";
			this.Load += new System.EventHandler(this.PceBgViewer_Load);
			this.PceBgViewerMenu.ResumeLayout(false);
			this.PceBgViewerMenu.PerformLayout();
			this.groupBox1.ResumeLayout(false);
			this.groupBox5.ResumeLayout(false);
			this.groupBox5.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.RefreshRate)).EndInit();
			this.groupBox4.ResumeLayout(false);
			this.groupBox4.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private PCEBGCanvas canvas;
        private MenuStripEx PceBgViewerMenu;
		private System.Windows.Forms.ToolStripMenuItem ViewerSubMenu;
        private System.Windows.Forms.ToolStripMenuItem ExitMenuItem;
		private System.Windows.Forms.ToolStripMenuItem VDC1MenuItem;
		private System.Windows.Forms.ToolStripMenuItem VDC2MenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.GroupBox groupBox5;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.TrackBar RefreshRate;
		private System.Windows.Forms.GroupBox groupBox4;
		private System.Windows.Forms.Label PaletteLabel;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label PPUAddressLabel;
		private System.Windows.Forms.Label XYLabel;
		private System.Windows.Forms.Label TileIDLabel;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
	}
}