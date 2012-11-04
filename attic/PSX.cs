using System;
using System.IO;
using System.Collections.Generic;

namespace BizHawk.Emulation.Consoles.Calculator
{
	[CoreVersion("0.0.1", FriendlyName = "PsxHawk!")]
	public class PSX : IEmulator, IVideoProvider, ISoundProvider
	{
		PsxApi api = new PsxApi();

		public string SystemId { get { return "PSX"; } }
		public static readonly ControllerDefinition NullController = new ControllerDefinition { Name = "Null Controller" };

		private int[] frameBuffer = new int[256 * 192];
		private Random rand = new Random();
		public CoreInputComm CoreInputComm { get; set; }
		public CoreOutputComm CoreOutputComm { get; private set; }
		public IVideoProvider VideoProvider { get { return this; } }
		public ISoundProvider SoundProvider { get { return this; } }
		public PSX()
		{
			CoreOutputComm = new CoreOutputComm();
			CoreInputComm = new CoreInputComm();
		}
		public void ResetFrameCounter()
		{
			Frame = 0;
		}

		public void LoadFile(string file)
		{
			api.Load_EXE(file);
		}

		public void FrameAdvance(bool render)
		{
			if (render == false) return;
			for (int i = 0; i < 256 * 192; i++)
				frameBuffer[i] = Colors.Luminosity((byte)rand.Next());
			api.RunForever();
		}
		public ControllerDefinition ControllerDefinition { get { return NullController; } }
		public IController Controller { get; set; }

		public int Frame { get; set; }
		public int LagCount { get { return 0; } set { return; } }
		public bool IsLagFrame { get { return false; } }

		public byte[] SaveRam { get { return new byte[0]; } }
		public bool DeterministicEmulation { get; set; }
		public bool SaveRamModified { get; set; }
		public void SaveStateText(TextWriter writer) { }
		public void LoadStateText(TextReader reader) { }
		public void SaveStateBinary(BinaryWriter writer) { }
		public void LoadStateBinary(BinaryReader reader) { }
		public byte[] SaveStateBinary() { return new byte[1]; }
		public int[] GetVideoBuffer() { return frameBuffer; }
		public int BufferWidth { get { return 256; } }
		public int BufferHeight { get { return 192; } }
		public int BackgroundColor { get { return 0; } }
		public void GetSamples(short[] samples) { }
		public void DiscardSamples() { }
		public int MaxVolume { get; set; }
		public IList<MemoryDomain> MemoryDomains { get { return new List<MemoryDomain>(); } }
		public MemoryDomain MainMemory { get { return null; } }
		public void Dispose() { }
	}
}