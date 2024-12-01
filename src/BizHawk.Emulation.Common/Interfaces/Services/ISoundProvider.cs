namespace BizHawk.Emulation.Common
{
	public enum SyncSoundMode
	{
		Sync, Async
	}

	/// <summary>
	/// This service provides the ability to output sound from the client,
	/// If available the client will provide sound output
	/// If unavailable the client will fallback to a default sound implementation
	/// that generates empty samples (silence)
	/// </summary>
	public interface ISoundProvider : IEmulatorService
	{
		/// <summary>
		/// Gets a value indicating whether a core can provide Async sound
		/// </summary>
		bool CanProvideAsync { get; }

		/// <summary>
		/// Sets sync or async sound mode,
		/// Sync should be the default mode if not set
		/// All implementations must provide sync
		/// If a core can not provide async sound and the mode is set to sync,
		/// an NotSupportedException should be thrown
		/// </summary>
		/// <exception cref="NotSupportedException"></exception>
		void SetSyncMode(SyncSoundMode mode);

		/// <summary>
		/// Gets which mode the sound provider is currently in
		/// </summary>
		SyncSoundMode SyncMode { get; }

		/// <summary>
		/// Provides samples in sync mode
		/// If the core is not in sync mode, this should throw an InvalidOperationException
		/// </summary>
		/// <exception cref="InvalidOperationException"></exception>
		void GetSamplesSync(out short[] samples, out int nsamp);

		/// <summary>
		/// Provides samples in async mode
		/// If the core is not in async mode, this should throw an InvalidOperationException
		/// </summary>
		/// <exception cref="InvalidOperationException"></exception>
		void GetSamplesAsync(short[] samples);

		/// <summary>
		/// Discards stuff, is there anything more to say here?
		/// </summary>
		void DiscardSamples();
	}
}
