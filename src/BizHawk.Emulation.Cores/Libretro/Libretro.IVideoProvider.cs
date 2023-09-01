using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Libretro
{
	public partial class LibretroHost : IVideoProvider
	{
		private int[] _vidBuffer;
		private int _vidWidth, _vidHeight;

		private void InitVideoBuffer(int width, int height, int maxSize)
		{
			_vidBuffer = new int[maxSize];
			_vidWidth = width;
			_vidHeight = height;
			bridge.LibretroBridge_SetVideoSize(cbHandler, maxSize);
		}

		private void UpdateVideoBuffer() => bridge.LibretroBridge_GetVideo(cbHandler, out _vidWidth, out _vidHeight, _vidBuffer);

		public int BackgroundColor => 0;
		public int[] GetVideoBuffer() => _vidBuffer;

		public int VirtualWidth
		{
			get
			{
				float dar = av_info.geometry.aspect_ratio;
				if (dar <= 0)
				{
					return _vidWidth;
				}
				if (dar > 1.0f)
				{
					return (int)(_vidHeight * dar);
				}
				return _vidWidth;
			}
		}

		public int VirtualHeight
		{
			get
			{
				float dar = av_info.geometry.aspect_ratio;
				if (dar <= 0)
				{
					return _vidHeight;
				}
				if (dar < 1.0f)
				{
					return (int)(_vidWidth / dar);
				}
				return _vidHeight;
			}
		}

		public int BufferWidth => _vidWidth;
		public int BufferHeight => _vidHeight;

		public int VsyncNumerator { get; private set; }
		public int VsyncDenominator { get; private set; }
	}
}
