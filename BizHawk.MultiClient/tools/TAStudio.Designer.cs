namespace BizHawk.MultiClient.tools
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveProjectAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.importTASFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveWindowPositionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.restoreWindowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.TASView = new VirtualListView();
            this.UpcolumnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.DowncolumnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.LeftcolumnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.RightcolumnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.recentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.nToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.clearToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
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
            this.menuStrip1.Size = new System.Drawing.Size(541, 24);
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
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(35, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // openProjectToolStripMenuItem
            // 
            this.openProjectToolStripMenuItem.Name = "openProjectToolStripMenuItem";
            this.openProjectToolStripMenuItem.Size = new System.Drawing.Size(161, 22);
            this.openProjectToolStripMenuItem.Text = "&Open Project";
            // 
            // saveProjectToolStripMenuItem
            // 
            this.saveProjectToolStripMenuItem.Name = "saveProjectToolStripMenuItem";
            this.saveProjectToolStripMenuItem.Size = new System.Drawing.Size(161, 22);
            this.saveProjectToolStripMenuItem.Text = "&Save Project";
            // 
            // saveProjectAsToolStripMenuItem
            // 
            this.saveProjectAsToolStripMenuItem.Name = "saveProjectAsToolStripMenuItem";
            this.saveProjectAsToolStripMenuItem.Size = new System.Drawing.Size(161, 22);
            this.saveProjectAsToolStripMenuItem.Text = "Save Project As";
            // 
            // newProjectToolStripMenuItem
            // 
            this.newProjectToolStripMenuItem.Name = "newProjectToolStripMenuItem";
            this.newProjectToolStripMenuItem.Size = new System.Drawing.Size(161, 22);
            this.newProjectToolStripMenuItem.Text = "New Project";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(158, 6);
            // 
            // importTASFileToolStripMenuItem
            // 
            this.importTASFileToolStripMenuItem.Name = "importTASFileToolStripMenuItem";
            this.importTASFileToolStripMenuItem.Size = new System.Drawing.Size(161, 22);
            this.importTASFileToolStripMenuItem.Text = "Import TAS file";
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(161, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.editToolStripMenuItem.Text = "&Edit";
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveWindowPositionToolStripMenuItem,
            this.restoreWindowToolStripMenuItem});
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(58, 20);
            this.settingsToolStripMenuItem.Text = "&Settings";
            this.settingsToolStripMenuItem.DropDownOpened += new System.EventHandler(this.settingsToolStripMenuItem_DropDownOpened);
            // 
            // saveWindowPositionToolStripMenuItem
            // 
            this.saveWindowPositionToolStripMenuItem.Name = "saveWindowPositionToolStripMenuItem";
            this.saveWindowPositionToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            this.saveWindowPositionToolStripMenuItem.Text = "Save Window Position";
            this.saveWindowPositionToolStripMenuItem.Click += new System.EventHandler(this.saveWindowPositionToolStripMenuItem_Click);
            // 
            // restoreWindowToolStripMenuItem
            // 
            this.restoreWindowToolStripMenuItem.Name = "restoreWindowToolStripMenuItem";
            this.restoreWindowToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            this.restoreWindowToolStripMenuItem.Text = "Restore Window";
            this.restoreWindowToolStripMenuItem.Click += new System.EventHandler(this.restoreWindowToolStripMenuItem_Click);
            // 
            // TASView
            // 
            this.TASView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.UpcolumnHeader1,
            this.DowncolumnHeader1,
            this.LeftcolumnHeader1,
            this.RightcolumnHeader1});
            this.TASView.GridLines = true;
            this.TASView.Location = new System.Drawing.Point(12, 38);
            this.TASView.Name = "TASView";
            this.TASView.Size = new System.Drawing.Size(399, 424);
            this.TASView.TabIndex = 1;
            this.TASView.UseCompatibleStateImageBehavior = false;
            this.TASView.View = System.Windows.Forms.View.Details;
            // 
            // UpcolumnHeader1
            // 
            this.UpcolumnHeader1.Text = "U";
            this.UpcolumnHeader1.Width = 19;
            // 
            // DowncolumnHeader1
            // 
            this.DowncolumnHeader1.Text = "D";
            this.DowncolumnHeader1.Width = 17;
            // 
            // LeftcolumnHeader1
            // 
            this.LeftcolumnHeader1.Text = "L";
            this.LeftcolumnHeader1.Width = 20;
            // 
            // RightcolumnHeader1
            // 
            this.RightcolumnHeader1.Text = "R";
            this.RightcolumnHeader1.Width = 21;
            // 
            // recentToolStripMenuItem
            // 
            this.recentToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.nToolStripMenuItem,
            this.toolStripSeparator3,
            this.clearToolStripMenuItem});
            this.recentToolStripMenuItem.Name = "recentToolStripMenuItem";
            this.recentToolStripMenuItem.Size = new System.Drawing.Size(161, 22);
            this.recentToolStripMenuItem.Text = "Recent";
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(158, 6);
            // 
            // nToolStripMenuItem
            // 
            this.nToolStripMenuItem.Name = "nToolStripMenuItem";
            this.nToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.nToolStripMenuItem.Text = "None";
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(149, 6);
            // 
            // clearToolStripMenuItem
            // 
            this.clearToolStripMenuItem.Name = "clearToolStripMenuItem";
            this.clearToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.clearToolStripMenuItem.Text = "Clear";
            // 
            // TAStudio
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(541, 474);
            this.Controls.Add(this.TASView);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "TAStudio";
            this.Text = "TAStudio";
            this.Load += new System.EventHandler(this.TAStudio_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
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
        private System.Windows.Forms.ColumnHeader UpcolumnHeader1;
        private System.Windows.Forms.ColumnHeader DowncolumnHeader1;
        private System.Windows.Forms.ColumnHeader LeftcolumnHeader1;
        private System.Windows.Forms.ColumnHeader RightcolumnHeader1;
        private System.Windows.Forms.ToolStripMenuItem recentToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem nToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem clearToolStripMenuItem;
    }
}