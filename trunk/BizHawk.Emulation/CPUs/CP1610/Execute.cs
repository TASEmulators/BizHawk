using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.CPUs.CP1610
{
	public sealed partial class CP1610
	{
		public void Execute(int cycles)
		{
			byte target;
			int value;
            PendingCycles += cycles;
			while (PendingCycles > 0)
			{
				int opcode = ReadMemory(RegisterPC++) & 0x3FF;
				switch (opcode)
				{
					case 0x000: // HLT
						throw new NotImplementedException();
					case 0x001: // SDBD
						FlagD = true;
						PendingCycles -= 4; TotalExecutedCycles += 4;
						break;
					case 0x002: // EIS
						FlagI = true;
						PendingCycles -= 4; TotalExecutedCycles += 4;
						break;
					case 0x003: // DIS
						FlagI = false;
						PendingCycles -= 4; TotalExecutedCycles += 4;
						break;
					case 0x004: // J, JE, JD, JSR, JSRE, JSRD
						throw new NotImplementedException();
					case 0x005: // TCI
						throw new NotImplementedException();
					case 0x006: // CLRC
						FlagC = false;
						PendingCycles -= 4; TotalExecutedCycles += 4;
						break;
					case 0x007: // SETC
						FlagC = true;
						PendingCycles -= 4; TotalExecutedCycles += 4;
						break;
					// INCR
					case 0x008:
					case 0x009:
					case 0x00A:
					case 0x00B:
					case 0x00C:
					case 0x00D:
					case 0x00E:
					case 0x00F:
						target = (byte)(opcode & 0x7);
						value = (Register[target] + 1) & 0xFFFF;
						FlagS = ((value & 0x8000) != 0);
						FlagZ = (value == 0);
						Register[target] = (ushort)value;
						PendingCycles -= 6; TotalExecutedCycles += 6;
						break;
					// DECR
					case 0x010:
					case 0x011:
					case 0x012:
					case 0x013:
					case 0x014:
					case 0x015:
					case 0x016:
					case 0x017:
						target = (byte)(opcode & 0x7);
						value = (Register[target] - 1) & 0xFFFF;
						FlagS = ((value & 0x8000) != 0);
						FlagZ = (value == 0);
						Register[target] = (ushort)value;
						PendingCycles -= 6; TotalExecutedCycles += 6;
						break;
					// COMR
					case 0x018:
					case 0x019:
					case 0x01A:
					case 0x01B:
					case 0x01C:
					case 0x01D:
					case 0x01E:
					case 0x01F:
						target = (byte)(opcode & 0x7);
						value = Register[target] ^ 0xFFFF;
						FlagS = ((value & 0x8000) != 0);
						FlagZ = (value == 0);
						Register[target] = (ushort)value;
						PendingCycles -= 6; TotalExecutedCycles += 6;
						break;
					// NEGR
					case 0x020:
					case 0x021:
					case 0x022:
					case 0x023:
					case 0x024:
					case 0x025:
					case 0x026:
					case 0x027:
						target = (byte)(opcode & 0x7);
						value = (Register[target] ^ 0xFFFF) + 1;
						FlagC = ((value & 0x10000) != 0);
						// 0x8000 is the only value that overflows when negated.
						FlagO = (Register[target] == 0x8000);
						value &= 0xFFFF;
						FlagS = ((value & 0x8000) != 0);
						FlagZ = (value == 0);
						Register[target] = (ushort)value;
						PendingCycles -= 6; TotalExecutedCycles += 6;
						break;
					default:
						throw new NotImplementedException();
				}
			}
		}
	}
}
