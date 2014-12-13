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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GenericDebugger));
			this.menuStrip1 = new MenuStripEx();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ExitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.OptionsSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.AutoloadMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SaveWindowPositionMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.AlwaysOnTopMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FloatingWindowMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.RestoreDefaultsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.RegistersGroupBox = new System.Windows.Forms.GroupBox();
			this.RegisterPanel = new BizHawk.Client.EmuHawk.RegisterBoxControl();
			this.BreakpointsGroupBox = new System.Windows.Forms.GroupBox();
			this.BreakPointControl1 = new BizHawk.Client.EmuHawk.tools.Debugger.BreakpointControl();
			this.DisassemblerBox = new System.Windows.Forms.GroupBox();
			this.menuStrip1.SuspendLayout();
			this.RegistersGroupBox.SuspendLayout();
			this.BreakpointsGroupBox.SuspendLayout();
			this.SuspendLayout();
			// 
			// menuStrip1
			// 
			this.menuStrip1.ClickThrough = true;
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.OptionsSubMenu});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(767, 24);
			this.menuStrip1.TabIndex = 1;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ExitMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this.fileToolStripMenuItem.Text = "&File";
			// 
			// ExitMenuItem
			// 
			this.ExitMenuItem.Name = "ExitMenuItem";
			this.ExitMenuItem.ShortcutKeyDisplayString = "Alt+F4";
			this.ExitMenuItem.Size = new System.Drawing.Size(145, 22);
			this.ExitMenuItem.Text = "&Close";
			this.ExitMenuItem.Click += new System.EventHandler(this.ExitMenuItem_Click);
			// 
			// OptionsSubMenu
			// 
			this.OptionsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.AutoloadMenuItem,
            this.SaveWindowPositionMenuItem,
            this.AlwaysOnTopMenuItem,
            this.FloatingWindowMenuItem,
            this.toolStripSeparator1,
            this.RestoreDefaultsMenuItem});
			this.OptionsSubMenu.Name = "OptionsSubMenu";
			this.OptionsSubMenu.Size = new System.Drawing.Size(61, 20);
			this.OptionsSubMenu.Text = "&Options";
			this.OptionsSubMenu.DropDownOpened += new System.EventHandler(this.OptionsSubMenu_DropDownOpened);
			// 
			// AutoloadMenuItem
			// 
			this.AutoloadMenuItem.Name = "AutoloadMenuItem";
			this.AutoloadMenuItem.Size = new System.Drawing.Size(191, 22);
			this.AutoloadMenuItem.Text = "Autoload";
			this.AutoloadMenuItem.Click += new System.EventHandler(this.AutoloadMenuItem_Click);
			// 
			// SaveWindowPositionMenuItem
			// 
			this.SaveWindowPositionMenuItem.Name = "SaveWindowPositionMenuItem";
			this.SaveWindowPositionMenuItem.Size = new System.Drawing.Size(191, 22);
			this.SaveWindowPositionMenuItem.Text = "Save Window Position";
			this.SaveWindowPositionMenuItem.Click += new System.EventHandler(this.SaveWindowPositionMenuItem_Click);
			// 
			// AlwaysOnTopMenuItem
			// 
			this.AlwaysOnTopMenuItem.Name = "AlwaysOnTopMenuItem";
			this.AlwaysOnTopMenuItem.Size = new System.Drawing.Size(191, 22);
			this.AlwaysOnTopMenuItem.Text = "Always On Top";
			this.AlwaysOnTopMenuItem.Click += new System.EventHandler(this.AlwaysOnTopMenuItem_Click);
			// 
			// FloatingWindowMenuItem
			// 
			this.FloatingWindowMenuItem.Name = "FloatingWindowMenuItem";
			this.FloatingWindowMenuItem.Size = new System.Drawing.Size(191, 22);
			this.FloatingWindowMenuItem.Text = "Floating Window";
			this.FloatingWindowMenuItem.Click += new System.EventHandler(this.FloatingWindowMenuItem_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(188, 6);
			// 
			// RestoreDefaultsMenuItem
			// 
			this.RestoreDefaultsMenuItem.Name = "RestoreDefaultsMenuItem";
			this.RestoreDefaultsMenuItem.Size = new System.Drawing.Size(191, 22);
			this.RestoreDefaultsMenuItem.Text = "Restore Defaults";
			this.RestoreDefaultsMenuItem.Click += new System.EventHandler(this.RestoreDefaultsMenuItem_Click);
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
			this.BreakPointControl1.Name = "BreakPointControl1";
			this.BreakPointControl1.ParentDebugger = null;
			this.BreakPointControl1.Size = new System.Drawing.Size(225, 256);
			this.BreakPointControl1.TabIndex = 0;
			// 
			// DisassemblerBox
			// 
			this.DisassemblerBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.DisassemblerBox.Location = new System.Drawing.Point(12, 27);
			this.DisassemblerBox.Name = "DisassemblerBox";
			this.DisassemblerBox.Size = new System.Drawing.Size(407, 521);
			this.DisassemblerBox.TabIndex = 7;
			this.DisassemblerBox.TabStop = false;
			this.DisassemblerBox.Text = "Disassembler";
			// 
			// GenericDebugger
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(767, 560);
			this.Controls.Add(this.BreakpointsGroupBox);
			this.Controls.Add(this.RegistersGroupBox);
			this.Controls.Add(this.DisassemblerBox);
			this.Controls.Add(this.menuStrip1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "GenericDebugger";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Debugger";
			this.Load += new System.EventHandler(this.GenericDebugger_Load);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.RegistersGroupBox.ResumeLayout(false);
			this.BreakpointsGroupBox.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private MenuStripEx menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem ExitMenuItem;
		private System.Windows.Forms.ToolStripMenuItem OptionsSubMenu;
		private System.Windows.Forms.ToolStripMenuItem AutoloadMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SaveWindowPositionMenuItem;
		private System.Windows.Forms.ToolStripMenuItem AlwaysOnTopMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FloatingWindowMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem RestoreDefaultsMenuItem;
		private System.Windows.Forms.GroupBox RegistersGroupBox;
		private RegisterBoxControl RegisterPanel;
		private System.Windows.Forms.GroupBox BreakpointsGroupBox;
		private tools.Debugger.BreakpointControl BreakPointControl1;
		private System.Windows.Forms.GroupBox DisassemblerBox;
	}
}