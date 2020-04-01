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

		// BizHawk needs to be able to modify the buffer when loading savestates.
		private const int SingleScreenLength = 256 * 192;
		private readonly int[] _buffer = new int[256 * 192 * 2];
		private bool _getNewBuffer = true;

		public int[] GetVideoBuffer()
		{
			if (_getNewBuffer)
			{
				_getNewBuffer = false;
				PopulateBuffer();
			}

			return _buffer;
		}

		private void PopulateBuffer()
		{
			var top = GetTopScreenBuffer();
			var bottom = GetBottomScreenBuffer();

			for (var i = 0; i < SingleScreenLength; i++)
			{
				_buffer[i] = top[i];
				_buffer[SingleScreenLength + i] = bottom[i]; 
			}
		}
	}
}
