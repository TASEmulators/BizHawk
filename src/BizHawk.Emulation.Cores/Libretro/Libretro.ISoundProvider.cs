using System;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Libretro
{
	public partial class LibretroEmulator : ISoundProvider
	{
		private SpeexResampler _resampler;

		private void SetupResampler(double fps, double sps)
		{
			Console.WriteLine("FPS {0} SPS {1}", fps, sps);

			// todo: more precise?
			uint spsnum = (uint)sps * 10000;
			uint spsden = 10000U;

			_resampler = new SpeexResampler(SpeexResampler.Quality.QUALITY_DESKTOP, 44100 * spsden, spsnum, (uint)sps, 44100, null, this);
		}

		public bool CanProvideAsync => false;

		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode == SyncSoundMode.Async)
			{
				throw new NotSupportedException("Async mode is not supported.");
			}
		}

		private short[] sampleBuf = new short[0];

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			var len = bridge.LibretroBridge_GetAudioSize(cbHandler);
			if (len == 0) // no audio?
			{
				samples = sampleBuf;
				nsamp = 0;
				return;
			}
			if (len > sampleBuf.Length)
			{
				sampleBuf = new short[len];
			}
			var ns = 0;
			bridge.LibretroBridge_GetAudio(cbHandler, ref ns, sampleBuf);
			samples = sampleBuf;
			nsamp = ns;
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new InvalidOperationException("Async mode is not supported.");
		}

		public void DiscardSamples()
		{
		}
	}
}
