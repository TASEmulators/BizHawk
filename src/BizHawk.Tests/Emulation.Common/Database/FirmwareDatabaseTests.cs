using BizHawk.Emulation.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

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
	}
}
