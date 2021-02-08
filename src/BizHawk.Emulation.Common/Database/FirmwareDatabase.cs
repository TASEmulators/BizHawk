using System.Collections.Generic;

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable StringLiteralTypo
namespace BizHawk.Emulation.Common
{
	public static class FirmwareDatabase
	{
		static FirmwareDatabase()
		{
			// FDS has two OK variants  (http://tcrf.net/Family_Computer_Disk_System)
			var fdsNintendo = File("57FE1BDEE955BB48D357E463CCBF129496930B62", 8192, "FDS_disksys-nintendo.rom", "Bios (Nintendo)");
			var fdsTwinFc = File("E4E41472C454F928E53EB10E0509BF7D1146ECC1", 8192, "FDS_disksys-nintendo.rom", "Bios (TwinFC)");
			Firmware("NES", "Bios_FDS", "Bios");
			Option("NES", "Bios_FDS", fdsNintendo, FirmwareOptionStatus.Ideal);
			Option("NES", "Bios_FDS", fdsTwinFc);

			FirmwareAndOption("973E10840DB683CF3FAF61BD443090786B3A9F04", 262144, "SNES", "Rom_SGB", "SNES_sgb.sfc", "Super GameBoy Rom"); // World (Rev B) ?
			FirmwareAndOption("A002F4EFBA42775A31185D443F3ED1790B0E949A", 3072, "SNES", "CX4", "SNES_cx4.rom", "CX4 Rom");
			FirmwareAndOption("188D471FEFEA71EB53F0EE7064697FF0971B1014", 8192, "SNES", "DSP1", "SNES_dsp1.rom", "DSP1 Rom");
			FirmwareAndOption("78B724811F5F18D8C67669D9390397EB1A47A5E2", 8192, "SNES", "DSP1b", "SNES_dsp1b.rom", "DSP1b Rom");
			FirmwareAndOption("198C4B1C3BFC6D69E734C5957AF3DBFA26238DFB", 8192, "SNES", "DSP2", "SNES_dsp2.rom", "DSP2 Rom");
			FirmwareAndOption("558DA7CB3BD3876A6CA693661FFC6C110E948CF9", 8192, "SNES", "DSP3", "SNES_dsp3.rom", "DSP3 Rom");
			FirmwareAndOption("AF6478AECB6F1B67177E79C82CA04C56250A8C72", 8192, "SNES", "DSP4", "SNES_dsp4.rom", "DSP4 Rom");
			FirmwareAndOption("6472828403DE3589433A906E2C3F3D274C0FF008", 53248, "SNES", "ST010", "SNES_st010.rom", "ST010 Rom");
			FirmwareAndOption("FECBAE2CEC76C710422486BAA186FFA7CA1CF925", 53248, "SNES", "ST011", "SNES_st011.rom", "ST011 Rom");
			FirmwareAndOption("91383B92745CC7CC4F15409AC5BC2C2F699A43F1", 163840, "SNES", "ST018", "SNES_st018.rom", "ST018 Rom");

			FirmwareAndOption("79F5FF55DD10187C7FD7B8DAAB0B3FFBD1F56A2C", 262144, "PCECD", "Bios", "PCECD_3.0-(J).pce", "Super CD Bios (J)");
			FirmwareAndOption("014881A959E045E00F4DB8F52955200865D40280", 32768, "PCECD", "GE-Bios", "PCECD_gecard.pce", "Games Express CD Card (Japan)");

			Firmware("A78", "Bios_NTSC", "NTSC Bios");
#if false
			Option("A78", "Bios_NTSC", File("CE236581AB7921B59DB95BA12837C22F160896CB", 4096, "A78_NTSC_speed_bios.bin", "NTSC Bios speed"));
#endif
			Option("A78", "Bios_NTSC", File("D9D134BB6B36907C615A594CC7688F7BFCEF5B43", 4096, "A78_NTSC_bios.bin", "NTSC Bios"));
			FirmwareAndOption("5A140136A16D1D83E4FF32A19409CA376A8DF874", 16384, "A78", "Bios_PAL", "A78_PAL_BIOS.bin", "PAL Bios");
			FirmwareAndOption("A3AF676991391A6DD716C79022D4947206B78164", 4096, "A78", "Bios_HSC", "A78_highscore.bin", "Highscore Bios");

			FirmwareAndOption("45BEDC4CBDEAC66C7DF59E9E599195C778D86A92", 8192, "Coleco", "Bios", "Coleco_Bios.bin", "Bios");

			FirmwareAndOption("B9BBF5BB0EAC52D039A4A993A2D8064B862C9E28", 4096, "Vectrex", "Bios", "Vectrex_Bios.bin", "Bios");
			FirmwareAndOption("65D07426B520DDD3115D40F255511E0FD2E20AE7", 8192, "Vectrex", "Minestorm", "Vectrex_Minestorm.vec", "Game");

			var gbaNormal = File("300C20DF6731A33952DED8C436F7F186D25D3492", 16384, "GBA_bios.rom", "Bios (World)");
			var gbaJDebug = File("AA98A2AD32B86106340665D1222D7D973A1361C7", 16384, "GBA_bios_Debug-(J).rom", "Bios (J Debug)");
			Firmware("GBA", "Bios", "Bios");
			Option("GBA", "Bios", gbaNormal);
			Option("GBA", "Bios", gbaJDebug);

			FirmwareAndOption("24F67BDEA115A2C847C8813A262502EE1607B7DF", 16384, "NDS", "bios7", "NDS_Bios7.bin", "ARM7 BIOS");
			FirmwareAndOption("BFAAC75F101C135E32E2AAF541DE6B1BE4C8C62D", 4096, "NDS", "bios9", "NDS_Bios9.bin", "ARM9 BIOS");
			FirmwareAndOption("22A7547DBC302BCBFB4005CFB5A2D426D3F85AC6", 262144, "NDS", "firmware", "NDS_Firmware.bin", "NDS Firmware (note: given hash is with blank user data)");

			FirmwareAndOption("E4ED47FAE31693E016B081C6BDA48DA5B70D7CCB", 512, "Lynx", "Boot", "LYNX_boot.img", "Boot Rom");

			FirmwareAndOption("5A65B922B562CB1F57DAB51B73151283F0E20C7A", 8192, "INTV", "EROM", "INTV_EROM.bin", "Executive Rom");
			FirmwareAndOption("F9608BB4AD1CFE3640D02844C7AD8E0BCD974917", 2048, "INTV", "GROM", "INTV_GROM.bin", "Graphics Rom");

			FirmwareAndOption("1D503E56DF85A62FEE696E7618DC5B4E781DF1BB", 8192, "C64", "Kernal", "C64_Kernal.bin", "Kernal Rom");
			FirmwareAndOption("79015323128650C742A3694C9429AA91F355905E", 8192, "C64", "Basic", "C64_Basic.bin", "Basic Rom");
			FirmwareAndOption("ADC7C31E18C7C7413D54802EF2F4193DA14711AA", 4096, "C64", "Chargen", "C64_Chargen.bin", "Chargen Rom");
			FirmwareAndOption("AB16F56989B27D89BABE5F89C5A8CB3DA71A82F0", 16384, "C64", "Drive1541", "C64_Drive-1541.bin", "1541 Disk Drive Rom");
			FirmwareAndOption("D3B78C3DBAC55F5199F33F3FE0036439811F7FB3", 16384, "C64", "Drive1541II", "C64_Drive-1541ii.bin", "1541-II Disk Drive Rom");

			// ZX Spectrum
			FirmwareAndOption("A584272F21DC82C14B7D4F1ED440E23A976E71F0", 32768, "ZXSpectrum", "PentagonROM", "ZX_pentagon.rom", "Russian Pentagon Clone ROM");
			FirmwareAndOption("282EB7BC819AAD2A12FD954E76F7838A4E1A7929", 16384, "ZXSpectrum", "TRDOSROM", "ZX_trdos.rom", "TRDOS ROM");

			// MSX
			FirmwareAndOption("B398CFCB94C9F7E808E0FECE54813CFDFB96F8D0", 16384, "MSX", "bios_test", "MSX_bios.rom", "MSX BIOS");
			FirmwareAndOption("18559FA9C2D9E99A319550D809009ECDBA6D396E", 16384, "MSX", "basic_test", "MSX_cbios_basic.rom", "MSX BASIC (C-BIOS v0.29a)");
			FirmwareAndOption("2F997E8A57528518C82AB3693FDAE243DBBCC508", 32768, "MSX", "bios_test_ext", "MSX_cbios_main_msx1.rom", "MSX BIOS (C-BIOS v0.29a)");
			FirmwareAndOption("E998F0C441F4F1800EF44E42CD1659150206CF79", 16384, "MSX", "bios_pal", "MSX_8020-20bios.rom", "MSX BIOS (Philips VG-8020)");
			FirmwareAndOption("DF48902F5F12AF8867AE1A87F255145F0E5E0774", 16384, "MSX", "bios_jp", "MSX_4000bios.rom", "MSX BIOS (FS-4000)");

			// Channel F
			FirmwareAndOption("81193965A374D77B99B4743D317824B53C3E3C78", 1024, "ChannelF", "ChannelF_sl131253", "ChannelF_SL31253.rom", "Channel F Rom0");
			FirmwareAndOption("8F70D1B74483BA3A37E86CF16C849D601A8C3D2C", 1024, "ChannelF", "ChannelF_sl131254", "ChannelF_SL31254.rom", "Channel F Rom1");

			// for saturn, we think any bios region can pretty much run any iso
			// so, we're going to lay this out carefully so that we choose things in a sensible order, but prefer the correct region
			var ss_100_j = File("2B8CB4F87580683EB4D760E4ED210813D667F0A2", 524288, "SAT_1.00-(J).bin", "Bios v1.00 (J)");
			var ss_100_ue = File("FAA8EA183A6D7BBE5D4E03BB1332519800D3FBC3", 524288, "SAT_1.00-(U+E).bin", "Bios v1.00 (U+E)");
			var ss_100a_ue = File("3BB41FEB82838AB9A35601AC666DE5AACFD17A58", 524288, "SAT_1.00a-(U+E).bin", "Bios v1.00a (U+E)"); // ?? is this size correct?
			var ss_101_j = File("DF94C5B4D47EB3CC404D88B33A8FDA237EAF4720", 524288, "SAT_1.01-(J).bin", "Bios v1.01 (J)"); // ?? is this size correct?
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
			FirmwareAndOption("A67CD4F550751F8B91DE2B8B74528AB4E0C11C77", 2 * 1024 * 1024, "SAT", "KOF95", "SAT_KoF95.bin", "King of Fighters cartridge");
			//Firmware("SAT", "ULTRAMAN", "Ultraman cartridge");
			FirmwareAndOption("56C1B93DA6B660BF393FBF48CA47569000EF4047", 2 * 1024 * 1024, "SAT", "ULTRAMAN", "SAT_Ultraman.bin", "Ultraman cartridge");

			var ti83_102 = File("CE08F6A808701FC6672230A790167EE485157561", 262144, "TI83_102.rom", "TI-83 Rom v1.02"); // ?? is this size correct?
			var ti83_103 = File("8399E384804D8D29866CAA4C8763D7A61946A467", 262144, "TI83_103.rom", "TI-83 Rom v1.03"); // ?? is this size correct?
			var ti83_104 = File("33877FF637DC5F4C5388799FD7E2159B48E72893", 262144, "TI83_104.rom", "TI-83 Rom v1.04"); // ?? is this size correct?
			var ti83_106 = File("3D65C2A1B771CE8E5E5A0476EC1AA9C9CDC0E833", 262144, "TI83_106.rom", "TI-83 Rom v1.06"); // ?? is this size correct?
			var ti83_107 = File("EF66DAD3E7B2B6A86F326765E7DFD7D1A308AD8F", 262144, "TI83_107.rom", "TI-83 Rom v1.07"); // formerly the 1.?? recommended one
			var ti83_108 = File("9C74F0B61655E9E160E92164DB472AD7EE02B0F8", 262144, "TI83_108.rom", "TI-83 Rom v1.08"); // ?? is this size correct?
			var ti83p_103 = File("37EAEEB9FB5C18FB494E322B75070E80CC4D858E", 262144, "TI83p_103b.rom", "TI-83 Plus Rom v1.03"); // ?? is this size correct?
			var ti83p_112 = File("6615DF5554076B6B81BD128BF847D2FF046E556B", 262144, "TI83p_112.rom", "TI-83 Plus Rom v1.12"); // ?? is this size correct?
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
			var eu_mcd1_9210 = File("F891E0EA651E2232AF0C5C4CB46A0CAE2EE8F356", 131072, "MCD_eu_9210.bin", "Mega CD EU (9210)");
			var eu_mcd2_9303 = File("7063192AE9F6B696C5B81BC8F0A9FE6F0C400E58", 131072, "MCD_eu_9303.bin", "Mega CD EU (9303)");
			var eu_mcd2_9306 = File("523B3125FB0AC094E16AA072BC6CCDCA22E520E5", 131072, "MCD_eu_9306.bin", "Mega CD EU (9310)"); // ?? is this size correct?
			var jp_mcd1_9111 = File("4846F448160059A7DA0215A5DF12CA160F26DD69", 131072, "MCD_jp_9111.bin", "Mega CD JP (9111)");
			var jp_mcd1_9112 = File("E4193C6AE44C3CEA002707D2A88F1FBCCED664DE", 131072, "MCD_jp_9112.bin", "Mega CD JP (9112)");
			var us_scd1_9210 = File("F4F315ADCEF9B8FEB0364C21AB7F0EAF5457F3ED", 131072, "SCD_us_9210.bin", "Sega CD US (9210)");
			var us_scd2_9303 = File("BD3EE0C8AB732468748BF98953603CE772612704", 131072, "SCD_us_9303.bin", "Sega CD US (9303)");
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
			FirmwareAndOption("DBEBD76A448447CB6E524AC3CB0FD19FC065D944", 256, "32X", "G", "32X_G_BIOS.BIN", "32x 68k BIOS");
			FirmwareAndOption("1E5B0B2441A4979B6966D942B20CC76C413B8C5E", 2048, "32X", "M", "32X_M_BIOS.BIN", "32x SH2 MASTER BIOS");
			FirmwareAndOption("4103668C1BBD66C5E24558E73D4F3F92061A109A", 1024, "32X", "S", "32X_S_BIOS.BIN", "32x SH2 SLAVE BIOS");

			// SMS
			var sms_us_13 = File("C315672807D8DDB8D91443729405C766DD95CAE7", 8192, "SMS_us_1.3.sms", "SMS BIOS 1.3 (USA, Europe)");
			var sms_jp_21 = File("A8C1B39A2E41137835EDA6A5DE6D46DD9FADBAF2", 8192, "SMS_jp_2.1.sms", "SMS BIOS 2.1 (Japan)");
			var sms_us_1b = File("29091FF60EF4C22B1EE17AA21E0E75BAC6B36474", 8192, "SMS_us_1.0b.sms", "SMS BIOS 1.0 (USA) (Proto)"); // ?? is this size correct?
			var sms_m404 = File("4A06C8E66261611DCE0305217C42138B71331701", 8192, "SMS_m404.sms", "SMS BIOS (USA) (M404) (Proto)"); // ?? is this size correct?
			var sms_kr = File("2FEAFD8F1C40FDF1BD5668F8C5C02E5560945B17", 131072, "SMS_kr.sms", "SMS BIOS (Kr)"); // ?? is this size correct?
			Firmware("SMS", "Export", "SMS Bios (USA/Export)");
			Firmware("SMS", "Japan", "SMS Bios (Japan)");
			Firmware("SMS", "Korea", "SMS Bios (Korea)");
			Option("SMS", "Export", sms_us_13);
			Option("SMS", "Export", sms_us_1b);
			Option("SMS", "Export", sms_m404);
			Option("SMS", "Japan", sms_jp_21);
			Option("SMS", "Korea", sms_kr);

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
			var ps_ps3 = File("C40146361EB8CF670B19FDC9759190257803CAB7", 524288, "PSX_rom.bin", "PSX BIOS (Version 5.0 06/23/03 A)", "Found on a PS3. [t]");
			Firmware("PSX", "U", "BIOS (U)");
			Firmware("PSX", "J", "BIOS (J)");
			Firmware("PSX", "E", "BIOS (E)");
			Option("PSX", "U", ps_30a);
			Option("PSX", "J", ps_30j);
			Option("PSX", "E", ps_30e);
			// in general, alternates aren't allowed.. their quality isn't known.
			// we have this comment from fobby.net: "SCPH7502 works fine for European games" (TBD)
			// however, we're sticking with the 3.0 series.
			// please note: 2.1 or 2.2 would be a better choice, as the dates are the same and the bioses are more likely to matching in terms of entry points and such.
			// but 3.0 is what mednafen used
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
			Option("PSX", "J", ps_22d, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "E", ps_30e_bad, FirmwareOptionStatus.Bad);
			Option("PSX", "J", ps_40j, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "U", ps_41a, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "E", ps_41e, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "J", psone_43j, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "E", psone_44e, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "U", psone_45a, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "E", psone_r5e, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "J", ps2_50j, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "J", ps_22jv, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "U", ps_41aw, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "U", ps_ps3, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "U", ps_dtl_h2000, FirmwareOptionStatus.Unacceptable); //not really sure what to do with this one, let's just call it region free
			Option("PSX", "E", ps_dtl_h2000, FirmwareOptionStatus.Unacceptable);
			Option("PSX", "J", ps_dtl_h2000, FirmwareOptionStatus.Unacceptable);

			Firmware("AppleII", "AppleIIe", "AppleIIe.rom");
			var appleII_AppleIIe = File("B8EA90ABE135A0031065E01697C4A3A20D51198B", 16384, "AppleIIe.rom", "Apple II e");
			Option("AppleII", "AppleIIe", appleII_AppleIIe);
			Firmware("AppleII", "DiskII", "DiskII.rom");
			var appleII_DiskII = File("D4181C9F046AAFC3FB326B381BAAC809D9E38D16", 256, "AppleIIe_DiskII.rom", "Disk II");
			Option("AppleII", "DiskII", appleII_DiskII);

			FirmwareAndOption("B2E1955D957A475DE2411770452EFF4EA19F4CEE", 1024, "O2", "BIOS", "O2_Odyssey2.bin", "Odyssey 2 Bios");
			FirmwareAndOption("A6120AED50831C9C0D95DBDF707820F601D9452E", 1024, "O2", "BIOS-C52", "O2_PhillipsC52.bin", "Phillips C52 Bios");
			FirmwareAndOption("5130243429B40B01A14E1304D0394B8459A6FBAE", 1024, "G7400", "BIOS", "G7400_bios.bin", "G7400 Bios");

			Firmware("GB", "World", "Game Boy Boot Rom");
			Option("GB", "World", File("4ED31EC6B0B175BB109C0EB5FD3D193DA823339F", 256, "dmg.bin", "Game Boy Boot Rom"), FirmwareOptionStatus.Ideal);
			// Early revisions of GB/C boot ROMs are not well-supported because the corresponding CPU differences are not emulated.
			Option("GB", "World", File("8BD501E31921E9601788316DBD3CE9833A97BCBC", 256, "dmg0.bin", "Game Boy Boot Rom (Early J Revision)"), FirmwareOptionStatus.Unacceptable);
			Option("GB", "World", File("4E68F9DA03C310E84C523654B9026E51F26CE7F0", 256, "mgb.bin", "Game Boy Boot Rom (Pocket)"), FirmwareOptionStatus.Acceptable);

			Firmware("GBC", "World", "Game Boy Color Boot Rom");
			Option("GBC", "World", File("1293D68BF9643BC4F36954C1E80E38F39864528D", 2304, "cgb.bin", "Game Boy Color Boot Rom"), FirmwareOptionStatus.Ideal);
			Option("GBC", "World", File("DF5A0D2D49DE38FBD31CC2AAB8E62C8550E655C0", 2304, "cgb0.bin", "Game Boy Color Boot Rom (Early Revision)"), FirmwareOptionStatus.Unacceptable);
			Firmware("GBC", "AGB", "Game Boy Color Boot Rom (GBA)");
			Option("GBC", "AGB", File("FA5287E24B0FA533B3B5EF2B28A81245346C1A0F", 2304, "agb.bin", "Game Boy Color Boot Rom (GBA)"), FirmwareOptionStatus.Ideal);
			Option("GBC", "AGB", File("1ECAFA77AB3172193F3305486A857F443E28EBD9", 2304, "agb_gambatte.bin", "Game Boy Color Boot Rom (GBA, Gambatte RE)"), FirmwareOptionStatus.Bad);

			Firmware("PCFX", "BIOS", "PCFX bios");
			var pcfxbios = File("1A77FD83E337F906AECAB27A1604DB064CF10074", 1024 * 1024, "PCFX_bios.bin", "PCFX BIOS 1.00");
			var pcfxv101 = File("8B662F7548078BE52A871565E19511CCCA28C5C8", 1024 * 1024, "PCFX_v101.bin", "PCFX BIOS 1.01");
			Option("PCFX", "BIOS", pcfxbios, FirmwareOptionStatus.Ideal);
			Option("PCFX", "BIOS", pcfxv101, FirmwareOptionStatus.Acceptable);
			Firmware("PCFX", "SCSIROM", "fx-scsi.rom");
			var fxscsi = File("65482A23AC5C10A6095AEE1DB5824CCA54EAD6E5", 512 * 1024, "PCFX_fx-scsi.rom", "PCFX SCSI ROM");
			Option("PCFX", "SCSIROM", fxscsi);

			Firmware("PS2", "BIOS", "PS2 Bios");
			Option("PS2", "BIOS", File("FBD54BFC020AF34008B317DCB80B812DD29B3759", 4 * 1024 * 1024, "ps2-0230j-20080220.bin", "PS2 Bios"));
			Option("PS2", "BIOS", File("8361D615CC895962E0F0838489337574DBDC9173", 4 * 1024 * 1024, "ps2-0220a-20060905.bin", "PS2 Bios"));
			Option("PS2", "BIOS", File("DA5AACEAD2FB55807D6D4E70B1F10F4FDCFD3281", 4 * 1024 * 1024, "ps2-0220e-20060905.bin", "PS2 Bios"));
			Option("PS2", "BIOS", File("3BAF847C1C217AA71AC6D298389C88EDB3DB32E2", 4 * 1024 * 1024, "ps2-0220j-20060905.bin", "PS2 Bios"));
			Option("PS2", "BIOS", File("F9229FE159D0353B9F0632F3FDC66819C9030458", 4 * 1024 * 1024, "ps2-0230a-20080220.bin", "PS2 Bios"), FirmwareOptionStatus.Ideal);
			Option("PS2", "BIOS", File("9915B5BA56798F4027AC1BD8D10ABE0C1C9C326A", 4 * 1024 * 1024, "ps2-0230e-20080220.bin", "PS2 Bios"));
		}

