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
			int row;
			int col;
			int color;
			int pal;




			return _vidbuffer;
		}

		public int VirtualWidth => 102 * 2;
		public int VirtualHeight => 58 * 2;
		public int BufferWidth => 102;
		public int BufferHeight => 58;
		public int BackgroundColor => unchecked((int)0xFF000000);
		public int VsyncNumerator => _frameHz;
		public int VsyncDenominator => 1;


		private int[] colors = { 0x101010, 0xFDFDFD, 0x5331FF, 0x5DCC02, 0xF33F4B, 0xE0E0E0, 0xA6FF91, 0xD0CEFF };
		private int[] palette = {0,1,1,1, 7,2,4,3, 6,2,4,3, 5,2,4,3};
		private int[] buffer = new int[8192];

		int ARM = 0;
		int X = 0;
		int Y = 0;
		int Color = 2;

		public void VID_PortIN(ushort port, byte val)
		{
			switch (port)
			{
				case 0: // ARM 
					val &= 0x60;
					if (val == 0x40 && ARM == 0x60) // Strobed
					{
						// Write to display buffer
						buffer[(Y << 7) + X] = Color;
					}
					ARM = val;
					break;

				case 1: // Set Color (bits 6 and 7) 
					Color = ((val ^ 0xFF) >> 6) & 3;
					break;
				case 4: // X coordinate, inverted (bits 0-6)
					X = (val ^ 0xFF) & 0x7F;
					break;
				case 5: // Y coordinate, inverted (bits 0-5)
					Y = (val ^ 0xFF) & 0x3F;
					break;
			}
		}


		#region IRegionable

		public DisplayType Region => DisplayType.NTSC;

		#endregion
	}
}
