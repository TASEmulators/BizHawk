using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	/// <summary>
	/// Ports and related functions
	/// Based on the luxor schematic here:
	/// https://web.archive.org/web/20210524083634/http://channelf.se/veswiki/images/3/35/Luxor_page2_300dpi.png
	/// https://channelf.se/veswiki/images/2/23/Luxor_page3_300dpi.png
	/// </summary>
	public partial class ChannelF
	{
		/// <summary>
		/// The Channel F has 4 8-bit IO ports connected.
		/// CPU (3850) - ports 0 and 1
		/// PSU (3851) - ports 4 and 5
		/// (the second PSU has no IO ports wired up)
		/// All CPU and PSU I/O ports are active-low with output-latches
		/// </summary>
		public byte[] OutputLatch = new byte[4];

		public bool LS368Disabled;

		public const int PORT0 = 0;
		public const int PORT1 = 1;
		public const int PORT4 = 2;
		public const int PORT5 = 3;

		/// <summary>
		/// CPU is attempting to read from a port
		/// </summary>
		/// <param name="addr"></param>
		/// <returns></returns>
		public byte ReadPort(ushort addr)
		{
			byte result = 1;

			switch (addr)
			{
				default:
					break;
				case 0:

					// Console Buttons - these are connected to pins 0-3 (bits 0-3) through a 7404 Hex Inverter

					// sample RESET state first - this is connected directly to the RESET pin on the CPU
					if (DataConsole.Bit(5))
					{
						CPU.Reset();
					}

					// get the 4 console buttons state
					var cButtons = DataConsole & 0x0F;

					// hex inverter
					var cButtonsInverted = (byte)(DataConsole ^ 0xFF);

					// AND latched output (pins 4 and 7 not connected)
					result = (byte)((OutputLatch[PORT0] & 0x6F) | cButtonsInverted);

					break;

				case 1:

					// right controller (player 1)

					// connected through 7404 Hex Inverter
					// b0:	RIGHT
					// b1:	LEFT
					// b2:	BACK
					// b3:	FORWARD
					// b4:	CCW
					// b5:	CW
					var rButtons = DataRight & 0x3F;

					// connected through LS368 Hex Interting 3-State Buffer
					// the enable pin of this IC is driven by a CPU write to pin 6 on port 0
					// b6:	PULL
					// b7:	PUSH
					var rButtons2 = LS368Disabled ? 0 : DataRight & 0xC0;

					// hex inverters
					var rbuttonsInverted = (byte)((rButtons | rButtons2) ^ 0xFF);

					// AND latched output
					result = (byte)(OutputLatch[PORT1] | rbuttonsInverted);

					break;

				case 4:

					// left controller (player 2)

					// connected through LS368 Hex Interting 3-State Buffer
					// the enable pin of this IC is driven by a CPU write to pin 6 on port 0
					// b0:	RIGHT
					// b1:	LEFT
					// b2:	BACK
					// b3:	FORWARD
					// b4:	CCW
					// b5:	CW
					// b6:	PULL
					// b7:	PUSH
					var lButtons = LS368Disabled ? 0 : DataLeft & 0xFF;

					// hex inverter
					var lButtonsInverted = (byte)(lButtons ^ 0xFF);

					// AND latched output
					result = (byte)(OutputLatch[PORT4] | lButtonsInverted);

					break;

				case 5:

					// output only IO port - return the last latched output
					result = OutputLatch[PORT5];

					break;

			}

			return result;
		}

		/// <summary>
		/// CPU is attempting to write to the specified IO port
		/// </summary>
		public void WritePort(ushort addr, byte value)
		{
			switch (addr)
			{
				case 0:

					OutputLatch[PORT0] = value;

					// LS368 enable pin on bit 6
					LS368Disabled = value.Bit(6);

					if (value.Bit(5))
					{
						// pulse clocks the 74195 parallel access shift register which feeds inputs of 2 NAND gates
						// writing data to both sets of even and odd VRAM chips (based on the row and column addresses latched into the 7493 ICs
						VRAM[(latch_y * 0x80) + latch_x] = (byte)latch_colour;
					}

					break;

				case 1:

					OutputLatch[PORT1] = value;

					// set pixel colour
					// write data 0 = bit6
					// write data 1 = bit7
					latch_colour = ((value ^ 0xFF) >> 6) & 0x03;

					break;

				case 4:

					OutputLatch[PORT4] = value;

					// latch horiztonal column address
					// these are hex inverted along the way
					// bit7 is not sent to the 7493s (IO47N) - make it logical 1 before hex inversion
					var p1Data = value | 0x80;
					latch_x = (p1Data ^ 0xFF) & 0xFF;

					break;

				case 5:

					OutputLatch[PORT5] = value;

					// bits 6 (ToneAN) and 7 (ToneBN) are sound generation					
					var audio = (value >> 6) & 0x03;
					if (audio != tone)
					{
						tone = audio;
						time = 0;
						amplitude = 1;
						AudioChange();
					}

					// remaining bits latch vertical row address
					var vert = (value | 0xC0) & 0xFF;
					latch_y = (vert ^ 0xFF) & 0xFF;

					break;
			}
		}
		/*
		/// <summary>
		/// CPU attempts to read data byte from the requested port
		/// </summary>
		/// <param name="addr"></param>
		/// <returns></returns>
		public byte ReadPort1(ushort addr)
		{
			switch (addr)
			{				
				// CPU Port 0
				case 0:
					// Console buttons
					// b0:	TIME
					// b1:	MODE
					// b2:	HOLD
					// b3:	START
					return (byte)((DataConsole ^ 0xff) | OutputLatch[PORT0]);

				
				// CPU Port 1
				case 1:
					// Right controller
					// b0:	RIGHT
					// b1:	LEFT
					// b2:	BACK
					// b3:	FORWARD
					// b4:	CCW
					// b5:	CW
					// b6:	PULL
					// b7:	PUSH
					byte ed1;
					if ((OutputLatch[PORT0] & 0x40) == 0)
					{
						ed1 = DataRight;
					}
					else
					{
						ed1 = (byte) (0xC0 | DataRight);
					}
					return (byte) ((ed1 ^ 0xff) | OutputLatch[PORT1]);

				// PSU Port 4
				case 4:
					// Left controller
					// b0:	RIGHT
					// b1:	LEFT
					// b2:	BACK
					// b3:	FORWARD
					// b4:	CCW
					// b5:	CW
					// b6:	PULL
					// b7:	PUSH
					byte ed4;
					if ((OutputLatch[PORT0] & 0x40) == 0)
					{
						ed4 = DataLeft;
					}
					else
					{
						ed4 = 0xff;
					}
					return (byte)((ed4 ^ 0xff) | OutputLatch[PORT4]);

				// PSU Port 5
				case 5:
					return (byte) (0 | OutputLatch[PORT5]);

				default:
					return 0;
			}
		}

		/// <summary>
		/// CPU attempts to write data to the requested port (latch)
		/// </summary>
		/// <param name="addr"></param>
		/// <param name="value"></param>
		public void WritePort1(ushort addr, byte value)
		{
			switch (addr)
			{
				// CPU Port 0
				case 0:
					// b5:	Executes a write to VRAM
					// b6:	Enable controllers data
					OutputLatch[PORT0] = value;

					if ((value & 0x20) != 0)
					{
						// write to VRAM
						var offset = _x + (_y * 128);
						VRAM[offset] = (byte)(_colour);
					}					

					if ((value & 0x40) != 0)
					{
						//ControllersEnabled = false;
					}

					break;

				// CPU Port 1
				case 1:
					// bits 6 and 7 decide pixel colour (this is not inverted)

					OutputLatch[PORT1] = value;

					
					// Write Data0 - indicates that valid data is present for both VRAM ODD0 and EVEN0
					//bool data0 = value.Bit(6);
					// Write Data1 - indicates that valid data is present for both VRAM ODD1 and EVEN1
					//bool data1 = value.Bit(7);
					

					_colour = (value >> 6) & 0x3;
					break;

				// PSU Port 4
				case 4:
					// 
					OutputLatch[PORT4] = value;
					_x = (value ^ 0xff) & 0x7f;
					//_x = (value | 0x80) ^ 0xFF;
					/*

					// video horizontal position
					// 0 - video select
					// 1-6 - horiz A-F

					

					

					break;

				// PSU port 5
				case 5:

					OutputLatch[PORT5] = value;
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

*/
	}
}
