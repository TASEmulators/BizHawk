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
	public class MGBAHawk : IEmulator, IVideoProvider, ISyncSoundProvider
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

		public ControllerDefinition ControllerDefinition { get { return GBA.GBAController; } }

		public IController Controller { get; set; }

		public void FrameAdvance(bool render, bool rendersound = true)
		{
			Frame++;
			LibmGBA.BizAdvance(core, 0, videobuff, ref nsamp, soundbuff);
		}

		public int Frame { get; private set; }

		public string SystemId { get { return "GBA"; } }

		public bool DeterministicEmulation { get { return true; } }

		public string BoardName { get { return null; } }

		public void ResetCounters()
		{
			Frame = 0;
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

		#region IVideoProvider
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
		private readonly int[] videobuff = new int[240 * 160];
		#endregion

		#region ISoundProvider
		private readonly short[] soundbuff = new short[2048];
		private int nsamp;
		public void GetSamples(out short[] samples, out int nsamp)
		{
			nsamp = this.nsamp;
			samples = soundbuff;
			DiscardSamples();
		}
		public void DiscardSamples()
		{
			nsamp = 0;
		}
		public ISoundProvider SoundProvider { get { throw new InvalidOperationException(); } }
		public ISyncSoundProvider SyncSoundProvider { get { return this; } }
		public bool StartAsyncSound() { return false; }
		public void EndAsyncSound() { }
		#endregion
	}
}