		// adds a defined firmware ID to the database
		private static void Firmware(string systemId, string id, string descr)
		{
			var fr = new FirmwareRecord
			{
				SystemId = systemId,
				FirmwareId = id,
				Descr = descr
			};

			FirmwareRecords.Add(fr);
		}

		private static FirmwareOption _OptionWork(string hash, long size, string systemId, string id, FirmwareOptionStatus status = FirmwareOptionStatus.Acceptable)
		{
			var fo = new FirmwareOption
			{
				SystemId = systemId,
				FirmwareId = id,
				Hash = hash,
				Status = status,
				Size = size
			};
			FirmwareOptions.Add(fo);
			return fo;
		}

		// adds an acceptable option for a firmware ID to the database
		private static FirmwareOption Option(string systemId, string id, FirmwareFile ff, FirmwareOptionStatus status = FirmwareOptionStatus.Acceptable)
		{
			var fo = _OptionWork(ff.Hash, ff.Size, systemId, id, status);

			// make sure this goes in as bad
			if (ff.Bad)
			{
				fo.Status = FirmwareOptionStatus.Bad;
			}

			return fo;
		}

		// defines a firmware file
		private static FirmwareFile File(
			string hash,
			long size,
			string recommendedName,
			string desc,
			string additionalInfo = "",
			bool isBad = false)
		{
			var ff = new FirmwareFile
			{
				Hash = hash,
				Size = size,
				RecommendedName = recommendedName,
				Description = desc,
				Info = additionalInfo,
				Bad = isBad,
			};
			FirmwareFilesByHash[hash] = ff;
			return ff;
		}

