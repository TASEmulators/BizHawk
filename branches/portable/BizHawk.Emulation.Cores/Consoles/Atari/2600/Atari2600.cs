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
	public partial class Atari2600 : IEmulator, IMemoryDomains, IStatable, IDebuggable, IInputPollable, ISettable<Atari2600.A2600Settings, Atari2600.A2600SyncSettings>
	{
		private readonly GameInfo _game;
		private int _frame;

		[CoreConstructor("A26")]
		public Atari2600(CoreComm comm, GameInfo game, byte[] rom, object settings, object syncSettings)
		{
			Tracer = new TraceBuffer();
			MemoryCallbacks = new MemoryCallbackSystem();
			InputCallbacks = new InputCallbackSystem();

			Ram = new byte[128];
			CoreComm = comm;
			Settings = (A2600Settings)settings ?? new A2600Settings();
			SyncSettings = (A2600SyncSettings)syncSettings ?? new A2600SyncSettings();

			Rom = rom;
			_game = game;

			if (!game.GetOptionsDict().ContainsKey("m"))
			{
				game.AddOption("m", DetectMapper(rom));
			}

			Console.WriteLine("Game uses mapper " + game.GetOptionsDict()["m"]);
			RebootCore();
			SetupMemoryDomains();

			var ser = new  BasicServiceProvider(this);
			ser.Register<IDisassemblable>(Cpu);
			ServiceProvider = ser;
		}

		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		public string SystemId { get { return "A26"; } }

		public string BoardName { get { return _mapper.GetType().Name; } }

		public CoreComm CoreComm { get; private set; }

		public IVideoProvider VideoProvider { get { return _tia; } }

		public ISoundProvider SoundProvider { get { return _dcfilter; } }

		// todo: make this not so ugly
		public ISyncSoundProvider SyncSoundProvider
		{
			get
			{
				return new FakeSyncSound(_dcfilter, CoreComm.VsyncRate > 55.0 ? 735 : 882);
			}
		}

		public ControllerDefinition ControllerDefinition { get { return Atari2600ControllerDefinition; } }

		public IController Controller { get; set; }

		public int Frame { get { return _frame; } set { _frame = value; } }

		public bool DeterministicEmulation { get; set; }

		public A2600Settings Settings { get; private set; }

		public A2600SyncSettings SyncSettings { get; private set; }

		public static readonly ControllerDefinition Atari2600ControllerDefinition = new ControllerDefinition
		{
			Name = "Atari 2600 Basic Controller",
			BoolButtons =
			{
				"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Button", 
				"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Button", 
				"Reset", "Select", "Power"
			}
		};

		public int CurrentScanLine
		{
			get { return _tia.LineCount; }
		}

		public bool IsVsync
		{
			get { return _tia.IsVSync; }
		}

		public bool IsVBlank
		{
			get { return _tia.IsVBlank; }
		}

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

		public bool StartAsyncSound() { return true; }

		public void EndAsyncSound() { }

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
