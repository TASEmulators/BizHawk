using System.Collections.Generic;
using System.IO;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface IMovieSession
	{
		IMovieConfig Settings { get; }
		IMovie Movie { get; }
		bool ReadOnly { get; set; }

		/// <summary>
		/// Gets a value indicating whether or not a new movie is queued for loading
		/// </summary>
		bool NewMovieQueued { get; }

		/// <summary>
		/// Gets the sync settings from a queued movie, if a movie is queued
		/// </summary>
		string QueuedSyncSettings { get; }

		/// <value>The Core header of the queued movie iff one is queued, else <see langword="null"/></value>
		string QueuedCoreName { get; }

		IDictionary<string, object> UserBag { get; set; }

		IMovieController MovieController { get; }

		/// <summary>
		/// Provides a source for sticky controls to use when recording
		/// </summary>
		IController StickySource { get; set; }

		/// <summary>
		/// Represents the input source that is fed to
		/// the movie for the purpose of recording, if active,
		/// or to simply pass through if inactive
		/// </summary>
		IController MovieIn { get; set; }

		/// <summary>
		/// Represents the movie input in the input chain
		/// Is a pass through when movies are not active,
		/// otherwise they handle necessary movie logic
		/// </summary>
		IInputAdapter MovieOut { get; }

		/// <summary>
		/// Creates a <see cref="IMovieController" /> instance based on the
		/// given button definition if provided else the current
		/// <see cref="MovieController"/>s button definition will be used
		/// </summary>
		IMovieController GenerateMovieController(ControllerDefinition definition = null, string logKey = null);

		void HandleFrameBefore();
		void HandleFrameAfter();
		void HandleSaveState(TextWriter writer);

		bool CheckSavestateTimeline(TextReader reader);
		bool HandleLoadState(TextReader reader);

		/// <summary>
		/// Queues up a movie for loading
		/// When initializing a movie, it will be stored until Rom loading processes have been completed, then it will be moved to the Movie property
		/// If an existing movie is still active, it will remain in the Movie property while the new movie is queued
		/// </summary>
		void QueueNewMovie(
			IMovie movie,
			string systemId,
			string loadedRomHash,
			PathEntryCollection pathEntries,
			IDictionary<string, string> preferredCores);

		/// <summary>
		/// Sets the Movie property with the QueuedMovie, clears the queued movie, and starts the new movie
		/// </summary>
		void RunQueuedMovie(bool recordMode, IEmulator emulator);

		/// <summary>clears the queued movie</summary>
		void AbortQueuedMovie();

		void StopMovie(bool saveChanges = true);

		/// <summary>
		/// If a movie is active, it will be converted to a <see cref="ITasMovie" />
		/// </summary>
		void ConvertToTasProj();

		/// <summary>
		/// Create a new (Tas)Movie with the given path as filename. If <paramref name="loadMovie"/> is true,
		/// will also attempt to load an existing movie from <paramref name="path"/>.
		/// </summary>
		IMovie Get(string path, bool loadMovie = false);

		string BackupDirectory { get; set; }

		void PopupMessage(string message);
	}
}
