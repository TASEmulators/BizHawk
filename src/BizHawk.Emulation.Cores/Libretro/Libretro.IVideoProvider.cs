using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Libretro
{
	public partial class LibretroEmulator : IVideoProvider
	{
		private int[] vidBuffer;
		private int vidWidth = -1, vidHeight = -1;

		private void InitVideoBuffer(int width, int height, int maxSize)
		{
			vidBuffer = new int[maxSize];
			vidWidth = width;
			vidHeight = height;
			bridge.LibretroBridge_SetVideoSize(cbHandler, maxSize);
		}

		private void UpdateVideoBuffer()
		{
			bridge.LibretroBridge_GetVideo(cbHandler, ref vidWidth, ref vidHeight, vidBuffer);
		}

		public int BackgroundColor => 0;
		public int[] GetVideoBuffer() => vidBuffer;

		public int VirtualWidth
		{
			get
			{
				var dar = av_info.aspect_ratio;
				if (dar <= 0)
				{
					return vidWidth;
				}
				if (dar > 1.0f)
				{
					return (int)(vidHeight * dar);
				}
				return vidWidth;
			}
		}

		public int VirtualHeight
		{
			get
			{
				var dar = av_info.aspect_ratio;
				if (dar <= 0)
				{
					return vidHeight;
				}
				if (dar < 1.0f)
				{
					return (int)(vidWidth / dar);
				}
				return vidHeight;
			}
		}

		public int BufferWidth => vidWidth;
		public int BufferHeight => vidHeight;

		public int VsyncNumerator { get; private set; }
		public int VsyncDenominator { get; private set; }
	}
}
