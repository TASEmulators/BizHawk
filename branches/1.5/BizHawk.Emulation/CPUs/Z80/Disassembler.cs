//http://www.zophar.net/fileuploads/2/10819kouzv/z80undoc.html

//TODO: ex. (IX+00h) could be turned into (IX)

//usage:
//VgMuseum.Z80.Disassembler disasm = new Disassembler();
//ushort pc = RegPC.Word;
//string str = disasm.Disassemble(() => ReadMemory(pc++));
//Console.WriteLine(str);

//please note that however much youre tempted to, timings can't be put in a table here because they depend on how the instruction executes at runtime

using System;

namespace BizHawk.Emulation.CPUs.Z80 
{
	public class Disassembler
	{
		readonly static sbyte[,] opcodeSizes = new sbyte[7, 256];

		public static void GenerateOpcodeSizes()
		{
			Disassembler disasm = new Disassembler();

			for (int i = 0; i < 256; i++)
			{
				int pc = 0;
				byte[] opcode = { (byte)i, 0, 0, 0 };
				disasm.Disassemble(() => opcode[pc++]);
				opcodeSizes[0,i] = (sbyte)pc;
			}

			opcodeSizes[0, 0xCB] = -1;
			opcodeSizes[0, 0xED] = -2;
			opcodeSizes[0, 0xDD] = -3;
			opcodeSizes[0, 0xFD] = -4;

			for (int i = 0; i < 256; i++)
			{
				int pc = 0;
				byte[] opcode = { 0xCB, (byte)i, 0, 0, 0 };
				disasm.Disassemble(() => opcode[pc++]);
				opcodeSizes[1, i] = (sbyte)pc;
			}

			for (int i = 0; i < 256; i++)
			{
				int pc = 0;
				byte[] opcode = { 0xED, (byte)i, 0, 0, 0 };
				disasm.Disassemble(() => opcode[pc++]);
				opcodeSizes[2, i] = (sbyte)pc;
			}

			for (int i = 0; i < 256; i++)
			{
				int pc = 0;
				byte[] opcode = { 0xDD, (byte)i, 0, 0, 0 };
				disasm.Disassemble(() => opcode[pc++]);
				opcodeSizes[3, i] = (sbyte)pc;
			}

			opcodeSizes[3, 0xCB] = -5;
			opcodeSizes[3, 0xED] = -2;

			for (int i = 0; i < 256; i++)
			{
				int pc = 0;
				byte[] opcode = { 0xFD, (byte)i, 0, 0, 0 };
				disasm.Disassemble(() => opcode[pc++]);
				opcodeSizes[4, i] = (sbyte)pc;
			}

			opcodeSizes[3, 0xCB] = -6;
			opcodeSizes[3, 0xED] = -2;


			for (int i = 0; i < 256; i++)
			{
				int pc = 0;
				byte[] opcode = { 0xDD, 0xCB, (byte)i, 0, 0, 0 };
				disasm.Disassemble(() => opcode[pc++]);
				opcodeSizes[5, i] = (sbyte)pc;
			}

			for (int i = 0; i < 256; i++)
			{
				int pc = 0;
				byte[] opcode = { 0xFD, 0xCB, (byte)i, 0, 0, 0 };
				disasm.Disassemble(() => opcode[pc++]);
				opcodeSizes[6, i] = (sbyte)pc;
			}
		}

		static string Result(string format, Func<byte> read)
		{
			//d immediately succeeds the opcode
			//n immediate succeeds the opcode and the displacement (if present)
			//nn immediately succeeds the opcode and the displacement (if present)
			if (format.IndexOf("nn") != -1)
			{
				byte B = read();
				byte C = read();
				format = format.Replace("nn", string.Format("{0:X4}h", B + C * 256));
			}

			if (format.IndexOf("n") != -1)
			{
				byte B = read();
				format = format.Replace("n", string.Format("{0:X2}h", B));
			}

			if(format.IndexOf("+d") != -1) format = format.Replace("+d","d");

			if (format.IndexOf("d") != -1)
			{
				byte B = read();
				bool neg = ((B & 0x80) != 0);
				char sign = neg ? '-' : '+';
				int val = neg ? 256 - B : B;
				format = format.Replace("d", string.Format("{0}{1:X2}h", sign, val));
			}

			return format;
		}

