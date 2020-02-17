using BizHawk.Common.NumberExtensions;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	// emualtes pokey sound chip
	// note: A7800 implementation is used only for sound
	// potentiometers, keyboard, and IRQs are not used in this context
	/*
	 * Regs 0,2,4,6: Frequency control (divider = value + 1)
	 * Regs 1,3,5,7: Channel control (Bits 0-3 = volume) (bits 4 - 7 control clocking)
	 * Reg 8: Control register
	 * 
	 * Reg A: Random number generator
	 * 
	 * The registers are write only, except for the RNG none of the things that would return reads are connected
	 * for now return FF
	 */
	public class Pokey
	{
		public A7800Hawk Core { get; set; }

		public int LocalAudioCycles;

		// state variables
		public byte[] Regs = new byte[16];
		public int poly4, poly5, poly9, poly17;
		public int[] ch_div = new int[4];
		public int[] inc_ch = new int[4];
		public bool[] ch_out = new bool[4];
		public bool[] ch_src = new bool[4];
		public int[] ch_vol = new int[4];
		public bool high_pass_1;
		public bool high_pass_2;

		// these are derived values and do not need to be save-stated
		public bool[] clock_ch = new bool[4];
		public int bit_xor;

		public Pokey()
		{

		}

		public int sample()
		{
			LocalAudioCycles = 0;
			LocalAudioCycles += ch_vol[0];
			LocalAudioCycles += ch_vol[1];
			LocalAudioCycles += ch_vol[2];
			LocalAudioCycles += ch_vol[3];

			return LocalAudioCycles;
		}

		public byte ReadReg(int reg)
		{
			byte ret = 0xFF;

			if (reg==0xA)
			{
				ret = (byte)(poly17 >> 9);
			}

			return ret;
		}

		public void WriteReg(int reg, byte value)
		{
			Regs[reg] = value;

			// this condition resets poly counters and holds them in place
			if ((Regs[0xF] & 3) == 0)
			{
				poly4 = 0xF;
				poly5 = 0x1F;
				poly17 = 0x1FFFF;
			}
		}

		public void Tick()
		{
			// clock the 4-5-(9 or 17) bit poly counters
			// NOTE: These might not be the exact poly implementation, I just picked a maximal one from wikipedia
			// poly 4 and 5 are known to result in:
			// poly4 output: 000011101100101
			// poly5 output: 1101001100000111001000101011110
			if ((Regs[0xF] & 3) != 0)
			{
				bit_xor = ((poly4) ^ (poly4 >> 1)) & 1;
				poly4 = (poly4 >> 1) | (bit_xor << 3);

				bit_xor = ((poly5 >> 2) ^ poly5) & 1;
				poly5 = (poly5 >> 1) | (bit_xor << 4);

				if (Regs[8].Bit(7))
				{
					// clock only 9 bits of the 17 bit poly
					poly9 = poly17 >> 8;
					bit_xor = ((poly9 >> 4) ^ poly9) & 1;
					poly9 = (poly9 >> 1) | (bit_xor << 8);
					poly17 = (poly17 & 0xFF) | (poly9 << 8);
				}
				else
				{
					// clock the whole 17 bit poly
					bit_xor = ((poly17 >> 3) ^ poly17) & 1;
					poly17 = (poly17 >> 1) | (bit_xor << 16);
				}
			}

			clock_ch[0] = clock_ch[1] = clock_ch[2] = clock_ch[3] = false;
			
			// now that we have the poly counters, check which channels to clock
			if (Regs[8].Bit(6))
			{
				clock_ch[0] = true;
			}
			else
			{
				inc_ch[0]++;
				if (Regs[8].Bit(0))
				{
					if (inc_ch[0] >= 114) { inc_ch[0] = 0; clock_ch[0] = true; }					
				}
				else
				{
					if (inc_ch[0] >= 28) { inc_ch[0] = 0; clock_ch[0] = true; }
				}
			}

			if (Regs[8].Bit(5))
			{
				clock_ch[2] = true;
			}
			else
			{
				inc_ch[2]++;
				if (Regs[8].Bit(0))
				{
					if (inc_ch[2] >= 114) { inc_ch[2] = 0; clock_ch[2] = true; }					
				}
				else
				{
					if (inc_ch[2] >= 28) { inc_ch[2] = 0; clock_ch[2] = true; }
				}
			}

			if (Regs[8].Bit(4))
			{
				if (clock_ch[0])
				{
					clock_ch[1] = true;					
				}
			}
			else
			{
				inc_ch[1]++;
				if (Regs[8].Bit(0))
				{
					if (inc_ch[1] >= 114) { inc_ch[1] = 0; clock_ch[1] = true; }
									
				}
				else
				{
					if (inc_ch[1] >= 28) { inc_ch[1] = 0; clock_ch[1] = true; }
				}
			}

			if (Regs[8].Bit(3))
			{
				if (clock_ch[2])
				{
					clock_ch[3] = true;
				}
			}
			else
			{
				inc_ch[3]++;
				if (Regs[8].Bit(0))
				{
					if (inc_ch[3] >= 114) { inc_ch[3] = 0; clock_ch[3] = true; }								
				}
				else
				{
					if (inc_ch[3] >= 28) { inc_ch[3] = 0; clock_ch[3] = true; }
				}
			}

			// first update the high pass filter latch
			if (clock_ch[2] && Regs[8].Bit(2)) { high_pass_1 = ch_out[0]; }
			if (clock_ch[3] && Regs[8].Bit(1)) { high_pass_2 = ch_out[1]; }

			// now we know what channels to clock, execute the cycles
			for (int i = 0; i < 4; i++) {
				if (clock_ch[i])
				{
					ch_div[i]++;

					int test = (Regs[i * 2] + 1);

					if ((i == 1) && Regs[8].Bit(4))
					{
						test = Regs[i * 2] * 256 + Regs[0] + 1;
					}

					if ((i == 3) && Regs[8].Bit(3))
					{
						test = Regs[i * 2] * 256 + Regs[2] + 1;
					}

					if (ch_div[i] >= test)
					{
						ch_div[i] = 0;

						// select the next source based on the channel control register
						if (Regs[i * 2 + 1].Bit(4))
						{
							// forced output always on (used with volume modulation)
							ch_out[i] = true;
						}
						else if ((Regs[i * 2 + 1] & 0xF0) == 0)
						{
							// 17 bit poly then 5 bit poly
							if (ch_src[i])
							{
								ch_out[i] = poly5.Bit(4);
							}
							else
							{
								ch_out[i] = poly5.Bit(16);
							}
						}
						else if (((Regs[i * 2 + 1] & 0xF0) == 0x20) || ((Regs[i * 2 + 1] & 0xF0) == 0x60))
						{
							// 5 bit poly
							//if (ch_src[i])
							//{
								ch_out[i] = poly5.Bit(4);
							//}
						}
						else if ((Regs[i * 2 + 1] & 0xF0) == 0x40)
						{
							// 4 bit poly then 5 bit poly
							if (ch_src[i])
							{
								ch_out[i] = poly5.Bit(4);
							}
							else
							{
								ch_out[i] = poly4.Bit(3);
							}
						}
						else if ((Regs[i * 2 + 1] & 0xF0) == 0x80)
						{
							// 17 bit poly
							//if (ch_src[i])
							//{
								ch_out[i] = poly17.Bit(16);
							//}
						}
						else if ((Regs[i * 2 + 1] & 0xF0) == 0xA0)
						{
							// tone
							//if (ch_src[i])
							//{
								ch_out[i] = !ch_out[i];
							//}
						}
						else if ((Regs[i * 2 + 1] & 0xF0) == 0xC0)
						{
							// 4 bit poly
							//if (ch_src[i])
							//{
								ch_out[i] = poly4.Bit(3);
							//}					
						}
						else if ((Regs[i * 2 + 1] & 0xF0) == 0xE0)
						{
							// tone
							ch_out[i] = !ch_out[i];
						}

						ch_src[i] = !ch_src[i];

						// for channels 1 and 2, an optional high pass filter exists
						// the filter is just a flip flop and xor combo
						if ((i == 0 && Regs[8].Bit(2)) || (i == 1 && Regs[8].Bit(1)))
						{
							if (i == 0) { ch_vol[0] = (ch_out[0] ^ high_pass_1) ? (Regs[1] & 0xF) : 0; }
							if (i == 1) { ch_vol[1] = (ch_out[1] ^ high_pass_2) ? (Regs[3] & 0xF) : 0; }

						}
						else
						{
							ch_vol[i] = (ch_out[i] ? (Regs[i * 2 + 1] & 0xF) : 0) * 70;
						}
					}
				}
			}
		}

		public void Reset()
		{
			Regs = new byte[16];
			poly4 = 0xF;
			poly5 = 0x1F;
			poly17 = 0x1FFFF;
		}

		public void SyncState(Serializer ser)
		{
			ser.BeginSection(nameof(Pokey));

			ser.Sync(nameof(Regs), ref Regs, false);

			ser.Sync(nameof(poly4), ref poly4);
			ser.Sync(nameof(poly5), ref poly5);
			ser.Sync(nameof(poly9), ref poly9);
			ser.Sync(nameof(poly17), ref poly17);
			ser.Sync(nameof(ch_div), ref ch_div, false);
			ser.Sync(nameof(inc_ch), ref inc_ch, false);
			ser.Sync(nameof(ch_out), ref ch_out, false);
			ser.Sync(nameof(ch_src), ref ch_src, false);
			ser.Sync(nameof(ch_vol), ref ch_vol, false);
			ser.Sync(nameof(high_pass_1), ref high_pass_1);
			ser.Sync(nameof(high_pass_2), ref high_pass_2);

			ser.EndSection();
		}
	}
}
