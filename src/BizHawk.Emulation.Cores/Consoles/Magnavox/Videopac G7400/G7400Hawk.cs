using System;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.I8048;

namespace BizHawk.Emulation.Cores.Consoles.G7400Hawk
{
	[Core(
		"G7400Hawk",
		"",
		isPorted: false,
		isReleased: false,
		displayName: "Videopac G7400")]
	[ServiceNotApplicable(new[] { typeof(IDriveLight) })]
	public partial class G7400Hawk : IEmulator, ISaveRam, IDebuggable, IInputPollable, IRegionable, ISettable<G7400Hawk.G7400Settings, G7400Hawk.G7400SyncSettings>, IBoardInfo
	{
		// memory domains
		public byte[] RAM = new byte[0x80];

		public byte addr_latch;
		public byte kb_byte;
		public bool ppu_en, RAM_en, kybrd_en, copy_en, cart_b0, cart_b1;
		public ushort rom_bank;
		public ushort bank_size;

		public byte[] _bios;
		public readonly byte[] _rom;
		public readonly byte[] header = new byte[0x50];

		public byte[] cart_RAM;
		public bool has_bat;

		public int _frame = 0;

		public MapperBase mapper;

		private readonly ITraceable _tracer;

		public I8048 cpu;
		public PPU ppu;

		public bool is_pal;

		[CoreConstructor("G7400")]
		public G7400Hawk(CoreComm comm, GameInfo game, byte[] rom, /*string gameDbFn,*/ object settings, object syncSettings)
		{
			var ser = new BasicServiceProvider(this);

			cpu = new I8048
			{
				ReadMemory = ReadMemory,
				WriteMemory = WriteMemory,
				PeekMemory = PeekMemory,
				DummyReadMemory = ReadMemory,
				ReadPort = ReadPort,
				WritePort = WritePort,
				OnExecFetch = ExecFetch,
			};

			_settings = (G7400Settings)settings ?? new G7400Settings();
			_syncSettings = (G7400SyncSettings)syncSettings ?? new G7400SyncSettings();
			_controllerDeck = new G7400HawkControllerDeck("G7400 Controller", "G7400 Controller");

			_bios = comm.CoreFileProvider.GetFirmware("G7400", "BIOS", true, "BIOS Not Found, Cannot Load")
				?? throw new MissingFirmwareException("Missing Odyssey2 Bios");

			Buffer.BlockCopy(rom, 0x100, header, 0, 0x50);

			Console.WriteLine("MD5: " + rom.HashMD5(0, rom.Length));
			Console.WriteLine("SHA1: " + rom.HashSHA1(0, rom.Length));
			_rom = rom;
			Setup_Mapper();

			_frameHz = 60;

			ser.Register<IVideoProvider>(this);
			ServiceProvider = ser;

			_settings = (G7400Settings)settings ?? new G7400Settings();
			_syncSettings = (G7400SyncSettings)syncSettings ?? new G7400SyncSettings();

			_tracer = new TraceBuffer { Header = cpu.TraceHeader };
			ser.Register(_tracer);
			ser.Register<IStatable>(new StateSerializer(SyncState));
			SetupMemoryDomains();
			cpu.SetCallbacks(ReadMemory, PeekMemory, PeekMemory, WriteMemory);

			// G7400 is PAL only
			is_pal = true;
			pic_height = 240;
			_frameHz = 50;
			ppu = new PAL_PPU();

			ppu.Core = this;

			ppu.set_region(is_pal);

			ser.Register<ISoundProvider>(ppu);

			_vidbuffer = new int[372 * pic_height];
			frame_buffer = new int[320 * pic_height];

			HardReset();
		}

		public DisplayType Region => DisplayType.PAL;

		private readonly G7400HawkControllerDeck _controllerDeck;

		public void HardReset()
		{
			in_vblank = true; // we start off in vblank since the LCD is off
			in_vblank_old = true;

			ppu.Reset();

			cpu.Reset();

			RAM = new byte[0x80];

			ticker = 0;

			// some of these get overwritten, but 
			addr_latch = 0;
			kb_state_row = kb_state_col = 0;

			// bank switching carts expect to be in upper bank on boot up, so can't have 0 at ports
			WritePort(1, 0xFF);
			WritePort(2, 0xFF);
		}

		public void SoftReset()
		{
			cpu.Reset();
		}

		public string BoardName => mapper.GetType().Name;

		// TODO: move callbacks to cpu to avoid non-inlinable function call
		private void ExecFetch(ushort addr)
		{
			if (MemoryCallbacks.HasExecutes)
			{
				uint flags = (uint)MemoryCallbackFlags.AccessExecute;
				MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
			}
		}

		private void Setup_Mapper()
		{
			mapper = new MapperDefault
			{
				Core = this
			};

			mapper.Initialize();

			// bank size is different for 12 k carts, it uses all 3k per bank. Note that A11 is held low by the CPU during interrupts
			// so this means 12k games use the upper 1k outside of vbl
			if (_rom.Length == 0x3000)
			{
				bank_size = 0xC00;
			}
			else
			{
				bank_size = 0x800;
			}
		}
	}
}
