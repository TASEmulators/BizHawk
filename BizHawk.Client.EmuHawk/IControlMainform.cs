namespace BizHawk.Client.EmuHawk
{
	public interface IControlMainform
	{
		bool WantsToControlSavestates { get; }

		void SaveState();
		void LoadState();
		void SaveStateAs();
		void LoadStateAs();
		void SaveQuickSave(int slot);
		void LoadQuickSave(int slot);
		void SelectSlot(int slot);
		void PreviousSlot();
		void NextSlot();

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
		void StopMovie(bool supressSave);

		bool WantsToControlRewind { get; }

		void CaptureRewind();

		/// <summary>
		/// Function that is called by Mainform instead of using its own code
		/// when a Tool sets WantsToControlRewind
		/// Returns whether or not the rewind action actually occured
		/// </summary>
		/// <returns></returns>
		bool Rewind();

		bool WantsToControlRestartMovie { get; }

		void RestartMovie();
	}
}
