using System;
using System.Collections.Concurrent;

namespace NLua
{
	internal class ObjectTranslatorPool
	{
		private static volatile ObjectTranslatorPool _instance = new ObjectTranslatorPool();

		private ConcurrentDictionary<LuaState, ObjectTranslator> translators = new ConcurrentDictionary<LuaState, ObjectTranslator>();

		public static ObjectTranslatorPool Instance => _instance;


		public void Add(LuaState luaState, ObjectTranslator translator)
		{
			if(!translators.TryAdd(luaState, translator))
				throw new ArgumentException("An item with the same key has already been added. ", nameof(luaState));
		}

		public ObjectTranslator Find(LuaState luaState)
		{
			ObjectTranslator translator;

			if(!translators.TryGetValue(luaState, out translator))
			{
				LuaState main = luaState.MainThread;

				if (!translators.TryGetValue(main, out translator))
					throw new Exception("Invalid luaState, couldn't find ObjectTranslator");
			}
			return translator;
		}

		public void Remove(LuaState luaState)
		{
			ObjectTranslator translator;
			translators.TryRemove(luaState, out translator);
		}
	}
}

