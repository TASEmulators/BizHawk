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
		/// attempts to classify a rom based on iNES header information.
		/// this used to be way more complex. but later, we changed to have a board class implement a "MAPPERXXX" virtual board type and all hacks will be in there
		/// so theres nothing to do here but pick the board type corresponding to the cart
		/// </summary>
		static class iNESBoardDetector
		{
			public static string Detect(CartInfo cartInfo)
			{
				return string.Format("MAPPER{0:d3}",cartInfo.mapper);
			}
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
				if ((ROM_type & 8) != 0) mirroring += 2;
				if (mirroring == 0) ret.pad_v = 1;
				else if (mirroring == 1) ret.pad_h = 1;
				ret.inesmirroring = mirroring;
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
				{
					report.WriteLine("iNES wr=0 interpreted as wr=8");
					ret.wram_size = 8;
				}

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