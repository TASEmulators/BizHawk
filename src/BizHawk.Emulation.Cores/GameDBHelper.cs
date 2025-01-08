using System.Diagnostics;
using System.IO;

using BizHawk.Common;
using BizHawk.Common.PathExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Arcades.MAME;
using BizHawk.Emulation.Cores.Nintendo.NES;

namespace BizHawk.Emulation.Cores
{
	public static class GameDBHelper
	{
		public static void BackgroundInitAll()
		{
			var bundledGamedbPath = Path.Combine(PathUtils.ExeDirectoryPath, "gamedb");
			Database.InitializeDatabase(
				bundledRoot: bundledGamedbPath,
				userRoot: Path.Combine(PathUtils.DataDirectoryPath, "gamedb"),
				silent: true);
			BootGodDb.Initialize(bundledGamedbPath);
			MAMEMachineDB.Initialize(bundledGamedbPath);
		}

		public static void WaitForThreadAndQuickTest()
		{
			RomStatus StatusFor(string hashDigest)
				=> Database.CheckDatabase(hashDigest)?.Status ?? RomStatus.NotInDatabase;
			Console.WriteLine($"ex: NotInDatabase, ac: {StatusFor(SHA1Checksum.EmptyFile)}");
			Console.WriteLine($"ex: BadDump, ac: {StatusFor("3064E664D34859649B67559F0ED0C2FFD6948031")}");
			Console.WriteLine($"ex: GoodDump, ac: {StatusFor("B558814D54904CE0582E2F6A801D03AF")}");
			var giBanjo = Database.CheckDatabase("1FE1632098865F639E22C11B9A81EE8F29C75D7A");
			Console.WriteLine($"ex: 4, ac: {(
				giBanjo is null
					? "(DB miss)"
					: giBanjo.OptionValue("RiceRenderToTextureOption") ?? "(option not present)"
			)}");
			var stopwatch = Stopwatch.StartNew();
			for (var i = 0; i < 1_000_000; i++) _ = StatusFor("3064E664D34859649B67559F0ED0C2FFD6948031");
			Console.WriteLine($"lookup perf test: {stopwatch.Elapsed}");
		}
	}
}
