using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;

// ReSharper disable PossibleInvalidCastExceptionInForeachLoop
namespace BizHawk.Client.Common.RamSearchEngine
{
	public class RamSearchEngine
	{
		private Compare _compareTo = Compare.Previous;

		private List<IMiniWatch> _watchList = new List<IMiniWatch>();
		private readonly SearchEngineSettings _settings;
		private readonly UndoHistory<IMiniWatch> _history = new UndoHistory<IMiniWatch>(true);
		private bool _isSorted = true; // Tracks whether or not the list is sorted by address, if it is, binary search can be used for finding watches

		public RamSearchEngine(SearchEngineSettings settings, IMemoryDomains memoryDomains)
		{
			_settings = new SearchEngineSettings(memoryDomains)
			{
				Mode = settings.Mode,
				Domain = settings.Domain,
				Size = settings.Size,
				CheckMisAligned = settings.CheckMisAligned,
				Type = settings.Type,
				BigEndian = settings.BigEndian,
				PreviousType = settings.PreviousType
			};
		}

		public RamSearchEngine(SearchEngineSettings settings, IMemoryDomains memoryDomains, Compare compareTo, long? compareValue, int? differentBy)
			: this(settings, memoryDomains)
		{
			_compareTo = compareTo;
			DifferentBy = differentBy;
			CompareValue = compareValue;
		}

		#region API

		public IEnumerable<long> OutOfRangeAddress => _watchList
			.Where(watch => watch.Address >= Domain.Size)
			.Select(watch => watch.Address);

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
					if (_settings.IsDetailed())
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
					if (_settings.IsDetailed())
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
					if (_settings.IsDetailed())
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
				if (_settings.IsDetailed())
				{
					return Watch.GenerateWatch(
						_settings.Domain,
						_watchList[index].Address,
						_settings.Size,
						_settings.Type,
						_settings.BigEndian,
						"",
						0,
						_watchList[index].Previous,
						((IMiniWatchDetails)_watchList[index]).ChangeCount);
				}

