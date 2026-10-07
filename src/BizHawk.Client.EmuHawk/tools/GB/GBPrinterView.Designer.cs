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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GBPrinterView));
            this.paperView = new BizHawk.Client.EmuHawk.BmpView();
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
            // paperView
            // 
            resources.ApplyResources(this.paperView, "paperView");
            this.paperView.BackColor = System.Drawing.Color.Black;
            this.paperView.Name = "paperView";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // paperScroll
            // 
            resources.ApplyResources(this.paperScroll, "paperScroll");
            this.paperScroll.LargeChange = 101;
            this.paperScroll.Name = "paperScroll";
            this.paperScroll.SmallChange = 8;
            // 
            // menuStrip1
            // 
            resources.ApplyResources(this.menuStrip1, "menuStrip1");
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem});
            this.menuStrip1.Name = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            resources.ApplyResources(this.fileToolStripMenuItem, "fileToolStripMenuItem");
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveImageToolStripMenuItem});
            // 
            // saveImageToolStripMenuItem
            // 
            resources.ApplyResources(this.saveImageToolStripMenuItem, "saveImageToolStripMenuItem");
            this.saveImageToolStripMenuItem.Click += new System.EventHandler(this.SaveImageToolStripMenuItem_Click);
            // 
            // editToolStripMenuItem
            // 
            resources.ApplyResources(this.editToolStripMenuItem, "editToolStripMenuItem");
            this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyToolStripMenuItem});
            // 
            // copyToolStripMenuItem
            // 
            resources.ApplyResources(this.copyToolStripMenuItem, "copyToolStripMenuItem");
            this.copyToolStripMenuItem.Click += new System.EventHandler(this.CopyToolStripMenuItem_Click);
            // 
            // GBPrinterView
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.paperView);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.paperScroll);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MainMenuStrip = this.menuStrip1;
            this.MaximizeBox = false;
            this.Name = "GBPrinterView";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
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