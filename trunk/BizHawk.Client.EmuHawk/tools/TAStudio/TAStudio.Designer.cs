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
			this.ToBk2MenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.ExitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.UndoMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.RedoMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SelectionUndoMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SelectionRedoMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.DeselectMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SelectAllMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SelectBetweenMarkersMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ReselectClipboardMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
			this.CopyMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.PasteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.PasteInsertMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.CutMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
			this.ClearMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.DeleteFramesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.CloneMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.InsertFrameMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.InsertNumFramesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
			this.TruncateMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ConfigSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.SetMaxUndoLevelsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
			this.AutofirePatternSkipsLagMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.AutoadjustInputMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator11 = new System.Windows.Forms.ToolStripSeparator();
			this.DrawInputByDraggingMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.CombineConsecutiveRecordingsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.UseInputKeysItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.BindMarkersToInputMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.EmptyNewMarkerNotesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator13 = new System.Windows.Forms.ToolStripSeparator();
			this.BranchesRestoreEntireMovieMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.OsdInBranchScreenshotsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator14 = new System.Windows.Forms.ToolStripSeparator();
			this.AutopauseAtEndOfMovieMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.MetaSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.HeaderMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.GreenzoneSettingsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.CommentsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SubtitlesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SettingsSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.AutoloadMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.AutoloadProjectMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SaveWindowPositionMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.AlwaysOnTopMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FloatingWindowMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator12 = new System.Windows.Forms.ToolStripSeparator();
			this.RestoreDefaultSettingsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.HelpSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.EnableTooltipsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator10 = new System.Windows.Forms.ToolStripSeparator();
			this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.TasView = new BizHawk.Client.EmuHawk.TasListView();
			this.Frame = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Log = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.TasStatusStrip = new StatusStripEx();
			this.MessageStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.SplicerStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.TasPlaybackBox = new BizHawk.Client.EmuHawk.PlaybackBox();
			this.TASMenu.SuspendLayout();
			this.TasStatusStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// TASMenu
			// 
			this.TASMenu.ClickThrough = true;
			this.TASMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu,
            this.editToolStripMenuItem,
            this.ConfigSubMenu,
            this.MetaSubMenu,
            this.SettingsSubMenu,
            this.HelpSubMenu});
			this.TASMenu.Location = new System.Drawing.Point(0, 0);
			this.TASMenu.Name = "TASMenu";
			this.TASMenu.Size = new System.Drawing.Size(506, 24);
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
			this.NewTASMenuItem.Size = new System.Drawing.Size(186, 22);
			this.NewTASMenuItem.Text = "&New";
			this.NewTASMenuItem.Click += new System.EventHandler(this.NewTasMenuItem_Click);
			// 
			// OpenTASMenuItem
			// 
			this.OpenTASMenuItem.Name = "OpenTASMenuItem";
			this.OpenTASMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.OpenTASMenuItem.Size = new System.Drawing.Size(186, 22);
			this.OpenTASMenuItem.Text = "&Open";
			this.OpenTASMenuItem.Click += new System.EventHandler(this.OpenTasMenuItem_Click);
			// 
			// SaveTASMenuItem
			// 
			this.SaveTASMenuItem.Name = "SaveTASMenuItem";
			this.SaveTASMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			this.SaveTASMenuItem.Size = new System.Drawing.Size(186, 22);
			this.SaveTASMenuItem.Text = "&Save";
			this.SaveTASMenuItem.Click += new System.EventHandler(this.SaveTasMenuItem_Click);
			// 
			// SaveAsTASMenuItem
			// 
			this.SaveAsTASMenuItem.Name = "SaveAsTASMenuItem";
			this.SaveAsTASMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.S)));
			this.SaveAsTASMenuItem.Size = new System.Drawing.Size(186, 22);
			this.SaveAsTASMenuItem.Text = "Save As";
			this.SaveAsTASMenuItem.Click += new System.EventHandler(this.SaveAsTasMenuItem_Click);
			// 
			// RecentSubMenu
			// 
			this.RecentSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator3});
			this.RecentSubMenu.Image = ((System.Drawing.Image)(resources.GetObject("RecentSubMenu.Image")));
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
			// ToBk2MenuItem
			// 
			this.ToBk2MenuItem.Name = "ToBk2MenuItem";
			this.ToBk2MenuItem.Size = new System.Drawing.Size(186, 22);
			this.ToBk2MenuItem.Text = "&Export to Bk2";
			this.ToBk2MenuItem.Click += new System.EventHandler(this.ToBk2MenuItem_Click);
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
            this.SelectAllMenuItem,
            this.SelectBetweenMarkersMenuItem,
            this.ReselectClipboardMenuItem,
            this.toolStripSeparator7,
            this.CopyMenuItem,
            this.PasteMenuItem,
            this.PasteInsertMenuItem,
            this.CutMenuItem,
            this.toolStripSeparator8,
            this.ClearMenuItem,
            this.DeleteFramesMenuItem,
            this.CloneMenuItem,
            this.InsertFrameMenuItem,
            this.InsertNumFramesMenuItem,
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
			this.DeselectMenuItem.Name = "DeselectMenuItem";
			this.DeselectMenuItem.Size = new System.Drawing.Size(240, 22);
			this.DeselectMenuItem.Text = "Deselect";
			this.DeselectMenuItem.Click += new System.EventHandler(this.DeselectMenuItem_Click);
			// 
			// SelectAllMenuItem
			// 
			this.SelectAllMenuItem.Name = "SelectAllMenuItem";
			this.SelectAllMenuItem.ShortcutKeyDisplayString = "Ctrl+A";
			this.SelectAllMenuItem.Size = new System.Drawing.Size(240, 22);
			this.SelectAllMenuItem.Text = "Select &All";
			this.SelectAllMenuItem.Click += new System.EventHandler(this.SelectAllMenuItem_Click);
			// 
			// SelectBetweenMarkersMenuItem
			// 
			this.SelectBetweenMarkersMenuItem.Enabled = false;
			this.SelectBetweenMarkersMenuItem.Name = "SelectBetweenMarkersMenuItem";
			this.SelectBetweenMarkersMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
			this.SelectBetweenMarkersMenuItem.Size = new System.Drawing.Size(240, 22);
			this.SelectBetweenMarkersMenuItem.Text = "Select between Markers";
			// 
			// ReselectClipboardMenuItem
			// 
			this.ReselectClipboardMenuItem.Name = "ReselectClipboardMenuItem";
			this.ReselectClipboardMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.B)));
			this.ReselectClipboardMenuItem.Size = new System.Drawing.Size(240, 22);
			this.ReselectClipboardMenuItem.Text = "Reselect Clipboard";
			this.ReselectClipboardMenuItem.Click += new System.EventHandler(this.ReselectClipboardMenuItem_Click);
			// 
			// toolStripSeparator7
			// 
			this.toolStripSeparator7.Name = "toolStripSeparator7";
			this.toolStripSeparator7.Size = new System.Drawing.Size(237, 6);
			// 
			// CopyMenuItem
			// 
			this.CopyMenuItem.Name = "CopyMenuItem";
			this.CopyMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
			this.CopyMenuItem.Size = new System.Drawing.Size(240, 22);
			this.CopyMenuItem.Text = "Copy";
			this.CopyMenuItem.Click += new System.EventHandler(this.CopyMenuItem_Click);
			// 
			// PasteMenuItem
			// 
			this.PasteMenuItem.Name = "PasteMenuItem";
			this.PasteMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.V)));
			this.PasteMenuItem.Size = new System.Drawing.Size(240, 22);
			this.PasteMenuItem.Text = "&Paste";
			this.PasteMenuItem.Click += new System.EventHandler(this.PasteMenuItem_Click);
			// 
			// PasteInsertMenuItem
			// 
			this.PasteInsertMenuItem.Name = "PasteInsertMenuItem";
			this.PasteInsertMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.V)));
			this.PasteInsertMenuItem.Size = new System.Drawing.Size(240, 22);
			this.PasteInsertMenuItem.Text = "&Paste Insert";
			this.PasteInsertMenuItem.Click += new System.EventHandler(this.PasteInsertMenuItem_Click);
			// 
			// CutMenuItem
			// 
			this.CutMenuItem.Name = "CutMenuItem";
			this.CutMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.X)));
			this.CutMenuItem.Size = new System.Drawing.Size(240, 22);
			this.CutMenuItem.Text = "&Cut";
			this.CutMenuItem.Click += new System.EventHandler(this.CutMenuItem_Click);
			// 
			// toolStripSeparator8
			// 
			this.toolStripSeparator8.Name = "toolStripSeparator8";
			this.toolStripSeparator8.Size = new System.Drawing.Size(237, 6);
			// 
			// ClearMenuItem
			// 
			this.ClearMenuItem.Name = "ClearMenuItem";
			this.ClearMenuItem.ShortcutKeyDisplayString = "";
			this.ClearMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Delete)));
			this.ClearMenuItem.Size = new System.Drawing.Size(240, 22);
			this.ClearMenuItem.Text = "Clear";
			this.ClearMenuItem.Click += new System.EventHandler(this.ClearMenuItem_Click);
			// 
			// DeleteFramesMenuItem
			// 
			this.DeleteFramesMenuItem.Name = "DeleteFramesMenuItem";
			this.DeleteFramesMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Delete;
			this.DeleteFramesMenuItem.Size = new System.Drawing.Size(240, 22);
			this.DeleteFramesMenuItem.Text = "&Delete";
			this.DeleteFramesMenuItem.Click += new System.EventHandler(this.DeleteFramesMenuItem_Click);
			// 
			// CloneMenuItem
			// 
			this.CloneMenuItem.Name = "CloneMenuItem";
			this.CloneMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Insert)));
			this.CloneMenuItem.Size = new System.Drawing.Size(240, 22);
			this.CloneMenuItem.Text = "&Clone";
			this.CloneMenuItem.Click += new System.EventHandler(this.CloneMenuItem_Click);
			// 
			// InsertFrameMenuItem
			// 
			this.InsertFrameMenuItem.Name = "InsertFrameMenuItem";
			this.InsertFrameMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.Insert)));
			this.InsertFrameMenuItem.Size = new System.Drawing.Size(240, 22);
			this.InsertFrameMenuItem.Text = "&Insert";
			this.InsertFrameMenuItem.Click += new System.EventHandler(this.InsertFrameMenuItem_Click);
			// 
			// InsertNumFramesMenuItem
			// 
			this.InsertNumFramesMenuItem.Name = "InsertNumFramesMenuItem";
			this.InsertNumFramesMenuItem.ShortcutKeyDisplayString = "Ins";
			this.InsertNumFramesMenuItem.Size = new System.Drawing.Size(240, 22);
			this.InsertNumFramesMenuItem.Text = "Insert # of Frames";
			this.InsertNumFramesMenuItem.Click += new System.EventHandler(this.InsertNumFramesMenuItem_Click);
			// 
			// toolStripSeparator6
			// 
			this.toolStripSeparator6.Name = "toolStripSeparator6";
			this.toolStripSeparator6.Size = new System.Drawing.Size(237, 6);
			// 
			// TruncateMenuItem
			// 
			this.TruncateMenuItem.Name = "TruncateMenuItem";
			this.TruncateMenuItem.Size = new System.Drawing.Size(240, 22);
			this.TruncateMenuItem.Text = "&Truncate Movie";
			this.TruncateMenuItem.Click += new System.EventHandler(this.TruncateMenuItem_Click);
			// 
			// ConfigSubMenu
			// 
			this.ConfigSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SetMaxUndoLevelsMenuItem,
            this.toolStripSeparator9,
            this.AutofirePatternSkipsLagMenuItem,
            this.AutoadjustInputMenuItem,
            this.toolStripSeparator11,
            this.DrawInputByDraggingMenuItem,
            this.CombineConsecutiveRecordingsMenuItem,
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
			// SetMaxUndoLevelsMenuItem
			// 
			this.SetMaxUndoLevelsMenuItem.Enabled = false;
			this.SetMaxUndoLevelsMenuItem.Name = "SetMaxUndoLevelsMenuItem";
			this.SetMaxUndoLevelsMenuItem.Size = new System.Drawing.Size(288, 22);
			this.SetMaxUndoLevelsMenuItem.Text = "Set max Undo Levels";
			// 
			// toolStripSeparator9
			// 
			this.toolStripSeparator9.Name = "toolStripSeparator9";
			this.toolStripSeparator9.Size = new System.Drawing.Size(285, 6);
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
			// MetaSubMenu
			// 
			this.MetaSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.HeaderMenuItem,
            this.GreenzoneSettingsMenuItem,
            this.CommentsMenuItem,
            this.SubtitlesMenuItem});
			this.MetaSubMenu.Name = "MetaSubMenu";
			this.MetaSubMenu.Size = new System.Drawing.Size(69, 20);
			this.MetaSubMenu.Text = "&Metadata";
			// 
			// HeaderMenuItem
			// 
			this.HeaderMenuItem.Name = "HeaderMenuItem";
			this.HeaderMenuItem.Size = new System.Drawing.Size(184, 22);
			this.HeaderMenuItem.Text = "&Header...";
			this.HeaderMenuItem.Click += new System.EventHandler(this.HeaderMenuItem_Click);
			// 
			// GreenzoneSettingsMenuItem
			// 
			this.GreenzoneSettingsMenuItem.Name = "GreenzoneSettingsMenuItem";
			this.GreenzoneSettingsMenuItem.Size = new System.Drawing.Size(184, 22);
			this.GreenzoneSettingsMenuItem.Text = "&Greenzone Settings...";
			this.GreenzoneSettingsMenuItem.Click += new System.EventHandler(this.GreenzoneSettingsMenuItem_Click);
			// 
			// CommentsMenuItem
			// 
			this.CommentsMenuItem.Name = "CommentsMenuItem";
			this.CommentsMenuItem.Size = new System.Drawing.Size(184, 22);
			this.CommentsMenuItem.Text = "&Comments...";
			this.CommentsMenuItem.Click += new System.EventHandler(this.CommentsMenuItem_Click);
			// 
			// SubtitlesMenuItem
			// 
			this.SubtitlesMenuItem.Name = "SubtitlesMenuItem";
			this.SubtitlesMenuItem.Size = new System.Drawing.Size(184, 22);
			this.SubtitlesMenuItem.Text = "&Subtitles...";
			this.SubtitlesMenuItem.Click += new System.EventHandler(this.SubtitlesMenuItem_Click);
			// 
			// SettingsSubMenu
			// 
			this.SettingsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.AutoloadMenuItem,
            this.AutoloadProjectMenuItem,
            this.SaveWindowPositionMenuItem,
            this.AlwaysOnTopMenuItem,
            this.FloatingWindowMenuItem,
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
			// FloatingWindowMenuItem
			// 
			this.FloatingWindowMenuItem.Name = "FloatingWindowMenuItem";
			this.FloatingWindowMenuItem.Size = new System.Drawing.Size(199, 22);
			this.FloatingWindowMenuItem.Text = "Floating Window";
			this.FloatingWindowMenuItem.Click += new System.EventHandler(this.FloatingWindowMenuItem_Click);
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
			// TasView
			// 
			this.TasView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.TasView.BlazingFast = false;
			this.TasView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Frame,
            this.Log});
			this.TasView.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.TasView.FullRowSelect = true;
			this.TasView.GridLines = true;
			this.TasView.InputPaintingMode = false;
			this.TasView.ItemCount = 0;
			this.TasView.Location = new System.Drawing.Point(8, 27);
			this.TasView.Name = "TasView";
			this.TasView.SelectAllInProgress = false;
			this.TasView.selectedItem = -1;
			this.TasView.Size = new System.Drawing.Size(288, 471);
			this.TasView.TabIndex = 1;
			this.TasView.UseCompatibleStateImageBehavior = false;
			this.TasView.View = System.Windows.Forms.View.Details;
			this.TasView.SelectedIndexChanged += new System.EventHandler(this.TasView_SelectedIndexChanged);
			this.TasView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.TasView_MouseDown);
			this.TasView.MouseUp += new System.Windows.Forms.MouseEventHandler(this.TasView_MouseUp);
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
			// TasStatusStrip
			// 
			this.TasStatusStrip.ClickThrough = true;
			this.TasStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MessageStatusLabel,
            this.SplicerStatusLabel});
			this.TasStatusStrip.Location = new System.Drawing.Point(0, 497);
			this.TasStatusStrip.Name = "TasStatusStrip";
			this.TasStatusStrip.Size = new System.Drawing.Size(506, 22);
			this.TasStatusStrip.TabIndex = 4;
			this.TasStatusStrip.Text = "statusStrip1";
			// 
			// MessageStatusLabel
			// 
			this.MessageStatusLabel.Name = "MessageStatusLabel";
			this.MessageStatusLabel.Size = new System.Drawing.Size(105, 17);
			this.MessageStatusLabel.Text = "TAStudio engaged";
			// 
			// SplicerStatusLabel
			// 
			this.SplicerStatusLabel.Name = "SplicerStatusLabel";
			this.SplicerStatusLabel.Padding = new System.Windows.Forms.Padding(20, 0, 0, 0);
			this.SplicerStatusLabel.Size = new System.Drawing.Size(175, 17);
			this.SplicerStatusLabel.Text = "0 selected, clipboard: empty";
			// 
			// TasPlaybackBox
			// 
			this.TasPlaybackBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.TasPlaybackBox.Location = new System.Drawing.Point(302, 25);
			this.TasPlaybackBox.Name = "TasPlaybackBox";
			this.TasPlaybackBox.Size = new System.Drawing.Size(204, 120);
			this.TasPlaybackBox.TabIndex = 5;
			// 
			// TAStudio
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(506, 519);
			this.Controls.Add(this.TasPlaybackBox);
			this.Controls.Add(this.TasStatusStrip);
			this.Controls.Add(this.TASMenu);
			this.Controls.Add(this.TasView);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.TASMenu;
			this.MinimumSize = new System.Drawing.Size(437, 148);
			this.Name = "TAStudio";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "TAStudio";
			this.Load += new System.EventHandler(this.Tastudio_Load);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TAStudio_KeyDown);
			this.TASMenu.ResumeLayout(false);
			this.TASMenu.PerformLayout();
			this.TasStatusStrip.ResumeLayout(false);
			this.TasStatusStrip.PerformLayout();
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
		private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem ConfigSubMenu;
		private TasListView TasView;
		private System.Windows.Forms.ColumnHeader Log;
		private System.Windows.Forms.ToolStripMenuItem RecentSubMenu;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ColumnHeader Frame;
		private System.Windows.Forms.ToolStripMenuItem InsertFrameMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
		private System.Windows.Forms.ToolStripMenuItem CloneMenuItem;
		private System.Windows.Forms.ToolStripMenuItem DeleteFramesMenuItem;
		private System.Windows.Forms.ToolStripMenuItem ClearMenuItem;
		private System.Windows.Forms.ToolStripMenuItem InsertNumFramesMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SelectAllMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
		private System.Windows.Forms.ToolStripMenuItem TruncateMenuItem;
		private System.Windows.Forms.ToolStripMenuItem CopyMenuItem;
		private System.Windows.Forms.ToolStripMenuItem PasteMenuItem;
		private System.Windows.Forms.ToolStripMenuItem PasteInsertMenuItem;
		private System.Windows.Forms.ToolStripMenuItem CutMenuItem;
		private System.Windows.Forms.ToolStripMenuItem ToBk2MenuItem;
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
		private System.Windows.Forms.ToolStripMenuItem AutofirePatternSkipsLagMenuItem;
		private System.Windows.Forms.ToolStripMenuItem AutoadjustInputMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator11;
		private System.Windows.Forms.ToolStripMenuItem DrawInputByDraggingMenuItem;
		private System.Windows.Forms.ToolStripMenuItem CombineConsecutiveRecordingsMenuItem;
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
		private System.Windows.Forms.ToolStripMenuItem AutoloadProjectMenuItem;
		private StatusStripEx TasStatusStrip;
		private System.Windows.Forms.ToolStripStatusLabel MessageStatusLabel;
		private PlaybackBox TasPlaybackBox;
		private System.Windows.Forms.ToolStripStatusLabel SplicerStatusLabel;
		private System.Windows.Forms.ToolStripMenuItem FloatingWindowMenuItem;
		private System.Windows.Forms.ToolStripMenuItem MetaSubMenu;
		private System.Windows.Forms.ToolStripMenuItem HeaderMenuItem;
		private System.Windows.Forms.ToolStripMenuItem CommentsMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SubtitlesMenuItem;
		private System.Windows.Forms.ToolStripMenuItem GreenzoneSettingsMenuItem;
	}
}