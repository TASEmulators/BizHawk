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
			byte result = 0xFF;

			switch (addr)
			{
				default:
					break;
				case 0:

					// Console Buttons - these are connected to pins 0-3 (bits 0-3) through a 7404 Hex Inverter	
					// b0:	TIME
					// b1:	MODE
					// b2:	HOLD
					// b3:	START	
					
					// RESET button is connected directly to the RST pin on the CPU (this is handled here in the PollInput() method)

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
					LS368Disabled = !value.Bit(6);

					if (!value.Bit(5))
					{
						// pulse clocks the 74195 parallel access shift register which feeds inputs of 2 NAND gates
						// writing data to both sets of even and odd VRAM chips (based on the row and column addresses latched into the 7493 ICs
						VRAM[((latch_y) * 0x80) + latch_x] = (byte)latch_colour;
					}

					break;

				case 1:

					// latch pixel colour
					OutputLatch[PORT1] = value;
					
					// write data 0 = bit6
					// write data 1 = bit7
					latch_colour = ((value) >> 6) & 0x03;

					break;

				case 4:

					// latch horiztonal column address
					OutputLatch[PORT4] = value;

					// bit7 is not sent to the 7493s (IO47N)
					latch_x = value & 0x7F;

					break;

				case 5:

					// latch vertical row address and sound bits
					OutputLatch[PORT5] = value;

					// ignore the sound bits
					latch_y = value & 0x3F;

					// bits 6 (ToneAN) and 7 (ToneBN) are sound generation					
					var audio = (value >> 6) & 0x03;
					if (audio != tone)
					{
						tone = audio;
						time = 0;
						amplitude = 1;
						AudioChange();
					}

					break;
			}
		}
	}
}
