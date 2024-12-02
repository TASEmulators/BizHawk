using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Tests.Emulation.Common
{
	[TestClass]
	public class FirmwareDatabaseTests
	{
		[TestMethod]
		public void CheckFilesInOptions()
		{
			foreach (var fo in FirmwareDatabase.FirmwareOptions) Assert.IsTrue(FirmwareDatabase.FirmwareFilesByHash.ContainsKey(fo.Hash), $"option {fo.ID} references unknown file {fo.Hash}");
		}

		[TestMethod]
		public void CheckFormatOfHashes()
		{
			static void CustomAssert(string hash)
#pragma warning disable CA1862 // incorrect detection, see https://github.com/dotnet/roslyn-analyzers/issues/7074
				=> Assert.IsTrue(hash.Length == 40 && hash == hash.ToUpperInvariant() && hash.IsHex(), $"incorrectly formatted: {hash}");
#pragma warning restore CA1862
			foreach (var hash in FirmwareDatabase.FirmwareFilesByHash.Keys) CustomAssert(hash);
			foreach (var fo in FirmwareDatabase.FirmwareOptions) CustomAssert(fo.Hash);
		}
	}
}
