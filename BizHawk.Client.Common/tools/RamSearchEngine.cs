using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;

namespace BizHawk.Client.Common
{
	public class RamSearchEngine
	{
		public enum ComparisonOperator { Equal, GreaterThan, GreaterThanEqual, LessThan, LessThanEqual, NotEqual, DifferentBy }
		public enum Compare { Previous, SpecificValue, SpecificAddress, Changes, Difference }

		private int? _differentBy; //zero 07-sep-2014 - this isnt ideal. but dont bother changing it (to a long, for instance) until it can support floats. maybe store it as a double here.

		private Compare _compareTo = Compare.Previous;
		private long? _compareValue;
		private ComparisonOperator _operator = ComparisonOperator.Equal;

		private List<IMiniWatch> _watchList = new List<IMiniWatch>();
		private readonly Settings _settings;
		private readonly UndoHistory<IMiniWatch> _history = new UndoHistory<IMiniWatch>(true);
		private bool _keepHistory = true;
		private bool _isSorted = true; // Tracks whether or not the list is sorted by address, if it is, binary search can be used for finding watches

		public RamSearchEngine(Settings settings, IMemoryDomains memoryDomains)
		{
			_settings = new Settings(memoryDomains);
			_settings.Mode = settings.Mode;
			_settings.Domain = settings.Domain;
			_settings.Size = settings.Size;
			_settings.CheckMisAligned = settings.CheckMisAligned;
			_settings.Type = settings.Type;
			_settings.BigEndian = settings.BigEndian;
			_settings.PreviousType = settings.PreviousType;
		}

		public RamSearchEngine(Settings settings, IMemoryDomains memoryDomains, Compare compareTo, long? compareValue, int? differentBy)
			: this(settings, memoryDomains)
		{
			_compareTo = compareTo;
			_differentBy = differentBy;
			_compareValue = compareValue;
		}

		#region API

		public IEnumerable<long> OutOfRangeAddress
		{
			get
			{
				return _watchList
					.Where(watch => watch.Address >= Domain.Size)
					.Select(watch => watch.Address);
			}
		}

		public void Start()
		{
			_history.Clear();
			var domain = _settings.Domain;
			var listSize = domain.Size;
			if (!_settings.CheckMisAligned)
			{
				listSize /= (int)_settings.Size;
			}

			_watchList = new List<IMiniWatch>((int)listSize);

			switch (_settings.Size)
			{
				default:
				case WatchSize.Byte:
					if (_settings.Mode == Settings.SearchMode.Detailed)
					{
						for (int i = 0; i < domain.Size; i++)
						{
							_watchList.Add(new MiniByteWatchDetailed(domain, i));
						}
					}
					else
					{
						for (int i = 0; i < domain.Size; i++)
						{
							_watchList.Add(new MiniByteWatch(domain, i));
						}
					}

					break;
				case WatchSize.Word:
					if (_settings.Mode == Settings.SearchMode.Detailed)
					{
						for (int i = 0; i < domain.Size - 1; i += _settings.CheckMisAligned ? 1 : 2)
						{
							_watchList.Add(new MiniWordWatchDetailed(domain, i, _settings.BigEndian));
						}
					}
					else
					{
						for (int i = 0; i < domain.Size - 1; i += _settings.CheckMisAligned ? 1 : 2)
						{
							_watchList.Add(new MiniWordWatch(domain, i, _settings.BigEndian));
						}
					}

					break;
				case WatchSize.DWord:
					if (_settings.Mode == Settings.SearchMode.Detailed)
					{
						for (int i = 0; i < domain.Size - 3; i += _settings.CheckMisAligned ? 1 : 4)
						{
							_watchList.Add(new MiniDWordWatchDetailed(domain, i, _settings.BigEndian));
						}
					}
					else
					{
						for (int i = 0; i < domain.Size - 3; i += _settings.CheckMisAligned ? 1 : 4)
						{
							_watchList.Add(new MiniDWordWatch(domain, i, _settings.BigEndian));
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
						string.Empty,
						0,
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
						string.Empty,
						0,
						_watchList[index].Previous,
						0
					);
				}
			}
		}

