using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using BizHawk.Common.IOExtensions;

using static BizHawk.Tests.Testroms.GB.GBHelper;

namespace BizHawk.Tests.Testroms.GB
{
	[TestClass]
	public sealed class BullyGB
	{
		[AttributeUsage(AttributeTargets.Method)]
		private sealed class BullyTestData : Attribute, ITestDataSource
		{
			public IEnumerable<object?[]> GetData(MethodInfo methodInfo)
			{
				var testCases = new[] { ConsoleVariant.CGB_C, ConsoleVariant.DMG }.SelectMany(CoreSetup.ValidSetupsFor).ToList();
//				testCases.RemoveAll(static setup => setup.Variant is not ConsoleVariant.DMG); // uncomment and modify to run a subset of the test cases...
				testCases.RemoveAll(static setup => TestUtils.ShouldIgnoreCase(SUITE_ID, DisplayNameFor(setup))); // ...or use the global blocklist in TestUtils
				return testCases.OrderBy(static setup => setup.ToString())
					.Select(static setup => new object?[] { setup });
			}

			public string? GetDisplayName(MethodInfo methodInfo, object?[]? data)
				=> $"{methodInfo.Name}(\"{(CoreSetup) data![0]!}\")";
		}

		private const string ROM_EMBED_PATH = "res.BullyGB_artifact.bully.gb";

		private const string SUITE_ID = "BullyGB";

		private static readonly IReadOnlyCollection<string> KnownFailures = new[]
		{
			"BullyGB on CGB_C in GBHawk",
			"BullyGB on CGB_C in SameBoy (no BIOS)",
			"BullyGB on DMG in SameBoy (no BIOS)",
		};

		private static readonly bool RomIsPresent = ReflectionCache.EmbeddedResourceList().Contains(ROM_EMBED_PATH);

		[ClassCleanup]
		public static void AfterAll()
			=> TestUtils.WriteMetricsToDisk();

		[ClassInitialize]
		public static void BeforeAll(TestContext ctx)
			=> TestUtils.PrepareDBAndOutput(SUITE_ID);

		private static string DisplayNameFor(CoreSetup setup)
			=> $"BullyGB on {setup}";

		[BullyTestData]
		[DataTestMethod]
		public void RunBullyTest(CoreSetup setup)
		{
			TestUtils.ShortCircuitMissingRom(RomIsPresent);
			var caseStr = DisplayNameFor(setup);
			TestUtils.ShortCircuitKnownFailure(caseStr, KnownFailures, out var knownFail);
			var actualUnnormalised = DummyFrontend.RunAndScreenshot(
				InitGBCore(setup, "bully.gbc", ReflectionCache.EmbeddedResourceStream(ROM_EMBED_PATH).ReadAllBytes()),
				static fe => fe.FrameAdvanceBy(20));
			var state = GBScreenshotsEqual(
				ReflectionCache.EmbeddedResourceStream($"res.BullyGB_artifact.expected_{(setup.Variant.IsColour() ? "cgb" : "dmg")}.png"),
				actualUnnormalised,
				knownFail,
				setup,
				(SUITE_ID, caseStr));
			switch (state)
			{
				case TestUtils.TestSuccessState.ExpectedFailure:
					Assert.Inconclusive("expected failure, verified");
					break;
				case TestUtils.TestSuccessState.Failure:
					Assert.Fail("expected and actual screenshots differ");
					break;
				case TestUtils.TestSuccessState.UnexpectedSuccess:
					Assert.Fail("expected and actual screenshots matched unexpectedly (this is a good thing)");
					break;
			}
		}
	}
}
