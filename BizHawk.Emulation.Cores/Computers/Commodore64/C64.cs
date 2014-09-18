using System;
using System.Collections.Generic;
using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	[CoreAttributes(
		"C64Hawk",
		"SaxxonPIke",
		isPorted: false,
		isReleased: false
		)]
	sealed public partial class C64 : IEmulator, IMemoryDomains
	{
		// internal variables
		private bool _islag = true;
		private int _lagcount = 0;
		private int _frame = 0;
		private int cyclesPerFrame;
		private InputFileInfo inputFileInfo;

		// bizhawk I/O
		public CoreComm CoreComm { get; private set; }

		// game/rom specific
		public GameInfo game;
		public string SystemId { get { return "C64"; } }

		public string BoardName { get { return null; } }

		// memory domains
		private MemoryDomainList memoryDomains;
		public MemoryDomainList MemoryDomains { get { return memoryDomains; } }

		// running state
		public bool DeterministicEmulation { get { return true; } set { ; } }
		public int Frame { get { return _frame; } set { _frame = value; } }
		public bool IsLagFrame { get { return _islag; } }
		public int LagCount { get { return _lagcount; } set { _lagcount = value; } }
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

		// framework
		public C64(CoreComm comm, GameInfo game, byte[] rom, string romextension)
		{
			inputFileInfo = new InputFileInfo();
			inputFileInfo.Data = rom;
			inputFileInfo.Extension = romextension;
			CoreComm = comm;
			Init(Region.PAL);
			cyclesPerFrame = board.vic.CyclesPerFrame;
			CoreComm.UsesDriveLed = true;
			SetupMemoryDomains();
			HardReset();
		}

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
			CoreComm.DriveLED = DriveLED;
		}

		private void HandleFirmwareError(string file)
		{
			System.Windows.Forms.MessageBox.Show("the C64 core is referencing a firmware file which could not be found. Please make sure it's in your configured C64 firmwares folder. The referenced filename is: " + file);
			throw new FileNotFoundException();
		}

		public byte[] SaveStateBinary()
		{
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		public bool BinarySaveStatesPreferred { get { return false; } }

		private void SetupMemoryDomains()
		{
			// chips must be initialized before this code runs!
			var domains = new List<MemoryDomain>(1);
			domains.Add(new MemoryDomain("System Bus", 0x10000, MemoryDomain.Endian.Little, board.cpu.Peek, board.cpu.Poke));
			domains.Add(new MemoryDomain("RAM", 0x10000, MemoryDomain.Endian.Little, board.ram.Peek, board.ram.Poke));
			domains.Add(new MemoryDomain("CIA0", 0x10, MemoryDomain.Endian.Little, board.cia0.Peek, board.cia0.Poke));
			domains.Add(new MemoryDomain("CIA1", 0x10, MemoryDomain.Endian.Little, board.cia1.Peek, board.cia1.Poke));
			domains.Add(new MemoryDomain("VIC", 0x40, MemoryDomain.Endian.Little, board.vic.Peek, board.vic.Poke));
			domains.Add(new MemoryDomain("SID", 0x20, MemoryDomain.Endian.Little, board.sid.Peek, board.sid.Poke));
			//domains.Add(new MemoryDomain("1541 Bus", 0x10000, MemoryDomain.Endian.Little, new Func<int, byte>(disk.Peek), new Action<int, byte>(disk.Poke)));
			//domains.Add(new MemoryDomain("1541 VIA0", 0x10, MemoryDomain.Endian.Little, new Func<int, byte>(disk.PeekVia0), new Action<int, byte>(disk.PokeVia0)));
			//domains.Add(new MemoryDomain("1541 VIA1", 0x10, MemoryDomain.Endian.Little, new Func<int, byte>(disk.PeekVia1), new Action<int, byte>(disk.PokeVia1)));
			//domains.Add(new MemoryDomain("1541 RAM", 0x1000, MemoryDomain.Endian.Little, new Func<int, byte>(disk.PeekRam), new Action<int, byte>(disk.PokeRam)));
			memoryDomains = new MemoryDomainList(domains);
		}

		public object GetSettings() { return null; }
		public object GetSyncSettings() { return null; }
		public bool PutSettings(object o) { return false; }
		public bool PutSyncSettings(object o) { return false; }
	}
}
