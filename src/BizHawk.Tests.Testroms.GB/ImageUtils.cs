using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.PathExtensions;

using ImageMagick;

namespace BizHawk.Tests.Testroms.GB
{
	public static class ImageUtils
	{
		public static Bitmap AsBitmap(this Image img)
			=> img as Bitmap ?? new Bitmap(img, img.Size);

		/// <param name="fileExt">w/o leading '.'</param>
		private static (string Expect, string Actual, string Glob) GenFilenames((string Suite, string Case) id, string fileExt = "png")
		{
			var prefix = $"{id.Suite}/{id.Case.GetHashCode():X8}"; // hashcode of string sadly not stable
			var suffix = $"{id.Case.RemoveInvalidFileSystemChars().Replace(' ', '_').Replace("(no BIOS)", "noBIOS")}.{fileExt}";
			return ($"{prefix}_expect_{suffix}", $"{prefix}_actual_{suffix}", $"{prefix}_*_{suffix}");
		}

		public static int GetRawPixel(this Bitmap b, int x, int y)
			=> b.GetPixel(x, y).ToArgb() & 0xFFFFFF;

		/// <param name="map">ints are ARGB as <see cref="System.Drawing.Color.ToArgb"/></param>
		public static Bitmap PaletteSwap(Image img, IReadOnlyDictionary<int, int> map)
		{
			int Lookup(int c)
				=> map.TryGetValue(c, out var c1) ? c1 : c;
			var b = ((Image) img.Clone()).AsBitmap();
			for (int y = 0, ly = b.Height; y < ly; y++) for (int x = 0, lx = b.Width; x < lx; x++)
			{
				b.SetPixel(x, y, Color.FromArgb(0xFF, Color.FromArgb(Lookup(b.GetRawPixel(x, y)))));
			}
			return b;
		}

		public static void PrintPalette(Image imgA, string labelA, Image imgB, string labelB)
		{
			static HashSet<int> CollectPalette(Image img)
			{
				var b = img.AsBitmap();
				HashSet<int> paletteE = new();
				for (int y = 0, ly = b.Height; y < ly; y++) for (int x = 0, lx = b.Width; x < lx; x++)
				{
					paletteE.Add(b.GetRawPixel(x, y));
				}
				return paletteE;
			}
			static string F(Image img)
				=> string.Join(", ", CollectPalette(img).Select(static i => $"{i:X6}"));
			Console.WriteLine($"palette of {labelA}:\n{F(imgA)}\npalette of {labelB}:\n{F(imgB)}");
		}

		public static void SaveScreenshot(Image img, (string Suite, string Case) id)
		{
			var filename = GenFilenames(id).Actual;
			img.ToMagickImage().Write(filename, MagickFormat.Png);
			Console.WriteLine($"screenshot saved for {id.Case} as {filename}");
		}

		/// <remarks>initially added this as a workaround for various bugs in <c>System.Drawing.*</c> on Linux, but this also happens to be faster on Windows</remarks>
		public static TestUtils.TestSuccessState ScreenshotsEqualMagickDotNET(Stream expectFile, Image actual, bool expectingNotEqual, (string Suite, string Case) id)
		{
			var actualIM = actual.ToMagickImage();
			MagickImage expectIM = new(expectFile);
			var error = expectIM.Compare(actualIM, ErrorMetric.Absolute);
			var state = TestUtils.SuccessState(error == 0.0, expectingNotEqual);
			if (!SkipFileIO(state))
			{
				var (filenameExpect, filenameActual, filenameGlob) = GenFilenames(id);
				actualIM.Write(filenameActual, MagickFormat.Png);
				expectIM.Write(filenameExpect, MagickFormat.Png);
				Console.WriteLine($"screenshots saved for {id.Case} as {filenameGlob} (difference: {error})");
			}
			return state;
		}

		public static bool SkipFileIO(TestUtils.TestSuccessState state)
#if SAVE_IMAGES_ON_FAIL && SAVE_IMAGES_ON_PASS // run with env. var BIZHAWKTEST_SAVE_IMAGES=all
			=> false;
#elif SAVE_IMAGES_ON_FAIL // run without extra env. var, or with env. var BIZHAWKTEST_SAVE_IMAGES=failures
			=> state is TestUtils.TestSuccessState.Success;
#elif SAVE_IMAGES_ON_PASS // normally inaccessible
			=> state is not TestUtils.TestSuccessState.Success;
#else // run with env. var BIZHAWKTEST_SAVE_IMAGES=none
			=> true;
#endif

		public static MagickImage ToMagickImage(this Image img)
		{
			MemoryStream ms = new();
			img.Save(ms, OSTailoredCode.IsUnixHost ? ImageFormat.Bmp : ImageFormat.Png);
			ms.Position = 0;
			return new(ms);
		}
	}
}
