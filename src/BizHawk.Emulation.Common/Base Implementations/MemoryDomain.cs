using System;
using BizHawk.Common;

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

		public override string ToString() => Name;

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

		public virtual void BulkPeekByte(Range<long> addresses, byte[] values)
		{
			if (addresses == null || values == null)
			{
				throw new ArgumentException();
			}

			if (addresses.EndInclusive - addresses.Start + 1 != values.Length)
			{
				throw new InvalidOperationException("Invalid length of values array");
			}

			for (var i = addresses.Start; i <= addresses.EndInclusive; i++)
			{
				values[i - addresses.Start] = PeekByte(i);
			}
		}

		public virtual void BulkPeekUshort(Range<long> addresses,  bool bigEndian, ushort[] values)
		{
			if (addresses == null || values == null)
			{
				throw new ArgumentException();
			}

			long start = addresses.Start;
			long nAddresses = addresses.EndInclusive - addresses.Start + 1;
			if (nAddresses != values.Length)
			{
				throw new InvalidOperationException("Invalid length of values array");
			}

			for (var i = 0; i < nAddresses; i++)
			{
				values[i] = PeekUshort(start + i*2, bigEndian);
			}
		}

		public virtual void BulkPeekUint(Range<long> addresses, bool bigEndian, uint[] values)
		{
			if (addresses == null || values == null)
			{
				throw new ArgumentException();
			}

			long start = addresses.Start;
			long nAddresses = addresses.EndInclusive - addresses.Start + 1;
			if (nAddresses != values.Length)
			{
				throw new InvalidOperationException("Invalid length of values array");
			}

			for (var i = 0; i<nAddresses; i++)
			{
				values[i] = PeekUint(start + i*4, bigEndian);
			}
		}

		public virtual void SendCheatToCore(int addr, byte value, int compare, int compare_type) { }
	}
}
