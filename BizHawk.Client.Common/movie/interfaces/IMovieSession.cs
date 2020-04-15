using System.IO;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface IMovieSession
	{
		IMovie Movie { get; set; }

		/// <summary>
		/// Gets the queued movie
		/// When initializing a movie, it will be stored here until Rom processes have been completed, then it will be moved to the Movie property
		/// If an existing movie is still active, it will remain in the Movie property while the new movie is queued
		/// </summary>
		IMovie QueuedMovie { get; }

		bool MovieIsQueued { get; }

		bool ReadOnly { get; set; }

		IMovieController MovieController { get; }
		MultitrackRecorder MultiTrack { get; }

		/// <summary>
		/// Gets the controller state from the movie for the most recent frame
		/// </summary>
		IController CurrentInput { get; }

		IController PreviousFrame { get; }

		// TODO: this isn't sustainable
		bool? PreviousNesInQuickNES { get; set; }
		bool? PreviousSnesInSnes9x { get; set; }
		bool? PreviousGbaUsemGba { get; set; }
		bool? PreviousGbUseGbHawk { get; set; }

		/// <summary>
		/// Recreates MovieController with the given controller definition
		/// with an empty controller state
		/// </summary>
		void RecreateMovieController(ControllerDefinition definition);

		/// <summary>
		/// Creates a <see cref="IMovieController" /> instance based on the
		/// given button definition if provided else the
		/// current <see cref="MovieController" /> button definition
		/// will be used
		/// </summary>
		IMovieController GenerateMovieController(ControllerDefinition definition = null);

		void HandleMovieOnFrameLoop();
		void HandleMovieAfterFrameLoop();
		void HandleMovieSaveState(TextWriter writer);

		bool CheckSavestateTimeline(TextReader reader);
		bool HandleMovieLoadState(TextReader reader);

		void QueueNewMovie(IMovie movie, bool record, IEmulator emulator);

		/// <summary>
		/// Sets the Movie property with the QueuedMovie, clears the queued movie, and starts the new movie
		/// </summary>
		void RunQueuedMovie(bool recordMode);

		void ToggleMultitrack();

		void StopMovie(bool saveChanges = true);
	}
}
