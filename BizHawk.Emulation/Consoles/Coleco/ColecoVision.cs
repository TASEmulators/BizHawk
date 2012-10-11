using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using BizHawk.Emulation.CPUs.Z80;
using BizHawk.Emulation.Sound;
using BizHawk.Emulation.Consoles.Sega;

namespace BizHawk.Emulation.Consoles.Coleco
{
	public partial class ColecoVision : IEmulator, IVideoProvider, ISoundProvider
	{
		public string SystemId { get { return "Coleco"; } }
		public GameInfo game;
		public int[] frameBuffer = new int[256 * 192];
		public CoreInputComm CoreInputComm { get; set; }
		public CoreOutputComm CoreOutputComm { get; private set; }
		public IVideoProvider VideoProvider { get { return this; } }
		public ISoundProvider SoundProvider { get { return this; } }
		public ISyncSoundProvider SyncSoundProvider { get { return new FakeSyncSound(this, 735); } }
		public bool StartAsyncSound() { return true; }
		public void EndAsyncSound() { }
		public byte[] ram = new byte[2048];

		public DisplayType DisplayType { get; set; } //TOOD: delete me

		public ColecoVision(GameInfo game, byte[] rom)
		{
			cpu = new Z80A();
			Vdp = new VDP(this, cpu, VdpMode.SMS, DisplayType);

			var domains = new List<MemoryDomain>(1);
			domains.Add(new MemoryDomain("Main RAM", 1024, Endian.Little, addr => ram[1023], (addr, value) => ram[addr & 1023] = value));
			memoryDomains = domains.AsReadOnly();
			CoreOutputComm = new CoreOutputComm();
			CoreInputComm = new CoreInputComm();
			this.rom = rom;
			this.game = game;
			HardReset();
		}

		public void ResetFrameCounter() { _frame = 0; }

		public static readonly ControllerDefinition ColecoVisionControllerDefinition = new ControllerDefinition
		{
			Name = "ColecoVision Basic Controller",
			BoolButtons = 
			{
				"P1 Up", "P1 Down", "P1 Left", "P1 Right",
				"P1 L1", "P1 L2", "P1 R1", "P1 R2",
				"P1 Key1", "P1 Key2", "P1 Key3", "P1 Key4", "P1 Key5",
				"P1 Key6", "P1 Key7", "P1 Key8", "P1 Key9", "P1 Star", "P1 Pound" //adelikat: TODO: can there be multiple controllers?
			}
		};

		void SyncState(Serializer ser)
		{
			//cpu.SyncState(ser); //TODO: z80 does not have this, do it the SMS way?
			ser.Sync("ram", ref ram, false);
			ser.Sync("Lag", ref _lagcount);
			ser.Sync("Frame", ref _frame);
			ser.Sync("IsLag", ref _islag);
		}

		public ControllerDefinition ControllerDefinition { get { return ColecoVisionControllerDefinition; } }
		public IController Controller { get; set; }

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

		public bool DeterministicEmulation { get { return true; } }
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
        public int VirtualWidth { get { return 256; } }
        public int BufferWidth { get { return 256; } }
		public int BufferHeight { get { return 192; } }
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
