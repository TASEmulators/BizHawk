using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class Gameboy : ISoundProvider
	{
		public bool CanProvideAsync
		{
			get { return false; }
		}

		public void DiscardSamples()
		{
			soundoutbuffcontains = 0;
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			samples = soundoutbuff;
			nsamp = soundoutbuffcontains;
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

		internal bool Muted
		{
			get { return _settings.Muted; }
		}

		// sample pairs before resampling
		private short[] soundbuff = new short[(35112 + 2064) * 2];

		private int soundoutbuffcontains = 0;

		private short[] soundoutbuff = new short[2048];

		private int latchL = 0;
		private int latchR = 0;

		private BlipBuffer blipL, blipR;
		private uint blipAccumulate;

		private void ProcessSound(int nsamp)
		{
			for (uint i = 0; i < nsamp; i++)
			{
				int curr = soundbuff[i * 2];

				if (curr != latchL)
				{
					int diff = latchL - curr;
					latchL = curr;
					blipL.AddDelta(blipAccumulate, diff);
				}

				curr = soundbuff[i * 2 + 1];

				if (curr != latchR)
				{
					int diff = latchR - curr;
					latchR = curr;
					blipR.AddDelta(blipAccumulate, diff);
				}

				blipAccumulate++;
			}
		}

		private void ProcessSoundEnd()
		{
			blipL.EndFrame(blipAccumulate);
			blipR.EndFrame(blipAccumulate);
			blipAccumulate = 0;

			soundoutbuffcontains = blipL.SamplesAvailable();
			if (soundoutbuffcontains != blipR.SamplesAvailable())
			{
				throw new InvalidOperationException("Audio processing error");
			}

			blipL.ReadSamplesLeft(soundoutbuff, soundoutbuffcontains);
			blipR.ReadSamplesRight(soundoutbuff, soundoutbuffcontains);
		}

		private void InitSound()
		{
			blipL = new BlipBuffer(1024);
			blipL.SetRates(TICKSPERSECOND, 44100);
			blipR = new BlipBuffer(1024);
			blipR.SetRates(TICKSPERSECOND, 44100);
		}

		private void DisposeSound()
		{
			if (blipL != null)
			{
				blipL.Dispose();
				blipL = null;
			}
			if (blipR != null)
			{
				blipR.Dispose();
				blipR = null;
			}
		}
	}
}
