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
	public class MGBAHawk : IEmulator, IVideoProvider, ISyncSoundProvider, IGBAGPUViewable
	{
		IntPtr core;

		[CoreConstructor("GBA")]
		public MGBAHawk(byte[] file, CoreComm comm)
		{
			byte[] bios = null;
			if (true) // TODO: config me
			{
				bios = comm.CoreFileProvider.GetFirmware("GBA", "Bios", true);
			}

			if (bios != null && bios.Length != 16384)
			{
				throw new InvalidOperationException("BIOS must be exactly 16384 bytes!");
			}
			core = LibmGBA.BizCreate(bios);
			if (core == IntPtr.Zero)
			{
				throw new InvalidOperationException("BizCreate() returned NULL!  Bad BIOS?");
			}
			try
			{
				if (!LibmGBA.BizLoad(core, file, file.Length))
				{
					throw new InvalidOperationException("BizLoad() returned FALSE!  Bad ROM?");
				}

				var ser = new BasicServiceProvider(this);
				ser.Register<IDisassemblable>(new ArmV4Disassembler());
				ser.Register<IMemoryDomains>(CreateMemoryDomains(file.Length));

				ServiceProvider = ser;
				CoreComm = comm;
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
			if (Controller["Power"])
				LibmGBA.BizReset(core);

			LibmGBA.BizAdvance(core, VBANext.GetButtons(Controller), videobuff, ref nsamp, soundbuff);
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
			Console.WriteLine(nsamp);
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

		#region IMemoryDomains

		private MemoryDomainList CreateMemoryDomains(int romsize)
		{
			var s = new LibmGBA.MemoryAreas();
			var mm = new List<MemoryDomain>();
			LibmGBA.BizGetMemoryAreas(core, s);

			var l = MemoryDomain.Endian.Little;
			mm.Add(MemoryDomain.FromIntPtr("IWRAM", 32 * 1024, l, s.iwram, true, 4));
			mm.Add(MemoryDomain.FromIntPtr("EWRAM", 256 * 1024, l, s.wram, true, 4));
			mm.Add(MemoryDomain.FromIntPtr("BIOS", 16 * 1024, l, s.bios, false, 4));
			mm.Add(MemoryDomain.FromIntPtr("PALRAM", 1024, l, s.palram, false, 4));
			mm.Add(MemoryDomain.FromIntPtr("VRAM", 96 * 1024, l, s.vram, true, 4));
			mm.Add(MemoryDomain.FromIntPtr("OAM", 1024, l, s.oam, false, 4));
			mm.Add(MemoryDomain.FromIntPtr("ROM", romsize, l, s.rom, false, 4));

			_gpumem = new GBAGPUMemoryAreas
			{
				mmio = s.mmio,
				oam = s.oam,
				palram = s.palram,
				vram = s.vram
			};

			return new MemoryDomainList(mm);

		}

		#endregion

		private GBAGPUMemoryAreas _gpumem;

		public GBAGPUMemoryAreas GetMemoryAreas()
		{
			return _gpumem;
		}

		[FeatureNotImplemented]
		public void SetScanlineCallback(Action callback, int scanline)
		{
		}
	}
}