		readonly static string[] mnemonics = new string[]
		{
		  	"NOP", "LD BC, nn", "LD (BC), A", "INC BC", //0x04
			"INC B", "DEC B", "LD B, n", "RLCA", //0x08
			"EX AF, AF'", "ADD HL, BC", "LD A, (BC)", "DEC BC", //0x0C
			"INC C", "DEC C", "LD C, n", "RRCA", //0x10
			"DJNZ d", "LD DE, nn", "LD (DE), A", "INC DE", //0x14
			"INC D", "DEC D", "LD D, n", "RLA", //0x18
			"JR d", "ADD HL, DE", "LD A, (DE)", "DEC DE", //0x1C
			"INC E", "DEC E", "LD E, n", "RRA", //0x20
			"JR NZ, d", "LD HL, nn", "LD (nn), HL", "INC HL", //0x24
			"INC H", "DEC H", "LD H, n", "DAA", //0x28
			"JR Z, d", "ADD HL, HL", "LD HL, (nn)", "DEC HL", //0x2C
			"INC L", "DEC L", "LD L, n", "CPL", //0x30
			"JR NC, d", "LD SP, nn", "LD (nn), A", "INC SP", //0x34
			"INC (HL)", "DEC (HL)", "LD (HL), n", "SCF", //0x38
			"JR C, d", "ADD HL, SP", "LD A, (nn)", "DEC SP", //0x3C
			"INC A", "DEC A", "LD A, n", "CCF", //0x40
			"LD B, B", "LD B, C", "LD B, D", "LD B, E", //0x44
			"LD B, H", "LD B, L", "LD B, (HL)", "LD B, A", //0x48
			"LD C, B", "LD C, C", "LD C, D", "LD C, E", //0x4C
			"LD C, H", "LD C, L", "LD C, (HL)", "LD C, A", //0x50
			"LD D, B", "LD D, C", "LD D, D", "LD D, E", //0x54
			"LD D, H", "LD D, L", "LD D, (HL)", "LD D, A", //0x58
			"LD E, B", "LD E, C", "LD E, D", "LD E, E", //0x5C
			"LD E, H", "LD E, L", "LD E, (HL)", "LD E, A", //0x60
			"LD H, B", "LD H, C", "LD H, D", "LD H, E", //0x64
			"LD H, H", "LD H, L", "LD H, (HL)", "LD H, A", //0x68
			"LD L, B", "LD L, B", "LD L, D", "LD L, E", //0x6C
			"LD L, H", "LD L, L", "LD L, (HL)", "LD L, A", //0x70
			"LD (HL), B", "LD (HL), C", "LD (HL), D", "LD (HL), E", //0x74
			"LD (HL), H", "LD (HL), L", "HALT", "LD (HL), A", //0x78
			"LD A, B", "LD A, C", "LD A, D", "LD A, E", //0x7C
			"LD A, H", "LD A, L", "LD A, (HL)", "LD A, A", //0x80
			"ADD A, B", "ADD A, C", "ADD A, D", "ADD A, E", //0x84
			"ADD A, H", "ADD A, L", "ADD A, (HL)", "ADD A, A", //0x88
			"ADC A, B", "ADC A, C", "ADC A, D", "ADC A, E", //0x8C
			"ADC A, H", "ADC A, L", "ADC A, (HL)", "ADC A, A", //0x90
			"SUB A, B", "SUB A, C", "SUB A, D", "SUB A, E", //0x94
			"SUB A, H", "SUB A, L", "SUB A, (HL)", "SUB A, A", //0x98
			"SBC A, B", "SBC A, C", "SBC A, D", "SBC A, E", //0x9C
			"SBC A, H", "SBC A, L", "SBC A, (HL)", "SBC A, A", //0xA0
			"AND B", "AND C", "AND D", "AND E", //0xA4
			"AND H", "AND L", "AND (HL)", "AND A", //0xA8
			"XOR B", "XOR C", "XOR D", "XOR E", //0xAC
			"XOR H", "XOR L", "XOR (HL)", "XOR A", //0xB0
			"OR B", "OR C", "OR D", "OR E", //0xB4
			"OR H", "OR L", "OR (HL)", "OR A", //0xB8
			"CP B", "CP C", "CP D", "CP E", //0xBC
			"CP H", "CP L", "CP (HL)", "CP A", //0xC0
			"RET NZ", "POP BC", "JP NZ, nn", "JP nn", //0xC4
			"CALL NZ, nn", "PUSH BC", "ADD A, n", "RST $00", //0xC8
			"RET Z", "RET", "JP Z, nn", "[CB]", //0xCC
			"CALL Z, nn", "CALL nn", "ADC A, n", "RST $08", //0xD0
			"RET NC", "POP DE", "JP NC, nn", "OUT n, A", //0xD4
			"CALL NC, nn", "PUSH DE", "SUB n", "RST $10", //0xD8
			"RET C", "EXX", "JP C, nn", "IN A, n", //0xDC
			"CALL C, nn", "[DD]", "SBC A, n", "RST $18", //0xE0
			"RET PO", "POP HL", "JP PO, nn", "EX (SP), HL", //0xE4
			"CALL C, nn", "PUSH HL", "AND n", "RST $20", //0xE8
			"RET PE", "JP HL", "JP PE, nn", "EX DE, HL", //0xEC
			"CALL PE, nn", "[ED]", "XOR n", "RST $28", //0xF0
			"RET P", "POP AF", "JP P, nn", "DI", //0xF4
			"CALL P, nn", "PUSH AF", "OR n", "RST $30", //0xF8
			"RET M", "LD SP, HL", "JP M, nn", "EI", //0xFC
			"CALL M, nn", "[FD]", "CP n", "RST $38", //0x100
		};

