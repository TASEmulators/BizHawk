﻿namespace BizHawk.Emulation.Cores.Components.CP1610
{
	public sealed partial class CP1610
	{
		public const string UNKNOWN = "???";

		public string Disassemble(ushort pc, out int addrToAdvance)
		{
			addrToAdvance = 1;
			byte dest, src, mem;
			ushort addr, offset;
			int decle2, decle3, cond, ext;
			string result = "";
#pragma warning disable MA0084 // shadows `int this.opcode`
			int opcode = ReadMemory(pc, true) & 0x3FF;
#pragma warning restore MA0084
			switch (opcode)
			{
				case 0x000:
					return "HLT";
				case 0x001:
					return "SDBD";
				case 0x002:
					return "EIS";
				case 0x003:
					return "DIS";
				case 0x004:
					// 0000:0000:0000:0100    0000:00rr:aaaa:aaff    0000:00aa:aaaa:aaaa
					decle2 = ReadMemory((ushort)(pc + 1), true);
					decle3 = ReadMemory((ushort)(pc + 2), true);
					// rr indicates the register into which to store the return address
					dest = (byte)(((decle2 >> 8) & 0x3) + 4);
					// aaaaaaaaaaaaaaaa indicates the address to where the CP1610 should Jump
					addr = (ushort)(((decle2 << 8) & 0xFC00) | (decle3 & 0x3FF));
					result = "J";
					if (dest != 0x7)
					{
						result += "SR";
					}
					// ff indicates how to affect the Interrupt (I) flag in the CP1610
					switch (decle2 & 0x3)
					{
						case 0x1:
							result += "E";
							break;
						case 0x2:
							result += "D";
							break;
						case 0x3:
							// Unknown opcode.
							return UNKNOWN;
					}
					if (dest != 0x3)
					{
						result += " R" + dest + ",";
					}
					result += $" ${addr:X4}";
					addrToAdvance = 3;
					return result;
				case 0x005:
					return "TCI";
				case 0x006:
					return "CLRC";
				case 0x007:
					return "SETC";
				case 0x008:
				case 0x009:
				case 0x00A:
				case 0x00B:
				case 0x00C:
				case 0x00D:
				case 0x00E:
				case 0x00F:
					dest = (byte)(opcode & 0x7);
					return "INCR R" + dest;
				case 0x010:
				case 0x011:
				case 0x012:
				case 0x013:
				case 0x014:
				case 0x015:
				case 0x016:
				case 0x017:
					dest = (byte)(opcode & 0x7);
					return "DECR R" + dest;
				case 0x018:
				case 0x019:
				case 0x01A:
				case 0x01B:
				case 0x01C:
				case 0x01D:
				case 0x01E:
				case 0x01F:
					dest = (byte)(opcode & 0x7);
					return "COMR R" + dest;
				case 0x020:
				case 0x021:
				case 0x022:
				case 0x023:
				case 0x024:
				case 0x025:
				case 0x026:
				case 0x027:
					dest = (byte)(opcode & 0x7);
					return "NEGR R" + dest;
				case 0x028:
				case 0x029:
				case 0x02A:
				case 0x02B:
				case 0x02C:
				case 0x02D:
				case 0x02E:
				case 0x02F:
					dest = (byte)(opcode & 0x7);
					return "ADCR R" + dest;
				case 0x030:
				case 0x031:
				case 0x032:
				case 0x033:
					dest = (byte)(opcode & 0x3);
					return "GSWD R" + dest;
				case 0x034:
				case 0x035:
					result = "NOP";
					if ((opcode & 0x1) != 0)
					{
						result += " 1";
					}
					return result;
				case 0x036:
				case 0x037:
					result = "SIN";
					if ((opcode & 0x1) != 0)
					{
						result += " 1";
					}
					return result;
				case 0x038:
				case 0x039:
				case 0x03A:
				case 0x03B:
				case 0x03C:
				case 0x03D:
				case 0x03E:
				case 0x03F:
					src = (byte)(opcode & 0x7);
					return "RSWD R" + src;
				case 0x040:
				case 0x041:
				case 0x042:
				case 0x043:
				case 0x044:
				case 0x045:
				case 0x046:
				case 0x047:
					dest = (byte)(opcode & 0x3);
					result = "SWAP R" + dest;
					if (((opcode >> 2) & 0x1) != 0)
					{
						result += ", 1";
					}
					return result;
				case 0x048:
				case 0x049:
				case 0x04A:
				case 0x04B:
				case 0x04C:
				case 0x04D:
				case 0x04E:
				case 0x04F:
					dest = (byte)(opcode & 0x3);
					result = "SLL R" + dest;
					if (((opcode >> 2) & 0x1) != 0)
					{
						result += ", 1";
					}
					return result;
				case 0x050:
				case 0x051:
				case 0x052:
				case 0x053:
				case 0x054:
				case 0x055:
				case 0x056:
				case 0x057:
					dest = (byte)(opcode & 0x3);
					result = "RLC R" + dest;
					if (((opcode >> 2) & 0x1) != 0)
					{
						result += ", 1";
					}
					return result;
				case 0x058:
				case 0x059:
				case 0x05A:
				case 0x05B:
				case 0x05C:
				case 0x05D:
				case 0x05E:
				case 0x05F:
					dest = (byte)(opcode & 0x3);
					result = "SLLC R" + dest;
					if (((opcode >> 2) & 0x1) != 0)
					{
						result += ", 1";
					}
					return result;
				case 0x060:
				case 0x061:
				case 0x062:
				case 0x063:
				case 0x064:
				case 0x065:
				case 0x066:
				case 0x067:
					dest = (byte)(opcode & 0x3);
					result = "SLR R" + dest;
					if (((opcode >> 2) & 0x1) != 0)
					{
						result += ", 1";
					}
					return result;
				case 0x068:
				case 0x069:
				case 0x06A:
				case 0x06B:
				case 0x06C:
				case 0x06D:
				case 0x06E:
				case 0x06F:
					dest = (byte)(opcode & 0x3);
					result = "SAR R" + dest;
					if (((opcode >> 2) & 0x1) != 0)
					{
						result += ", 1";
					}
					return result;
				case 0x070:
				case 0x071:
				case 0x072:
				case 0x073:
				case 0x074:
				case 0x075:
				case 0x076:
				case 0x077:
					dest = (byte)(opcode & 0x3);
					result = "RRC R" + dest;
					if (((opcode >> 2) & 0x1) != 0)
					{
						result += ", 1";
					}
					return result;
				case 0x078:
				case 0x079:
				case 0x07A:
				case 0x07B:
				case 0x07C:
				case 0x07D:
				case 0x07E:
				case 0x07F:
					dest = (byte)(opcode & 0x3);
					result = "SARC R" + dest;
					if (((opcode >> 2) & 0x1) != 0)
					{
						result += ", 1";
					}
					return result;
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
					src = (byte)((opcode >> 3) & 0x7);
					dest = (byte)(opcode & 0x7);
					return "MOVR R" + src + ", R" + dest;
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
					src = (byte)((opcode >> 3) & 0x7);
					dest = (byte)(opcode & 0x7);
					return "ADDR R" + src + ", R" + dest;
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
					src = (byte)((opcode >> 3) & 0x7);
					dest = (byte)(opcode & 0x7);
					return "SUBR R" + src + ", R" + dest;
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
					src = (byte)((opcode >> 3) & 0x7);
					dest = (byte)(opcode & 0x7);
					return "CMPR R" + src + ", R" + dest;
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
					src = (byte)((opcode >> 3) & 0x7);
					dest = (byte)(opcode & 0x7);
					return "ANDR R" + src + ", R" + dest;
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
					src = (byte)((opcode >> 3) & 0x7);
					dest = (byte)(opcode & 0x7);
					return "XORR R" + src + ", R" + dest;
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
					offset = ReadMemory((ushort)(pc + 1), true);
					cond = opcode & 0xF;
					ext = opcode & 0x10;
					if (ext != 0)
					{
						result = "BEXT";
					}
					else
					{
						switch (cond)
						{
							case 0x0:
								result = "B";
								break;
							case 0x1:
								result = "BC";
								break;
							case 0x2:
								result = "BOV";
								break;
							case 0x3:
								result = "BPL";
								break;
							case 0x4:
								result = "BEQ";
								break;
							case 0x5:
								result = "BLT";
								break;
							case 0x6:
								result = "BLE";
								break;
							case 0x7:
								result = "BUSC";
								break;
							case 0x8:
								result = "NOPP";
								break;
							case 0x9:
								result = "BNC";
								break;
							case 0xA:
								result = "BNOV";
								break;
							case 0xB:
								result = "BMI";
								break;
							case 0xC:
								result = "BNEQ";
								break;
							case 0xD:
								result = "BGE";
								break;
							case 0xE:
								result = "BGT";
								break;
							case 0xF:
								result = "BESC";
								break;
						}
					}
					if (cond != 0x8)
					{
						// Branch in the reverse direction by negating the offset and subtracting 1.
						if (((opcode >> 5) & 0x1) != 0)
						{
							offset = (ushort)(-offset - 1);
						}
						result += $" ${offset:X4}";
						if (ext != 0)
						{
							result += $", ${opcode & 0x8:X1}";
						}
					}
					addrToAdvance = 2;
					return result;
				case 0x240:
				case 0x241:
				case 0x242:
				case 0x243:
				case 0x244:
				case 0x245:
				case 0x246:
				case 0x247:
					src = (byte)(opcode & 0x7);
					addr = ReadMemory((ushort)(pc + 1), true);
					addrToAdvance = 2;
					return $"MVO R{src:d}, ${addr:X4}";
				case 0x248:
				case 0x249:
				case 0x24A:
				case 0x24B:
				case 0x24C:
				case 0x24D:
				case 0x24E:
				case 0x24F:
				case 0x250:
				case 0x251:
				case 0x252:
				case 0x253:
				case 0x254:
				case 0x255:
				case 0x256:
				case 0x257:
				case 0x258:
				case 0x259:
				case 0x25A:
				case 0x25B:
				case 0x25C:
				case 0x25D:
				case 0x25E:
				case 0x25F:
				case 0x260:
				case 0x261:
				case 0x262:
				case 0x263:
				case 0x264:
				case 0x265:
				case 0x266:
				case 0x267:
				case 0x268:
				case 0x269:
				case 0x26A:
				case 0x26B:
				case 0x26C:
				case 0x26D:
				case 0x26E:
				case 0x26F:
				case 0x270:
				case 0x271:
				case 0x272:
				case 0x273:
				case 0x274:
				case 0x275:
				case 0x276:
				case 0x277:
				case 0x278:
				case 0x279:
				case 0x27A:
				case 0x27B:
				case 0x27C:
				case 0x27D:
				case 0x27E:
				case 0x27F:
					mem = (byte)((opcode >> 3) & 0x7);
					src = (byte)(opcode & 0x7);
					return "MVO@ R" + src + ", R" + mem;
				case 0x280:
				case 0x281:
				case 0x282:
				case 0x283:
				case 0x284:
				case 0x285:
				case 0x286:
				case 0x287:
					dest = (byte)(opcode & 0x7);
					addr = ReadMemory((ushort)(pc + 1), true);
					addrToAdvance = 2;
					return $"MVI R{dest:d}, ${addr:X4}";
				case 0x288:
				case 0x289:
				case 0x28A:
				case 0x28B:
				case 0x28C:
				case 0x28D:
				case 0x28E:
				case 0x28F:
				case 0x290:
				case 0x291:
				case 0x292:
				case 0x293:
				case 0x294:
				case 0x295:
				case 0x296:
				case 0x297:
				case 0x298:
				case 0x299:
				case 0x29A:
				case 0x29B:
				case 0x29C:
				case 0x29D:
				case 0x29E:
				case 0x29F:
				case 0x2A0:
				case 0x2A1:
				case 0x2A2:
				case 0x2A3:
				case 0x2A4:
				case 0x2A5:
				case 0x2A6:
				case 0x2A7:
				case 0x2A8:
				case 0x2A9:
				case 0x2AA:
				case 0x2AB:
				case 0x2AC:
				case 0x2AD:
				case 0x2AE:
				case 0x2AF:
				case 0x2B0:
				case 0x2B1:
				case 0x2B2:
				case 0x2B3:
				case 0x2B4:
				case 0x2B5:
				case 0x2B6:
				case 0x2B7:
				case 0x2B8:
				case 0x2B9:
				case 0x2BA:
				case 0x2BB:
				case 0x2BC:
				case 0x2BD:
				case 0x2BE:
				case 0x2BF:
					mem = (byte)((opcode >> 3) & 0x7);
					dest = (byte)(opcode & 0x7);
					return "MVI@ R" + mem + ", R" + dest;
				case 0x2C0:
				case 0x2C1:
				case 0x2C2:
				case 0x2C3:
				case 0x2C4:
				case 0x2C5:
				case 0x2C6:
				case 0x2C7:
					dest = (byte)(opcode & 0x7);
					addr = ReadMemory((ushort)(pc + 1), true);
					addrToAdvance = 2;
					return $"ADD R{dest:d}, ${addr:X4}";
				case 0x2C8:
				case 0x2C9:
				case 0x2CA:
				case 0x2CB:
				case 0x2CC:
				case 0x2CD:
				case 0x2CE:
				case 0x2CF:
				case 0x2D0:
				case 0x2D1:
				case 0x2D2:
				case 0x2D3:
				case 0x2D4:
				case 0x2D5:
				case 0x2D6:
				case 0x2D7:
				case 0x2D8:
				case 0x2D9:
				case 0x2DA:
				case 0x2DB:
				case 0x2DC:
				case 0x2DD:
				case 0x2DE:
				case 0x2DF:
				case 0x2E0:
				case 0x2E1:
				case 0x2E2:
				case 0x2E3:
				case 0x2E4:
				case 0x2E5:
				case 0x2E6:
				case 0x2E7:
				case 0x2E8:
				case 0x2E9:
				case 0x2EA:
				case 0x2EB:
				case 0x2EC:
				case 0x2ED:
				case 0x2EE:
				case 0x2EF:
				case 0x2F0:
				case 0x2F1:
				case 0x2F2:
				case 0x2F3:
				case 0x2F4:
				case 0x2F5:
				case 0x2F6:
				case 0x2F7:
				case 0x2F8:
				case 0x2F9:
				case 0x2FA:
				case 0x2FB:
				case 0x2FC:
				case 0x2FD:
				case 0x2FE:
				case 0x2FF:
					mem = (byte)((opcode >> 3) & 0x7);
					dest = (byte)(opcode & 0x7);
					return "ADD@ R" + mem + ", R" + dest;
				case 0x300:
				case 0x301:
				case 0x302:
				case 0x303:
				case 0x304:
				case 0x305:
				case 0x306:
				case 0x307:
					mem = (byte)(opcode & 0x7);
					addr = ReadMemory((ushort)(pc + 1), true);
					addrToAdvance = 2;
					return $"SUB R{mem:d}, ${addr:X4}";
				case 0x308:
				case 0x309:
				case 0x30A:
				case 0x30B:
				case 0x30C:
				case 0x30D:
				case 0x30E:
				case 0x30F:
				case 0x310:
				case 0x311:
				case 0x312:
				case 0x313:
				case 0x314:
				case 0x315:
				case 0x316:
				case 0x317:
				case 0x318:
				case 0x319:
				case 0x31A:
				case 0x31B:
				case 0x31C:
				case 0x31D:
				case 0x31E:
				case 0x31F:
				case 0x320:
				case 0x321:
				case 0x322:
				case 0x323:
				case 0x324:
				case 0x325:
				case 0x326:
				case 0x327:
				case 0x328:
				case 0x329:
				case 0x32A:
				case 0x32B:
				case 0x32C:
				case 0x32D:
				case 0x32E:
				case 0x32F:
				case 0x330:
				case 0x331:
				case 0x332:
				case 0x333:
				case 0x334:
				case 0x335:
				case 0x336:
				case 0x337:
				case 0x338:
				case 0x339:
				case 0x33A:
				case 0x33B:
				case 0x33C:
				case 0x33D:
				case 0x33E:
				case 0x33F:
					mem = (byte)((opcode >> 3) & 0x7);
					dest = (byte)(opcode & 0x7);
					return "SUB@ R" + mem + ", R" + dest;
				case 0x340:
				case 0x341:
				case 0x342:
				case 0x343:
				case 0x344:
				case 0x345:
				case 0x346:
				case 0x347:
					mem = (byte)(opcode & 0x7);
					addr = ReadMemory((ushort)(pc + 1), true);
					addrToAdvance = 2;
					return $"CMP R{mem:d}, ${addr:X4}";
				case 0x348:
				case 0x349:
				case 0x34A:
				case 0x34B:
				case 0x34C:
				case 0x34D:
				case 0x34E:
				case 0x34F:
				case 0x350:
				case 0x351:
				case 0x352:
				case 0x353:
				case 0x354:
				case 0x355:
				case 0x356:
				case 0x357:
				case 0x358:
				case 0x359:
				case 0x35A:
				case 0x35B:
				case 0x35C:
				case 0x35D:
				case 0x35E:
				case 0x35F:
				case 0x360:
				case 0x361:
				case 0x362:
				case 0x363:
				case 0x364:
				case 0x365:
				case 0x366:
				case 0x367:
				case 0x368:
				case 0x369:
				case 0x36A:
				case 0x36B:
				case 0x36C:
				case 0x36D:
				case 0x36E:
				case 0x36F:
				case 0x370:
				case 0x371:
				case 0x372:
				case 0x373:
				case 0x374:
				case 0x375:
				case 0x376:
				case 0x377:
				case 0x378:
				case 0x379:
				case 0x37A:
				case 0x37B:
				case 0x37C:
				case 0x37D:
				case 0x37E:
				case 0x37F:
					mem = (byte)((opcode >> 3) & 0x7);
					dest = (byte)(opcode & 0x7);
					return "CMP@ R" + mem + ", R" + dest;
				case 0x380:
				case 0x381:
				case 0x382:
				case 0x383:
				case 0x384:
				case 0x385:
				case 0x386:
				case 0x387:
					mem = (byte)(opcode & 0x7);
					addr = ReadMemory((ushort)(pc + 1), true);
					addrToAdvance = 2;
					return $"AND R{mem:d}, ${addr:X4}";
				case 0x388:
				case 0x389:
				case 0x38A:
				case 0x38B:
				case 0x38C:
				case 0x38D:
				case 0x38E:
				case 0x38F:
				case 0x390:
				case 0x391:
				case 0x392:
				case 0x393:
				case 0x394:
				case 0x395:
				case 0x396:
				case 0x397:
				case 0x398:
				case 0x399:
				case 0x39A:
				case 0x39B:
				case 0x39C:
				case 0x39D:
				case 0x39E:
				case 0x39F:
				case 0x3A0:
				case 0x3A1:
				case 0x3A2:
				case 0x3A3:
				case 0x3A4:
				case 0x3A5:
				case 0x3A6:
				case 0x3A7:
				case 0x3A8:
				case 0x3A9:
				case 0x3AA:
				case 0x3AB:
				case 0x3AC:
				case 0x3AD:
				case 0x3AE:
				case 0x3AF:
				case 0x3B0:
				case 0x3B1:
				case 0x3B2:
				case 0x3B3:
				case 0x3B4:
				case 0x3B5:
				case 0x3B6:
				case 0x3B7:
				case 0x3B8:
				case 0x3B9:
				case 0x3BA:
				case 0x3BB:
				case 0x3BC:
				case 0x3BD:
				case 0x3BE:
				case 0x3BF:
					mem = (byte)((opcode >> 3) & 0x7);
					dest = (byte)(opcode & 0x7);
					return "AND@ R" + mem + ", R" + dest;
				case 0x3C0:
				case 0x3C1:
				case 0x3C2:
				case 0x3C3:
				case 0x3C4:
				case 0x3C5:
				case 0x3C6:
				case 0x3C7:
					mem = (byte)(opcode & 0x7);
					addr = ReadMemory((ushort)(pc + 1), true);
					addrToAdvance = 2;
					return $"XOR R{mem:d}, ${addr:X4}";
				case 0x3C8:
				case 0x3C9:
				case 0x3CA:
				case 0x3CB:
				case 0x3CC:
				case 0x3CD:
				case 0x3CE:
				case 0x3CF:
				case 0x3D0:
				case 0x3D1:
				case 0x3D2:
				case 0x3D3:
				case 0x3D4:
				case 0x3D5:
				case 0x3D6:
				case 0x3D7:
				case 0x3D8:
				case 0x3D9:
				case 0x3DA:
				case 0x3DB:
				case 0x3DC:
				case 0x3DD:
				case 0x3DE:
				case 0x3DF:
				case 0x3E0:
				case 0x3E1:
				case 0x3E2:
				case 0x3E3:
				case 0x3E4:
				case 0x3E5:
				case 0x3E6:
				case 0x3E7:
				case 0x3E8:
				case 0x3E9:
				case 0x3EA:
				case 0x3EB:
				case 0x3EC:
				case 0x3ED:
				case 0x3EE:
				case 0x3EF:
				case 0x3F0:
				case 0x3F1:
				case 0x3F2:
				case 0x3F3:
				case 0x3F4:
				case 0x3F5:
				case 0x3F6:
				case 0x3F7:
				case 0x3F8:
				case 0x3F9:
				case 0x3FA:
				case 0x3FB:
				case 0x3FC:
				case 0x3FD:
				case 0x3FE:
				case 0x3FF:
					mem = (byte)((opcode >> 3) & 0x7);
					dest = (byte)(opcode & 0x7);
					return "XOR@ R" + mem + ", R" + dest;
			}
			return UNKNOWN;
		}
	}
}
