using System;
using System.Drawing;
using System.Windows.Forms;
using System.Text;

namespace BizHawk.MultiClient
{
	public class VirtualPadSNES : VirtualPad
	{
		public VirtualPadSNES()
		{
			ButtonPoints[0] = new Point(14, 2);
			ButtonPoints[1] = new Point(14, 46);
			ButtonPoints[2] = new Point(2, 24);
			ButtonPoints[3] = new Point(24, 24);
			ButtonPoints[4] = new Point(56, 42);
			ButtonPoints[5] = new Point(78, 42);
			ButtonPoints[6] = new Point(122, 12); //X
			ButtonPoints[7] = new Point(146, 22); //A
			ButtonPoints[8] = new Point(110, 34); //Y
			ButtonPoints[9] = new Point(134, 48); //B
			ButtonPoints[10] = new Point(42, 0); //L
			ButtonPoints[11] = new Point(94, 0); //R

			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			BorderStyle = BorderStyle.Fixed3D;
			Paint += VirtualPad_Paint;
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
			PU.CheckedChanged += Buttons_CheckedChanged;

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
					Text = "s",
					TextAlign = ContentAlignment.BottomCenter,
					UseVisualStyleBackColor = true
				};
			B1.CheckedChanged += Buttons_CheckedChanged;

			B2 = new CheckBox
				{
					Appearance = Appearance.Button,
					AutoSize = true,
					Location = ButtonPoints[5],
					TabIndex = 6,
					Text = "S",
					TextAlign = ContentAlignment.BottomCenter,
					UseVisualStyleBackColor = true
				};
			B2.CheckedChanged += Buttons_CheckedChanged;

			B3 = new CheckBox
				{
					Appearance = Appearance.Button,
					AutoSize = true,
					Location = ButtonPoints[9],
					TabIndex = 7,
					Text = "B",
					TextAlign = ContentAlignment.BottomCenter,
					UseVisualStyleBackColor = true
				};
			B3.CheckedChanged += Buttons_CheckedChanged;

			B4 = new CheckBox
				{
					Appearance = Appearance.Button,
					AutoSize = true,
					Location = ButtonPoints[7],
					TabIndex = 8,
					Text = "A",
					TextAlign = ContentAlignment.BottomCenter,
					UseVisualStyleBackColor = true
				};
			B4.CheckedChanged += Buttons_CheckedChanged;

			B5 = new CheckBox
				{
					Appearance = Appearance.Button,
					AutoSize = true,
					Location = ButtonPoints[6],
					TabIndex = 8,
					Text = "X",
					TextAlign = ContentAlignment.BottomCenter,
					UseVisualStyleBackColor = true
				};
			B5.CheckedChanged += Buttons_CheckedChanged;

			B6 = new CheckBox
				{
					Appearance = Appearance.Button,
					AutoSize = true,
					Location = ButtonPoints[8],
					TabIndex = 8,
					Text = "Y",
					TextAlign = ContentAlignment.BottomCenter,
					UseVisualStyleBackColor = true
				};
			B6.CheckedChanged += Buttons_CheckedChanged;

			B7 = new CheckBox
				{
					Appearance = Appearance.Button,
					AutoSize = true,
					Location = ButtonPoints[10],
					TabIndex = 8,
					Text = "L",
					TextAlign = ContentAlignment.BottomCenter,
					UseVisualStyleBackColor = true
				};
			B7.CheckedChanged += Buttons_CheckedChanged;

			B8 = new CheckBox
				{
					Appearance = Appearance.Button,
					AutoSize = true,
					Location = ButtonPoints[11],
					TabIndex = 8,
					Text = "R",
					TextAlign = ContentAlignment.BottomCenter,
					UseVisualStyleBackColor = true
				};
			B8.CheckedChanged += Buttons_CheckedChanged;


			Controls.Add(PU);
			Controls.Add(PD);
			Controls.Add(PL);
			Controls.Add(PR);
			Controls.Add(B1);
			Controls.Add(B2);
			Controls.Add(B3);
			Controls.Add(B4);
			Controls.Add(B5);
			Controls.Add(B6);
			Controls.Add(B7);
			Controls.Add(B8);
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

		private void VirtualPad_Paint(object sender, PaintEventArgs e)
		{

		}

