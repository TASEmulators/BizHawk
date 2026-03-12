using System;
using System.Collections.Concurrent;

using NLua.Native;

namespace NLua
{
	internal class ObjectTranslatorPool
	{
		private readonly ConcurrentDictionary<LuaState, ObjectTranslator> translators = new();
		public static ObjectTranslatorPool Instance { get; } = new();

		public void Add(LuaState luaState, ObjectTranslator translator)
		{
			if (!translators.TryAdd(luaState, translator))
			{
				throw new ArgumentException("An item with the same key has already been added. ", nameof(luaState));
			}
		}

		public ObjectTranslator Find(LuaState luaState)
		{
			if (!translators.TryGetValue(luaState, out var translator))
			{
				var main = luaState.MainThread;
				if (!translators.TryGetValue(main, out translator))
				{
					throw new Exception("Invalid luaState, couldn't find ObjectTranslator");
				}
			}

			return translator;
		}

		public void Remove(LuaState luaState)
			=> translators.TryRemove(luaState, out _);
	}
}

