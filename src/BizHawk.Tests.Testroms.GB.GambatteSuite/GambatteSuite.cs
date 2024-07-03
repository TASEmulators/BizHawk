using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using BizHawk.Common.CollectionExtensions;
using BizHawk.Common.IOExtensions;
using BizHawk.Common.StringExtensions;

using static BizHawk.Tests.Testroms.GB.GBHelper;

namespace BizHawk.Tests.Testroms.GB.GambatteSuite
{
	[TestClass]
	public sealed partial class GambatteSuite
	{
		/// <remarks>there are 4664 * 3 cores = 13992 of these tests @_0</remarks>
		[AttributeUsage(AttributeTargets.Method)]
		private sealed class GambatteHexStrTestDataAttribute : Attribute, ITestDataSource
		{
			public IEnumerable<object?[]> GetData(MethodInfo methodInfo)
			{
				if (!RomsArePresent) return new[] { new object?[] { GambatteHexStrTestCase.Dummy } };
				return (_allCases ??= EnumerateAllCases()).HexStrCases
//					.Where(static testCase => testCase.Setup.Variant is ConsoleVariant.DMG) // uncomment and modify to run a subset of the test cases...
					.Where(static testCase => !TestUtils.ShouldIgnoreCase(SUITE_ID, testCase.DisplayName())) // ...or use the global blocklist in TestUtils
					.OrderBy(static testCase => testCase.DisplayName())
					.Select(static testCase => new object?[] { testCase });
			}

			public string? GetDisplayName(MethodInfo methodInfo, object?[]? data)
				=> $"{methodInfo.Name}(\"{((GambatteHexStrTestCase) data![0]!).DisplayName()}\")";
		}

		[AttributeUsage(AttributeTargets.Method)]
		private sealed class GambatteRefImageTestDataAttribute : Attribute, ITestDataSource
		{
			public IEnumerable<object?[]> GetData(MethodInfo methodInfo)
			{
				if (!RomsArePresent) return new[] { new object?[] { GambatteRefImageTestCase.Dummy } };
				return (_allCases ??= EnumerateAllCases()).RefImageCases
//					.Where(static testCase => testCase.Setup.Variant is ConsoleVariant.DMG) // uncomment and modify to run a subset of the test cases...
					.Where(static testCase => !TestUtils.ShouldIgnoreCase(SUITE_ID, testCase.DisplayName())) // ...or use the global blocklist in TestUtils
					.OrderBy(static testCase => testCase.DisplayName())
					.Select(static testCase => new object?[] { testCase });
			}

			public string? GetDisplayName(MethodInfo methodInfo, object?[]? data)
				=> $"{methodInfo.Name}(\"{((GambatteRefImageTestCase) data![0]!).DisplayName()}\")";
		}

		private static readonly byte[,] GLYPHS = {
			{ 0b00000000, 0b00000000, 0b00000000, 0b00000000, 0b00000000, 0b00000000, 0b00000000, 0b00000000, 0b00000000, 0b00000000, 0b00000000, 0b00000000, 0b00000000, 0b00000000, 0b00000000, 0b00000000 },
			{ 0b01111111, 0b00001000, 0b01111111, 0b01111111, 0b01000001, 0b01111111, 0b01111111, 0b01111111, 0b00111110, 0b01111111, 0b00001000, 0b01111110, 0b00111110, 0b01111110, 0b01111111, 0b01111111 },
			{ 0b01000001, 0b00001000, 0b00000001, 0b00000001, 0b01000001, 0b01000000, 0b01000000, 0b00000001, 0b01000001, 0b01000001, 0b00100010, 0b01000001, 0b01000001, 0b01000001, 0b01000000, 0b01000000 },
			{ 0b01000001, 0b00001000, 0b00000001, 0b00000001, 0b01000001, 0b01000000, 0b01000000, 0b00000010, 0b01000001, 0b01000001, 0b01000001, 0b01000001, 0b01000000, 0b01000001, 0b01000000, 0b01000000 },
			{ 0b01000001, 0b00001000, 0b01111111, 0b00111111, 0b01111111, 0b01111110, 0b01111111, 0b00000100, 0b00111110, 0b01111111, 0b01111111, 0b01111110, 0b01000000, 0b01000001, 0b01111111, 0b01111111 },
			{ 0b01000001, 0b00001000, 0b01000000, 0b00000001, 0b00000001, 0b00000001, 0b01000001, 0b00001000, 0b01000001, 0b00000001, 0b01000001, 0b01000001, 0b01000000, 0b01000001, 0b01000000, 0b01000000 },
			{ 0b01000001, 0b00001000, 0b01000000, 0b00000001, 0b00000001, 0b00000001, 0b01000001, 0b00010000, 0b01000001, 0b00000001, 0b01000001, 0b01000001, 0b01000001, 0b01000001, 0b01000000, 0b01000000 },
			{ 0b01111111, 0b00001000, 0b01111111, 0b01111111, 0b00000001, 0b01111110, 0b01111111, 0b00010000, 0b00111110, 0b01111111, 0b01000001, 0b01111110, 0b00111110, 0b01111110, 0b01111111, 0b01000000 },
		};

