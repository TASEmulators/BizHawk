using BizHawk.WinForms.Controls;

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
            this.HexMenuStrip = new BizHawk.WinForms.Controls.MenuStripEx();
            this.FileSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SaveMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SaveAsBinaryMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SaveAsTextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.importAsBinaryToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator4 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.LoadTableFileMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.CloseTableFileMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.RecentTablesSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.noneToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.EditMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.CopyMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.ExportMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.PasteMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator6 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.FindMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.FindNextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.FindPrevMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.OptionsSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.MemoryDomainsMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator3 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.DataSizeSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.DataSizeByteMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.DataSizeWordMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.DataSizeDWordMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.BigEndianMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator2 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.GoToAddressMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.AddToRamWatchMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.FreezeAddressMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.UnfreezeAllMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.PokeAddressMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SettingsSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.CustomColorsSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SetColorsMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator8 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.ResetColorsToDefaultMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator7 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.resetToDefaultToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.ViewerContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.CopyContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.ExportContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.PasteContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.FreezeContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.AddToRamWatchContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.UnfreezeAllContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.PokeContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.ContextSeparator1 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.IncrementContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.DecrementContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.ContextSeparator2 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.GoToContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripMenuItem1 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.viewN64MatrixToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.MemoryViewerBox = new System.Windows.Forms.GroupBox();
            this.HexScrollBar = new System.Windows.Forms.VScrollBar();
            this.AddressLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.AddressesLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.Header = new BizHawk.WinForms.Controls.LocLabelEx();
            this.HexMenuStrip.SuspendLayout();
            this.ViewerContextMenuStrip.SuspendLayout();
            this.MemoryViewerBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // HexMenuStrip
            // 
            resources.ApplyResources(this.HexMenuStrip, "HexMenuStrip");
            this.HexMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu,
            this.EditMenuItem,
            this.OptionsSubMenu,
            this.SettingsSubMenu});
            // 
            // FileSubMenu
            // 
            resources.ApplyResources(this.FileSubMenu, "FileSubMenu");
            this.FileSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SaveMenuItem,
            this.SaveAsBinaryMenuItem,
            this.SaveAsTextMenuItem,
            this.importAsBinaryToolStripMenuItem,
            this.toolStripSeparator4,
            this.LoadTableFileMenuItem,
            this.CloseTableFileMenuItem,
            this.RecentTablesSubMenu});
            this.FileSubMenu.DropDownOpened += new System.EventHandler(this.FileSubMenu_DropDownOpened);
            // 
            // SaveMenuItem
            // 
            resources.ApplyResources(this.SaveMenuItem, "SaveMenuItem");
            this.SaveMenuItem.Click += new System.EventHandler(this.SaveMenuItem_Click);
            // 
            // SaveAsBinaryMenuItem
            // 
            resources.ApplyResources(this.SaveAsBinaryMenuItem, "SaveAsBinaryMenuItem");
            this.SaveAsBinaryMenuItem.Click += new System.EventHandler(this.SaveAsBinaryMenuItem_Click);
            // 
            // SaveAsTextMenuItem
            // 
            resources.ApplyResources(this.SaveAsTextMenuItem, "SaveAsTextMenuItem");
            this.SaveAsTextMenuItem.Click += new System.EventHandler(this.SaveAsTextMenuItem_Click);
            // 
            // importAsBinaryToolStripMenuItem
            // 
            resources.ApplyResources(this.importAsBinaryToolStripMenuItem, "importAsBinaryToolStripMenuItem");
            this.importAsBinaryToolStripMenuItem.Click += new System.EventHandler(this.importAsBinaryToolStripMenuItem_Click);
            // 
            // toolStripSeparator4
            // 
            resources.ApplyResources(this.toolStripSeparator4, "toolStripSeparator4");
            // 
            // LoadTableFileMenuItem
            // 
            resources.ApplyResources(this.LoadTableFileMenuItem, "LoadTableFileMenuItem");
            this.LoadTableFileMenuItem.Click += new System.EventHandler(this.LoadTableFileMenuItem_Click);
            // 
            // CloseTableFileMenuItem
            // 
            resources.ApplyResources(this.CloseTableFileMenuItem, "CloseTableFileMenuItem");
            this.CloseTableFileMenuItem.Click += new System.EventHandler(this.CloseTableFileMenuItem_Click);
            // 
            // RecentTablesSubMenu
            // 
            resources.ApplyResources(this.RecentTablesSubMenu, "RecentTablesSubMenu");
            this.RecentTablesSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.noneToolStripMenuItem});
            this.RecentTablesSubMenu.DropDownOpened += new System.EventHandler(this.RecentTablesSubMenu_DropDownOpened);
            // 
            // noneToolStripMenuItem
            // 
            resources.ApplyResources(this.noneToolStripMenuItem, "noneToolStripMenuItem");
            // 
            // EditMenuItem
            // 
            resources.ApplyResources(this.EditMenuItem, "EditMenuItem");
            this.EditMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CopyMenuItem,
            this.ExportMenuItem,
            this.PasteMenuItem,
            this.toolStripSeparator6,
            this.FindMenuItem,
            this.FindNextMenuItem,
            this.FindPrevMenuItem});
            this.EditMenuItem.DropDownOpened += new System.EventHandler(this.EditMenuItem_DropDownOpened);
            // 
            // CopyMenuItem
            // 
            resources.ApplyResources(this.CopyMenuItem, "CopyMenuItem");
            this.CopyMenuItem.Click += new System.EventHandler(this.CopyMenuItem_Click);
            // 
            // ExportMenuItem
            // 
            resources.ApplyResources(this.ExportMenuItem, "ExportMenuItem");
            this.ExportMenuItem.Click += new System.EventHandler(this.ExportMenuItem_Click);
            // 
            // PasteMenuItem
            // 
            resources.ApplyResources(this.PasteMenuItem, "PasteMenuItem");
            this.PasteMenuItem.Click += new System.EventHandler(this.PasteMenuItem_Click);
            // 
            // toolStripSeparator6
            // 
            resources.ApplyResources(this.toolStripSeparator6, "toolStripSeparator6");
            // 
            // FindMenuItem
            // 
            resources.ApplyResources(this.FindMenuItem, "FindMenuItem");
            this.FindMenuItem.Click += new System.EventHandler(this.FindMenuItem_Click);
            // 
            // FindNextMenuItem
            // 
            resources.ApplyResources(this.FindNextMenuItem, "FindNextMenuItem");
            this.FindNextMenuItem.Click += new System.EventHandler(this.FindNextMenuItem_Click);
            // 
            // FindPrevMenuItem
            // 
            resources.ApplyResources(this.FindPrevMenuItem, "FindPrevMenuItem");
            this.FindPrevMenuItem.Click += new System.EventHandler(this.FindPrevMenuItem_Click);
            // 
            // OptionsSubMenu
            // 
            resources.ApplyResources(this.OptionsSubMenu, "OptionsSubMenu");
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
            this.OptionsSubMenu.DropDownOpened += new System.EventHandler(this.OptionsSubMenu_DropDownOpened);
            // 
            // MemoryDomainsMenuItem
            // 
            resources.ApplyResources(this.MemoryDomainsMenuItem, "MemoryDomainsMenuItem");
            this.MemoryDomainsMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator3});
            this.MemoryDomainsMenuItem.DropDownOpened += new System.EventHandler(this.MemoryDomainsMenuItem_DropDownOpened);
            // 
            // toolStripSeparator3
            // 
            resources.ApplyResources(this.toolStripSeparator3, "toolStripSeparator3");
            // 
            // DataSizeSubMenu
            // 
            resources.ApplyResources(this.DataSizeSubMenu, "DataSizeSubMenu");
            this.DataSizeSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.DataSizeByteMenuItem,
            this.DataSizeWordMenuItem,
            this.DataSizeDWordMenuItem});
            // 
            // DataSizeByteMenuItem
            // 
            resources.ApplyResources(this.DataSizeByteMenuItem, "DataSizeByteMenuItem");
            this.DataSizeByteMenuItem.Click += new System.EventHandler(this.DataSizeByteMenuItem_Click);
            // 
            // DataSizeWordMenuItem
            // 
            resources.ApplyResources(this.DataSizeWordMenuItem, "DataSizeWordMenuItem");
            this.DataSizeWordMenuItem.Click += new System.EventHandler(this.DataSizeWordMenuItem_Click);
            // 
            // DataSizeDWordMenuItem
            // 
            resources.ApplyResources(this.DataSizeDWordMenuItem, "DataSizeDWordMenuItem");
            this.DataSizeDWordMenuItem.Click += new System.EventHandler(this.DataSizeDWordMenuItem_Click);
            // 
            // BigEndianMenuItem
            // 
            resources.ApplyResources(this.BigEndianMenuItem, "BigEndianMenuItem");
            this.BigEndianMenuItem.Click += new System.EventHandler(this.BigEndianMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            resources.ApplyResources(this.toolStripSeparator2, "toolStripSeparator2");
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
            // FreezeAddressMenuItem
            // 
            resources.ApplyResources(this.FreezeAddressMenuItem, "FreezeAddressMenuItem");
            this.FreezeAddressMenuItem.Click += new System.EventHandler(this.FreezeAddressMenuItem_Click);
            // 
            // UnfreezeAllMenuItem
            // 
            resources.ApplyResources(this.UnfreezeAllMenuItem, "UnfreezeAllMenuItem");
            this.UnfreezeAllMenuItem.Click += new System.EventHandler(this.UnfreezeAllMenuItem_Click);
            // 
            // PokeAddressMenuItem
            // 
            resources.ApplyResources(this.PokeAddressMenuItem, "PokeAddressMenuItem");
            this.PokeAddressMenuItem.Click += new System.EventHandler(this.PokeAddressMenuItem_Click);
            // 
            // SettingsSubMenu
            // 
            resources.ApplyResources(this.SettingsSubMenu, "SettingsSubMenu");
            this.SettingsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CustomColorsSubMenu});
            // 
            // CustomColorsSubMenu
            // 
            resources.ApplyResources(this.CustomColorsSubMenu, "CustomColorsSubMenu");
            this.CustomColorsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SetColorsMenuItem,
            this.toolStripSeparator8,
            this.ResetColorsToDefaultMenuItem});
            // 
            // SetColorsMenuItem
            // 
            resources.ApplyResources(this.SetColorsMenuItem, "SetColorsMenuItem");
            this.SetColorsMenuItem.Click += new System.EventHandler(this.SetColorsMenuItem_Click);
            // 
            // toolStripSeparator8
            // 
            resources.ApplyResources(this.toolStripSeparator8, "toolStripSeparator8");
            // 
            // ResetColorsToDefaultMenuItem
            // 
            resources.ApplyResources(this.ResetColorsToDefaultMenuItem, "ResetColorsToDefaultMenuItem");
            this.ResetColorsToDefaultMenuItem.Click += new System.EventHandler(this.ResetColorsToDefaultMenuItem_Click);
            // 
            // toolStripSeparator7
            // 
            resources.ApplyResources(this.toolStripSeparator7, "toolStripSeparator7");
            // 
            // resetToDefaultToolStripMenuItem
            // 
            resources.ApplyResources(this.resetToDefaultToolStripMenuItem, "resetToDefaultToolStripMenuItem");
            // 
            // ViewerContextMenuStrip
            // 
            resources.ApplyResources(this.ViewerContextMenuStrip, "ViewerContextMenuStrip");
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
            this.ViewerContextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.ViewerContextMenuStrip_Opening);
            // 
            // CopyContextItem
            // 
            resources.ApplyResources(this.CopyContextItem, "CopyContextItem");
            this.CopyContextItem.Click += new System.EventHandler(this.CopyMenuItem_Click);
            // 
            // ExportContextItem
            // 
            resources.ApplyResources(this.ExportContextItem, "ExportContextItem");
            // 
            // PasteContextItem
            // 
            resources.ApplyResources(this.PasteContextItem, "PasteContextItem");
            this.PasteContextItem.Click += new System.EventHandler(this.PasteMenuItem_Click);
            // 
            // FreezeContextItem
            // 
            resources.ApplyResources(this.FreezeContextItem, "FreezeContextItem");
            this.FreezeContextItem.Click += new System.EventHandler(this.FreezeAddressMenuItem_Click);
            // 
            // AddToRamWatchContextItem
            // 
            resources.ApplyResources(this.AddToRamWatchContextItem, "AddToRamWatchContextItem");
            this.AddToRamWatchContextItem.Click += new System.EventHandler(this.AddToRamWatchMenuItem_Click);
            // 
            // UnfreezeAllContextItem
            // 
            resources.ApplyResources(this.UnfreezeAllContextItem, "UnfreezeAllContextItem");
            this.UnfreezeAllContextItem.Click += new System.EventHandler(this.UnfreezeAllMenuItem_Click);
            // 
            // PokeContextItem
            // 
            resources.ApplyResources(this.PokeContextItem, "PokeContextItem");
            this.PokeContextItem.Click += new System.EventHandler(this.PokeAddressMenuItem_Click);
            // 
            // ContextSeparator1
            // 
            resources.ApplyResources(this.ContextSeparator1, "ContextSeparator1");
            // 
            // IncrementContextItem
            // 
            resources.ApplyResources(this.IncrementContextItem, "IncrementContextItem");
            this.IncrementContextItem.Click += new System.EventHandler(this.IncrementContextItem_Click);
            // 
            // DecrementContextItem
            // 
            resources.ApplyResources(this.DecrementContextItem, "DecrementContextItem");
            this.DecrementContextItem.Click += new System.EventHandler(this.DecrementContextItem_Click);
            // 
            // ContextSeparator2
            // 
            resources.ApplyResources(this.ContextSeparator2, "ContextSeparator2");
            // 
            // GoToContextItem
            // 
            resources.ApplyResources(this.GoToContextItem, "GoToContextItem");
            this.GoToContextItem.Click += new System.EventHandler(this.GoToAddressMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            resources.ApplyResources(this.toolStripMenuItem1, "toolStripMenuItem1");
            // 
            // viewN64MatrixToolStripMenuItem
            // 
            resources.ApplyResources(this.viewN64MatrixToolStripMenuItem, "viewN64MatrixToolStripMenuItem");
            this.viewN64MatrixToolStripMenuItem.Click += new System.EventHandler(this.viewN64MatrixToolStripMenuItem_Click);
            // 
            // MemoryViewerBox
            // 
            resources.ApplyResources(this.MemoryViewerBox, "MemoryViewerBox");
            this.MemoryViewerBox.ContextMenuStrip = this.ViewerContextMenuStrip;
            this.MemoryViewerBox.Controls.Add(this.HexScrollBar);
            this.MemoryViewerBox.Controls.Add(this.AddressLabel);
            this.MemoryViewerBox.Controls.Add(this.AddressesLabel);
            this.MemoryViewerBox.Name = "MemoryViewerBox";
            this.MemoryViewerBox.TabStop = false;
            this.MemoryViewerBox.Paint += new System.Windows.Forms.PaintEventHandler(this.MemoryViewerBox_Paint);
            // 
            // HexScrollBar
            // 
            resources.ApplyResources(this.HexScrollBar, "HexScrollBar");
            this.HexScrollBar.LargeChange = 16;
            this.HexScrollBar.Name = "HexScrollBar";
            this.HexScrollBar.ValueChanged += new System.EventHandler(this.HexScrollBar_ValueChanged);
            // 
            // AddressLabel
            // 
            resources.ApplyResources(this.AddressLabel, "AddressLabel");
            this.AddressLabel.Name = "AddressLabel";
            // 
            // AddressesLabel
            // 
            resources.ApplyResources(this.AddressesLabel, "AddressesLabel");
            this.AddressesLabel.ContextMenuStrip = this.ViewerContextMenuStrip;
            this.AddressesLabel.Name = "AddressesLabel";
            this.AddressesLabel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.AddressesLabel_MouseDown);
            this.AddressesLabel.MouseLeave += new System.EventHandler(this.AddressesLabel_MouseLeave);
            this.AddressesLabel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.AddressesLabel_MouseMove);
            this.AddressesLabel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.AddressesLabel_MouseUp);
            // 
            // Header
            // 
            resources.ApplyResources(this.Header, "Header");
            this.Header.Name = "Header";
            // 
            // HexEditor
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.Header);
            this.Controls.Add(this.MemoryViewerBox);
            this.Controls.Add(this.HexMenuStrip);
            this.MainMenuStrip = this.HexMenuStrip;
            this.Name = "HexEditor";
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
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FileSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SaveAsTextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx OptionsSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx MemoryDomainsMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DataSizeSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DataSizeByteMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DataSizeWordMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DataSizeDWordMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx GoToAddressMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SettingsSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx BigEndianMenuItem;
		private System.Windows.Forms.ContextMenuStrip ViewerContextMenuStrip;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FreezeContextItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx AddToRamWatchContextItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator2;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx AddToRamWatchMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FreezeAddressMenuItem;
		public System.Windows.Forms.GroupBox MemoryViewerBox;
		private BizHawk.WinForms.Controls.LocLabelEx AddressesLabel;
		private System.Windows.Forms.VScrollBar HexScrollBar;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx UnfreezeAllMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx UnfreezeAllContextItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx ContextSeparator1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx IncrementContextItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DecrementContextItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx GoToContextItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx ContextSeparator2;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx EditMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CopyMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx PasteMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FindMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator6;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SaveAsBinaryMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator7;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx resetToDefaultToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CustomColorsSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SetColorsMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator8;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ResetColorsToDefaultMenuItem;
		public BizHawk.WinForms.Controls.LocLabelEx Header;
		private BizHawk.WinForms.Controls.LocLabelEx AddressLabel;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CopyContextItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx PasteContextItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FindNextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FindPrevMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SaveMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx PokeAddressMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx PokeContextItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator4;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx LoadTableFileMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RecentTablesSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx noneToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CloseTableFileMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator3;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripMenuItem1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx viewN64MatrixToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ExportContextItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ExportMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx importAsBinaryToolStripMenuItem;
	}
}