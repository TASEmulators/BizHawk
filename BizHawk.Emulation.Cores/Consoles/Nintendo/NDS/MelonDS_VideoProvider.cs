using System;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	unsafe partial class MelonDS : IVideoProvider
	{
		public int VirtualWidth => 256;
		public int VirtualHeight => 384;

		public int BufferWidth => 256;
		public int BufferHeight => 384;

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
			return _buffer = ScreenArranger.UprightStack(TopScreen, BottomScreen, 0);
		}
		private VideoScreen TopScreen => new VideoScreen(GetTopScreenBuffer(), 256, 192);
		private VideoScreen BottomScreen => new VideoScreen(GetBottomScreenBuffer(), 256, 192);
	}
}
