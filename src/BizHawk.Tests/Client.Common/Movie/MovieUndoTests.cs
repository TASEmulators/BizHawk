using BizHawk.Client.Common;
using BizHawk.Tests.Emulation.Common;

namespace BizHawk.Tests.Client.Common.Movie
{
	[TestClass]
	public class MovieUndoTests
	{
		private TasMovie MakeMovie(int numberOfFrames)
		{
			FakeMovieSession session = new() { Movie = null! };
			TasMovie movie = new(session, "/fake/path");
			session.Movie = movie;

			movie.Attach(new FakeEmulator());
			movie.InsertEmptyFrame(0, numberOfFrames);

			return movie;
		}

		private void ValidateActionCanUndoAndRedo(TasMovie movie, Action action, int expectedUndoItems = 1)
		{
			IStringLog originalLog = movie.GetLogEntries().Clone();
			int originalUndoLength = movie.ChangeLog.UndoIndex;
			action();

			IStringLog changedLog = movie.GetLogEntries().Clone();
			int changedUndoLength = movie.ChangeLog.UndoIndex;
			int firstEditedFrame = originalLog.DivergentPoint(changedLog) ?? movie.InputLogLength;

			Assert.AreEqual(originalUndoLength + expectedUndoItems, changedUndoLength);

			// undo
			int undoFrame = int.MaxValue;
			for (int i = 0; i < expectedUndoItems; i++)
				undoFrame = Math.Min(undoFrame, movie.ChangeLog.Undo());
			Assert.AreEqual(firstEditedFrame, undoFrame);
			Assert.IsNull(originalLog.DivergentPoint(movie.GetLogEntries()));

			// redo
			int redoFrame = int.MaxValue;
			for (int i = 0; i < expectedUndoItems; i++)
				redoFrame = Math.Min(redoFrame, movie.ChangeLog.Redo());
			Assert.AreEqual(firstEditedFrame, redoFrame);
			Assert.IsNull(changedLog.DivergentPoint(movie.GetLogEntries()));
		}

		[TestMethod]
		public void SetBool()
		{
			TasMovie movie = MakeMovie(5);

			ValidateActionCanUndoAndRedo(movie, () =>
			{
				movie.SetBoolState(2, "A", true);
			});
		}

		[TestMethod]
		public void SetAxis()
		{
			TasMovie movie = MakeMovie(5);

			ValidateActionCanUndoAndRedo(movie, () =>
			{
				movie.SetAxisState(2, "Stick", 20);
			});
		}

		[TestMethod]
		public void InsertFrame()
		{
			TasMovie movie = MakeMovie(5);
			movie.SetBoolState(2, "A", true);
			movie.SetBoolState(3, "B", true);

			ValidateActionCanUndoAndRedo(movie, () =>
			{
				movie.InsertEmptyFrame(3);
			});
		}

		[TestMethod]
		public void DeleteFrame()
		{
			TasMovie movie = MakeMovie(5);
			movie.SetBoolState(2, "A", true);
			movie.SetBoolState(4, "B", true);

			ValidateActionCanUndoAndRedo(movie, () =>
			{
				movie.RemoveFrame(3);
			});
		}

		[TestMethod]
		public void CloneFrame()
		{
			TasMovie movie = MakeMovie(5);
			movie.SetBoolState(2, "A", true);
			movie.SetBoolState(3, "B", true);

			ValidateActionCanUndoAndRedo(movie, () =>
			{
				movie.InsertInput(2, movie.GetInputLogEntry(3));
			});
		}

		[TestMethod]
		public void MultipleEdits()
		{
			TasMovie movie = MakeMovie(5);

			ValidateActionCanUndoAndRedo(movie, () =>
			{
				movie.SetBoolState(2, "A", true);
				movie.SetBoolState(3, "B", true);
			}, 2);
		}

		[TestMethod]
		public void BatchedEdit()
		{
			TasMovie movie = MakeMovie(5);

			ValidateActionCanUndoAndRedo(movie, () =>
			{
				movie.ChangeLog.BeginNewBatch();
				movie.SetBoolState(2, "A", true);
				movie.SetBoolState(3, "B", true);
				movie.ChangeLog.EndBatch();
			});
		}

		[TestMethod]
		public void RecordFrameAtEnd()
		{
			TasMovie movie = MakeMovie(5);

			ValidateActionCanUndoAndRedo(movie, () =>
			{
				Bk2Controller controller = new Bk2Controller(movie.Emulator.ControllerDefinition);
				controller.SetBool("A", true);
				movie.RecordFrame(5, controller);
			});
		}

		[TestMethod]
		public void RecordFrameInMiddle()
		{
			TasMovie movie = MakeMovie(5);

			ValidateActionCanUndoAndRedo(movie, () =>
			{
				Bk2Controller controller = new Bk2Controller(movie.Emulator.ControllerDefinition);
				controller.SetBool("A", true);
				movie.RecordFrame(2, controller);
			});
		}

		[TestMethod]
		public void RecordFrameZero()
		{
			TasMovie movie = MakeMovie(5);

			ValidateActionCanUndoAndRedo(movie, () =>
			{
				Bk2Controller controller = new Bk2Controller(movie.Emulator.ControllerDefinition);
				controller.SetBool("A", true);
				movie.RecordFrame(0, controller);
			});
		}
	}
}
