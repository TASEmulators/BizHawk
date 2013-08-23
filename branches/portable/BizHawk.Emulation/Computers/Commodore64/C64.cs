using System;
using System.Collections.Generic;
using System.IO;


namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class  C64 : IEmulator
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

		// memory domains
		public MemoryDomain MainMemory { get { return memoryDomains[0]; } }
		private IList<MemoryDomain> memoryDomains;
		public IList<MemoryDomain> MemoryDomains { get { return memoryDomains; } }

		// running state
		public bool DeterministicEmulation { get { return true; } set { ; } }
		public int Frame { get { return _frame; } set { _frame = value; } }
		public bool IsLagFrame { get { return _islag; } }
		public int LagCount { get { return _lagcount; } set { _lagcount = value; } }
		public void ResetFrameCounter()
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
				"Key Insert/Delete", "Key Return", "Key Cursor Left/Right", "Key F7", "Key F1", "Key F3", "Key F5", "Key Cursor Up/Down",
				"Key 3", "Key W", "Key A", "Key 4", "Key Z", "Key S", "Key E", "Key Left Shift",
				"Key 5", "Key R", "Key D", "Key 6", "Key C", "Key F", "Key T", "Key X",
				"Key 7", "Key Y", "Key G", "Key 8", "Key B", "Key H", "Key U", "Key V",
				"Key 9", "Key I", "Key J", "Key 0", "Key M", "Key K", "Key O", "Key N",
				"Key Plus", "Key P", "Key L", "Key Minus", "Key Period", "Key Colon", "Key At", "Key Comma",
				"Key Pound", "Key Asterisk", "Key Semicolon", "Key Clear/Home", "Key Right Shift", "Key Equal", "Key Up Arrow", "Key Slash",
				"Key 1", "Key Left Arrow", "Key Control", "Key 2", "Key Space", "Key Commodore", "Key Q", "Key Run/Stop",
				"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Button",
				"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Button",
				"Key Restore", "Key Lck"
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
                        Media.PRG.Load(board.pla, inputFileInfo.Data);
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
			domains.Add(new MemoryDomain("System Bus", 0x10000, Endian.Little, board.cpu.Peek, board.cpu.Poke));
			domains.Add(new MemoryDomain("RAM", 0x10000, Endian.Little, board.ram.Peek, board.ram.Poke));
			domains.Add(new MemoryDomain("CIA0", 0x10, Endian.Little, board.cia0.Peek, board.cia0.Poke));
			domains.Add(new MemoryDomain("CIA1", 0x10, Endian.Little, board.cia1.Peek, board.cia1.Poke));
			domains.Add(new MemoryDomain("VIC", 0x40, Endian.Little, board.vic.Peek, board.vic.Poke));
			domains.Add(new MemoryDomain("SID", 0x20, Endian.Little, board.sid.Peek, board.sid.Poke));
			//domains.Add(new MemoryDomain("1541 Bus", 0x10000, Endian.Little, new Func<int, byte>(disk.Peek), new Action<int, byte>(disk.Poke)));
			//domains.Add(new MemoryDomain("1541 VIA0", 0x10, Endian.Little, new Func<int, byte>(disk.PeekVia0), new Action<int, byte>(disk.PokeVia0)));
			//domains.Add(new MemoryDomain("1541 VIA1", 0x10, Endian.Little, new Func<int, byte>(disk.PeekVia1), new Action<int, byte>(disk.PokeVia1)));
			//domains.Add(new MemoryDomain("1541 RAM", 0x1000, Endian.Little, new Func<int, byte>(disk.PeekRam), new Action<int, byte>(disk.PokeRam)));
			memoryDomains = domains.AsReadOnly();
		}
	}
}
