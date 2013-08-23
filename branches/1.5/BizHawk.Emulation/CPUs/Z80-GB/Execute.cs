using System;

/* TODO:
+ http://www.devrs.com/gb/files/opcodes.html was used as a flags and timing reference.  
+ [Opt] The DAA Table could be reduced from 128k to 8k.
+ Currently all instructions are using fixed m-cycles. Any instruction with variable cycles (ie branches) need to be checked.  
+ The following instructions were rewritten or substantially modified for Z80-GB. They should be
  treated with caution and checked further when the emulator is farther along.

	ADD
	ADC
	SUB
	SBC
	AND
	OR
	XOR
	CP
	SWAP
	DAA
	RLCA
	RLA
	RRCA
	RRA
	RLC
	RL
	RRC
	RR
	SLA
	SRA
	SRL
*/

namespace BizHawk.Emulation.CPUs.Z80GB
{
	public partial class Z80
	{
		public int TotalExecutedCycles;
		public int PendingCycles;

		bool Interruptable;

		public void ExecuteInstruction()
		{
			byte TB; byte TB2; sbyte TSB; ushort TUS; int TI1; int TI2; int TIR;
			while (RegPC.Word == 0x031A)
				break;
			byte op = ReadMemory(RegPC.Word++);
			int mCycleTime = mCycleTable[op];
			PendingCycles -= mCycleTime;
			TotalExecutedCycles += mCycleTime;
			switch (op)
			{
				case 0x00: // NOP
					break;
				case 0x01: // LD BC, nn
					RegBC.Word = (ushort)(ReadMemory(RegPC.Word++) + ReadMemory(RegPC.Word++) * 256);
					break;
				case 0x02: // LD (BC), A
					WriteMemory(RegBC.Word, RegAF.High);
					break;
				case 0x03: // INC BC
					++RegBC.Word;
					break;
				case 0x04: // INC B
					RegAF.Low = (byte)(IncTable[++RegBC.High] | (RegAF.Low & 16));
					break;
				case 0x05: // DEC B
					RegAF.Low = (byte)(DecTable[--RegBC.High] | (RegAF.Low & 16));
					break;
				case 0x06: // LD B, n
					RegBC.High = ReadMemory(RegPC.Word++);
					break;
				case 0x07: // RLCA
					RegAF.Low = (byte)((RegAF.High & 0x80) >> 3);
					RegAF.High = (byte)((RegAF.High >> 7) | (RegAF.High << 1));
					break;
				case 0x08: // LD (imm), SP
					TUS = (ushort)(ReadMemory(RegPC.Word++) | (ReadMemory(RegPC.Word++) << 8));
					WriteMemory(TUS++, RegSP.Low);
					WriteMemory(TUS, RegSP.High);
					break;
				case 0x09: // ADD HL, BC
					TI1 = (short)RegHL.Word; TI2 = (short)RegBC.Word; TIR = TI1 + TI2;
					TUS = (ushort)TIR;
					FlagH = ((TI1 & 0xFFF) + (TI2 & 0xFFF)) > 0xFFF;
					FlagN = false;
					FlagC = ((ushort)TI1 + (ushort)TI2) > 0xFFFF;
					RegHL.Word = TUS;
					break;
				case 0x0A: // LD A, (BC)
					RegAF.High = ReadMemory(RegBC.Word);
					break;
				case 0x0B: // DEC BC
					--RegBC.Word;
					break;
				case 0x0C: // INC C
					RegAF.Low = (byte)(IncTable[++RegBC.Low] | (RegAF.Low & 16));
					break;
				case 0x0D: // DEC C
					RegAF.Low = (byte)(DecTable[--RegBC.Low] | (RegAF.Low & 16));
					break;
				case 0x0E: // LD C, n
					RegBC.Low = ReadMemory(RegPC.Word++);
					break;
				case 0x0F: // RRCA
					RegAF.High = (byte)((RegAF.High << 7) | (RegAF.High >> 1));
					RegAF.Low = (byte)((RegAF.High & 0x80) >> 3);
					break;
				case 0x10: // STOP
					Console.WriteLine("STOP!!!!!!!!!!!!!!!!!!!!!!"); // TODO this instruction is actually STOP. not DJNZ d.
					throw new Exception("CPU stopped. What now?");
				case 0x11: // LD DE, nn
					RegDE.Word = (ushort)(ReadMemory(RegPC.Word++) + ReadMemory(RegPC.Word++) * 256);
					break;
				case 0x12: // LD (DE), A
					WriteMemory(RegDE.Word, RegAF.High);
					break;
				case 0x13: // INC DE
					++RegDE.Word;
					break;
				case 0x14: // INC D
					RegAF.Low = (byte)(IncTable[++RegDE.High] | (RegAF.Low & 16));
					break;
				case 0x15: // DEC D
					RegAF.Low = (byte)(DecTable[--RegDE.High] | (RegAF.Low & 16));
					break;
				case 0x16: // LD D, n
					RegDE.High = ReadMemory(RegPC.Word++);
					break;
				case 0x17: // RLA
					TB = (byte)((RegAF.High & 0x80) >> 3);
					RegAF.High = (byte)((RegAF.High << 1) | (FlagC ? 1 : 0));
					RegAF.Low = TB;
					break;
				case 0x18: // JR d
					TSB = (sbyte)ReadMemory(RegPC.Word++);
					RegPC.Word = (ushort)(RegPC.Word + TSB);
					break;
				case 0x19: // ADD HL, DE
					TI1 = (short)RegHL.Word; TI2 = (short)RegDE.Word; TIR = TI1 + TI2;
					TUS = (ushort)TIR;
					FlagH = ((TI1 & 0xFFF) + (TI2 & 0xFFF)) > 0xFFF;
					FlagN = false;
					FlagC = ((ushort)TI1 + (ushort)TI2) > 0xFFFF;
					RegHL.Word = TUS;
					break;
				case 0x1A: // LD A, (DE)
					RegAF.High = ReadMemory(RegDE.Word);
					break;
				case 0x1B: // DEC DE
					--RegDE.Word;
					break;
				case 0x1C: // INC E
					RegAF.Low = (byte)(IncTable[++RegDE.Low] | (RegAF.Low & 16));
					break;
				case 0x1D: // DEC E
					RegAF.Low = (byte)(DecTable[--RegDE.Low] | (RegAF.Low & 16));
					break;
				case 0x1E: // LD E, n
					RegDE.Low = ReadMemory(RegPC.Word++);
					break;
				case 0x1F: // RRA
					TB = (byte)((RegAF.High & 0x1) << 4);
					RegAF.High = (byte)((RegAF.High >> 1) | (FlagC ? 0x80 : 0));
					RegAF.Low = TB;
					break;
				case 0x20: // JR NZ, d
					TSB = (sbyte)ReadMemory(RegPC.Word++);
					if (!FlagZ)
						RegPC.Word = (ushort)(RegPC.Word + TSB);
					break;
				case 0x21: // LD HL, nn
					RegHL.Word = (ushort)(ReadMemory(RegPC.Word++) + ReadMemory(RegPC.Word++) * 256);
					break;
				case 0x22: // LDI (HL), A
					WriteMemory(RegHL.Word++, RegAF.High);
					break;
				case 0x23: // INC HL
					++RegHL.Word;
					break;
				case 0x24: // INC H
					RegAF.Low = (byte)(IncTable[++RegHL.High] | (RegAF.Low & 16));
					break;
				case 0x25: // DEC H
					RegAF.Low = (byte)(DecTable[--RegHL.High] | (RegAF.Low & 16));
					break;
				case 0x26: // LD H, n
					RegHL.High = ReadMemory(RegPC.Word++);
					break;
				case 0x27: // DAA
					RegAF.Word = TableDaa[RegAF.Word];
					break;
				case 0x28: // JR Z, d
					TSB = (sbyte)ReadMemory(RegPC.Word++);
					if (FlagZ)
						RegPC.Word = (ushort)(RegPC.Word + TSB);
					break;
				case 0x29: // ADD HL, HL
					TI1 = (short)RegHL.Word; TI2 = (short)RegHL.Word; TIR = TI1 + TI2;
					TUS = (ushort)TIR;
					FlagH = ((TI1 & 0xFFF) + (TI2 & 0xFFF)) > 0xFFF;
					FlagN = false;
					FlagC = ((ushort)TI1 + (ushort)TI2) > 0xFFFF;
					RegHL.Word = TUS;
					break;
				case 0x2A: // LDI A, (HL)
					RegAF.High = ReadMemory(RegHL.Word++);
					break;
				case 0x2B: // DEC HL
					--RegHL.Word;
					break;
				case 0x2C: // INC L
					RegAF.Low = (byte)(IncTable[++RegHL.Low] | (RegAF.Low & 16));
					break;
				case 0x2D: // DEC L
					RegAF.Low = (byte)(DecTable[--RegHL.Low] | (RegAF.Low & 16));
					break;
				case 0x2E: // LD L, n
					RegHL.Low = ReadMemory(RegPC.Word++);
					break;
				case 0x2F: // CPL
					RegAF.High ^= 0xFF; FlagH = true; FlagN = true;
					break;
				case 0x30: // JR NC, d
					TSB = (sbyte)ReadMemory(RegPC.Word++);
					if (!FlagC)
						RegPC.Word = (ushort)(RegPC.Word + TSB);
					break;
				case 0x31: // LD SP, nn
					RegSP.Word = (ushort)(ReadMemory(RegPC.Word++) + ReadMemory(RegPC.Word++) * 256);
					break;
				case 0x32: // LDD (HL), A
					WriteMemory(RegHL.Word--, RegAF.High);
					break;
				case 0x33: // INC SP
					++RegSP.Word;
					break;
				case 0x34: // INC (HL)
					TB = ReadMemory(RegHL.Word); RegAF.Low = (byte)(IncTable[++TB] | (RegAF.Low & 16)); WriteMemory(RegHL.Word, TB);
					break;
				case 0x35: // DEC (HL)
					TB = ReadMemory(RegHL.Word); RegAF.Low = (byte)(DecTable[--TB] | (RegAF.Low & 16)); WriteMemory(RegHL.Word, TB);
					break;
				case 0x36: // LD (HL), n
					WriteMemory(RegHL.Word, ReadMemory(RegPC.Word++));
					break;
				case 0x37: // SCF
					FlagH = false; FlagN = false; FlagC = true;
					break;
				case 0x38: // JR C, d
					TSB = (sbyte)ReadMemory(RegPC.Word++);
					if (FlagC)
						RegPC.Word = (ushort)(RegPC.Word + TSB);
					break;
				case 0x39: // ADD HL, SP
					TI1 = (short)RegHL.Word; TI2 = (short)RegSP.Word; TIR = TI1 + TI2;
					TUS = (ushort)TIR;
					FlagH = ((TI1 & 0xFFF) + (TI2 & 0xFFF)) > 0xFFF;
					FlagN = false;
					FlagC = ((ushort)TI1 + (ushort)TI2) > 0xFFFF;
					RegHL.Word = TUS;
					break;
				case 0x3A: // LDD A, (HL)
					RegAF.High = ReadMemory(RegHL.Word--);
					break;
				case 0x3B: // DEC SP
					--RegSP.Word;
					break;
				case 0x3C: // INC A
					RegAF.Low = (byte)(IncTable[++RegAF.High] | (RegAF.Low & 16));
					break;
				case 0x3D: // DEC A
					RegAF.Low = (byte)(DecTable[--RegAF.High] | (RegAF.Low & 16));
					break;
				case 0x3E: // LD A, n
					RegAF.High = ReadMemory(RegPC.Word++);
					break;
				case 0x3F: // CCF
					FlagH = FlagC; FlagN = false; FlagC ^= true;
					break;
				case 0x40: // LD B, B
					break;
				case 0x41: // LD B, C
					RegBC.High = RegBC.Low;
					break;
				case 0x42: // LD B, D
					RegBC.High = RegDE.High;
					break;
				case 0x43: // LD B, E
					RegBC.High = RegDE.Low;
					break;
				case 0x44: // LD B, H
					RegBC.High = RegHL.High;
					break;
				case 0x45: // LD B, L
					RegBC.High = RegHL.Low;
					break;
				case 0x46: // LD B, (HL)
					RegBC.High = ReadMemory(RegHL.Word);
					break;
				case 0x47: // LD B, A
					RegBC.High = RegAF.High;
					break;
				case 0x48: // LD C, B
					RegBC.Low = RegBC.High;
					break;
				case 0x49: // LD C, C
					break;
				case 0x4A: // LD C, D
					RegBC.Low = RegDE.High;
					break;
				case 0x4B: // LD C, E
					RegBC.Low = RegDE.Low;
					break;
				case 0x4C: // LD C, H
					RegBC.Low = RegHL.High;
					break;
				case 0x4D: // LD C, L
					RegBC.Low = RegHL.Low;
					break;
				case 0x4E: // LD C, (HL)
					RegBC.Low = ReadMemory(RegHL.Word);
					break;
				case 0x4F: // LD C, A
					RegBC.Low = RegAF.High;
					break;
				case 0x50: // LD D, B
					RegDE.High = RegBC.High;
					break;
				case 0x51: // LD D, C
					RegDE.High = RegBC.Low;
					break;
				case 0x52: // LD D, D
					break;
				case 0x53: // LD D, E
					RegDE.High = RegDE.Low;
					break;
				case 0x54: // LD D, H
					RegDE.High = RegHL.High;
					break;
				case 0x55: // LD D, L
					RegDE.High = RegHL.Low;
					break;
				case 0x56: // LD D, (HL)
					RegDE.High = ReadMemory(RegHL.Word);
					break;
				case 0x57: // LD D, A
					RegDE.High = RegAF.High;
					break;
				case 0x58: // LD E, B
					RegDE.Low = RegBC.High;
					break;
				case 0x59: // LD E, C
					RegDE.Low = RegBC.Low;
					break;
				case 0x5A: // LD E, D
					RegDE.Low = RegDE.High;
					break;
				case 0x5B: // LD E, E
					break;
				case 0x5C: // LD E, H
					RegDE.Low = RegHL.High;
					break;
				case 0x5D: // LD E, L
					RegDE.Low = RegHL.Low;
					break;
				case 0x5E: // LD E, (HL)
					RegDE.Low = ReadMemory(RegHL.Word);
					break;
				case 0x5F: // LD E, A
					RegDE.Low = RegAF.High;
					break;
				case 0x60: // LD H, B
					RegHL.High = RegBC.High;
					break;
				case 0x61: // LD H, C
					RegHL.High = RegBC.Low;
					break;
				case 0x62: // LD H, D
					RegHL.High = RegDE.High;
					break;
				case 0x63: // LD H, E
					RegHL.High = RegDE.Low;
					break;
				case 0x64: // LD H, H
					break;
				case 0x65: // LD H, L
					RegHL.High = RegHL.Low;
					break;
				case 0x66: // LD H, (HL)
					RegHL.High = ReadMemory(RegHL.Word);
					break;
				case 0x67: // LD H, A
					RegHL.High = RegAF.High;
					break;
				case 0x68: // LD L, B
					RegHL.Low = RegBC.High;
					break;
				case 0x69: // LD L, C
					RegHL.Low = RegBC.Low;
					break;
				case 0x6A: // LD L, D
					RegHL.Low = RegDE.High;
					break;
				case 0x6B: // LD L, E
					RegHL.Low = RegDE.Low;
					break;
				case 0x6C: // LD L, H
					RegHL.Low = RegHL.High;
					break;
				case 0x6D: // LD L, L
					break;
				case 0x6E: // LD L, (HL)
					RegHL.Low = ReadMemory(RegHL.Word);
					break;
				case 0x6F: // LD L, A
					RegHL.Low = RegAF.High;
					break;
				case 0x70: // LD (HL), B
					WriteMemory(RegHL.Word, RegBC.High);
					break;
				case 0x71: // LD (HL), C
					WriteMemory(RegHL.Word, RegBC.Low);
					break;
				case 0x72: // LD (HL), D
					WriteMemory(RegHL.Word, RegDE.High);
					break;
				case 0x73: // LD (HL), E
					WriteMemory(RegHL.Word, RegDE.Low);
					break;
				case 0x74: // LD (HL), H
					WriteMemory(RegHL.Word, RegHL.High);
					break;
				case 0x75: // LD (HL), L
					WriteMemory(RegHL.Word, RegHL.Low);
					break;
				case 0x76: // HALT
					Halt();
					break;
				case 0x77: // LD (HL), A
					WriteMemory(RegHL.Word, RegAF.High);
					break;
				case 0x78: // LD A, B
					RegAF.High = RegBC.High;
					break;
				case 0x79: // LD A, C
					RegAF.High = RegBC.Low;
					break;
				case 0x7A: // LD A, D
					RegAF.High = RegDE.High;
					break;
				case 0x7B: // LD A, E
					RegAF.High = RegDE.Low;
					break;
				case 0x7C: // LD A, H
					RegAF.High = RegHL.High;
					break;
				case 0x7D: // LD A, L
					RegAF.High = RegHL.Low;
					break;
				case 0x7E: // LD A, (HL)
					RegAF.High = ReadMemory(RegHL.Word);
					break;
				case 0x7F: // LD A, A
					break;
				case 0x80: // ADD B
					TI1 = RegAF.High + RegBC.High;
					RegAF.Low = 0;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 > 0xFF) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) + (RegBC.High & 0x0F) > 0x0F) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0x81: // ADD C
					TI1 = RegAF.High + RegBC.Low;
					RegAF.Low = 0;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 > 0xFF) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) + (RegBC.Low & 0x0F) > 0x0F) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0x82: // ADD D
					TI1 = RegAF.High + RegDE.High;
					RegAF.Low = 0;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 > 0xFF) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) + (RegDE.High & 0x0F) > 0x0F) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0x83: // ADD E
					TI1 = RegAF.High + RegDE.Low;
					RegAF.Low = 0;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 > 0xFF) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) + (RegDE.Low & 0x0F) > 0x0F) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0x84: // ADD H
					TI1 = RegAF.High + RegHL.High;
					RegAF.Low = 0;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 > 0xFF) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) + (RegHL.High & 0x0F) > 0x0F) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0x85: // ADD L
					TI1 = RegAF.High + RegHL.Low;
					RegAF.Low = 0;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 > 0xFF) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) + (RegHL.Low & 0x0F) > 0x0F) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0x86: // ADD (HL)
					TB = ReadMemory(RegHL.Word);
					TI1 = RegAF.High + TB;
					RegAF.Low = 0;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 > 0xFF) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) + (TB & 0x0F) > 0x0F) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0x87: // ADD A
					TI1 = RegAF.High + RegAF.High;
					RegAF.Low = 0;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 > 0xFF) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) + (RegAF.High & 0x0F) > 0x0F) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0x88: // ADC B
					TI2 = FlagC ? 1 : 0;
					TI1 = RegAF.High + RegBC.High + TI2;
					RegAF.Low = 0;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 > 0xFF) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) + (RegBC.High & 0x0F) + TI2 > 0x0F) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0x89: // ADC C
					TI2 = FlagC ? 1 : 0;
					TI1 = RegAF.High + RegBC.Low + TI2;
					RegAF.Low = 0;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 > 0xFF) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) + (RegBC.Low & 0x0F) + TI2 > 0x0F) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0x8A: // ADC D
					TI2 = FlagC ? 1 : 0;
					TI1 = RegAF.High + RegDE.High + TI2;
					RegAF.Low = 0;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 > 0xFF) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) + (RegDE.High & 0x0F) + TI2 > 0x0F) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0x8B: // ADC E
					TI2 = FlagC ? 1 : 0;
					TI1 = RegAF.High + RegDE.Low + TI2;
					RegAF.Low = 0;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 > 0xFF) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) + (RegDE.Low & 0x0F) + TI2 > 0x0F) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0x8C: // ADC H
					TI2 = FlagC ? 1 : 0;
					TI1 = RegAF.High + RegHL.High + TI2;
					RegAF.Low = 0;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 > 0xFF) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) + (RegHL.High & 0x0F) + TI2 > 0x0F) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0x8D: // ADC L
					TI2 = FlagC ? 1 : 0;
					TI1 = RegAF.High + RegHL.Low + TI2;
					RegAF.Low = 0;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 > 0xFF) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) + (RegHL.Low & 0x0F) + TI2 > 0x0F) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0x8E: // ADC (HL)
					TB = ReadMemory(RegHL.Word);
					TI2 = FlagC ? 1 : 0;
					TI1 = RegAF.High + TB + TI2;
					RegAF.Low = 0;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 > 0xFF) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) + (TB & 0x0F) + TI2 > 0x0F) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0x8F: // ADC A
					TI2 = FlagC ? 1 : 0;
					TI1 = RegAF.High + RegAF.High + TI2;
					RegAF.Low = 0;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 > 0xFF) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) + (RegAF.High & 0x0F) + TI2 > 0x0F) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0x90: // SUB B
					TI1 = RegAF.High - RegBC.High;
					RegAF.Low = 0x40;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 < 0) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) - (RegBC.High & 0x0F) < 0) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0x91: // SUB C
					TI1 = RegAF.High - RegBC.Low;
					RegAF.Low = 0x40;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 < 0) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) - (RegBC.Low & 0x0F) < 0) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0x92: // SUB D
					TI1 = RegAF.High - RegDE.High;
					RegAF.Low = 0x40;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 < 0) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) - (RegDE.High & 0x0F) < 0) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0x93: // SUB E
					TI1 = RegAF.High - RegDE.Low;
					RegAF.Low = 0x40;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 < 0) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) - (RegDE.Low & 0x0F) < 0) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0x94: // SUB H
					TI1 = RegAF.High - RegHL.High;
					RegAF.Low = 0x40;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 < 0) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) - (RegHL.High & 0x0F) < 0) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0x95: // SUB L
					TI1 = RegAF.High - RegHL.Low;
					RegAF.Low = 0x40;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 < 0) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) - (RegHL.Low & 0x0F) < 0) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0x96: // SUB (HL)
					TB = ReadMemory(RegHL.Word);
					TI1 = RegAF.High - TB;
					RegAF.Low = 0x40;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 < 0) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) - (TB & 0x0F) < 0) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0x97: // SUB A
					TI1 = RegAF.High - RegAF.High;
					RegAF.Low = 0x40;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 < 0) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) - (RegAF.High & 0x0F) < 0) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0x98: // SBC B
					TI2 = FlagC ? 1 : 0;
					TI1 = RegAF.High - RegBC.High - TI2;
					RegAF.Low = 0x40;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 < 0) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) - (RegBC.High & 0x0F) - TI2 < 0) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0x99: // SBC C
					TI2 = FlagC ? 1 : 0;
					TI1 = RegAF.High - RegBC.Low - TI2;
					RegAF.Low = 0x40;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 < 0) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) - (RegBC.Low & 0x0F) - TI2 < 0) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0x9A: // SBC D
					TI2 = FlagC ? 1 : 0;
					TI1 = RegAF.High - RegDE.High - TI2;
					RegAF.Low = 0x40;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 < 0) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) - (RegDE.High & 0x0F) - TI2 < 0) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0x9B: // SBC E
					TI2 = FlagC ? 1 : 0;
					TI1 = RegAF.High - RegDE.Low - TI2;
					RegAF.Low = 0x40;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 < 0) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) - (RegDE.Low & 0x0F) - TI2 < 0) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0x9C: // SBC H
					TI2 = FlagC ? 1 : 0;
					TI1 = RegAF.High - RegHL.High - TI2;
					RegAF.Low = 0x40;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 < 0) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) - (RegHL.High & 0x0F) - TI2 < 0) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0x9D: // SBC L
					TI2 = FlagC ? 1 : 0;
					TI1 = RegAF.High - RegHL.Low - TI2;
					RegAF.Low = 0x40;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 < 0) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) - (RegHL.Low & 0x0F) - TI2 < 0) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0x9E: // SBC (HL)
					TB = ReadMemory(RegHL.Word);
					TI2 = FlagC ? 1 : 0;
					TI1 = RegAF.High - TB - TI2;
					RegAF.Low = 0x40;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 < 0) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) - (TB & 0x0F) - TI2 < 0) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0x9F: // SBC A
					TI2 = FlagC ? 1 : 0;
					TI1 = RegAF.High - RegAF.High - TI2;
					RegAF.Low = 0x40;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 < 0) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) - (RegAF.High & 0x0F) - TI2 < 0) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0xA0: // AND B
					RegAF.High &= RegBC.High;
					RegAF.Low = (byte)(RegAF.High == 0 ? 0xA0 : 0x20);
					break;
				case 0xA1: // AND C
					RegAF.High &= RegBC.Low;
					RegAF.Low = (byte)(RegAF.High == 0 ? 0xA0 : 0x20);
					break;
				case 0xA2: // AND D
					RegAF.High &= RegDE.High;
					RegAF.Low = (byte)(RegAF.High == 0 ? 0xA0 : 0x20);
					break;
				case 0xA3: // AND E
					RegAF.High &= RegDE.Low;
					RegAF.Low = (byte)(RegAF.High == 0 ? 0xA0 : 0x20);
					break;
				case 0xA4: // AND H
					RegAF.High &= RegHL.High;
					RegAF.Low = (byte)(RegAF.High == 0 ? 0xA0 : 0x20);
					break;
				case 0xA5: // AND L
					RegAF.High &= RegHL.Low;
					RegAF.Low = (byte)(RegAF.High == 0 ? 0xA0 : 0x20);
					break;
				case 0xA6: // AND (HL)
					RegAF.High &= ReadMemory(RegHL.Word);
					RegAF.Low = (byte)(RegAF.High == 0 ? 0xA0 : 0x20);
					break;
				case 0xA7: // AND A
					RegAF.High &= RegAF.High;
					RegAF.Low = (byte)(RegAF.High == 0 ? 0xA0 : 0x20);
					break;
				case 0xA8: // XOR B
					RegAF.High ^= RegBC.High;
					RegAF.Low = (byte)(RegAF.High == 0 ? 0x80 : 0);
					break;
				case 0xA9: // XOR C
					RegAF.High ^= RegBC.Low;
					RegAF.Low = (byte)(RegAF.High == 0 ? 0x80 : 0);
					break;
				case 0xAA: // XOR D
					RegAF.High ^= RegDE.High;
					RegAF.Low = (byte)(RegAF.High == 0 ? 0x80 : 0);
					break;
				case 0xAB: // XOR E
					RegAF.High ^= RegDE.Low;
					RegAF.Low = (byte)(RegAF.High == 0 ? 0x80 : 0);
					break;
				case 0xAC: // XOR H
					RegAF.High ^= RegHL.High;
					RegAF.Low = (byte)(RegAF.High == 0 ? 0x80 : 0);
					break;
				case 0xAD: // XOR L
					RegAF.High ^= RegHL.Low;
					RegAF.Low = (byte)(RegAF.High == 0 ? 0x80 : 0);
					break;
				case 0xAE: // XOR (HL)
					RegAF.High ^= ReadMemory(RegHL.Word);
					RegAF.Low = (byte)(RegAF.High == 0 ? 0x80 : 0);
					break;
				case 0xAF: // XOR A
					RegAF.High ^= RegAF.High;
					RegAF.Low = (byte)(RegAF.High == 0 ? 0x80 : 0);
					break;
				case 0xB0: // OR B
					RegAF.High |= RegBC.High;
					RegAF.Low = (byte)(RegAF.High == 0 ? 0x80 : 0);
					break;
				case 0xB1: // OR C
					RegAF.High |= RegBC.Low;
					RegAF.Low = (byte)(RegAF.High == 0 ? 0x80 : 0);
					break;
				case 0xB2: // OR D
					RegAF.High |= RegDE.High;
					RegAF.Low = (byte)(RegAF.High == 0 ? 0x80 : 0);
					break;
				case 0xB3: // OR E
					RegAF.High |= RegDE.Low;
					RegAF.Low = (byte)(RegAF.High == 0 ? 0x80 : 0);
					break;
				case 0xB4: // OR H
					RegAF.High |= RegHL.High;
					RegAF.Low = (byte)(RegAF.High == 0 ? 0x80 : 0);
					break;
				case 0xB5: // OR L
					RegAF.High |= RegHL.Low;
					RegAF.Low = (byte)(RegAF.High == 0 ? 0x80 : 0);
					break;
				case 0xB6: // OR (HL)
					RegAF.High |= ReadMemory(RegHL.Word);
					RegAF.Low = (byte)(RegAF.High == 0 ? 0x80 : 0);
					break;
				case 0xB7: // OR A
					RegAF.High |= RegAF.High;
					RegAF.Low = (byte)(RegAF.High == 0 ? 0x80 : 0);
					break;
				case 0xB8: // CP B
					TI1 = RegAF.High - RegBC.High;
					RegAF.Low = 0x40;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 < 0) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) - (RegBC.High & 0x0F) < 0) RegAF.Low |= 0x20;
					break;
				case 0xB9: // CP C
					TI1 = RegAF.High - RegBC.Low;
					RegAF.Low = 0x40;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 < 0) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) - (RegBC.Low & 0x0F) < 0) RegAF.Low |= 0x20;
					break;
				case 0xBA: // CP D
					TI1 = RegAF.High - RegDE.High;
					RegAF.Low = 0x40;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 < 0) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) - (RegDE.High & 0x0F) < 0) RegAF.Low |= 0x20;
					break;
				case 0xBB: // CP E
					TI1 = RegAF.High - RegDE.Low;
					RegAF.Low = 0x40;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 < 0) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) - (RegDE.Low & 0x0F) < 0) RegAF.Low |= 0x20;
					break;
				case 0xBC: // CP H
					TI1 = RegAF.High - RegHL.High;
					RegAF.Low = 0x40;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 < 0) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) - (RegHL.High & 0x0F) < 0) RegAF.Low |= 0x20;
					break;
				case 0xBD: // CP L
					TI1 = RegAF.High - RegHL.Low;
					RegAF.Low = 0x40;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 < 0) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) - (RegHL.Low & 0x0F) < 0) RegAF.Low |= 0x20;
					break;
				case 0xBE: // CP (HL)
					TB = ReadMemory(RegHL.Word);
					TI1 = RegAF.High - TB;
					RegAF.Low = 0x40;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 < 0) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) - (TB & 0x0F) < 0) RegAF.Low |= 0x20;
					break;
				case 0xBF: // CP A
					TI1 = RegAF.High - RegAF.High;
					RegAF.Low = 0x40;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 < 0) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) - (RegAF.High & 0x0F) < 0) RegAF.Low |= 0x20;
					break;
				case 0xC0: // RET NZ
					if (!FlagZ)
						RegPC.Low = ReadMemory(RegSP.Word++); RegPC.High = ReadMemory(RegSP.Word++);
					break;
				case 0xC1: // POP BC
					RegBC.Low = ReadMemory(RegSP.Word++); RegBC.High = ReadMemory(RegSP.Word++);
					break;
				case 0xC2: // JP NZ, nn
					TUS = (ushort)(ReadMemory(RegPC.Word++) + ReadMemory(RegPC.Word++) * 256);
					if (!FlagZ)
						RegPC.Word = TUS;
					break;
				case 0xC3: // JP nn
					RegPC.Word = (ushort)(ReadMemory(RegPC.Word++) + ReadMemory(RegPC.Word++) * 256);
					break;
				case 0xC4: // CALL NZ, nn
					TUS = (ushort)(ReadMemory(RegPC.Word++) + ReadMemory(RegPC.Word++) * 256);
					if (!FlagZ)
					{
						WriteMemory(--RegSP.Word, RegPC.High); WriteMemory(--RegSP.Word, RegPC.Low);
						RegPC.Word = TUS;
					}
					break;
				case 0xC5: // PUSH BC
					WriteMemory(--RegSP.Word, RegBC.High); WriteMemory(--RegSP.Word, RegBC.Low);
					break;
				case 0xC6: // ADD n
					TB = ReadMemory(RegPC.Word++);
					TI1 = RegAF.High + TB;
					RegAF.Low = 0;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 > 0xFF) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) + (TB & 0x0F) > 0x0F) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0xC7: // RST $00
					WriteMemory(--RegSP.Word, RegPC.High); WriteMemory(--RegSP.Word, RegPC.Low);
					RegPC.Word = 0x00;
					break;
				case 0xC8: // RET Z
					if (FlagZ)
						RegPC.Low = ReadMemory(RegSP.Word++); RegPC.High = ReadMemory(RegSP.Word++);
					break;
				case 0xC9: // RET
					RegPC.Low = ReadMemory(RegSP.Word++); RegPC.High = ReadMemory(RegSP.Word++);
					break;
				case 0xCA: // JP Z, nn
					TUS = (ushort)(ReadMemory(RegPC.Word++) + ReadMemory(RegPC.Word++) * 256);
					if (FlagZ)
						RegPC.Word = TUS;
					break;
				case 0xCB: // (Prefix)
					op = ReadMemory(RegPC.Word++);
					mCycleTime = cbMCycleTable[op];
					PendingCycles -= mCycleTime;
					TotalExecutedCycles += mCycleTime;
					switch (op)
					{
						case 0x00: // RLC B
							RegAF.Low = (byte)((RegBC.High & 0x80) >> 3);
							RegBC.High = (byte)((RegBC.High >> 7) | (RegBC.High << 1));
							if (RegBC.High == 0) RegAF.Low |= 0x80;
							break;
						case 0x01: // RLC C
							RegAF.Low = (byte)((RegBC.Low & 0x80) >> 3);
							RegBC.Low = (byte)((RegBC.Low >> 7) | (RegBC.Low << 1));
							if (RegBC.Low == 0) RegAF.Low |= 0x80;
							break;
						case 0x02: // RLC D
							RegAF.Low = (byte)((RegDE.High & 0x80) >> 3);
							RegDE.High = (byte)((RegDE.High >> 7) | (RegDE.High << 1));
							if (RegDE.High == 0) RegAF.Low |= 0x80;
							break;
						case 0x03: // RLC E
							RegAF.Low = (byte)((RegDE.Low & 0x80) >> 3);
							RegDE.Low = (byte)((RegDE.Low >> 7) | (RegDE.Low << 1));
							if (RegDE.Low == 0) RegAF.Low |= 0x80;
							break;
						case 0x04: // RLC H
							RegAF.Low = (byte)((RegHL.High & 0x80) >> 3);
							RegHL.High = (byte)((RegHL.High >> 7) | (RegHL.High << 1));
							if (RegHL.High == 0) RegAF.Low |= 0x80;
							break;
						case 0x05: // RLC L
							RegAF.Low = (byte)((RegHL.Low & 0x80) >> 3);
							RegHL.Low = (byte)((RegHL.Low >> 7) | (RegHL.Low << 1));
							if (RegHL.Low == 0) RegAF.Low |= 0x80;
							break;
						case 0x06: // RLC (HL)
							TB = ReadMemory(RegHL.Word);
							RegAF.Low = (byte)((TB & 0x80) >> 3);
							TB = (byte)((TB >> 7) | (TB << 1));
							if (TB == 0) RegAF.Low |= 0x80;
							WriteMemory(RegHL.Word, TB);
							break;
						case 0x07: // RLC A
							RegAF.Low = (byte)((RegAF.High & 0x80) >> 3);
							RegAF.High = (byte)((RegAF.High >> 7) | (RegAF.High << 1));
							if (RegAF.High == 0) RegAF.Low |= 0x80;
							break;
						case 0x08: // RRC B
							RegBC.High = (byte)((RegBC.High << 7) | (RegBC.High >> 1));
							RegAF.Low = (byte)((RegBC.High & 0x80) >> 3);
							if (RegBC.High == 0) RegAF.Low |= 0x80;
							break;
						case 0x09: // RRC C
							RegBC.Low = (byte)((RegBC.Low << 7) | (RegBC.Low >> 1));
							RegAF.Low = (byte)((RegBC.Low & 0x80) >> 3);
							if (RegBC.Low == 0) RegAF.Low |= 0x80;
							break;
						case 0x0A: // RRC D
							RegDE.High = (byte)((RegDE.High << 7) | (RegDE.High >> 1));
							RegAF.Low = (byte)((RegDE.High & 0x80) >> 3);
							if (RegDE.High == 0) RegAF.Low |= 0x80;
							break;
						case 0x0B: // RRC E
							RegDE.Low = (byte)((RegDE.Low << 7) | (RegDE.Low >> 1));
							RegAF.Low = (byte)((RegDE.Low & 0x80) >> 3);
							if (RegDE.Low == 0) RegAF.Low |= 0x80;
							break;
						case 0x0C: // RRC H
							RegHL.High = (byte)((RegHL.High << 7) | (RegHL.High >> 1));
							RegAF.Low = (byte)((RegHL.High & 0x80) >> 3);
							if (RegHL.High == 0) RegAF.Low |= 0x80;
							break;
						case 0x0D: // RRC L
							RegHL.Low = (byte)((RegHL.Low << 7) | (RegHL.Low >> 1));
							RegAF.Low = (byte)((RegHL.Low & 0x80) >> 3);
							if (RegHL.Low == 0) RegAF.Low |= 0x80;
							break;
						case 0x0E: // RRC (HL)
							TB = ReadMemory(RegHL.Word);
							TB = (byte)((TB << 7) | (TB >> 1));
							RegAF.Low = (byte)((TB & 0x80) >> 3);
							if (TB == 0) RegAF.Low |= 0x80;
							WriteMemory(RegHL.Word, TB);
							break;
						case 0x0F: // RRC A
							RegAF.High = (byte)((RegAF.High << 7) | (RegAF.High >> 1));
							RegAF.Low = (byte)((RegAF.High & 0x80) >> 3);
							if (RegAF.High == 0) RegAF.Low |= 0x80;
							break;
						case 0x10: // RL B
							TB = (byte)((RegBC.High & 0x80) >> 3);
							RegBC.High = (byte)((RegBC.High << 1) | (FlagC ? 1 : 0));
							RegAF.Low = TB;
							if (RegBC.High == 0) RegAF.Low |= 0x80;
							break;
						case 0x11: // RL C
							TB = (byte)((RegBC.Low & 0x80) >> 3);
							RegBC.Low = (byte)((RegBC.Low << 1) | (FlagC ? 1 : 0));
							RegAF.Low = TB;
							if (RegBC.Low == 0) RegAF.Low |= 0x80;
							break;
						case 0x12: // RL D
							TB = (byte)((RegDE.High & 0x80) >> 3);
							RegDE.High = (byte)((RegDE.High << 1) | (FlagC ? 1 : 0));
							RegAF.Low = TB;
							if (RegDE.High == 0) RegAF.Low |= 0x80;
							break;
						case 0x13: // RL E
							TB = (byte)((RegDE.Low & 0x80) >> 3);
							RegDE.Low = (byte)((RegDE.Low << 1) | (FlagC ? 1 : 0));
							RegAF.Low = TB;
							if (RegDE.Low == 0) RegAF.Low |= 0x80;
							break;
						case 0x14: // RL H
							TB = (byte)((RegHL.High & 0x80) >> 3);
							RegHL.High = (byte)((RegHL.High << 1) | (FlagC ? 1 : 0));
							RegAF.Low = TB;
							if (RegHL.High == 0) RegAF.Low |= 0x80;
							break;
						case 0x15: // RL L
							TB = (byte)((RegHL.Low & 0x80) >> 3);
							RegHL.Low = (byte)((RegHL.Low << 1) | (FlagC ? 1 : 0));
							RegAF.Low = TB;
							if (RegHL.Low == 0) RegAF.Low |= 0x80;
							break;
						case 0x16: // RL (HL)
							TB2 = ReadMemory(RegHL.Word);
							TB = (byte)((TB2 & 0x80) >> 3);
							TB2 = (byte)((TB2 << 1) | (FlagC ? 1 : 0));
							RegAF.Low = TB;
							if (TB2 == 0) RegAF.Low |= 0x80;
							WriteMemory(RegHL.Word, TB2);
							break;
						case 0x17: // RL A
							TB = (byte)((RegAF.High & 0x80) >> 3);
							RegAF.High = (byte)((RegAF.High << 1) | (FlagC ? 1 : 0));
							RegAF.Low = TB;
							if (RegAF.High == 0) RegAF.Low |= 0x80;
							break;
						case 0x18: // RR B
							TB = (byte)((RegBC.High & 0x1) << 4);
							RegBC.High = (byte)((RegBC.High >> 1) | (FlagC ? 0x80 : 0));
							RegAF.Low = TB;
							if (RegBC.High == 0) RegAF.Low |= 0x80;
							break;
						case 0x19: // RR C
							TB = (byte)((RegBC.Low & 0x1) << 4);
							RegBC.Low = (byte)((RegBC.Low >> 1) | (FlagC ? 0x80 : 0));
							RegAF.Low = TB;
							if (RegBC.Low == 0) RegAF.Low |= 0x80;
							break;
						case 0x1A: // RR D
							TB = (byte)((RegDE.High & 0x1) << 4);
							RegDE.High = (byte)((RegDE.High >> 1) | (FlagC ? 0x80 : 0));
							RegAF.Low = TB;
							if (RegDE.High == 0) RegAF.Low |= 0x80;
							break;
						case 0x1B: // RR E
							TB = (byte)((RegDE.Low & 0x1) << 4);
							RegDE.Low = (byte)((RegDE.Low >> 1) | (FlagC ? 0x80 : 0));
							RegAF.Low = TB;
							if (RegDE.Low == 0) RegAF.Low |= 0x80;
							break;
						case 0x1C: // RR H
							TB = (byte)((RegHL.High & 0x1) << 4);
							RegHL.High = (byte)((RegHL.High >> 1) | (FlagC ? 0x80 : 0));
							RegAF.Low = TB;
							if (RegHL.High == 0) RegAF.Low |= 0x80;
							break;
						case 0x1D: // RR L
							TB = (byte)((RegHL.Low & 0x1) << 4);
							RegHL.Low = (byte)((RegHL.Low >> 1) | (FlagC ? 0x80 : 0));
							RegAF.Low = TB;
							if (RegHL.Low == 0) RegAF.Low |= 0x80;
							break;
						case 0x1E: // RR (HL)
							TB2 = ReadMemory(RegHL.Word);
							TB = (byte)((TB2 & 0x1) << 4);
							TB2 = (byte)((TB2 >> 1) | (FlagC ? 0x80 : 0));
							RegAF.Low = TB;
							if (TB2 == 0) RegAF.Low |= 0x80;
							WriteMemory(RegHL.Word, TB2);
							break;
						case 0x1F: // RR A
							TB = (byte)((RegAF.High & 0x1) << 4);
							RegAF.High = (byte)((RegAF.High >> 1) | (FlagC ? 0x80 : 0));
							RegAF.Low = TB;
							if (RegAF.High == 0) RegAF.Low |= 0x80;
							break;
						case 0x20: // SLA B
							RegAF.Low = 0;
							if ((RegBC.High & 0x80) != 0) RegAF.Low |= 0x10;
							RegBC.High <<= 1;
							if (RegBC.High == 0) RegAF.Low |= 0x80;
							break;
						case 0x21: // SLA C
							RegAF.Low = 0;
							if ((RegBC.Low & 0x80) != 0) RegAF.Low |= 0x10;
							RegBC.Low <<= 1;
							if (RegBC.Low == 0) RegAF.Low |= 0x80;
							break;
						case 0x22: // SLA D
							RegAF.Low = 0;
							if ((RegDE.High & 0x80) != 0) RegAF.Low |= 0x10;
							RegDE.High <<= 1;
							if (RegDE.High == 0) RegAF.Low |= 0x80;
							break;
						case 0x23: // SLA E
							RegAF.Low = 0;
							if ((RegDE.Low & 0x80) != 0) RegAF.Low |= 0x10;
							RegDE.Low <<= 1;
							if (RegDE.Low == 0) RegAF.Low |= 0x80;
							break;
						case 0x24: // SLA H
							RegAF.Low = 0;
							if ((RegHL.High & 0x80) != 0) RegAF.Low |= 0x10;
							RegHL.High <<= 1;
							if (RegHL.High == 0) RegAF.Low |= 0x80;
							break;
						case 0x25: // SLA L
							RegAF.Low = 0;
							if ((RegHL.Low & 0x80) != 0) RegAF.Low |= 0x10;
							RegHL.Low <<= 1;
							if (RegHL.Low == 0) RegAF.Low |= 0x80;
							break;
						case 0x26: // SLA (HL)
							TB = ReadMemory(RegHL.Word);
							RegAF.Low = 0;
							if ((TB & 0x80) != 0) RegAF.Low |= 0x10;
							TB <<= 1;
							if (TB == 0) RegAF.Low |= 0x80;
							WriteMemory(RegHL.Word, TB);
							break;
						case 0x27: // SLA A
							RegAF.Low = 0;
							if ((RegAF.High & 0x80) != 0) RegAF.Low |= 0x10;
							RegAF.High <<= 1;
							if (RegAF.High == 0) RegAF.Low |= 0x80;
							break;
						case 0x28: // SRA B
							RegAF.Low = 0;
							if ((RegBC.High & 1) != 0) RegAF.Low |= 0x10;
							RegBC.High = (byte)((RegBC.High >> 1) | (RegBC.High & 0x80));
							if (RegBC.High == 0) RegAF.Low |= 0x80;
							break;
						case 0x29: // SRA C
							RegAF.Low = 0;
							if ((RegBC.Low & 1) != 0) RegAF.Low |= 0x10;
							RegBC.Low = (byte)((RegBC.Low >> 1) | (RegBC.Low & 0x80));
							if (RegBC.Low == 0) RegAF.Low |= 0x80;
							break;
						case 0x2A: // SRA D
							RegAF.Low = 0;
							if ((RegDE.High & 1) != 0) RegAF.Low |= 0x10;
							RegDE.High = (byte)((RegDE.High >> 1) | (RegDE.High & 0x80));
							if (RegDE.High == 0) RegAF.Low |= 0x80;
							break;
						case 0x2B: // SRA E
							RegAF.Low = 0;
							if ((RegDE.Low & 1) != 0) RegAF.Low |= 0x10;
							RegDE.Low = (byte)((RegDE.Low >> 1) | (RegDE.Low & 0x80));
							if (RegDE.Low == 0) RegAF.Low |= 0x80;
							break;
						case 0x2C: // SRA H
							RegAF.Low = 0;
							if ((RegHL.High & 1) != 0) RegAF.Low |= 0x10;
							RegHL.High = (byte)((RegHL.High >> 1) | (RegHL.High & 0x80));
							if (RegHL.High == 0) RegAF.Low |= 0x80;
							break;
						case 0x2D: // SRA L
							RegAF.Low = 0;
							if ((RegHL.Low & 1) != 0) RegAF.Low |= 0x10;
							RegHL.Low = (byte)((RegHL.Low >> 1) | (RegHL.Low & 0x80));
							if (RegHL.Low == 0) RegAF.Low |= 0x80;
							break;
						case 0x2E: // SRA (HL)
							TB = ReadMemory(RegHL.Word);
							RegAF.Low = 0;
							if ((TB & 1) != 0) RegAF.Low |= 0x10;
							TB = (byte)((TB >> 1) | (TB & 0x80));
							if (TB == 0) RegAF.Low |= 0x80;
							WriteMemory(RegHL.Word, TB);
							break;
						case 0x2F: // SRA A
							RegAF.Low = 0;
							if ((RegAF.High & 1) != 0) RegAF.Low |= 0x10;
							RegAF.High = (byte)((RegAF.High >> 1) | (RegAF.High & 0x80));
							if (RegAF.High == 0) RegAF.Low |= 0x80;
							break;
						case 0x30: // SWAP B
							RegBC.High = SwapTable[RegBC.High];
							FlagZ = (RegBC.High == 0);
							break;
						case 0x31: // SWAP C
							RegBC.Low = SwapTable[RegBC.Low];
							FlagZ = (RegBC.Low == 0);
							break;
						case 0x32: // SWAP D
							RegDE.High = SwapTable[RegDE.High];
							FlagZ = (RegDE.High == 0);
							break;
						case 0x33: // SWAP E
							RegDE.Low = SwapTable[RegDE.Low];
							FlagZ = (RegDE.Low == 0);
							break;
						case 0x34: // SWAP H
							RegHL.High = SwapTable[RegHL.High];
							FlagZ = (RegHL.High == 0);
							break;
						case 0x35: // SWAP L 
							RegHL.Low = SwapTable[RegHL.Low];
							FlagZ = (RegHL.Low == 0);
							break;
						case 0x36: // SWAP (HL)
							TB = SwapTable[ReadMemory(RegHL.Word)];
							WriteMemory(RegHL.Word, TB);
							FlagZ = (TB == 0);
							break;
						case 0x37: // SWAP A
							RegAF.High = SwapTable[RegAF.High];
							FlagZ = (RegAF.High == 0);
							break;
						case 0x38: // SRL B
							RegAF.Low = 0;
							if ((RegBC.High & 1) != 0) RegAF.Low |= 0x10;
							RegBC.High >>= 1;
							if (RegBC.High == 0) RegAF.Low |= 0x80;
							break;
						case 0x39: // SRL C
							RegAF.Low = 0;
							if ((RegBC.Low & 1) != 0) RegAF.Low |= 0x10;
							RegBC.Low >>= 1;
							if (RegBC.Low == 0) RegAF.Low |= 0x80;
							break;
						case 0x3A: // SRL D
							RegAF.Low = 0;
							if ((RegDE.High & 1) != 0) RegAF.Low |= 0x10;
							RegDE.High >>= 1;
							if (RegDE.High == 0) RegAF.Low |= 0x80;
							break;
						case 0x3B: // SRL E
							RegAF.Low = 0;
							if ((RegDE.Low & 1) != 0) RegAF.Low |= 0x10;
							RegDE.Low >>= 1;
							if (RegDE.Low == 0) RegAF.Low |= 0x80;
							break;
						case 0x3C: // SRL H
							RegAF.Low = 0;
							if ((RegHL.High & 1) != 0) RegAF.Low |= 0x10;
							RegHL.High >>= 1;
							if (RegHL.High == 0) RegAF.Low |= 0x80;
							break;
						case 0x3D: // SRL L
							RegAF.Low = 0;
							if ((RegHL.Low & 1) != 0) RegAF.Low |= 0x10;
							RegHL.Low >>= 1;
							if (RegHL.Low == 0) RegAF.Low |= 0x80;
							break;
						case 0x3E: // SRL (HL)
							TB = ReadMemory(RegHL.Word);
							RegAF.Low = 0;
							if ((TB & 1) != 0) RegAF.Low |= 0x10;
							TB >>= 1;
							if (TB == 0) RegAF.Low |= 0x80;
							WriteMemory(RegHL.Word, TB);
							break;
						case 0x3F: // SRL A
							RegAF.Low = 0;
							if ((RegAF.High & 1) != 0) RegAF.Low |= 0x10;
							RegAF.High >>= 1;
							if (RegAF.High == 0) RegAF.Low |= 0x80;
							break;
						case 0x40: // BIT 0, B
							FlagZ = (RegBC.High & 0x01) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x41: // BIT 0, C
							FlagZ = (RegBC.Low & 0x01) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x42: // BIT 0, D
							FlagZ = (RegDE.High & 0x01) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x43: // BIT 0, E
							FlagZ = (RegDE.Low & 0x01) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x44: // BIT 0, H
							FlagZ = (RegHL.High & 0x01) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x45: // BIT 0, L
							FlagZ = (RegHL.Low & 0x01) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x46: // BIT 0, (HL)
							FlagZ = (ReadMemory(RegHL.Word) & 0x01) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x47: // BIT 0, A
							FlagZ = (RegAF.High & 0x01) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x48: // BIT 1, B
							FlagZ = (RegBC.High & 0x02) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x49: // BIT 1, C
							FlagZ = (RegBC.Low & 0x02) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x4A: // BIT 1, D
							FlagZ = (RegDE.High & 0x02) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x4B: // BIT 1, E
							FlagZ = (RegDE.Low & 0x02) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x4C: // BIT 1, H
							FlagZ = (RegHL.High & 0x02) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x4D: // BIT 1, L
							FlagZ = (RegHL.Low & 0x02) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x4E: // BIT 1, (HL)
							FlagZ = (ReadMemory(RegHL.Word) & 0x02) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x4F: // BIT 1, A
							FlagZ = (RegAF.High & 0x02) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x50: // BIT 2, B
							FlagZ = (RegBC.High & 0x04) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x51: // BIT 2, C
							FlagZ = (RegBC.Low & 0x04) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x52: // BIT 2, D
							FlagZ = (RegDE.High & 0x04) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x53: // BIT 2, E
							FlagZ = (RegDE.Low & 0x04) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x54: // BIT 2, H
							FlagZ = (RegHL.High & 0x04) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x55: // BIT 2, L
							FlagZ = (RegHL.Low & 0x04) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x56: // BIT 2, (HL)
							FlagZ = (ReadMemory(RegHL.Word) & 0x04) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x57: // BIT 2, A
							FlagZ = (RegAF.High & 0x04) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x58: // BIT 3, B
							FlagZ = (RegBC.High & 0x08) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x59: // BIT 3, C
							FlagZ = (RegBC.Low & 0x08) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x5A: // BIT 3, D
							FlagZ = (RegDE.High & 0x08) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x5B: // BIT 3, E
							FlagZ = (RegDE.Low & 0x08) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x5C: // BIT 3, H
							FlagZ = (RegHL.High & 0x08) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x5D: // BIT 3, L
							FlagZ = (RegHL.Low & 0x08) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x5E: // BIT 3, (HL)
							FlagZ = (ReadMemory(RegHL.Word) & 0x08) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x5F: // BIT 3, A
							FlagZ = (RegAF.High & 0x08) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x60: // BIT 4, B
							FlagZ = (RegBC.High & 0x10) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x61: // BIT 4, C
							FlagZ = (RegBC.Low & 0x10) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x62: // BIT 4, D
							FlagZ = (RegDE.High & 0x10) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x63: // BIT 4, E
							FlagZ = (RegDE.Low & 0x10) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x64: // BIT 4, H
							FlagZ = (RegHL.High & 0x10) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x65: // BIT 4, L
							FlagZ = (RegHL.Low & 0x10) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x66: // BIT 4, (HL)
							FlagZ = (ReadMemory(RegHL.Word) & 0x10) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x67: // BIT 4, A
							FlagZ = (RegAF.High & 0x10) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x68: // BIT 5, B
							FlagZ = (RegBC.High & 0x20) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x69: // BIT 5, C
							FlagZ = (RegBC.Low & 0x20) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x6A: // BIT 5, D
							FlagZ = (RegDE.High & 0x20) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x6B: // BIT 5, E
							FlagZ = (RegDE.Low & 0x20) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x6C: // BIT 5, H
							FlagZ = (RegHL.High & 0x20) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x6D: // BIT 5, L
							FlagZ = (RegHL.Low & 0x20) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x6E: // BIT 5, (HL)
							FlagZ = (ReadMemory(RegHL.Word) & 0x20) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x6F: // BIT 5, A
							FlagZ = (RegAF.High & 0x20) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x70: // BIT 6, B
							FlagZ = (RegBC.High & 0x40) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x71: // BIT 6, C
							FlagZ = (RegBC.Low & 0x40) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x72: // BIT 6, D
							FlagZ = (RegDE.High & 0x40) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x73: // BIT 6, E
							FlagZ = (RegDE.Low & 0x40) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x74: // BIT 6, H
							FlagZ = (RegHL.High & 0x40) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x75: // BIT 6, L
							FlagZ = (RegHL.Low & 0x40) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x76: // BIT 6, (HL)
							FlagZ = (ReadMemory(RegHL.Word) & 0x40) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x77: // BIT 6, A
							FlagZ = (RegAF.High & 0x40) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x78: // BIT 7, B
							FlagZ = (RegBC.High & 0x80) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x79: // BIT 7, C
							FlagZ = (RegBC.Low & 0x80) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x7A: // BIT 7, D
							FlagZ = (RegDE.High & 0x80) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x7B: // BIT 7, E
							FlagZ = (RegDE.Low & 0x80) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x7C: // BIT 7, H
							FlagZ = (RegHL.High & 0x80) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x7D: // BIT 7, L
							FlagZ = (RegHL.Low & 0x80) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x7E: // BIT 7, (HL)
							FlagZ = (ReadMemory(RegHL.Word) & 0x80) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x7F: // BIT 7, A
							FlagZ = (RegAF.High & 0x80) == 0;
							FlagH = true;
							FlagN = false;
							break;
						case 0x80: // RES 0, B
							RegBC.High &= unchecked((byte)~0x01);
							break;
						case 0x81: // RES 0, C
							RegBC.Low &= unchecked((byte)~0x01);
							break;
						case 0x82: // RES 0, D
							RegDE.High &= unchecked((byte)~0x01);
							break;
						case 0x83: // RES 0, E
							RegDE.Low &= unchecked((byte)~0x01);
							break;
						case 0x84: // RES 0, H
							RegHL.High &= unchecked((byte)~0x01);
							break;
						case 0x85: // RES 0, L
							RegHL.Low &= unchecked((byte)~0x01);
							break;
						case 0x86: // RES 0, (HL)
							WriteMemory(RegHL.Word, (byte)(ReadMemory(RegHL.Word) & unchecked((byte)~0x01)));
							break;
						case 0x87: // RES 0, A
							RegAF.High &= unchecked((byte)~0x01);
							break;
						case 0x88: // RES 1, B
							RegBC.High &= unchecked((byte)~0x02);
							break;
						case 0x89: // RES 1, C
							RegBC.Low &= unchecked((byte)~0x02);
							break;
						case 0x8A: // RES 1, D
							RegDE.High &= unchecked((byte)~0x02);
							break;
						case 0x8B: // RES 1, E
							RegDE.Low &= unchecked((byte)~0x02);
							break;
						case 0x8C: // RES 1, H
							RegHL.High &= unchecked((byte)~0x02);
							break;
						case 0x8D: // RES 1, L
							RegHL.Low &= unchecked((byte)~0x02);
							break;
						case 0x8E: // RES 1, (HL)
							WriteMemory(RegHL.Word, (byte)(ReadMemory(RegHL.Word) & unchecked((byte)~0x02)));
							break;
						case 0x8F: // RES 1, A
							RegAF.High &= unchecked((byte)~0x02);
							break;
						case 0x90: // RES 2, B
							RegBC.High &= unchecked((byte)~0x04);
							break;
						case 0x91: // RES 2, C
							RegBC.Low &= unchecked((byte)~0x04);
							break;
						case 0x92: // RES 2, D
							RegDE.High &= unchecked((byte)~0x04);
							break;
						case 0x93: // RES 2, E
							RegDE.Low &= unchecked((byte)~0x04);
							break;
						case 0x94: // RES 2, H
							RegHL.High &= unchecked((byte)~0x04);
							break;
						case 0x95: // RES 2, L
							RegHL.Low &= unchecked((byte)~0x04);
							break;
						case 0x96: // RES 2, (HL)
							WriteMemory(RegHL.Word, (byte)(ReadMemory(RegHL.Word) & unchecked((byte)~0x04)));
							break;
						case 0x97: // RES 2, A
							RegAF.High &= unchecked((byte)~0x04);
							break;
						case 0x98: // RES 3, B
							RegBC.High &= unchecked((byte)~0x08);
							break;
						case 0x99: // RES 3, C
							RegBC.Low &= unchecked((byte)~0x08);
							break;
						case 0x9A: // RES 3, D
							RegDE.High &= unchecked((byte)~0x08);
							break;
						case 0x9B: // RES 3, E
							RegDE.Low &= unchecked((byte)~0x08);
							break;
						case 0x9C: // RES 3, H
							RegHL.High &= unchecked((byte)~0x08);
							break;
						case 0x9D: // RES 3, L
							RegHL.Low &= unchecked((byte)~0x08);
							break;
						case 0x9E: // RES 3, (HL)
							WriteMemory(RegHL.Word, (byte)(ReadMemory(RegHL.Word) & unchecked((byte)~0x08)));
							break;
						case 0x9F: // RES 3, A
							RegAF.High &= unchecked((byte)~0x08);
							break;
						case 0xA0: // RES 4, B
							RegBC.High &= unchecked((byte)~0x10);
							break;
						case 0xA1: // RES 4, C
							RegBC.Low &= unchecked((byte)~0x10);
							break;
						case 0xA2: // RES 4, D
							RegDE.High &= unchecked((byte)~0x10);
							break;
						case 0xA3: // RES 4, E
							RegDE.Low &= unchecked((byte)~0x10);
							break;
						case 0xA4: // RES 4, H
							RegHL.High &= unchecked((byte)~0x10);
							break;
						case 0xA5: // RES 4, L
							RegHL.Low &= unchecked((byte)~0x10);
							break;
						case 0xA6: // RES 4, (HL)
							WriteMemory(RegHL.Word, (byte)(ReadMemory(RegHL.Word) & unchecked((byte)~0x10)));
							break;
						case 0xA7: // RES 4, A
							RegAF.High &= unchecked((byte)~0x10);
							break;
						case 0xA8: // RES 5, B
							RegBC.High &= unchecked((byte)~0x20);
							break;
						case 0xA9: // RES 5, C
							RegBC.Low &= unchecked((byte)~0x20);
							break;
						case 0xAA: // RES 5, D
							RegDE.High &= unchecked((byte)~0x20);
							break;
						case 0xAB: // RES 5, E
							RegDE.Low &= unchecked((byte)~0x20);
							break;
						case 0xAC: // RES 5, H
							RegHL.High &= unchecked((byte)~0x20);
							break;
						case 0xAD: // RES 5, L
							RegHL.Low &= unchecked((byte)~0x20);
							break;
						case 0xAE: // RES 5, (HL)
							WriteMemory(RegHL.Word, (byte)(ReadMemory(RegHL.Word) & unchecked((byte)~0x20)));
							break;
						case 0xAF: // RES 5, A
							RegAF.High &= unchecked((byte)~0x20);
							break;
						case 0xB0: // RES 6, B
							RegBC.High &= unchecked((byte)~0x40);
							break;
						case 0xB1: // RES 6, C
							RegBC.Low &= unchecked((byte)~0x40);
							break;
						case 0xB2: // RES 6, D
							RegDE.High &= unchecked((byte)~0x40);
							break;
						case 0xB3: // RES 6, E
							RegDE.Low &= unchecked((byte)~0x40);
							break;
						case 0xB4: // RES 6, H
							RegHL.High &= unchecked((byte)~0x40);
							break;
						case 0xB5: // RES 6, L
							RegHL.Low &= unchecked((byte)~0x40);
							break;
						case 0xB6: // RES 6, (HL)
							WriteMemory(RegHL.Word, (byte)(ReadMemory(RegHL.Word) & unchecked((byte)~0x40)));
							break;
						case 0xB7: // RES 6, A
							RegAF.High &= unchecked((byte)~0x40);
							break;
						case 0xB8: // RES 7, B
							RegBC.High &= unchecked((byte)~0x80);
							break;
						case 0xB9: // RES 7, C
							RegBC.Low &= unchecked((byte)~0x80);
							break;
						case 0xBA: // RES 7, D
							RegDE.High &= unchecked((byte)~0x80);
							break;
						case 0xBB: // RES 7, E
							RegDE.Low &= unchecked((byte)~0x80);
							break;
						case 0xBC: // RES 7, H
							RegHL.High &= unchecked((byte)~0x80);
							break;
						case 0xBD: // RES 7, L
							RegHL.Low &= unchecked((byte)~0x80);
							break;
						case 0xBE: // RES 7, (HL)
							WriteMemory(RegHL.Word, (byte)(ReadMemory(RegHL.Word) & unchecked((byte)~0x80)));
							break;
						case 0xBF: // RES 7, A
							RegAF.High &= unchecked((byte)~0x80);
							break;
						case 0xC0: // SET 0, B
							RegBC.High |= unchecked(0x01);
							break;
						case 0xC1: // SET 0, C
							RegBC.Low |= unchecked(0x01);
							break;
						case 0xC2: // SET 0, D
							RegDE.High |= unchecked(0x01);
							break;
						case 0xC3: // SET 0, E
							RegDE.Low |= unchecked(0x01);
							break;
						case 0xC4: // SET 0, H
							RegHL.High |= unchecked(0x01);
							break;
						case 0xC5: // SET 0, L
							RegHL.Low |= unchecked(0x01);
							break;
						case 0xC6: // SET 0, (HL)
							WriteMemory(RegHL.Word, (byte)(ReadMemory(RegHL.Word) | unchecked(0x01)));
							break;
						case 0xC7: // SET 0, A
							RegAF.High |= unchecked(0x01);
							break;
						case 0xC8: // SET 1, B
							RegBC.High |= unchecked(0x02);
							break;
						case 0xC9: // SET 1, C
							RegBC.Low |= unchecked(0x02);
							break;
						case 0xCA: // SET 1, D
							RegDE.High |= unchecked(0x02);
							break;
						case 0xCB: // SET 1, E
							RegDE.Low |= unchecked(0x02);
							break;
						case 0xCC: // SET 1, H
							RegHL.High |= unchecked(0x02);
							break;
						case 0xCD: // SET 1, L
							RegHL.Low |= unchecked(0x02);
							break;
						case 0xCE: // SET 1, (HL)
							WriteMemory(RegHL.Word, (byte)(ReadMemory(RegHL.Word) | unchecked(0x02)));
							break;
						case 0xCF: // SET 1, A
							RegAF.High |= unchecked(0x02);
							break;
						case 0xD0: // SET 2, B
							RegBC.High |= unchecked(0x04);
							break;
						case 0xD1: // SET 2, C
							RegBC.Low |= unchecked(0x04);
							break;
						case 0xD2: // SET 2, D
							RegDE.High |= unchecked(0x04);
							break;
						case 0xD3: // SET 2, E
							RegDE.Low |= unchecked(0x04);
							break;
						case 0xD4: // SET 2, H
							RegHL.High |= unchecked(0x04);
							break;
						case 0xD5: // SET 2, L
							RegHL.Low |= unchecked(0x04);
							break;
						case 0xD6: // SET 2, (HL)
							WriteMemory(RegHL.Word, (byte)(ReadMemory(RegHL.Word) | unchecked(0x04)));
							break;
						case 0xD7: // SET 2, A
							RegAF.High |= unchecked(0x04);
							break;
						case 0xD8: // SET 3, B
							RegBC.High |= unchecked(0x08);
							break;
						case 0xD9: // SET 3, C
							RegBC.Low |= unchecked(0x08);
							break;
						case 0xDA: // SET 3, D
							RegDE.High |= unchecked(0x08);
							break;
						case 0xDB: // SET 3, E
							RegDE.Low |= unchecked(0x08);
							break;
						case 0xDC: // SET 3, H
							RegHL.High |= unchecked(0x08);
							break;
						case 0xDD: // SET 3, L
							RegHL.Low |= unchecked(0x08);
							break;
						case 0xDE: // SET 3, (HL)
							WriteMemory(RegHL.Word, (byte)(ReadMemory(RegHL.Word) | unchecked(0x08)));
							break;
						case 0xDF: // SET 3, A
							RegAF.High |= unchecked(0x08);
							break;
						case 0xE0: // SET 4, B
							RegBC.High |= unchecked(0x10);
							break;
						case 0xE1: // SET 4, C
							RegBC.Low |= unchecked(0x10);
							break;
						case 0xE2: // SET 4, D
							RegDE.High |= unchecked(0x10);
							break;
						case 0xE3: // SET 4, E
							RegDE.Low |= unchecked(0x10);
							break;
						case 0xE4: // SET 4, H
							RegHL.High |= unchecked(0x10);
							break;
						case 0xE5: // SET 4, L
							RegHL.Low |= unchecked(0x10);
							break;
						case 0xE6: // SET 4, (HL)
							WriteMemory(RegHL.Word, (byte)(ReadMemory(RegHL.Word) | unchecked(0x10)));
							break;
						case 0xE7: // SET 4, A
							RegAF.High |= unchecked(0x10);
							break;
						case 0xE8: // SET 5, B
							RegBC.High |= unchecked(0x20);
							break;
						case 0xE9: // SET 5, C
							RegBC.Low |= unchecked(0x20);
							break;
						case 0xEA: // SET 5, D
							RegDE.High |= unchecked(0x20);
							break;
						case 0xEB: // SET 5, E
							RegDE.Low |= unchecked(0x20);
							break;
						case 0xEC: // SET 5, H
							RegHL.High |= unchecked(0x20);
							break;
						case 0xED: // SET 5, L
							RegHL.Low |= unchecked(0x20);
							break;
						case 0xEE: // SET 5, (HL)
							WriteMemory(RegHL.Word, (byte)(ReadMemory(RegHL.Word) | unchecked(0x20)));
							break;
						case 0xEF: // SET 5, A
							RegAF.High |= unchecked(0x20);
							break;
						case 0xF0: // SET 6, B
							RegBC.High |= unchecked(0x40);
							break;
						case 0xF1: // SET 6, C
							RegBC.Low |= unchecked(0x40);
							break;
						case 0xF2: // SET 6, D
							RegDE.High |= unchecked(0x40);
							break;
						case 0xF3: // SET 6, E
							RegDE.Low |= unchecked(0x40);
							break;
						case 0xF4: // SET 6, H
							RegHL.High |= unchecked(0x40);
							break;
						case 0xF5: // SET 6, L
							RegHL.Low |= unchecked(0x40);
							break;
						case 0xF6: // SET 6, (HL)
							WriteMemory(RegHL.Word, (byte)(ReadMemory(RegHL.Word) | unchecked(0x40)));
							break;
						case 0xF7: // SET 6, A
							RegAF.High |= unchecked(0x40);
							break;
						case 0xF8: // SET 7, B
							RegBC.High |= unchecked(0x80);
							break;
						case 0xF9: // SET 7, C
							RegBC.Low |= unchecked(0x80);
							break;
						case 0xFA: // SET 7, D
							RegDE.High |= unchecked(0x80);
							break;
						case 0xFB: // SET 7, E
							RegDE.Low |= unchecked(0x80);
							break;
						case 0xFC: // SET 7, H
							RegHL.High |= unchecked(0x80);
							break;
						case 0xFD: // SET 7, L
							RegHL.Low |= unchecked(0x80);
							break;
						case 0xFE: // SET 7, (HL)
							WriteMemory(RegHL.Word, (byte)(ReadMemory(RegHL.Word) | unchecked(0x80)));
							break;
						case 0xFF: // SET 7, A
							RegAF.High |= unchecked(0x80);
							break;
					}
					break;
				case 0xCC: // CALL Z, nn
					TUS = (ushort)(ReadMemory(RegPC.Word++) + ReadMemory(RegPC.Word++) * 256);
					if (FlagZ)
					{
						WriteMemory(--RegSP.Word, RegPC.High); WriteMemory(--RegSP.Word, RegPC.Low);
						RegPC.Word = TUS;
					}
					break;
				case 0xCD: // CALL nn
					TUS = (ushort)(ReadMemory(RegPC.Word++) + ReadMemory(RegPC.Word++) * 256);
					WriteMemory(--RegSP.Word, RegPC.High); WriteMemory(--RegSP.Word, RegPC.Low);
					RegPC.Word = TUS;
					break;
				case 0xCE: // ADC n
					TB = ReadMemory(RegPC.Word++);
					TI2 = FlagC ? 1 : 0;
					TI1 = RegAF.High + TB + TI2;
					RegAF.Low = 0;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 > 0xFF) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) + (TB & 0x0F) + TI2 > 0x0F) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0xCF: // RST $08
					WriteMemory(--RegSP.Word, RegPC.High); WriteMemory(--RegSP.Word, RegPC.Low);
					RegPC.Word = 0x08;
					break;
				case 0xD0: // RET NC
					if (!FlagC)
						RegPC.Low = ReadMemory(RegSP.Word++); RegPC.High = ReadMemory(RegSP.Word++);
					break;
				case 0xD1: // POP DE
					RegDE.Low = ReadMemory(RegSP.Word++); RegDE.High = ReadMemory(RegSP.Word++);
					break;
				case 0xD2: // JP NC, nn
					TUS = (ushort)(ReadMemory(RegPC.Word++) + ReadMemory(RegPC.Word++) * 256);
					if (!FlagC)
						RegPC.Word = TUS;
					break;
				case 0xD3: // NOP
					break;
				case 0xD4: // CALL NC, nn
					TUS = (ushort)(ReadMemory(RegPC.Word++) + ReadMemory(RegPC.Word++) * 256);
					if (!FlagC)
					{
						WriteMemory(--RegSP.Word, RegPC.High); WriteMemory(--RegSP.Word, RegPC.Low);
						RegPC.Word = TUS;
					}
					break;
				case 0xD5: // PUSH DE
					WriteMemory(--RegSP.Word, RegDE.High); WriteMemory(--RegSP.Word, RegDE.Low);
					break;
				case 0xD6: // SUB n
					TB = ReadMemory(RegPC.Word++);
					TI1 = RegAF.High - TB;
					RegAF.Low = 0x40;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 < 0) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) - (TB & 0x0F) < 0) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0xD7: // RST $10
					WriteMemory(--RegSP.Word, RegPC.High); WriteMemory(--RegSP.Word, RegPC.Low);
					RegPC.Word = 0x10;
					break;
				case 0xD8: // RET C
					if (FlagC)
						RegPC.Low = ReadMemory(RegSP.Word++); RegPC.High = ReadMemory(RegSP.Word++);
					break;
				case 0xD9: // RETI
					RegPC.Low = ReadMemory(RegSP.Word++); RegPC.High = ReadMemory(RegSP.Word++);
					// TODO Nothing else special needs to be done?
					break;
				case 0xDA: // JP C, nn
					TUS = (ushort)(ReadMemory(RegPC.Word++) + ReadMemory(RegPC.Word++) * 256);
					if (FlagC)
						RegPC.Word = TUS;
					break;
				case 0xDB: // NOP
					break;
				case 0xDC: // CALL C, nn
					TUS = (ushort)(ReadMemory(RegPC.Word++) + ReadMemory(RegPC.Word++) * 256);
					if (FlagC)
					{
						WriteMemory(--RegSP.Word, RegPC.High); WriteMemory(--RegSP.Word, RegPC.Low);
						RegPC.Word = TUS;
					}
					break;
				case 0xDD: // NOP
					break;
				case 0xDE: // SBC A, n
					TB = ReadMemory(RegPC.Word++);
					TI2 = FlagC ? 1 : 0;
					TI1 = RegAF.High - TB - TI2;
					RegAF.Low = 0x40;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 < 0) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) - (TB & 0x0F) - TI2 < 0) RegAF.Low |= 0x20;
					RegAF.High = (byte)TI1;
					break;
				case 0xDF: // RST $18
					WriteMemory(--RegSP.Word, RegPC.High); WriteMemory(--RegSP.Word, RegPC.Low);
					RegPC.Word = 0x18;
					break;
				case 0xE0: // LD ($FF00+nn), A
					WriteMemory((ushort)(0xFF00 + ReadMemory(RegPC.Word++)), RegAF.High);
					break;
				case 0xE1: // POP HL
					RegHL.Low = ReadMemory(RegSP.Word++); RegHL.High = ReadMemory(RegSP.Word++);
					break;
				case 0xE2: // LD ($FF00+C), A
					WriteMemory((ushort)(0xFF00 + RegBC.Low), RegAF.High);
					break;
				case 0xE3: // NOP
					break;
				case 0xE4: // NOP
					break;
				case 0xE5: // PUSH HL
					WriteMemory(--RegSP.Word, RegHL.High); WriteMemory(--RegSP.Word, RegHL.Low);
					break;
				case 0xE6: // AND n
					RegAF.High &= ReadMemory(RegPC.Word++);
					RegAF.Low = (byte)(RegAF.High == 0 ? 0xA0 : 0x20);
					break;
				case 0xE7: // RST $20
					WriteMemory(--RegSP.Word, RegPC.High); WriteMemory(--RegSP.Word, RegPC.Low);
					RegPC.Word = 0x20;
					break;
				case 0xE8: // ADD SP, n
					Console.WriteLine("E8 : ADD SP, n being executed. verify correctness");
					TSB = (sbyte)ReadMemory(RegPC.Word++);
					RegAF.Low = 0;
					if (RegSP.Word + TSB > 0xFFFF) RegAF.Low |= 0x10;
					if (((RegSP.Word & 0xFFF) + TSB) > 0xFFF) RegAF.Low |= 0x20;
					RegSP.Word = (ushort)(RegSP.Word + TSB);
					break;
				case 0xE9: // JP HL
					RegPC.Word = RegHL.Word;
					break;
				case 0xEA: // LD (imm), A
					TUS = (ushort)(ReadMemory(RegPC.Word++) | (ReadMemory(RegPC.Word++) << 8));
					WriteMemory(TUS, RegAF.High);
					break;
				case 0xEB: // NOP
					break;
				case 0xEC: // NOP
					break;
				case 0xED: // NOP
					break;
				case 0xEE: // XOR n
					RegAF.High ^= ReadMemory(RegPC.Word++);
					RegAF.Low = (byte)(RegAF.High == 0 ? 0x80 : 0);
					break;
				case 0xEF: // RST $28
					WriteMemory(--RegSP.Word, RegPC.High); WriteMemory(--RegSP.Word, RegPC.Low);
					RegPC.Word = 0x28;
					break;
				case 0xF0: // LD A, ($FF00+nn)
					RegAF.High = ReadMemory((ushort)(0xFF00 + ReadMemory(RegPC.Word++)));
					break;
				case 0xF1: // POP AF
					RegAF.Low = ReadMemory(RegSP.Word++); RegAF.High = ReadMemory(RegSP.Word++);
					break;
				case 0xF2: // LD A, ($FF00+C)
					RegAF.High = ReadMemory((ushort)(0xFF00 + RegBC.Low));
					break;
				case 0xF3: // DI
					IFF1 = IFF2 = false;
					break;
				case 0xF4: // NOP
					break;
				case 0xF5: // PUSH AF
					WriteMemory(--RegSP.Word, RegAF.High); WriteMemory(--RegSP.Word, RegAF.Low);
					break;
				case 0xF6: // OR n
					RegAF.High |= ReadMemory(RegPC.Word++);
					RegAF.Low = (byte)(RegAF.High == 0 ? 0x80 : 0);
					break;
				case 0xF7: // RST $30
					WriteMemory(--RegSP.Word, RegPC.High); WriteMemory(--RegSP.Word, RegPC.Low);
					RegPC.Word = 0x30;
					break;
				case 0xF8: // LD HL, SP+nn
					Console.WriteLine("F8 : LD HL, SP+n being executed. verify correctness");
					TSB = (sbyte)ReadMemory(RegPC.Word++);
					RegAF.Low = 0;
					if (RegSP.Word + TSB > 0xFFFF) RegAF.Low |= 0x10;
					if (((RegSP.Word & 0xFFF) + TSB) > 0xFFF) RegAF.Low |= 0x20;
					RegHL.Word = (ushort)(RegSP.Word + TSB);
					break;
				case 0xF9: // LD SP, HL
					RegSP.Word = RegHL.Word;
					break;
				case 0xFA: // LD A, (nnnn)
					TUS = (ushort)(ReadMemory(RegPC.Word++) | (ReadMemory(RegPC.Word++) << 8));
					RegAF.High = ReadMemory(TUS);
					break;
				case 0xFB: // EI
					IFF1 = IFF2 = true;
					Interruptable = false;
					break;
				case 0xFC: // NOP
					break;
				case 0xFD: // NOP
					break;
				case 0xFE: // CP n
					TB = ReadMemory(RegPC.Word++);
					TI1 = RegAF.High - TB;
					RegAF.Low = 0x40;
					if ((byte)TI1 == 0) RegAF.Low |= 0x80;
					if (TI1 < 0) RegAF.Low |= 0x10;
					if ((RegAF.High & 0x0F) - (TB & 0x0F) < 0) RegAF.Low |= 0x20;
					break;
				case 0xFF: // RST $38
					WriteMemory(--RegSP.Word, RegPC.High); WriteMemory(--RegSP.Word, RegPC.Low);
					RegPC.Word = 0x38;
					break;
				default: throw new Exception("unhandled opcode");
			}
			LogData();
		}

		void CheckIrq()
		{
			if (nonMaskableInterruptPending)
			{
				halted = false;

				PendingCycles -= 3;
				TotalExecutedCycles += 3;
				nonMaskableInterruptPending = false;

				iff2 = iff1;
				iff1 = false;

				WriteMemory(--RegSP.Word, RegPC.High); WriteMemory(--RegSP.Word, RegPC.Low);
				RegPC.Word = 0x66;
			}
			else if (iff1 && interrupt && Interruptable)
			{
				Halted = false;

				iff1 = iff2 = false;

				switch (interruptMode)
				{
					case 0:
						PendingCycles -= 4;
						TotalExecutedCycles += 4;
						break;
					case 1:
						WriteMemory(--RegSP.Word, RegPC.High); WriteMemory(--RegSP.Word, RegPC.Low);
						RegPC.Word = 0x38;
						PendingCycles -= 4;
						TotalExecutedCycles += 4;
						break;
					case 2:
						ushort TUS = (ushort)(RegI * 256 + 0);
						WriteMemory(--RegSP.Word, RegPC.High); WriteMemory(--RegSP.Word, RegPC.Low);
						RegPC.Low = ReadMemory(TUS++); RegPC.High = ReadMemory(TUS);
						PendingCycles -= 5;
						TotalExecutedCycles += 5;
						break;
				}
			}
		}

		public void SingleStepInto()
		{
			if (halted) return;
			ExecuteInstruction();
			CheckIrq();
		}

		public void ExecuteCycles(int cycles)
		{
			PendingCycles += cycles;

			while (PendingCycles > 0)
			{
				Interruptable = true;

				if (halted)
				{
					PendingCycles -= 1;
				}
				else
				{
					ExecuteInstruction();
				}

				CheckIrq();
			}
		}
	}
}