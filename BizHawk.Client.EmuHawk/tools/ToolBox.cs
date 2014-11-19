using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Emulation.Cores.Calculators;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;
using BizHawk.Emulation.Cores.Nintendo.GBA;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Cores.Sega.MasterSystem;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class ToolBox : Form, IToolForm
	{
		public ToolBox()
		{
			InitializeComponent();
		}

		private void ToolBox_Load(object sender, EventArgs e)
		{
			Location = new Point(
				GlobalWin.MainForm.Location.X + GlobalWin.MainForm.Size.Width,
				GlobalWin.MainForm.Location.Y
			);

			SetTools();
			SetSize();

			ToolBoxStrip.Select();
			ToolBoxItems.First().Select();
		}

		public bool AskSaveChanges() { return true;  }
		public bool UpdateBefore { get { return false; } }
		public void UpdateValues() { }

		public void FastUpdate()
		{
			// Do nothing
		}

		public void Restart()
		{
			SetTools();
		}

		private void SetTools()
		{
			HexEditorToolbarItem.Visible =
				RamWatchToolbarItem.Visible =
				RamSearchToolbarItem.Visible =
				CheatsToolBarItem.Visible =
				Global.Emulator.HasMemoryDomains();

			NesPPUToolbarItem.Visible =
				NesDebuggerToolbarItem.Visible =
				NesNameTableToolbarItem.Visible =
				Global.Emulator is NES;

			NesGameGenieToolbarItem.Visible = Global.Emulator.SystemId == "NES";

			TI83KeypadToolbarItem.Visible = Global.Emulator is TI83;

			SNESGraphicsDebuggerToolbarItem.Visible =
			SNESGameGenieToolbarItem.Visible =
				Global.Emulator is LibsnesCore;

			GGGameGenieToolbarItem.Visible =
				Global.Game.System == "GG";

			PceCdlToolbarItem.Visible =
				PceBgViewerToolbarItem.Visible =
				PceTileToolbarItem.Visible =
				PceSoundDebuggerButton.Visible =
				Global.Emulator is PCEngine;
			
			GBGameGenieToolbarItem.Visible = 
				GbGpuViewerToolBarItem.Visible =
				Global.Game.System == "GB";

			GbaGpuViewerToolBarItem.Visible = Global.Emulator is GBA;

			GenesisGameGenieToolBarItem.Visible = Global.Emulator.SystemId == "GEN" && VersionInfo.DeveloperBuild;
			GenesisVdpToolBarItem.Visible = Global.Emulator is GPGX;

			SmsVdpToolbarItem.Visible = Global.Emulator is SMS;

			foreach (var button in ToolBoxItems)
			{
				if (button.Visible)
				{
					var toolBtn = button as ToolStripButton;
					toolBtn.Click += (o, e) => Close();
				}
			}

			NesDebuggerToolbarItem.Visible = VersionInfo.DeveloperBuild && Global.Emulator.SystemId == "NES";
		}

		private void SetSize()
		{
			var rows = (int)Math.Ceiling(ToolBoxItems.Count() / 4.0);
			Height = 30 + (rows * 30);
		}

		// Provide LINQ capabilities to an outdated form collection
		private IEnumerable<ToolStripItem> ToolBoxItems
		{
			get
			{
				return ToolBoxStrip.Items.Cast<ToolStripItem>().Where(x => x.Visible);
			}
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.Escape)
			{
				Close();
				return true;
			}
			else
			{
				return base.ProcessCmdKey(ref msg, keyData);
			}
		}

		#region Icon Clicks

		private void CheatsToolBarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<Cheats>();
		}

		private void RamWatchToolbarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.LoadRamWatch(true);
		}

		private void RamSearchToolbarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<RamSearch>();
		}

		private void HexEditorToolbarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<HexEditor>();
		}

		private void LuaConsoleToolbarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.MainForm.OpenLuaConsole();
		}

		private void NesPPUToolbarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<NesPPU>();
		}

		private void NesDebuggerToolbarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<NESDebugger>();
		}

		private void NesGameGenieToolbarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.LoadGameGenieEc();
		}

		private void NesNameTableToolbarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<NESNameTableViewer>();
		}

		private void TI83KeypadToolbarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<TI83KeyPad>();
		}

		private void TAStudioToolbarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<TAStudio>();
		}

		private void SNESGraphicsDebuggerToolbarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<SNESGraphicsDebugger>();
		}

		private void VirtualpadToolbarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<VirtualpadTool>();
		}

		private void SNESGameGenieToolbarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.LoadGameGenieEc();
		}

		private void GGGameGenieToolbarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.LoadGameGenieEc();
		}

		private void GBGameGenieToolbarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.LoadGameGenieEc();
		}

		private void GbGpuViewerToolBarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<GBGPUView>();
		}

		private void PceCdlToolbarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<PCECDL>();
		}

		private void PceBgViewerToolbarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<PceBgViewer>();
		}

		private void PceSoundDebuggerButton_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<PCESoundDebugger>();
		}

		private void GbaGpuViewerToolBarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<GBAGPUView>();
		}

		private void GenesisGameGenieToolBarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<GenGameGenie>();
		}

		private void SmsVdpToolbarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<SmsVDPViewer>();
		}

		private void PceTileToolbarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<PCETileViewer>();
		}

		private void GenesisVdpToolBarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<GenVDPViewer>();
		}

		#endregion
	}
}
