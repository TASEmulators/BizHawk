using System;
using System.Text;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class VirtualPadN64 : UserControl, IVirtualPad
	{
		private string _controllerNum = string.Empty;
		public string Controller
		{
			get
			{
				return _controllerNum;
			}

			set
			{
				AnalogControl1.Controller = _controllerNum = value;
			}
		}

		private int old_X = 0;
		private int old_Y = 0;

		public VirtualPadN64()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			BorderStyle = BorderStyle.Fixed3D;
			InitializeComponent();

			Controller = "P1";
		}

		private void UserControl1_Load(object sender, EventArgs e)
		{
			// adelikat: What's wrong with having this on players 2 - 4?
			if (Controller == "P1")
			{
				numericUpDown1.Visible = true;
				numericUpDown2.Visible = true;
			}

			PU.ControllerButton = Controller + " Up";
			PD.ControllerButton = Controller + " Down";
			PL.ControllerButton = Controller + " Left";
			PR.ControllerButton = Controller + " Right";

			BA.ControllerButton = Controller + " A";
			BB.ControllerButton = Controller + " B";
			BZ.ControllerButton = Controller + " Z";

			BS.ControllerButton = Controller + " Start";

			BL.ControllerButton = Controller + " L";
			BR.ControllerButton = Controller + " R";

			CU.ControllerButton = Controller + " C Up";
			CD.ControllerButton = Controller + " C Down";
			CL.ControllerButton = Controller + " C Left";
			CR.ControllerButton = Controller + " C Right";
		}

		public void Clear()
		{
			if (Global.Emulator.SystemId != "N64") return;

			foreach (var button in Buttons)
			{
				button.Clear();
			}
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
			x = int.Parse(buttons.Substring(14, 4));
			y = int.Parse(buttons.Substring(19, 4));

			set_analog(true, x, y);
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

		private void AnalogControl1_MouseClick(object sender, MouseEventArgs e)
		{
			set_analog(AnalogControl1.HasValue, AnalogControl1.X, AnalogControl1.Y);
		}

		private void AnalogControl1_MouseMove(object sender, MouseEventArgs e)
		{
			set_analog(AnalogControl1.HasValue, AnalogControl1.X, AnalogControl1.Y);
		}

		private void ManualX_ValueChanged(object sender, EventArgs e)
		{
			SetAnalogControlFromNumerics();
		}

		private void ManualY_ValueChanged(object sender, EventArgs e)
		{
			SetAnalogControlFromNumerics();
		}

		public void RefreshAnalog()
		{
			AnalogControl1.Refresh();
		}

		public void set_analog(bool hasValue, int X, int Y)
		{
			int? x = hasValue ? X : (int?)null;
			int? y = hasValue ? Y : (int?)null;
			Global.StickyXORAdapter.SetFloat(Controller + " X Axis", x);
			Global.StickyXORAdapter.SetFloat(Controller + " Y Axis", y);

			AnalogControl1.X = X;
			AnalogControl1.Y = Y;
			AnalogControl1.Refresh();

			old_X = X;
			old_Y = Y;
			ManualX.Value = X;
			ManualY.Value = Y;
		}

		//TODO: multiplayer
		public void FudgeAnalog(int? dx, int? dy)
		{
			int newx = AnalogControl1.X;
			int newy = AnalogControl1.Y;
			if (dx.HasValue)
			{
				newx = AnalogControl1.X + dx.Value;
				if (newx > AnalogControlPanel.Max) newx = AnalogControlPanel.Max;
				if (newx < AnalogControlPanel.Min) newx = AnalogControlPanel.Min;
				
			}

			if (dy.HasValue)
			{
				newy = AnalogControl1.Y + dy.Value;
				if (newy > AnalogControlPanel.Max) newy = AnalogControlPanel.Max;
				if (newy < AnalogControlPanel.Min) newy = AnalogControlPanel.Min;
				
			}

			AnalogControl1.SetPosition(newx, newy);
			ManualX.Value = newx;
			ManualY.Value = newy;
			Refresh();
		}

		public List<VirtualPadButton> Buttons
		{
			get
			{
				List<VirtualPadButton> _list = new List<VirtualPadButton>();
				foreach(Control c in this.Controls)
				{
					if (c is VirtualPadButton)
					{
						_list.Add((c as VirtualPadButton));
					}
				}
				return _list;
			}
		}

		private void ManualX_KeyUp(object sender, KeyEventArgs e)
		{
			SetAnalogControlFromNumerics();
		}

		private void ManualY_KeyUp(object sender, KeyEventArgs e)
		{
			SetAnalogControlFromNumerics();
		}

		public void SetAnalogControlFromNumerics()
		{
			int x = (int)ManualX.Value;
			int y = (int)ManualY.Value;
			Global.StickyXORAdapter.SetFloat(Controller + " X Axis", x);
			Global.StickyXORAdapter.SetFloat(Controller + " Y Axis", y);

			old_X = x;
			old_Y = y;
			AnalogControl1.HasValue = true;
			AnalogControl1.X = x;
			AnalogControl1.Y = y;
			AnalogControl1.Refresh();
		}

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            AnalogControlPanel.Max = (int)numericUpDown1.Value;
            ManualX.Maximum = (int)numericUpDown1.Value;
            ManualY.Maximum = (int)numericUpDown1.Value;
            AnalogControl1.Refresh();
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            AnalogControlPanel.Min = (int)numericUpDown2.Value;
            ManualX.Minimum = (int)numericUpDown2.Value;
            ManualY.Minimum = (int)numericUpDown2.Value;
            AnalogControl1.Refresh();
        }
	}
}
