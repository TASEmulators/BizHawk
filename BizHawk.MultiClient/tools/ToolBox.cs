using System;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Consoles.Nintendo;
using BizHawk.Emulation.Consoles.Calculator;
using BizHawk.Emulation.Consoles.Nintendo.SNES;
using BizHawk.Emulation.Consoles.Sega;

namespace BizHawk.MultiClient
{
	public partial class ToolBox : Form
	{
		public ToolBox()
		{
			InitializeComponent();
		}

		private void ToolBox_Load(object sender, EventArgs e)
		{
			int x = GlobalWinF.MainForm.Location.X + GlobalWinF.MainForm.Size.Width;
			int y = GlobalWinF.MainForm.Location.Y;
			Location = new Point(x, y);
			HideShowIcons();
		}

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
			GlobalWinF.Tools.Load<Cheats>();
		}

		private void toolStripButton2_Click(object sender, EventArgs e)
		{
			GlobalWinF.MainForm.LoadRamWatch(true);
		}

		private void toolStripButton3_Click(object sender, EventArgs e)
		{
			GlobalWinF.Tools.Load<RamSearch>();
		}

		private void HexEditor_Click(object sender, EventArgs e)
		{
			GlobalWinF.Tools.Load<HexEditor>();
		}

		private void toolStripButton5_Click(object sender, EventArgs e)
		{
			GlobalWinF.MainForm.OpenLuaConsole();
		}

		private void NESPPU_Click(object sender, EventArgs e)
		{
			GlobalWinF.Tools.Load<NESPPU>();
		}

		private void NESDebugger_Click(object sender, EventArgs e)
		{
			GlobalWinF.Tools.Load<NESDebugger>();
		}

		private void NESGameGenie_Click(object sender, EventArgs e)
		{
			GlobalWinF.MainForm.LoadGameGenieEC();
		}

		private void NESNameTable_Click(object sender, EventArgs e)
		{
			GlobalWinF.Tools.Load<NESNameTableViewer>();
		}

		private void KeyPadTool_Click(object sender, EventArgs e)
		{
			if (Global.Emulator is TI83)
			{
				GlobalWinF.MainForm.LoadTI83KeyPad();
			}
		}

		private void TAStudioButton_Click(object sender, EventArgs e)
		{
			GlobalWinF.MainForm.LoadTAStudio();
		}

		private void SNESGraphicsDebuggerButton_Click(object sender, EventArgs e)
		{
			if (Global.Emulator is LibsnesCore)
			{
				GlobalWinF.MainForm.LoadSNESGraphicsDebugger();
			}
		}

		private void VirtualPadButton_Click(object sender, EventArgs e)
		{
			GlobalWinF.Tools.Load<VirtualPadForm>();
		}

		private void SNESGameGenie_Click(object sender, EventArgs e)
		{
			GlobalWinF.MainForm.LoadGameGenieEC();
		}

		private void GGGameGenie_Click(object sender, EventArgs e)
		{
			GlobalWinF.MainForm.LoadGameGenieEC();
		}

		private void GBGameGenie_Click(object sender, EventArgs e)
		{
			GlobalWinF.MainForm.LoadGameGenieEC();
		}


	}
}
