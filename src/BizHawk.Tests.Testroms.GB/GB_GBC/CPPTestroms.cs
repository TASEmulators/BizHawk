using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;

using BizHawk.Common.CollectionExtensions;
using BizHawk.Common.IOExtensions;

using static BizHawk.Tests.Testroms.GB.GBHelper;

namespace BizHawk.Tests.Testroms.GB.CPPTestroms
{
	[TestClass]
	public sealed class CPPTestroms
	{
		public readonly struct CPPTestromsHexStrTestCase
		{
			private static readonly IReadOnlyDictionary<string, string> ExpectedValues = new Dictionary<string, string>
			{
				["open-bus-ss-test"] = @"
					0A0A0A0A0A0A0A0A0A0A0A0A0A0A0A0A1A1A1A1A
					1A1A1A1A1A1A1A1A1A1A1A1A2A2A2A2A2A2A2A2A
					2A2A2A2A2A2A2A2A3A3A3A3A3A3A3A3A3A3A3A3A
					3A3A3A3A46464646464646464646464646464646
					4E4E4E4E4E4E4E4E4E4E4E4E4E4E4E4E56565656
					5656565656565656565656565E5E5E5E5E5E5E5E
					5E5E5E5E5E5E5E5E666666666666666666666666
					666666666E6E6E6E6E6E6E6E6E6E6E6E6E6E6E6E
					7E7E7E7E7E7E7E7E7E7E7E7E7E7E7E7E86868686
					8686868686868686868686868E8E8E8E8E8E8E8E
					8E8E8E8E8E8E8E8E969696969696969696969696
					969696969E9E9E9E9E9E9E9E9E9E9E9E9E9E9E9E
					A6A6A6A6A6A6A6A6A6A6A6A6A6A6A6A6AEAEAEAE
					AEAEAEAEAEAEAEAEAEAEAEAEB6B6B6B6B6B6B6B6
					B6B6B6B6B6B6B6B6BEBEBEBEBEBEBEBEBEBEBEBE
					BEBEBEBEA0A0A0A0A0A0A0A0A0A0A0A0A0A0A0A0
					C1FFC1FFC1FFC1FFC1FFC1FFC1FFC1FFD1FFD1FF
					D1FFD1FFD1FFD1FFD1FFD1FFE1FFE1FFE1FFE1FF
				".Replace("\t", "").Replace("\n", ""),
				["ramg-mbc3-test"] = @"
					00000000000000000000010000000000FFFFFFFF
					00000000000000000000010000000000FFFFFFFF
					00000000000000000000010000000000FFFFFFFF
					00000000000000000000010000000000FFFFFFFF
					00000000000000000000010000000000FFFFFFFF
					00000000000000000000010000000000FFFFFFFF
					00000000000000000000010000000000FFFFFFFF
					00000000000000000000010000000000FFFFFFFF
					00000000000000000000010000000000FFFFFFFF
					00000000000000000000010000000000FFFFFFFF
					00000000000000000000010000000000FFFFFFFF
					00000000000000000000010000000000FFFFFFFF
					00000000000000000000010000000000FFFFFFFF
					00000000000000000000010000000000FFFFFFFF
					00000000000000000000010000000000FFFFFFFF
					00000000000000000000010000000000FFFFFFFF
					FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF
					FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF
				".Replace("\t", "").Replace("\n", ""),
				["rtc-invalid-banks-test"] = @"
					00010203FFFFFFFF08090A0B00FFFFFF00000000
					00010203FFFFFFFF08090A0B00FFFFFF00000000
					00010203FFFFFFFF08090A0B00FFFFFF00000000
					00010203FFFFFFFF08090A0B00FFFFFF00000000
					00010203FFFFFFFF08090A0B00FFFFFF00000000
					00010203FFFFFFFF08090A0B00FFFFFF00000000
					00010203FFFFFFFF08090A0B00FFFFFF00000000
					00010203FFFFFFFF08090A0B00FFFFFF00000000
					00010203FFFFFFFF08090A0B00FFFFFF00000000
					00010203FFFFFFFF08090A0B00FFFFFF00000000
					00010203FFFFFFFF08090A0B00FFFFFF00000000
					00010203FFFFFFFF08090A0B00FFFFFF00000000
					00010203FFFFFFFF08090A0B00FFFFFF00000000
					00010203FFFFFFFF08090A0B00FFFFFF00000000
					00010203FFFFFFFF08090A0B00FFFFFF00000000
					00010203FFFFFFFF08090A0B00FFFFFF00000000
					0000000000000000000000000000000000000000
					0000000000000000000000000000000000000000
				".Replace("\t", "").Replace("\n", ""),
			};

			public static readonly IReadOnlyList<string> KnownFailures = new[]
			{
				"open-bus-ss-test on CGB_C in GBHawk",
				"open-bus-ss-test on DMG in GBHawk",
				"ramg-mbc3-test on CGB_C in GBHawk",
				"ramg-mbc3-test on DMG in GBHawk",
				"rtc-invalid-banks-test on CGB_C in GBHawk",
				"rtc-invalid-banks-test on DMG in Gambatte",
				"rtc-invalid-banks-test on DMG in Gambatte (no BIOS)",
				"rtc-invalid-banks-test on DMG in GBHawk",
				"rtc-invalid-banks-test on DMG in SameBoy",
				"rtc-invalid-banks-test on DMG in SameBoy (no BIOS)",
			};

			public readonly string ExpectedValue
				=> ExpectedValues[TestName];

			public readonly string RomEmbedPath
				=> $"{SUITE_PREFIX}{TestName}.gb";

			public readonly CoreSetup Setup;

			public readonly string TestName;

			public CPPTestromsHexStrTestCase(string testName, CoreSetup setup)
			{
				TestName = testName;
				Setup = setup;
			}

			public readonly string DisplayName()
				=> $"{TestName} on {Setup}";
		}

