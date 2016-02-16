using System.Collections.Generic;
using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	// TODO: message callback / event handler
	// TODO: consider other event handlers, switching modes?
	public interface IMovie
	{
		#region Status

		bool IsCountingRerecords { get; set; }
		bool IsActive { get; }
		bool IsPlaying { get; }
		bool IsRecording { get; }
		bool IsFinished { get; }
		bool Changes { get; }

		#endregion

		#region Properties

		/// <summary>
		/// Gets the total number of frames that count towards the completion time of the movie
		/// Possibly (but unlikely different from InputLogLength (could be infinity, or maybe an implementation automatically discounts empty frames at the end of a movie, etc)
		/// </summary>
		double FrameCount { get; }

		/// <summary>
		/// Gets the actual length of the input log, should only be used by code that iterates or needs a real length
		/// </summary>
		int InputLogLength { get; }

		/// <summary>
		/// Returns the file extension for this implementation
		/// </summary>
		string PreferredExtension { get; }

		/// <summary>
		/// Sync Settings from the Core
		/// </summary>
		string SyncSettingsJson { get; set; }

		SubtitleList Subtitles { get; }
		IList<string> Comments { get; }

		// savestate anchor.
		string TextSavestate { get; set; }
		byte[] BinarySavestate { get; set; }
		int[] SavestateFramebuffer { get; set; }

		// saveram anchor
		byte[] SaveRam { get; set; }

		ulong Rerecords { get; set; }
		bool StartsFromSavestate { get; set; }
		bool StartsFromSaveRam { get; set; }
		string GameName { get; set; }
		string SystemID { get; set; }
		string Hash { get; set; }
		string Author { get; set; }
		string Core { get; set; }
		string EmulatorVersion { get; set; }
		string FirmwareHash { get; set; }
		string BoardName { get; set; }

		/// <summary>
		/// Loads from the HawkFile the minimal amount of information needed to determine Header info and Movie length
		/// This method is intended to be more performant than a full load
		/// </summary>
		bool PreLoadHeaderAndLength(HawkFile hawkFile);
		
		/// <summary>
		/// Returns header key value pairs stored in the movie file
		/// </summary>
		IDictionary<string, string> HeaderEntries { get; }

		/// <summary>
		/// Forces the creation of a backup file of the current movie state
		/// </summary>
		void SaveBackup();

		/// <summary>
		/// Creates an instance of the Input log entry used to generate the input log
		/// </summary>
		ILogEntryGenerator LogGeneratorInstance();

		#endregion

		#region File Handling API

		// Filename of the movie, settable by the client
		string Filename { get; set; }

		/// <summary>
		/// Tells the movie to load the contents of Filename
		/// </summary>
		/// <returns>Return whether or not the file was successfully loaded</returns>
		bool Load(bool preload);

		/// <summary>
		/// Instructs the movie to save the current contents to Filename
		/// </summary>
		void Save();
		
		/// <summary>
		/// Extracts the current input log from the user.  
		/// This is provided as the means for putting the input log into savestates,
		/// for the purpose of out of order savestate loading (known as "bullet-proof rerecording")
		/// </summary>
		/// <returns>returns a string represntation of the input log in its current state</returns>
		string GetInputLog();

		/// <summary>
		/// Writes the input log directly to the stream, bypassing the need to load it all into ram as a string
		/// </summary>
		void WriteInputLog(TextWriter writer);

		/// <summary>
		/// Gets one frame from the input log.
		/// </summary>
		/// <param name="frame">The frame to get.</param>
		/// <returns></returns>
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
		/// <returns></returns>
		bool ExtractInputLog(TextReader reader, out string errorMessage);

		#endregion

		#region Mode Handling API

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

		#endregion

		#region Editing API

		/// <summary>
		/// Replaces the given frame's input with an empty frame
		/// </summary>
		void ClearFrame(int frame);
		
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
		/// Instructs the movie to remove all input from its input log after frame,
		/// AFter truncating, frame will be the last frame of input in the movie's input log
		/// </summary>
		/// <param name="frame">The frame at which to truncate</param>
		void Truncate(int frame);

		/// <summary>
		/// Gets a single frame of input via a controller state
		/// </summary>
		/// <param name="frame">The frame of input to be retrieved</param>
		/// <returns>A controller state representing the specified frame of input, if frame is out of range, will return null</returns>
		IController GetInputState(int frame);

		#endregion
	}
}
