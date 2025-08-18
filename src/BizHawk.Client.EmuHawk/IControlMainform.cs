namespace BizHawk.Client.EmuHawk
{
	public interface IControlMainform
	{
		bool WantsToControlReboot { get; }
		void RebootCore();

		bool WantsToControlSavestates { get; }

		void SaveState();

		bool LoadState();

		void SaveStateAs();

		bool LoadStateAs();

		void SaveQuickSave(int slot);

		bool LoadQuickSave(int slot);

		/// <summary>
		/// Overrides the select slot method
		/// </summary>
		/// <returns>Returns whether the function is handled.
		/// If false, the mainform should continue with its logic</returns>
		bool SelectSlot(int slot);
		bool PreviousSlot();
		bool NextSlot();

		bool WantsToControlReadOnly { get; }

		/// <summary>
		/// Function that is called by Mainform instead of using its own code
		/// when a Tool sets WantsToControlReadOnly.
		/// Should not be called directly.
		/// </summary>
		void ToggleReadOnly();

		bool WantsToControlStopMovie { get; }

		/// <summary>
		/// Function that is called by Mainform instead of using its own code
		/// when a Tool sets WantsToControlStopMovie.
		/// Should not be called directly.
		/// <remarks>Like MainForm's StopMovie(), saving the movie is part of this function's responsibility.</remarks>
		/// </summary>
		void StopMovie(bool suppressSave);

		bool WantsToControlRewind { get; }

		void CaptureRewind();

		/// <summary>
		/// Function that is called by Mainform instead of using its own code
		/// when a Tool sets WantsToControlRewind
		/// Returns whether or not the rewind action actually occured
		/// </summary>
		bool Rewind();

		bool WantsToControlRestartMovie { get; }

		bool RestartMovie();

		bool WantsToBypassMovieEndAction { get; }
	}
}
