namespace BizHawk.MultiClient
{
    partial class HexEditor
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
            this.MemoryViewer = new System.Windows.Forms.GroupBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dumpToFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.memoryDomainsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dataSizeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.byteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.byteToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.byteToolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.restoreWindowSizeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.autoloadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // MemoryViewer
            // 
            this.MemoryViewer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.MemoryViewer.Location = new System.Drawing.Point(12, 37);
            this.MemoryViewer.Name = "MemoryViewer";
            this.MemoryViewer.Size = new System.Drawing.Size(484, 328);
            this.MemoryViewer.TabIndex = 0;
            this.MemoryViewer.TabStop = false;
            this.MemoryViewer.Text = "RAM";
            this.MemoryViewer.Paint += new System.Windows.Forms.PaintEventHandler(this.MemoryViewer_Paint);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.optionsToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(508, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.dumpToFileToolStripMenuItem,
            this.toolStripSeparator1,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(35, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // dumpToFileToolStripMenuItem
            // 
            this.dumpToFileToolStripMenuItem.Name = "dumpToFileToolStripMenuItem";
            this.dumpToFileToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.dumpToFileToolStripMenuItem.Text = "&Dump to file...";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(151, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.memoryDomainsToolStripMenuItem,
            this.dataSizeToolStripMenuItem,
            this.toolStripSeparator2,
            this.restoreWindowSizeToolStripMenuItem,
            this.autoloadToolStripMenuItem});
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(56, 20);
            this.optionsToolStripMenuItem.Text = "&Options";
            this.optionsToolStripMenuItem.DropDownOpened += new System.EventHandler(this.optionsToolStripMenuItem_DropDownOpened);
            // 
            // memoryDomainsToolStripMenuItem
            // 
            this.memoryDomainsToolStripMenuItem.Name = "memoryDomainsToolStripMenuItem";
            this.memoryDomainsToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
            this.memoryDomainsToolStripMenuItem.Text = "&Memory Domains";
            // 
            // dataSizeToolStripMenuItem
            // 
            this.dataSizeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.byteToolStripMenuItem,
            this.byteToolStripMenuItem1,
            this.byteToolStripMenuItem2});
            this.dataSizeToolStripMenuItem.Name = "dataSizeToolStripMenuItem";
            this.dataSizeToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
            this.dataSizeToolStripMenuItem.Text = "Data Size";
            // 
            // byteToolStripMenuItem
            // 
            this.byteToolStripMenuItem.Name = "byteToolStripMenuItem";
            this.byteToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.byteToolStripMenuItem.Text = "1 Byte";
            // 
            // byteToolStripMenuItem1
            // 
            this.byteToolStripMenuItem1.Name = "byteToolStripMenuItem1";
            this.byteToolStripMenuItem1.Size = new System.Drawing.Size(152, 22);
            this.byteToolStripMenuItem1.Text = "2 Byte";
            // 
            // byteToolStripMenuItem2
            // 
            this.byteToolStripMenuItem2.Name = "byteToolStripMenuItem2";
            this.byteToolStripMenuItem2.Size = new System.Drawing.Size(152, 22);
            this.byteToolStripMenuItem2.Text = "4 Byte";
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(183, 6);
            // 
            // restoreWindowSizeToolStripMenuItem
            // 
            this.restoreWindowSizeToolStripMenuItem.Name = "restoreWindowSizeToolStripMenuItem";
            this.restoreWindowSizeToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            this.restoreWindowSizeToolStripMenuItem.Text = "&Restore Window Size";
            this.restoreWindowSizeToolStripMenuItem.Click += new System.EventHandler(this.restoreWindowSizeToolStripMenuItem_Click);
            // 
            // autoloadToolStripMenuItem
            // 
            this.autoloadToolStripMenuItem.Name = "autoloadToolStripMenuItem";
            this.autoloadToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            this.autoloadToolStripMenuItem.Text = "Auto-load";
            this.autoloadToolStripMenuItem.Click += new System.EventHandler(this.autoloadToolStripMenuItem_Click);
            // 
            // HexEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(508, 377);
            this.Controls.Add(this.MemoryViewer);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "HexEditor";
            this.Text = "HexEditor";
            this.Load += new System.EventHandler(this.HexEditor_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox MemoryViewer;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dumpToFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem memoryDomainsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dataSizeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem byteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem byteToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem byteToolStripMenuItem2;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem restoreWindowSizeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem autoloadToolStripMenuItem;
    }
}