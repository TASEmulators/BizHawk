using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace BizHawk.MultiClient
{
	class VirtualPadGBA : VirtualPad
	{
		public VirtualPadGBA()
		{
			ButtonPoints[0] = new Point(36, 2);
			ButtonPoints[1] = new Point(36, 46);
			ButtonPoints[2] = new Point(24, 24);
			ButtonPoints[3] = new Point(46, 24);
			ButtonPoints[4] = new Point(72, 24);
			ButtonPoints[5] = new Point(94, 24);
			ButtonPoints[6] = new Point(122, 24);
			ButtonPoints[7] = new Point(146, 24);
			ButtonPoints[8] = new Point(2, 2);
			ButtonPoints[9] = new Point(166, 2);

			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			this.BorderStyle = BorderStyle.Fixed3D;
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.VirtualPad_Paint);
			this.Size = new Size(194, 74);

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
			this.B1.Text = "s";
			this.B1.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.B1.UseVisualStyleBackColor = true;
			this.B1.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);

			this.B2 = new CheckBox();
			this.B2.Appearance = System.Windows.Forms.Appearance.Button;
			this.B2.AutoSize = true;
			this.B2.Location = ButtonPoints[5];
			this.B2.TabIndex = 6;
			this.B2.Text = "S";
			this.B2.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.B2.UseVisualStyleBackColor = true;
			this.B2.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);

			this.B3 = new CheckBox();
			this.B3.Appearance = System.Windows.Forms.Appearance.Button;
			this.B3.AutoSize = true;
			this.B3.Location = ButtonPoints[6];
			this.B3.TabIndex = 7;
			this.B3.Text = "B";
			this.B3.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.B3.UseVisualStyleBackColor = true;
			this.B3.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);

			this.B4 = new CheckBox();
			this.B4.Appearance = System.Windows.Forms.Appearance.Button;
			this.B4.AutoSize = true;
			this.B4.Location = ButtonPoints[7];
			this.B4.TabIndex = 8;
			this.B4.Text = "A";
			this.B4.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.B4.UseVisualStyleBackColor = true;
			this.B4.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);

			this.B5 = new CheckBox();
			this.B5.Appearance = System.Windows.Forms.Appearance.Button;
			this.B5.AutoSize = true;
			this.B5.Location = ButtonPoints[8];
			this.B5.TabIndex = 8;
			this.B5.Text = "L";
			this.B5.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.B5.UseVisualStyleBackColor = true;
			this.B5.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);

			this.B6 = new CheckBox();
			this.B6.Appearance = System.Windows.Forms.Appearance.Button;
			this.B6.AutoSize = true;
			this.B6.Location = ButtonPoints[9];
			this.B6.TabIndex = 8;
			this.B6.Text = "R";
			this.B6.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.B6.UseVisualStyleBackColor = true;
			this.B6.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);

			this.Controls.Add(this.PU);
			this.Controls.Add(this.PD);
			this.Controls.Add(this.PL);
			this.Controls.Add(this.PR);
			this.Controls.Add(this.B1);
			this.Controls.Add(this.B2);
			this.Controls.Add(this.B3);
			this.Controls.Add(this.B4);
			this.Controls.Add(this.B5);
			this.Controls.Add(this.B6);
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
			input.Append(PU.Checked ? "U" : ".");
			input.Append(PD.Checked ? "D" : ".");
			input.Append(PL.Checked ? "L" : ".");
			input.Append(PR.Checked ? "R" : ".");

			input.Append(B1.Checked ? "s" : ".");
			input.Append(B2.Checked ? "S" : ".");
			input.Append(B3.Checked ? "B" : ".");
			input.Append(B4.Checked ? "A" : ".");
			input.Append(B5.Checked ? "L" : ".");
			input.Append(B6.Checked ? "R" : ".");
			
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
		}

		private void Buttons_CheckedChanged(object sender, EventArgs e)
		{
			if (Global.Emulator.SystemId != "GBA") return;
			if (sender == PU)
				Global.StickyXORAdapter.SetSticky("Up", PU.Checked);
			else if (sender == PD)
				Global.StickyXORAdapter.SetSticky("Down", PD.Checked);
			else if (sender == PL)
				Global.StickyXORAdapter.SetSticky("Left", PL.Checked);
			else if (sender == PR)
				Global.StickyXORAdapter.SetSticky("Right", PR.Checked);
			else if (sender == B1)
				Global.StickyXORAdapter.SetSticky("Select", B1.Checked);
			else if (sender == B2)
				Global.StickyXORAdapter.SetSticky("Start", B2.Checked);
			else if (sender == B3)
				Global.StickyXORAdapter.SetSticky("B", B3.Checked);
			else if (sender == B4)
				Global.StickyXORAdapter.SetSticky("A", B4.Checked);
			else if (sender == B5)
				Global.StickyXORAdapter.SetSticky("L", B5.Checked);
			else if (sender == B6)
				Global.StickyXORAdapter.SetSticky("R", B6.Checked);
		}

		public override void Clear()
		{
			if (Global.Emulator.SystemId != "GBA") return;

			if (PU.Checked) Global.StickyXORAdapter.SetSticky("Up", false);
			if (PD.Checked) Global.StickyXORAdapter.SetSticky("Down", false);
			if (PL.Checked) Global.StickyXORAdapter.SetSticky("Left", false);
			if (PR.Checked) Global.StickyXORAdapter.SetSticky("Right", false);
			if (B1.Checked) Global.StickyXORAdapter.SetSticky("Select", false);
			if (B2.Checked) Global.StickyXORAdapter.SetSticky("Start", false);
			if (B3.Checked) Global.StickyXORAdapter.SetSticky("B", false);
			if (B4.Checked) Global.StickyXORAdapter.SetSticky("A", false);
			if (B5.Checked) Global.StickyXORAdapter.SetSticky("L", false);
			if (B6.Checked) Global.StickyXORAdapter.SetSticky("R", false);

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
		}
	}
}
