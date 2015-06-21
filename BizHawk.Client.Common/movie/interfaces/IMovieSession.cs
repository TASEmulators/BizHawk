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

		bool? PreviousNES_InQuickNES { get; set; }
		bool? PreviousSNES_InSnes9x { get; set; }
		bool? PreviousGBA_UsemGBA { get; set; }

		void HandleMovieOnFrameLoop();
		void HandleMovieAfterFrameLoop();
		void HandleMovieSaveState(TextWriter writer);
		bool HandleMovieLoadState(string path);

		// To function as a MovieSession, you must have hacky LoadState steps, non-hacky steps just won't do
		bool HandleMovieLoadState_HackyStep1(TextReader reader);
		bool HandleMovieLoadState_HackyStep2(TextReader reader);

		ILogEntryGenerator LogGeneratorInstance();

		void QueueNewMovie(IMovie movie, bool record, IEmulator emulator);
		void RunQueuedMovie(bool recordMode);

		void ToggleMultitrack();

		void StopMovie(bool saveChanges = true);
	}
}
