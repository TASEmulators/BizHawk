using BizHawk.Emulation.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores
{
	public class SideBySideVideo : IVideoProvider
	{
		public SideBySideVideo(IVideoProvider l, IVideoProvider r)
		{
			_l = l;
			_r = r;
			_buff = new int[BufferWidth * BufferHeight];
		}

		private static unsafe void Blit(int* src, int srcp, int* dst, int dstp, int w, int h)
		{
			int* srcend = src + h * srcp;
			while (src < srcend)
			{
				for (int j = 0; j < w; j++)
					dst[j] = src[j];
				src += srcp;
				dst += dstp;
			}
		}

		public unsafe void Fetch()
		{
			int h = BufferHeight;
			int w = BufferWidth;

			fixed(int* _pl = _l.GetVideoBuffer(), _pr = _r.GetVideoBuffer(), _pd = _buff)
			{
				Blit(_pl, w / 2, _pd, w, w / 2, h);
				Blit(_pr, w / 2, _pd + w / 2, w, w / 2, h);
			}
		}
		private readonly IVideoProvider _l;
		private readonly IVideoProvider _r;
		private int[] _buff;
		public int BackgroundColor => _l.BackgroundColor;
		public int BufferHeight => _l.BufferHeight;
		public int BufferWidth => _l.BufferWidth * 2;
		public int VirtualHeight => _l.VirtualHeight;
		public int VirtualWidth => _l.VirtualWidth * 2;
		public int VsyncDenominator => _l.VsyncDenominator;
		public int VsyncNumerator => _l.VsyncNumerator;

		public int[] GetVideoBuffer()
		{
			return _buff;
		}
	}
}
