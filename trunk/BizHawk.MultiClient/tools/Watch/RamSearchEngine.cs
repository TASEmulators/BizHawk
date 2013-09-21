using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.MultiClient
{
	class RamSearchEngine
	{
		public enum ComparisonOperator { Equal, GreaterThan, GreaterThanEqual, LessThan, LessThanEqual, NotEqual, DifferentBy };
		private List<IMiniWatch> _watchList = new List<IMiniWatch>();
		private Settings _settings;

		public ComparisonOperator Operator;
		public int? DifferentBy;

		#region Constructors

		public RamSearchEngine(Settings settings)
		{
			_settings = settings;
		}

		#endregion

		#region Initialize, Manipulate

		public void Start()
		{
			_watchList.Clear();
			switch (_settings.Size)
			{
				default:
				case Watch.WatchSize.Byte:
					if (_settings.Mode == Settings.SearchMode.Detailed)
					{
						for (int i = 0; i < _settings.Domain.Size; i++)
						{
							_watchList.Add(new MiniByteWatchDetailed(_settings.Domain, i));
						}
					}
					else
					{
						for (int i = 0; i < _settings.Domain.Size; i++)
						{
							_watchList.Add(new MiniByteWatch(_settings.Domain, i));
						}
					}
					break;
				case Watch.WatchSize.Word:
					if (_settings.Mode == Settings.SearchMode.Detailed)
					{
						for (int i = 0; i < _settings.Domain.Size; i += (_settings.CheckMisAligned ? 1 : 2))
						{
							_watchList.Add(new MiniWordWatchDetailed(_settings.Domain, i, _settings.BigEndian));
						}
					}
					else
					{
						for (int i = 0; i < _settings.Domain.Size; i += (_settings.CheckMisAligned ? 1 : 2))
						{
							_watchList.Add(new MiniWordWatch(_settings.Domain, i, _settings.BigEndian));
						}
					}
					break;
				case Watch.WatchSize.DWord:
					if (_settings.Mode == Settings.SearchMode.Detailed)
					{
						for (int i = 0; i < _settings.Domain.Size; i += (_settings.CheckMisAligned ? 1 : 4))
						{
							_watchList.Add(new MiniDWordWatchDetailed(_settings.Domain, i, _settings.BigEndian));
						}
					}
					else
					{
						for (int i = 0; i < _settings.Domain.Size; i += (_settings.CheckMisAligned ? 1 : 4))
						{
							_watchList.Add(new MiniDWordWatch(_settings.Domain, i, _settings.BigEndian));
						}
					}
					break;
			}
		}

		/// <summary>
		/// Exposes the current watch state based on index
		/// </summary>
		public Watch this[int index]
		{
			get
			{
				if (_settings.Mode == Settings.SearchMode.Detailed)
				{
					return Watch.GenerateWatch(
						_settings.Domain,
						_watchList[index].Address,
						_settings.Size,
						_settings.Type,
						_settings.BigEndian,
						_watchList[index].Previous,
						(_watchList[index] as IMiniWatchDetails).ChangeCount
					);
				}
				else
				{
					return Watch.GenerateWatch(
						_settings.Domain,
						_watchList[index].Address,
						_settings.Size,
						_settings.Type,
						_settings.BigEndian,
						_watchList[index].Previous,
						0
					);
				}
			}
		}

		public int Count
		{
			get { return _watchList.Count; }
		}

		public string DomainName
		{
			get { return _settings.Domain.Name; }
		}

		public void Update()
		{
			if (_settings.Mode == Settings.SearchMode.Detailed)
			{
				foreach (IMiniWatchDetails watch in _watchList)
				{
					watch.Update(_settings.PreviousType, _settings.Domain);
				}
			}
			else
			{
				/*TODO*/
			}
		}

		public void SetType(Watch.DisplayType type)
		{
			if (Watch.AvailableTypes(_settings.Size).Contains(type))
			{
				_settings.Type = type;
			}
			else
			{
				throw new InvalidOperationException();
			}
		}

		public void SetEndian(bool bigendian)
		{
			_settings.BigEndian = bigendian;
		}

		public void SetPreviousType(Watch.PreviousType type)
		{
			if (_settings.Mode == Settings.SearchMode.Fast)
			{
				if (type == Watch.PreviousType.LastFrame || type == Watch.PreviousType.LastChange)
				{
					throw new InvalidOperationException();
				}
			}

			_settings.PreviousType = type;
		}

		#endregion

		#region Comparisons

		public void ComparePrevious()
		{
			switch (Operator)
			{
				case ComparisonOperator.Equal:
					_watchList = _watchList.Where(x => x.Previous == GetValue(x.Address)).ToList();
					break;
			}
		}

		public void CompareSpecificValue(int val)
		{

		}

		public void CompareSpecificAddress(int addr)
		{

		}

		public void CompareChanges(int changes)
		{

		}

		public void CompareDifference()
		{

		}

		#endregion

		#region Private parts

		private int GetValue(int addr)
		{
			switch (_settings.Size)
			{
				default:
				case Watch.WatchSize.Byte:
					return _settings.Domain.PeekByte(addr);
				case Watch.WatchSize.Word:
					if (_settings.BigEndian)
					{
						return (ushort)((_settings.Domain.PeekByte(addr) << 8) | (_settings.Domain.PeekByte(addr + 1)));
					}
					else
					{
						return (ushort)((_settings.Domain.PeekByte(addr)) | (_settings.Domain.PeekByte(addr + 1) << 8));
					}
				case Watch.WatchSize.DWord:
					return 0;
			}
		}

		#endregion

		#region Classes

		private interface IMiniWatch
		{
			int Address { get; }
			int Previous { get; }
		}

		private interface IMiniWatchDetails
		{
			int ChangeCount { get; }
			void Update(Watch.PreviousType type, MemoryDomain domain);
		}

		private class MiniByteWatch : IMiniWatch
		{
			public int Address { get; private set; }
			private byte _previous;

			public MiniByteWatch(MemoryDomain domain, int addr)
			{
				Address = addr;
				_previous = domain.PeekByte(addr);
			}

			public int Previous
			{
				get { return _previous; }
			}
		}

		private class MiniWordWatch : IMiniWatch
		{
			public int Address { get; private set; }
			private ushort _previous;

			public MiniWordWatch(MemoryDomain domain, int addr, bool bigEndian)
			{
				Address = addr;
				if (bigEndian)
				{
					_previous = (ushort)((domain.PeekByte(addr) << 8) | (domain.PeekByte(addr + 1)));
				}
				else
				{
					_previous = (ushort)((domain.PeekByte(addr)) | (domain.PeekByte(addr + 1) << 8));
				}
			}

			public int Previous
			{
				get { return _previous; }
			}

		}

		public class MiniDWordWatch : IMiniWatch
		{
			public int Address { get; private set; }
			private uint _previous;

			public MiniDWordWatch(MemoryDomain domain, int addr, bool bigEndian)
			{
				Address = addr;

				if (bigEndian)
				{
					_previous = (uint)((domain.PeekByte(addr) << 24)
						| (domain.PeekByte(addr + 1) << 16)
						| (domain.PeekByte(addr + 2) << 8)
						| (domain.PeekByte(addr + 3) << 0));
				}
				else
				{
					_previous = (uint)((domain.PeekByte(addr) << 0)
						| (domain.PeekByte(addr + 1) << 8)
						| (domain.PeekByte(addr + 2) << 16)
						| (domain.PeekByte(addr + 3) << 24));
				}
			}

			public int Previous
			{
				get { return (int)_previous; }
			}
		}

		private class MiniByteWatchDetailed : IMiniWatch, IMiniWatchDetails
		{
			public int Address { get; private set; }
			private byte _previous;
			int _changecount = 0;

			public MiniByteWatchDetailed(MemoryDomain domain, int addr)
			{
				Address = addr;
				_previous = domain.PeekByte(addr);
			}

			public int Previous
			{
				get { return _previous; }
			}

			public int ChangeCount
			{
				get { return _changecount; }
			}

			public void Update(Watch.PreviousType type, MemoryDomain domain)
			{
				byte value = domain.PeekByte(Address);

				switch (type)
				{
					case Watch.PreviousType.Original:
						if (value != Previous)
						{
							_changecount++;
						}
						break;
					case Watch.PreviousType.LastSearch:
						if (value != _previous)
						{
							_changecount++;
							_previous = value;
						}
						break;
					case Watch.PreviousType.LastFrame:
						value = domain.PeekByte(Address);
						if (value != Previous)
						{
							_changecount++;
						}
						_previous = value;
						break;
					case Watch.PreviousType.LastChange:
						//TODO: this feature requires yet another variable, ugh
						if (value != Previous)
						{
							_changecount++;
						}
						break;
				}
			}
		}

		private class MiniWordWatchDetailed : IMiniWatch, IMiniWatchDetails
		{
			public int Address { get; private set; }
			private ushort _previous;
			int _changecount = 0;

			public MiniWordWatchDetailed(MemoryDomain domain, int addr, bool bigEndian)
			{
				Address = addr;
				if (bigEndian)
				{
					_previous = (ushort)((domain.PeekByte(addr) << 8) | (domain.PeekByte(addr + 1)));
				}
				else
				{
					_previous = (ushort)((domain.PeekByte(addr)) | (domain.PeekByte(addr + 1) << 8));
				}
			}

			public int Previous
			{
				get { return _previous; }
			}

			public int ChangeCount
			{
				get { return _changecount; }
			}

			public void Update(Watch.PreviousType type, MemoryDomain domain)
			{
				ushort value;
				switch (type)
				{
					case Watch.PreviousType.LastChange:
						break;
					case Watch.PreviousType.LastFrame:
						value = domain.PeekByte(Address); //TODO: need big endian passed in
						if (value != Previous)
						{
							_changecount++;
							_previous = value;
						}
						break;
				}
			}
		}

		public class MiniDWordWatchDetailed : IMiniWatch, IMiniWatchDetails
		{
			public int Address { get; private set; }
			private uint _previous;
			int _changecount = 0;

			public MiniDWordWatchDetailed(MemoryDomain domain, int addr, bool bigEndian)
			{
				Address = addr;

				if (bigEndian)
				{
					_previous = (uint)((domain.PeekByte(addr) << 24)
						| (domain.PeekByte(addr + 1) << 16)
						| (domain.PeekByte(addr + 2) << 8)
						| (domain.PeekByte(addr + 3) << 0));
				}
				else
				{
					_previous = (uint)((domain.PeekByte(addr) << 0)
						| (domain.PeekByte(addr + 1) << 8)
						| (domain.PeekByte(addr + 2) << 16)
						| (domain.PeekByte(addr + 3) << 24));
				}
			}

			public int Previous
			{
				get { return (int)_previous; }
			}

			public int ChangeCount
			{
				get { return _changecount; }
			}

			public void Update(Watch.PreviousType type, MemoryDomain domain)
			{
				switch (type)
				{
					case Watch.PreviousType.LastChange:
						break;
					case Watch.PreviousType.LastFrame:
						break;
				}
			}
		}

		public class Settings
		{
			/*Require restart*/
			public enum SearchMode { Fast, Detailed }

			public SearchMode Mode = SearchMode.Detailed;
			public MemoryDomain Domain = Global.Emulator.MainMemory;
			public Watch.WatchSize Size = Watch.WatchSize.Byte;
			public bool CheckMisAligned = false;

			/*Can be changed mid-search*/
			public Watch.DisplayType Type = Watch.DisplayType.Unsigned;
			public bool BigEndian = false;
			public Watch.PreviousType PreviousType = Watch.PreviousType.LastSearch;
		}

		#endregion
	}
}
