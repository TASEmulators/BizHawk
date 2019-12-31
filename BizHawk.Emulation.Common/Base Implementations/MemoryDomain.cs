using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// A memory region and the functionality to read/write from it
	/// as required by the IMemoryDomains service.
	/// </summary>
	/// <seealso cref="IMemoryDomains" />
	public abstract class MemoryDomain
	{
		public enum Endian
		{
			Big, Little, Unknown
		}

		public string Name { get; protected set; }

		public long Size { get; protected set; }

		public int WordSize { get; protected set; }

		public Endian EndianType { get; protected set; }

		public bool Writable { get; protected set; }

		public abstract byte PeekByte(long addr);

		public abstract void PokeByte(long addr, byte val);

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
					return (ushort)((PeekByte(addr) << 8) | PeekByte(addr + 1));
				case Endian.Little:
					return (ushort)(PeekByte(addr) | (PeekByte(addr + 1) << 8));
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
					PokeByte(addr + 1, (byte)val);
					break;
				case Endian.Little:
					PokeByte(addr + 0, (byte)val);
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
					PokeByte(addr + 3, (byte)val);
					break;
				case Endian.Little:
					PokeByte(addr + 0, (byte)val);
					PokeByte(addr + 1, (byte)(val >> 8));
					PokeByte(addr + 2, (byte)(val >> 16));
					PokeByte(addr + 3, (byte)(val >> 24));
					break;
			}
		}

		public virtual IEnumerable<byte> BulkPeekByte(ICollection<long> addresses)
		{
			if (addresses != null)
			{
				foreach (var address in addresses)
				{
					yield return PeekByte(address);
				}
			}
		}

		public virtual IEnumerable<ushort> BulkPeekUshort(ICollection<long> addresses, bool bigEndian)
		{
			if (addresses != null)
			{
				foreach (var address in addresses)
				{
					yield return PeekUshort(address, bigEndian);
				}
			}
		}

		public virtual IEnumerable<uint> BulkPeekUint(ICollection<long> addresses, bool bigEndian)
		{
			if (addresses != null)
			{
				foreach (var address in addresses)
				{
					yield return PeekUint(address, bigEndian);
				}
			}
		}
	}
}
