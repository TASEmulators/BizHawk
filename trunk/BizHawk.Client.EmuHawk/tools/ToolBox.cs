using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Calculators;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.PCEngine;

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
			ToolBoxItems.First().Select();
			SetText();
			SetSize();

			ToolBoxItems.First().Select();
		}

		public bool AskSave() { return true;  }
		public bool UpdateBefore { get { return false; } }
		public void UpdateValues() { }

		public void Restart()
		{
			SetTools();
		}

		private void SetTools()
		{
			NesPPUToolbarItem.Visible =
				NesDebuggerToolbarItem.Visible =
				NesGameGenieToolbarItem.Visible =
				NesNameTableToolbarItem.Visible =
				Global.Emulator is NES;

			TI83KeypadToolbarItem.Visible = Global.Emulator is TI83;

			SNESGraphicsDebuggerToolbarItem.Visible =
			SNESGameGenieToolbarItem.Visible =
				Global.Emulator is LibsnesCore;

			GGGameGenieToolbarItem.Visible =
				Global.Game.System == "GG";

			PceBgViewerToolbarItem.Visible = Global.Emulator is PCEngine;
			
			GBGameGenieToolbarItem.Visible = 
				GbGpuViewerToolBarItem.Visible =
				Global.Game.System == "GB";

			foreach (var button in ToolBoxItems)
			{
				var toolBtn = button as ToolStripButton;
				toolBtn.Click += (o, e) => Close();
				toolBtn.Paint += (o, e) =>
					{
						if (ToolBoxItems.Any(x => x.Selected))
						{
							SetText();
						}
					};
			}
		}

		private void SetSize()
		{
			var rows = (int)(Math.Ceiling(ToolBoxItems.Count() / 4.0));
			this.Height = 30 + (rows * 30);
		}

		private void SetText()
		{
			var items = ToolBoxItems.ToList();
			if (items.Any(x => x.Selected))
			{
				Text = items.FirstOrDefault(x => x.Selected).ToolTipText;
			}
			else
			{
				Text = String.Empty;
			}
		}

		/// <summary>
		/// Provide LINQ capabilities to an outdated form collection
		/// </summary>
		private IEnumerable<ToolStripItem> ToolBoxItems
		{
			get
			{
				return ToolBoxStrip.Items.Cast<ToolStripItem>().Where(x => x.Visible);
			}
		}

		private void CloseBtn_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void CloseBtn_Enter(object sender, EventArgs e)
		{
			ToolBoxStrip.Focus();
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
			GlobalWin.Tools.Load<NESPPU>();
		}

		private void NesDebuggerToolbarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<NESDebugger>();
		}

		private void NesGameGenieToolbarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.MainForm.LoadGameGenieEc();
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
			GlobalWin.MainForm.LoadTAStudio();
		}

		private void SNESGraphicsDebuggerToolbarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<SNESGraphicsDebugger>();
		}

		private void VirtualpadToolbarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<VirtualPadForm>();
		}

		private void SNESGameGenieToolbarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.MainForm.LoadGameGenieEc();
		}

		private void GGGameGenieToolbarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.MainForm.LoadGameGenieEc();
		}

		private void GBGameGenieToolbarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.MainForm.LoadGameGenieEc();
		}

		private void GbGpuViewerToolBarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<GBGPUView>();
		}

		private void PceBgViewerToolbarItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<PCEBGViewer>();
		}

		#endregion
	}
}
