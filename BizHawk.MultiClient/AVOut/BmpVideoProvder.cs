using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace BizHawk.MultiClient.AVOut
{
	/// <summary>
	/// an IVideoProivder wrapping a Bitmap
	/// </summary>
	public class BmpVideoProvder : IVideoProvider
	{
		Bitmap bmp;
		public BmpVideoProvder(Bitmap bmp)
		{
			this.bmp = bmp;
		}

		public int[] GetVideoBuffer()
		{
			// is there a faster way to do this?
			var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			int[] ret = new int[bmp.Width * bmp.Height];

			// won't work if stride is messed up
			System.Runtime.InteropServices.Marshal.Copy(data.Scan0, ret, 0, bmp.Width * bmp.Height);
			bmp.UnlockBits(data);
			return ret;
		}

		public int VirtualWidth
		{
			// todo: Bitmap actually has size metric data; use it
			get { return bmp.Width; }
		}

		public int BufferWidth
		{
			get { return bmp.Width; }
		}

		public int BufferHeight
		{
			get { return bmp.Height; }
		}

		public int BackgroundColor
		{
			get { return 0; }
		}
	}
}
