using BizHawk.Client.Common;

namespace BizHawk.Tests.Client.Common.Movie
{
	[TestClass]
	public class MovieUndoTests
	{
		// Our test looks for the first actually different frame and compares with the value returned by Undo.
		// Some operations (e.g. RemoveFrames) don't check for which frames were actually edited. (Should they? Would that give bad performance?)
		// So we should ensure the first frame we touch is actually changed.
		private void ValidateActionCanUndoAndRedo(ITasMovie movie, Action action, int expectedUndoItems = 1, bool skipUndoIndexCheck = false)
		{
			IStringLog originalLog = movie.GetLogEntries().Clone();
			int originalUndoLength = movie.ChangeLog.UndoIndex;
			action();

			IStringLog changedLog = movie.GetLogEntries().Clone();
			int changedUndoLength = movie.ChangeLog.UndoIndex;
			int firstEditedFrame = originalLog.DivergentPoint(changedLog) ?? movie.InputLogLength;

			if (!skipUndoIndexCheck)
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
			ITasMovie movie = TasMovieTests.MakeMovie(5);

			ValidateActionCanUndoAndRedo(movie, () =>
			{
				movie.SetBoolState(2, "A", true);
			});
		}

		[TestMethod]
		public void ExtendsMovie()
		{
			ITasMovie movie = TasMovieTests.MakeMovie(5);

			ValidateActionCanUndoAndRedo(movie, () =>
			{
				movie.SetBoolState(8, "A", true);
			});
			movie.ChangeLog.Undo();
			ValidateActionCanUndoAndRedo(movie, () =>
			{
				movie.SetBoolStates(8, 2, "A", true);
			});
			movie.ChangeLog.Undo();
			ValidateActionCanUndoAndRedo(movie, () =>
			{
				movie.SetAxisState(8, "Stick", 10);
			});
			movie.ChangeLog.Undo();
			ValidateActionCanUndoAndRedo(movie, () =>
			{
				movie.SetAxisStates(8, 2, "Stick", 10);
			});
			movie.ChangeLog.Undo();
		}

		[TestMethod]
		public void SetAxis()
		{
			ITasMovie movie = TasMovieTests.MakeMovie(5);

			ValidateActionCanUndoAndRedo(movie, () =>
			{
				movie.SetAxisState(2, "Stick", 20);
			});
		}

		[TestMethod]
		public void InsertFrame()
		{
			ITasMovie movie = TasMovieTests.MakeMovie(5);
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
			ITasMovie movie = TasMovieTests.MakeMovie(5);
			movie.SetBoolState(2, "A", true);
			movie.SetBoolState(4, "B", true);

			ValidateActionCanUndoAndRedo(movie, () =>
			{
				movie.RemoveFrame(3);
			});
		}

		[TestMethod]
		public void DeleteFrames()
		{
			ITasMovie movie = TasMovieTests.MakeMovie(10);
			movie.SetBoolState(2, "A", true);
			movie.SetBoolState(4, "B", true);

			// both overloads
			ValidateActionCanUndoAndRedo(movie, () =>
			{
				movie.RemoveFrames(1, 4);
			});
			movie.ChangeLog.Undo();

			ValidateActionCanUndoAndRedo(movie, () =>
			{
				movie.RemoveFrames([ 1, 2, 3, 5 ]);
			});
		}

		[TestMethod]
		public void CloneFrame()
		{
			ITasMovie movie = TasMovieTests.MakeMovie(5);
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
			ITasMovie movie = TasMovieTests.MakeMovie(5);

			ValidateActionCanUndoAndRedo(movie, () =>
			{
				movie.SetBoolState(2, "A", true);
				movie.SetBoolState(3, "B", true);
			}, 2);
		}

