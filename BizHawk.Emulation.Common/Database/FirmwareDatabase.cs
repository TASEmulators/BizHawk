using System;
using System.Linq;
using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	public static class FirmwareDatabase
	{
		static FirmwareDatabase()
		{
			//FDS has two OK variants  (http://tcrf.net/Family_Computer_Disk_System)
			var fds_nintendo = File("57FE1BDEE955BB48D357E463CCBF129496930B62", 8192, "disksys-nintendo.rom", "Bios (Nintendo)");
			var fds_twinfc = File("E4E41472C454F928E53EB10E0509BF7D1146ECC1", 8192, "disksys-nintendo.rom", "Bios (TwinFC)");
			Firmware("NES", "Bios_FDS", "Bios");
			Option("NES", "Bios_FDS", fds_nintendo);
			Option("NES", "Bios_FDS", fds_twinfc);

			FirmwareAndOption("973E10840DB683CF3FAF61BD443090786B3A9F04", 262144, "SNES", "Rom_SGB", "sgb.sfc", "Super GameBoy Rom"); //World (Rev B) ?
			FirmwareAndOption("A002F4EFBA42775A31185D443F3ED1790B0E949A", 3072, "SNES", "CX4", "cx4.rom", "CX4 Rom");
			FirmwareAndOption("188D471FEFEA71EB53F0EE7064697FF0971B1014", 8192, "SNES", "DSP1", "dsp1.rom", "DSP1 Rom");
			FirmwareAndOption("78B724811F5F18D8C67669D9390397EB1A47A5E2", 8192, "SNES", "DSP1b", "dsp1b.rom", "DSP1b Rom");
			FirmwareAndOption("198C4B1C3BFC6D69E734C5957AF3DBFA26238DFB", 8192, "SNES", "DSP2", "dsp2.rom", "DSP2 Rom");
			FirmwareAndOption("558DA7CB3BD3876A6CA693661FFC6C110E948CF9", 8192, "SNES", "DSP3", "dsp3.rom", "DSP3 Rom");
			FirmwareAndOption("AF6478AECB6F1B67177E79C82CA04C56250A8C72", 8192, "SNES", "DSP4", "dsp4.rom", "DSP4 Rom");
			FirmwareAndOption("6472828403DE3589433A906E2C3F3D274C0FF008", 53248, "SNES", "ST010", "st010.rom", "ST010 Rom");
			FirmwareAndOption("FECBAE2CEC76C710422486BAA186FFA7CA1CF925", 53248, "SNES", "ST011", "st011.rom", "ST011 Rom");
			FirmwareAndOption("91383B92745CC7CC4F15409AC5BC2C2F699A43F1", 163840, "SNES", "ST018", "st018.rom", "ST018 Rom");
			FirmwareAndOption("79F5FF55DD10187C7FD7B8DAAB0B3FFBD1F56A2C", 262144, "PCECD", "Bios", "pcecd-3.0-(J).pce", "Super CD Bios (J)");
			FirmwareAndOption("D9D134BB6B36907C615A594CC7688F7BFCEF5B43", 4096, "A78", "Bios_NTSC", "7800NTSCBIOS.bin", "NTSC Bios");
			FirmwareAndOption("5A140136A16D1D83E4FF32A19409CA376A8DF874", 16384, "A78", "Bios_PAL", "7800PALBIOS.bin", "PAL Bios");
			FirmwareAndOption("A3AF676991391A6DD716C79022D4947206B78164", 4096, "A78", "Bios_HSC", "7800highscore.bin", "Highscore Bios");
			FirmwareAndOption("45BEDC4CBDEAC66C7DF59E9E599195C778D86A92", 8192, "Coleco", "Bios", "ColecoBios.bin", "Bios");

			{
				var GBA_JDebug = File("AA98A2AD32B86106340665D1222D7D973A1361C7", 16384, "gbabios.rom", "Bios (J Debug)");
				var GBA_Normal = File("300C20DF6731A33952DED8C436F7F186D25D3492", 16384, "gbabios.rom", "Bios (World)");
				Firmware("GBA", "Bios", "Bios");
				Option("GBA", "Bios", GBA_Normal);
				Option("GBA", "Bios", GBA_JDebug);
			}

			FirmwareAndOption("E4ED47FAE31693E016B081C6BDA48DA5B70D7CCB", 512, "Lynx", "Boot", "lynxboot.img", "Boot Rom");


			//FirmwareAndOption("24F67BDEA115A2C847C8813A262502EE1607B7DF", "NDS", "Bios_Arm7", "biosnds7.rom", "ARM7 Bios");
			//FirmwareAndOption("BFAAC75F101C135E32E2AAF541DE6B1BE4C8C62D", "NDS", "Bios_Arm9", "biosnds9.rom", "ARM9 Bios");
			FirmwareAndOption("5A65B922B562CB1F57DAB51B73151283F0E20C7A", 8192, "INTV", "EROM", "erom.bin", "Executive Rom");
			FirmwareAndOption("F9608BB4AD1CFE3640D02844C7AD8E0BCD974917", 2048, "INTV", "GROM", "grom.bin", "Graphics Rom");

			FirmwareAndOption("1D503E56DF85A62FEE696E7618DC5B4E781DF1BB", 8192, "C64", "Kernal", "c64-kernal.bin", "Kernal Rom");
			FirmwareAndOption("79015323128650C742A3694C9429AA91F355905E", 8192, "C64", "Basic", "c64-basic.bin", "Basic Rom");
			FirmwareAndOption("ADC7C31E18C7C7413D54802EF2F4193DA14711AA", 4096, "C64", "Chargen", "c64-chargen.bin", "Chargen Rom");
            FirmwareAndOption("AB16F56989B27D89BABE5F89C5A8CB3DA71A82F0", 16384, "C64", "Drive1541", "drive-1541.bin", "1541 Disk Drive Rom");
            FirmwareAndOption("D3B78C3DBAC55F5199F33F3FE0036439811F7FB3", 16384, "C64", "Drive1541II", "drive-1541ii.bin", "1541-II Disk Drive Rom");

            //for saturn, we think any bios region can pretty much run any iso
            //so, we're going to lay this out carefully so that we choose things in a sensible order, but prefer the correct region
            var ss_100_j = File("2B8CB4F87580683EB4D760E4ED210813D667F0A2", 524288, "saturn-1.00-(J).bin", "Bios v1.00 (J)");
			var ss_100_ue = File("FAA8EA183A6D7BBE5D4E03BB1332519800D3FBC3", 524288, "saturn-1.00-(U+E).bin", "Bios v1.00 (U+E)");
			var ss_100a_ue = File("3BB41FEB82838AB9A35601AC666DE5AACFD17A58", 524288, "saturn-1.00a-(U+E).bin", "Bios v1.00a (U+E)"); //?? is this size correct?
			var ss_101_j = File("DF94C5B4D47EB3CC404D88B33A8FDA237EAF4720", 524288, "saturn-1.01-(J).bin", "Bios v1.01 (J)"); //?? is this size correct?
			Firmware("SAT", "J", "Bios (J)");
			Option("SAT", "J", ss_100_j);
			Option("SAT", "J", ss_101_j);
			Option("SAT", "J", ss_100_ue);
			Option("SAT", "J", ss_100a_ue);
			Firmware("SAT", "U", "Bios (U)");
			Option("SAT", "U", ss_100_ue);
			Option("SAT", "U", ss_100a_ue);
			Option("SAT", "U", ss_100_j);
			Option("SAT", "U", ss_101_j);
			Firmware("SAT", "E", "Bios (E)");
			Option("SAT", "E", ss_100_ue);
			Option("SAT", "E", ss_100a_ue);
			Option("SAT", "E", ss_100_j);
			Option("SAT", "E", ss_101_j);

			var ti83_102 = File("CE08F6A808701FC6672230A790167EE485157561", 262144, "ti83_102.rom", "TI-83 Rom v1.02"); //?? is this size correct?
			var ti83_103 = File("8399E384804D8D29866CAA4C8763D7A61946A467", 262144, "ti83_103.rom", "TI-83 Rom v1.03"); //?? is this size correct?
			var ti83_104 = File("33877FF637DC5F4C5388799FD7E2159B48E72893", 262144, "ti83_104.rom", "TI-83 Rom v1.04"); //?? is this size correct?
			var ti83_106 = File("3D65C2A1B771CE8E5E5A0476EC1AA9C9CDC0E833", 262144, "ti83_106.rom", "TI-83 Rom v1.06"); //?? is this size correct?
			var ti83_107 = File("EF66DAD3E7B2B6A86F326765E7DFD7D1A308AD8F", 262144, "ti83_107.rom", "TI-83 Rom v1.07"); //formerly the 1.?? recommended one
			var ti83_108 = File("9C74F0B61655E9E160E92164DB472AD7EE02B0F8", 262144, "ti83_108.rom", "TI-83 Rom v1.08"); //?? is this size correct?
			var ti83p_103 = File("37EAEEB9FB5C18FB494E322B75070E80CC4D858E", 262144, "ti83p_103b.rom", "TI-83 Plus Rom v1.03"); //?? is this size correct?
			var ti83p_112 = File("6615DF5554076B6B81BD128BF847D2FF046E556B", 262144, "ti83p_112.rom", "TI-83 Plus Rom v1.12"); //?? is this size correct?

			Firmware("TI83", "Rom", "TI-83 Rom");
			Option("TI83", "Rom", ti83_102);
			Option("TI83", "Rom", ti83_103);
			Option("TI83", "Rom", ti83_104);
			Option("TI83", "Rom", ti83_106);
			Option("TI83", "Rom", ti83_107);
			Option("TI83", "Rom", ti83_108);
			Option("TI83", "Rom", ti83p_103);
			Option("TI83", "Rom", ti83p_112);

			// mega cd
			var eu_mcd1_9210 = File("f891e0ea651e2232af0c5c4cb46a0cae2ee8f356", 131072, "eu_mcd1_9210.bin", "Mega CD EU (9210)");
			var eu_mcd2_9303 = File("7063192ae9f6b696c5b81bc8f0a9fe6f0c400e58", 131072, "eu_mcd2_9303.bin", "Mega CD EU (9303)");
			var eu_mcd2_9306 = File("523b3125fb0ac094e16aa072bc6ccdca22e520e5", 131072, "eu_mcd2_9306.bin", "Mega CD EU (9310)"); //?? is this size correct?
			var jp_mcd1_9111 = File("4846f448160059a7da0215a5df12ca160f26dd69", 131072, "jp_mcd1_9111.bin", "Mega CD JP (9111)");
			var jp_mcd1_9112 = File("e4193c6ae44c3cea002707d2a88f1fbcced664de", 131072, "jp_mcd1_9112.bin", "Mega CD JP (9112)");
			var us_scd1_9210 = File("f4f315adcef9b8feb0364c21ab7f0eaf5457f3ed", 131072, "us_scd1_9210.bin", "Sega CD US (9210)");
			var us_scd2_9303 = File("bd3ee0c8ab732468748bf98953603ce772612704", 131072, "us_scd2_9303.bin", "Sega CD US (9303)");

			Firmware("GEN", "CD_BIOS_EU", "Mega CD Bios (Europe)");
			Firmware("GEN", "CD_BIOS_JP", "Mega CD Bios (Japan)");
			Firmware("GEN", "CD_BIOS_US", "Sega CD Bios (USA)");
			Option("GEN", "CD_BIOS_EU", eu_mcd1_9210);
			Option("GEN", "CD_BIOS_EU", eu_mcd2_9303);
			Option("GEN", "CD_BIOS_EU", eu_mcd2_9306);
			Option("GEN", "CD_BIOS_JP", jp_mcd1_9111);
			Option("GEN", "CD_BIOS_JP", jp_mcd1_9112);
			Option("GEN", "CD_BIOS_US", us_scd1_9210);
			Option("GEN", "CD_BIOS_US", us_scd2_9303);

			// SMS
			var sms_us_13 = File("C315672807D8DDB8D91443729405C766DD95CAE7", 8192, "sms_us_1.3.sms", "SMS BIOS 1.3 (USA, Europe)");
			var sms_jp_21 = File("A8C1B39A2E41137835EDA6A5DE6D46DD9FADBAF2", 8192, "sms_jp_2.1.sms", "SMS BIOS 2.1 (Japan)");
			var sms_us_1b = File("29091FF60EF4C22B1EE17AA21E0E75BAC6B36474", 8192, "sms_us_1.0b.sms", "SMS BIOS 1.0 (USA) (Proto)"); //?? is this size correct?
			var sms_m404 = File("4A06C8E66261611DCE0305217C42138B71331701", 8192, "sms_m404.sms", "SMS BIOS (USA) (M404) (Proto)"); //?? is this size correct?

			Firmware("SMS", "Export", "SMS Bios (USA/Export)");
			Firmware("SMS", "Japan", "SMS Bios (Japan)");
			Option("SMS", "Export", sms_us_13);
			Option("SMS", "Export", sms_us_1b);
			Option("SMS", "Export", sms_m404);
			Option("SMS", "Japan", sms_jp_21);

			//PSX
			//http://forum.fobby.net/index.php?t=msg&goto=2763 [f]
			//http://www.psxdev.net/forum/viewtopic.php?f=69&t=56 [p]
			//https://en.wikipedia.org/wiki/PlayStation_models#Comparison_of_models [w]
			//https://github.com/petrockblog/RetroPie-Setup/wiki/PCSX-Core-Playstation-1 [g] 
			//http://redump.org/datfile/psx-bios/ also
			var ps_10j = File("343883A7B555646DA8CEE54AADD2795B6E7DD070", 524288, "ps-10j.bin", "PSX BIOS (Version 1.0 J)", "Used on SCPH-1000, DTL-H1000 [g]. This is Rev for A hardware [w].");
			var ps_11j = File("B06F4A861F74270BE819AA2A07DB8D0563A7CC4E", 524288, "ps-11j.bin", "PSX BIOS (Version 1.1 01/22/95)", "Used on SCPH-3000, DTL-H1000H [g]. This is for Rev B hardware [w].");
			var ps_20a = File("649895EFD79D14790EABB362E94EB0622093DFB9", 524288, "ps-20a.bin", "PSX BIOS (Version 2.0 05/07/95 A)", "Used on DTL-H1001 [g]. This is for Rev B hardware [w].");
			var ps_20e = File("20B98F3D80F11CBF5A7BFD0779B0E63760ECC62C", 524288, "ps-20e.bin", "PSX BIOS (Version 2.0 05/10/95 E)", "Used on DTL-H1002, SCPH-1002 [g]. This is for Rev B hardware [w].");
			var ps_21j = File("E38466A4BA8005FBA7E9E3C7B9EFEBA7205BEE3F", 524288, "ps-21j.bin", "PSX BIOS (Version 2.1 07/17/95 J)", "Used on SCPH-3500 [g]. This is for Rev B hardware [w].");
			var ps_21a = File("CA7AF30B50D9756CBD764640126C454CFF658479", 524288, "ps-21a.bin", "PSX BIOS (Version 2.1 07/17/95 A)", "Used on DTL-H1101 [g]. This is for Rev B hardware, presumably.");
			var ps_21e = File("76CF6B1B2A7C571A6AD07F2BAC0DB6CD8F71E2CC", 524288, "ps-21e.bin", "PSX BIOS (Version 2.1 07/17/95 E)", "Used on SCPH-1002, DTL-H1102 [g]. This is for Rev B hardware [w].");
			var ps_22j = File("FFA7F9A7FB19D773A0C3985A541C8E5623D2C30D", 524288, "ps-22j.bin", "PSX BIOS (Version 2.2 12/04/95 J)", "Used on SCPH-5000, DTL-H1200, DTL-H3000 [g]. This is for Rev C hardware [w].");
			var ps_22j_bad = File("E340DB2696274DDA5FDC25E434A914DB71E8B02B", 524288, "ps-22j-bad.bin", "BAD DUMP OF SCPH-5000. Found on [p]."); //BAD!!
			var ps_22j_bad2 = File("81622ACE63E25696A5D884692E554D350DDF57A6", 526083, "ps-22j-bad2.bin", "PSX BIOS (Version 2.2 12/04/95 J", "BAD DUMP OF SCPH-5000."); //BAD!
			var ps_22a = File("10155D8D6E6E832D6EA66DB9BC098321FB5E8EBF", 524288, "ps-22a.bin", "PSX BIOS (Version 2.2 12/04/95 A)", "Used on SCPH-1001, DTL-H1201, DTL-H3001 [g]. This is for Rev C hardware [w].");
			var ps_22e = File("B6A11579CAEF3875504FCF3831B8E3922746DF2C", 524288, "ps-22e.bin", "PSX BIOS (Version 2.2 12/04/95 E)", "Used on SCPH-1002, DTL-H1202, DTL-H3002 [g]. This is for Rev C hardware [w].");
			var ps_22d = File("73107D468FC7CB1D2C5B18B269715DD889ECEF06", 524288, "ps-22d.bin", "PSX BIOS (Version 2.2 03/06/96 D)", "Used on DTL-H1100 [g]. This is for Rev C hardware, presumably.");
			var ps_30j = File("B05DEF971D8EC59F346F2D9AC21FB742E3EB6917", 524288, "ps-30j.bin", "PSX BIOS (Version 3.0 09/09/96 J)", "Used on SCPH-5500 [g]. This is for Rev C hardware [w]. Recommended for (J) [f].");
			var ps_30a = File("0555C6FAE8906F3F09BAF5988F00E55F88E9F30B", 524288, "ps-30a.bin", "PSX BIOS (Version 3.0 11/18/96 A)", "Used on SCPH-5501, SCPH-5503, SCPH-7003 [g]. This is for Rev C hardware [w]. Recommended for (U) [f].");
			var ps_30e = File("F6BC2D1F5EB6593DE7D089C425AC681D6FFFD3F0", 524288, "ps-30e.bin", "PSX BIOS (Version 3.0 01/06/97 E)", "Used on SCPH-5502, SCPH-5552 [g]. This is for Rev C hardware [w]. Recommended for (E) [f].");
			var ps_30e_bad = File("F8DE9325FC36FCFA4B29124D291C9251094F2E54", 524288, "ps-30e-bad.bin", "BAD DUMP OF SCPH-5502. Found on [p]."); //BAD!
			var ps_40j = File("77B10118D21AC7FFA9B35F9C4FD814DA240EB3E9", 524288, "ps-40j.bin", "PSX BIOS (Version 4.0 08/18/97 J)", "Used on SCPH-7000, SCPH-7500, SCPH-9000 [g]. This is for Rev C hardware [w].");
			var ps_41a = File("14DF4F6C1E367CE097C11DEAE21566B4FE5647A9", 524288, "ps-41a.bin", "PSX BIOS (Version 4.1 12/16/97 A)", "Used on SCPH-7001, SCPH-7501, SCPH-7503, SCPH-9001, SCPH-9003, SCPH-9903 [g]. This is for Rev C hardware [w].");
			var ps_41e = File("8D5DE56A79954F29E9006929BA3FED9B6A418C1D", 524288, "ps-41e.bin", "PSX BIOS (Version 4.1 12/16/97 E)", "Used on SCPH-7002, SCPH-7502, SCPH-9002 [g]. This is for Rev C hardware [w].");
			var psone_43j = File("339A48F4FCF63E10B5B867B8C93CFD40945FAF6C", 524288, "psone-43j.bin", "PSX BIOS (Version 4.3 03/11/00 J)", "Used on PSone SCPH-100 [g]. This is for Rev C PSone hardware [w].");
			var psone_44e = File("BEB0AC693C0DC26DAF5665B3314DB81480FA5C7C", 524288, "psone-44e.bin", "PSX BIOS (Version 4.4 03/24/00 E)", "Used on PSone SCPH-102 [g]. This is for Rev C PSone hardware [w].");
			var psone_45a = File("DCFFE16BD90A723499AD46C641424981338D8378", 524288, "psone-45a.bin", "PSX BIOS (Version 4.5 05/25/00 A)", "Used on PSone SCPH-101 [g]. This is for Rev C PSone hardware [w].");
			var psone_r5e = File("DBC7339E5D85827C095764FC077B41F78FD2ECAE", 524288, "psone-45e.bin", "PSX BIOS (Version 4.5 05/25/00 E)", "Used on PSone SCPH-102 [g]. This is for Rev C PSone hardware [w].");
			var ps2_50j = File("D7D6BE084F51354BC951D8FA2D8D912AA70ABC5E", 4194304, "ps2-50j.bin", "PSX BIOS (Version 5.0 10/27/00 J)", "Found on a PS2 [p].");

			ps_22j_bad.bad = ps_22j_bad2.bad = ps_30e_bad.bad = true;

			Firmware("PSX", "U", "BIOS (U)");
			Firmware("PSX", "J", "BIOS (J)");
			Firmware("PSX", "E", "BIOS (E)");

			Option("PSX", "U", ps_30a);
			Option("PSX", "J", ps_30j);
			Option("PSX", "E", ps_30e);
			//in general, alternates arent allowed.. their quality isnt known.
			//we have this comment from fobby.net: "SCPH7502 works fine for European games" (TBD)
			//however, we're sticking with the 3.0 series.
			//please note: 2.1 or 2.2 would be a better choice, as the dates are the same and the bioses are more likely to matching in terms of entrypoints and such.
			//but 3.0 is what mednafen used

			Option("PSX", "J", ps_10j, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "J", ps_11j, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "U", ps_20a, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "E", ps_20e, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "J", ps_21j, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "U", ps_21a, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "E", ps_21e, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "J", ps_22j, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "J", ps_22j_bad, FirmwareOptionStatus.Bad);
			Option("PSX", "J", ps_22j_bad2, FirmwareOptionStatus.Bad);
			Option("PSX", "U", ps_22a, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "E", ps_22e, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "E", ps_30e_bad, FirmwareOptionStatus.Bad);
			Option("PSX", "J", ps_40j, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "U", ps_41a, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "E", ps_41e, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "J", psone_43j, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "U", psone_45a, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "E", psone_r5e, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "J", ps2_50j, FirmwareOptionStatus.Unacceptable);

			Firmware("AppleII", "AppleIIe", "AppleIIe.rom");
			var appleII_AppleIIe = File("B8EA90ABE135A0031065E01697C4A3A20D51198B", 16384, "AppleIIe.rom", "Apple II e");
			Option("AppleII", "AppleIIe", appleII_AppleIIe, FirmwareOptionStatus.Acceptable);

			Firmware("AppleII", "DiskII", "DiskII.rom");
			var appleII_DiskII = File("D4181C9F046AAFC3FB326B381BAAC809D9E38D16", 256, "DiskII.rom", "Disk II");
			Option("AppleII", "DiskII", appleII_DiskII, FirmwareOptionStatus.Acceptable);
		}

		//adds a defined firmware ID to the database
		static void Firmware(string systemId, string id, string descr)
		{
			var fr = new FirmwareRecord
				{
					systemId = systemId,
					firmwareId = id,
					descr = descr
				};

			FirmwareRecords.Add(fr);
		}

		//adds an acceptable option for a firmware ID to the database
		static FirmwareOption Option(string hash, long size, string systemId, string id, FirmwareOptionStatus status = FirmwareOptionStatus.Acceptable)
		{
			var fo = new FirmwareOption
				{
					systemId = systemId,
					firmwareId = id,
					hash = hash,
					status = status,
					size = size
				};

			FirmwareOptions.Add(fo);
			
			//first option is automatically ideal
			if (FirmwareOptions.Count == 1 && fo.status == FirmwareOptionStatus.Acceptable)
				fo.status = FirmwareOptionStatus.Ideal;
			
			return fo;
		}

		//adds an acceptable option for a firmware ID to the database
		static FirmwareOption Option(string systemId, string id, FirmwareFile ff, FirmwareOptionStatus status = FirmwareOptionStatus.Acceptable)
		{
			var fo = Option(ff.hash, ff.size, systemId, id, status);
			//make sure this goes in as bad
			if(ff.bad) fo.status = FirmwareOptionStatus.Bad;
			return fo;
		}

		//defines a firmware file
		static FirmwareFile File(string hash, long size, string recommendedName, string descr, string additionalInfo = "")
		{
			string hashfix = hash.ToUpperInvariant();

			var ff = new FirmwareFile
				{
					hash = hashfix,
					size = size,
					recommendedName = recommendedName,
					descr = descr,
					info = additionalInfo
				};
			FirmwareFiles.Add(ff);
			FirmwareFilesByHash[hashfix] = ff;
			return ff;
		}

		//adds a defined firmware ID and one file and option
		static void FirmwareAndOption(string hash, long size, string systemId, string id, string name, string descr)
		{
			Firmware(systemId, id, descr);
			File(hash, size, name, descr, "");
			Option(hash, size, systemId, id);
		}


		public static List<FirmwareRecord> FirmwareRecords = new List<FirmwareRecord>();
		public static List<FirmwareOption> FirmwareOptions = new List<FirmwareOption>();
		public static List<FirmwareFile> FirmwareFiles = new List<FirmwareFile>();

		public static Dictionary<string, FirmwareFile> FirmwareFilesByHash = new Dictionary<string, FirmwareFile>();

		public class FirmwareFile
		{
			public string hash;
			public long size;
			public string recommendedName;
			public string descr;
			public string info;
			public bool bad;
		}

		public class FirmwareRecord
		{
			public string systemId;
			public string firmwareId;
			public string descr;
			public string ConfigKey { get { return string.Format("{0}+{1}", systemId, firmwareId); } }
		}

		public enum FirmwareOptionStatus
		{
			Ideal, Acceptable, Unacceptable, Bad
		}

		public class FirmwareOption
		{
			public string systemId;
			public string firmwareId;
			public string hash;
			public long size;
			public FirmwareOptionStatus status;
			public bool IsAcceptableOrIdeal { get { return status == FirmwareOptionStatus.Ideal || status == FirmwareOptionStatus.Acceptable; } }
			public string ConfigKey { get { return string.Format("{0}+{1}", systemId, firmwareId); } }
		}


		public static FirmwareRecord LookupFirmwareRecord(string sysId, string firmwareId)
		{
			var found =
				(from fr in FirmwareRecords
				 where fr.firmwareId == firmwareId
				 && fr.systemId == sysId
				 select fr);

			return found.FirstOrDefault();
		}

	} //static class FirmwareDatabase
}