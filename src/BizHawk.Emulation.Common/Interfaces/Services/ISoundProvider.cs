using System;

namespace BizHawk.Emulation.Common
{
	public enum SyncSoundMode
	{
		Sync, Async
	}

	public interface ISoundProviderBase : IEmulatorService
	{
		/// <summary>
		/// Discards stuff, is there anything more to say here?
		/// </summary>
		void DiscardSamples();
	}

	/// <summary>
	/// This service provides the ability to output sound from the client,
	/// If available the client will provide sound output
	/// If unavailable the client will fallback to a default sound implementation
	/// that generates empty samples (silence)
	/// </summary>
	public interface IAsyncSoundProvider : ISoundProviderBase
	{
		/// <summary>
		/// Provides samples in async mode
		/// If the core is not in async mode, this should throw an InvalidOperationException
		/// </summary>
		/// <exception cref="InvalidOperationException"></exception>
		void GetSamplesAsync(short[] samples);
	}

	/// <summary>
	/// This service provides the ability to output sound from the client,
	/// If available the client will provide sound output
	/// If unavailable the client will fallback to a default sound implementation
	/// that generates empty samples (silence)
	/// </summary>
	public interface ISyncSoundProvider : ISoundProviderBase
	{
		/// <summary>
		/// Provides samples in sync mode
		/// If the core is not in sync mode, this should throw an InvalidOperationException
		/// </summary>
		/// <exception cref="InvalidOperationException"></exception>
		void GetSamplesSync(out short[] samples, out int nsamp);
	}

	/// <summary>implementor can operate as either <see cref="IAsyncSoundProvider"/> or <see cref="ISyncSoundProvider"/></summary>
	public interface ISoundProvider : ISoundProviderBase
	{
		IAsyncSoundProvider AsAsyncProvider();

		ISyncSoundProvider AsSyncProvider();
	}
}
