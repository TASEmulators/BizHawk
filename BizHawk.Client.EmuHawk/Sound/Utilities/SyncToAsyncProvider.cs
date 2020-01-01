using System;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public class SyncToAsyncProvider : ISoundProvider
	{
		private readonly SoundOutputProvider _outputProvider = new SoundOutputProvider(standaloneMode: true);

		public SyncToAsyncProvider(ISoundProvider baseProvider)
		{
			_outputProvider.BaseSoundProvider = baseProvider;
		}

		public void DiscardSamples()
		{
			_outputProvider.DiscardSamples();
		}

		public bool CanProvideAsync
		{
			get { return true; }
		}

		public SyncSoundMode SyncMode
		{
			get { return SyncSoundMode.Async; }
		}

		/// <exception cref="NotSupportedException"><paramref name="mode"/> is not <see cref="SyncSoundMode.Async"/></exception>
		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode != SyncSoundMode.Async)
			{
				throw new NotSupportedException("Sync mode is not supported.");
			}
		}

		/// <exception cref="InvalidOperationException">always</exception>
		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			throw new InvalidOperationException("Sync mode is not supported.");
		}

		public void GetSamplesAsync(short[] samples)
		{
			_outputProvider.GetSamples(samples);
		}
	}
}
