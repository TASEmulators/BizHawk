using BizHawk.WinForms.Controls;

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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.NumberOfScripts = new BizHawk.WinForms.Controls.LocLabelEx();
            this.LuaListView = new BizHawk.Client.EmuHawk.InputRoll();
            this.ScriptListContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ToggleScriptContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.PauseScriptContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.EditScriptContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.RemoveScriptContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.InsertSeperatorContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.ScriptContextSeparator = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.StopAllScriptsContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.ClearRegisteredFunctionsContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.InputBox = new System.Windows.Forms.TextBox();
            this.OutputBox = new System.Windows.Forms.RichTextBox();
            this.ConsoleContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.CopyContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SelectAllContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.ClearConsoleContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator5 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.RegisteredFunctionsContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.ClearRegisteredFunctionsLogContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.menuStrip1 = new BizHawk.WinForms.Controls.MenuStripEx();
            this.FileSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.NewSessionMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.OpenSessionMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SaveSessionMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SaveSessionAsMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator9 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.RecentSessionsSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator8 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.RecentScriptsSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator3 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.ScriptSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.NewScriptMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.OpenScriptMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.RefreshScriptMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.ToggleScriptMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.PauseScriptMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.EditScriptMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.RemoveScriptMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.DuplicateScriptMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.ClearConsoleMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator7 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.InsertSeparatorMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.MoveUpMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.MoveDownMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SelectAllMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator6 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.StopAllScriptsMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.RegisteredFunctionsMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SettingsSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.DisableScriptsOnLoadMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.ReturnAllIfNoneSelectedMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.ReloadWhenScriptFileChangesMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator4 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.RegisterToTextEditorsSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.RegisterSublimeText2MenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.RegisterNotePadMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.HelpSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.FunctionsListMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.OnlineDocsMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.OutputMessages = new BizHawk.WinForms.Controls.LocLabelEx();
            this.toolStrip1 = new BizHawk.WinForms.Controls.ToolStripEx();
            this.NewScriptToolbarItem = new System.Windows.Forms.ToolStripButton();
            this.OpenScriptToolbarItem = new System.Windows.Forms.ToolStripButton();
            this.ToggleScriptToolbarItem = new System.Windows.Forms.ToolStripButton();
            this.RefreshScriptToolbarItem = new System.Windows.Forms.ToolStripButton();
            this.PauseToolbarItem = new System.Windows.Forms.ToolStripButton();
            this.EditToolbarItem = new System.Windows.Forms.ToolStripButton();
            this.RemoveScriptToolbarItem = new System.Windows.Forms.ToolStripButton();
            this.DuplicateToolbarButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.MoveUpToolbarItem = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonMoveDown = new System.Windows.Forms.ToolStripButton();
            this.InsertSeparatorToolbarItem = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator10 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.ClearConsoleToolbarButton = new System.Windows.Forms.ToolStripButton();
            this.EraseToolbarItem = new System.Windows.Forms.ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.ScriptListContextMenu.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.ConsoleContextMenu.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            resources.ApplyResources(this.splitContainer1, "splitContainer1");
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            resources.ApplyResources(this.splitContainer1.Panel1, "splitContainer1.Panel1");
            this.splitContainer1.Panel1.Controls.Add(this.NumberOfScripts);
            this.splitContainer1.Panel1.Controls.Add(this.LuaListView);
            // 
            // splitContainer1.Panel2
            // 
            resources.ApplyResources(this.splitContainer1.Panel2, "splitContainer1.Panel2");
            this.splitContainer1.Panel2.Controls.Add(this.groupBox1);
            this.splitContainer1.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.BranchesMarkersSplit_SplitterMoved);
            // 
            // NumberOfScripts
            // 
            resources.ApplyResources(this.NumberOfScripts, "NumberOfScripts");
            this.NumberOfScripts.Name = "NumberOfScripts";
            // 
            // LuaListView
            // 
            resources.ApplyResources(this.LuaListView, "LuaListView");
            this.LuaListView.AllowColumnReorder = false;
            this.LuaListView.AllowColumnResize = true;
            this.LuaListView.AlwaysScroll = false;
            this.LuaListView.CellHeightPadding = 0;
            this.LuaListView.CellWidthPadding = 0;
            this.LuaListView.ContextMenuStrip = this.ScriptListContextMenu;
            this.LuaListView.FullRowSelect = true;
            this.LuaListView.HorizontalOrientation = false;
            this.LuaListView.LetKeysModifySelection = false;
            this.LuaListView.Name = "LuaListView";
            this.LuaListView.RowCount = 0;
            this.LuaListView.ScrollSpeed = 1;
            this.LuaListView.ColumnClick += new BizHawk.Client.EmuHawk.InputRoll.ColumnClickEventHandler(this.LuaListView_ColumnClick);
            this.LuaListView.DoubleClick += new System.EventHandler(this.LuaListView_DoubleClick);
            this.LuaListView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.LuaListView_KeyDown);
            // 
            // ScriptListContextMenu
            // 
            resources.ApplyResources(this.ScriptListContextMenu, "ScriptListContextMenu");
            this.ScriptListContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToggleScriptContextItem,
            this.PauseScriptContextItem,
            this.EditScriptContextItem,
            this.RemoveScriptContextItem,
            this.InsertSeperatorContextItem,
            this.ScriptContextSeparator,
            this.StopAllScriptsContextItem,
            this.ClearRegisteredFunctionsContextItem});
            this.ScriptListContextMenu.Name = "contextMenuStrip1";
            this.ScriptListContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.ScriptListContextMenu_Opening);
            // 
            // ToggleScriptContextItem
            // 
            resources.ApplyResources(this.ToggleScriptContextItem, "ToggleScriptContextItem");
            this.ToggleScriptContextItem.Click += new System.EventHandler(this.ToggleScriptMenuItem_Click);
            // 
            // PauseScriptContextItem
            // 
            resources.ApplyResources(this.PauseScriptContextItem, "PauseScriptContextItem");
            this.PauseScriptContextItem.Click += new System.EventHandler(this.PauseScriptMenuItem_Click);
            // 
            // EditScriptContextItem
            // 
            resources.ApplyResources(this.EditScriptContextItem, "EditScriptContextItem");
            this.EditScriptContextItem.Click += new System.EventHandler(this.EditScriptMenuItem_Click);
            // 
            // RemoveScriptContextItem
            // 
            resources.ApplyResources(this.RemoveScriptContextItem, "RemoveScriptContextItem");
            this.RemoveScriptContextItem.Click += new System.EventHandler(this.RemoveScriptMenuItem_Click);
            // 
            // InsertSeperatorContextItem
            // 
            resources.ApplyResources(this.InsertSeperatorContextItem, "InsertSeperatorContextItem");
            this.InsertSeperatorContextItem.Click += new System.EventHandler(this.InsertSeparatorMenuItem_Click);
            // 
            // ScriptContextSeparator
            // 
            resources.ApplyResources(this.ScriptContextSeparator, "ScriptContextSeparator");
            // 
            // StopAllScriptsContextItem
            // 
            resources.ApplyResources(this.StopAllScriptsContextItem, "StopAllScriptsContextItem");
            this.StopAllScriptsContextItem.Click += new System.EventHandler(this.StopAllScriptsMenuItem_Click);
            // 
            // ClearRegisteredFunctionsContextItem
            // 
            resources.ApplyResources(this.ClearRegisteredFunctionsContextItem, "ClearRegisteredFunctionsContextItem");
            this.ClearRegisteredFunctionsContextItem.Click += new System.EventHandler(this.ClearRegisteredFunctionsContextMenuItem_Click);
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.InputBox);
            this.groupBox1.Controls.Add(this.OutputBox);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // InputBox
            // 
            resources.ApplyResources(this.InputBox, "InputBox");
            this.InputBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.InputBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.InputBox.Name = "InputBox";
            this.InputBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.InputBox_KeyDown);
            // 
            // OutputBox
            // 
            resources.ApplyResources(this.OutputBox, "OutputBox");
            this.OutputBox.ContextMenuStrip = this.ConsoleContextMenu;
            this.OutputBox.HideSelection = false;
            this.OutputBox.Name = "OutputBox";
            this.OutputBox.ReadOnly = true;
            this.OutputBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OutputBox_KeyDown);
            // 
            // ConsoleContextMenu
            // 
            resources.ApplyResources(this.ConsoleContextMenu, "ConsoleContextMenu");
            this.ConsoleContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CopyContextItem,
            this.SelectAllContextItem,
            this.ClearConsoleContextItem,
            this.toolStripSeparator5,
            this.RegisteredFunctionsContextItem,
            this.ClearRegisteredFunctionsLogContextItem});
            this.ConsoleContextMenu.Name = "contextMenuStrip2";
            this.ConsoleContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.ConsoleContextMenu_Opening);
            // 
            // CopyContextItem
            // 
            resources.ApplyResources(this.CopyContextItem, "CopyContextItem");
            this.CopyContextItem.Click += new System.EventHandler(this.CopyContextItem_Click);
            // 
            // SelectAllContextItem
            // 
            resources.ApplyResources(this.SelectAllContextItem, "SelectAllContextItem");
            this.SelectAllContextItem.Click += new System.EventHandler(this.SelectAllContextItem_Click);
            // 
            // ClearConsoleContextItem
            // 
            resources.ApplyResources(this.ClearConsoleContextItem, "ClearConsoleContextItem");
            this.ClearConsoleContextItem.Click += new System.EventHandler(this.ClearConsoleContextItem_Click);
            // 
            // toolStripSeparator5
            // 
            resources.ApplyResources(this.toolStripSeparator5, "toolStripSeparator5");
            // 
            // RegisteredFunctionsContextItem
            // 
            resources.ApplyResources(this.RegisteredFunctionsContextItem, "RegisteredFunctionsContextItem");
            this.RegisteredFunctionsContextItem.Click += new System.EventHandler(this.RegisteredFunctionsMenuItem_Click);
            // 
            // ClearRegisteredFunctionsLogContextItem
            // 
            resources.ApplyResources(this.ClearRegisteredFunctionsLogContextItem, "ClearRegisteredFunctionsLogContextItem");
            this.ClearRegisteredFunctionsLogContextItem.Click += new System.EventHandler(this.ClearRegisteredFunctionsContextMenuItem_Click);
            // 
            // menuStrip1
            // 
            resources.ApplyResources(this.menuStrip1, "menuStrip1");
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu,
            this.ScriptSubMenu,
            this.SettingsSubMenu,
            this.HelpSubMenu});
            // 
            // FileSubMenu
            // 
            resources.ApplyResources(this.FileSubMenu, "FileSubMenu");
            this.FileSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.NewSessionMenuItem,
            this.OpenSessionMenuItem,
            this.SaveSessionMenuItem,
            this.SaveSessionAsMenuItem,
            this.toolStripSeparator9,
            this.RecentSessionsSubMenu,
            this.RecentScriptsSubMenu});
            this.FileSubMenu.DropDownOpened += new System.EventHandler(this.FileSubMenu_DropDownOpened);
            // 
            // NewSessionMenuItem
            // 
            resources.ApplyResources(this.NewSessionMenuItem, "NewSessionMenuItem");
            this.NewSessionMenuItem.Click += new System.EventHandler(this.NewSessionMenuItem_Click);
            // 
            // OpenSessionMenuItem
            // 
            resources.ApplyResources(this.OpenSessionMenuItem, "OpenSessionMenuItem");
            this.OpenSessionMenuItem.Click += new System.EventHandler(this.OpenSessionMenuItem_Click);
            // 
            // SaveSessionMenuItem
            // 
            resources.ApplyResources(this.SaveSessionMenuItem, "SaveSessionMenuItem");
            this.SaveSessionMenuItem.Click += new System.EventHandler(this.SaveSessionMenuItem_Click);
            // 
            // SaveSessionAsMenuItem
            // 
            resources.ApplyResources(this.SaveSessionAsMenuItem, "SaveSessionAsMenuItem");
            this.SaveSessionAsMenuItem.Click += new System.EventHandler(this.SaveSessionAsMenuItem_Click);
            // 
            // toolStripSeparator9
            // 
            resources.ApplyResources(this.toolStripSeparator9, "toolStripSeparator9");
            // 
            // RecentSessionsSubMenu
            // 
            resources.ApplyResources(this.RecentSessionsSubMenu, "RecentSessionsSubMenu");
            this.RecentSessionsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator8});
            this.RecentSessionsSubMenu.DropDownOpened += new System.EventHandler(this.RecentSessionsSubMenu_DropDownOpened);
            // 
            // toolStripSeparator8
            // 
            resources.ApplyResources(this.toolStripSeparator8, "toolStripSeparator8");
            // 
            // RecentScriptsSubMenu
            // 
            resources.ApplyResources(this.RecentScriptsSubMenu, "RecentScriptsSubMenu");
            this.RecentScriptsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator3});
            this.RecentScriptsSubMenu.DropDownOpened += new System.EventHandler(this.RecentScriptsSubMenu_DropDownOpened);
            // 
            // toolStripSeparator3
            // 
            resources.ApplyResources(this.toolStripSeparator3, "toolStripSeparator3");
            // 
            // ScriptSubMenu
            // 
            resources.ApplyResources(this.ScriptSubMenu, "ScriptSubMenu");
            this.ScriptSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.NewScriptMenuItem,
            this.OpenScriptMenuItem,
            this.RefreshScriptMenuItem,
            this.ToggleScriptMenuItem,
            this.PauseScriptMenuItem,
            this.EditScriptMenuItem,
            this.RemoveScriptMenuItem,
            this.DuplicateScriptMenuItem,
            this.ClearConsoleMenuItem,
            this.toolStripSeparator7,
            this.InsertSeparatorMenuItem,
            this.MoveUpMenuItem,
            this.MoveDownMenuItem,
            this.SelectAllMenuItem,
            this.toolStripSeparator6,
            this.StopAllScriptsMenuItem,
            this.RegisteredFunctionsMenuItem});
            this.ScriptSubMenu.DropDownOpened += new System.EventHandler(this.ScriptSubMenu_DropDownOpened);
            // 
            // NewScriptMenuItem
            // 
            resources.ApplyResources(this.NewScriptMenuItem, "NewScriptMenuItem");
            this.NewScriptMenuItem.Click += new System.EventHandler(this.NewScriptMenuItem_Click);
            // 
            // OpenScriptMenuItem
            // 
            resources.ApplyResources(this.OpenScriptMenuItem, "OpenScriptMenuItem");
            this.OpenScriptMenuItem.Click += new System.EventHandler(this.OpenScriptMenuItem_Click);
            // 
            // RefreshScriptMenuItem
            // 
            resources.ApplyResources(this.RefreshScriptMenuItem, "RefreshScriptMenuItem");
            this.RefreshScriptMenuItem.Click += new System.EventHandler(this.RefreshScriptMenuItem_Click);
            // 
            // ToggleScriptMenuItem
            // 
            resources.ApplyResources(this.ToggleScriptMenuItem, "ToggleScriptMenuItem");
            this.ToggleScriptMenuItem.Click += new System.EventHandler(this.ToggleScriptMenuItem_Click);
            // 
            // PauseScriptMenuItem
            // 
            resources.ApplyResources(this.PauseScriptMenuItem, "PauseScriptMenuItem");
            this.PauseScriptMenuItem.Click += new System.EventHandler(this.PauseScriptMenuItem_Click);
            // 
            // EditScriptMenuItem
            // 
            resources.ApplyResources(this.EditScriptMenuItem, "EditScriptMenuItem");
            this.EditScriptMenuItem.Click += new System.EventHandler(this.EditScriptMenuItem_Click);
            // 
            // RemoveScriptMenuItem
            // 
            resources.ApplyResources(this.RemoveScriptMenuItem, "RemoveScriptMenuItem");
            this.RemoveScriptMenuItem.Click += new System.EventHandler(this.RemoveScriptMenuItem_Click);
            // 
            // DuplicateScriptMenuItem
            // 
            resources.ApplyResources(this.DuplicateScriptMenuItem, "DuplicateScriptMenuItem");
            this.DuplicateScriptMenuItem.Click += new System.EventHandler(this.DuplicateScriptMenuItem_Click);
            // 
            // ClearConsoleMenuItem
            // 
            resources.ApplyResources(this.ClearConsoleMenuItem, "ClearConsoleMenuItem");
            this.ClearConsoleMenuItem.Click += new System.EventHandler(this.ClearConsoleMenuItem_Click);
            // 
            // toolStripSeparator7
            // 
            resources.ApplyResources(this.toolStripSeparator7, "toolStripSeparator7");
            // 
            // InsertSeparatorMenuItem
            // 
            resources.ApplyResources(this.InsertSeparatorMenuItem, "InsertSeparatorMenuItem");
            this.InsertSeparatorMenuItem.Click += new System.EventHandler(this.InsertSeparatorMenuItem_Click);
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
            // SelectAllMenuItem
            // 
            resources.ApplyResources(this.SelectAllMenuItem, "SelectAllMenuItem");
            this.SelectAllMenuItem.Click += new System.EventHandler(this.SelectAllMenuItem_Click);
            // 
            // toolStripSeparator6
            // 
            resources.ApplyResources(this.toolStripSeparator6, "toolStripSeparator6");
            // 
            // StopAllScriptsMenuItem
            // 
            resources.ApplyResources(this.StopAllScriptsMenuItem, "StopAllScriptsMenuItem");
            this.StopAllScriptsMenuItem.Click += new System.EventHandler(this.StopAllScriptsMenuItem_Click);
            // 
            // RegisteredFunctionsMenuItem
            // 
            resources.ApplyResources(this.RegisteredFunctionsMenuItem, "RegisteredFunctionsMenuItem");
            this.RegisteredFunctionsMenuItem.Click += new System.EventHandler(this.RegisteredFunctionsMenuItem_Click);
            // 
            // SettingsSubMenu
            // 
            resources.ApplyResources(this.SettingsSubMenu, "SettingsSubMenu");
            this.SettingsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.DisableScriptsOnLoadMenuItem,
            this.ReturnAllIfNoneSelectedMenuItem,
            this.ReloadWhenScriptFileChangesMenuItem,
            this.toolStripSeparator4,
            this.RegisterToTextEditorsSubMenu});
            this.SettingsSubMenu.DropDownOpened += new System.EventHandler(this.OptionsSubMenu_DropDownOpened);
            // 
            // DisableScriptsOnLoadMenuItem
            // 
            resources.ApplyResources(this.DisableScriptsOnLoadMenuItem, "DisableScriptsOnLoadMenuItem");
            this.DisableScriptsOnLoadMenuItem.Click += new System.EventHandler(this.DisableScriptsOnLoadMenuItem_Click);
            // 
            // ReturnAllIfNoneSelectedMenuItem
            // 
            resources.ApplyResources(this.ReturnAllIfNoneSelectedMenuItem, "ReturnAllIfNoneSelectedMenuItem");
            this.ReturnAllIfNoneSelectedMenuItem.Click += new System.EventHandler(this.ToggleAllIfNoneSelectedMenuItem_Click);
            // 
            // ReloadWhenScriptFileChangesMenuItem
            // 
            resources.ApplyResources(this.ReloadWhenScriptFileChangesMenuItem, "ReloadWhenScriptFileChangesMenuItem");
            this.ReloadWhenScriptFileChangesMenuItem.Click += new System.EventHandler(this.ReloadWhenScriptFileChangesMenuItem_Click);
            // 
            // toolStripSeparator4
            // 
            resources.ApplyResources(this.toolStripSeparator4, "toolStripSeparator4");
            // 
            // RegisterToTextEditorsSubMenu
            // 
            resources.ApplyResources(this.RegisterToTextEditorsSubMenu, "RegisterToTextEditorsSubMenu");
            this.RegisterToTextEditorsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.RegisterSublimeText2MenuItem,
            this.RegisterNotePadMenuItem});
            this.RegisterToTextEditorsSubMenu.DropDownOpened += new System.EventHandler(this.RegisterToTextEditorsSubMenu_DropDownOpened);
            // 
            // RegisterSublimeText2MenuItem
            // 
            resources.ApplyResources(this.RegisterSublimeText2MenuItem, "RegisterSublimeText2MenuItem");
            this.RegisterSublimeText2MenuItem.Click += new System.EventHandler(this.RegisterSublimeText2MenuItem_Click);
            // 
            // RegisterNotePadMenuItem
            // 
            resources.ApplyResources(this.RegisterNotePadMenuItem, "RegisterNotePadMenuItem");
            this.RegisterNotePadMenuItem.Click += new System.EventHandler(this.RegisterNotePadMenuItem_Click);
            // 
            // HelpSubMenu
            // 
            resources.ApplyResources(this.HelpSubMenu, "HelpSubMenu");
            this.HelpSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FunctionsListMenuItem,
            this.OnlineDocsMenuItem});
            // 
            // FunctionsListMenuItem
            // 
            resources.ApplyResources(this.FunctionsListMenuItem, "FunctionsListMenuItem");
            this.FunctionsListMenuItem.Click += new System.EventHandler(this.FunctionsListMenuItem_Click);
            // 
            // OnlineDocsMenuItem
            // 
            resources.ApplyResources(this.OnlineDocsMenuItem, "OnlineDocsMenuItem");
            this.OnlineDocsMenuItem.Click += new System.EventHandler(this.OnlineDocsMenuItem_Click);
            // 
            // OutputMessages
            // 
            resources.ApplyResources(this.OutputMessages, "OutputMessages");
            this.OutputMessages.Name = "OutputMessages";
            // 
            // toolStrip1
            // 
            resources.ApplyResources(this.toolStrip1, "toolStrip1");
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.NewScriptToolbarItem,
            this.OpenScriptToolbarItem,
            this.ToggleScriptToolbarItem,
            this.RefreshScriptToolbarItem,
            this.PauseToolbarItem,
            this.EditToolbarItem,
            this.RemoveScriptToolbarItem,
            this.DuplicateToolbarButton,
            this.toolStripSeparator2,
            this.MoveUpToolbarItem,
            this.toolStripButtonMoveDown,
            this.InsertSeparatorToolbarItem,
            this.toolStripSeparator10,
            this.ClearConsoleToolbarButton,
            this.EraseToolbarItem});
            this.toolStrip1.Name = "toolStrip1";
            // 
            // NewScriptToolbarItem
            // 
            resources.ApplyResources(this.NewScriptToolbarItem, "NewScriptToolbarItem");
            this.NewScriptToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.NewScriptToolbarItem.Name = "NewScriptToolbarItem";
            this.NewScriptToolbarItem.Click += new System.EventHandler(this.NewScriptMenuItem_Click);
            // 
            // OpenScriptToolbarItem
            // 
            resources.ApplyResources(this.OpenScriptToolbarItem, "OpenScriptToolbarItem");
            this.OpenScriptToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.OpenScriptToolbarItem.Name = "OpenScriptToolbarItem";
            this.OpenScriptToolbarItem.Click += new System.EventHandler(this.OpenScriptMenuItem_Click);
            // 
            // ToggleScriptToolbarItem
            // 
            resources.ApplyResources(this.ToggleScriptToolbarItem, "ToggleScriptToolbarItem");
            this.ToggleScriptToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.ToggleScriptToolbarItem.Name = "ToggleScriptToolbarItem";
            this.ToggleScriptToolbarItem.Click += new System.EventHandler(this.ToggleScriptMenuItem_Click);
            // 
            // RefreshScriptToolbarItem
            // 
            resources.ApplyResources(this.RefreshScriptToolbarItem, "RefreshScriptToolbarItem");
            this.RefreshScriptToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.RefreshScriptToolbarItem.Name = "RefreshScriptToolbarItem";
            this.RefreshScriptToolbarItem.Click += new System.EventHandler(this.RefreshScriptMenuItem_Click);
            // 
            // PauseToolbarItem
            // 
            resources.ApplyResources(this.PauseToolbarItem, "PauseToolbarItem");
            this.PauseToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.PauseToolbarItem.Name = "PauseToolbarItem";
            this.PauseToolbarItem.Click += new System.EventHandler(this.PauseScriptMenuItem_Click);
            // 
            // EditToolbarItem
            // 
            resources.ApplyResources(this.EditToolbarItem, "EditToolbarItem");
            this.EditToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.EditToolbarItem.Name = "EditToolbarItem";
            this.EditToolbarItem.Click += new System.EventHandler(this.EditScriptMenuItem_Click);
            // 
            // RemoveScriptToolbarItem
            // 
            resources.ApplyResources(this.RemoveScriptToolbarItem, "RemoveScriptToolbarItem");
            this.RemoveScriptToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.RemoveScriptToolbarItem.Name = "RemoveScriptToolbarItem";
            this.RemoveScriptToolbarItem.Click += new System.EventHandler(this.RemoveScriptMenuItem_Click);
            // 
            // DuplicateToolbarButton
            // 
            resources.ApplyResources(this.DuplicateToolbarButton, "DuplicateToolbarButton");
            this.DuplicateToolbarButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.DuplicateToolbarButton.Name = "DuplicateToolbarButton";
            this.DuplicateToolbarButton.Click += new System.EventHandler(this.DuplicateScriptMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            resources.ApplyResources(this.toolStripSeparator2, "toolStripSeparator2");
            // 
            // MoveUpToolbarItem
            // 
            resources.ApplyResources(this.MoveUpToolbarItem, "MoveUpToolbarItem");
            this.MoveUpToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.MoveUpToolbarItem.Name = "MoveUpToolbarItem";
            this.MoveUpToolbarItem.Click += new System.EventHandler(this.MoveUpMenuItem_Click);
            // 
            // toolStripButtonMoveDown
            // 
            resources.ApplyResources(this.toolStripButtonMoveDown, "toolStripButtonMoveDown");
            this.toolStripButtonMoveDown.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonMoveDown.Name = "toolStripButtonMoveDown";
            this.toolStripButtonMoveDown.Click += new System.EventHandler(this.MoveDownMenuItem_Click);
            // 
            // InsertSeparatorToolbarItem
            // 
            resources.ApplyResources(this.InsertSeparatorToolbarItem, "InsertSeparatorToolbarItem");
            this.InsertSeparatorToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.InsertSeparatorToolbarItem.Name = "InsertSeparatorToolbarItem";
            this.InsertSeparatorToolbarItem.Click += new System.EventHandler(this.InsertSeparatorMenuItem_Click);
            // 
            // toolStripSeparator10
            // 
            resources.ApplyResources(this.toolStripSeparator10, "toolStripSeparator10");
            // 
            // ClearConsoleToolbarButton
            // 
            resources.ApplyResources(this.ClearConsoleToolbarButton, "ClearConsoleToolbarButton");
            this.ClearConsoleToolbarButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.ClearConsoleToolbarButton.Name = "ClearConsoleToolbarButton";
            this.ClearConsoleToolbarButton.Click += new System.EventHandler(this.ClearConsoleMenuItem_Click);
            // 
            // EraseToolbarItem
            // 
            resources.ApplyResources(this.EraseToolbarItem, "EraseToolbarItem");
            this.EraseToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.EraseToolbarItem.Name = "EraseToolbarItem";
            this.EraseToolbarItem.Click += new System.EventHandler(this.EraseToolbarItem_Click);
            // 
            // LuaConsole
            // 
            resources.ApplyResources(this, "$this");
            this.AllowDrop = true;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.OutputMessages);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "LuaConsole";
            this.Load += new System.EventHandler(this.LuaConsole_Load);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.LuaConsole_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.DragEnterWrapper);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ScriptListContextMenu.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ConsoleContextMenu.ResumeLayout(false);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private InputRoll LuaListView;
		private MenuStripEx menuStrip1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FileSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SaveSessionMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SaveSessionAsMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ScriptSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx EditScriptMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ToggleScriptMenuItem;
		private System.Windows.Forms.GroupBox groupBox1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx NewSessionMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SettingsSubMenu;
		private BizHawk.WinForms.Controls.LocLabelEx NumberOfScripts;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx InsertSeparatorMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx StopAllScriptsMenuItem;
		private System.Windows.Forms.ContextMenuStrip ScriptListContextMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RecentScriptsSubMenu;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator3;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx StopAllScriptsContextItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RemoveScriptContextItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx InsertSeperatorContextItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx ScriptContextSeparator;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx EditScriptContextItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ToggleScriptContextItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RemoveScriptMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator6;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator7;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx MoveUpMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx MoveDownMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SelectAllMenuItem;
		private ToolStripEx toolStrip1;
		private System.Windows.Forms.ToolStripButton OpenScriptToolbarItem;
		private System.Windows.Forms.ToolStripButton RemoveScriptToolbarItem;
		private System.Windows.Forms.ToolStripButton ToggleScriptToolbarItem;
		private System.Windows.Forms.ToolStripButton InsertSeparatorToolbarItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator2;
		private System.Windows.Forms.ToolStripButton MoveUpToolbarItem;
		private System.Windows.Forms.ToolStripButton toolStripButtonMoveDown;
		private System.Windows.Forms.ToolStripButton EditToolbarItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx OpenScriptMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx OpenSessionMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator9;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RecentSessionsSubMenu;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator8;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx HelpSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FunctionsListMenuItem;
		private System.Windows.Forms.ContextMenuStrip ConsoleContextMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ClearConsoleContextItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DisableScriptsOnLoadMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx PauseScriptMenuItem;
		private System.Windows.Forms.ToolStripButton PauseToolbarItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx PauseScriptContextItem;
		public System.Windows.Forms.RichTextBox OutputBox;
		private BizHawk.WinForms.Controls.LocLabelEx OutputMessages;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx OnlineDocsMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx NewScriptMenuItem;
		private System.Windows.Forms.ToolStripButton NewScriptToolbarItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RegisteredFunctionsMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RegisteredFunctionsContextItem;
		private System.Windows.Forms.ToolStripButton RefreshScriptToolbarItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RefreshScriptMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator10;
		private System.Windows.Forms.ToolStripButton EraseToolbarItem;
		private System.Windows.Forms.ToolStripButton DuplicateToolbarButton;
		private System.Windows.Forms.ToolStripButton ClearConsoleToolbarButton;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DuplicateScriptMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ClearConsoleMenuItem;
		private System.Windows.Forms.TextBox InputBox;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ReturnAllIfNoneSelectedMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ReloadWhenScriptFileChangesMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator4;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RegisterToTextEditorsSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RegisterSublimeText2MenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RegisterNotePadMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SelectAllContextItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CopyContextItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ClearRegisteredFunctionsContextItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator5;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ClearRegisteredFunctionsLogContextItem;
	}
}