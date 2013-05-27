using System;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class VirtualPadN64 : UserControl, IVirtualPad
	{
		public string Controller = "P1";

		public VirtualPadN64()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			BorderStyle = BorderStyle.Fixed3D;
			InitializeComponent();
		}

		private void UserControl1_Load(object sender, EventArgs e)
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

		public void Clear()
		{
			if (Global.Emulator.SystemId != "N64") return;


			if (PU.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Up", false);
			if (PD.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Down", false);
			if (PL.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Left", false);
			if (PR.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Right", false);

			if (BB.Checked) Global.StickyXORAdapter.SetSticky(Controller + " B", false);
			if (BA.Checked) Global.StickyXORAdapter.SetSticky(Controller + " A", false);
			if (BZ.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Z", false);
			if (BS.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Start", false);

			if (BL.Checked) Global.StickyXORAdapter.SetSticky(Controller + " L", false);
			if (BR.Checked) Global.StickyXORAdapter.SetSticky(Controller + " R", false);

			if (CU.Checked) Global.StickyXORAdapter.SetSticky(Controller + " C Up", false);
			if (CD.Checked) Global.StickyXORAdapter.SetSticky(Controller + " C Down", false);
			if (CL.Checked) Global.StickyXORAdapter.SetSticky(Controller + " C Left", false);
			if (CR.Checked) Global.StickyXORAdapter.SetSticky(Controller + " C Right", false);

			PU.Checked = false;
			PD.Checked = false;
			PL.Checked = false;
			PR.Checked = false;

			BB.Checked = false;
			BA.Checked = false;
			BZ.Checked = false;
			BS.Checked = false;
			BL.Checked = false;
			BR.Checked = false;

			CU.Checked = false;
			CD.Checked = false;
			CL.Checked = false;
			CR.Checked = false;
		}

		public void SetButtons(string buttons)
		{
			if (buttons.Length < 14) return;
			if (buttons[0] == '.') PU.Checked = false; else PU.Checked = true;
			if (buttons[1] == '.') PD.Checked = false; else PD.Checked = true;
			if (buttons[2] == '.') PL.Checked = false; else PL.Checked = true;
			if (buttons[3] == '.') PR.Checked = false; else PR.Checked = true;
			if (buttons[4] == '.') BB.Checked = false; else BB.Checked = true;
			if (buttons[5] == '.') BA.Checked = false; else BA.Checked = true;
			if (buttons[6] == '.') BZ.Checked = false; else BZ.Checked = true;
			if (buttons[7] == '.') BS.Checked = false; else BS.Checked = true;
			if (buttons[8] == '.') BL.Checked = false; else BL.Checked = true;
			if (buttons[9] == '.') BR.Checked = false; else BR.Checked = true;
			if (buttons[10] == '.') CU.Checked = false; else CU.Checked = true;
			if (buttons[11] == '.') CD.Checked = false; else CD.Checked = true;
			if (buttons[12] == '.') CL.Checked = false; else CL.Checked = true;
			if (buttons[13] == '.') CR.Checked = false; else CR.Checked = true;

			int x = 0;
			int y = 0;
			if (InputValidate.IsValidUnsignedNumber(buttons.Substring(14, 3)))
			{
				x = Int32.Parse(buttons.Substring(14, 3)) - 128;

			}
			if (InputValidate.IsValidUnsignedNumber(buttons.Substring(17, 3)))
			{
				y = Int32.Parse(buttons.Substring(17, 3)) - 128;
			}
			AnalogControl1.SetPosition(x, y);
		}

		public string GetMnemonic()
		{
			StringBuilder input = new StringBuilder("");
			input.Append(PU.Checked ? "U" : ".");
			input.Append(PD.Checked ? "D" : ".");
			input.Append(PL.Checked ? "L" : ".");
			input.Append(PR.Checked ? "R" : ".");

			input.Append(BB.Checked ? "B" : ".");
			input.Append(BA.Checked ? "A" : ".");
			input.Append(BZ.Checked ? "Z" : ".");
			input.Append(BS.Checked ? "S" : ".");

			input.Append(BL.Checked ? "L" : ".");
			input.Append(BR.Checked ? "R" : ".");

			input.Append(CU.Checked ? "u" : ".");
			input.Append(CD.Checked ? "d" : ".");
			input.Append(CL.Checked ? "l" : "."); 
			input.Append(CR.Checked ? "r" : ".");
			input.Append(String.Format("{0:000}", AnalogControl1.X + 128));
			input.Append(String.Format("{0:000}", AnalogControl1.Y + 128));

			input.Append("|");
			return input.ToString();
		}

		private void Buttons_CheckedChanged(object sender, EventArgs e)
		{
			if (Global.Emulator.SystemId != "N64")
			{
				return;
			}
			else if (sender == PU)
			{
				Global.StickyXORAdapter.SetSticky(Controller + " DPad U", PU.Checked);
			}
			else if (sender == PD)
			{
				Global.StickyXORAdapter.SetSticky(Controller + " DPad D", PD.Checked);
			}
			else if (sender == PL)
			{
				Global.StickyXORAdapter.SetSticky(Controller + " DPad L", PL.Checked);
			}
			else if (sender == PR)
			{
				Global.StickyXORAdapter.SetSticky(Controller + " DPad R", PR.Checked);
			}
			else if (sender == CR)
			{
				Global.StickyXORAdapter.SetSticky(Controller + " C Right", CR.Checked);
			}
			else if (sender == CL)
			{
				Global.StickyXORAdapter.SetSticky(Controller + " C Left", CL.Checked);
			}
			else if (sender == CU)
			{
				Global.StickyXORAdapter.SetSticky(Controller + " C Up", CU.Checked);
			}
			else if (sender == CD)
			{
				Global.StickyXORAdapter.SetSticky(Controller + " C Down", CD.Checked);
			}
			else if (sender == BR)
			{
				Global.StickyXORAdapter.SetSticky(Controller + " R", BR.Checked);
			}
			else if (sender == BL)
			{
				Global.StickyXORAdapter.SetSticky(Controller + " L", BL.Checked);
			}
			else if (sender == BS)
			{
				Global.StickyXORAdapter.SetSticky(Controller + " Start", BS.Checked);
			}
			else if (sender == BA)
			{
				Global.StickyXORAdapter.SetSticky(Controller + " A", BA.Checked);
			}
			else if (sender == BB)
			{
				Global.StickyXORAdapter.SetSticky(Controller + " B", BB.Checked);
			}
			else if (sender == BZ)
			{
				Global.StickyXORAdapter.SetSticky(Controller + " Z", BZ.Checked);
			}
		}

		private void AnalogControl1_MouseClick(object sender, MouseEventArgs e)
		{
			Global.StickyXORAdapter.SetFloat(Controller + " X Axis", AnalogControl1.X);
			Global.StickyXORAdapter.SetFloat(Controller + " Y Axis", -AnalogControl1.Y - 1);
		}

		private void AnalogControl1_MouseMove(object sender, MouseEventArgs e)
		{
			Global.StickyXORAdapter.SetFloat(Controller + " X Axis", AnalogControl1.X);
			Global.StickyXORAdapter.SetFloat(Controller + " Y Axis", -AnalogControl1.Y - 1);
		}
	}
}
