using System;

using BizHawk.Emulation.Common;

using BizHawk.Emulation.Cores.Nintendo.NES;

namespace BizHawk.Emulation.Cores.Nintendo.SubNESHawk
{
	[Core(
		"SubNESHawk",
		"",
		isPorted: false,
		isReleased: false)]
	[ServiceNotApplicable(typeof(IDriveLight))]
	public partial class SubNESHawk : IEmulator, ISaveRam, IDebuggable, IStatable, IInputPollable, IRegionable,
	ISettable<SubNESHawk.SubNESHawkSettings, SubNESHawk.SubNESHawkSyncSettings>, INESPPUViewable
	{
		public NES.NES subnes;

		[CoreConstructor("NES")]
		public SubNESHawk(CoreComm comm, GameInfo game, byte[] rom, /*string gameDbFn,*/ object settings, object syncSettings)
		{
			var ser = new BasicServiceProvider(this);

			subnesSettings = (SubNESHawkSettings)settings ?? new SubNESHawkSettings();
			subnesSyncSettings = (SubNESHawkSyncSettings)syncSettings ?? new SubNESHawkSyncSettings();
			_controllerDeck = new SubNESHawkControllerDeck(SubNESHawkControllerDeck.DefaultControllerName, SubNESHawkControllerDeck.DefaultControllerName);

			CoreComm = comm;

			var temp_set = new NES.NES.NESSettings();

			var temp_sync = new NES.NES.NESSyncSettings();

			subnes = new NES.NES(new CoreComm(comm.ShowMessage, comm.Notify) { CoreFileProvider = comm.CoreFileProvider },
				game, rom, temp_set, temp_sync);

			ser.Register<IVideoProvider>(subnes.videoProvider);
			ser.Register<ISoundProvider>(subnes.magicSoundProvider); 

			_tracer = new TraceBuffer { Header = subnes.cpu.TraceHeader };
			ser.Register<ITraceable>(_tracer);

			ServiceProvider = ser;

			SetupMemoryDomains();

			HardReset();

			// input override for subframe input
			subnes.use_sub_input = true;
		}

		public void HardReset()
		{
			subnes.HardReset();
		}

		public void SoftReset()
		{
			subnes.Board.NESSoftReset();
			subnes.cpu.NESSoftReset();
			subnes.apu.NESSoftReset();
			subnes.ppu.NESSoftReset();
		}

		public DisplayType Region => DisplayType.NTSC;

		public int _frame = 0;

		private readonly SubNESHawkControllerDeck _controllerDeck;

		private readonly ITraceable _tracer;

		private void ExecFetch(ushort addr)
		{
			MemoryCallbacks.CallExecutes(addr, "System Bus");
		}

		#region PPU Viewable

		public int[] GetPalette()
		{
			return subnes.palette_compiled;
		}

		public bool BGBaseHigh
		{
			get { return subnes.ppu.reg_2000.bg_pattern_hi; }
		}

		public bool SPBaseHigh
		{
			get { return subnes.ppu.reg_2000.obj_pattern_hi; }
		}

		public bool SPTall
		{
			get { return subnes.ppu.reg_2000.obj_size_16; }
		}

		public byte[] GetPPUBus()
		{
			byte[] ret = new byte[0x3000];
			for (int i = 0; i < 0x3000; i++)
			{
				ret[i] = subnes.ppu.ppubus_peek(i);
			}
			return ret;
		}

		public byte[] GetPalRam()
		{
			return subnes.ppu.PALRAM;
		}

		public byte[] GetOam()
		{
			return subnes.ppu.OAM;
		}

		public byte PeekPPU(int addr)
		{
			return subnes.Board.PeekPPU(addr);
		}

		public byte[] GetExTiles()
		{
			if (subnes.Board is ExROM)
			{
				return subnes.Board.VROM ?? subnes.Board.VRAM;
			}
			else
			{
				throw new InvalidOperationException();
			}
		}

		public bool ExActive
		{
			get { return subnes.Board is ExROM && (subnes.Board as ExROM).ExAttrActive; }
		}

		public byte[] GetExRam()
		{
			if (subnes.Board is ExROM)
			{
				return (subnes.Board as ExROM).GetExRAMArray();
			}
			else
			{
				throw new InvalidOperationException();
			}
		}

		public MemoryDomain GetCHRROM()
		{
			return _memoryDomains["CHR VROM"];
		}


		public void InstallCallback1(Action cb, int sl)
		{
			subnes.ppu.NTViewCallback = new PPU.DebugCallback { Callback = cb, Scanline = sl };
		}

		public void InstallCallback2(Action cb, int sl)
		{
			subnes.ppu.PPUViewCallback = new PPU.DebugCallback { Callback = cb, Scanline = sl };
		}

		public void RemoveCallback1()
		{
			subnes.ppu.NTViewCallback = null;
		}

		public void RemoveCallback2()
		{
			subnes.ppu.PPUViewCallback = null;
		}

		#endregion
	}
}
