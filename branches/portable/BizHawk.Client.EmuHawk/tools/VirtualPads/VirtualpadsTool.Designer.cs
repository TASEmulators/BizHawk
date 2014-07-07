namespace BizHawk.Client.EmuHawk
{
	partial class VirtualpadTool
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VirtualpadTool));
			this.ControllerBox = new System.Windows.Forms.GroupBox();
			this.PadBoxContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.clearAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.StickyContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.PadMenu = new MenuStripEx();
			this.OptionsSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.AutoloadMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SaveWindowPositionMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.AlwaysOnTopMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FloatingWindowMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.RestoreDefaultSettingsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.ExitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.PadsSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.ClearAllMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.StickyMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.PadBoxContextMenu.SuspendLayout();
			this.PadMenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// ControllerBox
			// 
			this.ControllerBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.ControllerBox.ContextMenuStrip = this.PadBoxContextMenu;
			this.ControllerBox.Location = new System.Drawing.Point(12, 27);
			this.ControllerBox.Name = "ControllerBox";
			this.ControllerBox.Size = new System.Drawing.Size(431, 277);
			this.ControllerBox.TabIndex = 11;
			this.ControllerBox.TabStop = false;
			this.ControllerBox.Text = "Controllers";
			// 
			// PadBoxContextMenu
			// 
			this.PadBoxContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.clearAllToolStripMenuItem,
            this.StickyContextMenuItem});
			this.PadBoxContextMenu.Name = "PadBoxContextMenu";
			this.PadBoxContextMenu.Size = new System.Drawing.Size(175, 70);
			this.PadBoxContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.PadBoxContextMenu_Opening);
			// 
			// clearAllToolStripMenuItem
			// 
			this.clearAllToolStripMenuItem.Name = "clearAllToolStripMenuItem";
			this.clearAllToolStripMenuItem.ShortcutKeyDisplayString = "Shift+Del";
			this.clearAllToolStripMenuItem.Size = new System.Drawing.Size(174, 22);
			this.clearAllToolStripMenuItem.Text = "Clear All";
			this.clearAllToolStripMenuItem.Click += new System.EventHandler(this.ClearAllMenuItem_Click);
			// 
			// StickyContextMenuItem
			// 
			this.StickyContextMenuItem.Name = "StickyContextMenuItem";
			this.StickyContextMenuItem.Size = new System.Drawing.Size(174, 22);
			this.StickyContextMenuItem.Text = "Sticky";
			this.StickyContextMenuItem.Click += new System.EventHandler(this.StickyMenuItem_Click);
			// 
			// PadMenu
			// 
			this.PadMenu.ClickThrough = true;
			this.PadMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.OptionsSubMenu,
            this.PadsSubMenu});
			this.PadMenu.Location = new System.Drawing.Point(0, 0);
			this.PadMenu.Name = "PadMenu";
			this.PadMenu.Size = new System.Drawing.Size(452, 24);
			this.PadMenu.TabIndex = 7;
			this.PadMenu.Text = "menuStrip1";
			// 
			// OptionsSubMenu
			// 
			this.OptionsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.AutoloadMenuItem,
            this.SaveWindowPositionMenuItem,
            this.AlwaysOnTopMenuItem,
            this.FloatingWindowMenuItem,
            this.toolStripSeparator2,
            this.RestoreDefaultSettingsMenuItem,
            this.toolStripSeparator1,
            this.ExitMenuItem});
			this.OptionsSubMenu.Name = "OptionsSubMenu";
			this.OptionsSubMenu.Size = new System.Drawing.Size(61, 20);
			this.OptionsSubMenu.Text = "&Options";
			this.OptionsSubMenu.DropDownOpened += new System.EventHandler(this.OptionsSubMenu_DropDownOpened);
			// 
			// AutoloadMenuItem
			// 
			this.AutoloadMenuItem.Name = "AutoloadMenuItem";
			this.AutoloadMenuItem.Size = new System.Drawing.Size(199, 22);
			this.AutoloadMenuItem.Text = "&Autoload";
			this.AutoloadMenuItem.Click += new System.EventHandler(this.AutoloadMenuItem_Click);
			// 
			// SaveWindowPositionMenuItem
			// 
			this.SaveWindowPositionMenuItem.Name = "SaveWindowPositionMenuItem";
			this.SaveWindowPositionMenuItem.Size = new System.Drawing.Size(199, 22);
			this.SaveWindowPositionMenuItem.Text = "&Save Window Position";
			this.SaveWindowPositionMenuItem.Click += new System.EventHandler(this.SaveWindowPositionMenuItem_Click);
			// 
			// AlwaysOnTopMenuItem
			// 
			this.AlwaysOnTopMenuItem.Name = "AlwaysOnTopMenuItem";
			this.AlwaysOnTopMenuItem.Size = new System.Drawing.Size(199, 22);
			this.AlwaysOnTopMenuItem.Text = "Always On Top";
			this.AlwaysOnTopMenuItem.Click += new System.EventHandler(this.AlwaysOnTopMenuItem_Click);
			// 
			// FloatingWindowMenuItem
			// 
			this.FloatingWindowMenuItem.Name = "FloatingWindowMenuItem";
			this.FloatingWindowMenuItem.Size = new System.Drawing.Size(199, 22);
			this.FloatingWindowMenuItem.Text = "Floating Window";
			this.FloatingWindowMenuItem.Click += new System.EventHandler(this.FloatingWindowMenuItem_Click);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(196, 6);
			// 
			// RestoreDefaultSettingsMenuItem
			// 
			this.RestoreDefaultSettingsMenuItem.Name = "RestoreDefaultSettingsMenuItem";
			this.RestoreDefaultSettingsMenuItem.Size = new System.Drawing.Size(199, 22);
			this.RestoreDefaultSettingsMenuItem.Text = "Restore Default Settings";
			this.RestoreDefaultSettingsMenuItem.Click += new System.EventHandler(this.RestoreDefaultSettingsMenuItem_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(196, 6);
			// 
			// ExitMenuItem
			// 
			this.ExitMenuItem.Name = "ExitMenuItem";
			this.ExitMenuItem.ShortcutKeyDisplayString = "Alt+F4";
			this.ExitMenuItem.Size = new System.Drawing.Size(199, 22);
			this.ExitMenuItem.Text = "E&xit";
			this.ExitMenuItem.Click += new System.EventHandler(this.ExitMenuItem_Click);
			// 
			// PadsSubMenu
			// 
			this.PadsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ClearAllMenuItem,
            this.StickyMenuItem});
			this.PadsSubMenu.Name = "PadsSubMenu";
			this.PadsSubMenu.Size = new System.Drawing.Size(44, 20);
			this.PadsSubMenu.Text = "&Pads";
			this.PadsSubMenu.DropDownOpened += new System.EventHandler(this.PadsSubMenu_DropDownOpened);
			// 
			// ClearAllMenuItem
			// 
			this.ClearAllMenuItem.Name = "ClearAllMenuItem";
			this.ClearAllMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.Delete)));
			this.ClearAllMenuItem.Size = new System.Drawing.Size(174, 22);
			this.ClearAllMenuItem.Text = "&Clear All";
			this.ClearAllMenuItem.Click += new System.EventHandler(this.ClearAllMenuItem_Click);
			// 
			// StickyMenuItem
			// 
			this.StickyMenuItem.Name = "StickyMenuItem";
			this.StickyMenuItem.Size = new System.Drawing.Size(174, 22);
			this.StickyMenuItem.Text = "Sticky";
			this.StickyMenuItem.Click += new System.EventHandler(this.StickyMenuItem_Click);
			// 
			// VirtualpadTool
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(452, 312);
			this.Controls.Add(this.ControllerBox);
			this.Controls.Add(this.PadMenu);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "VirtualpadTool";
			this.Text = "Virtual Pads";
			this.Load += new System.EventHandler(this.VirtualpadTool_Load);
			this.PadBoxContextMenu.ResumeLayout(false);
			this.PadMenu.ResumeLayout(false);
			this.PadMenu.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private MenuStripEx PadMenu;
		private System.Windows.Forms.ToolStripMenuItem OptionsSubMenu;
		private System.Windows.Forms.ToolStripMenuItem AutoloadMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SaveWindowPositionMenuItem;
		private System.Windows.Forms.ToolStripMenuItem AlwaysOnTopMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FloatingWindowMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripMenuItem RestoreDefaultSettingsMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem ExitMenuItem;
		private System.Windows.Forms.ToolStripMenuItem PadsSubMenu;
		private System.Windows.Forms.ToolStripMenuItem ClearAllMenuItem;
		private System.Windows.Forms.GroupBox ControllerBox;
		private System.Windows.Forms.ToolStripMenuItem StickyMenuItem;
		private System.Windows.Forms.ContextMenuStrip PadBoxContextMenu;
		private System.Windows.Forms.ToolStripMenuItem clearAllToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem StickyContextMenuItem;
	}
}