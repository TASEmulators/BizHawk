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
		
		public Point[] ButtonPoints = new Point[8];
		

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
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			this.BorderStyle = BorderStyle.Fixed3D;
			this.Size = new Size(174, 74);
		}

		public virtual string GetMnemonic()
		{
			return "......|";
		}
	}
}
