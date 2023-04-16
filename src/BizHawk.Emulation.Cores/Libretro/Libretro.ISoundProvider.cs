using System;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Libretro
{
	public partial class LibretroEmulator : ISoundProvider
	{
		private BlipBuffer _blipL;
		private BlipBuffer _blipR;

		private short[] _inSampBuf = new short[0];
		private short[] _outSampBuf = new short[0];
		private int _outSamps;

		private int _latchL = 0;
		private int _latchR = 0;

		private void SetupResampler(double fps, double sps)
		{
			Console.WriteLine("FPS {0} SPS {1}", fps, sps);

			_outSampBuf = new short[44100]; // big enough

			_blipL = new BlipBuffer(44100);
			_blipL.SetRates(sps, 44100);
			_blipR = new BlipBuffer(44100);
			_blipR.SetRates(sps, 44100);
		}

		private void ProcessSound()
		{
			var len = bridge.LibretroBridge_GetAudioSize(cbHandler);
			if (len == 0) // no audio?
			{
				return;
			}
			if (len > _inSampBuf.Length)
			{
				_inSampBuf = new short[len];
			}
			var ns = 0;
			bridge.LibretroBridge_GetAudio(cbHandler, ref ns, _inSampBuf);

			for (uint i = 0; i < ns; i++)
			{
				int curr = _inSampBuf[i * 2];

				if (curr != _latchL)
				{
					int diff = _latchL - curr;
					_latchL = curr;
					_blipL.AddDelta(i, diff);
				}

				curr = _inSampBuf[(i * 2) + 1];

				if (curr != _latchR)
				{
					int diff = _latchR - curr;
					_latchR = curr;
					_blipR.AddDelta(i, diff);
				}
			}

			_blipL.EndFrame((uint)ns);
			_blipR.EndFrame((uint)ns);
			_outSamps = _blipL.SamplesAvailable();
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
