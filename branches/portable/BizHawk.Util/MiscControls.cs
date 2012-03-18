using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace BizHawk.Core
{
	public class HorizontalLine : Control
	{
		public HorizontalLine()
		{
		}

		protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
		{
			base.SetBoundsCore(x, y, width, 2, specified);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			ControlPaint.DrawBorder3D(e.Graphics, 0, 0, Width, 2, Border3DStyle.Etched);
		}
	}

}