		private const string SUITE_ID = "GambatteSuite";

		private const string SUITE_PREFIX = "res.Gambatte_testroms_artifact.";

		private static (IReadOnlyList<GambatteRefImageTestCase> RefImageCases, IReadOnlyList<GambatteHexStrTestCase> HexStrCases)? _allCases = null;

		private static readonly Regex HexStrFilenameRegex = new(@"_out[0-9A-F]+\.gbc?$");

		private static readonly bool RomsArePresent = ReflectionCache.EmbeddedResourceList().Any(static s => s.StartsWith(SUITE_PREFIX));

		[ClassCleanup]
		public static void AfterAll()
			=> TestUtils.WriteMetricsToDisk();

		[ClassInitialize]
		public static void BeforeAll(TestContext ctx)
		{
			if (!(GambatteHexStrTestCase.KnownFailures.IsSortedAsc() && GambatteRefImageTestCase.KnownFailures.IsSortedAsc())) throw new Exception(SUITE_ID + " known-failing testcase list must be sorted");
			TestUtils.PrepareDBAndOutput(SUITE_ID);
		}

		private static (IReadOnlyList<GambatteRefImageTestCase> RefImageCases, IReadOnlyList<GambatteHexStrTestCase> HexStrCases) EnumerateAllCases()
		{
			var variants = new[] { ("_cgb04c.png", ConsoleVariant.CGB_C), ("_dmg08.png", ConsoleVariant.DMG) };
			static IReadOnlyList<(ConsoleVariant Variant, string ExpectValue)> ParseHexStrFilename(string filename)
			{
				List<(ConsoleVariant Variant, string Expect)> parsed = new();
				string? lastSeenValue = null;
				var endIndex = filename.LastIndexOf('.');
				while (true)
				{
					var i = filename.LastIndexOf('_', endIndex - 1);
					var seg = filename[(i + 1)..endIndex];
					if (seg == "cgb04c") parsed.Add((ConsoleVariant.CGB_C, lastSeenValue!));
					else if (seg == "dmg08") parsed.Add((ConsoleVariant.DMG, lastSeenValue!));
					else if (seg.StartsWith("out")) lastSeenValue = seg.SubstringAfter("out");
					else return parsed;
					endIndex = i;
				}
			}
			var allFilenames = ReflectionCache.EmbeddedResourceList(SUITE_PREFIX).ToList();
			List<GambatteRefImageTestCase> refImageCases = new();
			foreach (var filename in allFilenames.Where(static item => item.EndsWith(".png")).ToList())
			{
				var found = variants.FirstOrNull(kvp => filename.EndsWith(kvp.Item1));
				if (found is null) continue;
				var (suffix, variant) = found.Value;
				var testName = filename.RemoveSuffix(suffix);
				var romEmbedPath = $"{testName}.{(testName.StartsWith("dmgpalette_during_m3") ? "gb" : "gbc")}";
				foreach (var setup in CoreSetup.ValidSetupsFor(variant))
				{
					refImageCases.Add(new(testName, setup, SUITE_PREFIX + romEmbedPath, SUITE_PREFIX + filename));
				}
				allFilenames.Remove(filename);
				allFilenames.Remove(romEmbedPath);
			}
			var hexStrFilenames = allFilenames.Where(static s => HexStrFilenameRegex.IsMatch(s)).ToList();
			List<GambatteHexStrTestCase> hexStrCases = new();
			foreach (var hexStrFilename in hexStrFilenames)
			{
				var testName = hexStrFilename.SubstringBeforeLast('.');
				foreach (var (variant, expectValue) in ParseHexStrFilename(hexStrFilename)) foreach (var setup in CoreSetup.ValidSetupsFor(variant))
				{
					hexStrCases.Add(new(testName, setup, SUITE_PREFIX + hexStrFilename, expectValue));
				}
				allFilenames.Remove(hexStrFilename);
			}
//			Console.WriteLine($"unused files:\n>>>\n{string.Join("\n", allFilenames.OrderBy(static s => s))}\n<<<");
			return (refImageCases, hexStrCases);
		}

