using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	partial class TAStudio
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TAStudio));
            this.BranchesMarkersSplit = new System.Windows.Forms.SplitContainer();
            this.BookMarkControl = new BizHawk.Client.EmuHawk.BookmarksBranchesBox();
            this.TasPlaybackBox = new BizHawk.Client.EmuHawk.PlaybackBox();
            this.MarkerControl = new BizHawk.Client.EmuHawk.MarkerControl();
            this.MainVertialSplit = new System.Windows.Forms.SplitContainer();
            this.TASMenu = new BizHawk.WinForms.Controls.MenuStripEx();
            this.FileSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.NewTASMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.NewFromSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.NewFromNowMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.NewFromCurrentSaveRamMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.OpenTASMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SaveTASMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SaveAsTASMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SaveBackupMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SaveBk2BackupMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.RecentSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator3 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.toolStripSeparator1 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.saveSelectionToMacroToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.placeMacroAtSelectionToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.recentMacrosToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator22 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.toolStripSeparator20 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.ToBk2MenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.EditSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.UndoMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.RedoMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.showUndoHistoryToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SelectionUndoMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SelectionRedoMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator5 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.DeselectMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SelectBetweenMarkersMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SelectAllMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.ReselectClipboardMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.GoToFrameMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator7 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.CopyMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.PasteMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.PasteInsertMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.CutMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator8 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.ClearFramesMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.DeleteFramesMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.InsertFrameMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.InsertNumFramesMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.CloneFramesMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.CloneFramesXTimesMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator6 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.TruncateMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.ClearGreenzoneMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.GreenzoneICheckSeparator = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.StateHistoryIntegrityCheckMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.MetaSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.HeaderMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.CommentsMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SubtitlesMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SettingsSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.TAStudioSettingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ColumnsSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.HelpSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.TASEditorManualOnlineMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.ForumThreadMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.aboutToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator19 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.TasStatusStrip = new BizHawk.WinForms.Controls.StatusStripEx();
            this.MessageStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.ProgressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
            this.SplicerStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.RightClickMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.SetMarkersContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SetMarkerWithTextContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.RemoveMarkersContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator15 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.DeselectContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SelectBetweenMarkersContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator16 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.UngreenzoneContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.CancelSeekContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator17 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.copyToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.pasteToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.pasteInsertToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.cutToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.separateToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.ClearContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.DeleteFramesContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.InsertFrameContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.InsertNumFramesContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.CloneContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.CloneXTimesContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator18 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.TruncateContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.BranchContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.StartFromNowSeparator = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.StartNewProjectFromNowMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.StartANewProjectFromSaveRamMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.ColumnRightClickMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.AutoHoldContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.HideColumnContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ShowColumnsContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.NewInputRollContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.DeleteInputRollContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.BranchesMarkersSplit)).BeginInit();
            this.BranchesMarkersSplit.Panel1.SuspendLayout();
            this.BranchesMarkersSplit.Panel2.SuspendLayout();
            this.BranchesMarkersSplit.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.MainVertialSplit)).BeginInit();
            this.MainVertialSplit.Panel2.SuspendLayout();
            this.MainVertialSplit.SuspendLayout();
            this.TASMenu.SuspendLayout();
            this.TasStatusStrip.SuspendLayout();
            this.RightClickMenu.SuspendLayout();
            this.ColumnRightClickMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // BranchesMarkersSplit
            // 
            resources.ApplyResources(this.BranchesMarkersSplit, "BranchesMarkersSplit");
            this.BranchesMarkersSplit.Name = "BranchesMarkersSplit";
            // 
            // BranchesMarkersSplit.Panel1
            // 
            resources.ApplyResources(this.BranchesMarkersSplit.Panel1, "BranchesMarkersSplit.Panel1");
            this.BranchesMarkersSplit.Panel1.Controls.Add(this.BookMarkControl);
            this.BranchesMarkersSplit.Panel1.Controls.Add(this.TasPlaybackBox);
            this.toolTip1.SetToolTip(this.BranchesMarkersSplit.Panel1, resources.GetString("BranchesMarkersSplit.Panel1.ToolTip"));
            // 
            // BranchesMarkersSplit.Panel2
            // 
            resources.ApplyResources(this.BranchesMarkersSplit.Panel2, "BranchesMarkersSplit.Panel2");
            this.BranchesMarkersSplit.Panel2.Controls.Add(this.MarkerControl);
            this.toolTip1.SetToolTip(this.BranchesMarkersSplit.Panel2, resources.GetString("BranchesMarkersSplit.Panel2.ToolTip"));
            this.toolTip1.SetToolTip(this.BranchesMarkersSplit, resources.GetString("BranchesMarkersSplit.ToolTip"));
            this.BranchesMarkersSplit.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.BranchesMarkersSplit_SplitterMoved);
            // 
            // BookMarkControl
            // 
            resources.ApplyResources(this.BookMarkControl, "BookMarkControl");
            this.BookMarkControl.Name = "BookMarkControl";
            this.BookMarkControl.Tastudio = null;
            this.toolTip1.SetToolTip(this.BookMarkControl, resources.GetString("BookMarkControl.ToolTip"));
            // 
            // TasPlaybackBox
            // 
            resources.ApplyResources(this.TasPlaybackBox, "TasPlaybackBox");
            this.TasPlaybackBox.Name = "TasPlaybackBox";
            this.TasPlaybackBox.Tastudio = null;
            this.toolTip1.SetToolTip(this.TasPlaybackBox, resources.GetString("TasPlaybackBox.ToolTip"));
            // 
            // MarkerControl
            // 
            resources.ApplyResources(this.MarkerControl, "MarkerControl");
            this.MarkerControl.Name = "MarkerControl";
            this.MarkerControl.Tastudio = null;
            this.toolTip1.SetToolTip(this.MarkerControl, resources.GetString("MarkerControl.ToolTip"));
            // 
            // MainVertialSplit
            // 
            resources.ApplyResources(this.MainVertialSplit, "MainVertialSplit");
            this.MainVertialSplit.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.MainVertialSplit.Name = "MainVertialSplit";
            // 
            // MainVertialSplit.Panel1
            // 
            resources.ApplyResources(this.MainVertialSplit.Panel1, "MainVertialSplit.Panel1");
            this.toolTip1.SetToolTip(this.MainVertialSplit.Panel1, resources.GetString("MainVertialSplit.Panel1.ToolTip"));
            // 
            // MainVertialSplit.Panel2
            // 
            resources.ApplyResources(this.MainVertialSplit.Panel2, "MainVertialSplit.Panel2");
            this.MainVertialSplit.Panel2.Controls.Add(this.BranchesMarkersSplit);
            this.toolTip1.SetToolTip(this.MainVertialSplit.Panel2, resources.GetString("MainVertialSplit.Panel2.ToolTip"));
            this.toolTip1.SetToolTip(this.MainVertialSplit, resources.GetString("MainVertialSplit.ToolTip"));
            this.MainVertialSplit.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.MainVerticalSplit_SplitterMoved);
            // 
            // TASMenu
            // 
            resources.ApplyResources(this.TASMenu, "TASMenu");
            this.TASMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu,
            this.EditSubMenu,
            this.MetaSubMenu,
            this.SettingsSubMenu,
            this.ColumnsSubMenu,
            this.HelpSubMenu});
            this.TASMenu.ShowItemToolTips = true;
            this.toolTip1.SetToolTip(this.TASMenu, resources.GetString("TASMenu.ToolTip"));
            // 
            // FileSubMenu
            // 
            resources.ApplyResources(this.FileSubMenu, "FileSubMenu");
            this.FileSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.NewTASMenuItem,
            this.NewFromSubMenu,
            this.OpenTASMenuItem,
            this.SaveTASMenuItem,
            this.SaveAsTASMenuItem,
            this.SaveBackupMenuItem,
            this.SaveBk2BackupMenuItem,
            this.RecentSubMenu,
            this.toolStripSeparator1,
            this.saveSelectionToMacroToolStripMenuItem,
            this.placeMacroAtSelectionToolStripMenuItem,
            this.recentMacrosToolStripMenuItem,
            this.toolStripSeparator20,
            this.ToBk2MenuItem});
            this.FileSubMenu.DropDownOpened += new System.EventHandler(this.FileSubMenu_DropDownOpened);
            // 
            // NewTASMenuItem
            // 
            resources.ApplyResources(this.NewTASMenuItem, "NewTASMenuItem");
            this.NewTASMenuItem.Click += new System.EventHandler(this.NewTasMenuItem_Click);
            // 
            // NewFromSubMenu
            // 
            resources.ApplyResources(this.NewFromSubMenu, "NewFromSubMenu");
            this.NewFromSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.NewFromNowMenuItem,
            this.NewFromCurrentSaveRamMenuItem});
            this.NewFromSubMenu.DropDownOpened += new System.EventHandler(this.NewFromSubMenu_DropDownOpened);
            // 
            // NewFromNowMenuItem
            // 
            resources.ApplyResources(this.NewFromNowMenuItem, "NewFromNowMenuItem");
            this.NewFromNowMenuItem.Click += new System.EventHandler(this.StartNewProjectFromNowMenuItem_Click);
            // 
            // NewFromCurrentSaveRamMenuItem
            // 
            resources.ApplyResources(this.NewFromCurrentSaveRamMenuItem, "NewFromCurrentSaveRamMenuItem");
            this.NewFromCurrentSaveRamMenuItem.Click += new System.EventHandler(this.StartANewProjectFromSaveRamMenuItem_Click);
            // 
            // OpenTASMenuItem
            // 
            resources.ApplyResources(this.OpenTASMenuItem, "OpenTASMenuItem");
            this.OpenTASMenuItem.Click += new System.EventHandler(this.OpenTasMenuItem_Click);
            // 
            // SaveTASMenuItem
            // 
            resources.ApplyResources(this.SaveTASMenuItem, "SaveTASMenuItem");
            this.SaveTASMenuItem.Click += new System.EventHandler(this.SaveTasMenuItem_Click);
            // 
            // SaveAsTASMenuItem
            // 
            resources.ApplyResources(this.SaveAsTASMenuItem, "SaveAsTASMenuItem");
            this.SaveAsTASMenuItem.Click += new System.EventHandler(this.SaveAsTasMenuItem_Click);
            // 
            // SaveBackupMenuItem
            // 
            resources.ApplyResources(this.SaveBackupMenuItem, "SaveBackupMenuItem");
            this.SaveBackupMenuItem.Click += new System.EventHandler(this.SaveBackupMenuItem_Click);
            // 
            // SaveBk2BackupMenuItem
            // 
            resources.ApplyResources(this.SaveBk2BackupMenuItem, "SaveBk2BackupMenuItem");
            this.SaveBk2BackupMenuItem.Click += new System.EventHandler(this.SaveBk2BackupMenuItem_Click);
            // 
            // RecentSubMenu
            // 
            resources.ApplyResources(this.RecentSubMenu, "RecentSubMenu");
            this.RecentSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator3});
            this.RecentSubMenu.DropDownOpened += new System.EventHandler(this.RecentSubMenu_DropDownOpened);
            // 
            // toolStripSeparator3
            // 
            resources.ApplyResources(this.toolStripSeparator3, "toolStripSeparator3");
            // 
            // toolStripSeparator1
            // 
            resources.ApplyResources(this.toolStripSeparator1, "toolStripSeparator1");
            // 
            // saveSelectionToMacroToolStripMenuItem
            // 
            resources.ApplyResources(this.saveSelectionToMacroToolStripMenuItem, "saveSelectionToMacroToolStripMenuItem");
            this.saveSelectionToMacroToolStripMenuItem.Click += new System.EventHandler(this.SaveSelectionToMacroMenuItem_Click);
            // 
            // placeMacroAtSelectionToolStripMenuItem
            // 
            resources.ApplyResources(this.placeMacroAtSelectionToolStripMenuItem, "placeMacroAtSelectionToolStripMenuItem");
            this.placeMacroAtSelectionToolStripMenuItem.Click += new System.EventHandler(this.PlaceMacroAtSelectionMenuItem_Click);
            // 
            // recentMacrosToolStripMenuItem
            // 
            resources.ApplyResources(this.recentMacrosToolStripMenuItem, "recentMacrosToolStripMenuItem");
            this.recentMacrosToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator22});
            this.recentMacrosToolStripMenuItem.DropDownOpened += new System.EventHandler(this.RecentMacrosMenuItem_DropDownOpened);
            // 
            // toolStripSeparator22
            // 
            resources.ApplyResources(this.toolStripSeparator22, "toolStripSeparator22");
            // 
            // toolStripSeparator20
            // 
            resources.ApplyResources(this.toolStripSeparator20, "toolStripSeparator20");
            // 
            // ToBk2MenuItem
            // 
            resources.ApplyResources(this.ToBk2MenuItem, "ToBk2MenuItem");
            this.ToBk2MenuItem.Click += new System.EventHandler(this.ToBk2MenuItem_Click);
            // 
            // EditSubMenu
            // 
            resources.ApplyResources(this.EditSubMenu, "EditSubMenu");
            this.EditSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.UndoMenuItem,
            this.RedoMenuItem,
            this.showUndoHistoryToolStripMenuItem,
            this.SelectionUndoMenuItem,
            this.SelectionRedoMenuItem,
            this.toolStripSeparator5,
            this.DeselectMenuItem,
            this.SelectBetweenMarkersMenuItem,
            this.SelectAllMenuItem,
            this.ReselectClipboardMenuItem,
            this.GoToFrameMenuItem,
            this.toolStripSeparator7,
            this.CopyMenuItem,
            this.PasteMenuItem,
            this.PasteInsertMenuItem,
            this.CutMenuItem,
            this.toolStripSeparator8,
            this.ClearFramesMenuItem,
            this.DeleteFramesMenuItem,
            this.InsertFrameMenuItem,
            this.InsertNumFramesMenuItem,
            this.CloneFramesMenuItem,
            this.CloneFramesXTimesMenuItem,
            this.toolStripSeparator6,
            this.TruncateMenuItem,
            this.ClearGreenzoneMenuItem,
            this.GreenzoneICheckSeparator,
            this.StateHistoryIntegrityCheckMenuItem});
            this.EditSubMenu.DropDownClosed += new System.EventHandler(this.EditSubMenu_DropDownClosed);
            this.EditSubMenu.DropDownOpened += new System.EventHandler(this.EditSubMenu_DropDownOpened);
            // 
            // UndoMenuItem
            // 
            resources.ApplyResources(this.UndoMenuItem, "UndoMenuItem");
            this.UndoMenuItem.Click += new System.EventHandler(this.UndoMenuItem_Click);
            // 
            // RedoMenuItem
            // 
            resources.ApplyResources(this.RedoMenuItem, "RedoMenuItem");
            this.RedoMenuItem.Click += new System.EventHandler(this.RedoMenuItem_Click);
            // 
            // showUndoHistoryToolStripMenuItem
            // 
            resources.ApplyResources(this.showUndoHistoryToolStripMenuItem, "showUndoHistoryToolStripMenuItem");
            this.showUndoHistoryToolStripMenuItem.Click += new System.EventHandler(this.ShowUndoHistoryMenuItem_Click);
            // 
            // SelectionUndoMenuItem
            // 
            resources.ApplyResources(this.SelectionUndoMenuItem, "SelectionUndoMenuItem");
            // 
            // SelectionRedoMenuItem
            // 
            resources.ApplyResources(this.SelectionRedoMenuItem, "SelectionRedoMenuItem");
            // 
            // toolStripSeparator5
            // 
            resources.ApplyResources(this.toolStripSeparator5, "toolStripSeparator5");
            // 
            // DeselectMenuItem
            // 
            resources.ApplyResources(this.DeselectMenuItem, "DeselectMenuItem");
            this.DeselectMenuItem.Click += new System.EventHandler(this.DeselectMenuItem_Click);
            // 
            // SelectBetweenMarkersMenuItem
            // 
            resources.ApplyResources(this.SelectBetweenMarkersMenuItem, "SelectBetweenMarkersMenuItem");
            this.SelectBetweenMarkersMenuItem.Click += new System.EventHandler(this.SelectBetweenMarkersMenuItem_Click);
            // 
            // SelectAllMenuItem
            // 
            resources.ApplyResources(this.SelectAllMenuItem, "SelectAllMenuItem");
            this.SelectAllMenuItem.Click += new System.EventHandler(this.SelectAllMenuItem_Click);
            // 
            // ReselectClipboardMenuItem
            // 
            resources.ApplyResources(this.ReselectClipboardMenuItem, "ReselectClipboardMenuItem");
            this.ReselectClipboardMenuItem.Click += new System.EventHandler(this.ReselectClipboardMenuItem_Click);
            // 
            // GoToFrameMenuItem
            // 
            resources.ApplyResources(this.GoToFrameMenuItem, "GoToFrameMenuItem");
            this.GoToFrameMenuItem.Click += new System.EventHandler(this.GoToFrameMenuItem_Click);
            // 
            // toolStripSeparator7
            // 
            resources.ApplyResources(this.toolStripSeparator7, "toolStripSeparator7");
            // 
            // CopyMenuItem
            // 
            resources.ApplyResources(this.CopyMenuItem, "CopyMenuItem");
            this.CopyMenuItem.Click += new System.EventHandler(this.CopyMenuItem_Click);
            // 
            // PasteMenuItem
            // 
            resources.ApplyResources(this.PasteMenuItem, "PasteMenuItem");
            this.PasteMenuItem.Click += new System.EventHandler(this.PasteMenuItem_Click);
            // 
            // PasteInsertMenuItem
            // 
            resources.ApplyResources(this.PasteInsertMenuItem, "PasteInsertMenuItem");
            this.PasteInsertMenuItem.Click += new System.EventHandler(this.PasteInsertMenuItem_Click);
            // 
            // CutMenuItem
            // 
            resources.ApplyResources(this.CutMenuItem, "CutMenuItem");
            this.CutMenuItem.Click += new System.EventHandler(this.CutMenuItem_Click);
            // 
            // toolStripSeparator8
            // 
            resources.ApplyResources(this.toolStripSeparator8, "toolStripSeparator8");
            // 
            // ClearFramesMenuItem
            // 
            resources.ApplyResources(this.ClearFramesMenuItem, "ClearFramesMenuItem");
            this.ClearFramesMenuItem.Click += new System.EventHandler(this.ClearFramesMenuItem_Click);
            // 
            // DeleteFramesMenuItem
            // 
            resources.ApplyResources(this.DeleteFramesMenuItem, "DeleteFramesMenuItem");
            this.DeleteFramesMenuItem.Click += new System.EventHandler(this.DeleteFramesMenuItem_Click);
            // 
            // InsertFrameMenuItem
            // 
            resources.ApplyResources(this.InsertFrameMenuItem, "InsertFrameMenuItem");
            this.InsertFrameMenuItem.Click += new System.EventHandler(this.InsertFrameMenuItem_Click);
            // 
            // InsertNumFramesMenuItem
            // 
            resources.ApplyResources(this.InsertNumFramesMenuItem, "InsertNumFramesMenuItem");
            this.InsertNumFramesMenuItem.Click += new System.EventHandler(this.InsertNumFramesMenuItem_Click);
            // 
            // CloneFramesMenuItem
            // 
            resources.ApplyResources(this.CloneFramesMenuItem, "CloneFramesMenuItem");
            this.CloneFramesMenuItem.Click += new System.EventHandler(this.CloneFramesMenuItem_Click);
            // 
            // CloneFramesXTimesMenuItem
            // 
            resources.ApplyResources(this.CloneFramesXTimesMenuItem, "CloneFramesXTimesMenuItem");
            this.CloneFramesXTimesMenuItem.Click += new System.EventHandler(this.CloneFramesXTimesMenuItem_Click);
            // 
            // toolStripSeparator6
            // 
            resources.ApplyResources(this.toolStripSeparator6, "toolStripSeparator6");
            // 
            // TruncateMenuItem
            // 
            resources.ApplyResources(this.TruncateMenuItem, "TruncateMenuItem");
            this.TruncateMenuItem.Click += new System.EventHandler(this.TruncateMenuItem_Click);
            // 
            // ClearGreenzoneMenuItem
            // 
            resources.ApplyResources(this.ClearGreenzoneMenuItem, "ClearGreenzoneMenuItem");
            this.ClearGreenzoneMenuItem.Click += new System.EventHandler(this.ClearGreenzoneMenuItem_Click);
            // 
            // GreenzoneICheckSeparator
            // 
            resources.ApplyResources(this.GreenzoneICheckSeparator, "GreenzoneICheckSeparator");
            // 
            // StateHistoryIntegrityCheckMenuItem
            // 
            resources.ApplyResources(this.StateHistoryIntegrityCheckMenuItem, "StateHistoryIntegrityCheckMenuItem");
            this.StateHistoryIntegrityCheckMenuItem.Click += new System.EventHandler(this.StateHistoryIntegrityCheckMenuItem_Click);
            // 
            // MetaSubMenu
            // 
            resources.ApplyResources(this.MetaSubMenu, "MetaSubMenu");
            this.MetaSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.HeaderMenuItem,
            this.CommentsMenuItem,
            this.SubtitlesMenuItem});
            // 
            // HeaderMenuItem
            // 
            resources.ApplyResources(this.HeaderMenuItem, "HeaderMenuItem");
            this.HeaderMenuItem.Click += new System.EventHandler(this.HeaderMenuItem_Click);
            // 
            // CommentsMenuItem
            // 
            resources.ApplyResources(this.CommentsMenuItem, "CommentsMenuItem");
            this.CommentsMenuItem.Click += new System.EventHandler(this.CommentsMenuItem_Click);
            // 
            // SubtitlesMenuItem
            // 
            resources.ApplyResources(this.SubtitlesMenuItem, "SubtitlesMenuItem");
            this.SubtitlesMenuItem.Click += new System.EventHandler(this.SubtitlesMenuItem_Click);
            // 
            // SettingsSubMenu
            // 
            resources.ApplyResources(this.SettingsSubMenu, "SettingsSubMenu");
            this.SettingsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.TAStudioSettingsToolStripMenuItem});
            // 
            // TAStudioSettingsToolStripMenuItem
            // 
            resources.ApplyResources(this.TAStudioSettingsToolStripMenuItem, "TAStudioSettingsToolStripMenuItem");
            this.TAStudioSettingsToolStripMenuItem.Name = "TAStudioSettingsToolStripMenuItem";
            this.TAStudioSettingsToolStripMenuItem.Click += new System.EventHandler(this.TAStudioSettingsToolStripMenuItem_Click);
            // 
            // ColumnsSubMenu
            // 
            resources.ApplyResources(this.ColumnsSubMenu, "ColumnsSubMenu");
            // 
            // HelpSubMenu
            // 
            resources.ApplyResources(this.HelpSubMenu, "HelpSubMenu");
            this.HelpSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.TASEditorManualOnlineMenuItem,
            this.ForumThreadMenuItem,
            this.aboutToolStripMenuItem});
            // 
            // TASEditorManualOnlineMenuItem
            // 
            resources.ApplyResources(this.TASEditorManualOnlineMenuItem, "TASEditorManualOnlineMenuItem");
            this.TASEditorManualOnlineMenuItem.Click += new System.EventHandler(this.TASEditorManualOnlineMenuItem_Click);
            // 
            // ForumThreadMenuItem
            // 
            resources.ApplyResources(this.ForumThreadMenuItem, "ForumThreadMenuItem");
            this.ForumThreadMenuItem.Click += new System.EventHandler(this.ForumThreadMenuItem_Click);
            // 
            // aboutToolStripMenuItem
            // 
            resources.ApplyResources(this.aboutToolStripMenuItem, "aboutToolStripMenuItem");
            // 
            // toolStripSeparator19
            // 
            resources.ApplyResources(this.toolStripSeparator19, "toolStripSeparator19");
            // 
            // TasStatusStrip
            // 
            resources.ApplyResources(this.TasStatusStrip, "TasStatusStrip");
            this.TasStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MessageStatusLabel,
            this.ProgressBar,
            this.toolStripStatusLabel2,
            this.SplicerStatusLabel});
            this.TasStatusStrip.Name = "TasStatusStrip";
            this.toolTip1.SetToolTip(this.TasStatusStrip, resources.GetString("TasStatusStrip.ToolTip"));
            // 
            // MessageStatusLabel
            // 
            resources.ApplyResources(this.MessageStatusLabel, "MessageStatusLabel");
            this.MessageStatusLabel.Name = "MessageStatusLabel";
            // 
            // ProgressBar
            // 
            resources.ApplyResources(this.ProgressBar, "ProgressBar");
            this.ProgressBar.Name = "ProgressBar";
            // 
            // toolStripStatusLabel2
            // 
            resources.ApplyResources(this.toolStripStatusLabel2, "toolStripStatusLabel2");
            this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            this.toolStripStatusLabel2.Spring = true;
            // 
            // SplicerStatusLabel
            // 
            resources.ApplyResources(this.SplicerStatusLabel, "SplicerStatusLabel");
            this.SplicerStatusLabel.Name = "SplicerStatusLabel";
            this.SplicerStatusLabel.Padding = new System.Windows.Forms.Padding(20, 0, 0, 0);
            // 
            // RightClickMenu
            // 
            resources.ApplyResources(this.RightClickMenu, "RightClickMenu");
            this.RightClickMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SetMarkersContextMenuItem,
            this.SetMarkerWithTextContextMenuItem,
            this.RemoveMarkersContextMenuItem,
            this.toolStripSeparator15,
            this.DeselectContextMenuItem,
            this.SelectBetweenMarkersContextMenuItem,
            this.toolStripSeparator16,
            this.UngreenzoneContextMenuItem,
            this.CancelSeekContextMenuItem,
            this.toolStripSeparator17,
            this.copyToolStripMenuItem,
            this.pasteToolStripMenuItem,
            this.pasteInsertToolStripMenuItem,
            this.cutToolStripMenuItem,
            this.separateToolStripMenuItem,
            this.ClearContextMenuItem,
            this.DeleteFramesContextMenuItem,
            this.InsertFrameContextMenuItem,
            this.InsertNumFramesContextMenuItem,
            this.CloneContextMenuItem,
            this.CloneXTimesContextMenuItem,
            this.toolStripSeparator18,
            this.TruncateContextMenuItem,
            this.BranchContextMenuItem,
            this.StartFromNowSeparator,
            this.StartNewProjectFromNowMenuItem,
            this.StartANewProjectFromSaveRamMenuItem});
            this.RightClickMenu.Name = "RightClickMenu";
            this.toolTip1.SetToolTip(this.RightClickMenu, resources.GetString("RightClickMenu.ToolTip"));
            this.RightClickMenu.Opened += new System.EventHandler(this.RightClickMenu_Opened);
            // 
            // SetMarkersContextMenuItem
            // 
            resources.ApplyResources(this.SetMarkersContextMenuItem, "SetMarkersContextMenuItem");
            this.SetMarkersContextMenuItem.Click += new System.EventHandler(this.SetMarkersMenuItem_Click);
            // 
            // SetMarkerWithTextContextMenuItem
            // 
            resources.ApplyResources(this.SetMarkerWithTextContextMenuItem, "SetMarkerWithTextContextMenuItem");
            this.SetMarkerWithTextContextMenuItem.Click += new System.EventHandler(this.SetMarkerWithTextMenuItem_Click);
            // 
            // RemoveMarkersContextMenuItem
            // 
            resources.ApplyResources(this.RemoveMarkersContextMenuItem, "RemoveMarkersContextMenuItem");
            this.RemoveMarkersContextMenuItem.Click += new System.EventHandler(this.RemoveMarkersMenuItem_Click);
            // 
            // toolStripSeparator15
            // 
            resources.ApplyResources(this.toolStripSeparator15, "toolStripSeparator15");
            // 
            // DeselectContextMenuItem
            // 
            resources.ApplyResources(this.DeselectContextMenuItem, "DeselectContextMenuItem");
            this.DeselectContextMenuItem.Click += new System.EventHandler(this.DeselectMenuItem_Click);
            // 
            // SelectBetweenMarkersContextMenuItem
            // 
            resources.ApplyResources(this.SelectBetweenMarkersContextMenuItem, "SelectBetweenMarkersContextMenuItem");
            this.SelectBetweenMarkersContextMenuItem.Click += new System.EventHandler(this.SelectBetweenMarkersMenuItem_Click);
            // 
            // toolStripSeparator16
            // 
            resources.ApplyResources(this.toolStripSeparator16, "toolStripSeparator16");
            // 
            // UngreenzoneContextMenuItem
            // 
            resources.ApplyResources(this.UngreenzoneContextMenuItem, "UngreenzoneContextMenuItem");
            this.UngreenzoneContextMenuItem.Click += new System.EventHandler(this.ClearGreenzoneMenuItem_Click);
            // 
            // CancelSeekContextMenuItem
            // 
            resources.ApplyResources(this.CancelSeekContextMenuItem, "CancelSeekContextMenuItem");
            this.CancelSeekContextMenuItem.Click += new System.EventHandler(this.CancelSeekContextMenuItem_Click);
            // 
            // toolStripSeparator17
            // 
            resources.ApplyResources(this.toolStripSeparator17, "toolStripSeparator17");
            // 
            // copyToolStripMenuItem
            // 
            resources.ApplyResources(this.copyToolStripMenuItem, "copyToolStripMenuItem");
            this.copyToolStripMenuItem.Click += new System.EventHandler(this.CopyMenuItem_Click);
            // 
            // pasteToolStripMenuItem
            // 
            resources.ApplyResources(this.pasteToolStripMenuItem, "pasteToolStripMenuItem");
            this.pasteToolStripMenuItem.Click += new System.EventHandler(this.PasteMenuItem_Click);
            // 
            // pasteInsertToolStripMenuItem
            // 
            resources.ApplyResources(this.pasteInsertToolStripMenuItem, "pasteInsertToolStripMenuItem");
            this.pasteInsertToolStripMenuItem.Click += new System.EventHandler(this.PasteInsertMenuItem_Click);
            // 
            // cutToolStripMenuItem
            // 
            resources.ApplyResources(this.cutToolStripMenuItem, "cutToolStripMenuItem");
            this.cutToolStripMenuItem.Click += new System.EventHandler(this.CutMenuItem_Click);
            // 
            // separateToolStripMenuItem
            // 
            resources.ApplyResources(this.separateToolStripMenuItem, "separateToolStripMenuItem");
            // 
            // ClearContextMenuItem
            // 
            resources.ApplyResources(this.ClearContextMenuItem, "ClearContextMenuItem");
            this.ClearContextMenuItem.Click += new System.EventHandler(this.ClearFramesMenuItem_Click);
            // 
            // DeleteFramesContextMenuItem
            // 
            resources.ApplyResources(this.DeleteFramesContextMenuItem, "DeleteFramesContextMenuItem");
            this.DeleteFramesContextMenuItem.Click += new System.EventHandler(this.DeleteFramesMenuItem_Click);
            // 
            // InsertFrameContextMenuItem
            // 
            resources.ApplyResources(this.InsertFrameContextMenuItem, "InsertFrameContextMenuItem");
            this.InsertFrameContextMenuItem.Click += new System.EventHandler(this.InsertFrameMenuItem_Click);
            // 
            // InsertNumFramesContextMenuItem
            // 
            resources.ApplyResources(this.InsertNumFramesContextMenuItem, "InsertNumFramesContextMenuItem");
            this.InsertNumFramesContextMenuItem.Click += new System.EventHandler(this.InsertNumFramesMenuItem_Click);
            // 
            // CloneContextMenuItem
            // 
            resources.ApplyResources(this.CloneContextMenuItem, "CloneContextMenuItem");
            this.CloneContextMenuItem.Click += new System.EventHandler(this.CloneFramesMenuItem_Click);
            // 
            // CloneXTimesContextMenuItem
            // 
            resources.ApplyResources(this.CloneXTimesContextMenuItem, "CloneXTimesContextMenuItem");
            this.CloneXTimesContextMenuItem.Click += new System.EventHandler(this.CloneFramesXTimesMenuItem_Click);
            // 
            // toolStripSeparator18
            // 
            resources.ApplyResources(this.toolStripSeparator18, "toolStripSeparator18");
            // 
            // TruncateContextMenuItem
            // 
            resources.ApplyResources(this.TruncateContextMenuItem, "TruncateContextMenuItem");
            this.TruncateContextMenuItem.Click += new System.EventHandler(this.TruncateMenuItem_Click);
            // 
            // BranchContextMenuItem
            // 
            resources.ApplyResources(this.BranchContextMenuItem, "BranchContextMenuItem");
            this.BranchContextMenuItem.Click += new System.EventHandler(this.BranchContextMenuItem_Click);
            // 
            // StartFromNowSeparator
            // 
            resources.ApplyResources(this.StartFromNowSeparator, "StartFromNowSeparator");
            // 
            // StartNewProjectFromNowMenuItem
            // 
            resources.ApplyResources(this.StartNewProjectFromNowMenuItem, "StartNewProjectFromNowMenuItem");
            this.StartNewProjectFromNowMenuItem.Click += new System.EventHandler(this.StartNewProjectFromNowMenuItem_Click);
            // 
            // StartANewProjectFromSaveRamMenuItem
            // 
            resources.ApplyResources(this.StartANewProjectFromSaveRamMenuItem, "StartANewProjectFromSaveRamMenuItem");
            this.StartANewProjectFromSaveRamMenuItem.Click += new System.EventHandler(this.StartANewProjectFromSaveRamMenuItem_Click);
            // 
            // ColumnRightClickMenu
            // 
            resources.ApplyResources(this.ColumnRightClickMenu, "ColumnRightClickMenu");
            this.ColumnRightClickMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.AutoHoldContextMenuItem,
            this.HideColumnContextMenuItem,
            this.ShowColumnsContextMenuItem,
            this.NewInputRollContextMenuItem,
            this.DeleteInputRollContextMenuItem});
            this.ColumnRightClickMenu.Name = "ColumnRightClickMenu";
            this.toolTip1.SetToolTip(this.ColumnRightClickMenu, resources.GetString("ColumnRightClickMenu.ToolTip"));
            this.ColumnRightClickMenu.Opened += new System.EventHandler(this.ColumnRightClickMenu_Opened);
            // 
            // AutoHoldContextMenuItem
            // 
            resources.ApplyResources(this.AutoHoldContextMenuItem, "AutoHoldContextMenuItem");
            this.AutoHoldContextMenuItem.Name = "AutoHoldContextMenuItem";
            this.AutoHoldContextMenuItem.Click += new System.EventHandler(this.AutoHoldContextMenuItem_Click);
            // 
            // HideColumnContextMenuItem
            // 
            resources.ApplyResources(this.HideColumnContextMenuItem, "HideColumnContextMenuItem");
            this.HideColumnContextMenuItem.Name = "HideColumnContextMenuItem";
            this.HideColumnContextMenuItem.Click += new System.EventHandler(this.HideColumnContextMenuItem_Click);
            // 
            // ShowColumnsContextMenuItem
            // 
            resources.ApplyResources(this.ShowColumnsContextMenuItem, "ShowColumnsContextMenuItem");
            this.ShowColumnsContextMenuItem.Name = "ShowColumnsContextMenuItem";
            // 
            // NewInputRollContextMenuItem
            // 
            resources.ApplyResources(this.NewInputRollContextMenuItem, "NewInputRollContextMenuItem");
            this.NewInputRollContextMenuItem.Name = "NewInputRollContextMenuItem";
            this.NewInputRollContextMenuItem.Click += new System.EventHandler(this.NewInputRollContextMenuItem_Click);
            // 
            // DeleteInputRollContextMenuItem
            // 
            resources.ApplyResources(this.DeleteInputRollContextMenuItem, "DeleteInputRollContextMenuItem");
            this.DeleteInputRollContextMenuItem.Name = "DeleteInputRollContextMenuItem";
            this.DeleteInputRollContextMenuItem.Click += new System.EventHandler(this.DeleteInputRollContextMenuItem_Click);
            // 
            // TAStudio
            // 
            resources.ApplyResources(this, "$this");
            this.AllowDrop = true;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.MainVertialSplit);
            this.Controls.Add(this.TasStatusStrip);
            this.Controls.Add(this.TASMenu);
            this.KeyPreview = true;
            this.MainMenuStrip = this.TASMenu;
            this.Name = "TAStudio";
            this.toolTip1.SetToolTip(this, resources.GetString("$this.ToolTip"));
            this.Deactivate += new System.EventHandler(this.TAStudio_Deactivate);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Tastudio_Closing);
            this.Load += new System.EventHandler(this.Tastudio_Load);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.TAStudio_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.DragEnterWrapper);
            this.Resize += new System.EventHandler(this.TAStudio_Resize);
            this.BranchesMarkersSplit.Panel1.ResumeLayout(false);
            this.BranchesMarkersSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.BranchesMarkersSplit)).EndInit();
            this.BranchesMarkersSplit.ResumeLayout(false);
            this.MainVertialSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.MainVertialSplit)).EndInit();
            this.MainVertialSplit.ResumeLayout(false);
            this.TASMenu.ResumeLayout(false);
            this.TASMenu.PerformLayout();
            this.TasStatusStrip.ResumeLayout(false);
            this.TasStatusStrip.PerformLayout();
            this.RightClickMenu.ResumeLayout(false);
            this.ColumnRightClickMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private MenuStripEx TASMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FileSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx NewTASMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx OpenTASMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SaveTASMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SaveAsTASMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx EditSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RecentSubMenu;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator3;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx InsertFrameMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator7;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CloneFramesMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CloneFramesXTimesMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DeleteFramesMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ClearFramesMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx InsertNumFramesMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SelectAllMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator8;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx TruncateMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CopyMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx PasteMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx PasteInsertMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CutMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx UndoMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RedoMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SelectionUndoMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SelectionRedoMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator5;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DeselectMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SelectBetweenMarkersMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ReselectClipboardMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx GoToFrameMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator6;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx HelpSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx aboutToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SettingsSubMenu;
		private StatusStripEx TasStatusStrip;
		private System.Windows.Forms.ToolStripStatusLabel MessageStatusLabel;
		public PlaybackBox TasPlaybackBox;
		private System.Windows.Forms.ToolStripStatusLabel SplicerStatusLabel;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx MetaSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx HeaderMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CommentsMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SubtitlesMenuItem;
		private MarkerControl MarkerControl;
		private System.Windows.Forms.ContextMenuStrip RightClickMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SetMarkersContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RemoveMarkersContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator15;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DeselectContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SelectBetweenMarkersContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator16;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx UngreenzoneContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator17;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ClearContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DeleteFramesContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx InsertFrameContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx InsertNumFramesContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CloneContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CloneXTimesContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator18;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx TruncateContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ClearGreenzoneMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx GreenzoneICheckSeparator;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx StateHistoryIntegrityCheckMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ColumnsSubMenu;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator19;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CancelSeekContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx StartFromNowSeparator;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx StartNewProjectFromNowMenuItem;
		private System.Windows.Forms.ToolStripProgressBar ProgressBar;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel2;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx copyToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx pasteToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx separateToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx pasteInsertToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx cutToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx showUndoHistoryToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx saveSelectionToMacroToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx placeMacroAtSelectionToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator20;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ToBk2MenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx recentMacrosToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator22;
		private BookmarksBranchesBox BookMarkControl;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx BranchContextMenuItem;
		private System.Windows.Forms.SplitContainer BranchesMarkersSplit;
		private System.Windows.Forms.SplitContainer MainVertialSplit;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx StartANewProjectFromSaveRamMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx NewFromSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx NewFromNowMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx NewFromCurrentSaveRamMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SetMarkerWithTextContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx TASEditorManualOnlineMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ForumThreadMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SaveBackupMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SaveBk2BackupMenuItem;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.ToolStripMenuItem TAStudioSettingsToolStripMenuItem;
		private System.Windows.Forms.ContextMenuStrip ColumnRightClickMenu;
		private System.Windows.Forms.ToolStripMenuItem AutoHoldContextMenuItem;
		private System.Windows.Forms.ToolStripMenuItem HideColumnContextMenuItem;
		private System.Windows.Forms.ToolStripMenuItem ShowColumnsContextMenuItem;
		private System.Windows.Forms.ToolStripMenuItem NewInputRollContextMenuItem;
		private System.Windows.Forms.ToolStripMenuItem DeleteInputRollContextMenuItem;
	}
}