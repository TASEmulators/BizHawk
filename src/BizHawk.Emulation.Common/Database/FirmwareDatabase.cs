#nullable disable

using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable StringLiteralTypo
namespace BizHawk.Emulation.Common
{
	public static class FirmwareDatabase
	{
		public static IEnumerable<FirmwareFile> FirmwareFiles => FirmwareFilesByHash.Values;

		public static readonly IReadOnlyDictionary<string, FirmwareFile> FirmwareFilesByHash;

		public static readonly IReadOnlyDictionary<FirmwareOption, FirmwareFile> FirmwareFilesByOption;

		public static readonly IReadOnlyCollection<FirmwareOption> FirmwareOptions;

		public static readonly IReadOnlyCollection<FirmwareRecord> FirmwareRecords;

		public static readonly IReadOnlyList<FirmwarePatchOption> AllPatches;

		static FirmwareDatabase()
		{
			List<FirmwarePatchOption> allPatches = new();
			Dictionary<string, FirmwareFile> filesByHash = new();
			Dictionary<FirmwareOption, FirmwareFile> filesByOption = new();
			List<FirmwareOption> options = new();
			List<FirmwareRecord> records = new();

			FirmwareFile File(
				string hash,
				long size,
				string recommendedName,
				string desc,
				string additionalInfo = "",
				bool isBad = false)
			{
				FirmwareFile ff = new(
					hash: hash,
					size: size,
					recommendedName: recommendedName,
					desc: desc,
					additionalInfo: additionalInfo,
					isBad: isBad);
				return filesByHash[ff.Hash/*may have been reformatted*/] = ff;
			}

			void Option(string systemId, string id, in FirmwareFile ff, FirmwareOptionStatus status = FirmwareOptionStatus.Acceptable)
			{
				var option = new FirmwareOption(new(systemId, id), ff.Hash, ff.Size, ff.IsBad ? FirmwareOptionStatus.Bad : status);
				options.Add(option);
				filesByOption[option] = ff;
			}

			void Firmware(string systemId, string id, string desc)
				=> records.Add(new(new(systemId, id), desc));

			void FirmwareAndOption(string hash, long size, string systemId, string id, string name, string desc)
			{
				Firmware(systemId, id, desc);
				Option(systemId, id, File(hash, size, name, desc), FirmwareOptionStatus.Ideal);
			}

			void AddPatchAndMaybeReverse(FirmwarePatchOption fpo)
			{
				allPatches.Add(fpo);
				if (fpo.Patches.Any(fpd => fpd.Overwrite)) return;
				allPatches.Add(new(fpo.TargetHash, fpo.Patches.Reverse().ToArray(), fpo.BaseHash));
			}

			// 32X
			FirmwareAndOption("DBEBD76A448447CB6E524AC3CB0FD19FC065D944", 256, "32X", "G", "32X_G_BIOS.BIN", "32x 68k BIOS");
			FirmwareAndOption("1E5B0B2441A4979B6966D942B20CC76C413B8C5E", 2048, "32X", "M", "32X_M_BIOS.BIN", "32x SH2 MASTER BIOS");
			FirmwareAndOption("4103668C1BBD66C5E24558E73D4F3F92061A109A", 1024, "32X", "S", "32X_S_BIOS.BIN", "32x SH2 SLAVE BIOS");

			// 3DS
			// bleh, undefined hash AND size...
			FirmwareAndOption(SHA1Checksum.Dummy, 0, "3DS", "aes_keys", "aes_keys.txt", "AES Keys");
			FirmwareAndOption(SHA1Checksum.Dummy, 0, "3DS", "seeddb", "seeddb.bin", "SEEDDB");

			// Amiga
			Firmware("Amiga", "A500", "A500 Kickstart");
			Option("Amiga", "A500", File("891E9A547772FE0C6C19B610BAF8BC4EA7FCB785", 262144, "amiga-os-130.rom", "Kickstart v1.3 r34.005 (1987-12)(Commodore)(A500-A1000-A2000-CDTV)[!]"), FirmwareOptionStatus.Ideal);
			Option("Amiga", "A500", File("11F9E62CF299F72184835B7B2A70A16333FC0D88", 262144, "amiga-os-120.rom", "Kickstart v1.2 r33.180 (1986-10)(Commodore)(A500-A1000-A2000)[!]"));
			Option("Amiga", "A500", File("3B7F1493B27E212830F989F26CA76C02049F09CA", 524288, "amiga-os-310-a600.rom", "Kickstart v3.1 r40.063 (1993-07)(Commodore)(A500-A600-A2000)[!]"));

			Firmware("Amiga", "A600", "A500+/A600/A600HD Kickstart");
			Option("Amiga", "A600", File("02843C4253BBD29ABA535B0AA3BD9A85034ECDE4", 524288, "amiga-os-205-a600.rom", "Kickstart v2.05 r37.350 (1992-04)(Commodore)(A600HD)[!]"), FirmwareOptionStatus.Ideal);
			Option("Amiga", "A600", File("C5839F5CB98A7A8947065C3ED2F14F5F42E334A1", 524288, "amiga-os-204.rom", "Kickstart v2.04 r37.175 (1991-05)(Commodore)(A500+)[!]"));
			Option("Amiga", "A600", File("3B7F1493B27E212830F989F26CA76C02049F09CA", 524288, "amiga-os-310-a600.rom", "Kickstart v3.1 r40.063 (1993-07)(Commodore)(A500-A600-A2000)[!]"));

			Firmware("Amiga", "A1200", "A1200 Kickstart");
			Option("Amiga", "A1200", File("E21545723FE8374E91342617604F1B3D703094F1", 524288, "amiga-os-310-a1200.rom", "Kickstart v3.1 r40.068 (1993-12)(Commodore)(A1200)[!]"), FirmwareOptionStatus.Ideal);
			Option("Amiga", "A1200", File("70033828182FFFC7ED106E5373A8B89DDA76FAA5", 524288, "amiga-os-300-a1200.rom", "Kickstart v3.0 r39.106 (1992-09)(Commodore)(A1200)[!]"));

			Firmware("Amiga", "A4000", "A4000/A4000T Kickstart");
			Option("Amiga", "A4000", File("5FE04842D04A489720F0F4BB0E46948199406F49", 524288, "amiga-os-310-a4000.rom", "Kickstart v3.1 r40.068 (1993-12)(Commodore)(A4000)[!]"), FirmwareOptionStatus.Ideal);
			Option("Amiga", "A4000", File("F0B4E9E29E12218C2D5BD7020E4E785297D91FD7", 524288, "amiga-os-300-a4000.rom", "Kickstart v3.0 r39.106 (1992-09)(Commodore)(A4000)[!]"));
			Option("Amiga", "A4000", File("B0EC8B84D6768321E01209F11E6248F2F5281A21", 524288, "amiga-os-310-a4000t.rom", "Kickstart v3.1 r40.070 (1994-02)(Commodore)(A4000T)[!]"));

			Firmware("Amiga", "CD32", "CD32 Kickstart");
			Option("Amiga", "CD32", File("3525BE8887F79B5929E017B42380A79EDFEE542D", 524288, "amiga-os-310-cd32.rom", "Kickstart v3.1 r40.060 (1993-05)(Commodore)(CD32)[!].rom"), FirmwareOptionStatus.Ideal);
			Firmware("Amiga", "CD32ext", "CD32 Extended ROM");
			Option("Amiga", "CD32ext", File("5BEF3D628CE59CC02A66E6E4AE0DA48F60E78F7F", 524288, "amiga-os-310-cd32-ext.rom", "CD32 Extended-ROM r40.60 (1993)(Commodore)(CD32).rom"), FirmwareOptionStatus.Ideal);

			// A7800
			Firmware("A78", "Bios_NTSC", "NTSC Bios");
#if false
			Option("A78", "Bios_NTSC", File("CE236581AB7921B59DB95BA12837C22F160896CB", 4096, "A78_NTSC_speed_bios.bin", "NTSC Bios speed"));
#endif
			Option("A78", "Bios_NTSC", File("D9D134BB6B36907C615A594CC7688F7BFCEF5B43", 4096, "A78_NTSC_bios.bin", "NTSC Bios"));
			FirmwareAndOption("5A140136A16D1D83E4FF32A19409CA376A8DF874", 16384, "A78", "Bios_PAL", "A78_PAL_BIOS.bin", "PAL Bios");
			FirmwareAndOption("A3AF676991391A6DD716C79022D4947206B78164", 4096, "A78", "Bios_HSC", "A78_highscore.bin", "Highscore Bios");

			// AppleII
			Firmware("AppleII", "AppleIIe", "AppleIIe.rom");
			var appleII_AppleIIe = File("B8EA90ABE135A0031065E01697C4A3A20D51198B", 16384, "AppleIIe.rom", "Apple II e");
			Option("AppleII", "AppleIIe", in appleII_AppleIIe);
			Firmware("AppleII", "DiskII", "DiskII.rom");
			var appleII_DiskII = File("D4181C9F046AAFC3FB326B381BAAC809D9E38D16", 256, "AppleIIe_DiskII.rom", "Disk II");
			Option("AppleII", "DiskII", in appleII_DiskII);

			// C64
			FirmwareAndOption("1D503E56DF85A62FEE696E7618DC5B4E781DF1BB", 8192, "C64", "Kernal", "C64_Kernal.bin", "Kernal Rom");
			FirmwareAndOption("79015323128650C742A3694C9429AA91F355905E", 8192, "C64", "Basic", "C64_Basic.bin", "Basic Rom");
			FirmwareAndOption("ADC7C31E18C7C7413D54802EF2F4193DA14711AA", 4096, "C64", "Chargen", "C64_Chargen.bin", "Chargen Rom");
			FirmwareAndOption("AB16F56989B27D89BABE5F89C5A8CB3DA71A82F0", 16384, "C64", "Drive1541", "C64_Drive-1541.bin", "1541 Disk Drive Rom");
			FirmwareAndOption("D3B78C3DBAC55F5199F33F3FE0036439811F7FB3", 16384, "C64", "Drive1541II", "C64_Drive-1541ii.bin", "1541-II Disk Drive Rom");

			// Channel F
			FirmwareAndOption("81193965A374D77B99B4743D317824B53C3E3C78", 1024, "ChannelF", "ChannelF_sl131253", "ChannelF_SL31253.rom", "Channel F BIOS (PSU1)");
			FirmwareAndOption("8F70D1B74483BA3A37E86CF16C849D601A8C3D2C", 1024, "ChannelF", "ChannelF_sl131254", "ChannelF_SL31254.rom", "Channel F BIOS (PSU2)");
			FirmwareAndOption("759E2ED31FBDE4A2D8DAF8B9F3E0DFFEBC90DAE2", 1024, "ChannelF", "ChannelF_sl90025", "ChannelF_sl90025.rom", "Channel F II BIOS (PSU1)");

			// Coleco
			FirmwareAndOption("45BEDC4CBDEAC66C7DF59E9E599195C778D86A92", 8192, "Coleco", "Bios", "Coleco_Bios.bin", "Bios");

			// GBA
			var gbaNormal = File("300C20DF6731A33952DED8C436F7F186D25D3492", 16384, "GBA_bios.rom", "Bios (World)");
			var gbaJDebug = File("AA98A2AD32B86106340665D1222D7D973A1361C7", 16384, "GBA_bios_Debug-(J).rom", "Bios (J Debug)");
			Firmware("GBA", "Bios", "Bios");
			Option("GBA", "Bios", in gbaNormal);
			Option("GBA", "Bios", in gbaJDebug);

			// GB
			Firmware("GB", "World", "Game Boy Boot Rom");
			Option("GB", "World", File("4ED31EC6B0B175BB109C0EB5FD3D193DA823339F", 256, "GB_dmg.bin", "Game Boy Boot Rom"), FirmwareOptionStatus.Ideal);
			// Early revisions of GB/C boot ROMs are not well-supported because the corresponding CPU differences are not emulated.
			Option("GB", "World", File("8BD501E31921E9601788316DBD3CE9833A97BCBC", 256, "GB_dmg0.bin", "Game Boy Boot Rom (Early J Revision)"), FirmwareOptionStatus.Unacceptable);
			Option("GB", "World", File("4E68F9DA03C310E84C523654B9026E51F26CE7F0", 256, "GB_mgb.bin", "Game Boy Boot Rom (Pocket)"), FirmwareOptionStatus.Acceptable);
			FirmwarePatchData gbCommonPatchAt0xFD = new(0xFD, new byte[] { 0xFE }); // 2 pairs, all have either 0x01 or 0xFF at this octet
			AddPatchAndMaybeReverse(new(
				"4ED31EC6B0B175BB109C0EB5FD3D193DA823339F",
				gbCommonPatchAt0xFD,
				"4E68F9DA03C310E84C523654B9026E51F26CE7F0"));

			// these are only used for supported SGB cores
			// placed in GB as these are within the Game Boy side rather than the SNES side
			Firmware("GB", "SGB", "Super Game Boy Boot Rom");
			Option("GB", "SGB", File("AA2F50A77DFB4823DA96BA99309085A3C6278515", 256, "GB_sgb.bin", "Super Game Boy Boot Rom"), FirmwareOptionStatus.Ideal);
			Firmware("GB", "SGB2", "Super Game Boy 2 Boot Rom");
			Option("GB", "SGB2", File("93407EA10D2F30AB96A314D8ECA44FE160AEA734", 256, "GB_sgb2.bin", "Super Game Boy 2 Boot Rom"), FirmwareOptionStatus.Ideal);
			AddPatchAndMaybeReverse(new(
				"AA2F50A77DFB4823DA96BA99309085A3C6278515",
				gbCommonPatchAt0xFD,
				"93407EA10D2F30AB96A314D8ECA44FE160AEA734"));

			// GBC
			Firmware("GBC", "World", "Game Boy Color Boot Rom");
			Option("GBC", "World", File("1293D68BF9643BC4F36954C1E80E38F39864528D", 2304, "GBC_cgb.bin", "Game Boy Color Boot Rom"), FirmwareOptionStatus.Ideal);
			// CGB E models have the logo reading TOCTOU patched (only relevant for bootlegs that logo swap)
			Option("GBC", "World", File("F5F33729A956131D9C44310F0AE3BB0599E9EC3E", 2304, "GBC_cgbE.bin", "Game Boy Color Boot Rom (Late Revision)"));
			Option("GBC", "World", File("DF5A0D2D49DE38FBD31CC2AAB8E62C8550E655C0", 2304, "GBC_cgb0.bin", "Game Boy Color Boot Rom (Early Revision)"), FirmwareOptionStatus.Unacceptable);
			Firmware("GBC", "AGB", "Game Boy Color Boot Rom (GBA)");
			Option("GBC", "AGB", File("FA5287E24B0FA533B3B5EF2B28A81245346C1A0F", 2304, "GBC_agb.bin", "Game Boy Color Boot Rom (GBA)"), FirmwareOptionStatus.Ideal);
			// early GBAs did not patch the logo reading TOCTOU (only relevant for bootlegs that logo swap)
			Option("GBC", "AGB", File("0DAAC31ACB6CB346FC954368ACB02ACB3ADCC3AB", 2304, "GBC_agb0.bin", "Game Boy Color Boot Rom (GBA, Early Revision)"));
			// functionally equal to agb0.bin (no TOCTOU patch)
			Option("GBC", "AGB", File("1ECAFA77AB3172193F3305486A857F443E28EBD9", 2304, "GBC_agb_gambatte.bin", "Game Boy Color Boot Rom (GBA, Gambatte RE)"), FirmwareOptionStatus.Bad);
			AddPatchAndMaybeReverse(new(
				"1293D68BF9643BC4F36954C1E80E38F39864528D",
				new FirmwarePatchData(0xF3, new byte[] { 0x03, 0x00, 0xCD, 0x1D, 0xD5, 0xAA, 0x4F, 0x90, 0x74 }),
				"1ECAFA77AB3172193F3305486A857F443E28EBD9"));

			// Genesis/CD
			FirmwareAndOption("3F50B76B0529DB7F79C396B5E808CC0786FFC311", 2048, "GEN", "Boot", "GEN_boot.md", "Genesis Boot Rom (World)");

			Firmware("GEN", "CD_BIOS_EU", "Mega CD Bios (Europe)");
			Firmware("GEN", "CD_BIOS_JP", "Mega CD Bios (Japan)");
			Firmware("GEN", "CD_BIOS_US", "Sega CD Bios (USA)");

			var jp_mcda_211c = File("219D284DCF63CE366A4DC6D1FF767A0D2EEA283D", 131072, "MCD_aiwa_jp_211c.bin", "Mega CD Aiwa JP (v2.11c)");
			var us_scd1_100 = File("C5C24E6439A148B7F4C7EA269D09B7A23FE25075", 131072, "SCD_m1_us_100.bin", "Sega CD Model 1 US (v1.00)");
			var us_scd1_100_h = File("2F397218764502F184F23055055BC5728C71F259", 131072, "SCD_m1_us_100[h].bin", "Sega CD Model 1 US (v1.00) [h]", isBad: true);
			var us_scd1_110 = File("F4F315ADCEF9B8FEB0364C21AB7F0EAF5457F3ED", 131072, "SCD_m1_us_110.bin", "Sega CD Model 1 US (v1.10)");
			var us_scd2_200 = File("5A8C4B91D3034C1448AAC4B5DC9A6484FCE51636", 131072, "SCD_m2_us_200.bin", "Sega CD Model 2 US (v2.00)");
			var us_scd2_200_b = File("BD3EE0C8AB732468748BF98953603CE772612704", 131072, "SCD_m2_us_200[b].bin", "Sega CD Model 2 US (v2.00) [b]", isBad: true);
			var us_scd2_200w = File("5ADB6C3AF218C60868E6B723EC47E36BBDF5E6F0", 131072, "SCD_m2_us_200w.bin", "Sega CD Model 2 US (v2.00w)");
			var us_scd2_200w_b = File("27358448FE5514C90AC25430851EF075B7ADC0DB", 131072, "SCD_m2_us_200w[b].bin", "Sega CD Model 2 US (v2.00w) [b]", isBad: true);
			var us_scd2_211x = File("328A3228C29FBA244B9DB2055ADC1EC4F7A87E6B", 131072, "SCD_m2_us_211x.bin", "Sega CD Model 2 US (v2.11x)");
			var us_scd2_211x_b = File("0CCB6A3589F2FA6E70BD4996578AD106B8C6D35C", 131072, "SCD_m2_us_211x[b].bin", "Sega CD Model 2 US (v2.11x) [b]", isBad: true);
			var us_gcdx_221x = File("2B125C0545AFA089B617F2558E686EA723BDC06E", 131072, "GCDX_us_221x.bin", "Genesis CDX US (v2.21x)");
			var us_gcdx_221x_b = File("830B414197F388DA0ABF2B5FF55DAFC84781D9E5", 131072, "GCDX_us_221x[b].bin", "Genesis CDX US (v2.21x) [b]", isBad: true);
			var jp_mcd2_200c = File("D203CFE22C03AE479DD8CA33840CF8D9776EB3FF", 131072, "MCD_2_jp_200c.bin", "Mega CD 2 JP (v2.00c)");
			var jp_mcd2_200c_b2 = File("DC68146AD1FF50FAEEB2A3F685FAB1A2961DABB2", 131072, "MCD_2_jp_200c[b2].bin", "Mega CD 2 JP (v2.00c) [b2]", isBad: true);
			var jp_mcd2_200c_b = File("762D20EBB85B980C17C53F928C002D747920A281", 131072, "MCD_2_jp_200c[b].bin", "Mega CD 2 JP (v2.00c) [b]", isBad: true);
			var eu_mcd_100 = File("F891E0EA651E2232AF0C5C4CB46A0CAE2EE8F356", 131072, "MCD_eu_100.bin", "Mega CD EU (v1.00)");
			var jp_mcd_100g = File("6A40A5CEC00C3B49A4FD013505C5580BAA733A29", 131072, "MCD_jp_100g.bin", "Mega CD JP (v1.00g)");
			var jp_mcd_100l = File("0D5485E67C3F033C41D677CC9936AFD6AD618D5F", 131072, "MCD_jp_100l.bin", "Mega CD JP (v1.00l)");
			var jp_mcd_100o = File("9E1495E62B000E1E1C868C0F3B6982E1ABBB8A94", 131072, "MCD_jp_100o.bin", "Mega CD JP (v1.00o)");
			var jp_mcd_100p = File("4846F448160059A7DA0215A5DF12CA160F26DD69", 131072, "MCD_jp_100p.bin", "Mega CD JP (v1.00p)");
			var jp_mcd_100p_b = File("2BD871E53960BC0202C948525C02584399BC2478", 131074, "MCD_jp_100p[b].bin", "Mega CD JP (v1.00p) [b]", isBad: true);
			var as_mcd_100s = File("E4193C6AE44C3CEA002707D2A88F1FBCCED664DE", 131072, "MCD_as_100s.bin", "Mega CD AS (v1.00s)");
			var jp_mcd_100s = File("230EBFC49DC9E15422089474BCC9FA040F2C57EB", 131072, "MCD_jp_100s.bin", "Mega CD JP (v1.00s)");
			var eu_mcdii_200 = File("7063192AE9F6B696C5B81BC8F0A9FE6F0C400E58", 131072, "MCD_eu_200.bin", "Mega CD II EU (v2.00)");
			var eu_mcdii_200_b2 = File("CFCF092E0A70779FC5912DA0FBD154838DF997DA", 131072, "MCD_eu_200[b2].bin", "Mega CD II EU (v2.00) [b2]", isBad: true);
			var eu_mcdii_200_b = File("0CBA6B33B306A293471D3697CF30F2A4D20673EB", 131072, "MCD_eu_200[b].bin", "Mega CD II EU (v2.00) [b]", isBad: true);
			var eu_mcdii_200w = File("F5F60F03501908962446EE02FC27D98694DD157D", 131072, "MCD_eu_200w.bin", "Mega CD II EU (v2.00w)");
			var eu_mcdii_200w_b2 = File("45134EF8655B9D06B130726786EFE2F8B1D430A3", 131072, "MCD_eu_200w[b2].bin", "Mega CD II EU (v2.00w) [b2]", isBad: true);
			var eu_mcdii_200w_b = File("523B3125FB0AC094E16AA072BC6CCDCA22E520E5", 131072, "MCD_eu_200w[b].bin", "Mega CD II EU (v2.00w) [b]", isBad: true);
			var eu_mm_221x = File("75548AC9AAA6E81224499F9A1403B2B42433F5B7", 131072, "MM_eu_221x.bin", "Multi Mega EU (v2.21x)");
			var eu_mm_221x_b2 = File("73FC9C014AD803E9E7D8076B3642A8A5224B3E51", 131072, "MM_eu_221x.bin", "Multi Mega EU (v2.21x) [b]", isBad: true);
			var jp_wm_m2 = File("B3F32E409BD5508C89ED8BE33D41A58D791D0E5D", 131072, "WM_jp_m2.bin", "Wondermega M2 JP");
			var jp_wm_100W = File("3FC9358072F74BD24E3E297EA11B2BF15A7AF891", 131072, "WM_jp_100W.bin", "Wondermega JP (v1.00W)");
			var us_xeye = File("651F14D5A5E0ECB974A60C0F43B1D2006323FB09", 131072, "XEye_us.bin", "X'Eye US");

			var us_la_104 = File("AA811861F8874775075BD3F53008C8AAF59B07DB", 131072, "LA_us_104.bin", "LaserActive US (v1.04)");
			var jp_la_105 = File("B3B1D880E288B6DC79EEC0FF1B0480C229EC141D", 131072, "LA_us_105.bin", "LaserActive JP (v1.05)");
			var jp_la_102 = File("26237B333DB4A4C6770297FA5E655EA95840D5D9", 131072, "LA_us_102.bin", "LaserActive JP (v1.02)");
			var us_la_102 = File("8AF162223BB12FC19B414F126022910372790103", 131072, "LA_us_102.bin", "LaserActive US (v1.02)");

			var jp_mcd_111b = File("204758D5A64C24E96E1A9FE6BD82E1878FEF7ADE", 131072, "MCD_jp_111b.bin", "Mega CD JP (v1.11b)");
			var jp_mcd_reva = File("062E6A912E3683F7F127CBFD6314B44F93C42DB7", 131072, "MCD_jp_reva.bin", "Mega CD JP (Rev A)");
			var jp_mcd_beta = File("F30D109D1C2F7C9FEAF38600C65834261DB73D1F", 131072, "MCD_jp_beta.bin", "Mega CD JP (Beta)");
			var eu_mcd_221 = File("9DE4EDA59F544DB2D5FD7E6514601F7B648D8EB4", 131072, "MCD_eu_221.bin", "Mega CD EU (v2.21)");

			Option("GEN", "CD_BIOS_EU", in eu_mcd_100);
			Option("GEN", "CD_BIOS_EU", in eu_mcdii_200);
			Option("GEN", "CD_BIOS_EU", in eu_mcdii_200_b2);
			Option("GEN", "CD_BIOS_EU", in eu_mcdii_200_b);
			Option("GEN", "CD_BIOS_EU", in eu_mcdii_200w);
			Option("GEN", "CD_BIOS_EU", in eu_mcdii_200w_b2);
			Option("GEN", "CD_BIOS_EU", in eu_mcdii_200w_b);
			Option("GEN", "CD_BIOS_EU", in eu_mm_221x);
			Option("GEN", "CD_BIOS_EU", in eu_mm_221x_b2);
			Option("GEN", "CD_BIOS_EU", in eu_mcd_221);

			Option("GEN", "CD_BIOS_JP", in jp_mcda_211c);
			Option("GEN", "CD_BIOS_JP", in jp_mcd2_200c);
			Option("GEN", "CD_BIOS_JP", in jp_mcd2_200c_b2);
			Option("GEN", "CD_BIOS_JP", in jp_mcd2_200c_b);
			Option("GEN", "CD_BIOS_JP", in jp_mcd_100g);
			Option("GEN", "CD_BIOS_JP", in jp_mcd_100l);
			Option("GEN", "CD_BIOS_JP", in jp_mcd_100o);
			Option("GEN", "CD_BIOS_JP", in jp_mcd_100p);
			Option("GEN", "CD_BIOS_JP", in jp_mcd_100p_b);
			Option("GEN", "CD_BIOS_JP", in as_mcd_100s);
			Option("GEN", "CD_BIOS_JP", in jp_mcd_100s);
			Option("GEN", "CD_BIOS_JP", in jp_wm_m2);
			Option("GEN", "CD_BIOS_JP", in jp_wm_100W);
			Option("GEN", "CD_BIOS_JP", in jp_la_105);
			Option("GEN", "CD_BIOS_JP", in jp_la_102);
			Option("GEN", "CD_BIOS_JP", in jp_mcd_111b);
			Option("GEN", "CD_BIOS_JP", in jp_mcd_reva);
			Option("GEN", "CD_BIOS_JP", in jp_mcd_beta);

			Option("GEN", "CD_BIOS_US", in us_scd1_100);
			Option("GEN", "CD_BIOS_US", in us_scd1_100_h);
			Option("GEN", "CD_BIOS_US", in us_scd1_110);
			Option("GEN", "CD_BIOS_US", in us_scd2_200);
			Option("GEN", "CD_BIOS_US", in us_scd2_200_b);
			Option("GEN", "CD_BIOS_US", in us_scd2_200w);
			Option("GEN", "CD_BIOS_US", in us_scd2_200w_b);
			Option("GEN", "CD_BIOS_US", in us_scd2_211x);
			Option("GEN", "CD_BIOS_US", in us_scd2_211x_b);
			Option("GEN", "CD_BIOS_US", in us_gcdx_221x);
			Option("GEN", "CD_BIOS_US", in us_gcdx_221x_b);
			Option("GEN", "CD_BIOS_US", in us_xeye);
			Option("GEN", "CD_BIOS_US", in us_la_104);
			Option("GEN", "CD_BIOS_US", in us_la_102);

			// GG
			FirmwareAndOption("914AA165E3D879F060BE77870D345B60CFEB4EDE", 1024, "GG", "Majesco", "GG_majesco.gg", "GG BIOS (USA/Majesco)");

			// INTV
			FirmwareAndOption("5A65B922B562CB1F57DAB51B73151283F0E20C7A", 8192, "INTV", "EROM", "INTV_EROM.bin", "Executive Rom");
			FirmwareAndOption("F9608BB4AD1CFE3640D02844C7AD8E0BCD974917", 2048, "INTV", "GROM", "INTV_GROM.bin", "Graphics Rom");

			// Lynx
			FirmwareAndOption("E4ED47FAE31693E016B081C6BDA48DA5B70D7CCB", 512, "Lynx", "Boot", "Atari_LYNX_boot.img", "Boot Rom");

			// MSX
			FirmwareAndOption("2F997E8A57528518C82AB3693FDAE243DBBCC508", 32768, "MSX", "bios_test_ext", "MSX_cbios_main_msx1.rom", "MSX BIOS (C-BIOS v0.29a)");
			//FirmwareAndOption("E998F0C441F4F1800EF44E42CD1659150206CF79", 16384, "MSX", "bios_pal", "MSX_8020-20bios.rom", "MSX BIOS (Philips VG-8020)");
			//FirmwareAndOption("DF48902F5F12AF8867AE1A87F255145F0E5E0774", 16384, "MSX", "bios_jp", "MSX_4000bios.rom", "MSX BIOS (FS-4000)");
			FirmwareAndOption("409E82ADAC40F6BDD18EB6C84E8B2FBDC7FB5498", 32768, "MSX", "bios_basic_usa", "MSX.rom", "MSX BIOS and BASIC");
			FirmwareAndOption("3656BB3BBC17D280D2016FE4F6FF3CDED3082A41", 32768, "MSX", "bios_basic_usa", "MSX_1.0.rom", "MSX 1.0 BIOS and BASIC");
			FirmwareAndOption("302AFB5D8BE26C758309CA3DF611AE69CCED2821", 32768, "MSX", "bios_basic_jpn", "MSX_jpn.rom", "MSX 1.0 JPN BIOS and BASIC");

			// N64DD
			Firmware("N64DD", "IPL_JPN", "N64DD Japan IPL");
			var ddv10 = File("58670C0063793A8F3BE957D71D937B618829BA9E", 4 * 1024 * 1024, "64DD_IPL_v10_JPN.bin", "N64DD JPN IPL v1.0 (Beta)");
			var ddv11 = File("B3E26DBB4E945F78C918FABC5B9E60FCF262C47B", 4 * 1024 * 1024, "64DD_IPL_v11_JPN.bin", "N64DD JPN IPL v1.1 (Beta)");
			var ddv12 = File("BF861922DCB78C316360E3E742F4F70FF63C9BC3", 4 * 1024 * 1024, "64DD_IPL_v12_JPN.bin", "N64DD JPN IPL v1.2 (Retail)");
			Option("N64DD", "IPL_JPN", in ddv10, FirmwareOptionStatus.Unacceptable);
			Option("N64DD", "IPL_JPN", in ddv11, FirmwareOptionStatus.Unacceptable);
			Option("N64DD", "IPL_JPN", in ddv12, FirmwareOptionStatus.Ideal);
			FirmwareAndOption("10C4173C2A7EB09C6579818F72EF18FA0B6D32DE", 4 * 1024 * 1024, "N64DD", "IPL_DEV", "64DD_IPL_DEV.bin", "N64DD Development IPL");
			FirmwareAndOption("3C5B93CA231550C68693A14F03CEA8D5DBD1BE9E", 4 * 1024 * 1024, "N64DD", "IPL_USA", "64DD_IPL_USA.bin", "N64DD Prototype USA IPL");

			// NDS/i
			FirmwareAndOption("24F67BDEA115A2C847C8813A262502EE1607B7DF", 16384, "NDS", "bios7", "NDS_Bios7.bin", "ARM7 BIOS");
			FirmwareAndOption("BFAAC75F101C135E32E2AAF541DE6B1BE4C8C62D", 4096, "NDS", "bios9", "NDS_Bios9.bin", "ARM9 BIOS");
			// CHECKME: bios7i supposedly is different due to crypto keys and such, but yet it seems to work for various different NAND dumps?
			// supposedly two different systems dumped the same bios7i too? (according to nointro)
			FirmwareAndOption("C7C7570BFE51C3C7C5DA3B01331B94E7E7CB4F53", 65536, "NDS", "bios7i", "NDS_Bios7i.bin", "ARM7i BIOS");
			// full hash according to nointro, although incomplete dumps work just as well...
			FirmwareAndOption("719B9EEF33692406D7170FF526069615759C4DFC", 65536, "NDS", "bios9i", "NDS_Bios9i.bin", "ARM9i BIOS");
			Firmware("NDS", "firmware", "NDS Firmware");
			// throwing a ton of hashes from various reported firmware
			// TODO: Probably should just add in no-intro hashes
			var knownhack1 = File("22A7547DBC302BCBFB4005CFB5A2D426D3F85AC6", 262144, "NDS_Firmware [b1].bin", "NDS Firmware", "known hack", true);
			var knownhack2 = File("AE22DE59FBF3F35CCFBEACAEBA6FA87AC5E7B14B", 262144, "NDS_Firmware [b2].bin", "NDS Firmware", "known hack", true);
			var knownhack3 = File("1CF9E67C2C703BB9961BBCDD39CD2C7E319A803B", 262144, "NDS_Firmware [b3].bin", "NDS Firmware", "known hack", true);
			var likelygood1 = File("EDE9ADD041614EAA232059C63D8613B83FE4E954", 262144, "NDS_Firmware.bin", "NDS Firmware", "likely good");
			var likelygood2 = File("2EF20B45D12CF00657D4B1BD37A5CC8506923440", 262144, "NDS_Firmware.bin", "NDS Firmware", "likely good");
			var likelygood3 = File("87DAE2500E889737AF51F4A5B5845770A62482F5", 262144, "NDS_Lite_Firmware.bin", "NDS-Lite Firmware", "likely good");
			Option("NDS", "firmware", in knownhack1);
			Option("NDS", "firmware", in knownhack2);
			Option("NDS", "firmware", in knownhack3);
			Option("NDS", "firmware", in likelygood1);
			Option("NDS", "firmware", in likelygood2);
			Option("NDS", "firmware", in likelygood3);

			// really, this is pointless, firmwarei would just contain user settings for old DS mode? some wifi settings too? (maybe some crypto keys?)
			FirmwareAndOption(SHA1Checksum.Dummy, 131072, "NDS", "firmwarei", "DSi_Firmware.bin", "DSi Firmware");
			// options for each region due to region locking of the DSi
			// also, the sizes include the "nocash footer" which contains the eMMC CID and CPU/Console ID
			FirmwareAndOption(SHA1Checksum.Dummy, 251658240 + 64, "NDS", "NAND_JPN", "DSi_Nand_JPN.bin", "DSi NAND (Japan)");
			FirmwareAndOption(SHA1Checksum.Dummy, 251658240 + 64, "NDS", "NAND_EUR", "DSi_Nand_EUR.bin", "DSi NAND (Europe)");
			FirmwareAndOption(SHA1Checksum.Dummy, 251658240 + 64, "NDS", "NAND_USA", "DSi_Nand_USA.bin", "DSi NAND (USA)");
			FirmwareAndOption(SHA1Checksum.Dummy, 251658240 + 64, "NDS", "NAND_AUS", "DSi_Nand_AUS.bin", "DSi NAND (Australia)");
			FirmwareAndOption(SHA1Checksum.Dummy, 251658240 + 64, "NDS", "NAND_CHN", "DSi_Nand_CHN.bin", "DSi NAND (China)");
			FirmwareAndOption(SHA1Checksum.Dummy, 251658240 + 64, "NDS", "NAND_KOR", "DSi_Nand_KOR.bin", "DSi NAND (Korea)");

			// NES
			// FDS has two OK variants  (http://tcrf.net/Family_Computer_Disk_System)
			var fdsNintendo = File("57FE1BDEE955BB48D357E463CCBF129496930B62", 8192, "FDS_disksys-nintendo.rom", "Bios (Nintendo)");
			var fdsTwinFc = File("E4E41472C454F928E53EB10E0509BF7D1146ECC1", 8192, "FDS_disksys-nintendo.rom", "Bios (TwinFC)");
			Firmware("NES", "Bios_FDS", "Bios");
			Option("NES", "Bios_FDS", in fdsNintendo, FirmwareOptionStatus.Ideal);
			Option("NES", "Bios_FDS", in fdsTwinFc);

			// O2
			FirmwareAndOption("B2E1955D957A475DE2411770452EFF4EA19F4CEE", 1024, "O2", "BIOS-O2", "O2_Odyssey2.bin", "Odyssey 2 Bios");
			FirmwareAndOption("A6120AED50831C9C0D95DBDF707820F601D9452E", 1024, "O2", "BIOS-C52", "O2_PhillipsC52.bin", "Phillips C52 Bios");
			FirmwareAndOption("5130243429B40B01A14E1304D0394B8459A6FBAE", 1024, "O2", "BIOS-G7400", "O2_G7400_bios.bin", "G7400 Bios");

			// PCECD
			FirmwareAndOption("79F5FF55DD10187C7FD7B8DAAB0B3FFBD1F56A2C", 262144, "PCECD", "Bios", "PCECD_3.0-(J).pce", "Super CD Bios (J)");
			FirmwareAndOption("014881A959E045E00F4DB8F52955200865D40280", 32768, "PCECD", "GE-Bios", "PCECD_gecard.pce", "Games Express CD Card (Japan)");

			// PCFX
			Firmware("PCFX", "BIOS", "PCFX bios");
			var pcfxbios = File("1A77FD83E337F906AECAB27A1604DB064CF10074", 1024 * 1024, "PCFX_bios.bin", "PCFX BIOS 1.00");
			var pcfxv101 = File("8B662F7548078BE52A871565E19511CCCA28C5C8", 1024 * 1024, "PCFX_v101.bin", "PCFX BIOS 1.01");
			Option("PCFX", "BIOS", in pcfxbios, FirmwareOptionStatus.Ideal);
			Option("PCFX", "BIOS", in pcfxv101, FirmwareOptionStatus.Acceptable);
			Firmware("PCFX", "SCSIROM", "fx-scsi.rom");
			var fxscsi = File("65482A23AC5C10A6095AEE1DB5824CCA54EAD6E5", 512 * 1024, "PCFX_fx-scsi.rom", "PCFX SCSI ROM");
			Option("PCFX", "SCSIROM", in fxscsi);

			// PSX
			// http://forum.fobby.net/index.php?t=msg&goto=2763 [f]
			// http://www.psxdev.net/forum/viewtopic.php?f=69&t=56 [p]
			// https://en.wikipedia.org/wiki/PlayStation_models#Comparison_of_models [w]
			// https://github.com/petrockblog/RetroPie-Setup/wiki/PCSX-Core-Playstation-1 [g]
			// http://redump.org/datfile/psx-bios/ also
			// http://emulation.gametechwiki.com/index.php/File_Hashes [t]
			var ps_10j = File("343883A7B555646DA8CEE54AADD2795B6E7DD070", 524288, "PSX_1.0(J).bin", "PSX BIOS (Version 1.0 J)", "Used on SCPH-1000, DTL-H1000 [g]. This is Rev for A hardware [w].");
			var ps_11j = File("B06F4A861F74270BE819AA2A07DB8D0563A7CC4E", 524288, "PSX_1.1(J).bin", "PSX BIOS (Version 1.1 01/22/95)", "Used on SCPH-3000, DTL-H1000H [g]. This is for Rev B hardware [w].");
			var ps_20a = File("649895EFD79D14790EABB362E94EB0622093DFB9", 524288, "PSX_2.0(A).bin", "PSX BIOS (Version 2.0 05/07/95 A)", "Used on DTL-H1001 [g]. This is for Rev B hardware [w].");
			var ps_20e = File("20B98F3D80F11CBF5A7BFD0779B0E63760ECC62C", 524288, "PSX_2.0(E).bin", "PSX BIOS (Version 2.0 05/10/95 E)", "Used on DTL-H1002, SCPH-1002 [g]. This is for Rev B hardware [w].");
			var ps_21j = File("E38466A4BA8005FBA7E9E3C7B9EFEBA7205BEE3F", 524288, "PSX_2.1(J).bin", "PSX BIOS (Version 2.1 07/17/95 J)", "Used on SCPH-3500 [g]. This is for Rev B hardware [w].");
			var ps_21a = File("CA7AF30B50D9756CBD764640126C454CFF658479", 524288, "PSX_2.1(A).bin", "PSX BIOS (Version 2.1 07/17/95 A)", "Used on DTL-H1101 [g]. This is for Rev B hardware, presumably.");
			var ps_21e = File("76CF6B1B2A7C571A6AD07F2BAC0DB6CD8F71E2CC", 524288, "PSX_2.1(E).bin", "PSX BIOS (Version 2.1 07/17/95 E)", "Used on SCPH-1002, DTL-H1102 [g]. This is for Rev B hardware [w].");
			var ps_22j = File("FFA7F9A7FB19D773A0C3985A541C8E5623D2C30D", 524288, "PSX_2.2(J).bin", "PSX BIOS (Version 2.2 12/04/95 J)", "Used on SCPH-5000, DTL-H1200, DTL-H3000 [g]. This is for Rev C hardware [w].");
			var ps_22j_bad = File("E340DB2696274DDA5FDC25E434A914DB71E8B02B", 524288, "PSX_2.2(J)-bad.bin", "(bad dump) PSX BIOS (Version 2.2 12/04/95 J)", "BAD DUMP OF SCPH-5000. Found on [p].", isBad: true);
			var ps_22j_bad2 = File("81622ACE63E25696A5D884692E554D350DDF57A6", 526083, "PSX_2.2(J)-bad2.bin", "(bad dump) PSX BIOS (Version 2.2 12/04/95 J)", "BAD DUMP OF SCPH-5000.", isBad: true);
			var ps_22a = File("10155D8D6E6E832D6EA66DB9BC098321FB5E8EBF", 524288, "PSX_2.2(A).bin", "PSX BIOS (Version 2.2 12/04/95 A)", "Used on SCPH-1001, DTL-H1201, DTL-H3001 [g]. This is for Rev C hardware [w].");
			var ps_22e = File("B6A11579CAEF3875504FCF3831B8E3922746DF2C", 524288, "PSX_2.2(E).bin", "PSX BIOS (Version 2.2 12/04/95 E)", "Used on SCPH-1002, DTL-H1202, DTL-H3002 [g]. This is for Rev C hardware [w].");
			var ps_22d = File("73107D468FC7CB1D2C5B18B269715DD889ECEF06", 524288, "PSX_2.2(D).bin", "PSX BIOS (Version 2.2 03/06/96 D)", "Used on DTL-H1100 [g]. This is for Rev C hardware, presumably.");
			var ps_22jv = File("15C94DA3CC5A38A582429575AF4198C487FE893C", 1048576, "PSX_2.2(J).bin", "PSX BIOS (Version 2.2 12/04/95 J)", "Used on SCPH-5903 [t].");
			var ps_30j = File("B05DEF971D8EC59F346F2D9AC21FB742E3EB6917", 524288, "PSX_3.0(J).bin", "PSX BIOS (Version 3.0 09/09/96 J)", "Used on SCPH-5500 [g]. This is for Rev C hardware [w]. Recommended for (J) [f].");
			var ps_30a = File("0555C6FAE8906F3F09BAF5988F00E55F88E9F30B", 524288, "PSX_3.0(A).bin", "PSX BIOS (Version 3.0 11/18/96 A)", "Used on SCPH-5501, SCPH-5503, SCPH-7003 [g]. This is for Rev C hardware [w]. Recommended for (U) [f].");
			var ps_30e = File("F6BC2D1F5EB6593DE7D089C425AC681D6FFFD3F0", 524288, "PSX_3.0(E).bin", "PSX BIOS (Version 3.0 01/06/97 E)", "Used on SCPH-5502, SCPH-5552 [g]. This is for Rev C hardware [w]. Recommended for (E) [f].");
			var ps_30e_bad = File("F8DE9325FC36FCFA4B29124D291C9251094F2E54", 524288, "PSX_3.0(E)-bad.bin", "PSX BIOS (Version 3.0 01/06/97 E)", "BAD DUMP OF SCPH-5502. Found on [p].", isBad: true);
			var ps_40j = File("77B10118D21AC7FFA9B35F9C4FD814DA240EB3E9", 524288, "PSX_4.0(J).bin", "PSX BIOS (Version 4.0 08/18/97 J)", "Used on SCPH-7000, SCPH-7500, SCPH-9000 [g]. This is for Rev C hardware [w].");
			var ps_41a = File("14DF4F6C1E367CE097C11DEAE21566B4FE5647A9", 524288, "PSX_4.1(A).bin", "PSX BIOS (Version 4.1 12/16/97 A)", "Used on SCPH-7001, SCPH-7501, SCPH-7503, SCPH-9001, SCPH-9003, SCPH-9903 [g]. This is for Rev C hardware [w].");
			var ps_41e = File("8D5DE56A79954F29E9006929BA3FED9B6A418C1D", 524288, "PSX_4.1(E).bin", "PSX BIOS (Version 4.1 12/16/97 E)", "Used on SCPH-7002, SCPH-7502, SCPH-9002 [g]. This is for Rev C hardware [w].");
			var ps_41aw = File("1B0DBDB23DA9DC0776AAC58D0755DC80FEA20975", 524288, "PSX_4.1(A).bin", "PSX BIOS (Version 4.1 11/14/97 A)", "Used on SCPH-7000W [t].");
			var psone_43j = File("339A48F4FCF63E10B5B867B8C93CFD40945FAF6C", 524288, "PSX_4.3(J).bin", "PSX BIOS (Version 4.3 03/11/00 J)", "Used on PSone SCPH-100 [g]. This is for Rev C PSone hardware [w].");
			var psone_44e = File("BEB0AC693C0DC26DAF5665B3314DB81480FA5C7C", 524288, "PSX_4.4(E).bin", "PSX BIOS (Version 4.4 03/24/00 E)", "Used on PSone SCPH-102 [g]. This is for Rev C PSone hardware [w].");
			var psone_45a = File("DCFFE16BD90A723499AD46C641424981338D8378", 524288, "PSX_4.5(A).bin", "PSX BIOS (Version 4.5 05/25/00 A)", "Used on PSone SCPH-101 [g]. This is for Rev C PSone hardware [w].");
			var psone_r5e = File("DBC7339E5D85827C095764FC077B41F78FD2ECAE", 524288, "PSX_4.5(E).bin", "PSX BIOS (Version 4.5 05/25/00 E)", "Used on PSone SCPH-102 [g]. This is for Rev C PSone hardware [w].");
			var ps2_50j = File("D7D6BE084F51354BC951D8FA2D8D912AA70ABC5E", 4194304, "PSX_5.0(J).bin", "PSX BIOS (Version 5.0 10/27/00 J)", "Found on a PS2 [p]. May be known as SCPH18000.BIN.");
			var ps_dtl_h2000 = File("1A8D6F9453111B1D317BB7DAE300495FBF54600C", 524288, "PSX_DTLH2000.bin", "DTL-H2000 Devkit [t]");
			var psxOnPSP = File("96880D1CA92A016FF054BE5159BB06FE03CB4E14", 524288, "PSXONPSP660.BIN", "PSX on PSP", "From PSP. Allegedly based on PSone v4.5 BIOS.");
			var ps_ps3 = File("C40146361EB8CF670B19FDC9759190257803CAB7", 524288, "PSX_rom.bin", "PSX BIOS (Version 5.0 06/23/03 A)", "Found on a PS3. [t]");
			Firmware("PSX", "U", "BIOS (U)");
			Firmware("PSX", "J", "BIOS (J)");
			Firmware("PSX", "E", "BIOS (E)");
			Option("PSX", "U", in ps_30a, FirmwareOptionStatus.Ideal);
			Option("PSX", "J", in ps_30j, FirmwareOptionStatus.Ideal);
			Option("PSX", "E", in ps_30e, FirmwareOptionStatus.Ideal);
			// in general, alternates aren't allowed.. their quality isn't known.
			// we have this comment from fobby.net: "SCPH7502 works fine for European games" (TBD)
			// however, we're sticking with the 3.0 series.
			// please note: 2.1 or 2.2 would be a better choice, as the dates are the same and the bioses are more likely to matching in terms of entry points and such.
			// but 3.0 is what mednafen used
			Option("PSX", "J", in ps_10j, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "J", in ps_11j, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "U", in ps_20a, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "E", in ps_20e, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "J", in ps_21j, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "U", in ps_21a, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "E", in ps_21e, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "J", in ps_22j, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "J", in ps_22j_bad, FirmwareOptionStatus.Bad);
			Option("PSX", "J", in ps_22j_bad2, FirmwareOptionStatus.Bad);
			Option("PSX", "U", in ps_22a, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "E", in ps_22e, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "J", in ps_22d, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "E", in ps_30e_bad, FirmwareOptionStatus.Bad);
			Option("PSX", "J", in ps_40j, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "U", in ps_41a, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "E", in ps_41e, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "J", in psone_43j, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "E", in psone_44e, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "U", in psone_45a, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "E", in psone_r5e, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "J", in ps2_50j, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "J", in ps_22jv, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "U", in ps_41aw, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "U", in psxOnPSP, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "E", in psxOnPSP, FirmwareOptionStatus.Unacceptable); // doesn't work w/ PAL titles
			Option("PSX", "J", in psxOnPSP, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "U", in ps_ps3, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "U", in ps_dtl_h2000, FirmwareOptionStatus.Unacceptable); //not really sure what to do with this one, let's just call it region free
			Option("PSX", "E", in ps_dtl_h2000, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "J", in ps_dtl_h2000, FirmwareOptionStatus.Unacceptable);

			// Saturn
			var ss_100_j = File("2B8CB4F87580683EB4D760E4ED210813D667F0A2", 524288, "SAT_1.00-(J).bin", "Bios v1.00 (J)");
			var ss_100_ue = File("FAA8EA183A6D7BBE5D4E03BB1332519800D3FBC3", 524288, "SAT_1.00-(U+E).bin", "Bios v1.00 (U+E)");
			var ss_100a_ue = File("3BB41FEB82838AB9A35601AC666DE5AACFD17A58", 524288, "SAT_1.00a-(U+E).bin", "Bios v1.00a (U+E)");
			var ss_101_j = File("DF94C5B4D47EB3CC404D88B33A8FDA237EAF4720", 524288, "SAT_1.01-(J).bin", "Bios v1.01 (J)");
			// set mednafen's preferred bios files to ideal (we'll consider other known bios files to be acceptable)
			Firmware("SAT", "J", "Bios (J)");
			Option("SAT", "J", in ss_100_j);
			Option("SAT", "J", in ss_101_j, FirmwareOptionStatus.Ideal);
			Firmware("SAT", "U", "Bios (U)");
			Option("SAT", "U", in ss_100_ue, FirmwareOptionStatus.Ideal);
			Option("SAT", "U", in ss_100a_ue);
			Firmware("SAT", "E", "Bios (E)");
			Option("SAT", "E", in ss_100_ue, FirmwareOptionStatus.Ideal);
			Option("SAT", "E", in ss_100a_ue);
			FirmwareAndOption("A67CD4F550751F8B91DE2B8B74528AB4E0C11C77", 2 * 1024 * 1024, "SAT", "KOF95", "SAT_KoF95.bin", "King of Fighters cartridge");
			FirmwareAndOption("56C1B93DA6B660BF393FBF48CA47569000EF4047", 2 * 1024 * 1024, "SAT", "ULTRAMAN", "SAT_Ultraman.bin", "Ultraman cartridge");
			// Saturn ST-V (arcade variant, which mednafen supports)
			// hashes taken from MAME's db: https://github.com/mamedev/mame/blob/e9bdabea3702683e8b962acf8dbe61f2a5f46de0/src/mame/sega/stv.cpp#L1893-L1955
			var ss_stv_j = File("1A31B6B1A4257FCB6AC6A91E67DD798F91505F48", 524288, "SAT_STV_23603-(J).bin", "ST-V BIOS EPR-23603 (J)");
			var ss_stv_j1 = File("EFF0F54C70BCE05FF3A289BF30B1027E1C8CD117", 524288, "SAT_STV_20091-(J).bin", "ST-V BIOS EPR-20091 (J)");
			var ss_stv_j2 = File("FAE53107C894E0C41C49E191DBE706C9CD6E50BD", 524288, "SAT_STV_19730-(J).bin", "ST-V BIOS EPR-19730 (J)");
			var ss_stv_j3 = File("63CF4A6432F6C87952F9CF3AB0F977AED2367303", 524288, "SAT_STV_17951A-(J).bin", "ST-V BIOS EPR-17951A (J)");
			var ss_stv_j4 = File("F9B282FD27693E9891843597B2E1823DA3D23C7B", 524288, "SAT_STV_17740A-(J).bin", "ST-V BIOS EPR-17740A (J)");
			var ss_stv_j5 = File("06860D96923B81AFBC21E0AD32EE19487D8FF6E7", 524288, "SAT_STV_17740-(J).bin", "ST-V BIOS EPR-17740 (J)");
			var ss_stv_u = File("EAF1C3E5D602E1139D2090A78D7E19F04F916794", 524288, "SAT_STV_17952A-(U).bin", "ST-V BIOS EPR-17952A (U)");
			var ss_stv_u1 = File("CC41D30DE06160083D77E0DC5D69E61FEC7FDCB5", 524288, "SAT_STV_17741A-(U).bin", "ST-V BIOS EPR-17741A (U)");
			var ss_stv_e = File("AF79CFF317E5B57D49E463AF16A9F616ED1EEE08", 524288, "SAT_STV_17954A-(E).bin", "ST-V BIOS EPR-17954A (E)");
			var ss_stv_t = File("B55CDCB45B2A5B0B35E352CF7625F0BD659084DF", 524288, "SAT_STV_19854-(T).bin", "ST-V BIOS EPR-19854 (T)");
			var ss_stv_t1 = File("9EFC73717EC8A13417E65C54344DED9FC25BF5EF", 524288, "SAT_STV_17953A-(T).bin", "ST-V BIOS EPR-17953A (T)");
			var ss_stv_t2 = File("23185BEB1CE9C09B8719E57D1ADB7B28C8141FD5", 524288, "SAT_STV_17742A-(T).bin", "ST-V BIOS EPR-17742A (T)");
			Firmware("SAT", "STV_J", "ST-V Bios (J)");
			// mame appears to order bios files from newest to oldest, so reverse the order of presentation here
			// similarly to regular saturn bios files, set mednafen's preferred bios files to ideal
			Option("SAT", "STV_J", in ss_stv_j5);
			Option("SAT", "STV_J", in ss_stv_j4);
			Option("SAT", "STV_J", in ss_stv_j3);
			Option("SAT", "STV_J", in ss_stv_j2);
			Option("SAT", "STV_J", in ss_stv_j1, FirmwareOptionStatus.Ideal);
			Option("SAT", "STV_J", in ss_stv_j);
			// mednafen appears to consider Taiwan as Japan for purposes of bios files, so we'll do that too
			Option("SAT", "STV_J", in ss_stv_t2);
			Option("SAT", "STV_J", in ss_stv_t1);
			Option("SAT", "STV_J", in ss_stv_t);
			Firmware("SAT", "STV_U", "ST-V Bios (U)");
			Option("SAT", "STV_U", in ss_stv_u1);
			Option("SAT", "STV_U", in ss_stv_u, FirmwareOptionStatus.Ideal);
			Firmware("SAT", "STV_E", "ST-V Bios (E)");
			Option("SAT", "STV_E", in ss_stv_e, FirmwareOptionStatus.Ideal);

			// SMS
			var sms_us_13 = File("C315672807D8DDB8D91443729405C766DD95CAE7", 8192, "SMS_us_1.3.sms", "SMS BIOS 1.3 (USA, Europe)");
			var sms_jp_21 = File("A8C1B39A2E41137835EDA6A5DE6D46DD9FADBAF2", 8192, "SMS_jp_2.1.sms", "SMS BIOS 2.1 (Japan)");
			var sms_us_1b = File("29091FF60EF4C22B1EE17AA21E0E75BAC6B36474", 8192, "SMS_us_1.0b.sms", "SMS BIOS 1.0 (USA) (Proto)"); // ?? is this size correct?
			var sms_m404 = File("4A06C8E66261611DCE0305217C42138B71331701", 8192, "SMS_m404.sms", "SMS BIOS (USA) (M404) (Proto)"); // ?? is this size correct?
			var sms_kr = File("2FEAFD8F1C40FDF1BD5668F8C5C02E5560945B17", 131072, "SMS_kr.sms", "SMS BIOS (Kr)"); // ?? is this size correct?
			Firmware("SMS", "Export", "SMS Bios (USA/Export)");
			Firmware("SMS", "Japan", "SMS Bios (Japan)");
			Firmware("SMS", "Korea", "SMS Bios (Korea)");
			Option("SMS", "Export", in sms_us_13);
			Option("SMS", "Export", in sms_us_1b);
			Option("SMS", "Export", in sms_m404);
			Option("SMS", "Japan", in sms_jp_21);
			Option("SMS", "Korea", in sms_kr);

			// SNES
			var sgb = File("6ED55C4368333B57F6A2F8BBD70CCD87ED48058E", 262144, "SNES_SGB_(JU).sfc", "Super Game Boy Rom (JU)");
			var sgbA = File("6380A5913ACE3041A305FBAF822B5A8847FEA7ED", 262144, "SNES_SGB_RevA_(JU).sfc", "Super Game Boy Rom (JU, Rev A)");
			var sgbA_Beta = File("4ED5621A9022E1D94B673CC0F68EA24764E8D6BB", 262144, "SNES_SGB_RevABeta_(JU).sfc", "Super Game Boy Rom (JU, Rev A Beta)");
			var sgbB = File("973E10840DB683CF3FAF61BD443090786B3A9F04", 262144, "SNES_SGB_RevB_(World).sfc", "Super Game Boy Rom (World, Rev B)");
			var sgb2 = File("E5B2922CA137051059E4269B236D07A22C07BC84", 524288, "SNES_SGB2_(J).sfc", "Super Game Boy 2 Rom (J)");
			Firmware("SNES", "Rom_SGB", "Super Game Boy Rom");
			Firmware("SNES", "Rom_SGB2", "Super Game Boy 2 Rom");
			Option("SNES", "Rom_SGB", in sgb, FirmwareOptionStatus.Ideal);
			Option("SNES", "Rom_SGB", in sgbA, FirmwareOptionStatus.Ideal);
			Option("SNES", "Rom_SGB", in sgbA_Beta);
			Option("SNES", "Rom_SGB", in sgbB, FirmwareOptionStatus.Ideal);
			Option("SNES", "Rom_SGB2", in sgb2, FirmwareOptionStatus.Ideal);
			FirmwareAndOption("A002F4EFBA42775A31185D443F3ED1790B0E949A", 3072, "SNES", "CX4", "SNES_cx4.rom", "CX4 Rom");
			FirmwareAndOption("188D471FEFEA71EB53F0EE7064697FF0971B1014", 8192, "SNES", "DSP1", "SNES_dsp1.rom", "DSP1 Rom");
			FirmwareAndOption("78B724811F5F18D8C67669D9390397EB1A47A5E2", 8192, "SNES", "DSP1b", "SNES_dsp1b.rom", "DSP1b Rom");
			FirmwareAndOption("198C4B1C3BFC6D69E734C5957AF3DBFA26238DFB", 8192, "SNES", "DSP2", "SNES_dsp2.rom", "DSP2 Rom");
			FirmwareAndOption("558DA7CB3BD3876A6CA693661FFC6C110E948CF9", 8192, "SNES", "DSP3", "SNES_dsp3.rom", "DSP3 Rom");
			FirmwareAndOption("AF6478AECB6F1B67177E79C82CA04C56250A8C72", 8192, "SNES", "DSP4", "SNES_dsp4.rom", "DSP4 Rom");
			FirmwareAndOption("6472828403DE3589433A906E2C3F3D274C0FF008", 53248, "SNES", "ST010", "SNES_st010.rom", "ST010 Rom");
			FirmwareAndOption("FECBAE2CEC76C710422486BAA186FFA7CA1CF925", 53248, "SNES", "ST011", "SNES_st011.rom", "ST011 Rom");
			FirmwareAndOption("91383B92745CC7CC4F15409AC5BC2C2F699A43F1", 163840, "SNES", "ST018", "SNES_st018.rom", "ST018 Rom");

			// see SATELLAVIEW_CARTRIDGE enum in BsnesCore
			FirmwareAndOption("604556B2E62860AF18DB5A77F2956EBC75450020", 1048576, "BSX", "Rom_BSX", "Satellaview_BS-X.sfc", "BS-X Rom");
			FirmwareAndOption("8417556B5CE22FB9BC4026FE28F7AEE348A76FD9", 1048576, "BSX", "Rom_Mahjong", "Satellaview_Mahjong.sfc", "Joushou Mahjong Tenpai Rom");
			FirmwareAndOption("356A3D60E9FABFF4B4CEC66277C9A6B717C958CE", 1572864, "BSX", "Rom_GNEXT", "Satellaview_GNEXT.sfc", "SD Gundam G-Next Rom");
			FirmwareAndOption("95F31DFB31200E31E80007CC66A32E2C1A7B02BE", 2097152, "BSX", "Rom_RPG_Tsukuru", "Satellaview_RPG_Tsukuru.sfc", "RPG Tsukūru 2 Rom");
			FirmwareAndOption("A6AD0F89C3E75327D70AA0F0239C7B06F151E0A4", 1048576, "BSX", "Rom_SameGame", "Satellaview_SameGame.sfc", "SameGame Rom");
			FirmwareAndOption("24AF9A5A78635EC493BBBC2540AB98BD161C5798", 3145728, "BSX", "Rom_DS96", "Satellaview_Derby_Stallion_96.sfc", "Derby Stallion 96 Rom");
			FirmwareAndOption("337E0C4BDBF544787B543A1FB83AFBA7526191B9", 1048576, "BSX", "Rom_Ongaku_Tsukuru", "Satellaview_Ongaku_Tsukuru.sfc", "Ongaku Tsukūru: Kanadēru Rom");
			FirmwareAndOption("A9B124948A19BB5A295EC208FCA8180476BDFEBB", 3145728, "BSX", "Rom_SoundNovel_Tsukuru", "Satellaview_SoundNovel_Tsukuru.sfc", "Sound-Novel Tsukūru Rom");
			FirmwareAndOption("357AC4826297A6496035E3951CACDA55DCAE4B1B", 4194304, "BSX", "Rom_Tsuri", "Satellaview_Bass_Tsuri.sfc", "Itoi Shigesato no Bass Tsuri No. 1 Rom");

			// TI83
			var ti83_102 = File("CE08F6A808701FC6672230A790167EE485157561", 262144, "TI83_102.rom", "TI-83 Rom v1.02"); // ?? is this size correct?
			var ti83_103 = File("8399E384804D8D29866CAA4C8763D7A61946A467", 262144, "TI83_103.rom", "TI-83 Rom v1.03"); // ?? is this size correct?
			var ti83_104 = File("33877FF637DC5F4C5388799FD7E2159B48E72893", 262144, "TI83_104.rom", "TI-83 Rom v1.04"); // ?? is this size correct?
			var ti83_106 = File("3D65C2A1B771CE8E5E5A0476EC1AA9C9CDC0E833", 262144, "TI83_106.rom", "TI-83 Rom v1.06"); // ?? is this size correct?
			var ti83_107 = File("EF66DAD3E7B2B6A86F326765E7DFD7D1A308AD8F", 262144, "TI83_107.rom", "TI-83 Rom v1.07"); // formerly the 1.?? recommended one
			var ti83_108 = File("9C74F0B61655E9E160E92164DB472AD7EE02B0F8", 262144, "TI83_108.rom", "TI-83 Rom v1.08"); // ?? is this size correct?
			var ti83p_103 = File("37EAEEB9FB5C18FB494E322B75070E80CC4D858E", 262144, "TI83p_103b.rom", "TI-83 Plus Rom v1.03"); // ?? is this size correct?
			var ti83p_112 = File("6615DF5554076B6B81BD128BF847D2FF046E556B", 262144, "TI83p_112.rom", "TI-83 Plus Rom v1.12"); // ?? is this size correct?
			Firmware("TI83", "Rom", "TI-83 Rom");
			Option("TI83", "Rom", in ti83_102);
			Option("TI83", "Rom", in ti83_103);
			Option("TI83", "Rom", in ti83_104);
			Option("TI83", "Rom", in ti83_106);
			Option("TI83", "Rom", in ti83_107);
			Option("TI83", "Rom", in ti83_108);
			Option("TI83", "Rom", in ti83p_103);
			Option("TI83", "Rom", in ti83p_112);

			// VEC
			FirmwareAndOption("B9BBF5BB0EAC52D039A4A993A2D8064B862C9E28", 4096, "VEC", "Bios", "VEC_Bios.bin", "Bios");
			FirmwareAndOption("65D07426B520DDD3115D40F255511E0FD2E20AE7", 8192, "VEC", "Minestorm", "VEC_Minestorm.vec", "Game");

			// ZX Spectrum
			FirmwareAndOption("A584272F21DC82C14B7D4F1ED440E23A976E71F0", 32768, "ZXSpectrum", "PentagonROM", "ZX_pentagon.rom", "Russian Pentagon Clone ROM");
			FirmwareAndOption("282EB7BC819AAD2A12FD954E76F7838A4E1A7929", 16384, "ZXSpectrum", "TRDOSROM", "ZX_trdos.rom", "TRDOS ROM");

			/*
			Firmware("PS2", "BIOS", "PS2 Bios");
			Option("PS2", "BIOS", File("FBD54BFC020AF34008B317DCB80B812DD29B3759", 4 * 1024 * 1024, "ps2-0230j-20080220.bin", "PS2 Bios"));
			Option("PS2", "BIOS", File("8361D615CC895962E0F0838489337574DBDC9173", 4 * 1024 * 1024, "ps2-0220a-20060905.bin", "PS2 Bios"));
			Option("PS2", "BIOS", File("DA5AACEAD2FB55807D6D4E70B1F10F4FDCFD3281", 4 * 1024 * 1024, "ps2-0220e-20060905.bin", "PS2 Bios"));
			Option("PS2", "BIOS", File("3BAF847C1C217AA71AC6D298389C88EDB3DB32E2", 4 * 1024 * 1024, "ps2-0220j-20060905.bin", "PS2 Bios"));
			Option("PS2", "BIOS", File("F9229FE159D0353B9F0632F3FDC66819C9030458", 4 * 1024 * 1024, "ps2-0230a-20080220.bin", "PS2 Bios"), FirmwareOptionStatus.Ideal);
			Option("PS2", "BIOS", File("9915B5BA56798F4027AC1BD8D10ABE0C1C9C326A", 4 * 1024 * 1024, "ps2-0230e-20080220.bin", "PS2 Bios"));
			*/

			AllPatches = allPatches;
			FirmwareFilesByHash = filesByHash;
			FirmwareFilesByOption = filesByOption;
			FirmwareOptions = options;
			FirmwareRecords = records;
		}
	} // static class FirmwareDatabase
}
