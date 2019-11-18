using System;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.Components.I8048;

using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Nintendo.O2Hawk
{
	[Core(
		"O2Hawk",
		"",
		isPorted: false,
		isReleased: false)]
	[ServiceNotApplicable(typeof(IDriveLight))]
	public partial class O2Hawk : IEmulator, ISaveRam, IDebuggable, IStatable, IInputPollable, IRegionable, ISettable<O2Hawk.O2Settings, O2Hawk.O2SyncSettings>
	{
		public byte input_register;

		// memory domains
		public byte[] RAM = new byte[0x80];

		public byte[] VRAM = new byte[0x4000];
		public byte[] OAM = new byte[0xA0];

		public int RAM_Bank;

		public byte[] _bios;
		public readonly byte[] _rom;		
		public readonly byte[] header = new byte[0x50];

		public byte[] cart_RAM;
		public bool has_bat;

		private int _frame = 0;

		public ushort addr_access;

		public MapperBase mapper;

		private readonly ITraceable _tracer;

		public I8048 cpu;
		public PPU ppu;
		public Audio audio;
		public SerialPort serialport;

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
			
			audio = new Audio();
			serialport = new SerialPort();

			CoreComm = comm;

			_settings = (O2Settings)settings ?? new O2Settings();
			_syncSettings = (O2SyncSettings)syncSettings ?? new O2SyncSettings();
			_controllerDeck = new O2HawkControllerDeck("O2 Controller", "O2 Controller");

			byte[] Bios = null;

			Bios = comm.CoreFileProvider.GetFirmware("O2", "World", true, "BIOS Not Found, Cannot Load");
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

			audio.Core = this;
			ppu.Core = this;
			serialport.Core = this;

			ser.Register<IVideoProvider>(this);
			ser.Register<ISoundProvider>(audio);
			ServiceProvider = ser;

			_settings = (O2Settings)settings ?? new O2Settings();
			_syncSettings = (O2SyncSettings)syncSettings ?? new O2SyncSettings();

			_tracer = new TraceBuffer { Header = cpu.TraceHeader };
			ser.Register<ITraceable>(_tracer);

			SetupMemoryDomains();
			HardReset();
		}

		public DisplayType Region => DisplayType.NTSC;

		private readonly O2HawkControllerDeck _controllerDeck;

		public void HardReset()
		{
			in_vblank = true; // we start off in vblank since the LCD is off
			in_vblank_old = true;

			RAM_Bank = 1; // RAM bank always starts as 1 (even writing zero still sets 1)

			ppu.Reset();
			audio.Reset();
			serialport.Reset();

			cpu.SetCallbacks(ReadMemory, PeekMemory, PeekMemory, WriteMemory);

			_vidbuffer = new int[VirtualWidth * VirtualHeight];
			frame_buffer = new int[VirtualWidth * VirtualHeight];
		}

		private void ExecFetch(ushort addr)
		{
			uint flags = (uint)(MemoryCallbackFlags.AccessRead);
			MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
		}

		private void Setup_Mapper()
		{

		}
	}
}
