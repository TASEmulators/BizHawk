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
		public Bitmap bmp { get; private set; }
		bool scaled;

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
			ChangeBitmapSize(1, 1);
		}

		void BmpView_SizeChanged(object sender, EventArgs e)
		{
			scaled = !(bmp.Width == Width && bmp.Height == Height);
		}

		void BmpView_Paint(object sender, PaintEventArgs e)
		{
			if (scaled)
			{
				e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
				e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
				e.Graphics.DrawImage(bmp, 0, 0, Width, Height);
			}
			else
			{
				e.Graphics.DrawImageUnscaled(bmp, 0, 0);
			}
		}

		public void ChangeBitmapSize(Size s)
		{
			ChangeBitmapSize(s.Width, s.Height);
		}

		public void ChangeBitmapSize(int w, int h)
		{
			if (bmp != null)
			{
				if (w == bmp.Width && h == bmp.Height)
					return;
				bmp.Dispose();
			}
			bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			BmpView_SizeChanged(null, null);
			Refresh();
		}

		public void Clear()
		{
			var lockdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			//Win32.ZeroMemory(lockdata.Scan0, (uint)(lockdata.Height * lockdata.Stride));
			Win32.MemSet(lockdata.Scan0, 0xff, (uint)(lockdata.Height * lockdata.Stride));
			bmp.UnlockBits(lockdata);
			Refresh();
		}

		// kill unused props
		[Browsable(false)]
		public override Color BackColor { get { return base.BackColor; } set { base.BackColor = value; } }
		[Browsable(false)]
		public override string Text { get { return base.Text; } set { base.Text = value; } }
	}
}