				return Watch.GenerateWatch(
						_settings.Domain,
						_watchList[index].Address,
						_settings.Size,
						_settings.Type,
						_settings.BigEndian,
						"",
						0,
						_watchList[index].Previous);
			}
		}

		public int DoSearch()
		{
			int before = _watchList.Count;

			_watchList = _compareTo switch
			{
				Compare.Previous => ComparePrevious(_watchList).ToList(),
				Compare.SpecificValue => CompareSpecificValue(_watchList).ToList(),
				Compare.SpecificAddress => CompareSpecificAddress(_watchList).ToList(),
				Compare.Changes => CompareChanges(_watchList).ToList(),
				Compare.Difference => CompareDifference(_watchList).ToList(),
				_ => ComparePrevious(_watchList).ToList()
			};

			if (_settings.PreviousType == PreviousType.LastSearch)
			{
				SetPreviousToCurrent();
			}

			if (UndoEnabled)
			{
				_history.AddState(_watchList);
			}

			return before - _watchList.Count;
		}

		public bool Preview(long address)
		{
			var listOfOne = Enumerable.Repeat(_isSorted
				? _watchList.BinarySearch(w => w.Address, address)
				: _watchList.FirstOrDefault(w => w.Address == address), 1);

			return _compareTo switch
			{
				Compare.Previous => !ComparePrevious(listOfOne).Any(),
				Compare.SpecificValue => !CompareSpecificValue(listOfOne).Any(),
				Compare.SpecificAddress => !CompareSpecificAddress(listOfOne).Any(),
				Compare.Changes => !CompareChanges(listOfOne).Any(),
				Compare.Difference => !CompareDifference(listOfOne).Any(),
				_ => !ComparePrevious(listOfOne).Any()
			};
		}

		public int Count => _watchList.Count;

		public SearchMode Mode => _settings.Mode;

		public MemoryDomain Domain => _settings.Domain;

		/// <exception cref="InvalidOperationException">(from setter) <see cref="Mode"/> is <see cref="Settings.SearchMode.Fast"/> and <paramref name="value"/> is not <see cref="Compare.Changes"/></exception>
		public Compare CompareTo
		{
			get => _compareTo;

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

		public long? CompareValue { get; set; }

		public ComparisonOperator Operator { get; set; }

		// zero 07-sep-2014 - this isn't ideal. but don't bother changing it (to a long, for instance) until it can support floats. maybe store it as a double here.
		public int? DifferentBy { get; set; }

		public void Update()
		{
			if (_settings.IsDetailed())
			{
				foreach (IMiniWatchDetails watch in _watchList)
				{
					watch.Update(_settings.PreviousType, _settings.Domain, _settings.BigEndian);
				}
			}
		}

		public void SetType(DisplayType type) => _settings.Type = type;

		public void SetEndian(bool bigEndian) => _settings.BigEndian = bigEndian;

		/// <exception cref="InvalidOperationException"><see cref="Mode"/> is <see cref="Settings.SearchMode.Fast"/> and <paramref name="type"/> is <see cref="PreviousType.LastFrame"/></exception>
		public void SetPreviousType(PreviousType type)
		{
			if (_settings.IsFastMode() && type == PreviousType.LastFrame)
			{
				throw new InvalidOperationException();
			}

			_settings.PreviousType = type;
		}

		public void SetPreviousToCurrent()
		{
			_watchList.ForEach(w => w.SetPreviousToCurrent(_settings.Domain, _settings.BigEndian));
		}

		public void ClearChangeCounts()
		{
			if (_settings.IsDetailed())
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
			if (UndoEnabled)
			{
				_history.AddState(_watchList);
			}

			var addresses = watches.Select(w => w.Address);
			_watchList.RemoveAll(w => addresses.Contains(w.Address));
		}

		public void RemoveRange(IEnumerable<int> indices)
		{
			if (UndoEnabled)
			{
				_history.AddState(_watchList);
			}

			var removeList = indices.Select(i => _watchList[i]); // This will fail after int.MaxValue but RAM Search fails on domains that large anyway
			_watchList = _watchList.Except(removeList).ToList();
		}

		public void AddRange(IEnumerable<long> addresses, bool append)
		{
			if (!append)
			{
				_watchList.Clear();
			}

			_watchList = _settings.Size switch
			{
				WatchSize.Byte => addresses.ToBytes(_settings).ToList(),
				WatchSize.Word => addresses.ToWords(_settings).ToList(),
				WatchSize.DWord => addresses.ToDWords(_settings).ToList(),
				_ => addresses.ToBytes(_settings).ToList()
			};
		}

		public void Sort(string column, bool reverse)
		{
			_isSorted = false;
			switch (column)
			{
				case WatchList.Address:
					if (!reverse)
					{
						_isSorted = true;
					}

					_watchList = _watchList.OrderBy(w => w.Address, reverse).ToList();
					break;
				case WatchList.Value:
					_watchList = _watchList.OrderBy(w => GetValue(w.Address), reverse).ToList();
					break;
				case WatchList.Prev:
					_watchList = _watchList.OrderBy(w => w.Previous, reverse).ToList();
					break;
				case WatchList.ChangesCol:
					if (_settings.IsDetailed())
					{
						_watchList = _watchList
							.Cast<IMiniWatchDetails>()
							.OrderBy(w => w.ChangeCount, reverse)
							.Cast<IMiniWatch>().ToList();
					}

					break;
				case WatchList.Diff:
					_watchList = _watchList.OrderBy(w => GetValue(w.Address) - w.Previous, reverse).ToList();
					break;
			}
		}

		#endregion

		#region Undo API

		public bool UndoEnabled { get; set; }
		

		public bool CanUndo => UndoEnabled && _history.CanUndo;

		public bool CanRedo => UndoEnabled && _history.CanRedo;

		public void ClearHistory() => _history.Clear();

		public int Undo()
		{
			int origCount = _watchList.Count;
			if (UndoEnabled)
			{
				_watchList = _history.Undo().ToList();
				return _watchList.Count - origCount;
			}

			return _watchList.Count;
		}

		public int Redo()
		{
			int origCount = _watchList.Count;
			if (UndoEnabled)
			{
				_watchList = _history.Redo().ToList();
				return origCount - _watchList.Count;
			}

			return _watchList.Count;
		}

		#endregion

		#region Comparisons

		private IEnumerable<IMiniWatch> ComparePrevious(IEnumerable<IMiniWatch> watchList)
		{
			switch (Operator)
			{
				default:
				case ComparisonOperator.Equal:
					return _settings.Type == DisplayType.Float
						? watchList.Where(w => GetValue(w.Address).ToFloat() == w.Previous.ToFloat())
						: watchList.Where(w => SignExtendAsNeeded(GetValue(w.Address)) == SignExtendAsNeeded(w.Previous));
				case ComparisonOperator.NotEqual:
					return watchList.Where(w => SignExtendAsNeeded(GetValue(w.Address)) != SignExtendAsNeeded(w.Previous));
				case ComparisonOperator.GreaterThan:
					return _settings.Type == DisplayType.Float
						? watchList.Where(w => GetValue(w.Address).ToFloat() > w.Previous.ToFloat())
						: watchList.Where(w => SignExtendAsNeeded(GetValue(w.Address)) > SignExtendAsNeeded(w.Previous));
				case ComparisonOperator.GreaterThanEqual:
					return _settings.Type == DisplayType.Float
						? watchList.Where(w => GetValue(w.Address).ToFloat() >= w.Previous.ToFloat())
						: watchList.Where(w => SignExtendAsNeeded(GetValue(w.Address)) >= SignExtendAsNeeded(w.Previous));

				case ComparisonOperator.LessThan:
					return _settings.Type == DisplayType.Float
						? watchList.Where(w => GetValue(w.Address).ToFloat() < w.Previous.ToFloat())
						: watchList.Where(w => SignExtendAsNeeded(GetValue(w.Address)) < SignExtendAsNeeded(w.Previous));
				case ComparisonOperator.LessThanEqual:
					return _settings.Type == DisplayType.Float
						? watchList.Where(w => GetValue(w.Address).ToFloat() <= w.Previous.ToFloat())
						: watchList.Where(w => SignExtendAsNeeded(GetValue(w.Address)) <= SignExtendAsNeeded(w.Previous));
				case ComparisonOperator.DifferentBy:
					if (DifferentBy.HasValue)
					{
						var differentBy = DifferentBy.Value;
						if (_settings.Type == DisplayType.Float)
						{
							return watchList.Where(w => GetValue(w.Address).ToFloat() + differentBy == w.Previous.ToFloat()
								|| GetValue(w.Address).ToFloat() - differentBy == w.Previous.ToFloat());
						}

						return watchList.Where(w =>
						{
							long val = SignExtendAsNeeded(GetValue(w.Address));
							long prev = SignExtendAsNeeded(w.Previous);
							return val + differentBy == prev
								|| val - differentBy == prev;
						});
					}
					else
					{
						throw new InvalidOperationException();
					}
			}
		}

		private IEnumerable<IMiniWatch> CompareSpecificValue(IEnumerable<IMiniWatch> watchList)
		{
			if (CompareValue.HasValue)
			{
				var compareValue = CompareValue.Value;
				switch (Operator)
				{
					default:
					case ComparisonOperator.Equal:
						return _settings.Type == DisplayType.Float
							? watchList.Where(w => GetValue(w.Address).ToFloat() == compareValue.ToFloat())
							: watchList.Where(w => SignExtendAsNeeded(GetValue(w.Address)) == SignExtendAsNeeded(CompareValue.Value));
					case ComparisonOperator.NotEqual:
						return _settings.Type == DisplayType.Float
							? watchList.Where(w => GetValue(w.Address).ToFloat() != compareValue.ToFloat())
							: watchList.Where(w => SignExtendAsNeeded(GetValue(w.Address)) != SignExtendAsNeeded(compareValue));
					case ComparisonOperator.GreaterThan:
						return _settings.Type == DisplayType.Float
							? watchList.Where(w => GetValue(w.Address).ToFloat() > compareValue.ToFloat())
							: watchList.Where(w => SignExtendAsNeeded(GetValue(w.Address)) > SignExtendAsNeeded(compareValue));
					case ComparisonOperator.GreaterThanEqual:
						return _settings.Type == DisplayType.Float
							? watchList.Where(w => GetValue(w.Address).ToFloat() >= compareValue.ToFloat())
							: watchList.Where(w => SignExtendAsNeeded(GetValue(w.Address)) >= SignExtendAsNeeded(compareValue));
					case ComparisonOperator.LessThan:
						return _settings.Type == DisplayType.Float
							? watchList.Where(w => GetValue(w.Address).ToFloat() < compareValue.ToFloat())
							: watchList.Where(w => SignExtendAsNeeded(GetValue(w.Address)) < SignExtendAsNeeded(compareValue));

					case ComparisonOperator.LessThanEqual:
						return _settings.Type == DisplayType.Float
							? watchList.Where(w => GetValue(w.Address).ToFloat() <= compareValue.ToFloat())
							: watchList.Where(w => SignExtendAsNeeded(GetValue(w.Address)) <= SignExtendAsNeeded(compareValue));

					case ComparisonOperator.DifferentBy:
						if (DifferentBy.HasValue)
						{
							var differentBy = DifferentBy.Value;
							if (_settings.Type == DisplayType.Float)
							{
								return watchList.Where(w => GetValue(w.Address).ToFloat() + differentBy == compareValue
									|| GetValue(w.Address).ToFloat() - differentBy == compareValue);
							}

							return watchList.Where(w
								=> SignExtendAsNeeded(GetValue(w.Address)) + differentBy == compareValue
								|| SignExtendAsNeeded(GetValue(w.Address)) - differentBy == compareValue);
						}

						throw new InvalidOperationException();
				}
			}

			throw new InvalidOperationException();
		}

		private IEnumerable<IMiniWatch> CompareSpecificAddress(IEnumerable<IMiniWatch> watchList)
		{
			if (CompareValue.HasValue)
			{
				var compareValue = CompareValue.Value;
				switch (Operator)
				{
					default:
					case ComparisonOperator.Equal:
						return watchList.Where(w => w.Address == compareValue);
					case ComparisonOperator.NotEqual:
						return watchList.Where(w => w.Address != compareValue);
					case ComparisonOperator.GreaterThan:
						return watchList.Where(w => w.Address > compareValue);
					case ComparisonOperator.GreaterThanEqual:
						return watchList.Where(w => w.Address >= compareValue);
					case ComparisonOperator.LessThan:
						return watchList.Where(w => w.Address < compareValue);
					case ComparisonOperator.LessThanEqual:
						return watchList.Where(w => w.Address <= compareValue);
					case ComparisonOperator.DifferentBy:
						if (DifferentBy.HasValue)
						{
							return watchList.Where(w => w.Address + DifferentBy.Value == compareValue
								|| w.Address - DifferentBy.Value == compareValue);
						}

						throw new InvalidOperationException();
				}
			}

			throw new InvalidOperationException();
		}

		private IEnumerable<IMiniWatch> CompareChanges(IEnumerable<IMiniWatch> watchList)
		{
			if (_settings.IsDetailed() && CompareValue.HasValue)
			{
				var compareValue = CompareValue.Value;
				switch (Operator)
				{
					default:
					case ComparisonOperator.Equal:
						return watchList
							.Cast<IMiniWatchDetails>()
							.Where(w => w.ChangeCount == compareValue);
					case ComparisonOperator.NotEqual:
						return watchList
							.Cast<IMiniWatchDetails>()
							.Where(w => w.ChangeCount != compareValue);
					case ComparisonOperator.GreaterThan:
						return watchList
							.Cast<IMiniWatchDetails>()
							.Where(w => w.ChangeCount > compareValue);
					case ComparisonOperator.GreaterThanEqual:
						return watchList
							.Cast<IMiniWatchDetails>()
							.Where(w => w.ChangeCount >= compareValue);
					case ComparisonOperator.LessThan:
						return watchList
							.Cast<IMiniWatchDetails>()
							.Where(w => w.ChangeCount < compareValue);
					case ComparisonOperator.LessThanEqual:
						return watchList
							.Cast<IMiniWatchDetails>()
							.Where(w => w.ChangeCount <= compareValue);
					case ComparisonOperator.DifferentBy:
						if (DifferentBy.HasValue)
						{
							return watchList
								.Cast<IMiniWatchDetails>()
								.Where(w => w.ChangeCount + DifferentBy.Value == compareValue
									|| w.ChangeCount - DifferentBy.Value == compareValue);
						}

						throw new InvalidOperationException();
				}
			}

			throw new InvalidCastException();
		}

		private IEnumerable<IMiniWatch> CompareDifference(IEnumerable<IMiniWatch> watchList)
		{
			if (CompareValue.HasValue)
			{
				var compareValue = CompareValue.Value;
				switch (Operator)
				{
					default:
					case ComparisonOperator.Equal:
						return _settings.Type == DisplayType.Float
							? watchList.Where(w => GetValue(w.Address).ToFloat() - w.Previous.ToFloat() == compareValue)
							: watchList.Where(w => SignExtendAsNeeded(GetValue(w.Address)) - SignExtendAsNeeded(w.Previous) == compareValue);
					case ComparisonOperator.NotEqual:
						return _settings.Type == DisplayType.Float
							? watchList.Where(w => GetValue(w.Address).ToFloat() - w.Previous.ToFloat() != compareValue)
							: watchList.Where(w => SignExtendAsNeeded(GetValue(w.Address)) - SignExtendAsNeeded(w.Previous) != compareValue);
					case ComparisonOperator.GreaterThan:
						return _settings.Type == DisplayType.Float
							? watchList.Where(w => GetValue(w.Address).ToFloat() - w.Previous.ToFloat() > compareValue)
							: watchList.Where(w => SignExtendAsNeeded(GetValue(w.Address)) - SignExtendAsNeeded(w.Previous) > compareValue);
					case ComparisonOperator.GreaterThanEqual:
						return _settings.Type == DisplayType.Float
							? watchList.Where(w => GetValue(w.Address).ToFloat() - w.Previous.ToFloat() >= compareValue)
							: watchList.Where(w => SignExtendAsNeeded(GetValue(w.Address)) - SignExtendAsNeeded(w.Previous) >= compareValue);
					case ComparisonOperator.LessThan:
						return _settings.Type == DisplayType.Float
							? watchList.Where(w => GetValue(w.Address).ToFloat() - w.Previous.ToFloat() < compareValue)
							: watchList.Where(w => SignExtendAsNeeded(GetValue(w.Address)) - SignExtendAsNeeded(w.Previous) < compareValue);
					case ComparisonOperator.LessThanEqual:
						return _settings.Type == DisplayType.Float
							? watchList.Where(w => GetValue(w.Address).ToFloat() - w.Previous.ToFloat() <= compareValue)
							: watchList.Where(w => SignExtendAsNeeded(GetValue(w.Address)) - SignExtendAsNeeded(w.Previous) <= compareValue);
					case ComparisonOperator.DifferentBy:
						if (DifferentBy.HasValue)
						{
							var differentBy = DifferentBy.Value;
							if (_settings.Type == DisplayType.Float)
							{
								return watchList.Where(w => GetValue(w.Address).ToFloat() - w.Previous.ToFloat() + differentBy == compareValue
									|| GetValue(w.Address).ToFloat() - w.Previous.ToFloat() - differentBy == w.Previous);
							}

							return watchList.Where(w
								=> SignExtendAsNeeded(GetValue(w.Address)) - SignExtendAsNeeded(w.Previous) + differentBy == compareValue
								|| SignExtendAsNeeded(GetValue(w.Address)) - SignExtendAsNeeded(w.Previous) - differentBy == compareValue);
						}

						throw new InvalidOperationException();
				}
			}

			throw new InvalidCastException();
		}

		#endregion

		#region Private parts

		private long SignExtendAsNeeded(long val)
		{
			if (_settings.Type != DisplayType.Signed)
			{
				return val;
			}

			return _settings.Size switch
			{
				WatchSize.Byte => (sbyte) val,
				WatchSize.Word => (short) val,
				WatchSize.DWord => (int) val,
				_ => (sbyte) val
			};
		}

		private long GetValue(long addr)
		{
			// do not return sign extended variables from here.
			return _settings.Size switch
			{
				WatchSize.Byte => _settings.Domain.PeekByte(addr % Domain.Size),
				WatchSize.Word => _settings.Domain.PeekUshort(addr % Domain.Size, _settings.BigEndian),
				WatchSize.DWord => _settings.Domain.PeekUint(addr % Domain.Size, _settings.BigEndian),
				_ => _settings.Domain.PeekByte(addr % Domain.Size)
			};
		}

		private bool CanDoCompareType(Compare compareType)
		{
			return _settings.Mode switch
			{
				SearchMode.Detailed => true,
				SearchMode.Fast => (compareType != Compare.Changes),
				_ => true
			};
		}

		#endregion
	}
}
