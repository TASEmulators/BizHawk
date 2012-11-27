using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;


namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class  C64 : IEmulator
	{
		private uint cyclesPerFrame;
		private string extension;
		private byte[] inputFile;

		public C64(GameInfo game, byte[] rom, string romextension)
		{
			inputFile = rom;
			extension = romextension;
			SetupMemoryDomains();
			CoreOutputComm = new CoreOutputComm();
			CoreInputComm = new CoreInputComm();
			Init(Region.PAL);
			cyclesPerFrame = (uint)chips.vic.CyclesPerFrame;
			CoreOutputComm.UsesDriveLed = true;
		}

		// internal variables
		private bool _islag = true;
		private int _lagcount = 0;
		private int _frame = 0;

		// bizhawk I/O
		public CoreInputComm CoreInputComm { get; set; }
		public CoreOutputComm CoreOutputComm { get; private set; }
		
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
		public ISoundProvider SoundProvider { get { return chips.sid; } }
		public bool StartAsyncSound() { return true; } //TODO
		public ISyncSoundProvider SyncSoundProvider { get { return chips.sid; } }
		public IVideoProvider VideoProvider { get { return chips.vic; } }

		// controller
		public ControllerDefinition ControllerDefinition { get { return C64ControllerDefinition; } }
		public IController Controller { get { return null; } set { } }
		public static readonly ControllerDefinition C64ControllerDefinition = new ControllerDefinition
		{
			Name = "Commodore 64 Controller", //TODO
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
				"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Button"
			}
		};

		// framework
		public void Dispose() { }

		// process frame
		public void FrameAdvance(bool render, bool rendersound)
		{
			chips.pla.InputWasRead = false;
			Execute(cyclesPerFrame);
			_islag = !chips.pla.InputWasRead;

			if (_islag)
				LagCount++;
			_frame++;

			CoreOutputComm.DriveLED = DriveLED;
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

		private void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>(1);
			domains.Add(new MemoryDomain("System Bus", 0x10000, Endian.Little, new Func<int, byte>(Peek), new Action<int, byte>(Poke)));
			memoryDomains = domains.AsReadOnly();
		}
	}
}
