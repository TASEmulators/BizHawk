namespace BizHawk.Client.EmuHawk
{
	partial class HexEditor
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HexEditor));
			this.HexMenuStrip = new MenuStripEx();
			this.FileSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.SaveMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SaveAsBinaryMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SaveAsTextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.LoadTableFileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.CloseTableFileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.RecentTablesSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.noneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.ExitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.EditMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.CopyMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ExportMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.PasteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
			this.FindMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FindNextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FindPrevMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.OptionsSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.MemoryDomainsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.DataSizeSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.DataSizeByteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.DataSizeWordMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.DataSizeDWordMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.BigEndianMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.GoToAddressMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.AddToRamWatchMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FreezeAddressMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.UnfreezeAllMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.PokeAddressMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SettingsSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.CustomColorsSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.SetColorsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
			this.ResetColorsToDefaultMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
			this.resetToDefaultToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ViewerContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.CopyContextItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ExportContextItem = new System.Windows.Forms.ToolStripMenuItem();
			this.PasteContextItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FreezeContextItem = new System.Windows.Forms.ToolStripMenuItem();
			this.AddToRamWatchContextItem = new System.Windows.Forms.ToolStripMenuItem();
			this.UnfreezeAllContextItem = new System.Windows.Forms.ToolStripMenuItem();
			this.PokeContextItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ContextSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.IncrementContextItem = new System.Windows.Forms.ToolStripMenuItem();
			this.DecrementContextItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ContextSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.GoToContextItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			this.viewN64MatrixToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.MemoryViewerBox = new System.Windows.Forms.GroupBox();
			this.HexScrollBar = new System.Windows.Forms.VScrollBar();
			this.AddressLabel = new System.Windows.Forms.Label();
			this.AddressesLabel = new System.Windows.Forms.Label();
			this.Header = new System.Windows.Forms.Label();
			this.HexMenuStrip.SuspendLayout();
			this.ViewerContextMenuStrip.SuspendLayout();
			this.MemoryViewerBox.SuspendLayout();
			this.SuspendLayout();
			// 
			// HexMenuStrip
			// 
			this.HexMenuStrip.ClickThrough = true;
			this.HexMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu,
            this.EditMenuItem,
            this.OptionsSubMenu,
            this.SettingsSubMenu});
			this.HexMenuStrip.Location = new System.Drawing.Point(0, 0);
			this.HexMenuStrip.Name = "HexMenuStrip";
			this.HexMenuStrip.Size = new System.Drawing.Size(584, 24);
			this.HexMenuStrip.TabIndex = 1;
			this.HexMenuStrip.Text = "menuStrip1";
			this.HexMenuStrip.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.HexMenuStrip_ItemClicked);
			// 
			// FileSubMenu
			// 
			this.FileSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SaveMenuItem,
            this.SaveAsBinaryMenuItem,
            this.SaveAsTextMenuItem,
            this.toolStripSeparator4,
            this.LoadTableFileMenuItem,
            this.CloseTableFileMenuItem,
            this.RecentTablesSubMenu,
            this.toolStripSeparator1,
            this.ExitMenuItem});
			this.FileSubMenu.Name = "FileSubMenu";
			this.FileSubMenu.Size = new System.Drawing.Size(35, 20);
			this.FileSubMenu.Text = "&File";
			this.FileSubMenu.DropDownOpened += new System.EventHandler(this.FileSubMenu_DropDownOpened);
			// 
			// SaveMenuItem
			// 
			this.SaveMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.SaveAs;
			this.SaveMenuItem.Name = "SaveMenuItem";
			this.SaveMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			this.SaveMenuItem.Size = new System.Drawing.Size(236, 22);
			this.SaveMenuItem.Text = "Save";
			this.SaveMenuItem.Click += new System.EventHandler(this.SaveMenuItem_Click);
			// 
			// SaveAsBinaryMenuItem
			// 
			this.SaveAsBinaryMenuItem.Name = "SaveAsBinaryMenuItem";
			this.SaveAsBinaryMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.S)));
			this.SaveAsBinaryMenuItem.Size = new System.Drawing.Size(236, 22);
			this.SaveAsBinaryMenuItem.Text = "Save as binary...";
			this.SaveAsBinaryMenuItem.Click += new System.EventHandler(this.SaveAsBinaryMenuItem_Click);
			// 
			// SaveAsTextMenuItem
			// 
			this.SaveAsTextMenuItem.Name = "SaveAsTextMenuItem";
			this.SaveAsTextMenuItem.Size = new System.Drawing.Size(236, 22);
			this.SaveAsTextMenuItem.Text = "Save as text...";
			this.SaveAsTextMenuItem.Click += new System.EventHandler(this.SaveAsTextMenuItem_Click);
			// 
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size(233, 6);
			// 
			// LoadTableFileMenuItem
			// 
			this.LoadTableFileMenuItem.Name = "LoadTableFileMenuItem";
			this.LoadTableFileMenuItem.Size = new System.Drawing.Size(236, 22);
			this.LoadTableFileMenuItem.Text = "&Load .tbl file";
			this.LoadTableFileMenuItem.Click += new System.EventHandler(this.LoadTableFileMenuItem_Click);
			// 
			// CloseTableFileMenuItem
			// 
			this.CloseTableFileMenuItem.Name = "CloseTableFileMenuItem";
			this.CloseTableFileMenuItem.Size = new System.Drawing.Size(236, 22);
			this.CloseTableFileMenuItem.Text = "Close .tbl file";
			this.CloseTableFileMenuItem.Click += new System.EventHandler(this.CloseTableFileMenuItem_Click);
			// 
			// RecentTablesSubMenu
			// 
			this.RecentTablesSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.noneToolStripMenuItem});
			this.RecentTablesSubMenu.Name = "RecentTablesSubMenu";
			this.RecentTablesSubMenu.Size = new System.Drawing.Size(236, 22);
			this.RecentTablesSubMenu.Text = "Recent";
			this.RecentTablesSubMenu.DropDownOpened += new System.EventHandler(this.RecentTablesSubMenu_DropDownOpened);
			// 
			// noneToolStripMenuItem
			// 
			this.noneToolStripMenuItem.Name = "noneToolStripMenuItem";
			this.noneToolStripMenuItem.Size = new System.Drawing.Size(110, 22);
			this.noneToolStripMenuItem.Text = "None";
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(233, 6);
			// 
			// ExitMenuItem
			// 
			this.ExitMenuItem.Name = "ExitMenuItem";
			this.ExitMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
			this.ExitMenuItem.Size = new System.Drawing.Size(236, 22);
			this.ExitMenuItem.Text = "E&xit";
			this.ExitMenuItem.Click += new System.EventHandler(this.ExitMenuItem_Click);
			// 
			// EditMenuItem
			// 
			this.EditMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CopyMenuItem,
            this.ExportMenuItem,
            this.PasteMenuItem,
            this.toolStripSeparator6,
            this.FindMenuItem,
            this.FindNextMenuItem,
            this.FindPrevMenuItem});
			this.EditMenuItem.Name = "EditMenuItem";
			this.EditMenuItem.Size = new System.Drawing.Size(37, 20);
			this.EditMenuItem.Text = "&Edit";
			this.EditMenuItem.DropDownOpened += new System.EventHandler(this.EditMenuItem_DropDownOpened);
			// 
			// CopyMenuItem
			// 
			this.CopyMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Duplicate;
			this.CopyMenuItem.Name = "CopyMenuItem";
			this.CopyMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
			this.CopyMenuItem.Size = new System.Drawing.Size(155, 22);
			this.CopyMenuItem.Text = "&Copy";
			this.CopyMenuItem.Click += new System.EventHandler(this.CopyMenuItem_Click);
			// 
			// ExportMenuItem
			// 
			this.ExportMenuItem.Name = "ExportMenuItem";
			this.ExportMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E)));
			this.ExportMenuItem.Size = new System.Drawing.Size(155, 22);
			this.ExportMenuItem.Text = "&Export";
			this.ExportMenuItem.Click += new System.EventHandler(this.ExportMenuItem_Click);
			// 
			// PasteMenuItem
			// 
			this.PasteMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Paste;
			this.PasteMenuItem.Name = "PasteMenuItem";
			this.PasteMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.V)));
			this.PasteMenuItem.Size = new System.Drawing.Size(155, 22);
			this.PasteMenuItem.Text = "&Paste";
			this.PasteMenuItem.Click += new System.EventHandler(this.PasteMenuItem_Click);
			// 
			// toolStripSeparator6
			// 
			this.toolStripSeparator6.Name = "toolStripSeparator6";
			this.toolStripSeparator6.Size = new System.Drawing.Size(152, 6);
			// 
			// FindMenuItem
			// 
			this.FindMenuItem.Name = "FindMenuItem";
			this.FindMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F)));
			this.FindMenuItem.Size = new System.Drawing.Size(155, 22);
			this.FindMenuItem.Text = "&Find...";
			this.FindMenuItem.Click += new System.EventHandler(this.FindMenuItem_Click);
			// 
			// FindNextMenuItem
			// 
			this.FindNextMenuItem.Name = "FindNextMenuItem";
			this.FindNextMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F3;
			this.FindNextMenuItem.Size = new System.Drawing.Size(155, 22);
			this.FindNextMenuItem.Text = "Find Next";
			this.FindNextMenuItem.Click += new System.EventHandler(this.FindNextMenuItem_Click);
			// 
			// FindPrevMenuItem
			// 
			this.FindPrevMenuItem.Name = "FindPrevMenuItem";
			this.FindPrevMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F2;
			this.FindPrevMenuItem.Size = new System.Drawing.Size(155, 22);
			this.FindPrevMenuItem.Text = "Find Prev";
			this.FindPrevMenuItem.Click += new System.EventHandler(this.FindPrevMenuItem_Click);
			// 
			// OptionsSubMenu
			// 
			this.OptionsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MemoryDomainsMenuItem,
            this.DataSizeSubMenu,
            this.BigEndianMenuItem,
            this.toolStripSeparator2,
            this.GoToAddressMenuItem,
            this.AddToRamWatchMenuItem,
            this.FreezeAddressMenuItem,
            this.UnfreezeAllMenuItem,
            this.PokeAddressMenuItem});
			this.OptionsSubMenu.Name = "OptionsSubMenu";
			this.OptionsSubMenu.Size = new System.Drawing.Size(56, 20);
			this.OptionsSubMenu.Text = "&Options";
			this.OptionsSubMenu.DropDownOpened += new System.EventHandler(this.OptionsSubMenu_DropDownOpened);
			// 
			// MemoryDomainsMenuItem
			// 
			this.MemoryDomainsMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator3});
			this.MemoryDomainsMenuItem.Name = "MemoryDomainsMenuItem";
			this.MemoryDomainsMenuItem.Size = new System.Drawing.Size(217, 22);
			this.MemoryDomainsMenuItem.Text = "&Memory Domains";
			this.MemoryDomainsMenuItem.DropDownOpened += new System.EventHandler(this.MemoryDomainsMenuItem_DropDownOpened);
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(57, 6);
			// 
			// DataSizeSubMenu
			// 
			this.DataSizeSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.DataSizeByteMenuItem,
            this.DataSizeWordMenuItem,
            this.DataSizeDWordMenuItem});
			this.DataSizeSubMenu.Name = "DataSizeSubMenu";
			this.DataSizeSubMenu.Size = new System.Drawing.Size(217, 22);
			this.DataSizeSubMenu.Text = "Data Size";
			// 
			// DataSizeByteMenuItem
			// 
			this.DataSizeByteMenuItem.Name = "DataSizeByteMenuItem";
			this.DataSizeByteMenuItem.Size = new System.Drawing.Size(116, 22);
			this.DataSizeByteMenuItem.Text = "1 Byte";
			this.DataSizeByteMenuItem.Click += new System.EventHandler(this.DataSizeByteMenuItem_Click);
			// 
			// DataSizeWordMenuItem
			// 
			this.DataSizeWordMenuItem.Name = "DataSizeWordMenuItem";
			this.DataSizeWordMenuItem.Size = new System.Drawing.Size(116, 22);
			this.DataSizeWordMenuItem.Text = "2 Byte";
			this.DataSizeWordMenuItem.Click += new System.EventHandler(this.DataSizeWordMenuItem_Click);
			// 
			// DataSizeDWordMenuItem
			// 
			this.DataSizeDWordMenuItem.Name = "DataSizeDWordMenuItem";
			this.DataSizeDWordMenuItem.Size = new System.Drawing.Size(116, 22);
			this.DataSizeDWordMenuItem.Text = "4 Byte";
			this.DataSizeDWordMenuItem.Click += new System.EventHandler(this.DataSizeDWordMenuItem_Click);
			// 
			// BigEndianMenuItem
			// 
			this.BigEndianMenuItem.Name = "BigEndianMenuItem";
			this.BigEndianMenuItem.Size = new System.Drawing.Size(217, 22);
			this.BigEndianMenuItem.Text = "Big Endian";
			this.BigEndianMenuItem.Click += new System.EventHandler(this.BigEndianMenuItem_Click);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(214, 6);
			// 
			// GoToAddressMenuItem
			// 
			this.GoToAddressMenuItem.Name = "GoToAddressMenuItem";
			this.GoToAddressMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.G)));
			this.GoToAddressMenuItem.Size = new System.Drawing.Size(217, 22);
			this.GoToAddressMenuItem.Text = "&Go to Address...";
			this.GoToAddressMenuItem.Click += new System.EventHandler(this.GoToAddressMenuItem_Click);
			// 
			// AddToRamWatchMenuItem
			// 
			this.AddToRamWatchMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.FindHS;
			this.AddToRamWatchMenuItem.Name = "AddToRamWatchMenuItem";
			this.AddToRamWatchMenuItem.ShortcutKeyDisplayString = "Ctrl+W";
			this.AddToRamWatchMenuItem.Size = new System.Drawing.Size(217, 22);
			this.AddToRamWatchMenuItem.Text = "Add to RAM Watch";
			this.AddToRamWatchMenuItem.Click += new System.EventHandler(this.AddToRamWatchMenuItem_Click);
			// 
			// FreezeAddressMenuItem
			// 
			this.FreezeAddressMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Freeze;
			this.FreezeAddressMenuItem.Name = "FreezeAddressMenuItem";
			this.FreezeAddressMenuItem.ShortcutKeyDisplayString = "Space";
			this.FreezeAddressMenuItem.Size = new System.Drawing.Size(217, 22);
			this.FreezeAddressMenuItem.Text = "&Freeze Address";
			this.FreezeAddressMenuItem.Click += new System.EventHandler(this.FreezeAddressMenuItem_Click);
			// 
			// UnfreezeAllMenuItem
			// 
			this.UnfreezeAllMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Unfreeze;
			this.UnfreezeAllMenuItem.Name = "UnfreezeAllMenuItem";
			this.UnfreezeAllMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.Delete)));
			this.UnfreezeAllMenuItem.Size = new System.Drawing.Size(217, 22);
			this.UnfreezeAllMenuItem.Text = "Unfreeze All";
			this.UnfreezeAllMenuItem.Click += new System.EventHandler(this.UnfreezeAllMenuItem_Click);
			// 
			// PokeAddressMenuItem
			// 
			this.PokeAddressMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.poke;
			this.PokeAddressMenuItem.Name = "PokeAddressMenuItem";
			this.PokeAddressMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.P)));
			this.PokeAddressMenuItem.Size = new System.Drawing.Size(217, 22);
			this.PokeAddressMenuItem.Text = "&Poke Address";
			this.PokeAddressMenuItem.Click += new System.EventHandler(this.PokeAddressMenuItem_Click);
			// 
			// SettingsSubMenu
			// 
			this.SettingsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CustomColorsSubMenu});
			this.SettingsSubMenu.Name = "SettingsSubMenu";
			this.SettingsSubMenu.Size = new System.Drawing.Size(58, 20);
			this.SettingsSubMenu.Text = "&Settings";
			// 
			// CustomColorsSubMenu
			// 
			this.CustomColorsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SetColorsMenuItem,
            this.toolStripSeparator8,
            this.ResetColorsToDefaultMenuItem});
			this.CustomColorsSubMenu.Name = "CustomColorsSubMenu";
			this.CustomColorsSubMenu.Size = new System.Drawing.Size(154, 22);
			this.CustomColorsSubMenu.Text = "Custom Colors";
			// 
			// SetColorsMenuItem
			// 
			this.SetColorsMenuItem.Name = "SetColorsMenuItem";
			this.SetColorsMenuItem.Size = new System.Drawing.Size(164, 22);
			this.SetColorsMenuItem.Text = "Set Colors";
			this.SetColorsMenuItem.Click += new System.EventHandler(this.SetColorsMenuItem_Click);
			// 
			// toolStripSeparator8
			// 
			this.toolStripSeparator8.Name = "toolStripSeparator8";
			this.toolStripSeparator8.Size = new System.Drawing.Size(161, 6);
			// 
			// ResetColorsToDefaultMenuItem
			// 
			this.ResetColorsToDefaultMenuItem.Name = "ResetColorsToDefaultMenuItem";
			this.ResetColorsToDefaultMenuItem.Size = new System.Drawing.Size(164, 22);
			this.ResetColorsToDefaultMenuItem.Text = "Reset to Default";
			this.ResetColorsToDefaultMenuItem.Click += new System.EventHandler(this.ResetColorsToDefaultMenuItem_Click);
			// 
			// toolStripSeparator7
			// 
			this.toolStripSeparator7.Name = "toolStripSeparator7";
			this.toolStripSeparator7.Size = new System.Drawing.Size(154, 6);
			// 
			// resetToDefaultToolStripMenuItem
			// 
			this.resetToDefaultToolStripMenuItem.Name = "resetToDefaultToolStripMenuItem";
			this.resetToDefaultToolStripMenuItem.Size = new System.Drawing.Size(157, 22);
			this.resetToDefaultToolStripMenuItem.Text = "Reset to Default";
			// 
			// ViewerContextMenuStrip
			// 
			this.ViewerContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CopyContextItem,
            this.ExportContextItem,
            this.PasteContextItem,
            this.FreezeContextItem,
            this.AddToRamWatchContextItem,
            this.UnfreezeAllContextItem,
            this.PokeContextItem,
            this.ContextSeparator1,
            this.IncrementContextItem,
            this.DecrementContextItem,
            this.ContextSeparator2,
            this.GoToContextItem,
            this.toolStripMenuItem1,
            this.viewN64MatrixToolStripMenuItem});
			this.ViewerContextMenuStrip.Name = "ViewerContextMenuStrip";
			this.ViewerContextMenuStrip.Size = new System.Drawing.Size(207, 264);
			this.ViewerContextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.ViewerContextMenuStrip_Opening);
			// 
			// CopyContextItem
			// 
			this.CopyContextItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Duplicate;
			this.CopyContextItem.Name = "CopyContextItem";
			this.CopyContextItem.ShortcutKeyDisplayString = "Ctrl+C";
			this.CopyContextItem.Size = new System.Drawing.Size(206, 22);
			this.CopyContextItem.Text = "&Copy";
			this.CopyContextItem.Click += new System.EventHandler(this.CopyMenuItem_Click);
			// 
			// ExportContextItem
			// 
			this.ExportContextItem.Name = "ExportContextItem";
			this.ExportContextItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E)));
			this.ExportContextItem.Size = new System.Drawing.Size(206, 22);
			this.ExportContextItem.Text = "&Export";
			// 
			// PasteContextItem
			// 
			this.PasteContextItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Paste;
			this.PasteContextItem.Name = "PasteContextItem";
			this.PasteContextItem.ShortcutKeyDisplayString = "Ctrl+V";
			this.PasteContextItem.Size = new System.Drawing.Size(206, 22);
			this.PasteContextItem.Text = "&Paste";
			this.PasteContextItem.Click += new System.EventHandler(this.PasteMenuItem_Click);
			// 
			// FreezeContextItem
			// 
			this.FreezeContextItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Freeze;
			this.FreezeContextItem.Name = "FreezeContextItem";
			this.FreezeContextItem.ShortcutKeyDisplayString = "Space";
			this.FreezeContextItem.Size = new System.Drawing.Size(206, 22);
			this.FreezeContextItem.Text = "&Freeze";
			this.FreezeContextItem.Click += new System.EventHandler(this.FreezeAddressMenuItem_Click);
			// 
			// AddToRamWatchContextItem
			// 
			this.AddToRamWatchContextItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.FindHS;
			this.AddToRamWatchContextItem.Name = "AddToRamWatchContextItem";
			this.AddToRamWatchContextItem.ShortcutKeyDisplayString = "Ctrl+W";
			this.AddToRamWatchContextItem.Size = new System.Drawing.Size(206, 22);
			this.AddToRamWatchContextItem.Text = "&Add to RAM Watch";
			this.AddToRamWatchContextItem.Click += new System.EventHandler(this.AddToRamWatchMenuItem_Click);
			// 
			// UnfreezeAllContextItem
			// 
			this.UnfreezeAllContextItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Unfreeze;
			this.UnfreezeAllContextItem.Name = "UnfreezeAllContextItem";
			this.UnfreezeAllContextItem.ShortcutKeyDisplayString = "Shift+Del";
			this.UnfreezeAllContextItem.Size = new System.Drawing.Size(206, 22);
			this.UnfreezeAllContextItem.Text = "&Unfreeze All";
			this.UnfreezeAllContextItem.Click += new System.EventHandler(this.UnfreezeAllMenuItem_Click);
			// 
			// PokeContextItem
			// 
			this.PokeContextItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.poke;
			this.PokeContextItem.Name = "PokeContextItem";
			this.PokeContextItem.ShortcutKeyDisplayString = "Ctrl+P";
			this.PokeContextItem.Size = new System.Drawing.Size(206, 22);
			this.PokeContextItem.Text = "&Poke Address";
			this.PokeContextItem.Click += new System.EventHandler(this.PokeAddressMenuItem_Click);
			// 
			// ContextSeparator1
			// 
			this.ContextSeparator1.Name = "ContextSeparator1";
			this.ContextSeparator1.Size = new System.Drawing.Size(203, 6);
			// 
			// IncrementContextItem
			// 
			this.IncrementContextItem.Name = "IncrementContextItem";
			this.IncrementContextItem.ShortcutKeyDisplayString = "+";
			this.IncrementContextItem.Size = new System.Drawing.Size(206, 22);
			this.IncrementContextItem.Text = "&Increment";
			this.IncrementContextItem.Click += new System.EventHandler(this.IncrementContextItem_Click);
			// 
			// DecrementContextItem
			// 
			this.DecrementContextItem.Name = "DecrementContextItem";
			this.DecrementContextItem.ShortcutKeyDisplayString = "-";
			this.DecrementContextItem.Size = new System.Drawing.Size(206, 22);
			this.DecrementContextItem.Text = "&Decrement";
			this.DecrementContextItem.Click += new System.EventHandler(this.DecrementContextItem_Click);
			// 
			// ContextSeparator2
			// 
			this.ContextSeparator2.Name = "ContextSeparator2";
			this.ContextSeparator2.Size = new System.Drawing.Size(203, 6);
			// 
			// GoToContextItem
			// 
			this.GoToContextItem.Name = "GoToContextItem";
			this.GoToContextItem.ShortcutKeyDisplayString = "Ctrl+G";
			this.GoToContextItem.Size = new System.Drawing.Size(206, 22);
			this.GoToContextItem.Text = "&Go to Address...";
			this.GoToContextItem.Click += new System.EventHandler(this.GoToAddressMenuItem_Click);
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(203, 6);
			// 
			// viewN64MatrixToolStripMenuItem
			// 
			this.viewN64MatrixToolStripMenuItem.Name = "viewN64MatrixToolStripMenuItem";
			this.viewN64MatrixToolStripMenuItem.Size = new System.Drawing.Size(206, 22);
			this.viewN64MatrixToolStripMenuItem.Text = "View N64 Matrix";
			this.viewN64MatrixToolStripMenuItem.Click += new System.EventHandler(this.viewN64MatrixToolStripMenuItem_Click);
			// 
			// MemoryViewerBox
			// 
			this.MemoryViewerBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.MemoryViewerBox.ContextMenuStrip = this.ViewerContextMenuStrip;
			this.MemoryViewerBox.Controls.Add(this.HexScrollBar);
			this.MemoryViewerBox.Controls.Add(this.AddressLabel);
			this.MemoryViewerBox.Controls.Add(this.AddressesLabel);
			this.MemoryViewerBox.Controls.Add(this.Header);
			this.MemoryViewerBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.MemoryViewerBox.Location = new System.Drawing.Point(12, 27);
			this.MemoryViewerBox.MaximumSize = new System.Drawing.Size(600, 1024);
			this.MemoryViewerBox.MinimumSize = new System.Drawing.Size(260, 180);
			this.MemoryViewerBox.Name = "MemoryViewerBox";
			this.MemoryViewerBox.Size = new System.Drawing.Size(560, 262);
			this.MemoryViewerBox.TabIndex = 2;
			this.MemoryViewerBox.TabStop = false;
			this.MemoryViewerBox.Paint += new System.Windows.Forms.PaintEventHandler(this.MemoryViewerBox_Paint);
			// 
			// HexScrollBar
			// 
			this.HexScrollBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.HexScrollBar.LargeChange = 16;
			this.HexScrollBar.Location = new System.Drawing.Point(544, 16);
			this.HexScrollBar.Name = "HexScrollBar";
			this.HexScrollBar.Size = new System.Drawing.Size(16, 246);
			this.HexScrollBar.TabIndex = 1;
			this.HexScrollBar.ValueChanged += new System.EventHandler(this.HexScrollBar_ValueChanged);
			// 
			// AddressLabel
			// 
			this.AddressLabel.AutoSize = true;
			this.AddressLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.AddressLabel.Location = new System.Drawing.Point(3, 30);
			this.AddressLabel.Name = "AddressLabel";
			this.AddressLabel.Size = new System.Drawing.Size(25, 13);
			this.AddressLabel.TabIndex = 2;
			this.AddressLabel.Text = "      ";
			// 
			// AddressesLabel
			// 
			this.AddressesLabel.AutoSize = true;
			this.AddressesLabel.ContextMenuStrip = this.ViewerContextMenuStrip;
			this.AddressesLabel.Location = new System.Drawing.Point(79, 30);
			this.AddressesLabel.Name = "AddressesLabel";
			this.AddressesLabel.Size = new System.Drawing.Size(31, 13);
			this.AddressesLabel.TabIndex = 0;
			this.AddressesLabel.Text = "RAM";
			this.AddressesLabel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.AddressesLabel_MouseDown);
			this.AddressesLabel.MouseLeave += new System.EventHandler(this.AddressesLabel_MouseLeave);
			this.AddressesLabel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.AddressesLabel_MouseMove);
			this.AddressesLabel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.AddressesLabel_MouseUp);
			// 
			// Header
			// 
			this.Header.AutoSize = true;
			this.Header.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Header.Location = new System.Drawing.Point(28, 44);
			this.Header.Name = "Header";
			this.Header.Size = new System.Drawing.Size(35, 13);
			this.Header.TabIndex = 2;
			this.Header.Text = "label1";
			// 
			// HexEditor
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(584, 301);
			this.Controls.Add(this.Header);
			this.Controls.Add(this.MemoryViewerBox);
			this.Controls.Add(this.HexMenuStrip);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.HexMenuStrip;
			this.MinimumSize = new System.Drawing.Size(360, 180);
			this.Name = "HexEditor";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Hex Editor";
			this.Load += new System.EventHandler(this.HexEditor_Load);
			this.ResizeEnd += new System.EventHandler(this.HexEditor_ResizeEnd);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.HexEditor_KeyDown);
			this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.HexEditor_KeyPress);
			this.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.HexEditor_MouseWheel);
			this.Resize += new System.EventHandler(this.HexEditor_Resize);
			this.HexMenuStrip.ResumeLayout(false);
			this.HexMenuStrip.PerformLayout();
			this.ViewerContextMenuStrip.ResumeLayout(false);
			this.MemoryViewerBox.ResumeLayout(false);
			this.MemoryViewerBox.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		public MenuStripEx HexMenuStrip;
		private System.Windows.Forms.ToolStripMenuItem FileSubMenu;
		private System.Windows.Forms.ToolStripMenuItem SaveAsTextMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem ExitMenuItem;
		private System.Windows.Forms.ToolStripMenuItem OptionsSubMenu;
		private System.Windows.Forms.ToolStripMenuItem MemoryDomainsMenuItem;
		private System.Windows.Forms.ToolStripMenuItem DataSizeSubMenu;
		private System.Windows.Forms.ToolStripMenuItem DataSizeByteMenuItem;
		private System.Windows.Forms.ToolStripMenuItem DataSizeWordMenuItem;
		private System.Windows.Forms.ToolStripMenuItem DataSizeDWordMenuItem;
		private System.Windows.Forms.ToolStripMenuItem GoToAddressMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SettingsSubMenu;
		private System.Windows.Forms.ToolStripMenuItem BigEndianMenuItem;
		private System.Windows.Forms.ContextMenuStrip ViewerContextMenuStrip;
		private System.Windows.Forms.ToolStripMenuItem FreezeContextItem;
		private System.Windows.Forms.ToolStripMenuItem AddToRamWatchContextItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripMenuItem AddToRamWatchMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FreezeAddressMenuItem;
		public System.Windows.Forms.GroupBox MemoryViewerBox;
		private System.Windows.Forms.Label AddressesLabel;
		private System.Windows.Forms.VScrollBar HexScrollBar;
		private System.Windows.Forms.ToolStripMenuItem UnfreezeAllMenuItem;
		private System.Windows.Forms.ToolStripMenuItem UnfreezeAllContextItem;
		private System.Windows.Forms.ToolStripSeparator ContextSeparator1;
		private System.Windows.Forms.ToolStripMenuItem IncrementContextItem;
		private System.Windows.Forms.ToolStripMenuItem DecrementContextItem;
		private System.Windows.Forms.ToolStripMenuItem GoToContextItem;
		private System.Windows.Forms.ToolStripSeparator ContextSeparator2;
		private System.Windows.Forms.ToolStripMenuItem EditMenuItem;
		private System.Windows.Forms.ToolStripMenuItem CopyMenuItem;
		private System.Windows.Forms.ToolStripMenuItem PasteMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FindMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
		private System.Windows.Forms.ToolStripMenuItem SaveAsBinaryMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
		private System.Windows.Forms.ToolStripMenuItem resetToDefaultToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem CustomColorsSubMenu;
		private System.Windows.Forms.ToolStripMenuItem SetColorsMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
		private System.Windows.Forms.ToolStripMenuItem ResetColorsToDefaultMenuItem;
		public System.Windows.Forms.Label Header;
		private System.Windows.Forms.Label AddressLabel;
		private System.Windows.Forms.ToolStripMenuItem CopyContextItem;
		private System.Windows.Forms.ToolStripMenuItem PasteContextItem;
		private System.Windows.Forms.ToolStripMenuItem FindNextMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FindPrevMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SaveMenuItem;
		private System.Windows.Forms.ToolStripMenuItem PokeAddressMenuItem;
		private System.Windows.Forms.ToolStripMenuItem PokeContextItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.ToolStripMenuItem LoadTableFileMenuItem;
		private System.Windows.Forms.ToolStripMenuItem RecentTablesSubMenu;
		private System.Windows.Forms.ToolStripMenuItem noneToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem CloseTableFileMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem viewN64MatrixToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem ExportContextItem;
		private System.Windows.Forms.ToolStripMenuItem ExportMenuItem;
	}
}