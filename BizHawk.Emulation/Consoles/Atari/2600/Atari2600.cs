using System;
using System.Collections.Generic;
using System.IO;

namespace BizHawk
{
	public partial class Atari2600 : IEmulator, IVideoProvider, ISoundProvider
	{
		public string SystemId { get { return "A26"; } }

		private int[] frameBuffer = new int[320 * 262];
		public CoreInputComm CoreInputComm { get; set; }
		public CoreOutputComm CoreOutputComm { get; private set; }
		public IVideoProvider VideoProvider { get { return this; } }
		public ISoundProvider SoundProvider { get { return this; } }
		public byte[] ram = new byte[128];
		public Atari2600(GameInfo game, byte[] rom)
		{
			var domains = new List<MemoryDomain>(1);
			domains.Add(new MemoryDomain("Main RAM", 1, Endian.Little, addr => 0, (a, v) => { }));
			memoryDomains = domains.AsReadOnly();
			CoreOutputComm = new CoreOutputComm();
			CoreInputComm = new CoreInputComm();
			this.rom = rom;
			HardReset();
		}
		public void ResetFrameCounter()
		{
			Frame = 0;
		}

		public static readonly ControllerDefinition Atari2600ControllerDefinition = new ControllerDefinition
		{
			Name = "Atari 2600 Basic Controller",
			BoolButtons =
			{
				"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Button"
			}
		};

		void SyncState(Serializer ser)
		{
			cpu.SyncState(ser);
			ser.Sync("ram", ref ram, false);
		}

		public ControllerDefinition ControllerDefinition { get { return Atari2600ControllerDefinition; } }
		public IController Controller { get; set; }

		public int Frame { get; set; }
		public int LagCount { get { return 0; } set { return; } }
		public bool IsLagFrame { get { return false; } }

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
		public void GetSamples(short[] samples) { }
		public void DiscardSamples() { }
		public int MaxVolume { get; set; }
		private IList<MemoryDomain> memoryDomains;
		public IList<MemoryDomain> MemoryDomains { get { return memoryDomains; } }
		public MemoryDomain MainMemory { get { return memoryDomains[0]; } }
		public void Dispose() { }
	}

}
