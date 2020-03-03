using BizHawk.Emulation.Common;

namespace  BizHawk.Client.Common
{
	/// <summary>
	/// Represents a <see cref="IMiniWatch" /> but with added details
	/// to do change tracking. These types add more information but at a cost of
	/// having to poll the ram address on every update
	/// </summary>
	internal interface IMiniWatchDetails : IMiniWatch
	{
		int ChangeCount { get; }

		void ClearChangeCount();
		void Update(PreviousType type, MemoryDomain domain, bool bigEndian);
	}

	internal sealed class MiniByteWatchDetailed : IMiniWatchDetails
	{
		public long Address { get; }

		private byte _previous;
		private byte _prevFrame;

		public MiniByteWatchDetailed(MemoryDomain domain, long addr)
		{
			Address = addr;
			SetPreviousToCurrent(domain, false);
		}

		public void SetPreviousToCurrent(MemoryDomain domain, bool bigEndian)
		{
			_previous = _prevFrame = domain.PeekByte(Address % domain.Size);
		}

		public long Previous => _previous;

		public int ChangeCount { get; private set; }

		public void Update(PreviousType type, MemoryDomain domain, bool bigEndian)
		{
			var value = domain.PeekByte(Address % domain.Size);

			if (value != _prevFrame)
			{
				ChangeCount++;
			}

			switch (type)
			{
				case PreviousType.Original:
				case PreviousType.LastSearch:
					break;
				case PreviousType.LastFrame:
					_previous = _prevFrame;
					break;
				case PreviousType.LastChange:
					if (_prevFrame != value)
					{
						_previous = _prevFrame;
					}

					break;
			}

			_prevFrame = value;
		}

		public void ClearChangeCount() => ChangeCount = 0;
	}

	internal sealed class MiniWordWatchDetailed : IMiniWatch, IMiniWatchDetails
	{
		public long Address { get; }

		private ushort _previous;
		private ushort _prevFrame;

		public MiniWordWatchDetailed(MemoryDomain domain, long addr, bool bigEndian)
		{
			Address = addr;
			SetPreviousToCurrent(domain, bigEndian);
		}

		public void SetPreviousToCurrent(MemoryDomain domain, bool bigEndian)
		{
			_previous = _prevFrame = domain.PeekUshort(Address % domain.Size, bigEndian);
		}

		public long Previous => _previous;

		public int ChangeCount { get; private set; }

		public void Update(PreviousType type, MemoryDomain domain, bool bigEndian)
		{
			var value = domain.PeekUshort(Address % domain.Size, bigEndian);
			if (value != Previous)
			{
				ChangeCount++;
			}

			switch (type)
			{
				case PreviousType.Original:
				case PreviousType.LastSearch:
					break;
				case PreviousType.LastFrame:
					_previous = _prevFrame;
					break;
				case PreviousType.LastChange:
					if (_prevFrame != value)
					{
						_previous = _prevFrame;
					}

					break;
			}

			_prevFrame = value;
		}

		public void ClearChangeCount() => ChangeCount = 0;
	}

	internal sealed class MiniDWordWatchDetailed : IMiniWatch, IMiniWatchDetails
	{
		public long Address { get; }

		private uint _previous;
		private uint _prevFrame;

		public MiniDWordWatchDetailed(MemoryDomain domain, long addr, bool bigEndian)
		{
			Address = addr;
			SetPreviousToCurrent(domain, bigEndian);
		}

		public void SetPreviousToCurrent(MemoryDomain domain, bool bigEndian)
		{
			_previous = _prevFrame = domain.PeekUint(Address % domain.Size, bigEndian);
		}

		public long Previous => (int)_previous;

		public int ChangeCount { get; private set; }

		public void Update(PreviousType type, MemoryDomain domain, bool bigEndian)
		{
			var value = domain.PeekUint(Address % domain.Size, bigEndian);
			if (value != Previous)
			{
				ChangeCount++;
			}

			switch (type)
			{
				case PreviousType.Original:
				case PreviousType.LastSearch:
					break;
				case PreviousType.LastFrame:
					_previous = _prevFrame;
					break;
				case PreviousType.LastChange:
					if (_prevFrame != value)
					{
						_previous = _prevFrame;
					}

					break;
			}

			_prevFrame = value;
		}

		public void ClearChangeCount() => ChangeCount = 0;
	}
}