		public int DoSearch()
		{
			int before = _watchList.Count;

			switch (_compareTo)
			{
				default:
				case Compare.Previous:
					_watchList = ComparePrevious(_watchList).ToList();
					break;
				case Compare.SpecificValue:
					_watchList = CompareSpecificValue(_watchList).ToList();
					break;
				case Compare.SpecificAddress:
					_watchList = CompareSpecificAddress(_watchList).ToList();
					break;
				case Compare.Changes:
					_watchList = CompareChanges(_watchList).ToList();
					break;
				case Compare.Difference:
					_watchList = CompareDifference(_watchList).ToList();
					break;
			}

			if (_settings.PreviousType == PreviousType.LastSearch)
			{
				SetPreviousToCurrent();
			}

			if (_keepHistory)
			{
				_history.AddState(_watchList);
			}

			return before - _watchList.Count;
		}

		public bool Preview(long address)
		{
			IEnumerable<IMiniWatch> listOfOne;

			if (_isSorted)
			{
				listOfOne = Enumerable.Repeat(_watchList.BinarySearch(x => x.Address, address), 1);
			}
			else
			{
				listOfOne = Enumerable.Repeat(_watchList.FirstOrDefault(x => x.Address == address), 1);
			}

			switch (_compareTo)
			{
				default:
				case Compare.Previous:
					return !ComparePrevious(listOfOne).Any();
				case Compare.SpecificValue:
					return !CompareSpecificValue(listOfOne).Any();
				case Compare.SpecificAddress:
					return !CompareSpecificAddress(listOfOne).Any();
				case Compare.Changes:
					return !CompareChanges(listOfOne).Any();
				case Compare.Difference:
					return !CompareDifference(listOfOne).Any();
			}
		}

		public int Count
		{
			get { return _watchList.Count; }
		}

		public Settings.SearchMode Mode { get { return _settings.Mode; } }

		public MemoryDomain Domain
		{
			get { return _settings.Domain; }
		}

		public Compare CompareTo
		{
			get
			{
				return _compareTo;
			}

			set
			{
				if (CanDoCompareType(value))
				{
					_compareTo = value;
				}
				else
				{
					throw new InvalidOperationException();
				}
			}
		}

		public long? CompareValue
		{
			get { return _compareValue; }
			set { _compareValue = value; }
		}

		public ComparisonOperator Operator
		{
			get { return _operator; }
			set { _operator = value; }
		}

		public int? DifferentBy
		{
			get { return _differentBy; }
			set { _differentBy = value; }
		}

		public void Update()
		{
			if (_settings.Mode == Settings.SearchMode.Detailed)
			{
				foreach (IMiniWatchDetails watch in _watchList)
				{
					watch.Update(_settings.PreviousType, _settings.Domain, _settings.BigEndian);
				}
			}
			else
			{
				return;
			}
		}

		public void SetType(DisplayType type)
		{
			_settings.Type = type;
		}

		public void SetEndian(bool bigendian)
		{
			_settings.BigEndian = bigendian;
		}

		public void SetPreviousType(PreviousType type)
		{
			if (_settings.Mode == Settings.SearchMode.Fast)
			{
				if (type == PreviousType.LastFrame)
				{
					throw new InvalidOperationException();
				}
			}

			_settings.PreviousType = type;
		}

		public void SetPreviousToCurrent()
		{
			_watchList.ForEach(x => x.SetPreviousToCurrent(_settings.Domain, _settings.BigEndian));
		}

		public void ClearChangeCounts()
		{
			if (_settings.Mode == Settings.SearchMode.Detailed)
			{
				foreach (var watch in _watchList.Cast<IMiniWatchDetails>())
				{
					watch.ClearChangeCount();
				}
			}
		}

		/// <summary>
		/// Remove a set of watches
		/// However, this should not be used with large data sets (100k or more) as it uses a contains logic to perform the task
		/// </summary>
		public void RemoveSmallWatchRange(IEnumerable<Watch> watches)
		{
			if (_keepHistory)
			{
				_history.AddState(_watchList);
			}

			var addresses = watches.Select(x => x.Address);
			var removeList = _watchList.Where(x => addresses.Contains(x.Address)).ToList();
			_watchList = _watchList.Except(removeList).ToList();
		}