		// adds a defined firmware ID and one file and option
		private static void FirmwareAndOption(string hash, long size, string systemId, string id, string name, string descr)
		{
			Firmware(systemId, id, descr);
			File(hash, size, name, descr);
			_OptionWork(hash, size, systemId, id);
		}

		public static readonly List<FirmwareRecord> FirmwareRecords = new List<FirmwareRecord>();
		public static readonly List<FirmwareOption> FirmwareOptions = new List<FirmwareOption>();

		public static IEnumerable<FirmwareFile> FirmwareFiles => FirmwareFilesByHash.Values;

		public static readonly Dictionary<string, FirmwareFile> FirmwareFilesByHash = new Dictionary<string, FirmwareFile>();

		public class FirmwareFile
		{
			public string Hash { get; set; }
			public long Size { get; set; }
			public string RecommendedName { get; set; }
			public string Description { get; set; }
			public string Info { get; set; }
			public bool Bad { get; set; }
		}

		public class FirmwareRecord
		{
			public string SystemId { get; set; }
			public string FirmwareId { get; set; }
			public string Descr { get; set; }

			public string ConfigKey => $"{SystemId}+{FirmwareId}";
		}

		public enum FirmwareOptionStatus
		{
			/// <summary>Preferred to get checkmarks, and for TASing</summary>
			Ideal,

			/// <summary>Works with our core, but not preferred for TASing</summary>
			Acceptable,

			/// <summary>A good file, but it doesn't work with our core</summary>
			Unacceptable,

			/// <summary>Nonlegitimate files that are notable enough to be worth detecting, even if mainly to categorize as a BAD option</summary>
			Bad
		}

		public class FirmwareOption
		{
			public string SystemId { get; set; }
			public string FirmwareId { get; set; }
			public string Hash { get; set; }
			public long Size { get; set; }
			public FirmwareOptionStatus Status { get; set; }

			public bool IsAcceptableOrIdeal => Status == FirmwareOptionStatus.Ideal || Status == FirmwareOptionStatus.Acceptable;
			public string ConfigKey => $"{SystemId}+{FirmwareId}";
		}
	} // static class FirmwareDatabase
}
