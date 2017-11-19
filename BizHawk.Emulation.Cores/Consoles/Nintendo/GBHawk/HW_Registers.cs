using System;
using BizHawk.Emulation.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Common;

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

					input_register &= 0xF0;
					if ((input_register & 0x30) == 0x20)
					{
						input_register |= (byte)(controller_state & 0xF);
					}
					else if ((input_register & 0x30) == 0x10)
					{
						input_register |= (byte)((controller_state & 0xF0) >> 4);
					}
					else if ((input_register & 0x30) == 0x30)
					{
						// if both polls are set, then a bit is zero if either or both pins are zero
						byte temp = (byte)((controller_state & 0xF) & ((controller_state & 0xF0) >> 4));
						input_register |= temp;
					}
					else
					{
						input_register |= 0xF;
					}
					ret = input_register;
					break;

				// Serial data port
				case 0xFF01:
					ret = serial_data_in;
					break;

				// Serial port control
				case 0xFF02:
					ret = serial_control;
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
					ret = REG_FF0F;
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

				// Bios control register. Not sure if it is readable
				case 0xFF50:
					ret = 0xFF;
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
					input_register = (byte)(0xC0 | (value & 0x3F)); // top 2 bits always 1
					break;

				// Serial data port
				case 0xFF01:
					serial_data_out = value;
					serial_data_in = serial_data_out;
					break;

				// Serial port control
				case 0xFF02:
					serial_control = (byte)(0x7E | (value & 0x81)); // middle six bits always 1
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

				// Bios control register. Writing 1 permanently disables BIOS until a power cycle occurs
				case 0xFF50:
					//Console.WriteLine(value);
					if (GB_bios_register != 1)
					{
						GB_bios_register = value;
					}			
					break;

				// interrupt control register
				case 0xFFFF:
					REG_FFFF = value;
					enable_VBL = REG_FFFF.Bit(0);
					enable_STAT = REG_FFFF.Bit(1);
					enable_TIMO = REG_FFFF.Bit(2);
					enable_SER = REG_FFFF.Bit(3);
					enable_PRS = REG_FFFF.Bit(4);

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
			}
		}

		public void Register_Reset()
		{
			input_register = 0xCF; // not reading any input
			serial_control = 0x7E;
		}
	}
}
