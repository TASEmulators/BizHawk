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
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0x01: // ORA (addr,X)
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					tmp8 = (byte)(ReadMemory((ushort)(PC + 1)) + X);
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8));
					break;
				case 0x02: // SXY
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0x03: // ST0 #nn
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					break;
				case 0x04: // TSB zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x05: // ORA zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x06: // ASL zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x07: // RMB0 zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x08: // PHP
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					MarkPush(1);
					break;
				case 0x09: // ORA #nn
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					break;
				case 0x0A: // ASL A
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0x0B: // ??
					MarkCode(PC);
					break;
				case 0x0C: // TSB addr
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x0D: // ORA addr
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x0E: // ASL addr
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x0F: // BBR0
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x10: // BPL +/-rel
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					break;
				case 0x11: // ORA (addr),Y
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					tmp8 = ReadMemory((ushort)(PC + 1));
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8) + Y);
					break;
				case 0x12: // ORA (addr)
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					tmp8 = ReadMemory((ushort)(PC + 1));
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8));
					break;
				case 0x13: // ST1 #nn
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					break;
				case 0x14: // TRB zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x15: // ORA zp,X
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0x16: // ASL zp,X
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0x17: // RMB1 zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x18: // CLC
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0x19: // ORA addr,Y
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)) + Y);
					break;
				case 0x1A: // INC A
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0x1B: // ??
					MarkCode(PC);
					break;
				case 0x1C: // TRB addr
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x1D: // ORA addr,X
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0x1E: // ASL addr,X
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0x1F: // BBR1
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x20: // JSR addr
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x21: // AND (addr,X)
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					tmp8 = (byte)(ReadMemory((ushort)(PC + 1)) + X);
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8));
					break;
				case 0x22: // SAX
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0x23: // ST2 #nn
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					break;
				case 0x24: // BIT zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x25: // AND zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x26: // ROL zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x27: // RMB2 zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x28: // PLP
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					MarkPop(1);
					break;
				case 0x29: // AND #nn
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					break;
				case 0x2A: // ROL A
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0x2B: // ??
					MarkCode(PC);
					break;
				case 0x2C: // BIT addr
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x2D: // AND addr
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x2E: // ROL addr
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x2F: // BBR2
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x30: // BMI +/-rel
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					break;
				case 0x31: // AND (addr),Y
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					tmp8 = ReadMemory((ushort)(PC + 1));
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8) + Y);
					break;
				case 0x32: // AND (addr)
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					tmp8 = ReadMemory((ushort)(PC + 1));
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8));
					break;
				case 0x33: // ??
					MarkCode(PC);
					break;
				case 0x34: // BIT zp,X
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0x35: // AND zp,X
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0x36: // ROL zp,X
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0x37: // RMB3 zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x38: // SEC
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0x39: // AND addr,Y
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)) + Y);
					break;
				case 0x3A: // DEC A
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0x3B: // ??
					MarkCode(PC);
					break;
				case 0x3C: // BIT addr,X
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0x3D: // AND addr,X
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0x3E: // ROL addr,X
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0x3F: // BBR3
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x40: // RTI
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					MarkPop(3);
					break;
				case 0x41: // EOR (addr,X)
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					tmp8 = (byte)(ReadMemory((ushort)(PC + 1)) + X);
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8));
					break;
				case 0x42: // SAY
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0x43: // TMA #nn
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					break;
				case 0x44: // BSR +/-rel
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					break;
				case 0x45: // EOR zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x46: // LSR zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x47: // RMB4 zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x48: // PHA
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					MarkPush(1);
					break;
				case 0x49: // EOR #nn
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					break;
				case 0x4A: // LSR A
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0x4B: // ??
					MarkCode(PC);
					break;
				case 0x4C: // JMP addr
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x4D: // EOR addr
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x4E: // LSR addr
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x4F: // BBR4
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x50: // BVC +/-rel
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					break;
				case 0x51: // EOR (addr),Y
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					tmp8 = ReadMemory((ushort)(PC + 1));
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8) + Y);
					break;
				case 0x52: // EOR (addr)
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					tmp8 = ReadMemory((ushort)(PC + 1));
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8));
					break;
				case 0x53: // TAM #nn
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					break;
				case 0x54: // CSL
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0x55: // EOR zp,X
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0x56: // LSR zp,X
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0x57: // RMB5 zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x58: // CLI
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0x59: // EOR addr,Y
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)) + Y);
					break;
				case 0x5A: // PHY
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					MarkPush(1);
					break;
				case 0x5B: // ??
					MarkCode(PC);
					break;
				case 0x5C: // ??
					MarkCode(PC);
					break;
				case 0x5D: // EOR addr,X
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0x5E: // LSR addr,X
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0x5F: // BBR5
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x60: // RTS
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					MarkPop(2);
					break;
				case 0x61: // ADC (addr,X)
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					tmp8 = (byte)(ReadMemory((ushort)(PC + 1)) + X);
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8));
					break;
				case 0x62: // CLA
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0x63: // ??
					MarkCode(PC);
					break;
				case 0x64: // STZ zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x65: // ADC zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x66: // ROR zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x67: // RMB6 zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x68: // PLA
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					MarkPop(1);
					break;
				case 0x69: // ADC #nn
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					break;
				case 0x6A: // ROR A
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0x6B: // ??
					MarkCode(PC);
					break;
				case 0x6C: // JMP
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkFptr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x6D: // ADC addr
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x6E: // ROR addr
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x6F: // BBR6
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x70: // BVS +/-rel
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					break;
				case 0x71: // ADC (addr),Y
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					tmp8 = ReadMemory((ushort)(PC + 1));
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8) + Y);
					break;
				case 0x72: // ADC (addr)
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					tmp8 = ReadMemory((ushort)(PC + 1));
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8));
					break;
				case 0x73: // TII src, dest, len
					for (int i = 0; i < 7; i++)
						MarkCode(PC + i);
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
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0x75: // ADC zp,X
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0x76: // ROR zp,X
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0x77: // RMB7 zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x78: // SEI
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0x79: // ADC addr,Y
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)) + Y);
					break;
				case 0x7A: // PLY
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					MarkPop(1);
					break;
				case 0x7B: // ??
					MarkCode(PC);
					break;
				case 0x7C: // JMP
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkFptr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0x7D: // ADC addr,X
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0x7E: // ROR addr,X
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0x7F: // BBR7
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x80: // BRA +/-rel
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					break;
				case 0x81: // STA (addr,X)
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					tmp8 = (byte)(ReadMemory((ushort)(PC + 1)) + X);
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8));
					break;
				case 0x82: // CLX
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0x83: // TST
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 2)));
					break;
				case 0x84: // STY zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x85: // STA zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x86: // STX zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x87: // SMB0 zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x88: // DEY
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0x89: // BIT #nn
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					break;
				case 0x8A: // TXA
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0x8B: // ??
					MarkCode(PC);
					break;
				case 0x8C: // STY addr
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x8D: // STA addr
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x8E: // STX addr
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x8F: // BBS0
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x90: // BCC +/-rel
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					break;
				case 0x91: // STA (addr),Y
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					tmp8 = ReadMemory((ushort)(PC + 1));
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8) + Y);
					break;
				case 0x92: // STA (addr)
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					tmp8 = ReadMemory((ushort)(PC + 1));
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8));
					break;
				case 0x93: // TST
					for (int i = 0; i < 4; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 2)));
					break;
				case 0x94: // STY zp,X
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0x95: // STA zp,X
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0x96: // STX zp,Y
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)) + Y);
					break;
				case 0x97: // SMB1 zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0x98: // TYA
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0x99: // STA addr,Y
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)) + Y);
					break;
				case 0x9A: // TXS
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0x9B: // ??
					MarkCode(PC);
					break;
				case 0x9C: // STZ addr
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0x9D: // STA addr,X
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0x9E: // STZ addr,X
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0x9F: // BBS1
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xA0: // LDY #nn
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					break;
				case 0xA1: // LDA (addr,X)
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					tmp8 = (byte)(ReadMemory((ushort)(PC + 1)) + X);
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8));
					break;
				case 0xA2: // LDX #nn
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					break;
				case 0xA3: // TST
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 2)) + X);
					break;
				case 0xA4: // LDY zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xA5: // LDA zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xA6: // LDX zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xA7: // SMB2 zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xA8: // TAY
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0xA9: // LDA #nn
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					break;
				case 0xAA: // TAX
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0xAB: // ??
					MarkCode(PC);
					break;
				case 0xAC: // LDY addr
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0xAD: // LDA addr
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0xAE: // LDX addr
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0xAF: // BBS2
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xB0: // BCS +/-rel
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					break;
				case 0xB1: // LDA (addr),Y
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					tmp8 = ReadMemory((ushort)(PC + 1));
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8) + Y);
					break;
				case 0xB2: // LDA (addr)
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					tmp8 = ReadMemory((ushort)(PC + 1));
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8));
					break;
				case 0xB3: // TST
					for (int i = 0; i < 4; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 2)) + X);
					break;
				case 0xB4: // LDY zp,X
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0xB5: // LDA zp,X
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0xB6: // LDX zp,Y
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)) + Y);
					break;
				case 0xB7: // SMB3 zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xB8: // CLV
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0xB9: // LDA addr,Y
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)) + Y);
					break;
				case 0xBA: // TSX
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0xBB: // ??
					MarkCode(PC);
					break;
				case 0xBC: // LDY addr,X
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0xBD: // LDA addr,X
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0xBE: // LDX addr,Y
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)) + Y);
					break;
				case 0xBF: // BBS3
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xC0: // CPY #nn
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					break;
				case 0xC1: // CMP (addr,X)
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					tmp8 = (byte)(ReadMemory((ushort)(PC + 1)) + X);
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8));
					break;
				case 0xC2: // CLY
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0xC3: // TDD src, dest, len
					for (int i = 0; i < 7; i++)
						MarkCode(PC + i);
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
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xC5: // CMP zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xC6: // DEC zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xC7: // SMB4 zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xC8: // INY
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0xC9: // CMP #nn
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					break;
				case 0xCA: // DEX
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0xCB: // ??
					MarkCode(PC);
					break;
				case 0xCC: // CPY addr
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0xCD: // CMP addr
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0xCE: // DEC addr
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0xCF: // BBS4
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xD0: // BNE +/-rel
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					break;
				case 0xD1: // CMP (addr),Y
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					tmp8 = ReadMemory((ushort)(PC + 1));
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8) + Y);
					break;
				case 0xD2: // CMP (addr)
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					tmp8 = ReadMemory((ushort)(PC + 1));
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8));
					break;
				case 0xD3: // TIN src, dest, len
					for (int i = 0; i < 7; i++)
						MarkCode(PC + i);
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
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0xD5: // CMP zp,X
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0xD6: // DEC zp,X
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0xD7: // SMB5 zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xD8: // CLD
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0xD9: // CMP addr,Y
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)) + Y);
					break;
				case 0xDA: // PHX
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					MarkPush(1);
					break;
				case 0xDB: // ??
					MarkCode(PC);
					break;
				case 0xDC: // ??
					MarkCode(PC);
					break;
				case 0xDD: // CMP addr,X
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0xDE: // DEC addr,X
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0xDF: // BBS5
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xE0: // CPX #nn
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					break;
				case 0xE1: // SBC (addr,X)
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					tmp8 = (byte)(ReadMemory((ushort)(PC + 1)) + X);
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8));
					break;
				case 0xE2: // ??
					MarkCode(PC);
					break;
				case 0xE3: // TIA src, dest, len
					for (int i = 0; i < 7; i++)
						MarkCode(PC + i);
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
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xE5: // SBC zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xE6: // INC zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xE7: // SMB6 zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xE8: // INX
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0xE9: // SBC #nn
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					break;
				case 0xEA: // NOP
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0xEB: // ??
					MarkCode(PC);
					break;
				case 0xEC: // CPX addr
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0xED: // SBC addr
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0xEE: // INC addr
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)));
					break;
				case 0xEF: // BBS6
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xF0: // BEQ +/-rel
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					break;
				case 0xF1: // SBC (addr),Y
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					tmp8 = ReadMemory((ushort)(PC + 1));
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8) + Y);
					break;
				case 0xF2: // SBC (addr)
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					tmp8 = ReadMemory((ushort)(PC + 1));
					MarkZPPtr(tmp8);
					MarkIndirect(GetIndirect(tmp8));
					break;
				case 0xF3: // TAI src, dest, len
					for (int i = 0; i < 7; i++)
						MarkCode(PC + i);
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
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0xF5: // SBC zp,X
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0xF6: // INC zp,X
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)) + X);
					break;
				case 0xF7: // SMB7 zp
					for (int i = 0; i < 2; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
				case 0xF8: // SED
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					break;
				case 0xF9: // SBC addr,Y
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)) + Y);
					break;
				case 0xFA: // PLX
					for (int i = 0; i < 1; i++)
						MarkCode(PC + i);
					MarkPop(1);
					break;
				case 0xFB: // ??
					MarkCode(PC);
					break;
				case 0xFC: // ??
					MarkCode(PC);
					break;
				case 0xFD: // SBC addr,X
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0xFE: // INC addr,X
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkAddr(ReadWord((ushort)(PC + 1)) + X);
					break;
				case 0xFF: // BBS7
					for (int i = 0; i < 3; i++)
						MarkCode(PC + i);
					MarkZP(ReadMemory((ushort)(PC + 1)));
					break;
			}
		}
	}
}
