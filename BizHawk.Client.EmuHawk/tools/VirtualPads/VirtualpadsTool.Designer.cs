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
			this.ControllerPanel = new System.Windows.Forms.Panel();
			this.PadMenu = new MenuStripEx();
			this.PadsSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.ClearAllMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.StickyMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.ExitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SettingsSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.ClearClearsAnalogInputMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ControllerBox.SuspendLayout();
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
			this.ControllerBox.Controls.Add(this.ControllerPanel);
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
			this.PadBoxContextMenu.Size = new System.Drawing.Size(143, 48);
			this.PadBoxContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.PadBoxContextMenu_Opening);
			// 
			// clearAllToolStripMenuItem
			// 
			this.clearAllToolStripMenuItem.Name = "clearAllToolStripMenuItem";
			this.clearAllToolStripMenuItem.ShortcutKeyDisplayString = "Del";
			this.clearAllToolStripMenuItem.Size = new System.Drawing.Size(142, 22);
			this.clearAllToolStripMenuItem.Text = "Clear All";
			this.clearAllToolStripMenuItem.Click += new System.EventHandler(this.ClearAllMenuItem_Click);
			// 
			// StickyContextMenuItem
			// 
			this.StickyContextMenuItem.Name = "StickyContextMenuItem";
			this.StickyContextMenuItem.Size = new System.Drawing.Size(142, 22);
			this.StickyContextMenuItem.Text = "Sticky";
			this.StickyContextMenuItem.Click += new System.EventHandler(this.StickyMenuItem_Click);
			// 
			// ControllerPanel
			// 
			this.ControllerPanel.AutoScroll = true;
			this.ControllerPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ControllerPanel.Location = new System.Drawing.Point(3, 16);
			this.ControllerPanel.Name = "ControllerPanel";
			this.ControllerPanel.Size = new System.Drawing.Size(425, 258);
			this.ControllerPanel.TabIndex = 0;
			// 
			// PadMenu
			// 
			this.PadMenu.ClickThrough = true;
			this.PadMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.PadsSubMenu,
            this.SettingsSubMenu});
			this.PadMenu.Location = new System.Drawing.Point(0, 0);
			this.PadMenu.Name = "PadMenu";
			this.PadMenu.Size = new System.Drawing.Size(452, 24);
			this.PadMenu.TabIndex = 7;
			this.PadMenu.Text = "menuStrip1";
			// 
			// PadsSubMenu
			// 
			this.PadsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ClearAllMenuItem,
            this.StickyMenuItem,
            this.toolStripSeparator4,
            this.ExitMenuItem});
			this.PadsSubMenu.Name = "PadsSubMenu";
			this.PadsSubMenu.Size = new System.Drawing.Size(44, 20);
			this.PadsSubMenu.Text = "&Pads";
			this.PadsSubMenu.DropDownOpened += new System.EventHandler(this.PadsSubMenu_DropDownOpened);
			// 
			// ClearAllMenuItem
			// 
			this.ClearAllMenuItem.Name = "ClearAllMenuItem";
			this.ClearAllMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Delete;
			this.ClearAllMenuItem.Size = new System.Drawing.Size(142, 22);
			this.ClearAllMenuItem.Text = "&Clear All";
			this.ClearAllMenuItem.Click += new System.EventHandler(this.ClearAllMenuItem_Click);
			// 
			// StickyMenuItem
			// 
			this.StickyMenuItem.Name = "StickyMenuItem";
			this.StickyMenuItem.Size = new System.Drawing.Size(142, 22);
			this.StickyMenuItem.Text = "Sticky";
			this.StickyMenuItem.Click += new System.EventHandler(this.StickyMenuItem_Click);
			// 
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size(139, 6);
			// 
			// ExitMenuItem
			// 
			this.ExitMenuItem.Name = "ExitMenuItem";
			this.ExitMenuItem.ShortcutKeyDisplayString = "Alt+F4";
			this.ExitMenuItem.Size = new System.Drawing.Size(142, 22);
			this.ExitMenuItem.Text = "E&xit";
			this.ExitMenuItem.Click += new System.EventHandler(this.ExitMenuItem_Click);
			// 
			// SettingsSubMenu
			// 
			this.SettingsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ClearClearsAnalogInputMenuItem});
			this.SettingsSubMenu.Name = "SettingsSubMenu";
			this.SettingsSubMenu.Size = new System.Drawing.Size(61, 20);
			this.SettingsSubMenu.Text = "&Settings";
			this.SettingsSubMenu.DropDownOpened += new System.EventHandler(this.OptionsSubMenu_DropDownOpened);
			// 
			// ClearClearsAnalogInputMenuItem
			// 
			this.ClearClearsAnalogInputMenuItem.Name = "ClearClearsAnalogInputMenuItem";
			this.ClearClearsAnalogInputMenuItem.Size = new System.Drawing.Size(230, 22);
			this.ClearClearsAnalogInputMenuItem.Text = "&Clear also clears Analog Input";
			this.ClearClearsAnalogInputMenuItem.Click += new System.EventHandler(this.ClearClearsAnalogInputMenuItem_Click);
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
			this.ControllerBox.ResumeLayout(false);
			this.PadBoxContextMenu.ResumeLayout(false);
			this.PadMenu.ResumeLayout(false);
			this.PadMenu.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private MenuStripEx PadMenu;
		private System.Windows.Forms.ToolStripMenuItem SettingsSubMenu;
		private System.Windows.Forms.ToolStripMenuItem PadsSubMenu;
		private System.Windows.Forms.ToolStripMenuItem ClearAllMenuItem;
		private System.Windows.Forms.GroupBox ControllerBox;
		private System.Windows.Forms.ToolStripMenuItem StickyMenuItem;
		private System.Windows.Forms.ContextMenuStrip PadBoxContextMenu;
		private System.Windows.Forms.ToolStripMenuItem clearAllToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem StickyContextMenuItem;
		private System.Windows.Forms.ToolStripMenuItem ClearClearsAnalogInputMenuItem;
		private System.Windows.Forms.Panel ControllerPanel;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.ToolStripMenuItem ExitMenuItem;
	}
}