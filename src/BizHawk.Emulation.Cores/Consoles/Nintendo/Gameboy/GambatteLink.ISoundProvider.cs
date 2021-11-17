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
		private BlipBuffer[] _linkedBlips;

		private readonly short[][] _linkedSoundBuffers;

		private readonly short[] SampleBuffer = new short[1536];
		private int _sampleBufferContains = 0;

		private readonly int[] _linkedLatches;

		private unsafe void PrepSound()
		{
			fixed (short* sl = _linkedSoundBuffers[0], sr = _linkedSoundBuffers[1])
			{
				for (uint i = 0; i < SampPerFrame * 2; i += 2)
				{
					int s = (sl[i] + sl[i + 1]) / 2;
					if (s != _linkedLatches[0])
					{
						_linkedBlips[0].AddDelta(i, s - _linkedLatches[0]);
						_linkedLatches[0] = s;
					}

					s = (sr[i] + sr[i + 1]) / 2;
					if (s != _linkedLatches[1])
					{
						_linkedBlips[1].AddDelta(i, s - _linkedLatches[1]);
						_linkedLatches[1] = s;
					}
				}
			}

			_linkedBlips[0].EndFrame(SampPerFrame * 2);
			_linkedBlips[1].EndFrame(SampPerFrame * 2);
			int count = _linkedBlips[0].SamplesAvailable();
			if (count != _linkedBlips[1].SamplesAvailable())
			{
				throw new Exception("Sound problem?");
			}

			// calling blip.Clear() causes rounding fractions to be reset,
			// and if only one channel is muted, in subsequent frames we can be off by a sample or two
			// not a big deal, but we didn't account for it.  so we actually complete the entire
			// audio read and then stamp it out if muted.
			_linkedBlips[0].ReadSamplesLeft(SampleBuffer, count);
			if (_settings._linkedSettings[0].Muted)
			{
				fixed (short* p = SampleBuffer)
				{
					for (int i = 0; i < SampleBuffer.Length; i += 2)
					{
						p[i] = 0;
					}
				}
			}

			_linkedBlips[1].ReadSamplesRight(SampleBuffer, count);
			if (_settings._linkedSettings[1].Muted)
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
