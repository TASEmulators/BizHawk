using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.MultiClient.GBtools
{
	public partial class BmpView : Control
	{
		public Bitmap bmp;

		public BmpView()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.Opaque, true);
			this.BackColor = Color.Transparent;
			this.Paint += new PaintEventHandler(BmpView_Paint);
			this.SizeChanged += new EventHandler(BmpView_SizeChanged);
		}

		void BmpView_Paint(object sender, PaintEventArgs e)
		{
			if (bmp != null)
			{
				e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
				e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
				e.Graphics.DrawImageUnscaled(bmp, 0, 0);
			}
		}

		void BmpView_SizeChanged(object sender, EventArgs e)
		{
			if (bmp != null)
			{
				bmp.Dispose();
				bmp = null;
			}
			if (Width == 0 || Height == 0)
				return;
			bmp = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
		}
		
	}
}
