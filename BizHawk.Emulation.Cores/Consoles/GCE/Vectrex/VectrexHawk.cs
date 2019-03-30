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
		// declaractions
		// put top level core variables here
		// including things like RAM and BIOS
		// they will be used in the hex editor and others

		// the following declaraion is only an example
		// see memoryDomains.cs to see how it is used to define a Memory Domain that you can see in Hex editor
		// ex:
		public byte[] RAM = new byte[0x8000];


		public byte[] _bios;
		public readonly byte[] _rom;	
		
		// sometimes roms will have a header
		// the following is only an example in order to demonstrate how to extract the header
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

		private static byte[] GBA_override = { 0xFF, 0x00, 0xCD, 0x03, 0x35, 0xAA, 0x31, 0x90, 0x94, 0x00, 0x00, 0x00, 0x00 };

		[CoreConstructor("Vectrex")]
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

			// BIOS stuff can be tricky. Sometimes you'll have more then one vailable BIOS or different BIOSes for different regions
			// for now I suggest just picking one going
			byte[] Bios = null;
			//Bios = comm.CoreFileProvider.GetFirmware("Vectrex", "Bios", true, "BIOS Not Found, Cannot Load");			
			_bios = Bios;

			// the following few lines are jsut examples of working with a header and hashes
			Buffer.BlockCopy(rom, 0x100, header, 0, 0x50);
			string hash_md5 = null;
			hash_md5 = "md5:" + rom.HashMD5(0, rom.Length);
			Console.WriteLine(hash_md5);

			// in this case our working ROm has the header removed (might not be the case for your system)
			_rom = rom;
			Setup_Mapper();

			_frameHz = 60;

			// usually you want to have a reflected core available to the various components since they share some information
			audio.Core = this;
			ppu.Core = this;
			serialport.Core = this;

			// the following is just interface setup, dont worry to much about it
			ser.Register<IVideoProvider>(this);
			ser.Register<ISoundProvider>(audio);
			ServiceProvider = ser;

			_settings = (VectrexSettings)settings ?? new VectrexSettings();
			_syncSettings = (VectrexSyncSettings)syncSettings ?? new VectrexSyncSettings();

			_tracer = new TraceBuffer { Header = cpu.TraceHeader };
			ser.Register<ITraceable>(_tracer);

			SetupMemoryDomains();
			HardReset();
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

		// most systems have cartridges or other storage media that map memory in more then one way.
		// Use this ethod to set that stuff up when first starting the core
		private void Setup_Mapper()
		{
			mapper = new MapperDefault();
		}
	}
}