		[AttributeUsage(AttributeTargets.Method)]
		private sealed class CPPTestromsHexStrTestDataAttribute : Attribute, ITestDataSource
		{
			public IEnumerable<object?[]> GetData(MethodInfo methodInfo)
			{
				List<CPPTestromsHexStrTestCase> testCases = new();
				foreach (var setup in new[] { ConsoleVariant.CGB_C, ConsoleVariant.DMG }.SelectMany(CoreSetup.ValidSetupsFor))
				{
					testCases.Add(new("open-bus-ss-test", setup));
					testCases.Add(new("ramg-mbc3-test", setup));
					testCases.Add(new("rtc-invalid-banks-test", setup));
				}
//				testCases.RemoveAll(static testCase => testCase.Setup.Variant is not ConsoleVariant.DMG); // uncomment and modify to run a subset of the test cases...
				testCases.RemoveAll(static testCase => TestUtils.ShouldIgnoreCase(SUITE_ID, testCase.DisplayName())); // ...or use the global blocklist in TestUtils
				return testCases.OrderBy(static testCase => testCase.DisplayName())
					.Select(static testCase => new object?[] { testCase });
			}

			public string? GetDisplayName(MethodInfo methodInfo, object?[]? data)
				=> $"{methodInfo.Name}(\"{((CPPTestromsHexStrTestCase) data![0]!).DisplayName()}\")";
		}

		private static readonly byte[,] GLYPHS = {
			{ 0b0000, 0b0000, 0b0000, 0b0000, 0b0000, 0b0000, 0b0000, 0b0000, 0b0000, 0b0000, 0b0000, 0b0000, 0b0000, 0b0000, 0b0000, 0b0000 },
			{ 0b1110, 0b0100, 0b1110, 0b1110, 0b1010, 0b1110, 0b1110, 0b1110, 0b1110, 0b1110, 0b1110, 0b1100, 0b1110, 0b1100, 0b1110, 0b1110 },
			{ 0b1010, 0b1100, 0b1010, 0b0010, 0b1010, 0b1000, 0b1000, 0b1010, 0b1010, 0b1010, 0b1010, 0b1010, 0b1010, 0b1010, 0b1000, 0b1000 },
			{ 0b1010, 0b0100, 0b0010, 0b0110, 0b1110, 0b1110, 0b1110, 0b0010, 0b1110, 0b1110, 0b1110, 0b1110, 0b1000, 0b1010, 0b1100, 0b1100 },
			{ 0b1010, 0b0100, 0b1110, 0b0010, 0b0010, 0b0010, 0b1010, 0b0010, 0b1010, 0b0010, 0b1010, 0b1010, 0b1000, 0b1010, 0b1000, 0b1000 },
			{ 0b1010, 0b0100, 0b1000, 0b1010, 0b0010, 0b1010, 0b1010, 0b0010, 0b1010, 0b1010, 0b1010, 0b1010, 0b1010, 0b1010, 0b1000, 0b1000 },
			{ 0b1110, 0b1110, 0b1110, 0b1110, 0b0010, 0b1110, 0b1110, 0b0010, 0b1110, 0b1110, 0b1010, 0b1100, 0b1110, 0b1100, 0b1110, 0b1000 },
			{ 0b0000, 0b0000, 0b0000, 0b0000, 0b0000, 0b0000, 0b0000, 0b0000, 0b0000, 0b0000, 0b0000, 0b0000, 0b0000, 0b0000, 0b0000, 0b0000 },
		};

