using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;
using static BizHawk.Common.NumberExtensions.NumberExtensions;

// ReSharper disable PossibleInvalidCastExceptionInForeachLoop
namespace BizHawk.Client.Common.RamSearchEngine
{
	public class RamSearchEngine
	{
		private static bool DifferByExactly(ulong difference, long a, long b)
			=> unchecked((ulong) Math.Abs(a - b) == difference);

		private static bool DifferByExactly(ulong difference, double a, double b)
			=> Math.Abs(a - b).HawkFloatEquality(ReinterpretAsF64(difference));

		private Compare _compareTo = Compare.Previous;

		private IMiniWatch[] _watchList = Array.Empty<IMiniWatch>();
		private readonly SearchEngineSettings _settings;
		private readonly UndoHistory<IEnumerable<IMiniWatch>> _history = new UndoHistory<IEnumerable<IMiniWatch>>(true, new List<IMiniWatch>()); //TODO use IList instead of IEnumerable and stop calling `.ToArray()` (i.e. cloning) on reads and writes?

		public RamSearchEngine(SearchEngineSettings settings, IMemoryDomains memoryDomains)
		{
			_settings = new SearchEngineSettings(memoryDomains, settings.UseUndoHistory)
			{
				Mode = settings.Mode,
				Domain = settings.Domain,
				Size = settings.Size,
				CheckMisAligned = settings.CheckMisAligned,
				Type = settings.Type,
				BigEndian = settings.BigEndian,
				PreviousType = settings.PreviousType,
			};
		}

		public RamSearchEngine(
			SearchEngineSettings settings,
			IMemoryDomains memoryDomains,
			Compare compareTo,
			ulong? compareValue,
			ulong? differentBy)
				: this(settings, memoryDomains)
		{
			_compareTo = compareTo;
			DifferentBy = differentBy;
			CompareValue = compareValue;
		}

		public IEnumerable<long> OutOfRangeAddress => _watchList
			.Where(watch => !watch.IsValid(Domain))
			.Select(watch => watch.Address);

		public void Start()
		{
			_history.Clear();
			var domain = _settings.Domain;
			int stepSize = _settings.CheckMisAligned ? 1 : (int)_settings.Size;
			long listSize = domain.Size / stepSize - (int)_settings.Size + stepSize;

			_watchList = new IMiniWatch[listSize];
			using var @lock = Domain.EnterExit();
			switch (_settings.Size)
			{
				default:
				case WatchSize.Byte:
					if (_settings.IsDetailed())
					{
						for (var i = 0; i < _watchList.Length; i++) _watchList[i] = new MiniByteWatchDetailed(domain, i);
					}
					else
					{
						for (var i = 0; i < _watchList.Length; i++) _watchList[i] = new MiniByteWatch(domain, i);
					}
					break;
				case WatchSize.Word:
					if (_settings.IsDetailed())
					{
						for (var i = 0; i < _watchList.Length; i++)
						{
							_watchList[i] = new MiniWordWatchDetailed(domain, i * stepSize, _settings.BigEndian);
						}
					}
					else
					{
						for (var i = 0; i < _watchList.Length; i++)
						{
							_watchList[i] = new MiniWordWatch(domain, i * stepSize, _settings.BigEndian);
						}
					}
					break;
				case WatchSize.DWord:
					if (_settings.IsDetailed())
					{
						for (var i = 0; i < _watchList.Length; i++)
						{
							_watchList[i] = new MiniDWordWatchDetailed(domain, i * stepSize, _settings.BigEndian);
						}
					}
					else
					{
						for (var i = 0; i < _watchList.Length; i++)
						{
							_watchList[i] = new MiniDWordWatch(domain, i * stepSize, _settings.BigEndian);
						}
					}
					break;
			}
		}

		/// <summary>
		/// Exposes the current watch state based on index
		/// </summary>
		public Watch this[int index] =>
			Watch.GenerateWatch(
				_settings.Domain,
				_watchList[index].Address,
				_settings.Size,
				_settings.Type,
				_settings.BigEndian,
				"",
				0,
				unchecked((long) _watchList[index].Previous),
				_settings.IsDetailed() ? ((IMiniWatchDetails)_watchList[index]).ChangeCount : 0);

