using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	partial class SmsVdpViewer
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SmsVdpViewer));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.bmpViewTiles = new BizHawk.Client.EmuHawk.BmpView();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.bmpViewPalette = new BizHawk.Client.EmuHawk.BmpView();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.bmpViewBG = new BizHawk.Client.EmuHawk.BmpView();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.menuStrip1 = new BizHawk.WinForms.Controls.MenuStripEx();
            this.FileSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.saveTilesScreenshotToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.savePalettesScrenshotToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.saveBGScreenshotToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.bmpViewTiles);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // bmpViewTiles
            // 
            resources.ApplyResources(this.bmpViewTiles, "bmpViewTiles");
            this.bmpViewTiles.BackColor = System.Drawing.Color.Transparent;
            this.bmpViewTiles.Name = "bmpViewTiles";
            // 
            // groupBox2
            // 
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Controls.Add(this.bmpViewPalette);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // bmpViewPalette
            // 
            resources.ApplyResources(this.bmpViewPalette, "bmpViewPalette");
            this.bmpViewPalette.BackColor = System.Drawing.Color.Transparent;
            this.bmpViewPalette.Name = "bmpViewPalette";
            this.bmpViewPalette.MouseClick += new System.Windows.Forms.MouseEventHandler(this.BmpViewPalette_MouseClick);
            // 
            // groupBox3
            // 
            resources.ApplyResources(this.groupBox3, "groupBox3");
            this.groupBox3.Controls.Add(this.bmpViewBG);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.TabStop = false;
            // 
            // bmpViewBG
            // 
            resources.ApplyResources(this.bmpViewBG, "bmpViewBG");
            this.bmpViewBG.BackColor = System.Drawing.Color.Transparent;
            this.bmpViewBG.Name = "bmpViewBG";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // menuStrip1
            // 
            resources.ApplyResources(this.menuStrip1, "menuStrip1");
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu});
            // 
            // FileSubMenu
            // 
            resources.ApplyResources(this.FileSubMenu, "FileSubMenu");
            this.FileSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveTilesScreenshotToolStripMenuItem,
            this.savePalettesScrenshotToolStripMenuItem,
            this.saveBGScreenshotToolStripMenuItem});
            // 
            // saveTilesScreenshotToolStripMenuItem
            // 
            resources.ApplyResources(this.saveTilesScreenshotToolStripMenuItem, "saveTilesScreenshotToolStripMenuItem");
            this.saveTilesScreenshotToolStripMenuItem.Click += new System.EventHandler(this.SaveTilesScreenshotToolStripMenuItem_Click);
            // 
            // savePalettesScrenshotToolStripMenuItem
            // 
            resources.ApplyResources(this.savePalettesScrenshotToolStripMenuItem, "savePalettesScrenshotToolStripMenuItem");
            this.savePalettesScrenshotToolStripMenuItem.Click += new System.EventHandler(this.SavePalettesScreenshotMenuItem_Click);
            // 
            // saveBGScreenshotToolStripMenuItem
            // 
            resources.ApplyResources(this.saveBGScreenshotToolStripMenuItem, "saveBGScreenshotToolStripMenuItem");
            this.saveBGScreenshotToolStripMenuItem.Click += new System.EventHandler(this.SaveBgScreenshotMenuItem_Click);
            // 
            // SmsVdpViewer
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label1);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.menuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.KeyPreview = true;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "SmsVdpViewer";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.VDPViewer_KeyDown);
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private BmpView bmpViewTiles;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.GroupBox groupBox2;
		private BmpView bmpViewPalette;
		private System.Windows.Forms.GroupBox groupBox3;
		private BmpView bmpViewBG;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private MenuStripEx menuStrip1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FileSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx saveTilesScreenshotToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx savePalettesScrenshotToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx saveBGScreenshotToolStripMenuItem;
	}
}