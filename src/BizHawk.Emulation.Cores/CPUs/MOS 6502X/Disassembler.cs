using BizHawk.Emulation.Common;
using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Components.M6502
{
	public partial class MOS6502X<TLink> : IDisassemblable
	{
		public string Disassemble(ushort pc, out int bytesToAdvance)
		{
			return MOS6502X.Disassemble(pc, out bytesToAdvance, _link.PeekMemory);
		}

		public string Cpu
		{
			get => "6502";
			set
			{
			}
		}

		public string PCRegisterName => "PC";

		public IEnumerable<string> AvailableCpus { get; } = [ "6502" ];

		public string Disassemble(MemoryDomain m, uint addr, out int length)
		{
			return MOS6502X.Disassemble((ushort)addr, out length, a => m.PeekByte(a));
		}
	}

	public static class MOS6502X
	{
		private static ushort peeker_word(ushort address, Func<ushort, byte> peeker)
		{
			byte l = peeker(address);
			byte h = peeker(++address);
			return (ushort)((h << 8) | l);
		}

		/// <summary>
		/// disassemble not from our own memory map, but from the supplied memory domain
		/// </summary>
		public static string Disassemble(ushort pc, out int bytesToAdvance, Func<ushort, byte> peeker)
		{
			byte op = peeker(pc);
			switch (op)
			{
				case 0x00: bytesToAdvance = 1; return "BRK";
				case 0x01: bytesToAdvance = 2; return $"ORA (${peeker(++pc):X2},X)";
				case 0x04: bytesToAdvance = 2; return $"NOP ${peeker(++pc):X2}";
				case 0x05: bytesToAdvance = 2; return $"ORA ${peeker(++pc):X2}";
				case 0x06: bytesToAdvance = 2; return $"ASL ${peeker(++pc):X2}";
				case 0x08: bytesToAdvance = 1; return "PHP";
				case 0x09: bytesToAdvance = 2; return $"ORA #${peeker(++pc):X2}";
				case 0x0A: bytesToAdvance = 1; return "ASL A";
				case 0x0C: bytesToAdvance = 3; return $"NOP (${peeker_word(++pc, peeker):X4})";
				case 0x0D: bytesToAdvance = 3; return $"ORA ${peeker_word(++pc, peeker):X4}";
				case 0x0E: bytesToAdvance = 3; return $"ASL ${peeker_word(++pc, peeker):X4}";
				case 0x10: bytesToAdvance = 2; return $"BPL ${(ushort)(pc + 2 + (sbyte)peeker(++pc)):X4}";
				case 0x11: bytesToAdvance = 2; return $"ORA (${peeker(++pc):X2}),Y *";
				case 0x14: bytesToAdvance = 2; return $"NOP ${peeker(++pc):X2},X";
				case 0x15: bytesToAdvance = 2; return $"ORA ${peeker(++pc):X2},X";
				case 0x16: bytesToAdvance = 2; return $"ASL ${peeker(++pc):X2},X";
				case 0x18: bytesToAdvance = 1; return "CLC";
				case 0x19: bytesToAdvance = 3; return $"ORA ${peeker_word(++pc, peeker):X4},Y *";
				case 0x1A: bytesToAdvance = 1; return "NOP";
				case 0x1C: bytesToAdvance = 2; return $"NOP (${peeker(++pc):X2},X)";
				case 0x1D: bytesToAdvance = 3; return $"ORA ${peeker_word(++pc, peeker):X4},X *";
				case 0x1E: bytesToAdvance = 3; return $"ASL ${peeker_word(++pc, peeker):X4},X";
				case 0x20: bytesToAdvance = 3; return $"JSR ${peeker_word(++pc, peeker):X4}";
				case 0x21: bytesToAdvance = 2; return $"AND (${peeker(++pc):X2},X)";
				case 0x24: bytesToAdvance = 2; return $"BIT ${peeker(++pc):X2}";
				case 0x25: bytesToAdvance = 2; return $"AND ${peeker(++pc):X2}";
				case 0x26: bytesToAdvance = 2; return $"ROL ${peeker(++pc):X2}";
				case 0x28: bytesToAdvance = 1; return "PLP";
				case 0x29: bytesToAdvance = 2; return $"AND #${peeker(++pc):X2}";
				case 0x2A: bytesToAdvance = 1; return "ROL A";
				case 0x2C: bytesToAdvance = 3; return $"BIT ${peeker_word(++pc, peeker):X4}";
				case 0x2D: bytesToAdvance = 3; return $"AND ${peeker_word(++pc, peeker):X4}";
				case 0x2E: bytesToAdvance = 3; return $"ROL ${peeker_word(++pc, peeker):X4}";
				case 0x30: bytesToAdvance = 2; return $"BMI ${(ushort)(pc + 2 + (sbyte)peeker(++pc)):X4}";
				case 0x31: bytesToAdvance = 2; return $"AND (${peeker(++pc):X2}),Y *";
				case 0x34: bytesToAdvance = 2; return $"NOP ${peeker(++pc):X2},X";
				case 0x35: bytesToAdvance = 2; return $"AND ${peeker(++pc):X2},X";
				case 0x36: bytesToAdvance = 2; return $"ROL ${peeker(++pc):X2},X";
				case 0x38: bytesToAdvance = 1; return "SEC";
				case 0x39: bytesToAdvance = 3; return $"AND ${peeker_word(++pc, peeker):X4},Y *";
				case 0x3A: bytesToAdvance = 1; return "NOP";
				case 0x3C: bytesToAdvance = 2; return $"NOP (${peeker(++pc):X2},X)";
				case 0x3D: bytesToAdvance = 3; return $"AND ${peeker_word(++pc, peeker):X4},X *";
				case 0x3E: bytesToAdvance = 3; return $"ROL ${peeker_word(++pc, peeker):X4},X";
				case 0x40: bytesToAdvance = 1; return "RTI";
				case 0x41: bytesToAdvance = 2; return $"EOR (${peeker(++pc):X2},X)";
				case 0x44: bytesToAdvance = 2; return $"NOP ${peeker(++pc):X2}";
				case 0x45: bytesToAdvance = 2; return $"EOR ${peeker(++pc):X2}";
				case 0x46: bytesToAdvance = 2; return $"LSR ${peeker(++pc):X2}";
				case 0x48: bytesToAdvance = 1; return "PHA";
				case 0x49: bytesToAdvance = 2; return $"EOR #${peeker(++pc):X2}";
				case 0x4A: bytesToAdvance = 1; return "LSR A";
				case 0x4C: bytesToAdvance = 3; return $"JMP ${peeker_word(++pc, peeker):X4}";
				case 0x4D: bytesToAdvance = 3; return $"EOR ${peeker_word(++pc, peeker):X4}";
				case 0x4E: bytesToAdvance = 3; return $"LSR ${peeker_word(++pc, peeker):X4}";
				case 0x50: bytesToAdvance = 2; return $"BVC ${(ushort)(pc + 2 + (sbyte)peeker(++pc)):X4}";
				case 0x51: bytesToAdvance = 2; return $"EOR (${peeker(++pc):X2}),Y *";
				case 0x54: bytesToAdvance = 2; return $"NOP ${peeker(++pc):X2},X";
				case 0x55: bytesToAdvance = 2; return $"EOR ${peeker(++pc):X2},X";
				case 0x56: bytesToAdvance = 2; return $"LSR ${peeker(++pc):X2},X";
				case 0x58: bytesToAdvance = 1; return "CLI";
				case 0x59: bytesToAdvance = 3; return $"EOR ${peeker_word(++pc, peeker):X4},Y *";
				case 0x5A: bytesToAdvance = 1; return "NOP";
				case 0x5C: bytesToAdvance = 2; return $"NOP (${peeker(++pc):X2},X)";
				case 0x5D: bytesToAdvance = 3; return $"EOR ${peeker_word(++pc, peeker):X4},X *";
				case 0x5E: bytesToAdvance = 3; return $"LSR ${peeker_word(++pc, peeker):X4},X";
				case 0x60: bytesToAdvance = 1; return "RTS";
				case 0x61: bytesToAdvance = 2; return $"ADC (${peeker(++pc):X2},X)";
				case 0x64: bytesToAdvance = 2; return $"NOP ${peeker(++pc):X2}";
				case 0x65: bytesToAdvance = 2; return $"ADC ${peeker(++pc):X2}";
				case 0x66: bytesToAdvance = 2; return $"ROR ${peeker(++pc):X2}";
				case 0x68: bytesToAdvance = 1; return "PLA";
				case 0x69: bytesToAdvance = 2; return $"ADC #${peeker(++pc):X2}";
				case 0x6A: bytesToAdvance = 1; return "ROR A";
				case 0x6C: bytesToAdvance = 3; return $"JMP (${peeker_word(++pc, peeker):X4})";
				case 0x6D: bytesToAdvance = 3; return $"ADC ${peeker_word(++pc, peeker):X4}";
				case 0x6E: bytesToAdvance = 3; return $"ROR ${peeker_word(++pc, peeker):X4}";
				case 0x70: bytesToAdvance = 2; return $"BVS ${(ushort)(pc + 2 + (sbyte)peeker(++pc)):X4}";
				case 0x71: bytesToAdvance = 2; return $"ADC (${peeker(++pc):X2}),Y *";
				case 0x74: bytesToAdvance = 2; return $"NOP ${peeker(++pc):X2},X";
				case 0x75: bytesToAdvance = 2; return $"ADC ${peeker(++pc):X2},X";
				case 0x76: bytesToAdvance = 2; return $"ROR ${peeker(++pc):X2},X";
				case 0x78: bytesToAdvance = 1; return "SEI";
				case 0x79: bytesToAdvance = 3; return $"ADC ${peeker_word(++pc, peeker):X4},Y *";
				case 0x7A: bytesToAdvance = 1; return "NOP";
				case 0x7C: bytesToAdvance = 2; return $"NOP (${peeker(++pc):X2},X)";
				case 0x7D: bytesToAdvance = 3; return $"ADC ${peeker_word(++pc, peeker):X4},X *";
				case 0x7E: bytesToAdvance = 3; return $"ROR ${peeker_word(++pc, peeker):X4},X";
				case 0x80: bytesToAdvance = 2; return $"NOP #${peeker(++pc):X2}";
				case 0x81: bytesToAdvance = 2; return $"STA (${peeker(++pc):X2},X)";
				case 0x82: bytesToAdvance = 2; return $"NOP #${peeker(++pc):X2}";
				case 0x84: bytesToAdvance = 2; return $"STY ${peeker(++pc):X2}";
				case 0x85: bytesToAdvance = 2; return $"STA ${peeker(++pc):X2}";
				case 0x86: bytesToAdvance = 2; return $"STX ${peeker(++pc):X2}";
				case 0x88: bytesToAdvance = 1; return "DEY";
				case 0x89: bytesToAdvance = 2; return $"NOP #${peeker(++pc):X2}";
				case 0x8A: bytesToAdvance = 1; return "TXA";
				case 0x8C: bytesToAdvance = 3; return $"STY ${peeker_word(++pc, peeker):X4}";
				case 0x8D: bytesToAdvance = 3; return $"STA ${peeker_word(++pc, peeker):X4}";
				case 0x8E: bytesToAdvance = 3; return $"STX ${peeker_word(++pc, peeker):X4}";
				case 0x90: bytesToAdvance = 2; return $"BCC ${(ushort)(pc + 2 + (sbyte)peeker(++pc)):X4}";
				case 0x91: bytesToAdvance = 2; return $"STA (${peeker(++pc):X2}),Y";
				case 0x94: bytesToAdvance = 2; return $"STY ${peeker(++pc):X2},X";
				case 0x95: bytesToAdvance = 2; return $"STA ${peeker(++pc):X2},X";
				case 0x96: bytesToAdvance = 2; return $"STX ${peeker(++pc):X2},Y";
				case 0x98: bytesToAdvance = 1; return "TYA";
				case 0x99: bytesToAdvance = 3; return $"STA ${peeker_word(++pc, peeker):X4},Y";
				case 0x9A: bytesToAdvance = 1; return "TXS";
				case 0x9D: bytesToAdvance = 3; return $"STA ${peeker_word(++pc, peeker):X4},X";
				case 0xA0: bytesToAdvance = 2; return $"LDY #${peeker(++pc):X2}";
				case 0xA1: bytesToAdvance = 2; return $"LDA (${peeker(++pc):X2},X)";
				case 0xA2: bytesToAdvance = 2; return $"LDX #${peeker(++pc):X2}";
				case 0xA4: bytesToAdvance = 2; return $"LDY ${peeker(++pc):X2}";
				case 0xA5: bytesToAdvance = 2; return $"LDA ${peeker(++pc):X2}";
				case 0xA6: bytesToAdvance = 2; return $"LDX ${peeker(++pc):X2}";
				case 0xA8: bytesToAdvance = 1; return "TAY";
				case 0xA9: bytesToAdvance = 2; return $"LDA #${peeker(++pc):X2}";
				case 0xAA: bytesToAdvance = 1; return "TAX";
				case 0xAC: bytesToAdvance = 3; return $"LDY ${peeker_word(++pc, peeker):X4}";
				case 0xAD: bytesToAdvance = 3; return $"LDA ${peeker_word(++pc, peeker):X4}";
				case 0xAE: bytesToAdvance = 3; return $"LDX ${peeker_word(++pc, peeker):X4}";
				case 0xB0: bytesToAdvance = 2; return $"BCS ${(ushort)(pc + 2 + (sbyte)peeker(++pc)):X4}";
				case 0xB1: bytesToAdvance = 2; return $"LDA (${peeker(++pc):X2}),Y *";
				case 0xB3: bytesToAdvance = 2; return $"LAX (${peeker(++pc):X2}),Y *";
				case 0xB4: bytesToAdvance = 2; return $"LDY ${peeker(++pc):X2},X";
				case 0xB5: bytesToAdvance = 2; return $"LDA ${peeker(++pc):X2},X";
				case 0xB6: bytesToAdvance = 2; return $"LDX ${peeker(++pc):X2},Y";
				case 0xB8: bytesToAdvance = 1; return "CLV";
				case 0xB9: bytesToAdvance = 3; return $"LDA ${peeker_word(++pc, peeker):X4},Y *";
				case 0xBA: bytesToAdvance = 1; return "TSX";
				case 0xBC: bytesToAdvance = 3; return $"LDY ${peeker_word(++pc, peeker):X4},X *";
				case 0xBD: bytesToAdvance = 3; return $"LDA ${peeker_word(++pc, peeker):X4},X *";
				case 0xBE: bytesToAdvance = 3; return $"LDX ${peeker_word(++pc, peeker):X4},Y *";
				case 0xC0: bytesToAdvance = 2; return $"CPY #${peeker(++pc):X2}";
				case 0xC1: bytesToAdvance = 2; return $"CMP (${peeker(++pc):X2},X)";
				case 0xC2: bytesToAdvance = 2; return $"NOP #${peeker(++pc):X2}";
				case 0xC4: bytesToAdvance = 2; return $"CPY ${peeker(++pc):X2}";
				case 0xC5: bytesToAdvance = 2; return $"CMP ${peeker(++pc):X2}";
				case 0xC6: bytesToAdvance = 2; return $"DEC ${peeker(++pc):X2}";
				case 0xC8: bytesToAdvance = 1; return "INY";
				case 0xC9: bytesToAdvance = 2; return $"CMP #${peeker(++pc):X2}";
				case 0xCA: bytesToAdvance = 1; return "DEX";
				case 0xCB: bytesToAdvance = 2; return $"AXS ${peeker(++pc):X2}";
				case 0xCC: bytesToAdvance = 3; return $"CPY ${peeker_word(++pc, peeker):X4}";
				case 0xCD: bytesToAdvance = 3; return $"CMP ${peeker_word(++pc, peeker):X4}";
				case 0xCE: bytesToAdvance = 3; return $"DEC ${peeker_word(++pc, peeker):X4}";
				case 0xD0: bytesToAdvance = 2; return $"BNE ${(ushort)(pc + 2 + (sbyte)peeker(++pc)):X4}";
				case 0xD1: bytesToAdvance = 2; return $"CMP (${peeker(++pc):X2}),Y *";
				case 0xD4: bytesToAdvance = 2; return $"NOP ${peeker(++pc):X2},X";
				case 0xD5: bytesToAdvance = 2; return $"CMP ${peeker(++pc):X2},X";
				case 0xD6: bytesToAdvance = 2; return $"DEC ${peeker(++pc):X2},X";
				case 0xD8: bytesToAdvance = 1; return "CLD";
				case 0xD9: bytesToAdvance = 3; return $"CMP ${peeker_word(++pc, peeker):X4},Y *";
				case 0xDA: bytesToAdvance = 1; return "NOP";
				case 0xDC: bytesToAdvance = 2; return $"NOP (${peeker(++pc):X2},X)";
				case 0xDD: bytesToAdvance = 3; return $"CMP ${peeker_word(++pc, peeker):X4},X *";
				case 0xDE: bytesToAdvance = 3; return $"DEC ${peeker_word(++pc, peeker):X4},X";
				case 0xE0: bytesToAdvance = 2; return $"CPX #${peeker(++pc):X2}";
				case 0xE1: bytesToAdvance = 2; return $"SBC (${peeker(++pc):X2},X)";
				case 0xE2: bytesToAdvance = 2; return $"NOP #${peeker(++pc):X2}";
				case 0xE4: bytesToAdvance = 2; return $"CPX ${peeker(++pc):X2}";
				case 0xE5: bytesToAdvance = 2; return $"SBC ${peeker(++pc):X2}";
				case 0xE6: bytesToAdvance = 2; return $"INC ${peeker(++pc):X2}";
				case 0xE8: bytesToAdvance = 1; return "INX";
				case 0xE9: bytesToAdvance = 2; return $"SBC #${peeker(++pc):X2}";
				case 0xEA: bytesToAdvance = 1; return "NOP";
				case 0xEC: bytesToAdvance = 3; return $"CPX ${peeker_word(++pc, peeker):X4}";
				case 0xED: bytesToAdvance = 3; return $"SBC ${peeker_word(++pc, peeker):X4}";
				case 0xEE: bytesToAdvance = 3; return $"INC ${peeker_word(++pc, peeker):X4}";
				case 0xF0: bytesToAdvance = 2; return $"BEQ ${(ushort)(pc + 2 + (sbyte)peeker(++pc)):X4}";
				case 0xF1: bytesToAdvance = 2; return $"SBC (${peeker(++pc):X2}),Y *";
				case 0xF4: bytesToAdvance = 2; return $"NOP ${peeker(++pc):X2},X";
				case 0xF5: bytesToAdvance = 2; return $"SBC ${peeker(++pc):X2},X";
				case 0xF6: bytesToAdvance = 2; return $"INC ${peeker(++pc):X2},X";
				case 0xF8: bytesToAdvance = 1; return "SED";
				case 0xF9: bytesToAdvance = 3; return $"SBC ${peeker_word(++pc, peeker):X4},Y *";
				case 0xFA: bytesToAdvance = 1; return "NOP";
				case 0xFC: bytesToAdvance = 2; return $"NOP (${peeker(++pc):X2},X)";
				case 0xFD: bytesToAdvance = 3; return $"SBC ${peeker_word(++pc, peeker):X4},X *";
				case 0xFE: bytesToAdvance = 3; return $"INC ${peeker_word(++pc, peeker):X4},X";
			}
			bytesToAdvance = 1;
			return "???";
		}
	}
}
