using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	partial class CDL
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
			this.menuStrip1 = new BizHawk.WinForms.Controls.MenuStripEx();
			this.FileSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.NewMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.OpenMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.SaveMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.SaveAsMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.AppendMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.RecentSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.noneToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.miAutoStart = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.miAutoSave = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.miAutoResume = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.toolStripSeparator2 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.ClearMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.DisassembleMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.toolStrip1 = new BizHawk.WinForms.Controls.ToolStripEx();
			this.tsbLoggingActive = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator3 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.tsbViewUpdate = new System.Windows.Forms.ToolStripButton();
			this.tsbViewStyle = new System.Windows.Forms.ToolStripComboBox();
			this.toolStripSeparator4 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.tsbExportText = new System.Windows.Forms.ToolStripButton();
			this.lvCDL = new BizHawk.Client.EmuHawk.InputRoll();
			this.menuStrip1.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu});
			this.menuStrip1.TabIndex = 2;
			// 
			// FileSubMenu
			// 
			this.FileSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.NewMenuItem,
            this.OpenMenuItem,
            this.SaveMenuItem,
            this.SaveAsMenuItem,
            this.AppendMenuItem,
            this.RecentSubMenu,
            this.miAutoStart,
            this.miAutoSave,
            this.miAutoResume,
            this.toolStripSeparator2,
            this.ClearMenuItem,
            this.DisassembleMenuItem});
			this.FileSubMenu.Text = "&File";
			this.FileSubMenu.DropDownOpened += new System.EventHandler(this.FileSubMenu_DropDownOpened);
			// 
			// NewMenuItem
			// 
			this.NewMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
			this.NewMenuItem.Text = "&New";
			this.NewMenuItem.Click += new System.EventHandler(this.NewMenuItem_Click);
			// 
			// OpenMenuItem
			// 
			this.OpenMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.OpenMenuItem.Text = "&Open...";
			this.OpenMenuItem.Click += new System.EventHandler(this.OpenMenuItem_Click);
			// 
			// SaveMenuItem
			// 
			this.SaveMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			this.SaveMenuItem.Text = "&Save";
			this.SaveMenuItem.Click += new System.EventHandler(this.SaveMenuItem_Click);
			// 
			// SaveAsMenuItem
			// 
			this.SaveAsMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.S)));
			this.SaveAsMenuItem.Text = "&Save As...";
			this.SaveAsMenuItem.Click += new System.EventHandler(this.SaveAsMenuItem_Click);
			// 
			// AppendMenuItem
			// 
			this.AppendMenuItem.Text = "&Append File...";
			this.AppendMenuItem.Click += new System.EventHandler(this.AppendMenuItem_Click);
			// 
			// RecentSubMenu
			// 
			this.RecentSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.noneToolStripMenuItem});
			this.RecentSubMenu.Text = "Recent";
			this.RecentSubMenu.DropDownOpened += new System.EventHandler(this.RecentSubMenu_DropDownOpened);
			// 
			// noneToolStripMenuItem
			// 
			this.noneToolStripMenuItem.Text = "None";
			// 
			// miAutoStart
			// 
			this.miAutoStart.Text = "Auto-Start";
			this.miAutoStart.Click += new System.EventHandler(this.MiAutoStart_Click);
			// 
			// miAutoSave
			// 
			this.miAutoSave.Text = "Auto-Save";
			this.miAutoSave.Click += new System.EventHandler(this.MiAutoSave_Click);
			// 
			// miAutoResume
			// 
			this.miAutoResume.Text = "Auto-Resume";
			this.miAutoResume.Click += new System.EventHandler(this.MiAutoResume_Click);
			// 
			// ClearMenuItem
			// 
			this.ClearMenuItem.Text = "&Clear";
			this.ClearMenuItem.Click += new System.EventHandler(this.ClearMenuItem_Click);
			// 
			// DisassembleMenuItem
			// 
			this.DisassembleMenuItem.Text = "&Disassemble...";
			this.DisassembleMenuItem.Click += new System.EventHandler(this.DisassembleMenuItem_Click);
			// 
			// toolStrip1
			// 
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbLoggingActive,
            this.toolStripSeparator3,
            this.tsbViewUpdate,
            this.tsbViewStyle,
            this.toolStripSeparator4,
            this.tsbExportText});
			this.toolStrip1.Location = new System.Drawing.Point(0, 24);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.TabIndex = 8;
			// 
			// tsbLoggingActive
			// 
			this.tsbLoggingActive.CheckOnClick = true;
			this.tsbLoggingActive.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.tsbLoggingActive.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tsbLoggingActive.Name = "tsbLoggingActive";
			this.tsbLoggingActive.Size = new System.Drawing.Size(44, 22);
			this.tsbLoggingActive.Text = "Active";
			this.tsbLoggingActive.CheckedChanged += new System.EventHandler(this.TsbLoggingActive_CheckedChanged);
			// 
			// tsbViewUpdate
			// 
			this.tsbViewUpdate.Checked = true;
			this.tsbViewUpdate.CheckOnClick = true;
			this.tsbViewUpdate.CheckState = System.Windows.Forms.CheckState.Checked;
			this.tsbViewUpdate.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.tsbViewUpdate.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tsbViewUpdate.Name = "tsbViewUpdate";
			this.tsbViewUpdate.Size = new System.Drawing.Size(49, 22);
			this.tsbViewUpdate.Text = "Update";
			// 
			// tsbViewStyle
			// 
			this.tsbViewStyle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.tsbViewStyle.Items.AddRange(new object[] {
            "Show %",
            "Show Bytes",
            "Show KBytes"});
			this.tsbViewStyle.Name = "tsbViewStyle";
			this.tsbViewStyle.Size = new System.Drawing.Size(121, 25);
			this.tsbViewStyle.SelectedIndexChanged += new System.EventHandler(this.TsbViewStyle_SelectedIndexChanged);
			// 
			// tsbExportText
			// 
			this.tsbExportText.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tsbExportText.Name = "tsbExportText";
			this.tsbExportText.Size = new System.Drawing.Size(78, 22);
			this.tsbExportText.Text = "To Clipboard";
			this.tsbExportText.Click += new System.EventHandler(this.TsbExportText_Click);
			// 
			// lvCDL
			// 
			this.lvCDL.AllowColumnReorder = false;
			this.lvCDL.AllowColumnResize = true;
			this.lvCDL.AllowMassNavigationShortcuts = true;
			this.lvCDL.AllowRightClickSelection = true;
			this.lvCDL.AlwaysScroll = false;
			this.lvCDL.CellHeightPadding = 0;
			this.lvCDL.Dock = System.Windows.Forms.DockStyle.Fill;
			this.lvCDL.FullRowSelect = true;
			this.lvCDL.HorizontalOrientation = false;
			this.lvCDL.LetKeysModifySelection = false;
			this.lvCDL.Location = new System.Drawing.Point(0, 49);
			this.lvCDL.Name = "lvCDL";
			this.lvCDL.RowCount = 0;
			this.lvCDL.ScrollSpeed = 0;
			this.lvCDL.SeekingCutoffInterval = 0;
			this.lvCDL.Size = new System.Drawing.Size(992, 323);
			this.lvCDL.TabIndex = 9;
			this.lvCDL.QueryItemText += new BizHawk.Client.EmuHawk.InputRoll.QueryItemTextHandler(this.LvCDL_QueryItemText);
			// 
			// CDL
			// 
			this.AllowDrop = true;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(992, 372);
			this.Controls.Add(this.lvCDL);
			this.Controls.Add(this.toolStrip1);
			this.Controls.Add(this.menuStrip1);
			this.MainMenuStrip = this.menuStrip1;
			this.MinimumSize = new System.Drawing.Size(150, 130);
			this.Name = "CDL";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Load += new System.EventHandler(this.CDL_Load);
			this.DragDrop += new System.Windows.Forms.DragEventHandler(this.CDL_DragDrop);
			this.DragEnter += new System.Windows.Forms.DragEventHandler(this.CDL_DragEnter);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private MenuStripEx menuStrip1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FileSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ClearMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx OpenMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SaveAsMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx AppendMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx NewMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DisassembleMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SaveMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RecentSubMenu;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator2;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx noneToolStripMenuItem;
		private System.Windows.Forms.ToolStripButton tsbLoggingActive;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator3;
		private System.Windows.Forms.ToolStripButton tsbViewUpdate;
		private System.Windows.Forms.ToolStripComboBox tsbViewStyle;
		private InputRoll lvCDL;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator4;
		private System.Windows.Forms.ToolStripButton tsbExportText;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx miAutoStart;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx miAutoSave;
		private ToolStripEx toolStrip1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx miAutoResume;
	}
}