		public void RemoveRange(IEnumerable<int> indices)
		{
			if (_keepHistory)
			{
				_history.AddState(_watchList);
			}

			var removeList = indices.Select(i => _watchList[i]); // This will fail after int.MaxValue but RAM Search fails on domains that large anyway
			_watchList = _watchList.Except(removeList).ToList();
		}

		public void AddRange(List<long> addresses, bool append)
		{
			if (!append)
			{
				_watchList.Clear();
			}

			switch (_settings.Size)
			{
				default:
				case WatchSize.Byte:
					if (_settings.Mode == Settings.SearchMode.Detailed)
					{
						foreach (var addr in addresses)
						{
							_watchList.Add(new MiniByteWatchDetailed(_settings.Domain, addr));
						}
					}
					else
					{
						foreach (var addr in addresses)
						{
							_watchList.Add(new MiniByteWatch(_settings.Domain, addr));
						}
					}

					break;
				case WatchSize.Word:
					if (_settings.Mode == Settings.SearchMode.Detailed)
					{
						foreach (var addr in addresses)
						{
							_watchList.Add(new MiniWordWatchDetailed(_settings.Domain, addr, _settings.BigEndian));
						}
					}
					else
					{
						foreach (var addr in addresses)
						{
							_watchList.Add(new MiniWordWatch(_settings.Domain, addr, _settings.BigEndian));
						}
					}

					break;
				case WatchSize.DWord:
					if (_settings.Mode == Settings.SearchMode.Detailed)
					{
						foreach (var addr in addresses)
						{
							_watchList.Add(new MiniDWordWatchDetailed(_settings.Domain, addr, _settings.BigEndian));
						}
					}
					else
					{
						foreach (var addr in addresses)
						{
							_watchList.Add(new MiniDWordWatch(_settings.Domain, addr, _settings.BigEndian));
						}
					}

					break;
			}
		}

		public void Sort(string column, bool reverse)
		{
			_isSorted = false;
			switch (column)
			{
				case WatchList.ADDRESS:
					if (reverse)
					{
						_watchList = _watchList.OrderByDescending(x => x.Address).ToList();
					}
					else
					{
						_watchList = _watchList.OrderBy(x => x.Address).ToList();
						_isSorted = true;
					}

					break;
				case WatchList.VALUE:
					if (reverse)
					{
						_watchList = _watchList.OrderByDescending(x => GetValue(x.Address)).ToList();
					}
					else
					{
						_watchList = _watchList.OrderBy(x => GetValue(x.Address)).ToList();
					}

					break;
				case WatchList.PREV:
					if (reverse)
					{
						_watchList = _watchList.OrderByDescending(x => x.Previous).ToList();
					}
					else
					{
						_watchList = _watchList.OrderBy(x => x.Previous).ToList();
					}

					break;
				case WatchList.CHANGES:
					if (_settings.Mode == Settings.SearchMode.Detailed)
					{
						if (reverse)
						{
							_watchList = _watchList
								.Cast<IMiniWatchDetails>()
								.OrderByDescending(x => x.ChangeCount)
								.Cast<IMiniWatch>().ToList();
						}
						else
						{
							_watchList = _watchList
								.Cast<IMiniWatchDetails>()
								.OrderBy(x => x.ChangeCount)
								.Cast<IMiniWatch>().ToList();
						}
					}

					break;
				case WatchList.DIFF:
					if (reverse)
					{
						_watchList = _watchList.OrderByDescending(x => (GetValue(x.Address) - x.Previous)).ToList();
					}
					else
					{
						_watchList = _watchList.OrderBy(x => (GetValue(x.Address) - x.Previous)).ToList();
					}

					break;
			}
		}

		#endregion

		#region Undo API

		public bool UndoEnabled
		{
			get { return _keepHistory; }
			set { _keepHistory = value; }
		}

		public bool CanUndo
		{
			get { return _keepHistory && _history.CanUndo; }
		}

