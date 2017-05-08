using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components;
using BizHawk.Emulation.Cores.Components.Z80;

namespace BizHawk.Emulation.Cores.ColecoVision
{
	[CoreAttributes(
		"ColecoHawk",
		"Vecna",
		isPorted: false,
		isReleased: true)]
	[ServiceNotApplicable(typeof(ISaveRam), typeof(IDriveLight))]
	public sealed partial class ColecoVision : IEmulator, IDebuggable, IInputPollable, IStatable, ISettable<ColecoVision.ColecoSettings, ColecoVision.ColecoSyncSettings>
	{
		[CoreConstructor("Coleco")]
		public ColecoVision(CoreComm comm, GameInfo game, byte[] rom, object syncSettings)
		{
			ServiceProvider = new BasicServiceProvider(this);
			MemoryCallbacks = new MemoryCallbackSystem();
			CoreComm = comm;
			_syncSettings = (ColecoSyncSettings)syncSettings ?? new ColecoSyncSettings();
			bool skipbios = _syncSettings.SkipBiosIntro;

			Cpu = new Z80A
			{
				ReadMemory = ReadMemory,
				WriteMemory = WriteMemory,
				ReadHardware = ReadPort,
				WriteHardware = WritePort,
				MemoryCallbacks = MemoryCallbacks
			};

			PSG = new SN76489();
			_fakeSyncSound = new FakeSyncSound(PSG, 735);
			(ServiceProvider as BasicServiceProvider).Register<ISoundProvider>(_fakeSyncSound);

			ControllerDeck = new ColecoVisionControllerDeck(_syncSettings.Port1, _syncSettings.Port2);

			VDP = new TMS9918A(Cpu);
			(ServiceProvider as BasicServiceProvider).Register<IVideoProvider>(VDP);

			// TODO: hack to allow bios-less operation would be nice, no idea if its feasible
			BiosRom = CoreComm.CoreFileProvider.GetFirmware("Coleco", "Bios", true, "Coleco BIOS file is required.");

			// gamedb can overwrite the syncsettings; this is ok
			if (game["NoSkip"])
			{
				skipbios = false;
			}

			LoadRom(rom, skipbios);
			_game = game;
			SetupMemoryDomains();

			Tracer.Header = Cpu.TraceHeader;
			var serviceProvider = ServiceProvider as BasicServiceProvider;
			serviceProvider.Register<IDisassemblable>(new Disassembler());
			serviceProvider.Register<ITraceable>(Tracer);
		}

		// ROM
		private byte[] RomData;
		private byte[] BiosRom;

		// Machine
		private Z80A Cpu;
		private TMS9918A VDP;

		private byte[] Ram = new byte[1024];
		private readonly TraceBuffer Tracer = new TraceBuffer();

		public ColecoVisionControllerDeck ControllerDeck { get; private set; }

		private const ushort RamSizeMask = 0x03FF;

		private IController _controller;

		private void LoadRom(byte[] rom, bool skipbios)
		{
			RomData = new byte[0x8000];
			for (int i = 0; i < 0x8000; i++)
			{
				RomData[i] = rom[i % rom.Length];
			}

			// hack to skip colecovision title screen
			if (skipbios)
			{
				RomData[0] = 0x55;
				RomData[1] = 0xAA;
			}
		}

		private byte ReadPort(ushort port)
		{
			port &= 0xFF;

			if (port >= 0xA0 && port < 0xC0)
			{
				if ((port & 1) == 0)
				{
					return VDP.ReadData();
				}

				return VDP.ReadVdpStatus();
			}

			if (port >= 0xE0)
			{
				if ((port & 1) == 0)
				{
					return ReadController1();
				}

				return ReadController2();
			}

			return 0xFF;
		}

		private void WritePort(ushort port, byte value)
		{
			port &= 0xFF;

			if (port >= 0xA0 && port <= 0xBF)
			{
				if ((port & 1) == 0)
				{
					VDP.WriteVdpData(value);
				}
				else
				{
					VDP.WriteVdpControl(value);
				}

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

		private GameInfo _game;

		public enum InputPortMode { Left, Right }
		private InputPortMode InputPortSelection;

		private byte ReadController1()
		{
			_isLag = false;
			byte retval;
			if (InputPortSelection == InputPortMode.Left)
			{
				retval = ControllerDeck.ReadPort1(_controller, true, false);
				return retval;
			}

			if (InputPortSelection == InputPortMode.Right)
			{
				retval = ControllerDeck.ReadPort1(_controller, false, false);
				return retval;
			}
			return 0x7F;
		}

		private byte ReadController2()
		{
			_isLag = false;
			byte retval;
			if (InputPortSelection == InputPortMode.Left)
			{
				retval = ControllerDeck.ReadPort2(_controller, true, false);
				return retval;
			}

			if (InputPortSelection == InputPortMode.Right)
			{
				retval = ControllerDeck.ReadPort2(_controller, false, false);
				return retval;
			}
			return 0x7F;
		}

		private int frame;
	}
}