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
			this.CheatListView = new BizHawk.Client.EmuHawk.InputRoll();
			this.CheatsContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.ToggleContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.RemoveContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.DisableAllContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.ViewInHexEditorContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.CheatsMenu = new BizHawk.WinForms.Controls.MenuStripEx();
			this.FileSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.NewMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.OpenMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.SaveMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.SaveAsMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.AppendMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.RecentSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.toolStripSeparator4 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.CheatsSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.RemoveCheatMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.InsertSeparatorMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.toolStripSeparator3 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.MoveUpMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.MoveDownMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.SelectAllMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.toolStripSeparator6 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.ToggleMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.DisableAllCheatsMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.GameGenieSeparator = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.OpenGameGenieEncoderDecoderMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.OptionsSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.AlwaysLoadCheatsMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.AutoSaveCheatsMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.DisableCheatsOnLoadMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.toolStrip1 = new BizHawk.WinForms.Controls.ToolStripEx();
			this.NewToolBarItem = new System.Windows.Forms.ToolStripButton();
			this.OpenToolBarItem = new System.Windows.Forms.ToolStripButton();
			this.SaveToolBarItem = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.RemoveToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.SeparatorToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator2 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.MoveUpToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.MoveDownToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.GameGenieToolbarSeparator = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
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
			this.CheatListView.AllowColumnReorder = true;
			this.CheatListView.AllowColumnResize = true;
			this.CheatListView.AllowDrop = true;
			this.CheatListView.AllowMassNavigationShortcuts = true;
			this.CheatListView.AllowRightClickSelection = true;
			this.CheatListView.AlwaysScroll = false;
			this.CheatListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.CheatListView.CellHeightPadding = 0;
			this.CheatListView.ContextMenuStrip = this.CheatsContextMenu;
			this.CheatListView.FullRowSelect = true;
			this.CheatListView.HorizontalOrientation = false;
			this.CheatListView.LetKeysModifySelection = false;
			this.CheatListView.Location = new System.Drawing.Point(12, 72);
			this.CheatListView.Name = "CheatListView";
			this.CheatListView.RowCount = 0;
			this.CheatListView.ScrollSpeed = 0;
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
			this.CheatsContextMenu.Size = new System.Drawing.Size(171, 92);
			this.CheatsContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.CheatsContextMenu_Opening);
			// 
			// ToggleContextMenuItem
			// 
			this.ToggleContextMenuItem.ShortcutKeyDisplayString = "Enter";
			this.ToggleContextMenuItem.Text = "&Toggle";
			this.ToggleContextMenuItem.Click += new System.EventHandler(this.ToggleMenuItem_Click);
			// 
			// RemoveContextMenuItem
			// 
			this.RemoveContextMenuItem.ShortcutKeyDisplayString = "Delete";
			this.RemoveContextMenuItem.Text = "&Remove";
			this.RemoveContextMenuItem.Click += new System.EventHandler(this.RemoveCheatMenuItem_Click);
			// 
			// DisableAllContextMenuItem
			// 
			this.DisableAllContextMenuItem.Text = "&Disable All";
			this.DisableAllContextMenuItem.Click += new System.EventHandler(this.DisableAllCheatsMenuItem_Click);
			// 
			// ViewInHexEditorContextMenuItem
			// 
			this.ViewInHexEditorContextMenuItem.Text = "View in Hex Editor";
			this.ViewInHexEditorContextMenuItem.Click += new System.EventHandler(this.ViewInHexEditorContextMenuItem_Click);
			// 
			// CheatsMenu
			// 
			this.CheatsMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu,
            this.CheatsSubMenu,
            this.OptionsSubMenu});
			this.CheatsMenu.TabIndex = 2;
			// 
			// FileSubMenu
			// 
			this.FileSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.NewMenuItem,
            this.OpenMenuItem,
            this.SaveMenuItem,
            this.SaveAsMenuItem,
            this.AppendMenuItem,
            this.RecentSubMenu});
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
			this.SaveAsMenuItem.Text = "Save &As...";
			this.SaveAsMenuItem.Click += new System.EventHandler(this.SaveAsMenuItem_Click);
			// 
			// AppendMenuItem
			// 
			this.AppendMenuItem.Text = "Append File";
			// 
			// RecentSubMenu
			// 
			this.RecentSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator4});
			this.RecentSubMenu.Text = "Recent";
			this.RecentSubMenu.DropDownOpened += new System.EventHandler(this.RecentSubMenu_DropDownOpened);
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
			this.CheatsSubMenu.Text = "&Cheats";
			this.CheatsSubMenu.DropDownOpened += new System.EventHandler(this.CheatsSubMenu_DropDownOpened);
			// 
			// RemoveCheatMenuItem
			// 
			this.RemoveCheatMenuItem.ShortcutKeyDisplayString = "Delete";
			this.RemoveCheatMenuItem.Text = "&Remove Cheat";
			this.RemoveCheatMenuItem.Click += new System.EventHandler(this.RemoveCheatMenuItem_Click);
			// 
			// InsertSeparatorMenuItem
			// 
			this.InsertSeparatorMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.I)));
			this.InsertSeparatorMenuItem.Text = "Insert Separator";
			this.InsertSeparatorMenuItem.Click += new System.EventHandler(this.InsertSeparatorMenuItem_Click);
			// 
			// MoveUpMenuItem
			// 
			this.MoveUpMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.U)));
			this.MoveUpMenuItem.Text = "Move &Up";
			this.MoveUpMenuItem.Click += new System.EventHandler(this.MoveUpMenuItem_Click);
			// 
			// MoveDownMenuItem
			// 
			this.MoveDownMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
			this.MoveDownMenuItem.Text = "Move &Down";
			this.MoveDownMenuItem.Click += new System.EventHandler(this.MoveDownMenuItem_Click);
			// 
			// SelectAllMenuItem
			// 
			this.SelectAllMenuItem.ShortcutKeyDisplayString = "Ctrl+A";
			this.SelectAllMenuItem.Text = "Select &All";
			this.SelectAllMenuItem.Click += new System.EventHandler(this.SelectAllMenuItem_Click);
			// 
			// ToggleMenuItem
			// 
			this.ToggleMenuItem.ShortcutKeyDisplayString = "Ctrl + Enter";
			this.ToggleMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Return)));
			this.ToggleMenuItem.Text = "&Toggle";
			this.ToggleMenuItem.Click += new System.EventHandler(this.ToggleMenuItem_Click);
			// 
			// DisableAllCheatsMenuItem
			// 
			this.DisableAllCheatsMenuItem.ShortcutKeyDisplayString = "Ctrl + Backspace";
			this.DisableAllCheatsMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Back)));
			this.DisableAllCheatsMenuItem.Text = "Disable all";
			this.DisableAllCheatsMenuItem.Click += new System.EventHandler(this.DisableAllCheatsMenuItem_Click);
			// 
			// OpenGameGenieEncoderDecoderMenuItem
			// 
			this.OpenGameGenieEncoderDecoderMenuItem.Text = "Code Converter";
			this.OpenGameGenieEncoderDecoderMenuItem.Click += new System.EventHandler(this.OpenGameGenieEncoderDecoderMenuItem_Click);
			// 
			// OptionsSubMenu
			// 
			this.OptionsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.AlwaysLoadCheatsMenuItem,
            this.AutoSaveCheatsMenuItem,
            this.DisableCheatsOnLoadMenuItem});
			this.OptionsSubMenu.Text = "&Settings";
			this.OptionsSubMenu.DropDownOpened += new System.EventHandler(this.SettingsSubMenu_DropDownOpened);
			// 
			// AlwaysLoadCheatsMenuItem
			// 
			this.AlwaysLoadCheatsMenuItem.Text = "Always load cheats";
			this.AlwaysLoadCheatsMenuItem.Click += new System.EventHandler(this.AlwaysLoadCheatsMenuItem_Click);
			// 
			// AutoSaveCheatsMenuItem
			// 
			this.AutoSaveCheatsMenuItem.Text = "Autosave cheats";
			this.AutoSaveCheatsMenuItem.Click += new System.EventHandler(this.AutoSaveCheatsMenuItem_Click);
			// 
			// DisableCheatsOnLoadMenuItem
			// 
			this.DisableCheatsOnLoadMenuItem.Text = "Disable Cheats on Load";
			this.DisableCheatsOnLoadMenuItem.Click += new System.EventHandler(this.CheatsOnOffLoadMenuItem_Click);
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
			// LoadGameGenieToolbarItem
			// 
			this.LoadGameGenieToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.LoadGameGenieToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.LoadGameGenieToolbarItem.Name = "LoadGameGenieToolbarItem";
			this.LoadGameGenieToolbarItem.Size = new System.Drawing.Size(94, 22);
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
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FileSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx NewMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx OpenMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SaveMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SaveAsMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx AppendMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RecentSubMenu;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator4;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CheatsSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RemoveCheatMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx InsertSeparatorMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator3;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx MoveUpMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx MoveDownMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SelectAllMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator6;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DisableAllCheatsMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx GameGenieSeparator;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx OpenGameGenieEncoderDecoderMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx OptionsSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx AlwaysLoadCheatsMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx AutoSaveCheatsMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DisableCheatsOnLoadMenuItem;
		private ToolStripEx toolStrip1;
		private System.Windows.Forms.ToolStripButton NewToolBarItem;
		private System.Windows.Forms.ToolStripButton OpenToolBarItem;
		private System.Windows.Forms.ToolStripButton SaveToolBarItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator;
		private System.Windows.Forms.ToolStripButton RemoveToolbarItem;
		private System.Windows.Forms.ToolStripButton SeparatorToolbarItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator2;
		private System.Windows.Forms.ToolStripButton MoveUpToolbarItem;
		private System.Windows.Forms.ToolStripButton MoveDownToolbarItem;
		private System.Windows.Forms.ToolStripButton LoadGameGenieToolbarItem;
		private BizHawk.WinForms.Controls.LocLabelEx TotalLabel;
		private BizHawk.WinForms.Controls.LocLabelEx MessageLabel;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ToggleMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx GameGenieToolbarSeparator;
		private System.Windows.Forms.ContextMenuStrip CheatsContextMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ToggleContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RemoveContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DisableAllContextMenuItem;
		private System.Windows.Forms.GroupBox CheatGroupBox;
		private CheatEdit CheatEditor;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ViewInHexEditorContextMenuItem;
	}
}