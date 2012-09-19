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
		
		public Point[] ButtonPoints = new Point[16];
		

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
		public string Controller;

		public VirtualPad()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			this.BorderStyle = BorderStyle.Fixed3D;
			this.Size = new Size(174, 74);
			Controller = "P1";
		}

		public virtual string GetMnemonic()
		{
			return "......|";
		}

		public virtual void Clear()
		{
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

		public virtual void SetButtons(string buttons)
		{

		}
	}
}
