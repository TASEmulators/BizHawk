using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	public partial class AppleII : ISoundProvider
	{
		public bool CanProvideAsync => false;

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			_machine.Memory.Speaker.GetSamples(out samples, out nsamp);
		}

		public void DiscardSamples()
		{
			_machine.Memory.Speaker.Clear();
		}

		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode == SyncSoundMode.Async)
			{
				throw new NotSupportedException("Async mode is not supported.");
			}
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new InvalidOperationException("Async mode is not supported.");
		}
	}
}
