using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BizHawk.Emulation.Common
{
	public class MemoryDomain
	{
		public enum Endian { Big, Little, Unknown }

		public readonly string Name;
		public readonly int Size;
		public readonly Endian EndianType;

		public readonly Func<int, byte> PeekByte;
		public readonly Action<int, byte> PokeByte;

		public MemoryDomain(string name, int size, Endian endian, Func<int, byte> peekByte, Action<int, byte> pokeByte)
		{
			Name = name;
			Size = size;
			EndianType = endian;
			PeekByte = peekByte;
			PokeByte = pokeByte;
		}

		public MemoryDomain() { }

		public override string ToString()
		{
			return Name;
		}

		public ushort PeekWord(int addr, bool bigEndian)
		{
			Endian endian = bigEndian ? Endian.Big : Endian.Little;
			switch (endian)
			{
				default:
				case Endian.Big:
					return (ushort)((PeekByte(addr) << 8) | (PeekByte(addr + 1)));
				case Endian.Little:
					return (ushort)((PeekByte(addr)) | (PeekByte(addr + 1) << 8));
			}
		}

		public uint PeekDWord(int addr, bool bigEndian)
		{
			Endian endian = bigEndian ? Endian.Big : Endian.Little;
			switch (endian)
			{
				default:
				case Endian.Big:
					return (uint)((PeekByte(addr) << 24)
					| (PeekByte(addr + 1) << 16)
					| (PeekByte(addr + 2) << 8)
					| (PeekByte(addr + 3) << 0));
				case Endian.Little:
					return (uint)((PeekByte(addr) << 0)
					| (PeekByte(addr + 1) << 8)
					| (PeekByte(addr + 2) << 16)
					| (PeekByte(addr + 3) << 24));
			}
		}

		public void PokeWord(int addr, ushort val, bool bigEndian)
		{
			Endian endian = bigEndian ? Endian.Big : Endian.Little;
			switch (endian)
			{
				default:
				case Endian.Big:
					PokeByte(addr + 0, (byte)(val >> 8));
					PokeByte(addr + 1, (byte)(val));
					break;
				case Endian.Little:
					PokeByte(addr + 0, (byte)(val));
					PokeByte(addr + 1, (byte)(val >> 8));
					break;
			}
		}

		public void PokeDWord(int addr, uint val, bool bigEndian)
		{
			Endian endian = bigEndian ? Endian.Big : Endian.Little;
			switch (endian)
			{
				default:
				case Endian.Big:
					PokeByte(addr + 0, (byte)(val >> 24));
					PokeByte(addr + 1, (byte)(val >> 16));
					PokeByte(addr + 2, (byte)(val >> 8));
					PokeByte(addr + 3, (byte)(val));
					break;
				case Endian.Little:
					PokeByte(addr + 0, (byte)(val));
					PokeByte(addr + 1, (byte)(val >> 8));
					PokeByte(addr + 2, (byte)(val >> 16));
					PokeByte(addr + 3, (byte)(val >> 24));
					break;
			}
		}
	}

	public class MemoryDomainList : ReadOnlyCollection<MemoryDomain>
	{
		/// <summary>
		/// creates a minimal valid MemoryDomainList that does nothing
		/// </summary>
		/// <returns></returns>
		public static MemoryDomainList GetDummyList()
		{
			MemoryDomain dummy = new MemoryDomain("Dummy", 256, MemoryDomain.Endian.Little, (a) => 0, (a, v) => { });
			List<MemoryDomain> tmp = new List<MemoryDomain>(1);
			tmp.Add(dummy);
			return new MemoryDomainList(tmp, 0);
		}

		private readonly int _mainMemoryIndex;

		public MemoryDomainList(IList<MemoryDomain> domains) 
			: base(domains)
		{
		}

		public MemoryDomainList(IList<MemoryDomain> domains, int mainMemoryIndex)
			: this(domains)
		{
			_mainMemoryIndex = mainMemoryIndex;
		}

		public MemoryDomain this[string name]
		{
			get
			{
				return this.FirstOrDefault(x => x.Name == name);
			}
		}

		public MemoryDomain MainMemory
		{
			get
			{
				return this[_mainMemoryIndex];
			}
		}
	}
}
