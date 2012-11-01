using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;


namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class  C64 : IEmulator
	{
		public C64(GameInfo game, byte[] rom)
		{
			videoProvider = new MyVideoProvider(this);
			SetupMemoryDomains();
			CoreOutputComm = new CoreOutputComm();
			CoreInputComm = new CoreInputComm();
			HardReset();
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
		public IVideoProvider VideoProvider { get { return videoProvider; } }
		public ISoundProvider SoundProvider { get { return soundProvider; } }
		public void ResetFrameCounter()
		{
			_frame = 0;
		}

		/*TODO*/
		public int[] frameBuffer = new int[256 * 192]; //TODO
		public ISyncSoundProvider SyncSoundProvider { get { return null; } } //TODO
		public bool StartAsyncSound() { return true; } //TODO
		public void EndAsyncSound() { } //TODO
		public bool DeterministicEmulation { get; set; } //TODO
		public void SaveStateText(TextWriter writer) { } //TODO
		public void LoadStateText(TextReader reader) { } //TODO
		public void SaveStateBinary(BinaryWriter bw) { } //TODO
		public void LoadStateBinary(BinaryReader br) { } //TODO
		public ControllerDefinition ControllerDefinition { get { return Atari7800ControllerDefinition; } }
		public IController Controller { get; set; }
		public static readonly ControllerDefinition Atari7800ControllerDefinition = new ControllerDefinition
		{
			Name = "Atari 7800 Basic Controller", //TODO
			BoolButtons =
			{
				"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Button", 
				"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Button", 
				"Reset", "Select"
			}
		};

		class MySoundProvider : ISoundProvider
		{
			Atari7800 emu;
			public MySoundProvider(Atari7800 emu)
			{
				this.emu = emu;
			}
			public int MaxVolume { get { return 0; } set { } }
			public void DiscardSamples()
			{
			}

			public void GetSamples(short[] samples)
			{
			}
		}

		public void FrameAdvance(bool render, bool rendersound)
		{
			_frame++;
			_islag = true;

			//TODO
			//Do stuff here

			if (_islag)
			{
				LagCount++;
			}

			videoProvider.FillFrameBuffer();
		}

		/*******************************/

		private MySoundProvider soundProvider;
		private MyVideoProvider videoProvider;
		class MyVideoProvider : IVideoProvider
		{
			public int top = 0; //TODO
			public int bottom = 262; //TODO
			public int left = 0; //TODO
			public int right = 320; //TODO

			C64 emu;
			public MyVideoProvider(C64 emu)
			{
				this.emu = emu;
			}

			int[] buffer = new int[262 * 320]; 

			public void FillFrameBuffer() 
			{
				for (int i = 0; i < buffer.Length; i++)
				{
					buffer[i] = 0; //TODO
				}
			}

			public int[] GetVideoBuffer()
			{
				return buffer;
			}

			public int VirtualWidth { get { return BufferWidth; } }
			public int BufferWidth { get { return right - left + 1; } }
			public int BufferHeight { get { return bottom - top + 1; } }
			public int BackgroundColor { get { return 0; } }
		}

		private void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>(1);
			domains.Add(new MemoryDomain("Main RAM", 1, Endian.Little, addr => 0xFF, null)); //TODO
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
