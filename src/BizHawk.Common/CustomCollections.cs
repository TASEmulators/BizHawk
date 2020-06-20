using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;

namespace BizHawk.Common
{
	/// <summary>Wrapper over <see cref="WorkingDictionary{TKey, TValue}">WorkingDictionary</see>&lt;<typeparamref name="TKey"/>, <see cref="List{T}">List</see>&lt;<typeparamref name="TValue"/>>>.</summary>
	[Serializable]
	public class Bag<TKey, TValue> : IEnumerable<TValue> where TKey : notnull
	{
		private readonly WorkingDictionary<TKey, List<TValue>> dictionary = new WorkingDictionary<TKey, List<TValue>>();

		public IList<TKey> Keys => dictionary.Keys.ToList();

		public List<TValue> this[TKey key]
		{
#pragma warning disable CS8603 // the only call to the index setter of `dictionary` is this index setter, which only takes non-null `List<TValue>`s
			get => dictionary[key];
#pragma warning restore CS8603
			set => dictionary[key] = value;
		}

		public void Add(TKey key, IEnumerable<TValue> val) => this[key].AddRange(val);

		public void Add(TKey key, TValue val) => this[key].Add(val);

		public bool ContainsKey(TKey key) => dictionary.ContainsKey(key);

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public IEnumerator<TValue> GetEnumerator() => dictionary.Values.SelectMany(lv => lv).GetEnumerator();

		public IEnumerator<KeyValuePair<TKey, List<TValue>>> GetKVPEnumerator() => dictionary.GetEnumerator();
	}

	/// <summary>A dictionary whose index getter creates an entry if the requested key isn't part of the collection, making it always safe to use the returned value. The new entry's value will be the result of the default constructor of <typeparamref name="TValue"/>.</summary>
	[Serializable]
	public class WorkingDictionary<TKey, TValue> : Dictionary<TKey, TValue>
		where TKey : notnull
		where TValue : new()
	{
		public WorkingDictionary() {}

		protected WorkingDictionary(SerializationInfo info, StreamingContext context) : base(info, context) {}

		[property: MaybeNull]
		public new TValue this[TKey key]
		{
			get => TryGetValue(key, out var temp)
				? temp
				: (base[key] = new TValue());
			set => base[key] = value;
		}
	}
}
