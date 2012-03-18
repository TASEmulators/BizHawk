namespace BizHawk.MultiClient
{
	partial class PCEBGViewer
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
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.autoloadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveWindowPositionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.vDC1ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.vCD2ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.canvas = new BizHawk.MultiClient.PCEBGCanvas();
			this.menuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.optionsToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(536, 24);
			this.menuStrip1.TabIndex = 2;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.vDC1ToolStripMenuItem,
            this.vCD2ToolStripMenuItem,
            this.toolStripSeparator1,
            this.exitToolStripMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(54, 20);
			this.fileToolStripMenuItem.Text = "&Viewer";
			this.fileToolStripMenuItem.DropDownOpened += new System.EventHandler(this.fileToolStripMenuItem_DropDownOpened);
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.ShortcutKeyDisplayString = "Alt+F4";
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.exitToolStripMenuItem.Text = "E&xit";
			this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
			// 
			// optionsToolStripMenuItem
			// 
			this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.autoloadToolStripMenuItem,
            this.saveWindowPositionToolStripMenuItem});
			this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
			this.optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
			this.optionsToolStripMenuItem.Text = "&Options";
			this.optionsToolStripMenuItem.DropDownOpened += new System.EventHandler(this.optionsToolStripMenuItem_DropDownOpened);
			// 
			// autoloadToolStripMenuItem
			// 
			this.autoloadToolStripMenuItem.Name = "autoloadToolStripMenuItem";
			this.autoloadToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
			this.autoloadToolStripMenuItem.Text = "&Auto-load";
			this.autoloadToolStripMenuItem.Click += new System.EventHandler(this.autoloadToolStripMenuItem_Click);
			// 
			// saveWindowPositionToolStripMenuItem
			// 
			this.saveWindowPositionToolStripMenuItem.Name = "saveWindowPositionToolStripMenuItem";
			this.saveWindowPositionToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
			this.saveWindowPositionToolStripMenuItem.Text = "&Save Window position";
			this.saveWindowPositionToolStripMenuItem.Click += new System.EventHandler(this.saveWindowPositionToolStripMenuItem_Click);
			// 
			// vDC1ToolStripMenuItem
			// 
			this.vDC1ToolStripMenuItem.Name = "vDC1ToolStripMenuItem";
			this.vDC1ToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.vDC1ToolStripMenuItem.Text = "VDC&1";
			this.vDC1ToolStripMenuItem.Click += new System.EventHandler(this.vDC1ToolStripMenuItem_Click);
			// 
			// vCD2ToolStripMenuItem
			// 
			this.vCD2ToolStripMenuItem.Name = "vCD2ToolStripMenuItem";
			this.vCD2ToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.vCD2ToolStripMenuItem.Text = "VCD&2";
			this.vCD2ToolStripMenuItem.Click += new System.EventHandler(this.vCD2ToolStripMenuItem_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(149, 6);
			// 
			// canvas
			// 
			this.canvas.Location = new System.Drawing.Point(12, 27);
			this.canvas.Name = "canvas";
			this.canvas.Size = new System.Drawing.Size(512, 543);
			this.canvas.TabIndex = 0;
			// 
			// PCEBGViewer
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(536, 582);
			this.Controls.Add(this.canvas);
			this.Controls.Add(this.menuStrip1);
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "PCEBGViewer";
			this.ShowIcon = false;
			this.Text = "PCE BG Viewer";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.PCEBGViewer_FormClosed);
			this.Load += new System.EventHandler(this.PCEBGViewer_Load);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private PCEBGCanvas canvas;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem autoloadToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveWindowPositionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem vDC1ToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem vCD2ToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
	}
}