using System;
using System.Drawing;
using System.Windows.Forms;
using System.Text;

namespace BizHawk.MultiClient
{
	public class VirtualPadSNESControl : VirtualPad
	{

		public VirtualPadSNESControl()
		{
			ButtonPoints[0] = new Point(2, 2);
			ButtonPoints[1] = new Point(56, 2);


			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			BorderStyle = BorderStyle.Fixed3D;
			Paint += VirtualPad_Paint;
			Size = new Size(108, 34);

			B1 = new CheckBox
				{
					Appearance = Appearance.Button,
					AutoSize = true,
					Location = ButtonPoints[0],
					TabIndex = 5,
					Text = "Power",
					TextAlign = ContentAlignment.BottomCenter,
					UseVisualStyleBackColor = true
				};
			B1.CheckedChanged += Buttons_CheckedChanged;
			B1.ForeColor = Color.Red;

			B2 = new CheckBox
				{
					Appearance = Appearance.Button,
					AutoSize = true,
					Location = ButtonPoints[1],
					TabIndex = 6,
					Text = "Reset",
					TextAlign = ContentAlignment.BottomCenter,
					UseVisualStyleBackColor = true
				};
			B2.CheckedChanged += Buttons_CheckedChanged;
			B2.ForeColor = Color.Red;

			Controls.Add(B1);
			Controls.Add(B2);
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
			if (B1.Checked)
			{
				input.Append('P');
			}
			else if (B2.Checked)
			{
				input.Append('r');
			}
			else
			{
				input.Append('.');
			}
			input.Append("|");
			return input.ToString();
		}

		public override void SetButtons(string buttons)
		{
			if (buttons.Length < 1) return;
			if (buttons[0] == '.' || buttons[0] == '0')
			{
				B2.Checked = false;
			}
			else if (buttons[0] == 'P')
			{
				B1.Checked = true;
			}
			else
			{
				B2.Checked = true;
			}
		}

		private void Buttons_CheckedChanged(object sender, EventArgs e)
		{
			if (Global.Emulator.SystemId != "SNES")
			{
				return;
			}
			else if (sender == B1)
			{
				Global.StickyXORAdapter.SetSticky("Power", B1.Checked);
				if (B1.Checked)
				{
					B1.BackColor = Color.Pink;
				}
				else
				{
					B1.BackColor = SystemColors.Control;
				}
			}
			else if (sender == B2)
			{
				Global.StickyXORAdapter.SetSticky("Reset", B2.Checked);
				if (B2.Checked)
				{
					B2.BackColor = Color.Pink;
				}
				else
				{
					B2.BackColor = SystemColors.Control;
				}
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
				if (B1.Checked) Global.StickyXORAdapter.SetSticky("Reset", false);
				if (B2.Checked) Global.StickyXORAdapter.SetSticky("Power", false);

				B1.Checked = false;
				B2.Checked = false;
			}
		}
	}
}
