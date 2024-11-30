using System.Collections.Generic;
using System.Linq;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Cores;

namespace BizHawk.Tests.Client.Common.config
{
	[TestClass]
	public sealed class CorePickerStabilityTests
	{
		private static readonly IReadOnlyDictionary<string, string> DefaultCorePrefDict = new Config().PreferredCores;

		[TestMethod]
		public void AssertAllChoicesValid()
		{
			var multiCoreSystems = CoreInventory.Instance.AllCores.Where(kvp => kvp.Value.Count != 1)
				.Select(kvp => kvp.Key)
				.ToHashSet();
			foreach (var sysID in DefaultCorePrefDict.Keys)
			{
				Assert.IsTrue(multiCoreSystems.Contains(sysID), $"a default core preference exists for {sysID} but that system doesn't have alternate cores");
			}
			foreach (var (appliesTo, _) in Config.CorePickerUIData)
			{
				Assert.IsTrue(
					appliesTo.All(multiCoreSystems.Contains),
					appliesTo.Length is 1
						? $"core picker has submenu for {appliesTo[0]}, but that system doesn't have alternate cores"
						: $"core picker has submenu for {appliesTo[0]} ({string.Join("/", appliesTo)}), but none of those systems have alternate cores");
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
			foreach (var (appliesTo, coreNames) in Config.CorePickerUIData) foreach (var coreName in coreNames)
			{
				Assert.IsTrue(allCoreNames.Contains(coreName), $"core picker includes nonexistant core \"{coreName}\" under {appliesTo[0]} group");
			}
		}

		[TestMethod]
		public void AssertNoMissingChoices()
		{
			var multiCoreSystems = CoreInventory.Instance.AllCores.Where(kvp => kvp.Value.Count != 1).ToArray();
			foreach (var (sysID, cores) in multiCoreSystems)
			{
				Assert.IsTrue(DefaultCorePrefDict.ContainsKey(sysID), $"missing default core preference for {sysID} with {cores.Count} core choices");
				Assert.IsTrue(Config.CorePickerUIData.Any(item => item.AppliesTo.Contains(sysID)), $"missing core picker submenu for {sysID} with {cores.Count} core choices");
			}
		}
	}
}
