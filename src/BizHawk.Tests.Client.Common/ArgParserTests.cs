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
//			Assert.Contains(substring: "--nonexistent", e.Message);
		}
	}
}
