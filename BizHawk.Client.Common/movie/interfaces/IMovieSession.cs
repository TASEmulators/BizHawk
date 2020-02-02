using System.IO;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface IMovieSession
	{
		IMovie Movie { get; set; }
		IMovie QueuedMovie { get; }
		IMovieController MovieControllerAdapter { get; }
		IMovieController MovieControllerInstance();
		MultitrackRecorder MultiTrack { get; }

		IController PreviousFrame { get; }
		IController CurrentInput { get; }

		bool ReadOnly { get; set; }
		bool MovieIsQueued { get; }

		bool? PreviousNesInQuickNES { get; set; }
		bool? PreviousSnesInSnes9x { get; set; }
		bool? PreviousGbaUsemGba { get; set; }

		void HandleMovieOnFrameLoop();
		void HandleMovieAfterFrameLoop();
		void HandleMovieSaveState(TextWriter writer);

		bool CheckSavestateTimeline(TextReader reader);
		bool HandleMovieLoadState(TextReader reader);

		ILogEntryGenerator LogGeneratorInstance();

		void QueueNewMovie(IMovie movie, bool record, IEmulator emulator);
		void RunQueuedMovie(bool recordMode);

		void ToggleMultitrack();

		void StopMovie(bool saveChanges = true);
	}
}