		readonly static string[] mnemonicsDD = new string[]
		{
			"NOP", "LD BC, nn", "LD (BC), A", "INC BC", //0x04
			"INC B", "DEC B", "LD B, n", "RLCA", //0x08
			"EX AF, AF'", "ADD IX, BC", "LD A, (BC)", "DEC BC", //0x0C
			"INC C", "DEC C", "LD C, n", "RRCA", //0x10
			"DJNZ d", "LD DE, nn", "LD (DE), A", "INC DE", //0x14
			"INC D", "DEC D", "LD D, n", "RLA", //0x18
			"JR d", "ADD IX, DE", "LD A, (DE)", "DEC DE", //0x1C
			"INC E", "DEC E", "LD E, n", "RRA", //0x20
			"JR NZ, d", "LD IX, nn", "LD (nn), IX", "INC IX", //0x24
			"INC IXH", "DEC IXH", "LD IXH, n", "DAA", //0x28
			"JR Z, d", "ADD IX, IX", "LD IX, (nn)", "DEC IX", //0x2C
			"INC IXL", "DEC IXL", "LD IXL, n", "CPL", //0x30
			"JR NC, d", "LD SP, nn", "LD (nn), A", "INC SP", //0x34
			"INC (IX+d)", "DEC (IX+d)", "LD (IX+d), n", "SCF", //0x38
			"JR C, d", "ADD IX, SP", "LD A, (nn)", "DEC SP", //0x3C
			"INC A", "DEC A", "LD A, n", "CCF", //0x40
			"LD B, B", "LD B, C", "LD B, D", "LD B, E", //0x44
			"LD B, IXH", "LD B, IXL", "LD B, (IX+d)", "LD B, A", //0x48
			"LD C, B", "LD C, C", "LD C, D", "LD C, E", //0x4C
			"LD C, IXH", "LD C, IXL", "LD C, (IX+d)", "LD C, A", //0x50
			"LD D, B", "LD D, C", "LD D, D", "LD D, E", //0x54
			"LD D, IXH", "LD D, IXL", "LD D, (IX+d)", "LD D, A", //0x58
			"LD E, B", "LD E, C", "LD E, D", "LD E, E", //0x5C
			"LD E, IXH", "LD E, IXL", "LD E, (IX+d)", "LD E, A", //0x60
			"LD IXH, B", "LD IXH, C", "LD IXH, D", "LD IXH, E", //0x64
			"LD IXH, IXH", "LD IXH, IXL", "LD H, (IX+d)", "LD IXH, A", //0x68
			"LD IXL, B", "LD IXL, C", "LD IXL, D", "LD IXL, E", //0x6C
			"LD IXL, IXH", "LD IXL, IXL", "LD L, (IX+d)", "LD IXL, A", //0x70
			"LD (IX+d), B", "LD (IX+d), C", "LD (IX+d), D", "LD (IX+d), E", //0x74
			"LD (IX+d), H", "LD (IX+d), L", "HALT", "LD (IX+d), A", //0x78
			"LD A, B", "LD A, C", "LD A, D", "LD A, E", //0x7C
			"LD A, IXH", "LD A, IXL", "LD A, (IX+d)", "LD A, A", //0x80
			"ADD A, B", "ADD A, C", "ADD A, D", "ADD A, E", //0x84
			"ADD A, IXH", "ADD A, IXL", "ADD A, (IX+d)", "ADD A, A", //0x88
			"ADC A, B", "ADC A, C", "ADC A, D", "ADC A, E", //0x8C
			"ADC A, IXH", "ADC A, IXL", "ADC A, (IX+d)", "ADC A, A", //0x90
			"SUB A, B", "SUB A, C", "SUB A, D", "SUB A, E", //0x94
			"SUB A, IXH", "SUB A, IXL", "SUB A, (IX+d)", "SUB A, A", //0x98
			"SBC A, B", "SBC A, C", "SBC A, D", "SBC A, E", //0x9C
			"SBC A, IXH", "SBC A, IXL", "SBC A, (IX+d)", "SBC A, A", //0xA0
			"AND B", "AND C", "AND D", "AND E", //0xA4
			"AND IXH", "AND IXL", "AND (IX+d)", "AND A", //0xA8
			"XOR B", "XOR C", "XOR D", "XOR E", //0xAC
			"XOR IXH", "XOR IXL", "XOR (IX+d)", "XOR A", //0xB0
			"OR B", "OR C", "OR D", "OR E", //0xB4
			"OR IXH", "OR IXL", "OR (IX+d)", "OR A", //0xB8
			"CP B", "CP C", "CP D", "CP E", //0xBC
			"CP IXH", "CP IXL", "CP (IX+d)", "CP A", //0xC0
			"RET NZ", "POP BC", "JP NZ, nn", "JP nn", //0xC4
			"CALL NZ, nn", "PUSH BC", "ADD A, n", "RST $00", //0xC8
			"RET Z", "RET", "JP Z, nn", "[DD CB]", //0xCC
			"CALL Z, nn", "CALL nn", "ADC A, n", "RST $08", //0xD0
			"RET NC", "POP DE", "JP NC, nn", "OUT n, A", //0xD4
			"CALL NC, nn", "PUSH DE", "SUB n", "RST $10", //0xD8
			"RET C", "EXX", "JP C, nn", "IN A, n", //0xDC
			"CALL C, nn", "[!DD DD!]", "SBC A, n", "RST $18", //0xE0
			"RET PO", "POP IX", "JP PO, nn", "EX (SP), IX", //0xE4
			"CALL C, nn", "PUSH IX", "AND n", "RST $20", //0xE8
			"RET PE", "JP IX", "JP PE, nn", "EX DE, HL", //0xEC
			"CALL PE, nn", "[DD ED]", "XOR n", "RST $28", //0xF0
			"RET P", "POP AF", "JP P, nn", "DI", //0xF4
			"CALL P, nn", "PUSH AF", "OR n", "RST $30", //0xF8
			"RET M", "LD SP, IX", "JP M, nn", "EI", //0xFC
			"CALL M, nn", "[!!DD FD!!]", "CP n", "RST $38", //0x100
		};

