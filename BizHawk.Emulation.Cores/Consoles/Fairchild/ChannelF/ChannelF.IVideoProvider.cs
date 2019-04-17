using System;
using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	public partial class ChannelF : IVideoProvider, IRegionable
	{
		public int _frameHz = 60;

		public int[] _vidbuffer = new int[102 * 58];

		public int[] GetVideoBuffer()
		{
			return _vidbuffer;
		}

		public int VirtualWidth => 102 * 2;
		public int VirtualHeight => 58 * 2;
		public int BufferWidth => 102;
		public int BufferHeight => 58;
		public int BackgroundColor => unchecked((int)0xFF000000);
		public int VsyncNumerator => _frameHz;
		public int VsyncDenominator => 1;


		#region IRegionable

		public DisplayType Region => DisplayType.NTSC;

		#endregion
	}
}
