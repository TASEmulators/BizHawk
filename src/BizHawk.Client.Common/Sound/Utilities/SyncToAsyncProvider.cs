using System;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class SyncToAsyncProvider : IAsyncSoundProvider
	{
		private readonly SoundOutputProvider _outputProvider;

		public SyncToAsyncProvider(Func<double> getCoreVsyncRateCallback, ISyncSoundProvider baseProvider)
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

		public void GetSamplesAsync(short[] samples)
		{
			_outputProvider.GetSamples(samples);
		}
	}
}
