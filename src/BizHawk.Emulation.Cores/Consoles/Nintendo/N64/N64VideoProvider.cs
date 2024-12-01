using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.N64.NativeApi;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	internal class N64VideoProvider : IVideoProvider, IDisposable
	{
		private int[] frameBuffer;
		private mupen64plusVideoApi api;
		private readonly mupen64plusApi coreAPI;

		public bool FrameFinished;

		/// <summary>
		/// Creates N64 Video system with mupen64plus backend
		/// </summary>
		public N64VideoProvider(mupen64plusApi core, VideoPluginSettings videosettings)
		{
			this.api = new mupen64plusVideoApi(core, videosettings);
			int width = 0;
			int height = 0;
			api.GetScreenDimensions(ref width, ref height);
			
			SetBufferSize(
				width > videosettings.Width ? width : videosettings.Width,
				height > videosettings.Height ? height : videosettings.Height
			);

			coreAPI = core;
			coreAPI.BeforeRender += DoVideoFrame;
			coreAPI.FrameFinished += () => { FrameFinished = true; };
		}

		public int[] GetVideoBuffer() => frameBuffer;

		public int VirtualWidth => BufferWidth;
		public int VirtualHeight => BufferHeight;
		public int BufferWidth { get; private set; }
		public int BufferHeight { get; private set; }
		public int BackgroundColor => 0;
		public int VsyncNumerator { get; internal set; }
		public int VsyncDenominator { get; internal set; }

		/// <summary>
		/// Fetches current frame buffer from mupen64
		/// </summary>
		public void DoVideoFrame()
		{
			int width = 0;
			int height = 0;
			api.GetScreenDimensions(ref width, ref height);
			if (width != BufferWidth || height != BufferHeight)
			{
				SetBufferSize(width, height);
			}

			api.Getm64pFrameBuffer(frameBuffer, ref width, ref height);
		}

		/// <summary>
		/// Sets a new width and height for frame buffer
		/// </summary>
		/// <param name="width">New width in pixels</param>
		/// <param name="height">New height in pixels</param>
		private void SetBufferSize(int width, int height)
		{
			BufferHeight = height;
			BufferWidth = width;
			frameBuffer = new int[width * height];
		}

		public void Dispose()
		{
			coreAPI.BeforeRender -= DoVideoFrame;
			api = null;
		}
	}
}
