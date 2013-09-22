namespace BizHawk.MultiClient
{
    partial class NewRamSearch
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
			System.Windows.Forms.ToolStripMenuItem SearchMenuItem;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewRamSearch));
			this.TotalSearchLabel = new System.Windows.Forms.Label();
			this.WatchListView = new BizHawk.VirtualListView();
			this.AddressColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.ValueColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.PreviousColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.ChangesColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.startNewSearchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.searchToolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
			this.removeSelectedToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.addToRamWatchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.freezeAddressToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.pokeAddressToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.unfreezeAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator12 = new System.Windows.Forms.ToolStripSeparator();
			this.viewInHexEditorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator14 = new System.Windows.Forms.ToolStripSeparator();
			this.clearPreviewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.menuStrip1 = new MenuStripEx();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.OpenMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.AppendFileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.TruncateFromFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.RecentSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.modeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.DetailedMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FastMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.MemoryDomainsSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
			this.sizeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._1ByteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._2ByteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._4ByteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.CheckMisalignedMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
			this.DisplayTypeSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.DefinePreviousValueSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.PreviousFrameMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.Previous_LastSearchMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.Previous_LastChangeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.Previous_OriginalMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.BigEndianMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.searchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.newSearchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
			this.undoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.redoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.CopyValueToPrevMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ClearChangeCountsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.RemoveMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.addSelectedToRamWatchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.pokeAddressToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.freezeAddressToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator13 = new System.Windows.Forms.ToolStripSeparator();
			this.clearUndoHistoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.previewModeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.alwaysExcludeRamSearchListToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.useUndoHistoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator11 = new System.Windows.Forms.ToolStripSeparator();
			this.AutoloadDialogMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveWindowPositionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.alwaysOnTopToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.restoreOriginalWindowSizeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.MemDomainLabel = new System.Windows.Forms.Label();
			this.MessageLabel = new System.Windows.Forms.Label();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.CompareToBox = new System.Windows.Forms.GroupBox();
			this.DifferenceRadio = new System.Windows.Forms.RadioButton();
			this.label1 = new System.Windows.Forms.Label();
			this.NumberOfChangesBox = new BizHawk.UnsignedIntegerBox();
			this.SpecificAddressBox = new BizHawk.HexTextBox();
			this.SpecificValueBox = new BizHawk.MultiClient.WatchValueBox();
			this.NumberOfChangesRadio = new System.Windows.Forms.RadioButton();
			this.SpecificAddressRadio = new System.Windows.Forms.RadioButton();
			this.SpecificValueRadio = new System.Windows.Forms.RadioButton();
			this.PreviousValueRadio = new System.Windows.Forms.RadioButton();
			this.toolStrip1 = new ToolStripEx();
			this.DoSearchToolButton = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator10 = new System.Windows.Forms.ToolStripSeparator();
			this.NewSearchToolButton = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator15 = new System.Windows.Forms.ToolStripSeparator();
			this.CopyValueToPrevToolBarItem = new System.Windows.Forms.ToolStripButton();
			this.ClearChangeCountsToolBarItem = new System.Windows.Forms.ToolStripButton();
			this.RemoveToolBarItem = new System.Windows.Forms.ToolStripButton();
			this.ComparisonBox = new System.Windows.Forms.GroupBox();
			this.DifferentByBox = new BizHawk.UnsignedIntegerBox();
			this.DifferentByRadio = new System.Windows.Forms.RadioButton();
			this.NotEqualToRadio = new System.Windows.Forms.RadioButton();
			this.EqualToRadio = new System.Windows.Forms.RadioButton();
			this.GreaterThanOrEqualToRadio = new System.Windows.Forms.RadioButton();
			this.LessThanOrEqualToRadio = new System.Windows.Forms.RadioButton();
			this.GreaterThanRadio = new System.Windows.Forms.RadioButton();
			this.LessThanRadio = new System.Windows.Forms.RadioButton();
			SearchMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.contextMenuStrip1.SuspendLayout();
			this.menuStrip1.SuspendLayout();
			this.CompareToBox.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			this.ComparisonBox.SuspendLayout();
			this.SuspendLayout();
			// 
			// SearchMenuItem
			// 
			SearchMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.search;
			SearchMenuItem.Name = "SearchMenuItem";
			SearchMenuItem.Size = new System.Drawing.Size(215, 22);
			SearchMenuItem.Text = "&Search";
			SearchMenuItem.Click += new System.EventHandler(this.SearchMenuItem_Click);
			// 
			// TotalSearchLabel
			// 
			this.TotalSearchLabel.AutoSize = true;
			this.TotalSearchLabel.Location = new System.Drawing.Point(12, 49);
			this.TotalSearchLabel.Name = "TotalSearchLabel";
			this.TotalSearchLabel.Size = new System.Drawing.Size(64, 13);
			this.TotalSearchLabel.TabIndex = 2;
			this.TotalSearchLabel.Text = "0 addresses";
			// 
			// WatchListView
			// 
			this.WatchListView.AllowColumnReorder = true;
			this.WatchListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.WatchListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.AddressColumn,
            this.ValueColumn,
            this.PreviousColumn,
            this.ChangesColumn});
			this.WatchListView.ContextMenuStrip = this.contextMenuStrip1;
			this.WatchListView.FullRowSelect = true;
			this.WatchListView.GridLines = true;
			this.WatchListView.HideSelection = false;
			this.WatchListView.ItemCount = 0;
			this.WatchListView.LabelEdit = true;
			this.WatchListView.Location = new System.Drawing.Point(9, 65);
			this.WatchListView.Name = "WatchListView";
			this.WatchListView.selectedItem = -1;
			this.WatchListView.Size = new System.Drawing.Size(232, 366);
			this.WatchListView.TabIndex = 1;
			this.WatchListView.UseCompatibleStateImageBehavior = false;
			this.WatchListView.View = System.Windows.Forms.View.Details;
			this.WatchListView.VirtualMode = true;
			this.WatchListView.SelectedIndexChanged += new System.EventHandler(this.WatchListView_SelectedIndexChanged);
			this.WatchListView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.WatchListView_KeyDown);
			// 
			// AddressColumn
			// 
			this.AddressColumn.Text = "Address";
			this.AddressColumn.Width = 65;
			// 
			// ValueColumn
			// 
			this.ValueColumn.Text = "Value";
			this.ValueColumn.Width = 48;
			// 
			// PreviousColumn
			// 
			this.PreviousColumn.Text = "Prev";
			this.PreviousColumn.Width = 48;
			// 
			// ChangesColumn
			// 
			this.ChangesColumn.Text = "Changes";
			this.ChangesColumn.Width = 55;
			// 
			// contextMenuStrip1
			// 
			this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.startNewSearchToolStripMenuItem,
            this.searchToolStripMenuItem2,
            this.toolStripSeparator9,
            this.removeSelectedToolStripMenuItem1,
            this.addToRamWatchToolStripMenuItem,
            this.freezeAddressToolStripMenuItem1,
            this.pokeAddressToolStripMenuItem1,
            this.unfreezeAllToolStripMenuItem,
            this.toolStripSeparator12,
            this.viewInHexEditorToolStripMenuItem,
            this.toolStripSeparator14,
            this.clearPreviewToolStripMenuItem});
			this.contextMenuStrip1.Name = "contextMenuStrip1";
			this.contextMenuStrip1.Size = new System.Drawing.Size(216, 220);
			// 
			// startNewSearchToolStripMenuItem
			// 
			this.startNewSearchToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.restart;
			this.startNewSearchToolStripMenuItem.Name = "startNewSearchToolStripMenuItem";
			this.startNewSearchToolStripMenuItem.Size = new System.Drawing.Size(215, 22);
			this.startNewSearchToolStripMenuItem.Text = "&Start New Search";
			// 
			// searchToolStripMenuItem2
			// 
			this.searchToolStripMenuItem2.Image = global::BizHawk.MultiClient.Properties.Resources.search;
			this.searchToolStripMenuItem2.Name = "searchToolStripMenuItem2";
			this.searchToolStripMenuItem2.Size = new System.Drawing.Size(215, 22);
			this.searchToolStripMenuItem2.Text = "&Search";
			// 
			// toolStripSeparator9
			// 
			this.toolStripSeparator9.Name = "toolStripSeparator9";
			this.toolStripSeparator9.Size = new System.Drawing.Size(212, 6);
			// 
			// removeSelectedToolStripMenuItem1
			// 
			this.removeSelectedToolStripMenuItem1.Image = global::BizHawk.MultiClient.Properties.Resources.Delete;
			this.removeSelectedToolStripMenuItem1.Name = "removeSelectedToolStripMenuItem1";
			this.removeSelectedToolStripMenuItem1.ShortcutKeyDisplayString = "Del";
			this.removeSelectedToolStripMenuItem1.Size = new System.Drawing.Size(215, 22);
			this.removeSelectedToolStripMenuItem1.Text = "Remove Selected";
			// 
			// addToRamWatchToolStripMenuItem
			// 
			this.addToRamWatchToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.FindHS;
			this.addToRamWatchToolStripMenuItem.Name = "addToRamWatchToolStripMenuItem";
			this.addToRamWatchToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+R";
			this.addToRamWatchToolStripMenuItem.Size = new System.Drawing.Size(215, 22);
			this.addToRamWatchToolStripMenuItem.Text = "Add to Ram Watch";
			// 
			// freezeAddressToolStripMenuItem1
			// 
			this.freezeAddressToolStripMenuItem1.Image = global::BizHawk.MultiClient.Properties.Resources.Freeze;
			this.freezeAddressToolStripMenuItem1.Name = "freezeAddressToolStripMenuItem1";
			this.freezeAddressToolStripMenuItem1.ShortcutKeyDisplayString = "Ctrl+F";
			this.freezeAddressToolStripMenuItem1.Size = new System.Drawing.Size(215, 22);
			this.freezeAddressToolStripMenuItem1.Text = "Freeze Address";
			// 
			// pokeAddressToolStripMenuItem1
			// 
			this.pokeAddressToolStripMenuItem1.Image = global::BizHawk.MultiClient.Properties.Resources.poke;
			this.pokeAddressToolStripMenuItem1.Name = "pokeAddressToolStripMenuItem1";
			this.pokeAddressToolStripMenuItem1.ShortcutKeyDisplayString = "Ctrl+P";
			this.pokeAddressToolStripMenuItem1.Size = new System.Drawing.Size(215, 22);
			this.pokeAddressToolStripMenuItem1.Text = "Poke Address";
			// 
			// unfreezeAllToolStripMenuItem
			// 
			this.unfreezeAllToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.Unfreeze;
			this.unfreezeAllToolStripMenuItem.Name = "unfreezeAllToolStripMenuItem";
			this.unfreezeAllToolStripMenuItem.Size = new System.Drawing.Size(215, 22);
			this.unfreezeAllToolStripMenuItem.Text = "Unfreeze &All";
			// 
			// toolStripSeparator12
			// 
			this.toolStripSeparator12.Name = "toolStripSeparator12";
			this.toolStripSeparator12.Size = new System.Drawing.Size(212, 6);
			// 
			// viewInHexEditorToolStripMenuItem
			// 
			this.viewInHexEditorToolStripMenuItem.Name = "viewInHexEditorToolStripMenuItem";
			this.viewInHexEditorToolStripMenuItem.Size = new System.Drawing.Size(215, 22);
			this.viewInHexEditorToolStripMenuItem.Text = "View in Hex Editor";
			// 
			// toolStripSeparator14
			// 
			this.toolStripSeparator14.Name = "toolStripSeparator14";
			this.toolStripSeparator14.Size = new System.Drawing.Size(212, 6);
			// 
			// clearPreviewToolStripMenuItem
			// 
			this.clearPreviewToolStripMenuItem.Name = "clearPreviewToolStripMenuItem";
			this.clearPreviewToolStripMenuItem.Size = new System.Drawing.Size(215, 22);
			this.clearPreviewToolStripMenuItem.Text = "&Clear Preview";
			// 
			// menuStrip1
			// 
			this.menuStrip1.ClickThrough = true;
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.settingsToolStripMenuItem,
            this.searchToolStripMenuItem,
            this.optionsToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(470, 24);
			this.menuStrip1.TabIndex = 4;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.OpenMenuItem,
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.AppendFileMenuItem,
            this.TruncateFromFileToolStripMenuItem,
            this.RecentSubMenu,
            this.toolStripSeparator4,
            this.exitToolStripMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this.fileToolStripMenuItem.Text = "&File";
			this.fileToolStripMenuItem.DropDownOpened += new System.EventHandler(this.FileSubMenu_DropDownOpened);
			// 
			// OpenMenuItem
			// 
			this.OpenMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.OpenFile;
			this.OpenMenuItem.Name = "OpenMenuItem";
			this.OpenMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.OpenMenuItem.Size = new System.Drawing.Size(195, 22);
			this.OpenMenuItem.Text = "&Open...";
			this.OpenMenuItem.Click += new System.EventHandler(this.OpenMenuItem_Click);
			// 
			// saveToolStripMenuItem
			// 
			this.saveToolStripMenuItem.Enabled = false;
			this.saveToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.SaveAs;
			this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
			this.saveToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			this.saveToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
			this.saveToolStripMenuItem.Text = "&Save";
			// 
			// saveAsToolStripMenuItem
			// 
			this.saveAsToolStripMenuItem.Enabled = false;
			this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
			this.saveAsToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.S)));
			this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
			this.saveAsToolStripMenuItem.Text = "Save As...";
			// 
			// AppendFileMenuItem
			// 
			this.AppendFileMenuItem.Name = "AppendFileMenuItem";
			this.AppendFileMenuItem.Size = new System.Drawing.Size(195, 22);
			this.AppendFileMenuItem.Text = "&Append File...";
			this.AppendFileMenuItem.Click += new System.EventHandler(this.OpenMenuItem_Click);
			// 
			// TruncateFromFileToolStripMenuItem
			// 
			this.TruncateFromFileToolStripMenuItem.Enabled = false;
			this.TruncateFromFileToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.TruncateFromFile;
			this.TruncateFromFileToolStripMenuItem.Name = "TruncateFromFileToolStripMenuItem";
			this.TruncateFromFileToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
			this.TruncateFromFileToolStripMenuItem.Text = "&Truncate from File...";
			// 
			// RecentSubMenu
			// 
			this.RecentSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator2});
			this.RecentSubMenu.Image = global::BizHawk.MultiClient.Properties.Resources.Recent;
			this.RecentSubMenu.Name = "RecentSubMenu";
			this.RecentSubMenu.Size = new System.Drawing.Size(195, 22);
			this.RecentSubMenu.Text = "Recent";
			this.RecentSubMenu.DropDownOpened += new System.EventHandler(this.RecentSubMenu_DropDownOpened);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(57, 6);
			// 
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size(192, 6);
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
			this.exitToolStripMenuItem.Text = "&Close";
			this.exitToolStripMenuItem.Click += new System.EventHandler(this.CloseMenuItem_Click);
			// 
			// settingsToolStripMenuItem
			// 
			this.settingsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.modeToolStripMenuItem,
            this.MemoryDomainsSubMenu,
            this.sizeToolStripMenuItem,
            this.CheckMisalignedMenuItem,
            this.toolStripSeparator8,
            this.DisplayTypeSubMenu,
            this.DefinePreviousValueSubMenu,
            this.BigEndianMenuItem});
			this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
			this.settingsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
			this.settingsToolStripMenuItem.Text = "&Settings";
			this.settingsToolStripMenuItem.DropDownOpened += new System.EventHandler(this.SettingsSubMenu_DropDownOpened);
			// 
			// modeToolStripMenuItem
			// 
			this.modeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.DetailedMenuItem,
            this.FastMenuItem});
			this.modeToolStripMenuItem.Name = "modeToolStripMenuItem";
			this.modeToolStripMenuItem.Size = new System.Drawing.Size(188, 22);
			this.modeToolStripMenuItem.Text = "&Mode";
			this.modeToolStripMenuItem.DropDownOpened += new System.EventHandler(this.ModeSubMenu_DropDownOpened);
			// 
			// DetailedMenuItem
			// 
			this.DetailedMenuItem.Name = "DetailedMenuItem";
			this.DetailedMenuItem.Size = new System.Drawing.Size(117, 22);
			this.DetailedMenuItem.Text = "&Detailed";
			this.DetailedMenuItem.Click += new System.EventHandler(this.DetailedMenuItem_Click);
			// 
			// FastMenuItem
			// 
			this.FastMenuItem.Name = "FastMenuItem";
			this.FastMenuItem.Size = new System.Drawing.Size(117, 22);
			this.FastMenuItem.Text = "&Fast";
			this.FastMenuItem.Click += new System.EventHandler(this.FastMenuItem_Click);
			// 
			// MemoryDomainsSubMenu
			// 
			this.MemoryDomainsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator6});
			this.MemoryDomainsSubMenu.Name = "MemoryDomainsSubMenu";
			this.MemoryDomainsSubMenu.Size = new System.Drawing.Size(188, 22);
			this.MemoryDomainsSubMenu.Text = "&Memory Domains";
			this.MemoryDomainsSubMenu.DropDownOpened += new System.EventHandler(this.MemoryDomainsSubMenu_DropDownOpened);
			// 
			// toolStripSeparator6
			// 
			this.toolStripSeparator6.Name = "toolStripSeparator6";
			this.toolStripSeparator6.Size = new System.Drawing.Size(57, 6);
			// 
			// sizeToolStripMenuItem
			// 
			this.sizeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._1ByteMenuItem,
            this._2ByteMenuItem,
            this._4ByteMenuItem});
			this.sizeToolStripMenuItem.Name = "sizeToolStripMenuItem";
			this.sizeToolStripMenuItem.Size = new System.Drawing.Size(188, 22);
			this.sizeToolStripMenuItem.Text = "&Size";
			this.sizeToolStripMenuItem.DropDownOpened += new System.EventHandler(this.SizeSubMenu_DropDownOpened);
			// 
			// _1ByteMenuItem
			// 
			this._1ByteMenuItem.Name = "_1ByteMenuItem";
			this._1ByteMenuItem.Size = new System.Drawing.Size(106, 22);
			this._1ByteMenuItem.Text = "&1 Byte";
			this._1ByteMenuItem.Click += new System.EventHandler(this._1ByteMenuItem_Click);
			// 
			// _2ByteMenuItem
			// 
			this._2ByteMenuItem.Name = "_2ByteMenuItem";
			this._2ByteMenuItem.Size = new System.Drawing.Size(106, 22);
			this._2ByteMenuItem.Text = "&2 Byte";
			this._2ByteMenuItem.Click += new System.EventHandler(this._2ByteMenuItem_Click);
			// 
			// _4ByteMenuItem
			// 
			this._4ByteMenuItem.Name = "_4ByteMenuItem";
			this._4ByteMenuItem.Size = new System.Drawing.Size(106, 22);
			this._4ByteMenuItem.Text = "&4 Byte";
			this._4ByteMenuItem.Click += new System.EventHandler(this._4ByteMenuItem_Click);
			// 
			// CheckMisalignedMenuItem
			// 
			this.CheckMisalignedMenuItem.Name = "CheckMisalignedMenuItem";
			this.CheckMisalignedMenuItem.Size = new System.Drawing.Size(188, 22);
			this.CheckMisalignedMenuItem.Text = "Check Mis-aligned";
			this.CheckMisalignedMenuItem.Click += new System.EventHandler(this.CheckMisalignedMenuItem_Click);
			// 
			// toolStripSeparator8
			// 
			this.toolStripSeparator8.Name = "toolStripSeparator8";
			this.toolStripSeparator8.Size = new System.Drawing.Size(185, 6);
			// 
			// DisplayTypeSubMenu
			// 
			this.DisplayTypeSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator1});
			this.DisplayTypeSubMenu.Name = "DisplayTypeSubMenu";
			this.DisplayTypeSubMenu.Size = new System.Drawing.Size(188, 22);
			this.DisplayTypeSubMenu.Text = "&Display Type";
			this.DisplayTypeSubMenu.DropDownOpened += new System.EventHandler(this.DisplayTypeSubMenu_DropDownOpened);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(57, 6);
			// 
			// DefinePreviousValueSubMenu
			// 
			this.DefinePreviousValueSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.PreviousFrameMenuItem,
            this.Previous_LastSearchMenuItem,
            this.Previous_LastChangeMenuItem,
            this.Previous_OriginalMenuItem});
			this.DefinePreviousValueSubMenu.Name = "DefinePreviousValueSubMenu";
			this.DefinePreviousValueSubMenu.Size = new System.Drawing.Size(188, 22);
			this.DefinePreviousValueSubMenu.Text = "Define Previous Value";
			this.DefinePreviousValueSubMenu.DropDownOpened += new System.EventHandler(this.DefinePreviousValueSubMenu_DropDownOpened);
			// 
			// PreviousFrameMenuItem
			// 
			this.PreviousFrameMenuItem.Name = "PreviousFrameMenuItem";
			this.PreviousFrameMenuItem.Size = new System.Drawing.Size(155, 22);
			this.PreviousFrameMenuItem.Text = "&Previous Frame";
			this.PreviousFrameMenuItem.Click += new System.EventHandler(this.Previous_LastFrameMenuItem_Click);
			// 
			// Previous_LastSearchMenuItem
			// 
			this.Previous_LastSearchMenuItem.Name = "Previous_LastSearchMenuItem";
			this.Previous_LastSearchMenuItem.Size = new System.Drawing.Size(155, 22);
			this.Previous_LastSearchMenuItem.Text = "Last &Search";
			this.Previous_LastSearchMenuItem.Click += new System.EventHandler(this.Previous_LastSearchMenuItem_Click);
			// 
			// Previous_LastChangeMenuItem
			// 
			this.Previous_LastChangeMenuItem.Name = "Previous_LastChangeMenuItem";
			this.Previous_LastChangeMenuItem.Size = new System.Drawing.Size(155, 22);
			this.Previous_LastChangeMenuItem.Text = "Last &Change";
			this.Previous_LastChangeMenuItem.Click += new System.EventHandler(this.Previous_LastChangeMenuItem_Click);
			// 
			// Previous_OriginalMenuItem
			// 
			this.Previous_OriginalMenuItem.Name = "Previous_OriginalMenuItem";
			this.Previous_OriginalMenuItem.Size = new System.Drawing.Size(155, 22);
			this.Previous_OriginalMenuItem.Text = "&Original";
			this.Previous_OriginalMenuItem.Click += new System.EventHandler(this.Previous_OriginalMenuItem_Click);
			// 
			// BigEndianMenuItem
			// 
			this.BigEndianMenuItem.Name = "BigEndianMenuItem";
			this.BigEndianMenuItem.Size = new System.Drawing.Size(188, 22);
			this.BigEndianMenuItem.Text = "&Big Endian";
			this.BigEndianMenuItem.Click += new System.EventHandler(this.BigEndianMenuItem_Click);
			// 
			// searchToolStripMenuItem
			// 
			this.searchToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newSearchToolStripMenuItem,
            this.toolStripSeparator7,
            SearchMenuItem,
            this.undoToolStripMenuItem,
            this.redoToolStripMenuItem,
            this.CopyValueToPrevMenuItem,
            this.ClearChangeCountsMenuItem,
            this.RemoveMenuItem,
            this.toolStripSeparator5,
            this.addSelectedToRamWatchToolStripMenuItem,
            this.pokeAddressToolStripMenuItem,
            this.freezeAddressToolStripMenuItem,
            this.toolStripSeparator13,
            this.clearUndoHistoryToolStripMenuItem});
			this.searchToolStripMenuItem.Name = "searchToolStripMenuItem";
			this.searchToolStripMenuItem.Size = new System.Drawing.Size(54, 20);
			this.searchToolStripMenuItem.Text = "&Search";
			this.searchToolStripMenuItem.DropDownOpened += new System.EventHandler(this.SearchSubMenu_DropDownOpened);
			// 
			// newSearchToolStripMenuItem
			// 
			this.newSearchToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.restart;
			this.newSearchToolStripMenuItem.Name = "newSearchToolStripMenuItem";
			this.newSearchToolStripMenuItem.Size = new System.Drawing.Size(215, 22);
			this.newSearchToolStripMenuItem.Text = "&New Search";
			this.newSearchToolStripMenuItem.Click += new System.EventHandler(this.NewSearchMenuMenuItem_Click);
			// 
			// toolStripSeparator7
			// 
			this.toolStripSeparator7.Name = "toolStripSeparator7";
			this.toolStripSeparator7.Size = new System.Drawing.Size(212, 6);
			// 
			// undoToolStripMenuItem
			// 
			this.undoToolStripMenuItem.Enabled = false;
			this.undoToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.undo;
			this.undoToolStripMenuItem.Name = "undoToolStripMenuItem";
			this.undoToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Z)));
			this.undoToolStripMenuItem.Size = new System.Drawing.Size(215, 22);
			this.undoToolStripMenuItem.Text = "&Undo";
			// 
			// redoToolStripMenuItem
			// 
			this.redoToolStripMenuItem.Enabled = false;
			this.redoToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.redo;
			this.redoToolStripMenuItem.Name = "redoToolStripMenuItem";
			this.redoToolStripMenuItem.ShortcutKeyDisplayString = "";
			this.redoToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Y)));
			this.redoToolStripMenuItem.Size = new System.Drawing.Size(215, 22);
			this.redoToolStripMenuItem.Text = "&Redo";
			// 
			// CopyValueToPrevMenuItem
			// 
			this.CopyValueToPrevMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.Previous;
			this.CopyValueToPrevMenuItem.Name = "CopyValueToPrevMenuItem";
			this.CopyValueToPrevMenuItem.Size = new System.Drawing.Size(215, 22);
			this.CopyValueToPrevMenuItem.Text = "Copy Value to Prev";
			this.CopyValueToPrevMenuItem.Click += new System.EventHandler(this.CopyValueToPrevMenuItem_Click);
			// 
			// ClearChangeCountsMenuItem
			// 
			this.ClearChangeCountsMenuItem.Name = "ClearChangeCountsMenuItem";
			this.ClearChangeCountsMenuItem.Size = new System.Drawing.Size(215, 22);
			this.ClearChangeCountsMenuItem.Text = "&Clear Change Counts";
			this.ClearChangeCountsMenuItem.Click += new System.EventHandler(this.ClearChangeCountsMenuItem_Click);
			// 
			// RemoveMenuItem
			// 
			this.RemoveMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.Delete;
			this.RemoveMenuItem.Name = "RemoveMenuItem";
			this.RemoveMenuItem.ShortcutKeyDisplayString = "Delete";
			this.RemoveMenuItem.Size = new System.Drawing.Size(215, 22);
			this.RemoveMenuItem.Text = "&Remove selected";
			this.RemoveMenuItem.Click += new System.EventHandler(this.RemoveMenuItem_Click);
			// 
			// toolStripSeparator5
			// 
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			this.toolStripSeparator5.Size = new System.Drawing.Size(212, 6);
			// 
			// addSelectedToRamWatchToolStripMenuItem
			// 
			this.addSelectedToRamWatchToolStripMenuItem.Enabled = false;
			this.addSelectedToRamWatchToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.FindHS;
			this.addSelectedToRamWatchToolStripMenuItem.Name = "addSelectedToRamWatchToolStripMenuItem";
			this.addSelectedToRamWatchToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R)));
			this.addSelectedToRamWatchToolStripMenuItem.Size = new System.Drawing.Size(215, 22);
			this.addSelectedToRamWatchToolStripMenuItem.Text = "&Add to Ram Watch";
			// 
			// pokeAddressToolStripMenuItem
			// 
			this.pokeAddressToolStripMenuItem.Enabled = false;
			this.pokeAddressToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.poke;
			this.pokeAddressToolStripMenuItem.Name = "pokeAddressToolStripMenuItem";
			this.pokeAddressToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.P)));
			this.pokeAddressToolStripMenuItem.Size = new System.Drawing.Size(215, 22);
			this.pokeAddressToolStripMenuItem.Text = "&Poke Address";
			// 
			// freezeAddressToolStripMenuItem
			// 
			this.freezeAddressToolStripMenuItem.Enabled = false;
			this.freezeAddressToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.Freeze;
			this.freezeAddressToolStripMenuItem.Name = "freezeAddressToolStripMenuItem";
			this.freezeAddressToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F)));
			this.freezeAddressToolStripMenuItem.Size = new System.Drawing.Size(215, 22);
			this.freezeAddressToolStripMenuItem.Text = "Freeze Address";
			// 
			// toolStripSeparator13
			// 
			this.toolStripSeparator13.Name = "toolStripSeparator13";
			this.toolStripSeparator13.Size = new System.Drawing.Size(212, 6);
			// 
			// clearUndoHistoryToolStripMenuItem
			// 
			this.clearUndoHistoryToolStripMenuItem.Enabled = false;
			this.clearUndoHistoryToolStripMenuItem.Name = "clearUndoHistoryToolStripMenuItem";
			this.clearUndoHistoryToolStripMenuItem.Size = new System.Drawing.Size(215, 22);
			this.clearUndoHistoryToolStripMenuItem.Text = "Clear Undo History";
			// 
			// optionsToolStripMenuItem
			// 
			this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.previewModeToolStripMenuItem,
            this.alwaysExcludeRamSearchListToolStripMenuItem,
            this.useUndoHistoryToolStripMenuItem,
            this.toolStripSeparator11,
            this.AutoloadDialogMenuItem,
            this.saveWindowPositionToolStripMenuItem,
            this.alwaysOnTopToolStripMenuItem,
            this.toolStripSeparator3,
            this.restoreOriginalWindowSizeToolStripMenuItem});
			this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
			this.optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
			this.optionsToolStripMenuItem.Text = "&Options";
			this.optionsToolStripMenuItem.DropDownOpened += new System.EventHandler(this.OptionsSubMenu_DropDownOpened);
			// 
			// previewModeToolStripMenuItem
			// 
			this.previewModeToolStripMenuItem.Enabled = false;
			this.previewModeToolStripMenuItem.Name = "previewModeToolStripMenuItem";
			this.previewModeToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
			this.previewModeToolStripMenuItem.Text = "Preview Mode";
			// 
			// alwaysExcludeRamSearchListToolStripMenuItem
			// 
			this.alwaysExcludeRamSearchListToolStripMenuItem.Enabled = false;
			this.alwaysExcludeRamSearchListToolStripMenuItem.Name = "alwaysExcludeRamSearchListToolStripMenuItem";
			this.alwaysExcludeRamSearchListToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
			this.alwaysExcludeRamSearchListToolStripMenuItem.Text = "Always Exclude Ram Search List";
			// 
			// useUndoHistoryToolStripMenuItem
			// 
			this.useUndoHistoryToolStripMenuItem.Enabled = false;
			this.useUndoHistoryToolStripMenuItem.Name = "useUndoHistoryToolStripMenuItem";
			this.useUndoHistoryToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
			this.useUndoHistoryToolStripMenuItem.Text = "&Use Undo History";
			// 
			// toolStripSeparator11
			// 
			this.toolStripSeparator11.Name = "toolStripSeparator11";
			this.toolStripSeparator11.Size = new System.Drawing.Size(237, 6);
			// 
			// AutoloadDialogMenuItem
			// 
			this.AutoloadDialogMenuItem.Name = "AutoloadDialogMenuItem";
			this.AutoloadDialogMenuItem.Size = new System.Drawing.Size(240, 22);
			this.AutoloadDialogMenuItem.Text = "Autoload";
			this.AutoloadDialogMenuItem.Click += new System.EventHandler(this.AutoloadDialogMenuItem_Click);
			// 
			// saveWindowPositionToolStripMenuItem
			// 
			this.saveWindowPositionToolStripMenuItem.Enabled = false;
			this.saveWindowPositionToolStripMenuItem.Name = "saveWindowPositionToolStripMenuItem";
			this.saveWindowPositionToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
			this.saveWindowPositionToolStripMenuItem.Text = "Save Window Position";
			// 
			// alwaysOnTopToolStripMenuItem
			// 
			this.alwaysOnTopToolStripMenuItem.Enabled = false;
			this.alwaysOnTopToolStripMenuItem.Name = "alwaysOnTopToolStripMenuItem";
			this.alwaysOnTopToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
			this.alwaysOnTopToolStripMenuItem.Text = "Always On Top";
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(237, 6);
			// 
			// restoreOriginalWindowSizeToolStripMenuItem
			// 
			this.restoreOriginalWindowSizeToolStripMenuItem.Enabled = false;
			this.restoreOriginalWindowSizeToolStripMenuItem.Name = "restoreOriginalWindowSizeToolStripMenuItem";
			this.restoreOriginalWindowSizeToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
			this.restoreOriginalWindowSizeToolStripMenuItem.Text = "Restore Default Settings";
			// 
			// MemDomainLabel
			// 
			this.MemDomainLabel.AutoSize = true;
			this.MemDomainLabel.Location = new System.Drawing.Point(135, 49);
			this.MemDomainLabel.Name = "MemDomainLabel";
			this.MemDomainLabel.Size = new System.Drawing.Size(70, 13);
			this.MemDomainLabel.TabIndex = 8;
			this.MemDomainLabel.Text = "Main Memory";
			// 
			// MessageLabel
			// 
			this.MessageLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.MessageLabel.AutoSize = true;
			this.MessageLabel.Location = new System.Drawing.Point(9, 434);
			this.MessageLabel.Name = "MessageLabel";
			this.MessageLabel.Size = new System.Drawing.Size(106, 13);
			this.MessageLabel.TabIndex = 9;
			this.MessageLabel.Text = " todo                         ";
			// 
			// CompareToBox
			// 
			this.CompareToBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.CompareToBox.Controls.Add(this.DifferenceRadio);
			this.CompareToBox.Controls.Add(this.label1);
			this.CompareToBox.Controls.Add(this.NumberOfChangesBox);
			this.CompareToBox.Controls.Add(this.SpecificAddressBox);
			this.CompareToBox.Controls.Add(this.SpecificValueBox);
			this.CompareToBox.Controls.Add(this.NumberOfChangesRadio);
			this.CompareToBox.Controls.Add(this.SpecificAddressRadio);
			this.CompareToBox.Controls.Add(this.SpecificValueRadio);
			this.CompareToBox.Controls.Add(this.PreviousValueRadio);
			this.CompareToBox.Location = new System.Drawing.Point(247, 65);
			this.CompareToBox.Name = "CompareToBox";
			this.CompareToBox.Size = new System.Drawing.Size(211, 125);
			this.CompareToBox.TabIndex = 10;
			this.CompareToBox.TabStop = false;
			this.CompareToBox.Text = "Compare To / By";
			// 
			// DifferenceRadio
			// 
			this.DifferenceRadio.AutoSize = true;
			this.DifferenceRadio.Location = new System.Drawing.Point(6, 100);
			this.DifferenceRadio.Name = "DifferenceRadio";
			this.DifferenceRadio.Size = new System.Drawing.Size(74, 17);
			this.DifferenceRadio.TabIndex = 29;
			this.DifferenceRadio.Text = "Difference";
			this.DifferenceRadio.UseVisualStyleBackColor = true;
			this.DifferenceRadio.Click += new System.EventHandler(this.DifferenceRadio_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(116, 62);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(18, 13);
			this.label1.TabIndex = 10;
			this.label1.Text = "0x";
			// 
			// NumberOfChangesBox
			// 
			this.NumberOfChangesBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.NumberOfChangesBox.Enabled = false;
			this.NumberOfChangesBox.Location = new System.Drawing.Point(135, 78);
			this.NumberOfChangesBox.MaxLength = 8;
			this.NumberOfChangesBox.Name = "NumberOfChangesBox";
			this.NumberOfChangesBox.Size = new System.Drawing.Size(65, 20);
			this.NumberOfChangesBox.TabIndex = 28;
			this.NumberOfChangesBox.TextChanged += new System.EventHandler(this.CompareToValue_TextChanged);
			// 
			// SpecificAddressBox
			// 
			this.SpecificAddressBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.SpecificAddressBox.Enabled = false;
			this.SpecificAddressBox.Location = new System.Drawing.Point(135, 58);
			this.SpecificAddressBox.MaxLength = 8;
			this.SpecificAddressBox.Name = "SpecificAddressBox";
			this.SpecificAddressBox.Size = new System.Drawing.Size(65, 20);
			this.SpecificAddressBox.TabIndex = 26;
			this.SpecificAddressBox.TextChanged += new System.EventHandler(this.CompareToValue_TextChanged);
			// 
			// SpecificValueBox
			// 
			this.SpecificValueBox.ByteSize = BizHawk.MultiClient.Watch.WatchSize.Byte;
			this.SpecificValueBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.SpecificValueBox.Enabled = false;
			this.SpecificValueBox.Location = new System.Drawing.Point(135, 38);
			this.SpecificValueBox.MaxLength = 2;
			this.SpecificValueBox.Name = "SpecificValueBox";
			this.SpecificValueBox.Size = new System.Drawing.Size(65, 20);
			this.SpecificValueBox.TabIndex = 24;
			this.SpecificValueBox.Type = BizHawk.MultiClient.Watch.DisplayType.Hex;
			this.SpecificValueBox.TextChanged += new System.EventHandler(this.CompareToValue_TextChanged);
			// 
			// NumberOfChangesRadio
			// 
			this.NumberOfChangesRadio.AutoSize = true;
			this.NumberOfChangesRadio.Location = new System.Drawing.Point(7, 80);
			this.NumberOfChangesRadio.Name = "NumberOfChangesRadio";
			this.NumberOfChangesRadio.Size = new System.Drawing.Size(122, 17);
			this.NumberOfChangesRadio.TabIndex = 3;
			this.NumberOfChangesRadio.Text = "Number of Changes:";
			this.NumberOfChangesRadio.UseVisualStyleBackColor = true;
			this.NumberOfChangesRadio.Click += new System.EventHandler(this.NumberOfChangesRadio_Click);
			// 
			// SpecificAddressRadio
			// 
			this.SpecificAddressRadio.AutoSize = true;
			this.SpecificAddressRadio.Location = new System.Drawing.Point(7, 60);
			this.SpecificAddressRadio.Name = "SpecificAddressRadio";
			this.SpecificAddressRadio.Size = new System.Drawing.Size(107, 17);
			this.SpecificAddressRadio.TabIndex = 2;
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
			this.SpecificValueRadio.TabIndex = 22;
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
			this.PreviousValueRadio.TabIndex = 20;
			this.PreviousValueRadio.TabStop = true;
			this.PreviousValueRadio.Text = "Previous Value";
			this.PreviousValueRadio.UseVisualStyleBackColor = true;
			this.PreviousValueRadio.Click += new System.EventHandler(this.PreviousValueRadio_Click);
			// 
			// toolStrip1
			// 
			this.toolStrip1.ClickThrough = true;
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.DoSearchToolButton,
            this.toolStripSeparator10,
            this.NewSearchToolButton,
            this.toolStripSeparator15,
            this.CopyValueToPrevToolBarItem,
            this.ClearChangeCountsToolBarItem,
            this.RemoveToolBarItem});
			this.toolStrip1.Location = new System.Drawing.Point(0, 24);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.Size = new System.Drawing.Size(470, 25);
			this.toolStrip1.TabIndex = 11;
			this.toolStrip1.Text = "toolStrip1";
			// 
			// DoSearchToolButton
			// 
			this.DoSearchToolButton.Image = ((System.Drawing.Image)(resources.GetObject("DoSearchToolButton.Image")));
			this.DoSearchToolButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.DoSearchToolButton.Name = "DoSearchToolButton";
			this.DoSearchToolButton.Size = new System.Drawing.Size(65, 22);
			this.DoSearchToolButton.Text = "Search ";
			this.DoSearchToolButton.Click += new System.EventHandler(this.SearchMenuItem_Click);
			// 
			// toolStripSeparator10
			// 
			this.toolStripSeparator10.Name = "toolStripSeparator10";
			this.toolStripSeparator10.Size = new System.Drawing.Size(6, 25);
			// 
			// NewSearchToolButton
			// 
			this.NewSearchToolButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.NewSearchToolButton.Image = global::BizHawk.MultiClient.Properties.Resources.restart;
			this.NewSearchToolButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.NewSearchToolButton.Name = "NewSearchToolButton";
			this.NewSearchToolButton.Size = new System.Drawing.Size(23, 22);
			this.NewSearchToolButton.Text = "Start new search";
			this.NewSearchToolButton.Click += new System.EventHandler(this.NewSearchMenuMenuItem_Click);
			// 
			// toolStripSeparator15
			// 
			this.toolStripSeparator15.Name = "toolStripSeparator15";
			this.toolStripSeparator15.Size = new System.Drawing.Size(6, 25);
			// 
			// CopyValueToPrevToolBarItem
			// 
			this.CopyValueToPrevToolBarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.CopyValueToPrevToolBarItem.Image = global::BizHawk.MultiClient.Properties.Resources.Previous;
			this.CopyValueToPrevToolBarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.CopyValueToPrevToolBarItem.Name = "CopyValueToPrevToolBarItem";
			this.CopyValueToPrevToolBarItem.Size = new System.Drawing.Size(23, 22);
			this.CopyValueToPrevToolBarItem.Text = "Copy Value to Previous";
			this.CopyValueToPrevToolBarItem.Click += new System.EventHandler(this.CopyValueToPrevMenuItem_Click);
			// 
			// ClearChangeCountsToolBarItem
			// 
			this.ClearChangeCountsToolBarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.ClearChangeCountsToolBarItem.Image = ((System.Drawing.Image)(resources.GetObject("ClearChangeCountsToolBarItem.Image")));
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
			this.RemoveToolBarItem.Image = global::BizHawk.MultiClient.Properties.Resources.Delete;
			this.RemoveToolBarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.RemoveToolBarItem.Name = "RemoveToolBarItem";
			this.RemoveToolBarItem.Size = new System.Drawing.Size(23, 22);
			this.RemoveToolBarItem.Text = "C&ut";
			this.RemoveToolBarItem.ToolTipText = "Eliminate Selected Items";
			this.RemoveToolBarItem.Click += new System.EventHandler(this.RemoveMenuItem_Click);
			// 
			// ComparisonBox
			// 
			this.ComparisonBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.ComparisonBox.Controls.Add(this.DifferentByBox);
			this.ComparisonBox.Controls.Add(this.DifferentByRadio);
			this.ComparisonBox.Controls.Add(this.NotEqualToRadio);
			this.ComparisonBox.Controls.Add(this.EqualToRadio);
			this.ComparisonBox.Controls.Add(this.GreaterThanOrEqualToRadio);
			this.ComparisonBox.Controls.Add(this.LessThanOrEqualToRadio);
			this.ComparisonBox.Controls.Add(this.GreaterThanRadio);
			this.ComparisonBox.Controls.Add(this.LessThanRadio);
			this.ComparisonBox.Location = new System.Drawing.Point(247, 196);
			this.ComparisonBox.Name = "ComparisonBox";
			this.ComparisonBox.Size = new System.Drawing.Size(211, 159);
			this.ComparisonBox.TabIndex = 12;
			this.ComparisonBox.TabStop = false;
			this.ComparisonBox.Text = "Comparison Operator";
			// 
			// DifferentByBox
			// 
			this.DifferentByBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.DifferentByBox.Enabled = false;
			this.DifferentByBox.Location = new System.Drawing.Point(90, 132);
			this.DifferentByBox.MaxLength = 9;
			this.DifferentByBox.Name = "DifferentByBox";
			this.DifferentByBox.Size = new System.Drawing.Size(50, 20);
			this.DifferentByBox.TabIndex = 34;
			this.DifferentByBox.TextChanged += new System.EventHandler(this.DifferentByBox_TextChanged);
			// 
			// DifferentByRadio
			// 
			this.DifferentByRadio.AutoSize = true;
			this.DifferentByRadio.Location = new System.Drawing.Point(7, 134);
			this.DifferentByRadio.Name = "DifferentByRadio";
			this.DifferentByRadio.Size = new System.Drawing.Size(83, 17);
			this.DifferentByRadio.TabIndex = 6;
			this.DifferentByRadio.Text = "Different By:";
			this.DifferentByRadio.UseVisualStyleBackColor = true;
			this.DifferentByRadio.Click += new System.EventHandler(this.DifferentByRadio_Click);
			// 
			// NotEqualToRadio
			// 
			this.NotEqualToRadio.AutoSize = true;
			this.NotEqualToRadio.Location = new System.Drawing.Point(7, 35);
			this.NotEqualToRadio.Name = "NotEqualToRadio";
			this.NotEqualToRadio.Size = new System.Drawing.Size(88, 17);
			this.NotEqualToRadio.TabIndex = 5;
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
			this.EqualToRadio.TabIndex = 32;
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
			this.GreaterThanOrEqualToRadio.TabIndex = 3;
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
			this.LessThanOrEqualToRadio.TabIndex = 2;
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
			this.GreaterThanRadio.TabIndex = 1;
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
			this.LessThanRadio.TabIndex = 0;
			this.LessThanRadio.Text = "Less Than";
			this.LessThanRadio.UseVisualStyleBackColor = true;
			this.LessThanRadio.Click += new System.EventHandler(this.LessThanRadio_Click);
			// 
			// NewRamSearch
			// 
			this.AllowDrop = true;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(470, 459);
			this.Controls.Add(this.ComparisonBox);
			this.Controls.Add(this.toolStrip1);
			this.Controls.Add(this.CompareToBox);
			this.Controls.Add(this.MessageLabel);
			this.Controls.Add(this.MemDomainLabel);
			this.Controls.Add(this.WatchListView);
			this.Controls.Add(this.TotalSearchLabel);
			this.Controls.Add(this.menuStrip1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.menuStrip1;
			this.MinimumSize = new System.Drawing.Size(291, 463);
			this.Name = "NewRamSearch";
			this.Text = "Brand New Experimental Ram Search";
			this.Load += new System.EventHandler(this.RamSearch_Load);
			this.contextMenuStrip1.ResumeLayout(false);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
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

		private System.Windows.Forms.Label TotalSearchLabel;
        VirtualListView WatchListView;
        private System.Windows.Forms.ColumnHeader AddressColumn;
        private System.Windows.Forms.ColumnHeader ValueColumn;
        private System.Windows.Forms.ColumnHeader PreviousColumn;
		private System.Windows.Forms.ColumnHeader ChangesColumn;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem OpenMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem restoreOriginalWindowSizeToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveWindowPositionToolStripMenuItem;
        private System.Windows.Forms.Label MemDomainLabel;
		private System.Windows.Forms.Label MessageLabel;
		private System.Windows.Forms.ToolStripMenuItem RecentSubMenu;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripMenuItem AppendFileMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.ToolStripMenuItem searchToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ClearChangeCountsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem undoToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem RemoveMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.ToolStripMenuItem addSelectedToRamWatchToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem pokeAddressToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem TruncateFromFileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem alwaysExcludeRamSearchListToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem CopyValueToPrevMenuItem;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem startNewSearchToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator9;
		private System.Windows.Forms.ToolStripMenuItem searchToolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem freezeAddressToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem removeSelectedToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem addToRamWatchToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pokeAddressToolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem freezeAddressToolStripMenuItem1;
		private MenuStripEx menuStrip1;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.ToolStripMenuItem redoToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem viewInHexEditorToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem AutoloadDialogMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator11;
		private System.Windows.Forms.ToolStripMenuItem unfreezeAllToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator12;
        private System.Windows.Forms.ToolStripMenuItem alwaysOnTopToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator13;
		private System.Windows.Forms.ToolStripMenuItem clearUndoHistoryToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem useUndoHistoryToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator14;
		private System.Windows.Forms.ToolStripMenuItem clearPreviewToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripMenuItem newSearchToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
		private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem modeToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem DetailedMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FastMenuItem;
		private System.Windows.Forms.ToolStripMenuItem MemoryDomainsSubMenu;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
		private System.Windows.Forms.ToolStripMenuItem sizeToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _1ByteMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _2ByteMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _4ByteMenuItem;
		private System.Windows.Forms.ToolStripMenuItem DisplayTypeSubMenu;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem BigEndianMenuItem;
		private System.Windows.Forms.ToolStripMenuItem CheckMisalignedMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
		private System.Windows.Forms.ToolStripMenuItem DefinePreviousValueSubMenu;
		private System.Windows.Forms.ToolStripMenuItem PreviousFrameMenuItem;
		private System.Windows.Forms.ToolStripMenuItem Previous_LastSearchMenuItem;
		private System.Windows.Forms.ToolStripMenuItem Previous_LastChangeMenuItem;
		private System.Windows.Forms.ToolStripMenuItem Previous_OriginalMenuItem;
		private System.Windows.Forms.GroupBox CompareToBox;
		private System.Windows.Forms.RadioButton DifferenceRadio;
		private System.Windows.Forms.Label label1;
		private UnsignedIntegerBox NumberOfChangesBox;
		private HexTextBox SpecificAddressBox;
		private WatchValueBox SpecificValueBox;
		private System.Windows.Forms.RadioButton NumberOfChangesRadio;
		private System.Windows.Forms.RadioButton SpecificAddressRadio;
		private System.Windows.Forms.RadioButton SpecificValueRadio;
		private System.Windows.Forms.RadioButton PreviousValueRadio;
		private ToolStripEx toolStrip1;
		private System.Windows.Forms.ToolStripButton DoSearchToolButton;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator10;
		private System.Windows.Forms.ToolStripButton NewSearchToolButton;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator15;
		private System.Windows.Forms.GroupBox ComparisonBox;
		private UnsignedIntegerBox DifferentByBox;
		private System.Windows.Forms.RadioButton DifferentByRadio;
		private System.Windows.Forms.RadioButton NotEqualToRadio;
		private System.Windows.Forms.RadioButton EqualToRadio;
		private System.Windows.Forms.RadioButton GreaterThanOrEqualToRadio;
		private System.Windows.Forms.RadioButton LessThanOrEqualToRadio;
		private System.Windows.Forms.RadioButton GreaterThanRadio;
		private System.Windows.Forms.RadioButton LessThanRadio;
		private System.Windows.Forms.ToolStripButton CopyValueToPrevToolBarItem;
		private System.Windows.Forms.ToolStripButton ClearChangeCountsToolBarItem;
		private System.Windows.Forms.ToolStripMenuItem previewModeToolStripMenuItem;
		private System.Windows.Forms.ToolStripButton RemoveToolBarItem;
    }
}