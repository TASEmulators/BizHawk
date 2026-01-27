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
		private const string ERR_MSG_DEST_WRONG_LEN = "Invalid length of values array";

		private const string ERR_MSG_NOT_ALIGNED = "The API contract doesn't define what to do for unaligned reads and writes!";

		protected const string ERR_MSG_TOO_MANY_BYTES_REQ = "too many bytes requested, would read beyond bounds of domain";

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

		public virtual byte[] BulkPeekByte(Range<long> addresses)
		{
			var buf = new byte[addresses.Count()];
			BulkPeekByte((ulong) addresses.Start, buf);
			return buf;
		}

		public virtual void BulkPeekByte(ulong srcStartOffset, Span<byte> dstBuffer)
		{
			using var handle = this.EnterExit();
			var iSrc = (long) srcStartOffset;
			var endExcl = (long) srcStartOffset + dstBuffer.Length;
			var iDst = 0;
			while (iSrc != endExcl) dstBuffer[iDst++] = PeekByte(iSrc++);
		}

		public virtual void BulkPeekByte(Range<long> addresses, byte[] values)
		{
			if (addresses is null) throw new ArgumentNullException(paramName: nameof(addresses));
			if (values is null) throw new ArgumentNullException(paramName: nameof(values));

			if ((ulong) values.Length != addresses.Count())
			{
				throw new InvalidOperationException(ERR_MSG_DEST_WRONG_LEN);
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

			if (start % 2 is not 0 || end % 2 is not 0) throw new InvalidOperationException(ERR_MSG_NOT_ALIGNED);

			if (checked((ulong) values.Length * 2UL) != addresses.Count())
			{
				// a longer array could be valid, but nothing needs that so don't support it for now
				throw new InvalidOperationException(ERR_MSG_DEST_WRONG_LEN);
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

			if (start % 4 is not 0 || end % 4 is not 0) throw new InvalidOperationException(ERR_MSG_NOT_ALIGNED);

			if (checked((ulong) values.Length * 4UL) != addresses.Count())
			{
				// a longer array could be valid, but nothing needs that so don't support it for now
				throw new InvalidOperationException(ERR_MSG_DEST_WRONG_LEN);
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
