using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	partial class GenVdpViewer
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GenVdpViewer));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.bmpViewTiles = new BizHawk.Client.EmuHawk.BmpView();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.bmpViewPal = new BizHawk.Client.EmuHawk.BmpView();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.bmpViewNTW = new BizHawk.Client.EmuHawk.BmpView();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.bmpViewNTA = new BizHawk.Client.EmuHawk.BmpView();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.bmpViewNTB = new BizHawk.Client.EmuHawk.BmpView();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.menuStrip1 = new BizHawk.WinForms.Controls.MenuStripEx();
            this.fileToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.saveBGAScreenshotToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.saveBGBScreenshotToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.saveTilesScreenshotToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.saveWindowScreenshotToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.savePaletteScreenshotToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox5.SuspendLayout();
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
            this.groupBox2.Controls.Add(this.bmpViewPal);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // bmpViewPal
            // 
            resources.ApplyResources(this.bmpViewPal, "bmpViewPal");
            this.bmpViewPal.BackColor = System.Drawing.Color.Transparent;
            this.bmpViewPal.Name = "bmpViewPal";
            this.bmpViewPal.MouseClick += new System.Windows.Forms.MouseEventHandler(this.BmpViewPal_MouseClick);
            // 
            // groupBox3
            // 
            resources.ApplyResources(this.groupBox3, "groupBox3");
            this.groupBox3.Controls.Add(this.bmpViewNTW);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.TabStop = false;
            // 
            // bmpViewNTW
            // 
            resources.ApplyResources(this.bmpViewNTW, "bmpViewNTW");
            this.bmpViewNTW.BackColor = System.Drawing.Color.Transparent;
            this.bmpViewNTW.Name = "bmpViewNTW";
            // 
            // groupBox4
            // 
            resources.ApplyResources(this.groupBox4, "groupBox4");
            this.groupBox4.Controls.Add(this.bmpViewNTA);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.TabStop = false;
            // 
            // bmpViewNTA
            // 
            resources.ApplyResources(this.bmpViewNTA, "bmpViewNTA");
            this.bmpViewNTA.BackColor = System.Drawing.Color.Transparent;
            this.bmpViewNTA.Name = "bmpViewNTA";
            // 
            // groupBox5
            // 
            resources.ApplyResources(this.groupBox5, "groupBox5");
            this.groupBox5.Controls.Add(this.bmpViewNTB);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.TabStop = false;
            // 
            // bmpViewNTB
            // 
            resources.ApplyResources(this.bmpViewNTB, "bmpViewNTB");
            this.bmpViewNTB.BackColor = System.Drawing.Color.Transparent;
            this.bmpViewNTB.Name = "bmpViewNTB";
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
            this.fileToolStripMenuItem});
            // 
            // fileToolStripMenuItem
            // 
            resources.ApplyResources(this.fileToolStripMenuItem, "fileToolStripMenuItem");
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveBGAScreenshotToolStripMenuItem,
            this.saveBGBScreenshotToolStripMenuItem,
            this.saveTilesScreenshotToolStripMenuItem,
            this.saveWindowScreenshotToolStripMenuItem,
            this.savePaletteScreenshotToolStripMenuItem});
            // 
            // saveBGAScreenshotToolStripMenuItem
            // 
            resources.ApplyResources(this.saveBGAScreenshotToolStripMenuItem, "saveBGAScreenshotToolStripMenuItem");
            this.saveBGAScreenshotToolStripMenuItem.Click += new System.EventHandler(this.SaveBGAScreenshotToolStripMenuItem_Click);
            // 
            // saveBGBScreenshotToolStripMenuItem
            // 
            resources.ApplyResources(this.saveBGBScreenshotToolStripMenuItem, "saveBGBScreenshotToolStripMenuItem");
            this.saveBGBScreenshotToolStripMenuItem.Click += new System.EventHandler(this.SaveBGBScreenshotToolStripMenuItem_Click);
            // 
            // saveTilesScreenshotToolStripMenuItem
            // 
            resources.ApplyResources(this.saveTilesScreenshotToolStripMenuItem, "saveTilesScreenshotToolStripMenuItem");
            this.saveTilesScreenshotToolStripMenuItem.Click += new System.EventHandler(this.SaveTilesScreenshotToolStripMenuItem_Click);
            // 
            // saveWindowScreenshotToolStripMenuItem
            // 
            resources.ApplyResources(this.saveWindowScreenshotToolStripMenuItem, "saveWindowScreenshotToolStripMenuItem");
            this.saveWindowScreenshotToolStripMenuItem.Click += new System.EventHandler(this.SaveWindowScreenshotToolStripMenuItem_Click);
            // 
            // savePaletteScreenshotToolStripMenuItem
            // 
            resources.ApplyResources(this.savePaletteScreenshotToolStripMenuItem, "savePaletteScreenshotToolStripMenuItem");
            this.savePaletteScreenshotToolStripMenuItem.Click += new System.EventHandler(this.SavePaletteScreenshotToolStripMenuItem_Click);
            // 
            // GenVdpViewer
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label1);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.menuStrip1);
            this.KeyPreview = true;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "GenVdpViewer";
            this.ShowIcon = false;
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.VDPViewer_KeyDown);
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox4.ResumeLayout(false);
            this.groupBox5.ResumeLayout(false);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private BmpView bmpViewTiles;
		private BmpView bmpViewNTA;
		private BmpView bmpViewNTB;
		private BmpView bmpViewNTW;
		private BmpView bmpViewPal;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.GroupBox groupBox4;
		private System.Windows.Forms.GroupBox groupBox5;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private MenuStripEx menuStrip1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx fileToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx saveBGAScreenshotToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx saveBGBScreenshotToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx saveTilesScreenshotToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx saveWindowScreenshotToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx savePaletteScreenshotToolStripMenuItem;
	}
}