using System;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using BizHawk.Emulation.CPUs.M6502;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	partial class NES
	{
		/// <summary>
		/// attempts to classify a rom based on iNES header information
		/// </summary>
		static class iNESBoardDetector
		{
			public static string Detect(CartInfo cartInfo)
			{
				string key = string.Format("{0}	{1}	{2}	{3}	{4}", cartInfo.mapper, cartInfo.prg_size, cartInfo.chr_size, cartInfo.wram_size, cartInfo.vram_size);
				string board;
				Table.TryGetValue(key, out board);
				if (board == null)
				{
					//if it didnt work, try again with a different wram size. because iNES is weird that way
					key = string.Format("{0}	{1}	{2}	{3}	{4}", cartInfo.mapper, cartInfo.prg_size, cartInfo.chr_size, 8, cartInfo.vram_size);
					if (!Table.TryGetValue(key, out board))
					{
						//if it still didnt work, look for one with empty keys, to detect purely based on mapper
						key = string.Format("{0}	{1}	{2}	{3}	{4}", cartInfo.mapper, -1, -1, -1, -1);
						Table.TryGetValue(key, out board);
					}
				}
				return board;
			}

			public static Dictionary<string, string> Table = new Dictionary<string, string>();
			static iNESBoardDetector()
			{
				var sr = new StringReader(ClassifyTable);
				string line;
				while ((line = sr.ReadLine()) != null)
				{
					var parts = line.Split('\t');
					if (parts.Length < 6) continue;
					string key = parts[0] + "\t" + parts[1] + "\t" + parts[2] + "\t" + parts[3] + "\t" + parts[4];
					string board = line.Replace(key, "");
					board = board.TrimStart('\t');
					if (board.IndexOf(';') != -1)
						board = board.Substring(0, board.IndexOf(';'));
					Table[key] = board;
				}
			}

//what to do about 034?

//MAP PRG CHR WRAM VRAM BOARD
static string ClassifyTable = @"
0	-1	-1	-1	-1	MAPPER000
1	-1	-1	-1	-1	MAPPER001
2	-1	-1	-1	-1	MAPPER002
3	-1	-1	-1	-1	MAPPER003
4	-1	-1	-1	-1	MAPPER004
5	-1	-1	-1	-1	MAPPER005
7	-1	-1	-1	-1	MAPPER007
9	-1	-1	-1	-1	MAPPER009
10	-1	-1	-1	-1	MAPPER010
11	-1	-1	-1	-1	MAPPER011
13	-1	-1	-1	-1	MAPPER013
19	-1	-1	-1	-1	MAPPER019
21	-1	-1	-1	-1	MAPPER021
22	-1	-1	-1	-1	MAPPER022
23	-1	-1	-1	-1	MAPPER023
23	-1	-1	-1	-1	MAPPER023
25	-1	-1	-1	-1	MAPPER025
26	-1	-1	-1	-1	MAPPER026
32	-1	-1	-1	-1	MAPPER032
33	-1	-1	-1	-1	MAPPER033
44	-1	-1	-1	-1	MAPPER044
46	-1	-1	-1	-1	MAPPER046
49	-1	-1	-1	-1	MAPPER049
64	-1	-1	-1	-1	MAPPER064
65	-1	-1	-1	-1	MAPPER065
66	-1	-1	-1	-1	MAPPER066
68	-1	-1	-1	-1	MAPPER068
69	-1	-1	-1	-1	MAPPER069
70	-1	-1	-1	-1	MAPPER070
71	-1	-1	-1	-1	MAPPER071
72	-1	-1	-1	-1	MAPPER072
73	-1	-1	-1	-1	MAPPER073
75	-1	-1	-1	-1	MAPPER075
77	-1	-1	-1	-1	MAPPER077
78	-1	-1	-1	-1	MAPPER078
79	-1	-1	-1	-1	MAPPER079
80	-1	-1	-1	-1	MAPPER080
82	-1	-1	-1	-1	MAPPER082
85	-1	-1	-1	-1	MAPPER085
86	-1	-1	-1	-1	MAPPER086
87	-1	-1	-1	-1	MAPPER087
89	-1	-1	-1	-1	MAPPER089
93	-1	-1	-1	-1	MAPPER093
97	-1	-1	-1	-1	MAPPER097
105	-1	-1	-1	-1	MAPPER105
107	-1	-1	-1	-1	MAPPER107
113	-1	-1	-1	-1	MAPPER113
115	-1	-1	-1	-1	MAPPER115
140	-1	-1	-1	-1	MAPPER140
152	-1	-1	-1	-1	MAPPER152
164	-1	-1	-1	-1	MAPPER164
180	-1	-1	-1	-1	MAPPER180
182	-1	-1	-1	-1	MAPPER182
184	-1	-1	-1	-1	MAPPER184
189	-1	-1	-1	-1	MAPPER189
191	-1	-1	-1	-1	MAPPER191
193	-1	-1	-1	-1	MAPPER193
210	-1	-1	-1	-1	MAPPER210
227	-1	-1	-1	-1	MAPPER227
232	-1	-1	-1	-1	MAPPER232
240	-1	-1	-1	-1	MAPPER240
242	-1	-1	-1	-1	MAPPER242
248	-1	-1	-1	-1	MAPPER248
";
}

		unsafe struct iNES_HEADER
		{
			public fixed byte ID[4]; /*NES^Z*/
			public byte ROM_size;
			public byte VROM_size;
			public byte ROM_type;
			public byte ROM_type2;
			public byte wram_size;
			public byte flags9, flags10;
			public byte zero11, zero12, zero13, zero14, zero15;


			public bool CheckID()
			{
				fixed (iNES_HEADER* self = &this)
					return 0 == Util.memcmp(self, "NES\x1A", 4);
			}

			//some cleanup code recommended by fceux
			public void Cleanup()
			{
				fixed (iNES_HEADER* self = &this)
				{
					if (0 == Util.memcmp((byte*)(self) + 0x7, "DiskDude", 8))
					{
						Util.memset((byte*)(self) + 0x7, 0, 0x9);
					}

					if (0 == Util.memcmp((byte*)(self) + 0x7, "demiforce", 9))
					{
						Util.memset((byte*)(self) + 0x7, 0, 0x9);
					}

					if (0 == Util.memcmp((byte*)(self) + 0x8, "blargg", 6)) //found a test rom with this in there, mucking up the wram size
					{
						Util.memset((byte*)(self) + 0x8, 0, 6);
					}

					if (0 == Util.memcmp((byte*)(self) + 0xA, "Ni03", 4))
					{
						if (0 == Util.memcmp((byte*)(self) + 0x7, "Dis", 3))
							Util.memset((byte*)(self) + 0x7, 0, 0x9);
						else
							Util.memset((byte*)(self) + 0xA, 0, 0x6);
					}
				}
			}

			public CartInfo Analyze(TextWriter report)
			{
				var ret = new CartInfo();
				ret.game = new NESGameInfo();
				int mapper = (ROM_type >> 4);
				mapper |= (ROM_type2 & 0xF0);
				ret.mapper = (byte)mapper;
				int mirroring = (ROM_type & 1);
				if ((ROM_type & 8) != 0) mirroring = 2;
				if (mirroring == 0) ret.pad_v = 1;
				else if (mirroring == 1) ret.pad_h = 1;
				ret.prg_size = (short)(ROM_size * 16);
				if (ret.prg_size == 0)
					ret.prg_size = 256 * 16;
				ret.chr_size = (short)(VROM_size * 8);
				ret.wram_battery = (ROM_type & 2) != 0;

				if(wram_size != 0 || flags9 != 0 || flags10 != 0 || zero11 != 0 || zero12 != 0 || zero13 != 0 || zero14 != 0 || zero15 != 0)
				{
					report.WriteLine("Looks like you have an iNES 2.0 header, or some other kind of weird garbage.");
					report.WriteLine("We haven't bothered to support iNES 2.0.");
					report.WriteLine("We might, if we can find anyone who uses it. Let us know.");
				}

				ret.wram_size = (short)(wram_size * 8);
				//0 is supposed to mean 8KB (for compatibility, as this is an extension to original iNES format)
				if (ret.wram_size == 0)
					ret.wram_size = 8;

				//iNES wants us to assume that no chr -> 8KB vram
				if (ret.chr_size == 0) ret.vram_size = 8;

				//let's not put a lot of hacks in here. that's what the databases are for.
				//for example of one not to add: videomation hack to change vram = 8 -> 16

				string mirror_memo = mirroring == 0 ? "horz" : (mirroring == 1 ? "vert" : "4screen");
				report.WriteLine("map={0},pr={1},ch={2},wr={3},vr={4},ba={5},mir={6}({7})", ret.mapper, ret.prg_size, ret.chr_size, ret.wram_size, ret.vram_size, ret.wram_battery ? 1 : 0, mirroring, mirror_memo);

				return ret;
			}
		}

	}
}