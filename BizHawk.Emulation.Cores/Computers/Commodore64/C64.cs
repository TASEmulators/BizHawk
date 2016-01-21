using System;
using System.Collections.Generic;
using System.IO;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Computers.Commodore64.MOS;
using System.Windows.Forms;
using BizHawk.Emulation.Cores.Computers.Commodore64.Media;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	[CoreAttributes(
		"C64Hawk",
		"SaxxonPike",
		isPorted: false,
		isReleased: false
		)]
	[ServiceNotApplicable(typeof(ISettable<,>))]
	sealed public partial class C64 : IEmulator, IRegionable
	{
		// framework
		public C64(CoreComm comm, GameInfo game, byte[] rom, string romextension, object Settings, object SyncSettings)
		{
			PutSyncSettings((C64SyncSettings)SyncSettings ?? new C64SyncSettings());
			PutSettings((C64Settings)Settings ?? new C64Settings());

			ServiceProvider = new BasicServiceProvider(this);
			InputCallbacks = new InputCallbackSystem();

			inputFileInfo = new InputFileInfo();
			inputFileInfo.Data = rom;
			inputFileInfo.Extension = romextension;
			CoreComm = comm;
			Init(this.SyncSettings.vicType);
			cyclesPerFrame = board.vic.CyclesPerFrame;
			SetupMemoryDomains();
			MemoryCallbacks = new MemoryCallbackSystem();
			HardReset();

			(ServiceProvider as BasicServiceProvider).Register<IVideoProvider>(board.vic);
		}

		// internal variables
		private int _frame = 0;
		private readonly int cyclesPerFrame;
		private InputFileInfo inputFileInfo;

		// bizhawk I/O
		public CoreComm CoreComm { get; private set; }

		// game/rom specific
		public GameInfo game;
		public string SystemId { get { return "C64"; } }

		public string BoardName { get { return null; } }

		// running state
		public bool DeterministicEmulation { get { return true; } set { ; } }
		public int Frame { get { return _frame; } set { _frame = value; } }
		public void ResetCounters()
		{
			_frame = 0;
			_lagcount = 0;
			_islag = false;
			frameCycles = 0;
		}

		// audio/video
		public void EndAsyncSound() { } //TODO
		public ISoundProvider SoundProvider { get { return null; } }
		public bool StartAsyncSound() { return false; } //TODO
		public ISyncSoundProvider SyncSoundProvider { get { return board.sid.resampler; } }

		// controller
		public ControllerDefinition ControllerDefinition { get { return C64ControllerDefinition; } }
		public IController Controller { get { return board.controller; } set { board.controller = value; } }
		public static readonly ControllerDefinition C64ControllerDefinition = new ControllerDefinition
		{
			Name = "Commodore 64 Controller",
			BoolButtons =
			{
				"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Button",
				"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Button",
				"Key Left Arrow", "Key 1", "Key 2", "Key 3", "Key 4", "Key 5", "Key 6", "Key 7", "Key 8", "Key 9", "Key 0", "Key Plus", "Key Minus", "Key Pound", "Key Clear/Home", "Key Insert/Delete",
				"Key Control", "Key Q", "Key W", "Key E", "Key R", "Key T", "Key Y", "Key U", "Key I", "Key O", "Key P", "Key At", "Key Asterisk", "Key Up Arrow", "Key Restore",
				"Key Run/Stop", "Key Lck", "Key A", "Key S", "Key D", "Key F", "Key G", "Key H", "Key J", "Key K", "Key L", "Key Colon", "Key Semicolon", "Key Equal", "Key Return", 
				"Key Commodore", "Key Left Shift", "Key Z", "Key X", "Key C", "Key V", "Key B", "Key N", "Key M", "Key Comma", "Key Period", "Key Slash", "Key Right Shift", "Key Cursor Up/Down", "Key Cursor Left/Right", 
				"Key Space", 
				"Key F1", "Key F3", "Key F5", "Key F7"
			}
		};

		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		public DisplayType Region
		{
			get;
			private set;
		}

		public void Dispose()
		{
			if (board.sid != null)
			{
				board.sid.Dispose();
			}
		}

	    private int frameCycles;

		// process frame
		public void FrameAdvance(bool render, bool rendersound)
		{
			do
			{
				DoCycle();
			}
			while (frameCycles != 0);
		}

		private void DoCycle()
		{
			if (frameCycles == 0) {
				board.inputRead = false;
				board.PollInput();
				board.cpu.LagCycles = 0;
			}

			//disk.Execute();
			board.Execute();
			frameCycles++;

			// load PRG file if needed
			if (loadPrg)
			{
				// check to see if cpu PC is at the BASIC warm start vector
				if (board.cpu.PC == ((board.ram.Peek(0x0303) << 8) | board.ram.Peek(0x0302)))
				{
					PRG.Load(board.pla, inputFileInfo.Data);
					loadPrg = false;
				}
			}

		    if (frameCycles != cyclesPerFrame)
		    {
		        return;
		    }

		    board.Flush();
		    _islag = !board.inputRead;

		    if (_islag)
		        _lagcount++;
		    frameCycles -= cyclesPerFrame;
		    _frame++;

		    DriveLightOn = DriveLED;
		}

		private void HandleFirmwareError(string file)
		{
			MessageBox.Show("the C64 core is referencing a firmware file which could not be found. Please make sure it's in your configured C64 firmwares folder. The referenced filename is: " + file);
			throw new FileNotFoundException();
		}

		private Motherboard board;
		private bool loadPrg;

		private byte[] GetFirmware(string name, int length)
		{
			byte[] result = CoreComm.CoreFileProvider.GetFirmware("C64", name, true);
			if (result.Length != length)
				throw new MissingFirmwareException(string.Format("Firmware {0} was {1} bytes, should be {2} bytes", name, result.Length, length));
			return result;
		}

		private void Init(VicType initRegion)
		{
			board = new Motherboard(this, initRegion);
			InitRoms();
			board.Init();
			InitMedia();

			// configure video
			CoreComm.VsyncDen = board.vic.CyclesPerFrame;
			CoreComm.VsyncNum = board.vic.CyclesPerSecond;
		}

		private void InitMedia()
		{
			switch (inputFileInfo.Extension.ToUpper())
			{
				case @".CRT":
					var cart = Cart.Load(inputFileInfo.Data);
					if (cart != null)
					{
						board.cartPort.Connect(cart);
					}
					break;
				case @".TAP":
					var tape = CassettePort.Tape.Load(inputFileInfo.Data);
					if (tape != null)
					{
						board.cassPort.Connect(tape);
					}
					break;
				case @".PRG":
					if (inputFileInfo.Data.Length > 2)
						loadPrg = true;
					break;
			}
		}

		private void InitRoms()
		{
			byte[] basicRom = GetFirmware("Basic", 0x2000);
			byte[] charRom = GetFirmware("Chargen", 0x1000);
			byte[] kernalRom = GetFirmware("Kernal", 0x2000);

			board.basicRom = new Chip23XX(Chip23XXmodel.Chip2364, basicRom);
			board.kernalRom = new Chip23XX(Chip23XXmodel.Chip2364, kernalRom);
			board.charRom = new Chip23XX(Chip23XXmodel.Chip2332, charRom);
		}

		// ------------------------------------

		public void HardReset()
		{
			board.HardReset();
			//disk.HardReset();
		}
	}
}
