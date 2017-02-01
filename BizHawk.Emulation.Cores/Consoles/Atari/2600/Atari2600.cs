using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	[CoreAttributes(
		"Atari2600Hawk",
		"Micro500, adelikat, natt",
		isPorted: false,
		isReleased: true
		)]
	[ServiceNotApplicable(typeof(ISaveRam), typeof(IDriveLight))]
	public partial class Atari2600 : IEmulator, IStatable, IDebuggable, IInputPollable,
		IRegionable, ICreateGameDBEntries, ISettable<Atari2600.A2600Settings, Atari2600.A2600SyncSettings>
	{
		private readonly GameInfo _game;
		private int _frame;

		private ITraceable Tracer { get; set; }

		[CoreConstructor("A26")]
		public Atari2600(CoreComm comm, GameInfo game, byte[] rom, object settings, object syncSettings)
		{
			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;

			MemoryCallbacks = new MemoryCallbackSystem();
			InputCallbacks = new InputCallbackSystem();

			Ram = new byte[128];
			CoreComm = comm;
			Settings = (A2600Settings)settings ?? new A2600Settings();
			SyncSettings = (A2600SyncSettings)syncSettings ?? new A2600SyncSettings();

			_leftDifficultySwitchPressed = SyncSettings.LeftDifficulty;
			_rightDifficultySwitchPressed = SyncSettings.RightDifficulty;

			Rom = rom;
			_game = game;

			if (!game.GetOptionsDict().ContainsKey("m"))
			{
				game.AddOption("m", DetectMapper(rom));
			}

			if (Rom.HashSHA1() == "3A77DB43B6583E8689435F0F14AA04B9E57BDDED" ||
				Rom.HashSHA1() == "E986E1818E747BEB9B33CE4DFF1CDC6B55BDB620")
			{
				game.RemoveOption("m");
				game.AddOption("m", "F8_sega");
			}

			Console.WriteLine("Game uses mapper " + game.GetOptionsDict()["m"]);
			Console.WriteLine(Rom.HashSHA1());
			RebootCore();
			SetupMemoryDomains();

			Tracer = new TraceBuffer { Header = Cpu.TraceHeader };

			ser.Register<IDisassemblable>(Cpu);
			ser.Register<ITraceable>(Tracer);
			ser.Register<IVideoProvider>(_tia);
			ser.Register<ISoundProvider>(_dcfilter);
		}

		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		public DisplayType Region
		{
			get { return _pal ? DisplayType.PAL : Common.DisplayType.NTSC; }
		}

		public string SystemId { get { return "A26"; } }

		public string BoardName { get { return _mapper.GetType().Name; } }

		public CoreComm CoreComm { get; private set; }

		public ControllerDefinition ControllerDefinition { get { return Atari2600ControllerDefinition; } }

		public IController Controller { get; set; }

		public int Frame { get { return _frame; } set { _frame = value; } }

		public bool DeterministicEmulation { get; set; }

		public static readonly ControllerDefinition Atari2600ControllerDefinition = new ControllerDefinition
		{
			Name = "Atari 2600 Basic Controller",
			BoolButtons =
			{
				"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Button", 
				"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Button", 
				"Reset", "Select", "Power", "Toggle Left Difficulty", "Toggle Right Difficulty"
			}
		};

		// ICreateGameDBEntries
		public CompactGameInfo GenerateGameDbEntry()
		{
			return new CompactGameInfo
			{
				Name = _game.Name,
				System = "A26",
				MetaData = "m=" + _mapper.GetType().ToString().Split('.').ToList().Last(),
				Hash = Rom.HashSHA1(),
				Region = _game.Region,
				Status = RomStatus.Unknown
			};
		}

		public void ResetCounters()
		{
			_frame = 0;
			_lagcount = 0;
			_islag = false;
		}

		public void Dispose() { }

		private static bool DetectPal(GameInfo game, byte[] rom)
		{
			// force NTSC mode for the new core we instantiate
			var newgame = game.Clone();
			if (newgame["PAL"])
				newgame.RemoveOption("PAL");
			if (!newgame["NTSC"])
				newgame.AddOption("NTSC");

			// give the emu a minimal of input\output connections so it doesn't crash
			var comm = new CoreComm(null, null);

			using (Atari2600 emu = new Atari2600(new CoreComm(null, null), newgame, rom, null, null))
			{
				emu.Controller = new NullController();

				List<int> framecounts = new List<int>();
				emu._tia.FrameEndCallBack = (i) => framecounts.Add(i);
				for (int i = 0; i < 71; i++) // run for 71 * 262 lines, since we're in NTSC mode
					emu.FrameAdvance(false, false);
				int numpal = framecounts.Count((i) => i > 287);
				bool pal = numpal >= 25;
				Console.WriteLine("PAL Detection: {0} lines, {1}", numpal, pal);
				return pal;
			}
		}
	}
}
