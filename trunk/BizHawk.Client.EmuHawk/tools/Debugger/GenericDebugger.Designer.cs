namespace BizHawk.Client.EmuHawk
{
	partial class GenericDebugger
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GenericDebugger));
			this.menuStrip1 = new MenuStripEx();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ExitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.OptionsSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.AutoloadMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SaveWindowPositionMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.AlwaysOnTopMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FloatingWindowMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.RestoreDefaultsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.TracerBox = new System.Windows.Forms.GroupBox();
			this.TraceView = new BizHawk.Client.EmuHawk.VirtualListView();
			this.Script = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.menuStrip1.SuspendLayout();
			this.TracerBox.SuspendLayout();
			this.SuspendLayout();
			// 
			// menuStrip1
			// 
			this.menuStrip1.ClickThrough = true;
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.OptionsSubMenu});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(685, 24);
			this.menuStrip1.TabIndex = 1;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ExitMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this.fileToolStripMenuItem.Text = "&File";
			// 
			// ExitMenuItem
			// 
			this.ExitMenuItem.Name = "ExitMenuItem";
			this.ExitMenuItem.ShortcutKeyDisplayString = "Alt+F4";
			this.ExitMenuItem.Size = new System.Drawing.Size(145, 22);
			this.ExitMenuItem.Text = "&Close";
			this.ExitMenuItem.Click += new System.EventHandler(this.ExitMenuItem_Click);
			// 
			// OptionsSubMenu
			// 
			this.OptionsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.AutoloadMenuItem,
            this.SaveWindowPositionMenuItem,
            this.AlwaysOnTopMenuItem,
            this.FloatingWindowMenuItem,
            this.toolStripSeparator1,
            this.RestoreDefaultsMenuItem});
			this.OptionsSubMenu.Name = "OptionsSubMenu";
			this.OptionsSubMenu.Size = new System.Drawing.Size(61, 20);
			this.OptionsSubMenu.Text = "&Options";
			this.OptionsSubMenu.DropDownOpened += new System.EventHandler(this.OptionsSubMenu_DropDownOpened);
			// 
			// AutoloadMenuItem
			// 
			this.AutoloadMenuItem.Name = "AutoloadMenuItem";
			this.AutoloadMenuItem.Size = new System.Drawing.Size(191, 22);
			this.AutoloadMenuItem.Text = "Autoload";
			this.AutoloadMenuItem.Click += new System.EventHandler(this.AutoloadMenuItem_Click);
			// 
			// SaveWindowPositionMenuItem
			// 
			this.SaveWindowPositionMenuItem.Name = "SaveWindowPositionMenuItem";
			this.SaveWindowPositionMenuItem.Size = new System.Drawing.Size(191, 22);
			this.SaveWindowPositionMenuItem.Text = "Save Window Position";
			this.SaveWindowPositionMenuItem.Click += new System.EventHandler(this.SaveWindowPositionMenuItem_Click);
			// 
			// AlwaysOnTopMenuItem
			// 
			this.AlwaysOnTopMenuItem.Name = "AlwaysOnTopMenuItem";
			this.AlwaysOnTopMenuItem.Size = new System.Drawing.Size(191, 22);
			this.AlwaysOnTopMenuItem.Text = "Always On Top";
			this.AlwaysOnTopMenuItem.Click += new System.EventHandler(this.AlwaysOnTopMenuItem_Click);
			// 
			// FloatingWindowMenuItem
			// 
			this.FloatingWindowMenuItem.Name = "FloatingWindowMenuItem";
			this.FloatingWindowMenuItem.Size = new System.Drawing.Size(191, 22);
			this.FloatingWindowMenuItem.Text = "Floating Window";
			this.FloatingWindowMenuItem.Click += new System.EventHandler(this.FloatingWindowMenuItem_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(188, 6);
			// 
			// RestoreDefaultsMenuItem
			// 
			this.RestoreDefaultsMenuItem.Name = "RestoreDefaultsMenuItem";
			this.RestoreDefaultsMenuItem.Size = new System.Drawing.Size(191, 22);
			this.RestoreDefaultsMenuItem.Text = "Restore Defaults";
			this.RestoreDefaultsMenuItem.Click += new System.EventHandler(this.RestoreDefaultsMenuItem_Click);
			// 
			// TracerBox
			// 
			this.TracerBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.TracerBox.Controls.Add(this.TraceView);
			this.TracerBox.Location = new System.Drawing.Point(12, 27);
			this.TracerBox.Name = "TracerBox";
			this.TracerBox.Size = new System.Drawing.Size(407, 521);
			this.TracerBox.TabIndex = 7;
			this.TracerBox.TabStop = false;
			this.TracerBox.Text = "Trace log";
			// 
			// TraceView
			// 
			this.TraceView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.TraceView.BlazingFast = false;
			this.TraceView.CheckBoxes = true;
			this.TraceView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Script});
			this.TraceView.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.TraceView.FullRowSelect = true;
			this.TraceView.GridLines = true;
			this.TraceView.HideSelection = false;
			this.TraceView.ItemCount = 0;
			this.TraceView.Location = new System.Drawing.Point(8, 18);
			this.TraceView.Name = "TraceView";
			this.TraceView.SelectAllInProgress = false;
			this.TraceView.selectedItem = -1;
			this.TraceView.Size = new System.Drawing.Size(393, 491);
			this.TraceView.TabIndex = 4;
			this.TraceView.TabStop = false;
			this.TraceView.UseCompatibleStateImageBehavior = false;
			this.TraceView.UseCustomBackground = true;
			this.TraceView.View = System.Windows.Forms.View.Details;
			// 
			// Script
			// 
			this.Script.Text = "Instructions";
			this.Script.Width = 599;
			// 
			// GenericDebugger
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(685, 560);
			this.Controls.Add(this.TracerBox);
			this.Controls.Add(this.menuStrip1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "GenericDebugger";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Debugger";
			this.Load += new System.EventHandler(this.GenericDebugger_Load);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.TracerBox.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private MenuStripEx menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem ExitMenuItem;
		private System.Windows.Forms.ToolStripMenuItem OptionsSubMenu;
		private System.Windows.Forms.ToolStripMenuItem AutoloadMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SaveWindowPositionMenuItem;
		private System.Windows.Forms.ToolStripMenuItem AlwaysOnTopMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FloatingWindowMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem RestoreDefaultsMenuItem;
		private System.Windows.Forms.GroupBox TracerBox;
		private VirtualListView TraceView;
		public System.Windows.Forms.ColumnHeader Script;
	}
}