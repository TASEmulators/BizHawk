using BizHawk.Common.NumberExtensions;

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

		public bool PB7_undriven;

		public byte pot_val;

		// Port B controls
		public bool sw, sel0, sel1, bc1, bdir, compare, shift_start;

		public byte int_en, int_fl, aux_ctrl, prt_ctrl, shift_reg, shift_reg_wait, shift_count;

		public bool frame_end;

		public byte Read_Registers(int addr)
		{
			byte ret = 0;

			switch (addr)
			{
				case 0x0:
					if (!aux_ctrl.Bit(7))
					{
						ret = portB_ret;
					}
					else
					{
						ret = (byte)((portB_ret & 0x7F) | (PB7 ? 0x80 : 0x0));
					}

					if (!dir_ctrl.Bit(5)) { ret |= (byte)(compare ? 0x20 : 0x0); }

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
					ret = shift_reg;
					update_int_fl();
					break;
				case 0xB:
					ret = aux_ctrl;
					break;
				case 0xC:
					ret = prt_ctrl;
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

					if (aux_ctrl.Bit(7))
					{
						wrt_val = (byte)((wrt_val & 0x7F) | (PB7 ? 0x80 : 0x0));
					}

					portB_ret = (byte)(wrt_val | (reg_B & ~(dir_ctrl)));

					if (dir_ctrl.Bit(0)) { sw = !value.Bit(0); }
					if (dir_ctrl.Bit(1)) { sel0 = value.Bit(1); }
					if (dir_ctrl.Bit(2)) { sel1 = value.Bit(2); }
					if (dir_ctrl.Bit(3)) { bc1 = value.Bit(3); }
					if (dir_ctrl.Bit(4)) { bdir = value.Bit(4); }
					if (dir_ctrl.Bit(5)) { /*compare = value.Bit(5);*/ }
					if (dir_ctrl.Bit(6)) { /*writing here seems to change the bank for only a single cycle, and only in output mode, not implemented*/ }
					if (dir_ctrl.Bit(7))
					{
						//Console.WriteLine(PB7_undriven + " " + !wrt_val.Bit(7));
						ppu.ramp_sig = !wrt_val.Bit(7);
						ppu.new_draw_line();
						if (PB7_undriven && !wrt_val.Bit(7))
						{
							PB7_undriven = false;
							ppu.skip = 14;
						}	
					}

					// writing to sound reg
					if (bdir)
					{
						if (bc1) { audio.port_sel = (byte)(portA_ret & 0xF); }
						else { audio.WriteReg(0, portA_ret); }
					}

					if (sw)
					{
						if (sel0)
						{
							if (sel1)
							{
								/* sound samples direct to output */
								audio.pcm_sample = (short)(portA_ret << 6);
							}
							else
							{
								ppu.vec_scale = portA_ret;
								if (portA_ret != 0) { Console.WriteLine("scale: " + portA_ret); }
							}
						}
						else
						{
							if (sel1)
							{
								ppu.bright = (byte)((portA_ret & 0x7F) << 1);
								ppu.bright_int_1 = (0xFF000000 | (uint)(ppu.bright << 16) | (uint)(ppu.bright << 8) | ppu.bright);
								ppu.bright = (byte)(portA_ret & 0x7F);
								ppu.bright_int_2 = (0xFF000000 | (uint)(ppu.bright << 16) | (uint)(ppu.bright << 8) | ppu.bright);
								ppu.bright = (byte)(portA_ret & 0x3F);
								ppu.bright_int_3 = (0xFF000000 | (uint)(ppu.bright << 16) | (uint)(ppu.bright << 8) | ppu.bright);
							}
							else
							{
								ppu.y_vel = (byte)(portA_ret ^ 0x80);
							}

							ppu.new_draw_line();
						}
					}

					if (sel0)
					{
						if (sel1)
						{
							if ((byte)(portA_ret ^ 0x80) >= joy2_UD) { compare = false; }
							else { compare = true; }
						}
						else
						{							
							if ((byte)(portA_ret ^ 0x80) >= joy1_UD) { compare = false; }
							else { compare = true; }
						}
					}
					else
					{
						if (sel1)
						{
							if ((byte)(portA_ret ^ 0x80) >= joy2_LR) { compare = false; }
							else { compare = true; }
						}
						else
						{
							if ((byte)(portA_ret ^ 0x80) >= joy1_LR) { compare = false; }
							else { compare = true; }
						}
					}

					ppu.x_vel = (byte)(portA_ret ^ 0x80);
					ppu.new_draw_line();

					int_fl &= 0xE7;
					update_int_fl();
					break;
				case 0x1:
					wrt_val = (byte)(value & dir_dac);

					portA_ret = (byte)(wrt_val | (reg_A & ~(dir_dac)));

					// writing to sound reg
					if (bdir)
					{
						if (bc1) { audio.port_sel = (byte)(portA_ret & 0xF); }
						else { audio.WriteReg(0, portA_ret); }
					}

					if (sw)
					{
						if (sel0)
						{
							if (sel1)
							{
								/* sound samples direct to output */
								audio.pcm_sample = (short)(portA_ret << 6);
							}
							else
							{
								ppu.vec_scale = portA_ret;
								if (portA_ret != 0) { Console.WriteLine("scale: " + portA_ret); }
							}
						}
						else
						{
							if (sel1)
							{
								ppu.bright = (byte)((portA_ret & 0x7F) << 1);
								ppu.bright_int_1 = (0xFF000000 | (uint)(ppu.bright << 16) | (uint)(ppu.bright << 8) | ppu.bright);
								ppu.bright = (byte)(portA_ret & 0x7F);
								ppu.bright_int_2 = (0xFF000000 | (uint)(ppu.bright << 16) | (uint)(ppu.bright << 8) | ppu.bright);
								ppu.bright = (byte)(portA_ret & 0x3F);
								ppu.bright_int_3 = (0xFF000000 | (uint)(ppu.bright << 16) | (uint)(ppu.bright << 8) | ppu.bright);
							}
							else
							{
								ppu.y_vel = (byte)(portA_ret ^ 0x80);							
							}
							ppu.new_draw_line();
						}
					}

					if (sel0)
					{
						if (sel1)
						{
							if ((byte)(portA_ret ^ 0x80) >= joy2_UD) { compare = false; }
							else { compare = true; }
						}
						else
						{
							if ((byte)(portA_ret ^ 0x80) >= joy1_UD) { compare = false; }
							else { compare = true; }
						}
					}
					else
					{
						if (sel1)
						{
							if ((byte)(portA_ret ^ 0x80) >= joy2_LR) { compare = false; }
							else { compare = true; }
						}
						else
						{
							if ((byte)(portA_ret ^ 0x80) >= joy1_LR) { compare = false; }
							else { compare = true; }
						}
					}

					ppu.x_vel = (byte)(portA_ret ^ 0x80);
					ppu.new_draw_line();

					int_fl &= 0xFC;
					update_int_fl();
					break;
				case 0x2:
					dir_ctrl = value;
					// the direction of bit 6 here controls the bank
					mapper.bank = dir_ctrl.Bit(6) ? 1 : 0;

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

					if (aux_ctrl.Bit(7))
					{
						PB7 = false;
						ppu.ramp_sig = true;
						ppu.new_draw_line();
					}

					t1_shot_go = true;

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
					shift_reg = value;
					shift_start = true;

					shift_reg_wait = 2;
					shift_count = 0;

					update_int_fl();
					break;
				case 0xB:
					if (aux_ctrl.Bit(7) && !value.Bit(7))
					{
						PB7_undriven = true;
					}

					aux_ctrl = value;
					t1_ctrl = aux_ctrl;
					
					break;
				case 0xC:
					prt_ctrl = value;

					// since CA2 / CB2 are tied to beam controls, most of their functions can be glossed over here
					// If there are games / demos that make use of other modes, they will have to be accounted for here

					if ((value & 0xE) == 0xC) { ppu.zero_sig = true; }
					else { ppu.zero_sig = false; }

					if ((value & 0xE0) == 0xC0) { ppu.blank_sig = true; }
					else { ppu.blank_sig = false; }

					ppu.new_draw_line();

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

					// writing to sound reg
					if (bdir)
					{
						if (bc1) { audio.port_sel = (byte)(portA_ret & 0xf); }
						else { audio.WriteReg(0, portA_ret); }
					}

					if (sw)
					{
						if (sel0)
						{
							if (sel1) {/* sound line? */ }
							else { ppu.vec_scale = portA_ret; }
						}
						else
						{
							if (sel1) { ppu.bright = portA_ret; Console.WriteLine("brightness change?"); }
							else { ppu.y_vel = portA_ret; ppu.new_draw_line(); }
						}
					}
					else
					{
						ppu.x_vel = portA_ret;
						ppu.new_draw_line();
					}

					if (sel0)
					{
						if (sel1)
						{
							if ((byte)(portA_ret ^ 0x80) >= joy2_UD) { compare = false; }
							else { compare = true; }
						}
						else
						{
							if ((byte)(portA_ret ^ 0x80) >= joy1_UD) { compare = false; }
							else { compare = true; }
						}
					}
					else
					{
						if (sel1)
						{
							if ((byte)(portA_ret ^ 0x80) >= joy2_LR) { compare = false; }
							else { compare = true; }
						}
						else
						{
							if ((byte)(portA_ret ^ 0x80) >= joy1_LR) { compare = false; }
							else { compare = true; }
						}
					}

					int_fl &= 0xFC;
					update_int_fl();
					break;
			}
		}

		public void internal_state_tick()
		{
			// Timer 1
			t1_counter--;

			if (t1_counter < 0)
			{
				if (t1_ctrl.Bit(6))
				{
					t1_counter = (t1_high << 8) | t1_low;

					int_fl |= 0x40;
					update_int_fl();
					if (int_en.Bit(6)) { cpu.IRQPending = true; }

					if (t1_ctrl.Bit(7)) { PB7 = !PB7; ppu.ramp_sig = !PB7; ppu.new_draw_line(); }
				}
				else
				{
					t1_counter = 0xFFFF;

					if (t1_shot_go)
					{
						int_fl |= 0x40;
						update_int_fl();
						if (int_en.Bit(6)) { cpu.IRQPending = true; }
						if (t1_ctrl.Bit(7)) { PB7 = true; ppu.ramp_sig = false; ppu.new_draw_line(); }

						t1_shot_go = false;
					}				
				}
			}

			// Timer 2
			t2_counter--;

			if (t2_counter < 0)
			{
				if (t2_ctrl.Bit(5))
				{
					t2_counter = (t2_high << 8) | t2_low;
					int_fl |= 0x20;
					update_int_fl();
					if (int_en.Bit(5)) { cpu.IRQPending = true; }
				}
				else
				{
					t2_counter = 0xFFFF;

					if (t2_shot_go)
					{
						int_fl |= 0x20;
						update_int_fl();
						if (int_en.Bit(5)) { cpu.IRQPending = true; }

						t2_shot_go = false;
					}
				}
				frame_end = true;
			}

			// Shift register
			if (shift_start)
			{
				if (shift_reg_wait > 0)
				{
					shift_reg_wait--;
				}
				else
				{
					// tick on every clock cycle
					if ((aux_ctrl & 0x1C) == 0x18)
					{
						if (shift_count == 8)
						{
							// reset blank signal back to contorl of peripheral controller
							shift_start = false;
							//if ((prt_ctrl & 0xE0) == 0xC0) { ppu.blank_sig = true; }
							//else { ppu.blank_sig = false; }
						}
						else
						{
							ppu.blank_sig = !shift_reg.Bit(7 - shift_count);
							ppu.new_draw_line();

							shift_count++;
							shift_reg_wait = 1;
						}
					}
					else
					{
						Console.WriteLine("method 2");
					}

					// other clocking modes are not used. Maybe some demos use them?
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

			if (!test) { cpu.IRQPending = false; }
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
			PB6 = PB6_prev = false;

			int_en = int_fl = aux_ctrl = 0;

			shift_reg = shift_reg_wait = 0;
			shift_start = false;
		}
	}
}
