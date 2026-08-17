using BizHawk.Common.IOExtensions;

namespace BizHawk.Emulation.Cores.Properties {
	internal static class Resources {
		/// <param name="embedPath">Dir separator is '<c>.</c>'. Path is relative to <c>&lt;NS></c>.</param>
		private static byte[] ReadEmbeddedByteArray(string embedPath) => ReflectionCache.EmbeddedResourceStream($"Resources.{embedPath}").ReadAllBytes();

		internal static readonly Lazy<byte[]> CPC_AMSDOS_0_5_ROM = new(() => ReadEmbeddedByteArray("CPC_AMSDOS_0.5.ROM.zst"));
		internal static readonly Lazy<byte[]> CPC_BASIC_1_0_ROM = new(() => ReadEmbeddedByteArray("CPC_BASIC_1.0.ROM.zst"));
		internal static readonly Lazy<byte[]> CPC_BASIC_1_1_ROM = new(() => ReadEmbeddedByteArray("CPC_BASIC_1.1.ROM.zst"));
		internal static readonly Lazy<byte[]> CPC_OS_6128_ROM = new(() => ReadEmbeddedByteArray("CPC_OS_6128.ROM.zst"));
		internal static readonly Lazy<byte[]> DOSBOX_CONF_BASE = new(() => ReadEmbeddedByteArray("dosbox-x.base.conf"));
		internal static readonly Lazy<byte[]> DOSBOX_CONF_1981_IBM_5150 = new(() => ReadEmbeddedByteArray("dosbox-x.1981.ibm_xt5150.conf"));
		internal static readonly Lazy<byte[]> DOSBOX_CONF_1983_IBM_5160 = new(() => ReadEmbeddedByteArray("dosbox-x.1983.ibm_xt5160.conf"));
		internal static readonly Lazy<byte[]> DOSBOX_CONF_1986_IBM_5162 = new(() => ReadEmbeddedByteArray("dosbox-x.1986.ibm_xt5162.conf"));
		internal static readonly Lazy<byte[]> DOSBOX_CONF_1987_IBM_PS2_25 = new(() => ReadEmbeddedByteArray("dosbox-x.1987.ibm_ps2_25.conf"));
		internal static readonly Lazy<byte[]> DOSBOX_CONF_1990_IBM_PS2_25_286 = new(() => ReadEmbeddedByteArray("dosbox-x.1990.ibm_ps2_25_286.conf"));
		internal static readonly Lazy<byte[]> DOSBOX_CONF_1991_IBM_PS2_25_386 = new(() => ReadEmbeddedByteArray("dosbox-x.1991.ibm_ps2_25_386.conf"));
		internal static readonly Lazy<byte[]> DOSBOX_CONF_1993_IBM_PS2_53_SLC2_486 = new(() => ReadEmbeddedByteArray("dosbox-x.1993.ibm_ps2_53_slc2_486.conf"));
		internal static readonly Lazy<byte[]> DOSBOX_CONF_1994_IBM_PS2_76i_SLC2_486 = new(() => ReadEmbeddedByteArray("dosbox-x.1994.ibm_ps2_76i_slc2_486.conf"));
		internal static readonly Lazy<byte[]> DOSBOX_CONF_1997_IBM_APTIVA_2140 = new(() => ReadEmbeddedByteArray("dosbox-x.1997.ibm_aptiva_2140.conf"));
		internal static readonly Lazy<byte[]> DOSBOX_CONF_1999_IBM_THINKPAD_240 = new(() => ReadEmbeddedByteArray("dosbox-x.1999.ibm_thinkpad_240.conf"));
		internal static readonly Lazy<byte[]> DOSBOX_HDD_IMAGE_FAT16_21MB = new(() => ReadEmbeddedByteArray("dosbox-x.hdd.fat16.21mb.img.zst"));
		internal static readonly Lazy<byte[]> DOSBOX_HDD_IMAGE_FAT16_41MB = new(() => ReadEmbeddedByteArray("dosbox-x.hdd.fat16.41mb.img.zst"));
		internal static readonly Lazy<byte[]> DOSBOX_HDD_IMAGE_FAT16_241MB = new(() => ReadEmbeddedByteArray("dosbox-x.hdd.fat16.241mb.img.zst"));
		internal static readonly Lazy<byte[]> DOSBOX_HDD_IMAGE_FAT16_504MB = new(() => ReadEmbeddedByteArray("dosbox-x.hdd.fat16.504mb.img.zst"));
		internal static readonly Lazy<byte[]> DOSBOX_HDD_IMAGE_FAT16_2014MB = new(() => ReadEmbeddedByteArray("dosbox-x.hdd.fat16.2014mb.img.zst"));
		internal static readonly Lazy<byte[]> OS_464_ROM = new(() => ReadEmbeddedByteArray("OS_464.ROM.zst"));
		internal static readonly Lazy<byte[]> FastCgbBoot = new(() => ReadEmbeddedByteArray("cgb_boot.rom.zst"));
		internal static readonly Lazy<byte[]> FastAgbBoot = new(() => ReadEmbeddedByteArray("agb_boot.rom.zst"));
		internal static readonly Lazy<byte[]> FastDmgBoot = new(() => ReadEmbeddedByteArray("dmg_boot.rom.zst"));
		internal static readonly Lazy<byte[]> SameboyCgbBoot = new(() => ReadEmbeddedByteArray("sameboy_cgb_boot.rom.zst"));
		internal static readonly Lazy<byte[]> SameboyAgbBoot = new(() => ReadEmbeddedByteArray("sameboy_agb_boot.rom.zst"));
		internal static readonly Lazy<byte[]> SameboyDmgBoot = new(() => ReadEmbeddedByteArray("sameboy_dmg_boot.rom.zst"));
		internal static readonly Lazy<byte[]> SgbCartPresent_SPC = new(() => ReadEmbeddedByteArray("sgb-cart-present.spc.zst"));
		internal static readonly Lazy<byte[]> ZX_128_ROM = new(() => ReadEmbeddedByteArray("128.ROM.zst"));
		internal static readonly Lazy<byte[]> ZX_48_ROM = new(() => ReadEmbeddedByteArray("48.ROM.zst"));
		internal static readonly Lazy<byte[]> ZX_plus2_rom = new(() => ReadEmbeddedByteArray("plus2.rom.zst"));
		internal static readonly Lazy<byte[]> ZX_plus2a_rom = new(() => ReadEmbeddedByteArray("plus2a.rom.zst"));
		internal static readonly Lazy<byte[]> TMDS = new(() => ReadEmbeddedByteArray("tmds.zip.zst"));
		internal static readonly Lazy<byte[]> PIF_PAL_ROM = new(() => ReadEmbeddedByteArray("pif.pal.rom.zst"));
		internal static readonly Lazy<byte[]> PIF_NTSC_ROM = new(() => ReadEmbeddedByteArray("pif.ntsc.rom.zst"));
		internal static readonly Lazy<byte[]> JAGUAR_KSERIES_ROM = new(() => ReadEmbeddedByteArray("JAGUAR_KSERIES.ROM.zst"));
		internal static readonly Lazy<byte[]> JAGUAR_MSERIES_ROM = new(() => ReadEmbeddedByteArray("JAGUAR_MSERIES.ROM.zst"));
		internal static readonly Lazy<byte[]> JAGUAR_MEMTRACK_ROM = new(() => ReadEmbeddedByteArray("JAGUAR_MEMTRACK.ROM.zst"));
		internal static readonly Lazy<byte[]> DSDA_DOOM_WAD = new(() => ReadEmbeddedByteArray("dsda-doom.zst"));
	}
}
