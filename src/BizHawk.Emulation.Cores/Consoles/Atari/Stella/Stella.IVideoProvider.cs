using System;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Atari.Atari2600;
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

		private void UpdateVideoInitial()
		{
			// hack: you should call update_video() here, but that gives you 256x192 on frame 0
			// and we know that we only use GPGX to emulate genesis games that will always be 320x224 immediately afterwards

			// so instead, just assume a 320x224 size now; if that happens to be wrong, it'll be fixed soon enough.

			_vwidth = 320;
			_vheight = 224;
			_vidBuff = new int[_vwidth * _vheight];
			for (int i = 0; i < _vidBuff.Length; i++)
			{
				_vidBuff[i] = unchecked((int)0xff000000);
			}
		}

		private readonly byte[] TwoBitToEightBitTable = new byte[] { 0, 85, 171, 255 };
		private readonly byte[] ThreeBitToEightBitTable = new byte[] { 0, 36, 73, 109, 146, 182, 219, 255 };

		private unsafe void UpdateVideo()
		{
			if (Frame == 0)
			{
				UpdateVideoInitial();
				return;
			}

			using (_elf.EnterExit())
			{
				IntPtr src = IntPtr.Zero;

				Core.stella_get_video(out var width, out var height, out var pitch, ref src);

                _vwidth = width;
				_vheight = height;
			 
			    byte* buffer = (byte*)src.ToPointer();

				if (_vidBuff.Length < _vwidth * _vheight)
					_vidBuff = new int[_vwidth * _vheight];

                if (Region == DisplayType.NTSC)
				 for (int i = 0; i < _vidBuff.Length; i++) _vidBuff[i] = NTSCPalette[buffer[i]];
				
				if (Region == DisplayType.PAL)
				 for (int i = 0; i < _vidBuff.Length; i++) _vidBuff[i] = PALPalette[buffer[i]];

				if (Region == DisplayType.SECAM)
				 for (int i = 0; i < _vidBuff.Length; i++) _vidBuff[i] = SecamPalette[buffer[i]];
			}
		}

	}
}
