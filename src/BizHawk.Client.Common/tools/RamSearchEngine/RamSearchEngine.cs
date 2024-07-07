using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;
using static BizHawk.Common.NumberExtensions.NumberExtensions;

namespace BizHawk.Client.Common.RamSearchEngine
{
	public class RamSearchEngine
	{
		private Compare _compareTo = Compare.Previous;

		private IMiniWatch[] _watchList = [ ];
		private readonly SearchEngineSettings _settings;
		private readonly UndoHistory<IEnumerable<IMiniWatch>> _history = new(true, [ ]); //TODO use IList instead of IEnumerable and stop calling `.ToArray()` (i.e. cloning) on reads and writes?

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

		public RamSearchEngine(SearchEngineSettings settings, IMemoryDomains memoryDomains, Compare compareTo, uint? compareValue, uint? differentBy)
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
					for (var i = 0; i < _watchList.Length; i++) _watchList[i] = new MiniByteWatch(domain, i);
					break;
				case WatchSize.Word:
					for (var i = 0; i < _watchList.Length; i++) _watchList[i] = new MiniWordWatch(domain, i * stepSize, _settings.BigEndian);
					break;
				case WatchSize.DWord:
					for (var i = 0; i < _watchList.Length; i++) _watchList[i] = new MiniDWordWatch(domain, i * stepSize, _settings.BigEndian);
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
				_watchList[index].Previous,
				_settings.IsDetailed() ? _watchList[index].ChangeCount : 0);

