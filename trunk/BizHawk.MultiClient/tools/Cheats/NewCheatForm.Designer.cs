namespace BizHawk.MultiClient
{
	partial class NewCheatForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewCheatForm));
			this.CheatListView = new BizHawk.VirtualListView();
			this.CheatName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Address = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Value = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Compare = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.On = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Domain = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
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
			this.AddCheatMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.RemoveCheatMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.DuplicateMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.InsertSeparatorMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.MoveUpMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.MoveDownMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SelectAllMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
			this.DisableAllCheatsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.GameGenieSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.OpenGameGenieEncoderDecoderMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.OptionsSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.LoadCheatFileByGameMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SaveCheatsOnCloseMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.CheatsOnOffLoadMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ShowValuesAsHexMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.AutoloadDialogMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SaveWindowPositionMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.AlwaysOnTopMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.RestoreWindowSizeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ColumnsSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStrip1 = new ToolStripEx();
			this.NewToolBarItem = new System.Windows.Forms.ToolStripButton();
			this.OpenToolBarItem = new System.Windows.Forms.ToolStripButton();
			this.SaveToolBarItem = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.RemoveToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.DuplicateToolBarItem = new System.Windows.Forms.ToolStripButton();
			this.SeparatorToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.MoveUpToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.MoveDownToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.LoadGameGenieToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.TotalLabel = new System.Windows.Forms.Label();
			this.MessageLabel = new System.Windows.Forms.Label();
			this.nameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.addressToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.valueToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.compareToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.onToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.domainToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ToggleMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.GameGenieToolbarSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.CheatsMenu.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// CheatListView
			// 
			this.CheatListView.AllowColumnReorder = true;
			this.CheatListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.CheatListView.AutoArrange = false;
			this.CheatListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.CheatName,
            this.Address,
            this.Value,
            this.Compare,
            this.On,
            this.Domain});
			this.CheatListView.FullRowSelect = true;
			this.CheatListView.GridLines = true;
			this.CheatListView.HideSelection = false;
			this.CheatListView.ItemCount = 0;
			this.CheatListView.LabelEdit = true;
			this.CheatListView.Location = new System.Drawing.Point(12, 72);
			this.CheatListView.Name = "CheatListView";
			this.CheatListView.selectedItem = -1;
			this.CheatListView.Size = new System.Drawing.Size(376, 236);
			this.CheatListView.TabIndex = 1;
			this.CheatListView.UseCompatibleStateImageBehavior = false;
			this.CheatListView.View = System.Windows.Forms.View.Details;
			// 
			// CheatName
			// 
			this.CheatName.Text = "Name";
			this.CheatName.Width = 104;
			// 
			// Address
			// 
			this.Address.Text = "Address";
			this.Address.Width = 52;
			// 
			// Value
			// 
			this.Value.Text = "Value";
			this.Value.Width = 40;
			// 
			// Compare
			// 
			this.Compare.Text = "Compare";
			// 
			// On
			// 
			this.On.Text = "On";
			this.On.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.On.Width = 40;
			// 
			// Domain
			// 
			this.Domain.Text = "Domain";
			this.Domain.Width = 75;
			// 
			// CheatsMenu
			// 
			this.CheatsMenu.ClickThrough = true;
			this.CheatsMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu,
            this.CheatsSubMenu,
            this.OptionsSubMenu,
            this.ColumnsSubMenu});
			this.CheatsMenu.Location = new System.Drawing.Point(0, 0);
			this.CheatsMenu.Name = "CheatsMenu";
			this.CheatsMenu.Size = new System.Drawing.Size(587, 24);
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
			this.NewMenuItem.Enabled = false;
			this.NewMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.NewFile;
			this.NewMenuItem.Name = "NewMenuItem";
			this.NewMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
			this.NewMenuItem.Size = new System.Drawing.Size(195, 22);
			this.NewMenuItem.Text = "&New";
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
			// SaveMenuItem
			// 
			this.SaveMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.SaveAs;
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
			this.RecentSubMenu.Image = global::BizHawk.MultiClient.Properties.Resources.Recent;
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
            this.AddCheatMenuItem,
            this.RemoveCheatMenuItem,
            this.DuplicateMenuItem,
            this.InsertSeparatorMenuItem,
            this.toolStripSeparator3,
            this.MoveUpMenuItem,
            this.MoveDownMenuItem,
            this.SelectAllMenuItem,
            this.toolStripSeparator6,
            this.DisableAllCheatsMenuItem,
            this.ToggleMenuItem,
            this.GameGenieSeparator,
            this.OpenGameGenieEncoderDecoderMenuItem});
			this.CheatsSubMenu.Name = "CheatsSubMenu";
			this.CheatsSubMenu.Size = new System.Drawing.Size(55, 20);
			this.CheatsSubMenu.Text = "&Cheats";
			this.CheatsSubMenu.DropDownOpened += new System.EventHandler(this.CheatsSubMenu_DropDownOpened);
			// 
			// AddCheatMenuItem
			// 
			this.AddCheatMenuItem.Enabled = false;
			this.AddCheatMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.Freeze;
			this.AddCheatMenuItem.Name = "AddCheatMenuItem";
			this.AddCheatMenuItem.Size = new System.Drawing.Size(233, 22);
			this.AddCheatMenuItem.Text = "&Add Cheat";
			// 
			// RemoveCheatMenuItem
			// 
			this.RemoveCheatMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.Delete;
			this.RemoveCheatMenuItem.Name = "RemoveCheatMenuItem";
			this.RemoveCheatMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R)));
			this.RemoveCheatMenuItem.Size = new System.Drawing.Size(233, 22);
			this.RemoveCheatMenuItem.Text = "&Remove Cheat";
			this.RemoveCheatMenuItem.Click += new System.EventHandler(this.RemoveCheatMenuItem_Click);
			// 
			// DuplicateMenuItem
			// 
			this.DuplicateMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.Duplicate;
			this.DuplicateMenuItem.Name = "DuplicateMenuItem";
			this.DuplicateMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
			this.DuplicateMenuItem.Size = new System.Drawing.Size(233, 22);
			this.DuplicateMenuItem.Text = "&Duplicate";
			this.DuplicateMenuItem.Click += new System.EventHandler(this.DuplicateMenuItem_Click);
			// 
			// InsertSeparatorMenuItem
			// 
			this.InsertSeparatorMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.InsertSeparator;
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
			this.MoveUpMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.MoveUp;
			this.MoveUpMenuItem.Name = "MoveUpMenuItem";
			this.MoveUpMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.U)));
			this.MoveUpMenuItem.Size = new System.Drawing.Size(233, 22);
			this.MoveUpMenuItem.Text = "Move &Up";
			this.MoveUpMenuItem.Click += new System.EventHandler(this.MoveUpMenuItem_Click);
			// 
			// MoveDownMenuItem
			// 
			this.MoveDownMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.MoveDown;
			this.MoveDownMenuItem.Name = "MoveDownMenuItem";
			this.MoveDownMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
			this.MoveDownMenuItem.Size = new System.Drawing.Size(233, 22);
			this.MoveDownMenuItem.Text = "Move &Down";
			this.MoveDownMenuItem.Click += new System.EventHandler(this.MoveDownMenuItem_Click);
			// 
			// SelectAllMenuItem
			// 
			this.SelectAllMenuItem.Name = "SelectAllMenuItem";
			this.SelectAllMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
			this.SelectAllMenuItem.Size = new System.Drawing.Size(233, 22);
			this.SelectAllMenuItem.Text = "Select &All";
			this.SelectAllMenuItem.Click += new System.EventHandler(this.SelectAllMenuItem_Click);
			// 
			// toolStripSeparator6
			// 
			this.toolStripSeparator6.Name = "toolStripSeparator6";
			this.toolStripSeparator6.Size = new System.Drawing.Size(230, 6);
			// 
			// DisableAllCheatsMenuItem
			// 
			this.DisableAllCheatsMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.Stop;
			this.DisableAllCheatsMenuItem.Name = "DisableAllCheatsMenuItem";
			this.DisableAllCheatsMenuItem.Size = new System.Drawing.Size(233, 22);
			this.DisableAllCheatsMenuItem.Text = "Disable all Cheats";
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
			this.OpenGameGenieEncoderDecoderMenuItem.Text = "Game Genie Encoder/Decoder";
			this.OpenGameGenieEncoderDecoderMenuItem.Click += new System.EventHandler(this.OpenGameGenieEncoderDecoderMenuItem_Click);
			// 
			// OptionsSubMenu
			// 
			this.OptionsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.LoadCheatFileByGameMenuItem,
            this.SaveCheatsOnCloseMenuItem,
            this.CheatsOnOffLoadMenuItem,
            this.ShowValuesAsHexMenuItem,
            this.AutoloadDialogMenuItem,
            this.SaveWindowPositionMenuItem,
            this.AlwaysOnTopMenuItem,
            this.toolStripSeparator5,
            this.RestoreWindowSizeMenuItem});
			this.OptionsSubMenu.Name = "OptionsSubMenu";
			this.OptionsSubMenu.Size = new System.Drawing.Size(61, 20);
			this.OptionsSubMenu.Text = "&Options";
			this.OptionsSubMenu.DropDownOpened += new System.EventHandler(this.OptionsSubMenu_DropDownOpened);
			// 
			// LoadCheatFileByGameMenuItem
			// 
			this.LoadCheatFileByGameMenuItem.Enabled = false;
			this.LoadCheatFileByGameMenuItem.Name = "LoadCheatFileByGameMenuItem";
			this.LoadCheatFileByGameMenuItem.Size = new System.Drawing.Size(205, 22);
			this.LoadCheatFileByGameMenuItem.Text = "Load Cheat File by Game";
			// 
			// SaveCheatsOnCloseMenuItem
			// 
			this.SaveCheatsOnCloseMenuItem.Enabled = false;
			this.SaveCheatsOnCloseMenuItem.Name = "SaveCheatsOnCloseMenuItem";
			this.SaveCheatsOnCloseMenuItem.Size = new System.Drawing.Size(205, 22);
			this.SaveCheatsOnCloseMenuItem.Text = "Save Cheats on Close";
			// 
			// CheatsOnOffLoadMenuItem
			// 
			this.CheatsOnOffLoadMenuItem.Enabled = false;
			this.CheatsOnOffLoadMenuItem.Name = "CheatsOnOffLoadMenuItem";
			this.CheatsOnOffLoadMenuItem.Size = new System.Drawing.Size(205, 22);
			this.CheatsOnOffLoadMenuItem.Text = "Disable Cheats on Load";
			// 
			// ShowValuesAsHexMenuItem
			// 
			this.ShowValuesAsHexMenuItem.Enabled = false;
			this.ShowValuesAsHexMenuItem.Name = "ShowValuesAsHexMenuItem";
			this.ShowValuesAsHexMenuItem.Size = new System.Drawing.Size(205, 22);
			this.ShowValuesAsHexMenuItem.Text = "Show Values as Hex";
			// 
			// AutoloadDialogMenuItem
			// 
			this.AutoloadDialogMenuItem.Enabled = false;
			this.AutoloadDialogMenuItem.Name = "AutoloadDialogMenuItem";
			this.AutoloadDialogMenuItem.Size = new System.Drawing.Size(205, 22);
			this.AutoloadDialogMenuItem.Text = "Auto-load Dialog";
			// 
			// SaveWindowPositionMenuItem
			// 
			this.SaveWindowPositionMenuItem.Name = "SaveWindowPositionMenuItem";
			this.SaveWindowPositionMenuItem.Size = new System.Drawing.Size(205, 22);
			this.SaveWindowPositionMenuItem.Text = "Save Window Position";
			this.SaveWindowPositionMenuItem.Click += new System.EventHandler(this.SaveWindowPositionMenuItem_Click);
			// 
			// AlwaysOnTopMenuItem
			// 
			this.AlwaysOnTopMenuItem.Name = "AlwaysOnTopMenuItem";
			this.AlwaysOnTopMenuItem.Size = new System.Drawing.Size(205, 22);
			this.AlwaysOnTopMenuItem.Text = "Always on &Top";
			this.AlwaysOnTopMenuItem.Click += new System.EventHandler(this.AlwaysOnTopMenuItem_Click);
			// 
			// toolStripSeparator5
			// 
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			this.toolStripSeparator5.Size = new System.Drawing.Size(202, 6);
			// 
			// RestoreWindowSizeMenuItem
			// 
			this.RestoreWindowSizeMenuItem.Enabled = false;
			this.RestoreWindowSizeMenuItem.Name = "RestoreWindowSizeMenuItem";
			this.RestoreWindowSizeMenuItem.Size = new System.Drawing.Size(205, 22);
			this.RestoreWindowSizeMenuItem.Text = "Restore Default Settings";
			// 
			// ColumnsSubMenu
			// 
			this.ColumnsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.nameToolStripMenuItem,
            this.addressToolStripMenuItem,
            this.valueToolStripMenuItem,
            this.compareToolStripMenuItem,
            this.onToolStripMenuItem,
            this.domainToolStripMenuItem});
			this.ColumnsSubMenu.Name = "ColumnsSubMenu";
			this.ColumnsSubMenu.Size = new System.Drawing.Size(67, 20);
			this.ColumnsSubMenu.Text = "&Columns";
			this.ColumnsSubMenu.DropDownOpened += new System.EventHandler(this.ColumnsSubMenu_DropDownOpened);
			// 
			// toolStrip1
			// 
			this.toolStrip1.ClickThrough = true;
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.NewToolBarItem,
            this.OpenToolBarItem,
            this.SaveToolBarItem,
            this.toolStripSeparator,
            this.RemoveToolbarItem,
            this.DuplicateToolBarItem,
            this.SeparatorToolbarItem,
            this.toolStripSeparator2,
            this.MoveUpToolbarItem,
            this.MoveDownToolbarItem,
            this.GameGenieToolbarSeparator,
            this.LoadGameGenieToolbarItem});
			this.toolStrip1.Location = new System.Drawing.Point(0, 24);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.Size = new System.Drawing.Size(587, 25);
			this.toolStrip1.TabIndex = 3;
			this.toolStrip1.Text = "toolStrip1";
			// 
			// NewToolBarItem
			// 
			this.NewToolBarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.NewToolBarItem.Enabled = false;
			this.NewToolBarItem.Image = ((System.Drawing.Image)(resources.GetObject("NewToolBarItem.Image")));
			this.NewToolBarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.NewToolBarItem.Name = "NewToolBarItem";
			this.NewToolBarItem.Size = new System.Drawing.Size(23, 22);
			this.NewToolBarItem.Text = "&New";
			// 
			// OpenToolBarItem
			// 
			this.OpenToolBarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.OpenToolBarItem.Image = ((System.Drawing.Image)(resources.GetObject("OpenToolBarItem.Image")));
			this.OpenToolBarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.OpenToolBarItem.Name = "OpenToolBarItem";
			this.OpenToolBarItem.Size = new System.Drawing.Size(23, 22);
			this.OpenToolBarItem.Text = "&Open";
			this.OpenToolBarItem.Click += new System.EventHandler(this.OpenMenuItem_Click);
			// 
			// SaveToolBarItem
			// 
			this.SaveToolBarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.SaveToolBarItem.Image = ((System.Drawing.Image)(resources.GetObject("SaveToolBarItem.Image")));
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
			this.RemoveToolbarItem.Image = global::BizHawk.MultiClient.Properties.Resources.Delete;
			this.RemoveToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.RemoveToolbarItem.Name = "RemoveToolbarItem";
			this.RemoveToolbarItem.Size = new System.Drawing.Size(23, 22);
			this.RemoveToolbarItem.Text = "&Remove";
			this.RemoveToolbarItem.Click += new System.EventHandler(this.RemoveCheatMenuItem_Click);
			// 
			// DuplicateToolBarItem
			// 
			this.DuplicateToolBarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.DuplicateToolBarItem.Image = ((System.Drawing.Image)(resources.GetObject("DuplicateToolBarItem.Image")));
			this.DuplicateToolBarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.DuplicateToolBarItem.Name = "DuplicateToolBarItem";
			this.DuplicateToolBarItem.Size = new System.Drawing.Size(23, 22);
			this.DuplicateToolBarItem.Text = "&Duplicate";
			this.DuplicateToolBarItem.Click += new System.EventHandler(this.DuplicateMenuItem_Click);
			// 
			// SeparatorToolbarItem
			// 
			this.SeparatorToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.SeparatorToolbarItem.Image = global::BizHawk.MultiClient.Properties.Resources.InsertSeparator;
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
			this.MoveUpToolbarItem.Image = global::BizHawk.MultiClient.Properties.Resources.MoveUp;
			this.MoveUpToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.MoveUpToolbarItem.Name = "MoveUpToolbarItem";
			this.MoveUpToolbarItem.Size = new System.Drawing.Size(23, 22);
			this.MoveUpToolbarItem.Text = "Move Up";
			this.MoveUpToolbarItem.Click += new System.EventHandler(this.MoveUpMenuItem_Click);
			// 
			// MoveDownToolbarItem
			// 
			this.MoveDownToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.MoveDownToolbarItem.Image = global::BizHawk.MultiClient.Properties.Resources.MoveDown;
			this.MoveDownToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.MoveDownToolbarItem.Name = "MoveDownToolbarItem";
			this.MoveDownToolbarItem.Size = new System.Drawing.Size(23, 22);
			this.MoveDownToolbarItem.Text = "Move Down";
			this.MoveDownToolbarItem.Click += new System.EventHandler(this.MoveDownMenuItem_Click);
			// 
			// LoadGameGenieToolbarItem
			// 
			this.LoadGameGenieToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.LoadGameGenieToolbarItem.Image = ((System.Drawing.Image)(resources.GetObject("LoadGameGenieToolbarItem.Image")));
			this.LoadGameGenieToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.LoadGameGenieToolbarItem.Name = "LoadGameGenieToolbarItem";
			this.LoadGameGenieToolbarItem.Size = new System.Drawing.Size(75, 22);
			this.LoadGameGenieToolbarItem.Text = "Game Genie";
			this.LoadGameGenieToolbarItem.ToolTipText = "Open the Game Genie Encoder/Decoder";
			this.LoadGameGenieToolbarItem.Click += new System.EventHandler(this.OpenGameGenieEncoderDecoderMenuItem_Click);
			// 
			// TotalLabel
			// 
			this.TotalLabel.AutoSize = true;
			this.TotalLabel.Location = new System.Drawing.Point(9, 52);
			this.TotalLabel.Name = "TotalLabel";
			this.TotalLabel.Size = new System.Drawing.Size(49, 13);
			this.TotalLabel.TabIndex = 6;
			this.TotalLabel.Text = "0 Cheats";
			// 
			// MessageLabel
			// 
			this.MessageLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.MessageLabel.AutoSize = true;
			this.MessageLabel.Location = new System.Drawing.Point(13, 312);
			this.MessageLabel.Name = "MessageLabel";
			this.MessageLabel.Size = new System.Drawing.Size(31, 13);
			this.MessageLabel.TabIndex = 7;
			this.MessageLabel.Text = "        ";
			// 
			// nameToolStripMenuItem
			// 
			this.nameToolStripMenuItem.Enabled = false;
			this.nameToolStripMenuItem.Name = "nameToolStripMenuItem";
			this.nameToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.nameToolStripMenuItem.Text = "&Name";
			// 
			// addressToolStripMenuItem
			// 
			this.addressToolStripMenuItem.Enabled = false;
			this.addressToolStripMenuItem.Name = "addressToolStripMenuItem";
			this.addressToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.addressToolStripMenuItem.Text = "&Address";
			// 
			// valueToolStripMenuItem
			// 
			this.valueToolStripMenuItem.Enabled = false;
			this.valueToolStripMenuItem.Name = "valueToolStripMenuItem";
			this.valueToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.valueToolStripMenuItem.Text = "&Value";
			// 
			// compareToolStripMenuItem
			// 
			this.compareToolStripMenuItem.Enabled = false;
			this.compareToolStripMenuItem.Name = "compareToolStripMenuItem";
			this.compareToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.compareToolStripMenuItem.Text = "&Compare";
			// 
			// onToolStripMenuItem
			// 
			this.onToolStripMenuItem.Enabled = false;
			this.onToolStripMenuItem.Name = "onToolStripMenuItem";
			this.onToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.onToolStripMenuItem.Text = "&On";
			// 
			// domainToolStripMenuItem
			// 
			this.domainToolStripMenuItem.Enabled = false;
			this.domainToolStripMenuItem.Name = "domainToolStripMenuItem";
			this.domainToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.domainToolStripMenuItem.Text = "&Domain";
			// 
			// ToggleMenuItem
			// 
			this.ToggleMenuItem.Name = "ToggleMenuItem";
			this.ToggleMenuItem.ShortcutKeyDisplayString = "Enter";
			this.ToggleMenuItem.Size = new System.Drawing.Size(233, 22);
			this.ToggleMenuItem.Text = "&Toggle";
			this.ToggleMenuItem.Click += new System.EventHandler(this.ToggleMenuItem_Click);
			// 
			// GameGenieToolbarSeparator
			// 
			this.GameGenieToolbarSeparator.Name = "GameGenieToolbarSeparator";
			this.GameGenieToolbarSeparator.Size = new System.Drawing.Size(6, 25);
			// 
			// NewCheatForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(587, 328);
			this.Controls.Add(this.MessageLabel);
			this.Controls.Add(this.TotalLabel);
			this.Controls.Add(this.toolStrip1);
			this.Controls.Add(this.CheatsMenu);
			this.Controls.Add(this.CheatListView);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MinimumSize = new System.Drawing.Size(285, 311);
			this.Name = "NewCheatForm";
			this.Text = "New Cheat form";
			this.Load += new System.EventHandler(this.NewCheatForm_Load);
			this.CheatsMenu.ResumeLayout(false);
			this.CheatsMenu.PerformLayout();
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private VirtualListView CheatListView;
		private System.Windows.Forms.ColumnHeader CheatName;
		private System.Windows.Forms.ColumnHeader Address;
		private System.Windows.Forms.ColumnHeader Value;
		private System.Windows.Forms.ColumnHeader Compare;
		private System.Windows.Forms.ColumnHeader On;
		private System.Windows.Forms.ColumnHeader Domain;
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
		private System.Windows.Forms.ToolStripMenuItem AddCheatMenuItem;
		private System.Windows.Forms.ToolStripMenuItem RemoveCheatMenuItem;
		private System.Windows.Forms.ToolStripMenuItem DuplicateMenuItem;
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
		private System.Windows.Forms.ToolStripMenuItem LoadCheatFileByGameMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SaveCheatsOnCloseMenuItem;
		private System.Windows.Forms.ToolStripMenuItem CheatsOnOffLoadMenuItem;
		private System.Windows.Forms.ToolStripMenuItem ShowValuesAsHexMenuItem;
		private System.Windows.Forms.ToolStripMenuItem AutoloadDialogMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SaveWindowPositionMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
		private System.Windows.Forms.ToolStripMenuItem RestoreWindowSizeMenuItem;
		private ToolStripEx toolStrip1;
		private System.Windows.Forms.ToolStripButton NewToolBarItem;
		private System.Windows.Forms.ToolStripButton OpenToolBarItem;
		private System.Windows.Forms.ToolStripButton SaveToolBarItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator;
		private System.Windows.Forms.ToolStripButton RemoveToolbarItem;
		private System.Windows.Forms.ToolStripButton DuplicateToolBarItem;
		private System.Windows.Forms.ToolStripButton SeparatorToolbarItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripButton MoveUpToolbarItem;
		private System.Windows.Forms.ToolStripButton MoveDownToolbarItem;
		private System.Windows.Forms.ToolStripButton LoadGameGenieToolbarItem;
		private System.Windows.Forms.Label TotalLabel;
		private System.Windows.Forms.ToolStripMenuItem ColumnsSubMenu;
		private System.Windows.Forms.Label MessageLabel;
		private System.Windows.Forms.ToolStripMenuItem AlwaysOnTopMenuItem;
		private System.Windows.Forms.ToolStripMenuItem nameToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem addressToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem valueToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem compareToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem onToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem domainToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem ToggleMenuItem;
		private System.Windows.Forms.ToolStripSeparator GameGenieToolbarSeparator;
	}
}