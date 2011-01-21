using System;
using System.Collections.Generic;
using System.IO;

namespace BizHawk
{
    public class NullEmulator : IEmulator, IVideoProvider, ISoundProvider
    {
        private static readonly ControllerDefinition NullController = new ControllerDefinition { Name = "Null Controller" };

        private int[] frameBuffer = new int[256 * 192];
        private Random rand = new Random();
        public IVideoProvider VideoProvider { get { return this; } }
        public ISoundProvider SoundProvider { get { return this; } }
        public void LoadGame(IGame game) { }
        public void FrameAdvance(bool render)
        {
            if (render == false) return;
            for (int i = 0; i < 256 * 192; i++)
                frameBuffer[i] = Colors.Luminosity((byte)rand.Next());
        }
        public void HardReset() { }
        public ControllerDefinition ControllerDefinition { get { return NullController; } }
        public IController Controller { get; set; }
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

        public IList<MemoryDomain> MemoryDomains { get { return new List<MemoryDomain>(0); } }
        public MemoryDomain MainMemory { get { return null; } }
    }
}
