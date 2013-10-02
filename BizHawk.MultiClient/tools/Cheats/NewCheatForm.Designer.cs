namespace BizHawk.MultiClient.tools.Cheats
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
			this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.appendFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.recentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.CheatsSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.addCheatToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.removeCheatToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.duplicateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.insertSeparatorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.moveUpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.moveDownToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.selectAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
			this.disableAllCheatsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
			this.openGameGenieEncoderDecoderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.OptionsSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.LoadCheatFileByGameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveCheatsOnCloseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.CheatsOnOffLoadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.showValuesAsHexToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.autoloadDialogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveWindowPositionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.restoreWindowSizeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
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
			this.NumCheatsLabel = new System.Windows.Forms.Label();
			this.ColumnsSubMenu = new System.Windows.Forms.ToolStripMenuItem();
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
            this.newToolStripMenuItem,
            this.openToolStripMenuItem,
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.appendFileToolStripMenuItem,
            this.recentToolStripMenuItem,
            this.toolStripSeparator1,
            this.exitToolStripMenuItem});
			this.FileSubMenu.Name = "FileSubMenu";
			this.FileSubMenu.Size = new System.Drawing.Size(37, 20);
			this.FileSubMenu.Text = "&File";
			// 
			// newToolStripMenuItem
			// 
			this.newToolStripMenuItem.Enabled = false;
			this.newToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.NewFile;
			this.newToolStripMenuItem.Name = "newToolStripMenuItem";
			this.newToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
			this.newToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
			this.newToolStripMenuItem.Text = "&New";
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
			this.appendFileToolStripMenuItem.Text = "Append File";
			// 
			// recentToolStripMenuItem
			// 
			this.recentToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator4});
			this.recentToolStripMenuItem.Enabled = false;
			this.recentToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.Recent;
			this.recentToolStripMenuItem.Name = "recentToolStripMenuItem";
			this.recentToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
			this.recentToolStripMenuItem.Text = "Recent";
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
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Enabled = false;
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
			this.exitToolStripMenuItem.Text = "E&xit";
			// 
			// CheatsSubMenu
			// 
			this.CheatsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addCheatToolStripMenuItem,
            this.removeCheatToolStripMenuItem,
            this.duplicateToolStripMenuItem,
            this.insertSeparatorToolStripMenuItem,
            this.toolStripSeparator3,
            this.moveUpToolStripMenuItem,
            this.moveDownToolStripMenuItem,
            this.selectAllToolStripMenuItem,
            this.toolStripSeparator6,
            this.disableAllCheatsToolStripMenuItem,
            this.toolStripSeparator7,
            this.openGameGenieEncoderDecoderToolStripMenuItem});
			this.CheatsSubMenu.Name = "CheatsSubMenu";
			this.CheatsSubMenu.Size = new System.Drawing.Size(55, 20);
			this.CheatsSubMenu.Text = "&Cheats";
			// 
			// addCheatToolStripMenuItem
			// 
			this.addCheatToolStripMenuItem.Enabled = false;
			this.addCheatToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.Freeze;
			this.addCheatToolStripMenuItem.Name = "addCheatToolStripMenuItem";
			this.addCheatToolStripMenuItem.Size = new System.Drawing.Size(233, 22);
			this.addCheatToolStripMenuItem.Text = "&Add Cheat";
			// 
			// removeCheatToolStripMenuItem
			// 
			this.removeCheatToolStripMenuItem.Enabled = false;
			this.removeCheatToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.Delete;
			this.removeCheatToolStripMenuItem.Name = "removeCheatToolStripMenuItem";
			this.removeCheatToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R)));
			this.removeCheatToolStripMenuItem.Size = new System.Drawing.Size(233, 22);
			this.removeCheatToolStripMenuItem.Text = "&Remove Cheat";
			// 
			// duplicateToolStripMenuItem
			// 
			this.duplicateToolStripMenuItem.Enabled = false;
			this.duplicateToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.Duplicate;
			this.duplicateToolStripMenuItem.Name = "duplicateToolStripMenuItem";
			this.duplicateToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
			this.duplicateToolStripMenuItem.Size = new System.Drawing.Size(233, 22);
			this.duplicateToolStripMenuItem.Text = "&Duplicate";
			// 
			// insertSeparatorToolStripMenuItem
			// 
			this.insertSeparatorToolStripMenuItem.Enabled = false;
			this.insertSeparatorToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.InsertSeparator;
			this.insertSeparatorToolStripMenuItem.Name = "insertSeparatorToolStripMenuItem";
			this.insertSeparatorToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.I)));
			this.insertSeparatorToolStripMenuItem.Size = new System.Drawing.Size(233, 22);
			this.insertSeparatorToolStripMenuItem.Text = "Insert Separator";
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(230, 6);
			// 
			// moveUpToolStripMenuItem
			// 
			this.moveUpToolStripMenuItem.Enabled = false;
			this.moveUpToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.MoveUp;
			this.moveUpToolStripMenuItem.Name = "moveUpToolStripMenuItem";
			this.moveUpToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.U)));
			this.moveUpToolStripMenuItem.Size = new System.Drawing.Size(233, 22);
			this.moveUpToolStripMenuItem.Text = "Move &Up";
			// 
			// moveDownToolStripMenuItem
			// 
			this.moveDownToolStripMenuItem.Enabled = false;
			this.moveDownToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.MoveDown;
			this.moveDownToolStripMenuItem.Name = "moveDownToolStripMenuItem";
			this.moveDownToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
			this.moveDownToolStripMenuItem.Size = new System.Drawing.Size(233, 22);
			this.moveDownToolStripMenuItem.Text = "Move &Down";
			// 
			// selectAllToolStripMenuItem
			// 
			this.selectAllToolStripMenuItem.Enabled = false;
			this.selectAllToolStripMenuItem.Name = "selectAllToolStripMenuItem";
			this.selectAllToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
			this.selectAllToolStripMenuItem.Size = new System.Drawing.Size(233, 22);
			this.selectAllToolStripMenuItem.Text = "Select &All";
			// 
			// toolStripSeparator6
			// 
			this.toolStripSeparator6.Name = "toolStripSeparator6";
			this.toolStripSeparator6.Size = new System.Drawing.Size(230, 6);
			// 
			// disableAllCheatsToolStripMenuItem
			// 
			this.disableAllCheatsToolStripMenuItem.Enabled = false;
			this.disableAllCheatsToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.Stop;
			this.disableAllCheatsToolStripMenuItem.Name = "disableAllCheatsToolStripMenuItem";
			this.disableAllCheatsToolStripMenuItem.Size = new System.Drawing.Size(233, 22);
			this.disableAllCheatsToolStripMenuItem.Text = "Disable all Cheats";
			// 
			// toolStripSeparator7
			// 
			this.toolStripSeparator7.Name = "toolStripSeparator7";
			this.toolStripSeparator7.Size = new System.Drawing.Size(230, 6);
			// 
			// openGameGenieEncoderDecoderToolStripMenuItem
			// 
			this.openGameGenieEncoderDecoderToolStripMenuItem.Enabled = false;
			this.openGameGenieEncoderDecoderToolStripMenuItem.Name = "openGameGenieEncoderDecoderToolStripMenuItem";
			this.openGameGenieEncoderDecoderToolStripMenuItem.Size = new System.Drawing.Size(233, 22);
			this.openGameGenieEncoderDecoderToolStripMenuItem.Text = "Game Genie Encoder/Decoder";
			// 
			// OptionsSubMenu
			// 
			this.OptionsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.LoadCheatFileByGameToolStripMenuItem,
            this.saveCheatsOnCloseToolStripMenuItem,
            this.CheatsOnOffLoadToolStripMenuItem,
            this.showValuesAsHexToolStripMenuItem,
            this.autoloadDialogToolStripMenuItem,
            this.saveWindowPositionToolStripMenuItem,
            this.toolStripSeparator5,
            this.restoreWindowSizeToolStripMenuItem});
			this.OptionsSubMenu.Name = "OptionsSubMenu";
			this.OptionsSubMenu.Size = new System.Drawing.Size(61, 20);
			this.OptionsSubMenu.Text = "&Options";
			// 
			// LoadCheatFileByGameToolStripMenuItem
			// 
			this.LoadCheatFileByGameToolStripMenuItem.Enabled = false;
			this.LoadCheatFileByGameToolStripMenuItem.Name = "LoadCheatFileByGameToolStripMenuItem";
			this.LoadCheatFileByGameToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
			this.LoadCheatFileByGameToolStripMenuItem.Text = "Load Cheat File by Game";
			// 
			// saveCheatsOnCloseToolStripMenuItem
			// 
			this.saveCheatsOnCloseToolStripMenuItem.Enabled = false;
			this.saveCheatsOnCloseToolStripMenuItem.Name = "saveCheatsOnCloseToolStripMenuItem";
			this.saveCheatsOnCloseToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
			this.saveCheatsOnCloseToolStripMenuItem.Text = "Save Cheats on Close";
			// 
			// CheatsOnOffLoadToolStripMenuItem
			// 
			this.CheatsOnOffLoadToolStripMenuItem.Enabled = false;
			this.CheatsOnOffLoadToolStripMenuItem.Name = "CheatsOnOffLoadToolStripMenuItem";
			this.CheatsOnOffLoadToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
			this.CheatsOnOffLoadToolStripMenuItem.Text = "Disable Cheats on Load";
			// 
			// showValuesAsHexToolStripMenuItem
			// 
			this.showValuesAsHexToolStripMenuItem.Enabled = false;
			this.showValuesAsHexToolStripMenuItem.Name = "showValuesAsHexToolStripMenuItem";
			this.showValuesAsHexToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
			this.showValuesAsHexToolStripMenuItem.Text = "Show Values as Hex";
			// 
			// autoloadDialogToolStripMenuItem
			// 
			this.autoloadDialogToolStripMenuItem.Enabled = false;
			this.autoloadDialogToolStripMenuItem.Name = "autoloadDialogToolStripMenuItem";
			this.autoloadDialogToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
			this.autoloadDialogToolStripMenuItem.Text = "Auto-load Dialog";
			// 
			// saveWindowPositionToolStripMenuItem
			// 
			this.saveWindowPositionToolStripMenuItem.Enabled = false;
			this.saveWindowPositionToolStripMenuItem.Name = "saveWindowPositionToolStripMenuItem";
			this.saveWindowPositionToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
			this.saveWindowPositionToolStripMenuItem.Text = "Save Window Position";
			// 
			// toolStripSeparator5
			// 
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			this.toolStripSeparator5.Size = new System.Drawing.Size(202, 6);
			// 
			// restoreWindowSizeToolStripMenuItem
			// 
			this.restoreWindowSizeToolStripMenuItem.Enabled = false;
			this.restoreWindowSizeToolStripMenuItem.Name = "restoreWindowSizeToolStripMenuItem";
			this.restoreWindowSizeToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
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
			// NumCheatsLabel
			// 
			this.NumCheatsLabel.AutoSize = true;
			this.NumCheatsLabel.Location = new System.Drawing.Point(9, 52);
			this.NumCheatsLabel.Name = "NumCheatsLabel";
			this.NumCheatsLabel.Size = new System.Drawing.Size(49, 13);
			this.NumCheatsLabel.TabIndex = 6;
			this.NumCheatsLabel.Text = "0 Cheats";
			// 
			// ColumnsSubMenu
			// 
			this.ColumnsSubMenu.Name = "ColumnsSubMenu";
			this.ColumnsSubMenu.Size = new System.Drawing.Size(67, 20);
			this.ColumnsSubMenu.Text = "&Columns";
			// 
			// NewCheatForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(587, 328);
			this.Controls.Add(this.NumCheatsLabel);
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
		private System.Windows.Forms.ToolStripMenuItem newToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem appendFileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem recentToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem CheatsSubMenu;
		private System.Windows.Forms.ToolStripMenuItem addCheatToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem removeCheatToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem duplicateToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem insertSeparatorToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripMenuItem moveUpToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem moveDownToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem selectAllToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
		private System.Windows.Forms.ToolStripMenuItem disableAllCheatsToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
		private System.Windows.Forms.ToolStripMenuItem openGameGenieEncoderDecoderToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem OptionsSubMenu;
		private System.Windows.Forms.ToolStripMenuItem LoadCheatFileByGameToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveCheatsOnCloseToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem CheatsOnOffLoadToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem showValuesAsHexToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem autoloadDialogToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveWindowPositionToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
		private System.Windows.Forms.ToolStripMenuItem restoreWindowSizeToolStripMenuItem;
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
		private System.Windows.Forms.Label NumCheatsLabel;
		private System.Windows.Forms.ToolStripMenuItem ColumnsSubMenu;
	}
}