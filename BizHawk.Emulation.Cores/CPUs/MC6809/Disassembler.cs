using System;
using System.Collections.Generic;
using System.Text;

namespace BizHawk.Emulation.Common.Components.MC6809
{
	public sealed partial class MC6809
	{
		static string[] table =
		{
			"NEG   DP,i8", // 00
			"???", // 01
			"???", // 02
			"COM   DP,i8", // 03
			"LSR   DP,i8", // 04
			"???", // 05
			"ROR   DP,i8", // 06
			"ASR   DP,i8", // 07
			"ASL   DP,i8", // 08
			"ROL   DP,i8", // 09
			"DEC   DP,i8", // 0a
			"???", // 0b
			"INC   DP,i8", // 0c
			"TST   DP,i8", // 0d
			"JMP   DP,i8", // 0e
			"CLR   DP,i8", // 0f
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
			"CWAI", // 3c
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
			"ADD  A,B", // 80
			"ADD  A,C", // 81
			"ADD  A,D", // 82
			"ADD  A,E", // 83
			"ADD  A,H", // 84
			"ADD  A,L", // 85
			"ADD  A,(HL)", // 86
			"ADD  A,A", // 87
			"ADC  A,B", // 88
			"ADC  A,C", // 89
			"ADC  A,D", // 8a
			"ADC  A,E", // 8b
			"ADC  A,H", // 8c
			"ADC  A,L", // 8d
			"ADC  A,(HL)", // 8e
			"ADC  A,A", // 8f
			"SUB  B", // 90
			"SUB  C", // 91
			"SUB  D", // 92
			"SUB  E", // 93
			"SUB  H", // 94
			"SUB  L", // 95
			"SUB  (HL)", // 96
			"SUB  A", // 97
			"SBC  A,B", // 98
			"SBC  A,C", // 99
			"SBC  A,D", // 9a
			"SBC  A,E", // 9b
			"SBC  A,H", // 9c
			"SBC  A,L", // 9d
			"SBC  A,(HL)", // 9e
			"SBC  A,A", // 9f
			"AND  B", // a0
			"AND  C", // a1
			"AND  D", // a2
			"AND  E", // a3
			"AND  H", // a4
			"AND  L", // a5
			"AND  (HL)", // a6
			"AND  A", // a7
			"XOR  B", // a8
			"XOR  C", // a9
			"XOR  D", // aa
			"XOR  E", // ab
			"XOR  H", // ac
			"XOR  L", // ad
			"XOR  (HL)", // ae
			"XOR  A", // af
			"OR   B", // b0
			"OR   C", // b1
			"OR   D", // b2
			"OR   E", // b3
			"OR   H", // b4
			"OR   L", // b5
			"OR   (HL)", // b6
			"OR   A", // b7
			"CP   B", // b8
			"CP   C", // b9
			"CP   D", // ba
			"CP   E", // bb
			"CP   H", // bc
			"CP   L", // bd
			"CP   (HL)", // be
			"CP   A", // bf
			"RET  NZ", // c0
			"POP  BC", // c1
			"JP   NZ,a16", // c2
			"JP   a16", // c3
			"CALL NZ,a16", // c4
			"PUSH BC", // c5
			"ADD  A,d8", // c6
			"RST  00H", // c7
			"RET  Z", // c8
			"RET", // c9
			"JP   Z,a16", // ca
			"PREFIX CB", // cb
			"CALL Z,a16", // cc
			"CALL a16", // cd
			"ADC  A,d8", // ce
			"RST  08H", // cf
			"RET  NC", // d0
			"POP  DE", // d1
			"JP   NC,a16", // d2
			"???", // d3
			"CALL NC,a16", // d4
			"PUSH DE", // d5
			"SUB  d8", // d6
			"RST  10H", // d7
			"RET  C", // d8
			"RETI", // d9
			"JP   C,a16", // da
			"???", // db
			"CALL C,a16", // dc
			"???", // dd
			"SBC  A,d8", // de
			"RST  18H", // df
			"LDH  (a8),A", // e0
			"POP  HL", // e1
			"LD   (C),A", // e2
			"???", // e3
			"???", // e4
			"PUSH HL", // e5
			"AND  d8", // e6
			"RST  20H", // e7
			"ADD  SP,r8", // e8
			"JP   (HL)", // e9
			"LD   (a16),A", // ea
			"???", // eb
			"???", // ec
			"???", // ed
			"XOR  d8", // ee
			"RST  28H", // ef
			"LDH  A,(a8)", // f0
			"POP  AF", // f1
			"LD   A,(C)", // f2
			"DI", // f3
			"???", // f4
			"PUSH AF", // f5
			"OR   d8", // f6
			"RST  30H", // f7
			"LD   HL,SP+r8", // f8
			"LD   SP,HL", // f9
			"LD   A,(a16)", // fa
			"EI   ", // fb
			"???", // fc
			"???", // fd
			"CP   d8", // fe
			"RST  38H", // ff
		};

