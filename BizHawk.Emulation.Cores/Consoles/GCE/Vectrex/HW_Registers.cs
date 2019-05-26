using System;
using BizHawk.Emulation.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
	// MOS6522 Interface

	/* Port B writes to both the PSG and the DAC simultaneously
	 * The trick here is that bits 3 and 4 both zero represent PSG disabled
	 * So it's easy to not interfere with the PSG
	 * However, the DAC will always receive some input, controlled by the multiplexer and selector bits  
	 * BIOS functions keep everything in order
	 */

	public partial class VectrexHawk
	{
		public byte dir_dac, dir_ctrl;

		public byte portB_ret, portA_ret;

		public byte t1_low, t1_high;
		public int t1_counter;
		public bool t1_on, t1_shot_done;
		public bool PB7;

		public byte int_en, int_fl, aux_ctrl;

		public byte Read_Registers(int addr)
		{
			byte ret = 0;

			switch (addr)
			{
				case 0x0:
					ret = portB_ret;

					int_fl &= 0xE7;
					update_int_fl();
					break;
				case 0x1:
					ret = portA_ret;

					int_fl &= 0xFC;
					update_int_fl();
					break;
				case 0x2:
					ret = dir_ctrl;
					break;
				case 0x3:
					ret = dir_dac;
					break;
				case 0x4:
					ret = (byte)(t1_counter & 0xFF);

					int_fl &= 0xBF;
					update_int_fl();
					break;
				case 0x5:
					ret = (byte)((t1_counter >> 8) & 0xFF);
					break;
				case 0x6:
					ret = t1_low;
					break;
				case 0x7:
					ret = t1_high;
					break;
				case 0x8:
					break;
				case 0x9:
					break;
				case 0xA:
					int_fl &= 0xFB;
					update_int_fl();
					break;
				case 0xB:
					ret = aux_ctrl;
					break;
				case 0xC:
					break;
				case 0xD:
					ret = int_fl;
					break;
				case 0xE:
					ret = int_en;
					break;
				case 0xF:
					break;
			}
			return ret;
		}

		public void Write_Registers(int addr, byte value)
		{
			byte wrt_val = 0;

			switch (addr)
			{
				case 0x0:
					wrt_val = (byte)(value & dir_ctrl);

					int_fl &= 0xE7;
					update_int_fl();
					break;
				case 0x1:
					wrt_val = (byte)(value & dir_dac);

					int_fl &= 0xFC;
					update_int_fl();
					break;
				case 0x2:
					dir_ctrl = value;
					break;
				case 0x3:
					dir_dac = value;
					break;
				case 0x4:
					t1_low = value;
					break;
				case 0x5:
					t1_high = value;
					t1_counter = (t1_high << 8) | t1_low;
					t1_on = true;
					t1_shot_done = false;
					if (aux_ctrl.Bit(7)) { PB7 = true; }

					int_fl &= 0xBF;				
					update_int_fl();
					break;
				case 0x6:
					t1_low = value;
					break;
				case 0x7:
					t1_high = value;

					int_fl &= 0xBF;
					update_int_fl();
					break;
				case 0x8:
					break;
				case 0x9:
					break;
				case 0xA:
					int_fl &= 0xFB;
					update_int_fl();
					break;
				case 0xB:
					aux_ctrl = value;
					break;
				case 0xC:
					break;
				case 0xD:
					// writing to flags does not clear bit 7 directly
					int_fl &= (byte)~(value & 0x7F);

					update_int_fl();
					break;
				case 0xE:
					// bit 7 is always 0
					if (value.Bit(7))
					{
						int_en |= (byte)(value & 0x7F);
					}
					else
					{
						int_en &= (byte)((~value) & 0x7F);
					}
					break;
				case 0xF:
					break;
			}
		}

		public void Register_Reset()
		{

		}

		public void timer_1_tick()
		{
			if (t1_on)
			{
				t1_counter--;

				if (t1_counter == 0)
				{
					if (aux_ctrl.Bit(6))
					{
						t1_counter = (t1_high << 8) | t1_low;

						int_fl |= 0x40;
						//if (int_en.Bit(6)) { cpu.IRQPending = true; }

						if (aux_ctrl.Bit(7)) { PB7 = !PB7; }
					}
					else
					{
						t1_counter = 0xFFFF;

						if (!t1_shot_done)
						{
							int_fl |= 0x40;
							//if (int_en.Bit(6)) { cpu.IRQPending = true; }
							if (aux_ctrl.Bit(7)) { PB7 = false; }
						}
						t1_shot_done = true;
					}
				}
			}
		}

		public void timer_2_tick()
		{

		}

		public void update_int_fl()
		{
			// bit 7 is (IF.bit(X) & IE.bit(X)) OR'ed together for each bit
			bool test = false;

			for (int i = 0; i < 7; i++)
			{
				test |= int_en.Bit(i) & int_fl.Bit(i);
			}

			int_fl |= (byte)(test ? 0x80 : 0);
		}
	}
}
