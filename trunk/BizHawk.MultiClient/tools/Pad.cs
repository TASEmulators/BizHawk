using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace BizHawk.MultiClient
{
	public class VirtualPad : Panel
	{
		public enum ControllerType { NES, SMS, PCE }
		Point[] NESPoints = new Point[8];
		public ControllerType Controller;

		public CheckBox PU;
		public CheckBox PD;
		public CheckBox PL;
		public CheckBox PR;
		public CheckBox B1;
		public CheckBox B2;
		public CheckBox B3;
		public CheckBox B4;
		public CheckBox B5;
		public CheckBox B6;
		public CheckBox B7;
		public CheckBox B8;

		public VirtualPad()
		{
			Controller = ControllerType.NES; //Default
			NESPoints[0] = new Point(14, 2);
			NESPoints[1] = new Point(14, 46);
			NESPoints[2] = new Point(2, 24);
			NESPoints[3] = new Point(24, 24);
			NESPoints[4] = new Point(52, 24);
			NESPoints[5] = new Point(74, 24);
			NESPoints[6] = new Point(122, 24);
			NESPoints[7] = new Point(146, 24);

			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			this.BorderStyle = BorderStyle.Fixed3D;
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.VirtualPad_Paint);
			this.Size = new Size(174, 164);

			Point n = new Point(this.Size);

			this.PU = new CheckBox();
			this.PU.Appearance = System.Windows.Forms.Appearance.Button;
			this.PU.AutoSize = true;
			this.PU.Image = global::BizHawk.MultiClient.Properties.Resources.BlueUp;
			this.PU.ImageAlign = System.Drawing.ContentAlignment.BottomRight;
			this.PU.Location = NESPoints[0];
			this.PU.TabIndex = 1;
			this.PU.UseVisualStyleBackColor = true; ;

			this.PD = new CheckBox();
			this.PD.Appearance = System.Windows.Forms.Appearance.Button;
			this.PD.AutoSize = true;
			this.PD.Image = global::BizHawk.MultiClient.Properties.Resources.BlueDown;
			this.PD.ImageAlign = System.Drawing.ContentAlignment.BottomRight;
			this.PD.Location = NESPoints[1];
			this.PD.TabIndex = 4;
			this.PD.UseVisualStyleBackColor = true;

			this.PR = new CheckBox();
			this.PR.Appearance = System.Windows.Forms.Appearance.Button;
			this.PR.AutoSize = true;
			this.PR.Image = global::BizHawk.MultiClient.Properties.Resources.Forward;
			this.PR.ImageAlign = System.Drawing.ContentAlignment.BottomRight;
			this.PR.Location = NESPoints[3];
			this.PR.TabIndex = 3;
			this.PR.UseVisualStyleBackColor = true;

			this.PL = new CheckBox();
			this.PL.Appearance = System.Windows.Forms.Appearance.Button;
			this.PL.AutoSize = true;
			this.PL.Image = global::BizHawk.MultiClient.Properties.Resources.Back;
			this.PL.ImageAlign = System.Drawing.ContentAlignment.BottomRight;
			this.PL.Location = NESPoints[2];
			this.PL.TabIndex = 2;
			this.PL.UseVisualStyleBackColor = true;

			this.B1 = new CheckBox();
			this.B1.Appearance = System.Windows.Forms.Appearance.Button;
			this.B1.AutoSize = true;
			this.B1.Location = NESPoints[4];
			this.B1.TabIndex = 5;
			this.B1.Text = "s";
			this.B1.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.B1.UseVisualStyleBackColor = true;

			this.B2 = new CheckBox();
			this.B2.Appearance = System.Windows.Forms.Appearance.Button;
			this.B2.AutoSize = true;
			this.B2.Location = NESPoints[5];
			this.B2.TabIndex = 6;
			this.B2.Text = "S";
			this.B2.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.B2.UseVisualStyleBackColor = true;

			this.B3 = new CheckBox();
			this.B3.Appearance = System.Windows.Forms.Appearance.Button;
			this.B3.AutoSize = true;
			this.B3.Location = NESPoints[6];
			this.B3.TabIndex = 7;
			this.B3.Text = "B";
			this.B3.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.B3.UseVisualStyleBackColor = true;

			this.B4 = new CheckBox();
			this.B4.Appearance = System.Windows.Forms.Appearance.Button;
			this.B4.AutoSize = true;
			this.B4.Location = NESPoints[7];
			this.B4.TabIndex = 8;
			this.B4.Text = "A";
			this.B4.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.B4.UseVisualStyleBackColor = true;

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

		public string GetMnemonic()
		{
			switch (Controller)
			{
				default:
				case ControllerType.NES:
					return GetMnemonicNES();
				case ControllerType.PCE:
					return GetMnemonicPCE();
				case ControllerType.SMS:
					return GetMnemonicSMS();
			}
		}

		public string GetMnemonicNES()
		{
			StringBuilder input = new StringBuilder("|0|"); //TODO: Reset button
			input.Append(PR.Checked ? "R" : ".");
			input.Append(PL.Checked ? "L" : ".");
			input.Append(PD.Checked ? "D" : ".");
			input.Append(PU.Checked ? "U" : ".");

			input.Append(B2.Checked ? "S" : ".");
			input.Append(B1.Checked ? "s" : ".");
			input.Append(B3.Checked ? "B" : ".");
			input.Append(B4.Checked ? "A" : ".");
			input.Append("|");
			return input.ToString();
		}

		private string GetMnemonicPCE()
		{
			return "";
		}

		private string GetMnemonicSMS()
		{
			return "";
		}
	}
}
