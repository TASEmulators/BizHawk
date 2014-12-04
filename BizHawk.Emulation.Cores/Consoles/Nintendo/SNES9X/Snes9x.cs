using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Common;
using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES9X
{
	[CoreAttributes("Snes9x", "FIXME", true, false, "5e0319ab3ef9611250efb18255186d0dc0d7e125", "https://github.com/snes9xgit/snes9x")]
	public class Snes9x : IEmulator, IVideoProvider, ISyncSoundProvider
	{
		#region controller

		public ControllerDefinition ControllerDefinition
		{
			get { return NullEmulator.NullController; }
		}

		public IController Controller { get; set; }

		#endregion

		public void Dispose()
		{
		}

		[CoreConstructor("SNES")]
		public Snes9x(CoreComm comm, byte[] rom)
		{
			if (!LibSnes9x.debug_init(rom, rom.Length))
				throw new Exception();

			ServiceProvider = new BasicServiceProvider(this);
			CoreComm = comm;
		}

		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		public void FrameAdvance(bool render, bool rendersound = true)
		{
			Frame++;

			LibSnes9x.debug_advance(_vbuff);
		}

		public int Frame { get; private set; }

		public void ResetCounters()
		{
			Frame = 0;
		}

		public string SystemId { get { return "SNES"; } }
		public bool DeterministicEmulation { get { return true; } }
		public string BoardName { get { return null; } }
		public CoreComm CoreComm { get; private set; }

		#region IVideoProvider

		private int[] _vbuff = new int[512 * 480];
		public IVideoProvider VideoProvider { get { return this; } }
		public int[] GetVideoBuffer() { return _vbuff; }
		public int VirtualWidth
		{ get { return BufferWidth; } }
		public int VirtualHeight { get { return BufferHeight; } }
		public int BufferWidth { get { return 256; } }
		public int BufferHeight { get { return 224; } }
		public int BackgroundColor { get { return unchecked((int)0xff000000); } }

		#endregion

		#region ISyncSoundProvider

		private short[] _sbuff = new short[2048];
		public ISoundProvider SoundProvider { get { return null; } }
		public ISyncSoundProvider SyncSoundProvider { get { return this; } }
		public bool StartAsyncSound() { return false; }
		public void EndAsyncSound() { }

		public void GetSamples(out short[] samples, out int nsamp)
		{
			samples = _sbuff;
			nsamp = 735;
		}

		public void DiscardSamples()
		{
		}

		#endregion
	}
}
