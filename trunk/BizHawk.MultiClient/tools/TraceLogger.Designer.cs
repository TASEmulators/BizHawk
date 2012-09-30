namespace BizHawk.MultiClient
{
	partial class TraceLogger
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
			this.TraceView = new BizHawk.VirtualListView();
			this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveLogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.ClearButton = new System.Windows.Forms.Button();
			this.LoggingEnabled = new System.Windows.Forms.CheckBox();
			this.groupBox1.SuspendLayout();
			this.menuStrip1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.SuspendLayout();
			// 
			// TraceView
			// 
			this.TraceView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.TraceView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
			this.TraceView.FullRowSelect = true;
			this.TraceView.GridLines = true;
			this.TraceView.HideSelection = false;
			this.TraceView.ItemCount = 0;
			this.TraceView.Location = new System.Drawing.Point(9, 16);
			this.TraceView.Name = "TraceView";
			this.TraceView.selectedItem = -1;
			this.TraceView.Size = new System.Drawing.Size(320, 386);
			this.TraceView.TabIndex = 0;
			this.TraceView.UseCompatibleStateImageBehavior = false;
			this.TraceView.View = System.Windows.Forms.View.Details;
			this.TraceView.VirtualMode = true;
			// 
			// columnHeader1
			// 
			this.columnHeader1.Text = "Instructions";
			this.columnHeader1.Width = 316;
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add(this.TraceView);
			this.groupBox1.Location = new System.Drawing.Point(12, 27);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(337, 411);
			this.groupBox1.TabIndex = 1;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Trace log";
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1,
            this.fileToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(555, 24);
			this.menuStrip1.TabIndex = 2;
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
            this.saveLogToolStripMenuItem,
            this.toolStripSeparator1,
            this.exitToolStripMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this.fileToolStripMenuItem.Text = "&File";
			// 
			// saveLogToolStripMenuItem
			// 
			this.saveLogToolStripMenuItem.Enabled = false;
			this.saveLogToolStripMenuItem.Name = "saveLogToolStripMenuItem";
			this.saveLogToolStripMenuItem.Size = new System.Drawing.Size(134, 22);
			this.saveLogToolStripMenuItem.Text = "&Save Log";
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(131, 6);
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.ShortcutKeyDisplayString = "Alt+F4";
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(134, 22);
			this.exitToolStripMenuItem.Text = "E&xit";
			this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
			// 
			// groupBox2
			// 
			this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox2.Controls.Add(this.ClearButton);
			this.groupBox2.Controls.Add(this.LoggingEnabled);
			this.groupBox2.Location = new System.Drawing.Point(356, 27);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(187, 411);
			this.groupBox2.TabIndex = 3;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Control";
			// 
			// ClearButton
			// 
			this.ClearButton.Location = new System.Drawing.Point(15, 73);
			this.ClearButton.Name = "ClearButton";
			this.ClearButton.Size = new System.Drawing.Size(75, 23);
			this.ClearButton.TabIndex = 1;
			this.ClearButton.Text = "&Clear";
			this.ClearButton.UseVisualStyleBackColor = true;
			this.ClearButton.Click += new System.EventHandler(this.ClearButton_Click);
			// 
			// LoggingEnabled
			// 
			this.LoggingEnabled.Appearance = System.Windows.Forms.Appearance.Button;
			this.LoggingEnabled.AutoSize = true;
			this.LoggingEnabled.Location = new System.Drawing.Point(15, 31);
			this.LoggingEnabled.Name = "LoggingEnabled";
			this.LoggingEnabled.Size = new System.Drawing.Size(97, 23);
			this.LoggingEnabled.TabIndex = 0;
			this.LoggingEnabled.Text = "&Logging Enabled";
			this.LoggingEnabled.UseVisualStyleBackColor = true;
			this.LoggingEnabled.CheckedChanged += new System.EventHandler(this.LoggingEnabled_CheckedChanged);
			// 
			// TraceLogger
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(555, 450);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.menuStrip1);
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "TraceLogger";
			this.Text = "TraceLogger";
			this.Load += new System.EventHandler(this.TraceLogger_Load);
			this.groupBox1.ResumeLayout(false);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private VirtualListView TraceView;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveLogToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.CheckBox LoggingEnabled;
		private System.Windows.Forms.Button ClearButton;
		private System.Windows.Forms.ColumnHeader columnHeader1;
	}
}