using System.IO;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.H6280;

namespace BizHawk.Emulation.Cores.PCEngine
{
	public sealed partial class PCEngine : ICodeDataLogger
	{
		public void SetCDL(ICodeDataLog cdl)
		{
			Cpu.CDL = cdl;
		}

		public void NewCDL(ICodeDataLog cdl)
		{
			InitCDLMappings();
			var mm = this.Cpu.Mappings;
			foreach (var kvp in SizesFromHuMap(mm))
			{
				cdl[kvp.Key] = new byte[kvp.Value];
			}

			cdl.SubType = "PCE";
			cdl.SubVer = 0;
		}

		public void DisassembleCDL(Stream s, ICodeDataLog cdl)
		{
			Cpu.DisassembleCDL(s, cdl, _memoryDomains);
		}

		private static void CDLMappingApplyRange(HuC6280.MemMapping[] mm, string name, int block, int len, int initialoffs = 0)
		{
			for (int i = block, offs = initialoffs; i < 256 && len > (offs - initialoffs); i++, offs += 8192)
			{
				mm[i].Name = name;
				mm[i].Offs = offs;
			}
		}

		/// <summary>
		/// informs the CPU of the general memory layout, so it can do CDL
		/// </summary>
		private void InitCDLMappings()
		{
			if (Cpu.Mappings != null)
			{
				return;
			}

			SF2UpdateCDLMappings = true;

			var mm = new HuC6280.MemMapping[256];

			CDLMappingApplyRange(mm, "ROM", 0x00, Math.Min(RomLength, 1024 * 1024));
			if (PopulousRAM != null)
			{
				CDLMappingApplyRange(mm, "Cart Battery RAM", 0x40, PopulousRAM.Length);
			}

			// actual games came in 128K, 256K, 384K, 512K, 768K, 1024K, and Street Fighter sizes
			// except street fighter, games were on 1 or 2 mask roms
			// 1 maskrom: POT size rom, high address lines ignored, mirrored throughout 1M
			// 2 maskrom: (POT + POT) size rom, high address lines ignored, one chip enabled in first 512K,
			//   second chip enabled in second 512K
			// this means that for the one case of 384K, there's not a mirror of everything contiguous starting from org 0
			if (RomLength == 640 * 1024) // 384K has been preprocessed up to 640K, including some dummy areas
			{
				for (int i = 0x20; i < 0x40; i++)
				{
					// mark as unknown mirrors
					mm[i].Name = null;
					mm[i].Offs = 0;
				}

				for (int i = 0x40; i < 0x50; i++)
				{
					// rebase
					mm[i].Offs -= 0x40000;
				}
			}

			if (RomLength > 1024 * 1024)
			{
				mm[0x7f].VOffs = 0x27e000; // hint that the total size of this domain will be 2.5MiB
			}

			if (SuperRam != null)
			{
				CDLMappingApplyRange(mm, "Super System Card RAM", 0x68, SuperRam.Length);
			}

			if (CDRam != null)
			{
				CDLMappingApplyRange(mm, "TurboCD RAM", 0x80, CDRam.Length);
			}

			if (BRAM != null)
			{
				CDLMappingApplyRange(mm, "Battery RAM", 0xf7, BRAM.Length);
			}

			var ramMirrors = new HuC6280.MemMapping { Name = "Main Memory", Offs = 0 };
			mm[0xf9] = mm[0xfa] = mm[0xfb] = ramMirrors;

			CDLMappingApplyRange(mm, "Main Memory", 0xf8, Ram.Length);

			mm[0xff] = new HuC6280.MemMapping { Name = "MMIO", Offs = 0 };

			for (int i = 0; i < 256; i++)
			{
				if (mm[i].Name == null)
				{
					mm[i].Name = "UNKNOWN";
				}
			}

			Cpu.Mappings = mm;
		}
	}
}
