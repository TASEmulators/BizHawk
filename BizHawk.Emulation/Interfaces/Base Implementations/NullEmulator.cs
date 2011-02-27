using System;
using System.Collections.Generic;
using System.IO;

namespace BizHawk
{
    public class NullEmulator : IEmulator, IVideoProvider, ISoundProvider
    {
        public string SystemId { get { return "NULL"; } }
        private static readonly ControllerDefinition NullController = new ControllerDefinition { Name = "Null Controller" };

        private int[] frameBuffer = new int[256 * 192];
        private Random rand = new Random();
        public IVideoProvider VideoProvider { get { return this; } }
        public ISoundProvider SoundProvider { get { return this; } }
        public NullEmulator()
        {
            var domains = new List<MemoryDomain>(1);
            domains.Add(new MemoryDomain("Main RAM", 1, Endian.Little, addr=>0, (a,v)=> { }));
            memoryDomains = domains.AsReadOnly();
        }
        public void LoadGame(IGame game) { }
        public void FrameAdvance(bool render)
        {
            if (render == false) return;
            for (int i = 0; i < 256 * 192; i++)
                frameBuffer[i] = Colors.Luminosity((byte)rand.Next());
        }
        public ControllerDefinition ControllerDefinition { get { return NullController; } }
        public IController Controller { get; set; }

        public string GetControllersAsMnemonic() { return "|.|.|"; }
        public void SetControllersAsMnemonic(string mnemonic) { return; }

        public int Frame { get; set; }
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
        private IList<MemoryDomain> memoryDomains;
        public IList<MemoryDomain> MemoryDomains { get { return memoryDomains; } }
        public MemoryDomain MainMemory { get { return memoryDomains[0]; } }

		public object Query(EmulatorQuery query)
		{
			return null;
		}
    }
}
