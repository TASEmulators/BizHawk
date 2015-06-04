using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	[CoreAttributes("mGBA", "endrift", true, false, "NOT DONE", "NOT DONE", false)]
	public class MGBAHawk : IEmulator, IVideoProvider
	{
		IntPtr core;

		[CoreConstructor("GBA")]
		public MGBAHawk(byte[] file, CoreComm comm)
		{
			var ser = new BasicServiceProvider(this);
			ser.Register<IDisassemblable>(new ArmV4Disassembler());
			ServiceProvider = ser;
			CoreComm = comm;

			core = LibmGBA.BizCreate();
			if (core == IntPtr.Zero)
				throw new InvalidOperationException("BizCreate() returned NULL!");
			try
			{
				if (!LibmGBA.BizLoad(core, file, file.Length))
				{
					throw new InvalidOperationException("BizLoad() returned FALSE!");
				}

			}
			catch
			{
				LibmGBA.BizDestroy(core);
				throw;
			}
		}

		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		public ISoundProvider SoundProvider { get { throw new InvalidOperationException(); } }

		public ISyncSoundProvider SyncSoundProvider { get { return new FakeSyncSound(new NullSound(), 735); } }

		public bool StartAsyncSound() { return false; }

		public void EndAsyncSound() { }

		public ControllerDefinition ControllerDefinition { get { return GBA.GBAController; } }

		public IController Controller { get; set; }

		public void FrameAdvance(bool render, bool rendersound = true)
		{
			IntPtr vp = IntPtr.Zero;
			LibmGBA.BizAdvance(core, 0, ref vp);
			Marshal.Copy(vp, videobuff, 0, 240 * 160);
		}

		public int Frame { get; private set; }

		public string SystemId { get { return "GBA"; } }

		public bool DeterministicEmulation { get { return true; } }

		public string BoardName { get { return null; } }

		public void ResetCounters()
		{
			throw new NotImplementedException();
		}

		public CoreComm CoreComm { get; private set; }

		public void Dispose()
		{
			if (core != IntPtr.Zero)
			{
				LibmGBA.BizDestroy(core);
				core = IntPtr.Zero;
			}
		}

		public int VirtualWidth { get { return 240; } }
		public int VirtualHeight { get { return 160; } }
		public int BufferWidth { get { return 240; } }
		public int BufferHeight { get { return 160; } }

		public int BackgroundColor
		{
			get { return unchecked((int)0xff000000); }
		}

		public int[] GetVideoBuffer()
		{
			return videobuff;
		}

		private int[] videobuff = new int[240 * 160];
	}
}
