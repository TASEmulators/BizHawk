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

		public int BufferWidth => _settings.ScreenOptions.Width();
		public int BufferHeight => _settings.ScreenOptions.Height();

		public int VsyncNumerator => 60;

		public int VsyncDenominator => 1;

		public int BackgroundColor => 0;

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

				_buffer = _settings.ScreenOptions switch
				{
					VideoScreenOptions.TopOnly => ScreenArranger.Copy(TopScreen),
					VideoScreenOptions.SideBySideLR => ScreenArranger.SideBySide(TopScreen, BottomScreen),
					VideoScreenOptions.SideBySideRL => ScreenArranger.SideBySide(BottomScreen, TopScreen),
				_ => ScreenArranger.Stack(TopScreen, BottomScreen, 0)
				};
			}

			return _buffer;
		}

		private VideoScreen TopScreen => new VideoScreen(GetTopScreenBuffer(), NativeWidth, NativeHeight);
		private VideoScreen BottomScreen => new VideoScreen(GetBottomScreenBuffer(), NativeWidth, NativeHeight);
	}
}
