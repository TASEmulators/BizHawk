using System.Collections.Generic;
using System.Text;

namespace BizHawk.Emulation.Cores.Components.LR35902
{
	// adapted from http://www.pastraiser.com/cpu/gameboy/gameboy_opcodes.html
	// and https://rgbds.gbdev.io/docs/gbz80.7
	public sealed partial class LR35902
	{	
		private static readonly string[] table =
		{
			"NOP", // 00
			"LD   BC,d16", // 01
			"LD   (BC),A", // 02
			"INC  BC", // 03
			"INC  B", // 04
			"DEC  B", // 05
			"LD   B,d8", // 06
			"RLCA", // 07
			"LD   (a16),SP", // 08
			"ADD  HL,BC", // 09
			"LD   A,(BC)", // 0a
			"DEC  BC", // 0b
			"INC  C", // 0c
			"DEC  C", // 0d
			"LD   C,d8", // 0e
			"RRCA", // 0f
			"STOP d8", // 10
			"LD   DE,d16", // 11
			"LD   (DE),A", // 12
			"INC  DE", // 13
			"INC  D", // 14
			"DEC  D", // 15
			"LD   D,d8", // 16
			"RLA", // 17
			"JR   r8", // 18
			"ADD  HL,DE", // 19
			"LD   A,(DE)", // 1a
			"DEC  DE", // 1b
			"INC  E", // 1c
			"DEC  E", // 1d
			"LD   E,d8", // 1e
			"RRA", // 1f
			"JR   NZ,r8", // 20
			"LD   HL,d16", // 21
			"LD   (HL+),A", // 22
			"INC  HL", // 23
			"INC  H", // 24
			"DEC  H", // 25
			"LD   H,d8", // 26
			"DAA", // 27
			"JR   Z,r8", // 28
			"ADD  HL,HL", // 29
			"LD   A,(HL+)", // 2a
			"DEC  HL", // 2b
			"INC  L", // 2c
			"DEC  L", // 2d
			"LD   L,d8", // 2e
			"CPL", // 2f
			"JR   NC,r8", // 30
			"LD   SP,d16", // 31
			"LD   (HL-),A", // 32
			"INC  SP", // 33
			"INC  (HL)", // 34
			"DEC  (HL)", // 35
			"LD   (HL),d8", // 36
			"SCF", // 37
			"JR   C,r8", // 38
			"ADD  HL,SP", // 39
			"LD   A,(HL-)", // 3a
			"DEC  SP", // 3b
			"INC  A", // 3c
			"DEC  A", // 3d
			"LD   A,d8", // 3e
			"CCF", // 3f
			"LD   B,B", // 40
			"LD   B,C", // 41
			"LD   B,D", // 42
			"LD   B,E", // 43
			"LD   B,H", // 44
			"LD   B,L", // 45
			"LD   B,(HL)", // 46
			"LD   B,A", // 47
			"LD   C,B", // 48
			"LD   C,C", // 49
			"LD   C,D", // 4a
			"LD   C,E", // 4b
			"LD   C,H", // 4c
			"LD   C,L", // 4d
			"LD   C,(HL)", // 4e
			"LD   C,A", // 4f
			"LD   D,B", // 50
			"LD   D,C", // 51
			"LD   D,D", // 52
			"LD   D,E", // 53
			"LD   D,H", // 54
			"LD   D,L", // 55
			"LD   D,(HL)", // 56
			"LD   D,A", // 57
			"LD   E,B", // 58
			"LD   E,C", // 59
			"LD   E,D", // 5a
			"LD   E,E", // 5b
			"LD   E,H", // 5c
			"LD   E,L", // 5d
			"LD   E,(HL)", // 5e
			"LD   E,A", // 5f
			"LD   H,B", // 60
			"LD   H,C", // 61
			"LD   H,D", // 62
			"LD   H,E", // 63
			"LD   H,H", // 64
			"LD   H,L", // 65
			"LD   H,(HL)", // 66
			"LD   H,A", // 67
			"LD   L,B", // 68
			"LD   L,C", // 69
			"LD   L,D", // 6a
			"LD   L,E", // 6b
			"LD   L,H", // 6c
			"LD   L,L", // 6d
			"LD   L,(HL)", // 6e
			"LD   L,A", // 6f
			"LD   (HL),B", // 70
			"LD   (HL),C", // 71
			"LD   (HL),D", // 72
			"LD   (HL),E", // 73
			"LD   (HL),H", // 74
			"LD   (HL),L", // 75
			"HALT", // 76
			"LD   (HL),A", // 77
			"LD   A,B", // 78
			"LD   A,C", // 79
			"LD   A,D", // 7a
			"LD   A,E", // 7b
			"LD   A,H", // 7c
			"LD   A,L", // 7d
			"LD   A,(HL)", // 7e
			"LD   A,A", // 7f
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
			"ADD  SP,e8", // e8
			"JP   HL", // e9
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
			"LD   HL,SP+e8", // f8
			"LD   SP,HL", // f9
			"LD   A,(a16)", // fa
			"EI   ", // fb
			"???", // fc
			"???", // fd
			"CP   d8", // fe
			"RST  38H", // ff
			"RLC  B", // 00
			"RLC  C", // 01
			"RLC  D", // 02
			"RLC  E", // 03
			"RLC  H", // 04
			"RLC  L", // 05
			"RLC  (HL)", // 06
			"RLC  A", // 07
			"RRC  B", // 08
			"RRC  C", // 09
			"RRC  D", // 0a
			"RRC  E", // 0b
			"RRC  H", // 0c
			"RRC  L", // 0d
			"RRC  (HL)", // 0e
			"RRC  A", // 0f
			"RL   B", // 10
			"RL   C", // 11
			"RL   D", // 12
			"RL   E", // 13
			"RL   H", // 14
			"RL   L", // 15
			"RL   (HL)", // 16
			"RL   A", // 17
			"RR   B", // 18
			"RR   C", // 19
			"RR   D", // 1a
			"RR   E", // 1b
			"RR   H", // 1c
			"RR   L", // 1d
			"RR   (HL)", // 1e
			"RR   A", // 1f
			"SLA  B", // 20
			"SLA  C", // 21
			"SLA  D", // 22
			"SLA  E", // 23
			"SLA  H", // 24
			"SLA  L", // 25
			"SLA  (HL)", // 26
			"SLA  A", // 27
			"SRA  B", // 28
			"SRA  C", // 29
			"SRA  D", // 2a
			"SRA  E", // 2b
			"SRA  H", // 2c
			"SRA  L", // 2d
			"SRA  (HL)", // 2e
			"SRA  A", // 2f
			"SWAP B", // 30
			"SWAP C", // 31
			"SWAP D", // 32
			"SWAP E", // 33
			"SWAP H", // 34
			"SWAP L", // 35
			"SWAP (HL)", // 36
			"SWAP A", // 37
			"SRL  B", // 38
			"SRL  C", // 39
			"SRL  D", // 3a
			"SRL  E", // 3b
			"SRL  H", // 3c
			"SRL  L", // 3d
			"SRL  (HL)", // 3e
			"SRL  A", // 3f
			"BIT  0,B", // 40
			"BIT  0,C", // 41
			"BIT  0,D", // 42
			"BIT  0,E", // 43
			"BIT  0,H", // 44
			"BIT  0,L", // 45
			"BIT  0,(HL)", // 46
			"BIT  0,A", // 47
			"BIT  1,B", // 48
			"BIT  1,C", // 49
			"BIT  1,D", // 4a
			"BIT  1,E", // 4b
			"BIT  1,H", // 4c
			"BIT  1,L", // 4d
			"BIT  1,(HL)", // 4e
			"BIT  1,A", // 4f
			"BIT  2,B", // 50
			"BIT  2,C", // 51
			"BIT  2,D", // 52
			"BIT  2,E", // 53
			"BIT  2,H", // 54
			"BIT  2,L", // 55
			"BIT  2,(HL)", // 56
			"BIT  2,A", // 57
			"BIT  3,B", // 58
			"BIT  3,C", // 59
			"BIT  3,D", // 5a
			"BIT  3,E", // 5b
			"BIT  3,H", // 5c
			"BIT  3,L", // 5d
			"BIT  3,(HL)", // 5e
			"BIT  3,A", // 5f
			"BIT  4,B", // 60
			"BIT  4,C", // 61
			"BIT  4,D", // 62
			"BIT  4,E", // 63
			"BIT  4,H", // 64
			"BIT  4,L", // 65
			"BIT  4,(HL)", // 66
			"BIT  4,A", // 67
			"BIT  5,B", // 68
			"BIT  5,C", // 69
			"BIT  5,D", // 6a
			"BIT  5,E", // 6b
			"BIT  5,H", // 6c
			"BIT  5,L", // 6d
			"BIT  5,(HL)", // 6e
			"BIT  5,A", // 6f
			"BIT  6,B", // 70
			"BIT  6,C", // 71
			"BIT  6,D", // 72
			"BIT  6,E", // 73
			"BIT  6,H", // 74
			"BIT  6,L", // 75
			"BIT  6,(HL)", // 76
			"BIT  6,A", // 77
			"BIT  7,B", // 78
			"BIT  7,C", // 79
			"BIT  7,D", // 7a
			"BIT  7,E", // 7b
			"BIT  7,H", // 7c
			"BIT  7,L", // 7d
			"BIT  7,(HL)", // 7e
			"BIT  7,A", // 7f
			"RES  0,B", // 80
			"RES  0,C", // 81
			"RES  0,D", // 82
			"RES  0,E", // 83
			"RES  0,H", // 84
			"RES  0,L", // 85
			"RES  0,(HL)", // 86
			"RES  0,A", // 87
			"RES  1,B", // 88
			"RES  1,C", // 89
			"RES  1,D", // 8a
			"RES  1,E", // 8b
			"RES  1,H", // 8c
			"RES  1,L", // 8d
			"RES  1,(HL)", // 8e
			"RES  1,A", // 8f
			"RES  2,B", // 90
			"RES  2,C", // 91
			"RES  2,D", // 92
			"RES  2,E", // 93
			"RES  2,H", // 94
			"RES  2,L", // 95
			"RES  2,(HL)", // 96
			"RES  2,A", // 97
			"RES  3,B", // 98
			"RES  3,C", // 99
			"RES  3,D", // 9a
			"RES  3,E", // 9b
			"RES  3,H", // 9c
			"RES  3,L", // 9d
			"RES  3,(HL)", // 9e
			"RES  3,A", // 9f
			"RES  4,B", // a0
			"RES  4,C", // a1
			"RES  4,D", // a2
			"RES  4,E", // a3
			"RES  4,H", // a4
			"RES  4,L", // a5
			"RES  4,(HL)", // a6
			"RES  4,A", // a7
			"RES  5,B", // a8
			"RES  5,C", // a9
			"RES  5,D", // aa
			"RES  5,E", // ab
			"RES  5,H", // ac
			"RES  5,L", // ad
			"RES  5,(HL)", // ae
			"RES  5,A", // af
			"RES  6,B", // b0
			"RES  6,C", // b1
			"RES  6,D", // b2
			"RES  6,E", // b3
			"RES  6,H", // b4
			"RES  6,L", // b5
			"RES  6,(HL)", // b6
			"RES  6,A", // b7
			"RES  7,B", // b8
			"RES  7,C", // b9
			"RES  7,D", // ba
			"RES  7,E", // bb
			"RES  7,H", // bc
			"RES  7,L", // bd
			"RES  7,(HL)", // be
			"RES  7,A", // bf
			"SET  0,B", // c0
			"SET  0,C", // c1
			"SET  0,D", // c2
			"SET  0,E", // c3
			"SET  0,H", // c4
			"SET  0,L", // c5
			"SET  0,(HL)", // c6
			"SET  0,A", // c7
			"SET  1,B", // c8
			"SET  1,C", // c9
			"SET  1,D", // ca
			"SET  1,E", // cb
			"SET  1,H", // cc
			"SET  1,L", // cd
			"SET  1,(HL)", // ce
			"SET  1,A", // cf
			"SET  2,B", // d0
			"SET  2,C", // d1
			"SET  2,D", // d2
			"SET  2,E", // d3
			"SET  2,H", // d4
			"SET  2,L", // d5
			"SET  2,(HL)", // d6
			"SET  2,A", // d7
			"SET  3,B", // d8
			"SET  3,C", // d9
			"SET  3,D", // da
			"SET  3,E", // db
			"SET  3,H", // dc
			"SET  3,L", // dd
			"SET  3,(HL)", // de
			"SET  3,A", // df
			"SET  4,B", // e0
			"SET  4,C", // e1
			"SET  4,D", // e2
			"SET  4,E", // e3
			"SET  4,H", // e4
			"SET  4,L", // e5
			"SET  4,(HL)", // e6
			"SET  4,A", // e7
			"SET  5,B", // e8
			"SET  5,C", // e9
			"SET  5,D", // ea
			"SET  5,E", // eb
			"SET  5,H", // ec
			"SET  5,L", // ed
			"SET  5,(HL)", // ee
			"SET  5,A", // ef
			"SET  6,B", // f0
			"SET  6,C", // f1
			"SET  6,D", // f2
			"SET  6,E", // f3
			"SET  6,H", // f4
			"SET  6,L", // f5
			"SET  6,(HL)", // f6
			"SET  6,A", // f7
			"SET  7,B", // f8
			"SET  7,C", // f9
			"SET  7,D", // fa
			"SET  7,E", // fb
			"SET  7,H", // fc
			"SET  7,L", // fd
			"SET  7,(HL)", // fe
			"SET  7,A", // ff
		};

