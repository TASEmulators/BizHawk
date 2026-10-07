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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TraceLogger));
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
            resources.ApplyResources(this.TracerBox, "TracerBox");
            this.TracerBox.Controls.Add(this.TraceView);
            this.TracerBox.Name = "TracerBox";
            this.TracerBox.TabStop = false;
            // 
            // TraceView
            // 
            resources.ApplyResources(this.TraceView, "TraceView");
            this.TraceView.AllowColumnReorder = false;
            this.TraceView.AllowColumnResize = true;
            this.TraceView.AlwaysScroll = false;
            this.TraceView.CellHeightPadding = 0;
            this.TraceView.CellWidthPadding = 0;
            this.TraceView.ContextMenuStrip = this.TraceContextMenu;
            this.TraceView.FullRowSelect = true;
            this.TraceView.HorizontalOrientation = false;
            this.TraceView.LetKeysModifySelection = false;
            this.TraceView.Name = "TraceView";
            this.TraceView.RowCount = 0;
            this.TraceView.ScrollSpeed = 3;
            this.TraceView.TabStop = false;
            // 
            // TraceContextMenu
            // 
            resources.ApplyResources(this.TraceContextMenu, "TraceContextMenu");
            this.TraceContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CopyContextMenu,
            this.SelectAllContextMenu,
            this.ClearContextMenu});
            this.TraceContextMenu.Name = "TraceContextMenu";
            // 
            // CopyContextMenu
            // 
            resources.ApplyResources(this.CopyContextMenu, "CopyContextMenu");
            this.CopyContextMenu.Click += new System.EventHandler(this.CopyMenuItem_Click);
            // 
            // SelectAllContextMenu
            // 
            resources.ApplyResources(this.SelectAllContextMenu, "SelectAllContextMenu");
            this.SelectAllContextMenu.Click += new System.EventHandler(this.SelectAllMenuItem_Click);
            // 
            // ClearContextMenu
            // 
            resources.ApplyResources(this.ClearContextMenu, "ClearContextMenu");
            this.ClearContextMenu.Click += new System.EventHandler(this.ClearMenuItem_Click);
            // 
            // menuStrip1
            // 
            resources.ApplyResources(this.menuStrip1, "menuStrip1");
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu,
            this.EditSubMenu,
            this.OptionsSubMenu});
            // 
            // FileSubMenu
            // 
            resources.ApplyResources(this.FileSubMenu, "FileSubMenu");
            this.FileSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SaveLogMenuItem});
            // 
            // SaveLogMenuItem
            // 
            resources.ApplyResources(this.SaveLogMenuItem, "SaveLogMenuItem");
            this.SaveLogMenuItem.Click += new System.EventHandler(this.SaveLogMenuItem_Click);
            // 
            // EditSubMenu
            // 
            resources.ApplyResources(this.EditSubMenu, "EditSubMenu");
            this.EditSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CopyMenuItem,
            this.SelectAllMenuItem,
            this.ClearMenuItem});
            // 
            // CopyMenuItem
            // 
            resources.ApplyResources(this.CopyMenuItem, "CopyMenuItem");
            this.CopyMenuItem.Click += new System.EventHandler(this.CopyMenuItem_Click);
            // 
            // SelectAllMenuItem
            // 
            resources.ApplyResources(this.SelectAllMenuItem, "SelectAllMenuItem");
            this.SelectAllMenuItem.Click += new System.EventHandler(this.SelectAllMenuItem_Click);
            // 
            // ClearMenuItem
            // 
            resources.ApplyResources(this.ClearMenuItem, "ClearMenuItem");
            this.ClearMenuItem.Click += new System.EventHandler(this.ClearMenuItem_Click);
            // 
            // OptionsSubMenu
            // 
            resources.ApplyResources(this.OptionsSubMenu, "OptionsSubMenu");
            this.OptionsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MaxLinesMenuItem,
            this.SegmentSizeMenuItem});
            // 
            // MaxLinesMenuItem
            // 
            resources.ApplyResources(this.MaxLinesMenuItem, "MaxLinesMenuItem");
            this.MaxLinesMenuItem.Click += new System.EventHandler(this.MaxLinesMenuItem_Click);
            // 
            // SegmentSizeMenuItem
            // 
            resources.ApplyResources(this.SegmentSizeMenuItem, "SegmentSizeMenuItem");
            this.SegmentSizeMenuItem.Click += new System.EventHandler(this.SegmentSizeMenuItem_Click);
            // 
            // groupBox2
            // 
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Controls.Add(this.OpenLogFile);
            this.groupBox2.Controls.Add(this.BrowseBox);
            this.groupBox2.Controls.Add(this.FileBox);
            this.groupBox2.Controls.Add(this.ToFileRadio);
            this.groupBox2.Controls.Add(this.ToWindowRadio);
            this.groupBox2.Controls.Add(this.LoggingEnabled);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // OpenLogFile
            // 
            resources.ApplyResources(this.OpenLogFile, "OpenLogFile");
            this.OpenLogFile.Name = "OpenLogFile";
            this.OpenLogFile.UseVisualStyleBackColor = true;
            this.OpenLogFile.Click += new System.EventHandler(this.OpenLogFile_Click);
            // 
            // BrowseBox
            // 
            resources.ApplyResources(this.BrowseBox, "BrowseBox");
            this.BrowseBox.Name = "BrowseBox";
            this.BrowseBox.UseVisualStyleBackColor = true;
            this.BrowseBox.Click += new System.EventHandler(this.BrowseBox_Click);
            // 
            // FileBox
            // 
            resources.ApplyResources(this.FileBox, "FileBox");
            this.FileBox.Name = "FileBox";
            this.FileBox.ReadOnly = true;
            this.FileBox.TabStop = false;
            // 
            // ToFileRadio
            // 
            resources.ApplyResources(this.ToFileRadio, "ToFileRadio");
            this.ToFileRadio.Name = "ToFileRadio";
            this.ToFileRadio.UseVisualStyleBackColor = true;
            this.ToFileRadio.CheckedChanged += new System.EventHandler(this.ToFileRadio_CheckedChanged);
            // 
            // ToWindowRadio
            // 
            resources.ApplyResources(this.ToWindowRadio, "ToWindowRadio");
            this.ToWindowRadio.Checked = true;
            this.ToWindowRadio.Name = "ToWindowRadio";
            this.ToWindowRadio.TabStop = true;
            this.ToWindowRadio.UseVisualStyleBackColor = true;
            // 
            // LoggingEnabled
            // 
            resources.ApplyResources(this.LoggingEnabled, "LoggingEnabled");
            this.LoggingEnabled.Name = "LoggingEnabled";
            this.LoggingEnabled.UseVisualStyleBackColor = true;
            this.LoggingEnabled.CheckedChanged += new System.EventHandler(this.LoggingEnabled_CheckedChanged);
            // 
            // TraceLogger
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.TracerBox);
            this.Controls.Add(this.menuStrip1);
            this.KeyPreview = true;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "TraceLogger";
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