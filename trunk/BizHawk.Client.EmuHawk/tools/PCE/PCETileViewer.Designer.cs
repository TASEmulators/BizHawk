namespace BizHawk.Client.EmuHawk
{
	partial class PCETileViewer
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PCETileViewer));
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.bmpViewBGPal = new BizHawk.Client.EmuHawk.BmpView();
			this.bmpViewBG = new BizHawk.Client.EmuHawk.BmpView();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.bmpViewSPPal = new BizHawk.Client.EmuHawk.BmpView();
			this.bmpViewSP = new BizHawk.Client.EmuHawk.BmpView();
			this.checkBoxVDC2 = new System.Windows.Forms.CheckBox();
			this.label1 = new System.Windows.Forms.Label();
			this.menuStrip1 = new MenuStripEx();
			this.FileSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.saveBackgroundScreenshotToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveSpriteScreenshotToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.menuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.bmpViewBGPal);
			this.groupBox1.Controls.Add(this.bmpViewBG);
			this.groupBox1.Location = new System.Drawing.Point(12, 25);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(786, 281);
			this.groupBox1.TabIndex = 4;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Background";
			// 
			// bmpViewBGPal
			// 
			this.bmpViewBGPal.Location = new System.Drawing.Point(524, 19);
			this.bmpViewBGPal.Name = "bmpViewBGPal";
			this.bmpViewBGPal.Size = new System.Drawing.Size(256, 256);
			this.bmpViewBGPal.TabIndex = 3;
			this.bmpViewBGPal.Text = "bmpView2";
			this.bmpViewBGPal.MouseClick += new System.Windows.Forms.MouseEventHandler(this.bmpViewBGPal_MouseClick);
			// 
			// bmpViewBG
			// 
			this.bmpViewBG.Location = new System.Drawing.Point(6, 19);
			this.bmpViewBG.Name = "bmpViewBG";
			this.bmpViewBG.Size = new System.Drawing.Size(512, 256);
			this.bmpViewBG.TabIndex = 2;
			this.bmpViewBG.Text = "bmpView1";
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.bmpViewSPPal);
			this.groupBox2.Controls.Add(this.bmpViewSP);
			this.groupBox2.Location = new System.Drawing.Point(12, 312);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(786, 281);
			this.groupBox2.TabIndex = 5;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Sprite";
			// 
			// bmpViewSPPal
			// 
			this.bmpViewSPPal.Location = new System.Drawing.Point(524, 19);
			this.bmpViewSPPal.Name = "bmpViewSPPal";
			this.bmpViewSPPal.Size = new System.Drawing.Size(256, 256);
			this.bmpViewSPPal.TabIndex = 1;
			this.bmpViewSPPal.Text = "bmpView4";
			this.bmpViewSPPal.MouseClick += new System.Windows.Forms.MouseEventHandler(this.bmpViewSPPal_MouseClick);
			// 
			// bmpViewSP
			// 
			this.bmpViewSP.Location = new System.Drawing.Point(6, 19);
			this.bmpViewSP.Name = "bmpViewSP";
			this.bmpViewSP.Size = new System.Drawing.Size(512, 256);
			this.bmpViewSP.TabIndex = 0;
			this.bmpViewSP.Text = "bmpView3";
			// 
			// checkBoxVDC2
			// 
			this.checkBoxVDC2.AutoSize = true;
			this.checkBoxVDC2.Location = new System.Drawing.Point(12, 599);
			this.checkBoxVDC2.Name = "checkBoxVDC2";
			this.checkBoxVDC2.Size = new System.Drawing.Size(57, 17);
			this.checkBoxVDC2.TabIndex = 6;
			this.checkBoxVDC2.Text = "VDC 2";
			this.checkBoxVDC2.UseVisualStyleBackColor = true;
			this.checkBoxVDC2.CheckedChanged += new System.EventHandler(this.checkBoxVDC2_CheckedChanged);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(504, 599);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(294, 13);
			this.label1.TabIndex = 7;
			this.label1.Text = "CTRL + C copies the pane under the mouse to the clipboard.";
			// 
			// menuStrip1
			// 
			this.menuStrip1.ClickThrough = true;
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(810, 24);
			this.menuStrip1.TabIndex = 8;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// FileSubMenu
			// 
			this.FileSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveBackgroundScreenshotToolStripMenuItem,
            this.saveSpriteScreenshotToolStripMenuItem,
            this.toolStripSeparator1,
            this.closeToolStripMenuItem});
			this.FileSubMenu.Name = "FileSubMenu";
			this.FileSubMenu.Size = new System.Drawing.Size(35, 20);
			this.FileSubMenu.Text = "&File";
			// 
			// saveBackgroundScreenshotToolStripMenuItem
			// 
			this.saveBackgroundScreenshotToolStripMenuItem.Name = "saveBackgroundScreenshotToolStripMenuItem";
			this.saveBackgroundScreenshotToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
			this.saveBackgroundScreenshotToolStripMenuItem.Text = "Save BG Screenshot...";
			this.saveBackgroundScreenshotToolStripMenuItem.Click += new System.EventHandler(this.saveBackgroundScreenshotToolStripMenuItem_Click);
			// 
			// saveSpriteScreenshotToolStripMenuItem
			// 
			this.saveSpriteScreenshotToolStripMenuItem.Name = "saveSpriteScreenshotToolStripMenuItem";
			this.saveSpriteScreenshotToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
			this.saveSpriteScreenshotToolStripMenuItem.Text = "Save Sprite Screenshot...";
			this.saveSpriteScreenshotToolStripMenuItem.Click += new System.EventHandler(this.saveSpriteScreenshotToolStripMenuItem_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(195, 6);
			// 
			// closeToolStripMenuItem
			// 
			this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
			this.closeToolStripMenuItem.ShortcutKeyDisplayString = "Alt+F4";
			this.closeToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
			this.closeToolStripMenuItem.Text = "&Close";
			this.closeToolStripMenuItem.Click += new System.EventHandler(this.closeToolStripMenuItem_Click);
			// 
			// PCETileViewer
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(810, 624);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.checkBoxVDC2);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.menuStrip1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.KeyPreview = true;
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "PCETileViewer";
			this.Text = "Tile Viewer";
			this.Load += new System.EventHandler(this.PCETileViewer_Load);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PCETileViewer_KeyDown);
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
		private System.Windows.Forms.Label label1;
		private MenuStripEx menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem FileSubMenu;
		private System.Windows.Forms.ToolStripMenuItem closeToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveBackgroundScreenshotToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveSpriteScreenshotToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
	}
}