		public override string GetMnemonic()
		{
			StringBuilder input = new StringBuilder("");
			input.Append(PU.Checked ? "U" : ".");
			input.Append(PD.Checked ? "D" : ".");
			input.Append(PL.Checked ? "L" : ".");
			input.Append(PR.Checked ? "R" : ".");

			input.Append(B1.Checked ? "s" : ".");
			input.Append(B2.Checked ? "S" : ".");
			input.Append(B3.Checked ? "B" : ".");
			input.Append(B4.Checked ? "A" : ".");
			input.Append(B5.Checked ? "X" : ".");
			input.Append(B6.Checked ? "Y" : ".");
			input.Append(B7.Checked ? "L" : ".");
			input.Append(B8.Checked ? "R" : ".");
			input.Append("|");
			return input.ToString();
		}

		public override void SetButtons(string buttons)
		{
			if (buttons.Length < 8) return;
			if (buttons[0] == '.') PU.Checked = false; else PU.Checked = true;
			if (buttons[1] == '.') PD.Checked = false; else PD.Checked = true;
			if (buttons[2] == '.') PL.Checked = false; else PL.Checked = true;
			if (buttons[3] == '.') PR.Checked = false; else PR.Checked = true;

			if (buttons[4] == '.') B1.Checked = false; else B1.Checked = true;
			if (buttons[5] == '.') B2.Checked = false; else B2.Checked = true;
			if (buttons[6] == '.') B3.Checked = false; else B3.Checked = true;
			if (buttons[7] == '.') B4.Checked = false; else B4.Checked = true;
			if (buttons[8] == '.') B5.Checked = false; else B5.Checked = true;
			if (buttons[9] == '.') B6.Checked = false; else B6.Checked = true;
			if (buttons[10] == '.') B7.Checked = false; else B7.Checked = true;
			if (buttons[11] == '.') B8.Checked = false; else B8.Checked = true;
		}

		private void Buttons_CheckedChanged(object sender, EventArgs e)
		{
			if (Global.Emulator.SystemId != "SNES")
			{
				return;
			}
			else if (sender == PU)
			{
				Global.StickyXORAdapter.SetSticky(Controller + " Up", PU.Checked);
			}
			else if (sender == PD)
			{
				Global.StickyXORAdapter.SetSticky(Controller + " Down", PD.Checked);
			}
			else if (sender == PL)
			{
				Global.StickyXORAdapter.SetSticky(Controller + " Left", PL.Checked);
			}
			else if (sender == PR)
			{
				Global.StickyXORAdapter.SetSticky(Controller + " Right", PR.Checked);
			}
			else if (sender == B1)
			{
				Global.StickyXORAdapter.SetSticky(Controller + " Select", B1.Checked);
			}
			else if (sender == B2)
			{
				Global.StickyXORAdapter.SetSticky(Controller + " Start", B2.Checked);
			}
			else if (sender == B3)
			{
				Global.StickyXORAdapter.SetSticky(Controller + " B", B3.Checked);
			}
			else if (sender == B4)
			{
				Global.StickyXORAdapter.SetSticky(Controller + " A", B4.Checked);
			}
			else if (sender == B5)
			{
				Global.StickyXORAdapter.SetSticky(Controller + " X", B5.Checked);
			}
			else if (sender == B6)
			{
				Global.StickyXORAdapter.SetSticky(Controller + " Y", B6.Checked);
			}

			else if (sender == B7)
			{
				Global.StickyXORAdapter.SetSticky(Controller + " L", B7.Checked);
			}
			else if (sender == B8)
			{
				Global.StickyXORAdapter.SetSticky(Controller + " R", B8.Checked);
			}
		}

		public override void Clear()
		{
			if (Global.Emulator.SystemId != "SNES")
			{
				return;
			}
			else
			{
				if (PU.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Up", false);
				if (PD.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Down", false);
				if (PL.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Left", false);
				if (PR.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Right", false);
				if (B1.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Select", false);
				if (B2.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Start", false);
				if (B3.Checked) Global.StickyXORAdapter.SetSticky(Controller + " B", false);
				if (B4.Checked) Global.StickyXORAdapter.SetSticky(Controller + " A", false);
				if (B5.Checked) Global.StickyXORAdapter.SetSticky(Controller + " X", false);
				if (B6.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Y", false);
				if (B7.Checked) Global.StickyXORAdapter.SetSticky(Controller + " L", false);
				if (B8.Checked) Global.StickyXORAdapter.SetSticky(Controller + " R", false);

				PU.Checked = false;
				PD.Checked = false;
				PL.Checked = false;
				PR.Checked = false;

				B1.Checked = false;
				B2.Checked = false;
				B3.Checked = false;
				B4.Checked = false;

				B5.Checked = false;
				B6.Checked = false;
				B7.Checked = false;
				B8.Checked = false;
			}
		}
	}
}
