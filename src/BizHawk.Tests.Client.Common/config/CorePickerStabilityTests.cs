using System.Collections.Generic;
using System.Linq;

using BizHawk.Client.Common;
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
				CollectionAssert.That.Contains(multiCoreSystems, sysID, $"a default core preference exists for {sysID} but that system doesn't have alternate cores");
			}
			foreach (var (appliesTo, _) in Config.CorePickerUIData)
			{
				CollectionAssert.That.IsSubsetOf(
					superset: multiCoreSystems,
					subset: appliesTo,
					message: appliesTo.Length is 1
						? $"core picker has submenu for {appliesTo[0]}, but that system doesn't have alternate cores"
#pragma warning disable MA0089 // CI build this for .NET Core where there's a `char` overload for `string.Join`
						: $"core picker has submenu for {appliesTo[0]} ({string.Join("/", appliesTo)}), but none of those systems have alternate cores");
#pragma warning restore MA0089
			}
		}

		[TestMethod]
		public void AssertNoMissingCores()
		{
			var allCoreNames = CoreInventory.Instance.SystemsFlat.Select(coreInfo => coreInfo.Name).ToHashSet();
			foreach (var (sysID, coreName) in DefaultCorePrefDict)
			{
				CollectionAssert.That.Contains(allCoreNames, coreName, $"default core preference for {sysID} is \"{coreName}\", which doesn't exist");
			}
			foreach (var (appliesTo, coreNames) in Config.CorePickerUIData) foreach (var coreName in coreNames)
			{
				CollectionAssert.That.Contains(allCoreNames, coreName, $"core picker includes nonexistant core \"{coreName}\" under {appliesTo[0]} group");
			}
		}

		[TestMethod]
		public void AssertNoMissingChoices()
		{
			var multiCoreSystems = CoreInventory.Instance.AllCores.Where(kvp => kvp.Value.Count != 1).ToArray();
			foreach (var (sysID, cores) in multiCoreSystems)
			{
				CollectionAssert.That.ContainsKey(DefaultCorePrefDict, sysID, $"missing default core preference for {sysID} with {cores.Count} core choices");
				CollectionAssert.That.Contains(Config.CorePickerUIData.SelectMany(static item => item.AppliesTo), sysID, $"missing core picker submenu for {sysID} with {cores.Count} core choices");
			}
		}
	}
}
