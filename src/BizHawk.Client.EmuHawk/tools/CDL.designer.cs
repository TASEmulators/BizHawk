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
			this.menuStrip1 = new MenuStripEx();
			this.FileSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.NewMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.OpenMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SaveMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SaveAsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.AppendMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.RecentSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.noneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.miAutoStart = new System.Windows.Forms.ToolStripMenuItem();
			this.miAutoSave = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.ClearMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.DisassembleMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.ExitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStrip1 = new ToolStripEx();
			this.tsbLoggingActive = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.tsbViewUpdate = new System.Windows.Forms.ToolStripButton();
			this.tsbViewStyle = new System.Windows.Forms.ToolStripComboBox();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.tsbExportText = new System.Windows.Forms.ToolStripButton();
			this.lvCDL = new InputRoll();
			this.miAutoResume = new System.Windows.Forms.ToolStripMenuItem();
			this.menuStrip1.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(992, 24);
			this.menuStrip1.TabIndex = 2;
			this.menuStrip1.Text = "menuStrip1";
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
            this.DisassembleMenuItem,
            this.toolStripSeparator1,
            this.ExitMenuItem});
			this.FileSubMenu.Name = "FileSubMenu";
			this.FileSubMenu.Size = new System.Drawing.Size(35, 20);
			this.FileSubMenu.Text = "&File";
			this.FileSubMenu.DropDownOpened += new System.EventHandler(this.FileSubMenu_DropDownOpened);
			// 
			// NewMenuItem
			// 
			this.NewMenuItem.Name = "NewMenuItem";
			this.NewMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
			this.NewMenuItem.Size = new System.Drawing.Size(193, 22);
			this.NewMenuItem.Text = "&New";
			this.NewMenuItem.Click += new System.EventHandler(this.NewMenuItem_Click);
			// 
			// OpenMenuItem
			// 
			this.OpenMenuItem.Name = "OpenMenuItem";
			this.OpenMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.OpenMenuItem.Size = new System.Drawing.Size(193, 22);
			this.OpenMenuItem.Text = "&Open...";
			this.OpenMenuItem.Click += new System.EventHandler(this.OpenMenuItem_Click);
			// 
			// SaveMenuItem
			// 
			this.SaveMenuItem.Name = "SaveMenuItem";
			this.SaveMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			this.SaveMenuItem.Size = new System.Drawing.Size(193, 22);
			this.SaveMenuItem.Text = "&Save";
			this.SaveMenuItem.Click += new System.EventHandler(this.SaveMenuItem_Click);
			// 
			// SaveAsMenuItem
			// 
			this.SaveAsMenuItem.Name = "SaveAsMenuItem";
			this.SaveAsMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.S)));
			this.SaveAsMenuItem.Size = new System.Drawing.Size(193, 22);
			this.SaveAsMenuItem.Text = "&Save As...";
			this.SaveAsMenuItem.Click += new System.EventHandler(this.SaveAsMenuItem_Click);
			// 
			// AppendMenuItem
			// 
			this.AppendMenuItem.Name = "AppendMenuItem";
			this.AppendMenuItem.Size = new System.Drawing.Size(193, 22);
			this.AppendMenuItem.Text = "&Append File...";
			this.AppendMenuItem.Click += new System.EventHandler(this.AppendMenuItem_Click);
			// 
			// RecentSubMenu
			// 
			this.RecentSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.noneToolStripMenuItem});
			this.RecentSubMenu.Name = "RecentSubMenu";
			this.RecentSubMenu.Size = new System.Drawing.Size(193, 22);
			this.RecentSubMenu.Text = "Recent";
			this.RecentSubMenu.DropDownOpened += new System.EventHandler(this.RecentSubMenu_DropDownOpened);
			// 
			// noneToolStripMenuItem
			// 
			this.noneToolStripMenuItem.Name = "noneToolStripMenuItem";
			this.noneToolStripMenuItem.Size = new System.Drawing.Size(99, 22);
			this.noneToolStripMenuItem.Text = "None";
			// 
			// miAutoStart
			// 
			this.miAutoStart.Name = "miAutoStart";
			this.miAutoStart.Size = new System.Drawing.Size(193, 22);
			this.miAutoStart.Text = "Auto-Start";
			this.miAutoStart.Click += new System.EventHandler(this.miAutoStart_Click);
			// 
			// miAutoSave
			// 
			this.miAutoSave.Name = "miAutoSave";
			this.miAutoSave.Size = new System.Drawing.Size(193, 22);
			this.miAutoSave.Text = "Auto-Save";
			this.miAutoSave.Click += new System.EventHandler(this.miAutoSave_Click);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(190, 6);
			// 
			// ClearMenuItem
			// 
			this.ClearMenuItem.Name = "ClearMenuItem";
			this.ClearMenuItem.Size = new System.Drawing.Size(193, 22);
			this.ClearMenuItem.Text = "&Clear";
			this.ClearMenuItem.Click += new System.EventHandler(this.ClearMenuItem_Click);
			// 
			// DisassembleMenuItem
			// 
			this.DisassembleMenuItem.Name = "DisassembleMenuItem";
			this.DisassembleMenuItem.Size = new System.Drawing.Size(193, 22);
			this.DisassembleMenuItem.Text = "&Disassemble...";
			this.DisassembleMenuItem.Click += new System.EventHandler(this.DisassembleMenuItem_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(190, 6);
			// 
			// ExitMenuItem
			// 
			this.ExitMenuItem.Name = "ExitMenuItem";
			this.ExitMenuItem.ShortcutKeyDisplayString = "Alt+F4";
			this.ExitMenuItem.Size = new System.Drawing.Size(193, 22);
			this.ExitMenuItem.Text = "&Close";
			this.ExitMenuItem.Click += new System.EventHandler(this.ExitMenuItem_Click);
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
			this.toolStrip1.Size = new System.Drawing.Size(992, 25);
			this.toolStrip1.TabIndex = 8;
			this.toolStrip1.Text = "toolStrip1";
			// 
			// tsbLoggingActive
			// 
			this.tsbLoggingActive.CheckOnClick = true;
			this.tsbLoggingActive.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.tsbLoggingActive.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tsbLoggingActive.Name = "tsbLoggingActive";
			this.tsbLoggingActive.Size = new System.Drawing.Size(41, 22);
			this.tsbLoggingActive.Text = "Active";
			this.tsbLoggingActive.CheckedChanged += new System.EventHandler(this.tsbLoggingActive_CheckedChanged);
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
			// 
			// tsbViewUpdate
			// 
			this.tsbViewUpdate.Checked = true;
			this.tsbViewUpdate.CheckOnClick = true;
			this.tsbViewUpdate.CheckState = System.Windows.Forms.CheckState.Checked;
			this.tsbViewUpdate.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.tsbViewUpdate.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tsbViewUpdate.Name = "tsbViewUpdate";
			this.tsbViewUpdate.Size = new System.Drawing.Size(46, 22);
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
			this.tsbViewStyle.SelectedIndexChanged += new System.EventHandler(this.tsbViewStyle_SelectedIndexChanged);
			// 
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size(6, 25);
			// 
			// tsbExportText
			// 
			this.tsbExportText.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tsbExportText.Name = "tsbExportText";
			this.tsbExportText.Size = new System.Drawing.Size(87, 22);
			this.tsbExportText.Text = "To Clipboard";
			this.tsbExportText.Click += new System.EventHandler(this.tsbExportText_Click);
			// 
			// lvCDL
			// 
			this.lvCDL.CellWidthPadding = 3;
			this.lvCDL.Dock = System.Windows.Forms.DockStyle.Fill;
			this.lvCDL.FullRowSelect = true;
			this.lvCDL.GridLines = true;
			this.lvCDL.RowCount = 0;
			this.lvCDL.Location = new System.Drawing.Point(0, 49);
			this.lvCDL.Name = "lvCDL";
			this.lvCDL.Size = new System.Drawing.Size(992, 323);
			this.lvCDL.TabIndex = 9;
			this.lvCDL.AllowColumnReorder = false;
			this.lvCDL.AllowColumnResize = true;
			this.lvCDL.QueryItemText += new InputRoll.QueryItemTextHandler(this.lvCDL_QueryItemText);
			// 
			// miAutoResume
			// 
			this.miAutoResume.Name = "miAutoResume";
			this.miAutoResume.Size = new System.Drawing.Size(193, 22);
			this.miAutoResume.Text = "Auto-Resume";
			this.miAutoResume.Click += new System.EventHandler(this.miAutoResume_Click);
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
			this.Text = "Code Data Logger";
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
		private System.Windows.Forms.ToolStripMenuItem FileSubMenu;
		private System.Windows.Forms.ToolStripMenuItem ClearMenuItem;
		private System.Windows.Forms.ToolStripMenuItem OpenMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SaveAsMenuItem;
		private System.Windows.Forms.ToolStripMenuItem AppendMenuItem;
		private System.Windows.Forms.ToolStripMenuItem NewMenuItem;
		private System.Windows.Forms.ToolStripMenuItem DisassembleMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem ExitMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SaveMenuItem;
		private System.Windows.Forms.ToolStripMenuItem RecentSubMenu;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripMenuItem noneToolStripMenuItem;
		private System.Windows.Forms.ToolStripButton tsbLoggingActive;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripButton tsbViewUpdate;
		private System.Windows.Forms.ToolStripComboBox tsbViewStyle;
		private InputRoll lvCDL;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.ToolStripButton tsbExportText;
		private System.Windows.Forms.ToolStripMenuItem miAutoStart;
		private System.Windows.Forms.ToolStripMenuItem miAutoSave;
		private ToolStripEx toolStrip1;
		private System.Windows.Forms.ToolStripMenuItem miAutoResume;
	}
}