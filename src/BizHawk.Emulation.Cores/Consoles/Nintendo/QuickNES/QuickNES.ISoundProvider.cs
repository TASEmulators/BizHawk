using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES
{
	public partial class QuickNES : ISoundProvider
	{
		private readonly short[] _monoBuff = new short[1024];
		private readonly short[] _stereoBuff = new short[2048];
		private int _numSamples;

		public bool CanProvideAsync => false;

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			samples = _stereoBuff;
			nsamp = _numSamples;
		}

		public void DiscardSamples()
		{
			// Nothing to do
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

		private void InitAudio()
		{
			LibQuickNES.ThrowStringError(QN.qn_set_sample_rate(Context, 44100));
		}

		private void DrainAudio()
		{
			_numSamples = QN.qn_read_audio(Context, _monoBuff, _monoBuff.Length);
			unsafe
			{
				fixed (short* _src = &_monoBuff[0], _dst = &_stereoBuff[0])
				{
					short* src = _src;
					short* dst = _dst;
					for (int i = 0; i < _numSamples; i++)
					{
						*dst++ = *src;
						*dst++ = *src++;
					}
				}
			}
		}
	}
}
