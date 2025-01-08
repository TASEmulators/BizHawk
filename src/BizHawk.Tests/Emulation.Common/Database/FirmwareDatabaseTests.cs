using System.Collections.Generic;
using System.Linq;

#if !NET7_0_OR_GREATER
using BizHawk.Common.CollectionExtensions;
#endif
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
				=> Assert.IsTrue(hash.Length == 40 && hash == hash.ToUpperInvariant() && hash.IsHex(), $"incorrectly formatted: {hash}");
			foreach (var hash in FirmwareDatabase.FirmwareFilesByHash.Keys) CustomAssert(hash);
			foreach (var fo in FirmwareDatabase.FirmwareOptions) CustomAssert(fo.Hash);
		}

		[TestMethod]
		public void CheckOrganizeFilenames()
		{
			SortedList<string, FirmwareFile> seen = new();
			List<FirmwareFile> dupes = new();
			foreach (var ff in FirmwareDatabase.FirmwareFilesByHash.Values)
			{
				if (seen.TryGetValue(ff.RecommendedName, out var ffExisting))
				{
					dupes.Add(ffExisting);
					dupes.Add(ff);
				}
				else
				{
					seen.Add(ff.RecommendedName, ff);
				}
			}
			if (dupes.Count is 0) return;
			Assert.Fail($"multiple {nameof(FirmwareFile)}s have the same suggested filename (breaks Organize function):\n{
				string.Join("\n", dupes.Select(static ff => $"\t{ff.RecommendedName} {ff.Hash}").Order())
			}");
		}
	}
}
