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
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.DebugSubMenu});
			this.menuStrip1.TabIndex = 1;
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.Text = "&File";
			// 
			// DebugSubMenu
			// 
			this.DebugSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.StepIntoMenuItem,
            this.StepOverMenuItem,
            this.StepOutMenuItem,
            this.toolStripSeparator1,
            this.RefreshMenuItem});
			this.DebugSubMenu.Text = "&Debug";
			// 
			// StepIntoMenuItem
			// 
			this.StepIntoMenuItem.Enabled = false;
			this.StepIntoMenuItem.ShortcutKeyDisplayString = "F11";
			this.StepIntoMenuItem.Text = "Step &Into";
			this.StepIntoMenuItem.Click += new System.EventHandler(this.StepIntoMenuItem_Click);
			// 
			// StepOverMenuItem
			// 
			this.StepOverMenuItem.Enabled = false;
			this.StepOverMenuItem.ShortcutKeyDisplayString = "F10";
			this.StepOverMenuItem.Text = "Step O&ver";
			this.StepOverMenuItem.Click += new System.EventHandler(this.StepOverMenuItem_Click);
			// 
			// StepOutMenuItem
			// 
			this.StepOutMenuItem.Enabled = false;
			this.StepOutMenuItem.ShortcutKeyDisplayString = "Shift+F11";
			this.StepOutMenuItem.Text = "Step Ou&t";
			this.StepOutMenuItem.Click += new System.EventHandler(this.StepOutMenuItem_Click);
			// 
			// RefreshMenuItem
			// 
			this.RefreshMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
			this.RefreshMenuItem.Text = "Refresh";
			this.RefreshMenuItem.Click += new System.EventHandler(this.RefreshMenuItem_Click);
			// 
			// RegistersGroupBox
			// 
			this.RegistersGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.RegistersGroupBox.Controls.Add(this.RegisterPanel);
			this.RegistersGroupBox.Location = new System.Drawing.Point(425, 27);
			this.RegistersGroupBox.Name = "RegistersGroupBox";
			this.RegistersGroupBox.Size = new System.Drawing.Size(330, 234);
			this.RegistersGroupBox.TabIndex = 8;
			this.RegistersGroupBox.TabStop = false;
			this.RegistersGroupBox.Text = "Registers";
			// 
			// RegisterPanel
			// 
			this.RegisterPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.RegisterPanel.AutoScroll = true;
			this.RegisterPanel.Core = null;
			this.RegisterPanel.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.RegisterPanel.Location = new System.Drawing.Point(8, 19);
			this.RegisterPanel.Name = "RegisterPanel";
			this.RegisterPanel.ParentDebugger = null;
			this.RegisterPanel.Size = new System.Drawing.Size(316, 209);
			this.RegisterPanel.TabIndex = 0;
			// 
			// BreakpointsGroupBox
			// 
			this.BreakpointsGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.BreakpointsGroupBox.Controls.Add(this.BreakPointControl1);
			this.BreakpointsGroupBox.Location = new System.Drawing.Point(425, 267);
			this.BreakpointsGroupBox.Name = "BreakpointsGroupBox";
			this.BreakpointsGroupBox.Size = new System.Drawing.Size(239, 281);
			this.BreakpointsGroupBox.TabIndex = 9;
			this.BreakpointsGroupBox.TabStop = false;
			this.BreakpointsGroupBox.Text = "Breakpoints";
			// 
			// BreakPointControl1
			// 
			this.BreakPointControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.BreakPointControl1.Core = null;
			this.BreakPointControl1.Location = new System.Drawing.Point(8, 19);
			this.BreakPointControl1.MainForm = null;
			this.BreakPointControl1.Mcs = null;
			this.BreakPointControl1.MemoryDomains = null;
			this.BreakPointControl1.Name = "BreakPointControl1";
			this.BreakPointControl1.ParentDebugger = null;
			this.BreakPointControl1.Size = new System.Drawing.Size(225, 256);
			this.BreakPointControl1.TabIndex = 0;
			// 
			// DisassemblerBox
			// 
			this.DisassemblerBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.DisassemblerBox.Controls.Add(this.ToPCBtn);
			this.DisassemblerBox.Controls.Add(this.label1);
			this.DisassemblerBox.Controls.Add(this.DisassemblerView);
			this.DisassemblerBox.Location = new System.Drawing.Point(12, 27);
			this.DisassemblerBox.Name = "DisassemblerBox";
			this.DisassemblerBox.Size = new System.Drawing.Size(407, 521);
			this.DisassemblerBox.TabIndex = 7;
			this.DisassemblerBox.TabStop = false;
			this.DisassemblerBox.Text = "Disassembler";
			// 
			// ToPCBtn
			// 
			this.ToPCBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.ToPCBtn.Location = new System.Drawing.Point(351, 13);
			this.ToPCBtn.Name = "ToPCBtn";
			this.ToPCBtn.Size = new System.Drawing.Size(50, 23);
			this.ToPCBtn.TabIndex = 3;
			this.ToPCBtn.Text = "To PC";
			this.ToPCBtn.UseVisualStyleBackColor = true;
			this.ToPCBtn.Click += new System.EventHandler(this.ToPCBtn_Click);
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(6, 51);
			this.label1.Name = "label1";
			this.label1.Text = "Cpu:  Use \'[\' or \']\' to change address, hold \'shift\' for speed.";
			// 
			// DisassemblerView
			// 
			this.DisassemblerView.AllowColumnReorder = false;
			this.DisassemblerView.AllowColumnResize = true;
			this.DisassemblerView.AllowMassNavigationShortcuts = true;
			this.DisassemblerView.AllowRightClickSelection = true;
			this.DisassemblerView.AlwaysScroll = false;
			this.DisassemblerView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.DisassemblerView.CellHeightPadding = 0;
			this.DisassemblerView.ContextMenuStrip = this.DisassemblerContextMenu;
			this.DisassemblerView.Font = new System.Drawing.Font("Courier New", 8F);
			this.DisassemblerView.FullRowSelect = true;
			this.DisassemblerView.HorizontalOrientation = false;
			this.DisassemblerView.LetKeysModifySelection = false;
			this.DisassemblerView.Location = new System.Drawing.Point(6, 67);
			this.DisassemblerView.Name = "DisassemblerView";
			this.DisassemblerView.RowCount = 0;
			this.DisassemblerView.ScrollSpeed = 0;
			this.DisassemblerView.SeekingCutoffInterval = 0;
			this.DisassemblerView.Size = new System.Drawing.Size(395, 448);
			this.DisassemblerView.TabIndex = 1;
			this.DisassemblerView.SizeChanged += new System.EventHandler(this.DisassemblerView_SizeChanged);
			this.DisassemblerView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.DisassemblerView_KeyDown);
			// 
			// DisassemblerContextMenu
			// 
			this.DisassemblerContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.AddBreakpointContextMenuItem});
			this.DisassemblerContextMenu.Name = "DisassemblerContextMenu";
			this.DisassemblerContextMenu.Size = new System.Drawing.Size(157, 26);
			this.DisassemblerContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.DisassemblerContextMenu_Opening);
			// 
			// AddBreakpointContextMenuItem
			// 
			this.AddBreakpointContextMenuItem.Text = "Add Breakpoint";
			this.AddBreakpointContextMenuItem.Click += new System.EventHandler(this.AddBreakpointContextMenuItem_Click);
			// 
			// StepOutBtn
			// 
			this.StepOutBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.StepOutBtn.Enabled = false;
			this.StepOutBtn.Location = new System.Drawing.Point(680, 519);
			this.StepOutBtn.Name = "StepOutBtn";
			this.StepOutBtn.Size = new System.Drawing.Size(75, 23);
			this.StepOutBtn.TabIndex = 10;
			this.StepOutBtn.Text = "Step Out";
			this.StepOutBtn.UseVisualStyleBackColor = true;
			this.StepOutBtn.Click += new System.EventHandler(this.StepOutMenuItem_Click);
			// 
			// StepIntoBtn
			// 
			this.StepIntoBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.StepIntoBtn.Enabled = false;
			this.StepIntoBtn.Location = new System.Drawing.Point(680, 461);
			this.StepIntoBtn.Name = "StepIntoBtn";
			this.StepIntoBtn.Size = new System.Drawing.Size(75, 23);
			this.StepIntoBtn.TabIndex = 11;
			this.StepIntoBtn.Text = "Step Into";
			this.StepIntoBtn.UseVisualStyleBackColor = true;
			this.StepIntoBtn.Click += new System.EventHandler(this.StepIntoMenuItem_Click);
			// 
			// StepOverBtn
			// 
			this.StepOverBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.StepOverBtn.Enabled = false;
			this.StepOverBtn.Location = new System.Drawing.Point(680, 490);
			this.StepOverBtn.Name = "StepOverBtn";
			this.StepOverBtn.Size = new System.Drawing.Size(75, 23);
			this.StepOverBtn.TabIndex = 12;
			this.StepOverBtn.Text = "Step Over";
			this.StepOverBtn.UseVisualStyleBackColor = true;
			this.StepOverBtn.Click += new System.EventHandler(this.StepOverMenuItem_Click);
			// 
			// SeekToBtn
			// 
			this.SeekToBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.SeekToBtn.Location = new System.Drawing.Point(680, 267);
			this.SeekToBtn.Name = "SeekToBtn";
			this.SeekToBtn.Size = new System.Drawing.Size(75, 23);
			this.SeekToBtn.TabIndex = 13;
			this.SeekToBtn.Text = "Seek To:";
			this.SeekToBtn.UseVisualStyleBackColor = true;
			this.SeekToBtn.Click += new System.EventHandler(this.SeekToBtn_Click);
			// 
			// SeekToBox
			// 
			this.SeekToBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.SeekToBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.SeekToBox.Location = new System.Drawing.Point(680, 297);
			this.SeekToBox.Name = "SeekToBox";
			this.SeekToBox.Nullable = false;
			this.SeekToBox.Size = new System.Drawing.Size(75, 20);
			this.SeekToBox.TabIndex = 14;
			// 
			// CancelSeekBtn
			// 
			this.CancelSeekBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.CancelSeekBtn.Location = new System.Drawing.Point(680, 323);
			this.CancelSeekBtn.Name = "CancelSeekBtn";
			this.CancelSeekBtn.Size = new System.Drawing.Size(75, 23);
			this.CancelSeekBtn.TabIndex = 15;
			this.CancelSeekBtn.Text = "Cancel";
			this.CancelSeekBtn.UseVisualStyleBackColor = true;
			this.CancelSeekBtn.Click += new System.EventHandler(this.CancelSeekBtn_Click);
			// 
			// RunBtn
			// 
			this.RunBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.RunBtn.Location = new System.Drawing.Point(680, 432);
			this.RunBtn.Name = "RunBtn";
			this.RunBtn.Size = new System.Drawing.Size(75, 23);
			this.RunBtn.TabIndex = 16;
			this.RunBtn.Text = "Run";
			this.RunBtn.UseVisualStyleBackColor = true;
			this.RunBtn.Click += new System.EventHandler(this.RunBtn_Click);
			// 
			// GenericDebugger
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(767, 560);
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
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
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