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
			this.HexMenu = new System.Windows.Forms.MenuStrip();
			this.FileSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.ExitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.HexViewControl = new BizHawk.Client.EmuHawk.HexView();
			this.HexMenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// HexMenu
			// 
			this.HexMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu});
			this.HexMenu.Location = new System.Drawing.Point(0, 0);
			this.HexMenu.Name = "HexMenu";
			this.HexMenu.Size = new System.Drawing.Size(448, 24);
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
			this.ExitMenuItem.Size = new System.Drawing.Size(134, 22);
			this.ExitMenuItem.Text = "E&xit";
			this.ExitMenuItem.Click += new System.EventHandler(this.ExitMenuItem_Click);
			// 
			// HexViewControl
			// 
			this.HexViewControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.HexViewControl.ArrayLength = 0;
			this.HexViewControl.Location = new System.Drawing.Point(12, 27);
			this.HexViewControl.Name = "HexViewControl";
			this.HexViewControl.Size = new System.Drawing.Size(424, 231);
			this.HexViewControl.TabIndex = 1;
			this.HexViewControl.Text = "hexView1";
			// 
			// NewHexEditor
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(448, 270);
			this.Controls.Add(this.HexViewControl);
			this.Controls.Add(this.HexMenu);
			this.MainMenuStrip = this.HexMenu;
			this.Name = "NewHexEditor";
			this.Text = "NewHexEditor";
			this.Load += new System.EventHandler(this.NewHexEditor_Load);
			this.HexMenu.ResumeLayout(false);
			this.HexMenu.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip HexMenu;
		private System.Windows.Forms.ToolStripMenuItem FileSubMenu;
		private System.Windows.Forms.ToolStripMenuItem ExitMenuItem;
		private HexView HexViewControl;
	}
}