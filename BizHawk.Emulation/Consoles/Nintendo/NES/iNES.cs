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
					Table.TryGetValue(key, out board);
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

//MAP PRG CHR WRAM VRAM BOARD
static string ClassifyTable = @"
0	16	0	8	8	NROM-HOMEBREW; some of blargg's test (sprite tests)
0	16	8	8	0	NES-NROM-128; balloon fight, but its broken right now
0	32	8	8	0	NES-NROM-256; super mario bros
1	32	32	8	0	NES-SEROM; lolo
1	128	0	8	0	NES-SNROM; zelda
1	128	128	8	0	NES-SKROM; zelda 2
1	32	0	8	8	NROM-HOMEBREW; instr_timing.nes
1	64	0	8	8	NROM-HOMEBREW; instr_misc.nes
1	80	0	8	8	NROM-HOMEBREW; blargg's cpu_interrupts.nes
1	128	0	8	8	NES-SNROM; some of blargg's tests (apu) [TODO recheck as NROM-HOMEBREW]
1	256	0	8	8	NES-SNROM; some of blargg's test (cpu tests) [TODO recheck as NROM-HOMEBREW]
2	128	0	8	0	NES-UNROM; mega man
2	256	0	8	0	NES-UOROM; paperboy 2
2	128	0	8	8	HVC-UNROM; JJ - Tobidase Daisakusen Part 2 (J)
3	32	32	8	0	NES-CNROM; adventure island
4	128	128	8	0	NES-TSROM; double dragon 2 (should be TL1ROM but maybe this will work)
4	32	8	8	0	TXROM-HOMEBREW; blargg's mmc3 tests
7	128	0	8	0	NES-ANROM; marble madness
7	256	0	8	8	NES-AOROM; battletoads
11	32	16	8	0	Discrete_74x377
11	16	32	8	0	Discrete_74x377
13	32	0	8	16	NES-CPROM; videomation
66	64	16	8	0	NES-MHROM; super mario bros / duck hunt
66	128	32	8	0	NES-GNROM; gumshoe
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
					if (0 == Util.memcmp((char*)(self) + 0x7, "DiskDude", 8))
					{
						Util.memset((char*)(self) + 0x7, 0, 0x9);
					}

					if (0 == Util.memcmp((char*)(self) + 0x7, "demiforce", 9))
					{
						Util.memset((char*)(self) + 0x7, 0, 0x9);
					}

					if (0 == Util.memcmp((char*)(self) + 0xA, "Ni03", 4))
					{
						if (0 == Util.memcmp((char*)(self) + 0x7, "Dis", 3))
							Util.memset((char*)(self) + 0x7, 0, 0x9);
						else
							Util.memset((char*)(self) + 0xA, 0, 0x6);
					}
				}
			}

			public CartInfo Analyze()
			{
				var ret = new CartInfo();
				ret.game = new GameInfo();
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
					Console.WriteLine("Looks like you have an iNES 2.0 header, or some other kind of weird garbage.");
					Console.WriteLine("We haven't bothered to support iNES 2.0.");
					Console.WriteLine("We might, if we can find anyone who uses it. Let us know.");
				}

				ret.wram_size = (short)(wram_size * 8);
				//0 is supposed to mean 8KB (for compatibility, as this is an extension to original iNES format)
				if (ret.wram_size == 0)
					ret.wram_size = 8;

				//iNES wants us to assume that no chr -> 8KB vram
				if (ret.chr_size == 0) ret.vram_size = 8;

				//let's not put a lot of hacks in here. that's what the databases are for.
				//for example of one not to add: videomation hack to change vram = 8 -> 16

				Console.WriteLine("iNES: map:{0}, mirror:{1}, PRG:{2}, CHR:{3}, WRAM:{4}, VRAM:{5}, bat:{6}", ret.mapper, mirroring, ret.prg_size, ret.chr_size, ret.wram_size, ret.vram_size, ret.wram_battery ? 1 : 0);

				return ret;
			}
		}

	}
}