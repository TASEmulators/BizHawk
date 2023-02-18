using System;
using BizHawk.Emulation.Common;
using System.Runtime.InteropServices;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	public partial class GPGX : ISyncSoundProvider
	{
		private readonly short[] _samples = new short[4096];
		private int _nsamp;

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			nsamp = _nsamp;
			samples = _samples;
			_nsamp = 0;
		}

		public void DiscardSamples()
		{
			_nsamp = 0;
		}

		private void update_audio()
		{
			IntPtr src = IntPtr.Zero;
			Core.gpgx_get_audio(ref _nsamp, ref src);
			if (src != IntPtr.Zero)
			{
				using (_elf.EnterExit())
					Marshal.Copy(src, _samples, 0, _nsamp * 2);
			}
		}
	}
}