		private static readonly string[] rgbds_table =
		{
			"nop", // 00
			"ld bc, d16", // 01
			"ld [bc], a", // 02
			"inc bc", // 03
			"inc b", // 04
			"dec b", // 05
			"ld b, d8", // 06
			"rlca", // 07
			"ld [a16], sp", // 08
			"add hl, bc", // 09
			"ld a, [bc]", // 0a
			"dec bc", // 0b
			"inc c", // 0c
			"dec c", // 0d
			"ld c, d8", // 0e
			"rrca", // 0f
			"stop d8", // 10
			"ld de, d16", // 11
			"ld [de], a", // 12
			"inc de", // 13
			"inc d", // 14
			"dec d", // 15
			"ld d, d8", // 16
			"rla", // 17
			"jr r8", // 18
			"add hl, de", // 19
			"ld a, [de]", // 1a
			"dec de", // 1b
			"inc e", // 1c
			"dec e", // 1d
			"ld e, d8", // 1e
			"rra", // 1f
			"jr nz, r8", // 20
			"ld hl, d16", // 21
			"ld [hl+], a", // 22
			"inc hl", // 23
			"inc h", // 24
			"dec h", // 25
			"ld h, d8", // 26
			"daa", // 27
			"jr z, r8", // 28
			"add hl, hl", // 29
			"ld a, [hl+]", // 2a
			"dec hl", // 2b
			"inc l", // 2c
			"dec l", // 2d
			"ld l, d8", // 2e
			"cpl", // 2f
			"jr nc, r8", // 30
			"ld sp, d16", // 31
			"ld [hl-], a", // 32
			"inc sp", // 33
			"inc [hl]", // 34
			"dec [hl]", // 35
			"ld [hl], d8", // 36
			"scf", // 37
			"jr c, r8", // 38
			"add hl, sp", // 39
			"ld a, [hl-]", // 3a
			"dec sp", // 3b
			"inc a", // 3c
			"dec a", // 3d
			"ld a, d8", // 3e
			"ccf", // 3f
			"ld b, b", // 40
			"ld b, c", // 41
			"ld b, d", // 42
			"ld b, e", // 43
			"ld b, h", // 44
			"ld b, l", // 45
			"ld b, [hl]", // 46
			"ld b, a", // 47
			"ld c, b", // 48
			"ld c, c", // 49
			"ld c, d", // 4a
			"ld c, e", // 4b
			"ld c, h", // 4c
			"ld c, l", // 4d
			"ld c, [hl]", // 4e
			"ld c, a", // 4f
			"ld d, b", // 50
			"ld d, c", // 51
			"ld d, d", // 52
			"ld d, e", // 53
			"ld d, h", // 54
			"ld d, l", // 55
			"ld d, [hl]", // 56
			"ld d, a", // 57
			"ld e, b", // 58
			"ld e, c", // 59
			"ld e, d", // 5a
			"ld e, e", // 5b
			"ld e, h", // 5c
			"ld e, l", // 5d
			"ld e, [hl]", // 5e
			"ld e, a", // 5f
			"ld h, b", // 60
			"ld h, c", // 61
			"ld h, d", // 62
			"ld h, e", // 63
			"ld h, h", // 64
			"ld h, l", // 65
			"ld h, [hl]", // 66
			"ld h, a", // 67
			"ld l, b", // 68
			"ld l, c", // 69
			"ld l, d", // 6a
			"ld l, e", // 6b
			"ld l, h", // 6c
			"ld l, l", // 6d
			"ld l, [hl]", // 6e
			"ld l, a", // 6f
			"ld [hl], b", // 70
			"ld [hl], c", // 71
			"ld [hl], d", // 72
			"ld [hl], e", // 73
			"ld [hl], h", // 74
			"ld [hl], l", // 75
			"halt", // 76
			"ld [hl], a", // 77
			"ld a, b", // 78
			"ld a, c", // 79
			"ld a, d", // 7a
			"ld a, e", // 7b
			"ld a, h", // 7c
			"ld a, l", // 7d
			"ld a, [hl]", // 7e
			"ld a, a", // 7f
			"add a, b", // 80
			"add a, c", // 81
			"add a, d", // 82
			"add a, e", // 83
			"add a, h", // 84
			"add a, l", // 85
			"add a, [hl]", // 86
			"add a, a", // 87
			"adc a, b", // 88
			"adc a, c", // 89
			"adc a, d", // 8a
			"adc a, e", // 8b
			"adc a, h", // 8c
			"adc a, l", // 8d
			"adc a, [hl]", // 8e
			"adc a, a", // 8f
			"sub a, b", // 90
			"sub a, c", // 91
			"sub a, d", // 92
			"sub a, e", // 93
			"sub a, h", // 94
			"sub a, l", // 95
			"sub a, [hl]", // 96
			"sub a, a", // 97
			"sbc a, b", // 98
			"sbc a, c", // 99
			"sbc a, d", // 9a
			"sbc a, e", // 9b
			"sbc a, h", // 9c
			"sbc a, l", // 9d
			"sbc a, [hl]", // 9e
			"sbc a, a", // 9f
			"and a, b", // a0
			"and a, c", // a1
			"and a, d", // a2
			"and a, e", // a3
			"and a, h", // a4
			"and a, l", // a5
			"and a, [hl]", // a6
			"and a, a", // a7
			"xor a, b", // a8
			"xor a, c", // a9
			"xor a, d", // aa
			"xor a, e", // ab
			"xor a, h", // ac
			"xor a, l", // ad
			"xor a, [hl]", // ae
			"xor a, a", // af
			"or a, b", // b0
			"or a, c", // b1
			"or a, d", // b2
			"or a, e", // b3
			"or a, h", // b4
			"or a, l", // b5
			"or a, [hl]", // b6
			"or a, a", // b7
			"cp a, b", // b8
			"cp a, c", // b9
			"cp a, d", // ba
			"cp a, e", // bb
			"cp a, h", // bc
			"cp a, l", // bd
			"cp a, [hl]", // be
			"cp a, a", // bf
			"ret nz", // c0
			"pop bc", // c1
			"jp nz, a16", // c2
			"jp a16", // c3
			"call nz, a16", // c4
			"push bc", // c5
			"add a, d8", // c6
			"rst $00", // c7
			"ret z", // c8
			"ret", // c9
			"jp z, a16", // ca
			"prefix", // cb
			"call z, a16", // cc
			"call a16", // cd
			"adc a, d8", // ce
			"rst $08", // cf
			"ret nc", // d0
			"pop de", // d1
			"jp nc, a16", // d2
			"invalid opcode $D3", // d3
			"call nc, a16", // d4
			"push de", // d5
			"sub a, d8", // d6
			"rst $10", // d7
			"ret c", // d8
			"reti", // d9
			"jp c, a16", // da
			"invalid opcode $DB", // db
			"call c, a16", // dc
			"invalid opcode $DD", // dd
			"sbc a, d8", // de
			"rst $18", // df
			"ldh [a8], a", // e0
			"pop hl", // e1
			"ldh [c], a", // e2
			"invalid opcode $E3", // e3
			"invalid opcode $E4", // e4
			"push hl", // e5
			"and a, d8", // e6
			"rst $20", // e7
			"add sp, e8", // e8
			"jp hl", // e9
			"ld [a16], a", // ea
			"invalid opcode $EB", // eb
			"invalid opcode $EC", // ec
			"invalid opcode $ED", // ed
			"xor a, d8", // ee
			"rst $28", // ef
			"ldh a, [a8]", // f0
			"pop af", // f1
			"ldh a, [c]", // f2
			"di", // f3
			"invalid opcode $DB", // f4
			"push af", // f5
			"or a, d8", // f6
			"rst $30", // f7
			"ld hl, sp + e8", // f8
			"ld sp, hl", // f9
			"ld a, [a16]", // fa
			"ei", // fb
			"invalid opcode $FC", // fc
			"invalid opcode $FD", // fd
			"cp a, d8", // fe
			"rst $38", // ff
			"rlc b", // 00
			"rlc c", // 01
			"rlc d", // 02
			"rlc e", // 03
			"rlc h", // 04
			"rlc l", // 05
			"rlc [hl]", // 06
			"rlc a", // 07
			"rrc b", // 08
			"rrc c", // 09
			"rrc d", // 0a
			"rrc e", // 0b
			"rrc h", // 0c
			"rrc l", // 0d
			"rrc [hl]", // 0e
			"rrc a", // 0f
			"rl b", // 10
			"rl c", // 11
			"rl d", // 12
			"rl e", // 13
			"rl h", // 14
			"rl l", // 15
			"rl [hl]", // 16
			"rl a", // 17
			"rr b", // 18
			"rr c", // 19
			"rr d", // 1a
			"rr e", // 1b
			"rr h", // 1c
			"rr l", // 1d
			"rr [hl]", // 1e
			"rr a", // 1f
			"sla b", // 20
			"sla c", // 21
			"sla d", // 22
			"sla e", // 23
			"sla h", // 24
			"sla l", // 25
			"sla [hl]", // 26
			"sla a", // 27
			"sra b", // 28
			"sra c", // 29
			"sra d", // 2a
			"sra e", // 2b
			"sra h", // 2c
			"sra l", // 2d
			"sra [hl]", // 2e
			"sra a", // 2f
			"swap b", // 30
			"swap c", // 31
			"swap d", // 32
			"swap e", // 33
			"swap h", // 34
			"swap l", // 35
			"swap [hl]", // 36
			"swap a", // 37
			"srl b", // 38
			"srl c", // 39
			"srl d", // 3a
			"srl e", // 3b
			"srl h", // 3c
			"srl l", // 3d
			"srl [hl]", // 3e
			"srl a", // 3f
			"bit 0, b", // 40
			"bit 0, c", // 41
			"bit 0, d", // 42
			"bit 0, e", // 43
			"bit 0, h", // 44
			"bit 0, l", // 45
			"bit 0, [hl]", // 46
			"bit 0, a", // 47
			"bit 1, b", // 48
			"bit 1, c", // 49
			"bit 1, d", // 4a
			"bit 1, e", // 4b
			"bit 1, h", // 4c
			"bit 1, l", // 4d
			"bit 1, [hl]", // 4e
			"bit 1, a", // 4f
			"bit 2, b", // 50
			"bit 2, c", // 51
			"bit 2, d", // 52
			"bit 2, e", // 53
			"bit 2, h", // 54
			"bit 2, l", // 55
			"bit 2, [hl]", // 56
			"bit 2, a", // 57
			"bit 3, b", // 58
			"bit 3, c", // 59
			"bit 3, d", // 5a
			"bit 3, e", // 5b
			"bit 3, h", // 5c
			"bit 3, l", // 5d
			"bit 3, [hl]", // 5e
			"bit 3, a", // 5f
			"bit 4, b", // 60
			"bit 4, c", // 61
			"bit 4, d", // 62
			"bit 4, e", // 63
			"bit 4, h", // 64
			"bit 4, l", // 65
			"bit 4, [hl]", // 66
			"bit 4, a", // 67
			"bit 5, b", // 68
			"bit 5, c", // 69
			"bit 5, d", // 6a
			"bit 5, e", // 6b
			"bit 5, h", // 6c
			"bit 5, l", // 6d
			"bit 5, [hl]", // 6e
			"bit 5, a", // 6f
			"bit 6, b", // 70
			"bit 6, c", // 71
			"bit 6, d", // 72
			"bit 6, e", // 73
			"bit 6, h", // 74
			"bit 6, l", // 75
			"bit 6, [hl]", // 76
			"bit 6, a", // 77
			"bit 7, b", // 78
			"bit 7, c", // 79
			"bit 7, d", // 7a
			"bit 7, e", // 7b
			"bit 7, h", // 7c
			"bit 7, l", // 7d
			"bit 7, [hl]", // 7e
			"bit 7, a", // 7f
			"res 0, b", // 80
			"res 0, c", // 81
			"res 0, d", // 82
			"res 0, e", // 83
			"res 0, h", // 84
			"res 0, l", // 85
			"res 0, [hl]", // 86
			"res 0, a", // 87
			"res 1, b", // 88
			"res 1, c", // 89
			"res 1, d", // 8a
			"res 1, e", // 8b
			"res 1, h", // 8c
			"res 1, l", // 8d
			"res 1, [hl]", // 8e
			"res 1, a", // 8f
			"res 2, b", // 90
			"res 2, c", // 91
			"res 2, d", // 92
			"res 2, e", // 93
			"res 2, h", // 94
			"res 2, l", // 95
			"res 2, [hl]", // 96
			"res 2, a", // 97
			"res 3, b", // 98
			"res 3, c", // 99
			"res 3, d", // 9a
			"res 3, e", // 9b
			"res 3, h", // 9c
			"res 3, l", // 9d
			"res 3, [hl]", // 9e
			"res 3, a", // 9f
			"res 4, b", // a0
			"res 4, c", // a1
			"res 4, d", // a2
			"res 4, e", // a3
			"res 4, h", // a4
			"res 4, l", // a5
			"res 4, [hl]", // a6
			"res 4, a", // a7
			"res 5, b", // a8
			"res 5, c", // a9
			"res 5, d", // aa
			"res 5, e", // ab
			"res 5, h", // ac
			"res 5, l", // ad
			"res 5, [hl]", // ae
			"res 5, a", // af
			"res 6, b", // b0
			"res 6, c", // b1
			"res 6, d", // b2
			"res 6, e", // b3
			"res 6, h", // b4
			"res 6, l", // b5
			"res 6, [hl]", // b6
			"res 6, a", // b7
			"res 7, b", // b8
			"res 7, c", // b9
			"res 7, d", // ba
			"res 7, e", // bb
			"res 7, h", // bc
			"res 7, l", // bd
			"res 7, [hl]", // be
			"res 7, a", // bf
			"set 0, b", // c0
			"set 0, c", // c1
			"set 0, d", // c2
			"set 0, e", // c3
			"set 0, h", // c4
			"set 0, l", // c5
			"set 0, [hl]", // c6
			"set 0, a", // c7
			"set 1, b", // c8
			"set 1, c", // c9
			"set 1, d", // ca
			"set 1, e", // cb
			"set 1, h", // cc
			"set 1, l", // cd
			"set 1, [hl]", // ce
			"set 1, a", // cf
			"set 2, b", // d0
			"set 2, c", // d1
			"set 2, d", // d2
			"set 2, e", // d3
			"set 2, h", // d4
			"set 2, l", // d5
			"set 2, [hl]", // d6
			"set 2, a", // d7
			"set 3, b", // d8
			"set 3, c", // d9
			"set 3, d", // da
			"set 3, e", // db
			"set 3, h", // dc
			"set 3, l", // dd
			"set 3, [hl]", // de
			"set 3, a", // df
			"set 4, b", // e0
			"set 4, c", // e1
			"set 4, d", // e2
			"set 4, e", // e3
			"set 4, h", // e4
			"set 4, l", // e5
			"set 4, [hl]", // e6
			"set 4, a", // e7
			"set 5, b", // e8
			"set 5, c", // e9
			"set 5, d", // ea
			"set 5, e", // eb
			"set 5, h", // ec
			"set 5, l", // ed
			"set 5, [hl]", // ee
			"set 5, a", // ef
			"set 6, b", // f0
			"set 6, c", // f1
			"set 6, d", // f2
			"set 6, e", // f3
			"set 6, h", // f4
			"set 6, l", // f5
			"set 6, [hl]", // f6
			"set 6, a", // f7
			"set 7, b", // f8
			"set 7, c", // f9
			"set 7, d", // fa
			"set 7, e", // fb
			"set 7, h", // fc
			"set 7, l", // fd
			"set 7, [hl]", // fe
			"set 7, a", // ff
		};