		public int DoSearch()
		{
			int before = _watchList.Length;

			using (Domain.EnterExit())
			{
				_watchList = _compareTo switch
				{
					Compare.Previous => ComparePrevious(_watchList).ToArray(),
					Compare.SpecificValue => CompareSpecificValue(_watchList).ToArray(),
					Compare.SpecificAddress => CompareSpecificAddress(_watchList).ToArray(),
					Compare.Changes => CompareChanges(_watchList).ToArray(),
					Compare.Difference => CompareDifference(_watchList).ToArray(),
					_ => ComparePrevious(_watchList).ToArray(),
				};

				if (_settings.PreviousType == PreviousType.LastSearch)
				{
					SetPreviousToCurrent();
				}
			}

			if (UndoEnabled)
			{
				_history.AddState(_watchList.ToArray());
			}

			return before - _watchList.Length;
		}

		public bool Preview(int index)
		{
			var addressWatch = _watchList[index];
			IMiniWatch[] listOfOne = [ addressWatch ];

			return _compareTo switch
			{
				Compare.Previous => !ComparePrevious(listOfOne).Any(),
				Compare.SpecificValue => !CompareSpecificValue(listOfOne).Any(),
				Compare.SpecificAddress => !CompareSpecificAddress(listOfOne).Any(),
				Compare.Changes => !CompareChanges(listOfOne).Any(),
				Compare.Difference => !CompareDifference(listOfOne).Any(),
				_ => !ComparePrevious(listOfOne).Any(),
			};
		}

		public int Count => _watchList.Length;

		public SearchMode Mode => _settings.Mode;

		public MemoryDomain Domain => _settings.Domain;

		/// <exception cref="InvalidOperationException">(from setter) <see cref="Mode"/> is <see cref="SearchMode.Fast"/> and <paramref name="value"/> is not <see cref="Compare.Changes"/></exception>
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

		public ulong? CompareValue { get; set; }

		public ComparisonOperator Operator { get; set; }

		/// <remarks>
		/// zero 07-sep-2014 - this isn't ideal. but don't bother changing it (to a long, for instance) until it can support floats. maybe store it as a double here.<br/>
		/// it already supported floats by way of reinterpret-cast, it just wasn't implemented correctly on this side --yoshi
		/// </remarks>
		public ulong? DifferentBy { get; set; }

		public void Update()
		{
			if (!_settings.IsDetailed()) return;
			using var @lock = _settings.Domain.EnterExit();
			foreach (IMiniWatchDetails watch in _watchList)
			{
				watch.Update(_settings.PreviousType, _settings.Domain, _settings.BigEndian);
			}
		}

		public void SetType(WatchDisplayType type) => _settings.Type = type;

		public void SetEndian(bool bigEndian) => _settings.BigEndian = bigEndian;

		/// <exception cref="InvalidOperationException"><see cref="Mode"/> is <see cref="SearchMode.Fast"/> and <paramref name="type"/> is <see cref="PreviousType.LastFrame"/></exception>
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
			Array.ForEach(_watchList, w => w.SetPreviousToCurrent(_settings.Domain, _settings.BigEndian));
		}

		public void ClearChangeCounts()
		{
			if (!_settings.IsDetailed()) return;
			foreach (var watch in _watchList.Cast<IMiniWatchDetails>())
			{
				watch.ClearChangeCount();
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
				_history.AddState(_watchList.ToArray());
			}

			var addresses = watches.Select(w => w.Address);
			RemoveAddressRange(addresses);
		}

		public void RemoveRange(IEnumerable<int> indices)
		{
			if (UndoEnabled)
			{
				_history.AddState(_watchList.ToArray());
			}

			var removeList = indices.Select(i => _watchList[i]); // This will fail after int.MaxValue but RAM Search fails on domains that large anyway
			_watchList = _watchList.Except(removeList).ToArray();
		}

