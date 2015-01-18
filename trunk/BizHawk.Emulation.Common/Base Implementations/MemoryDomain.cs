using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BizHawk.Emulation.Common
{
	public class MemoryDomain
	{
		public enum Endian { Big, Little, Unknown }

		public MemoryDomain(string name, long size, Endian endian, Func<long, byte> peekByte, Action<long, byte> pokeByte)
		{
			Name = name;
			Size = size;
			EndianType = endian;
			PeekByte = peekByte;
			PokeByte = pokeByte;
		}

		public string Name { get; private set; }

		public long Size { get; private set; }

		public Endian EndianType { get; private set; }

		public Func<long, byte> PeekByte { get; private set; }

		public Action<long, byte> PokeByte { get; private set; }

		/// <summary>
		/// create a memorydomain that references an unmanaged memory block
		/// </summary>
		/// <param name="data">must remain valid as long as the MemoryDomain exists!</param>
		/// <param name="writable">if false, writes will be ignored</param>
		/// <returns></returns>
		public unsafe static MemoryDomain FromIntPtr(string name, int size, Endian endian, IntPtr data, bool writable = true)
		{
			if (data == IntPtr.Zero)
				throw new ArgumentNullException("data");
			if (size <= 0)
				throw new ArgumentOutOfRangeException("size");
			byte* p = (byte*)data;
			return new MemoryDomain
			(
				name,
				size,
				endian,
				delegate(long addr)
				{
					if ((uint)addr >= size)
						throw new ArgumentOutOfRangeException();
					return p[addr];
				},
				delegate(long addr, byte val)
				{
					if (writable)
					{
						if ((uint)addr >= size)
							throw new ArgumentOutOfRangeException();
						p[addr] = val;
					}
				}
			);
		}

		/// <summary>
		/// create a memorydomain that references an unmanaged memory block with 16 bit swaps
		/// </summary>
		/// <param name="data">must remain valid as long as the MemoryDomain exists!</param>
		/// <param name="writable">if false, writes will be ignored</param>
		/// <returns></returns>
		public unsafe static MemoryDomain FromIntPtrSwap16(string name, int size, Endian endian, IntPtr data, bool writable = true)
		{
			if (data == IntPtr.Zero)
				throw new ArgumentNullException("data");
			if (size <= 0)
				throw new ArgumentOutOfRangeException("size");
			byte* p = (byte*)data;
			return new MemoryDomain
			(
				name,
				size,
				endian,
				delegate(long addr)
				{
					if (addr < 0 || addr >= size)
						throw new ArgumentOutOfRangeException();
					return p[addr ^ 1];
				},
				delegate(long addr, byte val)
				{
					if (writable)
					{
						if (addr < 0 || addr >= size)
							throw new ArgumentOutOfRangeException();
						p[addr ^ 1] = val;
					}
				}
			);
		}

		public override string ToString()
		{
			return Name;
		}

		public ushort PeekWord(long addr, bool bigEndian)
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

		public uint PeekDWord(long addr, bool bigEndian)
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

		public void PokeWord(long addr, ushort val, bool bigEndian)
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

		public void PokeDWord(long addr, uint val, bool bigEndian)
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

	public class MemoryDomainList : ReadOnlyCollection<MemoryDomain>, IMemoryDomains
	{
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

		public bool HasCheatDomain
		{
			get
			{
				return this.Any(x => x.Name == "System Bus");
			}
		}

		public MemoryDomain CheatDomain
		{
			get
			{
				return this.FirstOrDefault(x => x.Name == "System Bus");
			}
		}
	}
}
