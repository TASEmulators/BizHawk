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
			System.Windows.Forms.ToolStripMenuItem searchToolStripMenuItem1;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewRamSearch));
			this.TotalSearchLabel = new System.Windows.Forms.Label();
			this.SearchListView = new BizHawk.VirtualListView();
			this.Address = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Value = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Previous = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Changes = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
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
			this.newSearchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.appendFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.TruncateFromFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.recentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.searchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.memoryDomainsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.undoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.redoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.copyValueToPrevToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.clearChangeCountsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.removeSelectedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.excludeRamWatchListToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.addSelectedToRamWatchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.pokeAddressToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.freezeAddressToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator13 = new System.Windows.Forms.ToolStripSeparator();
			this.clearUndoHistoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.definePreviousValueToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.sinceLastSearchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.originalValueToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.sinceLastFrameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.sinceLastChangeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.fastModeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.previewModeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.alwaysExcludeRamSearchListToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.autoloadDialogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveWindowPositionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator11 = new System.Windows.Forms.ToolStripSeparator();
			this.alwaysOnTopToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.restoreOriginalWindowSizeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.useUndoHistoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.CompareToBox = new System.Windows.Forms.GroupBox();
			this.label1 = new System.Windows.Forms.Label();
			this.NumberOfChangesBox = new System.Windows.Forms.TextBox();
			this.SpecificAddressBox = new BizHawk.HexTextBox();
			this.SpecificValueBox = new System.Windows.Forms.TextBox();
			this.NumberOfChangesRadio = new System.Windows.Forms.RadioButton();
			this.SpecificAddressRadio = new System.Windows.Forms.RadioButton();
			this.SpecificValueRadio = new System.Windows.Forms.RadioButton();
			this.PreviousValueRadio = new System.Windows.Forms.RadioButton();
			this.ComparisonBox = new System.Windows.Forms.GroupBox();
			this.DifferentByBox = new System.Windows.Forms.TextBox();
			this.DifferentByRadio = new System.Windows.Forms.RadioButton();
			this.NotEqualToRadio = new System.Windows.Forms.RadioButton();
			this.EqualToRadio = new System.Windows.Forms.RadioButton();
			this.GreaterThanOrEqualToRadio = new System.Windows.Forms.RadioButton();
			this.LessThanOrEqualToRadio = new System.Windows.Forms.RadioButton();
			this.GreaterThanRadio = new System.Windows.Forms.RadioButton();
			this.LessThanRadio = new System.Windows.Forms.RadioButton();
			this.MemDomainLabel = new System.Windows.Forms.Label();
			this.MessageLabel = new System.Windows.Forms.Label();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			searchToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.contextMenuStrip1.SuspendLayout();
			this.menuStrip1.SuspendLayout();
			this.CompareToBox.SuspendLayout();
			this.ComparisonBox.SuspendLayout();
			this.SuspendLayout();
			// 
			// searchToolStripMenuItem1
			// 
			searchToolStripMenuItem1.Enabled = false;
			searchToolStripMenuItem1.Image = global::BizHawk.MultiClient.Properties.Resources.search;
			searchToolStripMenuItem1.Name = "searchToolStripMenuItem1";
			searchToolStripMenuItem1.Size = new System.Drawing.Size(215, 22);
			searchToolStripMenuItem1.Text = "&Search";
			// 
			// TotalSearchLabel
			// 
			this.TotalSearchLabel.AutoSize = true;
			this.TotalSearchLabel.Location = new System.Drawing.Point(13, 33);
			this.TotalSearchLabel.Name = "TotalSearchLabel";
			this.TotalSearchLabel.Size = new System.Drawing.Size(64, 13);
			this.TotalSearchLabel.TabIndex = 2;
			this.TotalSearchLabel.Text = "0 addresses";
			// 
			// SearchListView
			// 
			this.SearchListView.AllowColumnReorder = true;
			this.SearchListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.SearchListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Address,
            this.Value,
            this.Previous,
            this.Changes});
			this.SearchListView.ContextMenuStrip = this.contextMenuStrip1;
			this.SearchListView.FullRowSelect = true;
			this.SearchListView.GridLines = true;
			this.SearchListView.HideSelection = false;
			this.SearchListView.ItemCount = 0;
			this.SearchListView.LabelEdit = true;
			this.SearchListView.Location = new System.Drawing.Point(9, 58);
			this.SearchListView.Name = "SearchListView";
			this.SearchListView.selectedItem = -1;
			this.SearchListView.Size = new System.Drawing.Size(221, 363);
			this.SearchListView.TabIndex = 1;
			this.SearchListView.UseCompatibleStateImageBehavior = false;
			this.SearchListView.View = System.Windows.Forms.View.Details;
			this.SearchListView.VirtualMode = true;
			// 
			// Address
			// 
			this.Address.Text = "Address";
			this.Address.Width = 65;
			// 
			// Value
			// 
			this.Value.Text = "Value";
			this.Value.Width = 48;
			// 
			// Previous
			// 
			this.Previous.Text = "Prev";
			this.Previous.Width = 48;
			// 
			// Changes
			// 
			this.Changes.Text = "Changes";
			this.Changes.Width = 55;
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
            this.newSearchToolStripMenuItem,
            this.toolStripSeparator1,
            this.openToolStripMenuItem,
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.appendFileToolStripMenuItem,
            this.TruncateFromFileToolStripMenuItem,
            this.recentToolStripMenuItem,
            this.toolStripSeparator4,
            this.exitToolStripMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this.fileToolStripMenuItem.Text = "&File";
			// 
			// newSearchToolStripMenuItem
			// 
			this.newSearchToolStripMenuItem.Enabled = false;
			this.newSearchToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.restart;
			this.newSearchToolStripMenuItem.Name = "newSearchToolStripMenuItem";
			this.newSearchToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
			this.newSearchToolStripMenuItem.Text = "&New Search";
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(192, 6);
			// 
			// openToolStripMenuItem
			// 
			this.openToolStripMenuItem.Enabled = false;
			this.openToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.OpenFile;
			this.openToolStripMenuItem.Name = "openToolStripMenuItem";
			this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.openToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
			this.openToolStripMenuItem.Text = "&Open...";
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
			// appendFileToolStripMenuItem
			// 
			this.appendFileToolStripMenuItem.Enabled = false;
			this.appendFileToolStripMenuItem.Name = "appendFileToolStripMenuItem";
			this.appendFileToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
			this.appendFileToolStripMenuItem.Text = "&Append File...";
			// 
			// TruncateFromFileToolStripMenuItem
			// 
			this.TruncateFromFileToolStripMenuItem.Enabled = false;
			this.TruncateFromFileToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.TruncateFromFile;
			this.TruncateFromFileToolStripMenuItem.Name = "TruncateFromFileToolStripMenuItem";
			this.TruncateFromFileToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
			this.TruncateFromFileToolStripMenuItem.Text = "&Truncate from File...";
			// 
			// recentToolStripMenuItem
			// 
			this.recentToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator2});
			this.recentToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.Recent;
			this.recentToolStripMenuItem.Name = "recentToolStripMenuItem";
			this.recentToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
			this.recentToolStripMenuItem.Text = "Recent";
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
			this.exitToolStripMenuItem.Enabled = false;
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
			this.exitToolStripMenuItem.Text = "&Close";
			// 
			// searchToolStripMenuItem
			// 
			this.searchToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.memoryDomainsToolStripMenuItem,
            searchToolStripMenuItem1,
            this.undoToolStripMenuItem,
            this.redoToolStripMenuItem,
            this.copyValueToPrevToolStripMenuItem,
            this.clearChangeCountsToolStripMenuItem,
            this.removeSelectedToolStripMenuItem,
            this.excludeRamWatchListToolStripMenuItem,
            this.toolStripSeparator5,
            this.addSelectedToRamWatchToolStripMenuItem,
            this.pokeAddressToolStripMenuItem,
            this.freezeAddressToolStripMenuItem,
            this.toolStripSeparator13,
            this.clearUndoHistoryToolStripMenuItem});
			this.searchToolStripMenuItem.Name = "searchToolStripMenuItem";
			this.searchToolStripMenuItem.Size = new System.Drawing.Size(54, 20);
			this.searchToolStripMenuItem.Text = "&Search";
			// 
			// memoryDomainsToolStripMenuItem
			// 
			this.memoryDomainsToolStripMenuItem.Enabled = false;
			this.memoryDomainsToolStripMenuItem.Name = "memoryDomainsToolStripMenuItem";
			this.memoryDomainsToolStripMenuItem.Size = new System.Drawing.Size(215, 22);
			this.memoryDomainsToolStripMenuItem.Text = "&Memory Domains";
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
			// copyValueToPrevToolStripMenuItem
			// 
			this.copyValueToPrevToolStripMenuItem.Enabled = false;
			this.copyValueToPrevToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.Previous;
			this.copyValueToPrevToolStripMenuItem.Name = "copyValueToPrevToolStripMenuItem";
			this.copyValueToPrevToolStripMenuItem.Size = new System.Drawing.Size(215, 22);
			this.copyValueToPrevToolStripMenuItem.Text = "Copy Value to Prev";
			// 
			// clearChangeCountsToolStripMenuItem
			// 
			this.clearChangeCountsToolStripMenuItem.Enabled = false;
			this.clearChangeCountsToolStripMenuItem.Name = "clearChangeCountsToolStripMenuItem";
			this.clearChangeCountsToolStripMenuItem.Size = new System.Drawing.Size(215, 22);
			this.clearChangeCountsToolStripMenuItem.Text = "&Clear Change Counts";
			// 
			// removeSelectedToolStripMenuItem
			// 
			this.removeSelectedToolStripMenuItem.Enabled = false;
			this.removeSelectedToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.Delete;
			this.removeSelectedToolStripMenuItem.Name = "removeSelectedToolStripMenuItem";
			this.removeSelectedToolStripMenuItem.ShortcutKeyDisplayString = "Delete";
			this.removeSelectedToolStripMenuItem.Size = new System.Drawing.Size(215, 22);
			this.removeSelectedToolStripMenuItem.Text = "&Remove selected";
			// 
			// excludeRamWatchListToolStripMenuItem
			// 
			this.excludeRamWatchListToolStripMenuItem.Enabled = false;
			this.excludeRamWatchListToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.TruncateFromRW;
			this.excludeRamWatchListToolStripMenuItem.Name = "excludeRamWatchListToolStripMenuItem";
			this.excludeRamWatchListToolStripMenuItem.Size = new System.Drawing.Size(215, 22);
			this.excludeRamWatchListToolStripMenuItem.Text = "Exclude Ram Watch List";
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
            this.definePreviousValueToolStripMenuItem,
            this.fastModeToolStripMenuItem,
            this.previewModeToolStripMenuItem,
            this.alwaysExcludeRamSearchListToolStripMenuItem,
            this.autoloadDialogToolStripMenuItem,
            this.saveWindowPositionToolStripMenuItem,
            this.toolStripSeparator11,
            this.alwaysOnTopToolStripMenuItem,
            this.restoreOriginalWindowSizeToolStripMenuItem,
            this.useUndoHistoryToolStripMenuItem});
			this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
			this.optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
			this.optionsToolStripMenuItem.Text = "&Options";
			// 
			// definePreviousValueToolStripMenuItem
			// 
			this.definePreviousValueToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.sinceLastSearchToolStripMenuItem,
            this.originalValueToolStripMenuItem,
            this.sinceLastFrameToolStripMenuItem,
            this.sinceLastChangeToolStripMenuItem});
			this.definePreviousValueToolStripMenuItem.Enabled = false;
			this.definePreviousValueToolStripMenuItem.Name = "definePreviousValueToolStripMenuItem";
			this.definePreviousValueToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
			this.definePreviousValueToolStripMenuItem.Text = "Define Previous Value As";
			// 
			// sinceLastSearchToolStripMenuItem
			// 
			this.sinceLastSearchToolStripMenuItem.Enabled = false;
			this.sinceLastSearchToolStripMenuItem.Name = "sinceLastSearchToolStripMenuItem";
			this.sinceLastSearchToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
			this.sinceLastSearchToolStripMenuItem.Text = "Since last Search";
			// 
			// originalValueToolStripMenuItem
			// 
			this.originalValueToolStripMenuItem.Enabled = false;
			this.originalValueToolStripMenuItem.Name = "originalValueToolStripMenuItem";
			this.originalValueToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
			this.originalValueToolStripMenuItem.Text = "Original value";
			// 
			// sinceLastFrameToolStripMenuItem
			// 
			this.sinceLastFrameToolStripMenuItem.Enabled = false;
			this.sinceLastFrameToolStripMenuItem.Name = "sinceLastFrameToolStripMenuItem";
			this.sinceLastFrameToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
			this.sinceLastFrameToolStripMenuItem.Text = "Since last Frame";
			// 
			// sinceLastChangeToolStripMenuItem
			// 
			this.sinceLastChangeToolStripMenuItem.Enabled = false;
			this.sinceLastChangeToolStripMenuItem.Name = "sinceLastChangeToolStripMenuItem";
			this.sinceLastChangeToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
			this.sinceLastChangeToolStripMenuItem.Text = "Since last Change";
			// 
			// fastModeToolStripMenuItem
			// 
			this.fastModeToolStripMenuItem.Enabled = false;
			this.fastModeToolStripMenuItem.Name = "fastModeToolStripMenuItem";
			this.fastModeToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
			this.fastModeToolStripMenuItem.Text = "Fast Mode";
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
			// autoloadDialogToolStripMenuItem
			// 
			this.autoloadDialogToolStripMenuItem.Enabled = false;
			this.autoloadDialogToolStripMenuItem.Name = "autoloadDialogToolStripMenuItem";
			this.autoloadDialogToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
			this.autoloadDialogToolStripMenuItem.Text = "Autoload Dialog";
			// 
			// saveWindowPositionToolStripMenuItem
			// 
			this.saveWindowPositionToolStripMenuItem.Enabled = false;
			this.saveWindowPositionToolStripMenuItem.Name = "saveWindowPositionToolStripMenuItem";
			this.saveWindowPositionToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
			this.saveWindowPositionToolStripMenuItem.Text = "Save Window Position";
			// 
			// toolStripSeparator11
			// 
			this.toolStripSeparator11.Name = "toolStripSeparator11";
			this.toolStripSeparator11.Size = new System.Drawing.Size(237, 6);
			// 
			// alwaysOnTopToolStripMenuItem
			// 
			this.alwaysOnTopToolStripMenuItem.Enabled = false;
			this.alwaysOnTopToolStripMenuItem.Name = "alwaysOnTopToolStripMenuItem";
			this.alwaysOnTopToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
			this.alwaysOnTopToolStripMenuItem.Text = "Always On Top";
			// 
			// restoreOriginalWindowSizeToolStripMenuItem
			// 
			this.restoreOriginalWindowSizeToolStripMenuItem.Enabled = false;
			this.restoreOriginalWindowSizeToolStripMenuItem.Name = "restoreOriginalWindowSizeToolStripMenuItem";
			this.restoreOriginalWindowSizeToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
			this.restoreOriginalWindowSizeToolStripMenuItem.Text = "Restore Window Size";
			// 
			// useUndoHistoryToolStripMenuItem
			// 
			this.useUndoHistoryToolStripMenuItem.Enabled = false;
			this.useUndoHistoryToolStripMenuItem.Name = "useUndoHistoryToolStripMenuItem";
			this.useUndoHistoryToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
			this.useUndoHistoryToolStripMenuItem.Text = "&Use Undo History";
			// 
			// CompareToBox
			// 
			this.CompareToBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.CompareToBox.Controls.Add(this.label1);
			this.CompareToBox.Controls.Add(this.NumberOfChangesBox);
			this.CompareToBox.Controls.Add(this.SpecificAddressBox);
			this.CompareToBox.Controls.Add(this.SpecificValueBox);
			this.CompareToBox.Controls.Add(this.NumberOfChangesRadio);
			this.CompareToBox.Controls.Add(this.SpecificAddressRadio);
			this.CompareToBox.Controls.Add(this.SpecificValueRadio);
			this.CompareToBox.Controls.Add(this.PreviousValueRadio);
			this.CompareToBox.Location = new System.Drawing.Point(248, 114);
			this.CompareToBox.Name = "CompareToBox";
			this.CompareToBox.Size = new System.Drawing.Size(211, 111);
			this.CompareToBox.TabIndex = 0;
			this.CompareToBox.TabStop = false;
			this.CompareToBox.Text = "Compare To / By";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(116, 65);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(18, 13);
			this.label1.TabIndex = 10;
			this.label1.Text = "0x";
			// 
			// NumberOfChangesBox
			// 
			this.NumberOfChangesBox.Enabled = false;
			this.NumberOfChangesBox.Location = new System.Drawing.Point(135, 82);
			this.NumberOfChangesBox.MaxLength = 8;
			this.NumberOfChangesBox.Name = "NumberOfChangesBox";
			this.NumberOfChangesBox.Size = new System.Drawing.Size(65, 20);
			this.NumberOfChangesBox.TabIndex = 28;
			// 
			// SpecificAddressBox
			// 
			this.SpecificAddressBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.SpecificAddressBox.Enabled = false;
			this.SpecificAddressBox.Location = new System.Drawing.Point(135, 60);
			this.SpecificAddressBox.MaxLength = 8;
			this.SpecificAddressBox.Name = "SpecificAddressBox";
			this.SpecificAddressBox.Size = new System.Drawing.Size(65, 20);
			this.SpecificAddressBox.TabIndex = 26;
			// 
			// SpecificValueBox
			// 
			this.SpecificValueBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.SpecificValueBox.Enabled = false;
			this.SpecificValueBox.Location = new System.Drawing.Point(135, 38);
			this.SpecificValueBox.MaxLength = 9;
			this.SpecificValueBox.Name = "SpecificValueBox";
			this.SpecificValueBox.Size = new System.Drawing.Size(65, 20);
			this.SpecificValueBox.TabIndex = 24;
			// 
			// NumberOfChangesRadio
			// 
			this.NumberOfChangesRadio.AutoSize = true;
			this.NumberOfChangesRadio.Location = new System.Drawing.Point(7, 85);
			this.NumberOfChangesRadio.Name = "NumberOfChangesRadio";
			this.NumberOfChangesRadio.Size = new System.Drawing.Size(122, 17);
			this.NumberOfChangesRadio.TabIndex = 3;
			this.NumberOfChangesRadio.Text = "Number of Changes:";
			this.NumberOfChangesRadio.UseVisualStyleBackColor = true;
			// 
			// SpecificAddressRadio
			// 
			this.SpecificAddressRadio.AutoSize = true;
			this.SpecificAddressRadio.Location = new System.Drawing.Point(7, 63);
			this.SpecificAddressRadio.Name = "SpecificAddressRadio";
			this.SpecificAddressRadio.Size = new System.Drawing.Size(107, 17);
			this.SpecificAddressRadio.TabIndex = 2;
			this.SpecificAddressRadio.Text = "Specific Address:";
			this.SpecificAddressRadio.UseVisualStyleBackColor = true;
			// 
			// SpecificValueRadio
			// 
			this.SpecificValueRadio.AutoSize = true;
			this.SpecificValueRadio.Location = new System.Drawing.Point(7, 41);
			this.SpecificValueRadio.Name = "SpecificValueRadio";
			this.SpecificValueRadio.Size = new System.Drawing.Size(96, 17);
			this.SpecificValueRadio.TabIndex = 22;
			this.SpecificValueRadio.Text = "Specific Value:";
			this.SpecificValueRadio.UseVisualStyleBackColor = true;
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
			this.ComparisonBox.Location = new System.Drawing.Point(248, 231);
			this.ComparisonBox.Name = "ComparisonBox";
			this.ComparisonBox.Size = new System.Drawing.Size(211, 159);
			this.ComparisonBox.TabIndex = 6;
			this.ComparisonBox.TabStop = false;
			this.ComparisonBox.Text = "Comparison Operator";
			// 
			// DifferentByBox
			// 
			this.DifferentByBox.Enabled = false;
			this.DifferentByBox.Location = new System.Drawing.Point(90, 131);
			this.DifferentByBox.MaxLength = 9;
			this.DifferentByBox.Name = "DifferentByBox";
			this.DifferentByBox.Size = new System.Drawing.Size(50, 20);
			this.DifferentByBox.TabIndex = 34;
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
			// 
			// MemDomainLabel
			// 
			this.MemDomainLabel.AutoSize = true;
			this.MemDomainLabel.Location = new System.Drawing.Point(129, 33);
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
			this.MessageLabel.Size = new System.Drawing.Size(85, 13);
			this.MessageLabel.TabIndex = 9;
			this.MessageLabel.Text = "                          ";
			// 
			// NewRamSearch
			// 
			this.AllowDrop = true;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(470, 459);
			this.Controls.Add(this.MessageLabel);
			this.Controls.Add(this.MemDomainLabel);
			this.Controls.Add(this.ComparisonBox);
			this.Controls.Add(this.CompareToBox);
			this.Controls.Add(this.SearchListView);
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
			this.ComparisonBox.ResumeLayout(false);
			this.ComparisonBox.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

		private System.Windows.Forms.Label TotalSearchLabel;
        VirtualListView SearchListView;
        private System.Windows.Forms.ColumnHeader Address;
        private System.Windows.Forms.ColumnHeader Value;
        private System.Windows.Forms.ColumnHeader Previous;
		private System.Windows.Forms.ColumnHeader Changes;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.GroupBox CompareToBox;
        private System.Windows.Forms.RadioButton NumberOfChangesRadio;
        private System.Windows.Forms.RadioButton SpecificAddressRadio;
        private System.Windows.Forms.RadioButton SpecificValueRadio;
        private System.Windows.Forms.RadioButton PreviousValueRadio;
        private System.Windows.Forms.TextBox NumberOfChangesBox;
        private HexTextBox SpecificAddressBox;
		private System.Windows.Forms.TextBox SpecificValueBox;
        private System.Windows.Forms.GroupBox ComparisonBox;
        private System.Windows.Forms.RadioButton DifferentByRadio;
        private System.Windows.Forms.RadioButton NotEqualToRadio;
        private System.Windows.Forms.RadioButton EqualToRadio;
        private System.Windows.Forms.RadioButton GreaterThanOrEqualToRadio;
        private System.Windows.Forms.RadioButton LessThanOrEqualToRadio;
        private System.Windows.Forms.RadioButton GreaterThanRadio;
        private System.Windows.Forms.RadioButton LessThanRadio;
		private System.Windows.Forms.TextBox DifferentByBox;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newSearchToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem restoreOriginalWindowSizeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveWindowPositionToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.Label MemDomainLabel;
        private System.Windows.Forms.Label MessageLabel;
        private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ToolStripMenuItem recentToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripMenuItem appendFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.ToolStripMenuItem searchToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem clearChangeCountsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem undoToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem removeSelectedToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.ToolStripMenuItem addSelectedToRamWatchToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pokeAddressToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem definePreviousValueToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem sinceLastSearchToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem sinceLastFrameToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem previewModeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem originalValueToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem TruncateFromFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem excludeRamWatchListToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem alwaysExcludeRamSearchListToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem copyValueToPrevToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem startNewSearchToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator9;
        private System.Windows.Forms.ToolStripMenuItem searchToolStripMenuItem2;
		private System.Windows.Forms.ToolStripMenuItem memoryDomainsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem freezeAddressToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem removeSelectedToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem addToRamWatchToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pokeAddressToolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem freezeAddressToolStripMenuItem1;
		private MenuStripEx menuStrip1;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.ToolStripMenuItem redoToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem viewInHexEditorToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem sinceLastChangeToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem autoloadDialogToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator11;
		private System.Windows.Forms.ToolStripMenuItem unfreezeAllToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator12;
        private System.Windows.Forms.ToolStripMenuItem alwaysOnTopToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator13;
		private System.Windows.Forms.ToolStripMenuItem clearUndoHistoryToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem fastModeToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem useUndoHistoryToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator14;
		private System.Windows.Forms.ToolStripMenuItem clearPreviewToolStripMenuItem;
    }
}