using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class Gameboy : ISoundProvider
	{
		public bool CanProvideAsync => false;

		public void DiscardSamples()
			=> _soundoutbuffcontains = 0;

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
			=> throw new InvalidOperationException("Async mode is not supported.");

		internal bool Muted => _settings.Muted;

		// sample pairs before resampling
		private readonly short[] _soundbuff = new short[(35112 + 2064) * 2];
		private readonly short[] _sgbsoundbuff = new short[2048 * 2];
		private readonly short[] _mbcsoundbuff = new short[(35112 + 2064) * 2];

		private int _soundoutbuffcontains;

		private readonly short[] _soundoutbuff = new short[2048];

		private int _latchL, _latchR;
		private int _sgbLatchL, _sgbLatchR;
		private int _mbcLatchL, _mbcLatchR;

		private BlipBuffer _blipL, _blipR;
		private uint _blipAccumulate;

		private void ProcessSound(int nsamp)
		{
			for (uint i = 0; i < nsamp; i++)
			{
				int curr = _soundbuff[i * 2];

				if (curr != _latchL)
				{
					var diff = _latchL - curr;
					_latchL = curr;
					_blipL.AddDelta(_blipAccumulate, diff >> 1);
				}

				curr = _soundbuff[(i * 2) + 1];

				if (curr != _latchR)
				{
					var diff = _latchR - curr;
					_latchR = curr;
					_blipR.AddDelta(_blipAccumulate, diff >> 1);
				}

				_blipAccumulate++;
			}
		}

		private void ProcessSgbSound(bool processSound)
		{
			var remainder = LibGambatte.gambatte_generatesgbsamples(GambatteState, _sgbsoundbuff, out var samples);
			if (remainder < 0)
			{
				throw new InvalidOperationException($"{nameof(LibGambatte.gambatte_generatesgbsamples)}() returned negative (spc error???)");
			}
			var t = 65 - (uint)remainder;
			for (var i = 0; i < samples; i++, t += 65)
			{
				var ls = _sgbsoundbuff[i * 2] - _sgbLatchL;
				var rs = _sgbsoundbuff[i * 2 + 1] - _sgbLatchR;
				if (ls != 0 && processSound)
				{
					_blipL.AddDelta(t, ls);
				}
				if (rs != 0 && processSound)
				{
					_blipR.AddDelta(t, rs);
				}
				_sgbLatchL = _sgbsoundbuff[i * 2];
				_sgbLatchR = _sgbsoundbuff[i * 2 + 1];
			}
		}

		private void ProcessMbcSound(bool processSound)
		{
			var nsamp = LibGambatte.gambatte_generatembcsamples(GambatteState, _mbcsoundbuff);
			if (nsamp == 0)
			{
				return;
			}

			for (uint i = 0; i < nsamp; i++)
			{
				var ls = _mbcsoundbuff[i * 2] - _mbcLatchL;
				var rs = _mbcsoundbuff[i * 2 + 1] - _mbcLatchR;
				if (ls != 0 && processSound)
				{
					_blipL.AddDelta(i, ls);
				}
				if (rs != 0 && processSound)
				{
					_blipR.AddDelta(i, rs);
				}
				_mbcLatchL = _mbcsoundbuff[i * 2];
				_mbcLatchR = _mbcsoundbuff[(i * 2) + 1];
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
			_blipL = new(1024);
			_blipL.SetRates(TICKSPERSECOND, 44100);
			_blipR = new(1024);
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
