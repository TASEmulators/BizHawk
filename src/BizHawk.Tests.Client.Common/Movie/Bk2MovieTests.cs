using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Tests.Client.Common.Movie
{
	[TestClass]
	public class Bk2MovieTests
	{
		internal static Bk2Movie MakeMovie(int numberOfFrames)
		{
			FakeEmulator emu = new FakeEmulator();
			FakeMovieSession session = new(emu);
			Bk2Movie movie = new(session, "/fake/path");
			session.Movie = movie;

			movie.Attach(emu);
			for (int i = 0; i < numberOfFrames; i++)
				movie.RecordFrame(i, session.MovieController);

			return movie;
		}

		public static void SetSingleActiveInput(IMovie movie, string button)
		{
			ControllerDefinition activeInputs = new("single");
			activeInputs.BoolButtons.Add(button);
			movie.ActiveControllerInputs = activeInputs.MakeImmutable();
		}

		[TestMethod]
		public void GetInputReturnsDifferentInstances()
		{
			// arrange
			Bk2Movie movie = MakeMovie(2);
			IMovieController controller = movie.Session.MovieController;
			controller.SetBool("A", true);
			movie.PokeFrame(0, controller);

			// act
			IController frame0 = movie.GetInputState(0);
			IController frame1 = movie.GetInputState(1);

			// assert
			Assert.AreNotEqual(frame0, frame1);
			Assert.IsTrue(frame0.IsPressed("A"));
			Assert.IsFalse(frame1.IsPressed("A"));
		}

		[TestMethod]
		public void MultitrackRecordFrame()
		{
			// arrange
			Bk2Movie movie = MakeMovie(2);
			SetSingleActiveInput(movie, "A");
			IMovieController controller = movie.Session.MovieController;
			controller.SetBool("A", true);
			controller.SetBool("B", true);

			// act
			movie.RecordFrame(0, controller);

			// assert
			Assert.IsTrue(movie.GetInputState(0).IsPressed("A"));
			Assert.IsFalse(movie.GetInputState(0).IsPressed("B"));
		}

		[TestMethod]
		public void MultitrackPokeFrame()
		{
			// arrange
			Bk2Movie movie = MakeMovie(2);
			SetSingleActiveInput(movie, "A");
			IMovieController controller = movie.Session.MovieController;
			controller.SetBool("A", true);
			controller.SetBool("B", true);

			// act
			movie.PokeFrame(0, controller);

			// assert
			Assert.IsTrue(movie.GetInputState(0).IsPressed("A"));
			Assert.IsFalse(movie.GetInputState(0).IsPressed("B"));
		}

		[TestMethod]
		public void MultitrackAppendFrame()
		{
			// arrange
			Bk2Movie movie = MakeMovie(2);
			SetSingleActiveInput(movie, "A");
			IMovieController controller = movie.Session.MovieController;
			controller.SetBool("A", true);
			controller.SetBool("B", true);

			// act
			movie.AppendFrame(controller);

			// assert
			Assert.IsTrue(movie.GetInputState(2).IsPressed("A"));
			Assert.IsFalse(movie.GetInputState(2).IsPressed("B"));
		}

		[TestMethod]
		public void MultitrackTruncateFrameWithoutLengthChange()
		{
			// arrange
			Bk2Movie movie = MakeMovie(2);
			IMovieController controller = movie.Session.MovieController;
			controller.SetBool("A", true);
			controller.SetBool("B", true);
			movie.PokeFrame(1, controller);

			SetSingleActiveInput(movie, "A");

			// act
			movie.Truncate(1);

			// assert
			Assert.IsFalse(movie.GetInputState(1).IsPressed("A"));
			Assert.IsTrue(movie.GetInputState(1).IsPressed("B"));
			Assert.AreEqual(2, movie.FrameCount);
		}

		[TestMethod]
		public void MultitrackTruncateFrameWithLengthChange()
		{
			// arrange
			Bk2Movie movie = MakeMovie(2);
			SetSingleActiveInput(movie, "A");

			// act
			movie.Truncate(1);

			// assert
			Assert.AreEqual(1, movie.FrameCount);
		}
	}
}
