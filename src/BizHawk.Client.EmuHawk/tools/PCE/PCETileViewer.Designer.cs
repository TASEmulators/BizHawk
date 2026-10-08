using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	partial class PceTileViewer
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PceTileViewer));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.bmpViewBGPal = new BizHawk.Client.EmuHawk.BmpView();
            this.bmpViewBG = new BizHawk.Client.EmuHawk.BmpView();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.bmpViewSPPal = new BizHawk.Client.EmuHawk.BmpView();
            this.bmpViewSP = new BizHawk.Client.EmuHawk.BmpView();
            this.checkBoxVDC2 = new System.Windows.Forms.CheckBox();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.menuStrip1 = new BizHawk.WinForms.Controls.MenuStripEx();
            this.FileSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.saveBackgroundScreenshotToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.saveSpriteScreenshotToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.bmpViewBGPal);
            this.groupBox1.Controls.Add(this.bmpViewBG);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // bmpViewBGPal
            // 
            resources.ApplyResources(this.bmpViewBGPal, "bmpViewBGPal");
            this.bmpViewBGPal.BackColor = System.Drawing.Color.Transparent;
            this.bmpViewBGPal.Name = "bmpViewBGPal";
            this.bmpViewBGPal.MouseClick += new System.Windows.Forms.MouseEventHandler(this.BmpViewBGPal_MouseClick);
            // 
            // bmpViewBG
            // 
            resources.ApplyResources(this.bmpViewBG, "bmpViewBG");
            this.bmpViewBG.BackColor = System.Drawing.Color.Transparent;
            this.bmpViewBG.Name = "bmpViewBG";
            // 
            // groupBox2
            // 
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Controls.Add(this.bmpViewSPPal);
            this.groupBox2.Controls.Add(this.bmpViewSP);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // bmpViewSPPal
            // 
            resources.ApplyResources(this.bmpViewSPPal, "bmpViewSPPal");
            this.bmpViewSPPal.BackColor = System.Drawing.Color.Transparent;
            this.bmpViewSPPal.Name = "bmpViewSPPal";
            this.bmpViewSPPal.MouseClick += new System.Windows.Forms.MouseEventHandler(this.BmpViewSPPal_MouseClick);
            // 
            // bmpViewSP
            // 
            resources.ApplyResources(this.bmpViewSP, "bmpViewSP");
            this.bmpViewSP.BackColor = System.Drawing.Color.Transparent;
            this.bmpViewSP.Name = "bmpViewSP";
            // 
            // checkBoxVDC2
            // 
            resources.ApplyResources(this.checkBoxVDC2, "checkBoxVDC2");
            this.checkBoxVDC2.Name = "checkBoxVDC2";
            this.checkBoxVDC2.UseVisualStyleBackColor = true;
            this.checkBoxVDC2.CheckedChanged += new System.EventHandler(this.CheckBoxVDC2_CheckedChanged);
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
            this.saveBackgroundScreenshotToolStripMenuItem,
            this.saveSpriteScreenshotToolStripMenuItem});
            // 
            // saveBackgroundScreenshotToolStripMenuItem
            // 
            resources.ApplyResources(this.saveBackgroundScreenshotToolStripMenuItem, "saveBackgroundScreenshotToolStripMenuItem");
            this.saveBackgroundScreenshotToolStripMenuItem.Click += new System.EventHandler(this.SaveBackgroundScreenshotMenuItem_Click);
            // 
            // saveSpriteScreenshotToolStripMenuItem
            // 
            resources.ApplyResources(this.saveSpriteScreenshotToolStripMenuItem, "saveSpriteScreenshotToolStripMenuItem");
            this.saveSpriteScreenshotToolStripMenuItem.Click += new System.EventHandler(this.SaveSpriteScreenshotMenuItem_Click);
            // 
            // PceTileViewer
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label1);
            this.Controls.Add(this.checkBoxVDC2);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.menuStrip1);
            this.KeyPreview = true;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "PceTileViewer";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PceTileViewer_KeyDown);
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.CheckBox checkBoxVDC2;
		private BmpView bmpViewBGPal;
		private BmpView bmpViewBG;
		private BmpView bmpViewSPPal;
		private BmpView bmpViewSP;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private MenuStripEx menuStrip1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FileSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx saveBackgroundScreenshotToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx saveSpriteScreenshotToolStripMenuItem;
	}
}