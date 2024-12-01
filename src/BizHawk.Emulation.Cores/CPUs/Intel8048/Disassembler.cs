using System.Collections.Generic;
using System.Text;

namespace BizHawk.Emulation.Cores.Components.I8048
{
	public sealed partial class I8048
	{
		private static readonly string[] table =
		{
			"NOP", // 00
			"???", // 01
			"OUT   BUS,A", // 02
			"ADD   A,i8", // 03
			"JP    2K 0,i8", // 04
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
			"JPB   0,i8", // 12
			"ADC   A,i8", // 13
			"CALL  0,i8", // 14
			"DI", // 15
			"JP    TF,i8", // 16
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
			"JP    2K 1,i8", // 24
			"EN", // 25
			"JP    !T0,i8", // 26
			"CLR   A", // 27
			"XCH   A,R0", // 28
			"XCH   A,R1", // 29
			"XCH   A,R2", // 2a
			"XCH   A,R3", // 2b
			"XCH   A,R4", // 2c
			"XCH   A,R5", // 2d
			"XCH   A,R6", // 2e
			"XCH   A,R7", // 2f
			"XCHD  A,@R0", // 30
			"XCHD  A,@R1", // 31
			"JPB   1,i8", // 32
			"???", // 33
			"CALL  1,i8", // 34
			"DN", // 35
			"JP    T0,i8", // 36
			"COM   A", // 37
			"???", // 38
			"OUT   P1", // 39
			"OUT   P2", // 3a
			"???", // 3b
			"MOV   P4,A", // 3c
			"MOV   P5,A", // 3d
			"MOV   P6,A", // 3e
			"MOV   P7,A", // 3f
			"OR    A,@R0", // 40
			"OR    A,@R1", // 41
			"MOV   A,TIM", // 42
			"OR    A,i8", // 43
			"JP    2K 2,i8", // 44
			"START CNT", // 45
			"JP    NT1,i8", // 46
			"SWP", // 47
			"OR    A,R0", // 48
			"OR    A,R1", // 49
			"OR    A,R2", // 4a
			"OR    A,R3", // 4b
			"OR    A,R4", // 4c
			"OR    A,R5", // 4d
			"OR    A,R6", // 4e
			"OR    A,R7", // 4f
			"AND   A,@R0", // 50
			"AND   A,@R1", // 51
			"JPB   2,i8", // 52
			"AND   A,i8", // 53
			"CALL  2,i8", // 54
			"START TIM", // 55
			"JP    T1,i8", // 56
			"DAA", // 57
			"AND   A,R0", // 58
			"AND   A,R1", // 59
			"AND   A,R2", // 5a
			"AND   A,R3", // 5b
			"AND   A,R4", // 5c
			"AND   A,R5", // 5d
			"AND   A,R6", // 5e
			"AND   A,R7", // 5f
			"ADD   A,@R0", // 60
			"ADD   A,@R1", // 61
			"MOV   TIM,A", // 62
			"???", // 63
			"JP    2K 3,i8", // 64
			"STOP  CNT", // 65
			"???", // 66
			"RRC", // 67
			"ADD   A,R0", // 68
			"ADD   A,R1", // 69
			"ADD   A,R2", // 6a
			"ADD   A,R3", // 6b
			"ADD   A,R4", // 6c
			"ADD   A,R5", // 6d
			"ADD   A,R6", // 6e
			"ADD   A,R7", // 6f
			"ADC   A,@R0", // 70
			"ADC   A,@R1", // 71
			"JPB   3,i8", // 72
			"???", // 73
			"CALL  3,i8", // 74
			"ENT0  CLK", // 75
			"JP    F1,i8", // 76
			"ROR", // 77
			"ADC   A,R0", // 78
			"ADC   A,R1", // 79
			"ADC   A,R2", // 7a
			"ADC   A,R3", // 7b
			"ADC   A,R4", // 7c
			"ADC   A,R5", // 7d
			"ADC   A,R6", // 7e
			"ADC   A,R7", // 7f
			"MOVX  A,@R0", // 80
			"MOVX  A,@R1", // 81
			"???", // 82
			"RET", // 83
			"JP    2K 4,i8", // 84
			"CLR   F0", // 85
			"JP    !IRQ,i8", // 86
			"???", // 87
			"OR    BUS,i8", // 88
			"OR    P1,i8", // 89
			"OR    P2,i8", // 8a
			"???", // 8b
			"OR    P4,A", // 8c
			"OR    P5,A", // 8d
			"OR    P6,A", // 8e
			"OR    P7,A", // 8f
			"MOVX  @R0,A", // 90
			"MOVX  @R1,A", // 91
			"JPB   4,i8", // 92
			"RETR", // 93
			"CALL  4,i8", // 94
			"COM   F0", // 95
			"JP    A!=0,i8", // 96
			"CLR   C", // 97
			"AND   BUS,i8", // 98
			"AND   P1,i8", // 99
			"AND   P2,i8", // 9a
			"???", // 9b
			"AND   P4,A", // 9c
			"AND   P5,A", // 9d
			"AND   P6,A", // 9e
			"AND   P7,A", // 9f
			"MOV   @R0,A", // a0
			"MOV   @R1,A", // a1
			"???", // a2
			"MOV   A,@A", // a3
			"JP    2K 5,i8", // a4
			"CLR   F1", // a5
			"???", // a6
			"COM   C", // a7
			"MOV   R0,A", // a8
			"MOV   R1,A", // a9
			"MOV   R2,A", // aa
			"MOV   R3,A", // ab
			"MOV   R4,A", // ac
			"MOV   R5,A", // ad
			"MOV   R6,A", // ae
			"MOV   R7,A", // af
			"MOV   @R0,i8", // b0
			"MOV   @R1,i8", // b1
			"JPB   5,i8", // b2
			"JPP   A", // b3
			"CALL  5,i8", // b4
			"COM   F1", // b5
			"JP    F0,i8", // b6
			"???", // b7
			"MOV   R0,i8", // b8
			"MOV   R1,i8", // b9
			"MOV   R2,i8", // ba
			"MOV   R3,i8", // bb
			"MOV   R4,i8", // bc
			"MOV   R5,i8", // bd
			"MOV   R6,i8", // be
			"MOV   R7,i8", // bf
			"???", // c0
			"???", // c1
			"???", // c2
			"???", // c3
			"JP    2K 6,i8", // c4
			"SEL   RB 0", // c5
			"JP    A==0,i8", // c6
			"MOV   A,PSW", // c7
			"DEC   R0", // c8
			"DEC   R1", // c9
			"DEC   R2", // ca
			"DEC   R3", // cb
			"DEC   R4", // cc
			"DEC   R5", // cd
			"DEC   R6", // ce
			"DEC   R7", // cf
			"XOR   A,@R0", // d0
			"XOR   A,@R1", // d1
			"JPB   6,i8", // d2
			"XOR   A,i8", // d3
			"CALL  6,i8", // d4
			"SEL   RB 1", // d5
			"???", // d6
			"MOV   PSW,A", // d7
			"XOR   A,R0", // d8
			"XOR   A,R1", // d9
			"XOR   A,R2", // da
			"XOR   A,R3", // db
			"XOR   A,R4", // dc
			"XOR   A,R5", // dd
			"XOR   A,R6", // de
			"XOR   A,R7", // df
			"???", // e0
			"???", // e1
			"???", // e2
			"MOV3  A,@A", // e3
			"JP    2K 7,i8", // e4
			"SEL   MB 0", // e5
			"JP    NC,i8", // e6
			"ROL", // e7
			"DJNZ  R0,i8", // e8
			"DJNZ  R1,i8", // e9
			"DJNZ  R2,i8", // ea
			"DJNZ  R3,i8", // eb
			"DJNZ  R4,i8", // ec
			"DJNZ  R5,i8", // ed
			"DJNZ  R6,i8", // ee
			"DJNZ  R7,i8", // ef
			"MOV   A,@R0", // f0
			"MOV   A,@R1", // f1
			"JPB   7,i8", // f2
			"???", // f3
			"CALL  7,i8", // f4
			"SEL   MB 1", // f5
			"JP    C,i8", // f6
			"RLC", // f7
			"MOV   A,R0", // f8
			"MOV   A,R1", // f9
			"MOV   A,R2", // fa
			"MOV   A,R3", // fb
			"MOV   A,R4", // fc
			"MOV   A,R5", // fd
			"MOV   A,R6", // fe
			"MOV   A,R7" // ff
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
