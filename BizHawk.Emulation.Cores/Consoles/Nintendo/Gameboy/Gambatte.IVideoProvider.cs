using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class Gameboy
	{
		/// <summary>
		/// stored image of most recent frame
		/// </summary>
		private int[] VideoBuffer = new int[160 * 144];

		public int[] GetVideoBuffer()
		{
			return VideoBuffer;
		}

		public int VirtualWidth
		{
			// only sgb changes this, which we don't emulate here
			get { return 160; }
		}

		public int VirtualHeight
		{
			get { return 144; }
		}

		public int BufferWidth
		{
			get { return 160; }
		}

		public int BufferHeight
		{
			get { return 144; }
		}

		public int BackgroundColor
		{
			get { return 0; }
		}
	}
}
