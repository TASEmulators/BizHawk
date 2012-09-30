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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TraceLogger));
			this.TracerBox = new System.Windows.Forms.GroupBox();
			this.TraceView = new BizHawk.VirtualListView();
			this.Script = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveLogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.setMaxWindowLinesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.autoloadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveWindowPositionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.ToFileRadio = new System.Windows.Forms.RadioButton();
			this.ToWindowRadio = new System.Windows.Forms.RadioButton();
			this.ClearButton = new System.Windows.Forms.Button();
			this.LoggingEnabled = new System.Windows.Forms.CheckBox();
			this.CloseButton = new System.Windows.Forms.Button();
			this.TracerBox.SuspendLayout();
			this.menuStrip1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.SuspendLayout();
			// 
			// TracerBox
			// 
			this.TracerBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.TracerBox.Controls.Add(this.TraceView);
			this.TracerBox.Location = new System.Drawing.Point(12, 27);
			this.TracerBox.Name = "TracerBox";
			this.TracerBox.Size = new System.Drawing.Size(620, 444);
			this.TracerBox.TabIndex = 1;
			this.TracerBox.TabStop = false;
			this.TracerBox.Text = "Trace log";
			// 
			// TraceView
			// 
			this.TraceView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.TraceView.CheckBoxes = true;
			this.TraceView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Script});
			this.TraceView.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.TraceView.FullRowSelect = true;
			this.TraceView.GridLines = true;
			this.TraceView.HideSelection = false;
			this.TraceView.ItemCount = 0;
			this.TraceView.Location = new System.Drawing.Point(8, 18);
			this.TraceView.Name = "TraceView";
			this.TraceView.selectedItem = -1;
			this.TraceView.Size = new System.Drawing.Size(603, 414);
			this.TraceView.TabIndex = 4;
			this.TraceView.UseCompatibleStateImageBehavior = false;
			this.TraceView.View = System.Windows.Forms.View.Details;
			this.TraceView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TraceView_KeyDown);
			// 
			// Script
			// 
			this.Script.Text = "Instructions";
			this.Script.Width = 599;
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1,
            this.fileToolStripMenuItem,
            this.optionsToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(644, 24);
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
			// optionsToolStripMenuItem
			// 
			this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.setMaxWindowLinesToolStripMenuItem,
            this.toolStripSeparator2,
            this.autoloadToolStripMenuItem,
            this.saveWindowPositionToolStripMenuItem});
			this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
			this.optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
			this.optionsToolStripMenuItem.Text = "&Options";
			this.optionsToolStripMenuItem.DropDownOpened += new System.EventHandler(this.optionsToolStripMenuItem_DropDownOpened);
			// 
			// setMaxWindowLinesToolStripMenuItem
			// 
			this.setMaxWindowLinesToolStripMenuItem.Name = "setMaxWindowLinesToolStripMenuItem";
			this.setMaxWindowLinesToolStripMenuItem.Size = new System.Drawing.Size(201, 22);
			this.setMaxWindowLinesToolStripMenuItem.Text = "&Set Max Window Lines...";
			this.setMaxWindowLinesToolStripMenuItem.Click += new System.EventHandler(this.setMaxWindowLinesToolStripMenuItem_Click);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(198, 6);
			// 
			// autoloadToolStripMenuItem
			// 
			this.autoloadToolStripMenuItem.Name = "autoloadToolStripMenuItem";
			this.autoloadToolStripMenuItem.Size = new System.Drawing.Size(201, 22);
			this.autoloadToolStripMenuItem.Text = "&Autoload";
			this.autoloadToolStripMenuItem.Click += new System.EventHandler(this.autoloadToolStripMenuItem_Click);
			// 
			// saveWindowPositionToolStripMenuItem
			// 
			this.saveWindowPositionToolStripMenuItem.Name = "saveWindowPositionToolStripMenuItem";
			this.saveWindowPositionToolStripMenuItem.Size = new System.Drawing.Size(201, 22);
			this.saveWindowPositionToolStripMenuItem.Text = "&Save Window Position";
			this.saveWindowPositionToolStripMenuItem.Click += new System.EventHandler(this.saveWindowPositionToolStripMenuItem_Click);
			// 
			// groupBox2
			// 
			this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.groupBox2.Controls.Add(this.ToFileRadio);
			this.groupBox2.Controls.Add(this.ToWindowRadio);
			this.groupBox2.Controls.Add(this.ClearButton);
			this.groupBox2.Controls.Add(this.LoggingEnabled);
			this.groupBox2.Location = new System.Drawing.Point(12, 477);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(530, 50);
			this.groupBox2.TabIndex = 3;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Control";
			// 
			// ToFileRadio
			// 
			this.ToFileRadio.AutoSize = true;
			this.ToFileRadio.Location = new System.Drawing.Point(173, 22);
			this.ToFileRadio.Name = "ToFileRadio";
			this.ToFileRadio.Size = new System.Drawing.Size(50, 17);
			this.ToFileRadio.TabIndex = 3;
			this.ToFileRadio.Text = "to file";
			this.ToFileRadio.UseVisualStyleBackColor = true;
			this.ToFileRadio.CheckedChanged += new System.EventHandler(this.ToFileRadio_CheckedChanged);
			// 
			// ToWindowRadio
			// 
			this.ToWindowRadio.AutoSize = true;
			this.ToWindowRadio.Checked = true;
			this.ToWindowRadio.Location = new System.Drawing.Point(94, 22);
			this.ToWindowRadio.Name = "ToWindowRadio";
			this.ToWindowRadio.Size = new System.Drawing.Size(73, 17);
			this.ToWindowRadio.TabIndex = 2;
			this.ToWindowRadio.TabStop = true;
			this.ToWindowRadio.Text = "to window";
			this.ToWindowRadio.UseVisualStyleBackColor = true;
			// 
			// ClearButton
			// 
			this.ClearButton.Location = new System.Drawing.Point(449, 19);
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
			this.LoggingEnabled.Location = new System.Drawing.Point(9, 19);
			this.LoggingEnabled.Name = "LoggingEnabled";
			this.LoggingEnabled.Size = new System.Drawing.Size(55, 23);
			this.LoggingEnabled.TabIndex = 0;
			this.LoggingEnabled.Text = "&Logging";
			this.LoggingEnabled.UseVisualStyleBackColor = true;
			this.LoggingEnabled.CheckedChanged += new System.EventHandler(this.LoggingEnabled_CheckedChanged);
			// 
			// CloseButton
			// 
			this.CloseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.CloseButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CloseButton.Location = new System.Drawing.Point(548, 496);
			this.CloseButton.Name = "CloseButton";
			this.CloseButton.Size = new System.Drawing.Size(75, 23);
			this.CloseButton.TabIndex = 4;
			this.CloseButton.Text = "&Close";
			this.CloseButton.UseVisualStyleBackColor = true;
			this.CloseButton.Click += new System.EventHandler(this.CloseButton_Click);
			// 
			// TraceLogger
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.CloseButton;
			this.ClientSize = new System.Drawing.Size(644, 539);
			this.Controls.Add(this.CloseButton);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.TracerBox);
			this.Controls.Add(this.menuStrip1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "TraceLogger";
			this.Text = "TraceLogger";
			this.Load += new System.EventHandler(this.TraceLogger_Load);
			this.TracerBox.ResumeLayout(false);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.GroupBox TracerBox;
		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveLogToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.CheckBox LoggingEnabled;
		private System.Windows.Forms.Button ClearButton;
		private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem autoloadToolStripMenuItem;
		private VirtualListView TraceView;
		public System.Windows.Forms.ColumnHeader Script;
		private System.Windows.Forms.Button CloseButton;
		private System.Windows.Forms.ToolStripMenuItem setMaxWindowLinesToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripMenuItem saveWindowPositionToolStripMenuItem;
		private System.Windows.Forms.RadioButton ToFileRadio;
		private System.Windows.Forms.RadioButton ToWindowRadio;
	}
}