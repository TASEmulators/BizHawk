using System.Collections.Generic;
using System.Linq;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Emulation.Cores;

namespace BizHawk.Tests.Client.Common.config
{
	[TestClass]
	public sealed class CorePickerStabilityTests
	{
		private static readonly IReadOnlyDictionary<string, string> DefaultCorePrefDict = new Config().PreferredCores;

		[TestMethod]
		public void AssertAllPrefencesExist()
		{
			var multiCoreSystems = CoreInventory.Instance.AllCores.Where(kvp => kvp.Value.Count != 1)
				.Select(kvp => kvp.Key);
			foreach (var sysID in multiCoreSystems)
			{
				Assert.IsTrue(DefaultCorePrefDict.ContainsKey(sysID), $"{sysID} has multiple cores, but no default core preference exists for it!");
			}
		}

		[TestMethod]
		public void AssertNoExtraPreferences()
		{
			var singleCoreSystems = CoreInventory.Instance.AllCores.Where(kvp => kvp.Value.Count == 1)
				.Select(kvp => kvp.Key);
			foreach (var sysID in singleCoreSystems)
			{
				Assert.IsFalse(DefaultCorePrefDict.ContainsKey(sysID), $"{sysID} only has one core implementing it, but an unnecessary preference choice exists for it!");
			}
		}

		[TestMethod]
		public void AssertNoMissingCores()
		{
			var allCoreNames = CoreInventory.Instance.SystemsFlat.Select(coreInfo => coreInfo.Name).ToHashSet();
			foreach (var (sysID, coreName) in DefaultCorePrefDict)
			{
				Assert.IsTrue(allCoreNames.Contains(coreName), $"default core preference for {sysID} is \"{coreName}\", which doesn't exist");
			}
		}

		[TestMethod]
		public void AssertExactlyOnePreferredCore()
		{
			foreach(var (systemId, cores) in CoreInventory.Instance.AllCores)
			{
				if (cores.Count >= 2)
				{
					int preferredCoresCount = cores.Count(core => core.Priority == CorePriority.DefaultPreference);
					Assert.IsTrue(preferredCoresCount == 1, $"{systemId} has {preferredCoresCount} preferred cores, expected exactly 1.");
				}
			}
		}

		[TestMethod]
		public void AssertNoConflictingPreferenceInGroup()
		{
			foreach(var (systemIds, cores) in CoreInventory.Instance.SystemGroups.Where(tuple => tuple.CoreNames.Count > 1))
			{
				var preferredCoreForGroup = DefaultCorePrefDict[systemIds[0]];
				foreach (var systemId in systemIds)
				{
					var preferredCore = DefaultCorePrefDict[systemId];

					Assert.AreEqual(preferredCoreForGroup, preferredCore, $"Default core preference for {systemId} does not match the preferred core for the whole group ({string.Join(" | ", systemIds)})");
				}
			}
		}
	}
}
