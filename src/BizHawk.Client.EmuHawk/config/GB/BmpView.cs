using System;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public class BmpView : Control
	{
		[Browsable(false)]
		public Bitmap Bmp { get; private set; }

		private bool _scaled;

		public BmpView()
		{
			if (DesignMode)
			{
				SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			}
			else
			{
				SetStyle(ControlStyles.AllPaintingInWmPaint, true);
				SetStyle(ControlStyles.UserPaint, true);
				SetStyle(ControlStyles.DoubleBuffer, true);
				SetStyle(ControlStyles.SupportsTransparentBackColor, true);
				SetStyle(ControlStyles.Opaque, true);
				BackColor = Color.Transparent;
				Paint += BmpView_Paint;
				SizeChanged += BmpView_SizeChanged;
				ChangeBitmapSize(1, 1);
			}
		}

		private void BmpView_SizeChanged(object sender, EventArgs e)
		{
			_scaled = !(Bmp.Width == Width && Bmp.Height == Height);
		}

		private void BmpView_Paint(object sender, PaintEventArgs e)
		{
			if (_scaled)
			{
				e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
				e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
				e.Graphics.DrawImage(Bmp, 0, 0, Width, Height);
			}
			else
			{
				e.Graphics.DrawImageUnscaled(Bmp, 0, 0);
			}
		}

		public void ChangeBitmapSize(Size s) => ChangeBitmapSize(s.Width, s.Height);

		public void ChangeBitmapSize(int w, int h)
		{
			if (Bmp != null)
			{
				if (w == Bmp.Width && h == Bmp.Height)
				{
					return;
				}

				Bmp.Dispose();
			}


			Bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
			BmpView_SizeChanged(null, null);
			Refresh();
		}

		public void Clear()
		{
			var lockBits = Bmp.LockBits(new Rectangle(0, 0, Bmp.Width, Bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			Win32Imports.MemSet(lockBits.Scan0, 0xff, (uint)(lockBits.Height * lockBits.Stride));
			Bmp.UnlockBits(lockBits);
			Refresh();
		}

		public void SaveFile()
		{
			Bmp.SaveAsFile(GlobalWin.Game, "Palettes", GlobalWin.Emulator.SystemId, GlobalWin.Config.PathEntries, this);
		}
	}
}
