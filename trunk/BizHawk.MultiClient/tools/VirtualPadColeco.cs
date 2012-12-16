using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class VirtualPadColeco : UserControl , IVirtualPad
	{
		public string Controller = "P1";
		public VirtualPadColeco()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			this.BorderStyle = BorderStyle.Fixed3D;
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.VirtualPad_Paint);
			InitializeComponent();
		}

		private void VirtualPadColeco_Load(object sender, EventArgs e)
		{

		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.Up)
			{
				//TODO: move to next logical key
				this.Refresh();
			}
			else if (keyData == Keys.Down)
			{
				this.Refresh();
			}
			else if (keyData == Keys.Left)
			{
				this.Refresh();
			}
			else if (keyData == Keys.Right)
			{
				this.Refresh();
			}
			else if (keyData == Keys.Tab)
			{
				this.Refresh();
			}
			return true;
		}

		private void VirtualPad_Paint(object sender, PaintEventArgs e)
		{

		}

		public string GetMnemonic()
		{
			StringBuilder input = new StringBuilder("");
			input.Append(PU.Checked ? "U" : ".");
			input.Append(PD.Checked ? "D" : ".");
			input.Append(PL.Checked ? "L" : ".");
			input.Append(PR.Checked ? "R" : ".");

			input.Append(KeyLeft.Checked ? "l" : ".");
			input.Append(KeyRight.Checked ? "r" : ".");
			input.Append(KP1.Checked ? "1" : ".");
			input.Append(KP2.Checked ? "2" : ".");
			input.Append(KP3.Checked ? "3" : ".");
			input.Append(KP4.Checked ? "4" : ".");
			input.Append(KP5.Checked ? "5" : ".");
			input.Append(KP6.Checked ? "6" : ".");
			input.Append(KP7.Checked ? "7" : ".");
			input.Append(KP8.Checked ? "8" : ".");
			input.Append(KP9.Checked ? "9" : ".");
			input.Append(KPStar.Checked ? "*" : ".");
			input.Append(KP0.Checked ? "0" : ".");
			input.Append(KPPound.Checked ? "#" : ".");
			input.Append("|");
			return input.ToString();
		}

		public void SetButtons(string buttons)
		{
			if (buttons.Length < 18) return;
			if (buttons[0] == '.') PU.Checked = false; else PU.Checked = true;
			if (buttons[1] == '.') PD.Checked = false; else PD.Checked = true;
			if (buttons[2] == '.') PL.Checked = false; else PL.Checked = true;
			if (buttons[3] == '.') PR.Checked = false; else PR.Checked = true;

			if (buttons[4] == '.') KeyLeft.Checked = false; else KeyLeft.Checked = true;
			if (buttons[5] == '.') KeyRight.Checked = false; else KeyRight.Checked = true;
			if (buttons[6] == '.') KP1.Checked = false; else KP1.Checked = true;
			if (buttons[6] == '.') KP2.Checked = false; else KP2.Checked = true;
			if (buttons[6] == '.') KP3.Checked = false; else KP3.Checked = true;
			if (buttons[6] == '.') KP4.Checked = false; else KP4.Checked = true;
			if (buttons[6] == '.') KP5.Checked = false; else KP5.Checked = true;
			if (buttons[6] == '.') KP6.Checked = false; else KP6.Checked = true;
			if (buttons[6] == '.') KP7.Checked = false; else KP7.Checked = true;
			if (buttons[6] == '.') KP8.Checked = false; else KP8.Checked = true;
			if (buttons[6] == '.') KP9.Checked = false; else KP9.Checked = true;
			if (buttons[6] == '.') KPStar.Checked = false; else KPStar.Checked = true;
			if (buttons[6] == '.') KP0.Checked = false; else KP0.Checked = true;
			if (buttons[6] == '.') KPPound.Checked = false; else KPPound.Checked = true;
		}

		private void Buttons_CheckedChanged(object sender, EventArgs e)
		{
			if (Global.Emulator.SystemId != "Coleco") return;
			if (sender == PU)
				Global.StickyXORAdapter.SetSticky(Controller + " Up", PU.Checked);
			else if (sender == PD)
				Global.StickyXORAdapter.SetSticky(Controller + " Down", PD.Checked);
			else if (sender == PL)
				Global.StickyXORAdapter.SetSticky(Controller + " Left", PL.Checked);
			else if (sender == PR)
				Global.StickyXORAdapter.SetSticky(Controller + " Right", PR.Checked);
			
			else if (sender == KeyLeft)
				Global.StickyXORAdapter.SetSticky(Controller + " L", KeyLeft.Checked);
			else if (sender == KeyRight)
				Global.StickyXORAdapter.SetSticky(Controller + " R", KeyRight.Checked);
			else if (sender == KP1)
				Global.StickyXORAdapter.SetSticky(Controller + " Key1", KP1.Checked);
			else if (sender == KP2)
				Global.StickyXORAdapter.SetSticky(Controller + " Key2", KP2.Checked);
			else if (sender == KP3)
				Global.StickyXORAdapter.SetSticky(Controller + " Key3", KP3.Checked);
			else if (sender == KP4)
				Global.StickyXORAdapter.SetSticky(Controller + " Key4", KP4.Checked);
			else if (sender == KP5)
				Global.StickyXORAdapter.SetSticky(Controller + " Key5", KP5.Checked);
			else if (sender == KP6)
				Global.StickyXORAdapter.SetSticky(Controller + " Key6", KP6.Checked);
			else if (sender == KP7)
				Global.StickyXORAdapter.SetSticky(Controller + " Key7", KP7.Checked);
			else if (sender == KP8)
				Global.StickyXORAdapter.SetSticky(Controller + " Key8", KP8.Checked);
			else if (sender == KP9)
				Global.StickyXORAdapter.SetSticky(Controller + " Key9", KP9.Checked);
			else if (sender == KPStar)
				Global.StickyXORAdapter.SetSticky(Controller + " Star", KPStar.Checked);
			else if (sender == KP0)
				Global.StickyXORAdapter.SetSticky(Controller + " Key0", KP0.Checked);
			else if (sender == KPPound)
				Global.StickyXORAdapter.SetSticky(Controller + " Pound", KPPound.Checked);
		}

		public void Clear()
		{
			if (Global.Emulator.SystemId != "Coleco") return;


			if (PU.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Up", false);
			if (PD.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Down", false);
			if (PL.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Left", false);
			if (PR.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Right", false);
			if (KeyLeft.Checked) Global.StickyXORAdapter.SetSticky(Controller + " L", false);
			if (KeyRight.Checked) Global.StickyXORAdapter.SetSticky(Controller + " R", false);
			if (KP1.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Key0", false);
			if (KP2.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Key1", false);
			if (KP3.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Key2", false);
			if (KP4.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Key3", false);
			if (KP5.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Key4", false);
			if (KP6.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Key5", false);
			if (KP7.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Key6", false);
			if (KP8.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Key7", false);
			if (KP9.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Key8", false);
			if (KPStar.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Key9", false);
			if (KP0.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Star", false);
			if (KPPound.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Pound", false);

			PU.Checked = false;
			PD.Checked = false;
			PL.Checked = false;
			PR.Checked = false;
			KeyLeft.Checked = false;
			KeyRight.Checked = false;
			KP1.Checked = false;
			KP2.Checked = false;
			KP3.Checked = false;
			KP4.Checked = false;
			KP5.Checked = false;
			KP6.Checked = false;
			KP7.Checked = false;
			KP8.Checked = false;
			KP9.Checked = false;
			KPStar.Checked = false;
			KP0.Checked = false;
			KPPound.Checked = false;
		}
	}
}
