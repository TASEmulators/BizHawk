using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	public partial class ChannelF : IVideoProvider, IRegionable
	{
		public int _frameHz = 60;

		public int[] CroppedBuffer = new int[102 * 58];

		#region IVideoProvider

		public int VirtualWidth => BufferWidth * 2;
		public int VirtualHeight => (int)((double)BufferHeight * 1.3) * 2;
		public int BufferWidth => 102; //128
		public int BufferHeight => 58; //64
		public int BackgroundColor => Colors.ARGB(0x00, 0x00, 0x00);
		public int VsyncNumerator => _frameHz;
		public int VsyncDenominator => 1;

		public int[] GetVideoBuffer()
		{
			BuildFrame();

			var lBorderWidth = 4;
			var rBorderWidth = 128 - 102 - lBorderWidth;
			var tBorderHeight = 4;
			var bBorderHeight = 64 - 58 - tBorderHeight;
			var startP = 128 * tBorderHeight;
			var endP = 128 * bBorderHeight;

			int index = 0;

			for (int i = startP; i < frameBuffer.Length - endP; i += 128)
			{
				for (int p = lBorderWidth; p < 128 - rBorderWidth; p++)
				{
					if (index == CroppedBuffer.Length)
						break;

					CroppedBuffer[index++] = FPalette[frameBuffer[i + p]];
				}
			}

			return CroppedBuffer;

			//return frameBuffer;

			
		}

		#endregion

		#region IRegionable

		public DisplayType Region => DisplayType.NTSC;

		#endregion
	}
}
