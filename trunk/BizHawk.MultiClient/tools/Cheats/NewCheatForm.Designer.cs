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
			this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
			this.OpenGameGenieEncoderDecoderMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.OptionsSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.LoadCheatFileByGameMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SaveCheatsOnCloseMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.CheatsOnOffLoadMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ShowValuesAsHexMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.AutoloadDialogMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SaveWindowPositionMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.RestoreWindowSizeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ColumnsSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStrip1 = new ToolStripEx();
			this.newToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.openToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.saveToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.cutToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.copyToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.toolStripButtonSeparator = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripButtonMoveUp = new System.Windows.Forms.ToolStripButton();
			this.toolStripButtonMoveDown = new System.Windows.Forms.ToolStripButton();
			this.toolStripButtonLoadGameGenie = new System.Windows.Forms.ToolStripButton();
			this.TotalLabel = new System.Windows.Forms.Label();
			this.MessageLabel = new System.Windows.Forms.Label();
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
			this.OpenMenuItem.Enabled = false;
			this.OpenMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.OpenFile;
			this.OpenMenuItem.Name = "OpenMenuItem";
			this.OpenMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.OpenMenuItem.Size = new System.Drawing.Size(195, 22);
			this.OpenMenuItem.Text = "&Open...";
			// 
			// SaveMenuItem
			// 
			this.SaveMenuItem.Enabled = false;
			this.SaveMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.SaveAs;
			this.SaveMenuItem.Name = "SaveMenuItem";
			this.SaveMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			this.SaveMenuItem.Size = new System.Drawing.Size(195, 22);
			this.SaveMenuItem.Text = "&Save";
			// 
			// SaveAsMenuItem
			// 
			this.SaveAsMenuItem.Enabled = false;
			this.SaveAsMenuItem.Name = "SaveAsMenuItem";
			this.SaveAsMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.S)));
			this.SaveAsMenuItem.Size = new System.Drawing.Size(195, 22);
			this.SaveAsMenuItem.Text = "Save &As...";
			// 
			// AppendMenuItem
			// 
			this.AppendMenuItem.Enabled = false;
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
			this.toolStripSeparator4.Size = new System.Drawing.Size(149, 6);
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
            this.toolStripSeparator7,
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
			this.RemoveCheatMenuItem.Enabled = false;
			this.RemoveCheatMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.Delete;
			this.RemoveCheatMenuItem.Name = "RemoveCheatMenuItem";
			this.RemoveCheatMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R)));
			this.RemoveCheatMenuItem.Size = new System.Drawing.Size(233, 22);
			this.RemoveCheatMenuItem.Text = "&Remove Cheat";
			// 
			// DuplicateMenuItem
			// 
			this.DuplicateMenuItem.Enabled = false;
			this.DuplicateMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.Duplicate;
			this.DuplicateMenuItem.Name = "DuplicateMenuItem";
			this.DuplicateMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
			this.DuplicateMenuItem.Size = new System.Drawing.Size(233, 22);
			this.DuplicateMenuItem.Text = "&Duplicate";
			// 
			// InsertSeparatorMenuItem
			// 
			this.InsertSeparatorMenuItem.Enabled = false;
			this.InsertSeparatorMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.InsertSeparator;
			this.InsertSeparatorMenuItem.Name = "InsertSeparatorMenuItem";
			this.InsertSeparatorMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.I)));
			this.InsertSeparatorMenuItem.Size = new System.Drawing.Size(233, 22);
			this.InsertSeparatorMenuItem.Text = "Insert Separator";
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(230, 6);
			// 
			// MoveUpMenuItem
			// 
			this.MoveUpMenuItem.Enabled = false;
			this.MoveUpMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.MoveUp;
			this.MoveUpMenuItem.Name = "MoveUpMenuItem";
			this.MoveUpMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.U)));
			this.MoveUpMenuItem.Size = new System.Drawing.Size(233, 22);
			this.MoveUpMenuItem.Text = "Move &Up";
			// 
			// MoveDownMenuItem
			// 
			this.MoveDownMenuItem.Enabled = false;
			this.MoveDownMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.MoveDown;
			this.MoveDownMenuItem.Name = "MoveDownMenuItem";
			this.MoveDownMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
			this.MoveDownMenuItem.Size = new System.Drawing.Size(233, 22);
			this.MoveDownMenuItem.Text = "Move &Down";
			// 
			// SelectAllMenuItem
			// 
			this.SelectAllMenuItem.Enabled = false;
			this.SelectAllMenuItem.Name = "SelectAllMenuItem";
			this.SelectAllMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
			this.SelectAllMenuItem.Size = new System.Drawing.Size(233, 22);
			this.SelectAllMenuItem.Text = "Select &All";
			// 
			// toolStripSeparator6
			// 
			this.toolStripSeparator6.Name = "toolStripSeparator6";
			this.toolStripSeparator6.Size = new System.Drawing.Size(230, 6);
			// 
			// DisableAllCheatsMenuItem
			// 
			this.DisableAllCheatsMenuItem.Enabled = false;
			this.DisableAllCheatsMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.Stop;
			this.DisableAllCheatsMenuItem.Name = "DisableAllCheatsMenuItem";
			this.DisableAllCheatsMenuItem.Size = new System.Drawing.Size(233, 22);
			this.DisableAllCheatsMenuItem.Text = "Disable all Cheats";
			// 
			// toolStripSeparator7
			// 
			this.toolStripSeparator7.Name = "toolStripSeparator7";
			this.toolStripSeparator7.Size = new System.Drawing.Size(230, 6);
			// 
			// OpenGameGenieEncoderDecoderMenuItem
			// 
			this.OpenGameGenieEncoderDecoderMenuItem.Enabled = false;
			this.OpenGameGenieEncoderDecoderMenuItem.Name = "OpenGameGenieEncoderDecoderMenuItem";
			this.OpenGameGenieEncoderDecoderMenuItem.Size = new System.Drawing.Size(233, 22);
			this.OpenGameGenieEncoderDecoderMenuItem.Text = "Game Genie Encoder/Decoder";
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
			this.SaveWindowPositionMenuItem.Enabled = false;
			this.SaveWindowPositionMenuItem.Name = "SaveWindowPositionMenuItem";
			this.SaveWindowPositionMenuItem.Size = new System.Drawing.Size(205, 22);
			this.SaveWindowPositionMenuItem.Text = "Save Window Position";
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
			this.ColumnsSubMenu.Name = "ColumnsSubMenu";
			this.ColumnsSubMenu.Size = new System.Drawing.Size(67, 20);
			this.ColumnsSubMenu.Text = "&Columns";
			this.ColumnsSubMenu.DropDownOpened += new System.EventHandler(this.ColumnsSubMenu_DropDownOpened);
			// 
			// toolStrip1
			// 
			this.toolStrip1.ClickThrough = true;
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripButton,
            this.openToolStripButton,
            this.saveToolStripButton,
            this.toolStripSeparator,
            this.cutToolStripButton,
            this.copyToolStripButton,
            this.toolStripButtonSeparator,
            this.toolStripSeparator2,
            this.toolStripButtonMoveUp,
            this.toolStripButtonMoveDown,
            this.toolStripButtonLoadGameGenie});
			this.toolStrip1.Location = new System.Drawing.Point(0, 24);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.Size = new System.Drawing.Size(587, 25);
			this.toolStrip1.TabIndex = 3;
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
			// cutToolStripButton
			// 
			this.cutToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.cutToolStripButton.Enabled = false;
			this.cutToolStripButton.Image = global::BizHawk.MultiClient.Properties.Resources.Delete;
			this.cutToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.cutToolStripButton.Name = "cutToolStripButton";
			this.cutToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.cutToolStripButton.Text = "&Remove";
			// 
			// copyToolStripButton
			// 
			this.copyToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.copyToolStripButton.Enabled = false;
			this.copyToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("copyToolStripButton.Image")));
			this.copyToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.copyToolStripButton.Name = "copyToolStripButton";
			this.copyToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.copyToolStripButton.Text = "&Duplicate";
			// 
			// toolStripButtonSeparator
			// 
			this.toolStripButtonSeparator.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButtonSeparator.Enabled = false;
			this.toolStripButtonSeparator.Image = global::BizHawk.MultiClient.Properties.Resources.InsertSeparator;
			this.toolStripButtonSeparator.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButtonSeparator.Name = "toolStripButtonSeparator";
			this.toolStripButtonSeparator.Size = new System.Drawing.Size(23, 22);
			this.toolStripButtonSeparator.Text = "Insert Separator";
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
			// 
			// toolStripButtonMoveUp
			// 
			this.toolStripButtonMoveUp.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButtonMoveUp.Enabled = false;
			this.toolStripButtonMoveUp.Image = global::BizHawk.MultiClient.Properties.Resources.MoveUp;
			this.toolStripButtonMoveUp.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButtonMoveUp.Name = "toolStripButtonMoveUp";
			this.toolStripButtonMoveUp.Size = new System.Drawing.Size(23, 22);
			this.toolStripButtonMoveUp.Text = "Move Up";
			// 
			// toolStripButtonMoveDown
			// 
			this.toolStripButtonMoveDown.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButtonMoveDown.Enabled = false;
			this.toolStripButtonMoveDown.Image = global::BizHawk.MultiClient.Properties.Resources.MoveDown;
			this.toolStripButtonMoveDown.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButtonMoveDown.Name = "toolStripButtonMoveDown";
			this.toolStripButtonMoveDown.Size = new System.Drawing.Size(23, 22);
			this.toolStripButtonMoveDown.Text = "Move Down";
			// 
			// toolStripButtonLoadGameGenie
			// 
			this.toolStripButtonLoadGameGenie.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.toolStripButtonLoadGameGenie.Enabled = false;
			this.toolStripButtonLoadGameGenie.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonLoadGameGenie.Image")));
			this.toolStripButtonLoadGameGenie.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButtonLoadGameGenie.Name = "toolStripButtonLoadGameGenie";
			this.toolStripButtonLoadGameGenie.Size = new System.Drawing.Size(75, 22);
			this.toolStripButtonLoadGameGenie.Text = "Game Genie";
			this.toolStripButtonLoadGameGenie.ToolTipText = "Open the Game Genie Encoder/Decoder";
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
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
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
		private System.Windows.Forms.ToolStripButton newToolStripButton;
		private System.Windows.Forms.ToolStripButton openToolStripButton;
		private System.Windows.Forms.ToolStripButton saveToolStripButton;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator;
		private System.Windows.Forms.ToolStripButton cutToolStripButton;
		private System.Windows.Forms.ToolStripButton copyToolStripButton;
		private System.Windows.Forms.ToolStripButton toolStripButtonSeparator;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripButton toolStripButtonMoveUp;
		private System.Windows.Forms.ToolStripButton toolStripButtonMoveDown;
		private System.Windows.Forms.ToolStripButton toolStripButtonLoadGameGenie;
		private System.Windows.Forms.Label TotalLabel;
		private System.Windows.Forms.ToolStripMenuItem ColumnsSubMenu;
		private System.Windows.Forms.Label MessageLabel;
	}
}