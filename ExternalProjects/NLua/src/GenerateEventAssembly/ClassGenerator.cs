using System;

namespace NLua
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
		{
			return CodeGeneration.Instance.GetClassInstance(_klass, _translator.GetTable(luaState, stackPos));
		}
	}
}
