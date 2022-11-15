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

		/// <summary>
		/// Attoseconds for the emulated system's vsync rate.
		/// Use this to calculate a precise movie time
		/// </summary>
		public long VsyncAttoseconds { get; private set; }

		private void UpdateFramerate()
		{
			VsyncNumerator = 1000000000;
			VsyncAttoseconds = _core.mame_lua_get_long(MAMELuaCommand.GetRefresh);
			VsyncDenominator = (int)(VsyncAttoseconds / 1000000000);
		}

		private void UpdateAspect()
		{
			_core.mame_lua_get_double(MAMELuaCommand.GetBoundX, out var x);
			_core.mame_lua_get_double(MAMELuaCommand.GetBoundY, out var y);
			VirtualHeight = BufferWidth > BufferHeight * x.val / y.val
				? (int)Math.Round(BufferWidth * y.val / x.val)
				: BufferHeight;
			VirtualWidth = (int)Math.Round(VirtualHeight * x.val / y.val);
		}

		private void UpdateVideo()
		{
			_core.mame_video_get_dimensions(out var width, out var height);

			BufferWidth = width;
			BufferHeight = height;
			var numPixels = width * height;

			if (_frameBuffer.Length < numPixels)
			{
				_frameBuffer = new int[numPixels];
			}

			_core.mame_video_get_pixels(_frameBuffer);
		}
	}
}