		readonly static string[] mnemonicsFD = new string[]
		{
			"NOP", "LD BC, nn", "LD (BC), A", "INC BC", //0x04
			"INC B", "DEC B", "LD B, n", "RLCA", //0x08
			"EX AF, AF'", "ADD IY, BC", "LD A, (BC)", "DEC BC", //0x0C
			"INC C", "DEC C", "LD C, n", "RRCA", //0x10
			"DJNZ d", "LD DE, nn", "LD (DE), A", "INC DE", //0x14
			"INC D", "DEC D", "LD D, n", "RLA", //0x18
			"JR d", "ADD IY, DE", "LD A, (DE)", "DEC DE", //0x1C
			"INC E", "DEC E", "LD E, n", "RRA", //0x20
			"JR NZ, d", "LD IY, nn", "LD (nn), IY", "INC IY", //0x24
			"INC IYH", "DEC IYH", "LD IYH, n", "DAA", //0x28
			"JR Z, d", "ADD IY, IY", "LD IY, (nn)", "DEC IY", //0x2C
			"INC IYL", "DEC IYL", "LD IYL, n", "CPL", //0x30
			"JR NC, d", "LD SP, nn", "LD (nn), A", "INC SP", //0x34
			"INC (IY+d)", "DEC (IY+d)", "LD (IY+d), n", "SCF", //0x38
			"JR C, d", "ADD IY, SP", "LD A, (nn)", "DEC SP", //0x3C
			"INC A", "DEC A", "LD A, n", "CCF", //0x40
			"LD B, B", "LD B, C", "LD B, D", "LD B, E", //0x44
			"LD B, IYH", "LD B, IYL", "LD B, (IY+d)", "LD B, A", //0x48
			"LD C, B", "LD C, C", "LD C, D", "LD C, E", //0x4C
			"LD C, IYH", "LD C, IYL", "LD C, (IY+d)", "LD C, A", //0x50
			"LD D, B", "LD D, C", "LD D, D", "LD D, E", //0x54
			"LD D, IYH", "LD D, IYL", "LD D, (IY+d)", "LD D, A", //0x58
			"LD E, B", "LD E, C", "LD E, D", "LD E, E", //0x5C
			"LD E, IYH", "LD E, IYL", "LD E, (IY+d)", "LD E, A", //0x60
			"LD IYH, B", "LD IYH, C", "LD IYH, D", "LD IYH, E", //0x64
			"LD IYH, IYH", "LD IYH, IYL", "LD H, (IY+d)", "LD IYH, A", //0x68
			"LD IYL, B", "LD IYL, C", "LD IYL, D", "LD IYL, E", //0x6C
			"LD IYL, IYH", "LD IYL, IYL", "LD L, (IY+d)", "LD IYL, A", //0x70
			"LD (IY+d), B", "LD (IY+d), C", "LD (IY+d), D", "LD (IY+d), E", //0x74
			"LD (IY+d), H", "LD (IY+d), L", "HALT", "LD (IY+d), A", //0x78
			"LD A, B", "LD A, C", "LD A, D", "LD A, E", //0x7C
			"LD A, IYH", "LD A, IYL", "LD A, (IY+d)", "LD A, A", //0x80
			"ADD A, B", "ADD A, C", "ADD A, D", "ADD A, E", //0x84
			"ADD A, IYH", "ADD A, IYL", "ADD A, (IY+d)", "ADD A, A", //0x88
			"ADC A, B", "ADC A, C", "ADC A, D", "ADC A, E", //0x8C
			"ADC A, IYH", "ADC A, IYL", "ADC A, (IY+d)", "ADC A, A", //0x90
			"SUB A, B", "SUB A, C", "SUB A, D", "SUB A, E", //0x94
			"SUB A, IYH", "SUB A, IYL", "SUB A, (IY+d)", "SUB A, A", //0x98
			"SBC A, B", "SBC A, C", "SBC A, D", "SBC A, E", //0x9C
			"SBC A, IYH", "SBC A, IYL", "SBC A, (IY+d)", "SBC A, A", //0xA0
			"AND B", "AND C", "AND D", "AND E", //0xA4
			"AND IYH", "AND IYL", "AND (IY+d)", "AND A", //0xA8
			"XOR B", "XOR C", "XOR D", "XOR E", //0xAC
			"XOR IYH", "XOR IYL", "XOR (IY+d)", "XOR A", //0xB0
			"OR B", "OR C", "OR D", "OR E", //0xB4
			"OR IYH", "OR IYL", "OR (IY+d)", "OR A", //0xB8
			"CP B", "CP C", "CP D", "CP E", //0xBC
			"CP IYH", "CP IYL", "CP (IY+d)", "CP A", //0xC0
			"RET NZ", "POP BC", "JP NZ, nn", "JP nn", //0xC4
			"CALL NZ, nn", "PUSH BC", "ADD A, n", "RST $00", //0xC8
			"RET Z", "RET", "JP Z, nn", "[DD CB]", //0xCC
			"CALL Z, nn", "CALL nn", "ADC A, n", "RST $08", //0xD0
			"RET NC", "POP DE", "JP NC, nn", "OUT n, A", //0xD4
			"CALL NC, nn", "PUSH DE", "SUB n", "RST $10", //0xD8
			"RET C", "EXX", "JP C, nn", "IN A, n", //0xDC
			"CALL C, nn", "[!FD DD!]", "SBC A, n", "RST $18", //0xE0
			"RET PO", "POP IY", "JP PO, nn", "EX (SP), IY", //0xE4
			"CALL C, nn", "PUSH IY", "AND n", "RST $20", //0xE8
			"RET PE", "JP IY", "JP PE, nn", "EX DE, HL", //0xEC
			"CALL PE, nn", "[FD ED]", "XOR n", "RST $28", //0xF0
			"RET P", "POP AF", "JP P, nn", "DI", //0xF4
			"CALL P, nn", "PUSH AF", "OR n", "RST $30", //0xF8
			"RET M", "LD SP, IY", "JP M, nn", "EI", //0xFC
			"CALL M, nn", "[!FD FD!]", "CP n", "RST $38", //0x100
		};

