using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using BizHawk.Common.IOExtensions;

using static BizHawk.Tests.Testroms.GB.GBHelper;

namespace BizHawk.Tests.Testroms.GB
{
	[TestClass]
	public sealed class AcidTestroms
	{
		public readonly struct AcidTestCase
		{
			public string ExpectEmbedPath => TestName switch
			{
				"cgb-acid-hell" => "res.cgb_acid_hell_artifact.reference.png",
				"cgb-acid2" => "res.cgb_acid2_artifact.reference.png",
				"dmg-acid2" => $"res.dmg_acid2_artifact.reference-{(Setup.Variant.IsColour() ? "cgb" : "dmg")}.png",
				_ => throw new InvalidOperationException()
			};

			public readonly string RomEmbedPath => TestName switch
			{
				"cgb-acid-hell" => "res.cgb_acid_hell_artifact.cgb-acid-hell.gbc",
				"cgb-acid2" => "res.cgb_acid2_artifact.cgb-acid2.gbc",
				"dmg-acid2" => "res.dmg_acid2_artifact.dmg-acid2.gb",
				_ => throw new InvalidOperationException()
			};

			public readonly CoreSetup Setup;

			public readonly string TestName;

			public AcidTestCase(string testName, CoreSetup setup)
			{
				Setup = setup;
				TestName = testName;
			}

			public readonly string DisplayName()
				=> $"{TestName} on {Setup}";
		}

		[AttributeUsage(AttributeTargets.Method)]
		private sealed class AcidTestDataAttribute : Attribute, ITestDataSource
		{
			public IEnumerable<object?[]> GetData(MethodInfo methodInfo)
			{
				List<AcidTestCase> testCases = new();
				foreach (var setup in CoreSetup.ValidSetupsFor(ConsoleVariant.CGB_C))
				{
					testCases.Add(new("cgb-acid-hell", setup));
					testCases.Add(new("cgb-acid2", setup));
					testCases.Add(new("dmg-acid2", setup));
				}
				foreach (var setup in CoreSetup.ValidSetupsFor(ConsoleVariant.DMG))
				{
					testCases.Add(new("dmg-acid2", setup));
				}
//				testCases.RemoveAll(static testCase => testCase.Setup.Variant is not ConsoleVariant.DMG); // uncomment and modify to run a subset of the test cases...
				testCases.RemoveAll(static testCase => TestUtils.ShouldIgnoreCase(SUITE_ID, testCase.DisplayName())); // ...or use the global blocklist in TestUtils
				return testCases.OrderBy(static testCase => testCase.DisplayName())
					.Select(static testCase => new object?[] { testCase });
			}

			public string? GetDisplayName(MethodInfo methodInfo, object?[]? data)
				=> $"{methodInfo.Name}(\"{((AcidTestCase) data![0]!).DisplayName()}\")";
		}

		private const string SUITE_ID = "AcidTestroms";

		private static readonly IReadOnlyList<string> FilteredEmbedPaths = ReflectionCache.EmbeddedResourceList().Where(static s => s.Contains("acid")).ToList();

		private static readonly IReadOnlyCollection<string> KnownFailures = new[]
		{
			"", // none \o/
		};

		[ClassCleanup]
		public static void AfterAll()
			=> TestUtils.WriteMetricsToDisk();

		[ClassInitialize]
		public static void BeforeAll(TestContext ctx)
			=> TestUtils.PrepareDBAndOutput(SUITE_ID);

		[AcidTestData]
		[DataTestMethod]
		public void RunAcidTest(AcidTestCase testCase)
		{
			TestUtils.ShortCircuitMissingRom(isPresent: FilteredEmbedPaths.Contains(testCase.RomEmbedPath));
			var caseStr = testCase.DisplayName();
			TestUtils.ShortCircuitKnownFailure(caseStr, KnownFailures, out var knownFail);
			var actualUnnormalised = DummyFrontend.RunAndScreenshot(
				InitGBCore(testCase.Setup, $"{testCase.TestName}.gbc", ReflectionCache.EmbeddedResourceStream(testCase.RomEmbedPath).ReadAllBytes()),
				static fe => fe.FrameAdvanceBy(18));
			var state = GBScreenshotsEqual(
				ReflectionCache.EmbeddedResourceStream(testCase.ExpectEmbedPath),
				actualUnnormalised,
				knownFail,
				testCase.Setup,
				(SUITE_ID, caseStr),
				MattCurriePaletteMap);
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
