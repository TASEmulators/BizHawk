using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sony.PSP
{
	public partial class PPSSPP : ISoundProvider
	{
		private short[] _sampleBuf = new short[1024 * 2];
		private int _nsamps;

		private void ProcessSound()
		{
			_core.PPSSPP_GetAudio(_context, out var buffer, out var frames);
			if (frames > _sampleBuf.Length)
			{
				_sampleBuf = new short[frames];
			}

			Marshal.Copy(buffer, _sampleBuf, 0, frames);
			_nsamps = frames / 2;
		}

		public bool CanProvideAsync => false;

		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		public void DiscardSamples()
		{
			_nsamps = 0;
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new NotSupportedException("Aync mode is not supported");
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			samples = _sampleBuf;
			nsamp = _nsamps;
			DiscardSamples();
		}

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode == SyncSoundMode.Async)
			{
				throw new NotSupportedException("Async mode is not supported");
			}
		}
	}
}