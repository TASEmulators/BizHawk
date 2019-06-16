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
		public byte reg_A, reg_B;

		public byte portB_ret, portA_ret;

		public byte t1_low, t1_high;	
		public int t1_counter, t1_ctrl;	
		public bool t1_shot_go;

		public byte t2_low, t2_high;
		public int t2_counter, t2_ctrl;
		public bool t2_shot_go;

		public bool PB7, PB6;
		public bool PB7_prev, PB6_prev;

		// Port B controls
		public bool sw, sel0, sel1, bc1, bdir, compare, ramp;

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
					if (!bdir && bc1)
					{
						ret = audio.ReadReg(0);
						if (audio.port_sel == 14) { _islag = false; }
					}
					else
					{
						ret = portA_ret;
					}

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
					ret = (byte)(t2_counter & 0xFF);

					int_fl &= 0xDF;
					update_int_fl();
					break;
				case 0x9:
					ret = (byte)((t2_counter >> 8) & 0xFF);
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
					ret = portA_ret;

					int_fl &= 0xFC;
					update_int_fl();
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

					portB_ret = (byte)(wrt_val | (reg_B & ~(dir_ctrl)));

					if (dir_ctrl.Bit(0)) { sw = value.Bit(0); }
					if (dir_ctrl.Bit(1)) { sel0 = value.Bit(1); }
					if (dir_ctrl.Bit(2)) { sel1 = value.Bit(2); }
					if (dir_ctrl.Bit(3)) { bc1 = value.Bit(3); }
					if (dir_ctrl.Bit(4)) { bdir = value.Bit(4); }
					if (dir_ctrl.Bit(5)) { /*compare = value.Bit(5);*/ }
					if (dir_ctrl.Bit(6)) { /* cart bank switch */ }
					if (dir_ctrl.Bit(7)) { ramp = !value.Bit(7); }

					// writing to sound reg
					if (bdir)
					{
						if (bc1) { audio.port_sel = (byte)(portA_ret & 0xF); }
						else { audio.WriteReg(0, portA_ret); }
					}

					int_fl &= 0xE7;
					update_int_fl();
					break;
				case 0x1:
					wrt_val = (byte)(value & dir_dac);

					portA_ret = (byte)(wrt_val | (reg_A & ~(dir_dac)));

					// writing to sound reg
					if (bdir)
					{
						if (bc1) { audio.port_sel = (byte)(portA_ret & 0xf); }
						else { audio.WriteReg(0, portA_ret); }
					}

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
					t1_shot_go = true;
					if (aux_ctrl.Bit(7)) { PB7 = true; }
					t1_ctrl = aux_ctrl;

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
					t2_low = value;
					break;
				case 0x9:
					t2_high = value;

					t2_counter = (t2_high << 8) | t2_low;
					t2_shot_go = true;
					t2_ctrl = aux_ctrl;

					int_fl &= 0xDF;
					update_int_fl();
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
					update_int_fl();
					break;
				case 0xF:
					wrt_val = (byte)(value & dir_dac);

					portA_ret = (byte)(wrt_val | (reg_A & ~(dir_dac)));

					int_fl &= 0xFC;
					update_int_fl();
					break;
			}
		}

		public void timer_1_tick()
		{
			t1_counter--;

			if (t1_counter < 0)
			{
				if (t1_ctrl.Bit(6))
				{
					t1_counter = (t1_high << 8) | t1_low;

					int_fl |= 0x40;
					update_int_fl();
					//if (int_en.Bit(6)) { cpu.IRQPending = true; }

					if (t1_ctrl.Bit(7)) { PB7 = !PB7; }
				}
				else
				{
					t1_counter = 0xFFFF;

					if (t1_shot_go)
					{
						int_fl |= 0x40;
						update_int_fl();
						//if (int_en.Bit(6)) { cpu.IRQPending = true; }
						if (t1_ctrl.Bit(7)) { PB7 = false; }

						t1_shot_go = false;
					}				
				}
			}
		}

		public void timer_2_tick()
		{
			t2_counter--;

			if (t2_counter < 0)
			{
				if (t2_ctrl.Bit(5))
				{
					t2_counter = (t2_high << 8) | t2_low;

					int_fl |= 0x20;
					update_int_fl();
					//if (int_en.Bit(6)) { cpu.IRQPending = true; }
				}
				else
				{
					t2_counter = 0xFFFF;

					if (t2_shot_go)
					{
						int_fl |= 0x20;
						update_int_fl();
						//if (int_en.Bit(6)) { cpu.IRQPending = true; }

						t2_shot_go = false;
					}
				}
			}
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

		public void Register_Reset()
		{
			dir_dac = dir_ctrl = 0;

			portB_ret = portA_ret = 0;

			t1_low = t1_high = 0;
			t1_counter = t1_ctrl = 0;
			t1_shot_go = false;
			PB7 = PB7_prev = false;

			t2_low = t2_high = 0;
			t2_counter = t2_ctrl = 0;
			t2_shot_go = false;
			PB6 = PB7_prev = false;

			int_en = int_fl = aux_ctrl = 0;
		}
	}
}