		[DataTestMethod]
		[GambatteHexStrTestData]
		public void RunGambatteHexStrTest(GambatteHexStrTestCase testCase)
		{
			static bool GlyphMatches(Bitmap b, int xOffset, byte v)
			{
				// `(xOffset, 0)` is the top-left of an 8x8 square of pixels to read from `b`, which is compared against the glyph for the nybble `v`
				bool GlyphRowMatches(int y)
				{
					byte rowAsByte = 0;
					for (int x = xOffset, l = x + 8; x < l; x++)
					{
						rowAsByte <<= 1;
						if ((b.GetPixel(x, y).ToArgb() & 0xFFFFFF) == 0) rowAsByte |= 1;
					}
					return rowAsByte == GLYPHS[y, v];
				}
				for (var y = 0; y < 8; y++) if (!GlyphRowMatches(y)) return false;
				return true;
			}
			TestUtils.ShortCircuitMissingRom(RomsArePresent);
			var caseStr = testCase.DisplayName();
			TestUtils.ShortCircuitKnownFailure(caseStr, GambatteHexStrTestCase.KnownFailures, out var knownFail);
			var actualUnnormalised = DummyFrontend.RunAndScreenshot(
				InitGBCore(testCase.Setup, testCase.RomEmbedPath.SubstringBeforeLast('.'), ReflectionCache.EmbeddedResourceStream(testCase.RomEmbedPath).ReadAllBytes()),
				static fe => fe.FrameAdvanceBy(11)).AsBitmap();
			var glyphCount = testCase.ExpectedValue.Length;
			var screenshotMatches = true;
			var i = 0;
			var xOffset = 0;
			while (i < glyphCount)
			{
				if (!GlyphMatches(actualUnnormalised, xOffset, byte.Parse(testCase.ExpectedValue[i..(i + 1)], NumberStyles.HexNumber)))
				{
					screenshotMatches = false;
					break;
				}
				i++;
				xOffset += 8;
			}
			var state = TestUtils.SuccessState(screenshotMatches, knownFail);
			if (!ImageUtils.SkipFileIO(state))
			{
				ImageUtils.SaveScreenshot(NormaliseGBScreenshot(actualUnnormalised, testCase.Setup), (SUITE_ID, caseStr));
				Console.WriteLine($"should read: {testCase.ExpectedValue}");
			}
			switch (state)
			{
				case TestUtils.TestSuccessState.ExpectedFailure:
					Assert.Inconclusive("expected failure, verified");
					break;
				case TestUtils.TestSuccessState.Failure:
					Assert.Fail("screenshot contains incorrect value");
					break;
				case TestUtils.TestSuccessState.UnexpectedSuccess:
					Assert.Fail("screenshot contains correct value unexpectedly (this is a good thing)");
					break;
			}
		}

		[DataTestMethod]
		[GambatteRefImageTestData]
		public void RunGambatteRefImageTest(GambatteRefImageTestCase testCase)
		{
			TestUtils.ShortCircuitMissingRom(RomsArePresent);
			var caseStr = testCase.DisplayName();
			TestUtils.ShortCircuitKnownFailure(caseStr, GambatteRefImageTestCase.KnownFailures, out var knownFail);
			var actualUnnormalised = DummyFrontend.RunAndScreenshot(
				InitGBCore(testCase.Setup, testCase.RomEmbedPath.SubstringBeforeLast('.'), ReflectionCache.EmbeddedResourceStream(testCase.RomEmbedPath).ReadAllBytes()),
				static fe => fe.FrameAdvanceBy(14));
			var state = GBScreenshotsEqual(
				ReflectionCache.EmbeddedResourceStream(testCase.ExpectEmbedPath),
				actualUnnormalised,
				knownFail,
				testCase.Setup,
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
