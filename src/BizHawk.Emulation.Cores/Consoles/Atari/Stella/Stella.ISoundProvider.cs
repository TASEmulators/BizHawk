using BizHawk.Emulation.Common;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Stella
{
	public partial class Stella : ISoundProvider
	{
		private BlipBuffer _blipL, _blipR;
		private int _latchL, _latchR;

		private readonly short[] _samples = new short[4096];
		private int _nsamp;

		public bool CanProvideAsync => false;

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			nsamp = _nsamp;
			samples = _samples;
			_nsamp = 0;
		}

		public void DiscardSamples()
			=> _nsamp = 0;

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

		private unsafe void UpdateAudio()
		{
			var src = IntPtr.Zero;
			var nsamp = 0;
			Core.stella_get_audio(ref nsamp, ref src);

			if (src != IntPtr.Zero)
			{
				using (_elf.EnterExit())
				{
					var samplePtr = (ushort*)src.ToPointer();
					for (uint i = 0; i < nsamp; i++)
					{
						int sample = *samplePtr++;
						if (sample != _latchL)
						{
							var diff = _latchL - sample;
							_latchL = sample;
							_blipL.AddDelta(i, diff);
						}

						sample = *samplePtr++;
						if (sample != _latchR)
						{
							var diff = _latchR - sample;
							_latchR = sample;
							_blipR.AddDelta(i, diff);
						}
					}
				}

				_blipL.EndFrame((uint)nsamp);
				_blipR.EndFrame((uint)nsamp);

				_nsamp = _blipL.SamplesAvailable();
				if (_nsamp != _blipR.SamplesAvailable())
				{
					throw new InvalidOperationException("Audio processing error");
				}

				_blipL.ReadSamplesLeft(_samples, _nsamp);
				_blipR.ReadSamplesRight(_samples, _nsamp);
			}
			else
			{
				_nsamp = 0;
			}
		}

		private void InitSound(int fps)
		{
			var sampleRate = fps switch
			{
				60 => 262 * 76 * 60 / 38, // 31440Hz
				50 => 312 * 76 * 50 / 38, // 31200Hz
				_ => throw new InvalidOperationException()
			};

			_blipL = new(2048);
			_blipL.SetRates(sampleRate, 44100);
			_blipR = new(2048);
			_blipR.SetRates(sampleRate, 44100);
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
