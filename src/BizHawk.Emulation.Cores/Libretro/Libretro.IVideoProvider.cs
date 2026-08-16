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

		private void UpdateVideoBuffer()
		{
			bridge.LibretroBridge_GetVideo(cbHandler, out _vidWidth, out _vidHeight, _vidBuffer);
		}

		public int BackgroundColor => 0;
		public int[] GetVideoBuffer() => _vidBuffer;

		public int VirtualWidth
		{
			get
			{
				var dar = av_info.geometry.aspect_ratio;
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
				var dar = av_info.geometry.aspect_ratio;
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

	public class Libretro_IGLTextureProvider : IGLTextureProvider
	{
		private readonly LibretroHost _host;

		public Libretro_IGLTextureProvider(LibretroHost host)
		{
			_host = host;
		}

		public int GetGLTexture() => (int)_host.fboObject.TextureId;

		public int[] GetVideoBuffer()
		{
			var videoBuffer = _host.GetVideoBuffer();
			_host.openGLProvider.ReadFBO(_host.fboObject, BufferWidth, BufferHeight, videoBuffer.AsSpan());
			return videoBuffer;
		}

		public int VirtualWidth => _host.VirtualWidth;
		public int VirtualHeight => _host.VirtualHeight;
		public int BufferWidth => _host.BufferWidth;
		public int BufferHeight => _host.BufferHeight;
		public int VsyncNumerator => _host.VsyncNumerator;
		public int VsyncDenominator => _host.VsyncDenominator;
		public int BackgroundColor => _host.BackgroundColor;
	}
}
