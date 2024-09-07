#nullable disable

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
			if (bigEndian)
			{
				return (ushort)((PeekByte(addr) << 8) | PeekByte(addr + 1));
			}

			return (ushort)(PeekByte(addr) | (PeekByte(addr + 1) << 8));
		}

		public virtual uint PeekUint(long addr, bool bigEndian)
		{
			if (bigEndian)
			{
				return (uint)((PeekByte(addr) << 24)
					| (PeekByte(addr + 1) << 16)
					| (PeekByte(addr + 2) << 8)
					| (PeekByte(addr + 3) << 0));
			}

			return (uint)((PeekByte(addr) << 0)
				| (PeekByte(addr + 1) << 8)
				| (PeekByte(addr + 2) << 16)
				| (PeekByte(addr + 3) << 24));
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
			if (bigEndian)
			{
				PokeByte(addr + 0, (byte)(val >> 24));
				PokeByte(addr + 1, (byte)(val >> 16));
				PokeByte(addr + 2, (byte)(val >> 8));
				PokeByte(addr + 3, (byte)val);
			}
			else
			{
				PokeByte(addr + 0, (byte)val);
				PokeByte(addr + 1, (byte)(val >> 8));
				PokeByte(addr + 2, (byte)(val >> 16));
				PokeByte(addr + 3, (byte)(val >> 24));
			}
		}

		public virtual void BulkPeekByte(Range<long> addresses, byte[] values)
		{
			if (addresses is null) throw new ArgumentNullException(paramName: nameof(addresses));
			if (values is null) throw new ArgumentNullException(paramName: nameof(values));

			if ((long)addresses.Count() != values.Length)
			{
				throw new InvalidOperationException("Invalid length of values array");
			}

			using (this.EnterExit())
			{
				for (var i = addresses.Start; i <= addresses.EndInclusive; i++)
				{
					values[i - addresses.Start] = PeekByte(i);
				}
			}
		}

		public virtual void BulkPeekUshort(Range<long> addresses, bool bigEndian, ushort[] values)
		{
			if (addresses is null) throw new ArgumentNullException(paramName: nameof(addresses));
			if (values is null) throw new ArgumentNullException(paramName: nameof(values));

			var start = addresses.Start;
			var end = addresses.EndInclusive + 1;

			if ((start & 1) != 0 || (end & 1) != 0)
				throw new InvalidOperationException("The API contract doesn't define what to do for unaligned reads and writes!");

			if (values.LongLength * 2 != end - start)
			{
				// a longer array could be valid, but nothing needs that so don't support it for now
				throw new InvalidOperationException("Invalid length of values array");
			}

			using (this.EnterExit())
			{
				for (var i = 0; i < values.Length; i++, start += 2)
					values[i] = PeekUshort(start, bigEndian);
			}
		}

		public virtual void BulkPeekUint(Range<long> addresses, bool bigEndian, uint[] values)
		{
			if (addresses is null) throw new ArgumentNullException(paramName: nameof(addresses));
			if (values is null) throw new ArgumentNullException(paramName: nameof(values));

			var start = addresses.Start;
			var end = addresses.EndInclusive + 1;

			if ((start & 3) != 0 || (end & 3) != 0)
				throw new InvalidOperationException("The API contract doesn't define what to do for unaligned reads and writes!");

			if (values.LongLength * 4 != end - start)
			{
				// a longer array could be valid, but nothing needs that so don't support it for now
				throw new InvalidOperationException("Invalid length of values array");
			}

			using (this.EnterExit())
			{
				for (var i = 0; i < values.Length; i++, start += 4)
					values[i] = PeekUint(start, bigEndian);
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
