using System;
using System.Linq;
using System.Collections.Generic;

namespace BizHawk
{
	public static class FirmwareDatabase
	{
		static FirmwareDatabase()
		{
			//FDS has two OK variants  (http://tcrf.net/Family_Computer_Disk_System)
			var fds_nintendo = File("57FE1BDEE955BB48D357E463CCBF129496930B62", "disksys-nintendo.rom", "Bios (Nintendo)");
			var fds_twinfc = File("E4E41472C454F928E53EB10E0509BF7D1146ECC1", "disksys-nintendo.rom", "Bios (TwinFC)");
			Firmware("NES", "Bios_FDS", "Bios");
			Option("NES", "Bios_FDS", fds_nintendo);
			Option("NES", "Bios_FDS", fds_twinfc);

			FirmwareAndOption("973E10840DB683CF3FAF61BD443090786B3A9F04", "SNES", "Rom_SGB", "sgb.sfc", "Super GameBoy Rom"); //World (Rev B) ?
			FirmwareAndOption("A002F4EFBA42775A31185D443F3ED1790B0E949A", "SNES", "CX4", "cx4.rom", "CX4 Rom");
			FirmwareAndOption("188D471FEFEA71EB53F0EE7064697FF0971B1014", "SNES", "DSP1", "dsp1.rom", "DSP1 Rom");
			FirmwareAndOption("78B724811F5F18D8C67669D9390397EB1A47A5E2", "SNES", "DSP1b", "dsp1b.rom", "DSP1b Rom");
			FirmwareAndOption("198C4B1C3BFC6D69E734C5957AF3DBFA26238DFB", "SNES", "DSP2", "dsp2.rom", "DSP2 Rom");
			FirmwareAndOption("558DA7CB3BD3876A6CA693661FFC6C110E948CF9", "SNES", "DSP3", "dsp3.rom", "DSP3 Rom");
			FirmwareAndOption("AF6478AECB6F1B67177E79C82CA04C56250A8C72", "SNES", "DSP4", "dsp4.rom", "DSP4 Rom");
			FirmwareAndOption("6472828403DE3589433A906E2C3F3D274C0FF008", "SNES", "ST010", "st010.rom", "ST010 Rom");
			FirmwareAndOption("FECBAE2CEC76C710422486BAA186FFA7CA1CF925", "SNES", "ST011", "st011.rom", "ST011 Rom");
			FirmwareAndOption("91383B92745CC7CC4F15409AC5BC2C2F699A43F1", "SNES", "ST018", "st018.rom", "ST018 Rom");
			FirmwareAndOption("79F5FF55DD10187C7FD7B8DAAB0B3FFBD1F56A2C", "PCECD", "Bios", "pcecd-3.0-(J).pce", "Super CD Bios (J)");
			FirmwareAndOption("D9D134BB6B36907C615A594CC7688F7BFCEF5B43", "A78", "Bios_NTSC", "7800NTSCBIOS.bin", "NTSC Bios");
			FirmwareAndOption("5A140136A16D1D83E4FF32A19409CA376A8DF874", "A78", "Bios_PAL", "7800PALBIOS.bin", "PAL Bios");
			FirmwareAndOption("A3AF676991391A6DD716C79022D4947206B78164", "A78", "Bios_HSC", "7800highscore.bin", "Highscore Bios");
			FirmwareAndOption("45BEDC4CBDEAC66C7DF59E9E599195C778D86A92", "Coleco", "Bios", "ColecoBios.bin", "Bios");
			FirmwareAndOption("300C20DF6731A33952DED8C436F7F186D25D3492", "GBA", "Bios", "gbabios.rom", "Bios");
			//FirmwareAndOption("24F67BDEA115A2C847C8813A262502EE1607B7DF", "NDS", "Bios_Arm7", "biosnds7.rom", "ARM7 Bios");
			//FirmwareAndOption("BFAAC75F101C135E32E2AAF541DE6B1BE4C8C62D", "NDS", "Bios_Arm9", "biosnds9.rom", "ARM9 Bios");
			FirmwareAndOption("EF66DAD3E7B2B6A86F326765E7DFD7D1A308AD8F", "TI83", "Rom", "ti83_1.rom", "TI-83 Rom");
			FirmwareAndOption("5A65B922B562CB1F57DAB51B73151283F0E20C7A", "INTV", "EROM", "erom.bin", "Executive Rom");
			FirmwareAndOption("F9608BB4AD1CFE3640D02844C7AD8E0BCD974917", "INTV", "GROM", "grom.bin", "Graphics Rom");
			FirmwareAndOption("1D503E56DF85A62FEE696E7618DC5B4E781DF1BB", "C64", "Kernal", "c64-kernal.bin", "Kernal Rom");
			FirmwareAndOption("79015323128650C742A3694C9429AA91F355905E", "C64", "Basic", "c64-basic.bin", "Basic Rom");
			FirmwareAndOption("ADC7C31E18C7C7413D54802EF2F4193DA14711AA", "C64", "Chargen", "c64-chargen.bin", "Chargen Rom");

			//for saturn, we think any bios region can pretty much run any iso
			//so, we're going to lay this out carefully so that we choose things in a sensible order, but prefer the correct region
			var ss_100_j = File("2B8CB4F87580683EB4D760E4ED210813D667F0A2", "saturn-1.00-(J).bin", "Bios v1.00 (J)");
			var ss_100_ue = File("FAA8EA183A6D7BBE5D4E03BB1332519800D3FBC3", "saturn-1.00-(U+E).bin", "Bios v1.00 (U+E)");
			var ss_100a_ue = File("3BB41FEB82838AB9A35601AC666DE5AACFD17A58", "saturn-1.00a-(U+E).bin", "Bios v1.00a (U+E)");
			var ss_101_j = File("DF94C5B4D47EB3CC404D88B33A8FDA237EAF4720", "saturn-1.01-(J).bin", "Bios v1.01 (J)");
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
		}

