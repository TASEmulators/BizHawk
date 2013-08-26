namespace BizHawk.MultiClient
{
	partial class NewRamWatch
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewRamWatch));
			this.WatchListView = new BizHawk.VirtualListView();
			this.Address = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Value = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Prev = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.ChangeCounts = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Diff = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.DomainColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Notes = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.menuStrip1 = new MenuStripEx();
			this.filesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.newListToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.appendFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.recentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.noneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.clearToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.autoLoadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.watchesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.memoryDomainsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
			this.newWatchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.editWatchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.removeWatchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.duplicateWatchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.pokeAddressToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.freezeAddressToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.insertSeparatorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.clearChangeCountsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.moveUpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.moveDownToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.selectAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.showPreviousValueToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.showChangeCountsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.diffToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.domainToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.definePreviousValueAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.previousFrameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.lastChangeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.displayWatchesOnScreenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveWindowPositionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
			this.restoreWindowSizeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStrip1 = new ToolStripEx();
			this.newToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.openToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.saveToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.NewWatchStripButton1 = new System.Windows.Forms.ToolStripButton();
			this.EditWatchToolStripButton1 = new System.Windows.Forms.ToolStripButton();
			this.cutToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.ClearChangeCountstoolStripButton = new System.Windows.Forms.ToolStripButton();
			this.DuplicateWatchToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.PoketoolStripButton2 = new System.Windows.Forms.ToolStripButton();
			this.FreezetoolStripButton2 = new System.Windows.Forms.ToolStripButton();
			this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.MoveUpStripButton1 = new System.Windows.Forms.ToolStripButton();
			this.MoveDownStripButton1 = new System.Windows.Forms.ToolStripButton();
			this.menuStrip1.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// WatchListView
			// 
			this.WatchListView.AllowColumnReorder = true;
			this.WatchListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.WatchListView.AutoArrange = false;
			this.WatchListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Address,
            this.Value,
            this.Prev,
            this.ChangeCounts,
            this.Diff,
            this.DomainColumn,
            this.Notes});
			this.WatchListView.FullRowSelect = true;
			this.WatchListView.GridLines = true;
			this.WatchListView.HideSelection = false;
			this.WatchListView.ItemCount = 0;
			this.WatchListView.LabelEdit = true;
			this.WatchListView.Location = new System.Drawing.Point(16, 76);
			this.WatchListView.Name = "WatchListView";
			this.WatchListView.selectedItem = -1;
			this.WatchListView.Size = new System.Drawing.Size(327, 281);
			this.WatchListView.TabIndex = 2;
			this.WatchListView.UseCompatibleStateImageBehavior = false;
			this.WatchListView.View = System.Windows.Forms.View.Details;
			// 
			// Address
			// 
			this.Address.Text = "Address";
			// 
			// Value
			// 
			this.Value.Text = "Value";
			this.Value.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.Value.Width = 59;
			// 
			// Prev
			// 
			this.Prev.Text = "Prev";
			this.Prev.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.Prev.Width = 0;
			// 
			// ChangeCounts
			// 
			this.ChangeCounts.Text = "Changes";
			this.ChangeCounts.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.ChangeCounts.Width = 54;
			// 
			// Diff
			// 
			this.Diff.Text = "Diff";
			this.Diff.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.Diff.Width = 59;
			// 
			// DomainColumn
			// 
			this.DomainColumn.Text = "Domain";
			this.DomainColumn.Width = 55;
			// 
			// Notes
			// 
			this.Notes.Text = "Notes";
			this.Notes.Width = 128;
			// 
			// menuStrip1
			// 
			this.menuStrip1.ClickThrough = true;
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.filesToolStripMenuItem,
            this.watchesToolStripMenuItem,
            this.viewToolStripMenuItem,
            this.optionsToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(359, 24);
			this.menuStrip1.TabIndex = 3;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// filesToolStripMenuItem
			// 
			this.filesToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newListToolStripMenuItem,
            this.openToolStripMenuItem,
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.appendFileToolStripMenuItem,
            this.recentToolStripMenuItem,
            this.toolStripSeparator1,
            this.exitToolStripMenuItem});
			this.filesToolStripMenuItem.Name = "filesToolStripMenuItem";
			this.filesToolStripMenuItem.Size = new System.Drawing.Size(42, 20);
			this.filesToolStripMenuItem.Text = "&Files";
			// 
			// newListToolStripMenuItem
			// 
			this.newListToolStripMenuItem.Enabled = false;
			this.newListToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.NewFile;
			this.newListToolStripMenuItem.Name = "newListToolStripMenuItem";
			this.newListToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
			this.newListToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
			this.newListToolStripMenuItem.Text = "&New List";
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
			this.saveAsToolStripMenuItem.Text = "Save &As...";
			// 
			// appendFileToolStripMenuItem
			// 
			this.appendFileToolStripMenuItem.Enabled = false;
			this.appendFileToolStripMenuItem.Name = "appendFileToolStripMenuItem";
			this.appendFileToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
			this.appendFileToolStripMenuItem.Text = "A&ppend File...";
			// 
			// recentToolStripMenuItem
			// 
			this.recentToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.noneToolStripMenuItem,
            this.toolStripSeparator4,
            this.clearToolStripMenuItem,
            this.autoLoadToolStripMenuItem});
			this.recentToolStripMenuItem.Enabled = false;
			this.recentToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.Recent;
			this.recentToolStripMenuItem.Name = "recentToolStripMenuItem";
			this.recentToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
			this.recentToolStripMenuItem.Text = "Recent";
			// 
			// noneToolStripMenuItem
			// 
			this.noneToolStripMenuItem.Name = "noneToolStripMenuItem";
			this.noneToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.noneToolStripMenuItem.Text = "None";
			// 
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size(149, 6);
			// 
			// clearToolStripMenuItem
			// 
			this.clearToolStripMenuItem.Name = "clearToolStripMenuItem";
			this.clearToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.clearToolStripMenuItem.Text = "Clear";
			// 
			// autoLoadToolStripMenuItem
			// 
			this.autoLoadToolStripMenuItem.Name = "autoLoadToolStripMenuItem";
			this.autoLoadToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.autoLoadToolStripMenuItem.Text = "Auto-Load";
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(192, 6);
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Enabled = false;
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
			this.exitToolStripMenuItem.Text = "&Close";
			// 
			// watchesToolStripMenuItem
			// 
			this.watchesToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.memoryDomainsToolStripMenuItem,
            this.toolStripSeparator8,
            this.newWatchToolStripMenuItem,
            this.editWatchToolStripMenuItem,
            this.removeWatchToolStripMenuItem,
            this.duplicateWatchToolStripMenuItem,
            this.pokeAddressToolStripMenuItem,
            this.freezeAddressToolStripMenuItem,
            this.insertSeparatorToolStripMenuItem,
            this.clearChangeCountsToolStripMenuItem,
            this.toolStripSeparator3,
            this.moveUpToolStripMenuItem,
            this.moveDownToolStripMenuItem,
            this.selectAllToolStripMenuItem});
			this.watchesToolStripMenuItem.Name = "watchesToolStripMenuItem";
			this.watchesToolStripMenuItem.Size = new System.Drawing.Size(64, 20);
			this.watchesToolStripMenuItem.Text = "&Watches";
			// 
			// memoryDomainsToolStripMenuItem
			// 
			this.memoryDomainsToolStripMenuItem.Enabled = false;
			this.memoryDomainsToolStripMenuItem.Name = "memoryDomainsToolStripMenuItem";
			this.memoryDomainsToolStripMenuItem.Size = new System.Drawing.Size(224, 22);
			this.memoryDomainsToolStripMenuItem.Text = "Memory Domains";
			// 
			// toolStripSeparator8
			// 
			this.toolStripSeparator8.Name = "toolStripSeparator8";
			this.toolStripSeparator8.Size = new System.Drawing.Size(221, 6);
			// 
			// newWatchToolStripMenuItem
			// 
			this.newWatchToolStripMenuItem.Enabled = false;
			this.newWatchToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.FindHS;
			this.newWatchToolStripMenuItem.Name = "newWatchToolStripMenuItem";
			this.newWatchToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.W)));
			this.newWatchToolStripMenuItem.Size = new System.Drawing.Size(224, 22);
			this.newWatchToolStripMenuItem.Text = "&New Watch";
			// 
			// editWatchToolStripMenuItem
			// 
			this.editWatchToolStripMenuItem.Enabled = false;
			this.editWatchToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.CutHS;
			this.editWatchToolStripMenuItem.Name = "editWatchToolStripMenuItem";
			this.editWatchToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E)));
			this.editWatchToolStripMenuItem.Size = new System.Drawing.Size(224, 22);
			this.editWatchToolStripMenuItem.Text = "&Edit Watch";
			// 
			// removeWatchToolStripMenuItem
			// 
			this.removeWatchToolStripMenuItem.Enabled = false;
			this.removeWatchToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.Delete;
			this.removeWatchToolStripMenuItem.Name = "removeWatchToolStripMenuItem";
			this.removeWatchToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R)));
			this.removeWatchToolStripMenuItem.Size = new System.Drawing.Size(224, 22);
			this.removeWatchToolStripMenuItem.Text = "&Remove Watch";
			// 
			// duplicateWatchToolStripMenuItem
			// 
			this.duplicateWatchToolStripMenuItem.Enabled = false;
			this.duplicateWatchToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.Duplicate;
			this.duplicateWatchToolStripMenuItem.Name = "duplicateWatchToolStripMenuItem";
			this.duplicateWatchToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
			this.duplicateWatchToolStripMenuItem.Size = new System.Drawing.Size(224, 22);
			this.duplicateWatchToolStripMenuItem.Text = "&Duplicate Watch";
			// 
			// pokeAddressToolStripMenuItem
			// 
			this.pokeAddressToolStripMenuItem.Enabled = false;
			this.pokeAddressToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.poke;
			this.pokeAddressToolStripMenuItem.Name = "pokeAddressToolStripMenuItem";
			this.pokeAddressToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.P)));
			this.pokeAddressToolStripMenuItem.Size = new System.Drawing.Size(224, 22);
			this.pokeAddressToolStripMenuItem.Text = "Poke Address";
			// 
			// freezeAddressToolStripMenuItem
			// 
			this.freezeAddressToolStripMenuItem.Enabled = false;
			this.freezeAddressToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.Freeze;
			this.freezeAddressToolStripMenuItem.Name = "freezeAddressToolStripMenuItem";
			this.freezeAddressToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F)));
			this.freezeAddressToolStripMenuItem.Size = new System.Drawing.Size(224, 22);
			this.freezeAddressToolStripMenuItem.Text = "Freeze Address";
			// 
			// insertSeparatorToolStripMenuItem
			// 
			this.insertSeparatorToolStripMenuItem.Enabled = false;
			this.insertSeparatorToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.InsertSeparator;
			this.insertSeparatorToolStripMenuItem.Name = "insertSeparatorToolStripMenuItem";
			this.insertSeparatorToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.I)));
			this.insertSeparatorToolStripMenuItem.Size = new System.Drawing.Size(224, 22);
			this.insertSeparatorToolStripMenuItem.Text = "Insert Separator";
			// 
			// clearChangeCountsToolStripMenuItem
			// 
			this.clearChangeCountsToolStripMenuItem.Enabled = false;
			this.clearChangeCountsToolStripMenuItem.Name = "clearChangeCountsToolStripMenuItem";
			this.clearChangeCountsToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.C)));
			this.clearChangeCountsToolStripMenuItem.Size = new System.Drawing.Size(224, 22);
			this.clearChangeCountsToolStripMenuItem.Text = "&Clear Change Counts";
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(221, 6);
			// 
			// moveUpToolStripMenuItem
			// 
			this.moveUpToolStripMenuItem.Enabled = false;
			this.moveUpToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.MoveUp;
			this.moveUpToolStripMenuItem.Name = "moveUpToolStripMenuItem";
			this.moveUpToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Up)));
			this.moveUpToolStripMenuItem.Size = new System.Drawing.Size(224, 22);
			this.moveUpToolStripMenuItem.Text = "Move &Up";
			// 
			// moveDownToolStripMenuItem
			// 
			this.moveDownToolStripMenuItem.Enabled = false;
			this.moveDownToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.MoveDown;
			this.moveDownToolStripMenuItem.Name = "moveDownToolStripMenuItem";
			this.moveDownToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Down)));
			this.moveDownToolStripMenuItem.Size = new System.Drawing.Size(224, 22);
			this.moveDownToolStripMenuItem.Text = "Move &Down";
			// 
			// selectAllToolStripMenuItem
			// 
			this.selectAllToolStripMenuItem.Enabled = false;
			this.selectAllToolStripMenuItem.Name = "selectAllToolStripMenuItem";
			this.selectAllToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
			this.selectAllToolStripMenuItem.Size = new System.Drawing.Size(224, 22);
			this.selectAllToolStripMenuItem.Text = "Select &All";
			// 
			// viewToolStripMenuItem
			// 
			this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.showPreviousValueToolStripMenuItem,
            this.showChangeCountsToolStripMenuItem,
            this.diffToolStripMenuItem,
            this.domainToolStripMenuItem});
			this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
			this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
			this.viewToolStripMenuItem.Text = "&View";
			// 
			// showPreviousValueToolStripMenuItem
			// 
			this.showPreviousValueToolStripMenuItem.Enabled = false;
			this.showPreviousValueToolStripMenuItem.Name = "showPreviousValueToolStripMenuItem";
			this.showPreviousValueToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
			this.showPreviousValueToolStripMenuItem.Text = "Previous Value";
			// 
			// showChangeCountsToolStripMenuItem
			// 
			this.showChangeCountsToolStripMenuItem.Checked = true;
			this.showChangeCountsToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
			this.showChangeCountsToolStripMenuItem.Enabled = false;
			this.showChangeCountsToolStripMenuItem.Name = "showChangeCountsToolStripMenuItem";
			this.showChangeCountsToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
			this.showChangeCountsToolStripMenuItem.Text = "Change Counts";
			// 
			// diffToolStripMenuItem
			// 
			this.diffToolStripMenuItem.Enabled = false;
			this.diffToolStripMenuItem.Name = "diffToolStripMenuItem";
			this.diffToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
			this.diffToolStripMenuItem.Text = "Difference";
			// 
			// domainToolStripMenuItem
			// 
			this.domainToolStripMenuItem.Enabled = false;
			this.domainToolStripMenuItem.Name = "domainToolStripMenuItem";
			this.domainToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
			this.domainToolStripMenuItem.Text = "Domain";
			// 
			// optionsToolStripMenuItem
			// 
			this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.definePreviousValueAsToolStripMenuItem,
            this.displayWatchesOnScreenToolStripMenuItem,
            this.saveWindowPositionToolStripMenuItem,
            this.toolStripSeparator7,
            this.restoreWindowSizeToolStripMenuItem});
			this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
			this.optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
			this.optionsToolStripMenuItem.Text = "&Options";
			// 
			// definePreviousValueAsToolStripMenuItem
			// 
			this.definePreviousValueAsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.previousFrameToolStripMenuItem,
            this.lastChangeToolStripMenuItem});
			this.definePreviousValueAsToolStripMenuItem.Enabled = false;
			this.definePreviousValueAsToolStripMenuItem.Name = "definePreviousValueAsToolStripMenuItem";
			this.definePreviousValueAsToolStripMenuItem.Size = new System.Drawing.Size(217, 22);
			this.definePreviousValueAsToolStripMenuItem.Text = "Define Previous Value As";
			// 
			// previousFrameToolStripMenuItem
			// 
			this.previousFrameToolStripMenuItem.Name = "previousFrameToolStripMenuItem";
			this.previousFrameToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
			this.previousFrameToolStripMenuItem.Text = "Previous Frame";
			// 
			// lastChangeToolStripMenuItem
			// 
			this.lastChangeToolStripMenuItem.Name = "lastChangeToolStripMenuItem";
			this.lastChangeToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
			this.lastChangeToolStripMenuItem.Text = "Last Change";
			// 
			// displayWatchesOnScreenToolStripMenuItem
			// 
			this.displayWatchesOnScreenToolStripMenuItem.Enabled = false;
			this.displayWatchesOnScreenToolStripMenuItem.Name = "displayWatchesOnScreenToolStripMenuItem";
			this.displayWatchesOnScreenToolStripMenuItem.Size = new System.Drawing.Size(217, 22);
			this.displayWatchesOnScreenToolStripMenuItem.Text = "Display Watches On Screen";
			// 
			// saveWindowPositionToolStripMenuItem
			// 
			this.saveWindowPositionToolStripMenuItem.Enabled = false;
			this.saveWindowPositionToolStripMenuItem.Name = "saveWindowPositionToolStripMenuItem";
			this.saveWindowPositionToolStripMenuItem.Size = new System.Drawing.Size(217, 22);
			this.saveWindowPositionToolStripMenuItem.Text = "Save Window Position";
			// 
			// toolStripSeparator7
			// 
			this.toolStripSeparator7.Name = "toolStripSeparator7";
			this.toolStripSeparator7.Size = new System.Drawing.Size(214, 6);
			// 
			// restoreWindowSizeToolStripMenuItem
			// 
			this.restoreWindowSizeToolStripMenuItem.Enabled = false;
			this.restoreWindowSizeToolStripMenuItem.Name = "restoreWindowSizeToolStripMenuItem";
			this.restoreWindowSizeToolStripMenuItem.Size = new System.Drawing.Size(217, 22);
			this.restoreWindowSizeToolStripMenuItem.Text = "Restore Default Settings";
			// 
			// toolStrip1
			// 
			this.toolStrip1.ClickThrough = true;
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripButton,
            this.openToolStripButton,
            this.saveToolStripButton,
            this.toolStripSeparator,
            this.NewWatchStripButton1,
            this.EditWatchToolStripButton1,
            this.cutToolStripButton,
            this.ClearChangeCountstoolStripButton,
            this.DuplicateWatchToolStripButton,
            this.PoketoolStripButton2,
            this.FreezetoolStripButton2,
            this.toolStripButton1,
            this.toolStripSeparator5,
            this.MoveUpStripButton1,
            this.MoveDownStripButton1});
			this.toolStrip1.Location = new System.Drawing.Point(0, 24);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.Size = new System.Drawing.Size(359, 25);
			this.toolStrip1.TabIndex = 4;
			this.toolStrip1.TabStop = true;
			this.toolStrip1.Text = "toolStrip1";
			// 
			// newToolStripButton
			// 
			this.newToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.newToolStripButton.Enabled = false;
			this.newToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("newToolStripButton.Image")));
			this.newToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.newToolStripButton.Name = "newToolStripButton";
			this.newToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.newToolStripButton.Text = "&New";
			// 
			// openToolStripButton
			// 
			this.openToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.openToolStripButton.Enabled = false;
			this.openToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("openToolStripButton.Image")));
			this.openToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.openToolStripButton.Name = "openToolStripButton";
			this.openToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.openToolStripButton.Text = "&Open";
			// 
			// saveToolStripButton
			// 
			this.saveToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.saveToolStripButton.Enabled = false;
			this.saveToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("saveToolStripButton.Image")));
			this.saveToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.saveToolStripButton.Name = "saveToolStripButton";
			this.saveToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.saveToolStripButton.Text = "&Save";
			// 
			// toolStripSeparator
			// 
			this.toolStripSeparator.Name = "toolStripSeparator";
			this.toolStripSeparator.Size = new System.Drawing.Size(6, 25);
			// 
			// NewWatchStripButton1
			// 
			this.NewWatchStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.NewWatchStripButton1.Enabled = false;
			this.NewWatchStripButton1.Image = global::BizHawk.MultiClient.Properties.Resources.FindHS;
			this.NewWatchStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.NewWatchStripButton1.Name = "NewWatchStripButton1";
			this.NewWatchStripButton1.Size = new System.Drawing.Size(23, 22);
			this.NewWatchStripButton1.Text = "New Watch";
			this.NewWatchStripButton1.ToolTipText = "New Watch";
			// 
			// EditWatchToolStripButton1
			// 
			this.EditWatchToolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.EditWatchToolStripButton1.Enabled = false;
			this.EditWatchToolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("EditWatchToolStripButton1.Image")));
			this.EditWatchToolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.EditWatchToolStripButton1.Name = "EditWatchToolStripButton1";
			this.EditWatchToolStripButton1.Size = new System.Drawing.Size(23, 22);
			this.EditWatchToolStripButton1.Text = "Edit Watch";
			// 
			// cutToolStripButton
			// 
			this.cutToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.cutToolStripButton.Enabled = false;
			this.cutToolStripButton.Image = global::BizHawk.MultiClient.Properties.Resources.Delete;
			this.cutToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.cutToolStripButton.Name = "cutToolStripButton";
			this.cutToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.cutToolStripButton.Text = "C&ut";
			this.cutToolStripButton.ToolTipText = "Remove Watch";
			// 
			// ClearChangeCountstoolStripButton
			// 
			this.ClearChangeCountstoolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.ClearChangeCountstoolStripButton.Enabled = false;
			this.ClearChangeCountstoolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("ClearChangeCountstoolStripButton.Image")));
			this.ClearChangeCountstoolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.ClearChangeCountstoolStripButton.Name = "ClearChangeCountstoolStripButton";
			this.ClearChangeCountstoolStripButton.Size = new System.Drawing.Size(23, 22);
			this.ClearChangeCountstoolStripButton.Text = "C";
			this.ClearChangeCountstoolStripButton.ToolTipText = "Clear Change Counts";
			// 
			// DuplicateWatchToolStripButton
			// 
			this.DuplicateWatchToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.DuplicateWatchToolStripButton.Enabled = false;
			this.DuplicateWatchToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("DuplicateWatchToolStripButton.Image")));
			this.DuplicateWatchToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.DuplicateWatchToolStripButton.Name = "DuplicateWatchToolStripButton";
			this.DuplicateWatchToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.DuplicateWatchToolStripButton.Text = "Duplicate Watch";
			// 
			// PoketoolStripButton2
			// 
			this.PoketoolStripButton2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.PoketoolStripButton2.Enabled = false;
			this.PoketoolStripButton2.Image = global::BizHawk.MultiClient.Properties.Resources.poke;
			this.PoketoolStripButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.PoketoolStripButton2.Name = "PoketoolStripButton2";
			this.PoketoolStripButton2.Size = new System.Drawing.Size(23, 22);
			this.PoketoolStripButton2.Text = "toolStripButton2";
			this.PoketoolStripButton2.ToolTipText = "Poke address";
			// 
			// FreezetoolStripButton2
			// 
			this.FreezetoolStripButton2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FreezetoolStripButton2.Enabled = false;
			this.FreezetoolStripButton2.Image = global::BizHawk.MultiClient.Properties.Resources.Freeze;
			this.FreezetoolStripButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FreezetoolStripButton2.Name = "FreezetoolStripButton2";
			this.FreezetoolStripButton2.Size = new System.Drawing.Size(23, 22);
			this.FreezetoolStripButton2.Text = "Freeze Address";
			// 
			// toolStripButton1
			// 
			this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButton1.Enabled = false;
			this.toolStripButton1.Image = global::BizHawk.MultiClient.Properties.Resources.InsertSeparator;
			this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton1.Name = "toolStripButton1";
			this.toolStripButton1.Size = new System.Drawing.Size(23, 22);
			this.toolStripButton1.Text = "-";
			this.toolStripButton1.ToolTipText = "Insert Separator";
			// 
			// toolStripSeparator5
			// 
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			this.toolStripSeparator5.Size = new System.Drawing.Size(6, 25);
			// 
			// MoveUpStripButton1
			// 
			this.MoveUpStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.MoveUpStripButton1.Enabled = false;
			this.MoveUpStripButton1.Image = global::BizHawk.MultiClient.Properties.Resources.MoveUp;
			this.MoveUpStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.MoveUpStripButton1.Name = "MoveUpStripButton1";
			this.MoveUpStripButton1.Size = new System.Drawing.Size(23, 22);
			this.MoveUpStripButton1.Text = "Move Up";
			// 
			// MoveDownStripButton1
			// 
			this.MoveDownStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.MoveDownStripButton1.Enabled = false;
			this.MoveDownStripButton1.Image = global::BizHawk.MultiClient.Properties.Resources.MoveDown;
			this.MoveDownStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.MoveDownStripButton1.Name = "MoveDownStripButton1";
			this.MoveDownStripButton1.Size = new System.Drawing.Size(23, 22);
			this.MoveDownStripButton1.Text = "Move Down";
			// 
			// NewRamWatch
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(359, 378);
			this.Controls.Add(this.toolStrip1);
			this.Controls.Add(this.menuStrip1);
			this.Controls.Add(this.WatchListView);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "NewRamWatch";
			this.Text = "Brand New Experimental Ram Watch";
			this.Load += new System.EventHandler(this.NewRamWatch_Load);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private VirtualListView WatchListView;
		private System.Windows.Forms.ColumnHeader Address;
		private System.Windows.Forms.ColumnHeader Value;
		private System.Windows.Forms.ColumnHeader Prev;
		private System.Windows.Forms.ColumnHeader ChangeCounts;
		private System.Windows.Forms.ColumnHeader Diff;
		private System.Windows.Forms.ColumnHeader DomainColumn;
		private System.Windows.Forms.ColumnHeader Notes;
		private MenuStripEx menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem filesToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem newListToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem appendFileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem recentToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem noneToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.ToolStripMenuItem clearToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem autoLoadToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem watchesToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem memoryDomainsToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
		private System.Windows.Forms.ToolStripMenuItem newWatchToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem editWatchToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem removeWatchToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem duplicateWatchToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem pokeAddressToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem freezeAddressToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem insertSeparatorToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem clearChangeCountsToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripMenuItem moveUpToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem moveDownToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem selectAllToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem showPreviousValueToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem showChangeCountsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem diffToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem domainToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem definePreviousValueAsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem previousFrameToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem lastChangeToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem displayWatchesOnScreenToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveWindowPositionToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
		private System.Windows.Forms.ToolStripMenuItem restoreWindowSizeToolStripMenuItem;
		private ToolStripEx toolStrip1;
		private System.Windows.Forms.ToolStripButton newToolStripButton;
		private System.Windows.Forms.ToolStripButton openToolStripButton;
		private System.Windows.Forms.ToolStripButton saveToolStripButton;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator;
		private System.Windows.Forms.ToolStripButton NewWatchStripButton1;
		private System.Windows.Forms.ToolStripButton EditWatchToolStripButton1;
		private System.Windows.Forms.ToolStripButton cutToolStripButton;
		private System.Windows.Forms.ToolStripButton ClearChangeCountstoolStripButton;
		private System.Windows.Forms.ToolStripButton DuplicateWatchToolStripButton;
		private System.Windows.Forms.ToolStripButton PoketoolStripButton2;
		private System.Windows.Forms.ToolStripButton FreezetoolStripButton2;
		private System.Windows.Forms.ToolStripButton toolStripButton1;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
		private System.Windows.Forms.ToolStripButton MoveUpStripButton1;
		private System.Windows.Forms.ToolStripButton MoveDownStripButton1;
	}
}