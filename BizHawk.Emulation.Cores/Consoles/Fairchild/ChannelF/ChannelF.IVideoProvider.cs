using System;
using BizHawk.Common;
using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	public partial class ChannelF : IVideoProvider, IRegionable
	{
		public int _frameHz = 60;

		public int[] CroppedBuffer = new int[(128*64) * 2]; //new int[102 * 58];

		#region IVideoProvider

		public int VirtualWidth => BufferWidth * 2;
		public int VirtualHeight => (int)((double)BufferHeight * 1.5) * 2;
		public int BufferWidth => 128;// 102;
		public int BufferHeight => 64; // 58;
		public int BackgroundColor => Colors.ARGB(0x00, 0x00, 0x00);
		public int VsyncNumerator => _frameHz;
		public int VsyncDenominator => 1;

		public int[] GetVideoBuffer()
		{
			BuildFrame();
			/*
			for (int i = 0; i < frameBuffer.Length; i++)
			{
				CroppedBuffer[i] = frameBuffer[i];
				CroppedBuffer[i + frameBuffer.Length] = frameBuffer[i];
			}

			return CroppedBuffer;
			*/
			return frameBuffer;

			// crop to visible area
			var lR = 4;
			var rR = 128 - BufferWidth - lR;
			var tR = 4;
			var bR = 64 - BufferHeight - tR;
			var sW = 128 - lR - rR;
			var startP = 128 * tR;
			var endP = 128 * bR;

			int index2 = 0;

			// line by line
			for (int i = startP; i < CroppedBuffer.Length - endP; i += sW + lR + rR)
			{
				// each pixel in each line
				for (int p = lR; p < sW + lR + rR - rR; p++)
				{
					if (index2 == CroppedBuffer.Length)
						break;
					CroppedBuffer[index2++] = frameBuffer[i + p];
				}
			}

			return CroppedBuffer;
		}

		#endregion

		#region IRegionable

		public DisplayType Region => DisplayType.NTSC;

		#endregion
	}
}
