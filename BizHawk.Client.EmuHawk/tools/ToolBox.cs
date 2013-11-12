using System;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Consoles.Nintendo;
using BizHawk.Emulation.Cores.Calculator;
using BizHawk.Emulation.Consoles.Nintendo.SNES;
using BizHawk.Emulation.Consoles.Sega;

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
			int x = GlobalWin.MainForm.Location.X + GlobalWin.MainForm.Size.Width;
			int y = GlobalWin.MainForm.Location.Y;
			Location = new Point(x, y);
			HideShowIcons();
		}

		public bool AskSave() { return true;  }
		public bool UpdateBefore { get { return false; } }
		public void UpdateValues() { }
		public void Restart()
		{
			HideShowIcons();
		}

		private void HideShowIcons()
		{
			if (Global.Emulator is NES)
			{
				NESPPU.Visible = true;
				NESDebugger.Visible = true;
				NESGameGenie.Visible = true;
				NESNameTable.Visible = true;
			}
			else
			{
				NESPPU.Visible = false;
				NESDebugger.Visible = false;
				NESGameGenie.Visible = false;
				NESNameTable.Visible = false;
			}

			if (Global.Emulator is TI83)
			{
				KeypadTool.Visible = true;
			}
			else
			{
				KeypadTool.Visible = false;
			}

			if (Global.Emulator is LibsnesCore)
			{
				SNESGraphicsDebuggerButton.Visible = true;
				SNESGameGenie.Visible = true;
			}
			else
			{
				SNESGraphicsDebuggerButton.Visible = false;
				SNESGameGenie.Visible = false;
			}
			if (Global.Game.System == "GG") 
			{
				GGGameGenie.Visible = true;
			}
			else
			{
				GGGameGenie.Visible = false;
			}
			if (Global.Game.System == "GB")
			{
				GBGameGenie.Visible = true;
			}
			else
			{
				GBGameGenie.Visible = false;
			}

			Size = new Size(Size.Width, toolStrip1.Size.Height + 50);
		}

		private void toolStripButton1_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<Cheats>();
		}

		private void toolStripButton2_Click(object sender, EventArgs e)
		{
			GlobalWin.MainForm.LoadRamWatch(true);
		}

		private void toolStripButton3_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<RamSearch>();
		}

		private void HexEditor_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<HexEditor>();
		}

		private void toolStripButton5_Click(object sender, EventArgs e)
		{
			GlobalWin.MainForm.OpenLuaConsole();
		}

		private void NESPPU_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<NESPPU>();
		}

		private void NESDebugger_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<NESDebugger>();
		}

		private void NESGameGenie_Click(object sender, EventArgs e)
		{
			GlobalWin.MainForm.LoadGameGenieEC();
		}

		private void NESNameTable_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<NESNameTableViewer>();
		}

		private void KeyPadTool_Click(object sender, EventArgs e)
		{
			if (Global.Emulator is TI83)
			{
				GlobalWin.Tools.Load<TI83KeyPad>();
			}
		}

		private void TAStudioButton_Click(object sender, EventArgs e)
		{
			GlobalWin.MainForm.LoadTAStudio();
		}

		private void SNESGraphicsDebuggerButton_Click(object sender, EventArgs e)
		{
			if (Global.Emulator is LibsnesCore)
			{
				GlobalWin.Tools.Load<SNESGraphicsDebugger>();
			}
		}

		private void VirtualPadButton_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<VirtualPadForm>();
		}

		private void SNESGameGenie_Click(object sender, EventArgs e)
		{
			GlobalWin.MainForm.LoadGameGenieEC();
		}

		private void GGGameGenie_Click(object sender, EventArgs e)
		{
			GlobalWin.MainForm.LoadGameGenieEC();
		}

		private void GBGameGenie_Click(object sender, EventArgs e)
		{
			GlobalWin.MainForm.LoadGameGenieEC();
		}


	}
}
