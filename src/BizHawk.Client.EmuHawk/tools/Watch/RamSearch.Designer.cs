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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RamSearch));
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
            resources.ApplyResources(this.SearchMenuItem, "SearchMenuItem");
            this.SearchMenuItem.Click += new System.EventHandler(this.SearchMenuItem_Click);
            // 
            // TotalSearchLabel
            // 
            resources.ApplyResources(this.TotalSearchLabel, "TotalSearchLabel");
            this.TotalSearchLabel.Name = "TotalSearchLabel";
            this.toolTip1.SetToolTip(this.TotalSearchLabel, resources.GetString("TotalSearchLabel.ToolTip"));
            // 
            // WatchListView
            // 
            resources.ApplyResources(this.WatchListView, "WatchListView");
            this.WatchListView.AllowColumnReorder = true;
            this.WatchListView.AllowColumnResize = true;
            this.WatchListView.AllowDrop = true;
            this.WatchListView.AlwaysScroll = false;
            this.WatchListView.CellHeightPadding = 0;
            this.WatchListView.CellWidthPadding = 0;
            this.WatchListView.ContextMenuStrip = this.ListViewContextMenu;
            this.WatchListView.FullRowSelect = true;
            this.WatchListView.HorizontalOrientation = false;
            this.WatchListView.LetKeysModifySelection = false;
            this.WatchListView.Name = "WatchListView";
            this.WatchListView.RowCount = 0;
            this.WatchListView.ScrollSpeed = 3;
            this.toolTip1.SetToolTip(this.WatchListView, resources.GetString("WatchListView.ToolTip"));
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
            resources.ApplyResources(this.ListViewContextMenu, "ListViewContextMenu");
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
            this.toolTip1.SetToolTip(this.ListViewContextMenu, resources.GetString("ListViewContextMenu.ToolTip"));
            this.ListViewContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.ListViewContextMenu_Opening);
            // 
            // DoSearchContextMenuItem
            // 
            resources.ApplyResources(this.DoSearchContextMenuItem, "DoSearchContextMenuItem");
            this.DoSearchContextMenuItem.Click += new System.EventHandler(this.SearchMenuItem_Click);
            // 
            // NewSearchContextMenuItem
            // 
            resources.ApplyResources(this.NewSearchContextMenuItem, "NewSearchContextMenuItem");
            this.NewSearchContextMenuItem.Click += new System.EventHandler(this.NewSearchMenuMenuItem_Click);
            // 
            // ContextMenuSeparator1
            // 
            resources.ApplyResources(this.ContextMenuSeparator1, "ContextMenuSeparator1");
            // 
            // RemoveContextMenuItem
            // 
            resources.ApplyResources(this.RemoveContextMenuItem, "RemoveContextMenuItem");
            this.RemoveContextMenuItem.Click += new System.EventHandler(this.RemoveMenuItem_Click);
            // 
            // AddToRamWatchContextMenuItem
            // 
            resources.ApplyResources(this.AddToRamWatchContextMenuItem, "AddToRamWatchContextMenuItem");
            this.AddToRamWatchContextMenuItem.Click += new System.EventHandler(this.AddToRamWatchMenuItem_Click);
            // 
            // PokeContextMenuItem
            // 
            resources.ApplyResources(this.PokeContextMenuItem, "PokeContextMenuItem");
            this.PokeContextMenuItem.Click += new System.EventHandler(this.PokeAddressMenuItem_Click);
            // 
            // FreezeContextMenuItem
            // 
            resources.ApplyResources(this.FreezeContextMenuItem, "FreezeContextMenuItem");
            this.FreezeContextMenuItem.Click += new System.EventHandler(this.FreezeAddressMenuItem_Click);
            // 
            // UnfreezeAllContextMenuItem
            // 
            resources.ApplyResources(this.UnfreezeAllContextMenuItem, "UnfreezeAllContextMenuItem");
            this.UnfreezeAllContextMenuItem.Click += new System.EventHandler(this.UnfreezeAllContextMenuItem_Click);
            // 
            // ContextMenuSeparator2
            // 
            resources.ApplyResources(this.ContextMenuSeparator2, "ContextMenuSeparator2");
            // 
            // ViewInHexEditorContextMenuItem
            // 
            resources.ApplyResources(this.ViewInHexEditorContextMenuItem, "ViewInHexEditorContextMenuItem");
            this.ViewInHexEditorContextMenuItem.Click += new System.EventHandler(this.ViewInHexEditorContextMenuItem_Click);
            // 
            // ContextMenuSeparator3
            // 
            resources.ApplyResources(this.ContextMenuSeparator3, "ContextMenuSeparator3");
            // 
            // ClearPreviewContextMenuItem
            // 
            resources.ApplyResources(this.ClearPreviewContextMenuItem, "ClearPreviewContextMenuItem");
            this.ClearPreviewContextMenuItem.Click += new System.EventHandler(this.ClearPreviewContextMenuItem_Click);
            // 
            // RamSearchMenu
            // 
            resources.ApplyResources(this.RamSearchMenu, "RamSearchMenu");
            this.RamSearchMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.OptionsSubMenuMenuItem,
            this.searchToolStripMenuItem,
            this.SettingsMenuItem});
            this.toolTip1.SetToolTip(this.RamSearchMenu, resources.GetString("RamSearchMenu.ToolTip"));
            // 
            // fileToolStripMenuItem
            // 
            resources.ApplyResources(this.fileToolStripMenuItem, "fileToolStripMenuItem");
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.OpenMenuItem,
            this.SaveMenuItem,
            this.SaveAsMenuItem,
            this.AppendFileMenuItem,
            this.TruncateFromFileMenuItem,
            this.RecentSubMenu});
            this.fileToolStripMenuItem.DropDownOpened += new System.EventHandler(this.FileSubMenu_DropDownOpened);
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
            // AppendFileMenuItem
            // 
            resources.ApplyResources(this.AppendFileMenuItem, "AppendFileMenuItem");
            this.AppendFileMenuItem.Click += new System.EventHandler(this.OpenMenuItem_Click);
            // 
            // TruncateFromFileMenuItem
            // 
            resources.ApplyResources(this.TruncateFromFileMenuItem, "TruncateFromFileMenuItem");
            this.TruncateFromFileMenuItem.Click += new System.EventHandler(this.OpenMenuItem_Click);
            // 
            // RecentSubMenu
            // 
            resources.ApplyResources(this.RecentSubMenu, "RecentSubMenu");
            this.RecentSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator2});
            this.RecentSubMenu.DropDownOpened += new System.EventHandler(this.RecentSubMenu_DropDownOpened);
            // 
            // toolStripSeparator2
            // 
            resources.ApplyResources(this.toolStripSeparator2, "toolStripSeparator2");
            // 
            // OptionsSubMenuMenuItem
            // 
            resources.ApplyResources(this.OptionsSubMenuMenuItem, "OptionsSubMenuMenuItem");
            this.OptionsSubMenuMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.modeToolStripMenuItem,
            this.MemoryDomainsSubMenu,
            this.sizeToolStripMenuItem,
            this.CheckMisalignedMenuItem,
            this.toolStripSeparator8,
            this.BigEndianMenuItem,
            this.DisplayTypeSubMenu,
            this.DefinePreviousValueSubMenu});
            this.OptionsSubMenuMenuItem.DropDownOpened += new System.EventHandler(this.OptionsSubMenu_DropDownOpened);
            // 
            // modeToolStripMenuItem
            // 
            resources.ApplyResources(this.modeToolStripMenuItem, "modeToolStripMenuItem");
            this.modeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.DetailedMenuItem,
            this.FastMenuItem});
            this.modeToolStripMenuItem.DropDownOpened += new System.EventHandler(this.ModeSubMenu_DropDownOpened);
            // 
            // DetailedMenuItem
            // 
            resources.ApplyResources(this.DetailedMenuItem, "DetailedMenuItem");
            this.DetailedMenuItem.Click += new System.EventHandler(this.DetailedMenuItem_Click);
            // 
            // FastMenuItem
            // 
            resources.ApplyResources(this.FastMenuItem, "FastMenuItem");
            this.FastMenuItem.Click += new System.EventHandler(this.FastMenuItem_Click);
            // 
            // MemoryDomainsSubMenu
            // 
            resources.ApplyResources(this.MemoryDomainsSubMenu, "MemoryDomainsSubMenu");
            this.MemoryDomainsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator6});
            this.MemoryDomainsSubMenu.DropDownOpened += new System.EventHandler(this.MemoryDomainsSubMenu_DropDownOpened);
            // 
            // toolStripSeparator6
            // 
            resources.ApplyResources(this.toolStripSeparator6, "toolStripSeparator6");
            // 
            // sizeToolStripMenuItem
            // 
            resources.ApplyResources(this.sizeToolStripMenuItem, "sizeToolStripMenuItem");
            this.sizeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ByteMenuItem,
            this.WordMenuItem,
            this.DWordMenuItem});
            this.sizeToolStripMenuItem.DropDownOpened += new System.EventHandler(this.SizeSubMenu_DropDownOpened);
            // 
            // ByteMenuItem
            // 
            resources.ApplyResources(this.ByteMenuItem, "ByteMenuItem");
            this.ByteMenuItem.Click += new System.EventHandler(this.ByteMenuItem_Click);
            // 
            // WordMenuItem
            // 
            resources.ApplyResources(this.WordMenuItem, "WordMenuItem");
            this.WordMenuItem.Click += new System.EventHandler(this.WordMenuItem_Click);
            // 
            // DWordMenuItem
            // 
            resources.ApplyResources(this.DWordMenuItem, "DWordMenuItem");
            this.DWordMenuItem.Click += new System.EventHandler(this.DWordMenuItem_Click_Click);
            // 
            // CheckMisalignedMenuItem
            // 
            resources.ApplyResources(this.CheckMisalignedMenuItem, "CheckMisalignedMenuItem");
            this.CheckMisalignedMenuItem.Click += new System.EventHandler(this.CheckMisalignedMenuItem_Click);
            // 
            // toolStripSeparator8
            // 
            resources.ApplyResources(this.toolStripSeparator8, "toolStripSeparator8");
            // 
            // BigEndianMenuItem
            // 
            resources.ApplyResources(this.BigEndianMenuItem, "BigEndianMenuItem");
            this.BigEndianMenuItem.Click += new System.EventHandler(this.BigEndianMenuItem_Click);
            // 
            // DisplayTypeSubMenu
            // 
            resources.ApplyResources(this.DisplayTypeSubMenu, "DisplayTypeSubMenu");
            this.DisplayTypeSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator1});
            this.DisplayTypeSubMenu.DropDownOpened += new System.EventHandler(this.DisplayTypeSubMenu_DropDownOpened);
            // 
            // toolStripSeparator1
            // 
            resources.ApplyResources(this.toolStripSeparator1, "toolStripSeparator1");
            // 
            // DefinePreviousValueSubMenu
            // 
            resources.ApplyResources(this.DefinePreviousValueSubMenu, "DefinePreviousValueSubMenu");
            this.DefinePreviousValueSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Previous_LastSearchMenuItem,
            this.PreviousFrameMenuItem,
            this.Previous_OriginalMenuItem,
            this.Previous_LastChangeMenuItem});
            this.DefinePreviousValueSubMenu.DropDownOpened += new System.EventHandler(this.DefinePreviousValueSubMenu_DropDownOpened);
            // 
            // Previous_LastSearchMenuItem
            // 
            resources.ApplyResources(this.Previous_LastSearchMenuItem, "Previous_LastSearchMenuItem");
            this.Previous_LastSearchMenuItem.Click += new System.EventHandler(this.Previous_LastSearchMenuItem_Click);
            // 
            // PreviousFrameMenuItem
            // 
            resources.ApplyResources(this.PreviousFrameMenuItem, "PreviousFrameMenuItem");
            this.PreviousFrameMenuItem.Click += new System.EventHandler(this.Previous_LastFrameMenuItem_Click);
            // 
            // Previous_OriginalMenuItem
            // 
            resources.ApplyResources(this.Previous_OriginalMenuItem, "Previous_OriginalMenuItem");
            this.Previous_OriginalMenuItem.Click += new System.EventHandler(this.Previous_OriginalMenuItem_Click);
            // 
            // Previous_LastChangeMenuItem
            // 
            resources.ApplyResources(this.Previous_LastChangeMenuItem, "Previous_LastChangeMenuItem");
            this.Previous_LastChangeMenuItem.Click += new System.EventHandler(this.Previous_LastChangeMenuItem_Click);
            // 
            // searchToolStripMenuItem
            // 
            resources.ApplyResources(this.searchToolStripMenuItem, "searchToolStripMenuItem");
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
            this.searchToolStripMenuItem.DropDownOpened += new System.EventHandler(this.SearchSubMenu_DropDownOpened);
            // 
            // newSearchToolStripMenuItem
            // 
            resources.ApplyResources(this.newSearchToolStripMenuItem, "newSearchToolStripMenuItem");
            this.newSearchToolStripMenuItem.Click += new System.EventHandler(this.NewSearchMenuMenuItem_Click);
            // 
            // toolStripSeparator7
            // 
            resources.ApplyResources(this.toolStripSeparator7, "toolStripSeparator7");
            // 
            // UndoMenuItem
            // 
            resources.ApplyResources(this.UndoMenuItem, "UndoMenuItem");
            this.UndoMenuItem.Click += new System.EventHandler(this.UndoMenuItem_Click);
            // 
            // RedoMenuItem
            // 
            resources.ApplyResources(this.RedoMenuItem, "RedoMenuItem");
            this.RedoMenuItem.Click += new System.EventHandler(this.RedoMenuItem_Click);
            // 
            // CopyValueToPrevMenuItem
            // 
            resources.ApplyResources(this.CopyValueToPrevMenuItem, "CopyValueToPrevMenuItem");
            this.CopyValueToPrevMenuItem.Click += new System.EventHandler(this.CopyValueToPrevMenuItem_Click);
            // 
            // ClearChangeCountsMenuItem
            // 
            resources.ApplyResources(this.ClearChangeCountsMenuItem, "ClearChangeCountsMenuItem");
            this.ClearChangeCountsMenuItem.Click += new System.EventHandler(this.ClearChangeCountsMenuItem_Click);
            // 
            // RemoveMenuItem
            // 
            resources.ApplyResources(this.RemoveMenuItem, "RemoveMenuItem");
            this.RemoveMenuItem.Click += new System.EventHandler(this.RemoveMenuItem_Click);
            // 
            // toolStripSeparator5
            // 
            resources.ApplyResources(this.toolStripSeparator5, "toolStripSeparator5");
            // 
            // GoToAddressMenuItem
            // 
            resources.ApplyResources(this.GoToAddressMenuItem, "GoToAddressMenuItem");
            this.GoToAddressMenuItem.Click += new System.EventHandler(this.GoToAddressMenuItem_Click);
            // 
            // AddToRamWatchMenuItem
            // 
            resources.ApplyResources(this.AddToRamWatchMenuItem, "AddToRamWatchMenuItem");
            this.AddToRamWatchMenuItem.Click += new System.EventHandler(this.AddToRamWatchMenuItem_Click);
            // 
            // PokeAddressMenuItem
            // 
            resources.ApplyResources(this.PokeAddressMenuItem, "PokeAddressMenuItem");
            this.PokeAddressMenuItem.Click += new System.EventHandler(this.PokeAddressMenuItem_Click);
            // 
            // FreezeAddressMenuItem
            // 
            resources.ApplyResources(this.FreezeAddressMenuItem, "FreezeAddressMenuItem");
            this.FreezeAddressMenuItem.Click += new System.EventHandler(this.FreezeAddressMenuItem_Click);
            // 
            // SelectAllMenuItem
            // 
            resources.ApplyResources(this.SelectAllMenuItem, "SelectAllMenuItem");
            this.SelectAllMenuItem.Click += new System.EventHandler(this.SelectAllMenuItem_Click);
            // 
            // toolStripSeparator13
            // 
            resources.ApplyResources(this.toolStripSeparator13, "toolStripSeparator13");
            // 
            // ClearUndoMenuItem
            // 
            resources.ApplyResources(this.ClearUndoMenuItem, "ClearUndoMenuItem");
            this.ClearUndoMenuItem.Click += new System.EventHandler(this.ClearUndoMenuItem_Click);
            // 
            // SettingsMenuItem
            // 
            resources.ApplyResources(this.SettingsMenuItem, "SettingsMenuItem");
            this.SettingsMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.PreviewModeMenuItem,
            this.AutoSearchMenuItem,
            this.AutoSearchAccountForLagMenuItem,
            this.toolStripSeparator9,
            this.ExcludeRamWatchMenuItem,
            this.UseUndoHistoryMenuItem});
            this.SettingsMenuItem.DropDownOpened += new System.EventHandler(this.SettingsSubMenu_DropDownOpened);
            // 
            // PreviewModeMenuItem
            // 
            resources.ApplyResources(this.PreviewModeMenuItem, "PreviewModeMenuItem");
            this.PreviewModeMenuItem.Click += new System.EventHandler(this.PreviewModeMenuItem_Click);
            // 
            // AutoSearchMenuItem
            // 
            resources.ApplyResources(this.AutoSearchMenuItem, "AutoSearchMenuItem");
            this.AutoSearchMenuItem.Click += new System.EventHandler(this.AutoSearchMenuItem_Click);
            // 
            // AutoSearchAccountForLagMenuItem
            // 
            resources.ApplyResources(this.AutoSearchAccountForLagMenuItem, "AutoSearchAccountForLagMenuItem");
            this.AutoSearchAccountForLagMenuItem.Click += new System.EventHandler(this.AutoSearchAccountForLagMenuItem_Click);
            // 
            // toolStripSeparator9
            // 
            resources.ApplyResources(this.toolStripSeparator9, "toolStripSeparator9");
            // 
            // ExcludeRamWatchMenuItem
            // 
            resources.ApplyResources(this.ExcludeRamWatchMenuItem, "ExcludeRamWatchMenuItem");
            this.ExcludeRamWatchMenuItem.Click += new System.EventHandler(this.ExcludeRamWatchMenuItem_Click);
            // 
            // UseUndoHistoryMenuItem
            // 
            resources.ApplyResources(this.UseUndoHistoryMenuItem, "UseUndoHistoryMenuItem");
            this.UseUndoHistoryMenuItem.Click += new System.EventHandler(this.UseUndoHistoryMenuItem_Click);
            // 
            // MemDomainLabel
            // 
            resources.ApplyResources(this.MemDomainLabel, "MemDomainLabel");
            this.MemDomainLabel.Name = "MemDomainLabel";
            this.toolTip1.SetToolTip(this.MemDomainLabel, resources.GetString("MemDomainLabel.ToolTip"));
            // 
            // MessageLabel
            // 
            resources.ApplyResources(this.MessageLabel, "MessageLabel");
            this.MessageLabel.Name = "MessageLabel";
            this.toolTip1.SetToolTip(this.MessageLabel, resources.GetString("MessageLabel.ToolTip"));
            // 
            // AutoSearchCheckBox
            // 
            resources.ApplyResources(this.AutoSearchCheckBox, "AutoSearchCheckBox");
            this.AutoSearchCheckBox.Name = "AutoSearchCheckBox";
            this.toolTip1.SetToolTip(this.AutoSearchCheckBox, resources.GetString("AutoSearchCheckBox.ToolTip"));
            this.AutoSearchCheckBox.UseVisualStyleBackColor = true;
            this.AutoSearchCheckBox.Click += new System.EventHandler(this.AutoSearchMenuItem_Click);
            // 
            // CompareToBox
            // 
            resources.ApplyResources(this.CompareToBox, "CompareToBox");
            this.CompareToBox.Controls.Add(this.DifferenceBox);
            this.CompareToBox.Controls.Add(this.DifferenceRadio);
            this.CompareToBox.Controls.Add(this.NumberOfChangesBox);
            this.CompareToBox.Controls.Add(this.SpecificAddressBox);
            this.CompareToBox.Controls.Add(this.SpecificValueBox);
            this.CompareToBox.Controls.Add(this.NumberOfChangesRadio);
            this.CompareToBox.Controls.Add(this.SpecificAddressRadio);
            this.CompareToBox.Controls.Add(this.SpecificValueRadio);
            this.CompareToBox.Controls.Add(this.PreviousValueRadio);
            this.CompareToBox.Name = "CompareToBox";
            this.CompareToBox.TabStop = false;
            this.toolTip1.SetToolTip(this.CompareToBox, resources.GetString("CompareToBox.ToolTip"));
            // 
            // DifferenceBox
            // 
            resources.ApplyResources(this.DifferenceBox, "DifferenceBox");
            this.DifferenceBox.ByteSize = BizHawk.Client.Common.WatchSize.Byte;
            this.DifferenceBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.DifferenceBox.Name = "DifferenceBox";
            this.DifferenceBox.Nullable = false;
            this.toolTip1.SetToolTip(this.DifferenceBox, resources.GetString("DifferenceBox.ToolTip"));
            this.DifferenceBox.Type = BizHawk.Client.Common.WatchDisplayType.Hex;
            this.DifferenceBox.TextChanged += new System.EventHandler(this.CompareToValue_TextChanged);
            // 
            // DifferenceRadio
            // 
            resources.ApplyResources(this.DifferenceRadio, "DifferenceRadio");
            this.DifferenceRadio.Name = "DifferenceRadio";
            this.toolTip1.SetToolTip(this.DifferenceRadio, resources.GetString("DifferenceRadio.ToolTip"));
            this.DifferenceRadio.UseVisualStyleBackColor = true;
            this.DifferenceRadio.Click += new System.EventHandler(this.DifferenceRadio_Click);
            // 
            // NumberOfChangesBox
            // 
            resources.ApplyResources(this.NumberOfChangesBox, "NumberOfChangesBox");
            this.NumberOfChangesBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.NumberOfChangesBox.Name = "NumberOfChangesBox";
            this.NumberOfChangesBox.Nullable = false;
            this.toolTip1.SetToolTip(this.NumberOfChangesBox, resources.GetString("NumberOfChangesBox.ToolTip"));
            this.NumberOfChangesBox.TextChanged += new System.EventHandler(this.CompareToValue_TextChanged);
            // 
            // SpecificAddressBox
            // 
            resources.ApplyResources(this.SpecificAddressBox, "SpecificAddressBox");
            this.SpecificAddressBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.SpecificAddressBox.Name = "SpecificAddressBox";
            this.SpecificAddressBox.Nullable = false;
            this.toolTip1.SetToolTip(this.SpecificAddressBox, resources.GetString("SpecificAddressBox.ToolTip"));
            this.SpecificAddressBox.TextChanged += new System.EventHandler(this.CompareToValue_TextChanged);
            // 
            // SpecificValueBox
            // 
            resources.ApplyResources(this.SpecificValueBox, "SpecificValueBox");
            this.SpecificValueBox.ByteSize = BizHawk.Client.Common.WatchSize.Byte;
            this.SpecificValueBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.SpecificValueBox.Name = "SpecificValueBox";
            this.SpecificValueBox.Nullable = false;
            this.toolTip1.SetToolTip(this.SpecificValueBox, resources.GetString("SpecificValueBox.ToolTip"));
            this.SpecificValueBox.Type = BizHawk.Client.Common.WatchDisplayType.Hex;
            this.SpecificValueBox.TextChanged += new System.EventHandler(this.CompareToValue_TextChanged);
            // 
            // NumberOfChangesRadio
            // 
            resources.ApplyResources(this.NumberOfChangesRadio, "NumberOfChangesRadio");
            this.NumberOfChangesRadio.Name = "NumberOfChangesRadio";
            this.toolTip1.SetToolTip(this.NumberOfChangesRadio, resources.GetString("NumberOfChangesRadio.ToolTip"));
            this.NumberOfChangesRadio.UseVisualStyleBackColor = true;
            this.NumberOfChangesRadio.Click += new System.EventHandler(this.NumberOfChangesRadio_Click);
            // 
            // SpecificAddressRadio
            // 
            resources.ApplyResources(this.SpecificAddressRadio, "SpecificAddressRadio");
            this.SpecificAddressRadio.Name = "SpecificAddressRadio";
            this.toolTip1.SetToolTip(this.SpecificAddressRadio, resources.GetString("SpecificAddressRadio.ToolTip"));
            this.SpecificAddressRadio.UseVisualStyleBackColor = true;
            this.SpecificAddressRadio.Click += new System.EventHandler(this.SpecificAddressRadio_Click);
            // 
            // SpecificValueRadio
            // 
            resources.ApplyResources(this.SpecificValueRadio, "SpecificValueRadio");
            this.SpecificValueRadio.Name = "SpecificValueRadio";
            this.toolTip1.SetToolTip(this.SpecificValueRadio, resources.GetString("SpecificValueRadio.ToolTip"));
            this.SpecificValueRadio.UseVisualStyleBackColor = true;
            this.SpecificValueRadio.Click += new System.EventHandler(this.SpecificValueRadio_Click);
            // 
            // PreviousValueRadio
            // 
            resources.ApplyResources(this.PreviousValueRadio, "PreviousValueRadio");
            this.PreviousValueRadio.Checked = true;
            this.PreviousValueRadio.Name = "PreviousValueRadio";
            this.PreviousValueRadio.TabStop = true;
            this.toolTip1.SetToolTip(this.PreviousValueRadio, resources.GetString("PreviousValueRadio.ToolTip"));
            this.PreviousValueRadio.UseVisualStyleBackColor = true;
            this.PreviousValueRadio.Click += new System.EventHandler(this.PreviousValueRadio_Click);
            // 
            // toolStrip1
            // 
            resources.ApplyResources(this.toolStrip1, "toolStrip1");
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
            this.toolStrip1.Name = "toolStrip1";
            this.toolTip1.SetToolTip(this.toolStrip1, resources.GetString("toolStrip1.ToolTip"));
            // 
            // DoSearchToolButton
            // 
            resources.ApplyResources(this.DoSearchToolButton, "DoSearchToolButton");
            this.DoSearchToolButton.Name = "DoSearchToolButton";
            this.DoSearchToolButton.Click += new System.EventHandler(this.SearchMenuItem_Click);
            // 
            // toolStripSeparator10
            // 
            resources.ApplyResources(this.toolStripSeparator10, "toolStripSeparator10");
            // 
            // NewSearchToolButton
            // 
            resources.ApplyResources(this.NewSearchToolButton, "NewSearchToolButton");
            this.NewSearchToolButton.Name = "NewSearchToolButton";
            this.NewSearchToolButton.Click += new System.EventHandler(this.NewSearchMenuMenuItem_Click);
            // 
            // toolStripSeparator15
            // 
            resources.ApplyResources(this.toolStripSeparator15, "toolStripSeparator15");
            // 
            // CopyValueToPrevToolBarItem
            // 
            resources.ApplyResources(this.CopyValueToPrevToolBarItem, "CopyValueToPrevToolBarItem");
            this.CopyValueToPrevToolBarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.CopyValueToPrevToolBarItem.Name = "CopyValueToPrevToolBarItem";
            this.CopyValueToPrevToolBarItem.Click += new System.EventHandler(this.CopyValueToPrevMenuItem_Click);
            // 
            // ClearChangeCountsToolBarItem
            // 
            resources.ApplyResources(this.ClearChangeCountsToolBarItem, "ClearChangeCountsToolBarItem");
            this.ClearChangeCountsToolBarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.ClearChangeCountsToolBarItem.Name = "ClearChangeCountsToolBarItem";
            this.ClearChangeCountsToolBarItem.Click += new System.EventHandler(this.ClearChangeCountsMenuItem_Click);
            // 
            // toolStripSeparator16
            // 
            resources.ApplyResources(this.toolStripSeparator16, "toolStripSeparator16");
            // 
            // RemoveToolBarItem
            // 
            resources.ApplyResources(this.RemoveToolBarItem, "RemoveToolBarItem");
            this.RemoveToolBarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.RemoveToolBarItem.Name = "RemoveToolBarItem";
            this.RemoveToolBarItem.Click += new System.EventHandler(this.RemoveMenuItem_Click);
            // 
            // AddToRamWatchToolBarItem
            // 
            resources.ApplyResources(this.AddToRamWatchToolBarItem, "AddToRamWatchToolBarItem");
            this.AddToRamWatchToolBarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.AddToRamWatchToolBarItem.Name = "AddToRamWatchToolBarItem";
            this.AddToRamWatchToolBarItem.Click += new System.EventHandler(this.AddToRamWatchMenuItem_Click);
            // 
            // PokeAddressToolBarItem
            // 
            resources.ApplyResources(this.PokeAddressToolBarItem, "PokeAddressToolBarItem");
            this.PokeAddressToolBarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.PokeAddressToolBarItem.Name = "PokeAddressToolBarItem";
            this.PokeAddressToolBarItem.Click += new System.EventHandler(this.PokeAddressMenuItem_Click);
            // 
            // FreezeAddressToolBarItem
            // 
            resources.ApplyResources(this.FreezeAddressToolBarItem, "FreezeAddressToolBarItem");
            this.FreezeAddressToolBarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.FreezeAddressToolBarItem.Name = "FreezeAddressToolBarItem";
            this.FreezeAddressToolBarItem.Click += new System.EventHandler(this.FreezeAddressMenuItem_Click);
            // 
            // toolStripSeparator12
            // 
            resources.ApplyResources(this.toolStripSeparator12, "toolStripSeparator12");
            // 
            // UndoToolBarButton
            // 
            resources.ApplyResources(this.UndoToolBarButton, "UndoToolBarButton");
            this.UndoToolBarButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.UndoToolBarButton.Name = "UndoToolBarButton";
            this.UndoToolBarButton.Click += new System.EventHandler(this.UndoMenuItem_Click);
            // 
            // RedoToolBarItem
            // 
            resources.ApplyResources(this.RedoToolBarItem, "RedoToolBarItem");
            this.RedoToolBarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.RedoToolBarItem.Name = "RedoToolBarItem";
            this.RedoToolBarItem.Click += new System.EventHandler(this.RedoMenuItem_Click);
            // 
            // RebootToolBarSeparator
            // 
            resources.ApplyResources(this.RebootToolBarSeparator, "RebootToolBarSeparator");
            // 
            // RebootToolbarButton
            // 
            resources.ApplyResources(this.RebootToolbarButton, "RebootToolbarButton");
            this.RebootToolbarButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.RebootToolbarButton.Name = "RebootToolbarButton";
            this.RebootToolbarButton.Click += new System.EventHandler(this.NewSearchMenuMenuItem_Click);
            // 
            // ErrorIconButton
            // 
            resources.ApplyResources(this.ErrorIconButton, "ErrorIconButton");
            this.ErrorIconButton.BackColor = System.Drawing.Color.NavajoWhite;
            this.ErrorIconButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.ErrorIconButton.Name = "ErrorIconButton";
            this.ErrorIconButton.Click += new System.EventHandler(this.ErrorIconButton_Click);
            // 
            // ComparisonBox
            // 
            resources.ApplyResources(this.ComparisonBox, "ComparisonBox");
            this.ComparisonBox.Controls.Add(this.DifferentByBox);
            this.ComparisonBox.Controls.Add(this.DifferentByRadio);
            this.ComparisonBox.Controls.Add(this.NotEqualToRadio);
            this.ComparisonBox.Controls.Add(this.EqualToRadio);
            this.ComparisonBox.Controls.Add(this.GreaterThanOrEqualToRadio);
            this.ComparisonBox.Controls.Add(this.LessThanOrEqualToRadio);
            this.ComparisonBox.Controls.Add(this.GreaterThanRadio);
            this.ComparisonBox.Controls.Add(this.LessThanRadio);
            this.ComparisonBox.Name = "ComparisonBox";
            this.ComparisonBox.TabStop = false;
            this.toolTip1.SetToolTip(this.ComparisonBox, resources.GetString("ComparisonBox.ToolTip"));
            // 
            // DifferentByBox
            // 
            resources.ApplyResources(this.DifferentByBox, "DifferentByBox");
            this.DifferentByBox.ByteSize = BizHawk.Client.Common.WatchSize.Byte;
            this.DifferentByBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.DifferentByBox.Name = "DifferentByBox";
            this.DifferentByBox.Nullable = false;
            this.toolTip1.SetToolTip(this.DifferentByBox, resources.GetString("DifferentByBox.ToolTip"));
            this.DifferentByBox.Type = BizHawk.Client.Common.WatchDisplayType.Hex;
            this.DifferentByBox.TextChanged += new System.EventHandler(this.DifferentByBox_TextChanged);
            // 
            // DifferentByRadio
            // 
            resources.ApplyResources(this.DifferentByRadio, "DifferentByRadio");
            this.DifferentByRadio.Name = "DifferentByRadio";
            this.toolTip1.SetToolTip(this.DifferentByRadio, resources.GetString("DifferentByRadio.ToolTip"));
            this.DifferentByRadio.UseVisualStyleBackColor = true;
            this.DifferentByRadio.Click += new System.EventHandler(this.DifferentByRadio_Click);
            // 
            // NotEqualToRadio
            // 
            resources.ApplyResources(this.NotEqualToRadio, "NotEqualToRadio");
            this.NotEqualToRadio.Name = "NotEqualToRadio";
            this.toolTip1.SetToolTip(this.NotEqualToRadio, resources.GetString("NotEqualToRadio.ToolTip"));
            this.NotEqualToRadio.UseVisualStyleBackColor = true;
            this.NotEqualToRadio.Click += new System.EventHandler(this.NotEqualToRadio_Click);
            // 
            // EqualToRadio
            // 
            resources.ApplyResources(this.EqualToRadio, "EqualToRadio");
            this.EqualToRadio.Checked = true;
            this.EqualToRadio.Name = "EqualToRadio";
            this.EqualToRadio.TabStop = true;
            this.toolTip1.SetToolTip(this.EqualToRadio, resources.GetString("EqualToRadio.ToolTip"));
            this.EqualToRadio.UseVisualStyleBackColor = true;
            this.EqualToRadio.Click += new System.EventHandler(this.EqualToRadio_Click);
            // 
            // GreaterThanOrEqualToRadio
            // 
            resources.ApplyResources(this.GreaterThanOrEqualToRadio, "GreaterThanOrEqualToRadio");
            this.GreaterThanOrEqualToRadio.Name = "GreaterThanOrEqualToRadio";
            this.toolTip1.SetToolTip(this.GreaterThanOrEqualToRadio, resources.GetString("GreaterThanOrEqualToRadio.ToolTip"));
            this.GreaterThanOrEqualToRadio.UseVisualStyleBackColor = true;
            this.GreaterThanOrEqualToRadio.Click += new System.EventHandler(this.GreaterThanOrEqualToRadio_Click);
            // 
            // LessThanOrEqualToRadio
            // 
            resources.ApplyResources(this.LessThanOrEqualToRadio, "LessThanOrEqualToRadio");
            this.LessThanOrEqualToRadio.Name = "LessThanOrEqualToRadio";
            this.toolTip1.SetToolTip(this.LessThanOrEqualToRadio, resources.GetString("LessThanOrEqualToRadio.ToolTip"));
            this.LessThanOrEqualToRadio.UseVisualStyleBackColor = true;
            this.LessThanOrEqualToRadio.Click += new System.EventHandler(this.LessThanOrEqualToRadio_Click);
            // 
            // GreaterThanRadio
            // 
            resources.ApplyResources(this.GreaterThanRadio, "GreaterThanRadio");
            this.GreaterThanRadio.Name = "GreaterThanRadio";
            this.toolTip1.SetToolTip(this.GreaterThanRadio, resources.GetString("GreaterThanRadio.ToolTip"));
            this.GreaterThanRadio.UseVisualStyleBackColor = true;
            this.GreaterThanRadio.Click += new System.EventHandler(this.GreaterThanRadio_Click);
            // 
            // LessThanRadio
            // 
            resources.ApplyResources(this.LessThanRadio, "LessThanRadio");
            this.LessThanRadio.Name = "LessThanRadio";
            this.toolTip1.SetToolTip(this.LessThanRadio, resources.GetString("LessThanRadio.ToolTip"));
            this.LessThanRadio.UseVisualStyleBackColor = true;
            this.LessThanRadio.Click += new System.EventHandler(this.LessThanRadio_Click);
            // 
            // SearchButton
            // 
            resources.ApplyResources(this.SearchButton, "SearchButton");
            this.SearchButton.Name = "SearchButton";
            this.toolTip1.SetToolTip(this.SearchButton, resources.GetString("SearchButton.ToolTip"));
            this.SearchButton.UseVisualStyleBackColor = true;
            this.SearchButton.Click += new System.EventHandler(this.SearchMenuItem_Click);
            // 
            // SizeDropdown
            // 
            resources.ApplyResources(this.SizeDropdown, "SizeDropdown");
            this.SizeDropdown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.SizeDropdown.FormattingEnabled = true;
            this.SizeDropdown.Items.AddRange(new object[] {
            resources.GetString("SizeDropdown.Items"),
            resources.GetString("SizeDropdown.Items1"),
            resources.GetString("SizeDropdown.Items2")});
            this.SizeDropdown.Name = "SizeDropdown";
            this.toolTip1.SetToolTip(this.SizeDropdown, resources.GetString("SizeDropdown.ToolTip"));
            this.SizeDropdown.SelectedIndexChanged += new System.EventHandler(this.SizeDropdown_SelectedIndexChanged);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            this.toolTip1.SetToolTip(this.label1, resources.GetString("label1.ToolTip"));
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            this.toolTip1.SetToolTip(this.label2, resources.GetString("label2.ToolTip"));
            // 
            // DisplayTypeDropdown
            // 
            resources.ApplyResources(this.DisplayTypeDropdown, "DisplayTypeDropdown");
            this.DisplayTypeDropdown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.DisplayTypeDropdown.FormattingEnabled = true;
            this.DisplayTypeDropdown.Items.AddRange(new object[] {
            resources.GetString("DisplayTypeDropdown.Items"),
            resources.GetString("DisplayTypeDropdown.Items1"),
            resources.GetString("DisplayTypeDropdown.Items2")});
            this.DisplayTypeDropdown.Name = "DisplayTypeDropdown";
            this.toolTip1.SetToolTip(this.DisplayTypeDropdown, resources.GetString("DisplayTypeDropdown.ToolTip"));
            this.DisplayTypeDropdown.SelectedIndexChanged += new System.EventHandler(this.DisplayTypeDropdown_SelectedIndexChanged);
            // 
            // RamSearch
            // 
            resources.ApplyResources(this, "$this");
            this.AllowDrop = true;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
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
            this.Name = "RamSearch";
            this.toolTip1.SetToolTip(this, resources.GetString("$this.ToolTip"));
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