		static string[] table2 =
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
			"AND  E", // a3
			"???", // a4
			"???", // a5
			"???", // a6
			"???", // a7
			"???", // a8
			"???", // a9
			"???", // aa
			"???", // ab
			"XOR  H", // ac
			"???", // ad
			"XOR  (HL)", // ae
			"XOR  A", // af
			"OR   B", // b0
			"OR   C", // b1
			"OR   D", // b2
			"OR   E", // b3
			"OR   H", // b4
			"OR   L", // b5
			"OR   (HL)", // b6
			"OR   A", // b7
			"CP   B", // b8
			"CP   C", // b9
			"CP   D", // ba
			"CP   E", // bb
			"CP   H", // bc
			"CP   L", // bd
			"CP   (HL)", // be
			"CP   A", // bf
			"RET  NZ", // c0
			"POP  BC", // c1
			"JP   NZ,a16", // c2
			"JP   a16", // c3
			"CALL NZ,a16", // c4
			"PUSH BC", // c5
			"ADD  A,d8", // c6
			"RST  00H", // c7
			"RET  Z", // c8
			"RET", // c9
			"JP   Z,a16", // ca
			"PREFIX CB", // cb
			"CALL Z,a16", // cc
			"CALL a16", // cd
			"ADC  A,d8", // ce
			"RST  08H", // cf
			"RET  NC", // d0
			"POP  DE", // d1
			"JP   NC,a16", // d2
			"???", // d3
			"CALL NC,a16", // d4
			"PUSH DE", // d5
			"SUB  d8", // d6
			"RST  10H", // d7
			"RET  C", // d8
			"RETI", // d9
			"JP   C,a16", // da
			"???", // db
			"CALL C,a16", // dc
			"???", // dd
			"SBC  A,d8", // de
			"RST  18H", // df
			"LDH  (a8),A", // e0
			"POP  HL", // e1
			"LD   (C),A", // e2
			"???", // e3
			"???", // e4
			"PUSH HL", // e5
			"AND  d8", // e6
			"RST  20H", // e7
			"ADD  SP,r8", // e8
			"JP   (HL)", // e9
			"LD   (a16),A", // ea
			"???", // eb
			"???", // ec
			"???", // ed
			"XOR  d8", // ee
			"RST  28H", // ef
			"LDH  A,(a8)", // f0
			"POP  AF", // f1
			"LD   A,(C)", // f2
			"DI", // f3
			"???", // f4
			"PUSH AF", // f5
			"OR   d8", // f6
			"RST  30H", // f7
			"LD   HL,SP+r8", // f8
			"LD   SP,HL", // f9
			"LD   A,(a16)", // fa
			"EI   ", // fb
			"???", // fc
			"???", // fd
			"CP   d8", // fe
			"RST  38H", // ff
		};