		readonly static string[] mnemonicsDDCB = new string[]
		{
			"RLC (IX+d)->B", "RLC (IX+d)->C", "RLC (IX+d)->D", "RLC (IX+d)->E", "RLC (IX+d)->H", "RLC (IX+d)->L", "RLC (IX+d)", "RLC (IX+d)->A", 
			"RRC (IX+d)->B", "RRC (IX+d)->C", "RRC (IX+d)->D", "RRC (IX+d)->E", "RRC (IX+d)->H", "RRC (IX+d)->L", "RRC (IX+d)", "RRC (IX+d)->A", 
			"RL (IX+d)->B", "RL (IX+d)->C", "RL (IX+d)->D", "RL (IX+d)->E", "RL (IX+d)->H", "RL (IX+d)->L", "RL (IX+d)", "RL (IX+d)->A", 
			"RR (IX+d)->B", "RR (IX+d)->C", "RR (IX+d)->D", "RR (IX+d)->E", "RR (IX+d)->H", "RR (IX+d)->L", "RR (IX+d)", "RR (IX+d)->A", 
			"SLA (IX+d)->B", "SLA (IX+d)->C", "SLA (IX+d)->D", "SLA (IX+d)->E", "SLA (IX+d)->H", "SLA (IX+d)->L", "SLA (IX+d)", "SLA (IX+d)->A", 
			"SRA (IX+d)->B", "SRA (IX+d)->C", "SRA (IX+d)->D", "SRA (IX+d)->E", "SRA (IX+d)->H", "SRA (IX+d)->L", "SRA (IX+d)", "SRA (IX+d)->A", 
			"SL1 (IX+d)->B", "SL1 (IX+d)->C", "SL1 (IX+d)->D", "SL1 (IX+d)->E", "SL1 (IX+d)->H", "SL1 (IX+d)->L", "SL1 (IX+d)", "SL1 (IX+d)->A", 
			"SRL (IX+d)->B", "SRL (IX+d)->C", "SRL (IX+d)->D", "SRL (IX+d)->E", "SRL (IX+d)->H", "SRL (IX+d)->L", "SRL (IX+d)", "SRL (IX+d)->A", 
			"BIT 0, (IX+d)", "BIT 0, (IX+d)", "BIT 0, (IX+d)", "BIT 0, (IX+d)", "BIT 0, (IX+d)", "BIT 0, (IX+d)", "BIT 0, (IX+d)", "BIT 0, (IX+d)", 
			"BIT 1, (IX+d)", "BIT 1, (IX+d)", "BIT 1, (IX+d)", "BIT 1, (IX+d)", "BIT 1, (IX+d)", "BIT 1, (IX+d)", "BIT 1, (IX+d)", "BIT 1, (IX+d)", 
			"BIT 2, (IX+d)", "BIT 2, (IX+d)", "BIT 2, (IX+d)", "BIT 2, (IX+d)", "BIT 2, (IX+d)", "BIT 2, (IX+d)", "BIT 2, (IX+d)", "BIT 2, (IX+d)", 
			"BIT 3, (IX+d)", "BIT 3, (IX+d)", "BIT 3, (IX+d)", "BIT 3, (IX+d)", "BIT 3, (IX+d)", "BIT 3, (IX+d)", "BIT 3, (IX+d)", "BIT 3, (IX+d)", 
			"BIT 4, (IX+d)", "BIT 4, (IX+d)", "BIT 4, (IX+d)", "BIT 4, (IX+d)", "BIT 4, (IX+d)", "BIT 4, (IX+d)", "BIT 4, (IX+d)", "BIT 4, (IX+d)", 
			"BIT 5, (IX+d)", "BIT 5, (IX+d)", "BIT 5, (IX+d)", "BIT 5, (IX+d)", "BIT 5, (IX+d)", "BIT 5, (IX+d)", "BIT 5, (IX+d)", "BIT 5, (IX+d)", 
			"BIT 6, (IX+d)", "BIT 6, (IX+d)", "BIT 6, (IX+d)", "BIT 6, (IX+d)", "BIT 6, (IX+d)", "BIT 6, (IX+d)", "BIT 6, (IX+d)", "BIT 6, (IX+d)", 
			"BIT 7, (IX+d)", "BIT 7, (IX+d)", "BIT 7, (IX+d)", "BIT 7, (IX+d)", "BIT 7, (IX+d)", "BIT 7, (IX+d)", "BIT 7, (IX+d)", "BIT 7, (IX+d)", 
			"RES 0 (IX+d)->B", "RES 0 (IX+d)->C", "RES 0 (IX+d)->D", "RES 0 (IX+d)->E", "RES 0 (IX+d)->H", "RES 0 (IX+d)->L", "RES 0 (IX+d)", "RES 0 (IX+d)->A", 
			"RES 1 (IX+d)->B", "RES 1 (IX+d)->C", "RES 1 (IX+d)->D", "RES 1 (IX+d)->E", "RES 1 (IX+d)->H", "RES 1 (IX+d)->L", "RES 1 (IX+d)", "RES 1 (IX+d)->A", 
			"RES 2 (IX+d)->B", "RES 2 (IX+d)->C", "RES 2 (IX+d)->D", "RES 2 (IX+d)->E", "RES 2 (IX+d)->H", "RES 2 (IX+d)->L", "RES 2 (IX+d)", "RES 2 (IX+d)->A", 
			"RES 3 (IX+d)->B", "RES 3 (IX+d)->C", "RES 3 (IX+d)->D", "RES 3 (IX+d)->E", "RES 3 (IX+d)->H", "RES 3 (IX+d)->L", "RES 3 (IX+d)", "RES 3 (IX+d)->A", 
			"RES 4 (IX+d)->B", "RES 4 (IX+d)->C", "RES 4 (IX+d)->D", "RES 4 (IX+d)->E", "RES 4 (IX+d)->H", "RES 4 (IX+d)->L", "RES 4 (IX+d)", "RES 4 (IX+d)->A", 
			"RES 5 (IX+d)->B", "RES 5 (IX+d)->C", "RES 5 (IX+d)->D", "RES 5 (IX+d)->E", "RES 5 (IX+d)->H", "RES 5 (IX+d)->L", "RES 5 (IX+d)", "RES 5 (IX+d)->A", 
			"RES 6 (IX+d)->B", "RES 6 (IX+d)->C", "RES 6 (IX+d)->D", "RES 6 (IX+d)->E", "RES 6 (IX+d)->H", "RES 6 (IX+d)->L", "RES 6 (IX+d)", "RES 6 (IX+d)->A", 
			"RES 7 (IX+d)->B", "RES 7 (IX+d)->C", "RES 7 (IX+d)->D", "RES 7 (IX+d)->E", "RES 7 (IX+d)->H", "RES 7 (IX+d)->L", "RES 7 (IX+d)", "RES 7 (IX+d)->A", 
			"SET 0 (IX+d)->B", "SET 0 (IX+d)->C", "SET 0 (IX+d)->D", "SET 0 (IX+d)->E", "SET 0 (IX+d)->H", "SET 0 (IX+d)->L", "SET 0 (IX+d)", "SET 0 (IX+d)->A", 
			"SET 1 (IX+d)->B", "SET 1 (IX+d)->C", "SET 1 (IX+d)->D", "SET 1 (IX+d)->E", "SET 1 (IX+d)->H", "SET 1 (IX+d)->L", "SET 1 (IX+d)", "SET 1 (IX+d)->A", 
			"SET 2 (IX+d)->B", "SET 2 (IX+d)->C", "SET 2 (IX+d)->D", "SET 2 (IX+d)->E", "SET 2 (IX+d)->H", "SET 2 (IX+d)->L", "SET 2 (IX+d)", "SET 2 (IX+d)->A", 
			"SET 3 (IX+d)->B", "SET 3 (IX+d)->C", "SET 3 (IX+d)->D", "SET 3 (IX+d)->E", "SET 3 (IX+d)->H", "SET 3 (IX+d)->L", "SET 3 (IX+d)", "SET 3 (IX+d)->A", 
			"SET 4 (IX+d)->B", "SET 4 (IX+d)->C", "SET 4 (IX+d)->D", "SET 4 (IX+d)->E", "SET 4 (IX+d)->H", "SET 4 (IX+d)->L", "SET 4 (IX+d)", "SET 4 (IX+d)->A", 
			"SET 5 (IX+d)->B", "SET 5 (IX+d)->C", "SET 5 (IX+d)->D", "SET 5 (IX+d)->E", "SET 5 (IX+d)->H", "SET 5 (IX+d)->L", "SET 5 (IX+d)", "SET 5 (IX+d)->A", 
			"SET 6 (IX+d)->B", "SET 6 (IX+d)->C", "SET 6 (IX+d)->D", "SET 6 (IX+d)->E", "SET 6 (IX+d)->H", "SET 6 (IX+d)->L", "SET 6 (IX+d)", "SET 6 (IX+d)->A", 
			"SET 7 (IX+d)->B", "SET 7 (IX+d)->C", "SET 7 (IX+d)->D", "SET 7 (IX+d)->E", "SET 7 (IX+d)->H", "SET 7 (IX+d)->L", "SET 7 (IX+d)", "SET 7 (IX+d)->A", 
		};

