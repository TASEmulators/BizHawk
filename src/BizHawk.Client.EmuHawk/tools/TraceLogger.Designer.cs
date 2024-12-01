using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
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
			this.components = new System.ComponentModel.Container();
			this.TracerBox = new System.Windows.Forms.GroupBox();
			this.TraceView = new BizHawk.Client.EmuHawk.InputRoll();
			this.TraceContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.CopyContextMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.SelectAllContextMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.ClearContextMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.menuStrip1 = new BizHawk.WinForms.Controls.MenuStripEx();
			this.FileSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.SaveLogMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.EditSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.CopyMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.SelectAllMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.ClearMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.OptionsSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.MaxLinesMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.SegmentSizeMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.OpenLogFile = new System.Windows.Forms.Button();
			this.BrowseBox = new System.Windows.Forms.Button();
			this.FileBox = new System.Windows.Forms.TextBox();
			this.ToFileRadio = new System.Windows.Forms.RadioButton();
			this.ToWindowRadio = new System.Windows.Forms.RadioButton();
			this.LoggingEnabled = new System.Windows.Forms.CheckBox();
			this.TracerBox.SuspendLayout();
			this.TraceContextMenu.SuspendLayout();
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
			this.TraceView.AllowColumnReorder = false;
			this.TraceView.AllowColumnResize = true;
			this.TraceView.AlwaysScroll = false;
			this.TraceView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.TraceView.CellHeightPadding = 0;
			this.TraceView.CellWidthPadding = 0;
			this.TraceView.ContextMenuStrip = this.TraceContextMenu;
			this.TraceView.Font = new System.Drawing.Font("Courier New", 8F);
			this.TraceView.FullRowSelect = true;
			this.TraceView.HorizontalOrientation = false;
			this.TraceView.LetKeysModifySelection = false;
			this.TraceView.Location = new System.Drawing.Point(8, 18);
			this.TraceView.Name = "TraceView";
			this.TraceView.RowCount = 0;
			this.TraceView.ScrollSpeed = 0;
			this.TraceView.SeekingCutoffInterval = 0;
			this.TraceView.Size = new System.Drawing.Size(603, 414);
			this.TraceView.TabIndex = 4;
			this.TraceView.TabStop = false;
			// 
			// TraceContextMenu
			// 
			this.TraceContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CopyContextMenu,
            this.SelectAllContextMenu,
            this.ClearContextMenu});
			this.TraceContextMenu.Name = "TraceContextMenu";
			this.TraceContextMenu.Size = new System.Drawing.Size(165, 70);
			// 
			// CopyContextMenu
			// 
			this.CopyContextMenu.ShortcutKeyDisplayString = "Ctrl+C";
			this.CopyContextMenu.Text = "&Copy";
			this.CopyContextMenu.Click += new System.EventHandler(this.CopyMenuItem_Click);
			// 
			// SelectAllContextMenu
			// 
			this.SelectAllContextMenu.ShortcutKeyDisplayString = "Ctrl+A";
			this.SelectAllContextMenu.Text = "Select &All";
			this.SelectAllContextMenu.Click += new System.EventHandler(this.SelectAllMenuItem_Click);
			// 
			// ClearContextMenu
			// 
			this.ClearContextMenu.Text = "Clear";
			this.ClearContextMenu.Click += new System.EventHandler(this.ClearMenuItem_Click);
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu,
            this.EditSubMenu,
            this.OptionsSubMenu});
			this.menuStrip1.TabIndex = 2;
			// 
			// FileSubMenu
			// 
			this.FileSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SaveLogMenuItem});
			this.FileSubMenu.Text = "&File";
			// 
			// SaveLogMenuItem
			// 
			this.SaveLogMenuItem.Text = "&Save Log";
			this.SaveLogMenuItem.Click += new System.EventHandler(this.SaveLogMenuItem_Click);
			// 
			// EditSubMenu
			// 
			this.EditSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CopyMenuItem,
            this.SelectAllMenuItem,
            this.ClearMenuItem});
			this.EditSubMenu.Text = "Edit";
			// 
			// CopyMenuItem
			// 
			this.CopyMenuItem.ShortcutKeyDisplayString = "";
			this.CopyMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
			this.CopyMenuItem.Text = "&Copy";
			this.CopyMenuItem.Click += new System.EventHandler(this.CopyMenuItem_Click);
			// 
			// SelectAllMenuItem
			// 
			this.SelectAllMenuItem.ShortcutKeyDisplayString = "";
			this.SelectAllMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
			this.SelectAllMenuItem.Text = "Select &All";
			this.SelectAllMenuItem.Click += new System.EventHandler(this.SelectAllMenuItem_Click);
			// 
			// ClearMenuItem
			// 
			this.ClearMenuItem.Text = "Clear";
			this.ClearMenuItem.Click += new System.EventHandler(this.ClearMenuItem_Click);
			// 
			// OptionsSubMenu
			// 
			this.OptionsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MaxLinesMenuItem,
            this.SegmentSizeMenuItem});
			this.OptionsSubMenu.Text = "&Settings";
			// 
			// MaxLinesMenuItem
			// 
			this.MaxLinesMenuItem.Text = "&Set Max Lines...";
			this.MaxLinesMenuItem.Click += new System.EventHandler(this.MaxLinesMenuItem_Click);
			// 
			// SegmentSizeMenuItem
			// 
			this.SegmentSizeMenuItem.Text = "Set Segment Size...";
			this.SegmentSizeMenuItem.Click += new System.EventHandler(this.SegmentSizeMenuItem_Click);
			// 
			// groupBox2
			// 
			this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox2.Controls.Add(this.OpenLogFile);
			this.groupBox2.Controls.Add(this.BrowseBox);
			this.groupBox2.Controls.Add(this.FileBox);
			this.groupBox2.Controls.Add(this.ToFileRadio);
			this.groupBox2.Controls.Add(this.ToWindowRadio);
			this.groupBox2.Controls.Add(this.LoggingEnabled);
			this.groupBox2.Location = new System.Drawing.Point(12, 477);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(620, 50);
			this.groupBox2.TabIndex = 3;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Control";
			// 
			// OpenLogFile
			// 
			this.OpenLogFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.OpenLogFile.Location = new System.Drawing.Point(551, 19);
			this.OpenLogFile.Name = "OpenLogFile";
			this.OpenLogFile.Size = new System.Drawing.Size(60, 23);
			this.OpenLogFile.TabIndex = 21;
			this.OpenLogFile.Text = "&Open";
			this.OpenLogFile.UseVisualStyleBackColor = true;
			this.OpenLogFile.Click += new System.EventHandler(this.OpenLogFile_Click);
			// 
			// BrowseBox
			// 
			this.BrowseBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.BrowseBox.Location = new System.Drawing.Point(470, 19);
			this.BrowseBox.Name = "BrowseBox";
			this.BrowseBox.Size = new System.Drawing.Size(54, 23);
			this.BrowseBox.TabIndex = 20;
			this.BrowseBox.Text = "&Browse";
			this.BrowseBox.UseVisualStyleBackColor = true;
			this.BrowseBox.Visible = false;
			this.BrowseBox.Click += new System.EventHandler(this.BrowseBox_Click);
			// 
			// FileBox
			// 
			this.FileBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.FileBox.Location = new System.Drawing.Point(223, 20);
			this.FileBox.Name = "FileBox";
			this.FileBox.ReadOnly = true;
			this.FileBox.Size = new System.Drawing.Size(242, 20);
			this.FileBox.TabIndex = 15;
			this.FileBox.TabStop = false;
			this.FileBox.Visible = false;
			// 
			// ToFileRadio
			// 
			this.ToFileRadio.AutoSize = true;
			this.ToFileRadio.Location = new System.Drawing.Point(170, 22);
			this.ToFileRadio.Name = "ToFileRadio";
			this.ToFileRadio.Size = new System.Drawing.Size(50, 17);
			this.ToFileRadio.TabIndex = 10;
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
			this.ToWindowRadio.TabIndex = 5;
			this.ToWindowRadio.TabStop = true;
			this.ToWindowRadio.Text = "to window";
			this.ToWindowRadio.UseVisualStyleBackColor = true;
			// 
			// LoggingEnabled
			// 
			this.LoggingEnabled.Appearance = System.Windows.Forms.Appearance.Button;
			this.LoggingEnabled.AutoSize = true;
			this.LoggingEnabled.Location = new System.Drawing.Point(9, 19);
			this.LoggingEnabled.Name = "LoggingEnabled";
			this.LoggingEnabled.Size = new System.Drawing.Size(55, 23);
			this.LoggingEnabled.TabIndex = 1;
			this.LoggingEnabled.Text = "Start &logging";
			this.LoggingEnabled.UseVisualStyleBackColor = true;
			this.LoggingEnabled.CheckedChanged += new System.EventHandler(this.LoggingEnabled_CheckedChanged);
			// 
			// TraceLogger
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(644, 539);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.TracerBox);
			this.Controls.Add(this.menuStrip1);
			this.KeyPreview = true;
			this.MainMenuStrip = this.menuStrip1;
			this.MinimumSize = new System.Drawing.Size(400, 230);
			this.Name = "TraceLogger";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Load += new System.EventHandler(this.TraceLogger_Load);
			this.TracerBox.ResumeLayout(false);
			this.TraceContextMenu.ResumeLayout(false);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.GroupBox TracerBox;
		private MenuStripEx menuStrip1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FileSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SaveLogMenuItem;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.CheckBox LoggingEnabled;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx OptionsSubMenu;
		private InputRoll TraceView;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx MaxLinesMenuItem;
		private System.Windows.Forms.RadioButton ToFileRadio;
		private System.Windows.Forms.RadioButton ToWindowRadio;
		private System.Windows.Forms.TextBox FileBox;
		private System.Windows.Forms.Button BrowseBox;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx EditSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CopyMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SelectAllMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ClearMenuItem;
		private System.Windows.Forms.ContextMenuStrip TraceContextMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CopyContextMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SelectAllContextMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ClearContextMenu;
		private System.Windows.Forms.Button OpenLogFile;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SegmentSizeMenuItem;
	}
}