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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CDL));
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
            resources.ApplyResources(this.menuStrip1, "menuStrip1");
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu});
            // 
            // FileSubMenu
            // 
            resources.ApplyResources(this.FileSubMenu, "FileSubMenu");
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
            this.FileSubMenu.DropDownOpened += new System.EventHandler(this.FileSubMenu_DropDownOpened);
            // 
            // NewMenuItem
            // 
            resources.ApplyResources(this.NewMenuItem, "NewMenuItem");
            this.NewMenuItem.Click += new System.EventHandler(this.NewMenuItem_Click);
            // 
            // OpenMenuItem
            // 
            resources.ApplyResources(this.OpenMenuItem, "OpenMenuItem");
            this.OpenMenuItem.Click += new System.EventHandler(this.OpenMenuItem_Click);
            // 
            // SaveMenuItem
            // 
            resources.ApplyResources(this.SaveMenuItem, "SaveMenuItem");
            this.SaveMenuItem.Click += new System.EventHandler(this.SaveMenuItem_Click);
            // 
            // SaveAsMenuItem
            // 
            resources.ApplyResources(this.SaveAsMenuItem, "SaveAsMenuItem");
            this.SaveAsMenuItem.Click += new System.EventHandler(this.SaveAsMenuItem_Click);
            // 
            // AppendMenuItem
            // 
            resources.ApplyResources(this.AppendMenuItem, "AppendMenuItem");
            this.AppendMenuItem.Click += new System.EventHandler(this.AppendMenuItem_Click);
            // 
            // RecentSubMenu
            // 
            resources.ApplyResources(this.RecentSubMenu, "RecentSubMenu");
            this.RecentSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.noneToolStripMenuItem});
            this.RecentSubMenu.DropDownOpened += new System.EventHandler(this.RecentSubMenu_DropDownOpened);
            // 
            // noneToolStripMenuItem
            // 
            resources.ApplyResources(this.noneToolStripMenuItem, "noneToolStripMenuItem");
            // 
            // miAutoStart
            // 
            resources.ApplyResources(this.miAutoStart, "miAutoStart");
            this.miAutoStart.Click += new System.EventHandler(this.MiAutoStart_Click);
            // 
            // miAutoSave
            // 
            resources.ApplyResources(this.miAutoSave, "miAutoSave");
            this.miAutoSave.Click += new System.EventHandler(this.MiAutoSave_Click);
            // 
            // miAutoResume
            // 
            resources.ApplyResources(this.miAutoResume, "miAutoResume");
            this.miAutoResume.Click += new System.EventHandler(this.MiAutoResume_Click);
            // 
            // toolStripSeparator2
            // 
            resources.ApplyResources(this.toolStripSeparator2, "toolStripSeparator2");
            // 
            // ClearMenuItem
            // 
            resources.ApplyResources(this.ClearMenuItem, "ClearMenuItem");
            this.ClearMenuItem.Click += new System.EventHandler(this.ClearMenuItem_Click);
            // 
            // DisassembleMenuItem
            // 
            resources.ApplyResources(this.DisassembleMenuItem, "DisassembleMenuItem");
            this.DisassembleMenuItem.Click += new System.EventHandler(this.DisassembleMenuItem_Click);
            // 
            // toolStrip1
            // 
            resources.ApplyResources(this.toolStrip1, "toolStrip1");
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbLoggingActive,
            this.toolStripSeparator3,
            this.tsbViewUpdate,
            this.tsbViewStyle,
            this.toolStripSeparator4,
            this.tsbExportText});
            this.toolStrip1.Name = "toolStrip1";
            // 
            // tsbLoggingActive
            // 
            resources.ApplyResources(this.tsbLoggingActive, "tsbLoggingActive");
            this.tsbLoggingActive.CheckOnClick = true;
            this.tsbLoggingActive.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbLoggingActive.Name = "tsbLoggingActive";
            this.tsbLoggingActive.CheckedChanged += new System.EventHandler(this.TsbLoggingActive_CheckedChanged);
            // 
            // toolStripSeparator3
            // 
            resources.ApplyResources(this.toolStripSeparator3, "toolStripSeparator3");
            // 
            // tsbViewUpdate
            // 
            resources.ApplyResources(this.tsbViewUpdate, "tsbViewUpdate");
            this.tsbViewUpdate.Checked = true;
            this.tsbViewUpdate.CheckOnClick = true;
            this.tsbViewUpdate.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tsbViewUpdate.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbViewUpdate.Name = "tsbViewUpdate";
            // 
            // tsbViewStyle
            // 
            resources.ApplyResources(this.tsbViewStyle, "tsbViewStyle");
            this.tsbViewStyle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.tsbViewStyle.Items.AddRange(new object[] {
            resources.GetString("tsbViewStyle.Items"),
            resources.GetString("tsbViewStyle.Items1"),
            resources.GetString("tsbViewStyle.Items2")});
            this.tsbViewStyle.Name = "tsbViewStyle";
            this.tsbViewStyle.SelectedIndexChanged += new System.EventHandler(this.TsbViewStyle_SelectedIndexChanged);
            // 
            // toolStripSeparator4
            // 
            resources.ApplyResources(this.toolStripSeparator4, "toolStripSeparator4");
            // 
            // tsbExportText
            // 
            resources.ApplyResources(this.tsbExportText, "tsbExportText");
            this.tsbExportText.Name = "tsbExportText";
            this.tsbExportText.Click += new System.EventHandler(this.TsbExportText_Click);
            // 
            // lvCDL
            // 
            resources.ApplyResources(this.lvCDL, "lvCDL");
            this.lvCDL.AllowColumnReorder = false;
            this.lvCDL.AllowColumnResize = true;
            this.lvCDL.AlwaysScroll = false;
            this.lvCDL.CellHeightPadding = 0;
            this.lvCDL.FullRowSelect = true;
            this.lvCDL.HorizontalOrientation = false;
            this.lvCDL.LetKeysModifySelection = false;
            this.lvCDL.Name = "lvCDL";
            this.lvCDL.RowCount = 0;
            this.lvCDL.ScrollSpeed = 3;
            this.lvCDL.QueryItemText += new BizHawk.Client.EmuHawk.InputRoll.QueryItemTextHandler(this.LvCDL_QueryItemText);
            // 
            // CDL
            // 
            resources.ApplyResources(this, "$this");
            this.AllowDrop = true;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lvCDL);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "CDL";
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