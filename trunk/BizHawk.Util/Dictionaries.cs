using System;
using System.Collections;
using System.Collections.Generic;

namespace BizHawk
{
	/// <summary>
	/// a Dictionary-of-lists with key K and values List&lt;V&gt;
	/// </summary>
	[Serializable]
	public class Bag<K, V> : BagBase<K, V, Dictionary<K, List<V>>, List<V>> { }

	/// <summary>
	/// a Dictionary-of-lists with key K and values List&lt;V&gt;
	/// </summary>
	[Serializable]
	public class SortedBag<K, V> : BagBase<K, V, SortedDictionary<K, List<V>>, List<V>> { }

	/// <summary>
	/// A dictionary that creates new values on the fly as necessary so that any key you need will be defined. 
	/// </summary>
	/// <typeparam name="K">dictionary keys</typeparam>
	/// <typeparam name="V">dictionary values</typeparam>
	public class WorkingDictionary<K, V> : Dictionary<K, V> where V : new() {
		public new V this[K key] {
			get {
				V temp;
				if(!TryGetValue(key, out temp))
					temp = this[key] = new V();
				return temp;
			}
			set { base[key] = value; }
		}
	}

	/// <summary>
	/// base class for Bag and SortedBag
	/// </summary>
	/// <typeparam name="K">dictionary keys</typeparam>
	/// <typeparam name="V">list values</typeparam>
	/// <typeparam name="D">dictionary type</typeparam>
	/// <typeparam name="L">list type</typeparam>
	[Serializable]
	public class BagBase<K, V, D, L> : IEnumerable<V>
		where D : IDictionary<K, L>, new()
		where L : IList<V>, IEnumerable<V>, new() {
		D dictionary = new D();
		public void Add(K key, V val) {
			this[key].Add(val);
		}

		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
		public IEnumerator<V> GetEnumerator() {
			foreach(L lv in dictionary.Values)
				foreach(V v in lv)
					yield return v;
		}

		public IEnumerable KeyValuePairEnumerator { get { return dictionary; } }

		/// <summary>
		/// the list of keys contained herein
		/// </summary>
		public IList<K> Keys { get { return new List<K>(dictionary.Keys); } }



		public L this[K key] {
			get {
				L slot;
				if(!dictionary.TryGetValue(key, out slot))
					dictionary[key] = slot = new L();
				return slot;
			}
			set {
				dictionary[key] = value;
			}
		}
	}

}