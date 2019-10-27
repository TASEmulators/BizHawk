namespace BizHawk.Client.EmuHawk
{
	partial class NewHexEditor
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewHexEditor));
			this.HexMenu = new MenuStripEx();
			this.FileSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.ExitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.HexViewControl = new BizHawk.Client.EmuHawk.HexView();
			this.DataSizeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.OneByteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.TwoByteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FourByteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.HexMenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// HexMenu
			// 
			this.HexMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu,
            this.optionsToolStripMenuItem});
			this.HexMenu.Location = new System.Drawing.Point(0, 0);
			this.HexMenu.Name = "HexMenu";
			this.HexMenu.Size = new System.Drawing.Size(584, 24);
			this.HexMenu.TabIndex = 0;
			this.HexMenu.Text = "menuStrip1";
			// 
			// FileSubMenu
			// 
			this.FileSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ExitMenuItem});
			this.FileSubMenu.Name = "FileSubMenu";
			this.FileSubMenu.Size = new System.Drawing.Size(37, 20);
			this.FileSubMenu.Text = "&File";
			this.FileSubMenu.DropDownOpened += new System.EventHandler(this.FileSubMenu_DropDownOpened);
			// 
			// ExitMenuItem
			// 
			this.ExitMenuItem.Name = "ExitMenuItem";
			this.ExitMenuItem.ShortcutKeyDisplayString = "Alt+F4";
			this.ExitMenuItem.Size = new System.Drawing.Size(135, 22);
			this.ExitMenuItem.Text = "E&xit";
			this.ExitMenuItem.Click += new System.EventHandler(this.ExitMenuItem_Click);
			// 
			// optionsToolStripMenuItem
			// 
			this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.DataSizeMenuItem});
			this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
			this.optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
			this.optionsToolStripMenuItem.Text = "&Options";
			// 
			// HexViewControl
			// 
			this.HexViewControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.HexViewControl.ArrayLength = ((long)(0));
			this.HexViewControl.DataSize = 1;
			this.HexViewControl.Location = new System.Drawing.Point(12, 27);
			this.HexViewControl.Name = "HexViewControl";
			this.HexViewControl.Size = new System.Drawing.Size(560, 262);
			this.HexViewControl.TabIndex = 1;
			this.HexViewControl.Text = "hexView1";
			// 
			// DataSizeMenuItem
			// 
			this.DataSizeMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.OneByteMenuItem,
            this.TwoByteMenuItem,
            this.FourByteMenuItem});
			this.DataSizeMenuItem.Name = "DataSizeMenuItem";
			this.DataSizeMenuItem.Size = new System.Drawing.Size(180, 22);
			this.DataSizeMenuItem.Text = "&Data Size";
			this.DataSizeMenuItem.DropDownOpened += new System.EventHandler(this.DataSizeMenuItem_DropDownOpened);
			// 
			// OneByteMenuItem
			// 
			this.OneByteMenuItem.Name = "OneByteMenuItem";
			this.OneByteMenuItem.Size = new System.Drawing.Size(180, 22);
			this.OneByteMenuItem.Text = "1 Byte";
			this.OneByteMenuItem.Click += new System.EventHandler(this.OneByteMenuItem_Click);
			// 
			// TwoByteMenuItem
			// 
			this.TwoByteMenuItem.Name = "TwoByteMenuItem";
			this.TwoByteMenuItem.Size = new System.Drawing.Size(180, 22);
			this.TwoByteMenuItem.Text = "2 Byte";
			this.TwoByteMenuItem.Click += new System.EventHandler(this.TwoByteMenuItem_Click);
			// 
			// FourByteMenuItem
			// 
			this.FourByteMenuItem.Name = "FourByteMenuItem";
			this.FourByteMenuItem.Size = new System.Drawing.Size(180, 22);
			this.FourByteMenuItem.Text = "4 Byte";
			this.FourByteMenuItem.Click += new System.EventHandler(this.FourByteMenuItem_Click);
			// 
			// NewHexEditor
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(584, 301);
			this.Controls.Add(this.HexViewControl);
			this.Controls.Add(this.HexMenu);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.HexMenu;
			this.Name = "NewHexEditor";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Hex Editor";
			this.Load += new System.EventHandler(this.NewHexEditor_Load);
			this.HexMenu.ResumeLayout(false);
			this.HexMenu.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private MenuStripEx HexMenu;
		private System.Windows.Forms.ToolStripMenuItem FileSubMenu;
		private System.Windows.Forms.ToolStripMenuItem ExitMenuItem;
		private HexView HexViewControl;
		private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem DataSizeMenuItem;
		private System.Windows.Forms.ToolStripMenuItem OneByteMenuItem;
		private System.Windows.Forms.ToolStripMenuItem TwoByteMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FourByteMenuItem;
	}
}