		public bool CanRedo
		{
			get { return _keepHistory && _history.CanRedo; }
		}

		public void ClearHistory()
		{
			_history.Clear();
		}

		public void Undo()
		{
			if (_keepHistory)
			{
				_watchList = _history.Undo().ToList();
			}
		}

		public void Redo()
		{
			if (_keepHistory)
			{
				_watchList = _history.Redo().ToList();
			}
		}

		#endregion

		#region Comparisons

		private IEnumerable<IMiniWatch> ComparePrevious(IEnumerable<IMiniWatch> watchList)
		{
			switch (_operator)
			{
				default:
				case ComparisonOperator.Equal:
					return watchList.Where(x => GetValue(x.Address) == x.Previous);
				case ComparisonOperator.NotEqual:
					return watchList.Where(x => GetValue(x.Address) != x.Previous);

				case ComparisonOperator.GreaterThan:
					if (_settings.Type == DisplayType.Float)
					{
						return watchList.Where(x => ToFloat(GetValue(x.Address)) > ToFloat(x.Previous));
					}

					return watchList.Where(x => SignExtendAsNeeded(GetValue(x.Address)) > SignExtendAsNeeded(x.Previous));

				case ComparisonOperator.GreaterThanEqual:
					if (_settings.Type == DisplayType.Float)
					{
						return watchList.Where(x => ToFloat(GetValue(x.Address)) >= ToFloat(x.Previous));
					}

					return watchList.Where(x => SignExtendAsNeeded(GetValue(x.Address)) >= SignExtendAsNeeded(x.Previous));

				case ComparisonOperator.LessThan:
					if (_settings.Type == DisplayType.Float)
					{
						return watchList.Where(x => ToFloat(GetValue(x.Address)) < ToFloat(x.Previous));
					}

					return watchList.Where(x => SignExtendAsNeeded(GetValue(x.Address)) < SignExtendAsNeeded(x.Previous));

				case ComparisonOperator.LessThanEqual:
					if (_settings.Type == DisplayType.Float)
					{
						return watchList.Where(x => ToFloat(GetValue(x.Address)) <= ToFloat(x.Previous));
					}

					return watchList.Where(x => SignExtendAsNeeded(GetValue(x.Address)) <= SignExtendAsNeeded(x.Previous));

				case ComparisonOperator.DifferentBy:
					if (_differentBy.HasValue)
					{
						if (_settings.Type == DisplayType.Float)
						{
							return watchList.Where(x => (ToFloat(GetValue(x.Address)) + _differentBy.Value == ToFloat(x.Previous))
								|| (ToFloat(GetValue(x.Address)) - _differentBy.Value == ToFloat(x.Previous)));
						}

						return watchList.Where(x =>
						{
							long val = SignExtendAsNeeded(GetValue(x.Address));
							long prev = SignExtendAsNeeded(x.Previous);
							return (val + _differentBy.Value == prev) || (val - _differentBy.Value == prev);
						}
						);
					}
					else
					{
						throw new InvalidOperationException();
					}
			}
		}

