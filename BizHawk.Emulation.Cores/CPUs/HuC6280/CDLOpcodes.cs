using System;

// Do not modify this file directly! This is GENERATED code.
// Please open the CpuCoreGenerator solution and make your modifications there.

namespace BizHawk.Emulation.Cores.Components.H6280
{
	public partial class HuC6280
	{
		void CDLOpcode()
		{
			byte tmp8;
			byte opcode = ReadMemory(PC);
			switch (opcode)
			{
				case 0x00: // BRK
					MarkCode(PC, 1);
					break;
				case 0x01: // ORA (addr,X)
					MarkCode(PC, 2);
					tmp8 = (byte)(ReadMemory((ushort)(PC + 1)) + X);
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8));
					break;
				case 0x02: // SXY
					MarkCode(PC, 1);
					break;
				case 0x03: // ST0 #nn
					MarkCode(PC, 2);
					break;
				case 0x04: // TSB zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x05: // ORA zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x06: // ASL zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x07: // RMB0 zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x08: // PHP
					MarkCode(PC, 1);
					MarkPush(1);
					break;
				case 0x09: // ORA #nn
					MarkCode(PC, 2);
					break;
				case 0x0A: // ASL A
					MarkCode(PC, 1);
					break;
				case 0x0B: // ??
					MarkCode(PC, 1);
					break;
				case 0x0C: // TSB addr
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x0D: // ORA addr
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x0E: // ASL addr
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x0F: // BBR0
					MarkCode(PC, 3);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x10: // BPL +/-rel
					MarkCode(PC, 2);
					break;
				case 0x11: // ORA (addr),Y
					MarkCode(PC, 2);
					tmp8 = ReadMemory((ushort)(PC + 1));
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8) + Y);
					break;
				case 0x12: // ORA (addr)
					MarkCode(PC, 2);
					tmp8 = ReadMemory((ushort)(PC + 1));
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8));
					break;
				case 0x13: // ST1 #nn
					MarkCode(PC, 2);
					break;
				case 0x14: // TRB zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x15: // ORA zp,X
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0x16: // ASL zp,X
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0x17: // RMB1 zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x18: // CLC
					MarkCode(PC, 1);
					break;
				case 0x19: // ORA addr,Y
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)) + Y);
					break;
				case 0x1A: // INC A
					MarkCode(PC, 1);
					break;
				case 0x1B: // ??
					MarkCode(PC, 1);
					break;
				case 0x1C: // TRB addr
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x1D: // ORA addr,X
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0x1E: // ASL addr,X
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0x1F: // BBR1
					MarkCode(PC, 3);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x20: // JSR addr
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x21: // AND (addr,X)
					MarkCode(PC, 2);
					tmp8 = (byte)(ReadMemory((ushort)(PC + 1)) + X);
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8));
					break;
				case 0x22: // SAX
					MarkCode(PC, 1);
					break;
				case 0x23: // ST2 #nn
					MarkCode(PC, 2);
					break;
				case 0x24: // BIT zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x25: // AND zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x26: // ROL zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x27: // RMB2 zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x28: // PLP
					MarkCode(PC, 1);
					MarkPop(1);
					break;
				case 0x29: // AND #nn
					MarkCode(PC, 2);
					break;
				case 0x2A: // ROL A
					MarkCode(PC, 1);
					break;
				case 0x2B: // ??
					MarkCode(PC, 1);
					break;
				case 0x2C: // BIT addr
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x2D: // AND addr
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x2E: // ROL addr
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x2F: // BBR2
					MarkCode(PC, 3);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x30: // BMI +/-rel
					MarkCode(PC, 2);
					break;
				case 0x31: // AND (addr),Y
					MarkCode(PC, 2);
					tmp8 = ReadMemory((ushort)(PC + 1));
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8) + Y);
					break;
				case 0x32: // AND (addr)
					MarkCode(PC, 2);
					tmp8 = ReadMemory((ushort)(PC + 1));
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8));
					break;
				case 0x33: // ??
					MarkCode(PC, 1);
					break;
				case 0x34: // BIT zp,X
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0x35: // AND zp,X
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0x36: // ROL zp,X
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0x37: // RMB3 zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x38: // SEC
					MarkCode(PC, 1);
					break;
				case 0x39: // AND addr,Y
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)) + Y);
					break;
				case 0x3A: // DEC A
					MarkCode(PC, 1);
					break;
				case 0x3B: // ??
					MarkCode(PC, 1);
					break;
				case 0x3C: // BIT addr,X
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0x3D: // AND addr,X
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0x3E: // ROL addr,X
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0x3F: // BBR3
					MarkCode(PC, 3);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x40: // RTI
					MarkCode(PC, 1);
					MarkPop(3);
					break;
				case 0x41: // EOR (addr,X)
					MarkCode(PC, 2);
					tmp8 = (byte)(ReadMemory((ushort)(PC + 1)) + X);
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8));
					break;
				case 0x42: // SAY
					MarkCode(PC, 1);
					break;
				case 0x43: // TMA #nn
					MarkCode(PC, 2);
					break;
				case 0x44: // BSR +/-rel
					MarkCode(PC, 2);
					break;
				case 0x45: // EOR zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x46: // LSR zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x47: // RMB4 zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x48: // PHA
					MarkCode(PC, 1);
					MarkPush(1);
					break;
				case 0x49: // EOR #nn
					MarkCode(PC, 2);
					break;
				case 0x4A: // LSR A
					MarkCode(PC, 1);
					break;
				case 0x4B: // ??
					MarkCode(PC, 1);
					break;
				case 0x4C: // JMP addr
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x4D: // EOR addr
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x4E: // LSR addr
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x4F: // BBR4
					MarkCode(PC, 3);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x50: // BVC +/-rel
					MarkCode(PC, 2);
					break;
				case 0x51: // EOR (addr),Y
					MarkCode(PC, 2);
					tmp8 = ReadMemory((ushort)(PC + 1));
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8) + Y);
					break;
				case 0x52: // EOR (addr)
					MarkCode(PC, 2);
					tmp8 = ReadMemory((ushort)(PC + 1));
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8));
					break;
				case 0x53: // TAM #nn
					MarkCode(PC, 2);
					break;
				case 0x54: // CSL
					MarkCode(PC, 1);
					break;
				case 0x55: // EOR zp,X
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0x56: // LSR zp,X
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0x57: // RMB5 zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x58: // CLI
					MarkCode(PC, 1);
					break;
				case 0x59: // EOR addr,Y
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)) + Y);
					break;
				case 0x5A: // PHY
					MarkCode(PC, 1);
					MarkPush(1);
					break;
				case 0x5B: // ??
					MarkCode(PC, 1);
					break;
				case 0x5C: // ??
					MarkCode(PC, 1);
					break;
				case 0x5D: // EOR addr,X
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0x5E: // LSR addr,X
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0x5F: // BBR5
					MarkCode(PC, 3);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x60: // RTS
					MarkCode(PC, 1);
					MarkPop(2);
					break;
				case 0x61: // ADC (addr,X)
					MarkCode(PC, 2);
					tmp8 = (byte)(ReadMemory((ushort)(PC + 1)) + X);
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8));
					break;
				case 0x62: // CLA
					MarkCode(PC, 1);
					break;
				case 0x63: // ??
					MarkCode(PC, 1);
					break;
				case 0x64: // STZ zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x65: // ADC zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x66: // ROR zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x67: // RMB6 zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x68: // PLA
					MarkCode(PC, 1);
					MarkPop(1);
					break;
				case 0x69: // ADC #nn
					MarkCode(PC, 2);
					break;
				case 0x6A: // ROR A
					MarkCode(PC, 1);
					break;
				case 0x6B: // ??
					MarkCode(PC, 1);
					break;
				case 0x6C: // JMP
					MarkCode(PC, 3);
					MarkFptr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x6D: // ADC addr
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x6E: // ROR addr
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x6F: // BBR6
					MarkCode(PC, 3);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x70: // BVS +/-rel
					MarkCode(PC, 2);
					break;
				case 0x71: // ADC (addr),Y
					MarkCode(PC, 2);
					tmp8 = ReadMemory((ushort)(PC + 1));
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8) + Y);
					break;
				case 0x72: // ADC (addr)
					MarkCode(PC, 2);
					tmp8 = ReadMemory((ushort)(PC + 1));
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8));
					break;
				case 0x73: // TII src, dest, len
					MarkCode(PC, 7);
					if (!InBlockTransfer)
					{
						MarkBTFrom(ReadWord((ushort)(PC + 1)));
						MarkBTTo(ReadWord((ushort)(PC + 3)));
					}
					else
					{
						MarkBTFrom(btFrom);
						MarkBTTo(btTo);
						}
					break;
				case 0x74: // STZ zp,X
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0x75: // ADC zp,X
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0x76: // ROR zp,X
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0x77: // RMB7 zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x78: // SEI
					MarkCode(PC, 1);
					break;
				case 0x79: // ADC addr,Y
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)) + Y);
					break;
				case 0x7A: // PLY
					MarkCode(PC, 1);
					MarkPop(1);
					break;
				case 0x7B: // ??
					MarkCode(PC, 1);
					break;
				case 0x7C: // JMP
					MarkCode(PC, 3);
					MarkFptr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0x7D: // ADC addr,X
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0x7E: // ROR addr,X
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0x7F: // BBR7
					MarkCode(PC, 3);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x80: // BRA +/-rel
					MarkCode(PC, 2);
					break;
				case 0x81: // STA (addr,X)
					MarkCode(PC, 2);
					tmp8 = (byte)(ReadMemory((ushort)(PC + 1)) + X);
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8));
					break;
				case 0x82: // CLX
					MarkCode(PC, 1);
					break;
				case 0x83: // TST
					MarkCode(PC, 3);
					MarkZP(ReadMemory((ushort)(PC + 2)));
					break;
				case 0x84: // STY zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x85: // STA zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x86: // STX zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x87: // SMB0 zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x88: // DEY
					MarkCode(PC, 1);
					break;
				case 0x89: // BIT #nn
					MarkCode(PC, 2);
					break;
				case 0x8A: // TXA
					MarkCode(PC, 1);
					break;
				case 0x8B: // ??
					MarkCode(PC, 1);
					break;
				case 0x8C: // STY addr
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x8D: // STA addr
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x8E: // STX addr
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x8F: // BBS0
					MarkCode(PC, 3);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x90: // BCC +/-rel
					MarkCode(PC, 2);
					break;
				case 0x91: // STA (addr),Y
					MarkCode(PC, 2);
					tmp8 = ReadMemory((ushort)(PC + 1));
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8) + Y);
					break;
				case 0x92: // STA (addr)
					MarkCode(PC, 2);
					tmp8 = ReadMemory((ushort)(PC + 1));
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8));
					break;
				case 0x93: // TST
					MarkCode(PC, 4);
					MarkAddr(ReadWord((ushort)(PC + 2)));
					break;
				case 0x94: // STY zp,X
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0x95: // STA zp,X
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0x96: // STX zp,Y
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)) + Y);
					break;
				case 0x97: // SMB1 zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x98: // TYA
					MarkCode(PC, 1);
					break;
				case 0x99: // STA addr,Y
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)) + Y);
					break;
				case 0x9A: // TXS
					MarkCode(PC, 1);
					break;
				case 0x9B: // ??
					MarkCode(PC, 1);
					break;
				case 0x9C: // STZ addr
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x9D: // STA addr,X
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0x9E: // STZ addr,X
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0x9F: // BBS1
					MarkCode(PC, 3);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xA0: // LDY #nn
					MarkCode(PC, 2);
					break;
				case 0xA1: // LDA (addr,X)
					MarkCode(PC, 2);
					tmp8 = (byte)(ReadMemory((ushort)(PC + 1)) + X);
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8));
					break;
				case 0xA2: // LDX #nn
					MarkCode(PC, 2);
					break;
				case 0xA3: // TST
					MarkCode(PC, 3);
					MarkZP(ReadMemory((ushort)(PC + 2)) + X);
					break;
				case 0xA4: // LDY zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xA5: // LDA zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xA6: // LDX zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xA7: // SMB2 zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xA8: // TAY
					MarkCode(PC, 1);
					break;
				case 0xA9: // LDA #nn
					MarkCode(PC, 2);
					break;
				case 0xAA: // TAX
					MarkCode(PC, 1);
					break;
				case 0xAB: // ??
					MarkCode(PC, 1);
					break;
				case 0xAC: // LDY addr
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0xAD: // LDA addr
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0xAE: // LDX addr
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0xAF: // BBS2
					MarkCode(PC, 3);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xB0: // BCS +/-rel
					MarkCode(PC, 2);
					break;
				case 0xB1: // LDA (addr),Y
					MarkCode(PC, 2);
					tmp8 = ReadMemory((ushort)(PC + 1));
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8) + Y);
					break;
				case 0xB2: // LDA (addr)
					MarkCode(PC, 2);
					tmp8 = ReadMemory((ushort)(PC + 1));
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8));
					break;
				case 0xB3: // TST
					MarkCode(PC, 4);
					MarkAddr(ReadWord((ushort)(PC + 2)) + X);
					break;
				case 0xB4: // LDY zp,X
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0xB5: // LDA zp,X
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0xB6: // LDX zp,Y
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)) + Y);
					break;
				case 0xB7: // SMB3 zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xB8: // CLV
					MarkCode(PC, 1);
					break;
				case 0xB9: // LDA addr,Y
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)) + Y);
					break;
				case 0xBA: // TSX
					MarkCode(PC, 1);
					break;
				case 0xBB: // ??
					MarkCode(PC, 1);
					break;
				case 0xBC: // LDY addr,X
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0xBD: // LDA addr,X
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0xBE: // LDX addr,Y
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)) + Y);
					break;
				case 0xBF: // BBS3
					MarkCode(PC, 3);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xC0: // CPY #nn
					MarkCode(PC, 2);
					break;
				case 0xC1: // CMP (addr,X)
					MarkCode(PC, 2);
					tmp8 = (byte)(ReadMemory((ushort)(PC + 1)) + X);
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8));
					break;
				case 0xC2: // CLY
					MarkCode(PC, 1);
					break;
				case 0xC3: // TDD src, dest, len
					MarkCode(PC, 7);
					if (!InBlockTransfer)
					{
						MarkBTFrom(ReadWord((ushort)(PC + 1)));
						MarkBTTo(ReadWord((ushort)(PC + 3)));
					}
					else
					{
						MarkBTFrom(btFrom);
						MarkBTTo(btTo);
						}
					break;
				case 0xC4: // CPY zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xC5: // CMP zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xC6: // DEC zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xC7: // SMB4 zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xC8: // INY
					MarkCode(PC, 1);
					break;
				case 0xC9: // CMP #nn
					MarkCode(PC, 2);
					break;
				case 0xCA: // DEX
					MarkCode(PC, 1);
					break;
				case 0xCB: // ??
					MarkCode(PC, 1);
					break;
				case 0xCC: // CPY addr
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0xCD: // CMP addr
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0xCE: // DEC addr
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0xCF: // BBS4
					MarkCode(PC, 3);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xD0: // BNE +/-rel
					MarkCode(PC, 2);
					break;
				case 0xD1: // CMP (addr),Y
					MarkCode(PC, 2);
					tmp8 = ReadMemory((ushort)(PC + 1));
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8) + Y);
					break;
				case 0xD2: // CMP (addr)
					MarkCode(PC, 2);
					tmp8 = ReadMemory((ushort)(PC + 1));
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8));
					break;
				case 0xD3: // TIN src, dest, len
					MarkCode(PC, 7);
					if (!InBlockTransfer)
					{
						MarkBTFrom(ReadWord((ushort)(PC + 1)));
						MarkBTTo(ReadWord((ushort)(PC + 3)));
					}
					else
					{
						MarkBTFrom(btFrom);
						MarkBTTo(btTo);
						}
					break;
				case 0xD4: // CSH
					MarkCode(PC, 1);
					break;
				case 0xD5: // CMP zp,X
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0xD6: // DEC zp,X
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0xD7: // SMB5 zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xD8: // CLD
					MarkCode(PC, 1);
					break;
				case 0xD9: // CMP addr,Y
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)) + Y);
					break;
				case 0xDA: // PHX
					MarkCode(PC, 1);
					MarkPush(1);
					break;
				case 0xDB: // ??
					MarkCode(PC, 1);
					break;
				case 0xDC: // ??
					MarkCode(PC, 1);
					break;
				case 0xDD: // CMP addr,X
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0xDE: // DEC addr,X
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0xDF: // BBS5
					MarkCode(PC, 3);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xE0: // CPX #nn
					MarkCode(PC, 2);
					break;
				case 0xE1: // SBC (addr,X)
					MarkCode(PC, 2);
					tmp8 = (byte)(ReadMemory((ushort)(PC + 1)) + X);
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8));
					break;
				case 0xE2: // ??
					MarkCode(PC, 1);
					break;
				case 0xE3: // TIA src, dest, len
					MarkCode(PC, 7);
					if (!InBlockTransfer)
					{
						MarkBTFrom(ReadWord((ushort)(PC + 1)));
						MarkBTTo(ReadWord((ushort)(PC + 3)));
					}
					else
					{
						MarkBTFrom(btFrom);
						MarkBTTo(btTo+btAlternator);
						}
					break;
				case 0xE4: // CPX zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xE5: // SBC zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xE6: // INC zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xE7: // SMB6 zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xE8: // INX
					MarkCode(PC, 1);
					break;
				case 0xE9: // SBC #nn
					MarkCode(PC, 2);
					break;
				case 0xEA: // NOP
					MarkCode(PC, 1);
					break;
				case 0xEB: // ??
					MarkCode(PC, 1);
					break;
				case 0xEC: // CPX addr
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0xED: // SBC addr
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0xEE: // INC addr
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0xEF: // BBS6
					MarkCode(PC, 3);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xF0: // BEQ +/-rel
					MarkCode(PC, 2);
					break;
				case 0xF1: // SBC (addr),Y
					MarkCode(PC, 2);
					tmp8 = ReadMemory((ushort)(PC + 1));
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8) + Y);
					break;
				case 0xF2: // SBC (addr)
					MarkCode(PC, 2);
					tmp8 = ReadMemory((ushort)(PC + 1));
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8));
					break;
				case 0xF3: // TAI src, dest, len
					MarkCode(PC, 7);
					if (!InBlockTransfer)
					{
						MarkBTFrom(ReadWord((ushort)(PC + 1)));
						MarkBTTo(ReadWord((ushort)(PC + 3)));
					}
					else
					{
						MarkBTFrom(btFrom+btAlternator);
						MarkBTTo(btTo);
						}
					break;
				case 0xF4: // SET
					MarkCode(PC, 1);
					break;
				case 0xF5: // SBC zp,X
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0xF6: // INC zp,X
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0xF7: // SMB7 zp
					MarkCode(PC, 2);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xF8: // SED
					MarkCode(PC, 1);
					break;
				case 0xF9: // SBC addr,Y
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)) + Y);
					break;
				case 0xFA: // PLX
					MarkCode(PC, 1);
					MarkPop(1);
					break;
				case 0xFB: // ??
					MarkCode(PC, 1);
					break;
				case 0xFC: // ??
					MarkCode(PC, 1);
					break;
				case 0xFD: // SBC addr,X
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0xFE: // INC addr,X
					MarkCode(PC, 3);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0xFF: // BBS7
					MarkCode(PC, 3);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
			}
		}
	}
}
