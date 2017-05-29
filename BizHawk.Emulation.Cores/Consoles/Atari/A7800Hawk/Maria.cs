using System;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	// Emulates the Atari 7800 Maria graphics chip
	public class Maria : IVideoProvider
	{
		public int _frameHz;

		public int[] _vidbuffer;
		public int[] _palette;

		public int[] GetVideoBuffer()
		{
			return _vidbuffer;
		}

		public int VirtualWidth => 275;
		public int VirtualHeight => BufferHeight;
		public int BufferWidth { get; private set; }
		public int BufferHeight { get; private set; }
		public int BackgroundColor => unchecked((int)0xff000000);
		public int VsyncNumerator => _frameHz;
		public int VsyncDenominator => 1;

		// the Maria chip can directly access memory
		public Func<ushort, byte> ReadMemory;

		// there are 4 maria cycles in a CPU cycle (fast access, both NTSC and PAL)
		// if the 6532 or TIA are accessed (PC goes to one of those addresses) the next access will be slower by 1/2 a CPU cycle
		// i.e. it will take 6 Maria cycles instead of 4
		public bool slow_access = false;

		// each frame contains 263 scanlines
		// each scanline consists of 113.5 CPU cycles (fast access) which equates to 454 Maria cycles
		// In total there are 29850.5 CPU cycles (fast access) in a frame
		public void Execute(int cycle, int scanline)
		{

		}

		public void Reset()
		{
			
		}

	}
}
