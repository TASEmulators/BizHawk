using System;
using System.Collections.Generic;
using System.IO;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Computers.Commodore64.MOS;
using System.Windows.Forms;
using BizHawk.Emulation.Cores.Computers.Commodore64.Cartridge;
using BizHawk.Emulation.Cores.Computers.Commodore64.Cassette;
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
	public sealed partial class C64 : IEmulator, IRegionable
	{
		// framework
		public C64(CoreComm comm, GameInfo game, byte[] rom, string romextension, object settings, object syncSettings)
		{
			PutSyncSettings((C64SyncSettings)syncSettings ?? new C64SyncSettings());
			PutSettings((C64Settings)settings ?? new C64Settings());

			ServiceProvider = new BasicServiceProvider(this);
			InputCallbacks = new InputCallbackSystem();

		    _inputFileInfo = new InputFileInfo
		    {
		        Data = rom,
		        Extension = romextension
		    };

		    CoreComm = comm;
			Init(SyncSettings.VicType);
			_cyclesPerFrame = _board.Vic.CyclesPerFrame;
			SetupMemoryDomains();
			MemoryCallbacks = new MemoryCallbackSystem();
			HardReset();

		    switch (SyncSettings.VicType)
		    {
		        case VicType.Ntsc:
                case VicType.Drean:
                case VicType.NtscOld:
                    Region = DisplayType.NTSC;
		            break;
                case VicType.Pal:
                    Region = DisplayType.PAL;
		            break;
		    }

			((BasicServiceProvider) ServiceProvider).Register<IVideoProvider>(_board.Vic);
		}

		// internal variables
		private int _frame;
		private readonly int _cyclesPerFrame;
		private InputFileInfo _inputFileInfo;

		// bizhawk I/O
		public CoreComm CoreComm { get; private set; }

		// game/rom specific
		public GameInfo Game;
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
			_frameCycles = 0;
		}

		// audio/video
		public void EndAsyncSound() { } //TODO
		public ISoundProvider SoundProvider { get { return null; } }
		public bool StartAsyncSound() { return false; } //TODO
		public ISyncSoundProvider SyncSoundProvider { get { return _board.Sid.Resampler; } }

		// controller
		public ControllerDefinition ControllerDefinition { get { return C64ControllerDefinition; } }
		public IController Controller { get { return _board.Controller; } set { _board.Controller = value; } }

	    private static readonly ControllerDefinition C64ControllerDefinition = new ControllerDefinition
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
			if (_board.Sid != null)
			{
				_board.Sid.Dispose();
			}
		}

	    private int _frameCycles;

		// process frame
		public void FrameAdvance(bool render, bool rendersound)
		{
			do
			{
				DoCycle();
			}
			while (_frameCycles != 0);
		}

		private void DoCycle()
		{
			if (_frameCycles == 0) {
				_board.InputRead = false;
				_board.PollInput();
				_board.Cpu.LagCycles = 0;
			}

			//disk.Execute();
			_board.Execute();
			_frameCycles++;

			// load PRG file if needed
			if (_loadPrg)
			{
				// check to see if cpu PC is at the BASIC warm start vector
				if (_board.Cpu.Pc == ((_board.Ram.Peek(0x0303) << 8) | _board.Ram.Peek(0x0302)))
				{
					Prg.Load(_board.Pla, _inputFileInfo.Data);
					_loadPrg = false;
				}
			}

		    if (_frameCycles != _cyclesPerFrame)
		    {
		        return;
		    }

		    _board.Flush();
		    _islag = !_board.InputRead;

		    if (_islag)
		        _lagcount++;
		    _frameCycles -= _cyclesPerFrame;
		    _frame++;

		    DriveLightOn = DriveLED;
		}

		private void HandleFirmwareError(string file)
		{
			MessageBox.Show("the C64 core is referencing a firmware file which could not be found. Please make sure it's in your configured C64 firmwares folder. The referenced filename is: " + file);
			throw new FileNotFoundException();
		}

		private Motherboard _board;
		private bool _loadPrg;

		private byte[] GetFirmware(string name, int length)
		{
			var result = CoreComm.CoreFileProvider.GetFirmware("C64", name, true);
			if (result.Length != length)
				throw new MissingFirmwareException(string.Format("Firmware {0} was {1} bytes, should be {2} bytes", name, result.Length, length));
			return result;
		}

		private void Init(VicType initRegion)
		{
			_board = new Motherboard(this, initRegion);
			InitRoms();
			_board.Init();
			InitMedia();

			// configure video
			CoreComm.VsyncDen = _board.Vic.CyclesPerFrame;
			CoreComm.VsyncNum = _board.Vic.CyclesPerSecond;
		}

		private void InitMedia()
		{
			switch (_inputFileInfo.Extension.ToUpper())
			{
				case @".CRT":
					var cart = CartridgeDevice.Load(_inputFileInfo.Data);
					if (cart != null)
					{
						_board.CartPort.Connect(cart);
					}
					break;
				case @".TAP":
					var tape = Tape.Load(_inputFileInfo.Data);
					if (tape != null)
					{
					    var tapeDrive = new TapeDrive();
					    tapeDrive.Insert(tape);
                        _board.Cassette.Connect(tapeDrive);
					}
					break;
				case @".PRG":
					if (_inputFileInfo.Data.Length > 2)
						_loadPrg = true;
					break;
			}
		}

		private void InitRoms()
		{
			var basicRom = GetFirmware("Basic", 0x2000);
			var charRom = GetFirmware("Chargen", 0x1000);
			var kernalRom = GetFirmware("Kernal", 0x2000);

			_board.BasicRom = new Chip23XX(Chip23XXmodel.Chip2364, basicRom);
			_board.KernalRom = new Chip23XX(Chip23XXmodel.Chip2364, kernalRom);
			_board.CharRom = new Chip23XX(Chip23XXmodel.Chip2332, charRom);
		}

		// ------------------------------------

		public void HardReset()
		{
			_board.HardReset();
			//disk.HardReset();
		}
	}
}
