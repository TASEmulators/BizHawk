using System;
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

		#endregion

		#region Properties

		/// <summary>
		/// Gets the total number of frames that count towards the completion time of the movie
		/// Possibly (but unlikely different from InputLogLength (could be infinity, or maybe an implementation automatically discounts empty frames at the end of a movie, etc)
		/// </summary>
		double FrameCount { get; }
		
		/// <summary>
		/// Gets the Fps used to calculate the time of the movie
		/// </summary>
		double Fps { get; }

		/// <summary>
		/// Gets the time calculation based on FrameCount and Fps
		/// </summary>
		TimeSpan Time { get; }

		/// <summary>
		/// Gets the actual length of the input log, should only be used by code that iterates or needs a real length
		/// </summary>
		int InputLogLength { get; }

		IMovieHeader Header { get; }

		#endregion

		#region File Handling API

		string Filename { get; set; }
		bool Load();
		void Save();
		string GetInputLog();
		bool CheckTimeLines(TextReader reader, out string errorMessage);
		bool ExtractInputLog(TextReader reader, out string errorMessage);

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

		void Truncate(int frame);
		string GetInput(int frame);

		#endregion
	}
}
