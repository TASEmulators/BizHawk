using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Emulation.Cores.Components.H6280;

namespace BizHawk.Emulation.Cores.PCEngine
{
	partial class PCEngine
	{
		static void CDLMappingApplyRange(HuC6280.MemMapping[] mm, string name, int block, int len)
		{
			for (int i = block, offs = 0; i < 256 && len > offs; i++, offs += 8192)
			{
				mm[i].Name = name;
				mm[i].Offs = offs;
			}
		}

	    /// <summary>
		/// informs the CPU of the general memory layout, so it can do CDL
	    /// </summary>
		void InitCDLMappings()
		{
			// todo: arcade card

			var mm = new HuC6280.MemMapping[256];

			CDLMappingApplyRange(mm, "ROM", 0x00, RomLength);
			if (PopulousRAM != null)
				CDLMappingApplyRange(mm, "Cart Battery RAM",, 0x40, PopulousRAM.Length);

			if (SuperRam != null)
				CDLMappingApplyRange(mm, "Super System Card RAM", 0x68, SuperRam.Length);

			if (CDRam != null)
				CDLMappingApplyRange(mm, "TurboCD RAM", 0x80, CDRam.Length);

			if (BRAM != null)
				CDLMappingApplyRange(mm, "Battery RAM", 0xf7, BRAM.Length);

			{
				var rammirrors = new HuC6280.MemMapping { Name = "Main Memory", Offs = 0 };
				mm[0xf9] = mm[0xfa] = mm[0xfb] = rammirrors;
			}
			CDLMappingApplyRange(mm, "Main Memory", 0xf8, Ram.Length);

			mm[0xff] = new HuC6280.MemMapping { Name = "MMIO", Offs = 0 };

			for (int i = 0; i < 256; i++)
			{
				if (mm[i].Name == null)
					mm[i].Name = "UNKNOWN";
			}

			Cpu.Mappings = mm;
		}
	}
}
