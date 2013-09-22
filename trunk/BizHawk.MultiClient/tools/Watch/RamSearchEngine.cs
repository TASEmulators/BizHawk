using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.MultiClient
{
	class RamSearchEngine
	{
		public enum ComparisonOperator { Equal, GreaterThan, GreaterThanEqual, LessThan, LessThanEqual, NotEqual, DifferentBy };
		public enum Compare { Previous, SpecificValue, SpecificAddress, Changes, Difference }

		private List<IMiniWatch> _watchList = new List<IMiniWatch>();
		private Settings _settings;

		public Compare CompareTo = Compare.Previous;
		public int? CompareValue = null;
		public ComparisonOperator Operator = ComparisonOperator.Equal;
		public int? DifferentBy = null;

		#region Constructors

		public RamSearchEngine(Settings settings)
		{
			_settings = settings;
		}

		#endregion

		#region API

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

		public int DoSearch()
		{
			int before = _watchList.Count;
			switch (CompareTo)
			{
				default:
				case RamSearchEngine.Compare.Previous:
					ComparePrevious();
					break;
				case RamSearchEngine.Compare.SpecificValue:
					CompareSpecificValue();
					break;
				case RamSearchEngine.Compare.SpecificAddress:
					CompareSpecificAddress();
					break;
				case RamSearchEngine.Compare.Changes:
					CompareChanges();
					break;
				case RamSearchEngine.Compare.Difference:
					throw new NotImplementedException();
					
			}

			if (_settings.PreviousType == Watch.PreviousType.LastSearch)
			{
				SetPrevousToCurrent();
			}

			return before - _watchList.Count;
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

		public void SetPrevousToCurrent()
		{
			_watchList.ForEach(x => x.SetPreviousToCurrent(_settings.Domain, _settings.BigEndian));
		}

		public void ClearChangeCounts()
		{
			if (_settings.Mode == Settings.SearchMode.Detailed)
			{
				foreach (IMiniWatchDetails watch in _watchList.Cast<IMiniWatchDetails>())
				{
					watch.ClearChangeCount();
				}
			}
		}

		public void RemoveRange(List<int> addresses)
		{
			_watchList = _watchList.Where(x => !addresses.Contains(x.Address)).ToList();
		}

		public void AddRange(List<int> addresses, bool append)
		{
			if (!append)
			{
				_watchList.Clear();
			}

			switch(_settings.Size)
			{
				default:
				case Watch.WatchSize.Byte:
					if (_settings.Mode == Settings.SearchMode.Detailed)
					{
						foreach(var addr in addresses) { _watchList.Add(new MiniByteWatchDetailed(_settings.Domain, addr)); }
					}
					else
					{
						foreach(var addr in addresses) { _watchList.Add(new MiniByteWatch(_settings.Domain, addr)); }
					}
					break;
				case Watch.WatchSize.Word:
					if (_settings.Mode == Settings.SearchMode.Detailed)
					{
						foreach (var addr in addresses) { _watchList.Add(new MiniWordWatchDetailed(_settings.Domain, addr, _settings.BigEndian)); }
					}
					else
					{
						foreach (var addr in addresses) { _watchList.Add(new MiniWordWatch(_settings.Domain, addr, _settings.BigEndian)); }
					}
					break;
				case Watch.WatchSize.DWord:
					if (_settings.Mode == Settings.SearchMode.Detailed)
					{
						foreach (var addr in addresses) { _watchList.Add(new MiniDWordWatchDetailed(_settings.Domain, addr, _settings.BigEndian)); }
					}
					else
					{
						foreach (var addr in addresses) { _watchList.Add(new MiniDWordWatch(_settings.Domain, addr, _settings.BigEndian)); }
					}
					break;
			}
		}

		#endregion

		#region Comparisons

		private void ComparePrevious()
		{
			switch (Operator)
			{
				case ComparisonOperator.Equal:
					_watchList = _watchList.Where(x => GetValue(x.Address) == x.Previous).ToList();
					break;
				case ComparisonOperator.NotEqual:
					_watchList = _watchList.Where(x => GetValue(x.Address) != x.Previous).ToList();
					break;
				case ComparisonOperator.GreaterThan:
					_watchList = _watchList.Where(x => GetValue(x.Address) > x.Previous).ToList();
					break;
				case ComparisonOperator.GreaterThanEqual:
					_watchList = _watchList.Where(x => GetValue(x.Address) >= x.Previous).ToList();
					break;
				case ComparisonOperator.LessThan:
					_watchList = _watchList.Where(x => GetValue(x.Address) < x.Previous).ToList();
					break;
				case ComparisonOperator.LessThanEqual:
					_watchList = _watchList.Where(x => GetValue(x.Address) <= x.Previous).ToList();
					break;
				case ComparisonOperator.DifferentBy:
					if (DifferentBy.HasValue)
					{
						_watchList = _watchList.Where(x => (GetValue(x.Address) + DifferentBy.Value == x.Previous) || (GetValue(x.Address) - DifferentBy.Value == x.Previous)).ToList();
					}
					else
					{
						throw new InvalidOperationException();
					}
					break;
			}
		}

		private void CompareSpecificValue()
		{
			if (CompareValue.HasValue)
			{
				switch (Operator)
				{
					case ComparisonOperator.Equal:
						_watchList = _watchList.Where(x => GetValue(x.Address) == CompareValue.Value).ToList();
						break;
					case ComparisonOperator.NotEqual:
						_watchList = _watchList.Where(x => GetValue(x.Address) != CompareValue.Value).ToList();
						break;
					case ComparisonOperator.GreaterThan:
						_watchList = _watchList.Where(x =>  GetValue(x.Address) > CompareValue.Value).ToList();
						break;
					case ComparisonOperator.GreaterThanEqual:
						_watchList = _watchList.Where(x => GetValue(x.Address) >= CompareValue.Value).ToList();
						break;
					case ComparisonOperator.LessThan:
						_watchList = _watchList.Where(x => GetValue(x.Address) < CompareValue.Value).ToList();
						break;
					case ComparisonOperator.LessThanEqual:
						_watchList = _watchList.Where(x => GetValue(x.Address) <= CompareValue.Value).ToList();
						break;
					case ComparisonOperator.DifferentBy:
						if (DifferentBy.HasValue)
						{
							_watchList = _watchList.Where(x => (GetValue(x.Address) + DifferentBy.Value == CompareValue.Value) || (GetValue(x.Address) - DifferentBy.Value == CompareValue.Value)).ToList();
						}
						else
						{
							throw new InvalidOperationException();
						}
						break;
				}
			}
			else
			{
				throw new InvalidOperationException();
			}
		}

		private void CompareSpecificAddress()
		{
			if (CompareValue.HasValue)
			{
				switch (Operator)
				{
					case ComparisonOperator.Equal:
						_watchList = _watchList.Where(x => x.Address == CompareValue.Value).ToList();
						break;
					case ComparisonOperator.NotEqual:
						_watchList = _watchList.Where(x => x.Address != CompareValue.Value).ToList();
						break;
					case ComparisonOperator.GreaterThan:
						_watchList = _watchList.Where(x => x.Address > CompareValue.Value).ToList();
						break;
					case ComparisonOperator.GreaterThanEqual:
						_watchList = _watchList.Where(x => x.Address >= CompareValue.Value).ToList();
						break;
					case ComparisonOperator.LessThan:
						_watchList = _watchList.Where(x => x.Address < CompareValue.Value).ToList();
						break;
					case ComparisonOperator.LessThanEqual:
						_watchList = _watchList.Where(x => x.Address <= CompareValue.Value).ToList();
						break;
					case ComparisonOperator.DifferentBy:
						if (DifferentBy.HasValue)
						{
							_watchList = _watchList.Where(x => (x.Address + DifferentBy.Value == CompareValue.Value) || (x.Address - DifferentBy.Value == CompareValue.Value)).ToList();
						}
						else
						{
							throw new InvalidOperationException();
						}
						break;
				}
			}
			else
			{
				throw new InvalidOperationException();
			}
		}

		public void CompareChanges()
		{
			if (_settings.Mode == Settings.SearchMode.Detailed && CompareValue.HasValue)
			{
				switch (Operator)
				{
					case ComparisonOperator.Equal:
						_watchList = _watchList
							.Cast<IMiniWatchDetails>()
							.Where(x => x.ChangeCount == CompareValue.Value)
							.Cast<IMiniWatch>()
							.ToList();
						break;
					case ComparisonOperator.NotEqual:
						_watchList = _watchList
							.Cast<IMiniWatchDetails>()
							.Where(x => x.ChangeCount != CompareValue.Value)
							.Cast<IMiniWatch>()
							.ToList();
						break;
					case ComparisonOperator.GreaterThan:
						_watchList = _watchList
							.Cast<IMiniWatchDetails>()
							.Where(x => x.ChangeCount > CompareValue.Value)
							.Cast<IMiniWatch>()
							.ToList();
						break;
					case ComparisonOperator.GreaterThanEqual:
						_watchList = _watchList
							.Cast<IMiniWatchDetails>()
							.Where(x => x.ChangeCount >= CompareValue.Value)
							.Cast<IMiniWatch>()
							.ToList();
						break;
					case ComparisonOperator.LessThan:
						_watchList = _watchList
							.Cast<IMiniWatchDetails>()
							.Where(x => x.ChangeCount < CompareValue.Value)
							.Cast<IMiniWatch>()
							.ToList();
						break;
					case ComparisonOperator.LessThanEqual:
						_watchList = _watchList
							.Cast<IMiniWatchDetails>()
							.Where(x => x.ChangeCount <= CompareValue.Value)
							.Cast<IMiniWatch>()
							.ToList();
						break;
					case ComparisonOperator.DifferentBy:
						if (DifferentBy.HasValue)
						{
							_watchList = _watchList
								.Cast<IMiniWatchDetails>()
								.Where(x => (x.ChangeCount + DifferentBy.Value == CompareValue.Value) || (x.ChangeCount - DifferentBy.Value == CompareValue.Value))
								.Cast<IMiniWatch>()
								.ToList();
						}
						else
						{
							throw new InvalidOperationException();
						}
						break;
				}
			}
			else
			{
				throw new InvalidCastException();
			}
		}

		private void CompareDifference()
		{
			if (CompareValue.HasValue)
			{
				switch (Operator)
				{
					case ComparisonOperator.Equal:
						_watchList = _watchList.Where(x => (GetValue(x.Address) - x.Previous) == CompareValue.Value).ToList();
						break;
					case ComparisonOperator.NotEqual:
						_watchList = _watchList.Where(x => (GetValue(x.Address) - x.Previous) != CompareValue.Value).ToList();
						break;
					case ComparisonOperator.GreaterThan:
						_watchList = _watchList.Where(x => (GetValue(x.Address) - x.Previous) > CompareValue.Value).ToList();
						break;
					case ComparisonOperator.GreaterThanEqual:
						_watchList = _watchList.Where(x => (GetValue(x.Address) - x.Previous) >= CompareValue.Value).ToList();
						break;
					case ComparisonOperator.LessThan:
						_watchList = _watchList.Where(x => (GetValue(x.Address) - x.Previous) < CompareValue.Value).ToList();
						break;
					case ComparisonOperator.LessThanEqual:
						_watchList = _watchList.Where(x => (GetValue(x.Address) - x.Previous) <= CompareValue.Value).ToList();
						break;
					case ComparisonOperator.DifferentBy:
						if (DifferentBy.HasValue)
						{
							_watchList = _watchList.Where(x => (GetValue(x.Address) - x.Previous + DifferentBy.Value == CompareValue) || (GetValue(x.Address) - x.Previous - DifferentBy.Value == x.Previous)).ToList();
						}
						else
						{
							throw new InvalidOperationException();
						}
						break;
				}
			}
			else
			{
				throw new InvalidCastException();
			}
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
					throw new NotImplementedException();
			}
		}

		#endregion

		#region Classes

		private interface IMiniWatch
		{
			int Address { get; }
			int Previous { get; }
			void SetPreviousToCurrent(MemoryDomain domain, bool bigendian);
		}

		private interface IMiniWatchDetails
		{
			int ChangeCount { get; }
			
			void ClearChangeCount();
			void Update(Watch.PreviousType type, MemoryDomain domain);
		}

		private class MiniByteWatch : IMiniWatch
		{
			public int Address { get; private set; }
			private byte _previous;

			public MiniByteWatch(MemoryDomain domain, int addr)
			{
				Address = addr;
				SetPreviousToCurrent(domain, false);
			}

			public int Previous
			{
				get { return _previous; }
			}

			public void SetPreviousToCurrent(MemoryDomain domain, bool bigendian)
			{
				_previous = domain.PeekByte(Address);
			}
		}

		private class MiniWordWatch : IMiniWatch
		{
			public int Address { get; private set; }
			private ushort _previous;

			public MiniWordWatch(MemoryDomain domain, int addr, bool bigEndian)
			{
				Address = addr;
				SetPreviousToCurrent(domain, bigEndian);
			}

			public int Previous
			{
				get { return _previous; }
			}

			public void SetPreviousToCurrent(MemoryDomain domain, bool bigendian)
			{
				_previous = domain.PeekWord(Address, bigendian ? Endian.Big : Endian.Little);
			}
		}

		public class MiniDWordWatch : IMiniWatch
		{
			public int Address { get; private set; }
			private uint _previous;

			public MiniDWordWatch(MemoryDomain domain, int addr, bool bigEndian)
			{
				Address = addr;
				SetPreviousToCurrent(domain, bigEndian);
			}

			public int Previous
			{
				get { return (int)_previous; }
			}

			public void SetPreviousToCurrent(MemoryDomain domain, bool bigendian)
			{
				_previous = domain.PeekDWord(Address, bigendian ? Endian.Big : Endian.Little);
			}
		}

		private sealed class MiniByteWatchDetailed : IMiniWatch, IMiniWatchDetails
		{
			public int Address { get; private set; }
			private byte _previous;
			int _changecount = 0;

			public MiniByteWatchDetailed(MemoryDomain domain, int addr)
			{
				Address = addr;
				SetPreviousToCurrent(domain, false);
			}

			public void SetPreviousToCurrent(MemoryDomain domain, bool bigendian)
			{
				_previous = domain.PeekByte(Address);
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

			public void ClearChangeCount()
			{
				_changecount = 0;
			}
		}

		private sealed class MiniWordWatchDetailed : IMiniWatch, IMiniWatchDetails
		{
			public int Address { get; private set; }
			private ushort _previous;
			int _changecount = 0;

			public MiniWordWatchDetailed(MemoryDomain domain, int addr, bool bigEndian)
			{
				Address = addr;
				SetPreviousToCurrent(domain, bigEndian);
			}

			public void SetPreviousToCurrent(MemoryDomain domain, bool bigendian)
			{
				_previous = domain.PeekWord(Address, bigendian ? Endian.Big : Endian.Little);
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

			public void ClearChangeCount()
			{
				_changecount = 0;
			}
		}

		public sealed class MiniDWordWatchDetailed : IMiniWatch, IMiniWatchDetails
		{
			public int Address { get; private set; }
			private uint _previous;
			int _changecount = 0;

			public MiniDWordWatchDetailed(MemoryDomain domain, int addr, bool bigEndian)
			{
				Address = addr;
				SetPreviousToCurrent(domain, bigEndian);
			}

			public void SetPreviousToCurrent(MemoryDomain domain, bool bigendian)
			{
				_previous = domain.PeekDWord(Address, bigendian ? Endian.Big : Endian.Little);
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

			public void ClearChangeCount()
			{
				_changecount = 0;
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
