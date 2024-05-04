using System;
using BizHawk.Emulation.Common;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Stella
{
	public partial class Stella : IVideoProvider
	{
		public int[] GetVideoBuffer() => _vidBuff;

		public int VirtualWidth => 160;

		public int VirtualHeight => 192;

		public int BufferWidth => _vwidth;

		public int BufferHeight => _vheight;

		public int BackgroundColor => unchecked((int)0xff000000);

		public int VsyncNumerator { get; }

		public int VsyncDenominator { get; }

		private int[] _vidBuff = new int[0];
		private int _vwidth;
		private int _vheight;

		private unsafe void UpdateVideo()
		{
			using (_elf.EnterExit())
			{
				IntPtr src = IntPtr.Zero;

				_vidBuff = new int[VirtualWidth * VirtualHeight];
				for (int i = 0; i < _vidBuff.Length; i++)
				{
					_vidBuff[i] = unchecked((int)0xff000000);
				}

				Core.stella_get_video(out var gpwidth, out var gpheight, out var gppitch, ref src);

				_vwidth = gpwidth;
				_vheight = gpheight;

				int xpad = (_vwidth - gpwidth) / 2;
				int xpad2 = _vwidth - gpwidth - xpad;

                // for (int i = 0; i < _vwidth * _vheight; i++)  _vidBuff[i] = src[i];
                

				// if (_vidBuff.Length < _vwidth * _vheight)
				// 	_vidBuff = new int[_vwidth * _vheight];

				// int rinc = (gppitch / 4) - gpwidth;
				// fixed (int* pdst_ = _vidBuff)
				// {
				// 	int* pdst = pdst_;
				// 	int* psrc = (int*)src;

				// 	for (int j = 0; j < gpheight; j++)
				// 	{
				// 		for (int i = 0; i < xpad; i++)
				// 			*pdst++ = unchecked((int)0xff000000);
				// 		for (int i = 0; i < gpwidth; i++)
				// 			*pdst++ = *psrc++;
				// 		for (int i = 0; i < xpad2; i++)
				// 			*pdst++ = unchecked((int)0xff000000);
				// 		psrc += rinc;
				// 	}
				// }
			}
		}

	}
}
