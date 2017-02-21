using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components;
using BizHawk.Emulation.Cores.Components.Z80;

namespace BizHawk.Emulation.Cores.ColecoVision
{
	[CoreAttributes(
		"ColecoHawk",
		"Vecna",
		isPorted: false,
		isReleased: true
		)]
	[ServiceNotApplicable(typeof(ISaveRam), typeof(IDriveLight))]
	public sealed partial class ColecoVision : IEmulator, IDebuggable, IInputPollable, IStatable, ISettable<object, ColecoVision.ColecoSyncSettings>
	{
		// ROM
		public byte[] RomData;
		public int RomLength;
		public byte[] BiosRom;

		// Machine
		public Z80A Cpu;
		public TMS9918A VDP;
		
		public byte[] Ram = new byte[1024];
		private readonly TraceBuffer Tracer = new TraceBuffer();

		[CoreConstructor("Coleco")]
		public ColecoVision(CoreComm comm, GameInfo game, byte[] rom, object SyncSettings)
		{
			ServiceProvider = new BasicServiceProvider(this);
			MemoryCallbacks = new MemoryCallbackSystem();
			CoreComm = comm;
			_syncSettings = (ColecoSyncSettings)SyncSettings ?? new ColecoSyncSettings();
			bool skipbios = _syncSettings.SkipBiosIntro;

			Cpu = new Z80A();
			Cpu.ReadMemory = ReadMemory;
			Cpu.WriteMemory = WriteMemory;
			Cpu.ReadHardware = ReadPort;
			Cpu.WriteHardware = WritePort;
			Cpu.MemoryCallbacks = MemoryCallbacks;

			VDP = new TMS9918A(Cpu);
			(ServiceProvider as BasicServiceProvider).Register<IVideoProvider>(VDP);
			PSG = new SN76489();
			_fakeSyncSound = new FakeSyncSound(PSG, 735);
			(ServiceProvider as BasicServiceProvider).Register<ISoundProvider>(_fakeSyncSound);

			// TODO: hack to allow bios-less operation would be nice, no idea if its feasible
			BiosRom = CoreComm.CoreFileProvider.GetFirmware("Coleco", "Bios", true, "Coleco BIOS file is required.");

			// gamedb can overwrite the syncsettings; this is ok
			if (game["NoSkip"])
				skipbios = false;
			LoadRom(rom, skipbios);
			this.game = game;
			SetupMemoryDomains();

			Tracer.Header = Cpu.TraceHeader;
			var serviceProvider = ServiceProvider as BasicServiceProvider;
			serviceProvider.Register<IDisassemblable>(new Disassembler());
			serviceProvider.Register<ITraceable>(Tracer);
		}

		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		const ushort RamSizeMask = 0x03FF;

		public void FrameAdvance(bool render, bool renderSound)
		{
			Cpu.Debug = Tracer.Enabled;
			Frame++;
			_isLag = true;
			PSG.BeginFrame(Cpu.TotalExecutedCycles);

			if (Cpu.Debug && Cpu.Logger == null) // TODO, lets not do this on each frame. But lets refactor CoreComm/CoreComm first
			{
				Cpu.Logger = (s) => Tracer.Put(s);
			}

			VDP.ExecuteFrame();
			PSG.EndFrame(Cpu.TotalExecutedCycles);

			if (_isLag)
			{
				_lagCount++;
			}
		}

		void LoadRom(byte[] rom, bool skipbios)
		{
			RomData = new byte[0x8000];
			for (int i = 0; i < 0x8000; i++)
				RomData[i] = rom[i % rom.Length];

			// hack to skip colecovision title screen
			if (skipbios)
			{
				RomData[0] = 0x55;
				RomData[1] = 0xAA;
			}
		}

		byte ReadPort(ushort port)
		{
			port &= 0xFF;

			if (port >= 0xA0 && port < 0xC0)
			{
				if ((port & 1) == 0)
					return VDP.ReadData();
				return VDP.ReadVdpStatus();
			}

			if (port >= 0xE0)
			{
				if ((port & 1) == 0)
					return ReadController1();
				return ReadController2();
			}

			return 0xFF;
		}

		void WritePort(ushort port, byte value)
		{
			port &= 0xFF;

			if (port >= 0xA0 && port <= 0xBF)
			{
				if ((port & 1) == 0)
					VDP.WriteVdpData(value);
				else
					VDP.WriteVdpControl(value);
				return;
			}

			if (port >= 0x80 && port <= 0x9F)
			{
				InputPortSelection = InputPortMode.Right;
				return;
			}

			if (port >= 0xC0 && port <= 0xDF)
			{
				InputPortSelection = InputPortMode.Left;
				return;
			}

			if (port >= 0xE0)
			{
				PSG.WritePsgData(value, Cpu.TotalExecutedCycles);
				return;
			}
		}

		public bool DeterministicEmulation { get { return true; } }

		public void Dispose() { }
		public void ResetCounters()
		{
			Frame = 0;
			_lagCount = 0;
			_isLag = false;
		}

		public string SystemId { get { return "Coleco"; } }
		public GameInfo game;
		public CoreComm CoreComm { get; private set; }
		public string BoardName { get { return null; } }
	}
}