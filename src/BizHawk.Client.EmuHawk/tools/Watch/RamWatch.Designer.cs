using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	partial class RamWatch
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RamWatch));
            this.WatchCountLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.ListViewContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.newToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.EditContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.RemoveContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.DuplicateContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SplitContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.PokeContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.FreezeContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.UnfreezeAllContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.ViewInHexEditorContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.Separator4 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.ReadBreakpointContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.WriteBreakpointContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.Separator6 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.InsertSeperatorContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.MoveUpContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.MoveDownContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.MoveTopContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.MoveBottomContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.statusStrip1 = new BizHawk.WinForms.Controls.StatusStripEx();
            this.ErrorIconButton = new System.Windows.Forms.ToolStripButton();
            this.MessageLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStrip1 = new BizHawk.WinForms.Controls.ToolStripEx();
            this.newToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.openToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.saveToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.newWatchToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.editWatchToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.cutToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.clearChangeCountsToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.duplicateWatchToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.SplitWatchToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.PokeAddressToolBarItem = new System.Windows.Forms.ToolStripButton();
            this.FreezeAddressToolBarItem = new System.Windows.Forms.ToolStripButton();
            this.seperatorToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator6 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.moveUpToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.moveDownToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator5 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.RamWatchMenu = new BizHawk.WinForms.Controls.MenuStripEx();
            this.FileSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.NewListMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.OpenMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SaveMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SaveAsMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.AppendMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.RecentSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.noneToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.WatchesSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.MemoryDomainsSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.Separator2 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.toolStripSeparator8 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.NewWatchMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.EditWatchMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.RemoveWatchMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.DuplicateWatchMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SplitWatchMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.PokeAddressMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.FreezeAddressMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.InsertSeparatorMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.ClearChangeCountsMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator3 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.MoveUpMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.MoveDownMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.MoveTopMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.MoveBottomMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SelectAllMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.OptionsSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.DefinePreviousValueSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.PreviousFrameMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.LastChangeMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.OriginalMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.WatchesOnScreenMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.DoubleClickActionSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.DoubleClickToEditMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.DoubleClickToPokeMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.WatchListView = new BizHawk.Client.EmuHawk.InputRoll();
            this.ListViewContextMenu.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.RamWatchMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // WatchCountLabel
            // 
            resources.ApplyResources(this.WatchCountLabel, "WatchCountLabel");
            this.WatchCountLabel.Name = "WatchCountLabel";
            // 
            // ListViewContextMenu
            // 
            resources.ApplyResources(this.ListViewContextMenu, "ListViewContextMenu");
            this.ListViewContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripMenuItem,
            this.EditContextMenuItem,
            this.RemoveContextMenuItem,
            this.DuplicateContextMenuItem,
            this.SplitContextMenuItem,
            this.PokeContextMenuItem,
            this.FreezeContextMenuItem,
            this.UnfreezeAllContextMenuItem,
            this.ViewInHexEditorContextMenuItem,
            this.Separator4,
            this.ReadBreakpointContextMenuItem,
            this.WriteBreakpointContextMenuItem,
            this.Separator6,
            this.InsertSeperatorContextMenuItem,
            this.MoveUpContextMenuItem,
            this.MoveDownContextMenuItem,
            this.MoveTopContextMenuItem,
            this.MoveBottomContextMenuItem});
            this.ListViewContextMenu.Name = "contextMenuStrip1";
            this.ListViewContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.ListViewContextMenu_Opening);
            // 
            // newToolStripMenuItem
            // 
            resources.ApplyResources(this.newToolStripMenuItem, "newToolStripMenuItem");
            this.newToolStripMenuItem.Click += new System.EventHandler(this.NewWatchMenuItem_Click);
            // 
            // EditContextMenuItem
            // 
            resources.ApplyResources(this.EditContextMenuItem, "EditContextMenuItem");
            this.EditContextMenuItem.Click += new System.EventHandler(this.EditWatchMenuItem_Click);
            // 
            // RemoveContextMenuItem
            // 
            resources.ApplyResources(this.RemoveContextMenuItem, "RemoveContextMenuItem");
            this.RemoveContextMenuItem.Click += new System.EventHandler(this.RemoveWatchMenuItem_Click);
            // 
            // DuplicateContextMenuItem
            // 
            resources.ApplyResources(this.DuplicateContextMenuItem, "DuplicateContextMenuItem");
            this.DuplicateContextMenuItem.Click += new System.EventHandler(this.DuplicateWatchMenuItem_Click);
            // 
            // SplitContextMenuItem
            // 
            resources.ApplyResources(this.SplitContextMenuItem, "SplitContextMenuItem");
            this.SplitContextMenuItem.Click += new System.EventHandler(this.SplitWatchMenuItem_Click);
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
            // ViewInHexEditorContextMenuItem
            // 
            resources.ApplyResources(this.ViewInHexEditorContextMenuItem, "ViewInHexEditorContextMenuItem");
            this.ViewInHexEditorContextMenuItem.Click += new System.EventHandler(this.ViewInHexEditorContextMenuItem_Click);
            // 
            // Separator4
            // 
            resources.ApplyResources(this.Separator4, "Separator4");
            // 
            // ReadBreakpointContextMenuItem
            // 
            resources.ApplyResources(this.ReadBreakpointContextMenuItem, "ReadBreakpointContextMenuItem");
            this.ReadBreakpointContextMenuItem.Click += new System.EventHandler(this.ReadBreakpointContextMenuItem_Click);
            // 
            // WriteBreakpointContextMenuItem
            // 
            resources.ApplyResources(this.WriteBreakpointContextMenuItem, "WriteBreakpointContextMenuItem");
            this.WriteBreakpointContextMenuItem.Click += new System.EventHandler(this.WriteBreakpointContextMenuItem_Click);
            // 
            // Separator6
            // 
            resources.ApplyResources(this.Separator6, "Separator6");
            // 
            // InsertSeperatorContextMenuItem
            // 
            resources.ApplyResources(this.InsertSeperatorContextMenuItem, "InsertSeperatorContextMenuItem");
            this.InsertSeperatorContextMenuItem.Click += new System.EventHandler(this.InsertSeparatorMenuItem_Click);
            // 
            // MoveUpContextMenuItem
            // 
            resources.ApplyResources(this.MoveUpContextMenuItem, "MoveUpContextMenuItem");
            this.MoveUpContextMenuItem.Click += new System.EventHandler(this.MoveUpMenuItem_Click);
            // 
            // MoveDownContextMenuItem
            // 
            resources.ApplyResources(this.MoveDownContextMenuItem, "MoveDownContextMenuItem");
            this.MoveDownContextMenuItem.Click += new System.EventHandler(this.MoveDownMenuItem_Click);
            // 
            // MoveTopContextMenuItem
            // 
            resources.ApplyResources(this.MoveTopContextMenuItem, "MoveTopContextMenuItem");
            this.MoveTopContextMenuItem.Click += new System.EventHandler(this.MoveTopMenuItem_Click);
            // 
            // MoveBottomContextMenuItem
            // 
            resources.ApplyResources(this.MoveBottomContextMenuItem, "MoveBottomContextMenuItem");
            this.MoveBottomContextMenuItem.Click += new System.EventHandler(this.MoveBottomMenuItem_Click);
            // 
            // statusStrip1
            // 
            resources.ApplyResources(this.statusStrip1, "statusStrip1");
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ErrorIconButton,
            this.MessageLabel});
            this.statusStrip1.Name = "statusStrip1";
            // 
            // ErrorIconButton
            // 
            resources.ApplyResources(this.ErrorIconButton, "ErrorIconButton");
            this.ErrorIconButton.BackColor = System.Drawing.Color.NavajoWhite;
            this.ErrorIconButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.ErrorIconButton.Name = "ErrorIconButton";
            this.ErrorIconButton.Click += new System.EventHandler(this.ErrorIconButton_Click);
            // 
            // MessageLabel
            // 
            resources.ApplyResources(this.MessageLabel, "MessageLabel");
            this.MessageLabel.Name = "MessageLabel";
            // 
            // toolStrip1
            // 
            resources.ApplyResources(this.toolStrip1, "toolStrip1");
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripButton,
            this.openToolStripButton,
            this.saveToolStripButton,
            this.toolStripSeparator,
            this.newWatchToolStripButton,
            this.editWatchToolStripButton,
            this.cutToolStripButton,
            this.clearChangeCountsToolStripButton,
            this.duplicateWatchToolStripButton,
            this.SplitWatchToolStripButton,
            this.PokeAddressToolBarItem,
            this.FreezeAddressToolBarItem,
            this.seperatorToolStripButton,
            this.toolStripSeparator6,
            this.moveUpToolStripButton,
            this.moveDownToolStripButton,
            this.toolStripSeparator5});
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.TabStop = true;
            // 
            // newToolStripButton
            // 
            resources.ApplyResources(this.newToolStripButton, "newToolStripButton");
            this.newToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.newToolStripButton.Name = "newToolStripButton";
            this.newToolStripButton.Click += new System.EventHandler(this.NewListMenuItem_Click);
            // 
            // openToolStripButton
            // 
            resources.ApplyResources(this.openToolStripButton, "openToolStripButton");
            this.openToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.openToolStripButton.Name = "openToolStripButton";
            this.openToolStripButton.Click += new System.EventHandler(this.OpenMenuItem_Click);
            // 
            // saveToolStripButton
            // 
            resources.ApplyResources(this.saveToolStripButton, "saveToolStripButton");
            this.saveToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.saveToolStripButton.Name = "saveToolStripButton";
            this.saveToolStripButton.Click += new System.EventHandler(this.SaveMenuItem_Click);
            // 
            // toolStripSeparator
            // 
            resources.ApplyResources(this.toolStripSeparator, "toolStripSeparator");
            // 
            // newWatchToolStripButton
            // 
            resources.ApplyResources(this.newWatchToolStripButton, "newWatchToolStripButton");
            this.newWatchToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.newWatchToolStripButton.Name = "newWatchToolStripButton";
            this.newWatchToolStripButton.Click += new System.EventHandler(this.NewWatchMenuItem_Click);
            // 
            // editWatchToolStripButton
            // 
            resources.ApplyResources(this.editWatchToolStripButton, "editWatchToolStripButton");
            this.editWatchToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.editWatchToolStripButton.Name = "editWatchToolStripButton";
            this.editWatchToolStripButton.Click += new System.EventHandler(this.EditWatchMenuItem_Click);
            // 
            // cutToolStripButton
            // 
            resources.ApplyResources(this.cutToolStripButton, "cutToolStripButton");
            this.cutToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.cutToolStripButton.Name = "cutToolStripButton";
            this.cutToolStripButton.Click += new System.EventHandler(this.RemoveWatchMenuItem_Click);
            // 
            // clearChangeCountsToolStripButton
            // 
            resources.ApplyResources(this.clearChangeCountsToolStripButton, "clearChangeCountsToolStripButton");
            this.clearChangeCountsToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.clearChangeCountsToolStripButton.Name = "clearChangeCountsToolStripButton";
            this.clearChangeCountsToolStripButton.Click += new System.EventHandler(this.ClearChangeCountsMenuItem_Click);
            // 
            // duplicateWatchToolStripButton
            // 
            resources.ApplyResources(this.duplicateWatchToolStripButton, "duplicateWatchToolStripButton");
            this.duplicateWatchToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.duplicateWatchToolStripButton.Name = "duplicateWatchToolStripButton";
            this.duplicateWatchToolStripButton.Click += new System.EventHandler(this.DuplicateWatchMenuItem_Click);
            // 
            // SplitWatchToolStripButton
            // 
            resources.ApplyResources(this.SplitWatchToolStripButton, "SplitWatchToolStripButton");
            this.SplitWatchToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.SplitWatchToolStripButton.Name = "SplitWatchToolStripButton";
            this.SplitWatchToolStripButton.Click += new System.EventHandler(this.SplitWatchMenuItem_Click);
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
            // seperatorToolStripButton
            // 
            resources.ApplyResources(this.seperatorToolStripButton, "seperatorToolStripButton");
            this.seperatorToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.seperatorToolStripButton.Name = "seperatorToolStripButton";
            this.seperatorToolStripButton.Click += new System.EventHandler(this.InsertSeparatorMenuItem_Click);
            // 
            // toolStripSeparator6
            // 
            resources.ApplyResources(this.toolStripSeparator6, "toolStripSeparator6");
            // 
            // moveUpToolStripButton
            // 
            resources.ApplyResources(this.moveUpToolStripButton, "moveUpToolStripButton");
            this.moveUpToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.moveUpToolStripButton.Name = "moveUpToolStripButton";
            this.moveUpToolStripButton.Click += new System.EventHandler(this.MoveUpMenuItem_Click);
            // 
            // moveDownToolStripButton
            // 
            resources.ApplyResources(this.moveDownToolStripButton, "moveDownToolStripButton");
            this.moveDownToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.moveDownToolStripButton.Name = "moveDownToolStripButton";
            this.moveDownToolStripButton.Click += new System.EventHandler(this.MoveDownMenuItem_Click);
            // 
            // toolStripSeparator5
            // 
            resources.ApplyResources(this.toolStripSeparator5, "toolStripSeparator5");
            // 
            // RamWatchMenu
            // 
            resources.ApplyResources(this.RamWatchMenu, "RamWatchMenu");
            this.RamWatchMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu,
            this.WatchesSubMenu,
            this.OptionsSubMenu});
            // 
            // FileSubMenu
            // 
            resources.ApplyResources(this.FileSubMenu, "FileSubMenu");
            this.FileSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.NewListMenuItem,
            this.OpenMenuItem,
            this.SaveMenuItem,
            this.SaveAsMenuItem,
            this.AppendMenuItem,
            this.RecentSubMenu});
            this.FileSubMenu.DropDownOpened += new System.EventHandler(this.FileSubMenu_DropDownOpened);
            // 
            // NewListMenuItem
            // 
            resources.ApplyResources(this.NewListMenuItem, "NewListMenuItem");
            this.NewListMenuItem.Click += new System.EventHandler(this.NewListMenuItem_Click);
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
            // AppendMenuItem
            // 
            resources.ApplyResources(this.AppendMenuItem, "AppendMenuItem");
            this.AppendMenuItem.Click += new System.EventHandler(this.OpenMenuItem_Click);
            // 
            // RecentSubMenu
            // 
            resources.ApplyResources(this.RecentSubMenu, "RecentSubMenu");
            this.RecentSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.noneToolStripMenuItem});
            this.RecentSubMenu.DropDownOpened += new System.EventHandler(this.RecentSubMenu_DropDownOpened);
            // 
            // noneToolStripMenuItem
            // 
            resources.ApplyResources(this.noneToolStripMenuItem, "noneToolStripMenuItem");
            // 
            // WatchesSubMenu
            // 
            resources.ApplyResources(this.WatchesSubMenu, "WatchesSubMenu");
            this.WatchesSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MemoryDomainsSubMenu,
            this.toolStripSeparator8,
            this.NewWatchMenuItem,
            this.EditWatchMenuItem,
            this.RemoveWatchMenuItem,
            this.DuplicateWatchMenuItem,
            this.SplitWatchMenuItem,
            this.PokeAddressMenuItem,
            this.FreezeAddressMenuItem,
            this.InsertSeparatorMenuItem,
            this.ClearChangeCountsMenuItem,
            this.toolStripSeparator3,
            this.MoveUpMenuItem,
            this.MoveDownMenuItem,
            this.MoveTopMenuItem,
            this.MoveBottomMenuItem,
            this.SelectAllMenuItem});
            this.WatchesSubMenu.DropDownOpened += new System.EventHandler(this.WatchesSubMenu_DropDownOpened);
            // 
            // MemoryDomainsSubMenu
            // 
            resources.ApplyResources(this.MemoryDomainsSubMenu, "MemoryDomainsSubMenu");
            this.MemoryDomainsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Separator2});
            this.MemoryDomainsSubMenu.DropDownOpened += new System.EventHandler(this.MemoryDomainsSubMenu_DropDownOpened);
            // 
            // Separator2
            // 
            resources.ApplyResources(this.Separator2, "Separator2");
            // 
            // toolStripSeparator8
            // 
            resources.ApplyResources(this.toolStripSeparator8, "toolStripSeparator8");
            // 
            // NewWatchMenuItem
            // 
            resources.ApplyResources(this.NewWatchMenuItem, "NewWatchMenuItem");
            this.NewWatchMenuItem.Click += new System.EventHandler(this.NewWatchMenuItem_Click);
            // 
            // EditWatchMenuItem
            // 
            resources.ApplyResources(this.EditWatchMenuItem, "EditWatchMenuItem");
            this.EditWatchMenuItem.Click += new System.EventHandler(this.EditWatchMenuItem_Click);
            // 
            // RemoveWatchMenuItem
            // 
            resources.ApplyResources(this.RemoveWatchMenuItem, "RemoveWatchMenuItem");
            this.RemoveWatchMenuItem.Click += new System.EventHandler(this.RemoveWatchMenuItem_Click);
            // 
            // DuplicateWatchMenuItem
            // 
            resources.ApplyResources(this.DuplicateWatchMenuItem, "DuplicateWatchMenuItem");
            this.DuplicateWatchMenuItem.Click += new System.EventHandler(this.DuplicateWatchMenuItem_Click);
            // 
            // SplitWatchMenuItem
            // 
            resources.ApplyResources(this.SplitWatchMenuItem, "SplitWatchMenuItem");
            this.SplitWatchMenuItem.Click += new System.EventHandler(this.SplitWatchMenuItem_Click);
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
            // InsertSeparatorMenuItem
            // 
            resources.ApplyResources(this.InsertSeparatorMenuItem, "InsertSeparatorMenuItem");
            this.InsertSeparatorMenuItem.Click += new System.EventHandler(this.InsertSeparatorMenuItem_Click);
            // 
            // ClearChangeCountsMenuItem
            // 
            resources.ApplyResources(this.ClearChangeCountsMenuItem, "ClearChangeCountsMenuItem");
            this.ClearChangeCountsMenuItem.Click += new System.EventHandler(this.ClearChangeCountsMenuItem_Click);
            // 
            // toolStripSeparator3
            // 
            resources.ApplyResources(this.toolStripSeparator3, "toolStripSeparator3");
            // 
            // MoveUpMenuItem
            // 
            resources.ApplyResources(this.MoveUpMenuItem, "MoveUpMenuItem");
            this.MoveUpMenuItem.Click += new System.EventHandler(this.MoveUpMenuItem_Click);
            // 
            // MoveDownMenuItem
            // 
            resources.ApplyResources(this.MoveDownMenuItem, "MoveDownMenuItem");
            this.MoveDownMenuItem.Click += new System.EventHandler(this.MoveDownMenuItem_Click);
            // 
            // MoveTopMenuItem
            // 
            resources.ApplyResources(this.MoveTopMenuItem, "MoveTopMenuItem");
            this.MoveTopMenuItem.Click += new System.EventHandler(this.MoveTopMenuItem_Click);
            // 
            // MoveBottomMenuItem
            // 
            resources.ApplyResources(this.MoveBottomMenuItem, "MoveBottomMenuItem");
            this.MoveBottomMenuItem.Click += new System.EventHandler(this.MoveBottomMenuItem_Click);
            // 
            // SelectAllMenuItem
            // 
            resources.ApplyResources(this.SelectAllMenuItem, "SelectAllMenuItem");
            this.SelectAllMenuItem.Click += new System.EventHandler(this.SelectAllMenuItem_Click);
            // 
            // OptionsSubMenu
            // 
            resources.ApplyResources(this.OptionsSubMenu, "OptionsSubMenu");
            this.OptionsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.DefinePreviousValueSubMenu,
            this.WatchesOnScreenMenuItem,
            this.DoubleClickActionSubMenu});
            this.OptionsSubMenu.DropDownOpened += new System.EventHandler(this.SettingsSubMenu_DropDownOpened);
            // 
            // DefinePreviousValueSubMenu
            // 
            resources.ApplyResources(this.DefinePreviousValueSubMenu, "DefinePreviousValueSubMenu");
            this.DefinePreviousValueSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.PreviousFrameMenuItem,
            this.LastChangeMenuItem,
            this.OriginalMenuItem});
            this.DefinePreviousValueSubMenu.DropDownOpened += new System.EventHandler(this.DefinePreviousValueSubMenu_DropDownOpened);
            // 
            // PreviousFrameMenuItem
            // 
            resources.ApplyResources(this.PreviousFrameMenuItem, "PreviousFrameMenuItem");
            this.PreviousFrameMenuItem.Click += new System.EventHandler(this.PreviousFrameMenuItem_Click);
            // 
            // LastChangeMenuItem
            // 
            resources.ApplyResources(this.LastChangeMenuItem, "LastChangeMenuItem");
            this.LastChangeMenuItem.Click += new System.EventHandler(this.LastChangeMenuItem_Click);
            // 
            // OriginalMenuItem
            // 
            resources.ApplyResources(this.OriginalMenuItem, "OriginalMenuItem");
            this.OriginalMenuItem.Click += new System.EventHandler(this.OriginalMenuItem_Click);
            // 
            // WatchesOnScreenMenuItem
            // 
            resources.ApplyResources(this.WatchesOnScreenMenuItem, "WatchesOnScreenMenuItem");
            this.WatchesOnScreenMenuItem.Click += new System.EventHandler(this.WatchesOnScreenMenuItem_Click);
            // 
            // DoubleClickActionSubMenu
            // 
            resources.ApplyResources(this.DoubleClickActionSubMenu, "DoubleClickActionSubMenu");
            this.DoubleClickActionSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.DoubleClickToEditMenuItem,
            this.DoubleClickToPokeMenuItem});
            this.DoubleClickActionSubMenu.DropDownOpening += new System.EventHandler(this.DoubleClickActionSubMenu_DropDownOpening);
            // 
            // DoubleClickToEditMenuItem
            // 
            resources.ApplyResources(this.DoubleClickToEditMenuItem, "DoubleClickToEditMenuItem");
            this.DoubleClickToEditMenuItem.Click += new System.EventHandler(this.DoubleClickToEditMenuItem_Click);
            // 
            // DoubleClickToPokeMenuItem
            // 
            resources.ApplyResources(this.DoubleClickToPokeMenuItem, "DoubleClickToPokeMenuItem");
            this.DoubleClickToPokeMenuItem.Click += new System.EventHandler(this.DoubleClickToPokeMenuItem_Click);
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
            this.WatchListView.ColumnClick += new BizHawk.Client.EmuHawk.InputRoll.ColumnClickEventHandler(this.WatchListView_ColumnClick);
            this.WatchListView.SelectedIndexChanged += new System.EventHandler(this.WatchListView_SelectedIndexChanged);
            this.WatchListView.DragDrop += new System.Windows.Forms.DragEventHandler(this.RamWatch_DragDrop);
            this.WatchListView.DragEnter += new System.Windows.Forms.DragEventHandler(this.DragEnterWrapper);
            this.WatchListView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.WatchListView_KeyDown);
            this.WatchListView.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.WatchListView_MouseDoubleClick);
            // 
            // RamWatch
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.WatchCountLabel);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.RamWatchMenu);
            this.Controls.Add(this.WatchListView);
            this.Name = "RamWatch";
            this.Load += new System.EventHandler(this.RamWatch_Load);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.RamWatch_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.DragEnterWrapper);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.WatchListView_KeyDown);
            this.ListViewContextMenu.ResumeLayout(false);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.RamWatchMenu.ResumeLayout(false);
            this.RamWatchMenu.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private InputRoll WatchListView;
		private MenuStripEx RamWatchMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FileSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx NewListMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx OpenMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SaveMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SaveAsMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx AppendMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RecentSubMenu;
        private BizHawk.WinForms.Controls.ToolStripMenuItemEx noneToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx WatchesSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx MemoryDomainsSubMenu;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator8;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx NewWatchMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx EditWatchMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RemoveWatchMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DuplicateWatchMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SplitWatchMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx PokeAddressMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FreezeAddressMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx InsertSeparatorMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ClearChangeCountsMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator3;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx MoveUpMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx MoveDownMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SelectAllMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx OptionsSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DefinePreviousValueSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx PreviousFrameMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx LastChangeMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx WatchesOnScreenMenuItem;
		private ToolStripEx toolStrip1;
		private System.Windows.Forms.ToolStripButton newToolStripButton;
		private System.Windows.Forms.ToolStripButton openToolStripButton;
		private System.Windows.Forms.ToolStripButton saveToolStripButton;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator;
		private System.Windows.Forms.ToolStripButton newWatchToolStripButton;
		private System.Windows.Forms.ToolStripButton editWatchToolStripButton;
		private System.Windows.Forms.ToolStripButton cutToolStripButton;
		private System.Windows.Forms.ToolStripButton clearChangeCountsToolStripButton;
		private System.Windows.Forms.ToolStripButton duplicateWatchToolStripButton;
		private System.Windows.Forms.ToolStripButton SplitWatchToolStripButton;
		private System.Windows.Forms.ToolStripButton PokeAddressToolBarItem;
		private System.Windows.Forms.ToolStripButton FreezeAddressToolBarItem;
		private System.Windows.Forms.ToolStripButton seperatorToolStripButton;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator5;
		private System.Windows.Forms.ToolStripButton moveUpToolStripButton;
		private System.Windows.Forms.ToolStripButton moveDownToolStripButton;
		private BizHawk.WinForms.Controls.LocLabelEx WatchCountLabel;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx Separator2;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx OriginalMenuItem;
		private System.Windows.Forms.ContextMenuStrip ListViewContextMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx EditContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RemoveContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DuplicateContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SplitContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx PokeContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FreezeContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx UnfreezeAllContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ViewInHexEditorContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx Separator6;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx InsertSeperatorContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx MoveUpContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx MoveDownContextMenuItem;
		private StatusStripEx statusStrip1;
		private System.Windows.Forms.ToolStripStatusLabel MessageLabel;
		private System.Windows.Forms.ToolStripButton ErrorIconButton;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator6;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx Separator4;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ReadBreakpointContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx WriteBreakpointContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx newToolStripMenuItem;
        private BizHawk.WinForms.Controls.ToolStripMenuItemEx MoveTopMenuItem;
        private BizHawk.WinForms.Controls.ToolStripMenuItemEx MoveBottomMenuItem;
        private BizHawk.WinForms.Controls.ToolStripMenuItemEx MoveTopContextMenuItem;
        private BizHawk.WinForms.Controls.ToolStripMenuItemEx MoveBottomContextMenuItem;
        private ToolStripMenuItemEx DoubleClickActionSubMenu;
        private ToolStripMenuItemEx DoubleClickToEditMenuItem;
        private ToolStripMenuItemEx DoubleClickToPokeMenuItem;
    }
}