		public void RemoveAddressRange(IEnumerable<long> addresses)
		{
			_watchList = _watchList.Where(w => !addresses.Contains(w.Address)).ToArray();
		}

		public void AddRange(IEnumerable<long> addresses, bool append)
		{
			using var @lock = Domain.EnterExit();
			var list = _settings.Size switch
			{
				WatchSize.Byte => addresses.ToBytes(_settings),
				WatchSize.Word => addresses.ToWords(_settings),
				WatchSize.DWord => addresses.ToDWords(_settings),
				_ => addresses.ToBytes(_settings),
			};

			_watchList = (append ? _watchList.Concat(list) : list).ToArray();
		}

		public void ConvertTo(WatchSize size)
		{
			using var @lock = Domain.EnterExit();
			var maxAddress = Domain.Size - (int)size;
			var addresses = AllAddresses().Where(address => address <= maxAddress);
			_watchList = size switch
			{
				WatchSize.Byte => addresses.ToBytes(_settings).ToArray(),
				WatchSize.Word when _settings.CheckMisAligned => addresses.ToWords(_settings).ToArray(),
				WatchSize.Word => addresses.Where(static address => address % 2 == 0).ToWords(_settings).ToArray(),
				WatchSize.DWord when _settings.CheckMisAligned => addresses.ToDWords(_settings).ToArray(),
				WatchSize.DWord => addresses.Where(static address => address % 4 == 0).ToDWords(_settings).ToArray(),
				_ => _watchList,
			};

			_settings.Size = size;
		}

		private IEnumerable<long> AllAddresses()
		{
			foreach (var watch in _watchList)
			{
				var addr = watch.Address;
				yield return addr;
				if (_settings.CheckMisAligned) continue; // I guess we're assuming the other parts of the address are also in `_watchList`? --yoshi
				switch (_settings.Size)
				{
					case WatchSize.Word:
						yield return addr + 1;
						break;
					case WatchSize.DWord:
						yield return addr + 1;
						yield return addr + 2;
						yield return addr + 3;
						break;
				}
			}
		}

		public void Sort(string column, bool reverse)
		{
			switch (column)
			{
				case WatchList.Address:
					_watchList = _watchList.OrderBy(w => w.Address, reverse).ToArray();
					break;
				case WatchList.Value:
					_watchList = _watchList.OrderBy(w => GetValue(w), reverse).ToArray();
					break;
				case WatchList.Prev:
					_watchList = _watchList.OrderBy(w => w.Previous, reverse).ToArray();
					break;
				case WatchList.ChangesCol:
					if (!_settings.IsDetailed()) break;
					_watchList = _watchList
						.Cast<IMiniWatchDetails>()
						.OrderBy(w => w.ChangeCount, reverse)
						.Cast<IMiniWatch>()
						.ToArray();
					break;
				case WatchList.Diff:
					_watchList = _watchList.OrderBy(w => GetValue(w) - w.Previous, reverse).ToArray();
					break;
			}
		}

		public bool UndoEnabled
		{
			get => _settings.UseUndoHistory;
			set => _settings.UseUndoHistory = value;
		}

		public bool CanUndo => UndoEnabled && _history.CanUndo;

		public bool CanRedo => UndoEnabled && _history.CanRedo;

		public void ClearHistory() => _history.Clear();

		public int Undo()
		{
			int origCount = _watchList.Length;
			if (UndoEnabled)
			{
				_watchList = _history.Undo().ToArray();
				return _watchList.Length - origCount;
			}

			return _watchList.Length;
		}

		public int Redo()
		{
			int origCount = _watchList.Length;
			if (UndoEnabled)
			{
				_watchList = _history.Redo().ToArray();
				return origCount - _watchList.Length;
			}

			return _watchList.Length;
		}

