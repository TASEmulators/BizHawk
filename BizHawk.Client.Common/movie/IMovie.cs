using System.IO;
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
		bool Loaded { get; }
		bool StartsFromSavestate { get; }

		int Rerecords { get; set; }

		#endregion

		#region File Handling API

		string Filename { get; set; }
		bool Load();
		void Save();
		void SaveAs();

		#endregion

		#region Mode Handling API

		/// <summary>
		/// Tells the movie to start recording from the beginning.
		/// This will clear sram, and the movie log
		/// </summary>
		/// <param name="truncate"></param>
		void StartNewRecording();

		/// <summary>
		/// Tells the movie to start playback from the beginning
		/// This will clear sram
		/// </summary>
		void StartNewPlayback();

		/// <summary>
		/// Sets the movie to inactive (note that it will still be in memory)
		/// The saveChanges flag will tell the movie to save its contents to disk
		/// </summary>
		/// <param name="saveChanges"></param>
		void Stop(bool saveChanges = true);

		/// <summary>
		/// Switches to record mode
		/// Does not change the movie log or clear sram
		/// </summary>
		void SwitchToRecord();

		/// <summary>
		/// Switches to playback mode
		/// Does not change the movie log or clear sram
		/// </summary>
		void SwitchToPlay();

		#endregion

		#region Editing API

		void ClearFrame(int frame);
		void ModifyFrame(string record, int frame);
		void AppendFrame(string record);
		void InsertFrame(string record, int frame);
		void InsertBlankFrame(int frame);
		void DeleteFrame(int frame);
		void TruncateMovie(int frame);

		#endregion

		#region Dubious, should reconsider
		void CommitFrame(int frameNum, IController source); // Why pass in frameNum? Calling api 
		void PokeFrame(int frameNum, string input); // Why does this exist as something different than Commit Frame?
		LoadStateResult CheckTimeLines(TextReader reader, bool onlyGuid, bool ignoreGuidMismatch, out string errorMessage); // No need to return a status, no reason to have hacky flags, no need to pass a textreader
		string GetTime(bool preLoad); // Rename to simply: Time, and make it a Timespan
		void DumpLogIntoSavestateText(TextWriter writer); // Why pass a Textwriter, just make a string property that is the inputlog as text
		void LoadLogFromSavestateText(TextReader reader, bool isMultitracking); // Pass in the text? do we need to care if it is multitracking, and can't hte movie already know that?
		int? Frames { get; } // Nullable is a hack, also why does calling code need to know the number of frames, can that be minimized?
		int RawFrames { get; } // Hacky to need two different frame properties

		string GetInput(int frame); // Should be a property of a Record object

		IMovieHeader Header { get; } // Expose IMovieHEader instead
		MovieLog LogDump { get; } // Don't expose this!!!
		//SubtitleList Subtitles { get; } // Don't expose this!!!

		#endregion
	}
}

// TODO: delete this and refactor code that uses it!
public enum LoadStateResult { Pass, GuidMismatch, TimeLineError, FutureEventError, NotInRecording, EmptyLog, MissingFrameNumber }