		private IEnumerable<IMiniWatch> CompareSpecificValue(IEnumerable<IMiniWatch> watchList)
		{
			if (_compareValue.HasValue)
			{
				switch (_operator)
				{
					default:
					case ComparisonOperator.Equal:
						if (_settings.Type == DisplayType.Float)
						{
							return watchList.Where(x => ToFloat(GetValue(x.Address)) == ToFloat(_compareValue.Value));
						}

						return watchList.Where(x => GetValue(x.Address) == _compareValue.Value);
					case ComparisonOperator.NotEqual:
						if (_settings.Type == DisplayType.Float)
						{
							return watchList.Where(x => ToFloat(GetValue(x.Address)) != ToFloat(_compareValue.Value));
						}

						return watchList.Where(x => GetValue(x.Address) != _compareValue.Value);

					case ComparisonOperator.GreaterThan:
						if (_settings.Type == DisplayType.Float)
						{
							return watchList.Where(x => ToFloat(GetValue(x.Address)) > ToFloat(_compareValue.Value));
						}

						return watchList.Where(x => SignExtendAsNeeded(GetValue(x.Address)) > _compareValue.Value);
					case ComparisonOperator.GreaterThanEqual:
						if (_settings.Type == DisplayType.Float)
						{
							return watchList.Where(x => ToFloat(GetValue(x.Address)) >= ToFloat(_compareValue.Value));
						}

						return watchList.Where(x => SignExtendAsNeeded(GetValue(x.Address)) >= _compareValue.Value);
					case ComparisonOperator.LessThan:
						if (_settings.Type == DisplayType.Float)
						{
							return watchList.Where(x => ToFloat(GetValue(x.Address)) < ToFloat(_compareValue.Value));
						}

						return watchList.Where(x => SignExtendAsNeeded(GetValue(x.Address)) < _compareValue.Value);
					case ComparisonOperator.LessThanEqual:
						if (_settings.Type == DisplayType.Float)
						{
							return watchList.Where(x => ToFloat(GetValue(x.Address)) <= ToFloat(_compareValue.Value));
						}

						return watchList.Where(x => SignExtendAsNeeded(GetValue(x.Address)) <= _compareValue.Value);
					case ComparisonOperator.DifferentBy:
						if (_differentBy.HasValue)
						{
							if (_settings.Type == DisplayType.Float)
							{
								return watchList.Where(x => (ToFloat(GetValue(x.Address)) + _differentBy.Value == _compareValue.Value) ||
									(ToFloat(GetValue(x.Address)) - _differentBy.Value == _compareValue.Value));
							}

							return watchList.Where(x
								=> (SignExtendAsNeeded(GetValue(x.Address)) + _differentBy.Value == _compareValue.Value)
								|| (SignExtendAsNeeded(GetValue(x.Address)) - _differentBy.Value == _compareValue.Value));
						}
						else
						{
							throw new InvalidOperationException();
						}
				}
			}
			else
			{
				throw new InvalidOperationException();
			}
		}

		private IEnumerable<IMiniWatch> CompareSpecificAddress(IEnumerable<IMiniWatch> watchList)
		{
			if (_compareValue.HasValue)
			{
				switch (_operator)
				{
					default:
					case ComparisonOperator.Equal:
						return watchList.Where(x => x.Address == _compareValue.Value);
					case ComparisonOperator.NotEqual:
						return watchList.Where(x => x.Address != _compareValue.Value);
					case ComparisonOperator.GreaterThan:
						return watchList.Where(x => x.Address > _compareValue.Value);
					case ComparisonOperator.GreaterThanEqual:
						return watchList.Where(x => x.Address >= _compareValue.Value);
					case ComparisonOperator.LessThan:
						return watchList.Where(x => x.Address < _compareValue.Value);
					case ComparisonOperator.LessThanEqual:
						return watchList.Where(x => x.Address <= _compareValue.Value);
					case ComparisonOperator.DifferentBy:
						if (_differentBy.HasValue)
						{
							return watchList.Where(x => (x.Address + _differentBy.Value == _compareValue.Value) || (x.Address - _differentBy.Value == _compareValue.Value));
						}
						else
						{
							throw new InvalidOperationException();
						}
				}
			}
			else
			{
				throw new InvalidOperationException();
			}
		}

