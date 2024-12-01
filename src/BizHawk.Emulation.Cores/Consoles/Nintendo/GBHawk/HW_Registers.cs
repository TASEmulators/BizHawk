using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	public partial class GBHawk
	{
		public byte Read_Registers(int addr)
		{
			byte ret = 0;

			switch (addr)
			{
				// Read Input
				case 0xFF00:
					_islag = false;
					ret = input_register;
					break;

				// Serial data port
				case 0xFF01:
					ret = serialport.ReadReg(addr);
					break;

				// Serial port control
				case 0xFF02:
					ret = serialport.ReadReg(addr);
					break;

				// Timer Registers
				case 0xFF04:
				case 0xFF05:
				case 0xFF06:
				case 0xFF07:
					ret = timer.ReadReg(addr);
					break;

				// Interrupt flags
				case 0xFF0F:
					//Console.WriteLine("FF0F " + cpu.TotalExecutedCycles);
					ret = REG_FF0F_OLD;
					break;

				// audio regs
				case 0xFF10:
				case 0xFF11:
				case 0xFF12:
				case 0xFF13:
				case 0xFF14:
				case 0xFF16:
				case 0xFF17:
				case 0xFF18:
				case 0xFF19:
				case 0xFF1A:
				case 0xFF1B:
				case 0xFF1C:
				case 0xFF1D:
				case 0xFF1E:
				case 0xFF20:
				case 0xFF21:
				case 0xFF22:
				case 0xFF23:
				case 0xFF24:
				case 0xFF25:
				case 0xFF26:
				case 0xFF30:
				case 0xFF31:
				case 0xFF32:
				case 0xFF33:
				case 0xFF34:
				case 0xFF35:
				case 0xFF36:
				case 0xFF37:
				case 0xFF38:
				case 0xFF39:
				case 0xFF3A:
				case 0xFF3B:
				case 0xFF3C:
				case 0xFF3D:
				case 0xFF3E:
				case 0xFF3F:
					ret = audio.ReadReg(addr);
					break;

				// PPU Regs
				case 0xFF40:
				case 0xFF41:
				case 0xFF42:
				case 0xFF43:
				case 0xFF44:
				case 0xFF45:
				case 0xFF46:
				case 0xFF47:
				case 0xFF48:
				case 0xFF49:
				case 0xFF4A:
				case 0xFF4B:
					ret = ppu.ReadReg(addr);
					break;

				// Speed Control for GBC
				case 0xFF4D:
					if (GBC_compat)
					{
						ret = (byte)(((double_speed ? 1 : 0) << 7) | (speed_switch ? 1 : 0) | 0x7E);
					}
					else
					{
						ret = 0xFF;
					}
					break;

				case 0xFF4F: // VBK
					if (is_GBC)
					{
						ret = (byte)(0xFE | VRAM_Bank);
					}
					else
					{
						ret = 0xFF;
					}
					break;

				// Bios control register. Not sure if it is readable
				case 0xFF50:
					ret = 0xFF;
					break;

				// PPU Regs for GBC
				case 0xFF51:
				case 0xFF52:
				case 0xFF53:
				case 0xFF54:
				case 0xFF55:
					if (GBC_compat)
					{
						ret = ppu.ReadReg(addr);
					}
					else
					{
						ret = 0xFF;
					}
					break;

				case 0xFF56:
					if (GBC_compat)
					{
						// can receive data
						if ((IR_reg & 0xC0) == 0xC0)
						{
							ret = IR_reg;
						}
						else
						{
							ret = (byte)(IR_reg | 2);
						}
					}
					else
					{
						ret = 0xFF;
					}
					break;

				case 0xFF68:
				case 0xFF69:
				case 0xFF6A:
				case 0xFF6B:
					if (is_GBC)
					{
						ret = ppu.ReadReg(addr);
					}
					else
					{
						ret = 0xFF;
					}
					break;

				// Ram bank for GBC
				case 0xFF70:
					if (GBC_compat)
					{
						ret = (byte)(0xF8 | RAM_Bank_ret);
					}
					else
					{
						ret = 0xFF;
					}
					break;

				case 0xFF6C:
					if (GBC_compat) { ret = undoc_6C; }
					else { ret = 0xFF; }
					break;

				case 0xFF72:
					if (is_GBC) { ret = undoc_72; }
					else { ret = 0xFF; }
					break;

				case 0xFF73:
					if (is_GBC) { ret = undoc_73; }
					else { ret = 0xFF; }
					break;

				case 0xFF74:
					if (GBC_compat) { ret = undoc_74; }
					else { ret = 0xFF; }
					break;

				case 0xFF75:
					if (is_GBC) { ret = undoc_75; }
					else { ret = 0xFF; }
					break;

				case 0xFF76:
					var ret1 = audio.SQ1_output >= Audio.DAC_OFST
						? (byte) (audio.SQ1_output - Audio.DAC_OFST)
						: (byte) 0;
					var ret2 = audio.SQ2_output >= Audio.DAC_OFST
						? (byte) (audio.SQ2_output - Audio.DAC_OFST)
						: (byte) 0;
					if (is_GBC) { ret = (byte)(ret1 | (ret2 << 4)); }
					else { ret = 0xFF; }
					break;

				case 0xFF77:
					var retN = audio.NOISE_output >= Audio.DAC_OFST
						? (byte) (audio.NOISE_output - Audio.DAC_OFST)
						: (byte) 0;
					var retW = audio.WAVE_output >= Audio.DAC_OFST
						? (byte) (audio.WAVE_output - Audio.DAC_OFST)
						: (byte) 0;
					if (is_GBC) { ret = (byte)(retN | (retW << 4)); }
					else { ret = 0xFF; }
					break;

				// interrupt control register
				case 0xFFFF:
					ret = REG_FFFF;
					break;

				default:
					ret = 0xFF;
					break;

			}
			return ret;
		}

		public void Write_Registers(int addr, byte value)
		{
			switch (addr)
			{
				// select input
				case 0xFF00:
					input_register &= 0xCF;
					input_register |= (byte)(value & 0x30); // top 2 bits always 1

					// check for high to low transitions that trigger IRQs
					byte contr_prev = input_register;

					input_register &= 0xF0;
					if ((input_register & 0x30) == 0x20)
					{
						input_register |= (byte)(controller_state & 0xF);
					}
					else if ((input_register & 0x30) == 0x10)
					{
						input_register |= (byte)((controller_state & 0xF0) >> 4);
					}
					else if ((input_register & 0x30) == 0x00)
					{
						// if both polls are set, then a bit is zero if either or both pins are zero
						byte temp = (byte)((controller_state & 0xF) & ((controller_state & 0xF0) >> 4));
						input_register |= temp;
					}
					else
					{
						input_register |= 0xF;
					}
					
					// check for interrupts
					// if an interrupt is triggered, it is delayed by 4 cycles
					if (((contr_prev & 0b1000) is not 0 && (input_register & 0b1000) is 0)
						|| ((contr_prev & 0b100) is not 0 && (input_register & 0b100) is 0)
						|| ((contr_prev & 0b10) is not 0 && (input_register & 0b10) is 0)
						|| ((contr_prev & 0b1) is not 0 && (input_register & 0b1) is 0))
					{
						controller_delay_cd = 4; delays_to_process = true;
					}
					
					break;

				// Serial data port
				case 0xFF01:
					serialport.WriteReg(addr, value);
					break;

				// Serial port control
				case 0xFF02:
					serialport.WriteReg(addr, value);
					break;

				// Timer Registers
				case 0xFF04:
				case 0xFF05:
				case 0xFF06:
				case 0xFF07:
					timer.WriteReg(addr, value);
					break;

				// Interrupt flags
				case 0xFF0F:
					REG_FF0F = (byte)(0xE0 | value);

					// check if enabling any of the bits triggered an IRQ
					for (int i = 0; i < 5; i++)
					{
						if (REG_FFFF.Bit(i) && REG_FF0F.Bit(i))
						{
							cpu.FlagI = true;
						}
					}

					// if no bits are in common between flags and enables, de-assert the IRQ
					if (((REG_FF0F & 0x1F) & REG_FFFF) == 0) { cpu.FlagI = false; }
					break;

				// audio regs
				case 0xFF10:
				case 0xFF11:
				case 0xFF12:
				case 0xFF13:
				case 0xFF14:
				case 0xFF16:
				case 0xFF17:
				case 0xFF18:
				case 0xFF19:
				case 0xFF1A:
				case 0xFF1B:
				case 0xFF1C:
				case 0xFF1D:
				case 0xFF1E:
				case 0xFF20:
				case 0xFF21:
				case 0xFF22:
				case 0xFF23:
				case 0xFF24:
				case 0xFF25:
				case 0xFF26:
				case 0xFF30:
				case 0xFF31:
				case 0xFF32:
				case 0xFF33:
				case 0xFF34:
				case 0xFF35:
				case 0xFF36:
				case 0xFF37:
				case 0xFF38:
				case 0xFF39:
				case 0xFF3A:
				case 0xFF3B:
				case 0xFF3C:
				case 0xFF3D:
				case 0xFF3E:
				case 0xFF3F:
					audio.WriteReg(addr, value);
					break;

				// PPU Regs
				case 0xFF40:
				case 0xFF41:
				case 0xFF42:
				case 0xFF43:
				case 0xFF44:
				case 0xFF45:
				case 0xFF46:
				case 0xFF47:
				case 0xFF48:
				case 0xFF49:
				case 0xFF4A:
				case 0xFF4B:
					ppu.WriteReg(addr, value);
					break;

				// GBC compatibility register (I think)
				case 0xFF4C:
					if ((value != 0xC0) && (value != 0x80) && (GB_bios_register == 0))// && (value != 0xFF) && (value != 0x04))
					{
						GBC_compat = false;
					}
					Console.Write("GBC Compatibility? ");
					Console.WriteLine(value);
					break;

				// Speed Control for GBC
				case 0xFF4D:
					if (GBC_compat)
					{
						speed_switch = (value & 1) > 0;
					}
					break;

				// VBK
				case 0xFF4F:
					if (is_GBC/* && !ppu.HDMA_active*/)
					{
						VRAM_Bank = (byte)(value & 1);
					}
					break;

				// Bios control register. Writing 1 permanently disables BIOS until a power cycle occurs
				case 0xFF50:
					// Console.WriteLine(value);
					if (GB_bios_register == 0)
					{
						GB_bios_register = value;
						if (!GBC_compat) { ppu.pal_change_blocked = true; RAM_Bank = 1; RAM_Bank_ret = 0; }
					}			
					break;

				// PPU Regs for GBC
				case 0xFF51:
				case 0xFF52:
				case 0xFF53:
				case 0xFF54:
				case 0xFF55:
					if (GBC_compat)
					{
						ppu.WriteReg(addr, value);
					}
					break;

				case 0xFF56:
					if (is_GBC)
					{
						IR_reg = (byte)((value & 0xC1) | (IR_reg & 0x3E));

						// send IR signal out
						if ((IR_reg & 0x1) == 0x1) { IR_signal = (byte)(0 | IR_mask); } else { IR_signal = 2; }
						
						// receive own signal if IR on and receive on
						if ((IR_reg & 0xC1) == 0xC1) { IR_self = (byte)(0 | IR_mask); } else { IR_self = 2; }

						IR_write = 8;
					}
					break;

				case 0xFF68:
				case 0xFF69:
				case 0xFF6A:
				case 0xFF6B:
					if (is_GBC)
					{
						ppu.WriteReg(addr, value);
					}
					break;

				// RAM Bank in GBC mode
				case 0xFF70:
					if (GBC_compat)
					{
						RAM_Bank = value & 7;
						RAM_Bank_ret = RAM_Bank;
						if (RAM_Bank == 0) { RAM_Bank = 1; }
					}
					break;

				case 0xFF6C:
					if (GBC_compat) { undoc_6C |= (byte)(value & 1); }
					break;

				case 0xFF72:
					if (is_GBC) { undoc_72 = value; }
					break;

				case 0xFF73:
					if (is_GBC) { undoc_73 = value; }
					break;

				case 0xFF74:
					if (GBC_compat) { undoc_74 = value; }
					break;

				case 0xFF75:
					if (is_GBC) { undoc_75 |= (byte)(value & 0x70); }
					break;

				case 0xFF76:
					// read only
					break;

				case 0xFF77:
					// read only
					break;

				// interrupt control register
				case 0xFFFF:
					REG_FFFF = value;

					// check if enabling any of the bits triggered an IRQ
					for (int i = 0; i < 5; i++)
					{
						if (REG_FFFF.Bit(i) && REG_FF0F.Bit(i))
						{
							cpu.FlagI = true;
						}
					}

					// if no bits are in common between flags and enables, de-assert the IRQ
					if (((REG_FF0F & 0x1F) & REG_FFFF) == 0) { cpu.FlagI = false; }
					break;

				default:
					//Console.WriteLine(addr + " " + value);
					break;
			}
		}

		public void Register_Reset()
		{
			input_register = 0xCF; // not reading any input

			REG_FFFF = 0;
			REG_FF0F = 0xE0;
			REG_FF0F_OLD = 0xE0;

			//undocumented registers
			undoc_6C = 0xFE;
			undoc_72 = 0;
			undoc_73 = 0;
			undoc_74 = 0;
			undoc_75 = 0x8F;
			undoc_76 = 0;
			undoc_77 = 0;
		}
	}
}
