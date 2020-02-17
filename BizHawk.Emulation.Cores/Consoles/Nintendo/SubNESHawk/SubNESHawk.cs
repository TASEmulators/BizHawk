using System;
using System.Linq;
using BizHawk.Emulation.Common;

using BizHawk.Emulation.Cores.Nintendo.NES;

namespace BizHawk.Emulation.Cores.Nintendo.SubNESHawk
{
	[Core(
		"SubNESHawk",
		"",
		isPorted: false,
		isReleased: true)]
	[ServiceNotApplicable(typeof(IDriveLight))]
	public partial class SubNESHawk : IEmulator, ISaveRam, IDebuggable, IStatable, IInputPollable, IRegionable,
	ISettable<NES.NES.NESSettings, NES.NES.NESSyncSettings>, INESPPUViewable
	{
		public NES.NES subnes;

		// needed for movies to accurately calculate timing
		public int VBL_CNT;

		[CoreConstructor("NES")]
		public SubNESHawk(CoreComm comm, GameInfo game, byte[] rom, /*string gameDbFn,*/ object settings, object syncSettings)
		{
			var ser = new BasicServiceProvider(this);

			subnesSettings = (NES.NES.NESSettings)settings ?? new NES.NES.NESSettings();
			subnesSyncSettings = (NES.NES.NESSyncSettings)syncSettings ?? new NES.NES.NESSyncSettings();

			CoreComm = comm;

			subnes = new NES.NES(new CoreComm(comm.ShowMessage, comm.Notify) { CoreFileProvider = comm.CoreFileProvider },
				game, rom, subnesSettings, subnesSyncSettings);

			ser.Register<IVideoProvider>(subnes.videoProvider);
			ser.Register<ISoundProvider>(subnes); 

			_tracer = new TraceBuffer { Header = "6502: PC, machine code, mnemonic, operands, registers (A, X, Y, P, SP), flags (NVTBDIZCR), CPU Cycle, PPU Cycle" };
			ser.Register<ITraceable>(_tracer);

			ServiceProvider = ser;

			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(subnes._memoryDomains);

			subnes.using_reset_timing = true;
			HardReset();
			current_cycle = 0;
			subnes.cpu.ext_ppu_cycle = current_cycle;
			VBL_CNT = 0;

			_nesStatable = subnes.ServiceProvider.GetService<IStatable>();
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
			current_cycle = 0;
			subnes.cpu.ext_ppu_cycle = current_cycle;
		}

		public DisplayType Region => DisplayType.NTSC;

		public int _frame = 0;

		public bool IsFDS => subnes.IsFDS;

		public bool IsVs => subnes.IsVS;

		public bool HasMapperProperties
		{
			get
			{
				var fields = subnes.Board.GetType().GetFields();
				foreach (var field in fields)
				{
					var attrib = field.GetCustomAttributes(typeof(MapperPropAttribute), false).OfType<MapperPropAttribute>().SingleOrDefault();
					if (attrib != null)
					{
						return true;
					}
				}

				return false;
			}
		}

		private readonly ITraceable _tracer;

		#region ISettable
		private NES.NES.NESSettings subnesSettings = new NES.NES.NESSettings();
		public NES.NES.NESSyncSettings subnesSyncSettings = new NES.NES.NESSyncSettings();

		public NES.NES.NESSettings GetSettings()
		{
			return subnesSettings.Clone();
		}

		public NES.NES.NESSyncSettings GetSyncSettings()
		{
			return subnesSyncSettings.Clone();
		}

		public bool PutSettings(NES.NES.NESSettings o)
		{
			subnesSettings = o;
			if (subnesSettings.ClipLeftAndRight)
			{
				subnes.videoProvider.left = 8;
				subnes.videoProvider.right = 247;
			}
			else
			{
				subnes.videoProvider.left = 0;
				subnes.videoProvider.right = 255;
			}

			CoreComm.ScreenLogicalOffsetX = subnes.videoProvider.left;
			CoreComm.ScreenLogicalOffsetY = Region == DisplayType.NTSC ? subnesSettings.NTSC_TopLine : subnesSettings.PAL_TopLine;

			subnes.SetPalette(subnesSettings.Palette);

			subnes.apu.m_vol = subnesSettings.APU_vol;

			return false;
		}

		public bool PutSyncSettings(NES.NES.NESSyncSettings o)
		{
			bool ret = NES.NES.NESSyncSettings.NeedsReboot(subnesSyncSettings, o);
			subnesSyncSettings = o;
			return ret;
		}
		#endregion

		#region PPU Viewable

		public int[] GetPalette()
		{
			return subnes.palette_compiled;
		}

		public bool BGBaseHigh => subnes.ppu.reg_2000.bg_pattern_hi;

		public bool SPBaseHigh => subnes.ppu.reg_2000.obj_pattern_hi;

		public bool SPTall => subnes.ppu.reg_2000.obj_size_16;

		public byte[] GetPPUBus()
		{
			byte[] ret = new byte[0x3000];
			for (int i = 0; i < 0x3000; i++)
			{
				ret[i] = subnes.ppu.ppubus_peek(i);
			}
			return ret;
		}

		public byte[] GetPalRam() => subnes.ppu.PALRAM;

		public byte[] GetOam() => subnes.ppu.OAM;

		public byte PeekPPU(int addr) => subnes.Board.PeekPPU(addr);

		public byte[] GetExTiles()
		{
			if (subnes.Board is ExROM)
			{
				return subnes.Board.VROM ?? subnes.Board.VRAM;
			}
			
			throw new InvalidOperationException();
		}

		public bool ExActive => subnes.Board is ExROM && (subnes.Board as ExROM).ExAttrActive;

		public byte[] GetExRam()
		{
			if (subnes.Board is ExROM)
			{
				return (subnes.Board as ExROM).GetExRAMArray();
			}

			throw new InvalidOperationException();
		}

		public MemoryDomain GetCHRROM() => _memoryDomains["CHR VROM"];

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
