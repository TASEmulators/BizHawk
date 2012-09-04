using System;
using System.Collections.Generic;
using System.IO;

namespace BizHawk
{
	public interface IEmulator : IDisposable
	{
		IVideoProvider VideoProvider { get; }
		ISoundProvider SoundProvider { get; }

		ControllerDefinition ControllerDefinition { get; }
		IController Controller { get; set; }

		void FrameAdvance(bool render);

		int Frame { get; }
		int LagCount { get; set; }
		bool IsLagFrame { get; }
		string SystemId { get; }
		bool DeterministicEmulation { get; set; }

		/// <summary>
		/// Don't edit this data, it may be useless as it may be a copy or some compiled data.
		/// use StoreSaveRam if you want to store sram into the core.
		/// Well, this rule is being violated. This comment is just a reminder that this is a bad architecture
		/// </summary>
		byte[] ReadSaveRam { get; }
		bool SaveRamModified { get; set; }

		void ResetFrameCounter();
		void SaveStateText(TextWriter writer);
		void LoadStateText(TextReader reader);
		void SaveStateBinary(BinaryWriter writer);
		void LoadStateBinary(BinaryReader reader);
		byte[] SaveStateBinary();

		// Arbitrary extensible core comm mechanism
		CoreInputComm CoreInputComm { get; set; }
		CoreOutputComm CoreOutputComm { get; }

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

		public MemoryDomain()
		{
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
}
