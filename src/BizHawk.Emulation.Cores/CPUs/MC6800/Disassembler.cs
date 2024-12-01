using System.Collections.Generic;
using System.Text;

namespace BizHawk.Emulation.Cores.Components.MC6800
{
	public sealed partial class MC6800
	{
		private static readonly string[] table =
		{
			"???", // 00
			"NOP", // 01
			"???", // 02
			"???", // 03
			"???", // 04
			"???", // 05
			"TAP", // 06
			"TPA", // 07
			"INX", // 08
			"DEX", // 09
			"CLV", // 0a
			"SEV", // 0b
			"CLC", // 0c
			"SEC", // 0d
			"CLI", // 0e
			"SEI", // 0f
			"SBA", // 10
			"CBA", // 11
			"???", // 12
			"???", // 13
			"???", // 14
			"???", // 15
			"TAB", // 16
			"TBA", // 17
			"???", // 18
			"DAA", // 19
			"???", // 1a
			"ABA", // 1b
			"???", // 1c
			"???", // 1d
			"???", // 1e
			"???", // 1f
			"BRA   i8", // 20
			"???", // 21
			"BHI   i8", // 22
			"BLS   i8", // 23
			"BHS   i8", // 24
			"BLO   i8", // 25
			"BNE   i8", // 26
			"BEQ   i8", // 27
			"BVC   i8", // 28
			"BVS   i8", // 29
			"BPL   i8", // 2a
			"BMI   i8", // 2b
			"BGE   i8", // 2c
			"BLT   i8", // 2d
			"BGT   i8", // 2e
			"BLE   i8", // 2f
			"TSX", // 30
			"INS", // 31
			"PULA", // 32
			"PULB", // 33
			"DES", // 34
			"TXS", // 35
			"PSHA", // 36
			"PSHB", // 37
			"???", // 38
			"RTS", // 39
			"???", // 3a
			"RTI", // 3b
			"???", // 3c
			"???", // 3d
			"WAI", // 3e
			"SWI", // 3f
			"NEG   A", // 40
			"???", // 41
			"???", // 42
			"COM   A", // 43
			"LSR   A", // 44
			"???", // 45
			"ROR   A", // 46
			"ASR   A", // 47
			"ASL   A", // 48
			"ROL   A", // 49
			"DEC   A", // 4a
			"???", // 4b
			"INC   A", // 4c
			"TST   A", // 4d
			"???", // 4e
			"CLR   A", // 4f
			"NEG   B", // 50
			"???", // 51
			"???", // 52
			"COM   B", // 53
			"LSR   B", // 54
			"???", // 55
			"ROR   B", // 56
			"ASR   B", // 57
			"ASL   B", // 58
			"ROL   B", // 59
			"DEC   B", // 5a
			"???", // 5b
			"INC   B", // 5c
			"TST   B", // 5d
			"???", // 5e
			"CLR   B", // 5f
			"NEG   ix16", // 60
			"???", // 61
			"???", // 62
			"COM   ix16", // 63
			"LSR   ix16", // 64
			"???", // 65
			"ROR   ix16", // 66
			"ASR   ix16", // 67
			"ASL   ix16", // 68
			"ROL   ix16", // 69
			"DEC   ix16", // 6a
			"???", // 6b
			"INC   ix16", // 6c
			"TST   ix16", // 6d
			"JMP   ix16", // 6e
			"CLR   ix16", // 6f
			"NEG   ex16", // 70
			"???", // 71
			"???", // 72
			"COM   ex16", // 73
			"LSR   ex16", // 74
			"???", // 75
			"ROR   ex16", // 76
			"ASR   ex16", // 77
			"ASL   ex16", // 78
			"ROL   ex16", // 79
			"DEC   ex16", // 7a
			"???", // 7b
			"INC   ex16", // 7c
			"TST   ex16", // 7d
			"JMP   ex16", // 7e
			"CLR   ex16", // 7f
			"SUB   A,i8", // 80
			"CMP   A,i8", // 81
			"SBC   A,i8", // 82
			"???", // 83
			"AND   A,i8", // 84
			"BIT   A,i8", // 85
			"LD    A,i8", // 86
			"???", // 87
			"EOR   A,i8", // 88
			"ADC   A,i8", // 89
			"OR    A,i8", // 8a
			"ADD   A,i8", // 8b
			"CMP   X,i16", // 8c
			"BSR   i8", // 8d
			"LD    SP,i16", // 8e
			"???", // 8f
			"SUB   A,DP+i8", // 90
			"CMP   A,DP+i8", // 91
			"SBC   A,DP+i8", // 92
			"???", // 93
			"AND   A,DP+i8", // 94
			"BIT   A,DP+i8", // 95
			"LD    A,DP+i8", // 96
			"ST    A,DP+i8", // 97
			"EOR   A,DP+i8", // 98
			"ADC   A,DP+i8", // 99
			"OR    A,DP+i8", // 9a
			"ADD   A,DP+i8", // 9b
			"CMP   X,DP+i8", // 9c
			"???", // 9d
			"LD    SP,DP+i8", // 9e
			"ST    SP,DP+i8", // 9f
			"SUB   A,ix16", // a0
			"CMP   A,ix16", // a1
			"SBC   A,ix16", // a2
			"???", // a3
			"AND   A,ix16", // a4
			"BIT   A,ix16", // a5
			"LD    A,ix16", // a6
			"ST    A,ix16", // a7
			"EOR   A,ix16", // a8
			"ADC   A,ix16", // a9
			"OR    A,ix16", // aa
			"ADD   A,ix16", // ab
			"CMP   X,ix16", // ac
			"JSR   ix16", // ad
			"LD    SP,ix16", // ae
			"ST    SP,ix16", // af
			"SUB   A,ex16", // b0
			"CMP   A,ex16", // b1
			"SBC   A,ex16", // b2
			"???", // b3
			"AND   A,ex16", // b4
			"BIT   A,ex16", // b5
			"LD    A,ex16", // b6
			"ST    A,ex16", // b7
			"EOR   A,ex16", // b8
			"ADC   A,ex16", // b9
			"OR    A,ex16", // ba
			"ADD   A,ex16", // bb
			"CMP   X,ex16", // bc
			"JSR   ex16", // bd
			"LD    SP,ex16", // be
			"ST    SP,ex16", // bf
			"SUB   B,i8", // c0
			"CMP   B,i8", // c1
			"SBC   B,i8", // c2
			"???", // c3
			"AND   B,i8", // c4
			"BIT   B,i8", // c5
			"LD    B,i8", // c6
			"???", // c7
			"EOR   B,i8", // c8
			"ADC   B,i8", // c9
			"OR    B,i8", // ca
			"ADD   B,i8", // cb
			"???", // cc
			"???", // cd
			"LD    X,i16", // ce
			"???", // cf
			"SUB   B,DP+i8", // d0
			"CMP   B,DP+i8", // d1
			"SBC   B,DP+i8", // d2
			"???", // d3
			"AND   B,DP+i8", // d4
			"BIT   B,DP+i8", // d5
			"LD    B,DP+i8", // d6
			"ST    B,DP+i8", // d7
			"EOR   B,DP+i8", // d8
			"ADC   B,DP+i8", // d9
			"OR    B,DP+i8", // da
			"ADD   B,DP+i8", // db
			"???", // dc
			"???", // dd
			"LD    X,DP+i8", // de
			"ST    X,DP+i8", // df
			"SUB   B,ix16", // e0
			"CMP   B,ix16", // e1
			"SBC   B,ix16", // e2
			"???", // e3
			"AND   B,ix16", // e4
			"BIT   B,ix16", // e5
			"LD    B,ix16", // e6
			"ST    B,ix16", // e7
			"EOR   B,ix16", // e8
			"ADC   B,ix16", // e9
			"OR    B,ix16", // ea
			"ADD   B,ix16", // eb
			"???", // ec
			"???", // ed
			"LD    X,ix16", // ee
			"ST    X,ix16", // ef
			"SUB   B,ex16", // f0
			"CMP   B,ex16", // f1
			"SBC   B,ex16", // f2
			"???", // f3
			"AND   B,ex16", // f4
			"BIT   B,ex16", // f5
			"LD    B,ex16", // f6
			"ST    B,ex16", // f7
			"EOR   B,ex16", // f8
			"ADC   B,ex16", // f9
			"OR    B,ex16", // fa
			"ADD   B,ex16", // fb
			"???", // fc
			"???", // fd
			"LD    X,ex16", // fe
			"ST    X,ex16", // ff
		};

