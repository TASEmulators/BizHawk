using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class SyncToAsyncProvider : ISoundProvider
	{
		private readonly SoundOutputProvider _outputProvider;

		public SyncToAsyncProvider(Func<double> getCoreVsyncRateCallback, ISoundProvider baseProvider)
		{
			_outputProvider = new SoundOutputProvider(getCoreVsyncRateCallback, standaloneMode: true)
			{
				BaseSoundProvider = baseProvider
			};
		}

		public void DiscardSamples()
		{
			_outputProvider.DiscardSamples();
		}

		public bool CanProvideAsync => true;

		public SyncSoundMode SyncMode => SyncSoundMode.Async;

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
