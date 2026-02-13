using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Tests.Client.Common.Movie
{
	[TestClass]
	public class TasMovieTests
	{
		internal static TasMovie MakeMovie(int numberOfFrames)
			=> MakeMovie(numberOfFrames, out _);

		internal static TasMovie MakeMovie(int numberOfFrames, out IEmulator emu)
		{
			emu = new FakeEmulator();
			FakeMovieSession session = new(emu);
			TasMovie movie = new(session, "/fake/path");
			session.Movie = movie;

			movie.Attach(emu);
			movie.InsertEmptyFrame(0, numberOfFrames);

			return movie;
		}

		public static void TestAllOperations(ITasMovie movie, IEmulator emulator, Action PreOperation, Action PostOperation)
		{
			Bk2Controller controllerEmpty = new Bk2Controller(emulator.ControllerDefinition);
			string entryEmpty = Bk2LogEntryGenerator.GenerateLogEntry(controllerEmpty);
			Bk2Controller controllerA = new Bk2Controller(emulator.ControllerDefinition);
			controllerA.SetBool("A", true);
			string entryA = Bk2LogEntryGenerator.GenerateLogEntry(controllerA);

			// Make sure all operations actually do something.
			movie.SetBoolState(3, "A", true);

			void TestForSingleOperation(Action a)
			{
				PreOperation();
				a();
				PostOperation();
				movie.ChangeLog.Undo();
			}

			TestForSingleOperation(() => movie.RecordFrame(targetFrame: 1, currentFrame: emulator.Frame, controllerA));
			TestForSingleOperation(() => movie.Truncate(3));
			TestForSingleOperation(() => movie.PokeFrame(1, controllerA));
			TestForSingleOperation(() => movie.SetFrame(1, entryA));
			TestForSingleOperation(() => movie.ClearFrame(3));

			TestForSingleOperation(() => movie.InsertInput(2, entryA));
			TestForSingleOperation(() => movie.InsertInput(2, [ entryA, entryEmpty, entryA, entryEmpty ]));
			TestForSingleOperation(() => movie.InsertInput(2, [ controllerA, controllerEmpty, controllerA, controllerEmpty ]));

			TestForSingleOperation(() => movie.RemoveFrame(2));
			TestForSingleOperation(() => movie.RemoveFrames(2, 4));
			TestForSingleOperation(() => movie.RemoveFrames([ 2, 4, 6 ]));

			TestForSingleOperation(() => movie.CopyOverInput(2, [ controllerA, controllerEmpty ]));
			TestForSingleOperation(() => movie.InsertEmptyFrame(2, 2));

			TestForSingleOperation(() => movie.ToggleBoolState(2, "B"));
			TestForSingleOperation(() => movie.SetBoolState(3, "B", true));
			TestForSingleOperation(() => movie.SetBoolStates(3, 2, "B", true));

			TestForSingleOperation(() => movie.SetAxisState(2, "Stick", 10));
			TestForSingleOperation(() => movie.SetAxisStates(3, 2, "Stick", 20));

			// actions that can also extend the movie
			TestForSingleOperation(() => movie.CopyOverInput(9, [ controllerA, controllerEmpty, controllerA ]));
			TestForSingleOperation(() => movie.ToggleBoolState(15, "B"));
			TestForSingleOperation(() => movie.SetBoolState(15, "B", true));
			TestForSingleOperation(() => movie.SetBoolStates(15, 2, "B", true));
			TestForSingleOperation(() => movie.SetAxisState(15, "Stick", 10));
			TestForSingleOperation(() => movie.SetAxisStates(15, 2, "Stick", 20));
		}

#pragma warning disable BHI1600 //TODO disambiguate assert calls
		[TestMethod]
		public void AllOperationsFlagChanges()
		{
			ITasMovie movie = MakeMovie(10, out var emulator);

			TestAllOperations(movie,
				emulator,
				movie.ClearChanges,
				() => Assert.IsTrue(movie.Changes)
			);
		}

		[TestMethod]
		public void AllOperationsProduceSingleInvalidation()
		{
			ITasMovie movie = MakeMovie(10, out var emulator);
			int invalidations = 0;
			movie.GreenzoneInvalidated = (_) => invalidations++;

			TestAllOperations(movie,
				emulator,
				() => invalidations = 0,
				() => Assert.AreEqual(1, invalidations)
			);
		}
#pragma warning restore BHI1600
	}
}
