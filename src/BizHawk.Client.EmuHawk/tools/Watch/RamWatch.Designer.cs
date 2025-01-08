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
			this.WatchCountLabel.Location = new System.Drawing.Point(16, 57);
			this.WatchCountLabel.Name = "WatchCountLabel";
			this.WatchCountLabel.Text = "0 watches";
			// 
			// ListViewContextMenu
			// 
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
			this.ListViewContextMenu.Size = new System.Drawing.Size(245, 368);
			this.ListViewContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.ListViewContextMenu_Opening);
			// 
			// newToolStripMenuItem
			// 
			this.newToolStripMenuItem.Text = "&New Watch";
			this.newToolStripMenuItem.Click += new System.EventHandler(this.NewWatchMenuItem_Click);
			// 
			// EditContextMenuItem
			// 
			this.EditContextMenuItem.ShortcutKeyDisplayString = "Ctrl+E";
			this.EditContextMenuItem.Text = "&Edit";
			this.EditContextMenuItem.Click += new System.EventHandler(this.EditWatchMenuItem_Click);
			// 
			// RemoveContextMenuItem
			// 
			this.RemoveContextMenuItem.ShortcutKeyDisplayString = "Ctrl+R";
			this.RemoveContextMenuItem.Text = "&Remove";
			this.RemoveContextMenuItem.Click += new System.EventHandler(this.RemoveWatchMenuItem_Click);
			// 
			// DuplicateContextMenuItem
			// 
			this.DuplicateContextMenuItem.ShortcutKeyDisplayString = "Ctrl+D";
			this.DuplicateContextMenuItem.Text = "&Duplicate";
			this.DuplicateContextMenuItem.Click += new System.EventHandler(this.DuplicateWatchMenuItem_Click);
			// 
			// SplitContextMenuItem
			// 
			this.SplitContextMenuItem.ShortcutKeyDisplayString = "Ctrl+L";
			this.SplitContextMenuItem.Text = "Sp&lit";
			this.SplitContextMenuItem.Click += new System.EventHandler(this.SplitWatchMenuItem_Click);
			// 
			// PokeContextMenuItem
			// 
			this.PokeContextMenuItem.ShortcutKeyDisplayString = "Ctrl+P";
			this.PokeContextMenuItem.Text = "&Poke";
			this.PokeContextMenuItem.Click += new System.EventHandler(this.PokeAddressMenuItem_Click);
			// 
			// FreezeContextMenuItem
			// 
			this.FreezeContextMenuItem.ShortcutKeyDisplayString = "Ctrl+F";
			this.FreezeContextMenuItem.Text = "&Freeze";
			this.FreezeContextMenuItem.Click += new System.EventHandler(this.FreezeAddressMenuItem_Click);
			// 
			// UnfreezeAllContextMenuItem
			// 
			this.UnfreezeAllContextMenuItem.Text = "Unfreeze &All";
			this.UnfreezeAllContextMenuItem.Click += new System.EventHandler(this.UnfreezeAllContextMenuItem_Click);
			// 
			// ViewInHexEditorContextMenuItem
			// 
			this.ViewInHexEditorContextMenuItem.Text = "View in Hex Editor";
			this.ViewInHexEditorContextMenuItem.Click += new System.EventHandler(this.ViewInHexEditorContextMenuItem_Click);
			// 
			// ReadBreakpointContextMenuItem
			// 
			this.ReadBreakpointContextMenuItem.Text = "Set Read Breakpoint";
			this.ReadBreakpointContextMenuItem.Click += new System.EventHandler(this.ReadBreakpointContextMenuItem_Click);
			// 
			// WriteBreakpointContextMenuItem
			// 
			this.WriteBreakpointContextMenuItem.Text = "Set Write Breakpoint";
			this.WriteBreakpointContextMenuItem.Click += new System.EventHandler(this.WriteBreakpointContextMenuItem_Click);
			// 
			// InsertSeperatorContextMenuItem
			// 
			this.InsertSeperatorContextMenuItem.ShortcutKeyDisplayString = "Ctrl+I";
			this.InsertSeperatorContextMenuItem.Text = "&Insert Separator";
			this.InsertSeperatorContextMenuItem.Click += new System.EventHandler(this.InsertSeparatorMenuItem_Click);
			// 
			// MoveUpContextMenuItem
			// 
			this.MoveUpContextMenuItem.ShortcutKeyDisplayString = "Ctrl+Up";
			this.MoveUpContextMenuItem.Text = "Move &Up";
			this.MoveUpContextMenuItem.Click += new System.EventHandler(this.MoveUpMenuItem_Click);
			// 
			// MoveDownContextMenuItem
			// 
			this.MoveDownContextMenuItem.ShortcutKeyDisplayString = "Ctrl+Down";
			this.MoveDownContextMenuItem.Text = "Move &Down";
			this.MoveDownContextMenuItem.Click += new System.EventHandler(this.MoveDownMenuItem_Click);
			// 
			// MoveTopContextMenuItem
			// 
			this.MoveTopContextMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.Up)));
			this.MoveTopContextMenuItem.Text = "Move &Top";
			this.MoveTopContextMenuItem.Click += new System.EventHandler(this.MoveTopMenuItem_Click);
			// 
			// MoveBottomContextMenuItem
			// 
			this.MoveBottomContextMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.Down)));
			this.MoveBottomContextMenuItem.Text = "Move &Bottom";
			this.MoveBottomContextMenuItem.Click += new System.EventHandler(this.MoveBottomMenuItem_Click);
			// 
			// statusStrip1
			// 
			this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ErrorIconButton,
            this.MessageLabel});
			this.statusStrip1.Location = new System.Drawing.Point(0, 356);
			this.statusStrip1.Name = "statusStrip1";
			this.statusStrip1.TabIndex = 8;
			// 
			// ErrorIconButton
			// 
			this.ErrorIconButton.BackColor = System.Drawing.Color.NavajoWhite;
			this.ErrorIconButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.ErrorIconButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.ErrorIconButton.Name = "ErrorIconButton";
			this.ErrorIconButton.Size = new System.Drawing.Size(23, 20);
			this.ErrorIconButton.Text = "Warning! Out of Range Addresses in list, click to remove them";
			this.ErrorIconButton.Click += new System.EventHandler(this.ErrorIconButton_Click);
			// 
			// MessageLabel
			// 
			this.MessageLabel.Name = "MessageLabel";
			this.MessageLabel.Size = new System.Drawing.Size(31, 17);
			this.MessageLabel.Text = "        ";
			// 
			// toolStrip1
			// 
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
			this.toolStrip1.Location = new System.Drawing.Point(0, 24);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.TabIndex = 4;
			this.toolStrip1.TabStop = true;
			// 
			// newToolStripButton
			// 
			this.newToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.newToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.newToolStripButton.Name = "newToolStripButton";
			this.newToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.newToolStripButton.Text = "&New";
			this.newToolStripButton.Click += new System.EventHandler(this.NewListMenuItem_Click);
			// 
			// openToolStripButton
			// 
			this.openToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.openToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.openToolStripButton.Name = "openToolStripButton";
			this.openToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.openToolStripButton.Text = "&Open";
			this.openToolStripButton.Click += new System.EventHandler(this.OpenMenuItem_Click);
			// 
			// saveToolStripButton
			// 
			this.saveToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.saveToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.saveToolStripButton.Name = "saveToolStripButton";
			this.saveToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.saveToolStripButton.Text = "&Save";
			this.saveToolStripButton.Click += new System.EventHandler(this.SaveMenuItem_Click);
			// 
			// newWatchToolStripButton
			// 
			this.newWatchToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.newWatchToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.newWatchToolStripButton.Name = "newWatchToolStripButton";
			this.newWatchToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.newWatchToolStripButton.Text = "New Watch";
			this.newWatchToolStripButton.ToolTipText = "New Watch";
			this.newWatchToolStripButton.Click += new System.EventHandler(this.NewWatchMenuItem_Click);
			// 
			// editWatchToolStripButton
			// 
			this.editWatchToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.editWatchToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.editWatchToolStripButton.Name = "editWatchToolStripButton";
			this.editWatchToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.editWatchToolStripButton.Text = "Edit Watch";
			this.editWatchToolStripButton.Click += new System.EventHandler(this.EditWatchMenuItem_Click);
			// 
			// cutToolStripButton
			// 
			this.cutToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.cutToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.cutToolStripButton.Name = "cutToolStripButton";
			this.cutToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.cutToolStripButton.Text = "C&ut";
			this.cutToolStripButton.ToolTipText = "Remove Watch";
			this.cutToolStripButton.Click += new System.EventHandler(this.RemoveWatchMenuItem_Click);
			// 
			// clearChangeCountsToolStripButton
			// 
			this.clearChangeCountsToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.clearChangeCountsToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.clearChangeCountsToolStripButton.Name = "clearChangeCountsToolStripButton";
			this.clearChangeCountsToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.clearChangeCountsToolStripButton.Text = "C";
			this.clearChangeCountsToolStripButton.ToolTipText = "Clear Change Counts";
			this.clearChangeCountsToolStripButton.Click += new System.EventHandler(this.ClearChangeCountsMenuItem_Click);
			// 
			// duplicateWatchToolStripButton
			// 
			this.duplicateWatchToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.duplicateWatchToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.duplicateWatchToolStripButton.Name = "duplicateWatchToolStripButton";
			this.duplicateWatchToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.duplicateWatchToolStripButton.Text = "Duplicate Watch";
			this.duplicateWatchToolStripButton.Click += new System.EventHandler(this.DuplicateWatchMenuItem_Click);
			// 
			// SplitWatchToolStripButton
			// 
			this.SplitWatchToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.SplitWatchToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.SplitWatchToolStripButton.Name = "SplitWatchToolStripButton";
			this.SplitWatchToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.SplitWatchToolStripButton.Text = "Split Watch";
			this.SplitWatchToolStripButton.Click += new System.EventHandler(this.SplitWatchMenuItem_Click);
			// 
			// PokeAddressToolBarItem
			// 
			this.PokeAddressToolBarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.PokeAddressToolBarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.PokeAddressToolBarItem.Name = "PokeAddressToolBarItem";
			this.PokeAddressToolBarItem.Size = new System.Drawing.Size(23, 22);
			this.PokeAddressToolBarItem.Text = "toolStripButton2";
			this.PokeAddressToolBarItem.ToolTipText = "Poke address";
			this.PokeAddressToolBarItem.Click += new System.EventHandler(this.PokeAddressMenuItem_Click);
			// 
			// FreezeAddressToolBarItem
			// 
			this.FreezeAddressToolBarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FreezeAddressToolBarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FreezeAddressToolBarItem.Name = "FreezeAddressToolBarItem";
			this.FreezeAddressToolBarItem.Size = new System.Drawing.Size(23, 22);
			this.FreezeAddressToolBarItem.Text = "Freeze Address";
			this.FreezeAddressToolBarItem.Click += new System.EventHandler(this.FreezeAddressMenuItem_Click);
			// 
			// seperatorToolStripButton
			// 
			this.seperatorToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.seperatorToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.seperatorToolStripButton.Name = "seperatorToolStripButton";
			this.seperatorToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.seperatorToolStripButton.Text = "-";
			this.seperatorToolStripButton.ToolTipText = "Insert Separator";
			this.seperatorToolStripButton.Click += new System.EventHandler(this.InsertSeparatorMenuItem_Click);
			// 
			// moveUpToolStripButton
			// 
			this.moveUpToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.moveUpToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.moveUpToolStripButton.Name = "moveUpToolStripButton";
			this.moveUpToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.moveUpToolStripButton.Text = "Move Up";
			this.moveUpToolStripButton.Click += new System.EventHandler(this.MoveUpMenuItem_Click);
			// 
			// moveDownToolStripButton
			// 
			this.moveDownToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.moveDownToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.moveDownToolStripButton.Name = "moveDownToolStripButton";
			this.moveDownToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.moveDownToolStripButton.Text = "Move Down";
			this.moveDownToolStripButton.Click += new System.EventHandler(this.MoveDownMenuItem_Click);
			// 
			// RamWatchMenu
			// 
			this.RamWatchMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu,
            this.WatchesSubMenu,
            this.OptionsSubMenu});
			this.RamWatchMenu.TabIndex = 3;
			// 
			// FileSubMenu
			// 
			this.FileSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.NewListMenuItem,
            this.OpenMenuItem,
            this.SaveMenuItem,
            this.SaveAsMenuItem,
            this.AppendMenuItem,
            this.RecentSubMenu});
			this.FileSubMenu.Text = "&Files";
			this.FileSubMenu.DropDownOpened += new System.EventHandler(this.FileSubMenu_DropDownOpened);
			// 
			// NewListMenuItem
			// 
			this.NewListMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
			this.NewListMenuItem.Text = "&New List";
			this.NewListMenuItem.Click += new System.EventHandler(this.NewListMenuItem_Click);
			// 
			// OpenMenuItem
			// 
			this.OpenMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.OpenMenuItem.Text = "&Open...";
			this.OpenMenuItem.Click += new System.EventHandler(this.OpenMenuItem_Click);
			// 
			// SaveMenuItem
			// 
			this.SaveMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			this.SaveMenuItem.Text = "&Save";
			this.SaveMenuItem.Click += new System.EventHandler(this.SaveMenuItem_Click);
			// 
			// SaveAsMenuItem
			// 
			this.SaveAsMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.S)));
			this.SaveAsMenuItem.Text = "Save &As...";
			this.SaveAsMenuItem.Click += new System.EventHandler(this.SaveAsMenuItem_Click);
			// 
			// AppendMenuItem
			// 
			this.AppendMenuItem.Text = "A&ppend File...";
			this.AppendMenuItem.Click += new System.EventHandler(this.OpenMenuItem_Click);
			// 
			// RecentSubMenu
			// 
			this.RecentSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.noneToolStripMenuItem});
			this.RecentSubMenu.Text = "Recent";
			this.RecentSubMenu.DropDownOpened += new System.EventHandler(this.RecentSubMenu_DropDownOpened);
			// 
			// noneToolStripMenuItem
			// 
			this.noneToolStripMenuItem.Text = "None";
			// 
			// WatchesSubMenu
			// 
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
			this.WatchesSubMenu.Text = "&Watches";
			this.WatchesSubMenu.DropDownOpened += new System.EventHandler(this.WatchesSubMenu_DropDownOpened);
			// 
			// MemoryDomainsSubMenu
			// 
			this.MemoryDomainsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Separator2});
			this.MemoryDomainsSubMenu.Text = "Default Domain";
			this.MemoryDomainsSubMenu.DropDownOpened += new System.EventHandler(this.MemoryDomainsSubMenu_DropDownOpened);
			// 
			// NewWatchMenuItem
			// 
			this.NewWatchMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.W)));
			this.NewWatchMenuItem.Text = "&New Watch";
			this.NewWatchMenuItem.Click += new System.EventHandler(this.NewWatchMenuItem_Click);
			// 
			// EditWatchMenuItem
			// 
			this.EditWatchMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E)));
			this.EditWatchMenuItem.Text = "&Edit Watch";
			this.EditWatchMenuItem.Click += new System.EventHandler(this.EditWatchMenuItem_Click);
			// 
			// RemoveWatchMenuItem
			// 
			this.RemoveWatchMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R)));
			this.RemoveWatchMenuItem.Text = "&Remove Watch";
			this.RemoveWatchMenuItem.Click += new System.EventHandler(this.RemoveWatchMenuItem_Click);
			// 
			// DuplicateWatchMenuItem
			// 
			this.DuplicateWatchMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
			this.DuplicateWatchMenuItem.Text = "&Duplicate Watch";
			this.DuplicateWatchMenuItem.Click += new System.EventHandler(this.DuplicateWatchMenuItem_Click);
			// 
			// SplitWatchMenuItem
			// 
			this.SplitWatchMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.L)));
			this.SplitWatchMenuItem.Text = "Sp&lit Watch";
			this.SplitWatchMenuItem.Click += new System.EventHandler(this.SplitWatchMenuItem_Click);
			// 
			// PokeAddressMenuItem
			// 
			this.PokeAddressMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.P)));
			this.PokeAddressMenuItem.Text = "Poke Address";
			this.PokeAddressMenuItem.Click += new System.EventHandler(this.PokeAddressMenuItem_Click);
			// 
			// FreezeAddressMenuItem
			// 
			this.FreezeAddressMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F)));
			this.FreezeAddressMenuItem.Text = "Freeze Address";
			this.FreezeAddressMenuItem.Click += new System.EventHandler(this.FreezeAddressMenuItem_Click);
			// 
			// InsertSeparatorMenuItem
			// 
			this.InsertSeparatorMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.I)));
			this.InsertSeparatorMenuItem.Text = "Insert Separator";
			this.InsertSeparatorMenuItem.Click += new System.EventHandler(this.InsertSeparatorMenuItem_Click);
			// 
			// ClearChangeCountsMenuItem
			// 
			this.ClearChangeCountsMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.C)));
			this.ClearChangeCountsMenuItem.Text = "&Clear Change Counts";
			this.ClearChangeCountsMenuItem.Click += new System.EventHandler(this.ClearChangeCountsMenuItem_Click);
			// 
			// MoveUpMenuItem
			// 
			this.MoveUpMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Up)));
			this.MoveUpMenuItem.Text = "Move &Up";
			this.MoveUpMenuItem.Click += new System.EventHandler(this.MoveUpMenuItem_Click);
			// 
			// MoveDownMenuItem
			// 
			this.MoveDownMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Down)));
			this.MoveDownMenuItem.Text = "Move &Down";
			this.MoveDownMenuItem.Click += new System.EventHandler(this.MoveDownMenuItem_Click);
			// 
			// MoveTopMenuItem
			// 
			this.MoveTopMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.Up)));
			this.MoveTopMenuItem.Text = "Move &Top";
			this.MoveTopMenuItem.Click += new System.EventHandler(this.MoveTopMenuItem_Click);
			// 
			// MoveBottomMenuItem
			// 
			this.MoveBottomMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.Down)));
			this.MoveBottomMenuItem.Text = "Move &Bottom";
			this.MoveBottomMenuItem.Click += new System.EventHandler(this.MoveBottomMenuItem_Click);
			// 
			// SelectAllMenuItem
			// 
			this.SelectAllMenuItem.ShortcutKeyDisplayString = "Ctrl+A";
			this.SelectAllMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
			this.SelectAllMenuItem.Text = "Select &All";
			this.SelectAllMenuItem.Click += new System.EventHandler(this.SelectAllMenuItem_Click);
			// 
			// OptionsSubMenu
			// 
			this.OptionsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.DefinePreviousValueSubMenu,
            this.WatchesOnScreenMenuItem,
            this.DoubleClickActionSubMenu});
			this.OptionsSubMenu.Text = "&Settings";
			this.OptionsSubMenu.DropDownOpened += new System.EventHandler(this.SettingsSubMenu_DropDownOpened);
			// 
			// DefinePreviousValueSubMenu
			// 
			this.DefinePreviousValueSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.PreviousFrameMenuItem,
            this.LastChangeMenuItem,
            this.OriginalMenuItem});
			this.DefinePreviousValueSubMenu.Text = "Define Previous Value";
			this.DefinePreviousValueSubMenu.DropDownOpened += new System.EventHandler(this.DefinePreviousValueSubMenu_DropDownOpened);
			// 
			// PreviousFrameMenuItem
			// 
			this.PreviousFrameMenuItem.Text = "Previous Frame";
			this.PreviousFrameMenuItem.Click += new System.EventHandler(this.PreviousFrameMenuItem_Click);
			// 
			// LastChangeMenuItem
			// 
			this.LastChangeMenuItem.Text = "Last Change";
			this.LastChangeMenuItem.Click += new System.EventHandler(this.LastChangeMenuItem_Click);
			// 
			// OriginalMenuItem
			// 
			this.OriginalMenuItem.Text = "&Original";
			this.OriginalMenuItem.Click += new System.EventHandler(this.OriginalMenuItem_Click);
			// 
			// WatchesOnScreenMenuItem
			// 
			this.WatchesOnScreenMenuItem.Text = "Display Watches On Screen";
			this.WatchesOnScreenMenuItem.Click += new System.EventHandler(this.WatchesOnScreenMenuItem_Click);
			// 
			// DoubleClickActionSubMenu
			// 
			this.DoubleClickActionSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.DoubleClickToEditMenuItem,
            this.DoubleClickToPokeMenuItem});
			this.DoubleClickActionSubMenu.Text = "On Double-Clicking a Watch";
			this.DoubleClickActionSubMenu.DropDownOpening += new System.EventHandler(this.DoubleClickActionSubMenu_DropDownOpening);
			// 
			// DoubleClickToEditMenuItem
			// 
			this.DoubleClickToEditMenuItem.Text = "Edit Watch";
			this.DoubleClickToEditMenuItem.Click += new System.EventHandler(this.DoubleClickToEditMenuItem_Click);
			// 
			// DoubleClickToPokeMenuItem
			// 
			this.DoubleClickToPokeMenuItem.Text = "Poke Address";
			this.DoubleClickToPokeMenuItem.Click += new System.EventHandler(this.DoubleClickToPokeMenuItem_Click);
			// 
			// WatchListView
			// 
			this.WatchListView.AllowColumnReorder = true;
			this.WatchListView.AllowColumnResize = true;
			this.WatchListView.AllowDrop = true;
			this.WatchListView.AlwaysScroll = false;
			this.WatchListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.WatchListView.CellHeightPadding = 0;
			this.WatchListView.CellWidthPadding = 0;
			this.WatchListView.ContextMenuStrip = this.ListViewContextMenu;
			this.WatchListView.FullRowSelect = true;
			this.WatchListView.HorizontalOrientation = false;
			this.WatchListView.LetKeysModifySelection = false;
			this.WatchListView.Location = new System.Drawing.Point(16, 76);
			this.WatchListView.Name = "WatchListView";
			this.WatchListView.RowCount = 0;
			this.WatchListView.ScrollSpeed = 0;
			this.WatchListView.Size = new System.Drawing.Size(363, 281);
			this.WatchListView.TabIndex = 2;
			this.WatchListView.ColumnClick += new BizHawk.Client.EmuHawk.InputRoll.ColumnClickEventHandler(this.WatchListView_ColumnClick);
			this.WatchListView.SelectedIndexChanged += new System.EventHandler(this.WatchListView_SelectedIndexChanged);
			this.WatchListView.DragDrop += new System.Windows.Forms.DragEventHandler(this.RamWatch_DragDrop);
			this.WatchListView.DragEnter += new System.Windows.Forms.DragEventHandler(this.DragEnterWrapper);
			this.WatchListView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.WatchListView_KeyDown);
			this.WatchListView.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.WatchListView_MouseDoubleClick);
			// 
			// RamWatch
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(395, 378);
			this.Controls.Add(this.statusStrip1);
			this.Controls.Add(this.WatchCountLabel);
			this.Controls.Add(this.toolStrip1);
			this.Controls.Add(this.RamWatchMenu);
			this.Controls.Add(this.WatchListView);
			this.Name = "RamWatch";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
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