		public int DoSearch(bool updatePrevious)
		{
			int before = _watchList.Length;

			Update(updatePrevious);

			using (Domain.EnterExit())
			{
				_watchList = _compareTo switch
				{
					Compare.Previous => ComparePrevious(_watchList).ToArray(),
					Compare.SpecificValue => CompareSpecificValue(_watchList).ToArray(),
					Compare.SpecificAddress => CompareSpecificAddress(_watchList).ToArray(),
					Compare.Changes => CompareChanges(_watchList).ToArray(),
					Compare.Difference => CompareDifference(_watchList).ToArray(),
					_ => ComparePrevious(_watchList).ToArray()
				};
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
			addressWatch.Update(PreviousType.Original, _settings.Domain, _settings.BigEndian);
			IMiniWatch[] listOfOne = [ addressWatch ];

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

		public uint? CompareValue { get; set; }

		public ComparisonOperator Operator { get; set; }

		/// <remarks>
		/// zero 07-sep-2014 - this isn't ideal. but don't bother changing it (to a long, for instance) until it can support floats. maybe store it as a double here.<br/>
		/// it already supported floats by way of reinterpret-cast, it just wasn't implemented correctly on this side --yoshi
		/// </remarks>
		public uint? DifferentBy { get; set; }

		public void Update(bool updatePrevious)
		{
			using var @lock = _settings.Domain.EnterExit();
			foreach (var watch in _watchList)
			{
				watch.Update(updatePrevious ? _settings.PreviousType : PreviousType.Original, _settings.Domain, _settings.BigEndian);
			}
		}

		public void SetType(WatchDisplayType type) => _settings.Type = type;

		public void SetEndian(bool bigEndian) => _settings.BigEndian = bigEndian;

		public void SetPreviousType(PreviousType type) => _settings.PreviousType = type;

		public void SetMode(SearchMode mode) => _settings.Mode = mode;

		public void SetPreviousToCurrent()
		{
			Array.ForEach(_watchList, static w => w.SetPreviousToCurrent());
		}

		public void ClearChangeCounts()
		{
			foreach (var watch in _watchList)
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
			var list = _settings.Size switch
			{
				WatchSize.Byte => addresses.ToBytes(_settings),
				WatchSize.Word => addresses.ToWords(_settings),
				WatchSize.DWord => addresses.ToDWords(_settings),
				_ => addresses.ToBytes(_settings)
			};

			_watchList = (append ? _watchList.Concat(list) : list).ToArray();
		}

		public void Sort(string column, bool reverse)
		{
			switch (column)
			{
				case WatchList.Address:
					_watchList = _watchList.OrderBy(w => w.Address, reverse).ToArray();
					break;
				case WatchList.Value:
					_watchList = _watchList.OrderBy(w => w.Current, reverse).ToArray();
					break;
				case WatchList.Prev:
					_watchList = _watchList.OrderBy(w => w.Previous, reverse).ToArray();
					break;
				case WatchList.ChangesCol:
					_watchList = _watchList.OrderBy(w => w.ChangeCount, reverse).ToArray();
					break;
				case WatchList.Diff:
					_watchList = _watchList.OrderBy(w => w.Current - w.Previous, reverse).ToArray();
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
						return watchList.Where(w => SignExtendAsNeeded(w.Current) == SignExtendAsNeeded(w.Previous));
					case ComparisonOperator.NotEqual:
						return watchList.Where(w => SignExtendAsNeeded(w.Current) != SignExtendAsNeeded(w.Previous));
					case ComparisonOperator.GreaterThan:
						return watchList.Where(w => SignExtendAsNeeded(w.Current) > SignExtendAsNeeded(w.Previous));
					case ComparisonOperator.GreaterThanEqual:
						return watchList.Where(w => SignExtendAsNeeded(w.Current) >= SignExtendAsNeeded(w.Previous));
					case ComparisonOperator.LessThan:
						return watchList.Where(w => SignExtendAsNeeded(w.Current) < SignExtendAsNeeded(w.Previous));
					case ComparisonOperator.LessThanEqual:
						return watchList.Where(w => SignExtendAsNeeded(w.Current) <= SignExtendAsNeeded(w.Previous));
					case ComparisonOperator.DifferentBy:
						if (DifferentBy is not uint differentBy) throw new InvalidOperationException();
						return watchList.Where(w =>
							differentBy == Math.Abs(SignExtendAsNeeded(w.Current) - SignExtendAsNeeded(w.Previous)));
				}
			}
			switch (Operator)
			{
				default:
				case ComparisonOperator.Equal:
					return watchList.Where(w => ReinterpretAsF32(w.Current).HawkFloatEquality(ReinterpretAsF32(w.Previous)));
				case ComparisonOperator.NotEqual:
					return watchList.Where(w => !ReinterpretAsF32(w.Current).HawkFloatEquality(ReinterpretAsF32(w.Previous)));
				case ComparisonOperator.GreaterThan:
					return watchList.Where(w => ReinterpretAsF32(w.Current) > ReinterpretAsF32(w.Previous));
				case ComparisonOperator.GreaterThanEqual:
					return watchList.Where(w =>
					{
						var val = ReinterpretAsF32(w.Current);
						var prev = ReinterpretAsF32(w.Previous);
						return val > prev || val.HawkFloatEquality(prev);
					});
				case ComparisonOperator.LessThan:
					return watchList.Where(w => ReinterpretAsF32(w.Current) < ReinterpretAsF32(w.Previous));
				case ComparisonOperator.LessThanEqual:
					return watchList.Where(w =>
					{
						var val = ReinterpretAsF32(w.Current);
						var prev = ReinterpretAsF32(w.Previous);
						return val < prev || val.HawkFloatEquality(prev);
					});
				case ComparisonOperator.DifferentBy:
					if (DifferentBy is not uint differentBy) throw new InvalidOperationException();
					var differentByF = ReinterpretAsF32(differentBy);
					return watchList.Where(w => Math.Abs(ReinterpretAsF32(w.Current) - ReinterpretAsF32(w.Previous))
						.HawkFloatEquality(differentByF));
			}
		}

		private IEnumerable<IMiniWatch> CompareSpecificValue(IEnumerable<IMiniWatch> watchList)
		{
			if (CompareValue is not uint compareValue) throw new InvalidOperationException();
			if (_settings.Type is not WatchDisplayType.Float)
			{
				switch (Operator)
				{
					default:
					case ComparisonOperator.Equal:
						return watchList.Where(w => SignExtendAsNeeded(w.Current) == SignExtendAsNeeded(compareValue));
					case ComparisonOperator.NotEqual:
						return watchList.Where(w => SignExtendAsNeeded(w.Current) != SignExtendAsNeeded(compareValue));
					case ComparisonOperator.GreaterThan:
						return watchList.Where(w => SignExtendAsNeeded(w.Current) > SignExtendAsNeeded(compareValue));
					case ComparisonOperator.GreaterThanEqual:
						return watchList.Where(w => SignExtendAsNeeded(w.Current) >= SignExtendAsNeeded(compareValue));
					case ComparisonOperator.LessThan:
						return watchList.Where(w => SignExtendAsNeeded(w.Current) < SignExtendAsNeeded(compareValue));
					case ComparisonOperator.LessThanEqual:
						return watchList.Where(w => SignExtendAsNeeded(w.Current) <= SignExtendAsNeeded(compareValue));
					case ComparisonOperator.DifferentBy:
						if (DifferentBy is not uint differentBy) throw new InvalidOperationException();
						return watchList.Where(w =>
							differentBy == Math.Abs(SignExtendAsNeeded(w.Current) - SignExtendAsNeeded(compareValue)));
				}
			}
			var compareValueF = ReinterpretAsF32(compareValue);
			switch (Operator)
			{
				default:
				case ComparisonOperator.Equal:
					return watchList.Where(w => ReinterpretAsF32(w.Current).HawkFloatEquality(compareValueF));
				case ComparisonOperator.NotEqual:
					return watchList.Where(w => !ReinterpretAsF32(w.Current).HawkFloatEquality(compareValueF));
				case ComparisonOperator.GreaterThan:
					return watchList.Where(w => ReinterpretAsF32(w.Current) > compareValueF);
				case ComparisonOperator.GreaterThanEqual:
					return watchList.Where(w =>
					{
						var val = ReinterpretAsF32(w.Current);
						return val > compareValueF || val.HawkFloatEquality(compareValueF);
					});
				case ComparisonOperator.LessThan:
					return watchList.Where(w => ReinterpretAsF32(w.Current) < compareValueF);
				case ComparisonOperator.LessThanEqual:
					return watchList.Where(w =>
					{
						var val = ReinterpretAsF32(w.Current);
						return val < compareValueF || val.HawkFloatEquality(compareValueF);
					});
				case ComparisonOperator.DifferentBy:
					if (DifferentBy is not uint differentBy) throw new InvalidOperationException();
					var differentByF = ReinterpretAsF32(differentBy);
					return watchList.Where(w => Math.Abs(ReinterpretAsF32(w.Current) - compareValueF)
						.HawkFloatEquality(differentByF));
			}
		}

		private IEnumerable<IMiniWatch> CompareSpecificAddress(IEnumerable<IMiniWatch> watchList)
		{
			if (CompareValue is not uint compareValue) throw new InvalidOperationException();
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
					if (DifferentBy is not uint differentBy) throw new InvalidOperationException();
					return watchList.Where(w => Math.Abs(w.Address - compareValue) == differentBy);
			}
		}

