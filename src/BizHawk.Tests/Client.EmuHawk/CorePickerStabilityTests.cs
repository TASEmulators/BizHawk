using System.Collections.Generic;
using System.Linq;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BizHawk.Tests.Client.EmuHawk
{
	[TestClass]
	public sealed class CorePickerStabilityTests
	{
		private static readonly IReadOnlyDictionary<string, string> DefaultCorePrefDict = new Config().PreferredCores;

		[TestMethod]
		public void AssertAllChoicesInMenu()
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
					appliesTo.Any(multiCoreSystems.Contains),
					$"core picker has submenu for {appliesTo[0]} ({string.Join('/', appliesTo)}), but {(appliesTo.Length == 1 ? "that system doesn't have" : "none of those systems have")} alternate cores");
			}
		}

		[TestMethod]
		public void AssertNoMissingCores()
		{
			var allCoreNames = CoreInventory.Instance.SystemsFlat.Select(coreInfo => coreInfo.Name).ToHashSet();
			foreach (var kvp in DefaultCorePrefDict)
			{
				Assert.IsTrue(allCoreNames.Contains(kvp.Value), $"default core preference for {kvp.Key} is \"{kvp.Value}\", which doesn't exist");
			}
			foreach (var (appliesTo, coreNames) in Config.CorePickerUIData) foreach (var coreName in coreNames)
			{
				Assert.IsTrue(allCoreNames.Contains(coreName), $"core picker includes nonexistant core \"{coreName}\" under {appliesTo[0]} group");
			}
		}

		/// <remarks>this really shouldn't be necessary</remarks>
		[TestMethod]
		public void AssertNoMissingSystems()
		{
			var allSysIDs = CoreInventory.Instance.AllCores.Keys.ToHashSet();
#if false // already covered by AssertAllChoicesInMenu
			foreach (var sysID in DefaultCorePrefDict.Keys)
			{
				Assert.IsTrue(allSysIDs.Contains(sysID), $"a default core preference exists for {sysID}, which isn't emulated by any core");
			}
#endif
			foreach (var (appliesTo, _) in Config.CorePickerUIData) foreach (var sysID in appliesTo)
			{
				Assert.IsTrue(allSysIDs.Contains(sysID), $"core picker has choices for {sysID}, which isn't emulated by any core");
			}
		}
	}
}
