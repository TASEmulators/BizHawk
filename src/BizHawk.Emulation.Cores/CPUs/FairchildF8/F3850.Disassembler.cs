using System.Collections.Generic;

using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Components.FairchildF8
{
	/// <summary>
	/// Disassembler
	/// </summary>
	public sealed partial class F3850<TLink> : IDisassemblable
	{
		private static string Result(string format, Func<ushort, byte> read, ref ushort addr)
		{
			//d immediately succeeds the opcode
			//n immediate succeeds the opcode and the displacement (if present)
			//nn immediately succeeds the opcode and the displacement (if present)

			if (format.ContainsOrdinal("nn"))
			{
				format = format.Replace("nn", read(addr++)
					.ToString("X2") + read(addr++)
					.ToString("X2") + "h"); // MSB is read first
			}
			if (format.ContainsOrdinal('n')) format = format.Replace("n", $"{read(addr++):X2}h");

			format = format.Replace("+d", "d");
			if (format.ContainsOrdinal('d'))
			{
				var b = unchecked((sbyte)read(addr++));
				format = format.Replace("d", $"{(b < 0 ? '-' : '+')}{Math.Abs((short) b):X2}h");
			}

			return format;
		}


		private static readonly string[] mnemonics =
		{
			"LR A, KU",			// 0x00
			"LR A, KL",			// 0x01
			"LR A, QU",			// 0x02
			"LR A, QL",			// 0x03
			"LR KU, A",			// 0x04
			"LR KL, A",			// 0x05
			"LR QU, A",			// 0x06
			"LR QL, A",			// 0x07
			"LR K, P",			// 0x08
			"LR P, K",			// 0x09
			"LR A, IS",			// 0x0A
			"LR IS, A",			// 0x0B
			"PK",				// 0x0C
			"LR P0, Q",			// 0x0D
			"LR Q, DC",			// 0x0E
			"LR DC, Q",			// 0x0F
			"LR DC, H",			// 0x10
			"LR H, DC",			// 0x11
			"SR 1",				// 0x12
			"SL 1",				// 0x13
			"SR 4",				// 0x14
			"SL 4",				// 0x15
			"LM",				// 0x16
			"ST",				// 0x17
			"COM",				// 0x18
			"LNK",				// 0x19
			"DI",				// 0x1A
			"EI",				// 0x1B
			"POP",				// 0x1C
			"LR W, J",			// 0x1D
			"LR J, W",			// 0x1E
			"INC",				// 0x1F
			"LI n",				// 0x20
			"NI n",				// 0x21
			"OI n",				// 0x22
			"XI n",				// 0x23
			"AI n",				// 0x24
			"CI n",				// 0x25
			"IN n",				// 0x26
			"OUT n",			// 0x27
			"PI nn",			// 0x28
			"JMP nn",			// 0x29
			"DCI nn",			// 0x2A
			"NOP",				// 0x2B
			"XDC",				// 0x2C
			"ILLEGAL",			// 0x2D
			"ILLEGAL",			// 0x2E
			"ILLEGAL",			// 0x2F
			"DS r00",			// 0x30
			"DS r01",			// 0x31
			"DS r02",			// 0x32
			"DS r03",			// 0x33
			"DS r04",			// 0x34
			"DS r05",			// 0x35
			"DS r06",			// 0x36
			"DS r07",			// 0x37
			"DS r08",			// 0x38
			"DS r09",			// 0x39
			"DS r10",			// 0x3A
			"DS r11",			// 0x3B
			"DS ISAR",			// 0x3C
			"DS ISAR INC",		// 0x3D
			"DS ISAR DEC",		// 0x3E
			"ILLEGAL",			// 0x3F
			"LR A, r00",		// 0x40
			"LR A, r01",		// 0x41
			"LR A, r02",		// 0x42
			"LR A, r03",		// 0x43
			"LR A, r04",		// 0x44
			"LR A, r05",		// 0x45
			"LR A, r06",		// 0x46
			"LR A, r07",		// 0x47
			"LR A, r08",		// 0x48
			"LR A, r09",		// 0x49
			"LR A, r10",		// 0x4A
			"LR A, r11",		// 0x4B
			"LR A, (ISAR)",		// 0x4C
			"LR A, (ISAR) INC",	// 0x4D
			"LR A, (ISAR) DEC",	// 0x4E
			"ILLEGAL",			// 0x4F
			"LR r00, A",		// 0x50
			"LR r01, A",		// 0x51
			"LR r02, A",		// 0x52
			"LR r03, A",		// 0x53
			"LR r04, A",		// 0x54
			"LR r05, A",		// 0x55
			"LR r06, A",		// 0x56
			"LR r07, A",		// 0x57
			"LR r08, A",		// 0x58
			"LR r09, A",		// 0x59
			"LR r10, A",		// 0x5A
			"LR r11, A",		// 0x5B
			"LR ((ISAR)), A",	// 0x5C
			"LR (ISAR), A INC",	// 0x5D
			"LR (ISAR), A DEC",	// 0x5E
			"ILLEGAL",			// 0x5F
			"LISU 0",			// 0x60
			"LISU 1",			// 0x61
			"LISU 2",			// 0x62
			"LISU 3",			// 0x63
			"LISU 4",			// 0x64
			"LISU 5",			// 0x65
			"LISU 6",			// 0x66
			"LISU 7",			// 0x67
			"LISL 0",			// 0x68
			"LISL 1",			// 0x69
			"LISL 2",			// 0x6A
			"LISL 3",			// 0x6B
			"LISL 4",			// 0x6C
			"LISL 5",			// 0x6D
			"LISL 6",			// 0x6E
			"LISL 7",			// 0x6F
			"LIS 0",			// 0x70
			"LIS 1",			// 0x71
			"LIS 2",			// 0x72
			"LIS 3",			// 0x73
			"LIS 4",			// 0x74
			"LIS 5",			// 0x75
			"LIS 6",			// 0x76
			"LIS 7",			// 0x77
			"LIS 8",			// 0x78
			"LIS 9",			// 0x79
			"LIS A",			// 0x7A
			"LIS B",			// 0x7B
			"LIS C",			// 0x7C
			"LIS D",			// 0x7D
			"LIS E",			// 0x7E
			"LIS F",			// 0x7F
			"BT NOBRANCH",		// 0x80
			"BP d",				// 0x81
			"BC d",				// 0x82
			"BP or C d",		// 0x83
			"BZ d",				// 0x84
			"BP d",				// 0x85
			"BZ or C d",		// 0x86
			"BP or C d",		// 0x87
			"AM",				// 0x88
			"AMD",				// 0x89
			"NM",				// 0x8A
			"OM",				// 0x8B
			"XM",				// 0x8C
			"CM",				// 0x8D
			"ADC",				// 0x8E
			"BR7 n",			// 0x8F
			"BF UNCON d",		// 0x90
			"BN d",				// 0x91
			"BNC d",			// 0x92
			"BNC & deg d",		// 0x93
			"BNZ d",			// 0x94
			"BN d",				// 0x95
			"BNC & dZ d",		// 0x96
			"BNC & deg d",		// 0x97
			"BNO d",			// 0x98
			"BN & dO d",		// 0x99
			"BNO & dC d",		// 0x9A
			"BNO & dC & deg d",	// 0x9B
			"BNO & dZ d",		// 0x9C
			"BN & dO d",		// 0x9D
			"BNO & dC & dZ d",	// 0x9E
			"BNO & dC & deg d",	// 0x9F
			"INS 0",			// 0xA0
			"INS 1",			// 0xA1
			"ILLEGAL",			// 0xA2
			"ILLEGAL",			// 0xA3
			"INS 4",			// 0xA4
			"INS 5",			// 0xA5
			"INS 6",			// 0xA6
			"INS 7",			// 0xA7
			"INS 8",			// 0xA8
			"INS 9",			// 0xA9
			"INS 10",			// 0xAA
			"INS 11",			// 0xAB
			"INS 12",			// 0xAC
			"INS 13",			// 0xAD
			"INS 14",			// 0xAE
			"INS 16",			// 0xAF
			"OUTS 0",			// 0xB0
			"OUTS 1",			// 0xB1
			"ILLEGAL",			// 0xB2
			"ILLEGAL",			// 0xB3
			"OUTS 4",			// 0xB4
			"OUTS 5",			// 0xB5
			"OUTS 6",			// 0xB6
			"OUTS 7",			// 0xB7
			"OUTS 8",			// 0xB8
			"OUTS 9",			// 0xB9
			"OUTS 10",			// 0xBA
			"OUTS 11",			// 0xBB
			"OUTS 12",			// 0xBC
			"OUTS 13",			// 0xBD
			"OUTS 14",			// 0xBE
			"OUTS 15",			// 0xBF
			"AS r00",			// 0xC0
			"AS r01",			// 0xC1
			"AS r02",			// 0xC2
			"AS r03",			// 0xC3
			"AS r04",			// 0xC4
			"AS r05",			// 0xC5
			"AS r06",			// 0xC6
			"AS r07",			// 0xC7
			"AS r08",			// 0xC8
			"AS r09",			// 0xC9
			"AS r10",			// 0xCA
			"AS r11",			// 0xCB
			"AS ISAR",			// 0xCC
			"AS ISAR INC",		// 0xCD
			"AS ISAR DEC",		// 0xCE
			"ILLEGAL",			// 0xCF
			"ASD r00",			// 0xD0
			"ASD r01",			// 0xD1
			"ASD r02",			// 0xD2
			"ASD r03",			// 0xD3
			"ASD r04",			// 0xD4
			"ASD r05",			// 0xD5
			"ASD r06",			// 0xD6
			"ASD r07",			// 0xD7
			"ASD r08",			// 0xD8
			"ASD r09",			// 0xD9
			"ASD r10",			// 0xDA
			"ASD r11",			// 0xDB
			"ASD ISAR",			// 0xDC
			"ASD ISAR INC",		// 0xDD
			"ASD ISAR DEC",		// 0xDE
			"ILLEGAL",			// 0xDF
			"XS r00",			// 0xE0
			"XS r01",			// 0xE1
			"XS r02",			// 0xE2
			"XS r03",			// 0xE3
			"XS r04",			// 0xE4
			"XS r05",			// 0xE5
			"XS r06",			// 0xE6
			"XS r07",			// 0xE7
			"XS r08",			// 0xE8
			"XS r09",			// 0xE9
			"XS r10",			// 0xEA
			"XS r11",			// 0xEB
			"XS ISAR",			// 0xEC
			"XS ISAR INC",		// 0xED
			"XS ISAR DEC",		// 0xEE
			"ILLEGAL",			// 0xEF
			"NS r00",			// 0xF0
			"NS r01",			// 0xF1
			"NS r02",			// 0xF2
			"NS r03",			// 0xF3
			"NS r04",			// 0xF4
			"NS r05",			// 0xF5
			"NS r06",			// 0xF6
			"NS r07",			// 0xF7
			"NS r08",			// 0xF8
			"NS r09",			// 0xF9
			"NS r10",			// 0xFA
			"NS r11",			// 0xFB
			"NS ISAR",			// 0xFC
			"NS ISAR INC",		// 0xFD
			"NS ISAR DEC",		// 0xFE
			"ILLEGAL",			// 0xFF
		};

		public string Disassemble(ushort addr, Func<ushort, byte> read, out int size)
		{
			ushort start_addr = addr;
//			ushort extra_inc = 0;
			byte A = read(addr++);
			string format;
			format = mnemonics[A];

			string temp = Result(format, read, ref addr);
			size = addr - start_addr;

			if (addr < start_addr)
			{
				size = (0x10000 + addr) - start_addr;
			}

			return temp;
		}

		public string Cpu
		{
			get => "F3850";
			set { }
		}

		public string PCRegisterName => "PC0";

		public IEnumerable<string> AvailableCpus { get; } = [ "F3850" ];

		public string Disassemble(MemoryDomain m, uint addr, out int length)
		{
			string ret = Disassemble((ushort)addr, a => m.PeekByte(a), out length);
			return ret;
		}
	}
}
