using BizHawk.Client.Common;

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
			this.TASMenu = new MenuStripEx();
			this.FileSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.NewTASMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.NewFromSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.NewFromNowMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.NewFromCurrentSaveRamMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.OpenTASMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SaveTASMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SaveAsTASMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SaveBackupMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SaveBk2BackupMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.RecentSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.saveSelectionToMacroToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.placeMacroAtSelectionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.recentMacrosToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator22 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripSeparator20 = new System.Windows.Forms.ToolStripSeparator();
			this.ToBk2MenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.ExitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.EditSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.UndoMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.RedoMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.showUndoHistoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SelectionUndoMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SelectionRedoMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.DeselectMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SelectBetweenMarkersMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SelectAllMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ReselectClipboardMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
			this.CopyMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.PasteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.PasteInsertMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.CutMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
			this.ClearFramesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.InsertFrameMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.DeleteFramesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.CloneFramesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.InsertNumFramesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
			this.TruncateMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ClearGreenzoneMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.GreenzoneICheckSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.StateHistoryIntegrityCheckMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ConfigSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.SetMaxUndoLevelsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SetBranchCellHoverIntervalMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SetSeekingCutoffIntervalMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator26 = new System.Windows.Forms.ToolStripSeparator();
			this.autosaveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SetAutosaveIntervalMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.AutosaveAsBk2MenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.AutosaveAsBackupFileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.BackupPerFileSaveMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
			this.AutoRestoreOnMouseUpOnlyMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.AutoadjustInputMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.DrawInputByDraggingMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.applyPatternToPaintedInputToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.onlyOnAutoFireColumnsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SingleClickFloatEditMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.UseInputKeysItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.BindMarkersToInputMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.EmptyNewMarkerNotesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.OldControlSchemeForBranchesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.LoadBranchOnDoubleclickMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.OsdInBranchScreenshotsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator14 = new System.Windows.Forms.ToolStripSeparator();
			this.AutopauseAtEndOfMovieMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.sepToolStripMenuItem = new System.Windows.Forms.ToolStripSeparator();
			this.autoHoldFireToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.keepSetPatternsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.sepToolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			this.autoHoldToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.autoFireToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.customPatternToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.setpToolStripMenuItem = new System.Windows.Forms.ToolStripSeparator();
			this.setCustomsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.MetaSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.HeaderMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.StateHistorySettingsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.CommentsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SubtitlesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator21 = new System.Windows.Forms.ToolStripSeparator();
			this.DefaultStateSettingsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SettingsSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.RotateMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.RendererOptionsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SetRenderer0 = new System.Windows.Forms.ToolStripMenuItem();
			this.SetRenderer1 = new System.Windows.Forms.ToolStripMenuItem();
			this.HideLagFramesSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.HideLagFrames0 = new System.Windows.Forms.ToolStripMenuItem();
			this.HideLagFrames1 = new System.Windows.Forms.ToolStripMenuItem();
			this.HideLagFrames2 = new System.Windows.Forms.ToolStripMenuItem();
			this.HideLagFrames3 = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator12 = new System.Windows.Forms.ToolStripSeparator();
			this.hideWasLagFramesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.iconsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.DenoteStatesWithIconsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.DenoteStatesWithBGColorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.DenoteMarkersWithIconsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.DenoteMarkersWithBGColorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator23 = new System.Windows.Forms.ToolStripSeparator();
			this.followCursorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.alwaysScrollToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator24 = new System.Windows.Forms.ToolStripSeparator();
			this.scrollToViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.scrollToTopToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.scrollToBottomToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.scrollToCenterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator25 = new System.Windows.Forms.ToolStripSeparator();
			this.wheelScrollSpeedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ColumnsSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator19 = new System.Windows.Forms.ToolStripSeparator();
			this.HelpSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.TASEditorManualOnlineMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ForumThreadMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator10 = new System.Windows.Forms.ToolStripSeparator();
			this.EnableTooltipsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.TasView = new BizHawk.Client.EmuHawk.InputRoll();
			this.TasStatusStrip = new StatusStripEx();
			this.MessageStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.SavingProgressBar = new System.Windows.Forms.ToolStripProgressBar();
			this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
			this.SplicerStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.TasPlaybackBox = new BizHawk.Client.EmuHawk.PlaybackBox();
			this.MarkerControl = new BizHawk.Client.EmuHawk.MarkerControl();
			this.RightClickMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.SetMarkersContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SetMarkerWithTextContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.RemoveMarkersContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator15 = new System.Windows.Forms.ToolStripSeparator();
			this.DeselectContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SelectBetweenMarkersContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator16 = new System.Windows.Forms.ToolStripSeparator();
			this.UngreenzoneContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.CancelSeekContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator17 = new System.Windows.Forms.ToolStripSeparator();
			this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.pasteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.pasteInsertToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.cutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.separateToolStripMenuItem = new System.Windows.Forms.ToolStripSeparator();
			this.ClearContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.InsertFrameContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.DeleteFramesContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.CloneContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.InsertNumFramesContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator18 = new System.Windows.Forms.ToolStripSeparator();
			this.TruncateContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.BranchContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.StartFromNowSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.StartNewProjectFromNowMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.StartANewProjectFromSaveRamMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.BookMarkControl = new BizHawk.Client.EmuHawk.BookmarksBranchesBox();
			this.BranchesMarkersSplit = new System.Windows.Forms.SplitContainer();
			this.MainVertialSplit = new System.Windows.Forms.SplitContainer();
			this.TASMenu.SuspendLayout();
			this.TasStatusStrip.SuspendLayout();
			this.RightClickMenu.SuspendLayout();
			this.groupBox1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.BranchesMarkersSplit)).BeginInit();
			this.BranchesMarkersSplit.Panel1.SuspendLayout();
			this.BranchesMarkersSplit.Panel2.SuspendLayout();
			this.BranchesMarkersSplit.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.MainVertialSplit)).BeginInit();
			this.MainVertialSplit.Panel1.SuspendLayout();
			this.MainVertialSplit.Panel2.SuspendLayout();
			this.MainVertialSplit.SuspendLayout();
			this.SuspendLayout();
			// 
			// TASMenu
			// 
			this.TASMenu.ClickThrough = true;
			this.TASMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu,
            this.EditSubMenu,
            this.ConfigSubMenu,
            this.MetaSubMenu,
            this.SettingsSubMenu,
            this.ColumnsSubMenu,
            this.HelpSubMenu});
			this.TASMenu.Location = new System.Drawing.Point(0, 0);
			this.TASMenu.Name = "TASMenu";
			this.TASMenu.Size = new System.Drawing.Size(509, 24);
			this.TASMenu.TabIndex = 0;
			this.TASMenu.Text = "menuStrip1";
			this.TASMenu.MenuActivate += new System.EventHandler(this.TASMenu_MenuActivate);
			this.TASMenu.MenuDeactivate += new System.EventHandler(this.TASMenu_MenuDeactivate);
			// 
			// FileSubMenu
			// 
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
            this.ToBk2MenuItem,
            this.toolStripSeparator2,
            this.ExitMenuItem});
			this.FileSubMenu.Name = "FileSubMenu";
			this.FileSubMenu.Size = new System.Drawing.Size(37, 20);
			this.FileSubMenu.Text = "&File";
			this.FileSubMenu.DropDownOpened += new System.EventHandler(this.FileSubMenu_DropDownOpened);
			// 
			// NewTASMenuItem
			// 
			this.NewTASMenuItem.Name = "NewTASMenuItem";
			this.NewTASMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
			this.NewTASMenuItem.Size = new System.Drawing.Size(203, 22);
			this.NewTASMenuItem.Text = "&New";
			this.NewTASMenuItem.Click += new System.EventHandler(this.NewTasMenuItem_Click);
			// 
			// NewFromSubMenu
			// 
			this.NewFromSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.NewFromNowMenuItem,
            this.NewFromCurrentSaveRamMenuItem});
			this.NewFromSubMenu.Name = "NewFromSubMenu";
			this.NewFromSubMenu.Size = new System.Drawing.Size(203, 22);
			this.NewFromSubMenu.Text = "New From";
			this.NewFromSubMenu.DropDownOpened += new System.EventHandler(this.NewFromSubMenu_DropDownOpened);
			// 
			// NewFromNowMenuItem
			// 
			this.NewFromNowMenuItem.Name = "NewFromNowMenuItem";
			this.NewFromNowMenuItem.Size = new System.Drawing.Size(165, 22);
			this.NewFromNowMenuItem.Text = "&Now";
			this.NewFromNowMenuItem.Click += new System.EventHandler(this.StartNewProjectFromNowMenuItem_Click);
			// 
			// NewFromCurrentSaveRamMenuItem
			// 
			this.NewFromCurrentSaveRamMenuItem.Name = "NewFromCurrentSaveRamMenuItem";
			this.NewFromCurrentSaveRamMenuItem.Size = new System.Drawing.Size(165, 22);
			this.NewFromCurrentSaveRamMenuItem.Text = "&Current SaveRam";
			this.NewFromCurrentSaveRamMenuItem.Click += new System.EventHandler(this.StartANewProjectFromSaveRamMenuItem_Click);
			// 
			// OpenTASMenuItem
			// 
			this.OpenTASMenuItem.Name = "OpenTASMenuItem";
			this.OpenTASMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.OpenTASMenuItem.Size = new System.Drawing.Size(203, 22);
			this.OpenTASMenuItem.Text = "&Open";
			this.OpenTASMenuItem.Click += new System.EventHandler(this.OpenTasMenuItem_Click);
			// 
			// SaveTASMenuItem
			// 
			this.SaveTASMenuItem.Name = "SaveTASMenuItem";
			this.SaveTASMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			this.SaveTASMenuItem.Size = new System.Drawing.Size(203, 22);
			this.SaveTASMenuItem.Text = "&Save";
			this.SaveTASMenuItem.Click += new System.EventHandler(this.SaveTasMenuItem_Click);
			// 
			// SaveAsTASMenuItem
			// 
			this.SaveAsTASMenuItem.Name = "SaveAsTASMenuItem";
			this.SaveAsTASMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.S)));
			this.SaveAsTASMenuItem.Size = new System.Drawing.Size(203, 22);
			this.SaveAsTASMenuItem.Text = "Save As";
			this.SaveAsTASMenuItem.Click += new System.EventHandler(this.SaveAsTasMenuItem_Click);
			// 
			// SaveBackupMenuItem
			// 
			this.SaveBackupMenuItem.Name = "SaveBackupMenuItem";
			this.SaveBackupMenuItem.Size = new System.Drawing.Size(203, 22);
			this.SaveBackupMenuItem.Text = "Save Backup";
			this.SaveBackupMenuItem.Click += new System.EventHandler(this.SaveBackupMenuItem_Click);
			// 
			// SaveBk2BackupMenuItem
			// 
			this.SaveBk2BackupMenuItem.Name = "SaveBk2BackupMenuItem";
			this.SaveBk2BackupMenuItem.Size = new System.Drawing.Size(203, 22);
			this.SaveBk2BackupMenuItem.Text = "Save Bk2 Backup";
			this.SaveBk2BackupMenuItem.Visible = false;
			this.SaveBk2BackupMenuItem.Click += new System.EventHandler(this.SaveBk2BackupMenuItem_Click);
			// 
			// RecentSubMenu
			// 
			this.RecentSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator3});
			this.RecentSubMenu.Image = ((System.Drawing.Image)(resources.GetObject("RecentSubMenu.Image")));
			this.RecentSubMenu.Name = "RecentSubMenu";
			this.RecentSubMenu.Size = new System.Drawing.Size(203, 22);
			this.RecentSubMenu.Text = "Recent";
			this.RecentSubMenu.DropDownOpened += new System.EventHandler(this.RecentSubMenu_DropDownOpened);
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(57, 6);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(200, 6);
			// 
			// saveSelectionToMacroToolStripMenuItem
			// 
			this.saveSelectionToMacroToolStripMenuItem.Name = "saveSelectionToMacroToolStripMenuItem";
			this.saveSelectionToMacroToolStripMenuItem.Size = new System.Drawing.Size(203, 22);
			this.saveSelectionToMacroToolStripMenuItem.Text = "Save Selection to Macro";
			this.saveSelectionToMacroToolStripMenuItem.Click += new System.EventHandler(this.SaveSelectionToMacroMenuItem_Click);
			// 
			// placeMacroAtSelectionToolStripMenuItem
			// 
			this.placeMacroAtSelectionToolStripMenuItem.Name = "placeMacroAtSelectionToolStripMenuItem";
			this.placeMacroAtSelectionToolStripMenuItem.Size = new System.Drawing.Size(203, 22);
			this.placeMacroAtSelectionToolStripMenuItem.Text = "Place Macro at Selection";
			this.placeMacroAtSelectionToolStripMenuItem.Click += new System.EventHandler(this.PlaceMacroAtSelectionMenuItem_Click);
			// 
			// recentMacrosToolStripMenuItem
			// 
			this.recentMacrosToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator22});
			this.recentMacrosToolStripMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Recent;
			this.recentMacrosToolStripMenuItem.Name = "recentMacrosToolStripMenuItem";
			this.recentMacrosToolStripMenuItem.Size = new System.Drawing.Size(203, 22);
			this.recentMacrosToolStripMenuItem.Text = "Recent Macros";
			this.recentMacrosToolStripMenuItem.DropDownOpened += new System.EventHandler(this.RecentMacrosMenuItem_DropDownOpened);
			// 
			// toolStripSeparator22
			// 
			this.toolStripSeparator22.Name = "toolStripSeparator22";
			this.toolStripSeparator22.Size = new System.Drawing.Size(57, 6);
			// 
			// toolStripSeparator20
			// 
			this.toolStripSeparator20.Name = "toolStripSeparator20";
			this.toolStripSeparator20.Size = new System.Drawing.Size(200, 6);
			// 
			// ToBk2MenuItem
			// 
			this.ToBk2MenuItem.Name = "ToBk2MenuItem";
			this.ToBk2MenuItem.Size = new System.Drawing.Size(203, 22);
			this.ToBk2MenuItem.Text = "&Export to Bk2";
			this.ToBk2MenuItem.Click += new System.EventHandler(this.ToBk2MenuItem_Click);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(200, 6);
			// 
			// ExitMenuItem
			// 
			this.ExitMenuItem.Name = "ExitMenuItem";
			this.ExitMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
			this.ExitMenuItem.Size = new System.Drawing.Size(203, 22);
			this.ExitMenuItem.Text = "E&xit";
			this.ExitMenuItem.Click += new System.EventHandler(this.ExitMenuItem_Click);
			// 
			// EditSubMenu
			// 
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
            this.toolStripSeparator7,
            this.CopyMenuItem,
            this.PasteMenuItem,
            this.PasteInsertMenuItem,
            this.CutMenuItem,
            this.toolStripSeparator8,
            this.ClearFramesMenuItem,
            this.InsertFrameMenuItem,
            this.DeleteFramesMenuItem,
            this.CloneFramesMenuItem,
            this.InsertNumFramesMenuItem,
            this.toolStripSeparator6,
            this.TruncateMenuItem,
            this.ClearGreenzoneMenuItem,
            this.GreenzoneICheckSeparator,
            this.StateHistoryIntegrityCheckMenuItem});
			this.EditSubMenu.Name = "EditSubMenu";
			this.EditSubMenu.Size = new System.Drawing.Size(39, 20);
			this.EditSubMenu.Text = "&Edit";
			this.EditSubMenu.DropDownOpened += new System.EventHandler(this.EditSubMenu_DropDownOpened);
			// 
			// UndoMenuItem
			// 
			this.UndoMenuItem.Name = "UndoMenuItem";
			this.UndoMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Z)));
			this.UndoMenuItem.Size = new System.Drawing.Size(293, 22);
			this.UndoMenuItem.Text = "&Undo";
			this.UndoMenuItem.Click += new System.EventHandler(this.UndoMenuItem_Click);
			// 
			// RedoMenuItem
			// 
			this.RedoMenuItem.Enabled = false;
			this.RedoMenuItem.Name = "RedoMenuItem";
			this.RedoMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Y)));
			this.RedoMenuItem.Size = new System.Drawing.Size(293, 22);
			this.RedoMenuItem.Text = "&Redo";
			this.RedoMenuItem.Click += new System.EventHandler(this.RedoMenuItem_Click);
			// 
			// showUndoHistoryToolStripMenuItem
			// 
			this.showUndoHistoryToolStripMenuItem.Name = "showUndoHistoryToolStripMenuItem";
			this.showUndoHistoryToolStripMenuItem.Size = new System.Drawing.Size(293, 22);
			this.showUndoHistoryToolStripMenuItem.Text = "Show Undo History";
			this.showUndoHistoryToolStripMenuItem.Click += new System.EventHandler(this.ShowUndoHistoryMenuItem_Click);
			// 
			// SelectionUndoMenuItem
			// 
			this.SelectionUndoMenuItem.Enabled = false;
			this.SelectionUndoMenuItem.Name = "SelectionUndoMenuItem";
			this.SelectionUndoMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Q)));
			this.SelectionUndoMenuItem.Size = new System.Drawing.Size(293, 22);
			this.SelectionUndoMenuItem.Text = "Selection Undo";
			// 
			// SelectionRedoMenuItem
			// 
			this.SelectionRedoMenuItem.Enabled = false;
			this.SelectionRedoMenuItem.Name = "SelectionRedoMenuItem";
			this.SelectionRedoMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.W)));
			this.SelectionRedoMenuItem.Size = new System.Drawing.Size(293, 22);
			this.SelectionRedoMenuItem.Text = "Selection Redo";
			// 
			// toolStripSeparator5
			// 
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			this.toolStripSeparator5.Size = new System.Drawing.Size(290, 6);
			// 
			// DeselectMenuItem
			// 
			this.DeselectMenuItem.Name = "DeselectMenuItem";
			this.DeselectMenuItem.Size = new System.Drawing.Size(293, 22);
			this.DeselectMenuItem.Text = "Deselect";
			this.DeselectMenuItem.Click += new System.EventHandler(this.DeselectMenuItem_Click);
			// 
			// SelectBetweenMarkersMenuItem
			// 
			this.SelectBetweenMarkersMenuItem.Name = "SelectBetweenMarkersMenuItem";
			this.SelectBetweenMarkersMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
			this.SelectBetweenMarkersMenuItem.Size = new System.Drawing.Size(293, 22);
			this.SelectBetweenMarkersMenuItem.Text = "Select between Markers";
			this.SelectBetweenMarkersMenuItem.Click += new System.EventHandler(this.SelectBetweenMarkersMenuItem_Click);
			// 
			// SelectAllMenuItem
			// 
			this.SelectAllMenuItem.Name = "SelectAllMenuItem";
			this.SelectAllMenuItem.ShortcutKeyDisplayString = "";
			this.SelectAllMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.A)));
			this.SelectAllMenuItem.Size = new System.Drawing.Size(293, 22);
			this.SelectAllMenuItem.Text = "Select &All";
			this.SelectAllMenuItem.Click += new System.EventHandler(this.SelectAllMenuItem_Click);
			// 
			// ReselectClipboardMenuItem
			// 
			this.ReselectClipboardMenuItem.Name = "ReselectClipboardMenuItem";
			this.ReselectClipboardMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.B)));
			this.ReselectClipboardMenuItem.Size = new System.Drawing.Size(293, 22);
			this.ReselectClipboardMenuItem.Text = "Reselect Clipboard";
			this.ReselectClipboardMenuItem.Click += new System.EventHandler(this.ReselectClipboardMenuItem_Click);
			// 
			// toolStripSeparator7
			// 
			this.toolStripSeparator7.Name = "toolStripSeparator7";
			this.toolStripSeparator7.Size = new System.Drawing.Size(290, 6);
			// 
			// CopyMenuItem
			// 
			this.CopyMenuItem.Name = "CopyMenuItem";
			this.CopyMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
			this.CopyMenuItem.Size = new System.Drawing.Size(293, 22);
			this.CopyMenuItem.Text = "Copy";
			this.CopyMenuItem.Click += new System.EventHandler(this.CopyMenuItem_Click);
			// 
			// PasteMenuItem
			// 
			this.PasteMenuItem.Name = "PasteMenuItem";
			this.PasteMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.V)));
			this.PasteMenuItem.Size = new System.Drawing.Size(293, 22);
			this.PasteMenuItem.Text = "&Paste";
			this.PasteMenuItem.Click += new System.EventHandler(this.PasteMenuItem_Click);
			// 
			// PasteInsertMenuItem
			// 
			this.PasteInsertMenuItem.Name = "PasteInsertMenuItem";
			this.PasteInsertMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.V)));
			this.PasteInsertMenuItem.Size = new System.Drawing.Size(293, 22);
			this.PasteInsertMenuItem.Text = "&Paste Insert";
			this.PasteInsertMenuItem.Click += new System.EventHandler(this.PasteInsertMenuItem_Click);
			// 
			// CutMenuItem
			// 
			this.CutMenuItem.Name = "CutMenuItem";
			this.CutMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.X)));
			this.CutMenuItem.Size = new System.Drawing.Size(293, 22);
			this.CutMenuItem.Text = "&Cut";
			this.CutMenuItem.Click += new System.EventHandler(this.CutMenuItem_Click);
			// 
			// toolStripSeparator8
			// 
			this.toolStripSeparator8.Name = "toolStripSeparator8";
			this.toolStripSeparator8.Size = new System.Drawing.Size(290, 6);
			// 
			// ClearFramesMenuItem
			// 
			this.ClearFramesMenuItem.Name = "ClearFramesMenuItem";
			this.ClearFramesMenuItem.ShortcutKeyDisplayString = "";
			this.ClearFramesMenuItem.Size = new System.Drawing.Size(293, 22);
			this.ClearFramesMenuItem.Text = "Clear";
			this.ClearFramesMenuItem.Click += new System.EventHandler(this.ClearFramesMenuItem_Click);
			// 
			// InsertFrameMenuItem
			// 
			this.InsertFrameMenuItem.Name = "InsertFrameMenuItem";
			this.InsertFrameMenuItem.Size = new System.Drawing.Size(293, 22);
			this.InsertFrameMenuItem.Text = "&Insert";
			this.InsertFrameMenuItem.Click += new System.EventHandler(this.InsertFrameMenuItem_Click);
			// 
			// DeleteFramesMenuItem
			// 
			this.DeleteFramesMenuItem.Name = "DeleteFramesMenuItem";
			this.DeleteFramesMenuItem.Size = new System.Drawing.Size(293, 22);
			this.DeleteFramesMenuItem.Text = "&Delete";
			this.DeleteFramesMenuItem.Click += new System.EventHandler(this.DeleteFramesMenuItem_Click);
			// 
			// CloneFramesMenuItem
			// 
			this.CloneFramesMenuItem.Name = "CloneFramesMenuItem";
			this.CloneFramesMenuItem.Size = new System.Drawing.Size(293, 22);
			this.CloneFramesMenuItem.Text = "&Clone";
			this.CloneFramesMenuItem.Click += new System.EventHandler(this.CloneFramesMenuItem_Click);
			// 
			// InsertNumFramesMenuItem
			// 
			this.InsertNumFramesMenuItem.Name = "InsertNumFramesMenuItem";
			this.InsertNumFramesMenuItem.ShortcutKeyDisplayString = "";
			this.InsertNumFramesMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.Insert)));
			this.InsertNumFramesMenuItem.Size = new System.Drawing.Size(293, 22);
			this.InsertNumFramesMenuItem.Text = "Insert # of Frames";
			this.InsertNumFramesMenuItem.Click += new System.EventHandler(this.InsertNumFramesMenuItem_Click);
			// 
			// toolStripSeparator6
			// 
			this.toolStripSeparator6.Name = "toolStripSeparator6";
			this.toolStripSeparator6.Size = new System.Drawing.Size(290, 6);
			// 
			// TruncateMenuItem
			// 
			this.TruncateMenuItem.Name = "TruncateMenuItem";
			this.TruncateMenuItem.Size = new System.Drawing.Size(293, 22);
			this.TruncateMenuItem.Text = "&Truncate Movie";
			this.TruncateMenuItem.Click += new System.EventHandler(this.TruncateMenuItem_Click);
			// 
			// ClearGreenzoneMenuItem
			// 
			this.ClearGreenzoneMenuItem.Name = "ClearGreenzoneMenuItem";
			this.ClearGreenzoneMenuItem.Size = new System.Drawing.Size(293, 22);
			this.ClearGreenzoneMenuItem.Text = "&Clear Savestate History";
			this.ClearGreenzoneMenuItem.Click += new System.EventHandler(this.ClearGreenzoneMenuItem_Click);
			// 
			// GreenzoneICheckSeparator
			// 
			this.GreenzoneICheckSeparator.Name = "GreenzoneICheckSeparator";
			this.GreenzoneICheckSeparator.Size = new System.Drawing.Size(290, 6);
			// 
			// StateHistoryIntegrityCheckMenuItem
			// 
			this.StateHistoryIntegrityCheckMenuItem.Name = "StateHistoryIntegrityCheckMenuItem";
			this.StateHistoryIntegrityCheckMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.I)));
			this.StateHistoryIntegrityCheckMenuItem.Size = new System.Drawing.Size(293, 22);
			this.StateHistoryIntegrityCheckMenuItem.Text = "State History Integrity Check";
			this.StateHistoryIntegrityCheckMenuItem.Click += new System.EventHandler(this.StateHistoryIntegrityCheckMenuItem_Click);
			// 
			// ConfigSubMenu
			// 
			this.ConfigSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SetMaxUndoLevelsMenuItem,
            this.SetBranchCellHoverIntervalMenuItem,
            this.SetSeekingCutoffIntervalMenuItem,
            this.toolStripSeparator26,
            this.autosaveToolStripMenuItem,
            this.BackupPerFileSaveMenuItem,
            this.toolStripSeparator9,
            this.AutoRestoreOnMouseUpOnlyMenuItem,
            this.AutoadjustInputMenuItem,
            this.DrawInputByDraggingMenuItem,
            this.applyPatternToPaintedInputToolStripMenuItem,
            this.onlyOnAutoFireColumnsToolStripMenuItem,
            this.SingleClickFloatEditMenuItem,
            this.UseInputKeysItem,
            this.toolStripSeparator4,
            this.BindMarkersToInputMenuItem,
            this.EmptyNewMarkerNotesMenuItem,
            this.OldControlSchemeForBranchesMenuItem,
            this.LoadBranchOnDoubleclickMenuItem,
            this.OsdInBranchScreenshotsMenuItem,
            this.toolStripSeparator14,
            this.AutopauseAtEndOfMovieMenuItem,
            this.sepToolStripMenuItem,
            this.autoHoldFireToolStripMenuItem});
			this.ConfigSubMenu.Name = "ConfigSubMenu";
			this.ConfigSubMenu.Size = new System.Drawing.Size(55, 20);
			this.ConfigSubMenu.Text = "&Config";
			this.ConfigSubMenu.DropDownOpened += new System.EventHandler(this.ConfigSubMenu_DropDownOpened);
			// 
			// SetMaxUndoLevelsMenuItem
			// 
			this.SetMaxUndoLevelsMenuItem.Name = "SetMaxUndoLevelsMenuItem";
			this.SetMaxUndoLevelsMenuItem.Size = new System.Drawing.Size(264, 22);
			this.SetMaxUndoLevelsMenuItem.Text = "Set max Undo Levels";
			this.SetMaxUndoLevelsMenuItem.Click += new System.EventHandler(this.SetMaxUndoLevelsMenuItem_Click);
			// 
			// SetBranchCellHoverIntervalMenuItem
			// 
			this.SetBranchCellHoverIntervalMenuItem.Name = "SetBranchCellHoverIntervalMenuItem";
			this.SetBranchCellHoverIntervalMenuItem.Size = new System.Drawing.Size(264, 22);
			this.SetBranchCellHoverIntervalMenuItem.Text = "Set Branch Cell Hover Interval";
			this.SetBranchCellHoverIntervalMenuItem.Click += new System.EventHandler(this.SetBranchCellHoverIntervalMenuItem_Click);
			// 
			// SetSeekingCutoffIntervalMenuItem
			// 
			this.SetSeekingCutoffIntervalMenuItem.Name = "SetSeekingCutoffIntervalMenuItem";
			this.SetSeekingCutoffIntervalMenuItem.Size = new System.Drawing.Size(264, 22);
			this.SetSeekingCutoffIntervalMenuItem.Text = "Set Seeking Cutoff Interval";
			this.SetSeekingCutoffIntervalMenuItem.Visible = false;
			this.SetSeekingCutoffIntervalMenuItem.Click += new System.EventHandler(this.SetSeekingCutoffIntervalMenuItem_Click);
			// 
			// toolStripSeparator26
			// 
			this.toolStripSeparator26.Name = "toolStripSeparator26";
			this.toolStripSeparator26.Size = new System.Drawing.Size(261, 6);
			// 
			// autosaveToolStripMenuItem
			// 
			this.autosaveToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SetAutosaveIntervalMenuItem,
            this.AutosaveAsBk2MenuItem,
            this.AutosaveAsBackupFileMenuItem});
			this.autosaveToolStripMenuItem.Name = "autosaveToolStripMenuItem";
			this.autosaveToolStripMenuItem.Size = new System.Drawing.Size(264, 22);
			this.autosaveToolStripMenuItem.Text = "Autosave";
			// 
			// SetAutosaveIntervalMenuItem
			// 
			this.SetAutosaveIntervalMenuItem.Name = "SetAutosaveIntervalMenuItem";
			this.SetAutosaveIntervalMenuItem.Size = new System.Drawing.Size(202, 22);
			this.SetAutosaveIntervalMenuItem.Text = "Set Autosave Interval";
			this.SetAutosaveIntervalMenuItem.Click += new System.EventHandler(this.SetAutosaveIntervalMenuItem_Click);
			// 
			// AutosaveAsBk2MenuItem
			// 
			this.AutosaveAsBk2MenuItem.Name = "AutosaveAsBk2MenuItem";
			this.AutosaveAsBk2MenuItem.Size = new System.Drawing.Size(202, 22);
			this.AutosaveAsBk2MenuItem.Text = "Autosave As Bk2";
			this.AutosaveAsBk2MenuItem.Click += new System.EventHandler(this.AutosaveAsBk2MenuItem_Click);
			// 
			// AutosaveAsBackupFileMenuItem
			// 
			this.AutosaveAsBackupFileMenuItem.Name = "AutosaveAsBackupFileMenuItem";
			this.AutosaveAsBackupFileMenuItem.Size = new System.Drawing.Size(202, 22);
			this.AutosaveAsBackupFileMenuItem.Text = "Autosave As Backup File";
			this.AutosaveAsBackupFileMenuItem.Click += new System.EventHandler(this.AutosaveAsBackupFileMenuItem_Click);
			// 
			// BackupPerFileSaveMenuItem
			// 
			this.BackupPerFileSaveMenuItem.Name = "BackupPerFileSaveMenuItem";
			this.BackupPerFileSaveMenuItem.Size = new System.Drawing.Size(264, 22);
			this.BackupPerFileSaveMenuItem.Text = "Backup Per File Save";
			this.BackupPerFileSaveMenuItem.Click += new System.EventHandler(this.BackupPerFileSaveMenuItem_Click);
			// 
			// toolStripSeparator9
			// 
			this.toolStripSeparator9.Name = "toolStripSeparator9";
			this.toolStripSeparator9.Size = new System.Drawing.Size(261, 6);
			// 
			// AutoRestoreOnMouseUpOnlyMenuItem
			// 
			this.AutoRestoreOnMouseUpOnlyMenuItem.Name = "AutoRestoreOnMouseUpOnlyMenuItem";
			this.AutoRestoreOnMouseUpOnlyMenuItem.Size = new System.Drawing.Size(264, 22);
			this.AutoRestoreOnMouseUpOnlyMenuItem.Text = "Auto-restore on Mouse Up only";
			this.AutoRestoreOnMouseUpOnlyMenuItem.Click += new System.EventHandler(this.AutoRestoreOnMouseUpOnlyMenuItem_Click);
			// 
			// AutoadjustInputMenuItem
			// 
			this.AutoadjustInputMenuItem.CheckOnClick = true;
			this.AutoadjustInputMenuItem.Name = "AutoadjustInputMenuItem";
			this.AutoadjustInputMenuItem.Size = new System.Drawing.Size(264, 22);
			this.AutoadjustInputMenuItem.Text = "Auto-adjust Input according to Lag";
			// 
			// DrawInputByDraggingMenuItem
			// 
			this.DrawInputByDraggingMenuItem.Name = "DrawInputByDraggingMenuItem";
			this.DrawInputByDraggingMenuItem.Size = new System.Drawing.Size(264, 22);
			this.DrawInputByDraggingMenuItem.Text = "Draw Input by dragging";
			this.DrawInputByDraggingMenuItem.Click += new System.EventHandler(this.DrawInputByDraggingMenuItem_Click);
			// 
			// applyPatternToPaintedInputToolStripMenuItem
			// 
			this.applyPatternToPaintedInputToolStripMenuItem.CheckOnClick = true;
			this.applyPatternToPaintedInputToolStripMenuItem.Name = "applyPatternToPaintedInputToolStripMenuItem";
			this.applyPatternToPaintedInputToolStripMenuItem.Size = new System.Drawing.Size(264, 22);
			this.applyPatternToPaintedInputToolStripMenuItem.Text = "Apply Pattern to painted input";
			this.applyPatternToPaintedInputToolStripMenuItem.CheckedChanged += new System.EventHandler(this.ApplyPatternToPaintedInputMenuItem_CheckedChanged);
			// 
			// onlyOnAutoFireColumnsToolStripMenuItem
			// 
			this.onlyOnAutoFireColumnsToolStripMenuItem.Checked = true;
			this.onlyOnAutoFireColumnsToolStripMenuItem.CheckOnClick = true;
			this.onlyOnAutoFireColumnsToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
			this.onlyOnAutoFireColumnsToolStripMenuItem.Enabled = false;
			this.onlyOnAutoFireColumnsToolStripMenuItem.Name = "onlyOnAutoFireColumnsToolStripMenuItem";
			this.onlyOnAutoFireColumnsToolStripMenuItem.Size = new System.Drawing.Size(264, 22);
			this.onlyOnAutoFireColumnsToolStripMenuItem.Text = "Only on Auto-Fire columns";
			// 
			// SingleClickFloatEditMenuItem
			// 
			this.SingleClickFloatEditMenuItem.Enabled = false;
			this.SingleClickFloatEditMenuItem.Name = "SingleClickFloatEditMenuItem";
			this.SingleClickFloatEditMenuItem.Size = new System.Drawing.Size(264, 22);
			this.SingleClickFloatEditMenuItem.Text = "Enter Float Edit mode by single click";
			this.SingleClickFloatEditMenuItem.Visible = false;
			this.SingleClickFloatEditMenuItem.Click += new System.EventHandler(this.SingleClickFloatEditMenuItem_Click);
			// 
			// UseInputKeysItem
			// 
			this.UseInputKeysItem.Enabled = false;
			this.UseInputKeysItem.Name = "UseInputKeysItem";
			this.UseInputKeysItem.Size = new System.Drawing.Size(264, 22);
			this.UseInputKeysItem.Text = "Use Input keys for Column Set";
			this.UseInputKeysItem.Visible = false;
			// 
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size(261, 6);
			// 
			// BindMarkersToInputMenuItem
			// 
			this.BindMarkersToInputMenuItem.Checked = true;
			this.BindMarkersToInputMenuItem.CheckOnClick = true;
			this.BindMarkersToInputMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
			this.BindMarkersToInputMenuItem.Name = "BindMarkersToInputMenuItem";
			this.BindMarkersToInputMenuItem.Size = new System.Drawing.Size(264, 22);
			this.BindMarkersToInputMenuItem.Text = "Bind Markers to Input";
			this.BindMarkersToInputMenuItem.Click += new System.EventHandler(this.BindMarkersToInputMenuItem_Click);
			// 
			// EmptyNewMarkerNotesMenuItem
			// 
			this.EmptyNewMarkerNotesMenuItem.Name = "EmptyNewMarkerNotesMenuItem";
			this.EmptyNewMarkerNotesMenuItem.Size = new System.Drawing.Size(264, 22);
			this.EmptyNewMarkerNotesMenuItem.Text = "Empty new Marker Notes";
			this.EmptyNewMarkerNotesMenuItem.Click += new System.EventHandler(this.EmptyNewMarkerNotesMenuItem_Click);
			// 
			// OldControlSchemeForBranchesMenuItem
			// 
			this.OldControlSchemeForBranchesMenuItem.Name = "OldControlSchemeForBranchesMenuItem";
			this.OldControlSchemeForBranchesMenuItem.Size = new System.Drawing.Size(264, 22);
			this.OldControlSchemeForBranchesMenuItem.Text = "Old control scheme for Branches";
			this.OldControlSchemeForBranchesMenuItem.Click += new System.EventHandler(this.OldControlSchemeForBranchesMenuItem_Click);
			// 
			// LoadBranchOnDoubleclickMenuItem
			// 
			this.LoadBranchOnDoubleclickMenuItem.Name = "LoadBranchOnDoubleclickMenuItem";
			this.LoadBranchOnDoubleclickMenuItem.Size = new System.Drawing.Size(264, 22);
			this.LoadBranchOnDoubleclickMenuItem.Text = "Load Branch on double-click";
			this.LoadBranchOnDoubleclickMenuItem.Click += new System.EventHandler(this.LoadBranchOnDoubleclickMenuItem_Click);
			// 
			// OsdInBranchScreenshotsMenuItem
			// 
			this.OsdInBranchScreenshotsMenuItem.Enabled = false;
			this.OsdInBranchScreenshotsMenuItem.Name = "OsdInBranchScreenshotsMenuItem";
			this.OsdInBranchScreenshotsMenuItem.Size = new System.Drawing.Size(264, 22);
			this.OsdInBranchScreenshotsMenuItem.Text = "OSD in Branch screenshots";
			this.OsdInBranchScreenshotsMenuItem.Visible = false;
			// 
			// toolStripSeparator14
			// 
			this.toolStripSeparator14.Name = "toolStripSeparator14";
			this.toolStripSeparator14.Size = new System.Drawing.Size(261, 6);
			// 
			// AutopauseAtEndOfMovieMenuItem
			// 
			this.AutopauseAtEndOfMovieMenuItem.Name = "AutopauseAtEndOfMovieMenuItem";
			this.AutopauseAtEndOfMovieMenuItem.Size = new System.Drawing.Size(264, 22);
			this.AutopauseAtEndOfMovieMenuItem.Text = "Autopause at end of Movie";
			this.AutopauseAtEndOfMovieMenuItem.Click += new System.EventHandler(this.AutopauseAtEndMenuItem_Click);
			// 
			// sepToolStripMenuItem
			// 
			this.sepToolStripMenuItem.Name = "sepToolStripMenuItem";
			this.sepToolStripMenuItem.Size = new System.Drawing.Size(261, 6);
			// 
			// autoHoldFireToolStripMenuItem
			// 
			this.autoHoldFireToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.keepSetPatternsToolStripMenuItem,
            this.sepToolStripMenuItem1,
            this.autoHoldToolStripMenuItem,
            this.autoFireToolStripMenuItem,
            this.customPatternToolStripMenuItem,
            this.setpToolStripMenuItem,
            this.setCustomsToolStripMenuItem});
			this.autoHoldFireToolStripMenuItem.Name = "autoHoldFireToolStripMenuItem";
			this.autoHoldFireToolStripMenuItem.Size = new System.Drawing.Size(264, 22);
			this.autoHoldFireToolStripMenuItem.Text = "Auto Hold/Fire";
			// 
			// keepSetPatternsToolStripMenuItem
			// 
			this.keepSetPatternsToolStripMenuItem.CheckOnClick = true;
			this.keepSetPatternsToolStripMenuItem.Name = "keepSetPatternsToolStripMenuItem";
			this.keepSetPatternsToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
			this.keepSetPatternsToolStripMenuItem.Text = "Keep set patterns";
			// 
			// sepToolStripMenuItem1
			// 
			this.sepToolStripMenuItem1.Name = "sepToolStripMenuItem1";
			this.sepToolStripMenuItem1.Size = new System.Drawing.Size(161, 6);
			// 
			// autoHoldToolStripMenuItem
			// 
			this.autoHoldToolStripMenuItem.Checked = true;
			this.autoHoldToolStripMenuItem.CheckOnClick = true;
			this.autoHoldToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
			this.autoHoldToolStripMenuItem.Name = "autoHoldToolStripMenuItem";
			this.autoHoldToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
			this.autoHoldToolStripMenuItem.Text = "Auto-Hold";
			this.autoHoldToolStripMenuItem.CheckedChanged += new System.EventHandler(this.AutoHoldMenuItem_CheckedChanged);
			// 
			// autoFireToolStripMenuItem
			// 
			this.autoFireToolStripMenuItem.CheckOnClick = true;
			this.autoFireToolStripMenuItem.Name = "autoFireToolStripMenuItem";
			this.autoFireToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
			this.autoFireToolStripMenuItem.Text = "Auto-Fire";
			this.autoFireToolStripMenuItem.CheckedChanged += new System.EventHandler(this.AutoFireMenuItem_CheckedChanged);
			// 
			// customPatternToolStripMenuItem
			// 
			this.customPatternToolStripMenuItem.CheckOnClick = true;
			this.customPatternToolStripMenuItem.Name = "customPatternToolStripMenuItem";
			this.customPatternToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
			this.customPatternToolStripMenuItem.Text = "Custom Pattern";
			this.customPatternToolStripMenuItem.CheckedChanged += new System.EventHandler(this.CustomPatternMenuItem_CheckedChanged);
			// 
			// setpToolStripMenuItem
			// 
			this.setpToolStripMenuItem.Name = "setpToolStripMenuItem";
			this.setpToolStripMenuItem.Size = new System.Drawing.Size(161, 6);
			// 
			// setCustomsToolStripMenuItem
			// 
			this.setCustomsToolStripMenuItem.Name = "setCustomsToolStripMenuItem";
			this.setCustomsToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
			this.setCustomsToolStripMenuItem.Text = "Set Customs...";
			this.setCustomsToolStripMenuItem.Click += new System.EventHandler(this.SetCustomsMenuItem_Click);
			// 
			// MetaSubMenu
			// 
			this.MetaSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.HeaderMenuItem,
            this.StateHistorySettingsMenuItem,
            this.CommentsMenuItem,
            this.SubtitlesMenuItem,
            this.toolStripSeparator21,
            this.DefaultStateSettingsMenuItem});
			this.MetaSubMenu.Name = "MetaSubMenu";
			this.MetaSubMenu.Size = new System.Drawing.Size(69, 20);
			this.MetaSubMenu.Text = "&Metadata";
			// 
			// HeaderMenuItem
			// 
			this.HeaderMenuItem.Name = "HeaderMenuItem";
			this.HeaderMenuItem.Size = new System.Drawing.Size(236, 22);
			this.HeaderMenuItem.Text = "&Header...";
			this.HeaderMenuItem.Click += new System.EventHandler(this.HeaderMenuItem_Click);
			// 
			// StateHistorySettingsMenuItem
			// 
			this.StateHistorySettingsMenuItem.Name = "StateHistorySettingsMenuItem";
			this.StateHistorySettingsMenuItem.Size = new System.Drawing.Size(236, 22);
			this.StateHistorySettingsMenuItem.Text = "&Savestate History Settings...";
			this.StateHistorySettingsMenuItem.Click += new System.EventHandler(this.StateHistorySettingsMenuItem_Click);
			// 
			// CommentsMenuItem
			// 
			this.CommentsMenuItem.Name = "CommentsMenuItem";
			this.CommentsMenuItem.Size = new System.Drawing.Size(236, 22);
			this.CommentsMenuItem.Text = "&Comments...";
			this.CommentsMenuItem.Click += new System.EventHandler(this.CommentsMenuItem_Click);
			// 
			// SubtitlesMenuItem
			// 
			this.SubtitlesMenuItem.Name = "SubtitlesMenuItem";
			this.SubtitlesMenuItem.Size = new System.Drawing.Size(236, 22);
			this.SubtitlesMenuItem.Text = "&Subtitles...";
			this.SubtitlesMenuItem.Click += new System.EventHandler(this.SubtitlesMenuItem_Click);
			// 
			// toolStripSeparator21
			// 
			this.toolStripSeparator21.Name = "toolStripSeparator21";
			this.toolStripSeparator21.Size = new System.Drawing.Size(233, 6);
			// 
			// DefaultStateSettingsMenuItem
			// 
			this.DefaultStateSettingsMenuItem.Name = "DefaultStateSettingsMenuItem";
			this.DefaultStateSettingsMenuItem.Size = new System.Drawing.Size(236, 22);
			this.DefaultStateSettingsMenuItem.Text = "&Default State History Settings...";
			this.DefaultStateSettingsMenuItem.Click += new System.EventHandler(this.DefaultStateSettingsMenuItem_Click);
			// 
			// SettingsSubMenu
			// 
			this.SettingsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.RotateMenuItem,
            this.RendererOptionsMenuItem,
            this.HideLagFramesSubMenu,
            this.iconsToolStripMenuItem,
            this.toolStripSeparator23,
            this.followCursorToolStripMenuItem,
            this.toolStripSeparator25,
            this.wheelScrollSpeedToolStripMenuItem});
			this.SettingsSubMenu.Name = "SettingsSubMenu";
			this.SettingsSubMenu.Size = new System.Drawing.Size(61, 20);
			this.SettingsSubMenu.Text = "&Settings";
			this.SettingsSubMenu.DropDownOpened += new System.EventHandler(this.SettingsSubMenu_DropDownOpened);
			// 
			// RotateMenuItem
			// 
			this.RotateMenuItem.Name = "RotateMenuItem";
			this.RotateMenuItem.Size = new System.Drawing.Size(183, 22);
			this.RotateMenuItem.Text = "Rotate";
			this.RotateMenuItem.Click += new System.EventHandler(this.RotateMenuItem_Click);
			// 
			// RendererOptionsMenuItem
			// 
			this.RendererOptionsMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SetRenderer0,
            this.SetRenderer1});
			this.RendererOptionsMenuItem.Name = "RendererOptionsMenuItem";
			this.RendererOptionsMenuItem.Size = new System.Drawing.Size(183, 22);
			this.RendererOptionsMenuItem.Text = "Renderer";
			this.RendererOptionsMenuItem.DropDownOpened += new System.EventHandler(this.SelectedRendererSubMenu_DropDownOpened);
			// 
			// SetRenderer0
			// 
			this.SetRenderer0.CheckOnClick = true;
			this.SetRenderer0.Name = "SetRenderer0";
			this.SetRenderer0.Size = new System.Drawing.Size(180, 22);
			this.SetRenderer0.Tag = 0;
			this.SetRenderer0.Text = "GDI";
			this.SetRenderer0.Click += new System.EventHandler(this.SetRenderer_Click);
			// 
			// SetRenderer1
			// 
			this.SetRenderer1.CheckOnClick = true;
			this.SetRenderer1.Name = "SetRenderer1";
			this.SetRenderer1.Size = new System.Drawing.Size(180, 22);
			this.SetRenderer1.Tag = 1;
			this.SetRenderer1.Text = "GDI+ (Experimental)";
			this.SetRenderer1.Click += new System.EventHandler(this.SetRenderer_Click);
			// 
			// HideLagFramesSubMenu
			// 
			this.HideLagFramesSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.HideLagFrames0,
            this.HideLagFrames1,
            this.HideLagFrames2,
            this.HideLagFrames3,
            this.toolStripSeparator12,
            this.hideWasLagFramesToolStripMenuItem});
			this.HideLagFramesSubMenu.Name = "HideLagFramesSubMenu";
			this.HideLagFramesSubMenu.Size = new System.Drawing.Size(183, 22);
			this.HideLagFramesSubMenu.Text = "Hide Lag Frames";
			this.HideLagFramesSubMenu.DropDownOpened += new System.EventHandler(this.HideLagFramesSubMenu_DropDownOpened);
			// 
			// HideLagFrames0
			// 
			this.HideLagFrames0.Checked = true;
			this.HideLagFrames0.CheckOnClick = true;
			this.HideLagFrames0.CheckState = System.Windows.Forms.CheckState.Checked;
			this.HideLagFrames0.Name = "HideLagFrames0";
			this.HideLagFrames0.Size = new System.Drawing.Size(184, 22);
			this.HideLagFrames0.Tag = 0;
			this.HideLagFrames0.Text = "Don\'t Hide";
			this.HideLagFrames0.Click += new System.EventHandler(this.HideLagFramesX_Click);
			// 
			// HideLagFrames1
			// 
			this.HideLagFrames1.CheckOnClick = true;
			this.HideLagFrames1.Name = "HideLagFrames1";
			this.HideLagFrames1.Size = new System.Drawing.Size(184, 22);
			this.HideLagFrames1.Tag = 1;
			this.HideLagFrames1.Text = "1 (30 fps)";
			this.HideLagFrames1.Click += new System.EventHandler(this.HideLagFramesX_Click);
			// 
			// HideLagFrames2
			// 
			this.HideLagFrames2.Name = "HideLagFrames2";
			this.HideLagFrames2.Size = new System.Drawing.Size(184, 22);
			this.HideLagFrames2.Tag = 2;
			this.HideLagFrames2.Text = "2 (20 fps)";
			this.HideLagFrames2.Click += new System.EventHandler(this.HideLagFramesX_Click);
			// 
			// HideLagFrames3
			// 
			this.HideLagFrames3.CheckOnClick = true;
			this.HideLagFrames3.Name = "HideLagFrames3";
			this.HideLagFrames3.Size = new System.Drawing.Size(184, 22);
			this.HideLagFrames3.Tag = 3;
			this.HideLagFrames3.Text = "3 (15fps)";
			this.HideLagFrames3.Click += new System.EventHandler(this.HideLagFramesX_Click);
			// 
			// toolStripSeparator12
			// 
			this.toolStripSeparator12.Name = "toolStripSeparator12";
			this.toolStripSeparator12.Size = new System.Drawing.Size(181, 6);
			// 
			// hideWasLagFramesToolStripMenuItem
			// 
			this.hideWasLagFramesToolStripMenuItem.CheckOnClick = true;
			this.hideWasLagFramesToolStripMenuItem.Name = "hideWasLagFramesToolStripMenuItem";
			this.hideWasLagFramesToolStripMenuItem.Size = new System.Drawing.Size(184, 22);
			this.hideWasLagFramesToolStripMenuItem.Text = "Hide WasLag Frames";
			this.hideWasLagFramesToolStripMenuItem.Click += new System.EventHandler(this.HideWasLagFramesMenuItem_Click);
			// 
			// iconsToolStripMenuItem
			// 
			this.iconsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.DenoteStatesWithIconsToolStripMenuItem,
            this.DenoteStatesWithBGColorToolStripMenuItem,
            this.DenoteMarkersWithIconsToolStripMenuItem,
            this.DenoteMarkersWithBGColorToolStripMenuItem});
			this.iconsToolStripMenuItem.Name = "iconsToolStripMenuItem";
			this.iconsToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
			this.iconsToolStripMenuItem.Text = "Icons";
			this.iconsToolStripMenuItem.DropDownOpened += new System.EventHandler(this.IconsMenuItem_DropDownOpened);
			// 
			// DenoteStatesWithIconsToolStripMenuItem
			// 
			this.DenoteStatesWithIconsToolStripMenuItem.CheckOnClick = true;
			this.DenoteStatesWithIconsToolStripMenuItem.Name = "DenoteStatesWithIconsToolStripMenuItem";
			this.DenoteStatesWithIconsToolStripMenuItem.Size = new System.Drawing.Size(235, 22);
			this.DenoteStatesWithIconsToolStripMenuItem.Text = "Denote States With Icons";
			this.DenoteStatesWithIconsToolStripMenuItem.Click += new System.EventHandler(this.DenoteStatesWithIconsToolStripMenuItem_Click);
			// 
			// DenoteStatesWithBGColorToolStripMenuItem
			// 
			this.DenoteStatesWithBGColorToolStripMenuItem.CheckOnClick = true;
			this.DenoteStatesWithBGColorToolStripMenuItem.Name = "DenoteStatesWithBGColorToolStripMenuItem";
			this.DenoteStatesWithBGColorToolStripMenuItem.Size = new System.Drawing.Size(235, 22);
			this.DenoteStatesWithBGColorToolStripMenuItem.Text = "Denote States With BG Color";
			this.DenoteStatesWithBGColorToolStripMenuItem.Click += new System.EventHandler(this.DenoteStatesWithBGColorToolStripMenuItem_Click);
			// 
			// DenoteMarkersWithIconsToolStripMenuItem
			// 
			this.DenoteMarkersWithIconsToolStripMenuItem.CheckOnClick = true;
			this.DenoteMarkersWithIconsToolStripMenuItem.Name = "DenoteMarkersWithIconsToolStripMenuItem";
			this.DenoteMarkersWithIconsToolStripMenuItem.Size = new System.Drawing.Size(235, 22);
			this.DenoteMarkersWithIconsToolStripMenuItem.Text = "Denote Markers With Icons";
			this.DenoteMarkersWithIconsToolStripMenuItem.Click += new System.EventHandler(this.DenoteMarkersWithIconsToolStripMenuItem_Click);
			// 
			// DenoteMarkersWithBGColorToolStripMenuItem
			// 
			this.DenoteMarkersWithBGColorToolStripMenuItem.CheckOnClick = true;
			this.DenoteMarkersWithBGColorToolStripMenuItem.Name = "DenoteMarkersWithBGColorToolStripMenuItem";
			this.DenoteMarkersWithBGColorToolStripMenuItem.Size = new System.Drawing.Size(235, 22);
			this.DenoteMarkersWithBGColorToolStripMenuItem.Text = "Denote Markers With BG Color";
			this.DenoteMarkersWithBGColorToolStripMenuItem.Click += new System.EventHandler(this.DenoteMarkersWithBGColorToolStripMenuItem_Click);
			// 
			// toolStripSeparator23
			// 
			this.toolStripSeparator23.Name = "toolStripSeparator23";
			this.toolStripSeparator23.Size = new System.Drawing.Size(180, 6);
			// 
			// followCursorToolStripMenuItem
			// 
			this.followCursorToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.alwaysScrollToolStripMenuItem,
            this.toolStripSeparator24,
            this.scrollToViewToolStripMenuItem,
            this.scrollToTopToolStripMenuItem,
            this.scrollToBottomToolStripMenuItem,
            this.scrollToCenterToolStripMenuItem});
			this.followCursorToolStripMenuItem.Name = "followCursorToolStripMenuItem";
			this.followCursorToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
			this.followCursorToolStripMenuItem.Text = "Follow Cursor";
			this.followCursorToolStripMenuItem.DropDownOpened += new System.EventHandler(this.FollowCursorMenuItem_DropDownOpened);
			// 
			// alwaysScrollToolStripMenuItem
			// 
			this.alwaysScrollToolStripMenuItem.CheckOnClick = true;
			this.alwaysScrollToolStripMenuItem.Name = "alwaysScrollToolStripMenuItem";
			this.alwaysScrollToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
			this.alwaysScrollToolStripMenuItem.Text = "Always Scroll";
			this.alwaysScrollToolStripMenuItem.Click += new System.EventHandler(this.AlwaysScrollMenuItem_Click);
			// 
			// toolStripSeparator24
			// 
			this.toolStripSeparator24.Name = "toolStripSeparator24";
			this.toolStripSeparator24.Size = new System.Drawing.Size(157, 6);
			// 
			// scrollToViewToolStripMenuItem
			// 
			this.scrollToViewToolStripMenuItem.Checked = true;
			this.scrollToViewToolStripMenuItem.CheckOnClick = true;
			this.scrollToViewToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
			this.scrollToViewToolStripMenuItem.Name = "scrollToViewToolStripMenuItem";
			this.scrollToViewToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
			this.scrollToViewToolStripMenuItem.Text = "Scroll to View";
			this.scrollToViewToolStripMenuItem.Click += new System.EventHandler(this.ScrollToViewMenuItem_Click);
			// 
			// scrollToTopToolStripMenuItem
			// 
			this.scrollToTopToolStripMenuItem.CheckOnClick = true;
			this.scrollToTopToolStripMenuItem.Name = "scrollToTopToolStripMenuItem";
			this.scrollToTopToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
			this.scrollToTopToolStripMenuItem.Text = "Scroll to Top";
			this.scrollToTopToolStripMenuItem.Click += new System.EventHandler(this.ScrollToTopMenuItem_Click);
			// 
			// scrollToBottomToolStripMenuItem
			// 
			this.scrollToBottomToolStripMenuItem.CheckOnClick = true;
			this.scrollToBottomToolStripMenuItem.Name = "scrollToBottomToolStripMenuItem";
			this.scrollToBottomToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
			this.scrollToBottomToolStripMenuItem.Text = "Scroll to Bottom";
			this.scrollToBottomToolStripMenuItem.Click += new System.EventHandler(this.ScrollToBottomMenuItem_Click);
			// 
			// scrollToCenterToolStripMenuItem
			// 
			this.scrollToCenterToolStripMenuItem.CheckOnClick = true;
			this.scrollToCenterToolStripMenuItem.Name = "scrollToCenterToolStripMenuItem";
			this.scrollToCenterToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
			this.scrollToCenterToolStripMenuItem.Text = "Scroll to Center";
			this.scrollToCenterToolStripMenuItem.Click += new System.EventHandler(this.ScrollToCenterMenuItem_Click);
			// 
			// toolStripSeparator25
			// 
			this.toolStripSeparator25.Name = "toolStripSeparator25";
			this.toolStripSeparator25.Size = new System.Drawing.Size(180, 6);
			// 
			// wheelScrollSpeedToolStripMenuItem
			// 
			this.wheelScrollSpeedToolStripMenuItem.Name = "wheelScrollSpeedToolStripMenuItem";
			this.wheelScrollSpeedToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
			this.wheelScrollSpeedToolStripMenuItem.Text = "Wheel Scroll Speed...";
			this.wheelScrollSpeedToolStripMenuItem.Click += new System.EventHandler(this.WheelScrollSpeedMenuItem_Click);
			// 
			// ColumnsSubMenu
			// 
			this.ColumnsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator19});
			this.ColumnsSubMenu.Name = "ColumnsSubMenu";
			this.ColumnsSubMenu.Size = new System.Drawing.Size(67, 20);
			this.ColumnsSubMenu.Text = "&Columns";
			// 
			// toolStripSeparator19
			// 
			this.toolStripSeparator19.Name = "toolStripSeparator19";
			this.toolStripSeparator19.Size = new System.Drawing.Size(57, 6);
			// 
			// HelpSubMenu
			// 
			this.HelpSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.TASEditorManualOnlineMenuItem,
            this.ForumThreadMenuItem,
            this.aboutToolStripMenuItem,
            this.toolStripSeparator10,
            this.EnableTooltipsMenuItem});
			this.HelpSubMenu.Name = "HelpSubMenu";
			this.HelpSubMenu.Size = new System.Drawing.Size(44, 20);
			this.HelpSubMenu.Text = "&Help";
			// 
			// TASEditorManualOnlineMenuItem
			// 
			this.TASEditorManualOnlineMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Help;
			this.TASEditorManualOnlineMenuItem.Name = "TASEditorManualOnlineMenuItem";
			this.TASEditorManualOnlineMenuItem.Size = new System.Drawing.Size(218, 22);
			this.TASEditorManualOnlineMenuItem.Text = "TAS Editor Manual Online...";
			this.TASEditorManualOnlineMenuItem.Click += new System.EventHandler(this.TASEditorManualOnlineMenuItem_Click);
			// 
			// ForumThreadMenuItem
			// 
			this.ForumThreadMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.TAStudio;
			this.ForumThreadMenuItem.Name = "ForumThreadMenuItem";
			this.ForumThreadMenuItem.Size = new System.Drawing.Size(218, 22);
			this.ForumThreadMenuItem.Text = "Forum Thread...";
			this.ForumThreadMenuItem.Click += new System.EventHandler(this.ForumThreadMenuItem_Click);
			// 
			// aboutToolStripMenuItem
			// 
			this.aboutToolStripMenuItem.Enabled = false;
			this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
			this.aboutToolStripMenuItem.Size = new System.Drawing.Size(218, 22);
			this.aboutToolStripMenuItem.Text = "&About";
			// 
			// toolStripSeparator10
			// 
			this.toolStripSeparator10.Name = "toolStripSeparator10";
			this.toolStripSeparator10.Size = new System.Drawing.Size(215, 6);
			// 
			// EnableTooltipsMenuItem
			// 
			this.EnableTooltipsMenuItem.Enabled = false;
			this.EnableTooltipsMenuItem.Name = "EnableTooltipsMenuItem";
			this.EnableTooltipsMenuItem.Size = new System.Drawing.Size(218, 22);
			this.EnableTooltipsMenuItem.Text = "&Enable Tooltips";
			// 
			// TasView
			// 
			this.TasView.AllowColumnReorder = false;
			this.TasView.AllowColumnResize = false;
			this.TasView.AllowRightClickSelecton = false;
			this.TasView.AlwaysScroll = false;
			this.TasView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.TasView.CellHeightPadding = 0;
			this.TasView.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.TasView.FullRowSelect = true;
			this.TasView.HideWasLagFrames = false;
			this.TasView.HorizontalOrientation = false;
			this.TasView.LagFramesToHide = 0;
			this.TasView.LetKeysModifySelection = true;
			this.TasView.Location = new System.Drawing.Point(3, 0);
			this.TasView.MaxCharactersInHorizontal = 1;
			this.TasView.MultiSelect = false;
			this.TasView.Name = "TasView";
			this.TasView.RowCount = 0;
			this.TasView.ScrollSpeed = 1;
			this.TasView.SeekingCutoffInterval = 0;
			this.TasView.Size = new System.Drawing.Size(289, 528);
			this.TasView.SuspendHotkeys = false;
			this.TasView.TabIndex = 1;
			this.TasView.ColumnClick += new BizHawk.Client.EmuHawk.InputRoll.ColumnClickEventHandler(this.TasView_ColumnClick);
			this.TasView.ColumnRightClick += new BizHawk.Client.EmuHawk.InputRoll.ColumnClickEventHandler(this.TasView_ColumnRightClick);
			this.TasView.SelectedIndexChanged += new System.EventHandler(this.TasView_SelectedIndexChanged);
			this.TasView.RightMouseScrolled += new BizHawk.Client.EmuHawk.InputRoll.RightMouseScrollEventHandler(this.TasView_MouseWheel);
			this.TasView.ColumnReordered += new BizHawk.Client.EmuHawk.InputRoll.ColumnReorderedEventHandler(this.TasView_ColumnReordered);
			this.TasView.CellDropped += new BizHawk.Client.EmuHawk.InputRoll.CellDroppedEvent(this.TasView_CellDropped);
			this.TasView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TasView_KeyDown);
			this.TasView.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.TasView_MouseDoubleClick);
			this.TasView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.TasView_MouseDown);
			this.TasView.MouseEnter += new System.EventHandler(this.TasView_MouseEnter);
			this.TasView.MouseMove += new System.Windows.Forms.MouseEventHandler(this.TasView_MouseMove);
			this.TasView.MouseUp += new System.Windows.Forms.MouseEventHandler(this.TasView_MouseUp);
			this.TasView.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.TasView_PreviewKeyDown);
			// 
			// TasStatusStrip
			// 
			this.TasStatusStrip.ClickThrough = true;
			this.TasStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MessageStatusLabel,
            this.SavingProgressBar,
            this.toolStripStatusLabel2,
            this.SplicerStatusLabel});
			this.TasStatusStrip.Location = new System.Drawing.Point(0, 554);
			this.TasStatusStrip.Name = "TasStatusStrip";
			this.TasStatusStrip.Size = new System.Drawing.Size(509, 22);
			this.TasStatusStrip.TabIndex = 4;
			this.TasStatusStrip.Text = "statusStrip1";
			// 
			// MessageStatusLabel
			// 
			this.MessageStatusLabel.Name = "MessageStatusLabel";
			this.MessageStatusLabel.Size = new System.Drawing.Size(104, 17);
			this.MessageStatusLabel.Text = "TAStudio engaged";
			// 
			// SavingProgressBar
			// 
			this.SavingProgressBar.Name = "SavingProgressBar";
			this.SavingProgressBar.Size = new System.Drawing.Size(100, 16);
			// 
			// toolStripStatusLabel2
			// 
			this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
			this.toolStripStatusLabel2.Size = new System.Drawing.Size(268, 17);
			this.toolStripStatusLabel2.Spring = true;
			// 
			// SplicerStatusLabel
			// 
			this.SplicerStatusLabel.Name = "SplicerStatusLabel";
			this.SplicerStatusLabel.Padding = new System.Windows.Forms.Padding(20, 0, 0, 0);
			this.SplicerStatusLabel.Size = new System.Drawing.Size(20, 17);
			// 
			// TasPlaybackBox
			// 
			this.TasPlaybackBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.TasPlaybackBox.Location = new System.Drawing.Point(3, 4);
			this.TasPlaybackBox.Name = "TasPlaybackBox";
			this.TasPlaybackBox.Size = new System.Drawing.Size(204, 111);
			this.TasPlaybackBox.TabIndex = 5;
			this.TasPlaybackBox.Tastudio = null;
			// 
			// MarkerControl
			// 
			this.MarkerControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.MarkerControl.Location = new System.Drawing.Point(2, 16);
			this.MarkerControl.Name = "MarkerControl";
			this.MarkerControl.Size = new System.Drawing.Size(194, 193);
			this.MarkerControl.TabIndex = 6;
			this.MarkerControl.Tastudio = null;
			// 
			// RightClickMenu
			// 
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
            this.InsertFrameContextMenuItem,
            this.DeleteFramesContextMenuItem,
            this.CloneContextMenuItem,
            this.InsertNumFramesContextMenuItem,
            this.toolStripSeparator18,
            this.TruncateContextMenuItem,
            this.BranchContextMenuItem,
            this.StartFromNowSeparator,
            this.StartNewProjectFromNowMenuItem,
            this.StartANewProjectFromSaveRamMenuItem});
			this.RightClickMenu.Name = "RightClickMenu";
			this.RightClickMenu.Size = new System.Drawing.Size(253, 480);
			this.RightClickMenu.Opened += new System.EventHandler(this.RightClickMenu_Opened);
			// 
			// SetMarkersContextMenuItem
			// 
			this.SetMarkersContextMenuItem.Name = "SetMarkersContextMenuItem";
			this.SetMarkersContextMenuItem.Size = new System.Drawing.Size(252, 22);
			this.SetMarkersContextMenuItem.Text = "Set Markers";
			this.SetMarkersContextMenuItem.Click += new System.EventHandler(this.SetMarkersMenuItem_Click);
			// 
			// SetMarkerWithTextContextMenuItem
			// 
			this.SetMarkerWithTextContextMenuItem.Name = "SetMarkerWithTextContextMenuItem";
			this.SetMarkerWithTextContextMenuItem.Size = new System.Drawing.Size(252, 22);
			this.SetMarkerWithTextContextMenuItem.Text = "Set Marker with Text";
			this.SetMarkerWithTextContextMenuItem.Click += new System.EventHandler(this.SetMarkerWithTextMenuItem_Click);
			// 
			// RemoveMarkersContextMenuItem
			// 
			this.RemoveMarkersContextMenuItem.Name = "RemoveMarkersContextMenuItem";
			this.RemoveMarkersContextMenuItem.Size = new System.Drawing.Size(252, 22);
			this.RemoveMarkersContextMenuItem.Text = "Remove Markers";
			this.RemoveMarkersContextMenuItem.Click += new System.EventHandler(this.RemoveMarkersMenuItem_Click);
			// 
			// toolStripSeparator15
			// 
			this.toolStripSeparator15.Name = "toolStripSeparator15";
			this.toolStripSeparator15.Size = new System.Drawing.Size(249, 6);
			// 
			// DeselectContextMenuItem
			// 
			this.DeselectContextMenuItem.Name = "DeselectContextMenuItem";
			this.DeselectContextMenuItem.Size = new System.Drawing.Size(252, 22);
			this.DeselectContextMenuItem.Text = "Deselect";
			this.DeselectContextMenuItem.Click += new System.EventHandler(this.DeselectMenuItem_Click);
			// 
			// SelectBetweenMarkersContextMenuItem
			// 
			this.SelectBetweenMarkersContextMenuItem.Name = "SelectBetweenMarkersContextMenuItem";
			this.SelectBetweenMarkersContextMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
			this.SelectBetweenMarkersContextMenuItem.Size = new System.Drawing.Size(252, 22);
			this.SelectBetweenMarkersContextMenuItem.Text = "Select between Markers";
			this.SelectBetweenMarkersContextMenuItem.Click += new System.EventHandler(this.SelectBetweenMarkersMenuItem_Click);
			// 
			// toolStripSeparator16
			// 
			this.toolStripSeparator16.Name = "toolStripSeparator16";
			this.toolStripSeparator16.Size = new System.Drawing.Size(249, 6);
			// 
			// UngreenzoneContextMenuItem
			// 
			this.UngreenzoneContextMenuItem.Name = "UngreenzoneContextMenuItem";
			this.UngreenzoneContextMenuItem.Size = new System.Drawing.Size(252, 22);
			this.UngreenzoneContextMenuItem.Text = "Clear Greenzone";
			this.UngreenzoneContextMenuItem.Click += new System.EventHandler(this.ClearGreenzoneMenuItem_Click);
			// 
			// CancelSeekContextMenuItem
			// 
			this.CancelSeekContextMenuItem.Name = "CancelSeekContextMenuItem";
			this.CancelSeekContextMenuItem.Size = new System.Drawing.Size(252, 22);
			this.CancelSeekContextMenuItem.Text = "Cancel Seek";
			this.CancelSeekContextMenuItem.Click += new System.EventHandler(this.CancelSeekContextMenuItem_Click);
			// 
			// toolStripSeparator17
			// 
			this.toolStripSeparator17.Name = "toolStripSeparator17";
			this.toolStripSeparator17.Size = new System.Drawing.Size(249, 6);
			// 
			// copyToolStripMenuItem
			// 
			this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
			this.copyToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+C";
			this.copyToolStripMenuItem.Size = new System.Drawing.Size(252, 22);
			this.copyToolStripMenuItem.Text = "Copy";
			this.copyToolStripMenuItem.Click += new System.EventHandler(this.CopyMenuItem_Click);
			// 
			// pasteToolStripMenuItem
			// 
			this.pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
			this.pasteToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+V";
			this.pasteToolStripMenuItem.Size = new System.Drawing.Size(252, 22);
			this.pasteToolStripMenuItem.Text = "Paste";
			this.pasteToolStripMenuItem.Click += new System.EventHandler(this.PasteMenuItem_Click);
			// 
			// pasteInsertToolStripMenuItem
			// 
			this.pasteInsertToolStripMenuItem.Name = "pasteInsertToolStripMenuItem";
			this.pasteInsertToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+Shift+V";
			this.pasteInsertToolStripMenuItem.Size = new System.Drawing.Size(252, 22);
			this.pasteInsertToolStripMenuItem.Text = "Paste Insert";
			this.pasteInsertToolStripMenuItem.Click += new System.EventHandler(this.PasteInsertMenuItem_Click);
			// 
			// cutToolStripMenuItem
			// 
			this.cutToolStripMenuItem.Name = "cutToolStripMenuItem";
			this.cutToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+X";
			this.cutToolStripMenuItem.Size = new System.Drawing.Size(252, 22);
			this.cutToolStripMenuItem.Text = "Cut";
			this.cutToolStripMenuItem.Click += new System.EventHandler(this.CutMenuItem_Click);
			// 
			// separateToolStripMenuItem
			// 
			this.separateToolStripMenuItem.Name = "separateToolStripMenuItem";
			this.separateToolStripMenuItem.Size = new System.Drawing.Size(249, 6);
			// 
			// ClearContextMenuItem
			// 
			this.ClearContextMenuItem.Name = "ClearContextMenuItem";
			this.ClearContextMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Delete;
			this.ClearContextMenuItem.Size = new System.Drawing.Size(252, 22);
			this.ClearContextMenuItem.Text = "Clear";
			this.ClearContextMenuItem.Click += new System.EventHandler(this.ClearFramesMenuItem_Click);
			// 
			// InsertFrameContextMenuItem
			// 
			this.InsertFrameContextMenuItem.Name = "InsertFrameContextMenuItem";
			this.InsertFrameContextMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Insert;
			this.InsertFrameContextMenuItem.Size = new System.Drawing.Size(252, 22);
			this.InsertFrameContextMenuItem.Text = "Insert";
			this.InsertFrameContextMenuItem.Click += new System.EventHandler(this.InsertFrameMenuItem_Click);
			// 
			// DeleteFramesContextMenuItem
			// 
			this.DeleteFramesContextMenuItem.Name = "DeleteFramesContextMenuItem";
			this.DeleteFramesContextMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Delete)));
			this.DeleteFramesContextMenuItem.Size = new System.Drawing.Size(252, 22);
			this.DeleteFramesContextMenuItem.Text = "Delete";
			this.DeleteFramesContextMenuItem.Click += new System.EventHandler(this.DeleteFramesMenuItem_Click);
			// 
			// CloneContextMenuItem
			// 
			this.CloneContextMenuItem.Name = "CloneContextMenuItem";
			this.CloneContextMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Insert)));
			this.CloneContextMenuItem.Size = new System.Drawing.Size(252, 22);
			this.CloneContextMenuItem.Text = "Clone";
			this.CloneContextMenuItem.Click += new System.EventHandler(this.CloneFramesMenuItem_Click);
			// 
			// InsertNumFramesContextMenuItem
			// 
			this.InsertNumFramesContextMenuItem.Name = "InsertNumFramesContextMenuItem";
			this.InsertNumFramesContextMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.Insert)));
			this.InsertNumFramesContextMenuItem.Size = new System.Drawing.Size(252, 22);
			this.InsertNumFramesContextMenuItem.Text = "Insert # of Frames";
			this.InsertNumFramesContextMenuItem.Click += new System.EventHandler(this.InsertNumFramesMenuItem_Click);
			// 
			// toolStripSeparator18
			// 
			this.toolStripSeparator18.Name = "toolStripSeparator18";
			this.toolStripSeparator18.Size = new System.Drawing.Size(249, 6);
			// 
			// TruncateContextMenuItem
			// 
			this.TruncateContextMenuItem.Name = "TruncateContextMenuItem";
			this.TruncateContextMenuItem.Size = new System.Drawing.Size(252, 22);
			this.TruncateContextMenuItem.Text = "Truncate Movie";
			this.TruncateContextMenuItem.Click += new System.EventHandler(this.TruncateMenuItem_Click);
			// 
			// BranchContextMenuItem
			// 
			this.BranchContextMenuItem.Name = "BranchContextMenuItem";
			this.BranchContextMenuItem.Size = new System.Drawing.Size(252, 22);
			this.BranchContextMenuItem.Text = "&Branch";
			this.BranchContextMenuItem.Click += new System.EventHandler(this.BranchContextMenuItem_Click);
			// 
			// StartFromNowSeparator
			// 
			this.StartFromNowSeparator.Name = "StartFromNowSeparator";
			this.StartFromNowSeparator.Size = new System.Drawing.Size(249, 6);
			// 
			// StartNewProjectFromNowMenuItem
			// 
			this.StartNewProjectFromNowMenuItem.Name = "StartNewProjectFromNowMenuItem";
			this.StartNewProjectFromNowMenuItem.Size = new System.Drawing.Size(252, 22);
			this.StartNewProjectFromNowMenuItem.Text = "Start a new project from Now";
			this.StartNewProjectFromNowMenuItem.Click += new System.EventHandler(this.StartNewProjectFromNowMenuItem_Click);
			// 
			// StartANewProjectFromSaveRamMenuItem
			// 
			this.StartANewProjectFromSaveRamMenuItem.Name = "StartANewProjectFromSaveRamMenuItem";
			this.StartANewProjectFromSaveRamMenuItem.Size = new System.Drawing.Size(252, 22);
			this.StartANewProjectFromSaveRamMenuItem.Text = "Start a new project from SaveRam";
			this.StartANewProjectFromSaveRamMenuItem.Click += new System.EventHandler(this.StartANewProjectFromSaveRamMenuItem_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add(this.MarkerControl);
			this.groupBox1.Location = new System.Drawing.Point(-2, 3);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(204, 215);
			this.groupBox1.TabIndex = 7;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Markers";
			// 
			// BookMarkControl
			// 
			this.BookMarkControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.BookMarkControl.HoverInterval = 1;
			this.BookMarkControl.LoadedCallback = null;
			this.BookMarkControl.Location = new System.Drawing.Point(-2, 5);
			this.BookMarkControl.Name = "BookMarkControl";
			this.BookMarkControl.RemovedCallback = null;
			this.BookMarkControl.SavedCallback = null;
			this.BookMarkControl.Size = new System.Drawing.Size(204, 173);
			this.BookMarkControl.TabIndex = 8;
			this.BookMarkControl.Tastudio = null;
			// 
			// BranchesMarkersSplit
			// 
			this.BranchesMarkersSplit.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.BranchesMarkersSplit.Location = new System.Drawing.Point(3, 121);
			this.BranchesMarkersSplit.Name = "BranchesMarkersSplit";
			this.BranchesMarkersSplit.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// BranchesMarkersSplit.Panel1
			// 
			this.BranchesMarkersSplit.Panel1.Controls.Add(this.BookMarkControl);
			// 
			// BranchesMarkersSplit.Panel2
			// 
			this.BranchesMarkersSplit.Panel2.Controls.Add(this.groupBox1);
			this.BranchesMarkersSplit.Size = new System.Drawing.Size(204, 404);
			this.BranchesMarkersSplit.SplitterDistance = 179;
			this.BranchesMarkersSplit.TabIndex = 9;
			this.BranchesMarkersSplit.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.BranchesMarkersSplit_SplitterMoved);
			// 
			// MainVertialSplit
			// 
			this.MainVertialSplit.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.MainVertialSplit.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
			this.MainVertialSplit.Location = new System.Drawing.Point(2, 23);
			this.MainVertialSplit.Name = "MainVertialSplit";
			// 
			// MainVertialSplit.Panel1
			// 
			this.MainVertialSplit.Panel1.Controls.Add(this.TasView);
			// 
			// MainVertialSplit.Panel2
			// 
			this.MainVertialSplit.Panel2.Controls.Add(this.TasPlaybackBox);
			this.MainVertialSplit.Panel2.Controls.Add(this.BranchesMarkersSplit);
			this.MainVertialSplit.Size = new System.Drawing.Size(507, 528);
			this.MainVertialSplit.SplitterDistance = 295;
			this.MainVertialSplit.TabIndex = 10;
			this.MainVertialSplit.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.MainVertialSplit_SplitterMoved);
			// 
			// TAStudio
			// 
			this.AllowDrop = true;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(509, 576);
			this.Controls.Add(this.MainVertialSplit);
			this.Controls.Add(this.TasStatusStrip);
			this.Controls.Add(this.TASMenu);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.KeyPreview = true;
			this.MainMenuStrip = this.TASMenu;
			this.MinimumSize = new System.Drawing.Size(200, 148);
			this.Name = "TAStudio";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "TAStudio";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Tastudio_Closing);
			this.Load += new System.EventHandler(this.Tastudio_Load);
			this.DragDrop += new System.Windows.Forms.DragEventHandler(this.TAStudio_DragDrop);
			this.DragEnter += new System.Windows.Forms.DragEventHandler(this.DragEnterWrapper);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TAStudio_KeyDown);
			this.MouseLeave += new System.EventHandler(this.TAStudio_MouseLeave);
			this.TASMenu.ResumeLayout(false);
			this.TASMenu.PerformLayout();
			this.TasStatusStrip.ResumeLayout(false);
			this.TasStatusStrip.PerformLayout();
			this.RightClickMenu.ResumeLayout(false);
			this.groupBox1.ResumeLayout(false);
			this.BranchesMarkersSplit.Panel1.ResumeLayout(false);
			this.BranchesMarkersSplit.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.BranchesMarkersSplit)).EndInit();
			this.BranchesMarkersSplit.ResumeLayout(false);
			this.MainVertialSplit.Panel1.ResumeLayout(false);
			this.MainVertialSplit.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.MainVertialSplit)).EndInit();
			this.MainVertialSplit.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private MenuStripEx TASMenu;
		private System.Windows.Forms.ToolStripMenuItem FileSubMenu;
		private System.Windows.Forms.ToolStripMenuItem NewTASMenuItem;
		private System.Windows.Forms.ToolStripMenuItem OpenTASMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SaveTASMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SaveAsTASMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem ExitMenuItem;
		private System.Windows.Forms.ToolStripMenuItem EditSubMenu;
		private System.Windows.Forms.ToolStripMenuItem ConfigSubMenu;
		private InputRoll TasView;
		private System.Windows.Forms.ToolStripMenuItem RecentSubMenu;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripMenuItem InsertFrameMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
		private System.Windows.Forms.ToolStripMenuItem CloneFramesMenuItem;
		private System.Windows.Forms.ToolStripMenuItem DeleteFramesMenuItem;
		private System.Windows.Forms.ToolStripMenuItem ClearFramesMenuItem;
		private System.Windows.Forms.ToolStripMenuItem InsertNumFramesMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SelectAllMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
		private System.Windows.Forms.ToolStripMenuItem TruncateMenuItem;
		private System.Windows.Forms.ToolStripMenuItem CopyMenuItem;
		private System.Windows.Forms.ToolStripMenuItem PasteMenuItem;
		private System.Windows.Forms.ToolStripMenuItem PasteInsertMenuItem;
		private System.Windows.Forms.ToolStripMenuItem CutMenuItem;
		private System.Windows.Forms.ToolStripMenuItem UndoMenuItem;
		private System.Windows.Forms.ToolStripMenuItem RedoMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SelectionUndoMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SelectionRedoMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
		private System.Windows.Forms.ToolStripMenuItem DeselectMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SelectBetweenMarkersMenuItem;
		private System.Windows.Forms.ToolStripMenuItem ReselectClipboardMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator9;
		private System.Windows.Forms.ToolStripMenuItem HelpSubMenu;
		private System.Windows.Forms.ToolStripMenuItem EnableTooltipsMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator10;
		private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SetMaxUndoLevelsMenuItem;
		private System.Windows.Forms.ToolStripMenuItem AutoadjustInputMenuItem;
		private System.Windows.Forms.ToolStripMenuItem DrawInputByDraggingMenuItem;
		private System.Windows.Forms.ToolStripMenuItem UseInputKeysItem;
		private System.Windows.Forms.ToolStripMenuItem BindMarkersToInputMenuItem;
		private System.Windows.Forms.ToolStripMenuItem EmptyNewMarkerNotesMenuItem;
		private System.Windows.Forms.ToolStripMenuItem OldControlSchemeForBranchesMenuItem;
		private System.Windows.Forms.ToolStripMenuItem OsdInBranchScreenshotsMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator14;
		private System.Windows.Forms.ToolStripMenuItem AutopauseAtEndOfMovieMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SettingsSubMenu;
		private StatusStripEx TasStatusStrip;
		private System.Windows.Forms.ToolStripStatusLabel MessageStatusLabel;
		public PlaybackBox TasPlaybackBox;
		private System.Windows.Forms.ToolStripStatusLabel SplicerStatusLabel;
		private System.Windows.Forms.ToolStripMenuItem MetaSubMenu;
		private System.Windows.Forms.ToolStripMenuItem HeaderMenuItem;
		private System.Windows.Forms.ToolStripMenuItem CommentsMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SubtitlesMenuItem;
		private System.Windows.Forms.ToolStripMenuItem StateHistorySettingsMenuItem;
		private MarkerControl MarkerControl;
		private System.Windows.Forms.ContextMenuStrip RightClickMenu;
		private System.Windows.Forms.ToolStripMenuItem SetMarkersContextMenuItem;
		private System.Windows.Forms.ToolStripMenuItem RemoveMarkersContextMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator15;
		private System.Windows.Forms.ToolStripMenuItem DeselectContextMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SelectBetweenMarkersContextMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator16;
		private System.Windows.Forms.ToolStripMenuItem UngreenzoneContextMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator17;
		private System.Windows.Forms.ToolStripMenuItem ClearContextMenuItem;
		private System.Windows.Forms.ToolStripMenuItem DeleteFramesContextMenuItem;
		private System.Windows.Forms.ToolStripMenuItem InsertFrameContextMenuItem;
		private System.Windows.Forms.ToolStripMenuItem InsertNumFramesContextMenuItem;
		private System.Windows.Forms.ToolStripMenuItem CloneContextMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator18;
		private System.Windows.Forms.ToolStripMenuItem TruncateContextMenuItem;
		private System.Windows.Forms.ToolStripMenuItem ClearGreenzoneMenuItem;
		private System.Windows.Forms.ToolStripSeparator GreenzoneICheckSeparator;
		private System.Windows.Forms.ToolStripMenuItem StateHistoryIntegrityCheckMenuItem;
		private System.Windows.Forms.ToolStripMenuItem ColumnsSubMenu;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator19;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator21;
		private System.Windows.Forms.ToolStripMenuItem DefaultStateSettingsMenuItem;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.ToolStripMenuItem CancelSeekContextMenuItem;
		private System.Windows.Forms.ToolStripSeparator StartFromNowSeparator;
		private System.Windows.Forms.ToolStripMenuItem StartNewProjectFromNowMenuItem;
		private System.Windows.Forms.ToolStripMenuItem RotateMenuItem;
		private System.Windows.Forms.ToolStripProgressBar SavingProgressBar;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel2;
		private System.Windows.Forms.ToolStripMenuItem HideLagFramesSubMenu;
		private System.Windows.Forms.ToolStripMenuItem HideLagFrames3;
		private System.Windows.Forms.ToolStripMenuItem HideLagFrames0;
		private System.Windows.Forms.ToolStripMenuItem HideLagFrames1;
		private System.Windows.Forms.ToolStripMenuItem HideLagFrames2;
		private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem pasteToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator separateToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem pasteInsertToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem cutToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem showUndoHistoryToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator sepToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem autoHoldFireToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem keepSetPatternsToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator sepToolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem autoHoldToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem autoFireToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem customPatternToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator setpToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem setCustomsToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator12;
		private System.Windows.Forms.ToolStripMenuItem hideWasLagFramesToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveSelectionToMacroToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem placeMacroAtSelectionToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator20;
		private System.Windows.Forms.ToolStripMenuItem ToBk2MenuItem;
		private System.Windows.Forms.ToolStripMenuItem recentMacrosToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator22;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator23;
		private System.Windows.Forms.ToolStripMenuItem followCursorToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem alwaysScrollToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator24;
		private System.Windows.Forms.ToolStripMenuItem scrollToViewToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem scrollToTopToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem scrollToBottomToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem scrollToCenterToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem applyPatternToPaintedInputToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem onlyOnAutoFireColumnsToolStripMenuItem;
		private BookmarksBranchesBox BookMarkControl;
		private System.Windows.Forms.ToolStripMenuItem BranchContextMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator25;
		private System.Windows.Forms.ToolStripMenuItem wheelScrollSpeedToolStripMenuItem;
		private System.Windows.Forms.SplitContainer BranchesMarkersSplit;
		private System.Windows.Forms.SplitContainer MainVertialSplit;
		private System.Windows.Forms.ToolStripMenuItem StartANewProjectFromSaveRamMenuItem;
		private System.Windows.Forms.ToolStripMenuItem iconsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem DenoteStatesWithIconsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem DenoteStatesWithBGColorToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem DenoteMarkersWithIconsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem DenoteMarkersWithBGColorToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem NewFromSubMenu;
		private System.Windows.Forms.ToolStripMenuItem NewFromNowMenuItem;
		private System.Windows.Forms.ToolStripMenuItem NewFromCurrentSaveRamMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SetBranchCellHoverIntervalMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SetMarkerWithTextContextMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SetSeekingCutoffIntervalMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator26;
		private System.Windows.Forms.ToolStripMenuItem TASEditorManualOnlineMenuItem;
		private System.Windows.Forms.ToolStripMenuItem ForumThreadMenuItem;
		private System.Windows.Forms.ToolStripMenuItem autosaveToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SetAutosaveIntervalMenuItem;
		private System.Windows.Forms.ToolStripMenuItem AutosaveAsBk2MenuItem;
		private System.Windows.Forms.ToolStripMenuItem AutosaveAsBackupFileMenuItem;
		private System.Windows.Forms.ToolStripMenuItem BackupPerFileSaveMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SaveBackupMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SaveBk2BackupMenuItem;
		private System.Windows.Forms.ToolStripMenuItem AutoRestoreOnMouseUpOnlyMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SingleClickFloatEditMenuItem;
		private System.Windows.Forms.ToolStripMenuItem LoadBranchOnDoubleclickMenuItem;
		private System.Windows.Forms.ToolStripMenuItem RendererOptionsMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SetRenderer0;
		private System.Windows.Forms.ToolStripMenuItem SetRenderer1;
	}
}