		[TestMethod]
		public void BatchedEdit()
		{
			ITasMovie movie = TasMovieTests.MakeMovie(5);

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
			ITasMovie movie = TasMovieTests.MakeMovie(5);

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
			ITasMovie movie = TasMovieTests.MakeMovie(5);

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
			ITasMovie movie = TasMovieTests.MakeMovie(5);

			ValidateActionCanUndoAndRedo(movie, () =>
			{
				Bk2Controller controller = new Bk2Controller(movie.Emulator.ControllerDefinition);
				controller.SetBool("A", true);
				movie.RecordFrame(0, controller);
			});
		}

		[TestMethod]
		public void MarkersGetMoved()
		{
			ITasMovie movie = TasMovieTests.MakeMovie(5);
			movie.BindMarkersToInput = true;
			movie.Markers.Add(3, "a");
			movie.InsertEmptyFrame(2);

			movie.ChangeLog.Undo();
			Assert.AreEqual(3, movie.Markers[0].Frame);

			movie.ChangeLog.Redo();
			Assert.AreEqual(4, movie.Markers[0].Frame);
		}

		[TestMethod]
		public void MarkersGetUndeleted()
		{
			ITasMovie movie = TasMovieTests.MakeMovie(5);
			movie.BindMarkersToInput = true;
			movie.Markers.Add(3, "a");
			movie.RemoveFrame(3);

			movie.ChangeLog.Undo();
			Assert.AreEqual(1, movie.Markers.Count);
			Assert.AreEqual(3, movie.Markers[0].Frame);
			Assert.AreEqual("a", movie.Markers[0].Message);

			movie.ChangeLog.Redo();
			Assert.AreEqual(0, movie.Markers.Count);
		}

		[TestMethod]
		public void MarkersUnaffectedByMovieExtension()
		{
			ITasMovie movie = TasMovieTests.MakeMovie(5);
			movie.BindMarkersToInput = true;
			movie.Markers.Add(5, "a");
			movie.Markers.Add(8, "b");
			movie.SetBoolState(10, "A", true);

			movie.ChangeLog.Undo();
			Assert.AreEqual(2, movie.Markers.Count);
			Assert.AreEqual(5, movie.Markers[0].Frame);
			Assert.AreEqual(8, movie.Markers[1].Frame);

			movie.ChangeLog.Redo();
			Assert.AreEqual(2, movie.Markers.Count);
			Assert.AreEqual(5, movie.Markers[0].Frame);
			Assert.AreEqual(8, movie.Markers[1].Frame);
		}

		[TestMethod]
		public void AllOperationsRespectBatching()
		{
			ITasMovie movie = TasMovieTests.MakeMovie(10);

			// Some actions can move markers.
			movie.Markers.Add(9, "");
			movie.BindMarkersToInput = true;

			Bk2Controller controllerA = new Bk2Controller(movie.Emulator.ControllerDefinition);
			controllerA.SetBool("A", true);
			string entryA = Bk2LogEntryGenerator.GenerateLogEntry(controllerA);

			int beginIndex = 0;
			TasMovieTests.TestAllOperations(movie,
				() =>
				{
					beginIndex = movie.ChangeLog.UndoIndex;
					movie.ChangeLog.BeginNewBatch();
				},
				() =>
				{
					movie.SetFrame(0, entryA);
					movie.ChangeLog.EndBatch();

					Assert.AreEqual(1, movie.ChangeLog.UndoIndex - beginIndex);
				});
		}

		[TestMethod]
		public void AllOperationsGiveOneUndo()
		{
			ITasMovie movie = TasMovieTests.MakeMovie(10);

			// Some actions can move markers.
			movie.Markers.Add(9, "");
			movie.BindMarkersToInput = true;

			int beginIndex = 0;
			TasMovieTests.TestAllOperations(movie,
				() => beginIndex = movie.ChangeLog.UndoIndex,
				() => Assert.AreEqual(1, movie.ChangeLog.UndoIndex - beginIndex)
			);
		}

