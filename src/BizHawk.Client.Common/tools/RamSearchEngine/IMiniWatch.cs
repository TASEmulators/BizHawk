using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common.RamSearchEngine
{
	/// <summary>
	/// Represents a Ram address for watching in the <see cref="RamSearchEngine" />
	/// </summary>
	internal interface IMiniWatch
	{
		long Address { get; }
		uint Previous { get; }
		uint Current { get; }
		int ChangeCount { get; }
		void ClearChangeCount();
		void SetPreviousToCurrent();
		bool IsValid(MemoryDomain domain);
		void Update(PreviousType type, MemoryDomain domain, bool bigEndian);
	}

	internal sealed class MiniByteWatch : IMiniWatch
	{
		public long Address { get; }

		private byte _previous;
		private byte _current;

		public MiniByteWatch(MemoryDomain domain, long addr)
		{
			Address = addr;
			_previous = _current = GetByte(domain);
		}

		public void SetPreviousToCurrent()
		{
			_previous = _current;
		}

		public uint Previous => _previous;
		public uint Current => _current;

		public int ChangeCount { get; private set; }

		public void Update(PreviousType type, MemoryDomain domain, bool bigEndian)
		{
			var newValue = GetByte(domain);

			if (newValue != _current)
			{
				ChangeCount++;
				if (type == PreviousType.LastChange)
				{
					_previous = _current;
				}
			}

			if (type == PreviousType.LastFrame)
				_previous = _current;

			_current = newValue;
		}

		public void ClearChangeCount() => ChangeCount = 0;

		public bool IsValid(MemoryDomain domain) => Address < domain.Size;

		public byte GetByte(MemoryDomain domain)
		{
			return IsValid(domain) ? domain.PeekByte(Address) : (byte)0;
		}
	}

	internal sealed class MiniWordWatch : IMiniWatch
	{
		public long Address { get; }

		private ushort _previous;
		private ushort _current;

		public MiniWordWatch(MemoryDomain domain, long addr, bool bigEndian)
		{
			Address = addr;
			_previous = _current = GetUshort(domain, bigEndian);
		}

		public void SetPreviousToCurrent()
		{
			_previous = _current;
		}

		public uint Previous => _previous;
		public uint Current => _current;

		public int ChangeCount { get; private set; }

		public void Update(PreviousType type, MemoryDomain domain, bool bigEndian)
		{
			var newValue = GetUshort(domain, bigEndian);

			if (newValue != _current)
			{
				ChangeCount++;
				if (type == PreviousType.LastChange)
				{
					_previous = _current;
				}
			}

			if (type == PreviousType.LastFrame)
				_previous = _current;

			_current = newValue;
		}

		public void ClearChangeCount() => ChangeCount = 0;

		public bool IsValid(MemoryDomain domain) => Address < domain.Size - 1;

		private ushort GetUshort(MemoryDomain domain, bool bigEndian)
		{
			return IsValid(domain) ? domain.PeekUshort(Address, bigEndian) : (ushort)0;
		}
	}

	internal sealed class MiniDWordWatch : IMiniWatch
	{
		public long Address { get; }

		private uint _previous;
		private uint _current;

		public MiniDWordWatch(MemoryDomain domain, long addr, bool bigEndian)
		{
			Address = addr;
			_previous = _current = GetUint(domain, bigEndian);
		}

		public void SetPreviousToCurrent()
		{
			_previous = _current;
		}

		public uint Previous => _previous;
		public uint Current => _current;

		public int ChangeCount { get; private set; }

		public void Update(PreviousType type, MemoryDomain domain, bool bigEndian)
		{
			var newValue = GetUint(domain, bigEndian);

			if (newValue != _current)
			{
				ChangeCount++;
				if (type == PreviousType.LastChange)
				{
					_previous = _current;
				}
			}

			if (type == PreviousType.LastFrame)
				_previous = _current;

			_current = newValue;
		}

		public void ClearChangeCount() => ChangeCount = 0;

		public bool IsValid(MemoryDomain domain) => Address < domain.Size - 3;

		private uint GetUint(MemoryDomain domain, bool bigEndian)
		{
			return IsValid(domain) ? domain.PeekUint(Address, bigEndian) : 0;
		}
	}
}
