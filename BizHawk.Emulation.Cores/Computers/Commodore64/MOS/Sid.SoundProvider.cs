using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	public sealed partial class Sid : ISoundProvider
	{
		public bool CanProvideAsync
		{
			get { return false; }
		}

		public SyncSoundMode SyncMode
		{
			get { return SyncSoundMode.Sync; }
		}

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode != SyncSoundMode.Sync)
			{
				throw new InvalidOperationException("Only Sync mode is supported.");
			}
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new NotSupportedException("Async is not available");
		}

		public void DiscardSamples()
		{
			_outputBufferIndex = 0;
		}

		// Expose this as GetSamplesAsync to support async sound
		// There's not need to do this though unless this core wants to handle async in its own way (the client can handle these situations if not available from the core)
		private void GetSamples(short[] samples)
		{
			Flush();
			var length = Math.Min(samples.Length, _outputBufferIndex);
			for (var i = 0; i < length; i++)
			{
				samples[i] = _outputBuffer[i];
			}
			_outputBufferIndex = 0;
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			Flush();
			samples = _outputBuffer;
			nsamp = _outputBufferIndex >> 1;
			_outputBufferIndex = 0;
		}
	}
}
