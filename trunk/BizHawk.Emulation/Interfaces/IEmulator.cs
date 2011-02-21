using System;
using System.Collections.Generic;
using System.IO;

namespace BizHawk
{
    public interface IEmulator
    {
        IVideoProvider VideoProvider { get; }
        ISoundProvider SoundProvider { get; }

        ControllerDefinition ControllerDefinition { get; }
        IController Controller { get; set; }

        void LoadGame(IGame game);
        void FrameAdvance(bool render);

        int Frame { get; }
        string SystemId { get; }
        bool DeterministicEmulation { get; set; }

        byte[] SaveRam { get; }
        bool SaveRamModified { get; set; }

        // TODO: should IEmulator expose a way of enumerating the Options it understands?
        // (the answer is yes)

        void SaveStateText(TextWriter writer);
        void LoadStateText(TextReader reader);
        void SaveStateBinary(BinaryWriter writer);
        void LoadStateBinary(BinaryReader reader);
        byte[] SaveStateBinary();

		//arbitrary extensible query mechanism
		object Query(EmulatorQuery query);

        // ----- Client Debugging API stuff -----
        IList<MemoryDomain> MemoryDomains { get; }
        MemoryDomain MainMemory { get; }
    }

    public class MemoryDomain
    {
        public readonly string Name;
        public readonly int Size;
        public readonly Endian Endian;

        public readonly Func<int, byte> PeekByte;
        public readonly Action<int, byte> PokeByte;

        public MemoryDomain(string name, int size, Endian endian, Func<int, byte> peekByte, Action<int, byte> pokeByte)
        {
            Name = name;
            Size = size;
            Endian = endian;
            PeekByte = peekByte;
            PokeByte = pokeByte;
        }

        public override string ToString()
        {
            return Name;
        }
    }
    
    public enum Endian { Big, Little, Unknown }

    public enum DisplayType { NTSC, PAL }

	public enum EmulatorQuery
	{
		VsyncRate
	}
}
