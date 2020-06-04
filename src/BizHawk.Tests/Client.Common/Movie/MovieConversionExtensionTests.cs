using Microsoft.VisualStudio.TestTools.UnitTesting;
using BizHawk.Client.Common.MovieConversionExtensions;

namespace BizHawk.Common.Tests.Client.Common.Movie
{
	[TestClass]
	public class MovieConversionExtensionTests
	{
		private string oldBk2FileName = "C:\\Temp\\TestMovie.bk2";

		[TestMethod]
		public void GetNewFileNameFromBk2()
		{
			var actual = MovieConversionExtensions.GetNewFileName(oldBk2FileName);
			Assert.AreEqual("C:\\Temp\\TestMovie.tasproj", actual);
		}
	}
}