		readonly static string[] mnemonicsFDCB = new string[]
		{
			"RLC (IY+d)->B", "RLC (IY+d)->C", "RLC (IY+d)->D", "RLC (IY+d)->E", "RLC (IY+d)->H", "RLC (IY+d)->L", "RLC (IY+d)", "RLC (IY+d)->A", 
			"RRC (IY+d)->B", "RRC (IY+d)->C", "RRC (IY+d)->D", "RRC (IY+d)->E", "RRC (IY+d)->H", "RRC (IY+d)->L", "RRC (IY+d)", "RRC (IY+d)->A", 
			"RL (IY+d)->B", "RL (IY+d)->C", "RL (IY+d)->D", "RL (IY+d)->E", "RL (IY+d)->H", "RL (IY+d)->L", "RL (IY+d)", "RL (IY+d)->A", 
			"RR (IY+d)->B", "RR (IY+d)->C", "RR (IY+d)->D", "RR (IY+d)->E", "RR (IY+d)->H", "RR (IY+d)->L", "RR (IY+d)", "RR (IY+d)->A", 
			"SLA (IY+d)->B", "SLA (IY+d)->C", "SLA (IY+d)->D", "SLA (IY+d)->E", "SLA (IY+d)->H", "SLA (IY+d)->L", "SLA (IY+d)", "SLA (IY+d)->A", 
			"SRA (IY+d)->B", "SRA (IY+d)->C", "SRA (IY+d)->D", "SRA (IY+d)->E", "SRA (IY+d)->H", "SRA (IY+d)->L", "SRA (IY+d)", "SRA (IY+d)->A", 
			"SL1 (IY+d)->B", "SL1 (IY+d)->C", "SL1 (IY+d)->D", "SL1 (IY+d)->E", "SL1 (IY+d)->H", "SL1 (IY+d)->L", "SL1 (IY+d)", "SL1 (IY+d)->A", 
			"SRL (IY+d)->B", "SRL (IY+d)->C", "SRL (IY+d)->D", "SRL (IY+d)->E", "SRL (IY+d)->H", "SRL (IY+d)->L", "SRL (IY+d)", "SRL (IY+d)->A", 
			"BIT 0, (IY+d)", "BIT 0, (IY+d)", "BIT 0, (IY+d)", "BIT 0, (IY+d)", "BIT 0, (IY+d)", "BIT 0, (IY+d)", "BIT 0, (IY+d)", "BIT 0, (IY+d)", 
			"BIT 1, (IY+d)", "BIT 1, (IY+d)", "BIT 1, (IY+d)", "BIT 1, (IY+d)", "BIT 1, (IY+d)", "BIT 1, (IY+d)", "BIT 1, (IY+d)", "BIT 1, (IY+d)", 
			"BIT 2, (IY+d)", "BIT 2, (IY+d)", "BIT 2, (IY+d)", "BIT 2, (IY+d)", "BIT 2, (IY+d)", "BIT 2, (IY+d)", "BIT 2, (IY+d)", "BIT 2, (IY+d)", 
			"BIT 3, (IY+d)", "BIT 3, (IY+d)", "BIT 3, (IY+d)", "BIT 3, (IY+d)", "BIT 3, (IY+d)", "BIT 3, (IY+d)", "BIT 3, (IY+d)", "BIT 3, (IY+d)", 
			"BIT 4, (IY+d)", "BIT 4, (IY+d)", "BIT 4, (IY+d)", "BIT 4, (IY+d)", "BIT 4, (IY+d)", "BIT 4, (IY+d)", "BIT 4, (IY+d)", "BIT 4, (IY+d)", 
			"BIT 5, (IY+d)", "BIT 5, (IY+d)", "BIT 5, (IY+d)", "BIT 5, (IY+d)", "BIT 5, (IY+d)", "BIT 5, (IY+d)", "BIT 5, (IY+d)", "BIT 5, (IY+d)", 
			"BIT 6, (IY+d)", "BIT 6, (IY+d)", "BIT 6, (IY+d)", "BIT 6, (IY+d)", "BIT 6, (IY+d)", "BIT 6, (IY+d)", "BIT 6, (IY+d)", "BIT 6, (IY+d)", 
			"BIT 7, (IY+d)", "BIT 7, (IY+d)", "BIT 7, (IY+d)", "BIT 7, (IY+d)", "BIT 7, (IY+d)", "BIT 7, (IY+d)", "BIT 7, (IY+d)", "BIT 7, (IY+d)", 
			"RES 0 (IY+d)->B", "RES 0 (IY+d)->C", "RES 0 (IY+d)->D", "RES 0 (IY+d)->E", "RES 0 (IY+d)->H", "RES 0 (IY+d)->L", "RES 0 (IY+d)", "RES 0 (IY+d)->A", 
			"RES 1 (IY+d)->B", "RES 1 (IY+d)->C", "RES 1 (IY+d)->D", "RES 1 (IY+d)->E", "RES 1 (IY+d)->H", "RES 1 (IY+d)->L", "RES 1 (IY+d)", "RES 1 (IY+d)->A", 
			"RES 2 (IY+d)->B", "RES 2 (IY+d)->C", "RES 2 (IY+d)->D", "RES 2 (IY+d)->E", "RES 2 (IY+d)->H", "RES 2 (IY+d)->L", "RES 2 (IY+d)", "RES 2 (IY+d)->A", 
			"RES 3 (IY+d)->B", "RES 3 (IY+d)->C", "RES 3 (IY+d)->D", "RES 3 (IY+d)->E", "RES 3 (IY+d)->H", "RES 3 (IY+d)->L", "RES 3 (IY+d)", "RES 3 (IY+d)->A", 
			"RES 4 (IY+d)->B", "RES 4 (IY+d)->C", "RES 4 (IY+d)->D", "RES 4 (IY+d)->E", "RES 4 (IY+d)->H", "RES 4 (IY+d)->L", "RES 4 (IY+d)", "RES 4 (IY+d)->A", 
			"RES 5 (IY+d)->B", "RES 5 (IY+d)->C", "RES 5 (IY+d)->D", "RES 5 (IY+d)->E", "RES 5 (IY+d)->H", "RES 5 (IY+d)->L", "RES 5 (IY+d)", "RES 5 (IY+d)->A", 
			"RES 6 (IY+d)->B", "RES 6 (IY+d)->C", "RES 6 (IY+d)->D", "RES 6 (IY+d)->E", "RES 6 (IY+d)->H", "RES 6 (IY+d)->L", "RES 6 (IY+d)", "RES 6 (IY+d)->A", 
			"RES 7 (IY+d)->B", "RES 7 (IY+d)->C", "RES 7 (IY+d)->D", "RES 7 (IY+d)->E", "RES 7 (IY+d)->H", "RES 7 (IY+d)->L", "RES 7 (IY+d)", "RES 7 (IY+d)->A", 
			"SET 0 (IY+d)->B", "SET 0 (IY+d)->C", "SET 0 (IY+d)->D", "SET 0 (IY+d)->E", "SET 0 (IY+d)->H", "SET 0 (IY+d)->L", "SET 0 (IY+d)", "SET 0 (IY+d)->A", 
			"SET 1 (IY+d)->B", "SET 1 (IY+d)->C", "SET 1 (IY+d)->D", "SET 1 (IY+d)->E", "SET 1 (IY+d)->H", "SET 1 (IY+d)->L", "SET 1 (IY+d)", "SET 1 (IY+d)->A", 
			"SET 2 (IY+d)->B", "SET 2 (IY+d)->C", "SET 2 (IY+d)->D", "SET 2 (IY+d)->E", "SET 2 (IY+d)->H", "SET 2 (IY+d)->L", "SET 2 (IY+d)", "SET 2 (IY+d)->A", 
			"SET 3 (IY+d)->B", "SET 3 (IY+d)->C", "SET 3 (IY+d)->D", "SET 3 (IY+d)->E", "SET 3 (IY+d)->H", "SET 3 (IY+d)->L", "SET 3 (IY+d)", "SET 3 (IY+d)->A", 
			"SET 4 (IY+d)->B", "SET 4 (IY+d)->C", "SET 4 (IY+d)->D", "SET 4 (IY+d)->E", "SET 4 (IY+d)->H", "SET 4 (IY+d)->L", "SET 4 (IY+d)", "SET 4 (IY+d)->A", 
			"SET 5 (IY+d)->B", "SET 5 (IY+d)->C", "SET 5 (IY+d)->D", "SET 5 (IY+d)->E", "SET 5 (IY+d)->H", "SET 5 (IY+d)->L", "SET 5 (IY+d)", "SET 5 (IY+d)->A", 
			"SET 6 (IY+d)->B", "SET 6 (IY+d)->C", "SET 6 (IY+d)->D", "SET 6 (IY+d)->E", "SET 6 (IY+d)->H", "SET 6 (IY+d)->L", "SET 6 (IY+d)", "SET 6 (IY+d)->A", 
			"SET 7 (IY+d)->B", "SET 7 (IY+d)->C", "SET 7 (IY+d)->D", "SET 7 (IY+d)->E", "SET 7 (IY+d)->H", "SET 7 (IY+d)->L", "SET 7 (IY+d)", "SET 7 (IY+d)->A", 
		};

