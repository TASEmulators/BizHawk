using System;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class VirtualPadWonderSawn : UserControl, IVirtualPad
	{
		public string Controller { get; set; }

		public VirtualPadWonderSawn()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			BorderStyle = BorderStyle.Fixed3D;
			Paint += VirtualPad_Paint;
			InitializeComponent();

			Controller = "";
		}

		private void VirtualPadWonderSawn_Load(object sender, EventArgs e)
		{

		}

		private void VirtualPad_Paint(object sender, PaintEventArgs e)
		{

		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.Up)
			{
				//TODO: move to next logical key
				Refresh();
			}
			else if (keyData == Keys.Down)
			{
				Refresh();
			}
			else if (keyData == Keys.Left)
			{
				Refresh();
			}
			else if (keyData == Keys.Right)
			{
				Refresh();
			}
			else if (keyData == Keys.Tab)
			{
				Refresh();
			}
			return true;
		}

		public string GetMnemonic()
		{
			StringBuilder input = new StringBuilder("");
			input.Append(PU.Checked ? "Up X" : ".");
			input.Append(PD.Checked ? "Down X" : ".");
			input.Append(PL.Checked ? "Left X" : ".");
			input.Append(PR.Checked ? "Right X" : ".");

			input.Append("|");

			input.Append(PU2.Checked ? "Up Y" : ".");
			input.Append(PD2.Checked ? "Down Y" : ".");
			input.Append(PL2.Checked ? "Left Y" : ".");
			input.Append(PR2.Checked ? "Right Y" : ".");

			input.Append("|");

			input.Append(BStart.Checked ? "Start" : ".");
			input.Append(BB.Checked ? "B" : ".");
			input.Append(BA.Checked ? "A" : ".");
			input.Append(BPower.Checked ? "P" : ".");

			input.Append("|");

			return input.ToString();
		}

		public void Clear()
		{
			if (Global.Emulator.SystemId != "WSWAN")
			{
				return;
			}

			if (PU.Checked) Global.StickyXORAdapter.SetSticky("Up X", false);
			if (PD.Checked) Global.StickyXORAdapter.SetSticky("Down X", false);
			if (PL.Checked) Global.StickyXORAdapter.SetSticky("Left X", false);
			if (PR.Checked) Global.StickyXORAdapter.SetSticky("Right X", false);

			if (PU2.Checked) Global.StickyXORAdapter.SetSticky("Up Y", false);
			if (PD2.Checked) Global.StickyXORAdapter.SetSticky("Down Y", false);
			if (PL2.Checked) Global.StickyXORAdapter.SetSticky("Left Y", false);
			if (PR2.Checked) Global.StickyXORAdapter.SetSticky("Right Y", false);

			if (BStart.Checked) Global.StickyXORAdapter.SetSticky("Start", false);

			if (BB.Checked) Global.StickyXORAdapter.SetSticky("B", false);
			if (BA.Checked) Global.StickyXORAdapter.SetSticky("A", false);

			if (BPower.Checked) Global.StickyXORAdapter.SetSticky("Power", false);

			PU.Checked = false;
			PD.Checked = false;
			PL.Checked = false;
			PR.Checked = false;

			PU2.Checked = false;
			PD2.Checked = false;
			PL2.Checked = false;
			PR2.Checked = false;

			BStart.Checked = false;

			BA.Checked = false;
			BB.Checked = false;

			BPower.Checked = false;
		}

		public void SetButtons(string buttons)
		{
			if (buttons.Length < 15) return;

			if (buttons[1] == '.') PU.Checked = false; else PU.Checked = true;
			if (buttons[2] == '.') PD.Checked = false; else PD.Checked = true;
			if (buttons[3] == '.') PL.Checked = false; else PL.Checked = true;
			if (buttons[4] == '.') PR.Checked = false; else PR.Checked = true;

			if (buttons[6] == '.') PU2.Checked = false; else PU2.Checked = true;
			if (buttons[7] == '.') PD2.Checked = false; else PD2.Checked = true;
			if (buttons[8] == '.') PL2.Checked = false; else PL2.Checked = true;
			if (buttons[9] == '.') PR2.Checked = false; else PR2.Checked = true;

			if (buttons[11] == '.') BStart.Checked = false; else BStart.Checked = true;
			if (buttons[12] == '.') BB.Checked = false; else BB.Checked = true;
			if (buttons[13] == '.') BA.Checked = false; else BA.Checked = true;
			if (buttons[14] == '.') BPower.Checked = false; else BPower.Checked = true;
		}

		private void Buttons_CheckedChanged(object sender, EventArgs e)
		{
			if (Global.Emulator.SystemId != "WSWAN")
			{
				return;
			}
			else if (sender == PU)
			{
				Global.StickyXORAdapter.SetSticky("Up X", PU.Checked);
			}
			else if (sender == PD)
			{
				Global.StickyXORAdapter.SetSticky("Down X", PD.Checked);
			}
			else if (sender == PL)
			{
				Global.StickyXORAdapter.SetSticky("Left X", PL.Checked);
			}
			else if (sender == PR)
			{
				Global.StickyXORAdapter.SetSticky("Right X", PR.Checked);
			}
			else if (sender == PU2)
			{
				Global.StickyXORAdapter.SetSticky("Up Y", PU2.Checked);
			}
			else if (sender == PD2)
			{
				Global.StickyXORAdapter.SetSticky("Down Y", PD2.Checked);
			}
			else if (sender == PL2)
			{
				Global.StickyXORAdapter.SetSticky("Left Y", PL2.Checked);
			}
			else if (sender == PR2)
			{
				Global.StickyXORAdapter.SetSticky("Right Y", PR2.Checked);
			}
			else if (sender == BStart)
			{
				Global.StickyXORAdapter.SetSticky("Start", BStart.Checked);
			}
			else if (sender == BB)
			{
				Global.StickyXORAdapter.SetSticky("B", BB.Checked);
			}
			else if (sender == BA)
			{
				Global.StickyXORAdapter.SetSticky("A", BA.Checked);
			}
			else if (sender == BPower)
			{
				Global.StickyXORAdapter.SetSticky("Power", BPower.Checked);
			}
		}
	}
}
