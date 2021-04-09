using System;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.CP1610;

namespace BizHawk.Emulation.Cores.Intellivision
{
	[Core(
		CoreNames.IntelliHawk,
		"BrandonE, Alyosha",
		isPorted: false,
		isReleased: true)]
	[ServiceNotApplicable(new[] { typeof(IDriveLight), typeof(IRegionable), typeof(ISaveRam) })]
	public sealed partial class Intellivision : IEmulator, IInputPollable, IDisassemblable,
		IBoardInfo, IDebuggable, ISettable<Intellivision.IntvSettings, Intellivision.IntvSyncSettings>
	{
		[CoreConstructor("INTV")]
		public Intellivision(CoreComm comm, byte[] rom, Intellivision.IntvSettings settings, Intellivision.IntvSyncSettings syncSettings)
		{
			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;
			_rom = rom;
			_settings = (IntvSettings)settings ?? new IntvSettings();
			_syncSettings = (IntvSyncSettings)syncSettings ?? new IntvSyncSettings();

			_controllerDeck = new IntellivisionControllerDeck(_syncSettings.Port1, _syncSettings.Port2);

			_cart = new Intellicart();
			if (_cart.Parse(_rom) == -1)
			{
				_cart = new Cartridge();
				_cart.Parse(_rom);
			}

			_cpu = new CP1610
			{
				ReadMemory = ReadMemory,
				WriteMemory = WriteMemory,
				MemoryCallbacks = MemoryCallbacks
			};
			_cpu.Reset();

			_stic = new STIC
			{
				ReadMemory = ReadMemory,
				WriteMemory = WriteMemory
			};
			_stic.Reset();

			_psg = new PSG
			{
				ReadMemory = ReadMemory,
				WriteMemory = WriteMemory
			};
			_psg.Reset();

			ser.Register<IVideoProvider>(_stic);
			ser.Register<ISoundProvider>(_psg);

			Connect();

			LoadExecutiveRom(comm.CoreFileProvider.GetFirmware("INTV", "EROM", true, "Executive ROM is required."));
			LoadGraphicsRom(comm.CoreFileProvider.GetFirmware("INTV", "GROM", true, "Graphics ROM is required."));

			_tracer = new TraceBuffer { Header = _cpu.TraceHeader };
			ser.Register<ITraceable>(_tracer);
			ser.Register<IStatable>(new StateSerializer(SyncState));
			SetupMemoryDomains();
		}

		private readonly IntellivisionControllerDeck _controllerDeck;

		private readonly byte[] _rom;
		private readonly ITraceable _tracer;
		private readonly CP1610 _cpu;
		private readonly STIC _stic;
		private readonly PSG _psg;

		private ICart _cart;
		private int _frame;
		private int _sticRow;

		private void Connect()
		{
			_cpu.SetIntRM(_stic.GetSr1());
			_cpu.SetBusRq(_stic.GetSr2());
			_stic.SetSst(_cpu.GetBusAk());
		}

		private void LoadExecutiveRom(byte[] erom)
		{
			if (erom.Length != 8192)
			{
				throw new ApplicationException("EROM file is wrong size - expected 8192 bytes");
			}

			int index = 0;

			// Combine every two bytes into a word.
			while (index + 1 < erom.Length)
			{
				ExecutiveRom[index / 2] = (ushort)((erom[index++] << 8) | erom[index++]);
			}
		}

		private void LoadGraphicsRom(byte[] grom)
		{
			if (grom.Length != 2048)
			{
				throw new ApplicationException("GROM file is wrong size - expected 2048 bytes");
			}

			GraphicsRom = grom;
		}

		private void GetControllerState(IController controller)
		{
			InputCallbacks.Call();

			ushort port1 = _controllerDeck.ReadPort1(controller);
			_psg.Register[15] = (ushort)(0xFF - port1);

			ushort port2 = _controllerDeck.ReadPort2(controller);
			_psg.Register[14] = (ushort)(0xFF - port2);
		}

		private void HardReset()
		{
			_cpu.Reset();
			_stic.Reset();
			_psg.Reset();

			Connect();

			ScratchpadRam = new byte[240];
			SystemRam = new ushort[352];

			_cart = new Intellicart();
			if (_cart.Parse(_rom) == -1)
			{
				_cart = new Cartridge();
				_cart.Parse(_rom);
			}
		}

		private void SoftReset()
		{
			_cpu.Reset();
			_stic.Reset();
			_psg.Reset();

			Connect();
		}
	}
}
