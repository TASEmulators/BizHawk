using System.Collections.Generic;
using System.IO;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public enum MovieMode
	{
		/// <summary>
		/// There is no movie loaded
		/// </summary>
		Inactive,

		/// <summary>
		/// The movie is in playback mode
		/// </summary>
		Play,

		/// <summary>
		/// The movie is currently recording
		/// </summary>
		Record,

		/// <summary>
		/// The movie has played past the end, but is still loaded in memory
		/// </summary>
		Finished
	}

	// TODO: message callback / event handler
	// TODO: consider other event handlers, switching modes?
	public interface IMovie : IBasicMovieInfo
	{
		/// <summary>
		/// Gets the current movie mode
		/// </summary>
		MovieMode Mode { get; }

		bool IsCountingRerecords { get; set; }

		bool Changes { get; }

		/// <summary>
		/// Gets the actual length of the input log, should only be used by code that needs the input log length
		/// specifically, not the frame count
		/// </summary>
		int InputLogLength { get; }

		/// <summary>
		/// Gets the file extension for the current <see cref="IMovie"/> implementation
		/// </summary>
		string PreferredExtension { get; }

		/// <summary>
		/// Gets or sets the Sync Settings from the Core
		/// </summary>
		string SyncSettingsJson { get; set; }

		// savestate anchor.
		string TextSavestate { get; set; }
		byte[] BinarySavestate { get; set; }
		int[] SavestateFramebuffer { get; set; }

		// saveram anchor
		byte[] SaveRam { get; set; }

		bool StartsFromSavestate { get; set; }
		bool StartsFromSaveRam { get; set; }

		string LogKey { get; set; }

		/// <summary>
		/// Forces the creation of a backup file of the current movie state
		/// </summary>
		void SaveBackup();

		/// <summary>
		/// Instructs the movie to save the current contents to Filename
		/// </summary>
		void Save();

		/// <summary>updates the <see cref="HeaderKeys.CycleCount"/> and <see cref="HeaderKeys.ClockRate"/> headers from the currently loaded core</summary>
		void SetCycleValues();

		/// <summary>
		/// Writes the input log directly to the stream, bypassing the need to load it all into ram as a string
		/// </summary>
		void WriteInputLog(TextWriter writer);

		/// <summary>
		/// Gets one frame from the input log.
		/// </summary>
		/// <param name="frame">The frame to get.</param>
		string GetInputLogEntry(int frame);

		/// <summary>
		/// Compares the input log inside reader with the movie's current input to see if the reader's input belongs to the same timeline,
		/// in other words, if reader's input is completely contained in the movie's input, then it is considered in the same timeline
		/// </summary>
		/// <param name="reader">The reader containing the contents of the input log</param>
		/// <param name="errorMessage">Returns an error message, if any</param>
		/// <returns>Returns whether or not the input log in reader is in the same timeline as the movie</returns>
		bool CheckTimeLines(TextReader reader, out string errorMessage);
		
		/// <summary>
		/// Takes reader and extracts the input log, then replaces the movies input log with it
		/// </summary>
		/// <param name="reader">The reader containing the contents of the input log</param>
		/// <param name="errorMessage">Returns an error message, if any</param>
		bool ExtractInputLog(TextReader reader, out string errorMessage);

		/// <summary>
		/// Tells the movie to start recording from the beginning.
		/// </summary>
		void StartNewRecording();

		/// <summary>
		/// Tells the movie to start playback from the beginning
		/// </summary>
		void StartNewPlayback();

		/// <summary>
		/// Sets the movie to inactive (note that it will still be in memory)
		/// The saveChanges flag will tell the movie to save its contents to disk
		/// </summary>
		/// <param name="saveChanges">if true, will save to disk</param>
		/// <returns>Whether or not the movie was saved</returns>
		bool Stop(bool saveChanges = true);

		/// <summary>
		/// Switches to record mode
		/// </summary>
		void SwitchToRecord();

		/// <summary>
		/// Switches to playback mode
		/// </summary>
		void SwitchToPlay();

		/// <summary>
		/// Tells the movie to go into "Finished" mode, where the user resumes control of input but the movie is still loaded in memory
		/// </summary>
		void FinishedMode();

		/// <summary>
		/// Adds the given input to the movie
		/// Note: this edits the input log without the normal movie recording logic applied
		/// </summary>
		void AppendFrame(IController source);

		/// <summary>
		/// Replaces the input at the given frame with the given input
		/// Note: this edits the input log without the normal movie recording logic applied
		/// </summary>
		void PokeFrame(int frame, IController source);

		/// <summary>
		/// Records the given input into the given frame,
		/// This is subject to normal movie recording logic
		/// </summary>
		void RecordFrame(int frame, IController source);

		/// <summary>
		/// Instructs the movie to remove all input from its input log starting with the input at frame.
		/// </summary>
		/// <param name="frame">The frame at which to truncate</param>
		void Truncate(int frame);

		/// <summary>
		/// Gets a single frame of input via a controller state
		/// </summary>
		/// <param name="frame">The frame of input to be retrieved</param>
		/// <returns>A controller state representing the specified frame of input, if frame is out of range, will return null</returns>
		IMovieController GetInputState(int frame);

		/// <summary>
		/// Attaches a core to the given movie instance, this must be done and
		/// it must be done only once, a movie can not and should not exist for more
		/// than the lifetime of the core
		/// </summary>
		/// <exception cref="System.InvalidOperationException">
		/// Thrown if attempting to attach a core when one is already attached
		/// or if the given core does not meet all required dependencies
		/// </exception>
		void Attach(IEmulator emulator);

		/// <summary>
		/// The currently attached core or null if not yet attached
		/// </summary>
		IEmulator Emulator { get; }

		/// <summary>
		/// The current movie session
		/// </summary>
		IMovieSession Session { get; }

		IStringLog GetLogEntries();
		void CopyLog(IEnumerable<string> log);
	}

	public static class MovieExtensions
	{
		public static FilesystemFilterSet GetFSFilterSet(this IMovie/*?*/ movie)
			=> new(new FilesystemFilter("Movie Files", new[] { movie?.PreferredExtension ?? MovieService.StandardMovieExtension }));

		public static bool IsActive(this IMovie movie) => movie != null && movie.Mode != MovieMode.Inactive;
		public static bool NotActive(this IMovie movie) => movie == null || movie.Mode == MovieMode.Inactive;
		public static bool IsPlaying(this IMovie movie) => movie?.Mode == MovieMode.Play;
		public static bool IsRecording(this IMovie movie) => movie?.Mode == MovieMode.Record;
		public static bool IsFinished(this IMovie movie) => movie?.Mode == MovieMode.Finished;
		public static bool IsPlayingOrFinished(this IMovie movie) => movie?.Mode == MovieMode.Play || movie?.Mode == MovieMode.Finished;
		public static bool IsPlayingOrRecording(this IMovie movie) => movie?.Mode == MovieMode.Play || movie?.Mode == MovieMode.Record;

		/// <summary>
		/// Emulation is currently right after the movie's last input frame,
		/// but no further frames have been emulated.
		/// </summary>
		public static bool IsAtEnd(this IMovie movie) => movie != null && movie.Emulator?.Frame == movie.InputLogLength;


		/// <summary>
		/// If the given movie contains a savestate it will be loaded if
		/// the given core has savestates, and a framebuffer populated
		/// if it is contained in the state and the given core supports it
		/// </summary>
		public static void ProcessSavestate(this IMovie movie, IEmulator emulator)
		{
			if (emulator.HasSavestates() && movie.StartsFromSavestate)
			{
				if (movie.TextSavestate != null)
				{
					emulator.AsStatable().LoadStateText(movie.TextSavestate);
				}
				else
				{
					emulator.AsStatable().LoadStateBinary(movie.BinarySavestate);
				}

				if (movie.SavestateFramebuffer != null && emulator.HasVideoProvider())
				{
					emulator.AsVideoProvider().PopulateFromBuffer(movie.SavestateFramebuffer);
				}

				emulator.ResetCounters();
			}
		}

		/// <summary>
		/// Sets the given <paramref name="emulator"/> save ram if the movie contains save ram
		/// and the core supports save ram
		/// </summary>
		public static void ProcessSram(this IMovie movie, IEmulator emulator)
		{
			if (movie.StartsFromSaveRam && emulator.HasSaveRam())
			{
				emulator.AsSaveRam().StoreSaveRam(movie.SaveRam!);
			}
		}

		public static bool BoolIsPressed(this IMovie movie, int frame, string buttonName)
			=> movie.GetInputState(frame).IsPressed(buttonName);

		public static int GetAxisState(this IMovie movie, int frame, string buttonName)
			=> movie.GetInputState(frame).AxisValue(buttonName);
	}
}