		readonly static string[] mnemonicsCB = new string[]
		{
			"RLC B", "RLC C", "RLC D", "RLC E", "RLC H", "RLC L", "RLC (HL)", "RLC A", 
			"RRC B", "RRC C", "RRC D", "RRC E", "RRC H", "RRC L", "RRC (HL)", "RRC A",
			"RL B", "RL C", "RL D", "RL E", "RL H", "RL L", "RL (HL)", "RL A",
			"RR B", "RR C", "RR D", "RR E", "RR H", "RR L", "RR (HL)", "RR A",
			"SLA B", "SLA C", "SLA D", "SLA E", "SLA H", "SLA L", "SLA (HL)", "SLA A",
			"SRA B", "SRA C", "SRA D", "SRA E", "SRA H", "SRA L", "SRA (HL)", "SRA A",
			"SL1 B", "SL1 C", "SL1 D", "SL1 E", "SL1 H", "SL1 L", "SL1 (HL)", "SL1 A",
			"SRL B", "SRL C", "SRL D", "SRL E", "SRL H", "SRL L", "SRL (HL)", "SRL A",
			"BIT 0, B", "BIT 0, C", "BIT 0, D", "BIT 0, E", "BIT 0, H", "BIT 0, L", "BIT 0, (HL)", "BIT 0, A",
			"BIT 1, B", "BIT 1, C", "BIT 1, D", "BIT 1, E", "BIT 1, H", "BIT 1, L", "BIT 1, (HL)", "BIT 1, A",
			"BIT 2, B", "BIT 2, C", "BIT 2, D", "BIT 2, E", "BIT 2, H", "BIT 2, L", "BIT 2, (HL)", "BIT 2, A",
			"BIT 3, B", "BIT 3, C", "BIT 3, D", "BIT 3, E", "BIT 3, H", "BIT 3, L", "BIT 3, (HL)", "BIT 3, A",
			"BIT 4, B", "BIT 4, C", "BIT 4, D", "BIT 4, E", "BIT 4, H", "BIT 4, L", "BIT 4, (HL)", "BIT 4, A",
			"BIT 5, B", "BIT 5, C", "BIT 5, D", "BIT 5, E", "BIT 5, H", "BIT 5, L", "BIT 5, (HL)", "BIT 5, A",
			"BIT 6, B", "BIT 6, C", "BIT 6, D", "BIT 6, E", "BIT 6, H", "BIT 6, L", "BIT 6, (HL)", "BIT 6, A",
			"BIT 7, B", "BIT 7, C", "BIT 7, D", "BIT 7, E", "BIT 7, H", "BIT 7, L", "BIT 7, (HL)", "BIT 7, A",
			"RES 0, B", "RES 0, C", "RES 0, D", "RES 0, E", "RES 0, H", "RES 0, L", "RES 0, (HL)", "RES 0, A",
			"RES 1, B", "RES 1, C", "RES 1, D", "RES 1, E", "RES 1, H", "RES 1, L", "RES 1, (HL)", "RES 1, A",
			"RES 2, B", "RES 2, C", "RES 2, D", "RES 2, E", "RES 2, H", "RES 2, L", "RES 2, (HL)", "RES 2, A",
			"RES 3, B", "RES 3, C", "RES 3, D", "RES 3, E", "RES 3, H", "RES 3, L", "RES 3, (HL)", "RES 3, A",
			"RES 4, B", "RES 4, C", "RES 4, D", "RES 4, E", "RES 4, H", "RES 4, L", "RES 4, (HL)", "RES 4, A",
			"RES 5, B", "RES 5, C", "RES 5, D", "RES 5, E", "RES 5, H", "RES 5, L", "RES 5, (HL)", "RES 5, A",
			"RES 6, B", "RES 6, C", "RES 6, D", "RES 6, E", "RES 6, H", "RES 6, L", "RES 6, (HL)", "RES 6, A",
			"RES 7, B", "RES 7, C", "RES 7, D", "RES 7, E", "RES 7, H", "RES 7, L", "RES 7, (HL)", "RES 7, A",
			"SET 0, B", "SET 0, C", "SET 0, D", "SET 0, E", "SET 0, H", "SET 0, L", "SET 0, (HL)", "SET 0, A",
			"SET 1, B", "SET 1, C", "SET 1, D", "SET 1, E", "SET 1, H", "SET 1, L", "SET 1, (HL)", "SET 1, A",
			"SET 2, B", "SET 2, C", "SET 2, D", "SET 2, E", "SET 2, H", "SET 2, L", "SET 2, (HL)", "SET 2, A",
			"SET 3, B", "SET 3, C", "SET 3, D", "SET 3, E", "SET 3, H", "SET 3, L", "SET 3, (HL)", "SET 3, A",
			"SET 4, B", "SET 4, C", "SET 4, D", "SET 4, E", "SET 4, H", "SET 4, L", "SET 4, (HL)", "SET 4, A",
			"SET 5, B", "SET 5, C", "SET 5, D", "SET 5, E", "SET 5, H", "SET 5, L", "SET 5, (HL)", "SET 5, A",
			"SET 6, B", "SET 6, C", "SET 6, D", "SET 6, E", "SET 6, H", "SET 6, L", "SET 6, (HL)", "SET 6, A",
			"SET 7, B", "SET 7, C", "SET 7, D", "SET 7, E", "SET 7, H", "SET 7, L", "SET 7, (HL)", "SET 7, A",
		};

