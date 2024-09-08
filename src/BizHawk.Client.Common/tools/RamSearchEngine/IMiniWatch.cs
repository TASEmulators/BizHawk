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
		uint Previous { get; }
		void SetPreviousToCurrent(MemoryDomain domain, bool bigEndian);
		bool IsValid(MemoryDomain domain);
	}

	internal class MiniByteWatch : IMiniWatch
	{
		public long Address { get; }
		private protected byte _previous;

		public MiniByteWatch(MemoryDomain domain, long addr)
		{
			Address = addr;
			_previous = GetByte(Address, domain);
		}

		public uint Previous => _previous;

		public bool IsValid(MemoryDomain domain)
		{
			return IsValid(Address, domain);
		}

		public virtual void SetPreviousToCurrent(MemoryDomain domain, bool bigEndian)
		{
			_previous = GetByte(Address, domain);
		}

		public static bool IsValid(long address, MemoryDomain domain)
		{
			return address < domain.Size;
		}

		public static byte GetByte(long address, MemoryDomain domain)
		{
			if (!IsValid(address, domain))
			{
				return 0;
			}

			return domain.PeekByte(address);
		}
	}

	internal class MiniWordWatch : IMiniWatch
	{
		public long Address { get; }
		private protected ushort _previous;

		public MiniWordWatch(MemoryDomain domain, long addr, bool bigEndian)
		{
			Address = addr;
			_previous = GetUshort(Address, domain, bigEndian);
		}

		public uint Previous => _previous;

		public virtual void SetPreviousToCurrent(MemoryDomain domain, bool bigEndian)
		{
			_previous = GetUshort(Address, domain, bigEndian);
		}

		public bool IsValid(MemoryDomain domain)
		{
			return IsValid(Address, domain);
		}

		public static bool IsValid(long address, MemoryDomain domain)
		{
			return address < (domain.Size - 1);
		}

		public static ushort GetUshort(long address, MemoryDomain domain, bool bigEndian)
		{
			if (!IsValid(address, domain))
			{
				return 0;
			}

			return domain.PeekUshort(address, bigEndian);
		}
	}

	internal class MiniDWordWatch : IMiniWatch
	{
		public long Address { get; }
		private protected uint _previous;

		public MiniDWordWatch(MemoryDomain domain, long addr, bool bigEndian)
		{
			Address = addr;
			_previous = GetUint(Address, domain, bigEndian);
		}

		public uint Previous => _previous;

		public virtual void SetPreviousToCurrent(MemoryDomain domain, bool bigEndian)
		{
			_previous = GetUint(Address, domain, bigEndian);
		}

		public bool IsValid(MemoryDomain domain)
		{
			return IsValid(Address, domain);
		}

		public static bool IsValid(long address, MemoryDomain domain)
		{
			return address < (domain.Size - 3);
		}

		public static uint GetUint(long address, MemoryDomain domain, bool bigEndian)
		{
			if (!IsValid(address, domain))
			{
				return 0;
			}

			return domain.PeekUint(address, bigEndian);
		}
	}
}
