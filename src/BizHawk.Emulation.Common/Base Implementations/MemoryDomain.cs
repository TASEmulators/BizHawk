#nullable disable

using System.Buffers.Binary;

using BizHawk.Common;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// A memory region and the functionality to read/write from it
	/// as required by the IMemoryDomains service.
	/// </summary>
	/// <seealso cref="IMemoryDomains" />
	public abstract class MemoryDomain : IMonitor
	{
		public enum Endian
		{
			Big,
			Little,
			Unknown,
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
			if (bigEndian)
			{
				return (ushort)((PeekByte(addr) << 8) | PeekByte(addr + 1));
			}

			return (ushort)(PeekByte(addr) | (PeekByte(addr + 1) << 8));
		}

		public virtual uint PeekUint(long addr, bool bigEndian)
		{
			ReadOnlySpan<byte> scratch = stackalloc byte[]
			{
				PeekByte(addr),
				PeekByte(addr + 1),
				PeekByte(addr + 2),
				PeekByte(addr + 3),
			};
			return bigEndian
				? BinaryPrimitives.ReadUInt32BigEndian(scratch)
				: BinaryPrimitives.ReadUInt32LittleEndian(scratch);
		}

		public virtual void PokeUshort(long addr, ushort val, bool bigEndian)
		{
			if (bigEndian)
			{
				PokeByte(addr + 0, (byte)(val >> 8));
				PokeByte(addr + 1, (byte)val);
			}
			else
			{
				PokeByte(addr + 0, (byte)val);
				PokeByte(addr + 1, (byte)(val >> 8));
			}
		}

		public virtual void PokeUint(long addr, uint val, bool bigEndian)
		{
			Span<byte> scratch = stackalloc byte[4];
			if (bigEndian) BinaryPrimitives.WriteUInt32BigEndian(scratch, val);
			else BinaryPrimitives.WriteUInt32LittleEndian(scratch, val);
			PokeByte(addr, scratch[0]);
			PokeByte(addr + 1, scratch[1]);
			PokeByte(addr + 2, scratch[2]);
			PokeByte(addr + 3, scratch[3]);
		}

		public virtual void BulkPeekByte(long startAddress, Span<byte> values)
		{
			long addr = startAddress;
			using (this.EnterExit())
			{
				for (int i = 0; i < values.Length; i++, addr += sizeof(byte))
				{
					values[i] = PeekByte(addr);
				}
			}
		}

		public virtual void BulkPeekUshort(long startAddress, Span<ushort> values, bool bigEndian)
		{
			long addr = startAddress;
			using (this.EnterExit())
			{
				for (int i = 0; i < values.Length; i++, addr += sizeof(ushort))
				{
					values[i] = PeekUshort(addr, bigEndian);
				}
			}
		}

		public virtual void BulkPeekUint(long startAddress, Span<uint> values, bool bigEndian)
		{
			long addr = startAddress;
			using (this.EnterExit())
			{
				for (int i = 0; i < values.Length; i++, addr += sizeof(uint))
				{
					values[i] = PeekUint(addr, bigEndian);
				}
			}
		}

		public virtual void BulkPokeByte(long startAddress, ReadOnlySpan<byte> values)
		{
			long addr = startAddress;
			using (this.EnterExit())
			{
				for (int i = 0; i < values.Length; i++, addr += sizeof(byte))
				{
					PokeByte(addr, values[i]);
				}
			}
		}

		public virtual void BulkPokeUshort(long startAddress, ReadOnlySpan<ushort> values, bool bigEndian)
		{
			long addr = startAddress;
			using (this.EnterExit())
			{
				for (int i = 0; i < values.Length; i++, addr += sizeof(ushort))
				{
					PokeUshort(addr, values[i], bigEndian);
				}
			}
		}

		public virtual void BulkPokeUint(long startAddress, ReadOnlySpan<uint> values, bool bigEndian)
		{
			long addr = startAddress;
			using (this.EnterExit())
			{
				for (int i = 0; i < values.Length; i++, addr += sizeof(uint))
				{
					PokeUint(addr, values[i], bigEndian);
				}
			}
		}


		public virtual void SendCheatToCore(int addr, byte value, int compare, int compare_type) { }

		/// <summary>
		/// only use this if you are expecting to do a lot of peeks/pokes
		/// no-op if the domain has no monitor
		/// </summary>
		public virtual void Enter() { }

		/// <summary>
		/// only use this if you are expecting to do a lot of peeks/pokes
		/// no-op if the domain has no monitor
		/// </summary>
		public virtual void Exit() { }
	}
}
