using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Lynx
{
	public partial class Lynx : ISoundProvider
	{
		private readonly short[] _soundBuff = new short[2048];
		private int _numSamp;

		public bool CanProvideAsync => false;

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			samples = _soundBuff;
			nsamp = _numSamp;
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
	}
}
