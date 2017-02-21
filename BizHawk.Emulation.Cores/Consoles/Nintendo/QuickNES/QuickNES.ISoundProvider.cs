using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES
{
	public partial class QuickNES : ISoundProvider
	{
		private short[] MonoBuff = new short[1024];
		private short[] StereoBuff = new short[2048];
		private int NumSamples = 0;

		public bool CanProvideAsync
		{
			get { return false; }
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			samples = StereoBuff;
			nsamp = NumSamples;
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

		public SyncSoundMode SyncMode
		{
			get { return SyncSoundMode.Sync; }
		}

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
			NumSamples = QN.qn_read_audio(Context, MonoBuff, MonoBuff.Length);
			unsafe
			{
				fixed (short* _src = &MonoBuff[0], _dst = &StereoBuff[0])
				{
					short* src = _src;
					short* dst = _dst;
					for (int i = 0; i < NumSamples; i++)
					{
						*dst++ = *src;
						*dst++ = *src++;
					}
				}
			}
		}
	}
}
