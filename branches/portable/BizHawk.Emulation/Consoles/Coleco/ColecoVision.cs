using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BizHawk.Emulation.Consoles.Coleco
{
	public partial class ColecoVision : IEmulator, IVideoProvider, ISoundProvider
	{
		public string SystemId { get { return "Coleco"; } }
		public int[] frameBuffer = new int[256 * 192];
		public CoreInputComm CoreInputComm { get; set; }
		public CoreOutputComm CoreOutputComm { get; private set; }
		public IVideoProvider VideoProvider { get { return this; } }
		public ISoundProvider SoundProvider { get { return this; } }
		public byte[] ram = new byte[1024];

		public ColecoVision(GameInfo game, byte[] rom)
		{
			var domains = new List<MemoryDomain>(1);
			domains.Add(new MemoryDomain("Main RAM", 128, Endian.Little, addr => ram[1023], (addr, value) => ram[addr & 1023] = value));
			memoryDomains = domains.AsReadOnly();
			CoreOutputComm = new CoreOutputComm();
			CoreInputComm = new CoreInputComm();
			this.rom = rom;
			HardReset();
		}

		public void ResetFrameCounter() { _frame = 0; }

		public static readonly ControllerDefinition ColecoVisionControllerDefinition = new ControllerDefinition
		{
			Name = "ColecoVision Basic Controller",
			BoolButtons = 
			{
				"P1 Up", "P1 Down", "P1 Left", "P1 Right",
				"P1 B1", "P1 B2", "P1 B3", "P1 B4",
				"P1 Key1", "P1 Key2", "P1 Key3", "P1 Key4", "P1 Key5",
				"P1 Key6", "P1 Key7", "P1 Key8", "P1 Key9" //adelikat: TODO: this was based on a picture, is this the right buttons?, semantics?, can there be multiple controllers?
			}
		};

		void SyncState(Serializer ser)
		{
			//cpu.SyncState(ser); //TODO: z80 does not have this, do it the SMS way?
			ser.Sync("ram", ref ram, false);
			ser.Sync("Lag", ref _lagcount);
			ser.Sync("Frame", ref _frame);
		}

		public ControllerDefinition ControllerDefinition { get { return ColecoVisionControllerDefinition; } }
		public IController Controller { get; set; }

		public int Frame { get { return _frame; } set { _frame = value; } }
		public int LagCount { get { return _lagcount; } set { _lagcount = value; } }
		public bool IsLagFrame { get { return _islag; } }
		private bool _islag = true;
		private int _lagcount = 0;
		private int _frame = 0;

		public byte[] SaveRam { get { return new byte[0]; } }
		public bool DeterministicEmulation { get; set; }
		public bool SaveRamModified { get; set; }
		public void SaveStateText(TextWriter writer) { SyncState(Serializer.CreateTextWriter(writer)); }
		public void LoadStateText(TextReader reader) { SyncState(Serializer.CreateTextReader(reader)); }
		public void SaveStateBinary(BinaryWriter bw) { SyncState(Serializer.CreateBinaryWriter(bw)); }
		public void LoadStateBinary(BinaryReader br) { SyncState(Serializer.CreateBinaryReader(br)); }

		public byte[] SaveStateBinary()
		{
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		public int[] GetVideoBuffer() { return frameBuffer; }
		public int BufferWidth { get { return 320; } }
		public int BufferHeight { get { return 262; } }
		public int BackgroundColor { get { return 0; } }
		public void GetSamples(short[] samples)
		{
		}

		public void DiscardSamples() { }
		public int MaxVolume { get; set; }
		private IList<MemoryDomain> memoryDomains;
		public IList<MemoryDomain> MemoryDomains { get { return memoryDomains; } }
		public MemoryDomain MainMemory { get { return memoryDomains[0]; } }
		public void Dispose() { }
	}
}
