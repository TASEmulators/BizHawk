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
		uint Current { get; }
		int ChangeCount { get; }
		void ClearChangeCount();
		void Update(PreviousType type, MemoryDomain domain, bool bigEndian);
	}

	internal sealed class MiniByteWatchDetailed : MiniByteWatch, IMiniWatchDetails
	{
		private byte _current;

		public MiniByteWatchDetailed(MemoryDomain domain, long addr) : base(domain, addr)
		{
			_previous = _current = GetByte(Address, domain);
		}

		public override void SetPreviousToCurrent(MemoryDomain domain, bool bigEndian)
		{
			_previous = _current;
		}

		public uint Current => _current;

		public int ChangeCount { get; private set; }

		public void Update(PreviousType type, MemoryDomain domain, bool bigEndian)
		{
			var newValue = GetByte(Address, domain);
			if (newValue != _current)
			{
				ChangeCount++;
				if (type is PreviousType.LastChange)
				{
					_previous = _current;
				}
			}

			if (type is PreviousType.LastFrame)
			{
				_previous = _current;
			}

			_current = newValue;
		}

		public void ClearChangeCount() => ChangeCount = 0;
	}

	internal sealed class MiniWordWatchDetailed : MiniWordWatch, IMiniWatchDetails
	{
		private ushort _current;

		public MiniWordWatchDetailed(MemoryDomain domain, long addr, bool bigEndian) : base(domain, addr, bigEndian)
		{
			_previous = _current = GetUshort(Address, domain, bigEndian);
		}

		public override void SetPreviousToCurrent(MemoryDomain domain, bool bigEndian)
		{
			_previous = _current;
		}

		public uint Current => _current;

		public int ChangeCount { get; private set; }

		public void Update(PreviousType type, MemoryDomain domain, bool bigEndian)
		{
			var newValue = GetUshort(Address, domain, bigEndian);
			if (newValue != _current)
			{
				ChangeCount++;
				if (type is PreviousType.LastChange)
				{
					_previous = _current;
				}
			}

			if (type is PreviousType.LastFrame)
			{
				_previous = _current;
			}

			_current = newValue;
		}

		public void ClearChangeCount() => ChangeCount = 0;
	}

	internal sealed class MiniDWordWatchDetailed : MiniDWordWatch, IMiniWatchDetails
	{
		private uint _current;

		public MiniDWordWatchDetailed(MemoryDomain domain, long addr, bool bigEndian) : base(domain, addr, bigEndian)
		{
			_previous = _current = GetUint(Address, domain, bigEndian);
		}

		public override void SetPreviousToCurrent(MemoryDomain domain, bool bigEndian)
		{
			_previous = _current;
		}

		public uint Current => _current;

		public int ChangeCount { get; private set; }

		public void Update(PreviousType type, MemoryDomain domain, bool bigEndian)
		{
			var newValue = GetUint(Address, domain, bigEndian);
			if (newValue != _current)
			{
				ChangeCount++;
				if (type is PreviousType.LastChange)
				{
					_previous = _current;
				}
			}

			if (type is PreviousType.LastFrame)
			{
				_previous = _current;
			}

			_current = newValue;
		}

		public void ClearChangeCount() => ChangeCount = 0;
	}
}
