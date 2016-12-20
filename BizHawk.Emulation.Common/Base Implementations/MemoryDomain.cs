using System;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// A memory region and the functionality to read/write from it
	/// as required by the IMemoryDomains service.
	/// </summary>
	/// <seealso cref="IMemoryDomains" />
	public abstract class MemoryDomain
	{
		public enum Endian { Big, Little, Unknown }

		public string Name { get; protected set; }

		public long Size { get; protected set; }

		public int WordSize { get; protected set; }

		public Endian EndianType { get; protected set; }

		public bool Writable { get; protected set; }

		public abstract byte PeekByte(long addr);

		public abstract void PokeByte(long addr, byte val);

		/// <summary>
		/// creates a memorydomain that references a managed byte array
		/// </summary>
		/// <param name="writable">if false, writes will be ignored</param>
		[Obsolete]
		public static MemoryDomain FromByteArray(string name, Endian endian, byte[] data, bool writable = true, int wordSize = 1)
		{
			return new MemoryDomainByteArray(name, endian, data, writable, wordSize);
		}

		/// <summary>
		/// create a memorydomain that references an unmanaged memory block
		/// </summary>
		/// <param name="data">must remain valid as long as the MemoryDomain exists!</param>
		/// <param name="writable">if false, writes will be ignored</param>
		[Obsolete]
		public unsafe static MemoryDomain FromIntPtr(string name, long size, Endian endian, IntPtr data, bool writable = true, int wordSize = 1)
		{
			return new MemoryDomainIntPtr(name, endian, data, size, writable, wordSize);
		}

		/// <summary>
		/// create a memorydomain that references an unmanaged memory block with 16 bit swaps
		/// </summary>
		/// <param name="data">must remain valid as long as the MemoryDomain exists!</param>
		/// <param name="writable">if false, writes will be ignored</param>
		[Obsolete]
		public unsafe static MemoryDomain FromIntPtrSwap16(string name, long size, Endian endian, IntPtr data, bool writable = true)
		{
			return new MemoryDomainIntPtrSwap16(name, endian, data, size, writable);
		}

		public override string ToString()
		{
			return Name;
		}

		public virtual ushort PeekUshort(long addr, bool bigEndian)
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

		public virtual uint PeekUint(long addr, bool bigEndian)
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

		public virtual void PokeUshort(long addr, ushort val, bool bigEndian)
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

		public virtual void PokeUint(long addr, uint val, bool bigEndian)
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
}
