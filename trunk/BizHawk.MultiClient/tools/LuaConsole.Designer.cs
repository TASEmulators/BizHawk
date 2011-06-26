namespace BizHawk.MultiClient
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
			this.LuaListView = new BizHawk.VirtualListView();
			this.Script = new System.Windows.Forms.ColumnHeader();
			this.PathName = new System.Windows.Forms.ColumnHeader();
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.recentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.noneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.clearToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.scriptToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toggleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.insertSeparatorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.turnOffAllScriptsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.removeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.insertSeperatorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.moveUpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.moveDownToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveWindowPositionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.autoloadConsoleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.restoreWindowSizeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.OutputBox = new System.Windows.Forms.RichTextBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.NumberOfScripts = new System.Windows.Forms.Label();
			this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.removeScriptToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.insertSeperatorToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.stopAllScriptsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.menuStrip1.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.contextMenuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// LuaListView
			// 
			this.LuaListView.CheckBoxes = true;
			this.LuaListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Script,
            this.PathName});
			this.LuaListView.FullRowSelect = true;
			this.LuaListView.GridLines = true;
			this.LuaListView.ItemCount = 0;
			this.LuaListView.Location = new System.Drawing.Point(12, 51);
			this.LuaListView.Name = "LuaListView";
			this.LuaListView.selectedItem = -1;
			this.LuaListView.Size = new System.Drawing.Size(293, 278);
			this.LuaListView.TabIndex = 0;
			this.LuaListView.UseCompatibleStateImageBehavior = false;
			this.LuaListView.View = System.Windows.Forms.View.Details;
			this.LuaListView.SelectedIndexChanged += new System.EventHandler(this.LuaListView_SelectedIndexChanged);
			this.LuaListView.DoubleClick += new System.EventHandler(this.LuaListView_DoubleClick);
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
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1,
            this.fileToolStripMenuItem,
            this.scriptToolStripMenuItem,
            this.viewToolStripMenuItem,
            this.optionsToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(598, 24);
			this.menuStrip1.TabIndex = 1;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(12, 20);
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripMenuItem,
            this.openToolStripMenuItem,
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.recentToolStripMenuItem,
            this.toolStripSeparator1,
            this.exitToolStripMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(35, 20);
			this.fileToolStripMenuItem.Text = "&File";
			// 
			// newToolStripMenuItem
			// 
			this.newToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.NewFile;
			this.newToolStripMenuItem.Name = "newToolStripMenuItem";
			this.newToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
			this.newToolStripMenuItem.Size = new System.Drawing.Size(204, 22);
			this.newToolStripMenuItem.Text = "&New";
			this.newToolStripMenuItem.Click += new System.EventHandler(this.newToolStripMenuItem_Click);
			// 
			// openToolStripMenuItem
			// 
			this.openToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.OpenFile;
			this.openToolStripMenuItem.Name = "openToolStripMenuItem";
			this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.openToolStripMenuItem.Size = new System.Drawing.Size(204, 22);
			this.openToolStripMenuItem.Text = "&Open";
			this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
			// 
			// saveToolStripMenuItem
			// 
			this.saveToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.SaveAs;
			this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
			this.saveToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			this.saveToolStripMenuItem.Size = new System.Drawing.Size(204, 22);
			this.saveToolStripMenuItem.Text = "&Save";
			this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
			// 
			// saveAsToolStripMenuItem
			// 
			this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
			this.saveAsToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
						| System.Windows.Forms.Keys.S)));
			this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(204, 22);
			this.saveAsToolStripMenuItem.Text = "&Save As...";
			this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.saveAsToolStripMenuItem_Click);
			// 
			// recentToolStripMenuItem
			// 
			this.recentToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.noneToolStripMenuItem,
            this.toolStripSeparator3,
            this.clearToolStripMenuItem});
			this.recentToolStripMenuItem.Name = "recentToolStripMenuItem";
			this.recentToolStripMenuItem.Size = new System.Drawing.Size(204, 22);
			this.recentToolStripMenuItem.Text = "Recent";
			// 
			// noneToolStripMenuItem
			// 
			this.noneToolStripMenuItem.Name = "noneToolStripMenuItem";
			this.noneToolStripMenuItem.Size = new System.Drawing.Size(110, 22);
			this.noneToolStripMenuItem.Text = "None";
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(107, 6);
			// 
			// clearToolStripMenuItem
			// 
			this.clearToolStripMenuItem.Name = "clearToolStripMenuItem";
			this.clearToolStripMenuItem.Size = new System.Drawing.Size(110, 22);
			this.clearToolStripMenuItem.Text = "Clear";
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(201, 6);
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(204, 22);
			this.exitToolStripMenuItem.Text = "E&xit";
			this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
			// 
			// scriptToolStripMenuItem
			// 
			this.scriptToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.editToolStripMenuItem,
            this.toggleToolStripMenuItem,
            this.insertSeparatorToolStripMenuItem,
            this.turnOffAllScriptsToolStripMenuItem});
			this.scriptToolStripMenuItem.Name = "scriptToolStripMenuItem";
			this.scriptToolStripMenuItem.Size = new System.Drawing.Size(46, 20);
			this.scriptToolStripMenuItem.Text = "&Script";
			// 
			// editToolStripMenuItem
			// 
			this.editToolStripMenuItem.Name = "editToolStripMenuItem";
			this.editToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
			this.editToolStripMenuItem.Text = "Edit";
			// 
			// toggleToolStripMenuItem
			// 
			this.toggleToolStripMenuItem.Name = "toggleToolStripMenuItem";
			this.toggleToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
			this.toggleToolStripMenuItem.Text = "Toggle";
			// 
			// insertSeparatorToolStripMenuItem
			// 
			this.insertSeparatorToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.InsertSeparator;
			this.insertSeparatorToolStripMenuItem.Name = "insertSeparatorToolStripMenuItem";
			this.insertSeparatorToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
			this.insertSeparatorToolStripMenuItem.Text = "Insert Separator";
			// 
			// turnOffAllScriptsToolStripMenuItem
			// 
			this.turnOffAllScriptsToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.Stop;
			this.turnOffAllScriptsToolStripMenuItem.Name = "turnOffAllScriptsToolStripMenuItem";
			this.turnOffAllScriptsToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
			this.turnOffAllScriptsToolStripMenuItem.Text = "Turn Off All Scripts";
			this.turnOffAllScriptsToolStripMenuItem.Click += new System.EventHandler(this.turnOffAllScriptsToolStripMenuItem_Click);
			// 
			// viewToolStripMenuItem
			// 
			this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.removeToolStripMenuItem,
            this.insertSeperatorToolStripMenuItem,
            this.toolStripSeparator2,
            this.moveUpToolStripMenuItem,
            this.moveDownToolStripMenuItem});
			this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
			this.viewToolStripMenuItem.Size = new System.Drawing.Size(41, 20);
			this.viewToolStripMenuItem.Text = "&View";
			// 
			// removeToolStripMenuItem
			// 
			this.removeToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.Delete;
			this.removeToolStripMenuItem.Name = "removeToolStripMenuItem";
			this.removeToolStripMenuItem.Size = new System.Drawing.Size(165, 22);
			this.removeToolStripMenuItem.Text = "Remove";
			this.removeToolStripMenuItem.Click += new System.EventHandler(this.removeToolStripMenuItem_Click);
			// 
			// insertSeperatorToolStripMenuItem
			// 
			this.insertSeperatorToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.InsertSeparator;
			this.insertSeperatorToolStripMenuItem.Name = "insertSeperatorToolStripMenuItem";
			this.insertSeperatorToolStripMenuItem.Size = new System.Drawing.Size(165, 22);
			this.insertSeperatorToolStripMenuItem.Text = "Insert Seperator";
			this.insertSeperatorToolStripMenuItem.Click += new System.EventHandler(this.insertSeperatorToolStripMenuItem_Click);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(162, 6);
			// 
			// moveUpToolStripMenuItem
			// 
			this.moveUpToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.MoveUp;
			this.moveUpToolStripMenuItem.Name = "moveUpToolStripMenuItem";
			this.moveUpToolStripMenuItem.Size = new System.Drawing.Size(165, 22);
			this.moveUpToolStripMenuItem.Text = "Move Up";
			this.moveUpToolStripMenuItem.Click += new System.EventHandler(this.moveUpToolStripMenuItem_Click);
			// 
			// moveDownToolStripMenuItem
			// 
			this.moveDownToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.MoveDown;
			this.moveDownToolStripMenuItem.Name = "moveDownToolStripMenuItem";
			this.moveDownToolStripMenuItem.Size = new System.Drawing.Size(165, 22);
			this.moveDownToolStripMenuItem.Text = "Move Down";
			this.moveDownToolStripMenuItem.Click += new System.EventHandler(this.moveDownToolStripMenuItem_Click);
			// 
			// optionsToolStripMenuItem
			// 
			this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveWindowPositionToolStripMenuItem,
            this.autoloadConsoleToolStripMenuItem,
            this.toolStripSeparator5,
            this.restoreWindowSizeToolStripMenuItem});
			this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
			this.optionsToolStripMenuItem.Size = new System.Drawing.Size(56, 20);
			this.optionsToolStripMenuItem.Text = "&Options";
			this.optionsToolStripMenuItem.DropDownOpened += new System.EventHandler(this.optionsToolStripMenuItem_DropDownOpened);
			// 
			// saveWindowPositionToolStripMenuItem
			// 
			this.saveWindowPositionToolStripMenuItem.Name = "saveWindowPositionToolStripMenuItem";
			this.saveWindowPositionToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
			this.saveWindowPositionToolStripMenuItem.Text = "Save Window Position";
			this.saveWindowPositionToolStripMenuItem.Click += new System.EventHandler(this.saveWindowPositionToolStripMenuItem_Click);
			// 
			// autoloadConsoleToolStripMenuItem
			// 
			this.autoloadConsoleToolStripMenuItem.Name = "autoloadConsoleToolStripMenuItem";
			this.autoloadConsoleToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
			this.autoloadConsoleToolStripMenuItem.Text = "Autoload Console";
			this.autoloadConsoleToolStripMenuItem.Click += new System.EventHandler(this.autoloadConsoleToolStripMenuItem_Click);
			// 
			// toolStripSeparator5
			// 
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			this.toolStripSeparator5.Size = new System.Drawing.Size(187, 6);
			// 
			// restoreWindowSizeToolStripMenuItem
			// 
			this.restoreWindowSizeToolStripMenuItem.Name = "restoreWindowSizeToolStripMenuItem";
			this.restoreWindowSizeToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
			this.restoreWindowSizeToolStripMenuItem.Text = "Restore Window Size";
			this.restoreWindowSizeToolStripMenuItem.Click += new System.EventHandler(this.restoreWindowSizeToolStripMenuItem_Click);
			// 
			// OutputBox
			// 
			this.OutputBox.Location = new System.Drawing.Point(6, 17);
			this.OutputBox.Name = "OutputBox";
			this.OutputBox.ReadOnly = true;
			this.OutputBox.Size = new System.Drawing.Size(246, 253);
			this.OutputBox.TabIndex = 2;
			this.OutputBox.Text = "";
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.OutputBox);
			this.groupBox1.Location = new System.Drawing.Point(311, 51);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(258, 278);
			this.groupBox1.TabIndex = 3;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Output";
			// 
			// NumberOfScripts
			// 
			this.NumberOfScripts.AutoSize = true;
			this.NumberOfScripts.Location = new System.Drawing.Point(12, 29);
			this.NumberOfScripts.Name = "NumberOfScripts";
			this.NumberOfScripts.Size = new System.Drawing.Size(66, 13);
			this.NumberOfScripts.TabIndex = 4;
			this.NumberOfScripts.Text = " 0 Scripts     ";
			// 
			// contextMenuStrip1
			// 
			this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.removeScriptToolStripMenuItem,
            this.insertSeperatorToolStripMenuItem1,
            this.toolStripSeparator4,
            this.stopAllScriptsToolStripMenuItem});
			this.contextMenuStrip1.Name = "contextMenuStrip1";
			this.contextMenuStrip1.Size = new System.Drawing.Size(176, 76);
			// 
			// removeScriptToolStripMenuItem
			// 
			this.removeScriptToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.Close;
			this.removeScriptToolStripMenuItem.Name = "removeScriptToolStripMenuItem";
			this.removeScriptToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
			this.removeScriptToolStripMenuItem.Text = "Remove script";
			this.removeScriptToolStripMenuItem.Click += new System.EventHandler(this.removeScriptToolStripMenuItem_Click);
			// 
			// insertSeperatorToolStripMenuItem1
			// 
			this.insertSeperatorToolStripMenuItem1.Image = global::BizHawk.MultiClient.Properties.Resources.InsertSeparator;
			this.insertSeperatorToolStripMenuItem1.Name = "insertSeperatorToolStripMenuItem1";
			this.insertSeperatorToolStripMenuItem1.Size = new System.Drawing.Size(175, 22);
			this.insertSeperatorToolStripMenuItem1.Text = "Insert Seperator";
			this.insertSeperatorToolStripMenuItem1.Click += new System.EventHandler(this.insertSeperatorToolStripMenuItem1_Click);
			// 
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size(172, 6);
			// 
			// stopAllScriptsToolStripMenuItem
			// 
			this.stopAllScriptsToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.Stop;
			this.stopAllScriptsToolStripMenuItem.Name = "stopAllScriptsToolStripMenuItem";
			this.stopAllScriptsToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
			this.stopAllScriptsToolStripMenuItem.Text = "Turn Off All Scripts";
			this.stopAllScriptsToolStripMenuItem.Click += new System.EventHandler(this.stopAllScriptsToolStripMenuItem_Click);
			// 
			// LuaConsole
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(598, 359);
			this.Controls.Add(this.NumberOfScripts);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.LuaListView);
			this.Controls.Add(this.menuStrip1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "LuaConsole";
			this.Text = "Lua Console";
			this.Load += new System.EventHandler(this.LuaConsole_Load);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.groupBox1.ResumeLayout(false);
			this.contextMenuStrip1.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private VirtualListView LuaListView;
        private System.Windows.Forms.ColumnHeader PathName;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem scriptToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toggleToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem moveUpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem moveDownToolStripMenuItem;
        public System.Windows.Forms.ColumnHeader Script;
        private System.Windows.Forms.RichTextBox OutputBox;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ToolStripMenuItem newToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem removeToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveWindowPositionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem restoreWindowSizeToolStripMenuItem;
        private System.Windows.Forms.Label NumberOfScripts;
        private System.Windows.Forms.ToolStripMenuItem insertSeparatorToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem turnOffAllScriptsToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem recentToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem noneToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem clearToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem stopAllScriptsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem autoloadConsoleToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem removeScriptToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem insertSeperatorToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem insertSeperatorToolStripMenuItem1;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
    }
}