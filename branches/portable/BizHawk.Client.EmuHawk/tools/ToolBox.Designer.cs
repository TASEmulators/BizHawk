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
			this.GbGpuViewerToolBarItem = new System.Windows.Forms.ToolStripButton();
			this.GBGameGenieToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.PceBgViewerToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.GbaGpuViewerToolBarItem = new System.Windows.Forms.ToolStripButton();
			this.GenesisGameGenieToolBarItem = new System.Windows.Forms.ToolStripButton();
			this.ToolBoxStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// ToolBoxStrip
			// 
			this.ToolBoxStrip.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.ToolBoxStrip.AutoSize = false;
			this.ToolBoxStrip.BackColor = System.Drawing.SystemColors.Control;
			this.ToolBoxStrip.ClickThrough = true;
			this.ToolBoxStrip.Dock = System.Windows.Forms.DockStyle.None;
			this.ToolBoxStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
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
            this.GbGpuViewerToolBarItem,
            this.GBGameGenieToolbarItem,
            this.PceBgViewerToolbarItem,
            this.GbaGpuViewerToolBarItem,
            this.GenesisGameGenieToolBarItem});
			this.ToolBoxStrip.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.Flow;
			this.ToolBoxStrip.Location = new System.Drawing.Point(2, 2);
			this.ToolBoxStrip.Name = "ToolBoxStrip";
			this.ToolBoxStrip.Padding = new System.Windows.Forms.Padding(0);
			this.ToolBoxStrip.Size = new System.Drawing.Size(137, 141);
			this.ToolBoxStrip.Stretch = true;
			this.ToolBoxStrip.TabIndex = 0;
			this.ToolBoxStrip.TabStop = true;
			// 
			// CheatsToolBarItem
			// 
			this.CheatsToolBarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Freeze;
			this.CheatsToolBarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.CheatsToolBarItem.Name = "CheatsToolBarItem";
			this.CheatsToolBarItem.Size = new System.Drawing.Size(23, 20);
			this.CheatsToolBarItem.ToolTipText = "Cheats";
			this.CheatsToolBarItem.Click += new System.EventHandler(this.CheatsToolBarItem_Click);
			// 
			// RamWatchToolbarItem
			// 
			this.RamWatchToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.FindHS;
			this.RamWatchToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.RamWatchToolbarItem.Name = "RamWatchToolbarItem";
			this.RamWatchToolbarItem.Size = new System.Drawing.Size(23, 20);
			this.RamWatchToolbarItem.ToolTipText = "Ram Watch";
			this.RamWatchToolbarItem.Click += new System.EventHandler(this.RamWatchToolbarItem_Click);
			// 
			// RamSearchToolbarItem
			// 
			this.RamSearchToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.search;
			this.RamSearchToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.RamSearchToolbarItem.Name = "RamSearchToolbarItem";
			this.RamSearchToolbarItem.Size = new System.Drawing.Size(23, 20);
			this.RamSearchToolbarItem.ToolTipText = "Ram Search";
			this.RamSearchToolbarItem.Click += new System.EventHandler(this.RamSearchToolbarItem_Click);
			// 
			// HexEditorToolbarItem
			// 
			this.HexEditorToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.poke;
			this.HexEditorToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.HexEditorToolbarItem.Name = "HexEditorToolbarItem";
			this.HexEditorToolbarItem.Size = new System.Drawing.Size(23, 20);
			this.HexEditorToolbarItem.ToolTipText = "Hex Editor";
			this.HexEditorToolbarItem.Click += new System.EventHandler(this.HexEditorToolbarItem_Click);
			// 
			// LuaConsoleToolbarItem
			// 
			this.LuaConsoleToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.textdoc;
			this.LuaConsoleToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.LuaConsoleToolbarItem.Name = "LuaConsoleToolbarItem";
			this.LuaConsoleToolbarItem.Size = new System.Drawing.Size(23, 20);
			this.LuaConsoleToolbarItem.ToolTipText = "Lua Console";
			this.LuaConsoleToolbarItem.Click += new System.EventHandler(this.LuaConsoleToolbarItem_Click);
			// 
			// TAStudioToolbarItem
			// 
			this.TAStudioToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.TAStudio;
			this.TAStudioToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.TAStudioToolbarItem.Name = "TAStudioToolbarItem";
			this.TAStudioToolbarItem.Size = new System.Drawing.Size(23, 20);
			this.TAStudioToolbarItem.ToolTipText = "TAStudio";
			this.TAStudioToolbarItem.Click += new System.EventHandler(this.TAStudioToolbarItem_Click);
			// 
			// VirtualpadToolbarItem
			// 
			this.VirtualpadToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.GameController;
			this.VirtualpadToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.VirtualpadToolbarItem.Name = "VirtualpadToolbarItem";
			this.VirtualpadToolbarItem.Size = new System.Drawing.Size(23, 20);
			this.VirtualpadToolbarItem.ToolTipText = "Virtualpads";
			this.VirtualpadToolbarItem.Click += new System.EventHandler(this.VirtualpadToolbarItem_Click);
			// 
			// NesDebuggerToolbarItem
			// 
			this.NesDebuggerToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.NESControllerIcon;
			this.NesDebuggerToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.NesDebuggerToolbarItem.Name = "NesDebuggerToolbarItem";
			this.NesDebuggerToolbarItem.Size = new System.Drawing.Size(49, 20);
			this.NesDebuggerToolbarItem.Text = "Dbg";
			this.NesDebuggerToolbarItem.ToolTipText = "Nes Debugger";
			this.NesDebuggerToolbarItem.Click += new System.EventHandler(this.NesDebuggerToolbarItem_Click);
			// 
			// NesPPUToolbarItem
			// 
			this.NesPPUToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.NESControllerIcon;
			this.NesPPUToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.NesPPUToolbarItem.Name = "NesPPUToolbarItem";
			this.NesPPUToolbarItem.Size = new System.Drawing.Size(49, 20);
			this.NesPPUToolbarItem.Text = "PPU";
			this.NesPPUToolbarItem.ToolTipText = "Nes PPU Viewer";
			this.NesPPUToolbarItem.Click += new System.EventHandler(this.NesPPUToolbarItem_Click);
			// 
			// NesNameTableToolbarItem
			// 
			this.NesNameTableToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.NESControllerIcon;
			this.NesNameTableToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.NesNameTableToolbarItem.Name = "NesNameTableToolbarItem";
			this.NesNameTableToolbarItem.Size = new System.Drawing.Size(40, 20);
			this.NesNameTableToolbarItem.Text = "Nt";
			this.NesNameTableToolbarItem.ToolTipText = "Nes Nametable Viewer";
			this.NesNameTableToolbarItem.Click += new System.EventHandler(this.NesNameTableToolbarItem_Click);
			// 
			// NesGameGenieToolbarItem
			// 
			this.NesGameGenieToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.NESControllerIcon;
			this.NesGameGenieToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.NesGameGenieToolbarItem.Name = "NesGameGenieToolbarItem";
			this.NesGameGenieToolbarItem.Size = new System.Drawing.Size(43, 20);
			this.NesGameGenieToolbarItem.Text = "GG";
			this.NesGameGenieToolbarItem.ToolTipText = "NES Game Genie Encoder/Decoder";
			this.NesGameGenieToolbarItem.Click += new System.EventHandler(this.NesGameGenieToolbarItem_Click);
			// 
			// TI83KeypadToolbarItem
			// 
			this.TI83KeypadToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.calculator;
			this.TI83KeypadToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.TI83KeypadToolbarItem.Name = "TI83KeypadToolbarItem";
			this.TI83KeypadToolbarItem.Size = new System.Drawing.Size(23, 20);
			this.TI83KeypadToolbarItem.ToolTipText = "TI83 Keypad";
			this.TI83KeypadToolbarItem.Click += new System.EventHandler(this.TI83KeypadToolbarItem_Click);
			// 
			// SNESGraphicsDebuggerToolbarItem
			// 
			this.SNESGraphicsDebuggerToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.SNESControllerIcon;
			this.SNESGraphicsDebuggerToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.SNESGraphicsDebuggerToolbarItem.Name = "SNESGraphicsDebuggerToolbarItem";
			this.SNESGraphicsDebuggerToolbarItem.Size = new System.Drawing.Size(44, 20);
			this.SNESGraphicsDebuggerToolbarItem.Text = "Gfx";
			this.SNESGraphicsDebuggerToolbarItem.ToolTipText = "SNES Gfx Debugger";
			this.SNESGraphicsDebuggerToolbarItem.Click += new System.EventHandler(this.SNESGraphicsDebuggerToolbarItem_Click);
			// 
			// SNESGameGenieToolbarItem
			// 
			this.SNESGameGenieToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.SNESControllerIcon;
			this.SNESGameGenieToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.SNESGameGenieToolbarItem.Name = "SNESGameGenieToolbarItem";
			this.SNESGameGenieToolbarItem.Size = new System.Drawing.Size(43, 20);
			this.SNESGameGenieToolbarItem.Text = "GG";
			this.SNESGameGenieToolbarItem.ToolTipText = "SNES Game Genie Encoder/Decoder";
			this.SNESGameGenieToolbarItem.Click += new System.EventHandler(this.SNESGameGenieToolbarItem_Click);
			// 
			// GGGameGenieToolbarItem
			// 
			this.GGGameGenieToolbarItem.Image = ((System.Drawing.Image)(resources.GetObject("GGGameGenieToolbarItem.Image")));
			this.GGGameGenieToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.GGGameGenieToolbarItem.Name = "GGGameGenieToolbarItem";
			this.GGGameGenieToolbarItem.Size = new System.Drawing.Size(43, 20);
			this.GGGameGenieToolbarItem.Text = "GG";
			this.GGGameGenieToolbarItem.ToolTipText = "Game Gear Game Genie Encoder/Decoder";
			this.GGGameGenieToolbarItem.Click += new System.EventHandler(this.GGGameGenieToolbarItem_Click);
			// 
			// GbGpuViewerToolBarItem
			// 
			this.GbGpuViewerToolBarItem.Image = ((System.Drawing.Image)(resources.GetObject("GbGpuViewerToolBarItem.Image")));
			this.GbGpuViewerToolBarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.GbGpuViewerToolBarItem.Name = "GbGpuViewerToolBarItem";
			this.GbGpuViewerToolBarItem.Size = new System.Drawing.Size(49, 20);
			this.GbGpuViewerToolBarItem.Text = "Gpu";
			this.GbGpuViewerToolBarItem.ToolTipText = "Gameboy Game Genie Encoder/Decoder";
			this.GbGpuViewerToolBarItem.Click += new System.EventHandler(this.GbGpuViewerToolBarItem_Click);
			// 
			// GBGameGenieToolbarItem
			// 
			this.GBGameGenieToolbarItem.Image = ((System.Drawing.Image)(resources.GetObject("GBGameGenieToolbarItem.Image")));
			this.GBGameGenieToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.GBGameGenieToolbarItem.Name = "GBGameGenieToolbarItem";
			this.GBGameGenieToolbarItem.Size = new System.Drawing.Size(43, 20);
			this.GBGameGenieToolbarItem.Text = "GG";
			this.GBGameGenieToolbarItem.ToolTipText = "Gameboy Game Genie Encoder/Decoder";
			this.GBGameGenieToolbarItem.Click += new System.EventHandler(this.GBGameGenieToolbarItem_Click);
			// 
			// PceBgViewerToolbarItem
			// 
			this.PceBgViewerToolbarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.pcejin1;
			this.PceBgViewerToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.PceBgViewerToolbarItem.Name = "PceBgViewerToolbarItem";
			this.PceBgViewerToolbarItem.Size = new System.Drawing.Size(41, 20);
			this.PceBgViewerToolbarItem.Text = "Bg";
			this.PceBgViewerToolbarItem.ToolTipText = "PC Engine Background Viewer";
			this.PceBgViewerToolbarItem.Click += new System.EventHandler(this.PceBgViewerToolbarItem_Click);
			// 
			// GbaGpuViewerToolBarItem
			// 
			this.GbaGpuViewerToolBarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.gba_icon;
			this.GbaGpuViewerToolBarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.GbaGpuViewerToolBarItem.Name = "GbaGpuViewerToolBarItem";
			this.GbaGpuViewerToolBarItem.Size = new System.Drawing.Size(49, 20);
			this.GbaGpuViewerToolBarItem.Text = "Gpu";
			this.GbaGpuViewerToolBarItem.ToolTipText = "Gameboy Advance Gpu Viewer";
			this.GbaGpuViewerToolBarItem.Click += new System.EventHandler(this.GbaGpuViewerToolBarItem_Click);
			// 
			// GenesisGameGenieToolBarItem
			// 
			this.GenesisGameGenieToolBarItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.GenesisControllerIcon;
			this.GenesisGameGenieToolBarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.GenesisGameGenieToolBarItem.Name = "GenesisGameGenieToolBarItem";
			this.GenesisGameGenieToolBarItem.Size = new System.Drawing.Size(43, 20);
			this.GenesisGameGenieToolBarItem.Text = "GG";
			this.GenesisGameGenieToolBarItem.ToolTipText = "Genesis Game Genie Encoder/Decoder";
			this.GenesisGameGenieToolBarItem.Click += new System.EventHandler(this.GenesisGameGenieToolBarItem_Click);
			// 
			// ToolBox
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(140, 145);
			this.Controls.Add(this.ToolBoxStrip);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximumSize = new System.Drawing.Size(270, 600);
			this.MinimumSize = new System.Drawing.Size(135, 38);
			this.Name = "ToolBox";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Load += new System.EventHandler(this.ToolBox_Load);
			this.ToolBoxStrip.ResumeLayout(false);
			this.ToolBoxStrip.PerformLayout();
			this.ResumeLayout(false);

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
		private System.Windows.Forms.ToolStripButton SNESGraphicsDebuggerToolbarItem;
		private System.Windows.Forms.ToolStripButton SNESGameGenieToolbarItem;
		private System.Windows.Forms.ToolStripButton TAStudioToolbarItem;
		private System.Windows.Forms.ToolStripButton GGGameGenieToolbarItem;
		private System.Windows.Forms.ToolStripButton PceBgViewerToolbarItem;
		private System.Windows.Forms.ToolStripButton GBGameGenieToolbarItem;
		private System.Windows.Forms.ToolStripButton GbGpuViewerToolBarItem;
		private System.Windows.Forms.ToolStripButton GbaGpuViewerToolBarItem;
		private System.Windows.Forms.ToolStripButton GenesisGameGenieToolBarItem;

	}
}