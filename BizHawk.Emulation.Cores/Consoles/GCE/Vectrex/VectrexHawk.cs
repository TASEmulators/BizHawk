using System;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.Components.MC6809;

using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
	[Core(
		"VectrexHawk",
		"",
		isPorted: false,
		isReleased: true)]
	[ServiceNotApplicable(typeof(IDriveLight))]
	public partial class VectrexHawk : IEmulator, ISaveRam, IDebuggable, IStatable, IInputPollable, IRegionable, 
	ISettable<VectrexHawk.VectrexSettings, VectrexHawk.VectrexSyncSettings>
	{
		public byte[] RAM = new byte[0x400];


		public byte[] _bios;
		public readonly byte[] _rom;	
		
		public readonly byte[] header = new byte[0x50];

		public byte[] cart_RAM;
		public bool has_bat;

		private int _frame = 0;

		public MapperBase mapper;

		private readonly ITraceable _tracer;

		public MC6809 cpu;
		public PPU ppu;
		public Audio audio;
		public SerialPort serialport;

		[CoreConstructor("VEC")]
		public VectrexHawk(CoreComm comm, GameInfo game, byte[] rom, /*string gameDbFn,*/ object settings, object syncSettings)
		{
			var ser = new BasicServiceProvider(this);

			cpu = new MC6809
			{
				ReadMemory = ReadMemory,
				WriteMemory = WriteMemory,
				PeekMemory = PeekMemory,
				DummyReadMemory = ReadMemory,
				OnExecFetch = ExecFetch,
			};

			audio = new Audio();
			ppu = new PPU();
			serialport = new SerialPort();

			CoreComm = comm;

			_settings = (VectrexSettings)settings ?? new VectrexSettings();
			_syncSettings = (VectrexSyncSettings)syncSettings ?? new VectrexSyncSettings();
			_controllerDeck = new VectrexHawkControllerDeck(_syncSettings.Port1);

			byte[] Bios = null;
			Bios = comm.CoreFileProvider.GetFirmware("Vectrex", "Bios", true, "BIOS Not Found, Cannot Load");			
			_bios = Bios;

			Buffer.BlockCopy(rom, 0x100, header, 0, 0x50);
			string hash_md5 = null;
			hash_md5 = "md5:" + rom.HashMD5(0, rom.Length);
			Console.WriteLine(hash_md5);

			_rom = rom;
			Setup_Mapper();

			_frameHz = 60;

			audio.Core = this;
			ppu.Core = this;
			serialport.Core = this;

			ser.Register<IVideoProvider>(this);
			ser.Register<ISoundProvider>(audio);
			ServiceProvider = ser;

			_settings = (VectrexSettings)settings ?? new VectrexSettings();
			_syncSettings = (VectrexSyncSettings)syncSettings ?? new VectrexSyncSettings();

			_tracer = new TraceBuffer { Header = cpu.TraceHeader };
			ser.Register<ITraceable>(_tracer);

			SetupMemoryDomains();
			HardReset();

			cpu.SetCallbacks(ReadMemory, PeekMemory, PeekMemory, WriteMemory);
		}

		public DisplayType Region => DisplayType.NTSC;

		private readonly VectrexHawkControllerDeck _controllerDeck;

		public void HardReset()
		{
			Register_Reset();
			ppu.Reset();
			audio.Reset();
			serialport.Reset();

			_vidbuffer = new int[VirtualWidth * VirtualHeight];
		}

		private void ExecFetch(ushort addr)
		{
			MemoryCallbacks.CallExecutes(addr, "System Bus");
		}

		private void Setup_Mapper()
		{
			mapper = new MapperDefault();
		}
	}
}
