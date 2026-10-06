using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	partial class GenericDebugger
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GenericDebugger));
            this.menuStrip1 = new BizHawk.WinForms.Controls.MenuStripEx();
            this.fileToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.DebugSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.StepIntoMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.StepOverMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.StepOutMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator1 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.RefreshMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.RegistersGroupBox = new System.Windows.Forms.GroupBox();
            this.RegisterPanel = new BizHawk.Client.EmuHawk.RegisterBoxControl();
            this.BreakpointsGroupBox = new System.Windows.Forms.GroupBox();
            this.BreakPointControl1 = new BizHawk.Client.EmuHawk.BreakpointControl();
            this.DisassemblerBox = new System.Windows.Forms.GroupBox();
            this.ToPCBtn = new System.Windows.Forms.Button();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.DisassemblerView = new BizHawk.Client.EmuHawk.InputRoll();
            this.DisassemblerContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.AddBreakpointContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.StepOutBtn = new System.Windows.Forms.Button();
            this.StepIntoBtn = new System.Windows.Forms.Button();
            this.StepOverBtn = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.SeekToBtn = new System.Windows.Forms.Button();
            this.SeekToBox = new BizHawk.Client.EmuHawk.HexTextBox();
            this.CancelSeekBtn = new System.Windows.Forms.Button();
            this.RunBtn = new System.Windows.Forms.Button();
            this.menuStrip1.SuspendLayout();
            this.RegistersGroupBox.SuspendLayout();
            this.BreakpointsGroupBox.SuspendLayout();
            this.DisassemblerBox.SuspendLayout();
            this.DisassemblerContextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            resources.ApplyResources(this.menuStrip1, "menuStrip1");
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.DebugSubMenu});
            // 
            // fileToolStripMenuItem
            // 
            resources.ApplyResources(this.fileToolStripMenuItem, "fileToolStripMenuItem");
            // 
            // DebugSubMenu
            // 
            resources.ApplyResources(this.DebugSubMenu, "DebugSubMenu");
            this.DebugSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.StepIntoMenuItem,
            this.StepOverMenuItem,
            this.StepOutMenuItem,
            this.toolStripSeparator1,
            this.RefreshMenuItem});
            // 
            // StepIntoMenuItem
            // 
            resources.ApplyResources(this.StepIntoMenuItem, "StepIntoMenuItem");
            this.StepIntoMenuItem.Click += new System.EventHandler(this.StepIntoMenuItem_Click);
            // 
            // StepOverMenuItem
            // 
            resources.ApplyResources(this.StepOverMenuItem, "StepOverMenuItem");
            this.StepOverMenuItem.Click += new System.EventHandler(this.StepOverMenuItem_Click);
            // 
            // StepOutMenuItem
            // 
            resources.ApplyResources(this.StepOutMenuItem, "StepOutMenuItem");
            this.StepOutMenuItem.Click += new System.EventHandler(this.StepOutMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            resources.ApplyResources(this.toolStripSeparator1, "toolStripSeparator1");
            // 
            // RefreshMenuItem
            // 
            resources.ApplyResources(this.RefreshMenuItem, "RefreshMenuItem");
            this.RefreshMenuItem.Click += new System.EventHandler(this.RefreshMenuItem_Click);
            // 
            // RegistersGroupBox
            // 
            resources.ApplyResources(this.RegistersGroupBox, "RegistersGroupBox");
            this.RegistersGroupBox.Controls.Add(this.RegisterPanel);
            this.RegistersGroupBox.Name = "RegistersGroupBox";
            this.RegistersGroupBox.TabStop = false;
            // 
            // RegisterPanel
            // 
            resources.ApplyResources(this.RegisterPanel, "RegisterPanel");
            this.RegisterPanel.Core = null;
            this.RegisterPanel.Name = "RegisterPanel";
            this.RegisterPanel.ParentDebugger = null;
            // 
            // BreakpointsGroupBox
            // 
            resources.ApplyResources(this.BreakpointsGroupBox, "BreakpointsGroupBox");
            this.BreakpointsGroupBox.Controls.Add(this.BreakPointControl1);
            this.BreakpointsGroupBox.Name = "BreakpointsGroupBox";
            this.BreakpointsGroupBox.TabStop = false;
            // 
            // BreakPointControl1
            // 
            resources.ApplyResources(this.BreakPointControl1, "BreakPointControl1");
            this.BreakPointControl1.Core = null;
            this.BreakPointControl1.MainForm = null;
            this.BreakPointControl1.Mcs = null;
            this.BreakPointControl1.MemoryDomains = null;
            this.BreakPointControl1.Name = "BreakPointControl1";
            this.BreakPointControl1.ParentDebugger = null;
            // 
            // DisassemblerBox
            // 
            resources.ApplyResources(this.DisassemblerBox, "DisassemblerBox");
            this.DisassemblerBox.Controls.Add(this.ToPCBtn);
            this.DisassemblerBox.Controls.Add(this.label1);
            this.DisassemblerBox.Controls.Add(this.DisassemblerView);
            this.DisassemblerBox.Name = "DisassemblerBox";
            this.DisassemblerBox.TabStop = false;
            // 
            // ToPCBtn
            // 
            resources.ApplyResources(this.ToPCBtn, "ToPCBtn");
            this.ToPCBtn.Name = "ToPCBtn";
            this.ToPCBtn.UseVisualStyleBackColor = true;
            this.ToPCBtn.Click += new System.EventHandler(this.ToPCBtn_Click);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // DisassemblerView
            // 
            resources.ApplyResources(this.DisassemblerView, "DisassemblerView");
            this.DisassemblerView.AllowColumnReorder = false;
            this.DisassemblerView.AllowColumnResize = true;
            this.DisassemblerView.AlwaysScroll = false;
            this.DisassemblerView.CellHeightPadding = 0;
            this.DisassemblerView.ContextMenuStrip = this.DisassemblerContextMenu;
            this.DisassemblerView.FullRowSelect = true;
            this.DisassemblerView.HorizontalOrientation = false;
            this.DisassemblerView.LetKeysModifySelection = false;
            this.DisassemblerView.Name = "DisassemblerView";
            this.DisassemblerView.RowCount = 0;
            this.DisassemblerView.ScrollSpeed = 3;
            this.DisassemblerView.SizeChanged += new System.EventHandler(this.DisassemblerView_SizeChanged);
            this.DisassemblerView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.DisassemblerView_KeyDown);
            // 
            // DisassemblerContextMenu
            // 
            resources.ApplyResources(this.DisassemblerContextMenu, "DisassemblerContextMenu");
            this.DisassemblerContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.AddBreakpointContextMenuItem});
            this.DisassemblerContextMenu.Name = "DisassemblerContextMenu";
            this.DisassemblerContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.DisassemblerContextMenu_Opening);
            // 
            // AddBreakpointContextMenuItem
            // 
            resources.ApplyResources(this.AddBreakpointContextMenuItem, "AddBreakpointContextMenuItem");
            this.AddBreakpointContextMenuItem.Click += new System.EventHandler(this.AddBreakpointContextMenuItem_Click);
            // 
            // StepOutBtn
            // 
            resources.ApplyResources(this.StepOutBtn, "StepOutBtn");
            this.StepOutBtn.Name = "StepOutBtn";
            this.StepOutBtn.UseVisualStyleBackColor = true;
            this.StepOutBtn.Click += new System.EventHandler(this.StepOutMenuItem_Click);
            // 
            // StepIntoBtn
            // 
            resources.ApplyResources(this.StepIntoBtn, "StepIntoBtn");
            this.StepIntoBtn.Name = "StepIntoBtn";
            this.StepIntoBtn.UseVisualStyleBackColor = true;
            this.StepIntoBtn.Click += new System.EventHandler(this.StepIntoMenuItem_Click);
            // 
            // StepOverBtn
            // 
            resources.ApplyResources(this.StepOverBtn, "StepOverBtn");
            this.StepOverBtn.Name = "StepOverBtn";
            this.StepOverBtn.UseVisualStyleBackColor = true;
            this.StepOverBtn.Click += new System.EventHandler(this.StepOverMenuItem_Click);
            // 
            // SeekToBtn
            // 
            resources.ApplyResources(this.SeekToBtn, "SeekToBtn");
            this.SeekToBtn.Name = "SeekToBtn";
            this.toolTip1.SetToolTip(this.SeekToBtn, resources.GetString("SeekToBtn.ToolTip"));
            this.SeekToBtn.UseVisualStyleBackColor = true;
            this.SeekToBtn.Click += new System.EventHandler(this.SeekToBtn_Click);
            // 
            // SeekToBox
            // 
            resources.ApplyResources(this.SeekToBox, "SeekToBox");
            this.SeekToBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.SeekToBox.Name = "SeekToBox";
            this.SeekToBox.Nullable = false;
            this.toolTip1.SetToolTip(this.SeekToBox, resources.GetString("SeekToBox.ToolTip"));
            // 
            // CancelSeekBtn
            // 
            resources.ApplyResources(this.CancelSeekBtn, "CancelSeekBtn");
            this.CancelSeekBtn.Name = "CancelSeekBtn";
            this.toolTip1.SetToolTip(this.CancelSeekBtn, resources.GetString("CancelSeekBtn.ToolTip"));
            this.CancelSeekBtn.UseVisualStyleBackColor = true;
            this.CancelSeekBtn.Click += new System.EventHandler(this.CancelSeekBtn_Click);
            // 
            // RunBtn
            // 
            resources.ApplyResources(this.RunBtn, "RunBtn");
            this.RunBtn.Name = "RunBtn";
            this.toolTip1.SetToolTip(this.RunBtn, resources.GetString("RunBtn.ToolTip"));
            this.RunBtn.UseVisualStyleBackColor = true;
            this.RunBtn.Click += new System.EventHandler(this.RunBtn_Click);
            // 
            // GenericDebugger
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.RunBtn);
            this.Controls.Add(this.CancelSeekBtn);
            this.Controls.Add(this.SeekToBox);
            this.Controls.Add(this.SeekToBtn);
            this.Controls.Add(this.StepOverBtn);
            this.Controls.Add(this.StepIntoBtn);
            this.Controls.Add(this.StepOutBtn);
            this.Controls.Add(this.BreakpointsGroupBox);
            this.Controls.Add(this.RegistersGroupBox);
            this.Controls.Add(this.DisassemblerBox);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "GenericDebugger";
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.GenericDebugger_MouseMove);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.RegistersGroupBox.ResumeLayout(false);
            this.BreakpointsGroupBox.ResumeLayout(false);
            this.DisassemblerBox.ResumeLayout(false);
            this.DisassemblerBox.PerformLayout();
            this.DisassemblerContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private MenuStripEx menuStrip1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx fileToolStripMenuItem;
		private System.Windows.Forms.GroupBox RegistersGroupBox;
		private RegisterBoxControl RegisterPanel;
		private System.Windows.Forms.GroupBox BreakpointsGroupBox;
		private BreakpointControl BreakPointControl1;
		private System.Windows.Forms.GroupBox DisassemblerBox;
		private InputRoll DisassemblerView;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private System.Windows.Forms.Button StepOutBtn;
		private System.Windows.Forms.Button StepIntoBtn;
		private System.Windows.Forms.Button StepOverBtn;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DebugSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx StepIntoMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx StepOverMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx StepOutMenuItem;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.Button SeekToBtn;
		private HexTextBox SeekToBox;
		private System.Windows.Forms.Button CancelSeekBtn;
		private System.Windows.Forms.Button ToPCBtn;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RefreshMenuItem;
		private System.Windows.Forms.ContextMenuStrip DisassemblerContextMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx AddBreakpointContextMenuItem;
		private System.Windows.Forms.Button RunBtn;
	}
}