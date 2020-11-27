namespace BizHawk.Client.EmuHawk
{
	partial class GBPrinterView
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
			this.paperView = new BmpView();
			this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.paperScroll = new System.Windows.Forms.VScrollBar();
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.saveImageToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.editToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.copyToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(336, 24);
            this.menuStrip1.TabIndex = 2;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveImageToolStripMenuItem});
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // saveImageToolStripMenuItem
            // 
            this.saveImageToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.saveImageToolStripMenuItem.Text = "&Save Image...";
            this.saveImageToolStripMenuItem.Click += new System.EventHandler(this.SaveImageToolStripMenuItem_Click);
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyToolStripMenuItem});
            this.editToolStripMenuItem.Text = "&Edit";
            // 
            // copyToolStripMenuItem
            // 
            this.copyToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
            this.copyToolStripMenuItem.Text = "&Copy";
            this.copyToolStripMenuItem.Click += new System.EventHandler(this.CopyToolStripMenuItem_Click);
			//
			// paperView
			//
			this.paperView.Name = "paperView";
			this.paperView.Location = new System.Drawing.Point(0, 48);
			this.paperView.Size = new System.Drawing.Size(320, 320);
			this.paperView.BackColor = System.Drawing.Color.Black;
			this.paperView.TabIndex = 0;
			//
			// label1
			//
			this.label1.Name = "label1";
			this.label1.Location = new System.Drawing.Point(0, 24);
			this.label1.Text = "Note: the printer is only connected while this window is open.";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			//
			// paperScroll
			//
			this.paperScroll.Name = "paperScroll";
			this.paperScroll.Location = new System.Drawing.Point(320, 48);
			this.paperScroll.Size = new System.Drawing.Size(16, 320);
			this.paperScroll.Minimum = 0;
			this.paperScroll.SmallChange = 8;
			this.paperScroll.LargeChange = 160;
			this.paperScroll.ValueChanged += PaperScroll_ValueChanged;
			// 
			// GBPrinterView
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(336, 358);
            this.Controls.Add(this.menuStrip1);
			this.Controls.Add(this.paperView);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.paperScroll);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MainMenuStrip = this.menuStrip1;
            this.MaximizeBox = false;
            this.Name = "GBPrinterView";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
			this.FormClosed += GBPrinterView_FormClosed;
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.MenuStrip menuStrip1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx fileToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx saveImageToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx editToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx copyToolStripMenuItem;
		private BmpView paperView;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private System.Windows.Forms.VScrollBar paperScroll;
	}
}