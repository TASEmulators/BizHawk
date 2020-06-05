using Microsoft.VisualStudio.TestTools.UnitTesting;
using BizHawk.Client.Common.MovieConversionExtensions;

namespace BizHawk.Common.Tests.Client.Common.Movie
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
		public void GetNewFileNameFromBk2(string original, string expected)
		{
			var actual = MovieConversionExtensions.GetNewFileName(original);
			Assert.AreEqual(expected, actual);
		}
	}
}