using System;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.O2Hawk
{
	public partial class O2Hawk
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

				// Interrupt flags
				case 0xFF0F:

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

					break;

				case 0xFF4F: // VBK

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

					break;

				case 0xFF56:

					break;

				case 0xFF68:
				case 0xFF69:
				case 0xFF6A:
				case 0xFF6B:

					break;

				// Speed Control for GBC
				case 0xFF70:

					break;

				case 0xFF6C:
				case 0xFF72:
				case 0xFF73:
				case 0xFF74:
				case 0xFF75:
				case 0xFF76:
				case 0xFF77:

					break;

				// interrupt control register
				case 0xFFFF:

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

					break;

				// Serial data port
				case 0xFF01:
					serialport.WriteReg(addr, value);
					break;

				// Serial port control
				case 0xFF02:
					serialport.WriteReg(addr, value);
					break;

				// Interrupt flags
				case 0xFF0F:

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

					break;

				// Speed Control for GBC
				case 0xFF4D:

					break;

				// VBK
				case 0xFF4F:

					break;

				// Bios control register. Writing 1 permanently disables BIOS until a power cycle occurs
				case 0xFF50:
		
					break;

				// PPU Regs for GBC
				case 0xFF51:
				case 0xFF52:
				case 0xFF53:
				case 0xFF54:
				case 0xFF55:

					break;

				case 0xFF56:

					break;

				case 0xFF68:
				case 0xFF69:
				case 0xFF6A:
				case 0xFF6B:
					//if (GBC_compat)
					//{
						ppu.WriteReg(addr, value);
					//}
					break;

				default:
					Console.Write(addr);
					Console.Write(" ");
					Console.WriteLine(value);
					break;
			}
		}

		public void Register_Reset()
		{
			input_register = 0xCF; // not reading any input
		}
	}
}
