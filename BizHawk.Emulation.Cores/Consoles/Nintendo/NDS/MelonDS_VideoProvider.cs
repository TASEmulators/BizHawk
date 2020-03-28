using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	unsafe partial class MelonDS : IVideoProvider
	{
		public const int NativeWidth = 256;

		/// <summary>
		/// for a single screen
		/// </summary>
		public const int NativeHeight = 192;

		public int VirtualWidth => BufferWidth;
		public int VirtualHeight => BufferHeight;

		public int BufferWidth => _settings.ScreenOptions.FinalSize.Width;
		public int BufferHeight => _settings.ScreenOptions.FinalSize.Height;

		public int VsyncNumerator => 60;

		public int VsyncDenominator => 1;

		public int BackgroundColor => 0;

		private readonly ScreenArranger _screenArranger;

		[DllImport(dllPath)]
		private static extern int* GetTopScreenBuffer();
		[DllImport(dllPath)]
		private static extern int* GetBottomScreenBuffer();
		[DllImport(dllPath)]
		private static extern int GetScreenBufferSize();

		// BizHawk needs to be able to modify the buffer when loading savestates.
		private int[] _buffer;
		private bool _getNewBuffer = true;
		public int[] GetVideoBuffer()
		{
			if (_getNewBuffer)
			{
				_getNewBuffer = false;

				int*[] buffers = { GetTopScreenBuffer(), GetBottomScreenBuffer() };
				int bufferSize = GetScreenBufferSize();
				_buffer = _screenArranger.GenerateFramebuffer(buffers, new[] { bufferSize, bufferSize });
			}
			return _buffer;
		}
	}
}
