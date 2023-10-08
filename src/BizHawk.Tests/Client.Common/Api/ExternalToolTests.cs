using System.IO;
using System.Linq;
using System.Reflection;
using BizHawk.Client.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BizHawk.Tests.Client.Common.Api
{
	[TestClass]
	public class ExternalToolTests
	{
		[ClassInitialize]
		public static void TestInitialize(TestContext context)
		{
			// Move our .dll to a directory by itself, so that the ExternalToolManager will only find us.
			string asmName = Assembly.GetExecutingAssembly().Location;
			Directory.CreateDirectory("extTools");
			File.Copy(asmName, "extTools/ExternalToolTests.dll");
		}

		[TestMethod]
		public void TestExternalToolIsFound()
		{
			Config config = new Config();
			config.PathEntries.Paths.Find((e) => string.Equals(e.Type, "External Tools", System.StringComparison.Ordinal))!.Path = "./extTools";
			string t = config.PathEntries[PathEntryCollection.GLOBAL, "External Tools"].Path;
			ExternalToolManager manager = new ExternalToolManager(config, () => ("", ""), (p1, p2, p3) => true);

			Assert.IsTrue(manager.ToolStripItems.Count != 0);
			var item = manager.ToolStripItems.First(static (info) => info.Text == "TEST");
			Assert.AreEqual("TEST", item.Text);
		}
	}
}
