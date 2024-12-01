using System.Collections;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class LuaFunctionList : IEnumerable<NamedLuaFunction>
	{
		private readonly List<NamedLuaFunction> _functions = new List<NamedLuaFunction>();

		private readonly Action Changed;

		public LuaFunctionList(Action onChanged) => Changed = onChanged;

		public NamedLuaFunction this[string guid]
			=> _functions.Find(nlf => nlf.Guid.ToString() == guid);

		public void Add(NamedLuaFunction nlf)
		{
			_functions.Add(nlf);
			Changed();
		}

		public bool Remove(NamedLuaFunction function, IEmulator emulator)
		{
			if (emulator.InputCallbacksAvailable())
			{
				emulator.AsInputPollable().InputCallbacks.Remove(function.InputCallback);
			}

			if (emulator.MemoryCallbacksAvailable())
			{
				emulator.AsDebuggable().MemoryCallbacks.Remove(function.MemCallback);
			}

			var result = _functions.Remove(function);
			if (result)
			{
				Changed();
			}

			return result;
		}

		public void RemoveForFile(LuaFile file, IEmulator emulator)
		{
			var functionsToRemove = _functions.Where(l => l.LuaFile.Path == file.Path || ReferenceEquals(l.LuaFile.Thread, file.Thread)).ToList();

			foreach (var function in functionsToRemove)
			{
				Remove(function, emulator);
			}

			if (functionsToRemove.Count != 0)
			{
				Changed();
			}
		}

		public void Clear(IEmulator emulator)
		{
			if (emulator.InputCallbacksAvailable())
			{
				emulator.AsInputPollable().InputCallbacks.RemoveAll(_functions.Select(w => w.InputCallback));
			}

			if (emulator.MemoryCallbacksAvailable())
			{
				var memoryCallbacks = emulator.AsDebuggable().MemoryCallbacks;
				memoryCallbacks.RemoveAll(_functions.Select(w => w.MemCallback));
			}

			_functions.Clear();
			Changed();
		}

		public IEnumerator<NamedLuaFunction> GetEnumerator() => _functions.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => _functions.GetEnumerator();
	}
}
