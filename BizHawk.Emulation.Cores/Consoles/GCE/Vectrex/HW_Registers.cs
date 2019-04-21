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

		public byte Read_Registers(int addr)
		{
			byte ret = 0;

			switch (addr)
			{
				case 0x0:
					ret = portB_ret;
					break;
				case 0x1:
					ret = portA_ret;
					break;
				case 0x2:
					ret = dir_ctrl;
					break;
				case 0x3:
					ret = dir_dac;
					break;
				case 0x4:
					break;
				case 0x5:
					break;
				case 0x6:
					break;
				case 0x7:
					break;
				case 0x8:
					break;
				case 0x9:
					break;
				case 0xA:
					break;
				case 0xB:
					break;
				case 0xC:
					break;
				case 0xD:
					break;
				case 0xE:
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
					break;
				case 0x1:
					wrt_val = (byte)(value & dir_dac);
					break;
				case 0x2:
					dir_ctrl = value;
					break;
				case 0x3:
					dir_dac = value;
					break;
				case 0x4:
					break;
				case 0x5:
					break;
				case 0x6:
					break;
				case 0x7:
					break;
				case 0x8:
					break;
				case 0x9:
					break;
				case 0xA:
					break;
				case 0xB:
					break;
				case 0xC:
					break;
				case 0xD:
					break;
				case 0xE:
					break;
				case 0xF:
					break;
			}
		}

		public void Register_Reset()
		{

		}
	}
}
