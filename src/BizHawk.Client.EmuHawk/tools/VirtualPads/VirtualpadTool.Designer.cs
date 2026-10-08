using BizHawk.WinForms.Controls;

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
            this.clearAllToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.StickyContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.ControllerPanel = new System.Windows.Forms.Panel();
            this.PadMenu = new BizHawk.WinForms.Controls.MenuStripEx();
            this.PadsSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.ClearAllMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.StickyMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator4 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.ExitMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SettingsSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.ClearClearsAnalogInputMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.ControllerBox.SuspendLayout();
            this.PadBoxContextMenu.SuspendLayout();
            this.PadMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // ControllerBox
            // 
            resources.ApplyResources(this.ControllerBox, "ControllerBox");
            this.ControllerBox.ContextMenuStrip = this.PadBoxContextMenu;
            this.ControllerBox.Controls.Add(this.ControllerPanel);
            this.ControllerBox.Name = "ControllerBox";
            this.ControllerBox.TabStop = false;
            // 
            // PadBoxContextMenu
            // 
            resources.ApplyResources(this.PadBoxContextMenu, "PadBoxContextMenu");
            this.PadBoxContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.clearAllToolStripMenuItem,
            this.StickyContextMenuItem});
            this.PadBoxContextMenu.Name = "PadBoxContextMenu";
            this.PadBoxContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.PadBoxContextMenu_Opening);
            // 
            // clearAllToolStripMenuItem
            // 
            resources.ApplyResources(this.clearAllToolStripMenuItem, "clearAllToolStripMenuItem");
            this.clearAllToolStripMenuItem.Click += new System.EventHandler(this.ClearAllMenuItem_Click);
            // 
            // StickyContextMenuItem
            // 
            resources.ApplyResources(this.StickyContextMenuItem, "StickyContextMenuItem");
            this.StickyContextMenuItem.Click += new System.EventHandler(this.StickyMenuItem_Click);
            // 
            // ControllerPanel
            // 
            resources.ApplyResources(this.ControllerPanel, "ControllerPanel");
            this.ControllerPanel.Name = "ControllerPanel";
            // 
            // PadMenu
            // 
            resources.ApplyResources(this.PadMenu, "PadMenu");
            this.PadMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.PadsSubMenu,
            this.SettingsSubMenu});
            // 
            // PadsSubMenu
            // 
            resources.ApplyResources(this.PadsSubMenu, "PadsSubMenu");
            this.PadsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ClearAllMenuItem,
            this.StickyMenuItem,
            this.toolStripSeparator4,
            this.ExitMenuItem});
            this.PadsSubMenu.DropDownOpened += new System.EventHandler(this.PadsSubMenu_DropDownOpened);
            // 
            // ClearAllMenuItem
            // 
            resources.ApplyResources(this.ClearAllMenuItem, "ClearAllMenuItem");
            this.ClearAllMenuItem.Click += new System.EventHandler(this.ClearAllMenuItem_Click);
            // 
            // StickyMenuItem
            // 
            resources.ApplyResources(this.StickyMenuItem, "StickyMenuItem");
            this.StickyMenuItem.Click += new System.EventHandler(this.StickyMenuItem_Click);
            // 
            // toolStripSeparator4
            // 
            resources.ApplyResources(this.toolStripSeparator4, "toolStripSeparator4");
            // 
            // ExitMenuItem
            // 
            resources.ApplyResources(this.ExitMenuItem, "ExitMenuItem");
            this.ExitMenuItem.Click += new System.EventHandler(this.ExitMenuItem_Click);
            // 
            // SettingsSubMenu
            // 
            resources.ApplyResources(this.SettingsSubMenu, "SettingsSubMenu");
            this.SettingsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ClearClearsAnalogInputMenuItem});
            this.SettingsSubMenu.DropDownOpened += new System.EventHandler(this.OptionsSubMenu_DropDownOpened);
            // 
            // ClearClearsAnalogInputMenuItem
            // 
            resources.ApplyResources(this.ClearClearsAnalogInputMenuItem, "ClearClearsAnalogInputMenuItem");
            this.ClearClearsAnalogInputMenuItem.Click += new System.EventHandler(this.ClearClearsAnalogInputMenuItem_Click);
            // 
            // VirtualpadTool
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.ControllerBox);
            this.Controls.Add(this.PadMenu);
            this.Name = "VirtualpadTool";
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
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SettingsSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx PadsSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ClearAllMenuItem;
		private System.Windows.Forms.GroupBox ControllerBox;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx StickyMenuItem;
		private System.Windows.Forms.ContextMenuStrip PadBoxContextMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx clearAllToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx StickyContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ClearClearsAnalogInputMenuItem;
		private System.Windows.Forms.Panel ControllerPanel;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator4;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ExitMenuItem;
	}
}