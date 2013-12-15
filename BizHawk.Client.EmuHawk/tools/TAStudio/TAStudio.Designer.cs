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

			//Global.MovieSession.Movie.StateCapturing = false; //TODO: This doesn't go here, extend this method in the .cs file

			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TAStudio));
			this.TASMenu = new MenuStripEx();
			this.FileSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.NewTASMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.OpenTASMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SaveTASMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SaveAsTASMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.RecentSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.ImportMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ExportMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.ExitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.UndoMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.RedoMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SelectionUndoMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SelectionRedoMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.DeselectMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.selectAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SelectBetweenMarkersMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.reselectClipboardToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
			this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.pasteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.pasteInsertToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.cutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
			this.clearToolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
			this.deleteFramesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.cloneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.insertFrameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.insertNumFramesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
			this.TruncateMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ConfigSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.ProjectOptionsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SetMaxUndoLevelsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SetGreenzoneCapacityMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
			this.EnableGreenzoningMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.AutofirePatternSkipsLagMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.AutoadjustInputMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator11 = new System.Windows.Forms.ToolStripSeparator();
			this.DrawInputByDraggingMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.CombineConsecutiveRecordingsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.Use1PKeysMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.UseInputKeysItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.BindMarkersToInputMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.EmptyNewMarkerNotesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator13 = new System.Windows.Forms.ToolStripSeparator();
			this.BranchesRestoreEntireMovieMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.OsdInBranchScreenshotsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator14 = new System.Windows.Forms.ToolStripSeparator();
			this.AutopauseAtEndOfMovieMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SettingsSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.AutoloadMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.AutoloadProjectMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SaveWindowPositionMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.AlwaysOnTopMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator12 = new System.Windows.Forms.ToolStripSeparator();
			this.RestoreDefaultSettingsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.HelpSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.EnableTooltipsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator10 = new System.Windows.Forms.ToolStripSeparator();
			this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.TASView = new BizHawk.Client.EmuHawk.TasListView();
			this.Frame = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Log = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.MarkerLabel = new System.Windows.Forms.Label();
			this.MarkerDescriptionBox = new System.Windows.Forms.TextBox();
			this.TopMarkerBox = new System.Windows.Forms.TextBox();
			this.TopMarkerLabel = new System.Windows.Forms.Label();
			this.TASMenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// TASMenu
			// 
			this.TASMenu.ClickThrough = true;
			this.TASMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu,
            this.editToolStripMenuItem,
            this.ConfigSubMenu,
            this.SettingsSubMenu,
            this.HelpSubMenu});
			this.TASMenu.Location = new System.Drawing.Point(0, 0);
			this.TASMenu.Name = "TASMenu";
			this.TASMenu.Size = new System.Drawing.Size(530, 24);
			this.TASMenu.TabIndex = 0;
			this.TASMenu.Text = "menuStrip1";
			// 
			// FileSubMenu
			// 
			this.FileSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.NewTASMenuItem,
            this.OpenTASMenuItem,
            this.SaveTASMenuItem,
            this.SaveAsTASMenuItem,
            this.RecentSubMenu,
            this.toolStripSeparator1,
            this.ImportMenuItem,
            this.ExportMenuItem,
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
			this.NewTASMenuItem.Size = new System.Drawing.Size(186, 22);
			this.NewTASMenuItem.Text = "&New";
			this.NewTASMenuItem.Click += new System.EventHandler(this.NewTASMenuItem_Click);
			// 
			// OpenTASMenuItem
			// 
			this.OpenTASMenuItem.Name = "OpenTASMenuItem";
			this.OpenTASMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.OpenTASMenuItem.Size = new System.Drawing.Size(186, 22);
			this.OpenTASMenuItem.Text = "&Open";
			this.OpenTASMenuItem.Click += new System.EventHandler(this.OpenTASMenuItem_Click);
			// 
			// SaveTASMenuItem
			// 
			this.SaveTASMenuItem.Name = "SaveTASMenuItem";
			this.SaveTASMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			this.SaveTASMenuItem.Size = new System.Drawing.Size(186, 22);
			this.SaveTASMenuItem.Text = "&Save";
			this.SaveTASMenuItem.Click += new System.EventHandler(this.SaveTASMenuItem_Click);
			// 
			// SaveAsTASMenuItem
			// 
			this.SaveAsTASMenuItem.Name = "SaveAsTASMenuItem";
			this.SaveAsTASMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.S)));
			this.SaveAsTASMenuItem.Size = new System.Drawing.Size(186, 22);
			this.SaveAsTASMenuItem.Text = "Save As";
			this.SaveAsTASMenuItem.Click += new System.EventHandler(this.SaveAsTASMenuItem_Click);
			// 
			// RecentSubMenu
			// 
			this.RecentSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator3});
			this.RecentSubMenu.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Recent;
			this.RecentSubMenu.Name = "RecentSubMenu";
			this.RecentSubMenu.Size = new System.Drawing.Size(186, 22);
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
			this.toolStripSeparator1.Size = new System.Drawing.Size(183, 6);
			// 
			// ImportMenuItem
			// 
			this.ImportMenuItem.Enabled = false;
			this.ImportMenuItem.Name = "ImportMenuItem";
			this.ImportMenuItem.Size = new System.Drawing.Size(186, 22);
			this.ImportMenuItem.Text = "Import";
			// 
			// ExportMenuItem
			// 
			this.ExportMenuItem.Enabled = false;
			this.ExportMenuItem.Name = "ExportMenuItem";
			this.ExportMenuItem.Size = new System.Drawing.Size(186, 22);
			this.ExportMenuItem.Text = "&Export";
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(183, 6);
			// 
			// ExitMenuItem
			// 
			this.ExitMenuItem.Name = "ExitMenuItem";
			this.ExitMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
			this.ExitMenuItem.Size = new System.Drawing.Size(186, 22);
			this.ExitMenuItem.Text = "E&xit";
			this.ExitMenuItem.Click += new System.EventHandler(this.ExitMenuItem_Click);
			// 
			// editToolStripMenuItem
			// 
			this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.UndoMenuItem,
            this.RedoMenuItem,
            this.SelectionUndoMenuItem,
            this.SelectionRedoMenuItem,
            this.toolStripSeparator5,
            this.DeselectMenuItem,
            this.selectAllToolStripMenuItem,
            this.SelectBetweenMarkersMenuItem,
            this.reselectClipboardToolStripMenuItem,
            this.toolStripSeparator7,
            this.copyToolStripMenuItem,
            this.pasteToolStripMenuItem,
            this.pasteInsertToolStripMenuItem,
            this.cutToolStripMenuItem,
            this.toolStripSeparator8,
            this.clearToolStripMenuItem2,
            this.deleteFramesToolStripMenuItem,
            this.cloneToolStripMenuItem,
            this.insertFrameToolStripMenuItem,
            this.insertNumFramesToolStripMenuItem,
            this.toolStripSeparator6,
            this.TruncateMenuItem});
			this.editToolStripMenuItem.Name = "editToolStripMenuItem";
			this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
			this.editToolStripMenuItem.Text = "&Edit";
			// 
			// UndoMenuItem
			// 
			this.UndoMenuItem.Enabled = false;
			this.UndoMenuItem.Name = "UndoMenuItem";
			this.UndoMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Z)));
			this.UndoMenuItem.Size = new System.Drawing.Size(240, 22);
			this.UndoMenuItem.Text = "&Undo";
			// 
			// RedoMenuItem
			// 
			this.RedoMenuItem.Enabled = false;
			this.RedoMenuItem.Name = "RedoMenuItem";
			this.RedoMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Y)));
			this.RedoMenuItem.Size = new System.Drawing.Size(240, 22);
			this.RedoMenuItem.Text = "&Redo";
			// 
			// SelectionUndoMenuItem
			// 
			this.SelectionUndoMenuItem.Enabled = false;
			this.SelectionUndoMenuItem.Name = "SelectionUndoMenuItem";
			this.SelectionUndoMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Q)));
			this.SelectionUndoMenuItem.Size = new System.Drawing.Size(240, 22);
			this.SelectionUndoMenuItem.Text = "Selection Undo";
			// 
			// SelectionRedoMenuItem
			// 
			this.SelectionRedoMenuItem.Enabled = false;
			this.SelectionRedoMenuItem.Name = "SelectionRedoMenuItem";
			this.SelectionRedoMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.W)));
			this.SelectionRedoMenuItem.Size = new System.Drawing.Size(240, 22);
			this.SelectionRedoMenuItem.Text = "Selection Redo";
			// 
			// toolStripSeparator5
			// 
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			this.toolStripSeparator5.Size = new System.Drawing.Size(237, 6);
			// 
			// DeselectMenuItem
			// 
			this.DeselectMenuItem.Enabled = false;
			this.DeselectMenuItem.Name = "DeselectMenuItem";
			this.DeselectMenuItem.Size = new System.Drawing.Size(240, 22);
			this.DeselectMenuItem.Text = "Deselect";
			// 
			// selectAllToolStripMenuItem
			// 
			this.selectAllToolStripMenuItem.Enabled = false;
			this.selectAllToolStripMenuItem.Name = "selectAllToolStripMenuItem";
			this.selectAllToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
			this.selectAllToolStripMenuItem.Text = "Select &All";
			// 
			// SelectBetweenMarkersMenuItem
			// 
			this.SelectBetweenMarkersMenuItem.Enabled = false;
			this.SelectBetweenMarkersMenuItem.Name = "SelectBetweenMarkersMenuItem";
			this.SelectBetweenMarkersMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
			this.SelectBetweenMarkersMenuItem.Size = new System.Drawing.Size(240, 22);
			this.SelectBetweenMarkersMenuItem.Text = "Select between Markers";
			// 
			// reselectClipboardToolStripMenuItem
			// 
			this.reselectClipboardToolStripMenuItem.Enabled = false;
			this.reselectClipboardToolStripMenuItem.Name = "reselectClipboardToolStripMenuItem";
			this.reselectClipboardToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.B)));
			this.reselectClipboardToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
			this.reselectClipboardToolStripMenuItem.Text = "Reselect Clipboard";
			// 
			// toolStripSeparator7
			// 
			this.toolStripSeparator7.Name = "toolStripSeparator7";
			this.toolStripSeparator7.Size = new System.Drawing.Size(237, 6);
			// 
			// copyToolStripMenuItem
			// 
			this.copyToolStripMenuItem.Enabled = false;
			this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
			this.copyToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
			this.copyToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
			this.copyToolStripMenuItem.Text = "Copy";
			// 
			// pasteToolStripMenuItem
			// 
			this.pasteToolStripMenuItem.Enabled = false;
			this.pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
			this.pasteToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.V)));
			this.pasteToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
			this.pasteToolStripMenuItem.Text = "&Paste";
			// 
			// pasteInsertToolStripMenuItem
			// 
			this.pasteInsertToolStripMenuItem.Enabled = false;
			this.pasteInsertToolStripMenuItem.Name = "pasteInsertToolStripMenuItem";
			this.pasteInsertToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.V)));
			this.pasteInsertToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
			this.pasteInsertToolStripMenuItem.Text = "&Paste Insert";
			// 
			// cutToolStripMenuItem
			// 
			this.cutToolStripMenuItem.Enabled = false;
			this.cutToolStripMenuItem.Name = "cutToolStripMenuItem";
			this.cutToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.X)));
			this.cutToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
			this.cutToolStripMenuItem.Text = "&Cut";
			// 
			// toolStripSeparator8
			// 
			this.toolStripSeparator8.Name = "toolStripSeparator8";
			this.toolStripSeparator8.Size = new System.Drawing.Size(237, 6);
			// 
			// clearToolStripMenuItem2
			// 
			this.clearToolStripMenuItem2.Enabled = false;
			this.clearToolStripMenuItem2.Name = "clearToolStripMenuItem2";
			this.clearToolStripMenuItem2.ShortcutKeyDisplayString = "Del";
			this.clearToolStripMenuItem2.Size = new System.Drawing.Size(240, 22);
			this.clearToolStripMenuItem2.Text = "Clear";
			// 
			// deleteFramesToolStripMenuItem
			// 
			this.deleteFramesToolStripMenuItem.Enabled = false;
			this.deleteFramesToolStripMenuItem.Name = "deleteFramesToolStripMenuItem";
			this.deleteFramesToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Delete)));
			this.deleteFramesToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
			this.deleteFramesToolStripMenuItem.Text = "&Delete";
			// 
			// cloneToolStripMenuItem
			// 
			this.cloneToolStripMenuItem.Enabled = false;
			this.cloneToolStripMenuItem.Name = "cloneToolStripMenuItem";
			this.cloneToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Insert)));
			this.cloneToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
			this.cloneToolStripMenuItem.Text = "&Clone";
			// 
			// insertFrameToolStripMenuItem
			// 
			this.insertFrameToolStripMenuItem.Enabled = false;
			this.insertFrameToolStripMenuItem.Name = "insertFrameToolStripMenuItem";
			this.insertFrameToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.Insert)));
			this.insertFrameToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
			this.insertFrameToolStripMenuItem.Text = "&Insert";
			// 
			// insertNumFramesToolStripMenuItem
			// 
			this.insertNumFramesToolStripMenuItem.Enabled = false;
			this.insertNumFramesToolStripMenuItem.Name = "insertNumFramesToolStripMenuItem";
			this.insertNumFramesToolStripMenuItem.ShortcutKeyDisplayString = "Ins";
			this.insertNumFramesToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
			this.insertNumFramesToolStripMenuItem.Text = "Insert # of Frames";
			// 
			// toolStripSeparator6
			// 
			this.toolStripSeparator6.Name = "toolStripSeparator6";
			this.toolStripSeparator6.Size = new System.Drawing.Size(237, 6);
			// 
			// TruncateMenuItem
			// 
			this.TruncateMenuItem.Enabled = false;
			this.TruncateMenuItem.Name = "TruncateMenuItem";
			this.TruncateMenuItem.Size = new System.Drawing.Size(240, 22);
			this.TruncateMenuItem.Text = "&Truncate Movie";
			// 
			// ConfigSubMenu
			// 
			this.ConfigSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ProjectOptionsMenuItem,
            this.SetMaxUndoLevelsMenuItem,
            this.SetGreenzoneCapacityMenuItem,
            this.toolStripSeparator9,
            this.EnableGreenzoningMenuItem,
            this.AutofirePatternSkipsLagMenuItem,
            this.AutoadjustInputMenuItem,
            this.toolStripSeparator11,
            this.DrawInputByDraggingMenuItem,
            this.CombineConsecutiveRecordingsMenuItem,
            this.Use1PKeysMenuItem,
            this.UseInputKeysItem,
            this.toolStripSeparator4,
            this.BindMarkersToInputMenuItem,
            this.EmptyNewMarkerNotesMenuItem,
            this.toolStripSeparator13,
            this.BranchesRestoreEntireMovieMenuItem,
            this.OsdInBranchScreenshotsMenuItem,
            this.toolStripSeparator14,
            this.AutopauseAtEndOfMovieMenuItem});
			this.ConfigSubMenu.Name = "ConfigSubMenu";
			this.ConfigSubMenu.Size = new System.Drawing.Size(55, 20);
			this.ConfigSubMenu.Text = "&Config";
			this.ConfigSubMenu.DropDownOpened += new System.EventHandler(this.ConfigSubMenu_DropDownOpened);
			// 
			// ProjectOptionsMenuItem
			// 
			this.ProjectOptionsMenuItem.Enabled = false;
			this.ProjectOptionsMenuItem.Name = "ProjectOptionsMenuItem";
			this.ProjectOptionsMenuItem.Size = new System.Drawing.Size(288, 22);
			this.ProjectOptionsMenuItem.Text = "&Project file saving options";
			// 
			// SetMaxUndoLevelsMenuItem
			// 
			this.SetMaxUndoLevelsMenuItem.Enabled = false;
			this.SetMaxUndoLevelsMenuItem.Name = "SetMaxUndoLevelsMenuItem";
			this.SetMaxUndoLevelsMenuItem.Size = new System.Drawing.Size(288, 22);
			this.SetMaxUndoLevelsMenuItem.Text = "Set max Undo Levels";
			// 
			// SetGreenzoneCapacityMenuItem
			// 
			this.SetGreenzoneCapacityMenuItem.Enabled = false;
			this.SetGreenzoneCapacityMenuItem.Name = "SetGreenzoneCapacityMenuItem";
			this.SetGreenzoneCapacityMenuItem.Size = new System.Drawing.Size(288, 22);
			this.SetGreenzoneCapacityMenuItem.Text = "Set Greenzone capacity";
			// 
			// toolStripSeparator9
			// 
			this.toolStripSeparator9.Name = "toolStripSeparator9";
			this.toolStripSeparator9.Size = new System.Drawing.Size(285, 6);
			// 
			// EnableGreenzoningMenuItem
			// 
			this.EnableGreenzoningMenuItem.Enabled = false;
			this.EnableGreenzoningMenuItem.Name = "EnableGreenzoningMenuItem";
			this.EnableGreenzoningMenuItem.Size = new System.Drawing.Size(288, 22);
			this.EnableGreenzoningMenuItem.Text = "Enable Greenzoning";
			// 
			// AutofirePatternSkipsLagMenuItem
			// 
			this.AutofirePatternSkipsLagMenuItem.Enabled = false;
			this.AutofirePatternSkipsLagMenuItem.Name = "AutofirePatternSkipsLagMenuItem";
			this.AutofirePatternSkipsLagMenuItem.Size = new System.Drawing.Size(288, 22);
			this.AutofirePatternSkipsLagMenuItem.Text = "Autofire Pattern skips Lag";
			// 
			// AutoadjustInputMenuItem
			// 
			this.AutoadjustInputMenuItem.Enabled = false;
			this.AutoadjustInputMenuItem.Name = "AutoadjustInputMenuItem";
			this.AutoadjustInputMenuItem.Size = new System.Drawing.Size(288, 22);
			this.AutoadjustInputMenuItem.Text = "Auto-adjust Input according to Lag";
			// 
			// toolStripSeparator11
			// 
			this.toolStripSeparator11.Name = "toolStripSeparator11";
			this.toolStripSeparator11.Size = new System.Drawing.Size(285, 6);
			// 
			// DrawInputByDraggingMenuItem
			// 
			this.DrawInputByDraggingMenuItem.Name = "DrawInputByDraggingMenuItem";
			this.DrawInputByDraggingMenuItem.Size = new System.Drawing.Size(288, 22);
			this.DrawInputByDraggingMenuItem.Text = "Draw Input by dragging";
			this.DrawInputByDraggingMenuItem.Click += new System.EventHandler(this.DrawInputByDraggingMenuItem_Click);
			// 
			// CombineConsecutiveRecordingsMenuItem
			// 
			this.CombineConsecutiveRecordingsMenuItem.Enabled = false;
			this.CombineConsecutiveRecordingsMenuItem.Name = "CombineConsecutiveRecordingsMenuItem";
			this.CombineConsecutiveRecordingsMenuItem.Size = new System.Drawing.Size(288, 22);
			this.CombineConsecutiveRecordingsMenuItem.Text = "Combine consecutive Recordings/Draws";
			// 
			// Use1PKeysMenuItem
			// 
			this.Use1PKeysMenuItem.Enabled = false;
			this.Use1PKeysMenuItem.Name = "Use1PKeysMenuItem";
			this.Use1PKeysMenuItem.Size = new System.Drawing.Size(288, 22);
			this.Use1PKeysMenuItem.Text = "Use 1P keys for all single Recordings";
			// 
			// UseInputKeysItem
			// 
			this.UseInputKeysItem.Enabled = false;
			this.UseInputKeysItem.Name = "UseInputKeysItem";
			this.UseInputKeysItem.Size = new System.Drawing.Size(288, 22);
			this.UseInputKeysItem.Text = "Use Input keys for Column Set";
			// 
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size(285, 6);
			// 
			// BindMarkersToInputMenuItem
			// 
			this.BindMarkersToInputMenuItem.Enabled = false;
			this.BindMarkersToInputMenuItem.Name = "BindMarkersToInputMenuItem";
			this.BindMarkersToInputMenuItem.Size = new System.Drawing.Size(288, 22);
			this.BindMarkersToInputMenuItem.Text = "Bind Markers to Input";
			// 
			// EmptyNewMarkerNotesMenuItem
			// 
			this.EmptyNewMarkerNotesMenuItem.Enabled = false;
			this.EmptyNewMarkerNotesMenuItem.Name = "EmptyNewMarkerNotesMenuItem";
			this.EmptyNewMarkerNotesMenuItem.Size = new System.Drawing.Size(288, 22);
			this.EmptyNewMarkerNotesMenuItem.Text = "Empty new Marker Notes";
			// 
			// toolStripSeparator13
			// 
			this.toolStripSeparator13.Name = "toolStripSeparator13";
			this.toolStripSeparator13.Size = new System.Drawing.Size(285, 6);
			// 
			// BranchesRestoreEntireMovieMenuItem
			// 
			this.BranchesRestoreEntireMovieMenuItem.Enabled = false;
			this.BranchesRestoreEntireMovieMenuItem.Name = "BranchesRestoreEntireMovieMenuItem";
			this.BranchesRestoreEntireMovieMenuItem.Size = new System.Drawing.Size(288, 22);
			this.BranchesRestoreEntireMovieMenuItem.Text = "Branches restore entire Movie";
			// 
			// OsdInBranchScreenshotsMenuItem
			// 
			this.OsdInBranchScreenshotsMenuItem.Enabled = false;
			this.OsdInBranchScreenshotsMenuItem.Name = "OsdInBranchScreenshotsMenuItem";
			this.OsdInBranchScreenshotsMenuItem.Size = new System.Drawing.Size(288, 22);
			this.OsdInBranchScreenshotsMenuItem.Text = "OSD in Branch screenshots";
			// 
			// toolStripSeparator14
			// 
			this.toolStripSeparator14.Name = "toolStripSeparator14";
			this.toolStripSeparator14.Size = new System.Drawing.Size(285, 6);
			// 
			// AutopauseAtEndOfMovieMenuItem
			// 
			this.AutopauseAtEndOfMovieMenuItem.Enabled = false;
			this.AutopauseAtEndOfMovieMenuItem.Name = "AutopauseAtEndOfMovieMenuItem";
			this.AutopauseAtEndOfMovieMenuItem.Size = new System.Drawing.Size(288, 22);
			this.AutopauseAtEndOfMovieMenuItem.Text = "Autopause at end of Movie";
			// 
			// SettingsSubMenu
			// 
			this.SettingsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.AutoloadMenuItem,
            this.AutoloadProjectMenuItem,
            this.SaveWindowPositionMenuItem,
            this.AlwaysOnTopMenuItem,
            this.toolStripSeparator12,
            this.RestoreDefaultSettingsMenuItem});
			this.SettingsSubMenu.Name = "SettingsSubMenu";
			this.SettingsSubMenu.Size = new System.Drawing.Size(61, 20);
			this.SettingsSubMenu.Text = "&Settings";
			this.SettingsSubMenu.DropDownOpened += new System.EventHandler(this.SettingsSubMenu_DropDownOpened);
			// 
			// AutoloadMenuItem
			// 
			this.AutoloadMenuItem.Name = "AutoloadMenuItem";
			this.AutoloadMenuItem.Size = new System.Drawing.Size(199, 22);
			this.AutoloadMenuItem.Text = "Autoload";
			this.AutoloadMenuItem.Click += new System.EventHandler(this.AutoloadMenuItem_Click);
			// 
			// AutoloadProjectMenuItem
			// 
			this.AutoloadProjectMenuItem.Name = "AutoloadProjectMenuItem";
			this.AutoloadProjectMenuItem.Size = new System.Drawing.Size(199, 22);
			this.AutoloadProjectMenuItem.Text = "Autload &Project";
			this.AutoloadProjectMenuItem.Click += new System.EventHandler(this.AutoloadProjectMenuItem_Click);
			// 
			// SaveWindowPositionMenuItem
			// 
			this.SaveWindowPositionMenuItem.Name = "SaveWindowPositionMenuItem";
			this.SaveWindowPositionMenuItem.Size = new System.Drawing.Size(199, 22);
			this.SaveWindowPositionMenuItem.Text = "Save Window Position";
			this.SaveWindowPositionMenuItem.Click += new System.EventHandler(this.SaveWindowPositionMenuItem_Click);
			// 
			// AlwaysOnTopMenuItem
			// 
			this.AlwaysOnTopMenuItem.Name = "AlwaysOnTopMenuItem";
			this.AlwaysOnTopMenuItem.Size = new System.Drawing.Size(199, 22);
			this.AlwaysOnTopMenuItem.Text = "Always On Top";
			this.AlwaysOnTopMenuItem.Click += new System.EventHandler(this.AlwaysOnTopMenuItem_Click);
			// 
			// toolStripSeparator12
			// 
			this.toolStripSeparator12.Name = "toolStripSeparator12";
			this.toolStripSeparator12.Size = new System.Drawing.Size(196, 6);
			// 
			// RestoreDefaultSettingsMenuItem
			// 
			this.RestoreDefaultSettingsMenuItem.Name = "RestoreDefaultSettingsMenuItem";
			this.RestoreDefaultSettingsMenuItem.Size = new System.Drawing.Size(199, 22);
			this.RestoreDefaultSettingsMenuItem.Text = "Restore Default Settings";
			this.RestoreDefaultSettingsMenuItem.Click += new System.EventHandler(this.RestoreDefaultSettingsMenuItem_Click);
			// 
			// HelpSubMenu
			// 
			this.HelpSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.EnableTooltipsMenuItem,
            this.toolStripSeparator10,
            this.aboutToolStripMenuItem});
			this.HelpSubMenu.Name = "HelpSubMenu";
			this.HelpSubMenu.Size = new System.Drawing.Size(44, 20);
			this.HelpSubMenu.Text = "&Help";
			// 
			// EnableTooltipsMenuItem
			// 
			this.EnableTooltipsMenuItem.Enabled = false;
			this.EnableTooltipsMenuItem.Name = "EnableTooltipsMenuItem";
			this.EnableTooltipsMenuItem.Size = new System.Drawing.Size(155, 22);
			this.EnableTooltipsMenuItem.Text = "&Enable Tooltips";
			// 
			// toolStripSeparator10
			// 
			this.toolStripSeparator10.Name = "toolStripSeparator10";
			this.toolStripSeparator10.Size = new System.Drawing.Size(152, 6);
			// 
			// aboutToolStripMenuItem
			// 
			this.aboutToolStripMenuItem.Enabled = false;
			this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
			this.aboutToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
			this.aboutToolStripMenuItem.Text = "&About";
			// 
			// TASView
			// 
			this.TASView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.TASView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Frame,
            this.Log});
			this.TASView.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.TASView.FullRowSelect = true;
			this.TASView.GridLines = true;
			this.TASView.InputPaintingMode = false;
			this.TASView.ItemCount = 0;
			this.TASView.Location = new System.Drawing.Point(12, 43);
			this.TASView.Name = "TASView";
			this.TASView.selectedItem = -1;
			this.TASView.Size = new System.Drawing.Size(291, 452);
			this.TASView.TabIndex = 1;
			this.TASView.UseCompatibleStateImageBehavior = false;
			this.TASView.View = System.Windows.Forms.View.Details;
			this.TASView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.TASView_MouseDown);
			this.TASView.MouseUp += new System.Windows.Forms.MouseEventHandler(this.TASView_MouseUp);
			// 
			// Frame
			// 
			this.Frame.Text = "Frame";
			// 
			// Log
			// 
			this.Log.Text = "Log";
			this.Log.Width = 222;
			// 
			// MarkerLabel
			// 
			this.MarkerLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.MarkerLabel.AutoSize = true;
			this.MarkerLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.MarkerLabel.ForeColor = System.Drawing.Color.DeepSkyBlue;
			this.MarkerLabel.Location = new System.Drawing.Point(12, 496);
			this.MarkerLabel.Name = "MarkerLabel";
			this.MarkerLabel.Size = new System.Drawing.Size(100, 16);
			this.MarkerLabel.TabIndex = 2;
			this.MarkerLabel.Text = "Marker 99999";
			// 
			// MarkerDescriptionBox
			// 
			this.MarkerDescriptionBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.MarkerDescriptionBox.Location = new System.Drawing.Point(118, 495);
			this.MarkerDescriptionBox.Name = "MarkerDescriptionBox";
			this.MarkerDescriptionBox.Size = new System.Drawing.Size(185, 20);
			this.MarkerDescriptionBox.TabIndex = 3;
			// 
			// TopMarkerBox
			// 
			this.TopMarkerBox.Location = new System.Drawing.Point(115, 23);
			this.TopMarkerBox.Name = "TopMarkerBox";
			this.TopMarkerBox.Size = new System.Drawing.Size(188, 20);
			this.TopMarkerBox.TabIndex = 5;
			// 
			// TopMarkerLabel
			// 
			this.TopMarkerLabel.AutoSize = true;
			this.TopMarkerLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.TopMarkerLabel.ForeColor = System.Drawing.Color.DeepSkyBlue;
			this.TopMarkerLabel.Location = new System.Drawing.Point(9, 24);
			this.TopMarkerLabel.Name = "TopMarkerLabel";
			this.TopMarkerLabel.Size = new System.Drawing.Size(100, 16);
			this.TopMarkerLabel.TabIndex = 4;
			this.TopMarkerLabel.Text = "Marker 99999";
			// 
			// TAStudio
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(530, 519);
			this.Controls.Add(this.TopMarkerBox);
			this.Controls.Add(this.TopMarkerLabel);
			this.Controls.Add(this.MarkerDescriptionBox);
			this.Controls.Add(this.MarkerLabel);
			this.Controls.Add(this.TASMenu);
			this.Controls.Add(this.TASView);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.TASMenu;
			this.MinimumSize = new System.Drawing.Size(437, 148);
			this.Name = "TAStudio";
			this.StartPosition = System.Windows.Forms.FormStartPosition.WindowsDefaultBounds;
			this.Text = "TAStudio";
			this.Load += new System.EventHandler(this.TAStudio_Load);
			this.TASMenu.ResumeLayout(false);
			this.TASMenu.PerformLayout();
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
		private System.Windows.Forms.ToolStripMenuItem ImportMenuItem;
		private System.Windows.Forms.ToolStripMenuItem ExitMenuItem;
		private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem ConfigSubMenu;
		private TasListView TASView;
		private System.Windows.Forms.ColumnHeader Log;
		private System.Windows.Forms.ToolStripMenuItem RecentSubMenu;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ColumnHeader Frame;
		private System.Windows.Forms.ToolStripMenuItem insertFrameToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
		private System.Windows.Forms.ToolStripMenuItem cloneToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem deleteFramesToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem clearToolStripMenuItem2;
		private System.Windows.Forms.ToolStripMenuItem insertNumFramesToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem selectAllToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
		private System.Windows.Forms.ToolStripMenuItem TruncateMenuItem;
		private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem pasteToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem pasteInsertToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem cutToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem ExportMenuItem;
		private System.Windows.Forms.ToolStripMenuItem UndoMenuItem;
		private System.Windows.Forms.ToolStripMenuItem RedoMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SelectionUndoMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SelectionRedoMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
		private System.Windows.Forms.ToolStripMenuItem DeselectMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SelectBetweenMarkersMenuItem;
		private System.Windows.Forms.ToolStripMenuItem reselectClipboardToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
		private System.Windows.Forms.ToolStripMenuItem ProjectOptionsMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator9;
		private System.Windows.Forms.ToolStripMenuItem HelpSubMenu;
		private System.Windows.Forms.ToolStripMenuItem EnableTooltipsMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator10;
		private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SetMaxUndoLevelsMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SetGreenzoneCapacityMenuItem;
		private System.Windows.Forms.ToolStripMenuItem EnableGreenzoningMenuItem;
		private System.Windows.Forms.ToolStripMenuItem AutofirePatternSkipsLagMenuItem;
		private System.Windows.Forms.ToolStripMenuItem AutoadjustInputMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator11;
		private System.Windows.Forms.ToolStripMenuItem DrawInputByDraggingMenuItem;
		private System.Windows.Forms.ToolStripMenuItem CombineConsecutiveRecordingsMenuItem;
		private System.Windows.Forms.ToolStripMenuItem Use1PKeysMenuItem;
		private System.Windows.Forms.ToolStripMenuItem UseInputKeysItem;
		private System.Windows.Forms.ToolStripMenuItem BindMarkersToInputMenuItem;
		private System.Windows.Forms.ToolStripMenuItem EmptyNewMarkerNotesMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator13;
		private System.Windows.Forms.ToolStripMenuItem BranchesRestoreEntireMovieMenuItem;
		private System.Windows.Forms.ToolStripMenuItem OsdInBranchScreenshotsMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator14;
		private System.Windows.Forms.ToolStripMenuItem AutopauseAtEndOfMovieMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SettingsSubMenu;
		private System.Windows.Forms.ToolStripMenuItem AutoloadMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SaveWindowPositionMenuItem;
		private System.Windows.Forms.ToolStripMenuItem AlwaysOnTopMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator12;
		private System.Windows.Forms.ToolStripMenuItem RestoreDefaultSettingsMenuItem;
		private System.Windows.Forms.Label MarkerLabel;
		private System.Windows.Forms.TextBox MarkerDescriptionBox;
		private System.Windows.Forms.TextBox TopMarkerBox;
		private System.Windows.Forms.Label TopMarkerLabel;
		private System.Windows.Forms.ToolStripMenuItem AutoloadProjectMenuItem;
	}
}