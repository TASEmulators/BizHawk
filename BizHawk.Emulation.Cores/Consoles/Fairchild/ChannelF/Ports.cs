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
			switch (addr)
			{
				// Console buttons
				// b0:	TIME
				// b1:	MODE
				// b2:	HOLD
				// b3:	START
				case 0:
					return (byte)((DataConsole ^ 0xff) | PortLatch[PORT0]);

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
					byte ed1;
					if ((PortLatch[PORT0] & 0x40) == 0)
					{
						ed1 = DataRight;
					}
					else
					{
						ed1 = (byte) (0xC0 | DataRight);
					}
					return (byte) ((ed1 ^ 0xff) | PortLatch[PORT1]);

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
					byte ed4;
					if ((PortLatch[PORT0] & 0x40) == 0)
					{
						ed4 = DataLeft;
					}
					else
					{
						ed4 = 0xff;
					}
					return (byte)((ed4 ^ 0xff) | PortLatch[PORT4]);

				case 5:
					return (byte) (0 | PortLatch[PORT5]);

				default:
					return 0;
			}
		}

		/// <summary>
		/// CPU attempts to write data to the requested port (latch)
		/// </summary>
		/// <param name="addr"></param>
		/// <param name="value"></param>
		public void WritePort(ushort addr, byte value)
		{
			switch (addr)
			{
				case 0:
					PortLatch[PORT0] = value;
					if ((value & 0x20) != 0)
					{
						var offset = _x + (_y * 128);
						VRAM[offset] = (byte)(_colour);
					}
					break;

				case 1:

					PortLatch[PORT1] = value;

					// Write Data0 - indicates that valid data is present for both VRAM ODD0 and EVEN0
					bool data0 = value.Bit(6);
					// Write Data1 - indicates that valid data is present for both VRAM ODD1 and EVEN1
					bool data1 = value.Bit(7);

					//_colour = ((value) >> 6) & 3;
					_colour = ((value ^ 0xff) >> 6) & 0x3;
					break;

				case 4:
					PortLatch[PORT4] = value;
					_x = (value ^ 0xff) & 0x7f;
					//_x = (value | 0x80) ^ 0xFF;
					/*

					// video horizontal position
					// 0 - video select
					// 1-6 - horiz A-F

					

					*/

					break;


				case 5:

					PortLatch[PORT5] = value;
					//_y = (value & 31); // ^ 0xff;
					//_y = (value | 0xC0) ^ 0xff;

					//_y = (value ^ 0xff) & 0x1f;

					// video vertical position and sound
					// 0-5 - Vertical A-F
					// 6 - Tone AN, 7 - Tone BN

					_y = (value ^ 0xff) & 0x3f;

					// audio
					var aVal = ((value >> 6) & 0x03); // (value & 0xc0) >> 6;
					if (aVal != tone)
					{
						tone = aVal;
						time = 0;
						amplitude = 1;
						AudioChange();
					}
					break;
			}
		}
	}
}
