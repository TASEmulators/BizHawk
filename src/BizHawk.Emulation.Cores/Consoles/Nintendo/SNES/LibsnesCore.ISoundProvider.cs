using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	public partial class LibsnesCore : ISoundProvider
	{
		private readonly BlipBuffer _blipL = new(4096);
		private readonly BlipBuffer _blipR = new(4096);
		private short _latchL, _latchR;
		private readonly short[] _sampleBuffer = new short[4096];
		private uint _inSamps;
		private int _outSamps;

		public void DiscardSamples()
		{
			_outSamps = 0;
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			samples = _sampleBuffer;
			nsamp = _outSamps;
			DiscardSamples();
		}

		private void InitAudio()
		{
			_blipL.SetRates(32040.5, 44100);
			_blipR.SetRates(32040.5, 44100);
		}

		private void snes_audio_sample(short left, short right)
		{
			if (_latchL != left)
			{
				_blipL.AddDelta(_inSamps, _latchL - left);
				_latchL = left;
			}

			if (_latchR != right)
			{
				_blipR.AddDelta(_inSamps, _latchR - right);
				_latchR = right;
			}

			_inSamps++;
		}

		private void ProcessSoundEnd()
		{
			_blipL.EndFrame(_inSamps);
			_blipR.EndFrame(_inSamps);
			_inSamps = 0;

			_outSamps = _blipL.SamplesAvailable();
			if (_outSamps != _blipR.SamplesAvailable())
			{
				throw new InvalidOperationException("Audio processing error");
			}

			_blipL.ReadSamplesLeft(_sampleBuffer, _outSamps);
			_blipR.ReadSamplesRight(_sampleBuffer, _outSamps);
		}

		public bool CanProvideAsync => false;

		public void GetSamplesAsync(short[] samples)
		{
			throw new InvalidOperationException("Async mode is not supported.");
		}

		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode == SyncSoundMode.Async)
			{
				throw new NotSupportedException("Async mode is not supported.");
			}
		}
	}
}
