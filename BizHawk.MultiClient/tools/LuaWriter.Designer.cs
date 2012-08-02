namespace BizHawk.MultiClient
{
    partial class LuaWriter
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LuaWriter));
            this.timer = new System.Windows.Forms.Timer(this.components);
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.configToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fontToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.syntaxHighlightingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.restoreSettingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.MessageLabel = new System.Windows.Forms.Label();
            this.AutoCompleteView = new System.Windows.Forms.ListView();
            this.PositionLabel = new System.Windows.Forms.Label();
            this.ZoomLabel = new System.Windows.Forms.Label();
            this.LuaText = new BizHawk.MultiClient.LuaWriterBox();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // timer
            // 
            this.timer.Enabled = true;
            this.timer.Interval = 1000;
            this.timer.Tick += new System.EventHandler(this.timer_Tick);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.configToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(474, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.toolStripSeparator1,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
            this.saveToolStripMenuItem.Text = "&Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
                        | System.Windows.Forms.Keys.S)));
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
            this.saveAsToolStripMenuItem.Text = "Save &As...";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.saveAsToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(192, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.ShortcutKeyDisplayString = "Alt+F4";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // configToolStripMenuItem
            // 
            this.configToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fontToolStripMenuItem,
            this.syntaxHighlightingToolStripMenuItem,
            this.restoreSettingsToolStripMenuItem});
            this.configToolStripMenuItem.Name = "configToolStripMenuItem";
            this.configToolStripMenuItem.Size = new System.Drawing.Size(55, 20);
            this.configToolStripMenuItem.Text = "&Config";
            // 
            // fontToolStripMenuItem
            // 
            this.fontToolStripMenuItem.Name = "fontToolStripMenuItem";
            this.fontToolStripMenuItem.Size = new System.Drawing.Size(178, 22);
            this.fontToolStripMenuItem.Text = "&Font";
            this.fontToolStripMenuItem.Click += new System.EventHandler(this.fontToolStripMenuItem_Click);
            // 
            // syntaxHighlightingToolStripMenuItem
            // 
            this.syntaxHighlightingToolStripMenuItem.Name = "syntaxHighlightingToolStripMenuItem";
            this.syntaxHighlightingToolStripMenuItem.Size = new System.Drawing.Size(178, 22);
            this.syntaxHighlightingToolStripMenuItem.Text = "&Syntax Highlighting";
            this.syntaxHighlightingToolStripMenuItem.Click += new System.EventHandler(this.syntaxHighlightingToolStripMenuItem_Click);
            // 
            // restoreSettingsToolStripMenuItem
            // 
            this.restoreSettingsToolStripMenuItem.Name = "restoreSettingsToolStripMenuItem";
            this.restoreSettingsToolStripMenuItem.Size = new System.Drawing.Size(178, 22);
            this.restoreSettingsToolStripMenuItem.Text = "Restore Settings";
            this.restoreSettingsToolStripMenuItem.Click += new System.EventHandler(this.restoreSettingsToolStripMenuItem_Click);
            // 
            // MessageLabel
            // 
            this.MessageLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.MessageLabel.AutoSize = true;
            this.MessageLabel.Location = new System.Drawing.Point(15, 424);
            this.MessageLabel.Name = "MessageLabel";
            this.MessageLabel.Size = new System.Drawing.Size(91, 13);
            this.MessageLabel.TabIndex = 2;
            this.MessageLabel.Text = "                            ";
            // 
            // AutoCompleteView
            // 
            this.AutoCompleteView.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.AutoCompleteView.FullRowSelect = true;
            this.AutoCompleteView.Location = new System.Drawing.Point(324, 322);
            this.AutoCompleteView.Name = "AutoCompleteView";
            this.AutoCompleteView.Size = new System.Drawing.Size(121, 97);
            this.AutoCompleteView.TabIndex = 3;
            this.AutoCompleteView.UseCompatibleStateImageBehavior = false;
            this.AutoCompleteView.View = System.Windows.Forms.View.List;
            this.AutoCompleteView.Visible = false;
            this.AutoCompleteView.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.AutoCompleteView_MouseDoubleClick);
            // 
            // PositionLabel
            // 
            this.PositionLabel.AutoSize = true;
            this.PositionLabel.Location = new System.Drawing.Point(14, 30);
            this.PositionLabel.Name = "PositionLabel";
            this.PositionLabel.Size = new System.Drawing.Size(46, 13);
            this.PositionLabel.TabIndex = 4;
            this.PositionLabel.Text = "             ";
            // 
            // ZoomLabel
            // 
            this.ZoomLabel.AutoSize = true;
            this.ZoomLabel.Location = new System.Drawing.Point(393, 30);
            this.ZoomLabel.Name = "ZoomLabel";
            this.ZoomLabel.Size = new System.Drawing.Size(66, 13);
            this.ZoomLabel.TabIndex = 5;
            this.ZoomLabel.Text = "Zoom: 100%";
            // 
            // LuaText
            // 
            this.LuaText.AcceptsTab = true;
            this.LuaText.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.LuaText.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.LuaText.EnableAutoDragDrop = true;
            this.LuaText.Location = new System.Drawing.Point(15, 50);
            this.LuaText.Name = "LuaText";
            this.LuaText.Size = new System.Drawing.Size(444, 369);
            this.LuaText.TabIndex = 0;
            this.LuaText.Text = "";
            this.LuaText.WordWrap = false;
            this.LuaText.SelectionChanged += new System.EventHandler(this.LuaText_SelectionChanged);
            this.LuaText.TextChanged += new System.EventHandler(this.LuaText_TextChanged);
            this.LuaText.KeyDown += new System.Windows.Forms.KeyEventHandler(this.LuaText_KeyDown);
            this.LuaText.KeyUp += new System.Windows.Forms.KeyEventHandler(this.LuaText_KeyUp);
            this.LuaText.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.LuaText_PreviewKeyDown);
            // 
            // LuaWriter
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(474, 441);
            this.Controls.Add(this.AutoCompleteView);
            this.Controls.Add(this.ZoomLabel);
            this.Controls.Add(this.PositionLabel);
            this.Controls.Add(this.MessageLabel);
            this.Controls.Add(this.LuaText);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "LuaWriter";
            this.Text = "LuaWriter";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.LuaWriter_FormClosing);
            this.Load += new System.EventHandler(this.LuaWriter_Load);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.LuaWriter_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.LuaWriter_DragEnter);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private LuaWriterBox LuaText;
		private System.Windows.Forms.Timer timer;
		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
		private System.Windows.Forms.Label MessageLabel;
		private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem configToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem fontToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem syntaxHighlightingToolStripMenuItem;
		private System.Windows.Forms.ListView AutoCompleteView;
		private System.Windows.Forms.Label PositionLabel;
		private System.Windows.Forms.Label ZoomLabel;
		private System.Windows.Forms.ToolStripMenuItem restoreSettingsToolStripMenuItem;
    }
}