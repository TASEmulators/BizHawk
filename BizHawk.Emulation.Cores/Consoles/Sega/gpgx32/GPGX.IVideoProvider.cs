using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	public partial class GPGX : IVideoProvider
	{
		public int[] GetVideoBuffer() { return vidbuff; }

		public int VirtualWidth { get { return 320; } }

		public int VirtualHeight { get { return 224; } }

		public int BufferWidth { get { return vwidth; } }

		public int BufferHeight { get { return vheight; } }

		public int BackgroundColor { get { return unchecked((int)0xff000000); } }

		private int[] vidbuff = new int[0];
		private int vwidth;
		private int vheight;

		private void UpdateVideoInitial()
		{
			// hack: you should call update_video() here, but that gives you 256x192 on frame 0
			// and we know that we only use GPGX to emulate genesis games that will always be 320x224 immediately afterwards

			// so instead, just assume a 320x224 size now; if that happens to be wrong, it'll be fixed soon enough.

			vwidth = 320;
			vheight = 224;
			vidbuff = new int[vwidth * vheight];
			for (int i = 0; i < vidbuff.Length; i++)
				vidbuff[i] = unchecked((int)0xff000000);
		}

		private unsafe void UpdateVideo()
		{
			int gppitch, gpwidth, gpheight;
			IntPtr src = IntPtr.Zero;

			LibGPGX.gpgx_get_video(out gpwidth, out gpheight, out gppitch, ref src);

			vwidth = gpwidth;
			vheight = gpheight;

			if (_settings.PadScreen320 && vwidth == 256)
				vwidth = 320;

			int xpad = (vwidth - gpwidth) / 2;
			int xpad2 = vwidth - gpwidth - xpad;

			if (vidbuff.Length < vwidth * vheight)
				vidbuff = new int[vwidth * vheight];

			int rinc = (gppitch / 4) - gpwidth;
			fixed (int* pdst_ = &vidbuff[0])
			{
				int* pdst = pdst_;
				int* psrc = (int*)src;

				for (int j = 0; j < gpheight; j++)
				{
					for (int i = 0; i < xpad; i++)
						*pdst++ = unchecked((int)0xff000000);
					for (int i = 0; i < gpwidth; i++)
						*pdst++ = *psrc++;// | unchecked((int)0xff000000);
					for (int i = 0; i < xpad2; i++)
						*pdst++ = unchecked((int)0xff000000);
					psrc += rinc;
				}
			}
		}

	}
}
