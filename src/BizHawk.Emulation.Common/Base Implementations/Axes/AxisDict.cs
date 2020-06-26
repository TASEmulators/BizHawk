#nullable enable

using System.Collections;
using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	public sealed class AxisDict : IReadOnlyDictionary<string, AxisSpec>
	{
		private readonly IList<string> _keys = new List<string>();

		private readonly IDictionary<string, AxisSpec> _specs = new Dictionary<string, AxisSpec>();

		public int Count => _keys.Count;

		public bool HasContraints { get; private set; }

		public IEnumerable<string> Keys => _keys;

		public IEnumerable<AxisSpec> Values => _specs.Values;

		public string this[int index] => _keys[index];

		public AxisSpec this[string index]
		{
			get => _specs[index];
			set => _specs[index] = value;
		}

		public void Add(string key, AxisSpec value)
		{
			_keys.Add(key);
			_specs.Add(key, value);
			if (value.Constraint != null) HasContraints = true;
		}

		public void Add(KeyValuePair<string, AxisSpec> item) => Add(item.Key, item.Value);

		public void Clear()
		{
			_keys.Clear();
			_specs.Clear();
			HasContraints = false;
		}

		public bool ContainsKey(string key) => _keys.Contains(key);

		public IEnumerator<KeyValuePair<string, AxisSpec>> GetEnumerator() => _specs.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public int IndexOf(string key) => _keys.IndexOf(key);

		public AxisSpec SpecAtIndex(int index) => this[_keys[index]];

		public bool TryGetValue(string key, out AxisSpec value) => _specs.TryGetValue(key, out value);
	}
}
