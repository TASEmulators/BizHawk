namespace BizHawk.Client.EmuHawk
{
	partial class LuaConsole
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LuaConsole));
			this.ScriptListContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.ToggleScriptContextItem = new System.Windows.Forms.ToolStripMenuItem();
			this.PauseScriptContextItem = new System.Windows.Forms.ToolStripMenuItem();
			this.EditScriptContextItem = new System.Windows.Forms.ToolStripMenuItem();
			this.RemoveScriptContextItem = new System.Windows.Forms.ToolStripMenuItem();
			this.InsertSeperatorContextItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ScriptContextSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.StopAllScriptsContextItem = new System.Windows.Forms.ToolStripMenuItem();
			this.menuStrip1 = new MenuStripEx();
			this.FileSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.NewSessionMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.OpenSessionMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SaveSessionMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SaveSessionAsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
			this.RecentSessionsSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
			this.RecentScriptsSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.ExitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ScriptSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.NewScriptMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.OpenScriptMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ToggleScriptMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.PauseScriptMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.EditScriptMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.RemoveScriptMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.InsertSeparatorMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
			this.MoveUpMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.MoveDownMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SelectAllMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
			this.StopAllScriptsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.RegisteredFunctionsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.OptionsSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.SaveWindowPositionMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.AutoloadConsoleMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.AutoloadSessionMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.DisableScriptsOnLoadMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.RestoreDefaultSettingsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.HelpSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.FunctionsListMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.OnlineDocsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.OutputBox = new System.Windows.Forms.RichTextBox();
			this.ConsoleContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.ClearConsoleContextItem = new System.Windows.Forms.ToolStripMenuItem();
			this.RegisteredFunctionsContextItem = new System.Windows.Forms.ToolStripMenuItem();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.NumberOfScripts = new System.Windows.Forms.Label();
			this.OutputMessages = new System.Windows.Forms.Label();
			this.toolStrip1 = new ToolStripEx();
			this.NewScriptToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.OpenScriptToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.ToggleScriptToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.PauseToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.EditToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.RemoveScriptToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.InsertSeparatorToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.MoveUpToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.toolStripButtonMoveDown = new System.Windows.Forms.ToolStripButton();
			this.LuaListView = new BizHawk.Client.EmuHawk.VirtualListView();
			this.Script = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.PathName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.ScriptListContextMenu.SuspendLayout();
			this.menuStrip1.SuspendLayout();
			this.ConsoleContextMenu.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// ScriptListContextMenu
			// 
			this.ScriptListContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToggleScriptContextItem,
            this.PauseScriptContextItem,
            this.EditScriptContextItem,
            this.RemoveScriptContextItem,
            this.InsertSeperatorContextItem,
            this.ScriptContextSeparator,
            this.StopAllScriptsContextItem});
			this.ScriptListContextMenu.Name = "contextMenuStrip1";
			this.ScriptListContextMenu.Size = new System.Drawing.Size(165, 142);
			this.ScriptListContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.ScriptListContextMenu_Opening);
			// 
			// ToggleScriptContextItem
			// 
			this.ToggleScriptContextItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Refresh1;
			this.ToggleScriptContextItem.Name = "ToggleScriptContextItem";
			this.ToggleScriptContextItem.Size = new System.Drawing.Size(164, 22);
			this.ToggleScriptContextItem.Text = "&Toggle";
			this.ToggleScriptContextItem.Click += new System.EventHandler(this.ToggleScriptMenuItem_Click);
			// 
			// PauseScriptContextItem
			// 
			this.PauseScriptContextItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Pause;
			this.PauseScriptContextItem.Name = "PauseScriptContextItem";
			this.PauseScriptContextItem.Size = new System.Drawing.Size(164, 22);
			this.PauseScriptContextItem.Text = "Pause or Resume";
			this.PauseScriptContextItem.Click += new System.EventHandler(this.PauseScriptMenuItem_Click);
			// 
			// EditScriptContextItem
			// 
			this.EditScriptContextItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.CutHS;
			this.EditScriptContextItem.Name = "EditScriptContextItem";
			this.EditScriptContextItem.Size = new System.Drawing.Size(164, 22);
			this.EditScriptContextItem.Text = "&Edit";
			this.EditScriptContextItem.Click += new System.EventHandler(this.EditScriptMenuItem_Click);
			// 
			// RemoveScriptContextItem
			// 
			this.RemoveScriptContextItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Close;
			this.RemoveScriptContextItem.Name = "RemoveScriptContextItem";
			this.RemoveScriptContextItem.Size = new System.Drawing.Size(164, 22);
			this.RemoveScriptContextItem.Text = "&Remove";
			this.RemoveScriptContextItem.Click += new System.EventHandler(this.RemoveScriptMenuItem_Click);
			// 
			// InsertSeperatorContextItem
			// 
			this.InsertSeperatorContextItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.InsertSeparator;
			this.InsertSeperatorContextItem.Name = "InsertSeperatorContextItem";
			this.InsertSeperatorContextItem.Size = new System.Drawing.Size(164, 22);
			this.InsertSeperatorContextItem.Text = "Insert Seperator";
			this.InsertSeperatorContextItem.Click += new System.EventHandler(this.InsertSeparatorMenuItem_Click);
			// 
			// ScriptContextSeparator
			// 
			this.ScriptContextSeparator.Name = "ScriptContextSeparator";
			this.ScriptContextSeparator.Size = new System.Drawing.Size(161, 6);
			// 
			// StopAllScriptsContextItem
			// 
			this.StopAllScriptsContextItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Stop;
			this.StopAllScriptsContextItem.Name = "StopAllScriptsContextItem";
			this.StopAllScriptsContextItem.Size = new System.Drawing.Size(164, 22);
			this.StopAllScriptsContextItem.Text = "Stop All Scripts";
			this.StopAllScriptsContextItem.Click += new System.EventHandler(this.StopAllScriptsMenuItem_Click);
			// 
			// menuStrip1
			// 
			this.menuStrip1.ClickThrough = true;
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu,
            this.ScriptSubMenu,
            this.OptionsSubMenu,
            this.HelpSubMenu});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(584, 24);
			this.menuStrip1.TabIndex = 1;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// FileSubMenu
			// 
			this.FileSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.NewSessionMenuItem,
            this.OpenSessionMenuItem,
            this.SaveSessionMenuItem,
            this.SaveSessionAsMenuItem,
            this.toolStripSeparator9,
            this.RecentSessionsSubMenu,
            this.RecentScriptsSubMenu,
            this.toolStripSeparator1,
            this.ExitMenuItem});
			this.FileSubMenu.Name = "FileSubMenu";
			this.FileSubMenu.Size = new System.Drawing.Size(37, 20);
			this.FileSubMenu.Text = "&File";
			this.FileSubMenu.DropDownOpened += new System.EventHandler(this.FileSubMenu_DropDownOpened);
			// 
			// NewSessionMenuItem
			// 
			this.NewSessionMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.NewFile;
			this.NewSessionMenuItem.Name = "NewSessionMenuItem";
			this.NewSessionMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.N)));
			this.NewSessionMenuItem.Size = new System.Drawing.Size(237, 22);
			this.NewSessionMenuItem.Text = "&New Session";
			this.NewSessionMenuItem.Click += new System.EventHandler(this.NewSessionMenuItem_Click);
			// 
			// OpenSessionMenuItem
			// 
			this.OpenSessionMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.OpenFile;
			this.OpenSessionMenuItem.Name = "OpenSessionMenuItem";
			this.OpenSessionMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.O)));
			this.OpenSessionMenuItem.Size = new System.Drawing.Size(237, 22);
			this.OpenSessionMenuItem.Text = "&Open Session...";
			this.OpenSessionMenuItem.Click += new System.EventHandler(this.OpenSessionMenuItem_Click);
			// 
			// SaveSessionMenuItem
			// 
			this.SaveSessionMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.SaveAs;
			this.SaveSessionMenuItem.Name = "SaveSessionMenuItem";
			this.SaveSessionMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			this.SaveSessionMenuItem.Size = new System.Drawing.Size(237, 22);
			this.SaveSessionMenuItem.Text = "&Save Session";
			this.SaveSessionMenuItem.Click += new System.EventHandler(this.SaveSessionMenuItem_Click);
			// 
			// SaveSessionAsMenuItem
			// 
			this.SaveSessionAsMenuItem.Name = "SaveSessionAsMenuItem";
			this.SaveSessionAsMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.S)));
			this.SaveSessionAsMenuItem.Size = new System.Drawing.Size(237, 22);
			this.SaveSessionAsMenuItem.Text = "Save Session &As...";
			this.SaveSessionAsMenuItem.Click += new System.EventHandler(this.SaveSessionAsMenuItem_Click);
			// 
			// toolStripSeparator9
			// 
			this.toolStripSeparator9.Name = "toolStripSeparator9";
			this.toolStripSeparator9.Size = new System.Drawing.Size(234, 6);
			// 
			// RecentSessionsSubMenu
			// 
			this.RecentSessionsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator8});
			this.RecentSessionsSubMenu.Name = "RecentSessionsSubMenu";
			this.RecentSessionsSubMenu.Size = new System.Drawing.Size(237, 22);
			this.RecentSessionsSubMenu.Text = "Recent Sessions";
			this.RecentSessionsSubMenu.DropDownOpened += new System.EventHandler(this.RecentSessionsSubMenu_DropDownOpened);
			// 
			// toolStripSeparator8
			// 
			this.toolStripSeparator8.Name = "toolStripSeparator8";
			this.toolStripSeparator8.Size = new System.Drawing.Size(57, 6);
			// 
			// RecentScriptsSubMenu
			// 
			this.RecentScriptsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator3});
			this.RecentScriptsSubMenu.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Recent;
			this.RecentScriptsSubMenu.Name = "RecentScriptsSubMenu";
			this.RecentScriptsSubMenu.Size = new System.Drawing.Size(237, 22);
			this.RecentScriptsSubMenu.Text = "Recent Scripts";
			this.RecentScriptsSubMenu.DropDownOpened += new System.EventHandler(this.RecentScriptsSubMenu_DropDownOpened);
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(57, 6);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(234, 6);
			// 
			// ExitMenuItem
			// 
			this.ExitMenuItem.Name = "ExitMenuItem";
			this.ExitMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
			this.ExitMenuItem.Size = new System.Drawing.Size(237, 22);
			this.ExitMenuItem.Text = "E&xit";
			this.ExitMenuItem.Click += new System.EventHandler(this.ExitMenuItem_Click);
			// 
			// ScriptSubMenu
			// 
			this.ScriptSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.NewScriptMenuItem,
            this.OpenScriptMenuItem,
            this.ToggleScriptMenuItem,
            this.PauseScriptMenuItem,
            this.EditScriptMenuItem,
            this.RemoveScriptMenuItem,
            this.InsertSeparatorMenuItem,
            this.toolStripSeparator7,
            this.MoveUpMenuItem,
            this.MoveDownMenuItem,
            this.SelectAllMenuItem,
            this.toolStripSeparator6,
            this.StopAllScriptsMenuItem,
            this.RegisteredFunctionsMenuItem});
			this.ScriptSubMenu.Name = "ScriptSubMenu";
			this.ScriptSubMenu.Size = new System.Drawing.Size(49, 20);
			this.ScriptSubMenu.Text = "&Script";
			this.ScriptSubMenu.DropDownOpened += new System.EventHandler(this.ScriptSubMenu_DropDownOpened);
			// 
			// NewScriptMenuItem
			// 
			this.NewScriptMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.NewFile;
			this.NewScriptMenuItem.Name = "NewScriptMenuItem";
			this.NewScriptMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
			this.NewScriptMenuItem.Size = new System.Drawing.Size(218, 22);
			this.NewScriptMenuItem.Text = "New Script";
			this.NewScriptMenuItem.Click += new System.EventHandler(this.NewScriptMenuItem_Click);
			// 
			// OpenScriptMenuItem
			// 
			this.OpenScriptMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.OpenFile;
			this.OpenScriptMenuItem.Name = "OpenScriptMenuItem";
			this.OpenScriptMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.OpenScriptMenuItem.Size = new System.Drawing.Size(218, 22);
			this.OpenScriptMenuItem.Text = "&Open Script...";
			this.OpenScriptMenuItem.Click += new System.EventHandler(this.OpenScriptMenuItem_Click);
			// 
			// ToggleScriptMenuItem
			// 
			this.ToggleScriptMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Refresh1;
			this.ToggleScriptMenuItem.Name = "ToggleScriptMenuItem";
			this.ToggleScriptMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.T)));
			this.ToggleScriptMenuItem.Size = new System.Drawing.Size(218, 22);
			this.ToggleScriptMenuItem.Text = "&Toggle";
			this.ToggleScriptMenuItem.Click += new System.EventHandler(this.ToggleScriptMenuItem_Click);
			// 
			// PauseScriptMenuItem
			// 
			this.PauseScriptMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Pause;
			this.PauseScriptMenuItem.Name = "PauseScriptMenuItem";
			this.PauseScriptMenuItem.Size = new System.Drawing.Size(218, 22);
			this.PauseScriptMenuItem.Text = "Pause or Resume";
			this.PauseScriptMenuItem.Click += new System.EventHandler(this.PauseScriptMenuItem_Click);
			// 
			// EditScriptMenuItem
			// 
			this.EditScriptMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.CutHS;
			this.EditScriptMenuItem.Name = "EditScriptMenuItem";
			this.EditScriptMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E)));
			this.EditScriptMenuItem.Size = new System.Drawing.Size(218, 22);
			this.EditScriptMenuItem.Text = "&Edit Script";
			this.EditScriptMenuItem.Click += new System.EventHandler(this.EditScriptMenuItem_Click);
			// 
			// RemoveScriptMenuItem
			// 
			this.RemoveScriptMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Delete;
			this.RemoveScriptMenuItem.Name = "RemoveScriptMenuItem";
			this.RemoveScriptMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R)));
			this.RemoveScriptMenuItem.Size = new System.Drawing.Size(218, 22);
			this.RemoveScriptMenuItem.Text = "&Remove Script";
			this.RemoveScriptMenuItem.Click += new System.EventHandler(this.RemoveScriptMenuItem_Click);
			// 
			// InsertSeparatorMenuItem
			// 
			this.InsertSeparatorMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.InsertSeparator;
			this.InsertSeparatorMenuItem.Name = "InsertSeparatorMenuItem";
			this.InsertSeparatorMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.I)));
			this.InsertSeparatorMenuItem.Size = new System.Drawing.Size(218, 22);
			this.InsertSeparatorMenuItem.Text = "Insert Separator";
			this.InsertSeparatorMenuItem.Click += new System.EventHandler(this.InsertSeparatorMenuItem_Click);
			// 
			// toolStripSeparator7
			// 
			this.toolStripSeparator7.Name = "toolStripSeparator7";
			this.toolStripSeparator7.Size = new System.Drawing.Size(215, 6);
			// 
			// MoveUpMenuItem
			// 
			this.MoveUpMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.MoveUp;
			this.MoveUpMenuItem.Name = "MoveUpMenuItem";
			this.MoveUpMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.U)));
			this.MoveUpMenuItem.Size = new System.Drawing.Size(218, 22);
			this.MoveUpMenuItem.Text = "Move &Up";
			this.MoveUpMenuItem.Click += new System.EventHandler(this.MoveUpMenuItem_Click);
			// 
			// MoveDownMenuItem
			// 
			this.MoveDownMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.MoveDown;
			this.MoveDownMenuItem.Name = "MoveDownMenuItem";
			this.MoveDownMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
			this.MoveDownMenuItem.Size = new System.Drawing.Size(218, 22);
			this.MoveDownMenuItem.Text = "Move &Down";
			this.MoveDownMenuItem.Click += new System.EventHandler(this.MoveDownMenuItem_Click);
			// 
			// SelectAllMenuItem
			// 
			this.SelectAllMenuItem.Name = "SelectAllMenuItem";
			this.SelectAllMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
			this.SelectAllMenuItem.Size = new System.Drawing.Size(218, 22);
			this.SelectAllMenuItem.Text = "Select &All";
			this.SelectAllMenuItem.Click += new System.EventHandler(this.SelectAllMenuItem_Click);
			// 
			// toolStripSeparator6
			// 
			this.toolStripSeparator6.Name = "toolStripSeparator6";
			this.toolStripSeparator6.Size = new System.Drawing.Size(215, 6);
			// 
			// StopAllScriptsMenuItem
			// 
			this.StopAllScriptsMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Stop;
			this.StopAllScriptsMenuItem.Name = "StopAllScriptsMenuItem";
			this.StopAllScriptsMenuItem.Size = new System.Drawing.Size(218, 22);
			this.StopAllScriptsMenuItem.Text = "Stop All Scripts";
			this.StopAllScriptsMenuItem.Click += new System.EventHandler(this.StopAllScriptsMenuItem_Click);
			// 
			// RegisteredFunctionsMenuItem
			// 
			this.RegisteredFunctionsMenuItem.Name = "RegisteredFunctionsMenuItem";
			this.RegisteredFunctionsMenuItem.ShortcutKeyDisplayString = "F12";
			this.RegisteredFunctionsMenuItem.Size = new System.Drawing.Size(218, 22);
			this.RegisteredFunctionsMenuItem.Text = "&Registered Functions...";
			this.RegisteredFunctionsMenuItem.Click += new System.EventHandler(this.RegisteredFunctionsMenuItem_Click);
			// 
			// OptionsSubMenu
			// 
			this.OptionsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SaveWindowPositionMenuItem,
            this.AutoloadConsoleMenuItem,
            this.AutoloadSessionMenuItem,
            this.DisableScriptsOnLoadMenuItem,
            this.toolStripSeparator5,
            this.RestoreDefaultSettingsMenuItem});
			this.OptionsSubMenu.Name = "OptionsSubMenu";
			this.OptionsSubMenu.Size = new System.Drawing.Size(61, 20);
			this.OptionsSubMenu.Text = "&Options";
			this.OptionsSubMenu.DropDownOpened += new System.EventHandler(this.OptionsSubMenu_DropDownOpened);
			// 
			// SaveWindowPositionMenuItem
			// 
			this.SaveWindowPositionMenuItem.Name = "SaveWindowPositionMenuItem";
			this.SaveWindowPositionMenuItem.Size = new System.Drawing.Size(199, 22);
			this.SaveWindowPositionMenuItem.Text = "Save Window Position";
			this.SaveWindowPositionMenuItem.Click += new System.EventHandler(this.SaveWindowPositionMenuItem_Click);
			// 
			// AutoloadConsoleMenuItem
			// 
			this.AutoloadConsoleMenuItem.Name = "AutoloadConsoleMenuItem";
			this.AutoloadConsoleMenuItem.Size = new System.Drawing.Size(199, 22);
			this.AutoloadConsoleMenuItem.Text = "Autoload Console";
			this.AutoloadConsoleMenuItem.Click += new System.EventHandler(this.AutoloadConsoleMenuItem_Click);
			// 
			// AutoloadSessionMenuItem
			// 
			this.AutoloadSessionMenuItem.Name = "AutoloadSessionMenuItem";
			this.AutoloadSessionMenuItem.Size = new System.Drawing.Size(199, 22);
			this.AutoloadSessionMenuItem.Text = "Autoload Session";
			this.AutoloadSessionMenuItem.Click += new System.EventHandler(this.AutoloadSessionMenuItem_Click);
			// 
			// DisableScriptsOnLoadMenuItem
			// 
			this.DisableScriptsOnLoadMenuItem.Name = "DisableScriptsOnLoadMenuItem";
			this.DisableScriptsOnLoadMenuItem.Size = new System.Drawing.Size(199, 22);
			this.DisableScriptsOnLoadMenuItem.Text = "Disable Scripts on Load";
			this.DisableScriptsOnLoadMenuItem.Click += new System.EventHandler(this.DisableScriptsOnLoadMenuItem_Click);
			// 
			// toolStripSeparator5
			// 
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			this.toolStripSeparator5.Size = new System.Drawing.Size(196, 6);
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
            this.FunctionsListMenuItem,
            this.OnlineDocsMenuItem});
			this.HelpSubMenu.Name = "HelpSubMenu";
			this.HelpSubMenu.Size = new System.Drawing.Size(44, 20);
			this.HelpSubMenu.Text = "&Help";
			// 
			// FunctionsListMenuItem
			// 
			this.FunctionsListMenuItem.Name = "FunctionsListMenuItem";
			this.FunctionsListMenuItem.Size = new System.Drawing.Size(202, 22);
			this.FunctionsListMenuItem.Text = "&Lua Functions List";
			this.FunctionsListMenuItem.Click += new System.EventHandler(this.FunctionsListMenuItem_Click);
			// 
			// OnlineDocsMenuItem
			// 
			this.OnlineDocsMenuItem.Name = "OnlineDocsMenuItem";
			this.OnlineDocsMenuItem.Size = new System.Drawing.Size(202, 22);
			this.OnlineDocsMenuItem.Text = "Documentation online...";
			this.OnlineDocsMenuItem.Click += new System.EventHandler(this.OnlineDocsMenuItem_Click);
			// 
			// OutputBox
			// 
			this.OutputBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.OutputBox.ContextMenuStrip = this.ConsoleContextMenu;
			this.OutputBox.Location = new System.Drawing.Point(6, 17);
			this.OutputBox.Name = "OutputBox";
			this.OutputBox.ReadOnly = true;
			this.OutputBox.Size = new System.Drawing.Size(246, 283);
			this.OutputBox.TabIndex = 2;
			this.OutputBox.Text = "";
			this.OutputBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OutputBox_KeyDown);
			// 
			// ConsoleContextMenu
			// 
			this.ConsoleContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ClearConsoleContextItem,
            this.RegisteredFunctionsContextItem});
			this.ConsoleContextMenu.Name = "contextMenuStrip2";
			this.ConsoleContextMenu.Size = new System.Drawing.Size(185, 48);
			this.ConsoleContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.ConsoleContextMenu_Opening);
			// 
			// ClearConsoleContextItem
			// 
			this.ClearConsoleContextItem.Name = "ClearConsoleContextItem";
			this.ClearConsoleContextItem.Size = new System.Drawing.Size(184, 22);
			this.ClearConsoleContextItem.Text = "&Clear";
			this.ClearConsoleContextItem.Click += new System.EventHandler(this.ClearConsoleContextItem_Click);
			// 
			// RegisteredFunctionsContextItem
			// 
			this.RegisteredFunctionsContextItem.Name = "RegisteredFunctionsContextItem";
			this.RegisteredFunctionsContextItem.Size = new System.Drawing.Size(184, 22);
			this.RegisteredFunctionsContextItem.Text = "&Registered Functions";
			this.RegisteredFunctionsContextItem.Click += new System.EventHandler(this.RegisteredFunctionsMenuItem_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add(this.OutputBox);
			this.groupBox1.Location = new System.Drawing.Point(310, 71);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(258, 304);
			this.groupBox1.TabIndex = 3;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Output";
			// 
			// NumberOfScripts
			// 
			this.NumberOfScripts.AutoSize = true;
			this.NumberOfScripts.Location = new System.Drawing.Point(12, 53);
			this.NumberOfScripts.Name = "NumberOfScripts";
			this.NumberOfScripts.Size = new System.Drawing.Size(56, 13);
			this.NumberOfScripts.TabIndex = 4;
			this.NumberOfScripts.Text = "0 script     ";
			// 
			// OutputMessages
			// 
			this.OutputMessages.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.OutputMessages.AutoSize = true;
			this.OutputMessages.Location = new System.Drawing.Point(13, 384);
			this.OutputMessages.Name = "OutputMessages";
			this.OutputMessages.Size = new System.Drawing.Size(106, 13);
			this.OutputMessages.TabIndex = 6;
			this.OutputMessages.Text = "                                 ";
			// 
			// toolStrip1
			// 
			this.toolStrip1.ClickThrough = true;
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.NewScriptToolbarItem,
            this.OpenScriptToolbarItem,
            this.ToggleScriptToolbarItem,
            this.PauseToolbarItem,
            this.EditToolbarItem,
            this.RemoveScriptToolbarItem,
            this.InsertSeparatorToolbarItem,
            this.toolStripSeparator2,
            this.MoveUpToolbarItem,
            this.toolStripButtonMoveDown});
			this.toolStrip1.Location = new System.Drawing.Point(0, 24);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.Size = new System.Drawing.Size(584, 25);
			this.toolStrip1.TabIndex = 5;
			this.toolStrip1.Text = "toolStrip1";
			// 
			// NewScriptToolbarItem
			// 
			this.NewScriptToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.NewScriptToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.NewFile;
			this.NewScriptToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.NewScriptToolbarItem.Name = "NewScriptToolbarItem";
			this.NewScriptToolbarItem.Size = new System.Drawing.Size(23, 22);
			this.NewScriptToolbarItem.Text = "New Lua Script";
			this.NewScriptToolbarItem.Click += new System.EventHandler(this.NewScriptMenuItem_Click);
			// 
			// OpenScriptToolbarItem
			// 
			this.OpenScriptToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.OpenScriptToolbarItem.Image = ((System.Drawing.Image)(resources.GetObject("OpenScriptToolbarItem.Image")));
			this.OpenScriptToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.OpenScriptToolbarItem.Name = "OpenScriptToolbarItem";
			this.OpenScriptToolbarItem.Size = new System.Drawing.Size(23, 22);
			this.OpenScriptToolbarItem.Text = "Open Script";
			this.OpenScriptToolbarItem.Click += new System.EventHandler(this.OpenScriptMenuItem_Click);
			// 
			// ToggleScriptToolbarItem
			// 
			this.ToggleScriptToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.ToggleScriptToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Refresh1;
			this.ToggleScriptToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.ToggleScriptToolbarItem.Name = "ToggleScriptToolbarItem";
			this.ToggleScriptToolbarItem.Size = new System.Drawing.Size(23, 22);
			this.ToggleScriptToolbarItem.Text = "Toggle Script";
			this.ToggleScriptToolbarItem.Click += new System.EventHandler(this.ToggleScriptMenuItem_Click);
			// 
			// PauseToolbarItem
			// 
			this.PauseToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.PauseToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Pause;
			this.PauseToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.PauseToolbarItem.Name = "PauseToolbarItem";
			this.PauseToolbarItem.Size = new System.Drawing.Size(23, 22);
			this.PauseToolbarItem.Text = "Pause or Resume";
			this.PauseToolbarItem.Click += new System.EventHandler(this.PauseScriptMenuItem_Click);
			// 
			// EditToolbarItem
			// 
			this.EditToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.EditToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.CutHS;
			this.EditToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.EditToolbarItem.Name = "EditToolbarItem";
			this.EditToolbarItem.Size = new System.Drawing.Size(23, 22);
			this.EditToolbarItem.Text = "Edit Script";
			this.EditToolbarItem.Click += new System.EventHandler(this.EditToolbarItem_Click);
			// 
			// RemoveScriptToolbarItem
			// 
			this.RemoveScriptToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.RemoveScriptToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Delete;
			this.RemoveScriptToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.RemoveScriptToolbarItem.Name = "RemoveScriptToolbarItem";
			this.RemoveScriptToolbarItem.Size = new System.Drawing.Size(23, 22);
			this.RemoveScriptToolbarItem.Text = "Remove Script";
			this.RemoveScriptToolbarItem.Click += new System.EventHandler(this.RemoveScriptMenuItem_Click);
			// 
			// InsertSeparatorToolbarItem
			// 
			this.InsertSeparatorToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.InsertSeparatorToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.InsertSeparator;
			this.InsertSeparatorToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.InsertSeparatorToolbarItem.Name = "InsertSeparatorToolbarItem";
			this.InsertSeparatorToolbarItem.Size = new System.Drawing.Size(23, 22);
			this.InsertSeparatorToolbarItem.Text = "Insert Separator";
			this.InsertSeparatorToolbarItem.Click += new System.EventHandler(this.InsertSeparatorMenuItem_Click);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
			// 
			// MoveUpToolbarItem
			// 
			this.MoveUpToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.MoveUpToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.MoveUp;
			this.MoveUpToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.MoveUpToolbarItem.Name = "MoveUpToolbarItem";
			this.MoveUpToolbarItem.Size = new System.Drawing.Size(23, 22);
			this.MoveUpToolbarItem.Text = "Move Up";
			this.MoveUpToolbarItem.Click += new System.EventHandler(this.MoveUpMenuItem_Click);
			// 
			// toolStripButtonMoveDown
			// 
			this.toolStripButtonMoveDown.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButtonMoveDown.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.MoveDown;
			this.toolStripButtonMoveDown.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButtonMoveDown.Name = "toolStripButtonMoveDown";
			this.toolStripButtonMoveDown.Size = new System.Drawing.Size(23, 22);
			this.toolStripButtonMoveDown.Text = "Move Down";
			this.toolStripButtonMoveDown.Click += new System.EventHandler(this.MoveDownMenuItem_Click);
			// 
			// LuaListView
			// 
			this.LuaListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.LuaListView.CheckBoxes = true;
			this.LuaListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Script,
            this.PathName});
			this.LuaListView.ContextMenuStrip = this.ScriptListContextMenu;
			this.LuaListView.FullRowSelect = true;
			this.LuaListView.GridLines = true;
			this.LuaListView.HideSelection = false;
			this.LuaListView.ItemCount = 0;
			this.LuaListView.Location = new System.Drawing.Point(13, 71);
			this.LuaListView.Name = "LuaListView";
			this.LuaListView.selectedItem = -1;
			this.LuaListView.Size = new System.Drawing.Size(291, 304);
			this.LuaListView.TabIndex = 0;
			this.LuaListView.UseCompatibleStateImageBehavior = false;
			this.LuaListView.View = System.Windows.Forms.View.Details;
			this.LuaListView.ItemActivate += new System.EventHandler(this.LuaListView_ItemActivate);
			this.LuaListView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.LuaListView_KeyDown);
			// 
			// Script
			// 
			this.Script.Text = "Script";
			this.Script.Width = 92;
			// 
			// PathName
			// 
			this.PathName.Text = "Path";
			this.PathName.Width = 195;
			// 
			// LuaConsole
			// 
			this.AllowDrop = true;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(584, 402);
			this.Controls.Add(this.OutputMessages);
			this.Controls.Add(this.toolStrip1);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.NumberOfScripts);
			this.Controls.Add(this.menuStrip1);
			this.Controls.Add(this.LuaListView);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.menuStrip1;
			this.MinimumSize = new System.Drawing.Size(400, 180);
			this.Name = "LuaConsole";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Lua Console";
			this.Load += new System.EventHandler(this.LuaConsole_Load);
			this.DragDrop += new System.Windows.Forms.DragEventHandler(this.LuaConsole_DragDrop);
			this.DragEnter += new System.Windows.Forms.DragEventHandler(this.LuaConsole_DragEnter);
			this.ScriptListContextMenu.ResumeLayout(false);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.ConsoleContextMenu.ResumeLayout(false);
			this.groupBox1.ResumeLayout(false);
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private VirtualListView LuaListView;
		private System.Windows.Forms.ColumnHeader PathName;
		private MenuStripEx menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem FileSubMenu;
		private System.Windows.Forms.ToolStripMenuItem SaveSessionMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SaveSessionAsMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem ExitMenuItem;
		private System.Windows.Forms.ToolStripMenuItem ScriptSubMenu;
		private System.Windows.Forms.ToolStripMenuItem EditScriptMenuItem;
		private System.Windows.Forms.ToolStripMenuItem ToggleScriptMenuItem;
		public System.Windows.Forms.ColumnHeader Script;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.ToolStripMenuItem NewSessionMenuItem;
		private System.Windows.Forms.ToolStripMenuItem OptionsSubMenu;
		private System.Windows.Forms.ToolStripMenuItem SaveWindowPositionMenuItem;
		private System.Windows.Forms.ToolStripMenuItem RestoreDefaultSettingsMenuItem;
		private System.Windows.Forms.Label NumberOfScripts;
		private System.Windows.Forms.ToolStripMenuItem InsertSeparatorMenuItem;
		private System.Windows.Forms.ToolStripMenuItem StopAllScriptsMenuItem;
		private System.Windows.Forms.ContextMenuStrip ScriptListContextMenu;
		private System.Windows.Forms.ToolStripMenuItem RecentScriptsSubMenu;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripMenuItem StopAllScriptsContextItem;
		private System.Windows.Forms.ToolStripMenuItem AutoloadConsoleMenuItem;
		private System.Windows.Forms.ToolStripMenuItem RemoveScriptContextItem;
		private System.Windows.Forms.ToolStripMenuItem InsertSeperatorContextItem;
		private System.Windows.Forms.ToolStripSeparator ScriptContextSeparator;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
		private System.Windows.Forms.ToolStripMenuItem EditScriptContextItem;
		private System.Windows.Forms.ToolStripMenuItem ToggleScriptContextItem;
		private System.Windows.Forms.ToolStripMenuItem RemoveScriptMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
		private System.Windows.Forms.ToolStripMenuItem MoveUpMenuItem;
		private System.Windows.Forms.ToolStripMenuItem MoveDownMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SelectAllMenuItem;
		private ToolStripEx toolStrip1;
		private System.Windows.Forms.ToolStripButton OpenScriptToolbarItem;
		private System.Windows.Forms.ToolStripButton RemoveScriptToolbarItem;
		private System.Windows.Forms.ToolStripButton ToggleScriptToolbarItem;
		private System.Windows.Forms.ToolStripButton InsertSeparatorToolbarItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripButton MoveUpToolbarItem;
		private System.Windows.Forms.ToolStripButton toolStripButtonMoveDown;
		private System.Windows.Forms.ToolStripButton EditToolbarItem;
		private System.Windows.Forms.ToolStripMenuItem OpenScriptMenuItem;
		private System.Windows.Forms.ToolStripMenuItem OpenSessionMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator9;
		private System.Windows.Forms.ToolStripMenuItem RecentSessionsSubMenu;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
		private System.Windows.Forms.ToolStripMenuItem HelpSubMenu;
		private System.Windows.Forms.ToolStripMenuItem FunctionsListMenuItem;
		private System.Windows.Forms.ContextMenuStrip ConsoleContextMenu;
		private System.Windows.Forms.ToolStripMenuItem ClearConsoleContextItem;
		private System.Windows.Forms.ToolStripMenuItem DisableScriptsOnLoadMenuItem;
		private System.Windows.Forms.ToolStripMenuItem AutoloadSessionMenuItem;
		private System.Windows.Forms.ToolStripMenuItem PauseScriptMenuItem;
		private System.Windows.Forms.ToolStripButton PauseToolbarItem;
		private System.Windows.Forms.ToolStripMenuItem PauseScriptContextItem;
		public System.Windows.Forms.RichTextBox OutputBox;
		private System.Windows.Forms.Label OutputMessages;
		private System.Windows.Forms.ToolStripMenuItem OnlineDocsMenuItem;
		private System.Windows.Forms.ToolStripMenuItem NewScriptMenuItem;
		private System.Windows.Forms.ToolStripButton NewScriptToolbarItem;
		private System.Windows.Forms.ToolStripMenuItem RegisteredFunctionsMenuItem;
		private System.Windows.Forms.ToolStripMenuItem RegisteredFunctionsContextItem;
	}
}