		private const string SUITE_ID = "CPPTestroms";

		private const string SUITE_PREFIX = "res.CasualPokePlayer_test_roms_artifact.";

		private static readonly IReadOnlyList<string> FilteredEmbedPaths = ReflectionCache.EmbeddedResourceList().Where(static s => s.StartsWith(SUITE_PREFIX)).ToList();

		[ClassCleanup]
		public static void AfterAll()
			=> TestUtils.WriteMetricsToDisk();

		[ClassInitialize]
		public static void BeforeAll(TestContext ctx)
		{
			if (!CPPTestromsHexStrTestCase.KnownFailures.IsSortedAsc()) throw new Exception(SUITE_ID + " known-failing testcase list must be sorted");
			TestUtils.PrepareDBAndOutput(SUITE_ID);
		}

		[DataTestMethod]
		[CPPTestromsHexStrTestData]
		public void RunCPPTestromsHexStrTest(CPPTestromsHexStrTestCase testCase)
		{
			static bool GlyphMatches(Bitmap b, int xOffset, int yOffset, byte v)
			{
				// `(xOffset, yOffset)` is the top-left of a 4x8 rectangle of pixels to read from `b`, which is compared against the glyph for the nybble `v`
				bool GlyphRowMatches(int y)
				{
					byte rowAsByte = 0;
					for (int x = xOffset, l = x + 4; x < l; x++)
					{
						rowAsByte <<= 1;
						if ((b.GetPixel(x, yOffset + y).ToArgb() & 0xFFFFFF) == 0) rowAsByte |= 1;
					}
					return rowAsByte == GLYPHS[y, v];
				}
				for (var y = 0; y < 8; y++) if (!GlyphRowMatches(y)) return false;
				return true;
			}
			TestUtils.ShortCircuitMissingRom(isPresent: FilteredEmbedPaths.Contains(testCase.RomEmbedPath));
			var caseStr = testCase.DisplayName();
			TestUtils.ShortCircuitKnownFailure(caseStr, CPPTestromsHexStrTestCase.KnownFailures, out var knownFail);
			var actualUnnormalised = DummyFrontend.RunAndScreenshot(
				InitGBCore(testCase.Setup, testCase.RomEmbedPath, ReflectionCache.EmbeddedResourceStream(testCase.RomEmbedPath).ReadAllBytes()),
				static fe => fe.FrameAdvanceBy(200)).AsBitmap();
			var glyphCount = testCase.ExpectedValue.Length;
			var screenshotMatches = true;
			var i = 0;
			var xOffset = 0;
			var yOffset = 0;
			while (i < glyphCount)
			{
				if (!GlyphMatches(actualUnnormalised, xOffset, yOffset, byte.Parse(testCase.ExpectedValue[i..(i + 1)], NumberStyles.HexNumber)))
				{
					screenshotMatches = false;
					break;
				}
				i++;
				xOffset += 4;
				if (xOffset is 160)
				{
					xOffset = 0;
					yOffset += 8;
				}
			}
			var state = TestUtils.SuccessState(screenshotMatches, knownFail);
			if (!ImageUtils.SkipFileIO(state))
			{
				ImageUtils.SaveScreenshot(NormaliseGBScreenshot(actualUnnormalised, testCase.Setup), (SUITE_ID, caseStr));
				Console.WriteLine("should read: ");
				const int STR_FOLD_COL = 40;
				for (var iEx = 0; iEx < testCase.ExpectedValue.Length; iEx += STR_FOLD_COL)
				{
					Console.WriteLine(testCase.ExpectedValue.Substring(startIndex: iEx, length: STR_FOLD_COL));
				}
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
	}
}
