using System;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class VirtualPadSaturn : UserControl, IVirtualPad
	{
		public string Controller = "P1";

		public VirtualPadSaturn()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			BorderStyle = BorderStyle.Fixed3D;
			Paint += VirtualPad_Paint;
			InitializeComponent();
		}

		private void VirtualPadSaturn_Load(object sender, EventArgs e)
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
			input.Append(PU.Checked ? "U" : ".");
			input.Append(PD.Checked ? "D" : ".");
			input.Append(PL.Checked ? "L" : ".");
			input.Append(PR.Checked ? "R" : ".");

			input.Append(BStart.Checked ? "S" : ".");
			input.Append(BX.Checked ? "X" : ".");
			input.Append(BY.Checked ? "Y" : ".");
			input.Append(BZ.Checked ? "Z" : ".");
			input.Append(BA.Checked ? "A" : ".");
			input.Append(BB.Checked ? "B" : ".");
			input.Append(BC.Checked ? "C" : ".");

			input.Append(BL.Checked ? "L" : ".");
			input.Append(BR.Checked ? "R" : ".");

			input.Append("|");
			return input.ToString();
		}

		public void Clear()
		{
			if (GlobalWinF.Emulator.SystemId != "SAT") return;

			if (PU.Checked) GlobalWinF.StickyXORAdapter.SetSticky(Controller + " Up", false);
			if (PD.Checked) GlobalWinF.StickyXORAdapter.SetSticky(Controller + " Down", false);
			if (PL.Checked) GlobalWinF.StickyXORAdapter.SetSticky(Controller + " Left", false);
			if (PR.Checked) GlobalWinF.StickyXORAdapter.SetSticky(Controller + " Right", false);

			if (BStart.Checked) GlobalWinF.StickyXORAdapter.SetSticky(Controller + " Start", false);

			if (BX.Checked) GlobalWinF.StickyXORAdapter.SetSticky(Controller + " X", false);
			if (BY.Checked) GlobalWinF.StickyXORAdapter.SetSticky(Controller + " Y", false);
			if (BZ.Checked) GlobalWinF.StickyXORAdapter.SetSticky(Controller + " Z", false);

			if (BA.Checked) GlobalWinF.StickyXORAdapter.SetSticky(Controller + " A", false);
			if (BB.Checked) GlobalWinF.StickyXORAdapter.SetSticky(Controller + " B", false);
			if (BC.Checked) GlobalWinF.StickyXORAdapter.SetSticky(Controller + " C", false);

			if (BL.Checked) GlobalWinF.StickyXORAdapter.SetSticky(Controller + " L", false);
			if (BR.Checked) GlobalWinF.StickyXORAdapter.SetSticky(Controller + " R", false);

			PU.Checked = false;
			PD.Checked = false;
			PL.Checked = false;
			PR.Checked = false;

			BX.Checked = false;
			BY.Checked = false;
			BZ.Checked = false;

			BA.Checked = false;
			BB.Checked = false;
			BC.Checked = false;

			BL.Checked = false;
			BR.Checked = false;
		}

		public void SetButtons(string buttons)
		{
			if (buttons.Length < 13) return;

			if (buttons[0] == '.') PU.Checked = false; else PU.Checked = true;
			if (buttons[1] == '.') PD.Checked = false; else PD.Checked = true;
			if (buttons[2] == '.') PL.Checked = false; else PL.Checked = true;
			if (buttons[3] == '.') PR.Checked = false; else PR.Checked = true;
			if (buttons[4] == '.') BStart.Checked = false; else BStart.Checked = true;
			if (buttons[5] == '.') BX.Checked = false; else BX.Checked = true;
			if (buttons[6] == '.') BY.Checked = false; else BY.Checked = true;
			if (buttons[7] == '.') BZ.Checked = false; else BZ.Checked = true;
			if (buttons[8] == '.') BA.Checked = false; else BA.Checked = true;
			if (buttons[9] == '.') BB.Checked = false; else BB.Checked = true;
			if (buttons[10] == '.') BC.Checked = false; else BC.Checked = true;
			if (buttons[11] == '.') BL.Checked = false; else BL.Checked = true;
			if (buttons[12] == '.') BR.Checked = false; else BR.Checked = true;
		}

		private void Buttons_CheckedChanged(object sender, EventArgs e)
		{
			if (GlobalWinF.Emulator.SystemId != "SAT")
			{
				return;
			}
			else if (sender == PU)
			{
				GlobalWinF.StickyXORAdapter.SetSticky(Controller + " Up", PU.Checked);
			}
			else if (sender == PD)
			{
				GlobalWinF.StickyXORAdapter.SetSticky(Controller + " Down", PD.Checked);
			}
			else if (sender == PL)
			{
				GlobalWinF.StickyXORAdapter.SetSticky(Controller + " Left", PL.Checked);
			}
			else if (sender == PR)
			{
				GlobalWinF.StickyXORAdapter.SetSticky(Controller + " Right", PR.Checked);
			}
			else if (sender == BStart)
			{
				GlobalWinF.StickyXORAdapter.SetSticky(Controller + " Start", BStart.Checked);
			}
			else if (sender == BX)
			{
				GlobalWinF.StickyXORAdapter.SetSticky(Controller + " X", BX.Checked);
			}
			else if (sender == BY)
			{
				GlobalWinF.StickyXORAdapter.SetSticky(Controller + " Y", BY.Checked);
			}
			else if (sender == BZ)
			{
				GlobalWinF.StickyXORAdapter.SetSticky(Controller + " Z", BZ.Checked);
			}
			else if (sender == BA)
			{
				GlobalWinF.StickyXORAdapter.SetSticky(Controller + " A", BA.Checked);
			}
			else if (sender == BB)
			{
				GlobalWinF.StickyXORAdapter.SetSticky(Controller + " B", BB.Checked);
			}
			else if (sender == BC)
			{
				GlobalWinF.StickyXORAdapter.SetSticky(Controller + " C", BC.Checked);
			}
			else if (sender == BL)
			{
				GlobalWinF.StickyXORAdapter.SetSticky(Controller + " L", BL.Checked);
			}
			else if (sender == BR)
			{
				GlobalWinF.StickyXORAdapter.SetSticky(Controller + " R", BR.Checked);
			}
		}
	}
}
