using BizHawk.Client.Common;

namespace BizHawk.Tests.Client.Common.Movie
{
	[TestClass]
	public class MovieServiceTests
	{
		[TestMethod]
		[DataRow(null, 1.0)]
		[DataRow("", 1.0)]
		[DataRow(" ", 1.0)]
		[DataRow("NonsenseString", 1.0)]
		[DataRow("BizHawk v2.0", 1.0)]
		[DataRow("BizHawk v2.0 Tasproj v1.0", 1.0)]
		[DataRow("BizHawk v2.0 Tasproj v1.1", TasMovie.CurrentVersion)]
		public void ParseTasMovieVersion(string movieVersion, double expected)
		{
			var actual = MovieService.ParseTasMovieVersion(movieVersion);
			Assert.AreEqual(expected, actual);
		}
	}
}
