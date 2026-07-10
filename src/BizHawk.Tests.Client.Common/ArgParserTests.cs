using System.IO;
using System.Linq;

using BizHawk.Client.Common;

namespace BizHawk.Tests.Client.Common
{
	[TestClass]
	public sealed class ArgParserTests
	{
		[DataRow("--config=config.json", "--help")]
		[DataRow("--config=config.json", "--version")]
		[TestMethod]
		public void TestConfigWithHelpOrVersion(params string[] args)
			=> Assert.AreEqual(0, ArgParser.ParseArguments(out _, args, fromUnitTest: true));

		[TestMethod]
		public void TestHelpSaysPassFlagsFirst()
		{
			using StringWriter output = new();
			ArgParser.RunHelpActionForUnitTest(output);
			var outputLines = output.ToString().Split('\n');
			var usageLine = outputLines[outputLines.Index().First(tuple => tuple.Item.Contains("Usage:")).Index + 1].ToUpperInvariant();
			Assert.IsTrue(usageLine.IndexOf("OPTION") < usageLine.IndexOf("ROM"));
		}

		[DataRow("rom.nes", "--nonexistent")]
		[DataRow("--nonexistent", "rom.nes")]
		[DataRow("--nonexistent", "--", "rom.nes")]
		[TestMethod]
		public void TestWithNonexistent(params string[] args)
		{
			int? exitCode = null;
			var e = Assert.ThrowsExactly<ArgParser.ArgParserException>(() => exitCode = ArgParser.ParseArguments(out _, args, fromUnitTest: true));
			Assert.AreNotEqual(0, exitCode ?? 1);
			Assert.Contains(substring: "Unrecog", e.Message);
			Assert.Contains(substring: "--nonexistent", e.Message);
		}
	}
}