		private IEnumerable<IMiniWatch> CompareChanges(IEnumerable<IMiniWatch> watchList)
		{
			if (_settings.Mode == Settings.SearchMode.Detailed && _compareValue.HasValue)
			{
				switch (_operator)
				{
					default:
					case ComparisonOperator.Equal:
						return watchList
							.Cast<IMiniWatchDetails>()
							.Where(x => x.ChangeCount == _compareValue.Value)
							.Cast<IMiniWatch>();
					case ComparisonOperator.NotEqual:
						return watchList
							.Cast<IMiniWatchDetails>()
							.Where(x => x.ChangeCount != _compareValue.Value)
							.Cast<IMiniWatch>();
					case ComparisonOperator.GreaterThan:
						return watchList
							.Cast<IMiniWatchDetails>()
							.Where(x => x.ChangeCount > _compareValue.Value)
							.Cast<IMiniWatch>();
					case ComparisonOperator.GreaterThanEqual:
						return watchList
							.Cast<IMiniWatchDetails>()
							.Where(x => x.ChangeCount >= _compareValue.Value)
							.Cast<IMiniWatch>();
					case ComparisonOperator.LessThan:
						return watchList
							.Cast<IMiniWatchDetails>()
							.Where(x => x.ChangeCount < _compareValue.Value)
							.Cast<IMiniWatch>();
					case ComparisonOperator.LessThanEqual:
						return watchList
							.Cast<IMiniWatchDetails>()
							.Where(x => x.ChangeCount <= _compareValue.Value)
							.Cast<IMiniWatch>();
					case ComparisonOperator.DifferentBy:
						if (_differentBy.HasValue)
						{
							return watchList
								.Cast<IMiniWatchDetails>()
								.Where(x => (x.ChangeCount + _differentBy.Value == _compareValue.Value) || (x.ChangeCount - _differentBy.Value == _compareValue.Value))
								.Cast<IMiniWatch>();
						}
						else
						{
							throw new InvalidOperationException();
						}
				}
			}
			else
			{
				throw new InvalidCastException();
			}
		}

		private IEnumerable<IMiniWatch> CompareDifference(IEnumerable<IMiniWatch> watchList)
		{
			if (_compareValue.HasValue)
			{
				switch (_operator)
				{
					default:
					case ComparisonOperator.Equal:
						if (_settings.Type == DisplayType.Float)
						{
							return watchList.Where(x => (ToFloat(GetValue(x.Address)) - ToFloat(x.Previous)) == _compareValue.Value);
						}

						return watchList.Where(x => (SignExtendAsNeeded(GetValue(x.Address)) - SignExtendAsNeeded(x.Previous)) == _compareValue.Value);
					case ComparisonOperator.NotEqual:
						if (_settings.Type == DisplayType.Float)
						{
							return watchList.Where(x => (ToFloat(GetValue(x.Address)) - x.Previous) != _compareValue.Value);
						}

						return watchList.Where(x => (SignExtendAsNeeded(GetValue(x.Address)) - SignExtendAsNeeded(x.Previous)) != _compareValue.Value);
					case ComparisonOperator.GreaterThan:
						if (_settings.Type == DisplayType.Float)
						{
							return watchList.Where(x => (ToFloat(GetValue(x.Address)) - x.Previous) > _compareValue.Value);
						}

						return watchList.Where(x => (SignExtendAsNeeded(GetValue(x.Address)) - SignExtendAsNeeded(x.Previous)) > _compareValue.Value);
					case ComparisonOperator.GreaterThanEqual:
						if (_settings.Type == DisplayType.Float)
						{
							return watchList.Where(x => (ToFloat(GetValue(x.Address)) - x.Previous) >= _compareValue.Value);
						}

						return watchList.Where(x => (SignExtendAsNeeded(GetValue(x.Address)) - SignExtendAsNeeded(x.Previous)) >= _compareValue.Value);
					case ComparisonOperator.LessThan:
						if (_settings.Type == DisplayType.Float)
						{
							return watchList.Where(x => (ToFloat(GetValue(x.Address)) - x.Previous) < _compareValue.Value);
						}

						return watchList.Where(x => (SignExtendAsNeeded(GetValue(x.Address)) - SignExtendAsNeeded(x.Previous)) < _compareValue.Value);
					case ComparisonOperator.LessThanEqual:
						if (_settings.Type == DisplayType.Float)
						{
							return watchList.Where(x => (ToFloat(GetValue(x.Address)) - x.Previous) <= _compareValue.Value);
						}

						return watchList.Where(x => (SignExtendAsNeeded(GetValue(x.Address)) - SignExtendAsNeeded(x.Previous)) <= _compareValue.Value);
					case ComparisonOperator.DifferentBy:
						if (_differentBy.HasValue)
						{
							if (_settings.Type == DisplayType.Float)
							{
								return watchList.Where(x => (ToFloat(GetValue(x.Address)) - x.Previous + _differentBy.Value == _compareValue) ||
									(ToFloat(GetValue(x.Address)) - x.Previous - _differentBy.Value == x.Previous));
							}

							return watchList.Where(x
								=> (SignExtendAsNeeded(GetValue(x.Address)) - SignExtendAsNeeded(x.Previous) + _differentBy.Value == _compareValue)
								|| (SignExtendAsNeeded(GetValue(x.Address)) - SignExtendAsNeeded(x.Previous) - _differentBy.Value == _compareValue));
						}
						else
						{
							throw new InvalidOperationException();
						}
				}
			}
			else
			{
				throw new InvalidCastException();
			}
		}

