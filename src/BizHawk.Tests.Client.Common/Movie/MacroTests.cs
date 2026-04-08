using System.IO;

using BizHawk.Client.Common;

namespace BizHawk.Tests.Client.Common.Movie
{
	[TestClass]
	public class MacroTests
	{
		private static readonly string BASE_MACRO_PATH = Path.Combine(Environment.CurrentDirectory, "Movie/macros");

		[TestMethod]
		public void TestButton()
		{
			// arrange
			ITasMovie movie = TasMovieTests.MakeMovie(2);
			movie.SetBoolState(0, "A", true);
			movie.SetBoolState(1, "B", true);
			MovieZone macro = new(movie, 0, 1);

			// act
			macro.PlaceZone(movie, 1);

			// assert
			Assert.AreEqual(movie.GetInputLogEntry(0), movie.GetInputLogEntry(1));
		}

		[TestMethod]
		public void TestAxis()
		{
			// arrange
			ITasMovie movie = TasMovieTests.MakeMovie(2);
			movie.SetAxisState(0, "Stick", 7);
			MovieZone macro = new(movie, 0, 1);

			// act
			macro.PlaceZone(movie, 1);

			// assert
			Assert.AreEqual(movie.GetInputLogEntry(0), movie.GetInputLogEntry(1));
		}

		[TestMethod]
		public void TestMacroWithSubsetOfInputs()
		{
			// arrange
			ITasMovie movie = TasMovieTests.MakeMovie(1);
			movie.SetBoolState(0, "B", true);
			movie.SetAxisState(0, "Stick", 7);
			string expectedFrame = movie.GetInputLogEntry(0);

			MovieZone macro = new(Path.Combine(BASE_MACRO_PATH, "MacroWithJustANotPressed.bk2m"), null!, movie);
			movie.SetBoolState(0, "A", true);

			// act
			macro.PlaceZone(movie, 0);

			// assert
			Assert.AreEqual(expectedFrame, movie.GetInputLogEntry(0));
		}

		[TestMethod]
		public void MacroCanExtendMovie()
		{
			// arrange
			ITasMovie movie = TasMovieTests.MakeMovie(2);
			MovieZone macro = new(movie, 0, 2);

			// act
			macro.PlaceZone(movie, 1);

			// assert
			Assert.AreEqual(3, movie.InputLogLength);
		}

		[TestMethod]
		public void TestOverlay()
		{
			// arrange
			ITasMovie movie = TasMovieTests.MakeMovie(1);
			movie.SetBoolState(0, "A", true);
			MovieZone macro = new(movie, 0, 1);
			macro.Overlay = true;

			movie.SetBoolState(0, "B", true);
			movie.SetAxisState(0, "Stick", 7);
			string expectedFrame = movie.GetInputLogEntry(0);
			movie.SetBoolState(0, "A", false);

			// act
			macro.PlaceZone(movie, 0);

			// assert
			Assert.AreEqual(expectedFrame, movie.GetInputLogEntry(0));
		}

		[TestMethod]
		public void TestInsert()
		{
			// arrange
			ITasMovie movie = TasMovieTests.MakeMovie(1);
			MovieZone macro = new(movie, 0, 1);
			macro.Replace = false;

			// act
			macro.PlaceZone(movie, 0);

			// assert
			Assert.AreEqual(2, movie.InputLogLength);
		}
	}
}
