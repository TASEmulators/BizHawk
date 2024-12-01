using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Libretro
{
	public partial class LibretroHost : ISoundProvider
	{
		private const int OUT_SAMPLE_RATE = 44100;

		private BlipBuffer _blipL, _blipR;
		private int _latchL, _latchR;

		private short[] _inSampBuf = Array.Empty<short>(); // variable size, will grow as needed

		private readonly short[] _outSampBuf = new short[OUT_SAMPLE_RATE * 2]; // big enough
		private int _outSamps;

		private void SetupResampler(double sps)
		{
			_blipL = new(OUT_SAMPLE_RATE);
			_blipL.SetRates(sps, OUT_SAMPLE_RATE);
			_blipR = new(OUT_SAMPLE_RATE);
			_blipR.SetRates(sps, OUT_SAMPLE_RATE);
		}

		private void ProcessSound()
		{
			var len = bridge.LibretroBridge_GetAudioSize(cbHandler);
			if (len == 0) // no audio?
			{
				return;
			}

			// skip resampling if in sample rate == out sample rate
			if (av_info.timing.sample_rate == OUT_SAMPLE_RATE)
			{
				if (len > (OUT_SAMPLE_RATE * 2))
				{
					throw new Exception("Audio buffer overflow!");
				}

				// copy directly to our output buffer
				bridge.LibretroBridge_GetAudio(cbHandler, out _outSamps, _outSampBuf);
				return;
			}

			if (len > _inSampBuf.Length)
			{
				_inSampBuf = new short[len];
			}

			bridge.LibretroBridge_GetAudio(cbHandler, out var ns, _inSampBuf);

			for (uint i = 0; i < ns; i++)
			{
				int cur = _inSampBuf[i * 2];

				if (cur != _latchL)
				{
					int diff = _latchL - cur;
					_latchL = cur;
					_blipL.AddDelta(i, diff);
				}

				cur = _inSampBuf[(i * 2) + 1];

				if (cur != _latchR)
				{
					int diff = _latchR - cur;
					_latchR = cur;
					_blipR.AddDelta(i, diff);
				}
			}

			_blipL.EndFrame((uint)ns);
			_blipR.EndFrame((uint)ns);
			_outSamps = _blipL.SamplesAvailable();

			if (_outSamps > OUT_SAMPLE_RATE)
			{
				throw new Exception("Audio buffer overflow!");
			}

			_blipL.ReadSamplesLeft(_outSampBuf, _outSamps);
			_blipR.ReadSamplesRight(_outSampBuf, _outSamps);
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

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			nsamp = _outSamps;
			samples = _outSampBuf;
			DiscardSamples();
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new InvalidOperationException("Async mode is not supported.");
		}

		public void DiscardSamples()
		{
			_outSamps = 0;
		}
	}
}
