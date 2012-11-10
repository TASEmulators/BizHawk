using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;


namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class  C64 : IEmulator
	{
		public C64(GameInfo game, byte[] rom, string romextension)
		{
			inputFile = rom;
			extension = romextension;
			SetupMemoryDomains();
			CoreOutputComm = new CoreOutputComm();
			CoreInputComm = new CoreInputComm();
		}

		public string SystemId { get { return "C64"; } }
		public GameInfo game;
		
		public CoreInputComm CoreInputComm { get; set; }
		public CoreOutputComm CoreOutputComm { get; private set; }
		private IList<MemoryDomain> memoryDomains;
		public IList<MemoryDomain> MemoryDomains { get { return memoryDomains; } }
		public MemoryDomain MainMemory { get { return memoryDomains[0]; } }
		public int Frame { get { return _frame; } set { _frame = value; } }
		public int LagCount { get { return _lagcount; } set { _lagcount = value; } }
		public bool IsLagFrame { get { return _islag; } }
		private bool _islag = true;
		private int _lagcount = 0;
		private int _frame = 0;
		public byte[] ReadSaveRam() { return null; }
		public void StoreSaveRam(byte[] data) { }
		public void ClearSaveRam() { }
		public bool SaveRamModified { get; set; }
		public void Dispose() { }
		public IVideoProvider VideoProvider { get { return vic; } }
		public ISoundProvider SoundProvider { get { return sid; } }
		public void ResetFrameCounter()
		{
			_frame = 0;
		}

		/*TODO*/
		public ISyncSoundProvider SyncSoundProvider { get { return null; } } //TODO
		public bool StartAsyncSound() { return true; } //TODO
		public void EndAsyncSound() { } //TODO
		public bool DeterministicEmulation { get; set; } //TODO
		public void SaveStateText(TextWriter writer) { } //TODO
		public void LoadStateText(TextReader reader) { } //TODO
		public void SaveStateBinary(BinaryWriter bw) { } //TODO
		public void LoadStateBinary(BinaryReader br) { } //TODO
		public ControllerDefinition ControllerDefinition { get { return C64ControllerDefinition; } }
		public IController Controller { get { return input.controller; } set { input.controller = value; } }
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

		public void FrameAdvance(bool render, bool rendersound)
		{
			_frame++;
			_islag = true;

			const int cyclesPerFrame = (14318181 / 14 / 60);

			foreach (IMedia media in mediaAttached)
			{
				if (!media.Loaded() && media.Ready())
				{
					media.Apply();
				}
			}

			PollInput();

			for (int i = 0; i < cyclesPerFrame; i++)
			{
				vic.PerformCycle();
				cpu.IRQ = signal.CpuIRQ;
				cpu.NMI = signal.CpuNMI;
				if (signal.CpuAEC)
				{
					cpu.ExecuteOne();
				}
				sid.PerformCycle();
				cia0.PerformCycle();
				cia1.PerformCycle();
			}

			if (_islag)
			{
				LagCount++;
			}
		}

		private void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>(1);
			domains.Add(new MemoryDomain("RAM", 0x10000, Endian.Little, new Func<int, byte>(PeekMemoryInt), new Action<int,byte>(PokeMemoryInt))); //TODO
			memoryDomains = domains.AsReadOnly();
		}

		public byte[] SaveStateBinary()
		{
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		void SyncState(Serializer ser) //TODO
		{
			ser.Sync("Lag", ref _lagcount);
			ser.Sync("Frame", ref _frame);
			ser.Sync("IsLag", ref _islag);
		}
	}
}
