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
	}

	internal sealed class MiniByteWatch : IMiniWatch
	{
		public long Address { get; }
		private byte _previous;

		public MiniByteWatch(MemoryDomain domain, long addr)
		{
			Address = addr;
			_previous = domain.PeekByte(Address % domain.Size);
		}

		public long Previous => _previous;

		public void SetPreviousToCurrent(MemoryDomain domain, bool bigEndian)
		{
			_previous = domain.PeekByte(Address % domain.Size);
		}
	}

	internal sealed class MiniWordWatch : IMiniWatch
	{
		public long Address { get; }
		private ushort _previous;

		public MiniWordWatch(MemoryDomain domain, long addr, bool bigEndian)
		{
			Address = addr;
			_previous = domain.PeekUshort(Address % domain.Size, bigEndian);
		}

		public long Previous => _previous;

		public void SetPreviousToCurrent(MemoryDomain domain, bool bigEndian)
		{
			_previous = domain.PeekUshort(Address, bigEndian);
		}
	}

	internal sealed class MiniDWordWatch : IMiniWatch
	{
		public long Address { get; }
		private uint _previous;

		public MiniDWordWatch(MemoryDomain domain, long addr, bool bigEndian)
		{
			Address = addr;
			_previous = domain.PeekUint(Address % domain.Size, bigEndian);
		}

		public long Previous => _previous;

		public void SetPreviousToCurrent(MemoryDomain domain, bool bigEndian)
		{
			_previous = domain.PeekUint(Address, bigEndian);
		}
	}
}
