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

		uint GetValue(MemoryDomain domain, bool bigEndian);

		void SetPreviousToCurrent(MemoryDomain domain, bool bigEndian);
		bool IsValid(MemoryDomain domain);
	}

	internal abstract class MiniWatchBase : IMiniWatch
	{
		public long Address { get; }

		public uint Previous { get; protected set; }

		protected MiniWatchBase(long addr, uint prevValue)
		{
			Address = addr;
			Previous = prevValue;
		}

		public uint GetValue(MemoryDomain domain, bool bigEndian)
			=> IsValid(domain) ? GetValueInner(Address, domain, bigEndian: bigEndian) : default;

		protected abstract uint GetValueInner(long address, MemoryDomain domain, bool bigEndian);

		public bool IsValid(MemoryDomain domain)
			=> IsValid(Address, domain);

		protected abstract bool IsValid(long address, MemoryDomain domain);

		public virtual void SetPreviousToCurrent(MemoryDomain domain, bool bigEndian)
			=> Previous = GetValueInner(Address, domain, bigEndian: bigEndian);
	}

	internal class MiniByteWatch : MiniWatchBase
	{
		public MiniByteWatch(MemoryDomain domain, long addr)
			: base(addr: addr, prevValue: domain.PeekByte(addr)) {}

		protected override uint GetValueInner(long address, MemoryDomain domain, bool bigEndian)
			=> domain.PeekByte(address);

		protected override bool IsValid(long address, MemoryDomain domain)
			=> 0L <= address && address < domain.Size;
	}

	internal class MiniWordWatch : MiniWatchBase
	{
		public MiniWordWatch(MemoryDomain domain, long addr, bool bigEndian)
			: base(addr: addr, prevValue: domain.PeekUshort(addr, bigEndian: bigEndian)) {}

		protected override uint GetValueInner(long address, MemoryDomain domain, bool bigEndian)
			=> domain.PeekUshort(address, bigEndian);

		protected override bool IsValid(long address, MemoryDomain domain)
			=> 0L <= address && address <= domain.Size - sizeof(ushort);
	}

	internal class MiniDWordWatch : MiniWatchBase
	{
		public MiniDWordWatch(MemoryDomain domain, long addr, bool bigEndian)
			: base(addr: addr, prevValue: domain.PeekUint(addr, bigEndian: bigEndian)) {}

		protected override uint GetValueInner(long address, MemoryDomain domain, bool bigEndian)
			=> domain.PeekUint(address, bigEndian);

		protected override bool IsValid(long address, MemoryDomain domain)
			=> 0L <= address && address <= domain.Size - sizeof(uint);
	}
}
