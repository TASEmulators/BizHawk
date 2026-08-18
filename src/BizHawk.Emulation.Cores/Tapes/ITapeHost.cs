namespace BizHawk.Emulation.Cores.Tapes
{
	/// <summary>
	/// The per-core services a TapeDeck needs from its host machine. This lets the shared deck
	/// generate the tape signal and drive transport without referencing any core-specific CPU or machine type:
	/// the host supplies the CPU cycle counter, feeds its own tape beeper, and receives tape notifications.
	/// A core with a tape motor/remote line or a fast-loader layers those on top; the deck itself is unaware.
	/// </summary>
	public interface ITapeHost
	{
		/// <summary>
		/// Running total of executed CPU cycles. The deck compares pulse periods against this.
		/// </summary>
		long TotalExecutedCycles { get; }

		/// <summary>
		/// True when the host is running in 48K mode (gates the STOP_THE_TAPE_48K command block).
		/// </summary>
		bool IsIn48kMode { get; }

		/// <summary>
		/// True when non-cycle-accurate fast/trap tape loading is permitted (i.e. emulation is not
		/// deterministic). Reserved for a future speed-loader; the cycle-accurate signal path does not use it.
		/// </summary>
		bool FastLoadAllowed { get; }

		/// <summary>
		/// Feeds the current tape EAR level to the host's tape beeper.
		/// </summary>
		void FeedBeeper(bool earLevel);

		// Notifications - a host may surface these on-screen (OSD) or ignore them entirely.
		void NotifyPlay();
		void NotifyStop();
		void NotifyRewind();
		void NotifyNextBlock(string blockInfo);
		void NotifyPrevBlock(string blockInfo);
		void NotifyPlayingBlock(string blockInfo);
		void NotifySkipBlock(string blockInfo);
		void NotifyStoppedAuto();

		/// <summary>
		/// A STOP_THE_TAPE command block was reached during playback. A host that auto-detects tape activity
		/// may use this to reset its auto-stop timing; others can ignore it.
		/// </summary>
		void NotifyStopCommand();
	}
}
