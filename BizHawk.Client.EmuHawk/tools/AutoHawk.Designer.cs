namespace BizHawk.Client.EmuHawk
{
	partial class AutoHawk
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
			this.AutoMenu = new System.Windows.Forms.MenuStrip();
			this.FileSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.ExitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.AutoMenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// AutoMenu
			// 
			this.AutoMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu});
			this.AutoMenu.Location = new System.Drawing.Point(0, 0);
			this.AutoMenu.Name = "AutoMenu";
			this.AutoMenu.Size = new System.Drawing.Size(508, 24);
			this.AutoMenu.TabIndex = 2;
			this.AutoMenu.Text = "menuStrip1";
			// 
			// FileSubMenu
			// 
			this.FileSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ExitMenuItem});
			this.FileSubMenu.Name = "FileSubMenu";
			this.FileSubMenu.Size = new System.Drawing.Size(37, 20);
			this.FileSubMenu.Text = "&File";
			// 
			// ExitMenuItem
			// 
			this.ExitMenuItem.Name = "ExitMenuItem";
			this.ExitMenuItem.Size = new System.Drawing.Size(152, 22);
			this.ExitMenuItem.Text = "E&xit";
			this.ExitMenuItem.Click += new System.EventHandler(this.ExitMenuItem_Click);
			// 
			// AutoHawk
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(508, 444);
			this.Controls.Add(this.AutoMenu);
			this.MainMenuStrip = this.AutoMenu;
			this.Name = "AutoHawk";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "AutoHawk";
			this.Load += new System.EventHandler(this.AutoHawk_Load);
			this.AutoMenu.ResumeLayout(false);
			this.AutoMenu.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip AutoMenu;
		private System.Windows.Forms.ToolStripMenuItem FileSubMenu;
		private System.Windows.Forms.ToolStripMenuItem ExitMenuItem;
	}
}