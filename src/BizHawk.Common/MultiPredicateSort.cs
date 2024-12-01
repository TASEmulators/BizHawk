using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Common
{
#if false
	/// <summary>Sorts using a reorderable list of predicates.</summary>
	/// <seealso cref="RigidMultiPredicateSort{T}"/>
	public sealed class MultiPredicateSort<T>
	{
		private readonly int _count;

		/// <remarks>TODO would an array be faster?</remarks>
		private readonly List<(string ID, bool IsDesc)> _order;

		private readonly IReadOnlyDictionary<string, Func<T, IComparable>> _predicates;

		public MultiPredicateSort(IReadOnlyDictionary<string, Func<T, IComparable>> predicates)
		{
			_count = predicates.Count;
			if (_count == 0) throw new ArgumentException("must have at least 1 predicate", nameof(predicates));
			_order = predicates.Select(kvp => (kvp.Key, false)).ToList();
			_predicates = predicates;
		}

		public List<T> AppliedTo(IReadOnlyCollection<T> list)
		{
			var temp = _order[0].IsDesc
				? list.OrderByDescending(_predicates[_order[0].ID])
				: list.OrderBy(_predicates[_order[0].ID]);
			for (var i = 1; i < _count; i++)
			{
				temp = _order[i].IsDesc
					? temp.ThenByDescending(_predicates[_order[i].ID])
					: temp.ThenBy(_predicates[_order[i].ID]);
			}
			return temp.ToList();
		}
	}
#endif

	/// <summary>Sorts using a single primary predicate, with subsorts using the remaining predicates in order.</summary>
#if false
	/// <seealso cref="MultiPredicateSort{T}"/>
#endif
	public sealed class RigidMultiPredicateSort<T>
	{
		private readonly IReadOnlyDictionary<string, Func<T, IComparable>> _predicates;

		public RigidMultiPredicateSort(IReadOnlyDictionary<string, Func<T, IComparable>> predicates)
		{
			if (predicates.Count == 0) throw new ArgumentException("must have at least 1 predicate", nameof(predicates));
			_predicates = predicates;
		}

		/// <remarks>sorts using <paramref name="idOfFirst"/> asc/desc (by <paramref name="firstIsDesc"/>), then by the remaining predicates, all asc</remarks>
		public List<T> AppliedTo(IReadOnlyCollection<T> list, string idOfFirst, bool firstIsDesc = false)
		{
			var temp = firstIsDesc
				? list.OrderByDescending(_predicates[idOfFirst])
				: list.OrderBy(_predicates[idOfFirst]);
			foreach (var (id, pred) in _predicates)
			{
				if (id == idOfFirst) continue;
				temp = temp.ThenBy(pred);
			}
			return temp.ToList();
		}

		public List<T> AppliedTo(IReadOnlyCollection<T> list, string idOfFirst, IReadOnlyDictionary<string, bool> isDescMap)
		{
			var temp = isDescMap[idOfFirst]
				? list.OrderByDescending(_predicates[idOfFirst])
				: list.OrderBy(_predicates[idOfFirst]);
			foreach (var (id, pred) in _predicates)
			{
				if (id == idOfFirst) continue;
				temp = isDescMap[id] ? temp.ThenByDescending(pred) : temp.ThenBy(pred);
			}
			return temp.ToList();
		}
	}
}
