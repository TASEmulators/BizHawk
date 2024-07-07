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
		private readonly BlipBuffer[] _linkedBlips;

		private readonly short[] SoundBuffer;

		private readonly short[] ScratchBuffer = new short[1536];

		private readonly short[] SampleBuffer = new short[1536];
		private int _sampleBufferContains = 0;

		private readonly int[] _linkedLatches;

		private unsafe void PrepSound()
		{
			fixed (short* sb = &SoundBuffer[0])
			{
				for (int i = 0; i < _numCores; i++)
				{
					for (uint j = 0; j < SampPerFrame * 2; j += 2)
					{
						int s = (sb[(i * MaxSampsPerFrame) + j] + sb[(i * MaxSampsPerFrame) + j + 1]) / 2;
						if (s != _linkedLatches[i])
						{
							_linkedBlips[i].AddDelta(j, s - _linkedLatches[i]);
							_linkedLatches[i] = s;
						}
					}
					_linkedBlips[i].EndFrame(SampPerFrame * 2);
				}
			}

			int count = _linkedBlips[P1].SamplesAvailable();
			for (int i = 1; i < _numCores; i++)
			{
				if (count != _linkedBlips[i].SamplesAvailable())
				{
					throw new Exception("Sound problem?");
				}
			}

			// calling blip.Clear() causes rounding fractions to be reset,
			// and if only one channel is muted, in subsequent frames we can be off by a sample or two
			// not a big deal, but we didn't account for it.  so we actually complete the entire
			// audio read and then stamp it out if muted.

			switch (_numCores)
			{
				case 2:
					{
						// no need to do any complicated mixing
						_linkedBlips[P1].ReadSamplesLeft(_settings._linkedSettings[P1].Muted ? ScratchBuffer : SampleBuffer, count);
						_linkedBlips[P2].ReadSamplesRight(_settings._linkedSettings[P2].Muted ? ScratchBuffer : SampleBuffer, count);
						break;
					}
				case 3:
					{
						// since P2 is center, mix its samples with P1 and P3
						_linkedBlips[P1].ReadSamplesLeft(_settings._linkedSettings[P1].Muted ? ScratchBuffer : SampleBuffer, count);
						_linkedBlips[P3].ReadSamplesRight(_settings._linkedSettings[P3].Muted ? ScratchBuffer : SampleBuffer, count);
						_linkedBlips[P2].ReadSamplesLeft(ScratchBuffer, count);
						if (!_settings._linkedSettings[P2].Muted)
						{
							fixed (short* p = SampleBuffer, q = ScratchBuffer)
							{
								if (_settings._linkedSettings[P1].Muted && _settings._linkedSettings[P3].Muted)
								{
									for (int i = 0; i < SampleBuffer.Length; i += 2)
									{
										p[i] = q[i];
										p[i + 1] = q[i];
									}
								}
								else if (_settings._linkedSettings[P1].Muted)
								{
									for (int i = 0; i < SampleBuffer.Length; i += 2)
									{
										p[i] = q[i];
										int s = (p[i + 1] + q[i]) / 2;
										p[i + 1] = (short)s;
									}
								}
								else if (_settings._linkedSettings[P3].Muted)
								{
									for (int i = 0; i < SampleBuffer.Length; i += 2)
									{
										int s = (p[i] + q[i]) / 2;
										p[i] = (short)s;
										p[i + 1] = q[i];
									}
								}
								else
								{
									for (int i = 0; i < SampleBuffer.Length; i += 2)
									{
										int s = (p[i] + q[i]) / 2;
										p[i] = (short)s;
										s = (p[i + 1] + q[i]) / 2;
										p[i + 1] = (short)s;
									}
								}
							}
						}
						break;
					}
				case 4:
					{
						// since P1 and P2 are left side and P3 and P4 are right side, mix their samples accordingly
						_linkedBlips[P1].ReadSamplesLeft(_settings._linkedSettings[P1].Muted ? ScratchBuffer : SampleBuffer, count);
						_linkedBlips[P3].ReadSamplesRight(_settings._linkedSettings[P3].Muted ? ScratchBuffer : SampleBuffer, count);
						_linkedBlips[P2].ReadSamplesLeft(ScratchBuffer, count);
						_linkedBlips[P4].ReadSamplesRight(ScratchBuffer, count);
						if (_settings._linkedSettings[P2].Muted)
						{
							fixed (short* p = ScratchBuffer)
							{
								for (int i = 0; i < ScratchBuffer.Length; i += 2)
								{
									p[i] = 0;
								}
							}
						}
						if (_settings._linkedSettings[P4].Muted)
						{
							fixed (short* p = ScratchBuffer)
							{
								for (int i = 1; i < ScratchBuffer.Length; i += 2)
								{
									p[i] = 0;
								}
							}
						}
						fixed (short* p = SampleBuffer, q = ScratchBuffer)
						{
							for (int i = 0; i < SampleBuffer.Length; i++)
							{
								int s = (p[i] + q[i]) / 2;
								p[i] = (short)s;
							}
						}
						break;
					}
				default:
					throw new Exception();
			}

			_sampleBufferContains = count;
		}
	}
}
