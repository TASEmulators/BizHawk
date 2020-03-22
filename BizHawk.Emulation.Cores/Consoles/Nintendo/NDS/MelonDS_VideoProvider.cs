using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	unsafe partial class MelonDS : IVideoProvider
	{
		public const int NATIVE_WIDTH = 256;
		/// <summary>
		/// for a single screen
		/// </summary>
		public const int NATIVE_HEIGHT = 192;

		public int VirtualWidth => BufferWidth;
		public int VirtualHeight => BufferHeight;

		public int BufferWidth => _settings.screenOptions.finalSize.Width;
		public int BufferHeight => _settings.screenOptions.finalSize.Height;

		public int VsyncNumerator => 60;

		public int VsyncDenominator => 1;

		public int BackgroundColor => 0;

		ScreenArranger screenArranger;


		[DllImport(dllPath)]
		private static extern int* GetTopScreenBuffer();
		[DllImport(dllPath)]
		private static extern int* GetBottomScreenBuffer();
		[DllImport(dllPath)]
		private static extern int GetScreenBufferSize();

		// BizHawk needs to be able to modify the buffer when loading savestates.
		private int[] buffer = null;
		private bool getNewBuffer = true;
		public int[] GetVideoBuffer()
		{
			if (getNewBuffer)
			{
				getNewBuffer = false;

				int*[] buffers = new int*[] { GetTopScreenBuffer(), GetBottomScreenBuffer() };
				int bufferSize = GetScreenBufferSize();
				buffer = screenArranger.GenerateFramebuffer(buffers, new int[] { bufferSize, bufferSize});
			}
			return buffer;
		}
	}
}
