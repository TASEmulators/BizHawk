using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	partial class Cheats
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
			this.CheatListView = new InputRoll();
			this.CheatsContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.ToggleContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.RemoveContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.DisableAllContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ViewInHexEditorContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.CheatsMenu = new MenuStripEx();
			this.FileSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.NewMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.OpenMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SaveMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SaveAsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.AppendMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.RecentSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.ExitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.CheatsSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.RemoveCheatMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.InsertSeparatorMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.MoveUpMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.MoveDownMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SelectAllMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
			this.ToggleMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.DisableAllCheatsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.GameGenieSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.OpenGameGenieEncoderDecoderMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.OptionsSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.AlwaysLoadCheatsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.AutoSaveCheatsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.DisableCheatsOnLoadMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
			this.AutoloadMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SaveWindowPositionMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.AlwaysOnTopMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FloatingWindowMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.RestoreWindowSizeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStrip1 = new ToolStripEx();
			this.NewToolBarItem = new System.Windows.Forms.ToolStripButton();
			this.OpenToolBarItem = new System.Windows.Forms.ToolStripButton();
			this.SaveToolBarItem = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.RemoveToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.SeparatorToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.MoveUpToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.MoveDownToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.GameGenieToolbarSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.LoadGameGenieToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.TotalLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.MessageLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.CheatGroupBox = new System.Windows.Forms.GroupBox();
			this.CheatEditor = new BizHawk.Client.EmuHawk.CheatEdit();
			this.CheatsContextMenu.SuspendLayout();
			this.CheatsMenu.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			this.CheatGroupBox.SuspendLayout();
			this.SuspendLayout();
			// 
			// CheatListView
			// 
			this.CheatListView.CellWidthPadding = 3;
			this.CheatListView.AllowColumnReorder = true;
			this.CheatListView.AllowColumnResize = true;
			this.CheatListView.MultiSelect = true;
			this.CheatListView.AllowDrop = true;
			this.CheatListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.CheatListView.ContextMenuStrip = this.CheatsContextMenu;
			this.CheatListView.FullRowSelect = true;
			this.CheatListView.GridLines = true;
			this.CheatListView.RowCount = 0;
			this.CheatListView.Location = new System.Drawing.Point(12, 72);
			this.CheatListView.Name = "CheatListView";
			this.CheatListView.Size = new System.Drawing.Size(414, 321);
			this.CheatListView.TabIndex = 1;
			this.CheatListView.ColumnClick += new BizHawk.Client.EmuHawk.InputRoll.ColumnClickEventHandler(this.CheatListView_ColumnClick);
			this.CheatListView.SelectedIndexChanged += new System.EventHandler(this.CheatListView_SelectedIndexChanged);
			this.CheatListView.DragDrop += new System.Windows.Forms.DragEventHandler(this.NewCheatForm_DragDrop);
			this.CheatListView.DragEnter += new System.Windows.Forms.DragEventHandler(this.NewCheatForm_DragEnter);
			this.CheatListView.DoubleClick += new System.EventHandler(this.CheatListView_DoubleClick);
			this.CheatListView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.CheatListView_KeyDown);
			// 
			// CheatsContextMenu
			// 
			this.CheatsContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToggleContextMenuItem,
            this.RemoveContextMenuItem,
            this.DisableAllContextMenuItem,
            this.ViewInHexEditorContextMenuItem});
			this.CheatsContextMenu.Name = "contextMenuStrip1";
			this.CheatsContextMenu.Size = new System.Drawing.Size(170, 92);
			this.CheatsContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.CheatsContextMenu_Opening);
			// 
			// ToggleContextMenuItem
			// 
			this.ToggleContextMenuItem.Name = "ToggleContextMenuItem";
			this.ToggleContextMenuItem.ShortcutKeyDisplayString = "Enter";
			this.ToggleContextMenuItem.Size = new System.Drawing.Size(169, 22);
			this.ToggleContextMenuItem.Text = "&Toggle";
			this.ToggleContextMenuItem.Click += new System.EventHandler(this.ToggleMenuItem_Click);
			// 
			// RemoveContextMenuItem
			// 
			this.RemoveContextMenuItem.Name = "RemoveContextMenuItem";
			this.RemoveContextMenuItem.ShortcutKeyDisplayString = "Delete";
			this.RemoveContextMenuItem.Size = new System.Drawing.Size(169, 22);
			this.RemoveContextMenuItem.Text = "&Remove";
			this.RemoveContextMenuItem.Click += new System.EventHandler(this.RemoveCheatMenuItem_Click);
			// 
			// DisableAllContextMenuItem
			// 
			this.DisableAllContextMenuItem.Name = "DisableAllContextMenuItem";
			this.DisableAllContextMenuItem.Size = new System.Drawing.Size(169, 22);
			this.DisableAllContextMenuItem.Text = "&Disable All";
			this.DisableAllContextMenuItem.Click += new System.EventHandler(this.DisableAllCheatsMenuItem_Click);
			// 
			// ViewInHexEditorContextMenuItem
			// 
			this.ViewInHexEditorContextMenuItem.Name = "ViewInHexEditorContextMenuItem";
			this.ViewInHexEditorContextMenuItem.Size = new System.Drawing.Size(169, 22);
			this.ViewInHexEditorContextMenuItem.Text = "View in Hex Editor";
			this.ViewInHexEditorContextMenuItem.Click += new System.EventHandler(this.ViewInHexEditorContextMenuItem_Click);
			// 
			// CheatsMenu
			// 
			this.CheatsMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu,
            this.CheatsSubMenu,
            this.OptionsSubMenu});
			this.CheatsMenu.Name = "CheatsMenu";
			this.CheatsMenu.TabIndex = 2;
			this.CheatsMenu.Text = "menuStrip1";
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
            this.toolStripSeparator1,
            this.ExitMenuItem});
			this.FileSubMenu.Name = "FileSubMenu";
			this.FileSubMenu.Size = new System.Drawing.Size(37, 20);
			this.FileSubMenu.Text = "&File";
			this.FileSubMenu.DropDownOpened += new System.EventHandler(this.FileSubMenu_DropDownOpened);
			// 
			// NewMenuItem
			// 
			this.NewMenuItem.Name = "NewMenuItem";
			this.NewMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
			this.NewMenuItem.Size = new System.Drawing.Size(195, 22);
			this.NewMenuItem.Text = "&New";
			this.NewMenuItem.Click += new System.EventHandler(this.NewMenuItem_Click);
			// 
			// OpenMenuItem
			// 
			this.OpenMenuItem.Name = "OpenMenuItem";
			this.OpenMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.OpenMenuItem.Size = new System.Drawing.Size(195, 22);
			this.OpenMenuItem.Text = "&Open...";
			this.OpenMenuItem.Click += new System.EventHandler(this.OpenMenuItem_Click);
			// 
			// SaveMenuItem
			// 
			this.SaveMenuItem.Name = "SaveMenuItem";
			this.SaveMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			this.SaveMenuItem.Size = new System.Drawing.Size(195, 22);
			this.SaveMenuItem.Text = "&Save";
			this.SaveMenuItem.Click += new System.EventHandler(this.SaveMenuItem_Click);
			// 
			// SaveAsMenuItem
			// 
			this.SaveAsMenuItem.Name = "SaveAsMenuItem";
			this.SaveAsMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.S)));
			this.SaveAsMenuItem.Size = new System.Drawing.Size(195, 22);
			this.SaveAsMenuItem.Text = "Save &As...";
			this.SaveAsMenuItem.Click += new System.EventHandler(this.SaveAsMenuItem_Click);
			// 
			// AppendMenuItem
			// 
			this.AppendMenuItem.Name = "AppendMenuItem";
			this.AppendMenuItem.Size = new System.Drawing.Size(195, 22);
			this.AppendMenuItem.Text = "Append File";
			// 
			// RecentSubMenu
			// 
			this.RecentSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator4});
			this.RecentSubMenu.Name = "RecentSubMenu";
			this.RecentSubMenu.Size = new System.Drawing.Size(195, 22);
			this.RecentSubMenu.Text = "Recent";
			this.RecentSubMenu.DropDownOpened += new System.EventHandler(this.RecentSubMenu_DropDownOpened);
			// 
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size(57, 6);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(192, 6);
			// 
			// ExitMenuItem
			// 
			this.ExitMenuItem.Name = "ExitMenuItem";
			this.ExitMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
			this.ExitMenuItem.Size = new System.Drawing.Size(195, 22);
			this.ExitMenuItem.Text = "E&xit";
			this.ExitMenuItem.Click += new System.EventHandler(this.ExitMenuItem_Click);
			// 
			// CheatsSubMenu
			// 
			this.CheatsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.RemoveCheatMenuItem,
            this.InsertSeparatorMenuItem,
            this.toolStripSeparator3,
            this.MoveUpMenuItem,
            this.MoveDownMenuItem,
            this.SelectAllMenuItem,
            this.toolStripSeparator6,
            this.ToggleMenuItem,
            this.DisableAllCheatsMenuItem,
            this.GameGenieSeparator,
            this.OpenGameGenieEncoderDecoderMenuItem});
			this.CheatsSubMenu.Name = "CheatsSubMenu";
			this.CheatsSubMenu.Size = new System.Drawing.Size(55, 20);
			this.CheatsSubMenu.Text = "&Cheats";
			this.CheatsSubMenu.DropDownOpened += new System.EventHandler(this.CheatsSubMenu_DropDownOpened);
			// 
			// RemoveCheatMenuItem
			// 
			this.RemoveCheatMenuItem.Name = "RemoveCheatMenuItem";
			this.RemoveCheatMenuItem.ShortcutKeyDisplayString = "Delete";
			this.RemoveCheatMenuItem.Size = new System.Drawing.Size(233, 22);
			this.RemoveCheatMenuItem.Text = "&Remove Cheat";
			this.RemoveCheatMenuItem.Click += new System.EventHandler(this.RemoveCheatMenuItem_Click);
			// 
			// InsertSeparatorMenuItem
			// 
			this.InsertSeparatorMenuItem.Name = "InsertSeparatorMenuItem";
			this.InsertSeparatorMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.I)));
			this.InsertSeparatorMenuItem.Size = new System.Drawing.Size(233, 22);
			this.InsertSeparatorMenuItem.Text = "Insert Separator";
			this.InsertSeparatorMenuItem.Click += new System.EventHandler(this.InsertSeparatorMenuItem_Click);
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(230, 6);
			// 
			// MoveUpMenuItem
			// 
			this.MoveUpMenuItem.Name = "MoveUpMenuItem";
			this.MoveUpMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.U)));
			this.MoveUpMenuItem.Size = new System.Drawing.Size(233, 22);
			this.MoveUpMenuItem.Text = "Move &Up";
			this.MoveUpMenuItem.Click += new System.EventHandler(this.MoveUpMenuItem_Click);
			// 
			// MoveDownMenuItem
			// 
			this.MoveDownMenuItem.Name = "MoveDownMenuItem";
			this.MoveDownMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
			this.MoveDownMenuItem.Size = new System.Drawing.Size(233, 22);
			this.MoveDownMenuItem.Text = "Move &Down";
			this.MoveDownMenuItem.Click += new System.EventHandler(this.MoveDownMenuItem_Click);
			// 
			// SelectAllMenuItem
			// 
			this.SelectAllMenuItem.Name = "SelectAllMenuItem";
			this.SelectAllMenuItem.ShortcutKeyDisplayString = "Ctrl+A";
			this.SelectAllMenuItem.Size = new System.Drawing.Size(233, 22);
			this.SelectAllMenuItem.Text = "Select &All";
			this.SelectAllMenuItem.Click += new System.EventHandler(this.SelectAllMenuItem_Click);
			// 
			// toolStripSeparator6
			// 
			this.toolStripSeparator6.Name = "toolStripSeparator6";
			this.toolStripSeparator6.Size = new System.Drawing.Size(230, 6);
			// 
			// ToggleMenuItem
			// 
			this.ToggleMenuItem.Name = "ToggleMenuItem";
			this.ToggleMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Enter)));
			this.ToggleMenuItem.ShortcutKeyDisplayString = "Ctrl + Enter";
			this.ToggleMenuItem.Size = new System.Drawing.Size(233, 22);
			this.ToggleMenuItem.Text = "&Toggle";
			this.ToggleMenuItem.Click += new System.EventHandler(this.ToggleMenuItem_Click);
			// 
			// DisableAllCheatsMenuItem
			// 
			this.DisableAllCheatsMenuItem.Name = "DisableAllCheatsMenuItem";
			this.DisableAllCheatsMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Back)));
			this.DisableAllCheatsMenuItem.ShortcutKeyDisplayString = "Ctrl + Backspace";
			this.DisableAllCheatsMenuItem.Size = new System.Drawing.Size(233, 22);
			this.DisableAllCheatsMenuItem.Text = "Disable all";
			this.DisableAllCheatsMenuItem.Click += new System.EventHandler(this.DisableAllCheatsMenuItem_Click);
			// 
			// GameGenieSeparator
			// 
			this.GameGenieSeparator.Name = "GameGenieSeparator";
			this.GameGenieSeparator.Size = new System.Drawing.Size(230, 6);
			// 
			// OpenGameGenieEncoderDecoderMenuItem
			// 
			this.OpenGameGenieEncoderDecoderMenuItem.Name = "OpenGameGenieEncoderDecoderMenuItem";
			this.OpenGameGenieEncoderDecoderMenuItem.Size = new System.Drawing.Size(233, 22);
			this.OpenGameGenieEncoderDecoderMenuItem.Text = "Code Converter";
			this.OpenGameGenieEncoderDecoderMenuItem.Click += new System.EventHandler(this.OpenGameGenieEncoderDecoderMenuItem_Click);
			// 
			// OptionsSubMenu
			// 
			this.OptionsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.AlwaysLoadCheatsMenuItem,
            this.AutoSaveCheatsMenuItem,
            this.DisableCheatsOnLoadMenuItem,
            this.toolStripSeparator7,
            this.AutoloadMenuItem,
            this.SaveWindowPositionMenuItem,
            this.AlwaysOnTopMenuItem,
            this.FloatingWindowMenuItem,
            this.toolStripSeparator5,
            this.RestoreWindowSizeMenuItem});
			this.OptionsSubMenu.Name = "OptionsSubMenu";
			this.OptionsSubMenu.Size = new System.Drawing.Size(61, 20);
			this.OptionsSubMenu.Text = "&Options";
			this.OptionsSubMenu.DropDownOpened += new System.EventHandler(this.OptionsSubMenu_DropDownOpened);
			// 
			// AlwaysLoadCheatsMenuItem
			// 
			this.AlwaysLoadCheatsMenuItem.Name = "AlwaysLoadCheatsMenuItem";
			this.AlwaysLoadCheatsMenuItem.Size = new System.Drawing.Size(199, 22);
			this.AlwaysLoadCheatsMenuItem.Text = "Always load cheats";
			this.AlwaysLoadCheatsMenuItem.Click += new System.EventHandler(this.AlwaysLoadCheatsMenuItem_Click);
			// 
			// AutoSaveCheatsMenuItem
			// 
			this.AutoSaveCheatsMenuItem.Name = "AutoSaveCheatsMenuItem";
			this.AutoSaveCheatsMenuItem.Size = new System.Drawing.Size(199, 22);
			this.AutoSaveCheatsMenuItem.Text = "Autosave cheats";
			this.AutoSaveCheatsMenuItem.Click += new System.EventHandler(this.AutoSaveCheatsMenuItem_Click);
			// 
			// DisableCheatsOnLoadMenuItem
			// 
			this.DisableCheatsOnLoadMenuItem.Name = "DisableCheatsOnLoadMenuItem";
			this.DisableCheatsOnLoadMenuItem.Size = new System.Drawing.Size(199, 22);
			this.DisableCheatsOnLoadMenuItem.Text = "Disable Cheats on Load";
			this.DisableCheatsOnLoadMenuItem.Click += new System.EventHandler(this.CheatsOnOffLoadMenuItem_Click);
			// 
			// toolStripSeparator7
			// 
			this.toolStripSeparator7.Name = "toolStripSeparator7";
			this.toolStripSeparator7.Size = new System.Drawing.Size(196, 6);
			// 
			// AutoloadMenuItem
			// 
			this.AutoloadMenuItem.Name = "AutoloadMenuItem";
			this.AutoloadMenuItem.Size = new System.Drawing.Size(199, 22);
			this.AutoloadMenuItem.Text = "Autoload";
			this.AutoloadMenuItem.Click += new System.EventHandler(this.AutoloadMenuItem_Click);
			// 
			// SaveWindowPositionMenuItem
			// 
			this.SaveWindowPositionMenuItem.Name = "SaveWindowPositionMenuItem";
			this.SaveWindowPositionMenuItem.Size = new System.Drawing.Size(199, 22);
			this.SaveWindowPositionMenuItem.Text = "Save Window Position";
			this.SaveWindowPositionMenuItem.Click += new System.EventHandler(this.SaveWindowPositionMenuItem_Click);
			// 
			// AlwaysOnTopMenuItem
			// 
			this.AlwaysOnTopMenuItem.Name = "AlwaysOnTopMenuItem";
			this.AlwaysOnTopMenuItem.Size = new System.Drawing.Size(199, 22);
			this.AlwaysOnTopMenuItem.Text = "Always on &Top";
			this.AlwaysOnTopMenuItem.Click += new System.EventHandler(this.AlwaysOnTopMenuItem_Click);
			// 
			// FloatingWindowMenuItem
			// 
			this.FloatingWindowMenuItem.Name = "FloatingWindowMenuItem";
			this.FloatingWindowMenuItem.Size = new System.Drawing.Size(199, 22);
			this.FloatingWindowMenuItem.Text = "Floating Window";
			this.FloatingWindowMenuItem.Click += new System.EventHandler(this.FloatingWindowMenuItem_Click);
			// 
			// toolStripSeparator5
			// 
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			this.toolStripSeparator5.Size = new System.Drawing.Size(196, 6);
			// 
			// RestoreWindowSizeMenuItem
			// 
			this.RestoreWindowSizeMenuItem.Name = "RestoreWindowSizeMenuItem";
			this.RestoreWindowSizeMenuItem.Size = new System.Drawing.Size(199, 22);
			this.RestoreWindowSizeMenuItem.Text = "Restore Default Settings";
			this.RestoreWindowSizeMenuItem.Click += new System.EventHandler(this.RestoreDefaultsMenuItem_Click);
			// 
			// toolStrip1
			// 
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.NewToolBarItem,
            this.OpenToolBarItem,
            this.SaveToolBarItem,
            this.toolStripSeparator,
            this.RemoveToolbarItem,
            this.SeparatorToolbarItem,
            this.toolStripSeparator2,
            this.MoveUpToolbarItem,
            this.MoveDownToolbarItem,
            this.GameGenieToolbarSeparator,
            this.LoadGameGenieToolbarItem});
			this.toolStrip1.Location = new System.Drawing.Point(0, 24);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.TabIndex = 3;
			// 
			// NewToolBarItem
			// 
			this.NewToolBarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.NewToolBarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.NewToolBarItem.Name = "NewToolBarItem";
			this.NewToolBarItem.Size = new System.Drawing.Size(23, 22);
			this.NewToolBarItem.Text = "&New";
			this.NewToolBarItem.Click += new System.EventHandler(this.NewMenuItem_Click);
			// 
			// OpenToolBarItem
			// 
			this.OpenToolBarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.OpenToolBarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.OpenToolBarItem.Name = "OpenToolBarItem";
			this.OpenToolBarItem.Size = new System.Drawing.Size(23, 22);
			this.OpenToolBarItem.Text = "&Open";
			this.OpenToolBarItem.Click += new System.EventHandler(this.OpenMenuItem_Click);
			// 
			// SaveToolBarItem
			// 
			this.SaveToolBarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.SaveToolBarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.SaveToolBarItem.Name = "SaveToolBarItem";
			this.SaveToolBarItem.Size = new System.Drawing.Size(23, 22);
			this.SaveToolBarItem.Text = "&Save";
			this.SaveToolBarItem.Click += new System.EventHandler(this.SaveMenuItem_Click);
			// 
			// toolStripSeparator
			// 
			this.toolStripSeparator.Name = "toolStripSeparator";
			this.toolStripSeparator.Size = new System.Drawing.Size(6, 25);
			// 
			// RemoveToolbarItem
			// 
			this.RemoveToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.RemoveToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.RemoveToolbarItem.Name = "RemoveToolbarItem";
			this.RemoveToolbarItem.Size = new System.Drawing.Size(23, 22);
			this.RemoveToolbarItem.Text = "&Remove";
			this.RemoveToolbarItem.Click += new System.EventHandler(this.RemoveCheatMenuItem_Click);
			// 
			// SeparatorToolbarItem
			// 
			this.SeparatorToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.SeparatorToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.SeparatorToolbarItem.Name = "SeparatorToolbarItem";
			this.SeparatorToolbarItem.Size = new System.Drawing.Size(23, 22);
			this.SeparatorToolbarItem.Text = "Insert Separator";
			this.SeparatorToolbarItem.Click += new System.EventHandler(this.InsertSeparatorMenuItem_Click);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
			// 
			// MoveUpToolbarItem
			// 
			this.MoveUpToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.MoveUpToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.MoveUpToolbarItem.Name = "MoveUpToolbarItem";
			this.MoveUpToolbarItem.Size = new System.Drawing.Size(23, 22);
			this.MoveUpToolbarItem.Text = "Move Up";
			this.MoveUpToolbarItem.Click += new System.EventHandler(this.MoveUpMenuItem_Click);
			// 
			// MoveDownToolbarItem
			// 
			this.MoveDownToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.MoveDownToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.MoveDownToolbarItem.Name = "MoveDownToolbarItem";
			this.MoveDownToolbarItem.Size = new System.Drawing.Size(23, 22);
			this.MoveDownToolbarItem.Text = "Move Down";
			this.MoveDownToolbarItem.Click += new System.EventHandler(this.MoveDownMenuItem_Click);
			// 
			// GameGenieToolbarSeparator
			// 
			this.GameGenieToolbarSeparator.Name = "GameGenieToolbarSeparator";
			this.GameGenieToolbarSeparator.Size = new System.Drawing.Size(6, 25);
			// 
			// LoadGameGenieToolbarItem
			// 
			this.LoadGameGenieToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.LoadGameGenieToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.LoadGameGenieToolbarItem.Name = "LoadGameGenieToolbarItem";
			this.LoadGameGenieToolbarItem.Size = new System.Drawing.Size(75, 22);
			this.LoadGameGenieToolbarItem.Text = "Code Converter";
			this.LoadGameGenieToolbarItem.ToolTipText = "Open the Cheat Code Converter";
			this.LoadGameGenieToolbarItem.Click += new System.EventHandler(this.OpenGameGenieEncoderDecoderMenuItem_Click);
			// 
			// TotalLabel
			// 
			this.TotalLabel.Location = new System.Drawing.Point(9, 52);
			this.TotalLabel.Name = "TotalLabel";
			this.TotalLabel.Text = "0 Cheats";
			// 
			// MessageLabel
			// 
			this.MessageLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.MessageLabel.Location = new System.Drawing.Point(13, 397);
			this.MessageLabel.Name = "MessageLabel";
			this.MessageLabel.Text = "        ";
			// 
			// CheatGroupBox
			// 
			this.CheatGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.CheatGroupBox.Controls.Add(this.CheatEditor);
			this.CheatGroupBox.Location = new System.Drawing.Point(432, 66);
			this.CheatGroupBox.Name = "CheatGroupBox";
			this.CheatGroupBox.Size = new System.Drawing.Size(202, 327);
			this.CheatGroupBox.TabIndex = 8;
			this.CheatGroupBox.TabStop = false;
			this.CheatGroupBox.Text = "New Cheat";
			// 
			// CheatEditor
			// 
			this.CheatEditor.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.CheatEditor.Location = new System.Drawing.Point(6, 14);
			this.CheatEditor.MemoryDomains = null;
			this.CheatEditor.Name = "CheatEditor";
			this.CheatEditor.Size = new System.Drawing.Size(190, 307);
			this.CheatEditor.TabIndex = 0;
			// 
			// Cheats
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(646, 413);
			this.Controls.Add(this.CheatGroupBox);
			this.Controls.Add(this.MessageLabel);
			this.Controls.Add(this.TotalLabel);
			this.Controls.Add(this.toolStrip1);
			this.Controls.Add(this.CheatsMenu);
			this.Controls.Add(this.CheatListView);
			this.MinimumSize = new System.Drawing.Size(285, 384);
			this.Name = "Cheats";
			this.Text = "Cheats";
			this.Load += new System.EventHandler(this.Cheats_Load);
			this.DragDrop += new System.Windows.Forms.DragEventHandler(this.NewCheatForm_DragDrop);
			this.DragEnter += new System.Windows.Forms.DragEventHandler(this.NewCheatForm_DragEnter);
			this.CheatsContextMenu.ResumeLayout(false);
			this.CheatsMenu.ResumeLayout(false);
			this.CheatsMenu.PerformLayout();
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.CheatGroupBox.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private InputRoll CheatListView;
		private MenuStripEx CheatsMenu;
		private System.Windows.Forms.ToolStripMenuItem FileSubMenu;
		private System.Windows.Forms.ToolStripMenuItem NewMenuItem;
		private System.Windows.Forms.ToolStripMenuItem OpenMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SaveMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SaveAsMenuItem;
		private System.Windows.Forms.ToolStripMenuItem AppendMenuItem;
		private System.Windows.Forms.ToolStripMenuItem RecentSubMenu;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem ExitMenuItem;
		private System.Windows.Forms.ToolStripMenuItem CheatsSubMenu;
		private System.Windows.Forms.ToolStripMenuItem RemoveCheatMenuItem;
		private System.Windows.Forms.ToolStripMenuItem InsertSeparatorMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripMenuItem MoveUpMenuItem;
		private System.Windows.Forms.ToolStripMenuItem MoveDownMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SelectAllMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
		private System.Windows.Forms.ToolStripMenuItem DisableAllCheatsMenuItem;
		private System.Windows.Forms.ToolStripSeparator GameGenieSeparator;
		private System.Windows.Forms.ToolStripMenuItem OpenGameGenieEncoderDecoderMenuItem;
		private System.Windows.Forms.ToolStripMenuItem OptionsSubMenu;
		private System.Windows.Forms.ToolStripMenuItem AlwaysLoadCheatsMenuItem;
		private System.Windows.Forms.ToolStripMenuItem AutoSaveCheatsMenuItem;
		private System.Windows.Forms.ToolStripMenuItem DisableCheatsOnLoadMenuItem;
		private System.Windows.Forms.ToolStripMenuItem AutoloadMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SaveWindowPositionMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
		private System.Windows.Forms.ToolStripMenuItem RestoreWindowSizeMenuItem;
		private ToolStripEx toolStrip1;
		private System.Windows.Forms.ToolStripButton NewToolBarItem;
		private System.Windows.Forms.ToolStripButton OpenToolBarItem;
		private System.Windows.Forms.ToolStripButton SaveToolBarItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator;
		private System.Windows.Forms.ToolStripButton RemoveToolbarItem;
		private System.Windows.Forms.ToolStripButton SeparatorToolbarItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripButton MoveUpToolbarItem;
		private System.Windows.Forms.ToolStripButton MoveDownToolbarItem;
		private System.Windows.Forms.ToolStripButton LoadGameGenieToolbarItem;
		private BizHawk.WinForms.Controls.LocLabelEx TotalLabel;
		private BizHawk.WinForms.Controls.LocLabelEx MessageLabel;
		private System.Windows.Forms.ToolStripMenuItem AlwaysOnTopMenuItem;
		private System.Windows.Forms.ToolStripMenuItem ToggleMenuItem;
		private System.Windows.Forms.ToolStripSeparator GameGenieToolbarSeparator;
		private System.Windows.Forms.ContextMenuStrip CheatsContextMenu;
		private System.Windows.Forms.ToolStripMenuItem ToggleContextMenuItem;
		private System.Windows.Forms.ToolStripMenuItem RemoveContextMenuItem;
		private System.Windows.Forms.ToolStripMenuItem DisableAllContextMenuItem;
		private System.Windows.Forms.GroupBox CheatGroupBox;
		private CheatEdit CheatEditor;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
		private System.Windows.Forms.ToolStripMenuItem ViewInHexEditorContextMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FloatingWindowMenuItem;
	}
}