		readonly static string[] mnemonicsED = new string[]
		{
			"NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", 
			"NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", 
			"NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", 
			"NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", 
			
			"IN B, C", "OUT C, B", "SBC HL, BC", "LD (nn), BC", //0x44
			"NEG", "RETN", "IM $0", "LD I, A", //0x48
			"IN C, C", "OUT C, C", "ADC HL, BC", "LD BC, (nn)", //0x4C
			"NEG", "RETI", "IM $0", "LD R, A", //0x50
			"IN D, C", "OUT C, D", "SBC HL, DE", "LD (nn), DE", //0x54
			"NEG", "RETN", "IM $1", "LD A, I", //0x58
			"IN E, C", "OUT C, E", "ADC HL, DE", "LD DE, (nn)", //0x5C
			"NEG", "RETI", "IM $2", "LD A, R", //0x60
			
			"IN H, C", "OUT C, H", "SBC HL, HL", "LD (nn), HL", //0x64
			"NEG", "RETN", "IM $0", "RRD", //0x68
			"IN L, C", "OUT C, L", "ADC HL, HL", "LD HL, (nn)", //0x6C
			"NEG", "RETI", "IM $0", "RLD", //0x70
			"IN 0, C", "OUT C, 0", "SBC HL, SP", "LD (nn), SP", //0x74
			"NEG", "RETN", "IM $1", "NOP", //0x78
			"IN A, C", "OUT C, A", "ADC HL, SP", "LD SP, (nn)", //0x7C
			"NEG", "RETI", "IM $2", "NOP", //0x80

			"NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", //0x90
			"NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", //0xA0
			"LDI", "CPI", "INI", "OUTI", //0xA4
			"NOP", "NOP", "NOP", "NOP", //0xA8
			"LDD", "CPD", "IND", "OUTD", //0xAC
			"NOP", "NOP", "NOP", "NOP", //0xB0
			"LDIR", "CPIR", "INIR", "OTIR", //0xB4
			"NOP", "NOP", "NOP", "NOP", //0xB8
			"LDDR", "CPDR", "INDR", "OTDR", //0xBC
			"NOP", "NOP", "NOP", "NOP", //0xC0

			"NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", //0xD0
			"NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", //0xE0
			"NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", //0xF0
			"NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", //0x100
		};

		string DisassembleInternal(Func<byte> read)
		{
			byte A = read();
			string format;
			switch (A)
			{
				case 0xCB:
					A = read();
					format = mnemonicsCB[A];
					break;
				case 0xDD:
					A = read();
					switch(A)
					{
						case 0xCB: format = mnemonicsDDCB[A]; break;
						case 0xED: format = mnemonicsED[A]; break;
						default: format = mnemonicsDD[A]; break;
					}
					break;
				case 0xED:
					A = read();
					format = mnemonicsED[A];
					break;
				case 0xFD:
					A = read();
					switch (A)
					{
						case 0xCB: format = mnemonicsFDCB[A]; break;
						case 0xED: format = mnemonicsED[A]; break;
						default: format = mnemonicsFD[A]; break;
					}
					break;
				default: format = mnemonics[A]; break;
			}
			return format;
		}

		public string Disassemble(Func<byte> read)
		{
			return Result(DisassembleInternal(read),read);
		}
	}
}
