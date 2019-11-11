using System;
using System.Collections.Generic;
using System.Text;

namespace BizHawk.Emulation.Common.Components.I8048
{
	public sealed partial class I8048
	{
		static string[] table =
		{
			"NOP", // 00
			"???", // 01
			"OUT   BUS,A", // 02
			"ADD   A,i8", // 03
			"JP    R0", // 04
			"EI", // 05
			"???", // 06
			"DEC   A", // 07
			"IN    A,BUS", // 08
			"IN    A,P1", // 09
			"IN    A,P2", // 0a
			"???", // 0b
			"MOV   A,P4", // 0c
			"MOV   A,P5", // 0d
			"MOV   A,P6", // 0e
			"MOV   A,P7", // 0f
			"INC   @R0", // 10
			"INC   @R1", // 11
			"JPB   0", // 12
			"ADC   A,i8", // 13
			"CALL  @R0", // 14
			"DI", // 15
			"JP    TF", // 16
			"INC   A", // 17
			"INC   R0", // 18
			"INC   R1", // 19
			"INC   R2", // 1a
			"INC   R3", // 1b
			"INC   R4", // 1c
			"INC   R5", // 1d
			"INC   R6", // 1e
			"INC   R7", // 1f
			"XCH   A,@R0", // 20
			"XCH   A,@R1", // 21
			"???", // 22
			"MOV   A,i8", // 23
			"JP    R1", // 24
			"EN", // 25
			"JP    !T0", // 26
			"CLR   A", // 27
			"XCH   A,R0", // 28
			"XCH   A,R1", // 29
			"XCH   A,R2", // 2a
			"XCH   A,R3", // 2b
			"XCH   A,R4", // 2c
			"XCH   A,R5", // 2d
			"XCH   A,R6", // 2e
			"XCH   A,R7", // 2f
			"LEAX  ix16", // 30
			"LEAY  ix16", // 31
			"LEAS  ix16", // 32
			"LEAU  ix16", // 33
			"PSHS  i8", // 34
			"PULS  i8", // 35
			"PSHU  i8", // 36
			"PULU  i8", // 37
			"???", // 38
			"RTS", // 39
			"ABX", // 3a
			"RTI", // 3b
			"CWAI  i8", // 3c
			"MUL", // 3d
			"???", // 3e
			"SWI1", // 3f
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
			"SUB   D,i16", // 83
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
			"LD    X,i16", // 8e
			"???", // 8f
			"SUB   A,DP+i8", // 90
			"CMP   A,DP+i8", // 91
			"SBC   A,DP+i8", // 92
			"SUB   D,DP+i8", // 93
			"AND   A,DP+i8", // 94
			"BIT   A,DP+i8", // 95
			"LD    A,DP+i8", // 96
			"ST    A,DP+i8", // 97
			"EOR   A,DP+i8", // 98
			"ADC   A,DP+i8", // 99
			"OR    A,DP+i8", // 9a
			"ADD   A,DP+i8", // 9b
			"CMP   X,DP+i8", // 9c
			"JSR   DP+i8", // 9d
			"LD    X,DP+i8", // 9e
			"ST    X,DP+i8", // 9f
			"SUB   A,ix16", // a0
			"CMP   A,ix16", // a1
			"SBC   A,ix16", // a2
			"SUB   D,ix16", // a3
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
			"LD    X,ix16", // ae
			"ST    X,ix16", // af
			"SUB   A,ex16", // b0
			"CMP   A,ex16", // b1
			"SBC   A,ex16", // b2
			"SUB   D,ex16", // b3
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
			"LD    X,ex16", // be
			"ST    X,ex16", // bf
			"SUB   B,i8", // c0
			"CMP   B,i8", // c1
			"SBC   B,i8", // c2
			"ADD   D,i16", // c3
			"AND   B,i8", // c4
			"BIT   B,i8", // c5
			"LD    B,i8", // c6
			"???", // c7
			"EOR   B,i8", // c8
			"ADC   B,i8", // c9
			"OR    B,i8", // ca
			"ADD   B,i8", // cb
			"LD    D,i16", // cc
			"???", // cd
			"LD    U,i16", // ce
			"???", // cf
			"SUB   B,DP+i8", // d0
			"CMP   B,DP+i8", // d1
			"SBC   B,DP+i8", // d2
			"ADD   D,DP+i8", // d3
			"AND   B,DP+i8", // d4
			"BIT   B,DP+i8", // d5
			"LD    B,DP+i8", // d6
			"ST    B,DP+i8", // d7
			"EOR   B,DP+i8", // d8
			"ADC   B,DP+i8", // d9
			"OR    B,DP+i8", // da
			"ADD   B,DP+i8", // db
			"LD    D,DP+i8", // dc
			"ST    D,DP+i8", // dd
			"LD    U,DP+i8", // de
			"ST    U,DP+i8", // df
			"SUB   B,ix16", // e0
			"CMP   B,ix16", // e1
			"SBC   B,ix16", // e2
			"ADD   D,ix16", // e3
			"AND   B,ix16", // e4
			"BIT   B,ix16", // e5
			"LD    B,ix16", // e6
			"ST    B,ix16", // e7
			"EOR   B,ix16", // e8
			"ADC   B,ix16", // e9
			"OR    B,ix16", // ea
			"ADD   B,ix16", // eb
			"LD    D,ix16", // ec
			"ST    D,ix16", // ed
			"LD    U,ix16", // ee
			"ST    U,ix16", // ef
			"SUB   B,ex16", // f0
			"CMP   B,ex16", // f1
			"SBC   B,ex16", // f2
			"ADD   D,ex16", // f3
			"AND   B,ex16", // f4
			"BIT   B,ex16", // f5
			"LD    B,ex16", // f6
			"ST    B,ex16", // f7
			"EOR   B,ex16", // f8
			"ADC   B,ex16", // f9
			"OR    B,ex16", // fa
			"ADD   B,ex16", // fb
			"LD    D,ex16", // fc
			"ST    D,ex16", // fd
			"LD    U,ex16", // fe
			"ST    U,ex16", // ff
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
