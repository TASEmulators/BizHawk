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

		public int VsyncNumerator { get; }

		public int VsyncDenominator { get; }

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

			//in case we're receiving high vertical resolution video, we shall double the horizontal resolution to keep the same proportions
			//(concept pioneered for snes)

			bool dotDouble = (gpheight == 448); //todo: pal?
			bool lineDouble = false;

			vwidth = gpwidth;
			vheight = gpheight;

			if (_settings.AlwaysDoubleSize)
			{
				dotDouble = true;
				if (gpheight == 224 || gpheight == 240)
				{
					lineDouble = true;
					vheight *= 2;
				}
			}

			if (_settings.PadScreen320 && vwidth == 256)
				vwidth = 320;

			int xpad = (vwidth - gpwidth) / 2;
			int xpad2 = vwidth - gpwidth - xpad;

			if (dotDouble) vwidth *= 2;

			if (vidbuff.Length < vwidth * vheight)
				vidbuff = new int[vwidth * vheight];

			int xskip = 1;
			if (dotDouble)
				xskip = 2;

			int lines = lineDouble ? 2: 1;

			for (int D = 0; D < xskip; D++)
			{
				int rinc = (gppitch / 4) - gpwidth;
				fixed (int* pdst_ = &vidbuff[0])
				{
					int* pdst = pdst_ + D;
					int* psrc = (int*)src;

					for (int j = 0; j < gpheight; j++)
					{
						int* ppsrc = psrc;
						for (int L = 0; L < lines; L++)
						{
							int* pppsrc = ppsrc;
							for (int i = 0; i < xpad; i++)
							{
								*pdst = unchecked((int)0xff000000);
								pdst += xskip;
							}
							for (int i = 0; i < gpwidth; i++)
							{
								*pdst = *pppsrc++;// | unchecked((int)0xff000000);
								pdst += xskip;
							}
							for (int i = 0; i < xpad2; i++)
							{
								*pdst = unchecked((int)0xff000000);
								pdst += xskip;
							}
							psrc = pppsrc;
						}
						psrc += rinc;
					}
				}
			}
		}

	}
}
