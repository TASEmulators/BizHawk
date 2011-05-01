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

        string GetControllersAsMnemonic();
        void SetControllersAsMnemonic(string mnemonic);

        void LoadGame(IGame game);
        void FrameAdvance(bool render);

        int Frame { get; }
        int LagCount { get; set; }
        bool IsLagFrame { get; }
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

		//perhaps inconveniently, this is a struct. 
		//this is a premature optimization, since I anticipate having millions of these and i didnt want millions of objects
		public struct FreezeData
		{
			public FreezeData(Flag flags, byte value)
			{
				this.flags = flags;
				this.value = value;
			}
			public readonly byte value;
			public readonly Flag flags;
			public enum Flag : byte
			{
				None = 0,
				Frozen = 1,
			}

			public bool IsFrozen { get { return (flags & Flag.Frozen) != 0; } }
			public static FreezeData Empty { get { return new FreezeData(); } }
		}

        public readonly Func<int, byte> PeekByte;
        public readonly Action<int, byte> PokeByte;
		public Func<int, FreezeData> GetFreeze;
		public Action<int, FreezeData> SetFreeze;

        public MemoryDomain(string name, int size, Endian endian, Func<int, byte> peekByte, Action<int, byte> pokeByte)
        {
            Name = name;
            Size = size;
            Endian = endian;
            PeekByte = peekByte;
            PokeByte = pokeByte;
        }

        public MemoryDomain(MemoryDomain domain)
        {
            Name = domain.Name;
            Size = domain.Size;
            Endian = domain.Endian;
            PeekByte = domain.PeekByte;
            PokeByte = domain.PokeByte;
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
