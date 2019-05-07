using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	/// <summary>
	/// Ports and related functions
	/// </summary>
	public partial class ChannelF
	{
		/// <summary>
		/// The Channel F has 4 8-bit IO ports connected.
		/// CPU - ports 0 and 1
		/// PSU - ports 4 and 5
		/// (the second PSU has no IO ports wired up)
		/// </summary>
		public byte[] PortLatch = new byte[4];

		public bool ControllersEnabled;

		public const int PORT0 = 0;
		public const int PORT1 = 1;
		public const int PORT4 = 2;
		public const int PORT5 = 3;

		/// <summary>
		/// CPU attempts to read data byte from the requested port
		/// </summary>
		/// <param name="addr"></param>
		/// <returns></returns>
		public byte ReadPort(ushort addr)
		{
			byte port = (byte) (addr & 0x07);

			switch (port)
			{
				// Console buttons
				// b0:	TIME
				// b1:	MODE
				// b2:	HOLD
				// b3:	START
				case 0:
					return (byte)((DataConsole ^ 0xFF) & 0x0F);

				// Right controller
				// b0:	RIGHT
				// b1:	LEFT
				// b2:	BACK
				// b3:	FORWARD
				// b4:	CCW
				// b5:	CW
				// b6:	PULL
				// b7:	PUSH
				case 1:
					if (ControllersEnabled)
					{
						return (byte)((DataRight ^ 0xFF) & 0xFF);
					}
					return 0;

				// Left controller
				// b0:	RIGHT
				// b1:	LEFT
				// b2:	BACK
				// b3:	FORWARD
				// b4:	CCW
				// b5:	CW
				// b6:	PULL
				// b7:	PUSH
				case 4:
					if (ControllersEnabled)
					{
						return (byte)((DataLeft ^ 0xFF) & 0xFF);
					}
					return 0;
			}
			
			return 0xFF;
		}

		/// <summary>
		/// CPU attempts to write data to the requested port (latch)
		/// </summary>
		/// <param name="addr"></param>
		/// <param name="value"></param>
		public void WritePort(ushort addr, byte value)
		{
			byte port = (byte)(addr & 0x07);

			switch (port)
			{
				case 0:

					ControllersEnabled = (value & 0x40) == 0;

					var val = value & 0x60;
					if (val == 0x40)// && _arm == 0x60)
					{
						VRAM[(128 * _y) + _x] = (byte)_colour;
					}

					/*

					// RAM WRT - A pulse here executes a write to video RAM
					bool ramWrt = value.Bit(5);

					// Enable data from controllers (1 equals enable)
					// also needs pulse to write to video RAM
					bool controllerDataEnable = value.Bit(6);

					if (ramWrt || controllerDataEnable)
					{
						// triggered write to VRAM
						var yxIndex = (_y * 128) + _x;
						var byteIndex = yxIndex / 4;
						var byteRem = yxIndex % 4;

						switch (byteRem)
						{
							case 0:
								VRAM[byteIndex] |= (byte) _colour;
								break;
							case 1:
								VRAM[byteIndex] |= (byte) (_colour << 2);
								break;
							case 2:
								VRAM[byteIndex] |= (byte)(_colour << 4);
								break;
							case 3:
								VRAM[byteIndex] |= (byte)(_colour << 6);
								break;
						}

					}
					*/

					_arm = value;

					PortLatch[PORT0] = value;

					break;

				case 1:

					// Write Data0 - indicates that valid data is present for both VRAM ODD0 and EVEN0
					bool data0 = value.Bit(6);
					// Write Data1 - indicates that valid data is present for both VRAM ODD1 and EVEN1
					bool data1 = value.Bit(7);

					_colour = ((value ^ 0xff) >> 6) & 0x03;

					PortLatch[PORT1] = value;

					break;

				case 4:

					// video horizontal position
					// 0 - video select
					// 1-6 - horiz A-F

					_x = (value ^ 0xff) & 0x7f;

					PortLatch[PORT4] = value;

					break;


				case 5:

					// video vertical position and sound
					// 0-5 - Vertical A-F
					// 6 - Tone AN, 7 - Tone BN

					_y = (value ^ 0xff) & 0x3f;

					PortLatch[PORT5] = value;

					break;
			}
		}
	}
}
