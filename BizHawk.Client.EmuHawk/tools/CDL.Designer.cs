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
			this.lvCDL = new BizHawk.Client.EmuHawk.VirtualListView();
			this.colAddress = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.colDomain = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.colPct = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.colMapped = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.colSize = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.colFlag01 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.colFlag02 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.colFlag04 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.colFlag08 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.colFlag10 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.colFlag20 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.colFlag40 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.colFlag80 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.menuStrip1.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// menuStrip1
			// 
			this.menuStrip1.ClickThrough = true;
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
			this.NewMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.NewFile;
			this.NewMenuItem.Name = "NewMenuItem";
			this.NewMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
			this.NewMenuItem.Size = new System.Drawing.Size(193, 22);
			this.NewMenuItem.Text = "&New";
			this.NewMenuItem.Click += new System.EventHandler(this.NewMenuItem_Click);
			// 
			// OpenMenuItem
			// 
			this.OpenMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.OpenFile;
			this.OpenMenuItem.Name = "OpenMenuItem";
			this.OpenMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.OpenMenuItem.Size = new System.Drawing.Size(193, 22);
			this.OpenMenuItem.Text = "&Open...";
			this.OpenMenuItem.Click += new System.EventHandler(this.OpenMenuItem_Click);
			// 
			// SaveMenuItem
			// 
			this.SaveMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.SaveAs;
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
			this.RecentSubMenu.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Recent;
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
			this.toolStrip1.ClickThrough = true;
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
			this.tsbLoggingActive.Image = ((System.Drawing.Image)(resources.GetObject("tsbLoggingActive.Image")));
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
			this.tsbViewUpdate.Image = ((System.Drawing.Image)(resources.GetObject("tsbViewUpdate.Image")));
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
			this.tsbExportText.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.LoadConfig;
			this.tsbExportText.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tsbExportText.Name = "tsbExportText";
			this.tsbExportText.Size = new System.Drawing.Size(87, 22);
			this.tsbExportText.Text = "To Clipboard";
			this.tsbExportText.Click += new System.EventHandler(this.tsbExportText_Click);
			// 
			// lvCDL
			// 
			this.lvCDL.BlazingFast = false;
			this.lvCDL.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colAddress,
            this.colDomain,
            this.colPct,
            this.colMapped,
            this.colSize,
            this.colFlag01,
            this.colFlag02,
            this.colFlag04,
            this.colFlag08,
            this.colFlag10,
            this.colFlag20,
            this.colFlag40,
            this.colFlag80});
			this.lvCDL.Dock = System.Windows.Forms.DockStyle.Fill;
			this.lvCDL.FullRowSelect = true;
			this.lvCDL.GridLines = true;
			this.lvCDL.ItemCount = 0;
			this.lvCDL.Location = new System.Drawing.Point(0, 49);
			this.lvCDL.Name = "lvCDL";
			this.lvCDL.SelectAllInProgress = false;
			this.lvCDL.selectedItem = -1;
			this.lvCDL.Size = new System.Drawing.Size(992, 323);
			this.lvCDL.TabIndex = 9;
			this.lvCDL.UseCompatibleStateImageBehavior = false;
			this.lvCDL.UseCustomBackground = true;
			this.lvCDL.View = System.Windows.Forms.View.Details;
			this.lvCDL.VirtualMode = true;
			this.lvCDL.QueryItemText += new BizHawk.Client.EmuHawk.QueryItemTextHandler(this.lvCDL_QueryItemText);
			// 
			// colAddress
			// 
			this.colAddress.Text = "CDL File @";
			this.colAddress.Width = 107;
			// 
			// colDomain
			// 
			this.colDomain.Text = "Domain";
			this.colDomain.Width = 126;
			// 
			// colPct
			// 
			this.colPct.Text = "%";
			this.colPct.Width = 58;
			// 
			// colMapped
			// 
			this.colMapped.Text = "Mapped";
			this.colMapped.Width = 64;
			// 
			// colSize
			// 
			this.colSize.Text = "Size";
			this.colSize.Width = 102;
			// 
			// colFlag01
			// 
			this.colFlag01.Text = "0x01";
			// 
			// colFlag02
			// 
			this.colFlag02.Text = "0x02";
			// 
			// colFlag04
			// 
			this.colFlag04.Text = "0x04";
			// 
			// colFlag08
			// 
			this.colFlag08.Text = "0x08";
			// 
			// colFlag10
			// 
			this.colFlag10.Text = "0x10";
			// 
			// colFlag20
			// 
			this.colFlag20.Text = "0x20";
			// 
			// colFlag40
			// 
			this.colFlag40.Text = "0x40";
			// 
			// colFlag80
			// 
			this.colFlag80.Text = "0x80";
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
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.menuStrip1;
			this.MinimumSize = new System.Drawing.Size(150, 130);
			this.Name = "CDL";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Code Data Logger";
			this.Load += new System.EventHandler(this.PCECDL_Load);
			this.DragDrop += new System.Windows.Forms.DragEventHandler(this.PCECDL_DragDrop);
			this.DragEnter += new System.Windows.Forms.DragEventHandler(this.PCECDL_DragEnter);
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
		private VirtualListView lvCDL;
		private System.Windows.Forms.ColumnHeader colAddress;
		private System.Windows.Forms.ColumnHeader colDomain;
		private System.Windows.Forms.ColumnHeader colPct;
		private System.Windows.Forms.ColumnHeader colMapped;
		private System.Windows.Forms.ColumnHeader colSize;
		private System.Windows.Forms.ColumnHeader colFlag01;
		private System.Windows.Forms.ColumnHeader colFlag02;
		private System.Windows.Forms.ColumnHeader colFlag04;
		private System.Windows.Forms.ColumnHeader colFlag08;
		private System.Windows.Forms.ColumnHeader colFlag10;
		private System.Windows.Forms.ColumnHeader colFlag20;
		private System.Windows.Forms.ColumnHeader colFlag40;
		private System.Windows.Forms.ColumnHeader colFlag80;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.ToolStripButton tsbExportText;
		private System.Windows.Forms.ToolStripMenuItem miAutoStart;
		private System.Windows.Forms.ToolStripMenuItem miAutoSave;
		private ToolStripEx toolStrip1;

	}
}