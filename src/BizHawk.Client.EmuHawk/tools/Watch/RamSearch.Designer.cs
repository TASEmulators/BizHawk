using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	partial class RamSearch
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
			this.SearchMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.TotalSearchLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.WatchListView = new BizHawk.Client.EmuHawk.InputRoll();
			this.ListViewContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.DoSearchContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.NewSearchContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.ContextMenuSeparator1 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.RemoveContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.AddToRamWatchContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.PokeContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.FreezeContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.UnfreezeAllContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.ContextMenuSeparator2 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.ViewInHexEditorContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.ContextMenuSeparator3 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.ClearPreviewContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.RamSearchMenu = new BizHawk.WinForms.Controls.MenuStripEx();
			this.fileToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.OpenMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.SaveMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.SaveAsMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.AppendFileMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.TruncateFromFileMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.RecentSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.toolStripSeparator2 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.OptionsSubMenuMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.modeToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.DetailedMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.FastMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.MemoryDomainsSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.toolStripSeparator6 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.sizeToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.ByteMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.WordMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.DWordMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.CheckMisalignedMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.toolStripSeparator8 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.BigEndianMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.DisplayTypeSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.toolStripSeparator1 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.DefinePreviousValueSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.Previous_LastSearchMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.PreviousFrameMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.Previous_OriginalMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.Previous_LastChangeMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.searchToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.newSearchToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.toolStripSeparator7 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.UndoMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.RedoMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.CopyValueToPrevMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.ClearChangeCountsMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.RemoveMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.toolStripSeparator5 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.GoToAddressMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.AddToRamWatchMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.PokeAddressMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.FreezeAddressMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.SelectAllMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.toolStripSeparator13 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.ClearUndoMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.SettingsMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.PreviewModeMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.AutoSearchMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.AutoSearchAccountForLagMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.toolStripSeparator9 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.ExcludeRamWatchMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.UseUndoHistoryMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.MemDomainLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.MessageLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.AutoSearchCheckBox = new System.Windows.Forms.CheckBox();
			this.CompareToBox = new System.Windows.Forms.GroupBox();
			this.DifferenceBox = new BizHawk.Client.EmuHawk.WatchValueBox();
			this.DifferenceRadio = new System.Windows.Forms.RadioButton();
			this.NumberOfChangesBox = new BizHawk.Client.EmuHawk.UnsignedIntegerBox();
			this.SpecificAddressBox = new BizHawk.Client.EmuHawk.HexTextBox();
			this.SpecificValueBox = new BizHawk.Client.EmuHawk.WatchValueBox();
			this.NumberOfChangesRadio = new System.Windows.Forms.RadioButton();
			this.SpecificAddressRadio = new System.Windows.Forms.RadioButton();
			this.SpecificValueRadio = new System.Windows.Forms.RadioButton();
			this.PreviousValueRadio = new System.Windows.Forms.RadioButton();
			this.toolStrip1 = new BizHawk.WinForms.Controls.ToolStripEx();
			this.DoSearchToolButton = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator10 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.NewSearchToolButton = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator15 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.CopyValueToPrevToolBarItem = new System.Windows.Forms.ToolStripButton();
			this.ClearChangeCountsToolBarItem = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator16 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.RemoveToolBarItem = new System.Windows.Forms.ToolStripButton();
			this.AddToRamWatchToolBarItem = new System.Windows.Forms.ToolStripButton();
			this.PokeAddressToolBarItem = new System.Windows.Forms.ToolStripButton();
			this.FreezeAddressToolBarItem = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator12 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.UndoToolBarButton = new System.Windows.Forms.ToolStripButton();
			this.RedoToolBarItem = new System.Windows.Forms.ToolStripButton();
			this.RebootToolBarSeparator = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.RebootToolbarButton = new System.Windows.Forms.ToolStripButton();
			this.ErrorIconButton = new System.Windows.Forms.ToolStripButton();
			this.ComparisonBox = new System.Windows.Forms.GroupBox();
			this.DifferentByBox = new BizHawk.Client.EmuHawk.WatchValueBox();
			this.DifferentByRadio = new System.Windows.Forms.RadioButton();
			this.NotEqualToRadio = new System.Windows.Forms.RadioButton();
			this.EqualToRadio = new System.Windows.Forms.RadioButton();
			this.GreaterThanOrEqualToRadio = new System.Windows.Forms.RadioButton();
			this.LessThanOrEqualToRadio = new System.Windows.Forms.RadioButton();
			this.GreaterThanRadio = new System.Windows.Forms.RadioButton();
			this.LessThanRadio = new System.Windows.Forms.RadioButton();
			this.SearchButton = new System.Windows.Forms.Button();
			this.SizeDropdown = new System.Windows.Forms.ComboBox();
			this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.DisplayTypeDropdown = new System.Windows.Forms.ComboBox();
			this.ListViewContextMenu.SuspendLayout();
			this.RamSearchMenu.SuspendLayout();
			this.CompareToBox.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			this.ComparisonBox.SuspendLayout();
			this.SuspendLayout();
			// 
			// SearchMenuItem
			// 
			this.SearchMenuItem.Text = "&Search";
			this.SearchMenuItem.Click += new System.EventHandler(this.SearchMenuItem_Click);
			// 
			// TotalSearchLabel
			// 
			this.TotalSearchLabel.Location = new System.Drawing.Point(12, 49);
			this.TotalSearchLabel.Name = "TotalSearchLabel";
			this.TotalSearchLabel.Text = "0 addresses";
			// 
			// WatchListView
			// 
			this.WatchListView.AllowColumnReorder = true;
			this.WatchListView.AllowColumnResize = true;
			this.WatchListView.AllowDrop = true;
			this.WatchListView.AlwaysScroll = false;
			this.WatchListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.WatchListView.CellHeightPadding = 0;
			this.WatchListView.CellWidthPadding = 0;
			this.WatchListView.ContextMenuStrip = this.ListViewContextMenu;
			this.WatchListView.FullRowSelect = true;
			this.WatchListView.HorizontalOrientation = false;
			this.WatchListView.LetKeysModifySelection = false;
			this.WatchListView.Location = new System.Drawing.Point(9, 65);
			this.WatchListView.Name = "WatchListView";
			this.WatchListView.RowCount = 0;
			this.WatchListView.ScrollSpeed = 0;
			this.WatchListView.Size = new System.Drawing.Size(230, 366);
			this.WatchListView.TabIndex = 1;
			this.WatchListView.ColumnClick += new BizHawk.Client.EmuHawk.InputRoll.ColumnClickEventHandler(this.WatchListView_ColumnClick);
			this.WatchListView.SelectedIndexChanged += new System.EventHandler(this.WatchListView_SelectedIndexChanged);
			this.WatchListView.DragDrop += new System.Windows.Forms.DragEventHandler(this.NewRamSearch_DragDrop);
			this.WatchListView.DragEnter += new System.Windows.Forms.DragEventHandler(this.DragEnterWrapper);
			this.WatchListView.Enter += new System.EventHandler(this.WatchListView_Enter);
			this.WatchListView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.WatchListView_KeyDown);
			this.WatchListView.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.WatchListView_MouseDoubleClick);
			// 
			// ListViewContextMenu
			// 
			this.ListViewContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.DoSearchContextMenuItem,
            this.NewSearchContextMenuItem,
            this.ContextMenuSeparator1,
            this.RemoveContextMenuItem,
            this.AddToRamWatchContextMenuItem,
            this.PokeContextMenuItem,
            this.FreezeContextMenuItem,
            this.UnfreezeAllContextMenuItem,
            this.ContextMenuSeparator2,
            this.ViewInHexEditorContextMenuItem,
            this.ContextMenuSeparator3,
            this.ClearPreviewContextMenuItem});
			this.ListViewContextMenu.Name = "contextMenuStrip1";
			this.ListViewContextMenu.Size = new System.Drawing.Size(222, 220);
			this.ListViewContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.ListViewContextMenu_Opening);
			// 
			// DoSearchContextMenuItem
			// 
			this.DoSearchContextMenuItem.Text = "&Search";
			this.DoSearchContextMenuItem.Click += new System.EventHandler(this.SearchMenuItem_Click);
			// 
			// NewSearchContextMenuItem
			// 
			this.NewSearchContextMenuItem.Text = "&Start New Search";
			this.NewSearchContextMenuItem.Click += new System.EventHandler(this.NewSearchMenuMenuItem_Click);
			// 
			// RemoveContextMenuItem
			// 
			this.RemoveContextMenuItem.ShortcutKeyDisplayString = "Del";
			this.RemoveContextMenuItem.Text = "Remove Selected";
			this.RemoveContextMenuItem.Click += new System.EventHandler(this.RemoveMenuItem_Click);
			// 
			// AddToRamWatchContextMenuItem
			// 
			this.AddToRamWatchContextMenuItem.ShortcutKeyDisplayString = "Ctrl+W";
			this.AddToRamWatchContextMenuItem.Text = "Add to RAM Watch";
			this.AddToRamWatchContextMenuItem.Click += new System.EventHandler(this.AddToRamWatchMenuItem_Click);
			// 
			// PokeContextMenuItem
			// 
			this.PokeContextMenuItem.ShortcutKeyDisplayString = "Ctrl+P";
			this.PokeContextMenuItem.Text = "Poke Address";
			this.PokeContextMenuItem.Click += new System.EventHandler(this.PokeAddressMenuItem_Click);
			// 
			// FreezeContextMenuItem
			// 
			this.FreezeContextMenuItem.ShortcutKeyDisplayString = "Ctrl+F";
			this.FreezeContextMenuItem.Text = "Freeze Address";
			this.FreezeContextMenuItem.Click += new System.EventHandler(this.FreezeAddressMenuItem_Click);
			// 
			// UnfreezeAllContextMenuItem
			// 
			this.UnfreezeAllContextMenuItem.Text = "Unfreeze &All";
			this.UnfreezeAllContextMenuItem.Click += new System.EventHandler(this.UnfreezeAllContextMenuItem_Click);
			// 
			// ViewInHexEditorContextMenuItem
			// 
			this.ViewInHexEditorContextMenuItem.Text = "View in Hex Editor";
			this.ViewInHexEditorContextMenuItem.Click += new System.EventHandler(this.ViewInHexEditorContextMenuItem_Click);
			// 
			// ClearPreviewContextMenuItem
			// 
			this.ClearPreviewContextMenuItem.Text = "&Clear Preview";
			this.ClearPreviewContextMenuItem.Click += new System.EventHandler(this.ClearPreviewContextMenuItem_Click);
			// 
			// RamSearchMenu
			// 
			this.RamSearchMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.OptionsSubMenuMenuItem,
            this.searchToolStripMenuItem,
            this.SettingsMenuItem});
			this.RamSearchMenu.TabIndex = 4;
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.OpenMenuItem,
            this.SaveMenuItem,
            this.SaveAsMenuItem,
            this.AppendFileMenuItem,
            this.TruncateFromFileMenuItem,
            this.RecentSubMenu});
			this.fileToolStripMenuItem.Text = "&File";
			this.fileToolStripMenuItem.DropDownOpened += new System.EventHandler(this.FileSubMenu_DropDownOpened);
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
			this.SaveAsMenuItem.Text = "Save As...";
			this.SaveAsMenuItem.Click += new System.EventHandler(this.SaveAsMenuItem_Click);
			// 
			// AppendFileMenuItem
			// 
			this.AppendFileMenuItem.Text = "&Append File...";
			this.AppendFileMenuItem.Click += new System.EventHandler(this.OpenMenuItem_Click);
			// 
			// TruncateFromFileMenuItem
			// 
			this.TruncateFromFileMenuItem.Text = "&Truncate from File...";
			this.TruncateFromFileMenuItem.Click += new System.EventHandler(this.OpenMenuItem_Click);
			// 
			// RecentSubMenu
			// 
			this.RecentSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator2});
			this.RecentSubMenu.Text = "Recent";
			this.RecentSubMenu.DropDownOpened += new System.EventHandler(this.RecentSubMenu_DropDownOpened);
			// 
			// OptionsSubMenuMenuItem
			// 
			this.OptionsSubMenuMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.modeToolStripMenuItem,
            this.MemoryDomainsSubMenu,
            this.sizeToolStripMenuItem,
            this.CheckMisalignedMenuItem,
            this.toolStripSeparator8,
            this.BigEndianMenuItem,
            this.DisplayTypeSubMenu,
            this.DefinePreviousValueSubMenu});
			this.OptionsSubMenuMenuItem.Text = "&Options";
			this.OptionsSubMenuMenuItem.DropDownOpened += new System.EventHandler(this.OptionsSubMenu_DropDownOpened);
			// 
			// modeToolStripMenuItem
			// 
			this.modeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.DetailedMenuItem,
            this.FastMenuItem});
			this.modeToolStripMenuItem.Text = "&Mode";
			this.modeToolStripMenuItem.DropDownOpened += new System.EventHandler(this.ModeSubMenu_DropDownOpened);
			// 
			// DetailedMenuItem
			// 
			this.DetailedMenuItem.Text = "&Detailed";
			this.DetailedMenuItem.Click += new System.EventHandler(this.DetailedMenuItem_Click);
			// 
			// FastMenuItem
			// 
			this.FastMenuItem.Text = "&Fast";
			this.FastMenuItem.Click += new System.EventHandler(this.FastMenuItem_Click);
			// 
			// MemoryDomainsSubMenu
			// 
			this.MemoryDomainsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator6});
			this.MemoryDomainsSubMenu.Text = "&Memory Domains";
			this.MemoryDomainsSubMenu.DropDownOpened += new System.EventHandler(this.MemoryDomainsSubMenu_DropDownOpened);
			// 
			// sizeToolStripMenuItem
			// 
			this.sizeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ByteMenuItem,
            this.WordMenuItem,
            this.DWordMenuItem});
			this.sizeToolStripMenuItem.Text = "&Size";
			this.sizeToolStripMenuItem.DropDownOpened += new System.EventHandler(this.SizeSubMenu_DropDownOpened);
			// 
			// ByteMenuItem
			// 
			this.ByteMenuItem.Text = "&1 Byte";
			this.ByteMenuItem.Click += new System.EventHandler(this.ByteMenuItem_Click);
			// 
			// WordMenuItem
			// 
			this.WordMenuItem.Text = "&2 Byte";
			this.WordMenuItem.Click += new System.EventHandler(this.WordMenuItem_Click);
			// 
			// DWordMenuItem
			// 
			this.DWordMenuItem.Text = "&4 Byte";
			this.DWordMenuItem.Click += new System.EventHandler(this.DWordMenuItem_Click_Click);
			// 
			// CheckMisalignedMenuItem
			// 
			this.CheckMisalignedMenuItem.Text = "Check Mis-aligned";
			this.CheckMisalignedMenuItem.Click += new System.EventHandler(this.CheckMisalignedMenuItem_Click);
			// 
			// BigEndianMenuItem
			// 
			this.BigEndianMenuItem.Text = "&Big Endian";
			this.BigEndianMenuItem.Click += new System.EventHandler(this.BigEndianMenuItem_Click);
			// 
			// DisplayTypeSubMenu
			// 
			this.DisplayTypeSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator1});
			this.DisplayTypeSubMenu.Text = "&Display Type";
			this.DisplayTypeSubMenu.DropDownOpened += new System.EventHandler(this.DisplayTypeSubMenu_DropDownOpened);
			// 
			// DefinePreviousValueSubMenu
			// 
			this.DefinePreviousValueSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Previous_LastSearchMenuItem,
            this.PreviousFrameMenuItem,
            this.Previous_OriginalMenuItem,
            this.Previous_LastChangeMenuItem});
			this.DefinePreviousValueSubMenu.Text = "Define Previous Value";
			this.DefinePreviousValueSubMenu.DropDownOpened += new System.EventHandler(this.DefinePreviousValueSubMenu_DropDownOpened);
			// 
			// Previous_LastSearchMenuItem
			// 
			this.Previous_LastSearchMenuItem.Text = "Last &Search";
			this.Previous_LastSearchMenuItem.Click += new System.EventHandler(this.Previous_LastSearchMenuItem_Click);
			// 
			// PreviousFrameMenuItem
			// 
			this.PreviousFrameMenuItem.Text = "&Previous Frame";
			this.PreviousFrameMenuItem.Click += new System.EventHandler(this.Previous_LastFrameMenuItem_Click);
			// 
			// Previous_OriginalMenuItem
			// 
			this.Previous_OriginalMenuItem.Text = "&Original";
			this.Previous_OriginalMenuItem.Click += new System.EventHandler(this.Previous_OriginalMenuItem_Click);
			// 
			// Previous_LastChangeMenuItem
			// 
			this.Previous_LastChangeMenuItem.Text = "Last &Change";
			this.Previous_LastChangeMenuItem.Click += new System.EventHandler(this.Previous_LastChangeMenuItem_Click);
			// 
			// searchToolStripMenuItem
			// 
			this.searchToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newSearchToolStripMenuItem,
            this.toolStripSeparator7,
            this.SearchMenuItem,
            this.UndoMenuItem,
            this.RedoMenuItem,
            this.CopyValueToPrevMenuItem,
            this.ClearChangeCountsMenuItem,
            this.RemoveMenuItem,
            this.toolStripSeparator5,
            this.GoToAddressMenuItem,
            this.AddToRamWatchMenuItem,
            this.PokeAddressMenuItem,
            this.FreezeAddressMenuItem,
            this.SelectAllMenuItem,
            this.toolStripSeparator13,
            this.ClearUndoMenuItem});
			this.searchToolStripMenuItem.Text = "&Search";
			this.searchToolStripMenuItem.DropDownOpened += new System.EventHandler(this.SearchSubMenu_DropDownOpened);
			// 
			// newSearchToolStripMenuItem
			// 
			this.newSearchToolStripMenuItem.Text = "&New Search";
			this.newSearchToolStripMenuItem.Click += new System.EventHandler(this.NewSearchMenuMenuItem_Click);
			// 
			// UndoMenuItem
			// 
			this.UndoMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Z)));
			this.UndoMenuItem.Text = "&Undo";
			this.UndoMenuItem.Click += new System.EventHandler(this.UndoMenuItem_Click);
			// 
			// RedoMenuItem
			// 
			this.RedoMenuItem.ShortcutKeyDisplayString = "";
			this.RedoMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Y)));
			this.RedoMenuItem.Text = "&Redo";
			this.RedoMenuItem.Click += new System.EventHandler(this.RedoMenuItem_Click);
			// 
			// CopyValueToPrevMenuItem
			// 
			this.CopyValueToPrevMenuItem.Text = "Copy Value to Prev";
			this.CopyValueToPrevMenuItem.Click += new System.EventHandler(this.CopyValueToPrevMenuItem_Click);
			// 
			// ClearChangeCountsMenuItem
			// 
			this.ClearChangeCountsMenuItem.Text = "&Clear Change Counts";
			this.ClearChangeCountsMenuItem.Click += new System.EventHandler(this.ClearChangeCountsMenuItem_Click);
			// 
			// RemoveMenuItem
			// 
			this.RemoveMenuItem.ShortcutKeyDisplayString = "Delete";
			this.RemoveMenuItem.Text = "&Remove selected";
			this.RemoveMenuItem.Click += new System.EventHandler(this.RemoveMenuItem_Click);
			// 
			// GoToAddressMenuItem
			// 
			this.GoToAddressMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.G)));
			this.GoToAddressMenuItem.Text = "&Go to Address...";
			this.GoToAddressMenuItem.Click += new System.EventHandler(this.GoToAddressMenuItem_Click);
			// 
			// AddToRamWatchMenuItem
			// 
			this.AddToRamWatchMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.W)));
			this.AddToRamWatchMenuItem.Text = "&Add to RAM Watch";
			this.AddToRamWatchMenuItem.Click += new System.EventHandler(this.AddToRamWatchMenuItem_Click);
			// 
			// PokeAddressMenuItem
			// 
			this.PokeAddressMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.P)));
			this.PokeAddressMenuItem.Text = "&Poke Address";
			this.PokeAddressMenuItem.Click += new System.EventHandler(this.PokeAddressMenuItem_Click);
			// 
			// FreezeAddressMenuItem
			// 
			this.FreezeAddressMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F)));
			this.FreezeAddressMenuItem.Text = "Freeze Address";
			this.FreezeAddressMenuItem.Click += new System.EventHandler(this.FreezeAddressMenuItem_Click);
			// 
			// SelectAllMenuItem
			// 
			this.SelectAllMenuItem.ShortcutKeyDisplayString = "Ctrl+A";
			this.SelectAllMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
			this.SelectAllMenuItem.Text = "Select All";
			this.SelectAllMenuItem.Click += new System.EventHandler(this.SelectAllMenuItem_Click);
			// 
			// ClearUndoMenuItem
			// 
			this.ClearUndoMenuItem.Text = "Clear Undo History";
			this.ClearUndoMenuItem.Click += new System.EventHandler(this.ClearUndoMenuItem_Click);
			// 
			// SettingsMenuItem
			// 
			this.SettingsMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.PreviewModeMenuItem,
            this.AutoSearchMenuItem,
            this.AutoSearchAccountForLagMenuItem,
            this.toolStripSeparator9,
            this.ExcludeRamWatchMenuItem,
            this.UseUndoHistoryMenuItem});
			this.SettingsMenuItem.Text = "&Settings";
			this.SettingsMenuItem.DropDownOpened += new System.EventHandler(this.SettingsSubMenu_DropDownOpened);
			// 
			// PreviewModeMenuItem
			// 
			this.PreviewModeMenuItem.Text = "&Preview Mode";
			this.PreviewModeMenuItem.Click += new System.EventHandler(this.PreviewModeMenuItem_Click);
			// 
			// AutoSearchMenuItem
			// 
			this.AutoSearchMenuItem.Text = "&Auto-Search";
			this.AutoSearchMenuItem.Click += new System.EventHandler(this.AutoSearchMenuItem_Click);
			// 
			// AutoSearchAccountForLagMenuItem
			// 
			this.AutoSearchAccountForLagMenuItem.Text = "&Auto-Search Account for Lag";
			this.AutoSearchAccountForLagMenuItem.Click += new System.EventHandler(this.AutoSearchAccountForLagMenuItem_Click);
			// 
			// ExcludeRamWatchMenuItem
			// 
			this.ExcludeRamWatchMenuItem.Text = "Always E&xclude RAM Watch List";
			this.ExcludeRamWatchMenuItem.Click += new System.EventHandler(this.ExcludeRamWatchMenuItem_Click);
			// 
			// UseUndoHistoryMenuItem
			// 
			this.UseUndoHistoryMenuItem.Text = "&Use Undo History";
			this.UseUndoHistoryMenuItem.Click += new System.EventHandler(this.UseUndoHistoryMenuItem_Click);
			// 
			// MemDomainLabel
			// 
			this.MemDomainLabel.Location = new System.Drawing.Point(135, 49);
			this.MemDomainLabel.Name = "MemDomainLabel";
			this.MemDomainLabel.Text = "Main Memory";
			// 
			// MessageLabel
			// 
			this.MessageLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.MessageLabel.Location = new System.Drawing.Point(9, 434);
			this.MessageLabel.Name = "MessageLabel";
			this.MessageLabel.Text = " todo                         ";
			// 
			// AutoSearchCheckBox
			// 
			this.AutoSearchCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.AutoSearchCheckBox.Appearance = System.Windows.Forms.Appearance.Button;
			this.AutoSearchCheckBox.AutoSize = true;
			this.AutoSearchCheckBox.Location = new System.Drawing.Point(348, 410);
			this.AutoSearchCheckBox.Name = "AutoSearchCheckBox";
			this.AutoSearchCheckBox.Size = new System.Drawing.Size(6, 6);
			this.AutoSearchCheckBox.TabIndex = 105;
			this.AutoSearchCheckBox.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.toolTip1.SetToolTip(this.AutoSearchCheckBox, "Automatically search each frame");
			this.AutoSearchCheckBox.UseVisualStyleBackColor = true;
			this.AutoSearchCheckBox.Click += new System.EventHandler(this.AutoSearchMenuItem_Click);
			// 
			// CompareToBox
			// 
			this.CompareToBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.CompareToBox.Controls.Add(this.DifferenceBox);
			this.CompareToBox.Controls.Add(this.DifferenceRadio);
			this.CompareToBox.Controls.Add(this.NumberOfChangesBox);
			this.CompareToBox.Controls.Add(this.SpecificAddressBox);
			this.CompareToBox.Controls.Add(this.SpecificValueBox);
			this.CompareToBox.Controls.Add(this.NumberOfChangesRadio);
			this.CompareToBox.Controls.Add(this.SpecificAddressRadio);
			this.CompareToBox.Controls.Add(this.SpecificValueRadio);
			this.CompareToBox.Controls.Add(this.PreviousValueRadio);
			this.CompareToBox.Location = new System.Drawing.Point(244, 65);
			this.CompareToBox.Name = "CompareToBox";
			this.CompareToBox.Size = new System.Drawing.Size(190, 125);
			this.CompareToBox.TabIndex = 10;
			this.CompareToBox.TabStop = false;
			this.CompareToBox.Text = "Compare To / By";
			// 
			// DifferenceBox
			// 
			this.DifferenceBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.DifferenceBox.ByteSize = BizHawk.Client.Common.WatchSize.Byte;
			this.DifferenceBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.DifferenceBox.Enabled = false;
			this.DifferenceBox.Location = new System.Drawing.Point(114, 98);
			this.DifferenceBox.MaxLength = 2;
			this.DifferenceBox.Name = "DifferenceBox";
			this.DifferenceBox.Nullable = false;
			this.DifferenceBox.Size = new System.Drawing.Size(72, 20);
			this.DifferenceBox.TabIndex = 45;
			this.DifferenceBox.Text = "00";
			this.DifferenceBox.Type = BizHawk.Client.Common.WatchDisplayType.Hex;
			this.DifferenceBox.TextChanged += new System.EventHandler(this.CompareToValue_TextChanged);
			// 
			// DifferenceRadio
			// 
			this.DifferenceRadio.AutoSize = true;
			this.DifferenceRadio.Location = new System.Drawing.Point(6, 100);
			this.DifferenceRadio.Name = "DifferenceRadio";
			this.DifferenceRadio.Size = new System.Drawing.Size(89, 17);
			this.DifferenceRadio.TabIndex = 40;
			this.DifferenceRadio.Text = "Difference of:";
			this.DifferenceRadio.UseVisualStyleBackColor = true;
			this.DifferenceRadio.Click += new System.EventHandler(this.DifferenceRadio_Click);
			// 
			// NumberOfChangesBox
			// 
			this.NumberOfChangesBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.NumberOfChangesBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.NumberOfChangesBox.Enabled = false;
			this.NumberOfChangesBox.Location = new System.Drawing.Point(114, 78);
			this.NumberOfChangesBox.MaxLength = 8;
			this.NumberOfChangesBox.Name = "NumberOfChangesBox";
			this.NumberOfChangesBox.Nullable = false;
			this.NumberOfChangesBox.Size = new System.Drawing.Size(72, 20);
			this.NumberOfChangesBox.TabIndex = 35;
			this.NumberOfChangesBox.TextChanged += new System.EventHandler(this.CompareToValue_TextChanged);
			// 
			// SpecificAddressBox
			// 
			this.SpecificAddressBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.SpecificAddressBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.SpecificAddressBox.Enabled = false;
			this.SpecificAddressBox.Location = new System.Drawing.Point(114, 58);
			this.SpecificAddressBox.MaxLength = 8;
			this.SpecificAddressBox.Name = "SpecificAddressBox";
			this.SpecificAddressBox.Nullable = false;
			this.SpecificAddressBox.Size = new System.Drawing.Size(72, 20);
			this.SpecificAddressBox.TabIndex = 25;
			this.SpecificAddressBox.TextChanged += new System.EventHandler(this.CompareToValue_TextChanged);
			// 
			// SpecificValueBox
			// 
			this.SpecificValueBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.SpecificValueBox.ByteSize = BizHawk.Client.Common.WatchSize.Byte;
			this.SpecificValueBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.SpecificValueBox.Enabled = false;
			this.SpecificValueBox.Location = new System.Drawing.Point(114, 38);
			this.SpecificValueBox.MaxLength = 2;
			this.SpecificValueBox.Name = "SpecificValueBox";
			this.SpecificValueBox.Nullable = false;
			this.SpecificValueBox.Size = new System.Drawing.Size(72, 20);
			this.SpecificValueBox.TabIndex = 15;
			this.SpecificValueBox.Text = "00";
			this.SpecificValueBox.Type = BizHawk.Client.Common.WatchDisplayType.Hex;
			this.SpecificValueBox.TextChanged += new System.EventHandler(this.CompareToValue_TextChanged);
			// 
			// NumberOfChangesRadio
			// 
			this.NumberOfChangesRadio.AutoSize = true;
			this.NumberOfChangesRadio.Location = new System.Drawing.Point(7, 80);
			this.NumberOfChangesRadio.Name = "NumberOfChangesRadio";
			this.NumberOfChangesRadio.Size = new System.Drawing.Size(111, 17);
			this.NumberOfChangesRadio.TabIndex = 30;
			this.NumberOfChangesRadio.Text = "Specific Changes:";
			this.NumberOfChangesRadio.UseVisualStyleBackColor = true;
			this.NumberOfChangesRadio.Click += new System.EventHandler(this.NumberOfChangesRadio_Click);
			// 
			// SpecificAddressRadio
			// 
			this.SpecificAddressRadio.AutoSize = true;
			this.SpecificAddressRadio.Location = new System.Drawing.Point(7, 60);
			this.SpecificAddressRadio.Name = "SpecificAddressRadio";
			this.SpecificAddressRadio.Size = new System.Drawing.Size(107, 17);
			this.SpecificAddressRadio.TabIndex = 20;
			this.SpecificAddressRadio.Text = "Specific Address:";
			this.SpecificAddressRadio.UseVisualStyleBackColor = true;
			this.SpecificAddressRadio.Click += new System.EventHandler(this.SpecificAddressRadio_Click);
			// 
			// SpecificValueRadio
			// 
			this.SpecificValueRadio.AutoSize = true;
			this.SpecificValueRadio.Location = new System.Drawing.Point(7, 40);
			this.SpecificValueRadio.Name = "SpecificValueRadio";
			this.SpecificValueRadio.Size = new System.Drawing.Size(96, 17);
			this.SpecificValueRadio.TabIndex = 10;
			this.SpecificValueRadio.Text = "Specific Value:";
			this.SpecificValueRadio.UseVisualStyleBackColor = true;
			this.SpecificValueRadio.Click += new System.EventHandler(this.SpecificValueRadio_Click);
			// 
			// PreviousValueRadio
			// 
			this.PreviousValueRadio.AutoSize = true;
			this.PreviousValueRadio.Checked = true;
			this.PreviousValueRadio.Location = new System.Drawing.Point(7, 20);
			this.PreviousValueRadio.Name = "PreviousValueRadio";
			this.PreviousValueRadio.Size = new System.Drawing.Size(96, 17);
			this.PreviousValueRadio.TabIndex = 5;
			this.PreviousValueRadio.TabStop = true;
			this.PreviousValueRadio.Text = "Previous Value";
			this.PreviousValueRadio.UseVisualStyleBackColor = true;
			this.PreviousValueRadio.Click += new System.EventHandler(this.PreviousValueRadio_Click);
			// 
			// toolStrip1
			// 
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.DoSearchToolButton,
            this.toolStripSeparator10,
            this.NewSearchToolButton,
            this.toolStripSeparator15,
            this.CopyValueToPrevToolBarItem,
            this.ClearChangeCountsToolBarItem,
            this.toolStripSeparator16,
            this.RemoveToolBarItem,
            this.AddToRamWatchToolBarItem,
            this.PokeAddressToolBarItem,
            this.FreezeAddressToolBarItem,
            this.toolStripSeparator12,
            this.UndoToolBarButton,
            this.RedoToolBarItem,
            this.RebootToolBarSeparator,
            this.RebootToolbarButton,
            this.ErrorIconButton});
			this.toolStrip1.Location = new System.Drawing.Point(0, 24);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.TabIndex = 11;
			// 
			// DoSearchToolButton
			// 
			this.DoSearchToolButton.Enabled = false;
			this.DoSearchToolButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.DoSearchToolButton.Name = "DoSearchToolButton";
			this.DoSearchToolButton.Size = new System.Drawing.Size(49, 22);
			this.DoSearchToolButton.Text = "Search ";
			this.DoSearchToolButton.Click += new System.EventHandler(this.SearchMenuItem_Click);
			// 
			// NewSearchToolButton
			// 
			this.NewSearchToolButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.NewSearchToolButton.Name = "NewSearchToolButton";
			this.NewSearchToolButton.Size = new System.Drawing.Size(35, 22);
			this.NewSearchToolButton.Text = "New";
			this.NewSearchToolButton.Click += new System.EventHandler(this.NewSearchMenuMenuItem_Click);
			// 
			// CopyValueToPrevToolBarItem
			// 
			this.CopyValueToPrevToolBarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.CopyValueToPrevToolBarItem.Enabled = false;
			this.CopyValueToPrevToolBarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.CopyValueToPrevToolBarItem.Name = "CopyValueToPrevToolBarItem";
			this.CopyValueToPrevToolBarItem.Size = new System.Drawing.Size(23, 22);
			this.CopyValueToPrevToolBarItem.Text = "Copy Value to Previous";
			this.CopyValueToPrevToolBarItem.Click += new System.EventHandler(this.CopyValueToPrevMenuItem_Click);
			// 
			// ClearChangeCountsToolBarItem
			// 
			this.ClearChangeCountsToolBarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.ClearChangeCountsToolBarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.ClearChangeCountsToolBarItem.Name = "ClearChangeCountsToolBarItem";
			this.ClearChangeCountsToolBarItem.Size = new System.Drawing.Size(23, 22);
			this.ClearChangeCountsToolBarItem.Text = "C";
			this.ClearChangeCountsToolBarItem.ToolTipText = "Clear Change Counts";
			this.ClearChangeCountsToolBarItem.Click += new System.EventHandler(this.ClearChangeCountsMenuItem_Click);
			// 
			// RemoveToolBarItem
			// 
			this.RemoveToolBarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.RemoveToolBarItem.Enabled = false;
			this.RemoveToolBarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.RemoveToolBarItem.Name = "RemoveToolBarItem";
			this.RemoveToolBarItem.Size = new System.Drawing.Size(23, 22);
			this.RemoveToolBarItem.Text = "C&ut";
			this.RemoveToolBarItem.ToolTipText = "Eliminate Selected Items";
			this.RemoveToolBarItem.Click += new System.EventHandler(this.RemoveMenuItem_Click);
			// 
			// AddToRamWatchToolBarItem
			// 
			this.AddToRamWatchToolBarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.AddToRamWatchToolBarItem.Enabled = false;
			this.AddToRamWatchToolBarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.AddToRamWatchToolBarItem.Name = "AddToRamWatchToolBarItem";
			this.AddToRamWatchToolBarItem.Size = new System.Drawing.Size(23, 22);
			this.AddToRamWatchToolBarItem.Text = "Watch";
			this.AddToRamWatchToolBarItem.Click += new System.EventHandler(this.AddToRamWatchMenuItem_Click);
			// 
			// PokeAddressToolBarItem
			// 
			this.PokeAddressToolBarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.PokeAddressToolBarItem.Enabled = false;
			this.PokeAddressToolBarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.PokeAddressToolBarItem.Name = "PokeAddressToolBarItem";
			this.PokeAddressToolBarItem.Size = new System.Drawing.Size(23, 22);
			this.PokeAddressToolBarItem.Text = "Poke";
			this.PokeAddressToolBarItem.Click += new System.EventHandler(this.PokeAddressMenuItem_Click);
			// 
			// FreezeAddressToolBarItem
			// 
			this.FreezeAddressToolBarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FreezeAddressToolBarItem.Enabled = false;
			this.FreezeAddressToolBarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FreezeAddressToolBarItem.Name = "FreezeAddressToolBarItem";
			this.FreezeAddressToolBarItem.Size = new System.Drawing.Size(23, 22);
			this.FreezeAddressToolBarItem.Text = "Freeze";
			this.FreezeAddressToolBarItem.Click += new System.EventHandler(this.FreezeAddressMenuItem_Click);
			// 
			// UndoToolBarButton
			// 
			this.UndoToolBarButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.UndoToolBarButton.Enabled = false;
			this.UndoToolBarButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.UndoToolBarButton.Name = "UndoToolBarButton";
			this.UndoToolBarButton.Size = new System.Drawing.Size(23, 22);
			this.UndoToolBarButton.Text = "Undo Search";
			this.UndoToolBarButton.Click += new System.EventHandler(this.UndoMenuItem_Click);
			// 
			// RedoToolBarItem
			// 
			this.RedoToolBarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.RedoToolBarItem.Enabled = false;
			this.RedoToolBarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.RedoToolBarItem.Name = "RedoToolBarItem";
			this.RedoToolBarItem.Size = new System.Drawing.Size(23, 22);
			this.RedoToolBarItem.Text = "Redo";
			this.RedoToolBarItem.Click += new System.EventHandler(this.RedoMenuItem_Click);
			// 
			// RebootToolbarButton
			// 
			this.RebootToolbarButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.RebootToolbarButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.RebootToolbarButton.Name = "RebootToolbarButton";
			this.RebootToolbarButton.Size = new System.Drawing.Size(23, 22);
			this.RebootToolbarButton.Text = "A new search needs to be started in order for these changes to take effect";
			this.RebootToolbarButton.Click += new System.EventHandler(this.NewSearchMenuMenuItem_Click);
			// 
			// ErrorIconButton
			// 
			this.ErrorIconButton.BackColor = System.Drawing.Color.NavajoWhite;
			this.ErrorIconButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.ErrorIconButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.ErrorIconButton.Name = "ErrorIconButton";
			this.ErrorIconButton.Size = new System.Drawing.Size(23, 22);
			this.ErrorIconButton.Text = "Warning! Out of Range Addresses in list, click to remove them";
			this.ErrorIconButton.Click += new System.EventHandler(this.ErrorIconButton_Click);
			// 
			// ComparisonBox
			// 
			this.ComparisonBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.ComparisonBox.Controls.Add(this.DifferentByBox);
			this.ComparisonBox.Controls.Add(this.DifferentByRadio);
			this.ComparisonBox.Controls.Add(this.NotEqualToRadio);
			this.ComparisonBox.Controls.Add(this.EqualToRadio);
			this.ComparisonBox.Controls.Add(this.GreaterThanOrEqualToRadio);
			this.ComparisonBox.Controls.Add(this.LessThanOrEqualToRadio);
			this.ComparisonBox.Controls.Add(this.GreaterThanRadio);
			this.ComparisonBox.Controls.Add(this.LessThanRadio);
			this.ComparisonBox.Location = new System.Drawing.Point(244, 196);
			this.ComparisonBox.Name = "ComparisonBox";
			this.ComparisonBox.Size = new System.Drawing.Size(190, 159);
			this.ComparisonBox.TabIndex = 12;
			this.ComparisonBox.TabStop = false;
			this.ComparisonBox.Text = "Comparison Operator";
			// 
			// DifferentByBox
			// 
			this.DifferentByBox.ByteSize = BizHawk.Client.Common.WatchSize.Byte;
			this.DifferentByBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.DifferentByBox.Enabled = false;
			this.DifferentByBox.Location = new System.Drawing.Point(88, 132);
			this.DifferentByBox.MaxLength = 2;
			this.DifferentByBox.Name = "DifferentByBox";
			this.DifferentByBox.Nullable = false;
			this.DifferentByBox.Size = new System.Drawing.Size(55, 20);
			this.DifferentByBox.TabIndex = 85;
			this.DifferentByBox.Text = "00";
			this.DifferentByBox.Type = BizHawk.Client.Common.WatchDisplayType.Hex;
			this.DifferentByBox.TextChanged += new System.EventHandler(this.DifferentByBox_TextChanged);
			// 
			// DifferentByRadio
			// 
			this.DifferentByRadio.AutoSize = true;
			this.DifferentByRadio.Location = new System.Drawing.Point(7, 134);
			this.DifferentByRadio.Name = "DifferentByRadio";
			this.DifferentByRadio.Size = new System.Drawing.Size(82, 17);
			this.DifferentByRadio.TabIndex = 80;
			this.DifferentByRadio.Text = "Different by:";
			this.DifferentByRadio.UseVisualStyleBackColor = true;
			this.DifferentByRadio.Click += new System.EventHandler(this.DifferentByRadio_Click);
			// 
			// NotEqualToRadio
			// 
			this.NotEqualToRadio.AutoSize = true;
			this.NotEqualToRadio.Location = new System.Drawing.Point(7, 35);
			this.NotEqualToRadio.Name = "NotEqualToRadio";
			this.NotEqualToRadio.Size = new System.Drawing.Size(88, 17);
			this.NotEqualToRadio.TabIndex = 55;
			this.NotEqualToRadio.Text = "Not Equal To";
			this.NotEqualToRadio.UseVisualStyleBackColor = true;
			this.NotEqualToRadio.Click += new System.EventHandler(this.NotEqualToRadio_Click);
			// 
			// EqualToRadio
			// 
			this.EqualToRadio.AutoSize = true;
			this.EqualToRadio.Checked = true;
			this.EqualToRadio.Location = new System.Drawing.Point(7, 15);
			this.EqualToRadio.Name = "EqualToRadio";
			this.EqualToRadio.Size = new System.Drawing.Size(68, 17);
			this.EqualToRadio.TabIndex = 50;
			this.EqualToRadio.TabStop = true;
			this.EqualToRadio.Text = "Equal To";
			this.EqualToRadio.UseVisualStyleBackColor = true;
			this.EqualToRadio.Click += new System.EventHandler(this.EqualToRadio_Click);
			// 
			// GreaterThanOrEqualToRadio
			// 
			this.GreaterThanOrEqualToRadio.AutoSize = true;
			this.GreaterThanOrEqualToRadio.Location = new System.Drawing.Point(7, 113);
			this.GreaterThanOrEqualToRadio.Name = "GreaterThanOrEqualToRadio";
			this.GreaterThanOrEqualToRadio.Size = new System.Drawing.Size(146, 17);
			this.GreaterThanOrEqualToRadio.TabIndex = 75;
			this.GreaterThanOrEqualToRadio.Text = "Greater Than or Equal To";
			this.GreaterThanOrEqualToRadio.UseVisualStyleBackColor = true;
			this.GreaterThanOrEqualToRadio.Click += new System.EventHandler(this.GreaterThanOrEqualToRadio_Click);
			// 
			// LessThanOrEqualToRadio
			// 
			this.LessThanOrEqualToRadio.AutoSize = true;
			this.LessThanOrEqualToRadio.Location = new System.Drawing.Point(7, 93);
			this.LessThanOrEqualToRadio.Name = "LessThanOrEqualToRadio";
			this.LessThanOrEqualToRadio.Size = new System.Drawing.Size(133, 17);
			this.LessThanOrEqualToRadio.TabIndex = 70;
			this.LessThanOrEqualToRadio.Text = "Less Than or Equal To";
			this.LessThanOrEqualToRadio.UseVisualStyleBackColor = true;
			this.LessThanOrEqualToRadio.Click += new System.EventHandler(this.LessThanOrEqualToRadio_Click);
			// 
			// GreaterThanRadio
			// 
			this.GreaterThanRadio.AutoSize = true;
			this.GreaterThanRadio.Location = new System.Drawing.Point(7, 74);
			this.GreaterThanRadio.Name = "GreaterThanRadio";
			this.GreaterThanRadio.Size = new System.Drawing.Size(88, 17);
			this.GreaterThanRadio.TabIndex = 65;
			this.GreaterThanRadio.Text = "Greater Than";
			this.GreaterThanRadio.UseVisualStyleBackColor = true;
			this.GreaterThanRadio.Click += new System.EventHandler(this.GreaterThanRadio_Click);
			// 
			// LessThanRadio
			// 
			this.LessThanRadio.AutoSize = true;
			this.LessThanRadio.Location = new System.Drawing.Point(7, 54);
			this.LessThanRadio.Name = "LessThanRadio";
			this.LessThanRadio.Size = new System.Drawing.Size(75, 17);
			this.LessThanRadio.TabIndex = 60;
			this.LessThanRadio.Text = "Less Than";
			this.LessThanRadio.UseVisualStyleBackColor = true;
			this.LessThanRadio.Click += new System.EventHandler(this.LessThanRadio_Click);
			// 
			// SearchButton
			// 
			this.SearchButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.SearchButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.SearchButton.Location = new System.Drawing.Point(244, 409);
			this.SearchButton.Name = "SearchButton";
			this.SearchButton.Size = new System.Drawing.Size(70, 23);
			this.SearchButton.TabIndex = 100;
			this.SearchButton.Text = "&Search";
			this.SearchButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.SearchButton.UseVisualStyleBackColor = true;
			this.SearchButton.Click += new System.EventHandler(this.SearchMenuItem_Click);
			// 
			// SizeDropdown
			// 
			this.SizeDropdown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.SizeDropdown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.SizeDropdown.FormattingEnabled = true;
			this.SizeDropdown.Items.AddRange(new object[] {
            "1 Byte",
            "2 Byte",
            "4 Byte"});
			this.SizeDropdown.Location = new System.Drawing.Point(244, 374);
			this.SizeDropdown.Name = "SizeDropdown";
			this.SizeDropdown.Size = new System.Drawing.Size(73, 21);
			this.SizeDropdown.TabIndex = 90;
			this.SizeDropdown.SelectedIndexChanged += new System.EventHandler(this.SizeDropdown_SelectedIndexChanged);
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label1.Location = new System.Drawing.Point(244, 358);
			this.label1.Name = "label1";
			this.label1.Text = "Size";
			// 
			// label2
			// 
			this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label2.Location = new System.Drawing.Point(327, 358);
			this.label2.Name = "label2";
			this.label2.Text = "Display";
			// 
			// DisplayTypeDropdown
			// 
			this.DisplayTypeDropdown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.DisplayTypeDropdown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.DisplayTypeDropdown.FormattingEnabled = true;
			this.DisplayTypeDropdown.Items.AddRange(new object[] {
            "1 Byte",
            "2 Byte",
            "4 Byte"});
			this.DisplayTypeDropdown.Location = new System.Drawing.Point(327, 374);
			this.DisplayTypeDropdown.Name = "DisplayTypeDropdown";
			this.DisplayTypeDropdown.Size = new System.Drawing.Size(107, 21);
			this.DisplayTypeDropdown.TabIndex = 95;
			this.DisplayTypeDropdown.SelectedIndexChanged += new System.EventHandler(this.DisplayTypeDropdown_SelectedIndexChanged);
			// 
			// RamSearch
			// 
			this.AllowDrop = true;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(445, 459);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.DisplayTypeDropdown);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.SizeDropdown);
			this.Controls.Add(this.SearchButton);
			this.Controls.Add(this.AutoSearchCheckBox);
			this.Controls.Add(this.ComparisonBox);
			this.Controls.Add(this.toolStrip1);
			this.Controls.Add(this.MessageLabel);
			this.Controls.Add(this.MemDomainLabel);
			this.Controls.Add(this.CompareToBox);
			this.Controls.Add(this.WatchListView);
			this.Controls.Add(this.TotalSearchLabel);
			this.Controls.Add(this.RamSearchMenu);
			this.MainMenuStrip = this.RamSearchMenu;
			this.MinimumSize = new System.Drawing.Size(290, 399);
			this.Name = "RamSearch";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Activated += new System.EventHandler(this.NewRamSearch_Activated);
			this.Load += new System.EventHandler(this.RamSearch_Load);
			this.DragDrop += new System.Windows.Forms.DragEventHandler(this.NewRamSearch_DragDrop);
			this.DragEnter += new System.Windows.Forms.DragEventHandler(this.DragEnterWrapper);
			this.ListViewContextMenu.ResumeLayout(false);
			this.RamSearchMenu.ResumeLayout(false);
			this.RamSearchMenu.PerformLayout();
			this.CompareToBox.ResumeLayout(false);
			this.CompareToBox.PerformLayout();
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.ComparisonBox.ResumeLayout(false);
			this.ComparisonBox.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private BizHawk.WinForms.Controls.LocLabelEx TotalSearchLabel;
		private InputRoll WatchListView;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx fileToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx OpenMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SaveAsMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SaveMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SettingsMenuItem;
		private BizHawk.WinForms.Controls.LocLabelEx MemDomainLabel;
		private BizHawk.WinForms.Controls.LocLabelEx MessageLabel;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RecentSubMenu;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator2;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx AppendFileMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx searchToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ClearChangeCountsMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx UndoMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RemoveMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator5;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx AddToRamWatchMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx PokeAddressMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx TruncateFromFileMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ExcludeRamWatchMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CopyValueToPrevMenuItem;
		private System.Windows.Forms.ContextMenuStrip ListViewContextMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx NewSearchContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx ContextMenuSeparator1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DoSearchContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FreezeAddressMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RemoveContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx AddToRamWatchContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx PokeContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FreezeContextMenuItem;
		private MenuStripEx RamSearchMenu;
		private System.Windows.Forms.ToolTip toolTip1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RedoMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ViewInHexEditorContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx UnfreezeAllContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx ContextMenuSeparator3;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator13;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ClearUndoMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx UseUndoHistoryMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx ContextMenuSeparator2;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ClearPreviewContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx newSearchToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator7;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx OptionsSubMenuMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx modeToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DetailedMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FastMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx MemoryDomainsSubMenu;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator6;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx sizeToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ByteMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx WordMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DWordMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DisplayTypeSubMenu;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx BigEndianMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CheckMisalignedMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator8;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DefinePreviousValueSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx PreviousFrameMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx Previous_LastSearchMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx Previous_OriginalMenuItem;
		private System.Windows.Forms.GroupBox CompareToBox;
		private System.Windows.Forms.RadioButton DifferenceRadio;
		private UnsignedIntegerBox NumberOfChangesBox;
		private HexTextBox SpecificAddressBox;
		private WatchValueBox SpecificValueBox;
		private System.Windows.Forms.RadioButton NumberOfChangesRadio;
		private System.Windows.Forms.RadioButton SpecificAddressRadio;
		private System.Windows.Forms.RadioButton SpecificValueRadio;
		private System.Windows.Forms.RadioButton PreviousValueRadio;
		private ToolStripEx toolStrip1;
		private System.Windows.Forms.ToolStripButton DoSearchToolButton;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator10;
		private System.Windows.Forms.ToolStripButton NewSearchToolButton;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator15;
		private System.Windows.Forms.GroupBox ComparisonBox;
		private WatchValueBox DifferentByBox;
		private System.Windows.Forms.RadioButton DifferentByRadio;
		private System.Windows.Forms.RadioButton NotEqualToRadio;
		private System.Windows.Forms.RadioButton EqualToRadio;
		private System.Windows.Forms.RadioButton GreaterThanOrEqualToRadio;
		private System.Windows.Forms.RadioButton LessThanOrEqualToRadio;
		private System.Windows.Forms.RadioButton GreaterThanRadio;
		private System.Windows.Forms.RadioButton LessThanRadio;
		private System.Windows.Forms.ToolStripButton CopyValueToPrevToolBarItem;
		private System.Windows.Forms.ToolStripButton ClearChangeCountsToolBarItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx PreviewModeMenuItem;
		private System.Windows.Forms.ToolStripButton RemoveToolBarItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator16;
		private System.Windows.Forms.ToolStripButton AddToRamWatchToolBarItem;
		private System.Windows.Forms.ToolStripButton PokeAddressToolBarItem;
		private System.Windows.Forms.ToolStripButton FreezeAddressToolBarItem;
		private WatchValueBox DifferenceBox;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx AutoSearchMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator9;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator12;
		private System.Windows.Forms.ToolStripButton UndoToolBarButton;
		private System.Windows.Forms.ToolStripButton RedoToolBarItem;
		private System.Windows.Forms.CheckBox AutoSearchCheckBox;
		private System.Windows.Forms.Button SearchButton;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx RebootToolBarSeparator;
		private System.Windows.Forms.ToolStripButton RebootToolbarButton;
		private System.Windows.Forms.ComboBox SizeDropdown;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private BizHawk.WinForms.Controls.LocLabelEx label2;
		private System.Windows.Forms.ComboBox DisplayTypeDropdown;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx GoToAddressMenuItem;
		private System.Windows.Forms.ToolStripButton ErrorIconButton;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx Previous_LastChangeMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx AutoSearchAccountForLagMenuItem;
		private ToolStripMenuItemEx SelectAllMenuItem;
		private ToolStripMenuItemEx SearchMenuItem;
	}
}