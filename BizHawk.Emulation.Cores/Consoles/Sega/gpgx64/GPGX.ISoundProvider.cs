using System;
using BizHawk.Emulation.Common;
using System.Runtime.InteropServices;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	public partial class GPGX : ISoundProvider
	{
		private short[] samples = new short[4096];
		private int nsamp = 0;

		public bool CanProvideAsync
		{
			get { return false; }
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			nsamp = this.nsamp;
			samples = this.samples;
			this.nsamp = 0;
		}

		public void DiscardSamples()
		{
			this.nsamp = 0;
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

		private void update_audio()
		{
			IntPtr src = IntPtr.Zero;
			Core.gpgx_get_audio(ref nsamp, ref src);
			if (src != IntPtr.Zero)
			{
				using (_elf.EnterExit())
					Marshal.Copy(src, samples, 0, nsamp * 2);
			}
		}
	}
}
