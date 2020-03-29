using System;
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

		public int BufferWidth => _settings.Width();
		public int BufferHeight => _settings.Height();

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
			if (!_getNewBuffer) return _buffer;
			_getNewBuffer = false;
			return _buffer = _settings.ScreenOptions switch
			{
				VideoScreenOptions.Default => ScreenArranger.UprightStack(TopScreen, BottomScreen, _settings.ScreenGap),
				VideoScreenOptions.TopOnly => ScreenArranger.Copy(TopScreen),
				VideoScreenOptions.SideBySideLR => ScreenArranger.UprightSideBySide(TopScreen, BottomScreen, _settings.ScreenGap),
				VideoScreenOptions.SideBySideRL => ScreenArranger.UprightSideBySide(BottomScreen, TopScreen, _settings.ScreenGap),
				VideoScreenOptions.Rotate90 => ScreenArranger.Rotate90Stack(TopScreen, BottomScreen, _settings.ScreenGap),
				VideoScreenOptions.Rotate270 => ScreenArranger.Rotate270Stack(BottomScreen, TopScreen, _settings.ScreenGap),
				_ => throw new InvalidOperationException()
			};
		}

		private VideoScreen TopScreen => new VideoScreen(GetTopScreenBuffer(), NativeWidth, NativeHeight);
		private VideoScreen BottomScreen => new VideoScreen(GetBottomScreenBuffer(), NativeWidth, NativeHeight);
	}
}