		public static string Disassemble(ushort addr, Func<ushort, byte> reader, bool rgbds, out ushort size)
		{
			ushort origaddr = addr;
			var bytes = new List<byte>
			{
				reader(addr++)
			};

			string result = (rgbds ? rgbds_table : table)[bytes[0]];
			if (bytes[0] == 0xcb)
			{
				bytes.Add(reader(addr++));
				result = (rgbds ? rgbds_table : table)[bytes[1] + 256];
			}

			if (result.Contains("d8"))
			{
				byte d = reader(addr++);
				bytes.Add(d);
				result = result.Replace("d8", rgbds ? $"${d:X2}" : $"#{d:X2}h");
			}
			else if (result.Contains("d16"))
			{
				byte dlo = reader(addr++);
				byte dhi = reader(addr++);
				bytes.Add(dlo);
				bytes.Add(dhi);
				result = result.Replace("d16", rgbds ? $"${dhi:X2}{dlo:X2}" : $"#{dhi:X2}{dlo:X2}h");
			}
			else if (result.Contains("a16"))
			{
				byte dlo = reader(addr++);
				byte dhi = reader(addr++);
				bytes.Add(dlo);
				bytes.Add(dhi);
				result = result.Replace("a16", rgbds ? $"${dhi:X2}{dlo:X2}" : $"#{dhi:X2}{dlo:X2}h");
			}
			else if (result.Contains("a8"))
			{
				byte d = reader(addr++);
				bytes.Add(d);
				result = result.Replace("a8", rgbds ? $"$FF{d:X2}" : $"#FF{d:X2}h");
			}
			else if (result.Contains("r8"))
			{
				byte d = reader(addr++);
				bytes.Add(d);
				int offs = d;
				if (offs >= 128)
					offs -= 256;
				var u = (ushort) (addr + offs);
				result = result.Replace("r8", rgbds ? $"${u:X4}" : $"{u:X4}h");
			}
			else if (result.Contains("e8"))
			{
				byte d = reader(addr++);
				bytes.Add(d);
				int offs = (d >= 128) ? (256 - d) : d;
				string sign = (d >= 128) ? "-" : "";
				result = result.Replace("e8", rgbds ? sign + $"${offs:X2}" : sign + $"{offs:X2}h");
			}
			var ret = new StringBuilder();
			ret.Append($"{origaddr:X4}:  ");
			foreach (var b in bytes)
				ret.Append($"{b:X2} ");
			while (ret.Length < 17)
				ret.Append(' ');
			ret.Append(result);
			size = (ushort)(addr - origaddr);
			return ret.ToString();
		}
	}
}
