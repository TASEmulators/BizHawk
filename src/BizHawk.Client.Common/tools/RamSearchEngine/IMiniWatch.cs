using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common.RamSearchEngine
{
	/// <summary>
	/// Represents a Ram address for watching in the <see cref="RamSearchEngine" />
	/// With the minimal details necessary for searching
	/// </summary>
	internal interface IMiniWatch
	{
		long Address { get; }
		long Previous { get; } // do not store sign extended variables in here.
		void SetPreviousToCurrent(MemoryDomain domain, bool bigEndian);
		bool IsValid(MemoryDomain domain);
	}

	internal sealed class MiniByteWatch : IMiniWatch
	{
		public long Address { get; }
		private byte _previous;

		public MiniByteWatch(MemoryDomain domain, long addr)
		{
			Address = addr;
			_previous = GetByte(Address, domain);
		}

		public long Previous => _previous;

		public void SetPreviousToCurrent(MemoryDomain domain, bool bigEndian)
		{
			_previous = GetByte(Address, domain);
		}

		public bool IsValid(MemoryDomain domain)
		{
			return Address < domain.Size;
		}

		public static byte GetByte(long address, MemoryDomain domain)
		{
			if (address >= domain.Size)
			{
				return 0;
			}

			return domain.PeekByte(address);
		}
	}

	internal sealed class MiniWordWatch : IMiniWatch
	{
		public long Address { get; }
		private ushort _previous;

		public MiniWordWatch(MemoryDomain domain, long addr, bool bigEndian)
		{
			Address = addr;
			_previous = GetUshort(Address, domain, bigEndian);
		}

		public long Previous => _previous;

		public void SetPreviousToCurrent(MemoryDomain domain, bool bigEndian)
		{
			_previous = GetUshort(Address, domain, bigEndian);
		}

		public bool IsValid(MemoryDomain domain)
		{
			return Address < (domain.Size - 1);
		}

		public static ushort GetUshort(long address, MemoryDomain domain, bool bigEndian)
		{
			if (address >= (domain.Size - 1))
			{
				return 0;
			}

			return domain.PeekUshort(address, bigEndian);
		}
	}

	internal sealed class MiniDWordWatch : IMiniWatch
	{
		public long Address { get; }
		private uint _previous;

		public MiniDWordWatch(MemoryDomain domain, long addr, bool bigEndian)
		{
			Address = addr;
			_previous = GetUint(Address, domain, bigEndian);
		}

		public long Previous => _previous;

		public void SetPreviousToCurrent(MemoryDomain domain, bool bigEndian)
		{
			_previous = GetUint(Address, domain, bigEndian);
		}

		public bool IsValid(MemoryDomain domain)
		{
			return Address < (domain.Size - 3);
		}

		public static uint GetUint(long address, MemoryDomain domain, bool bigEndian)
		{
			if (address >= (domain.Size - 3))
			{
				return 0;
			}

			return domain.PeekUint(address, bigEndian);
		}
	}
}
