using System;
using System.IO;
using System.Collections.Generic;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.CP1610;

namespace BizHawk.Emulation.Cores.Intellivision
{
	[CoreAttributes(
		"IntelliHawk",
		"BrandonE",
		isPorted: false,
		isReleased: false
		)]
	[ServiceNotApplicable(typeof(ISaveRam), typeof(IDriveLight))]
	public sealed partial class Intellivision : IEmulator, IStatable, IInputPollable, ISettable<Intellivision.IntvSettings, Intellivision.IntvSyncSettings>
	{
		[CoreConstructor("INTV")]
		public Intellivision(CoreComm comm, GameInfo game, byte[] rom, object Settings, object SyncSettings)
		{
			ServiceProvider = new BasicServiceProvider(this);
			CoreComm = comm;

			_rom = rom;
			_gameInfo = game;

			this.Settings = (IntvSettings)Settings ?? new IntvSettings();
			this.SyncSettings = (IntvSyncSettings)SyncSettings ?? new IntvSyncSettings();

			ControllerDeck = new IntellivisionControllerDeck(this.SyncSettings.Port1, this.SyncSettings.Port2);

			_cart = new Intellicart();
			if (_cart.Parse(_rom) == -1)
			{
				_cart = new Cartridge();
				_cart.Parse(_rom);
			}

			_cpu = new CP1610();
			_cpu.ReadMemory = ReadMemory;
			_cpu.WriteMemory = WriteMemory;
			_cpu.Reset();

			_stic = new STIC();
			_stic.ReadMemory = ReadMemory;
			_stic.WriteMemory = WriteMemory;
			_stic.Reset();
			(ServiceProvider as BasicServiceProvider).Register<IVideoProvider>(_stic);

			_psg = new PSG();
			_psg.Reset();
			_psg.ReadMemory = ReadMemory;
			_psg.WriteMemory = WriteMemory;
			(ServiceProvider as BasicServiceProvider).Register<ISoundProvider>(_psg);

			Connect();

			//_cpu.LogData();

			LoadExecutiveRom(CoreComm.CoreFileProvider.GetFirmware("INTV", "EROM", true, "Executive ROM is required."));
			LoadGraphicsRom(CoreComm.CoreFileProvider.GetFirmware("INTV", "GROM", true, "Graphics ROM is required."));

			Tracer = new TraceBuffer { Header = _cpu.TraceHeader };
			(ServiceProvider as BasicServiceProvider).Register<ITraceable>(Tracer);

			SetupMemoryDomains();
		}

		public IntellivisionControllerDeck ControllerDeck { get; private set; }

		private ITraceable Tracer { get; set; }

		public int LagCount
		{
			get {return lagcount;}

			set{}
		}

		public bool IsLagFrame
		{
			get {return islag;}

			set {}
		}

		public IInputCallbackSystem InputCallbacks
		{
			get { return _inputCallbacks; }
		}

		private readonly InputCallbackSystem _inputCallbacks = new InputCallbackSystem();

		private byte[] _rom;
		private GameInfo _gameInfo;

		private CP1610 _cpu;
		private ICart _cart;
		private STIC _stic;
		private PSG _psg;

		public void Connect()
		{
			_cpu.SetIntRM(_stic.GetSr1());
			_cpu.SetBusRq(_stic.GetSr2());
			_stic.SetSst(_cpu.GetBusAk());
		}

		public void LoadExecutiveRom(byte[] erom)
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

		public void LoadGraphicsRom(byte[] grom)
		{
			if (grom.Length != 2048)
			{
				throw new ApplicationException("GROM file is wrong size - expected 2048 bytes");
			}

			GraphicsRom = grom;
		}

		public void get_controller_state()
		{
			InputCallbacks.Call();

			ushort port1 = ControllerDeck.ReadPort1(Controller);
			_psg.Register[15] = (ushort)(0xFF - port1);

			ushort port2 = ControllerDeck.ReadPort2(Controller);
			_psg.Register[14] = (ushort)(0xFF - port2);
		}
	}
}
