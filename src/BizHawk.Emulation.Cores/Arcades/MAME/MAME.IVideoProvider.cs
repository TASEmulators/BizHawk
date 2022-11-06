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
			long refresh = _core.mame_lua_get_long(MAMELuaCommand.GetRefresh);
			VsyncDenominator = (int)(refresh / 1000000000);
		}

		private void UpdateAspect()
		{
			int x = (int)_core.mame_lua_get_long(MAMELuaCommand.GetBoundX);
			int y = (int)_core.mame_lua_get_long(MAMELuaCommand.GetBoundY);
			VirtualHeight = BufferWidth > BufferHeight * x / y
				? BufferWidth * y / x
				: BufferHeight;
			VirtualWidth = VirtualHeight * x / y;
		}

		private void UpdateVideo()
		{
			_core.mame_video_get_dimensions(out var width, out var height);

			BufferWidth = width;
			BufferHeight = height;
			int numPixels = width * height;

			if (_frameBuffer.Length < numPixels)
			{
				_frameBuffer = new int[numPixels];
			}

			_core.mame_video_get_pixels(_frameBuffer);
		}
	}
}