using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common.RamSearchEngine
{
	/// <summary>
	/// Represents a <see cref="IMiniWatch" /> but with added details
	/// to do change tracking. These types add more information but at a cost of
	/// having to poll the ram address on every update
	/// </summary>
	internal interface IMiniWatchDetails : IMiniWatch
	{
		ulong Current { get; }

		int ChangeCount { get; }
		void ClearChangeCount();
		void Update(PreviousType type, MemoryDomain domain, bool bigEndian);
	}

	internal sealed class MiniByteWatchDetailed : MiniByteWatch, IMiniWatchDetails
	{
		private byte _current;

		public MiniByteWatchDetailed(MemoryDomain domain, long addr) : base(domain, addr)
			=> Previous = _current = unchecked((byte) GetValueInner(Address, domain, bigEndian: default));

		public override void SetPreviousToCurrent(MemoryDomain domain, bool bigEndian)
			=> Previous = _current;

		public ulong Current => _current;

		public int ChangeCount { get; private set; }

		public void Update(PreviousType type, MemoryDomain domain, bool bigEndian)
		{
			var newValue = unchecked((byte) GetValueInner(Address, domain, bigEndian: default));
			if (newValue != _current)
			{
				ChangeCount++;
				if (type is PreviousType.LastChange)
				{
					Previous = _current;
				}
			}

			if (type is PreviousType.LastFrame)
			{
				Previous = _current;
			}

			_current = newValue;
		}

		public void ClearChangeCount() => ChangeCount = 0;
	}

	internal sealed class MiniWordWatchDetailed : MiniWordWatch, IMiniWatchDetails
	{
		private ushort _current;

		public MiniWordWatchDetailed(MemoryDomain domain, long addr, bool bigEndian) : base(domain, addr, bigEndian)
			=> Previous = _current = unchecked((ushort) GetValueInner(Address, domain, bigEndian: bigEndian));

		public override void SetPreviousToCurrent(MemoryDomain domain, bool bigEndian)
			=> Previous = _current;

		public ulong Current => _current;

		public int ChangeCount { get; private set; }

		public void Update(PreviousType type, MemoryDomain domain, bool bigEndian)
		{
			var newValue = unchecked((ushort) GetValueInner(Address, domain, bigEndian: bigEndian));
			if (newValue != _current)
			{
				ChangeCount++;
				if (type is PreviousType.LastChange)
				{
					Previous = _current;
				}
			}

			if (type is PreviousType.LastFrame)
			{
				Previous = _current;
			}

			_current = newValue;
		}

		public void ClearChangeCount() => ChangeCount = 0;
	}

	internal sealed class MiniDWordWatchDetailed : MiniDWordWatch, IMiniWatchDetails
	{
		private uint _current;

		public MiniDWordWatchDetailed(MemoryDomain domain, long addr, bool bigEndian) : base(domain, addr, bigEndian)
			=> Previous = _current = unchecked((uint) GetValueInner(Address, domain, bigEndian: bigEndian));

		public override void SetPreviousToCurrent(MemoryDomain domain, bool bigEndian)
			=> Previous = _current;

		public ulong Current => _current;

		public int ChangeCount { get; private set; }

		public void Update(PreviousType type, MemoryDomain domain, bool bigEndian)
		{
			var newValue = unchecked((uint) GetValueInner(Address, domain, bigEndian: bigEndian));
			if (newValue != _current)
			{
				ChangeCount++;
				if (type is PreviousType.LastChange)
				{
					Previous = _current;
				}
			}

			if (type is PreviousType.LastFrame)
			{
				Previous = _current;
			}

			_current = newValue;
		}

		public void ClearChangeCount() => ChangeCount = 0;
	}

	internal sealed class MiniQWordWatchDetailed : MiniQWordWatch, IMiniWatchDetails
	{
		private ulong _current;

		public MiniQWordWatchDetailed(MemoryDomain domain, long addr, bool bigEndian) : base(domain, addr, bigEndian)
			=> Previous = _current = GetValueInner(Address, domain, bigEndian: bigEndian);

		public override void SetPreviousToCurrent(MemoryDomain domain, bool bigEndian)
			=> Previous = _current;

		public ulong Current => _current;

		public int ChangeCount { get; private set; }

		public void Update(PreviousType type, MemoryDomain domain, bool bigEndian)
		{
			var newValue = GetValueInner(Address, domain, bigEndian: bigEndian);
			if (newValue != _current)
			{
				ChangeCount++;
				if (type is PreviousType.LastChange)
				{
					Previous = _current;
				}
			}

			if (type is PreviousType.LastFrame)
			{
				Previous = _current;
			}

			_current = newValue;
		}

		public void ClearChangeCount() => ChangeCount = 0;
	}
}
