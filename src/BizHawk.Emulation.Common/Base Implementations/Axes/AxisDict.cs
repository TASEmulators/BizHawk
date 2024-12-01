using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace BizHawk.Emulation.Common
{
	public sealed class AxisDict : IReadOnlyDictionary<string, AxisSpec>
	{
		private IList<string> _keys = new List<string>();

		private bool _mutable = true;

		private IDictionary<string, AxisSpec> _specs = new Dictionary<string, AxisSpec>();

		public int Count => _keys.Count;

		public bool HasContraints { get; private set; }

		public IEnumerable<string> Keys => _keys;

		public IEnumerable<AxisSpec> Values => _specs.Values;

		public string this[int index] => _keys[index];

		public AxisSpec this[string index]
		{
			get => _specs[index];
			set
			{
				AssertMutable();
				_specs[index] = value;
			}
		}

		public void Add(string key, AxisSpec value)
		{
			AssertMutable();
			_keys.Add(key);
			_specs.Add(key, value);
			if (value.Constraint != null) HasContraints = true;
		}

		public void Add(KeyValuePair<string, AxisSpec> item) => Add(item.Key, item.Value);

		private void AssertMutable()
		{
			const string ERR_MSG = "this " + nameof(AxisDict) + " has been built and sealed and may not be mutated";
			if (!_mutable) throw new InvalidOperationException(ERR_MSG);
		}

		public void Clear()
		{
			AssertMutable();
			_keys.Clear();
			_specs.Clear();
			HasContraints = false;
		}

		public bool ContainsKey(string key) => _keys.Contains(key);

		public IEnumerator<KeyValuePair<string, AxisSpec>> GetEnumerator()
			=> _keys.Select(key => new KeyValuePair<string, AxisSpec>(key, _specs[key])).GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public int IndexOf(string key) => _keys.IndexOf(key);

		public void MakeImmutable()
		{
			_mutable = false;
			_keys = _keys.ToImmutableList();
			_specs = _specs.ToImmutableDictionary();
		}

		public AxisSpec SpecAtIndex(int index) => this[_keys[index]];

		public bool TryGetValue(string key, out AxisSpec value) => _specs.TryGetValue(key, out value);
	}
}