		static string[] table3 =
		{
			"NEG   DP,i8", // 00
			"???", // 01
			"???", // 02
			"COM   DP,i8", // 03
			"LSR   DP,i8", // 04
			"???", // 05
			"ROR   DP,i8", // 06
			"ASR   DP,i8", // 07
			"ASL   DP,i8", // 08
			"ROL   DP,i8", // 09
			"DEC   DP,i8", // 0a
			"???", // 0b
			"INC   DP,i8", // 0c
			"TST   DP,i8", // 0d
			"JMP   DP,i8", // 0e
			"CLR   DP,i8", // 0f
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
			"CWAI", // 3c
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
			"ADD  A,B", // 80
			"ADD  A,C", // 81
			"ADD  A,D", // 82
			"ADD  A,E", // 83
			"ADD  A,H", // 84
			"ADD  A,L", // 85
			"ADD  A,(HL)", // 86
			"ADD  A,A", // 87
			"ADC  A,B", // 88
			"ADC  A,C", // 89
			"ADC  A,D", // 8a
			"ADC  A,E", // 8b
			"ADC  A,H", // 8c
			"ADC  A,L", // 8d
			"ADC  A,(HL)", // 8e
			"ADC  A,A", // 8f
			"SUB  B", // 90
			"SUB  C", // 91
			"SUB  D", // 92
			"SUB  E", // 93
			"SUB  H", // 94
			"SUB  L", // 95
			"SUB  (HL)", // 96
			"SUB  A", // 97
			"SBC  A,B", // 98
			"SBC  A,C", // 99
			"SBC  A,D", // 9a
			"SBC  A,E", // 9b
			"SBC  A,H", // 9c
			"SBC  A,L", // 9d
			"SBC  A,(HL)", // 9e
			"SBC  A,A", // 9f
			"AND  B", // a0
			"AND  C", // a1
			"AND  D", // a2
			"AND  E", // a3
			"AND  H", // a4
			"AND  L", // a5
			"AND  (HL)", // a6
			"AND  A", // a7
			"XOR  B", // a8
			"XOR  C", // a9
			"XOR  D", // aa
			"XOR  E", // ab
			"XOR  H", // ac
			"XOR  L", // ad
			"XOR  (HL)", // ae
			"XOR  A", // af
			"OR   B", // b0
			"OR   C", // b1
			"OR   D", // b2
			"OR   E", // b3
			"OR   H", // b4
			"OR   L", // b5
			"OR   (HL)", // b6
			"OR   A", // b7
			"CP   B", // b8
			"CP   C", // b9
			"CP   D", // ba
			"CP   E", // bb
			"CP   H", // bc
			"CP   L", // bd
			"CP   (HL)", // be
			"CP   A", // bf
			"RET  NZ", // c0
			"POP  BC", // c1
			"JP   NZ,a16", // c2
			"JP   a16", // c3
			"CALL NZ,a16", // c4
			"PUSH BC", // c5
			"ADD  A,d8", // c6
			"RST  00H", // c7
			"RET  Z", // c8
			"RET", // c9
			"JP   Z,a16", // ca
			"PREFIX CB", // cb
			"CALL Z,a16", // cc
			"CALL a16", // cd
			"ADC  A,d8", // ce
			"RST  08H", // cf
			"RET  NC", // d0
			"POP  DE", // d1
			"JP   NC,a16", // d2
			"???", // d3
			"CALL NC,a16", // d4
			"PUSH DE", // d5
			"SUB  d8", // d6
			"RST  10H", // d7
			"RET  C", // d8
			"RETI", // d9
			"JP   C,a16", // da
			"???", // db
			"CALL C,a16", // dc
			"???", // dd
			"SBC  A,d8", // de
			"RST  18H", // df
			"LDH  (a8),A", // e0
			"POP  HL", // e1
			"LD   (C),A", // e2
			"???", // e3
			"???", // e4
			"PUSH HL", // e5
			"AND  d8", // e6
			"RST  20H", // e7
			"ADD  SP,r8", // e8
			"JP   (HL)", // e9
			"LD   (a16),A", // ea
			"???", // eb
			"???", // ec
			"???", // ed
			"XOR  d8", // ee
			"RST  28H", // ef
			"LDH  A,(a8)", // f0
			"POP  AF", // f1
			"LD   A,(C)", // f2
			"DI", // f3
			"???", // f4
			"PUSH AF", // f5
			"OR   d8", // f6
			"RST  30H", // f7
			"LD   HL,SP+r8", // f8
			"LD   SP,HL", // f9
			"LD   A,(a16)", // fa
			"EI   ", // fb
			"???", // fc
			"???", // fd
			"CP   d8", // fe
			"RST  38H", // ff
		};

		public static string Disassemble(ushort addr, Func<ushort, byte> reader, out ushort size)
		{
			ushort origaddr = addr;
			List<byte> bytes = new List<byte>();
			bytes.Add(reader(addr++));

			string result = table[bytes[0]];
			if (bytes[0] == 0xcb)
			{
				bytes.Add(reader(addr++));
				result = table[bytes[1] + 256];
			}

			if (result.Contains("d8"))
			{
				byte d = reader(addr++);
				bytes.Add(d);
				result = result.Replace("d8", string.Format("#{0:X2}h", d));
			}
			else if (result.Contains("d16"))
			{
				byte dlo = reader(addr++);
				byte dhi = reader(addr++);
				bytes.Add(dlo);
				bytes.Add(dhi);
				result = result.Replace("d16", string.Format("#{0:X2}{1:X2}h", dhi, dlo));
			}
			else if (result.Contains("a16"))
			{
				byte dlo = reader(addr++);
				byte dhi = reader(addr++);
				bytes.Add(dlo);
				bytes.Add(dhi);
				result = result.Replace("a16", string.Format("#{0:X2}{1:X2}h", dhi, dlo));
			}
			else if (result.Contains("a8"))
			{
				byte d = reader(addr++);
				bytes.Add(d);
				result = result.Replace("a8", string.Format("#FF{0:X2}h", d));
			}
			else if (result.Contains("r8"))
			{
				byte d = reader(addr++);
				bytes.Add(d);
				int offs = d;
				if (offs >= 128)
					offs -= 256;
				result = result.Replace("r8", string.Format("{0:X4}h", (ushort)(addr + offs)));
			}
			StringBuilder ret = new StringBuilder();
			ret.Append(string.Format("{0:X4}:  ", origaddr));
			foreach (var b in bytes)
				ret.Append(string.Format("{0:X2} ", b));
			while (ret.Length < 17)
				ret.Append(' ');
			ret.Append(result);
			size = (ushort)(addr - origaddr);
			return ret.ToString();
		}
	}
}
