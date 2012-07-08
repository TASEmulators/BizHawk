using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.CPUs.CP1610
{
	public sealed partial class CP1610
	{
		private void Calc_FlagC(int result)
		{
			FlagC = ((result & 0x10000) != 0);
		}

		private void Calc_FlagO_Add(int op1, int op2)
		{
			FlagO = (((op1 ^ (op1 + op2)) & 0x8000) != 0);
		}

		private void Calc_FlagS(int result)
		{
			FlagS = ((result & 0x8000) != 0);
		}

		private void Calc_FlagZ(int result)
		{
			FlagZ = (result == 0);
		}

		public void Execute(int cycles)
		{
			byte target;
			int op1;
			int op2;
			int temp;
			int result;
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
						result = (Register[target] + 1) & 0xFFFF;
						Calc_FlagS(result);
						Calc_FlagZ(result);
						Register[target] = (ushort)result;
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
						result = (Register[target] - 1) & 0xFFFF;
						Calc_FlagS(result);
						Calc_FlagZ(result);
						Register[target] = (ushort)result;
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
						result = Register[target] ^ 0xFFFF;
						Calc_FlagS(result);
						Calc_FlagZ(result);
						Register[target] = (ushort)result;
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
						op1 = Register[target];
						temp = (op1 ^ 0xFFFF);
						result = temp + 1;
						Calc_FlagC(result);
						Calc_FlagO_Add(temp, 1);
						Calc_FlagS(result);
						Calc_FlagZ(result);
						result &= 0xFFFF;
						Register[target] = (ushort)result;
						PendingCycles -= 6; TotalExecutedCycles += 6;
						break;
					// ADCR
					case 0x028:
					case 0x029:
					case 0x02A:
					case 0x02B:
					case 0x02C:
					case 0x02D:
					case 0x02E:
					case 0x02F:
						target = (byte)(opcode & 0x7);
						op1 = Register[target];
						op2 = FlagC ? 1 : 0;
						result = op1 + op2;
						Calc_FlagC(result);
						Calc_FlagO_Add(op1, op2);
						Calc_FlagS(result);
						Calc_FlagZ(result);
						result &= 0xFFFF;
						Register[target] = (ushort)result;
						PendingCycles -= 6; TotalExecutedCycles += 6;
						break;
					// GSWD
					case 0x030:
					case 0x031:
					case 0x032:
					case 0x033:
						throw new NotImplementedException();
					// NOP
					case 0x034:
					case 0x035:
						throw new NotImplementedException();
					// SIN
					case 0x036:
					case 0x037:
						throw new NotImplementedException();
					// RSWD
					case 0x038:
					case 0x039:
					case 0x03A:
					case 0x03B:
					case 0x03C:
					case 0x03D:
					case 0x03E:
					case 0x03F:
						throw new NotImplementedException();
					// SWAP
					case 0x040:
					case 0x041:
					case 0x042:
					case 0x043:
					case 0x044:
					case 0x045:
					case 0x046:
					case 0x047:
						throw new NotImplementedException();
					// SLL
					case 0x048:
					case 0x049:
					case 0x04A:
					case 0x04B:
					case 0x04C:
					case 0x04D:
					case 0x04E:
					case 0x04F:
						throw new NotImplementedException();
					// RLC
					case 0x050:
					case 0x051:
					case 0x052:
					case 0x053:
					case 0x054:
					case 0x055:
					case 0x056:
					case 0x057:
						throw new NotImplementedException();
					// SLLC
					case 0x058:
					case 0x059:
					case 0x05A:
					case 0x05B:
					case 0x05C:
					case 0x05D:
					case 0x05E:
					case 0x05F:
						throw new NotImplementedException();
					// SLR
					case 0x060:
					case 0x061:
					case 0x062:
					case 0x063:
					case 0x064:
					case 0x065:
					case 0x066:
					case 0x067:
						throw new NotImplementedException();
					// SAR
					case 0x068:
					case 0x069:
					case 0x06A:
					case 0x06B:
					case 0x06C:
					case 0x06D:
					case 0x06E:
					case 0x06F:
						throw new NotImplementedException();
					// RRC
					case 0x070:
					case 0x071:
					case 0x072:
					case 0x073:
					case 0x074:
					case 0x075:
					case 0x076:
					case 0x077:
						throw new NotImplementedException();
					// SARC
					case 0x078:
					case 0x079:
					case 0x07A:
					case 0x07B:
					case 0x07C:
					case 0x07D:
					case 0x07E:
					case 0x07F:
						throw new NotImplementedException();
					// MOVR
					case 0x080:
					case 0x081:
					case 0x082:
					case 0x083:
					case 0x084:
					case 0x085:
					case 0x086:
					case 0x087:
					case 0x088:
					case 0x089:
					case 0x08A:
					case 0x08B:
					case 0x08C:
					case 0x08D:
					case 0x08E:
					case 0x08F:
					case 0x090:
					case 0x091:
					case 0x092:
					case 0x093:
					case 0x094:
					case 0x095:
					case 0x096:
					case 0x097:
					case 0x098:
					case 0x099:
					case 0x09A:
					case 0x09B:
					case 0x09C:
					case 0x09D:
					case 0x09E:
					case 0x09F:
					case 0x0A0:
					case 0x0A1:
					case 0x0A2:
					case 0x0A3:
					case 0x0A4:
					case 0x0A5:
					case 0x0A6:
					case 0x0A7:
					case 0x0A8:
					case 0x0A9:
					case 0x0AA:
					case 0x0AB:
					case 0x0AC:
					case 0x0AD:
					case 0x0AE:
					case 0x0AF:
					case 0x0B0:
					case 0x0B1:
					case 0x0B2:
					case 0x0B3:
					case 0x0B4:
					case 0x0B5:
					case 0x0B6:
					case 0x0B7:
					case 0x0B8:
					case 0x0B9:
					case 0x0BA:
					case 0x0BB:
					case 0x0BC:
					case 0x0BD:
					case 0x0BE:
					case 0x0BF:
						throw new NotImplementedException();
					// ADDR
					case 0x0C0:
					case 0x0C1:
					case 0x0C2:
					case 0x0C3:
					case 0x0C4:
					case 0x0C5:
					case 0x0C6:
					case 0x0C7:
					case 0x0C8:
					case 0x0C9:
					case 0x0CA:
					case 0x0CB:
					case 0x0CC:
					case 0x0CD:
					case 0x0CE:
					case 0x0CF:
					case 0x0D0:
					case 0x0D1:
					case 0x0D2:
					case 0x0D3:
					case 0x0D4:
					case 0x0D5:
					case 0x0D6:
					case 0x0D7:
					case 0x0D8:
					case 0x0D9:
					case 0x0DA:
					case 0x0DB:
					case 0x0DC:
					case 0x0DD:
					case 0x0DE:
					case 0x0DF:
					case 0x0E0:
					case 0x0E1:
					case 0x0E2:
					case 0x0E3:
					case 0x0E4:
					case 0x0E5:
					case 0x0E6:
					case 0x0E7:
					case 0x0E8:
					case 0x0E9:
					case 0x0EA:
					case 0x0EB:
					case 0x0EC:
					case 0x0ED:
					case 0x0EE:
					case 0x0EF:
					case 0x0F0:
					case 0x0F1:
					case 0x0F2:
					case 0x0F3:
					case 0x0F4:
					case 0x0F5:
					case 0x0F6:
					case 0x0F7:
					case 0x0F8:
					case 0x0F9:
					case 0x0FA:
					case 0x0FB:
					case 0x0FC:
					case 0x0FD:
					case 0x0FE:
					case 0x0FF:
						throw new NotImplementedException();
					// SUBR
					case 0x100:
					case 0x101:
					case 0x102:
					case 0x103:
					case 0x104:
					case 0x105:
					case 0x106:
					case 0x107:
					case 0x108:
					case 0x109:
					case 0x10A:
					case 0x10B:
					case 0x10C:
					case 0x10D:
					case 0x10E:
					case 0x10F:
					case 0x110:
					case 0x111:
					case 0x112:
					case 0x113:
					case 0x114:
					case 0x115:
					case 0x116:
					case 0x117:
					case 0x118:
					case 0x119:
					case 0x11A:
					case 0x11B:
					case 0x11C:
					case 0x11D:
					case 0x11E:
					case 0x11F:
					case 0x120:
					case 0x121:
					case 0x122:
					case 0x123:
					case 0x124:
					case 0x125:
					case 0x126:
					case 0x127:
					case 0x128:
					case 0x129:
					case 0x12A:
					case 0x12B:
					case 0x12C:
					case 0x12D:
					case 0x12E:
					case 0x12F:
					case 0x130:
					case 0x131:
					case 0x132:
					case 0x133:
					case 0x134:
					case 0x135:
					case 0x136:
					case 0x137:
					case 0x138:
					case 0x139:
					case 0x13A:
					case 0x13B:
					case 0x13C:
					case 0x13D:
					case 0x13E:
					case 0x13F:
						throw new NotImplementedException();
					// CMPR
					case 0x140:
					case 0x141:
					case 0x142:
					case 0x143:
					case 0x144:
					case 0x145:
					case 0x146:
					case 0x147:
					case 0x148:
					case 0x149:
					case 0x14A:
					case 0x14B:
					case 0x14C:
					case 0x14D:
					case 0x14E:
					case 0x14F:
					case 0x150:
					case 0x151:
					case 0x152:
					case 0x153:
					case 0x154:
					case 0x155:
					case 0x156:
					case 0x157:
					case 0x158:
					case 0x159:
					case 0x15A:
					case 0x15B:
					case 0x15C:
					case 0x15D:
					case 0x15E:
					case 0x15F:
					case 0x160:
					case 0x161:
					case 0x162:
					case 0x163:
					case 0x164:
					case 0x165:
					case 0x166:
					case 0x167:
					case 0x168:
					case 0x169:
					case 0x16A:
					case 0x16B:
					case 0x16C:
					case 0x16D:
					case 0x16E:
					case 0x16F:
					case 0x170:
					case 0x171:
					case 0x172:
					case 0x173:
					case 0x174:
					case 0x175:
					case 0x176:
					case 0x177:
					case 0x178:
					case 0x179:
					case 0x17A:
					case 0x17B:
					case 0x17C:
					case 0x17D:
					case 0x17E:
					case 0x17F:
						throw new NotImplementedException();
					// ANDR
					case 0x180:
					case 0x181:
					case 0x182:
					case 0x183:
					case 0x184:
					case 0x185:
					case 0x186:
					case 0x187:
					case 0x188:
					case 0x189:
					case 0x18A:
					case 0x18B:
					case 0x18C:
					case 0x18D:
					case 0x18E:
					case 0x18F:
					case 0x190:
					case 0x191:
					case 0x192:
					case 0x193:
					case 0x194:
					case 0x195:
					case 0x196:
					case 0x197:
					case 0x198:
					case 0x199:
					case 0x19A:
					case 0x19B:
					case 0x19C:
					case 0x19D:
					case 0x19E:
					case 0x19F:
					case 0x1A0:
					case 0x1A1:
					case 0x1A2:
					case 0x1A3:
					case 0x1A4:
					case 0x1A5:
					case 0x1A6:
					case 0x1A7:
					case 0x1A8:
					case 0x1A9:
					case 0x1AA:
					case 0x1AB:
					case 0x1AC:
					case 0x1AD:
					case 0x1AE:
					case 0x1AF:
					case 0x1B0:
					case 0x1B1:
					case 0x1B2:
					case 0x1B3:
					case 0x1B4:
					case 0x1B5:
					case 0x1B6:
					case 0x1B7:
					case 0x1B8:
					case 0x1B9:
					case 0x1BA:
					case 0x1BB:
					case 0x1BC:
					case 0x1BD:
					case 0x1BE:
					case 0x1BF:
						throw new NotImplementedException();
					// XORR
					case 0x1C0:
					case 0x1C1:
					case 0x1C2:
					case 0x1C3:
					case 0x1C4:
					case 0x1C5:
					case 0x1C6:
					case 0x1C7:
					case 0x1C8:
					case 0x1C9:
					case 0x1CA:
					case 0x1CB:
					case 0x1CC:
					case 0x1CD:
					case 0x1CE:
					case 0x1CF:
					case 0x1D0:
					case 0x1D1:
					case 0x1D2:
					case 0x1D3:
					case 0x1D4:
					case 0x1D5:
					case 0x1D6:
					case 0x1D7:
					case 0x1D8:
					case 0x1D9:
					case 0x1DA:
					case 0x1DB:
					case 0x1DC:
					case 0x1DD:
					case 0x1DE:
					case 0x1DF:
					case 0x1E0:
					case 0x1E1:
					case 0x1E2:
					case 0x1E3:
					case 0x1E4:
					case 0x1E5:
					case 0x1E6:
					case 0x1E7:
					case 0x1E8:
					case 0x1E9:
					case 0x1EA:
					case 0x1EB:
					case 0x1EC:
					case 0x1ED:
					case 0x1EE:
					case 0x1EF:
					case 0x1F0:
					case 0x1F1:
					case 0x1F2:
					case 0x1F3:
					case 0x1F4:
					case 0x1F5:
					case 0x1F6:
					case 0x1F7:
					case 0x1F8:
					case 0x1F9:
					case 0x1FA:
					case 0x1FB:
					case 0x1FC:
					case 0x1FD:
					case 0x1FE:
					case 0x1FF:
						throw new NotImplementedException();
					// Branch Forward, no External Condition
					case 0x200: // B
					case 0x201: // BC
					case 0x202: // BOV
					case 0x203: // BPL
					case 0x204: // BEQ
					case 0x205: // BLT
					case 0x206: // BLE
					case 0x207: // BUSC
					case 0x208: // NOPP
					case 0x209: // BNC
					case 0x20A: // BNOV
					case 0x20B: // BMI
					case 0x20C: // BNEQ
					case 0x20D: // BGE
					case 0x20E: // BGT
					case 0x20F: // BESC
					// Branch Forward, Exteranl Condition (BEXT)
					case 0x210:
					case 0x211:
					case 0x212:
					case 0x213:
					case 0x214:
					case 0x215:
					case 0x216:
					case 0x217:
					case 0x218:
					case 0x219:
					case 0x21A:
					case 0x21B:
					case 0x21C:
					case 0x21D:
					case 0x21E:
					case 0x21F:
					// Branch Reverse, no External Condition
					case 0x220: // B
					case 0x221: // BC
					case 0x222: // BOV
					case 0x223: // BPL
					case 0x224: // BEQ
					case 0x225: // BLT
					case 0x226: // BLE
					case 0x227: // BUSC
					case 0x228: // NOPP
					case 0x229: // BNC
					case 0x22A: // BNOV
					case 0x22B: // BMI
					case 0x22C: // BNEQ
					case 0x22D: // BGE
					case 0x22E: // BGT
					case 0x22F: // BESC
					// Branch Reverse, Exteranl Condition (BEXT)
					case 0x230:
					case 0x231:
					case 0x232:
					case 0x233:
					case 0x234:
					case 0x235:
					case 0x236:
					case 0x237:
					case 0x238:
					case 0x239:
					case 0x23A:
					case 0x23B:
					case 0x23C:
					case 0x23D:
					case 0x23E:
					case 0x23F:
						throw new NotImplementedException();
					default:
						throw new NotImplementedException();
				}
			}
		}
	}
}
