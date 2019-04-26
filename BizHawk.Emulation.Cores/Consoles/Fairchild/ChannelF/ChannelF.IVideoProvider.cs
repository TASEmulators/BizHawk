using System;
using BizHawk.Common;
using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	public partial class ChannelF : IVideoProvider, IRegionable
	{
		public static readonly int[] FPalette =
		{
			Colors.ARGB(0x10, 0x10, 0x10),		// Black
			Colors.ARGB(0xFD, 0xFD, 0xFD),		// White
			Colors.ARGB(0xFF, 0x31, 0x53),		// Red
			Colors.ARGB(0x02, 0xCC, 0x5D),		// Green
			Colors.ARGB(0x4B, 0x3F, 0xF3),		// Blue
			Colors.ARGB(0xE0, 0xE0, 0xE0),		// Gray
			Colors.ARGB(0x91, 0xFF, 0xA6),		// BGreen
			Colors.ARGB(0xCE, 0xD0, 0xFF),		// BBlue
		};

		public static readonly int[] CMap =
		{
			0, 1, 1, 1,
			7, 4, 2, 3,
			5, 4, 2, 3,
			6, 4, 2, 3,
		};

		public int _frameHz = 60;

		public int[] _vidbuffer = new int[108 * 64];

		public int[] GetVideoBuffer()
		{
			int colour;
			int a = 0;
			int pOff;

			// rows
			for (int y = 0; y < 64; y++)
			{
				pOff = ((FrameBuffer[(y * 128) + 125] & 0x3) & 0x02) | ((FrameBuffer[(y * 128) + 126] & 0x3) >> 1) << 2;

				for (int x = a; x < a + 128; x++)
				{
					colour = pOff + (FrameBuffer[x | (y << 7)] & 0x03);
					var yM = y * 64;
					_vidbuffer[yM + x] = FPalette[CMap[colour]];
				}
			}

			return _vidbuffer;
		}

		public int VirtualWidth => BufferWidth * 2;
		public int VirtualHeight => BufferHeight * 2;
		public int BufferWidth => 108; // 102
		public int BufferHeight => 64;	// 58
		public int BackgroundColor => unchecked((int)0xFF000000);
		public int VsyncNumerator => _frameHz;
		public int VsyncDenominator => 1;

		private int row = 0;
		private int col = 0;
		private byte value = 0;

		public void VID_PortIN(ushort port, byte val)
		{
			switch (port)
			{
				case 0:

					int o;
					PortLatch[port] = val;
					if ((val & 0x20) != 0)
					{
						o = (row * 128) + col;
						FrameBuffer[o] = value;
					}
					break;

				case 1: // Set Color (bits 6 and 7) 
					PortLatch[port] = val;
					value = (byte)(((val ^ 0xFF) >> 6) & 0x03);
					break;
				case 4: // X coordinate, inverted (bits 0-6)
					PortLatch[2] = val;
					col = (val | 0x80) ^ 0xFF;
					break;
				case 5: // Y coordinate, inverted (bits 0-5)
					PortLatch[3] = val;
					//sound  TODO
					row = (val | 0xC0) ^ 0xFF;
					break;
			}
		}


		#region IRegionable

		public DisplayType Region => DisplayType.NTSC;

		#endregion
	}
}
