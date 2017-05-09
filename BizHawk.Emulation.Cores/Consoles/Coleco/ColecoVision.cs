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
			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;
			MemoryCallbacks = new MemoryCallbackSystem();
			CoreComm = comm;
			_syncSettings = (ColecoSyncSettings)syncSettings ?? new ColecoSyncSettings();
			bool skipbios = _syncSettings.SkipBiosIntro;

			_cpu = new Z80A
			{
				ReadMemory = ReadMemory,
				WriteMemory = WriteMemory,
				ReadHardware = ReadPort,
				WriteHardware = WritePort,
				MemoryCallbacks = MemoryCallbacks
			};

			PSG = new SN76489();
			_fakeSyncSound = new FakeSyncSound(PSG, 735);
			ser.Register<ISoundProvider>(_fakeSyncSound);

			ControllerDeck = new ColecoVisionControllerDeck(_syncSettings.Port1, _syncSettings.Port2);

			_vdp = new TMS9918A(_cpu);
			ser.Register<IVideoProvider>(_vdp);

			// TODO: hack to allow bios-less operation would be nice, no idea if its feasible
			_biosRom = CoreComm.CoreFileProvider.GetFirmware("Coleco", "Bios", true, "Coleco BIOS file is required.");

			// gamedb can overwrite the syncsettings; this is ok
			if (game["NoSkip"])
			{
				skipbios = false;
			}

			LoadRom(rom, skipbios);
			SetupMemoryDomains();

			_tracer.Header = _cpu.TraceHeader;
			ser.Register<IDisassemblable>(new Disassembler());
			ser.Register<ITraceable>(_tracer);
		}

		private readonly Z80A _cpu;
		private readonly TMS9918A _vdp;
		private readonly byte[] _biosRom;
		private readonly TraceBuffer _tracer = new TraceBuffer();

		private byte[] _romData;
		private byte[] _ram = new byte[1024];
		private int _frame;
		private IController _controller;

		private enum InputPortMode
		{
			Left, Right
		}

		private InputPortMode _inputPortSelection;

		public ColecoVisionControllerDeck ControllerDeck { get; }

		private void LoadRom(byte[] rom, bool skipbios)
		{
			_romData = new byte[0x8000];
			for (int i = 0; i < 0x8000; i++)
			{
				_romData[i] = rom[i % rom.Length];
			}

			// hack to skip colecovision title screen
			if (skipbios)
			{
				_romData[0] = 0x55;
				_romData[1] = 0xAA;
			}
		}

		private byte ReadPort(ushort port)
		{
			port &= 0xFF;

			if (port >= 0xA0 && port < 0xC0)
			{
				if ((port & 1) == 0)
				{
					return _vdp.ReadData();
				}

				return _vdp.ReadVdpStatus();
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
					_vdp.WriteVdpData(value);
				}
				else
				{
					_vdp.WriteVdpControl(value);
				}

				return;
			}

			if (port >= 0x80 && port <= 0x9F)
			{
				_inputPortSelection = InputPortMode.Right;
				return;
			}

			if (port >= 0xC0 && port <= 0xDF)
			{
				_inputPortSelection = InputPortMode.Left;
				return;
			}

			if (port >= 0xE0)
			{
				PSG.WritePsgData(value, _cpu.TotalExecutedCycles);
			}
		}

		private byte ReadController1()
		{
			_isLag = false;
			byte retval;
			if (_inputPortSelection == InputPortMode.Left)
			{
				retval = ControllerDeck.ReadPort1(_controller, true, false);
				return retval;
			}

			if (_inputPortSelection == InputPortMode.Right)
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
			if (_inputPortSelection == InputPortMode.Left)
			{
				retval = ControllerDeck.ReadPort2(_controller, true, false);
				return retval;
			}

			if (_inputPortSelection == InputPortMode.Right)
			{
				retval = ControllerDeck.ReadPort2(_controller, false, false);
				return retval;
			}

			return 0x7F;
		}

		private byte ReadMemory(ushort addr)
		{
			if (addr >= 0x8000)
			{
				return _romData[addr & 0x7FFF];
			}

			if (addr >= 0x6000)
			{
				return _ram[addr & 1023];
			}

			if (addr < 0x2000)
			{
				return _biosRom[addr];
			}

			////Console.WriteLine("Unhandled read at {0:X4}", addr);
			return 0xFF;
		}

		private void WriteMemory(ushort addr, byte value)
		{
			if (addr >= 0x6000 && addr < 0x8000)
			{
				_ram[addr & 1023] = value;
			}

			////Console.WriteLine("Unhandled write at {0:X4}:{1:X2}", addr, value);
		}
	}
}