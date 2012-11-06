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
			HardReset();
			videoProvider = new MyVideoProvider(vic);
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
		public ISyncSoundProvider SyncSoundProvider { get { return null; } } //TODO
		public bool StartAsyncSound() { return true; } //TODO
		public void EndAsyncSound() { } //TODO
		public bool DeterministicEmulation { get; set; } //TODO
		public void SaveStateText(TextWriter writer) { } //TODO
		public void LoadStateText(TextReader reader) { } //TODO
		public void SaveStateBinary(BinaryWriter bw) { } //TODO
		public void LoadStateBinary(BinaryReader br) { } //TODO
		public ControllerDefinition ControllerDefinition { get { return C64ControllerDefinition; } }
		public IController Controller { get; set; }
		public static readonly ControllerDefinition C64ControllerDefinition = new ControllerDefinition
		{
			Name = "Commodore 64 Controller", //TODO
			BoolButtons =
			{
				"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Button",
				"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Button" 
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

			int cyclesPerSecond = (14318181 / 14 / 60);

			for (int i = 0; i < cyclesPerSecond; i++)
			{
				if (signal.CpuIRQ)
				{
					cpu.IRQ = true;
				}
				if (signal.CpuAEC)
				{
					cpu.ExecuteOne();
				}
				vic.PerformCycle();
				sid.PerformCycle();
				cia0.PerformCycle();
				cia1.PerformCycle();
			}

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
			public int top;
			public int bottom;
			public int left;
			public int right;

			VicII vic;
			public MyVideoProvider(VicII vic)
			{
				this.vic = vic;

				buffer = new int[vic.visibleWidth * vic.visibleHeight];
				top = 0;
				bottom = vic.visibleHeight - 1;
				left = 0;
				right = vic.visibleWidth - 1;
			}

			int[] buffer; 

			public void FillFrameBuffer() 
			{
				Array.Copy(vic.buffer, buffer, buffer.Length);
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
