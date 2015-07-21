using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using BizHawk.Emulation.Common;
using BizHawk.Bizware.BizwareGL;

namespace BizHawk.Client.Common
{
	public class BitmapBufferVideoProvider : IVideoProvider, IDisposable
	{
		BitmapBuffer bb;
		public BitmapBufferVideoProvider(BitmapBuffer bb)
		{
			this.bb = bb;
		}

		public void Dispose()
		{
			if (bb != null) bb.Dispose();
			bb = null;
		}

		public int[] GetVideoBuffer()
		{
			return bb.Pixels;
		}

		public int VirtualWidth
		{
			get { return bb.Width; }
		}

		public int VirtualHeight
		{
			get { return bb.Height; }
		}

		public int BufferWidth
		{
			get { return bb.Width; }
		}

		public int BufferHeight
		{
			get { return bb.Height; }
		}

		public int BackgroundColor
		{
			get { return 0; }
		}
	}
}
