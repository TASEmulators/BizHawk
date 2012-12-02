namespace BizHawk.MultiClient
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

			Global.MovieSession.Movie.StateCapturing = false;

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
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.newProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveProjectAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.recentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.nToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.clearToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.importTASFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.clearToolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
			this.deleteFramesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.cloneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.insertFrameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.insertNumFramesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
			this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.pasteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.pasteInsertToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.cutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.selectAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
			this.truncateMovieToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.clearVirtualPadsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveWindowPositionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.autoloadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.updatePadsOnMovePlaybackToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.restoreWindowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ReadOnlyCheckBox = new System.Windows.Forms.CheckBox();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.clearToolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
			this.ContextMenu_Delete = new System.Windows.Forms.ToolStripMenuItem();
			this.cloneToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.ContextMenu_Insert = new System.Windows.Forms.ToolStripMenuItem();
			this.insertFramesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripItem_SelectAll = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
			this.truncateMovieToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.ClipboardDisplay = new System.Windows.Forms.Label();
			this.SelectionDisplay = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.toolStrip1 = new ToolStripEx();
			this.RewindToBeginning = new System.Windows.Forms.ToolStripButton();
			this.RewindButton = new System.Windows.Forms.ToolStripButton();
			this.PauseButton = new System.Windows.Forms.ToolStripButton();
			this.FrameAdvanceButton = new System.Windows.Forms.ToolStripButton();
			this.FastForward = new System.Windows.Forms.ToolStripButton();
			this.TurboFastForward = new System.Windows.Forms.ToolStripButton();
			this.FastFowardToEnd = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
			this.StopButton = new System.Windows.Forms.ToolStripButton();
			this.TASView = new BizHawk.VirtualListView();
			this.Frame = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Log = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.menuStrip1.SuspendLayout();
			this.contextMenuStrip1.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem,
            this.settingsToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(460, 24);
			this.menuStrip1.TabIndex = 0;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newProjectToolStripMenuItem,
            this.openProjectToolStripMenuItem,
            this.saveProjectToolStripMenuItem,
            this.saveProjectAsToolStripMenuItem,
            this.recentToolStripMenuItem,
            this.toolStripSeparator1,
            this.importTASFileToolStripMenuItem,
            this.toolStripSeparator2,
            this.exitToolStripMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this.fileToolStripMenuItem.Text = "&File";
			// 
			// newProjectToolStripMenuItem
			// 
			this.newProjectToolStripMenuItem.Name = "newProjectToolStripMenuItem";
			this.newProjectToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
			this.newProjectToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
			this.newProjectToolStripMenuItem.Text = "New Project";
			this.newProjectToolStripMenuItem.Click += new System.EventHandler(this.newProjectToolStripMenuItem_Click);
			// 
			// openProjectToolStripMenuItem
			// 
			this.openProjectToolStripMenuItem.Name = "openProjectToolStripMenuItem";
			this.openProjectToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.openProjectToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
			this.openProjectToolStripMenuItem.Text = "&Open Project";
			this.openProjectToolStripMenuItem.Click += new System.EventHandler(this.openProjectToolStripMenuItem_Click);
			// 
			// saveProjectToolStripMenuItem
			// 
			this.saveProjectToolStripMenuItem.Name = "saveProjectToolStripMenuItem";
			this.saveProjectToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
			this.saveProjectToolStripMenuItem.Text = "&Save Project";
			this.saveProjectToolStripMenuItem.Click += new System.EventHandler(this.saveProjectToolStripMenuItem_Click);
			// 
			// saveProjectAsToolStripMenuItem
			// 
			this.saveProjectAsToolStripMenuItem.Name = "saveProjectAsToolStripMenuItem";
			this.saveProjectAsToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
			this.saveProjectAsToolStripMenuItem.Text = "Save Project As";
			this.saveProjectAsToolStripMenuItem.Click += new System.EventHandler(this.saveProjectAsToolStripMenuItem_Click);
			// 
			// recentToolStripMenuItem
			// 
			this.recentToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.nToolStripMenuItem,
            this.toolStripSeparator3,
            this.clearToolStripMenuItem});
			this.recentToolStripMenuItem.Image = global::BizHawk.MultiClient.Properties.Resources.Recent;
			this.recentToolStripMenuItem.Name = "recentToolStripMenuItem";
			this.recentToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
			this.recentToolStripMenuItem.Text = "Recent";
			// 
			// nToolStripMenuItem
			// 
			this.nToolStripMenuItem.Name = "nToolStripMenuItem";
			this.nToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
			this.nToolStripMenuItem.Text = "None";
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(100, 6);
			// 
			// clearToolStripMenuItem
			// 
			this.clearToolStripMenuItem.Name = "clearToolStripMenuItem";
			this.clearToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
			this.clearToolStripMenuItem.Text = "Clear";
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(183, 6);
			// 
			// importTASFileToolStripMenuItem
			// 
			this.importTASFileToolStripMenuItem.Name = "importTASFileToolStripMenuItem";
			this.importTASFileToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
			this.importTASFileToolStripMenuItem.Text = "Import TAS file";
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(183, 6);
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
			this.exitToolStripMenuItem.Text = "E&xit";
			this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
			// 
			// editToolStripMenuItem
			// 
			this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.clearToolStripMenuItem2,
            this.deleteFramesToolStripMenuItem,
            this.cloneToolStripMenuItem,
            this.insertFrameToolStripMenuItem,
            this.insertNumFramesToolStripMenuItem,
            this.toolStripSeparator7,
            this.copyToolStripMenuItem,
            this.pasteToolStripMenuItem,
            this.pasteInsertToolStripMenuItem,
            this.cutToolStripMenuItem,
            this.selectAllToolStripMenuItem,
            this.toolStripSeparator8,
            this.truncateMovieToolStripMenuItem,
            this.clearVirtualPadsToolStripMenuItem});
			this.editToolStripMenuItem.Name = "editToolStripMenuItem";
			this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
			this.editToolStripMenuItem.Text = "&Edit";
			this.editToolStripMenuItem.DropDownOpened += new System.EventHandler(this.editToolStripMenuItem_DropDownOpened);
			// 
			// clearToolStripMenuItem2
			// 
			this.clearToolStripMenuItem2.Name = "clearToolStripMenuItem2";
			this.clearToolStripMenuItem2.ShortcutKeyDisplayString = "Del";
			this.clearToolStripMenuItem2.Size = new System.Drawing.Size(207, 22);
			this.clearToolStripMenuItem2.Text = "Clear";
			this.clearToolStripMenuItem2.Click += new System.EventHandler(this.clearToolStripMenuItem2_Click);
			// 
			// deleteFramesToolStripMenuItem
			// 
			this.deleteFramesToolStripMenuItem.Name = "deleteFramesToolStripMenuItem";
			this.deleteFramesToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Delete)));
			this.deleteFramesToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
			this.deleteFramesToolStripMenuItem.Text = "&Delete";
			this.deleteFramesToolStripMenuItem.Click += new System.EventHandler(this.deleteFramesToolStripMenuItem_Click);
			// 
			// cloneToolStripMenuItem
			// 
			this.cloneToolStripMenuItem.Name = "cloneToolStripMenuItem";
			this.cloneToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Insert)));
			this.cloneToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
			this.cloneToolStripMenuItem.Text = "&Clone";
			this.cloneToolStripMenuItem.Click += new System.EventHandler(this.cloneToolStripMenuItem_Click);
			// 
			// insertFrameToolStripMenuItem
			// 
			this.insertFrameToolStripMenuItem.Name = "insertFrameToolStripMenuItem";
			this.insertFrameToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.Insert)));
			this.insertFrameToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
			this.insertFrameToolStripMenuItem.Text = "&Insert";
			this.insertFrameToolStripMenuItem.Click += new System.EventHandler(this.insertFrameToolStripMenuItem_Click);
			// 
			// insertNumFramesToolStripMenuItem
			// 
			this.insertNumFramesToolStripMenuItem.Name = "insertNumFramesToolStripMenuItem";
			this.insertNumFramesToolStripMenuItem.ShortcutKeyDisplayString = "Ins";
			this.insertNumFramesToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
			this.insertNumFramesToolStripMenuItem.Text = "Insert # of Frames";
			this.insertNumFramesToolStripMenuItem.Click += new System.EventHandler(this.insertNumFramesToolStripMenuItem_Click);
			// 
			// toolStripSeparator7
			// 
			this.toolStripSeparator7.Name = "toolStripSeparator7";
			this.toolStripSeparator7.Size = new System.Drawing.Size(204, 6);
			// 
			// copyToolStripMenuItem
			// 
			this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
			this.copyToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
			this.copyToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
			this.copyToolStripMenuItem.Text = "Copy";
			this.copyToolStripMenuItem.Click += new System.EventHandler(this.copyToolStripMenuItem_Click);
			// 
			// pasteToolStripMenuItem
			// 
			this.pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
			this.pasteToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.V)));
			this.pasteToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
			this.pasteToolStripMenuItem.Text = "&Paste";
			this.pasteToolStripMenuItem.Click += new System.EventHandler(this.pasteToolStripMenuItem_Click);
			// 
			// pasteInsertToolStripMenuItem
			// 
			this.pasteInsertToolStripMenuItem.Name = "pasteInsertToolStripMenuItem";
			this.pasteInsertToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.V)));
			this.pasteInsertToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
			this.pasteInsertToolStripMenuItem.Text = "&Paste Insert";
			this.pasteInsertToolStripMenuItem.Click += new System.EventHandler(this.pasteInsertToolStripMenuItem_Click);
			// 
			// cutToolStripMenuItem
			// 
			this.cutToolStripMenuItem.Name = "cutToolStripMenuItem";
			this.cutToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.X)));
			this.cutToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
			this.cutToolStripMenuItem.Text = "&Cut";
			this.cutToolStripMenuItem.Click += new System.EventHandler(this.cutToolStripMenuItem_Click);
			// 
			// selectAllToolStripMenuItem
			// 
			this.selectAllToolStripMenuItem.Name = "selectAllToolStripMenuItem";
			this.selectAllToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
			this.selectAllToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
			this.selectAllToolStripMenuItem.Text = "Select &All";
			this.selectAllToolStripMenuItem.Click += new System.EventHandler(this.selectAllToolStripMenuItem_Click);
			// 
			// toolStripSeparator8
			// 
			this.toolStripSeparator8.Name = "toolStripSeparator8";
			this.toolStripSeparator8.Size = new System.Drawing.Size(204, 6);
			// 
			// truncateMovieToolStripMenuItem
			// 
			this.truncateMovieToolStripMenuItem.Name = "truncateMovieToolStripMenuItem";
			this.truncateMovieToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
			this.truncateMovieToolStripMenuItem.Text = "&Truncate Movie";
			this.truncateMovieToolStripMenuItem.Click += new System.EventHandler(this.truncateMovieToolStripMenuItem_Click);
			// 
			// clearVirtualPadsToolStripMenuItem
			// 
			this.clearVirtualPadsToolStripMenuItem.Name = "clearVirtualPadsToolStripMenuItem";
			this.clearVirtualPadsToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
			this.clearVirtualPadsToolStripMenuItem.Text = "Clear controller &holds";
			// 
			// settingsToolStripMenuItem
			// 
			this.settingsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveWindowPositionToolStripMenuItem,
            this.autoloadToolStripMenuItem,
            this.updatePadsOnMovePlaybackToolStripMenuItem,
            this.toolStripSeparator4,
            this.restoreWindowToolStripMenuItem});
			this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
			this.settingsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
			this.settingsToolStripMenuItem.Text = "&Settings";
			this.settingsToolStripMenuItem.DropDownOpened += new System.EventHandler(this.settingsToolStripMenuItem_DropDownOpened);
			// 
			// saveWindowPositionToolStripMenuItem
			// 
			this.saveWindowPositionToolStripMenuItem.Name = "saveWindowPositionToolStripMenuItem";
			this.saveWindowPositionToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
			this.saveWindowPositionToolStripMenuItem.Text = "Save Window Position";
			this.saveWindowPositionToolStripMenuItem.Click += new System.EventHandler(this.saveWindowPositionToolStripMenuItem_Click);
			// 
			// autoloadToolStripMenuItem
			// 
			this.autoloadToolStripMenuItem.Name = "autoloadToolStripMenuItem";
			this.autoloadToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
			this.autoloadToolStripMenuItem.Text = "Autoload";
			this.autoloadToolStripMenuItem.Click += new System.EventHandler(this.autoloadToolStripMenuItem_Click);
			// 
			// updatePadsOnMovePlaybackToolStripMenuItem
			// 
			this.updatePadsOnMovePlaybackToolStripMenuItem.Name = "updatePadsOnMovePlaybackToolStripMenuItem";
			this.updatePadsOnMovePlaybackToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
			this.updatePadsOnMovePlaybackToolStripMenuItem.Text = "Update Pads on Move playback";
			this.updatePadsOnMovePlaybackToolStripMenuItem.Click += new System.EventHandler(this.updatePadsOnMovePlaybackToolStripMenuItem_Click);
			// 
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size(237, 6);
			// 
			// restoreWindowToolStripMenuItem
			// 
			this.restoreWindowToolStripMenuItem.Name = "restoreWindowToolStripMenuItem";
			this.restoreWindowToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
			this.restoreWindowToolStripMenuItem.Text = "Restore Default Settings";
			this.restoreWindowToolStripMenuItem.Click += new System.EventHandler(this.restoreWindowToolStripMenuItem_Click);
			// 
			// ReadOnlyCheckBox
			// 
			this.ReadOnlyCheckBox.Appearance = System.Windows.Forms.Appearance.Button;
			this.ReadOnlyCheckBox.AutoSize = true;
			this.ReadOnlyCheckBox.BackColor = System.Drawing.SystemColors.Control;
			this.ReadOnlyCheckBox.Image = global::BizHawk.MultiClient.Properties.Resources.ReadOnly;
			this.ReadOnlyCheckBox.ImageAlign = System.Drawing.ContentAlignment.BottomRight;
			this.ReadOnlyCheckBox.Location = new System.Drawing.Point(12, 27);
			this.ReadOnlyCheckBox.Name = "ReadOnlyCheckBox";
			this.ReadOnlyCheckBox.Size = new System.Drawing.Size(22, 22);
			this.ReadOnlyCheckBox.TabIndex = 3;
			this.toolTip1.SetToolTip(this.ReadOnlyCheckBox, "Read-only");
			this.ReadOnlyCheckBox.UseVisualStyleBackColor = false;
			this.ReadOnlyCheckBox.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
			// 
			// contextMenuStrip1
			// 
			this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.clearToolStripMenuItem3,
            this.ContextMenu_Delete,
            this.cloneToolStripMenuItem1,
            this.ContextMenu_Insert,
            this.insertFramesToolStripMenuItem,
            this.toolStripSeparator5,
            this.toolStripItem_SelectAll,
            this.toolStripSeparator9,
            this.truncateMovieToolStripMenuItem1});
			this.contextMenuStrip1.Name = "contextMenuStrip1";
			this.contextMenuStrip1.Size = new System.Drawing.Size(185, 170);
			this.contextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip1_Opening);
			// 
			// clearToolStripMenuItem3
			// 
			this.clearToolStripMenuItem3.Name = "clearToolStripMenuItem3";
			this.clearToolStripMenuItem3.Size = new System.Drawing.Size(184, 22);
			this.clearToolStripMenuItem3.Text = "Clear";
			// 
			// ContextMenu_Delete
			// 
			this.ContextMenu_Delete.Name = "ContextMenu_Delete";
			this.ContextMenu_Delete.ShortcutKeyDisplayString = "Ctrl+Del";
			this.ContextMenu_Delete.Size = new System.Drawing.Size(184, 22);
			this.ContextMenu_Delete.Text = "Delete";
			this.ContextMenu_Delete.Click += new System.EventHandler(this.Delete_Click);
			// 
			// cloneToolStripMenuItem1
			// 
			this.cloneToolStripMenuItem1.Name = "cloneToolStripMenuItem1";
			this.cloneToolStripMenuItem1.ShortcutKeyDisplayString = "Ctrl+Ins";
			this.cloneToolStripMenuItem1.Size = new System.Drawing.Size(184, 22);
			this.cloneToolStripMenuItem1.Text = "Clone";
			this.cloneToolStripMenuItem1.Click += new System.EventHandler(this.cloneToolStripMenuItem1_Click);
			// 
			// ContextMenu_Insert
			// 
			this.ContextMenu_Insert.Name = "ContextMenu_Insert";
			this.ContextMenu_Insert.ShortcutKeyDisplayString = "Ctrl+Shift+Ins";
			this.ContextMenu_Insert.Size = new System.Drawing.Size(184, 22);
			this.ContextMenu_Insert.Text = "Insert";
			this.ContextMenu_Insert.Click += new System.EventHandler(this.Insert_Click);
			// 
			// insertFramesToolStripMenuItem
			// 
			this.insertFramesToolStripMenuItem.Name = "insertFramesToolStripMenuItem";
			this.insertFramesToolStripMenuItem.Size = new System.Drawing.Size(184, 22);
			this.insertFramesToolStripMenuItem.Text = "Insert # Frames";
			this.insertFramesToolStripMenuItem.Click += new System.EventHandler(this.insertFramesToolStripMenuItem_Click);
			// 
			// toolStripSeparator5
			// 
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			this.toolStripSeparator5.Size = new System.Drawing.Size(181, 6);
			// 
			// toolStripItem_SelectAll
			// 
			this.toolStripItem_SelectAll.Name = "toolStripItem_SelectAll";
			this.toolStripItem_SelectAll.Size = new System.Drawing.Size(184, 22);
			this.toolStripItem_SelectAll.Text = "Select All";
			this.toolStripItem_SelectAll.Click += new System.EventHandler(this.SelectAll_Click);
			// 
			// toolStripSeparator9
			// 
			this.toolStripSeparator9.Name = "toolStripSeparator9";
			this.toolStripSeparator9.Size = new System.Drawing.Size(181, 6);
			// 
			// truncateMovieToolStripMenuItem1
			// 
			this.truncateMovieToolStripMenuItem1.Name = "truncateMovieToolStripMenuItem1";
			this.truncateMovieToolStripMenuItem1.Size = new System.Drawing.Size(184, 22);
			this.truncateMovieToolStripMenuItem1.Text = "&Truncate Movie";
			this.truncateMovieToolStripMenuItem1.Click += new System.EventHandler(this.truncateMovieToolStripMenuItem1_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add(this.ClipboardDisplay);
			this.groupBox1.Controls.Add(this.SelectionDisplay);
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Location = new System.Drawing.Point(302, 55);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(146, 83);
			this.groupBox1.TabIndex = 5;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Slicer";
			// 
			// ClipboardDisplay
			// 
			this.ClipboardDisplay.AutoSize = true;
			this.ClipboardDisplay.Location = new System.Drawing.Point(68, 36);
			this.ClipboardDisplay.Name = "ClipboardDisplay";
			this.ClipboardDisplay.Size = new System.Drawing.Size(31, 13);
			this.ClipboardDisplay.TabIndex = 3;
			this.ClipboardDisplay.Text = "none";
			// 
			// SelectionDisplay
			// 
			this.SelectionDisplay.AutoSize = true;
			this.SelectionDisplay.Location = new System.Drawing.Point(68, 19);
			this.SelectionDisplay.Name = "SelectionDisplay";
			this.SelectionDisplay.Size = new System.Drawing.Size(31, 13);
			this.SelectionDisplay.TabIndex = 2;
			this.SelectionDisplay.Text = "none";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(8, 36);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(54, 13);
			this.label2.TabIndex = 1;
			this.label2.Text = "Clipboard:";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(8, 19);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(54, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Selection:";
			// 
			// toolStrip1
			// 
			this.toolStrip1.ClickThrough = true;
			this.toolStrip1.Dock = System.Windows.Forms.DockStyle.None;
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.RewindToBeginning,
            this.RewindButton,
            this.PauseButton,
            this.FrameAdvanceButton,
            this.FastForward,
            this.TurboFastForward,
            this.FastFowardToEnd,
            this.toolStripSeparator6,
            this.StopButton});
			this.toolStrip1.Location = new System.Drawing.Point(37, 27);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.Size = new System.Drawing.Size(202, 25);
			this.toolStrip1.TabIndex = 0;
			// 
			// RewindToBeginning
			// 
			this.RewindToBeginning.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.RewindToBeginning.Image = global::BizHawk.MultiClient.Properties.Resources.BackMore;
			this.RewindToBeginning.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.RewindToBeginning.Name = "RewindToBeginning";
			this.RewindToBeginning.Size = new System.Drawing.Size(23, 22);
			this.RewindToBeginning.Text = "<<";
			this.RewindToBeginning.ToolTipText = "Rewind to Beginning";
			this.RewindToBeginning.Click += new System.EventHandler(this.RewindToBeginning_Click);
			// 
			// RewindButton
			// 
			this.RewindButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.RewindButton.Image = global::BizHawk.MultiClient.Properties.Resources.Back;
			this.RewindButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.RewindButton.Name = "RewindButton";
			this.RewindButton.Size = new System.Drawing.Size(23, 22);
			this.RewindButton.Text = "<";
			this.RewindButton.ToolTipText = "Rewind";
			this.RewindButton.Click += new System.EventHandler(this.RewindButton_Click);
			// 
			// PauseButton
			// 
			this.PauseButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.PauseButton.Image = global::BizHawk.MultiClient.Properties.Resources.Pause;
			this.PauseButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.PauseButton.Name = "PauseButton";
			this.PauseButton.Size = new System.Drawing.Size(23, 22);
			this.PauseButton.Text = "Pause Button";
			this.PauseButton.ToolTipText = "Pause";
			this.PauseButton.Click += new System.EventHandler(this.PauseButton_Click);
			// 
			// FrameAdvanceButton
			// 
			this.FrameAdvanceButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FrameAdvanceButton.Image = global::BizHawk.MultiClient.Properties.Resources.Forward;
			this.FrameAdvanceButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FrameAdvanceButton.Name = "FrameAdvanceButton";
			this.FrameAdvanceButton.Size = new System.Drawing.Size(23, 22);
			this.FrameAdvanceButton.Text = ">";
			this.FrameAdvanceButton.ToolTipText = "Frame Advance";
			this.FrameAdvanceButton.Click += new System.EventHandler(this.FrameAdvanceButton_Click);
			// 
			// FastForward
			// 
			this.FastForward.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FastForward.Image = global::BizHawk.MultiClient.Properties.Resources.FastForward;
			this.FastForward.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FastForward.Name = "FastForward";
			this.FastForward.Size = new System.Drawing.Size(23, 22);
			this.FastForward.Text = ">>";
			this.FastForward.ToolTipText = "Fast Forward";
			this.FastForward.Click += new System.EventHandler(this.FastForward_Click);
			// 
			// TurboFastForward
			// 
			this.TurboFastForward.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.TurboFastForward.Enabled = false;
			this.TurboFastForward.Image = global::BizHawk.MultiClient.Properties.Resources.TurboFastForward;
			this.TurboFastForward.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.TurboFastForward.Name = "TurboFastForward";
			this.TurboFastForward.Size = new System.Drawing.Size(23, 22);
			this.TurboFastForward.Text = ">>>";
			this.TurboFastForward.ToolTipText = "Turbo Fast Forward";
			this.TurboFastForward.Click += new System.EventHandler(this.TurboFastForward_Click);
			// 
			// FastFowardToEnd
			// 
			this.FastFowardToEnd.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FastFowardToEnd.Image = global::BizHawk.MultiClient.Properties.Resources.ForwardMore;
			this.FastFowardToEnd.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FastFowardToEnd.Name = "FastFowardToEnd";
			this.FastFowardToEnd.Size = new System.Drawing.Size(23, 22);
			this.FastFowardToEnd.Text = ">>";
			this.FastFowardToEnd.ToolTipText = "Fast Foward To End";
			this.FastFowardToEnd.Click += new System.EventHandler(this.FastForwardToEnd_Click);
			// 
			// toolStripSeparator6
			// 
			this.toolStripSeparator6.Name = "toolStripSeparator6";
			this.toolStripSeparator6.Size = new System.Drawing.Size(6, 25);
			// 
			// StopButton
			// 
			this.StopButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.StopButton.Image = global::BizHawk.MultiClient.Properties.Resources.Stop;
			this.StopButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.StopButton.Name = "StopButton";
			this.StopButton.Size = new System.Drawing.Size(23, 22);
			this.StopButton.Text = "Stop Movie";
			this.StopButton.Click += new System.EventHandler(this.StopButton_Click);
			// 
			// TASView
			// 
			this.TASView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.TASView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Frame,
            this.Log});
			this.TASView.ContextMenuStrip = this.contextMenuStrip1;
			this.TASView.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.TASView.FullRowSelect = true;
			this.TASView.GridLines = true;
			this.TASView.ItemCount = 0;
			this.TASView.Location = new System.Drawing.Point(12, 55);
			this.TASView.Name = "TASView";
			this.TASView.selectedItem = -1;
			this.TASView.Size = new System.Drawing.Size(282, 452);
			this.TASView.TabIndex = 1;
			this.TASView.UseCompatibleStateImageBehavior = false;
			this.TASView.View = System.Windows.Forms.View.Details;
			this.TASView.SelectedIndexChanged += new System.EventHandler(this.TASView_SelectedIndexChanged);
			this.TASView.Click += new System.EventHandler(this.TASView_Click);
			this.TASView.DoubleClick += new System.EventHandler(this.TASView_DoubleClick);
			this.TASView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TASView_KeyDown);
			this.TASView.MouseUp += new System.Windows.Forms.MouseEventHandler(this.TASView_MouseUp);
			this.TASView.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.TASView_MouseWheel);
			// 
			// Frame
			// 
			this.Frame.Text = "Frame";
			// 
			// Log
			// 
			this.Log.Text = "Log";
			this.Log.Width = 201;
			// 
			// TAStudio
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(460, 519);
			this.Controls.Add(this.toolStrip1);
			this.Controls.Add(this.menuStrip1);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.ReadOnlyCheckBox);
			this.Controls.Add(this.TASView);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.menuStrip1;
			this.MinimumSize = new System.Drawing.Size(437, 148);
			this.Name = "TAStudio";
			this.Text = "TAStudio";
			this.Load += new System.EventHandler(this.TAStudio_Load);
			this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TAStudio_KeyPress);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.contextMenuStrip1.ResumeLayout(false);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newProjectToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openProjectToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveProjectToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveProjectAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem importTASFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveWindowPositionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem restoreWindowToolStripMenuItem;
        private VirtualListView TASView;
		private System.Windows.Forms.ColumnHeader Log;
        private System.Windows.Forms.ToolStripMenuItem recentToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem nToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripMenuItem clearToolStripMenuItem;
		private ToolStripEx toolStrip1;
        private System.Windows.Forms.ToolStripButton FrameAdvanceButton;
        private System.Windows.Forms.ToolStripButton RewindButton;
		private System.Windows.Forms.ToolStripButton PauseButton;
		private System.Windows.Forms.ToolStripMenuItem autoloadToolStripMenuItem;
		private System.Windows.Forms.CheckBox ReadOnlyCheckBox;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.ColumnHeader Frame;
		private System.Windows.Forms.ToolStripButton RewindToBeginning;
		private System.Windows.Forms.ToolStripButton FastFowardToEnd;
		private System.Windows.Forms.ToolStripMenuItem insertFrameToolStripMenuItem;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
		private System.Windows.Forms.ToolStripMenuItem ContextMenu_Insert;
		private System.Windows.Forms.ToolStripMenuItem toolStripItem_SelectAll;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
		private System.Windows.Forms.ToolStripButton StopButton;
		private System.Windows.Forms.ToolStripMenuItem updatePadsOnMovePlaybackToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
		private System.Windows.Forms.ToolStripMenuItem clearVirtualPadsToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton FastForward;
        private System.Windows.Forms.ToolStripButton TurboFastForward;
		private System.Windows.Forms.ToolStripMenuItem ContextMenu_Delete;
		private System.Windows.Forms.ToolStripMenuItem cloneToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem cloneToolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem deleteFramesToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem clearToolStripMenuItem2;
		private System.Windows.Forms.ToolStripMenuItem insertNumFramesToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem clearToolStripMenuItem3;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
		private System.Windows.Forms.ToolStripMenuItem insertFramesToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem selectAllToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
		private System.Windows.Forms.ToolStripMenuItem truncateMovieToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator9;
		private System.Windows.Forms.ToolStripMenuItem truncateMovieToolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label ClipboardDisplay;
		private System.Windows.Forms.Label SelectionDisplay;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ToolStripMenuItem pasteToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem pasteInsertToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem cutToolStripMenuItem;
    }
}