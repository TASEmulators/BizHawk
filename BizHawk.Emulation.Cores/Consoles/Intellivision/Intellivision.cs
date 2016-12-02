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
	[ServiceNotApplicable(typeof(ISaveRam))]
	public sealed partial class Intellivision : IEmulator
	{
		[CoreConstructor("INTV")]
		public Intellivision(CoreComm comm, GameInfo game, byte[] rom)
		{
			ServiceProvider = new BasicServiceProvider(this);
			CoreComm = comm;

			_rom = rom;
			_gameInfo = game;
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
			_psg.ReadMemory = ReadMemory;
			_psg.WriteMemory = WriteMemory;

			Connect();

			//_cpu.LogData();

			LoadExecutiveRom(CoreComm.CoreFileProvider.GetFirmware("INTV", "EROM", true, "Executive ROM is required."));
			LoadGraphicsRom(CoreComm.CoreFileProvider.GetFirmware("INTV", "GROM", true, "Graphics ROM is required."));

			Tracer = new TraceBuffer { Header = _cpu.TraceHeader };
			(ServiceProvider as BasicServiceProvider).Register<ITraceable>(Tracer);

			SetupMemoryDomains();
		}

		private ITraceable Tracer { get; set; }

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

		public static readonly ControllerDefinition IntellivisionController =
			new ControllerDefinition
			{
				Name = "Intellivision Controller",
				BoolButtons = {					
					"P1 L", "P1 R", "P1 Top",
					"P1 Key 0", "P1 Key 1", "P1 Key 2", "P1 Key 3", "P1 Key 4", "P1 Key 5",
					"P1 Key 6", "P1 Key 7", "P1 Key 8", "P1 Key 9", "P1 Enter", "P1 Clear",
					"P1 N", "P1 NNE", "P1 NE", "P1 ENE","P1 E", "P1 ESE", "P1 SE", "P1 SSE",
					"P1 S", "P1 SSW", "P1 SW", "P1 WSW","P1 W", "P1 WNW", "P1 NW", "P1 NNW",

					"P2 L", "P2 R", "P2 Top",
					"P2 Key 0", "P2 Key 1", "P2 Key 2", "P2 Key 3", "P2 Key 4", "P2 Key 5",
					"P2 Key 6", "P2 Key 7", "P2 Key 8", "P2 Key 9", "P2 Enter", "P2 Clear",
					"P2 N", "P2 NNE", "P2 NE", "P2 ENE","P2 E", "P2 ESE", "P2 SE", "P2 SSE",
					"P2 S", "P2 SSW", "P2 SW", "P2 WSW","P2 W", "P2 WNW", "P2 NW", "P2 NNW",
				}
			};
	}
}
