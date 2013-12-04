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

		/// <summary>
		/// Repalces the given frame's input with an empty frame
		/// </summary>
		/// <param name="frame"></param>
		void ClearFrame(int frame);
		
		/// <summary>
		/// Adds the given input to the movie
		/// Note: this edits the input log without the normal movie recording logic applied
		/// </summary>
		/// <param name="mg"></param>
		void AppendFrame(MnemonicsGenerator mg);

		/// <summary>
		/// Replaces the input at the given frame with the given input
		/// Note: this edits the input log without the normal movie recording logic applied
		/// </summary>
		void PokeFrame(int frame, MnemonicsGenerator mg);

		/// <summary>
		/// Records the given input into the given frame,
		/// This is subject to normal movie recording logic
		/// </summary>
		void RecordFrame(int frame, MnemonicsGenerator mg);

		void Truncate(int frame);
		string GetInput(int frame);

		#endregion

		#region Dubious, should reconsider

		bool CheckTimeLines(TextReader reader, out string errorMessage); //Can we avoid passing a text reader?
		void ExtractInputLog(TextReader reader); //Is passing a textreader the only reasonable way to do this?

		#endregion
	}
}