		#endregion

		#region Private parts

		private float ToFloat(long val)
		{
			var bytes = BitConverter.GetBytes((int)val);
			return BitConverter.ToSingle(bytes, 0);
		}

		private long SignExtendAsNeeded(long val)
		{
			if (_settings.Type != DisplayType.Signed) return val;
			switch (_settings.Size)
			{
				default:
				case WatchSize.Byte:
					return (sbyte)val;
				case WatchSize.Word:
					return (short)val;
				case WatchSize.DWord:
					return (int)val;
			}
		}

		private long GetValue(long addr)
		{
			//do not return sign extended variables from here.
			switch (_settings.Size)
			{
				default:
				case WatchSize.Byte:
					var theByte = _settings.Domain.PeekByte(addr % Domain.Size);
					return theByte;

				case WatchSize.Word:
					var theWord = _settings.Domain.PeekUshort(addr % Domain.Size, _settings.BigEndian);
					return theWord;

				case WatchSize.DWord:
					var theDWord = _settings.Domain.PeekUint(addr % Domain.Size, _settings.BigEndian);
					return theDWord;
			}
		}

		private bool CanDoCompareType(Compare compareType)
		{
			switch (_settings.Mode)
			{
				default:
				case Settings.SearchMode.Detailed:
					return true;
				case Settings.SearchMode.Fast:
					return compareType != Compare.Changes;
			}
		}

		#endregion

		#region Classes

		public interface IMiniWatch
		{
			long Address { get; }
			long Previous { get; } //do not store sign extended variables in here.
			void SetPreviousToCurrent(MemoryDomain domain, bool bigendian);
		}

		private interface IMiniWatchDetails
		{
			int ChangeCount { get; }

			void ClearChangeCount();
			void Update(PreviousType type, MemoryDomain domain, bool bigendian);
		}

		private sealed class MiniByteWatch : IMiniWatch
		{
			public long Address { get; private set; }
			private byte _previous;

			public MiniByteWatch(MemoryDomain domain, long addr)
			{
				Address = addr;
				_previous = domain.PeekByte(Address % domain.Size);
			}

			public long Previous
			{
				get { return _previous; }
			}

			public void SetPreviousToCurrent(MemoryDomain domain, bool bigendian)
			{
				_previous = domain.PeekByte(Address % domain.Size);
			}
		}

		private sealed class MiniWordWatch : IMiniWatch
		{
			public long Address { get; private set; }
			private ushort _previous;

			public MiniWordWatch(MemoryDomain domain, long addr, bool bigEndian)
			{
				Address = addr;
				_previous = domain.PeekUshort(Address % domain.Size, bigEndian);
			}

			public long Previous
			{
				get { return _previous; }
			}

			public void SetPreviousToCurrent(MemoryDomain domain, bool bigendian)
			{
				_previous = domain.PeekUshort(Address, bigendian);
			}
		}

		public sealed class MiniDWordWatch : IMiniWatch
		{
			public long Address { get; private set; }
			private uint _previous;

			public MiniDWordWatch(MemoryDomain domain, long addr, bool bigEndian)
			{
				Address = addr;
				_previous = domain.PeekUint(Address % domain.Size, bigEndian);
			}

			public long Previous
			{
				get { return _previous; }
			}

			public void SetPreviousToCurrent(MemoryDomain domain, bool bigendian)
			{
				_previous = domain.PeekUint(Address, bigendian);
			}
		}

		private sealed class MiniByteWatchDetailed : IMiniWatch, IMiniWatchDetails
		{
			public long Address { get; private set; }

			private byte _previous;
			private byte _prevFrame;
			private int _changecount;