		public static string Disassemble(ushort addr, Func<ushort, byte> reader, out ushort size)
		{
			ushort origaddr = addr;
			List<byte> bytes = new List<byte>();
			bytes.Add(reader(addr++));

			string result = table[bytes[0]];

			if (result.Contains("i8"))
			{
				byte d = reader(addr++);
				bytes.Add(d);
				result = result.Replace("i8", string.Format("#{0:X2}h", d));
			}
			else if (result.Contains("i16"))
			{
				byte dhi = reader(addr++);
				byte dlo = reader(addr++);
				bytes.Add(dhi);
				bytes.Add(dlo);
				result = result.Replace("i16", string.Format("#{0:X2}{1:X2}h", dhi, dlo));
			}
			else if (result.Contains("ex16"))
			{
				byte dhi = reader(addr++);
				byte dlo = reader(addr++);
				bytes.Add(dhi);
				bytes.Add(dlo);
				result = result.Replace("ex16", "(" + string.Format("#{0:X2}{1:X2}h", dhi, dlo) + ")");
			}
			else if (result.Contains("ix16"))
			{
				byte d = reader(addr++);
				bytes.Add(d);

				result = result.Replace("ix16", "X + " + "ea");
				result = result.Replace("ea", string.Format("{0:N}h", d));			
			}

			StringBuilder ret = new StringBuilder();
			ret.Append(string.Format("{0:X4}:  ", origaddr));
			foreach (var b in bytes)
				ret.Append(string.Format("{0:X2} ", b));
			while (ret.Length < 22)
				ret.Append(' ');
			ret.Append(result);
			size = (ushort)(addr - origaddr);
			return ret.ToString();
		}
	}
}