		[TestMethod]
		public void WorkWithFullUndoHistory()
		{
			ITasMovie movie = TasMovieTests.MakeMovie(5);
			movie.ChangeLog.MaxSteps = 3;

			movie.SetBoolState(0, "A", true);
			movie.SetBoolState(1, "A", true);
			movie.SetBoolState(2, "A", true);

			ValidateActionCanUndoAndRedo(movie, () =>
			{
				movie.SetBoolState(10, "A", true);
			}, 1, true);
		}

		[TestMethod]
		public void UndoingMidBatchRetainsBatchState()
		{
			ITasMovie movie = TasMovieTests.MakeMovie(8);
			movie.ChangeLog.BeginNewBatch();

			movie.SetBoolState(1, "A", true);
			movie.SetBoolState(2, "A", true);
			movie.ChangeLog.Undo();

			ValidateActionCanUndoAndRedo(movie, () =>
			{
				movie.SetBoolState(3, "A", true);
				movie.SetBoolState(4, "A", true);

				movie.ChangeLog.EndBatch();
			}, 1, true);

			Assert.AreEqual(1, movie.ChangeLog.UndoIndex);
		}

		[TestMethod]
		public void UndoRedoProduceSingleInvalidation()
		{
			ITasMovie movie = TasMovieTests.MakeMovie(5);
			int invalidations = 0;
			movie.GreenzoneInvalidated = (_) => invalidations++;

			movie.ChangeLog.BeginNewBatch();
			movie.SetBoolState(1, "A", true);
			movie.SetBoolState(2, "B", true);
			movie.ChangeLog.EndBatch();

			invalidations = 0;
			movie.ChangeLog.Undo();
			Assert.AreEqual(1, invalidations);

			invalidations = 0;
			movie.ChangeLog.Redo();
			Assert.AreEqual(1, invalidations);
		}

		[TestMethod]
		public void InsertRespectsMarkerBinding()
		{
			ITasMovie movie = TasMovieTests.MakeMovie(5);
			movie.Markers.Add(3, "a");

			movie.BindMarkersToInput = true;
			movie.InsertEmptyFrame(1);
			movie.BindMarkersToInput = false;

			movie.ChangeLog.Undo();
			Assert.AreEqual(3, movie.Markers[0].Frame);
			Assert.IsFalse(movie.BindMarkersToInput);

			movie.ChangeLog.Redo();
			Assert.AreEqual(4, movie.Markers[0].Frame);
			Assert.IsFalse(movie.BindMarkersToInput);
		}

		[TestMethod]
		public void DeleteRespectsMarkerBinding()
		{
			ITasMovie movie = TasMovieTests.MakeMovie(5);
			movie.Markers.Add(3, "a");

			movie.BindMarkersToInput = true;
			movie.RemoveFrame(1);
			movie.BindMarkersToInput = false;

			movie.ChangeLog.Undo();
			Assert.AreEqual(3, movie.Markers[0].Frame);
			Assert.IsFalse(movie.BindMarkersToInput);

			movie.ChangeLog.Redo();
			Assert.AreEqual(2, movie.Markers[0].Frame);
			Assert.IsFalse(movie.BindMarkersToInput);
		}

		[TestMethod]
		public void GeneralRespectsMarkerBinding()
		{
			// This was just a silly bug.
			ITasMovie movie = TasMovieTests.MakeMovie(5);
			movie.Markers.Add(3, "a");

			Bk2Controller controllerA = new Bk2Controller(movie.Emulator.ControllerDefinition);
			controllerA.SetBool("A", true);

			movie.BindMarkersToInput = true;
			movie.PokeFrame(1, controllerA);
			movie.BindMarkersToInput = false;
			movie.InsertEmptyFrame(1);

			movie.ChangeLog.Undo();
			movie.ChangeLog.Undo();
			Assert.AreEqual(3, movie.Markers[0].Frame);
			Assert.IsFalse(movie.BindMarkersToInput);

			movie.ChangeLog.Redo();
			movie.ChangeLog.Redo();
			Assert.AreEqual(3, movie.Markers[0].Frame);
			Assert.IsFalse(movie.BindMarkersToInput);
		}
	}
}
