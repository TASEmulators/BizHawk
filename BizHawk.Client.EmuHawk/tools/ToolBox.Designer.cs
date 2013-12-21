namespace BizHawk.Client.EmuHawk
{
	partial class ToolBox
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ToolBox));
			this.ToolBoxStrip = new ToolStripEx();
			this.CheatsToolBarItem = new System.Windows.Forms.ToolStripButton();
			this.RamWatchToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.RamSearchToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.HexEditorToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.LuaConsoleToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.TAStudioToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.VirtualpadToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.NesDebuggerToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.NesPPUToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.NesNameTableToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.NesGameGenieToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.TI83KeypadToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.SNESGraphicsDebuggerToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.SNESGameGenieToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.GGGameGenieToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.GBGameGenieToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.GameboyDebuggerTool = new System.Windows.Forms.ToolStripButton();
			this.ToolBoxStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// ToolBoxStrip
			// 
			this.ToolBoxStrip.BackColor = System.Drawing.SystemColors.Control;
			this.ToolBoxStrip.ClickThrough = true;
			this.ToolBoxStrip.Dock = System.Windows.Forms.DockStyle.None;
			this.ToolBoxStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CheatsToolBarItem,
            this.RamWatchToolbarItem,
            this.RamSearchToolbarItem,
            this.HexEditorToolbarItem,
            this.LuaConsoleToolbarItem,
            this.TAStudioToolbarItem,
            this.VirtualpadToolbarItem,
            this.NesDebuggerToolbarItem,
            this.NesPPUToolbarItem,
            this.NesNameTableToolbarItem,
            this.NesGameGenieToolbarItem,
            this.TI83KeypadToolbarItem,
            this.SNESGraphicsDebuggerToolbarItem,
            this.SNESGameGenieToolbarItem,
            this.GGGameGenieToolbarItem,
            this.GBGameGenieToolbarItem,
            this.GameboyDebuggerTool});
			this.ToolBoxStrip.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.Table;
			this.ToolBoxStrip.Location = new System.Drawing.Point(9, 11);
			this.ToolBoxStrip.Name = "ToolBoxStrip";
			this.ToolBoxStrip.Size = new System.Drawing.Size(100, 394);
			this.ToolBoxStrip.TabIndex = 0;
			this.ToolBoxStrip.TabStop = true;
			// 
			// CheatsToolBarItem
			// 
			this.CheatsToolBarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Freeze;
			this.CheatsToolBarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.CheatsToolBarItem.Name = "CheatsToolBarItem";
			this.CheatsToolBarItem.Size = new System.Drawing.Size(63, 20);
			this.CheatsToolBarItem.Text = "Cheats";
			this.CheatsToolBarItem.Click += new System.EventHandler(this.CheatsToolBarItem_Click);
			// 
			// RamWatchToolbarItem
			// 
			this.RamWatchToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.FindHS;
			this.RamWatchToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.RamWatchToolbarItem.Name = "RamWatchToolbarItem";
			this.RamWatchToolbarItem.Size = new System.Drawing.Size(88, 20);
			this.RamWatchToolbarItem.Text = "Ram Watch";
			this.RamWatchToolbarItem.Click += new System.EventHandler(this.RamWatchToolbarItem_Click);
			// 
			// RamSearchToolbarItem
			// 
			this.RamSearchToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.search;
			this.RamSearchToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.RamSearchToolbarItem.Name = "RamSearchToolbarItem";
			this.RamSearchToolbarItem.Size = new System.Drawing.Size(89, 20);
			this.RamSearchToolbarItem.Text = "Ram Search";
			this.RamSearchToolbarItem.Click += new System.EventHandler(this.RamSearchToolbarItem_Click);
			// 
			// HexEditorToolbarItem
			// 
			this.HexEditorToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.poke;
			this.HexEditorToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.HexEditorToolbarItem.Name = "HexEditorToolbarItem";
			this.HexEditorToolbarItem.Size = new System.Drawing.Size(81, 20);
			this.HexEditorToolbarItem.Text = "Hex Editor";
			this.HexEditorToolbarItem.Click += new System.EventHandler(this.HexEditorToolbarItem_Click);
			// 
			// LuaConsoleToolbarItem
			// 
			this.LuaConsoleToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.textdoc;
			this.LuaConsoleToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.LuaConsoleToolbarItem.Name = "LuaConsoleToolbarItem";
			this.LuaConsoleToolbarItem.Size = new System.Drawing.Size(92, 20);
			this.LuaConsoleToolbarItem.Text = "Lua Console";
			this.LuaConsoleToolbarItem.Click += new System.EventHandler(this.LuaConsoleToolbarItem_Click);
			// 
			// TAStudioToolbarItem
			// 
			this.TAStudioToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.TAStudio;
			this.TAStudioToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.TAStudioToolbarItem.Name = "TAStudioToolbarItem";
			this.TAStudioToolbarItem.Size = new System.Drawing.Size(76, 20);
			this.TAStudioToolbarItem.Text = "TAStudio";
			this.TAStudioToolbarItem.Click += new System.EventHandler(this.TAStudioToolbarItem_Click);
			// 
			// VirtualpadToolbarItem
			// 
			this.VirtualpadToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.GameController;
			this.VirtualpadToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.VirtualpadToolbarItem.Name = "VirtualpadToolbarItem";
			this.VirtualpadToolbarItem.Size = new System.Drawing.Size(86, 20);
			this.VirtualpadToolbarItem.Text = "VirtualPads";
			this.VirtualpadToolbarItem.Click += new System.EventHandler(this.VirtualpadToolbarItem_Click);
			// 
			// NesDebuggerToolbarItem
			// 
			this.NesDebuggerToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.NESControllerIcon;
			this.NesDebuggerToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.NesDebuggerToolbarItem.Name = "NesDebuggerToolbarItem";
			this.NesDebuggerToolbarItem.Size = new System.Drawing.Size(79, 20);
			this.NesDebuggerToolbarItem.Text = "Debugger";
			this.NesDebuggerToolbarItem.Click += new System.EventHandler(this.NesDebuggerToolbarItem_Click);
			// 
			// NesPPUToolbarItem
			// 
			this.NesPPUToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.NESControllerIcon;
			this.NesPPUToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.NesPPUToolbarItem.Name = "NesPPUToolbarItem";
			this.NesPPUToolbarItem.Size = new System.Drawing.Size(49, 20);
			this.NesPPUToolbarItem.Text = "PPU";
			this.NesPPUToolbarItem.Click += new System.EventHandler(this.NesPPUToolbarItem_Click);
			// 
			// NesNameTableToolbarItem
			// 
			this.NesNameTableToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.NESControllerIcon;
			this.NesNameTableToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.NesNameTableToolbarItem.Name = "NesNameTableToolbarItem";
			this.NesNameTableToolbarItem.Size = new System.Drawing.Size(85, 20);
			this.NesNameTableToolbarItem.Text = "Nametable";
			this.NesNameTableToolbarItem.Click += new System.EventHandler(this.NesNameTableToolbarItem_Click);
			// 
			// NesGameGenieToolbarItem
			// 
			this.NesGameGenieToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.NESControllerIcon;
			this.NesGameGenieToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.NesGameGenieToolbarItem.Name = "NesGameGenieToolbarItem";
			this.NesGameGenieToolbarItem.Size = new System.Drawing.Size(91, 20);
			this.NesGameGenieToolbarItem.Text = "Game Genie";
			this.NesGameGenieToolbarItem.Click += new System.EventHandler(this.NesGameGenieToolbarItem_Click);
			// 
			// TI83KeypadToolbarItem
			// 
			this.TI83KeypadToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.calculator;
			this.TI83KeypadToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.TI83KeypadToolbarItem.Name = "TI83KeypadToolbarItem";
			this.TI83KeypadToolbarItem.Size = new System.Drawing.Size(66, 20);
			this.TI83KeypadToolbarItem.Text = "Keypad";
			this.TI83KeypadToolbarItem.Click += new System.EventHandler(this.TI83KeypadToolbarItem_Click);
			// 
			// SNESGraphicsDebuggerToolbarItem
			// 
			this.SNESGraphicsDebuggerToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.SNESControllerIcon;
			this.SNESGraphicsDebuggerToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.SNESGraphicsDebuggerToolbarItem.Name = "SNESGraphicsDebuggerToolbarItem";
			this.SNESGraphicsDebuggerToolbarItem.Size = new System.Drawing.Size(99, 20);
			this.SNESGraphicsDebuggerToolbarItem.Text = "Gfx Debugger";
			this.SNESGraphicsDebuggerToolbarItem.Click += new System.EventHandler(this.SNESGraphicsDebuggerToolbarItem_Click);
			// 
			// SNESGameGenieToolbarItem
			// 
			this.SNESGameGenieToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.SNESControllerIcon;
			this.SNESGameGenieToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.SNESGameGenieToolbarItem.Name = "SNESGameGenieToolbarItem";
			this.SNESGameGenieToolbarItem.Size = new System.Drawing.Size(91, 20);
			this.SNESGameGenieToolbarItem.Text = "Game Genie";
			this.SNESGameGenieToolbarItem.Click += new System.EventHandler(this.SNESGameGenieToolbarItem_Click);
			// 
			// GGGameGenieToolbarItem
			// 
			this.GGGameGenieToolbarItem.Image = ((System.Drawing.Image)(resources.GetObject("GGGameGenieToolbarItem.Image")));
			this.GGGameGenieToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.GGGameGenieToolbarItem.Name = "GGGameGenieToolbarItem";
			this.GGGameGenieToolbarItem.Size = new System.Drawing.Size(91, 20);
			this.GGGameGenieToolbarItem.Text = "Game Genie";
			this.GGGameGenieToolbarItem.Click += new System.EventHandler(this.GGGameGenieToolbarItem_Click);
			// 
			// GBGameGenieToolbarItem
			// 
			this.GBGameGenieToolbarItem.Image = ((System.Drawing.Image)(resources.GetObject("GBGameGenieToolbarItem.Image")));
			this.GBGameGenieToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.GBGameGenieToolbarItem.Name = "GBGameGenieToolbarItem";
			this.GBGameGenieToolbarItem.Size = new System.Drawing.Size(91, 20);
			this.GBGameGenieToolbarItem.Text = "Game Genie";
			this.GBGameGenieToolbarItem.Click += new System.EventHandler(this.GBGameGenieToolbarItem_Click);
			// 
			// GameboyDebuggerTool
			// 
			this.GameboyDebuggerTool.Name = "GameboyDebuggerTool";
			this.GameboyDebuggerTool.Size = new System.Drawing.Size(23, 4);
			// 
			// ToolBox
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(171, 449);
			this.Controls.Add(this.ToolBoxStrip);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MinimumSize = new System.Drawing.Size(129, 62);
			this.Name = "ToolBox";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Tool Box";
			this.Load += new System.EventHandler(this.ToolBox_Load);
			this.ToolBoxStrip.ResumeLayout(false);
			this.ToolBoxStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private ToolStripEx ToolBoxStrip;
		private System.Windows.Forms.ToolStripButton CheatsToolBarItem;
		private System.Windows.Forms.ToolStripButton RamWatchToolbarItem;
		private System.Windows.Forms.ToolStripButton RamSearchToolbarItem;
		private System.Windows.Forms.ToolStripButton HexEditorToolbarItem;
		private System.Windows.Forms.ToolStripButton LuaConsoleToolbarItem;
		private System.Windows.Forms.ToolStripButton NesPPUToolbarItem;
		private System.Windows.Forms.ToolStripButton NesDebuggerToolbarItem;
		private System.Windows.Forms.ToolStripButton NesGameGenieToolbarItem;
		private System.Windows.Forms.ToolStripButton NesNameTableToolbarItem;
		private System.Windows.Forms.ToolStripButton TI83KeypadToolbarItem;
		private System.Windows.Forms.ToolStripButton VirtualpadToolbarItem;
		private System.Windows.Forms.ToolStripButton GameboyDebuggerTool;
		private System.Windows.Forms.ToolStripButton SNESGraphicsDebuggerToolbarItem;
		private System.Windows.Forms.ToolStripButton SNESGameGenieToolbarItem;
		private System.Windows.Forms.ToolStripButton TAStudioToolbarItem;
		private System.Windows.Forms.ToolStripButton GGGameGenieToolbarItem;
		private System.Windows.Forms.ToolStripButton GBGameGenieToolbarItem;

	}
}