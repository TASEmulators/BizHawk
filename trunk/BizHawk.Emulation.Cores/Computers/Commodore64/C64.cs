using System;
using System.Collections.Generic;
using System.IO;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Computers.Commodore64.MOS;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	// TODO: use the EMulation.Common Region enum
	public enum Region
	{
		NTSC,
		PAL
	}

	[CoreAttributes(
		"C64Hawk",
		"SaxxonPIke",
		isPorted: false,
		isReleased: false
		)]
	[ServiceNotApplicable(typeof(ISettable<,>))]
	sealed public partial class C64 : IEmulator, IStatable, IInputPollable, IDriveLight, IDebuggable
	{
		// framework
		public C64(CoreComm comm, GameInfo game, byte[] rom, string romextension)
		{
			ServiceProvider = new BasicServiceProvider(this);
			InputCallbacks = new InputCallbackSystem();

			inputFileInfo = new InputFileInfo();
			inputFileInfo.Data = rom;
			inputFileInfo.Extension = romextension;
			CoreComm = comm;
			Init(Region.PAL);
			cyclesPerFrame = board.vic.CyclesPerFrame;
			SetupMemoryDomains();
			HardReset();
		}

		// internal variables
		private int _frame = 0;
		private int cyclesPerFrame;
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
		}

		// audio/video
		public void EndAsyncSound() { } //TODO
		public ISoundProvider SoundProvider { get { return null; } }
		public bool StartAsyncSound() { return false; } //TODO
		public ISyncSoundProvider SyncSoundProvider { get { return board.sid.resampler; } }
		public IVideoProvider VideoProvider { get { return board.vic; } }

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

		public void Dispose()
		{
			if (board.sid != null)
			{
				board.sid.Dispose();
				board.sid = null;
			}
		}

		// process frame
		public void FrameAdvance(bool render, bool rendersound)
		{
			board.inputRead = false;
			board.PollInput();
			board.cpu.LagCycles = 0;

			for (int count = 0; count < cyclesPerFrame; count++)
			{
				//disk.Execute();
				board.Execute();

#if false
				if (board.cpu.PC == 0xE16F && (board.cpu.ReadPort() & 0x7) == 7)
				{
					// HUGE kernal hack to load files
					// the only purpose for this is to be able to run the Lorenz
					// test suite!

					int fileNameLength = board.ram.Peek(0xB7);
					int fileNameOffset = board.ram.Peek(0xBB) | ((int)board.ram.Peek(0xBC) << 8);
					byte[] fileNameRaw = new byte[fileNameLength];
					for (int i = 0; i < fileNameLength; i++)
					{
						fileNameRaw[i] = board.ram.Peek(fileNameOffset + i);
					}
					var enc = System.Text.Encoding.ASCII;
					string fileName = enc.GetString(fileNameRaw);
					string filePath = Path.Combine(@"E:\Programming\Visual Studio 2013\Vice\testprogs\general\Lorenz-2.15\src\", fileName + ".prg");
					if (File.Exists(filePath))
					{
						PRG.Load(board.pla, File.ReadAllBytes(filePath));
					}
					board.cpu.PC = 0xE1B5;
				}
#endif

				// load PRG file if needed
				if (loadPrg)
				{
					// check to see if cpu PC is at the BASIC warm start vector
					if (board.cpu.PC == ((board.ram.Peek(0x0303) << 8) | board.ram.Peek(0x0302)))
					{
						//board.ram.Poke(0x0302, 0xAE);
						//board.ram.Poke(0x0303, 0xA7);
						////board.ram.Poke(0x0302, board.ram.Peek(0x0308));
						////board.ram.Poke(0x0303, board.ram.Peek(0x0309));

						//if (inputFileInfo.Data.Length >= 6)
						//{
						//    board.ram.Poke(0x0039, inputFileInfo.Data[4]);
						//    board.ram.Poke(0x003A, inputFileInfo.Data[5]);
						//}
						PRG.Load(board.pla, inputFileInfo.Data);
						loadPrg = false;
					}
				}
			}

			board.Flush();
			_islag = !board.inputRead;

			if (_islag)
				LagCount++;
			_frame++;

			//Console.WriteLine("CPUPC: " + C64Util.ToHex(board.cpu.PC, 4) + " 1541PC: " + C64Util.ToHex(disk.PC, 4));

			int test = board.cpu.LagCycles;
			DriveLightOn = DriveLED;
		}

		private void HandleFirmwareError(string file)
		{
			System.Windows.Forms.MessageBox.Show("the C64 core is referencing a firmware file which could not be found. Please make sure it's in your configured C64 firmwares folder. The referenced filename is: " + file);
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

		private void Init(Region initRegion)
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
					Cart cart = Cart.Load(inputFileInfo.Data);
					if (cart != null)
					{
						board.cartPort.Connect(cart);
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
