using System.Collections.Generic;
using System.IO;
using BizHawk.Emulation.Common;
using System;

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

		#endregion

		#region Properties

		double FrameCount { get; }

		TimeSpan Time { get; }

		/// <summary>
		/// Actual length of the input log, should only be used by code that iterates or needs a real length
		/// </summary>
		int InputLogLength { get; }

		IMovieHeader Header { get; }

		#endregion

		#region File Handling API

		string Filename { get; set; }
		bool Load();
		void Save();
		void SaveAs();
		string GetInputLog();

		#endregion

		#region Mode Handling API

		/// <summary>
		/// Tells the movie to start recording from the beginning.
		/// This will clear SRAM, and the movie log
		/// </summary>
		void StartNewRecording();

		/// <summary>
		/// Tells the movie to start playback from the beginning
		/// This will clear SRAM
		/// </summary>
		void StartNewPlayback();

		/// <summary>
		/// Sets the movie to inactive (note that it will still be in memory)
		/// The saveChanges flag will tell the movie to save its contents to disk
		/// </summary>
		/// <param name="saveChanges">if true, will save to disk</param>
		void Stop(bool saveChanges = true);

		/// <summary>
		/// Switches to record mode
		/// Does not change the movie log or clear SRAM
		/// </summary>
		void SwitchToRecord();

		/// <summary>
		/// Switches to playback mode
		/// Does not change the movie log or clear SRAM
		/// </summary>
		void SwitchToPlay();

		#endregion

		#region Editing API

		void ClearFrame(int frame);
		void AppendFrame(MnemonicsGenerator mg);
		void Truncate(int frame);

		#endregion

		#region Dubious, should reconsider

		void CommitFrame(int frameNum, MnemonicsGenerator mg); // Why pass in frameNum? Calling api 
		void PokeFrame(int frameNum, string input); // Why does this exist as something different than Commit Frame?
		LoadStateResult CheckTimeLines(TextReader reader, bool onlyGuid, bool ignoreGuidMismatch, out string errorMessage); // No need to return a status, no reason to have hacky flags, no need to pass a textreader
		
		void ExtractInputLog(TextReader reader, bool isMultitracking); // how about the movie know if it is multi-tracking rather than having to pass it in

		string GetInput(int frame); // Should be a property of a Record object

		#endregion
	}
}

// TODO: delete this and refactor code that uses it!
public enum LoadStateResult { Pass, GuidMismatch, TimeLineError, FutureEventError, NotInRecording, EmptyLog, MissingFrameNumber }