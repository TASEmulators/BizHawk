using System;

using BizHawk.Common.IOExtensions;

namespace BizHawk.Emulation.Cores.Properties {
	internal static class Resources {
		/// <param name="embedPath">Dir separator is '<c>.</c>'. Path is relative to <c>&lt;NS></c>.</param>
		private static byte[] ReadEmbeddedByteArray(string embedPath) => Emulation.Cores.ReflectionCache.EmbeddedResourceStream($"Resources.{embedPath}").ReadAllBytes();

		internal static readonly Lazy<byte[]> CPC_AMSDOS_0_5_ROM = new Lazy<byte[]>(() => ReadEmbeddedByteArray("CPC_AMSDOS_0.5.ROM.gz"));
		internal static readonly Lazy<byte[]> CPC_BASIC_1_0_ROM = new Lazy<byte[]>(() => ReadEmbeddedByteArray("CPC_BASIC_1.0.ROM.gz"));
		internal static readonly Lazy<byte[]> CPC_BASIC_1_1_ROM = new Lazy<byte[]>(() => ReadEmbeddedByteArray("CPC_BASIC_1.1.ROM.gz"));
		internal static readonly Lazy<byte[]> CPC_OS_6128_ROM = new Lazy<byte[]>(() => ReadEmbeddedByteArray("CPC_OS_6128.ROM.gz"));
		internal static readonly Lazy<byte[]> OS_464_ROM = new Lazy<byte[]>(() => ReadEmbeddedByteArray("OS_464.ROM.gz"));
		internal static readonly Lazy<byte[]> FastCgbBoot = new Lazy<byte[]>(() => ReadEmbeddedByteArray("cgb_boot.rom.gz"));
		internal static readonly Lazy<byte[]> FastAgbBoot = new Lazy<byte[]>(() => ReadEmbeddedByteArray("agb_boot.rom.gz"));
		internal static readonly Lazy<byte[]> FastDmgBoot = new Lazy<byte[]>(() => ReadEmbeddedByteArray("dmg_boot.rom.gz"));
		internal static readonly Lazy<byte[]> SameboyCgbBoot = new Lazy<byte[]>(() => ReadEmbeddedByteArray("sameboy_cgb_boot.rom.gz"));
		internal static readonly Lazy<byte[]> SameboyAgbBoot = new Lazy<byte[]>(() => ReadEmbeddedByteArray("sameboy_agb_boot.rom.gz"));
		internal static readonly Lazy<byte[]> SameboyDmgBoot = new Lazy<byte[]>(() => ReadEmbeddedByteArray("sameboy_dmg_boot.rom.gz"));
		internal static readonly Lazy<byte[]> SgbCartPresent_SPC = new Lazy<byte[]>(() => ReadEmbeddedByteArray("sgb-cart-present.spc.gz"));
		internal static readonly Lazy<byte[]> ZX_128_ROM = new Lazy<byte[]>(() => ReadEmbeddedByteArray("128.ROM.gz"));
		internal static readonly Lazy<byte[]> ZX_48_ROM = new Lazy<byte[]>(() => ReadEmbeddedByteArray("48.ROM.gz"));
		internal static readonly Lazy<byte[]> ZX_plus2_rom = new Lazy<byte[]>(() => ReadEmbeddedByteArray("plus2.rom.gz"));
		internal static readonly Lazy<byte[]> ZX_plus2a_rom = new Lazy<byte[]>(() => ReadEmbeddedByteArray("plus2a.rom.gz"));
	}
}
