using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	unsafe partial class MelonDS : IVideoProvider
	{
		private const int NATIVE_WIDTH = 256;
		/// <summary>
		/// for a single screen
		/// </summary>
		private const int NATIVE_HEIGHT = 192;

		public int VirtualWidth => NATIVE_WIDTH;
		public int VirtualHeight => NATIVE_HEIGHT * 2;

		public int BufferWidth => NATIVE_WIDTH;
		public int BufferHeight => NATIVE_HEIGHT * 2;

		public int VsyncNumerator => 60;

		public int VsyncDenominator => 1;

		public int BackgroundColor => 0;


		[DllImport(dllPath)]
		private static extern void VideoBuffer32bit(int* dstBuffer);

		// BizHawk needs to be able to modify the buffer when loading savestates.
		private int[] buffer = new int[NATIVE_WIDTH * NATIVE_HEIGHT * 2];
		private bool getNewBuffer = true;
		public int[] GetVideoBuffer()
		{
			if (getNewBuffer)
			{
				fixed (int* v = buffer)
				{
					VideoBuffer32bit(v);
				}
				getNewBuffer = false;
			}
			return buffer;
		}
	}
}
