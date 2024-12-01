using BizHawk.Client.Common;

namespace BizHawk.Tests.Client.Common.Movie
{
	[TestClass]
	public class MovieConversionExtensionTests
	{
		[TestMethod]
		[DataRow(null, null)]
		[DataRow("", "")]
		[DataRow("C:\\Temp\\TestMovie.bk2", "C:\\Temp\\TestMovie.tasproj")]
		[DataRow("C:\\Temp\\TestMovie.tasproj.bk2", "C:\\Temp\\TestMovie.tasproj.tasproj")]
		[DataRow("C:\\Temp\\TestMovie.tasproj", "C:\\Temp\\TestMovie.tasproj")]
		public void ConvertFileNameToTasMovie(string original, string expected)
		{
			PlatformTestUtils.OnlyRunOnWindows();

			var actual = MovieConversionExtensions.ConvertFileNameToTasMovie(original);
			Assert.AreEqual(expected, actual);
		}
	}
}