		private IEnumerable<IMiniWatch> ComparePrevious(IEnumerable<IMiniWatch> watchList)
		{
			if (_settings.Type is not WatchDisplayType.Float)
			{
				switch (Operator)
				{
					default:
					case ComparisonOperator.Equal:
						return watchList.Where(w => SignExtendAsNeeded(GetValue(w)) == SignExtendAsNeeded(w.Previous));
					case ComparisonOperator.NotEqual:
						return watchList.Where(w => SignExtendAsNeeded(GetValue(w)) != SignExtendAsNeeded(w.Previous));
					case ComparisonOperator.GreaterThan:
						return watchList.Where(w => SignExtendAsNeeded(GetValue(w)) > SignExtendAsNeeded(w.Previous));
					case ComparisonOperator.GreaterThanEqual:
						return watchList.Where(w => SignExtendAsNeeded(GetValue(w)) >= SignExtendAsNeeded(w.Previous));
					case ComparisonOperator.LessThan:
						return watchList.Where(w => SignExtendAsNeeded(GetValue(w)) < SignExtendAsNeeded(w.Previous));
					case ComparisonOperator.LessThanEqual:
						return watchList.Where(w => SignExtendAsNeeded(GetValue(w)) <= SignExtendAsNeeded(w.Previous));
					case ComparisonOperator.DifferentBy:
						if (DifferentBy is not ulong differentBy) throw new InvalidOperationException();
						return watchList.Where(w => DifferByExactly(
							difference: differentBy,
							SignExtendAsNeeded(GetValue(w)),
							SignExtendAsNeeded(w.Previous)));
				}
			}
			switch (Operator)
			{
				default:
				case ComparisonOperator.Equal:
					return watchList.Where(w => ReinterpretAsF64(GetValue(w)).HawkFloatEquality(ReinterpretAsF64(w.Previous)));
				case ComparisonOperator.NotEqual:
					return watchList.Where(w => !ReinterpretAsF64(GetValue(w)).HawkFloatEquality(ReinterpretAsF64(w.Previous)));
				case ComparisonOperator.GreaterThan:
					return watchList.Where(w => ReinterpretAsF64(GetValue(w)) > ReinterpretAsF64(w.Previous));
				case ComparisonOperator.GreaterThanEqual:
					return watchList.Where(w =>
					{
						var val = ReinterpretAsF64(GetValue(w));
						var prev = ReinterpretAsF64(w.Previous);
						return val > prev || val.HawkFloatEquality(prev);
					});
				case ComparisonOperator.LessThan:
					return watchList.Where(w => ReinterpretAsF64(GetValue(w)) < ReinterpretAsF64(w.Previous));
				case ComparisonOperator.LessThanEqual:
					return watchList.Where(w =>
					{
						var val = ReinterpretAsF64(GetValue(w));
						var prev = ReinterpretAsF64(w.Previous);
						return val < prev || val.HawkFloatEquality(prev);
					});
				case ComparisonOperator.DifferentBy:
					if (DifferentBy is not ulong differentBy) throw new InvalidOperationException();
					return watchList.Where(w => DifferByExactly(
						difference: differentBy,
						ReinterpretAsF64(GetValue(w)),
						ReinterpretAsF64(w.Previous)));
			}
		}

