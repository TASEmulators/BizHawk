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
		//public class RomHeaderInfo
		//{
		//    public int MapperNo, Mirroring, Num_PRG_Banks, Num_CHR_Banks, Num_PRAM_Banks;
		//    public bool Battery;
		//    public byte[] ROM, VROM;
		//}


		unsafe struct iNES_HEADER
		{
			public fixed byte ID[4]; /*NES^Z*/
			public byte ROM_size;
			public byte VROM_size;
			public byte ROM_type;
			public byte ROM_type2;
			public fixed byte reserve[8];

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

			public RomInfo Analyze()
			{
				var ret = new RomInfo();
				ret.MapperNumber = (ROM_type >> 4);
				ret.MapperNumber |= (ROM_type2 & 0xF0);
				int mirroring = (ROM_type & 1);
				if ((ROM_type & 8) != 0) mirroring = 2;
				if (mirroring == 0) ret.MirrorType = EMirrorType.Horizontal;
				else if (mirroring == 1) ret.MirrorType = EMirrorType.Vertical;
				else ret.MirrorType = EMirrorType.External;
				ret.PRG_Size = ROM_size;
				if (ret.PRG_Size == 0)
					ret.PRG_Size = 256;
				ret.CHR_Size = VROM_size;
				ret.Battery = (ROM_type & 2) != 0;

				fixed (iNES_HEADER* self = &this) ret.PRAM_Size = self->reserve[0] * 8;
				//0 is supposed to mean 1 (for compatibility, as this is an extension to original iNES format)
				if (ret.PRAM_Size == 0) ret.PRAM_Size = 8;

				Console.WriteLine("iNES header: map:{0}, mirror:{1}, PRG:{2}, CHR:{3}, CRAM:{4}, PRAM:{5}, bat:{6}", ret.MapperNumber, ret.MirrorType, ret.PRG_Size, ret.CHR_Size, ret.CRAM_Size, ret.PRAM_Size, ret.Battery ? 1 : 0);

				//fceux calls uppow2(PRG_Banks) here, and also ups the chr size as well
				//then it does something complicated that i don't understand with making sure it doesnt read too much data
				//fceux only allows this condition for mappers in the list "not_power2" which is only 228

				return ret;
			}
		}

	}
}