using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace BizHawk.MultiClient
{
	public class VirtualPadSMS : VirtualPad
	{

		public VirtualPadSMS()
		{
			ButtonPoints[0] = new Point(14, 2);
			ButtonPoints[1] = new Point(14, 46);
			ButtonPoints[2] = new Point(2, 24);
			ButtonPoints[3] = new Point(24, 24);
			ButtonPoints[4] = new Point(122, 24);
			ButtonPoints[5] = new Point(146, 24);

			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			this.BorderStyle = BorderStyle.Fixed3D;
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.VirtualPad_Paint);
			this.Size = new Size(174, 74);

			Point n = new Point(this.Size);

			this.PU = new CheckBox();
			this.PU.Appearance = System.Windows.Forms.Appearance.Button;
			this.PU.AutoSize = true;
			this.PU.Image = global::BizHawk.MultiClient.Properties.Resources.BlueUp;
			this.PU.ImageAlign = System.Drawing.ContentAlignment.BottomRight;
			this.PU.Location = ButtonPoints[0];
			this.PU.TabIndex = 1;
			this.PU.UseVisualStyleBackColor = true;
			this.PU.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);

			this.PD = new CheckBox();
			this.PD.Appearance = System.Windows.Forms.Appearance.Button;
			this.PD.AutoSize = true;
			this.PD.Image = global::BizHawk.MultiClient.Properties.Resources.BlueDown;
			this.PD.ImageAlign = System.Drawing.ContentAlignment.BottomRight;
			this.PD.Location = ButtonPoints[1];
			this.PD.TabIndex = 4;
			this.PD.UseVisualStyleBackColor = true;
			this.PD.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);

			this.PR = new CheckBox();
			this.PR.Appearance = System.Windows.Forms.Appearance.Button;
			this.PR.AutoSize = true;
			this.PR.Image = global::BizHawk.MultiClient.Properties.Resources.Forward;
			this.PR.ImageAlign = System.Drawing.ContentAlignment.BottomRight;
			this.PR.Location = ButtonPoints[3];
			this.PR.TabIndex = 3;
			this.PR.UseVisualStyleBackColor = true;
			this.PR.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);

			this.PL = new CheckBox();
			this.PL.Appearance = System.Windows.Forms.Appearance.Button;
			this.PL.AutoSize = true;
			this.PL.Image = global::BizHawk.MultiClient.Properties.Resources.Back;
			this.PL.ImageAlign = System.Drawing.ContentAlignment.BottomRight;
			this.PL.Location = ButtonPoints[2];
			this.PL.TabIndex = 2;
			this.PL.UseVisualStyleBackColor = true;
			this.PL.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);

			this.B1 = new CheckBox();
			this.B1.Appearance = System.Windows.Forms.Appearance.Button;
			this.B1.AutoSize = true;
			this.B1.Location = ButtonPoints[4];
			this.B1.TabIndex = 5;
			this.B1.Text = "1";
			this.B1.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.B1.UseVisualStyleBackColor = true;
			this.B1.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);

			this.B2 = new CheckBox();
			this.B2.Appearance = System.Windows.Forms.Appearance.Button;
			this.B2.AutoSize = true;
			this.B2.Location = ButtonPoints[5];
			this.B2.TabIndex = 6;
			this.B2.Text = "2";
			this.B2.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.B2.UseVisualStyleBackColor = true;
			this.B2.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);

			this.Controls.Add(this.PU);
			this.Controls.Add(this.PD);
			this.Controls.Add(this.PL);
			this.Controls.Add(this.PR);
			this.Controls.Add(this.B1);
			this.Controls.Add(this.B2);
			this.Controls.Add(this.B3);
			this.Controls.Add(this.B4);
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
			input.Append(PR.Checked ? "R" : ".");
			input.Append(PL.Checked ? "L" : ".");
			input.Append(PD.Checked ? "D" : ".");
			input.Append(PU.Checked ? "U" : ".");

			input.Append(B1.Checked ? "1" : ".");
			input.Append(B2.Checked ? "2" : ".");
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
		}

		private void Buttons_CheckedChanged(object sender, EventArgs e)
		{
			if (Global.Emulator.SystemId != "SMS" && Global.Emulator.SystemId != "GG" && Global.Emulator.SystemId != "SG") return;

			if (sender == PU)
				Global.StickyXORAdapter.SetSticky(Controller + " Up", PU.Checked);
			else if (sender == PD)
				Global.StickyXORAdapter.SetSticky(Controller + " Down", PD.Checked);
			else if (sender == PL)
				Global.StickyXORAdapter.SetSticky(Controller + " Left", PL.Checked);
			else if (sender == PR)
				Global.StickyXORAdapter.SetSticky(Controller + " Right", PR.Checked);
			else if (sender == B1)
				Global.StickyXORAdapter.SetSticky(Controller + " B1", B1.Checked);
			else if (sender == B2)
				Global.StickyXORAdapter.SetSticky(Controller + " B2", B2.Checked);
		}

		public override void Clear()
		{
			if (Global.Emulator.SystemId != "SMS" && Global.Emulator.SystemId != "GG" && Global.Emulator.SystemId != "SG") return;

			if (PU.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Up", false);
			if (PD.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Down", false);
			if (PL.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Left", false);
			if (PR.Checked) Global.StickyXORAdapter.SetSticky(Controller + " Right", false);
			if (B1.Checked) Global.StickyXORAdapter.SetSticky(Controller + " B1", false);
			if (B2.Checked) Global.StickyXORAdapter.SetSticky(Controller + " B2", false);

			PU.Checked = false;
			PD.Checked = false;
			PL.Checked = false;
			PR.Checked = false;

			B1.Checked = false;
			B2.Checked = false;
		}
	}
}
