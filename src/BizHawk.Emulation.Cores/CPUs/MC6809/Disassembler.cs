using System.Collections.Generic;
using System.Text;

namespace BizHawk.Emulation.Cores.Components.MC6809
{
	public sealed partial class MC6809
	{
		private static readonly string[] table =
		{
			"NEG   DP+i8", // 00
			"???", // 01
			"???", // 02
			"COM   DP+i8", // 03
			"LSR   DP+i8", // 04
			"???", // 05
			"ROR   DP+i8", // 06
			"ASR   DP+i8", // 07
			"ASL   DP+i8", // 08
			"ROL   DP+i8", // 09
			"DEC   DP+i8", // 0a
			"???", // 0b
			"INC   DP+i8", // 0c
			"TST   DP+i8", // 0d
			"JMP   DP+i8", // 0e
			"CLR   DP+i8", // 0f
			"PAGE 2", // 10
			"PAGE 3", // 11
			"NOP", // 12
			"SYNC", // 13
			"???", // 14
			"???", // 15
			"LBRA  i16", // 16
			"LBSR  i16", // 17
			"???", // 18
			"DAA", // 19
			"ORCC  i8", // 1a
			"???", // 1b
			"ANDCC i8", // 1c
			"SEX", // 1d
			"EXG   i8", // 1e
			"TFR   i8", // 1f
			"BRA   i8", // 20
			"BRN   i8", // 21
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

		private static readonly string[] table2 =
		{
			"???", // 00
			"???", // 01
			"???", // 02
			"???", // 03
			"???", // 04
			"???", // 05
			"???", // 06
			"???", // 07
			"???", // 08
			"???", // 09
			"???", // 0a
			"???", // 0b
			"???", // 0c
			"???", // 0d
			"???", // 0e
			"???", // 0f
			"???", // 10
			"???", // 11
			"???", // 12
			"???", // 13
			"???", // 14
			"???", // 15
			"???", // 16
			"???", // 17
			"???", // 18
			"???", // 19
			"???", // 1a
			"???", // 1b
			"???", // 1c
			"???", // 1d
			"???", // 1e
			"???", // 1f
			"???", // 20
			"LBRN   i16", // 21
			"LBHI   i16", // 22
			"LBLS   i16", // 23
			"LBHS   i16", // 24
			"LBLO   i16", // 25
			"LBNE   i16", // 26
			"LBEQ   i16", // 27
			"LBVC   i16", // 28
			"LBVS   i16", // 29
			"LBPL   i16", // 2a
			"LBMI   i16", // 2b
			"LBGE   i16", // 2c
			"LBLT   i16", // 2d
			"LBGT   i16", // 2e
			"LBLE   i16", // 2f
			"???", // 30
			"???", // 31
			"???", // 32
			"???", // 33
			"???", // 34
			"???", // 35
			"???", // 36
			"???", // 37
			"???", // 38
			"???", // 39
			"???", // 3a
			"???", // 3b
			"???", // 3c
			"???", // 3d
			"???", // 3e
			"SWI2", // 3f
			"???", // 40
			"???", // 41
			"???", // 42
			"???", // 43
			"???", // 44
			"???", // 45
			"???", // 46
			"???", // 47
			"???", // 48
			"???", // 49
			"???", // 4a
			"???", // 4b
			"???", // 4c
			"???", // 4d
			"???", // 4e
			"???", // 4f
			"???", // 50
			"???", // 51
			"???", // 52
			"???", // 53
			"???", // 54
			"???", // 55
			"???", // 56
			"???", // 57
			"???", // 58
			"???", // 59
			"???", // 5a
			"???", // 5b
			"???", // 5c
			"???", // 5d
			"???", // 5e
			"???", // 5f
			"???", // 60
			"???", // 61
			"???", // 62
			"???", // 63
			"???", // 64
			"???", // 65
			"???", // 66
			"???", // 67
			"???", // 68
			"???", // 69
			"???", // 6a
			"???", // 6b
			"???", // 6c
			"???", // 6d
			"???", // 6e
			"???", // 6f
			"???", // 70
			"???", // 71
			"???", // 72
			"???", // 73
			"???", // 74
			"???", // 75
			"???", // 76
			"???", // 77
			"???", // 78
			"???", // 79
			"???", // 7a
			"???", // 7b
			"???", // 7c
			"???", // 7d
			"???", // 7e
			"???", // 7f
			"???", // 80
			"???", // 81
			"???", // 82
			"CMP   D,(i16)", // 83
			"???", // 84
			"???", // 85
			"???", // 86
			"???", // 87
			"???", // 88
			"???", // 89
			"???", // 8a
			"???", // 8b
			"CMP   Y,(i16)", // 8c
			"???", // 8d
			"LD    Y,(i16)", // 8e
			"???", // 8f
			"???", // 90
			"???", // 91
			"???", // 92
			"CMP   D,(DP+i8)", // 93
			"???", // 94
			"???", // 95
			"???", // 96
			"???", // 97
			"???", // 98
			"???", // 99
			"???", // 9a
			"???", // 9b
			"CMP   Y,(DP+i8)", // 9c
			"???", // 9d
			"LD    Y,(DP+i8)", // 9e
			"ST    Y,(DP+i8)", // 9f
			"???", // a0
			"???", // a1
			"???", // a2
			"CMP   D,ix16", // a3
			"???", // a4
			"???", // a5
			"???", // a6
			"???", // a7
			"???", // a8
			"???", // a9
			"???", // aa
			"???", // ab
			"CMP   Y,ix16", // ac
			"???", // ad
			"LD    Y,ix16", // ae
			"ST    Y,ix16", // af
			"???", // b0
			"???", // b1
			"???", // b2
			"CMP   D,ex16", // b3
			"???", // b4
			"???", // b5
			"???", // b6
			"???", // b7
			"???", // b8
			"???", // b9
			"???", // ba
			"???", // bb
			"CMP   Y,ex16", // bc
			"???", // bd
			"LD    Y,ex16", // be
			"ST    Y,ex16", // bf
			"???", // c0
			"???", // c1
			"???", // c2
			"???", // c3
			"???", // c4
			"???", // c5
			"???", // c6
			"???", // c7
			"???", // c8
			"???", // c9
			"???", // ca
			"???", // cb
			"???", // cc
			"???", // cd
			"LD    SP,i16", // ce
			"???", // cf
			"???", // d0
			"???", // d1
			"???", // d2
			"???", // d3
			"???", // d4
			"???", // d5
			"???", // d6
			"???", // d7
			"???", // d8
			"???", // d9
			"???", // da
			"???", // db
			"???", // dc
			"???", // dd
			"LD    SP,DP+i8", // de
			"ST    SP,DP+i8", // df
			"???", // e0
			"???", // e1
			"???", // e2
			"???", // e3
			"???", // e4
			"???", // e5
			"???", // e6
			"???", // e7
			"???", // e8
			"???", // e9
			"???", // ea
			"???", // eb
			"???", // ec
			"???", // ed
			"LD    SP,ix16", // ee
			"ST    SP,ix16", // ef
			"???", // f0
			"???", // f1
			"???", // f2
			"???", // f3
			"???", // f4
			"???", // f5
			"???", // f6
			"???", // f7
			"???", // f8
			"???", // f9
			"???", // fa
			"???", // fb
			"???", // fc
			"???", // fd
			"LD    SP,ex16", // fe
			"ST    SP,ex16", // ff
		};

		private static readonly string[] table3 =
		{
			"???", // 00
			"???", // 01
			"???", // 02
			"???", // 03
			"???", // 04
			"???", // 05
			"???", // 06
			"???", // 07
			"???", // 08
			"???", // 09
			"???", // 0a
			"???", // 0b
			"???", // 0c
			"???", // 0d
			"???", // 0e
			"???", // 0f
			"???", // 10
			"???", // 11
			"???", // 12
			"???", // 13
			"???", // 14
			"???", // 15
			"???", // 16
			"???", // 17
			"???", // 18
			"???", // 19
			"???", // 1a
			"???", // 1b
			"???", // 1c
			"???", // 1d
			"???", // 1e
			"???", // 1f
			"???", // 20
			"???", // 21
			"???", // 22
			"???", // 23
			"???", // 24
			"???", // 25
			"???", // 26
			"???", // 27
			"???", // 28
			"???", // 29
			"???", // 2a
			"???", // 2b
			"???", // 2c
			"???", // 2d
			"???", // 2e
			"???", // 2f
			"???", // 30
			"???", // 31
			"???", // 32
			"???", // 33
			"???", // 34
			"???", // 35
			"???", // 36
			"???", // 37
			"???", // 38
			"???", // 39
			"???", // 3a
			"???", // 3b
			"???", // 3c
			"???", // 3d
			"???", // 3e
			"SWI3", // 3f
			"???", // 40
			"???", // 41
			"???", // 42
			"???", // 43
			"???", // 44
			"???", // 45
			"???", // 46
			"???", // 47
			"???", // 48
			"???", // 49
			"???", // 4a
			"???", // 4b
			"???", // 4c
			"???", // 4d
			"???", // 4e
			"???", // 4f
			"???", // 50
			"???", // 51
			"???", // 52
			"???", // 53
			"???", // 54
			"???", // 55
			"???", // 56
			"???", // 57
			"???", // 58
			"???", // 59
			"???", // 5a
			"???", // 5b
			"???", // 5c
			"???", // 5d
			"???", // 5e
			"???", // 5f
			"???", // 60
			"???", // 61
			"???", // 62
			"???", // 63
			"???", // 64
			"???", // 65
			"???", // 66
			"???", // 67
			"???", // 68
			"???", // 69
			"???", // 6a
			"???", // 6b
			"???", // 6c
			"???", // 6d
			"???", // 6e
			"???", // 6f
			"???", // 70
			"???", // 71
			"???", // 72
			"???", // 73
			"???", // 74
			"???", // 75
			"???", // 76
			"???", // 77
			"???", // 78
			"???", // 79
			"???", // 7a
			"???", // 7b
			"???", // 7c
			"???", // 7d
			"???", // 7e
			"???", // 7f
			"???", // 80
			"???", // 81
			"???", // 82
			"CMP   US,(i16)", // 83
			"???", // 84
			"???", // 85
			"???", // 86
			"???", // 87
			"???", // 88
			"???", // 89
			"???", // 8a
			"???", // 8b
			"CMP   SP,(i16)", // 8c
			"???", // 8d
			"???", // 8e
			"???", // 8f
			"???", // 90
			"???", // 91
			"???", // 92
			"CMP   US,(DP+i8)", // 93
			"???", // 94
			"???", // 95
			"???", // 96
			"???", // 97
			"???", // 98
			"???", // 99
			"???", // 9a
			"???", // 9b
			"CMP   SP,(DP+i8)", // 9c
			"???", // 9d
			"???", // 9e
			"???", // 9f
			"???", // a0
			"???", // a1
			"???", // a2
			"CMP   US,ix16", // a3
			"???", // a4
			"???", // a5
			"???", // a6
			"???", // a7
			"???", // a8
			"???", // a9
			"???", // aa
			"???", // ab
			"CMP   SP,ix16", // ac
			"???", // ad
			"???", // ae
			"???", // af
			"???", // b0
			"???", // b1
			"???", // b2
			"CMP   US,ex16", // b3
			"???", // b4
			"???", // b5
			"???", // b6
			"???", // b7
			"???", // b8
			"???", // b9
			"???", // ba
			"???", // bb
			"CMP   SP,ex16", // bc
			"???", // bd
			"???", // be
			"???", // bf
			"???", // c0
			"???", // c1
			"???", // c2
			"???", // c3
			"???", // c4
			"???", // c5
			"???", // c6
			"???", // c7
			"???", // c8
			"???", // c9
			"???", // ca
			"???", // cb
			"???", // cc
			"???", // cd
			"???", // ce
			"???", // cf
			"???", // d0
			"???", // d1
			"???", // d2
			"???", // d3
			"???", // d4
			"???", // d5
			"???", // d6
			"???", // d7
			"???", // d8
			"???", // d9
			"???", // da
			"???", // db
			"???", // dc
			"???", // dd
			"???", // de
			"???", // df
			"???", // e0
			"???", // e1
			"???", // e2
			"???", // e3
			"???", // e4
			"???", // e5
			"???", // e6
			"???", // e7
			"???", // e8
			"???", // e9
			"???", // ea
			"???", // eb
			"???", // ec
			"???", // ed
			"???", // ee
			"???", // ef
			"???", // f0
			"???", // f1
			"???", // f2
			"???", // f3
			"???", // f4
			"???", // f5
			"???", // f6
			"???", // f7
			"???", // f8
			"???", // f9
			"???", // fa
			"???", // fb
			"???", // fc
			"???", // fd
			"???", // fe
			"???", // ff
		};

		public static string Disassemble(ushort addr, Func<ushort, byte> reader, out ushort size)
		{
			ushort origaddr = addr;
			List<byte> bytes = new List<byte>();
			bytes.Add(reader(addr++));

			string result = table[bytes[0]];
			if (bytes[0] == 0x10)
			{
				bytes.Add(reader(addr++));
				result = table2[bytes[1]];
			}

			if (bytes[0] == 0x11)
			{
				bytes.Add(reader(addr++));
				result = table3[bytes[1]];
			}

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

				string temp_reg = "";

				switch ((d >> 5) & 3)
				{
					case 0: temp_reg = "X"; break;
					case 1: temp_reg = "Y"; break;
					case 2: temp_reg = "US"; break;
					case 3: temp_reg = "SP"; break;
				}

				if ((d & 0x80) == 0)
				{
					short tempdis = (short)(d & 0x1F);
					if (tempdis >= 16)
						tempdis -= 32;

					result = result.Replace("ix16", temp_reg + " + ea");
					result = result.Replace("ea", string.Format("{0:N}h", tempdis));
				}
				else
				{
					if ((d & 0x10) == 0x10)
					{
						switch (d & 0xF)
						{
							case 0x0:
								result = result.Replace("ix16", "???");
								break;
							case 0x1:
								result = result.Replace("ix16","(" + temp_reg + ")++");
								break;
							case 0x2:
								result = result.Replace("ix16", "???");
								break;
							case 0x3:
								result = result.Replace("ix16", "--(" + temp_reg + ")");
								break;
							case 0x4:
								result = result.Replace("ix16", "(" + temp_reg + ")");
								break;
							case 0x5:
								result = result.Replace("ix16", "(" + temp_reg + " + B)");
								break;
							case 0x6:
								result = result.Replace("ix16", "(" + temp_reg + " + A)");
								break;
							case 0x7:
								result = result.Replace("ix16", "???");
								break;
							case 0x8:
								byte e = reader(addr++);
								bytes.Add(e);
								result = result.Replace("ix16", "(" + temp_reg + " + ea)");
								result = result.Replace("ea", string.Format("{0:X2}h", e));
								break;
							case 0x9:
								byte f = reader(addr++);
								bytes.Add(f);
								byte g = reader(addr++);
								bytes.Add(g);
								result = result.Replace("ix16", "(" + temp_reg + " + ea)");
								result = result.Replace("ea", string.Format("{0:X2}{1:X2}h", f, g));
								break;
							case 0xA:
								result = result.Replace("ix16", "???");
								break;
							case 0xB:
								result = result.Replace("ix16", "(" + temp_reg + " + D)");
								break;
							case 0xC:
								temp_reg = "PC";
								byte h = reader(addr++);
								bytes.Add(h);
								result = result.Replace("ix16", "(" + temp_reg + " + ea)");
								result = result.Replace("ea", string.Format("{0:X2}h", h));
								break;
							case 0xD:
								temp_reg = "PC";
								byte i = reader(addr++);
								bytes.Add(i);
								byte j = reader(addr++);
								bytes.Add(j);
								result = result.Replace("ix16", "(" + temp_reg + " + ea)");
								result = result.Replace("ea", string.Format("{0:X2}{1:X2}h", i, j));
								break;
							case 0xE:
								result = result.Replace("ix16", "???");
								break;
							case 0xF:
								if (((d >> 5) & 3) == 0)
								{
									byte k = reader(addr++);
									bytes.Add(k);
									byte l = reader(addr++);
									bytes.Add(l);
									result = result.Replace("ix16", "(" + string.Format("{0:X2}{1:X2}h", k, l) + ")");
								}
								else
								{
									result = result.Replace("ix16", "???");
								}
								break;
						}
					}
					else
					{
						switch (d & 0xF)
						{
							case 0x0:
								result = result.Replace("ix16", temp_reg + "+");
								break;
							case 0x1:
								result = result.Replace("ix16", temp_reg + "++");
								break;
							case 0x2:
								result = result.Replace("ix16", "-" + temp_reg);
								break;
							case 0x3:
								result = result.Replace("ix16", "--" + temp_reg);
								break;
							case 0x4:
								result = result.Replace("ix16", temp_reg);
								break;
							case 0x5:
								result = result.Replace("ix16", temp_reg + " + B");
								break;
							case 0x6:
								result = result.Replace("ix16", temp_reg + " + A");
								break;
							case 0x7:
								result = result.Replace("ix16", "???");
								break;
							case 0x8:
								byte e = reader(addr++);
								bytes.Add(e);
								result = result.Replace("ix16", temp_reg + " + ea");
								result = result.Replace("ea", string.Format("{0:X2}h", e));
								break;
							case 0x9:
								byte f = reader(addr++);
								bytes.Add(f);
								byte g = reader(addr++);
								bytes.Add(g);
								result = result.Replace("ix16", temp_reg + " + ea");
								result = result.Replace("ea", string.Format("{0:X2}{1:X2}h", f, g));
								break;
							case 0xA:
								result = result.Replace("ix16", "???");
								break;
							case 0xB:
								result = result.Replace("ix16", temp_reg + " + D");
								break;
							case 0xC:
								temp_reg = "PC";
								byte h = reader(addr++);
								bytes.Add(h);
								result = result.Replace("ix16", temp_reg + " + ea");
								result = result.Replace("ea", string.Format("{0:X2}h", h));
								break;
							case 0xD:
								temp_reg = "PC";
								byte i = reader(addr++);
								bytes.Add(i);
								byte j = reader(addr++);
								bytes.Add(j);
								result = result.Replace("ix16", temp_reg + " + ea");
								result = result.Replace("ea", string.Format("{0:X2}{1:X2}h", i, j));
								break;
							case 0xE:
								result = result.Replace("ix16", "???");
								break;
							case 0xF:
								result = result.Replace("ix16", "???");
								break;
						}
					}
				}				
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
