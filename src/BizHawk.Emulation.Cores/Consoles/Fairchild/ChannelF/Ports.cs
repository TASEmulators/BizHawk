using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	/// <summary>
	/// Ports and related functions
	/// Based on the schematic here:
	/// https://web.archive.org/web/20210524083636/http://channelf.se/veswiki/images/3/31/FVE100_schematic_sheet_1of3.gif
	/// https://web.archive.org/web/20160313115333/http://channelf.se/veswiki/images/0/04/FVE_schematic_sheet_2_of_3.png
	/// </summary>
	public partial class ChannelF
	{
		/// <summary>
		/// The Channel F has 4 8-bit IO ports connected.
		/// CPU (3850) - ports 0 and 1
		/// PSU (3851) - ports 4 and 5
		/// (the second PSU has no IO ports wired up)
		/// Depending on the attached cartridge, there may be additional hardware on the IO bus
		/// All CPU and PSU I/O ports are active-low with output-latches
		/// </summary>
		private byte[] _outputLatch = new byte[0xFF];

		private bool LS368Enable;

		/// <summary>
		/// CPU is attempting to read from a port
		/// </summary>
		private byte ReadPort(ushort addr)
		{
			int result;
			switch (addr)
			{
				case 0:
					// Console Buttons - these are connected to pins 0-3 (bits 0-3) through a 7404 Hex Inverter
					// b0: TIME
					// b1: MODE
					// b2: HOLD
					// b3: START
					// RESET button is connected directly to the RST pin on the CPU (this is handled here in the PollInput() method)
					result = (~DataConsole & 0x0F) | _outputLatch[addr];
					InputCallbacks.Call();
					_isLag = false;
					break;

				case 1:
					// right controller (player 1)
					// connected through 7404 Hex Inverter
					// b0: RIGHT
					// b1: LEFT
					// b2: BACK
					// b3: FORWARD
					// b4: CCW
					// b5: CW
					var v1 = LS368Enable ? DataRight : DataRight | 0xC0;
					result = ~v1 | _outputLatch[addr];
					InputCallbacks.Call();
					_isLag = false;
					break;

				case 4:
					// left controller (player 2)
					// connected through LS368 Hex Interting 3-State Buffer
					// the enable pin of this IC is driven by a CPU write to pin 6 on port 0
					// b0: RIGHT
					// b1: LEFT
					// b2: BACK
					// b3: FORWARD
					// b4: CCW
					// b5: CW
					// b6: PULL
					// b7: PUSH
					var v2 = LS368Enable ? DataLeft : 0xFF;
					result = ~v2 | _outputLatch[addr];
					if (LS368Enable)
					{
						InputCallbacks.Call();
						_isLag = false;
					}

					break;

				case 5:
					result = _outputLatch[addr];
					break;

				default:
					// possible cartridge hardware IO space
					result = _cartridge.ReadPort(addr);
					break;
			}

			return (byte)result;
		}

		/// <summary>
		/// CPU is attempting to write to the specified IO port
		/// </summary>
		private void WritePort(ushort addr, byte value)
		{
			switch (addr)
			{
				case 0:
					_outputLatch[addr] = value;
					LS368Enable = !value.Bit(6);
					if (value.Bit(5))
					{
						// WRT pulse
						// pulse clocks the 74195 parallel access shift register which feeds inputs of 2 NAND gates
						// writing data to both sets of even and odd VRAM chips (based on the row and column addresses latched into the 7493 ICs)
						_vram[_latchY * 0x80 + _latchX] = (byte)_latchColour;
					}

					break;

				case 1:
					_outputLatch[addr] = value;
					_latchColour = ((value ^ 0xFF) >> 6) & 0x03;
					break;

				case 4:
					_outputLatch[addr] = value;
					_latchX = (value | 0x80) ^ 0xFF;
					break;

				case 5:
					_outputLatch[addr] = value;
					_latchY = (value | 0xC0) ^ 0xFF;
					var audio = (value >> 6) & 0x03;
					if (audio != _tone)
					{
						_tone = audio;
						AudioChange();
					}

					break;

				default:
					// possible write to cartridge hardware
					_cartridge.WritePort(addr, value);
					break;
			}
		}
	}
}
