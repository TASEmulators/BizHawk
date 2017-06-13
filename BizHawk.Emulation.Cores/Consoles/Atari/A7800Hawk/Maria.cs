using System;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	// Emulates the Atari 7800 Maria graphics chip
	public class Maria : IVideoProvider
	{
		public int _frameHz = 60;
		public int _screen_width = 454;
		public int _screen_height = 263;

		public int[] _vidbuffer;
		public int[] _palette;

		public int[] GetVideoBuffer()
		{
			return _vidbuffer;
		}

		public int VirtualWidth => 454;
		public int VirtualHeight => _screen_height;
		public int BufferWidth => 454;
		public int BufferHeight => _screen_height;
		public int BackgroundColor => unchecked((int)0xff000000);
		public int VsyncNumerator => _frameHz;
		public int VsyncDenominator => 1;

		// the Maria chip can directly access memory
		public Func<ushort, byte> ReadMemory;

		// each frame contains 263 scanlines
		// each scanline consists of 113.5 CPU cycles (fast access) which equates to 454 Maria cycles
		// In total there are 29850.5 CPU cycles (fast access) in a frame
		public void Execute(int cycle, int scanline)
		{

		}

		public void Reset()
		{
			_vidbuffer = new int[VirtualWidth * VirtualHeight];
		}

	}
}