		//adds a defined firmware ID to the database
		static void Firmware(string systemId, string id, string descr)
		{
			var fr = new FirmwareRecord();
			fr.systemId = systemId;
			fr.firmwareId = id;

			fr.descr = descr;
			FirmwareRecords.Add(fr);
		}

		//adds an acceptable option for a firmware ID to the database
		static void Option(string hash, string systemId, string id)
		{
			var fo = new FirmwareOption();
			fo.systemId = systemId;
			fo.firmwareId = id;
			fo.hash = hash;

			FirmwareOptions.Add(fo);
		}

		//adds an acceptable option for a firmware ID to the database
		static void Option(string systemId, string id, FirmwareFile ff)
		{
			Option(ff.hash, systemId, id);
		}

		//defines a firmware file
		static FirmwareFile File(string hash, string recommendedName, string descr)
		{
			var ff = new FirmwareFile();
			ff.hash = hash;
			ff.recommendedName = recommendedName;
			ff.descr = descr;
			FirmwareFiles.Add(ff);
			FirmwareFilesByHash[hash] = ff;
			return ff;
		}

		//adds a defined firmware ID and one file and option
		static void FirmwareAndOption(string hash, string systemId, string id, string name, string descr)
		{
			Firmware(systemId, id, descr);
			File(hash, name, descr);
			Option(hash, systemId, id);
		}


		public static List<FirmwareRecord> FirmwareRecords = new List<FirmwareRecord>();
		public static List<FirmwareOption> FirmwareOptions = new List<FirmwareOption>();
		public static List<FirmwareFile> FirmwareFiles = new List<FirmwareFile>();

		public static Dictionary<string, FirmwareFile> FirmwareFilesByHash = new Dictionary<string, FirmwareFile>();

		public class FirmwareFile
		{
			public string hash;
			public string recommendedName;
			public string descr;
		}

		public class FirmwareRecord
		{
			public string systemId;
			public string firmwareId;
			public string descr;
			public string ConfigKey { get { return string.Format("{0}+{1}", systemId, firmwareId); } }
		}

		public class FirmwareOption
		{
			public string systemId;
			public string firmwareId;
			public string hash;
			public string ConfigKey { get { return string.Format("{0}+{1}", systemId, firmwareId); } }
		}


		public static FirmwareRecord LookupFirmwareRecord(string sysId, string firmwareId)
		{
			var found =
				(from fr in FirmwareRecords
				 where fr.firmwareId == firmwareId
				 && fr.systemId == sysId
				 select fr).First();

			return found;
		}

	} //static class FirmwareDatabase
}