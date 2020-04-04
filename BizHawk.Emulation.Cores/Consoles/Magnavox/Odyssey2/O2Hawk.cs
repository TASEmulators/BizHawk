using System;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.I8048;

namespace BizHawk.Emulation.Cores.Consoles.O2Hawk
{
	[Core(
		"O2Hawk",
		"",
		isPorted: false,
		isReleased: false)]
	[ServiceNotApplicable(new[] { typeof(IDriveLight) })]
	public partial class O2Hawk : IEmulator, ISaveRam, IDebuggable, IInputPollable, IRegionable, ISettable<O2Hawk.O2Settings, O2Hawk.O2SyncSettings>
	{
		// memory domains
		public byte[] RAM = new byte[0x80];

		public byte addr_latch;
		public byte kb_byte;
		public bool ppu_en, RAM_en, kybrd_en, copy_en, cart_b0, cart_b1;
		public ushort rom_bank;

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

		[CoreConstructor("O2")]
		public O2Hawk(CoreComm comm, GameInfo game, byte[] rom, /*string gameDbFn,*/ object settings, object syncSettings)
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

			_settings = (O2Settings)settings ?? new O2Settings();
			_syncSettings = (O2SyncSettings)syncSettings ?? new O2SyncSettings();
			_controllerDeck = new O2HawkControllerDeck("O2 Controller", "O2 Controller");

			byte[] Bios = null;

			Bios = comm.CoreFileProvider.GetFirmware("O2", "BIOS", true, "BIOS Not Found, Cannot Load");
			ppu = new PPU();

			if (Bios == null)
			{
				throw new MissingFirmwareException("Missing Odyssey2 Bios");
			}

			_bios = Bios;

			Buffer.BlockCopy(rom, 0x100, header, 0, 0x50);

			Console.WriteLine("MD5: " + rom.HashMD5(0, rom.Length));
			Console.WriteLine("SHA1: " + rom.HashSHA1(0, rom.Length));
			_rom = rom;
			Setup_Mapper();

			_frameHz = 60;

			ppu.Core = this;
			cpu.Core = this;

			ser.Register<IVideoProvider>(this);
			ser.Register<ISoundProvider>(ppu);
			ServiceProvider = ser;

			_settings = (O2Settings)settings ?? new O2Settings();
			_syncSettings = (O2SyncSettings)syncSettings ?? new O2SyncSettings();

			_tracer = new TraceBuffer { Header = cpu.TraceHeader };
			ser.Register<ITraceable>(_tracer);
			ser.Register<IStatable>(new StateSerializer(SyncState));
			SetupMemoryDomains();
			cpu.SetCallbacks(ReadMemory, PeekMemory, PeekMemory, WriteMemory);

			HardReset();

			// set up differences between PAL and NTSC systems
			is_pal = true;
			pic_height = 288;
			_frameHz = 50;

			if ((game.Region == "US") || (game.Region == "EU-US") || (game.Region == null))
			{
				is_pal = false;
				pic_height = 240;
				_frameHz = 60;
			}

			ppu.set_region(is_pal);


			_vidbuffer = new int[372 * pic_height];
			frame_buffer = new int[320 * pic_height];
		}

		public DisplayType Region => DisplayType.NTSC;

		private readonly O2HawkControllerDeck _controllerDeck;

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

		private void ExecFetch(ushort addr)
		{
			uint flags = (uint)(MemoryCallbackFlags.AccessRead);
			MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
		}

		private void Setup_Mapper()
		{
			mapper = new MapperDefault();
			mapper.Core = this;
		}
	}
}
