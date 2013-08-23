using System;
using System.Drawing;
using System.Windows.Forms;
using System.Text;

namespace BizHawk.MultiClient
{
	class VirtualPadA26 : VirtualPad
	{
		public VirtualPadA26()
		{
			ButtonPoints[0] = new Point(14, 2);
			ButtonPoints[1] = new Point(14, 46);
			ButtonPoints[2] = new Point(2, 24);
			ButtonPoints[3] = new Point(24, 24);
			ButtonPoints[4] = new Point(122, 24);

			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			BorderStyle = BorderStyle.Fixed3D;
			Size = new Size(174, 74);

			PU = new CheckBox
				{
					Appearance = Appearance.Button,
					AutoSize = true,
					Image = Properties.Resources.BlueUp,
					ImageAlign = ContentAlignment.BottomRight,
					Location = ButtonPoints[0],
					TabIndex = 1,
					UseVisualStyleBackColor = true
				};
			PU.CheckedChanged += this.Buttons_CheckedChanged;

			PD = new CheckBox
				{
					Appearance = Appearance.Button,
					AutoSize = true,
					Image = Properties.Resources.BlueDown,
					ImageAlign = ContentAlignment.BottomRight,
					Location = ButtonPoints[1],
					TabIndex = 4,
					UseVisualStyleBackColor = true
				};
			PD.CheckedChanged += Buttons_CheckedChanged;

			PR = new CheckBox
				{
					Appearance = Appearance.Button,
					AutoSize = true,
					Image = Properties.Resources.Forward,
					ImageAlign = ContentAlignment.BottomRight,
					Location = ButtonPoints[3],
					TabIndex = 3,
					UseVisualStyleBackColor = true
				};
			PR.CheckedChanged += Buttons_CheckedChanged;

			PL = new CheckBox
				{
					Appearance = Appearance.Button,
					AutoSize = true,
					Image = Properties.Resources.Back,
					ImageAlign = ContentAlignment.BottomRight,
					Location = ButtonPoints[2],
					TabIndex = 2,
					UseVisualStyleBackColor = true
				};
			PL.CheckedChanged += Buttons_CheckedChanged;

			B1 = new CheckBox
				{
					Appearance = Appearance.Button,
					AutoSize = true,
					Location = ButtonPoints[4],
					TabIndex = 5,
					Text = "B",
					TextAlign = ContentAlignment.BottomCenter,
					UseVisualStyleBackColor = true
				};
			B1.CheckedChanged += Buttons_CheckedChanged;

			Controls.Add(PU);
			Controls.Add(PD);
			Controls.Add(PL);
			Controls.Add(PR);
			Controls.Add(B1);
			Controls.Add(B2);
			Controls.Add(B3);
			Controls.Add(B4);
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

		public override string GetMnemonic()
		{
			StringBuilder input = new StringBuilder("");
			input.Append(PR.Checked ? "U" : ".");
			input.Append(PL.Checked ? "D" : ".");
			input.Append(PD.Checked ? "L" : ".");
			input.Append(PU.Checked ? "R" : ".");

			input.Append(B1.Checked ? "B" : ".");
			input.Append("|");
			return input.ToString();
		}

		public override void SetButtons(string buttons)
		{
			if (buttons.Length < 5) return;
			if (buttons[0] == '.') PU.Checked = false; else PU.Checked = true;
			if (buttons[1] == '.') PD.Checked = false; else PD.Checked = true;
			if (buttons[2] == '.') PL.Checked = false; else PL.Checked = true;
			if (buttons[3] == '.') PR.Checked = false; else PR.Checked = true;

			if (buttons[4] == '.') B1.Checked = false; else B1.Checked = true;
		}

		private void Buttons_CheckedChanged(object sender, EventArgs e)
		{
			if (Global.Emulator.SystemId != "A26" && Global.Emulator.SystemId != "C64") return;
			if (sender == PU)
				Global.StickyXORAdapter.SetSticky(Controller + " Up", PU.Checked);
			else if (sender == PD)
				Global.StickyXORAdapter.SetSticky(Controller + " Down", PD.Checked);
			else if (sender == PL)
				Global.StickyXORAdapter.SetSticky(Controller + " Left", PL.Checked);
			else if (sender == PR)
				Global.StickyXORAdapter.SetSticky(Controller + " Right", PR.Checked);
			else if (sender == B1)
				Global.StickyXORAdapter.SetSticky(Controller + " Button", B1.Checked);
		}

		public override void Clear()
		{
			if (Global.Emulator.SystemId != "A26" && Global.Emulator.SystemId != "C64") return;
			

			if (PU.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Up", false);
			if (PD.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Down", false);
			if (PL.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Left", false);
			if (PR.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Right", false);
			if (B1.Checked) Global.StickyXORAdapter.SetSticky(Controller + " B", false);

			PU.Checked = false;
			PD.Checked = false;
			PL.Checked = false;
			PR.Checked = false;

			B1.Checked = false;
		}
	}
}
