using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;

using BizHawk.Common.IOExtensions;

using static BizHawk.Tests.Testroms.SMS.SMSHelper;

namespace BizHawk.Tests.Testroms.SMS
{
	[TestClass]
	public sealed class SMSmemtest
	{
		[AttributeUsage(AttributeTargets.Method)]
		private sealed class SMSmemtestDataAttribute : Attribute, ITestDataSource
		{
			public static readonly IReadOnlyCollection<string> Subtests = [ "RAM", "VRAM", "SRAM" ];

			public IEnumerable<object?[]> GetData(MethodInfo methodInfo)
			{
				var testCases = CoreSetup.ValidSetupsFor().ToList();
//				testCases.RemoveAll(static setup => setup.CoreName is not CoreSetup.GPGX_MAME); // uncomment and modify to run a subset of the test cases...
				foreach (var subTest in SMSmemtestDataAttribute.Subtests)
				{
					testCases.RemoveAll(setup => TestUtils.ShouldIgnoreCase(SUITE_ID, DisplayNameFor(setup, subTest))); // ...or use the global blocklist in TestUtils
				}
				return testCases.OrderBy(static setup => setup.ToString())
					.Select(static setup => new object?[] { setup });
			}

			public string? GetDisplayName(MethodInfo methodInfo, object?[]? data)
				=> $"{methodInfo.Name}(\"{(CoreSetup) data![0]!}\")";
		}

		private const string ROM_EMBED_PATH = "res.SMSmemtest_artifact.SMSmemtest.sms";

		private const string SUITE_ID = "SMSmemtest";

		private static readonly IReadOnlyList<string> KnownFailures = new[]
		{
			"", // none \o/
		};

		private static readonly bool RomIsPresent = ReflectionCache.EmbeddedResourceList().Contains(ROM_EMBED_PATH);

		[ClassCleanup]
		public static void AfterAll()
			=> TestUtils.WriteMetricsToDisk();

		[ClassInitialize]
		public static void BeforeAll(TestContext ctx)
		{
			TestUtils.AssertKnownFailuresAreSorted(KnownFailures, suiteID: SUITE_ID);
			TestUtils.PrepareDBAndOutput(SUITE_ID);
		}

		private static string DisplayNameFor(CoreSetup setup, string subTest)
			=> $"SMSmemtest.{subTest} on {setup}";

		[SMSmemtestData]
		[TestMethod]
		public void RunSMSmemtest(CoreSetup setup)
		{
			TestUtils.ShortCircuitMissingRom(RomIsPresent);
			TestUtils.ShortCircuitKnownFailure(SMSmemtestDataAttribute.Subtests.Select(subTest => DisplayNameFor(setup, subTest)).All(KnownFailures.Contains));
			DummyFrontend fe = new(InitSMSCore(setup, "SMSmemtest.sms", ReflectionCache.EmbeddedResourceStream(ROM_EMBED_PATH).ReadAllBytes()));
			bool DoSubcaseAssertion(string subTest, Bitmap actual)
			{
				var caseStr = DisplayNameFor(setup, subTest);
				var knownFail = TestUtils.IsKnownFailure(caseStr, KnownFailures);
				var state = SMSScreenshotsEqual(
					ReflectionCache.EmbeddedResourceStream($"res.SMSmemtest_artifact.expected_{subTest.ToLowerInvariant()}.png"),
					actual,
					knownFail,
					setup,
					(SUITE_ID, caseStr));
				switch (state)
				{
					case TestUtils.TestSuccessState.ExpectedFailure:
						Console.WriteLine("expected failure, verified");
						return false;
					case TestUtils.TestSuccessState.Failure:
						Assert.Fail("expected and actual screenshots differ");
						return default;
					case TestUtils.TestSuccessState.Success:
						return true;
					case TestUtils.TestSuccessState.UnexpectedSuccess:
						Assert.Fail("expected and actual screenshots matched unexpectedly (this is a good thing)");
						return default;
					default:
						return default;
				}
			}
			fe.FrameAdvanceBy(273);
			// SMSHawk is done now
			fe.FrameAdvanceBy(18);
			var ramPassed = DoSubcaseAssertion("RAM", fe.Screenshot());
			fe.Dispose();
			if (!ramPassed) Assert.Inconclusive(); // for this to be false, it must have been an expected failure or execution would have stopped with an Assert.Fail call
//			Assert.Inconclusive("(other subtests aren't implemented)"); // uncommenting this causes tests to hang..?
		}
	}
}
