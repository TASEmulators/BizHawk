using System;

using NLua.Native;

namespace NLua.GenerateEventAssembly
{
	internal class ClassGenerator
	{
		private readonly ObjectTranslator _translator;
		private readonly Type _klass;

		public ClassGenerator(ObjectTranslator objTranslator, Type typeClass)
		{
			_translator = objTranslator;
			_klass = typeClass;
		}

		public object ExtractGenerated(LuaState luaState, int stackPos)
			=> CodeGeneration.Instance.GetClassInstance(_klass, _translator.GetTable(luaState, stackPos));
	}
}
