namespace BizHawk.MultiClient.tools
{
    partial class LuaWindow
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
            this.IDT_SCRIPTFILE = new System.Windows.Forms.TextBox();
            this.IDB_BROWSE = new System.Windows.Forms.Button();
            this.IDB_EDIT = new System.Windows.Forms.Button();
            this.IDB_RUN = new System.Windows.Forms.Button();
            this.IDB_STOP = new System.Windows.Forms.Button();
            this.IDT_OUTPUT = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveWindowPositionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.restoreWindowSizeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // IDT_SCRIPTFILE
            // 
            this.IDT_SCRIPTFILE.Location = new System.Drawing.Point(12, 45);
            this.IDT_SCRIPTFILE.Name = "IDT_SCRIPTFILE";
            this.IDT_SCRIPTFILE.Size = new System.Drawing.Size(349, 20);
            this.IDT_SCRIPTFILE.TabIndex = 0;
            // 
            // IDB_BROWSE
            // 
            this.IDB_BROWSE.Location = new System.Drawing.Point(12, 71);
            this.IDB_BROWSE.Name = "IDB_BROWSE";
            this.IDB_BROWSE.Size = new System.Drawing.Size(75, 23);
            this.IDB_BROWSE.TabIndex = 1;
            this.IDB_BROWSE.Text = "Browse";
            this.IDB_BROWSE.UseVisualStyleBackColor = true;
            this.IDB_BROWSE.Click += new System.EventHandler(this.IDB_BROWSE_Click);
            // 
            // IDB_EDIT
            // 
            this.IDB_EDIT.Location = new System.Drawing.Point(93, 71);
            this.IDB_EDIT.Name = "IDB_EDIT";
            this.IDB_EDIT.Size = new System.Drawing.Size(75, 23);
            this.IDB_EDIT.TabIndex = 2;
            this.IDB_EDIT.Text = "Edit";
            this.IDB_EDIT.UseVisualStyleBackColor = true;
            this.IDB_EDIT.Click += new System.EventHandler(this.IDB_EDIT_Click);
            // 
            // IDB_RUN
            // 
            this.IDB_RUN.Location = new System.Drawing.Point(286, 71);
            this.IDB_RUN.Name = "IDB_RUN";
            this.IDB_RUN.Size = new System.Drawing.Size(75, 23);
            this.IDB_RUN.TabIndex = 4;
            this.IDB_RUN.Text = "Run";
            this.IDB_RUN.UseVisualStyleBackColor = true;
            this.IDB_RUN.Click += new System.EventHandler(this.IDB_RUN_Click);
            // 
            // IDB_STOP
            // 
            this.IDB_STOP.Location = new System.Drawing.Point(205, 71);
            this.IDB_STOP.Name = "IDB_STOP";
            this.IDB_STOP.Size = new System.Drawing.Size(75, 23);
            this.IDB_STOP.TabIndex = 3;
            this.IDB_STOP.Text = "Stop";
            this.IDB_STOP.UseVisualStyleBackColor = true;
            this.IDB_STOP.Click += new System.EventHandler(this.IDB_STOP_Click);
            // 
            // IDT_OUTPUT
            // 
            this.IDT_OUTPUT.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.IDT_OUTPUT.Location = new System.Drawing.Point(12, 115);
            this.IDT_OUTPUT.Multiline = true;
            this.IDT_OUTPUT.Name = "IDT_OUTPUT";
            this.IDT_OUTPUT.ReadOnly = true;
            this.IDT_OUTPUT.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.IDT_OUTPUT.Size = new System.Drawing.Size(353, 151);
            this.IDT_OUTPUT.TabIndex = 5;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 99);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(83, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "Output Console:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 29);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 13);
            this.label2.TabIndex = 7;
            this.label2.Text = "Script File";
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.optionsToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(369, 24);
            this.menuStrip1.TabIndex = 8;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.toolStripSeparator1,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(35, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveWindowPositionToolStripMenuItem,
            this.restoreWindowSizeToolStripMenuItem});
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(56, 20);
            this.optionsToolStripMenuItem.Text = "&Options";
            this.optionsToolStripMenuItem.DropDownOpened += new System.EventHandler(this.optionsToolStripMenuItem_DropDownOpened);
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.openToolStripMenuItem.Size = new System.Drawing.Size(163, 22);
            this.openToolStripMenuItem.Text = "&Open...";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(160, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(163, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // saveWindowPositionToolStripMenuItem
            // 
            this.saveWindowPositionToolStripMenuItem.Name = "saveWindowPositionToolStripMenuItem";
            this.saveWindowPositionToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            this.saveWindowPositionToolStripMenuItem.Text = "Save Window Position";
            this.saveWindowPositionToolStripMenuItem.Click += new System.EventHandler(this.saveWindowPositionToolStripMenuItem_Click);
            // 
            // restoreWindowSizeToolStripMenuItem
            // 
            this.restoreWindowSizeToolStripMenuItem.Name = "restoreWindowSizeToolStripMenuItem";
            this.restoreWindowSizeToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            this.restoreWindowSizeToolStripMenuItem.Text = "Restore Window Size";
            this.restoreWindowSizeToolStripMenuItem.Click += new System.EventHandler(this.restoreWindowSizeToolStripMenuItem_Click);
            // 
            // LuaWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(369, 281);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.IDT_OUTPUT);
            this.Controls.Add(this.IDB_RUN);
            this.Controls.Add(this.IDB_STOP);
            this.Controls.Add(this.IDB_EDIT);
            this.Controls.Add(this.IDB_BROWSE);
            this.Controls.Add(this.IDT_SCRIPTFILE);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "LuaWindow";
            this.Text = "Lua Script";
            this.Load += new System.EventHandler(this.LuaWindow_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox IDT_SCRIPTFILE;
        private System.Windows.Forms.Button IDB_BROWSE;
        private System.Windows.Forms.Button IDB_EDIT;
        private System.Windows.Forms.Button IDB_RUN;
        private System.Windows.Forms.Button IDB_STOP;
        private System.Windows.Forms.TextBox IDT_OUTPUT;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveWindowPositionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem restoreWindowSizeToolStripMenuItem;
    }
}