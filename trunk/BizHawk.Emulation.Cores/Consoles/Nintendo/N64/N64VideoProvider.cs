using System;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.N64.NativeApi;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	internal class N64VideoProvider : IVideoProvider, IDisposable
	{
		private int[] frameBuffer;
		private mupen64plusVideoApi api;
		private mupen64plusApi coreAPI;

		public bool IsVIFrame;

		/// <summary>
		/// Creates N64 Video system with mupen64plus backend
		/// </summary>
		/// <param name="api">mupen64plus DLL that is used</param>
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
			coreAPI.BeforeRender += () => { IsVIFrame = true; };
		}

		public int[] GetVideoBuffer()
		{
			return frameBuffer;
		}

		public int VirtualWidth { get { return BufferWidth; } }
		public int VirtualHeight { get { return BufferHeight; } }
		public int BufferWidth { get; private set; }
		public int BufferHeight { get; private set; }
		public int BackgroundColor { get { return 0; } }

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