		private IEnumerable<IMiniWatch> CompareSpecificValue(IEnumerable<IMiniWatch> watchList)
		{
			if (CompareValue is not ulong compareValue) throw new InvalidOperationException();
			if (_settings.Type is not WatchDisplayType.Float)
			{
				switch (Operator)
				{
					default:
					case ComparisonOperator.Equal:
						return watchList.Where(w => SignExtendAsNeeded(GetValue(w)) == SignExtendAsNeeded(compareValue));
					case ComparisonOperator.NotEqual:
						return watchList.Where(w => SignExtendAsNeeded(GetValue(w)) != SignExtendAsNeeded(compareValue));
					case ComparisonOperator.GreaterThan:
						return watchList.Where(w => SignExtendAsNeeded(GetValue(w)) > SignExtendAsNeeded(compareValue));
					case ComparisonOperator.GreaterThanEqual:
						return watchList.Where(w => SignExtendAsNeeded(GetValue(w)) >= SignExtendAsNeeded(compareValue));
					case ComparisonOperator.LessThan:
						return watchList.Where(w => SignExtendAsNeeded(GetValue(w)) < SignExtendAsNeeded(compareValue));
					case ComparisonOperator.LessThanEqual:
						return watchList.Where(w => SignExtendAsNeeded(GetValue(w)) <= SignExtendAsNeeded(compareValue));
					case ComparisonOperator.DifferentBy:
						if (DifferentBy is not ulong differentBy) throw new InvalidOperationException();
						return watchList.Where(w => DifferByExactly(
							difference: differentBy,
							SignExtendAsNeeded(GetValue(w)),
							SignExtendAsNeeded(compareValue)));
				}
			}
			var compareValueF = ReinterpretAsF64(compareValue);
			switch (Operator)
			{
				default:
				case ComparisonOperator.Equal:
					return watchList.Where(w => ReinterpretAsF64(GetValue(w)).HawkFloatEquality(compareValueF));
				case ComparisonOperator.NotEqual:
					return watchList.Where(w => !ReinterpretAsF64(GetValue(w)).HawkFloatEquality(compareValueF));
				case ComparisonOperator.GreaterThan:
					return watchList.Where(w => ReinterpretAsF64(GetValue(w)) > compareValueF);
				case ComparisonOperator.GreaterThanEqual:
					return watchList.Where(w =>
					{
						var val = ReinterpretAsF64(GetValue(w));
						return val > compareValueF || val.HawkFloatEquality(compareValueF);
					});
				case ComparisonOperator.LessThan:
					return watchList.Where(w => ReinterpretAsF64(GetValue(w)) < compareValueF);
				case ComparisonOperator.LessThanEqual:
					return watchList.Where(w =>
					{
						var val = ReinterpretAsF64(GetValue(w));
						return val < compareValueF || val.HawkFloatEquality(compareValueF);
					});
				case ComparisonOperator.DifferentBy:
					if (DifferentBy is not ulong differentBy) throw new InvalidOperationException();
					return watchList.Where(w => DifferByExactly(
						difference: differentBy,
						ReinterpretAsF64(GetValue(w)),
						compareValueF));
			}
		}