		private IEnumerable<IMiniWatch> CompareChanges(IEnumerable<IMiniWatch> watchList)
		{
			if (CompareValue is not uint compareValue) throw new InvalidOperationException();
			switch (Operator)
			{
				default:
				case ComparisonOperator.Equal:
					return watchList.Where(w => w.ChangeCount == compareValue);
				case ComparisonOperator.NotEqual:
					return watchList.Where(w => w.ChangeCount != compareValue);
				case ComparisonOperator.GreaterThan:
					return watchList.Where(w => w.ChangeCount > compareValue);
				case ComparisonOperator.GreaterThanEqual:
					return watchList.Where(w => w.ChangeCount >= compareValue);
				case ComparisonOperator.LessThan:
					return watchList.Where(w => w.ChangeCount < compareValue);
				case ComparisonOperator.LessThanEqual:
					return watchList.Where(w => w.ChangeCount <= compareValue);
				case ComparisonOperator.DifferentBy:
					if (DifferentBy is not uint differentBy) throw new InvalidOperationException();
					return watchList.Where(w => Math.Abs(w.ChangeCount - compareValue) == differentBy);
			}
		}

		private IEnumerable<IMiniWatch> CompareDifference(IEnumerable<IMiniWatch> watchList)
		{
			if (CompareValue is not uint compareValue) throw new InvalidCastException(); //TODO typo for IOE?
			if (_settings.Type is not WatchDisplayType.Float)
			{
				switch (Operator)
				{
					default:
					case ComparisonOperator.Equal:
						return watchList.Where(w => SignExtendAsNeeded(w.Current) - SignExtendAsNeeded(w.Previous) == compareValue);
					case ComparisonOperator.NotEqual:
						return watchList.Where(w => SignExtendAsNeeded(w.Current) - SignExtendAsNeeded(w.Previous) != compareValue);
					case ComparisonOperator.GreaterThan:
						return watchList.Where(w => SignExtendAsNeeded(w.Current) - SignExtendAsNeeded(w.Previous) > compareValue);
					case ComparisonOperator.GreaterThanEqual:
						return watchList.Where(w => SignExtendAsNeeded(w.Current) - SignExtendAsNeeded(w.Previous) >= compareValue);
					case ComparisonOperator.LessThan:
						return watchList.Where(w => SignExtendAsNeeded(w.Current) - SignExtendAsNeeded(w.Previous) < compareValue);
					case ComparisonOperator.LessThanEqual:
						return watchList.Where(w => SignExtendAsNeeded(w.Current) - SignExtendAsNeeded(w.Previous) <= compareValue);
					case ComparisonOperator.DifferentBy:
						if (DifferentBy is not uint differentBy) throw new InvalidOperationException();
						return watchList.Where(w =>
							differentBy == Math.Abs(Math.Abs(SignExtendAsNeeded(w.Current) - SignExtendAsNeeded(w.Previous)) - compareValue));
				}
			}
			var compareValueF = ReinterpretAsF32(compareValue);
			switch (Operator)
			{
				default:
				case ComparisonOperator.Equal:
					return watchList.Where(w => (ReinterpretAsF32(w.Current) - ReinterpretAsF32(w.Previous)).HawkFloatEquality(compareValueF));
				case ComparisonOperator.NotEqual:
					return watchList.Where(w => !(ReinterpretAsF32(w.Current) - ReinterpretAsF32(w.Previous)).HawkFloatEquality(compareValueF));
				case ComparisonOperator.GreaterThan:
					return watchList.Where(w => ReinterpretAsF32(w.Current) - ReinterpretAsF32(w.Previous) > compareValueF);
				case ComparisonOperator.GreaterThanEqual:
					return watchList.Where(w =>
					{
						var diff = ReinterpretAsF32(w.Current) - ReinterpretAsF32(w.Previous);
						return diff > compareValueF || diff.HawkFloatEquality(compareValueF);
					});
				case ComparisonOperator.LessThan:
					return watchList.Where(w => ReinterpretAsF32(w.Current) - ReinterpretAsF32(w.Previous) < compareValueF);
				case ComparisonOperator.LessThanEqual:
					return watchList.Where(w =>
					{
						var diff = ReinterpretAsF32(w.Current) - ReinterpretAsF32(w.Previous);
						return diff < compareValueF || diff.HawkFloatEquality(compareValueF);
					});
				case ComparisonOperator.DifferentBy:
					if (DifferentBy is not uint differentBy) throw new InvalidOperationException();
					var differentByF = ReinterpretAsF32(differentBy);
					return watchList.Where(w => Math.Abs(ReinterpretAsF32(w.Current) - ReinterpretAsF32(w.Previous) - compareValueF)
						.HawkFloatEquality(differentByF));
			}
		}

		private long SignExtendAsNeeded(long val)
		{
			if (_settings.Type != WatchDisplayType.Signed)
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

		private bool CanDoCompareType(Compare compareType)
		{
			return _settings.Mode switch
			{
				SearchMode.Detailed => true,
				SearchMode.Fast => compareType != Compare.Changes,
				_ => true
			};
		}
	}
}
