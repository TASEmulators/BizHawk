using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class LuaFunctionList : List<NamedLuaFunction>
	{
		public NamedLuaFunction this[string guid] => 
			this.FirstOrDefault(nlf => nlf.Guid.ToString() == guid);

		public new bool Remove(NamedLuaFunction function)
		{
			if (Global.Emulator.InputCallbacksAvailable())
			{
				Global.Emulator.AsInputPollable().InputCallbacks.Remove(function.Callback);
			}

			if (Global.Emulator.MemoryCallbacksAvailable())
			{
				Global.Emulator.AsDebuggable().MemoryCallbacks.Remove(function.MemCallback);
			}

			return base.Remove(function);
		}

		public void RemoveForFile(LuaFile file)
		{
			var functionsToRemove = this
				.ForFile(file)
				.ToList();

			foreach (var function in functionsToRemove)
			{
				Remove(function);
			}
		}

		public new void Clear()
		{
			if (Global.Emulator.InputCallbacksAvailable())
			{
				Global.Emulator.AsInputPollable().InputCallbacks.RemoveAll(this.Select(w => w.Callback));
			}

			if (Global.Emulator.MemoryCallbacksAvailable())
			{
				var memoryCallbacks = Global.Emulator.AsDebuggable().MemoryCallbacks;
				memoryCallbacks.RemoveAll(this.Select(w => w.MemCallback));
			}

			base.Clear();
		}
	}

	public static class LuaFunctionListExtensions
	{
		public static IEnumerable<NamedLuaFunction> ForFile(this IEnumerable<NamedLuaFunction> list, LuaFile luaFile)
		{
			return list
				.Where(l => l.LuaFile.Path == luaFile.Path
					|| l.LuaFile.Thread == luaFile.Thread);
		}

		public static IEnumerable<NamedLuaFunction> ForEvent(this IEnumerable<NamedLuaFunction> list, string eventName)
		{
			return list.Where(l => l.Event == eventName);
		}
	}
}
