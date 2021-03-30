using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
	public partial class MAME : IVideoProvider
	{
		public int[] GetVideoBuffer() => _frameBuffer;
		public int BackgroundColor => 0;
		public int VirtualWidth { get; private set; } = 320;
		public int VirtualHeight { get; private set; } = 240;
		public int BufferWidth { get; private set; } = 320;
		public int BufferHeight { get; private set; } = 240;
		public int VsyncNumerator { get; private set; } = 60;
		public int VsyncDenominator { get; private set; } = 1;

		private int[] _frameBuffer = new int[0];

		private void UpdateFramerate()
		{
			VsyncNumerator = 1000000000;
			long refresh = (long)LibMAME.mame_lua_get_double(MAMELuaCommand.GetRefresh);
			VsyncDenominator = (int)(refresh / 1000000000);
		}

		private void UpdateAspect()
		{
			int x = (int)LibMAME.mame_lua_get_double(MAMELuaCommand.GetBoundX);
			int y = (int)LibMAME.mame_lua_get_double(MAMELuaCommand.GetBoundY);
			VirtualHeight = BufferWidth > BufferHeight * x / y
				? BufferWidth * y / x
				: BufferHeight;
			VirtualWidth = VirtualHeight * x / y;
		}

		private void UpdateVideo()
		{
			BufferWidth = LibMAME.mame_lua_get_int(MAMELuaCommand.GetWidth);
			BufferHeight = LibMAME.mame_lua_get_int(MAMELuaCommand.GetHeight);
			int expectedSize = BufferWidth * BufferHeight;
			int bytesPerPixel = 4;
			IntPtr ptr = LibMAME.mame_lua_get_string(MAMELuaCommand.GetPixels, out var lengthInBytes);

			if (ptr == IntPtr.Zero)
			{
				Console.WriteLine("LibMAME ERROR: frame buffer pointer is null");
				return;
			}

			if (expectedSize * bytesPerPixel != lengthInBytes)
			{
				Console.WriteLine(
					"LibMAME ERROR: frame buffer has wrong size\n" +
					$"width:    { BufferWidth                  } pixels\n" +
					$"height:   { BufferHeight                 } pixels\n" +
					$"expected: { expectedSize * bytesPerPixel } bytes\n" +
					$"received: { lengthInBytes                } bytes\n");
				return;
			}

			_frameBuffer = new int[expectedSize];
			Marshal.Copy(ptr, _frameBuffer, 0, expectedSize);

			if (!LibMAME.mame_lua_free_string(ptr))
			{
				Console.WriteLine("LibMAME ERROR: frame buffer wasn't freed");
			}
		}
	}
}