			public MiniByteWatchDetailed(MemoryDomain domain, long addr)
			{
				Address = addr;
				SetPreviousToCurrent(domain, false);
			}

			public void SetPreviousToCurrent(MemoryDomain domain, bool bigendian)
			{
				_previous = _prevFrame = domain.PeekByte(Address % domain.Size);
			}

			public long Previous
			{
				get { return _previous; }
			}

			public int ChangeCount
			{
				get { return _changecount; }
			}

			public void Update(PreviousType type, MemoryDomain domain, bool bigendian)
			{
				var value = domain.PeekByte(Address % domain.Size);

				if (value != _prevFrame)
				{
					_changecount++;
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
							_previous = _prevFrame;
						break;
				}

				_prevFrame = value;
			}

			public void ClearChangeCount()
			{
				_changecount = 0;
			}
		}

		private sealed class MiniWordWatchDetailed : IMiniWatch, IMiniWatchDetails
		{
			public long Address { get; private set; }

			private ushort _previous;
			private ushort _prevFrame;
			private int _changecount;

			public MiniWordWatchDetailed(MemoryDomain domain, long addr, bool bigEndian)
			{
				Address = addr;
				SetPreviousToCurrent(domain, bigEndian);
			}

			public void SetPreviousToCurrent(MemoryDomain domain, bool bigendian)
			{
				_previous = _prevFrame = domain.PeekUshort(Address % domain.Size, bigendian);
			}

			public long Previous
			{
				get { return _previous; }
			}

			public int ChangeCount
			{
				get { return _changecount; }
			}

			public void Update(PreviousType type, MemoryDomain domain, bool bigendian)
			{
				var value = domain.PeekUshort(Address % domain.Size, bigendian);
				if (value != Previous)
				{
					_changecount++;
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
							_previous = _prevFrame;
						break;
				}

				_prevFrame = value;
			}

			public void ClearChangeCount()
			{
				_changecount = 0;
			}
		}

		public sealed class MiniDWordWatchDetailed : IMiniWatch, IMiniWatchDetails
		{
			public long Address { get; private set; }

			private uint _previous;
			private uint _prevFrame;
			private int _changecount;

			public MiniDWordWatchDetailed(MemoryDomain domain, long addr, bool bigEndian)
			{
				Address = addr;
				SetPreviousToCurrent(domain, bigEndian);
			}

			public void SetPreviousToCurrent(MemoryDomain domain, bool bigendian)
			{
				_previous = _prevFrame = domain.PeekUint(Address % domain.Size, bigendian);
			}

			public long Previous
			{
				get { return (int)_previous; }
			}

			public int ChangeCount
			{
				get { return _changecount; }
			}

			public void Update(PreviousType type, MemoryDomain domain, bool bigendian)
			{
				var value = domain.PeekUint(Address % domain.Size, bigendian);
				if (value != Previous)
				{
					_changecount++;
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
							_previous = _prevFrame;
						break;
				}

				_prevFrame = value;
			}

			public void ClearChangeCount()
			{
				_changecount = 0;
			}
		}

		public class Settings
		{
			public Settings(IMemoryDomains memoryDomains)
			{
				BigEndian = memoryDomains.MainMemory.EndianType == MemoryDomain.Endian.Big;
				Size = (WatchSize)memoryDomains.MainMemory.WordSize;
				Type = DisplayType.Unsigned;
				Mode = memoryDomains.MainMemory.Size > (1024 * 1024) ?
					SearchMode.Fast :
					SearchMode.Detailed;

				Domain = memoryDomains.MainMemory;
				CheckMisAligned = false;
				PreviousType = PreviousType.LastSearch;
			}

			/*Require restart*/
			public enum SearchMode { Fast, Detailed }

			public SearchMode Mode { get; set; }
			public MemoryDomain Domain { get; set; }
			public WatchSize Size { get; set; }
			public bool CheckMisAligned { get; set; }

			/*Can be changed mid-search*/
			public DisplayType Type { get; set; }
			public bool BigEndian { get; set; }
			public PreviousType PreviousType { get; set; }
		}

		#endregion
	}
}
