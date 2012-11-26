using System;
using System.Collections.Generic;
using System.IO;

namespace BizHawk
{
	public class NullEmulator : IEmulator, IVideoProvider, ISoundProvider
	{
		public string SystemId { get { return "NULL"; } }
		public static readonly ControllerDefinition NullController = new ControllerDefinition { Name = "Null Controller" };

		private int[] frameBuffer = new int[256 * 192];
		private Random rand = new Random();
		public CoreInputComm CoreInputComm { get; set; }
		public CoreOutputComm CoreOutputComm { get; private set; }
		public IVideoProvider VideoProvider { get { return this; } }
		public ISoundProvider SoundProvider { get { return this; } }
		public ISyncSoundProvider SyncSoundProvider { get { return new FakeSyncSound(this, 735); } }
		public bool StartAsyncSound() { return true; }
		public void EndAsyncSound() { }
		
		public NullEmulator()
		{
			var domains = new List<MemoryDomain>(1);
			domains.Add(new MemoryDomain("Main RAM", 1, Endian.Little, addr => 0, (a, v) => { }));
			memoryDomains = domains.AsReadOnly();
			CoreOutputComm = new CoreOutputComm();
			CoreInputComm = new CoreInputComm();
		}
		public void ResetFrameCounter()
		{
			Frame = 0;
			// no lag frames on this stub core
		}

		public void FrameAdvance(bool render, bool rendersound)
		{
			if (render == false) return;
			for (int i = 0; i < 256 * 192; i++)
				frameBuffer[i] = Colors.Luminosity((byte)rand.Next());
		}
		public ControllerDefinition ControllerDefinition { get { return NullController; } }
		public IController Controller { get; set; }

		public int Frame { get; set; }
		public int LagCount { get { return 0; } set { return; } }
		public bool IsLagFrame { get { return false; } }

		public byte[] ReadSaveRam() { return null; }
		public void StoreSaveRam(byte[] data) { }
		public void ClearSaveRam() { }
		public bool DeterministicEmulation { get { return true; } }
		public bool SaveRamModified { get; set; }
		public void SaveStateText(TextWriter writer) { }
		public void LoadStateText(TextReader reader) { }
		public void SaveStateBinary(BinaryWriter writer) { }
		public void LoadStateBinary(BinaryReader reader) { }
		public byte[] SaveStateBinary() { return new byte[1]; }
		public int[] GetVideoBuffer() { return frameBuffer; }
        public int VirtualWidth { get { return 256; } }
        public int BufferWidth { get { return 256; } }
		public int BufferHeight { get { return 192; } }
		public int BackgroundColor { get { return 0; } }
		public void GetSamples(short[] samples) { }
		public void DiscardSamples() { }
		public int MaxVolume { get; set; }
		private IList<MemoryDomain> memoryDomains;
		public IList<MemoryDomain> MemoryDomains { get { return memoryDomains; } }
		public MemoryDomain MainMemory { get { return memoryDomains[0]; } }
		public void Dispose() { }
	}

	public class NullSound : ISoundProvider
	{
		public static readonly NullSound SilenceProvider = new NullSound();

		public void GetSamples(short[] samples) { }
		public void DiscardSamples() { }
		public int MaxVolume { get; set; }
	}
}
