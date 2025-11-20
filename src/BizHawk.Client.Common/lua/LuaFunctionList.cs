using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Client.Common
{
	public class LuaFunctionList : IEnumerable<NamedLuaFunction>
	{
		private readonly List<NamedLuaFunction> _functions = new List<NamedLuaFunction>();

		private readonly Action Changed;

		public int Count
			=> _functions.Count;

		public LuaFunctionList(Action onChanged) => Changed = onChanged;

		public NamedLuaFunction/*?*/ this[string guid]
			=> Guid.TryParseExact(guid, format: "D", out var parsed)
				? _functions.Find(nlf => nlf.Guid == parsed)
				: null;

		public void Add(NamedLuaFunction nlf)
		{
			_functions.Add(nlf);
			Changed();
		}

		public bool Remove(NamedLuaFunction function)
		{
			if (!RemoveInner(function)) return false;
			Changed();
			return true;
		}

		private bool RemoveInner(NamedLuaFunction function)
		{
			if (!_functions.Remove(function)) return false;
			function.OnRemove?.Invoke();
			return true;
		}

		public void RemoveForFile(LuaFile file)
		{
			var functionsToRemove = _functions.Where(l => l.LuaFile.Path == file.Path || ReferenceEquals(l.LuaFile.Thread, file.Thread)).ToList();

			foreach (var function in functionsToRemove)
			{
				_ = RemoveInner(function);
			}

			if (functionsToRemove.Count != 0)
			{
				Changed();
			}
		}

		public void Clear()
		{
			if (Count is 0) return;
			foreach (var function in _functions) function.OnRemove?.Invoke();
			_functions.Clear();
			Changed();
		}

		public IEnumerator<NamedLuaFunction> GetEnumerator() => _functions.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => _functions.GetEnumerator();
	}
}
