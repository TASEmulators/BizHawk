using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class Gameboy : ISoundProvider
	{
		public bool CanProvideAsync => false;

		public void DiscardSamples()
		{
			_soundoutbuffcontains = 0;
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			samples = _soundoutbuff;
			nsamp = _soundoutbuffcontains;
		}

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode == SyncSoundMode.Async)
			{
				throw new NotSupportedException("Async mode is not supported.");
			}
		}

		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		public void GetSamplesAsync(short[] samples)
		{
			throw new InvalidOperationException("Async mode is not supported.");
		}

		internal bool Muted => _settings.Muted;

		// sample pairs before resampling
		private readonly short[] _soundbuff = new short[(35112 + 2064) * 2];

		private int _soundoutbuffcontains = 0;

		private readonly short[] _soundoutbuff = new short[2048];

		private int _latchL = 0;
		private int _latchR = 0;

		private BlipBuffer _blipL, _blipR;
		private uint _blipAccumulate;

		private void ProcessSound(int nsamp)
		{
			for (uint i = 0; i < nsamp; i++)
			{
				int curr = _soundbuff[i * 2];

				if (curr != _latchL)
				{
					int diff = _latchL - curr;
					_latchL = curr;
					_blipL.AddDelta(_blipAccumulate, diff >> 2);
				}

				curr = _soundbuff[(i * 2) + 1];

				if (curr != _latchR)
				{
					int diff = _latchR - curr;
					_latchR = curr;
					_blipR.AddDelta(_blipAccumulate, diff >> 2);
				}

				_blipAccumulate++;
			}
		}

		private void ProcessSoundEnd()
		{
			_blipL.EndFrame(_blipAccumulate);
			_blipR.EndFrame(_blipAccumulate);
			_blipAccumulate = 0;

			_soundoutbuffcontains = _blipL.SamplesAvailable();
			if (_soundoutbuffcontains != _blipR.SamplesAvailable())
			{
				throw new InvalidOperationException("Audio processing error");
			}

			_blipL.ReadSamplesLeft(_soundoutbuff, _soundoutbuffcontains);
			_blipR.ReadSamplesRight(_soundoutbuff, _soundoutbuffcontains);
		}

		private void InitSound()
		{
			_blipL = new BlipBuffer(1024);
			_blipL.SetRates(TICKSPERSECOND, 44100);
			_blipR = new BlipBuffer(1024);
			_blipR.SetRates(TICKSPERSECOND, 44100);
		}

		private void DisposeSound()
		{
			if (_blipL != null)
			{
				_blipL.Dispose();
				_blipL = null;
			}

			if (_blipR != null)
			{
				_blipR.Dispose();
				_blipR = null;
			}
		}
	}
}