		private IEnumerable<IMiniWatch> CompareSpecificAddress(IEnumerable<IMiniWatch> watchList)
		{
			if (CompareValue is not ulong compareValue0) throw new InvalidOperationException();
			var compareValue = compareValue0 > long.MaxValue ? -1L : unchecked((long) compareValue0);
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
					if (DifferentBy is not ulong differentBy) throw new InvalidOperationException();
					return watchList.Where(w => DifferByExactly(difference: differentBy, w.Address, compareValue));
			}
		}

		private IEnumerable<IMiniWatch> CompareChanges(IEnumerable<IMiniWatch> watchList)
		{
			if (!_settings.IsDetailed()) throw new InvalidCastException(); //TODO matches previous behaviour; was this intended to skip processing? --yoshi
			if (CompareValue is not ulong compareValue0) throw new InvalidOperationException();
			var compareValue = compareValue0 > int.MaxValue ? -1L : unchecked((int) compareValue0);
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
					if (DifferentBy is not ulong differentBy) throw new InvalidOperationException();
					return watchList
						.Cast<IMiniWatchDetails>()
						.Where(w => DifferByExactly(difference: differentBy, w.ChangeCount, compareValue));
			}
		}

		private IEnumerable<IMiniWatch> CompareDifference(IEnumerable<IMiniWatch> watchList)
		{
			if (CompareValue is not ulong compareValue0) throw new InvalidOperationException();
			var compareValue = unchecked((long) compareValue0);
			if (_settings.Type is not WatchDisplayType.Float)
			{
				switch (Operator)
				{
					default:
					case ComparisonOperator.Equal:
						return watchList.Where(w => SignExtendAsNeeded(GetValue(w)) - SignExtendAsNeeded(w.Previous) == compareValue);
					case ComparisonOperator.NotEqual:
						return watchList.Where(w => SignExtendAsNeeded(GetValue(w)) - SignExtendAsNeeded(w.Previous) != compareValue);
					case ComparisonOperator.GreaterThan:
						return watchList.Where(w => SignExtendAsNeeded(GetValue(w)) - SignExtendAsNeeded(w.Previous) > compareValue);
					case ComparisonOperator.GreaterThanEqual:
						return watchList.Where(w => SignExtendAsNeeded(GetValue(w)) - SignExtendAsNeeded(w.Previous) >= compareValue);
					case ComparisonOperator.LessThan:
						return watchList.Where(w => SignExtendAsNeeded(GetValue(w)) - SignExtendAsNeeded(w.Previous) < compareValue);
					case ComparisonOperator.LessThanEqual:
						return watchList.Where(w => SignExtendAsNeeded(GetValue(w)) - SignExtendAsNeeded(w.Previous) <= compareValue);
					case ComparisonOperator.DifferentBy:
						if (DifferentBy is not ulong differentBy) throw new InvalidOperationException();
						return watchList.Where(w => DifferByExactly(
							difference: differentBy,
							SignExtendAsNeeded(GetValue(w)) - SignExtendAsNeeded(w.Previous),
							compareValue));
				}
			}
			var compareValueF = ReinterpretAsF64(compareValue0);
			switch (Operator)
			{
				default:
				case ComparisonOperator.Equal:
					return watchList.Where(w => (ReinterpretAsF64(GetValue(w)) - ReinterpretAsF64(w.Previous)).HawkFloatEquality(compareValueF));
				case ComparisonOperator.NotEqual:
					return watchList.Where(w => !(ReinterpretAsF64(GetValue(w)) - ReinterpretAsF64(w.Previous)).HawkFloatEquality(compareValueF));
				case ComparisonOperator.GreaterThan:
					return watchList.Where(w => ReinterpretAsF64(GetValue(w)) - ReinterpretAsF64(w.Previous) > compareValueF);
				case ComparisonOperator.GreaterThanEqual:
					return watchList.Where(w =>
					{
						var diff = ReinterpretAsF64(GetValue(w)) - ReinterpretAsF64(w.Previous);
						return diff > compareValueF || diff.HawkFloatEquality(compareValueF);
					});
				case ComparisonOperator.LessThan:
					return watchList.Where(w => ReinterpretAsF64(GetValue(w)) - ReinterpretAsF64(w.Previous) < compareValueF);
				case ComparisonOperator.LessThanEqual:
					return watchList.Where(w =>
					{
						var diff = ReinterpretAsF64(GetValue(w)) - ReinterpretAsF64(w.Previous);
						return diff < compareValueF || diff.HawkFloatEquality(compareValueF);
					});
				case ComparisonOperator.DifferentBy:
					if (DifferentBy is not ulong differentBy) throw new InvalidOperationException();
					return watchList.Where(w => DifferByExactly(
						difference: differentBy,
						ReinterpretAsF64(GetValue(w)) - ReinterpretAsF64(w.Previous),
						compareValueF));
			}
		}

		private long SignExtendAsNeeded(ulong val)
		{
			if (_settings.Type is not WatchDisplayType.Signed) return unchecked((long) val);
			return _settings.Size switch
			{
				WatchSize.Byte => (sbyte) val,
				WatchSize.Word => (short) val,
				WatchSize.DWord => (int) val,
				_ => (sbyte) val,
			};
		}

		private ulong GetValue(IMiniWatch watch)
		{
			if (watch is IMiniWatchDetails detailedWatch)
			{
				return detailedWatch.Current;
			}
			return watch.GetValue(Domain, bigEndian: _settings.BigEndian);
		}

		private bool CanDoCompareType(Compare compareType)
		{
			return _settings.Mode switch
			{
				SearchMode.Detailed => true,
				SearchMode.Fast => (compareType != Compare.Changes),
				_ => true,
			};
		}
	}
}
