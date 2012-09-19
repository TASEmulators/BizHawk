using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

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
			this.BorderStyle = BorderStyle.Fixed3D;
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.VirtualPad_Paint);
			this.Size = new Size(108, 34);

			Point n = new Point(this.Size);


			this.B1 = new CheckBox();
			this.B1.Appearance = System.Windows.Forms.Appearance.Button;
			this.B1.AutoSize = true;
			this.B1.Location = ButtonPoints[0];
			this.B1.TabIndex = 5;
			this.B1.Text = "Power";
			this.B1.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.B1.UseVisualStyleBackColor = true;
			this.B1.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			this.B1.ForeColor = Color.Red;

			this.B2 = new CheckBox();
			this.B2.Appearance = System.Windows.Forms.Appearance.Button;
			this.B2.AutoSize = true;
			this.B2.Location = ButtonPoints[1];
			this.B2.TabIndex = 6;
			this.B2.Text = "Reset";
			this.B2.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.B2.UseVisualStyleBackColor = true;
			this.B2.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			this.B2.ForeColor = Color.Red;

			this.Controls.Add(this.B1);
			this.Controls.Add(this.B2);
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
				if (B1.Checked == true)
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
				if (B2.Checked == true)
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
				B1.Checked = false;
				B2.Checked = false;
				Global.StickyXORAdapter.SetSticky("Reset", false);
				Global.StickyXORAdapter.SetSticky("Power", false);
			}
		}
	}
}
