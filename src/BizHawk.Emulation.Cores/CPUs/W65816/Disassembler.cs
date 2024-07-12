using System.Collections.Generic;
using System.Text;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Components.W65816
{
	internal class W65816_DisassemblerService : IDisassemblable
	{
		public string Cpu { get; set; }

		private readonly W65816 disassemblerCpu = new W65816();
		
		public IEnumerable<string> AvailableCpus { get; } = [ "W65816" ];

		public string PCRegisterName => "PC";

		public string Disassemble(MemoryDomain m, uint addr, out int length)
		{
			byte P = 0; //TODO - user preferences somehow...
			return disassemblerCpu.Disassemble(addr, m.PeekByte, ref P, out length);
		}
	}

	/// <remarks>
	/// Ported from C-lang project https://github.com/pelrun/Dispel at <c>cb38eeee0</c> (specifically, the file <c>65816.c</c>).<br/>
	/// The DisPel software is unlicensed, and is thus assumed to be copyrighted without any transfer of rights.
	/// This reproduction is made with the assumption that it cannot be infringing because every part of its structure is necessary for its function (in the US, scènes à faire).
	/// </remarks>
	internal class W65816
	{
		//unsigned char *mem, unsigned long pos, unsigned char *flag, char *inst, unsigned char tsrc
		//TODO - what ha ppens at the end of memory? make sure peek wraps around?
		public string Disassemble(uint addr, Func<long, byte> peek, ref byte P, out int length)
		{
			byte opcode = peek(addr);
			string ibuf = null;
			string pbuf = null;
			int offset = -1, sval = -1;

			bool tsrc_2 = false;

			switch (opcode)
			{
				case 0x69:case 0x6D:case 0x6F:case 0x65:case 0x72:case 0x67:case 0x7D:case 0x7F:case 0x79:case 0x75:case 0x61:case 0x71:case 0x77:case 0x63:case 0x73:
					ibuf = "adc"; break;
				case 0x29:case 0x2D:case 0x2F:case 0x25:case 0x32:case 0x27:case 0x3D:case 0x3F:case 0x39:case 0x35:case 0x21:case 0x31:case 0x37:case 0x23:case 0x33:
					ibuf = "and"; break;
				case 0x0A:case 0x0E:case 0x06:case 0x1E:case 0x16:
					ibuf = "asl"; break;
				case 0x90:
					ibuf = "bcc"; break;
				case 0xB0: 
					ibuf = "bcs"; break;
				case 0xF0:
					ibuf = "beq"; break;
				case 0xD0:
					ibuf = "bne"; break;
				case 0x30:
					ibuf = "bmi"; break;
				case 0x10:
					ibuf = "bpl"; break;
				case 0x50:
					ibuf = "bvc"; break;
				case 0x70:
					ibuf = "bvs"; break;
				case 0x80:
					ibuf = "bra"; break;
				case 0x82:
					ibuf = "brl"; break;
				case 0x89:case 0x2C:case 0x24:case 0x3C:case 0x34:
					ibuf = "bit"; break;
				case 0x00: 
					ibuf = "brk"; break;
				case 0x18:
					ibuf = "clc"; break;
				case 0xD8:
					ibuf = "cld"; break;
				case 0x58:
					ibuf = "cli"; break;
				case 0xB8:
					ibuf = "clv"; break;
				case 0x38:
					ibuf = "sec"; break;
				case 0xF8:
					ibuf = "sed"; break;
				case 0x78:
					ibuf = "sei"; break;
				case 0xC9:case 0xCD:case 0xCF:case 0xC5:case 0xD2:case 0xC7:case 0xDD:case 0xDF:case 0xD9:case 0xD5:case 0xC1:case 0xD1:case 0xD7:case 0xC3:case 0xD3:
					ibuf = "cmp"; break;
				case 0x02:
					ibuf = "cop"; break;
				case 0xE0:case 0xEC:case 0xE4:
					ibuf = "cpx"; break;
				case 0xC0:case 0xCC:case 0xC4:
					ibuf = "cpy"; break;
				case 0x3A:case 0xCE:case 0xC6:case 0xDE:case 0xD6:
					ibuf = "dec"; break;
				case 0xCA: 
					ibuf = "dex"; break;
				case 0x88:
					ibuf = "dey"; break;
				case 0x49:case 0x4D:case 0x4F:case 0x45:case 0x52:case 0x47:case 0x5D:case 0x5F:case 0x59:case 0x55:case 0x41:case 0x51:case 0x57:case 0x43:case 0x53:
					ibuf = "eor"; break;
				case 0x1A:case 0xEE:case 0xE6:case 0xFE:case 0xF6:
					ibuf = "inc"; break;
				case 0xE8:
					ibuf = "inx"; break;
				case 0xC8:
					ibuf = "iny"; break;
				case 0x4C:case 0x6C:case 0x7C:case 0x5C:case 0xDC:
					ibuf = "jmp"; break;
				case 0x22:case 0x20:case 0xFC:
					ibuf = "jsr"; break;
				case 0xA9:case 0xAD:case 0xAF:case 0xA5:case 0xB2:case 0xA7:case 0xBD:case 0xBF:case 0xB9:case 0xB5:case 0xA1:case 0xB1:case 0xB7:case 0xA3:case 0xB3:
					ibuf = "lda"; break;
				case 0xA2:case 0xAE:case 0xA6:case 0xBE:case 0xB6:
					ibuf = "ldx";break;
				case 0xA0:case 0xAC:case 0xA4:case 0xBC:case 0xB4:
					ibuf = "ldy"; break;
				case 0x4A:case 0x4E:case 0x46:case 0x5E:case 0x56:
					ibuf = "lsr"; break;
				case 0x54:
					ibuf = "mvn"; break;
				case 0x44:
					ibuf = "mvp"; break;
				case 0xEA:
					ibuf = "nop"; break;
				case 0x09:case 0x0D:case 0x0F:case 0x05:case 0x12:case 0x07:case 0x1D:case 0x1F:case 0x19:case 0x15:case 0x01:case 0x11:case 0x17:case 0x03:case 0x13:
					ibuf = "ora"; break;
				case 0xF4:
					ibuf = "pea"; break;
				case 0xD4:
					ibuf = "pei"; break;
				case 0x62:
					ibuf = "per"; break;
				case 0x48:
					ibuf = "pha"; break;
				case 0x08:
					ibuf = "php"; break;
				case 0xDA:
					ibuf = "phx"; break;
				case 0x5A:
					ibuf = "phy"; break;
				case 0x68:
					ibuf = "pla"; break;
				case 0x28:
					ibuf = "plp"; break;
				case 0xFA:
					ibuf = "plx"; break;
				case 0x7A:
					ibuf = "ply"; break;
				case 0x8B:
					ibuf = "phb"; break;
				case 0x0B:
					ibuf = "phd"; break;
				case 0x4B:
					ibuf = "phk"; break;
				case 0xAB:
					ibuf = "plb"; break;
				case 0x2B:
					ibuf = "pld"; break;
				case 0xC2:
					ibuf = "rep"; break;
				case 0x2A:case 0x2E:case 0x26:case 0x3E:case 0x36:
					ibuf = "rol"; break;
				case 0x6A:case 0x6E:case 0x66:case 0x7E:case 0x76:
					ibuf = "ror"; break;
				case 0x40:
					ibuf = "rti";
					if (tsrc_2)
						ibuf += "\n";
					break;
				case 0x6B:
					ibuf = "rtl";
					if (tsrc_2)
						ibuf += "\n";
					break;
				case 0x60:
					ibuf = "rts";
					if (tsrc_2)
						ibuf += "\n";
					break;
				case 0xE9:case 0xED:case 0xEF:case 0xE5:case 0xF2:case 0xE7:case 0xFD:case 0xFF:case 0xF9:case 0xF5:case 0xE1:case 0xF1:case 0xF7:case 0xE3:case 0xF3:
					ibuf = "sbc"; break;
				case 0xE2:
					ibuf = "sep"; break;
				case 0x8D:case 0x8F:case 0x85:case 0x92:case 0x87:case 0x9D:case 0x9F:case 0x99:case 0x95:case 0x81:case 0x91:case 0x97:case 0x83:case 0x93:
					ibuf = "sta"; break;
				case 0xDB:
					ibuf = "stp"; break;
				case 0x8E:case 0x86:case 0x96:
					ibuf = "stx"; break;
				case 0x8C:case 0x84:case 0x94:
					ibuf = "sty"; break;
				case 0x9C:case 0x64:case 0x9E:case 0x74:
					ibuf = "stz";break;
				case 0xAA:
					ibuf = "tax"; break;
				case 0xA8:
					ibuf = "tay"; break;
				case 0x8A:
					ibuf = "txa"; break;
				case 0x98:
					ibuf = "tya"; break;
				case 0xBA:
					ibuf = "tsx"; break;
				case 0x9A:
					ibuf = "txs"; break;
				case 0x9B:
					ibuf = "txy"; break;
				case 0xBB:
					ibuf = "tyx"; break;
				case 0x5B:
					ibuf = "tcd"; break;
				case 0x7B:
					ibuf = "tdc"; break;
				case 0x1B:
					ibuf = "tcs"; break;
				case 0x3B:
					ibuf = "tsc"; break;
				case 0x1C:case 0x14:
					ibuf = "trb"; break;
				case 0x0C:case 0x04:
					ibuf = "tsb"; break;
				case 0xCB:
					ibuf = "wai";break;
				case 0x42:
					ibuf = "wdm";break;
				case 0xEB:
					ibuf = "xba";break;
				case 0xFB:
					ibuf = "xce";break;
			}

			// Parse out parameter list
			switch (opcode)
			{
				// Absolute
				case 0x0C:case 0x0D:case 0x0E:case 0x1C:case 0x20:case 0x2C:case 0x2D:case 0x2E:case 0x4C:case 0x4D:case 0x4E:case 0x6D:case 0x6E:case 0x8C:case 0x8D:case 0x8E:case 0x9C:case 0xAC:case 0xAD:case 0xAE:case 0xCC:case 0xCD:case 0xCE:case 0xEC:case 0xED:case 0xEE:
					pbuf = $"${peek(addr + 1) + peek(addr + 2) * 256:X4}";
					//sprintf(pbuf, "$%04X", mem[1] + mem[2] * 256);
					offset = 3;
					break;
				// Absolute Indexed Indirect
				case 0x7C:case 0xFC:
					pbuf = $"(${peek(addr + 1) + peek(addr + 2) * 256:X4},X";
					//sprintf(pbuf, "($%04X,X)", mem[1] + mem[2] * 256);
					offset = 3;
					break;
				// Absolute Indexed, X
				case 0x1D:case 0x1E:case 0x3C:case 0x3D:case 0x3E:case 0x5D:case 0x5E:case 0x7D:case 0x7E:case 0x9D:case 0x9E:case 0xBC:case 0xBD:case 0xDD:case 0xDE:case 0xFD:case 0xFE:
					pbuf = $"${peek(addr + 1) + peek(addr + 2) * 256:X4},X";
					//sprintf(pbuf, "$%04X,X", mem[1] + mem[2] * 256);
					offset = 3;
					break;
				// Absolute Indexed, Y
				case 0x19:case 0x39:case 0x59:case 0x79:case 0x99:case 0xB9:case 0xBE:case 0xD9:case 0xF9:
					pbuf = $"${peek(addr + 1) + peek(addr + 2) * 256:X4},Y";
					//sprintf(pbuf, "$%04X,Y", mem[1] + mem[2] * 256);
					offset = 3;
					break;
				// Absolute Indirect
				case 0x6C:
					pbuf = $"(${peek(addr + 1) + peek(addr + 2) * 256:X4})";
					//sprintf(pbuf, "($%04X)", mem[1] + mem[2] * 256);
					offset = 3;
					break;
				// Absolute Indirect Long
				case 0xDC:
					pbuf = $"[${peek(addr + 1) + peek(addr + 2) * 256:X4}]";
					//sprintf(pbuf, "[$%04X]", mem[1] + mem[2] * 256);
					offset = 3;
					break;
				// Absolute Long
				case 0x0F:case 0x22:case 0x2F:case 0x4F:case 0x5C:case 0x6F:case 0x8F:case 0xAF:case 0xCF:case 0xEF:
					pbuf = $"${peek(addr + 1) + peek(addr + 2) * 256 + peek(addr + 3) * 65536:X6}";
					//sprintf(pbuf, "$%06X", mem[1] + mem[2] * 256 + mem[3] * 65536);
					offset = 4;
					break;
				// Absolute Long Indexed, X
				case 0x1F:case 0x3F:case 0x5F:case 0x7F:case 0x9F:case 0xBF:case 0xDF:case 0xFF:
					pbuf = $"${peek(addr + 1) + peek(addr + 2) * 256 + peek(addr + 3) * 65536:X6},X";
					//sprintf(pbuf, "$%06X,X", mem[1] + mem[2] * 256 + mem[3] * 65536);
					offset = 4;
					break;
				// Accumulator
				case 0x0A:case 0x1A:case 0x2A:case 0x3A:case 0x4A:case 0x6A:
					pbuf = "A";
					offset = 1;
					break;
				// Block Move
				case 0x44: case 0x54:
					pbuf = $"${peek(addr + 1):X2},${peek(addr + 2):X2}";
					//sprintf(pbuf, "$%02X,$%02X", mem[1], mem[2]);
					offset = 3;
					break;
				// Direct Page
				case 0x04:case 0x05:case 0x06:case 0x14:case 0x24:case 0x25:case 0x26:case 0x45:case 0x46:case 0x64:case 0x65:case 0x66:case 0x84:case 0x85:case 0x86:case 0xA4:case 0xA5:case 0xA6:case 0xC4:case 0xC5:case 0xC6:case 0xE4:case 0xE5:case 0xE6:
					pbuf = $"${peek(addr + 1):X2}";
					//sprintf(pbuf, "$%02X", mem[1]);
					offset = 2;
					break;
				// Direct Page Indexed, X
				case 0x15:case 0x16:case 0x34:case 0x35:case 0x36:case 0x55:case 0x56:case 0x74:case 0x75:case 0x76:case 0x94:case 0x95:case 0xB4:case 0xB5:case 0xD5:case 0xD6:case 0xF5:case 0xF6:
					pbuf = $"${peek(addr + 1):X2},X";
					//sprintf(pbuf, "$%02X,X", mem[1]);
					offset = 2;
					break;
				// Direct Page Indexed, Y
				case 0x96:case 0xB6:
					pbuf = $"${peek(addr + 1):X2},Y";
					//sprintf(pbuf, "$%02X,Y", mem[1]);
					offset = 2;
					break;
				// Direct Page Indirect
				case 0x12:case 0x32:case 0x52:case 0x72:case 0x92:case 0xB2:case 0xD2:case 0xF2:
					pbuf = $"(${peek(addr + 1):X2})";
					//sprintf(pbuf, "($%02X)", mem[1]);
					offset = 2;
					break;
				// Direct Page Indirect Long
				case 0x07:case 0x27:case 0x47:case 0x67:case 0x87:case 0xA7:case 0xC7:case 0xE7:
					pbuf = $"[${peek(addr + 1):X2}]";
					//sprintf(pbuf, "[$%02X]", mem[1]);
					offset = 2;
					break;
				// Direct Page Indexed Indirect, X
				case 0x01:case 0x21:case 0x41:case 0x61:case 0x81:case 0xA1:case 0xC1:case 0xE1:
					pbuf = $"(${peek(addr + 1):X2},X)";
					//sprintf(pbuf, "($%02X,X)", mem[1]);
					offset = 2;
					break;
				// Direct Page Indirect Indexed, Y
				case 0x11:case 0x31:case 0x51:case 0x71:case 0x91:case 0xB1:case 0xD1:case 0xF1:
					pbuf = $"(${peek(addr + 1):X2},Y)";
					//sprintf(pbuf, "($%02X),Y", mem[1]);
					offset = 2;
					break;
				// Direct Page Indirect Long Indexed, Y
				case 0x17:case 0x37:case 0x57:case 0x77:case 0x97:case 0xB7:case 0xD7:case 0xF7:
					pbuf = $"[${peek(addr + 1):X2}],Y";
					//sprintf(pbuf, "[$%02X],Y", mem[1]);
					offset = 2;
					break;
				// Stack (Pull)
				case 0x28:case 0x2B:case 0x68:case 0x7A:case 0xAB:case 0xFA:
				// Stack (Push)
				case 0x08:case 0x0B:case 0x48:case 0x4B:case 0x5A:case 0x8B:case 0xDA:
				// Stack (RTL)
				case 0x6B:
				// Stack (RTS)
				case 0x60:
				// Stack/RTI
				case 0x40:
				// Implied
				case 0x18:case 0x1B:case 0x38:case 0x3B:case 0x58:case 0x5B:case 0x78:case 0x7B:case 0x88:case 0x8A:case 0x98:case 0x9A:case 0x9B:case 0xA8:case 0xAA:case 0xB8:case 0xBA:case 0xBB:case 0xC8:case 0xCA:case 0xCB:case 0xD8:case 0xDB:case 0xE8:case 0xEA:case 0xEB:case 0xF8:case 0xFB:
					pbuf = "";
					offset = 1;
					break;
				// Program Counter Relative
				case 0x10:case 0x30:case 0x50:case 0x70:case 0x80:case 0x90:case 0xB0:case 0xD0:case 0xF0:
					// Calculate the signed value of the param
					{
						byte mem1 = peek(addr+1);
						sval = (mem1 > 127) ? (mem1 - 256) : mem1;
						pbuf = $"${(addr + sval + 2) & 0xFFFF:X4}";
						//sprintf(pbuf, "$%04lX", (pos + sval + 2) & 0xFFFF);
						offset = 2;
						break;
					}
				// Stack (Program Counter Relative Long)
				case 0x62:
				// Program Counter Relative Long
				case 0x82:
					// Calculate the signed value of the param
					sval = peek(addr+1) + peek(addr+2) * 256;
					sval = (sval > 32767) ? (sval - 65536) : sval;
					pbuf = $"${(addr + sval + 3) & 0xFFFF:X4}";
					//sprintf(pbuf, "$%04lX", (pos + sval + 3) & 0xFFFF);
					offset = 3;
					break;
				// Stack Relative Indirect Indexed, Y
				case 0x13:case 0x33:case 0x53:case 0x73:case 0x93:case 0xB3:case 0xD3:case 0xF3:
					pbuf = $"(${peek(addr + 1):X4},S),Y";
					//sprintf(pbuf, "($%02X,S),Y", mem[1]);
					offset = 2;
					break;
				// Stack (Absolute)
				case 0xF4:
					pbuf = $"${peek(addr + 1) + peek(addr + 2) * 256:X4}";
					//sprintf(pbuf, "$%04X", mem[1] + mem[2] * 256);
					offset = 3;
					break;
				// Stack (Direct Page Indirect)
				case 0xD4:
					pbuf = $"(${peek(addr + 1):X2}";
					//sprintf(pbuf, "($%02X)", mem[1]);
					offset = 2;
					break;
				// Stack Relative
				case 0x03:case 0x23:case 0x43:case 0x63:case 0x83:case 0xA3:case 0xC3:case 0xE3:
					pbuf = $"${peek(addr = 1):X2},S";
					//sprintf(pbuf, "$%02X,S", mem[1]);
					offset = 2;
					break;
				// WDM mode
				case 0x42:
				// Stack/Interrupt
				case 0x00: case 0x02:
					pbuf = $"${peek(addr + 1):X2}";
					//sprintf(pbuf, "$%02X", mem[1]);
					offset = 2;
					break;
				// Immediate (Invariant)
				case 0xC2:
					// REP following
					{
						byte mem1 = peek(addr + 1);
						P = (byte)(P & ~mem1);
						pbuf = $"#${peek(addr + 1):X2}";
						//sprintf(pbuf, "#$%02X", mem[1]);
						offset = 2;
						break;
					}
				case 0xE2:
					// SEP following
					{
						byte mem1 = peek(addr + 1);
						P = (byte)(P | mem1);
						pbuf = $"#${mem1:X2}";
						//sprintf(pbuf, "#$%02X", mem[1]);
						offset = 2;
						break;
					}
				// Immediate (A size dependent)
				case 0x09:case 0x29:case 0x49:case 0x69:case 0x89:case 0xA9:case 0xC9:case 0xE9:
					if ((P & 0x20)!=0)
					{
						pbuf = $"#${peek(addr + 1):X2}";
						//sprintf(pbuf, "#$%02X", mem[1]);
						offset = 2;
					}
					else
					{
						pbuf = $"#${peek(addr + 1) + peek(addr + 2) * 256:X4}";
						offset = 3;
					}
					break;
				// Immediate (X/Y size dependent)
				case 0xA0:case 0xA2:case 0xC0:case 0xE0:
					if ((P & 0x10)!=0)
					{
						pbuf = $"#${peek(addr + 1):X2}";
						//sprintf(pbuf, "#$%02X", mem[1]);
						offset = 2;
					}
					else
					{
						pbuf = $"#${peek(addr + 1) + peek(addr + 2) * 256:X4}";
						//sprintf(pbuf, "#$%04X", mem[1] + mem[2] * 256);
						offset = 3;
					}
					break;
			}

			StringBuilder sb = new StringBuilder();

			bool print_addr = false;
			bool print_hex = false;

			if (print_addr)
			{
				sb.AppendFormat("{0:X2}/{1:X4}: ", (addr >> 16) & 0xFF, addr & 0xFFFF);
			}

			if (print_hex)
			{
				for (uint i = 0; i < offset; i++)
				{
					sb.AppendFormat("{0:X2} ", peek(addr + i));
				}
				for (int i = offset; i < 3; i++)
					sb.AppendFormat("   ");
			}

			sb.AppendFormat("{0} {1}", ibuf, pbuf);

			length = offset;
			return sb.ToString();
		}
	}
}
