using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class GambatteLink : ISoundProvider
	{
		public bool CanProvideAsync => false;

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode == SyncSoundMode.Async)
			{
				throw new NotSupportedException("Async mode is not supported.");
			}
		}

		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			nsamp = _sampleBufferContains;
			samples = SampleBuffer;
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new InvalidOperationException("Async mode is not supported.");
		}

		public void DiscardSamples()
		{
			_sampleBufferContains = 0;
		}

		// i tried using the left and right buffers and then mixing them together... it was kind of a mess of code, and slow
		private BlipBuffer _blipLeft;
		private BlipBuffer _blipRight;

		private readonly short[] LeftBuffer = new short[(35112 + 2064) * 2];
		private readonly short[] RightBuffer = new short[(35112 + 2064) * 2];

		private readonly short[] SampleBuffer = new short[1536];
		private int _sampleBufferContains = 0;

		private int _latchLeft;
		private int _latchRight;

		private unsafe void PrepSound()
		{
			fixed (short* sl = LeftBuffer, sr = RightBuffer)
			{
				for (uint i = 0; i < SampPerFrame * 2; i += 2)
				{
					int s = (sl[i] + sl[i + 1]) / 2;
					if (s != _latchLeft)
					{
						_blipLeft.AddDelta(i, s - _latchLeft);
						_latchLeft = s;
					}

					s = (sr[i] + sr[i + 1]) / 2;
					if (s != _latchRight)
					{
						_blipRight.AddDelta(i, s - _latchRight);
						_latchRight = s;
					}
				}
			}

			_blipLeft.EndFrame(SampPerFrame * 2);
			_blipRight.EndFrame(SampPerFrame * 2);
			int count = _blipLeft.SamplesAvailable();
			if (count != _blipRight.SamplesAvailable())
			{
				throw new Exception("Sound problem?");
			}

			// calling blip.Clear() causes rounding fractions to be reset,
			// and if only one channel is muted, in subsequent frames we can be off by a sample or two
			// not a big deal, but we didn't account for it.  so we actually complete the entire
			// audio read and then stamp it out if muted.
			_blipLeft.ReadSamplesLeft(SampleBuffer, count);
			if (L.Muted)
			{
				fixed (short* p = SampleBuffer)
				{
					for (int i = 0; i < SampleBuffer.Length; i += 2)
					{
						p[i] = 0;
					}
				}
			}

			_blipRight.ReadSamplesRight(SampleBuffer, count);
			if (R.Muted)
			{
				fixed (short* p = SampleBuffer)
				{
					for (int i = 1; i < SampleBuffer.Length; i += 2)
					{
						p[i] = 0;
					}
				}
			}

			_sampleBufferContains = count;
		}
	}
}
