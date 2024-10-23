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
			Console.WriteLine(StatusFor(SHA1Checksum.EmptyFile)); // NotInDatabase
			Console.WriteLine(StatusFor("3064E664D34859649B67559F0ED0C2FFD6948031")); // BadDump
		}
	}
}
