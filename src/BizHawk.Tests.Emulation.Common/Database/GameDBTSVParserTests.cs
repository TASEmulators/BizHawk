using BizHawk.Emulation.Common;

namespace BizHawk.Tests.Emulation.Common
{
	[TestClass]
	public sealed class GameDBTSVParserTests
	{
		[DataRow(
			"sha1:f4885610503bff2c4ca816f4f28d1fe517b92f35\t\t2 Pak Special Yellow - Star Warrior,Frogger (1990) (HES) (PAL) [!]\tA26\t\tm=F6;PAL=true",
			"F4885610503BFF2C4CA816F4F28D1FE517B92F35",
			RomStatus.GoodDump,
			"2 Pak Special Yellow - Star Warrior,Frogger (1990) (HES) (PAL) [!]",
			"A26",
			"m=F6;PAL=true",
			""/*empty string*/,
			""/*empty string*/)]
		[DataRow(
			"3064E664D34859649B67559F0ED0C2FFD6948031\tB\tActRaiser 2 (U) [b1]\tSNES",
			"3064E664D34859649B67559F0ED0C2FFD6948031",
			RomStatus.BadDump,
			"ActRaiser 2 (U) [b1]",
			"SNES",
			null,
			""/*empty string*/,
			""/*empty string*/)]
		[TestMethod]
		public void Check(
			string line,
			string hashDigest,
			RomStatus dumpStatus,
			string knownName,
			string sysID,
			string? metadata,
			string region,
			string forcedCore)
		{
			var gi = Database.ParseCGIRecord(line);
			Assert.AreEqual(expected: hashDigest, actual: gi.Hash, message: nameof(CompactGameInfo.Hash));
			Assert.AreEqual(expected: dumpStatus, actual: gi.Status, message: nameof(CompactGameInfo.Status));
			Assert.AreEqual(expected: knownName, actual: gi.Name, message: nameof(CompactGameInfo.Name));
			Assert.AreEqual(expected: sysID, actual: gi.System, message: nameof(CompactGameInfo.System));
			Assert.AreEqual(expected: metadata, actual: gi.MetaData, message: nameof(CompactGameInfo.MetaData));
			Assert.AreEqual(expected: region, actual: gi.Region, message: nameof(CompactGameInfo.Region));
			Assert.AreEqual(expected: forcedCore, actual: gi.ForcedCore, message: nameof(CompactGameInfo.ForcedCore));
		}
	}
}
